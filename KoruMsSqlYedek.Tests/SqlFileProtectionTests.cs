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
using KoruMsSqlYedek.Engine.FileBackup;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// Saha hatası (v0.99.98): dosya yedeği VSS snapshot'tan kopyaladıktan sonra SHA-256
    /// doğrulamasını ORİJİNAL dosya üzerinden yapıyordu. AUTO_CLOSE ile kapanmış Mikro
    /// veritabanının .ldf/._MS dosyasını tutması SQL Server'ın yeniden açışını OS error 32
    /// ile düşürdü ve veritabanı günlerce RECOVERY_PENDING'de kaldı.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class SqlFileProtectionTests
    {
        private string _testDir;
        private string _sourceDir;
        private string _snapshotDir;
        private string _destDir;
        private Mock<IVssService> _mockVss;
        private Mock<ISqlBackupService> _mockSql;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedek_SqlFileTests", Guid.NewGuid().ToString());
            _sourceDir = Path.Combine(_testDir, "Source");
            _snapshotDir = Path.Combine(_testDir, "Snapshot");
            _destDir = Path.Combine(_testDir, "Dest");
            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_snapshotDir);
            Directory.CreateDirectory(_destDir);

            _mockVss = new Mock<IVssService>();
            _mockSql = new Mock<ISqlBackupService>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, recursive: true); } catch { }
        }

        #region Doğrulama snapshot'tan

        [TestMethod]
        public async Task Verify_UsesSnapshotPath_NotOriginal()
        {
            // Snapshot alındıktan sonra orijinal değişmiş (canlı dosya)
            string original = CreateFile(_sourceDir, "outlook.pst", "ORIGINAL-CHANGED-AFTER-SNAPSHOT");
            string snapshot = CreateFile(_snapshotDir, "outlook.pst", "SNAPSHOT");
            SetupSnapshot(original, snapshot);

            var service = new FileBackupService(_mockVss.Object);
            var result = await service.BackupSourceAsync(
                CreateSource(useVss: true), _destDir, null, CancellationToken.None, verifyAfterCopy: true);

            result.FilesCopied.Should().Be(1);
            result.FilesVerified.Should().Be(1, "doğrulama kopyanın okunduğu snapshot ile yapılmalı");
            result.FilesVerificationFailed.Should().Be(0);
            File.ReadAllText(Path.Combine(result.DestinationPath, "outlook.pst")).Should().Be("SNAPSHOT");
        }

        [TestMethod]
        public async Task SqlFile_WithSnapshot_CopiedWhileOriginalExclusivelyLocked()
        {
            // SQL Server dosyayı paylaşımsız tutuyor; kopya ve doğrulama yalnızca snapshot'tan
            // Boyutlar farklı: doğrulama orijinali okusaydı boyut uyuşmazlığı verirdi
            string original = CreateFile(_sourceDir, "MikroDesktop_PRIMA2018_log.ldf", "LIVE-GREW-AFTER-SNAPSHOT");
            string snapshot = CreateFile(_snapshotDir, "MikroDesktop_PRIMA2018_log.ldf", "SNAP");
            SetupSnapshot(original, snapshot);

            var service = new FileBackupService(_mockVss.Object);
            using (new FileStream(original, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await service.BackupSourceAsync(
                    CreateSource(useVss: true), _destDir, null, CancellationToken.None, verifyAfterCopy: true);

                result.Status.Should().Be(BackupResultStatus.Success);
                result.FilesCopied.Should().Be(1);
                result.FilesVerified.Should().Be(1);
            }
        }

        #endregion

        #region Snapshot yoksa SQL dosyası açılmaz

        [TestMethod]
        public async Task SqlFile_NoVss_IsNotCopiedDirectly_OtherFilesAre()
        {
            CreateFile(_sourceDir, "MikroDB.mdf", "DATA");
            CreateFile(_sourceDir, "MikroDB_log.LDF", "LOG");
            CreateFile(_sourceDir, "rapor.txt", "TEXT");
            _mockVss.Setup(v => v.IsAvailable()).Returns(false);

            var service = new FileBackupService(_mockVss.Object);
            var result = await service.BackupSourceAsync(
                CreateSource(useVss: true), _destDir, null, CancellationToken.None, verifyAfterCopy: true);

            result.FilesCopied.Should().Be(1);
            result.Status.Should().Be(BackupResultStatus.PartialSuccess);
            result.FailedFiles.Select(f => Path.GetFileName(f.FilePath))
                .Should().BeEquivalentTo(new[] { "MikroDB.mdf", "MikroDB_log.LDF" });
            result.FailedFiles.Should().OnlyContain(f => f.ErrorMessage == FileBackupService.SqlFileSkippedMessage);
            File.Exists(Path.Combine(result.DestinationPath, "MikroDB.mdf")).Should().BeFalse();
            File.Exists(Path.Combine(result.DestinationPath, "rapor.txt")).Should().BeTrue();
        }

        [TestMethod]
        public async Task SqlFile_SnapshotCopyFails_NoDirectFallback()
        {
            string original = CreateFile(_sourceDir, "MikroDB.mdf", "DATA");
            var snapshotId = Guid.NewGuid();
            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, It.IsAny<string>()))
                .Returns(Path.Combine(_snapshotDir, "yok", "MikroDB.mdf")); // snapshot'ta okunamıyor

            var service = new FileBackupService(_mockVss.Object);
            var result = await service.BackupSourceAsync(
                CreateSource(useVss: true), _destDir, null, CancellationToken.None);

            result.FilesCopied.Should().Be(0);
            result.FailedFiles.Should().ContainSingle(f => f.FilePath == original);
            File.Exists(Path.Combine(result.DestinationPath, "MikroDB.mdf")).Should().BeFalse();
        }

        [TestMethod]
        public async Task NonSqlFile_SnapshotCopyFails_FallsBackToDirect()
        {
            CreateFile(_sourceDir, "belge.docx", "DOC");
            var snapshotId = Guid.NewGuid();
            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, It.IsAny<string>()))
                .Returns(Path.Combine(_snapshotDir, "yok", "belge.docx"));

            var service = new FileBackupService(_mockVss.Object);
            var result = await service.BackupSourceAsync(
                CreateSource(useVss: true), _destDir, null, CancellationToken.None, verifyAfterCopy: true);

            result.Status.Should().Be(BackupResultStatus.Success);
            result.FilesCopied.Should().Be(1);
            result.FilesVerified.Should().Be(1);
        }

        [TestMethod]
        public async Task NonSqlFile_AllCopiesFail_StaleDestinationRemoved()
        {
            string original = CreateFile(_sourceDir, "kilitli.pst", "LIVE");
            var snapshotId = Guid.NewGuid();
            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, It.IsAny<string>()))
                .Returns(Path.Combine(_snapshotDir, "yok", "kilitli.pst"));

            // Önceki çalıştırmadan kalmış eski hedef dosya
            string staleDest = Path.Combine(_destDir, "Kaynak", "kilitli.pst");
            Directory.CreateDirectory(Path.GetDirectoryName(staleDest));
            File.WriteAllText(staleDest, "ESKI");

            var service = new FileBackupService(_mockVss.Object);
            using (new FileStream(original, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = await service.BackupSourceAsync(
                    CreateSource(useVss: true), _destDir, null, CancellationToken.None);
                result.FilesCopied.Should().Be(0);
            }

            File.Exists(staleDest).Should().BeFalse("kopyalanamayan dosyanın eski/yarım hedefi arşive girmemeli");
        }

        #endregion

        #region sys.master_files + AUTO_CLOSE

        [TestMethod]
        public async Task SqlFile_UnknownExtension_ProtectedViaMasterFiles_AndAutoCloseDisabled()
        {
            // Standart dışı uzantılı veri dosyası — yalnızca sys.master_files'tan tanınır
            string msFile = CreateFile(_sourceDir, "MikroDesktop_PRIMA2022.dat", "DATA");
            CreateFile(_sourceDir, "ayar.ini", "INI");
            _mockVss.Setup(v => v.IsAvailable()).Returns(false);

            _mockSql.Setup(s => s.GetDatabaseFilesAsync(It.IsAny<SqlConnectionInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SqlDatabaseFileInfo>
                {
                    new() { DatabaseName = "MikroDesktop_PRIMA2022", PhysicalPath = msFile, IsAutoCloseOn = true },
                    new() { DatabaseName = "BaskaDB", PhysicalPath = @"Z:\Baska\BaskaDB.mdf", IsAutoCloseOn = true },
                    new() { DatabaseName = "KapaliDegil", PhysicalPath = Path.Combine(_sourceDir, "x.mdf"), IsAutoCloseOn = false }
                });
            _mockSql.Setup(s => s.DisableAutoCloseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "MikroDesktop_PRIMA2022" });

            var plan = CreatePlan(disableAutoClose: true);
            var service = new FileBackupService(_mockVss.Object, _mockSql.Object);
            var results = await service.BackupFilesAsync(plan, null, CancellationToken.None);

            results.Should().ContainSingle();
            results[0].FilesCopied.Should().Be(1);
            results[0].FailedFiles.Should().ContainSingle(f => f.FilePath == msFile);

            // Yalnızca kaynak altında kalan ve AUTO_CLOSE'u açık olan veritabanı kapatılır
            _mockSql.Verify(s => s.DisableAutoCloseAsync(
                    It.IsAny<SqlConnectionInfo>(),
                    It.Is<IEnumerable<string>>(n => n.SequenceEqual(new[] { "MikroDesktop_PRIMA2022" })),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task AutoCloseOptionOff_DoesNotDisableAutoClose()
        {
            string msFile = CreateFile(_sourceDir, "Db._MS", "DATA");
            _mockVss.Setup(v => v.IsAvailable()).Returns(false);
            _mockSql.Setup(s => s.GetDatabaseFilesAsync(It.IsAny<SqlConnectionInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SqlDatabaseFileInfo>
                {
                    new() { DatabaseName = "Db", PhysicalPath = msFile, IsAutoCloseOn = true }
                });

            var service = new FileBackupService(_mockVss.Object, _mockSql.Object);
            await service.BackupFilesAsync(CreatePlan(disableAutoClose: false), null, CancellationToken.None);

            _mockSql.Verify(s => s.DisableAutoCloseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task MasterFilesQueryFails_StillProtectsByExtension()
        {
            CreateFile(_sourceDir, "Db.mdf", "DATA");
            CreateFile(_sourceDir, "MikroDesktop_PRIMA2022._MS", "DATA");
            _mockVss.Setup(v => v.IsAvailable()).Returns(false);
            _mockSql.Setup(s => s.GetDatabaseFilesAsync(It.IsAny<SqlConnectionInfo>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("bağlantı yok"));

            var service = new FileBackupService(_mockVss.Object, _mockSql.Object);
            var results = await service.BackupFilesAsync(CreatePlan(disableAutoClose: true), null, CancellationToken.None);

            results[0].FilesCopied.Should().Be(0);
            results[0].FailedFiles.Should().HaveCount(2);
        }

        [TestMethod]
        public void IsUnderAnyRoot_MatchesFolderBoundaryOnly()
        {
            var roots = new[] { @"D:\Mikro\v17xx" };
            FileBackupService.IsUnderAnyRoot(@"D:\Mikro\v17xx\PRIMA2018\DATA\a.ldf", roots).Should().BeTrue();
            FileBackupService.IsUnderAnyRoot(@"d:\mikro\V17XX\a.mdf", roots).Should().BeTrue();
            FileBackupService.IsUnderAnyRoot(@"D:\Mikro\v17xx2\a.mdf", roots).Should().BeFalse();
            FileBackupService.IsUnderAnyRoot(@"C:\Mikro\v17xx\a.mdf", roots).Should().BeFalse();
        }

        [TestMethod]
        public void IsSqlDatabaseFile_ExtensionOrList()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"D:\Data\X.dat" };
            FileBackupService.IsSqlDatabaseFile(@"C:\a\b.MDF", null).Should().BeTrue();
            FileBackupService.IsSqlDatabaseFile(@"C:\a\b.ndf", null).Should().BeTrue();
            FileBackupService.IsSqlDatabaseFile(@"C:\a\b_log.ldf", null).Should().BeTrue();
            FileBackupService.IsSqlDatabaseFile(@"C:\a\MikroDesktop_PRIMA2022._MS", null).Should().BeTrue();
            FileBackupService.IsSqlDatabaseFile(@"d:\data\x.DAT", list).Should().BeTrue();
            FileBackupService.IsSqlDatabaseFile(@"D:\Data\Y.dat", list).Should().BeFalse();
            FileBackupService.IsSqlDatabaseFile(@"C:\a\b.pst", list).Should().BeFalse();
        }

        #endregion

        #region Helpers

        private static string CreateFile(string dir, string name, string content)
        {
            string path = Path.Combine(dir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private void SetupSnapshot(string original, string snapshot)
        {
            var snapshotId = Guid.NewGuid();
            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, original)).Returns(snapshot);
        }

        private FileBackupSource CreateSource(bool useVss) => new()
        {
            SourceName = "Kaynak",
            SourcePath = _sourceDir,
            Recursive = true,
            UseVss = useVss,
            IsEnabled = true
        };

        private BackupPlan CreatePlan(bool disableAutoClose) => new()
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanName = "Test",
            LocalPath = _destDir,
            DisableAutoClose = disableAutoClose,
            SqlConnection = new SqlConnectionInfo { Server = @".\MIKRO" },
            FileBackup = new FileBackupConfig
            {
                IsEnabled = true,
                Sources = new List<FileBackupSource> { CreateSource(useVss: true) }
            }
        };

        #endregion
    }
}
