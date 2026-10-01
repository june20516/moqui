# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M7 거실 버티컬 슬라이스 (Stage 1·2)** (진행 중)
- 다음 할 일: Unity — HUD(spec/08), 튜토리얼, 최종 캡처, 버티컬 슬라이스 리포트
- 브랜치: `milestone/m7-living-room`

## 현재 마일스톤 체크리스트 (M7)
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
- [ ] HUD 요소가 각 모델 값의 변화에 반영된다 (Unity: 값 주입 후 UI 상태 확인).
- [ ] 인간 머리가 화면 밖일 때만 방향 화살표가 보인다 (Unity).
- [ ] 화면 밖 공격 예고 시 해당 방향 가장자리 경고와 경고음이 나온다 (Unity).
- [ ] 광분 중에만 은신처 방향 표시가 나온다 (Unity).
- [ ] 튜토리얼 안내가 행동 이벤트로 순서대로 진행되고, 설정으로 끌 수 있다 (Unity).
- [ ] 1920×1080, 1280×720, 2560×1440에서 HUD 요소가 화면 밖으로 나가거나 겹치지 않는다 (캡처 3장 검토).

### 봇 · 캡처 · 리포트
- [x] Stage 1·2 클리어 봇이 고정 시드 5개 중 4개 이상 성공한다 (Core, tech/verification §3). — 증거: `ScenarioTests.Scenario_MeetsExpectationOnEnoughSeeds("stage01_clear")` 5/5, `("stage02_clear")` 4/5 (D-038)
- [x] Stage 1·2 발각 봇이 Red Zone → PlayerDied(Attack)에 도달한다 (Core). — 증거: `ScenarioTests.Scenario_MeetsExpectationOnEnoughSeeds("stage01_detect")` 5/5(졸다가 귀 소음으로 깨어 박수), `("stage02_detect")` 5/5
- [ ] Stage 1·2 대표 캡처 4장(전경, 시작 위치, 인간 근접, Shadow Zone 내부)을 검토했다.
- [ ] `plan/progress.md`에 버티컬 슬라이스 리포트를 작성했다 (사람 검토 권장 시점).

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트: `plan/archive/m0-checklist.md`
- **M1 Core 충돌 월드 · 비행 · 카메라** — 태그 `m1-done` (2026-10-01). 체크리스트: `plan/archive/m1-checklist.md`. 이월: 부착 시선 제한 → M4 (D-028)
- **M2 인간 감지 · 어그로 · 광분** — 태그 `m2-done` (2026-10-01). 체크리스트: `plan/archive/m2-checklist.md`
- **M3 공격 · 반응 · 무작위 움직임 · 사망** — 태그 `m3-done` (2026-10-01). 체크리스트: `plan/archive/m3-checklist.md`. 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031)
- **M4 스텔스** — 태그 `m4-done` (2026-10-01). 체크리스트: `plan/archive/m4-checklist.md`. 이월: 젖은 날개·습기 2배 회복 → M6, 중독 → M9, 레벨 데이터 obstacle 검사 → M7 (D-034)
- **M5 흡혈 세션 · 물린 자국 · 포만 · 승리** — 태그 `m5-done` (2026-10-01). 체크리스트: `plan/archive/m5-checklist.md`
- **M6 물방울 QTE · 습기** — 태그 `m6-done` (2026-10-01). 체크리스트: `plan/archive/m6-checklist.md`. 이월: DripSource 레벨 적용 → M7

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| 2026-10-01 | M7 | 감각 큐: Co2Plume·SenseCueModel·SensesView(CO₂·체온·자국·은신처 강도), 머티리얼 3개, tuning 키 11개, 캡처에 감각 반영, D-040 | Core 256 + EditMode 51 + PlayMode 6 통과, 캡처 검토 | (이 커밋) |
| 2026-10-01 | M7 | 흐린 시야: SensesSettings·FogModel·SensesFog, 깊이 기반 안개+블러 셰이더, PC_Renderer 전체 화면 패스, tuning 키 2개, D-039 | Core 256 + EditMode 46 + PlayMode 6 통과, 캡처 검토 | 0159a69 |
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 사람 검토 권장 (막힘 아님)
- **밸런스(D-035):** 설계 검증 봇 기준으로 자국 5개(하한 40 = 의심 진입선)가 되면 인간이 영구 의심 상태로 몸 주변을 훑어 접근이 거의 불가능하다. 긴 세션 전략도 40%가 자국 5개에서 멈춘다. 수치(`biteMark.floorPerBite` 8, `floorMax` 45, 반응률)는 spec 제안값 그대로 두었으며, M7 버티컬 슬라이스 리포트에서 플레이 감각과 함께 검토를 요청한다.

## 막힘
(없음)

## 캡처 검토 기록
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
