# 05. 물방울 라이딩 (QTE) · 습기 · 젖은 날개

## 목적
모기가 빗방울에 맞아도 함께 떨어지다 탈출하는 실제 생태(`knowledge/mosquito-flight.md` §2)를 긴장감 있는 QTE로 옮긴다. 물 관련 요소는 Stage 4(화장실)에서 처음 등장한다 (D-015).

## 1. 물방울
- `DripSource`: 고정 위치에서 `water.dropInterval`마다 물방울(반경 `water.dropRadius`)을 떨어뜨린다. 물방울은 `world.gravity`로 낙하하고 바닥에 닿으면 사라진다.
- 발생원은 착지면에서 최소 `water.minSourceHeight` 위에 둔다 (탈출 시간 보장).
- 플레이어가 낙하 중인 물방울에 닿으면 **Trapped** 상태가 된다.
  - 플레이어는 물방울 중심에 고정되고 물방울은 `water.trappedFallSpeed`로 등속 낙하한다.
  - 이동 입력은 무시된다. 화면에 "Shift ×2" QTE 프롬프트와 남은 높이 게이지를 표시한다.
  - Dash 입력을 `water.escapePresses`회 하면 탈출한다. 탈출 입력은 스태미나와 쿨타임을 소모하지 않는다.
  - 탈출 전에 물방울이 바닥/가구 상면에 닿으면 사망한다 (DeathCause.WaterImpact).
- 탈출하면 위쪽으로 대시 1회분을 이동하고 **젖은 날개** 디버프를 받는다.
  - `wetWings.duration` 동안 속도 `wetWings.speedMul`, 스태미나 회복 `wetWings.regenMul`, 대시 비용 `+wetWings.dashCostAdd`.
  - 다시 젖으면 지속시간이 갱신된다 (중첩 없음).
- 스킬 발수 코팅(spec/09)이 지속시간을 줄이고, 3레벨에서 탈출 입력 횟수를 1회 줄인다.
- Trapped 상태에서도 인간의 감지와 공격은 계속된다.
- 숨은 상태(Shadow Zone)에서는 젖은 날개 남은 시간이 `hiding.debuffRecoveryMul`배 빠르게 줄어든다 (spec/03).

## 2. 습기
- `HumidZone`: 레벨 데이터의 박스 영역. 세기 `strong` 또는 `weak`.
- **습기 게이지**(플레이어, 0~100): 습기 영역 안에서는 `humid.gainStrong` 또는 `humid.gainWeak`/s로 오르고(스킬 발수 코팅 배율), 밖에서는 `humid.decay`/s로 내린다 (숨은 상태면 `hiding.debuffRecoveryMul`배).
- 게이지가 100에 도달하면 **젖은 날개**가 걸린다. 100인 동안 계속 습기 영역에 있으면 지속시간이 매 틱 갱신된다.
- 강한 습기 영역은 **증기**로 채워진다:
  - 인간의 시각 증가율에 `humid.steamVisionMul`을 곱한다 (증기 속 모기는 잘 안 보임).
  - 플레이어의 선명 시야 거리에 `humid.steamClearRangeMul`을 곱한다 (spec/11).
  - 즉 증기는 숨기 좋지만 날개를 적신다.
- 부착 상태에서도 습기는 오른다.

## 수용 기준
- [x] 물방울이 2.5초 간격으로 생성된다 (Core). — 증거: `WaterTests.Drops_SpawnEvery25Seconds`, `Drops_FallAndVanishOnFloor`
- [x] 물방울에 닿으면 Trapped가 되고 이동 입력이 무시된다 (Core). — 증거: `WaterTests.DropHitsPlayer_Trapped_MoveInputIgnored`, `DropPassesPlayer_AnyHeight_AlwaysTraps`
- [x] Trapped 중 낙하 속도가 100u/s 등속이다 (Core, ±2%). — 증거: `WaterTests.Trapped_FallsAtConstant100`
- [x] 대시 2회 입력 시 탈출하고, 스태미나가 줄지 않는다 (Core). — 증거: `WaterTests.DashTwice_Escapes_StaminaUnchanged`
- [x] 탈출 전 바닥 충돌 시 WaterImpact로 사망한다 (Core). — 증거: D-031) — 증거: `WaterTests.TrappedUntilFloor_DiesWithWaterImpact`
- [x] 탈출 후 10초간 속도 0.7배, 회복 0.5배, 대시 비용 35 (Core). — 증거: `WaterTests.AfterEscape_WetWingsFor10Seconds_SpeedRegenDashCost`
- [x] 젖은 날개 중 다시 젖으면 지속시간만 10초로 갱신된다 (Core). — 증거: `WaterTests.WetAgain_DurationRefreshedNotStacked`
- [x] 모든 레벨의 DripSource가 착지면 기준 150u 이상이다 (Core: 레벨 데이터 검사 — 아래로 광선 검사). — 증거: `LevelDataTests.Level_DripSourcesAreHighEnough`(stage01~05, Stage 4의 샤워기·결로 4개)
- [x] 습기 게이지가 강/약 영역에서 15/5 per s로 오르고, 밖에서 10/s로 내린다 (Core). — 증거: `WaterTests.Humidity_StrongWeakOutside_15_5_Minus10`, `Humidity_RisesWhileAttached`
- [x] 습기 100에서 젖은 날개가 걸리고, 영역 안에 머무는 동안 지속시간이 갱신된다 (Core). — 증거: `WaterTests.Humidity_Reaches100_WetWingsAndRefreshedWhileInside`
- [x] 증기 안의 플레이어에 대한 시각 증가율이 0.6배이다 (Core). — 증거: `WaterTests.Steam_VisionRateIs06Times`, `Steam_IntegratedInHumanSystem`
- [x] 숨은 상태에서 젖은 날개 시간과 습기 게이지가 2배 빠르게 줄어든다 (Core). — 증거: D-034) — 증거: `WaterTests.Hidden_WetAndHumidityRecoverTwiceAsFast`

## 범위 외
- 비 내리는 야외 스테이지, 물방울 크기 변화, 바닥 물웅덩이
