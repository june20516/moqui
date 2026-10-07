# 04. 흡혈 · 가려움 · 물린 자국 · 포만 · 승패

## 목적
하이 리스크 하이 리턴의 핵심 행동. **적게 물고 길게 빠는 신중한 플레이가 보상받도록** 설계한다. (D-012)
- 한 번 오래 빨수록 흡혈 속도가 올라간다 (세션 가속).
- 하지만 오래 빨수록 가려움이 차서 반응 위험이 커진다.
- 떠날 때마다 물린 자국이 남아 인간이 점점 예민해진다.
- 피를 많이 빨수록 몸이 무거워진다.

## 1. SkinSite
- 인간 몸 캡슐 표면의 구간으로 정의한다 (`attachable`, `skinSite` 플래그). 스테이지별 목록은 spec/07에 있다.
- 각 부위는 **유형**을 가지며, 유형별로 민감도(`site.<유형>.sensitivity`)와 혈액량 배율(`site.<유형>.yield`)이 다르다.

  | 유형 | 민감도 | 혈액량 | 성격 |
  |---|---|---|---|
  | forearm (팔뚝) | 1.0 | 1.0 | 기준 |
  | calf (종아리/정강이) | 0.6 | 0.8 | 안전, 적음 |
  | footTop (발등) | 0.5 | 0.7 | 가장 안전, 가장 적음 |
  | neck (목) | 1.6 | 1.4 | 위험, 많음 |
  | cheek (볼) | 2.2 | 1.6 | Red Zone 인접, 최고 위험·최고 보상 |
- 각 부위는 독립적인 **가려움 게이지**(0~100)와 **물린 자국 여부**를 가진다.

## 2. 흡혈 세션
- SkinSite에 부착한 상태에서 Suck 입력을 누르고 있는 동안이 흡혈이다. 부착 후 처음 Suck을 누르면 **세션이 시작**되고, 부착이 풀리면(이탈, 튕겨남, 사망) **세션이 끝난다**. 부착 중 Suck을 잠시 놓아도 세션은 유지된다.
- 흡혈 속도(%/s) = `lerp(suck.rateStart, suck.rateMax, min(1, 세션 흡혈 시간 / suck.rampTime))` × `site.yield` × 스킬·취함 배율.
- 흡혈 중 해당 부위 가려움이 `suck.itchRate × site.sensitivity × 스킬 배율`/s로 오른다. 흡혈하지 않는 동안 모든 부위는 `suck.itchDecay`/s로 감소한다.
- 가려움은 반응 확률을 올린다 (spec/02 §5). 가려움 100이면 즉시 반응한다.
- 흡혈 자체는 소음이 없다. 부착 상태 규칙(spec/03)이 그대로 적용된다.
- **흡혈 중 몸 고정 (M13):** Suck을 누른 채 세션이 진행 중이면 이동·상승·하강 입력으로 떨어지지 않는다. 시점(마우스)은 자유롭다. 빠져나오는 방법은 F(떼기), 대시(긴급 탈출, 표면 법선 방향), Suck을 놓은 뒤 이동 입력이다 (D-055). 개념 모델은 "지팡이를 꽂으면 붙들린다"(spec/12, gulf §1).
- **억지로 뽑기 (M14, D-066):** 흡혈 중(Suck을 누른 채 세션 진행) 대시로 빠져나오면 그 부위 가려움이 `suck.yankItch`만큼 오른다(긴급 탈출의 대가). F 떼기나 Suck을 놓은 뒤 떨어지기는 대가가 없다. 모키 큐 `wand.yank`.

## 3. 착지
- SkinSite에 부착하는 순간 착지 반응을 판정한다 (spec/02 §5). 스킬 "깃털 착지"가 확률을 낮춘다.

