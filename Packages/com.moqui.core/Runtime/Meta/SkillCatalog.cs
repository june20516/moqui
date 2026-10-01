using System;
using System.Collections.Generic;
using System.Linq;

namespace Moqui.Core.Meta
{
    /// <summary>스킬 비용 단계 (spec/09): A = skill.cost.A, B = skill.cost.B.</summary>
    public enum SkillCostTier
    {
        A,
        B,
    }

    /// <summary>Skills 화면 탭 (spec/08).</summary>
    public enum SkillCategory
    {
        Resist,
        Stats,
        Chain,
        Wand,
        Misc,
    }

    public sealed class SkillDefinition
    {
        public SkillDefinition(string id, string name, SkillCategory category, int maxLevel, SkillCostTier tier, bool isActive = false)
        {
            Id = id;
            Name = name;
            Category = category;
            MaxLevel = maxLevel;
            Tier = tier;
            IsActive = isActive;
        }

        public string Id { get; }

        /// <summary>표시 이름 (spec/09 표).</summary>
        public string Name { get; }

        public SkillCategory Category { get; }

        public int MaxLevel { get; }

        public SkillCostTier Tier { get; }

        /// <summary>액티브 스킬은 하나만 장착해 Skill 입력으로 쓴다.</summary>
        public bool IsActive { get; }
    }

    /// <summary>스킬 목록 (spec/09 §2).</summary>
    public static class SkillCatalog
    {
        public const string ResistSpray = "resistSpray";
        public const string ResistWet = "resistWet";
        public const string ResistSatiety = "resistSatiety";
        public const string SilentWings = "silentWings";
        public const string SwiftWings = "swiftWings";
        public const string VortexControl = "vortexControl";
        public const string Stamina = "stamina";
        public const string FeatherLanding = "featherLanding";
        public const string NumbingSaliva = "numbingSaliva";
        public const string ShadowBlend = "shadowBlend";
        public const string ChainVortex = "chainVortex";
        public const string MagicWand = "magicWand";
        public const string CompoundEyes = "compoundEyes";
        public const string DecoyCharm = "decoyCharm";

        public static readonly IReadOnlyList<SkillDefinition> All = new[]
        {
            new SkillDefinition(ResistSpray, "해독 체질", SkillCategory.Resist, 3, SkillCostTier.A),
            new SkillDefinition(ResistWet, "발수 코팅", SkillCategory.Resist, 3, SkillCostTier.A),
            new SkillDefinition(ResistSatiety, "소화 촉진", SkillCategory.Resist, 3, SkillCostTier.A),
            new SkillDefinition(SilentWings, "고요한 날개", SkillCategory.Stats, 3, SkillCostTier.B),
            new SkillDefinition(SwiftWings, "순풍", SkillCategory.Stats, 3, SkillCostTier.A),
            new SkillDefinition(VortexControl, "와류 제어", SkillCategory.Stats, 3, SkillCostTier.B),
            new SkillDefinition(Stamina, "지구력", SkillCategory.Stats, 3, SkillCostTier.A),
            new SkillDefinition(FeatherLanding, "깃털 착지", SkillCategory.Stats, 3, SkillCostTier.A),
            new SkillDefinition(NumbingSaliva, "마취 타액", SkillCategory.Stats, 3, SkillCostTier.B),
            new SkillDefinition(ShadowBlend, "그림자 동화", SkillCategory.Stats, 3, SkillCostTier.B),
            new SkillDefinition(ChainVortex, "연속 와류", SkillCategory.Chain, 2, SkillCostTier.B),
            new SkillDefinition(MagicWand, "마법봉 강화", SkillCategory.Wand, 3, SkillCostTier.B),
            new SkillDefinition(CompoundEyes, "겹눈 각성", SkillCategory.Misc, 2, SkillCostTier.A),
            new SkillDefinition(DecoyCharm, "미끼 마법", SkillCategory.Misc, 2, SkillCostTier.B, isActive: true),
        };

        public static SkillDefinition Get(string id)
        {
            return All.FirstOrDefault(skill => skill.Id == id) ?? throw new ArgumentException($"Unknown skill '{id}'", nameof(id));
        }

        public static bool Exists(string id)
        {
            return All.Any(skill => skill.Id == id);
        }
    }
}
