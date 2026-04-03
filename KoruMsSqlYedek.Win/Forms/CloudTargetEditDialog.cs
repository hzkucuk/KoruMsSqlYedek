using System;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Cloud;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Bulut hedef ekleme/düzenleme dialogu.
    /// Provider türüne göre dinamik alan görünürlüğü sağlar.
    /// </summary>
    public partial class CloudTargetEditDialog : Theme.ModernFormBase
    {
        private readonly CloudTargetConfig _target;
        private readonly bool _isNew;
        private readonly AppSettings _appSettings;
        private readonly IAppSettingsManager _settingsManager;

        /// <summary>Combo box index → CloudProviderType eşlemesi.</summary>
        private static readonly CloudProviderType[] ProviderMap =
        {
            CloudProviderType.GoogleDrivePersonal,  // 0: Google Drive ✓
            CloudProviderType.OneDrivePersonal,      // 1: OneDrive
            CloudProviderType.Ftp,                   // 2: FTP
            CloudProviderType.Ftps,                  // 3: FTPS
            CloudProviderType.Sftp,                  // 4: SFTP
            CloudProviderType.LocalPath,             // 5: Yerel Yol
            CloudProviderType.UncPath,               // 6: UNC Ağ Paylaşımı
        };

        /// <summary>Düzenlenen/oluşturulan bulut hedef yapılandırması.</summary>
        public CloudTargetConfig Target => _target;

        /// <summary>Yeni bulut hedef oluşturma.</summary>
        public CloudTargetEditDialog(AppSettings appSettings, IAppSettingsManager settingsManager)
            : this(appSettings, settingsManager, null) { }

        /// <summary>Mevcut bulut hedefi düzenleme. null ise yeni hedef.</summary>
        public CloudTargetEditDialog(AppSettings appSettings, IAppSettingsManager settingsManager, CloudTargetConfig existing)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(settingsManager);

            _appSettings = appSettings;
            _settingsManager = settingsManager;

            InitializeComponent();
            ApplyIcons();

            if (existing != null)
            {
                _target = existing;
                _isNew = false;
                Text = Res.Get("CloudTarget_TitleEdit");
            }
            else
            {
                _target = new CloudTargetConfig();
                _isNew = true;
                Text = Res.Get("CloudTarget_TitleNew");
            }
        }

        private void ApplyIcons()
        {
            const int sz = 16;
            _btnSave.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.FloppyDisk, System.Drawing.Color.White, sz);
            _btnSave.Text = "Kaydet";
            _btnSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnCancel.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.XCircle, System.Drawing.Color.White, sz);
            _btnCancel.Text = "Iptal";
            _btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnBrowseLocal.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Folder, Theme.ModernTheme.AccentPrimary, 14);
            _btnBrowseLocal.Text = "";

            _btnGoogleAuth.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.ShieldCheck, System.Drawing.Color.White, sz);
            _btnGoogleAuth.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnOAuthSettings.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Gear, Theme.ModernTheme.TextSecondary, 14);
            _btnOAuthSettings.Text = "";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PopulateProviderTypes();
            LoadTargetToUi();
        }

        #region ComboBox Population

        private void PopulateProviderTypes()
        {
            _cmbProviderType.Items.Clear();
            _cmbProviderType.Items.Add("Google Drive  \u2713");
            _cmbProviderType.Items.Add("OneDrive");
            _cmbProviderType.Items.Add("FTP");
            _cmbProviderType.Items.Add("FTPS");
            _cmbProviderType.Items.Add("SFTP");
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_LocalPath"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_UncPath"));
        }

        /// <summary>Combo box seçili index'inden CloudProviderType döner.</summary>
        private CloudProviderType GetSelectedProviderType()
        {
            int idx = _cmbProviderType.SelectedIndex;
            if (idx < 0 || idx >= ProviderMap.Length)
                return CloudProviderType.GoogleDrivePersonal;
            return ProviderMap[idx];
        }

        /// <summary>CloudProviderType'a karşılık gelen combo box index'ini döner.</summary>
        private int GetComboIndexForType(CloudProviderType type)
        {
            // Workspace/Business alt türlerini birleşik eşlemeye yönlendir
            if (type == CloudProviderType.GoogleDriveWorkspace)
                type = CloudProviderType.GoogleDrivePersonal;
            if (type == CloudProviderType.OneDriveBusiness)
                type = CloudProviderType.OneDrivePersonal;

            for (int i = 0; i < ProviderMap.Length; i++)
            {
                if (ProviderMap[i] == type) return i;
            }
            return 0;
        }

        #endregion

        #region Load / Save

        private void LoadTargetToUi()
        {
            _txtDisplayName.Text = _target.DisplayName ?? "";
            _cmbProviderType.SelectedIndex = GetComboIndexForType(_target.Type);
            _chkEnabled.Checked = _target.IsEnabled;

            // FTP/SFTP alanları
            _txtHost.Text = _target.Host ?? "";
            _nudPort.Value = _target.Port ?? GetDefaultPort(_target.Type);
            _txtUsername.Text = _target.Username ?? "";
            _txtPassword.Text = ""; // Şifre gösterilmez

            // Uzak klasör yolu — yeni hedefte varsayılan değer ata
            if (_isNew && string.IsNullOrWhiteSpace(_target.RemoteFolderPath))
                _txtRemotePath.Text = IsFtpType(_target.Type) ? "/Koru MsSql Yedek" : "Koru MsSql Yedek";
            else
                _txtRemotePath.Text = _target.RemoteFolderPath ?? "";

            // Yerel/UNC
            _txtLocalOrUncPath.Text = _target.LocalOrUncPath ?? "";

            // Ortak
            _nudBandwidth.Value = _target.BandwidthLimitMbps ?? 0;
            _chkPermanentDelete.Checked = _target.PermanentDeleteFromTrash;

            // OAuth durum
            bool hasToken = !string.IsNullOrEmpty(_target.OAuthTokenJson);
            _lblAuthStatus.Text = hasToken ? "\u2714 Ba\u011fl\u0131" : "Hen\u00fcz do\u011frulanmad\u0131";
            _lblAuthStatus.ForeColor = hasToken ? Theme.ModernTheme.AccentPrimary : Theme.ModernTheme.TextSecondary;

            UpdateFieldVisibility();
        }

        private bool SaveUiToTarget()
        {
            if (string.IsNullOrWhiteSpace(_txtDisplayName.Text))
            {
                Theme.ModernMessageBox.Show(Res.Get("CloudTarget_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDisplayName.Focus();
                return false;
            }

            _target.DisplayName = _txtDisplayName.Text.Trim();
            _target.Type = GetSelectedProviderType();
            _target.IsEnabled = _chkEnabled.Checked;
            _target.RemoteFolderPath = _txtRemotePath.Text.Trim();
            _target.BandwidthLimitMbps = (int)_nudBandwidth.Value == 0 ? (int?)null : (int)_nudBandwidth.Value;
            _target.PermanentDeleteFromTrash = _chkPermanentDelete.Checked;

            var providerType = _target.Type;

            if (IsFtpType(providerType))
            {
                _target.Host = _txtHost.Text.Trim();
                _target.Port = (int)_nudPort.Value;
                _target.Username = _txtUsername.Text.Trim();

                if (!string.IsNullOrEmpty(_txtPassword.Text))
                {
                    _target.Password = PasswordProtector.Protect(_txtPassword.Text);
                }
            }
            else if (IsOAuthType(providerType))
            {
                // Gömülü credential kullanılır; eski özel değerleri temizle
                _target.OAuthClientId = null;
                _target.OAuthClientSecret = null;
            }
            else if (IsLocalType(providerType))
            {
                _target.LocalOrUncPath = _txtLocalOrUncPath.Text.Trim();

                if (providerType == CloudProviderType.UncPath)
                {
                    _target.Username = _txtUsername.Text.Trim();

                    if (!string.IsNullOrEmpty(_txtPassword.Text))
                    {
                        _target.Password = PasswordProtector.Protect(_txtPassword.Text);
                    }
                }
            }

            return true;
        }

        #endregion

        #region Events

        private void OnProviderTypeChanged(object sender, EventArgs e)
        {
            UpdateFieldVisibility();
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!SaveUiToTarget()) return;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnBrowseLocalPath(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("CloudTarget_BrowsePath");
                if (!string.IsNullOrEmpty(_txtLocalOrUncPath.Text))
                    fbd.SelectedPath = _txtLocalOrUncPath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                    _txtLocalOrUncPath.Text = fbd.SelectedPath;
            }
        }

        private async void OnGoogleAuthClick(object sender, EventArgs e)
        {
            try
            {
                var providerType = GetSelectedProviderType();
                if (providerType != CloudProviderType.GoogleDrivePersonal &&
                    providerType != CloudProviderType.GoogleDriveWorkspace)
                {
                    Theme.ModernMessageBox.Show("Bu sa\u011flay\u0131c\u0131 i\u00e7in hesap ba\u011flama hen\u00fcz desteklenmiyor.",
                        "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _btnGoogleAuth.Enabled = false;
                _lblAuthStatus.Text = "Taray\u0131c\u0131da onaylay\u0131n...";
                _lblAuthStatus.ForeColor = Theme.ModernTheme.TextSecondary;

                // ResolveCredentials otomatik olarak AppSettings özel > gömülü seçimi yapar
                string tokenJson = await GoogleDriveAuthHelper.AuthorizeInteractiveAsync(
                    System.Threading.CancellationToken.None);

                _target.OAuthTokenJson = tokenJson;

                // Eski per-target credential kalıntılarını temizle
                _target.OAuthClientId = null;
                _target.OAuthClientSecret = null;
                _lblAuthStatus.Text = "\u2714 Ba\u011fl\u0131";
                _lblAuthStatus.ForeColor = Theme.ModernTheme.AccentPrimary;
            }
            catch (Exception ex)
            {
                _lblAuthStatus.Text = "\u2718 Ba\u015far\u0131s\u0131z";
                _lblAuthStatus.ForeColor = System.Drawing.Color.Red;
                Theme.ModernMessageBox.Show($"Kimlik do\u011frulama ba\u015far\u0131s\u0131z:\n{ex.Message}",
                    "OAuth Hatas\u0131", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnGoogleAuth.Enabled = true;
            }
        }

        private void OnOAuthSettingsClick(object sender, EventArgs e)
        {
            using var dialog = new GoogleOAuthSettingsDialog(_appSettings, _settingsManager);
            dialog.ShowDialog(this);
        }

        #endregion

        #region Visibility Helpers

        private void UpdateFieldVisibility()
        {
            if (_cmbProviderType.SelectedIndex < 0) return;
            var type = GetSelectedProviderType();
            UpdateRemotePathTooltip(type);

            bool isFtp = IsFtpType(type);
            bool isOAuth = IsOAuthType(type);
            bool isLocal = IsLocalType(type);
            bool isUnc = type == CloudProviderType.UncPath;
            bool hasTrash = isOAuth; // Google Drive ve OneDrive çöp kutusu desteği

            // FTP/SFTP grubu
            _grpFtp.Visible = isFtp;

            // OAuth grubu
            _grpOAuth.Visible = isOAuth;

            // Yerel/UNC grubu
            _grpLocal.Visible = isLocal;

            // UNC kimlik bilgileri (Yerel yol'da gizli, UNC'da görünür)
            _lblUncUser.Visible = isUnc;
            _txtUncUser.Visible = isUnc;
            _lblUncPassword.Visible = isUnc;
            _txtUncPassword.Visible = isUnc;

            // Ortak: RemotePath (FTP + OAuth), Trash (OAuth), Bandwidth (hepsi)
            _lblRemotePath.Visible = isFtp || isOAuth;
            _txtRemotePath.Visible = isFtp || isOAuth;
            _chkPermanentDelete.Visible = hasTrash;

            // Port varsayılan değeri
            if (!_isNew || _nudPort.Value == 0)
            {
                _nudPort.Value = GetDefaultPort(type);
            }

            // Yeni hedefte uzak klasör yolu görünür hale gelince varsayılan ata
            if (_isNew && (_lblRemotePath.Visible) && string.IsNullOrWhiteSpace(_txtRemotePath.Text))
            {
                _txtRemotePath.Text = IsFtpType(type) ? "/Koru MsSql Yedek" : "Koru MsSql Yedek";
            }
        }

        private static bool IsFtpType(CloudProviderType type)
        {
            return type == CloudProviderType.Ftp
                || type == CloudProviderType.Ftps
                || type == CloudProviderType.Sftp;
        }

        private static bool IsOAuthType(CloudProviderType type)
        {
            return type == CloudProviderType.GoogleDrivePersonal
                || type == CloudProviderType.GoogleDriveWorkspace
                || type == CloudProviderType.OneDrivePersonal
                || type == CloudProviderType.OneDriveBusiness;
        }

        private static bool IsLocalType(CloudProviderType type)
        {
            return type == CloudProviderType.LocalPath
                || type == CloudProviderType.UncPath;
        }

        private static int GetDefaultPort(CloudProviderType type)
        {
            switch (type)
            {
                case CloudProviderType.Ftp: return 21;
                case CloudProviderType.Ftps: return 990;
                case CloudProviderType.Sftp: return 22;
                default: return 0;
            }
        }

        /// <summary>
        /// Provider türüne göre "Uzak Klasör Yolu" alanının tooltip metnini günceller.
        /// Google Drive / OneDrive: slash (/) ayırıcı, başına slash konmaz.
        /// FTP / FTPS / SFTP: Unix yolu, başında slash olmalı.
        /// </summary>
        private void UpdateRemotePathTooltip(CloudProviderType type)
        {
            string tip;

            if (IsOAuthType(type))
            {
                tip = "Dosyaların yükleneceği klasör adını girin.\r\n"
                    + "• Alt klasör eklemek için \"/\" ayırıcısını kullanın.\r\n"
                    + "• Başına \"/\" veya \"\\\" koymayın.\r\n"
                    + "Örnek:  Yedekler            → Drive'ın kökünde\r\n"
                    + "Örnek:  Yedekler/Plan1      → Yedekler altında Plan1 klasörü";
            }
            else if (IsFtpType(type))
            {
                tip = "FTP sunucusundaki hedef klasörün Unix yolunu girin.\r\n"
                    + "• Başında \"/\" olmalıdır.\r\n"
                    + "• Alt klasörler için \"/\" ayırıcısını kullanın.\r\n"
                    + "Örnek:  /yedekler           → Kök altında yedekler klasörü\r\n"
                    + "Örnek:  /yedekler/plan1     → Yedekler altında plan1 alt klasörü";
            }
            else
            {
                tip = string.Empty;
            }

            _toolTipRemotePath.SetToolTip(_txtRemotePath, tip);
            _toolTipRemotePath.SetToolTip(_lblRemotePath, tip);
        }

        #endregion
    }
}
