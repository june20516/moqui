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

    /// <summary>흡혈 가능한 피부 부위 유형 (spec/04 §1). tuning 키 site.<유형>.* 의 이름과 같다.</summary>
    /// <summary>인간이 손에 든 도구 (M14).</summary>
    public enum HumanTool
    {
        None,

        /// <summary>전기 모기채: 오른손(없으면 첫 팔)에 든다. 손이 채 길이만큼 길어지고 판정이 넓다.</summary>
        Swatter,
    }

    public enum SkinSiteType
    {
        Forearm,
        Calf,
        FootTop,
        Neck,
        Cheek,
    }

    /// <summary>판정용 몸 캡슐 하나 (tech/architecture.md §4.6). 좌표는 인간 루트(위치, 정면 yaw) 기준 로컬이다.</summary>
    public sealed class BodyPartDefinition
    {
        public BodyPartDefinition(string id, BodyPartKind kind, Vector3 localA, Vector3 localB, float radius, SkinSiteType? siteType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Kind = kind;
            LocalA = localA;
            LocalB = localB;
            Radius = radius;
            SiteType = siteType;
        }

        public string Id { get; }

        public BodyPartKind Kind { get; }

        public Vector3 LocalA { get; }

        public Vector3 LocalB { get; }

        public float Radius { get; }

        /// <summary>흡혈할 수 있는 노출 피부(SkinSite)이면 그 유형, 아니면 null.</summary>
        public SkinSiteType? SiteType { get; }

        public bool IsSkin => SiteType.HasValue;
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
            IReadOnlyList<float> idleLookYaws,
            IReadOnlyList<HumanActionDefinition> actions = null,
            HumanTraits traits = null,
            float facingPitch = 0f,
            float restPitch = 0f,
            PostureLevel maxPosture = PostureLevel.Rise,
            HumanWalkDefinition walk = null,
            HumanTool tool = HumanTool.None)
        {
            Tool = tool;
            Walk = walk;
            MaxPosture = maxPosture;
            FacingPitch = facingPitch;
            RestPitch = restPitch;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Position = position;
            FacingYaw = facingYaw;
            Parts = parts ?? throw new ArgumentNullException(nameof(parts));
            HeadPartId = headPartId;
            ShoulderLocals = shoulderLocals ?? throw new ArgumentNullException(nameof(shoulderLocals));
            IdleLookYaws = idleLookYaws != null && idleLookYaws.Count > 0 ? idleLookYaws : new[] { 0f };
            Actions = actions ?? Array.Empty<HumanActionDefinition>();
            Traits = traits ?? HumanTraits.None;

            var head = parts.FirstOrDefault(part => part.Id == headPartId);
            if (head == null || head.Kind != BodyPartKind.Head)
            {
                throw new ArgumentException($"Human '{id}': head part '{headPartId}' is missing or not a head.", nameof(headPartId));
            }

            if (shoulderLocals.Count == 0)
            {
                throw new ArgumentException($"Human '{id}' needs at least one shoulder.", nameof(shoulderLocals));
            }

            foreach (var action in Actions)
            {
                foreach (var motion in action.Motions)
                {
                    if (!parts.Any(part => part.Id == motion.PartId))
                    {
                        throw new ArgumentException($"Human '{id}': action '{action.Name}' moves unknown part '{motion.PartId}'.", nameof(actions));
                    }
                }
            }
        }

        public string Id { get; }

        /// <summary>손에 든 도구 (spec/02 §7 전기 모기채, M14).</summary>
        public HumanTool Tool { get; }

        /// <summary>걷기 경로 (없으면 제자리, spec/02 §9).</summary>
        public HumanWalkDefinition Walk { get; }

        public Vector3 Position { get; }

        public float FacingYaw { get; }

        /// <summary>몸 전체의 앞뒤 기울기(도). 90이면 선 자세로 작성한 몸이 등을 대고 누워 정면이 위(+Y)를 본다 (Stage 3, D-047).</summary>
        public float FacingPitch { get; }

        /// <summary>평소(경계하지 않을 때) 머리 피치(도, − 아래). 고개를 숙이고 휴대폰을 보는 자세 (Stage 4, D-047).</summary>
        public float RestPitch { get; }

        public IReadOnlyList<BodyPartDefinition> Parts { get; }

        public string HeadPartId { get; }

        public IReadOnlyList<Vector3> ShoulderLocals { get; }

        public IReadOnlyList<float> IdleLookYaws { get; }

        /// <summary>무작위 동작 목록 (spec/02 §6). 없으면 움직이지 않는다.</summary>
        public IReadOnlyList<HumanActionDefinition> Actions { get; }

        /// <summary>수정자(졸음·취함), 스프레이 사용 여부, 둘러보기 (spec/06, spec/07).</summary>
        public HumanTraits Traits { get; }

        /// <summary>공격하려고 쓸 수 있는 가장 큰 몸동작 (spec/02, D-053). 누운 사람은 일어서지 않게 낮춘다.</summary>
        public PostureLevel MaxPosture { get; }
    }
}
