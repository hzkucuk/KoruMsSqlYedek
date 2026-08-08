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

        [DataTestMethod]
        [DataRow(587)]
        [DataRow(25)]
        [DataRow(2525)]
        public void GetSocketOptions_DigerPortlar_StartTlsKullanir(int port)
        {
            SmtpConnectionHelper.GetSocketOptions(port, useSsl: true)
                .Should().Be(SecureSocketOptions.StartTls);
        }

        [DataTestMethod]
        [DataRow(465)]
        [DataRow(587)]
        [DataRow(25)]
        public void GetSocketOptions_SslKapali_SifrelemeUygulamaz(int port)
        {
            SmtpConnectionHelper.GetSocketOptions(port, useSsl: false)
                .Should().Be(SecureSocketOptions.None);
        }
    }
}
