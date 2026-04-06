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
