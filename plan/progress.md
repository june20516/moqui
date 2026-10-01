# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M12 플레이테스트 반영** (계획 수립, 착수 전 — 사람 확인 대기)
- 다음 할 일: 계획 확인 후 A(입력·대시)부터
- 브랜치: `main` (태그 `m11-done`), 착수 시 `milestone/m12-playtest`

## 현재 마일스톤 체크리스트 (M12 플레이테스트 반영 — 계획, 착수 전)
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
- [ ] A. 입력·대시 (#6, #7, #1): Input Actions 매핑 변경과 키 표기(HUD 프롬프트·튜토리얼 문구), Core `DashDirectionResolver`를 진행 방향 + 무작위(시드 스트림)로 교체, 부착 중 대시 = 법선 방향 대시, 와류 제어 3레벨 = 쿨타임 0.7배. Core 테스트(방향·무작위 재현·부착 대시·스킬) + Unity 입력 매핑 테스트, 시나리오 봇 재검증(봇이 대시를 쓰는 경로 확인)
- [ ] B. 벽·천장 부착 (#2): Core 테스트(벽 옆면·천장 아랫면 부착, 부착 위치 유지), `PlayerView`가 부착 중 `Player.Up`에 맞춰 회전(모키가 벽·천장에 앉은 자세), 부착 프롬프트·카메라 확인, 캡처(천장 부착)
- [ ] C0. 사람 팔 동작 근거 조사 (#11): 성인 팔 길이(어깨→손끝)·관절 가동 범위·손바닥 치기/휘두르기 손 최고 속도·반응 시간·박수 동작을 자료로 확인해 `knowledge/human-arm-motion.md`에 출처와 함께 정리하고, `spec/tuning.md`에 수치 키(팔 길이 비율, 관절 한계, 손 최고 속도, 자세별 기울임) 제안
- [ ] C1. 인간 뼈대 포즈 (#13 기반): 레벨 데이터의 고정 캡슐 → 뼈대(루트·골반·척추·어깨·팔꿈치·손목·목·머리) + 뼈에 붙은 캡슐, 루트를 상태로. 기존 자세(앉기·눕기·고개 숙임)·actions·SkinSite·판정이 그대로 나오는지 회귀 테스트, 레벨 데이터 변환
- [ ] C2. 몸동작 계획기 (#13): 목표에 닿기 위한 단계(팔 → 기울이기 → 돌기 → 일어서기, `human.maxPosture` 상한), 자세 전환 시간, 일어서면 머리·시야 이동. 광분 손바닥·맹목 휘두르기·반응이 모두 계획기를 거침
- [ ] C. 실제 팔 공격 (#3, #11): Core 팔 2관절 포즈(어깨·팔꿈치 각도 한계 안), 손 경로 = 어깨 → 팔꿈치 → 손목 순서의 호, 타격 시간 = 경로 길이 ÷ 손 최고 속도, 닿는 거리 밖 목표는 때리지 않음, 내 팔에 앉은 모기는 반대 손, 박수는 두 손, 판정 = 움직이는 손 구의 경로. Core가 공격 단계에 따라 가까운 어깨 쪽 upperArm·forearm 캡슐을 움직인다(어깨 고정 2관절, 예고 치켜듦 → 판정 목표로 뻗음 → 회복). 표현용 팔(`HumanArmPose`의 별도 팔·주먹) 제거, `HumanView`는 Core 형상만 따름. 그 팔에 붙은 모기는 따라가거나 튕겨 남(기존 dislodge 규칙). Core 테스트 + 봇 재검증 + 공격 단계 캡처
- [ ] D. 자국 위치 (#5): Core에 자국 목록(부위 ID + 부위 로컬 좌표, 세션의 부착 지점) 추가·스냅샷, `SensesView`가 문 자리에 점을 그리고 부위를 따라 움직임. 자국 수 n 규칙은 그대로
- [ ] E. 흐림 강화 (#4): 제안값(clearRange 50, fogFullRange 160, fogMaxDensity 0.88, fogBlurPixels 6)으로 캡처 비교 → 인간은 흐림에 묻히고 CO₂·가구 실루엣은 보이는 값으로 `spec/tuning.md`·`data/tuning.json` 확정, 겹눈 스킬 효과 재확인
- [ ] H. 체온 표시 재디자인 (#9): 굵은 캡슐 막 → 피부 윤곽 림/아지랑이처럼 일렁이는 얇은 선(셰이더), 존재감 낮게·인지는 되게. 캡처로 공격 예고보다 낮은 위상 확인
- [ ] I. 공격 예고 동적 표현 (#10): 예고 진행률만큼 차오르는 링·좁혀 오는 테두리·일렁임, 판정 순간 번쩍임, 손 접근 방향 표시. 화면에서 가장 눈에 띄는지 캡처 검토(체온·CO₂·은신처 표시와 비교)
- [ ] F. 포만 아이콘 (#8): 원 대신 배부른 배/추 모양 + "포만" 글자, HUD 테스트·3해상도 캡처
- [ ] G. 마무리: 전체 테스트·봇(클리어 4/5 이상, 발각 5/5)·Unity 안 봇 재생·캡처 검토·빌드, 사람 재플레이용 빌드 실행, GOAL D1~D9 다시 체크

### 확인이 필요한 점 (기본값으로 진행, D-051)
- 무작위 대시 방향: 3D 전체(구면 균등) vs 수평만 → 기본값 3D 전체
- 공격 판정 방식 변경(고정 목표 구 → 움직이는 손 경로): 사람다운 대신 봇·밸런스 재조정 필요
- 걷기(자리 이동)는 이번 범위 밖, 고난도 확장 아이디어로 기록(D-053, plan/ideas.md)
- 사거리 축소(팔 길이 기반): 광분 손바닥 공격 빈도가 줄어 난이도가 내려갈 수 있음 → 봇·밸런스로 확인, 필요하면 몸 기울임·자세 변경 허용 폭으로 보정
- 와류 제어 3레벨 대체 효과: 대시 쿨타임 0.7배
- LAlt: Windows 창 모드에서 Alt+Enter는 Unity 플레이어의 전체화면 전환 단축키라, 하강 중 Enter를 누르면 화면 모드가 바뀔 수 있음(Enter는 게임 조작에 쓰지 않음)

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
- **M8 화면 흐름 · 스킬 트리 · 저장** — 태그 `m8-done` (2026-10-01). 체크리스트: `plan/archive/m8-checklist.md`. 이월: 해독 체질 적용 → M9, 음악 볼륨 → M10. 사람 요청(CO₂·증기 기체형) 반영 (D-045)
- **M11 폴리시 · 릴리스** — 태그 `m11-done` (2026-10-01). 체크리스트: `plan/archive/m11-checklist.md`. 전 흐름 테스트(D2), spec 체크박스 동기화(D3), 성능 측정, Unity 안 봇 재생 스모크(D6), 결함 수정 6건, 최종 보고서
- **M10 에셋 패스** — 태그 `m10-done` (2026-10-01). 체크리스트: `plan/archive/m10-checklist.md`. 툰 셰이더·팔레트·후처리, 모키(애니메이터 7상태), 인간 머리·공격 팔, 사운드 31종 합성·카탈로그, CREDITS(D-049). 빌드 성공(예상 밖 경고 0)
- **M9 Stage 3 · 4 · 5와 기믹** — 태그 `m9-done` (2026-10-01). 체크리스트: `plan/archive/m9-checklist.md`. 봇: 다섯 스테이지 클리어 4/5 이상(Stage 4는 스킬 구성, D-048)

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 사람 검토 권장 (막힘 아님)
- **에셋 품질(D-049):** 모키·인간은 프리미티브 조합, 사운드 31종은 코드 합성이다. 수용 기준은 충족하지만 품질 향상을 원하면 VRoid 모델(같은 Animator 파라미터 `State`)이나 CC0 음원으로 교체할 수 있다.
- **한글 글꼴(D-041):** 허용 라이선스 목록에 OFL이 없어 한글 글꼴 파일을 넣지 못하고 OS 글꼴(맑은 고딕)을 실행 중에 씁니다. OFL(예: Pretendard, Noto Sans KR)을 허용 목록에 추가하면 M10에서 번들 글꼴 + TextMeshPro로 바꿀 수 있습니다.
- **밸런스(D-035, D-048):** Stage 4 봇은 스킬 없이 클리어하지 못한다(목 반응 → 자국 3개 → 의심 고정). 아울러: 설계 검증 봇 기준으로 자국 5개(하한 40 = 의심 진입선)가 되면 인간이 영구 의심 상태로 몸 주변을 훑어 접근이 거의 불가능하다. 긴 세션 전략도 40%가 자국 5개에서 멈춘다. 수치(`biteMark.floorPerBite` 8, `floorMax` 45, 반응률)는 spec 제안값 그대로 두었으며, M7 버티컬 슬라이스 리포트에서 플레이 감각과 함께 검토를 요청한다.

## 막힘
(없음)

## 캡처 검토 기록
- 2026-10-01 M10 `Captures/2026-10-01_173131/` 에셋 패스: 툰 셀 3단·외곽선·남보라 그림자, 밤 실내 팔레트(라벤더 벽·나무 바닥·남색 환경광), 약한 Bloom·색 보정. 마젠타 없음. **플레이어 구분 소견**: Stage 1·2(거실, 안개 속 분홍 모키 선명), Stage 3(침실, 침대 위 분홍), Stage 4(화장실, 역광 쪽이라 보라로 어두웠음 → 툰 자체 밝기 0.3 적용 후 분홍으로 구분), Stage 5(베란다, 거미줄 격자 앞에서도 구분). 모키 자세 7종(`Moki_*.png`), 인간 공격 3단계(`Sandbox_Human_attack_*.png`), HUD 3해상도 정상. **결함 수정**: 툰 셰이더 변수 이름 중복(`lit`)으로 전체 마젠타 → 수정 후 컴파일 오류 검사 테스트 추가
- 2026-10-01 M9 `Captures/2026-10-01_165113/` Stage 3~5: 침실(누운 인간·선풍기·옷장·침대 밑 은신처), 화장실(변기 위 고개 숙인 인간·유리 샤워 부스 안 증기), 베란다(취한 인간·테이블·거미줄 격자·연녹색 모기약 연무·모기향 받침과 연기·선풍기 머리 원판). CO₂ 바람 장면은 옅지만 바람 방향으로 기욺 → 인간을 헤드보드에서 10u 띄우고 촬영 시점을 날숨·바람 정면에 맞춤. 마젠타 없음. 작은 방(화장실) 전경 카메라가 벽에 가까움 — 아트 단계에서 포즈 조정
- 2026-10-01 M8 `Captures/2026-10-01_160546/` 기체 표현: CO₂는 코에서 흩어지는 분홍 기체 줄기(노이즈로 일렁임, 소프트 파티클), 증기는 상자 윤곽 없는 뭉게 덩어리(가구 뒤 가리지 않음), 증기 안에서는 선명 시야가 줄어든 안개. 첫 시도(`160443`)에서 증기 윤곽이 상자 모양 → 가장자리 노이즈 침식으로 수정
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
| 2026-10-01 | M12 | 피드백 #13 기록(광분은 몸이 직접 움직여 닿기, 자리에서 자세 변경 → 걷기는 확장): spec/02 규칙·기준, 계획 C1·C2, plan/ideas.md, D-053 | 문서만 변경 | (이 커밋) |
| 2026-10-01 | M12 | 추가 피드백 4건 기록(체온 표시 위상, 공격 예고 동적 강조, 사람 팔 신체 제약, 도구 공격 아이디어): spec/02·11 규칙·기준, plan/ideas.md, D-052, 계획 C0·H·I | 문서만 변경 | 2f5ef1e |
| 2026-10-01 | M12 | 사람 플레이테스트 피드백 8건 기록: spec/01·02·03·04·08·09·11 규칙 수정과 미체크 기준 추가, M12 계획, D-051, GOAL D3 해제 | 문서만 변경 | 50e72ba |
| 2026-10-01 | M11 | 리뷰 반영(첫 포커스 선택음 억제, one-shot pitch 인자 제거), GOAL D1~D9 체크, 최종 보고서, M11 종료 | Core 336 + EditMode 126 + PlayMode 22 통과, 빌드 성공(예상 밖 경고 0) | 93e4a14 |
| 2026-10-01 | M11 | **결함 수정**: 커서 잠금이 메뉴에서 풀리지 않음(CursorPolicy), 일시정지 중 반복음. 경고·TODO 점검 | Core 336 + EditMode 126 + PlayMode 22 통과 | 9e2de7a |
| 2026-10-01 | M11 | Unity 안 시나리오 봇 재생 스모크(다섯 스테이지, D6): Core ScenarioRunner.Pilot, SimulationDriver.CommandOverride, StageBootstrap 시드·구동기 훅, 테스트 메모리 세션 공용화, D-050 | Core 336 + EditMode 126 + PlayMode 20 통과 | cf86d7c |
| 2026-10-01 | M11 | 성능 측정 도구(FrameStats·PerfRunner·Tools/perf.ps1)와 측정 보고서, **결함 수정**: AudioListener 없음(무음), 종료 시 오디오 NullReferenceException | Core 336 + EditMode 126 + PlayMode 15 통과, 빌드 성공, 플레이어 로그 오류 0 | 7b3609e |
| 2026-10-01 | M11 | 전 흐름 PlayMode 테스트(D2): 키보드/마우스·게임패드로 Title → Stage 1~5 → Ending | Core 336 + EditMode 123 + PlayMode 14 통과 | c17f925 |
| 2026-10-01 | M11 | spec 수용 기준 체크박스 동기화(D3): 146개 기준에 보관 체크리스트 증거 연결, spec/07·10 최신 캡처·셰이더 검사 증거 보완 | spec·asset-pipeline의 `- [ ]` 0개 | (이 커밋) |
| 2026-10-01 | M10 | M10 종료: 체크리스트 보관, 빌드 확인(성공, 예상 밖 경고 0), M11 체크리스트 | Tools/build.ps1 result=Succeeded | (이 커밋) |
| 2026-10-01 | M10 | CREDITS.md·파일 대조 검사, 후처리 Volume(Bloom·Color Grading), 모키 자체 밝기, 셰이더 컴파일 오류 검사, 진행 문서 정리(반복 로그 행 위치). **결함 수정**: 툰 셰이더 변수 중복(마젠타), 오디오 생성기 커밋 누락 | Core 336 + EditMode 123 + PlayMode 12 통과, 캡처 검토 | 78cf927 |
| 2026-10-01 | M10 | 사운드 31종 합성(tools/gen_audio.py), AudioCatalog·AudioCues·AudioDirector·AudioOutput, UI 버튼음·메뉴 음악, 음악 볼륨 적용, D-049 | Core 336 + EditMode 118 + PlayMode 12 통과 | 1bc4b53 |
| 2026-10-01 | M10 | 인간 얼굴(머리 회전)과 공격 팔 3단계, 단계 캡처 | EditMode 109 + PlayMode 12 통과, 캡처 검토 | 34bd5ef |
| 2026-10-01 | M10 | 모키 캐릭터·애니메이터 7상태·이동 기울기, 자세 캡처 | EditMode 102 + PlayMode 12 통과, 캡처 검토 | 11a6128 |
| 2026-10-01 | M10 | 공통 툰 셰이더·반투명 변형, 밤 실내 팔레트, 모든 렌더러 Moqui 셰이더 검사 | EditMode 91 + PlayMode 12 통과, 캡처 검토 | 82d3749 |
| 2026-10-01 | M9 | Unity 기믹 표현: GimmickView(선풍기 머리·모기향 연기·연무), 거미줄 격자 머티리얼(생성 텍스처), 중독 게이지 HUD·녹색 가장자리, CO₂ 바람 흩어짐, Stage 3~5 1:1·씬 테스트·대표 캡처, Stage 3 인간 위치 조정(봇 5/5). M9 종료 | Core 336 + EditMode 91 + PlayMode 12 통과, 빌드 경고 0, 캡처 검토 | (이 커밋) |
| 2026-10-01 | M9 | 베란다 방·Stage 5(취한 인간, canSpray, 선풍기, 거미줄 3, 모기향, 자동 분사기, 방충망 유리, 아이스박스 은신처), stage05_clear 5/5·stage05_detect 5/5, 봇은 자기 근처를 노린 예고에만 도망 | Core 336/336 통과(시나리오 10종) | bf2f3f6 |
| 2026-10-01 | M9 | 화장실 방·Stage 4(고개 숙인 인간, 샤워 부스 유리·물방울 4·습기 강/약), stage04_clear 4/5(스킬 구성)·stage04_detect 5/5, 시나리오 skills·precise, 봇 중독 반전 보정, D-048. **결함 수정**: 봇 러너가 레벨 기믹을 빠뜨림, Stage 4 몸통이 목을 덮음 | Core 시나리오 통과(stage03 4/5, stage04 4/5) | e5901b7 |
| 2026-10-01 | M9 | 인간 자세(facingPitch·restPitch), 레벨 기믹 배열 파서·스키마, 침실 방·Stage 3(누운 인간, 선풍기, canSpray), stage03_clear 5/5·stage03_detect 5/5, 시나리오 hideRoutes, ScenarioDiagnostics, D-047 | Core 통과(시나리오 포함) | fd7f126 |
| 2026-10-01 | M9 | 기믹 Core: FanSystem(바람·회전·소음 마스킹), 거미줄(Webbed→Web 사망), ToxinSystem(연무·자동 분사기·모기향 하한·디버프·Spray 사망), 인간 분사, 취한 타겟(흡혈·가려움·시각 배율, 무작위 휘두르기), 해독 체질 적용, 스냅샷 기믹 상태, D-046 | Core 319/319 통과 | f238329 |
| 2026-10-01 | M8 | (사람 요청) CO₂·증기 기체형: SoftGas·VolumeFog 셰이더, GasNoise, 쿼드 빌보드 CO₂, 기체 캡처, D-045. M8 종료 | Core 300 + EditMode 82 + PlayMode 9 통과, 빌드 경고 0, 캡처 검토 | 3a66aef |
| 2026-10-01 | M8 | HUD 액티브 스킬 칸 테스트, 겹눈 선명 범위 테스트, 미끼 월드 표시 | Core 300 + EditMode 80 + PlayMode 9 통과 | 18ced4e |
| 2026-10-01 | M8 | 화면 흐름: GameSession·ScreenFlow·SceneNavigator, Title·StageSelect·Skills·Settings·Confirm·Stage(Pause/Result)·Ending·Boot, FileSaveStorage, UserSettings, Pause(틱·입력 정지), HUD 액티브 스킬 칸, 메뉴 캡처, D-044. **결함 수정**: 씬 생성기 메서드 손실 복구, 메뉴 글자 누락, 행 레이아웃 | Core 300 + EditMode 77 + PlayMode 9 통과, 빌드 경고 0, 캡처 검토 | 42912f0 |
| 2026-10-01 | M8 | Core 메타: SkillCatalog·SkillLoadout·SkillEffects(스킬 반영 Tuning), 연속 와류, 미끼(DecoySystem), 보상, SaveData·직렬화·SaveStore·SkillShop, D-043 | Core 300/300 통과 | 16446af |
| 2026-10-01 | M7 | 카메라 URP 후처리 켜기(은신 비네트 미표시 결함), 은신처·증기 양면 렌더링, 씬 무결성 테스트, 대표 캡처 검토, 버티컬 슬라이스 리포트. M7 종료 | Core 265 + EditMode 66 + PlayMode 6 통과, 빌드 경고 0, 캡처 검토 | (이 커밋) |
| 2026-10-01 | M7 | HUD(HudState·HudView·HudPresenter·HudController, 절차 스프라이트·경고음, OS 한글 글꼴), 튜토리얼(Core TutorialTracker, TutorialHints·설정), HUD 3해상도 캡처, D-041·D-042. **결함 수정**: 가장자리 표시 겹침·해상도별 위치, 누락 스크립트 | Core 265 + EditMode 64 + PlayMode 6 통과, 빌드 경고 0, 캡처 검토 | 977d12b |
| 2026-10-01 | M7 | HUD 원천 데이터(Core): PlayerOccluded(가려짐), FrenzyMinRemaining·CalmProgress, CanAttach, DashCost, 스냅샷 포함 | Core 261/261 통과 | 6833ed6 |
| 2026-10-01 | M7 | 감각 큐: Co2Plume·SenseCueModel·SensesView(CO₂·체온·자국·은신처 강도), 머티리얼 3개, tuning 키 11개, 캡처에 감각 반영, D-040 | Core 256 + EditMode 51 + PlayMode 6 통과, 캡처 검토 | 6ecbce1 |
| 2026-10-01 | M7 | 흐린 시야: SensesSettings·FogModel·SensesFog, 깊이 기반 안개+블러 셰이더, PC_Renderer 전체 화면 패스, tuning 키 2개, D-039 | Core 256 + EditMode 46 + PlayMode 6 통과, 캡처 검토 | 0159a69 |
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
