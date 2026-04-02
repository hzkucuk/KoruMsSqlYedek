using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Scheduling;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Stress")]
    public class StressTests
    {
        // ── Eşzamanlı Farklı Planlar ─────────────────────────────────────────

        [TestMethod]
        public async Task Stress_MultipleDifferentPlans_RunInParallel()
        {
            // Arrange — 5 farklı plan eşzamanlı çalıştırılır; her biri kendi pipeline'ını tamamlamalı
            const int planCount = 5;
            var completedPlans = new ConcurrentBag<string>();
            var startedPlans = new ConcurrentBag<string>();

            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Started)
                    startedPlans.Add(e.PlanId);
                if (e.ActivityType == BackupActivityType.Completed)
                    completedPlans.Add(e.PlanId);
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                var tasks = new List<Task>();
                var planIds = new List<string>();

                for (int i = 0; i < planCount; i++)
                {
                    var executor = CreateExecutor(out var mocks);
                    var plan = TestDataFactory.CreateValidPlan($"Stress Plan {i}");
                    plan.Databases = new List<string> { $"StressDB_{i}" };
                    planIds.Add(plan.PlanId);

                    var mockContext = CreateJobContext(plan.PlanId, "Full");
                    mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

                    mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                            It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                            It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                            It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                        .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                            async (conn, db, type, path, prog, ct) =>
                            {
                                await Task.Delay(50, ct); // I/O simülasyonu
                                return TestDataFactory.CreateSuccessResult(plan.PlanId, db);
                            });

                    mocks.Compression.Setup(c => c.CompressAsync(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                            It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1024L * 1024 * 30);

                    tasks.Add(executor.Execute(mockContext.Object));
                }

                // Act — tüm planları paralel başlat
                await Task.WhenAll(tasks);

                // Assert — her plan Started ve Completed olmalı
                startedPlans.Should().HaveCount(planCount, "her plan başlamalı");
                completedPlans.Should().HaveCount(planCount, "her plan tamamlanmalı");

                foreach (string planId in planIds)
                    completedPlans.Should().Contain(planId);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Stress_SamePlanConcurrentTrigger_SecondSkipped()
        {
            // Arrange — Aynı plan 2 kez eşzamanlı tetiklenir;
            // SemaphoreSlim(1,1) nedeniyle biri beklemeli veya atlanmalı
            var executor = CreateExecutor(out var mocks);
            var plan = TestDataFactory.CreateValidPlan("SamePlan");
            plan.Databases = new List<string> { "DB1" };

            mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var callCount = 0;
            mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                    async (conn, db, type, path, prog, ct) =>
                    {
                        Interlocked.Increment(ref callCount);
                        await Task.Delay(200, ct); // uzun I/O simülasyonu
                        return TestDataFactory.CreateSuccessResult(plan.PlanId, db);
                    });

            mocks.Compression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            var ctx1 = CreateJobContext(plan.PlanId, "Full");
            var ctx2 = CreateJobContext(plan.PlanId, "Full");

            // Act — aynı planı 2 kez paralel tetikle
            var task1 = executor.Execute(ctx1.Object);
            var task2 = executor.Execute(ctx2.Object);
            await Task.WhenAll(task1, task2);

            // Assert — SemaphoreSlim(1,1) WaitAsync(0) ile sadece 1 tanesi SQL çağırmalı
            // İkinci çağrı planLock alamadığından atlanır
            callCount.Should().Be(1, "aynı plan eşzamanlı çalıştırılamaz — SemaphoreSlim(1,1) biri atar");
        }

        [TestMethod]
        public async Task Stress_LargeDatabaseList_AllProcessed()
        {
            // Arrange — 20 veritabanı ile plan; hepsi sırayla işlenmeli
            const int dbCount = 20;
            var executor = CreateExecutor(out var mocks);
            var plan = TestDataFactory.CreateValidPlan("LargePlan");
            plan.Databases = Enumerable.Range(1, dbCount).Select(i => $"DB_{i:D3}").ToList();

            var mockContext = CreateJobContext(plan.PlanId, "Full");
            mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            var backedUpDbs = new ConcurrentBag<string>();
            mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                    (conn, db, type, path, prog, ct) =>
                    {
                        backedUpDbs.Add(db);
                        return Task.FromResult(TestDataFactory.CreateSuccessResult(plan.PlanId, db));
                    });

            mocks.Compression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await executor.Execute(mockContext.Object);

            // Assert — 20 DB'nin tümü yedeklenmeli
            backedUpDbs.Should().HaveCount(dbCount);
            foreach (string dbName in plan.Databases)
                backedUpDbs.Should().Contain(dbName);

            mocks.HistoryManager.Verify(h => h.SaveResult(It.IsAny<BackupResult>()), Times.Exactly(dbCount));
        }

        [TestMethod]
        public async Task Stress_MultiplePlansWithMixedFailures_EachCompletesIndependently()
        {
            // Arrange — 3 plan: 1 başarılı, 1 başarısız (SQL hata), 1 iptal edilmiş
            // Her plan diğerini etkilemeden bitmeli
            var completedEvents = new ConcurrentBag<BackupActivityEventArgs>();

            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType is BackupActivityType.Completed
                    or BackupActivityType.Failed
                    or BackupActivityType.Cancelled)
                    completedEvents.Add(e);
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                // Plan 1 — başarılı
                var exec1 = CreateExecutor(out var mocks1);
                var plan1 = TestDataFactory.CreateValidPlan("Success Plan");
                plan1.Databases = new List<string> { "SuccessDB" };
                mocks1.PlanManager.Setup(p => p.GetPlanById(plan1.PlanId)).Returns(plan1);
                mocks1.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                        It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                        It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan1.PlanId));
                mocks1.Compression.Setup(c => c.CompressAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1024L * 1024 * 30);

                // Plan 2 — SQL exception → Failed
                var exec2 = CreateExecutor(out var mocks2);
                var plan2 = TestDataFactory.CreateValidPlan("Fail Plan");
                plan2.Databases = new List<string> { "FailDB" };
                mocks2.PlanManager.Setup(p => p.GetPlanById(plan2.PlanId)).Returns(plan2);
                mocks2.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                        It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                        It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("SQL Hata"));

                // Plan 3 — iptal
                var exec3 = CreateExecutor(out var mocks3);
                var plan3 = TestDataFactory.CreateValidPlan("Cancel Plan");
                plan3.Databases = new List<string> { "CancelDB" };
                mocks3.PlanManager.Setup(p => p.GetPlanById(plan3.PlanId)).Returns(plan3);
                mocks3.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                        It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                        It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException());

                var ctx1 = CreateJobContext(plan1.PlanId, "Full");
                var ctx2 = CreateJobContext(plan2.PlanId, "Full");
                var ctx3 = CreateJobContext(plan3.PlanId, "Full");

                // Act — hepsini eşzamanlı başlat
                await Task.WhenAll(
                    exec1.Execute(ctx1.Object),
                    exec2.Execute(ctx2.Object),
                    exec3.Execute(ctx3.Object));

                // Assert — her plan kendine ait son duruma ulaşmalı
                completedEvents.Should().HaveCount(3,
                    "3 farklı plan 3 farklı sonuçla bitmeli");

                completedEvents.Should().Contain(e =>
                    e.PlanId == plan1.PlanId && e.ActivityType == BackupActivityType.Completed);
                completedEvents.Should().Contain(e =>
                    e.PlanId == plan2.PlanId && e.ActivityType == BackupActivityType.Failed);
                completedEvents.Should().Contain(e =>
                    e.PlanId == plan3.PlanId && e.ActivityType == BackupActivityType.Cancelled);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Stress_RapidSequentialExecutions_SamePlan_NoDeadlock()
        {
            // Arrange — Aynı planı 10 kez sırayla hızla çalıştır; deadlock olmamalı
            const int runCount = 10;
            var executor = CreateExecutor(out var mocks);
            var plan = TestDataFactory.CreateValidPlan("RapidPlan");
            plan.Databases = new List<string> { "DB1" };

            mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);
            mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));
            mocks.Compression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act — sırayla 10 kez çalıştır
            for (int i = 0; i < runCount; i++)
            {
                var ctx = CreateJobContext(plan.PlanId, "Full");
                await executor.Execute(ctx.Object);
            }

            // Assert — deadlock yok; her çalıştırma SQL backup çağırmış olmalı
            mocks.SqlBackup.Verify(
                s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(runCount));
        }

        [TestMethod]
        public async Task Stress_ParallelPlansWithCloudUpload_AllComplete()
        {
            // Arrange — 4 plan, her biri bulut upload ile; hepsi paralel tamamlanmalı
            const int planCount = 4;
            var completedPlans = new ConcurrentBag<string>();

            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.ActivityType == BackupActivityType.Completed)
                    completedPlans.Add(e.PlanId);
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                var tasks = new List<Task>();
                var planIds = new List<string>();

                for (int i = 0; i < planCount; i++)
                {
                    var executor = CreateExecutor(out var mocks);
                    var plan = TestDataFactory.CreatePlanWithCloudTargets();
                    plan.PlanName = $"CloudPlan_{i}";
                    plan.Databases = new List<string> { $"CloudDB_{i}" };
                    planIds.Add(plan.PlanId);

                    var ctx = CreateJobContext(plan.PlanId, "Full");
                    mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

                    mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                            It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                            It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                            It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                        .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                            async (conn, db, type, path, prog, ct) =>
                            {
                                await Task.Delay(30, ct);
                                return TestDataFactory.CreateSuccessResult(plan.PlanId, db);
                            });

                    mocks.Compression.Setup(c => c.CompressAsync(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                            It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1024L * 1024 * 30);

                    mocks.CloudOrchestrator.Setup(c => c.UploadToAllAsync(
                            It.IsAny<string>(), It.IsAny<string>(),
                            It.IsAny<List<CloudTargetConfig>>(),
                            It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                            It.IsAny<string>(), It.IsAny<string>()))
                        .Returns<string, string, List<CloudTargetConfig>, IProgress<int>, CancellationToken, string, string>(
                            async (file, remote, targets, prog, ct, name, id) =>
                            {
                                await Task.Delay(30, ct);
                                return TestDataFactory.CreateCloudUploadResults();
                            });

                    tasks.Add(executor.Execute(ctx.Object));
                }

                // Act
                await Task.WhenAll(tasks);

                // Assert
                completedPlans.Should().HaveCount(planCount);
                foreach (string planId in planIds)
                    completedPlans.Should().Contain(planId);
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Stress_LargeDatabaseList_ActivityEventsMonotonicProgress()
        {
            // Arrange — 10 DB'lik plan; DatabaseProgress event'leri monoton artmalı
            const int dbCount = 10;
            var executor = CreateExecutor(out var mocks);
            var plan = TestDataFactory.CreateValidPlan("MonotonicPlan");
            plan.Databases = Enumerable.Range(1, dbCount).Select(i => $"DB_{i:D2}").ToList();

            var progressEvents = new ConcurrentBag<int>();

            void handler(object s, BackupActivityEventArgs e)
            {
                if (e.PlanId == plan.PlanId && e.ActivityType == BackupActivityType.DatabaseProgress)
                    progressEvents.Add(e.CurrentIndex);
            }

            BackupActivityHub.ActivityChanged += handler;
            try
            {
                var ctx = CreateJobContext(plan.PlanId, "Full");
                mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

                mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                        It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                        It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(TestDataFactory.CreateSuccessResult(plan.PlanId));

                mocks.Compression.Setup(c => c.CompressAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1024L * 1024 * 30);

                // Act
                await executor.Execute(ctx.Object);

                // Assert — her DB için DatabaseProgress event gelmiş olmalı ve sıra monoton artmalı
                var sorted = progressEvents.OrderBy(x => x).ToList();
                sorted.Should().HaveCount(dbCount);
                for (int i = 0; i < sorted.Count; i++)
                    sorted[i].Should().Be(i + 1, $"DB index monoton artmalı: beklenen {i + 1}");
            }
            finally
            {
                BackupActivityHub.ActivityChanged -= handler;
            }
        }

        [TestMethod]
        public async Task Stress_CancellationDuringMultipleDbBackup_StopsGracefully()
        {
            // Arrange — 10 DB; 5. DB'de iptal; 5'ten fazla SQL çağrısı olmamalı
            const int dbCount = 10;
            const int cancelAtDb = 5;
            var executor = CreateExecutor(out var mocks);
            var plan = TestDataFactory.CreateValidPlan("CancelStressPlan");
            plan.Databases = Enumerable.Range(1, dbCount).Select(i => $"DB_{i:D2}").ToList();

            using var cts = new CancellationTokenSource();
            var ctx = CreateJobContext(plan.PlanId, "Full", cts.Token);
            mocks.PlanManager.Setup(p => p.GetPlanById(plan.PlanId)).Returns(plan);

            int callNumber = 0;
            mocks.SqlBackup.Setup(s => s.BackupDatabaseAsync(
                    It.IsAny<SqlConnectionInfo>(), It.IsAny<string>(),
                    It.IsAny<SqlBackupType>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<SqlConnectionInfo, string, SqlBackupType, string, IProgress<int>, CancellationToken>(
                    (conn, db, type, path, prog, ct) =>
                    {
                        int current = Interlocked.Increment(ref callNumber);
                        if (current >= cancelAtDb)
                            cts.Cancel();
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult(TestDataFactory.CreateSuccessResult(plan.PlanId, db));
                    });

            mocks.Compression.Setup(c => c.CompressAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024L * 1024 * 30);

            // Act
            await executor.Execute(ctx.Object);

            // Assert — cancelAtDb civarında durmuş olmalı; 10 çağrı yapılmamalı
            callNumber.Should().BeLessThanOrEqualTo(cancelAtDb + 1,
                $"iptal {cancelAtDb}. DB'de tetiklendi; en fazla {cancelAtDb + 1} çağrı olmalı");
        }

        // ── Helper Metotlar ─────────────────────────────────────────────────────

        private static BackupJobExecutor CreateExecutor(out MockSet mocks)
        {
            mocks = new MockSet();
            return new BackupJobExecutor
            {
                PlanManager = mocks.PlanManager.Object,
                SqlBackupService = mocks.SqlBackup.Object,
                CompressionService = mocks.Compression.Object,
                NotificationService = mocks.Notification.Object,
                RetentionService = mocks.Retention.Object,
                FileBackupService = mocks.FileBackup.Object,
                CloudOrchestrator = mocks.CloudOrchestrator.Object,
                HistoryManager = mocks.HistoryManager.Object,
                CancellationRegistry = mocks.CancellationRegistry.Object
            };
        }

        private static Mock<IJobExecutionContext> CreateJobContext(
            string planId, string backupType, CancellationToken ct = default)
        {
            var mock = new Mock<IJobExecutionContext>();
            var jobDataMap = new JobDataMap
            {
                { "planId", planId },
                { "backupType", backupType },
                { "manualTrigger", "false" }
            };
            mock.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
            mock.Setup(c => c.CancellationToken).Returns(ct);
            return mock;
        }

        /// <summary>
        /// Her test için bağımsız mock seti oluşturur.
        /// </summary>
        internal sealed class MockSet
        {
            public Mock<IPlanManager> PlanManager { get; } = new();
            public Mock<ISqlBackupService> SqlBackup { get; } = new();
            public Mock<ICompressionService> Compression { get; } = new();
            public Mock<INotificationService> Notification { get; } = new();
            public Mock<IRetentionService> Retention { get; } = new();
            public Mock<IFileBackupService> FileBackup { get; } = new();
            public Mock<ICloudUploadOrchestrator> CloudOrchestrator { get; } = new();
            public Mock<IBackupHistoryManager> HistoryManager { get; } = new();
            public Mock<IBackupCancellationRegistry> CancellationRegistry { get; } = new();
        }
    }
}
