using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    partial class MainWindow
    {
        #region ── TAB 3: Ayarlar ──────────────────────────────────────────────────

        private void LoadSettings()
        {
            try
            {
                _settings = _settingsManager.Load();
                SettingsToControls(_settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar yüklenemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsToControls(AppSettings s)
        {
            _cmbLanguage.SelectedIndex = s.Language == "en-US" ? 1 : 0;
            _cmbTheme.SelectedIndex = s.Theme == "light" ? 1 : 0;
            _chkStartWithWindows.Checked = s.StartWithWindows;
            _chkMinimizeToTray.Checked = s.MinimizeToTray;
            _txtDefaultBackupPath.Text = s.DefaultBackupPath;
            _nudLogRetention.Value = Math.Min(Math.Max(s.LogRetentionDays, 1), 365);
            _nudHistoryRetention.Value = Math.Min(Math.Max(s.HistoryRetentionDays, 1), 365);

            // Log renk şeması
            _cmbLogColorScheme.Items.Clear();
            var schemes = Theme.TerminalColorScheme.GetAll();
            int selectedIdx = 0;
            for (int i = 0; i < schemes.Length; i++)
            {
                _cmbLogColorScheme.Items.Add(schemes[i].DisplayName);
                if (string.Equals(schemes[i].Id, s.LogColorScheme, StringComparison.OrdinalIgnoreCase))
                    selectedIdx = i;
            }
            _cmbLogColorScheme.SelectedIndex = selectedIdx;

            LoadProfileList(s);
        }

        private AppSettings ControlsToSettings()
        {
            var s = _settings ?? new AppSettings();

            s.Language = _cmbLanguage.SelectedIndex == 1 ? "en-US" : "tr-TR";
            s.Theme = _cmbTheme.SelectedIndex == 1 ? "light" : "dark";
            s.StartWithWindows = _chkStartWithWindows.Checked;
            s.MinimizeToTray = _chkMinimizeToTray.Checked;
            s.DefaultBackupPath = _txtDefaultBackupPath.Text.Trim();
            s.LogRetentionDays = (int)_nudLogRetention.Value;
            s.HistoryRetentionDays = (int)_nudHistoryRetention.Value;

            // Log renk şeması
            var schemes = Theme.TerminalColorScheme.GetAll();
            int schemeIdx = _cmbLogColorScheme.SelectedIndex;
            s.LogColorScheme = (schemeIdx >= 0 && schemeIdx < schemes.Length)
                ? schemes[schemeIdx].Id
                : "koru";

            // SMTP profiller Add/Edit/Delete dialoglarında bağımsız kaydedilir; burada dokunulmaz.

            return s;
        }

        private bool ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_txtDefaultBackupPath.Text))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_BackupPathRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDefaultBackupPath.Focus();
                return false;
            }

            return true;
        }

        private void OnBrowseBackupPath(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("Settings_BrowsePath");
                fbd.SelectedPath = _txtDefaultBackupPath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                    _txtDefaultBackupPath.Text = fbd.SelectedPath;
            }
        }

        private void OnSaveSettingsClick(object sender, EventArgs e)
        {
            if (!ValidateSettings()) return;

            try
            {
                string previousLang = _settings?.Language ?? "tr-TR";
                var settings = ControlsToSettings();
                _settingsManager.Save(settings);
                Theme.ModernTheme.ApplyTheme(settings.Theme == "light"
                    ? Theme.ThemeMode.Light : Theme.ThemeMode.Dark);

                // Log renk şemasını uygula
                Theme.ModernTheme.ApplyLogColorScheme(settings.LogColorScheme);
                _txtBackupLog.BackColor = Theme.ModernTheme.LogConsoleBg;

                // Dil değişikliği varsa kültürü güncelle ve tüm UI'yı yeniden lokalize et
                if (!string.Equals(previousLang, settings.Language, StringComparison.OrdinalIgnoreCase))
                {
                    var culture = new CultureInfo(settings.Language);
                    Thread.CurrentThread.CurrentUICulture = culture;
                    Thread.CurrentThread.CurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    ApplyLocalization();
                    ApplyIcons();
                    LoadProfileList(settings);
                }

                Log.Information("Ayarlar kaydedildi.");
                Theme.ModernMessageBox.Show(Res.Get("Settings_SavedMessage"),
                    Res.Get("Settings_SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar kaydedilemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_SaveError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelSettingsClick(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadProfileList(AppSettings s)
        {
            _dgvSmtpProfiles.Rows.Clear();
            _dgvSmtpProfiles.Columns.Clear();
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", Visible = false });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = Res.Get("SmtpGrid_ColName"), FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHost", HeaderText = Res.Get("SmtpGrid_ColHost"), FillWeight = 30 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = Res.Get("SmtpGrid_ColUser"), FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRecipients", HeaderText = Res.Get("SmtpGrid_ColRecipients"), FillWeight = 20 });

            foreach (var p in s.SmtpProfiles)
            {
                _dgvSmtpProfiles.Rows.Add(
                    p.Id,
                    p.DisplayName,
                    string.IsNullOrEmpty(p.Host) ? "—" : $"{p.Host}:{p.Port}",
                    p.Username,
                    p.RecipientEmails);
            }
        }

        private void OnSmtpAddClick(object sender, EventArgs e)
        {
            using var dlg = new Forms.SmtpProfileEditDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _settings.SmtpProfiles.Add(dlg.ResultProfile);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpEditClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0) return;

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            var existing = _settings.SmtpProfiles.Find(p => p.Id == profileId);
            if (existing == null) return;

            using var dlg = new Forms.SmtpProfileEditDialog(existing);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int idx = _settings.SmtpProfiles.IndexOf(existing);
            _settings.SmtpProfiles[idx] = dlg.ResultProfile;
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpDeleteClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0) return;

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            string profileName = _dgvSmtpProfiles.SelectedRows[0].Cells["colName"].Value?.ToString() ?? profileId;

            if (Theme.ModernMessageBox.Show(Res.Format("Settings_SmtpDeleteConfirm", profileName),
                    Res.Get("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _settings.SmtpProfiles.RemoveAll(p => p.Id == profileId);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpTestClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0)
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpSelectProfileFirst"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            var profile = _settings.SmtpProfiles.Find(p => p.Id == profileId);
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.Host))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpServerRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.RecipientEmails))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpRecipientTestRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                using var client = new MailKit.Net.Smtp.SmtpClient();
                var options = profile.UseSsl
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None;

                client.Connect(profile.Host, profile.Port, options);

                if (!string.IsNullOrEmpty(profile.Username))
                {
                    string plainPwd = string.Empty;
                    if (!string.IsNullOrEmpty(profile.Password))
                    {
                        try { plainPwd = PasswordProtector.Unprotect(profile.Password); }
                        catch { /* şifreli değilse olduğu gibi kullan */ plainPwd = profile.Password; }
                    }
                    client.Authenticate(profile.Username, plainPwd);
                }

                string senderEmail = !string.IsNullOrWhiteSpace(profile.SenderEmail)
                    ? profile.SenderEmail : profile.Username;
                string senderName = !string.IsNullOrWhiteSpace(profile.SenderDisplayName)
                    ? profile.SenderDisplayName : "Koru MsSql Yedek";

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
                string firstRecipient = profile.RecipientEmails.Split(new[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                message.To.Add(MimeKit.MailboxAddress.Parse(firstRecipient));
                message.Subject = Res.Format("Settings_SmtpTestSubject", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                message.Body = new MimeKit.TextPart("plain") { Text = Res.Get("Settings_SmtpTestBody") };

                client.Send(message);
                client.Disconnect(true);

                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpTestSuccess"), Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_SmtpTestError", SanitizeErrorMessage(ex.Message)),
                    Res.Get("Settings_SmtpTestErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void OnPasswordSetupClick(object sender, EventArgs e)
        {
            var settings = _settingsManager.Load();
            using (var dlg = new Forms.PasswordSetupDialog(settings, _settingsManager))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // Ayarları yeniden yükle (şifre durumu değişmiş olabilir)
                    LoadSettings();
                }
            }
        }

        #endregion

        #region ── Localization ────────────────────────────────────────────────────

        private void ApplyLocalization()
        {
            Text = "Koru MsSql Yedek";

            // Tab headers
            _tabDashboard.Text = Res.Get("Tab_Dashboard");
            _tabPlans.Text = Res.Get("Tab_Plans");
            _tabLogs.Text = Res.Get("Tab_Logs");
            _tabSettings.Text = Res.Get("Tab_Settings");

            // Dashboard
            _lblStatusCaption.Text = Res.Get("Dashboard_StatusCaption");
            _lblStatusValue.Text = Res.Get("Dashboard_Ready");
            _lblNextBackupCaption.Text = Res.Get("Dashboard_NextBackupCaption");
            _lblActivePlansCaption.Text = Res.Get("Dashboard_ActivePlansCaption");
            _lblGridTitle.Text = Res.Get("Dashboard_LastBackupsGroup");

            // Plans — toolbar
            _tsbNew.Text = Res.Get("PlanList_BtnNew");
            _tsbEdit.Text = Res.Get("PlanList_BtnEdit");
            _tsbDelete.Text = Res.Get("PlanList_BtnDelete");
            _tsbExport.Text = Res.Get("PlanList_BtnExport");
            _tsbImport.Text = Res.Get("PlanList_BtnImport");
            _tsbRefreshPlans.Text = Res.Get("PlanList_BtnRefresh");
            _tslSearchLabel.Text = Res.Get("PlanList_SearchLabel");

            // Plans — grid columns
            _colEnabled.HeaderText = Res.Get("PlanList_ColEnabled");
            _colPlanName.HeaderText = Res.Get("PlanList_ColPlanName");
            _colStrategy.HeaderText = Res.Get("PlanList_ColStrategy");
            _colDatabases.HeaderText = Res.Get("PlanList_ColDatabases");
            _colSchedule.HeaderText = Res.Get("PlanList_ColSchedule");
            _colCloudTargets.HeaderText = Res.Get("PlanList_ColCloud");
            _colCreatedAt.HeaderText = Res.Get("PlanList_ColCreatedAt");
            _colStatus.HeaderText = Res.Get("PlanList_ColStatus");
            _colProgress.HeaderText = Res.Get("PlanList_ColProgress");
            _colNextRun.HeaderText = Res.Get("PlanList_ColNextRun");
            _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", 0);

            // Plans — context menu
            _ctxBackupNow.Text = Res.Get("PlanList_CtxBackupNow");
            _ctxStopBackup.Text = Res.Get("PlanList_CtxStopBackup");
            _ctxEditPlan.Text = Res.Get("PlanList_CtxEdit");
            _ctxDeletePlan.Text = Res.Get("PlanList_CtxDelete");
            _ctxExportPlan.Text = Res.Get("PlanList_CtxExport");
            _ctxViewPlanLogs.Text = Res.Get("PlanList_CtxViewLogs");
            _ctxRestore.Text = Res.Get("PlanList_CtxRestore");

            // Backup panel
            _lblBackupType.Text = Res.Get("Backup_Type");
            _lblBackupStatus.Text = Res.Get("Backup_StatusReady");
            _btnCancelBackup.Text = Res.Get("Backup_Cancel");
            _btnStart.Text = Res.Get("Backup_Start");
            int prevBackupIdx = _cmbBackupType.SelectedIndex;
            _cmbBackupType.Items.Clear();
            _cmbBackupType.Items.Add(Res.Get("Backup_TypeFull"));
            _cmbBackupType.Items.Add(Res.Get("Backup_TypeDiff"));
            _cmbBackupType.Items.Add(Res.Get("Backup_TypeIncr"));
            if (prevBackupIdx >= 0 && prevBackupIdx < _cmbBackupType.Items.Count)
                _cmbBackupType.SelectedIndex = prevBackupIdx;

            // Log viewer
            _lblLogFile.Text = Res.Get("LogViewer_LblFile");
            _lblLevel.Text = Res.Get("LogViewer_LblLevel");
            _chkAutoTail.Text = Res.Get("LogViewer_ChkAutoTail");
            _lblLogSearch.Text = Res.Get("LogViewer_LblSearch");
            _lblLogPlan.Text = Res.Get("LogViewer_LblPlan");
            _btnClearLogFilter.Text = Res.Get("LogViewer_BtnClear");
            _btnLogRefresh.Text = Res.Get("LogViewer_BtnRefresh");
            _btnLogExport.Text = Res.Get("LogViewer_BtnExport");
            _colTimestamp.HeaderText = Res.Get("LogViewer_ColTime");
            _colLevel.HeaderText = Res.Get("LogViewer_ColLevel");
            _colMessage.HeaderText = Res.Get("LogViewer_ColMessage");

            // Settings — General tab
            _tabGeneral.Text = Res.Get("Settings_TabGeneral");
            _lblLanguage.Text = Res.Get("Settings_Language");
            _chkStartWithWindows.Text = Res.Get("Settings_StartWithWindows");
            _chkMinimizeToTray.Text = Res.Get("Settings_MinimizeToTray");
            _lblDefaultBackupPath.Text = Res.Get("Settings_DefaultBackupPath");
            _lblLogRetention.Text = Res.Get("Settings_LogRetention");
            _lblLogRetentionSuffix.Text = Res.Get("Settings_RetentionSuffix");
            _lblHistoryRetention.Text = Res.Get("Settings_HistoryRetention");
            _lblHistoryRetentionSuffix.Text = Res.Get("Settings_RetentionSuffix");
            _lblTheme.Text = Res.Get("Settings_Theme");
            _lblLogColorScheme.Text = Res.Get("Settings_LogColorScheme");
            _lblLogColorScheme.Text = Res.Get("Settings_LogConsoleTheme");

            // Settings — theme items
            int prevThemeIdx = _cmbTheme.SelectedIndex;
            _cmbTheme.Items.Clear();
            _cmbTheme.Items.Add(Res.Get("Theme_Dark"));
            _cmbTheme.Items.Add(Res.Get("Theme_Light"));
            if (prevThemeIdx >= 0 && prevThemeIdx < _cmbTheme.Items.Count)
                _cmbTheme.SelectedIndex = prevThemeIdx;

            // Settings — SMTP tab
            _tabSmtp.Text = Res.Get("Settings_SmtpTab");
            _lblSmtpProfilesTitle.Text = Res.Get("Settings_SmtpTitle");
            _btnSmtpAdd.Text = Res.Get("Settings_SmtpAdd");
            _btnSmtpEdit.Text = Res.Get("Settings_SmtpEdit");
            _btnSmtpDelete.Text = Res.Get("Settings_SmtpDelete");
            _btnSmtpTest.Text = Res.Get("Settings_SmtpTest");

            // Settings — Security tab
            _tabSecurity.Text = Res.Get("Settings_SecurityTab");
            _lblSecurityTitle.Text = Res.Get("Settings_SecurityTitle");
            _btnPasswordSetup.Text = Res.Get("Settings_PasswordSetup");
            _lblSecurityInfo.Text = Res.Get("Settings_SecurityInfo");

            // Settings — buttons
            _btnCancelSettings.Text = Res.Get("Settings_Cancel");
            _btnSaveSettings.Text = Res.Get("Settings_Save");

            // Status bar
            _tslStatus.Text = Res.Get("Dashboard_Ready");

            // ── Rich Tooltips ────────────────────────────────────────────

            // Dashboard
            _toolTip.SetToolTip(_lblStatusValue, Res.Get("Tip_Dashboard_Status"));
            _toolTip.SetToolTip(_lblStatusCaption, Res.Get("Tip_Dashboard_Status"));
            _toolTip.SetToolTip(_lblNextBackupCaption, Res.Get("Tip_Dashboard_NextBackup"));
            _toolTip.SetToolTip(_lblActivePlansCaption, Res.Get("Tip_Dashboard_ActivePlans"));
            _toolTip.SetToolTip(_lblGridTitle, Res.Get("Tip_Dashboard_LastBackups"));

            // Plans — toolbar (ToolStripItems use .ToolTipText)
            _tsbNew.ToolTipText = Res.Get("Tip_Plans_New");
            _tsbEdit.ToolTipText = Res.Get("Tip_Plans_Edit");
            _tsbDelete.ToolTipText = Res.Get("Tip_Plans_Delete");
            _tsbExport.ToolTipText = Res.Get("Tip_Plans_Export");
            _tsbImport.ToolTipText = Res.Get("Tip_Plans_Import");
            _tsbRefreshPlans.ToolTipText = Res.Get("Tip_Plans_Refresh");
            _tstSearch.ToolTipText = Res.Get("Tip_Plans_Search");

            // Plans — context menu (ToolTipText)
            _ctxBackupNow.ToolTipText = Res.Get("Tip_Plans_BackupNow");
            _ctxStopBackup.ToolTipText = Res.Get("Tip_Plans_StopBackup");

            // Backup panel
            _toolTip.SetToolTip(_cmbBackupType, Res.Get("Tip_Backup_Type"));
            _toolTip.SetToolTip(_btnStart, Res.Get("Tip_Backup_Start"));
            _toolTip.SetToolTip(_btnCancelBackup, Res.Get("Tip_Backup_Cancel"));

            // Log viewer
            _toolTip.SetToolTip(_cmbLogFile, Res.Get("Tip_Log_FileSelect"));
            _toolTip.SetToolTip(_cmbLevel, Res.Get("Tip_Log_Level"));
            _toolTip.SetToolTip(_chkAutoTail, Res.Get("Tip_Log_AutoTail"));
            _toolTip.SetToolTip(_txtLogSearch, Res.Get("Tip_Log_Search"));
            _toolTip.SetToolTip(_cmbLogPlan, Res.Get("Tip_Log_PlanFilter"));
            _toolTip.SetToolTip(_btnClearLogFilter, Res.Get("Tip_Log_Clear"));
            _toolTip.SetToolTip(_btnLogRefresh, Res.Get("Tip_Log_Refresh"));
            _toolTip.SetToolTip(_btnLogExport, Res.Get("Tip_Log_Export"));

            // Settings — General
            _toolTip.SetToolTip(_cmbLanguage, Res.Get("Tip_Settings_Language"));
            _toolTip.SetToolTip(_chkStartWithWindows, Res.Get("Tip_Settings_StartWithWindows"));
            _toolTip.SetToolTip(_chkMinimizeToTray, Res.Get("Tip_Settings_MinimizeToTray"));
            _toolTip.SetToolTip(_txtDefaultBackupPath, Res.Get("Tip_Settings_DefaultBackupPath"));
            _toolTip.SetToolTip(_nudLogRetention, Res.Get("Tip_Settings_LogRetention"));
            _toolTip.SetToolTip(_nudHistoryRetention, Res.Get("Tip_Settings_HistoryRetention"));
            _toolTip.SetToolTip(_cmbTheme, Res.Get("Tip_Settings_Theme"));
            _toolTip.SetToolTip(_cmbLogColorScheme, Res.Get("Tip_Settings_LogColorScheme"));
            _toolTip.SetToolTip(_btnSaveSettings, Res.Get("Tip_Settings_Save"));
            _toolTip.SetToolTip(_btnCancelSettings, Res.Get("Tip_Settings_Cancel"));

            // Settings — SMTP
            _toolTip.SetToolTip(_btnSmtpAdd, Res.Get("Tip_Settings_SmtpAdd"));
            _toolTip.SetToolTip(_btnSmtpEdit, Res.Get("Tip_Settings_SmtpEdit"));
            _toolTip.SetToolTip(_btnSmtpDelete, Res.Get("Tip_Settings_SmtpDelete"));
            _toolTip.SetToolTip(_btnSmtpTest, Res.Get("Tip_Settings_SmtpTest"));

            // Settings — Security
            _toolTip.SetToolTip(_btnPasswordSetup, Res.Get("Tip_Settings_PasswordSetup"));
            _toolTip.SetToolTip(_lblSecurityInfo, Res.Get("Tip_Settings_SecurityInfo"));
        }

        #endregion

        #region ── Security Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Hata mesajından hassas bilgileri (dosya yolları, sunucu adresleri, stack trace) temizler.
        /// Kullanıcıya gösterilecek mesajlarda bilgi sızıntısını önler.
        /// </summary>
        private static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return Res.Get("Error_Unknown");

            // Stack trace varsa kaldır
            int stackIdx = message.IndexOf("   at ", StringComparison.Ordinal);
            if (stackIdx > 0)
                message = message.Substring(0, stackIdx).Trim();

            // Dosya yollarını gizle (C:\..., \\server\... vb.)
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Za-z]:\\[^\s""']+|\\\\[^\s""']+",
                "[yol gizlendi]");

            // Uzun mesajları kısalt
            const int maxLength = 300;
            if (message.Length > maxLength)
                message = message.Substring(0, maxLength) + "…";

            return message;
        }

        #endregion
    }
}
