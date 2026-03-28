using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Engine;
using MikroSqlDbYedek.Tests.Helpers;

namespace MikroSqlDbYedek.Tests
{
    [TestClass]
    [TestCategory("Integration")]
    public class PlanManagerTests
    {
        private PlanManager _planManager;

        [TestInitialize]
        public void Setup()
        {
            _planManager = new PlanManager();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Test planlarını temizle (Test Plan isimli planları sil)
        }

        [TestMethod]
        public void SavePlan_And_GetPlanById_Success()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();

            // Act
            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            // Assert
            loaded.Should().NotBeNull();
            loaded.PlanName.Should().Be(plan.PlanName);
            loaded.Databases.Should().HaveCount(2);
            loaded.SchemaVersion.Should().Be(2); // CurrentSchemaVersion

            // Cleanup
            _planManager.DeletePlan(plan.PlanId);
        }

        [TestMethod]
        public void DeletePlan_Existing_ReturnsTrue()
        {
            var plan = TestDataFactory.CreateValidPlan("ToDelete");
            _planManager.SavePlan(plan);

            bool deleted = _planManager.DeletePlan(plan.PlanId);

            deleted.Should().BeTrue();
            _planManager.GetPlanById(plan.PlanId).Should().BeNull();
        }

        [TestMethod]
        public void DeletePlan_NonExistent_ReturnsFalse()
        {
            bool deleted = _planManager.DeletePlan("non-existent-id");
            deleted.Should().BeFalse();
        }

        [TestMethod]
        public void GetPlanById_NullOrEmpty_ReturnsNull()
        {
            _planManager.GetPlanById(null).Should().BeNull();
            _planManager.GetPlanById(string.Empty).Should().BeNull();
        }

        [TestMethod]
        public void SavePlan_NullPlan_ThrowsArgumentNullException()
        {
            Action act = () => _planManager.SavePlan(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void SavePlan_SetsSchemaVersion2_And_UpdatesLastModified()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan();
            var beforeSave = DateTime.UtcNow;

            // Act
            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            // Assert
            loaded.SchemaVersion.Should().Be(2);
            loaded.LastModifiedAt.Should().BeOnOrAfter(beforeSave);

            // Cleanup
            _planManager.DeletePlan(plan.PlanId);
        }

        [TestMethod]
        public void SchemaMigration_V1Plan_UpgradesToV2()
        {
            // Arrange — v1 format plan JSON'u doğrudan dosyaya yaz
            string planId = Guid.NewGuid().ToString();
            var v1Json = new JObject
            {
                ["planId"] = planId,
                ["planName"] = "V1 Test Plan",
                ["isEnabled"] = true,
                ["schemaVersion"] = 1,
                ["sqlConnection"] = new JObject
                {
                    ["server"] = "localhost",
                    ["authMode"] = 0
                },
                ["databases"] = new JArray("TestDB"),
                ["strategy"] = new JObject
                {
                    ["type"] = 0,
                    ["fullSchedule"] = "0 0 2 ? * SUN",
                    ["autoPromoteToFullAfter"] = 7
                },
                ["localPath"] = @"C:\TestBackups"
                // v1'de fileBackup ve verifyAfterBackup alanları YOK
            };

            string filePath = PathHelper.GetPlanFilePath(planId);
            PathHelper.EnsureDirectoriesExist();
            File.WriteAllText(filePath, v1Json.ToString());

            // Act — PlanManager okurken otomatik migration yapmalı
            var loaded = _planManager.GetPlanById(planId);

            // Assert
            loaded.Should().NotBeNull();
            loaded.SchemaVersion.Should().Be(2);
            loaded.VerifyAfterBackup.Should().BeTrue(); // v2'de eklenen alan, varsayılan true
            loaded.PlanName.Should().Be("V1 Test Plan");
            loaded.Databases.Should().Contain("TestDB");

            // Migration sonrası dosya da güncellenmiş olmalı
            string updatedJson = File.ReadAllText(filePath);
            var updatedJObj = JObject.Parse(updatedJson);
            updatedJObj["schemaVersion"].Value<int>().Should().Be(2);

            // Cleanup
            _planManager.DeletePlan(planId);
        }

        [TestMethod]
        public void SchemaMigration_V2Plan_NoChanges()
        {
            // Arrange — zaten v2 format plan
            var plan = TestDataFactory.CreateValidPlan("V2 Test Plan");

            // Act
            _planManager.SavePlan(plan);
            var loaded = _planManager.GetPlanById(plan.PlanId);

            // Assert
            loaded.SchemaVersion.Should().Be(2);
            loaded.PlanName.Should().Be("V2 Test Plan");

            // Cleanup
            _planManager.DeletePlan(plan.PlanId);
        }

        [TestMethod]
        public void GetAllPlans_ReturnsMultiplePlans()
        {
            // Arrange
            var plan1 = TestDataFactory.CreateValidPlan("GetAll_Plan1");
            var plan2 = TestDataFactory.CreateValidPlan("GetAll_Plan2");
            _planManager.SavePlan(plan1);
            _planManager.SavePlan(plan2);

            // Act
            var allPlans = _planManager.GetAllPlans();

            // Assert — en az 2 plan olmalı (başka testlerden kalanlar olabilir)
            allPlans.Should().Contain(p => p.PlanId == plan1.PlanId);
            allPlans.Should().Contain(p => p.PlanId == plan2.PlanId);

            // Cleanup
            _planManager.DeletePlan(plan1.PlanId);
            _planManager.DeletePlan(plan2.PlanId);
        }

        [TestMethod]
        public void ExportPlan_And_ImportPlan_RoundTrip()
        {
            // Arrange
            var plan = TestDataFactory.CreateValidPlan("Export Test");
            _planManager.SavePlan(plan);

            string exportPath = Path.Combine(Path.GetTempPath(), $"export_test_{Guid.NewGuid()}.json");

            try
            {
                // Act — Export
                _planManager.ExportPlan(plan.PlanId, exportPath);
                File.Exists(exportPath).Should().BeTrue();

                // Act — Import (yeni PlanId atanır)
                var imported = _planManager.ImportPlan(exportPath);

                // Assert
                imported.Should().NotBeNull();
                imported.PlanId.Should().NotBe(plan.PlanId); // yeni GUID
                imported.PlanName.Should().Be("Export Test");
                imported.Databases.Should().BeEquivalentTo(plan.Databases);

                // Cleanup
                _planManager.DeletePlan(imported.PlanId);
            }
            finally
            {
                _planManager.DeletePlan(plan.PlanId);
                if (File.Exists(exportPath))
                    File.Delete(exportPath);
            }
        }

        [TestMethod]
        public void ImportPlan_InvalidFile_ThrowsException()
        {
            string invalidPath = Path.Combine(Path.GetTempPath(), "non_existent_plan.json");

            Action act = () => _planManager.ImportPlan(invalidPath);
            act.Should().Throw<FileNotFoundException>();
        }
    }
}
