using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Bulut klasörü içeriğini listeleyebilen provider'lar için opsiyonel arayüz.
    /// Retention "folder sweep" için kullanılır: history'de kaydı olmayan ama
    /// yedek dosya isim desenine uyan eski dosyaları temizlemek için.
    /// </summary>
    public interface ICloudFolderListProvider
    {
        /// <summary>
        /// Belirtilen uzak klasördeki dosyaları listeler.
        /// Yalnızca dosyalar (klasörler hariç) ve trashed=false olanlar döner.
        /// </summary>
        Task<List<CloudFileEntry>> ListFolderAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken);
    }
}
