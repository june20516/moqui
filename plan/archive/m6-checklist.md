# M6 물방울 QTE · 습기 — 체크리스트 (보관)

태그 `m6-done` (2026-10-01). 이월: DripSource 높이 검사의 실제 레벨 적용 → M7 (D-036).

- [x] 물방울이 2.5초 간격으로 생성된다 (Core). — 증거: `WaterTests.Drops_SpawnEvery25Seconds`, `Drops_FallAndVanishOnFloor`
- [x] 물방울에 닿으면 Trapped가 되고 이동 입력이 무시된다 (Core). — 증거: `WaterTests.DropHitsPlayer_Trapped_MoveInputIgnored`, `DropPassesPlayer_AnyHeight_AlwaysTraps`
- [x] Trapped 중 낙하 속도가 100u/s 등속이다 (Core, ±2%). — 증거: `WaterTests.Trapped_FallsAtConstant100`
- [x] 대시 2회 입력 시 탈출하고, 스태미나가 줄지 않는다 (Core). — 증거: `WaterTests.DashTwice_Escapes_StaminaUnchanged`
- [x] 탈출 전 바닥 충돌 시 WaterImpact로 사망한다 (Core). (spec/04 §7 DeathCause.WaterImpact 원인별 테스트 겸함 — D-031) — 증거: `WaterTests.TrappedUntilFloor_DiesWithWaterImpact`
- [x] 탈출 후 10초간 속도 0.7배, 회복 0.5배, 대시 비용 35 (Core). — 증거: `WaterTests.AfterEscape_WetWingsFor10Seconds_SpeedRegenDashCost`
- [x] 젖은 날개 중 다시 젖으면 지속시간만 10초로 갱신된다 (Core). — 증거: `WaterTests.WetAgain_DurationRefreshedNotStacked`
- [ ] 모든 레벨의 DripSource가 착지면 기준 150u 이상이다 (Core: 레벨 데이터 검사 — 아래로 광선 검사). (검사 함수는 M6, 실제 레벨 적용은 M7 — D-036) — 진행: 검사 함수 `LevelChecks.DripSourceHighEnough` + `WaterTests.DripSourceHeight_CheckedByDownwardRay`. 실제 레벨 적용은 M7
- [x] 습기 게이지가 강/약 영역에서 15/5 per s로 오르고, 밖에서 10/s로 내린다 (Core). — 증거: `WaterTests.Humidity_StrongWeakOutside_15_5_Minus10`, `Humidity_RisesWhileAttached`
- [x] 습기 100에서 젖은 날개가 걸리고, 영역 안에 머무는 동안 지속시간이 갱신된다 (Core). — 증거: `WaterTests.Humidity_Reaches100_WetWingsAndRefreshedWhileInside`
- [x] 증기 안의 플레이어에 대한 시각 증가율이 0.6배이다 (Core). — 증거: `WaterTests.Steam_VisionRateIs06Times`, `Steam_IntegratedInHumanSystem`
- [x] 숨은 상태에서 젖은 날개 시간과 습기 게이지가 2배 빠르게 줄어든다 (Core). (spec/03 2배 회복 기준의 젖은 날개·습기 부분 겸함 — D-034) — 증거: `WaterTests.Hidden_WetAndHumidityRecoverTwiceAsFast`

### 마일스톤 산출물
- [x] `Sandbox_Water` 씬 (빌드 제외) — 증거: `Assets/_Project/Scenes/Sandbox_Water.unity`, `SandboxWaterSceneTests.SandboxWater_Play_SpawnsAndDrawsDrops`, 캡처 `Captures/2026-10-01_141811/Sandbox_Water_*.png`

