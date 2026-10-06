# 10. 아트 디렉션 · 사운드

## 목적
에셋 패스(M10)의 목표 상태를 정의한다. 조달 방법과 라이선스 규칙은 `tech/asset-pipeline.md`를 따른다.

## 아트 디렉션
- 스타일: 서브컬처 애니메이션풍 툰 셰이딩. 2~3단 셀 셰이딩 + 외곽선.
- 원칙: **출처가 다른 에셋도 같은 툰 셰이더와 팔레트로 통일**한다. 에셋 고유의 PBR 질감은 버리고 베이스 컬러만 쓴다.
- 팔레트: 밤 실내 기반의 남색/보라 계열 그림자, 따뜻한 TV·스탠드 빛 대비. 플레이어는 흰색·연분홍 위주로 어두운 배경에서 잘 보이게 한다. (모기는 밝은 색을 기피한다는 설정과는 반대지만, 가독성을 우선한다.)
- 플레이어 캐릭터 "모키": 마법소녀 체형(2.5~3등신 데포르메), 반투명 날개 2장, 지팡이처럼 생긴 주둥이(흡혈 시 피부에 꽂음), 리본 장식.
  - 날개: 절차적 메시 + 셰이더로 고속 진동 표현 (모션블러형 잔상).
  - 최소 애니메이션: 호버링(idle), 이동 기울기, 대시, 부착, 흡혈, 갇힘, 사망.
- 인간: 툰 셰이딩된 일반 인간. 머리 회전과 팔 공격 애니메이션(예고 자세 → 타격 → 회복)이 필수다. 얼굴은 경계 상태에 따라 표정 3종이 있으면 좋다 (선택).
- 이펙트: 물방울(굴절 느낌의 투명 셰이더), 대시 소용돌이 잔상, 소음 핑 파동(디버그 겸용), 흡혈 시 게이지 파티클, CO₂ 흐름(spec/11), 체온 빛, 은신처 테두리 빛, 스프레이 연무, 모기향 연기, 증기, 미끼 마법.
- 모기 시야: 거리 기반 색 안개 + 블러 (spec/11 §1). 툰 셰이딩 팔레트에 맞춘 색으로 흐리게 한다.
- 포스트프로세싱: Bloom(약), Vignette(Shadow Zone/경계용), Color Grading.
- 조명(M14): 방마다 분위기 조명(TV·스탠드·달빛·형광등·도시 불빛·등불)이 실제 빛처럼 흔들린다. 툰 셰이더는 점·스포트 추가 조명도 셀 단계로 받는다. 방별 조명·쿠키·발광 텍스처 요구는 `spec/assets/lighting.md`.
- 움직임(M14): 선형 보간 대신 실제 몸처럼 가감속한다. 인간 머리는 가속해 돌고 남은 각에 맞춰 감속해 멈추며(`head.turnAccelTime`), 걷기는 출발·도착에서 속도를 올리고 줄이고 걸음마다 골반이 출렁인다(`human.walkAccelTime`, `human.walkBob`). 숨에 맞춰 몸통이 부풀고, 눈은 3~6초마다 깜빡인다. 모키는 정지 비행일수록 둥실거린다.

## 사운드
| ID | 설명 | 조달 |
|---|---|---|
| sfx_wing_loop | 날갯소리 루프. 속도에 따라 피치 변조 | 합성 가능 |
| sfx_dash | 바람 가르는 소리 | CC0 또는 합성 |
| sfx_attach / sfx_detach | 짧은 착지음 | CC0 또는 합성 |
| sfx_suck_loop | 흡혈 루프 | CC0 또는 합성 |
| sfx_slap / sfx_clap | 타격음 | CC0 |
| sfx_frenzy | 광분 진입 스팅 + 광분 중 긴장 루프 | 합성 가능 |
| sfx_telegraph | 공격 예고 경고음 (짧고 날카로움) | 합성 가능 |
| sfx_spray / sfx_toxin | 분사음 / 중독 단계 진입음 | CC0 또는 합성 |
| sfx_breath | 인간 숨소리 (CO₂ 표현과 동기) | CC0 |
| sfx_dislodge | 튕겨남 | 합성 가능 |
| sfx_decoy | 미끼 마법 날갯소리 | 합성 가능 |
| sfx_drop_trap / sfx_escape | 물방울 갇힘/탈출 | CC0 또는 합성 |
| sfx_ui_* | 선택/확인/취소 | CC0 |
| amb_stage1~5 | TV 소리(1·2), 선풍기(3), 환풍기·물소리(4), 풀벌레(5) | CC0 |
| sfx_snore / sfx_wake | 조는 숨소리 / 깜빡 깨는 소리 | CC0 또는 합성 |
| sfx_drip / sfx_steam | 물방울 떨어짐 / 증기 | CC0 |
| sfx_footstep | 걷는 인간의 발소리, 걸음마다 (M14) | 합성 가능 |
| sfx_wind_loop / sfx_wind_gust | 바람에 밀리는 동안 바람 루프(세기에 따라 음량·피치) / 바람에 처음 밀릴 때 휙 (M13) | 합성 가능 |
| bgm_title / bgm_stage | 짧은 루프 | CC0/CC-BY |

