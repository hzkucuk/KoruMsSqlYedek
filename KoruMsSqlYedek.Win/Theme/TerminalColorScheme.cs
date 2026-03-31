using System.Drawing;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Log konsolu için terminal renk şablonu.
    /// Linux terminal esinli renk paletlerini tanımlar.
    /// Her şablon 10 log rengi + arka plan renginden oluşur.
    /// </summary>
    internal sealed class TerminalColorScheme
    {
        /// <summary>Benzersiz tanımlayıcı (ayarlarda saklanır).</summary>
        public string Id { get; }

        /// <summary>Kullanıcı dostu görünen ad.</summary>
        public string DisplayName { get; }

        // ═══════════════ LOG COLORS ═══════════════

        public Color Default { get; init; }
        public Color Timestamp { get; init; }
        public Color Success { get; init; }
        public Color Error { get; init; }
        public Color Warning { get; init; }
        public Color Info { get; init; }
        public Color Progress { get; init; }
        public Color Cloud { get; init; }
        public Color Started { get; init; }
        public Color Background { get; init; }

        public TerminalColorScheme(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;

        // ═══════════════ BUILT-IN SCHEMES ═══════════════

        /// <summary>Tüm yerleşik şablonları döndürür.</summary>
        internal static TerminalColorScheme[] GetAll() =>
        [
            Koru,
            SolarizedDark,
            Monokai,
            Dracula,
            Nord,
            GruvboxDark,
            OneDark,
            TokyoNight,
            CatppuccinMocha,
            Ubuntu,
            MatrixGreen,
            SolarizedLight,
        ];

        /// <summary>Id ile şablon arar; bulunamazsa Koru döner.</summary>
        internal static TerminalColorScheme FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return Koru;
            foreach (var s in GetAll())
                if (s.Id == id) return s;
            return Koru;
        }

        // ───────────────────────────────────────────────────────
        //  1. Koru — varsayılan (mevcut palet)
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme Koru = new("koru", "🌿 Koru (Varsayılan)")
        {
            Default    = Color.FromArgb(190, 195, 200),
            Timestamp  = Color.FromArgb( 90, 160, 120),
            Success    = Color.FromArgb( 46, 204, 113),
            Error      = Color.FromArgb(255, 107, 107),
            Warning    = Color.FromArgb(255, 193,  69),
            Info       = Color.FromArgb(116, 185, 255),
            Progress   = Color.FromArgb(  0, 210, 211),
            Cloud      = Color.FromArgb(162, 155, 254),
            Started    = Color.FromArgb( 16, 185, 129),
            Background = Color.FromArgb( 22,  24,  28),
        };

        // ───────────────────────────────────────────────────────
        //  2. Solarized Dark — Ethan Schoonover
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme SolarizedDark = new("solarized-dark", "🌅 Solarized Dark")
        {
            Default    = Color.FromArgb(131, 148, 150), // base0
            Timestamp  = Color.FromArgb( 88, 110, 117), // base01
            Success    = Color.FromArgb(133, 153,   0), // green
            Error      = Color.FromArgb(220,  50,  47), // red
            Warning    = Color.FromArgb(181, 137,   0), // yellow
            Info       = Color.FromArgb( 38, 139, 210), // blue
            Progress   = Color.FromArgb( 42, 161, 152), // cyan
            Cloud      = Color.FromArgb(108, 113, 196), // violet
            Started    = Color.FromArgb(133, 153,   0), // green
            Background = Color.FromArgb(  0,  43,  54), // base03
        };

        // ───────────────────────────────────────────────────────
        //  3. Monokai — Wimer Hazenberg
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme Monokai = new("monokai", "🎨 Monokai")
        {
            Default    = Color.FromArgb(248, 248, 242), // foreground
            Timestamp  = Color.FromArgb(117, 113, 94),  // comment
            Success    = Color.FromArgb(166, 226,  46), // green
            Error      = Color.FromArgb(249,  38, 114), // red/pink
            Warning    = Color.FromArgb(230, 219, 116), // yellow
            Info       = Color.FromArgb(102, 217, 239), // cyan
            Progress   = Color.FromArgb(102, 217, 239), // cyan
            Cloud      = Color.FromArgb(174, 129, 255), // purple
            Started    = Color.FromArgb(166, 226,  46), // green
            Background = Color.FromArgb( 39,  40,  34), // background
        };

        // ───────────────────────────────────────────────────────
        //  4. Dracula — Zeno Rocha
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme Dracula = new("dracula", "🧛 Dracula")
        {
            Default    = Color.FromArgb(248, 248, 242), // foreground
            Timestamp  = Color.FromArgb( 98, 114, 164), // comment
            Success    = Color.FromArgb( 80, 250, 123), // green
            Error      = Color.FromArgb(255,  85,  85), // red
            Warning    = Color.FromArgb(241, 250, 140), // yellow
            Info       = Color.FromArgb(139, 233, 253), // cyan
            Progress   = Color.FromArgb(139, 233, 253), // cyan
            Cloud      = Color.FromArgb(189, 147, 249), // purple
            Started    = Color.FromArgb( 80, 250, 123), // green
            Background = Color.FromArgb( 40,  42,  54), // background
        };

        // ───────────────────────────────────────────────────────
        //  5. Nord — Arctic Ice Studio
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme Nord = new("nord", "❄️ Nord")
        {
            Default    = Color.FromArgb(216, 222, 233), // nord4
            Timestamp  = Color.FromArgb( 76,  86, 106), // nord3
            Success    = Color.FromArgb(163, 190, 140), // nord14 green
            Error      = Color.FromArgb(191,  97, 106), // nord11 red
            Warning    = Color.FromArgb(235, 203, 139), // nord13 yellow
            Info       = Color.FromArgb(129, 161, 193), // nord9 blue
            Progress   = Color.FromArgb(136, 192, 208), // nord8 cyan
            Cloud      = Color.FromArgb(180, 142, 173), // nord15 purple
            Started    = Color.FromArgb(163, 190, 140), // nord14 green
            Background = Color.FromArgb( 46,  52,  64), // nord0
        };

        // ───────────────────────────────────────────────────────
        //  6. Gruvbox Dark — morhetz
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme GruvboxDark = new("gruvbox-dark", "🟫 Gruvbox Dark")
        {
            Default    = Color.FromArgb(235, 219, 178), // fg
            Timestamp  = Color.FromArgb(146, 131, 116), // gray
            Success    = Color.FromArgb(184, 187, 38),  // green
            Error      = Color.FromArgb(251,  73,  52), // red
            Warning    = Color.FromArgb(250, 189,  47), // yellow
            Info       = Color.FromArgb(131, 165, 152), // aqua
            Progress   = Color.FromArgb(142, 192, 124), // light green
            Cloud      = Color.FromArgb(211, 134, 155), // purple
            Started    = Color.FromArgb(184, 187,  38), // green
            Background = Color.FromArgb( 40,  40,  40), // bg
        };

        // ───────────────────────────────────────────────────────
        //  7. One Dark — Atom editor
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme OneDark = new("one-dark", "⚛️ One Dark")
        {
            Default    = Color.FromArgb(171, 178, 191), // foreground
            Timestamp  = Color.FromArgb( 92, 99,  112), // comment
            Success    = Color.FromArgb(152, 195,  121), // green
            Error      = Color.FromArgb(224,  108, 117), // red
            Warning    = Color.FromArgb(229, 192, 123), // yellow
            Info       = Color.FromArgb( 97, 175, 239), // blue
            Progress   = Color.FromArgb( 86, 182, 194), // cyan
            Cloud      = Color.FromArgb(198, 120, 221), // purple
            Started    = Color.FromArgb(152, 195, 121), // green
            Background = Color.FromArgb( 40,  44,  52), // background
        };

        // ───────────────────────────────────────────────────────
        //  8. Tokyo Night — enkia
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme TokyoNight = new("tokyo-night", "🌃 Tokyo Night")
        {
            Default    = Color.FromArgb(169, 177, 214), // foreground
            Timestamp  = Color.FromArgb( 86,  95, 137), // comment
            Success    = Color.FromArgb(158, 206, 106), // green
            Error      = Color.FromArgb(247, 118, 142), // red
            Warning    = Color.FromArgb(224, 175, 104), // yellow
            Info       = Color.FromArgb(125, 207, 255), // blue
            Progress   = Color.FromArgb(115, 218, 202), // cyan
            Cloud      = Color.FromArgb(187, 154, 247), // purple
            Started    = Color.FromArgb(158, 206, 106), // green
            Background = Color.FromArgb( 26,  27,  38), // background
        };

        // ───────────────────────────────────────────────────────
        //  9. Catppuccin Mocha
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme CatppuccinMocha = new("catppuccin-mocha", "🐱 Catppuccin Mocha")
        {
            Default    = Color.FromArgb(205, 214, 244), // text
            Timestamp  = Color.FromArgb(108, 112, 134), // overlay0
            Success    = Color.FromArgb(166, 227, 161), // green
            Error      = Color.FromArgb(243, 139, 168), // red
            Warning    = Color.FromArgb(249, 226, 175), // yellow
            Info       = Color.FromArgb(137, 180, 250), // blue
            Progress   = Color.FromArgb(148, 226, 213), // teal
            Cloud      = Color.FromArgb(203, 166, 247), // mauve
            Started    = Color.FromArgb(166, 227, 161), // green
            Background = Color.FromArgb( 30,  30,  46), // base
        };

        // ───────────────────────────────────────────────────────
        // 10. Ubuntu — Canonical terminal
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme Ubuntu = new("ubuntu", "🐧 Ubuntu")
        {
            Default    = Color.FromArgb(204, 204, 204), // foreground
            Timestamp  = Color.FromArgb(128, 128, 128), // bright black
            Success    = Color.FromArgb( 78, 154,   6), // green
            Error      = Color.FromArgb(204,   0,   0), // red
            Warning    = Color.FromArgb(196, 160,   0), // yellow
            Info       = Color.FromArgb( 52, 101, 164), // blue
            Progress   = Color.FromArgb(  6, 152, 154), // cyan
            Cloud      = Color.FromArgb(117,  80, 123), // purple
            Started    = Color.FromArgb( 78, 154,   6), // green
            Background = Color.FromArgb( 48,  10,  36), // bg (Ambiance)
        };

        // ───────────────────────────────────────────────────────
        // 11. Matrix Green — retro hacker
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme MatrixGreen = new("matrix-green", "💊 Matrix Green")
        {
            Default    = Color.FromArgb(  0, 204,   0), // matrix green
            Timestamp  = Color.FromArgb(  0, 120,   0), // dim green
            Success    = Color.FromArgb(  0, 255,  65), // bright green
            Error      = Color.FromArgb(255,  50,  50), // red stands out
            Warning    = Color.FromArgb(200, 255,   0), // yellow-green
            Info       = Color.FromArgb(  0, 180, 140), // teal
            Progress   = Color.FromArgb(  0, 255, 128), // bright cyan-green
            Cloud      = Color.FromArgb(  0, 220, 200), // cyan
            Started    = Color.FromArgb(  0, 255,   0), // pure green
            Background = Color.FromArgb(  5,   5,   5), // near black
        };

        // ───────────────────────────────────────────────────────
        // 12. Solarized Light — Ethan Schoonover (light variant)
        // ───────────────────────────────────────────────────────
        internal static readonly TerminalColorScheme SolarizedLight = new("solarized-light", "☀️ Solarized Light")
        {
            Default    = Color.FromArgb(101, 123, 131), // base00
            Timestamp  = Color.FromArgb(147, 161, 161), // base1
            Success    = Color.FromArgb(133, 153,   0), // green
            Error      = Color.FromArgb(220,  50,  47), // red
            Warning    = Color.FromArgb(181, 137,   0), // yellow
            Info       = Color.FromArgb( 38, 139, 210), // blue
            Progress   = Color.FromArgb( 42, 161, 152), // cyan
            Cloud      = Color.FromArgb(108, 113, 196), // violet
            Started    = Color.FromArgb(133, 153,   0), // green
            Background = Color.FromArgb(253, 246, 227), // base3
        };
    }
}
