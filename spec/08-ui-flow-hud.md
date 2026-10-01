# 08. 화면 흐름 · HUD · 설정

## 목적
3D 공간에서 스태미나, 흡혈량, 인간의 경계 상태와 방향을 시선을 크게 옮기지 않고 읽을 수 있게 한다. (PRD §4 UI/UX의 제안 기본값, D-003)

## 화면 흐름
```
Title ─▶ Stage Select ─▶ Stage(Play) ─▶ Result ─┬▶ Skills ─▶ Stage Select
  │                          │                   └▶ Retry ─▶ Stage(Play)
  └▶ Settings                └▶ Pause ─▶ (Resume / Retry / Stage Select / Settings)
Stage 5 첫 클리어 ─▶ Ending ─▶ Title
```
- Stage Select: 이전 스테이지를 클리어해야 다음이 열린다. 클리어 기록(최고 시간, 광분 0회 여부, 최소 자국 수)을 표시한다. Stage Select에서도 Skills로 갈 수 있다.
- Skills: 스킬 트리(저항/능력치/연속 회피/마법봉/기타 탭), 현재 레벨과 다음 레벨 효과·비용, 액티브 장착 (spec/09).
- Result: 클리어/실패, 사망 원인, 소요 시간, 획득 혈액 포인트 내역(spec/09).
- Ending: 정지 이미지 1장 + 텍스트 3줄 + "Thanks for playing". 스킵 가능.
- 모든 화면은 마우스와 게임패드(방향키/스틱 + A/B)로 조작할 수 있다. 첫 선택 요소에 포커스를 둔다.

## HUD
| 요소 | 위치 | 내용 |
|---|---|---|
| 흡혈 게이지 | 상단 중앙 | 0~100% 붉은 가로 바 + 숫자 |
| 포만 표시 | 흡혈 게이지 끝 | 무게 아이콘. 감속 배율이 0.85 아래로 내려가면 강조 |
| 물린 자국 수 | 흡혈 게이지 옆 | 작은 점 n개 |
| 경계 아이콘 | 흡혈 게이지 아래 | 눈 아이콘. Safe 초록 / Suspicious 노랑(채움 = 경계값) / 광분 빨강 맥동 + 남은 최소 시간·진정 진행 링. 시야 차단 중이면 "가려짐", Shadow Zone이면 "은신" 표시 |
| 인간 방향 표시 | 화면 가장자리 | 인간 머리가 화면 밖일 때 그 방향의 가장자리에 화살표. 색은 경계 상태 |
| 경계 비네트 | 화면 테두리 | Suspicious 옅은 노랑, 광분 빨강 맥동 |
| 은신처 방향 | 화면 가장자리 | 광분 중 가장 가까운 Shadow Zone 방향 (spec/11 §4) |
| 스태미나 | 좌하단 | 바. 대시 비용 위치에 눈금. 탈진 시 회색, 젖은 날개 시 물방울 아이콘 + 남은 시간 |
| 가려움 링 | 화면 중앙 조준점 주변 | 흡혈 중에만 표시. 현재 부위의 가려움 |
| 중독 게이지 | 스태미나 위 | 중독 > 0일 때만 표시. 30/55/80 눈금, 현재 단계 아이콘(끊김/반전/랜덤), 모기향 하한 위치 표시 |
| 습기 게이지 | 중독 게이지 옆 | 습기 > 0일 때만 표시 |
| 액티브 스킬 | 우하단 | 장착 스킬 아이콘 + 쿨타임 |
| 상호작용 프롬프트 | 하단 중앙 | "F: 착지", "LMB 홀드: 흡혈", "Shift ×2!" 등. 입력 장치에 따라 표기 변경 |
| 공격 예고 | 월드 공간 + 화면 가장자리 | 판정 위치의 붉은 반투명 구, 경고음 `sfx_telegraph`. 판정 위치가 화면 밖이면 그 방향 가장자리가 붉게 번쩍임 (spec/02 §7) |

- 타겟 찾기는 화살표 대신 모기의 감각(CO₂ 흐름, 체온)으로 한다 (spec/11, D-009).

## 튜토리얼 안내 (Stage 1, 2)
- 화면 하단에 짧은 안내 문구를 순서대로 띄운다 (spec/07의 안내 순서). 각 안내는 해당 행동을 실제로 하면 완료되고 다음으로 넘어간다 (예: "Shift로 대시" → 대시 1회 실행).
- 안내 진행 판정은 Core 이벤트(대시 실행, 부착, 흡혈 시작, Shadow Zone 진입 등)로 한다.
- 이미 클리어한 스테이지를 다시 할 때는 안내를 끌 수 있다 (설정 "튜토리얼 안내").

