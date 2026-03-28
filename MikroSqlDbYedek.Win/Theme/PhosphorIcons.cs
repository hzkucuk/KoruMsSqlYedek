using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Phosphor Icons (MIT) — goemulu TTF font'tan renkli ikon Bitmap uretir.
    /// Fill ve Bold agirliklari desteklenir.
    /// </summary>
    internal static class PhosphorIcons
    {
        private static readonly PrivateFontCollection _fontCollection = new();
        private static FontFamily _fillFamily;
        private static FontFamily _boldFamily;
        private static bool _initialized;
        private static readonly object _lock = new();
        private static readonly Dictionary<string, Bitmap> _cache = new();

        // ─── Icon karakter kodlari (Fill) ───
        public const char Play = '\ue3d0';
        public const char Stop = '\ue46c';
        public const char FloppyDisk = '\ue248';
        public const char XCircle = '\ue4f8';
        public const char Gear = '\ue270';
        public const char Folder = '\ue24a';
        public const char ArrowsClockwise = '\ue094';
        public const char ArrowClockwise = '\ue036';
        public const char Export = '\ueaf0';
        public const char Trash = '\ue4a6';
        public const char PlusCircle = '\ue3d6';
        public const char PencilSimple = '\ue3b4';
        public const char NotePencil = '\ue34c';
        public const char Database = '\ue1de';
        public const char Cloud = '\ue1aa';
        public const char Envelope = '\ue214';
        public const char Clock = '\ue19a';
        public const char CheckCircle = '\ue184';
        public const char Warning = '\ue4e0';
        public const char Info = '\ue2ce';
        public const char MagnifyingGlass = '\ue30c';
        public const char Funnel = '\ue266';
        public const char FileText = '\ue23a';
        public const char ShieldCheck = '\ue40c';
        public const char Download = '\ue20a';
        public const char Upload = '\ue4be';
        public const char Bell = '\ue0ce';
        public const char Eye = '\ue220';
        public const char Power = '\ue3da';
        public const char HardDrive = '\ue29e';
        public const char Plug = '\ue946';
        public const char Eraser = '\ue21e';

        /// <summary>Font'lari embedded resource'dan yukler.</summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;

                _fillFamily = LoadEmbeddedFont("MikroSqlDbYedek.Win.Resources.Fonts.Phosphor-Fill.ttf");
                _boldFamily = LoadEmbeddedFont("MikroSqlDbYedek.Win.Resources.Fonts.Phosphor-Bold.ttf");
                _initialized = true;
            }
        }

        private static FontFamily LoadEmbeddedFont(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded font not found: {resourceName}");

            byte[] fontData = new byte[stream.Length];
            stream.ReadExactly(fontData, 0, fontData.Length);

            IntPtr ptr = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, ptr, fontData.Length);
            _fontCollection.AddMemoryFont(ptr, fontData.Length);
            // Not: ptr serbest birakilmaz — PrivateFontCollection yasam suresi boyunca gerekli

            return _fontCollection.Families[_fontCollection.Families.Length - 1];
        }

        /// <summary>
        /// Belirtilen ikonu verilen renk ve boyutta Bitmap olarak uretir.
        /// </summary>
        /// <param name="icon">Icon karakter kodu (orn. PhosphorIcons.Play)</param>
        /// <param name="color">Ikon rengi</param>
        /// <param name="size">Bitmap boyutu (kare, piksel)</param>
        /// <param name="useBold">true ise Bold, false ise Fill agirligi</param>
        public static Bitmap Render(char icon, Color color, int size = 20, bool useBold = false)
        {
            string key = $"{icon}_{color.ToArgb()}_{size}_{useBold}";
            lock (_cache)
            {
                if (_cache.TryGetValue(key, out var cached))
                    return cached;
            }

            EnsureInitialized();

            var family = useBold ? _boldFamily : _fillFamily;
            var bmp = new Bitmap(size, size);
            bmp.SetResolution(96, 96);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.CompositingQuality = CompositingQuality.HighQuality;

                float fontSize = size * 0.75f;
                using var font = new Font(family, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                string text = icon.ToString();

                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                var rect = new RectangleF(0, 0, size, size);
                using var brush = new SolidBrush(color);
                g.DrawString(text, font, brush, rect, sf);
            }

            lock (_cache)
            {
                _cache[key] = bmp;
            }
            return bmp;
        }

        /// <summary>
        /// Tema rengine gore standart boyutta ikon uretir.
        /// </summary>
        public static Bitmap Get(char icon, int size = 18)
        {
            return Render(icon, ModernTheme.TextPrimary, size);
        }

        /// <summary>
        /// Accent (yesil) renkli ikon uretir.
        /// </summary>
        public static Bitmap GetAccent(char icon, int size = 18)
        {
            return Render(icon, ModernTheme.AccentPrimary, size);
        }

        /// <summary>
        /// Tehlike (kirmizi) renkli ikon uretir.
        /// </summary>
        public static Bitmap GetDanger(char icon, int size = 18)
        {
            return Render(icon, ModernTheme.StatusError, size);
        }

        /// <summary>
        /// Uyari (sari/turuncu) renkli ikon uretir.
        /// </summary>
        public static Bitmap GetWarning(char icon, int size = 18)
        {
            return Render(icon, Color.FromArgb(255, 183, 77), size);
        }

        /// <summary>
        /// Bilgi (mavi) renkli ikon uretir.
        /// </summary>
        public static Bitmap GetInfo(char icon, int size = 18)
        {
            return Render(icon, Color.FromArgb(66, 165, 245), size);
        }

        /// <summary>Tum cache'i temizler (tema degisikliginde cagrilir).</summary>
        public static void ClearCache()
        {
            lock (_cache)
            {
                foreach (var bmp in _cache.Values)
                    bmp.Dispose();
                _cache.Clear();
            }
        }
    }
}
