# GOAL — Moqui 자율 개발 목표

> 이 문서는 자율 루프의 **진입점**이다. 매 반복은 이 문서 → `plan/progress.md` 순으로 읽고 시작한다.
> 상태: **확정 v1 (2026-10-01, D-001·D-004·D-011 사람 확정)**

## 1. 북극성
"마법소녀 외형의 모기"가 되어 실내를 6DOF로 비행하며, 인간의 시각·청각을 피해 피를 빨고 살아남는 **PC용 3D 스텔스 액션**을, Title → Stage 1~5 → Ending 까지 끊김 없이 플레이 가능한 상태로 만든다.

비전·장르 설명은 `prd/v3.md`, 배경 지식은 `knowledge/`를 참고한다. **구현 기준은 오직 `spec/`과 `spec/tuning.md`이다.** 게임 규칙은 엔진 독립 Core에 구현한다 (`tech/architecture.md`). PRD와 spec이 충돌하면 spec이 우선한다.

## 2. 완료 정의 (Definition of Done)
아래 항목이 **모두** 증거와 함께 체크되면 루프를 종료한다.

- [x] D1. 빌드 스크립트(`tech/verification.md` §4)로 PC Standalone 빌드가 오류 없이 생성된다. — 증거: `Tools/build.ps1` result=Succeeded, errors 0, 예상 밖 경고 0 (`Builds/Windows/Moqui.exe`)
- [x] D2. Title → Stage Select → Stage 1 → 2 → 3 → 4 → 5 → Ending 흐름이 키보드/마우스와 게임패드 양쪽으로 진행 가능하다. — 증거: PlayMode `FullFlowPlayModeTests.TitleThroughAllStagesToEnding(KeyboardMouse)`, `(Gamepad)`
- [x] D3. `spec/` 모든 문서의 수용 기준 체크박스가 체크되어 있고, 각 항목에 검증 증거(테스트 이름 또는 캡처 경로)가 적혀 있다. — 증거: spec/00~11·asset-pipeline 모든 기준 `- [x]` + 증거 (M12 플레이테스트 반영분 포함, 미체크 0개)
- [x] D4. Core 테스트(`dotnet test`)와 Unity 테스트(EditMode/PlayMode)가 전부 통과한다. — 증거: Core 336 / EditMode 126 / PlayMode 22 통과 (`Tools/run-tests.ps1`)
- [x] D5. 시나리오 봇이 각 스테이지를 클리어하고, "의도적 발각" 봇은 Game Over에 도달한다 (`tech/verification.md` §3). — 증거: Core `ScenarioTests` 클리어 봇 5/5·4/5·5/5·4/5·5/5, 발각 봇 전 스테이지 5/5
- [x] D6. 봇 전체 플레이스루 동안 Error/Exception 로그가 0건이다. — 증거: PlayMode `ScenarioSmokeTests`(다섯 클리어 봇 Unity 재생, Error/Exception 0), 빌드 플레이어 로그 오류 0
- [x] D7. 에셋 패스 완료: 플레이스홀더가 아닌 모든 에셋이 `Assets/_Project/CREDITS.md`에 허용 라이선스로 기록되어 있고, 누락 머티리얼(마젠타)이 없다 (`tech/asset-pipeline.md`). — 증거: `Assets/_Project/CREDITS.md`·EditMode `CreditsTests`, 셰이더 컴파일·렌더러 셰이더 검사, 캡처 마젠타 없음
- [x] D8. 각 스테이지의 대표 캡처 스크린샷을 검토하고 `plan/progress.md`에 기록했다. — 증거: `plan/progress.md` "캡처 검토 기록"(M7·M9·M10)
- [x] D9. `plan/final-report.md` 작성: 구현 요약, 미해결 이슈, 사람에게 넘길 항목. — 증거: `plan/final-report.md`

## 3. 범위
**포함:** `plan/milestones.md`의 M0~M12 전체.

**범위 외 (구현 금지):**
- 멀티플레이, 온라인 기능, 업적, 클라우드 세이브
- 모바일/콘솔 빌드
- 키 리바인딩 UI (설정 화면은 감도/반전/볼륨/전체화면만)
- 컷신, 보이스, 스토리 대사
- 롤(roll) 축 회전 (6DOF는 "3축 이동 + yaw/pitch 시점"으로 해석, D-004)
- 잠자리 등 비인간 적 AI, 인간의 보행·자리 이동 (D-011)
- 유료 에셋 구매, 로그인이 필요한 외부 서비스 사용 (사람 요청 목록으로 넘김)

