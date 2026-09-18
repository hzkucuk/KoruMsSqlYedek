using System;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace KoruMsSqlYedek.Engine.Notification
{
    /// <summary>
    /// SMTP bağlantı seçeneklerini porta göre belirler.
    /// 465 numaralı port implicit SSL (SMTPS) bekler: istemci daha ilk bayttan itibaren
    /// TLS el sıkışması başlatmalıdır. 587/25 ise düz bağlanıp STARTTLS ile yükseltilir.
    /// Yanlış seçim yapıldığında sunucu yanıt vermez ve bağlantı zaman aşımına uğrar.
    /// </summary>
    public static class SmtpConnectionHelper
    {
        /// <summary>Implicit SSL (SMTPS) portu.</summary>
        public const int ImplicitSslPort = 465;

        /// <summary>
        /// Bağlantı/işlem zaman aşımı (ms). MailKit varsayılanı 2 dakikadır;
        /// yanlış yapılandırmada kullanıcıyı bu kadar bekletmemek için kısaltıldı.
        /// </summary>
        public const int TimeoutMs = 20000;

        /// <summary>Porta ve SSL tercihine uygun <see cref="SecureSocketOptions"/> döndürür.</summary>
        public static SecureSocketOptions GetSocketOptions(int port, bool useSsl)
        {
            if (!useSsl)
                return SecureSocketOptions.None;

            return port == ImplicitSslPort
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
        }

        /// <summary>
        /// İstemciye ortak ayarları uygular: zaman aşımı ve sertifika doğrulama politikası.
        /// <paramref name="ignoreCertificateErrors"/> açıkken sunucu sertifikası (self-signed,
        /// ad uyuşmazlığı, süresi dolmuş) sorgulanmadan kabul edilir; kapalıyken .NET'in
        /// varsayılan zincir doğrulaması geçerlidir. Seçenek profil bazlıdır ve varsayılanı kapalıdır.
        /// </summary>
        public static void Configure(SmtpClient client, bool ignoreCertificateErrors)
        {
            client.Timeout = TimeoutMs;
            client.ServerCertificateValidationCallback = ignoreCertificateErrors
                ? (_, _, _, _) => true
                : null;
        }

        /// <summary>
        /// Hatanın TLS sertifika doğrulamasından kaynaklanıp kaynaklanmadığını söyler.
        /// UI bu durumda kullanıcıyı "sertifika hatalarını yoksay" seçeneğine yönlendirir.
        /// </summary>
        public static bool IsCertificateError(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SslHandshakeException || e is System.Security.Authentication.AuthenticationException)
                    return true;
            }
            return false;
        }
    }
}
