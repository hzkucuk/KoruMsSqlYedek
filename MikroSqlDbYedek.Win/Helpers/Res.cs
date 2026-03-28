using System.Globalization;
using System.Resources;

namespace MikroSqlDbYedek.Win.Helpers
{
    /// <summary>
    /// Lokalizasyon için ResourceManager sarmalayıcısı.
    /// CurrentUICulture'a göre resx dosyalarından string çeker.
    /// </summary>
    internal static class Res
    {
        private static readonly ResourceManager _rm =
            new ResourceManager("MikroSqlDbYedek.Win.Properties.Resources",
                typeof(Res).Assembly);

        /// <summary>Belirtilen anahtarın lokalize değerini döndürür.</summary>
        internal static string Get(string key)
        {
            return _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }

        /// <summary>Belirtilen anahtarın lokalize değerini string.Format ile biçimlendirir.</summary>
        internal static string Format(string key, params object[] args)
        {
            var template = _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
            return string.Format(template, args);
        }
    }
}
