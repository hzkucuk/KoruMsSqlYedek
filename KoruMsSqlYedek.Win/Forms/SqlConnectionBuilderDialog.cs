#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Theme;
using Microsoft.Data.SqlClient;
using Serilog;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Tam teşekküllü SQL Server bağlantı yapılandırıcısı.
    /// Visual Studio "Bağlantı Ekle" dialog'uyla eşdeğer deneyim sunar:
    /// sunucu tarama, Windows/SQL Auth, veritabanı listesi, test ve connection string önizleme.
    /// </summary>
    internal partial class SqlConnectionBuilderDialog : ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SqlConnectionBuilderDialog>();
        private readonly ISqlBackupService _sqlBackupService;

        /// <summary>
        /// Dialog başarıyla kapatıldığında doldurulan bağlantı bilgisi.
        /// </summary>
        public SqlConnectionInfo? Result { get; private set; }

        public SqlConnectionBuilderDialog(ISqlBackupService sqlBackupService)
        {
            ArgumentNullException.ThrowIfNull(sqlBackupService);
            _sqlBackupService = sqlBackupService;

            InitializeComponent();

            // Auth mode seçenekleri
            _cmbAuthMode.Items.Add("Windows Kimlik Doğrulama");
            _cmbAuthMode.Items.Add("SQL Server Kimlik Doğrulama");
            _cmbAuthMode.SelectedIndex = 0;

            UpdatePreview();
        }

        /// <summary>
        /// Mevcut bir <see cref="SqlConnectionInfo"/> ile dialog alanlarını önceden doldurur.
        /// </summary>
        public void LoadFrom(SqlConnectionInfo connInfo)
        {
            ArgumentNullException.ThrowIfNull(connInfo);

            _cmbServer.Text = connInfo.Server ?? string.Empty;
            _cmbAuthMode.SelectedIndex = connInfo.AuthMode == SqlAuthMode.SqlAuthentication ? 1 : 0;
            _txtUsername.Text = connInfo.Username ?? string.Empty;

            // Eğer mevcut bir şifre varsa veya boş değilse, şifrelenmiş şifreyi çözüp dialog şifre alanına yerleştirelim
            if (connInfo.AuthMode == SqlAuthMode.SqlAuthentication && !string.IsNullOrEmpty(connInfo.Password))
            {
                try
                {
                    _txtPassword.Text = PasswordProtector.Unprotect(connInfo.Password) ?? string.Empty;
                }
                catch
                {
                    _txtPassword.Text = string.Empty;
                }
            }
            else
            {
                _txtPassword.Text = string.Empty;
            }

            _nudTimeout.Value = Math.Clamp(connInfo.ConnectionTimeoutSeconds, 5, 300);
            _chkTrustCert.Checked = connInfo.TrustServerCertificate;

            UpdateCredentialVisibility();
            UpdatePreview();
        }

        // ─── Sunucu tarama ───────────────────────────────────────────────────────

        private async void OnBrowseServersClick(object? sender, EventArgs e)
        {
            _btnBrowseServers.Enabled = false;
            SetStatus("Ağdaki SQL Server örnekleri taranıyor...", isError: false);

            try
            {
                IReadOnlyList<string> servers = await Task.Run(DiscoverSqlServersAsync);

                _cmbServer.Items.Clear();
                foreach (string srv in servers)
                    _cmbServer.Items.Add(srv);

                if (servers.Count == 0)
                {
                    SetStatus("Ağda SQL Server örneği bulunamadı. Sunucu adını elle giriniz.", isError: true);
                }
                else
                {
                    SetStatus($"{servers.Count} sunucu bulundu.", isError: false);
                    _cmbServer.DroppedDown = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL Server tarama hatası.");
                SetStatus($"Tarama hatası: {ex.Message}", isError: true);
            }
            finally
            {
                _btnBrowseServers.Enabled = true;
            }
        }

        private static List<string> DiscoverSqlServersAsync()
        {
            var result = new List<string>();

            try
            {
                // SQL Browser servisi UDP 1434 portundan broadcast sorgusuna yanıt verir.
                using var udp = new UdpClient();
                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = 3000;

                byte[] request = [0x02]; // CLNT_UCAST_EX
                IPEndPoint broadcast = new(IPAddress.Broadcast, 1434);
                udp.Send(request, request.Length, broadcast);

                while (true)
                {
                    try
                    {
                        IPEndPoint remote = new(IPAddress.Any, 0);
                        byte[] response = udp.Receive(ref remote);
                        string text = Encoding.ASCII.GetString(response, 3, response.Length - 3);

                        foreach (string segment in text.Split(';'))
                        {
                            int idx = segment.IndexOf("ServerName;", StringComparison.OrdinalIgnoreCase);
                            if (idx < 0) continue;

                            string[] parts = segment.Split(';');
                            string server = string.Empty;
                            string instance = string.Empty;

                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (parts[i].Equals("ServerName", StringComparison.OrdinalIgnoreCase))
                                    server = parts[i + 1];
                                else if (parts[i].Equals("InstanceName", StringComparison.OrdinalIgnoreCase))
                                    instance = parts[i + 1];
                            }

                            if (string.IsNullOrWhiteSpace(server)) continue;

                            string entry = string.IsNullOrWhiteSpace(instance) || instance.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                                ? server
                                : $"{server}\\{instance}";

                            if (!result.Contains(entry, StringComparer.OrdinalIgnoreCase))
                                result.Add(entry);
                        }
                    }
                    catch (SocketException)
                    {
                        break; // Timeout — UDP alma bitti
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL Browser UDP tarama hatası.");
            }

            // Yerel makineyi de ekle
            string localHost = Environment.MachineName;
            if (!result.Contains(localHost, StringComparer.OrdinalIgnoreCase))
                result.Insert(0, localHost);

            return result.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ─── Bağlantı testi ──────────────────────────────────────────────────────

        private async void OnTestConnectionClick(object? sender, EventArgs e)
        {
            string server = _cmbServer.Text.Trim();
            if (string.IsNullOrWhiteSpace(server))
            {
                SetStatus("Sunucu adı boş olamaz.", isError: true);
                return;
            }

            _btnTestConn.Enabled = false;
            SetStatus("Bağlantı test ediliyor...", isError: false);

            try
            {
                SqlConnectionInfo connInfo = BuildCurrentConnInfo();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(connInfo.ConnectionTimeoutSeconds));
                bool ok = await _sqlBackupService.TestConnectionAsync(connInfo, cts.Token);

                if (ok)
                {
                    SetStatus("✔  Bağlantı başarılı!", isError: false, success: true);
                }
                else
                {
                    SetStatus("✘  Bağlantı kurulamadı. Sunucu adı ve kimlik bilgilerini kontrol ediniz.", isError: true);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Bağlantı test hatası.");
                SetStatus($"✘  Hata: {ex.Message}", isError: true);
            }
            finally
            {
                _btnTestConn.Enabled = true;
            }
        }

        // ─── Auth mode değişimi ──────────────────────────────────────────────────

        private void OnAuthModeChanged(object? sender, EventArgs e)
        {
            UpdateCredentialVisibility();
            UpdatePreview();
        }

        private void UpdateCredentialVisibility()
        {
            bool isSqlAuth = _cmbAuthMode.SelectedIndex == 1;
            _pnlCredentials.Visible = isSqlAuth;
        }

        // ─── Alan değişimi + önizleme ─────────────────────────────────────────────

        private void OnServerTextChanged(object? sender, EventArgs e) => UpdatePreview();

        private void OnFieldChanged(object? sender, EventArgs e) => UpdatePreview();

        private void UpdatePreview()
        {
            _txtPreview.Text = BuildConnectionStringPreview();
        }

        private string BuildConnectionStringPreview()
        {
            string server = _cmbServer.Text.Trim();
            if (string.IsNullOrWhiteSpace(server))
                return string.Empty;

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                IntegratedSecurity = _cmbAuthMode.SelectedIndex != 1,
                TrustServerCertificate = _chkTrustCert.Checked,
                ConnectTimeout = (int)_nudTimeout.Value
            };

            if (_cmbAuthMode.SelectedIndex == 1)
            {
                builder.UserID = _txtUsername.Text.Trim();
                builder.Password = "***";
            }

            return builder.ConnectionString;
        }

        // ─── OK ──────────────────────────────────────────────────────────────────

        private void OnOkClick(object? sender, EventArgs e)
        {
            string server = _cmbServer.Text.Trim();
            if (string.IsNullOrWhiteSpace(server))
            {
                SetStatus("Sunucu adı boş olamaz.", isError: true);
                DialogResult = DialogResult.None;
                return;
            }

            if (_cmbAuthMode.SelectedIndex == 1 && string.IsNullOrWhiteSpace(_txtUsername.Text.Trim()))
            {
                SetStatus("SQL kimlik doğrulama için kullanıcı adı girilmelidir.", isError: true);
                DialogResult = DialogResult.None;
                return;
            }

            Result = BuildCurrentConnInfo();
        }

        // ─── Yardımcılar ─────────────────────────────────────────────────────────

        private SqlConnectionInfo BuildCurrentConnInfo()
        {
            var info = new SqlConnectionInfo
            {
                Server = _cmbServer.Text.Trim(),
                AuthMode = _cmbAuthMode.SelectedIndex == 1
                    ? SqlAuthMode.SqlAuthentication
                    : SqlAuthMode.Windows,
                Username = _txtUsername.Text.Trim(),
                ConnectionTimeoutSeconds = (int)_nudTimeout.Value,
                TrustServerCertificate = _chkTrustCert.Checked
            };

            if (_cmbAuthMode.SelectedIndex == 1 && !string.IsNullOrWhiteSpace(_txtPassword.Text))
                info.Password = PasswordProtector.Protect(_txtPassword.Text);

            return info;
        }

        private void SetStatus(string message, bool isError, bool success = false)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = isError
                ? ModernTheme.StatusError
                : success
                    ? ModernTheme.StatusSuccess
                    : ModernTheme.TextSecondary;
        }
    }


}
