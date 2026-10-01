using System;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;
using TuningData = Moqui.Core.Data.Tuning;

namespace Moqui.Core.Tests.Meta
{
    /// <summary>스킬 효과 (spec/09 §2). 곱 효과 = 기본 × 배율^레벨, 합 효과 = 기본 + 증감 × 레벨 (D-043).</summary>
    public class SkillTests
    {
        private const double Tolerance = 1e-4;

        private static TuningData At(string skillId, int level)
        {
            return SkillEffects.Apply(Tuning, SkillLoadout.Of((skillId, level)));
        }

        private static void AssertEachLevel(string skillId, Action<TuningData, int> check)
        {
            int max = SkillCatalog.Get(skillId).MaxLevel;
            for (int level = 0; level <= max; level++)
            {
                check(At(skillId, level), level);
            }
        }

        private static double Pow(string key, int level) => Math.Pow(Tuning.GetFloat(key), level);

        private static void AssertKey(TuningData applied, string key, double expected, int level)
        {
            Assert.That(applied.GetFloat(key), Is.EqualTo(expected).Within(Tolerance), $"{key} at level {level}");
        }

        [Test]
        public void ResistSpray_ToxinMultiplierPerLevel()
        {
            for (int level = 0; level <= 3; level++)
            {
                var skills = SkillLoadout.Of((SkillCatalog.ResistSpray, level));
                Assert.That(SkillEffects.ToxinMultiplier(Tuning, skills), Is.EqualTo(Pow("skill.resistSpray.toxinMul", level)).Within(Tolerance));
            }
        }

        [Test]
        public void ResistWet_WetDurationHumidity_AndEscapePressAtMaxLevel()
        {
            AssertEachLevel(SkillCatalog.ResistWet, (applied, level) =>
            {
                AssertKey(applied, "wetWings.duration", Tuning.GetFloat("wetWings.duration") * Pow("skill.resistWet.durationMul", level), level);
                AssertKey(applied, "humid.gainStrong", Tuning.GetFloat("humid.gainStrong") * Pow("skill.resistWet.humidMul", level), level);
                AssertKey(applied, "humid.gainWeak", Tuning.GetFloat("humid.gainWeak") * Pow("skill.resistWet.humidMul", level), level);
                Assert.That(applied.GetInt("water.escapePresses"), Is.EqualTo(Tuning.GetInt("water.escapePresses") - (level == 3 ? 1 : 0)), $"escape presses at {level}");
            });
        }

        [Test]
        public void ResistSatiety_PenaltyWidthScaled()
        {
            AssertEachLevel(SkillCatalog.ResistSatiety, (applied, level) =>
            {
                double mul = Pow("skill.resistSatiety.penaltyMul", level);
                AssertKey(applied, "satiety.minSpeedMul", 1 - ((1 - Tuning.GetFloat("satiety.minSpeedMul")) * mul), level);
                AssertKey(applied, "satiety.minDashMul", 1 - ((1 - Tuning.GetFloat("satiety.minDashMul")) * mul), level);
            });
        }

        [Test]
        public void SilentWings_AllNoiseRadii()
        {
            AssertEachLevel(SkillCatalog.SilentWings, (applied, level) =>
            {
                AssertKey(applied, "noise.flightRadius", Tuning.GetFloat("noise.flightRadius") * Pow("skill.silentWings.noiseMul", level), level);
                AssertKey(applied, "dash.noiseRadius", Tuning.GetFloat("dash.noiseRadius") * Pow("skill.silentWings.noiseMul", level), level);
            });
        }

        [Test]
        public void SwiftWings_FlightSpeed()
        {
            AssertEachLevel(SkillCatalog.SwiftWings, (applied, level) =>
            {
                AssertKey(applied, "flight.speed", Tuning.GetFloat("flight.speed") * Pow("skill.swiftWings.speedMul", level), level);
                AssertKey(applied, "flight.verticalSpeed", Tuning.GetFloat("flight.verticalSpeed") * Pow("skill.swiftWings.speedMul", level), level);
            });

            var fast = new GameSimulation(GameSettings.FromTuning(At(SkillCatalog.SwiftWings, 3)), new SimulationSetup(new CollisionWorld(), Vector3.Zero));
            Run(fast, Forward, SecondsToTicks(1f));
            Assert.That(fast.Player.Velocity.Length(), Is.EqualTo(Settings.Flight.Speed * Pow("skill.swiftWings.speedMul", 3)).Within(0.1), "applied to flight");
        }

