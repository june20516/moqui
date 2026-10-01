# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M0 프로젝트 골격** (진행 중)
- 다음 할 일: `data/tuning.json` + 로더 + 문서 일치 검사 테스트 (Unity 작업은 R-003 대기)
- 브랜치: `milestone/m0-skeleton`

## 현재 마일스톤 체크리스트
- [ ] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록 — 버전 확정(D-019), 차단: R-003(라이선스)
- [ ] 필수 패키지 설치 (tech/conventions.md §1) — 차단: 프로젝트 생성
- [ ] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3) — dotnet 쪽 완료(증거: `Moqui.Core.Tests.Random.SplitMix64RandomTests` 4개 통과). Unity에서 asmdef 컴파일 확인은 프로젝트 생성 후
- [x] `.gitignore`, `.gitattributes`(LFS) — 증거: `git check-attr filter -- a.png` → `lfs`, D-020
- [ ] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트
- [ ] `Tools/run-tests`, `Tools/build`, `Tools/capture` — `run-core-tests`(ps1 정본 + sh 래퍼) 완료, 나머지는 Unity 프로젝트 생성 후
- [ ] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과 — dotnet 샘플 통과(`SplitMix64RandomTests`), Unity 샘플 남음
- [ ] 빈 씬 Standalone 빌드 성공

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 6000.6.3f1 사용으로 사람이 확정 (D-019) | - | 해결 |
| R-002 | .NET SDK 설치 | 시스템에는 런타임만 있음(9/30에 설치된 것은 .NET 10 런타임). Unity 번들 SDK 8.0.318로 대체 (D-021) | 적용 | 해결 |
| R-003 | Unity 로그인 + 라이선스 활성화 | `unity license status` → active: false, 로그인 안 됨. 배치 모드 테스트/빌드에 필요 (`unity auth login` → `unity license activate`, Personal) | 없음 | 대기 |

## 막힘
- Unity 프로젝트 생성·Unity 테스트·빌드: R-003

## 캡처 검토 기록
(없음)

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M0 | Unity 버전 확정(D-019), Windows 기준 재확인(D-022), 번들 .NET SDK 사용(D-021), Core 패키지 + dotnet sln + IRandom 샘플 테스트, `Tools/run-core-tests` | `SplitMix64RandomTests` 4/4 통과 (ps1·sh 양쪽) | (이 커밋) |
| 2026-10-01 | M0 | 환경 조사(Unity/dotnet/라이선스), M0 브랜치, .gitignore/.gitattributes, D-019·D-020 기록, 사람 요청 R-001~003 | 위 체크리스트 | 2d60090 |
