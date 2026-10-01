# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M1 Core 충돌 월드 · 비행 · 카메라** (진행 중)
- 다음 할 일: SimulationRunner + 카메라 리그(3인칭/1인칭, 벽 충돌, 전환, 저장)
- 브랜치: `milestone/m1-flight`

## 현재 마일스톤 체크리스트 (M1)
### 충돌 월드 (tech/architecture.md §4.5)
- [x] Raycast / SphereSweep / Overlap / ClosestSurface (Box OBB·Sphere·Capsule, 플래그 마스크) — 증거: `CollisionWorldTests` 16개, `ShapeCastCrossCheckTests.Cast_RandomRays_AgreesWithMarching` (형상별 무작위 3000건을 무차별 전진 계산과 비교)

### spec/00 월드 스케일 & 카메라
- [x] 빈 테스트 씬에서 플레이어를 원점 기준 ±500u 범위 어디에 두어도 이동과 충돌이 정상 동작한다 (Core). — 증거: `CollisionMovementTests.WorldRange_AnyPositionWithin500u_MovesAndCollides` (27개 위치), `FlyIntoWall_StopsBeforeSurfaceWithZeroNormalVelocity`, `DiagonalIntoWall_SlidesAlongSurface`, `VariedInputsInBox_NeverPenetrate`
- [ ] 3인칭 카메라 near clip이 `camera.nearClip`이고, 벽에 붙어도 카메라가 벽을 관통하지 않는다 (Unity).
- [x] pitch가 `camera.pitchLimit`를 넘지 않는다 (Unity: 입력 누적 테스트). — 증거: `CommandCollectorTests.MouseDelta_AccumulatedUpAndDown_PitchStaysWithinLimit`
- [x] 낙하체가 1초 동안 `world.gravity`로 가속되어 490.5u(±1%) 떨어진다 (Core). — 증거: `ExternalForceTests.FallingBody_OneSecond_Falls4905u`
- [ ] 게임 시작 시 기본 시점은 3인칭이다 (Unity, 저장 데이터 없음).
- [ ] ToggleView 입력으로 3인칭 ↔ 1인칭이 전환되고, `camera.switchTime` 후 위치와 FOV가 목표값에 도달한다 (Unity).
- [ ] 전환 전후의 yaw/pitch가 같다 (Unity).
- [ ] 같은 입력 시퀀스를 두 시점에서 재생하면 플레이어 최종 위치가 같다 (Unity: 시점 독립성).
- [ ] 1인칭에서 플레이어 메시의 렌더러가 Shadows Only이고, 3인칭으로 돌아오면 원래대로 복구된다 (Unity).
- [ ] 1인칭 부착 상태에서 시선이 법선 기준 `camera.fp.attachedLookLimit`를 벗어나지 않는다 (Unity).
- [ ] 1인칭에서 벽에 최대한 붙어도 화면에 벽 뒤가 보이지 않는다 (캡처 검토: 벽 접촉 포즈 1장).
- [ ] 선택한 시점이 재시작 후에도 유지된다 (Unity).

### spec/01 비행 · 대시 · 스태미나 · 입력
- [x] 최고 속도에서 입력을 놓으면 0.15초에 정지하고, 그동안 4.5u(±2%) 미끄러진다 (Core). — 증거: `FlightTests.Release_AtTopSpeed_StopsIn015SecondsAfterSliding45u`
- [x] 정지 상태에서 1초 동안 W 입력 시 이동 거리가 56.4u(±1%)이다 (Core). — 증거: `FlightTests.Forward_OneSecondFromRest_Travels564u`
- [x] 반대 방향 입력 시 속도가 가속 규칙에 따라 부드럽게 반전된다 (Core). — 증거: `FlightTests.ReverseInput_AtTopSpeed_ReversesSmoothly`
- [x] 대시 종료 직후 속도가 대시 방향 60u/s이고 이후 감속한다 (Core). — 증거: `DashTests.Dash_JustFinished_HasFlightSpeedAlongDashThenDecelerates`
- [x] 바람 외력은 입력과 무관하게 즉시 더해진다 (Core). — 증거: `ExternalForceTests.Wind_NoInput_AddedImmediatelyWithoutInertia`, `Wind_WithInput_AddsToInputMovement`
- [x] 대각선 입력 속도가 단일 방향 속도와 같다 (Core). — 증거: `FlightTests.DiagonalInput_TopSpeed_EqualsSingleDirectionSpeed`
- [x] 대시가 0.12초 동안 60u를 이동한다 (Core, ±1u). — 증거: `DashTests.Dash_FromRest_Moves60uIn012Seconds` (7틱, D-026)
- [x] 좌우·상하 입력이 없을 때 대시는 위쪽이다 (Core: DashDirectionResolver). — 증거: `DashTests.DashDirection_NoLateralOrVerticalInput_IsUp`, `DashDirection_LargestAxisWins`, `DashDirection_TieBetweenLateralAndVertical_PrefersLateral`
- [x] 스태미나 < 25이면 대시가 실행되지 않는다 (Core). — 증거: `DashTests.Dash_StaminaBelowCost_DoesNotExecute`
- [x] 쿨타임 안의 재입력은 무시된다 (Core). — 증거: `DashTests.Dash_PressedDuringCooldown_IsIgnored`
- [x] 대시 시 NoiseEvent가 정확히 1회, 반경 150u로 발생한다 (Core). — 증거: `DashTests.Dash_Executed_EmitsSingleNoiseEventWith150uRadius`
- [x] 스태미나가 마지막 소모 1초 후부터 20/s로 회복한다 (Core). — 증거: `StaminaTests.Stamina_AfterDash_RegeneratesFrom1SecondAt20PerSecond`
- [x] 스태미나 0이면 2초간 속도 50%, 대시 불가 (Core). — 증거: `StaminaTests.Stamina_ReachesZero_Exhausted2SecondsWithHalfSpeedAndNoDash`, `Exhaustion_WhileHidden_RecoversTwiceAsFast`
- [x] 대시가 벽을 관통하지 않는다 (Core). — 증거: `DashTests.Dash_IntoWall_StopsWithoutPenetrating`
- [x] 위 입력 매핑이 Input Actions 에셋에 존재하고, 게임패드로도 동일하게 동작한다 (Unity: 가상 Gamepad). — 증거: `Assets/_Project/Input/MoquiControls.inputactions`(Gameplay 맵), `CommandCollectorTests.Asset_GameplayAction_HasKeyboardAndGamepadBindings`(11개 액션), `Gamepad_AllGameplayInputs_ProduceCommand`, `KeyboardMouse_AllGameplayInputs_ProduceCommand`, `ToggleViewAndPause_Gamepad_AreConsumedSeparately`, `GamepadStick_FullRightOneSecond_RotatesYawByLookSpeed`

