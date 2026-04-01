using System;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KoruMsSqlYedek.Engine.FileBackup;

namespace KoruMsSqlYedek.Tests
{
    /// <summary>
    /// VssSnapshotService birim testleri.
    /// Not: Gerçek VSS API çağrıları SYSTEM/admin yetkisi gerektirir;
    /// bu testler yetki gerektirmeyen kenar durumlarını ve kontrat davranışlarını doğrular.
    /// Reflection ile iç _snapshots dictionary'sine erişilerek path mapping testleri yapılır.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class VssSnapshotServiceTests
    {
        private static readonly FieldInfo SnapshotsField = typeof(VssSnapshotService)
            .GetField("_snapshots", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly Type SnapshotInfoType = typeof(VssSnapshotService)
            .GetNestedType("VssSnapshotInfo", BindingFlags.NonPublic)!;

        private VssSnapshotService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new VssSnapshotService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service?.Dispose();
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            _service.Dispose();

            Action secondDispose = () => _service.Dispose();

            secondDispose.Should().NotThrow("ikinci Dispose çağrısı güvenli olmalıdır");
        }

        [TestMethod]
        public void Dispose_CleansUpAllSnapshots()
        {
            InjectSnapshot(_service, Guid.NewGuid(), @"C:\", @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\");

            _service.Dispose();

            GetSnapshotCount(_service).Should().Be(0, "Dispose tüm snapshot'ları temizlemelidir");
        }

        // ── CreateSnapshot — Argüman doğrulama ──────────────────────────────

        [TestMethod]
        public void CreateSnapshot_NullVolumePath_ThrowsArgumentNullException()
        {
            Action act = () => _service.CreateSnapshot(null);

            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("volumePath");
        }

        [TestMethod]
        public void CreateSnapshot_EmptyVolumePath_ThrowsArgumentNullException()
        {
            Action act = () => _service.CreateSnapshot(string.Empty);

            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("volumePath");
        }

        [TestMethod]
        public void CreateSnapshot_CancelledToken_ThrowsException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // AlphaVSS resolver kaydından sonra iptal kontrolü yapılır.
            // AlphaVSS DLL yoksa farklı exception fırlatabilir.
            Action act = () => _service.CreateSnapshot(@"C:\", cts.Token);

            act.Should().Throw<Exception>(
                "iptal edilmiş token veya eksik AlphaVSS DLL ile çağrı exception fırlatmalıdır");
        }

        // ── GetSnapshotFilePath ─────────────────────────────────────────────

        [TestMethod]
        public void GetSnapshotFilePath_UnknownSnapshotId_ThrowsInvalidOperationException()
        {
            var unknownId = Guid.NewGuid();

            Action act = () => _service.GetSnapshotFilePath(unknownId, @"C:\Users\data.pst");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{unknownId}*");
        }

        [TestMethod]
        public void GetSnapshotFilePath_ValidSnapshot_ReturnsCorrectShadowPath()
        {
            var id = Guid.NewGuid();
            string devicePath = @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1\";
            InjectSnapshot(_service, id, @"C:\", devicePath);

            string result = _service.GetSnapshotFilePath(id, @"C:\Users\Documents\report.docx");

            result.Should().StartWith(devicePath);
            result.Should().Contain(@"Users\Documents\report.docx");
        }

        [TestMethod]
        public void GetSnapshotFilePath_RootFile_ReturnsDevicePathPlusFileName()
        {
            var id = Guid.NewGuid();
            string devicePath = @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy2\";
            InjectSnapshot(_service, id, @"D:\", devicePath);

            string result = _service.GetSnapshotFilePath(id, @"D:\backup.bak");

            result.Should().Be(devicePath + @"backup.bak");
        }

        [TestMethod]
        public void GetSnapshotFilePath_NestedPath_PreservesRelativeStructure()
        {
            var id = Guid.NewGuid();
            string devicePath = @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy3\";
            InjectSnapshot(_service, id, @"E:\", devicePath);

            string result = _service.GetSnapshotFilePath(id, @"E:\Folder\Sub\Deep\file.pst");

            result.Should().Contain(@"Folder\Sub\Deep\file.pst");
        }

        [TestMethod]
        public void GetSnapshotFilePath_DifferentVolumes_ReturnsCorrectPaths()
        {
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();
            InjectSnapshot(_service, idC, @"C:\", @"\\?\device\shadow1\");
            InjectSnapshot(_service, idD, @"D:\", @"\\?\device\shadow2\");

            string resultC = _service.GetSnapshotFilePath(idC, @"C:\file1.txt");
            string resultD = _service.GetSnapshotFilePath(idD, @"D:\file2.txt");

            resultC.Should().Contain("shadow1");
            resultD.Should().Contain("shadow2");
        }

        // ── DeleteSnapshot ──────────────────────────────────────────────────

        [TestMethod]
        public void DeleteSnapshot_UnknownId_DoesNotThrow()
        {
            Action act = () => _service.DeleteSnapshot(Guid.NewGuid());

            act.Should().NotThrow("bilinmeyen snapshot ID ile silme güvenli olmalıdır");
        }

        [TestMethod]
        public void DeleteSnapshot_KnownId_RemovesFromDictionary()
        {
            var id = Guid.NewGuid();
            InjectSnapshot(_service, id, @"C:\", @"\\?\device\");

            _service.DeleteSnapshot(id);

            GetSnapshotCount(_service).Should().Be(0);
        }

        [TestMethod]
        public void DeleteSnapshot_OneOfMultiple_OnlyRemovesTarget()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            InjectSnapshot(_service, id1, @"C:\", @"\\?\device1\");
            InjectSnapshot(_service, id2, @"D:\", @"\\?\device2\");

            _service.DeleteSnapshot(id1);

            GetSnapshotCount(_service).Should().Be(1, "yalnızca hedef snapshot silinmeli");
            // id2 hâlâ erişilebilir olmalı
            Action act = () => _service.GetSnapshotFilePath(id2, @"D:\test.txt");
            act.Should().NotThrow();
        }

        // ── DeleteAllSnapshots ──────────────────────────────────────────────

        [TestMethod]
        public void DeleteAllSnapshots_EmptyDictionary_DoesNotThrow()
        {
            Action act = () => _service.DeleteAllSnapshots();

            act.Should().NotThrow("boş dictionary ile çağrı güvenli olmalıdır");
        }

        [TestMethod]
        public void DeleteAllSnapshots_MultipleSnapshots_ClearsAll()
        {
            InjectSnapshot(_service, Guid.NewGuid(), @"C:\", @"\\?\device1\");
            InjectSnapshot(_service, Guid.NewGuid(), @"D:\", @"\\?\device2\");
            InjectSnapshot(_service, Guid.NewGuid(), @"E:\", @"\\?\device3\");

            _service.DeleteAllSnapshots();

            GetSnapshotCount(_service).Should().Be(0, "tüm snapshot'lar silinmelidir");
        }

        // ── IsAvailable ─────────────────────────────────────────────────────

        [TestMethod]
        public void IsAvailable_ReturnsBoolean_DoesNotThrow()
        {
            Action act = () => _service.IsAvailable();

            act.Should().NotThrow("IsAvailable güvenli şekilde false dönmeli, exception fırlatmamalı");
        }

        // ── IVssService kontrat testleri ────────────────────────────────────

        [TestMethod]
        public void Service_ImplementsIVssService()
        {
            _service.Should().BeAssignableTo<Core.Interfaces.IVssService>(
                "VssSnapshotService IVssService arayüzünü implemente etmelidir");
        }

        [TestMethod]
        public void Service_ImplementsIDisposable()
        {
            _service.Should().BeAssignableTo<IDisposable>(
                "VssSnapshotService IDisposable arayüzünü implemente etmelidir");
        }

        // ── State after Dispose ─────────────────────────────────────────────

        [TestMethod]
        public void GetSnapshotFilePath_AfterDispose_ThrowsInvalidOperation()
        {
            var id = Guid.NewGuid();
            InjectSnapshot(_service, id, @"C:\", @"\\?\device\");
            _service.Dispose();

            // Dispose tüm snapshot'ları temizlediği için artık bulunamaz
            Action act = () => _service.GetSnapshotFilePath(id, @"C:\file.txt");

            act.Should().Throw<InvalidOperationException>();
        }

        // ── Yardımcı metotlar ───────────────────────────────────────────────

        /// <summary>
        /// Reflection ile VssSnapshotInfo oluşturup iç _snapshots dictionary'sine ekler.
        /// </summary>
        private static void InjectSnapshot(VssSnapshotService service, Guid id, string volumePath, string devicePath)
        {
            var info = Activator.CreateInstance(SnapshotInfoType)!;
            SnapshotInfoType.GetProperty("VolumePath")!.SetValue(info, volumePath);
            SnapshotInfoType.GetProperty("SnapshotDevicePath")!.SetValue(info, devicePath);
            // BackupComponents null bırakılır — DeleteSnapshot dispose çağrısını güvenle atlar

            var dict = SnapshotsField.GetValue(service)!;
            var addMethod = dict.GetType().GetMethod("Add")!;
            addMethod.Invoke(dict, new object[] { id, info });
        }

        /// <summary>
        /// Reflection ile iç _snapshots dictionary'sindeki eleman sayısını döndürür.
        /// </summary>
        private static int GetSnapshotCount(VssSnapshotService service)
        {
            var dict = SnapshotsField.GetValue(service)!;
            var countProp = dict.GetType().GetProperty("Count")!;
            return (int)countProp.GetValue(dict)!;
        }
    }
}
