# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M8 화면 흐름 · 스킬 트리 · 저장** (진행 중)
- 다음 할 일: CO₂·증기 기체형 교체(사람 요청) → M8 종료
- 브랜치: `milestone/m8-flow-skills`

## 현재 마일스톤 체크리스트 (M8)
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
- [ ] CO₂·증기 표현을 기체형(소프트 파티클/볼륨 안개)으로 교체 (캡처 검토). M9 시작 전

## M7 버티컬 슬라이스 리포트 (2026-10-01, 사람 검토 권장)
**할 수 있는 것:** Unity 에디터에서 `Assets/_Project/Scenes/Stage.unity`를 열고 Play하면 Stage 1(조는 인간)이 시작된다. `StageBootstrap.RequestedLevelId`로 stage02를 고를 수 있으며, 화면 흐름(타이틀·스테이지 선택)은 M8에서 붙인다. 빌드 실행 파일은 아직 Boot 씬만 연다.
- 비행·대시·정밀 비행·착지·흡혈·이탈·은신, 물방울, 인간 감지·광분·공격, 물린 자국·포만·승패가 Core 규칙대로 동작한다 (Core 265개 테스트).
- 모기 감각: 플레이어 기준 흐린 시야(80u 선명 → 250u 최대 흐림, 실루엣 유지), 분홍빛 CO₂ 연기(450u까지 보임), 피부 체온 빛과 물린 자국 점, 은신처 푸른빛(경계 3단계 강도).
- HUD: 흡혈 게이지·포만·자국, 경계 눈 아이콘(가려짐/은신, 광분 남은 시간·진정 링), 경계 비네트, 머리 방향 화살표, 화면 밖 공격 경고와 경고음, 광분 중 은신처 방향, 스태미나(대시 비용 눈금·탈진·젖은 날개), 습기, 가려움 링, 조준점, 상호작용 프롬프트(키보드/게임패드 표기), 튜토리얼 안내(설정으로 끄기).

**봇 결과 (고정 시드 5개):** stage01_clear 5/5, stage02_clear 4/5, stage01_detect 5/5, stage02_detect 5/5 (D-038). 클리어 경로는 천장 아래로 머리 뒤에 접근해 오른 팔뚝을 2~3회 나눠 빠는 방식이다.

**캡처:** `Captures/2026-10-01_152614/` (Stage 대표 캡처, HUD 3해상도는 `2026-10-01_152002/`).

