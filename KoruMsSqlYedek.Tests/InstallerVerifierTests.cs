using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Engine.Update;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class InstallerVerifierTests
    {
        // ── IsAllowedDownloadUrl ─────────────────────────────────────────────

        [DataTestMethod]
        [DataRow("https://github.com/hzkucuk/KoruMsSqlYedek/releases/download/v1.0.0/KoruMsSqlYedek_v1.0.0_Setup.exe")]
        [DataRow("https://api.github.com/repos/hzkucuk/KoruMsSqlYedek/releases/assets/123")]
        [DataRow("https://objects.githubusercontent.com/github-production-release-asset/abc?x=1")]
        [DataRow("https://release-assets.githubusercontent.com/github-production-release-asset/abc")]
        [DataRow("HTTPS://GITHUB.COM/x/y")]
        public void IsAllowedDownloadUrl_AllowsHttpsGitHubHosts(string url)
        {
            InstallerVerifier.IsAllowedDownloadUrl(url).Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("http://github.com/x/y")]                               // https değil
        [DataRow("ftp://github.com/x/y")]
        [DataRow("file:///C:/Windows/System32/cmd.exe")]
        [DataRow("https://evil.com/github.com/x")]
        [DataRow("https://github.com.evil.com/x")]
        [DataRow("https://notgithub.com/x")]
        [DataRow("https://raw.githubusercontent.com/x")]                 // listede yok
        [DataRow("https://user:pass@github.com/x")]                      // userinfo
        [DataRow("https://127.0.0.1/x")]
        [DataRow("C:\\Users\\Public\\evil.exe")]
        [DataRow("github.com/x/y")]                                      // göreli
        public void IsAllowedDownloadUrl_RejectsEverythingElse(string url)
        {
            InstallerVerifier.IsAllowedDownloadUrl(url).Should().BeFalse();
        }

        // ── IsValidSha256Hex / Sha256Matches ─────────────────────────────────

        [TestMethod]
        public void IsValidSha256Hex_Accepts64HexCharsAnyCase()
        {
            string lower = new string('a', 64);
            string upper = new string('A', 32) + new string('0', 32);

            InstallerVerifier.IsValidSha256Hex(lower).Should().BeTrue();
            InstallerVerifier.IsValidSha256Hex(upper).Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("abc")]
        [DataRow("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
        public void IsValidSha256Hex_RejectsInvalid(string value)
        {
            InstallerVerifier.IsValidSha256Hex(value).Should().BeFalse();
        }

        [TestMethod]
        public void IsValidSha256Hex_Rejects63And65Chars()
        {
            InstallerVerifier.IsValidSha256Hex(new string('a', 63)).Should().BeFalse();
            InstallerVerifier.IsValidSha256Hex(new string('a', 65)).Should().BeFalse();
        }

        [TestMethod]
        public void Sha256Matches_IsCaseInsensitive()
        {
            string hex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("koru")));

            InstallerVerifier.Sha256Matches(hex.ToLowerInvariant(), hex.ToUpperInvariant()).Should().BeTrue();
            InstallerVerifier.Sha256Matches(hex, hex).Should().BeTrue();
        }

        [TestMethod]
        public void Sha256Matches_DifferentHashes_ReturnsFalse()
        {
            string a = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("a")));
            string b = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("b")));

            InstallerVerifier.Sha256Matches(a, b).Should().BeFalse();
        }

        // ── ComputeSha256HexAsync ────────────────────────────────────────────

        [TestMethod]
        public async Task ComputeSha256HexAsync_MatchesReferenceImplementation()
        {
            string path = Path.Combine(Path.GetTempPath(), $"koru-sha-{Guid.NewGuid():N}.bin");
            byte[] payload = new byte[300_000];
            new Random(42).NextBytes(payload);
            await File.WriteAllBytesAsync(path, payload);

            try
            {
                string actual = await InstallerVerifier.ComputeSha256HexAsync(path);
                string expected = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

                actual.Should().Be(expected);
                InstallerVerifier.Sha256Matches(actual, expected.ToUpperInvariant()).Should().BeTrue();
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ── DownloadToFileAsync ──────────────────────────────────────────────

        [TestMethod]
        public async Task DownloadToFileAsync_DisallowedUrl_ThrowsWithoutCreatingFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"koru-dl-{Guid.NewGuid():N}.exe");

            Func<Task> act = () => InstallerVerifier.DownloadToFileAsync("http://github.com/x", path);

            await act.Should().ThrowAsync<InvalidOperationException>();
            File.Exists(path).Should().BeFalse();
        }
    }
}
