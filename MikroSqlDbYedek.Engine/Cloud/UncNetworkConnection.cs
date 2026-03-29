using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using Serilog;
using MikroSqlDbYedek.Core.Helpers;

namespace MikroSqlDbYedek.Engine.Cloud
{
    /// <summary>
    /// UNC ağ paylaşımına kimlik bilgileri ile bağlanır ve IDisposable ile otomatik disconnect sağlar.
    /// WNetAddConnection2 / WNetCancelConnection2 P/Invoke kullanır.
    /// </summary>
    public sealed class UncNetworkConnection : IDisposable
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<UncNetworkConnection>();

        private readonly string _uncPath;
        private bool _connected;
        private bool _disposed;

        /// <summary>
        /// UNC ağ paylaşımına bağlanır.
        /// </summary>
        /// <param name="uncPath">UNC yolu (örn: \\server\share).</param>
        /// <param name="username">Kullanıcı adı (domain\user veya user).</param>
        /// <param name="encryptedPassword">DPAPI + Base64 ile encode edilmiş şifre.</param>
        public UncNetworkConnection(string uncPath, string username, string encryptedPassword)
        {
            _uncPath = uncPath ?? throw new ArgumentNullException(nameof(uncPath));

            if (string.IsNullOrEmpty(username))
            {
                // Kimlik bilgisi yoksa bağlantı denemesi yapma (Windows Auth kullanılır)
                Log.Debug("UNC bağlantısı kimlik bilgisi olmadan açılıyor: {Path}", uncPath);
                return;
            }

            string password = DecryptPassword(encryptedPassword);

            var netResource = new NetResource
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplayType.Share,
                RemoteName = uncPath
            };

            int result = WNetAddConnection2(ref netResource, password, username, 0);
            if (result != 0)
            {
                string errorMsg = new Win32Exception(result).Message;
                Log.Error("UNC bağlantısı başarısız: {Path} — Hata: {Error} ({Code})", uncPath, errorMsg, result);
                throw new InvalidOperationException(
                    $"UNC bağlantısı kurulamadı '{uncPath}': {errorMsg} (hata kodu: {result})");
            }

            _connected = true;
            Log.Information("UNC bağlantısı kuruldu: {Path}", uncPath);
        }

        /// <summary>
        /// UNC bağlantısının aktif olup olmadığını döndürür.
        /// </summary>
        public bool IsConnected => _connected;

        private static string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                if (PasswordProtector.IsProtected(encryptedPassword))
                    return PasswordProtector.Unprotect(encryptedPassword);

                // DPAPI koruması yok — güvenlik riski, şifre düz metin saklanmış
                Log.Warning(
                    "UNC şifresi DPAPI koruması olmadan saklanmış — güvenlik riski! " +
                    "Şifreyi ayarlardan yeniden kaydedin.");
                return encryptedPassword;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "DPAPI şifre çözme başarısız. Şifre kullanılamıyor — " +
                    "şifreyi ayarlardan yeniden kaydedin.");
                return encryptedPassword;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_connected)
            {
                int result = WNetCancelConnection2(_uncPath, 0, true);
                if (result != 0)
                {
                    Log.Warning("UNC bağlantısı kapatılamadı: {Path} — Hata kodu: {Code}", _uncPath, result);
                }
                else
                {
                    Log.Debug("UNC bağlantısı kapatıldı: {Path}", _uncPath);
                }

                _connected = false;
            }
        }

        #region P/Invoke

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(
            ref NetResource netResource,
            string password,
            string username,
            int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(
            string name,
            int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplayType DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        private enum ResourceScope
        {
            Connected = 1,
            GlobalNetwork = 2,
            Remembered = 3
        }

        private enum ResourceType
        {
            Any = 0,
            Disk = 1,
            Print = 2
        }

        private enum ResourceDisplayType
        {
            Generic = 0,
            Domain = 1,
            Server = 2,
            Share = 3
        }

        #endregion
    }
}
