using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    public enum BodyPartKind
    {
        Head,
        Neck,
        Torso,
        Pelvis,
        UpperArm,
        Forearm,
        Hand,
        Thigh,
        Calf,
        Foot,
    }

    /// <summary>판정용 몸 캡슐 하나 (tech/architecture.md §4.6). 좌표는 인간 루트(위치, 정면 yaw) 기준 로컬이다.</summary>
    public sealed class BodyPartDefinition
    {
        public BodyPartDefinition(string id, BodyPartKind kind, Vector3 localA, Vector3 localB, float radius, bool isSkin)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Kind = kind;
            LocalA = localA;
            LocalB = localB;
            Radius = radius;
            IsSkin = isSkin;
        }

        public string Id { get; }

        public BodyPartKind Kind { get; }

        public Vector3 LocalA { get; }

        public Vector3 LocalB { get; }

        public float Radius { get; }

        /// <summary>노출된 피부라 흡혈할 수 있는 부위인가 (SkinSite).</summary>
        public bool IsSkin { get; }
    }

    /// <summary>
    /// 레벨 데이터의 인간 (tech/architecture.md §5 level JSON의 human). 자세와 몸 캡슐, 시선 패턴을 담는다.
    /// IdleLookYaws: 평온 상태에서 반복하는 머리 yaw 목표(몸 정면 기준, 도).
    /// </summary>
    public sealed class HumanDefinition
    {
        public HumanDefinition(
            string id,
            Vector3 position,
            float facingYaw,
            IReadOnlyList<BodyPartDefinition> parts,
            string headPartId,
            IReadOnlyList<Vector3> shoulderLocals,
            IReadOnlyList<float> idleLookYaws)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Position = position;
            FacingYaw = facingYaw;
            Parts = parts ?? throw new ArgumentNullException(nameof(parts));
            HeadPartId = headPartId;
            ShoulderLocals = shoulderLocals ?? throw new ArgumentNullException(nameof(shoulderLocals));
            IdleLookYaws = idleLookYaws != null && idleLookYaws.Count > 0 ? idleLookYaws : new[] { 0f };

            var head = parts.FirstOrDefault(part => part.Id == headPartId);
            if (head == null || head.Kind != BodyPartKind.Head)
            {
                throw new ArgumentException($"Human '{id}': head part '{headPartId}' is missing or not a head.", nameof(headPartId));
            }

            if (shoulderLocals.Count == 0)
            {
                throw new ArgumentException($"Human '{id}' needs at least one shoulder.", nameof(shoulderLocals));
            }
        }

        public string Id { get; }

        public Vector3 Position { get; }

        public float FacingYaw { get; }

        public IReadOnlyList<BodyPartDefinition> Parts { get; }

        public string HeadPartId { get; }

        public IReadOnlyList<Vector3> ShoulderLocals { get; }

        public IReadOnlyList<float> IdleLookYaws { get; }
    }
}
