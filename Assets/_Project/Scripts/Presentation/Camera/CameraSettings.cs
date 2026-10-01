using Moqui.Core.Data;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>카메라 수치 (spec/00, spec/tuning.md world / camera).</summary>
    public sealed class CameraSettings
    {
        public CameraSettings(Tuning tuning)
        {
            Distance = tuning.GetFloat("camera.distance");
            HeightOffset = tuning.GetFloat("camera.heightOffset");
            Fov = tuning.GetFloat("camera.fov");
            NearClip = tuning.GetFloat("camera.nearClip");
            CollisionRadius = tuning.GetFloat("camera.collisionRadius");
            DefaultView = ParseView(tuning.GetString("camera.defaultView"));
            SwitchTime = tuning.GetFloat("camera.switchTime");
            FirstPersonEyeOffset = tuning.GetVector3("camera.fp.eyeOffset").ToUnity();
            FirstPersonFov = tuning.GetFloat("camera.fp.fov");
            FirstPersonNearClip = tuning.GetFloat("camera.fp.nearClip");
            FirstPersonDashFovKick = tuning.GetFloat("camera.fp.dashFovKick");
            FirstPersonAttachedLookLimit = tuning.GetFloat("camera.fp.attachedLookLimit");
        }

        public float Distance { get; }

        public float HeightOffset { get; }

        public float Fov { get; }

        public float NearClip { get; }

        public float CollisionRadius { get; }

        public CameraViewMode DefaultView { get; }

        public float SwitchTime { get; }

        public Vector3 FirstPersonEyeOffset { get; }

        public float FirstPersonFov { get; }

        public float FirstPersonNearClip { get; }

        public float FirstPersonDashFovKick { get; }

        public float FirstPersonAttachedLookLimit { get; }

        public static CameraViewMode ParseView(string value)
        {
            return System.Enum.TryParse(value, out CameraViewMode view) ? view : throw new DataFormatException($"Unknown camera view '{value}'.");
        }
    }
}
