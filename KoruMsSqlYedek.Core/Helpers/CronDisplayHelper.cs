using System;
using System.Globalization;
using System.Linq;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Quartz cron ifadesini okunabilir metne çevirir.
    /// Format: saniye dakika saat günAy ay günHafta [yıl]
    /// </summary>
    public static class CronDisplayHelper
    {
        private static readonly string[] TrDays = { "Pzr", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt" };
        private static readonly string[] EnDays = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        private static readonly string[] TrDaysFull =
            { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };
        private static readonly string[] EnDaysFull =
            { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        /// <summary>
        /// Cron ifadesini okunabilir metne çevirir.
        /// Desteklenen desenler: günlük, haftalık, belirli günler, saatlik, dakikalık.
        /// </summary>
        public static string ToReadableText(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return "—";

            bool isTurkish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr";

            try
            {
                string[] parts = cronExpression.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6)
                    return cronExpression;

                string sec = parts[0];
                string min = parts[1];
                string hour = parts[2];
                string dayOfMonth = parts[3];
                string month = parts[4];
                string dayOfWeek = parts[5];

                // Saatlik: 0 0 * ? * * veya 0 30 * ? * *
                if (hour == "*" && dayOfMonth == "?" && month == "*" && dayOfWeek == "*")
                {
                    if (min == "0")
                        return isTurkish ? "Her saat başı" : "Every hour";

                    return isTurkish
                        ? $"Her saat, dakika {min}"
                        : $"Every hour at minute {min}";
                }

                // Her X saat: 0 0 0/2 ? * * veya 0 0 */3 ? * *
                if (IsInterval(hour, out int hourInterval) && dayOfMonth == "?" && month == "*" && dayOfWeek == "*")
                {
                    return isTurkish
                        ? $"Her {hourInterval} saatte bir"
                        : $"Every {hourInterval} hours";
                }

                // Dakikalık: 0 0/5 * ? * *
                if (hour == "*" && IsInterval(min, out int minInterval) && dayOfMonth == "?" && month == "*")
                {
                    return isTurkish
                        ? $"Her {minInterval} dakikada bir"
                        : $"Every {minInterval} minutes";
                }

                // Saat ve dakika sabit mi?
                int h = 0, m = 0;
                bool hasFixedTime = int.TryParse(hour, out h) && int.TryParse(min, out m);
                string timeStr = hasFixedTime ? $"{h:D2}:{m:D2}" : null;

                // Her gün: 0 30 2 ? * * veya 0 30 2 * * ?
                if (hasFixedTime && IsEveryDay(dayOfMonth, dayOfWeek, month))
                {
                    return isTurkish
                        ? $"Her gün saat {timeStr}"
                        : $"Daily at {timeStr}";
                }

                // Belirli haftanın günleri: 0 0 2 ? * MON-FRI veya 0 0 2 ? * 2-6
                if (hasFixedTime && dayOfMonth == "?" && month == "*" && dayOfWeek != "*" && dayOfWeek != "?")
                {
                    string daysText = ParseDayOfWeek(dayOfWeek, isTurkish);
                    if (daysText != null)
                    {
                        return isTurkish
                            ? $"{daysText} saat {timeStr}"
                            : $"{daysText} at {timeStr}";
                    }
                }

                // Ayın belirli günü: 0 0 3 1 * ?  (Her ayın 1'i saat 03:00)
                if (hasFixedTime && int.TryParse(dayOfMonth, out int dom) && month == "*"
                    && (dayOfWeek == "?" || dayOfWeek == "*"))
                {
                    return isTurkish
                        ? $"Her ayın {dom}. günü saat {timeStr}"
                        : $"Monthly on the {Ordinal(dom)} at {timeStr}";
                }

                // Ayın belirli günleri (virgüllü): 0 0 3 1,15 * ?
                if (hasFixedTime && dayOfMonth.Contains(",") && month == "*"
                    && (dayOfWeek == "?" || dayOfWeek == "*"))
                {
                    return isTurkish
                        ? $"Her ayın {dayOfMonth}. günleri saat {timeStr}"
                        : $"Monthly on days {dayOfMonth} at {timeStr}";
                }

                return cronExpression;
            }
            catch
            {
                return cronExpression;
            }
        }

        private static bool IsEveryDay(string dayOfMonth, string dayOfWeek, string month)
        {
            if (month != "*") return false;

            // 0 0 2 ? * *  veya  0 0 2 * * ?  veya 0 0 2 1/1 * ?
            return (dayOfMonth == "?" && (dayOfWeek == "*" || dayOfWeek == "?"))
                || (dayOfMonth == "*" && (dayOfWeek == "?" || dayOfWeek == "*"))
                || (dayOfMonth == "1/1" && (dayOfWeek == "?" || dayOfWeek == "*"));
        }

        private static bool IsInterval(string field, out int interval)
        {
            interval = 0;
            if (field == null) return false;

            // */3 veya 0/3
            int slashIdx = field.IndexOf('/');
            if (slashIdx > 0 && slashIdx < field.Length - 1)
            {
                return int.TryParse(field.Substring(slashIdx + 1), out interval) && interval > 1;
            }

            return false;
        }

        private static string ParseDayOfWeek(string dow, bool isTurkish)
        {
            string[] daysFull = isTurkish ? TrDaysFull : EnDaysFull;

            // Quartz: 1=SUN ... 7=SAT  veya SUN, MON, TUE...
            // Aralık: MON-FRI, 2-6
            // Virgüllü: MON,WED,FRI

            string normalized = dow.ToUpperInvariant()
                .Replace("SUN", "1").Replace("MON", "2").Replace("TUE", "3")
                .Replace("WED", "4").Replace("THU", "5").Replace("FRI", "6").Replace("SAT", "7");

            // Tek gün
            if (int.TryParse(normalized, out int singleDay) && singleDay >= 1 && singleDay <= 7)
            {
                return isTurkish
                    ? $"Her {daysFull[singleDay - 1]}"
                    : $"Every {daysFull[singleDay - 1]}";
            }

            // Aralık: 2-6 (Pzt-Cum)
            if (normalized.Contains("-"))
            {
                var rangeParts = normalized.Split('-');
                if (rangeParts.Length == 2
                    && int.TryParse(rangeParts[0], out int from)
                    && int.TryParse(rangeParts[1], out int to)
                    && from >= 1 && from <= 7 && to >= 1 && to <= 7)
                {
                    // Pzt-Cum özel durumu
                    if (from == 2 && to == 6)
                        return isTurkish ? "Hafta içi her gün" : "Weekdays";

                    string fromName = isTurkish ? TrDays[from - 1] : EnDays[from - 1];
                    string toName = isTurkish ? TrDays[to - 1] : EnDays[to - 1];
                    return $"{fromName}–{toName}";
                }
            }

            // Virgüllü: 2,4,6 (Pzt,Çar,Cum)
            if (normalized.Contains(","))
            {
                var dayNums = normalized.Split(',');
                var dayNames = dayNums
                    .Select(d => int.TryParse(d.Trim(), out int n) && n >= 1 && n <= 7
                        ? (isTurkish ? TrDays[n - 1] : EnDays[n - 1])
                        : null)
                    .Where(n => n != null)
                    .ToArray();

                if (dayNames.Length > 0)
                {
                    return isTurkish
                        ? $"Her {string.Join(", ", dayNames)}"
                        : $"Every {string.Join(", ", dayNames)}";
                }
            }

            return null;
        }

        private static string Ordinal(int n)
        {
            if (n % 100 >= 11 && n % 100 <= 13) return n + "th";
            switch (n % 10)
            {
                case 1: return n + "st";
                case 2: return n + "nd";
                case 3: return n + "rd";
                default: return n + "th";
            }
        }
    }
}
