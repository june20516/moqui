# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M6 물방울 QTE · 습기** (종료 기준 충족 → main merge, `m6-done`. 이월: DripSource 높이 검사의 실제 레벨 적용 → M7 (D-036))
- 다음 할 일: M7 시작 — 거실 버티컬 슬라이스(레벨 데이터 포맷, 거실 Stage 1·2, 졸음, HUD, 튜토리얼, 감각, 봇) 체크리스트 작성
- 브랜치: `milestone/m6-water`

## 현재 마일스톤 체크리스트 (M6: spec/05 + 이월)
- [x] 물방울이 2.5초 간격으로 생성된다 (Core). — 증거: `WaterTests.Drops_SpawnEvery25Seconds`, `Drops_FallAndVanishOnFloor`
- [x] 물방울에 닿으면 Trapped가 되고 이동 입력이 무시된다 (Core). — 증거: `WaterTests.DropHitsPlayer_Trapped_MoveInputIgnored`, `DropPassesPlayer_AnyHeight_AlwaysTraps`
- [x] Trapped 중 낙하 속도가 100u/s 등속이다 (Core, ±2%). — 증거: `WaterTests.Trapped_FallsAtConstant100`
- [x] 대시 2회 입력 시 탈출하고, 스태미나가 줄지 않는다 (Core). — 증거: `WaterTests.DashTwice_Escapes_StaminaUnchanged`
- [x] 탈출 전 바닥 충돌 시 WaterImpact로 사망한다 (Core). (spec/04 §7 DeathCause.WaterImpact 원인별 테스트 겸함 — D-031) — 증거: `WaterTests.TrappedUntilFloor_DiesWithWaterImpact`
- [x] 탈출 후 10초간 속도 0.7배, 회복 0.5배, 대시 비용 35 (Core). — 증거: `WaterTests.AfterEscape_WetWingsFor10Seconds_SpeedRegenDashCost`
- [x] 젖은 날개 중 다시 젖으면 지속시간만 10초로 갱신된다 (Core). — 증거: `WaterTests.WetAgain_DurationRefreshedNotStacked`
- [ ] 모든 레벨의 DripSource가 착지면 기준 150u 이상이다 (Core: 레벨 데이터 검사 — 아래로 광선 검사). (검사 함수는 M6, 실제 레벨 적용은 M7 — D-036) — 진행: 검사 함수 `LevelChecks.DripSourceHighEnough` + `WaterTests.DripSourceHeight_CheckedByDownwardRay`. 실제 레벨 적용은 M7
- [x] 습기 게이지가 강/약 영역에서 15/5 per s로 오르고, 밖에서 10/s로 내린다 (Core). — 증거: `WaterTests.Humidity_StrongWeakOutside_15_5_Minus10`, `Humidity_RisesWhileAttached`
- [x] 습기 100에서 젖은 날개가 걸리고, 영역 안에 머무는 동안 지속시간이 갱신된다 (Core). — 증거: `WaterTests.Humidity_Reaches100_WetWingsAndRefreshedWhileInside`
- [x] 증기 안의 플레이어에 대한 시각 증가율이 0.6배이다 (Core). — 증거: `WaterTests.Steam_VisionRateIs06Times`, `Steam_IntegratedInHumanSystem`
- [x] 숨은 상태에서 젖은 날개 시간과 습기 게이지가 2배 빠르게 줄어든다 (Core). (spec/03 2배 회복 기준의 젖은 날개·습기 부분 겸함 — D-034) — 증거: `WaterTests.Hidden_WetAndHumidityRecoverTwiceAsFast`

