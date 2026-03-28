using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Core.Interfaces
{
    /// <summary>
    /// Uygulama ayarlarının yüklenmesi ve kaydedilmesini yönetir.
    /// JSON dosyası: %APPDATA%\MikroSqlDbYedek\Config\appsettings.json
    /// </summary>
    public interface IAppSettingsManager
    {
        /// <summary>Mevcut ayarları yükler. Dosya yoksa varsayılan ayarlarla oluşturur.</summary>
        AppSettings Load();

        /// <summary>Ayarları JSON dosyasına kaydeder.</summary>
        void Save(AppSettings settings);
    }
}
