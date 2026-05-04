using System;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Bulut klasöründe listelenen bir dosya kaydı.
    /// Provider-bağımsız retention "folder sweep" için kullanılır.
    /// </summary>
    public class CloudFileEntry
    {
        /// <summary>Provider'a özgü dosya tanımlayıcı (Google Drive: fileId).</summary>
        public string FileId { get; set; }

        /// <summary>Dosya adı (yol içermez).</summary>
        public string Name { get; set; }

        /// <summary>Oluşturulma zamanı (UTC). Provider sağlamazsa ModifiedTime kullanılır.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Dosya boyutu (byte). Bilinmiyorsa 0.</summary>
        public long SizeBytes { get; set; }
    }
}
