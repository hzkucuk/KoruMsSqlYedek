using System;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    partial class PlanEditForm
    {
        #region Cloud Target Management

        private void RefreshCloudTargetList()
        {
            _lvCloudTargets.Items.Clear();
            if (_plan.CloudTargets == null) return;

            foreach (var target in _plan.CloudTargets)
            {
                var item = new ListViewItem(new[]
                {
                    target.DisplayName ?? target.Type.ToString(),
                    target.Type.ToString(),
                    target.IsEnabled ? Res.Get("Active") : Res.Get("Passive")
                });
                item.Tag = target;
                _lvCloudTargets.Items.Add(item);
            }
        }

        private void OnAddCloudTarget(object sender, EventArgs e)
        {
            var appSettings = _settingsManager.Load();
            using (var dialog = new CloudTargetEditDialog(appSettings, _settingsManager))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _plan.CloudTargets.Add(dialog.Target);
                    RefreshCloudTargetList();
                }
            }
        }

        private void OnEditCloudTarget(object sender, EventArgs e)
        {
            if (_lvCloudTargets.SelectedItems.Count == 0) return;
            var target = _lvCloudTargets.SelectedItems[0].Tag as CloudTargetConfig;
            if (target == null) return;

            var appSettings = _settingsManager.Load();
            using (var dialog = new CloudTargetEditDialog(appSettings, _settingsManager, target))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var index = _plan.CloudTargets.IndexOf(target);
                    if (index >= 0)
                    {
                        _plan.CloudTargets[index] = dialog.Target;
                    }
                    RefreshCloudTargetList();
                }
            }
        }

        private void OnRemoveCloudTarget(object sender, EventArgs e)
        {
            if (_lvCloudTargets.SelectedItems.Count == 0) return;
            var target = _lvCloudTargets.SelectedItems[0].Tag as CloudTargetConfig;
            if (target == null) return;

            var result = Theme.ModernMessageBox.Show(
                Res.Format("PlanEdit_RemoveTargetConfirm", target.DisplayName),
                Res.Get("PlanEdit_RemoveTargetTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _plan.CloudTargets.Remove(target);
                RefreshCloudTargetList();
            }
        }

        #endregion

        #region File Backup Source Management

        private void RefreshFileSourceList()
        {
            _lvFileSources.Items.Clear();
            if (_plan.FileBackup?.Sources == null) return;

            foreach (var source in _plan.FileBackup.Sources)
            {
                var item = new ListViewItem(new[]
                {
                    source.SourceName ?? "—",
                    source.SourcePath ?? "—",
                    source.UseVss ? Res.Get("YesLabel") : Res.Get("NoLabel"),
                    source.IsEnabled ? Res.Get("Active") : Res.Get("Passive")
                });
                item.Tag = source;
                _lvFileSources.Items.Add(item);
            }
        }

        private void OnAddFileSource(object sender, EventArgs e)
        {
            using (var dialog = new FileBackupSourceEditDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (_plan.FileBackup == null)
                        _plan.FileBackup = new FileBackupConfig();
                    _plan.FileBackup.Sources.Add(dialog.Source);
                    RefreshFileSourceList();
                }
            }
        }

        private void OnEditFileSource(object sender, EventArgs e)
        {
            if (_lvFileSources.SelectedItems.Count == 0) return;
            var source = _lvFileSources.SelectedItems[0].Tag as FileBackupSource;
            if (source == null) return;

            using (var dialog = new FileBackupSourceEditDialog(source))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var index = _plan.FileBackup.Sources.IndexOf(source);
                    if (index >= 0)
                    {
                        _plan.FileBackup.Sources[index] = dialog.Source;
                    }
                    RefreshFileSourceList();
                }
            }
        }

        private void OnRemoveFileSource(object sender, EventArgs e)
        {
            if (_lvFileSources.SelectedItems.Count == 0) return;
            var source = _lvFileSources.SelectedItems[0].Tag as FileBackupSource;
            if (source == null) return;

            _plan.FileBackup.Sources.Remove(source);
            RefreshFileSourceList();
        }

        #endregion
    }
}
