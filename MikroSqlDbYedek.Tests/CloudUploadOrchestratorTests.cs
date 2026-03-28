using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.Cloud;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class CloudUploadOrchestratorTests
    {
        [TestMethod]
        public async Task UploadToAllAsync_AllProvidersSucceed_ReturnsAllSuccess()
        {
            // Arrange
            var mockFtp = CreateMockProvider(CloudProviderType.Ftp, "FTP", true);
            var mockLocal = CreateMockProvider(CloudProviderType.LocalPath, "Local", true);

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtp.Object, mockLocal.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" },
                new CloudTargetConfig { Type = CloudProviderType.LocalPath, IsEnabled = true, DisplayName = "Local" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(r => r.IsSuccess);
        }

        [TestMethod]
        public async Task UploadToAllAsync_DisabledTargetsSkipped()
        {
            // Arrange
            var mockFtp = CreateMockProvider(CloudProviderType.Ftp, "FTP", true);

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtp.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false, DisplayName = "Disabled FTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().BeEmpty();
            mockFtp.Verify(p => p.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CloudTargetConfig>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task UploadToAllAsync_ProviderNotFound_ReturnsFailureResult()
        {
            // Arrange — boş provider listesi
            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Provider bulunamadı");
        }

        [TestMethod]
        public async Task UploadToAllAsync_ProviderThrowsOnce_RetriesAndSucceeds()
        {
            // Arrange — ilk denemede exception, ikincide başarı
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);
            mockProvider.Setup(p => p.DisplayName).Returns("FTP");

            int callCount = 0;
            mockProvider.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns((string local, string remote, CloudTargetConfig cfg, IProgress<int> prog, CancellationToken ct) =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new Exception("Bağlantı hatası");

                    return Task.FromResult(new CloudUploadResult
                    {
                        ProviderType = CloudProviderType.Ftp,
                        DisplayName = "FTP",
                        IsSuccess = true,
                        RemoteFilePath = "/backups/test.7z"
                    });
                });

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeTrue();
            callCount.Should().Be(2);
        }

        [TestMethod]
        public async Task UploadToAllAsync_AllRetriesFail_ReturnsFailureWithRetryCount()
        {
            // Arrange — tüm denemeler başarısız
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Sftp);
            mockProvider.Setup(p => p.DisplayName).Returns("SFTP");

            mockProvider.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Sunucu yanıt vermiyor"));

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Sunucu yanıt vermiyor");
            results[0].RetryCount.Should().Be(3); // MaxRetries = 3
        }

        [TestMethod]
        public async Task UploadToAllAsync_CancellationRequested_ThrowsOperationCanceled()
        {
            // Arrange
            var mockProvider = CreateMockProvider(CloudProviderType.Ftp, "FTP", true);
            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockProvider.Object });

            var cts = new CancellationTokenSource();
            cts.Cancel(); // hemen iptal

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => orchestrator.UploadToAllAsync(
                    @"C:\test.7z", "test.7z", targets, null, cts.Token));
        }

        [TestMethod]
        public async Task UploadToAllAsync_MixedResults_ReturnsPartialSuccess()
        {
            // Arrange — biri başarılı, biri başarısız
            var mockFtp = CreateMockProvider(CloudProviderType.Ftp, "FTP", true);
            var mockSftp = new Mock<ICloudProvider>();
            mockSftp.Setup(p => p.ProviderType).Returns(CloudProviderType.Sftp);
            mockSftp.Setup(p => p.DisplayName).Returns("SFTP");
            mockSftp.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SFTP hatası"));

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtp.Object, mockSftp.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" },
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(2);
            results[0].IsSuccess.Should().BeTrue();
            results[1].IsSuccess.Should().BeFalse();
        }

        // ── Factory Constructor Tests ──

        [TestMethod]
        public async Task UploadToAllAsync_WithFactory_CreatesProviderAndUploads()
        {
            // Arrange
            var mockProvider = CreateMockProvider(CloudProviderType.Ftp, "FTP", true);
            var mockFactory = new Mock<ICloudProviderFactory>();
            mockFactory.Setup(f => f.IsSupported(CloudProviderType.Ftp)).Returns(true);
            mockFactory.Setup(f => f.CreateProvider(CloudProviderType.Ftp)).Returns(mockProvider.Object);

            var orchestrator = new CloudUploadOrchestrator(mockFactory.Object);

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
            };

            // Act
            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeTrue();
            mockFactory.Verify(f => f.CreateProvider(CloudProviderType.Ftp), Times.Once);
        }

        [TestMethod]
        public void Constructor_NullFactory_ThrowsArgumentNull()
        {
            Action act = () => new CloudUploadOrchestrator((ICloudProviderFactory)null);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("factory");
        }

        [TestMethod]
        public async Task UploadToAllAsync_FactoryCachesProvider_SecondCallDoesNotRecreate()
        {
            // Arrange
            var mockProvider = CreateMockProvider(CloudProviderType.LocalPath, "Local", true);
            var mockFactory = new Mock<ICloudProviderFactory>();
            mockFactory.Setup(f => f.IsSupported(CloudProviderType.LocalPath)).Returns(true);
            mockFactory.Setup(f => f.CreateProvider(CloudProviderType.LocalPath)).Returns(mockProvider.Object);

            var orchestrator = new CloudUploadOrchestrator(mockFactory.Object);

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.LocalPath, IsEnabled = true, DisplayName = "Local" }
            };

            // Act — iki kez upload
            await orchestrator.UploadToAllAsync(@"C:\a.7z", "a.7z", targets, null, CancellationToken.None);
            await orchestrator.UploadToAllAsync(@"C:\b.7z", "b.7z", targets, null, CancellationToken.None);

            // Assert — factory yalnızca 1 kez çağrılmalı (cache)
            mockFactory.Verify(f => f.CreateProvider(CloudProviderType.LocalPath), Times.Once);
        }

        // ── DeleteFromAllAsync Tests ──

        [TestMethod]
        public async Task DeleteFromAllAsync_Success_ReturnsSuccessResult()
        {
            // Arrange
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);
            mockProvider.Setup(p => p.DeleteAsync(
                    It.IsAny<string>(), It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
            };

            // Act
            var results = await orchestrator.DeleteFromAllAsync("file-id-123", targets, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeTrue();
            results[0].ProviderType.Should().Be(CloudProviderType.Ftp);
        }

        [TestMethod]
        public async Task DeleteFromAllAsync_ProviderThrows_CapturesError()
        {
            // Arrange
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Sftp);
            mockProvider.Setup(p => p.DeleteAsync(
                    It.IsAny<string>(), It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Silme hatası"));

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            // Act
            var results = await orchestrator.DeleteFromAllAsync("file-id", targets, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Silme hatası");
        }

        [TestMethod]
        public async Task DeleteFromAllAsync_DisabledTarget_Skipped()
        {
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false, DisplayName = "FTP" }
            };

            var results = await orchestrator.DeleteFromAllAsync("file-id", targets, CancellationToken.None);

            results.Should().BeEmpty();
            mockProvider.Verify(p => p.DeleteAsync(
                It.IsAny<string>(), It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task DeleteFromAllAsync_ProviderNotFound_ReturnsErrorMessage()
        {
            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            var results = await orchestrator.DeleteFromAllAsync("file-id", targets, CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Provider bulunamadı");
        }

        // ── TestAllConnectionsAsync Tests ──

        [TestMethod]
        public async Task TestAllConnectionsAsync_Success_ReturnsConnected()
        {
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.LocalPath);
            mockProvider.Setup(p => p.TestConnectionAsync(
                    It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.LocalPath, IsEnabled = true, DisplayName = "Local" }
            };

            var results = await orchestrator.TestAllConnectionsAsync(targets, CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].IsConnected.Should().BeTrue();
            results[0].ProviderType.Should().Be(CloudProviderType.LocalPath);
        }

        [TestMethod]
        public async Task TestAllConnectionsAsync_ProviderThrows_CapturesError()
        {
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);
            mockProvider.Setup(p => p.TestConnectionAsync(
                    It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Bağlantı zaman aşımı"));

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
            };

            var results = await orchestrator.TestAllConnectionsAsync(targets, CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].IsConnected.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Bağlantı zaman aşımı");
        }

        [TestMethod]
        public async Task TestAllConnectionsAsync_DisabledTarget_Skipped()
        {
            var mockProvider = new Mock<ICloudProvider>();
            mockProvider.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);

            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { mockProvider.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false, DisplayName = "FTP" }
            };

            var results = await orchestrator.TestAllConnectionsAsync(targets, CancellationToken.None);

            results.Should().BeEmpty();
            mockProvider.Verify(p => p.TestConnectionAsync(
                It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task TestAllConnectionsAsync_ProviderNotFound_ReturnsErrorMessage()
        {
            var orchestrator = new CloudUploadOrchestrator(new ICloudProvider[] { });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP" }
            };

            var results = await orchestrator.TestAllConnectionsAsync(targets, CancellationToken.None);

            results.Should().HaveCount(1);
            results[0].IsConnected.Should().BeFalse();
            results[0].ErrorMessage.Should().Contain("Provider bulunamadı");
        }

        [TestMethod]
        public async Task TestAllConnectionsAsync_MultipleTargets_ReturnsAll()
        {
            var mockFtp = new Mock<ICloudProvider>();
            mockFtp.Setup(p => p.ProviderType).Returns(CloudProviderType.Ftp);
            mockFtp.Setup(p => p.TestConnectionAsync(
                    It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var mockLocal = new Mock<ICloudProvider>();
            mockLocal.Setup(p => p.ProviderType).Returns(CloudProviderType.LocalPath);
            mockLocal.Setup(p => p.TestConnectionAsync(
                    It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtp.Object, mockLocal.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" },
                new CloudTargetConfig { Type = CloudProviderType.LocalPath, IsEnabled = true, DisplayName = "Local" }
            };

            var results = await orchestrator.TestAllConnectionsAsync(targets, CancellationToken.None);

            results.Should().HaveCount(2);
            results[0].IsConnected.Should().BeTrue();
            results[1].IsConnected.Should().BeFalse();
        }

        // ── Helpers ──

        private Mock<ICloudProvider> CreateMockProvider(
            CloudProviderType type, string displayName, bool success)
        {
            var mock = new Mock<ICloudProvider>();
            mock.Setup(p => p.ProviderType).Returns(type);
            mock.Setup(p => p.DisplayName).Returns(displayName);
            mock.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CloudUploadResult
                {
                    ProviderType = type,
                    DisplayName = displayName,
                    IsSuccess = success,
                    RemoteFilePath = "/backups/test.7z",
                    UploadedAt = success ? DateTime.UtcNow : (DateTime?)null
                });
            return mock;
        }
    }
}
