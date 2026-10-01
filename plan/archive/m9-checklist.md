# M9 Stage 3 · 4 · 5와 기믹 — 체크리스트 (보관)

태그 `m9-done` (2026-10-01).

### 선풍기 · 거미줄 (spec/06)
- [x] 바람 원뿔 안에서 입력이 없으면 플레이어가 40u/s로 밀린다 (Core). — 증거: `GimmickTests.Fan_InsideCone_NoInput_Pushed40PerSecond`, `ExternalForceTests.Wind_*` (D-046)
- [x] 부착 상태에서는 바람의 영향이 없다 (Core). — 증거: `GimmickTests.Fan_Attached_NotAffected` (D-046)
- [x] 선풍기가 8초 주기로 ±45° 회전한다 (Core). — 증거: `GimmickTests.Fan_Oscillates45DegreesOver8SecondPeriod` (D-046)
- [x] 마스킹 반경 안에서 대시 소음 반경이 75u가 된다 (Core). — 증거: `GimmickTests.Fan_NoiseMask_DashNoiseRadiusBecomes75` (D-046)
- [x] 거미줄 접촉 1.5초 후 Web 원인으로 사망한다 (Core). (M3 이월: DeathCause Web 원인별 테스트 겸함) — 증거: `GimmickTests.Web_Contact_DiesWithWebCauseAfter15Seconds_CannotMove` (D-046)

### 스프레이 · 중독 · 모기향 (spec/06)
- [x] 연무 반경이 1초에 걸쳐 25u→60u로 커지고 8초 뒤 사라진다 (Core). — 증거: `GimmickTests.SprayCloud_Expands25To60OverOneSecond_GoneAfter8Seconds` (D-046)
- [x] 바람 영역 안의 연무가 20u/s로 떠밀린다 (Core). — 증거: `GimmickTests.SprayCloud_InWind_DriftsAt20PerSecond` (D-046)
- [x] 중독이 연무 안에서 30/s로 오르고 밖에서 15/s로 내린다 (Core). — 증거: `GimmickTests.Toxin_RisesInCloud30PerSecond_Decays15PerSecondOutside` (D-046)
- [x] 중독 30/55/80에서 끊김/반전/랜덤이 누적 적용되고, 같은 시드에서 재현된다 (Core). — 증거: `GimmickTests.Debuffs_StutterInvertRandom_StackByTier_ReproducibleWithSeed` (D-046)
- [x] 중독 100에서 Spray 원인으로 사망한다 (Core). (M3 이월: DeathCause Spray 겸함) — 증거: `GimmickTests.Toxin_100_DiesWithSprayCause` (D-046)
- [x] 광분 + canSpray + 사거리 안에서 보일 때만 인간이 분사하고, 쿨타임을 지킨다 (Core). — 증거: `GimmickTests.HumanSpray_OnlyInFrenzyWithCanSprayWhenVisibleInRange_RespectsCooldown` (D-046)
- [x] 자동 분사기가 12초마다 연무를 만든다 (Core). — 증거: `GimmickTests.Dispenser_CreatesCloudEvery12Seconds` (D-046)
- [x] 끊김 확률이 중독 30에서 0.1, 55에서 0.3이다 (Core). — 증거: `GimmickTests.StutterChance_Is01At30_03At55` (D-046)
- [x] 모기향 하한: 60u 이내 60, 400u 지점 30, 범위 밖 0이고, 바람·Shadow Zone 안에서 0.5배이다 (Core). — 증거: `GimmickTests.CoilFloor_60Within60_30At400_0Beyond_HalvedInWindAndShadow` (D-046)
- [x] 모기향 20u 이내에서는 중독이 하한과 별개로 25/s 오른다 (Core). — 증거: `GimmickTests.Coil_Within20_RisesExtra25PerSecond` (D-046)
- [x] 숨은 상태에서 중독이 2배 빠르게 줄어들되 하한 아래로는 내려가지 않는다 (Core). (M4 이월 D-034 중독 회복 겸함) — 증거: `GimmickTests.Hidden_ToxinDecaysTwiceAsFast_ButNotBelowCoilFloor` (D-046)
- [x] (M8 이월) 해독 체질이 중독 증가와 모기향 하한에 적용된다 (Core). — 증거: `GimmickTests.ResistSpray_ScalesToxinRateAndCoilFloor`, `SkillTests.ResistSpray_ToxinMultiplierPerLevel` (D-046)

### 취한 타겟 (spec/06)
- [x] 취한 타겟에게서 흡혈 속도 배율이 2.0이다 (Core). — 증거: `GimmickTests.Drunk_SuckRateMultiplierIs2` (D-046)
- [x] 무작위 휘두르기가 4~7초 간격으로, 같은 시드에서 같은 위치로 발생한다 (Core). — 증거: `GimmickTests.Drunk_RandomSwatsEvery4To7Seconds_ReproducibleWithSeed` (D-046)

### 레벨 (spec/07 Stage 3~5, spec/05 적용)
- [x] 다섯 스테이지 모두 공통 규칙의 레벨 데이터 검사를 통과한다 (Core). — 증거: `LevelDataTests.Level_PassesCommonLevelChecks`(stage01~05), `Level_AllFurnitureHasObstacleFlag`, `DataFiles_MatchJsonSchemas`
- [x] (M6 이월) 모든 레벨의 DripSource가 착지면 기준 150u 이상이다 (Core). — 증거: `LevelDataTests.Level_DripSourcesAreHighEnough`(stage01~05, Stage 4의 샤워기·결로 4개)
- [x] Unity 씬의 시각 오브젝트가 레벨 데이터의 모든 형상 ID와 1:1로 대응한다 (Unity, Stage 3~5). — 증거: EditMode `LevelViewTests.Build_Level_OneVisualPerShapeIdWithMatchingPose`(stage01~05), PlayMode `StageSceneTests.Stage_Play_BuildsRequestedLevel`(stage01~05)
- [x] 각 스테이지의 클리어 봇이 성공한다 (Core 헤드리스 봇, 고정 시드 5개 중 4개 이상). — 증거: `ScenarioTests` stage01 5/5, stage02 4/5, stage03 4/5, stage04 4/5(스킬 구성, D-048), stage05 5/5. 발각 봇 stage01~05 5/5
- [x] 각 스테이지의 대표 캡처 4장(전경, 시작 위치, 인간 근접, Shadow Zone 내부)이 생성된다. — 증거: `Captures/2026-10-01_165113/Stage_stage03~05_*.png`(각 4장 + 1인칭)

### 표현 (spec/06, spec/08, spec/11)
- [x] 중독 게이지는 중독 > 0일 때만 보이고 단계 아이콘이 맞다 (Unity, spec/08). — 증거: EditMode `HudTests.ToxinGauge_OnlyWhenPoisoned_TierIconMatches`
- [x] 바람 영역 안에서 CO₂ 흐름이 바람 방향으로 흩어진다 (캡처 검토: Stage 3, spec/11). — 증거: `Captures/2026-10-01_165113/Gas_co2_wind_stage03.png`(옅지만 바람 방향 +Z로 기울어 흩어짐), EditMode `SensesViewTests.Co2Plume_InWind_DriftsWithWindAndDispersesSooner`
