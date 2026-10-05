# 01. 비행 · 대시 · 스태미나 · 입력

## 목적
모기다운 정밀한 호버링(짧은 가감속의 약한 관성)과, 고위험 회피기인 볼텍스 대시를 구현한다. 관성이 전혀 없으면 비행의 손맛이 없으므로 살짝 미끄러지는 느낌을 준다 (D-016).

## 입력 매핑 (Unity Input System, Action Map `Gameplay`)
| 액션 | 키보드/마우스 | 게임패드 |
|---|---|---|
| Move (전후좌우) | WASD | 왼쪽 스틱 |
| Ascend / Descend | Space / Left Alt | RT / LT |
| Look | 마우스 | 오른쪽 스틱 |
| Dash | 마우스 오른쪽 | A (South) |
| Precision (홀드) | Left Ctrl | LB |
| Attach / Detach | F | B (East) |
| Suck (홀드) | 마우스 왼쪽 | X (West) |
| ToggleView (시점 전환) | V | 오른쪽 스틱 누르기 (R3) |
| Skill (액티브 스킬) | Q | Y (North) |
| Pause | Esc | Start |

## 규칙
### 이동
- 이동 방향은 카메라 yaw 기준 전후좌우 + 월드 상하이다. pitch는 이동 방향에 반영하지 않는다 (상하는 Ascend/Descend로만).
- 속도: 수평 `flight.speed`, 수직 `flight.verticalSpeed`. 대각선 입력은 정규화한다.
- 속도 배율은 모두 곱한다: 포만(spec/04 §5), 탈진, 젖은 날개(spec/05), 스킬 순풍(spec/09).
- **약한 관성:** 현재 속도는 목표 속도(입력 × 속도 × 배율)를 향해 가속한다.
  - 입력이 있으면 최대 가속도 `flight.speed / flight.accelTime`, 입력이 없으면 최대 감속도 `flight.speed / flight.decelTime`로 접근한다. 수직축도 같은 시간을 쓴다.
  - 스킬 와류 제어가 두 시간을 줄인다 (spec/09).
  - 바람(spec/06)은 관성과 별개의 외력으로 더한다.
  - 부착하면 속도가 0이 된다. 튕겨남(spec/02 §6)은 밀림 방향으로 순간 속도를 주고 일반 감속을 따른다.
- Precision을 누르고 있으면 속도에 `flight.precisionSpeedMul`, 소음 반경에 `noise.precisionRadiusMul`을 적용한다.
- 장애물과는 sweep으로 충돌 처리하고 표면을 따라 미끄러진다.

### 볼텍스 대시
- 방향 (M12): **진행 방향**(이동 입력 = 카메라 yaw 기준 전후좌우 + 월드 상하를 합친 벡터의 방향)으로 대시한다. 이동 입력이 없으면 Core 난수(시드 고정)로 고른 **무작위 방향**으로 대시한다 (D-051). 부착 중 대시는 붙은 표면의 **법선 방향**으로 떨어져 나가며 대시한다.
- (이전 규칙: 좌/우/상/하 중 가장 큰 축, 입력 없으면 위쪽 — 2026-10-01 플레이테스트에서 "무조건 위로만 간다"는 피드백으로 폐기)
- `dash.duration` 동안 `dash.distance × 포만 대시 배율`(spec/04 §5)을 등속으로 이동한다. 장애물에 닿으면 그 지점에서 멈춘다.
- 대시가 끝나면 대시 방향으로 `flight.speed`의 속도를 남기고, 이후 일반 가감속 규칙을 따른다 (살짝 미끄러지며 빠져나오는 느낌).
- 실행 조건: 쿨타임(`dash.cooldown`)이 끝났고, 스태미나가 `dash.staminaCost` 이상이며, 탈진 상태가 아니어야 한다.
- 실행 시 `NoiseEvent(position, dash.noiseRadius, dash.noiseAwareness)`를 1회 발생시킨다.
- 무적 시간은 없다. 공격 판정은 대시 중에도 적용된다.
- 스킬 연속 와류가 있으면 대시 후 `dash.chainWindow` 안에 쿨타임을 무시하고 1회 더 대시할 수 있다 (spec/09).

### 스태미나
- 대시할 때만 소모한다. 마지막 소모 후 `stamina.regenDelay`가 지나면 `stamina.regenRate`로 회복한다.
- 0이 되면 `stamina.exhaustedDuration` 동안 탈진: 이동 속도에 `stamina.exhaustedSpeedMul`을 적용하고 대시할 수 없다. 숨은 상태에서는 탈진 시간이 `hiding.debuffRecoveryMul`배 빠르게 줄어든다.

### 입력 디버프
- Core는 PlayerCommand를 시뮬레이션에 넣기 전에 **디버프 필터**를 적용한다: 스프레이 중독의 끊김/반전/랜덤(spec/06), 튕겨남 경직(spec/02 §6), Trapped 중 이동 무시(spec/05).
- 필터는 Core 난수를 써서 같은 시드에서 같은 결과를 낸다.

