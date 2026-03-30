using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class AppSettingsManagerTests
    {
        private static readonly string SettingsFilePath = Path.Combine(
            PathHelper.ConfigDirectory, "appsettings.json");

        private string _backupContent;
        private bool _hadExistingFile;
        private AppSettingsManager _manager;

        [TestInitialize]
        public void Setup()
        {
            // Mevcut ayar dosyasını yedekle
            _hadExistingFile = File.Exists(SettingsFilePath);
            if (_hadExistingFile)
            {
                _backupContent = File.ReadAllText(SettingsFilePath);
            }

            // Test için temiz başlangıç
            if (File.Exists(SettingsFilePath))
                File.Delete(SettingsFilePath);

            _manager = new AppSettingsManager();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Orijinal dosyayı geri yükle
            if (_hadExistingFile)
            {
                File.WriteAllText(SettingsFilePath, _backupContent);
            }
            else if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
        }

        [TestMethod]
        public void Load_WhenFileNotExists_ReturnsDefaults()
        {
            // Arrange — dosya yok (Setup'ta silindi)

            // Act
            var settings = _manager.Load();

            // Assert
            settings.Should().NotBeNull();
            settings.Language.Should().Be("tr-TR");
            settings.StartWithWindows.Should().BeTrue();
            settings.MinimizeToTray.Should().BeTrue();
            settings.LogRetentionDays.Should().Be(30);
            settings.HistoryRetentionDays.Should().Be(90);
            settings.SchemaVersion.Should().Be(1);
        }

        [TestMethod]
        public void Load_WhenFileNotExists_CreatesDefaultFile()
        {
            // Arrange — dosya yok

            // Act
            _manager.Load();

            // Assert — varsayılan dosya oluşturulmuş olmalı
            File.Exists(SettingsFilePath).Should().BeTrue();
        }

        [TestMethod]
        public void Save_WritesJsonToFile()
        {
            // Arrange
            var settings = new AppSettings
            {
                Language = "en-US",
                DefaultBackupPath = @"E:\TestBackups",
                LogRetentionDays = 7
            };

            // Act
            _manager.Save(settings);

            // Assert
            File.Exists(SettingsFilePath).Should().BeTrue();
            string json = File.ReadAllText(SettingsFilePath);
            json.Should().Contain("en-US");
            json.Should().Contain("E:\\\\TestBackups");
        }

        [TestMethod]
        public void Save_ThenLoad_RoundtripPreservesValues()
        {
            // Arrange
            var original = new AppSettings
            {
                Language = "en-US",
                StartWithWindows = false,
                MinimizeToTray = false,
                DefaultBackupPath = @"X:\Backups",
                LogRetentionDays = 14,
                HistoryRetentionDays = 60,
                SchemaVersion = 2
            };

            // Act
            _manager.Save(original);
            var loaded = _manager.Load();

            // Assert
            loaded.Language.Should().Be("en-US");
            loaded.StartWithWindows.Should().BeFalse();
            loaded.MinimizeToTray.Should().BeFalse();
            loaded.DefaultBackupPath.Should().Be(@"X:\Backups");
            loaded.LogRetentionDays.Should().Be(14);
            loaded.HistoryRetentionDays.Should().Be(60);
            loaded.SchemaVersion.Should().Be(2);
        }

        [TestMethod]
        public void Save_WithSmtpSettings_PreservesSmtp()
        {
            // Arrange — eski tekil smtp alanı; Load() sırasında SmtpProfiles'e migrate edilir
            var settings = new AppSettings
            {
                Smtp = new SmtpSettings
                {
                    Host = "smtp.gmail.com",
                    Port = 465,
                    UseSsl = true,
                    Username = "user@gmail.com",
                    SenderDisplayName = "Test Sender"
                }
            };

            // Act
            _manager.Save(settings);
            var loaded = _manager.Load();

            // Assert — MigrateSmtpLegacy: Smtp null yapılır, ayarlar SmtpProfiles[0]'a taşınır
            loaded.Smtp.Should().BeNull();
            loaded.SmtpProfiles.Should().HaveCount(1);
            loaded.SmtpProfiles[0].Host.Should().Be("smtp.gmail.com");
            loaded.SmtpProfiles[0].Port.Should().Be(465);
            loaded.SmtpProfiles[0].UseSsl.Should().BeTrue();
            loaded.SmtpProfiles[0].Username.Should().Be("user@gmail.com");
        }

        [TestMethod]
        public void Load_WhenFileCorrupted_ReturnsDefaults()
        {
            // Arrange — bozuk JSON yaz
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
            File.WriteAllText(SettingsFilePath, "{ this is not valid json !!!");

            // Act
            var settings = _manager.Load();

            // Assert — hata yutulur, varsayılan döner
            settings.Should().NotBeNull();
            settings.Language.Should().Be("tr-TR");
        }

        [TestMethod]
        public void Load_WhenFileEmpty_ReturnsDefaults()
        {
            // Arrange — boş dosya
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
            File.WriteAllText(SettingsFilePath, "");

            // Act
            var settings = _manager.Load();

            // Assert
            settings.Should().NotBeNull();
            settings.Language.Should().Be("tr-TR");
        }

        [TestMethod]
        public void Load_WhenFileHasNullContent_ReturnsDefaults()
        {
            // Arrange — "null" JSON
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
            File.WriteAllText(SettingsFilePath, "null");

            // Act
            var settings = _manager.Load();

            // Assert
            settings.Should().NotBeNull();
        }

        [TestMethod]
        public void Save_NullSettings_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _manager.Save(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Save_OverwritesExistingFile()
        {
            // Arrange — ilk kayıt
            var first = new AppSettings { Language = "tr-TR" };
            _manager.Save(first);

            // Act — üzerine kayıt
            var second = new AppSettings { Language = "en-US" };
            _manager.Save(second);

            // Assert — son hali
            var loaded = _manager.Load();
            loaded.Language.Should().Be("en-US");
        }
    }
}
