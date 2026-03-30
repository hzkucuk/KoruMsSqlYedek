using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Yarıda kalan upload işlemlerinin durumunu diske kaydeder ve okur.
    /// Dosyalar: %APPDATA%\KoruMsSqlYedek\UploadState\{stateId}.json
    /// </summary>
    public class UploadStateManager
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<UploadStateManager>();
        private readonly string _stateDir;

        public UploadStateManager()
        {
            _stateDir = PathHelper.UploadStateDirectory;
            Directory.CreateDirectory(_stateDir);
        }

        // ── Kaydet / Güncelle ──────────────────────────────────────────────

        /// <summary>
        /// Upload başlamadan önce state kaydeder.
        /// Var olan state üzerine yazar (aynı stateId ile).
        /// </summary>
        public void Save(UploadStateRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            string path = GetPath(record.StateId);
            try
            {
                string json = JsonConvert.SerializeObject(record, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Upload state kaydedilemedi: {StateId}", record.StateId);
            }
        }

        /// <summary>
        /// Session URI ve yüklenen byte miktarını günceller.
        /// </summary>
        public void UpdateProgress(string stateId, string sessionUri, long bytesUploaded)
        {
            var record = Load(stateId);
            if (record == null) return;
            record.ResumeSessionUri = sessionUri;
            record.BytesUploaded = bytesUploaded;
            record.LastAttemptAt = DateTime.UtcNow;
            Save(record);
        }

        /// <summary>Upload tamamlandı — state dosyasını sil.</summary>
        public void Delete(string stateId)
        {
            string path = GetPath(stateId);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Upload state silinemedi: {StateId}", stateId);
            }
        }

        // ── Oku ───────────────────────────────────────────────────────────

        public UploadStateRecord Load(string stateId)
        {
            string path = GetPath(stateId);
            return LoadFile(path);
        }

        /// <summary>
        /// Tüm bekleyen upload state kayıtlarını döndürür.
        /// Bozuk / süresi dolmuş kayıtlar otomatik temizlenir.
        /// </summary>
        public List<UploadStateRecord> GetAll()
        {
            var records = new List<UploadStateRecord>();
            var cutoff = DateTime.UtcNow.AddDays(-5); // 5 günden eski session URI geçersizdir

            foreach (var file in Directory.GetFiles(_stateDir, "*.json"))
            {
                var rec = LoadFile(file);
                if (rec == null)
                {
                    TryDeleteFile(file);
                    continue;
                }

                // Yerel dosya silinmişse state'i temizle
                if (!File.Exists(rec.LocalFilePath))
                {
                    Log.Information("Yerel dosya yok, state temizleniyor: {File}", rec.LocalFilePath);
                    TryDeleteFile(file);
                    continue;
                }

                // Session URI 5 günden eskiyse sıfırla (Google/OD session süresi dolmuş)
                if (!string.IsNullOrEmpty(rec.ResumeSessionUri) && rec.StartedAt < cutoff)
                {
                    Log.Warning("Session URI süresi dolmuş, sıfırlanıyor: {StateId}", rec.StateId);
                    rec.ResumeSessionUri = null;
                    rec.BytesUploaded = 0;
                    Save(rec);
                }

                records.Add(rec);
            }

            return records;
        }

        // ── SHA-256 ────────────────────────────────────────────────────────

        /// <summary>
        /// Dosyanın SHA-256 özetini hesaplar (hex, lowercase).
        /// Büyük dosyalarda streaming ile belleği zorlamaz.
        /// </summary>
        public static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize: 1024 * 1024);
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        // ── Yardımcılar ────────────────────────────────────────────────────

        private string GetPath(string stateId) =>
            Path.Combine(_stateDir, $"{stateId}.json");

        private static UploadStateRecord LoadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UploadStateRecord>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
