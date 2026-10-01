using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>환경 기믹 (spec/06): 선풍기, 거미줄, 모기약 연무·중독, 모기향, 인간 분사, 자동 분사기, 취한 타겟.</summary>
    public class GimmickTests
    {
        private const float Tolerance = 0.05f;

        private static readonly Vector3 FanPosition = new Vector3(-100f, 100f, 0f);

        /// <summary>+X를 보는 선풍기 (시작 틱에 머리가 정확히 +X).</summary>
        private static FanDefinition FanPlusX => new FanDefinition("fan", FanPosition, 90f);

        private static GameSimulation World(Vector3 spawn, GimmickSetup gimmicks, CollisionWorld world = null, HumanDefinition human = null, SkillLoadout skills = null, ulong seed = 7)
        {
            return new GameSimulation(Settings, new SimulationSetup(world ?? new CollisionWorld(), spawn, human, seed, null, skills, gimmicks));
        }

        private static GimmickSetup Fans(params FanDefinition[] fans) => new GimmickSetup(fans, null, null);

        private static GimmickSetup Coils(params Vector3[] coils) => new GimmickSetup(null, coils, null);

        // ---------- 선풍기 ----------

        [Test]
        public void Fan_InsideCone_NoInput_Pushed40PerSecond()
        {
            var simulation = World(FanPosition + new Vector3(50f, 0f, 0f), Fans(FanPlusX));
            Vector3 start = simulation.Player.Position;

            Run(simulation, PlayerCommand.None, 6);

            float speed = (simulation.Player.Position.X - start.X) / (6 * GameSimulation.DeltaTime);
            Assert.That(Settings.Fan.WindSpeed, Is.EqualTo(40f));
            Assert.That(speed, Is.EqualTo(40f).Within(0.5f));
        }

        [Test]
        public void Fan_Attached_NotAffected()
        {
            var world = new CollisionWorld();
            Vector3 wallCenter = FanPosition + new Vector3(80f, 0f, 0f);
            world.Add(CollisionShape.Box("wall", wallCenter, new Vector3(1f, 100f, 100f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = World(wallCenter - new Vector3(1.5f, 0f, 0f), Fans(FanPlusX), world);
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Vector3 attached = simulation.Player.Position;

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(Vector3.Distance(simulation.Player.Position, attached), Is.LessThan(1e-3f));
        }

        [Test]
        public void Fan_Oscillates45DegreesOver8SecondPeriod()
        {
            var simulation = World(Vector3.Zero, Fans(FanPlusX));
            var fans = simulation.Fans;
            var fan = fans.Fans[0];

            Assert.That(Settings.Fan.OscillationPeriod, Is.EqualTo(8f));
            Assert.That(fans.HeadYaw(fan, SecondsToTicks(0f)), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(fans.HeadYaw(fan, SecondsToTicks(2f)), Is.EqualTo(90f + 45f).Within(Tolerance));
            Assert.That(fans.HeadYaw(fan, SecondsToTicks(6f)), Is.EqualTo(90f - 45f).Within(Tolerance));
            Assert.That(fans.HeadYaw(fan, SecondsToTicks(8f)), Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Fan_NoiseMask_DashNoiseRadiusBecomes75()
        {
            var inside = World(FanPosition + new Vector3(0f, 0f, 150f), Fans(FanPlusX));
            inside.Step(new PlayerCommand { DashPressed = true });
            var maskedNoise = inside.Events.OfType<NoiseEmitted>().Single();

            var outside = World(FanPosition + new Vector3(0f, 0f, 300f), Fans(FanPlusX));
            outside.Step(new PlayerCommand { DashPressed = true });
            var normalNoise = outside.Events.OfType<NoiseEmitted>().Single();

            Assert.That(maskedNoise.Radius, Is.EqualTo(75f).Within(1e-3f));
            Assert.That(normalNoise.Radius, Is.EqualTo(150f).Within(1e-3f));
        }

        // ---------- 거미줄 ----------

        [Test]
        public void Web_Contact_DiesWithWebCauseAfter15Seconds_CannotMove()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("web", new Vector3(0f, 100f, 20f), new Vector3(30f, 30f, 0.5f), ShapeFlags.Hazard));
            var simulation = World(new Vector3(0f, 100f, 0f), GimmickSetup.None, world);

            int ticks = 0;
            while (simulation.Player.State != PlayerState.Webbed && ticks++ < SecondsToTicks(2f))
            {
                simulation.Step(Forward);
            }

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Webbed));
            int webbedTick = simulation.Player.WebbedTick;
            Vector3 caught = simulation.Player.Position;
            List<PlayerDied> deaths = new List<PlayerDied>();
            while (simulation.Player.State != PlayerState.Dead && simulation.Tick < webbedTick + SecondsToTicks(3f))
            {
                simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });
                deaths.AddRange(simulation.Events.OfType<PlayerDied>());
            }

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dead));
            Assert.That(deaths.Single().Cause, Is.EqualTo(DeathCause.Web));
            Assert.That((deaths.Single().Tick - webbedTick) * GameSimulation.DeltaTime, Is.EqualTo(1.5f).Within(GameSimulation.DeltaTime));
            Assert.That(Vector3.Distance(simulation.Player.Position, caught), Is.LessThan(1e-3f), "no escape");
        }

        // ---------- 모기약 연무 · 중독 ----------

        [Test]
        public void SprayCloud_Expands25To60OverOneSecond_GoneAfter8Seconds()
        {
            var simulation = World(new Vector3(0f, 100f, 500f), GimmickSetup.None);
            var toxin = simulation.Toxin;
            var cloud = toxin.Spawn(new Vector3(0f, 100f, 0f), 0);

            Assert.That(toxin.Radius(cloud, 0), Is.EqualTo(25f).Within(Tolerance));
            Assert.That(toxin.Radius(cloud, SecondsToTicks(0.5f)), Is.EqualTo(42.5f).Within(Tolerance));
            Assert.That(toxin.Radius(cloud, SecondsToTicks(1f)), Is.EqualTo(60f).Within(Tolerance));
            Assert.That(toxin.Radius(cloud, SecondsToTicks(3f)), Is.EqualTo(60f).Within(Tolerance));
            Assert.That(toxin.IsAlive(cloud, SecondsToTicks(8f) - 1), Is.True);
            Assert.That(toxin.IsAlive(cloud, SecondsToTicks(8f)), Is.False);

            Run(simulation, PlayerCommand.None, SecondsToTicks(8f) + 1);
            Assert.That(toxin.Clouds, Is.Empty);
        }

        [Test]
        public void SprayCloud_InWind_DriftsAt20PerSecond()
        {
            var simulation = World(new Vector3(0f, 100f, 500f), Fans(FanPlusX));
            var cloud = simulation.Toxin.Spawn(FanPosition + new Vector3(60f, 0f, 0f), 0);
            Vector3 start = cloud.Position;

            simulation.Step(PlayerCommand.None);

            float speed = (cloud.Position.X - start.X) / GameSimulation.DeltaTime;
            Assert.That(speed, Is.EqualTo(20f).Within(0.1f));
        }

        [Test]
        public void Toxin_RisesInCloud30PerSecond_Decays15PerSecondOutside()
        {
            var simulation = World(new Vector3(0f, 100f, 0f), GimmickSetup.None);
            simulation.Toxin.Spawn(new Vector3(0f, 100f, 0f), 0);
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Player.Toxin, Is.EqualTo(30f).Within(0.6f));

            simulation.Player.Position = new Vector3(0f, 100f, 1000f);
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Player.Toxin, Is.EqualTo(15f).Within(0.6f));
        }

        [Test]
        public void Toxin_100_DiesWithSprayCause()
        {
            var simulation = World(new Vector3(0f, 100f, 0f), GimmickSetup.None);
            simulation.Toxin.Spawn(new Vector3(0f, 100f, 0f), 0);
            var deaths = new List<PlayerDied>();
            for (int i = 0; i < SecondsToTicks(5f) && simulation.Player.State != PlayerState.Dead; i++)
            {
                simulation.Step(PlayerCommand.None);
                deaths.AddRange(simulation.Events.OfType<PlayerDied>());
            }

            Assert.That(deaths.Single().Cause, Is.EqualTo(DeathCause.Spray));
            Assert.That(simulation.Tick * GameSimulation.DeltaTime, Is.EqualTo(100f / 30f).Within(0.1f), "about 3 seconds in the middle of a cloud");
        }

        [Test]
        public void StutterChance_Is01At30_03At55()
        {
            var toxin = World(Vector3.Zero, GimmickSetup.None).Toxin;
            Assert.That(toxin.StutterChance(29f), Is.EqualTo(0f));
            Assert.That(toxin.StutterChance(30f), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(toxin.StutterChance(55f), Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(toxin.StutterChance(90f), Is.EqualTo(0.3f).Within(1e-5f));
        }

        /// <summary>고정 중독값에서 2초 동안 필터한 이동 입력 목록.</summary>
        private static List<(Vector2 Move, bool Dash)> Filtered(float toxinValue, ulong seed)
        {
            var simulation = World(Vector3.Zero, GimmickSetup.None, seed: seed);
            simulation.Player.Toxin = toxinValue;
            var result = new List<(Vector2, bool)>();
            var input = new PlayerCommand { Move = new Vector2(0f, 1f), DashPressed = true };
            for (int tick = 0; tick < SecondsToTicks(2f); tick++)
            {
                var filtered = simulation.Toxin.Filter(input, simulation.Player, tick);
                result.Add((filtered.Move, filtered.DashPressed));
            }

            return result;
        }

        [Test]
        public void Debuffs_StutterInvertRandom_StackByTier_ReproducibleWithSeed()
        {
            var clean = Filtered(0f, 1);
            Assert.That(clean.All(c => c.Move == new Vector2(0f, 1f) && c.Dash), Is.True, "no debuff below tier1");

            var tier1 = Filtered(30f, 1);
            Assert.That(tier1.Any(c => c.Move == Vector2.Zero && !c.Dash), Is.True, "stutter blocks move and dash");
            Assert.That(tier1.Where(c => c.Move != Vector2.Zero).All(c => c.Move == new Vector2(0f, 1f)), Is.True, "no inversion at tier1");

            var tier2 = Filtered(55f, 1);
            Assert.That(tier2.Where(c => c.Move != Vector2.Zero).All(c => c.Move == new Vector2(0f, -1f)), Is.True, "inverted at tier2");
            Assert.That(tier2.Any(c => c.Move == Vector2.Zero), Is.True, "stutter still applies");

            var tier3 = Filtered(80f, 1);
            Assert.That(tier3.Any(c => c.Move != Vector2.Zero && c.Move != new Vector2(0f, -1f)), Is.True, "random direction overrides input");

            Assert.That(Filtered(80f, 1), Is.EqualTo(tier3), "same seed, same debuffs");
            Assert.That(Filtered(80f, 2), Is.Not.EqualTo(tier3), "different seed differs");
        }

        [Test]
        public void HumanSpray_OnlyInFrenzyWithCanSprayWhenVisibleInRange_RespectsCooldown()
        {
            var sprayer = TestHumans.Seated(traits: new HumanTraits(new HumanModifier[0], true, null));
            Vector3 inRange = TestHumans.InFront(140f);

            var calm = TestHumans.Simulation(inRange, human: sprayer);
            TestHumans.DisableReactions(calm);
            Run(calm, PlayerCommand.None, SecondsToTicks(0.5f));
            Assert.That(calm.Human.Attack.Kind == AttackKind.Spray && calm.Human.Attack.IsBusy, Is.False, "not frenzied: no spray");

            var cannot = TestHumans.Simulation(inRange);
            TestHumans.Provoke(cannot, 100f);
            var cannotEvents = RunCollect(cannot, SecondsToTicks(1f));
            Assert.That(cannotEvents.OfType<SprayReleased>(), Is.Empty, "canSpray false");

            var simulation = TestHumans.Simulation(inRange, human: sprayer);
            TestHumans.Provoke(simulation, 100f);
            var events = RunCollect(simulation, SecondsToTicks(1f));
            var released = events.OfType<SprayReleased>().ToList();
            Assert.That(released, Has.Count.EqualTo(1));
            var telegraph = events.OfType<AttackTelegraphStarted>().First(e => e.Kind == AttackKind.Spray);
            Assert.That((released[0].Tick - telegraph.Tick) * GameSimulation.DeltaTime, Is.EqualTo(Settings.Toxin.Telegraph).Within(GameSimulation.DeltaTime));
            Assert.That(simulation.Toxin.Clouds, Has.Count.EqualTo(1));

            var later = RunCollect(simulation, SecondsToTicks(Settings.Toxin.Cooldown - 2f));
            Assert.That(later.OfType<SprayReleased>(), Is.Empty, "cooldown");

            var far = TestHumans.Simulation(TestHumans.InFront(250f), human: sprayer);
            TestHumans.Provoke(far, 100f);
            Assert.That(RunCollect(far, SecondsToTicks(1f)).OfType<SprayReleased>(), Is.Empty, "out of spray.useRange");
        }

        private static List<SimulationEvent> RunCollect(GameSimulation simulation, int ticks)
        {
            var events = new List<SimulationEvent>();
            for (int i = 0; i < ticks; i++)
            {
                simulation.Step(PlayerCommand.None);
                events.AddRange(simulation.Events);
            }

            return events;
        }

        [Test]
        public void Dispenser_CreatesCloudEvery12Seconds()
        {
            var dispenser = new Vector3(0f, 200f, 0f);
            var simulation = World(new Vector3(0f, 100f, 900f), new GimmickSetup(null, null, new[] { dispenser }));
            var spawnTicks = new List<int>();
            int known = -1;
            for (int i = 0; i < SecondsToTicks(37f); i++)
            {
                simulation.Step(PlayerCommand.None);
                foreach (var cloud in simulation.Toxin.Clouds.Where(c => c.Id > known))
                {
                    spawnTicks.Add(cloud.StartTick);
                    known = cloud.Id;
                }
            }

            Assert.That(spawnTicks.Select(t => t * GameSimulation.DeltaTime), Is.EqualTo(new[] { 12f, 24f, 36f }).Within(GameSimulation.DeltaTime));
        }

        // ---------- 모기향 ----------

        [Test]
        public void CoilFloor_60Within60_30At400_0Beyond_HalvedInWindAndShadow()
        {
            var coil = new Vector3(0f, 0f, 0f);
            var toxin = World(Vector3.Zero, Coils(coil)).Toxin;
            Assert.That(toxin.CoilFloor(coil + new Vector3(50f, 0f, 0f), false, 0), Is.EqualTo(60f).Within(Tolerance));
            Assert.That(toxin.CoilFloor(coil + new Vector3(400f, 0f, 0f), false, 0), Is.EqualTo(30f).Within(Tolerance));
            Assert.That(toxin.CoilFloor(coil + new Vector3(230f, 0f, 0f), false, 0), Is.EqualTo(45f).Within(Tolerance), "linear in between");
            Assert.That(toxin.CoilFloor(coil + new Vector3(401f, 0f, 0f), false, 0), Is.EqualTo(0f));
            Assert.That(toxin.CoilFloor(coil + new Vector3(50f, 0f, 0f), true, 0), Is.EqualTo(30f).Within(Tolerance), "shadow halves");

            var windy = World(Vector3.Zero, new GimmickSetup(new[] { new FanDefinition("fan", new Vector3(-100f, 0f, 0f), 90f) }, new[] { coil }, null)).Toxin;
            Assert.That(windy.CoilFloor(coil + new Vector3(50f, 0f, 0f), false, 0), Is.EqualTo(30f).Within(Tolerance), "wind halves");
            Assert.That(windy.CoilFloor(coil + new Vector3(50f, 0f, 0f), true, 0), Is.EqualTo(15f).Within(Tolerance), "both multiply");
        }

        [Test]
        public void Coil_Within20_RisesExtra25PerSecond()
        {
            var coil = new Vector3(0f, 100f, 0f);
            var simulation = World(coil + new Vector3(10f, 0f, 0f), Coils(coil));
            simulation.Player.Toxin = 60f;
            Run(simulation, PlayerCommand.None, SecondsToTicks(0.5f));
            Assert.That(simulation.Player.Toxin, Is.EqualTo(60f + (25f * 0.5f)).Within(0.6f));
        }

        [Test]
        public void Hidden_ToxinDecaysTwiceAsFast_ButNotBelowCoilFloor()
        {
            var world = ShadowWorld(new Vector3(0f, 100f, 0f), 50f);
            var exposed = World(new Vector3(0f, 100f, 600f), GimmickSetup.None);
            var hidden = World(new Vector3(0f, 100f, 0f), GimmickSetup.None, world);
            exposed.Player.Toxin = 50f;
            hidden.Player.Toxin = 50f;
            Run(exposed, PlayerCommand.None, SecondsToTicks(1f));
            Run(hidden, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(hidden.Player.IsHidden, Is.True);
            Assert.That(50f - hidden.Player.Toxin, Is.EqualTo(2f * (50f - exposed.Player.Toxin)).Within(0.6f));

            var coil = new Vector3(0f, 100f, 100f);
            var floored = World(new Vector3(0f, 100f, 0f), Coils(coil), ShadowWorld(new Vector3(0f, 100f, 0f), 50f));
            floored.Player.Toxin = 50f;
            Run(floored, PlayerCommand.None, SecondsToTicks(5f));
            float floor = floored.Toxin.CoilFloor(floored.Player.Position, true, floored.Tick);
            Assert.That(floor, Is.GreaterThan(0f));
            Assert.That(floored.Player.Toxin, Is.EqualTo(floor).Within(0.01f), "decays to the floor and stays");
        }

        [Test]
        public void ResistSpray_ScalesToxinRateAndCoilFloor()
        {
            var skills = SkillLoadout.Of((SkillCatalog.ResistSpray, 2));
            var settings = GameSettings.FromTuning(SkillEffects.Apply(Tuning, skills));
            float mul = SkillEffects.ToxinMultiplier(Tuning, skills);
            Assert.That(settings.Toxin.ToxinRate, Is.EqualTo(Settings.Toxin.ToxinRate * mul).Within(1e-3f));
            Assert.That(settings.Toxin.CoilNearFloor, Is.EqualTo(Settings.Toxin.CoilNearFloor * mul).Within(1e-3f));
            Assert.That(settings.Toxin.CoilFarFloor, Is.EqualTo(Settings.Toxin.CoilFarFloor * mul).Within(1e-3f));
            Assert.That(settings.Toxin.CoilCoreRate, Is.EqualTo(Settings.Toxin.CoilCoreRate * mul).Within(1e-3f));
        }

        // ---------- 취한 타겟 ----------

        private static HumanDefinition Drunk => TestHumans.Seated(traits: new HumanTraits(new[] { HumanModifier.Drunk }, false, null));

        [Test]
        public void Drunk_SuckRateMultiplierIs2()
        {
            var sober = TestHumans.Simulation(TestHumans.FarBehind);
            var drunk = TestHumans.Simulation(TestHumans.FarBehind, human: Drunk);
            Assert.That(drunk.Suck.RateMultiplier, Is.EqualTo(2f));

            float GaugeAfterSucking(GameSimulation simulation)
            {
                TestHumans.DisableReactions(simulation);
                simulation.Human.NextDrunkSwatTick = int.MaxValue;
                TestHumans.PlaceNearPart(simulation, "forearmR");
                simulation.Step(new PlayerCommand { AttachPressed = true });
                Run(simulation, new PlayerCommand { SuckHeld = true }, SecondsToTicks(1f));
                return simulation.Player.BloodGauge;
            }

            Assert.That(GaugeAfterSucking(drunk), Is.EqualTo(2f * GaugeAfterSucking(sober)).Within(0.05f));
        }

        private static List<(int Tick, Vector3 Target)> DrunkSwats(ulong seed)
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, human: Drunk, seed: seed);
            TestHumans.DisableReactions(simulation);
            var swats = new List<(int, Vector3)>();
            for (int i = 0; i < SecondsToTicks(40f); i++)
            {
                simulation.Step(PlayerCommand.None);
                foreach (var telegraph in simulation.Events.OfType<AttackTelegraphStarted>().Where(e => e.Kind == AttackKind.DrunkSwat))
                {
                    swats.Add((telegraph.Tick, telegraph.Target));
                }
            }

            return swats;
        }

        [Test]
        public void Drunk_RandomSwatsEvery4To7Seconds_ReproducibleWithSeed()
        {
            var swats = DrunkSwats(11);
            Assert.That(swats.Count, Is.GreaterThanOrEqualTo(5));
            for (int i = 1; i < swats.Count; i++)
            {
                float gap = (swats[i].Tick - swats[i - 1].Tick) * GameSimulation.DeltaTime;
                Assert.That(gap, Is.InRange(4f - GameSimulation.DeltaTime, 7f + GameSimulation.DeltaTime), $"gap {i}");
            }

            Assert.That(swats.All(s => s.Target.Length() <= Settings.Drunk.RandomSwatRadius + 1e-3f), Is.True, "within the radius around the body");
            Assert.That(DrunkSwats(11), Is.EqualTo(swats), "same seed, same swats");
            Assert.That(DrunkSwats(12), Is.Not.EqualTo(swats));
        }
    }
}
