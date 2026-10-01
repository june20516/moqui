using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>카메라에 적용할 최종 상태.</summary>
    public readonly struct CameraPose
    {
        public CameraPose(Vector3 position, Quaternion rotation, float fov, float nearClip)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
            NearClip = nearClip;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public float Fov { get; }

        public float NearClip { get; }

        public static CameraPose Lerp(CameraPose from, CameraPose to, float t)
        {
            return new CameraPose(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t),
                Mathf.Lerp(from.Fov, to.Fov, t),
                Mathf.Lerp(from.NearClip, to.NearClip, t));
        }

        public void ApplyTo(Camera camera)
        {
            camera.transform.SetPositionAndRotation(Position, Rotation);
            camera.fieldOfView = Fov;
            camera.nearClipPlane = NearClip;
        }
    }
}
