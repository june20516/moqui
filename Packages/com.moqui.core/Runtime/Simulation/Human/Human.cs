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

        public Human(HumanDefinition definition, CollisionWorld world)
        {
            Definition = definition;
            BodyRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, definition.FacingYaw * DegreesToRadians);
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
        }

        public HumanDefinition Definition { get; }

        public string Id => Definition.Id;

        public Quaternion BodyRotation { get; }

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

        public Vector3 HeadForward => Vector3.Transform(LocalDirection(HeadYaw, HeadPitch), BodyRotation);

        /// <summary>머리 회전을 반영한 귀 2개 위치 (머리 캡슐 양옆, spec/02 §2).</summary>
        public Vector3 LeftEar => HeadCenter - (HeadRight * HeadShape.Radius);

        public Vector3 RightEar => HeadCenter + (HeadRight * HeadShape.Radius);

        public Vector3 HeadRight => Vector3.Transform(LocalDirection(HeadYaw + 90f, 0f), BodyRotation);

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
        public void ApplyPose(HumanActionDefinition action, float elapsedSeconds)
        {
            float profile = action?.Profile(elapsedSeconds) ?? 0f;
            foreach (var part in Definition.Parts)
            {
                Vector3 a = part.LocalA;
                Vector3 b = part.LocalB;
                if (action != null)
                {
                    foreach (var motion in action.Motions)
                    {
                        if (motion.PartId == part.Id)
                        {
                            a += motion.OffsetA * profile;
                            b += motion.OffsetB * profile;
                        }
                    }
                }

                _shapes[part.Id].SetSegment(ToWorld(a), ToWorld(b));
            }
        }

        public float DistanceToNearestEar(Vector3 point)
        {
            return MathF.Min(Vector3.Distance(point, LeftEar), Vector3.Distance(point, RightEar));
        }

        public float DistanceToNearestShoulder(Vector3 point)
        {
            float best = float.MaxValue;
            foreach (var local in Definition.ShoulderLocals)
            {
                best = MathF.Min(best, Vector3.Distance(point, ToWorld(local)));
            }

            return best;
        }

        public Vector3 ToWorld(Vector3 local)
        {
            return Definition.Position + Vector3.Transform(local, BodyRotation);
        }

        /// <summary>world 점을 바라보는 머리 상대각(도). 몸 정면 기준.</summary>
        public void AnglesToward(Vector3 worldPoint, out float yaw, out float pitch)
        {
            Vector3 local = Vector3.Transform(worldPoint - HeadCenter, Quaternion.Conjugate(BodyRotation));
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
