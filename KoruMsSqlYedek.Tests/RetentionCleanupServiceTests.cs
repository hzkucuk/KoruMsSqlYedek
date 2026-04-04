using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Retention;

namespace KoruMsSqlYedek.Tests
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
            _testDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedek_RetentionTests", Guid.NewGuid().ToString());
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

        [TestMethod]
        public async Task CleanupAsync_FileBackupArchives_KeepLastN_DeletesOldArchives()
        {
            // Arrange — 5 dosya arşivi, KeepLastN=2
            for (int i = 0; i < 5; i++)
            {
                string file = CreateBackupFile($"Files_2025010{i + 1}_020000.7z");
                File.SetCreationTime(file, DateTime.Now.AddDays(-10 + i));
            }

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = new FileBackupConfig { IsEnabled = true },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 2
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — 2 dosya arşivi kalmalı
            var remaining = Directory.GetFiles(_testDir, "Files_*.7z");
            remaining.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task CleanupAsync_FileBackupArchives_DeleteOlderThanDays_DeletesExpired()
        {
            // Arrange
            string oldArchive = CreateBackupFile("Files_20240101_020000.7z");
            File.SetCreationTime(oldArchive, DateTime.Now.AddDays(-100));

            string recentArchive = CreateBackupFile("Files_20250601_020000.7z");
            File.SetCreationTime(recentArchive, DateTime.Now.AddDays(-5));

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = new FileBackupConfig { IsEnabled = true },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.DeleteOlderThanDays,
                    DeleteOlderThanDays = 30
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert
            File.Exists(oldArchive).Should().BeFalse("eski arşiv silinmeli");
            File.Exists(recentArchive).Should().BeTrue("yeni arşiv kalmalı");
        }

        [TestMethod]
        public async Task CleanupAsync_FileBackupDisabled_DoesNotCleanArchives()
        {
            // Arrange — arşivler var ama FileBackup devre dışı
            string archive = CreateBackupFile("Files_20240101_020000.7z");
            File.SetCreationTime(archive, DateTime.Now.AddDays(-100));

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = new FileBackupConfig { IsEnabled = false },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.DeleteOlderThanDays,
                    DeleteOlderThanDays = 30
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — dosya arşivi silinmemeli (FileBackup devre dışı)
            File.Exists(archive).Should().BeTrue();
        }

        [TestMethod]
        public async Task CleanupAsync_FileBackupNull_DoesNotCleanArchives()
        {
            // Arrange — FileBackup null
            string archive = CreateBackupFile("Files_20240101_020000.7z");
            File.SetCreationTime(archive, DateTime.Now.AddDays(-100));

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = null,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.DeleteOlderThanDays,
                    DeleteOlderThanDays = 30
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert
            File.Exists(archive).Should().BeTrue();
        }

        [TestMethod]
        public async Task CleanupAsync_MixedDbAndFileArchives_CleansBothIndependently()
        {
            // Arrange — hem DB dosyaları hem dosya arşivleri
            for (int i = 0; i < 4; i++)
            {
                string dbFile = CreateBackupFile($"TestDB_Full_2025010{i + 1}_020000.bak");
                File.SetCreationTime(dbFile, DateTime.Now.AddDays(-10 + i));

                string archiveFile = CreateBackupFile($"Files_2025010{i + 1}_020000.7z");
                File.SetCreationTime(archiveFile, DateTime.Now.AddDays(-10 + i));
            }

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = new FileBackupConfig { IsEnabled = true },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 2
                }
            };

            // Act
            await _service.CleanupAsync(plan, CancellationToken.None);

            // Assert — her ikisinden de 2'şer dosya kalmalı
            Directory.GetFiles(_testDir, "TestDB_*.bak").Should().HaveCount(2);
            Directory.GetFiles(_testDir, "Files_*.7z").Should().HaveCount(2);
        }

        [TestMethod]
        public async Task CleanupAsync_FileBackupArchives_GfsPolicy_ProtectsCorrectFiles()
        {
            // Arrange
            var mockHistory = new Mock<IBackupHistoryManager>();
            var service = new RetentionCleanupService(mockHistory.Object);

            string today = Path.Combine(_testDir, "Files_today.7z");
            File.WriteAllBytes(today, new byte[500]);
            File.SetCreationTime(today, DateTime.Now);

            string yesterday = Path.Combine(_testDir, "Files_yesterday.7z");
            File.WriteAllBytes(yesterday, new byte[400]);
            File.SetCreationTime(yesterday, DateTime.Now.AddDays(-1));

            string old = Path.Combine(_testDir, "Files_old.7z");
            File.WriteAllBytes(old, new byte[300]);
            File.SetCreationTime(old, DateTime.Now.AddDays(-30));

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                FileBackup = new FileBackupConfig { IsEnabled = true },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.GFS,
                    GfsKeepDaily = 2,
                    GfsKeepWeekly = 0,
                    GfsKeepMonthly = 0,
                    GfsKeepYearly = 0
                }
            };

            // Act
            await service.CleanupAsync(plan, CancellationToken.None);

            // Assert
            File.Exists(today).Should().BeTrue("bugünkü arşiv korunmalı");
            File.Exists(yesterday).Should().BeTrue("dünkü arşiv korunmalı");
            File.Exists(old).Should().BeFalse("30 gün önceki arşiv silinmeli");
        }

        private string CreateBackupFile(string fileName)
        {
            string filePath = Path.Combine(_testDir, fileName);
            File.WriteAllText(filePath, "fake");
            return filePath;
        }

        private string CreateBackupFileWithSize(string fileName, int sizeBytes)
        {
            string filePath = Path.Combine(_testDir, fileName);
            File.WriteAllBytes(filePath, new byte[sizeBytes]);
            return filePath;
        }
    }

    /// <summary>
    /// GFS (Grandfather-Father-Son) retention politikası unit testleri.
    /// BuildGfsProtectedSet internal static metodu üzerinden test edilir.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class GfsRetentionTests
    {
        private string _testDir;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedek_GfsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_Daily_ProtectsOneBestPerDay()
        {
            // Arrange — 3 gün, her gün 2 dosya (büyük ve küçük)
            var files = new List<FileInfo>();
            for (int d = 0; d < 3; d++)
            {
                DateTime day = DateTime.Now.Date.AddDays(-d);
                string smallFile = CreateFileWithDate($"DB_Full_small_{d}.bak", day, 100);
                string bigFile = CreateFileWithDate($"DB_Full_big_{d}.bak", day.AddHours(1), 500);
                files.Add(new FileInfo(smallFile));
                files.Add(new FileInfo(bigFile));
            }

            files = files.OrderByDescending(f => f.CreationTime).ToList();
            var policy = new RetentionPolicy { GfsKeepDaily = 3 };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — her günden büyük dosya korunmalı (3 dosya)
            protectedSet.Should().HaveCount(3);
            foreach (var file in files.Where(f => f.Name.Contains("big")))
            {
                protectedSet.Should().Contain(file.FullName);
            }
        }

        [TestMethod]
        public void BuildGfsProtectedSet_Weekly_ProtectsOneBestPerWeek()
        {
            // Arrange — 3 hafta, her haftadan 1 dosya
            var files = new List<FileInfo>();
            for (int w = 0; w < 3; w++)
            {
                DateTime weekDay = DateTime.Now.Date.AddDays(-w * 7);
                string file = CreateFileWithDate($"DB_Full_w{w}.bak", weekDay, 200);
                files.Add(new FileInfo(file));
            }

            files = files.OrderByDescending(f => f.CreationTime).ToList();
            var policy = new RetentionPolicy { GfsKeepWeekly = 2, GfsKeepDaily = 0, GfsKeepMonthly = 0, GfsKeepYearly = 0 };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — 2 hafta korunmalı
            protectedSet.Should().HaveCount(2);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_Monthly_ProtectsOneBestPerMonth()
        {
            // Arrange — 4 ay, her aydan 1 dosya
            var files = new List<FileInfo>();
            for (int m = 0; m < 4; m++)
            {
                DateTime monthDay = DateTime.Now.Date.AddMonths(-m);
                string file = CreateFileWithDate($"DB_Full_m{m}.bak", monthDay, 300);
                files.Add(new FileInfo(file));
            }

            files = files.OrderByDescending(f => f.CreationTime).ToList();
            var policy = new RetentionPolicy { GfsKeepMonthly = 3, GfsKeepDaily = 0, GfsKeepWeekly = 0, GfsKeepYearly = 0 };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — 3 ay korunmalı
            protectedSet.Should().HaveCount(3);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_Yearly_ProtectsOneBestPerYear()
        {
            // Arrange — 3 yıl, her yıldan 1 dosya
            var files = new List<FileInfo>();
            for (int y = 0; y < 3; y++)
            {
                DateTime yearDay = new DateTime(DateTime.Now.Year - y, 6, 15);
                string file = CreateFileWithDate($"DB_Full_y{y}.bak", yearDay, 400);
                files.Add(new FileInfo(file));
            }

            files = files.OrderByDescending(f => f.CreationTime).ToList();
            var policy = new RetentionPolicy { GfsKeepYearly = 2, GfsKeepDaily = 0, GfsKeepWeekly = 0, GfsKeepMonthly = 0 };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — 2 yıl korunmalı
            protectedSet.Should().HaveCount(2);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_CombinedGfs_UnionOfAllPeriods()
        {
            // Arrange — çoklu dosya: günlük, haftalık ve aylık koruma çakışabilir
            var files = new List<FileInfo>();

            // Son 7 gün — her gün 1 dosya
            for (int d = 0; d < 7; d++)
            {
                DateTime day = DateTime.Now.Date.AddDays(-d);
                string file = CreateFileWithDate($"DB_Full_d{d}.bak", day, 200);
                files.Add(new FileInfo(file));
            }

            // 30 gün önce — aylık yedek
            string monthAgo = CreateFileWithDate("DB_Full_month.bak", DateTime.Now.Date.AddDays(-30), 500);
            files.Add(new FileInfo(monthAgo));

            files = files.OrderByDescending(f => f.CreationTime).ToList();
            var policy = new RetentionPolicy
            {
                GfsKeepDaily = 3,
                GfsKeepWeekly = 2,
                GfsKeepMonthly = 2,
                GfsKeepYearly = 0
            };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — en az günlük 3 + aylık dosya korunmalı (bazıları çakışabilir)
            protectedSet.Count.Should().BeGreaterOrEqualTo(3);
            protectedSet.Should().Contain(monthAgo);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_SelectsLargestFilePerPeriod()
        {
            // Arrange — aynı gün 3 dosya, farklı boyutlar
            DateTime today = DateTime.Now.Date;
            string small = CreateFileWithDate("DB_Full_s.bak", today, 100);
            string medium = CreateFileWithDate("DB_Full_m.bak", today.AddHours(1), 300);
            string large = CreateFileWithDate("DB_Full_l.bak", today.AddHours(2), 600);

            var files = new List<FileInfo> { new(small), new(medium), new(large) }
                .OrderByDescending(f => f.CreationTime).ToList();

            var policy = new RetentionPolicy { GfsKeepDaily = 1, GfsKeepWeekly = 0, GfsKeepMonthly = 0, GfsKeepYearly = 0 };

            // Act
            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            // Assert — en büyük dosya seçilmeli
            protectedSet.Should().HaveCount(1);
            protectedSet.Should().Contain(large);
        }

        [TestMethod]
        public void BuildGfsProtectedSet_EmptyFileList_ReturnsEmpty()
        {
            var policy = new RetentionPolicy { GfsKeepDaily = 7, GfsKeepWeekly = 4 };

            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(new List<FileInfo>(), policy);

            protectedSet.Should().BeEmpty();
        }

        [TestMethod]
        public void BuildGfsProtectedSet_AllZeroPeriods_ReturnsEmpty()
        {
            DateTime today = DateTime.Now.Date;
            string file = CreateFileWithDate("DB_Full.bak", today, 100);
            var files = new List<FileInfo> { new(file) };

            var policy = new RetentionPolicy { GfsKeepDaily = 0, GfsKeepWeekly = 0, GfsKeepMonthly = 0, GfsKeepYearly = 0 };

            var protectedSet = RetentionCleanupService.BuildGfsProtectedSet(files, policy);

            protectedSet.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CleanupAsync_GfsPolicy_DeletesUnprotectedFiles()
        {
            // Arrange — 5 dosya: bugün, dün, 10 gün önce, 40 gün önce, 100 gün önce
            var mockHistory = new Mock<IBackupHistoryManager>();
            var service = new RetentionCleanupService(mockHistory.Object);

            string today = CreateFileWithDate("TestDB_Full_today.bak", DateTime.Now, 500);
            string yesterday = CreateFileWithDate("TestDB_Full_yesterday.bak", DateTime.Now.AddDays(-1), 400);
            string tenDays = CreateFileWithDate("TestDB_Full_10d.bak", DateTime.Now.AddDays(-10), 300);
            string fortyDays = CreateFileWithDate("TestDB_Full_40d.bak", DateTime.Now.AddDays(-40), 200);
            string hundredDays = CreateFileWithDate("TestDB_Full_100d.bak", DateTime.Now.AddDays(-100), 100);

            var plan = new BackupPlan
            {
                Databases = { "TestDB" },
                LocalPath = _testDir,
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.GFS,
                    GfsKeepDaily = 2,
                    GfsKeepWeekly = 0,
                    GfsKeepMonthly = 0,
                    GfsKeepYearly = 0
                }
            };

            // Act
            await service.CleanupAsync(plan, CancellationToken.None);

            // Assert — sadece son 2 günün yedekleri korunmalı, eski olanlar silinmeli
            File.Exists(today).Should().BeTrue("bugünkü dosya korunmalı");
            File.Exists(yesterday).Should().BeTrue("dünkü dosya korunmalı");
            File.Exists(tenDays).Should().BeFalse("10 gün önceki dosya silinmeli");
            File.Exists(fortyDays).Should().BeFalse("40 gün önceki dosya silinmeli");
            File.Exists(hundredDays).Should().BeFalse("100 gün önceki dosya silinmeli");
        }

        private string CreateFileWithDate(string fileName, DateTime creationTime, int sizeBytes)
        {
            string filePath = Path.Combine(_testDir, fileName);
            File.WriteAllBytes(filePath, new byte[sizeBytes]);
            File.SetCreationTime(filePath, creationTime);
            return filePath;
        }
    }
}
