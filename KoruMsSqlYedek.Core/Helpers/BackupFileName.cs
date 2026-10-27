using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Yedek dosya adlarını tam yapısıyla tanır:
    /// <c>{db}_{Tür}_yyyyMMdd_HHmmss.bak|.7z</c> (<see cref="PathHelper.GenerateBackupFileName"/>,
    /// <see cref="PathHelper.GenerateArchiveFileName"/>) ve <c>Files_yyyyMMdd_HHmmss.7z</c>.
    /// Önek eşleşmesi ("{db}_Log_*") kullanılmaz: "Muhasebe" deseni "Muhasebe_Log" veritabanının
    /// dosyalarını da yakalardı.
    /// </summary>
    public sealed class BackupFileName
    {
        public const string Full = "Full";
        public const string Differential = "Differential";
        public const string Incremental = "Incremental";
        public const string Vss = "VSS";

        private const string TimestampFormat = "yyyyMMdd_HHmmss";

        private readonly Regex _regex;

        private BackupFileName(string pattern)
        {
            _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>Bu veritabanının verilen türlerdeki yedeklerini tanıyan eşleştirici.</summary>
        public static BackupFileName For(string databaseName, params string[] backupTypes)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentException("Veritabanı adı boş olamaz.", nameof(databaseName));
            if (backupTypes == null || backupTypes.Length == 0)
                throw new ArgumentException("En az bir yedek türü gerekli.", nameof(backupTypes));

            string db = Regex.Escape(PathHelper.SanitizeFileNameComponent(databaseName));
            string types = string.Join("|", backupTypes.Select(Regex.Escape));
            return new BackupFileName($@"^{db}_(?:{types})_(\d{{8}}_\d{{6}})\.(?:bak|7z)$");
        }

        /// <summary>Dosya yedeği arşivlerini (<c>Files_yyyyMMdd_HHmmss.7z</c>) tanıyan eşleştirici.</summary>
        public static BackupFileName ForFileArchives()
            => new BackupFileName(@"^Files_(\d{8}_\d{6})\.7z$");

        /// <summary>Ad eşleşirse dosya adındaki zaman damgasını (yerel saat) döndürür.</summary>
        public bool TryMatch(string fileName, out DateTime timestamp)
        {
            timestamp = default;
            if (string.IsNullOrEmpty(fileName))
                return false;

            var match = _regex.Match(fileName);
            return match.Success &&
                   DateTime.TryParseExact(match.Groups[1].Value, TimestampFormat, CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeLocal, out timestamp);
        }
    }
}
