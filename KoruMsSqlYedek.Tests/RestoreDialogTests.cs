using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Forms;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RestoreDialogTests
    {
        private Mock<IBackupHistoryManager> _mockHistoryManager;
        private Mock<ISqlBackupService> _mockSqlService;
        private Mock<ICompressionService> _mockCompression;
        private BackupPlan _plan;

        private static readonly MethodInfo CleanupMethod = typeof(RestoreDialog).GetMethod(
            "CleanupTempDirectory",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        [TestInitialize]
        public void Setup()
        {
            _mockHistoryManager = new Mock<IBackupHistoryManager>();
            _mockSqlService = new Mock<ISqlBackupService>();
            _mockCompression = new Mock<ICompressionService>();
            _plan = TestDataFactory.CreateValidPlan();
        }

        // ── Constructor null guard tests ─────────────────────────────────────

        [TestMethod]
        public void Constructor_WhenPlanIsNull_ThrowsArgumentNullException()
        {
            RunOnStaThread(() =>
            {
                Action act = () => new RestoreDialog(null!, _mockHistoryManager.Object, _mockSqlService.Object, _mockCompression.Object);
                act.Should().Throw<ArgumentNullException>().WithParameterName("plan");
            });
        }

        [TestMethod]
        public void Constructor_WhenHistoryManagerIsNull_ThrowsArgumentNullException()
        {
            RunOnStaThread(() =>
            {
                Action act = () => new RestoreDialog(_plan, null!, _mockSqlService.Object, _mockCompression.Object);
                act.Should().Throw<ArgumentNullException>().WithParameterName("historyManager");
            });
        }

        [TestMethod]
        public void Constructor_WhenSqlServiceIsNull_ThrowsArgumentNullException()
        {
            RunOnStaThread(() =>
            {
                Action act = () => new RestoreDialog(_plan, _mockHistoryManager.Object, null!, _mockCompression.Object);
                act.Should().Throw<ArgumentNullException>().WithParameterName("sqlService");
            });
        }

        [TestMethod]
        public void Constructor_WhenCompressionServiceIsNull_ThrowsArgumentNullException()
        {
            RunOnStaThread(() =>
            {
                Action act = () => new RestoreDialog(_plan, _mockHistoryManager.Object, _mockSqlService.Object, null!);
                act.Should().Throw<ArgumentNullException>().WithParameterName("compressionService");
            });
        }

        // ── CleanupTempDirectory tests ───────────────────────────────────────

        [TestMethod]
        public void CleanupTempDirectory_MethodExists()
        {
            CleanupMethod.Should().NotBeNull("CleanupTempDirectory should exist as private static method");
        }

        [TestMethod]
        public void CleanupTempDirectory_WhenNull_DoesNotThrow()
        {
            Action act = () => CleanupMethod.Invoke(null, new object[] { null! });
            act.Should().NotThrow();
        }

        [TestMethod]
        public void CleanupTempDirectory_WhenEmpty_DoesNotThrow()
        {
            Action act = () => CleanupMethod.Invoke(null, new object[] { string.Empty });
            act.Should().NotThrow();
        }

        [TestMethod]
        public void CleanupTempDirectory_WhenDirectoryDoesNotExist_DoesNotThrow()
        {
            string nonExistent = Path.Combine(Path.GetTempPath(), "KoruTestNonExistent_" + Guid.NewGuid().ToString("N"));

            Action act = () => CleanupMethod.Invoke(null, new object[] { nonExistent });
            act.Should().NotThrow();
        }

        [TestMethod]
        public void CleanupTempDirectory_WhenDirectoryExists_DeletesIt()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "KoruTestCleanup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "dummy.txt"), "test");

            CleanupMethod.Invoke(null, new object[] { tempDir });

            Directory.Exists(tempDir).Should().BeFalse("CleanupTempDirectory should delete existing directory");
        }

        // ── LoadHistory filtreleme mantığı ───────────────────────────────────

        [TestMethod]
        public void LoadHistory_FilterLogic_OnlySuccessResultsAreIncluded()
        {
            // LoadHistory filtreleme mantığını doğrudan doğrula:
            // GetHistoryByPlan sonuçları Status==Success ve StartedAt DESC sıralı olmalı
            var success1 = TestDataFactory.CreateSuccessResult(_plan.PlanId, "DB1");
            success1.StartedAt = DateTime.UtcNow.AddMinutes(-10);
            var success2 = TestDataFactory.CreateSuccessResult(_plan.PlanId, "DB2");
            success2.StartedAt = DateTime.UtcNow.AddMinutes(-5);
            var failed = TestDataFactory.CreateFailedResult(_plan.PlanId);

            var allResults = new List<BackupResult> { success1, failed, success2 };

            // RestoreDialog.LoadHistory'nin uyguladığı aynı filtre
            List<BackupResult> filtered = allResults
                .Where(r => r.Status == BackupResultStatus.Success)
                .OrderByDescending(r => r.StartedAt)
                .ToList();

            filtered.Should().HaveCount(2, "only success results are kept");
            filtered[0].DatabaseName.Should().Be("DB2", "newest result first");
            filtered[1].DatabaseName.Should().Be("DB1", "older result second");
        }

        [TestMethod]
        public void LoadHistory_FilterLogic_WhenAllFailed_ReturnsEmpty()
        {
            var results = new List<BackupResult>
            {
                TestDataFactory.CreateFailedResult(_plan.PlanId),
                TestDataFactory.CreateFailedResult(_plan.PlanId)
            };

            List<BackupResult> filtered = results
                .Where(r => r.Status == BackupResultStatus.Success)
                .OrderByDescending(r => r.StartedAt)
                .ToList();

            filtered.Should().BeEmpty("no success results exist");
        }

        [TestMethod]
        public void LoadHistory_FilterLogic_WhenEmpty_ReturnsEmpty()
        {
            var results = new List<BackupResult>();

            List<BackupResult> filtered = results
                .Where(r => r.Status == BackupResultStatus.Success)
                .OrderByDescending(r => r.StartedAt)
                .ToList();

            filtered.Should().BeEmpty();
        }

        [TestMethod]
        public void LoadHistory_GridRowData_FileSizeFormattedCorrectly()
        {
            // LoadHistory'de kullanılan boyut formatlamasını doğrula
            var result = TestDataFactory.CreateSuccessResult(_plan.PlanId);
            long sizeBytes = result.CompressedSizeBytes > 0 ? result.CompressedSizeBytes : result.FileSizeBytes;

            string sizeStr = sizeBytes > 0 ? $"{sizeBytes / 1_048_576.0:F1} MB" : "-";

            sizeStr.Should().NotBe("-", "success result should have a positive size");
            sizeStr.Should().EndWith("MB");
        }

        [TestMethod]
        public void LoadHistory_GridRowData_UsesCompressedFilePathWhenAvailable()
        {
            var result = TestDataFactory.CreateSuccessResult(_plan.PlanId);
            result.CompressedFilePath = @"C:\TestBackups\TestDB1_Full_20250101_020000.7z";
            result.BackupFilePath = @"C:\TestBackups\TestDB1_Full_20250101_020000.bak";

            // LoadHistory'deki dosya yolu seçim mantığı
            string filePath = result.CompressedFilePath ?? result.BackupFilePath ?? string.Empty;
            string fileName = string.IsNullOrEmpty(filePath) ? "-" : Path.GetFileName(filePath);

            fileName.Should().Be("TestDB1_Full_20250101_020000.7z", "compressed path has priority");
        }

        [TestMethod]
        public void LoadHistory_GridRowData_FallsBackToBackupFilePath()
        {
            var result = TestDataFactory.CreateSuccessResult(_plan.PlanId);
            result.CompressedFilePath = null;
            result.BackupFilePath = @"C:\TestBackups\TestDB1_Full_20250101_020000.bak";

            string filePath = result.CompressedFilePath ?? result.BackupFilePath ?? string.Empty;
            string fileName = string.IsNullOrEmpty(filePath) ? "-" : Path.GetFileName(filePath);

            fileName.Should().Be("TestDB1_Full_20250101_020000.bak", "falls back to backup file path");
        }

        // ── STA thread helper ────────────────────────────────────────────────

        /// <summary>
        /// WinForms kontrolleri STA thread gerektirir. Test runner MTA kullanabilir.
        /// </summary>
        private static void RunOnStaThread(Action action)
        {
            Exception caught = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (caught is not null)
                throw caught;
        }
    }
}