        [Test]
        public void Stamina_MaxAddAndRegen()
        {
            AssertEachLevel(SkillCatalog.Stamina, (applied, level) =>
            {
                AssertKey(applied, "stamina.max", Tuning.GetFloat("stamina.max") + (Tuning.GetFloat("skill.stamina.maxAdd") * level), level);
                AssertKey(applied, "stamina.regenRate", Tuning.GetFloat("stamina.regenRate") * Pow("skill.stamina.regenMul", level), level);
            });
        }

        [Test]
        public void FeatherLanding_LandChance()
        {
            AssertEachLevel(SkillCatalog.FeatherLanding, (applied, level) =>
                AssertKey(applied, "reaction.landChance", Tuning.GetFloat("reaction.landChance") * Pow("skill.featherLanding.landChanceMul", level), level));
        }

        [Test]
        public void NumbingSaliva_ItchRate()
        {
            AssertEachLevel(SkillCatalog.NumbingSaliva, (applied, level) =>
                AssertKey(applied, "suck.itchRate", Tuning.GetFloat("suck.itchRate") * Pow("skill.numbingSaliva.itchMul", level), level));
        }

        [Test]
        public void ShadowBlend_AttachedVisionAndCalmTime()
        {
            AssertEachLevel(SkillCatalog.ShadowBlend, (applied, level) =>
            {
                AssertKey(applied, "vision.attachedMul", Math.Max(0, Tuning.GetFloat("vision.attachedMul") + (Tuning.GetFloat("skill.shadowBlend.attachedMulAdd") * level)), level);
                AssertKey(applied, "frenzy.calmTime", Tuning.GetFloat("frenzy.calmTime") + (Tuning.GetFloat("skill.shadowBlend.calmTimeAdd") * level), level);
            });
        }

        [Test]
        public void MagicWand_RateMaxAndRampTime()
        {
            AssertEachLevel(SkillCatalog.MagicWand, (applied, level) =>
            {
                AssertKey(applied, "suck.rateMax", Tuning.GetFloat("suck.rateMax") * Pow("skill.magicWand.rateMaxMul", level), level);
                AssertKey(applied, "suck.rampTime", Tuning.GetFloat("suck.rampTime") + (Tuning.GetFloat("skill.magicWand.rampTimeAdd") * level), level);
            });
        }

        [Test]
        public void CompoundEyes_ClearAndHeatRange()
        {
            AssertEachLevel(SkillCatalog.CompoundEyes, (applied, level) =>
            {
                AssertKey(applied, "senses.clearRange", Tuning.GetFloat("senses.clearRange") + (Tuning.GetFloat("skill.compoundEyes.clearRangeAdd") * level), level);
                AssertKey(applied, "senses.heatRange", Tuning.GetFloat("senses.heatRange") + (Tuning.GetFloat("skill.compoundEyes.heatRangeAdd") * level), level);
            });
        }

        [Test]
        public void VortexControl_AccelTimesScaledPerLevel_DiagonalDashOnlyAtLevel3()
        {
            AssertEachLevel(SkillCatalog.VortexControl, (applied, level) =>
            {
                AssertKey(applied, "flight.accelTime", Tuning.GetFloat("flight.accelTime") * Math.Pow(0.8, level), level);
                AssertKey(applied, "flight.decelTime", Tuning.GetFloat("flight.decelTime") * Math.Pow(0.8, level), level);
            });

            var diagonal = new PlayerCommand { DashPressed = true, Move = new Vector2(1f, 0f), Vertical = 1f };
            for (int level = 0; level <= 3; level++)
            {
                var skills = SkillLoadout.Of((SkillCatalog.VortexControl, level));
                var simulation = new GameSimulation(GameSettings.FromTuning(SkillEffects.Apply(Tuning, skills)), new SimulationSetup(new CollisionWorld(), new Vector3(0f, 100f, 0f), skills: skills));
                simulation.Step(diagonal);
                var direction = simulation.Player.DashDirection;
                bool isDiagonal = MathF.Abs(direction.X) > 0.1f && MathF.Abs(direction.Y) > 0.1f;
                Assert.That(isDiagonal, Is.EqualTo(level == 3), $"diagonal dash at level {level}");
            }
        }

        private static GameSimulation ChainSimulation(int level)
        {
            var skills = SkillLoadout.Of((SkillCatalog.ChainVortex, level));
            return new GameSimulation(Settings, new SimulationSetup(new CollisionWorld(), new Vector3(0f, 100f, 0f), skills: skills));
        }

        private static int DashTicks => Math.Max(1, SimulationTime.ToTicks(Settings.Dash.Duration));

        /// <summary>대시 후 대시가 끝난 다음 틱부터 extraTicks만큼 기다렸다가 다시 대시 입력.</summary>
        private static bool DashThenDashAgain(GameSimulation simulation, int waitTicksAfterEnd)
        {
            var dash = new PlayerCommand { DashPressed = true, Move = new Vector2(0f, 1f) };
            simulation.Step(dash);
            Run(simulation, PlayerCommand.None, DashTicks - 1 + waitTicksAfterEnd);
            int before = simulation.Player.LastDashStartTick;
            simulation.Step(dash);
            return simulation.Player.LastDashStartTick != before;
        }

