using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using KoruMsSqlYedek.Core.Events;

namespace KoruMsSqlYedek.Core.IPC
{
    /// <summary>
    /// Tray ↔ Service Named Pipe protokolü.
    /// Tüm mesajlar JSON, newline-delimited (\n) olarak gönderilir.
    /// "Type" alanına göre discriminated deserialization yapılır.
    /// </summary>
    public static class PipeMessageType
    {
        // Tray → Service (komutlar)
        public const string ManualBackup = "ManualBackup";
        public const string CancelBackup  = "CancelBackup";
        public const string RequestStatus = "RequestStatus";

        // Service → Tray (olaylar)
        public const string BackupActivity  = "BackupActivity";
        public const string ServiceStatus   = "ServiceStatus";
    }

    /// <summary>Her pipe mesajının ortak alanları.</summary>
    public class PipeMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    // ── Komutlar (Tray → Service) ────────────────────────────────────────────

    /// <summary>Belirtilen planı manuel olarak hemen çalıştır.</summary>
    public class ManualBackupCommand : PipeMessage
    {
        public ManualBackupCommand() { Type = PipeMessageType.ManualBackup; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        /// <summary>"Full", "Differential", "Incremental"</summary>
        [JsonProperty("backupType")]
        public string BackupType { get; set; }
    }

    /// <summary>Çalışan yedeği iptal et.</summary>
    public class CancelBackupCommand : PipeMessage
    {
        public CancelBackupCommand() { Type = PipeMessageType.CancelBackup; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }
    }

    /// <summary>Servis durum bilgisi iste (service yanıt olarak ServiceStatusMessage gönderir).</summary>
    public class RequestStatusCommand : PipeMessage
    {
        public RequestStatusCommand() { Type = PipeMessageType.RequestStatus; }
    }

    // ── Olaylar (Service → Tray) ─────────────────────────────────────────────

    /// <summary>
    /// BackupActivityEventArgs'ı pipe üzerinden taşır.
    /// Alıcı traf BackupActivityHub.Raise() ile yerel olarak yayınlar.
    /// </summary>
    public class BackupActivityMessage : PipeMessage
    {
        public BackupActivityMessage() { Type = PipeMessageType.BackupActivity; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("planName")]
        public string PlanName { get; set; }

        [JsonProperty("databaseName")]
        public string DatabaseName { get; set; }

        [JsonProperty("activityType")]
        public BackupActivityType ActivityType { get; set; }

        [JsonProperty("currentIndex")]
        public int CurrentIndex { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("stepName")]
        public string StepName { get; set; }

        [JsonProperty("cloudTargetName")]
        public string CloudTargetName { get; set; }

        [JsonProperty("cloudTargetIndex")]
        public int CloudTargetIndex { get; set; }

        [JsonProperty("cloudTargetTotal")]
        public int CloudTargetTotal { get; set; }

        [JsonProperty("cloudFileName")]
        public string CloudFileName { get; set; }

        [JsonProperty("cloudFileIndex")]
        public int CloudFileIndex { get; set; }

        [JsonProperty("cloudFileTotal")]
        public int CloudFileTotal { get; set; }

        [JsonProperty("progressPercent")]
        public int ProgressPercent { get; set; }

        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("bytesSent")]
        public long BytesSent { get; set; }

        [JsonProperty("bytesTotal")]
        public long BytesTotal { get; set; }

        [JsonProperty("speedBytesPerSecond")]
        public long SpeedBytesPerSecond { get; set; }

        [JsonProperty("remoteFileSizeBytes")]
        public long RemoteFileSizeBytes { get; set; }

        [JsonProperty("localFileSizeBytes")]
        public long LocalFileSizeBytes { get; set; }

        [JsonProperty("isIntegrityVerified")]
        public bool? IsIntegrityVerified { get; set; }

        [JsonProperty("hasFileBackup")]
        public bool HasFileBackup { get; set; }

        [JsonProperty("hasCloudTargets")]
        public bool HasCloudTargets { get; set; }

        [JsonProperty("abandonedFiles")]
        public List<string> AbandonedFiles { get; set; }

        /// <summary>
        /// Plan konfigürasyonundaki ToastEnabled değeri.
        /// Service tarafı plan config'den okuyarak doldurur; Win tarafı buna göre balloon gösterir.
        /// </summary>
        [JsonProperty("toastEnabled")]
        public bool ToastEnabled { get; set; } = true;

