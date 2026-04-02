using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;
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
        private Mock<IBackupCancellationRegistry> _mockCancellationRegistry;
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
            _mockCancellationRegistry = new Mock<IBackupCancellationRegistry>();

            _executor = new BackupJobExecutor
            {
                PlanManager = _mockPlanManager.Object,
                SqlBackupService = _mockSqlBackup.Object,
                CompressionService = _mockCompression.Object,
                NotificationService = _mockNotification.Object,
                RetentionService = _mockRetention.Object,
                FileBackupService = _mockFileBackup.Object,
                CloudOrchestrator = _mockCloudOrchestrator.Object,
                HistoryManager = _mockHistoryManager.Object,
                CancellationRegistry = _mockCancellationRegistry.Object
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
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataFactory.CreateCloudUploadResults());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — cloud orchestrator çağrılmalı
            _mockCloudOrchestrator.Verify(c => c.UploadToAllAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                plan.CloudTargets,
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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

        // ── Dosya Yedekleme Tetikleme Testleri ─────────────────────────────────

        [TestMethod]
        public async Task Execute_SqlJob_FileBackupEnabledNoSchedule_TriggersFileBackupAfterSql()
        {
            // Arrange — ayrı zamanlama yok; SQL job bittikten sonra dosya yedekleme çalışmalı
            var plan = TestDataFactory.CreatePlanWithFileBackup();
            plan.Databases = new List<string> { "TestDB" };
            plan.FileBackup.Schedule = null;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            _mockFileBackup.Setup(f => f.BackupFilesAsync(
                    plan, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileBackupResult> { TestDataFactory.CreateSuccessFileBackupResult() });

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — FileBackupService kesinlikle çağrılmalı
            _mockFileBackup.Verify(
                f => f.BackupFilesAsync(plan, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_SqlJob_FileBackupEnabledWithDedicatedSchedule_SkipsFileBackup()
        {
            // Arrange — ayrı zamanlama var; FileBackup kendi Quartz job'u ile çalışır, SQL job'undan tetiklenmemeli
            var plan = TestDataFactory.CreatePlanWithFileBackup();
            plan.Databases = new List<string> { "TestDB" };
            plan.FileBackup.Schedule = "0 0 4 ? * *";

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — kendi zamanlaması olduğu için SQL job'dan çağrılMAMALI
            _mockFileBackup.Verify(
                f => f.BackupFilesAsync(It.IsAny<BackupPlan>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_SqlJob_FileBackupDisabled_SkipsFileBackup()
        {
            // Arrange — FileBackup.IsEnabled = false; çalışmamalı
            var plan = TestDataFactory.CreatePlanWithFileBackup();
            plan.Databases = new List<string> { "TestDB" };
            plan.FileBackup.IsEnabled = false;
            plan.FileBackup.Schedule = null;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert
            _mockFileBackup.Verify(
                f => f.BackupFilesAsync(It.IsAny<BackupPlan>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_SqlJob_FileBackupConfigNull_SkipsFileBackup()
        {
            // Arrange — plan.FileBackup hiç yapılandırılmamış (null); çalışmamalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.FileBackup = null;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert
            _mockFileBackup.Verify(
                f => f.BackupFilesAsync(It.IsAny<BackupPlan>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_FileBackupType_OnlyCallsFileBackupService()
        {
            // Arrange — Quartz "FileBackup" tipi job; sadece FileBackupService çalışmalı
            var plan = TestDataFactory.CreatePlanWithFileBackup();

            SetJobData(plan.PlanId, "FileBackup");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockFileBackup.Setup(f => f.BackupFilesAsync(
                    plan, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileBackupResult> { TestDataFactory.CreateSuccessFileBackupResult() });

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — FileBackup çalışmalı
            _mockFileBackup.Verify(
                f => f.BackupFilesAsync(plan, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_FileBackupType_NeverCallsSqlBackupService()
        {
            // Arrange — Quartz "FileBackup" tipi job; SQL backup asla çalışmamalı
            var plan = TestDataFactory.CreatePlanWithFileBackup();

            SetJobData(plan.PlanId, "FileBackup");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockFileBackup.Setup(f => f.BackupFilesAsync(
                    plan, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileBackupResult>());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — SQL backup çağrılMAMALI
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Plan Ayar Testleri ──────────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_EmptyDatabaseList_NeverCallsSqlBackup()
        {
            // Arrange — plan.Databases boş; SQL backup çağrılmamalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string>();

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
        public async Task Execute_DifferentialType_PassesDifferentialTypeToService()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan(strategyType: BackupStrategyType.FullPlusDifferential);
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Differential");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Differential, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, backupType: SqlBackupType.Differential));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Differential tipi servisine iletilmeli
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Differential, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_IncrementalType_PassesIncrementalTypeToService()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan(strategyType: BackupStrategyType.FullPlusDifferentialPlusIncremental);
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Incremental");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Incremental, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, backupType: SqlBackupType.Incremental));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Incremental tipi servisine iletilmeli
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Incremental, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_NoEnabledCloudTargets_SkipsCloudUpload()
        {
            // Arrange — tüm cloud hedefleri devre dışı; CloudOrchestrator çağrılmamalı
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
            plan.Databases = new List<string> { "TestDB" };
            foreach (var target in plan.CloudTargets)
                target.IsEnabled = false;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — devre dışı hedefler için upload yapılmamalı
            _mockCloudOrchestrator.Verify(
                c => c.UploadToAllAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ── ChainValidator Testleri ───────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_ChainValidator_NoFullBackup_DifferentialPromotedToFull()
        {
            // Arrange — Full yedek dosyası yok; Differential → Full yükseltilmeli
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var plan = TestDataFactory.CreateValidPlan(strategyType: BackupStrategyType.FullPlusDifferential);
            plan.Databases = new List<string> { "TestDB" };
            plan.LocalPath = tempDir;

            _executor.ChainValidator = new BackupChainValidator();
            SetJobData(plan.PlanId, "Differential");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, backupType: SqlBackupType.Full));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Full yedek dosyası olmadığı için Differential → Full yükseltilmeli
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Differential, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Execute_ChainValidator_AutoPromoteThreshold_DifferentialPromotedToFull()
        {
            // Arrange — Full yedek var, diff eşiği aşıldı; Differential → Full yükseltilmeli
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            // Full yedek dosyasını eski tarihle oluştur
            string fullBakPath = Path.Combine(tempDir, "TestDB_Full_20250101_020000.bak");
            File.WriteAllText(fullBakPath, string.Empty);
            File.SetCreationTime(fullBakPath, DateTime.Now.AddDays(-10));

            // AutoPromoteToFullAfter = 1; 1 adet diff dosyası eşiği aşar
            string diffBakPath = Path.Combine(tempDir, "TestDB_Differential_20250102_020000.bak");
            File.WriteAllText(diffBakPath, string.Empty);

            var plan = TestDataFactory.CreateValidPlan(strategyType: BackupStrategyType.FullPlusDifferential);
            plan.Databases = new List<string> { "TestDB" };
            plan.LocalPath = tempDir;
            plan.Strategy.AutoPromoteToFullAfter = 1;

            _executor.ChainValidator = new BackupChainValidator();
            SetJobData(plan.PlanId, "Differential");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, backupType: SqlBackupType.Full));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — AutoPromote eşiği aşıldı; Full çalışmalı
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_ChainValidator_HasValidFull_BelowAutoPromote_DifferentialRunsAsDifferential()
        {
            // Arrange — Full yedek var, diff sayısı eşiğin altında; Differential olarak çalışmalı
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            // Geçerli Full yedek dosyası — diff dosyası yok; eşik = 7
            string fullBakPath = Path.Combine(tempDir, "TestDB_Full_20250101_020000.bak");
            File.WriteAllText(fullBakPath, string.Empty);
            File.SetCreationTime(fullBakPath, DateTime.Now.AddDays(-3));

            var plan = TestDataFactory.CreateValidPlan(strategyType: BackupStrategyType.FullPlusDifferential);
            plan.Databases = new List<string> { "TestDB" };
            plan.LocalPath = tempDir;
            // Strategy.AutoPromoteToFullAfter = 7 (varsayılan), diff sayısı = 0 → yükseltme yok

            _executor.ChainValidator = new BackupChainValidator();
            SetJobData(plan.PlanId, "Differential");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Differential, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, backupType: SqlBackupType.Differential));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Full yedek var, eşik aşılmadı; Differential olarak çalışmalı
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Differential, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "TestDB",
                    SqlBackupType.Full, It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Doğrulama Testleri ────────────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_VerifyAfterBackup_SqlVerifyFails_PipelineContinues()
        {
            // Arrange — SQL Verify başarısız; pipeline durmadan devam etmeli
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.VerifyAfterBackup = true;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            // Doğrulama başarısız
            _mockSqlBackup.Setup(s => s.VerifyBackupAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — VerifyResult=false kaydedilmeli, pipeline devam etmeli
            _mockHistoryManager.Verify(
                h => h.SaveResult(It.Is<BackupResult>(r => r.VerifyResult == false)),
                Times.Once);
            _mockRetention.Verify(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), plan.Notifications, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_VerifyAfterBackup_ArchiveVerifyFails_CompressionVerifiedSetToFalse()
        {
            // Arrange — Arşiv bütünlük doğrulaması başarısız; CompressionVerified=false, pipeline devam etmeli
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.VerifyAfterBackup = true;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            _mockSqlBackup.Setup(s => s.VerifyBackupAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Arşiv bütünlük doğrulaması başarısız
            _mockCompression.Setup(c => c.VerifyArchiveAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — CompressionVerified=false kaydedilmeli, pipeline devam etmeli
            _mockHistoryManager.Verify(
                h => h.SaveResult(It.Is<BackupResult>(r => r.CompressionVerified == false)),
                Times.Once);
            _mockRetention.Verify(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Sıkıştırma Testleri ───────────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_CompressionConfigNull_SkipsCompression()
        {
            // Arrange — plan.Compression null; CompressAsync çağrılmamalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.Compression = null;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — sıkıştırma çağrılmamalı; pipeline sonuna ulaşılmalı
            _mockCompression.Verify(c => c.CompressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);
        }

        // ── Bulut Upload Testleri ─────────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_CloudUploadThrows_PipelineContinues()
        {
            // Arrange — CloudOrchestrator hata fırlatır; Retention/History/Notify yine de çalışmalı
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
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
                .ReturnsAsync(1024L * 1024 * 30);

            _mockCloudOrchestrator.Setup(c => c.UploadToAllAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Ağ hatası — upload başarısız"));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — bulut upload hatası pipeline'ı durdurmaz
            _mockRetention.Verify(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()), Times.Once);
            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);
            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), plan.Notifications, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_NoCompressionConfig_WithCloudTargets_UploadsRawBakFile()
        {
            // Arrange — Sıkıştırma yok; bulut upload ham .bak dosyasını kullanmalı
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
            plan.Databases = new List<string> { "TestDB" };
            plan.Compression = null;

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var successResult = TestDataFactory.CreateSuccessResult(plan.PlanId);
            // Executor sıkıştırma yapmadığından CompressedFilePath boş olmalı
            successResult.CompressedFilePath = null;
            successResult.CompressedSizeBytes = 0;
            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            _mockCloudOrchestrator.Setup(c => c.UploadToAllAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataFactory.CreateCloudUploadResults());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — sıkıştırma çağrılmamalı; upload ham .bak yoluyla yapılmalı
            _mockCompression.Verify(c => c.CompressAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _mockCloudOrchestrator.Verify(
                c => c.UploadToAllAsync(
                    successResult.BackupFilePath,
                    It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        // ── Hata Dayanıklılığı Testleri ───────────────────────────────────────────

        [TestMethod]
        public async Task Execute_RetentionThrows_HistoryAndNotifyStillCalled()
        {
            // Arrange — Retention hatası; History ve Notify yine de çalışmalı
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
                .ReturnsAsync(1024L * 1024 * 30);

            _mockRetention.Setup(r => r.CleanupAsync(plan, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Retention I/O hatası"));

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Retention hatası History ve Notify'ı durdurmaz
            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Once);
            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), plan.Notifications, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Execute_NotificationsNull_DoesNotThrow()
        {
            // Arrange — plan.Notifications null; NotifyAsync çağrılmamalı, exception fırlatılmamalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };
            plan.Notifications = null;

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
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            Func<Task> act = async () => await _executor.Execute(_mockJobContext.Object);

            // Assert — exception fırlatılmamalı
            await act.Should().NotThrowAsync();

            // Bildirim servisi çağrılmamalı (Notifications null)
            _mockNotification.Verify(n => n.NotifyAsync(
                It.IsAny<BackupResult>(), It.IsAny<NotificationConfig>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Çoklu Veritabanı Testleri ─────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_MultipleDBs_FirstFails_SecondStillExecuted()
        {
            // Arrange — İlk DB başarısız; döngü durmadan ikinci DB çalışmalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "DB1", "DB2" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "DB1",
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateFailedResult(plan.PlanId));

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), "DB2",
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId, databaseName: "DB2"));

            _mockCompression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — her iki DB için backup ve history çağrılmalı
            _mockSqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            _mockHistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Exactly(2));
        }

        // ── İptal Testleri ────────────────────────────────────────────────────────

        [TestMethod]
        public async Task Execute_CancellationRequested_HandlesGracefully()
        {
            // Arrange — CancellationToken önceden iptal edilmiş; exception dışarıya iletilmemeli
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            _mockJobContext.Setup(c => c.CancellationToken).Returns(cts.Token);

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            // Act — OperationCanceledException içeride yutulmalı
            Func<Task> act = async () => await _executor.Execute(_mockJobContext.Object);

            // Assert
            await act.Should().NotThrowAsync();

            // İptal sonrası SQL backup çağrılmamalı
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
                { "backupType", backupType },
                { "manualTrigger", "false" }
            };
            _mockJobContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
        }

        private void SetJobData(string planId, string backupType, bool manualTrigger)
        {
            var jobDataMap = new JobDataMap
            {
                { "planId", planId },
                { "backupType", backupType },
                { "manualTrigger", manualTrigger ? "true" : "false" }
            };
            _mockJobContext.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
        }

        // ── İptal/Hata Temizlik (Cleanup) Testleri — K2 ──────────────────────────

        [TestMethod]
        public async Task Execute_SqlBackupThrowsOperationCanceled_CancellationRegistryUnregistered()
        {
            // Arrange — SQL yedek sırasında OperationCanceledException fırlatılırsa
            // CancellationRegistry.Unregister çağrılmalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Registry'den plan temizlenmeli
            _mockCancellationRegistry.Verify(
                r => r.Unregister(plan.PlanId), Times.Once);
        }

        [TestMethod]
        public async Task Execute_SqlBackupThrowsException_RaisesFailedActivity()
        {
            // Arrange — SQL yedek sırasında beklenmeyen hata fırlatılırsa
            // Failed activity event'i yayınlanmalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Disk dolu"));

            BackupActivityEventArgs capturedArgs = null;
            BackupActivityHub.ActivityChanged += (s, e) =>
            {
                if (e.ActivityType == BackupActivityType.Failed)
                    capturedArgs = e;
            };

            try
            {
                // Act
                await _executor.Execute(_mockJobContext.Object);

                // Assert — Failed event yayınlanmalı
                capturedArgs.Should().NotBeNull();
                capturedArgs.PlanId.Should().Be(plan.PlanId);
                capturedArgs.ActivityType.Should().Be(BackupActivityType.Failed);
                capturedArgs.Message.Should().Contain("Disk dolu");
            }
            finally
            {
                // Temizlik — statik event'ten handler'ı kaldır
                BackupActivityHub.ActivityChanged -= (s, e) => { };
            }
        }

        [TestMethod]
        public async Task Execute_CancellationDuringBackup_RaisesCancelledActivity()
        {
            // Arrange — İptal sırasında Cancelled activity event yayınlanmalı
            var plan = TestDataFactory.CreateValidPlan();
            plan.Databases = new List<string> { "TestDB" };

            using var cts = new CancellationTokenSource();
            _mockJobContext.Setup(c => c.CancellationToken).Returns(cts.Token);

            SetJobData(plan.PlanId, "Full");
            _mockPlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            _mockSqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                    (conn, db, type, path, prog, ct) =>
                    {
                        cts.Cancel();
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult(TestDataFactory.CreateSuccessResult());
                    });

            BackupActivityEventArgs capturedArgs = null;
            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Cancelled)
                    capturedArgs = e;
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                // Act
                await _executor.Execute(_mockJobContext.Object);

                // Assert — Cancelled event yayınlanmalı
                capturedArgs.Should().NotBeNull();
                capturedArgs.PlanId.Should().Be(plan.PlanId);
                capturedArgs.ActivityType.Should().Be(BackupActivityType.Cancelled);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Execute_SuccessfulPipeline_CompletedActivityRaised()
        {
            // Arrange — Tam pipeline başarıyla tamamlanınca Completed event yayınlanmalı
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
                .ReturnsAsync(1024L * 1024 * 30);

            BackupActivityEventArgs capturedArgs = null;
            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Completed)
                    capturedArgs = e;
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                // Act
                await _executor.Execute(_mockJobContext.Object);

                // Assert
                capturedArgs.Should().NotBeNull();
                capturedArgs.PlanId.Should().Be(plan.PlanId);
                capturedArgs.ActivityType.Should().Be(BackupActivityType.Completed);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Execute_CancellationRegistryRegisterCalledOnStart()
        {
            // Arrange — Job başladığında CancellationRegistry.Register çağrılmalı
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
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await _executor.Execute(_mockJobContext.Object);

            // Assert — Register ve Unregister sırayla çağrılmalı
            _mockCancellationRegistry.Verify(
                r => r.Register(plan.PlanId, It.IsAny<CancellationTokenSource>()), Times.Once);
            _mockCancellationRegistry.Verify(
                r => r.Unregister(plan.PlanId), Times.Once);
        }

        [TestMethod]
        public async Task Execute_CompressionThrowsOperationCanceled_PropagatesAsCancellation()
        {
            // Arrange — Sıkıştırma sırasında iptal; OperationCanceledException yeniden fırlatılmalı
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
                .ThrowsAsync(new OperationCanceledException());

            BackupActivityEventArgs capturedArgs = null;
            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Cancelled)
                    capturedArgs = e;
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                // Act
                await _executor.Execute(_mockJobContext.Object);

                // Assert — Cancelled event yayınlanmalı (OperationCanceledException propagate ediliyor)
                capturedArgs.Should().NotBeNull();
                capturedArgs.ActivityType.Should().Be(BackupActivityType.Cancelled);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Execute_CloudUploadThrowsOperationCanceled_PropagatesAsCancellation()
        {
            // Arrange — Bulut yükleme sırasında iptal; OperationCanceledException yeniden fırlatılmalı
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
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
                .ReturnsAsync(1024L * 1024 * 30);

            _mockCloudOrchestrator.Setup(c => c.UploadToAllAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<CloudTargetConfig>>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new OperationCanceledException());

            BackupActivityEventArgs capturedArgs = null;
            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Cancelled)
                    capturedArgs = e;
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                // Act
                await _executor.Execute(_mockJobContext.Object);

                // Assert — Cancelled event yayınlanmalı
                capturedArgs.Should().NotBeNull();
                capturedArgs.ActivityType.Should().Be(BackupActivityType.Cancelled);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }
    }
}
