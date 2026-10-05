using System;
using System.Collections.Generic;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 확률 기반 반사적 반응 (spec/02 §5).
    /// 착지 반응: SkinSite에 부착하는 순간 1회, 확률 landChance × 민감도 × 보정.
    /// 부착 중 반응: 위험률 λ = 민감도 × (baseRate + itchRate × (가려움/100)²) × 보정, 틱 확률 1 − e^(−λ·dt). 가려움 100이면 즉시.
    /// 귀 반응: 귀 근접 구역(공중)에 있는 동안 위험률 earRate × 보정.
    /// 보정 = 광분 배율 × 졸기 배율 × 자국 배율 × 스킬 배율 × 취함 배율. 반응은 예고 시작 시점의 모기 위치를 때리며, 공격 중이면 버린다.
    /// </summary>
    public sealed class ReactionSystem
    {
        /// <summary>게이지 최댓값 (spec/tuning.md: 게이지는 0~100).</summary>
        public const float GaugeMax = 100f;

        private readonly ReactionSettings _reaction;
        private readonly SiteSettings _sites;
        private readonly FrenzySettings _frenzy;
        private readonly BiteMarkSettings _biteMarks;
        private readonly AttachSettings _attach;
        private readonly AttackSettings _attack;
        private readonly HumanAttackSystem _attacks;
        private readonly DozeSettings _doze;
        private readonly IRandom _random;

        public ReactionSystem(GameSettings settings, HumanAttackSystem attacks, IRandom random)
        {
            _reaction = settings.Reaction;
            _sites = settings.Sites;
            _frenzy = settings.Frenzy;
            _biteMarks = settings.BiteMark;
            _attach = settings.Attach;
            _attack = settings.Attack;
            _attacks = attacks;
            _doze = settings.Doze;
            _random = random;
        }

        /// <summary>스킬 깃털 착지 등 착지 반응 확률 배율 (spec/09). 기본 1.</summary>
        public float LandingSkillMultiplier { get; set; } = 1f;

        /// <summary>그 밖의 반응 확률 배율(스킬·취함, spec/06·09). 기본 1.</summary>
        public float ExtraMultiplier { get; set; } = 1f;

        public static double TickProbability(double hazardPerSecond, float deltaTime)
        {
            return 1.0 - Math.Exp(-hazardPerSecond * deltaTime);
        }

        public float Modifier(Human human)
        {
            float frenzy = human.State == AwarenessState.Frenzy ? _frenzy.ReactionMul : 1f;
            float doze = human.IsAsleep ? _doze.ReactionMul : 1f;
            return frenzy * doze * _biteMarks.ReactionMultiplier(human.BiteMarkCount) * ExtraMultiplier;
        }

        public float LandingChance(float sensitivity, float modifier)
        {
            return _reaction.LandChance * sensitivity * modifier * LandingSkillMultiplier;
        }

        public float AttachedHazard(float sensitivity, float itch, float modifier)
        {
            float itchRatio = itch / GaugeMax;
            return sensitivity * (_reaction.BaseRate + (_reaction.ItchRate * itchRatio * itchRatio)) * modifier;
        }

        public float EarHazard(float modifier)
        {
            return _reaction.EarRate * modifier;
        }

        public bool RollLanding(float sensitivity, float modifier)
        {
            return _random.Chance(LandingChance(sensitivity, modifier));
        }

        public void Step(Human human, Player player, in HumanPerception perception, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (player.State == PlayerState.Dead)
            {
                return;
            }

            float modifier = Modifier(human);
            if (player.State == PlayerState.Attached && human.TryGetSite(player.Anchor.Shape, out var site))
            {
                float sensitivity = _sites.Sensitivity(site.Type);
                bool react = player.AttachedTick == tick
                    ? RollLanding(sensitivity, modifier)
                    : site.Itch >= _attach.ItchThreshold || _random.Chance(TickProbability(AttachedHazard(sensitivity, site.Itch, modifier), deltaTime));
                if (react)
                {
                    // 모기가 앉은 팔로는 그 팔을 칠 수 없으니 반대쪽 손으로 친다 (D-052).
                    TryReact(human, player, human.Rig.ArmOwning(site.PartId)?.Index ?? -1, tick, events);
                }
            }

            if (perception.InEarZone && _random.Chance(TickProbability(EarHazard(modifier), deltaTime)))
            {
                TryReact(human, player, -1, tick, events);
            }
        }

        private void TryReact(Human human, Player player, int excludedArm, int tick, List<SimulationEvent> events)
        {
            // 인간은 한 번에 하나만 공격한다. 공격 중에 발생한 반응은 버린다.
            if (human.Attack.IsBusy)
            {
                return;
            }

            _attacks.Start(human, AttackKind.ReactSlap, player.Position, _attack.SelfSlapRadius, _attack.SelfSlapTelegraph, _attack.SelfSlapRecovery, _attack.HandPeakSpeedReaction, excludedArm, tick, events);
        }
    }
}
