# 02. 인간 AI — 타겟이자 적

## 목적
흡혈 대상인 인간이 동시에 유일한 위협이다. 시각·청각(귀)·촉각으로 모기를 감지하고, 3단계 어그로(평온 → 의심 → 광분)에 따라 반응한다. 들키면 광분해 공격이 폭증하므로 숨는 것이 이득이어야 한다. (D-002, D-010)

## 구성 (모두 Core)
- `HumanBrain`: 어그로 상태머신 (Safe / Suspicious / Frenzy)
- 센서: `VisionSensor`, `HearingSensor`(귀 기준), `TouchSensor`(부착·가려움, spec/04)
- `AwarenessModel`: 0~100 게이지와 물린 자국 보정
- `ReactionModel`: 확률 기반 피격 이벤트 (§5)
- `ActionScheduler`: 무작위 움직임 (§6)
- `AttackController`: 공격 정의(데이터)를 실행하고 판정 구를 활성화한다.
- 판정용 몸은 Core 충돌 월드의 캡슐 묶음이다. 머리 방향, 몸 동작, 공격 타이밍은 Core가 정하고 Unity의 애니메이션은 그 상태를 따라 재생만 한다 (`tech/architecture.md` §4.6).
- 무작위 요소는 모두 레벨 시드에서 파생된 Core 난수를 쓴다.
- 인간 수정자(졸음, 취함)는 spec/06에 있다. 수정자는 이 문서의 수치에 배율을 곱하거나 감지를 끄는 방식으로만 작동한다.

## 1. 시각
- 눈 위치(머리) 기준으로 두 개의 원뿔 구역을 둔다.
  - **Yellow Zone:** `vision.yellow.halfAngle`, `vision.yellow.range`. 플레이어가 안에 있고 시야가 확보되면 경계가 초당 `rateNear`~`rateFar`(거리에 따라 선형 보간)만큼 오른다.
  - **Red Zone:** `vision.red.halfAngle`, `vision.red.range`. 시야가 확보된 상태로 진입하면 즉시 **박수 공격**을 시작하고 경계를 100으로 만든다 (광분 진입).
- 시야 확보 판정: 눈 → 플레이어 중심 광선이 `obstacle` 형상에 막히지 않아야 한다. 주기 `vision.losCheckInterval`.
- 배율: 부착 상태 `vision.attachedMul`, Shadow Zone `vision.shadowMul` (spec/03).
- 스냅샷에 `PlayerVisibleToHuman`(현재 시야 확보 여부)을 내보낸다 (HUD용).

## 2. 청각 (귀 기준)
- 머리 캡슐 양옆에 귀 위치 2개를 둔다. 소리 거리 `d`는 **가까운 귀까지의 거리**이다.
- 비행 소음: 공중에 있고 `d < noise.flightRadius`이면 경계가 초당 `noise.flightAwarenessRate × lerp(hearing.nearMul, hearing.farMul, d / 반경)`만큼 오른다. 시야와 무관하다.
- **귀 근접 구역:** `d < hearing.earZoneRadius`이면 추가로 초당 `hearing.earZoneRate`만큼 오르고, 귀 반응(§5)이 일어날 수 있다.
- 대시 소음: `NoiseEvent`의 반경 안에 귀가 있으면 즉시 `dash.noiseAwareness`만큼 오른다.
- 소리로 경계가 오르면 "마지막 자극 위치"를 소리가 난 위치로 갱신한다. 의심 상태에서 머리는 그쪽으로 돈다.

## 3. 어그로 상태
| 상태 | 진입 | 행동 |
|---|---|---|
| Safe | 경계 < `awareness.suspiciousExit` | 스테이지별 기본 행동과 무작위 움직임 (§6). 머리는 정해진 시선 패턴을 `head.idleTurnSpeed`로 반복 |
| Suspicious | 경계 ≥ `awareness.suspiciousEnter` | 머리를 마지막 자극 위치로 `head.suspiciousTurnSpeed`로 돌린다. 2초 응시 후 좌우 탐색 |
| Frenzy (광분) | 경계 ≥ `awareness.frenzyEnter` | §4 |

