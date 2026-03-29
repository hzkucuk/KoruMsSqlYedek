using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Engine.Retention
{
    /// <summary>
    /// Eski yedeklerin retention politikasına göre temizlenmesini yönetir.
    /// Bulut modda: buluta başarıyla gönderilmemiş dosyalar SİLİNMEZ.
    /// </summary>
    public class RetentionCleanupService : IRetentionService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RetentionCleanupService>();

        private readonly IBackupHistoryManager _historyManager;

        public RetentionCleanupService(IBackupHistoryManager historyManager)
        {
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        }

        public async Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (plan?.Retention == null || string.IsNullOrEmpty(plan.LocalPath))
                return;

            Log.Information("Retention temizliği başlıyor: Plan={PlanName}, Mod={Mode}", plan.PlanName, plan.Mode);

            // Bulut modda geçmiş kayıtlarını yükle — upload durumunu kontrol etmek için
            HashSet<string> cloudProtectedFiles = null;
            if (plan.Mode == BackupMode.Cloud)
            {
                cloudProtectedFiles = BuildCloudProtectedFileSet(plan);
            }

            await Task.Run(() =>
            {
                foreach (string dbName in plan.Databases)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CleanupForDatabase(plan.LocalPath, dbName, plan.Retention, cloudProtectedFiles);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Buluta gönderilmemiş dosyaların tam yol setini oluşturur.
        /// Bu dosyalar retention tarafından silinmeyecektir.
        /// </summary>
        private HashSet<string> BuildCloudProtectedFileSet(BackupPlan plan)
        {
            var protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var history = _historyManager.GetHistoryByPlan(plan.PlanId, 500);

                foreach (var result in history)
                {
                    bool allUploadsSuccessful = result.CloudUploadResults != null
                        && result.CloudUploadResults.Count > 0
                        && result.CloudUploadResults.All(r => r.IsSuccess);

                    if (!allUploadsSuccessful)
                    {
                        // Bu dosya buluta tam gönderilmemiş — koru
                        if (!string.IsNullOrEmpty(result.BackupFilePath))
                            protectedFiles.Add(result.BackupFilePath);

                        if (!string.IsNullOrEmpty(result.CompressedFilePath))
                            protectedFiles.Add(result.CompressedFilePath);
                    }
                }

                if (protectedFiles.Count > 0)
                {
                    Log.Warning(
                        "Bulut koruma: {Count} dosya buluta gönderilemediği için silinmeyecek (Plan={PlanName})",
                        protectedFiles.Count, plan.PlanName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulut koruma kontrolü yapılamadı — güvenlik gereği hiçbir dosya silinmeyecek");
                // Güvenlik: geçmiş okunamazsa tüm dosyalar korunur
                protectedFiles.Add("*PROTECT_ALL*");
            }

            return protectedFiles;
        }

        private void CleanupForDatabase(
            string localPath,
            string databaseName,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles)
        {
            if (!Directory.Exists(localPath))
                return;

            // .bak ve .7z dosyalarını topla
            var allFiles = Directory.GetFiles(localPath, $"{databaseName}_*.*")
                .Where(f => f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            int deletedCount = 0;
            int skippedCount = 0;

            if (retention.Type == RetentionPolicyType.KeepLastN ||
                retention.Type == RetentionPolicyType.Both)
            {
                var toDeleteByCount = allFiles.Skip(retention.KeepLastN).ToList();
                foreach (var file in toDeleteByCount)
                {
                    TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                }
            }

            if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                retention.Type == RetentionPolicyType.Both)
            {
                DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                var toDeleteByAge = allFiles
                    .Where(f => f.CreationTime < cutoff)
                    .ToList();

                foreach (var file in toDeleteByAge)
                {
                    TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                }
            }

            if (deletedCount > 0 || skippedCount > 0)
            {
                Log.Information(
                    "Retention tamamlandı: {Database} — {Deleted} silindi, {Skipped} korundu (bulut bekliyor)",
                    databaseName, deletedCount, skippedCount);
            }
        }

        private void TryDeleteFileWithCloudCheck(
            FileInfo file,
            HashSet<string> cloudProtectedFiles,
            ref int deletedCount,
            ref int skippedCount)
        {
            // Bulut koruma kontrolü
            if (cloudProtectedFiles != null)
            {
                // Geçmiş okunamazsa tüm dosyalar korunur
                if (cloudProtectedFiles.Contains("*PROTECT_ALL*"))
                {
                    skippedCount++;
                    Log.Warning(
                        "Retention atlandı (geçmiş okunamadı, güvenlik modu): {FileName}",
                        file.Name);
                    return;
                }

                if (cloudProtectedFiles.Contains(file.FullName))
                {
                    skippedCount++;
                    Log.Warning(
                        "Retention atlandı (buluta gönderilememiş): {FileName}",
                        file.Name);
                    return;
                }
            }

            TryDeleteFile(file, ref deletedCount);
        }

        private void TryDeleteFile(FileInfo file, ref int deletedCount)
        {
            try
            {
                if (file.Exists)
                {
                    file.Delete();
                    deletedCount++;
                    Log.Information("Eski yedek silindi: {FileName}", file.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya silinemedi: {FileName}", file.Name);
            }
        }
    }
}
