using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Eski yedek dosyalarinin retention politikasina gore temizlenmesini yonetir.
    /// </summary>
    public interface IRetentionService
    {
        /// <summary>
        /// Plan'in retention politikasina gore eski yedekleri temizler.
        /// </summary>
        Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken);
    }
}
