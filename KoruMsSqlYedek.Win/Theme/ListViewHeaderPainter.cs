using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme;

/// <summary>
/// ListView header control'ünü NativeWindow ile subclass'layarak
/// dark theme ile uyumlu kolon başlıkları çizer.
/// OwnerDraw=true kullanmadan header boyama yapar, böylece
/// native ListView group collapse/expand (+/-) butonları çalışır.
/// </summary>
internal sealed class ListViewHeaderPainter : NativeWindow, IDisposable
{
    // Win32 constants
    private const int LVM_GETHEADER = 0x101F;
    private const int WM_NOTIFY = 0x004E;
    private const int HDM_GETITEMCOUNT = 0x1200;
    private const int HDM_GETITEMRECT = 0x1207;
    private const int HDM_GETITEMW = 0x120B;
    private const int NM_CUSTOMDRAW = -12;
    private const int CDDS_PREPAINT = 0x00000001;
    private const int CDDS_POSTPAINT = 0x00000002;
    private const int CDDS_ITEMPREPAINT = 0x00010001;
    private const int CDRF_NOTIFYITEMDRAW = 0x00000020;
    private const int CDRF_NOTIFYPOSTPAINT = 0x00000010;
    private const int CDRF_SKIPDEFAULT = 0x00000004;
    private const int LVM_GETGROUPRECT = LVM_FIRST + 98;
    private const int LVGGR_LABEL = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct NMHDR
    {
        public IntPtr hwndFrom;
        public IntPtr idFrom;
        public int code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NMCUSTOMDRAW
    {
        public NMHDR hdr;
        public int dwDrawStage;
        public IntPtr hdc;
        public RECT rc;
        public IntPtr dwItemSpec;
        public uint uItemState;
        public IntPtr lItemlParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct HDITEM
    {
        public uint mask;
        public int cxy;
        public IntPtr pszText;
        public IntPtr hbm;
        public int cchTextMax;
        public int fmt;
        public IntPtr lParam;
        public int iImage;
        public int iOrder;
        public uint type;
        public IntPtr pvFilter;
        public uint state;
    }

    private const uint HDI_TEXT = 0x0002;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref HDITEM lParam);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    // ListView extended styles
    private const int LVM_FIRST = 0x1000;
    private const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
    private const int LVM_GETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 55;
    private const int LVM_ENABLEGROUPVIEW = LVM_FIRST + 157;
    private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

    private static readonly PropertyInfo? s_groupIdProp =
        typeof(ListViewGroup).GetProperty("ID", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly ListView _listView;
    private HeaderNativeWindow? _headerWindow;
    private int _sortColumn = -1;
    private bool _sortAscending;
    private bool _disposed;

    /// <summary>
    /// Ayarlanırsa tüm grup başlıkları bu renkte çizilir.
    /// </summary>
    public Color? GroupHeaderColor { get; set; }

    public ListViewHeaderPainter(ListView listView)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));

