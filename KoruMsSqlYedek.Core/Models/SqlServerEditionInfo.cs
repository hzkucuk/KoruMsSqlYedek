namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// SQL Server instance sürüm ve edition bilgisi.
    /// Bağlantı testi ve plan doğrulamasında kullanılır.
    /// </summary>
    public class SqlServerEditionInfo
    {
        /// <summary>
        /// Edition metni: "Express Edition (64-bit)", "Standard Edition (64-bit)" vb.
        /// </summary>
        public string Edition { get; set; } = string.Empty;

        /// <summary>
        /// Sürüm numarası: "16.0.1000.6" gibi.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// SQL Server Express mi? (Edition string'inde "Express" geçiyor mu)
        /// Express'te: varsayılan Simple recovery model, SQL Agent yok.
        /// </summary>
        public bool IsExpress { get; set; }

        /// <summary>
        /// Transaction log yedeklerini etkileyen sınırlamalar var mı?
        /// Express instance'larda True olma ihtimali yüksektir (Simple recovery model).
        /// </summary>
        public bool HasLogBackupLimitation => IsExpress;
    }
}