## 설정
- 마우스 감도(0.25~4배), Y축 반전, 튜토리얼 안내 켜기/끄기, 기본 시점(3인칭/1인칭, spec/00), 마스터/효과음/음악 볼륨, 전체화면/창모드, 해상도.
- 게임 중 ToggleView로 시점을 바꾸면 "기본 시점" 설정도 함께 갱신된다.
- 설정은 Unity 레이어의 PlayerPrefs에 저장한다 (게임 진행 데이터가 아니므로 Core 저장소와 분리).

## 시점별 HUD 차이
- HUD 구성은 두 시점에서 같다.
- 1인칭에서는 조준점을 항상 표시한다. 3인칭에서는 흡혈 중에만 가려움 링과 함께 표시한다.
- 1인칭에서는 자기 캐릭터가 보이지 않으므로 젖은 날개 상태를 화면 가장자리 물방울 오버레이로도 보여준다.

## 수용 기준
- [x] 위 흐름의 모든 전이가 동작한다 (Unity: UI 흐름 테스트 — 버튼 이벤트를 직접 호출). — 증거: 버튼 이벤트를 직접 호출). — 증거: EditMode `FlowTests.Title_ButtonsLeadToStageSelectSettingsResetAndQuit`, `StageSelect_LockedStagesCannotStart_UnlockAfterClear`, `Result_RecordsAndSaves_RetrySkillsStageSelectAndEnding`, `SceneIntegrityTests.BuildSettings_ScreenScenesInFlowOrder` (D-044)
- [x] 잠긴 스테이지는 선택할 수 없다 (Unity). — 증거: `FlowTests.StageSelect_LockedStagesCannotStart_UnlockAfterClear`
- [x] 게임패드만으로 Title부터 Stage 1 시작까지 갈 수 있다 (Unity: 가상 Gamepad). — 증거: PlayMode `FlowPlayModeTests.GamepadOnly_TitleToStage1`
- [x] HUD 요소가 각 모델 값의 변화에 반영된다 (Unity: 값 주입 후 UI 상태 확인). — 증거: EditMode `HudTests.ModelValues_AreReflectedInHudElements`, `Satiety_HighlightedOnlyWhenSpeedMultiplierBelowThreshold`, `Prompts_FollowPlayerStateAndInputDevice`, Core `HudDataTests`(5개) (D-041)
- [x] 인간 머리가 화면 밖일 때만 방향 화살표가 보인다 (Unity). — 증거: `HudTests.HeadArrow_VisibleOnlyWhenHeadOffscreen`, `EdgeMarkers_StayInsideSafeBandAvoidingTopAndBottomHud`
- [x] 화면 밖 공격 예고 시 해당 방향 가장자리 경고와 경고음이 나온다 (Unity). — 증거: `HudTests.OffscreenTelegraph_EdgeWarningAndSoundOnStart`
- [x] 중독 게이지는 중독 > 0일 때만 보이고 단계 아이콘이 맞다 (Unity). — 증거: EditMode `HudTests.ToxinGauge_OnlyWhenPoisoned_TierIconMatches`
- [x] 광분 중에만 은신처 방향 표시가 나온다 (Unity). — 증거: `HudTests.HidingDirection_OnlyDuringFrenzy`
- [x] Skills 화면에서 구매와 액티브 장착이 동작하고 저장된다 (Unity). — 증거: `FlowTests.Skills_PurchaseAndEquip_AreSaved_AndCarriedIntoStage`
- [x] 튜토리얼 안내가 행동 이벤트로 순서대로 진행되고, 설정으로 끌 수 있다 (Unity). — 증거: Core `TutorialTests`(4개), EditMode `HudTests.TutorialHints_FollowTrackerSteps_AndCanBeTurnedOff`, `TutorialHints_SettingPersistsInStore_AndEveryLevelStepHasText`, PlayMode `StageSceneTests`(첫 안내 표시) (D-042)
- [x] Pause 중에는 Core 시뮬레이션 틱이 진행되지 않고 입력 커맨드가 전달되지 않는다 (Unity). — 증거: PlayMode `FlowPlayModeTests.Pause_StopsTicksAndDropsInput`
- [x] 설정 값이 재시작 후에도 유지된다 (Unity). — 증거: `FlowTests.Settings_ChangedThroughPanel_PersistInStore`, `Settings_SurvivePlayerPrefsRestart`
- [x] 1920×1080, 1280×720, 2560×1440에서 HUD 요소가 화면 밖으로 나가거나 겹치지 않는다 (캡처 3장 검토). — 증거: `Captures/2026-10-01_152002/Hud_*.png`

## 범위 외
- 키 리바인딩, 다국어, 자막
