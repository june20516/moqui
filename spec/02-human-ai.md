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
  - 플레이어가 사거리 `attack.reach + frenzy.reachBonus` 안에 보이면 손바닥 공격을 `frenzy.slapInterval` 간격으로 반복한다 (예고 `frenzy.slapTelegraph`).
  - 플레이어가 보이지 않아도 마지막으로 본 위치 주변 `frenzy.blindSwatRadius` 안의 무작위 지점을 `frenzy.blindSwatInterval`마다 휘두른다 (사거리 안일 때).
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
| 손바닥 (slap) | 광분 + 사거리 안에서 보임 | `frenzy.slapTelegraph`. 예고 시작 시점의 모기 위치에 목표 고정 | 반경 `attack.slap.radius`, `attack.slap.activeTime` 동안 |
| 맹목 휘두르기 (blindSwat) | 광분 + 보이지 않음 | `frenzy.slapTelegraph` | 마지막 위치 주변 무작위 지점, 반경 `attack.slap.radius` |
| 반응 때리기 (reactSlap) | §5 | `attack.selfSlap.telegraph` | 예고 시작 시점의 모기 위치, 반경 `attack.selfSlap.radius` |
| 스프레이 | 광분 + `canSpray` | `spray.telegraph` (캔을 드는 동작) | 연무 구역 생성 (spec/06) |

- 판정 구에 플레이어 충돌 구가 겹치면 즉사한다 (스프레이 제외).
- **예고 피드백** (Unity): 손 동작, 판정 위치의 붉은 표시, 경고음 `sfx_telegraph`, 화면 밖 공격이면 화면 가장자리에 방향 표시 (spec/08).
- 공격 후 `recovery` 동안 다음 공격을 하지 않는다.

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

## 범위 외
- 인간의 보행과 자리 이동, 파리채·전기 모기채 등 도구 공격 (스프레이 제외), 다수의 인간
