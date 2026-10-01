# M8 화면 흐름 · 스킬 트리 · 저장 — 체크리스트 (보관)

태그 `m8-done` (2026-10-01). 이월: 해독 체질의 중독·모기향 적용 검증 → M9 (D-043), 음악 볼륨 연결 → M10 (D-044).

### 메타 성장 · 저장 (spec/09)
- [x] 보상 계산: 기본/광분 0회/신중한 흡혈/기준시간 조합 8가지가 맞다 (Core). — 증거: `MetaTests.Reward_AllEightCombinations`(8케이스)
- [x] 포인트가 부족하면 구매할 수 없고, 최대 레벨 이후 구매할 수 없다 (Core). — 증거: `MetaTests.Purchase_NeedsEnoughPoints_AndStopsAtMaxLevel`, `Costs_FollowTierTables`, `Equip_OnlyOwnedActiveSkills`
- [x] 모든 패시브 스킬 효과가 레벨별로 정확히 적용된다 (스킬당 1개 테스트) (Core). — 증거: `SkillTests.ResistSpray_*`, `ResistWet_*`, `ResistSatiety_*`, `SilentWings_*`, `SwiftWings_*`, `Stamina_*`, `FeatherLanding_*`, `NumbingSaliva_*`, `ShadowBlend_*`, `MagicWand_*`, `CompoundEyes_*`, `VortexControl_*` (D-043). 해독 체질의 중독·모기향 적용은 M9에서 기믹과 함께 검증
- [x] 와류 제어가 레벨당 가감속 시간을 0.8배로 줄이고, 3레벨에서만 대각선 대시가 가능하다 (Core). — 증거: `SkillTests.VortexControl_AccelTimesScaledPerLevel_DiagonalDashOnlyAtLevel3`
- [x] 연속 와류: 창 안의 추가 대시가 쿨타임을 무시하고 체인당 1회만 허용되며, 2레벨에서 스태미나가 줄지 않는다 (Core). — 증거: `SkillTests.ChainVortex_ExtraDashInsideWindowIgnoresCooldown_OncePerChain`, `ChainVortex_Level1CostsStamina_Level2Free`
- [x] 미끼가 지정 지점에서 지속시간 동안 NoiseEvent를 발생시키고, 인간의 마지막 자극 위치가 미끼로 바뀐다 (Core). — 증거: `SkillTests.Decoy_EmitsNoiseAtAimPointForDuration_LastStimulusMovesToDecoy`, `Decoy_NotEquipped_DoesNothing`
- [x] 저장 → 로드 왕복 후 모든 필드가 같다 (Core). — 증거: `MetaTests.SaveLoad_RoundTrip_AllFieldsEqual`
- [x] 손상된 save.json에서도 예외 없이 기본값으로 시작하고 손상 파일을 보존한다 (Core). — 증거: `MetaTests.CorruptSave_StartsWithDefaults_KeepsCorruptFile`(4케이스), `MissingSave_StartsWithDefaults_Reset_DeletesFile`
- [x] (M7 이월) 겹눈 각성 레벨에 따라 선명 범위가 늘어난다 (Unity, spec/11). — 증거: EditMode `SensesFogTests.CompoundEyes_ExtendsClearRangePerLevel` (Stage는 스킬 반영 Tuning으로 SensesSettings를 만든다, D-043)

### 화면 흐름 · 설정 (spec/08)
- [x] 위 흐름의 모든 전이가 동작한다 (Unity: UI 흐름 테스트 — 버튼 이벤트를 직접 호출). — 증거: EditMode `FlowTests.Title_ButtonsLeadToStageSelectSettingsResetAndQuit`, `StageSelect_LockedStagesCannotStart_UnlockAfterClear`, `Result_RecordsAndSaves_RetrySkillsStageSelectAndEnding`, `SceneIntegrityTests.BuildSettings_ScreenScenesInFlowOrder` (D-044)
- [x] 잠긴 스테이지는 선택할 수 없다 (Unity). — 증거: `FlowTests.StageSelect_LockedStagesCannotStart_UnlockAfterClear`
- [x] 게임패드만으로 Title부터 Stage 1 시작까지 갈 수 있다 (Unity: 가상 Gamepad). — 증거: PlayMode `FlowPlayModeTests.GamepadOnly_TitleToStage1`
- [x] Skills 화면에서 구매와 액티브 장착이 동작하고 저장된다 (Unity). — 증거: `FlowTests.Skills_PurchaseAndEquip_AreSaved_AndCarriedIntoStage`
- [x] Pause 중에는 Core 시뮬레이션 틱이 진행되지 않고 입력 커맨드가 전달되지 않는다 (Unity). — 증거: PlayMode `FlowPlayModeTests.Pause_StopsTicksAndDropsInput`
- [x] 설정 값이 재시작 후에도 유지된다 (Unity). — 증거: `FlowTests.Settings_ChangedThroughPanel_PersistInStore`, `Settings_SurvivePlayerPrefsRestart`
- [x] (M7 이월) HUD 액티브 스킬 칸(아이콘 + 쿨타임) (Unity). — 증거: `HudTests.ActiveSkill_SlotShowsOnlyWhenEquipped_WithCooldown`, 미끼 월드 표시 `SensesViewTests.Decoy_MarkerShownAtDecoyWhileActive`

### 사람 요청
- [x] CO₂·증기 표현을 기체형(소프트 파티클/볼륨 안개)으로 교체 (캡처 검토). M9 시작 전 — 증거: `Captures/2026-10-01_160546/Gas_co2_close.png`, `Gas_steam_outside.png`, `Gas_steam_inside.png`, EditMode `GasRenderingTests.GasMaterial_UsesGasShader_WithoutErrors`(2), `SensesViewTests.Co2_*`(쿼드 빌보드) (D-045)