**사람이 봐 주면 좋은 것:**
1. 밸런스(D-035): 자국 5개면 인간이 의심 상태에서 내려오지 않아 사실상 막힌다. 봇도 자국 3개(하한 24 > 의심 이탈 20)를 피하려고 Stage 2를 2회 세션으로 끝낸다. 플레이 감각상 `biteMark.floorPerBite`(8)를 낮출지 판단이 필요하다.
2. 흐린 시야 세기(`senses.fogMaxDensity` 0.75, 블러 3px)가 길 찾기에 충분한지.
3. 한글 글꼴(D-041): OS 글꼴 사용 중. OFL 글꼴 허용 여부.
4. 표현은 화이트박스(프리미티브·반투명 볼륨)이며 아트는 M10에서 교체한다.

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트: `plan/archive/m0-checklist.md`
- **M1 Core 충돌 월드 · 비행 · 카메라** — 태그 `m1-done` (2026-10-01). 체크리스트: `plan/archive/m1-checklist.md`. 이월: 부착 시선 제한 → M4 (D-028)
- **M2 인간 감지 · 어그로 · 광분** — 태그 `m2-done` (2026-10-01). 체크리스트: `plan/archive/m2-checklist.md`
- **M3 공격 · 반응 · 무작위 움직임 · 사망** — 태그 `m3-done` (2026-10-01). 체크리스트: `plan/archive/m3-checklist.md`. 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031)
- **M4 스텔스** — 태그 `m4-done` (2026-10-01). 체크리스트: `plan/archive/m4-checklist.md`. 이월: 젖은 날개·습기 2배 회복 → M6, 중독 → M9, 레벨 데이터 obstacle 검사 → M7 (D-034)
- **M5 흡혈 세션 · 물린 자국 · 포만 · 승리** — 태그 `m5-done` (2026-10-01). 체크리스트: `plan/archive/m5-checklist.md`
- **M6 물방울 QTE · 습기** — 태그 `m6-done` (2026-10-01). 체크리스트: `plan/archive/m6-checklist.md`. 이월: DripSource 레벨 적용 → M7
- **M7 거실 버티컬 슬라이스 (Stage 1·2)** — 태그 `m7-done` (2026-10-01). 체크리스트: `plan/archive/m7-checklist.md`. 이월: DripSource 검사 Stage 3~5 적용·CO₂ 바람 흩어짐·중독 게이지 → M9, 액티브 스킬 HUD → M8

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| 2026-10-01 | M8 | HUD 액티브 스킬 칸 테스트, 겹눈 선명 범위 테스트, 미끼 월드 표시 | Core 300 + EditMode 80 + PlayMode 9 통과 | (이 커밋) |
| 2026-10-01 | M8 | 화면 흐름: GameSession·ScreenFlow·SceneNavigator, Title·StageSelect·Skills·Settings·Confirm·Stage(Pause/Result)·Ending·Boot, FileSaveStorage, UserSettings, Pause(틱·입력 정지), HUD 액티브 스킬 칸, 메뉴 캡처, D-044. **결함 수정**: 씬 생성기 메서드 손실 복구, 메뉴 글자 누락, 행 레이아웃 | Core 300 + EditMode 77 + PlayMode 9 통과, 빌드 경고 0, 캡처 검토 | 42912f0 |
| 2026-10-01 | M8 | Core 메타: SkillCatalog·SkillLoadout·SkillEffects(스킬 반영 Tuning), 연속 와류, 미끼(DecoySystem), 보상, SaveData·직렬화·SaveStore·SkillShop, D-043 | Core 300/300 통과 | 16446af |
| 2026-10-01 | M7 | 카메라 URP 후처리 켜기(은신 비네트 미표시 결함), 은신처·증기 양면 렌더링, 씬 무결성 테스트, 대표 캡처 검토, 버티컬 슬라이스 리포트. M7 종료 | Core 265 + EditMode 66 + PlayMode 6 통과, 빌드 경고 0, 캡처 검토 | (이 커밋) |
| 2026-10-01 | M7 | HUD(HudState·HudView·HudPresenter·HudController, 절차 스프라이트·경고음, OS 한글 글꼴), 튜토리얼(Core TutorialTracker, TutorialHints·설정), HUD 3해상도 캡처, D-041·D-042. **결함 수정**: 가장자리 표시 겹침·해상도별 위치, 누락 스크립트 | Core 265 + EditMode 64 + PlayMode 6 통과, 빌드 경고 0, 캡처 검토 | 977d12b |
| 2026-10-01 | M7 | HUD 원천 데이터(Core): PlayerOccluded(가려짐), FrenzyMinRemaining·CalmProgress, CanAttach, DashCost, 스냅샷 포함 | Core 261/261 통과 | 6833ed6 |
| 2026-10-01 | M7 | 감각 큐: Co2Plume·SenseCueModel·SensesView(CO₂·체온·자국·은신처 강도), 머티리얼 3개, tuning 키 11개, 캡처에 감각 반영, D-040 | Core 256 + EditMode 51 + PlayMode 6 통과, 캡처 검토 | 6ecbce1 |
| 2026-10-01 | M7 | 흐린 시야: SensesSettings·FogModel·SensesFog, 깊이 기반 안개+블러 셰이더, PC_Renderer 전체 화면 패스, tuning 키 2개, D-039 | Core 256 + EditMode 46 + PlayMode 6 통과, 캡처 검토 | 0159a69 |
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 사람 검토 권장 (막힘 아님)
- **한글 글꼴(D-041):** 허용 라이선스 목록에 OFL이 없어 한글 글꼴 파일을 넣지 못하고 OS 글꼴(맑은 고딕)을 실행 중에 씁니다. OFL(예: Pretendard, Noto Sans KR)을 허용 목록에 추가하면 M10에서 번들 글꼴 + TextMeshPro로 바꿀 수 있습니다.
- **밸런스(D-035):** 설계 검증 봇 기준으로 자국 5개(하한 40 = 의심 진입선)가 되면 인간이 영구 의심 상태로 몸 주변을 훑어 접근이 거의 불가능하다. 긴 세션 전략도 40%가 자국 5개에서 멈춘다. 수치(`biteMark.floorPerBite` 8, `floorMax` 45, 반응률)는 spec 제안값 그대로 두었으며, M7 버티컬 슬라이스 리포트에서 플레이 감각과 함께 검토를 요청한다.

## 막힘
(없음)

