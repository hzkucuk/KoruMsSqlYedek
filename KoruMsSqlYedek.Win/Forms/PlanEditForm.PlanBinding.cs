using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    partial class PlanEditForm
    {
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

            // Retention şablonları
            _cmbRetentionTemplate.Items.Clear();
            _cmbRetentionTemplate.Items.Add("Minimal  (Full×3, Diff×7, Log×14, Files×5)");
            _cmbRetentionTemplate.Items.Add("Standard  (Full×7, Diff×14, Log×30, Files×14)  ★");
            _cmbRetentionTemplate.Items.Add("Extended  (Full×14, Diff×30, Log×90, Files×30)");
            _cmbRetentionTemplate.Items.Add("GFS  (Grandfather-Father-Son rotasyonu)");
            _cmbRetentionTemplate.Items.Add("Özel  (elle ayarla)");

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
            UpdateStrategyFieldsVisibility();

            // Adım 4: Sıkıştırma + Saklama
            _cmbAlgorithm.SelectedIndex = (int)(_plan.Compression?.Algorithm ?? CompressionAlgorithm.Lzma2);
            _cmbLevel.SelectedIndex = (int)(_plan.Compression?.Level ?? CompressionLevel.Ultra);
            _txtArchivePassword.Text = "";

            // Retention şablonu yükle
            if (_plan.RetentionScheme != null && _plan.RetentionScheme.Template != RetentionTemplateType.Custom)
            {
                // Şablon indeksi: Minimal=0, Standard=1, Extended=2, GFS=3
                _cmbRetentionTemplate.SelectedIndex = (int)_plan.RetentionScheme.Template - 1;
            }
            else
            {
                // Özel mod (index=4)
                _cmbRetentionTemplate.SelectedIndex = 4;
            }
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
            _plan.FileBackup.Schedule = null;

            // Adım 4: Sıkıştırma + Saklama
            _plan.Compression.Algorithm = (CompressionAlgorithm)_cmbAlgorithm.SelectedIndex;
            _plan.Compression.Level = (CompressionLevel)_cmbLevel.SelectedIndex;
            if (!string.IsNullOrEmpty(_txtArchivePassword.Text))
            {
                _plan.Compression.ArchivePassword = PasswordProtector.Protect(_txtArchivePassword.Text);
            }

            // Retention: şablon mu yoksa özel mi?
            int templateIdx = _cmbRetentionTemplate.SelectedIndex;
            if (templateIdx >= 0 && templateIdx < 4)
            {
                // Hazır şablon seçili — RetentionScheme oluştur
                var templateType = (RetentionTemplateType)(templateIdx + 1); // Minimal=1..GFS=4
                _plan.RetentionScheme = RetentionTemplates.FromType(templateType);
                // Fallback Retention'ı da güncelle (eski servis/test uyumluluğu)
                _plan.Retention.Type = RetentionPolicyType.KeepLastN;
                _plan.Retention.KeepLastN = _plan.RetentionScheme.SqlFull.KeepLastN;
            }
            else
            {
                // Özel mod — eski tek-policy davranışı
                _plan.RetentionScheme = null;
                _plan.Retention.Type = (RetentionPolicyType)_cmbRetention.SelectedIndex;
                _plan.Retention.KeepLastN = (int)_nudKeepLastN.Value;
                _plan.Retention.DeleteOlderThanDays = (int)_nudDeleteDays.Value;
            }

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
    }
}
