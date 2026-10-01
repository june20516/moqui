# M3 공격 · 반응 · 무작위 움직임 · 사망 — 체크리스트 (보관)

태그 `m3-done` (2026-10-01). 이월: 흡혈 연결 2개 → M5 (D-033), 사망 원인 WaterImpact → M6, Web·Spray → M9 (D-031).

- [x] 귀 근접 구역에서 귀 반응이 발생할 수 있다 (Core). (M2에서 이월) — 증거: `ReactionTests.EarZone_Airborne_ProducesEarReactionsAtEarRate`
- [x] 착지 반응 확률이 민감도에 비례한다: 같은 시드 10,000회 시행에서 기대값 ±2%p (Core, 통계 테스트). — 증거: `ReactionTests.LandingReaction_10000Trials_MatchesLandChanceTimesSensitivity`(5개 부위 유형), `LandingReaction_SameSeed_SameOutcomes`
- [x] 부착 중 반응 위험률이 가려움 제곱에 비례해 증가하고, 가려움 100이면 즉시 반응한다 (Core). — 증거: `ReactionTests.AttachedHazard_GrowsWithItchSquared`, `AttachedHazard_PerTickRolls_MatchExponentialProbability`, `Attached_Itch100_ReactsImmediately`
- [x] 모든 공격과 반응의 목표가 예고 시작 시점의 위치로 고정된다: 예고 중 판정 밖으로 이동하면 생존하고, 머무르면 사망한다 (Core). — 증거: `ReactionTests.ReactSlap_PlayerLeavesDuringTelegraph_Survives`, `ReactSlap_PlayerStays_DiesWithAttackCause`, `Clap_PlayerLeavesRedZoneDuringTelegraph_Survives`, `Clap_PlayerStays_Dies` (손바닥·맹목 휘두르기도 같은 `HumanAttackSystem.Start` 경로)
- [ ] 무작위 움직임이 흡혈 중에도 발생하고, 붙어 있는 모기가 부위를 따라 움직인다 (Core). (흡혈 세션 연결 검증은 M5) — 진행: `HumanMotionTests.Actions_ScheduledEvery4To9Seconds_MoveParts`, `Attached_DuringSlowAction_FollowsPartWithoutDislodging`, `Actions_KeepHappeningWhilePlayerIsAttached`. 흡혈 중 발생은 M5 (D-033)
- [ ] 부위 속도가 dislodgeSpeed를 넘으면 모기가 튕겨 나가고 흡혈 세션이 끝나며, 사망하지 않는다 (Core). (세션 종료 검증은 M5) — 진행: `HumanMotionTests.FastAction_PartFasterThanDislodgeSpeed_DislodgesWithoutDeath`. 세션 종료는 M5 (D-033)
- [x] 같은 시드와 같은 입력이면 인간의 움직임과 반응이 똑같이 재현된다 (Core). — 증거: `HumanMotionTests.SameSeed_MotionAndReactionsReplayExactly`(동작·부위 위치·반응·튕겨남 이벤트), `FrenzyTests.SameSeedAndCommands_HumanBehaviourReplaysExactly`
- [x] 공격 중에는 새 공격이나 반응이 시작되지 않는다 (Core). — 증거: `ReactionTests.AttackInProgress_NoNewAttackOrReactionUntilRecoveryEnds`
- [ ] 각 사망 원인이 올바른 DeathCause로 기록된다 (Core, 원인별 1개 테스트). (M3: Attack. WaterImpact는 M6, Web·Spray는 M9) — 진행: Attack 완료(`ReactionTests.ReactSlap_PlayerStays_DiesWithAttackCause`)
- [x] 재시도 시 흡혈 게이지, 가려움, 자국, 인간 경계, 플레이어 위치, 중독 게이지가 모두 초기화된다 (Core). — 증거: `RetryTests.Retry_AfterPlaying_EveryStateMatchesFreshStart`(스냅샷 전체 비교: 위치·스태미나·경계·광분·공격·가려움·자국), `Retry_SameCommands_ReplaysIdentically`. 흡혈·중독 게이지는 생기는 즉시 스냅샷에 넣어 같은 테스트로 검증 (D-031)

