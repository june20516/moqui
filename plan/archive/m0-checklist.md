# M0 프로젝트 골격 — 체크리스트 (보관)

태그 `m0-done` (2026-10-01).

- [x] Unity 프로젝트 생성 (URP 템플릿), 버전 고정 및 decisions 기록 — 증거: `ProjectSettings/ProjectVersion.txt` = 6000.6.3f1, D-019, 배치 모드 임포트 exit 0
- [x] 필수 패키지 설치 (tech/conventions.md §1) — 증거: manifest(URP 17.6.0, Input System 1.20.0 + activeInputHandler=1, Cinemachine 6.6.0(D-023), Test Framework 1.8.0, uGUI 2.6.0(TMP 포함), ProBuilder 6.1.2), `Tools/unity-import.ps1` exit 0·컴파일 이슈 0
- [x] Core 패키지(`Packages/com.moqui.core`, noEngineReferences) + `dotnet/` 빌드·테스트 프로젝트 (tech/architecture.md §3) — 증거: `SplitMix64RandomTests` 4개 통과, Unity 배치 모드에서 Moqui.Core.dll 컴파일 CS 에러/경고 0
- [x] `.gitignore`, `.gitattributes`(LFS) — 증거: `git check-attr filter -- a.png` → `lfs`, D-020
- [x] `data/tuning.json`(spec/tuning.md 전체) + 로더 + 문서 일치 검사 테스트 — 증거: `TuningDocumentTests.TuningJson_EverySpecKey_HasMatchingValue`, `TuningJson_NoKeysOutsideSpec`, `Load_RepoTuningJson_Succeeds` (변조 시 실패 확인), `TuningTests`, `JsonReaderTests`
- [x] `Tools/run-tests`, `Tools/build`, `Tools/capture` — 증거: `run-tests.ps1` exit 0 (Core 35, EditMode 1, PlayMode 1), `build.ps1` exit 0, `capture.ps1` exit 0 → `Captures/2026-10-01_123452/Boot.png`. 각 ps1에 Git Bash 래퍼(sh)
- [x] `dotnet test` 샘플 1개, Unity EditMode/PlayMode 샘플 각 1개 통과 — 증거: `SplitMix64RandomTests`, `Moqui.Unity.Tests.UnityDataSourceTests.Load_InEditor_ReadsRepoTuning`, `Moqui.Unity.Tests.PlayModeSmokeTests.Tuning_LoadedInPlayMode_SurvivesFrame`
- [x] 빈 씬 Standalone 빌드 성공 — 증거: `Builds/Windows/Moqui.exe`, BuildScript result=Succeeded errors=0 unexpected warnings=0 (D-024), StreamingAssets/data/tuning.json 포함 확인
- [x] Unity 어댑터 asmdef (Runtime/Presentation/UI/Editor, Tests.EditMode/PlayMode, D-025) — 증거: `unity-import.ps1` 컴파일 이슈 0

