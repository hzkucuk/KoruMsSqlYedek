using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Controls;

/// <summary>
/// Yedekleme sonuçlarını plan adına göre gruplar ve
/// her grup için <see cref="CollapsibleGroupPanel"/> oluşturur.
/// </summary>
internal sealed class GroupedBackupListPanel : Panel
{
    private readonly FlowLayoutPanel _flow;

    public GroupedBackupListPanel()
    {
        BackColor = ModernTheme.SurfaceColor;

        _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = ModernTheme.SurfaceColor,
            Padding = Padding.Empty
        };

        Controls.Add(_flow);
        _flow.Resize += Flow_Resize;
    }

    /// <summary>
    /// Yedekleme sonuçlarını plan adına göre gruplar ve panelleri oluşturur.
    /// </summary>
    public void SetData(IReadOnlyList<BackupResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        _flow.SuspendLayout();

        // Eski panelleri temizle
        foreach (Control c in _flow.Controls)
            c.Dispose();
        _flow.Controls.Clear();

        // Plan adına göre grupla
        var groups = results
            .GroupBy(r => r.PlanName ?? "—")
            .OrderBy(g => g.Key);

        int childWidth = CalcChildWidth();

        foreach (var group in groups)
        {
            CollapsibleGroupPanel panel = new()
            {
                GroupTitle = group.Key,
                Width = childWidth,
                Margin = new Padding(0, 0, 0, 2)
            };
            panel.SetItems(group.OrderByDescending(r => r.StartedAt).ToList());
            panel.HeightChanged += (_, _) => _flow.PerformLayout();
            _flow.Controls.Add(panel);
        }

        _flow.ResumeLayout(true);
    }

    /// <summary>Alt panel genişliğini hesaplar (scrollbar payı düşülür).</summary>
    private int CalcChildWidth()
    {
        int w = _flow.ClientSize.Width
              - _flow.Padding.Horizontal
              - SystemInformation.VerticalScrollBarWidth;
        return Math.Max(w, 200);
    }

    /// <summary>Container boyutu değiştiğinde alt panellerin genişliğini günceller.</summary>
    private void Flow_Resize(object? sender, EventArgs e)
    {
        int w = CalcChildWidth();
        foreach (Control c in _flow.Controls)
            c.Width = w;
    }
}
