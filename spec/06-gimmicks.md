# 06. 환경 기믹과 인간 수정자 — 선풍기 · 스프레이 · 모기향 · 거미줄 · 졸음 · 취함

## 목적
`knowledge/ecological-relationships.md`의 생태 요소, 인간의 도구(모기약), 인간의 상태(졸음, 취함)를 스테이지별로 단계적으로 도입한다. (D-003, D-014, D-015) 물방울과 습기는 spec/05에 있다.

| 기믹 | 처음 등장 | 위험 | 보상 |
|---|---|---|---|
| 졸음 (인간 수정자) | Stage 1 | 가끔 깜빡 깸 | 시야 거의 없음, 둔한 반응 |
| 선풍기 | Stage 3 | 바람에 밀려 궤적이 무너짐 | 소음 마스킹, 연기 흩뜨림 |
| 모기약 스프레이 | Stage 3 | 조작 교란, 오래 있으면 즉사 | 없음 (광분의 대가) |
| 물방울 · 습기 | Stage 4 | 추락, 젖은 날개 | 증기 속 은신 (spec/05) |
| 거미줄 | Stage 5 | 접촉 즉사 | 없음 (지형 제약) |
| 취한 타겟 (인간 수정자) | Stage 5 | 무작위 휘두르기 | 흡혈 2배, 짙은 CO₂ |
| 모기향 | Stage 5 | 스테이지 전체 약한 중독, 근처는 진함 | 없음 (환경 압박) |

## 선풍기 (`Fan`)
- 바람 원뿔: 반경 `fan.range`, 반각 `fan.halfAngle`. 머리가 `fan.oscillationAngle` 범위를 `fan.oscillationPeriod` 주기로 회전한다.
- 원뿔 안의 플레이어에게 바람 방향으로 `fan.windSpeed`를 더한다. 이동 속도와 별개로 더해지는 외력(관성 규칙 미적용)이므로 입력을 놓아도 밀린다. 부착 상태에서는 영향이 없다.
- 소음 마스킹: 선풍기에서 `fan.noiseMaskRadius` 안에 있으면 플레이어의 모든 소음 반경에 `fan.noiseMaskMul`을 곱한다.
- 바람 원뿔 안에서는 CO₂ 흐름이 흩어진다 (후각 교란, spec/11 §2). 스프레이 연무도 바람에 떠밀리고, 모기향의 중독 하한이 낮아진다.
- 선풍기 본체는 Obstacle이다.

## 에어컨 타이머 (`AirConditioner`, M14)
- 레벨 데이터 `fans[]`에 `"kind": "airConditioner"`와 주기 `schedule` {on, off, offset}(s)를 둔다. 머리가 돌지 않고 `yaw`/`pitch` 방향으로만 분다.
- 바람: 원뿔 반경 `aircon.range`, 반각 `aircon.halfAngle`, 속도 `aircon.windSpeed`. 켜져 있을 때만 바람·소음 마스킹·CO₂ 흩어짐이 있다. 시작 후 `offset`초가 지난 것처럼 주기가 돈다.
- 해법: 주기를 읽고 꺼진 동안 지나가거나, 켜진 동안 소음 마스킹을 이용한다. 본체는 Obstacle.

## 조명 스위치 (`Light`, M14)
- 레벨 데이터 `lights[]`: 전구 위치 `position`, 비추는 영역 `area` {center, size}(상자), 켜는 방식은 주기 `schedule` {on, off, offset} 또는 `onWhenAlert`(인간이 의심·광분하면 `light.switchDelay` 뒤 켜고, 평온이 `light.offDelay` 이어지면 끈다).
- 켜진 영역 안에서는 그림자가 사라져 Shadow Zone이 숨겨 주지 못하고, 인간의 시각 증가가 `light.visionMul`배가 된다.
- 해법: 불이 켜지기 전에 빠져나가거나 영역 밖의 은신처로, 꺼질 때까지 기다리기. 들키면 불이 켜지므로 다음 접근이 어려워진다.

