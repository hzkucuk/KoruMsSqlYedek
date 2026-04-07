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
        public PlanEditForm(IPlanManager planManager, ISqlBackupService sqlBackupService, IAppSettingsManager settingsManager, BackupPlan? existingPlan)
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