- 자극이 없으면 `awareness.decayDelay` 후 감소한다. 감소율은 `awareness.decayRate`이고, 플레이어가 Shadow Zone에 있으면 `awareness.shadowDecayRate`를 쓴다.
- 경계 변화에는 물린 자국 보정(spec/04 §4)이 적용된다: 증가 배율, 감소 배율, 하한값.
- 머리 회전은 `head.yawLimit`로 제한한다.

## 4. 광분 (Frenzy)
- 진입하면 `frenzy.minDuration` 동안은 반드시 유지된다. 그 뒤 플레이어를 **연속으로 `frenzy.calmTime` 동안 보지 못하면** 진정한다: 경계를 `frenzy.exitValue`로 설정하고 Suspicious로 간다. 그 전에 다시 보면 연속 시간이 초기화된다.
- 광분 중:
  - 경계는 감소하지 않는다.
  - 머리가 플레이어를 `head.frenzyTurnSpeed`로 추적한다 (보일 때).
  - 플레이어가 보이고 사람 몸으로 손이 닿으면(§7 몸 전체로 쫓기: 팔 → 기울이기 → 돌기 → 일어서기, `human.maxPosture`까지) 손바닥 공격을 `frenzy.slapInterval` 간격으로 반복한다 (예고 `frenzy.slapTelegraph` 이상, 자세 전환 시간 + `attack.minTelegraph`보다 짧지 않음).
  - 플레이어가 보이지 않아도 마지막으로 본 위치 주변 `frenzy.blindSwatRadius` 안의 무작위 지점을 `frenzy.blindSwatInterval`마다 휘두른다 (손이 닿는 지점일 때).
  - 피부에 붙은 모기에 대한 반응 확률에 `frenzy.reactionMul`을 곱한다.
  - 레벨 데이터에서 `human.canSpray`가 참이면 모기약 스프레이를 쓴다 (spec/06): 플레이어가 `spray.useRange` 안에 보이고 쿨타임 `spray.cooldown`이 끝났을 때 플레이어 방향으로 분사한다.
- 스테이지 결과에 광분 횟수를 기록한다 (spec/09 보너스).

## 5. 피격 이벤트 (확률 기반 반응)
인간의 공격은 두 종류이다. **의도적 공격**(박수, 손바닥, 맹목 휘두르기)과 **반사적 반응**(피부나 귀 근처의 감각에 반응해 그 자리를 때림)이다. 반응은 확률로 일어난다.

- **착지 반응:** SkinSite에 부착하는 순간 1회 판정한다. 확률 `reaction.landChance × site.sensitivity × 보정`.
- **부착 중 반응:** SkinSite에 붙어 있는 동안 매 틱 판정한다. 위험률(초당) `λ = site.sensitivity × (reaction.baseRate + reaction.itchRate × (itch/100)²) × 보정`이고, 틱 확률은 `1 − e^(−λ·dt)`이다. 가려움이 100이면 확률과 무관하게 즉시 반응한다.
- **귀 반응:** 귀 근접 구역 안에 있는 동안 위험률 `reaction.earRate × 보정`으로 귀 쪽을 휘두른다.
- **보정** = 광분 배율 × 물린 자국 배율(`1 + biteMark.reactionMulPerBite × 자국 수`) × 스킬 배율(spec/09) × 취함 배율(spec/06).
- 반응도 반드시 예고(`attack.selfSlap.telegraph`)가 있고 피할 수 있다. 목표는 **예고 시작 시점의 모기 위치**이다.
- 인간은 한 번에 하나의 공격만 한다. 공격 중에 발생한 반응은 버린다.

