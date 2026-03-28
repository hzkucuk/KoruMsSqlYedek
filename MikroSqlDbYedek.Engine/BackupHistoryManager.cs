using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Engine
{
    /// <summary>
    /// Yedekleme geçmişini JSON dosyalarında saklar.
    /// Her gün için ayrı dosya: %APPDATA%\MikroSqlDbYedek\History\{yyyy-MM-dd}.json
    /// Dashboard ve raporlama için geçmiş verileri sunar.
    /// </summary>
    public class BackupHistoryManager : IBackupHistoryManager
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupHistoryManager>();
        private static readonly string DefaultHistoryDirectory = Path.Combine(PathHelper.AppDataDirectory, "History");
        private static readonly object FileLock = new object();

        private readonly string _historyDirectory;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
        };

        /// <summary>
        /// Varsayılan History dizinini kullanır (%APPDATA%\MikroSqlDbYedek\History\).
        /// </summary>
        public BackupHistoryManager() : this(null)
        {
        }

        /// <summary>
        /// Belirtilen dizini kullanır. Test izolasyonu için kullanılır.
        /// </summary>
        /// <param name="historyDirectory">Özel History dizini. null ise varsayılan kullanılır.</param>
        public BackupHistoryManager(string historyDirectory)
        {
            _historyDirectory = historyDirectory ?? DefaultHistoryDirectory;
            Directory.CreateDirectory(_historyDirectory);
        }

        public void SaveResult(BackupResult result)
        {
            if (result == null)
                return;

            try
            {
                string dateKey = (result.CompletedAt ?? result.StartedAt).ToString("yyyy-MM-dd");
                string filePath = Path.Combine(_historyDirectory, $"{dateKey}.json");

                lock (FileLock)
                {
                    var records = LoadDayRecords(filePath);
                    records.Add(result);
                    string json = JsonConvert.SerializeObject(records, JsonSettings);
                    File.WriteAllText(filePath, json);
                }

                Log.Debug(
                    "Yedek geçmişi kaydedildi: {CorrelationId} — {Database} ({Status})",
                    result.CorrelationId, result.DatabaseName, result.Status);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yedek geçmişi kaydedilemedi: {CorrelationId}", result.CorrelationId);
            }
        }

        public List<BackupResult> GetHistoryByPlan(string planId, int maxRecords = 50)
        {
            return GetAllRecords()
                .Where(r => r.PlanId == planId)
                .OrderByDescending(r => r.StartedAt)
                .Take(maxRecords)
                .ToList();
        }

        public List<BackupResult> GetRecentHistory(int maxRecords = 100)
        {
            return GetAllRecords()
                .OrderByDescending(r => r.StartedAt)
                .Take(maxRecords)
                .ToList();
        }

        public List<BackupResult> GetHistoryByDateRange(DateTime from, DateTime to)
        {
            var results = new List<BackupResult>();

            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                string filePath = Path.Combine(_historyDirectory, $"{date:yyyy-MM-dd}.json");
                results.AddRange(LoadDayRecords(filePath));
            }

            return results
                .Where(r => r.StartedAt >= from && r.StartedAt <= to)
                .OrderByDescending(r => r.StartedAt)
                .ToList();
        }

        public void CleanupOldRecords(int keepDays = 90)
        {
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-keepDays);

                foreach (string file in Directory.GetFiles(_historyDirectory, "*.json"))
                {
                    var fileDate = File.GetCreationTime(file);
                    if (fileDate < cutoff)
                    {
                        File.Delete(file);
                        Log.Debug("Eski geçmiş dosyası silindi: {FileName}", Path.GetFileName(file));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Geçmiş temizliği hatası");
            }
        }

        private List<BackupResult> GetAllRecords()
        {
            var results = new List<BackupResult>();

            if (!Directory.Exists(_historyDirectory))
                return results;

            foreach (string file in Directory.GetFiles(_historyDirectory, "*.json")
                .OrderByDescending(f => f))
            {
                results.AddRange(LoadDayRecords(file));
            }

            return results;
        }

        private List<BackupResult> LoadDayRecords(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<BackupResult>();

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<BackupResult>>(json, JsonSettings)
                       ?? new List<BackupResult>();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Geçmiş dosyası okunamadı: {FilePath}", filePath);
                return new List<BackupResult>();
            }
        }
    }
}
