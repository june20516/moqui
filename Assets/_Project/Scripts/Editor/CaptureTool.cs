using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Sandbox;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Settings;
using Moqui.Unity.UI;
using Moqui.Unity.UI.Flow;
using Moqui.Unity.UI.Hud;
using Moqui.Core.Meta;
using Moqui.Core.Tutorial;
using Moqui.Core.Collision;
using Moqui.Core.Data.Levels;
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
        private const float HumanCloseupDistance = 25f;
        private const float StageSettleSeconds = 1f;
        private const float Co2SettleSeconds = 1.2f;
        private const float ShadowSettleSeconds = 5f;
        private const float HudCaptureYawOffset = 100f;
        private const float MenuCanvasDistance = 1f;
        private static readonly Vector2Int[] HudResolutions = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1440) };
        private const float OverviewBackOff = 60f;
        private const float OverviewRise = 80f;
        private const float OverviewWallMargin = 20f;
        private static readonly string[] StageLevelIds = { "stage01", "stage02" };

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

            CaptureMenus(outputDirectory);

            CaptureSandboxFlight(outputDirectory);
            CaptureSandboxHuman(outputDirectory);
            CaptureSandboxWater(outputDirectory);
            foreach (string levelId in StageLevelIds)
            {
                CaptureStage(levelId, outputDirectory);
            }

            CaptureHud(outputDirectory);

            Debug.Log($"[CaptureTool] Captures written to {outputDirectory}");
        }

        /// <summary>
        /// 스테이지 대표 캡처 (spec/07): 전경, 시작 위치, 인간 근접, Shadow Zone 내부(3인칭) + 시작 위치 1인칭.
        /// </summary>
        public static void CaptureStage(string levelId, string outputDirectory)
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.StageScenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(SandboxSceneBuilder.StageScenePath);
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load(levelId);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var materials = UnityEngine.Object.FindAnyObjectByType<LevelMaterials>();
            var visuals = LevelView.Build(level, simulation.World, null, materials);
            var humanView = UnityEngine.Object.FindAnyObjectByType<HumanView>();
            humanView.Build(simulation.Human);
            var senses = new SensesSettings(tuning);
            var sensesView = UnityEngine.Object.FindAnyObjectByType<SensesView>();
            sensesView.Bind(simulation, senses, visuals, materials);

            // 날숨 CO₂가 보이도록 날숨이 시작된 뒤 잠시 더 진행한다. 체온·자국 표시 확인용으로 오른 전완에 자국을 둔다.
            Step(simulation, sensesView, StageSettleSeconds);
            for (int i = 0; i < SimulationTime.ToTicks(simulation.Settings.Breath.Period) && !simulation.Human.IsExhaling; i++)
            {
                Step(simulation, sensesView, GameSimulation.DeltaTime);
            }

            Step(simulation, sensesView, Co2SettleSeconds);
            humanView.Refresh();
            simulation.Human.SkinSites.First(site => site.PartId == "forearmR").HasBiteMark = true;

            var solver = new CameraPoseSolver(new CameraSettings(tuning), simulation.World);
            var player = GameObject.Find("Player");
            var visibility = player.GetComponent<PlayerViewVisibility>();
            Vector3 spawn = simulation.Player.Position.ToUnity();
            Vector3 head = simulation.Human.HeadCenter.ToUnity();
            string prefix = $"Stage_{levelId}_";

            void Shot(Vector3 playerPosition, CameraPose pose, bool firstPerson, string name)
            {
                simulation.Player.Position = playerPosition.ToCore();
                sensesView.Render(0f);
                SensesFog.Apply(senses, playerPosition, false);
                Capture(camera, player, visibility, playerPosition, pose, firstPerson, outputDirectory, prefix + name);
            }

            Vector3 overviewPosition = OverviewPoint(simulation.World, spawn, head);
            Shot(spawn, new CameraPose(overviewPosition, Quaternion.LookRotation(head - overviewPosition), camera.fieldOfView, camera.nearClipPlane), false, "overview");

            float yawToHead = YawTowards(spawn, head);
            Shot(spawn, solver.ThirdPerson(spawn, yawToHead, -10f), false, "start");
            Shot(spawn, solver.FirstPerson(spawn, yawToHead, -5f), true, "start_fp");

            var site = simulation.Human.Shapes["forearmR"];
            Vector3 siteCenter = site.Center.ToUnity();
            Vector3 near = siteCenter + ((spawn - siteCenter).normalized * HumanCloseupDistance);
            Shot(near, solver.ThirdPerson(near, YawTowards(near, siteCenter), -15f), false, "human_close");

            var shadow = simulation.World.Shapes.First(shape => shape.Matches(ShapeFlags.ShadowZone));
            Vector3 hidden = shadow.Center.ToUnity();
            var vignette = UnityEngine.Object.FindAnyObjectByType<ShadowVignette>();
            vignette.Tick(tuning, true, ShadowSettleSeconds);
            Shot(hidden, solver.ThirdPerson(hidden, YawTowards(hidden, head), -5f), false, "shadow_zone");
            vignette.Tick(tuning, false, ShadowSettleSeconds);
            SensesFog.Disable();
            Debug.Log($"[CaptureTool] {prefix}: shapes={level.AllShapes().Count()}, shadow={shadow.Id}, human={simulation.Human.State}, co2Puffs={sensesView.Plume.Puffs.Count}");
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _files = new System.Collections.Generic.Dictionary<string, string>();

            public bool Exists(string fileName) => _files.ContainsKey(fileName);

            public string Read(string fileName) => _files[fileName];

            public void Write(string fileName, string text) => _files[fileName] = text;

            public void Move(string fromFileName, string toFileName)
            {
                _files[toFileName] = _files[fromFileName];
                _files.Remove(fromFileName);
            }

            public void Delete(string fileName) => _files.Remove(fileName);
        }

        private sealed class NullNavigator : ISceneNavigator
        {
            public void Load(ScreenId screen)
            {
            }

            public void Quit()
            {
            }
        }

        /// <summary>
        /// 메뉴 화면 캡처 (spec/08): Title, Title+설정, StageSelect, StageSelect+Skills, Ending. 예시 저장 데이터(Stage 1 클리어, 포인트 340)를 쓴다.
        /// 화면은 편집 모드에서 직접 만들고, 오버레이 캔버스를 카메라 캔버스로 바꿔 렌더 텍스처에 그린다.
        /// </summary>
        public static void CaptureMenus(string outputDirectory)
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var session = new GameSession(tuning, new SaveStore(new MemoryStorage()), new MemoryPreferenceStore(), id => id == "stage01" || id == "stage02");
            session.Save.BloodPoints = 340;
            session.Save.SkillLevels[SkillCatalog.SwiftWings] = 1;
            session.Save.Stages["stage01"] = new StageRecord { Cleared = true, BestSeconds = 128.4f, NoFrenzy = true, MinBiteMarks = 2 };
            var flow = new ScreenFlow(session, new NullNavigator());

            CaptureMenu<TitleScreen>(SandboxSceneBuilder.ScenePath(ScreenId.Title), flow, outputDirectory, "Menu_Title", null);
            CaptureMenu<TitleScreen>(SandboxSceneBuilder.ScenePath(ScreenId.Title), flow, outputDirectory, "Menu_Title_Settings", title => title.Settings.Open(title.SettingsButton));
            CaptureMenu<StageSelectScreen>(SandboxSceneBuilder.ScenePath(ScreenId.StageSelect), flow, outputDirectory, "Menu_StageSelect", null);
            CaptureMenu<StageSelectScreen>(SandboxSceneBuilder.ScenePath(ScreenId.StageSelect), flow, outputDirectory, "Menu_Skills", select =>
            {
                select.Skills.Open(select.SkillsButton);
                select.Skills.ShowTab(SkillCategory.Stats);
            });
            CaptureMenu<EndingScreen>(SandboxSceneBuilder.ScenePath(ScreenId.Ending), flow, outputDirectory, "Menu_Ending", null);
        }

        private static void CaptureMenu<T>(string scenePath, ScreenFlow flow, string outputDirectory, string name, Action<T> prepare)
            where T : ScreenBase
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(scenePath);
            var screen = UnityEngine.Object.FindAnyObjectByType<T>();
            screen.Initialize(flow);
            prepare?.Invoke(screen);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = MenuCanvasDistance;
            }

            CaptureCamera(camera, Path.Combine(outputDirectory, name + ".png"), Width, Height, () =>
            {
                camera.ResetAspect();
                RebuildUi();
            });
        }

        /// <summary>
        /// 배치 모드에는 다음 프레임이 없으므로 레이아웃과 글꼴 글리프 갱신을 즉시 끝낸다.
        /// 동적 글꼴은 첫 갱신에서 글리프 텍스처를 다시 만들고 텍스트를 다시 더럽히므로 한 번 더 갱신한다.
        /// </summary>
        private static void RebuildUi()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// HUD 해상도 검토 캡처 (spec/08): 1920×1080, 1280×720, 2560×1440. 겹침 검토를 위해 가능한 요소를 한 화면에 모두 켠다
        /// (광분·가려짐·흡혈 가려움·젖은 날개·습기·자국·예고 경고·머리 화살표·은신처 방향·튜토리얼 문구).
        /// </summary>
        public static void CaptureHud(string outputDirectory)
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.StageScenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(SandboxSceneBuilder.StageScenePath);
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load(StageLevelIds[0]);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var materials = UnityEngine.Object.FindAnyObjectByType<LevelMaterials>();
            LevelView.Build(level, simulation.World, null, materials);
            var humanView = UnityEngine.Object.FindAnyObjectByType<HumanView>();
            humanView.Build(simulation.Human);
            Step(simulation, PlayerCommand.None, StageSettleSeconds);
            humanView.Refresh();

            var player = simulation.Player;
            var human = simulation.Human;
            var site = human.SkinSites.First(s => s.PartId == "forearmR");
            player.BloodGauge = 62f;
            player.Stamina = simulation.Settings.Stamina.Max * 0.55f;
            player.WetRemaining = 3.4f;
            player.Humidity = 40f;
            player.SuckSession = new SuckSession(site, simulation.Tick);
            site.Itch = 40f;
            human.BiteMarkCount = 2;
            human.State = AwarenessState.Frenzy;
            human.FrenzyMinRemaining = 4.3f;
            human.CalmProgress = 0.35f;
            human.PlayerOccluded = true;

            Vector3 spawn = player.Position.ToUnity();
            Vector3 head = human.HeadCenter.ToUnity();
            var solver = new CameraPoseSolver(new CameraSettings(tuning), simulation.World);
            CameraPose pose = solver.ThirdPerson(spawn, YawTowards(spawn, head) + HudCaptureYawOffset, -10f);
            pose.ApplyTo(camera);
            GameObject.Find("Player").transform.position = spawn;
            human.Attack.Phase = AttackPhase.Telegraph;
            human.Attack.Target = (camera.transform.position - (camera.transform.forward * 50f)).ToCore();
            SensesFog.Apply(new SensesSettings(tuning), spawn, false);

            var hudView = UnityEngine.Object.FindAnyObjectByType<HudView>();
            hudView.Build();
            var canvas = hudView.GetComponent<Canvas>();
            var scaler = hudView.GetComponent<UnityEngine.UI.CanvasScaler>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane * 2f;
            scaler.enabled = false;
            var hints = new TutorialHints(new MemoryPreferenceStore());
            var tracker = new TutorialTracker(level.Tutorial, new TutorialSettings(tuning), level.Tutorial.ToList().IndexOf("dash"));

            foreach (var resolution in HudResolutions)
            {
                string path = Path.Combine(outputDirectory, $"Hud_{resolution.x}x{resolution.y}.png");
                CaptureCamera(camera, path, resolution.x, resolution.y, () =>
                {
                    camera.ResetAspect();

                    // CanvasScaler(기준 1920×1080, 너비·높이 0.5 맞춤)와 같은 배율을 직접 준다. 배치 모드에는 Update가 없다.
                    float logWidth = Mathf.Log(resolution.x / HudView.ReferenceResolution.x, 2f);
                    float logHeight = Mathf.Log(resolution.y / HudView.ReferenceResolution.y, 2f);
                    canvas.scaleFactor = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
                    Canvas.ForceUpdateCanvases();
                    hudView.Apply(HudState.Compute(simulation, camera, false, tuning), 0f);
                    hudView.TutorialText.text = hints.CurrentText(tracker, false);
                    Canvas.ForceUpdateCanvases();
                });
                Debug.Log($"[CaptureTool] Hud {resolution.x}x{resolution.y}: canvas={hudView.Root.rect.size}, scale={canvas.scaleFactor:F2}");
            }

            SensesFog.Disable();
        }

        /// <summary>시작 위치와 인간 머리의 중간 위쪽에서, 벽·천장 안쪽으로 물러난 점.</summary>
        private static Vector3 OverviewPoint(CollisionWorld world, Vector3 spawn, Vector3 head)
        {
            Vector3 middle = (spawn + head) * 0.5f;
            Vector3 desired = spawn + ((spawn - head).normalized * OverviewBackOff) + (Vector3.up * OverviewRise);
            Vector3 direction = desired - middle;
            if (world.Raycast(middle.ToCore(), System.Numerics.Vector3.Normalize(direction.ToCore()), direction.magnitude, ShapeFlags.Solid, out var hit))
            {
                return middle + (direction.normalized * Mathf.Max(0f, hit.Distance - OverviewWallMargin));
            }

            return desired;
        }

        private static float YawTowards(Vector3 from, Vector3 to)
        {
            Vector3 flat = to - from;
            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        /// <summary>물방울이 떨어지는 모습과, 물방울에 갇혀 함께 떨어지는 플레이어.</summary>
        public static void CaptureSandboxWater(string outputDirectory)
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.WaterScenePath, OpenSceneMode.Single);
            Camera camera = MainCameraOrThrow(SandboxSceneBuilder.WaterScenePath);
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), SandboxWaterWorld.CreateSetup());
            WorldView.Build(simulation.World, null);
            var waterView = UnityEngine.Object.FindAnyObjectByType<WaterView>();
            var player = GameObject.Find("Player");
            float dropRadius = simulation.Settings.Water.DropRadius;
            Vector3 lookAt = SandboxWaterWorld.UnderDrip.ToUnity();
            Vector3 cameraPosition = lookAt + new Vector3(-45f, 15f, -60f);

            simulation.Player.Position = SandboxWaterWorld.UnderDrip;
            Step(simulation, PlayerCommand.None, 0.4f);
            CaptureWaterPose(camera, player, waterView, simulation, dropRadius, cameraPosition, lookAt, outputDirectory, "Sandbox_Water_drop_falling");

            for (int i = 0; i < GameSimulation.TickRate && simulation.Player.State != PlayerState.Trapped; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            Step(simulation, PlayerCommand.None, 0.2f);
            CaptureWaterPose(camera, player, waterView, simulation, dropRadius, cameraPosition, simulation.Player.Position.ToUnity(), outputDirectory, "Sandbox_Water_trapped");
        }

        private static void CaptureWaterPose(Camera camera, GameObject player, WaterView waterView, GameSimulation simulation, float dropRadius, Vector3 cameraPosition, Vector3 lookAt, string outputDirectory, string name)
        {
            player.transform.position = simulation.Player.Position.ToUnity();
            waterView.Render(simulation.CaptureSnapshot().Drops, dropRadius);
            var pose = new CameraPose(cameraPosition, Quaternion.LookRotation(lookAt - cameraPosition), camera.fieldOfView, camera.nearClipPlane);
            pose.ApplyTo(camera);
            CaptureCamera(camera, Path.Combine(outputDirectory, name + ".png"));
            Debug.Log($"[CaptureTool] {name}: player={simulation.Player.State}, drops={waterView.VisibleDrops}, heightLeft={simulation.CaptureSnapshot().TrappedHeightRemaining:F1}");
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

        /// <summary>시뮬레이션과 감각 표현(CO₂ 흐름)을 함께 진행한다.</summary>
        private static void Step(GameSimulation simulation, SensesView sensesView, float seconds)
        {
            for (int i = 0; i < Mathf.Max(1, Mathf.RoundToInt(seconds * GameSimulation.TickRate)); i++)
            {
                simulation.Step(PlayerCommand.None);
                sensesView.Render(GameSimulation.DeltaTime);
            }
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
            CaptureCamera(camera, path, Width, Height, null);
        }

        /// <param name="beforeRender">렌더 대상 크기가 정해진 뒤 렌더 전에 부른다 (HUD 배치 갱신용).</param>
        public static void CaptureCamera(Camera camera, string path, int width, int height, Action beforeRender)
        {
            var renderTexture = new RenderTexture(width, height, DepthBits);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                beforeRender?.Invoke();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
