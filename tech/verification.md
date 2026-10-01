# 검증 방법

에이전트는 게임을 직접 "느껴볼" 수 없다. 그래서 모든 완료 주장은 아래 증거 중 하나 이상으로 뒷받침해야 한다. spec 수용 기준의 `(Core)`, `(Unity)`, `(캡처)` 표기가 어떤 방법을 쓸지 정한다.

## 1. Core 테스트 — `dotnet test` (게임 규칙의 주 검증 수단)
- 위치: `dotnet/Moqui.Core.Tests` (NUnit). Unity 없이 실행한다.
- 대상: spec에서 `(Core)`로 표기된 모든 기준. 이동, 충돌, 대시, 감지, 상태머신, 공격 판정, 흡혈, 물방울, 기믹, 보상, 저장 포맷.
- 시뮬레이션을 직접 만들고 `Step(command)`을 원하는 틱만큼 호출해 검증한다. 실시간 대기는 쓰지 않는다.
- 데이터 검사 테스트:
  - `spec/tuning.md`의 표와 `data/tuning.json`의 수치가 일치하는지 검사한다 (문서를 파싱해서 비교).
  - 레벨 데이터 규칙을 검사한다: 플래그, 좌표, DripSource 높이, 시작 위치 안전성, SkinSite 유형, 도망칠 곳 보장.
  - `data/schema`에 맞는지 검사한다.
- **확률 규칙 테스트:** 확률 기반 규칙(반응, 디버프, 무작위 동작)은 두 가지로 검증한다.
  - 재현성: 같은 시드면 같은 결과.
  - 통계: 고정 시드 묶음으로 충분히 많이(예: 10,000회) 시행해 기대값 허용오차 안에 드는지 확인한다. 실행 시간이 길면 `[Category("Statistical")]`로 분리하고 마일스톤 종료 때 실행한다.
- **설계 검증 시나리오:** 의도한 플레이가 실제로 유리한지 시나리오 비교로 확인한다 (예: spec/04의 "긴 세션 2회 vs 짧은 세션 6회").

## 2. Unity 테스트 — Unity Test Framework (어댑터와 표현)
- 위치: `Assets/_Project/Tests` (EditMode/PlayMode).
- 대상: spec에서 `(Unity)`로 표기된 기준. 입력 매핑과 게임패드, 카메라, 시점 전환, HUD 반영, 화면 흐름, 설정 저장, 레벨 데이터 ↔ 시각 오브젝트 대응, 머티리얼/오디오 카탈로그 검사.
- 게임 규칙을 Unity 테스트에서 다시 검증하지 않는다. 규칙은 Core 테스트가 맡는다.
- 입력은 Input System 테스트 유틸리티(`InputTestFixture`)의 가상 장치로 주입한다.
- 통합 스모크 테스트 1개: Stage 1 클리어 시나리오를 Unity 안에서 재생해, Core 결과와 같고 Error 로그가 0건인지 확인한다.

## 3. 시나리오 봇 (헤드리스, Core)
- `data/scenarios/*.json`을 Core의 `ScenarioRunner`가 재생하고 `expect`와 비교한다.
- 스테이지마다 최소 두 개를 둔다.
  - **클리어 봇:** 광분 없이(또는 `expect.maxFrenzies` 이내로) 흡혈 100% 도달 → `StageCleared`.
  - **발각 봇:** 인간 정면으로 직진 → Red Zone → `PlayerDied(Attack)`.
- 인간의 움직임과 반응에 무작위성이 있으므로, 클리어 봇은 고정 시드 5개 중 4개 이상에서 성공해야 한다. 특정 시드에서만 통과하는 경로는 레벨이나 봇의 결함으로 본다.
- 시나리오는 언어 중립 데이터이다. 다른 구현에서도 같은 파일로 검증한다 (`tech/architecture.md` §7).
- 레벨이나 수치를 바꾸면 시나리오도 함께 갱신한다. 봇이 클리어할 수 없다면 레벨 설계 결함으로 본다.

## 4. 빌드와 캡처
`Tools/`에 다음 스크립트를 둔다.

| 스크립트 | 동작 | 산출물 |
|---|---|---|
| `run-tests` | `dotnet test` → Unity EditMode/PlayMode (배치 모드) 순서로 실행. Core가 실패하면 Unity 단계는 건너뛴다 | `Logs/test-results-*.xml` |
| `run-core-tests` | `dotnet test`만 실행 (빠른 반복용) | 콘솔 출력 |
| `build` | `Moqui.Unity.Editor.BuildScript.BuildStandalone` 실행 | `Builds/<OS>/` |
| `capture` | `Moqui.Unity.Editor.CaptureTool.CaptureAll` — 스테이지별 카메라 포즈 4장(3인칭) + 1인칭 1장 + HUD 해상도 3장 | `Captures/<date>/*.png` |

- 캡처는 그래픽이 필요하므로 `-nographics` 없이 실행한다.
- 캡처 이미지는 에이전트가 직접 열어 검토하고, 소견(마젠타, 깨진 UI, 가독성)을 `plan/progress.md`에 기록한다.

## 5. 반복 종료 체크리스트
1. Core와 Unity 모두 컴파일 에러/경고 0
2. `run-core-tests` 통과 (Core를 수정한 반복). 마일스톤을 종료할 때와 Unity 쪽을 수정한 반복에서는 `run-tests` 전체 통과
3. 이번 반복에서 체크한 spec 항목에 증거(테스트 이름 또는 캡처 경로)를 기록했는가
4. `plan/progress.md` 갱신, 커밋
