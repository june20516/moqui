using Moqui.Unity.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>CO₂·증기 기체형 표현 (D-045): 전용 셰이더가 컴파일되고 머티리얼에 연결되어 있다.</summary>
    public class GasRenderingTests
    {
        [TestCase("Assets/_Project/Materials/Senses_Co2.mat", "Moqui/SoftGas")]
        [TestCase("Assets/_Project/Materials/Level_Steam.mat", "Moqui/VolumeFog")]
        public void GasMaterial_UsesGasShader_WithoutErrors(string materialPath, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Assert.That(material, Is.Not.Null, materialPath);
            Assert.That(material.shader.name, Is.EqualTo(shaderName));
            Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, shaderName);
            Assert.That(material.renderQueue, Is.GreaterThanOrEqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent), "drawn after the senses fog pass");
        }
    }
}
