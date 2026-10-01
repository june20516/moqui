using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Unity.Presentation.Stage;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moqui.Unity.Presentation.Senses
{
    /// <summary>
    /// 모기 감각 표현 (spec/11 §2~4): CO₂ 흐름, 체온 빛과 물린 자국 점, 은신처 표시 강도.
    /// 모두 반투명 큐라서 흐린 시야 패스 뒤에 그려지고 안개 영향을 받지 않는다. 판정에는 쓰지 않는다.
    /// </summary>
    public sealed class SensesView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Renderer> _co2Pool = new List<Renderer>();
        private readonly Dictionary<string, Renderer> _heatGlows = new Dictionary<string, Renderer>();
        private readonly Dictionary<string, Renderer> _biteDots = new Dictionary<string, Renderer>();
        private readonly Dictionary<string, Renderer> _shadowCues = new Dictionary<string, Renderer>();

        private LevelMaterials _materials;
        private GameSimulation _simulation;
        private SensesSettings _settings;
        private Co2Plume _plume;
        private MaterialPropertyBlock _block;

        public bool IsBound => _simulation != null;

        public Co2Plume Plume => _plume;

        /// <summary>보이는 CO₂ 덩이 수 (테스트·캡처 로그용).</summary>
        public int VisibleCo2Puffs { get; private set; }

        public void Bind(GameSimulation simulation, SensesSettings settings, IReadOnlyDictionary<string, GameObject> levelVisuals, LevelMaterials materials)
        {
            _simulation = simulation;
            _settings = settings;
            _materials = materials;
            _plume = new Co2Plume(settings);
            _block = new MaterialPropertyBlock();
            foreach (var zone in simulation.World.Shapes)
            {
                if (zone.Matches(ShapeFlags.ShadowZone) && levelVisuals.TryGetValue(zone.Id, out var visual))
                {
                    _shadowCues.Add(zone.Id, visual.GetComponent<Renderer>());
                }
            }

            if (simulation.Human == null)
            {
                return;
            }

            foreach (var site in simulation.Human.SkinSites)
            {
                _heatGlows.Add(site.PartId, CreateCue(PrimitiveType.Capsule, $"Heat_{site.PartId}", materials != null ? materials.Heat : null));
                _biteDots.Add(site.PartId, CreateCue(PrimitiveType.Sphere, $"BiteMark_{site.PartId}", materials != null ? materials.BiteMark : null));
            }
        }

        public Renderer HeatGlow(string partId) => _heatGlows[partId];

        public Renderer BiteDot(string partId) => _biteDots[partId];

        public Renderer ShadowCue(string zoneId) => _shadowCues[zoneId];

        /// <summary>현재 표시 세기(머티리얼 알파에 곱하는 값). 표시가 꺼져 있으면 0.</summary>
        public float ShadowCueIntensity(string zoneId)
        {
            var renderer = _shadowCues[zoneId];
            if (!renderer.enabled)
            {
                return 0f;
            }

            return CurrentIntensity(renderer);
        }

        public float HeatIntensity(string partId)
        {
            var renderer = _heatGlows[partId];
            if (!renderer.enabled)
            {
                return 0f;
            }

            return CurrentIntensity(renderer);
        }

        /// <summary>한 프레임 갱신. 테스트에서 직접 부를 수 있다.</summary>
        public void Render(float deltaTime)
        {
            Vector3 viewer = _simulation.Player.Position.ToUnity();
            RenderShadowCues(viewer);
            var human = _simulation.Human;
            if (human == null)
            {
                return;
            }

            RenderHeat(human, viewer);
            _plume.Update(deltaTime, human.IsExhaling, human.ExhalePosition.ToUnity(), human.HeadForward.ToUnity(), human.ExhaleStrength);
            RenderCo2(viewer);
        }

        private void LateUpdate()
        {
            if (IsBound)
            {
                Render(Time.deltaTime);
            }
        }

        private void RenderShadowCues(Vector3 viewer)
        {
            float intensity = SenseCueModel.ShadowCueIntensity(_settings, _simulation.Human?.State);
            foreach (var cue in _shadowCues)
            {
                _simulation.World.TryGet(cue.Key, out var zone);
                Vector3 closest = ShapeGeometry.Closest(zone, viewer.ToCore()).Point.ToUnity();
                bool inRange = Vector3.Distance(closest, viewer) <= _settings.CueRange;
                cue.Value.enabled = inRange;
                if (inRange)
                {
                    SetAlpha(cue.Value, intensity);
                }
            }
        }

        private void RenderHeat(Human human, Vector3 viewer)
        {
            foreach (var site in human.SkinSites)
            {
                Renderer glow = _heatGlows[site.PartId];
                Renderer dot = _biteDots[site.PartId];
                float distance = Vector3.Distance(site.Shape.Center.ToUnity(), viewer);
                float intensity = SenseCueModel.HeatIntensity(distance, _settings.HeatRange);
                glow.enabled = intensity > 0f;
                dot.enabled = intensity > 0f && site.HasBiteMark;
                if (!glow.enabled)
                {
                    continue;
                }

                WorldView.ApplyPose(site.Shape, glow.transform);
                Vector3 scale = glow.transform.localScale;
                glow.transform.localScale = new Vector3(scale.x * _settings.HeatGlowScale, scale.y, scale.z * _settings.HeatGlowScale);
                SetAlpha(glow, intensity);
                if (dot.enabled)
                {
                    // 자국 점은 부위 윗면(빛 바깥)에 붙인다.
                    float glowRadius = site.Shape.Radius * _settings.HeatGlowScale;
                    dot.transform.position = site.Shape.Center.ToUnity() + (Vector3.up * glowRadius);
                    dot.transform.localScale = Vector3.one * (_settings.BiteMarkDotRadius * 2f);
                }
            }
        }

        private void RenderCo2(Vector3 viewer)
        {
            VisibleCo2Puffs = 0;
            foreach (var puff in _plume.Puffs)
            {
                if (!_plume.IsVisibleFrom(puff, viewer))
                {
                    continue;
                }

                Renderer renderer = Co2Renderer(VisibleCo2Puffs++);
                renderer.enabled = true;
                renderer.transform.position = puff.Position;
                renderer.transform.localScale = Vector3.one * (_plume.Radius(puff) * 2f);
                SetAlpha(renderer, _plume.Opacity(puff));
            }

            for (int i = VisibleCo2Puffs; i < _co2Pool.Count; i++)
            {
                _co2Pool[i].enabled = false;
            }
        }

        private Renderer Co2Renderer(int index)
        {
            while (_co2Pool.Count <= index)
            {
                _co2Pool.Add(CreateCue(PrimitiveType.Sphere, $"Co2_{_co2Pool.Count}", _materials != null ? _materials.Co2 : null));
            }

            return _co2Pool[index];
        }

        private Renderer CreateCue(PrimitiveType primitive, string name, Material material)
        {
            GameObject cue = GameObject.CreatePrimitive(primitive);
            cue.name = name;
            cue.transform.SetParent(transform, false);
            DestroyImmediate(cue.GetComponent<Collider>());
            var renderer = cue.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.enabled = false;
            return renderer;
        }

        /// <summary>머티리얼 기본 알파 × intensity를 PropertyBlock으로 준다 (머티리얼 에셋은 바꾸지 않음).</summary>
        private void SetAlpha(Renderer renderer, float intensity)
        {
            Color color = BaseColor(renderer);
            color.a *= intensity;
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(_block);
        }

        private float CurrentIntensity(Renderer renderer)
        {
            renderer.GetPropertyBlock(_block);
            float baseAlpha = BaseColor(renderer).a;
            return baseAlpha > 0f ? _block.GetColor(BaseColorId).a / baseAlpha : 0f;
        }

        private static Color BaseColor(Renderer renderer)
        {
            return renderer.sharedMaterial != null ? renderer.sharedMaterial.GetColor(BaseColorId) : Color.white;
        }
    }
}