### 액티브 스킬
- Skill 입력으로 장착한 액티브 스킬(spec/09)을 쓴다. 장착하지 않았으면 아무 일도 없다.

## 상태
```
Flying ──(F, 부착 가능 표면 근처)──▶ Attached ──(F 또는 이동 입력)──▶ Flying
Attached ──(Dash)──▶ Dashing (표면 법선 방향, M12)
Flying ──(Dash)──▶ Dashing ──(duration 종료)──▶ Flying
Flying ──(물방울 피격)──▶ Trapped (spec/05)
Attached ──(부위 급격한 움직임)──▶ Dislodged ──(경직 종료)──▶ Flying (spec/02 §6)
* ──(공격 피격/거미줄/물방울 낙하/스프레이 중독)──▶ Dead
```

## 수용 기준
- [x] 최고 속도에서 입력을 놓으면 0.15초에 정지하고, 그동안 4.5u(±2%) 미끄러진다 (Core). — 증거: `FlightTests.Release_AtTopSpeed_StopsIn015SecondsAfterSliding45u`
- [x] 정지 상태에서 1초 동안 W 입력 시 이동 거리가 56.4u(±1%)이다 (가속 0.12초 반영, Core, 장애물 없음). — 증거: `FlightTests.Forward_OneSecondFromRest_Travels564u`
- [x] 반대 방향 입력 시 속도가 가속 규칙에 따라 부드럽게 반전된다 (순간 반전 없음) (Core). — 증거: `FlightTests.ReverseInput_AtTopSpeed_ReversesSmoothly`
- [x] 대시 종료 직후 속도가 대시 방향 60u/s이고 이후 감속한다 (Core). — 증거: `DashTests.Dash_JustFinished_HasFlightSpeedAlongDashThenDecelerates`
- [x] 바람 외력은 입력과 무관하게 즉시 더해진다 (Core). — 증거: `ExternalForceTests.Wind_NoInput_AddedImmediatelyWithoutInertia`, `Wind_WithInput_AddsToInputMovement`
- [x] 대각선 입력 속도가 단일 방향 속도와 같다 (Core). — 증거: `FlightTests.DiagonalInput_TopSpeed_EqualsSingleDirectionSpeed`
- [x] 대시가 0.12초 동안 60u를 이동한다 (Core, ±1u). — 증거: `DashTests.Dash_FromRest_Moves60uIn012Seconds` (7틱, D-026)
- [x] 대시가 진행 방향(전후좌우+상하 합성)으로 나가고, 이동 입력이 없으면 같은 시드에서 같은 무작위 방향으로 나간다 (Core). (M12) — 증거: `DashTests.DashDirection_FollowsMovementInput_IncludingForwardAndVertical`, `DashDirection_NoInput_RandomUnitVector_ReproducibleWithSeed_NotAlwaysUp`, `Dash_ForwardInput_MovesForward` (D-051)
- [x] 부착 중 대시 입력이면 표면 법선 방향으로 대시하며 부착이 풀린다 (Core). (M12) — 증거: `DashTests.Dash_WhileAttached_LeavesAlongSurfaceNormal`, `Dash_WhileAttached_WithoutStamina_StaysAttached`
- [x] 스태미나 < 25이면 대시가 실행되지 않는다 (Core). — 증거: `DashTests.Dash_StaminaBelowCost_DoesNotExecute`
- [x] 쿨타임 안의 재입력은 무시된다 (Core). — 증거: `DashTests.Dash_PressedDuringCooldown_IsIgnored`
- [x] 대시 시 NoiseEvent가 정확히 1회, 반경 150u로 발생한다 (Core). — 증거: `DashTests.Dash_Executed_EmitsSingleNoiseEventWith150uRadius`
- [x] 스태미나가 마지막 소모 1초 후부터 20/s로 회복한다 (Core). — 증거: `StaminaTests.Stamina_AfterDash_RegeneratesFrom1SecondAt20PerSecond`
- [x] 스태미나 0이면 2초간 속도 50%, 대시 불가 (Core). — 증거: `StaminaTests.Stamina_ReachesZero_Exhausted2SecondsWithHalfSpeedAndNoDash`, `Exhaustion_WhileHidden_RecoversTwiceAsFast`
- [x] 대시가 벽을 관통하지 않는다 (Core). — 증거: `DashTests.Dash_IntoWall_StopsWithoutPenetrating`
- [x] 위 입력 매핑이 Input Actions 에셋에 존재하고, 게임패드로도 동일하게 동작한다 (Unity: 가상 Gamepad 디바이스로 입력 주입). (M12) — 증거: EditMode `CommandCollectorTests.Asset_GameplayAction_HasKeyboardAndGamepadBindings`(LAlt 하강·LCtrl 정밀·우클릭 대시), `KeyboardMouse_AllGameplayInputs_ProduceCommand`, `Gamepad_*`

## 범위 외
- 롤, 대시 방향 8방향화, 공중 관성 옵션
