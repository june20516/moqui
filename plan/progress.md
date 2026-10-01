# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M0 프로젝트 골격** (진행 중)
- 다음 할 일: Unity 어댑터 asmdef + EditMode/PlayMode 샘플 테스트 → `Tools/run-tests`
- 브랜치: `milestone/m0-skeleton`

## 현재 마일스톤 체크리스트
- [x] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록 — 증거: `ProjectSettings/ProjectVersion.txt` = 6000.6.3f1, D-019, 배치 모드 임포트 exit 0
- [x] 필수 패키지 설치 (tech/conventions.md §1) — 증거: manifest(URP 17.6.0, Input System 1.20.0 + activeInputHandler=1, Cinemachine 6.6.0(D-023), Test Framework 1.8.0, uGUI 2.6.0(TMP 포함), ProBuilder 6.1.2), `Tools/unity-import.ps1` exit 0·컴파일 이슈 0
- [x] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3) — 증거: `SplitMix64RandomTests` 4개 통과, Unity 배치 모드에서 Moqui.Core.dll 컴파일 CS 에러/경고 0
- [x] `.gitignore`, `.gitattributes`(LFS) — 증거: `git check-attr filter -- a.png` → `lfs`, D-020
- [x] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트 — 증거: `TuningDocumentTests.TuningJson_EverySpecKey_HasMatchingValue`, `TuningJson_NoKeysOutsideSpec`, `Load_RepoTuningJson_Succeeds` (변조 시 실패 확인), `TuningTests`, `JsonReaderTests`
- [ ] `Tools/run-tests`, `Tools/build`, `Tools/capture` — `run-core-tests`(ps1 정본 + sh 래퍼) 완료, 나머지 남음
- [ ] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과 — dotnet 샘플 통과(`SplitMix64RandomTests`), Unity 샘플 남음
- [ ] 빈 씬 Standalone 빌드 성공

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | Unity Personal 활성화됨, 배치 모드 라이선스 초기화 확인 | - | 해결 |

## 막힘
(없음)

## 캡처 검토 기록
(없음)

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M0 | Core 데이터 계층(IDataSource, JsonReader, Tuning, TuningLoader), `data/tuning.json`, spec 문서 일치 검사 | Core 35/35 통과, Unity CS 이슈 0 | (이 커밋) |
| 2026-10-01 | M0 | 필수 패키지 설치, `Tools/unity-path.ps1`·`unity-import.ps1`, D-023 | unity-import exit 0, CS 이슈 0 | fd40a00 |
| 2026-10-01 | M0 | URP 프로젝트 생성(스크래치패드 생성 후 루트로 이동, 템플릿 튜토리얼 제거), 라이선스 배치 모드 확인 | Moqui.Core Unity 컴파일 CS 0건 | 28a2f7b |
| 2026-10-01 | M0 | Unity 버전 확정(D-019), Windows 기준 재확인(D-022), 번들 .NET SDK 사용(D-021), Core 패키지 + dotnet sln + IRandom 샘플 테스트, `Tools/run-core-tests` | `SplitMix64RandomTests` 4/4 통과 (ps1·sh 양쪽) | d26f851 |
| 2026-10-01 | M0 | 환경 조사(Unity/dotnet/라이선스), M0 브랜치, .gitignore/.gitattributes, D-019·D-020 기록, 사람 요청 R-001~003 | 위 체크리스트 | 2d60090 |
