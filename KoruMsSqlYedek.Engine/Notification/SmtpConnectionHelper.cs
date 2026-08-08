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
    }
}
