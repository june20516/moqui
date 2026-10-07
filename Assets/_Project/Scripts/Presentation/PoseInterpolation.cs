using System.Runtime.CompilerServices;
using Moqui.Core.Collision;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 움직이는 형상(인간 몸 캡슐)을 틱 사이에서 보간해 그린다.
    /// 모키·카메라는 직전·현재 틱 위치를 보간한 값을 쓰므로, 인간 몸도 같은 비율로 그려야 붙어 있는 모키·체온 윤곽과 어긋나지 않는다
    /// (어긋나면 인간이 움직일 때 화면이 떨리고 깨져 보인다, 플레이 피드백 2026-10-07).
    /// 형상마다 마지막 두 틱의 자세를 기억하므로, 같은 형상을 그리는 여러 뷰(몸·체온 윤곽)가 같은 자세를 쓴다.
    /// </summary>
    public static class PoseInterpolation
    {
        private sealed class State
        {
            public int Tick = int.MinValue;
            public Vector3 PreviousPosition;
            public Quaternion PreviousRotation = Quaternion.identity;
            public Vector3 PreviousScale;
            public Vector3 CurrentPosition;
            public Quaternion CurrentRotation = Quaternion.identity;
            public Vector3 CurrentScale;
        }

        private static readonly ConditionalWeakTable<CollisionShape, State> States = new ConditionalWeakTable<CollisionShape, State>();

        /// <summary>틱 tick의 형상 자세를 기억하고, 직전 틱 자세와 alpha(0 = 직전, 1 = 현재)로 섞어 visual에 놓는다.</summary>
        public static void Apply(CollisionShape shape, Transform visual, int tick, float alpha)
        {
            var state = States.GetOrCreateValue(shape);
            if (state.Tick != tick)
            {
                WorldView.ApplyPose(shape, visual);
                // 틱이 앞으로 갔으면 직전 자세를 이어받고, 처음이거나 되돌아갔으면(다시 시작) 보간 없이 놓는다.
                bool continues = state.Tick != int.MinValue && tick > state.Tick;
                state.PreviousPosition = continues ? state.CurrentPosition : visual.position;
                state.PreviousRotation = continues ? state.CurrentRotation : visual.rotation;
                state.PreviousScale = continues ? state.CurrentScale : visual.localScale;
                state.CurrentPosition = visual.position;
                state.CurrentRotation = visual.rotation;
                state.CurrentScale = visual.localScale;
                state.Tick = tick;
            }

            float t = Mathf.Clamp01(alpha);
            visual.SetPositionAndRotation(Vector3.Lerp(state.PreviousPosition, state.CurrentPosition, t), Quaternion.Slerp(state.PreviousRotation, state.CurrentRotation, t));
            visual.localScale = Vector3.Lerp(state.PreviousScale, state.CurrentScale, t);
        }
    }
}
