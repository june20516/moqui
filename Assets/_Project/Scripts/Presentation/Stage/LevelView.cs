using System;
using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Data.Levels;
using UnityEngine;

namespace Moqui.Unity.Presentation.Stage
{
    /// <summary>
    /// 레벨 데이터의 모든 형상을 시각 오브젝트로 만든다 (spec/07: 시각 오브젝트와 형상 ID 1:1).
    /// 가구(장애물·유리)는 화이트박스 프리미티브, Shadow Zone은 은신처 표시 볼륨, 습기 영역은 증기 볼륨이다.
    /// 판정은 Core가 하므로 Unity 콜라이더는 만들지 않는다.
    /// </summary>
    public static class LevelView
    {
        /// <summary>약한 습기 영역의 증기 밀도 배율 (표현 전용, 강한 영역 = 1).</summary>
        public const float WeakSteamDensityScale = 0.5f;

        public static readonly int DensityScaleId = Shader.PropertyToID("_DensityScale");

        public static Dictionary<string, GameObject> Build(LevelDefinition level, CollisionWorld world, Transform parent, LevelMaterials materials)
        {
            var root = new GameObject($"Level_{level.Id}").transform;
            root.SetParent(parent, false);
            var objects = new Dictionary<string, GameObject>();
            foreach (var definition in level.AllShapes())
            {
                if (!world.TryGet(definition.Id, out var shape))
                {
                    throw new InvalidOperationException($"Shape '{definition.Id}' of level '{level.Id}' is not in the collision world.");
                }

                GameObject visual = WorldView.CreateShapeObject(shape, root);
                ApplyMaterial(visual.GetComponent<Renderer>(), shape, materials);
                objects.Add(definition.Id, visual);
            }

            return objects;
        }

        private static void ApplyMaterial(Renderer renderer, CollisionShape shape, LevelMaterials materials)
        {
            if (materials == null)
            {
                return;
            }

            Material material = MaterialFor(shape, materials);
            if (material == null)
            {
                return;
            }

            // 화이트박스 색 PropertyBlock이 반투명 머티리얼의 알파를 덮지 않도록 지운다.
            renderer.SetPropertyBlock(null);
            renderer.sharedMaterial = material;
            if (shape.Matches(ShapeFlags.HumidWeak) && !shape.Matches(ShapeFlags.HumidStrong))
            {
                var block = new MaterialPropertyBlock();
                block.SetFloat(DensityScaleId, WeakSteamDensityScale);
                renderer.SetPropertyBlock(block);
            }
            if (!shape.Matches(ShapeFlags.Glass))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Material MaterialFor(CollisionShape shape, LevelMaterials materials)
        {
            if (shape.Matches(ShapeFlags.ShadowZone))
            {
                return materials.ShadowCue;
            }

            if (shape.Matches(ShapeFlags.HumidStrong | ShapeFlags.HumidWeak))
            {
                return materials.Steam;
            }

            if (shape.Matches(ShapeFlags.Hazard))
            {
                return materials.Web;
            }

            return shape.Matches(ShapeFlags.Glass) ? materials.Glass : null;
        }
    }
}
