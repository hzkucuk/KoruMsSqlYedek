using System;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// DPAPI + Base64 ile geri dönüşümlü şifre koruma.
    /// LocalMachine scope kullanılır — hem Tray (kullanıcı) hem Service (LocalSystem) tarafından çözülebilir.
    /// </summary>
    public static class PasswordProtector
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PasswordProtector));
        /// <summary>
        /// Düz metni DPAPI ile şifreler ve Base64 olarak döndürür.
        /// </summary>
        /// <param name="plainText">Şifrelenecek düz metin.</param>
        /// <returns>DPAPI + Base64 encode edilmiş değer.</returns>
        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// DPAPI + Base64 değerini çözer ve düz metni döndürür.
        /// Önce LocalMachine scope ile dener, başarısız olursa CurrentUser ile dener (eski veriler için).
        /// </summary>
        /// <param name="protectedBase64">DPAPI + Base64 encode edilmiş değer.</param>
        /// <returns>Düz metin.</returns>
        public static string Unprotect(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64))
                return null;

            byte[] encryptedBytes = Convert.FromBase64String(protectedBase64);

            // Önce LocalMachine scope ile dene (yeni format)
            try
            {
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // LocalMachine başarısız — eski CurrentUser scope ile dene
            }

            // Geriye dönük uyumluluk: CurrentUser scope (v0.75.1 ve öncesi)
            byte[] fallbackBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(fallbackBytes);
        }

        /// <summary>
        /// CurrentUser scope ile korunan değeri LocalMachine scope'a dönüştürür.
        /// Migrasyon sırasında kullanılır.
        /// </summary>
        /// <param name="protectedBase64">CurrentUser scope ile korunan DPAPI + Base64 değer.</param>
        /// <returns>LocalMachine scope ile yeniden korunan değer; çözme başarısız olursa null.</returns>
        public static string MigrateToLocalMachine(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64))
                return null;

            try
            {
                // Önce zaten LocalMachine mı kontrol et
                byte[] encrypted = Convert.FromBase64String(protectedBase64);
                try
                {
                    ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
                    return protectedBase64; // zaten LocalMachine scope — değişiklik gerekmez
                }
                catch (CryptographicException) { /* LocalMachine değil, CurrentUser ile dene */ }

                // CurrentUser ile çöz, LocalMachine ile yeniden şifrele
                byte[] plainBytes = ProtectedData.Unprotect(
                    encrypted,
                    null,
                    DataProtectionScope.CurrentUser);

                byte[] reEncrypted = ProtectedData.Protect(
                    plainBytes,
                    null,
                    DataProtectionScope.LocalMachine);

                return Convert.ToBase64String(reEncrypted);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "DPAPI migrasyon başarısız — şifre yeniden koruma yapılamadı");
                return null;
            }
        }

        /// <summary>
        /// Değerin geçerli bir DPAPI korumalı Base64 olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsProtected(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                Unprotect(value);
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "DPAPI çözme başarısız — değer korumalı değil");
                return false;
            }
        }
    }
}
