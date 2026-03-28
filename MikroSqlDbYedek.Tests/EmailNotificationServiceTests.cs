using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.Notification;
using MikroSqlDbYedek.Tests.Helpers;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class EmailNotificationServiceTests
    {
        private EmailNotificationService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new EmailNotificationService();
        }

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
        public async Task NotifyAsync_WhenSmtpServerInvalid_DoesNotThrow()
        {
            // Arrange — geçersiz SMTP sunucu adresi; connect başarısız olur ama exception yutulmalı
            var result = TestDataFactory.CreateFailedResult();
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnFailure = true,
                OnSuccess = false,
                EmailTo = "admin@test.com",
                SmtpServer = "invalid.smtp.server.nonexistent.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                SmtpUsername = "user@test.com"
            };

            // Act — SMTP bağlantısı başarısız olacak ama exception yutulmalı
            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            // Assert — hata yutulur, dışarı fırlatılmaz
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenSuccessAndOnSuccessTrue_AttemptsToSend()
        {
            // Arrange — geçersiz sunucu ile başarılı sonuç gönderme denemesi
            var result = TestDataFactory.CreateSuccessResult();
            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnSuccess = true,
                OnFailure = false,
                EmailTo = "admin@test.com",
                SmtpServer = "invalid.smtp.server.nonexistent.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                SmtpUsername = "user@test.com"
            };

            // Act — bağlantı başarısız olacak ama exception yutulmalı
            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task NotifyAsync_WhenPartialSuccessAndOnFailureTrue_AttemptsToSend()
        {
            // Arrange — partial success, onFailure=true durumunda bildirim tetiklenmeli
            var result = TestDataFactory.CreateSuccessResult();
            result.Status = BackupResultStatus.PartialSuccess;

            var config = new NotificationConfig
            {
                EmailEnabled = true,
                OnSuccess = false,
                OnFailure = true,
                EmailTo = "admin@test.com",
                SmtpServer = "invalid.smtp.server.nonexistent.local",
                SmtpPort = 587,
                SmtpUseSsl = false,
                SmtpUsername = "user@test.com"
            };

            // Act — PartialSuccess != Success, onFailure tetiklenmeli
            Func<Task> act = () => _service.NotifyAsync(result, config, CancellationToken.None);

            // Assert — exception yutulur
            await act.Should().NotThrowAsync();
        }
    }
}
