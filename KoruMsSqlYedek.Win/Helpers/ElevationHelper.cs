using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;
using Serilog;

namespace KoruMsSqlYedek.Win.Helpers
{
    /// <summary>
    /// Tray uygulaması normalde yükseltilmeden (asInvoker) çalışır: planları ve logları
    /// okumak, servis durumunu izlemek için yönetici hakkı gerekmez. Yalnızca servisin
    /// karar girdisi olan dosyaları (plan, ayar) değiştirmek yükseltme ister — bu dizinler
    /// Users için salt okunurdur. Bu yardımcı, o durumda kullanıcıya net bir yol sunar.
    /// </summary>
    internal static class ElevationHelper
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ElevationHelper));

        /// <summary>Süreç şu anda yükseltilmiş mi (Administrators etkin mi)?</summary>
        internal static bool IsElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Yükseltme durumu belirlenemedi.");
                return false;
            }
        }

        /// <summary>
        /// Kullanıcıya yetki gerektiğini açıklar ve isterse uygulamayı yönetici olarak
        /// yeniden başlatır. Kullanıcı UAC'yi reddederse hiçbir şey yapılmaz.
        /// </summary>
        internal static void OfferRestartElevated(IWin32Window owner, string message)
        {
            var answer = Theme.ModernMessageBox.Show(
                message,
                Res.Get("Elevation_Title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1);

            if (answer != DialogResult.Yes)
                return;

            RestartElevated();
        }

        /// <summary>
        /// Uygulamayı "runas" ile yeniden başlatır ve mevcut örneği kapatır.
        /// UAC reddedilirse (Win32 1223) mevcut örnek çalışmaya devam eder.
        /// </summary>
        internal static void RestartElevated()
        {
            string exePath = Application.ExecutablePath;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // Kullanıcı UAC istemini reddetti — sessizce devam et
                Log.Information("Yönetici olarak yeniden başlatma kullanıcı tarafından iptal edildi.");
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Yönetici olarak yeniden başlatılamadı: {Path}", exePath);
                Theme.ModernMessageBox.Show(
                    Res.Format("Elevation_Failed", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Exit();
        }
    }
}
