using System;
using System.Collections.Generic;

namespace Moqui.Core.Meta
{
    /// <summary>스테이지에 가지고 들어가는 스킬 레벨과 장착 액티브. 재시도에도 그대로 쓴다.</summary>
    public sealed class SkillLoadout
    {
        public static readonly SkillLoadout None = new SkillLoadout(new Dictionary<string, int>(), null);

        private readonly Dictionary<string, int> _levels;

        public SkillLoadout(IReadOnlyDictionary<string, int> levels, string equippedActive)
        {
            _levels = new Dictionary<string, int>();
            foreach (var pair in levels)
            {
                var skill = SkillCatalog.Get(pair.Key);
                _levels[pair.Key] = Math.Clamp(pair.Value, 0, skill.MaxLevel);
            }

            EquippedActive = equippedActive != null && Level(equippedActive) > 0 && SkillCatalog.Get(equippedActive).IsActive ? equippedActive : null;
        }

        public string EquippedActive { get; }

        public IReadOnlyDictionary<string, int> Levels => _levels;

        public int Level(string skillId)
        {
            return _levels.TryGetValue(skillId, out int level) ? level : 0;
        }

        /// <summary>장착한 액티브의 레벨. 장착하지 않았으면 0.</summary>
        public int ActiveLevel(string skillId)
        {
            return EquippedActive == skillId ? Level(skillId) : 0;
        }

        public static SkillLoadout Of(params (string Id, int Level)[] levels)
        {
            var dictionary = new Dictionary<string, int>();
            foreach (var (id, level) in levels)
            {
                dictionary[id] = level;
            }

            return new SkillLoadout(dictionary, null);
        }

        public SkillLoadout WithEquipped(string activeId)
        {
            return new SkillLoadout(_levels, activeId);
        }
    }
}
