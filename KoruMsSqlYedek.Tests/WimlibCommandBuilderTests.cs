using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.FileBackup;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// wimlib-imagex argüman üretimi testleri.
    /// Saf fonksiyonlar olduğu için wimlib kurulu olmadan çalışır.
    /// </summary>
    [TestClass]
    public class WimlibCommandBuilderTests
    {
        // ── Sıkıştırma bayrakları ────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        [DataRow(DiskImageCompression.None,  "--compress=none")]
        [DataRow(DiskImageCompression.Fast,  "--compress=fast")]
        [DataRow(DiskImageCompression.Max,   "--compress=maximum")]
        [DataRow(DiskImageCompression.Solid, "--solid")]
        public void GetCompressionFlag_MapsEachLevelToWimlibFlag(
            DiskImageCompression compression, string expected)
        {
            WimlibCommandBuilder.GetCompressionFlag(compression).Should().Be(expected);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetCompressionFlag_UnknownValue_FallsBackToSolid()
        {
            // Enum'a ileride yeni değer eklenirse sıkıştırmasız imaj üretilmemeli
            WimlibCommandBuilder.GetCompressionFlag((DiskImageCompression)99)
                .Should().Be("--solid");
        }

        // ── Kaynak yolu normalizasyonu ───────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void NormalizeSourcePath_BareDriveLetter_GetsTrailingSeparator()
        {
            // wimlib capture kaynağı dizin olarak ister; "C:" göreli yol anlamına gelir
            WimlibCommandBuilder.NormalizeSourcePath("C:")
                .Should().Be("C:" + Path.DirectorySeparatorChar);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NormalizeSourcePath_AlreadyRooted_LeftUnchanged()
        {
            WimlibCommandBuilder.NormalizeSourcePath(@"D:\Data").Should().Be(@"D:\Data");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NormalizeSourcePath_Whitespace_Throws()
        {
            Action act = () => WimlibCommandBuilder.NormalizeSourcePath("   ");
            act.Should().Throw<ArgumentException>();
        }

        // ── Dosya adı üretimi ────────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildImageFileName_StripsCharactersIllegalInFileNames()
        {
            string name = WimlibCommandBuilder.BuildImageFileName("C:", new DateTime(2026, 8, 9, 14, 30, 0));

            name.Should().Be("C_20260809_143000.wim");
            name.Should().NotContain(":");
            name.Should().NotContain("\\");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildImageFileName_DistinctTimestamps_ProduceDistinctNames()
        {
            string first  = WimlibCommandBuilder.BuildImageFileName("C:", new DateTime(2026, 8, 9, 14, 30, 0));
            string second = WimlibCommandBuilder.BuildImageFileName("C:", new DateTime(2026, 8, 9, 14, 30, 1));

            // Aynı sürücünün ardışık yedekleri birbirinin üzerine yazmamalı
            first.Should().NotBe(second);
        }

        // ── Capture argümanları ──────────────────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildCaptureArguments_AlwaysRequestsVssSnapshot()
        {
            string args = WimlibCommandBuilder.BuildCaptureArguments(
                "C:", @"D:\Backups\C.wim", DiskImageCompression.Solid, writeIntegrityTable: true);

            // Snapshot olmadan açık/kilitli sistem dosyaları okunamaz
            args.Should().Contain("--snapshot");
            args.Should().StartWith("capture ");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildCaptureArguments_QuotesPathsSoSpacesSurvive()
        {
            string args = WimlibCommandBuilder.BuildCaptureArguments(
                "C:", @"D:\Program Files\Backups\C.wim", DiskImageCompression.Max,
                writeIntegrityTable: false);

            args.Should().Contain(@"""D:\Program Files\Backups\C.wim""");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildCaptureArguments_IntegrityTableToggleAddsOrOmitsCheckFlag()
        {
            string withCheck = WimlibCommandBuilder.BuildCaptureArguments(
                "C:", @"D:\x.wim", DiskImageCompression.Solid, writeIntegrityTable: true);
            string without = WimlibCommandBuilder.BuildCaptureArguments(
                "C:", @"D:\x.wim", DiskImageCompression.Solid, writeIntegrityTable: false);

            withCheck.Should().Contain("--check");
            without.Should().NotContain("--check");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildCaptureArguments_NeverSuppressesAclsOrMetadata()
        {
            string args = WimlibCommandBuilder.BuildCaptureArguments(
                "C:", @"D:\x.wim", DiskImageCompression.Solid, writeIntegrityTable: true);

            // Bunlar bastırılırsa sistem sürücüsü geri yüklendiğinde açılmaz
            args.Should().NotContain("--no-acls");
            args.Should().NotContain("--norpfix");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BuildCaptureArguments_EmptyOutputPath_Throws()
        {
            Action act = () => WimlibCommandBuilder.BuildCaptureArguments(
                "C:", "", DiskImageCompression.Solid, writeIntegrityTable: true);

            act.Should().Throw<ArgumentException>();
        }

        // ── Çalıştırılabilir dosya çözümleme ─────────────────────

        [TestMethod]
        [TestCategory("Unit")]
        public void ResolveExecutablePath_PrefersBundledCopyOverPath()
        {
            string temp = Path.Combine(Path.GetTempPath(), "wimlib_test_" + Guid.NewGuid().ToString("N"));
            string nativeDir = Path.Combine(temp, WimlibCommandBuilder.NativeSubDirectory);
            Directory.CreateDirectory(nativeDir);

            string bundled = Path.Combine(nativeDir, WimlibCommandBuilder.ExecutableName);
            File.WriteAllText(bundled, "stub");

            try
            {
                WimlibCommandBuilder.ResolveExecutablePath(temp).Should().Be(bundled);
            }
            finally
            {
                try { Directory.Delete(temp, recursive: true); } catch { }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ResolveExecutablePath_NotBundledAnywhere_ReturnsNullRatherThanThrowing()
        {
            string empty = Path.Combine(Path.GetTempPath(), "wimlib_empty_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(empty);

            try
            {
                // PATH'te wimlib varsa bu test anlamsızlaşır — o durumda atla
                string onPath = WimlibCommandBuilder.ResolveExecutablePath(empty);
                if (onPath is not null)
                {
                    Assert.Inconclusive("wimlib-imagex PATH üzerinde kurulu; bu senaryo doğrulanamaz.");
                }

                onPath.Should().BeNull();
            }
            finally
            {
                try { Directory.Delete(empty, recursive: true); } catch { }
            }
        }
    }
}
