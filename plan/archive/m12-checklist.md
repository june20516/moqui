# M12 플레이테스트 반영 — 체크리스트 (보관)

태그 `m12-done` (2026-10-05).

> 2026-10-01 사람 플레이테스트 피드백 8건 + 추가 4건. spec에는 `(M12 플레이테스트 반영, 미구현)`으로 표시했다. 결정 기본값은 D-051·D-052·D-053.

| # | 피드백 | 원인(확인) | 반영 위치 |
|---|---|---|---|
| 1 | 대시가 무조건 위로만 간다 | 설계대로였음: spec/01 "좌우·상하 입력이 없으면 위쪽, 전후 입력은 무시" → 앞으로 날며 대시하면 항상 위 | spec/01 대시 방향, spec/09 와류 제어 3레벨 |
| 2 | 천장·벽에 앉을 수 있어야 함 | Core는 벽·천장 부착을 지원(모든 방 벽·천장 `attachable`)하지만 `PlayerView`가 표면 법선(`Player.Up`)을 무시해 모키가 늘 똑바로 서 있음 → 앉은 것으로 안 보임. 실제 부착 실패 여부는 테스트로 확인 | spec/03 |
| 3 | 사람이 직접 때려야 함 | M10에서 Core 팔은 정지 판정 캡슐이라 표현 전용 팔을 따로 만들었음 | spec/02 |
| 4 | 시야가 더 흐려야 CO₂ 힌트가 쓸모 있음 | 현재 80u 선명 → 250u 최대, 최대 흐림 0.75 | spec/11, tuning |
| 5 | 자국이 문 자리에 생겨야 함 | Core가 부위별 자국 여부(bool)만 저장, 점은 부위 중심 근처 하나 | spec/04, spec/11 |
| 6 | 하강 LAlt, 정밀 LCtrl, 대시 우클릭 | 매핑 변경 요청 | spec/01, spec/08 문구 |
| 7 | 붙은 상태: 방향키 = 이탈, 대시 = 표면 수직 대시 | 방향키 이탈은 이미 동작. 부착 중 대시는 무시되고 있음 | spec/01 상태도 |
| 9 | 체온 표시의 위상이 너무 높다(부피 막) — 얇게/인지적으로만 | 체온 = 피부 캡슐을 굵게 키운 반투명 주황 막(D-040) | spec/11 |
| 10 | 맞을 자리(공격 예고)는 위상을 더 높이고 움직임으로 | 예고 = 정지한 반투명 주황 구 → 판정 시 빨강 | spec/02 §7 |
| 11 | 실제 사람이 낼 수 있는 동작·속도로 때려야 | 사거리 80+40u가 팔 길이(약 66u+손)보다 김, 타격 시간 고정(0.1s), 팔 경로 없음 | spec/02 |
| 12 | (아이디어) 파리채 등 도구 → 난이도·레벨링 | 범위 밖 | plan/ideas.md |
| 13 | 다 보이는 상태에서 손이 총알처럼 뻗으면 안 됨 → 광분은 사람 몸이 직접 움직여야. 우선 자리에서 자세 변경, 걷기는 고난도 확장 | 인간 루트·몸 캡슐이 고정, 몸동작 없음 | spec/02, plan/ideas.md |
| 8 | 피 게이지 오른쪽 주황 큰 점의 의미 | **포만 아이콘**(속도 배율 < 0.85면 주황으로 강조). 자국 점과 같은 원 모양이라 헷갈림 | spec/08 |

