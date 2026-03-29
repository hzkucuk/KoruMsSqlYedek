using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Bulut provider fabrika arayüzü.
    /// CloudProviderType'a göre uygun ICloudProvider örneği oluşturur.
    /// </summary>
    public interface ICloudProviderFactory
    {
        /// <summary>
        /// Belirtilen türe göre cloud provider oluşturur.
        /// </summary>
        ICloudProvider CreateProvider(CloudProviderType type);

        /// <summary>
        /// Belirtilen türün desteklenip desteklenmediğini kontrol eder.
        /// </summary>
        bool IsSupported(CloudProviderType type);
    }
}