## 6. 무작위 움직임
- 레벨 데이터의 `human.actions[]`(이름, 가중치, 지속시간, 몸 캡슐 동작)에서 `human.actionInterval`마다 하나를 고른다. 어그로 상태와 흡혈 여부에 관계없이 계속된다. 광분 중에는 공격이 우선이다.
- 동작 예: 자세 고쳐 앉기, 다리 꼬기/풀기, 팔 위치 바꾸기(리모컨, 휴대폰, 맥주), 다른 부위 긁기, 몸 돌리기.
- 동작은 캡슐 판정을 움직인다 (절차적 포즈 또는 베이크 트랙, `tech/architecture.md` §4.6).
- 모기가 붙어 있는 부위가 움직이면 모기는 그 부위의 로컬 좌표를 따라간다.
- 부위의 이동 속도가 `human.dislodgeSpeed`를 넘으면 모기가 **튕겨 나간다**: 부착 해제, 움직임 방향으로 `human.dislodgePush`만큼 밀림, `human.dislodgeStun` 동안 입력 무시. 흡혈 세션은 끝난다 (spec/04). 사망은 아니다.
- 자리를 옮겨 걷는 동작은 없다 (D-011).

## 7. 공격 정의
| 공격 | 발동 | 예고 | 판정 |
|---|---|---|---|
| 박수 (clap) | Red Zone 진입 | `attack.clap.telegraph` | 얼굴 앞 25u, 반경 `attack.clap.radius` |
| 손바닥 (slap) | 광분 + 손이 닿는 곳에서 보임 | `frenzy.slapTelegraph`. 예고 시작 시점의 모기 위치에 목표 고정 | 반경 `attack.slap.radius`의 손바닥이 지나가는 경로, 타격 시간 = 손 경로 ÷ 손 최고 속도 동안 |
| 맹목 휘두르기 (blindSwat) | 광분 + 보이지 않음 | `frenzy.slapTelegraph` | 마지막 위치 주변 무작위 지점, 반경 `attack.slap.radius` |
| 반응 때리기 (reactSlap) | §5 | `attack.selfSlap.telegraph` | 예고 시작 시점의 모기 위치, 반경 `attack.selfSlap.radius` |
| 스프레이 | 광분 + `canSpray` + 손이 닿지 않음 | `spray.telegraph` (캔을 드는 동작) | 연무 구역 생성 (spec/06) |

- **전기 모기채 (M14):** 레벨 데이터 `human.tool` = `swatter`이면 오른손(없으면 첫 팔)에 채를 든다. 그 팔은 손목 앞으로 `attack.swatter.length`만큼 더 닿고(몸동작 계획·팔 IK·손 경로 모두 채 끝 기준), 그 팔로 치는 공격의 판정 반경은 `attack.swatter.radius` 이상이다. 표현: 손에 든 채(손잡이 + 납작한 머리).