        /// <summary>BackupActivityEventArgs'ı bu mesaja kopyalar.</summary>
        public static BackupActivityMessage FromArgs(BackupActivityEventArgs args)
        {
            return new BackupActivityMessage
            {
                PlanId            = args.PlanId,
                PlanName          = args.PlanName,
                DatabaseName      = args.DatabaseName,
                ActivityType      = args.ActivityType,
                CurrentIndex      = args.CurrentIndex,
                TotalCount        = args.TotalCount,
                Message           = args.Message,
                StepName          = args.StepName,
                CloudTargetName   = args.CloudTargetName,
                CloudTargetIndex  = args.CloudTargetIndex,
                CloudTargetTotal  = args.CloudTargetTotal,
                CloudFileName     = args.CloudFileName,
                CloudFileIndex    = args.CloudFileIndex,
                CloudFileTotal    = args.CloudFileTotal,
                ProgressPercent   = args.ProgressPercent,
                IsSuccess         = args.IsSuccess,
                BytesSent         = args.BytesSent,
                BytesTotal        = args.BytesTotal,
                SpeedBytesPerSecond = args.SpeedBytesPerSecond,
                RemoteFileSizeBytes = args.RemoteFileSizeBytes,
                LocalFileSizeBytes  = args.LocalFileSizeBytes,
                IsIntegrityVerified = args.IsIntegrityVerified,
                HasFileBackup     = args.HasFileBackup,
                HasCloudTargets   = args.HasCloudTargets,
                AbandonedFiles    = args.AbandonedFiles,
                ToastEnabled      = args.ToastEnabled
            };
        }

        /// <summary>Bu mesajı BackupActivityEventArgs'a dönüştürür.</summary>
        public BackupActivityEventArgs ToArgs()
        {
            return new BackupActivityEventArgs
            {
                PlanId           = PlanId,
                PlanName         = PlanName,
                DatabaseName     = DatabaseName,
                ActivityType     = ActivityType,
                CurrentIndex     = CurrentIndex,
                TotalCount       = TotalCount,
                Message          = Message,
                StepName         = StepName,
                CloudTargetName  = CloudTargetName,
                CloudTargetIndex = CloudTargetIndex,
                CloudTargetTotal = CloudTargetTotal,
                CloudFileName    = CloudFileName,
                CloudFileIndex   = CloudFileIndex,
                CloudFileTotal   = CloudFileTotal,
                ProgressPercent  = ProgressPercent,
                IsSuccess        = IsSuccess,
                BytesSent        = BytesSent,
                BytesTotal       = BytesTotal,
                SpeedBytesPerSecond = SpeedBytesPerSecond,
                RemoteFileSizeBytes = RemoteFileSizeBytes,
                LocalFileSizeBytes  = LocalFileSizeBytes,
                IsIntegrityVerified = IsIntegrityVerified,
                HasFileBackup    = HasFileBackup,
                HasCloudTargets  = HasCloudTargets,
                AbandonedFiles   = AbandonedFiles,
                ToastEnabled     = ToastEnabled
            };
        }
    }

    /// <summary>
    /// Servis genel durum bilgisi.
    /// Scheduler başlatılınca ve her bağlanan client'a gönderilir.
    /// </summary>
    public class ServiceStatusMessage : PipeMessage
    {
        public ServiceStatusMessage() { Type = PipeMessageType.ServiceStatus; }

        [JsonProperty("isRunning")]
        public bool IsRunning { get; set; }

        /// <summary>PlanId → sonraki tetiklenme zamanı (ISO 8601). null = zamanlanmamış.</summary>
        [JsonProperty("nextFireTimes")]
        public Dictionary<string, string> NextFireTimes { get; set; }
            = new Dictionary<string, string>();
    }

    /// <summary>Mesaj serileştirme / deserileştirme yardımcıları.</summary>
    public static class PipeSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>Herhangi bir PipeMessage'ı tek satır JSON'a çevirir.</summary>
        public static string Serialize(PipeMessage message)
        {
            return JsonConvert.SerializeObject(message, Settings);
        }

        /// <summary>
        /// JSON satırını okuyup Type alanına göre doğru sınıfa deserialize eder.
        /// Bilinmeyen Type değerlerinde null döner.
        /// </summary>
        public static PipeMessage Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var probe = JsonConvert.DeserializeObject<PipeMessage>(json);
                if (probe == null) return null;

                switch (probe.Type)
                {
                    case PipeMessageType.ManualBackup:
                        return JsonConvert.DeserializeObject<ManualBackupCommand>(json);
                    case PipeMessageType.CancelBackup:
                        return JsonConvert.DeserializeObject<CancelBackupCommand>(json);
                    case PipeMessageType.RequestStatus:
                        return JsonConvert.DeserializeObject<RequestStatusCommand>(json);
                    case PipeMessageType.BackupActivity:
                        return JsonConvert.DeserializeObject<BackupActivityMessage>(json);
                    case PipeMessageType.ServiceStatus:
                        return JsonConvert.DeserializeObject<ServiceStatusMessage>(json);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
