using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>흡혈 중 이벤트 (spec/04 §8, D-056): 긁으러 오는 손, 부위가 움직임, 시선.</summary>
    public class SuckEventTests
    {
        private static PlayerCommand Attach => new PlayerCommand { AttachPressed = true };

        private static PlayerCommand Suck => new PlayerCommand { SuckHeld = true };

        private static SuckEventSettings Events => Settings.SuckEvent;

        /// <summary>반응·무작위 이벤트를 끄고 partId에 붙어 잠시 빤 시뮬레이션. 이벤트는 테스트가 직접 시작한다.</summary>
        private static GameSimulation SuckingOn(string partId, float seconds = 0.5f)
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            simulation.HumanSystem.Reactions.ExtraMultiplier = 0f;
            simulation.HumanSystem.Reactions.LandingSkillMultiplier = 0f;
            TestHumans.PlaceNearPart(simulation, partId);
            simulation.Step(Attach);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Run(simulation, Suck, SecondsToTicks(seconds));
            Assert.That(simulation.Player.SuckSession, Is.Not.Null);
            return simulation;
        }

        private static void Begin(GameSimulation simulation, SuckEventKind kind)
        {
            simulation.HumanSystem.SuckEvents.Begin(simulation.Human, simulation.Player, kind, simulation.Tick, new List<SimulationEvent>());
            Assert.That(simulation.Human.SuckEvent.Kind, Is.EqualTo(kind));
        }

        private static float HeadAngleTo(Human human, Vector3 point)
        {
            Vector3 toPoint = Vector3.Normalize(point - human.HeadCenter);
            return MathF.Acos(Math.Clamp(Vector3.Dot(toPoint, Vector3.Normalize(human.HeadForward)), -1f, 1f)) * 180f / MathF.PI;
        }

        [Test]
        public void TwitchLevels_Are40_60_80_WithGrowingReach()
        {
            Assert.That(Events.TwitchLevel(39f), Is.EqualTo(0));
            Assert.That(Events.TwitchLevel(40f), Is.EqualTo(1));
            Assert.That(Events.TwitchLevel(60f), Is.EqualTo(2));
            Assert.That(Events.TwitchLevel(99f), Is.EqualTo(3));
            Assert.That(Events.TwitchLevels, Is.EqualTo(3));
            Assert.That(Events.TwitchReach(1), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(Events.TwitchReach(3), Is.EqualTo(0.65f).Within(1e-5f));
        }

        /// <summary>가려움이 단계를 넘으면 반대쪽 손이 문 자리 쪽으로 움찔했다 돌아온다. 판정은 없고, 같은 단계에서는 한 번뿐이다.</summary>
        [Test]
        public void Twitch_ItchCrossesThreshold_OppositeHandTwitchesTowardBiteAndBack()
        {
            var simulation = SuckingOn("forearmR");
            var human = simulation.Human;
            var site = TestHumans.Site(simulation, "forearmR");
            int leftArm = human.Rig.ArmOwning("forearmL").Index;
            Vector3 rest = human.Palm(leftArm);
            site.Itch = Events.TwitchItchStart + 1f;

            simulation.Step(Suck);
            Assert.That(simulation.Events.OfType<SuckEventStarted>().Single().Kind, Is.EqualTo(SuckEventKind.Twitch));
            Assert.That(human.SuckEvent.Arm, Is.EqualTo(leftArm), "the hand of the other arm scratches");

            Vector3 spot = human.SuckEvent.Target;
            Run(simulation, Suck, SecondsToTicks(Events.TwitchDuration * 0.5f));
            float halfway = Vector3.Distance(human.Palm(leftArm), spot);
            Assert.That(halfway, Is.LessThan(Vector3.Distance(rest, spot) - 1f), "the hand moved toward the bite");

            Run(simulation, Suck, SecondsToTicks(Events.TwitchDuration));
            Assert.That(human.SuckEvent.Kind, Is.EqualTo(SuckEventKind.None));
            Assert.That(human.Pose.HandTargets[leftArm], Is.Null, "the hand went back to rest");
            Assert.That(Vector3.Distance(human.Palm(leftArm), rest), Is.LessThan(0.5f));
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached), "a twitch never hits");

            site.Itch = Events.TwitchItchStart + 2f;
            simulation.Step(Suck);
            Assert.That(simulation.Events.OfType<SuckEventStarted>(), Is.Empty, "one twitch per level per session");
        }

        /// <summary>부위가 움직일 때 Suck을 누르고 버티면 붙어 있고, 피가 더 잘 나오며 가려움은 오르지 않는다.</summary>
        [Test]
        public void Shift_HoldingSuck_RidesItWithFasterBloodAndNoItch()
        {
            var simulation = SuckingOn("forearmR", seconds: 3f);
            var site = TestHumans.Site(simulation, "forearmR");
            Begin(simulation, SuckEventKind.Shift);
            Run(simulation, Suck, SecondsToTicks(Events.ShiftTelegraph) + 1);
            Assert.That(simulation.Human.SuckEvent.Phase, Is.EqualTo(SuckEventPhase.Active));
            Assert.That(simulation.Human.CurrentAction?.Name, Is.EqualTo(SuckEventSystem.ShiftActionName));

            float rate = simulation.Suck.SessionRate(simulation.Player.SuckSession.SuckSeconds, site.Type);
            float itch = site.Itch;
            float before = simulation.Player.BloodGauge;
            simulation.Step(Suck);
            float measured = (simulation.Player.BloodGauge - before) / GameSimulation.DeltaTime;

            Assert.That(measured, Is.EqualTo(rate * Events.ShiftRateMul).Within(0.05f));
            Assert.That(site.Itch, Is.EqualTo(itch).Within(1e-4f), "too busy moving to feel the itch");

            Run(simulation, Suck, SecondsToTicks(Events.ShiftDuration));
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached), "gripping with the proboscis");
            Assert.That(simulation.Events.OfType<PlayerDislodged>(), Is.Empty);
            Assert.That(simulation.Human.SuckEvent.Kind, Is.EqualTo(SuckEventKind.None));
        }

        /// <summary>부위가 움직일 때 Suck을 놓고 있으면 버티지 못하고 튕겨 난다 (최고 속도 > human.dislodgeSpeed).</summary>
        [Test]
        public void Shift_NotHoldingSuck_IsDislodged()
        {
            Assert.That(Events.ShiftDistance * MathF.PI / Events.ShiftDuration, Is.GreaterThan(Settings.HumanMotion.DislodgeSpeed)
                .And.LessThan(Settings.HumanMotion.DislodgeSpeed * Events.GripMul), "only the grip holds");
            var simulation = SuckingOn("forearmR");
            Begin(simulation, SuckEventKind.Shift);

            bool dislodged = false;
            for (int i = 0; i < SecondsToTicks(Events.ShiftTelegraph + Events.ShiftDuration) && !dislodged; i++)
            {
                simulation.Step(PlayerCommand.None);
                dislodged = simulation.Events.OfType<PlayerDislodged>().Any();
            }

            Assert.That(dislodged, Is.True);
        }

        /// <summary>시선: 머리가 문 자리로 돌고, 응시 중 계속 빨면 들켜 경계가 크게 오른다.</summary>
        [Test]
        public void Glance_KeepSucking_IsNoticed()
        {
            var simulation = SuckingOn("forearmR");
            var human = simulation.Human;
            float awareness = human.Awareness;
            Vector3 spot = human.SuckEvent.Target;
            float angleBefore = HeadAngleTo(human, spot);
            Begin(simulation, SuckEventKind.Glance);

            bool noticed = false;
            for (int i = 0; i < SecondsToTicks(Events.GlanceTurnTime + Events.GlanceHold) && !noticed; i++)
            {
                simulation.Step(Suck);
                noticed = simulation.Events.OfType<SuckGlanceNoticed>().Any();
            }

            Assert.That(HeadAngleTo(human, spot), Is.LessThan(angleBefore), "the head turned to the bite");
            Assert.That(noticed, Is.True);
            Assert.That(human.Awareness - awareness, Is.GreaterThanOrEqualTo(Events.GlanceNoticeAwareness - 1e-3f));
        }

        /// <summary>시선: 흡혈을 멈추고 얼어 있으면 들키지 않고 시야로도 경계가 오르지 않는다. 끝나면 머리가 원래대로 돌아간다.</summary>
        [Test]
        public void Glance_Frozen_PassesUnnoticed_HeadReturns()
        {
            var simulation = SuckingOn("forearmR");
            var human = simulation.Human;
            float awareness = human.Awareness;
            float yawBefore = human.HeadYaw;
            Begin(simulation, SuckEventKind.Glance);

            int ticks = SecondsToTicks((Events.GlanceTurnTime * 2f) + Events.GlanceHold) + 2;
            for (int i = 0; i < ticks; i++)
            {
                simulation.Step(PlayerCommand.None);
                Assert.That(simulation.Events.OfType<SuckGlanceNoticed>(), Is.Empty);
            }

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(human.SuckEvent.Kind, Is.EqualTo(SuckEventKind.None));
            Assert.That(human.Awareness, Is.LessThanOrEqualTo(awareness + 1e-3f), "a still mosquito is not noticed");
            Assert.That(human.HeadYaw, Is.EqualTo(yawBefore).Within(Settings.Head.IdleTurnSpeed * GameSimulation.DeltaTime * 3f), "the head turned back");
        }

        /// <summary>흡혈 중이 아니면 이벤트는 시작되지 않는다.</summary>
        [Test]
        public void NotSucking_NoEventStarts()
        {
            var simulation = SuckingOn("forearmR");
            TestHumans.Site(simulation, "forearmR").Itch = 90f;
            for (int i = 0; i < SecondsToTicks(30f); i++)
            {
                simulation.Step(PlayerCommand.None);
                Assert.That(simulation.Events.OfType<SuckEventStarted>(), Is.Empty);
            }
        }
    }
}
