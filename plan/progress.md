# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M3 공격 · 반응 · 무작위 움직임 · 사망** (종료 기준 충족 → main merge, `m3-done`. 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031))
- 다음 할 일: M4 시작 — spec/03 수용 기준 + 이월 항목(부착 시선 제한 D-028)을 체크리스트로 복사
- 브랜치: `milestone/m3-attacks`

## 현재 마일스톤 체크리스트 (M3: spec/02 §5~7, spec/04 §7)
- [x] 귀 근접 구역에서 귀 반응이 발생할 수 있다 (Core). (M2에서 이월) — 증거: `ReactionTests.EarZone_Airborne_ProducesEarReactionsAtEarRate`
- [x] 착지 반응 확률이 민감도에 비례한다: 같은 시드 10,000회 시행에서 기대값 ±2%p (Core, 통계 테스트). — 증거: `ReactionTests.LandingReaction_10000Trials_MatchesLandChanceTimesSensitivity`(5개 부위 유형), `LandingReaction_SameSeed_SameOutcomes`
- [x] 부착 중 반응 위험률이 가려움 제곱에 비례해 증가하고, 가려움 100이면 즉시 반응한다 (Core). — 증거: `ReactionTests.AttachedHazard_GrowsWithItchSquared`, `AttachedHazard_PerTickRolls_MatchExponentialProbability`, `Attached_Itch100_ReactsImmediately`
- [x] 모든 공격과 반응의 목표가 예고 시작 시점의 위치로 고정된다: 예고 중 판정 밖으로 이동하면 생존하고, 머무르면 사망한다 (Core). — 증거: `ReactionTests.ReactSlap_PlayerLeavesDuringTelegraph_Survives`, `ReactSlap_PlayerStays_DiesWithAttackCause`, `Clap_PlayerLeavesRedZoneDuringTelegraph_Survives`, `Clap_PlayerStays_Dies` (손바닥·맹목 휘두르기도 같은 `HumanAttackSystem.Start` 경로)
- [ ] 무작위 움직임이 흡혈 중에도 발생하고, 붙어 있는 모기가 부위를 따라 움직인다 (Core). (흡혈 세션 연결 검증은 M5) — 진행: `HumanMotionTests.Actions_ScheduledEvery4To9Seconds_MoveParts`, `Attached_DuringSlowAction_FollowsPartWithoutDislodging`, `Actions_KeepHappeningWhilePlayerIsAttached`. 흡혈 중 발생은 M5 (D-033)
- [ ] 부위 속도가 dislodgeSpeed를 넘으면 모기가 튕겨 나가고 흡혈 세션이 끝나며, 사망하지 않는다 (Core). (세션 종료 검증은 M5) — 진행: `HumanMotionTests.FastAction_PartFasterThanDislodgeSpeed_DislodgesWithoutDeath`. 세션 종료는 M5 (D-033)
- [x] 같은 시드와 같은 입력이면 인간의 움직임과 반응이 똑같이 재현된다 (Core). — 증거: `HumanMotionTests.SameSeed_MotionAndReactionsReplayExactly`(동작·부위 위치·반응·튕겨남 이벤트), `FrenzyTests.SameSeedAndCommands_HumanBehaviourReplaysExactly`
- [x] 공격 중에는 새 공격이나 반응이 시작되지 않는다 (Core). — 증거: `ReactionTests.AttackInProgress_NoNewAttackOrReactionUntilRecoveryEnds`
- [ ] 각 사망 원인이 올바른 DeathCause로 기록된다 (Core, 원인별 1개 테스트). (M3: Attack. WaterImpact는 M6, Web·Spray는 M9) — 진행: Attack 완료(`ReactionTests.ReactSlap_PlayerStays_DiesWithAttackCause`)
- [x] 재시도 시 흡혈 게이지, 가려움, 자국, 인간 경계, 플레이어 위치, 중독 게이지가 모두 초기화된다 (Core). — 증거: `RetryTests.Retry_AfterPlaying_EveryStateMatchesFreshStart`(스냅샷 전체 비교: 위치·스태미나·경계·광분·공격·가려움·자국), `Retry_SameCommands_ReplaysIdentically`. 흡혈·중독 게이지는 생기는 즉시 스냅샷에 넣어 같은 테스트로 검증 (D-031)

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트: `plan/archive/m0-checklist.md`
- **M1 Core 충돌 월드 · 비행 · 카메라** — 태그 `m1-done` (2026-10-01). 체크리스트: `plan/archive/m1-checklist.md`. 이월: 부착 시선 제한 → M4 (D-028)
- **M2 인간 감지 · 어그로 · 광분** — 태그 `m2-done` (2026-10-01). 체크리스트: `plan/archive/m2-checklist.md`

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 막힘
(없음)

## 캡처 검토 기록
- 2026-10-01 M2 `Captures/2026-10-01_133118/` Sandbox_Human: 평온(머리 초록, 시선 패턴 yaw 11°), 대시 소음 뒤 의심(머리 노랑, 소리 쪽 뒤-오른쪽으로 돌아 yawLimit 100°에서 멈춤), 광분 손바닥 예고(머리 빨강, 플레이어 위치에 주황 예고 표시 반경 12u). 마젠타 없음. 캡슐 인간 비율·소파 배치 정상. 첫 캡처에서 광분 장면 예고가 안 뜬 것은 머리가 뒤를 보고 있어 시야 밖이었기 때문(정상 동작) → 캡처 시나리오에서 머리를 정면으로 되돌림
- 2026-10-01 M1 `Captures/2026-10-01_131326/`, `2026-10-01_131432/` Sandbox_Flight: 3인칭 개요(가구·벽·플레이어 구 정상), 1인칭 벽 접촉 정면·비스듬히(벽면과 방 안쪽만 보임, 벽 뒤 노출 없음), 3인칭 벽 접촉(카메라가 벽 안쪽 유지). 마젠타 없음. 첫 캡처에서 플레이어 색이 빠진 문제(MaterialPropertyBlock 미저장) → 머티리얼 에셋 `Whitebox_Player.mat`으로 수정 후 재확인
- 2026-10-01 M0 `Captures/2026-10-01_123452/Boot.png`: 템플릿 빈 씬(하늘·바닥). 마젠타 없음, 템플릿 볼륨의 피사계 심도로 전체가 흐림 — 표현 작업(M1 이후)에서 볼륨 프로파일 정리 필요

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M3 | 무작위 동작(HumanActionDefinition, HumanMotionSystem, 절차적 포즈), 동작이 플레이어 이동보다 먼저 실행, 튕겨남 검증, 샌드박스 인간 동작 4종, D-033. M3 종료 | Core 172 + EditMode 36 + PlayMode 3 통과, 빌드 성공 | (이 커밋) |
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
