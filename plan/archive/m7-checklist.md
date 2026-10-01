# M7 거실 버티컬 슬라이스 (Stage 1·2) — 체크리스트 (보관)

태그 `m7-done` (2026-10-01). 이월: DripSource 높이 검사의 Stage 3~5 적용 → M9 (D-036), CO₂ 바람 흩어짐 → M9(선풍기), 중독 게이지·액티브 스킬 HUD → M9·M8 (D-041).

### 레벨 데이터 (spec/07, tech/architecture §5)
- [x] `livingRoom` 방 데이터가 표의 좌표/크기를 ±5u 안에서 따르고, Stage 1·2가 같은 방 데이터를 참조한다 (Core: 레벨 데이터 검사). — 증거: `LevelDataTests.LivingRoom_BoxFurniture_MatchesSpecTableWithin5u`(8개 가구), `LivingRoom_RoundFurniture_MatchesSpecTableWithin5u`, `LivingRoom_ShadowZones_UnderTableBehindShelfBehindCurtain`, `Stage1And2_ReferenceTheSameLivingRoom` (D-037 커튼 조정)
- [x] Stage 1·2가 공통 규칙의 레벨 데이터 검사를 통과한다 (Core). (Stage 3~5는 M9) — 증거: `LevelDataTests.Level_PassesCommonLevelChecks`(시작 위치·소음·DripSource·인간 데이터·도망칠 곳·가구 플래그), `Validator_SpawnInsideYellowZone_Reported`. 경로 규칙은 클리어 봇 `stage01_clear`·`stage02_clear`가 숨을 곳을 경유해 SkinSite에 도달해 증명
- [x] `glass` 형상은 충돌은 하지만 시야를 막지 않는다 (Core). — 증거: `LevelDataTests.Glass_BlocksMovementButNotLineOfSight`
- [x] 레벨·방 데이터가 `data/schema`의 JSON Schema를 만족한다 (Core, tech/verification §1). — 증거: `LevelDataTests.DataFiles_MatchJsonSchemas`, `Schema_RejectsInvalidLevel`
- [x] (M4 이월, D-034) 레벨 데이터의 모든 가구 형상에 obstacle 플래그가 있다 (Core: 레벨 데이터 검사). — 증거: `LevelDataTests.Level_AllFurnitureHasObstacleFlag`(stage01·02), `Validator_FurnitureWithoutObstacleFlag_Reported`
- [ ] (M6 이월, D-036) 모든 레벨의 DripSource가 착지면 기준 150u 이상이다 (Core: 레벨 데이터 검사). (Stage 1·2는 발생원 없음, Stage 3~5는 M9에서 같은 검사) — 진행: `LevelDataTests.Level_DripSourcesAreHighEnough`(stage01·02) + `LevelValidator` 포함. Stage 3~5 데이터가 생기는 M9에서 같은 테스트로 체크
- [x] Unity 씬의 시각 오브젝트가 레벨 데이터의 모든 형상 ID와 1:1로 대응한다 (Unity). — 증거: EditMode `LevelViewTests.Build_Level_OneVisualPerShapeIdWithMatchingPose`(stage01·02), PlayMode `StageSceneTests.Stage_Play_BuildsRequestedLevel`(stage01·02)

### 인간 수정자 · 행동 (spec/06, spec/07)
- [x] 졸기 중에는 Red Zone에 들어가도 시각 감지와 박수 공격이 없다 (Core). — 증거: `HumanModifierTests.Doze_SleepingInRedZone_NoVisionAndNoClap`
- [x] 졸음 주기가 tuning 범위를 따르고, 깨기 0.5초 전에 예고 이벤트가 나온다 (Core). — 증거: `HumanModifierTests.Doze_CycleFollowsTuningRanges_WakeTelegraph05SecondsBefore`(150초, 10회 이상 전환), `Doze_AwarenessAtSuspicion_FullyWakes_ThenResleepsAfter5CalmSeconds`, `Doze_Sleeping_HearingAndReactionsHalved`
- [x] 졸음 수정자 아래의 광분 최소 유지 시간이 0.5배이다 (Core). — 증거: `HumanModifierTests.Doze_FrenzyMinimumHalved_CalmsSoonerThanAwakeHuman`
- [x] Stage 2 인간이 Safe에서 6~10초마다 좌 또는 우로 60° 2초간 둘러본다 (Core). — 증거: `HumanModifierTests.Stage2_Safe_GlancesSideways60DegreesFor2SecondsEvery6To10Seconds`