### 작업 순서 (반복 1~3개 기준씩, 매번 테스트 → 커밋)
- [x] A. 입력·대시 (#6, #7, #1) — 증거: Core `DashTests`(진행 방향·무작위 재현·부착 중 법선 대시·스태미나 부족 시 유지), `SkillTests.VortexControl_*`, EditMode `CommandCollectorTests`(새 매핑), HUD 프롬프트·튜토리얼 문구 갱신. 봇은 대시를 쓰지 않아 영향 없음(Core 시나리오 통과): Input Actions 매핑 변경과 키 표기(HUD 프롬프트·튜토리얼 문구), Core `DashDirectionResolver`를 진행 방향 + 무작위(시드 스트림)로 교체, 부착 중 대시 = 법선 방향 대시, 와류 제어 3레벨 = 쿨타임 0.7배. Core 테스트(방향·무작위 재현·부착 대시·스킬) + Unity 입력 매핑 테스트, 시나리오 봇 재검증(봇이 대시를 쓰는 경로 확인)
- [x] B. 벽·천장 부착 (#2) — 증거: Core `AttachTests.Attach_CeilingAndWallSide_StaysOnSurface`(Core는 원래 지원, 원인은 표현), EditMode `MokiTests.AttachedRotation_UpMatchesSurfaceNormal`, `PlayerView` 부착 중 표면 법선 정렬: Core 테스트(벽 옆면·천장 아랫면 부착, 부착 위치 유지), `PlayerView`가 부착 중 `Player.Up`에 맞춰 회전(모키가 벽·천장에 앉은 자세), 부착 프롬프트·카메라 확인, 캡처(천장 부착)
- [x] C0. 사람 팔 동작 근거 조사 (#11) — 증거: `knowledge/human-arm-motion.md`(체절 길이 Drillis & Contini, AAOS 관절 범위, 손바닥 치기 5~7 m/s, 반응 200~250ms, 일어서기 약 2.2초, 출처 링크, 게임 수치 제안): 성인 팔 길이(어깨→손끝)·관절 가동 범위·손바닥 치기/휘두르기 손 최고 속도·반응 시간·박수 동작을 자료로 확인해 `knowledge/human-arm-motion.md`에 출처와 함께 정리하고, `spec/tuning.md`에 수치 키(팔 길이 비율, 관절 한계, 손 최고 속도, 자세별 기울임) 제안
- [x] C1. 인간 뼈대 포즈 (#13 기반) — 증거: Core `BodyRig`(데이터 캡슐에서 골반·팔 사슬 추출, 데이터 형식 그대로)·`BodyPose`·`Human.UpdatePose`(휴식 + 동작 → 일어섬 → 상체 비틀기·기울기 → 팔 2관절 IK), 루트·몸 회전 상태화, 기존 352개 Core 테스트 통과(자세·SkinSite·판정 회귀 없음): 레벨 데이터의 고정 캡슐 → 뼈대(루트·골반·척추·어깨·팔꿈치·손목·목·머리) + 뼈에 붙은 캡슐, 루트를 상태로. 기존 자세(앉기·눕기·고개 숙임)·actions·SkinSite·판정이 그대로 나오는지 회귀 테스트, 레벨 데이터 변환
- [x] C2. 몸동작 계획기 (#13) — 증거: `AttackPlanner`, `BodyAttackTests.Planner_*`·`Rise_*`·`Lean_*`, 레벨 `human.maxPosture`(stage03 누운 인간 turn): 목표에 닿기 위한 단계(팔 → 기울이기 → 돌기 → 일어서기, `human.maxPosture` 상한), 자세 전환 시간, 일어서면 머리·시야 이동. 광분 손바닥·맹목 휘두르기·반응이 모두 계획기를 거침
- [x] C. 실제 팔 공격 (#3, #11) — 증거: `HumanAttackSystem` 재작성(어깨 중심 호 경로, 타격 시간 = 호 최고 속도 ≤ 손 최고 속도, 손 경로 판정, 반대 손, 닿지 않으면 공격 안 함), `BodyAttackTests`(13), EditMode `HumanBodyViewTests`, 표현용 팔 제거, 봇 재검증(클리어 5·4·5·5·5, 발각 전 스테이지 5/5 — Stage 5 발각 봇은 Red Zone으로, 클리어 봇은 등 뒤 은신 경로 추가, 봇은 손 경로 위협만 피함), Unity 안 봇 재생 스모크 통과: Core 팔 2관절 포즈(어깨·팔꿈치 각도 한계 안), 손 경로 = 어깨 → 팔꿈치 → 손목 순서의 호, 타격 시간 = 경로 길이 ÷ 손 최고 속도, 닿는 거리 밖 목표는 때리지 않음, 내 팔에 앉은 모기는 반대 손, 박수는 두 손, 판정 = 움직이는 손 구의 경로. Core가 공격 단계에 따라 가까운 어깨 쪽 upperArm·forearm 캡슐을 움직인다(어깨 고정 2관절, 예고 치켜듦 → 판정 목표로 뻗음 → 회복). 표현용 팔(`HumanArmPose`의 별도 팔·주먹) 제거, `HumanView`는 Core 형상만 따름. 그 팔에 붙은 모기는 따라가거나 튕겨 남(기존 dislodge 규칙). Core 테스트 + 봇 재검증 + 공격 단계 캡처
- [x] D. 자국 위치 (#5) — 증거: Core `BiteMark`(SurfaceAnchor, 흡혈 세션 시작 때 문 자리 기록)·`Human.BiteMarks`·스냅샷 `BiteMarks`, `SuckTests.BiteMark_*`, EditMode `SensesViewTests.BiteMark_DotAtTheBiteSpot_OnlyInsideHeatRange`(자국마다 점): Core에 자국 목록(부위 ID + 부위 로컬 좌표, 세션의 부착 지점) 추가·스냅샷, `SensesView`가 문 자리에 점을 그리고 부위를 따라 움직임. 자국 수 n 규칙은 그대로
- [x] E. 흐림 강화 (#4) — 증거: tuning senses.clearRange 50·fogFullRange 160·fogMaxDensity 0.88·fogBlurPixels 6, 캡처 `Captures/2026-10-01_223838` 검토(인간 희미, CO₂ 또렷, 큰 가구 윤곽 유지), 테스트 통과: 제안값(clearRange 50, fogFullRange 160, fogMaxDensity 0.88, fogBlurPixels 6)으로 캡처 비교 → 인간은 흐림에 묻히고 CO₂·가구 실루엣은 보이는 값으로 `spec/tuning.md`·`data/tuning.json` 확정, 겹눈 스킬 효과 재확인
- [x] H. 체온 표시 재디자인 (#9) — 증거: 셰이더 `Moqui/HeatShimmer`(림 + 위로 흐르는 아지랑이 선, 가산), `senses.heatGlowScale` 1.8 → 1.04, `SensesViewTests.Heat_IsThinShimmer_NotAVolumeAroundTheSkin`, 캡처 검토: 굵은 캡슐 막 → 피부 윤곽 림/아지랑이처럼 일렁이는 얇은 선(셰이더), 존재감 낮게·인지는 되게. 캡처로 공격 예고보다 낮은 위상 확인
- [x] I. 공격 예고 동적 표현 (#10) — 증거: 셰이더 `Moqui/TelegraphRing`(카메라를 향한 판, 좁혀 오는 고리·차오름·가속 깜빡임·판정 번쩍임) + 손 접근 줄기, EditMode `HumanBodyViewTests.Telegraph_*`, 캡처 검토(체온 림보다 확실히 강함): 예고 진행률만큼 차오르는 링·좁혀 오는 테두리·일렁임, 판정 순간 번쩍임, 손 접근 방향 표시. 화면에서 가장 눈에 띄는지 캡처 검토(체온·CO₂·은신처 표시와 비교)
- [x] F. 포만 아이콘 (#8) — 증거: `HudSprites.Weight`(추 모양) + "포만" 글자, `HudTests.Satiety_*`, 캡처 `Captures/2026-10-05_170937/Hud_1920x1080.png`: 원 대신 배부른 배/추 모양 + "포만" 글자, HUD 테스트·3해상도 캡처
- [x] G. 마무리 — 증거: Core 353 + EditMode 126 + PlayMode 22 통과(Unity 안 봇 재생 포함), 봇 클리어 5·4·5·5·5 / 발각 5/5, 빌드 성공(예상 밖 경고 0), 성능 재측정(`plan/perf-report.md`, 평균 약 1ms, 예산 초과 0%, 로그 오류 0), GOAL D1~D9 재확인: 전체 테스트·봇(클리어 4/5 이상, 발각 5/5)·Unity 안 봇 재생·캡처 검토·빌드, 사람 재플레이용 빌드 실행, GOAL D1~D9 다시 체크

### 확인이 필요한 점 (기본값으로 진행, D-051)
- 무작위 대시 방향: 3D 전체(구면 균등) vs 수평만 → 기본값 3D 전체
- 공격 판정 방식 변경(고정 목표 구 → 움직이는 손 경로): 사람다운 대신 봇·밸런스 재조정 필요
- 걷기(자리 이동)는 이번 범위 밖, 고난도 확장 아이디어로 기록(D-053, plan/ideas.md)
- 사거리 축소(팔 길이 기반): 광분 손바닥 공격 빈도가 줄어 난이도가 내려갈 수 있음 → 봇·밸런스로 확인, 필요하면 몸 기울임·자세 변경 허용 폭으로 보정
- 와류 제어 3레벨 대체 효과: 대시 쿨타임 0.7배
- LAlt: Windows 창 모드에서 Alt+Enter는 Unity 플레이어의 전체화면 전환 단축키라, 하강 중 Enter를 누르면 화면 모드가 바뀔 수 있음(Enter는 게임 조작에 쓰지 않음)
