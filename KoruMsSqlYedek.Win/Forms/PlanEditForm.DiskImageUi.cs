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
        private GroupBox _grpDiskImage = null!;

        /// <summary>
        /// Disk İmajı GroupBox'ını Step 2 paneline ekler.
        /// OnLoad'dan önce InitializeComponent tamamlanmış olmalı.
        /// </summary>
        internal void BuildDiskImageUi()
        {
            // GroupBox
            _grpDiskImage = new GroupBox
            {
                Text = "Disk İmajı Yedekleme (wbadmin)",
                Font = new Font(_pnlStep2.Font, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 160,
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

            _grpDiskImage.Controls.Add(_chkDiskImageEnabled);
            _grpDiskImage.Controls.Add(lblVolumes);
            _grpDiskImage.Controls.Add(_clbDiskVolumes);
            _grpDiskImage.Controls.Add(_btnRefreshVolumes);

            _pnlStep2.Controls.Add(_grpDiskImage);

            RefreshAvailableVolumes();
            UpdateDiskImageFieldsVisibility();
        }

        private void UpdateDiskImageFieldsVisibility()
        {
            bool enabled = _chkDiskImageEnabled?.Checked ?? false;
            if (_clbDiskVolumes != null) _clbDiskVolumes.Enabled = enabled;
            if (_btnRefreshVolumes != null) _btnRefreshVolumes.Enabled = enabled;
        }

        private void OnRefreshVolumesClick(object? sender, EventArgs e)
        {
            RefreshAvailableVolumes();
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
                var paths = cfg.Sources.Where(s => s.IsEnabled).Select(s => s.VolumePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _clbDiskVolumes.Items.Count; i++)
                {
                    if (_clbDiskVolumes.Items[i] is VolumeItem vi)
                        _clbDiskVolumes.SetItemChecked(i, paths.Contains(vi.VolumePath));
                }
            }

            UpdateDiskImageFieldsVisibility();
        }

        internal void SaveDiskImageFromUi()
        {
            if (_chkDiskImageEnabled == null) return;

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

            _plan.DiskImageBackup.Sources = selectedPaths
                .Select(p => new DiskImageSource { VolumePath = p, DisplayName = p, IsEnabled = true })
                .ToList();
        }

        // ----- Helper -----

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
