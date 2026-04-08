using System;
using System.Runtime.InteropServices;

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
        /// Uygulama tanımlı pencere mesajı kaydeder.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint RegisterWindowMessage(string lpString);

        internal const int SW_RESTORE = 9;
        internal const int HWND_BROADCAST = 0xFFFF;

        /// <summary>
        /// Kontrol temasını değiştirir — "DarkMode_Explorer" ile dark scrollbar sağlanır.
        /// </summary>
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);
    }
}
