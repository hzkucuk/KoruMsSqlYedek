using System;
using System.Collections.Generic;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Tests.Helpers
{
    /// <summary>
    /// Test verisi oluşturmak için yardımcı fabrika.
    /// Tüm test sınıfları tarafından tekrar kullanılabilir.
    /// </summary>
    internal static class TestDataFactory
    {
        /// <summary>
        /// Varsayılan ayarlarla geçerli bir BackupPlan oluşturur.
        /// </summary>
        public static BackupPlan CreateValidPlan(
            string planName = "Test Plan",
            BackupStrategyType strategyType = BackupStrategyType.Full)
        {
            return new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = planName,
                IsEnabled = true,
                SqlConnection = CreateSqlConnection(),
                Databases = new List<string> { "TestDB1", "TestDB2" },
                Strategy = new BackupStrategyConfig
                {
                    Type = strategyType,
                    FullSchedule = "0 0 2 ? * SUN",
                    DifferentialSchedule = "0 0 3 ? * MON-SAT",
                    AutoPromoteToFullAfter = 7
                },
                Compression = new CompressionConfig
                {
                    Algorithm = CompressionAlgorithm.Lzma2,
                    Level = CompressionLevel.Normal
                },
                Retention = new RetentionPolicy
                {
                    Type = RetentionPolicyType.KeepLastN,
                    KeepLastN = 10,
                    DeleteOlderThanDays = 30
                },
                LocalPath = @"C:\TestBackups",
                VerifyAfterBackup = true,
                Notifications = CreateNotificationConfig()
            };
        }

        /// <summary>
        /// Windows Authentication ile SQL bağlantı bilgisi.
        /// </summary>
        public static SqlConnectionInfo CreateSqlConnection(
            string server = "localhost",
            SqlAuthMode authMode = SqlAuthMode.Windows)
        {
            return new SqlConnectionInfo
            {
                Server = server,
                AuthMode = authMode,
                ConnectionTimeoutSeconds = 30
            };
        }

        /// <summary>
        /// Bildirim yapılandırması.
        /// </summary>
        public static NotificationConfig CreateNotificationConfig(
            bool onSuccess = true,
            bool onFailure = true)
        {
            return new NotificationConfig
            {
                OnSuccess = onSuccess,
                OnFailure = onFailure,
                EmailEnabled = true,
                EmailTo = "test@example.com",
                SmtpServer = "smtp.test.com",
                SmtpPort = 587,
                SmtpUseSsl = true
            };
        }

        /// <summary>
        /// Başarılı bir BackupResult oluşturur.
        /// </summary>
        public static BackupResult CreateSuccessResult(
            string planId = null,
            string databaseName = "TestDB1",
            SqlBackupType backupType = SqlBackupType.Full)
        {
            return new BackupResult
            {
                PlanId = planId ?? Guid.NewGuid().ToString(),
                PlanName = "Test Plan",
                DatabaseName = databaseName,
                BackupType = backupType,
                Status = BackupResultStatus.Success,
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow,
                BackupFilePath = @"C:\TestBackups\TestDB1_Full_20250101_020000.bak",
                FileSizeBytes = 1024 * 1024 * 100, // 100 MB
                CompressedSizeBytes = 1024 * 1024 * 30, // 30 MB
                CompressedFilePath = @"C:\TestBackups\TestDB1_Full_20250101_020000.7z",
                VerifyResult = true
            };
        }

        /// <summary>
        /// Başarısız bir BackupResult oluşturur.
        /// </summary>
        public static BackupResult CreateFailedResult(
            string planId = null,
            string errorMessage = "Test error")
        {
            return new BackupResult
            {
                PlanId = planId ?? Guid.NewGuid().ToString(),
                PlanName = "Test Plan",
                DatabaseName = "TestDB1",
                BackupType = SqlBackupType.Full,
                Status = BackupResultStatus.Failed,
                StartedAt = DateTime.UtcNow.AddMinutes(-1),
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Aktif bulut hedefleri ile plan oluşturur.
        /// </summary>
        public static BackupPlan CreatePlanWithCloudTargets()
        {
            var plan = CreateValidPlan();
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
                },
                new CloudTargetConfig
                {
                    Type = CloudProviderType.UncPath,
                    IsEnabled = true,
                    DisplayName = "UNC Backup",
                    LocalOrUncPath = @"\\server\share\backups"
                }
            };
            return plan;
        }

        /// <summary>
        /// Dosya yedekleme yapılandırması ile plan oluşturur.
        /// </summary>
        public static BackupPlan CreatePlanWithFileBackup()
        {
            var plan = CreateValidPlan();
            plan.FileBackup = new FileBackupConfig
            {
                IsEnabled = true,
                Sources = new List<FileBackupSource>
                {
                    new FileBackupSource
                    {
                        SourceName = "Outlook PST",
                        SourcePath = @"C:\Users\Test\Outlook",
                        Recursive = true,
                        IncludePatterns = new List<string> { "*.pst", "*.ost" },
                        UseVss = true,
                        IsEnabled = true
                    }
                }
            };
            return plan;
        }

        /// <summary>
        /// Birden fazla kaynak ile dosya yedekleme planı oluşturur.
        /// </summary>
        public static BackupPlan CreatePlanWithMultipleFileBackupSources()
        {
            var plan = CreateValidPlan();
            plan.FileBackup = new FileBackupConfig
            {
                IsEnabled = true,
                Sources = new List<FileBackupSource>
                {
                    new FileBackupSource
                    {
                        SourceName = "Outlook PST",
                        SourcePath = @"C:\Users\Test\Outlook",
                        Recursive = true,
                        IncludePatterns = new List<string> { "*.pst", "*.ost" },
                        UseVss = true,
                        IsEnabled = true
                    },
                    new FileBackupSource
                    {
                        SourceName = "Muhasebe Dosyaları",
                        SourcePath = @"C:\Muhasebe",
                        Recursive = true,
                        ExcludePatterns = new List<string> { "*.tmp", "~*" },
                        UseVss = false,
                        IsEnabled = true
                    },
                    new FileBackupSource
                    {
                        SourceName = "Devre Dışı Kaynak",
                        SourcePath = @"C:\Disabled",
                        IsEnabled = false
                    }
                }
            };
            return plan;
        }

        /// <summary>
        /// Tek bir FileBackupSource oluşturur.
        /// </summary>
        public static FileBackupSource CreateFileBackupSource(
            string sourceName = "Test Source",
            string sourcePath = @"C:\TestSource",
            bool useVss = true,
            bool isEnabled = true)
        {
            return new FileBackupSource
            {
                SourceName = sourceName,
                SourcePath = sourcePath,
                Recursive = true,
                UseVss = useVss,
                IsEnabled = isEnabled,
                IncludePatterns = new List<string>(),
                ExcludePatterns = new List<string>()
            };
        }

        /// <summary>
        /// Başarılı bir FileBackupResult oluşturur.
        /// </summary>
        public static FileBackupResult CreateSuccessFileBackupResult(
            string sourceName = "Test Source",
            int filesCopied = 5)
        {
            return new FileBackupResult
            {
                PlanId = Guid.NewGuid().ToString(),
                SourceName = sourceName,
                SourcePath = @"C:\TestSource",
                Status = BackupResultStatus.Success,
                StartedAt = DateTime.UtcNow.AddMinutes(-2),
                CompletedAt = DateTime.UtcNow,
                FilesCopied = filesCopied,
                TotalSizeBytes = 1024 * 1024 * 50,
                DestinationPath = @"C:\TestBackups\Files\TestSource",
                UsedVss = true
            };
        }

        /// <summary>
        /// Başarılı bulut upload sonuçları listesi.
        /// </summary>
        public static List<CloudUploadResult> CreateCloudUploadResults(bool allSuccess = true)
        {
            return new List<CloudUploadResult>
            {
                new CloudUploadResult
                {
                    ProviderType = CloudProviderType.Ftp,
                    DisplayName = "Test FTP",
                    IsSuccess = true,
                    RemoteFilePath = "/backups/test.7z",
                    UploadedAt = DateTime.UtcNow
                },
                new CloudUploadResult
                {
                    ProviderType = CloudProviderType.UncPath,
                    DisplayName = "UNC Backup",
                    IsSuccess = allSuccess,
                    RemoteFilePath = @"\\server\share\backups\test.7z",
                    UploadedAt = allSuccess ? DateTime.UtcNow : (DateTime?)null,
                    ErrorMessage = allSuccess ? null : "Ağ bağlantısı hatası"
                }
            };
        }

        /// <summary>
        /// Test için SmtpProfile oluşturur.
        /// </summary>
        public static SmtpProfile CreateSmtpProfile(
            string id = null,
            string host = "smtp.test.com",
            int port = 587,
            string displayName = "Test Profil",
            string recipientEmails = "admin@test.com",
            string senderEmail = "noreply@test.com")
        {
            return new SmtpProfile
            {
                Id = id ?? Guid.NewGuid().ToString(),
                DisplayName = displayName,
                Host = host,
                Port = port,
                UseSsl = true,
                Username = "user@test.com",
                SenderEmail = senderEmail,
                SenderDisplayName = "KoruMsSqlYedek Test",
                RecipientEmails = recipientEmails
            };
        }

        /// <summary>
        /// SmtpProfiles listesi içeren AppSettings oluşturur.
        /// </summary>
        public static AppSettings CreateAppSettingsWithProfile(SmtpProfile profile = null)
        {
            var settings = new AppSettings();
            settings.SmtpProfiles.Add(profile ?? CreateSmtpProfile());
            return settings;
        }

        /// <summary>
        /// SmtpProfileId içeren NotificationConfig oluşturur.
        /// </summary>
        public static NotificationConfig CreateNotificationConfigWithProfile(
            string profileId,
            bool onSuccess = true,
            bool onFailure = true)
        {
            return new NotificationConfig
            {
                OnSuccess = onSuccess,
                OnFailure = onFailure,
                EmailEnabled = true,
                SmtpProfileId = profileId
            };
        }
    }
}