## 4. 루프 운영 규칙
1. **시작:** `GOAL.md` → `plan/progress.md` → 현재 마일스톤의 관련 `spec/` 문서 순으로 읽는다.
2. **작업 선택:** 현재 마일스톤에서 체크되지 않은 첫 항목을 고른다. 마일스톤은 순서대로 진행하며, 이전 마일스톤의 종료 기준이 충족되기 전에는 다음으로 넘어가지 않는다.
3. **작업 단위:** 한 반복에 수용 기준 1~3개 분량만 다룬다. 테스트를 먼저 쓰고(`tech/verification.md`) 구현한다.
4. **완료 선언:** 테스트 통과, 컴파일 에러 0, 콘솔 에러 0을 확인한 뒤에만 체크한다. 체크박스 옆에 증거를 남긴다.
   예: `- [x] 스태미나 < 25이면 대시 불가 — 증거: Core/StaminaTests.Dash_StaminaBelowCost_DoesNotExecute`
5. **기록:** 반복이 끝날 때마다 `plan/progress.md`를 갱신하고 커밋한다 (`tech/conventions.md` §5).
6. **spec에 없는 결정:** 가장 단순하고 되돌리기 쉬운 쪽을 택하고 `plan/decisions.md`에 기록한 뒤 진행한다. 수치를 바꾸려면 `spec/tuning.md`를 수정하고 decisions에 사유를 남긴다. 수용 기준 자체를 완화하는 것은 금지한다 (사람 요청으로 넘긴다).
7. **막힘:** 사람이 필요한 일(로그인, 구매, 환경 문제, 기획 판단)은 `plan/progress.md`의 "사람 요청"에 적고, 의존성이 없는 다음 항목으로 넘어간다. 같은 문제로 3회 연속 실패하면 반드시 이렇게 처리한다.
8. **회귀 금지:** 이미 체크된 기준을 깨뜨리는 변경은 커밋하지 않는다. 매 반복 마지막에 전체 테스트를 실행한다.
9. **종료:** §2의 D1~D9가 모두 체크되면 `plan/final-report.md`를 작성하고 루프를 멈춘다.

## 5. 문서 지도
| 경로 | 내용 |
|---|---|
| `spec/00-world-camera.md` | 월드 스케일, 단위, 카메라 |
| `spec/01-flight-controls.md` | 비행, 대시, 스태미나, 입력 매핑 |
| `spec/02-human-ai.md` | 인간(타겟 겸 적): 감지(귀), 어그로, 광분, 확률 반응, 무작위 움직임, 공격 |
| `spec/03-stealth.md` | 시야 차단, Shadow Zone, 벽면 부착 |
| `spec/04-blood-sucking.md` | 흡혈 세션, 부위 유형, 물린 자국, 포만, 승패 |
| `spec/05-water-drop.md` | 물방울 QTE, 습기, 젖은 날개 |
| `spec/06-gimmicks.md` | 선풍기, 모기약 스프레이, 모기향, 거미줄, 졸음·취함 수정자 |
| `spec/07-levels.md` | Stage 1~5 구성과 레이아웃 |
| `spec/08-ui-flow-hud.md` | 화면 흐름, HUD, 설정 |
| `spec/09-meta-progression.md` | 혈액 포인트, 스킬 트리, 저장 |
| `spec/10-art-audio.md` | 아트 디렉션, 사운드 목록 |
| `spec/11-mosquito-senses.md` | 흐린 시야, CO₂, 체온, 은신처 표시 |
| `spec/tuning.md` | **모든 수치의 단일 출처** |
| `tech/architecture.md` | 코드 구조, 시스템 경계 |
| `tech/conventions.md` | Unity 버전, 패키지, 폴더, 코드/깃 규칙 |
| `tech/verification.md` | 테스트, 시나리오 봇, 캡처, 빌드 |
| `tech/asset-pipeline.md` | 에셋 조달, 라이선스, 대체 규칙 |
| `plan/milestones.md` | M0~M11과 종료 기준 |
| `plan/progress.md` | 진행 상태 (매 반복 갱신) |
| `plan/decisions.md` | 결정 로그 |
