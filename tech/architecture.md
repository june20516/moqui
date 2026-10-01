# 아키텍처

## 1. 목표
게임 규칙(코어)은 엔진에 의존하지 않는 C# 라이브러리로 만들고, Unity는 **표현과 입력을 맡는 어댑터**로만 쓴다. (D-008)

- 코어는 Unity 없이 `dotnet test`로 빌드·검증할 수 있어야 한다.
- 코어는 다른 C# 환경(Godot C#, MonoGame 등)으로 그대로 옮길 수 있어야 한다.
- 레벨, 수치, 테스트 시나리오는 언어 중립 데이터(JSON)로 둔다. 다른 언어로 다시 구현하더라도 같은 데이터와 같은 시나리오로 검증할 수 있어야 한다.

## 2. 레이어
```
┌────────────────────── Unity (어댑터 / 표현) ──────────────────────┐
│  입력: Input System → PlayerCommand                               │
│  표현: 스냅샷/이벤트 → 트랜스폼 보간, 애니메이션, VFX, 사운드      │
│  카메라(3인칭/1인칭), HUD/UI, 화면 흐름, 설정(PlayerPrefs)         │
│  포트 구현: ISaveStorage, ILogger                                  │
└───────────────▲──────────────────────────────┬───────────────────┘
       Snapshot / Events (읽기 전용)     PlayerCommand (틱마다)
┌───────────────┴──────────────────────────────▼───────────────────┐
│                     Moqui.Core (엔진 독립)                         │
│  Simulation: 고정 틱 루프, 엔티티, 시스템, 상태머신                  │
│  Collision: 기본 도형 충돌 월드 (ray / sweep / overlap)             │
│  Data: tuning, level, scenario 로더 (JSON)                         │
│  Meta: 보상, 업그레이드, 저장 포맷                                  │
│  Bots: BotRoute 재생 (헤드리스 시나리오)                            │
└──────────────────────────────────────────────────────────────────┘
```

**의존 규칙:** Core → (없음). Unity → Core. 반대 방향은 금지한다. Core가 바깥에 알릴 것은 이벤트와 스냅샷으로, 바깥에서 받을 것은 커맨드와 포트 인터페이스로만 주고받는다.

### 2.1 무엇이 Core이고 무엇이 Unity인가
판단 기준은 하나다. **결과가 승패, 생사, 게이지, 판정에 영향을 주면 Core에 둔다.** 영향을 주지 않으면 Unity에 둔다.

| Core | Unity |
|---|---|
| 이동, 대시, 스태미나, 부착, 바람 | 카메라, 시점 전환, 1인칭 렌더링 |
| 충돌, 시야(LoS), 소음 전파 | 메시, 머티리얼, 셰이더, 포스트프로세싱 |
| 인간 상태머신, 머리 방향, 공격 타이밍과 판정 | 인간 애니메이션 (Core의 상태와 타이밍을 따라 재생) |
| 흡혈, 가려움, 물방울, 거미줄, 승패 | HUD, 화면 흐름, 비네트, 사운드 |
| 보상, 업그레이드, 저장 데이터 포맷 | 저장 파일 위치, 설정 값 |

## 3. 저장소 배치
```
Packages/com.moqui.core/          Unity 임베디드 패키지 = Core 소스의 정본
  package.json
  Runtime/Moqui.Core.asmdef       noEngineReferences: true
  Runtime/**/*.cs
dotnet/
  Moqui.Core/Moqui.Core.csproj    netstandard2.1, LangVersion 9 (Unity 6 컴파일러와 맞춤)
                                  <Compile Include="../../Packages/com.moqui.core/Runtime/**/*.cs" />
  Moqui.Core.Tests/               NUnit 테스트 프로젝트 (Core 테스트의 정본)
  Moqui.sln
data/                             언어 중립 데이터 정본
  tuning.json
  rooms/livingRoom.json …        방 구조(벽, 가구, Shadow Zone). 여러 스테이지가 공유
  levels/stage01.json …          방 참조 + 인간 + 기믹 + 시작 위치
  scenarios/*.json                봇 경로 + 기대 결과
  animations/*.json               베이크된 판정 프록시 트랙 (§4.6)
Assets/_Project/                  Unity 어댑터와 표현
```
- Core 소스는 한 벌만 있다. Unity는 패키지로 컴파일하고, dotnet은 같은 파일을 링크해 컴파일한다.
- `noEngineReferences: true`이므로 Core에서 `UnityEngine`을 쓰면 Unity에서 컴파일 에러가 난다. dotnet 빌드에서도 마찬가지다.
- `data/`는 에디터에서는 직접 읽는다. 빌드할 때는 빌드 전처리 단계(`IPreprocessBuildWithReport`)에서 `StreamingAssets/data/`로 복사한다.