### 모기 감각 (spec/11)
- [x] 스냅샷에 호흡 위상, 날숨 위치·세기, 바람 영역, 피부 목록·자국 여부, 은신처 목록, InShadow·PlayerVisibleToHuman이 포함된다 (Core). — 증거: `HumanModifierTests.Snapshot_ContainsSensesSourceData`
- [x] 호흡 주기와 날숨 구간이 tuning 값을 따르고, 취한 타겟의 세기가 1.6배이다 (Core). (취한 타겟 수정자는 M9, M7에서는 세기 배율 경로만) — 증거: `HumanModifierTests.Breathing_PeriodAndExhaleFollowTuning_DrunkStrength16x`
- [x] clearRange 안의 물체는 흐림이 0이고, fogFullRange 밖은 최대 흐림이다 (Unity: 셰이더 파라미터 검사 + 캡처). — 증거: EditMode `SensesFogTests.Amount_InsideClearRange_IsZero_BeyondFullRange_IsMax`, `ClearRange_InSteam_ScaledBySteamMultiplier`, `Apply_SetsGlobalShaderParameters_FromPlayerOrigin`, `PcRenderer_HasFogPassBeforeTransparents_WithCompilingShader`, 캡처 `Captures/2026-10-01_145312/Stage_stage01_start.png` (D-039)
- [x] CO₂ 흐름이 최대 흐림 거리 밖에서도 보인다 (캡처 검토: Stage 1 시작 위치 1장). — 증거: `Captures/2026-10-01_150001/Stage_stage01_start.png`(머리 위 연기, 약 300u), EditMode `SensesViewTests.Co2_PuffsDuringExhale_VisibleBeyondFullFogUpToCo2Range`, `Co2Plume_RisesFadesAndExpires_StrongerBreathIsLarger` (D-040)
- [x] 체온 표시가 heatRange 안에서만 나타나고, 자국 부위에 표시가 붙는다 (Unity). — 증거: EditMode `SensesViewTests.Heat_OnlyInsideHeatRange_StrongerWhenCloser`, `BiteMark_DotOnlyOnMarkedSite_InsideHeatRange`, 캡처 `Stage_stage01_human_close.png`
- [x] 은신처 표시 강도가 어그로 상태에 따라 3단계로 바뀐다 (Unity). — 증거: EditMode `SensesViewTests.ShadowCue_IntensityStepsWithAwarenessState_HiddenBeyondCueRange`

### HUD · 튜토리얼 (spec/08 중 HUD 부분)
- [x] HUD 요소가 각 모델 값의 변화에 반영된다 (Unity: 값 주입 후 UI 상태 확인). — 증거: EditMode `HudTests.ModelValues_AreReflectedInHudElements`, `Satiety_HighlightedOnlyWhenSpeedMultiplierBelowThreshold`, `Prompts_FollowPlayerStateAndInputDevice`, Core `HudDataTests`(5개) (D-041)
- [x] 인간 머리가 화면 밖일 때만 방향 화살표가 보인다 (Unity). — 증거: `HudTests.HeadArrow_VisibleOnlyWhenHeadOffscreen`, `EdgeMarkers_StayInsideSafeBandAvoidingTopAndBottomHud`
- [x] 화면 밖 공격 예고 시 해당 방향 가장자리 경고와 경고음이 나온다 (Unity). — 증거: `HudTests.OffscreenTelegraph_EdgeWarningAndSoundOnStart`
- [x] 광분 중에만 은신처 방향 표시가 나온다 (Unity). — 증거: `HudTests.HidingDirection_OnlyDuringFrenzy`
- [x] 튜토리얼 안내가 행동 이벤트로 순서대로 진행되고, 설정으로 끌 수 있다 (Unity). — 증거: Core `TutorialTests`(4개), EditMode `HudTests.TutorialHints_FollowTrackerSteps_AndCanBeTurnedOff`, `TutorialHints_SettingPersistsInStore_AndEveryLevelStepHasText`, PlayMode `StageSceneTests`(첫 안내 표시) (D-042)
- [x] 1920×1080, 1280×720, 2560×1440에서 HUD 요소가 화면 밖으로 나가거나 겹치지 않는다 (캡처 3장 검토). — 증거: `Captures/2026-10-01_152002/Hud_*.png`

### 봇 · 캡처 · 리포트
- [x] Stage 1·2 클리어 봇이 고정 시드 5개 중 4개 이상 성공한다 (Core, tech/verification §3). — 증거: `ScenarioTests.Scenario_MeetsExpectationOnEnoughSeeds("stage01_clear")` 5/5, `("stage02_clear")` 4/5 (D-038)
- [x] Stage 1·2 발각 봇이 Red Zone → PlayerDied(Attack)에 도달한다 (Core). — 증거: `ScenarioTests.Scenario_MeetsExpectationOnEnoughSeeds("stage01_detect")` 5/5(졸다가 귀 소음으로 깨어 박수), `("stage02_detect")` 5/5
- [x] Stage 1·2 대표 캡처 4장(전경, 시작 위치, 인간 근접, Shadow Zone 내부)을 검토했다. — 증거: `Captures/2026-10-01_152614/Stage_stage01_*.png`, `Stage_stage02_*.png`(각 4장 + 시작 위치 1인칭). **결함 수정**: 씬 카메라의 URP 후처리가 꺼져 있어 은신 비네트가 화면에 나오지 않았음 → 켜고 `SceneIntegrityTests.MainCamera_RendersPostProcessing_ForShadowVignette` 추가, 은신처·증기 볼륨을 양면 렌더링(안에서도 푸른빛)
- [x] `plan/progress.md`에 버티컬 슬라이스 리포트를 작성했다 (사람 검토 권장 시점). — 아래 "M7 버티컬 슬라이스 리포트"
