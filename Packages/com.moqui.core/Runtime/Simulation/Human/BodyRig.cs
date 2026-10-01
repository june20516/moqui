using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 레벨 데이터의 몸 캡슐(휴식 자세)에서 읽어 낸 뼈대 (D-053): 골반 피벗, 상체 부위, 팔 사슬(어깨 → 팔꿈치 → 손목), 허벅지.
    /// 포즈 계산(Human.UpdatePose)이 이 구조를 쓴다. 데이터 형식은 바꾸지 않는다.
    /// </summary>
    public sealed class BodyRig
    {
        private BodyRig(Vector3 hipLocal, IReadOnlyList<ArmRig> arms, ISet<string> upperBodyParts, ISet<string> thighs)
        {
            HipLocal = hipLocal;
            Arms = arms;
            UpperBodyParts = upperBodyParts;
            Thighs = thighs;
        }

        /// <summary>상체가 기울고 비틀릴 때의 회전 중심 (몸 로컬).</summary>
        public Vector3 HipLocal { get; }

        public IReadOnlyList<ArmRig> Arms { get; }

        /// <summary>상체 회전을 따르는 부위 (머리·목·몸통·골반·팔·손).</summary>
        public ISet<string> UpperBodyParts { get; }

        /// <summary>일어설 때 골반 쪽 끝(a)이 움직이는 허벅지.</summary>
        public ISet<string> Thighs { get; }

        public static BodyRig Build(HumanDefinition definition)
        {
            var parts = definition.Parts;
            var thighs = parts.Where(part => part.Kind == BodyPartKind.Thigh).ToList();
            var torso = parts.FirstOrDefault(part => part.Kind == BodyPartKind.Torso);
            Vector3 hip = torso == null
                ? definition.ShoulderLocals.Aggregate(Vector3.Zero, (sum, s) => sum + s) / definition.ShoulderLocals.Count
                : thighs.Count > 0
                    ? Closest(torso, Average(thighs.Select(thigh => thigh.LocalA)))
                    : (torso.LocalA.Y <= torso.LocalB.Y ? torso.LocalA : torso.LocalB);

            var upper = new HashSet<string>(parts
                .Where(part => part.Kind == BodyPartKind.Head || part.Kind == BodyPartKind.Neck || part.Kind == BodyPartKind.Torso
                    || part.Kind == BodyPartKind.Pelvis || part.Kind == BodyPartKind.UpperArm || part.Kind == BodyPartKind.Forearm
                    || part.Kind == BodyPartKind.Hand)
                .Select(part => part.Id));

            var arms = new List<ArmRig>();
            foreach (Vector3 shoulder in definition.ShoulderLocals)
            {
                var upperArm = parts.Where(part => part.Kind == BodyPartKind.UpperArm)
                    .OrderBy(part => Vector3.Distance(part.LocalA, shoulder)).FirstOrDefault();
                if (upperArm == null || arms.Any(arm => arm.UpperArmId == upperArm.Id))
                {
                    continue;
                }

                var forearm = parts.Where(part => part.Kind == BodyPartKind.Forearm)
                    .OrderBy(part => Vector3.Distance(part.LocalA, upperArm.LocalB)).FirstOrDefault();
                if (forearm == null)
                {
                    continue;
                }

                arms.Add(new ArmRig(arms.Count, upperArm.LocalA, upperArm.Id, forearm.Id,
                    Vector3.Distance(upperArm.LocalA, upperArm.LocalB), Vector3.Distance(forearm.LocalA, forearm.LocalB), Math.Sign(upperArm.LocalA.X)));
            }

            return new BodyRig(hip, arms, upper, new HashSet<string>(thighs.Select(thigh => thigh.Id)));
        }

        /// <summary>이 부위가 속한 팔 (없으면 null).</summary>
        public ArmRig ArmOwning(string partId)
        {
            return Arms.FirstOrDefault(arm => arm.UpperArmId == partId || arm.ForearmId == partId);
        }

        private static Vector3 Closest(BodyPartDefinition part, Vector3 point)
        {
            return Vector3.Distance(part.LocalA, point) <= Vector3.Distance(part.LocalB, point) ? part.LocalA : part.LocalB;
        }

        private static Vector3 Average(IEnumerable<Vector3> points)
        {
            var list = points.ToList();
            return list.Aggregate(Vector3.Zero, (sum, p) => sum + p) / list.Count;
        }
    }

    /// <summary>팔 하나: 어깨(위팔 a) → 팔꿈치(위팔 b = 아래팔 a) → 손목(아래팔 b). 손바닥 중심은 손목에서 아래팔 방향으로 손 길이만큼 더 간다.</summary>
    public sealed class ArmRig
    {
        public ArmRig(int index, Vector3 shoulderLocal, string upperArmId, string forearmId, float upperLength, float forearmLength, int side)
        {
            Index = index;
            ShoulderLocal = shoulderLocal;
            UpperArmId = upperArmId;
            ForearmId = forearmId;
            UpperLength = upperLength;
            ForearmLength = forearmLength;
            Side = side;
        }

        public int Index { get; }

        public Vector3 ShoulderLocal { get; }

        public string UpperArmId { get; }

        public string ForearmId { get; }

        public float UpperLength { get; }

        public float ForearmLength { get; }

        /// <summary>몸 로컬 X 부호 (−1 왼쪽, +1 오른쪽).</summary>
        public int Side { get; }
    }
}