- 움직이는 손바닥 판정 구(박수는 두 손)가 지나가는 경로에 플레이어 충돌 구가 닿으면 즉사한다 (스프레이 제외, D-052).
- **예고 피드백** (Unity): 손 동작, 판정 위치의 붉은 표시, 경고음 `sfx_telegraph`, 화면 밖 공격이면 화면 가장자리에 방향 표시 (spec/08).
- 공격 후 `recovery` 동안 다음 공격을 하지 않는다.
- 공격 동작 (M12): 목표에 가까운 어깨 쪽의 **실제 팔**(upperArm·forearm 캡슐)이 예고 동안 뒤로 치켜들고, 판정 동안 목표 지점으로 뻗고, 회복 동안 제자리로 돌아온다. 팔 캡슐은 Core가 움직이므로 그 팔에 붙어 있던 모기는 팔을 따라가거나 튕겨 나간다. 판정은 기존대로 예고 시작 시점에 고정한 목표 구다. 따로 튀어나오는 표현용 팔은 쓰지 않는다 (D-051).
- 사람의 신체 제약 (M12) (D-052):
  - **닿는 거리:** 손이 닿는 범위는 팔 길이(어깨 → 손끝)와 자세가 허락하는 몸 기울임으로 정한다. 그 밖의 목표는 때리지 않는다(손을 뻗는 대신 다른 행동). 현재 `attack.reach` 80u + `frenzy.reachBonus` 40u = 120u는 팔 길이(약 66u + 손)보다 길어 고친다.
  - **관절 한계:** 팔꿈치는 굽히기만 하고(과신전 없음), 어깨는 사람 가동 범위 안에서만 돈다. 손은 직선이 아니라 어깨·팔꿈치 회전으로 생기는 호를 그린다(근위 → 원위 순서: 어깨가 먼저, 팔꿈치가 펴지며, 손목이 마지막).
  - **속도:** 타격 시간은 고정값이 아니라 손이 가야 할 경로 길이 ÷ 사람 손 최고 속도로 정한다. 멀수록 늦게 도착한다. 예고(치켜듦)·회복도 사람이 낼 수 있는 시간 이상으로 둔다. 수치는 근거 조사 후 `spec/tuning.md`에 둔다.
  - **어느 손:** 내 몸의 한쪽 팔에 앉은 모기는 반대쪽 손으로 때린다. 박수는 두 손이 얼굴 앞에서 만난다. 다리(종아리)처럼 먼 부위는 몸을 숙이는 시간만큼 느리다.
  - **판정:** 예고 시작 시점에 목표를 고정하는 규칙은 유지하되, 판정은 실제로 움직이는 손(손바닥 구)이 지나가는 경로로 한다(손이 지나간 자리에 있으면 맞는다).
- 몸 전체로 쫓기 (M12) (D-053): 인간이 보이는 상태에서 팔이 늘어나거나 손이 총알처럼 날아오면 안 된다. 그래서 광분 공격은 사람 몸이 직접 움직여 닿는다. 목표에 닿기 위한 몸동작을 단계 순서로 고른다: **팔만 → 상체 기울이기 → 몸(허리·어깨) 돌리기 → 반쯤/완전히 일어서기**. 각 단계는 사람이 낼 수 있는 시간이 걸리고(자세 전환 시간), 그 사이 모기는 피할 수 있다. 일어서면 머리 위치·시야도 함께 바뀐다. 걷기(자리 이동)는 이번 범위가 아니다(D-011 유지).
  - 확장 구조: 몸은 뼈대(루트·골반·척추·어깨·팔꿈치·손목·목·머리)에 캡슐을 붙인 포즈로 표현하고, 루트(몸 위치·방향)는 상태로 둔다. 레벨 데이터 `human.maxPosture`(arm | lean | turn | rise, 기본 rise)로 허용 몸동작을 정한다. 이후 높은 난이도에서 `step`(걸음)·도구를 같은 계획기 단계로 추가한다 (plan/ideas.md).
- 예고 피드백 강조 (M12): "맞을 자리" 정보는 화면에서 가장 높은 위상이어야 한다. 정지한 반투명 구 대신 움직이는 요소로 보여 준다 — 예고 진행률만큼 차오르는 링, 좁혀 오는 테두리, 일렁임·떨림, 판정 순간 번쩍임. 손이 다가오는 방향도 읽혀야 한다.

