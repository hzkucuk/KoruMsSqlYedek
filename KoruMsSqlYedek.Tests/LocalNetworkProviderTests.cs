using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Cloud;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class LocalNetworkProviderTests
    {
        private string _testDir;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedekTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); }
                catch { /* test temizliği — yok sayılır */ }
            }
        }

        #region Constructor & Properties

        [TestMethod]
        public void Constructor_LocalPathType_DisplayNameIsYerelDizin()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            provider.DisplayName.Should().Be("Yerel Dizin");
            provider.ProviderType.Should().Be(CloudProviderType.LocalPath);
        }

        [TestMethod]
        public void Constructor_UncPathType_DisplayNameIsAgPaylasimiUNC()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.UncPath);
            provider.DisplayName.Should().Be("Ağ Paylaşımı (UNC)");
            provider.ProviderType.Should().Be(CloudProviderType.UncPath);
        }

        #endregion

        #region UploadAsync

        [TestMethod]
        public async Task UploadAsync_ValidLocalPath_CopiesFileSuccessfully()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string sourceFile = CreateTestFile("test_upload.dat", 1024);
            string destDir = Path.Combine(_testDir, "dest");
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = destDir
            };

            // Act
            var result = await provider.UploadAsync(
                sourceFile, "test_upload.dat", config,
                new Progress<int>(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.RemoteFilePath.Should().Be(Path.Combine(destDir, "test_upload.dat"));
            result.UploadedAt.Should().NotBeNull();
            File.Exists(result.RemoteFilePath).Should().BeTrue();

            // Boyut doğrulama
            new FileInfo(result.RemoteFilePath).Length.Should().Be(1024);
        }

        [TestMethod]
        public async Task UploadAsync_LargeFile_ReportsProgressCorrectly()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            int fileSizeBytes = 500_000; // ~500 KB — birden fazla buffer okuma gerektirir
            string sourceFile = CreateTestFile("progress_test.dat", fileSizeBytes);
            string destDir = Path.Combine(_testDir, "progress_dest");
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = destDir
            };

            int maxProgressReported = 0;
            var progress = new Progress<int>(p =>
            {
                if (p > maxProgressReported)
                    maxProgressReported = p;
            });

            // Act
            var result = await provider.UploadAsync(
                sourceFile, "progress_test.dat", config,
                progress, CancellationToken.None);

            // Biraz bekle — Progress<T> ThreadPool'da çalışır
            await Task.Delay(100);

            // Assert
            result.IsSuccess.Should().BeTrue();
            maxProgressReported.Should().Be(100, "kopyalama tamamlandığında %100 raporlanmalı");
        }

        [TestMethod]
        public async Task UploadAsync_OverwriteExistingFile_Succeeds()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string sourceFile = CreateTestFile("overwrite_test.dat", 512);
            string destDir = Path.Combine(_testDir, "overwrite_dest");
            Directory.CreateDirectory(destDir);
            File.WriteAllText(Path.Combine(destDir, "overwrite_test.dat"), "eski içerik");

            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = destDir
            };

            // Act
            var result = await provider.UploadAsync(
                sourceFile, "overwrite_test.dat", config,
                new Progress<int>(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            new FileInfo(result.RemoteFilePath).Length.Should().Be(512);
        }

        [TestMethod]
        public async Task UploadAsync_MissingSourceFile_ReturnsFailure()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = Path.Combine(_testDir, "dest_missing")
            };

            // Act
            var result = await provider.UploadAsync(
                @"C:\nonexistent\file.bak", "file.bak", config,
                new Progress<int>(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task UploadAsync_NullConfig_ReturnsFailure()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);

            var result = await provider.UploadAsync(
                "somefile.bak", "file.bak", null,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task UploadAsync_EmptyLocalOrUncPath_ReturnsFailure()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = "" // boş yol
            };

            var result = await provider.UploadAsync(
                "somefile.bak", "file.bak", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("LocalOrUncPath");
        }

        [TestMethod]
        public async Task UploadAsync_CancellationRequested_ReturnsCancelled()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string sourceFile = CreateTestFile("cancel_test.dat", 1024);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = Path.Combine(_testDir, "cancel_dest")
            };

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Hemen iptal et

            // Act
            var result = await provider.UploadAsync(
                sourceFile, "cancel_test.dat", config,
                new Progress<int>(), cts.Token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("iptal");
        }

        [TestMethod]
        public async Task UploadAsync_CreatesDestinationDirectory()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string sourceFile = CreateTestFile("dir_create.dat", 256);
            string deepDir = Path.Combine(_testDir, "a", "b", "c");
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = deepDir
            };

            // Act
            var result = await provider.UploadAsync(
                sourceFile, "dir_create.dat", config,
                new Progress<int>(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            Directory.Exists(deepDir).Should().BeTrue();
        }

        #endregion

        #region DeleteAsync

        [TestMethod]
        public async Task DeleteAsync_ExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string filePath = Path.Combine(_testDir, "to_delete.dat");
            File.WriteAllBytes(filePath, new byte[128]);
            var config = new CloudTargetConfig { Type = CloudProviderType.LocalPath };

            // Act
            bool deleted = await provider.DeleteAsync(filePath, config, CancellationToken.None);

            // Assert
            deleted.Should().BeTrue();
            File.Exists(filePath).Should().BeFalse();
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentFile_ReturnsTrue()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string filePath = Path.Combine(_testDir, "nonexistent.dat");
            var config = new CloudTargetConfig { Type = CloudProviderType.LocalPath };

            bool deleted = await provider.DeleteAsync(filePath, config, CancellationToken.None);

            deleted.Should().BeTrue("var olmayan dosya silme hatası değildir");
        }

        #endregion

        #region TestConnectionAsync

        [TestMethod]
        public async Task TestConnectionAsync_ExistingDirectory_ReturnsTrue()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = _testDir
            };

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestConnectionAsync_NewDirectory_CreatesAndReturnsTrue()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            string newDir = Path.Combine(_testDir, "new_test_dir");
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = newDir
            };

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeTrue();
            Directory.Exists(newDir).Should().BeTrue();
        }

        [TestMethod]
        public async Task TestConnectionAsync_EmptyPath_ReturnsFalse()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                LocalOrUncPath = ""
            };

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_NullConfig_ReturnsFalse()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);

            bool connected = await provider.TestConnectionAsync(null, CancellationToken.None);

            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_InvalidPath_ReturnsFalse()
        {
            var provider = new LocalNetworkProvider(CloudProviderType.LocalPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.LocalPath,
                // Geçersiz karakterli yol — UNC yerine yerel geçersiz yol (timeout önlenir)
                LocalOrUncPath = @"Z:\nonexistent_drive_xyz_" + Guid.NewGuid().ToString("N")
            };

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeFalse();
        }

        #endregion

        #region UNC Specific

        [TestMethod]
        public async Task UploadAsync_UncWithoutCredentials_WorksLikeLocal()
        {
            // UNC provider ama credential yok — normal yerel kopyalama yapmalı
            var provider = new LocalNetworkProvider(CloudProviderType.UncPath);
            string sourceFile = CreateTestFile("unc_local.dat", 256);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.UncPath,
                LocalOrUncPath = Path.Combine(_testDir, "unc_dest"),
                Username = null,
                Password = null
            };

            var result = await provider.UploadAsync(
                sourceFile, "unc_local.dat", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.ProviderType.Should().Be(CloudProviderType.UncPath);
        }

        #endregion

        #region Helpers

        private string CreateTestFile(string fileName, int sizeBytes)
        {
            string filePath = Path.Combine(_testDir, "source", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            byte[] data = new byte[sizeBytes];
            new Random(42).NextBytes(data);
            File.WriteAllBytes(filePath, data);
            return filePath;
        }

        #endregion
    }
}
