# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M4 스텔스** (종료 기준 충족 → main merge, `m4-done`. 이월: 젖은 날개·습기 2배 회복 → M6, 중독 → M9, 레벨 데이터 obstacle 검사 → M7 (D-034))
- 다음 할 일: M5 시작 — spec/04 수용 기준 + 이월(흡혈 연결 2개, D-033)을 체크리스트로 복사
- 브랜치: `milestone/m4-stealth`

## 현재 마일스톤 체크리스트 (M4: spec/03 + 이월)
- [x] Shadow Zone 안에서는 Yellow Zone이어도 시각 증가가 0이다 (Core). — 증거: `StealthTests.ShadowZone_InsideYellowZone_NoVisionGainAndHidden`, `ShadowZone_LeaveZone_NoLongerHidden`, `HumanVisionTests.Hidden_InYellowZone_NotVisibleAndNoGain`
- [x] Shadow Zone 안에서 경계가 25/s로 감소한다 (Core). — 증거: `StealthTests.ShadowZone_NoStimulus_AwarenessDecays25PerSecond`
- [x] Shadow Zone 안에서 대시하면 경계가 +40 오른다 (Core). — 증거: `StealthTests.ShadowZone_DashInside_AwarenessPlus40`
- [x] 진입 시 비네트가 0.3초에 걸쳐 0.4가 된다 (Unity). — 증거: `StealthPresentationTests.Vignette_EnterShadow_Reaches04In03Seconds`, `Vignette_Component_DrivesVolumeVignetteIntensity` (ShadowVignette, 샌드박스 씬에 전역 Volume)
- [ ] Shadow Zone 안에서 중독·젖은 날개·습기·탈진이 2배 빠르게 회복된다 (Core). (M4: 탈진. 젖은 날개·습기는 M6, 중독은 M9 — D-034) — 진행: 탈진 `StaminaTests.Exhaustion_WhileHidden_RecoversTwiceAsFast`(실제 Shadow Zone 볼륨)
- [x] 부착 상태에서는 비행 소음이 발생하지 않는다 (Core: 반경 안 인간 경계 증가 0). — 증거: `StealthTests.Attached_NearEars_NoFlightNoise`
- [x] 부착 상태의 시각 증가율이 비부착 대비 0.3배이다 (Core). — 증거: `StealthTests.Attached_InYellowZone_VisionRateIs03Times`
- [x] 2u보다 먼 표면에서는 부착되지 않는다 (Core). — 증거: `AttachTests.Attach_SurfaceFartherThan2u_DoesNotAttach`, `Attach_NonAttachableSurface_DoesNotAttach`
- [x] 부착 시 캐릭터 up 벡터가 표면 법선과 5° 이내로 정렬된다 (Core). — 증거: `AttachTests.Attach_WithinRange_AttachesAlignedToNormal`
- [ ] 레벨 데이터의 모든 가구 형상에 obstacle 플래그가 있다 (Core: 레벨 데이터 검사). (레벨 포맷이 생기는 M7에서 검증 — D-034)
- [x] (M1 이월, D-028) 1인칭 부착 상태에서 시선이 법선 기준 `camera.fp.attachedLookLimit`를 벗어나지 않는다 (Unity). — 증거: `StealthPresentationTests.FirstPersonAttached_LookIntoWall_StaysWithinLimitOfNormal`(실제 부착 시뮬레이션, 240프레임 입력 누적), `ThirdPersonAttached_LookNotConstrained`

## 완료 마일스톤
- **M0 프로젝트 골격** — 태그 `m0-done` (2026-10-01). 체크리스트: `plan/archive/m0-checklist.md`
- **M1 Core 충돌 월드 · 비행 · 카메라** — 태그 `m1-done` (2026-10-01). 체크리스트: `plan/archive/m1-checklist.md`. 이월: 부착 시선 제한 → M4 (D-028)
- **M2 인간 감지 · 어그로 · 광분** — 태그 `m2-done` (2026-10-01). 체크리스트: `plan/archive/m2-checklist.md`
- **M3 공격 · 반응 · 무작위 움직임 · 사망** — 태그 `m3-done` (2026-10-01). 체크리스트: `plan/archive/m3-checklist.md`. 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031)

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
| 2026-10-01 | M4 | ShadowVignette(URP Volume), 1인칭 부착 시선 제한·카메라 up(D-028 해소), 샌드박스 탁자 밑 Shadow Zone, run-tests가 결과 없을 때도 컴파일 오류 출력. M4 종료 | Core 178 + EditMode 40 + PlayMode 3 통과, 빌드 성공 | (이 커밋) |
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
