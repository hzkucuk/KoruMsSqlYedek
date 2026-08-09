using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// PlanEditForm — Disk İmajı yedekleme UI bölümü (Step 2 / Kaynaklar).
    /// Runtime'da Step2 panel'ine GroupBox olarak eklenir; Designer'a dokunulmaz.
    /// </summary>
    partial class PlanEditForm
    {
        // ----- Runtime controls -----
        private CheckBox _chkDiskImageEnabled = null!;
        private CheckedListBox _clbDiskVolumes = null!;
        private Button _btnRefreshVolumes = null!;
        private Button _btnAddFolder = null!;
        private GroupBox _grpDiskImage = null!;

        /// <summary>
        /// Kullanıcının elle eklediği klasörler. Sürücü taraması listeyi yeniden kurduğu için
        /// bunlar ayrıca tutulur; aksi halde "Sürücüleri Tara" her basıldığında silinirlerdi.
        /// </summary>
        private readonly List<string> _customFolders = new List<string>();
        private ComboBox _cmbDiskCompression = null!;
        private Label _lblDiskCompression = null!;

        /// <summary>Plus eklentisi kurulu mu? Değilse bölüm salt gösterim amaçlıdır.</summary>
        private bool _plusInstalled;

        /// <summary>
        /// Disk İmajı GroupBox'ını Step 2 paneline ekler.
        /// OnLoad'dan önce InitializeComponent tamamlanmış olmalı.
        /// </summary>
        internal void BuildDiskImageUi()
        {
            // GroupBox
            _plusInstalled = KoruMsSqlYedek.Engine.Plugins.PlusAvailability.IsPlusInstalled();

            _grpDiskImage = new GroupBox
            {
                Text = _plusInstalled
                    ? "Disk İmajı Yedekleme (wimlib)"
                    : "Disk İmajı Yedekleme — Plus sürümü",
                Font = new Font(_pnlStep2.Font, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 205,
                Padding = new Padding(8)
            };

            // Enable checkbox
            _chkDiskImageEnabled = new CheckBox
            {
                Text = "Disk imajı yedeklemeyi etkinleştir",
                AutoSize = true,
                Location = new Point(10, 24),
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };
            _chkDiskImageEnabled.CheckedChanged += (_, _) => UpdateDiskImageFieldsVisibility();

            // Volume list label
            var lblVolumes = new Label
            {
                Text = "Yedeklenecek sürücüler:",
                AutoSize = true,
                Location = new Point(10, 50),
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };

            // CheckedListBox for volumes
            _clbDiskVolumes = new CheckedListBox
            {
                Location = new Point(10, 70),
                Size = new Size(340, 65),
                CheckOnClick = true,
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };

            // Refresh button
            _btnRefreshVolumes = new Button
            {
                Text = "Sürücüleri Tara",
                Location = new Point(360, 70),
                Size = new Size(110, 28),
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };
            _btnRefreshVolumes.Click += OnRefreshVolumesClick;

            // Klasör ekleme — wimlib kaynak olarak herhangi bir dizini kabul eder,
            // tüm sürücü olmak zorunda değildir. (Tek dosya desteklenmez: "Expected a directory".)
            _btnAddFolder = new Button
            {
                Text = "Klasör Ekle",
                Location = new Point(360, 104),
                Size = new Size(110, 28),
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };
            _btnAddFolder.Click += OnAddFolderClick;

            _grpDiskImage.Controls.Add(_btnAddFolder);

            // Sıkıştırma seviyesi — imaj boyutunu belirleyen asıl ayar
            _lblDiskCompression = new Label
            {
                Text = "Sıkıştırma:",
                AutoSize = true,
                Location = new Point(10, 148),
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };

            _cmbDiskCompression = new ComboBox
            {
                Location = new Point(90, 144),
                Size = new Size(380, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(_pnlStep2.Font, FontStyle.Regular)
            };
            _cmbDiskCompression.Items.AddRange(new object[]
            {
                new CompressionItem(DiskImageCompression.Solid, "Solid (LZMS) — en küçük dosya, en yavaş (önerilen)"),
                new CompressionItem(DiskImageCompression.Max,   "Maksimum (LZX) — dengeli"),
                new CompressionItem(DiskImageCompression.Fast,  "Hızlı (XPRESS) — büyük dosya, hızlı"),
                new CompressionItem(DiskImageCompression.None,  "Sıkıştırma yok — en hızlı, en büyük")
            });
            _cmbDiskCompression.SelectedIndex = 0;

            var lblCompressionHint = new Label
            {
                Text = "Solid, LZX'e göre tipik olarak %25-35 daha küçük imaj üretir; karşılığında 2-4 kat uzun sürer.\r\n" +
                       "Solid imajlar Windows 8.1 ve üzeri gerektirir.",
                AutoSize = true,
                Location = new Point(10, 172),
                ForeColor = SystemColors.GrayText,
                Font = new Font(_pnlStep2.Font.FontFamily, _pnlStep2.Font.Size - 0.5f, FontStyle.Regular)
            };

            _grpDiskImage.Controls.Add(_chkDiskImageEnabled);
            _grpDiskImage.Controls.Add(lblVolumes);
            _grpDiskImage.Controls.Add(_clbDiskVolumes);
            _grpDiskImage.Controls.Add(_btnRefreshVolumes);
            _grpDiskImage.Controls.Add(_lblDiskCompression);
            _grpDiskImage.Controls.Add(_cmbDiskCompression);
            _grpDiskImage.Controls.Add(lblCompressionHint);

            _pnlStep2.Controls.Add(_grpDiskImage);

            RefreshAvailableVolumes();
            UpdateDiskImageFieldsVisibility();
        }

        private void UpdateDiskImageFieldsVisibility()
        {
            // Plus kurulu değilse tüm bölüm salt gösterim: kullanılamayacak bir özellik açtırılmaz
            if (!_plusInstalled)
            {
                if (_chkDiskImageEnabled != null) _chkDiskImageEnabled.Enabled = false;
                if (_clbDiskVolumes != null) _clbDiskVolumes.Enabled = false;
                if (_btnRefreshVolumes != null) _btnRefreshVolumes.Enabled = false;
                if (_btnAddFolder != null) _btnAddFolder.Enabled = false;
                if (_cmbDiskCompression != null) _cmbDiskCompression.Enabled = false;
                if (_lblDiskCompression != null) _lblDiskCompression.Enabled = false;
                return;
            }

            bool enabled = _chkDiskImageEnabled?.Checked ?? false;
            if (_clbDiskVolumes != null) _clbDiskVolumes.Enabled = enabled;
            if (_btnRefreshVolumes != null) _btnRefreshVolumes.Enabled = enabled;
            if (_btnAddFolder != null) _btnAddFolder.Enabled = enabled;
            if (_cmbDiskCompression != null) _cmbDiskCompression.Enabled = enabled;
            if (_lblDiskCompression != null) _lblDiskCompression.Enabled = enabled;
        }

        private void OnRefreshVolumesClick(object? sender, EventArgs e)
        {
            RefreshAvailableVolumes();
        }

        private void OnAddFolderClick(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "İmajı alınacak klasörü seçin",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string path = dlg.SelectedPath?.TrimEnd('\\', '/');
            if (string.IsNullOrWhiteSpace(path)) return;

            bool alreadyListed =
                _customFolders.Contains(path, StringComparer.OrdinalIgnoreCase) ||
                GetSelectedVolumePaths().Contains(path, StringComparer.OrdinalIgnoreCase);

            if (alreadyListed)
            {
                Theme.ModernMessageBox.Show("Bu klasör zaten listede.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _customFolders.Add(path);
            _clbDiskVolumes.Items.Add(new VolumeItem(path, $"{path}  (klasör)"), true);
        }

        /// <summary>
        /// Sistemdeki sabit disk sürücülerini listeler.
        /// </summary>
        private void RefreshAvailableVolumes()
        {
            if (_clbDiskVolumes == null) return;

            var selected = GetSelectedVolumePaths();
            _clbDiskVolumes.Items.Clear();

            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (drive.DriveType != System.IO.DriveType.Fixed) continue;

                string label = string.IsNullOrEmpty(drive.VolumeLabel)
                    ? drive.Name.TrimEnd('\\')
                    : $"{drive.Name.TrimEnd('\\')} ({drive.VolumeLabel})";

                string volumePath = drive.Name.TrimEnd('\\', '/');
                _clbDiskVolumes.Items.Add(new VolumeItem(volumePath, label),
                    selected.Contains(volumePath, StringComparer.OrdinalIgnoreCase));
            }

            // Elle eklenen klasörler tarama sonrası geri konur
            foreach (string folder in _customFolders)
            {
                _clbDiskVolumes.Items.Add(new VolumeItem(folder, $"{folder}  (klasör)"),
                    selected.Contains(folder, StringComparer.OrdinalIgnoreCase));
            }
        }

        private List<string> GetSelectedVolumePaths()
        {
            var result = new List<string>();
            if (_clbDiskVolumes == null) return result;
            for (int i = 0; i < _clbDiskVolumes.Items.Count; i++)
            {
                if (_clbDiskVolumes.GetItemChecked(i) && _clbDiskVolumes.Items[i] is VolumeItem vi)
                    result.Add(vi.VolumePath);
            }
            return result;
        }

        // ----- Load / Save -----

        internal void LoadDiskImageToUi()
        {
            if (_chkDiskImageEnabled == null) return;

            var cfg = _plan.DiskImageBackup;
            _chkDiskImageEnabled.Checked = cfg?.IsEnabled ?? false;

            if (cfg?.Sources != null && cfg.Sources.Count > 0)
            {
                // Sürücü listesinde olmayan kaynaklar elle eklenmiş klasörlerdir; geri yüklenmeli
                var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _clbDiskVolumes.Items.Count; i++)
                {
                    if (_clbDiskVolumes.Items[i] is VolumeItem vi)
                        listed.Add(vi.VolumePath);
                }

                foreach (var src in cfg.Sources)
                {
                    if (string.IsNullOrWhiteSpace(src.VolumePath)) continue;
                    if (listed.Contains(src.VolumePath)) continue;

                    _customFolders.Add(src.VolumePath);
                    _clbDiskVolumes.Items.Add(new VolumeItem(src.VolumePath, $"{src.VolumePath}  (klasör)"), false);
                }

                var paths = cfg.Sources.Where(s => s.IsEnabled).Select(s => s.VolumePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _clbDiskVolumes.Items.Count; i++)
                {
                    if (_clbDiskVolumes.Items[i] is VolumeItem vi)
                        _clbDiskVolumes.SetItemChecked(i, paths.Contains(vi.VolumePath));
                }
            }

            SelectCompression(cfg?.Compression ?? DiskImageCompression.Solid);

            UpdateDiskImageFieldsVisibility();
        }

        /// <summary>
        /// Kayıtlı sıkıştırma seviyesini listede seçer.
        /// Eşleşme bulunamazsa (ileride enum'a değer eklenirse) Solid'e döner.
        /// </summary>
        private void SelectCompression(DiskImageCompression compression)
        {
            if (_cmbDiskCompression == null) return;

            for (int i = 0; i < _cmbDiskCompression.Items.Count; i++)
            {
                if (_cmbDiskCompression.Items[i] is CompressionItem ci && ci.Value == compression)
                {
                    _cmbDiskCompression.SelectedIndex = i;
                    return;
                }
            }

            _cmbDiskCompression.SelectedIndex = 0;
        }

        internal void SaveDiskImageFromUi()
        {
            if (_chkDiskImageEnabled == null) return;

            // Plus kurulu değilken plandaki imaj ayarlarına DOKUNULMAZ.
            // Aksi halde Plus'lı bir kurulumda oluşturulmuş plan, ücretsiz sürümde
            // açılıp kaydedildiğinde sürücü seçimleri ve sıkıştırma ayarı sessizce silinirdi.
            if (!_plusInstalled) return;

            bool enabled = _chkDiskImageEnabled.Checked;
            var selectedPaths = GetSelectedVolumePaths();

            if (!enabled && selectedPaths.Count == 0)
            {
                _plan.DiskImageBackup = null;
                return;
            }

            if (_plan.DiskImageBackup == null)
                _plan.DiskImageBackup = new DiskImageBackupConfig();

            _plan.DiskImageBackup.IsEnabled = enabled;
            _plan.DiskImageBackup.Format = DiskImageFormat.Wim;

            if (_cmbDiskCompression?.SelectedItem is CompressionItem ci)
                _plan.DiskImageBackup.Compression = ci.Value;

            _plan.DiskImageBackup.Sources = selectedPaths
                .Select(p => new DiskImageSource { VolumePath = p, DisplayName = p, IsEnabled = true })
                .ToList();
        }

        // ----- Helper -----

        /// <summary>ComboBox öğesi — sıkıştırma seviyesini kullanıcıya görünen metinle eşler.</summary>
        private sealed class CompressionItem
        {
            public DiskImageCompression Value { get; }
            private readonly string _label;

            public CompressionItem(DiskImageCompression value, string label)
            {
                Value = value;
                _label = label;
            }

            public override string ToString() => _label;
        }

        private sealed class VolumeItem
        {
            public string VolumePath { get; }
            private readonly string _label;

            public VolumeItem(string volumePath, string label)
            {
                VolumePath = volumePath;
                _label = label;
            }

            public override string ToString() => _label;
        }
    }
}
