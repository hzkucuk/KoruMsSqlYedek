using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.Retention;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class RetentionCleanupServiceTests
    {
        private string _testDir;
        private RetentionCleanupService _service;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "MikroSqlDbYedek_RetentionTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            var mockHistory = new Mock<IBackupHistoryManager>();
            _service = new RetentionCleanupService(mockHistory.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [TestMethod]
        public async Task CleanupAsync_KeepLastN_DeletesOldFiles()
        {
            // Arrange — 5 dosya oluştur, KeepLastN=3
            for (int i = 0; i < 5; i++)
            {
                string file = CreateBackupFile($"TestDB_Full_2025010{i + 1}_020000.bak");
                File.SetCreationTime(file, DateTime.Now.AddDays(-10 + i));
            }

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 3
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — 3 dosya kalmalı
            var remaining = Directory.GetFiles(_testDir, "TestDB_Full_*.bak");
            remaining.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task CleanupAsync_DeleteOlderThanDays_DeletesExpired()
        {
            // Arrange — eski ve yeni dosyalar
            string oldFile = CreateBackupFile("TestDB_Full_20240101_020000.bak");
            File.SetCreationTime(oldFile, DateTime.Now.AddDays(-100));

            string recentFile = CreateBackupFile("TestDB_Full_20250101_020000.bak");
            File.SetCreationTime(recentFile, DateTime.Now.AddDays(-5));

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.DeleteOlderThanDays,
                    DeleteOlderThanDays = 30
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — eski dosya silinmeli, yeni dosya kalmalı
            File.Exists(oldFile).Should().BeFalse();
            File.Exists(recentFile).Should().BeTrue();
        }

        [TestMethod]
        public async Task CleanupAsync_BothPolicy_AppliesBothRules()
        {
            // Arrange — 6 dosya, KeepLastN=4, DeleteOlderThanDays=20
            for (int i = 0; i < 6; i++)
            {
                string file = CreateBackupFile($"TestDB_Full_2025010{i + 1}_020000.bak");
                File.SetCreationTime(file, DateTime.Now.AddDays(-30 + (i * 5)));
            }

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.Both,
                    KeepLastN = 4,
                    DeleteOlderThanDays = 20
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — hem count hem age bazlı temizlik yapılmış olmalı
            var remaining = Directory.GetFiles(_testDir, "TestDB_Full_*.bak");
            remaining.Length.Should().BeLessOrEqualTo(4);
        }

        [TestMethod]
        public async Task CleanupAsync_NullRetention_DoesNothing()
        {
            // Arrange
            CreateBackupFile("TestDB_Full_20250101_020000.bak");

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = null
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — dosya hâlâ orada
            Directory.GetFiles(_testDir, "*.bak").Should().HaveCount(1);
        }

        [TestMethod]
        public async Task CleanupAsync_EmptyLocalPath_DoesNothing()
        {
            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = null,
                Retention = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 1 }
            };

            // Act — exception fırlatmamalı
            Func<Task> act = () => _service.CleanupAsync(plan, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task CleanupAsync_Also_Handles_7zFiles()
        {
            // Arrange — .bak ve .7z dosyaları birlikte
            string bakFile = CreateBackupFile("TestDB_Full_20240101_020000.bak");
            File.SetCreationTime(bakFile, DateTime.Now.AddDays(-100));

            string archiveFile = CreateBackupFile("TestDB_Full_20240101_020000.7z");
            File.SetCreationTime(archiveFile, DateTime.Now.AddDays(-100));

            string recentBak = CreateBackupFile("TestDB_Full_20250101_020000.bak");
            File.SetCreationTime(recentBak, DateTime.Now);

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.DeleteOlderThanDays,
                    DeleteOlderThanDays = 30
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — eski .bak ve .7z silinmeli
            File.Exists(bakFile).Should().BeFalse();
            File.Exists(archiveFile).Should().BeFalse();
            File.Exists(recentBak).Should().BeTrue();
        }

        [TestMethod]
        public async Task CleanupAsync_Cancellation_Throws()
        {
            // Arrange
            CreateBackupFile("TestDB_Full_20250101_020000.bak");
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 0
                }
            };

            // Act & Assert
            Func<Task> act = () => _service.CleanupAsync(plan, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task CleanupAsync_MultipleDBs_CleansEachSeparately()
        {
            // Arrange — 2 DB, her biri 3 dosya
            for (int i = 0; i < 3; i++)
            {
                string fileA = CreateBackupFile($"DB_A_Full_2025010{i + 1}_020000.bak");
                File.SetCreationTime(fileA, DateTime.Now.AddDays(-5 + i));

                string fileB = CreateBackupFile($"DB_B_Full_2025010{i + 1}_020000.bak");
                File.SetCreationTime(fileB, DateTime.Now.AddDays(-5 + i));
            }

            var plan = new BackupPlan
            {
                Databases = { "DB_A", "DB_B" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 2
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — her DB'den 2 dosya kalmalı
            Directory.GetFiles(_testDir, "DB_A_*.bak").Should().HaveCount(2);
            Directory.GetFiles(_testDir, "DB_B_*.bak").Should().HaveCount(2);
        }

        private string CreateBackupFile(string fileName)
        {
            string filePath = Path.Combine(_testDir, fileName);
            File.WriteAllText(filePath, "fake");
            return filePath;
        }
    }
}
