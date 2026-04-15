#nullable enable
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
        private readonly IPlanManager _planManager = null!;
        private readonly ISqlBackupService _sqlBackupService = null!;
        private readonly IAppSettingsManager _settingsManager = null!;
        private readonly BackupPlan _plan = null!;
        private readonly bool _isNew;
        private int _currentStep;
        private bool _connectionTested;

        // Runtime arrays — Designer uses named fields, runtime code uses arrays for loops
        private Panel[] _stepPanels = null!;
        private Label[] _stepLabels = null!;
        private Label[] _stepDots = null!;
        private System.Collections.Generic.List<int> _activeSteps = null!;

        /// <summary>Designer-only. Do not use at runtime.</summary>
        public PlanEditForm()
        {
            InitializeComponent();
            BuildRuntimeArrays();
        }

        /// <summary>Yeni plan oluşturma.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, IAppSettingsManager settingsManager)
            : this(planManager, sqlBackupService, settingsManager, null) { }

        /// <summary>Kaydedilen planı döndürür (DialogResult.OK sonrası geçerlidir).</summary>
        public BackupPlan SavedPlan => _plan;

        /// <summary>Mevcut planı düzenleme. null ise yeni plan.</summary>
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, IAppSettingsManager settingsManager, BackupPlan? existingPlan)
        {
            ArgumentNullException.ThrowIfNull(planManager);
            ArgumentNullException.ThrowIfNull(sqlBackupService);
            ArgumentNullException.ThrowIfNull(settingsManager);

            InitializeComponent();
            BuildRuntimeArrays();
            ApplyThemeColors();
            ApplyIcons();
            ApplyLocalization();
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

        /// <summary>
        /// Maps named Designer fields to runtime arrays used by wizard navigation.
        /// Must be called after InitializeComponent().
        /// </summary>
        private void BuildRuntimeArrays()
        {
            _stepPanels = [_pnlStep1, _pnlStep2, _pnlStep3, _pnlStep4, _pnlStep5, _pnlStep6];
            _stepDots = [_lblStepNum1, _lblStepNum2, _lblStepNum3, _lblStepNum4, _lblStepNum5, _lblStepNum6];
            _stepLabels = [_lblStepTitle1, _lblStepTitle2, _lblStepTitle3, _lblStepTitle4, _lblStepTitle5, _lblStepTitle6];
            _activeSteps = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5 };
        }

        /// <summary>
        /// Applies runtime theme colors to controls whose colors were set to literals
        /// in InitializeComponent for Designer compatibility.
        /// </summary>
        private void ApplyThemeColors()
        {
            // Wizard infrastructure
            _pnlStepIndicator.BackColor = Theme.ModernTheme.SurfaceColor;
            _pnlNavigation.BackColor = Theme.ModernTheme.SurfaceColor;

            // Step indicator titles
            foreach (var lbl in new[] { _lblStepTitle1, _lblStepTitle2, _lblStepTitle3,
                                        _lblStepTitle4, _lblStepTitle5, _lblStepTitle6 })
            {
                lbl.ForeColor = Theme.ModernTheme.TextSecondary;
            }

            // Section headers — AccentPrimary
            foreach (var lbl in new System.Windows.Forms.Label[] {
                _lblStep1Header, _lblStep1SqlHeader,
                _lblStep2Header, _lblStep2FileHeader,
                _lblStep3Header, _lblStep3FileSchedHeader,
                _lblStep4Header, _lblStep4RetHeader,
                _lblStep5Header,
                _lblStep6Header, _lblStep6ReportHeader })
            {
                lbl.ForeColor = Theme.ModernTheme.AccentPrimary;
            }

            // Hint / secondary labels
            _lblStep2Hint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStep5Hint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblRetentionTemplateInfo.ForeColor = Theme.ModernTheme.TextSecondary;
            _chkProtectPlan.ForeColor = Theme.ModernTheme.TextPrimary;
        }

        private void ApplyIcons()
        {
            // Navigasyon
            _btnBack.Image = LoadIcon("Import_16x16.png");
            _btnBack.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            _btnNext.Image = LoadIcon("Export_16x16.png");
            _btnNext.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
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

        private void ApplyLocalization()
        {
            // Step indicator titles
            string[] stepTitles =
            [
                Res.Get("PlanEdit_StepConnection"),
                Res.Get("PlanEdit_StepSources"),
                Res.Get("PlanEdit_StepSchedule"),
                Res.Get("PlanEdit_StepCompression"),
                Res.Get("PlanEdit_StepTargets"),
                Res.Get("PlanEdit_StepNotification")
            ];
            for (int i = 0; i < 6 && i < _stepLabels.Length; i++)
                _stepLabels[i].Text = stepTitles[i];

            // Step 1
            _lblStep1Header.Text = Res.Get("PlanEdit_Step1Header");
            _lblPlanName.Text = Res.Get("PlanEdit_PlanName");
            _chkEnabled.Text = Res.Get("PlanEdit_PlanActive");
            _lblLocalPath.Text = Res.Get("PlanEdit_LocalPath");
            _lblStep1SqlHeader.Text = Res.Get("PlanEdit_Step1SqlHeader");
            _lblServer.Text = Res.Get("PlanEdit_Server");
            _lblAuthMode.Text = Res.Get("PlanEdit_AuthMode");
            _lblSqlUser.Text = Res.Get("PlanEdit_SqlUser");
            _lblSqlPassword.Text = Res.Get("PlanEdit_SqlPassword");
            _lblTimeout.Text = Res.Get("PlanEdit_Timeout");
            _chkTrustCert.Text = Res.Get("PlanEdit_TrustCert");
            _btnTestSql.Text = Res.Get("PlanEdit_TestSql");

            // Step 2
            _lblStep2Header.Text = Res.Get("PlanEdit_Step2Header");
            _lblStep2Hint.Text = Res.Get("PlanEdit_Step2Hint");
            _chkSelectAll.Text = Res.Get("PlanEdit_SelectAll");
            _btnRefreshDatabases.Text = Res.Get("PlanEdit_RefreshDatabases");
            _lblStep2FileHeader.Text = Res.Get("PlanEdit_Step2FileHeader");
            _chkFileBackupEnabled.Text = Res.Get("PlanEdit_FileBackupEnabled");
            _colFsName.Text = Res.Get("PlanEdit_ColFsName");
            _colFsPath.Text = Res.Get("PlanEdit_ColFsPath");
            _colFsVss.Text = Res.Get("PlanEdit_ColFsVss");
            _colFsStatus.Text = Res.Get("PlanEdit_ColFsStatus");
            _btnAddFileSource.Text = Res.Get("PlanEdit_FileAdd");
            _btnEditFileSource.Text = Res.Get("PlanEdit_FileEdit");
            _btnRemoveFileSource.Text = Res.Get("PlanEdit_FileRemove");

            // Step 3
            _lblStep3Header.Text = Res.Get("PlanEdit_Step3Header");
            _lblStrategy.Text = Res.Get("PlanEdit_Strategy");
            _lblFullCron.Text = Res.Get("PlanEdit_FullCron");
            _lblDiffCron.Text = Res.Get("PlanEdit_DiffCron");
            _lblIncrCron.Text = Res.Get("PlanEdit_IncrCron");
            _lblAutoPromote.Text = Res.Get("PlanEdit_AutoPromote");
            _chkVerify.Text = Res.Get("PlanEdit_Verify");
            _lblStep3FileSchedHeader.Text = Res.Get("PlanEdit_FileSchedHeader");
            _lblFileSchedule.Text = Res.Get("PlanEdit_FileSchedule");

            // Step 4
            _lblStep4Header.Text = Res.Get("PlanEdit_Step4Header");
            _lblAlgorithm.Text = Res.Get("PlanEdit_Algorithm");
            _lblLevel.Text = Res.Get("PlanEdit_Level");
            _lblArchivePassword.Text = Res.Get("PlanEdit_ArchivePassword");
            _lblStep4RetHeader.Text = Res.Get("PlanEdit_Step4RetHeader");
            _lblRetentionTemplate.Text = Res.Get("PlanEdit_RetentionTemplate");
            _lblRetention.Text = Res.Get("PlanEdit_Retention");
            _lblKeepLastN.Text = Res.Get("PlanEdit_KeepLastN");
            _lblDeleteDays.Text = Res.Get("PlanEdit_DeleteDays");
            _chkProtectPlan.Text = Res.Get("PlanEdit_ProtectPlan");
            _txtPlanPassword.PlaceholderText = Res.Get("PlanEdit_PlanPasswordPlaceholder");
            _txtRecoveryPassword.PlaceholderText = Res.Get("PlanEdit_RecoveryPasswordPlaceholder");

            // Step 5
            _lblStep5Header.Text = Res.Get("PlanEdit_Step5Header");
            _lblStep5Hint.Text = Res.Get("PlanEdit_Step5Hint");
            _colCtName.Text = Res.Get("PlanEdit_ColCtName");
            _colCtType.Text = Res.Get("PlanEdit_ColCtType");
            _colCtStatus.Text = Res.Get("PlanEdit_ColCtStatus");
            _btnAddCloud.Text = Res.Get("PlanEdit_CloudAdd");
            _btnEditCloud.Text = Res.Get("PlanEdit_CloudEdit");
            _btnRemoveCloud.Text = Res.Get("PlanEdit_CloudRemove");

            // Step 6
            _lblStep6Header.Text = Res.Get("PlanEdit_Step6Header");
            _chkEmailEnabled.Text = Res.Get("PlanEdit_EmailEnabled");
            _chkToast.Text = Res.Get("PlanEdit_Toast");
            _lblSmtpProfile.Text = Res.Get("PlanEdit_SmtpProfile");
            _lnkOpenSmtpSettings.Text = Res.Get("PlanEdit_SmtpLink");
            _chkNotifySuccess.Text = Res.Get("PlanEdit_NotifySuccess");
            _chkNotifyFailure.Text = Res.Get("PlanEdit_NotifyFailure");
            _lblStep6ReportHeader.Text = Res.Get("PlanEdit_Step6ReportHeader");
            _chkReportEnabled.Text = Res.Get("PlanEdit_ReportEnabled");
            _lblReportFreq.Text = Res.Get("PlanEdit_ReportFreq");
            _lblReportHour.Text = Res.Get("PlanEdit_ReportHour");
            _lblReportEmail.Text = Res.Get("PlanEdit_ReportEmail");

            // Navigation
            _btnCancel.Text = Res.Get("PlanEdit_Cancel");
            _btnSave.Text = Res.Get("PlanEdit_Save");
            _btnNext.Text = Res.Get("PlanEdit_Next");
            _btnBack.Text = Res.Get("PlanEdit_Back");

            // Tooltips
            _toolTip.SetToolTip(_txtPlanName, Res.Get("PlanEdit_Tip_PlanName"));
            _toolTip.SetToolTip(_chkEnabled, Res.Get("PlanEdit_Tip_Enabled"));
            _toolTip.SetToolTip(_txtLocalPath, Res.Get("PlanEdit_Tip_LocalPath"));
            _toolTip.SetToolTip(_txtServer, Res.Get("PlanEdit_Tip_Server"));
            _toolTip.SetToolTip(_cmbAuthMode, Res.Get("PlanEdit_Tip_AuthMode"));
            _toolTip.SetToolTip(_txtSqlUser, Res.Get("PlanEdit_Tip_SqlUser"));
            _toolTip.SetToolTip(_txtSqlPassword, Res.Get("PlanEdit_Tip_SqlPassword"));
            _toolTip.SetToolTip(_nudTimeout, Res.Get("PlanEdit_Tip_Timeout"));
            _toolTip.SetToolTip(_chkTrustCert, Res.Get("PlanEdit_Tip_TrustCert"));
            _toolTip.SetToolTip(_btnTestSql, Res.Get("PlanEdit_Tip_TestSql"));
            _toolTip.SetToolTip(_chkSelectAll, Res.Get("PlanEdit_Tip_SelectAll"));
            _toolTip.SetToolTip(_btnRefreshDatabases, Res.Get("PlanEdit_Tip_RefreshDatabases"));
            _toolTip.SetToolTip(_clbDatabases, Res.Get("PlanEdit_Tip_Databases"));
            _toolTip.SetToolTip(_chkFileBackupEnabled, Res.Get("PlanEdit_Tip_FileBackupEnabled"));
            _toolTip.SetToolTip(_lvFileSources, Res.Get("PlanEdit_Tip_FileSources"));
            _toolTip.SetToolTip(_cmbStrategy, Res.Get("PlanEdit_Tip_Strategy"));
            _toolTip.SetToolTip(_cronFull, Res.Get("PlanEdit_Tip_FullCron"));
            _toolTip.SetToolTip(_cronDiff, Res.Get("PlanEdit_Tip_DiffCron"));
            _toolTip.SetToolTip(_cronIncr, Res.Get("PlanEdit_Tip_IncrCron"));
            _toolTip.SetToolTip(_nudAutoPromote, Res.Get("PlanEdit_Tip_AutoPromote"));
            _toolTip.SetToolTip(_chkVerify, Res.Get("PlanEdit_Tip_Verify"));
            _toolTip.SetToolTip(_cmbAlgorithm, Res.Get("PlanEdit_Tip_Algorithm"));
            _toolTip.SetToolTip(_cmbLevel, Res.Get("PlanEdit_Tip_Level"));
            _toolTip.SetToolTip(_txtArchivePassword, Res.Get("PlanEdit_Tip_ArchivePassword"));
            _toolTip.SetToolTip(_cmbRetentionTemplate, Res.Get("PlanEdit_Tip_RetentionTemplate"));
            _toolTip.SetToolTip(_cmbRetention, Res.Get("PlanEdit_Tip_Retention"));
            _toolTip.SetToolTip(_nudKeepLastN, Res.Get("PlanEdit_Tip_KeepLastN"));
            _toolTip.SetToolTip(_nudDeleteDays, Res.Get("PlanEdit_Tip_DeleteDays"));
            _toolTip.SetToolTip(_chkProtectPlan, Res.Get("PlanEdit_Tip_ProtectPlan"));
            _toolTip.SetToolTip(_txtPlanPassword, Res.Get("PlanEdit_Tip_PlanPassword"));
            _toolTip.SetToolTip(_txtRecoveryPassword, Res.Get("PlanEdit_Tip_RecoveryPassword"));
            _toolTip.SetToolTip(_lvCloudTargets, Res.Get("PlanEdit_Tip_CloudTargets"));
            _toolTip.SetToolTip(_chkEmailEnabled, Res.Get("PlanEdit_Tip_EmailEnabled"));
            _toolTip.SetToolTip(_chkToast, Res.Get("PlanEdit_Tip_Toast"));
            _toolTip.SetToolTip(_chkReportEnabled, Res.Get("PlanEdit_Tip_ReportEnabled"));
            _toolTip.SetToolTip(_cmbReportFreq, Res.Get("PlanEdit_Tip_ReportFreq"));
            _toolTip.SetToolTip(_nudReportHour, Res.Get("PlanEdit_Tip_ReportHour"));
            _toolTip.SetToolTip(_txtReportEmail, Res.Get("PlanEdit_Tip_ReportEmail"));
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
    }
}
