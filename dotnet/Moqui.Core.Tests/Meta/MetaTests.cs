using System.Collections.Generic;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Meta
{
    /// <summary>혈액 포인트 보상, 스킬 구매, 저장 (spec/09 §1, §2, §3).</summary>
    public class MetaTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

            public bool Exists(string fileName) => Files.ContainsKey(fileName);

            public string Read(string fileName) => Files[fileName];

            public void Write(string fileName, string text) => Files[fileName] = text;

            public void Move(string fromFileName, string toFileName)
            {
                Files[toFileName] = Files[fromFileName];
                Files.Remove(fromFileName);
            }

            public void Delete(string fileName) => Files.Remove(fileName);
        }

        private static IEnumerable<TestCaseData> RewardCombinations()
        {
            foreach (bool noFrenzy in new[] { false, true })
            {
                foreach (bool careful in new[] { false, true })
                {
                    foreach (bool fast in new[] { false, true })
                    {
                        yield return new TestCaseData(noFrenzy, careful, fast).SetName($"Reward(noFrenzy={noFrenzy}, careful={careful}, par={fast})");
                    }
                }
            }
        }

        [TestCaseSource(nameof(RewardCombinations))]
        public void Reward_AllEightCombinations(bool noFrenzy, bool careful, bool withinPar)
        {
            var tuning = Tuning;
            var calculator = new RewardCalculator(tuning);
            float par = calculator.ParTime(2);
            int carefulMax = tuning.GetInt("meta.carefulBiteMax");
            var result = new StageResult(
                SimulationTime.ToTicks(withinPar ? par - 1f : par + 1f),
                noFrenzy ? 0 : 1,
                careful ? carefulMax : carefulMax + 1);

            var reward = calculator.Compute(2, result);

            int expected = tuning.GetInt("meta.clearReward")
                + (noFrenzy ? tuning.GetInt("meta.noFrenzyBonus") : 0)
                + (careful ? tuning.GetInt("meta.carefulBiteBonus") : 0)
                + (withinPar ? tuning.GetInt("meta.parTimeBonus") : 0);
            Assert.That(reward.Total, Is.EqualTo(expected));
        }

        [Test]
        public void Purchase_NeedsEnoughPoints_AndStopsAtMaxLevel()
        {
            var shop = new SkillShop(Tuning);
            var save = new SaveData { BloodPoints = 59 };
            Assert.That(shop.TryPurchase(save, SkillCatalog.CompoundEyes), Is.EqualTo(PurchaseResult.NotEnoughPoints));
            Assert.That(save.BloodPoints, Is.EqualTo(59));
            Assert.That(save.SkillLevel(SkillCatalog.CompoundEyes), Is.EqualTo(0));

            save.BloodPoints = 60 + 120;
            Assert.That(shop.TryPurchase(save, SkillCatalog.CompoundEyes), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(shop.TryPurchase(save, SkillCatalog.CompoundEyes), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(save.SkillLevel(SkillCatalog.CompoundEyes), Is.EqualTo(2));
            Assert.That(save.BloodPoints, Is.EqualTo(0));

            save.BloodPoints = 10000;
            Assert.That(shop.NextCost(save, SkillCatalog.CompoundEyes), Is.Null);
            Assert.That(shop.TryPurchase(save, SkillCatalog.CompoundEyes), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(save.BloodPoints, Is.EqualTo(10000));
        }

        [Test]
        public void Costs_FollowTierTables()
        {
            var shop = new SkillShop(Tuning);
            var save = new SaveData();
            Assert.That(shop.NextCost(save, SkillCatalog.SwiftWings), Is.EqualTo(60));
            Assert.That(shop.NextCost(save, SkillCatalog.SilentWings), Is.EqualTo(80));
            save.SkillLevels[SkillCatalog.SilentWings] = 2;
            Assert.That(shop.NextCost(save, SkillCatalog.SilentWings), Is.EqualTo(260));
        }

        [Test]
        public void Equip_OnlyOwnedActiveSkills()
        {
            var shop = new SkillShop(Tuning);
            var save = new SaveData();
            Assert.That(shop.Equip(save, SkillCatalog.DecoyCharm), Is.False, "not owned");
            save.SkillLevels[SkillCatalog.DecoyCharm] = 1;
            save.SkillLevels[SkillCatalog.SwiftWings] = 1;
            Assert.That(shop.Equip(save, SkillCatalog.SwiftWings), Is.False, "passive");
            Assert.That(shop.Equip(save, SkillCatalog.DecoyCharm), Is.True);
            Assert.That(save.Loadout.ActiveLevel(SkillCatalog.DecoyCharm), Is.EqualTo(1));
        }

        [Test]
        public void StageUnlock_RequiresPreviousClear_RecordsKeepBestValues()
        {
            var save = new SaveData();
            var calculator = new RewardCalculator(Tuning);
            Assert.That(save.IsUnlocked(1), Is.True);
            Assert.That(save.IsUnlocked(2), Is.False);

            var slow = new StageResult(SimulationTime.ToTicks(200f), 2, 4);
            save.RecordClear("stage01", slow, calculator.Compute(1, slow));
            var fast = new StageResult(SimulationTime.ToTicks(100f), 0, 1);
            save.RecordClear("stage01", fast, calculator.Compute(1, fast));
            var middle = new StageResult(SimulationTime.ToTicks(150f), 1, 3);
            save.RecordClear("stage01", middle, calculator.Compute(1, middle));

            var record = save.Record("stage01");
            Assert.That(save.IsUnlocked(2), Is.True);
            Assert.That(save.IsUnlocked(3), Is.False);
            Assert.That(record.BestSeconds, Is.EqualTo(100f).Within(0.01f));
            Assert.That(record.NoFrenzy, Is.True);
            Assert.That(record.MinBiteMarks, Is.EqualTo(1));
            Assert.That(save.BloodPoints, Is.EqualTo(calculator.Compute(1, slow).Total + calculator.Compute(1, fast).Total + calculator.Compute(1, middle).Total), "paid every clear");
        }

        [Test]
        public void SaveLoad_RoundTrip_AllFieldsEqual()
        {
            var storage = new MemoryStorage();
            var store = new SaveStore(storage);
            var save = new SaveData { BloodPoints = 345, EquippedActive = SkillCatalog.DecoyCharm };
            save.SkillLevels[SkillCatalog.DecoyCharm] = 2;
            save.SkillLevels[SkillCatalog.Stamina] = 3;
            save.Stages["stage01"] = new StageRecord { Cleared = true, BestSeconds = 123.45f, NoFrenzy = true, MinBiteMarks = 2 };
            save.Stages["stage02"] = new StageRecord { Cleared = true, BestSeconds = 99.5f, NoFrenzy = false, MinBiteMarks = 4 };

            store.Save(save);
            var loaded = store.Load();

            Assert.That(store.LastLoadWasCorrupt, Is.False);
            Assert.That(SaveSerializer.Serialize(loaded), Is.EqualTo(SaveSerializer.Serialize(save)));
            Assert.That(loaded.FormatVersion, Is.EqualTo(SaveData.CurrentFormatVersion));
            Assert.That(loaded.BloodPoints, Is.EqualTo(345));
            Assert.That(loaded.EquippedActive, Is.EqualTo(SkillCatalog.DecoyCharm));
            Assert.That(loaded.SkillLevels, Is.EquivalentTo(save.SkillLevels));
            Assert.That(loaded.Record("stage01").BestSeconds, Is.EqualTo(123.45f));
            Assert.That(loaded.Record("stage02").MinBiteMarks, Is.EqualTo(4));
        }

        [TestCase("{ not json")]
        [TestCase("{\"formatVersion\": 999}")]
        [TestCase("{\"formatVersion\": 1, \"bloodPoints\": \"many\"}")]
        [TestCase("")]
        public void CorruptSave_StartsWithDefaults_KeepsCorruptFile(string content)
        {
            var storage = new MemoryStorage();
            storage.Files[SaveStore.FileName] = content;
            var store = new SaveStore(storage);

            SaveData loaded = null;
            Assert.DoesNotThrow(() => loaded = store.Load());

            Assert.That(store.LastLoadWasCorrupt, Is.True);
            Assert.That(loaded.BloodPoints, Is.EqualTo(0));
            Assert.That(storage.Files[SaveStore.CorruptFileName], Is.EqualTo(content));
            Assert.That(storage.Exists(SaveStore.FileName), Is.False);
        }

        [Test]
        public void MissingSave_StartsWithDefaults_Reset_DeletesFile()
        {
            var storage = new MemoryStorage();
            var store = new SaveStore(storage);
            Assert.That(store.Load().BloodPoints, Is.EqualTo(0));
            Assert.That(store.LastLoadWasCorrupt, Is.False);

            store.Save(new SaveData { BloodPoints = 50 });
            Assert.That(store.Reset().BloodPoints, Is.EqualTo(0));
            Assert.That(storage.Exists(SaveStore.FileName), Is.False);
        }
    }
}
