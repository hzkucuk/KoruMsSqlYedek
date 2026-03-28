using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.Cloud;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class CloudProviderFactoryTests
    {
        private CloudProviderFactory _factory;

        [TestInitialize]
        public void Setup()
        {
            _factory = new CloudProviderFactory();
        }

        // ── CreateProvider — Google Drive ──

        [TestMethod]
        public void CreateProvider_GoogleDrivePersonal_ReturnsGoogleDriveProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.GoogleDrivePersonal);

            provider.Should().BeOfType<GoogleDriveProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.GoogleDrivePersonal);
        }

        [TestMethod]
        public void CreateProvider_GoogleDriveWorkspace_ReturnsGoogleDriveProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.GoogleDriveWorkspace);

            provider.Should().BeOfType<GoogleDriveProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.GoogleDriveWorkspace);
        }

        // ── CreateProvider — OneDrive ──

        [TestMethod]
        public void CreateProvider_OneDrivePersonal_ReturnsOneDriveProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.OneDrivePersonal);

            provider.Should().BeOfType<OneDriveProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.OneDrivePersonal);
        }

        [TestMethod]
        public void CreateProvider_OneDriveBusiness_ReturnsOneDriveProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.OneDriveBusiness);

            provider.Should().BeOfType<OneDriveProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.OneDriveBusiness);
        }

        // ── CreateProvider — FTP/SFTP ──

        [TestMethod]
        public void CreateProvider_Ftp_ReturnsFtpSftpProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.Ftp);

            provider.Should().BeOfType<FtpSftpProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.Ftp);
        }

        [TestMethod]
        public void CreateProvider_Ftps_ReturnsFtpSftpProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.Ftps);

            provider.Should().BeOfType<FtpSftpProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.Ftps);
        }

        [TestMethod]
        public void CreateProvider_Sftp_ReturnsFtpSftpProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.Sftp);

            provider.Should().BeOfType<FtpSftpProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.Sftp);
        }

        // ── CreateProvider — Local/UNC ──

        [TestMethod]
        public void CreateProvider_LocalPath_ReturnsLocalNetworkProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.LocalPath);

            provider.Should().BeOfType<LocalNetworkProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.LocalPath);
        }

        [TestMethod]
        public void CreateProvider_UncPath_ReturnsLocalNetworkProvider()
        {
            var provider = _factory.CreateProvider(CloudProviderType.UncPath);

            provider.Should().BeOfType<LocalNetworkProvider>();
            provider.ProviderType.Should().Be(CloudProviderType.UncPath);
        }

        // ── CreateProvider — Invalid ──

        [TestMethod]
        public void CreateProvider_InvalidType_ThrowsArgumentOutOfRange()
        {
            var invalidType = (CloudProviderType)99;

            Action act = () => _factory.CreateProvider(invalidType);

            act.Should().Throw<ArgumentOutOfRangeException>()
                .And.ParamName.Should().Be("type");
        }

        // ── IsSupported ──

        [TestMethod]
        public void IsSupported_AllValidTypes_ReturnsTrue()
        {
            _factory.IsSupported(CloudProviderType.GoogleDrivePersonal).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.GoogleDriveWorkspace).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.OneDrivePersonal).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.OneDriveBusiness).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.Ftp).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.Ftps).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.Sftp).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.LocalPath).Should().BeTrue();
            _factory.IsSupported(CloudProviderType.UncPath).Should().BeTrue();
        }

        [TestMethod]
        public void IsSupported_InvalidType_ReturnsFalse()
        {
            _factory.IsSupported((CloudProviderType)99).Should().BeFalse();
        }

        // ── CreateProvider — her çağrı yeni instance üretir ──

        [TestMethod]
        public void CreateProvider_CalledTwice_ReturnsDifferentInstances()
        {
            var first = _factory.CreateProvider(CloudProviderType.Ftp);
            var second = _factory.CreateProvider(CloudProviderType.Ftp);

            first.Should().NotBeSameAs(second);
        }
    }
}
