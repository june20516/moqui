using UnityEngine;

namespace Moqui.Unity.Presentation.Art
{
    /// <summary>
    /// 공통 툰 머티리얼 (spec/10). 코드로 만드는 오브젝트도 이 머티리얼을 쓰고 색은 _BaseColor PropertyBlock으로 준다.
    /// 빌드에서도 불러오도록 Resources에 둔다 (씬 생성기가 만든다).
    /// </summary>
    public static class ToonMaterials
    {
        public const string OpaqueResource = "Materials/Toon";
        public const string TransparentResource = "Materials/ToonTransparent";
        public const string TelegraphResource = "Materials/TelegraphRing";
        public const string TelegraphShader = "Moqui/TelegraphRing";
        public const string OpaqueShader = "Moqui/Toon";
        public const string TransparentShader = "Moqui/ToonTransparent";

        private static Material _opaque;
        private static Material _transparent;
        private static Material _telegraph;

        public static Material Opaque => _opaque != null ? _opaque : _opaque = Load(OpaqueResource);

        /// <summary>반투명 변형 (물방울 등 코드로 만드는 반투명 표시).</summary>
        public static Material Transparent => _transparent != null ? _transparent : _transparent = Load(TransparentResource);

        /// <summary>공격 예고 고리 (좁혀 오는 테두리·차오름·번쩍임, M12).</summary>
        public static Material Telegraph => _telegraph != null ? _telegraph : _telegraph = Load(TelegraphResource);

        private static Material Load(string resource)
        {
            var material = Resources.Load<Material>(resource);
            if (material == null)
            {
                throw new System.InvalidOperationException($"Toon material '{resource}' is missing. Run Moqui/Rebuild All Sandboxes.");
            }

            return material;
        }
    }

    /// <summary>판정용 콜라이더 없이 툰 머티리얼을 입힌 프리미티브를 만든다 (판정은 Core가 한다).</summary>
    public static class Primitives
    {
        public static GameObject Create(PrimitiveType type, string name, Transform parent, Material material = null)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.GetComponent<Renderer>().sharedMaterial = material != null ? material : ToonMaterials.Opaque;
            return primitive;
        }
    }
}
