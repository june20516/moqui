using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Sandbox;
using Moqui.Unity.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
        public const string ControlsPath = "Assets/_Project/Input/MoquiControls.inputactions";
        public const string PlayerMaterialPath = "Assets/_Project/Materials/Whitebox_Player.mat";

        private const float PlayerVisualDiameter = 1f;
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColorProperty = "_BaseColor";
        private static readonly Color PlayerColor = new Color(0.2f, 0.85f, 0.9f);

        [MenuItem("Moqui/Rebuild All Sandboxes")]
        public static void BuildAll()
        {
            BuildFlightSandbox();
            BuildHumanSandbox();
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
