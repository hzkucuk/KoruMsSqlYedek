using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup;

/// <summary>
/// Dosya yedekleme manifest verisi.
/// Her dosyanın son değişiklik zamanı ve boyutunu saklar.
/// Diferansiyel/artırımlı yedeklemelerde referans olarak kullanılır.
/// </summary>
public sealed class FileBackupManifest
{
    [JsonProperty("planId")]
    public string PlanId { get; set; } = string.Empty;

    [JsonProperty("strategy")]
    public FileBackupStrategy Strategy { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Yedeklenen dosyaların mutlak yol → metadata eşlemesi.
    /// Anahtar: dosya mutlak yolu (küçük harfe normalize).
    /// </summary>
    [JsonProperty("files")]
    public Dictionary<string, FileManifestEntry> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Manifest'teki tek dosya kaydı.
/// </summary>
public sealed class FileManifestEntry
{
    [JsonProperty("lastModified")]
    public DateTime LastModified { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }
}

/// <summary>
/// Dosya yedekleme manifest dosyalarını yönetir.
/// Tam yedek manifest'i (file_full.json) ve son yedek manifest'i (file_last.json) saklar/yükler.
/// </summary>
public sealed class FileBackupManifestManager
{
    private static readonly ILogger Log = Serilog.Log.ForContext<FileBackupManifestManager>();

    private const string ManifestsDir = "Manifests";
    private const string FullManifestFileName = "file_full.json";
    private const string LastManifestFileName = "file_last.json";

    /// <summary>
    /// Stratejiye uygun referans manifest'i yükler.
    /// Diferansiyel → son tam yedek manifest'i (file_full.json).
    /// Artırımlı → son yedek manifest'i (file_last.json).
    /// Tam → null (filtreleme yapılmaz).
    /// </summary>
    public FileBackupManifest LoadReferenceManifest(string localPath, FileBackupStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(localPath);

        if (strategy == FileBackupStrategy.Full)
            return null;

        string fileName = strategy == FileBackupStrategy.Differential
            ? FullManifestFileName
            : LastManifestFileName;

        string manifestPath = Path.Combine(localPath, ManifestsDir, fileName);

        if (!File.Exists(manifestPath))
        {
            Log.Warning(
                "Dosya yedekleme manifest bulunamadı, tam yedek olarak çalışılacak: {ManifestPath}",
                manifestPath);
            return null;
        }

        try
        {
            string json = File.ReadAllText(manifestPath);
            var manifest = JsonConvert.DeserializeObject<FileBackupManifest>(json);
            Log.Information(
                "Dosya yedekleme manifest yüklendi: {Strategy}, {FileCount} dosya, {Timestamp}",
                manifest?.Strategy, manifest?.Files?.Count ?? 0, manifest?.Timestamp);
            return manifest;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dosya yedekleme manifest okuma hatası: {ManifestPath}", manifestPath);
            return null;
        }
    }

    /// <summary>
    /// Yedekleme sonrası manifest dosyasını kaydeder.
    /// Tam yedek: hem file_full.json hem file_last.json güncellenir.
    /// Fark/Artırımlı: yalnızca file_last.json güncellenir (önceki dosyalar + yeni değişiklikler birleştirilir).
    /// </summary>
    public void SaveManifest(string localPath, FileBackupManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(localPath);
        ArgumentNullException.ThrowIfNull(manifest);

        string manifestDir = Path.Combine(localPath, ManifestsDir);
        Directory.CreateDirectory(manifestDir);

        string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);

        try
        {
            // Son yedek manifest'i her zaman güncelle
            string lastPath = Path.Combine(manifestDir, LastManifestFileName);
            File.WriteAllText(lastPath, json);

            // Tam yedek ise full manifest'i de güncelle
            if (manifest.Strategy == FileBackupStrategy.Full)
            {
                string fullPath = Path.Combine(manifestDir, FullManifestFileName);
                File.WriteAllText(fullPath, json);
            }

            Log.Information(
                "Dosya yedekleme manifest kaydedildi: {Strategy}, {FileCount} dosya",
                manifest.Strategy, manifest.Files.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dosya yedekleme manifest kaydetme hatası: {LocalPath}", localPath);
        }
    }

    /// <summary>
    /// Referans manifest'e göre değişmiş dosyaları filtreler.
    /// Manifest null ise tüm dosyalar döner (tam yedek davranışı).
    /// </summary>
    /// <param name="files">Kaynak dosya listesi (mutlak yollar).</param>
    /// <param name="referenceManifest">Karşılaştırma referansı (null = tümü).</param>
    /// <returns>Yalnızca değişmiş/yeni dosyalar.</returns>
    public List<string> FilterChangedFiles(List<string> files, FileBackupManifest referenceManifest)
    {
        if (referenceManifest is null || referenceManifest.Files.Count == 0)
            return files;

        var changed = new List<string>();
        int skipped = 0;

        foreach (string filePath in files)
        {
            if (!referenceManifest.Files.TryGetValue(filePath, out FileManifestEntry entry))
            {
                // Yeni dosya — manifest'te yok
                changed.Add(filePath);
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    continue;

                // Boyut veya son değişiklik zamanı farklıysa değişmiş say
                if (fileInfo.Length != entry.Size ||
                    fileInfo.LastWriteTimeUtc != entry.LastModified)
                {
                    changed.Add(filePath);
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya bilgisi okunamadı, yedeklemeye dahil edilecek: {FilePath}", filePath);
                changed.Add(filePath);
            }
        }

        Log.Information(
            "Dosya yedekleme filtre sonucu: {ChangedCount} değişmiş, {SkippedCount} değişmemiş (atlandı)",
            changed.Count, skipped);

        return changed;
    }

    /// <summary>
    /// Yedeklenen dosya listesinden manifest oluşturur.
    /// Fark/Artırımlı: önceki manifest ile birleştirilir (mevcut tüm dosya durumlarını tutar).
    /// </summary>
    public FileBackupManifest BuildManifest(
        string planId,
        FileBackupStrategy strategy,
        IEnumerable<string> backedUpFiles,
        FileBackupManifest previousManifest)
    {
        var manifest = new FileBackupManifest
        {
            PlanId = planId,
            Strategy = strategy,
            Timestamp = DateTime.UtcNow
        };

        // Fark/Artırımlı: önceki manifest dosyalarını taban olarak al
        if (strategy != FileBackupStrategy.Full && previousManifest is not null)
        {
            foreach (var kvp in previousManifest.Files)
                manifest.Files[kvp.Key] = kvp.Value;
        }

        // Yedeklenen dosyaların güncel metadata'sını ekle/güncelle
        foreach (string filePath in backedUpFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    continue;

                manifest.Files[filePath] = new FileManifestEntry
                {
                    LastModified = fileInfo.LastWriteTimeUtc,
                    Size = fileInfo.Length
                };
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Manifest dosya bilgisi okunamadı: {FilePath}", filePath);
            }
        }

        return manifest;
    }
}
