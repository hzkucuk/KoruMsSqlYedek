using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class PipeProtocolTests
    {
        // ── PipeSerializer Roundtrip ─────────────────────────────────────────

        [TestMethod]
        public void Serialize_ManualBackupCommand_RoundtripPreservesAllFields()
        {
            var original = new ManualBackupCommand
            {
                PlanId = "plan-123",
                BackupType = "Full"
            };

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as ManualBackupCommand;

            deserialized.Should().NotBeNull();
            deserialized.Type.Should().Be(PipeMessageType.ManualBackup);
            deserialized.PlanId.Should().Be("plan-123");
            deserialized.BackupType.Should().Be("Full");
        }

        [TestMethod]
        public void Serialize_CancelBackupCommand_RoundtripPreservesAllFields()
        {
            var original = new CancelBackupCommand
            {
                PlanId = "plan-456"
            };

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as CancelBackupCommand;

            deserialized.Should().NotBeNull();
            deserialized.Type.Should().Be(PipeMessageType.CancelBackup);
            deserialized.PlanId.Should().Be("plan-456");
        }

        [TestMethod]
        public void Serialize_RequestStatusCommand_RoundtripPreservesType()
        {
            var original = new RequestStatusCommand();

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as RequestStatusCommand;

            deserialized.Should().NotBeNull();
            deserialized.Type.Should().Be(PipeMessageType.RequestStatus);
        }

        [TestMethod]
        public void Serialize_BackupActivityMessage_RoundtripPreservesAllFields()
        {
            var original = new BackupActivityMessage
            {
                PlanId = "plan-789",
                PlanName = "Günlük Yedek",
                DatabaseName = "MyDB",
                ActivityType = BackupActivityType.CloudUploadProgress,
                CurrentIndex = 2,
                TotalCount = 5,
                Message = "Upload devam ediyor",
                StepName = "Bulut Yükleme",
                CloudTargetName = "Google Drive",
                CloudTargetIndex = 1,
                CloudTargetTotal = 3,
                ProgressPercent = 42,
                IsSuccess = false,
                BytesSent = 1024 * 1024 * 50,
                BytesTotal = 1024L * 1024 * 200,
                SpeedBytesPerSecond = 1024 * 1024 * 5,
                ToastEnabled = false
            };

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as BackupActivityMessage;

            deserialized.Should().NotBeNull();
            deserialized.PlanId.Should().Be("plan-789");
            deserialized.PlanName.Should().Be("Günlük Yedek");
            deserialized.DatabaseName.Should().Be("MyDB");
            deserialized.ActivityType.Should().Be(BackupActivityType.CloudUploadProgress);
            deserialized.CurrentIndex.Should().Be(2);
            deserialized.TotalCount.Should().Be(5);
            deserialized.Message.Should().Be("Upload devam ediyor");
            deserialized.StepName.Should().Be("Bulut Yükleme");
            deserialized.CloudTargetName.Should().Be("Google Drive");
            deserialized.CloudTargetIndex.Should().Be(1);
            deserialized.CloudTargetTotal.Should().Be(3);
            deserialized.ProgressPercent.Should().Be(42);
            deserialized.IsSuccess.Should().BeFalse();
            deserialized.BytesSent.Should().Be(1024 * 1024 * 50);
            deserialized.BytesTotal.Should().Be(1024L * 1024 * 200);
            deserialized.SpeedBytesPerSecond.Should().Be(1024 * 1024 * 5);
            deserialized.ToastEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void Serialize_ServiceStatusMessage_RoundtripPreservesAllFields()
        {
            var original = new ServiceStatusMessage
            {
                IsRunning = true,
                NextFireTimes = new Dictionary<string, string>
                {
                    ["plan-1"] = "19.07.2025 02:00",
                    ["plan-2"] = "20.07.2025 03:30"
                }
            };

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as ServiceStatusMessage;

            deserialized.Should().NotBeNull();
            deserialized.Type.Should().Be(PipeMessageType.ServiceStatus);
            deserialized.IsRunning.Should().BeTrue();
            deserialized.NextFireTimes.Should().HaveCount(2);
            deserialized.NextFireTimes["plan-1"].Should().Be("19.07.2025 02:00");
            deserialized.NextFireTimes["plan-2"].Should().Be("20.07.2025 03:30");
        }

        // ── Deserialize Edge Cases ───────────────────────────────────────────

        [TestMethod]
        public void Deserialize_NullInput_ReturnsNull()
        {
            PipeSerializer.Deserialize(null).Should().BeNull();
        }

        [TestMethod]
        public void Deserialize_EmptyString_ReturnsNull()
        {
            PipeSerializer.Deserialize("").Should().BeNull();
        }

        [TestMethod]
        public void Deserialize_WhitespaceOnly_ReturnsNull()
        {
            PipeSerializer.Deserialize("   ").Should().BeNull();
        }

        [TestMethod]
        public void Deserialize_MalformedJson_ReturnsNull()
        {
            PipeSerializer.Deserialize("{not valid json!!}").Should().BeNull();
        }

        [TestMethod]
        public void Deserialize_UnknownType_ReturnsNull()
        {
            string json = """{"type":"UnknownMessageType","data":"test"}""";
            PipeSerializer.Deserialize(json).Should().BeNull();
        }

        [TestMethod]
        public void Deserialize_MissingTypeField_ReturnsNull()
        {
            string json = """{"planId":"plan-1","backupType":"Full"}""";
            PipeSerializer.Deserialize(json).Should().BeNull();
        }

        // ── BackupActivityMessage FromArgs / ToArgs ──────────────────────────

        [TestMethod]
        public void BackupActivityMessage_FromArgs_CopiesAllProperties()
        {
            var args = new BackupActivityEventArgs
            {
                PlanId = "plan-abc",
                PlanName = "Test Plan",
                DatabaseName = "TestDB",
                ActivityType = BackupActivityType.DatabaseProgress,
                CurrentIndex = 3,
                TotalCount = 10,
                Message = "İlerleme mesajı",
                StepName = "SQL Yedekleme",
                CloudTargetName = "FTP Server",
                CloudTargetIndex = 2,
                CloudTargetTotal = 4,
                ProgressPercent = 75,
                IsSuccess = true,
                BytesSent = 500_000,
                BytesTotal = 1_000_000,
                SpeedBytesPerSecond = 250_000,
                HasFileBackup = true,
                ToastEnabled = false
            };

            var msg = BackupActivityMessage.FromArgs(args);

            msg.PlanId.Should().Be("plan-abc");
            msg.PlanName.Should().Be("Test Plan");
            msg.DatabaseName.Should().Be("TestDB");
            msg.ActivityType.Should().Be(BackupActivityType.DatabaseProgress);
            msg.CurrentIndex.Should().Be(3);
            msg.TotalCount.Should().Be(10);
            msg.Message.Should().Be("İlerleme mesajı");
            msg.StepName.Should().Be("SQL Yedekleme");
            msg.CloudTargetName.Should().Be("FTP Server");
            msg.CloudTargetIndex.Should().Be(2);
            msg.CloudTargetTotal.Should().Be(4);
            msg.ProgressPercent.Should().Be(75);
            msg.IsSuccess.Should().BeTrue();
            msg.BytesSent.Should().Be(500_000);
            msg.BytesTotal.Should().Be(1_000_000);
            msg.SpeedBytesPerSecond.Should().Be(250_000);
            msg.ToastEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void BackupActivityMessage_ToArgs_CopiesAllProperties()
        {
            var msg = new BackupActivityMessage
            {
                PlanId = "plan-def",
                PlanName = "Restore Plan",
                DatabaseName = "ProdDB",
                ActivityType = BackupActivityType.Completed,
                CurrentIndex = 5,
                TotalCount = 5,
                Message = "Tamamlandı",
                StepName = "Bulut Yükleme",
                CloudTargetName = "Google Drive",
                CloudTargetIndex = 1,
                CloudTargetTotal = 1,
                ProgressPercent = 100,
                IsSuccess = true,
                BytesSent = 2_000_000,
                BytesTotal = 2_000_000,
                SpeedBytesPerSecond = 1_000_000,
                ToastEnabled = true
            };

            var args = msg.ToArgs();

            args.PlanId.Should().Be("plan-def");
            args.PlanName.Should().Be("Restore Plan");
            args.DatabaseName.Should().Be("ProdDB");
            args.ActivityType.Should().Be(BackupActivityType.Completed);
            args.CurrentIndex.Should().Be(5);
            args.TotalCount.Should().Be(5);
            args.Message.Should().Be("Tamamlandı");
            args.StepName.Should().Be("Bulut Yükleme");
            args.CloudTargetName.Should().Be("Google Drive");
            args.CloudTargetIndex.Should().Be(1);
            args.CloudTargetTotal.Should().Be(1);
            args.ProgressPercent.Should().Be(100);
            args.IsSuccess.Should().BeTrue();
            args.BytesSent.Should().Be(2_000_000);
            args.BytesTotal.Should().Be(2_000_000);
            args.SpeedBytesPerSecond.Should().Be(1_000_000);
            args.ToastEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void BackupActivityMessage_FromArgs_ToArgs_FullRoundtrip()
        {
            var original = new BackupActivityEventArgs
            {
                PlanId = "roundtrip-plan",
                PlanName = "Roundtrip Test",
                DatabaseName = "RoundtripDB",
                ActivityType = BackupActivityType.CloudUploadCompleted,
                CurrentIndex = 1,
                TotalCount = 1,
                Message = "Test roundtrip",
                StepName = "Test Step",
                CloudTargetName = "S3",
                CloudTargetIndex = 1,
                CloudTargetTotal = 1,
                ProgressPercent = 100,
                IsSuccess = true,
                BytesSent = 999,
                BytesTotal = 999,
                SpeedBytesPerSecond = 333,
                ToastEnabled = true
            };

            var msg = BackupActivityMessage.FromArgs(original);
            string json = PipeSerializer.Serialize(msg);
            var deserialized = PipeSerializer.Deserialize(json) as BackupActivityMessage;
            var roundtripped = deserialized.ToArgs();

            roundtripped.PlanId.Should().Be(original.PlanId);
            roundtripped.PlanName.Should().Be(original.PlanName);
            roundtripped.DatabaseName.Should().Be(original.DatabaseName);
            roundtripped.ActivityType.Should().Be(original.ActivityType);
            roundtripped.CurrentIndex.Should().Be(original.CurrentIndex);
            roundtripped.TotalCount.Should().Be(original.TotalCount);
            roundtripped.Message.Should().Be(original.Message);
            roundtripped.StepName.Should().Be(original.StepName);
            roundtripped.CloudTargetName.Should().Be(original.CloudTargetName);
            roundtripped.CloudTargetIndex.Should().Be(original.CloudTargetIndex);
            roundtripped.CloudTargetTotal.Should().Be(original.CloudTargetTotal);
            roundtripped.ProgressPercent.Should().Be(original.ProgressPercent);
            roundtripped.IsSuccess.Should().Be(original.IsSuccess);
            roundtripped.BytesSent.Should().Be(original.BytesSent);
            roundtripped.BytesTotal.Should().Be(original.BytesTotal);
            roundtripped.SpeedBytesPerSecond.Should().Be(original.SpeedBytesPerSecond);
            roundtripped.ToastEnabled.Should().Be(original.ToastEnabled);
        }

        // ── ServiceStatusMessage edge cases ──────────────────────────────────

        [TestMethod]
        public void ServiceStatusMessage_EmptyNextFireTimes_RoundtripPreserved()
        {
            var original = new ServiceStatusMessage
            {
                IsRunning = false,
                NextFireTimes = new Dictionary<string, string>()
            };

            string json = PipeSerializer.Serialize(original);
            var deserialized = PipeSerializer.Deserialize(json) as ServiceStatusMessage;

            deserialized.Should().NotBeNull();
            deserialized.IsRunning.Should().BeFalse();
            deserialized.NextFireTimes.Should().BeEmpty();
        }

        // ── BackupActivityMessage default ToastEnabled ───────────────────────

        [TestMethod]
        public void BackupActivityMessage_DefaultToastEnabled_IsTrue()
        {
            var msg = new BackupActivityMessage();
            msg.ToastEnabled.Should().BeTrue();
        }

        // ── All ActivityType enum values serialize correctly ─────────────────

        [DataTestMethod]
        [DataRow(BackupActivityType.Started)]
        [DataRow(BackupActivityType.DatabaseProgress)]
        [DataRow(BackupActivityType.StepChanged)]
        [DataRow(BackupActivityType.CloudUploadStarted)]
        [DataRow(BackupActivityType.CloudUploadProgress)]
        [DataRow(BackupActivityType.CloudUploadCompleted)]
        [DataRow(BackupActivityType.Completed)]
        [DataRow(BackupActivityType.Failed)]
        [DataRow(BackupActivityType.Cancelled)]
        public void BackupActivityMessage_AllActivityTypes_RoundtripCorrectly(BackupActivityType activityType)
        {
            var msg = new BackupActivityMessage { ActivityType = activityType };

            string json = PipeSerializer.Serialize(msg);
            var deserialized = PipeSerializer.Deserialize(json) as BackupActivityMessage;

            deserialized.Should().NotBeNull();
            deserialized.ActivityType.Should().Be(activityType);
        }
    }
}
