using System;
using System.Collections.Generic;

namespace KoruMsSqlYedek.Core;

/// <summary>
/// Plan bazında kümülatif ilerleme hesaplama durumunu tutar ve yüzde hesaplar.
/// Her veritabanı toplam ilerlemenin eşit bir dilimini alır;
/// bulut yükleme, dilimin ikinci yarısına eşlenir.
/// </summary>
/// <remarks>
/// Ağırlık modeli (konsolide bulut yükleme):
/// <list type="bullet">
///   <item>SQL-only (bulut yok): her DB eşit dilim (100%), adım bazlı: SQL=%50, Doğrulama=%65, Sıkıştırma=%80, Arşiv=%88, Temizlik=%95</item>
///   <item>SQL+Dosya (bulut yok): SQL fazı %80, dosya fazı %20</item>
///   <item>SQL+Bulut (dosya yok): SQL lokal %0→40, konsolide bulut %40→100</item>
///   <item>SQL+Bulut+Dosya: SQL lokal %0→45, dosya lokal %45→50, konsolide bulut %50→100</item>
///   <item>Dosya-only+Bulut: dosya lokal %0→25, konsolide bulut %25→100</item>
///   <item>Dosya-only (bulut yok): kopyalama %50, sıkıştırma %85, temizlik %90</item>
/// </list>
/// </remarks>
public sealed class PlanProgressTracker
{
    /// <summary>1 tabanlı, şu anki veritabanı sırası.</summary>
    public int DbIndex;

    /// <summary>Toplam veritabanı sayısı (min 1).</summary>
    public int DbTotal;

    /// <summary>Gerçek SQL veritabanı sayısı (0 = dosya-only plan).</summary>
    public int SqlDbCount;

    /// <summary>Monoton artış garantisi — asla geriye gitmez.</summary>
    public int MaxPercent;

    /// <summary>"Express VSS" adımı algılandı — bu DB'de VSS dosyası var.</summary>
    public bool HasVssUpload;

    /// <summary>"VSS Bulut Yükleme" adımına girildi — şu an VSS upload aktif.</summary>
    public bool IsVssPhase;

    /// <summary>Plan dosya yedekleme fazı içeriyor.</summary>
    public bool HasFileBackup;

    /// <summary>Dosya yedekleme fazına girildi.</summary>
    public bool IsFileBackupPhase;

    /// <summary>Plan en az bir etkin bulut hedefi içeriyor.</summary>
    public bool HasCloudTargets;

    /// <summary>Konsolide bulut yükleme fazına girildi.</summary>
    public bool IsConsolidatedCloudPhase;

    /// <summary>Konsolide bulut fazı başlangıç yüzdesi (lokal fazlar bittiğinde kaydedilir).</summary>
    public int CloudPhaseBase;

    // ── Sabitler ──────────────────────────────────────────────────────────

    /// <summary>Dosya yedekleme varsa SQL fazına ayrılan üst sınır yüzdesi (bulut yok).</summary>
    public const int SqlRangeWithFileBackup = 80;

    /// <summary>Dosya yedekleme yoksa SQL fazına ayrılan üst sınır yüzdesi (bulut yok).</summary>
    public const int SqlRangeWithoutFileBackup = 100;

    /// <summary>SQL-only + bulut hedefi: SQL lokal fazı üst sınır.</summary>
    public const int SqlRangeWithCloudNoFile = 40;

    /// <summary>SQL+Dosya + bulut hedefi: SQL lokal fazı üst sınır.</summary>
    public const int SqlRangeWithCloudAndFile = 45;

    // Local-mode step weights (bulut hedefsiz)
    internal static readonly IReadOnlyDictionary<string, double> LocalStepWeights =
        new Dictionary<string, double>
        {
            ["SQL Yedekleme"] = 0.50,
            ["Doğrulama"] = 0.65,
            ["Sıkıştırma"] = 0.80,
            ["Arşiv Doğrulama"] = 0.88,
            ["Temizlik"] = 0.95
        };

    // ── Yardımcı Metotlar ─────────────────────────────────────────────────

