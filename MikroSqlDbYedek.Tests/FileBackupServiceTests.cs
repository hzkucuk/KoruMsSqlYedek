using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine.FileBackup;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class FileBackupServiceTests
    {
        private string _testDir;
        private string _sourceDir;
        private string _destDir;
        private Mock<IVssService> _mockVss;
        private FileBackupService _service;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "MikroSqlDbYedek_FileBackupTests", Guid.NewGuid().ToString());
            _sourceDir = Path.Combine(_testDir, "Source");
            _destDir = Path.Combine(_testDir, "Dest");
            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_destDir);

            _mockVss = new Mock<IVssService>();
            _service = new FileBackupService(_mockVss.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        #region BackupFilesAsync

        [TestMethod]
        public async Task BackupFilesAsync_WhenPlanIsNull_ReturnsEmptyList()
        {
            var results = await _service.BackupFilesAsync(null, null, CancellationToken.None);

            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task BackupFilesAsync_WhenFileBackupDisabled_ReturnsEmptyList()
        {
            var plan = new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                LocalPath = _destDir,
                FileBackup = new FileBackupConfig { IsEnabled = false }
            };

            var results = await _service.BackupFilesAsync(plan, null, CancellationToken.None);

            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task BackupFilesAsync_WhenFileBackupNull_ReturnsEmptyList()
        {
            var plan = new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                LocalPath = _destDir,
                FileBackup = null
            };

            var results = await _service.BackupFilesAsync(plan, null, CancellationToken.None);

            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task BackupFilesAsync_CopiesEnabledSources()
        {
            // Arrange
            CreateTestFile("doc1.txt", "Hello");
            CreateTestFile("doc2.txt", "World");

            var plan = CreatePlanWithSource("TestSource", _sourceDir, useVss: false);

            // Act
            var results = await _service.BackupFilesAsync(plan, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(1);
            results[0].Status.Should().Be(BackupResultStatus.Success);
            results[0].FilesCopied.Should().Be(2);
        }

        [TestMethod]
        public async Task BackupFilesAsync_SkipsDisabledSources()
        {
            // Arrange
            CreateTestFile("doc1.txt", "Hello");

            var plan = CreatePlanWithSource("DisabledSource", _sourceDir, useVss: false);
            plan.FileBackup.Sources[0].IsEnabled = false;

            // Act
            var results = await _service.BackupFilesAsync(plan, null, CancellationToken.None);

            // Assert
            results.Should().BeEmpty();
        }

        [TestMethod]
        public async Task BackupFilesAsync_ReportsProgress()
        {
            // Arrange
            CreateTestFile("doc1.txt", "Hello");

            var plan = CreatePlanWithSource("ProgressTest", _sourceDir, useVss: false);
            var progressValues = new List<int>();
            var progress = new Progress<int>(v => progressValues.Add(v));

            // Act
            await _service.BackupFilesAsync(plan, progress, CancellationToken.None);

            // Assert — en az bir ilerleme raporu gelmiş olmalı
            // Progress<T> async olarak raporladığı için kısa bekleme
            await Task.Delay(100);
            progressValues.Should().NotBeEmpty();
        }

        #endregion

        #region BackupSourceAsync — VSS Flow

        [TestMethod]
        public async Task BackupSourceAsync_WhenVssAvailable_CreatesSnapshotAndCopies()
        {
            // Arrange
            CreateTestFile("outlook.pst", "PST content");
            var snapshotId = Guid.NewGuid();

            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, It.IsAny<string>()))
                     .Returns<Guid, string>((id, path) => path); // Snapshot path = original path (test ortamında)

            var source = CreateSource("OutlookPST", _sourceDir, useVss: true, includePatterns: new[] { "*.pst" });

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.UsedVss.Should().BeTrue();
            result.FilesCopied.Should().Be(1);
            result.Status.Should().Be(BackupResultStatus.Success);
            _mockVss.Verify(v => v.CreateSnapshot(It.IsAny<string>()), Times.Once);
            _mockVss.Verify(v => v.DeleteSnapshot(snapshotId), Times.Once);
        }

        [TestMethod]
        public async Task BackupSourceAsync_WhenVssFails_FallsBackToDirectCopy()
        {
            // Arrange
            CreateTestFile("report.docx", "Report content");

            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>()))
                     .Throws(new InvalidOperationException("VSS service unavailable"));

            var source = CreateSource("Documents", _sourceDir, useVss: true, includePatterns: new[] { "*.docx" });

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.UsedVss.Should().BeFalse();
            result.FilesCopied.Should().Be(1);
            result.Status.Should().Be(BackupResultStatus.Success);
        }

        [TestMethod]
        public async Task BackupSourceAsync_WhenVssNotAvailable_UsesDirectCopy()
        {
            // Arrange
            CreateTestFile("data.xlsx", "Spreadsheet");

            _mockVss.Setup(v => v.IsAvailable()).Returns(false);

            var source = CreateSource("Spreadsheets", _sourceDir, useVss: true, includePatterns: new[] { "*.xlsx" });

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.UsedVss.Should().BeFalse();
            result.FilesCopied.Should().Be(1);
            _mockVss.Verify(v => v.CreateSnapshot(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task BackupSourceAsync_WhenVssDisabledOnSource_SkipsVss()
        {
            // Arrange
            CreateTestFile("notes.txt", "Notes");

            _mockVss.Setup(v => v.IsAvailable()).Returns(true);

            var source = CreateSource("Notes", _sourceDir, useVss: false);

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.UsedVss.Should().BeFalse();
            result.FilesCopied.Should().Be(1);
            _mockVss.Verify(v => v.CreateSnapshot(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task BackupSourceAsync_SnapshotDeletedAfterCompletion()
        {
            // Arrange
            CreateTestFile("file.dat", "data");
            var snapshotId = Guid.NewGuid();

            _mockVss.Setup(v => v.IsAvailable()).Returns(true);
            _mockVss.Setup(v => v.CreateSnapshot(It.IsAny<string>())).Returns(snapshotId);
            _mockVss.Setup(v => v.GetSnapshotFilePath(snapshotId, It.IsAny<string>()))
                     .Returns<Guid, string>((id, path) => path);

            var source = CreateSource("Data", _sourceDir, useVss: true);

            // Act
            await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            _mockVss.Verify(v => v.DeleteSnapshot(snapshotId), Times.Once);
        }

        #endregion

        #region BackupSourceAsync — File Patterns

        [TestMethod]
        public async Task BackupSourceAsync_WithIncludePatterns_OnlyIncludesMatchingFiles()
        {
            // Arrange
            CreateTestFile("report.docx", "Word doc");
            CreateTestFile("data.xlsx", "Spreadsheet");
            CreateTestFile("notes.txt", "Notes");

            var source = CreateSource("Docs", _sourceDir, useVss: false, includePatterns: new[] { "*.docx", "*.xlsx" });

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.FilesCopied.Should().Be(2);
            result.Status.Should().Be(BackupResultStatus.Success);
        }

        [TestMethod]
        public async Task BackupSourceAsync_WithExcludePatterns_ExcludesMatchingFiles()
        {
            // Arrange
            CreateTestFile("document.docx", "Doc");
            CreateTestFile("temp.tmp", "Temp");
            CreateTestFile("~lockfile", "Lock");

            var source = CreateSource("ExcludeTest", _sourceDir, useVss: false,
                excludePatterns: new[] { "*.tmp", "~*" });

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.FilesCopied.Should().Be(1); // Sadece document.docx
            result.Status.Should().Be(BackupResultStatus.Success);
        }

        [TestMethod]
        public async Task BackupSourceAsync_RecursiveTrue_IncludesSubdirectoryFiles()
        {
            // Arrange
            CreateTestFile("root.txt", "Root");
            Directory.CreateDirectory(Path.Combine(_sourceDir, "SubDir"));
            File.WriteAllText(Path.Combine(_sourceDir, "SubDir", "sub.txt"), "SubDir file");

            var source = CreateSource("Recursive", _sourceDir, useVss: false);
            source.Recursive = true;

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.FilesCopied.Should().Be(2);
        }

        [TestMethod]
        public async Task BackupSourceAsync_RecursiveFalse_ExcludesSubdirectoryFiles()
        {
            // Arrange
            CreateTestFile("root.txt", "Root");
            Directory.CreateDirectory(Path.Combine(_sourceDir, "SubDir"));
            File.WriteAllText(Path.Combine(_sourceDir, "SubDir", "sub.txt"), "SubDir file");

            var source = CreateSource("NonRecursive", _sourceDir, useVss: false);
            source.Recursive = false;

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.FilesCopied.Should().Be(1); // Sadece root.txt
        }

        #endregion

        #region BackupSourceAsync — Status & Error Handling

        [TestMethod]
        public async Task BackupSourceAsync_WhenSourceDirNotExists_ReturnsSuccessWithZeroFiles()
        {
            // Arrange
            string nonExistentDir = Path.Combine(_testDir, "NonExistent");

            var source = CreateSource("Missing", nonExistentDir, useVss: false);

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.FilesCopied.Should().Be(0);
            result.FilesSkipped.Should().Be(0);
            result.Status.Should().Be(BackupResultStatus.Success);
        }

        [TestMethod]
        public async Task BackupSourceAsync_ReportsSourceProgress()
        {
            // Arrange
            CreateTestFile("file1.txt", "Content1");
            CreateTestFile("file2.txt", "Content2");
            CreateTestFile("file3.txt", "Content3");

            var source = CreateSource("ProgressTest", _sourceDir, useVss: false);
            var progressValues = new List<int>();
            var progress = new Progress<int>(v => progressValues.Add(v));

            // Act
            await _service.BackupSourceAsync(source, _destDir, progress, CancellationToken.None);

            // Assert — Progress<T> raporları async olarak gelir
            await Task.Delay(100);
            progressValues.Should().NotBeEmpty();
        }

        [TestMethod]
        public async Task BackupSourceAsync_SetsTimestamps()
        {
            // Arrange
            CreateTestFile("file.txt", "Content");
            var source = CreateSource("Timestamps", _sourceDir, useVss: false);
            var before = DateTime.UtcNow;

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.StartedAt.Should().BeOnOrAfter(before);
            result.CompletedAt.Should().NotBeNull();
            result.CompletedAt.Value.Should().BeOnOrAfter(result.StartedAt);
            result.Duration.Should().NotBeNull();
        }

        [TestMethod]
        public async Task BackupSourceAsync_SetsTotalSizeBytes()
        {
            // Arrange
            string content = new string('A', 1024);
            CreateTestFile("sized.txt", content);

            var source = CreateSource("SizeCheck", _sourceDir, useVss: false);

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert
            result.TotalSizeBytes.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task BackupSourceAsync_CreatesDestinationDirectory()
        {
            // Arrange
            CreateTestFile("data.txt", "data");
            string freshDest = Path.Combine(_testDir, "FreshDest");

            var source = CreateSource("NewDest", _sourceDir, useVss: false);

            // Act
            var result = await _service.BackupSourceAsync(source, freshDest, null, CancellationToken.None);

            // Assert
            result.DestinationPath.Should().NotBeNullOrEmpty();
            Directory.Exists(result.DestinationPath).Should().BeTrue();
            result.FilesCopied.Should().Be(1);
        }

        [TestMethod]
        public async Task BackupSourceAsync_SanitizesFolderName()
        {
            // Arrange
            CreateTestFile("test.txt", "test");

            var source = CreateSource("Source:Name/With<Invalid>Chars", _sourceDir, useVss: false);

            // Act
            var result = await _service.BackupSourceAsync(source, _destDir, null, CancellationToken.None);

            // Assert — sadece klasör adı kısmını kontrol et (tam yol C: içerir)
            string folderName = Path.GetFileName(result.DestinationPath);
            folderName.Should().NotContain(":");
            folderName.Should().NotContain("/");
            folderName.Should().NotContain("<");
            folderName.Should().NotContain(">");
            result.FilesCopied.Should().Be(1);
        }

        #endregion

        #region Helpers

        private void CreateTestFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(_sourceDir, fileName), content);
        }

        private BackupPlan CreatePlanWithSource(string sourceName, string sourcePath, bool useVss)
        {
            return new BackupPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = "Test File Backup Plan",
                LocalPath = _destDir,
                FileBackup = new FileBackupConfig
                {
                    IsEnabled = true,
                    Sources = new List<FileBackupSource>
                    {
                        CreateSource(sourceName, sourcePath, useVss)
                    }
                }
            };
        }

        private FileBackupSource CreateSource(
            string name,
            string path,
            bool useVss,
            string[] includePatterns = null,
            string[] excludePatterns = null)
        {
            return new FileBackupSource
            {
                SourceName = name,
                SourcePath = path,
                Recursive = true,
                UseVss = useVss,
                IsEnabled = true,
                IncludePatterns = includePatterns != null
                    ? new List<string>(includePatterns)
                    : new List<string>(),
                ExcludePatterns = excludePatterns != null
                    ? new List<string>(excludePatterns)
                    : new List<string>()
            };
        }

        #endregion
    }
}