        [Test]
        public void ChainVortex_ExtraDashInsideWindowIgnoresCooldown_OncePerChain()
        {
            Assert.That(DashThenDashAgain(ChainSimulation(0), 2), Is.False, "no skill: cooldown blocks");
            Assert.That(DashThenDashAgain(ChainSimulation(1), 2), Is.True, "inside window: chained");
            Assert.That(DashThenDashAgain(ChainSimulation(1), SimulationTime.ToTicks(Settings.Dash.ChainWindow) + 3), Is.False, "outside window");

            var simulation = ChainSimulation(1);
            Assert.That(DashThenDashAgain(simulation, 2), Is.True);
            Run(simulation, PlayerCommand.None, DashTicks + 1);
            int before = simulation.Player.LastDashStartTick;
            simulation.Step(new PlayerCommand { DashPressed = true });
            Assert.That(simulation.Player.LastDashStartTick, Is.EqualTo(before), "only one extra dash per chain");
        }

        [Test]
        public void ChainVortex_Level1CostsStamina_Level2Free()
        {
            float cost = Settings.Dash.StaminaCost;
            foreach (int level in new[] { 1, 2 })
            {
                var simulation = ChainSimulation(level);
                float start = simulation.Player.Stamina;
                Assert.That(DashThenDashAgain(simulation, 2), Is.True);
                float spent = start - simulation.Player.Stamina;
                Assert.That(spent, Is.EqualTo(level == 1 ? cost * 2 : cost).Within(1f), $"level {level}");
            }
        }

        [Test]
        public void Decoy_EmitsNoiseAtAimPointForDuration_LastStimulusMovesToDecoy()
        {
            var skills = SkillLoadout.Of((SkillCatalog.DecoyCharm, 1)).WithEquipped(SkillCatalog.DecoyCharm);
            var human = TestHumans.Seated();
            var spawn = TestHumans.Head + new Vector3(0f, 0f, -150f);
            var simulation = new GameSimulation(Settings, new SimulationSetup(new CollisionWorld(), spawn, human, skills: skills));
            var decoy = Settings.Decoy;

            // 머리 쪽(+z)을 조준: 미끼는 머리 근처 range 지점에 생긴다.
            simulation.Step(new PlayerCommand { SkillPressed = true, LookYaw = 0f });
            var noise = simulation.Events.OfType<NoiseEmitted>().Single(e => e.Source == NoiseSource.Decoy);
            Vector3 expected = spawn + (DecoySystem.AimDirection(0f, 0f) * decoy.Range);
            Assert.That(Vector3.Distance(noise.Position, expected), Is.LessThan(0.01f));
            Assert.That(noise.Radius, Is.EqualTo(decoy.NoiseRadius));
            Assert.That(simulation.Human.LastStimulusPosition, Is.EqualTo(noise.Position), "human hears the decoy as real noise");

            int noiseTicks = 1;
            for (int i = 0; i < SecondsToTicks(decoy.Duration + 1f); i++)
            {
                simulation.Step(PlayerCommand.None);
                noiseTicks += simulation.Events.OfType<NoiseEmitted>().Count(e => e.Source == NoiseSource.Decoy);
            }

            Assert.That(noiseTicks, Is.EqualTo(SimulationTime.ToTicks(decoy.Duration)), "noise only during duration");
            Assert.That(simulation.CaptureSnapshot().Decoy.CooldownRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void Decoy_NotEquipped_DoesNothing()
        {
            var skills = SkillLoadout.Of((SkillCatalog.DecoyCharm, 2));
            var simulation = new GameSimulation(Settings, new SimulationSetup(new CollisionWorld(), Vector3.Zero, skills: skills));
            simulation.Step(new PlayerCommand { SkillPressed = true });
            Assert.That(simulation.Events.OfType<NoiseEmitted>().Any(), Is.False);
            Assert.That(simulation.CaptureSnapshot().Decoy, Is.Null);
        }

        [Test]
        public void Retry_KeepsSkills()
        {
            var skills = SkillLoadout.Of((SkillCatalog.VortexControl, 3), (SkillCatalog.ChainVortex, 2));
            var simulation = new GameSimulation(Settings, new SimulationSetup(() => new CollisionWorld(), Vector3.Zero, skills: skills));
            var retry = simulation.Retry();
            Assert.That(retry.Setup.Skills, Is.SameAs(skills));
            Assert.That(retry.DiagonalDashUnlocked, Is.True);
        }
    }
}
