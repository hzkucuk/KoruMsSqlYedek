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

        /// <summary>Users grubuna verilecek erişim düzeyi.</summary>
        public enum UsersAccess
        {
            /// <summary>Users hiç erişemez (yalnızca SYSTEM + Administrators).</summary>
            None,

            /// <summary>Users okuyabilir ama yazamaz — servisin üzerinde iş yaptığı dosyalar.</summary>
            ReadOnly,

            /// <summary>Users yazabilir — çalışma sırasında üretilen log/durum dosyaları.</summary>
            Modify
        }

        /// <summary>
        /// SYSTEM + Administrators FullControl, kalıtım kapalı; Users için
        /// <paramref name="usersAccess"/> düzeyinde ACE eklenir.
        /// </summary>
        /// <remarks>
        /// Users'a okuma hakkı vermek ŞARTTIR: tray uygulaması yükseltilmeden
        /// (asInvoker) çalışır ve planları/logları doğrudan diskten okur. v0.99.91'de
        /// Users tamamen kaldırılmış, bu yüzden sıradan kullanıcıda planlar hiç
        /// görünmemişti. Yazma hakkı ise yalnızca servisin karar girdisi olmayan
        /// dizinlere verilir; Plans/Config salt okunur kalır (kurcalamaya karşı).
        /// </remarks>
        public static DirectorySecurity CreateSecurity(UsersAccess usersAccess)
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

            if (usersAccess != UsersAccess.None)
            {
                FileSystemRights rights = usersAccess == UsersAccess.Modify
                    ? FileSystemRights.Modify
                    : FileSystemRights.ReadAndExecute;

                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                    rights, inherit, PropagationFlags.None, AccessControlType.Allow));
            }

            return security;
        }

        /// <summary>
        /// Dizini yoksa belirtilen ACL ile oluşturur; varsa ACL'i yeniden uygular.
        /// Başarısızlık durumunda hata fırlatır — çağıran taraf karar verir.
        /// </summary>
        public static void EnsureDirectory(string path, UsersAccess usersAccess)
        {
            var security = CreateSecurity(usersAccess);
            var dir = new DirectoryInfo(path);

            if (!dir.Exists)
            {
                dir.Create(security);
                Log.Debug("Dizin oluşturuldu ({Access}): {Path}", usersAccess, path);
                return;
            }

            dir.SetAccessControl(security);
            Log.Debug("ACL yeniden uygulandı ({Access}): {Path}", usersAccess, path);
        }

        /// <summary>
        /// Servis başlangıcında uygulama veri dizinlerini doğru ACL ile oluşturur / düzeltir.
        /// Hatalar loglanır; servis başlangıcını engellemez.
        /// </summary>
        public static void EnsureAppDataDirectoriesRestricted()
        {
            // Servisin üzerinde karar verdiği girdiler: Users okur, yazamaz.
            (string Path, UsersAccess Access)[] directories =
            {
                (PathHelper.PlansDirectory, UsersAccess.ReadOnly),
                (PathHelper.ConfigDirectory, UsersAccess.ReadOnly),

                // Doğrulanmış installer'ların indiği yer — Users'ın işi yok.
                (UpdatesDirectory, UsersAccess.None),

                // Çalışma çıktıları: tray de yedek çalıştırıp log yazabilmeli.
                (PathHelper.LogsDirectory, UsersAccess.Modify),
                (PathHelper.UploadStateDirectory, UsersAccess.Modify),
                (HistoryDirectory, UsersAccess.Modify)
            };

            // Kök dizin: Users okuyabilmeli ki alt dizinlere erişebilsin.
            try
            {
                EnsureDirectory(PathHelper.AppDataDirectory, UsersAccess.ReadOnly);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Kök veri dizini ACL'i uygulanamadı: {Path}", PathHelper.AppDataDirectory);
            }

            foreach (var (path, access) in directories)
            {
                try
                {
                    EnsureDirectory(path, access);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Dizin ACL'i uygulanamadı: {Path}", path);
                }
            }
        }
    }
}
