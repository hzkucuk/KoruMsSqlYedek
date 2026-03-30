using System;

namespace KoruMsSqlYedek.Core.IPC
{
    /// <summary>
    /// Uygulama genelinde servis durum mesajlarını yayınlayan statik event hub.
    /// ServicePipeClient, servisten gelen ServiceStatusMessage'ı burada raise eder;
    /// MainWindow ve diğer UI bileşenleri abone olarak sonraki çalışma zamanlarını günceller.
    /// </summary>
    public static class ServiceStatusHub
    {
        /// <summary>Servisten yeni durum bilgisi geldiğinde ateşlenir.</summary>
        public static event EventHandler<ServiceStatusMessage> StatusReceived;

        /// <summary>Yeni durum mesajını tüm abonelere yayınlar.</summary>
        public static void Raise(ServiceStatusMessage message)
        {
            StatusReceived?.Invoke(null, message);
        }
    }
}
