using System.Linq;
using Moqui.Core.Bots;
using Moqui.Core.Meta;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Meta
{
    /// <summary>
    /// 20스테이지 경제 (spec/09 §1, M14 G): 클리어 봇이 쓰는 스킬은 목록 순서대로 앞 스테이지들을 한 번씩 클리어만 해도(보너스 없이) 살 수 있어야 하고,
    /// 첫 플레이로 스킬 트리를 다 사지는 못해야 한다(반복·숙련의 보상이 남는다).
    /// </summary>
    public class EconomyTests
    {
        private static int CostOf(SkillLoadout loadout, SkillShop shop)
        {
            var save = new SaveData();
            int total = 0;
            foreach (var pair in loadout.Levels)
            {
                for (int level = 0; level < pair.Value; level++)
                {
                    total += shop.NextCost(save, pair.Key).Value;
                    save.SkillLevels[pair.Key] = level + 1;
                }
            }

            return total;
        }

        [Test]
        public void BotSkills_AreAffordableFromMinimumClearRewardsOfEarlierStages()
        {
            var source = FileSystemDataSource.ForRepoData();
            var tuning = TestSimulations.Tuning;
            var shop = new SkillShop(tuning);
            int clearReward = tuning.GetInt("meta.clearReward");
            var levels = StageCatalog.Load(source).LevelIds.ToList();
            for (int i = 0; i < levels.Count; i++)
            {
                string file = ScenarioDefinition.FilePath($"{levels[i]}_clear");
                var scenario = ScenarioDefinition.Parse(source.ReadText(file), file);
                int cost = CostOf(scenario.Skills, shop);
                Assert.That(cost, Is.LessThanOrEqualTo(i * clearReward), $"{levels[i]} (#{i + 1}) bot skills cost {cost}, earned at least {i * clearReward}");
            }
        }

        [Test]
        public void FirstPlaythroughWithAllBonuses_CannotBuyTheWholeTree()
        {
            var tuning = TestSimulations.Tuning;
            var shop = new SkillShop(tuning);
            var everything = new SkillLoadout(SkillCatalog.All.ToDictionary(skill => skill.Id, skill => skill.MaxLevel), null);
            int treeCost = CostOf(everything, shop);
            int perStageMax = tuning.GetInt("meta.clearReward") + tuning.GetInt("meta.noFrenzyBonus") + tuning.GetInt("meta.carefulBiteBonus") + tuning.GetInt("meta.parTimeBonus");
            int stages = StageCatalog.Load(FileSystemDataSource.ForRepoData()).LevelIds.Count();

            Assert.That(stages * perStageMax, Is.LessThan(treeCost), "replays and mastery still matter");
            Assert.That(stages * perStageMax, Is.GreaterThan(treeCost / 2), "a perfect first run buys more than half of the tree");
        }
    }
}
