using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// BackupPlan modeli — tüm özellik kombinasyonları, computed property'ler ve JSON serileştirme testleri.
    /// Her özelliğin diğerleriyle etkileşimi test edilir.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class BackupPlanModelTests
    {
        // ═══════════════════════════════════════════════════════
        // 1. HasPlanPassword — Computed Property
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void HasPlanPassword_WhenNull_ReturnsFalse()
        {
            var plan = new BackupPlan { PasswordHash = null };
            plan.HasPlanPassword.Should().BeFalse();
        }

        [TestMethod]
        public void HasPlanPassword_WhenEmpty_ReturnsFalse()
        {
            var plan = new BackupPlan { PasswordHash = "" };
            plan.HasPlanPassword.Should().BeFalse();
        }

        [TestMethod]
        public void HasPlanPassword_WhenSet_ReturnsTrue()
        {
            var plan = new BackupPlan { PasswordHash = PlanPasswordHelper.HashPassword("test") };
            plan.HasPlanPassword.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════
        // 2. HasCloudTargets — Computed Property
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void HasCloudTargets_NullList_ReturnsFalse()
        {
            var plan = new BackupPlan { CloudTargets = null };
            plan.HasCloudTargets.Should().BeFalse();
        }

        [TestMethod]
        public void HasCloudTargets_EmptyList_ReturnsFalse()
        {
            var plan = new BackupPlan { CloudTargets = new List<CloudTargetConfig>() };
            plan.HasCloudTargets.Should().BeFalse();
        }

        [TestMethod]
        public void HasCloudTargets_AllDisabled_ReturnsFalse()
        {
            var plan = new BackupPlan
            {
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { IsEnabled = false, Type = CloudProviderType.Ftp },
                    new CloudTargetConfig { IsEnabled = false, Type = CloudProviderType.Sftp }
                }
            };
            plan.HasCloudTargets.Should().BeFalse();
        }

        [TestMethod]
        public void HasCloudTargets_OneEnabled_ReturnsTrue()
        {
            var plan = new BackupPlan
            {
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { IsEnabled = false, Type = CloudProviderType.Ftp },
                    new CloudTargetConfig { IsEnabled = true, Type = CloudProviderType.Sftp }
                }
            };
            plan.HasCloudTargets.Should().BeTrue();
        }

        [TestMethod]
        public void HasCloudTargets_MultipleEnabled_ReturnsTrue()
        {
            var plan = new BackupPlan
            {
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { IsEnabled = true, Type = CloudProviderType.Ftp },
                    new CloudTargetConfig { IsEnabled = true, Type = CloudProviderType.GoogleDrivePersonal },
                    new CloudTargetConfig { IsEnabled = true, Type = CloudProviderType.Mega }
                }
            };
            plan.HasCloudTargets.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════
        // 3. HasCloudTargets × HasPlanPassword — Bağımsızlık
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(false, false, DisplayName = "CloudYok_ŞifreYok")]
        [DataRow(false, true, DisplayName = "CloudYok_ŞifreVar")]
        [DataRow(true, false, DisplayName = "CloudVar_ŞifreYok")]
        [DataRow(true, true, DisplayName = "CloudVar_ŞifreVar")]
        public void HasCloudTargets_And_HasPlanPassword_AreIndependent(bool hasCloud, bool hasPassword)
        {
            var plan = new BackupPlan();

            if (hasCloud)
            {
                plan.CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { IsEnabled = true, Type = CloudProviderType.Ftp }
                };
            }
            else
            {
                plan.CloudTargets = new List<CloudTargetConfig>();
            }

            plan.PasswordHash = hasPassword ? PlanPasswordHelper.HashPassword("test") : null;

            plan.HasCloudTargets.Should().Be(hasCloud);
            plan.HasPlanPassword.Should().Be(hasPassword);
        }

        // ═══════════════════════════════════════════════════════
        // 4. JSON Serileştirme Round-Trip — Tüm Alanlar
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void JsonRoundTrip_MinimalPlan_PreservesDefaults()
        {
            var plan = new BackupPlan { PlanName = "Minimal" };

            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            loaded.Should().NotBeNull();
            loaded.PlanName.Should().Be("Minimal");
            loaded.IsEnabled.Should().BeTrue();
            loaded.HasPlanPassword.Should().BeFalse();
            loaded.HasCloudTargets.Should().BeFalse();
            loaded.PasswordHash.Should().BeNull();
        }

        [TestMethod]
        public void JsonRoundTrip_PlanWithPassword_PreservesPasswordHash()
        {
            var plan = new BackupPlan
            {
                PlanName = "WithPassword",
                PasswordHash = PlanPasswordHelper.HashPassword("secret123")
            };

            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            loaded.HasPlanPassword.Should().BeTrue();
            loaded.PasswordHash.Should().Be(plan.PasswordHash);
            // Hash'in doğrulaması çalışmalı
            PlanPasswordHelper.VerifyPassword("secret123", loaded.PasswordHash).Should().BeTrue();
        }

        [TestMethod]
        public void JsonRoundTrip_PlanWithoutPassword_OmitsPasswordHash()
        {
            var plan = new BackupPlan { PlanName = "NoPassword", PasswordHash = null };

            string json = JsonConvert.SerializeObject(plan);

            // NullValueHandling.Ignore — json'da "passwordHash" olmamalı
            json.Should().NotContain("passwordHash");
        }

        [TestMethod]
        public void JsonRoundTrip_FullPlan_AllFieldsPreserved()
        {
            var plan = CreateFullyConfiguredPlan();

            string json = JsonConvert.SerializeObject(plan, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            loaded.PlanId.Should().Be(plan.PlanId);
            loaded.PlanName.Should().Be(plan.PlanName);
            loaded.IsEnabled.Should().Be(plan.IsEnabled);
            loaded.LocalPath.Should().Be(plan.LocalPath);
            loaded.VerifyAfterBackup.Should().Be(plan.VerifyAfterBackup);
            loaded.SchemaVersion.Should().Be(plan.SchemaVersion);

            // Password
            loaded.PasswordHash.Should().Be(plan.PasswordHash);
            loaded.HasPlanPassword.Should().BeTrue();

            // SQL Connection
            loaded.SqlConnection.Server.Should().Be("localhost\\SQLEXPRESS");
            loaded.SqlConnection.AuthMode.Should().Be(SqlAuthMode.SqlAuthentication);
            loaded.SqlConnection.Username.Should().Be("sa");

            // Databases
            loaded.Databases.Should().HaveCount(3);
            loaded.Databases.Should().Contain("master");

            // Strategy
            loaded.Strategy.Type.Should().Be(BackupStrategyType.FullPlusDifferentialPlusIncremental);
            loaded.Strategy.AutoPromoteToFullAfter.Should().Be(5);

            // Compression
            loaded.Compression.Algorithm.Should().Be(CompressionAlgorithm.Lzma2);
            loaded.Compression.Level.Should().Be(CompressionLevel.Ultra);
            loaded.Compression.ArchivePassword.Should().NotBeNullOrEmpty();

            // Retention
            loaded.Retention.Type.Should().Be(RetentionPolicyType.Both);
            loaded.Retention.KeepLastN.Should().Be(20);
            loaded.Retention.DeleteOlderThanDays.Should().Be(60);
            loaded.Retention.GfsKeepWeekly.Should().Be(8);

            // Cloud Targets
            loaded.CloudTargets.Should().HaveCount(3);
            loaded.HasCloudTargets.Should().BeTrue();

            // File Backup
            loaded.FileBackup.Should().NotBeNull();
            loaded.FileBackup.IsEnabled.Should().BeTrue();
            loaded.FileBackup.Sources.Should().HaveCount(2);

            // Notifications
            loaded.Notifications.EmailEnabled.Should().BeTrue();
            loaded.Notifications.ToastEnabled.Should().BeTrue();

            // Reporting
            loaded.Reporting.IsEnabled.Should().BeTrue();
            loaded.Reporting.Frequency.Should().Be(ReportFrequency.Weekly);
        }

        [TestMethod]
        public void JsonRoundTrip_CloudTargetTypes_AllPreserved()
        {
            var providerTypes = new[]
            {
                CloudProviderType.Ftp,
                CloudProviderType.Ftps,
                CloudProviderType.Sftp,
                CloudProviderType.GoogleDrivePersonal,
                CloudProviderType.Mega,
                CloudProviderType.UncPath
            };

            var plan = new BackupPlan
            {
                PlanName = "AllProviders",
                CloudTargets = providerTypes.Select(t => new CloudTargetConfig
                {
                    Type = t,
                    IsEnabled = true,
                    DisplayName = t.ToString()
                }).ToList()
            };

            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            loaded.CloudTargets.Should().HaveCount(providerTypes.Length);
            for (int i = 0; i < providerTypes.Length; i++)
            {
                loaded.CloudTargets[i].Type.Should().Be(providerTypes[i]);
            }
        }

        [TestMethod]
        public void JsonRoundTrip_AllCompressionAlgorithms_Preserved()
        {
            foreach (CompressionAlgorithm alg in Enum.GetValues(typeof(CompressionAlgorithm)))
            {
                var plan = new BackupPlan
                {
                    PlanName = $"Alg_{alg}",
                    Compression = new CompressionConfig { Algorithm = alg }
                };

                string json = JsonConvert.SerializeObject(plan);
                var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);
                loaded.Compression.Algorithm.Should().Be(alg, $"Algoritma {alg} korunmalı");
            }
        }

        [TestMethod]
        public void JsonRoundTrip_AllCompressionLevels_Preserved()
        {
            foreach (CompressionLevel level in Enum.GetValues(typeof(CompressionLevel)))
            {
                var plan = new BackupPlan
                {
                    PlanName = $"Level_{level}",
                    Compression = new CompressionConfig { Level = level }
                };

                string json = JsonConvert.SerializeObject(plan);
                var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);
                loaded.Compression.Level.Should().Be(level, $"Seviye {level} korunmalı");
            }
        }

        [TestMethod]
        public void JsonRoundTrip_AllRetentionPolicyTypes_Preserved()
        {
            foreach (RetentionPolicyType rt in Enum.GetValues(typeof(RetentionPolicyType)))
            {
                var plan = new BackupPlan
                {
                    PlanName = $"Ret_{rt}",
                    Retention = new RetentionPolicy { Type = rt, KeepLastN = 5, DeleteOlderThanDays = 15 }
                };

                string json = JsonConvert.SerializeObject(plan);
                var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);
                loaded.Retention.Type.Should().Be(rt, $"Retention tipi {rt} korunmalı");
            }
        }

        [TestMethod]
        public void JsonRoundTrip_AllStrategyTypes_Preserved()
        {
            foreach (BackupStrategyType st in Enum.GetValues(typeof(BackupStrategyType)))
            {
                var plan = new BackupPlan
                {
                    PlanName = $"Strategy_{st}",
                    Strategy = new BackupStrategyConfig { Type = st }
                };

                string json = JsonConvert.SerializeObject(plan);
                var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);
                loaded.Strategy.Type.Should().Be(st, $"Strateji {st} korunmalı");
            }
        }

        // ═══════════════════════════════════════════════════════
        // 5. Eski JSON Uyumluluğu — Geriye Dönük
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void JsonDeserialize_MissingPasswordHash_DefaultsToNull()
        {
            // v0.73.0 öncesi JSON'da passwordHash alanı yok
            var json = """{"planId":"abc","planName":"Old Plan","isEnabled":true}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.HasPlanPassword.Should().BeFalse();
            plan.PasswordHash.Should().BeNull();
        }

        [TestMethod]
        public void JsonDeserialize_NullPasswordHash_HasPlanPasswordFalse()
        {
            var json = """{"planId":"abc","planName":"Test","passwordHash":null}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.HasPlanPassword.Should().BeFalse();
        }

        [TestMethod]
        public void JsonDeserialize_EmptyPasswordHash_HasPlanPasswordFalse()
        {
            var json = """{"planId":"abc","planName":"Test","passwordHash":""}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.HasPlanPassword.Should().BeFalse();
        }

        [TestMethod]
        public void JsonDeserialize_ObsoleteBackupMode_StillParsed()
        {
            // backupMode alanı hâlâ JSON'da okunabilmeli (geriye uyumluluk)
            var json = """{"planId":"abc","planName":"Legacy","backupMode":1}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.Should().NotBeNull();
#pragma warning disable CS0618
            plan.Mode.Should().Be(BackupMode.Cloud);
#pragma warning restore CS0618
        }

        [TestMethod]
        public void JsonDeserialize_MissingCloudTargets_HasCloudTargetsFalse()
        {
            var json = """{"planId":"abc","planName":"NoCloud"}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.HasCloudTargets.Should().BeFalse();
        }

        [TestMethod]
        public void JsonDeserialize_EmptyCloudTargets_HasCloudTargetsFalse()
        {
            var json = """{"planId":"abc","planName":"Empty","cloudTargets":[]}""";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.HasCloudTargets.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════
        // 6. Özellik Kombinasyonu Matrisi
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(false, false, false, DisplayName = "SqlOnly_NoCloud_NoPassword_NoFile")]
        [DataRow(false, false, true, DisplayName = "SqlOnly_NoCloud_NoPassword_WithFile")]
        [DataRow(false, true, false, DisplayName = "SqlOnly_NoCloud_WithPassword_NoFile")]
        [DataRow(false, true, true, DisplayName = "SqlOnly_NoCloud_WithPassword_WithFile")]
        [DataRow(true, false, false, DisplayName = "SqlOnly_WithCloud_NoPassword_NoFile")]
        [DataRow(true, false, true, DisplayName = "SqlOnly_WithCloud_NoPassword_WithFile")]
        [DataRow(true, true, false, DisplayName = "SqlOnly_WithCloud_WithPassword_NoFile")]
        [DataRow(true, true, true, DisplayName = "SqlOnly_WithCloud_WithPassword_WithFile")]
        public void FeatureMatrix_AllCombinations_SerializeAndDeserializeCorrectly(
            bool withCloud, bool withPassword, bool withFileBackup)
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();

            if (withCloud)
            {
                plan.CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP" }
                };
            }
            else
            {
                plan.CloudTargets = new List<CloudTargetConfig>();
            }

            if (withPassword)
            {
                plan.PasswordHash = PlanPasswordHelper.HashPassword("testPw");
            }

            if (withFileBackup)
            {
                plan.FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource { SourceName = "Test", SourcePath = @"C:\Test", IsEnabled = true }
                    }
                };
            }

            // Act
            string json = JsonConvert.SerializeObject(plan, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            // Assert — computed property'ler doğru mu?
            loaded.HasCloudTargets.Should().Be(withCloud);
            loaded.HasPlanPassword.Should().Be(withPassword);

            if (withPassword)
            {
                PlanPasswordHelper.VerifyPassword("testPw", loaded.PasswordHash).Should().BeTrue();
            }

            if (withFileBackup)
            {
                loaded.FileBackup.Should().NotBeNull();
                loaded.FileBackup.IsEnabled.Should().BeTrue();
                loaded.FileBackup.Sources.Should().HaveCount(1);
            }

            if (withCloud)
            {
                loaded.CloudTargets.Should().NotBeEmpty();
                loaded.CloudTargets.First().Type.Should().Be(CloudProviderType.Ftp);
            }
        }

        // ═══════════════════════════════════════════════════════
        // 7. Password + ArchivePassword — Bağımsızlık
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void PlanPassword_And_ArchivePassword_AreIndependent()
        {
            // İki farklı şifre sistemi: plan koruması (SHA256+DPAPI) vs arşiv şifresi (DPAPI raw)
            var plan = new BackupPlan
            {
                PlanName = "DualPassword",
                PasswordHash = PlanPasswordHelper.HashPassword("planSecret"),
                Compression = new CompressionConfig
                {
                    ArchivePassword = PasswordProtector.Protect("archiveSecret")
                }
            };

            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            // Plan password — hash doğrulama ile
            loaded.HasPlanPassword.Should().BeTrue();
            PlanPasswordHelper.VerifyPassword("planSecret", loaded.PasswordHash).Should().BeTrue();
            PlanPasswordHelper.VerifyPassword("archiveSecret", loaded.PasswordHash).Should().BeFalse();

            // Archive password — DPAPI decrypt ile
            string decrypted = PasswordProtector.Unprotect(loaded.Compression.ArchivePassword);
            decrypted.Should().Be("archiveSecret");
        }

        [TestMethod]
        public void PlanPassword_Set_ArchivePassword_Empty_BothWorkIndependently()
        {
            var plan = new BackupPlan
            {
                PasswordHash = PlanPasswordHelper.HashPassword("planOnly"),
                Compression = new CompressionConfig { ArchivePassword = null }
            };

            plan.HasPlanPassword.Should().BeTrue();
            plan.Compression.ArchivePassword.Should().BeNull();
        }

        [TestMethod]
        public void PlanPassword_Empty_ArchivePassword_Set_BothWorkIndependently()
        {
            var plan = new BackupPlan
            {
                PasswordHash = null,
                Compression = new CompressionConfig
                {
                    ArchivePassword = PasswordProtector.Protect("archiveOnly")
                }
            };

            plan.HasPlanPassword.Should().BeFalse();
            PasswordProtector.Unprotect(plan.Compression.ArchivePassword).Should().Be("archiveOnly");
        }

        // ═══════════════════════════════════════════════════════
        // 8. Cloud Target Types × Enable/Disable Combinations
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void HasCloudTargets_MixedEnabledDisabled_AllProviderTypes()
        {
            // Her provider tipinden bir tane, bazıları enabled bazıları disabled
            var plan = new BackupPlan
            {
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false },
                    new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true },
                    new CloudTargetConfig { Type = CloudProviderType.GoogleDrivePersonal, IsEnabled = false },
                    new CloudTargetConfig { Type = CloudProviderType.Mega, IsEnabled = true },
                    new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = false }
                }
            };

            plan.HasCloudTargets.Should().BeTrue("en az bir aktif hedef var");
            plan.CloudTargets.Count(t => t.IsEnabled).Should().Be(2);
        }

        // ═══════════════════════════════════════════════════════
        // 9. GFS Retention + Password + Cloud — Tam Kombinasyon
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void JsonRoundTrip_GfsRetention_WithPassword_WithCloud_AllPreserved()
        {
            var plan = new BackupPlan
            {
                PlanName = "GFS_Full",
                PasswordHash = PlanPasswordHelper.HashPassword("gfsSecret"),
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.GFS,
                    GfsKeepDaily = 14,
                    GfsKeepWeekly = 8,
                    GfsKeepMonthly = 24,
                    GfsKeepYearly = 5
                },
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Mega, IsEnabled = true, DisplayName = "Mega" }
                }
            };

            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            loaded.HasPlanPassword.Should().BeTrue();
            loaded.HasCloudTargets.Should().BeTrue();
            loaded.Retention.Type.Should().Be(RetentionPolicyType.GFS);
            loaded.Retention.GfsKeepDaily.Should().Be(14);
            loaded.Retention.GfsKeepWeekly.Should().Be(8);
            loaded.Retention.GfsKeepMonthly.Should().Be(24);
            loaded.Retention.GfsKeepYearly.Should().Be(5);
        }

        // ═══════════════════════════════════════════════════════
        // 10. FileBackup + Cloud + Password Tam Senaryo
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void JsonRoundTrip_FileBackup_MultiSource_WithVss_WithCloud_WithPassword()
        {
            var plan = new BackupPlan
            {
                PlanName = "MaxConfig",
                PasswordHash = PlanPasswordHelper.HashPassword("maxSecret"),
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Schedule = "0 0 3 ? * MON-FRI",
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource
                        {
                            SourceName = "Outlook",
                            SourcePath = @"C:\Users\Test\Outlook",
                            Recursive = true,
                            UseVss = true,
                            IsEnabled = true,
                            IncludePatterns = new List<string> { "*.pst", "*.ost" }
                        },
                        new FileBackupSource
                        {
                            SourceName = "Muhasebe",
                            SourcePath = @"D:\Muhasebe",
                            Recursive = false,
                            UseVss = false,
                            IsEnabled = true,
                            ExcludePatterns = new List<string> { "*.tmp" }
                        },
                        new FileBackupSource
                        {
                            SourceName = "Disabled",
                            SourcePath = @"E:\Old",
                            IsEnabled = false
                        }
                    }
                },
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.Ftp,
                        IsEnabled = true,
                        Host = "ftp.example.com",
                        Port = 21,
                        Username = "user",
                        RemoteFolderPath = "/backups"
                    },
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.GoogleDrivePersonal,
                        IsEnabled = true,
                        OAuthClientId = "client-id",
                        RemoteFolderPath = "KoruBackups"
                    }
                }
            };

            string json = JsonConvert.SerializeObject(plan, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            // Password
            loaded.HasPlanPassword.Should().BeTrue();
            PlanPasswordHelper.VerifyPassword("maxSecret", loaded.PasswordHash).Should().BeTrue();

            // FileBackup
            loaded.FileBackup.IsEnabled.Should().BeTrue();
            loaded.FileBackup.Schedule.Should().Be("0 0 3 ? * MON-FRI");
            loaded.FileBackup.Sources.Should().HaveCount(3);
            loaded.FileBackup.Sources[0].UseVss.Should().BeTrue();
            loaded.FileBackup.Sources[0].IncludePatterns.Should().Contain("*.pst");
            loaded.FileBackup.Sources[1].ExcludePatterns.Should().Contain("*.tmp");
            loaded.FileBackup.Sources[2].IsEnabled.Should().BeFalse();

            // Cloud
            loaded.HasCloudTargets.Should().BeTrue();
            loaded.CloudTargets.Should().HaveCount(2);
            loaded.CloudTargets[0].Host.Should().Be("ftp.example.com");
            loaded.CloudTargets[1].OAuthClientId.Should().Be("client-id");
        }

        // ═══════════════════════════════════════════════════════
        // 11. Password Ekleme/Kaldırma Simülasyonu (Lifecycle)
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void PlanPasswordLifecycle_SetChangeClear()
        {
            var plan = new BackupPlan { PlanName = "Lifecycle" };

            // Başlangıçta şifre yok
            plan.HasPlanPassword.Should().BeFalse();

            // Şifre koy
            plan.PasswordHash = PlanPasswordHelper.HashPassword("first");
            plan.HasPlanPassword.Should().BeTrue();
            PlanPasswordHelper.VerifyPassword("first", plan.PasswordHash).Should().BeTrue();

            // Şifre değiştir
            plan.PasswordHash = PlanPasswordHelper.HashPassword("second");
            PlanPasswordHelper.VerifyPassword("first", plan.PasswordHash).Should().BeFalse();
            PlanPasswordHelper.VerifyPassword("second", plan.PasswordHash).Should().BeTrue();

            // Şifre kaldır
            plan.PasswordHash = null;
            plan.HasPlanPassword.Should().BeFalse();

            // Tekrar koy
            plan.PasswordHash = PlanPasswordHelper.HashPassword("third");
            plan.HasPlanPassword.Should().BeTrue();
            PlanPasswordHelper.VerifyPassword("third", plan.PasswordHash).Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════
        // 12. Default Value Doğrulaması
        // ═══════════════════════════════════════════════════════

        [TestMethod]
        public void NewPlan_AllDefaultValues_AreCorrect()
        {
            var plan = new BackupPlan();

            plan.PlanId.Should().NotBeNullOrEmpty();
            plan.IsEnabled.Should().BeTrue();
            plan.HasPlanPassword.Should().BeFalse();
            plan.HasCloudTargets.Should().BeFalse();
            plan.PasswordHash.Should().BeNull();
            plan.VerifyAfterBackup.Should().BeTrue();
            plan.SchemaVersion.Should().Be(1);
            plan.Databases.Should().NotBeNull().And.BeEmpty();
            plan.CloudTargets.Should().NotBeNull().And.BeEmpty();
            plan.SqlConnection.Should().NotBeNull();
            plan.Strategy.Should().NotBeNull();
            plan.Compression.Should().NotBeNull();
            plan.Retention.Should().NotBeNull();
            plan.Notifications.Should().NotBeNull();
            plan.Compression.Algorithm.Should().Be(CompressionAlgorithm.Lzma2);
            plan.Compression.Level.Should().Be(CompressionLevel.Ultra);
            plan.Retention.Type.Should().Be(RetentionPolicyType.KeepLastN);
            plan.Retention.KeepLastN.Should().Be(30);
            plan.Strategy.Type.Should().Be(BackupStrategyType.Full);
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private static BackupPlan CreateFullyConfiguredPlan()
        {
            return new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = "Full Config Plan",
                IsEnabled = true,
                PasswordHash = PlanPasswordHelper.HashPassword("fullConfigSecret"),
                SqlConnection = new SqlConnectionInfo
                {
                    Server = @"localhost\SQLEXPRESS",
                    AuthMode = SqlAuthMode.SqlAuthentication,
                    Username = "sa",
                    Password = PasswordProtector.Protect("sqlPass"),
                    ConnectionTimeoutSeconds = 60,
                    TrustServerCertificate = true
                },
                Databases = new List<string> { "master", "AdventureWorks", "Muhasebe" },
                Strategy = new BackupStrategyConfig
                {
                    Type = BackupStrategyType.FullPlusDifferentialPlusIncremental,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = "0 0 3 ? * MON-SAT",
                    IncrementalSchedule = "0 0 */4 ? * MON-SAT",
                    AutoPromoteToFullAfter = 5
                },
                Compression = new CompressionConfig
                {
                    Algorithm = CompressionAlgorithm.Lzma2,
                    Level = CompressionLevel.Ultra,
                    ArchivePassword = PasswordProtector.Protect("archPass")
                },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.Both,
                    KeepLastN = 20,
                    DeleteOlderThanDays = 60,
                    GfsKeepDaily = 7,
                    GfsKeepWeekly = 8,
                    GfsKeepMonthly = 12,
                    GfsKeepYearly = 2
                },
                LocalPath = @"D:\Backups\Production",
                VerifyAfterBackup = true,
                SchemaVersion = 2,
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.Ftp,
                        IsEnabled = true,
                        DisplayName = "Production FTP",
                        Host = "ftp.production.com",
                        Port = 21,
                        Username = "backup-user",
                        RemoteFolderPath = "/sql-backups"
                    },
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.GoogleDrivePersonal,
                        IsEnabled = true,
                        DisplayName = "Google Drive",
                        OAuthClientId = "test-client-id",
                        RemoteFolderPath = "KoruBackups"
                    },
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.UncPath,
                        IsEnabled = true,
                        DisplayName = "Network Share",
                        LocalOrUncPath = @"\\nas01\backups\sql"
                    }
                },
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Schedule = "0 0 4 ? * MON-FRI",
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource
                        {
                            SourceName = "Outlook PST",
                            SourcePath = @"C:\Users\Test\AppData\Local\Microsoft\Outlook",
                            Recursive = true,
                            UseVss = true,
                            IsEnabled = true,
                            IncludePatterns = new List<string> { "*.pst", "*.ost" }
                        },
                        new FileBackupSource
                        {
                            SourceName = "Muhasebe",
                            SourcePath = @"D:\Muhasebe\Data",
                            Recursive = true,
                            UseVss = false,
                            IsEnabled = true,
                            ExcludePatterns = new List<string> { "*.tmp", "~*" }
                        }
                    }
                },
                Notifications = new NotificationConfig
                {
                    EmailEnabled = true,
                    OnSuccess = true,
                    OnFailure = true,
                    ToastEnabled = true,
                    EmailTo = "admin@test.com",
                    SmtpServer = "smtp.test.com",
                    SmtpPort = 587,
                    SmtpUseSsl = true
                },
                Reporting = new ReportingConfig
                {
                    IsEnabled = true,
                    Frequency = ReportFrequency.Weekly,
                    EmailTo = "reports@test.com",
                    SendHour = 9
                }
            };
        }
    }
}
