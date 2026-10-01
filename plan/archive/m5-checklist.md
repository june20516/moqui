# M5 흡혈 세션 · 물린 자국 · 포만 · 승리 — 체크리스트 (보관)

태그 `m5-done` (2026-10-01).

- [x] 세션 흡혈 속도가 2%/s에서 시작해 6초에 걸쳐 6%/s까지 선형으로 오른다 (forearm 기준) (Core). — 증거: `SuckTests.SessionRate_Forearm_Linear2To6Over6Seconds`(0/1.5/3/4.5/6/8초 측정)
- [x] 부위 유형별 혈액량과 가려움 증가가 표의 배율을 따른다 (Core). — 증거: `SuckTests.SiteType_BloodAmountAndItch_FollowTable`(팔뚝·종아리·볼)
- [x] 부착 중 Suck을 놓았다 다시 눌러도 세션 가속이 유지되고, 이탈하면 초기화된다 (Core). — 증거: `SuckTests.SuckReleasedAndPressedAgain_KeepsAcceleration_DetachResets`
- [x] 부착하지 않았거나 피부가 아닌 표면에서는 흡혈이 되지 않는다 (Core). — 증거: `SuckTests.Suck_NotAttached_NoGain`, `Suck_AttachedToWall_NoGain`, `Suck_AttachedToClothedBodyPart_NoGain`
- [x] 세션 종료 시 빤 양이 5% 이상이면 자국이 생기고 경계가 +15 오른다. 5% 미만이면 생기지 않는다 (Core). — 증거: `SuckTests.SessionEnd_AmountAtLeast5_BiteMarkAndAwarenessPlus15`, `SessionEnd_AmountBelow5_NoBiteMark`
- [x] 자국 수에 따른 증가·감소 배율, 하한, 반응 배율이 정확히 적용된다 (n = 0, 1, 3, 6) (Core). — 증거: `SuckTests.BiteMarks_ModifiersMatchFormula`(n=0,1,3,6), `BiteMarks_AppliedToAwarenessAndReactions`(n=0,1,3,6: 시각 증가·감소·하한·반응 배율을 시뮬레이션에서 측정)
- [x] 포만 0%/50%/100%에서 속도 배율이 1.0/0.8/0.6, 대시 거리 배율이 1.0/0.875/0.75이다 (Core). — 증거: `SuckTests.Satiety_SpeedAndDashMultipliers`(0/50/100%)
- [x] 게이지 100% 도달 시 Stage Clear 이벤트가 정확히 1회 발생한다 (Core). — 증거: `SuckTests.Gauge100_StageClearedExactlyOnce`, `Cleared_InputIgnoredWorldFrozen`
- [x] 설계 검증: 같은 스테이지에서 "긴 세션 2회" 시나리오가 "짧은 세션 6회" 시나리오보다 평균 경계가 낮고 클리어 시간이 짧다 (Core 시나리오 비교, 시드 20개 평균). — 증거: `DesignValidationTests.LongSessions_VersusShortSessions_LowerAwarenessAndFasterClear` — 시드 20: 긴 세션 12/20 클리어·평균 경계 28.2·평균 280.6초, 짧은 세션 0/20·38.8·600초 (D-035)
- [x] (M3 이월, D-033) 무작위 움직임이 흡혈 중에도 발생하고, 붙어 있는 모기가 부위를 따라 움직인다 (Core). — 증거: `SuckTests.RandomMotion_WhileSucking_SessionContinuesAndFollowsPart`, `HumanMotionTests.Actions_KeepHappeningWhilePlayerIsAttached`
- [x] (M3 이월, D-033) 부위 속도가 dislodgeSpeed를 넘으면 모기가 튕겨 나가고 흡혈 세션이 끝나며, 사망하지 않는다 (Core). — 증거: `SuckTests.Dislodged_WhileSucking_SessionEndsWithoutDeath`, `HumanMotionTests.FastAction_PartFasterThanDislodgeSpeed_DislodgesWithoutDeath`