## 4. Core 설계
### 4.1 시뮬레이션 루프
- 고정 틱 60Hz (`Simulation.TickRate`). `Step(PlayerCommand)` 한 번이 1틱이다.
- Unity는 누적 시간으로 0~N틱을 돌리고, 직전 스냅샷과 현재 스냅샷 사이를 보간해서 그린다.
- 결정성: 같은 레벨, 같은 시드, 같은 커맨드 열을 넣으면 같은 결과가 나와야 한다 (같은 플랫폼 기준, 부동소수 오차 허용). 난수는 시드를 가진 `IRandom`을 쓰고, 시간은 틱 수로만 계산한다.
- 난수 스트림은 용도별로 분리한다 (인간 동작, 반응, 맹목 휘두르기, 디버프, 분사기). 레벨 시드에서 각 스트림 시드를 파생한다. 한 시스템을 고쳐도 다른 시스템의 난수열이 바뀌지 않게 하기 위해서다.

### 4.2 입력: PlayerCommand
```
PlayerCommand { Move(x, z), Vertical, LookYaw, LookPitch, DashPressed, PrecisionHeld,
                AttachPressed, SuckHeld }
```
- 버튼은 "이번 틱에 눌렸는지(edge)"와 "누르고 있는지(held)"로 구분한다.
- 카메라는 Core 밖에 있으므로, Unity가 현재 카메라 yaw를 커맨드에 담아 보낸다.
- 키 매핑과 장치(키보드/게임패드)는 Unity만 안다.

### 4.3 출력: Snapshot과 Event
- `Snapshot`: 렌더링에 필요한 읽기 전용 상태 (플레이어 위치/상태/게이지/중독/포만, 인간 몸 캡슐 포즈/머리 방향/경계/광분/공격 단계, 물방울, 연무, 선풍기 각도, 감각 데이터(호흡, 피부·자국, 은신처, InShadow, PlayerVisibleToHuman) 등).
- `Event`: 1회성 사건 (`NoiseEmitted`, `AwarenessStateChanged`, `AttackTelegraphStarted`, `PlayerDied(DeathCause)`, `StageCleared(StageResult)` 등). 틱마다 목록으로 내보낸다.

### 4.4 모델링 방식
- 엔티티(Player, Human, WaterDrop, Fan, Web, …)는 **데이터를 가진 단순한 객체**로 둔다. 규칙은 틱마다 정해진 순서로 실행되는 **시스템**에 둔다.
  - 시스템 순서: Input(디버프 필터) → HumanActions(무작위 움직임) → Movement(Flight/Dash/Wind/부착 추종) → Collision → Hazards(Water/Humid/Web/Spray/Coil) → Sensing(Vision/Hearing/Touch) → Awareness → Reaction(확률 반응) → HumanBrain(광분) → Attacks → Suck/BiteMark → Outcome.
  - 이 게임은 엔티티 수가 적으므로 ECS 프레임워크는 쓰지 않는다. 엔티티 + 시스템 분리만 지킨다.
- 상태를 가진 행동(플레이어 Flying/Attached/Dashing/Trapped/Dead, 인간 Safe/Suspicious/Frenzy, 공격 Telegraph/Active/Recovery)은 명시적인 상태머신으로 구현한다. 상태 전이는 모두 테스트 대상이다.
- 공격, 업그레이드, 기믹 수정자(취함 등)는 코드가 아닌 **정의 데이터**로 기술한다. 새 공격은 데이터만 추가해서 만들 수 있어야 한다.
- 수학 타입은 `System.Numerics.Vector3`, `Quaternion`을 쓴다. Unity 어댑터에서만 `UnityEngine.Vector3`로 변환한다.

### 4.5 충돌 월드
- 형상: Box(회전 포함 OBB), Sphere, Capsule. 각 형상은 ID와 플래그(`obstacle`, `attachable`, `skinSite`, `shadowZone`, `hazard`, `wind`)를 가진다.
- 질의: `Raycast`, `SphereSweep`(플레이어 이동과 대시), `Overlap`(판정과 영역 진입), `ClosestSurface`(부착 거리와 법선).
- 성능: 레벨당 형상은 수백 개 수준이므로 처음에는 전수 검사로 구현한다. 프로파일링으로 필요성이 확인될 때만 공간 분할을 추가한다.
- Unity 물리 엔진(Collider, Rigidbody, Physics.*)은 게임 규칙에 쓰지 않는다. 카메라 충돌처럼 표현에만 필요한 경우는 Unity 쪽에서 써도 된다.

### 4.6 판정 프록시 (복잡한 모델 대응)
그래픽 메시가 아무리 복잡해져도 **판정은 기본 도형 프록시로만** 한다. 대부분의 액션 게임이 쓰는 히트박스/허트박스 방식과 같다.
- **가구·소품:** 레벨 데이터의 박스/캡슐이 판정이다. 아트 메시는 형상 ID에 붙는 겉모습일 뿐이다. 프록시는 메시의 외곽을 ±10% 안에서 따르도록 맞춘다.
- **인간:** 뼈대 단위 캡슐 묶음(머리, 목, 몸통, 위팔·아래팔, 손, 허벅지, 종아리)이 판정이다. SkinSite는 이 캡슐 표면의 구간으로 정의한다.
- **인간의 움직임:** 두 가지 방법을 허용한다.
  1. **절차적 포즈 (기본):** Core가 머리 yaw/pitch와 손 목표 위치를 계산하고 캡슐을 갱신한다. Unity는 IK로 모델을 그 포즈에 맞춘다. Core가 원인이므로 판정과 그림이 어긋나지 않는다.
  2. **애니메이션 베이크:** 손으로 만든 애니메이션(Mixamo 등)을 쓸 때는 `Moqui.Unity.Editor.ProxyBaker`가 클립을 샘플링해 캡슐 트랙(틱 단위 키프레임)을 `data/animations/*.json`으로 내보낸다. Core는 이 트랙을 재생해 판정을 갱신하고, Unity는 같은 클립을 같은 시각으로 재생한다. 모델이나 클립을 바꾸면 다시 베이크한다.
