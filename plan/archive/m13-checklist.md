# M13 플레이테스트 반영 2 — 체크리스트 (보관)

태그 `m13-done` (2026-10-05).

> 2026-10-05 사람 플레이테스트 피드백. 사람 결정: 키 배치 제안 A, 흡혈 중 몸 고정(대시 탈출 허용), 관망 모드, 흡혈 이벤트 3종, 스테이지는 계획 수립.

| # | 피드백 | 원인(확인) | 반영 위치 |
|---|---|---|---|
| 1 | 정밀 + 하강을 함께 누르기 어렵다 | LCtrl + LAlt (새끼손가락 + 엄지) | spec/01 입력 매핑 |
| 2 | 흡혈 중에는 몸이 고정되어야 | `AttachSystem`이 흡혈 중에도 이동 입력으로 이탈 | spec/04 §2, spec/01 상태 |
| 3 | 바람에 밀릴 때 효과음 | 바람 소리 없음 | spec/10 |
| 4 | 천장에서 방이 훤히 보였으면 | 1인칭: 안개(선명 50·최대 160u)가 방을 가림. 3인칭: 피벗이 월드 위로 올라가다 천장에 막히고 뒤로도 못 빠져 모키 실루엣이 화면을 덮음 (탐침 캡처) | spec/00, spec/11 |
| 5 | 스테이지가 작다, 많이 찍고 레벨링 | 방 5개, 요소 조합이 적음 | `plan/stage-expansion.md` (M14 계획) |
| 6 | 광분을 거의 안 봐도 클리어 | 측정 필요 | spec/02, tuning |
| 7 | 흡혈 중 이벤트가 적다 | 흡혈 중 변화는 가려움 반응뿐 | spec/04 (D-056) |
| 8 | 벽·구석·팔꿈치 아래에서 시야가 튀고 까매짐 | 좁은 곳에서 카메라가 몸까지 당겨져 모키(외곽선 색)가 화면을 덮음, 충돌 결과를 매 프레임 즉시 반영 | spec/00 |

### 작업 순서
- [x] A. 키 배치(#1)·흡혈 중 몸 고정(#2)·바람 소리(#3) — 증거: EditMode `CommandCollectorTests`(C 하강·LShift 정밀), Core `SuckTests.WhileSucking_MoveInput_BodyStaysFixed`·`WhileSucking_EscapeInputs_Detach`, EditMode `AudioTests.Wind_LoopFollowsStrength_GustOnceOnEntering`, 음원 2종 합성
- [x] B. 카메라(#4·#8) — 증거: EditMode `CameraTests.ThirdPerson_AttachedToCeiling_PivotFollowsSurfaceNormal`·`ThirdPerson_Blocked_PullsInAtOnce_ReturnsAtReturnSpeed`·`ThirdPerson_CameraCrampedAgainstBody_HidesPlayer`·`ToonOutline_WidthScalesWithViewDistance`, 캡처 `Captures/2026-10-05_191556/Stage_*_ceiling_tp·corner_tp·under_forearm_tp.png`: 붙은 면 법선 기준 3인칭 피벗, 당김 즉시·복귀 완만, 몸에 가까우면 모키 숨김, 외곽선 껍질 제한, 회귀 캡처
- [x] C. 관망(#4) — 증거: EditMode `SensesFogTests.Perch_AttachedAndStill_WidensClearAndFullRanges`, 캡처 `Stage_stage01_ceiling_fp.png`: 붙어 있으면 감각 안개 범위 확대 (tuning `perch.*`)
- [x] D. 광분 빈도(#6) — 증거: `ScenarioDiagnostics.FrenzyStats` 측정, D-057(빗나감 경계 실험 → 철회, 시선 들킴 +70 채택): 봇·시뮬레이션으로 광분 진입률 측정 → 규칙 조정
- [x] E. 흡혈 중 이벤트(#7) — 증거: Core `SuckEventTests`(7), EditMode `HudTests.SuckEventWarnings_ShowDuringSessionOnly`, 봇 클리어 5·4·5·4·4, `DesignValidationTests` 통과: 긁으러 오는 손, 부위가 움직임, 시선 (D-056)
- [x] F. 스테이지 확장 계획(#5) — 증거: `plan/stage-expansion.md`(넓은 방, 4장 × 5, 소개 → 조합 → 광분 → 응용, 요소 목록 위협·해법, 사람 결정 항목 4개): `plan/stage-expansion.md`
- [x] G. 마무리 — 증거: 빌드 성공(예상 밖 경고 0), 성능 재측정 `plan/perf-report.md`(평균 약 1 ms, 예산 초과 0%, 플레이어 로그 오류 0), GOAL D3~D5 재체크, `plan/final-report.md` M13 반영: 봇·빌드·성능, spec 체크, 최종 보고서
