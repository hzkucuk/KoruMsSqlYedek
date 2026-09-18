using FluentAssertions;
using MailKit.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Engine.Notification;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SmtpConnectionHelperTests
    {
        [TestMethod]
        public void GetSocketOptions_Port465_ImplicitSslKullanir()
        {
            // 465 implicit SSL bekler; StartTls seçilirse bağlantı zaman aşımına uğrar.
            SmtpConnectionHelper.GetSocketOptions(465, useSsl: true)
                .Should().Be(SecureSocketOptions.SslOnConnect);
        }

        [TestMethod]
        [DataRow(587)]
        [DataRow(25)]
        [DataRow(2525)]
        public void GetSocketOptions_DigerPortlar_StartTlsKullanir(int port)
        {
            SmtpConnectionHelper.GetSocketOptions(port, useSsl: true)
                .Should().Be(SecureSocketOptions.StartTls);
        }

        [TestMethod]
        [DataRow(465)]
        [DataRow(587)]
        [DataRow(25)]
        public void GetSocketOptions_SslKapali_SifrelemeUygulamaz(int port)
        {
            SmtpConnectionHelper.GetSocketOptions(port, useSsl: false)
                .Should().Be(SecureSocketOptions.None);
        }

        [TestMethod]
        public void Configure_Varsayilan_SertifikaDogrulamasiAcik()
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            SmtpConnectionHelper.Configure(client, ignoreCertificateErrors: false);

            client.Timeout.Should().Be(SmtpConnectionHelper.TimeoutMs);
            client.ServerCertificateValidationCallback.Should().BeNull();
        }

        [TestMethod]
        public void Configure_YoksayAcik_SertifikaKabulEdilir()
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            SmtpConnectionHelper.Configure(client, ignoreCertificateErrors: true);

            client.ServerCertificateValidationCallback.Should().NotBeNull();
            client.ServerCertificateValidationCallback(client, null, null,
                System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch).Should().BeTrue();
        }

        [TestMethod]
        public void Configure_YoksayKapatilinca_CallbackTemizlenir()
        {
            // Aynı istemci nesnesi tekrar yapılandırılırsa eski izin kalıcı olmamalı.
            using var client = new MailKit.Net.Smtp.SmtpClient();
            SmtpConnectionHelper.Configure(client, ignoreCertificateErrors: true);
            SmtpConnectionHelper.Configure(client, ignoreCertificateErrors: false);

            client.ServerCertificateValidationCallback.Should().BeNull();
        }

        [TestMethod]
        public void IsCertificateError_SslHandshake_True()
        {
            var ex = new System.InvalidOperationException("dış",
                new SslHandshakeException("sertifika doğrulanamadı"));
            SmtpConnectionHelper.IsCertificateError(ex).Should().BeTrue();
        }

        [TestMethod]
        public void IsCertificateError_DigerHata_False()
        {
            SmtpConnectionHelper.IsCertificateError(new System.TimeoutException()).Should().BeFalse();
            SmtpConnectionHelper.IsCertificateError(null).Should().BeFalse();
        }
    }
}
