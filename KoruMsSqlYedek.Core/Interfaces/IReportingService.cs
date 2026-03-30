using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Periyodik yedekleme özet raporu oluşturan ve gönderen servis.
    /// </summary>
    public interface IReportingService
    {
        /// <summary>
        /// Belirtilen plan için periyodik rapor oluşturur ve yapılandırılmış e-posta adresine gönderir.
        /// Plan'ın <see cref="ReportingConfig.IsEnabled"/> değeri false ise hiçbir şey yapmaz.
        /// </summary>
        Task SendReportAsync(BackupPlan plan, CancellationToken cancellationToken);
    }
}