- **부착 시 시각 보정:** 플레이어는 Core에서 캡슐 표면에 붙는다. 실제 메시와의 작은 틈이나 파고듦은 Unity가 그 자리에서 메시에 광선을 쏴 **그림 위치만** 보정한다 (게임 결과에는 영향 없음).
- **삼각형 메시 판정은 범위 밖이다.** 정말 필요해지면 Core에 BVH 기반 메시 형상을 추가할 수 있다. 그러나 이 게임에서는 정밀도 이득에 비해 비용(성능, 데이터 크기, 베이크 파이프라인)이 크므로 decisions에 사유를 남긴 뒤에만 도입한다.

### 4.7 포트 (Core가 정의하고 바깥에서 구현)
| 포트 | 용도 | Unity 구현 | 테스트 구현 |
|---|---|---|---|
| `IDataSource` | tuning/level/scenario JSON 읽기 | StreamingAssets 또는 에디터 경로 | 파일 시스템 |
| `ISaveStorage` | 세이브 바이트 읽기/쓰기 | persistentDataPath | 메모리 |
| `ILogger` | 로그 | Debug.Log | 수집용 리스트 |

## 5. 데이터 포맷
- **tuning.json:** `spec/tuning.md`의 키를 평평한 키-값으로 둔다 (`"dash.cooldown": 0.5`). 범위 값은 `{min, max}`, 레벨별 값은 배열로 쓴다.
- **room JSON:** `id`, `bounds`, `shapes[]`(id, type, center, size/radius, rotation, flags: obstacle/attachable/glass/shadowZone 등).
- **level JSON:** `id`, `room`, `human`(자세, 위치, 정면, 몸 캡슐, skinSites(유형), actions[], canSpray, modifiers[]), `dripSources[]`, `humidZones[]`, `fans[]`, `webs[]`, `coils[]`, `sprayDispensers[]`, `playerSpawn`, `tutorial`, `parTime`, `seed`.
- **scenario JSON:** `level`, `seed`, `upgrades`, `steps[]`(웨이포인트 + 행동, 또는 틱 단위 커맨드 열), `expect`(결과: Cleared/Died, DeathCause, 최대 광분 횟수, 최대 자국 수, 제한 틱).
- 모든 포맷에 `formatVersion`을 둔다. 포맷 스키마는 `data/schema/*.json`(JSON Schema)으로 문서화한다.

## 6. Unity 어댑터 구성
| 어셈블리 | 내용 |
|---|---|
| `Moqui.Unity.Runtime` | `SimulationRunner`(틱 구동과 보간), 커맨드 수집, 스냅샷 → 뷰 동기화, 레벨 데이터 → 화이트박스/아트 생성, 포트 구현 |
| `Moqui.Unity.Presentation` | 카메라, 캐릭터 뷰(`PlayerVisual`, `HumanVisual`), VFX, 오디오 |
| `Moqui.Unity.UI` | 화면 흐름, HUD, 설정 |
| `Moqui.Unity.Editor` | 빌드, 캡처, 데이터 동기화, ProxyBaker, 검사 도구 |
| `Moqui.Unity.Tests` | Unity EditMode/PlayMode 테스트 (어댑터와 표현 전용) |

- MonoBehaviour는 Core 상태를 **읽어서 그리기만** 한다. MonoBehaviour 안에서 게이지를 계산하거나 판정을 내리면 안 된다.
- 씬 구성: `Boot` → `Title` → `StageSelect` → `Stage`(레벨 ID를 받아 데이터로 구성) → `Ending`. 스테이지 씬은 하나이고 레벨 데이터만 바꿔 끼운다. 조명이나 분위기처럼 스테이지마다 손으로 만든 표현 요소는 레벨 ID별 프리팹으로 둔다.
- 테스트용 씬: `Sandbox_Flight`, `Sandbox_Human`, `Sandbox_Water` (눈으로 확인하고 캡처하는 용도, 빌드에는 포함하지 않음).

## 7. 다른 엔진·언어로 옮길 때
- **C# 엔진:** `Packages/com.moqui.core/Runtime`과 `data/`를 그대로 가져가고, 어댑터(§6)만 새로 만든다.
- **다른 언어:** `spec/`, `data/`, `data/scenarios/`가 이식 기준이다. 새 구현이 같은 시나리오에서 같은 `expect`를 통과하면 규칙상 동등하다고 본다.
