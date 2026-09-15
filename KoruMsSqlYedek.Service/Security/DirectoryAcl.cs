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
    /// {Kurulum}\Data altındaki dizinler için ACL yardımcıları.
    /// v0.99.95'ten itibaren ağacın tamamına dokunulmaz: veri kökü installer
    /// tarafından Users:Modify ile oluşturulur, servis yalnızca bu girdinin
    /// yerinde olduğunu doğrular (elle kurulum için) ve <c>Updates</c> dizinini
    /// SYSTEM + Administrators'a kilitler. Kalıtımı kesip her alt dizine ayrı
    /// ACL yazan eski yaklaşım (v0.99.91–v0.99.94) tray'i defalarca kilitledi.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class DirectoryAcl
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(DirectoryAcl));

        /// <summary>Self-update installer'larının ve restart flag'inin tutulduğu dizin.</summary>
        public static string UpdatesDirectory => PathHelper.UpdatesDirectory;

        /// <summary>Yedek geçmişi dizini (BackupHistoryManager varsayılanı ile aynı).</summary>
        public static string HistoryDirectory => PathHelper.HistoryDirectory;

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

        private const InheritanceFlags Inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        /// <summary>
        /// SYSTEM + Administrators FullControl, kalıtım kapalı; Users için
        /// <paramref name="usersAccess"/> düzeyinde ACE eklenir.
        /// Yalnızca <c>Updates</c> gibi tek başına kilitlenecek dizinler için kullanılır.
        /// </summary>
        public static DirectorySecurity CreateSecurity(UsersAccess usersAccess)
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                FileSystemRights.FullControl, Inherit, PropagationFlags.None, AccessControlType.Allow));

            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl, Inherit, PropagationFlags.None, AccessControlType.Allow));

            if (usersAccess != UsersAccess.None)
            {
                FileSystemRights rights = usersAccess == UsersAccess.Modify
                    ? FileSystemRights.Modify
                    : FileSystemRights.ReadAndExecute;

                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                    rights, Inherit, PropagationFlags.None, AccessControlType.Allow));
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
        /// Veri köküne (<c>{Kurulum}\Data</c>) Users:Modify girdisini EKLER — mevcut
        /// girdilere ve kalıtıma dokunmaz. Installer bunu zaten yapar; elle/taşınabilir
        /// kurulumda Program Files altındaki varsayılan (Users: salt okunur) yükseltilmemiş
        /// tray'in plan yazmasını engellerdi.
        /// </summary>
        public static void EnsureDataRootWritableByUsers()
        {
            string root = PathHelper.AppDataDirectory;
            var dir = new DirectoryInfo(root);
            dir.Create();

            var security = dir.GetAccessControl();
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if (rule.IdentityReference == users
                    && rule.AccessControlType == AccessControlType.Allow
                    && (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify
                    && (rule.InheritanceFlags & Inherit) == Inherit)
                {
                    Log.Debug("Veri kökünde Users:Modify zaten var: {Path}", root);
                    return;
                }
            }

            security.AddAccessRule(new FileSystemAccessRule(
                users, FileSystemRights.Modify, Inherit, PropagationFlags.None, AccessControlType.Allow));
            dir.SetAccessControl(security);
            Log.Information("Veri köküne Users:Modify eklendi: {Path}", root);
        }

        /// <summary>
        /// Servis başlangıcında veri dizini ACL'ini doğrular: kök Users için yazılabilir,
        /// <c>Updates</c> yalnızca SYSTEM + Administrators. Hatalar loglanır; servis
        /// başlangıcını engellemez.
        /// </summary>
        public static void EnsureAppDataDirectoriesRestricted()
        {
            try
            {
                EnsureDataRootWritableByUsers();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Veri kökü ACL'i doğrulanamadı: {Path}", PathHelper.AppDataDirectory);
            }

            // Doğrulanmış installer'ların indiği yer — Users'ın işi yok.
            try
            {
                EnsureDirectory(UpdatesDirectory, UsersAccess.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Updates dizini ACL'i uygulanamadı: {Path}", UpdatesDirectory);
            }
        }
    }
}
