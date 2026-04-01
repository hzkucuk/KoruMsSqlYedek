using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class BackupCancellationRegistryTests
    {
        private BackupCancellationRegistry _registry;

        [TestInitialize]
        public void Setup()
        {
            _registry = new BackupCancellationRegistry();
        }

        // ── Register / IsRunning ─────────────────────────────────────────────

        [TestMethod]
        public void Register_ValidPlanId_IsRunningReturnsTrue()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);

            _registry.IsRunning("plan-1").Should().BeTrue();
        }

        [TestMethod]
        public void IsRunning_UnregisteredPlanId_ReturnsFalse()
        {
            _registry.IsRunning("non-existent").Should().BeFalse();
        }

        [TestMethod]
        public void IsRunning_NullPlanId_ReturnsFalse()
        {
            _registry.IsRunning(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsRunning_EmptyPlanId_ReturnsFalse()
        {
            _registry.IsRunning("").Should().BeFalse();
        }

        [TestMethod]
        public void IsRunning_WhitespacePlanId_ReturnsFalse()
        {
            _registry.IsRunning("   ").Should().BeFalse();
        }

        // ── IsAnyRunning ─────────────────────────────────────────────────────

        [TestMethod]
        public void IsAnyRunning_EmptyRegistry_ReturnsFalse()
        {
            _registry.IsAnyRunning().Should().BeFalse();
        }

        [TestMethod]
        public void IsAnyRunning_WithRegisteredPlan_ReturnsTrue()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);

            _registry.IsAnyRunning().Should().BeTrue();
        }

        [TestMethod]
        public void IsAnyRunning_AllUnregistered_ReturnsFalse()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);
            _registry.Unregister("plan-1");

            _registry.IsAnyRunning().Should().BeFalse();
        }

        // ── Cancel ───────────────────────────────────────────────────────────

        [TestMethod]
        public void Cancel_RegisteredPlan_TokenIsCancelled()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);

            _registry.Cancel("plan-1");

            cts.IsCancellationRequested.Should().BeTrue();
        }

        [TestMethod]
        public void Cancel_UnregisteredPlan_DoesNotThrow()
        {
            // Bilinmeyen planId için Cancel çağrısı sessizce geçmeli
            var act = () => _registry.Cancel("unknown-plan");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Cancel_NullPlanId_DoesNotThrow()
        {
            var act = () => _registry.Cancel(null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Cancel_EmptyPlanId_DoesNotThrow()
        {
            var act = () => _registry.Cancel("");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Cancel_AlreadyCancelled_DoesNotThrow()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);
            cts.Cancel(); // zaten iptal edilmiş

            var act = () => _registry.Cancel("plan-1");
            act.Should().NotThrow();
        }

        // ── Unregister ───────────────────────────────────────────────────────

        [TestMethod]
        public void Unregister_RegisteredPlan_IsRunningReturnsFalse()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("plan-1", cts);

            _registry.Unregister("plan-1");

            _registry.IsRunning("plan-1").Should().BeFalse();
        }

        [TestMethod]
        public void Unregister_UnregisteredPlan_DoesNotThrow()
        {
            var act = () => _registry.Unregister("non-existent");
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Unregister_NullPlanId_DoesNotThrow()
        {
            var act = () => _registry.Unregister(null);
            act.Should().NotThrow();
        }

        // ── Register Overwrite ───────────────────────────────────────────────

        [TestMethod]
        public void Register_SamePlanIdTwice_ReplacesWithNewCts()
        {
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _registry.Register("plan-1", cts1);
            _registry.Register("plan-1", cts2);

            // Cancel ile yeni CTS iptal edilmeli
            _registry.Cancel("plan-1");

            cts2.IsCancellationRequested.Should().BeTrue();
            // Eski CTS'e dokunulmamış olmalı
            cts1.IsCancellationRequested.Should().BeFalse();
        }

        // ── Register null/empty guards ───────────────────────────────────────

        [TestMethod]
        public void Register_NullPlanId_DoesNotThrow()
        {
            using var cts = new CancellationTokenSource();
            var act = () => _registry.Register(null, cts);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Register_EmptyPlanId_DoesNotAddEntry()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("", cts);

            _registry.IsAnyRunning().Should().BeFalse();
        }

        // ── Multiple plans ───────────────────────────────────────────────────

        [TestMethod]
        public void MultiplePlans_RegisterAndCancel_OnlyCancelledPlanAffected()
        {
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _registry.Register("plan-1", cts1);
            _registry.Register("plan-2", cts2);

            _registry.Cancel("plan-1");

            cts1.IsCancellationRequested.Should().BeTrue();
            cts2.IsCancellationRequested.Should().BeFalse();
            _registry.IsRunning("plan-1").Should().BeTrue(); // hala kayıtlı, sadece iptal edildi
            _registry.IsRunning("plan-2").Should().BeTrue();
        }

        [TestMethod]
        public void MultiplePlans_UnregisterOne_OtherStillRunning()
        {
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _registry.Register("plan-1", cts1);
            _registry.Register("plan-2", cts2);

            _registry.Unregister("plan-1");

            _registry.IsRunning("plan-1").Should().BeFalse();
            _registry.IsRunning("plan-2").Should().BeTrue();
            _registry.IsAnyRunning().Should().BeTrue();
        }

        // ── Case insensitive PlanId ──────────────────────────────────────────

        [TestMethod]
        public void PlanId_CaseInsensitive_RegisterAndCancelWorks()
        {
            using var cts = new CancellationTokenSource();
            _registry.Register("Plan-ABC", cts);

            _registry.IsRunning("plan-abc").Should().BeTrue();

            _registry.Cancel("PLAN-ABC");
            cts.IsCancellationRequested.Should().BeTrue();
        }
    }
}
