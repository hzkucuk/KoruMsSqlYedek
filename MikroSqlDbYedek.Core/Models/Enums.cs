namespace MikroSqlDbYedek.Core.Models
{
    /// <summary>
    /// Yedekleme stratejisi türlerini tanımlar.
    /// </summary>
    public enum BackupStrategyType
    {
        /// <summary>Yalnızca tam yedek.</summary>
        Full = 0,

        /// <summary>Tam + Fark yedek kombinasyonu.</summary>
        FullPlusDifferential = 1,

        /// <summary>Tam + Fark + Artırımlı yedek kombinasyonu.</summary>
        FullPlusDifferentialPlusIncremental = 2
    }

    /// <summary>
    /// SQL Server yedek türü.
    /// </summary>
    public enum SqlBackupType
    {
        Full = 0,
        Differential = 1,
        Incremental = 2
    }

    /// <summary>
    /// SQL Server kimlik doğrulama modu.
    /// </summary>
    public enum SqlAuthMode
    {
        Windows = 0,
        SqlAuthentication = 1
    }

    /// <summary>
    /// Bulut provider türü.
    /// </summary>
    public enum CloudProviderType
    {
        GoogleDrivePersonal = 0,
        GoogleDriveWorkspace = 1,
        OneDrivePersonal = 2,
        OneDriveBusiness = 3,
        Ftp = 4,
        Ftps = 5,
        Sftp = 6,
        LocalPath = 7,
        UncPath = 8
    }

    /// <summary>
    /// Sıkıştırma algoritması.
    /// </summary>
    public enum CompressionAlgorithm
    {
        Lzma2 = 0,
        Lzma = 1,
        BZip2 = 2,
        Deflate = 3
    }

    /// <summary>
    /// Sıkıştırma seviyesi.
    /// </summary>
    public enum CompressionLevel
    {
        None = 0,
        Fast = 1,
        Normal = 2,
        Maximum = 3,
        Ultra = 4
    }

    /// <summary>
    /// Yedekleme sonuç durumu.
    /// </summary>
    public enum BackupResultStatus
    {
        Success = 0,
        PartialSuccess = 1,
        Failed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Retention (saklama) politikası türü.
    /// </summary>
    public enum RetentionPolicyType
    {
        KeepLastN = 0,
        DeleteOlderThanDays = 1,
        Both = 2
    }
}
