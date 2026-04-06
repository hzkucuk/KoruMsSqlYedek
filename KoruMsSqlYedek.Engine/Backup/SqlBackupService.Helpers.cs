using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using SqlConnInfo = KoruMsSqlYedek.Core.Models.SqlConnectionInfo;

namespace KoruMsSqlYedek.Engine.Backup
{
    public partial class SqlBackupService
    {
        #region Retry & Error Helpers

        /// <summary>
        /// Geçici hatalarda otomatik yeniden deneme ile çalıştırır.
        /// </summary>
        private async Task ExecuteWithRetryAsync(
            Action action, string databaseName, string filePath,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    // ct yalnızca task başlamadan önce kontrol edilir; çalışırken Abort() gerekli
                    await Task.Run(action, cancellationToken);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (cancellationToken.IsCancellationRequested)
                {
                    // backup.Abort() çağrısı SmoException fırlatır — OperationCanceledException'a çevir
                    throw new OperationCanceledException(
                        "Yedekleme kullanıcı tarafından iptal edildi.", ex, cancellationToken);
                }
                catch (Exception ex) when (attempt < MaxRetryCount && IsTransientError(ex))
                {
                    Log.Warning(
                        "Geçici hata, yeniden deneniyor ({Attempt}/{MaxRetry}): {Database} — {Error}",
                        attempt, MaxRetryCount, databaseName, ExtractInnermostMessage(ex));

                    TryDeleteFile(filePath);
                    await Task.Delay(RetryBaseDelayMs * attempt, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Exception zincirinde geçici (transient) hata olup olmadığını kontrol eder.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            string msg = ExtractInnermostMessage(ex).ToLowerInvariant();
            return msg.Contains("operating system error 32")
                || msg.Contains("sharing violation")
                || msg.Contains("timeout")
                || msg.Contains("the semaphore timeout period has expired");
        }

        /// <summary>
        /// SMO exception zincirinden en içteki (asıl) hata mesajını çıkarır.
        /// </summary>
        private static string ExtractInnermostMessage(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner.Message;
        }

        /// <summary>
        /// Bilinen SQL/SMO hata kalıpları için Türkçe açıklama üretir.
        /// </summary>
        private static string TranslateBackupError(Exception ex)
        {
            string innerMsg = ExtractInnermostMessage(ex);
            string lowerMsg = innerMsg.ToLowerInvariant();

            if (lowerMsg.Contains("operating system error 32") || lowerMsg.Contains("sharing violation"))
                return $"Veritabanı dosyası başka bir işlem tarafından kullanılıyor. " +
                       $"Mikro yazılımını kapatıp tekrar deneyin. (Detay: {innerMsg})";

            if (lowerMsg.Contains("cannot be opened") && lowerMsg.Contains("inaccessible"))
                return $"Veritabanı dosyalarına erişilemiyor. MDF/LDF dosyalarının SQL Server tarafından " +
                       $"erişilebilir olduğunu kontrol edin. (Detay: {innerMsg})";

            if (lowerMsg.Contains("insufficient disk space") || lowerMsg.Contains("not enough space on the disk"))
                return $"Yetersiz disk alanı. Yedek dizininde yeterli boş alan olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("insufficient memory") || lowerMsg.Contains("not enough memory"))
                return $"Yetersiz bellek. SQL Server'ın yeterli RAM'e sahip olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("access is denied") || lowerMsg.Contains("operating system error 5"))
                return $"Erişim reddedildi. SQL Server servis hesabının yedek dizinine yazma yetkisi " +
                       $"olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("is not accessible") || lowerMsg.Contains("offline"))
                return $"Veritabanı çevrimdışı veya erişilemez durumda. " +
                       $"SQL Server Management Studio'dan veritabanı durumunu kontrol edin. (Detay: {innerMsg})";

            // Bilinmeyen hata — SMO sarmalayıcısı yerine asıl mesajı göster
            return innerMsg;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Başarısız yedek dosyası silinemedi: {Path}", path);
            }
        }

        #endregion

        private string BuildConnectionString(SqlConnInfo connectionInfo)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = connectionInfo.Server,
                ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds,
                TrustServerCertificate = connectionInfo.TrustServerCertificate,
                Encrypt = connectionInfo.TrustServerCertificate
                    ? SqlConnectionEncryptOption.Optional
                    : SqlConnectionEncryptOption.Mandatory
            };

            if (connectionInfo.AuthMode == SqlAuthMode.Windows)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = connectionInfo.Username;
                builder.Password = PasswordProtector.Unprotect(connectionInfo.Password);
            }

            return builder.ConnectionString;
        }
    }
}