## 모기장 (`Net`, M14)
- 레벨 데이터 `nets[]`(그물 판 상자)와 `netGaps[]`(틈 볼륨 상자). 그물은 유리처럼 몸은 막고 시야는 막지 않으며, 붙을 수 있다. 표현은 반투명 그물.
- 비행 중 틈 볼륨에 들어서는 순간 정밀 비행이 아니면 그물을 스쳐 소음(`net.rustleRadius`, 경계 +`net.rustleAwareness`)을 낸다.
- 해법: 관망으로 틈을 찾고 정밀 비행으로 조용히 통과한다.

## 거미줄 (`SpiderWeb`)
- 얇은 평면형 트리거. 시각적으로 잘 보여야 한다 (화이트박스: 흰색 반투명 격자).
- 접촉하면 이동 입력을 막고 `web.struggleTime` 후 사망한다 (DeathCause.Web). 탈출할 수 없다.

## 모기약 스프레이 (`SprayCloud`)
- 발생원:
  - 광분한 인간의 분사 (spec/02 §4, `human.canSpray`). 예고 `spray.telegraph` 후 인간 손에서 플레이어 방향 `spray.travel` 지점에 연무가 생긴다.
  - 레벨의 자동 분사기 `sprayDispensers[]`. `spray.dispenserInterval`마다 정해진 위치에 연무를 만든다.
- 연무: 반경이 `spray.radiusStart`에서 `spray.radiusMax`까지 `spray.expandTime` 동안 커지고, `spray.lifetime` 동안 남은 뒤 사라진다. 바람 영역 안에서는 바람 방향으로 `fan.windSpeed × spray.windDriftMul`만큼 떠밀린다.
- **중독 게이지**(플레이어, 0~100): 연무 안에서는 `spray.toxinRate`/s(스킬 해독 체질 배율)로 오르고, 밖에서는 `spray.toxinDecay`/s로 내린다. 부착 상태에서도 오른다.
- 디버프 (누적 적용, Core의 입력 디버프 필터로 구현, spec/01):
  | 중독 | 효과 |
  |---|---|
  | ≥ `spray.tier1` | **끊김:** `spray.stutterInterval`마다 일정 확률로 `spray.stutterDuration` 동안 이동·대시 입력 무시. 확률은 중독 tier1→tier2 구간에서 `spray.stutterChanceMin`→`spray.stutterChance`로 커진다 |
  | ≥ `spray.tier2` | **반전:** 이동 입력(전후·좌우·상하)이 반대로 적용 (대시 방향 포함) |
  | ≥ `spray.tier3` | **랜덤:** `spray.randomInterval`마다 `spray.randomDuration` 동안 무작위 방향 이동이 입력을 덮어씀 |
- 숨은 상태(Shadow Zone)에서는 중독 감소가 `hiding.debuffRecoveryMul`배 빨라진다 (spec/03).
- 중독 100이면 사망한다 (DeathCause.Spray). 연무 한가운데에 머물면 약 3초 만에 죽으므로 즉시 벗어나야 한다.
- 표현: 옅은 녹색 안개, 분사음 `sfx_spray`, 중독 단계별 화면 가장자리 녹색 일렁임. 반전·랜덤 단계는 멀미를 막기 위해 화면을 뒤집지 않고 HUD 아이콘으로만 알린다.

## 모기향 (`MosquitoCoil`)
- 레벨에 놓인 연기 발생원. 스테이지 전체에 **중독 하한**을 깐다: 중독 게이지는 감소해도 현재 위치의 하한 아래로 내려가지 않는다. 하한은 스프레이와 같은 중독 게이지와 디버프 단계를 쓴다.
- 하한 (모기향까지 거리 `d`):
  - `d ≤ coil.denseRadius`: `coil.nearFloor` (반전 단계)
  - `coil.denseRadius < d ≤ coil.range`: `coil.nearFloor`에서 `coil.farFloor`로 선형 감소 (멀면 약한 끊김)
  - `d > coil.range`: 0
- `d ≤ coil.lethalRadius`이면 하한과 별개로 중독이 `coil.coreRate`/s 오른다 (향 바로 위는 위험).
- 하한 배율 (곱): 바람 원뿔 안 `coil.windFloorMul` (연기가 흩어짐), Shadow Zone 안 `coil.shadowFloorMul` (연기가 덜 스며듦).
- 스킬 해독 체질은 하한과 증가율 모두에 적용된다.
- 표현: 모기향에서 피어오르는 가는 연기 줄기가 바람을 따라 흐르고, 하한이 높은 곳일수록 공기가 옅은 회녹색으로 보인다.

