using System;
using System.IO;
using System.Security.Principal;
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
        #region SQL Server Permission Helpers

        /// <summary>
        /// Uygulama ömrü boyunca yalnızca bir kez çalışır.
        /// </summary>
        private static volatile bool _systemPermissionChecked;

        /// <summary>
        /// Uygulamanın çalıştığı Windows hesabının SQL Server'da sysadmin rolüne sahip olup olmadığını kontrol eder.
        /// Yoksa otomatik olarak eklemeye çalışır. Windows Auth kullanılmıyorsa atlanır.
        /// Hem NT AUTHORITY\SYSTEM hem de makine hesapları (DOMAIN\MACHINE$) desteklenir.
        /// </summary>
        internal static void EnsureSystemLoginPermission(SqlConnInfo connectionInfo)
        {
            if (_systemPermissionChecked) return;
            if (connectionInfo.AuthMode != SqlAuthMode.Windows) { _systemPermissionChecked = true; return; }

            try
            {
                // Çalışan hesabın adını al (ör. NT AUTHORITY\SYSTEM veya DOMAIN\MACHINE$)
                string currentIdentity = WindowsIdentity.GetCurrent().Name;
                Log.Debug("Mevcut Windows kimliği: {Identity}", currentIdentity);

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = connectionInfo.Server,
                    IntegratedSecurity = true,
                    InitialCatalog = "master",
                    ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds
                };
                ApplyEncryptionSettings(builder, connectionInfo);

                using var conn = new SqlConnection(builder.ConnectionString);
                conn.Open();

                // Şu anki bağlantının zaten sysadmin olup olmadığını kontrol et
                const string checkCurrentRoleSql = "SELECT IS_SRVROLEMEMBER('sysadmin')";
                using var currentRoleCmd = new SqlCommand(checkCurrentRoleSql, conn);
                var currentRoleResult = currentRoleCmd.ExecuteScalar();
                bool isCurrentSysAdmin = currentRoleResult is not null
                    && currentRoleResult != DBNull.Value
                    && Convert.ToInt32(currentRoleResult) == 1;

                if (isCurrentSysAdmin)
                {
                    Log.Debug("Mevcut hesap zaten sysadmin: {Identity}", currentIdentity);
                    _systemPermissionChecked = true;
                    return;
                }

                // Login var mı kontrol et
                string escapedIdentity = currentIdentity.Replace("'", "''");
                string checkLoginSql =
                    $"SELECT COUNT(*) FROM sys.server_principals WHERE name = N'{escapedIdentity}'";
                using var checkCmd = new SqlCommand(checkLoginSql, conn);
                int loginExists = (int)checkCmd.ExecuteScalar();

                // Köşeli parantez içindeki identifier'da ']' karakteri ']]' olarak escape edilmeli
                string bracketedIdentity = EscapeBracketIdentifier(currentIdentity);

                if (loginExists == 0)
                {
                    string createSql = $"CREATE LOGIN [{bracketedIdentity}] FROM WINDOWS";
                    using var createCmd = new SqlCommand(createSql, conn);
                    createCmd.ExecuteNonQuery();
                    Log.Information("SQL Server login oluşturuldu: {Identity}", currentIdentity);
                }

                // sysadmin rolüne ekle
                string grantSql = $"ALTER SERVER ROLE [sysadmin] ADD MEMBER [{bracketedIdentity}]";
                using var grantCmd = new SqlCommand(grantSql, conn);
                grantCmd.ExecuteNonQuery();
                Log.Information("{Identity} hesabına sysadmin rolü verildi.", currentIdentity);

                _systemPermissionChecked = true;
            }
            catch (Exception ex)
            {
                _systemPermissionChecked = true; // Tekrar denememek için
                string identity = "bilinmiyor";
                try { identity = WindowsIdentity.GetCurrent().Name; } catch { }
                Log.Warning(ex,
                    "{Identity} için SQL Server yetki kontrolü başarısız. " +
                    "Manuel olarak şu komutu çalıştırın: " +
                    "ALTER SERVER ROLE [sysadmin] ADD MEMBER [{Identity}]",
                    identity, identity);
            }
        }

        #endregion

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

            if (lowerMsg.Contains("is not able to access the database") || lowerMsg.Contains("current security context"))
                return $"SQL Server hesabının veritabanına erişim yetkisi yok. " +
                       $"Servis hesabına SQL Server'da sysadmin rolü verin veya " +
                       $"SQL Authentication kullanın. (Detay: {innerMsg})";

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

        #region Connection String & Identifier Helpers

        /// <summary>
        /// TrustServerCertificate=true iken uzak sunucu için yalnızca bir kez uyarı loglamak amacıyla
        /// uyarılan sunucu adlarını tutar.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _trustCertWarnedServers
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Bağlantı şifreleme ayarlarını uygular. Bağlantı HER ZAMAN şifrelidir (Encrypt=Mandatory);
        /// TrustServerCertificate yalnızca sunucu sertifikasının doğrulanıp doğrulanmayacağını belirler.
        /// Uzak bir sunucuya sertifika doğrulaması olmadan bağlanılıyorsa sunucu başına bir kez uyarı loglar.
        /// </summary>
        internal static void ApplyEncryptionSettings(SqlConnectionStringBuilder builder, SqlConnInfo connectionInfo)
        {
            builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
            builder.TrustServerCertificate = connectionInfo.TrustServerCertificate;

            if (connectionInfo.TrustServerCertificate
                && !IsLocalServer(connectionInfo.Server)
                && _trustCertWarnedServers.TryAdd(connectionInfo.Server ?? string.Empty, 0))
            {
                Log.Warning(
                    "SQL bağlantısı şifreli ancak sunucu sertifikası doğrulanmıyor (TrustServerCertificate=true) " +
                    "ve sunucu yerel değil: {Server}. MITM riskini azaltmak için sunucuya geçerli bir " +
                    "CA sertifikası kurup bu seçeneği kapatmanız önerilir.",
                    connectionInfo.Server);
            }
        }

        /// <summary>
        /// Sunucu adının yerel makineyi gösterip göstermediğini belirler:
        /// localhost, 127.0.0.1, ::1, ".", "(local)", makine adı — instance adı ve port dikkate alınmaz.
        /// </summary>
        internal static bool IsLocalServer(string dataSource)
        {
            if (string.IsNullOrWhiteSpace(dataSource))
                return true;

            string host = dataSource.Trim();

            // "tcp:" / "np:" gibi protokol önekleri
            int colon = host.IndexOf(':');
            if (colon > 0 && colon <= 3)
                host = host.Substring(colon + 1);

            // Instance adı (SERVER\INSTANCE) ve port (SERVER,1433) ayrımı
            int sep = host.IndexOfAny(new[] { '\\', ',' });
            if (sep >= 0)
                host = host.Substring(0, sep);

            host = host.Trim();
            if (host.Length == 0)
                return true;

            if (host == "." ||
                string.Equals(host, "(local)", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                host == "127.0.0.1" || host == "::1")
                return true;

            string machine = Environment.MachineName;
            if (string.Equals(host, machine, StringComparison.OrdinalIgnoreCase))
                return true;

            // FQDN biçimi: MACHINE.domain.local
            int dot = host.IndexOf('.');
            if (dot > 0 && string.Equals(host.Substring(0, dot), machine, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Köşeli parantez ([...]) içinde kullanılacak T-SQL identifier'ında ']' karakterini ']]' olarak escape eder.
        /// </summary>
        internal static string EscapeBracketIdentifier(string identifier)
        {
            return identifier?.Replace("]", "]]");
        }

        #endregion

        private string BuildConnectionString(SqlConnInfo connectionInfo)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = connectionInfo.Server,
                ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds
            };
            ApplyEncryptionSettings(builder, connectionInfo);

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