## 9. 걷는 인간 (M14, D-058·D-059)
- 레벨 데이터의 `human.walk`(경로점 목록 `route` [[x, z], ...], 경로점에서 멈추는 시간 `pause` {min, max}, 반복 `loop`)가 있는 인간은 **서 있는 몸**으로 경로를 따라 걷는다. 몸 루트(골반)는 경로점 높이를 유지하고 수평으로만 움직인다. 이런 인간은 `maxPosture`를 `turn` 이하로 둔다(이미 서 있음).
- 평온: 다음 경로점 쪽으로 몸을 `human.walkTurnSpeed`로 돌리고, 정면과의 각이 45° 안이면 `human.walkSpeed`로 걷는다. 경로점에 닿으면 `pause` 동안 멈췄다가 다음 점으로 간다(끝이면 처음으로). 멈춤 시간은 레벨 시드의 walk 스트림.
- 의심: 그 자리에 멈춰 자극 쪽을 본다(§3 머리 행동). 평온으로 돌아오면 경로를 이어 간다.
- 광분: 마지막으로 본 위치가 몸동작(§7)으로 닿지 않으면 그 위치를 향해 `human.chaseSpeed`로 걸어 쫓는다(닿는 거리가 되면 멈춰서 때린다). 공격 중에는 걷지 않는다.
- 몸은 가구를 통과하지 않는다: 골반 높이의 반지름 `human.walkRadius` 구를 이동 방향으로 sweep해 막히면 거기서 멈춘다.
- 걸음: 다리는 걸은 거리에 맞춰 번갈아 흔들린다(보폭 `human.stepLength`, 무릎·발 쪽이 앞뒤로 `human.legSwing`). 걸음마다 표현용 발소리를 낸다.
- 튕겨남(§6)은 **몸 전체의 이동·회전을 뺀** 부위 자체의 움직임 속도로 판정한다: 걷는 사람의 몸통·팔에 붙은 모기는 함께 실려 가고, 흔들리는 종아리·발에 붙은 모기는 튕겨 날 수 있다.

## 10. 두 사람 (M14, D-058)
- 레벨 데이터 `companions[]`(형식은 `human`과 같고 id가 다르다)로 인간을 더 둔다. 모든 인간이 각자 시각·청각·경계·공격·반응·무작위 동작·걷기를 한다. 모기가 붙은 부위의 주인만 그 흡혈에 반응한다.
- **광분 전염:** 한 사람이 광분하면 광분하지 않은 다른 사람의 경계를 `human.alarmShare`까지 올리고, 그 사람이 본 자극 위치를 쳐다보게 한다.
- 결과의 광분 횟수·물린 자국 수는 모든 인간의 합이다.