## 졸음 (`DozeModifier`)
- 인간 수정자. 아래 두 상태를 반복한다.
  | 상태 | 지속 | 효과 |
  |---|---|---|
  | 졸기 | `doze.sleepDuration` | 눈을 감음: 시각 감지 없음(Red Zone 포함). 청각 증가 `doze.hearingMul`배, 반응 확률 `doze.reactionMul`배 |
  | 깜빡 깸 | `doze.wakeDuration` | 눈을 뜨고 고개를 듦: 평소처럼 감지. 깨기 `doze.wakeTelegraph` 전에 머리가 움찔하는 예고가 있다 |
- 경계가 `awareness.suspiciousEnter` 이상이 되면 완전히 깬다. 경계가 `awareness.suspiciousExit` 미만으로 `doze.resleepDelay` 동안 유지되면 다시 졸기 시작한다.
- 광분은 일어날 수 있지만 `doze.frenzyDurationMul`배로 짧다 (튜토리얼 배려).

## 취한 타겟 (`DrunkModifier`)
- 인간 컴포넌트에 붙는 수정자. 흡혈 속도 `drunk.suckRateMul`, 가려움 증가 `drunk.itchRateMul`, 시각 증가 `drunk.visionRateMul`, CO₂ 세기 `drunk.co2Mul`을 적용한다.
- 상태와 무관하게 `drunk.randomSwatInterval`마다 몸 주변 `drunk.randomSwatRadius` 안의 무작위 지점을 손바닥으로 친다. 예고 `drunk.slapTelegraph`.
- 무작위 지점은 시드 고정 난수로 결정한다 (테스트 재현성).

