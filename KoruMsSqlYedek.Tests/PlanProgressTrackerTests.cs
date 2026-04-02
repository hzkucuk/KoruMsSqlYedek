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
        public void FileCompression_MixedPlan_Returns85()
        {
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true);
            tracker.IsFileBackupPhase = true;
            tracker.MaxPercent = 80;

            int pct = tracker.CalculateFileCompressionProgress();
            pct.Should().Be(85, "SQL+dosya: fileBase(80) + fileCopyWeight(5) = 85");
        }

        [TestMethod]
        public void FileCompression_FileOnlyPlan_Returns25()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true);
            tracker.IsFileBackupPhase = true;

            int pct = tracker.CalculateFileCompressionProgress();
            pct.Should().Be(25, "dosya-only: fileBase(0) + fileCopyWeight(25) = 25");
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
        public void LocalStepProgress_WithCloudTargets_ReturnsMinusOne()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct = tracker.CalculateLocalStepProgress("SQL Yedekleme");
            pct.Should().Be(-1, "bulut hedef varsa local-mode ilerleme hesaplanmaz");
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

        // ── CloudUploadProgress ──────────────────────────────────────────────

        [TestMethod]
        public void CloudUpload_NoVss_SingleDb_At50Percent_Returns65()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // SQL %30 + Bulut %70 → 30 + 70*0.50 = 65
            int pct = tracker.CalculateCloudUploadProgress(50, 1, 1);
            pct.Should().Be(65);
        }

        [TestMethod]
        public void CloudUpload_NoVss_SingleDb_At100Percent_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);
            pct.Should().Be(100, "SQL(%30) + Bulut(%70) tam = 100");
        }

        [TestMethod]
        public void CloudUpload_WithVss_MainPhase_At100_Returns70()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.HasVssUpload = true;
            tracker.IsVssPhase = false;

            // SQL %20 + Ana bulut %50 → 20 + 50*1.0 = 70
            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);
            pct.Should().Be(70);
        }

        [TestMethod]
        public void CloudUpload_WithVss_VssPhase_At100_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.HasVssUpload = true;
            tracker.IsVssPhase = true;
            tracker.MaxPercent = 70;

            // dbBase(0) + SQL(20) + MainCloud(50) + VSSCloud(30)*1.0 = 100
            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);
            pct.Should().Be(100);
        }

        [TestMethod]
        public void CloudUpload_FilePhase_FileOnlyPlan_At50_Returns62()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);
            tracker.DbIndex = 0;
            tracker.DbTotal = 1;
            tracker.IsFileBackupPhase = true;

            // fileBase(0) + fileCopyWeight(25) + 75*0.50 = 62
            int pct = tracker.CalculateCloudUploadProgress(50, 1, 1);
            pct.Should().Be(62);
        }

        [TestMethod]
        public void CloudUpload_FilePhase_MixedPlan_At100_Returns100()
        {
            var tracker = CreateTracker(dbTotal: 2, sqlDbCount: 2, hasFileBackup: true, hasCloudTargets: true);
            tracker.DbIndex = 2;
            tracker.DbTotal = 2;
            tracker.IsFileBackupPhase = true;
            tracker.MaxPercent = 80;

            // fileBase(80) + fileCopyWeight(5) + fileCloudWeight(15)*1.0 = 100
            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);
            pct.Should().Be(100);
        }

        [TestMethod]
        public void CloudUpload_MultiTarget_CalculatesOverallCorrectly()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // 2 hedef, 1. hedef %100, 2. hedef %50 → overall = (0*100+50)/2 = 25 (hedef 2)
            // Ama bu metodu tekrar çağırıyoruz 2. hedef için:
            // overallCloudPct = ((2-1)*100 + 50)/2 = 75
            // SQL(%30) + Bulut(%70)*0.75 = 30 + 52 = 82
            int pct = tracker.CalculateCloudUploadProgress(50, 2, 2);
            pct.Should().Be(82);
        }

        [TestMethod]
        public void CloudUpload_MonotonicIncrease()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int pct1 = tracker.CalculateCloudUploadProgress(30, 1, 1);
            int pct2 = tracker.CalculateCloudUploadProgress(60, 1, 1);
            int pct3 = tracker.CalculateCloudUploadProgress(50, 1, 1);

            pct1.Should().BeLessThanOrEqualTo(pct2);
            pct3.Should().BeGreaterThanOrEqualTo(pct2, "monoton artış: %50 gelse bile MaxPercent nedeniyle düşmez");
        }

        [TestMethod]
        public void CloudUpload_DbTotalZero_ReturnsRawPercent()
        {
            var tracker = new PlanProgressTracker();
            tracker.DbTotal = 0;

            int pct = tracker.CalculateCloudUploadProgress(42, 1, 1);
            pct.Should().Be(42, "DbTotal=0 ise ham progressPercent döner");
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

                for (int p = 10; p <= 100; p += 10)
                    values.Add(tracker.CalculateCloudUploadProgress(p, 1, 1));
            }

            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThanOrEqualTo(values[i - 1],
                    $"yüzde dizisi monoton artmalı: index {i - 1}→{i}");
        }

        [TestMethod]
        public void FullPipeline_SqlPlusFileBackup_TotalReaches100()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasFileBackup: true, hasCloudTargets: true);

            // SQL faz
            tracker.CalculateDatabaseProgress(1, 1);
            tracker.CalculateCloudUploadProgress(100, 1, 1);

            // Dosya faz
            tracker.CalculateFileBackupPhaseStart();
            tracker.CalculateFileCompressionProgress();
            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);

            pct.Should().Be(100, "SQL+dosya bulut yükleme tamamlandığında %100 olmalı");
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

            // Son adım 95 olmalı (4. DB Temizlik: (3/4)*100 + 25*0.95 = 75+23 = 98... hmm)
            // Aslında: son DB (4.) base=75, slice=25, Temizlik=0.95 → 75+23=98
            allValues.Last().Should().BeInRange(95, 100);
        }

        // ── Ağırlık Modeli: VSS Hassas Hesaplama ────────────────────────────

        [TestMethod]
        public void CloudUpload_WithVss_WeightDistribution_20_50_30()
        {
            // 1 DB, 1 cloud target; VSS var
            // SQL=%20, Ana Bulut=%50, VSS Bulut=%30
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;
            tracker.HasVssUpload = true;

            // Ana bulut %0 → dbBase(0)+SQL(20)+0 = 20
            int atZero = tracker.CalculateCloudUploadProgress(0, 1, 1);
            atZero.Should().Be(20, "Ana bulut %0 → SQL kısmı(%20)");

            // Ana bulut %50 → 0+20+50*0.50=45
            tracker.MaxPercent = 0; // reset for test
            tracker.HasVssUpload = true;
            tracker.IsVssPhase = false;
            int atHalf = tracker.CalculateCloudUploadProgress(50, 1, 1);
            atHalf.Should().Be(45, "Ana bulut %50 → 20+25=45");

            // VSS faz — %0 → dbBase(0)+SQL(20)+MainCloud(50)+0=70
            tracker.IsVssPhase = true;
            tracker.MaxPercent = 0;
            tracker.HasVssUpload = true;
            int vssAtZero = tracker.CalculateCloudUploadProgress(0, 1, 1);
            vssAtZero.Should().Be(70, "VSS fazı %0 → 20+50+0=70");

            // VSS faz — %100 → 20+50+30=100
            tracker.MaxPercent = 0;
            tracker.HasVssUpload = true;
            tracker.IsVssPhase = true;
            int vssAt100 = tracker.CalculateCloudUploadProgress(100, 1, 1);
            vssAt100.Should().Be(100, "VSS fazı %100 → 20+50+30=100");
        }

        [TestMethod]
        public void CloudUpload_NoVss_WeightDistribution_30_70()
        {
            // 1 DB, bulut var, VSS yok → SQL=%30 + Bulut=%70
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int at0 = tracker.CalculateCloudUploadProgress(0, 1, 1);
            at0.Should().Be(30, "Bulut %0 → SQL kısmı(%30)");

            tracker.MaxPercent = 0;
            int at50 = tracker.CalculateCloudUploadProgress(50, 1, 1);
            at50.Should().Be(65, "SQL(%30) + Bulut(%70)*0.50=35 → 65");

            tracker.MaxPercent = 0;
            int at100 = tracker.CalculateCloudUploadProgress(100, 1, 1);
            at100.Should().Be(100, "SQL(%30) + Bulut(%70)*1.0=70 → 100");
        }

        [TestMethod]
        public void CloudUpload_MultiDb_SecondDb_CorrectBase()
        {
            // 3 DB, bulut var, VSS yok → her DB dilimi = 100/3 ≈ 33.3
            // 2. DB: base = 33.3, SQL = 33.3*0.30 ≈ 10, Cloud = 33.3*0.70 ≈ 23.3
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasCloudTargets: true);
            tracker.DbIndex = 2;
            tracker.DbTotal = 3;

            int pct = tracker.CalculateCloudUploadProgress(100, 1, 1);
            // dbBase=33.3, slice=33.3, SQL=10, Cloud=23.3 → 33.3+10+23.3=66.6 → 66
            pct.Should().BeInRange(66, 67, "2. DB bulut %100 → ~66-67");
        }

        // ── Dosya-Only Plan Tam Pipeline ─────────────────────────────────────

        [TestMethod]
        public void FileOnlyPlan_FullPipeline_ProgressTo100()
        {
            // SqlDbCount=0, HasFileBackup=true, HasCloudTargets=true
            // kopyalama=%25, bulut=%75
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true, hasCloudTargets: true);

            // Dosya fazına gir
            int start = tracker.CalculateFileBackupPhaseStart();
            start.Should().Be(0, "dosya-only plan %0'dan başlar");

            // Sıkıştırma
            int compress = tracker.CalculateFileCompressionProgress();
            compress.Should().Be(25, "dosya-only: kopyalama ağırlığı %25");

            // Bulut %50
            tracker.IsFileBackupPhase = true;
            tracker.DbTotal = 1;
            int cloudHalf = tracker.CalculateCloudUploadProgress(50, 1, 1);
            // fileBase(0)+fileCopyWeight(25)+75*0.50=62
            cloudHalf.Should().Be(62, "dosya-only: %50 bulut → 25+37=62");

            // Bulut %100
            int cloud100 = tracker.CalculateCloudUploadProgress(100, 1, 1);
            // 0+25+75=100
            cloud100.Should().Be(100, "dosya-only: %100 bulut → 25+75=100");
        }

        [TestMethod]
        public void FileOnlyPlan_NoCloud_CompressionMaxIs25()
        {
            // dosya-only, bulut hedefi yok
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 0, hasFileBackup: true);

            tracker.CalculateFileBackupPhaseStart();
            int compress = tracker.CalculateFileCompressionProgress();
            compress.Should().Be(25, "dosya-only, bulut yok: kopyalama=%25");
        }

        // ── SQL+Dosya+Bulut Karma Plan ───────────────────────────────────────

        [TestMethod]
        public void MixedPlan_SqlThenFile_ProgressNeverExceeds100()
        {
            // 2 SQL DB + dosya yedekleme + bulut → SQL fazı %80, dosya fazı %20
            var tracker = CreateTracker(dbTotal: 2, sqlDbCount: 2, hasFileBackup: true, hasCloudTargets: true);

            var values = new List<int>();

            // SQL fazı — DB1
            values.Add(tracker.CalculateDatabaseProgress(1, 2));
            values.Add(tracker.CalculateCloudUploadProgress(100, 1, 1));

            // SQL fazı — DB2
            values.Add(tracker.CalculateDatabaseProgress(2, 2));
            values.Add(tracker.CalculateCloudUploadProgress(100, 1, 1));

            // Dosya fazı
            values.Add(tracker.CalculateFileBackupPhaseStart());
            values.Add(tracker.CalculateFileCompressionProgress());
            values.Add(tracker.CalculateCloudUploadProgress(100, 1, 1));

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

        // ── Çoklu Bulut Hedefi Dağılımı ─────────────────────────────────────

        [TestMethod]
        public void CloudUpload_ThreeTargets_ProgressDistribution()
        {
            // 1 DB, 3 bulut hedef; VSS yok
            // overallCloudPct = ((targetIndex-1)*100 + progressPercent) / targetTotal
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // 1. hedef %100 → overall = (0*100+100)/3 = 33.3
            // cumPct = 0+30*1/3 + 70*(33.3/100) = ... wait, formula:
            // SQL portion = slice * 0.30 = 100*0.30 = 30
            // Cloud portion = slice * 0.70 = 70
            // cumPct = dbBase(0) + SQL(30) + (overall/100) * Cloud(70)
            // 1. hedef %100: overall=33.3 → 30 + 70*0.333 = 30+23=53
            int t1 = tracker.CalculateCloudUploadProgress(100, 1, 3);
            t1.Should().Be(53, "3 hedeften 1. tamamlandı → ~%53");

            // 2. hedef %100 → overall = (100+100)/3 = 66.6 → 30+70*0.666=30+46=76
            int t2 = tracker.CalculateCloudUploadProgress(100, 2, 3);
            t2.Should().Be(76, "3 hedeften 2. tamamlandı → ~%76");

            // 3. hedef %100 → overall = (200+100)/3 = 100 → 30+70=100
            int t3 = tracker.CalculateCloudUploadProgress(100, 3, 3);
            t3.Should().Be(100, "3 hedeften 3. tamamlandı → %100");
        }

        [TestMethod]
        public void CloudUpload_TwoTargets_PartialProgress()
        {
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            // 2 hedef, 1. hedef %50 → overall = (0+50)/2 = 25
            // cumPct = 30 + 70*0.25 = 30+17 = 47
            int pct = tracker.CalculateCloudUploadProgress(50, 1, 2);
            pct.Should().Be(47, "2 hedef, 1. yarıda → %47");
        }

        // ── Monoton Artış Senaryoları ────────────────────────────────────────

        [TestMethod]
        public void MonotonicGuarantee_ProgressNeverDecreasesAcrossAllMethods()
        {
            // Tam pipeline: 3 DB + dosya + bulut; her çağrı öncekinden >= olmalı
            var tracker = CreateTracker(dbTotal: 3, sqlDbCount: 3, hasFileBackup: true, hasCloudTargets: true);
            var values = new List<int>();

            for (int db = 1; db <= 3; db++)
            {
                values.Add(tracker.CalculateDatabaseProgress(db, 3));
                for (int p = 0; p <= 100; p += 25)
                    values.Add(tracker.CalculateCloudUploadProgress(p, 1, 1));
            }

            values.Add(tracker.CalculateFileBackupPhaseStart());
            values.Add(tracker.CalculateFileCompressionProgress());
            for (int p = 0; p <= 100; p += 25)
                values.Add(tracker.CalculateCloudUploadProgress(p, 1, 1));

            for (int i = 1; i < values.Count; i++)
                values[i].Should().BeGreaterThanOrEqualTo(values[i - 1],
                    $"index {i - 1}→{i}: {values[i - 1]}→{values[i]} monoton artış bozuldu");
        }

        [TestMethod]
        public void MonotonicGuarantee_RandomCloudProgressValues_NeverDecreases()
        {
            // Rastgele sırada bulut yüzdeleri gönderilir; monoton artış garanti olmalı
            var tracker = CreateTracker(dbTotal: 1, sqlDbCount: 1, hasCloudTargets: true);
            tracker.DbIndex = 1;
            tracker.DbTotal = 1;

            int[] randomProgress = { 30, 10, 60, 40, 80, 20, 100, 50, 90, 70 };
            int prev = 0;

            foreach (int p in randomProgress)
            {
                int pct = tracker.CalculateCloudUploadProgress(p, 1, 1);
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