        if (_listView.IsHandleCreated)
            AttachHeader();
        else
            _listView.HandleCreated += OnListViewHandleCreated;
    }

    /// <summary>
    /// Sıralama durumunu günceller ve header'ı yeniden çizdirir.
    /// </summary>
    public void SetSortState(int column, bool ascending)
    {
        _sortColumn = column;
        _sortAscending = ascending;
        _headerWindow?.Invalidate();
    }

    /// <summary>
    /// LVM_ENABLEGROUPVIEW(TRUE) mesajını doğrudan native control'e gönderir.
    /// .NET'in ShowGroups setter'ı value==current ise mesajı yuttuğundan,
    /// Groups.Clear() sonrası tekrar çağrıda grup görünümü kayboluyordu.
    /// Bu metot .NET'in change-detection'ını bypass eder.
    /// </summary>
    public void EnableGroupView()
    {
        if (_listView.IsHandleCreated && _listView.Groups.Count > 0)
            SendMessage(_listView.Handle, LVM_ENABLEGROUPVIEW, (IntPtr)1, IntPtr.Zero);
    }

    private void OnListViewHandleCreated(object? sender, EventArgs e)
    {
        _listView.HandleCreated -= OnListViewHandleCreated;
        AttachHeader();
    }

    private void AttachHeader()
    {
        // Explorer teması — native grup başlıklarını (collapse/expand) aktifleştirir.
        // Dark modda "DarkMode_Explorer" kullanılır; grup header renkleri otomatik uyumlanır.
        string theme = Application.IsDarkModeEnabled ? "DarkMode_Explorer" : "Explorer";
        SetWindowTheme(_listView.Handle, theme, null);

        // Double-buffer flicker'ı önler
        IntPtr exStyle = SendMessage(_listView.Handle, LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero);
        SendMessage(_listView.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,
            (IntPtr)LVS_EX_DOUBLEBUFFER, (IntPtr)((int)exStyle | LVS_EX_DOUBLEBUFFER));

        IntPtr headerHwnd = SendMessage(_listView.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
        if (headerHwnd != IntPtr.Zero)
        {
            _headerWindow = new HeaderNativeWindow(this);
            _headerWindow.AssignHandle(headerHwnd);
        }

        // Parent form'un WM_NOTIFY'lerini yakalamak için parent'ı subclass'la
        if (_listView.Parent is not null && _listView.Parent.IsHandleCreated)
            AssignHandle(_listView.Parent.Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NOTIFY && m.LParam != IntPtr.Zero)
        {
            var nmhdr = Marshal.PtrToStructure<NMHDR>(m.LParam);
            if (_headerWindow is not null && nmhdr.hwndFrom == _headerWindow.Handle && nmhdr.code == NM_CUSTOMDRAW)
            {
                var nmcd = Marshal.PtrToStructure<NMCUSTOMDRAW>(m.LParam);

                if (nmcd.dwDrawStage == CDDS_PREPAINT)
                {
                    m.Result = (IntPtr)CDRF_NOTIFYITEMDRAW;
                    return;
                }

                if (nmcd.dwDrawStage == CDDS_ITEMPREPAINT)
                {
                    int itemIndex = (int)nmcd.dwItemSpec;
                    PaintHeaderItem(nmcd.hdc, itemIndex);
                    m.Result = (IntPtr)CDRF_SKIPDEFAULT;
                    return;
                }
            }

            // ListView grup başlık renklendirme — NM_CUSTOMDRAW post-paint
            if (GroupHeaderColor.HasValue && nmhdr.hwndFrom == _listView.Handle && nmhdr.code == NM_CUSTOMDRAW)
            {
                var nmcd = Marshal.PtrToStructure<NMCUSTOMDRAW>(m.LParam);
                if (nmcd.dwDrawStage == CDDS_PREPAINT)
                {
                    m.Result = (IntPtr)CDRF_NOTIFYPOSTPAINT;
                    return;
                }
                if (nmcd.dwDrawStage == CDDS_POSTPAINT)
                {
                    PaintGroupHeaders(nmcd.hdc);
                    m.Result = IntPtr.Zero;
                    return;
                }
            }
        }

        base.WndProc(ref m);
    }

    private void PaintHeaderItem(IntPtr hdc, int itemIndex)
    {
        // Header item rect'i al
        var rect = new RECT();
        SendMessage(_headerWindow!.Handle, HDM_GETITEMRECT, (IntPtr)itemIndex, ref rect);
        var bounds = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

        // Header item text'i al
        string headerText = GetHeaderItemText(itemIndex);

        using var g = Graphics.FromHdc(hdc);
        g.SetClip(bounds);

        // Arka plan
        using var bgBrush = new SolidBrush(ModernTheme.GridHeaderBack);
        g.FillRectangle(bgBrush, bounds);

        // Alt çizgi
        using var borderPen = new Pen(ModernTheme.DividerColor);
        g.DrawLine(borderPen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

        // Metin
        bool isSorted = itemIndex == _sortColumn;
        int arrowAreaWidth = isSorted ? 18 : 0;
        var textRect = new Rectangle(bounds.X + 8, bounds.Y, bounds.Width - 16 - arrowAreaWidth, bounds.Height);

        using var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var textBrush = new SolidBrush(ModernTheme.GridHeaderText);
        g.DrawString(headerText, ModernTheme.FontCaptionBold, textBrush, textRect, sf);

        // Sıralama oku
        if (isSorted)
        {
            int ax = bounds.Right - 14;
            int ay = bounds.Y + bounds.Height / 2;
            using var arrowBrush = new SolidBrush(ModernTheme.AccentPrimary);
            Point[] arrow = _sortAscending
                ? new[] { new Point(ax, ay + 3), new Point(ax + 7, ay + 3), new Point(ax + 3, ay - 3) }
                : new[] { new Point(ax, ay - 3), new Point(ax + 7, ay - 3), new Point(ax + 3, ay + 3) };
            g.FillPolygon(arrowBrush, arrow);
        }

        g.ResetClip();
    }

    private void PaintGroupHeaders(IntPtr hdc)
    {
        if (!GroupHeaderColor.HasValue || _listView.Groups.Count == 0) return;

        using var g = Graphics.FromHdc(hdc);
        using var bgBrush = new SolidBrush(_listView.BackColor);
        using var textBrush = new SolidBrush(GroupHeaderColor.Value);
        using var font = new Font(_listView.Font.FontFamily, _listView.Font.Size + 1f, FontStyle.Bold);
        using var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        foreach (ListViewGroup group in _listView.Groups)
        {
            int groupId = s_groupIdProp != null ? (int)(s_groupIdProp.GetValue(group) ?? -1) : -1;
            if (groupId < 0) continue;

            var rect = new RECT { left = LVGGR_LABEL };
            if (SendMessage(_listView.Handle, LVM_GETGROUPRECT, (IntPtr)groupId, ref rect) == IntPtr.Zero)
                continue;

            var bounds = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
            if (bounds.Width <= 0 || bounds.Height <= 0) continue;

            g.FillRectangle(bgBrush, bounds);
            g.DrawString(group.Header, font, textBrush, bounds, sf);
        }
    }

    private string GetHeaderItemText(int itemIndex)
    {
        // WinForms ListView.Columns'dan oku — daha güvenilir
        if (itemIndex >= 0 && itemIndex < _listView.Columns.Count)
            return _listView.Columns[itemIndex].Text;

        return string.Empty;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _listView.HandleCreated -= OnListViewHandleCreated;

        if (_headerWindow is not null && _headerWindow.Handle != IntPtr.Zero)
        {
            _headerWindow.ReleaseHandle();
            _headerWindow = null;
        }

        if (Handle != IntPtr.Zero)
            ReleaseHandle();
    }

    /// <summary>
    /// Header HWND'sine bağlanan iç NativeWindow — sadece invalidate amaçlı.
    /// </summary>
    private sealed class HeaderNativeWindow : NativeWindow
    {
        private readonly ListViewHeaderPainter _owner;

        public HeaderNativeWindow(ListViewHeaderPainter owner) => _owner = owner;

        public void Invalidate()
        {
            if (Handle != IntPtr.Zero)
                InvalidateRect(Handle, IntPtr.Zero, true);
        }

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    }
}
