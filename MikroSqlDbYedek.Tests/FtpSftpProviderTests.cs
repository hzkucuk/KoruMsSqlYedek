using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.Cloud;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class FtpSftpProviderTests
    {
        [TestMethod]
        public void Constructor_FtpType_DisplayNameIsFTP()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            provider.DisplayName.Should().Be("FTP");
            provider.ProviderType.Should().Be(CloudProviderType.Ftp);
        }

        [TestMethod]
        public void Constructor_FtpsType_DisplayNameIsFTPS()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftps);
            provider.DisplayName.Should().Be("FTPS");
        }

        [TestMethod]
        public void Constructor_SftpType_DisplayNameIsSFTP()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            provider.DisplayName.Should().Be("SFTP");
            provider.ProviderType.Should().Be(CloudProviderType.Sftp);
        }

        [TestMethod]
        public async Task UploadAsync_MissingHost_ReturnsFailure()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                Host = null, // boş host
                DisplayName = "Test FTP"
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Sunucu adresi");
        }

        [TestMethod]
        public async Task UploadAsync_MissingLocalFile_ReturnsFailure()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                Host = "ftp.test.com",
                DisplayName = "Test FTP"
            };

            var result = await provider.UploadAsync(
                @"C:\surely_nonexistent_file_12345.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("bulunamadı");
        }

        [TestMethod]
        public async Task UploadAsync_SftpMissingHost_ReturnsFailure()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Sftp,
                Host = "",
                DisplayName = "Test SFTP"
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Sunucu adresi");
        }

        [TestMethod]
        public async Task UploadAsync_FtpUnreachableHost_ReturnsFailure()
        {
            // Arrange — ulaşılamaz adres
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");

            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                Host = "192.0.2.1", // RFC 5737 — documentation address, unreachable
                Port = 21,
                Username = "test",
                Password = "test",
                RemoteFolderPath = "/backups",
                DisplayName = "Unreachable FTP"
            };

            try
            {
                // Act
                var result = await provider.UploadAsync(
                    tempFile, "test.bak", config, null, CancellationToken.None);

                // Assert — bağlantı hatası
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().NotBeNullOrEmpty();
                result.ProviderType.Should().Be(CloudProviderType.Ftp);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task UploadAsync_SftpUnreachableHost_ReturnsFailure()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");

            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Sftp,
                Host = "192.0.2.1",
                Port = 22,
                Username = "test",
                Password = "test",
                RemoteFolderPath = "/backups",
                DisplayName = "Unreachable SFTP"
            };

            try
            {
                var result = await provider.UploadAsync(
                    tempFile, "test.bak", config, null, CancellationToken.None);

                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().NotBeNullOrEmpty();
                result.ProviderType.Should().Be(CloudProviderType.Sftp);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public async Task TestConnectionAsync_FtpUnreachable_ReturnsFalse()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            var config = new CloudTargetConfig
            {
                Host = "192.0.2.1",
                Port = 21,
                Username = "test",
                Password = "test"
            };

            var result = await provider.TestConnectionAsync(config, CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_SftpUnreachable_ReturnsFalse()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            var config = new CloudTargetConfig
            {
                Host = "192.0.2.1",
                Port = 22,
                Username = "test",
                Password = "test"
            };

            var result = await provider.TestConnectionAsync(config, CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task DeleteAsync_FtpUnreachable_ReturnsFalse()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            var config = new CloudTargetConfig
            {
                Host = "192.0.2.1",
                Port = 21,
                Username = "test",
                Password = "test"
            };

            var result = await provider.DeleteAsync("/backups/test.bak", config, CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task DeleteAsync_SftpUnreachable_ReturnsFalse()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            var config = new CloudTargetConfig
            {
                Host = "192.0.2.1",
                Port = 22,
                Username = "test",
                Password = "test"
            };

            var result = await provider.DeleteAsync("/backups/test.bak", config, CancellationToken.None);
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task UploadAsync_ResultContainsCorrectProviderInfo()
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftps);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftps,
                Host = null,
                DisplayName = "My FTPS Server"
            };

            var result = await provider.UploadAsync(
                @"C:\fake.bak", "test.bak", config, null, CancellationToken.None);

            result.ProviderType.Should().Be(CloudProviderType.Ftps);
            result.DisplayName.Should().Be("My FTPS Server");
        }
    }
}
