using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Serilog;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Plan listesi formu — tüm yedekleme planlarını DataGridView ile gösterir.
    /// CRUD işlemleri (Yeni, Düzenle, Sil, Dışa/İçe Aktar) sağlar.
    /// </summary>
    public partial class PlanListForm : Form
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<PlanListForm>();
        private readonly IPlanManager _planManager;
        private readonly ISqlBackupService _sqlBackupService;

        public PlanListForm(IPlanManager planManager, ISqlBackupService sqlBackupService)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));

            InitializeComponent();
            ApplyLocalization();
            _planManager = planManager;
            _sqlBackupService = sqlBackupService;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RefreshPlanList();
        }

        #region Data Binding

        private void RefreshPlanList()
        {
            try
            {
                var plans = _planManager.GetAllPlans();
                _dgvPlans.Rows.Clear();

                foreach (var plan in plans)
                {
                    var dbList = plan.Databases != null && plan.Databases.Count > 0
                        ? string.Join(", ", plan.Databases)
                        : "—";

                    var strategy = GetStrategyDisplayName(plan.Strategy?.Type ?? BackupStrategyType.Full);
                    var schedule = plan.Strategy?.FullSchedule ?? "—";
                    var cloudCount = plan.CloudTargets?.Count(t => t.IsEnabled) ?? 0;

                    var rowIndex = _dgvPlans.Rows.Add(
                        plan.IsEnabled,
                        plan.PlanName ?? Res.Get("PlanList_Unnamed"),
                        strategy,
                        dbList,
                        schedule,
                        Res.Format("PlanList_TargetFormat", cloudCount),
                        plan.CreatedAt.ToString("dd.MM.yyyy"));

                    _dgvPlans.Rows[rowIndex].Tag = plan;
                }

                UpdateStatusBar(plans.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan listesi yüklenirken hata oluştu.");
                MessageBox.Show(
                    Res.Format("PlanList_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatusBar(int planCount)
        {
            _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", planCount);
        }

        private string GetStrategyDisplayName(BackupStrategyType type)
        {
            switch (type)
            {
                case BackupStrategyType.Full: return Res.Get("PlanList_StratFull");
                case BackupStrategyType.FullPlusDifferential: return Res.Get("PlanList_StratFullDiff");
                case BackupStrategyType.FullPlusDifferentialPlusIncremental: return Res.Get("PlanList_StratFullDiffInc");
                default: return type.ToString();
            }
        }

        #endregion

        #region Toolbar Events

        private void OnNewPlanClick(object sender, EventArgs e)
        {
            using (var form = new PlanEditForm(_planManager, _sqlBackupService))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private void OnEditPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, plan))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private void OnDeletePlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            var result = MessageBox.Show(
                Res.Format("PlanList_DeleteConfirm", plan.PlanName),
                Res.Get("PlanList_DeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _planManager.DeletePlan(plan.PlanId);
                    Log.Information("Plan silindi: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                    RefreshPlanList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan silinirken hata: {PlanId}", plan.PlanId);
                    MessageBox.Show(Res.Format("PlanList_DeleteError", ex.Message),
                        Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("PlanList_ExportDialogTitle");
                sfd.Filter = Res.Get("PlanList_ExportFilter");
                sfd.FileName = $"{plan.PlanName ?? "plan"}.json";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _planManager.ExportPlan(plan.PlanId, sfd.FileName);
                        MessageBox.Show(Res.Get("PlanList_ExportSuccess"), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan export hatası: {PlanId}", plan.PlanId);
                        MessageBox.Show(Res.Format("PlanList_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnImportClick(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = Res.Get("PlanList_ImportDialogTitle");
                ofd.Filter = Res.Get("PlanList_ExportFilter");

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var plan = _planManager.ImportPlan(ofd.FileName);
                        Log.Information("Plan içe aktarıldı: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                        RefreshPlanList();
                        MessageBox.Show(Res.Format("PlanList_ImportSuccess", plan.PlanName), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan import hatası: {FilePath}", ofd.FileName);
                        MessageBox.Show(Res.Format("PlanList_ImportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            RefreshPlanList();
        }

        #endregion

        #region Grid Events

        private void OnGridCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OnEditPlanClick(sender, e);
        }

        #endregion

        #region Helpers

        private BackupPlan GetSelectedPlan()
        {
            if (_dgvPlans.CurrentRow == null || _dgvPlans.CurrentRow.Tag == null)
            {
                MessageBox.Show(Res.Get("PlanList_SelectPlan"), Res.Get("Info"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return _dgvPlans.CurrentRow.Tag as BackupPlan;
        }

        #endregion

        #region Localization

        private void ApplyLocalization()
        {
            Text = Res.Get("PlanList_Title");
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
            _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", 0);
        }

        #endregion
    }
}
