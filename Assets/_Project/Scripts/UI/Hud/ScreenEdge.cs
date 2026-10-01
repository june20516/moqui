using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>화면 가장자리 표시 하나 (방향 화살표, 경고). 위치는 뷰포트 좌표(0~1).</summary>
    public readonly struct EdgeMarker
    {
        public static readonly EdgeMarker Hidden = default;

        public EdgeMarker(Vector2 viewport, float angleDegrees, bool onScreen)
        {
            Visible = true;
            Viewport = viewport;
            AngleDegrees = angleDegrees;
            OnScreen = onScreen;
        }

        public bool Visible { get; }

        public Vector2 Viewport { get; }

        /// <summary>화면 중심에서 표시 쪽을 가리키는 각도 (0 = 오른쪽, 90 = 위).</summary>
        public float AngleDegrees { get; }

        /// <summary>대상이 화면 안에 있어 가장자리로 밀지 않은 표시인가.</summary>
        public bool OnScreen { get; }
    }

    /// <summary>월드 점을 화면 안/밖으로 판정하고, 밖이면 그 방향의 가장자리 점을 구한다.</summary>
    public static class ScreenEdge
    {
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static bool IsOnScreen(Camera camera, Vector3 world)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        /// <summary>화면 밖이면 가장자리 표시, 안이면 Hidden.</summary>
        public static EdgeMarker OffscreenMarker(Camera camera, Vector3 world, Vector2 margin)
        {
            return IsOnScreen(camera, world) ? EdgeMarker.Hidden : ToEdge(camera, world, margin);
        }

        /// <summary>화면 안이면 그 위치, 밖이면 가장자리로 민 위치의 표시.</summary>
        public static EdgeMarker Marker(Camera camera, Vector3 world, Vector2 margin)
        {
            if (!IsOnScreen(camera, world))
            {
                return ToEdge(camera, world, margin);
            }

            Vector2 viewport = camera.WorldToViewportPoint(world);
            return new EdgeMarker(viewport, AngleFromCenter(viewport - Center), true);
        }

        /// <param name="margin">가로·세로 각각 화면 끝에서 띄우는 뷰포트 비율 (상·하단 HUD 띠를 피한다).</param>
        private static EdgeMarker ToEdge(Camera camera, Vector3 world, Vector2 margin)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            Vector2 direction = new Vector2(viewport.x, viewport.y) - Center;
            if (viewport.z < 0f)
            {
                // 카메라 뒤의 점은 투영이 뒤집히므로 방향을 반대로 한다.
                direction = -direction;
            }

            if (direction.sqrMagnitude < 1e-8f)
            {
                direction = Vector2.down;
            }

            // 사각형 [margin, 1 − margin] 테두리와 중심에서 나간 방향선의 교점.
            Vector2 halfExtent = new Vector2(0.5f - margin.x, 0.5f - margin.y);
            float scale = Mathf.Min(
                Mathf.Abs(direction.x) > 1e-6f ? halfExtent.x / Mathf.Abs(direction.x) : float.MaxValue,
                Mathf.Abs(direction.y) > 1e-6f ? halfExtent.y / Mathf.Abs(direction.y) : float.MaxValue);
            return new EdgeMarker(Center + (direction * scale), AngleFromCenter(direction), false);
        }

        private static float AngleFromCenter(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
