using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Plan ekleme/düzenleme formu — 8 sekmeli TabControl.
    /// Tüm BackupPlan alanlarını düzenleme imkanı sağlar.
    /// </summary>
    public partial class PlanEditForm : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<PlanEditForm>();
        private readonly IPlanManager _planManager;
        private readonly ISqlBackupService _sqlBackupService;
        private readonly BackupPlan _plan;
        private readonly bool _isNew;

        /// <summary>Yeni plan oluşturma.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService)
            : this(planManager, sqlBackupService, null) { }

        /// <summary>Mevcut planı düzenleme. null ise yeni plan.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, BackupPlan existingPlan)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));

            InitializeComponent();
            ApplyIcons();
            _planManager = planManager;
            _sqlBackupService = sqlBackupService;

            if (existingPlan != null)
            {
                _plan = existingPlan;
                _isNew = false;
                Text = Res.Format("PlanEdit_TitleEdit", _plan.PlanName);
            }
            else
            {
                _plan = new BackupPlan();
                _isNew = true;
                Text = Res.Get("PlanEdit_TitleNew");
            }
        }

        private void ApplyIcons()
        {
            const int sz = 16;
            var ph = typeof(Theme.PhosphorIcons);

            _btnSave.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.FloppyDisk, System.Drawing.Color.White, sz);
            _btnSave.Text = "Kaydet";
            _btnSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnCancel.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.XCircle, System.Drawing.Color.White, sz);
            _btnCancel.Text = "Iptal";
            _btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnBrowseLocal.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Folder, Theme.ModernTheme.AccentPrimary, 14);
            _btnBrowseLocal.Text = "";

            _btnTestSql.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Plug, System.Drawing.Color.White, sz);
            _btnTestSql.Text = "Baglantiyi Test Et";
            _btnTestSql.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnAddCloud.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PlusCircle, System.Drawing.Color.White, sz);
            _btnAddCloud.Text = "Ekle";
            _btnAddCloud.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnEditCloud.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PencilSimple, System.Drawing.Color.White, sz);
            _btnEditCloud.Text = "Duzenle";
            _btnEditCloud.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnRemoveCloud.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Trash, System.Drawing.Color.White, sz);
            _btnRemoveCloud.Text = "Kaldir";
            _btnRemoveCloud.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnAddFileSource.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PlusCircle, System.Drawing.Color.White, sz);
            _btnAddFileSource.Text = "Ekle";
            _btnAddFileSource.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnEditFileSource.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PencilSimple, System.Drawing.Color.White, sz);
            _btnEditFileSource.Text = "Duzenle";
            _btnEditFileSource.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnRemoveFileSource.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Trash, System.Drawing.Color.White, sz);
            _btnRemoveFileSource.Text = "Kaldir";
            _btnRemoveFileSource.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PopulateComboBoxes();
            LoadPlanToUi();
        }

        #region ComboBox Population

        private void PopulateComboBoxes()
        {
            // Auth mode
            _cmbAuthMode.Items.Clear();
            _cmbAuthMode.Items.Add(Res.Get("PlanEdit_AuthWindows"));
            _cmbAuthMode.Items.Add(Res.Get("PlanEdit_AuthSql"));

            // Strategy
            _cmbStrategy.Items.Clear();
            _cmbStrategy.Items.Add(Res.Get("PlanEdit_StratFullOnly"));
            _cmbStrategy.Items.Add(Res.Get("PlanEdit_StratFullDiff"));
            _cmbStrategy.Items.Add(Res.Get("PlanEdit_StratFullDiffInc"));

            // Compression algorithm
            _cmbAlgorithm.Items.Clear();
            _cmbAlgorithm.Items.Add(Res.Get("PlanEdit_AlgoLzma2"));
            _cmbAlgorithm.Items.Add(Res.Get("PlanEdit_AlgoLzma"));
            _cmbAlgorithm.Items.Add(Res.Get("PlanEdit_AlgoBzip2"));
            _cmbAlgorithm.Items.Add(Res.Get("PlanEdit_AlgoDeflate"));

            // Compression level
            _cmbLevel.Items.Clear();
            _cmbLevel.Items.Add(Res.Get("PlanEdit_LevelNone"));
            _cmbLevel.Items.Add(Res.Get("PlanEdit_LevelFast"));
            _cmbLevel.Items.Add(Res.Get("PlanEdit_LevelNormal"));
            _cmbLevel.Items.Add(Res.Get("PlanEdit_LevelMax"));
            _cmbLevel.Items.Add(Res.Get("PlanEdit_LevelUltra"));

            // Retention
            _cmbRetention.Items.Clear();
            _cmbRetention.Items.Add(Res.Get("PlanEdit_RetKeepLast"));
            _cmbRetention.Items.Add(Res.Get("PlanEdit_RetDeleteOlder"));
            _cmbRetention.Items.Add(Res.Get("PlanEdit_RetBoth"));
        }

        #endregion

        #region Load Plan → UI

        private void LoadPlanToUi()
        {
            // Tab 1: Genel
            _txtPlanName.Text = _plan.PlanName ?? "";
            _chkEnabled.Checked = _plan.IsEnabled;
            _txtLocalPath.Text = _plan.LocalPath ?? @"D:\Backups\MikroSqlDbYedek";

            // Tab 2: SQL Bağlantı
            _txtServer.Text = _plan.SqlConnection?.Server ?? "";
            _cmbAuthMode.SelectedIndex = (_plan.SqlConnection?.AuthMode ?? SqlAuthMode.Windows) == SqlAuthMode.Windows ? 0 : 1;
            _txtSqlUser.Text = _plan.SqlConnection?.Username ?? "";
            _txtSqlPassword.Text = ""; // Şifre gösterilmez
            _nudTimeout.Value = _plan.SqlConnection?.ConnectionTimeoutSeconds ?? 30;
            UpdateAuthFieldsVisibility();

            // Veritabanı listesi
            if (_plan.Databases != null)
            {
                foreach (var db in _plan.Databases)
                {
                    _clbDatabases.Items.Add(db, true);
                }
            }

            // Tab 3: Strateji
            _cmbStrategy.SelectedIndex = (int)(_plan.Strategy?.Type ?? BackupStrategyType.Full);
            _txtFullCron.Text = _plan.Strategy?.FullSchedule ?? "";
            _txtDiffCron.Text = _plan.Strategy?.DifferentialSchedule ?? "";
            _txtIncrCron.Text = _plan.Strategy?.IncrementalSchedule ?? "";
            _nudAutoPromote.Value = _plan.Strategy?.AutoPromoteToFullAfter ?? 7;
            _chkVerify.Checked = _plan.VerifyAfterBackup;
            UpdateStrategyFieldsVisibility();

            // Tab 4: Sıkıştırma
            _cmbAlgorithm.SelectedIndex = (int)(_plan.Compression?.Algorithm ?? CompressionAlgorithm.Lzma2);
            _cmbLevel.SelectedIndex = (int)(_plan.Compression?.Level ?? CompressionLevel.Ultra);
            _txtArchivePassword.Text = ""; // Şifre gösterilmez

            // Tab 5: Bulut Hedefler
            RefreshCloudTargetList();

            // Tab 6: Retention
            _cmbRetention.SelectedIndex = (int)(_plan.Retention?.Type ?? RetentionPolicyType.KeepLastN);
            _nudKeepLastN.Value = _plan.Retention?.KeepLastN ?? 30;
            _nudDeleteDays.Value = _plan.Retention?.DeleteOlderThanDays ?? 90;
            UpdateRetentionFieldsVisibility();

            // Tab 7: Bildirim
            _chkEmailEnabled.Checked = _plan.Notifications?.EmailEnabled ?? false;
            _txtEmailTo.Text = _plan.Notifications?.EmailTo ?? "";
            _txtSmtpServer.Text = _plan.Notifications?.SmtpServer ?? "";
            _nudSmtpPort.Value = _plan.Notifications?.SmtpPort ?? 587;
            _chkSmtpSsl.Checked = _plan.Notifications?.SmtpUseSsl ?? true;
            _txtSmtpUser.Text = _plan.Notifications?.SmtpUsername ?? "";
            _txtSmtpPassword.Text = ""; // Şifre gösterilmez
            _chkNotifySuccess.Checked = _plan.Notifications?.OnSuccess ?? true;
            _chkNotifyFailure.Checked = _plan.Notifications?.OnFailure ?? true;
            _chkToast.Checked = _plan.Notifications?.ToastEnabled ?? true;
            UpdateEmailFieldsVisibility();

            // Tab 8: Dosya Yedekleme
            var fb = _plan.FileBackup;
            _chkFileBackupEnabled.Checked = fb?.IsEnabled ?? false;
            _txtFileSchedule.Text = fb?.Schedule ?? "";
            RefreshFileSourceList();
            UpdateFileBackupFieldsVisibility();
        }

        #endregion

        #region Save UI → Plan

        private bool SaveUiToPlan()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_txtPlanName.Text))
            {
                _tabControl.SelectedIndex = 0;
                MessageBox.Show(Res.Get("PlanEdit_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPlanName.Focus();
                return false;
            }

            // Tab 1: Genel
            _plan.PlanName = _txtPlanName.Text.Trim();
            _plan.IsEnabled = _chkEnabled.Checked;
            _plan.LocalPath = _txtLocalPath.Text.Trim();

            // Tab 2: SQL Bağlantı
            _plan.SqlConnection.Server = _txtServer.Text.Trim();
            _plan.SqlConnection.AuthMode = _cmbAuthMode.SelectedIndex == 0
                ? SqlAuthMode.Windows : SqlAuthMode.SqlAuthentication;
            _plan.SqlConnection.Username = _txtSqlUser.Text.Trim();
            _plan.SqlConnection.ConnectionTimeoutSeconds = (int)_nudTimeout.Value;

            // SQL şifre — yalnızca değiştirilmişse güncelle
            if (!string.IsNullOrEmpty(_txtSqlPassword.Text))
            {
                _plan.SqlConnection.Password = PasswordProtector.Protect(_txtSqlPassword.Text);
            }

            // Veritabanları
            _plan.Databases.Clear();
            for (int i = 0; i < _clbDatabases.Items.Count; i++)
            {
                if (_clbDatabases.GetItemChecked(i))
                {
                    _plan.Databases.Add(_clbDatabases.Items[i].ToString());
                }
            }

            // Tab 3: Strateji
            _plan.Strategy.Type = (BackupStrategyType)_cmbStrategy.SelectedIndex;
            _plan.Strategy.FullSchedule = _txtFullCron.Text.Trim();
            _plan.Strategy.DifferentialSchedule = _txtDiffCron.Text.Trim();
            _plan.Strategy.IncrementalSchedule = _txtIncrCron.Text.Trim();
            _plan.Strategy.AutoPromoteToFullAfter = (int)_nudAutoPromote.Value;
            _plan.VerifyAfterBackup = _chkVerify.Checked;

            // Tab 4: Sıkıştırma
            _plan.Compression.Algorithm = (CompressionAlgorithm)_cmbAlgorithm.SelectedIndex;
            _plan.Compression.Level = (CompressionLevel)_cmbLevel.SelectedIndex;
            if (!string.IsNullOrEmpty(_txtArchivePassword.Text))
            {
                _plan.Compression.ArchivePassword = PasswordProtector.Protect(_txtArchivePassword.Text);
            }

            // Tab 5: Bulut — zaten _plan.CloudTargets üzerinde çalışılıyor

            // Tab 6: Retention
            _plan.Retention.Type = (RetentionPolicyType)_cmbRetention.SelectedIndex;
            _plan.Retention.KeepLastN = (int)_nudKeepLastN.Value;
            _plan.Retention.DeleteOlderThanDays = (int)_nudDeleteDays.Value;

            // Tab 7: Bildirim
            _plan.Notifications.EmailEnabled = _chkEmailEnabled.Checked;
            _plan.Notifications.EmailTo = _txtEmailTo.Text.Trim();
            _plan.Notifications.SmtpServer = _txtSmtpServer.Text.Trim();
            _plan.Notifications.SmtpPort = (int)_nudSmtpPort.Value;
            _plan.Notifications.SmtpUseSsl = _chkSmtpSsl.Checked;
            _plan.Notifications.SmtpUsername = _txtSmtpUser.Text.Trim();
            _plan.Notifications.OnSuccess = _chkNotifySuccess.Checked;
            _plan.Notifications.OnFailure = _chkNotifyFailure.Checked;
            _plan.Notifications.ToastEnabled = _chkToast.Checked;
            if (!string.IsNullOrEmpty(_txtSmtpPassword.Text))
            {
                _plan.Notifications.SmtpPassword = PasswordProtector.Protect(_txtSmtpPassword.Text);
            }

            // Tab 8: Dosya Yedekleme
            if (_plan.FileBackup == null)
                _plan.FileBackup = new FileBackupConfig();
            _plan.FileBackup.IsEnabled = _chkFileBackupEnabled.Checked;
            _plan.FileBackup.Schedule = _txtFileSchedule.Text.Trim();

            // Metadata
            _plan.LastModifiedAt = DateTime.UtcNow;
            if (_isNew)
            {
                _plan.CreatedAt = DateTime.UtcNow;
                _plan.SchemaVersion = 1;
            }

            return true;
        }

        #endregion

        #region Button Events

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!SaveUiToPlan()) return;

            try
            {
                _planManager.SavePlan(_plan);
                Log.Information("Plan kaydedildi: {PlanName} ({PlanId})", _plan.PlanName, _plan.PlanId);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan kaydedilirken hata: {PlanId}", _plan.PlanId);
                MessageBox.Show(Res.Format("PlanEdit_SaveError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private async void OnTestSqlConnectionClick(object sender, EventArgs e)
        {
            _btnTestSql.Enabled = false;
            _btnTestSql.Text = Res.Get("PlanEdit_Testing");

            try
            {
                var connInfo = new SqlConnectionInfo
                {
                    Server = _txtServer.Text.Trim(),
                    AuthMode = _cmbAuthMode.SelectedIndex == 0 ? SqlAuthMode.Windows : SqlAuthMode.SqlAuthentication,
                    Username = _txtSqlUser.Text.Trim(),
                    ConnectionTimeoutSeconds = (int)_nudTimeout.Value
                };

                if (!string.IsNullOrEmpty(_txtSqlPassword.Text))
                {
                    connInfo.Password = PasswordProtector.Protect(_txtSqlPassword.Text);
                }
                else if (!string.IsNullOrEmpty(_plan.SqlConnection?.Password))
                {
                    connInfo.Password = _plan.SqlConnection.Password;
                }

                var sqlService = _sqlBackupService;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(connInfo.ConnectionTimeoutSeconds)))
                {
                    var isConnected = await sqlService.TestConnectionAsync(connInfo, cts.Token);

                    if (isConnected)
                    {
                        MessageBox.Show(Res.Get("PlanEdit_ConnSuccess"), Res.Get("PlanEdit_ConnSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Veritabanı listesini otomatik yükle
                        await LoadDatabaseListAsync(connInfo);
                    }
                    else
                    {
                        MessageBox.Show(Res.Get("PlanEdit_ConnFailed"), Res.Get("PlanEdit_ConnSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SQL bağlantı testi hatası.");
                MessageBox.Show(Res.Format("PlanEdit_ConnError", ex.Message), Res.Get("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnTestSql.Enabled = true;
                _btnTestSql.Text = Res.Get("PlanEdit_TestBtn");
            }
        }

        private async Task LoadDatabaseListAsync(SqlConnectionInfo connInfo)
        {
            try
            {
                var sqlService = _sqlBackupService;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var databases = await sqlService.ListDatabasesAsync(connInfo, cts.Token);

                    _clbDatabases.Items.Clear();
                    foreach (var db in databases.Where(d => !d.IsSystemDb).OrderBy(d => d.Name))
                    {
                        bool isChecked = _plan.Databases?.Contains(db.Name) ?? false;
                        _clbDatabases.Items.Add(Res.Format("PlanEdit_DbSizeFormat", db.Name, db.SizeInMb), isChecked);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Veritabanı listesi yüklenemedi.");
            }
        }

        private void OnBrowseLocalPathClick(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("PlanEdit_BrowseLocalPath");
                fbd.SelectedPath = _txtLocalPath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtLocalPath.Text = fbd.SelectedPath;
                }
            }
        }

        #endregion

        #region Cloud Target Management

        private void RefreshCloudTargetList()
        {
            _lvCloudTargets.Items.Clear();
            if (_plan.CloudTargets == null) return;

            foreach (var target in _plan.CloudTargets)
            {
                var item = new ListViewItem(new[]
                {
                    target.DisplayName ?? target.Type.ToString(),
                    target.Type.ToString(),
                    target.IsEnabled ? Res.Get("Active") : Res.Get("Passive")
                });
                item.Tag = target;
                _lvCloudTargets.Items.Add(item);
            }
        }

        private void OnAddCloudTarget(object sender, EventArgs e)
        {
            using (var dialog = new CloudTargetEditDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _plan.CloudTargets.Add(dialog.Target);
                    RefreshCloudTargetList();
                }
            }
        }

        private void OnEditCloudTarget(object sender, EventArgs e)
        {
            if (_lvCloudTargets.SelectedItems.Count == 0) return;
            var target = _lvCloudTargets.SelectedItems[0].Tag as CloudTargetConfig;
            if (target == null) return;

            using (var dialog = new CloudTargetEditDialog(target))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var index = _plan.CloudTargets.IndexOf(target);
                    if (index >= 0)
                    {
                        _plan.CloudTargets[index] = dialog.Target;
                    }
                    RefreshCloudTargetList();
                }
            }
        }

        private void OnRemoveCloudTarget(object sender, EventArgs e)
        {
            if (_lvCloudTargets.SelectedItems.Count == 0) return;
            var target = _lvCloudTargets.SelectedItems[0].Tag as CloudTargetConfig;
            if (target == null) return;

            var result = MessageBox.Show(
                Res.Format("PlanEdit_RemoveTargetConfirm", target.DisplayName),
                Res.Get("PlanEdit_RemoveTargetTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _plan.CloudTargets.Remove(target);
                RefreshCloudTargetList();
            }
        }

        #endregion

        #region File Backup Source Management

        private void RefreshFileSourceList()
        {
            _lvFileSources.Items.Clear();
            if (_plan.FileBackup?.Sources == null) return;

            foreach (var source in _plan.FileBackup.Sources)
            {
                var item = new ListViewItem(new[]
                {
                    source.SourceName ?? "—",
                    source.SourcePath ?? "—",
                    source.UseVss ? Res.Get("YesLabel") : Res.Get("NoLabel"),
                    source.IsEnabled ? Res.Get("Active") : Res.Get("Passive")
                });
                item.Tag = source;
                _lvFileSources.Items.Add(item);
            }
        }

        private void OnAddFileSource(object sender, EventArgs e)
        {
            using (var dialog = new FileBackupSourceEditDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (_plan.FileBackup == null)
                        _plan.FileBackup = new FileBackupConfig();
                    _plan.FileBackup.Sources.Add(dialog.Source);
                    RefreshFileSourceList();
                }
            }
        }

        private void OnEditFileSource(object sender, EventArgs e)
        {
            if (_lvFileSources.SelectedItems.Count == 0) return;
            var source = _lvFileSources.SelectedItems[0].Tag as FileBackupSource;
            if (source == null) return;

            using (var dialog = new FileBackupSourceEditDialog(source))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var index = _plan.FileBackup.Sources.IndexOf(source);
                    if (index >= 0)
                    {
                        _plan.FileBackup.Sources[index] = dialog.Source;
                    }
                    RefreshFileSourceList();
                }
            }
        }

        private void OnRemoveFileSource(object sender, EventArgs e)
        {
            if (_lvFileSources.SelectedItems.Count == 0) return;
            var source = _lvFileSources.SelectedItems[0].Tag as FileBackupSource;
            if (source == null) return;

            _plan.FileBackup.Sources.Remove(source);
            RefreshFileSourceList();
        }

        #endregion

        #region Visibility Helpers

        private void OnAuthModeChanged(object sender, EventArgs e) => UpdateAuthFieldsVisibility();
        private void OnStrategyChanged(object sender, EventArgs e) => UpdateStrategyFieldsVisibility();
        private void OnRetentionChanged(object sender, EventArgs e) => UpdateRetentionFieldsVisibility();
        private void OnEmailEnabledChanged(object sender, EventArgs e) => UpdateEmailFieldsVisibility();
        private void OnFileBackupEnabledChanged(object sender, EventArgs e) => UpdateFileBackupFieldsVisibility();

        private void UpdateAuthFieldsVisibility()
        {
            bool isSqlAuth = _cmbAuthMode.SelectedIndex == 1;
            _lblSqlUser.Visible = isSqlAuth;
            _txtSqlUser.Visible = isSqlAuth;
            _lblSqlPassword.Visible = isSqlAuth;
            _txtSqlPassword.Visible = isSqlAuth;
        }

        private void UpdateStrategyFieldsVisibility()
        {
            int idx = _cmbStrategy.SelectedIndex;
            _lblDiffCron.Visible = idx >= 1;
            _txtDiffCron.Visible = idx >= 1;
            _lblIncrCron.Visible = idx >= 2;
            _txtIncrCron.Visible = idx >= 2;
        }

        private void UpdateRetentionFieldsVisibility()
        {
            int idx = _cmbRetention.SelectedIndex;
            _lblKeepLastN.Visible = idx == 0 || idx == 2;
            _nudKeepLastN.Visible = idx == 0 || idx == 2;
            _lblDeleteDays.Visible = idx == 1 || idx == 2;
            _nudDeleteDays.Visible = idx == 1 || idx == 2;
        }

        private void UpdateEmailFieldsVisibility()
        {
            bool enabled = _chkEmailEnabled.Checked;
            _pnlSmtp.Enabled = enabled;
        }

        private void UpdateFileBackupFieldsVisibility()
        {
            bool enabled = _chkFileBackupEnabled.Checked;
            _pnlFileBackup.Enabled = enabled;
        }

        #endregion
    }
}
