# Moqui

PC용 3D 스텔스 비행 액션 게임. 플레이어는 마법소녀 외형의 모기다.
게임 규칙은 엔진 독립 C# 라이브러리(Core)에 있고, Unity(URP)는 표현·입력 어댑터이다 (`tech/architecture.md`).

## 반드시 먼저 읽을 것
1. `GOAL.md` — 완료 정의, 범위, 루프 운영 규칙
2. `plan/progress.md` — 현재 마일스톤과 다음 할 일

## 핵심 규칙 요약
- 구현 기준은 `spec/`과 `spec/tuning.md`이다. `prd/`는 비전, `knowledge/`는 배경 지식이다.
- Core(`Packages/com.moqui.core`)에서 `UnityEngine`을 참조하지 않는다. 게임 규칙을 Unity 쪽에 구현하지 않는다.
- 수치를 코드에 하드코딩하지 않는다. 모든 수치는 `data/tuning.json`(정본 문서는 `spec/tuning.md`)에서 읽는다.
- 테스트 없이 완료를 선언하지 않는다 (`tech/verification.md`).
- spec에 없는 결정은 `plan/decisions.md`에 기록한다.
- 문서는 한국어, 코드 식별자는 영어로 쓴다.
