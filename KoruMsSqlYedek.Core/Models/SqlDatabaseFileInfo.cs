namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// SQL Server'ın kullandığı bir veritabanı dosyası (sys.master_files).
    /// Dosya yedeği bu dosyaları orijinal yolundan ASLA açmaz: AUTO_CLOSE açık bir
    /// veritabanı kapalıyken dosyayı tutmak, SQL Server'ın yeniden açışını OS error 32
    /// ile düşürür ve veritabanı RECOVERY_PENDING'e geçer.
    /// </summary>
    public class SqlDatabaseFileInfo
    {
        public string DatabaseName { get; set; }

        /// <summary>Fiziksel yol — ör. "D:\Mikro\v17xx\PRIMA2022\DATA\MikroDesktop_PRIMA2022._MS".</summary>
        public string PhysicalPath { get; set; }

        /// <summary>sys.databases.is_auto_close_on.</summary>
        public bool IsAutoCloseOn { get; set; }
    }
}
