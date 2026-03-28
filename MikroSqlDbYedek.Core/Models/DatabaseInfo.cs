using Newtonsoft.Json;

namespace MikroSqlDbYedek.Core.Models
{
    /// <summary>
    /// SQL Server üzerindeki veritabanı bilgisi.
    /// </summary>
    public class DatabaseInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sizeInMb")]
        public double SizeInMb { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("recoveryModel")]
        public string RecoveryModel { get; set; }

        [JsonProperty("lastFullBackupDate")]
        public string LastFullBackupDate { get; set; }

        [JsonProperty("isSystemDb")]
        public bool IsSystemDb { get; set; }
    }
}
