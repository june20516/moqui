using Moqui.Core.Data;
using Moqui.Core.Meta;
using Moqui.Unity.Data;
using Moqui.Unity.Editor;
using Moqui.Unity.Presentation.Senses;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Moqui.Unity.Tests
{
    /// <summary>흐린 시야 (spec/11 §1): clearRange 안 흐림 0, fogFullRange 밖 최대 흐림, 셰이더 파라미터.</summary>
    public class SensesFogTests
    {
        private const float Tolerance = 1e-4f;
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        private static SensesSettings LoadSettings()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            return new SensesSettings(tuning);
        }

        [Test]
        public void Amount_InsideClearRange_IsZero_BeyondFullRange_IsMax()
        {
            var settings = LoadSettings();
            float clear = settings.ClearRange;
            float full = settings.FogFullRange;
            float max = settings.FogMaxDensity;

            Assert.That(FogModel.Amount(0f, clear, full, max), Is.EqualTo(0f));
            Assert.That(FogModel.Amount(clear, clear, full, max), Is.EqualTo(0f));
            Assert.That(FogModel.Amount(full, clear, full, max), Is.EqualTo(max).Within(Tolerance));
            Assert.That(FogModel.Amount(full * 3f, clear, full, max), Is.EqualTo(max).Within(Tolerance));
            float middle = FogModel.Amount((clear + full) * 0.5f, clear, full, max);
            Assert.That(middle, Is.GreaterThan(0f).And.LessThan(max));
            Assert.That(max, Is.LessThan(1f), "silhouettes of large furniture remain visible at full fog");
        }

        [Test]
        public void ClearRange_InSteam_ScaledBySteamMultiplier()
        {
            var settings = LoadSettings();

            Assert.That(FogModel.ClearRange(settings, false), Is.EqualTo(settings.ClearRange));
            Assert.That(FogModel.ClearRange(settings, true), Is.EqualTo(settings.ClearRange * settings.SteamClearRangeMul).Within(Tolerance));
        }

        [Test]
        public void Apply_SetsGlobalShaderParameters_FromPlayerOrigin()
        {
            var settings = LoadSettings();
            var origin = new Vector3(10f, 20f, 30f);
            try
            {
                SensesFog.Apply(settings, origin, inSteam: true);

                Assert.That((Vector3)Shader.GetGlobalVector(SensesFog.OriginId), Is.EqualTo(origin));
                Assert.That(Shader.GetGlobalFloat(SensesFog.ClearId), Is.EqualTo(FogModel.ClearRange(settings, true)).Within(Tolerance));
                Assert.That(Shader.GetGlobalFloat(SensesFog.FullId), Is.EqualTo(settings.FogFullRange));
                Assert.That(Shader.GetGlobalFloat(SensesFog.MaxId), Is.EqualTo(settings.FogMaxDensity));
                Assert.That(Shader.GetGlobalFloat(SensesFog.BlurId), Is.EqualTo(settings.FogBlurPixels));
            }
            finally
            {
                SensesFog.Disable();
            }

            Assert.That(Shader.GetGlobalFloat(SensesFog.MaxId), Is.EqualTo(0f), "disabled fog leaves the image unchanged");
        }

        [Test]
        public void PcRenderer_HasFogPassBeforeTransparents_WithCompilingShader()
        {
            SandboxSceneBuilder.EnsureSensesFogFeature();
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);

            var feature = rendererData.rendererFeatures.Find(f => f != null && f.name == "SensesFog") as FullScreenPassRendererFeature;
            Assert.That(feature, Is.Not.Null);
            Assert.That(feature.isActive, Is.True);
            Assert.That(feature.injectionPoint, Is.EqualTo(FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents), "CO2 and heat cues draw after fog");
            Assert.That(feature.passMaterial.shader.name, Is.EqualTo("Moqui/SensesFog"));
            Assert.That(ShaderUtil.ShaderHasError(feature.passMaterial.shader), Is.False);
            Assert.That(rendererData.rendererFeatures.FindAll(f => f != null && f.name == "SensesFog").Count, Is.EqualTo(1), "added once");
        }
    

        [Test]
        public void CompoundEyes_ExtendsClearRangePerLevel()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            float baseClear = new SensesSettings(tuning).ClearRange;
            float add = tuning.GetFloat("skill.compoundEyes.clearRangeAdd");
            for (int level = 0; level <= 2; level++)
            {
                var skilled = new SensesSettings(SkillEffects.Apply(tuning, SkillLoadout.Of((SkillCatalog.CompoundEyes, level))));
                Assert.That(FogModel.ClearRange(skilled, false), Is.EqualTo(baseClear + (add * level)).Within(Tolerance), $"level {level}");
            }
        }
    }
}
