using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// AlphaVSS tabanlı Volume Shadow Copy servisi.
    /// Açık/kilitli dosyaları (Outlook PST, OST, Office vb.) shadow copy üzerinden kopyalar.
    /// 
    /// VSS başarısız olursa (yetki eksikliği, servis kapalı) normal kopyalama denenecek
    /// ve uyarı log'a yazılacaktır.
    /// </summary>
    public class VssSnapshotService : IVssService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<VssSnapshotService>();
        private bool _disposed;

        // Aktif snapshot bilgilerini tutar
        private readonly System.Collections.Generic.Dictionary<Guid, VssSnapshotInfo> _snapshots
            = new System.Collections.Generic.Dictionary<Guid, VssSnapshotInfo>();

        private class VssSnapshotInfo
        {
            public string VolumePath { get; set; }
            public string SnapshotDevicePath { get; set; }
            public object BackupComponents { get; set; }
        }

        /// <summary>
        /// .NET 5+'da Assembly.Load(name) deps.json'a kayıtlı olmayan DLL'leri bulamaz.
        /// AlphaVSS.Win.x64 → AlphaVSS.x64.dll eşlemesini uygulama dizininden yükler.
        /// Bu handler process başına bir kez kayıt edilir.
        /// </summary>
        private static readonly object _resolverLock = new();
        private static bool _resolverRegistered;

        private static void EnsureAlphaVssResolver()
        {
            lock (_resolverLock)
            {
                if (_resolverRegistered) return;
                _resolverRegistered = true;

                AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
                {
                    var requestedName = new AssemblyName(args.Name);
                    if (!requestedName.Name.StartsWith("AlphaVSS", StringComparison.OrdinalIgnoreCase))
                        return null;

                    // AlphaVSS.Win.x64 → AlphaVSS.x64.dll
                    // AlphaVSS.Win.x86 → AlphaVSS.x86.dll
                    string fileName = requestedName.Name
                        .Replace("AlphaVSS.Win.", "AlphaVSS.", StringComparison.OrdinalIgnoreCase)
                        + ".dll";

                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        Serilog.Log.Warning(
                            "AlphaVSS platform DLL bulunamadı: {FilePath}", filePath);
                        return null;
                    }

                    Serilog.Log.Debug("AlphaVSS platform DLL yükleniyor: {FilePath}", filePath);
                    return Assembly.LoadFrom(filePath);
                };
            }
        }

        public bool IsAvailable()
        {
            EnsureAlphaVssResolver();

            try
            {
                // VSS servisinin çalışıp çalışmadığını kontrol et
                var vssService = new System.ServiceProcess.ServiceController("VSS");
                bool available = vssService.Status == System.ServiceProcess.ServiceControllerStatus.Running ||
                                 vssService.Status == System.ServiceProcess.ServiceControllerStatus.Stopped;
                vssService.Dispose();
                return available;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "VSS servis durumu kontrol edilemedi.");
                return false;
            }
        }

        public Guid CreateSnapshot(string volumePath, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(volumePath))
                throw new ArgumentNullException(nameof(volumePath));

            // Volume yolunu normalize et (ör. "C:\" formatı)
            string normalizedVolume = Path.GetPathRoot(volumePath);
            if (!normalizedVolume.EndsWith("\\"))
                normalizedVolume += "\\";

            Log.Information("VSS snapshot oluşturuluyor: {Volume}", normalizedVolume);

            EnsureAlphaVssResolver();
            ct.ThrowIfCancellationRequested();

            Alphaleonis.Win32.Vss.IVssBackupComponents backupComponents = null;
            try
            {
                // AlphaVSS ile snapshot oluştur
                var implementation = Alphaleonis.Win32.Vss.VssUtils.LoadImplementation();
                backupComponents = implementation.CreateVssBackupComponents();

                backupComponents.InitializeForBackup(null);
                backupComponents.SetBackupState(false, true,
                    Alphaleonis.Win32.Vss.VssBackupType.Full, false);

                // GatherWriterMetadata bloke edici çağrı — tamamlandıktan sonra ct kontrolü
                backupComponents.GatherWriterMetadata();
                ct.ThrowIfCancellationRequested();

                var snapshotSetId = backupComponents.StartSnapshotSet();
                var snapshotId = backupComponents.AddToSnapshotSet(normalizedVolume);

                ct.ThrowIfCancellationRequested();
                backupComponents.PrepareForBackup();

                ct.ThrowIfCancellationRequested();
                backupComponents.DoSnapshotSet();

                // Snapshot cihaz yolunu al
                var properties = backupComponents.GetSnapshotProperties(snapshotId);
                string devicePath = properties.SnapshotDeviceObject;

                var info = new VssSnapshotInfo
                {
                    VolumePath = normalizedVolume,
                    SnapshotDevicePath = devicePath,
                    BackupComponents = backupComponents
                };

                var id = Guid.NewGuid();
                _snapshots[id] = info;

                Log.Information(
                    "VSS snapshot oluşturuldu: {Volume} → {DevicePath} (ID: {SnapshotId})",
                    normalizedVolume, devicePath, id);

                return id;
            }
            catch (OperationCanceledException)
            {
                try { backupComponents?.Dispose(); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "VSS snapshot oluşturulamadı: {Volume}", normalizedVolume);
                throw;
            }
        }

        public string GetSnapshotFilePath(Guid snapshotId, string originalFilePath)
        {
            if (!_snapshots.TryGetValue(snapshotId, out var info))
                throw new InvalidOperationException($"Snapshot bulunamadı: {snapshotId}");

            // Orijinal dosya yolundaki volume kısmını snapshot device path ile değiştir
            // Ör: "C:\Users\data.pst" → "\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\Users\data.pst"
            string relativePath = originalFilePath.Substring(info.VolumePath.Length);
            string snapshotPath = Path.Combine(info.SnapshotDevicePath, relativePath);

            return snapshotPath;
        }

        public void DeleteSnapshot(Guid snapshotId)
        {
            if (!_snapshots.TryGetValue(snapshotId, out var info))
                return;

            try
            {
                if (info.BackupComponents is Alphaleonis.Win32.Vss.IVssBackupComponents components)
                {
                    components.Dispose();
                }

                _snapshots.Remove(snapshotId);
                Log.Information("VSS snapshot silindi: {SnapshotId}", snapshotId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "VSS snapshot silinemedi: {SnapshotId}", snapshotId);
            }
        }

        public void DeleteAllSnapshots()
        {
            var ids = new System.Collections.Generic.List<Guid>(_snapshots.Keys);
            foreach (var id in ids)
            {
                DeleteSnapshot(id);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DeleteAllSnapshots();
                _disposed = true;
            }
        }
    }
}
