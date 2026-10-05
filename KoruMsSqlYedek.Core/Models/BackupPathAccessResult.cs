namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// SQL Server servis hesabının yedek dizinine yazıp yazamadığının sonucu.
    /// Dizine uygulamanın/servisin yazabilmesi yetmez: .bak dosyasını SQL Server kendi
    /// servis hesabıyla yazar ve her instance'ın hesabı farklıdır.
    /// </summary>
    public class BackupPathAccessResult
    {
        /// <summary>
        /// true: yazılabilir. false: SQL Server yedek dosyasını açamadı (yetki/yol yok).
        /// null: test sonuçsuz (bağlantı, yetki vb.) — yedekleme engellenmez.
        /// </summary>
        public bool? IsWritable { get; set; }

        public string Path { get; set; }

        /// <summary>@@SERVERNAME — ör. "SUNUCU\MIKRO".</summary>
        public string InstanceName { get; set; }

        /// <summary>SQL Server servis hesabı — ör. "NT Service\MSSQL$MIKRO" (VIEW SERVER STATE yoksa boş).</summary>
        public string ServiceAccount { get; set; }

        /// <summary>SQL Server'ın döndürdüğü hata metni.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Kullanıcıya gösterilecek açıklama (yalnızca IsWritable == false için anlamlı).</summary>
        public string Describe()
        {
            string instance = string.IsNullOrEmpty(InstanceName) ? "SQL Server" : $"SQL Server instance '{InstanceName}'";
            string account = string.IsNullOrEmpty(ServiceAccount) ? "servis hesabı" : $"servis hesabı '{ServiceAccount}'";
            return $"{instance} {account} yedek dizinine yazamıyor: {Path}. " +
                   $"Bu hesaba klasörde Değiştirme (Modify) izni verin veya başka bir dizin seçin. " +
                   $"(Detay: {ErrorMessage})";
        }
    }
}
