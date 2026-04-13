using System;

namespace KoruMsSqlYedek.Core.Models
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
        Ftp = 4,
        Ftps = 5,
        Sftp = 6,
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
        Both = 2,

        /// <summary>Grandfather-Father-Son: Günlük / Haftalık / Aylık / Yıllık periyot bazlı saklama.</summary>
        GFS = 3
    }

    /// <summary>
    /// Yedek dosyası tipi — retention şemasında hangi politikanın uygulanacağını belirler.
    /// </summary>
    public enum BackupFileType
    {
        /// <summary>SQL Server tam yedek (.bak + .7z çifti).</summary>
        SqlFull = 0,

        /// <summary>SQL Server fark yedek (.bak + .7z çifti).</summary>
        SqlDifferential = 1,

        /// <summary>SQL Server log/artırımlı yedek (.bak + .7z çifti).</summary>
        SqlLog = 2,

        /// <summary>Dosya/klasör yedekleme arşivi (Files_*.7z).</summary>
        FileBackup = 3,

        /// <summary>VSS ek güvenlik yedeği (_VSS_*.7z).</summary>
        SqlVss = 4
    }

    /// <summary>
    /// Hazır retention şablon türleri.
    /// </summary>
    public enum RetentionTemplateType
    {
        /// <summary>Kullanıcı tanımlı özel politika.</summary>
        Custom = 0,

        /// <summary>Az yer: Full×3, Diff×7, Log×14, Files×5.</summary>
        Minimal = 1,

        /// <summary>Dengeli: Full×7, Diff×14, Log×30, Files×14.</summary>
        Standard = 2,

        /// <summary>Kapsamlı: Full×14, Diff×30, Log×90, Files×30.</summary>
        Extended = 3,

        /// <summary>Grandfather-Father-Son rotasyonu — tüm tipler için.</summary>
        GFS = 4
    }

    /// <summary>
    /// Yedekleme raporu gönderim sıklığı.
    /// </summary>
    public enum ReportFrequency
    {
        /// <summary>Her gün özet rapor.</summary>
        Daily = 0,

        /// <summary>Haftalık özet rapor (Pazartesi).</summary>
        Weekly = 1,

        /// <summary>Aylık özet rapor (ayın 1'i).</summary>
        Monthly = 2
    }

    /// <summary>
    /// [KALDIRILDI v0.73.0] Yedekleme modu artık kullanılmıyor.
    /// Bulut hedef eklenip eklenmediğine göre otomatik belirlenir.
    /// JSON geriye uyumluluk için korunmaktadır.
    /// </summary>
    [Obsolete("BackupMode artık kullanılmıyor. Bulut hedef varlığına göre otomatik belirlenir.")]
    public enum BackupMode
    {
        /// <summary>Yerel yedekleme: Disk, UNC, ağ paylaşımı, harici disk.</summary>
        Local = 0,

        /// <summary>Bulut yedekleme: Google Drive, OneDrive, FTP/SFTP + yerel staging.</summary>
        Cloud = 1
    }
}
