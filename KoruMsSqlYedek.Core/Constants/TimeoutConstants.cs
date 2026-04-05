namespace KoruMsSqlYedek.Core.Constants
{
    /// <summary>
    /// Tüm timeout değerlerinin merkezi tanımları.
    /// Taktir etkisini önlemek için tüm provider ve servisler bu sabitleri kullanmalıdır.
    /// </summary>
    public static class TimeoutConstants
    {
        // ── Bağlantı ──────────────────────────────────────────
        /// <summary>FTP/FTPS bağlantı zaman aşımı (ms).</summary>
        public const int FtpConnectTimeoutMs = 30_000;

        /// <summary>FTP/FTPS veri bağlantısı zaman aşımı (ms).</summary>
        public const int FtpDataConnectionTimeoutMs = 30_000;

        /// <summary>FTP/FTPS okuma zaman aşımı (ms).</summary>
        public const int FtpReadTimeoutMs = 60_000;

        /// <summary>FTP/SFTP bağlantı testi zaman aşımı (ms).</summary>
        public const int FtpTestConnectionTimeoutMs = 10_000;

        /// <summary>SFTP bağlantı zaman aşımı (saniye — SSH.NET TimeSpan bekler).</summary>
        public const int SftpConnectTimeoutSeconds = 30;

        /// <summary>SFTP işlem zaman aşımı (saniye).</summary>
        public const int SftpOperationTimeoutSeconds = 60;

        // ── Mega ──────────────────────────────────────────────
        /// <summary>Mega logout temizliği için kısa zaman aşımı (saniye).</summary>
        public const int MegaLogoutTimeoutSeconds = 10;

        /// <summary>Mega bağlantı ön kontrolü zaman aşımı (saniye).</summary>
        public const int MegaConnectivityCheckSeconds = 10;

        /// <summary>Mega login zaman aşımı (saniye). Hashcash/PBKDF2 hesaplaması nedeniyle yüksek.</summary>
        public const int MegaLoginTimeoutSeconds = 90;

        /// <summary>Mega oturum önbellek süresi (dakika).</summary>
        public const int MegaSessionExpiryMinutes = 15;

        // ── Genel ─────────────────────────────────────────────
        /// <summary>Cloud upload per-target maksimum süre (dakika). Takılmayı önler.</summary>
        public const int CloudUploadPerTargetTimeoutMinutes = 60;

        /// <summary>Bağlantı testi genel zaman aşımı (saniye).</summary>
        public const int GeneralTestConnectionTimeoutSeconds = 15;
    }
}
