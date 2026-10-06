using System.Collections.Generic;
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
                // 에어컨 송풍 날개는 켜져 있을 때만 보인다 (주기 읽기, spec/06 M14).
                head.GetComponent<Renderer>().enabled = fans.IsOn(fan, tick);
            }

            for (int i = 0; i < _lamps.Count; i++)
            {
                bool on = _simulation.Lights.IsOn(i);
                _lamps[i].enabled = on;
                _block.SetColor(BaseColorId, on ? LampColor : Color.gray);
                _bulbs[i].SetPropertyBlock(_block);
            }

            RenderCoilSmoke(deltaTime, tick);
            RenderClouds(tick);
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
