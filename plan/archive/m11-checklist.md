# M11 폴리시 · 릴리스 — 체크리스트 (보관)

태그 `m11-done` (2026-10-01).

### 폴리시 · 릴리스 (plan/milestones.md, GOAL.md §2)
- [x] 게임패드로 Title → StageSelect → Stage 1~5 → Ending 전 흐름 진행 확인 (D2, 키보드/마우스 포함) — 증거: PlayMode `FullFlowPlayModeTests.TitleThroughAllStagesToEnding(KeyboardMouse)`(Title 시작은 마우스 클릭, 메뉴는 방향키·Enter), `(Gamepad)`(D-pad·South). 메뉴는 장치 입력만으로 조작, 클리어는 흡혈 게이지 주입(실제 클리어 가능성은 Core 봇, D5), Stage 5 첫 클리어 → 엔딩 보기 → Ending, 다섯 스테이지 기록 저장
- [x] 성능 측정: `perf.targetFps` 기준 프레임 타임 로그 기록 — 증거: `plan/perf-report.md`(빌드 실행 `-moquiPerf`, 1920×1080, RTX 3060 Ti·i5-12400F, VSync 끔): 다섯 스테이지 평균 0.98~1.28 ms, p99 ≤ 1.83 ms, 60fps 예산(16.67 ms) 초과 0.0%(stage05 단발 최대 17.9 ms 1회). EditMode `PerfTests`(통계·인자)
- [x] spec/ 모든 문서 수용 기준 체크박스를 증거와 함께 체크 (D3, 보관 체크리스트에서 동기화) — spec/00~11 143개 + asset-pipeline 3개 = 146개 모두 `- [x] … — 증거: …`(보관 체크리스트 m0~m10의 증거를 문구 대조로 옮기고, 여러 마일스톤에 나뉜 4개는 증거를 합침). 남은 `- [ ]` 0개
- [x] 봇 전체 플레이스루 동안 Error/Exception 로그 0건 (D6) — 증거: PlayMode `ScenarioSmokeTests.ClearScenario_InStageScene_MatchesHeadlessCore_NoErrorLogs`(stage01~05 클리어 봇을 Stage 씬 안에서 재생, 헤드리스 Core와 결과·틱·광분·자국 일치, Result까지 `LogAssert.NoUnexpectedReceived`, D-050), Core 시나리오 10종(`ScenarioTests`, 예외 시 실패), 빌드 성능 실행 플레이어 로그 Error/Exception 0건
- [x] 버그 정리 (알려진 결함·경고 점검) — 증거: **결함 수정** ① 씬에 AudioListener가 없어 소리가 들리지 않음 → AudioOutput에 리스너 1개(PlayMode `StageSceneTests` 리스너 1개 검사) ② 종료 시 AudioDirector가 파괴된 출구의 AudioSource를 멈추다 NullReferenceException → Unity null 검사(PlayMode `AudioPlayModeTests.OutputDestroyedFirst_StageTeardownDoesNotThrow`) ③ 스테이지에서 커서를 잠근 뒤 일시정지·결과·메뉴에서 풀지 않아 마우스로 버튼을 못 누름 → `CursorPolicy`(플레이 중에만 잠금, PlayMode `FlowPlayModeTests.Cursor_LockedOnlyWhilePlaying`) ④ 일시정지 중에도 날갯소리 등 반복음이 계속 남 → 일시정지 동안 상태 반복음 끔(`AudioPlayModeTests.Pause_SilencesStateLoops_KeepsMusicAndAmbience`). ⑤ 화면이 여는 첫 포커스에도 선택음이 남 → `UiFactory.Focus` 동안 억제 ⑥ 공유 one-shot 소스의 pitch 인자 제거(오용 방지). M10~M11 변경분 독립 코드 리뷰(확정 결함 없음). 컴파일 경고 0(Core·Unity), TODO/FIXME 0
- [x] GOAL D1~D9 체크와 `plan/final-report.md` (D9) — GOAL.md §2 전 항목 증거와 함께 체크, `plan/final-report.md`
