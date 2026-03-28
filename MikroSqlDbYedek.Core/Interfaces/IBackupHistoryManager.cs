using System;
using System.Collections.Generic;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Core.Interfaces
{
    /// <summary>
    /// Yedekleme geçmişini yönetir.
    /// Her yedek sonucu JSON olarak kaydedilir (Dashboard için).
    /// </summary>
    public interface IBackupHistoryManager
    {
        /// <summary>
        /// Yedekleme sonucunu geçmişe kaydeder.
        /// </summary>
        void SaveResult(BackupResult result);

        /// <summary>
        /// Belirtilen plan'ın yedekleme geçmişini döndürür.
        /// </summary>
        List<BackupResult> GetHistoryByPlan(string planId, int maxRecords = 50);

        /// <summary>
        /// Tüm planların son yedekleme sonuçlarını döndürür.
        /// </summary>
        List<BackupResult> GetRecentHistory(int maxRecords = 100);

        /// <summary>
        /// Belirtilen tarih aralığındaki geçmişi döndürür.
        /// </summary>
        List<BackupResult> GetHistoryByDateRange(DateTime from, DateTime to);

        /// <summary>
        /// Eski geçmiş kayıtlarını temizler.
        /// </summary>
        void CleanupOldRecords(int keepDays = 90);
    }
}
