using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Yalnızca Plus sürümünde açık olan özellikler.
    /// Yeni bir ücretli özellik eklerken buraya değer eklenir ve
    /// ilgili kod yolu <c>ILicenseService.IsEnabled</c> ile korunur.
    /// </summary>
    public enum PlusFeature
    {
        /// <summary>Disk imajı yedekleme (wimlib).</summary>
        DiskImage = 0
    }

    /// <summary>
    /// Lisans anahtarının imzalanmış yükü.
    /// Anahtar biçimi: <c>base64url(payload) + "." + base64url(imza)</c>
    /// İmza ECDSA P-256 / SHA-256 ile üretilir; doğrulama çevrimdışıdır.
    /// </summary>
    public class LicensePayload
    {
        /// <summary>Lisansın verildiği kişi/kurum — yalnızca gösterim amaçlı.</summary>
        [JsonProperty("customer")]
        public string Customer { get; set; }

        /// <summary>Bu lisansın açtığı özellikler.</summary>
        [JsonProperty("features")]
        public List<PlusFeature> Features { get; set; } = new List<PlusFeature>();

        /// <summary>Veriliş tarihi (UTC).</summary>
        [JsonProperty("issuedAt")]
        public DateTime IssuedAt { get; set; }

        /// <summary>
        /// Bitiş tarihi (UTC). null ise süresiz.
        /// Süre dolduğunda uygulama çalışmayı sürdürür; yalnızca Plus özellikleri kapanır.
        /// </summary>
        [JsonProperty("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Lisans doğrulamasının sonucu.</summary>
    public enum LicenseStatus
    {
        /// <summary>Anahtar girilmemiş — ücretsiz sürüm.</summary>
        None = 0,

        /// <summary>Geçerli ve süresi dolmamış.</summary>
        Valid = 1,

        /// <summary>İmza doğrulanamadı — anahtar bozuk veya sahte.</summary>
        Invalid = 2,

        /// <summary>İmza geçerli ama süre dolmuş.</summary>
        Expired = 3
    }

    /// <summary>Doğrulama sonucu ve çözülmüş yük.</summary>
    public class LicenseInfo
    {
        public LicenseStatus Status { get; set; } = LicenseStatus.None;

        /// <summary>Yalnızca <see cref="LicenseStatus.Valid"/> veya <see cref="LicenseStatus.Expired"/> ise dolu.</summary>
        public LicensePayload Payload { get; set; }

        /// <summary>Kullanıcıya gösterilebilecek açıklama.</summary>
        public string Message { get; set; }

        public static LicenseInfo Free() => new LicenseInfo
        {
            Status = LicenseStatus.None,
            Message = "Ücretsiz sürüm"
        };
    }
}
