using System;
using System.Security.Cryptography;
using System.Text;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Görev düzenleme şifre koruması — SHA256 hash + DPAPI.
    /// Şifre ve güvenlik sorusu cevapları bu sınıf ile hashlenir/doğrulanır.
    /// </summary>
    public static class PlanPasswordHelper
    {
        /// <summary>
        /// Düz metin şifreyi SHA256 → DPAPI → Base64 olarak hashler.
        /// </summary>
        public static string HashPassword(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            byte[] sha256 = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
            byte[] dpapi = ProtectedData.Protect(sha256, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(dpapi);
        }

        /// <summary>
        /// Düz metin şifreyi saklanan hash ile karşılaştırır.
        /// </summary>
        public static bool VerifyPassword(string plainText, string storedHash)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                byte[] dpapi = Convert.FromBase64String(storedHash);
                byte[] storedSha256 = ProtectedData.Unprotect(dpapi, null, DataProtectionScope.CurrentUser);
                byte[] inputSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
                return CryptographicOperations.FixedTimeEquals(storedSha256, inputSha256);
            }
            catch
            {
                return false;
            }
        }
    }
}
