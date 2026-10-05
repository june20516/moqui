using System;
using System.Collections.Generic;

namespace Moqui.Core.Simulation
{
    /// <summary>진행 중인 흡혈 세션 (spec/04 §2). 부착 한 번 = 세션 한 번.</summary>
    public sealed class SuckSession
    {
        public SuckSession(SkinSiteState site, int attachedTick)
        {
            Site = site;
            AttachedTick = attachedTick;
        }

        public SkinSiteState Site { get; }

        /// <summary>주둥이를 꽂은 피부 지점. 자국은 여기에 생긴다 (M12).</summary>
        public Collision.SurfaceAnchor BiteSpot { get; set; }

        /// <summary>이 세션을 시작한 부착의 틱. 다시 부착하면 새 세션이다.</summary>
        public int AttachedTick { get; }

        /// <summary>Suck을 누르고 있던 누적 시간 (가속 기준).</summary>
        public float SuckSeconds { get; set; }

        /// <summary>이 세션에서 빤 양 (%).</summary>
        public float Amount { get; set; }
    }

    /// <summary>
    /// 흡혈 (spec/04 §2~6).
    /// SkinSite에 부착한 채 Suck을 누르면 세션이 시작되고, 부착이 풀리면(이탈·튕겨남·사망) 끝난다. Suck을 놓아도 세션은 유지된다.
    /// 속도 = lerp(rateStart, rateMax, min(1, 세션 흡혈 시간 / rampTime)) × 혈액량 × 배율.
    /// 흡혈 중 그 부위 가려움이 itchRate × 민감도 × 배율로 오르고, 흡혈하지 않는 부위는 itchDecay로 내린다.
    /// 세션이 끝날 때 빤 양이 biteMark.minAmount 이상이면 자국이 생기고 자국 수가 늘며 경계가 awarenessBump만큼 오른다.
    /// 흡혈 게이지 100%에 도달하면 Stage Clear.
    /// </summary>
    public sealed class SuckSystem
    {
        public const float GaugeMax = 100f;

        private readonly SuckSettings _suck;
        private readonly SiteSettings _sites;
        private readonly BiteMarkSettings _biteMarks;
        private readonly SuckEventSettings _suckEvent;

        public SuckSystem(SuckSettings suck, SiteSettings sites, BiteMarkSettings biteMarks, SuckEventSettings suckEvent)
        {
            _suckEvent = suckEvent;
            _suck = suck;
            _sites = sites;
            _biteMarks = biteMarks;
        }

        /// <summary>흡혈 속도 배율 (스킬 마법봉, 취한 타겟 — spec/06·09). 기본 1.</summary>
        public float RateMultiplier { get; set; } = 1f;

        /// <summary>가속에 걸리는 시간에 더하는 값 (스킬 마법봉, spec/09). 기본 0.</summary>
        public float RampTimeAdd { get; set; }

        /// <summary>최대 속도 배율 (스킬 마법봉, spec/09). 기본 1.</summary>
        public float RateMaxMultiplier { get; set; } = 1f;

        /// <summary>가려움 증가 배율 (스킬 마취 타액, 취한 타겟 — spec/06·09). 기본 1.</summary>
        public float ItchMultiplier { get; set; } = 1f;

        /// <summary>포만 감속 폭에 곱하는 값 (스킬 소화 촉진, spec/09). 기본 1.</summary>
        public float SatietyPenaltyMultiplier { get; set; } = 1f;

        public float SessionRate(float sessionSuckSeconds, SkinSiteType type)
        {
            float rampTime = Math.Max(0f, _suck.RampTime + RampTimeAdd);
            float t = rampTime > 0f ? Math.Min(1f, sessionSuckSeconds / rampTime) : 1f;
            float rateMax = _suck.RateMax * RateMaxMultiplier;
            float baseRate = _suck.RateStart + ((rateMax - _suck.RateStart) * t);
            return baseRate * _sites.BloodAmount(type) * RateMultiplier;
        }

        public float SpeedMultiplier(float bloodGauge)
        {
            return 1f - ((1f - _suck.SatietyMinSpeedMul) * SatietyPenaltyMultiplier * (bloodGauge / GaugeMax));
        }

        public float DashMultiplier(float bloodGauge)
        {
            return 1f - ((1f - _suck.SatietyMinDashMul) * SatietyPenaltyMultiplier * (bloodGauge / GaugeMax));
        }

        public void Step(Player player, Human human, in PlayerCommand command, int tick, float deltaTime, List<SimulationEvent> events)
        {
            SkinSiteState suckedSite = null;
            SkinSiteState attachedSite = null;
            bool attachedToSkin = human != null
                && player.State == PlayerState.Attached
                && human.TryGetSite(player.Anchor.Shape, out attachedSite);

            var session = player.SuckSession;
            if (session != null && (!attachedToSkin || session.Site != attachedSite || session.AttachedTick != player.AttachedTick))
            {
                EndSession(player, human, tick, events);
                session = null;
            }

            if (attachedToSkin && command.SuckHeld)
            {
                if (session == null)
                {
                    session = new SuckSession(attachedSite, player.AttachedTick);
                    player.Anchor.Resolve(out var spot, out var spotNormal);
                    session.BiteSpot = Collision.SurfaceAnchor.Create(attachedSite.Shape, spot, spotNormal);
                    player.SuckSession = session;
                }

                // 부위가 움직이는 동안 버티면 피가 더 잘 나오고, 움직이느라 가려움을 못 느낀다 (spec/04 §8, D-056).
                bool riding = human.SuckEvent.Is(SuckEventKind.Shift, SuckEventPhase.Active);
                float eventRate = riding ? _suckEvent.ShiftRateMul : 1f;
                float gain = Math.Min(SessionRate(session.SuckSeconds, attachedSite.Type) * eventRate * deltaTime, GaugeMax - player.BloodGauge);
                session.SuckSeconds += deltaTime;
                session.Amount += gain;
                player.BloodGauge += gain;
                float itchGain = riding ? 0f : _suck.ItchRate * _sites.Sensitivity(attachedSite.Type) * ItchMultiplier * deltaTime;
                attachedSite.Itch = Math.Min(GaugeMax, attachedSite.Itch + itchGain);
                suckedSite = attachedSite;
            }

            if (human != null)
            {
                foreach (var site in human.SkinSites)
                {
                    if (site != suckedSite)
                    {
                        site.Itch = Math.Max(0f, site.Itch - (_suck.ItchDecay * deltaTime));
                    }
                }
            }
        }

        private void EndSession(Player player, Human human, int tick, List<SimulationEvent> events)
        {
            var session = player.SuckSession;
            player.SuckSession = null;
            bool biteMark = session.Amount >= _biteMarks.MinAmount;
            if (biteMark && human != null)
            {
                session.Site.HasBiteMark = true;
                human.BiteMarkCount++;
                if (session.BiteSpot != null)
                {
                    human.AddBiteMark(new BiteMark(session.Site.PartId, session.BiteSpot));
                }

                // 세션이 끝나는 순간 가려움을 알아챈다 (spec/04 §4).
                human.Awareness = Math.Min(GaugeMax, human.Awareness + _biteMarks.AwarenessBump);
                human.HasStimulus = true;
                human.LastStimulusPosition = session.Site.Shape.Center;
                human.LastStimulusTick = tick;
            }

            events.Add(new SuckSessionEnded(tick, session.Site.PartId, session.Amount, biteMark));
        }
    }
}
