# 진행 상태

> 루프가 매 반복 끝에 갱신한다. 위에서부터 최신순으로 쓴다.

## 현재
- 마일스톤: **M0 프로젝트 골격** (시작 전)
- 다음 할 일: Unity 6 LTS 버전 확정 → 프로젝트 생성
- 브랜치: -

## 현재 마일스톤 체크리스트
- [ ] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록
- [ ] 필수 패키지 설치 (tech/conventions.md §1)
- [ ] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3)
- [ ] `.gitignore`, `.gitattributes`(LFS)
- [ ] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트
- [ ] `Tools/run-tests`, `Tools/build`, `Tools/capture`
- [ ] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과
- [ ] 빈 씬 Standalone 빌드 성공

## 사람 요청
| ID | 요청 | 필요 사양 | 대체물 적용 여부 | 상태 |
|---|---|---|---|---|
| (없음) | | | | |

## 막힘
(없음)

## 캡처 검토 기록
(없음)

## 반복 로그
| 일시 | 마일스톤 | 한 일 | 증거 | 커밋 |
|---|---|---|---|---|
