using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine;
using KoruMsSqlYedek.Engine.Cloud;
using KoruMsSqlYedek.Engine.Retention;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// Özellik kombinasyonu entegrasyon testleri.
    /// Password × Cloud × Retention × FileBackup × SchemaVersion × PlanManager cross-feature etkileşimleri.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class PlanPasswordIntegrationTests
    {
        private PlanManager _planManager;

        [TestInitialize]
        public void Setup()
        {
            _planManager = new PlanManager();
        }

        // ═══════════════════════════════════════════════════════
        // 1. PlanManager Save/Load — Password Persistence
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void SaveAndLoad_PlanWithPassword_PasswordHashPreserved()
        {
            var plan = TestDataFactory.CreateValidPlan("PW_SaveLoad");
            plan.PasswordHash = PlanPasswordHelper.HashPassword("saveLoadTest");

            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                loaded.Should().NotBeNull();
                loaded.HasPlanPassword.Should().BeTrue();
                loaded.PasswordHash.Should().Be(plan.PasswordHash);
                PlanPasswordHelper.VerifyPassword("saveLoadTest", loaded.PasswordHash).Should().BeTrue();
                PlanPasswordHelper.VerifyPassword("wrongPassword", loaded.PasswordHash).Should().BeFalse();
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        [TestMethod]
        public void SaveAndLoad_PlanWithoutPassword_PasswordHashNull()
        {
            var plan = TestDataFactory.CreateValidPlan("PW_NoPassword");
            plan.PasswordHash = null;

            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                loaded.HasPlanPassword.Should().BeFalse();
                loaded.PasswordHash.Should().BeNull();
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        [TestMethod]
        public void SaveAndLoad_PlanWithPassword_ThenRemovePassword()
        {
            var plan = TestDataFactory.CreateValidPlan("PW_SetThenRemove");
            plan.PasswordHash = PlanPasswordHelper.HashPassword("tempPassword");
            _planManager.SavePlan(plan);

            // Şifre kaldır
            var loaded = _planManager.GetPlanById(plan.PlanId);
            loaded.PasswordHash = null;
            _planManager.SavePlan(loaded);

            var reloaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                reloaded.HasPlanPassword.Should().BeFalse();
                reloaded.PasswordHash.Should().BeNull();
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        [TestMethod]
        public void SaveAndLoad_PlanWithPassword_ThenChangePassword()
        {
            var plan = TestDataFactory.CreateValidPlan("PW_Change");
            plan.PasswordHash = PlanPasswordHelper.HashPassword("oldPassword");
            _planManager.SavePlan(plan);

            // Şifre değiştir
            var loaded = _planManager.GetPlanById(plan.PlanId);
            loaded.PasswordHash = PlanPasswordHelper.HashPassword("newPassword");
            _planManager.SavePlan(loaded);

            var reloaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                PlanPasswordHelper.VerifyPassword("oldPassword", reloaded.PasswordHash).Should().BeFalse();
                PlanPasswordHelper.VerifyPassword("newPassword", reloaded.PasswordHash).Should().BeTrue();
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 2. Password + Cloud + FileBackup — Tam Plan Save/Load
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void SaveAndLoad_FullPlan_AllFeaturesCombined()
        {
            var plan = TestDataFactory.CreateValidPlan("FullCombo");
            plan.PasswordHash = PlanPasswordHelper.HashPassword("comboSecret");
            plan.CloudTargets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig
                {
                    Type = CloudProviderType.Ftp,
                    IsEnabled = true,
                    DisplayName = "Test FTP",
                    Host = "ftp.test.com",
                    Port = 21,
                    Username = "user",
                    RemoteFolderPath = "/backups"
                },
                new CloudTargetConfig
                {
                    Type = CloudProviderType.UncPath,
                    IsEnabled = true,
                    DisplayName = "UNC Share",
                    LocalOrUncPath = @"\\server\backups"
                },
                new CloudTargetConfig
                {
                    Type = CloudProviderType.Ftp,
                    IsEnabled = false,
                    DisplayName = "FTP (Disabled)"
                }
            };
            plan.FileBackup = new FileBackupConfig
            {
                IsEnabled = true,
                Schedule = "0 0 3 ? * MON-FRI",
                Sources = new List<FileBackupSource>
                {
                    new FileBackupSource
                    {
                        SourceName = "PST Files",
                        SourcePath = @"C:\Test\Outlook",
                        UseVss = true,
                        IsEnabled = true,
                        Recursive = true,
                        IncludePatterns = new List<string> { "*.pst" }
                    }
                }
            };
            plan.Compression = new CompressionConfig
            {
                Algorithm = CompressionAlgorithm.Lzma2,
                Level = CompressionLevel.Maximum,
                ArchivePassword = PasswordProtector.Protect("archivePass")
            };
            plan.Retention = new RetentionPolicy
            {
                Type = RetentionPolicyType.GFS,
                GfsKeepDaily = 14,
                GfsKeepWeekly = 8,
                GfsKeepMonthly = 12,
                GfsKeepYearly = 3
            };

            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                // Password
                loaded.HasPlanPassword.Should().BeTrue();
                PlanPasswordHelper.VerifyPassword("comboSecret", loaded.PasswordHash).Should().BeTrue();

                // Cloud — 2 aktif, 1 pasif
                loaded.HasCloudTargets.Should().BeTrue();
                loaded.CloudTargets.Should().HaveCount(3);
                loaded.CloudTargets.Count(c => c.IsEnabled).Should().Be(2);
                loaded.CloudTargets[0].Type.Should().Be(CloudProviderType.Ftp);
                loaded.CloudTargets[0].Host.Should().Be("ftp.test.com");
                loaded.CloudTargets[2].IsEnabled.Should().BeFalse();

                // File Backup
                loaded.FileBackup.Should().NotBeNull();
                loaded.FileBackup.IsEnabled.Should().BeTrue();
                loaded.FileBackup.Sources.Should().HaveCount(1);
                loaded.FileBackup.Sources[0].UseVss.Should().BeTrue();

                // Compression — archive password ve plan password bağımsız
                loaded.Compression.Algorithm.Should().Be(CompressionAlgorithm.Lzma2);
                loaded.Compression.Level.Should().Be(CompressionLevel.Maximum);
                PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archivePass");

                // Retention GFS
                loaded.Retention.Type.Should().Be(RetentionPolicyType.GFS);
                loaded.Retention.GfsKeepDaily.Should().Be(14);
                loaded.Retention.GfsKeepMonthly.Should().Be(12);
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 3. Schema Migration — PasswordHash eski planlarda null
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void SchemaMigration_V1PlanWithoutPasswordField_LoadsWithNullPassword()
        {
            // v0.73.0 öncesi JSON — passwordHash alanı yok
            string planId = Guid.NewGuid().ToString();
            var v1Json = new JObject
            {
                ["planId"] = planId,
                ["planName"] = "V1 No Password",
                ["isEnabled"] = true,
                ["schemaVersion"] = 1,
                ["sqlConnection"] = new JObject
                {
                    ["server"] = "localhost",
                    ["authMode"] = 0
                },
                ["databases"] = new JArray("TestDB"),
                ["strategy"] = new JObject
                {
                    ["type"] = 0,
                    ["fullSchedule"] = "0 0 2 ? * SUN",
                    ["autoPromoteToFullAfter"] = 7
                },
                ["localPath"] = @"C:\TestBackups"
            };

            string filePath = PathHelper.GetPlanFilePath(planId);
            PathHelper.EnsureDirectoriesExist();
            File.WriteAllText(filePath, v1Json.ToString());

            var loaded = _planManager.GetPlanById(planId);

            try
            {
                loaded.Should().NotBeNull();
                loaded.HasPlanPassword.Should().BeFalse();
                loaded.PasswordHash.Should().BeNull();
                loaded.PlanName.Should().Be("V1 No Password");
            }
            finally
            {
                _planManager.DeletePlan(planId);
            }
        }

        [TestMethod]
        public void SchemaMigration_V1Plan_ThenAddPassword_Persists()
        {
            // v1 plan yükle → şifre ekle → kaydet → tekrar yükle
            string planId = Guid.NewGuid().ToString();
            var v1Json = new JObject
            {
                ["planId"] = planId,
                ["planName"] = "V1 AddPw Later",
                ["isEnabled"] = true,
                ["schemaVersion"] = 1,
                ["sqlConnection"] = new JObject { ["server"] = "localhost", ["authMode"] = 0 },
                ["databases"] = new JArray("DB1"),
                ["localPath"] = @"C:\Backups"
            };

            string filePath = PathHelper.GetPlanFilePath(planId);
            PathHelper.EnsureDirectoriesExist();
            File.WriteAllText(filePath, v1Json.ToString());

            // Yükle
            var loaded = _planManager.GetPlanById(planId);
            loaded.HasPlanPassword.Should().BeFalse();

            // Şifre ekle ve kaydet
            loaded.PasswordHash = PlanPasswordHelper.HashPassword("newlyAdded");
            _planManager.SavePlan(loaded);

            // Tekrar yükle
            var reloaded = _planManager.GetPlanById(planId);

            try
            {
                reloaded.HasPlanPassword.Should().BeTrue();
                PlanPasswordHelper.VerifyPassword("newlyAdded", reloaded.PasswordHash).Should().BeTrue();
                reloaded.SchemaVersion.Should().Be(2); // v1→v2 migration da olmuş olmalı
            }
            finally
            {
                _planManager.DeletePlan(planId);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 4. Retention × HasCloudTargets — v0.73.0 refactor
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public async Task Retention_PlanWithCloudTargets_HasCloudTargetsTrue()
        {
            // HasCloudTargets computed property ile RetentionCleanupService'in
            // doğru davranışı kontrol edilir
            var plan = TestDataFactory.CreatePlanWithCloudTargets();
            plan.HasCloudTargets.Should().BeTrue();

            // Eski Mode property ile tutarlılık (geriye uyumluluk güvencesi)
#pragma warning disable CS0618
            // Mode artık bağımsız — HasCloudTargets gerçek kaynağı kontrol eder
            plan.Mode = BackupMode.Local; // Eski alan Local olsa bile...
#pragma warning restore CS0618
            plan.HasCloudTargets.Should().BeTrue("CloudTargets listesinde aktif hedef var");
        }

        [TestMethod]
        public async Task Retention_PlanWithPassword_DoesNotAffectCleanup()
        {
            // Plan şifresi retention temizliğini etkilememeli
            var testDir = Path.Combine(Path.GetTempPath(), "KoruRetention_PwTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);

            try
            {
                // 5 dosya oluştur
                for (int i = 0; i < 5; i++)
                {
                    string file = Path.Combine(testDir, $"TestDB_Full_2025010{i + 1}_020000.bak");
                    File.WriteAllText(file, "test");
                    File.SetCreationTime(file, DateTime.Now.AddDays(-10 + i));
                }

                var plan = new BackupPlan
                {
                    PasswordHash = PlanPasswordHelper.HashPassword("retentionTest"),
                    Databases = { "TestDB" },
                    LocalPath = testDir,
                    Retention = new RetentionPolicy
                    {
                        Type = RetentionPolicyType.KeepLastN,
                        KeepLastN = 3
                    }
                };

                plan.HasPlanPassword.Should().BeTrue("plan şifresi var");

                var mockHistory = new Mock<IBackupHistoryManager>();
                var service = new RetentionCleanupService(mockHistory.Object);
                await service.CleanupAsync(plan, CancellationToken.None);

                var remaining = Directory.GetFiles(testDir, "TestDB_Full_*.bak");
                remaining.Should().HaveCount(3, "şifre retention davranışını etkilememeli");
            }
            finally
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 5. CloudUploadOrchestrator × Provider Types — Mock
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public async Task CloudUpload_AllProviderTypes_EnabledDisabledMatrix()
        {
            // Her provider tipi için mock oluştur
            var mockFtp = CreateMockProvider(CloudProviderType.Ftp, true);
            var mockSftp = CreateMockProvider(CloudProviderType.Sftp, true);
            var mockUnc = CreateMockProvider(CloudProviderType.UncPath, true);

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtp.Object, mockSftp.Object, mockUnc.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" },
                new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = false, DisplayName = "SFTP (disabled)" },
                new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = true, DisplayName = "UNC" }
            };

            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            // 2 aktif hedef → 2 sonuç
            results.Should().HaveCount(2);
            results.Should().OnlyContain(r => r.IsSuccess);

            // Devre dışı SFTP çağrılmamalı
            mockSftp.Verify(p => p.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CloudTargetConfig>(),
                It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Never);
        }

        [TestMethod]
        public async Task CloudUpload_ProviderFails_OthersStillSucceed()
        {
            var mockFtpFail = CreateMockProvider(CloudProviderType.Ftp, false, "FTP bağlantı hatası");
            var mockUncSuccess = CreateMockProvider(CloudProviderType.UncPath, true);

            var orchestrator = new CloudUploadOrchestrator(
                new ICloudProvider[] { mockFtpFail.Object, mockUncSuccess.Object });

            var targets = new List<CloudTargetConfig>
            {
                new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "Failing FTP" },
                new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = true, DisplayName = "Good UNC" }
            };

            var results = await orchestrator.UploadToAllAsync(
                @"C:\test.7z", "test.7z", targets, null, CancellationToken.None);

            results.Should().HaveCount(2);
            results.First(r => r.ProviderType == CloudProviderType.Ftp).IsSuccess.Should().BeFalse();
            results.First(r => r.ProviderType == CloudProviderType.UncPath).IsSuccess.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════
        // 6. FTP/SFTP Provider — Validation Edge Cases
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(4, DisplayName = "FTP_NullConfig")]   // CloudProviderType.Ftp = 4
        [DataRow(6, DisplayName = "SFTP_NullConfig")]  // CloudProviderType.Sftp = 6
        [DataRow(5, DisplayName = "FTPS_NullConfig")]  // CloudProviderType.Ftps = 5
        public async Task FtpSftp_NullConfig_ThrowsNullReference(int typeInt)
        {
            var type = (CloudProviderType)typeInt;
            var provider = new FtpSftpProvider(type);

            Func<Task> act = () => provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", null, null, CancellationToken.None);

            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [TestMethod]
        [DataRow(null, DisplayName = "FTP_HostNull")]
        [DataRow("", DisplayName = "FTP_HostEmpty")]
        [DataRow("  ", DisplayName = "FTP_HostWhitespace")]
        public async Task FtpProvider_InvalidHost_ReturnsFailure(string host)
        {
            var provider = new FtpSftpProvider(CloudProviderType.Ftp);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                Host = host,
                DisplayName = "Test"
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null, DisplayName = "SFTP_HostNull")]
        [DataRow("", DisplayName = "SFTP_HostEmpty")]
        public async Task SftpProvider_InvalidHost_ReturnsFailure(string host)
        {
            var provider = new FtpSftpProvider(CloudProviderType.Sftp);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.Sftp,
                Host = host,
                DisplayName = "Test"
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════
        // 7. LocalNetworkProvider — UNC Path Validations
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(null, DisplayName = "UNC_PathNull")]
        [DataRow("", DisplayName = "UNC_PathEmpty")]
        public async Task LocalNetwork_EmptyPath_ReturnsFailure(string uncPath)
        {
            var provider = new LocalNetworkProvider(CloudProviderType.UncPath);
            var config = new CloudTargetConfig
            {
                Type = CloudProviderType.UncPath,
                LocalOrUncPath = uncPath,
                DisplayName = "Test"
            };

            var result = await provider.UploadAsync(
                @"C:\nonexistent.bak", "test.bak", config, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [TestMethod]
        public async Task LocalNetwork_ValidLocalPath_CopiesFile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "KoruUNC_Test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var sourceFile = Path.Combine(Path.GetTempPath(), $"koru_test_{Guid.NewGuid()}.bak");
            File.WriteAllText(sourceFile, "test backup content");

            try
            {
                var provider = new LocalNetworkProvider(CloudProviderType.UncPath);
                var config = new CloudTargetConfig
                {
                    Type = CloudProviderType.UncPath,
                    LocalOrUncPath = tempDir,
                    DisplayName = "Local Test"
                };

                var result = await provider.UploadAsync(
                    sourceFile, "test.bak", config, null, CancellationToken.None);

                result.IsSuccess.Should().BeTrue();
                File.Exists(Path.Combine(tempDir, "test.bak")).Should().BeTrue();
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 8. CloudProviderFactory — All Types Covered
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(CloudProviderType.Ftp, typeof(FtpSftpProvider))]
        [DataRow(CloudProviderType.Ftps, typeof(FtpSftpProvider))]
        [DataRow(CloudProviderType.Sftp, typeof(FtpSftpProvider))]
        [DataRow(CloudProviderType.UncPath, typeof(LocalNetworkProvider))]
        public void CloudProviderFactory_CreatesCorrectProvider(CloudProviderType type, Type expectedType)
        {
            var factory = new CloudProviderFactory();
            var provider = factory.CreateProvider(type);
            provider.Should().BeOfType(expectedType);
        }

        // ═══════════════════════════════════════════════════════
        // 9. Password × Plan Export/Import Round-Trip
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void ExportImport_PlanWithPassword_PasswordPreserved()
        {
            var plan = TestDataFactory.CreateValidPlan("ExportPwTest");
            plan.PasswordHash = PlanPasswordHelper.HashPassword("exportSecret");
            _planManager.SavePlan(plan);

            string exportPath = Path.Combine(Path.GetTempPath(), $"koru_export_{Guid.NewGuid()}.json");

            try
            {
                _planManager.ExportPlan(plan.PlanId, exportPath);
                File.Exists(exportPath).Should().BeTrue();

                // JSON dosyasında passwordHash olmalı
                string json = File.ReadAllText(exportPath);
                json.Should().Contain("passwordHash");

                // Import
                var imported = _planManager.ImportPlan(exportPath);
                imported.HasPlanPassword.Should().BeTrue();

                // Not: DPAPI makine+kullanıcıya bağlı olduğu için
                // aynı makinede import edilen hash doğrulanabilir
                PlanPasswordHelper.VerifyPassword("exportSecret", imported.PasswordHash).Should().BeTrue();

                _planManager.DeletePlan(imported.PlanId);
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
                if (File.Exists(exportPath)) File.Delete(exportPath);
            }
        }

        [TestMethod]
        public void ExportImport_PlanWithoutPassword_NoPasswordField()
        {
            var plan = TestDataFactory.CreateValidPlan("ExportNoPw");
            plan.PasswordHash = null;
            _planManager.SavePlan(plan);

            string exportPath = Path.Combine(Path.GetTempPath(), $"koru_export_nopw_{Guid.NewGuid()}.json");

            try
            {
                _planManager.ExportPlan(plan.PlanId, exportPath);
                string json = File.ReadAllText(exportPath);

                // NullValueHandling.Ignore — passwordHash JSON'da olmamalı
                json.Should().NotContain("passwordHash");

                var imported = _planManager.ImportPlan(exportPath);
                imported.HasPlanPassword.Should().BeFalse();

                _planManager.DeletePlan(imported.PlanId);
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
                if (File.Exists(exportPath)) File.Delete(exportPath);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 10. Full Combination Matrix — 8 Senaryo Save/Load
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(false, false, false, false, DisplayName = "NoCloud_NoPw_NoFile_NoArchivePw")]
        [DataRow(true, false, false, false, DisplayName = "Cloud_NoPw_NoFile_NoArchivePw")]
        [DataRow(false, true, false, false, DisplayName = "NoCloud_Pw_NoFile_NoArchivePw")]
        [DataRow(false, false, true, false, DisplayName = "NoCloud_NoPw_File_NoArchivePw")]
        [DataRow(false, false, false, true, DisplayName = "NoCloud_NoPw_NoFile_ArchivePw")]
        [DataRow(true, true, true, false, DisplayName = "Cloud_Pw_File_NoArchivePw")]
        [DataRow(true, true, false, true, DisplayName = "Cloud_Pw_NoFile_ArchivePw")]
        [DataRow(true, true, true, true, DisplayName = "Cloud_Pw_File_ArchivePw_FULL")]
        public void SaveAndLoad_FeatureCombinationMatrix(
            bool withCloud, bool withPlanPw, bool withFile, bool withArchivePw)
        {
            var plan = TestDataFactory.CreateValidPlan($"Matrix_{withCloud}_{withPlanPw}_{withFile}_{withArchivePw}");

            if (withCloud)
            {
                plan.CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" },
                    new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = true, DisplayName = "UNC" }
                };
            }
            else
            {
                plan.CloudTargets.Clear();
            }

            plan.PasswordHash = withPlanPw ? PlanPasswordHelper.HashPassword("matrixPw") : null;

            if (withFile)
            {
                plan.FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource { SourceName = "Src", SourcePath = @"C:\Src", IsEnabled = true }
                    }
                };
            }

            plan.Compression.ArchivePassword = withArchivePw
                ? PasswordProtector.Protect("archPw")
                : null;

            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            try
            {
                loaded.HasCloudTargets.Should().Be(withCloud);
                loaded.HasPlanPassword.Should().Be(withPlanPw);

                if (withPlanPw)
                    PlanPasswordHelper.VerifyPassword("matrixPw", loaded.PasswordHash).Should().BeTrue();

                if (withFile)
                {
                    loaded.FileBackup.Should().NotBeNull();
                    loaded.FileBackup.IsEnabled.Should().BeTrue();
                }

                if (withArchivePw)
                    PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archPw");
                else
                    loaded.Compression.ArchivePassword.Should().BeNull();

                if (withCloud)
                    loaded.CloudTargets.Should().HaveCount(2);
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
            }
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private static Mock<ICloudProvider> CreateMockProvider(
            CloudProviderType type, bool success, string errorMessage = null)
        {
            var mock = new Mock<ICloudProvider>();
            mock.Setup(p => p.ProviderType).Returns(type);

            mock.Setup(p => p.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CloudTargetConfig>(),
                    It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>(),
                    It.IsAny<string>(), It.IsAny<Action<string>>()))
                .ReturnsAsync(new CloudUploadResult
                {
                    ProviderType = type,
                    IsSuccess = success,
                    ErrorMessage = errorMessage,
                    RemoteFilePath = success ? "/backups/test.7z" : null,
                    UploadedAt = DateTime.UtcNow
                });

            return mock;
        }
    }
}
