using System;
using System.Security.Cryptography;
using System.Text;

namespace MikroSqlDbYedek.Core.Helpers
{
    /// <summary>
    /// DPAPI + Base64 ile geri dönüşümlü şifre koruma.
    /// Sadece aynı Windows kullanıcısı tarafından çözülebilir.
    /// </summary>
    public static class PasswordProtector
    {
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
                DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// DPAPI + Base64 değerini çözer ve düz metni döndürür.
        /// </summary>
        /// <param name="protectedBase64">DPAPI + Base64 encode edilmiş değer.</param>
        /// <returns>Düz metin.</returns>
        public static string Unprotect(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64))
                return null;

            byte[] encryptedBytes = Convert.FromBase64String(protectedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
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
            catch
            {
                return false;
            }
        }
    }
}
