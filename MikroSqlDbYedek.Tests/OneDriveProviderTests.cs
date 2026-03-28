using System;
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
    public class OneDriveProviderTests
    {
        #region Constructor & Properties

        [TestMethod]
        public void Constructor_PersonalType_SetsDisplayNameCorrectly()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            provider.DisplayName.Should().Be("OneDrive (Bireysel)");
            provider.ProviderType.Should().Be(CloudProviderType.OneDrivePersonal);
        }

        [TestMethod]
        public void Constructor_BusinessType_SetsDisplayNameCorrectly()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDriveBusiness);
            provider.DisplayName.Should().Be("OneDrive (Kurumsal)");
            provider.ProviderType.Should().Be(CloudProviderType.OneDriveBusiness);
        }

        [TestMethod]
        public void Constructor_InvalidType_ThrowsArgumentException()
        {
            Action act = () => new OneDriveProvider(CloudProviderType.Ftp);
            act.Should().Throw<ArgumentException>()
                .Which.Message.Should().Contain("Geçersiz OneDrive provider türü");
        }

        [TestMethod]
        public void Constructor_GoogleDriveType_ThrowsArgumentException()
        {
            Action act = () => new OneDriveProvider(CloudProviderType.GoogleDrivePersonal);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region UploadAsync — Validation

        [TestMethod]
        public async Task UploadAsync_NullConfig_ReturnsFailure()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", null,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("null");
        }

        [TestMethod]
        public async Task UploadAsync_MissingClientId_ReturnsFailure()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDrivePersonal,
                OAuthClientId = null,
                OAuthTokenJson = "sometoken"
            };

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Client ID");
        }

        [TestMethod]
        public async Task UploadAsync_MissingToken_ReturnsFailure()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDrivePersonal,
                OAuthClientId = "some-client-id",
                OAuthTokenJson = null
            };

            var result = await provider.UploadAsync(
                "somefile.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("token");
        }

        [TestMethod]
        public async Task UploadAsync_InvalidToken_ReturnsFailure()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDrivePersonal,
                OAuthClientId = "some-client-id",
                OAuthTokenJson = "not-valid-base64!!!"
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
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            // Base64 of at least a non-empty byte array to pass IsTokenValid
            var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDrivePersonal,
                OAuthClientId = "some-client-id",
                OAuthTokenJson = validBase64
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent\file.7z", "remote.7z", config,
                new Progress<int>(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("bulunamadı");
        }

        #endregion

        #region DeleteAsync — Validation

        [TestMethod]
        public async Task DeleteAsync_InvalidToken_ReturnsFalse()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDriveBusiness);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDriveBusiness,
                OAuthClientId = "some-client-id",
                OAuthTokenJson = "invalid-token"
            };

            var deleted = await provider.DeleteAsync("some-file-id", config, CancellationToken.None);
            deleted.Should().BeFalse();
        }

        #endregion

        #region TestConnectionAsync — Validation

        [TestMethod]
        public async Task TestConnectionAsync_NullConfig_ReturnsFalse()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            var connected = await provider.TestConnectionAsync(null, CancellationToken.None);
            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_MissingClientId_ReturnsFalse()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDrivePersonal);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDrivePersonal,
                OAuthClientId = null
            };

            var connected = await provider.TestConnectionAsync(config, CancellationToken.None);
            connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task TestConnectionAsync_InvalidToken_ReturnsFalse()
        {
            var provider = new OneDriveProvider(CloudProviderType.OneDriveBusiness);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.OneDriveBusiness,
                OAuthClientId = "some-client-id",
                OAuthTokenJson = "not-base64!!!"
            };

            var connected = await provider.TestConnectionAsync(config, CancellationToken.None);
            connected.Should().BeFalse();
        }

        #endregion

        #region OneDriveAuthHelper — IsTokenValid

        [TestMethod]
        public void IsTokenValid_NullOrEmpty_ReturnsFalse()
        {
            OneDriveAuthHelper.IsTokenValid(null).Should().BeFalse();
            OneDriveAuthHelper.IsTokenValid("").Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_InvalidBase64_ReturnsFalse()
        {
            OneDriveAuthHelper.IsTokenValid("not-valid-base64!!!").Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_EmptyBase64_ReturnsFalse()
        {
            // Base64 of empty byte array
            string emptyBase64 = Convert.ToBase64String(new byte[0]);
            OneDriveAuthHelper.IsTokenValid(emptyBase64).Should().BeFalse();
        }

        [TestMethod]
        public void IsTokenValid_ValidBase64WithContent_ReturnsTrue()
        {
            // Base64 of non-empty byte array (simulating MSAL cache data)
            string validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            OneDriveAuthHelper.IsTokenValid(validBase64).Should().BeTrue();
        }

        #endregion

        #region ValidateConfig

        [TestMethod]
        public void ValidateConfig_NullConfig_ReturnsError()
        {
            var error = OneDriveProvider.ValidateConfig(null);
            error.Should().NotBeNull();
            error.Should().Contain("null");
        }

        [TestMethod]
        public void ValidateConfig_MissingClientId_ReturnsError()
        {
            var config = new CloudTargetConfig
            {
                OAuthClientId = null,
                OAuthTokenJson = Convert.ToBase64String(new byte[] { 1, 2, 3 })
            };

            var error = OneDriveProvider.ValidateConfig(config);
            error.Should().Contain("Client ID");
        }

        [TestMethod]
        public void ValidateConfig_MissingToken_ReturnsError()
        {
            var config = new CloudTargetConfig
            {
                OAuthClientId = "some-id",
                OAuthTokenJson = null
            };

            var error = OneDriveProvider.ValidateConfig(config);
            error.Should().Contain("token");
        }

        [TestMethod]
        public void ValidateConfig_ValidConfig_ReturnsNull()
        {
            var config = new CloudTargetConfig
            {
                OAuthClientId = "some-id",
                OAuthTokenJson = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 })
            };

            var error = OneDriveProvider.ValidateConfig(config);
            error.Should().BeNull();
        }

        #endregion
    }
}
