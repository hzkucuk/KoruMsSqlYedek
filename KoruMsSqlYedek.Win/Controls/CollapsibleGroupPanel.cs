using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Controls;

/// <summary>
/// Açılır/kapanır grup paneli — yeşil renk başlık + temalı DataGridView.
/// Başlığa tıklanarak içerik açılır/kapanır.
/// </summary>
internal sealed class CollapsibleGroupPanel : Panel
{
    private const int HeaderHeight = 30;
    private const int MaxGridHeight = 350;
    private const int RowHeight = 24;

    private readonly Panel _header;
    private readonly BufferedDataGridView _grid;
    private bool _isExpanded = true;
    private string _groupTitle = "";
    private int _itemCount;

    /// <summary>Panel yüksekliği değiştiğinde üst container'ı bilgilendirir.</summary>
    internal event EventHandler HeightChanged;

    public CollapsibleGroupPanel()
    {
        BackColor = ModernTheme.SurfaceColor;

        // ── Başlık paneli ──
        _header = new Panel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight,
            BackColor = ModernTheme.SurfaceColor,
            Cursor = Cursors.Hand
        };
        _header.Paint += Header_Paint;
        _header.Click += (_, _) => ToggleExpand();
        _header.MouseEnter += (_, _) => _header.BackColor = ModernTheme.SurfaceHover;
        _header.MouseLeave += (_, _) => _header.BackColor = ModernTheme.SurfaceColor;

        // ── Veri tablosu ──
        _grid = CreateThemedGrid();
        _grid.Dock = DockStyle.Top;

        // DockStyle.Top — ekleme sırası: grid sonra header (header üstte görünür)
        SuspendLayout();
        Controls.Add(_grid);
        Controls.Add(_header);
        ResumeLayout(false);

