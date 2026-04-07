using System;
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
    public class GoogleDriveProviderTests
    {
        #region Constructor & Properties

        [TestMethod]
        public void Constructor_PersonalType_SetsDisplayNameCorrectly()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            provider.DisplayName.Should().Be("Google Drive");
            provider.ProviderType.Should().Be(CloudProviderType.GoogleDrivePersonal);
        }

        [TestMethod]
        public void Constructor_InvalidType_ThrowsArgumentException()
        {
            Action act = () => new GoogleDriveProvider(CloudProviderType.Ftp);
            act.Should().Throw<ArgumentException>()
                .Which.Message.Should().Contain("GoogleDrivePersonal");
        }

        [TestMethod]
        public void Constructor_SftpType_ThrowsArgumentException()
        {
            Action act = () => new GoogleDriveProvider(CloudProviderType.Sftp);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region UploadAsync — Validation

        [TestMethod]
        public async Task UploadAsync_NullConfig_ReturnsFailure()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", null,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task UploadAsync_MissingClientId_ReturnsFailure()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                OAuthClientId = null,
                OAuthClientSecret = "secret",
                OAuthTokenJson = "{}"
            };

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            // Gömülü credential'lar mevcut olduğunda config ClientId boş olsa bile
            // ValidateConfig geçer — hata token/auth aşamasında oluşur
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task UploadAsync_MissingClientSecret_ReturnsFailure()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                OAuthClientId = "client-id",
                OAuthClientSecret = null,
                OAuthTokenJson = "{}"
            };

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            // Gömülü credential'lar mevcut olduğunda config ClientSecret boş olsa bile
            // ValidateConfig geçer — hata token/auth aşamasında oluşur
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task UploadAsync_MissingToken_ReturnsFailure()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                OAuthClientId = "client-id",
                OAuthClientSecret = "secret",
                OAuthTokenJson = null
            };

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("token");
        }

        [TestMethod]
        public async Task UploadAsync_MissingSourceFile_ReturnsFailure()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = CreateValidConfig();

            var result = await provider.UploadAsync(
                @"C:\nonexistent\file.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region DeleteAsync — Validation

        [TestMethod]
        public async Task DeleteAsync_InvalidToken_ReturnsFalse()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = CreateValidConfig();

            // Geçersiz token ile API çağrısı başarısız olacak
            bool deleted = await provider.DeleteAsync("invalid-file-id", config, CancellationToken.None);

            deleted.Should().BeFalse("geçersiz token ile silme başarısız olmalı");
        }

        #endregion

        #region TestConnectionAsync — Validation

        [TestMethod]
        public async Task TestConnectionAsync_NullConfig_ReturnsFalse()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);

            bool connected = await provider.TestConnectionAsync(null, CancellationToken.None);

            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_MissingClientId_ReturnsFalse()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                OAuthClientId = "",
                OAuthClientSecret = "secret",
                OAuthTokenJson = "{}"
            };

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_InvalidToken_ReturnsFalse()
        {
            var provider = new GoogleDriveProvider(CloudProviderType.GoogleDrivePersonal);
            var config = CreateValidConfig();

            bool connected = await provider.TestConnectionAsync(config, CancellationToken.None);

            connected.Should().BeFalse("geçersiz token ile bağlantı testi başarısız olmalı");
        }

        #endregion

        #region GoogleDriveAuthHelper — Token Validation

        [TestMethod]
        public void IsTokenValid_NullOrEmpty_ReturnsFalse()
        {
            GoogleDriveAuthHelper.IsTokenValid(null).Should().BeFalse();
            GoogleDriveAuthHelper.IsTokenValid("").Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_InvalidJson_ReturnsFalse()
        {
            GoogleDriveAuthHelper.IsTokenValid("not-json").Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_MissingRefreshToken_ReturnsFalse()
        {
            string json = "{\"access_token\":\"test\"}";
            GoogleDriveAuthHelper.IsTokenValid(json).Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_WithRefreshToken_ReturnsTrue()
        {
            string json = "{\"access_token\":\"test\",\"refresh_token\":\"refresh123\",\"token_type\":\"Bearer\"}";
            GoogleDriveAuthHelper.IsTokenValid(json).Should().BeTrue();
        }

        #endregion

        #region Helpers

        private static CloudTargetConfig CreateValidConfig()
        {
            return new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                DisplayName = "Test Google Drive",
                OAuthClientId = "test-client-id.apps.googleusercontent.com",
                OAuthClientSecret = "test-client-secret",
                OAuthTokenJson = "{\"access_token\":\"invalid\",\"refresh_token\":\"invalid\",\"token_type\":\"Bearer\"}",
                RemoteFolderPath = "KoruMsSqlYedek/Backups"
            };
        }

        #endregion
    }
}
