using System;
using System.Collections.Generic;
using Moqui.Core.Data;

namespace Moqui.Core.Meta
{
    /// <summary>
    /// 패시브 스킬 효과 (spec/09 §2, spec/tuning.md skill). 스킬 레벨을 반영한 Tuning을 새로 만든다.
    /// 효과는 레벨마다 누적되며 "(합)"이 아니면 곱이다: 곱 효과 = 기본값 × 배율^레벨, 합 효과 = 기본값 + 증감 × 레벨.
    /// 이 Tuning으로 GameSettings와 감각 표현 설정을 만들면 모든 시스템에 같은 값이 적용된다 (D-043).
    /// 대각선 대시·연속 와류·미끼처럼 수치가 아닌 효과는 SimulationSetup.Skills로 시뮬레이션이 직접 읽는다.
    /// </summary>
    public static class SkillEffects
    {
        public static Tuning Apply(Tuning tuning, SkillLoadout skills)
        {
            var values = new Dictionary<string, JsonValue>();
            foreach (string key in tuning.Keys)
            {
                values[key] = tuning.GetRaw(key);
            }

            void Multiply(string key, string mulKey, int level)
            {
                if (level > 0)
                {
                    values[key] = JsonValue.FromNumber(tuning.GetFloat(key) * Math.Pow(tuning.GetFloat(mulKey), level));
                }
            }

            void Add(string key, string addKey, int level, double min = double.MinValue)
            {
                if (level > 0)
                {
                    values[key] = JsonValue.FromNumber(Math.Max(min, tuning.GetFloat(key) + (tuning.GetFloat(addKey) * level)));
                }
            }

            // 해독 체질: 중독 증가(연무·모기향 핵심)와 모기향 하한에 곱한다 (spec/09).
            int resistSpray = skills.Level(SkillCatalog.ResistSpray);
            Multiply("spray.toxinRate", "skill.resistSpray.toxinMul", resistSpray);
            Multiply("coil.coreRate", "skill.resistSpray.toxinMul", resistSpray);
            Multiply("coil.nearFloor", "skill.resistSpray.toxinMul", resistSpray);
            Multiply("coil.farFloor", "skill.resistSpray.toxinMul", resistSpray);

            int resistWet = skills.Level(SkillCatalog.ResistWet);
            Multiply("wetWings.duration", "skill.resistWet.durationMul", resistWet);
            Multiply("humid.gainStrong", "skill.resistWet.humidMul", resistWet);
            Multiply("humid.gainWeak", "skill.resistWet.humidMul", resistWet);
            if (resistWet >= SkillCatalog.Get(SkillCatalog.ResistWet).MaxLevel)
            {
                values["water.escapePresses"] = JsonValue.FromNumber(Math.Max(1, tuning.GetInt("water.escapePresses") - 1));
            }

            int resistSatiety = skills.Level(SkillCatalog.ResistSatiety);
            if (resistSatiety > 0)
            {
                double penaltyMul = Math.Pow(tuning.GetFloat("skill.resistSatiety.penaltyMul"), resistSatiety);
                values["satiety.minSpeedMul"] = JsonValue.FromNumber(1.0 - ((1.0 - tuning.GetFloat("satiety.minSpeedMul")) * penaltyMul));
                values["satiety.minDashMul"] = JsonValue.FromNumber(1.0 - ((1.0 - tuning.GetFloat("satiety.minDashMul")) * penaltyMul));
            }

            int silentWings = skills.Level(SkillCatalog.SilentWings);
            Multiply("noise.flightRadius", "skill.silentWings.noiseMul", silentWings);
            Multiply("dash.noiseRadius", "skill.silentWings.noiseMul", silentWings);

            int swiftWings = skills.Level(SkillCatalog.SwiftWings);
            Multiply("flight.speed", "skill.swiftWings.speedMul", swiftWings);
            Multiply("flight.verticalSpeed", "skill.swiftWings.speedMul", swiftWings);

            int vortex = skills.Level(SkillCatalog.VortexControl);
            Multiply("flight.accelTime", "skill.vortexControl.accelTimeMul", vortex);
            Multiply("flight.decelTime", "skill.vortexControl.accelTimeMul", vortex);
            if (vortex >= SkillCatalog.Get(SkillCatalog.VortexControl).MaxLevel)
            {
                // 최대 레벨: 대시 쿨타임 감소 (대시가 진행 방향 전체를 쓰게 되어 "대각선 대시"를 대체, D-051).
                values["dash.cooldown"] = JsonValue.FromNumber(tuning.GetFloat("dash.cooldown") * tuning.GetFloat("skill.vortexControl.maxLevelDashCooldownMul"));
            }

            int stamina = skills.Level(SkillCatalog.Stamina);
            Add("stamina.max", "skill.stamina.maxAdd", stamina);
            Multiply("stamina.regenRate", "skill.stamina.regenMul", stamina);

            Multiply("reaction.landChance", "skill.featherLanding.landChanceMul", skills.Level(SkillCatalog.FeatherLanding));
            Multiply("suck.itchRate", "skill.numbingSaliva.itchMul", skills.Level(SkillCatalog.NumbingSaliva));

            int shadowBlend = skills.Level(SkillCatalog.ShadowBlend);
            Add("vision.attachedMul", "skill.shadowBlend.attachedMulAdd", shadowBlend, min: 0);
            Add("frenzy.calmTime", "skill.shadowBlend.calmTimeAdd", shadowBlend, min: 0);

            int magicWand = skills.Level(SkillCatalog.MagicWand);
            Multiply("suck.rateMax", "skill.magicWand.rateMaxMul", magicWand);
            Add("suck.rampTime", "skill.magicWand.rampTimeAdd", magicWand, min: 0);

            int compoundEyes = skills.Level(SkillCatalog.CompoundEyes);
            Add("senses.clearRange", "skill.compoundEyes.clearRangeAdd", compoundEyes);
            Add("senses.heatRange", "skill.compoundEyes.heatRangeAdd", compoundEyes);

            return new Tuning(values);
        }

        /// <summary>해독 체질: 중독 증가와 모기향 하한에 곱하는 배율 (spec/09). Apply가 spray·coil 키에 이 값을 곱한다.</summary>
        public static float ToxinMultiplier(Tuning tuning, SkillLoadout skills)
        {
            return (float)Math.Pow(tuning.GetFloat("skill.resistSpray.toxinMul"), skills.Level(SkillCatalog.ResistSpray));
        }
    }
}
