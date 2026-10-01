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

## 3. 착지
- SkinSite에 부착하는 순간 착지 반응을 판정한다 (spec/02 §5). 스킬 "깃털 착지"가 확률을 낮춘다.

## 4. 물린 자국
- 자국은 그 세션에서 주둥이를 꽂은 **부착 지점**(부위 로컬 좌표)에 생기고, 부위가 움직이면 함께 움직인다. 같은 부위를 여러 번 물면 자국도 여러 개다 (M12 플레이테스트 반영, 미구현).
- 세션이 끝날 때 그 세션에서 빤 양이 `biteMark.minAmount` 이상이면 그 부위에 자국이 생기고 인간의 **자국 수 n**이 1 늘어난다. 같은 부위를 다시 물어도 새 세션이면 n이 늘어난다.
- 세션이 끝나는 순간 경계가 `biteMark.awarenessBump`만큼 오른다 (가려움을 알아챔).
- 자국 수에 따른 보정 (spec/02):
  - 경계 증가 배율 `1 + biteMark.gainMulPerBite × n`
  - 경계 감소 배율 `1 / (1 + biteMark.decayDivPerBite × n)`
  - 경계 하한 `min(biteMark.floorPerBite × n, biteMark.floorMax)`. 감소해도 이 값 아래로 내려가지 않는다.
  - 반응 확률 배율 `1 + biteMark.reactionMulPerBite × n`
- 결과적으로 짧게 여러 번 무는 플레이는 빠르게 불리해진다. 자국 수가 적으면 보너스가 있다 (spec/09).

## 5. 포만
- 흡혈 게이지 `b`(0~100%)에 따라 이동 속도에 `lerp(1, satiety.minSpeedMul, b/100)`, 대시 거리에 `lerp(1, satiety.minDashMul, b/100)`을 곱한다.
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

## 수용 기준
- [x] 세션 흡혈 속도가 2%/s에서 시작해 6초에 걸쳐 6%/s까지 선형으로 오른다 (forearm 기준) (Core). — 증거: `SuckTests.SessionRate_Forearm_Linear2To6Over6Seconds`(0/1.5/3/4.5/6/8초 측정)
- [x] 부위 유형별 혈액량과 가려움 증가가 표의 배율을 따른다 (Core). — 증거: `SuckTests.SiteType_BloodAmountAndItch_FollowTable`(팔뚝·종아리·볼)
- [x] 부착 중 Suck을 놓았다 다시 눌러도 세션 가속이 유지되고, 이탈하면 초기화된다 (Core). — 증거: `SuckTests.SuckReleasedAndPressedAgain_KeepsAcceleration_DetachResets`
- [x] 부착하지 않았거나 피부가 아닌 표면에서는 흡혈이 되지 않는다 (Core). — 증거: `SuckTests.Suck_NotAttached_NoGain`, `Suck_AttachedToWall_NoGain`, `Suck_AttachedToClothedBodyPart_NoGain`
- [x] 세션 종료 시 빤 양이 5% 이상이면 자국이 생기고 경계가 +15 오른다. 5% 미만이면 생기지 않는다 (Core). — 증거: `SuckTests.SessionEnd_AmountAtLeast5_BiteMarkAndAwarenessPlus15`, `SessionEnd_AmountBelow5_NoBiteMark`
- [x] 자국 수에 따른 증가·감소 배율, 하한, 반응 배율이 정확히 적용된다 (n = 0, 1, 3, 6) (Core). — 증거: `SuckTests.BiteMarks_ModifiersMatchFormula`(n=0,1,3,6), `BiteMarks_AppliedToAwarenessAndReactions`(n=0,1,3,6: 시각 증가·감소·하한·반응 배율을 시뮬레이션에서 측정)
- [x] 포만 0%/50%/100%에서 속도 배율이 1.0/0.8/0.6, 대시 거리 배율이 1.0/0.875/0.75이다 (Core). — 증거: `SuckTests.Satiety_SpeedAndDashMultipliers`(0/50/100%)
- [x] 게이지 100% 도달 시 Stage Clear 이벤트가 정확히 1회 발생한다 (Core). — 증거: `SuckTests.Gauge100_StageClearedExactlyOnce`, `Cleared_InputIgnoredWorldFrozen`
- [x] 각 사망 원인이 올바른 DeathCause로 기록된다 (Core, 원인별 1개 테스트). — 증거: Attack `ReactionTests.ReactSlap_PlayerStays_DiesWithAttackCause`, WaterImpact `WaterTests.TrappedUntilFloor_DiesWithWaterImpact`, Web `GimmickTests.Web_Contact_DiesWithWebCauseAfter15Seconds_CannotMove`, Spray `GimmickTests.Toxin_100_DiesWithSprayCause` (DeathCause 4종 전부)
- [x] 재시도 시 흡혈 게이지, 가려움, 자국, 인간 경계, 플레이어 위치, 중독 게이지가 모두 초기화된다 (Core). — 증거: `RetryTests.Retry_AfterPlaying_EveryStateMatchesFreshStart`(스냅샷 전체 비교: 위치·스태미나·경계·광분·공격·가려움·자국), `Retry_SameCommands_ReplaysIdentically`. 흡혈·중독 게이지는 생기는 즉시 스냅샷에 넣어 같은 테스트로 검증 (D-031)
- [x] 설계 검증: 같은 스테이지에서 "긴 세션 2회" 시나리오가 "짧은 세션 6회" 시나리오보다 평균 경계가 낮고 클리어 시간이 짧다 (Core 시나리오 비교, 시드 20개 평균). — 증거: `DesignValidationTests.LongSessions_VersusShortSessions_LowerAwarenessAndFasterClear` — 시드 20: 긴 세션 12/20 클리어·평균 경계 28.2·평균 280.6초, 짧은 세션 0/20·38.8·600초 (D-035)
- [ ] 자국이 세션의 부착 지점(부위 로컬 좌표)에 기록되고 스냅샷에 위치로 나온다 (Core). (M12 플레이테스트 반영, 미구현)

## 범위 외
- 흡혈 후 탈출(퇴장) 단계
