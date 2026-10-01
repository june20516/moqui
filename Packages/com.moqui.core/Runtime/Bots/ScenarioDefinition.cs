using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Data;
using Moqui.Core.Simulation;

namespace Moqui.Core.Bots
{
    public enum ScenarioStepKind
    {
        /// <summary>웨이포인트까지 비행.</summary>
        FlyTo,

        /// <summary>피부 부위 표면 1u 앞까지 접근해 부착.</summary>
        AttachSite,

        /// <summary>부착한 채 흡혈: 이번 세션 양(session) 또는 흡혈 게이지(gauge) 목표까지.</summary>
        Suck,

        Detach,

        /// <summary>가장 가까운 숨는 곳으로 가서 경계가 더 내려가지 않을 때까지 기다린다.</summary>
        Hide,

        /// <summary>제자리에서 seconds 동안 아무 입력 없이 기다린다.</summary>
        Wait,

        /// <summary>point로 날아가 seconds 동안 머문다 (발각 봇).</summary>
        Hover,
    }

    public sealed class ScenarioStep
    {
        public ScenarioStepKind Kind { get; set; }

        public Vector3 Point { get; set; }

        public string Part { get; set; }

        public float SessionAmount { get; set; }

        public float GaugeTarget { get; set; }

        public float Seconds { get; set; }

        /// <summary>flyTo: 처음부터 정밀 비행(소음 반경 절반)으로 간다 (귀 근처 접근용).</summary>
        public bool Precise { get; set; }
    }

    /// <summary>시나리오 결과 기대값 (tech/architecture.md §5 scenario JSON의 expect).</summary>
    public sealed class ScenarioExpectation
    {
        public StageOutcome Outcome { get; set; }

        public DeathCause? DeathCause { get; set; }

        public int? MaxFrenzies { get; set; }

        public int? MaxBiteMarks { get; set; }

        public int MaxTicks { get; set; }
    }

    /// <summary>
    /// 시나리오 데이터 (data/scenarios/*.json, tech/verification.md §3). 언어 중립 데이터로, 다른 구현도 같은 파일로 검증한다.
    /// start는 처음 한 번만 실행한다(시작 위치에서 접근 경로). steps는 한 회차이며 loop가 참이면 결과가 날 때까지 반복한다.
    /// flee가 참이면 예고·발각 시 회차를 멈추고 숨었다가 회차 처음부터 다시 한다.
    /// </summary>
    public sealed class ScenarioDefinition
    {
        public const int SupportedFormatVersion = 1;

        public string Id { get; private set; }

        public string LevelId { get; private set; }

        public IReadOnlyList<ulong> Seeds { get; private set; }

        public int MinSuccesses { get; private set; }

        /// <summary>
        /// 봇이 가지고 들어가는 스킬 (선택, 예: {"numbingSaliva": 2}). 앞 스테이지 보상으로 살 수 있는 수준만 쓴다 (D-048).
        /// </summary>
        public Meta.SkillLoadout Skills { get; private set; } = Meta.SkillLoadout.None;

        public bool Loop { get; private set; }

        public bool Flee { get; private set; }

        public IReadOnlyList<Vector3> HideSpots { get; private set; }

        /// <summary>
        /// 숨는 경로: 경유점을 차례로 지나 마지막 점에서 숨는다. hideSpots의 각 점은 한 점짜리 경로다.
        /// 도망칠 때는 첫 점이 가장 가까운 경로를 고른다 (가구에 막히는 직선 비행을 피하려고, D-047).
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Vector3>> HideRoutes { get; private set; }

        public IReadOnlyList<ScenarioStep> Start { get; private set; }

        public IReadOnlyList<ScenarioStep> Steps { get; private set; }

        public ScenarioExpectation Expect { get; private set; }

        public static string FilePath(string id)
        {
            return $"scenarios/{id}.json";
        }

        public static ScenarioDefinition Parse(string text, string file)
        {
            var root = JsonAccess.Parse(text, file);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
            }

