using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Helpers;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    public class PasswordProtectorTests
    {
        [TestMethod]
        public void Protect_And_Unprotect_RoundTrip_Success()
        {
            // Arrange
            string original = "TestŞifre123!@#";

            // Act
            string protectedValue = PasswordProtector.Protect(original);
            string unprotected = PasswordProtector.Unprotect(protectedValue);

            // Assert
            Assert.AreNotEqual(original, protectedValue);
            Assert.AreEqual(original, unprotected);
        }

        [TestMethod]
        public void Protect_NullInput_ReturnsNull()
        {
            Assert.IsNull(PasswordProtector.Protect(null));
            Assert.IsNull(PasswordProtector.Protect(string.Empty));
        }

        [TestMethod]
        public void Unprotect_NullInput_ReturnsNull()
        {
            Assert.IsNull(PasswordProtector.Unprotect(null));
            Assert.IsNull(PasswordProtector.Unprotect(string.Empty));
        }

        [TestMethod]
        public void IsProtected_ValidProtectedValue_ReturnsTrue()
        {
            string protectedValue = PasswordProtector.Protect("test");
            Assert.IsTrue(PasswordProtector.IsProtected(protectedValue));
        }

        [TestMethod]
        public void IsProtected_PlainText_ReturnsFalse()
        {
            Assert.IsFalse(PasswordProtector.IsProtected("not-encrypted"));
        }
    }
}
