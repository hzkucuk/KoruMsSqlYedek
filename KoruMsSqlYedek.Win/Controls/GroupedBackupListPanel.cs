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
/// Mevcut panelleri yeniden kullanarak gereksiz kontrol oluşturmayı önler.
/// </summary>
internal sealed class GroupedBackupListPanel : Panel
{
    private readonly FlowLayoutPanel _flow;

    /// <summary>Maksimum başlangıçta açık grup sayısı (performans koruması).</summary>
    private const int MaxInitialExpanded = 5;

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
    /// Mevcut panelleri yeniden kullanır; fazla olanları kaldırır, eksik olanları ekler.
    /// </summary>
    public void SetData(IReadOnlyList<BackupResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        _flow.SuspendLayout();

        // Plan adına göre grupla
        var groups = results
            .GroupBy(r => r.PlanName ?? "—")
            .OrderBy(g => g.Key)
            .ToList();

        int childWidth = CalcChildWidth();
        int existingCount = _flow.Controls.Count;
        int groupCount = groups.Count;

        // Fazla panelleri kaldır
        for (int i = existingCount - 1; i >= groupCount; i--)
        {
            Control c = _flow.Controls[i];
            _flow.Controls.RemoveAt(i);
            c.Dispose();
        }

        // Mevcut panelleri güncelle veya yeni ekle
        for (int i = 0; i < groupCount; i++)
        {
            var group = groups[i];
            var items = group.OrderByDescending(r => r.StartedAt).ToList();

            CollapsibleGroupPanel panel;
            if (i < existingCount)
            {
                // Mevcut paneli yeniden kullan
                panel = (CollapsibleGroupPanel)_flow.Controls[i];
            }
            else
            {
                // Yeni panel oluştur
                panel = new CollapsibleGroupPanel
                {
                    Width = childWidth,
                    Margin = new Padding(0, 0, 0, 2)
                };
                panel.HeightChanged += (_, _) => _flow.PerformLayout();
                _flow.Controls.Add(panel);
            }

            panel.GroupTitle = group.Key;
            panel.Width = childWidth;
            panel.IsExpanded = i < MaxInitialExpanded;
            panel.SetItems(items);
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

    /// <summary>Container boyutu değiştiğinde alt panellerin genişliğini toplu günceller.</summary>
    private void Flow_Resize(object? sender, EventArgs e)
    {
        int w = CalcChildWidth();
        _flow.SuspendLayout();
        foreach (Control c in _flow.Controls)
            c.Width = w;
        _flow.ResumeLayout(true);
    }
}
