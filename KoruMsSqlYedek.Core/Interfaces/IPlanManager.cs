using System.Collections.Generic;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Yedekleme planlarının CRUD işlemlerini yönetir.
    /// Planlar JSON dosyaları olarak %APPDATA%\KoruMsSqlYedek\Plans\ altında saklanır.
    /// </summary>
    public interface IPlanManager
    {
        /// <summary>Tüm planları yükler.</summary>
        List<BackupPlan> GetAllPlans();

        /// <summary>ID ile tek plan yükler.</summary>
        BackupPlan GetPlanById(string planId);

        /// <summary>Yeni plan kaydeder veya mevcut planı günceller.</summary>
        void SavePlan(BackupPlan plan);

        /// <summary>Planı siler.</summary>
        bool DeletePlan(string planId);

        /// <summary>Planı JSON dosyasına dışa aktarır.</summary>
        void ExportPlan(string planId, string exportFilePath);

        /// <summary>JSON dosyasından plan içe aktarır.</summary>
        BackupPlan ImportPlan(string importFilePath);
    }
}
