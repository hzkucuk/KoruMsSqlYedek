using System;

namespace KoruMsSqlYedek.Core.Events
{
    public enum BackupActivityType
    {
        Started,
        DatabaseProgress,
        Completed,
        Failed,
        Cancelled
    }

    public class BackupActivityEventArgs : EventArgs
    {
        public string PlanId { get; set; }
        public string PlanName { get; set; }
        public string DatabaseName { get; set; }
        public BackupActivityType ActivityType { get; set; }
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Uygulama genelinde yedekleme aktivitelerini yayınlayan statik event hub.
    /// Tüm UI bileşenleri (MainWindow, Tray, Toast) bu hub'ı dinleyerek güncel kalır.
    /// </summary>
    public static class BackupActivityHub
    {
        public static event EventHandler<BackupActivityEventArgs> ActivityChanged;

        public static void Raise(BackupActivityEventArgs args)
        {
            ActivityChanged?.Invoke(null, args);
        }
    }
}
