using System;
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
                var settings = ControlsToSettings();
                _settingsManager.Save(settings);
                Theme.ModernTheme.ApplyTheme(settings.Theme == "light"
                    ? Theme.ThemeMode.Light : Theme.ThemeMode.Dark);

                // Log renk şemasını uygula
                Theme.ModernTheme.ApplyLogColorScheme(settings.LogColorScheme);
                _txtBackupLog.BackColor = Theme.ModernTheme.LogConsoleBg;

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
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Profil Adı", FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHost", HeaderText = "Sunucu", FillWeight = 30 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "Kullanıcı", FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRecipients", HeaderText = "Alıcılar", FillWeight = 20 });

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
            _olvColDate.Text = Res.Get("Dashboard_ColDate");
            _olvColPlan.Text = Res.Get("Dashboard_ColPlan");
            _olvColDatabase.Text = Res.Get("Dashboard_ColDatabase");
            _olvColResult.Text = Res.Get("Dashboard_ColResult");
            _olvColSize.Text = Res.Get("Dashboard_ColSize");

            // Plans
            _tsbNew.Text = Res.Get("PlanList_BtnNew");
            _tsbEdit.Text = Res.Get("PlanList_BtnEdit");
            _tsbDelete.Text = Res.Get("PlanList_BtnDelete");
            _tsbExport.Text = Res.Get("PlanList_BtnExport");
            _tsbImport.Text = Res.Get("PlanList_BtnImport");
            _colEnabled.HeaderText = Res.Get("PlanList_ColEnabled");
            _colPlanName.HeaderText = Res.Get("PlanList_ColPlanName");
            _colStrategy.HeaderText = Res.Get("PlanList_ColStrategy");
            _colDatabases.HeaderText = Res.Get("PlanList_ColDatabases");
            _colSchedule.HeaderText = Res.Get("PlanList_ColSchedule");
            _colCloudTargets.HeaderText = Res.Get("PlanList_ColCloud");
            _colCreatedAt.HeaderText = Res.Get("PlanList_ColCreatedAt");
            _colStatus.HeaderText = Res.Get("PlanList_ColStatus");
            _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", 0);

            // Settings — theme items
            _lblTheme.Text = Res.Get("Settings_Theme");
            int prevThemeIdx = _cmbTheme.SelectedIndex;
            _cmbTheme.Items.Clear();
            _cmbTheme.Items.Add(Res.Get("Theme_Dark"));
            _cmbTheme.Items.Add(Res.Get("Theme_Light"));
            if (prevThemeIdx >= 0 && prevThemeIdx < _cmbTheme.Items.Count)
                _cmbTheme.SelectedIndex = prevThemeIdx;

            // Settings — log color scheme
            _lblLogColorScheme.Text = Res.Get("Settings_LogColorScheme");

            // Status bar
            _tslStatus.Text = Res.Get("Dashboard_Ready");
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