## 캡처 검토 기록
- 2026-10-01 M8 `Captures/2026-10-01_155910/` 메뉴: Title, Title+설정(9행), StageSelect(Stage 1 기록, 2 열림, 3~5 준비 중), Skills(능력치 탭, 비용·레벨), Ending. 한글·배치 정상. **결함 수정**: 첫 캡처에서 글자가 모두 빠짐(캔버스 평면이 근평면에 너무 가까움 + 씬을 다시 열면 동적 글꼴 텍스처가 비워짐), 행 높이가 기본값 100으로 잡혀 목록이 패널 밖으로 넘침 → 고정 크기 행·위쪽 기준 목록·씬별 글꼴
- 2026-10-01 M7 최종 `Captures/2026-10-01_152614/` Stage 1·2 대표: 전경(흐림 속 가구 실루엣·CO₂), 시작 위치 3인칭·1인칭, 인간 근접(체온 빛·자국 점), Shadow Zone 내부(푸른빛 + 은신 비네트). 마젠타 없음
- 2026-10-01 M7 `Captures/2026-10-01_152002/` HUD 3해상도: 모든 요소를 동시에 켠 상태(광분 4.3초·진정 링, 가려짐, 흡혈 62%·포만 강조·자국 2, 가려움 링·조준점, 젖은 날개 3.4s, 습기, 머리 화살표, 은신처 방향, 공격 경고, 튜토리얼 문구). 세 해상도에서 같은 비율 배치, 화면 밖·겹침 없음, 한글 정상. **결함 수정**: 첫 캡처(`151725`)에서 가장자리 표시가 하단 스태미나 문구와 겹치고 해상도마다 위치가 달랐음 → 세로 0.25~0.75 띠 제한 + 뷰포트 앵커 배치. 빌드에서 `HudAudioSource` 누락 스크립트 경고(파일명 불일치) → 파일 분리 + 씬 누락 스크립트 검사 테스트 추가
- 2026-10-01 M7 `Captures/2026-10-01_150001/` Stage 감각 큐: 시작 위치에서 최대 흐림 너머 머리 위 CO₂ 연기(분홍) 보임, 인간 근접에서 오른 전완 체온 빛과 붉은 자국 점, 종아리 체온 빛. 체온 빛이 희게 보여 색을 더 따뜻하게 조정. 마젠타 없음
- 2026-10-01 M7 `Captures/2026-10-01_145312/` Stage 흐린 시야: 시작 위치에서 가까운 커튼은 선명, 소파·인간·책장은 흐림과 블러, 최대 흐림에서도 가구 실루엣 유지. 은신처 표시(투명)는 안개 뒤에 그려져 선명. 마젠타 없음
- 2026-10-01 M7(중간) `Captures/2026-10-01_144646/` Stage_stage01·02: 전경(거실 가구·커튼·책장·화분·에어컨·소파 위 캡슐 인간), 시작 위치 3인칭·1인칭, 인간 근접(오른 전완), Shadow Zone 내부(테이블 아래). 은신처 볼륨은 반투명 어두운 표시, 마젠타 없음. 감각 표현 적용 전이라 최종 검토는 감각·HUD 뒤에 다시 찍는다
- 2026-10-01 M6 `Captures/2026-10-01_141811/` Sandbox_Water: 물방울(지름 3u)이 플레이어 위에서 낙하, 갇힌 플레이어가 물방울과 함께 세면대로 낙하(남은 높이 36.4u). 마젠타 없음. 첫 캡처에서 갇히지 않은 것은 물방울이 틱당 7u를 떨어지며 플레이어를 건너뛴 결함 → 선분 거리 판정으로 수정, 높이별 회귀 테스트 추가
- 2026-10-01 M2 `Captures/2026-10-01_133118/` Sandbox_Human: 평온(머리 초록, 시선 패턴 yaw 11°), 대시 소음 뒤 의심(머리 노랑, 소리 쪽 뒤-오른쪽으로 돌아 yawLimit 100°에서 멈춤), 광분 손바닥 예고(머리 빨강, 플레이어 위치에 주황 예고 표시 반경 12u). 마젠타 없음. 캡슐 인간 비율·소파 배치 정상. 첫 캡처에서 광분 장면 예고가 안 뜬 것은 머리가 뒤를 보고 있어 시야 밖이었기 때문(정상 동작) → 캡처 시나리오에서 머리를 정면으로 되돌림
- 2026-10-01 M1 `Captures/2026-10-01_131326/`, `2026-10-01_131432/` Sandbox_Flight: 3인칭 개요(가구·벽·플레이어 구 정상), 1인칭 벽 접촉 정면·비스듬히(벽면과 방 안쪽만 보임, 벽 뒤 노출 없음), 3인칭 벽 접촉(카메라가 벽 안쪽 유지). 마젠타 없음. 첫 캡처에서 플레이어 색이 빠진 문제(MaterialPropertyBlock 미저장) → 머티리얼 에셋 `Whitebox_Player.mat`으로 수정 후 재확인
- 2026-10-01 M0 `Captures/2026-10-01_123452/Boot.png`: 템플릿 빈 씬(하늘·바닥). 마젠타 없음, 템플릿 볼륨의 피사계 심도로 전체가 흐림 — 표현 작업(M1 이후)에서 볼륨 프로파일 정리 필요

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M7 | 단일 Stage 씬(StageBootstrap·LevelView·LevelMaterials), 반투명 레벨 머티리얼, 빌드 설정 등록, Stage 포즈 캡처 | Core 256 + EditMode 42 + PlayMode 6 통과, 캡처 검토 | 47bc1e6 |
| 2026-10-01 | M7 | 시나리오 포맷·ScenarioRunner·BotPilot, Stage 1·2 클리어/발각 시나리오, D-038. 봇 결함 수정(도착 판정, 이웃 부위 부착), 경로를 머리 뒤로 | Core 256/256 통과, Unity CS 이슈 0 | 78d7216 |
| 2026-10-01 | M7 | 졸음 수정자(DozeSystem), Stage 2 둘러보기(IdleGlance), 호흡(BreathSystem), 감각 스냅샷(호흡·부위 위치·은신처·바람 영역), HumanTraits | Core 251/251 통과, Unity CS 이슈 0 | adc0b05 |
| 2026-10-01 | M7 | 브랜치·체크리스트, 레벨 데이터 계층(JsonAccess, Shape·Room·Level 정의, HumanDataParser, LevelLoader, LevelValidator), 거실·stage01·stage02 데이터, JSON Schema, glass(ShapeFlags.Solid), D-037 | Core 243/243 통과, Unity CS 이슈 0 | c0bb672 |
| 2026-10-01 | M6 | 브랜치·체크리스트, D-036, WaterSystem(발생·Trapped·탈출·WaterImpact), HumiditySystem(습기·증기·젖은 날개), 대시 비용·회복 배율, LevelChecks, 스냅샷 확장, WaterView·Sandbox_Water·캡처. **결함 수정**: 물방울 포획 터널링(선분 판정). M6 종료 | Core 220 + EditMode 40 + PlayMode 4 통과, 빌드 성공, 캡처 검토 | 2613c44 |
| 2026-10-01 | M5 | 설계 검증 시나리오(SessionStrategyBot, Shadow Zone 무대, 실패=제한 시간), D-035, 밸런스 검토 권장 기록 | Core 204 + EditMode 40 + PlayMode 3 통과, 빌드 성공. M5 종료 | 6119aeb |
| 2026-10-01 | M5 | 브랜치·체크리스트, SuckSystem(세션 가속·가려움·자국·포만), 자국 경계 보정, StageOutcome·StageCleared, 스냅샷에 게이지·결과 추가 | Core 203/203 통과, Unity CS 이슈 0 | fcb7377 |
| 2026-10-01 | M4 | ShadowVignette(URP Volume), 1인칭 부착 시선 제한·카메라 up(D-028 해소), 샌드박스 탁자 밑 Shadow Zone, run-tests가 결과 없을 때도 컴파일 오류 출력. M4 종료 | Core 178 + EditMode 40 + PlayMode 3 통과, 빌드 성공 | f644e2f |
| 2026-10-01 | M4 | 브랜치·체크리스트, D-034, Shadow Zone 판정(매 틱 IsHidden), 숨김 테스트를 실제 볼륨으로 전환, StealthTests | Core 178/178 통과 | cb64102 |
| 2026-10-01 | M3 | 무작위 동작(HumanActionDefinition, HumanMotionSystem, 절차적 포즈), 동작이 플레이어 이동보다 먼저 실행, 튕겨남 검증, 샌드박스 인간 동작 4종, D-033. M3 종료 | Core 172 + EditMode 36 + PlayMode 3 통과, 빌드 성공 | 5581fec |
| 2026-10-01 | M3 | 부착·이탈(SurfaceAnchor, AttachSystem), 착지·부착 중·귀 반응(ReactionSystem), 튕겨남 경직 흐름, D-032. **결함 수정**: 캡슐 광선 교차가 멀어지는 광선을 거리 0 충돌로 판정(M1부터) → 수정, 교차 검증에 무작위 방향 추가(변조 시 4개 실패 확인) | Core 166/166 통과 | e4800ad |
| 2026-10-01 | M3 | 브랜치·체크리스트, 재시도 구조(SimulationSetup 월드 팩토리, Retry), SkinSite 유형·부위 상태, 반응·부착·튕겨남 설정 DTO, DeathCause.WaterImpact, tuning `attach.detachOffset`, D-031 | Core 142/142 통과, Unity CS 이슈 0 | 2a998d9 |
| 2026-10-01 | M2 | HumanView·SandboxHumanWorld·Bootstrap, 씬 빌더 공용 골격, 인간 상태 캡처, run-tests에 컴파일 경고 검사 추가. M2 종료 | Core 138 + EditMode 36 + PlayMode 3 통과, 빌드 성공, 캡처 검토 | 86fcab3 |
| 2026-10-01 | M2 | 브랜치·체크리스트, 인간 엔티티(몸 캡슐·머리·귀), 시각·청각 센서, 경계·상태머신·머리 행동, 광분, 공격(박수·손바닥·맹목), 난수 스트림, 스냅샷, tuning 추가(D-029), D-030. PlayMode 테스트의 사용 중단 API 경고 수정 | Core 138/138 통과, Unity CS 이슈 0 | a17ef4f |
| 2026-10-01 | M1 | WorldView(화이트박스), SandboxFlightWorld·Bootstrap, SandboxSceneBuilder·build-sandboxes, CaptureTool 샌드박스 포즈, PlayMode 씬 스모크. M1 종료 | Core 111 + EditMode 36 + PlayMode 2 통과, 빌드 성공(예상 외 경고 0), 캡처 검토 | 51834c3 |
| 2026-10-01 | M1 | SimulationClock·SimulationDriver·SimulationRunner, 카메라(CameraPoseSolver·CameraController·CameraRig·LookConstraint·PlayerViewVisibility), 설정 저장 포트, D-027·D-028 | EditMode 34/34 통과 | 2824119 |
| 2026-10-01 | M1 | 입력 에셋 MoquiControls(Gameplay 맵, spec/01 표), LookState, CommandCollector(edge 래치, 마우스·스틱 분리) | EditMode 19/19 통과 | cdbf11d |
| 2026-10-01 | M1 | 대시(DashSystem, DashDirectionResolver), 스태미나·탈진, 바람 외력, 낙하체 중력, D-026 | Core 111/111 통과, Unity CS 이슈 0 | f11b541 |
| 2026-10-01 | M1 | SphereMover(sweep·미끄러짐·밀어내기)를 시뮬레이션 이동에 연결 | Core 94/94 통과, Unity CS 이슈 0 | a94441c |
| 2026-10-01 | M1 | GameSimulation(60Hz), PlayerCommand, GameSettings, CameraBasis, FlightSystem(약한 관성) | Core 63/63 통과, Unity CS 이슈 0 | bb0fa66 |
| 2026-10-01 | M1 | 브랜치 생성, M1 체크리스트 복사, Core 충돌 월드(CollisionShape, ShapeGeometry, CollisionWorld) | Core 54/54 통과 (충돌 19 포함) | aa7f77d |
| 2026-10-01 | M0 | Unity 어댑터 asmdef, UnityDataSource, DataSync·BuildScript·CaptureTool, Tools/run-tests·build·capture, 씬·입력 에셋 이동, D-024·D-025. M0 종료 | Core 35 + EditMode 1 + PlayMode 1 통과, 빌드 성공(예상 외 경고 0), 캡처 1장 | e8dbd7a |
| 2026-10-01 | M0 | Core 데이터 계층(IDataSource, JsonReader, Tuning, TuningLoader), `data/tuning.json`, spec 문서 일치 검사 | Core 35/35 통과, Unity CS 이슈 0 | 2b265df |
| 2026-10-01 | M0 | 필수 패키지 설치, `Tools/unity-path.ps1`·`unity-import.ps1`, D-023 | unity-import exit 0, CS 이슈 0 | fd40a00 |
| 2026-10-01 | M0 | URP 프로젝트 생성(스크래치패드 생성 후 루트로 이동, 템플릿 튜토리얼 제거), 라이선스 배치 모드 확인 | Moqui.Core Unity 컴파일 CS 0건 | 28a2f7b |
| 2026-10-01 | M0 | Unity 버전 확정(D-019), Windows 기준 재확인(D-022), 번들 .NET SDK 사용(D-021), Core 패키지 + dotnet sln + IRandom 샘플 테스트, `Tools/run-core-tests` | `SplitMix64RandomTests` 4/4 통과 (ps1·sh 양쪽) | d26f851 |
| 2026-10-01 | M0 | 환경 조사(Unity/dotnet/라이선스), M0 브랜치, .gitignore/.gitattributes, D-019·D-020 기록, 사람 요청 R-001~003 | 위 체크리스트 | 2d60090 |
