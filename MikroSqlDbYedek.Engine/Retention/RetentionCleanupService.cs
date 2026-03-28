using System;
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
    /// </summary>
    public class RetentionCleanupService : IRetentionService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RetentionCleanupService>();

        public async Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (plan?.Retention == null || string.IsNullOrEmpty(plan.LocalPath))
                return;

            Log.Information("Retention temizliği başlıyor: Plan={PlanName}", plan.PlanName);

            await Task.Run(() =>
            {
                foreach (string dbName in plan.Databases)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CleanupForDatabase(plan.LocalPath, dbName, plan.Retention);
                }
            }, cancellationToken);
        }

        private void CleanupForDatabase(string localPath, string databaseName, RetentionPolicy retention)
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

            if (retention.Type == RetentionPolicyType.KeepLastN ||
                retention.Type == RetentionPolicyType.Both)
            {
                var toDeleteByCount = allFiles.Skip(retention.KeepLastN).ToList();
                foreach (var file in toDeleteByCount)
                {
                    TryDeleteFile(file, ref deletedCount);
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
                    TryDeleteFile(file, ref deletedCount);
                }
            }

            if (deletedCount > 0)
            {
                Log.Information(
                    "Retention temizliği tamamlandı: {Database} — {Count} dosya silindi",
                    databaseName, deletedCount);
            }
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
