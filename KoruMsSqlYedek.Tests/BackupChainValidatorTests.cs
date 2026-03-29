using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Engine.Backup;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class BackupChainValidatorTests
    {
        private string _testDir;
        private BackupChainValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedek_ChainTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _validator = new BackupChainValidator();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [TestMethod]
        public void HasValidFullBackup_NoFiles_ReturnsFalse()
        {
            _validator.HasValidFullBackup(_testDir, "TestDB").Should().BeFalse();
        }

        [TestMethod]
        public void HasValidFullBackup_NullPath_ReturnsFalse()
        {
            _validator.HasValidFullBackup(null, "TestDB").Should().BeFalse();
            _validator.HasValidFullBackup(string.Empty, "TestDB").Should().BeFalse();
        }

        [TestMethod]
        public void HasValidFullBackup_NonExistentPath_ReturnsFalse()
        {
            _validator.HasValidFullBackup(@"C:\NonExistent\Path", "TestDB").Should().BeFalse();
        }

        [TestMethod]
        public void HasValidFullBackup_WithFullBackup_ReturnsTrue()
        {
            // Arrange
            CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            // Act & Assert
            _validator.HasValidFullBackup(_testDir, "TestDB").Should().BeTrue();
        }

        [TestMethod]
        public void HasValidFullBackup_OnlyDiffBackup_ReturnsFalse()
        {
            // Arrange — sadece Differential var, Full yok
            CreateFakeBackupFile("TestDB_Differential_20250102_030000.bak");

            // Act & Assert
            _validator.HasValidFullBackup(_testDir, "TestDB").Should().BeFalse();
        }

        [TestMethod]
        public void ShouldPromoteToFull_NoDiffs_ReturnsFalse()
        {
            // Arrange
            CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            // Act & Assert
            _validator.ShouldPromoteToFull(_testDir, "TestDB", 7).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldPromoteToFull_NoFullBackup_ReturnsTrue()
        {
            _validator.ShouldPromoteToFull(_testDir, "TestDB", 7).Should().BeTrue();
        }

        [TestMethod]
        public void ShouldPromoteToFull_DiffCountExceedsLimit_ReturnsTrue()
        {
            // Arrange — Full + 8 Diff (limit=7)
            string fullFile = CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            // Full'den sonra oluşmuş gibi diff dosyaları
            for (int i = 1; i <= 8; i++)
            {
                string diffFile = CreateFakeBackupFile($"TestDB_Differential_202501{(i + 1):D2}_030000.bak");
                // CreationTime'ı Full'den sonra olacak şekilde ayarla
                File.SetCreationTime(diffFile, File.GetCreationTime(fullFile).AddDays(i));
            }

            // Act & Assert
            _validator.ShouldPromoteToFull(_testDir, "TestDB", 7).Should().BeTrue();
        }

        [TestMethod]
        public void ShouldPromoteToFull_DiffCountUnderLimit_ReturnsFalse()
        {
            // Arrange — Full + 3 Diff (limit=7)
            string fullFile = CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            for (int i = 1; i <= 3; i++)
            {
                string diffFile = CreateFakeBackupFile($"TestDB_Differential_202501{(i + 1):D2}_030000.bak");
                File.SetCreationTime(diffFile, File.GetCreationTime(fullFile).AddDays(i));
            }

            // Act & Assert
            _validator.ShouldPromoteToFull(_testDir, "TestDB", 7).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldPromoteToFull_AutoPromoteZero_ReturnsFalse()
        {
            // AutoPromoteAfter = 0 → devre dışı
            CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");
            _validator.ShouldPromoteToFull(_testDir, "TestDB", 0).Should().BeFalse();
        }

        [TestMethod]
        public void HasValidLogChain_WithFullBackup_ReturnsTrue()
        {
            CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");
            _validator.HasValidLogChain(_testDir, "TestDB").Should().BeTrue();
        }

        [TestMethod]
        public void HasValidLogChain_NoFullBackup_ReturnsFalse()
        {
            _validator.HasValidLogChain(_testDir, "TestDB").Should().BeFalse();
        }

        [TestMethod]
        public void GetDifferentialCountSinceLastFull_CountsCorrectly()
        {
            // Arrange
            string fullFile = CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            for (int i = 1; i <= 5; i++)
            {
                string diffFile = CreateFakeBackupFile($"TestDB_Differential_202501{(i + 1):D2}_030000.bak");
                File.SetCreationTime(diffFile, File.GetCreationTime(fullFile).AddDays(i));
            }

            // Act & Assert
            _validator.GetDifferentialCountSinceLastFull(_testDir, "TestDB").Should().Be(5);
        }

        [TestMethod]
        public void GetDifferentialCountSinceLastFull_NoFull_ReturnsZero()
        {
            _validator.GetDifferentialCountSinceLastFull(_testDir, "TestDB").Should().Be(0);
        }

        [TestMethod]
        public void GetIncrementalCountSinceLastFull_CountsCorrectly()
        {
            // Arrange
            string fullFile = CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");

            for (int i = 1; i <= 3; i++)
            {
                string logFile = CreateFakeBackupFile($"TestDB_Incremental_202501{(i + 1):D2}_040000.bak");
                File.SetCreationTime(logFile, File.GetCreationTime(fullFile).AddHours(i));
            }

            // Act & Assert
            _validator.GetIncrementalCountSinceLastFull(_testDir, "TestDB").Should().Be(3);
        }

        [TestMethod]
        public void GetLastFullBackupDate_ReturnsLatestDate()
        {
            // Arrange
            string full1 = CreateFakeBackupFile("TestDB_Full_20250101_020000.bak");
            File.SetCreationTime(full1, new DateTime(2025, 1, 1, 2, 0, 0));

            string full2 = CreateFakeBackupFile("TestDB_Full_20250108_020000.bak");
            File.SetCreationTime(full2, new DateTime(2025, 1, 8, 2, 0, 0));

            // Act
            var lastDate = _validator.GetLastFullBackupDate(_testDir, "TestDB");

            // Assert
            lastDate.Should().NotBeNull();
            lastDate.Value.Should().Be(new DateTime(2025, 1, 8, 2, 0, 0));
        }

        [TestMethod]
        public void GetLastFullBackupDate_NoBackups_ReturnsNull()
        {
            _validator.GetLastFullBackupDate(_testDir, "TestDB").Should().BeNull();
        }

        /// <summary>
        /// Boş bir dosya oluşturur (backup file simulator).
        /// </summary>
        private string CreateFakeBackupFile(string fileName)
        {
            string filePath = Path.Combine(_testDir, fileName);
            File.WriteAllText(filePath, "fake backup content");
            return filePath;
        }
    }
}