        RecalcHeight();
    }

    public string GroupTitle
    {
        get => _groupTitle;
        set
        {
            _groupTitle = value;
            _header.Invalidate();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            _grid.Visible = _isExpanded;
            RecalcHeight();
            _header.Invalidate();
        }
    }

    /// <summary>Gruba ait yedekleme sonuçlarını tabloya yükler.</summary>
    public void SetItems(IReadOnlyList<BackupResult> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _itemCount = items.Count;

        _grid.SuspendLayout();
        _grid.Rows.Clear();

        foreach (BackupResult r in items)
        {
            long sizeBytes = r.CompressedSizeBytes > 0 ? r.CompressedSizeBytes : r.FileSizeBytes;
            int idx = _grid.Rows.Add(
                r.StartedAt.ToString("yyyy-MM-dd HH:mm"),
                r.DatabaseName ?? "—",
                GetBackupTypeName(r.BackupType),
                GetStatusName(r.Status),
                FormatFileSize(sizeBytes));

            _grid.Rows[idx].DefaultCellStyle.ForeColor = r.Status switch
            {
                BackupResultStatus.Failed => ModernTheme.StatusError,
                BackupResultStatus.PartialSuccess => ModernTheme.StatusWarning,
                BackupResultStatus.Cancelled => ModernTheme.StatusCancelled,
                _ => ModernTheme.TextPrimary
            };
        }

        _grid.ResumeLayout();
        _header.Invalidate();
        RecalcHeight();
    }

    // ═══════════════ EXPAND / COLLAPSE ═══════════════

    private void ToggleExpand() => IsExpanded = !_isExpanded;

    private void RecalcHeight()
    {
        int gridH = 0;
        if (_isExpanded && _grid.Rows.Count > 0)
        {
            gridH = _grid.ColumnHeadersHeight + _grid.Rows.Count * RowHeight + 2;
            _grid.ScrollBars = gridH > MaxGridHeight ? ScrollBars.Vertical : ScrollBars.None;
            gridH = Math.Min(gridH, MaxGridHeight);
        }

        _grid.Height = gridH;
        Height = HeaderHeight + (_isExpanded ? gridH : 0);
        HeightChanged?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════ HEADER PAINTING ═══════════════

    private void Header_Paint(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // Ok simgesi
        string arrow = _itemCount == 0 ? "○" : (_isExpanded ? "▾" : "▸");
        using Font arrowFont = new("Segoe UI", 10f);
        SizeF arrowSize = g.MeasureString(arrow, arrowFont);
        float arrowX = 12;
        float arrowY = (HeaderHeight - arrowSize.Height) / 2f;

        using SolidBrush accentBrush = new(ModernTheme.AccentPrimaryHover);
        g.DrawString(arrow, arrowFont, accentBrush, arrowX, arrowY);

        // Grup başlığı (yeşil)
        float titleX = arrowX + arrowSize.Width + 4;
        float titleY = (HeaderHeight - ModernTheme.FontBodyBold.GetHeight(g)) / 2f;
        g.DrawString(_groupTitle, ModernTheme.FontBodyBold, accentBrush, titleX, titleY);

        // Öğe sayısı (gri)
        SizeF titleSize = g.MeasureString(_groupTitle, ModernTheme.FontBodyBold);
        string countText = $"  —  {_itemCount} {Res.Get("Dashboard_GroupBackupCount")}";
        using SolidBrush secondaryBrush = new(ModernTheme.TextSecondary);
        g.DrawString(countText, ModernTheme.FontCaption, secondaryBrush,
            titleX + titleSize.Width, titleY + 1);

        // Alt çizgi
        using Pen dividerPen = new(ModernTheme.DividerColor);
        g.DrawLine(dividerPen, 0, HeaderHeight - 1, _header.Width, HeaderHeight - 1);
    }

    // ═══════════════ THEMED DATA GRID VIEW ═══════════════

    private static BufferedDataGridView CreateThemedGrid()
    {
        BufferedDataGridView dgv = new()
        {
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            BackgroundColor = ModernTheme.SurfaceColor,
            GridColor = ModernTheme.DividerColor,
            ScrollBars = ScrollBars.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 26
        };

        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ModernTheme.GridHeaderBack,
            ForeColor = ModernTheme.GridHeaderText,
            Font = ModernTheme.FontCaptionBold,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ModernTheme.SurfaceColor,
            ForeColor = ModernTheme.TextPrimary,
            Font = ModernTheme.FontCaption,
            SelectionBackColor = ModernTheme.GridSelectionBack,
            SelectionForeColor = ModernTheme.TextPrimary,
            Padding = new Padding(4, 0, 0, 0)
        };

        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ModernTheme.GridAlternateRow,
            ForeColor = ModernTheme.TextPrimary,
            SelectionBackColor = ModernTheme.GridSelectionBack,
            SelectionForeColor = ModernTheme.TextPrimary
        };

        dgv.RowTemplate.Height = RowHeight;

        // Kolonlar — Plan hariç (grup başlığı olarak gösteriliyor)
        dgv.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = Res.Get("Dashboard_ColDate"), Width = 140 },
            new DataGridViewTextBoxColumn { HeaderText = Res.Get("Dashboard_ColDatabase"), Width = 140 },
            new DataGridViewTextBoxColumn { HeaderText = Res.Get("Dashboard_ColType"), Width = 80 },
            new DataGridViewTextBoxColumn { HeaderText = Res.Get("Dashboard_ColResult"), Width = 90 },
            new DataGridViewTextBoxColumn
            {
                HeaderText = Res.Get("Dashboard_ColSize"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

        return dgv;
    }

    // ═══════════════ YARDIMCI METOTLAR ═══════════════

    private static string GetBackupTypeName(SqlBackupType type) => type switch
    {
        SqlBackupType.Full => Res.Get("Dashboard_TypeFull"),
        SqlBackupType.Differential => Res.Get("Dashboard_TypeDiff"),
        SqlBackupType.Incremental => Res.Get("Dashboard_TypeInc"),
        _ => type.ToString()
    };

    private static string GetStatusName(BackupResultStatus status) => status switch
    {
        BackupResultStatus.Success => Res.Get("Dashboard_ResultSuccess"),
        BackupResultStatus.PartialSuccess => Res.Get("Dashboard_ResultPartial"),
        BackupResultStatus.Failed => Res.Get("Dashboard_ResultFailed"),
        BackupResultStatus.Cancelled => Res.Get("Dashboard_ResultCancelled"),
        _ => status.ToString()
    };

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "—";
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
        return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
    }

    /// <summary>Flicker-free DataGridView.</summary>
    private sealed class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView() => DoubleBuffered = true;
    }
}
