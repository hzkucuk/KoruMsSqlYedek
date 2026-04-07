using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace KoruMsSqlYedek.Win.Controls;

/// <summary>
/// ObjectListView türevi — grup başlık metin rengini NM_CUSTOMDRAW ile özelleştirir.
/// <see cref="GroupHeaderForeColor"/> set edildiğinde, UseExplorerTheme=false modunda
/// grup başlık metni belirtilen renkte çizilir.
/// </summary>
internal sealed class ThemedObjectListView : ObjectListView
{
    // WM_REFLECT | WM_NOTIFY — .NET tarafından kontrol'e yansıtılan bildirim mesajı
    private const int OCM_NOTIFY = 0x204E;
    private const int NM_CUSTOMDRAW = -12;

    // Custom Draw aşamaları
    private const int CDDS_PREPAINT = 0x00000001;
    private const int CDDS_ITEMPREPAINT = 0x00010001;

    // Custom Draw dönüş değerleri
    private const int CDRF_NEWFONT = 0x00000002;
    private const int CDRF_NOTIFYITEMDRAW = 0x00000020;

    // ListView custom draw item tipi — grup başlığı
    private const int LVCDI_GROUP = 0x00000001;

    // ═══════════════ NMLVCUSTOMDRAW offset hesaplama ═══════════════
    //
    // NMHDR: hwndFrom(ptr) + idFrom(ptr) + code(4) → size = ptr==8 ? 24 : 12
    // NMCUSTOMDRAW: NMHDR + drawStage(4) + [pad?] + hdc(ptr) + rc(16) + dwItemSpec(ptr) + uItemState(4) + [pad?] + lItemlParam(ptr)
    //    x64: 24 + 4+4pad + 8 + 16 + 8 + 4+4pad + 8 = 80
    //    x86: 12 + 4 + 4 + 16 + 4 + 4 + 4 = 48
    // NMLVCUSTOMDRAW: NMCUSTOMDRAW + clrText(4) + clrTextBk(4) + iSubItem(4) + dwItemType(4)
    //    clrText offset: x64=80, x86=48
    //    dwItemType offset: clrText + 12

    private static readonly int NmhdrCodeOffset = nint.Size * 2;
    private static readonly int DrawStageOffset = nint.Size == 8 ? 24 : 12;
    private static readonly int ClrTextOffset = nint.Size == 8 ? 80 : 48;
    private static readonly int ClrTextBkOffset = ClrTextOffset + 4;
    private static readonly int DwItemTypeOffset = ClrTextOffset + 12;

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
                    // Base'in PREPAINT'i işlemesine izin ver, sonra NOTIFYITEMDRAW ekle
                    base.WndProc(ref m);
                    m.Result = (nint)((int)m.Result | CDRF_NOTIFYITEMDRAW);
                    return;
                }

                if (stage == CDDS_ITEMPREPAINT)
                {
                    int itemType = Marshal.ReadInt32(lp, DwItemTypeOffset);

                    if (itemType == LVCDI_GROUP)
                    {
                        // Grup başlık metin rengini ayarla
                        Marshal.WriteInt32(lp, ClrTextOffset,
                            ColorTranslator.ToWin32(GroupHeaderForeColor.Value));

                        if (GroupHeaderBackColor.HasValue)
                        {
                            Marshal.WriteInt32(lp, ClrTextBkOffset,
                                ColorTranslator.ToWin32(GroupHeaderBackColor.Value));
                        }

                        m.Result = (nint)CDRF_NEWFONT;
                        return;
                    }
                }
            }
        }

        base.WndProc(ref m);
    }
}
