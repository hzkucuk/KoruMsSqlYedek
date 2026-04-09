using System;
using System.Runtime.InteropServices;

#nullable enable

namespace KoruMsSqlYedek.Win
{
    /// <summary>
    /// Win32 API P/Invoke tanımları.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// CreateIconIndirect veya GetHicon ile oluşturulan ikonu yok eder.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Belirtilen pencereyi ön plana getirir.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Pencereyi gösterir veya gizler.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Alıcı pencerelere mesaj gönderir (broadcast).
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// TreeView öğe durumunu değiştirmek için TVITEM yapısı ile mesaj gönderir.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref TVITEM lParam);

        /// <summary>
        /// Uygulama tanımlı pencere mesajı kaydeder.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint RegisterWindowMessage(string lpString);

        internal const int SW_RESTORE = 9;
        internal const int HWND_BROADCAST = 0xFFFF;

        // ── TreeView Constants ──
        internal const uint TV_FIRST = 0x1100;
        internal const uint TVM_SETITEM = TV_FIRST + 63;           // TVM_SETITEMW
        internal const uint TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
        internal const uint TVS_EX_PARTIALCHECKBOXES = 0x0080;
        internal const int TVIF_STATE = 0x0008;
        internal const int TVIS_STATEIMAGEMASK = 0xF000;

        /// <summary>
        /// TreeView öğe durumu yapısı (TVITEMW).
        /// Checkbox state image ayarlamak için kullanılır.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
        }

        /// <summary>
        /// Kontrol temasını değiştirir — "DarkMode_Explorer" ile dark scrollbar sağlanır.
        /// </summary>
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);
    }
}
