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

        /// <summary>미끼 표시 구 반지름 (표현 전용).</summary>
        private const float DecoyMarkerRadius = 3f;

        private readonly List<Renderer> _co2Pool = new List<Renderer>();
        private readonly Dictionary<string, Renderer> _heatGlows = new Dictionary<string, Renderer>();
        private readonly List<Renderer> _biteDots = new List<Renderer>();
        private readonly Dictionary<string, float> _heatIntensity = new Dictionary<string, float>();
        private readonly Dictionary<string, Renderer> _shadowCues = new Dictionary<string, Renderer>();

        private LevelMaterials _materials;
        private GameSimulation _simulation;
        private SensesSettings _settings;
        private Co2Plume _plume;
        private readonly List<Co2Plume> _plumes = new List<Co2Plume>();
        private MaterialPropertyBlock _block;
        private Renderer _decoy;

        /// <summary>미끼 마법 위치 표시 (장착하지 않았으면 null).</summary>
        public Renderer DecoyMarker => _decoy;

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

            if (simulation.Decoy.IsAvailable)
            {
                _decoy = CreateCue(PrimitiveType.Sphere, "Decoy", materials != null ? materials.Co2 : null);
                _decoy.transform.localScale = Vector3.one * (DecoyMarkerRadius * 2f);
            }

            if (simulation.Human == null)
            {
                return;
            }

            foreach (var human in simulation.Humans)
            {
                foreach (var site in human.SkinSites)
                {
                    string key = GlowKey(human, site.PartId);
                    _heatGlows.Add(key, CreateCue(PrimitiveType.Capsule, $"Heat_{key}", materials != null ? materials.Heat : null));
                }

                // 주 인간의 날숨은 기존 Plume, 함께 있는 인간은 각자 따로 (M14).
                _plumes.Add(human == simulation.Human ? _plume : new Co2Plume(settings));
            }
        }

        /// <summary>체온 표시 키: 주 인간은 부위 ID, 함께 있는 인간은 "인간ID.부위ID" (M14).</summary>
        private string GlowKey(Human human, string partId) => human == _simulation.Human ? partId : $"{human.Id}.{partId}";

        public Renderer HeatGlow(string partId) => _heatGlows[partId];

        /// <summary>자국 점 (자국마다 하나, 문 자리에 붙는다). 목록 길이는 지금까지 만든 점 수.</summary>
        public IReadOnlyList<Renderer> BiteDots => _biteDots;

        /// <summary>이번 프레임에 보인 자국 점 수.</summary>
        public int VisibleBiteDots { get; private set; }

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
            RenderDecoy();
            if (_simulation.Human == null)
            {
                return;
            }

            RenderHeat(viewer);
            int tick = _simulation.Tick;
            var fans = _simulation.Fans;
            for (int i = 0; i < _simulation.Humans.Count; i++)
            {
                var human = _simulation.Humans[i];
                _plumes[i].Update(deltaTime, human.IsExhaling, human.ExhalePosition.ToUnity(), human.HeadForward.ToUnity(), human.ExhaleStrength, position => fans.WindAt(position.ToCore(), tick).ToUnity());
            }

            RenderCo2(viewer);
        }

        private void LateUpdate()
        {
            if (IsBound)
            {
                Render(Time.deltaTime);
            }
        }

        private void RenderDecoy()
        {
            if (_decoy == null)
            {
                return;
            }

            var decoy = _simulation.Decoy;
            _decoy.enabled = decoy.IsActive(_simulation.Tick);
            if (_decoy.enabled)
            {
                _decoy.transform.position = decoy.Position.ToUnity();
                SetAlpha(_decoy, 1f);
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

        private void RenderHeat(Vector3 viewer)
        {
            _heatIntensity.Clear();
            int dotIndex = 0;
            foreach (var human in _simulation.Humans)
            {
                RenderHeat(human, viewer);
                dotIndex = RenderBiteDots(human, dotIndex);
            }

            VisibleBiteDots = 0;
            for (int i = 0; i < dotIndex; i++)
            {
                VisibleBiteDots += _biteDots[i].enabled ? 1 : 0;
            }

            for (int i = dotIndex; i < _biteDots.Count; i++)
            {
                _biteDots[i].enabled = false;
            }
        }

        private void RenderHeat(Human human, Vector3 viewer)
        {
            foreach (var site in human.SkinSites)
            {
                string key = GlowKey(human, site.PartId);
                Renderer glow = _heatGlows[key];
                float distance = Vector3.Distance(site.Shape.Center.ToUnity(), viewer);
                float intensity = SenseCueModel.HeatIntensity(distance, _settings.HeatRange);
                _heatIntensity[key] = intensity;
                glow.enabled = intensity > 0f;
                if (!glow.enabled)
                {
                    continue;
                }

                // 체온은 피부를 거의 덮지 않는 얇은 윤곽(림·아지랑이 셰이더)이다 (M12).
                WorldView.ApplyPose(site.Shape, glow.transform);
                Vector3 scale = glow.transform.localScale;
                glow.transform.localScale = new Vector3(scale.x * _settings.HeatGlowScale, scale.y, scale.z * _settings.HeatGlowScale);
                SetAlpha(glow, intensity);
            }

        }

        /// <summary>자국 점은 실제로 문 자리에, 그 부위의 체온이 보일 때만 (spec/04 §4, spec/11 §3, M12). 다음 점 번호를 돌려준다.</summary>
        private int RenderBiteDots(Human human, int dotIndex)
        {
            foreach (var mark in human.BiteMarks)
            {
                Renderer dot = BiteDotAt(dotIndex++);
                dot.enabled = _heatIntensity.TryGetValue(GlowKey(human, mark.PartId), out float intensity) && intensity > 0f;
                if (!dot.enabled)
                {
                    continue;
                }

                mark.Resolve(out var position, out var normal);
                dot.transform.position = (position + (normal * (_settings.BiteMarkDotRadius * 0.5f))).ToUnity();
                dot.transform.localScale = Vector3.one * (_settings.BiteMarkDotRadius * 2f);
            }

            return dotIndex;
        }

        private Renderer BiteDotAt(int index)
        {
            while (_biteDots.Count <= index)
            {
                _biteDots.Add(CreateCue(PrimitiveType.Sphere, $"BiteMark_{_biteDots.Count}", _materials != null ? _materials.BiteMark : null));
            }

            return _biteDots[index];
        }

        private void RenderCo2(Vector3 viewer)
        {
            VisibleCo2Puffs = 0;
            foreach (var plume in _plumes)
            {
                foreach (var puff in plume.Puffs)
                {
                    if (!plume.IsVisibleFrom(puff, viewer))
                    {
                        continue;
                    }

                    Renderer renderer = Co2Renderer(VisibleCo2Puffs++);
                    renderer.enabled = true;
                    renderer.transform.position = puff.Position;
                    renderer.transform.localScale = Vector3.one * (plume.Radius(puff) * 2f);
                    SetAlpha(renderer, plume.Opacity(puff));
                }
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
                // 기체형 표현: 셰이더(Moqui/SoftGas)가 카메라를 향하게 펴는 쿼드 (D-045).
                _co2Pool.Add(CreateCue(PrimitiveType.Quad, $"Co2_{_co2Pool.Count}", _materials != null ? _materials.Co2 : null));
            }

            return _co2Pool[index];
        }

        private Renderer CreateCue(PrimitiveType primitive, string name, Material material)
        {
            GameObject cue = Art.Primitives.Create(primitive, name, transform, material != null ? material : Art.ToonMaterials.Transparent);
            var renderer = cue.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

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
