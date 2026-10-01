using System;
using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>선풍기 하나 (spec/06). 본체 충돌 형상은 레벨 월드에 따로 있고, 여기에는 바람 원뿔의 기준만 둔다.</summary>
    public sealed class FanDefinition
    {
        public FanDefinition(string id, Vector3 position, float yaw, float pitch = 0f)
        {
            Id = id;
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
        }

        public string Id { get; }

        /// <summary>바람이 나오는 머리 중심.</summary>
        public Vector3 Position { get; }

        /// <summary>회전 중심 방향(도). 머리는 이 방향을 기준으로 ±fan.oscillationAngle을 오간다.</summary>
        public float Yaw { get; }

        /// <summary>위아래 기울기(도, + 위).</summary>
        public float Pitch { get; }
    }

    /// <summary>
    /// 레벨 기믹 구성 (spec/06): 선풍기, 모기향, 자동 분사기. 거미줄은 월드의 Hazard 형상이다.
    /// </summary>
    public sealed class GimmickSetup
    {
        public static readonly GimmickSetup None = new GimmickSetup(null, null, null);

        public GimmickSetup(IReadOnlyList<FanDefinition> fans, IReadOnlyList<Vector3> coils, IReadOnlyList<Vector3> sprayDispensers)
        {
            Fans = fans ?? Array.Empty<FanDefinition>();
            Coils = coils ?? Array.Empty<Vector3>();
            SprayDispensers = sprayDispensers ?? Array.Empty<Vector3>();
        }

        public IReadOnlyList<FanDefinition> Fans { get; }

        /// <summary>모기향 위치.</summary>
        public IReadOnlyList<Vector3> Coils { get; }

        /// <summary>자동 분사기가 연무를 만드는 위치.</summary>
        public IReadOnlyList<Vector3> SprayDispensers { get; }
    }
}
