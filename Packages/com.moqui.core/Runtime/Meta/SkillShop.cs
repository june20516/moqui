using Moqui.Core.Data;

namespace Moqui.Core.Meta
{
    public enum PurchaseResult
    {
        Purchased,
        NotEnoughPoints,
        MaxLevel,
    }

    /// <summary>스킬 구매와 액티브 장착 (spec/09 §2). 비용: 다음 레벨 = skill.cost.{A|B}[현재 레벨].</summary>
    public sealed class SkillShop
    {
        private readonly Tuning _tuning;

        public SkillShop(Tuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>다음 레벨 비용. 최대 레벨이면 null.</summary>
        public int? NextCost(SaveData save, string skillId)
        {
            var skill = SkillCatalog.Get(skillId);
            int level = save.SkillLevel(skillId);
            if (level >= skill.MaxLevel)
            {
                return null;
            }

            return (int)_tuning.GetFloats($"skill.cost.{skill.Tier}")[level];
        }

        public PurchaseResult TryPurchase(SaveData save, string skillId)
        {
            int? cost = NextCost(save, skillId);
            if (!cost.HasValue)
            {
                return PurchaseResult.MaxLevel;
            }

            if (save.BloodPoints < cost.Value)
            {
                return PurchaseResult.NotEnoughPoints;
            }

            save.BloodPoints -= cost.Value;
            save.SkillLevels[skillId] = save.SkillLevel(skillId) + 1;
            return PurchaseResult.Purchased;
        }

        /// <summary>구매한(레벨 1 이상) 액티브 스킬만 장착할 수 있다. null이면 해제.</summary>
        public bool Equip(SaveData save, string activeSkillId)
        {
            if (activeSkillId == null)
            {
                save.EquippedActive = null;
                return true;
            }

            if (!SkillCatalog.Get(activeSkillId).IsActive || save.SkillLevel(activeSkillId) == 0)
            {
                return false;
            }

            save.EquippedActive = activeSkillId;
            return true;
        }
    }
}
