# M10 에셋 패스 — 체크리스트 (보관)

태그 `m10-done` (2026-10-01).

### 아트 (spec/10)
- [x] 모든 렌더러가 프로젝트 공통 툰 셰이더(또는 그 변형)를 사용한다 (Unity: 씬·프리팹 머티리얼 검사). — `Moqui/Toon`(셀 3단·남보라 그림자·월드 외곽선), `Moqui/ToonTransparent`(림), 기체 `SoftGas`·`VolumeFog`. PlayMode `StageSceneTests`가 5개 스테이지의 모든 렌더러 셰이더가 `Moqui/*`이고 지원됨을 검사
- [x] 누락/오류 셰이더(마젠타)가 없다 (Unity 검사 + 캡처 검토). — `shader.isSupported` 검사 + `Captures/2026-10-01_165955` 검토: 마젠타 없음, 밤 실내 팔레트(라벤더·나무·청회색, 따뜻한 키 라이트·남색 환경광)
- [x] 플레이어 캐릭터가 위 최소 애니메이션 7종을 가진다 (애니메이터 상태 검사). — `MokiBuilder`가 모키(2.5등신·반투명 날개 2장·지팡이 주둥이·리본)와 `Art/Generated/Moki` 클립 7종·컨트롤러를 생성, `MokiAnimator`가 Core 상태→State 파라미터, `PlayerView`가 이동 기울기. EditMode `MokiTests`(매핑·7상태·전이 조건·바인딩 경로) 통과, 캡처 `Captures/2026-10-01_170956/Moki_*.png`
- [x] 인간이 머리 회전과 공격 3단계 애니메이션을 가진다. — `HumanView`: 얼굴(눈·코) 피벗이 Core `HeadForward`를 따라 회전, 공격 팔(어깨→주먹)이 `HumanArmPose`로 예고(뒤로 치켜듦)→타격(목표로)→회복(복귀). EditMode `HumanArmPoseTests`, PlayMode `SandboxHumanSceneTests`(얼굴 방향) 통과, 캡처 `Captures/2026-10-01_171446/Sandbox_Human_attack_*.png`
- [x] 각 스테이지 캡처에서 플레이어가 배경과 구분된다 (캡처 검토, `plan/progress.md`에 소견 기록). — 캡처 검토 기록 2026-10-01 M10 참조(다섯 스테이지 시작 3인칭에서 분홍 모키가 안개·벽과 구분, Stage 4 역광은 자체 밝기 0.3으로 보완)

- [x] (spec/10 목표 상태) 포스트프로세싱: Bloom(약)·Color Grading. — `PostProcessBuilder`가 `Rendering/PostProcess_Night.asset`(Bloom 0.35·Neutral 톤 매핑·대비·채도·그림자 남보라/하이라이트 따뜻하게)을 모든 씬 골격에 전역 Volume으로 둔다. EditMode `SceneIntegrityTests`(약한 Bloom·Color Grading) + 셰이더 컴파일 오류 검사(`ProjectShaders_CompileWithoutErrors`, 일부러 깨뜨린 셰이더로 실패 확인)

### 사운드 (spec/10)
- [x] 위 사운드 ID가 모두 존재하고 이벤트에 연결되어 있다 (Unity: AudioCatalog 검사). — 31종 `tools/gen_audio.py` 합성(D-049), `Resources/Audio/AudioCatalog`, `AudioCues`(이벤트·상태→ID)·`AudioDirector`(Stage)·`AudioOutput`, UI 버튼 select/confirm/cancel, 메뉴 bgm_title. EditMode `AudioTests`(카탈로그·스펙 목록·생성 폴더 일치·ID 참조·이벤트 매핑·대시·반복음), PlayMode `StageSceneTests`(음악·환경음·날갯소리) 통과
- [x] (M8 이월) 음악 볼륨 설정이 BGM에 적용된다 (Unity). — `UserSettings.ApplyToEngine` → `AudioVolumes`, EditMode 검사 + PlayMode에서 음악 볼륨 변경이 다음 프레임 BGM 음량에 반영

### 에셋 파이프라인 (tech/asset-pipeline.md)
- [x] `CREDITS.md`의 모든 행이 허용 목록 라이선스이고 URL과 확인일이 있다. — `Assets/_Project/CREDITS.md`(외부 에셋 0행, 자체 제작물 목록), EditMode `CreditsTests` 행 검사
- [x] `Assets/_Project/Art`, `Audio` 아래 모든 외부 파일이 CREDITS에 있다 (Unity: 파일 목록 대조 검사, 자체 제작물은 `Generated/` 폴더로 구분). — `CreditsTests.EveryExternalFile_IsCredited`(현재 40개 파일 모두 `Generated/`)
- [x] 사람 요청 항목이 비어 있지 않다면, 각 항목에 대체물이 적용되어 게임이 동작한다. — 에셋 관련 사람 요청 없음(R-001~003은 환경, 모두 해결). 모키·사운드는 조달 1·2순위(자체 생성)로 충족(D-049)
