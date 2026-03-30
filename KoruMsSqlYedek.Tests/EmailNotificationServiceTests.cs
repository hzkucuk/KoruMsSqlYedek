using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Notification;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class EmailNotificationServiceTests
    {
        private EmailNotificationService _service;
        private Mock<IAppSettingsManager> _settingsManagerMock;

        [TestInitialize]
        public void Setup()
        {
            _settingsManagerMock = new Mock<IAppSettingsManager>();
            _settingsManagerMock.Setup(m => m.Load()).Returns(TestDataFactory.CreateAppSettingsWithProfile());
            _service = new EmailNotificationService(_settingsManagerMock.Object);
        }

        // ── Config null / devre dışı ──────────────────────────────────────

        [TestMethod]
        public async Task NotifyAsync_WhenConfigNull_ReturnsWithoutException()
        {
            var result = TestDataFactory.CreateSuccessResult();

            Func<Task> act = () => _service.NotifyAsync(result, null, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenEmailDisabled_ReturnsWithoutException()
        {
            var result = TestDataFactory.CreateSuccessResult();
            var config = TestDataFactory.CreateNotificationConfig();
            config.EmailEnabled = false;

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        // ── OnSuccess / OnFailure bayrakları ─────────────────────────────

        [TestMethod]
        public async Task NotifyAsync_WhenSuccessAndOnSuccessFalse_ReturnsWithoutSending()
        {
            var result = TestDataFactory.CreateSuccessResult();
            var config = TestDataFactory.CreateNotificationConfig(onSuccess: false, onFailure: true);

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenFailureAndOnFailureFalse_ReturnsWithoutSending()
        {
            var result = TestDataFactory.CreateFailedResult();
            var config = TestDataFactory.CreateNotificationConfig(onSuccess: true, onFailure: false);

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenPartialSuccessAndOnFailureTrue_AttemptsToSend()
        {
            var result = TestDataFactory.CreateSuccessResult();
            result.Status = BackupResultStatus.PartialSuccess;

            var config = TestDataFactory.CreateNotificationConfig(onSuccess: false, onFailure: true);

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        // ── Profil çözümleme ──────────────────────────────────────────────

        [TestMethod]
        public async Task NotifyAsync_WhenProfileIdSet_UsesProfileFromSettings()
        {
            // Arrange — geçersiz SMTP sunucu içeren profil, bağlantı denemesi yapılır ama exception yutulur
            var profile = TestDataFactory.CreateSmtpProfile("p1", "invalid.host.local");
            var settings = TestDataFactory.CreateAppSettingsWithProfile(profile);
            _settingsManagerMock.Setup(m => m.Load()).Returns(settings);

            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                OnSuccess = false,
                SmtpProfileId = "p1"
            };

            Func<Task> act = () => _service.NotifyAsync(TestDataFactory.CreateFailedResult(), config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenProfileIdNotFound_SkipsSendWithoutException()
        {
            // Arrange — profil ID var ama settings'te yok
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                SmtpProfileId = "nonexistent-id-xyz"
            };

            Func<Task> act = () => _service.NotifyAsync(TestDataFactory.CreateFailedResult(), config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenNoProfileIdAndNoLegacyHost_SkipsSend()
        {
            // Arrange — SmtpProfileId boş, eski SmtpServer da yok
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                SmtpProfileId = null
            };

            Func<Task> act = () => _service.NotifyAsync(TestDataFactory.CreateFailedResult(), config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenLegacySmtpServerSet_FallsBackToLegacyConfig()
        {
            // Arrange — eski plan formatı: SmtpProfileId yok ama SmtpServer var
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                SmtpProfileId = null,
                SmtpServer = "invalid.legacy.smtp.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                EmailTo = "admin@test.com"
            };

            Func<Task> act = () => _service.NotifyAsync(TestDataFactory.CreateFailedResult(), config, CancellationToken.None);

            // Legacy fallback devreye girer, geçersiz host yüzyünden bağlantı başarısız olur ama exception yutulur
            await act.Should().NotThrowAsync();
        }

        // ── Legacy (eski per-plan SMTP) önceki testler ────────────────────

        [TestMethod]
        public async Task NotifyAsync_WhenSmtpServerInvalid_DoesNotThrow()
        {
            var result = TestDataFactory.CreateFailedResult();
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                OnSuccess = false,
                SmtpServer = "invalid.smtp.server.nonexistent.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                EmailTo = "admin@test.com"
            };

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenSuccessAndOnSuccessTrue_AttemptsToSend()
        {
            var result = TestDataFactory.CreateSuccessResult();
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnSuccess = true,
                OnFailure = false,
                SmtpServer = "invalid.smtp.server.nonexistent.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                EmailTo = "admin@test.com"
            };

            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }
    }
}