## 4. 물린 자국
- 자국은 그 세션에서 주둥이를 꽂은 **부착 지점**(부위 로컬 좌표)에 생기고, 부위가 움직이면 함께 움직인다. 같은 부위를 여러 번 물면 자국도 여러 개다 (M12).
- 세션이 끝날 때 그 세션에서 빤 양이 `biteMark.minAmount` 이상이면 그 부위에 자국이 생기고 인간의 **자국 수 n**이 1 늘어난다. 같은 부위를 다시 물어도 새 세션이면 n이 늘어난다.
- 세션이 끝나는 순간 경계가 `biteMark.awarenessBump`만큼 오른다 (가려움을 알아챔).
- 자국 수에 따른 보정 (spec/02):
  - 경계 증가 배율 `1 + biteMark.gainMulPerBite × n`
  - 경계 감소 배율 `1 / (1 + biteMark.decayDivPerBite × n)`
  - 경계 하한 `min(biteMark.floorPerBite × n, biteMark.floorMax)`. 감소해도 이 값 아래로 내려가지 않는다.
  - 반응 확률 배율 `1 + biteMark.reactionMulPerBite × n`
- 결과적으로 짧게 여러 번 무는 플레이는 빠르게 불리해진다. 자국 수가 적으면 보너스가 있다 (spec/09).

## 5. 포만
- 흡혈 게이지 `b`(0~100%)에 따라 **대시 발동이 늦어진다**: 대시를 누르면 `satiety.dashDelayMax × b/100`초 동안 숨을 고른 뒤 누른 순간의 방향으로 튀어 나간다(모키 큐 `dash.charge`). 이동 속도와 대시 거리는 그대로다 — 느려짐은 젖은 날개·탈진의 몫이고 포만은 "순발력"을 깎는다 (M14 플레이 피드백, D-071). 붙은 채 하는 대시(긴급 탈출)는 지연이 없다.
- 스킬 "소화 촉진"이 감속 폭을 줄인다.

## 6. 승리
- 흡혈 게이지 100% 도달 즉시 Stage Clear. 입력을 막고 결과 화면으로 간다 (spec/08).

## 7. 패배 (즉사)
| 원인 | DeathCause | 출처 |
|---|---|---|
| 공격·반응 판정 피격 | Attack | spec/02, 06 |
| 물방울에 갇힌 채 바닥 충돌 | WaterImpact | spec/05 |
| 거미줄 접촉 | Web | spec/06 |
| 스프레이 중독 100 | Spray | spec/06 |

- 사망 시 원인을 기록하고 0.8초 사망 연출 후 결과 화면(실패)으로 간다. 재시도하면 스테이지를 처음부터 시작한다.

## 8. 흡혈 중 이벤트 (M13, D-056)
"흡혈 중" = 인간의 피부에 붙어 Suck을 누른 채 세션이 진행 중. 이벤트는 한 번에 하나이고, 인간이 공격 중이거나 졸고 있으면 시작하지 않는다(광분 중에는 긁으러 오는 손만). 난수는 레벨 시드의 suckEvents 스트림을 쓴다. 각 이벤트는 "버틸지 / 멈출지 / 떠날지"의 선택을 만든다. HUD는 조준점 위에 경고 문구를 띄운다 (spec/08).
- **긁으러 오는 손 (Twitch):** 그 부위 가려움이 `suckEvent.twitchItchStart`부터 `suckEvent.twitchItchStep`마다(40·60·80) 반대쪽 손이 문 자리 쪽으로 움찔한다. 손바닥이 휴식 위치 → 문 자리 거리의 `twitchReachStart` + (단계 − 1) × `twitchReachStep`만큼 sin 곡선으로 갔다가 `twitchDuration`에 돌아온다. 판정은 없다(경고). 같은 세션에서 단계마다 한 번. 실제 때리기는 기존 반응 규칙(§2, spec/02 §5)이다.
- **부위가 움직임 (Shift):** 위험률 `suckEvent.shiftRate`. 예고 `shiftTelegraph` 뒤, 그 부위가 부위 축에 수직인 수평 방향(좌우 무작위)으로 `shiftDistance`만큼 `shiftDuration` 동안 갔다 돌아온다(최고 속도가 `human.dislodgeSpeed`를 넘는다). Suck을 누르고 있으면 주둥이로 버텨 튕김 기준 속도가 `gripMul`배가 되어 붙어 있고, 그동안 흡혈 속도 × `shiftRateMul`, 가려움은 오르지 않는다. 놓았으면 튕겨 난다(spec/02 §6).
- **시선 (Glance):** 위험률 `suckEvent.glanceRate` × (0.5 + 가려움/100). 머리가 문 자리로 `glanceTurnSpeed`로 `glanceTurnTime` 동안 돌고(예고), `glanceHold` 동안 보고, 다시 `glanceTurnTime` 동안 원래 보던 쪽으로 되돌린다. 보는 동안 모기가 Suck을 누르고 있고 머리 정면 Yellow 원뿔 안에서 가림 없이 보이면 **들킨다**: 경계 + `glanceNoticeAwareness`(평소 경계 30 이상이면 광분), 자극 위치 = 문 자리. 시선이 진행되는 동안 흡혈을 멈추고 붙어 있으면 시야로 경계가 오르지 않는다(가려운 자리만 살핀다).

