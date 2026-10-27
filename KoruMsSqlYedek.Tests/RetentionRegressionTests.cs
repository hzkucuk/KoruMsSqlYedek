using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Backup;
using KoruMsSqlYedek.Engine.Cloud;
using KoruMsSqlYedek.Engine.Retention;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// Sahada "yedekler ne diskten ne buluttan silinmiyor" şikâyetinin ve ardından yapılan
    /// Alfa/Beta çapraz incelemesinin regresyon testleri.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class RetentionRegressionTests
    {
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "KoruRetentionRegression", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        // ── Yerel ──

        [TestMethod]
        public async Task Local_IncrementalBackups_AreCleaned()
        {
            // Log yedekleri "{db}_Incremental_*" adıyla yazılır; eskiden "{db}_Log_*" aranıyordu
            CreateLocal("DB_Full", Now.AddDays(-1));
            for (int i = 0; i < 5; i++)
                CreateLocal("DB_Incremental", Now.AddDays(-3).AddHours(i));

            var plan = NewPlan(KeepLast(2));
            await Service().CleanupAsync(plan, CancellationToken.None);

            Directory.GetFiles(_dir, "DB_Incremental_*").Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Local_UnrecordedFiles_AreClaimedByNamePattern()
        {
            // Geçmişi kaybolmuş (veri taşıma, eski sürüm) yedekler de plan kuralına göre silinir
            for (int i = 0; i < 4; i++)
                CreateLocal("DB_Full", Now.AddDays(-10 + i));

            await Service(history: new List<BackupResult>()).CleanupAsync(NewPlan(KeepLast(2)), CancellationToken.None);

            Directory.GetFiles(_dir, "DB_Full_*").Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Local_FileOfAnotherPlan_IsNeverDeleted()
        {
            string foreignFile = CreateLocal("DB_Full", Now.AddDays(-30));
            CreateLocal("DB_Full", Now.AddDays(-2));
            CreateLocal("DB_Full", Now.AddDays(-1));

            var foreign = new List<BackupResult> { new BackupResult { PlanId = "baska-plan", CompressedFilePath = foreignFile } };
            await Service(foreignHistory: foreign).CleanupAsync(NewPlan(KeepLast(1)), CancellationToken.None);

            File.Exists(foreignFile).Should().BeTrue("başka planın geçmişinde kayıtlı");
            Directory.GetFiles(_dir, "DB_Full_*").Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Local_NamePrefixOfAnotherDatabase_IsNotMatched()
        {
            // "Muhasebe" deseni "Muhasebe_Log" veritabanının yedeklerini yakalamamalı
            string otherDb = CreateLocal("Muhasebe_Log_Full", Now.AddDays(-30));
            CreateLocal("Muhasebe_Full", Now.AddDays(-1));

            var plan = NewPlan(KeepLast(1));
            plan.Databases = new List<string> { "Muhasebe" };
            await Service().CleanupAsync(plan, CancellationToken.None);

            File.Exists(otherDb).Should().BeTrue();
        }

        [TestMethod]
        public async Task Local_AllOlderThanCutoff_NewestIsKept()
        {
            // Yedekleme uzun süre durduysa yaş kuralı grubun tamamını silmemeli
            CreateLocal("DB_Full", Now.AddDays(-200));
            string newest = CreateLocal("DB_Full", Now.AddDays(-100));

            var policy = new RetentionPolicy { Type = RetentionPolicyType.DeleteOlderThanDays, DeleteOlderThanDays = 30 };
            await Service().CleanupAsync(NewPlan(policy), CancellationToken.None);

            Directory.GetFiles(_dir, "DB_Full_*").Should().ContainSingle().Which.Should().Be(newest);
        }

        [TestMethod]
        public async Task Local_FutureDatedFile_IsNotCountedNorDeleted()
        {
            // Sunucu saati bir ara 2099'a kaymış: o dosya "en yeni" sayılıp gerçek yedekleri sildirmemeli
            string future = CreateLocal("DB_Full", new DateTime(2099, 12, 31));
            string yesterday = CreateLocal("DB_Full", Now.AddDays(-1));
            string older = CreateLocal("DB_Full", Now.AddDays(-2));

            await Service().CleanupAsync(NewPlan(KeepLast(1)), CancellationToken.None);

            File.Exists(future).Should().BeTrue();
            File.Exists(yesterday).Should().BeTrue();
            File.Exists(older).Should().BeFalse();
        }

        [TestMethod]
        public async Task Local_LogsAfterNewestFull_AreNeverDeleted()
        {
            // GFS (günde tek yedek) loglara uygulanırsa son Full'den sonraki zincir koparılırdı
            CreateLocal("DB_Full", Now.AddDays(-1).Date.AddHours(2));
            var logsAfterFull = Enumerable.Range(0, 6)
                .Select(i => CreateLocal("DB_Incremental", Now.AddDays(-1).Date.AddHours(3 + i)))
                .ToList();

            var gfs = new RetentionPolicy { Type = RetentionPolicyType.GFS, GfsKeepDaily = 30 };
            await Service().CleanupAsync(NewPlan(gfs), CancellationToken.None);

            logsAfterFull.Should().OnlyContain(f => File.Exists(f));
        }

        [TestMethod]
        public async Task Local_LogsWithoutAnyFull_AreNeverDeleted()
        {
            for (int i = 0; i < 5; i++)
                CreateLocal("DB_Incremental", Now.AddDays(-20 + i));

            await Service().CleanupAsync(NewPlan(KeepLast(1)), CancellationToken.None);

            Directory.GetFiles(_dir, "DB_Incremental_*").Should().HaveCount(5);
        }

        [TestMethod]
        public async Task Local_VssWithEmptyUploadResults_IsProtected()
        {
            // Yükleme istisna fırlattı: VSS sonuç listesi boş kaldı → buluta gitmedi, yerelde korunmalı
            var target = DriveTarget("KoruYedek");
            var plan = NewPlan(KeepLast(1), target);

            var history = new List<BackupResult>();
            for (int i = 0; i < 2; i++)
            {
                history.Add(new BackupResult
                {
                    PlanId = plan.PlanId,
                    Status = BackupResultStatus.Success,
                    CompressedFilePath = CreateLocal("DB_Full", Now.AddDays(-5 + i), ".7z"),
                    VssFileCopyPath = CreateLocal("DB_VSS", Now.AddDays(-5 + i), ".7z"),
                    CloudUploadResults = { Upload("x") },
                    VssCloudUploadResults = new List<CloudUploadResult>()
                });
            }

            await Service(history, cloud: NewCloud()).CleanupAsync(plan, CancellationToken.None);

            Directory.GetFiles(_dir, "DB_VSS_*").Should().HaveCount(2);
            Directory.GetFiles(_dir, "DB_Full_*").Should().HaveCount(1);
        }

        [TestMethod]
        public async Task Local_FailedVerifyFull_DoesNotDisplaceVerifiedFull()
        {
            // Bugünkü Full'ün doğrulaması başarısız, .bak diskte kaldı: dünkü doğrulanmış Full silinmemeli
            string verified = CreateLocal("DB_Full", Now.AddDays(-1), ".7z");
            string corrupt = CreateLocal("DB_Full", Now);

            var plan = NewPlan(KeepLast(1));
            var history = new List<BackupResult>
            {
                new BackupResult { PlanId = plan.PlanId, DatabaseName = "DB", Status = BackupResultStatus.Success, CompressedFilePath = verified },
                new BackupResult { PlanId = plan.PlanId, DatabaseName = "DB", Status = BackupResultStatus.Failed, BackupFilePath = corrupt }
            };

            await Service(history).CleanupAsync(plan, CancellationToken.None);

            File.Exists(verified).Should().BeTrue();
            File.Exists(corrupt).Should().BeTrue("daha yeni geçerli yedek yok, henüz silinmez");
        }

        [TestMethod]
        public async Task Local_FailedBackupFile_IsDeletedOnceSuperseded()
        {
            string corrupt = CreateLocal("DB_Full", Now.AddDays(-2));
            string verified = CreateLocal("DB_Full", Now.AddDays(-1), ".7z");

            var plan = NewPlan(KeepLast(5));
            var history = new List<BackupResult>
            {
                new BackupResult { PlanId = plan.PlanId, DatabaseName = "DB", Status = BackupResultStatus.Failed, BackupFilePath = corrupt },
                new BackupResult { PlanId = plan.PlanId, DatabaseName = "DB", Status = BackupResultStatus.Success, CompressedFilePath = verified }
            };

            await Service(history).CleanupAsync(plan, CancellationToken.None);

            File.Exists(corrupt).Should().BeFalse();
            File.Exists(verified).Should().BeTrue();
        }

        [TestMethod]
        public async Task Local_PartialFileBackupArchiveNotUploaded_IsProtected()
        {
            // Kısmi başarılı dosya yedeği "Failed" kaydedilir; buluta gitmemiş arşivi tek kopyadır
            var plan = NewPlan(KeepLast(1), DriveTarget("KoruYedek"));
            plan.FileBackup = new FileBackupConfig { IsEnabled = true };

            string notUploaded = CreateLocal("Files", Now.AddDays(-2), ".7z");
            CreateLocal("Files", Now.AddDays(-1), ".7z");
            var history = new List<BackupResult>
            {
                new BackupResult
                {
                    PlanId = plan.PlanId, DatabaseName = "__FileBackup__", Status = BackupResultStatus.Failed,
                    CompressedFilePath = notUploaded,
                    CloudUploadResults = { new CloudUploadResult { IsSuccess = false } }
                }
            };

            await Service(history, cloud: NewCloud()).CleanupAsync(plan, CancellationToken.None);

            File.Exists(notUploaded).Should().BeTrue();
        }

        [TestMethod]
        public async Task Local_ForeignFileViaDifferentPathSpelling_IsSkipped()
        {
            // Diğer plan aynı klasöre \\srv\Yedek, bu plan Z:\Yedek diye erişiyor
            string shared = CreateLocal("DB_Full", Now.AddDays(-10));
            CreateLocal("DB_Full", Now.AddDays(-1));

            var foreign = new List<BackupResult>
            {
                new BackupResult { PlanId = "diger", CompressedFilePath = @"\\srv\Yedek\" + Path.GetFileName(shared) }
            };
            await Service(foreignHistory: foreign).CleanupAsync(NewPlan(KeepLast(1)), CancellationToken.None);

            File.Exists(shared).Should().BeTrue();
        }

        [TestMethod]
        public async Task Local_HistoryOfDeletedPlan_IsNotForeign()
        {
            // Plan silinip aynı klasörle yeniden kuruldu (yeni PlanId): eski yedekler temizlenebilmeli
            var plan = NewPlan(KeepLast(1));
            string oldPlansFile = CreateLocal("DB_Full", Now.AddDays(-10));
            CreateLocal("DB_Full", Now.AddDays(-1));

            var planManager = new Mock<IPlanManager>();
            planManager.Setup(p => p.GetAllPlans()).Returns(new List<BackupPlan> { plan });
            var foreign = new List<BackupResult> { new BackupResult { PlanId = "silinmis-plan", CompressedFilePath = oldPlansFile } };

            await Service(foreignHistory: foreign, planManager: planManager).CleanupAsync(plan, CancellationToken.None);

            File.Exists(oldPlansFile).Should().BeFalse();
        }

        // ── Bulut ──

        [TestMethod]
        public async Task Cloud_UnrecordedRemoteFiles_AreCleanedByPolicy()
        {
            // Geçmişte kaydı olmayan (eski sürüm, hedef adı uyuşmazlığı) bulut yedekleri de silinir
            var remote = Enumerable.Range(0, 4)
                .Select(i => Remote($"id{i}", "DB_Full", Now.AddDays(-10 + i)))
                .ToList();
            var cloud = NewCloud(remote);

            await Service(cloud: cloud).CleanupAsync(NewPlan(KeepLast(2), DriveTarget("KoruYedek")), CancellationToken.None);

            VerifyDeleted(cloud, "id0");
            VerifyDeleted(cloud, "id1");
            VerifyNotDeleted(cloud, "id2");
            VerifyNotDeleted(cloud, "id3");
        }

        [TestMethod]
        public async Task Cloud_LocalFilesMissing_DoesNotDeleteCloudCopies()
        {
            // Yedek diski değişti / klasör taşındı: yerel dosyaların yokluğu bulut kopyasını sildirmemeli
            var plan = NewPlan(KeepLast(5), DriveTarget("KoruYedek"));
            plan.LocalPath = Path.Combine(_dir, "artik-yok");

            var history = Enumerable.Range(0, 3).Select(i => new BackupResult
            {
                PlanId = plan.PlanId,
                Status = BackupResultStatus.Success,
                CompressedFilePath = Path.Combine(plan.LocalPath, $"DB_Full_{Ts(Now.AddDays(-3 + i))}.7z"),
                CloudUploadResults = { Upload($"id{i}") }
            }).ToList();
            var remote = Enumerable.Range(0, 3).Select(i => Remote($"id{i}", "DB_Full", Now.AddDays(-3 + i))).ToList();
            var cloud = NewCloud(remote);

            await Service(history, cloud: cloud).CleanupAsync(plan, CancellationToken.None);

            cloud.Verify(c => c.DeleteFromTargetAsync(It.IsAny<string>(), It.IsAny<CloudTargetConfig>(),
                It.IsAny<CancellationToken>()), Times.Never);
            cloud.Verify(c => c.DeleteFromAllAsync(It.IsAny<string>(), It.IsAny<List<CloudTargetConfig>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Cloud_FileOfAnotherPlan_IsNeverDeleted()
        {
            var remote = new List<CloudFileEntry>
            {
                Remote("foreign", "DB_Full", Now.AddDays(-30)),
                Remote("mine1", "DB_Full", Now.AddDays(-2)),
                Remote("mine2", "DB_Full", Now.AddDays(-1))
            };
            var foreign = new List<BackupResult>
            {
                new BackupResult { PlanId = "baska-plan", CloudUploadResults = { Upload("foreign") } }
            };
            var cloud = NewCloud(remote);

            await Service(foreignHistory: foreign, cloud: cloud)
                .CleanupAsync(NewPlan(KeepLast(1), DriveTarget("Ortak")), CancellationToken.None);

            VerifyNotDeleted(cloud, "foreign");
            VerifyDeleted(cloud, "mine1");
        }

        [TestMethod]
        public async Task Cloud_SameNameCopies_CountAsOneBackup()
        {
            // Aynı adlı iki kopya (aynı saniyede iki kurulum) KeepLastN sayımını yarıya indirmemeli
            var remote = new List<CloudFileEntry>
            {
                Remote("a-old", "DB_Full", Now.AddDays(-3)),
                Remote("a-mid", "DB_Full", Now.AddDays(-2)),
                Remote("b-mid", "DB_Full", Now.AddDays(-2)),
                Remote("a-new", "DB_Full", Now.AddDays(-1))
            };
            var cloud = NewCloud(remote);

            await Service(cloud: cloud).CleanupAsync(NewPlan(KeepLast(2), DriveTarget("KoruYedek")), CancellationToken.None);

            VerifyDeleted(cloud, "a-old");
            VerifyNotDeleted(cloud, "a-mid");
            VerifyNotDeleted(cloud, "b-mid");
        }

        [TestMethod]
        public async Task Cloud_EmptyRemoteFolder_ListsAndDeletesInDefaultUploadFolder()
        {
            var plan = NewPlan(KeepLast(1), DriveTarget(""));
            plan.PlanName = "Muhasebe";
            var cloud = NewCloud(new List<CloudFileEntry>
            {
                Remote("old", "DB_Full", Now.AddDays(-2)),
                Remote("new", "DB_Full", Now.AddDays(-1))
            });

            await Service(cloud: cloud).CleanupAsync(plan, CancellationToken.None);

            cloud.Verify(c => c.ListFolderAsync(
                It.Is<CloudTargetConfig>(t => t.RemoteFolderPath == "KoruMsSqlYedek/Muhasebe"),
                It.IsAny<CancellationToken>()), Times.Once);
            cloud.Verify(c => c.DeleteFromTargetAsync("old",
                It.Is<CloudTargetConfig>(t => t.RemoteFolderPath == "KoruMsSqlYedek/Muhasebe"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Toplu temizlik emniyeti ──

        [TestMethod]
        public async Task Local_ManyOldBackups_DeletedGraduallyOldestFirst()
        {
            var files = Enumerable.Range(0, 30).Select(i => CreateLocal("DB_Full", Now.AddDays(-40 + i))).ToList();

            await Service().CleanupAsync(NewPlan(KeepLast(1)), CancellationToken.None);

            // 29 aday var; bu çalıştırmada en eski 20'si silinir
            files.Take(RetentionCleanupService.MaxDeletionsPerRun).Should().OnlyContain(f => !File.Exists(f));
            files.Skip(RetentionCleanupService.MaxDeletionsPerRun).Should().OnlyContain(f => File.Exists(f));
        }

        [TestMethod]
        public async Task Cloud_ManyOldBackups_DeletedGradually()
        {
            var remote = Enumerable.Range(0, 30).Select(i => Remote($"id{i}", "DB_Full", Now.AddDays(-40 + i))).ToList();
            var cloud = NewCloud(remote);

            await Service(cloud: cloud).CleanupAsync(NewPlan(KeepLast(1), DriveTarget("KoruYedek")), CancellationToken.None);

            cloud.Verify(c => c.DeleteFromTargetAsync(It.IsAny<string>(), It.IsAny<CloudTargetConfig>(),
                It.IsAny<CancellationToken>()), Times.Exactly(RetentionCleanupService.MaxDeletionsPerRun));
            VerifyDeleted(cloud, "id0");
            VerifyNotDeleted(cloud, "id25");
        }

        [TestMethod]
        public async Task Cloud_SafeMode_DriveDeletesGoToTrashEvenWhenTrashDisabled()
        {
            var cloud = NewCloud(new List<CloudFileEntry>
            {
                Remote("old", "DB_Full", Now.AddDays(-2)),
                Remote("new", "DB_Full", Now.AddDays(-1))
            });
            var target = DriveTarget("KoruYedek");
            target.TrashRetentionDays = 0;

            await Service(cloud: cloud).CleanupAsync(NewPlan(KeepLast(1), target), CancellationToken.None);

            cloud.Verify(c => c.DeleteFromTargetAsync("old",
                It.Is<CloudTargetConfig>(t => t.UsesTrash), It.IsAny<CancellationToken>()), Times.Once);
            target.TrashRetentionDays.Should().Be(0, "plandaki hedef ayarı değişmemeli");
        }

        [TestMethod]
        public async Task Cloud_AfterSafeMode_DriveDeletesFollowTargetSetting()
        {
            var cloud = NewCloud(new List<CloudFileEntry>
            {
                Remote("old", "DB_Full", Now.AddDays(-2)),
                Remote("new", "DB_Full", Now.AddDays(-1))
            });
            var service = Service(cloud: cloud);
            Directory.CreateDirectory(Path.GetDirectoryName(service.SafeModeMarkerPath));
            File.WriteAllText(service.SafeModeMarkerPath, DateTime.UtcNow.AddDays(-20).ToString("o"));

            await service.CleanupAsync(NewPlan(KeepLast(1), DriveTarget("KoruYedek")), CancellationToken.None);

            cloud.Verify(c => c.DeleteFromTargetAsync("old",
                It.Is<CloudTargetConfig>(t => !t.UsesTrash), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void UncAndFtpProviders_SupportFolderListing()
        {
            // Bulut retention yalnızca hedefin kendi listesinden yapılır; tüm sağlayıcılar listelemeli
            new LocalNetworkProvider(CloudProviderType.UncPath).Should().BeAssignableTo<ICloudFolderListProvider>();
            new FtpSftpProvider(CloudProviderType.Ftp).Should().BeAssignableTo<ICloudFolderListProvider>();
            new FtpSftpProvider(CloudProviderType.Sftp).Should().BeAssignableTo<ICloudFolderListProvider>();
        }

        [TestMethod]
        public async Task Unc_ListFolder_ReturnsBackupFilesWithFullPath()
        {
            string share = Path.Combine(_dir, "share");
            Directory.CreateDirectory(share);
            File.WriteAllText(Path.Combine(share, "DB_Full_20260101_020000.7z"), "x");
            File.WriteAllText(Path.Combine(share, "notlar.txt"), "x");

            var entries = await new LocalNetworkProvider(CloudProviderType.UncPath).ListFolderAsync(
                new CloudTargetConfig { Type = CloudProviderType.UncPath, LocalOrUncPath = share },
                CancellationToken.None);

            entries.Should().ContainSingle();
            entries[0].FileId.Should().Be(Path.Combine(share, "DB_Full_20260101_020000.7z"));
        }

        // ── Yükleme / zincir ──

        [TestMethod]
        public async Task Upload_ResultCarriesTargetDisplayName()
        {
            // Google Drive sağlayıcısı sonuca kendi sabit adını yazar
            var provider = new Mock<ICloudProvider>();
            provider.Setup(p => p.ProviderType).Returns(CloudProviderType.GoogleDrivePersonal);
            provider.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<Action<string>>()))
                .ReturnsAsync(new CloudUploadResult
                {
                    ProviderType = CloudProviderType.GoogleDrivePersonal,
                    DisplayName = "Google Drive",
                    IsSuccess = true,
                    RemoteFilePath = "fileId"
                });

            var orchestrator = new CloudUploadOrchestrator(new[] { provider.Object });
            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.GoogleDrivePersonal, IsEnabled = true, DisplayName = "Drive testi" }
            };

            var results = await orchestrator.UploadToAllAsync(
                @"C:\yok.7z", "yok.7z", targets, null, CancellationToken.None);

            results.Should().ContainSingle().Which.DisplayName.Should().Be("Drive testi");
        }

        [TestMethod]
        public void ChainValidator_CompressedFullOnly_CountsAsValidFull()
        {
            // Sıkıştırma sonrası .bak silinir, diskte yalnızca .7z kalır
            CreateLocal("DB_Full", Now.AddDays(-1), ".7z");

            var validator = new BackupChainValidator();

            validator.HasValidFullBackup(_dir, "DB").Should().BeTrue();
            validator.ShouldPromoteToFull(_dir, "DB", autoPromoteAfter: 7).Should().BeFalse();
        }

        [TestMethod]
        public void ChainValidator_BakAnd7zOfSameBackup_CountedOnce()
        {
            CreateLocal("DB_Full", Now.AddDays(-3), ".7z");
            DateTime diffAt = Now.AddDays(-1);
            CreateLocal("DB_Differential", diffAt);
            CreateLocal("DB_Differential", diffAt, ".7z");

            new BackupChainValidator().GetDifferentialCountSinceLastFull(_dir, "DB").Should().Be(1);
        }

        // ── Yardımcılar ──

        private static DateTime Now => DateTime.Now;

        private static string Ts(DateTime when) => RetentionCleanupServiceTests.Ts(when);

        private static RetentionPolicy KeepLast(int n) => new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = n };

        private static CloudTargetConfig DriveTarget(string folder) => new CloudTargetConfig
        {
            Type = CloudProviderType.GoogleDrivePersonal, IsEnabled = true, DisplayName = "Drive", RemoteFolderPath = folder
        };

        private BackupPlan NewPlan(RetentionPolicy retention, params CloudTargetConfig[] targets) => new BackupPlan
        {
            PlanName = "Test",
            Databases = { "DB" },
            LocalPath = _dir,
            Retention = retention,
            CloudTargets = targets.ToList()
        };

        private static RetentionCleanupService Service(
            List<BackupResult> history = null,
            List<BackupResult> foreignHistory = null,
            Mock<ICloudUploadOrchestrator> cloud = null,
            Mock<IPlanManager> planManager = null)
        {
            var mock = new Mock<IBackupHistoryManager>();
            mock.Setup(h => h.GetRecentHistory(It.IsAny<int>()))
                .Returns((history ?? new List<BackupResult>()).Concat(foreignHistory ?? new List<BackupResult>()).ToList());
            return new RetentionCleanupService(mock.Object, cloud?.Object, planManager?.Object)
            {
                SafeModeMarkerPath = Path.Combine(Path.GetTempPath(), "KoruRetentionRegression", Guid.NewGuid().ToString("N") + ".txt")
            };
        }

        private static Mock<ICloudUploadOrchestrator> NewCloud(List<CloudFileEntry> remote = null)
        {
            var cloud = new Mock<ICloudUploadOrchestrator>();
            cloud.Setup(c => c.ListFolderAsync(It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(remote ?? new List<CloudFileEntry>());
            cloud.Setup(c => c.DeleteFromTargetAsync(It.IsAny<string>(), It.IsAny<CloudTargetConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            return cloud;
        }

        private static CloudFileEntry Remote(string fileId, string prefix, DateTime when)
            => new CloudFileEntry { FileId = fileId, Name = $"{prefix}_{Ts(when)}.7z", CreatedAtUtc = when.ToUniversalTime(), SizeBytes = 1 };

        private static CloudUploadResult Upload(string fileId) => new CloudUploadResult
        {
            ProviderType = CloudProviderType.GoogleDrivePersonal, DisplayName = "Drive", IsSuccess = true, RemoteFilePath = fileId
        };

        private static void VerifyDeleted(Mock<ICloudUploadOrchestrator> cloud, string fileId)
            => cloud.Verify(c => c.DeleteFromTargetAsync(fileId, It.IsAny<CloudTargetConfig>(),
                It.IsAny<CancellationToken>()), Times.Once, $"{fileId} buluttan silinmeliydi");

        private static void VerifyNotDeleted(Mock<ICloudUploadOrchestrator> cloud, string fileId)
            => cloud.Verify(c => c.DeleteFromTargetAsync(fileId, It.IsAny<CloudTargetConfig>(),
                It.IsAny<CancellationToken>()), Times.Never, $"{fileId} korunmalıydı");

        private string CreateLocal(string prefix, DateTime when, string extension = ".bak")
        {
            string path = Path.Combine(_dir, $"{prefix}_{Ts(when)}{extension}");
            File.WriteAllText(path, "x");
            File.SetCreationTime(path, when);
            return path;
        }
    }
}
