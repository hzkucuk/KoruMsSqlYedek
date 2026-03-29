using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class BackupHistoryManagerTests
    {
        private BackupHistoryManager _historyManager;
        private string _testPlanId;
        private string _testHistoryDir;

        [TestInitialize]
        public void Setup()
        {
            _testHistoryDir = Path.Combine(Path.GetTempPath(), "KoruMsSqlYedekTests", "History_" + Guid.NewGuid().ToString("N"));
            _historyManager = new BackupHistoryManager(_testHistoryDir);
            _testPlanId = $"test-plan-{Guid.NewGuid():N}";
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testHistoryDir))
            {
                try { Directory.Delete(_testHistoryDir, true); }
                catch { /* test temizliği */ }
            }
        }

        [TestMethod]
        public void SaveResult_And_GetHistoryByPlan_ReturnsCorrect()
        {
            // Arrange
            var result1 = TestDataFactory.CreateSuccessResult(_testPlanId, "DB1");
            var result2 = TestDataFactory.CreateSuccessResult(_testPlanId, "DB2");
            var otherPlanResult = TestDataFactory.CreateSuccessResult("other-plan", "DB3");

            // Act
            _historyManager.SaveResult(result1);
            _historyManager.SaveResult(result2);
            _historyManager.SaveResult(otherPlanResult);

            var history = _historyManager.GetHistoryByPlan(_testPlanId);

            // Assert — sadece test planı sonuçları dönmeli
            history.Should().HaveCount(2);
            history.Should().OnlyContain(r => r.PlanId == _testPlanId);
        }

        [TestMethod]
        public void SaveResult_NullResult_DoesNotThrow()
        {
            // Act — null geçilince hata atmamalı
            Action act = () => _historyManager.SaveResult(null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void GetRecentHistory_ReturnsOrderedByDate()
        {
            // Arrange — aynı gün içinde yakın zamanlar kullan (global Take limiti aşılmasın)
            var oldResult = TestDataFactory.CreateSuccessResult(_testPlanId, "OldDB");
            oldResult.StartedAt = DateTime.UtcNow.AddMinutes(-30);
            oldResult.CompletedAt = DateTime.UtcNow.AddMinutes(-20);

            var newResult = TestDataFactory.CreateSuccessResult(_testPlanId, "NewDB");
            newResult.StartedAt = DateTime.UtcNow.AddMinutes(-5);
            newResult.CompletedAt = DateTime.UtcNow;

            _historyManager.SaveResult(oldResult);
            _historyManager.SaveResult(newResult);

            // Act — plan bazlı sorgula (global kayıt birikiminden etkilenmesin)
            var testPlanResults = _historyManager.GetHistoryByPlan(_testPlanId, maxRecords: 100);

            // Assert — yeni olan önce gelmeli (OrderByDescending)
            testPlanResults.Should().HaveCountGreaterOrEqualTo(2);
            testPlanResults[0].StartedAt.Should().BeOnOrAfter(testPlanResults[1].StartedAt);

            // GetRecentHistory sıralama doğrulaması
            var recent = _historyManager.GetRecentHistory(10000);
            recent.Should().NotBeEmpty();
            for (int i = 1; i < recent.Count; i++)
            {
                recent[i - 1].StartedAt.Should().BeOnOrAfter(recent[i].StartedAt,
                    "GetRecentHistory sonuçları tarihe göre azalan sırada olmalı");
            }
        }

        [TestMethod]
        public void GetHistoryByPlan_MaxRecords_LimitsResults()
        {
            // Arrange — 5 sonuç kaydet
            for (int i = 0; i < 5; i++)
            {
                var result = TestDataFactory.CreateSuccessResult(_testPlanId, $"DB{i}");
                _historyManager.SaveResult(result);
            }

            // Act — maxRecords=3 ile sorgula
            var history = _historyManager.GetHistoryByPlan(_testPlanId, maxRecords: 3);

            // Assert
            history.Should().HaveCount(3);
        }

        [TestMethod]
        public void GetHistoryByDateRange_ReturnsCorrectRange()
        {
            // Arrange
            var todayResult = TestDataFactory.CreateSuccessResult(_testPlanId, "TodayDB");
            todayResult.StartedAt = DateTime.UtcNow;
            todayResult.CompletedAt = DateTime.UtcNow;
            _historyManager.SaveResult(todayResult);

            // Act — bugünün aralığı
            var from = DateTime.UtcNow.Date;
            var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            var history = _historyManager.GetHistoryByDateRange(from, to);

            // Assert
            history.Should().NotBeEmpty();
            history.Should().Contain(r => r.PlanId == _testPlanId);
        }

        [TestMethod]
        public void GetHistoryByDateRange_EmptyRange_ReturnsEmpty()
        {
            // Act — geçmiş bir tarih aralığı (veri olmayan)
            var from = new DateTime(2020, 1, 1);
            var to = new DateTime(2020, 1, 2);
            var history = _historyManager.GetHistoryByDateRange(from, to);

            // Assert
            history.Should().BeEmpty();
        }

        [TestMethod]
        public void SaveResult_FailedResult_PersistsErrorMessage()
        {
            // Arrange
            var failedResult = TestDataFactory.CreateFailedResult(_testPlanId, "Bağlantı zaman aşımı");

            // Act
            _historyManager.SaveResult(failedResult);
            var history = _historyManager.GetHistoryByPlan(_testPlanId);

            // Assert
            history.Should().Contain(r =>
                r.Status == BackupResultStatus.Failed &&
                r.ErrorMessage == "Bağlantı zaman aşımı");
        }

        [TestMethod]
        public void SaveResult_MultipleSameDay_AppendsToDayFile()
        {
            // Arrange — aynı gün içinde birden fazla kayıt
            for (int i = 0; i < 3; i++)
            {
                var result = TestDataFactory.CreateSuccessResult(_testPlanId, $"SameDayDB{i}");
                result.CompletedAt = DateTime.UtcNow; // hepsi bugüne kaydedilecek
                _historyManager.SaveResult(result);
            }

            // Act
            var history = _historyManager.GetHistoryByPlan(_testPlanId);

            // Assert — 3 kayıt olmalı (aynı gün dosyasında)
            history.Should().HaveCountGreaterOrEqualTo(3);
        }
    }
}
