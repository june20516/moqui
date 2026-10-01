# 개발 규약

## 1. 엔진과 패키지
- Unity 6 LTS. M0에서 그 시점 최신 LTS 패치를 골라 `ProjectSettings/ProjectVersion.txt`에 고정하고 `plan/decisions.md`에 기록한다. 이후 업그레이드는 금지한다.
- 필수 패키지: Universal RP, Input System(Active Input Handling = Input System Package), Cinemachine 3.x, Unity Test Framework, TextMeshPro(uGUI 포함), ProBuilder(화이트박스용, 선택).
- 유료/로그인 필요 패키지는 쓰지 않는다.

## 2. 폴더
```
Packages/com.moqui.core/   Core 소스 (엔진 독립, tech/architecture.md §3)
dotnet/                    Core 빌드·테스트 프로젝트
data/                      tuning, levels, scenarios, animations, schema (JSON)
Assets/_Project/
  Scripts/<어댑터 어셈블리>/ (tech/architecture.md §6)
  Scenes/  Prefabs/  Materials/  Shaders/
  Art/<카테고리>/           외부 에셋은 출처별 하위 폴더
  Audio/
  Input/                   Input Actions 에셋
  Tests/EditMode/  Tests/PlayMode/   Unity 어댑터·표현 테스트
  CREDITS.md               에셋 출처·라이선스 (tech/asset-pipeline.md)
Tools/                     빌드/테스트 실행 스크립트 (sh, ps1)
Captures/                  캡처 결과 (git 제외)
Builds/                    빌드 결과 (git 제외)
```

## 3. 코드
- Core 규칙: `UnityEngine` 참조 금지, `System.Numerics` 수학 타입 사용, `DateTime.Now`·`Random`·정적 가변 상태 금지(시간은 틱, 난수는 `IRandom`), 리플렉션 기반 직렬화 대신 명시적 DTO 사용, C# 9 문법까지만 사용.
- C# 네이밍: 타입/메서드/프로퍼티 PascalCase, 필드 `_camelCase`, 지역 변수 camelCase.
- Unity 코드: `[SerializeField] private` 사용, public 필드 금지.
- Update 안에서 `Find`, `GetComponent`, LINQ 할당 금지.
- 매직 넘버 금지: tuning 값은 `data/tuning.json`에서, 그 외 상수는 이름 있는 const로.
- 주석은 "왜"를 설명할 때만 쓴다.
- 경고 0을 유지한다.

## 4. 테스트 이름
`<대상>_<조건>_<기대결과>` 예: `Dash_StaminaBelowCost_DoesNotExecute`. spec 체크박스의 증거로 이 이름을 적는다.

## 5. Git
- `.gitignore`: Unity 표준 (Library, Temp, Obj, Logs, UserSettings, Builds, Captures) + dotnet 산출물 (`dotnet/**/bin`, `dotnet/**/obj`).
- 대용량 바이너리(fbx, png > 1MB, wav, vrm)는 Git LFS로 관리한다 (`.gitattributes`).
- 브랜치: 마일스톤마다 `milestone/mN-<slug>` 브랜치에서 작업한다. 공용 브랜치를 upstream으로 설정하지 않는다: `git checkout --no-track -b milestone/m1-flight main`. 생성 후 `git status -sb`로 확인한다.
- 커밋: 반복 1회당 1개 이상. 메시지는 `[M1] 대시 쿨타임 구현 (spec/01)` 형식으로 쓴다.
- 마일스톤 종료 기준을 충족하면 main에 merge하고 태그 `m1-done`을 단다.
- 원격이 있을 때만 푸시하며, 반드시 `git push -u origin <브랜치명>` 형식을 쓴다.
