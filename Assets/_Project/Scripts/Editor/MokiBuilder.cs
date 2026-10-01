using System;
using System.IO;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Art;
using Moqui.Unity.Simulation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// 플레이어 캐릭터 "모키" (spec/10): 2.5등신 데포르메 몸, 반투명 날개 2장, 지팡이 주둥이, 리본.
    /// 프리미티브 조합과 코드로 만든 애니메이션 클립 7종·Animator Controller를 생성한다 (자체 제작물, Art/Generated).
    /// </summary>
    public static class MokiBuilder
    {
        public const string GeneratedFolder = "Assets/_Project/Art/Generated/Moki";
        public const string ControllerPath = GeneratedFolder + "/Moki.controller";
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string RigName = "Rig";
        private const string WingLeftPath = RigName + "/WingPivotL";
        private const string WingRightPath = RigName + "/WingPivotR";
        private const string ProboscisPath = RigName + "/Proboscis";
        private const float StateBlendSeconds = 0.08f;
        private const float SamplesPerSecond = 60f;

        /// <summary>작은 캐릭터라 외곽선을 공통값보다 얇게 한다 (월드 단위).</summary>
        private const float OutlineWidth = 0.015f;

        /// <summary>어두운 방에서도 플레이어가 보이도록 바탕색 쪽으로 끌어올리는 비율 (spec/10 가독성 우선).</summary>
        private const float SelfIllumination = 0.3f;

        private static readonly Color SkinColor = new Color(1f, 0.9f, 0.86f);
        private static readonly Color DressColor = new Color(1f, 0.86f, 0.92f);
        private static readonly Color HairColor = new Color(1f, 0.72f, 0.84f);
        private static readonly Color RibbonColor = new Color(0.95f, 0.38f, 0.62f);
        private static readonly Color EyeColor = new Color(0.16f, 0.12f, 0.3f);
        private static readonly Color WandColor = new Color(0.97f, 0.97f, 1f);
        private static readonly Color WingColor = new Color(0.86f, 0.95f, 1f, 0.35f);

        // 쉬는 자세: 날개는 위로 20°, 주둥이는 아래로 10°.
        private static readonly Vector3 WingLeftRest = new Vector3(0f, 0f, 20f);
        private static readonly Vector3 WingRightRest = new Vector3(0f, 0f, -20f);
        private static readonly Vector3 ProboscisRest = new Vector3(10f, 0f, 0f);

        /// <summary>플레이어 오브젝트 아래에 모키를 만들고 Animator·MokiAnimator를 붙인다.</summary>
        public static void Build(GameObject player, SimulationRunner runner)
        {
            Materials materials = LoadOrCreateMaterials();
            var rig = new GameObject(RigName).transform;
            rig.SetParent(player.transform, false);

            Part(PrimitiveType.Capsule, "Body", rig, materials.Dress, new Vector3(0f, -0.06f, 0f), Vector3.zero, new Vector3(0.3f, 0.17f, 0.26f));
            Part(PrimitiveType.Cylinder, "Skirt", rig, materials.Dress, new Vector3(0f, -0.17f, 0f), Vector3.zero, new Vector3(0.46f, 0.07f, 0.42f));
            Part(PrimitiveType.Sphere, "Head", rig, materials.Skin, new Vector3(0f, 0.24f, 0.02f), Vector3.zero, Vector3.one * 0.4f);
            Part(PrimitiveType.Sphere, "Hair", rig, materials.Hair, new Vector3(0f, 0.28f, -0.04f), Vector3.zero, new Vector3(0.44f, 0.42f, 0.42f));
            Part(PrimitiveType.Sphere, "EyeL", rig, materials.Eye, new Vector3(-0.08f, 0.25f, 0.2f), Vector3.zero, Vector3.one * 0.07f);
            Part(PrimitiveType.Sphere, "EyeR", rig, materials.Eye, new Vector3(0.08f, 0.25f, 0.2f), Vector3.zero, Vector3.one * 0.07f);
            Part(PrimitiveType.Cube, "RibbonL", rig, materials.Ribbon, new Vector3(-0.11f, 0.48f, -0.06f), new Vector3(0f, 0f, 20f), new Vector3(0.18f, 0.1f, 0.05f));
            Part(PrimitiveType.Cube, "RibbonR", rig, materials.Ribbon, new Vector3(0.11f, 0.48f, -0.06f), new Vector3(0f, 0f, -20f), new Vector3(0.18f, 0.1f, 0.05f));

            Transform wingLeft = Pivot("WingPivotL", rig, new Vector3(-0.08f, 0.08f, -0.12f), WingLeftRest);
            Part(PrimitiveType.Quad, "Wing", wingLeft, materials.Wing, new Vector3(-0.3f, 0.04f, 0f), Vector3.zero, new Vector3(0.55f, 0.26f, 1f));
            Transform wingRight = Pivot("WingPivotR", rig, new Vector3(0.08f, 0.08f, -0.12f), WingRightRest);
            Part(PrimitiveType.Quad, "Wing", wingRight, materials.Wing, new Vector3(0.3f, 0.04f, 0f), Vector3.zero, new Vector3(0.55f, 0.26f, 1f));

            // 지팡이 주둥이: 흡혈 때 피부 쪽으로 내려 꽂는다.
            Transform proboscis = Pivot("Proboscis", rig, new Vector3(0f, 0.17f, 0.2f), ProboscisRest);
            Part(PrimitiveType.Cylinder, "Wand", proboscis, materials.Wand, new Vector3(0f, 0f, 0.14f), new Vector3(90f, 0f, 0f), new Vector3(0.035f, 0.14f, 0.035f));
            Part(PrimitiveType.Sphere, "Tip", proboscis, materials.Ribbon, new Vector3(0f, 0f, 0.3f), Vector3.zero, Vector3.one * 0.08f);

            var animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = LoadOrCreateController();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var mokiAnimator = player.AddComponent<MokiAnimator>();
            SandboxSceneBuilder.SetReference(mokiAnimator, "_runner", runner);
            SandboxSceneBuilder.SetReference(mokiAnimator, "_animator", animator);
        }

        /// <summary>상태 7종(MokiPose 순서)과 State 정수 파라미터를 가진 컨트롤러. 에셋 GUID를 유지하도록 있으면 비우고 다시 채운다.</summary>
        public static AnimatorController LoadOrCreateController()
        {
            Directory.CreateDirectory(GeneratedFolder);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (var parameter in controller.parameters)
            {
                controller.RemoveParameter(parameter);
            }

            controller.AddParameter(MokiAnimator.StateParameter, AnimatorControllerParameterType.Int);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (var transition in machine.anyStateTransitions)
            {
                machine.RemoveAnyStateTransition(transition);
            }

            foreach (var child in machine.states)
            {
                machine.RemoveState(child.state);
            }

            foreach (MokiPose pose in Enum.GetValues(typeof(MokiPose)))
            {
                AnimatorState state = machine.AddState(pose.ToString());
                state.motion = LoadOrCreateClip(pose);
                AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, (int)pose, MokiAnimator.StateParameter);
                transition.hasExitTime = false;
                transition.duration = StateBlendSeconds;
                transition.canTransitionToSelf = false;
                if (pose == MokiPose.Idle)
                {
                    machine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadOrCreateClip(MokiPose pose)
        {
            string path = $"{GeneratedFolder}/Moki_{pose}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            bool loop = FillClip(clip, pose);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>자세별 곡선. 반환값은 반복 여부. 주기는 클립 길이에 정수 번 들어가게 해 반복 이음매가 없다.</summary>
        private static bool FillClip(AnimationClip clip, MokiPose pose)
        {
            switch (pose)
            {
                case MokiPose.Idle:
                    // 호버링: 빠른 날갯짓 + 천천히 위아래.
                    Flap(clip, 1f, 8f, 30f, Vector3.zero);
                    Track(clip, RigName, Position, 1f, t => new Vector3(0f, 0.04f * Wave(t, 1f), 0f));
                    return true;
                case MokiPose.Move:
                    Flap(clip, 0.5f, 12f, 25f, Vector3.zero);
                    Track(clip, RigName, Position, 0.5f, t => new Vector3(0f, 0.02f * Wave(t, 2f), 0f));
                    return true;
                case MokiPose.Dash:
                    // 날개를 뒤로 젖히고 몸을 앞뒤로 늘인다.
                    Flap(clip, 0.25f, 16f, 10f, new Vector3(0f, 55f, 0f));
                    Track(clip, RigName, Scale, 0.25f, _ => new Vector3(0.9f, 0.9f, 1.25f));
                    return true;
                case MokiPose.Attach:
                    // 날개를 접고 숨 쉬듯 부풀었다 줄어든다. 주둥이는 피부 쪽으로.
                    FoldWings(clip, 2f);
                    Track(clip, RigName, Scale, 2f, t => new Vector3(1f, 1f + 0.03f * Wave(t, 0.5f), 1f));
                    Track(clip, ProboscisPath, Rotation, 2f, _ => new Vector3(35f, 0f, 0f));
                    return true;
                case MokiPose.Suck:
                    // 주둥이를 꽂고 펌프질, 몸이 박자에 맞춰 부푼다.
                    FoldWings(clip, 0.6f);
                    Track(clip, ProboscisPath, Rotation, 0.6f, t => new Vector3(45f + 8f * Wave(t, 1f / 0.6f), 0f, 0f));
                    Track(clip, RigName, Scale, 0.6f, t =>
                    {
                        float swell = 0.5f + 0.5f * Wave(t, 1f / 0.6f);
                        return new Vector3(1f + 0.05f * swell, 1f + 0.06f * swell, 1f + 0.05f * swell);
                    });
                    return true;
                case MokiPose.Trapped:
                    // 버둥거림: 좌우로 흔들고 날개를 떤다.
                    Flap(clip, 0.4f, 20f, 15f, Vector3.zero);
                    Track(clip, RigName, Rotation, 0.4f, t => new Vector3(0f, 0f, 18f * Wave(t, 5f)));
                    return true;
                case MokiPose.Death:
                    // 뒤집혀 떨어지고 날개가 처진다 (반복하지 않음).
                    Track(clip, RigName, Rotation, 1f, t => new Vector3(0f, 0f, 160f * EaseOut(t)));
                    Track(clip, RigName, Position, 1f, t => new Vector3(0f, -0.25f * EaseOut(t), 0f));
                    Track(clip, WingLeftPath, Rotation, 1f, t => Vector3.Lerp(WingLeftRest, new Vector3(0f, 0f, -30f), EaseOut(t)));
                    Track(clip, WingRightPath, Rotation, 1f, t => Vector3.Lerp(WingRightRest, new Vector3(0f, 0f, 30f), EaseOut(t)));
                    Track(clip, ProboscisPath, Rotation, 1f, t => Vector3.Lerp(ProboscisRest, Vector3.zero, EaseOut(t)));
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pose), pose, null);
            }
        }

        /// <summary>좌우 대칭 날갯짓. sweepBack은 왼쪽 날개 기준 (오른쪽은 y·z 부호 반전).</summary>
        private static void Flap(AnimationClip clip, float duration, float frequency, float amplitude, Vector3 sweepBack)
        {
            Track(clip, WingLeftPath, Rotation, duration, t => new Vector3(0f, -sweepBack.y, WingLeftRest.z + amplitude * Wave(t, frequency)));
            Track(clip, WingRightPath, Rotation, duration, t => new Vector3(0f, sweepBack.y, WingRightRest.z - amplitude * Wave(t, frequency)));
        }

        private static void FoldWings(AnimationClip clip, float duration)
        {
            Track(clip, WingLeftPath, Rotation, duration, _ => new Vector3(0f, -70f, 5f));
            Track(clip, WingRightPath, Rotation, duration, _ => new Vector3(0f, 70f, -5f));
        }

        private const string Position = "m_LocalPosition";
        private const string Rotation = "localEulerAnglesRaw";
        private const string Scale = "m_LocalScale";

        /// <summary>함수를 일정 간격으로 샘플해 x·y·z 곡선 3개로 넣는다.</summary>
        private static void Track(AnimationClip clip, string path, string property, float duration, Func<float, Vector3> value)
        {
            int count = Mathf.CeilToInt(duration * SamplesPerSecond);
            var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            for (int i = 0; i <= count; i++)
            {
                float time = duration * i / count;
                Vector3 sample = value(time);
                for (int axis = 0; axis < 3; axis++)
                {
                    curves[axis].AddKey(time, sample[axis]);
                }
            }

            string[] suffixes = { ".x", ".y", ".z" };
            for (int axis = 0; axis < 3; axis++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property + suffixes[axis]);
                AnimationUtility.SetEditorCurve(clip, binding, curves[axis]);
            }
        }

        private static float Wave(float time, float frequency) => Mathf.Sin(2f * Mathf.PI * frequency * time);

        private static float EaseOut(float t) => 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));

        private static void Part(PrimitiveType type, string name, Transform parent, Material material, Vector3 position, Vector3 euler, Vector3 scale)
        {
            Transform part = Primitives.Create(type, name, parent, material).transform;
            part.localPosition = position;
            part.localEulerAngles = euler;
            part.localScale = scale;
        }

        private static Transform Pivot(string name, Transform parent, Vector3 position, Vector3 euler)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            pivot.localEulerAngles = euler;
            return pivot;
        }

        private sealed class Materials
        {
            public Material Skin;
            public Material Dress;
            public Material Hair;
            public Material Ribbon;
            public Material Eye;
            public Material Wand;
            public Material Wing;
        }

        // MaterialPropertyBlock은 씬에 저장되지 않으므로 부위 색은 머티리얼 에셋으로 둔다.
        private static Materials LoadOrCreateMaterials()
        {
            return new Materials
            {
                Skin = Toon("Moki_Skin", SkinColor, OutlineWidth),
                Dress = Toon("Moki_Dress", DressColor, OutlineWidth),
                Hair = Toon("Moki_Hair", HairColor, OutlineWidth),
                Ribbon = Toon("Moki_Ribbon", RibbonColor, OutlineWidth),
                Eye = Toon("Moki_Eye", EyeColor, 0f),
                Wand = Toon("Moki_Wand", WandColor, OutlineWidth),
                Wing = SandboxSceneBuilder.LoadOrCreateTransparentMaterial($"{MaterialFolder}/Moki_Wing.mat", WingColor, doubleSided: true),
            };
        }

        private static Material Toon(string name, Color color, float outlineWidth)
        {
            Material material = SandboxSceneBuilder.LoadOrCreateShaderMaterial($"{MaterialFolder}/{name}.mat", ToonMaterials.OpaqueShader, color);
            material.SetFloat("_OutlineWidth", outlineWidth);
            material.SetFloat("_SelfIllumination", SelfIllumination);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