## 수용 기준
- [x] 모든 렌더러가 프로젝트 공통 툰 셰이더(또는 그 변형)를 사용한다 (Unity: 씬·프리팹 머티리얼 검사). — 증거: `Moqui/Toon`(셀 3단·남보라 그림자·월드 외곽선), `Moqui/ToonTransparent`(림), 기체 `SoftGas`·`VolumeFog`. PlayMode `StageSceneTests`가 5개 스테이지의 모든 렌더러 셰이더가 `Moqui/*`이고 지원됨을 검사
- [x] 누락/오류 셰이더(마젠타)가 없다 (Unity 검사 + 캡처 검토). — 증거: EditMode `SceneIntegrityTests.ProjectShaders_CompileWithoutErrors`(일부러 깨뜨린 셰이더로 실패 확인), PlayMode `StageSceneTests`의 `shader.isSupported` 검사, 최종 캡처 `Captures/2026-10-01_173131` 마젠타 없음. 이전: + `Captures/2026-10-01_165955` 검토: 마젠타 없음, 밤 실내 팔레트(라벤더·나무·청회색, 따뜻한 키 라이트·남색 환경광)
- [x] 플레이어 캐릭터가 위 최소 애니메이션 7종을 가진다 (애니메이터 상태 검사). — 증거: `MokiBuilder`가 모키(2.5등신·반투명 날개 2장·지팡이 주둥이·리본)와 `Art/Generated/Moki` 클립 7종·컨트롤러를 생성, `MokiAnimator`가 Core 상태→State 파라미터, `PlayerView`가 이동 기울기. EditMode `MokiTests`(매핑·7상태·전이 조건·바인딩 경로) 통과, 캡처 `Captures/2026-10-01_170956/Moki_*.png`
- [x] 인간이 머리 회전과 공격 3단계 애니메이션을 가진다. — 증거: `HumanView`: 얼굴(눈·코) 피벗이 Core `HeadForward`를 따라 회전, 공격 팔(어깨→주먹)이 `HumanArmPose`로 예고(뒤로 치켜듦)→타격(목표로)→회복(복귀). EditMode `HumanArmPoseTests`, PlayMode `SandboxHumanSceneTests`(얼굴 방향) 통과, 캡처 `Captures/2026-10-01_171446/Sandbox_Human_attack_*.png`
- [x] 위 사운드 ID가 모두 존재하고 이벤트에 연결되어 있다 (Unity: AudioCatalog 검사). — 증거: 33종 `tools/gen_audio.py` 합성(D-049, M13 바람 루프·휙 추가 — `AudioTests.Wind_LoopFollowsStrength_GustOnceOnEntering`), `Resources/Audio/AudioCatalog`, `AudioCues`(이벤트·상태→ID)·`AudioDirector`(Stage)·`AudioOutput`, UI 버튼 select/confirm/cancel, 메뉴 bgm_title. EditMode `AudioTests`(카탈로그·스펙 목록·생성 폴더 일치·ID 참조·이벤트 매핑·대시·반복음), PlayMode `StageSceneTests`(음악·환경음·날갯소리) 통과
- [x] 각 스테이지 캡처에서 플레이어가 배경과 구분된다 (캡처 검토, `plan/progress.md`에 소견 기록). — 증거: 캡처 검토 기록 2026-10-01 M10 참조(다섯 스테이지 시작 3인칭에서 분홍 모키가 안개·벽과 구분, Stage 4 역광은 자체 밝기 0.3으로 보완)

## 범위 외
- 립싱크, 보이스, 동적 음악 레이어링
