namespace Moqui.Unity
{
    /// <summary>Core(System.Numerics)와 Unity 수학 타입 변환. 좌표계가 같아 성분을 그대로 옮긴다 (spec/00).</summary>
    public static class VectorConversions
    {
        public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 value)
        {
            return new UnityEngine.Vector3(value.X, value.Y, value.Z);
        }

        public static System.Numerics.Vector3 ToCore(this UnityEngine.Vector3 value)
        {
            return new System.Numerics.Vector3(value.x, value.y, value.z);
        }

        public static UnityEngine.Quaternion ToUnity(this System.Numerics.Quaternion value)
        {
            return new UnityEngine.Quaternion(value.X, value.Y, value.Z, value.W);
        }
    }
}
