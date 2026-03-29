using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Backup;
using KoruMsSqlYedek.Engine.Scheduling;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class BackupJobExecutorTests
    {
        private Mock<IPlanManager> _mockPlanManager;
        private Mock<ISqlBackupService> _mockSqlBackup;
        private Mock<ICompressionService> _mockCompression;
        private Mock<INotificationService> _mockNotification;
        private Mock<IRetentionService> _mockRetention;
        private Mock<IFileBackupService> _mockFileBackup;
        private Mock<ICloudUploadOrchestrator> _mockCloudOrchestrator;
        private Mock<IBackupHistoryManager> _mockHistoryManager;
        private Mock<IJobExecutionContext> _mockJobContext;
        private BackupJobExecutor _executor;

        [TestInitialize]
        public void Setup()
        {
            _mockPlanManager = new Mock<IPlanManager>();
            _mockSqlBackup = new Mock<ISqlBackupService>();
            _mockCompression = new Mock<ICompressionService>();
            _mockNotification = new Mock<INotificationService>();
            _mockRetention = new Mock<IRetentionService>();
            _mockFileBackup = new Mock<IFileBackupService>();
            _mockCloudOrchestrator = new Mock<ICloudUploadOrchestrator>();
            _mockHistoryManager = new Mock<IBackupHistoryManager>();

            _executor = new BackupJobExecutor
            {
                PlanManager = _mockPlanManager.Object,
                SqlBackupService = _mockSqlBackup.Object,
                CompressionService = _mockCompression.Object,
                NotificationService = _mockNotification.Object,
                RetentionService = _mockRetention.Object,
                FileBackupService = _mockFileBackup.Object,
                CloudOrchestrator = _mockCloudOrchestrator.Object,
                HistoryManager = _mockHistoryManager.Object
            };

            // JobExecutionContext mock
            _mockJobContext = new Mock<IJobExecutionContext>();
            var jobDataMap = new JobDataMap();
            _mockJobContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
            _mockJobContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        }

        [TestMethod]
        public async Task Execute_PlanNotFound_LogsErrorAndReturns()
        {
            // Arrange
            SetJobData("non-existent-plan", "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(It.IsAny<string>())).Returns((BackupPlan)null);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — SQL backup asla çağrılmamalı
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_PlanDisabled_SkipsExecution()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.IsEnabled = false;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_FullBackup_CallsBackupForEachDatabase()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "DB1", "DB2", "DB3" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — her veritabanı için 1 kez çağrılmalı
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }

        [TestMethod]
        public async Task Execute_SuccessfulBackup_CallsVerifyCompressRetentionHistoryNotify()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.VerifyAfterBackup = true;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var successResult = TestDataFactory.CreateSuccessResult(plan.PlanId);
            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            _mockSqlBackup.Setup(s => s.VerifyBackupAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — pipeline sırası: Backup → Verify → Compress → Retention → History → Notify
            _mockSqlBackup.Verify(s => s.VerifyBackupAsync(
                It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockCompression.Verify(c => c.CompressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);

            _mockRetention.Verify(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()), Times.Once);

            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);

            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), plan.Notifications,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_BackupFails_StillCallsHistoryAndNotify()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var failedResult = TestDataFactory.CreateFailedResult(plan.PlanId);
            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResult);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — başarısız olsa bile History ve Notify çağrılmalı
            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);
            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), plan.Notifications,
                It.IsAny<CancellationToken>()), Times.Once);

            // Compress ve Retention çağrılMAMALI
            _mockCompression.Verify(c => c.CompressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Never);

            _mockRetention.Verify(r => r.CleanupAsync(
                It.IsAny<BackupPlan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Execute_VerifyDisabled_SkipsVerification()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.VerifyAfterBackup = false;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert
            _mockSqlBackup.Verify(s => s.VerifyBackupAsync(
                It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Execute_WithCloudTargets_UploadsCompressedFile()
        {
            // Arrange
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var result = TestDataFactory.CreateSuccessResult(plan.PlanId);
            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            _mockCloudOrchestrator.Setup(c => c.UploadToAllAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateCloudUploadResults());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — cloud orchestrator çağrılmalı
            _mockCloudOrchestrator.Verify(c => c.UploadToAllAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                plan.CloudTargets,
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_CompressionError_ContinuesPipeline()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("7z.dll bulunamadı"));

            // Act — exception yutulmalı, pipeline devam etmeli
            await _executor.Execute(_mockJobContext.Object);

            // Assert — hata olmasına rağmen Retention ve History çağrılmalı
            _mockRetention.Verify(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_UnknownBackupType_DoesNotCallBackup()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            SetJobData(plan.PlanId, "InvalidType");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetJobData(string planId, string backupType)
        {
            var jobDataMap = new JobDataMap
            {
                { "planId", planId },
                { "backupType", backupType }
            };
            _mockJobContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
        }
    }
}