## 수용 기준
- [x] 바람 원뿔 안에서 입력이 없으면 플레이어가 40u/s로 밀린다 (Core). — 증거: `GimmickTests.Fan_InsideCone_NoInput_Pushed40PerSecond`, `ExternalForceTests.Wind_*` (D-046)
- [x] 부착 상태에서는 바람의 영향이 없다 (Core). — 증거: `GimmickTests.Fan_Attached_NotAffected` (D-046)
- [x] 선풍기가 8초 주기로 ±45° 회전한다 (Core). — 증거: `GimmickTests.Fan_Oscillates45DegreesOver8SecondPeriod` (D-046)
- [x] 마스킹 반경 안에서 대시 소음 반경이 75u가 된다 (Core). — 증거: `GimmickTests.Fan_NoiseMask_DashNoiseRadiusBecomes75` (D-046)
- [x] 거미줄 접촉 1.5초 후 Web 원인으로 사망한다 (Core). — 증거: `GimmickTests.Web_Contact_DiesWithWebCauseAfter15Seconds_CannotMove` (D-046)
- [x] 취한 타겟에게서 흡혈 속도 배율이 2.0이다 (Core). — 증거: `GimmickTests.Drunk_SuckRateMultiplierIs2` (D-046)
- [x] 연무 반경이 1초에 걸쳐 25u→60u로 커지고 8초 뒤 사라진다 (Core). — 증거: `GimmickTests.SprayCloud_Expands25To60OverOneSecond_GoneAfter8Seconds` (D-046)
- [x] 바람 영역 안의 연무가 20u/s로 떠밀린다 (Core). — 증거: `GimmickTests.SprayCloud_InWind_DriftsAt20PerSecond` (D-046)
- [x] 중독이 연무 안에서 30/s로 오르고 밖에서 15/s로 내린다 (Core). — 증거: `GimmickTests.Toxin_RisesInCloud30PerSecond_Decays15PerSecondOutside` (D-046)
- [x] 중독 30/55/80에서 끊김/반전/랜덤이 누적 적용되고, 같은 시드에서 재현된다 (Core). — 증거: `GimmickTests.Debuffs_StutterInvertRandom_StackByTier_ReproducibleWithSeed` (D-046)
- [x] 중독 100에서 Spray 원인으로 사망한다 (Core). — 증거: `GimmickTests.Toxin_100_DiesWithSprayCause` (D-046)
- [x] 광분 + canSpray + 사거리 안에서 보일 때만 인간이 분사하고, 쿨타임을 지킨다 (Core). — 증거: `GimmickTests.HumanSpray_OnlyInFrenzyWithCanSprayWhenVisibleInRange_RespectsCooldown` (D-046)
- [x] 자동 분사기가 12초마다 연무를 만든다 (Core). — 증거: `GimmickTests.Dispenser_CreatesCloudEvery12Seconds` (D-046)
- [x] 끊김 확률이 중독 30에서 0.1, 55에서 0.3이다 (Core). — 증거: `GimmickTests.StutterChance_Is01At30_03At55` (D-046)
- [x] 모기향 하한: 60u 이내 60, 400u 지점 30, 범위 밖 0이고, 바람·Shadow Zone 안에서 0.5배이다 (Core). — 증거: `GimmickTests.CoilFloor_60Within60_30At400_0Beyond_HalvedInWindAndShadow` (D-046)
- [x] 모기향 20u 이내에서는 중독이 하한과 별개로 25/s 오른다 (Core). — 증거: `GimmickTests.Coil_Within20_RisesExtra25PerSecond` (D-046)
- [x] 숨은 상태에서 중독이 2배 빠르게 줄어들되 하한 아래로는 내려가지 않는다 (Core). — 증거: `GimmickTests.Hidden_ToxinDecaysTwiceAsFast_ButNotBelowCoilFloor` (D-046)
- [x] 졸기 중에는 Red Zone에 들어가도 시각 감지와 박수 공격이 없다 (Core). — 증거: `HumanModifierTests.Doze_SleepingInRedZone_NoVisionAndNoClap`
- [x] 졸음 주기가 tuning 범위를 따르고, 깨기 0.5초 전에 예고 이벤트가 나온다 (Core). — 증거: `HumanModifierTests.Doze_CycleFollowsTuningRanges_WakeTelegraph05SecondsBefore`(150초, 10회 이상 전환), `Doze_AwarenessAtSuspicion_FullyWakes_ThenResleepsAfter5CalmSeconds`, `Doze_Sleeping_HearingAndReactionsHalved`
- [x] 경계 40 이상이면 완전히 깨고, 20 미만 5초 유지 후 다시 존다 (Core). — 증거: `HumanModifierTests.Doze_AwarenessAtSuspicion_FullyWakes_ThenResleepsAfter5CalmSeconds`
- [x] 졸음 수정자 아래의 광분 최소 유지 시간이 0.5배이다 (Core). — 증거: `HumanModifierTests.Doze_FrenzyMinimumHalved_CalmsSoonerThanAwakeHuman`
- [x] 무작위 휘두르기가 4~7초 간격으로, 같은 시드에서 같은 위치로 발생한다 (Core). — 증거: `GimmickTests.Drunk_RandomSwatsEvery4To7Seconds_ReproducibleWithSeed` (D-046)
- [x] 에어컨은 주기에 따라 켜진 동안만 고정 방향의 센 바람과 소음 마스킹을 주고, 송풍 날개는 켜진 동안만 보인다 (Core + Unity). (M14) — 증거: `GimmickTests.AirConditioner_BlowsOnlyWhileOn_FixedDirection_Stronger`, EditMode `GimmickViewTests.AirConditionerVane_VisibleOnlyWhileOn`
- [x] 조명이 주기 또는 인간 경계에 따라 켜지고 꺼지며, 켜진 영역 안에서는 은신이 풀리고 시각 증가가 배가된다 (Core + Unity). (M14) — 증거: `LightTests.Scheduled_LitOnlyWhileOnAndInsideArea`, `OnWhenAlert_SwitchesOnAfterDelay_OffAfterCalm`, `Lit_ShadowZoneNoLongerHides`, `Lit_VisionGainMultiplied`, EditMode `GimmickViewTests.Lamp_LitOnlyWhileOn`
- [x] 모기장 그물은 몸을 막고 시야는 막지 않으며, 틈을 정밀 비행 없이 지나면 소음이 난다 (Core). (M14) — 증거: `NetTests.Net_BlocksBody_NotSight`, `Gap_FlyingThroughWithoutPrecision_Rustles`(정밀 비행이면 무음)

## 범위 외
- 잠자리 등 포식자 AI, 모기향, 전기 모기채
