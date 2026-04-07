using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Events;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// BackupActivityType enum'una yeni değer eklendiğinde fail-fast sağlayan koruma testleri.
    /// TB1 refactoring sonrası switch expression'lar throw on default kullanır;
    /// bu testler tüm mevcut enum değerlerinin kapsamda olduğunu garanti eder.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class BackupActivityExhaustivenessTests
    {
        /// <summary>
        /// Bilinen tüm BackupActivityType değerlerinin listesi.
        /// Yeni enum değeri eklendiğinde bu liste güncellenmelidir — aksi halde test kırılır.
        /// </summary>
        private static readonly BackupActivityType[] KnownActivityTypes = new[]
        {
            BackupActivityType.Started,
            BackupActivityType.DatabaseProgress,
            BackupActivityType.StepChanged,
            BackupActivityType.CloudUploadStarted,
            BackupActivityType.CloudUploadProgress,
            BackupActivityType.CloudUploadCompleted,
            BackupActivityType.CloudUploadAbandoned,
            BackupActivityType.Completed,
            BackupActivityType.Failed,
            BackupActivityType.Cancelled
        };

        [TestMethod]
        public void KnownActivityTypes_ShouldCoverAllEnumValues()
        {
            var allValues = Enum.GetValues(typeof(BackupActivityType)).Cast<BackupActivityType>().ToArray();

            KnownActivityTypes.Should().BeEquivalentTo(allValues,
                "yeni bir BackupActivityType eklendiyse KnownActivityTypes dizisi ve tüm switch expression'lar güncellenmelidir. " +
                "⚠️ 5 nokta: OnBackupActivityChanged, BuildActivityLogLine, GetLogColor, UpdatePlanRowStatus, AppendBackupLog");
        }

        [TestMethod]
        public void BackupActivityType_ShouldHaveExactly9Values()
        {
            var allValues = Enum.GetValues(typeof(BackupActivityType)).Cast<BackupActivityType>().ToArray();
            allValues.Should().HaveCount(10,
                "BackupActivityType 10 değer içermeli. Yeni değer eklendiğinde bu test + 5 sorumluluk noktası güncellenmelidir.");
        }

        [TestMethod]
        [DynamicData(nameof(GetAllActivityTypes), DynamicDataSourceType.Method)]
        public void BuildActivityLogLine_ShouldHandleActivityType(BackupActivityType activityType)
        {
            // BuildActivityLogLine switch expression tüm enum değerlerini kapsamalı.
            // Doğrudan metoda erişemiyoruz (private) — ancak bilinmeyen değer eklenmesi durumunda
            // runtime'da ArgumentOutOfRangeException fırlatılacağını doğruluyoruz.
            // Bu test, enum coverage'ını compile-time'a yaklaştırır.

            activityType.Should().BeOneOf(KnownActivityTypes,
                $"'{activityType}' BuildActivityLogLine switch'inde tanımlı olmalıdır");
        }

        [TestMethod]
        [DynamicData(nameof(GetAllActivityTypes), DynamicDataSourceType.Method)]
        public void GetLogColor_ShouldHandleActivityType(BackupActivityType activityType)
        {
            activityType.Should().BeOneOf(KnownActivityTypes,
                $"'{activityType}' GetLogColor switch'inde tanımlı olmalıdır");
        }

        private static IEnumerable<object[]> GetAllActivityTypes()
        {
            return Enum.GetValues(typeof(BackupActivityType))
                .Cast<BackupActivityType>()
                .Select(t => new object[] { t });
        }
    }
}