            var seeds = new List<ulong>();
            foreach (var item in root.Get("seeds").Items())
            {
                seeds.Add(item.ULong());
            }

            var hideSpots = new List<Vector3>();
            foreach (var item in root.OptionalItems("hideSpots"))
            {
                hideSpots.Add(item.Vector3());
            }

            var hideRoutes = new List<IReadOnlyList<Vector3>>();
            foreach (var spot in hideSpots)
            {
                hideRoutes.Add(new[] { spot });
            }

            foreach (var route in root.OptionalItems("hideRoutes"))
            {
                var points = new List<Vector3>();
                foreach (var point in route.Items())
                {
                    points.Add(point.Vector3());
                }

                if (points.Count == 0)
                {
                    throw route.Error("must have at least one point");
                }

                hideRoutes.Add(points);
            }

            var start = new List<ScenarioStep>();
            foreach (var item in root.OptionalItems("start"))
            {
                start.Add(ParseStep(item));
            }

            var steps = new List<ScenarioStep>();
            foreach (var item in root.Get("steps").Items())
            {
                steps.Add(ParseStep(item));
            }

            var expectJson = root.Get("expect");
            var expect = new ScenarioExpectation
            {
                Outcome = expectJson.Get("outcome").Enum<StageOutcome>(),
                DeathCause = expectJson.Has("deathCause") ? expectJson.Get("deathCause").Enum<DeathCause>() : (DeathCause?)null,
                MaxFrenzies = expectJson.Has("maxFrenzies") ? expectJson.Get("maxFrenzies").Int() : (int?)null,
                MaxBiteMarks = expectJson.Has("maxBiteMarks") ? expectJson.Get("maxBiteMarks").Int() : (int?)null,
                MaxTicks = expectJson.Get("maxTicks").Int(),
            };

            return new ScenarioDefinition
            {
                Id = root.Get("id").String(),
                LevelId = root.Get("level").String(),
                Seeds = seeds,
                MinSuccesses = root.Get("minSuccesses").Int(),
                Skills = ParseSkills(root),
                Loop = root.Has("loop") && root.Get("loop").Bool(),
                Flee = root.Has("flee") && root.Get("flee").Bool(),
                HideSpots = hideSpots,
                HideRoutes = hideRoutes,
                Start = start,
                Steps = steps,
                Expect = expect,
            };
        }

        private static Meta.SkillLoadout ParseSkills(JsonAccess root)
        {
            if (!root.Has("skills"))
            {
                return Meta.SkillLoadout.None;
            }

            var skills = root.Get("skills");
            var levels = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var pair in skills.Value.Members)
            {
                if (!Meta.SkillCatalog.Exists(pair.Key))
                {
                    throw skills.Error($"unknown skill '{pair.Key}'");
                }

                levels[pair.Key] = (int)pair.Value.NumberValue;
            }

            return new Meta.SkillLoadout(levels, null);
        }

        private static ScenarioStep ParseStep(JsonAccess json)
        {
            var step = new ScenarioStep { Kind = json.Get("do").Enum<ScenarioStepKind>() };
            switch (step.Kind)
            {
                case ScenarioStepKind.FlyTo:
                    step.Point = json.Get("point").Vector3();
                    step.Precise = json.Has("precise") && json.Get("precise").Bool();
                    break;
                case ScenarioStepKind.AttachSite:
                    step.Part = json.Get("part").String();
                    break;
                case ScenarioStepKind.Suck:
                    step.SessionAmount = json.Has("session") ? json.Get("session").Float() : float.PositiveInfinity;
                    step.GaugeTarget = json.Has("gauge") ? json.Get("gauge").Float() : SuckSystem.GaugeMax;
                    break;
                case ScenarioStepKind.Wait:
                    step.Seconds = json.Get("seconds").Float();
                    break;
                case ScenarioStepKind.Hover:
                    step.Point = json.Get("point").Vector3();
                    step.Seconds = json.Get("seconds").Float();
                    break;
            }

            return step;
        }
    }
}
