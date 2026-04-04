using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Helpers;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// PlanPasswordHelper — SHA256 + DPAPI şifre hash/doğrulama testleri.
    /// Tüm girdi kombinasyonları ve edge-case'ler kapsanır.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class PlanPasswordHelperTests
    {
        // ═══════════════ HashPassword ═══════════════

        [TestMethod]
        public void HashPassword_ValidInput_ReturnsNonEmptyBase64()
        {
            var hash = PlanPasswordHelper.HashPassword("test123");

            hash.Should().NotBeNullOrWhiteSpace();
            // Base64 geçerli olmalı
            Action act = () => Convert.FromBase64String(hash);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void HashPassword_NullInput_ReturnsNull()
        {
            PlanPasswordHelper.HashPassword(null).Should().BeNull();
        }

        [TestMethod]
        public void HashPassword_EmptyString_ReturnsNull()
        {
            PlanPasswordHelper.HashPassword(string.Empty).Should().BeNull();
        }

        [TestMethod]
        public void HashPassword_SameInput_ProducesDifferentHashes()
        {
            // DPAPI her seferinde farklı çıktı üretir (entropy)
            var hash1 = PlanPasswordHelper.HashPassword("samePassword");
            var hash2 = PlanPasswordHelper.HashPassword("samePassword");

            // Hash'ler farklı olmalı (DPAPI nonce farklılığı)
            hash1.Should().NotBe(hash2);
        }

        [TestMethod]
        public void HashPassword_DifferentInputs_ProducesDifferentHashes()
        {
            var hash1 = PlanPasswordHelper.HashPassword("password1");
            var hash2 = PlanPasswordHelper.HashPassword("password2");

            hash1.Should().NotBe(hash2);
        }

        [TestMethod]
        public void HashPassword_UnicodeInput_ReturnsValidHash()
        {
            var hash = PlanPasswordHelper.HashPassword("şifre123!@#ğüöç");

            hash.Should().NotBeNullOrWhiteSpace();
            PlanPasswordHelper.VerifyPassword("şifre123!@#ğüöç", hash).Should().BeTrue();
        }

        [TestMethod]
        public void HashPassword_VeryLongInput_ReturnsValidHash()
        {
            string longPassword = new string('A', 10000);
            var hash = PlanPasswordHelper.HashPassword(longPassword);

            hash.Should().NotBeNullOrWhiteSpace();
            PlanPasswordHelper.VerifyPassword(longPassword, hash).Should().BeTrue();
        }

        [TestMethod]
        public void HashPassword_SingleChar_ReturnsValidHash()
        {
            var hash = PlanPasswordHelper.HashPassword("X");

            hash.Should().NotBeNullOrWhiteSpace();
            PlanPasswordHelper.VerifyPassword("X", hash).Should().BeTrue();
        }

        [TestMethod]
        public void HashPassword_WhitespaceOnly_ReturnsValidHash()
        {
            // Boşluk karakteri geçerli bir şifre
            var hash = PlanPasswordHelper.HashPassword("   ");

            hash.Should().NotBeNullOrWhiteSpace();
            PlanPasswordHelper.VerifyPassword("   ", hash).Should().BeTrue();
        }

        // ═══════════════ VerifyPassword ═══════════════

        [TestMethod]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            string password = "MySecurePassword!";
            string hash = PlanPasswordHelper.HashPassword(password);

            PlanPasswordHelper.VerifyPassword(password, hash).Should().BeTrue();
        }

        [TestMethod]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            string hash = PlanPasswordHelper.HashPassword("correctPassword");

            PlanPasswordHelper.VerifyPassword("wrongPassword", hash).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_NullPlainText_ReturnsFalse()
        {
            string hash = PlanPasswordHelper.HashPassword("test");
            PlanPasswordHelper.VerifyPassword(null, hash).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_EmptyPlainText_ReturnsFalse()
        {
            string hash = PlanPasswordHelper.HashPassword("test");
            PlanPasswordHelper.VerifyPassword(string.Empty, hash).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_NullStoredHash_ReturnsFalse()
        {
            PlanPasswordHelper.VerifyPassword("test", null).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_EmptyStoredHash_ReturnsFalse()
        {
            PlanPasswordHelper.VerifyPassword("test", string.Empty).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_BothNull_ReturnsFalse()
        {
            PlanPasswordHelper.VerifyPassword(null, null).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_BothEmpty_ReturnsFalse()
        {
            PlanPasswordHelper.VerifyPassword("", "").Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_InvalidBase64Hash_ReturnsFalse()
        {
            // Bozuk hash — exception fırlatmamalı, false dönmeli
            PlanPasswordHelper.VerifyPassword("test", "not-valid-base64!!!").Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_TamperedHash_ReturnsFalse()
        {
            // Hash oluştur, sonra bir byte değiştir
            string hash = PlanPasswordHelper.HashPassword("test");
            byte[] bytes = Convert.FromBase64String(hash);
            bytes[0] = (byte)(bytes[0] ^ 0xFF); // ilk byte'ı ters çevir
            string tampered = Convert.ToBase64String(bytes);

            PlanPasswordHelper.VerifyPassword("test", tampered).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_CaseSensitive_ReturnsFalseForDifferentCase()
        {
            string hash = PlanPasswordHelper.HashPassword("Password");

            PlanPasswordHelper.VerifyPassword("password", hash).Should().BeFalse();
            PlanPasswordHelper.VerifyPassword("PASSWORD", hash).Should().BeFalse();
        }

        [TestMethod]
        public void VerifyPassword_LeadingTrailingSpaces_AreSignificant()
        {
            string hash = PlanPasswordHelper.HashPassword("password");

            PlanPasswordHelper.VerifyPassword(" password", hash).Should().BeFalse();
            PlanPasswordHelper.VerifyPassword("password ", hash).Should().BeFalse();
            PlanPasswordHelper.VerifyPassword(" password ", hash).Should().BeFalse();
        }

        // ═══════════════ Round-trip Çoklu ═══════════════

        [TestMethod]
        public void HashAndVerify_MultiplePasswords_AllRoundTripCorrectly()
        {
            string[] passwords = { "a", "123", "şifre!", "pass word", "P@$$w0rd!", "日本語パス" };

            foreach (var pw in passwords)
            {
                var hash = PlanPasswordHelper.HashPassword(pw);
                PlanPasswordHelper.VerifyPassword(pw, hash).Should().BeTrue(
                    $"'{pw}' şifresinin hash round-trip'i başarılı olmalı");
            }
        }

        [TestMethod]
        public void HashAndVerify_CrossVerify_WrongPassword_NeverMatches()
        {
            // Her şifrenin hash'i yalnızca kendi şifresiyle eşleşmeli
            string[] passwords = { "alpha", "beta", "gamma", "delta" };
            string[] hashes = new string[passwords.Length];

            for (int i = 0; i < passwords.Length; i++)
            {
                hashes[i] = PlanPasswordHelper.HashPassword(passwords[i]);
            }

            for (int i = 0; i < passwords.Length; i++)
            {
                for (int j = 0; j < hashes.Length; j++)
                {
                    bool expected = i == j;
                    PlanPasswordHelper.VerifyPassword(passwords[i], hashes[j]).Should().Be(expected,
                        $"'{passwords[i]}' yalnızca kendi hash'iyle eşleşmeli (i={i}, j={j})");
                }
            }
        }
    }
}
