# 마일스톤

순서대로 진행한다. 각 마일스톤은 **종료 기준**이 모두 충족되어야 끝난다. 공통 종료 기준은 `tech/verification.md` §5이다.

| ID | 이름 | 관련 문서 | 종료 기준 |
|---|---|---|---|
| M0 | 프로젝트 골격 | tech/* | Unity 프로젝트 생성, 패키지 설치, Core 패키지와 `dotnet/` 프로젝트, Unity 어댑터 asmdef, `.gitignore`/LFS, `data/tuning.json`과 로더, `Tools/run-tests`·`build`·`capture` 동작, 샘플 테스트 각 1개 통과, 빈 씬 빌드 성공 |
| M1 | Core 충돌 월드 · 비행 · 카메라 | spec/00, 01, tech/architecture §4 | 기본 도형 충돌 질의(ray/sweep/overlap) 테스트, spec/00, 01 수용 기준 전부. `Sandbox_Flight` 씬 |
| M2 | 인간 감지 · 어그로 · 광분 | spec/02 §1~4 | 시각, 귀 기준 청각, 상태머신, 광분 기준 전부. `Sandbox_Human` 씬, 캡슐 인간 |
| M3 | 공격 · 반응 · 무작위 움직임 · 사망 | spec/02 §5~7, spec/04 §7 | 공격 정의 전부, 확률 반응(통계 테스트 포함), 무작위 동작과 튕겨남, DeathCause, 재시도 초기화 |
| M4 | 스텔스 | spec/03 | spec/03 전부 |
| M5 | 흡혈 세션 · 물린 자국 · 포만 · 승리 | spec/04 | spec/04 전부 (설계 검증 시나리오 포함) |
| M6 | 물방울 QTE · 습기 | spec/05 | spec/05 전부. `Sandbox_Water` 씬 |
| M7 | **거실 버티컬 슬라이스 (Stage 1·2)** | spec/07 (거실, Stage 1·2), spec/06 (졸음), spec/08 (HUD, 튜토리얼), spec/11 | 거실 화이트박스, 졸음 수정자, 튜토리얼 안내, HUD 전체, 모기 감각 표현(흐린 시야, CO₂, 체온, 은신처 표시), Stage 1·2 클리어 봇/발각 봇 통과, 캡처 검토. → `plan/progress.md`에 "버티컬 슬라이스 리포트" 작성 (사람 검토 권장 시점) |
| M8 | 화면 흐름 · 스킬 트리 · 저장 | spec/08 (흐름/설정/Skills), spec/09 | spec/08 나머지, spec/09 전부 (액티브 스킬 포함) |
| M9 | Stage 3 · 4 · 5와 기믹 | spec/05, spec/06, spec/07 | 선풍기, 모기약 스프레이, 모기향, 거미줄, 취한 타겟 기준 전부, 습기·물방울의 스테이지 적용, Stage 3/4/5 레벨 데이터 검사와 봇 통과 |
| M10 | 에셋 패스 | spec/10, tech/asset-pipeline | spec/10, asset-pipeline 수용 기준. 기존 테스트와 봇 전부 유지 |
| M11 | 폴리시 · 릴리스 | GOAL.md §2 | 게임패드 전 흐름 확인, 성능 측정(`perf.targetFps` 기준 프레임 타임 로그 기록), 버그 정리, GOAL D1~D9 |
| M12 | 플레이테스트 반영 | spec/01·02·03·04·08·09·11 (M12 표시 항목) | 2026-10-01 사람 플레이테스트 피드백 8건 반영, 해당 spec 기준 체크, 기존 테스트·봇 유지, 다시 GOAL D1~D9 |

## 마일스톤 내부 진행 순서 (공통)
1. 해당 spec의 수용 기준을 `plan/progress.md` 체크리스트로 복사한다.
2. Core 규칙 + `dotnet test` → Unity 어댑터/뷰 연결 + Unity 테스트 → Sandbox 씬 확인 → 캡처(필요 시).
3. 종료 기준 충족 → main merge, 태그 → progress에 다음 마일스톤 설정.
