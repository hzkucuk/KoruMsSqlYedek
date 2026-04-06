using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KoruMsSqlYedek.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class PlanProgressTrackerTests
    {
        // ── DatabaseProgress ─────────────────────────────────────────────────

        [TestMethod]
        public void DatabaseProgress_SingleDb_ReturnsZeroForFirstDb()
        {
            var tracker = CreateTracker(dbTotal: 1);
            int pct = tracker.CalculateDatabaseProgress(1, 1);
            pct.Should().Be(0, "ilk (ve tek) DB başladığında henüz tamamlanan DB yok");
        }

        [TestMethod]
        public void DatabaseProgress_TwoDb_SecondDbReturns50Percent()
        {
            var tracker = CreateTracker(dbTotal: 2);
            tracker.CalculateDatabaseProgress(1, 2);
            int pct = tracker.CalculateDatabaseProgress(2, 2);
            pct.Should().Be(50, "2 DB'den 1. tamamlandı → %50");
        }

        [TestMethod]
        public void DatabaseProgress_FiveDb_ProgressIncrementsBy20()
        {
            var tracker = CreateTracker(dbTotal: 5);

            int pct1 = tracker.CalculateDatabaseProgress(1, 5);
            int pct2 = tracker.CalculateDatabaseProgress(2, 5);
            int pct3 = tracker.CalculateDatabaseProgress(3, 5);
            int pct4 = tracker.CalculateDatabaseProgress(4, 5);
            int pct5 = tracker.CalculateDatabaseProgress(5, 5);

            pct1.Should().Be(0);
            pct2.Should().Be(20);
            pct3.Should().Be(40);
            pct4.Should().Be(60);
            pct5.Should().Be(80);
        }

        [TestMethod]
        public void DatabaseProgress_WithFileBackup_MaxSqlRangeIs80()
        {
            var tracker = CreateTracker(dbTotal: 2, hasFileBackup: true);

            int pct1 = tracker.CalculateDatabaseProgress(1, 2);
            int pct2 = tracker.CalculateDatabaseProgress(2, 2);

            pct1.Should().Be(0);
            pct2.Should().Be(40, "dosya yedekleme varsa SQL fazı %80'e kadar → 2 DB'den 1 tamam = %40");
        }

        [TestMethod]
        public void DatabaseProgress_MonotonicIncrease_NeverDecreases()
        {
            var tracker = CreateTracker(dbTotal: 4);
            tracker.MaxPercent = 50;

            int pct = tracker.CalculateDatabaseProgress(2, 4);
            pct.Should().Be(50, "MaxPercent 50 iken yeni hesaplama (25) daha düşük olduğundan 50 kalmalı");
        }

        [TestMethod]
        public void DatabaseProgress_ZeroTotalCount_ReturnsMaxPercent()
        {
            var tracker = CreateTracker(dbTotal: 0);
            tracker.MaxPercent = 30;

            int pct = tracker.CalculateDatabaseProgress(1, 0);
            pct.Should().Be(30);
        }

        [TestMethod]
        public void DatabaseProgress_ResetsVssFlags()
        {
            var tracker = CreateTracker(dbTotal: 2);
            tracker.HasVssUpload = true;
            tracker.IsVssPhase = true;

            tracker.CalculateDatabaseProgress(2, 2);

            tracker.HasVssUpload.Should().BeFalse("her yeni DB'de VSS durumu sıfırlanmalı");
            tracker.IsVssPhase.Should().BeFalse();
        }

        // ── FileBackupPhaseStart ─────────────────────────────────────────────

        [TestMethod]
        public void FileBackupPhaseStart_MixedPlan_Returns80()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true);
            tracker.MaxPercent = 60;

            int pct = tracker.CalculateFileBackupPhaseStart();

            pct.Should().Be(80, "SQL+dosya planında dosya fazı %80'den başlar");
            tracker.IsFileBackupPhase.Should().BeTrue();
        }

        [TestMethod]
        public void FileBackupPhaseStart_FileOnlyPlan_Returns0()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true);

            int pct = tracker.CalculateFileBackupPhaseStart();

            pct.Should().Be(0, "dosya-only planda %0'dan başlar");
            tracker.IsFileBackupPhase.Should().BeTrue();
        }

        [TestMethod]
        public void FileBackupPhaseStart_MaxPercentHigher_ReturnsMaxPercent()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true);
            tracker.MaxPercent = 90;

            int pct = tracker.CalculateFileBackupPhaseStart();
            pct.Should().Be(90, "monoton artış: MaxPercent 90 > fileBase 80");
        }

        // ── FileCompressionProgress ──────────────────────────────────────────

        [TestMethod]
        public void FileCompression_MixedPlan_Returns97()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true);
            tracker.IsFileBackupPhase = true;
            tracker.MaxPercent = 80;

            int pct = tracker.CalculateFileCompressionProgress();
            pct.Should().Be(97, "SQL+dosya, bulut yok: fileBase(80) + fileCopyWeight(10) + fileCompressWeight(7) = 97");
        }

        [TestMethod]
        public void FileCompression_FileOnlyPlan_Returns85()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true);
            tracker.IsFileBackupPhase = true;

            int pct = tracker.CalculateFileCompressionProgress();
            pct.Should().Be(85, "dosya-only, bulut yok: fileBase(0) + fileCopyWeight(50) + fileCompressWeight(35) = 85");
        }

        // ── LocalStepProgress ────────────────────────────────────────────────

        [TestMethod]
        public void LocalStepProgress_SqlYedekleme_SingleDb_Returns50()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(50, "1 DB, SQL adımı → %50");
        }

        [TestMethod]
        public void LocalStepProgress_Dogrulama_SingleDb_Returns65()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.MaxPercent = 50;

            int pct = tracker.CalculateLocalStepProgress("Doğrulama");
            pct.Should().Be(65);
        }

        [TestMethod]
        public void LocalStepProgress_Sikistirma_SingleDb_Returns80()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.MaxPercent = 65;

            int pct = tracker.CalculateLocalStepProgress("Sıkıştırma");
            pct.Should().Be(80);
        }

        [TestMethod]
        public void LocalStepProgress_ArsivDogrulama_Returns88()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.MaxPercent = 80;

            int pct = tracker.CalculateLocalStepProgress("Arşiv Doğrulama");
            pct.Should().Be(88);
        }

        [TestMethod]
        public void LocalStepProgress_Temizlik_Returns95()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.MaxPercent = 88;

            int pct = tracker.CalculateLocalStepProgress("Temizlik");
            pct.Should().Be(95);
        }

        [TestMethod]
        public void LocalStepProgress_TwoDb_SecondDbSqlYedekleme_Returns75()
        {
            var tracker = CreateTracker(dbTotal: 2, sqlDbCount: 2);
            tracker.DbIndex = 2;
            tracker.DbTotal = 2;
            tracker.MaxPercent = 50;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(75, "2. DB başlangıcı(%50) + dilim(50)*0.50 = 75");
        }

        [TestMethod]
        public void LocalStepProgress_WithFileBackup_SqlRangeIs80()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasFileBackup: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(40, "dosya yedekleme varsa SQL aralığı 80 → 80*0.50 = 40");
        }

        [TestMethod]
        public void LocalStepProgress_UnknownStep_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct = tracker.CalculateLocalStepProgress("Bilinmeyen Adım");
            pct.Should().Be(-1);
        }

        [TestMethod]
        public void LocalStepProgress_WithCloudTargets_UsesReducedSqlRange()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // SQL range = SqlRangeWithCloudNoFile = 40 → 40 * 0.50 = 20
            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(20, "bulut hedef varsa SQL aralığı 40 → 40*0.50 = 20");
        }

        [TestMethod]
        public void LocalStepProgress_InFileBackupPhase_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.IsFileBackupPhase = true;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(-1);
        }

        // ── CloudUploadProgress (Konsolide Model) ────────────────────────────

        [TestMethod]
        public void CloudUpload_Consolidated_SqlOnly_At50_Returns70()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 40; // SQL local fazı bitti
            tracker.StartConsolidatedCloudPhase();

            // CloudPhaseBase=40, range=60, batch %50 → 40 + 60*0.50 = 70
            int pct = tracker.CalculateCloudUploadProgress(50);
            pct.Should().Be(70);
        }

        [TestMethod]
        public void CloudUpload_Consolidated_SqlOnly_At100_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 40;
            tracker.StartConsolidatedCloudPhase();

            int pct = tracker.CalculateCloudUploadProgress(100);
            pct.Should().Be(100, "SQL local(%40) + Bulut batch(%100) → 100");
        }

        [TestMethod]
        public void CloudUpload_Consolidated_FileOnly_At50_Returns62()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);
            tracker.MaxPercent = 25; // dosya local fazı bitti
            tracker.StartConsolidatedCloudPhase();

            // CloudPhaseBase=25, range=75, batch %50 → 25 + 75*0.50 = 62
            int pct = tracker.CalculateCloudUploadProgress(50);
            pct.Should().Be(62);
        }

        [TestMethod]
        public void CloudUpload_Consolidated_FileOnly_At100_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);
            tracker.MaxPercent = 25;
            tracker.StartConsolidatedCloudPhase();

            int pct = tracker.CalculateCloudUploadProgress(100);
            pct.Should().Be(100, "dosya local(%25) + Bulut batch(%100) → 100");
        }

        [TestMethod]
        public void CloudUpload_Consolidated_MixedPlan_At100_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 2, sqlDbCount: 2, hasFileBackup: true, hasCloudTargets: true);
            tracker.MaxPercent = 50; // SQL + dosya local fazları bitti
            tracker.StartConsolidatedCloudPhase();

            int pct = tracker.CalculateCloudUploadProgress(100);
            pct.Should().Be(100, "SQL+dosya local(%50) + Bulut batch(%100) → 100");
        }

        [TestMethod]
        public void CloudUpload_NotConsolidated_ReturnsMaxPercent()
        {
            var tracker = new PlanProgressTracker();
            tracker.MaxPercent = 42;

            int pct = tracker.CalculateCloudUploadProgress(50);
            pct.Should().Be(42, "konsolide faz başlatılmamışsa MaxPercent döner");
        }

        [TestMethod]
        public void CloudUpload_Consolidated_MonotonicIncrease()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 40;
            tracker.StartConsolidatedCloudPhase();

            int pct1 = tracker.CalculateCloudUploadProgress(30);
            int pct2 = tracker.CalculateCloudUploadProgress(60);
            int pct3 = tracker.CalculateCloudUploadProgress(50); // geriye gitmemeli

            pct1.Should().BeLessThanOrEqualTo(pct2);
            pct3.Should().BeGreaterThanOrEqualTo(pct2, "monoton artış: %50 gelse bile MaxPercent nedeniyle düşmez");
        }

        [TestMethod]
        public void StartConsolidatedCloudPhase_RecordsBasePercent()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 38;

            int result = tracker.StartConsolidatedCloudPhase();

            result.Should().Be(40, "CloudPhaseBase = expectedBase = SqlRangeWithCloudNoFile = 40");
            tracker.IsConsolidatedCloudPhase.Should().BeTrue();
            tracker.CloudPhaseBase.Should().Be(40);
        }

        // ── Entegrasyon: Tam pipeline senaryoları ────────────────────────────

        [TestMethod]
        public void FullPipeline_SingleDbNoCloud_ProgressNeverExceeds100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);

            int pct = tracker.CalculateDatabaseProgress(1, 1);
            pct.Should().BeInRange(0, 100);

            pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().BeInRange(0, 100);

            pct = tracker.CalculateLocalStepProgress("Doğrulama");
            pct.Should().BeInRange(0, 100);

            pct = tracker.CalculateLocalStepProgress("Sıkıştırma");
            pct.Should().BeInRange(0, 100);

            pct = tracker.CalculateLocalStepProgress("Arşiv Doğrulama");
            pct.Should().BeInRange(0, 100);

            pct = tracker.CalculateLocalStepProgress("Temizlik");
            pct.Should().BeInRange(0, 100);
        }

        [TestMethod]
        public void FullPipeline_ThreeDbWithCloud_MonotonicIncrease()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasCloudTargets: true);

            var values = new List<int>();

            for (int db = 1; db <= 3; db++)
            {
                values.Add(tracker.CalculateDatabaseProgress(db, 3));

                foreach (string step in new[] { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" })
                    values.Add(tracker.CalculateLocalStepProgress(step));
            }

            // Konsolide bulut fazı
            values.Add(tracker.StartConsolidatedCloudPhase());
            for (int p = 10; p <= 100; p += 10)
                values.Add(tracker.CalculateCloudUploadProgress(p));

            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThanOrEqualTo(values[i - 1],
                    $"yüzde dizisi monoton artmalı: index {i - 1}→{i}");
        }

        [TestMethod]
        public void FullPipeline_SqlPlusFileBackup_TotalReaches100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasFileBackup: true, hasCloudTargets: true);

            // SQL lokal faz
            tracker.CalculateDatabaseProgress(1, 1);
            foreach (string step in new[] { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" })
                tracker.CalculateLocalStepProgress(step);

            // Dosya faz
            tracker.CalculateFileBackupPhaseStart();
            tracker.CalculateFileCompressionProgress();

            // Konsolide bulut fazı
            tracker.StartConsolidatedCloudPhase();
            int pct = tracker.CalculateCloudUploadProgress(100);

            pct.Should().Be(100, "SQL+dosya+bulut tamamlandığında %100 olmalı");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  O6 — Edge-Case ve Ağırlık Modeli Testleri
        // ══════════════════════════════════════════════════════════════════════

        // ── Sınır Değer Testleri ─────────────────────────────────────────────

        [TestMethod]
        public void DatabaseProgress_NegativeTotalCount_ReturnsMaxPercent()
        {
            var tracker = CreateTracker(dbTotal: 1);
            tracker.MaxPercent = 25;

            int pct = tracker.CalculateDatabaseProgress(1, -1);
            pct.Should().Be(25, "totalCount negatif ise MaxPercent döner");
        }

        [TestMethod]
        public void DatabaseProgress_LargeDbCount_100Db_LastDbNear100()
        {
            const int total = 100;
            var tracker = CreateTracker(dbTotal: total);

            int pct = tracker.CalculateDatabaseProgress(total, total);
            pct.Should().Be(99, "100 DB'den 99 tamamlandı → %99");
        }

        [TestMethod]
        public void DatabaseProgress_MaxPercentAlready100_StaysAt100()
        {
            var tracker = CreateTracker(dbTotal: 5);
            tracker.MaxPercent = 100;

            int pct = tracker.CalculateDatabaseProgress(3, 5);
            pct.Should().Be(100, "MaxPercent zaten 100 ise asla düşmez");
        }

        [TestMethod]
        public void DatabaseProgress_LargeIndex_ClampedTo100()
        {
            var tracker = CreateTracker(dbTotal: 2);
            // Normalde olmaz ama index > total gelirse taşma olmamalı
            int pct = tracker.CalculateDatabaseProgress(5, 2);
            pct.Should().BeInRange(0, 100, "klamplama nedeniyle [0,100] aralığında kalmalı");
        }

        // ── LocalStepProgress Sınır Testleri ────────────────────────────────

        [TestMethod]
        public void LocalStepProgress_DbIndexZero_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3);
            tracker.DbIndex = 0;
            tracker.DbTotal = 3;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(-1, "DbIndex=0 ise hesaplama yapılmaz");
        }

        [TestMethod]
        public void LocalStepProgress_DbTotalZero_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 0, sqlDbCount: 0);
            tracker.DbIndex = 1;
            tracker.DbTotal = 0;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(-1, "DbTotal=0 ise hesaplama yapılmaz");
        }

        [TestMethod]
        public void LocalStepProgress_AllSteps_MultiDb_CoverEntireRange()
        {
            // 4 DB, bulut yok, dosya yok → her DB eşit dilim (25%), her adım dilim içinde ağırlıkla
            var tracker = CreateTracker(dbTotal: 4, sqlDbCount: 4);
            var allValues = new List<int>();

            for (int db = 1; db <= 4; db++)
            {
                tracker.DbIndex = db;
                tracker.DbTotal = 4;

                foreach (string step in new[] { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" })
                {
                    int pct = tracker.CalculateLocalStepProgress(step);
                    pct.Should().BeGreaterThanOrEqualTo(0);
                    allValues.Add(pct);
                }
            }

            // Monoton artış
            for (int i = 1; i < allValues.Count; i++)
                allValues[i].Should().BeGreaterThanOrEqualTo(allValues[i - 1],
                    $"index {i}: monoton artış bozuldu");

            allValues.Last().Should().BeInRange(95, 100);
        }

        // ── Konsolide Bulut: Ağırlık Model Hassas Hesaplama ─────────────────

        [TestMethod]
        public void CloudUpload_Consolidated_WeightDistribution_SqlOnly()
        {
            // 1 DB, bulut var → SQL lokal 0-40, bulut 40-100
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // SQL Temizlik adımı: 40*0.95 = 38
            int localEnd = tracker.CalculateLocalStepProgress("Temizlik");
            localEnd.Should().Be(38, "SQL range=40, Temizlik ağırlığı=0.95 → 38");

            // Konsolide bulut başlat → expectedBase=40 (SqlRangeWithCloudNoFile)
            int cloudBase = tracker.StartConsolidatedCloudPhase();
            cloudBase.Should().Be(40);

            // Bulut %0 → 40
            int at0 = tracker.CalculateCloudUploadProgress(0);
            at0.Should().Be(40);

            // Bulut %50 → 40 + 60*0.50 = 70
            tracker.MaxPercent = 40; // reset for clean test
            int at50 = tracker.CalculateCloudUploadProgress(50);
            at50.Should().Be(70);

            // Bulut %100 → 40 + 60 = 100
            tracker.MaxPercent = 40;
            int at100 = tracker.CalculateCloudUploadProgress(100);
            at100.Should().Be(100);
        }

        [TestMethod]
        public void CloudUpload_Consolidated_WeightDistribution_FileOnly()
        {
            // SqlDbCount=0, dosya+bulut → dosya lokal 0-25, bulut 25-100
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);

            tracker.CalculateFileBackupPhaseStart();
            int compress = tracker.CalculateFileCompressionProgress();
            compress.Should().Be(25, "dosya-only+bulut: kopyalama=25, sıkıştırma=0 → 25");

            int cloudBase = tracker.StartConsolidatedCloudPhase();
            cloudBase.Should().Be(25);

            int at100 = tracker.CalculateCloudUploadProgress(100);
            at100.Should().Be(100, "dosya-only: %25 + %75 bulut = 100");
        }

        [TestMethod]
        public void CloudUpload_Consolidated_MultiDb_CorrectProgression()
        {
            // 3 DB, bulut var → SQL lokal range=40, her DB dilimi=40/3≈13.3
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasCloudTargets: true);

            // 3 DB SQL lokal fazını geç
            for (int db = 1; db <= 3; db++)
            {
                tracker.CalculateDatabaseProgress(db, 3);
                tracker.CalculateLocalStepProgress("SQL Yedekleme");
                tracker.CalculateLocalStepProgress("Temizlik");
            }

            int cloudBase = tracker.StartConsolidatedCloudPhase();
            cloudBase.Should().BeInRange(35, 40);

            int at100 = tracker.CalculateCloudUploadProgress(100);
            at100.Should().Be(100);
        }

        // ── Dosya-Only Plan Tam Pipeline ─────────────────────────────────────

        [TestMethod]
        public void FileOnlyPlan_FullPipeline_ProgressTo100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);

            int start = tracker.CalculateFileBackupPhaseStart();
            start.Should().Be(0, "dosya-only plan %0'dan başlar");

            int compress = tracker.CalculateFileCompressionProgress();
            compress.Should().Be(25, "dosya-only+bulut: kopyalama ağırlığı %25");

            tracker.StartConsolidatedCloudPhase();
            int cloudHalf = tracker.CalculateCloudUploadProgress(50);
            cloudHalf.Should().Be(62, "dosya-only: CloudPhaseBase=25, %50 batch → 25+37=62");

            int cloud100 = tracker.CalculateCloudUploadProgress(100);
            cloud100.Should().Be(100, "dosya-only: %100 batch → 100");
        }

        [TestMethod]
        public void FileOnlyPlan_NoCloud_CompressionWithCleanup()
        {
            // dosya-only, bulut hedefi yok → kopyalama=50, sıkıştırma=35
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true);

            tracker.CalculateFileBackupPhaseStart();
            int compress = tracker.CalculateFileCompressionProgress();
            compress.Should().Be(85, "dosya-only, bulut yok: kopyalama=50, sıkıştırma=35 → 85");
        }

        // ── SQL+Dosya+Bulut Karma Plan ───────────────────────────────────────

        [TestMethod]
        public void MixedPlan_SqlThenFile_ProgressNeverExceeds100()
        {
            // 2 SQL DB + dosya yedekleme + bulut → SQL fazı %45, dosya fazı, bulut fazı
            var tracker = CreateTracker(dbTotal: 2, sqlDbCount: 2, hasFileBackup: true, hasCloudTargets: true);

            var values = new List<int>();

            // SQL lokal fazı
            for (int db = 1; db <= 2; db++)
            {
                values.Add(tracker.CalculateDatabaseProgress(db, 2));
                foreach (string step in new[] { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" })
                    values.Add(tracker.CalculateLocalStepProgress(step));
            }

            // Dosya fazı
            values.Add(tracker.CalculateFileBackupPhaseStart());
            values.Add(tracker.CalculateFileCompressionProgress());

            // Konsolide bulut fazı
            values.Add(tracker.StartConsolidatedCloudPhase());
            values.Add(tracker.CalculateCloudUploadProgress(100));

            // Monoton artış
            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThanOrEqualTo(values[i - 1],
                    $"index {i}: monoton artış bozuldu");

            // Son %100
            values.Last().Should().Be(100, "tam pipeline sonunda %100");

            // Hiçbir değer 0-100 dışına çıkmamalı
            foreach (int v in values)
                v.Should().BeInRange(0, 100);
        }

        // ── Monoton Artış Senaryoları ────────────────────────────────────────

        [TestMethod]
        public void MonotonicGuarantee_ProgressNeverDecreasesAcrossAllMethods()
        {
            // Tam pipeline: 3 DB + dosya + bulut
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true, hasCloudTargets: true);
            var values = new List<int>();

            for (int db = 1; db <= 3; db++)
            {
                values.Add(tracker.CalculateDatabaseProgress(db, 3));
                foreach (string step in new[] { "SQL Yedekleme", "Doğrulama", "Sıkıştırma" })
                    values.Add(tracker.CalculateLocalStepProgress(step));
            }

            values.Add(tracker.CalculateFileBackupPhaseStart());
            values.Add(tracker.CalculateFileCompressionProgress());

            values.Add(tracker.StartConsolidatedCloudPhase());
            for (int p = 0; p <= 100; p += 25)
                values.Add(tracker.CalculateCloudUploadProgress(p));

            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThanOrEqualTo(values[i - 1],
                    $"index {i - 1}→{i}: {values[i - 1]}→{values[i]} monoton artış bozuldu");
        }

        [TestMethod]
        public void MonotonicGuarantee_RandomCloudProgressValues_NeverDecreases()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 40;
            tracker.StartConsolidatedCloudPhase();

            int[] randomProgress = { 30, 10, 60, 40, 80, 20, 100, 50, 90, 70 };
            int prev = 0;

            foreach (int p in randomProgress)
            {
                int pct = tracker.CalculateCloudUploadProgress(p);
                pct.Should().BeGreaterThanOrEqualTo(prev,
                    $"progress={p} → {pct} >= {prev} olmalı (monoton)");
                prev = pct;
            }
        }

        // ── LocalStepWeights — Public API üzerinden doğrulama ─────────────

        [TestMethod]
        public void LocalStepProgress_AllKnownSteps_ReturnValidPercent()
        {
            // Bilinen 5 adım public API üzerinden geçerli sonuç döndürmeli
            string[] knownSteps = { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" };
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            foreach (string step in knownSteps)
            {
                int pct = tracker.CalculateLocalStepProgress(step);
                pct.Should().BeGreaterThanOrEqualTo(0, $"'{step}' bilinen bir adım, -1 dönmemeli");
                pct.Should().BeLessThanOrEqualTo(100, $"'{step}' yüzdesi 0-100 aralığında olmalı");
            }
        }

        [TestMethod]
        public void LocalStepProgress_StepsProduceAscendingValues()
        {
            // Adımlar sırayla çağrıldığında monoton artan yüzdeler üretmeli
            string[] orderedSteps = { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" };
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            var values = new List<int>();
            foreach (string step in orderedSteps)
                values.Add(tracker.CalculateLocalStepProgress(step));

            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThan(values[i - 1],
                    $"'{orderedSteps[i]}' ağırlığı '{orderedSteps[i - 1]}'dan büyük olmalı");
        }

        [TestMethod]
        public void LocalStepProgress_AllKnownSteps_BetweenZeroAnd100()
        {
            string[] knownSteps = { "SQL Yedekleme", "Doğrulama", "Sıkıştırma", "Arşiv Doğrulama", "Temizlik" };
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            foreach (string step in knownSteps)
            {
                tracker.MaxPercent = 0; // her adım için bağımsız test
                tracker.DbIndex = 1;
                int pct = tracker.CalculateLocalStepProgress(step);
                pct.Should().BeGreaterThan(0, $"'{step}' > 0 olmalı");
                pct.Should().BeLessThanOrEqualTo(100, $"'{step}' <= 100 olmalı");
            }
        }

        // ── Constants Sabitleri Doğrulaması ──────────────────────────────────

        [TestMethod]
        public void SqlRangeConstants_AreCorrect()
        {
            PlanProgressTracker.SqlRangeWithFileBackup.Should().Be(80);
            PlanProgressTracker.SqlRangeWithoutFileBackup.Should().Be(100);
            PlanProgressTracker.SqlRangeWithCloudNoFile.Should().Be(40);
            PlanProgressTracker.SqlRangeWithCloudAndFile.Should().Be(45);
        }

        // ── Bug Fix: MaxPercent şişmesi bulut ilerlemesini bozmamalı ─────────

        [TestMethod]
        public void CloudUpload_InflatedMaxPercent_DoesNotCorruptCloudProgress()
        {
            // BUG SENARYOSU: MaxPercent bir şekilde beklenen aralığın ötesine şişmiş (100).
            // Eski kod Math.Max(cumPct, MaxPercent) kullandığı için bulut ilerlemesi hep 100 dönerdi.
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasFileBackup: true, hasCloudTargets: true);
            tracker.MaxPercent = 100; // Şişmiş MaxPercent!

            int cloudBase = tracker.StartConsolidatedCloudPhase();

            // CloudPhaseBase, ağırlık modelinin beklenen tavanı ile sınırlanmalı (50)
            cloudBase.Should().BeLessThan(100, "CloudPhaseBase şişmiş MaxPercent'e eşit olmamalı");

            int at11 = tracker.CalculateCloudUploadProgress(11);
            at11.Should().BeLessThan(100, "batchPct=%11 iken bulut ilerlemesi %100 göstermemeli");
            at11.Should().BeGreaterThan(cloudBase, "bulut ilerlemesi CloudPhaseBase'den büyük olmalı");
        }

        [TestMethod]
        public void CloudUpload_InflatedMaxPercent_SqlOnlyPlan_CappedTo40()
        {
            // SQL-only + bulut → beklenen tavan 40
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.MaxPercent = 100;

            int cloudBase = tracker.StartConsolidatedCloudPhase();
            cloudBase.Should().Be(40, "SQL-only+bulut: ağırlık tavanı 40");

            int at50 = tracker.CalculateCloudUploadProgress(50);
            at50.Should().Be(70, "40 + 60*0.50 = 70");
        }

        [TestMethod]
        public void CloudUpload_InflatedMaxPercent_FileOnlyPlan_CappedTo25()
        {
            // Dosya-only + bulut → beklenen tavan 25
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);
            tracker.MaxPercent = 100;

            int cloudBase = tracker.StartConsolidatedCloudPhase();
            cloudBase.Should().Be(25, "dosya-only+bulut: ağırlık tavanı 25");

            int at50 = tracker.CalculateCloudUploadProgress(50);
            at50.Should().Be(62, "25 + 75*0.50 = 62");
        }

        [TestMethod]
        public void LocalStepProgress_InConsolidatedCloudPhase_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.IsConsolidatedCloudPhase = true;

            int pct = tracker.CalculateLocalStepProgress("Temizlik");
            pct.Should().Be(-1, "bulut fazında SQL adım hesaplaması yapılmamalı");
        }

        // ── Helper ───────────────────────────────────────────────────────────

        private static PlanProgressTracker CreateTracker(
            int dbTotal = 1,
            int sqlDbCount = -1,
            bool hasFileBackup = false,
            bool hasCloudTargets = false)
        {
            if (sqlDbCount < 0)
                sqlDbCount = dbTotal;

            return new PlanProgressTracker
            {
                DbIndex = 0,
                DbTotal = dbTotal,
                SqlDbCount = sqlDbCount,
                MaxPercent = 0,
                HasFileBackup = hasFileBackup,
                HasCloudTargets = hasCloudTargets
            };
        }
    }
}
