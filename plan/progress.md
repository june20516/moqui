# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M0 프로젝트 골격** (진행 중, 사람 요청 대기)
- 다음 할 일: R-001~R-003 해결 대기. 해결되면 Unity 프로젝트 생성 → Core/dotnet 골격
- 브랜치: `milestone/m0-skeleton`

## 현재 마일스톤 체크리스트
- [ ] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록 — 차단: R-001(버전), R-003(라이선스)
- [ ] 필수 패키지 설치 (tech/conventions.md §1) — 차단: 프로젝트 생성
- [ ] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3) — 차단: R-002(.NET SDK)
- [x] `.gitignore`, `.gitattributes`(LFS) — 증거: `git check-attr filter -- a.png` → `lfs`, D-020
- [ ] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트 — 차단: R-002
- [ ] `Tools/run-tests`, `Tools/build`, `Tools/capture`
- [ ] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과
- [ ] 빈 씬 Standalone 빌드 성공

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| R-001 | Unity 버전 확정 | 설치본은 6000.6.3f1(비 LTS). conventions는 LTS 요구 → 6000.3.25f1 설치(Windows 모듈) 또는 6000.6.3f1 사용 승인 (D-019) | 없음 | 대기 |
| R-002 | .NET SDK 설치 | 런타임(3.1/6/8/10)만 있고 SDK 없음. `dotnet test`에 SDK 필요 (예: `winget install Microsoft.DotNet.SDK.8`) | 없음 | 대기 |
| R-003 | Unity 로그인 + 라이선스 활성화 | `unity license status` → active: false, 로그인 안 됨. 배치 모드 테스트/빌드에 필요 (`unity auth login` → `unity license activate`, Personal) | 없음 | 대기 |

## 막힘
- Unity 프로젝트 생성·Unity 테스트·빌드: R-001, R-003
- Core/dotnet·tuning 로더 테스트: R-002

## 캡처 검토 기록
(없음)

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
| 2026-10-01 | M0 | 환경 조사(Unity/dotnet/라이선스), M0 브랜치, .gitignore/.gitattributes, D-019·D-020 기록, 사람 요청 R-001~003 | 위 체크리스트 | (이 커밋) |
