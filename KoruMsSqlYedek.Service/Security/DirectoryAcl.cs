using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;

namespace KoruMsSqlYedek.Service.Security
{
    /// <summary>
    /// %ProgramData%\KoruMsSqlYedek altındaki dizinler için kısıtlı ACL yardımcıları.
    /// Kalıtım kapatılır; yalnızca SYSTEM ve BUILTIN\Administrators tam yetki alır.
    /// Böylece kurulum programı ACL uygulamamış olsa bile (manuel/taşınabilir kurulum)
    /// plan, config, log ve güncelleme dosyaları sıradan kullanıcılar tarafından değiştirilemez.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class DirectoryAcl
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(DirectoryAcl));

        /// <summary>Self-update installer'larının ve restart flag'inin tutulduğu dizin.</summary>
        public static string UpdatesDirectory => Path.Combine(PathHelper.AppDataDirectory, "Updates");

        /// <summary>Yedek geçmişi dizini (BackupHistoryManager varsayılanı ile aynı).</summary>
        public static string HistoryDirectory => Path.Combine(PathHelper.AppDataDirectory, "History");

        /// <summary>
        /// SYSTEM + Administrators FullControl, kalıtım kapalı, başka hiçbir ACE yok.
        /// </summary>
        public static DirectorySecurity CreateRestrictedSecurity()
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));

            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));

            return security;
        }

        /// <summary>
        /// Dizini yoksa kısıtlı ACL ile oluşturur; varsa kısıtlı ACL'i yeniden uygular.
        /// Başarısızlık durumunda hata fırlatır — çağıran taraf karar verir.
        /// </summary>
        public static void EnsureRestrictedDirectory(string path)
        {
            var security = CreateRestrictedSecurity();
            var dir = new DirectoryInfo(path);

            if (!dir.Exists)
            {
                dir.Create(security);
                Log.Debug("Kısıtlı ACL ile dizin oluşturuldu: {Path}", path);
                return;
            }

            dir.SetAccessControl(security);
            Log.Debug("Kısıtlı ACL yeniden uygulandı: {Path}", path);
        }

        /// <summary>
        /// Servis başlangıcında tüm uygulama veri dizinlerini kısıtlı ACL ile oluşturur / düzeltir.
        /// Hatalar loglanır; servis başlangıcını engellemez.
        /// </summary>
        public static void EnsureAppDataDirectoriesRestricted()
        {
            string[] directories =
            {
                PathHelper.PlansDirectory,
                PathHelper.LogsDirectory,
                PathHelper.ConfigDirectory,
                PathHelper.UploadStateDirectory,
                HistoryDirectory,
                UpdatesDirectory
            };

            foreach (string path in directories)
            {
                try
                {
                    EnsureRestrictedDirectory(path);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Dizin ACL'i uygulanamadı: {Path}", path);
                }
            }
        }
    }
}
