using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Presentation.Stage;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moqui.Unity.Presentation.Gimmicks
{
    /// <summary>
    /// 환경 기믹 표현 (spec/06): 선풍기 머리 방향, 모기향 받침과 바람에 휘는 연기 줄기, 모기약 연무.
    /// Core 상태를 읽어서 그리기만 한다. 거미줄·선풍기 본체는 LevelView가 레벨 형상으로 그린다.
    /// </summary>
    public sealed class GimmickView : MonoBehaviour
    {
        private const float FanHeadOffset = 12f;
        private const float FanHeadDiameter = 26f;
        private const float FanHeadThickness = 3f;
        private const float CoilDiameter = 12f;
        private const float CoilHeight = 1.5f;
        private const float CoilSmokeStrength = 0.6f;
        private const float CloudFadeIn = 0.3f;
        private const float CloudFadeOut = 1.5f;
        private const float BulbDiameter = 12f;

        /// <summary>조명 표현 (M14): 점광원 세기와 거리는 영역 크기에 맞춘다.</summary>
        private const float LampIntensity = 2.5f;
        private static readonly Color LampColor = new Color(1f, 0.92f, 0.75f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<string, Transform> _fanHeads = new Dictionary<string, Transform>();
        private readonly List<Light> _lamps = new List<Light>();
        private readonly List<Renderer> _bulbs = new List<Renderer>();
        private readonly List<Renderer> _cloudPool = new List<Renderer>();
        private readonly List<Renderer> _smokePool = new List<Renderer>();
        private readonly List<Co2Plume> _coilPlumes = new List<Co2Plume>();
        private readonly List<WindStreak> _streaks = new List<WindStreak>();
        private readonly List<NetVisual> _nets = new List<NetVisual>();
        private readonly HashSet<string> _openingFans = new HashSet<string>();
        private readonly HashSet<int> _warmingLamps = new HashSet<int>();
        private float _streakTimer;
        private uint _streakSeed = 7;

        /// <summary>환경 예고 (gulf §8, 표현 전용).</summary>
        public const float LampWarmupSeconds = 1f;
        public const float AirconOpenLeadSeconds = 1f;
        public const float StreakInterval = 0.12f;
        public const float StreakLifetime = 1.2f;
        public const float NetRippleSpeedRatio = 0.5f;
        public const float NetRippleRange = 30f;
        public const float NetRippleAmplitude = 1.5f;
        private const int StreakPoolSize = 40;
        private static readonly Color StreakColor = new Color(0.9f, 0.95f, 1f, 1f);

        private sealed class WindStreak
        {
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 Velocity;
            public float Age = float.PositiveInfinity;
        }

        private sealed class NetVisual
        {
            public Moqui.Core.Collision.CollisionShape Shape;
            public Transform Transform;
            public Vector3 RestPosition;
            public Vector3 Normal;
            public float Energy;
            public List<Transform> Threads = new List<Transform>();
        }

        /// <summary>곧 켜지는 조명이 예열 중인가 (테스트용).</summary>
        public bool LampWarming(int index) => _warmingLamps.Contains(index);

        /// <summary>에어컨 송풍 날개가 켜지기 직전 열리는 중인가 (테스트용).</summary>
        public bool FanOpening(string fanId) => _openingFans.Contains(fanId);

        /// <summary>보이는 바람 줄기 수 (테스트용).</summary>
        public int ActiveStreaks { get; private set; }

        /// <summary>모기장이 출렁이는 세기 0~1 (테스트용).</summary>
        public float NetRipple(string shapeId)
        {
            foreach (var net in _nets)
            {
                if (net.Shape.Id == shapeId)
                {
                    return net.Energy;
                }
            }

            return 0f;
        }
        private GameSimulation _simulation;
        private LevelMaterials _materials;
        private MaterialPropertyBlock _block;

        public bool IsBound => _simulation != null;

        public int VisibleClouds { get; private set; }

        public int VisibleSmokePuffs { get; private set; }

        public Transform FanHead(string fanId) => _fanHeads[fanId];

        /// <summary>조명별 점광원 (켜진 동안만 켜짐, spec/06 M14).</summary>
        public IReadOnlyList<Light> Lamps => _lamps;

        public IReadOnlyList<Co2Plume> CoilPlumes => _coilPlumes;

        public void Bind(GameSimulation simulation, SensesSettings senses, LevelMaterials materials)
        {
            _simulation = simulation;
            _materials = materials;
            _block = new MaterialPropertyBlock();
            foreach (var fan in simulation.Fans.Fans)
            {
                var head = Create(PrimitiveType.Cylinder, $"FanHead_{fan.Id}", null);
                head.transform.localScale = new Vector3(FanHeadDiameter, FanHeadThickness * 0.5f, FanHeadDiameter);
                _fanHeads.Add(fan.Id, head.transform);
            }

            foreach (var lightDefinition in simulation.Lights.Lights)
            {
                var bulb = Create(PrimitiveType.Sphere, $"Bulb_{lightDefinition.Id}", null);
                bulb.transform.position = lightDefinition.Position.ToUnity();
                bulb.transform.localScale = Vector3.one * BulbDiameter;
                bulb.enabled = true;
                _bulbs.Add(bulb);
                var lamp = new GameObject($"Lamp_{lightDefinition.Id}").AddComponent<Light>();
                lamp.transform.SetParent(transform, false);
                lamp.transform.position = lightDefinition.Position.ToUnity();
                lamp.type = LightType.Point;
                lamp.color = LampColor;
                lamp.intensity = LampIntensity;
                lamp.range = lightDefinition.AreaHalfSize.Length() * 2f;
                lamp.enabled = false;
                _lamps.Add(lamp);
            }

            foreach (var coil in simulation.Toxin.Coils)
            {
                var stand = Create(PrimitiveType.Cylinder, "Coil", null);
                stand.transform.position = coil.ToUnity();
                stand.transform.localScale = new Vector3(CoilDiameter, CoilHeight * 0.5f, CoilDiameter);
                stand.enabled = true;
                _coilPlumes.Add(new Co2Plume(senses));
            }
        }

        /// <summary>한 프레임 갱신. 테스트에서 직접 부를 수 있다.</summary>
        public void Render(float deltaTime)
        {
            int tick = _simulation.Tick;
            var fans = _simulation.Fans;
            foreach (var fan in fans.Fans)
            {
                Vector3 direction = fans.Direction(fan, tick).ToUnity();
                var head = _fanHeads[fan.Id];
                head.position = fan.Position.ToUnity() + (direction * FanHeadOffset);
                head.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                // 에어컨 송풍 날개는 켜져 있을 때 보이고, 켜지기 1초 전부터 반쯤 열린다 (주기 읽기, spec/06 M14, gulf §8).
                bool on = fans.IsOn(fan, tick);
                bool opening = fan.Kind == FanKind.AirConditioner && !on && fans.IsOn(fan, tick + Moqui.Core.Simulation.SimulationTime.ToTicks(AirconOpenLeadSeconds));
                if (opening)
                {
                    _openingFans.Add(fan.Id);
                }
                else
                {
                    _openingFans.Remove(fan.Id);
                }

                head.GetComponent<Renderer>().enabled = on || opening || fan.Kind != FanKind.AirConditioner;
                head.localScale = new Vector3(FanHeadDiameter, FanHeadThickness * (opening ? 0.25f : 0.5f), FanHeadDiameter);
            }

            for (int i = 0; i < _lamps.Count; i++)
            {
                bool on = _simulation.Lights.IsOn(i);
                // 켜지기 직전: 전구가 희미하게 깜빡이며 예열한다 (조명이 켜질 것을 미리 읽게, gulf §8).
                bool warming = !on && _simulation.Lights.IsAboutToTurnOn(i, tick, LampWarmupSeconds);
                float flicker = warming ? 0.5f + (0.5f * Mathf.Sin(tick * 1.7f) * Mathf.Sin(tick * 0.43f)) : 0f;
                if (warming)
                {
                    _warmingLamps.Add(i);
                }
                else
                {
                    _warmingLamps.Remove(i);
                }

                _lamps[i].enabled = on || warming;
                _lamps[i].intensity = on ? LampIntensity : LampIntensity * 0.2f * flicker;
                _block.SetColor(BaseColorId, on ? LampColor : Color.Lerp(Color.gray, LampColor, flicker * 0.6f));
                _bulbs[i].SetPropertyBlock(_block);
            }

            RenderStreaks(deltaTime, tick);
            RenderNets(deltaTime);

            RenderCoilSmoke(deltaTime, tick);
            RenderClouds(tick);
        }

        /// <summary>
        /// 바람 줄기: 켜진 선풍기·에어컨 원뿔 안으로 옅은 줄이 흘러간다. 바람이 어디로 부는지 미리 보인다 (gulf §8).
        /// </summary>
        private void RenderStreaks(float deltaTime, int tick)
        {
            while (_streaks.Count < StreakPoolSize)
            {
                var streak = Create(PrimitiveType.Capsule, "WindStreak", Art.ToonMaterials.Transparent);
                streak.enabled = false;
                _streaks.Add(new WindStreak { Transform = streak.transform, Renderer = streak });
            }

            var fans = _simulation.Fans;
            _streakTimer += deltaTime;
            while (_streakTimer >= StreakInterval)
            {
                _streakTimer -= StreakInterval;
                foreach (var fan in fans.Fans)
                {
                    if (fans.IsOn(fan, tick))
                    {
                        SpawnStreak(fan.Position.ToUnity(), fans.Direction(fan, tick).ToUnity(), fans.Settings.WindSpeedOf(fan));
                    }
                }
            }

            int active = 0;
            foreach (var streak in _streaks)
            {
                if (!streak.Renderer.enabled)
                {
                    continue;
                }

                streak.Age += deltaTime;
                if (streak.Age >= StreakLifetime)
                {
                    streak.Renderer.enabled = false;
                    continue;
                }

                active++;
                float t = streak.Age / StreakLifetime;
                streak.Transform.position += streak.Velocity * deltaTime;
                WorldView.Tint(streak.Renderer, new Color(StreakColor.r, StreakColor.g, StreakColor.b, 0.22f * Mathf.Sin(Mathf.PI * t)));
            }

            ActiveStreaks = active;
        }

        private void SpawnStreak(Vector3 origin, Vector3 direction, float windSpeed)
        {
            WindStreak free = null;
            foreach (var streak in _streaks)
            {
                if (!streak.Renderer.enabled)
                {
                    free = streak;
                    break;
                }
            }

            if (free == null)
            {
                return;
            }

            Vector3 side = Vector3.Cross(direction, Vector3.up).sqrMagnitude > 1e-4f ? Vector3.Cross(direction, Vector3.up).normalized : Vector3.right;
            Vector3 up = Vector3.Cross(side, direction).normalized;
            Vector3 spread = (side * (NextSigned() * 0.3f)) + (up * (NextSigned() * 0.3f));
            Vector3 heading = (direction + spread).normalized;
            free.Transform.SetPositionAndRotation(origin + (heading * 12f), Quaternion.FromToRotation(Vector3.up, heading));
            free.Transform.localScale = new Vector3(0.6f, 6f, 0.6f);
            free.Velocity = heading * (windSpeed * 1.5f);
            free.Age = 0f;
            free.Renderer.enabled = true;
        }

        private float NextSigned()
        {
            _streakSeed = (_streakSeed * 1664525u) + 1013904223u;
            return ((_streakSeed >> 8) / (float)(1u << 24) * 2f) - 1f;
        }

        /// <summary>
        /// 모기장: 빠르게 다가가면 그물이 출렁이고(통과 소음 예고), 틈에는 실밥이 나풀거린다 (gulf §8).
        /// visuals는 LevelView가 만든 형상 ID → 그림.
        /// </summary>
        public void BindNets(IReadOnlyDictionary<string, GameObject> visuals)
        {
            foreach (var shape in _simulation.World.Shapes)
            {
                bool net = shape.Matches(ShapeFlags.Net);
                bool gap = shape.Matches(ShapeFlags.NetGap);
                if ((!net && !gap) || shape.Type != ShapeType.Box)
                {
                    continue;
                }

                var visual = new NetVisual { Shape = shape, Normal = ThinAxis(shape).ToUnity() };
                if (net && visuals != null && visuals.TryGetValue(shape.Id, out var gameObject))
                {
                    visual.Transform = gameObject.transform;
                    visual.RestPosition = gameObject.transform.position;
                }

                if (gap)
                {
                    // 틈 위쪽 가장자리에 실밥 세 가닥.
                    Vector3 center = shape.Center.ToUnity();
                    for (int i = 0; i < 3; i++)
                    {
                        var thread = Create(PrimitiveType.Capsule, "NetThread", null);
                        thread.enabled = true;
                        thread.transform.position = center + new Vector3((i - 1) * shape.HalfExtents.X * 0.5f, shape.HalfExtents.Y - 3f, 0f);
                        thread.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
                        visual.Threads.Add(thread.transform);
                    }
                }

                _nets.Add(visual);
            }
        }

        private void RenderNets(float deltaTime)
        {
            var player = _simulation.Player;
            float fast = _simulation.Settings.Flight.Speed * NetRippleSpeedRatio;
            float time = _simulation.Tick * Moqui.Core.Simulation.GameSimulation.DeltaTime;
            foreach (var net in _nets)
            {
                float distance = ShapeGeometry.Closest(net.Shape, player.Position).Distance;
                if (distance < NetRippleRange && player.Velocity.Length() > fast)
                {
                    net.Energy = 1f;
                }

                net.Energy = Mathf.Max(0f, net.Energy - (2f * deltaTime));
                if (net.Transform != null)
                {
                    net.Transform.position = net.RestPosition + (net.Normal * (Mathf.Sin(time * 12f) * NetRippleAmplitude * net.Energy));
                }

                for (int i = 0; i < net.Threads.Count; i++)
                {
                    net.Threads[i].rotation = Quaternion.Euler(0f, 0f, 15f * Mathf.Sin((time * 3f) + i));
                }
            }
        }

        /// <summary>상자에서 가장 얇은 축 방향 (그물면의 법선).</summary>
        private static System.Numerics.Vector3 ThinAxis(Moqui.Core.Collision.CollisionShape box)
        {
            var half = box.HalfExtents;
            var local = half.X <= half.Y && half.X <= half.Z ? System.Numerics.Vector3.UnitX : (half.Y <= half.Z ? System.Numerics.Vector3.UnitY : System.Numerics.Vector3.UnitZ);
            return box.ToWorldDirection(local);
        }

        private void LateUpdate()
        {
            if (IsBound)
            {
                Render(Time.deltaTime);
            }
        }

        private void RenderCoilSmoke(float deltaTime, int tick)
        {
            var fans = _simulation.Fans;
            var coils = _simulation.Toxin.Coils;
            int used = 0;
            for (int i = 0; i < coils.Count; i++)
            {
                var plume = _coilPlumes[i];
                plume.Update(deltaTime, true, coils[i].ToUnity() + (Vector3.up * CoilHeight), Vector3.up, CoilSmokeStrength, position => fans.WindAt(position.ToCore(), tick).ToUnity());
                foreach (var puff in plume.Puffs)
                {
                    var renderer = Pooled(_smokePool, used++, "CoilSmoke", _materials != null ? _materials.CoilSmoke : null);
                    renderer.transform.position = puff.Position;
                    renderer.transform.localScale = Vector3.one * (plume.Radius(puff) * 2f);
                    SetAlpha(renderer, plume.Opacity(puff));
                }
            }

            VisibleSmokePuffs = used;
            Hide(_smokePool, used);
        }

        private void RenderClouds(int tick)
        {
            float lifetime = _simulation.Settings.Toxin.Lifetime;
            var clouds = _simulation.Toxin.Clouds;
            for (int i = 0; i < clouds.Count; i++)
            {
                var cloud = clouds[i];
                float age = (tick - cloud.StartTick) * GameSimulation.DeltaTime;
                float fade = Mathf.Clamp01(age / CloudFadeIn) * Mathf.Clamp01((lifetime - age) / CloudFadeOut);
                var renderer = Pooled(_cloudPool, i, "SprayCloud", _materials != null ? _materials.Spray : null);
                renderer.transform.position = cloud.Position.ToUnity();
                renderer.transform.localScale = Vector3.one * (_simulation.Toxin.Radius(cloud, tick) * 2f);
                SetAlpha(renderer, fade);
            }

            VisibleClouds = clouds.Count;
            Hide(_cloudPool, clouds.Count);
        }

        private Renderer Pooled(List<Renderer> pool, int index, string name, Material material)
        {
            while (pool.Count <= index)
            {
                // 기체형 표현: 셰이더(Moqui/SoftGas)가 카메라를 향하게 펴는 쿼드 (D-045).
                pool.Add(Create(PrimitiveType.Quad, $"{name}_{pool.Count}", material));
            }

            var renderer = pool[index];
            renderer.enabled = true;
            return renderer;
        }

        private static void Hide(List<Renderer> pool, int from)
        {
            for (int i = from; i < pool.Count; i++)
            {
                pool[i].enabled = false;
            }
        }

        private Renderer Create(PrimitiveType primitive, string name, Material material)
        {
            GameObject go = Art.Primitives.Create(primitive, name, transform, material);
            var renderer = go.GetComponent<Renderer>();
            if (material != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            renderer.enabled = false;
            return renderer;
        }

        private void SetAlpha(Renderer renderer, float intensity)
        {
            Color color = renderer.sharedMaterial != null ? renderer.sharedMaterial.GetColor(BaseColorId) : Color.white;
            color.a *= intensity;
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(_block);
        }
    }
}
