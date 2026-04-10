using System;
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
            _btnSave.Text = isLastStep ? Res.Get("PlanEdit_BtnSave") : Res.Get("PlanEdit_BtnSaveExit");
        }

        /// <summary>Aktif adımları hesaplar. Hedefler adımı her zaman dahildir.</summary>
        private void RebuildActiveSteps()
        {
            _activeSteps = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5 };
        }

        /// <summary>Aktif adımlara göre üst bar göstergesini yeniden çizer.</summary>
        private void RebuildStepIndicator()
        {
            string[] allTitles = {
                Res.Get("PlanEdit_StepConnection"),
                Res.Get("PlanEdit_StepSources"),
                Res.Get("PlanEdit_StepScheduling"),
                Res.Get("PlanEdit_StepCompression"),
                Res.Get("PlanEdit_StepTargets"),
                Res.Get("PlanEdit_StepNotification")
            };
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

        /// <summary>
        /// Step indicator (dot veya label) tıklanınca doğrudan ilgili adıma geçer.
        /// Veri bağlamı korunur — yalnızca panel görünürlüğü değişir.
        /// </summary>
        private void OnStepIndicatorClick(object? sender, EventArgs e)
        {
            if (sender is not Control ctl || ctl.Tag is not int visualIndex)
                return;

            // Tıklanan görsel index, aktif adımlar listesindeki sırayı temsil eder
            if (visualIndex < 0 || visualIndex >= _activeSteps.Count)
                return;

            // Zaten bu adımdaysa bir şey yapma
            if (visualIndex == _currentStep)
                return;

            ShowStep(visualIndex);
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
                _btnNext.Text = Res.Get("PlanEdit_Loading");

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
                _btnNext.Text = Res.Get("PlanEdit_Next");
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
    }
}
