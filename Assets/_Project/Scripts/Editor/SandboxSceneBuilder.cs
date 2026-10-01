using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Sandbox;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.UI;
using Moqui.Unity.UI.Flow;
using Moqui.Unity.UI.Hud;
using Moqui.Unity.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// 테스트용 씬을 코드로 생성한다 (씬 YAML을 손으로 편집하지 않기 위해). 생성한 씬은 빌드 설정에 넣지 않는다.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        public const string FlightScenePath = "Assets/_Project/Scenes/Sandbox_Flight.unity";
        public const string HumanScenePath = "Assets/_Project/Scenes/Sandbox_Human.unity";
        public const string WaterScenePath = "Assets/_Project/Scenes/Sandbox_Water.unity";
        public const string StageScenePath = "Assets/_Project/Scenes/Stage.unity";
        public const string ScenesFolder = "Assets/_Project/Scenes";

        /// <summary>빌드에 들어가는 화면 씬 순서 (architecture §6: Boot → Title → StageSelect → Stage → Ending).</summary>
        public static readonly ScreenId[] BuildOrder = { ScreenId.Boot, ScreenId.Title, ScreenId.StageSelect, ScreenId.Stage, ScreenId.Ending };
        public const string ControlsPath = "Assets/_Project/Input/MoquiControls.inputactions";
        public const string PlayerMaterialPath = "Assets/_Project/Materials/Whitebox_Player.mat";

        private const string ShadowCueMaterialPath = "Assets/_Project/Materials/Level_ShadowCue.mat";
        private const string SteamMaterialPath = "Assets/_Project/Materials/Level_Steam.mat";
        private const string GlassMaterialPath = "Assets/_Project/Materials/Level_Glass.mat";
        private const string Co2MaterialPath = "Assets/_Project/Materials/Senses_Co2.mat";
        private const string HeatMaterialPath = "Assets/_Project/Materials/Senses_Heat.mat";
        private const string BiteMarkMaterialPath = "Assets/_Project/Materials/Senses_BiteMark.mat";
        private const string SensesFogMaterialPath = "Assets/_Project/Materials/Senses_Fog.mat";
        private const string SensesFogShaderName = "Moqui/SensesFog";
        private const string SensesFogFeatureName = "SensesFog";
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const float PlayerVisualDiameter = 1f;
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColorProperty = "_BaseColor";
        private static readonly Color PlayerColor = new Color(0.2f, 0.85f, 0.9f);
        private static readonly Color MenuBackground = new Color(0.1f, 0.08f, 0.15f);
        private static readonly Color ShadowCueColor = new Color(0.35f, 0.6f, 1f, 0.35f);
        private static readonly Color Co2Color = new Color(0.95f, 0.78f, 0.98f, 0.5f);
        private static readonly Color HeatColor = new Color(1f, 0.42f, 0.12f, 0.5f);
        private static readonly Color BiteMarkColor = new Color(0.9f, 0.1f, 0.15f, 0.95f);
        private static readonly Color SteamColor = new Color(0.9f, 0.95f, 1f, 0.25f);
        private static readonly Color GlassColor = new Color(0.75f, 0.9f, 0.95f, 0.3f);

        [MenuItem("Moqui/Rebuild All Sandboxes")]
        public static void BuildAll()
        {
            BuildFlightSandbox();
            BuildHumanSandbox();
            BuildWaterSandbox();
            BuildStage();
            BuildFlowScenes();
        }

        public static string ScenePath(ScreenId screen) => $"{ScenesFolder}/{screen}.unity";

        /// <summary>Boot·Title·StageSelect·Ending 씬과 빌드 설정 순서 (spec/08 화면 흐름).</summary>
        [MenuItem("Moqui/Rebuild Flow Scenes")]
        public static void BuildFlowScenes()
        {
            BuildMenuScene(ScreenId.Boot, root => root.AddComponent<BootLoader>());
            BuildMenuScene(ScreenId.Title, root => root.AddComponent<TitleScreen>());
            BuildMenuScene(ScreenId.StageSelect, root => root.AddComponent<StageSelectScreen>());
            BuildMenuScene(ScreenId.Ending, root => root.AddComponent<EndingScreen>());
            EditorBuildSettings.scenes = System.Array.ConvertAll(BuildOrder, screen => new EditorBuildSettingsScene(ScenePath(screen), true));
        }

        private static void BuildMenuScene(ScreenId screen, System.Action<GameObject> addScreen)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = MenuBackground;
            addScreen(new GameObject(screen.ToString()));
            Save(scene, ScenePath(screen));
        }

        /// <summary>레벨 데이터를 바꿔 끼우는 단일 Stage 씬 (tech/architecture.md §6). 빌드 설정에 포함한다.</summary>
        [MenuItem("Moqui/Rebuild Stage")]
        public static void BuildStage()
        {
            Scene scene = CreateScaffold(out SimulationRunner runner);
            SetReference(new GameObject("HumanView").AddComponent<HumanView>(), "_runner", runner);
            SetReference(new GameObject("WaterView").AddComponent<WaterView>(), "_runner", runner);

            var materials = new GameObject("LevelMaterials").AddComponent<LevelMaterials>();
            SetReference(materials, "_shadowCue", LoadOrCreateTransparentMaterial(ShadowCueMaterialPath, ShadowCueColor, doubleSided: true));
            SetReference(materials, "_steam", LoadOrCreateTransparentMaterial(SteamMaterialPath, SteamColor, doubleSided: true));
            SetReference(materials, "_glass", LoadOrCreateTransparentMaterial(GlassMaterialPath, GlassColor));
            SetReference(materials, "_co2", LoadOrCreateTransparentMaterial(Co2MaterialPath, Co2Color));
            SetReference(materials, "_heat", LoadOrCreateTransparentMaterial(HeatMaterialPath, HeatColor));
            SetReference(materials, "_biteMark", LoadOrCreateTransparentMaterial(BiteMarkMaterialPath, BiteMarkColor));
            var senses = new GameObject("SensesView").AddComponent<SensesView>();

            EnsureSensesFogFeature();
            SetReference(new GameObject("SensesFog").AddComponent<SensesFog>(), "_runner", runner);

            var bootstrap = new GameObject("StageBootstrap").AddComponent<StageBootstrap>();
            SetReference(bootstrap, "_runner", runner);
            SetReference(bootstrap, "_materials", materials);
            SetReference(bootstrap, "_senses", senses);
            var hud = new GameObject("Hud", typeof(RectTransform));
            hud.AddComponent<HudView>();
            var hudController = hud.AddComponent<HudController>();
            SetReference(hudController, "_runner", runner);
            SetReference(hudController, "_camera", Camera.main);
            SetReference(hudController, "_cameraRig", Object.FindAnyObjectByType<CameraRig>());
            SetReference(hudController, "_audio", hud.AddComponent<HudAudioSource>());
            SetReference(hudController, "_stage", bootstrap);

            var stageScreen = new GameObject("StageScreen").AddComponent<StageScreen>();
            SetReference(stageScreen, "_runner", runner);
            SetReference(stageScreen, "_stage", bootstrap);
            SetReference(stageScreen, "_controls", AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath));
            Save(scene, StageScenePath);
        }

        /// <summary>
        /// PC 렌더러에 흐린 시야 전체 화면 패스를 한 번만 추가한다 (투명 렌더링 전, 깊이 필요).
        /// 전역 최대 흐림이 0인 씬(Sandbox)에서는 원본을 그대로 낸다.
        /// </summary>
        public static void EnsureSensesFogFeature()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath)
                ?? throw new System.InvalidOperationException($"Renderer data not found: {PcRendererPath}");
            if (rendererData.rendererFeatures.Exists(feature => feature != null && feature.name == SensesFogFeatureName))
            {
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(SensesFogMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find(SensesFogShaderName) ?? throw new System.InvalidOperationException($"Shader '{SensesFogShaderName}' not found.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SensesFogMaterialPath);
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = SensesFogFeatureName;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            feature.requirements = ScriptableRenderPassInput.Depth;
            feature.fetchColorBuffer = true;
            feature.passMaterial = material;
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            // 렌더러 데이터 인스펙터의 기능 추가와 같은 방식: 목록과 localId 맵을 함께 늘린다.
            var serialized = new SerializedObject(rendererData);
            var features = serialized.FindProperty("m_RendererFeatures");
            var map = serialized.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SandboxSceneBuilder] Added {SensesFogFeatureName} to {PcRendererPath}");
        }

        [MenuItem("Moqui/Rebuild Sandbox_Flight")]
        public static void BuildFlightSandbox()
        {
            Scene scene = CreateScaffold(out SimulationRunner runner);
            var bootstrap = new GameObject("SandboxFlightBootstrap").AddComponent<SandboxFlightBootstrap>();
            SetReference(bootstrap, "_runner", runner);
            Save(scene, FlightScenePath);
        }

        [MenuItem("Moqui/Rebuild Sandbox_Human")]
        public static void BuildHumanSandbox()
        {
            Scene scene = CreateScaffold(out SimulationRunner runner);
            var humanView = new GameObject("HumanView").AddComponent<HumanView>();
            SetReference(humanView, "_runner", runner);
            var bootstrap = new GameObject("SandboxHumanBootstrap").AddComponent<SandboxHumanBootstrap>();
            SetReference(bootstrap, "_runner", runner);
            Save(scene, HumanScenePath);
        }

        [MenuItem("Moqui/Rebuild Sandbox_Water")]
        public static void BuildWaterSandbox()
        {
            Scene scene = CreateScaffold(out SimulationRunner runner);
            var waterView = new GameObject("WaterView").AddComponent<WaterView>();
            SetReference(waterView, "_runner", runner);
            var bootstrap = new GameObject("SandboxWaterBootstrap").AddComponent<SandboxWaterBootstrap>();
            SetReference(bootstrap, "_runner", runner);
            Save(scene, WaterScenePath);
        }

        /// <summary>조명, 카메라, 시뮬레이션 구동기, 플레이어 그림, 카메라 리그를 만든다.</summary>
        private static Scene CreateScaffold(out SimulationRunner runner)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";

            // 은신 비네트(Volume)가 화면에 나오려면 URP 후처리가 켜져 있어야 한다.
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            runner = new GameObject("SimulationRunner").AddComponent<SimulationRunner>();
            SetReference(runner, "_controls", AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath));

            var player = new GameObject("Player");
            SetReference(player.AddComponent<PlayerView>(), "_runner", runner);
            var visibility = player.AddComponent<PlayerViewVisibility>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localScale = Vector3.one * PlayerVisualDiameter;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // MaterialPropertyBlock은 씬에 저장되지 않으므로 플레이어 색은 머티리얼 에셋으로 둔다.
            body.GetComponent<Renderer>().sharedMaterial = LoadOrCreateLitMaterial(PlayerMaterialPath, PlayerColor);

            // Shadow Zone 비네트 (spec/03): 전역 Volume, 프로필은 실행 시 만든다.
            var vignetteObject = new GameObject("ShadowVignette");
            vignetteObject.AddComponent<UnityEngine.Rendering.Volume>();
            SetReference(vignetteObject.AddComponent<ShadowVignette>(), "_runner", runner);

            var rig = new GameObject("CameraRig").AddComponent<CameraRig>();
            SetReference(rig, "_runner", runner);
            SetReference(rig, "_camera", camera);
            SetReference(rig, "_playerVisibility", visibility);
            return scene;
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SandboxSceneBuilder] Saved {path}");
        }

        private static Material LoadOrCreateLitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader lit = Shader.Find(LitShaderName) ?? throw new System.InvalidOperationException($"Shader '{LitShaderName}' not found.");
                material = new Material(lit);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor(BaseColorProperty, color);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        /// <summary>URP Lit 반투명(알파 블렌드) 머티리얼. 은신처 표시·증기·유리처럼 뒤가 비쳐야 하는 볼륨에 쓴다.</summary>
        /// <param name="doubleSided">볼륨 안에 들어가도 보이게 할지 (은신처·증기).</param>
        private static Material LoadOrCreateTransparentMaterial(string path, Color color, bool doubleSided = false)
        {
            Material material = LoadOrCreateLitMaterial(path, color);
            material.SetFloat("_Cull", (float)(doubleSided ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Back));
            material.doubleSidedGI = doubleSided;
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
