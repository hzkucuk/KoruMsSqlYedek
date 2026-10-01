using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ExceptionMessageHelperTests
    {
        private const string ClaimsAssembly =
            "System.Security.Claims, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        // ── Derleme yükleme hataları ─────────────────────────────────────────

        [TestMethod]
        public void Describe_AssemblyFileNotFoundWithEmptyMessage_NamesDll()
        {
            // Sahada görülen durum: mesaj boş, yalnızca FileName dolu
            var ex = new FileNotFoundException(string.Empty, ClaimsAssembly);

            string msg = ExceptionMessageHelper.Describe(ex);

            msg.Should().Contain("System.Security.Claims.dll");
            msg.Should().Contain(nameof(FileNotFoundException));
        }

        [TestMethod]
        public void Describe_AssemblyFailureWrappedInHttpRequestException_FindsAssembly()
        {
            // Google token yenileme: HttpRequestException("see inner exception") → FNFE
            var ex = new HttpRequestException(
                "The SSL connection could not be established, see inner exception.",
                new FileNotFoundException(string.Empty, ClaimsAssembly));

            ExceptionMessageHelper.Describe(ex).Should().Contain("System.Security.Claims.dll");
            ExceptionMessageHelper.GetFailedAssemblyName(ex).Should().Be("System.Security.Claims");
        }

        [TestMethod]
        public void GetFailedAssemblyName_PlainFileNotFound_IsNotAssemblyFailure()
        {
            // Yedek dosyası bulunamadı — derleme hatası sanılıp servis yeniden başlatılmamalı
            var ex = new FileNotFoundException("Dosya yok", @"C:\Backups\x.bak");

            ExceptionMessageHelper.GetFailedAssemblyName(ex).Should().BeNull();
            ExceptionMessageHelper.IsRestartableAssemblyFailure(ex).Should().BeFalse();
            ExceptionMessageHelper.Describe(ex).Should().Be("Dosya yok");
        }

        [TestMethod]
        public void Describe_FileLoadVersionMismatch_KeepsOriginalMessage()
        {
            var ex = new FileLoadException(
                "The located assembly's manifest definition does not match the assembly reference. (0x80131040)",
                "Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef");

            string msg = ExceptionMessageHelper.Describe(ex);

            msg.Should().Contain("Foo.dll");
            msg.Should().Contain("manifest definition does not match");
        }

        [TestMethod]
        public void IsRestartableAssemblyFailure_OnlyForNotFoundAndSharingViolation()
        {
            ExceptionMessageHelper.IsRestartableAssemblyFailure(
                new FileNotFoundException(string.Empty, ClaimsAssembly)).Should().BeTrue();

            var sharing = new FileLoadException("locked", ClaimsAssembly) { HResult = unchecked((int)0x80070020) };
            ExceptionMessageHelper.IsRestartableAssemblyFailure(sharing).Should().BeTrue();

            var mismatch = new FileLoadException("mismatch", ClaimsAssembly) { HResult = unchecked((int)0x80131040) };
            ExceptionMessageHelper.IsRestartableAssemblyFailure(mismatch).Should().BeFalse();

            ExceptionMessageHelper.IsRestartableAssemblyFailure(
                new BadImageFormatException("bad", ClaimsAssembly)).Should().BeFalse();
        }

        // ── Genel mesaj seçimi ───────────────────────────────────────────────

        [TestMethod]
        public void Describe_SkipsWrapperMessage_ReturnsInnerMeaningfulMessage()
        {
            var ex = new HttpRequestException(
                "The SSL connection could not be established, see inner exception.",
                new Win32Exception(5, "Erişim reddedildi"));

            ExceptionMessageHelper.Describe(ex).Should().Be("Erişim reddedildi");
        }

        [TestMethod]
        public void Describe_BothMeaningful_PrefersInnermost()
        {
            var ex = new InvalidOperationException("Dış bağlam",
                new IOException("Disk dolu"));

            ExceptionMessageHelper.Describe(ex).Should().Be("Disk dolu");
        }

        [TestMethod]
        public void Describe_InnerEmpty_FallsBackToOuterMeaningfulMessage()
        {
            var ex = new InvalidOperationException("Dış anlamlı mesaj",
                new IOException(string.Empty));

            ExceptionMessageHelper.Describe(ex).Should().Be("Dış anlamlı mesaj");
        }

        [TestMethod]
        public void Describe_AllMessagesEmpty_ReturnsTypeNameNeverEmpty()
        {
            var ex = new InvalidOperationException(" ", new IOException(string.Empty));

            ExceptionMessageHelper.Describe(ex).Should().Be(nameof(IOException));
        }

        [TestMethod]
        public void Describe_Null_ReturnsPlaceholder()
        {
            ExceptionMessageHelper.Describe(null).Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void Describe_AggregateException_UsesFirstInner()
        {
            // Describe yalnızca InnerException zincirini izler; AggregateException'da ilk iç hata
            var ex = new AggregateException(new IOException("A"), new IOException("B"));

            ExceptionMessageHelper.Describe(ex).Should().Be("A");
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    public class BackupCancellationRegistryRunningIdsTests
    {
        [TestMethod]
        public void GetRunningPlanIds_ReturnsSnapshotOfRegisteredPlans()
        {
            var registry = new BackupCancellationRegistry();
            using var a = new CancellationTokenSource();
            using var b = new CancellationTokenSource();
            registry.Register("plan-a", a);
            registry.Register("plan-b", b);
            registry.Unregister("plan-a");

            registry.GetRunningPlanIds().Should().BeEquivalentTo(new[] { "plan-b" });
        }
    }
}
