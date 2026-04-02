using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Engine.Notification;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class EmailTemplateBuilderTests
    {
        // ── Header ────────────────────────────────────────────────────────

        [TestMethod]
        public void WriteHeader_WhenCalled_ContainsTitleInOutput()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder.WriteHeader("Test Başlık").Build();

            html.Should().Contain("Test Başlık");
        }

        [TestMethod]
        public void WriteHeader_WithSubtitle_ContainsSubtitleInOutput()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder.WriteHeader("Başlık", "Alt başlık").Build();

            html.Should().Contain("Alt başlık");
        }

        [TestMethod]
        public void WriteHeader_CalledTwice_WritesHeaderOnlyOnce()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Birinci")
                .WriteHeader("İkinci")
                .Build();

            int headerCount = CountOccurrences(html, "border-radius:8px 8px 0 0");
            headerCount.Should().Be(1);
        }

        // ── StatusBadge ──────────────────────────────────────────────────

        [TestMethod]
        public void WriteStatusBadge_WhenSuccess_ContainsCheckMark()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder.WriteHeader("Test").WriteStatusBadge("Başarılı", true).Build();

            html.Should().Contain("✓");
        }

        [TestMethod]
        public void WriteStatusBadge_WhenFailure_ContainsCrossMark()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder.WriteHeader("Test").WriteStatusBadge("Başarısız", false).Build();

            html.Should().Contain("✗");
        }

        // ── SummaryTable ─────────────────────────────────────────────────

        [TestMethod]
        public void WriteTableRow_WhenCalled_ContainsLabelAndValue()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginSummaryTable()
                .WriteTableRow("Veritabanı", "TestDB")
                .EndTable()
                .Build();

            html.Should().Contain("Veritabanı");
            html.Should().Contain("TestDB");
        }

        [TestMethod]
        public void WriteTableRow_WithColor_ContainsColorStyle()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginSummaryTable()
                .WriteTableRow("Durum", "Başarılı", "#10b981")
                .EndTable()
                .Build();

            html.Should().Contain("color:#10b981");
        }

        // ── DetailTable ──────────────────────────────────────────────────

        [TestMethod]
        public void BeginDetailTable_WhenCalled_ContainsColumnHeaders()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginDetailTable("Hedef", "Durum", "Detay")
                .EndDetailTable()
                .Build();

            html.Should().Contain("Hedef");
            html.Should().Contain("Durum");
            html.Should().Contain("Detay");
        }

        [TestMethod]
        public void WriteDetailRow_EvenIndex_HasWhiteBackground()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginDetailTable("Kolon")
                .WriteDetailRow(0, ("Değer", null))
                .EndDetailTable()
                .Build();

            html.Should().Contain("background:#fff");
        }

        [TestMethod]
        public void WriteDetailRow_OddIndex_HasAltBackground()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginDetailTable("Kolon")
                .WriteDetailRow(1, ("Değer", null))
                .EndDetailTable()
                .Build();

            html.Should().Contain("background:#f9fafb");
        }

        [TestMethod]
        public void WriteDetailRow_WithCellColor_ContainsColorStyle()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .BeginDetailTable("Kolon")
                .WriteDetailRow(0, ("✓", "#10b981"))
                .EndDetailTable()
                .Build();

            html.Should().Contain("color:#10b981");
        }

        // ── ErrorBlock ───────────────────────────────────────────────────

        [TestMethod]
        public void WriteErrorBlock_WithMessage_ContainsErrorStyle()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .WriteErrorBlock("Bir hata oluştu")
                .Build();

            html.Should().Contain("Bir hata oluştu");
            html.Should().Contain("#ef4444");
        }

        [TestMethod]
        public void WriteErrorBlock_WhenNull_DoesNotWriteAnything()
        {
            var builder = new EmailTemplateBuilder();

            string htmlWithError = builder.WriteHeader("Test").WriteErrorBlock("hata").Build();
            var builder2 = new EmailTemplateBuilder();
            string htmlWithoutError = builder2.WriteHeader("Test").WriteErrorBlock(null).Build();

            htmlWithError.Should().Contain("Hata:");
            htmlWithoutError.Should().NotContain("Hata:");
        }

        // ── InfoBlock ────────────────────────────────────────────────────

        [TestMethod]
        public void WriteInfoBlock_WithMessage_ContainsMutedText()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .WriteInfoBlock("Bilgi notu")
                .Build();

            html.Should().Contain("Bilgi notu");
        }

        [TestMethod]
        public void WriteInfoBlock_WhenNull_DoesNotWriteAnything()
        {
            var builder = new EmailTemplateBuilder();

            string htmlWith = builder.WriteHeader("Test").WriteInfoBlock("not").Build();
            var builder2 = new EmailTemplateBuilder();
            string htmlWithout = builder2.WriteHeader("Test").WriteInfoBlock(null).Build();

            htmlWith.Length.Should().BeGreaterThan(htmlWithout.Length);
        }

        // ── Footer ───────────────────────────────────────────────────────

        [TestMethod]
        public void Build_WhenFooterNotWritten_AutoAddsFooter()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder.WriteHeader("Test").Build();

            html.Should().Contain("Koru MsSql Yedek");
            html.Should().Contain("otomatik");
        }

        [TestMethod]
        public void WriteFooter_CalledTwice_WritesFooterOnlyOnce()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .WriteFooter()
                .WriteFooter()
                .Build();

            int footerCount = CountOccurrences(html, "otomatik gönderilmiştir");
            footerCount.Should().Be(1);
        }

        // ── Encode ───────────────────────────────────────────────────────

        [TestMethod]
        public void Encode_WithHtmlChars_ReturnsEncodedString()
        {
            string result = EmailTemplateBuilder.Encode("<script>alert('xss')</script>");

            result.Should().Contain("&lt;script&gt;");
            result.Should().NotContain("<script>");
        }

        [TestMethod]
        public void Encode_WhenNull_ReturnsEmpty()
        {
            string result = EmailTemplateBuilder.Encode(null);

            result.Should().BeEmpty();
        }

        // ── Static Color Helpers ─────────────────────────────────────────

        [TestMethod]
        [DataRow(true, "#10b981")]
        [DataRow(false, "#ef4444")]
        public void GetStatusColor_ReturnsCorrectColor(bool isSuccess, string expectedColor)
        {
            string color = EmailTemplateBuilder.GetStatusColor(isSuccess);

            color.Should().Be(expectedColor);
        }

        [TestMethod]
        public void GetSuccessColor_ReturnsGreen()
        {
            EmailTemplateBuilder.GetSuccessColor().Should().Be("#10b981");
        }

        [TestMethod]
        public void GetFailureColor_ReturnsRed()
        {
            EmailTemplateBuilder.GetFailureColor().Should().Be("#ef4444");
        }

        [TestMethod]
        public void GetWarningColor_ReturnsAmber()
        {
            EmailTemplateBuilder.GetWarningColor().Should().Be("#f59e0b");
        }

        // ── WriteSectionTitle ────────────────────────────────────────────

        [TestMethod]
        public void WriteSectionTitle_WhenCalled_ContainsTitleText()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .WriteSectionTitle("Detay Bölümü")
                .Build();

            html.Should().Contain("Detay B&#246;l&#252;m&#252;");
        }

        // ── Full integration (header + badge + table + footer) ───────────

        [TestMethod]
        public void FullTemplate_ContainsAllSections()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Koru MsSql Yedek", "Plan X")
                .WriteStatusBadge("Başarılı", true)
                .WriteSectionTitle("Özet")
                .BeginSummaryTable()
                .WriteTableRow("DB", "TestDB")
                .WriteTableRow("Durum", "OK", "#10b981")
                .EndTable()
                .WriteSectionTitle("Detay")
                .BeginDetailTable("Kolon1", "Kolon2")
                .WriteDetailRow(0, ("A", null), ("B", "#ef4444"))
                .EndDetailTable()
                .WriteErrorBlock("Test hatası")
                .Build();

            html.Should().Contain("Koru MsSql Yedek");
            html.Should().Contain("Plan X");
            html.Should().Contain("✓");
            html.Should().Contain("TestDB");
            html.Should().Contain("Test hatas");
            html.Should().Contain("otomatik");
        }

        // ── WriteRawHtml ─────────────────────────────────────────────────

        [TestMethod]
        public void WriteRawHtml_InsertsExactHtml()
        {
            var builder = new EmailTemplateBuilder();

            string html = builder
                .WriteHeader("Test")
                .WriteRawHtml("<p>Custom HTML</p>")
                .Build();

            html.Should().Contain("<p>Custom HTML</p>");
        }

        // ── Yardımcı ────────────────────────────────────────────────────

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int idx = 0;
            while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) != -1)
            {
                count++;
                idx += pattern.Length;
            }
            return count;
        }
    }
}
