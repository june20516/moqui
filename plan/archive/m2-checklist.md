# M2 인간 감지 · 어그로 · 광분 — 체크리스트 (보관)

태그 `m2-done` (2026-10-01).

- [x] 상태 전이와 히스테리시스(40 진입 / 20 이탈)가 동작한다 (Core). — 증거: `HumanAwarenessTests.StateTransitions_Hysteresis_Enter40Exit20`
- [x] 자극이 없으면 2초 후부터 10/s로 감소한다 (Core). — 증거: `HumanAwarenessTests.NoStimulus_DecaysAfter2SecondsAt10PerSecond`, `NoStimulus_PlayerHidden_UsesShadowDecayRate`
- [x] Yellow Zone 안, 시야 확보 시 거리별 증가율이 25→8/s 선형이다 (Core). — 증거: `HumanVisionTests.YellowZone_LineOfSight_RateIsLinearFrom25To8`(40/150/299u), `YellowZone_IntegratedOverOneSecond_AwarenessRisesByRate`
- [x] 장애물이 시선을 막으면 시각 증가가 0이고, 스냅샷의 PlayerVisibleToHuman이 거짓이다 (Core). — 증거: `HumanVisionTests.Obstacle_BlocksLineOfSight_NoVisionGainAndNotVisible`, `Hidden_InYellowZone_NotVisibleAndNoGain`
- [x] Red Zone 진입 시 같은 틱에 박수 공격이 시작되고 광분이 된다 (Core). — 증거: `HumanVisionTests.RedZone_EnteredWithLineOfSight_ClapAndFrenzySameTick`
- [x] 소리 거리는 가까운 귀 기준이다: 머리를 돌리면 같은 위치의 소음 증가율이 달라진다 (Core). — 증거: `HumanHearingTests.FlightNoise_HeadTurned_SamePositionGivesDifferentRate`, `FlightNoise_InsideRadiusOutsideEarZone_UsesNearestEarDistance`
- [x] 비행 소음 증가율이 귀 거리 0에서 16/s, 반경 끝에서 4/s이다 (Core). — 증거: `HumanHearingTests.FlightNoise_EarDistanceZero_Is16AndRadiusEdge_Is4`, `FlightNoise_Precision_HalvesRadius`
- [x] 귀 근접 구역에서 +30/s가 추가된다 (Core). (귀 반응 발생은 M3 §5에서 검증) — 증거: `HumanHearingTests.EarZone_Inside_Adds30PerSecond`
- [x] 대시 NoiseEvent가 반경 안의 인간 경계를 즉시 +40 한다 (Core). — 증거: `HumanHearingTests.DashNoise_EarInsideRadius_RaisesAwarenessBy40Immediately`, `DashNoise_EarOutsideRadius_NoChange`
- [x] 광분은 최소 8초 유지되고, 그 뒤 6초 연속 보지 못하면 경계 60, Suspicious가 된다 (Core). — 증거: `FrenzyTests.Frenzy_NeverSeen_LastsMinimum8SecondsThenCalmsTo60`, `Frenzy_SeenUntil10Seconds_CalmsAfter6UnseenSeconds` (D-030)
- [x] 광분 중 다시 보이면 연속 미발견 시간이 초기화된다 (Core). — 증거: `FrenzyTests.Frenzy_SeenAgainWhileHidden_ResetsUnseenTimer`
- [x] 광분 중 보이지 않아도 마지막 위치 주변에 맹목 휘두르기가 발생한다 (Core). — 증거: `FrenzyTests.Frenzy_PlayerNotVisible_BlindSwatsAroundLastSeenPosition`
- [x] 머리가 yawLimit를 넘어 회전하지 않는다 (Core). — 증거: `HumanAwarenessTests.Head_StimulusBehind_NeverExceedsYawLimit`
- [x] 같은 시드와 같은 입력이면 인간의 움직임과 반응이 똑같이 재현된다 (Core). (M2 범위: 머리·경계·공격. 무작위 동작·반응은 M3에서 재검증) — 증거: `FrenzyTests.SameSeedAndCommands_HumanBehaviourReplaysExactly` (다른 시드는 다른 결과)

### 마일스톤 산출물
- [x] `Sandbox_Human` 씬, 캡슐 인간 (빌드 제외) — 증거: `Assets/_Project/Scenes/Sandbox_Human.unity`, `SandboxHumanSceneTests.SandboxHuman_Play_BuildsHumanViewFromSimulation`, 캡처 `Captures/2026-10-01_133118/Sandbox_Human_*.png`

