using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// CheckedListBox için veritabanı adını ve görüntüleme metnini ayrı tutan yardımcı.
    /// </summary>
    internal sealed class DatabaseListItem
    {
        public string Name { get; }
        public string DisplayText { get; }

        public DatabaseListItem(string name, string displayText)
        {
            Name = name;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Plan ekleme/düzenleme sihirbazı — dinamik adımlı wizard.
    /// Yerel mod: Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Bildirim (5 adım).
    /// Bulut mod: Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Hedefler → Bildirim (6 adım).
    /// </summary>
    public partial class PlanEditForm : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<PlanEditForm>();
        private readonly IPlanManager _planManager;
        private readonly ISqlBackupService _sqlBackupService;
        private readonly IAppSettingsManager _settingsManager;
        private readonly BackupPlan _plan;
        private readonly bool _isNew;
        private int _currentStep;
        private bool _connectionTested;

        /// <summary>Yeni plan oluşturma.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, IAppSettingsManager settingsManager)
            : this(planManager, sqlBackupService, settingsManager, null) { }

        /// <summary>Kaydedilen planı döndürür (DialogResult.OK sonrası geçerlidir).</summary>
        public BackupPlan SavedPlan => _plan;

        /// <summary>Mevcut planı düzenleme. null ise yeni plan.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, IAppSettingsManager settingsManager, BackupPlan existingPlan)
        {
            ArgumentNullException.ThrowIfNull(planManager);
            ArgumentNullException.ThrowIfNull(sqlBackupService);
            ArgumentNullException.ThrowIfNull(settingsManager);

            InitializeComponent();
            ApplyIcons();
            _planManager = planManager;
            _sqlBackupService = sqlBackupService;
            _settingsManager = settingsManager;

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
            // Navigasyon — Geri/İleri metin okları Designer.cs'de tanımlı, ikon yok
            _btnSave.Image = LoadIcon("Save_16x16.png");
            _btnSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _btnCancel.Image = LoadIcon("Cancel_16x16.png");
            _btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            // Step 1
            _btnBrowseLocal.Image = LoadIcon("Open_16x16.png");
            _btnTestSql.Image = LoadIcon("ForceTesting_16x16.png");

            // Step 2
            _btnRefreshDatabases.Image = LoadIcon("Refresh_16x16.png");
            _btnAddFileSource.Image = LoadIcon("Add_16x16.png");
            _btnEditFileSource.Image = LoadIcon("Edit_16x16.png");
            _btnRemoveFileSource.Image = LoadIcon("Delete_16x16.png");

            // Step 5
            _btnAddCloud.Image = LoadIcon("Add_16x16.png");
            _btnEditCloud.Image = LoadIcon("Edit_16x16.png");
            _btnRemoveCloud.Image = LoadIcon("Delete_16x16.png");
        }

        private static System.Drawing.Image? LoadIcon(string name)
        {
            var asm = typeof(PlanEditForm).Assembly;
            string resourceName = $"KoruMsSqlYedek.Win.Resources.Icons.{name}";
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) return null;
            return System.Drawing.Image.FromStream(stream);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PopulateComboBoxes();
            LoadPlanToUi();
            _connectionTested = !_isNew && _plan.Databases?.Count > 0;
            RebuildActiveSteps();
            ShowStep(0);
        }

        #region Wizard Navigation

        private void ShowStep(int activeIndex)
        {
            _currentStep = activeIndex;
            int panelIndex = _activeSteps[activeIndex];

            for (int i = 0; i < _stepPanels.Length; i++)
            {
                _stepPanels[i].Visible = i == panelIndex;
            }

            RebuildStepIndicator();

            // Navigation buttons
            bool isLastStep = activeIndex == _activeSteps.Count - 1;
            _btnBack.Visible = activeIndex > 0;
            _btnNext.Visible = !isLastStep;
            _btnSave.Visible = true;
            _btnSave.Text = isLastStep ? "Kaydet" : "Kaydet & Çık";
        }

        /// <summary>Aktif adımları hesaplar. Hedefler adımı her zaman dahildir.</summary>
        private void RebuildActiveSteps()
        {
            _activeSteps = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5 };
        }

        /// <summary>Aktif adımlara göre üst bar göstergesini yeniden çizer.</summary>
        private void RebuildStepIndicator()
        {
            string[] allTitles = { "Bağlantı", "Kaynaklar", "Zamanlama", "Sıkıştırma", "Hedefler", "Bildirim" };
            int count = _activeSteps.Count;
            int stepW = count <= 5 ? 124 : 103;
            int stepStartX = 6;

            // Tüm dotları/label'ları gizle
            for (int i = 0; i < _stepDots.Length; i++)
            {
                _stepDots[i].Visible = false;
                _stepLabels[i].Visible = false;
            }

            // Aktif adımları göster ve konumlandır
            for (int i = 0; i < count; i++)
            {
                int panelIdx = _activeSteps[i];

                _stepDots[i].Visible = true;
                _stepDots[i].Location = new System.Drawing.Point(stepStartX + i * stepW, 6);
                _stepDots[i].Size = new System.Drawing.Size(24, 24);

                _stepLabels[i].Visible = true;
                _stepLabels[i].Text = allTitles[panelIdx];
                _stepLabels[i].Location = new System.Drawing.Point(stepStartX + i * stepW, 32);
                _stepLabels[i].Size = new System.Drawing.Size(stepW - 6, 18);

                if (i < _currentStep)
                {
                    _stepDots[i].ForeColor = Theme.ModernTheme.AccentPrimary;
                    _stepDots[i].BackColor = System.Drawing.Color.Transparent;
                    _stepLabels[i].ForeColor = Theme.ModernTheme.AccentPrimary;
                    _stepDots[i].Text = "\u2713";
                }
                else if (i == _currentStep)
                {
                    _stepDots[i].ForeColor = System.Drawing.Color.White;
                    _stepDots[i].BackColor = Theme.ModernTheme.AccentPrimary;
                    _stepLabels[i].ForeColor = Theme.ModernTheme.TextPrimary;
                    _stepDots[i].Text = (i + 1).ToString();
                }
                else
                {
                    _stepDots[i].ForeColor = Theme.ModernTheme.TextDisabled;
                    _stepDots[i].BackColor = System.Drawing.Color.Transparent;
                    _stepLabels[i].ForeColor = Theme.ModernTheme.TextDisabled;
                    _stepDots[i].Text = (i + 1).ToString();
                }
            }
        }

        private void OnBackClick(object sender, EventArgs e)
        {
            if (_currentStep > 0)
                ShowStep(_currentStep - 1);
        }

        private async void OnNextClick(object sender, EventArgs e)
        {
            if (!ValidateCurrentStep())
                return;

            // Adım 1'den (Bağlantı panel=0) geçerken otomatik DB listesi yükle
            if (_activeSteps[_currentStep] == 0 && _clbDatabases.Items.Count == 0)
            {
                await TryLoadDatabaseListAsync();
            }

            if (_currentStep < _activeSteps.Count - 1)
                ShowStep(_currentStep + 1);
        }

        private bool ValidateCurrentStep()
        {
            int panelIndex = _activeSteps[_currentStep];
            switch (panelIndex)
            {
                case 0:
                    if (string.IsNullOrWhiteSpace(_txtPlanName.Text))
                    {
                        Theme.ModernMessageBox.Show(Res.Get("PlanEdit_NameRequired"), Res.Get("ValidationError"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _txtPlanName.Focus();
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(_txtServer.Text))
                    {
                        Theme.ModernMessageBox.Show(Res.Get("PlanEdit_ServerRequired"), Res.Get("ValidationError"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _txtServer.Focus();
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private async Task TryLoadDatabaseListAsync()
        {
            var connInfo = BuildCurrentConnInfo();

            try
            {
                _btnNext.Enabled = false;
                _btnNext.Text = "Y\u00fckleniyor...";

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(connInfo.ConnectionTimeoutSeconds)))
                {
                    var isConnected = await _sqlBackupService.TestConnectionAsync(connInfo, cts.Token);
                    if (isConnected)
                    {
                        _connectionTested = true;
                        await LoadDatabaseListAsync(connInfo);
                    }
                    else
                    {
                        Theme.ModernMessageBox.Show(Res.Get("PlanEdit_ConnFailed"), Res.Get("Warning"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Otomatik DB listesi yüklenemedi.");
            }
            finally
            {
                _btnNext.Enabled = true;
                _btnNext.Text = "\u0130leri";
            }
        }

        private SqlConnectionInfo BuildCurrentConnInfo()
        {
            var connInfo = new SqlConnectionInfo
            {
                Server = _txtServer.Text.Trim(),
                AuthMode = _cmbAuthMode.SelectedIndex == 0 ? SqlAuthMode.Windows : SqlAuthMode.SqlAuthentication,
                Username = _txtSqlUser.Text.Trim(),
                ConnectionTimeoutSeconds = (int)_nudTimeout.Value,
                TrustServerCertificate = _chkTrustCert.Checked
            };

            if (!string.IsNullOrEmpty(_txtSqlPassword.Text))
            {
                connInfo.Password = PasswordProtector.Protect(_txtSqlPassword.Text);
            }
            else if (!string.IsNullOrEmpty(_plan.SqlConnection?.Password))
            {
                connInfo.Password = _plan.SqlConnection.Password;
            }

            return connInfo;
        }

        private void OnSelectAllChanged(object sender, EventArgs e)
        {
            bool check = _chkSelectAll.Checked;
            for (int i = 0; i < _clbDatabases.Items.Count; i++)
            {
                _clbDatabases.SetItemChecked(i, check);
            }
        }

        private async void OnRefreshDatabasesClick(object sender, EventArgs e)
        {
            var connInfo = BuildCurrentConnInfo();
            await LoadDatabaseListAsync(connInfo);
        }

        #endregion

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

            // File backup strategy
            _cmbFileStrategy.Items.Clear();
            _cmbFileStrategy.Items.Add(Res.Get("PlanEdit_FileStratFull"));
            _cmbFileStrategy.Items.Add(Res.Get("PlanEdit_FileStratDiff"));
            _cmbFileStrategy.Items.Add(Res.Get("PlanEdit_FileStratInc"));

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

            // Rapor sıklığı
            _cmbReportFreq.Items.Clear();
            _cmbReportFreq.Items.Add("Günlük");
            _cmbReportFreq.Items.Add("Haftalık");
            _cmbReportFreq.Items.Add("Aylık");

            // SMTP Profilleri
            var settings = _settingsManager.Load();
            _cmbSmtpProfile.Items.Clear();
            _cmbSmtpProfile.Items.Add(new SmtpProfile { Id = string.Empty, DisplayName = Res.Get("PlanEdit_SmtpNoProfile") ?? "(Profil seçin)" });
            foreach (var profile in settings.SmtpProfiles)
                _cmbSmtpProfile.Items.Add(profile);
        }

        #endregion

        #region Load Plan → UI

        private void LoadPlanToUi()
        {
            // Adım 1: Plan Bilgileri + SQL Bağlantı
            _txtPlanName.Text = _plan.PlanName ?? "";
            _chkEnabled.Checked = _plan.IsEnabled;
            _txtLocalPath.Text = _plan.LocalPath ?? @"D:\Backups\KoruMsSqlYedek";
            _txtServer.Text = _plan.SqlConnection?.Server ?? "";
            _cmbAuthMode.SelectedIndex = (_plan.SqlConnection?.AuthMode ?? SqlAuthMode.Windows) == SqlAuthMode.Windows ? 0 : 1;
            _txtSqlUser.Text = _plan.SqlConnection?.Username ?? "";
            _txtSqlPassword.Text = "";
            _nudTimeout.Value = _plan.SqlConnection?.ConnectionTimeoutSeconds ?? 30;
            _chkTrustCert.Checked = _plan.SqlConnection?.TrustServerCertificate ?? true;
            UpdateAuthFieldsVisibility();

            // Adım 2: Kaynaklar (DB + Dosya)
            if (_plan.Databases != null)
            {
                foreach (var db in _plan.Databases)
                {
                    _clbDatabases.Items.Add(new DatabaseListItem(db, db), true);
                }
            }
            var fb = _plan.FileBackup;
            _chkFileBackupEnabled.Checked = fb?.IsEnabled ?? false;
            RefreshFileSourceList();
            UpdateFileBackupFieldsVisibility();

            // Adım 3: Zamanlama
            _cmbStrategy.SelectedIndex = (int)(_plan.Strategy?.Type ?? BackupStrategyType.Full);
            _cronFull.SetCronExpression(_plan.Strategy?.FullSchedule ?? "");
            _cronDiff.SetCronExpression(_plan.Strategy?.DifferentialSchedule ?? "");
            _cronIncr.SetCronExpression(_plan.Strategy?.IncrementalSchedule ?? "");
            _nudAutoPromote.Value = _plan.Strategy?.AutoPromoteToFullAfter ?? 7;
            _chkVerify.Checked = _plan.VerifyAfterBackup;
            _cmbFileStrategy.SelectedIndex = (int)(fb?.Strategy ?? FileBackupStrategy.Full);
            _cronFileSchedule.SetCronExpression(fb?.Schedule ?? "");
            UpdateStrategyFieldsVisibility();
            UpdateFileScheduleVisibility();

            // Adım 4: Sıkıştırma + Saklama
            _cmbAlgorithm.SelectedIndex = (int)(_plan.Compression?.Algorithm ?? CompressionAlgorithm.Lzma2);
            _cmbLevel.SelectedIndex = (int)(_plan.Compression?.Level ?? CompressionLevel.Ultra);
            _txtArchivePassword.Text = "";
            _cmbRetention.SelectedIndex = (int)(_plan.Retention?.Type ?? RetentionPolicyType.KeepLastN);
            _nudKeepLastN.Value = _plan.Retention?.KeepLastN ?? 30;
            _nudDeleteDays.Value = _plan.Retention?.DeleteOlderThanDays ?? 90;
            UpdateRetentionFieldsVisibility();
            _chkProtectPlan.Checked = _plan.HasPlanPassword;
            _txtPlanPassword.Text = "";
            _txtPlanPassword.Visible = _plan.HasPlanPassword;
            _txtRecoveryPassword.Text = "";
            _txtRecoveryPassword.Visible = _plan.HasPlanPassword;

            // Adım 5: Hedefler
            RefreshCloudTargetList();

            // Adım 6: Bildirim + Rapor
            _chkEmailEnabled.Checked = _plan.Notifications?.EmailEnabled ?? false;
            string profileId = _plan.Notifications?.SmtpProfileId ?? string.Empty;
            _cmbSmtpProfile.SelectedIndex = 0;
            for (int i = 0; i < _cmbSmtpProfile.Items.Count; i++)
            {
                if (_cmbSmtpProfile.Items[i] is SmtpProfile p && p.Id == profileId)
                {
                    _cmbSmtpProfile.SelectedIndex = i;
                    break;
                }
            }
            _chkNotifySuccess.Checked = _plan.Notifications?.OnSuccess ?? true;
            _chkNotifyFailure.Checked = _plan.Notifications?.OnFailure ?? true;
            _chkToast.Checked = _plan.Notifications?.ToastEnabled ?? true;
            UpdateEmailFieldsVisibility();

            var rpt = _plan.Reporting;
            _chkReportEnabled.Checked = rpt?.IsEnabled ?? false;
            _cmbReportFreq.SelectedIndex = (int)(rpt?.Frequency ?? ReportFrequency.Weekly);
            _txtReportEmail.Text = rpt?.EmailTo ?? "";
            _nudReportHour.Value = rpt?.SendHour ?? 8;
            UpdateReportFieldsVisibility();
        }

        #endregion

        #region Save UI → Plan

        private bool SaveUiToPlan()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_txtPlanName.Text))
            {
                ShowStep(0);
                Theme.ModernMessageBox.Show(Res.Get("PlanEdit_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPlanName.Focus();
                return false;
            }

            // Adım 1: Bağlantı
            _plan.PlanName = _txtPlanName.Text.Trim();
            _plan.IsEnabled = _chkEnabled.Checked;
            _plan.LocalPath = _txtLocalPath.Text.Trim();
            _plan.SqlConnection.Server = _txtServer.Text.Trim();
            _plan.SqlConnection.AuthMode = _cmbAuthMode.SelectedIndex == 0
                ? SqlAuthMode.Windows : SqlAuthMode.SqlAuthentication;
            _plan.SqlConnection.Username = _txtSqlUser.Text.Trim();
            _plan.SqlConnection.ConnectionTimeoutSeconds = (int)_nudTimeout.Value;
            _plan.SqlConnection.TrustServerCertificate = _chkTrustCert.Checked;
            if (!string.IsNullOrEmpty(_txtSqlPassword.Text))
            {
                _plan.SqlConnection.Password = PasswordProtector.Protect(_txtSqlPassword.Text);
            }

            // Adım 2: Kaynaklar
            _plan.Databases.Clear();
            for (int i = 0; i < _clbDatabases.Items.Count; i++)
            {
                if (_clbDatabases.GetItemChecked(i))
                {
                    var item = _clbDatabases.Items[i];
                    string dbName = item is DatabaseListItem dbItem ? dbItem.Name : item.ToString();
                    _plan.Databases.Add(dbName);
                }
            }
            if (_plan.FileBackup == null)
                _plan.FileBackup = new FileBackupConfig();
            _plan.FileBackup.IsEnabled = _chkFileBackupEnabled.Checked;

            // Adım 3: Zamanlama
            _plan.Strategy.Type = (BackupStrategyType)_cmbStrategy.SelectedIndex;
            _plan.Strategy.FullSchedule = _cronFull.GetCronExpression();
            _plan.Strategy.DifferentialSchedule = _cronDiff.GetCronExpression();
            _plan.Strategy.IncrementalSchedule = _cronIncr.GetCronExpression();
            _plan.Strategy.AutoPromoteToFullAfter = (int)_nudAutoPromote.Value;
            _plan.VerifyAfterBackup = _chkVerify.Checked;
            _plan.FileBackup.Strategy = (FileBackupStrategy)_cmbFileStrategy.SelectedIndex;
            _plan.FileBackup.Schedule = _cronFileSchedule.GetCronExpression();

            // Adım 4: Sıkıştırma + Saklama
            _plan.Compression.Algorithm = (CompressionAlgorithm)_cmbAlgorithm.SelectedIndex;
            _plan.Compression.Level = (CompressionLevel)_cmbLevel.SelectedIndex;
            if (!string.IsNullOrEmpty(_txtArchivePassword.Text))
            {
                _plan.Compression.ArchivePassword = PasswordProtector.Protect(_txtArchivePassword.Text);
            }
            _plan.Retention.Type = (RetentionPolicyType)_cmbRetention.SelectedIndex;
            _plan.Retention.KeepLastN = (int)_nudKeepLastN.Value;
            _plan.Retention.DeleteOlderThanDays = (int)_nudDeleteDays.Value;

            // Plan şifresi
            if (!_chkProtectPlan.Checked)
            {
                _plan.PasswordHash = null;
                _plan.RecoveryPasswordHash = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_txtPlanPassword.Text))
                {
                    _plan.PasswordHash = PlanPasswordHelper.HashPassword(_txtPlanPassword.Text);
                }
                // checkbox işaretli ama alan boş → mevcut hash korunur

                if (!string.IsNullOrWhiteSpace(_txtRecoveryPassword.Text))
                {
                    _plan.RecoveryPasswordHash = PlanPasswordHelper.HashPassword(_txtRecoveryPassword.Text);
                }
                // kurtarma alanı boş → mevcut recovery hash korunur
            }

            // Adım 5: Hedefler — zaten _plan.CloudTargets üzerinde çalışılıyor

            // Adım 6: Bildirim + Rapor
            _plan.Notifications.EmailEnabled = _chkEmailEnabled.Checked;
            _plan.Notifications.SmtpProfileId = (_cmbSmtpProfile.SelectedItem as SmtpProfile)?.Id;
            _plan.Notifications.OnSuccess = _chkNotifySuccess.Checked;
            _plan.Notifications.OnFailure = _chkNotifyFailure.Checked;
            _plan.Notifications.ToastEnabled = _chkToast.Checked;
            if (_plan.Reporting == null)
                _plan.Reporting = new ReportingConfig();
            _plan.Reporting.IsEnabled = _chkReportEnabled.Checked;
            _plan.Reporting.Frequency = (ReportFrequency)_cmbReportFreq.SelectedIndex;
            _plan.Reporting.EmailTo = _txtReportEmail.Text.Trim();
            _plan.Reporting.SendHour = (int)_nudReportHour.Value;

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
                Theme.ModernMessageBox.Show(Res.Format("PlanEdit_SaveError", ex.Message),
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
                var connInfo = BuildCurrentConnInfo();

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(connInfo.ConnectionTimeoutSeconds)))
                {
                    var isConnected = await _sqlBackupService.TestConnectionAsync(connInfo, cts.Token);

                    if (isConnected)
                    {
                        _connectionTested = true;
                        Theme.ModernMessageBox.Show(Res.Get("PlanEdit_ConnSuccess"), Res.Get("PlanEdit_ConnSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        await LoadDatabaseListAsync(connInfo);
                    }
                    else
                    {
                        Theme.ModernMessageBox.Show(Res.Get("PlanEdit_ConnFailed"), Res.Get("PlanEdit_ConnSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SQL bağlantı testi hatası.");
                Theme.ModernMessageBox.Show(Res.Format("PlanEdit_ConnError", ex.Message), Res.Get("Error"),
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
                        string displayText = Res.Format("PlanEdit_DbSizeFormat", db.Name, db.SizeInMb);
                        _clbDatabases.Items.Add(new DatabaseListItem(db.Name, displayText), isChecked);
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
            var appSettings = _settingsManager.Load();
            using (var dialog = new CloudTargetEditDialog(appSettings, _settingsManager))
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

            var appSettings = _settingsManager.Load();
            using (var dialog = new CloudTargetEditDialog(appSettings, _settingsManager, target))
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

            var result = Theme.ModernMessageBox.Show(
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

        private void OnOpenSmtpSettingsClick(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            Theme.ModernMessageBox.Show(
                Res.Get("PlanEdit_SmtpGoToSettings") ?? "SMTP profillerini yönetmek için ana pencereden\nAyarlar \u003e E-posta (SMTP) sekmesini açın.",
                Res.Get("Info") ?? "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void OnReportEnabledChanged(object sender, EventArgs e) => UpdateReportFieldsVisibility();
        private void OnFileBackupEnabledChanged(object sender, EventArgs e)
        {
            UpdateFileBackupFieldsVisibility();
            UpdateFileScheduleVisibility();
        }

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
            _cronDiff.Visible = idx >= 1;
            _lblIncrCron.Visible = idx >= 2;
            _cronIncr.Visible = idx >= 2;
        }

        private void UpdateRetentionFieldsVisibility()
        {
            int idx = _cmbRetention.SelectedIndex;
            _lblKeepLastN.Visible = idx == 0 || idx == 2;
            _nudKeepLastN.Visible = idx == 0 || idx == 2;
            _lblDeleteDays.Visible = idx == 1 || idx == 2;
            _nudDeleteDays.Visible = idx == 1 || idx == 2;
        }

        private void OnProtectPlanChanged(object? sender, EventArgs e)
        {
            _txtPlanPassword.Visible = _chkProtectPlan.Checked;
            _txtRecoveryPassword.Visible = _chkProtectPlan.Checked;
            if (_chkProtectPlan.Checked)
            {
                _txtPlanPassword.Focus();
            }
            else
            {
                _txtPlanPassword.Text = "";
                _txtRecoveryPassword.Text = "";
            }
        }

        private void UpdateEmailFieldsVisibility()
        {
            bool enabled = _chkEmailEnabled.Checked;
            _lblSmtpProfile.Enabled = enabled;
            _cmbSmtpProfile.Enabled = enabled;
            _lnkOpenSmtpSettings.Enabled = enabled;
            _chkNotifySuccess.Enabled = enabled;
            _chkNotifyFailure.Enabled = enabled;
        }

        private void UpdateReportFieldsVisibility()
        {
            bool enabled = _chkReportEnabled.Checked;
            _lblReportFreq.Enabled = enabled;
            _cmbReportFreq.Enabled = enabled;
            _lblReportEmail.Enabled = enabled;
            _txtReportEmail.Enabled = enabled;
            _lblReportHour.Enabled = enabled;
            _nudReportHour.Enabled = enabled;
        }

        private void UpdateFileBackupFieldsVisibility()
        {
            bool enabled = _chkFileBackupEnabled.Checked;
            _pnlFileBackup.Enabled = enabled;
        }

        private void UpdateFileScheduleVisibility()
        {
            bool enabled = _chkFileBackupEnabled.Checked;
            _lblStep3FileSep.Visible = enabled;
            _lblStep3FileSchedHeader.Visible = enabled;
            _lblFileStrategy.Visible = enabled;
            _cmbFileStrategy.Visible = enabled;
            _lblFileSchedule.Visible = enabled;
            _cronFileSchedule.Visible = enabled;
        }

        #endregion
    }
}
