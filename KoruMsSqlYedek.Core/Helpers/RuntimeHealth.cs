using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// .NET derleme yükleme sağlığı. CLR bir derlemeyi yükleyemezse bu sonucu süreç ömrü
    /// boyunca önbelleğe alır: dosya sonradan erişilebilir olsa bile aynı süreçte her deneme
    /// aynı FileNotFoundException'ı verir. Tek çare süreci yeniden başlatmaktır; bu sınıf
    /// hatayı yakalayıp servisin kendini yeniden başlatabilmesi için olay yayınlar.
    /// </summary>
    public static class RuntimeHealth
    {
        /// <summary>
        /// Gece yedeklemesinde ilk kez yüklenen ve yüklenemezse SQL/SSL yolunu kilitleyen derlemeler.
        /// Açılışta zorla yüklenerek sorun 22:00'da değil servis başlarken görünür olur.
        /// </summary>
        private static readonly string[] CriticalAssemblies =
        {
            "System.Security.Claims",
            "System.Security.Principal.Windows",
            "System.Net.Security",
            "System.Security.Cryptography",
            "System.Net.Http",
        };

        private static int _failureReported;

        /// <summary>
        /// Yeniden başlatmanın çözebileceği her derleme yükleme hatasında tetiklenir (derleme adı ile).
        /// Her raporda tetiklenir — abone o an çalışan planı "etkilenen" olarak kaydedebilsin;
        /// yeniden başlatmanın tek seferliğini abone (RuntimeRestartGuard) sağlar.
        /// </summary>
        public static event Action<string> AssemblyLoadFailed;

        /// <summary>Bu süreçte derleme yükleme hatası görüldü mü?</summary>
        public static bool HasAssemblyLoadFailure => Volatile.Read(ref _failureReported) != 0;

        /// <summary>
        /// Exception zincirinde derleme yükleme hatası varsa raporlar. Olay yalnızca yeniden
        /// başlatmanın çözebileceği hatalarda tetiklenir (sürüm uyuşmazlığı / bozuk imaj için
        /// yeniden başlatma işe yaramaz, yalnızca kurulum onarımı).
        /// </summary>
        /// <returns>Zincirde derleme yükleme hatası varsa true.</returns>
        public static bool ReportAssemblyLoadFailure(Exception ex)
        {
            string assembly = ExceptionMessageHelper.GetFailedAssemblyName(ex);
            if (assembly == null) return false;

            Volatile.Write(ref _failureReported, 1);

            if (!ExceptionMessageHelper.IsRestartableAssemblyFailure(ex))
                return true;

            try { AssemblyLoadFailed?.Invoke(assembly); }
            catch { /* abone hatası çağıranı etkilememeli */ }
            return true;
        }

        /// <summary>
        /// Kritik derlemeleri zorla yükler. Yüklenemeyenlerin adı, hata mesajı ve yeniden
        /// başlatmanın çözüp çözemeyeceği döner (boş liste = sağlıklı).
        /// </summary>
        public static IReadOnlyList<(string Assembly, string Error, bool Restartable)> PreloadCriticalAssemblies()
        {
            var failures = new List<(string, string, bool)>();
            foreach (string name in CriticalAssemblies)
            {
                try
                {
                    Assembly.Load(new AssemblyName(name));
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref _failureReported, 1);
                    failures.Add((name, ExceptionMessageHelper.Describe(ex),
                        ExceptionMessageHelper.IsRestartableLoadException(ex)));
                }
            }
            return failures;
        }
    }
}
