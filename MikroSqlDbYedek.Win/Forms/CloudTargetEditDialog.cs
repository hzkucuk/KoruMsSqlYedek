using System;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Bulut hedef ekleme/düzenleme dialogu.
    /// Provider türüne göre dinamik alan görünürlüğü sağlar.
    /// </summary>
    public partial class CloudTargetEditDialog : Theme.ModernFormBase
    {
        private readonly CloudTargetConfig _target;
        private readonly bool _isNew;

        /// <summary>Düzenlenen/oluşturulan bulut hedef yapılandırması.</summary>
        public CloudTargetConfig Target => _target;

        /// <summary>Yeni bulut hedef oluşturma.</summary>
        public CloudTargetEditDialog() : this(null) { }

        /// <summary>Mevcut bulut hedefi düzenleme. null ise yeni hedef.</summary>
        public CloudTargetEditDialog(CloudTargetConfig existing)
        {
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
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_GooglePersonal"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_GoogleWorkspace"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_OneDrivePersonal"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_OneDriveBusiness"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_Ftp"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_Ftps"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_Sftp"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_LocalPath"));
            _cmbProviderType.Items.Add(Res.Get("CloudTarget_UncPath"));
        }

        #endregion

        #region Load / Save

        private void LoadTargetToUi()
        {
            _txtDisplayName.Text = _target.DisplayName ?? "";
            _cmbProviderType.SelectedIndex = (int)_target.Type;
            _chkEnabled.Checked = _target.IsEnabled;

            // FTP/SFTP alanları
            _txtHost.Text = _target.Host ?? "";
            _nudPort.Value = _target.Port ?? GetDefaultPort(_target.Type);
            _txtUsername.Text = _target.Username ?? "";
            _txtPassword.Text = ""; // Şifre gösterilmez
            _txtRemotePath.Text = _target.RemoteFolderPath ?? "";

            // OAuth alanları
            _txtClientId.Text = _target.OAuthClientId ?? "";
            _txtClientSecret.Text = ""; // Şifre gösterilmez

            // Yerel/UNC
            _txtLocalOrUncPath.Text = _target.LocalOrUncPath ?? "";

            // Ortak
            _nudBandwidth.Value = _target.BandwidthLimitMbps ?? 0;
            _chkPermanentDelete.Checked = _target.PermanentDeleteFromTrash;

            UpdateFieldVisibility();
        }

        private bool SaveUiToTarget()
        {
            if (string.IsNullOrWhiteSpace(_txtDisplayName.Text))
            {
                MessageBox.Show(Res.Get("CloudTarget_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDisplayName.Focus();
                return false;
            }

            _target.DisplayName = _txtDisplayName.Text.Trim();
            _target.Type = (CloudProviderType)_cmbProviderType.SelectedIndex;
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
                _target.OAuthClientId = _txtClientId.Text.Trim();

                if (!string.IsNullOrEmpty(_txtClientSecret.Text))
                {
                    _target.OAuthClientSecret = PasswordProtector.Protect(_txtClientSecret.Text);
                }
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

        #endregion

        #region Visibility Helpers

        private void UpdateFieldVisibility()
        {
            if (_cmbProviderType.SelectedIndex < 0) return;
            var type = (CloudProviderType)_cmbProviderType.SelectedIndex;

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

        #endregion
    }
}
