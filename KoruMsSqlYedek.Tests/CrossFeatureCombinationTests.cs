using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Tests.Helpers;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// Tüm BackupPlan özelliklerinin çapraz kombinasyonlarını test eder.
    /// Her boyut: Strategy × Retention × Cloud × Password × FileBackup × Compression × Verify × SqlAuth × Notification × Reporting
    /// JSON round-trip ile veri kaybı olmaması doğrulanır.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CrossFeature")]
    public class CrossFeatureCombinationTests
    {
        // ═══════════════════════════════════════════════════════════════
        // 1. MEGA MATRİS: Strategy × Retention × Cloud × Password × FileBackup × ArchivePw × Verify
        //    3 × 4 × 2 × 2 × 2 × 2 × 2 = 384 kombinasyon (DynamicData ile)
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DynamicData(nameof(GetFullMatrixData), DynamicDataSourceType.Method)]
        public void FullMatrix_JsonRoundTrip_AllFieldsPreserved(
            BackupStrategyType strategy,
            RetentionPolicyType retention,
            bool withCloud,
            bool withPassword,
            bool withFileBackup,
            bool withArchivePassword,
            bool verifyAfterBackup)
        {
            // Arrange
            var plan = BuildFullMatrixPlan(
                strategy, retention, withCloud, withPassword,
                withFileBackup, withArchivePassword, verifyAfterBackup);

            // Act — JSON round-trip
            string json = JsonConvert.SerializeObject(plan, Formatting.Indented);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            // Assert — temel alanlar
            loaded.Should().NotBeNull();
            loaded.PlanId.Should().Be(plan.PlanId);
            loaded.Strategy.Type.Should().Be(strategy);
            loaded.Retention.Type.Should().Be(retention);
            loaded.VerifyAfterBackup.Should().Be(verifyAfterBackup);

            // Assert — Cloud
            loaded.HasCloudTargets.Should().Be(withCloud);
            if (withCloud)
            {
                loaded.CloudTargets.Should().NotBeEmpty();
                loaded.CloudTargets.First().IsEnabled.Should().BeTrue();
            }

            // Assert — Password
            loaded.HasPlanPassword.Should().Be(withPassword);
            if (withPassword)
            {
                PlanPasswordHelper.VerifyPassword("megaMatrixPw", loaded.PasswordHash).Should().BeTrue();
            }

            // Assert — FileBackup
            if (withFileBackup)
            {
                loaded.FileBackup.Should().NotBeNull();
                loaded.FileBackup.IsEnabled.Should().BeTrue();
                loaded.FileBackup.Sources.Should().NotBeEmpty();
            }

            // Assert — ArchivePassword
            if (withArchivePassword)
            {
                loaded.Compression.ArchivePassword.Should().NotBeNullOrEmpty();
                PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archPw123");
            }
            else
            {
                loaded.Compression.ArchivePassword.Should().BeNull();
            }

            // Assert — Retention detayları
            switch (retention)
            {
                case RetentionPolicyType.KeepLastN:
                    loaded.Retention.KeepLastN.Should().Be(10);
                    break;
                case RetentionPolicyType.DeleteOlderThanDays:
                    loaded.Retention.DeleteOlderThanDays.Should().Be(30);
                    break;
                case RetentionPolicyType.Both:
                    loaded.Retention.KeepLastN.Should().Be(10);
                    loaded.Retention.DeleteOlderThanDays.Should().Be(30);
                    break;
                case RetentionPolicyType.GFS:
                    loaded.Retention.GfsKeepDaily.Should().Be(7);
                    loaded.Retention.GfsKeepWeekly.Should().Be(4);
                    loaded.Retention.GfsKeepMonthly.Should().Be(12);
                    loaded.Retention.GfsKeepYearly.Should().Be(2);
                    break;
            }
        }

        /// <summary>
        /// 384 kombinasyonun tamamını üretir (3×4×2×2×2×2×2).
        /// Test runner hepsini ayrı ayrı çalıştırır.
        /// </summary>
        private static IEnumerable<object[]> GetFullMatrixData()
        {
            var strategies = new[] { BackupStrategyType.Full, BackupStrategyType.FullPlusDifferential, BackupStrategyType.FullPlusDifferentialPlusIncremental };
            var retentions = new[] { RetentionPolicyType.KeepLastN, RetentionPolicyType.DeleteOlderThanDays, RetentionPolicyType.Both, RetentionPolicyType.GFS };
            var bools = new[] { false, true };

            foreach (var s in strategies)
                foreach (var r in retentions)
                    foreach (var cloud in bools)
                        foreach (var pw in bools)
                            foreach (var file in bools)
                                foreach (var archPw in bools)
                                    foreach (var verify in bools)
                                        yield return new object[] { s, r, cloud, pw, file, archPw, verify };
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. SQL AUTH × STRATEGY × COMPRESSION (2 × 3 × 4algo × 5level = 120)
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DynamicData(nameof(GetSqlAuthCompressionData), DynamicDataSourceType.Method)]
        public void SqlAuth_Strategy_Compression_JsonRoundTrip(
            SqlAuthMode authMode,
            BackupStrategyType strategy,
            CompressionAlgorithm algorithm,
            CompressionLevel level)
        {
            // Arrange
            var plan = new BackupPlan
            {
                PlanName = $"SqlAuth_{authMode}_{strategy}_{algorithm}_{level}",
                SqlConnection = new SqlConnectionInfo
                {
                    Server = "localhost",
                    AuthMode = authMode,
                    Username = authMode == SqlAuthMode.SqlAuthentication ? "sa" : null,
                    Password = authMode == SqlAuthMode.SqlAuthentication ? PasswordProtector.Protect("sqlPw") : null,
                    ConnectionTimeoutSeconds = 30,
                    TrustServerCertificate = true
                },
                Strategy = new BackupStrategyConfig
                {
                    Type = strategy,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = strategy != BackupStrategyType.Full ? "0 0 3 ? * MON-SAT" : null,
                    IncrementalSchedule = strategy == BackupStrategyType.FullPlusDifferentialPlusIncremental ? "0 0 */4 ? * MON-SAT" : null,
                    AutoPromoteToFullAfter = 7
                },
                Compression = new CompressionConfig
                {
                    Algorithm = algorithm,
                    Level = level
                },
                Databases = new List<string> { "TestDB" }
            };

            // Act
            string json = JsonConvert.SerializeObject(plan);
            var loaded = JsonConvert.DeserializeObject<BackupPlan>(json);

            // Assert
            loaded.SqlConnection.AuthMode.Should().Be(authMode);
            loaded.Strategy.Type.Should().Be(strategy);
            loaded.Compression.Algorithm.Should().Be(algorithm);
            loaded.Compression.Level.Should().Be(level);

            if (authMode == SqlAuthMode.SqlAuthentication)
            {
                loaded.SqlConnection.Username.Should().Be("sa");
                loaded.SqlConnection.Password.Should().NotBeNullOrEmpty();
                PasswordProtector.Unprotect(loaded.SqlConnection.Password).Should().Be("sqlPw");
            }
            else
            {
                loaded.SqlConnection.Username.Should().BeNull();
            }

            // Strategy schedule consistency
            if (strategy == BackupStrategyType.Full)
            {
                loaded.Strategy.DifferentialSchedule.Should().BeNull();
                loaded.Strategy.IncrementalSchedule.Should().BeNull();
            }
            else if (strategy == BackupStrategyType.FullPlusDifferential)
            {
                loaded.Strategy.DifferentialSchedule.Should().NotBeNullOrEmpty();
                loaded.Strategy.IncrementalSchedule.Should().BeNull();
            }
            else
            {
                loaded.Strategy.DifferentialSchedule.Should().NotBeNullOrEmpty();
                loaded.Strategy.IncrementalSchedule.Should().NotBeNullOrEmpty();
            }
        }

        private static IEnumerable<object[]> GetSqlAuthCompressionData()
        {
            var authModes = new[] { SqlAuthMode.Windows, SqlAuthMode.SqlAuthentication };
            var strategies = new[] { BackupStrategyType.Full, BackupStrategyType.FullPlusDifferential, BackupStrategyType.FullPlusDifferentialPlusIncremental };
            var algorithms = new[] { CompressionAlgorithm.Lzma2, CompressionAlgorithm.Lzma, CompressionAlgorithm.BZip2, CompressionAlgorithm.Deflate };
            var levels = new[] { CompressionLevel.None, CompressionLevel.Fast, CompressionLevel.Normal, CompressionLevel.Maximum, CompressionLevel.Ultra };

            foreach (var auth in authModes)
                foreach (var s in strategies)
                    foreach (var alg in algorithms)
                        foreach (var lvl in levels)
                            yield return new object[] { auth, s, alg, lvl };
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. CLOUD TARGET CONFIG — Provider-Specific Fields
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void CloudTarget_FtpConfig_AllFieldsPreserved()
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                IsEnabled = true,
                DisplayName = "Production FTP",
                Host = "ftp.example.com",
                Port = 21,
                Username = "backup-user",
                Password = PasswordProtector.Protect("ftpPw"),
                RemoteFolderPath = "/sql-backups",
                BandwidthLimitMbps = 10,
                PermanentDeleteFromTrash = true
            };

            var plan = CreatePlanWithSingleCloud(target);
            var loaded = RoundTrip(plan);

            var ct = loaded.CloudTargets.First();
            ct.Type.Should().Be(CloudProviderType.Ftp);
            ct.Host.Should().Be("ftp.example.com");
            ct.Port.Should().Be(21);
            ct.Username.Should().Be("backup-user");
            PasswordProtector.Unprotect(ct.Password).Should().Be("ftpPw");
            ct.RemoteFolderPath.Should().Be("/sql-backups");
            ct.BandwidthLimitMbps.Should().Be(10);
            ct.PermanentDeleteFromTrash.Should().BeTrue();
            ct.FtpsSkipCertificateValidation.Should().BeFalse();
            ct.SftpHostFingerprint.Should().BeNull();
        }

        [TestMethod]
        public void CloudTarget_FtpsConfig_CertValidationSkip_Preserved()
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftps,
                IsEnabled = true,
                DisplayName = "FTPS Internal",
                Host = "ftps.internal.local",
                Port = 990,
                Username = "admin",
                Password = PasswordProtector.Protect("ftpsPw"),
                RemoteFolderPath = "/backups",
                FtpsSkipCertificateValidation = true,
                BandwidthLimitMbps = 50,
                PermanentDeleteFromTrash = false
            };

            var plan = CreatePlanWithSingleCloud(target);
            var loaded = RoundTrip(plan);

            var ct = loaded.CloudTargets.First();
            ct.Type.Should().Be(CloudProviderType.Ftps);
            ct.FtpsSkipCertificateValidation.Should().BeTrue();
            ct.Port.Should().Be(990);
            ct.BandwidthLimitMbps.Should().Be(50);
            ct.PermanentDeleteFromTrash.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(true, DisplayName = "FTPS_CertSkip_True")]
        [DataRow(false, DisplayName = "FTPS_CertSkip_False")]
        public void CloudTarget_Ftps_CertValidation_BothValues(bool skipCert)
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftps,
                IsEnabled = true,
                DisplayName = "FTPS",
                Host = "ftps.test.com",
                Port = 990,
                FtpsSkipCertificateValidation = skipCert
            };

            var loaded = RoundTrip(CreatePlanWithSingleCloud(target));
            loaded.CloudTargets.First().FtpsSkipCertificateValidation.Should().Be(skipCert);
        }

        [TestMethod]
        public void CloudTarget_SftpConfig_Fingerprint_Preserved()
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.Sftp,
                IsEnabled = true,
                DisplayName = "SFTP Production",
                Host = "sftp.production.com",
                Port = 22,
                Username = "sftpuser",
                Password = PasswordProtector.Protect("sftpPw"),
                RemoteFolderPath = "/backups/sql",
                SftpHostFingerprint = "SHA256:abc123def456789",
                BandwidthLimitMbps = 25,
                PermanentDeleteFromTrash = true
            };

            var plan = CreatePlanWithSingleCloud(target);
            var loaded = RoundTrip(plan);

            var ct = loaded.CloudTargets.First();
            ct.Type.Should().Be(CloudProviderType.Sftp);
            ct.SftpHostFingerprint.Should().Be("SHA256:abc123def456789");
            ct.Port.Should().Be(22);
            ct.BandwidthLimitMbps.Should().Be(25);
            ct.PermanentDeleteFromTrash.Should().BeTrue();
            ct.FtpsSkipCertificateValidation.Should().BeFalse();
        }

        [TestMethod]
        public void CloudTarget_GoogleDriveConfig_OAuthFields_Preserved()
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                IsEnabled = true,
                DisplayName = "Google Drive Backup",
                RemoteFolderPath = "KoruBackups",
                OAuthClientId = "123456-abcdef.apps.googleusercontent.com",
                OAuthClientSecret = PasswordProtector.Protect("gdClientSecret"),
                OAuthTokenJson = "{\"access_token\":\"ya29.test\",\"refresh_token\":\"1//test\"}",
                PermanentDeleteFromTrash = false
            };

            var plan = CreatePlanWithSingleCloud(target);
            var loaded = RoundTrip(plan);

            var ct = loaded.CloudTargets.First();
            ct.Type.Should().Be(CloudProviderType.GoogleDrivePersonal);
            ct.OAuthClientId.Should().Be("123456-abcdef.apps.googleusercontent.com");
            PasswordProtector.Unprotect(ct.OAuthClientSecret).Should().Be("gdClientSecret");
            ct.OAuthTokenJson.Should().Contain("access_token");
            ct.RemoteFolderPath.Should().Be("KoruBackups");
            ct.PermanentDeleteFromTrash.Should().BeFalse();
        }

        [TestMethod]
        public void CloudTarget_UncPathConfig_AllFields_Preserved()
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.UncPath,
                IsEnabled = true,
                DisplayName = "NAS Backup",
                LocalOrUncPath = @"\\nas01\backups\sql",
                Username = @"DOMAIN\backupuser",
                Password = PasswordProtector.Protect("uncPw"),
                PermanentDeleteFromTrash = false
            };

            var plan = CreatePlanWithSingleCloud(target);
            var loaded = RoundTrip(plan);

            var ct = loaded.CloudTargets.First();
            ct.Type.Should().Be(CloudProviderType.UncPath);
            ct.LocalOrUncPath.Should().Be(@"\\nas01\backups\sql");
            ct.Username.Should().Be(@"DOMAIN\backupuser");
            PasswordProtector.Unprotect(ct.Password).Should().Be("uncPw");
            ct.PermanentDeleteFromTrash.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null, DisplayName = "Bandwidth_Null_Unlimited")]
        [DataRow(1, DisplayName = "Bandwidth_1Mbps")]
        [DataRow(10, DisplayName = "Bandwidth_10Mbps")]
        [DataRow(100, DisplayName = "Bandwidth_100Mbps")]
        [DataRow(1000, DisplayName = "Bandwidth_1000Mbps")]
        public void CloudTarget_BandwidthLimit_AllValues(int? limitMbps)
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.Ftp,
                IsEnabled = true,
                DisplayName = "BW Test",
                Host = "ftp.test.com",
                BandwidthLimitMbps = limitMbps
            };

            var loaded = RoundTrip(CreatePlanWithSingleCloud(target));
            loaded.CloudTargets.First().BandwidthLimitMbps.Should().Be(limitMbps);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PermanentDelete_True")]
        [DataRow(false, DisplayName = "PermanentDelete_False")]
        public void CloudTarget_PermanentDeleteFromTrash_BothValues(bool permanentDelete)
        {
            var target = new CloudTargetConfig
            {
                Type = CloudProviderType.GoogleDrivePersonal,
                IsEnabled = true,
                DisplayName = "Trash Test",
                PermanentDeleteFromTrash = permanentDelete
            };

            var loaded = RoundTrip(CreatePlanWithSingleCloud(target));
            loaded.CloudTargets.First().PermanentDeleteFromTrash.Should().Be(permanentDelete);
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. MULTI-CLOUD: Birden fazla provider aynı planda
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void MultiCloud_AllProviderTypes_InSamePlan_JsonRoundTrip()
        {
            var plan = new BackupPlan
            {
                PlanName = "MultiCloud_All",
                PasswordHash = PlanPasswordHelper.HashPassword("multiCloudPw"),
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP", Host = "ftp.test.com", Port = 21 },
                    new CloudTargetConfig { Type = CloudProviderType.Ftps, IsEnabled = true, DisplayName = "FTPS", Host = "ftps.test.com", Port = 990, FtpsSkipCertificateValidation = true },
                    new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP", Host = "sftp.test.com", Port = 22, SftpHostFingerprint = "SHA256:xyz" },
                    new CloudTargetConfig { Type = CloudProviderType.GoogleDrivePersonal, IsEnabled = true, DisplayName = "GDrive", OAuthClientId = "cid" },
                    new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = true, DisplayName = "UNC", LocalOrUncPath = @"\\server\share" }
                }
            };

            var loaded = RoundTrip(plan);

            loaded.HasCloudTargets.Should().BeTrue();
            loaded.CloudTargets.Should().HaveCount(5);
            loaded.HasPlanPassword.Should().BeTrue();

            loaded.CloudTargets.Select(c => c.Type).Should().BeEquivalentTo(
                new[] {
                    CloudProviderType.Ftp, CloudProviderType.Ftps, CloudProviderType.Sftp,
                    CloudProviderType.GoogleDrivePersonal, CloudProviderType.UncPath
                });
        }

        [TestMethod]
        public void MultiCloud_MixedEnabledDisabled_HasCloudTargetsCorrect()
        {
            var plan = new BackupPlan
            {
                PlanName = "MultiCloud_Mixed",
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false, DisplayName = "FTP Off" },
                    new CloudTargetConfig { Type = CloudProviderType.Ftps, IsEnabled = true, DisplayName = "FTPS On" },
                    new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = false, DisplayName = "SFTP Off" }
                }
            };

            plan.HasCloudTargets.Should().BeTrue("en az bir target enabled");
            plan.CloudTargets.Count(t => t.IsEnabled).Should().Be(1);

            var loaded = RoundTrip(plan);
            loaded.HasCloudTargets.Should().BeTrue();
            loaded.CloudTargets.Count(t => t.IsEnabled).Should().Be(1);
        }

        [TestMethod]
        public void MultiCloud_AllDisabled_HasCloudTargetsFalse()
        {
            var plan = new BackupPlan
            {
                PlanName = "MultiCloud_AllOff",
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = false },
                    new CloudTargetConfig { Type = CloudProviderType.Ftps, IsEnabled = false },
                    new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = false }
                }
            };

            plan.HasCloudTargets.Should().BeFalse();
            RoundTrip(plan).HasCloudTargets.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. NOTIFICATION: SmtpProfile vs Legacy × Flags Matrix
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DynamicData(nameof(GetNotificationMatrixData), DynamicDataSourceType.Method)]
        public void Notification_FlagMatrix_JsonRoundTrip(
            bool onSuccess,
            bool onFailure,
            bool emailEnabled,
            bool toastEnabled,
            bool useSmtpProfile)
        {
            var notif = new NotificationConfig
            {
                OnSuccess = onSuccess,
                OnFailure = onFailure,
                EmailEnabled = emailEnabled,
                ToastEnabled = toastEnabled
            };

            if (useSmtpProfile)
            {
                notif.SmtpProfileId = "profile-001";
                // Legacy fields should be null when profile is used
                notif.SmtpServer = null;
                notif.SmtpPort = null;
                notif.SmtpUseSsl = null;
                notif.EmailTo = "admin@company.com";
            }
            else
            {
                notif.SmtpProfileId = null;
                notif.SmtpServer = "smtp.legacy.com";
                notif.SmtpPort = 587;
                notif.SmtpUseSsl = true;
                notif.SmtpUsername = "legacyuser";
                notif.SmtpPassword = PasswordProtector.Protect("legacyPw");
                notif.EmailTo = "legacy@company.com";
            }

            var plan = new BackupPlan { PlanName = "NotifTest", Notifications = notif };
            var loaded = RoundTrip(plan);

            loaded.Notifications.OnSuccess.Should().Be(onSuccess);
            loaded.Notifications.OnFailure.Should().Be(onFailure);
            loaded.Notifications.EmailEnabled.Should().Be(emailEnabled);
            loaded.Notifications.ToastEnabled.Should().Be(toastEnabled);

            if (useSmtpProfile)
            {
                loaded.Notifications.SmtpProfileId.Should().Be("profile-001");
                loaded.Notifications.SmtpServer.Should().BeNull();
            }
            else
            {
                loaded.Notifications.SmtpProfileId.Should().BeNull();
                loaded.Notifications.SmtpServer.Should().Be("smtp.legacy.com");
                loaded.Notifications.SmtpPort.Should().Be(587);
                loaded.Notifications.SmtpUseSsl.Should().BeTrue();
                loaded.Notifications.SmtpUsername.Should().Be("legacyuser");
                PasswordProtector.Unprotect(loaded.Notifications.SmtpPassword).Should().Be("legacyPw");
            }
        }

        /// <summary>
        /// 2×2×2×2×2 = 32 notification flag kombinasyonu.
        /// </summary>
        private static IEnumerable<object[]> GetNotificationMatrixData()
        {
            var bools = new[] { false, true };
            foreach (var onS in bools)
                foreach (var onF in bools)
                    foreach (var email in bools)
                        foreach (var toast in bools)
                            foreach (var profile in bools)
                                yield return new object[] { onS, onF, email, toast, profile };
        }

        // ═══════════════════════════════════════════════════════════════
        // 6. REPORTING: Frequency × Enabled × EmailTo Matrix
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DynamicData(nameof(GetReportingMatrixData), DynamicDataSourceType.Method)]
        public void Reporting_FrequencyMatrix_JsonRoundTrip(
            ReportFrequency frequency,
            bool isEnabled,
            bool hasEmailTo)
        {
            var reporting = new ReportingConfig
            {
                IsEnabled = isEnabled,
                Frequency = frequency,
                EmailTo = hasEmailTo ? "reports@company.com" : null,
                SendHour = 9
            };

            var plan = new BackupPlan { PlanName = "ReportTest", Reporting = reporting };
            var loaded = RoundTrip(plan);

            loaded.Reporting.IsEnabled.Should().Be(isEnabled);
            loaded.Reporting.Frequency.Should().Be(frequency);
            loaded.Reporting.SendHour.Should().Be(9);

            if (hasEmailTo)
            {
                loaded.Reporting.EmailTo.Should().Be("reports@company.com");
            }
            else
            {
                loaded.Reporting.EmailTo.Should().BeNull();
            }
        }

        /// <summary>
        /// 3 frequency × 2 enabled × 2 emailTo = 12 kombinasyon.
        /// </summary>
        private static IEnumerable<object[]> GetReportingMatrixData()
        {
            var frequencies = new[] { ReportFrequency.Daily, ReportFrequency.Weekly, ReportFrequency.Monthly };
            var bools = new[] { false, true };

            foreach (var freq in frequencies)
                foreach (var enabled in bools)
                    foreach (var emailTo in bools)
                        yield return new object[] { freq, enabled, emailTo };
        }

        // ═══════════════════════════════════════════════════════════════
        // 7. REPORTING SENDHOUR — Edge Cases
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(0, DisplayName = "SendHour_Midnight")]
        [DataRow(1, DisplayName = "SendHour_1AM")]
        [DataRow(8, DisplayName = "SendHour_8AM_Default")]
        [DataRow(12, DisplayName = "SendHour_Noon")]
        [DataRow(23, DisplayName = "SendHour_11PM")]
        public void Reporting_SendHour_AllValidValues(int sendHour)
        {
            var plan = new BackupPlan
            {
                PlanName = "SendHourTest",
                Reporting = new ReportingConfig { IsEnabled = true, SendHour = sendHour }
            };

            var loaded = RoundTrip(plan);
            loaded.Reporting.SendHour.Should().Be(sendHour);
        }

        // ═══════════════════════════════════════════════════════════════
        // 8. FILE BACKUP — Detaylı Kombinasyonlar
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(true, true, true, DisplayName = "FileBackup_Recursive_VSS_Enabled")]
        [DataRow(true, true, false, DisplayName = "FileBackup_Recursive_VSS_Disabled")]
        [DataRow(true, false, true, DisplayName = "FileBackup_Recursive_NoVSS_Enabled")]
        [DataRow(true, false, false, DisplayName = "FileBackup_Recursive_NoVSS_Disabled")]
        [DataRow(false, true, true, DisplayName = "FileBackup_Flat_VSS_Enabled")]
        [DataRow(false, true, false, DisplayName = "FileBackup_Flat_VSS_Disabled")]
        [DataRow(false, false, true, DisplayName = "FileBackup_Flat_NoVSS_Enabled")]
        [DataRow(false, false, false, DisplayName = "FileBackup_Flat_NoVSS_Disabled")]
        public void FileBackup_SourceOptions_JsonRoundTrip(bool recursive, bool useVss, bool isEnabled)
        {
            var plan = new BackupPlan
            {
                PlanName = "FileSourceTest",
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource
                        {
                            SourceName = "TestSource",
                            SourcePath = @"C:\TestPath",
                            Recursive = recursive,
                            UseVss = useVss,
                            IsEnabled = isEnabled,
                            IncludePatterns = new List<string> { "*.dat" },
                            ExcludePatterns = new List<string> { "*.tmp" }
                        }
                    }
                }
            };

            var loaded = RoundTrip(plan);
            var source = loaded.FileBackup.Sources.First();

            source.Recursive.Should().Be(recursive);
            source.UseVss.Should().Be(useVss);
            source.IsEnabled.Should().Be(isEnabled);
            source.IncludePatterns.Should().Contain("*.dat");
            source.ExcludePatterns.Should().Contain("*.tmp");
        }

        [TestMethod]
        public void FileBackup_EmptySources_JsonRoundTrip()
        {
            var plan = new BackupPlan
            {
                PlanName = "EmptyFileBackup",
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>()
                }
            };

            var loaded = RoundTrip(plan);
            loaded.FileBackup.Should().NotBeNull();
            loaded.FileBackup.IsEnabled.Should().BeTrue();
            loaded.FileBackup.Sources.Should().BeEmpty();
        }

        [TestMethod]
        public void FileBackup_NullFileBackup_StaysNull()
        {
            var plan = new BackupPlan { PlanName = "NoFileBackup", FileBackup = null };

            var loaded = RoundTrip(plan);
            loaded.FileBackup.Should().BeNull();
        }

        [TestMethod]
        public void FileBackup_MultipleSourcesMixedSettings_AllPreserved()
        {
            var plan = new BackupPlan
            {
                PlanName = "MultiSource",
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Schedule = "0 0 5 ? * MON-FRI",
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
                            ExcludePatterns = new List<string> { "*.tmp", "~*" }
                        },
                        new FileBackupSource
                        {
                            SourceName = "Old Archive",
                            SourcePath = @"E:\Archive",
                            IsEnabled = false
                        }
                    }
                }
            };

            var loaded = RoundTrip(plan);
            loaded.FileBackup.Sources.Should().HaveCount(3);
            loaded.FileBackup.Schedule.Should().Be("0 0 5 ? * MON-FRI");

            loaded.FileBackup.Sources[0].UseVss.Should().BeTrue();
            loaded.FileBackup.Sources[0].Recursive.Should().BeTrue();
            loaded.FileBackup.Sources[0].IncludePatterns.Should().Contain("*.pst");

            loaded.FileBackup.Sources[1].UseVss.Should().BeFalse();
            loaded.FileBackup.Sources[1].Recursive.Should().BeFalse();
            loaded.FileBackup.Sources[1].ExcludePatterns.Should().Contain("*.tmp");

            loaded.FileBackup.Sources[2].IsEnabled.Should().BeFalse();
        }

        // ═══════════════════════════════════════════════════════════════
        // 9. GFS RETENTION — Tüm Parametre Kombinasyonları
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(1, 1, 1, 0, DisplayName = "GFS_Minimal")]
        [DataRow(7, 4, 12, 2, DisplayName = "GFS_Default")]
        [DataRow(14, 8, 24, 5, DisplayName = "GFS_Extended")]
        [DataRow(30, 12, 36, 10, DisplayName = "GFS_Maximum")]
        [DataRow(0, 0, 0, 0, DisplayName = "GFS_AllZero")]
        public void GfsRetention_ParameterCombinations_JsonRoundTrip(
            int daily, int weekly, int monthly, int yearly)
        {
            var plan = new BackupPlan
            {
                PlanName = "GFS_Test",
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.GFS,
                    GfsKeepDaily = daily,
                    GfsKeepWeekly = weekly,
                    GfsKeepMonthly = monthly,
                    GfsKeepYearly = yearly
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Retention.Type.Should().Be(RetentionPolicyType.GFS);
            loaded.Retention.GfsKeepDaily.Should().Be(daily);
            loaded.Retention.GfsKeepWeekly.Should().Be(weekly);
            loaded.Retention.GfsKeepMonthly.Should().Be(monthly);
            loaded.Retention.GfsKeepYearly.Should().Be(yearly);
        }

        // ═══════════════════════════════════════════════════════════════
        // 10. RETENTION × STRATEGY — Birlikte Çalışma
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DynamicData(nameof(GetRetentionStrategyData), DynamicDataSourceType.Method)]
        public void Retention_Strategy_CrossCombination(
            RetentionPolicyType retType, BackupStrategyType stratType)
        {
            var plan = new BackupPlan
            {
                PlanName = $"RetStrat_{retType}_{stratType}",
                Retention = new RetentionPolicy
                {
                    Type = retType,
                    KeepLastN = 10,
                    DeleteOlderThanDays = 30,
                    GfsKeepDaily = 7,
                    GfsKeepWeekly = 4,
                    GfsKeepMonthly = 12,
                    GfsKeepYearly = 2
                },
                Strategy = new BackupStrategyConfig
                {
                    Type = stratType,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = "0 0 3 ? * MON-SAT",
                    AutoPromoteToFullAfter = 7
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Retention.Type.Should().Be(retType);
            loaded.Strategy.Type.Should().Be(stratType);
        }

        /// <summary>
        /// 4 retention × 3 strategy = 12 kombinasyon.
        /// </summary>
        private static IEnumerable<object[]> GetRetentionStrategyData()
        {
            var retentions = new[] { RetentionPolicyType.KeepLastN, RetentionPolicyType.DeleteOlderThanDays, RetentionPolicyType.Both, RetentionPolicyType.GFS };
            var strategies = new[] { BackupStrategyType.Full, BackupStrategyType.FullPlusDifferential, BackupStrategyType.FullPlusDifferentialPlusIncremental };

            foreach (var r in retentions)
                foreach (var s in strategies)
                    yield return new object[] { r, s };
        }

        // ═══════════════════════════════════════════════════════════════
        // 11. EDGE CASE: Tüm Özellikler Açık (Maximum Config)
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void MaximumConfig_AllFeaturesEnabled_JsonRoundTrip()
        {
            var plan = new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = "MaxConfig_AllFeatures",
                IsEnabled = true,
                PasswordHash = PlanPasswordHelper.HashPassword("maxPw"),
                VerifyAfterBackup = true,
                SchemaVersion = 2,
                LocalPath = @"D:\Backups\Max",
                SqlConnection = new SqlConnectionInfo
                {
                    Server = @"server\instance",
                    AuthMode = SqlAuthMode.SqlAuthentication,
                    Username = "sa",
                    Password = PasswordProtector.Protect("sqlPw"),
                    ConnectionTimeoutSeconds = 60,
                    TrustServerCertificate = true
                },
                Databases = new List<string> { "DB1", "DB2", "DB3", "DB4", "DB5" },
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
                    ArchivePassword = PasswordProtector.Protect("archMax")
                },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.GFS,
                    KeepLastN = 20,
                    DeleteOlderThanDays = 60,
                    GfsKeepDaily = 14,
                    GfsKeepWeekly = 8,
                    GfsKeepMonthly = 24,
                    GfsKeepYearly = 5
                },
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "FTP", Host = "ftp.test.com", Port = 21, BandwidthLimitMbps = 10 },
                    new CloudTargetConfig { Type = CloudProviderType.Ftps, IsEnabled = true, DisplayName = "FTPS", Host = "ftps.test.com", Port = 990, FtpsSkipCertificateValidation = true },
                    new CloudTargetConfig { Type = CloudProviderType.Sftp, IsEnabled = true, DisplayName = "SFTP", Host = "sftp.test.com", SftpHostFingerprint = "SHA256:abc" },
                    new CloudTargetConfig { Type = CloudProviderType.GoogleDrivePersonal, IsEnabled = true, DisplayName = "GDrive", OAuthClientId = "cid" },
                    new CloudTargetConfig { Type = CloudProviderType.UncPath, IsEnabled = true, DisplayName = "UNC", LocalOrUncPath = @"\\nas\backups" }
                },
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Schedule = "0 0 4 ? * MON-FRI",
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource { SourceName = "PST", SourcePath = @"C:\Outlook", UseVss = true, Recursive = true, IsEnabled = true, IncludePatterns = new List<string> { "*.pst" } },
                        new FileBackupSource { SourceName = "Data", SourcePath = @"D:\Data", UseVss = false, Recursive = true, IsEnabled = true, ExcludePatterns = new List<string> { "*.tmp" } }
                    }
                },
                Notifications = new NotificationConfig
                {
                    OnSuccess = true,
                    OnFailure = true,
                    EmailEnabled = true,
                    ToastEnabled = true,
                    SmtpProfileId = "smtp-main",
                    EmailTo = "admin@company.com"
                },
                Reporting = new ReportingConfig
                {
                    IsEnabled = true,
                    Frequency = ReportFrequency.Daily,
                    EmailTo = "reports@company.com",
                    SendHour = 7
                }
            };

            var loaded = RoundTrip(plan);

            // Every feature should survive
            loaded.HasPlanPassword.Should().BeTrue();
            loaded.HasCloudTargets.Should().BeTrue();
            loaded.VerifyAfterBackup.Should().BeTrue();
            loaded.Databases.Should().HaveCount(5);
            loaded.Strategy.Type.Should().Be(BackupStrategyType.FullPlusDifferentialPlusIncremental);
            loaded.Compression.Algorithm.Should().Be(CompressionAlgorithm.Lzma2);
            loaded.Compression.Level.Should().Be(CompressionLevel.Ultra);
            PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archMax");
            loaded.Retention.Type.Should().Be(RetentionPolicyType.GFS);
            loaded.Retention.GfsKeepYearly.Should().Be(5);
            loaded.CloudTargets.Should().HaveCount(5);
            loaded.FileBackup.IsEnabled.Should().BeTrue();
            loaded.FileBackup.Sources.Should().HaveCount(2);
            loaded.Notifications.SmtpProfileId.Should().Be("smtp-main");
            loaded.Notifications.ToastEnabled.Should().BeTrue();
            loaded.Reporting.IsEnabled.Should().BeTrue();
            loaded.Reporting.Frequency.Should().Be(ReportFrequency.Daily);
            loaded.Reporting.SendHour.Should().Be(7);
            loaded.SqlConnection.AuthMode.Should().Be(SqlAuthMode.SqlAuthentication);
            loaded.SchemaVersion.Should().Be(2);
        }

        // ═══════════════════════════════════════════════════════════════
        // 12. EDGE CASE: Tüm Özellikler Kapalı (Minimum Config)
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void MinimumConfig_AllFeaturesDisabled_JsonRoundTrip()
        {
            var plan = new BackupPlan
            {
                PlanName = "MinConfig",
                IsEnabled = false,
                PasswordHash = null,
                VerifyAfterBackup = false,
                SqlConnection = new SqlConnectionInfo
                {
                    Server = "localhost",
                    AuthMode = SqlAuthMode.Windows
                },
                Databases = new List<string>(),
                Strategy = new BackupStrategyConfig { Type = BackupStrategyType.Full },
                Compression = new CompressionConfig
                {
                    Algorithm = CompressionAlgorithm.Deflate,
                    Level = CompressionLevel.None,
                    ArchivePassword = null
                },
                Retention = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 1 },
                CloudTargets = new List<CloudTargetConfig>(),
                FileBackup = null,
                Notifications = new NotificationConfig
                {
                    OnSuccess = false,
                    OnFailure = false,
                    EmailEnabled = false,
                    ToastEnabled = false
                },
                Reporting = new ReportingConfig { IsEnabled = false }
            };

            var loaded = RoundTrip(plan);

            loaded.IsEnabled.Should().BeFalse();
            loaded.HasPlanPassword.Should().BeFalse();
            loaded.HasCloudTargets.Should().BeFalse();
            loaded.VerifyAfterBackup.Should().BeFalse();
            loaded.Databases.Should().BeEmpty();
            loaded.FileBackup.Should().BeNull();
            loaded.Notifications.OnSuccess.Should().BeFalse();
            loaded.Notifications.OnFailure.Should().BeFalse();
            loaded.Notifications.EmailEnabled.Should().BeFalse();
            loaded.Notifications.ToastEnabled.Should().BeFalse();
            loaded.Reporting.IsEnabled.Should().BeFalse();
            loaded.Compression.Level.Should().Be(CompressionLevel.None);
            loaded.Compression.ArchivePassword.Should().BeNull();
        }

        // ═══════════════════════════════════════════════════════════════
        // 13. SQL CONNECTION — Timeout ve TrustCert Kombinasyonları
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(SqlAuthMode.Windows, 15, true, DisplayName = "Win_15s_TrustCert")]
        [DataRow(SqlAuthMode.Windows, 30, false, DisplayName = "Win_30s_NoTrustCert")]
        [DataRow(SqlAuthMode.Windows, 60, true, DisplayName = "Win_60s_TrustCert")]
        [DataRow(SqlAuthMode.SqlAuthentication, 15, true, DisplayName = "Sql_15s_TrustCert")]
        [DataRow(SqlAuthMode.SqlAuthentication, 30, false, DisplayName = "Sql_30s_NoTrustCert")]
        [DataRow(SqlAuthMode.SqlAuthentication, 60, true, DisplayName = "Sql_60s_TrustCert")]
        public void SqlConnection_AuthTimeout_JsonRoundTrip(
            SqlAuthMode authMode, int timeout, bool trustCert)
        {
            var plan = new BackupPlan
            {
                PlanName = "SqlTest",
                SqlConnection = new SqlConnectionInfo
                {
                    Server = "testserver",
                    AuthMode = authMode,
                    Username = authMode == SqlAuthMode.SqlAuthentication ? "sa" : null,
                    Password = authMode == SqlAuthMode.SqlAuthentication ? PasswordProtector.Protect("pw") : null,
                    ConnectionTimeoutSeconds = timeout,
                    TrustServerCertificate = trustCert
                }
            };

            var loaded = RoundTrip(plan);
            loaded.SqlConnection.AuthMode.Should().Be(authMode);
            loaded.SqlConnection.ConnectionTimeoutSeconds.Should().Be(timeout);
            loaded.SqlConnection.TrustServerCertificate.Should().Be(trustCert);
        }

        // ═══════════════════════════════════════════════════════════════
        // 14. STRATEGY SCHEDULES — Zamanlama Tutarlılığı
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void Strategy_Full_OnlyFullScheduleNeeded()
        {
            var plan = new BackupPlan
            {
                PlanName = "FullOnly",
                Strategy = new BackupStrategyConfig
                {
                    Type = BackupStrategyType.Full,
                    FullSchedule = "0 0 2 ? * SUN"
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Strategy.Type.Should().Be(BackupStrategyType.Full);
            loaded.Strategy.FullSchedule.Should().Be("0 0 2 ? * SUN");
        }

        [TestMethod]
        public void Strategy_FullPlusDiff_TwoSchedules()
        {
            var plan = new BackupPlan
            {
                PlanName = "FullDiff",
                Strategy = new BackupStrategyConfig
                {
                    Type = BackupStrategyType.FullPlusDifferential,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = "0 0 3 ? * MON-SAT",
                    AutoPromoteToFullAfter = 7
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Strategy.Type.Should().Be(BackupStrategyType.FullPlusDifferential);
            loaded.Strategy.FullSchedule.Should().NotBeNullOrEmpty();
            loaded.Strategy.DifferentialSchedule.Should().NotBeNullOrEmpty();
            loaded.Strategy.AutoPromoteToFullAfter.Should().Be(7);
        }

        [TestMethod]
        public void Strategy_FullDiffIncr_ThreeSchedules()
        {
            var plan = new BackupPlan
            {
                PlanName = "FullDiffIncr",
                Strategy = new BackupStrategyConfig
                {
                    Type = BackupStrategyType.FullPlusDifferentialPlusIncremental,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = "0 0 3 ? * WED",
                    IncrementalSchedule = "0 0 */4 ? * MON-SAT",
                    AutoPromoteToFullAfter = 5
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Strategy.Type.Should().Be(BackupStrategyType.FullPlusDifferentialPlusIncremental);
            loaded.Strategy.FullSchedule.Should().NotBeNullOrEmpty();
            loaded.Strategy.DifferentialSchedule.Should().NotBeNullOrEmpty();
            loaded.Strategy.IncrementalSchedule.Should().NotBeNullOrEmpty();
            loaded.Strategy.AutoPromoteToFullAfter.Should().Be(5);
        }

        [TestMethod]
        [DataRow(1, DisplayName = "AutoPromote_1")]
        [DataRow(3, DisplayName = "AutoPromote_3")]
        [DataRow(7, DisplayName = "AutoPromote_7")]
        [DataRow(14, DisplayName = "AutoPromote_14")]
        [DataRow(30, DisplayName = "AutoPromote_30")]
        public void Strategy_AutoPromoteToFull_AllValues(int autoPromote)
        {
            var plan = new BackupPlan
            {
                PlanName = "AutoPromote",
                Strategy = new BackupStrategyConfig
                {
                    Type = BackupStrategyType.FullPlusDifferential,
                    AutoPromoteToFullAfter = autoPromote
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Strategy.AutoPromoteToFullAfter.Should().Be(autoPromote);
        }

        // ═══════════════════════════════════════════════════════════════
        // 15. COMPRESSION × PASSWORD × CLOUD — Üçlü Kombinasyon
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(CompressionAlgorithm.Lzma2, CompressionLevel.Ultra, true, true, DisplayName = "Lzma2_Ultra_ArchPw_PlanPw")]
        [DataRow(CompressionAlgorithm.Lzma2, CompressionLevel.Ultra, true, false, DisplayName = "Lzma2_Ultra_ArchPw_NoPlanPw")]
        [DataRow(CompressionAlgorithm.Lzma2, CompressionLevel.Ultra, false, true, DisplayName = "Lzma2_Ultra_NoArchPw_PlanPw")]
        [DataRow(CompressionAlgorithm.Lzma2, CompressionLevel.Ultra, false, false, DisplayName = "Lzma2_Ultra_NoArchPw_NoPlanPw")]
        [DataRow(CompressionAlgorithm.BZip2, CompressionLevel.Fast, true, true, DisplayName = "BZip2_Fast_ArchPw_PlanPw")]
        [DataRow(CompressionAlgorithm.BZip2, CompressionLevel.Fast, false, false, DisplayName = "BZip2_Fast_NoArchPw_NoPlanPw")]
        [DataRow(CompressionAlgorithm.Deflate, CompressionLevel.None, true, false, DisplayName = "Deflate_None_ArchPw_NoPlanPw")]
        [DataRow(CompressionAlgorithm.Lzma, CompressionLevel.Maximum, false, true, DisplayName = "Lzma_Max_NoArchPw_PlanPw")]
        public void Compression_Password_Cross(
            CompressionAlgorithm alg, CompressionLevel level,
            bool withArchivePw, bool withPlanPw)
        {
            var plan = new BackupPlan
            {
                PlanName = "CompPwTest",
                Compression = new CompressionConfig
                {
                    Algorithm = alg,
                    Level = level,
                    ArchivePassword = withArchivePw ? PasswordProtector.Protect("archCross") : null
                },
                PasswordHash = withPlanPw ? PlanPasswordHelper.HashPassword("planCross") : null,
                CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig { Type = CloudProviderType.Ftp, IsEnabled = true, DisplayName = "Cloud" }
                }
            };

            var loaded = RoundTrip(plan);

            loaded.Compression.Algorithm.Should().Be(alg);
            loaded.Compression.Level.Should().Be(level);
            loaded.HasPlanPassword.Should().Be(withPlanPw);
            loaded.HasCloudTargets.Should().BeTrue();

            if (withArchivePw)
            {
                PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archCross");
            }
            else
            {
                loaded.Compression.ArchivePassword.Should().BeNull();
            }

            if (withPlanPw)
            {
                PlanPasswordHelper.VerifyPassword("planCross", loaded.PasswordHash).Should().BeTrue();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 16. NOTIFICATION + REPORTING — Birlikte Çalışma
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(true, true, DisplayName = "Notif_On_Report_On")]
        [DataRow(true, false, DisplayName = "Notif_On_Report_Off")]
        [DataRow(false, true, DisplayName = "Notif_Off_Report_On")]
        [DataRow(false, false, DisplayName = "Notif_Off_Report_Off")]
        public void Notification_Reporting_Independence(bool notifEnabled, bool reportEnabled)
        {
            var plan = new BackupPlan
            {
                PlanName = "NotifReportTest",
                Notifications = new NotificationConfig
                {
                    EmailEnabled = notifEnabled,
                    OnSuccess = notifEnabled,
                    OnFailure = true,
                    ToastEnabled = true
                },
                Reporting = new ReportingConfig
                {
                    IsEnabled = reportEnabled,
                    Frequency = ReportFrequency.Monthly,
                    SendHour = 10,
                    EmailTo = reportEnabled ? "report@test.com" : null
                }
            };

            var loaded = RoundTrip(plan);
            loaded.Notifications.EmailEnabled.Should().Be(notifEnabled);
            loaded.Notifications.OnSuccess.Should().Be(notifEnabled);
            loaded.Reporting.IsEnabled.Should().Be(reportEnabled);
            loaded.Reporting.Frequency.Should().Be(ReportFrequency.Monthly);
        }

        // ═══════════════════════════════════════════════════════════════
        // 17. SCHEMA VERSION — Farklı Versiyonlar
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(1, DisplayName = "Schema_V1")]
        [DataRow(2, DisplayName = "Schema_V2")]
        [DataRow(3, DisplayName = "Schema_V3_Future")]
        public void SchemaVersion_AllValues_Preserved(int schemaVersion)
        {
            var plan = new BackupPlan
            {
                PlanName = "SchemaTest",
                SchemaVersion = schemaVersion
            };

            var loaded = RoundTrip(plan);
            loaded.SchemaVersion.Should().Be(schemaVersion);
        }

        // ═══════════════════════════════════════════════════════════════
        // 18. DATABASE LIST — Farklı Boyutlar
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void Databases_EmptyList_Preserved()
        {
            var plan = new BackupPlan { PlanName = "NoDB", Databases = new List<string>() };
            RoundTrip(plan).Databases.Should().BeEmpty();
        }

        [TestMethod]
        public void Databases_SingleDB_Preserved()
        {
            var plan = new BackupPlan { PlanName = "OneDB", Databases = new List<string> { "master" } };
            RoundTrip(plan).Databases.Should().ContainSingle().Which.Should().Be("master");
        }

        [TestMethod]
        public void Databases_ManyDBs_AllPreserved()
        {
            var dbs = Enumerable.Range(1, 20).Select(i => $"DB_{i:D3}").ToList();
            var plan = new BackupPlan { PlanName = "ManyDB", Databases = dbs };
            RoundTrip(plan).Databases.Should().HaveCount(20).And.BeEquivalentTo(dbs);
        }

        [TestMethod]
        public void Databases_SpecialNames_Preserved()
        {
            var dbs = new List<string>
            {
                "master", "tempdb", "msdb", "model",
                "DB-With-Dashes", "DB_With_Underscores",
                "DB With Spaces", "DB.With.Dots",
                "Muhasebe2025", "UPPERCASE_DB", "lowercase_db"
            };

            var plan = new BackupPlan { PlanName = "SpecialDB", Databases = dbs };
            RoundTrip(plan).Databases.Should().BeEquivalentTo(dbs);
        }

        // ═══════════════════════════════════════════════════════════════
        // 19. DUAL PASSWORD SYSTEM: PlanPw × ArchivePw × SqlPw
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        [DataRow(true, true, true, DisplayName = "AllPasswords_Set")]
        [DataRow(true, true, false, DisplayName = "PlanPw_ArchivePw_NoSqlPw")]
        [DataRow(true, false, true, DisplayName = "PlanPw_NoArchivePw_SqlPw")]
        [DataRow(false, true, true, DisplayName = "NoPlanPw_ArchivePw_SqlPw")]
        [DataRow(false, false, false, DisplayName = "NoPasswords")]
        [DataRow(true, false, false, DisplayName = "PlanPw_Only")]
        [DataRow(false, true, false, DisplayName = "ArchivePw_Only")]
        [DataRow(false, false, true, DisplayName = "SqlPw_Only")]
        public void TriplePasswordSystem_AllCombinations(
            bool hasPlanPw, bool hasArchivePw, bool hasSqlPw)
        {
            var plan = new BackupPlan
            {
                PlanName = "TriplePw",
                PasswordHash = hasPlanPw ? PlanPasswordHelper.HashPassword("planPw") : null,
                Compression = new CompressionConfig
                {
                    ArchivePassword = hasArchivePw ? PasswordProtector.Protect("archPw") : null
                },
                SqlConnection = new SqlConnectionInfo
                {
                    Server = "localhost",
                    AuthMode = hasSqlPw ? SqlAuthMode.SqlAuthentication : SqlAuthMode.Windows,
                    Username = hasSqlPw ? "sa" : null,
                    Password = hasSqlPw ? PasswordProtector.Protect("sqlPw") : null
                }
            };

            var loaded = RoundTrip(plan);

            loaded.HasPlanPassword.Should().Be(hasPlanPw);

            if (hasPlanPw)
                PlanPasswordHelper.VerifyPassword("planPw", loaded.PasswordHash).Should().BeTrue();

            if (hasArchivePw)
                PasswordProtector.Unprotect(loaded.Compression.ArchivePassword).Should().Be("archPw");
            else
                loaded.Compression.ArchivePassword.Should().BeNull();

            if (hasSqlPw)
            {
                loaded.SqlConnection.AuthMode.Should().Be(SqlAuthMode.SqlAuthentication);
                PasswordProtector.Unprotect(loaded.SqlConnection.Password).Should().Be("sqlPw");
            }
            else
            {
                loaded.SqlConnection.AuthMode.Should().Be(SqlAuthMode.Windows);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 20. BACKWARD COMPATIBILITY — Eski JSON Formatı
        // ═══════════════════════════════════════════════════════════════

        [TestMethod]
        public void BackwardCompat_V073_NoPasswordHash_NoFileBackup_NoReporting()
        {
            // v0.72 öncesi JSON: passwordHash, fileBackup, reporting alanları yok
            string json = @"{
                ""planId"": ""old-plan-001"",
                ""planName"": ""Legacy Plan"",
                ""isEnabled"": true,
                ""backupMode"": 0,
                ""sqlConnection"": { ""server"": ""localhost"", ""authMode"": 0 },
                ""databases"": [""TestDB""],
                ""strategy"": { ""type"": 0, ""fullSchedule"": ""0 0 2 ? * SUN"" },
                ""compression"": { ""algorithm"": 0, ""level"": 4 },
                ""retention"": { ""type"": 0, ""keepLastN"": 10 },
                ""localPath"": ""D:\\Backups"",
                ""cloudTargets"": [],
                ""notifications"": { ""onSuccess"": true, ""onFailure"": true, ""toastEnabled"": true },
                ""verifyAfterBackup"": true,
                ""schemaVersion"": 1
            }";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            plan.Should().NotBeNull();
            plan.PlanId.Should().Be("old-plan-001");
            plan.HasPlanPassword.Should().BeFalse();
            plan.HasCloudTargets.Should().BeFalse();
            plan.FileBackup.Should().BeNull();
            plan.VerifyAfterBackup.Should().BeTrue();
            plan.Strategy.Type.Should().Be(BackupStrategyType.Full);
            plan.Compression.Algorithm.Should().Be(CompressionAlgorithm.Lzma2);
            plan.Compression.Level.Should().Be(CompressionLevel.Ultra);
        }

        [TestMethod]
        public void BackwardCompat_NewFieldsAddedToOldPlan_DefaultsApplied()
        {
            // Minimum JSON — sadece zorunlu alanlar
            string json = @"{ ""planId"": ""min"", ""planName"": ""Min"" }";

            var plan = JsonConvert.DeserializeObject<BackupPlan>(json);

            // New fields should use defaults
            plan.HasPlanPassword.Should().BeFalse();
            plan.VerifyAfterBackup.Should().BeTrue();
            plan.SchemaVersion.Should().Be(1);
            plan.Reporting.Should().NotBeNull();
            plan.Reporting.IsEnabled.Should().BeFalse();
            plan.Notifications.Should().NotBeNull();
            plan.Notifications.ToastEnabled.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static BackupPlan RoundTrip(BackupPlan plan)
        {
            string json = JsonConvert.SerializeObject(plan, Formatting.Indented);
            return JsonConvert.DeserializeObject<BackupPlan>(json);
        }

        private static BackupPlan CreatePlanWithSingleCloud(CloudTargetConfig target)
        {
            return new BackupPlan
            {
                PlanName = $"CloudTest_{target.Type}",
                CloudTargets = new List<CloudTargetConfig> { target }
            };
        }

        private static BackupPlan BuildFullMatrixPlan(
            BackupStrategyType strategy,
            RetentionPolicyType retention,
            bool withCloud,
            bool withPassword,
            bool withFileBackup,
            bool withArchivePassword,
            bool verifyAfterBackup)
        {
            var plan = new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = $"MM_{strategy}_{retention}_{(withCloud ? "C" : "c")}_{(withPassword ? "P" : "p")}_{(withFileBackup ? "F" : "f")}_{(withArchivePassword ? "A" : "a")}_{(verifyAfterBackup ? "V" : "v")}",
                IsEnabled = true,
                VerifyAfterBackup = verifyAfterBackup,
                SqlConnection = new SqlConnectionInfo { Server = "localhost", AuthMode = SqlAuthMode.Windows },
                Databases = new List<string> { "TestDB" },
                Strategy = new BackupStrategyConfig
                {
                    Type = strategy,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = strategy != BackupStrategyType.Full ? "0 0 3 ? * MON-SAT" : null,
                    IncrementalSchedule = strategy == BackupStrategyType.FullPlusDifferentialPlusIncremental ? "0 0 */4 ? * MON-SAT" : null,
                    AutoPromoteToFullAfter = 7
                },
                Compression = new CompressionConfig
                {
                    Algorithm = CompressionAlgorithm.Lzma2,
                    Level = CompressionLevel.Normal,
                    ArchivePassword = withArchivePassword ? PasswordProtector.Protect("archPw123") : null
                },
                Retention = BuildRetention(retention),
                LocalPath = @"C:\TestBackups"
            };

            if (withCloud)
            {
                plan.CloudTargets = new List<CloudTargetConfig>
                {
                    new CloudTargetConfig
                    {
                        Type = CloudProviderType.Ftp,
                        IsEnabled = true,
                        DisplayName = "Test FTP",
                        Host = "ftp.test.com",
                        Port = 21,
                        RemoteFolderPath = "/backups"
                    }
                };
            }
            else
            {
                plan.CloudTargets = new List<CloudTargetConfig>();
            }

            if (withPassword)
            {
                plan.PasswordHash = PlanPasswordHelper.HashPassword("megaMatrixPw");
            }

            if (withFileBackup)
            {
                plan.FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>
                    {
                        new FileBackupSource
                        {
                            SourceName = "TestSource",
                            SourcePath = @"C:\TestSource",
                            IsEnabled = true,
                            Recursive = true,
                            UseVss = false
                        }
                    }
                };
            }

            return plan;
        }

        private static RetentionPolicy BuildRetention(RetentionPolicyType type)
        {
            return type switch
            {
                RetentionPolicyType.KeepLastN => new RetentionPolicy { Type = type, KeepLastN = 10 },
                RetentionPolicyType.DeleteOlderThanDays => new RetentionPolicy { Type = type, DeleteOlderThanDays = 30 },
                RetentionPolicyType.Both => new RetentionPolicy { Type = type, KeepLastN = 10, DeleteOlderThanDays = 30 },
                RetentionPolicyType.GFS => new RetentionPolicy
                {
                    Type = type,
                    GfsKeepDaily = 7,
                    GfsKeepWeekly = 4,
                    GfsKeepMonthly = 12,
                    GfsKeepYearly = 2
                },
                _ => new RetentionPolicy { Type = type }
            };
        }
    }
}