## 수용 기준
- [x] 상태 전이와 히스테리시스(40 진입 / 20 이탈)가 동작한다 (Core). — 증거: `HumanAwarenessTests.StateTransitions_Hysteresis_Enter40Exit20`
- [x] 자극이 없으면 2초 후부터 10/s로 감소한다 (Core). — 증거: `HumanAwarenessTests.NoStimulus_DecaysAfter2SecondsAt10PerSecond`, `NoStimulus_PlayerHidden_UsesShadowDecayRate`
- [x] Yellow Zone 안, 시야 확보 시 거리별 증가율이 25→8/s 선형이다 (Core). — 증거: `HumanVisionTests.YellowZone_LineOfSight_RateIsLinearFrom25To8`(40/150/299u), `YellowZone_IntegratedOverOneSecond_AwarenessRisesByRate`
- [x] 장애물이 시선을 막으면 시각 증가가 0이고, 스냅샷의 PlayerVisibleToHuman이 거짓이다 (Core). — 증거: `HumanVisionTests.Obstacle_BlocksLineOfSight_NoVisionGainAndNotVisible`, `Hidden_InYellowZone_NotVisibleAndNoGain`
- [x] Red Zone 진입 시 같은 틱에 박수 공격이 시작되고 광분이 된다 (Core). — 증거: `HumanVisionTests.RedZone_EnteredWithLineOfSight_ClapAndFrenzySameTick`
- [x] 소리 거리는 가까운 귀 기준이다: 머리를 돌리면 같은 위치의 소음 증가율이 달라진다 (Core). — 증거: `HumanHearingTests.FlightNoise_HeadTurned_SamePositionGivesDifferentRate`, `FlightNoise_InsideRadiusOutsideEarZone_UsesNearestEarDistance`
- [x] 비행 소음 증가율이 귀 거리 0에서 16/s, 반경 끝에서 4/s이다 (Core). — 증거: `HumanHearingTests.FlightNoise_EarDistanceZero_Is16AndRadiusEdge_Is4`, `FlightNoise_Precision_HalvesRadius`
- [x] 귀 근접 구역에서 +30/s가 추가되고 귀 반응이 발생할 수 있다 (Core). — 증거: `HumanHearingTests.EarZone_Inside_Adds30PerSecond`(+30/s), `ReactionTests.EarZone_Airborne_ProducesEarReactionsAtEarRate`(귀 반응)
- [x] 대시 NoiseEvent가 반경 안의 인간 경계를 즉시 +40 한다 (Core). — 증거: `HumanHearingTests.DashNoise_EarInsideRadius_RaisesAwarenessBy40Immediately`, `DashNoise_EarOutsideRadius_NoChange`
- [x] 광분은 최소 8초 유지되고, 그 뒤 6초 연속 보지 못하면 경계 60, Suspicious가 된다 (Core). — 증거: `FrenzyTests.Frenzy_NeverSeen_LastsMinimum8SecondsThenCalmsTo60`, `Frenzy_SeenUntil10Seconds_CalmsAfter6UnseenSeconds` (D-030)
- [x] 광분 중 다시 보이면 연속 미발견 시간이 초기화된다 (Core). — 증거: `FrenzyTests.Frenzy_SeenAgainWhileHidden_ResetsUnseenTimer`
- [x] 광분 중 보이지 않아도 마지막 위치 주변에 맹목 휘두르기가 발생한다 (Core). — 증거: `FrenzyTests.Frenzy_PlayerNotVisible_BlindSwatsAroundLastSeenPosition`
- [x] 착지 반응 확률이 민감도에 비례한다: 같은 시드 10,000회 시행에서 기대값 ±2%p (Core, 통계 테스트). — 증거: `ReactionTests.LandingReaction_10000Trials_MatchesLandChanceTimesSensitivity`(5개 부위 유형), `LandingReaction_SameSeed_SameOutcomes`
- [x] 부착 중 반응 위험률이 가려움 제곱에 비례해 증가하고, 가려움 100이면 즉시 반응한다 (Core). — 증거: `ReactionTests.AttachedHazard_GrowsWithItchSquared`, `AttachedHazard_PerTickRolls_MatchExponentialProbability`, `Attached_Itch100_ReactsImmediately`
- [x] 모든 공격과 반응의 목표가 예고 시작 시점의 위치로 고정된다: 예고 중 판정 밖으로 이동하면 생존하고, 머무르면 사망한다 (Core). — 증거: `ReactionTests.ReactSlap_PlayerLeavesDuringTelegraph_Survives`, `ReactSlap_PlayerStays_DiesWithAttackCause`, `Clap_PlayerLeavesRedZoneDuringTelegraph_Survives`, `Clap_PlayerStays_Dies` (손바닥·맹목 휘두르기도 같은 `HumanAttackSystem.Start` 경로)
- [x] 무작위 움직임이 흡혈 중에도 발생하고, 붙어 있는 모기가 부위를 따라 움직인다 (Core). — 증거: `SuckTests.RandomMotion_WhileSucking_SessionContinuesAndFollowsPart`, `HumanMotionTests.Actions_KeepHappeningWhilePlayerIsAttached`
- [x] 부위 속도가 dislodgeSpeed를 넘으면 모기가 튕겨 나가고 흡혈 세션이 끝나며, 사망하지 않는다 (Core). — 증거: `SuckTests.Dislodged_WhileSucking_SessionEndsWithoutDeath`, `HumanMotionTests.FastAction_PartFasterThanDislodgeSpeed_DislodgesWithoutDeath`
- [x] 같은 시드와 같은 입력이면 인간의 움직임과 반응이 똑같이 재현된다 (Core). — 증거: `FrenzyTests.SameSeedAndCommands_HumanBehaviourReplaysExactly` (다른 시드는 다른 결과)
- [x] 머리가 yawLimit를 넘어 회전하지 않는다 (Core). — 증거: `HumanAwarenessTests.Head_StimulusBehind_NeverExceedsYawLimit`
- [x] 공격 중에는 새 공격이나 반응이 시작되지 않는다 (Core). — 증거: `ReactionTests.AttackInProgress_NoNewAttackOrReactionUntilRecoveryEnds`
- [x] 공격 중 가까운 쪽 팔 캡슐이 예고(치켜듦) → 판정(목표로 뻗음) → 회복(제자리) 순서로 움직이고, 판정 시점에 손 끝이 목표 구 안에 있다 (Core). (M12) — 증거: `BodyAttackTests.Strike_RespectsJointLimitsAndHumanHandSpeed`(5개 목표: 손이 예고·타격·회복 동안 Core 팔 캡슐로 움직이고 타격 끝 손바닥이 목표 구 안), 캡처 `Captures/2026-10-01_222754/Sandbox_Human_attack_*.png`
- [x] 공격 표현에 별도 팔 오브젝트가 없고, 인간 팔 그림이 Core 팔 캡슐을 따른다 (Unity). (M12) — 증거: EditMode `HumanBodyViewTests.Attack_ArmVisualsFollowCoreArmCapsules_NoExtraArm` (HumanArmPose·AttackArm 제거)
- [x] 공격 목표가 팔 길이(+자세별 기울임) 밖이면 손으로 때리지 않는다 (Core). (M12) — 증거: `BodyAttackTests.Planner_TooFar_NoPlan_AndMaxPostureLimitsBody`, `Frenzy_PlayerSeenBeyondReach_NoSlap`
- [x] 팔꿈치·어깨 각도가 관절 한계를 넘지 않고, 손의 최고 속도가 tuning의 사람 손 속도를 넘지 않는다 (Core, 모든 공격 종류). (M12) — 증거: `BodyAttackTests.Strike_RespectsJointLimitsAndHumanHandSpeed`(팔꿈치 ≤ 150°, 위팔 길이 불변, 손 속도 ≤ 최고 속도), `Planner_TargetBehindNeedsTurn_NotJustArm`(어깨 폄 한계)
- [x] 한쪽 팔뚝에 앉은 모기를 반대쪽 손으로 때린다 (Core). (M12) — 증거: `BodyAttackTests.ReactSlap_OnOwnRightForearm_UsesLeftHand`
- [x] 판정이 움직이는 손 경로로 이뤄진다: 예고 뒤 손이 지나가기 전에 비키면 산다 (Core). (M12) — 증거: `BodyAttackTests.Strike_PlayerOnTheHandPath_NotAtTarget_Dies`, `ReactionTests.ReactSlap_PlayerLeavesDuringTelegraph_Survives`
- [x] 공격 예고 표시가 예고 진행률에 따라 변한다(차오름·좁혀짐 등) (Unity). (M12) — 증거: EditMode `HumanBodyViewTests.Telegraph_IndicatorChangesWithProgress_ThenFlashesOnStrike`(셰이더 `Moqui/TelegraphRing`의 _Progress가 진행에 따라 증가, 판정 때 _Strike = 1, 손 접근 줄기 표시), 캡처 `Captures/2026-10-05_170605/Sandbox_Human_attack_Telegraph.png`(좁혀 오는 고리·접근 줄기)·`_Active.png`(번쩍임)
- [x] 광분 중 목표가 팔 길이 밖이면 기울이기 → 돌기 → 일어서기 순서로 필요한 만큼만 몸을 움직여 닿고, 각 자세 전환에 tuning의 시간이 걸린다 (Core). (M12) — 증거: `BodyAttackTests.Planner_UsesOnlyAsMuchBodyAsNeeded`(팔·기울이기·일어서기), `Planner_TargetBehindNeedsTurn_NotJustArm`, `Rise_TakesHumanTime_AndHeadAndVisionFollowTheBody`(posture.riseTime)
- [x] 일어서기·기울이기 후 머리 위치와 시야 원뿔이 몸 포즈를 따른다 (Core). (M12) — 증거: `BodyAttackTests.Rise_TakesHumanTime_AndHeadAndVisionFollowTheBody`, `Lean_HeadForwardFollowsUpperBody`
- [x] 레벨의 `human.maxPosture`보다 큰 몸동작은 쓰지 않는다 (Core). (M12) — 증거: `BodyAttackTests.Planner_TooFar_NoPlan_AndMaxPostureLimitsBody`, stage03(누운 인간) `maxPosture: "turn"`
- [x] 걷는 인간이 경로점을 차례로 돌며 걷는 속도·회전 속도·멈춤을 지킨다 (Core). (M14) — 증거: `WalkTests.Walk_FollowsRouteAtWalkSpeed_PausesAndTurnsAtEachPoint`
- [x] 의심이면 멈추고, 광분 중 닿지 않으면 마지막으로 본 위치로 쫓아가 닿는 거리에서 멈춘다 (Core). (M14) — 증거: `WalkTests.Walk_Suspicious_StopsAndStays`, `Walk_FrenzyTargetOutOfReach_ChasesUntilReachable`
- [x] 가구에 막히면 통과하지 않고 멈추며, 경로가 가구를 지나면 레벨 검사가 보고한다 (Core). (M14) — 증거: `WalkTests.Walk_BlockedByFurniture_DoesNotPassThrough`, `LevelDataTests.Validator_WalkRouteThroughFurniture_Reported`
- [x] 걷는 사람의 몸통에 붙은 모기는 실려 가고, 흔들리는 종아리에 붙은 모기는 튕겨 날 수 있다 (Core). (M14) — 증거: `WalkTests.Walk_RiderOnBody_CarriedUnlessOnSwingingLeg`(팔뚝: 실려 감 150u 이상, 종아리: 튕겨남), 발소리 EditMode `AudioTests.Footstep_OncePerStep`
- [x] 전기 모기채를 든 팔은 채 길이만큼 더 닿고 판정이 넓다 (Core). (M14) — 증거: `BodyAttackTests.Swatter_ExtendsReachOfTheToolArm`, `Swatter_WiderHit`(손바닥이면 비껴갈 거리에서 채는 맞음)
- [x] 두 사람이 각자 감지·공격하고, 붙은 부위의 주인에게서 흡혈하며, 한 사람이 광분하면 다른 사람이 경계해 그쪽을 본다 (Core). (M14) — 증거: `CompanionTests.EachHuman_SensesOnItsOwn`, `Sucking_TheFriendsArm_FeedsAndMarksTheFriend`, `Frenzy_SpreadsAlarmToTheOtherHuman`

- [x] 광분 순간 최근 경계를 가장 많이 올린 원인(눈·귀·가려움·시선·동행자)을 낸다 (Core). (M14, gulf §10) — 증거: `AwarenessCauseTests.StayingInSight_FrenzyCauseIsSight`, `DashingBehindTheHead_FrenzyCauseIsHearing`, `Memory_ForgetsOldCauses`
- [x] 광분 순간 짧은 느린 화면과 원인 문구, 결과 화면에 원인 색 경계 타임라인 (Unity). (M14) — 증거: EditMode `FrenzyMomentTests`
## 범위 외
- 인간의 보행과 자리 이동, 파리채·전기 모기채 등 도구 공격 (향후 확장 후보: 도구는 닿는 거리·판정 면적을 늘려 난이도·레벨링 요소로 쓸 수 있다 — plan/ideas.md) (스프레이 제외), 다수의 인간
