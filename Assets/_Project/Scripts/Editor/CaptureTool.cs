using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// Tools/capture.ps1의 진입점 (tech/verification.md §4). 그래픽이 필요하므로 -nographics 없이 실행한다.
    /// 빌드 설정의 각 씬을 메인 카메라로 1장씩, Sandbox_Flight는 시점 확인용 포즈로 캡처한다. 스테이지별 포즈 캡처는 M7에서 확장한다.
    /// </summary>
    public static class CaptureTool
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const int DepthBits = 24;
        private const string CapturesFolder = "Captures";
        private const float WallApproachSeconds = 3f;

        [MenuItem("Moqui/Capture All")]
        public static void CaptureAll()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException("CaptureAll needs a graphics device. Run without -nographics.");
            }

            string outputDirectory = Path.Combine(
                BuildScript.ProjectRoot,
                CapturesFolder,
                DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(outputDirectory);

            foreach (string scenePath in EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path))
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string fileName = Path.GetFileNameWithoutExtension(scenePath) + ".png";
                CaptureCamera(MainCameraOrThrow(scenePath), Path.Combine(outputDirectory, fileName));
            }

            CaptureSandboxFlight(outputDirectory);
            CaptureSandboxHuman(outputDirectory);
            Debug.Log($"[CaptureTool] Captures written to {outputDirectory}");
        }

        /// <summary>
        /// 캡슐 인간의 세 상태: 평온, 대시 소음 뒤 의심(머리가 소리 쪽으로 돎), 광분 손바닥 예고(판정 위치 표시).
        /// </summary>
        public static void CaptureSandboxHuman(string outputDirectory)
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.HumanScenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(SandboxSceneBuilder.HumanScenePath);
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var setup = SandboxHumanWorld.CreateSetup();
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), setup);
            WorldView.Build(simulation.World, null);
            var humanView = UnityEngine.Object.FindAnyObjectByType<HumanView>();
            humanView.Build(simulation.Human);
            var player = GameObject.Find("Player");
            Vector3 head = simulation.Human.HeadCenter.ToUnity();
            Vector3 overview = head + new Vector3(140f, 60f, 170f);

            Step(simulation, PlayerCommand.None, 1f);
            CaptureHumanPose(camera, player, humanView, simulation, overview, head, outputDirectory, "Sandbox_Human_safe");

            simulation.Player.Position = (head + new Vector3(60f, 15f, -70f)).ToCore();
            simulation.Step(new PlayerCommand { DashPressed = true });
            Step(simulation, PlayerCommand.None, 1f);
            CaptureHumanPose(camera, player, humanView, simulation, overview, head, outputDirectory, "Sandbox_Human_suspicious");

            // 직전 장면에서 머리가 소리 쪽(뒤)을 보고 있으므로 정면으로 되돌린 뒤 시야 안에서 광분시킨다.
            simulation.Player.Position = (head + new Vector3(30f, -5f, 80f)).ToCore();
            simulation.Human.HeadYaw = 0f;
            simulation.Human.HeadPitch = 0f;
            simulation.Human.Awareness = tuning.GetFloat("awareness.frenzyEnter");
            simulation.Human.LastStimulusTick = simulation.Tick;
            for (int i = 0; i < GameSimulation.TickRate && simulation.Human.Attack.Phase != AttackPhase.Telegraph; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            CaptureHumanPose(camera, player, humanView, simulation, head + new Vector3(-120f, 40f, 160f), head, outputDirectory, "Sandbox_Human_frenzy_telegraph");
        }

        private static void Step(GameSimulation simulation, PlayerCommand command, float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds * GameSimulation.TickRate); i++)
            {
                simulation.Step(command);
            }
        }

        private static void CaptureHumanPose(Camera camera, GameObject player, HumanView humanView, GameSimulation simulation, Vector3 cameraPosition, Vector3 lookAt, string outputDirectory, string name)
        {
            player.transform.position = simulation.Player.Position.ToUnity();
            humanView.Refresh();
            var pose = new CameraPose(cameraPosition, Quaternion.LookRotation(lookAt - cameraPosition), camera.fieldOfView, camera.nearClipPlane);
            pose.ApplyTo(camera);
            CaptureCamera(camera, Path.Combine(outputDirectory, name + ".png"));
            Debug.Log($"[CaptureTool] {name}: state={simulation.Human.State}, awareness={simulation.Human.Awareness:F1}, headYaw={simulation.Human.HeadYaw:F1}, attack={simulation.Human.Attack.Phase}");
        }

        /// <summary>
        /// 3인칭 개요, 3인칭·1인칭 벽 접촉 포즈. 벽 접촉은 시뮬레이션으로 앞 벽까지 실제로 날아가서 만든다.
        /// 씬을 저장하지 않으므로 캡처용으로 만든 오브젝트는 남지 않는다.
        /// </summary>
        public static void CaptureSandboxFlight(string outputDirectory)
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.FlightScenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(SandboxSceneBuilder.FlightScenePath);
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var world = SandboxFlightWorld.Create();
            WorldView.Build(world, null);
            var cameraSettings = new CameraSettings(tuning);
            var solver = new CameraPoseSolver(cameraSettings, world);
            var player = GameObject.Find("Player");
            var visibility = player.GetComponent<PlayerViewVisibility>();

            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), world, SandboxFlightWorld.PlayerSpawn);
            Vector3 spawn = simulation.Player.Position.ToUnity();
            Capture(camera, player, visibility, spawn, solver.ThirdPerson(spawn, 30f, -10f), false, outputDirectory, "Sandbox_Flight_tp_overview");

            var forward = new PlayerCommand { Move = new System.Numerics.Vector2(0f, 1f) };
            for (int i = 0; i < Mathf.RoundToInt(WallApproachSeconds * GameSimulation.TickRate); i++)
            {
                simulation.Step(forward);
            }

            Vector3 contact = simulation.Player.Position.ToUnity();
            Capture(camera, player, visibility, contact, solver.FirstPerson(contact, 0f, 0f), true, outputDirectory, "Sandbox_Flight_fp_wall_contact");
            Capture(camera, player, visibility, contact, solver.FirstPerson(contact, -50f, 20f), true, outputDirectory, "Sandbox_Flight_fp_wall_contact_angled");
            Capture(camera, player, visibility, contact, solver.ThirdPerson(contact, 0f, 0f), false, outputDirectory, "Sandbox_Flight_tp_wall_contact");
        }

        public static void CaptureCamera(Camera camera, string path)
        {
            var renderTexture = new RenderTexture(Width, Height, DepthBits);
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Capture(Camera camera, GameObject player, PlayerViewVisibility visibility, Vector3 playerPosition, CameraPose pose, bool firstPerson, string outputDirectory, string name)
        {
            player.transform.position = playerPosition;
            visibility.SetFirstPerson(firstPerson);
            pose.ApplyTo(camera);
            CaptureCamera(camera, Path.Combine(outputDirectory, name + ".png"));
        }

        private static Camera MainCameraOrThrow(string scenePath)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException($"No main camera in scene {scenePath}.");
            }

            return camera;
        }
    }
}