### 마일스톤 산출물
- [x] `Sandbox_Water` 씬 (빌드 제외) — 증거: `Assets/_Project/Scenes/Sandbox_Water.unity`, `SandboxWaterSceneTests.SandboxWater_Play_SpawnsAndDrawsDrops`, 캡처 `Captures/2026-10-01_141811/Sandbox_Water_*.png`

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트: `plan/archive/m0-checklist.md`
- **M1 Core 충돌 월드 · 비행 · 카메라** — 태그 `m1-done` (2026-10-01). 체크리스트: `plan/archive/m1-checklist.md`. 이월: 부착 시선 제한 → M4 (D-028)
- **M2 인간 감지 · 어그로 · 광분** — 태그 `m2-done` (2026-10-01). 체크리스트: `plan/archive/m2-checklist.md`
- **M3 공격 · 반응 · 무작위 움직임 · 사망** — 태그 `m3-done` (2026-10-01). 체크리스트: `plan/archive/m3-checklist.md`. 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031)
- **M4 스텔스** — 태그 `m4-done` (2026-10-01). 체크리스트: `plan/archive/m4-checklist.md`. 이월: 젖은 날개·습기 2배 회복 → M6, 중독 → M9, 레벨 데이터 obstacle 검사 → M7 (D-034)
- **M5 흡혈 세션 · 물린 자국 · 포만 · 승리** — 태그 `m5-done` (2026-10-01). 체크리스트: `plan/archive/m5-checklist.md`

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 사람 검토 권장 (막힘 아님)
- **밸런스(D-035):** 설계 검증 봇 기준으로 자국 5개(하한 40 = 의심 진입선)가 되면 인간이 영구 의심 상태로 몸 주변을 훑어 접근이 거의 불가능하다. 긴 세션 전략도 40%가 자국 5개에서 멈춘다. 수치(`biteMark.floorPerBite` 8, `floorMax` 45, 반응률)는 spec 제안값 그대로 두었으며, M7 버티컬 슬라이스 리포트에서 플레이 감각과 함께 검토를 요청한다.

## 막힘
(없음)

## 캡처 검토 기록
- 2026-10-01 M6 `Captures/2026-10-01_141811/` Sandbox_Water: 물방울(지름 3u)이 플레이어 위에서 낙하, 갇힌 플레이어가 물방울과 함께 세면대로 낙하(남은 높이 36.4u). 마젠타 없음. 첫 캡처에서 갇히지 않은 것은 물방울이 틱당 7u를 떨어지며 플레이어를 건너뛴 결함 → 선분 거리 판정으로 수정, 높이별 회귀 테스트 추가
- 2026-10-01 M2 `Captures/2026-10-01_133118/` Sandbox_Human: 평온(머리 초록, 시선 패턴 yaw 11°), 대시 소음 뒤 의심(머리 노랑, 소리 쪽 뒤-오른쪽으로 돌아 yawLimit 100°에서 멈춤), 광분 손바닥 예고(머리 빨강, 플레이어 위치에 주황 예고 표시 반경 12u). 마젠타 없음. 캡슐 인간 비율·소파 배치 정상. 첫 캡처에서 광분 장면 예고가 안 뜬 것은 머리가 뒤를 보고 있어 시야 밖이었기 때문(정상 동작) → 캡처 시나리오에서 머리를 정면으로 되돌림
- 2026-10-01 M1 `Captures/2026-10-01_131326/`, `2026-10-01_131432/` Sandbox_Flight: 3인칭 개요(가구·벽·플레이어 구 정상), 1인칭 벽 접촉 정면·비스듬히(벽면과 방 안쪽만 보임, 벽 뒤 노출 없음), 3인칭 벽 접촉(카메라가 벽 안쪽 유지). 마젠타 없음. 첫 캡처에서 플레이어 색이 빠진 문제(MaterialPropertyBlock 미저장) → 머티리얼 에셋 `Whitebox_Player.mat`으로 수정 후 재확인
- 2026-10-01 M0 `Captures/2026-10-01_123452/Boot.png`: 템플릿 빈 씬(하늘·바닥). 마젠타 없음, 템플릿 볼륨의 피사계 심도로 전체가 흐림 — 표현 작업(M1 이후)에서 볼륨 프로파일 정리 필요

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M6 | 브랜치·체크리스트, D-036, WaterSystem(발생·Trapped·탈출·WaterImpact), HumiditySystem(습기·증기·젖은 날개), 대시 비용·회복 배율, LevelChecks, 스냅샷 확장, WaterView·Sandbox_Water·캡처. **결함 수정**: 물방울 포획 터널링(선분 판정). M6 종료 | Core 220 + EditMode 40 + PlayMode 4 통과, 빌드 성공, 캡처 검토 | (이 커밋) |
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
