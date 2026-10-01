using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    public enum AwarenessState
    {
        Safe,
        Suspicious,
        Frenzy,
    }

    /// <summary>
    /// 인간 엔티티 (spec/02). 몸 캡슐을 충돌 월드에 등록하고, 감지·어그로·공격 상태를 담는다.
    /// 머리 각도(HeadYaw, HeadPitch)는 몸 정면 기준 상대각(도)이다.
    /// </summary>
    public sealed class Human
    {
        public const ShapeFlags BodyFlags = ShapeFlags.Obstacle | ShapeFlags.Body | ShapeFlags.Attachable;

        private const float DegreesToRadians = MathF.PI / 180f;

        private readonly Dictionary<string, CollisionShape> _shapes = new Dictionary<string, CollisionShape>();
        private readonly Dictionary<CollisionShape, BodyPartDefinition> _parts = new Dictionary<CollisionShape, BodyPartDefinition>();
        private readonly Dictionary<CollisionShape, SkinSiteState> _sites = new Dictionary<CollisionShape, SkinSiteState>();

        public Human(HumanDefinition definition, CollisionWorld world, BodySettings body)
        {
            Definition = definition;
            Body = body;
            RootPosition = definition.Position;
            // 먼저 몸 로컬 X축으로 기울이고(정면 +Z가 위로 들림), 그다음 yaw로 돌린다.
            var pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -definition.FacingPitch * DegreesToRadians);
            var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, definition.FacingYaw * DegreesToRadians);
            BodyRotation = Quaternion.Concatenate(pitch, yaw);
            HeadPitch = definition.RestPitch;
            foreach (var part in definition.Parts)
            {
                ShapeFlags flags = part.IsSkin ? BodyFlags | ShapeFlags.SkinSite : BodyFlags;
                var shape = CollisionShape.Capsule(ShapeId(part.Id), ToWorld(part.LocalA), ToWorld(part.LocalB), part.Radius, flags);
                world.Add(shape);
                _shapes.Add(part.Id, shape);
                _parts.Add(shape, part);
                if (part.SiteType.HasValue)
                {
                    _sites.Add(shape, new SkinSiteState(part.Id, part.SiteType.Value, shape));
                }
            }

            HeadShape = _shapes[definition.HeadPartId];
            Rig = BodyRig.Build(definition);
            Pose = new BodyPose(Rig.Arms.Count);
            _palms = new Vector3[Rig.Arms.Count];
            _shoulders = new Vector3[Rig.Arms.Count];
            UpdatePose();
        }

        private readonly Vector3[] _palms;
        private readonly Vector3[] _shoulders;
        private HumanActionDefinition _poseAction;
        private float _poseActionElapsed;

        public BodySettings Body { get; }

        /// <summary>데이터에서 읽은 뼈대 (골반·팔 사슬).</summary>
        public BodyRig Rig { get; }

        /// <summary>현재 자세와 팔별 손 목표. 바꾼 뒤 UpdatePose를 불러야 형상에 반영된다.</summary>
        public BodyPose Pose { get; }

        /// <summary>몸 루트 위치 (월드). 지금은 자리 이동이 없어 시작 위치 그대로다 (걷기는 이후 확장, D-053).</summary>
        public Vector3 RootPosition { get; set; }

        /// <summary>상체 회전 (몸 로컬): 비틀기 후 기울기.</summary>
        public Quaternion UpperBodyRotation => UpperRotation(Pose.Posture);

        /// <summary>상체의 월드 회전 (머리 방향·어깨 가동 범위의 기준).</summary>
        public Quaternion UpperBodyWorldRotation => Quaternion.Concatenate(UpperBodyRotation, BodyRotation);

        /// <summary>가슴 중심 (두 어깨의 가운데, 자세 반영). 팔이 없으면 머리 중심.</summary>
        public Vector3 ChestCenter
        {
            get
            {
                if (_shoulders.Length == 0)
                {
                    return HeadCenter;
                }

                Vector3 sum = Vector3.Zero;
                foreach (Vector3 shoulder in _shoulders)
                {
                    sum += shoulder;
                }

                return sum / _shoulders.Length;
            }
        }

        /// <summary>팔별 현재 손바닥 중심 (월드).</summary>
        public Vector3 Palm(int arm) => _palms[arm];

        /// <summary>팔별 현재 어깨 (월드, 자세 반영).</summary>
        public Vector3 Shoulder(int arm) => _shoulders[arm];

        public static Quaternion UpperRotation(PostureState posture)
        {
            var twist = Quaternion.CreateFromAxisAngle(Vector3.UnitY, posture.Twist * DegreesToRadians);
            Vector3 axis = Vector3.Cross(Vector3.UnitY, posture.LeanDirection);
            if (posture.LeanAngle == 0f || axis.LengthSquared() < 1e-8f)
            {
                return twist;
            }

            var lean = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), posture.LeanAngle * DegreesToRadians);
            return Quaternion.Concatenate(twist, lean);
        }

        /// <summary>자세가 posture일 때 상체의 몸 로컬 점 하나가 가는 곳 (일어섬 이동 포함).</summary>
        public Vector3 PosedUpperLocal(Vector3 local, PostureState posture)
        {
            return Rig.HipLocal + RiseOffset(posture.Rise) + Vector3.Transform(local - Rig.HipLocal, UpperRotation(posture));
        }

        /// <summary>자세가 posture일 때 팔의 어깨 위치 (월드). 공격 계획기가 닿는지 미리 계산할 때 쓴다.</summary>
        public Vector3 ShoulderFor(int arm, PostureState posture) => ToWorld(PosedUpperLocal(Rig.Arms[arm].ShoulderLocal, posture));

        /// <summary>팔 길이 = 위팔 + 아래팔 + 손바닥까지.</summary>
        public float ArmReach(int arm) => Rig.Arms[arm].UpperLength + Rig.Arms[arm].ForearmLength + Body.HandReachExtra;

        private Vector3 RiseOffset(float rise) => ((Vector3.UnitY * Body.RiseLift) + (Vector3.UnitZ * Body.RiseForward)) * rise;

        public HumanDefinition Definition { get; }

        public string Id => Definition.Id;

        public Quaternion BodyRotation { get; set; }

        public CollisionShape HeadShape { get; }

        public IReadOnlyDictionary<string, CollisionShape> Shapes => _shapes;

        public IReadOnlyCollection<SkinSiteState> SkinSites => _sites.Values;

        /// <summary>인간의 물린 자국 수 n (spec/04 §4).</summary>
        public int BiteMarkCount { get; set; }

        public bool Owns(CollisionShape shape)
        {
            return _parts.ContainsKey(shape);
        }

        public bool TryGetSite(CollisionShape shape, out SkinSiteState site)
        {
            return _sites.TryGetValue(shape, out site);
        }

        public bool TryGetPart(CollisionShape shape, out BodyPartDefinition part)
        {
            return _parts.TryGetValue(shape, out part);
        }

        public float HeadYaw { get; set; }

        public float HeadPitch { get; set; }

        public Vector3 HeadCenter => HeadShape.Center;

        public Vector3 HeadForward => Vector3.Transform(LocalDirection(HeadYaw, HeadPitch), UpperBodyWorldRotation);

        /// <summary>머리 회전을 반영한 귀 2개 위치 (머리 캡슐 양옆, spec/02 §2).</summary>
        public Vector3 LeftEar => HeadCenter - (HeadRight * HeadShape.Radius);

        public Vector3 RightEar => HeadCenter + (HeadRight * HeadShape.Radius);

        public Vector3 HeadRight => Vector3.Transform(LocalDirection(HeadYaw + 90f, 0f), UpperBodyWorldRotation);

        // ---- 어그로 ----
        public float Awareness { get; set; }

        public AwarenessState State { get; set; } = AwarenessState.Safe;

        public int StateEnteredTick { get; set; }

        public bool HasStimulus { get; set; }

        public Vector3 LastStimulusPosition { get; set; }

        public int LastStimulusTick { get; set; } = Player.NeverTick;

        /// <summary>이번 틱 "보임" 여부 (D-029). 스냅샷의 PlayerVisibleToHuman.</summary>
        public bool PlayerVisible { get; set; }

        public bool HasSeenPlayer { get; set; }

        public Vector3 LastSeenPosition { get; set; }

        // ---- 시야 캐시 (vision.losCheckInterval마다 갱신) ----
        public int LineOfSightCheckedTick { get; set; } = Player.NeverTick;

        public bool LineOfSightCached { get; set; }

        // ---- 머리 행동 ----
        public int IdleLookIndex { get; set; }

        public int SearchSign { get; set; } = 1;

        // ---- 광분 ----
        public int FrenzyEnteredTick { get; set; }

        public int UnseenTicks { get; set; }

        /// <summary>광분 최소 유지 시간 중 남은 초. 광분이 아니면 0 (HUD, spec/08).</summary>
        public float FrenzyMinRemaining { get; set; }

        /// <summary>진정 진행 0~1 = 연속 미발견 시간 / frenzy.calmTime. 광분이 아니면 0 (HUD, spec/08).</summary>
        public float CalmProgress { get; set; }

        /// <summary>Yellow Zone 안이지만 장애물에 시야가 막힘 (HUD "가려짐").</summary>
        public bool PlayerOccluded { get; set; }

        public int FrenzyCount { get; set; }

        public int NextBlindSwatTick { get; set; }

        /// <summary>다음 모기약 분사가 가능한 틱 (spray.cooldown, spec/06).</summary>
        public int NextSprayTick { get; set; }

        /// <summary>취한 타겟의 다음 무작위 휘두르기 틱 (spec/06).</summary>
        public int NextDrunkSwatTick { get; set; } = Player.NeverTick;

        public bool IsDrunk => Definition.Traits.Has(HumanModifier.Drunk);

        public HumanAttack Attack { get; } = new HumanAttack();

        // ---- 졸음 (spec/06) ----

        public DozeState Doze { get; set; } = DozeState.Awake;

        public int DozeStateEndTick { get; set; }

        public bool DozeTelegraphSent { get; set; }

        /// <summary>경계가 의심 이탈선 아래로 내려간 틱 (다시 졸기 판정). 아니면 NeverTick.</summary>
        public int CalmSinceTick { get; set; } = Player.NeverTick;

        public bool IsAsleep => Doze == DozeState.Sleeping;

        // ---- 둘러보기 (spec/07 Stage 2) ----

        public int NextGlanceTick { get; set; }

        public int GlanceEndTick { get; set; } = Player.NeverTick;

        public float GlanceYaw { get; set; }

        // ---- 호흡 (spec/11 §2) ----

        /// <summary>호흡 주기 안의 위치 (0~1).</summary>
        public float BreathPhase { get; set; }

        public bool IsExhaling { get; set; }

        /// <summary>날숨이 나오는 코·입 위치.</summary>
        public Vector3 ExhalePosition { get; set; }

        /// <summary>CO₂ 세기 (human.co2Strength × 취함 배율). 날숨이 아니면 0.</summary>
        public float ExhaleStrength { get; set; }

        // ---- 무작위 동작 (spec/02 §6) ----

        /// <summary>진행 중인 동작. 없으면 null.</summary>
        public HumanActionDefinition CurrentAction { get; set; }

        public int ActionStartTick { get; set; }

        public int NextActionTick { get; set; }

        /// <summary>
        /// 몸 캡슐을 정의 자세 + 현재 동작의 이동량 × 곡선으로 다시 놓는다. 절차적 포즈 (tech/architecture.md §4.6).
        /// </summary>
        /// <summary>무작위 동작(spec/02 §6)을 정하고 포즈를 다시 계산한다.</summary>
        public void ApplyPose(HumanActionDefinition action, float elapsedSeconds)
        {
            _poseAction = action;
            _poseActionElapsed = elapsedSeconds;
            UpdatePose();
        }

        /// <summary>
        /// 몸 형상을 다시 놓는다: 휴식 자세 + 무작위 동작 → 일어섬(골반 상승, 무릎 고정) → 상체 비틀기·기울기(골반 피벗) → 손 목표가 있는 팔은 2관절 IK.
        /// </summary>
        public void UpdatePose()
        {
            float profile = _poseAction?.Profile(_poseActionElapsed) ?? 0f;
            var posture = Pose.Posture;
            Quaternion upper = UpperRotation(posture);
            Vector3 rise = RiseOffset(posture.Rise);
            foreach (var part in Definition.Parts)
            {
                Vector3 a = part.LocalA;
                Vector3 b = part.LocalB;
                if (_poseAction != null)
                {
                    foreach (var motion in _poseAction.Motions)
                    {
                        if (motion.PartId == part.Id)
                        {
                            a += motion.OffsetA * profile;
                            b += motion.OffsetB * profile;
                        }
                    }
                }

                if (Rig.UpperBodyParts.Contains(part.Id))
                {
                    a = Rig.HipLocal + rise + Vector3.Transform(a - Rig.HipLocal, upper);
                    b = Rig.HipLocal + rise + Vector3.Transform(b - Rig.HipLocal, upper);
                }
                else if (posture.Rise > 0f && Rig.Thighs.Contains(part.Id))
                {
                    // 무릎(b)은 두고 골반 쪽(a)만 올린다. 허벅지 길이는 유지한다.
                    float length = Vector3.Distance(a, b);
                    Vector3 raised = a + rise - b;
                    a = b + (raised.LengthSquared() > 1e-6f ? Vector3.Normalize(raised) * length : a - b);
                }

                _shapes[part.Id].SetSegment(ToWorld(a), ToWorld(b));
            }

            foreach (var arm in Rig.Arms)
            {
                Vector3 shoulder = ToWorld(PosedUpperLocal(arm.ShoulderLocal, posture));
                _shoulders[arm.Index] = shoulder;
                Vector3? target = Pose.HandTargets[arm.Index];
                if (target.HasValue)
                {
                    // 팔꿈치는 바깥·아래·뒤로 꺾인다 (상체 기준).
                    Vector3 poleLocal = new Vector3(arm.Side * 0.6f, -1f, -0.4f);
                    Vector3 pole = shoulder + Vector3.Transform(poleLocal, Quaternion.Concatenate(upper, BodyRotation));
                    BodyKinematics.SolveArm(shoulder, target.Value, arm.UpperLength, arm.ForearmLength, Body.HandReachExtra, Body.ElbowFlexMax, pole,
                        out Vector3 elbow, out Vector3 wrist, out Vector3 palm);
                    _shapes[arm.UpperArmId].SetSegment(shoulder, elbow);
                    _shapes[arm.ForearmId].SetSegment(elbow, wrist);
                    _palms[arm.Index] = palm;
                }
                else
                {
                    var forearm = _shapes[arm.ForearmId];
                    Vector3 direction = forearm.PointB - forearm.PointA;
                    _palms[arm.Index] = forearm.PointB + (direction.LengthSquared() > 1e-6f ? Vector3.Normalize(direction) * Body.HandReachExtra : Vector3.Zero);
                }
            }
        }

        public float DistanceToNearestEar(Vector3 point)
        {
            return MathF.Min(Vector3.Distance(point, LeftEar), Vector3.Distance(point, RightEar));
        }

        /// <summary>점에 가장 가까운 어깨(손 출발점)의 월드 위치.</summary>
        /// <summary>점에 가장 가까운 팔 (자세 반영 어깨 기준). 팔이 없으면 −1.</summary>
        public int NearestArm(Vector3 point)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _shoulders.Length; i++)
            {
                float distance = Vector3.Distance(point, _shoulders[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        public Vector3 NearestShoulder(Vector3 point)
        {
            int arm = NearestArm(point);
            return arm >= 0 ? _shoulders[arm] : RootPosition;
        }

        public float DistanceToNearestShoulder(Vector3 point)
        {
            int arm = NearestArm(point);
            return arm >= 0 ? Vector3.Distance(point, _shoulders[arm]) : float.MaxValue;
        }

        public Vector3 ToWorld(Vector3 local)
        {
            return RootPosition + Vector3.Transform(local, BodyRotation);
        }

        /// <summary>월드 점 → 몸 로컬.</summary>
        public Vector3 ToLocal(Vector3 world)
        {
            return Vector3.Transform(world - RootPosition, Quaternion.Conjugate(BodyRotation));
        }

        /// <summary>world 점을 바라보는 머리 상대각(도). 몸 정면 기준.</summary>
        public void AnglesToward(Vector3 worldPoint, out float yaw, out float pitch)
        {
            Vector3 local = Vector3.Transform(worldPoint - HeadCenter, Quaternion.Conjugate(UpperBodyWorldRotation));
            float horizontal = MathF.Sqrt((local.X * local.X) + (local.Z * local.Z));
            yaw = MathF.Atan2(local.X, local.Z) / DegreesToRadians;
            pitch = MathF.Atan2(local.Y, horizontal) / DegreesToRadians;
        }

        public string ShapeId(string partId)
        {
            return $"{Definition.Id}.{partId}";
        }

        private static Vector3 LocalDirection(float yawDegrees, float pitchDegrees)
        {
            float yaw = yawDegrees * DegreesToRadians;
            float pitch = pitchDegrees * DegreesToRadians;
            float cosPitch = MathF.Cos(pitch);
            return new Vector3(MathF.Sin(yaw) * cosPitch, MathF.Sin(pitch), MathF.Cos(yaw) * cosPitch);
        }
    }
}
