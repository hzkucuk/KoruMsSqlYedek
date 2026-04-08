using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace KoruMsSqlYedek.Win.Controls;

/// <summary>
/// ObjectListView türevi — grup başlık metin rengini CDDS_POSTPAINT + GDI+ ile özelleştirir.
/// SetWindowTheme("DarkMode_Explorer") aktifken explorer teması grup başlıklarını
/// kendi renkleriyle çizer; bu sınıf POSTPAINT aşamasında GDI+ ile üzerine boyar.
/// </summary>
internal sealed class ThemedObjectListView : ObjectListView
{
    // WM_REFLECT | WM_NOTIFY — .NET tarafından kontrol'e yansıtılan bildirim mesajı
    private const int OCM_NOTIFY = 0x204E;
    private const int NM_CUSTOMDRAW = -12;

    // Custom Draw aşamaları
    private const int CDDS_PREPAINT = 0x00000001;
    private const int CDDS_POSTPAINT = 0x00000002;

    // Custom Draw dönüş değerleri
    private const int CDRF_NOTIFYITEMDRAW = 0x00000020;
    private const int CDRF_NOTIFYPOSTPAINT = 0x00000010;

    // ═══════════════ NMCUSTOMDRAW offset hesaplama ═══════════════
    //
    // NMHDR: hwndFrom(ptr) + idFrom(ptr) + code(4)
    //    x64: 8+8+4 = 20 → align 8 → 24
    //    x86: 4+4+4 = 12
    // NMCUSTOMDRAW: NMHDR + drawStage(4) + [pad on x64] + hdc(ptr) + rc(16) + ...
    //    drawStage: x64=24, x86=12
    //    hdc: x64=32, x86=16

    private static readonly int NmhdrCodeOffset = nint.Size * 2;
    private static readonly int DrawStageOffset = nint.Size == 8 ? 24 : 12;
    private static readonly int HdcOffset = nint.Size == 8 ? 32 : 16;

    // LVM_GETGROUPRECT — grup başlık label rect'ini almak için
    private const int LVM_FIRST = 0x1000;
    private const int LVM_GETGROUPRECT = LVM_FIRST + 98;
    private const int LVGGR_LABEL = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, ref RECT lParam);

    private static readonly PropertyInfo? s_groupIdProp =
        typeof(ListViewGroup).GetProperty("ID", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>Grup başlık metin rengi. Null ise varsayılan kullanılır.</summary>
    public Color? GroupHeaderForeColor { get; set; }

    /// <summary>Grup başlık arka plan rengi. Null ise varsayılan kullanılır.</summary>
    public Color? GroupHeaderBackColor { get; set; }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == OCM_NOTIFY && GroupHeaderForeColor.HasValue)
        {
            nint lp = m.LParam;
            int code = Marshal.ReadInt32(lp, NmhdrCodeOffset);

            if (code == NM_CUSTOMDRAW)
            {
                int stage = Marshal.ReadInt32(lp, DrawStageOffset);

                if (stage == CDDS_PREPAINT)
                {
                    // Base'in PREPAINT'i işlemesine izin ver;
                    // NOTIFYITEMDRAW: OLV'nin satır boyaması çalışsın
                    // NOTIFYPOSTPAINT: tüm çizim bittikten sonra grup başlıklarını üzerine boyayalım
                    base.WndProc(ref m);
                    m.Result = (nint)((int)m.Result | CDRF_NOTIFYITEMDRAW | CDRF_NOTIFYPOSTPAINT);
                    return;
                }

                if (stage == CDDS_POSTPAINT)
                {
                    // Explorer teması grup başlıklarını çizdi; şimdi GDI+ ile üzerine boyuyoruz
                    PaintGroupHeaders(lp);
                    m.Result = nint.Zero;
                    return;
                }
            }
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Tüm görünür grup başlıklarını GDI+ ile özel renkle boyar.
    /// CDDS_POSTPAINT aşamasında çağrılır — explorer temasının çizimini override eder.
    /// </summary>
    private void PaintGroupHeaders(nint lpNmcd)
    {
        if (Groups.Count == 0) return;

        nint hdc = Marshal.ReadIntPtr(lpNmcd, HdcOffset);
        if (hdc == nint.Zero) return;

        using var g = Graphics.FromHdc(hdc);
        using var bgBrush = new SolidBrush(BackColor);
        using var textBrush = new SolidBrush(GroupHeaderForeColor!.Value);
        using var font = new Font(Font.FontFamily, Font.Size + 1f, FontStyle.Bold);
        using var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        foreach (ListViewGroup group in Groups)
        {
            int groupId = s_groupIdProp is not null
                ? (int)(s_groupIdProp.GetValue(group) ?? -1)
                : -1;
            if (groupId < 0) continue;

            var rect = new RECT { left = LVGGR_LABEL };
            if (SendMessage(Handle, LVM_GETGROUPRECT, groupId, ref rect) == nint.Zero)
                continue;

            var bounds = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
            if (bounds.Width <= 0 || bounds.Height <= 0) continue;

            g.FillRectangle(bgBrush, bounds);
            g.DrawString(group.Header, font, textBrush, bounds, sf);
        }
    }
}
