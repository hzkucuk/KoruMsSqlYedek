#nullable enable
using System;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    partial class PlanEditForm
    {
        #region Visibility Helpers

        private void OnAuthModeChanged(object sender, EventArgs e) => UpdateAuthFieldsVisibility();
        private void OnStrategyChanged(object sender, EventArgs e) => UpdateStrategyFieldsVisibility();
        private void OnRetentionChanged(object sender, EventArgs e) => UpdateRetentionFieldsVisibility();
        private void OnEmailEnabledChanged(object sender, EventArgs e) => UpdateEmailFieldsVisibility();

        private void OnOpenSmtpSettingsClick(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            using var dlg = new SmtpProfileEditDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            // Yeni profili kaydet
            var settings = _settingsManager.Load();
            settings.SmtpProfiles.Add(dlg.ResultProfile);
            _settingsManager.Save(settings);

            // ComboBox'ı yeniden yükle ve yeni profili seç
            string newId = dlg.ResultProfile.Id;
            _cmbSmtpProfile.Items.Clear();
            _cmbSmtpProfile.Items.Add(new SmtpProfile { Id = string.Empty, DisplayName = Res.Get("PlanEdit_SmtpNoProfile") ?? "(Profil seçin)" });
            foreach (var profile in settings.SmtpProfiles)
                _cmbSmtpProfile.Items.Add(profile);

            for (int i = 0; i < _cmbSmtpProfile.Items.Count; i++)
            {
                if (_cmbSmtpProfile.Items[i] is SmtpProfile p && p.Id == newId)
                {
                    _cmbSmtpProfile.SelectedIndex = i;
                    break;
                }
            }
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

            // Oto. Tam Yedek Eşiği yalnızca Fark/Artırımlı stratejilerinde anlamlı
            bool hasChain = idx >= 1;
            _lblAutoPromote.Enabled = hasChain;
            _nudAutoPromote.Enabled = hasChain;

            RelayoutStep3();
        }

        /// <summary>Recalculates vertical positions for all Step 3 controls based on visibility and dynamic panel heights.</summary>
        private void RelayoutStep3()
        {
            var step3 = _stepPanels[2];
            step3.SuspendLayout();

            int lx = 0, tx = 150;
            int y = 5;

            // Header
            _lblStep3Header.Location = new System.Drawing.Point(lx, y);
            y += 30;

            // Strategy combo
            _lblStrategy.Location = new System.Drawing.Point(lx, y + 3);
            _cmbStrategy.Location = new System.Drawing.Point(tx, y);
            y += 34;

            // Full Backup CronBuilderPanel (always visible)
            _lblFullCron.Location = new System.Drawing.Point(lx, y + 3);
            _cronFull.Location = new System.Drawing.Point(tx, y);
            y += _cronFull.Height + 4;

            // Differential CronBuilderPanel
            if (_cronDiff.Visible)
            {
                _lblDiffCron.Location = new System.Drawing.Point(lx, y + 3);
                _cronDiff.Location = new System.Drawing.Point(tx, y);
                y += _cronDiff.Height + 4;
            }

            // Incremental CronBuilderPanel
            if (_cronIncr.Visible)
            {
                _lblIncrCron.Location = new System.Drawing.Point(lx, y + 3);
                _cronIncr.Location = new System.Drawing.Point(tx, y);
                y += _cronIncr.Height + 6;
            }

            // Auto Full Backup Threshold
            _lblAutoPromote.Location = new System.Drawing.Point(lx, y + 3);
            _nudAutoPromote.Location = new System.Drawing.Point(tx, y);
            y += 30;

            // Verify checkbox
            _chkVerify.Location = new System.Drawing.Point(lx, y);
            y += 32;

            // File schedule separator (hidden)
            _lblStep3FileSep.Location = new System.Drawing.Point(lx, y);

            step3.ResumeLayout(true);
        }

        private void OnCronPanelHeightChanged(object? sender, EventArgs e) => RelayoutStep3();

        private void UpdateRetentionFieldsVisibility()
        {
            // Özel mod (index=4) olduğunda manuel kontroller görünür
            bool isCustom = _cmbRetentionTemplate.SelectedIndex < 0 ||
                            _cmbRetentionTemplate.SelectedIndex == 4;

            _lblRetention.Visible = isCustom;
            _cmbRetention.Visible = isCustom;

            int retIdx = _cmbRetention.SelectedIndex;
            _lblKeepLastN.Visible = isCustom && (retIdx == 0 || retIdx == 2);
            _nudKeepLastN.Visible = isCustom && (retIdx == 0 || retIdx == 2);
            _lblDeleteDays.Visible = isCustom && (retIdx == 1 || retIdx == 2);
            _nudDeleteDays.Visible = isCustom && (retIdx == 1 || retIdx == 2);
        }

        private void OnRetentionTemplateChanged(object? sender, EventArgs e)
        {
            // Info etiketi: seçili şablonun açıklaması
            int idx = _cmbRetentionTemplate.SelectedIndex;
            _lblRetentionTemplateInfo.Text = idx switch
            {
                0 => Res.Get("PlanEdit_RetTplMinimal"),
                1 => Res.Get("PlanEdit_RetTplStandard"),
                2 => Res.Get("PlanEdit_RetTplExtended"),
                3 => Res.Get("PlanEdit_RetTplGFS"),
                _ => Res.Get("PlanEdit_RetTplCustom")
            };
            UpdateRetentionFieldsVisibility();
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
            // Dosya yedekleme artık SQL ile aynı zamanlamayı kullanır; ayrı schedule UI'ı yok
            _lblStep3FileSep.Visible = false;
            _lblStep3FileSchedHeader.Visible = false;
            _lblFileSchedule.Visible = false;
            _cronFileSchedule.Visible = false;
        }

        #endregion
    }
}