    /// <summary>Mevcut duruma göre SQL fazının kullanacağı üst sınır yüzdesini döndürür.</summary>
    private int GetEffectiveSqlRange()
    {
        if (HasCloudTargets)
            return HasFileBackup ? SqlRangeWithCloudAndFile : SqlRangeWithCloudNoFile;

        return HasFileBackup ? SqlRangeWithFileBackup : SqlRangeWithoutFileBackup;
    }

    /// <summary>Dosya fazının başlangıç yüzdesini döndürür (file-only ise 0, değilse SQL fazı sonu).</summary>
    private int GetEffectiveFileBase(bool isFileOnly) =>
        isFileOnly ? 0 : GetEffectiveSqlRange();

    /// <summary>
    /// Konsolide bulut yükleme fazını başlatır.
    /// Mevcut MaxPercent'i baz alarak bulut aralığını [CloudPhaseBase, 100] olarak ayarlar.
    /// </summary>
    /// <returns>Bulut fazının başlangıç yüzdesi.</returns>
    public int StartConsolidatedCloudPhase()
    {
        IsConsolidatedCloudPhase = true;
        CloudPhaseBase = MaxPercent;
        return MaxPercent;
    }

    // ── Hesaplama Metotları ───────────────────────────────────────────────

    /// <summary>
    /// Yeni bir veritabanına geçişte kümülatif yüzdeyi hesaplar (DatabaseProgress event).
    /// </summary>
    /// <param name="currentIndex">1 tabanlı veritabanı sırası.</param>
    /// <param name="totalCount">Toplam veritabanı sayısı.</param>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100].</returns>
    public int CalculateDatabaseProgress(int currentIndex, int totalCount)
    {
        if (totalCount <= 0) return MaxPercent;

        DbIndex = currentIndex;
        DbTotal = totalCount;
        HasVssUpload = false;
        IsVssPhase = false;

        int maxSqlRange = GetEffectiveSqlRange();
        int pct = (int)(((currentIndex - 1.0) / totalCount) * maxSqlRange);
        pct = Math.Max(pct, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    /// <summary>
    /// Dosya yedekleme fazına geçiş yüzdesini hesaplar.
    /// </summary>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100].</returns>
    public int CalculateFileBackupPhaseStart()
    {
        IsFileBackupPhase = true;
        bool isFileOnly = SqlDbCount == 0;
        int fileBase = GetEffectiveFileBase(isFileOnly);
        int pct = Math.Max(fileBase, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    /// <summary>
    /// Dosya sıkıştırma adımı yüzdesini hesaplar.
    /// Bulut hedefi yoksa sıkıştırma fazı daha geniş bir dilim alır.
    /// </summary>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100].</returns>
    public int CalculateFileCompressionProgress()
    {
        bool isFileOnly = SqlDbCount == 0;
        int fileBase = GetEffectiveFileBase(isFileOnly);
        int fileCopyWeight = GetFileCopyWeight(isFileOnly);
        int fileCompressWeight = GetFileCompressWeight(isFileOnly);
        int pct = fileBase + fileCopyWeight + fileCompressWeight;
        pct = Math.Max(pct, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    /// <summary>
    /// Dosya yedekleme fazında kaynak bazlı ilerleme yüzdesini hesaplar.
    /// Her kaynak, dosya kopyalama ağırlığının eşit bir dilimini alır.
    /// Bulut hedefi yoksa kopyalama fazı daha geniş bir dilim alır.
    /// </summary>
    /// <param name="sourceIndex">1 tabanlı tamamlanan kaynak sırası.</param>
    /// <param name="totalSources">Toplam aktif kaynak sayısı.</param>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100] veya -1 (faz uyumsuz).</returns>
    public int CalculateFileSourceProgress(int sourceIndex, int totalSources)
    {
        if (!IsFileBackupPhase || totalSources <= 0 || sourceIndex <= 0)
            return -1;

        bool isFileOnly = SqlDbCount == 0;
        int fileBase = GetEffectiveFileBase(isFileOnly);
        int fileCopyWeight = GetFileCopyWeight(isFileOnly);

        int pct = fileBase + (int)((double)sourceIndex / totalSources * fileCopyWeight);
        pct = Math.Max(pct, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    /// <summary>
    /// Dosya yedekleme temizlik adımı yüzdesini hesaplar (bulut hedefsiz mod).
    /// Dosya kopyalama + sıkıştırma sonrasında temizlik adımını temsil eder.
    /// </summary>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100] veya -1 (faz uyumsuz veya bulut var).</returns>
    public int CalculateFileCleanupProgress()
    {
        if (!IsFileBackupPhase || HasCloudTargets)
            return -1;

        bool isFileOnly = SqlDbCount == 0;
        int fileBase = GetEffectiveFileBase(isFileOnly);
        int fileCopyWeight = GetFileCopyWeight(isFileOnly);
        int fileCompressWeight = GetFileCompressWeight(isFileOnly);
        int fileCleanupWeight = isFileOnly ? 5 : 2;
        int pct = fileBase + fileCopyWeight + fileCompressWeight + fileCleanupWeight;
        pct = Math.Max(pct, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    // ── Dosya fazı ağırlık yardımcıları ───────────────────────────────────

    /// <summary>Dosya kopyalama ağırlığı — bulut yoksa daha geniş dilim.</summary>
    private int GetFileCopyWeight(bool isFileOnly)
    {
        if (HasCloudTargets)
            return isFileOnly ? 25 : 5;

        // Bulut yok: kopyalama + sıkıştırma + temizlik tüm dosya aralığını doldurmalı
        return isFileOnly ? 50 : 10;
    }

    /// <summary>Dosya sıkıştırma ağırlığı — bulut yoksa sıkıştırma daha ağır.</summary>
    private int GetFileCompressWeight(bool isFileOnly)
    {
        if (HasCloudTargets)
            return 0; // Bulut varken sıkıştırma ayrı ağırlık almaz, copy sonrası cloud başlar

        return isFileOnly ? 35 : 7;
    }

    /// <summary>
    /// Bulut hedefsiz (local-mode) SQL adım bazlı ilerleme yüzdesini hesaplar.
    /// </summary>
    /// <param name="stepName">Adım adı (SQL Yedekleme, Doğrulama, Sıkıştırma, Arşiv Doğrulama, Temizlik).</param>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100] veya -1 (bilinmeyen adım).</returns>
    public int CalculateLocalStepProgress(string stepName)
    {
        if (IsFileBackupPhase || DbTotal <= 0 || DbIndex <= 0)
            return -1;

        if (!LocalStepWeights.TryGetValue(stepName, out double stepWeight))
            return -1;

        double maxSqlRange = GetEffectiveSqlRange();
        double slicePerDb = maxSqlRange / DbTotal;
        double dbBase = (DbIndex - 1) * slicePerDb;
        int pct = (int)(dbBase + slicePerDb * stepWeight);
        pct = Math.Max(pct, MaxPercent);
        pct = Math.Clamp(pct, 0, 100);
        MaxPercent = pct;
        return pct;
    }

    /// <summary>
    /// Konsolide bulut yükleme ilerlemesini hesaplar.
    /// Batch progress (0-100) değerini [CloudPhaseBase, 100] aralığına eşler.
    /// </summary>
    /// <param name="batchProgressPercent">Toplam batch yükleme yüzdesi (0-100).</param>
    /// <returns>Klamplanmış, monoton artan yüzde [0-100].</returns>
    public int CalculateCloudUploadProgress(int batchProgressPercent)
    {
        if (!IsConsolidatedCloudPhase)
            return MaxPercent;

        int cloudRange = 100 - CloudPhaseBase;
        int cumPct = CloudPhaseBase + (int)(batchProgressPercent / 100.0 * cloudRange);
        cumPct = Math.Max(cumPct, MaxPercent);
        cumPct = Math.Clamp(cumPct, 0, 100);
        MaxPercent = cumPct;
        return cumPct;
    }
}