### 마일스톤 산출물
- [ ] `Sandbox_Flight` 씬 (빌드 제외)

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트와 증거는 태그 시점의 이 문서 참조.

<!-- M0 체크리스트 (보관) -->
- [x] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록 — 증거: `ProjectSettings/ProjectVersion.txt` = 6000.6.3f1, D-019, 배치 모드 임포트 exit 0
- [x] 필수 패키지 설치 (tech/conventions.md §1) — 증거: manifest(URP 17.6.0, Input System 1.20.0 + activeInputHandler=1, Cinemachine 6.6.0(D-023), Test Framework 1.8.0, uGUI 2.6.0(TMP 포함), ProBuilder 6.1.2), `Tools/unity-import.ps1` exit 0·컴파일 이슈 0
- [x] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3) — 증거: `SplitMix64RandomTests` 4개 통과, Unity 배치 모드에서 Moqui.Core.dll 컴파일 CS 에러/경고 0
- [x] `.gitignore`, `.gitattributes`(LFS) — 증거: `git check-attr filter -- a.png` → `lfs`, D-020
- [x] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트 — 증거: `TuningDocumentTests.TuningJson_EverySpecKey_HasMatchingValue`, `TuningJson_NoKeysOutsideSpec`, `Load_RepoTuningJson_Succeeds` (변조 시 실패 확인), `TuningTests`, `JsonReaderTests`
- [x] `Tools/run-tests`, `Tools/build`, `Tools/capture` — 증거: `run-tests.ps1` exit 0 (Core 35, EditMode 1, PlayMode 1), `build.ps1` exit 0, `capture.ps1` exit 0 → `Captures/2026-10-01_123452/Boot.png`. 각 ps1에 Git Bash 래퍼(sh)
- [x] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과 — 증거: `SplitMix64RandomTests`, `Moqui.Unity.Tests.UnityDataSourceTests.Load_InEditor_ReadsRepoTuning`, `Moqui.Unity.Tests.PlayModeSmokeTests.Tuning_LoadedInPlayMode_SurvivesFrame`
- [x] 빈 씬 Standalone 빌드 성공 — 증거: `Builds/Windows/Moqui.exe`, BuildScript result=Succeeded errors=0 unexpected warnings=0 (D-024), StreamingAssets/data/tuning.json 포함 확인
- [x] Unity 어댑터 asmdef (Runtime/Presentation/UI/Editor, Tests.EditMode/PlayMode, D-025) — 증거: `unity-import.ps1` 컴파일 이슈 0

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 막힘
(없음)

## 캡처 검토 기록
- 2026-10-01 M0 `Captures/2026-10-01_123452/Boot.png`: 템플릿 빈 씬(하늘·바닥). 마젠타 없음, 템플릿 볼륨의 피사계 심도로 전체가 흐림 — 표현 작업(M1 이후)에서 볼륨 프로파일 정리 필요

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M1 | 입력 에셋 MoquiControls(Gameplay 맵, spec/01 표), LookState, CommandCollector(edge 래치, 마우스·스틱 분리) | EditMode 19/19 통과 | (이 커밋) |
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
