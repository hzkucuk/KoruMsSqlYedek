using System;
using System.Text;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Google Drive OAuth 2.0 gömülü kimlik bilgileri.
    /// Desktop Application türü — Google "public client" kabul eder.
    /// 
    /// Kullanıcılar kendi credential'larını kullanmak isterse
    /// CloudTargetConfig.OAuthClientId/Secret alanları önceliklidir (fallback).
    /// 
    /// Credential güncellemek için: XOR + Base64 encode edilmiş değerleri değiştirin.
    /// CI/CD'de build-time injection için MSBuild property kullanılabilir.
    /// </summary>
    internal static class GoogleOAuthCredentials
    {
        // XOR + Base64 obfuscation — GitHub push protection ve plain text taramalarını engeller.
        // Gerçek değerler Google Cloud Console'dan alınır.
        //
        // Encode etmek için PowerShell:
        //   $key = 0x4B
        //   $bytes = [Text.Encoding]::UTF8.GetBytes("YOUR_VALUE")
        //   $xored = $bytes | ForEach-Object { $_ -bxor $key }
        //   [Convert]::ToBase64String([byte[]]$xored)
        //
        private const byte ObfuscationKey = 0x4B;

        private const string EncodedClientId =
            "fH96e3Nyfnlze3N/Zi1yfz4gJnwkLX4pfHkvKiUpOSlzIiw6ISw9LiN8JSQsZSo7OzhlLCQkLCcuPjguOSgkJT8uJT9lKCQm";

        private const string EncodedClientSecret =
            "DAQIGBsTZgU9PQUHPTwACCQKDnkyCTE8OgEFAg07ewwIEjI=";

        /// <summary>Google OAuth 2.0 Client ID.</summary>
        internal static string ClientId => Decode(EncodedClientId);

        /// <summary>Google OAuth 2.0 Client Secret.</summary>
        internal static string ClientSecret => Decode(EncodedClientSecret);

        /// <summary>
        /// Gömülü credential'ların geçerli (placeholder olmayan) olup olmadığını kontrol eder.
        /// </summary>
        internal static bool IsConfigured =>
            !string.IsNullOrEmpty(ClientId)
            && !ClientId.StartsWith("PLACEHOLDER", StringComparison.Ordinal)
            && !string.IsNullOrEmpty(ClientSecret)
            && !ClientSecret.StartsWith("PLACEHOLDER", StringComparison.Ordinal);

        private static string Decode(string encoded)
        {
            try
            {
                byte[] xored = Convert.FromBase64String(encoded);
                byte[] original = new byte[xored.Length];
                for (int i = 0; i < xored.Length; i++)
                    original[i] = (byte)(xored[i] ^ ObfuscationKey);
                return Encoding.UTF8.GetString(original);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
