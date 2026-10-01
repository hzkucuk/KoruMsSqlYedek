using System;
using System.IO;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Exception zincirinden kullanıcıya gösterilecek anlamlı mesajı üretir.
    /// Sarmalayıcı mesajlar ("see inner exception") atlanır; mesajı boş gelen
    /// derleme yükleme hatalarında (FileNotFound/FileLoad/BadImageFormat) tür ve dosya adı yazılır.
    /// </summary>
    public static class ExceptionMessageHelper
    {
        /// <summary>Zincirin en içteki exception'ı.</summary>
        public static Exception Innermost(Exception ex)
        {
            if (ex == null) return null;
            var inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner;
        }

        /// <summary>
        /// Kullanıcıya gösterilecek mesaj. Önce en içteki anlamlı mesaj aranır;
        /// hiçbiri yoksa tür adı (ve varsa dosya adı) döner. Asla boş dönmez.
        /// </summary>
        public static string Describe(Exception ex)
        {
            if (ex == null) return "Bilinmeyen hata";

            // Derleme yükleme hataları: mesajları genellikle boştur, dosya adı asıl bilgidir
            string assembly = GetFailedAssemblyName(ex);
            if (assembly != null)
            {
                var load = FindAssemblyLoadException(ex);

                // FileLoad/BadImageFormat mesajı asıl ipucunu taşır (ör. sürüm uyuşmazlığı 0x80131040)
                string detail = load is not FileNotFoundException && IsMeaningful(load.Message)
                    ? $" — {load.Message}"
                    : string.Empty;

                return $"Program dosyası yüklenemedi: {assembly}.dll ({load.GetType().Name}){detail}. " +
                       "Servisi yeniden başlatın; sorun sürerse kurulumu onarın ve antivirüs kayıtlarını kontrol edin.";
            }

            // En içten dışa doğru ilk anlamlı mesaj
            string fallback = null;
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (IsMeaningful(e.Message))
                    fallback = e.Message;
            }

            return fallback ?? Innermost(ex).GetType().Name;
        }

        /// <summary>
        /// Zincirde bir .NET derleme yükleme hatası varsa derleme adını döner (ör. "System.Security.Claims"),
        /// yoksa null. Dosya/klasör bulunamadı hataları (Version= içermeyen FileName) derleme hatası sayılmaz.
        /// </summary>
        public static string GetFailedAssemblyName(Exception ex)
        {
            var load = FindAssemblyLoadException(ex);
            if (load == null) return null;

            string fileName = load switch
            {
                FileNotFoundException fnf => fnf.FileName,
                FileLoadException fle => fle.FileName,
                BadImageFormatException bif => bif.FileName,
                _ => null
            };

            int comma = fileName.IndexOf(',');
            return comma > 0 ? fileName.Substring(0, comma).Trim() : fileName.Trim();
        }

        /// <summary>
        /// Derleme yükleme hatası yeni bir süreçte düzelebilir mi? Dosya bulunamadı (geçici
        /// erişim engeli CLR'de önbelleğe alınır) ve paylaşım/kilit ihlali evet; sürüm
        /// uyuşmazlığı (0x80131040) ve bozuk imaj hayır — onları yalnızca kurulum onarımı çözer.
        /// </summary>
        public static bool IsRestartableAssemblyFailure(Exception ex)
            => IsRestartableLoadException(FindAssemblyLoadException(ex));

        /// <summary>
        /// Tek bir yükleme exception'ı (zincir aranmaz, FileName biçimi aranmaz) yeni süreçte
        /// düzelebilir mi? Assembly.Load(ad) çağrısında FileName yalnızca kısa ad olabilir.
        /// </summary>
        public static bool IsRestartableLoadException(Exception ex)
        {
            const int SharingViolation = unchecked((int)0x80070020);
            const int LockViolation = unchecked((int)0x80070021);

            return ex switch
            {
                FileNotFoundException => true,
                FileLoadException fle => fle.HResult == SharingViolation || fle.HResult == LockViolation,
                _ => false
            };
        }

        private static Exception FindAssemblyLoadException(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                string fileName = e switch
                {
                    FileNotFoundException fnf => fnf.FileName,
                    FileLoadException fle => fle.FileName,
                    BadImageFormatException bif => bif.FileName,
                    _ => null
                };

                if (!string.IsNullOrEmpty(fileName) &&
                    fileName.Contains("Version=", StringComparison.Ordinal) &&
                    fileName.Contains("PublicKeyToken=", StringComparison.Ordinal))
                    return e;
            }
            return null;
        }

        private static bool IsMeaningful(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("see inner exception", StringComparison.OrdinalIgnoreCase) < 0
                && message.IndexOf("iç özel duruma bakın", StringComparison.OrdinalIgnoreCase) < 0
                && message.IndexOf("One or more errors occurred", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