## 수용 기준
- [x] 세션 흡혈 속도가 2%/s에서 시작해 6초에 걸쳐 6%/s까지 선형으로 오른다 (forearm 기준) (Core). — 증거: `SuckTests.SessionRate_Forearm_Linear2To6Over6Seconds`(0/1.5/3/4.5/6/8초 측정)
- [x] 부위 유형별 혈액량과 가려움 증가가 표의 배율을 따른다 (Core). — 증거: `SuckTests.SiteType_BloodAmountAndItch_FollowTable`(팔뚝·종아리·볼)
- [x] 흡혈 중(Suck 누름 + 세션 진행) 이동 입력으로는 떨어지지 않고 몸이 고정되며, F·대시·Suck을 놓은 뒤 이동으로는 빠져나온다 (Core). (M13) — 증거: `SuckTests.WhileSucking_MoveInput_BodyStaysFixed`, `WhileSucking_EscapeInputs_Detach`
- [x] 부착 중 Suck을 놓았다 다시 눌러도 세션 가속이 유지되고, 이탈하면 초기화된다 (Core). — 증거: `SuckTests.SuckReleasedAndPressedAgain_KeepsAcceleration_DetachResets`
- [x] 부착하지 않았거나 피부가 아닌 표면에서는 흡혈이 되지 않는다 (Core). — 증거: `SuckTests.Suck_NotAttached_NoGain`, `Suck_AttachedToWall_NoGain`, `Suck_AttachedToClothedBodyPart_NoGain`
- [x] 세션 종료 시 빤 양이 5% 이상이면 자국이 생기고 경계가 +15 오른다. 5% 미만이면 생기지 않는다 (Core). — 증거: `SuckTests.SessionEnd_AmountAtLeast5_BiteMarkAndAwarenessPlus15`, `SessionEnd_AmountBelow5_NoBiteMark`
- [x] 자국 수에 따른 증가·감소 배율, 하한, 반응 배율이 정확히 적용된다 (n = 0, 1, 3, 6) (Core). — 증거: `SuckTests.BiteMarks_ModifiersMatchFormula`(n=0,1,3,6), `BiteMarks_AppliedToAwarenessAndReactions`(n=0,1,3,6: 시각 증가·감소·하한·반응 배율을 시뮬레이션에서 측정)
- [x] 포만 0%/50%/100%에서 속도 배율이 1.0/0.8/0.6, 대시 거리 배율이 1.0/0.875/0.75이다 (Core). — 증거: `SuckTests.Satiety_SpeedAndDashMultipliers`(0/50/100%)
- [x] 게이지 100% 도달 시 Stage Clear 이벤트가 정확히 1회 발생한다 (Core). — 증거: `SuckTests.Gauge100_StageClearedExactlyOnce`, `Cleared_InputIgnoredWorldFrozen`
- [x] 각 사망 원인이 올바른 DeathCause로 기록된다 (Core, 원인별 1개 테스트). — 증거: Attack `ReactionTests.ReactSlap_PlayerStays_DiesWithAttackCause`, WaterImpact `WaterTests.TrappedUntilFloor_DiesWithWaterImpact`, Web `GimmickTests.Web_Contact_DiesWithWebCauseAfter15Seconds_CannotMove`, Spray `GimmickTests.Toxin_100_DiesWithSprayCause` (DeathCause 4종 전부)
- [x] 재시도 시 흡혈 게이지, 가려움, 자국, 인간 경계, 플레이어 위치, 중독 게이지가 모두 초기화된다 (Core). — 증거: `RetryTests.Retry_AfterPlaying_EveryStateMatchesFreshStart`(스냅샷 전체 비교: 위치·스태미나·경계·광분·공격·가려움·자국), `Retry_SameCommands_ReplaysIdentically`. 흡혈·중독 게이지는 생기는 즉시 스냅샷에 넣어 같은 테스트로 검증 (D-031)
- [x] 설계 검증: 같은 스테이지에서 "긴 세션 2회" 시나리오가 "짧은 세션 6회" 시나리오보다 평균 경계가 낮고 클리어 시간이 짧다 (Core 시나리오 비교, 시드 20개 평균). — 증거: `DesignValidationTests.LongSessions_VersusShortSessions_LowerAwarenessAndFasterClear` — 시드 20: 긴 세션 12/20 클리어·평균 경계 28.2·평균 280.6초, 짧은 세션 0/20·38.8·600초 (D-035)
- [x] 자국이 세션의 부착 지점(부위 로컬 좌표)에 기록되고 스냅샷에 위치로 나온다 (Core). (M12) — 증거: `SuckTests.BiteMark_AtTheBiteSpot_FollowsThePart_OnePerSession`(문 자리·스냅샷 위치·부위를 따라감·같은 부위 두 번이면 2개)
- [x] 가려움 40·60·80에서 반대쪽 손이 문 자리 쪽으로 움찔했다 돌아오고(판정 없음), 단계마다 한 번이다 (Core). (M13) — 증거: `SuckEventTests.TwitchLevels_Are40_60_80_WithGrowingReach`, `Twitch_ItchCrossesThreshold_OppositeHandTwitchesTowardBiteAndBack`
- [x] 부위가 움직일 때 Suck을 누르고 있으면 버티며 흡혈 1.5배·가려움 정지, 놓았으면 튕겨 난다 (Core). (M13) — 증거: `SuckEventTests.Shift_HoldingSuck_RidesItWithFasterBloodAndNoItch`, `Shift_NotHoldingSuck_IsDislodged`
- [x] 시선 중 계속 빨면 들켜 경계가 +70 오르고, 멈추고 얼어 있으면 들키지 않으며 머리가 되돌아간다. 흡혈 중이 아니면 이벤트가 없다 (Core). (M13) — 증거: `SuckEventTests.Glance_KeepSucking_IsNoticed`, `Glance_Frozen_PassesUnnoticed_HeadReturns`(얼기 규칙을 끄면 실패함을 확인), `NotSucking_NoEventStarts`
- [x] 흡혈 중 이벤트마다 HUD 경고 문구가 뜬다 (Unity). (M13) — 증거: EditMode `HudTests.SuckEventWarnings_ShowDuringSessionOnly`
- [x] 이벤트가 있어도 "적게 물고 길게 빠는" 설계가 유지되고 봇이 각 스테이지를 4/5 이상 클리어한다 (Core). (M13) — 증거: `DesignValidationTests.LongSessions_VersusShortSessions_LowerAwarenessAndFasterClear`(긴 세션 12/20 vs 짧은 0/20), `ScenarioTests`(봇은 시선이 오면 흡혈을 멈추고 얼어 있는다)
- [x] 흡혈 중 대시로 빠져나오면 그 부위 가려움이 `suck.yankItch`만큼 오르고, F 떼기는 대가가 없다 (Core). (M14) — 증거: `SuckTests.WhileSucking_DashYank_RaisesItch_AttachReleaseDoesNot`

## 범위 외
- 흡혈 후 탈출(퇴장) 단계
