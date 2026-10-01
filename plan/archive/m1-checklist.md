# M1 Core 충돌 월드 · 비행 · 카메라 — 체크리스트 (보관)

태그 `m1-done` (2026-10-01). 미체크 1개(부착 시선)는 D-028에 따라 M4 체크리스트로 이월.

### 충돌 월드 (tech/architecture.md §4.5)
- [x] Raycast / SphereSweep / Overlap / ClosestSurface (Box OBB·Sphere·Capsule, 플래그 마스크) — 증거: `CollisionWorldTests` 16개, `ShapeCastCrossCheckTests.Cast_RandomRays_AgreesWithMarching` (형상별 무작위 3000건을 무차별 전진 계산과 비교)

### spec/00 월드 스케일 & 카메라
- [x] 빈 테스트 씬에서 플레이어를 원점 기준 ±500u 범위 어디에 두어도 이동과 충돌이 정상 동작한다 (Core). — 증거: `CollisionMovementTests.WorldRange_AnyPositionWithin500u_MovesAndCollides` (27개 위치), `FlyIntoWall_StopsBeforeSurfaceWithZeroNormalVelocity`, `DiagonalIntoWall_SlidesAlongSurface`, `VariedInputsInBox_NeverPenetrate`
- [x] 3인칭 카메라 near clip이 `camera.nearClip`이고, 벽에 붙어도 카메라가 벽을 관통하지 않는다 (Unity). — 증거: `CameraTests.ThirdPerson_NearClipAndFov_MatchTuning`, `ThirdPerson_WallBehindPlayer_CameraDoesNotPassThroughWall` (D-027)
- [x] pitch가 `camera.pitchLimit`를 넘지 않는다 (Unity: 입력 누적 테스트). — 증거: `CommandCollectorTests.MouseDelta_AccumulatedUpAndDown_PitchStaysWithinLimit`
- [x] 낙하체가 1초 동안 `world.gravity`로 가속되어 490.5u(±1%) 떨어진다 (Core). — 증거: `ExternalForceTests.FallingBody_OneSecond_Falls4905u`
- [x] 게임 시작 시 기본 시점은 3인칭이다 (Unity, 저장 데이터 없음). — 증거: `CameraTests.DefaultView_NoSavedData_IsThirdPerson`
- [x] ToggleView 입력으로 3인칭 ↔ 1인칭이 전환되고, `camera.switchTime` 후 위치와 FOV가 목표값에 도달한다 (Unity). — 증거: `CameraTests.Toggle_AfterSwitchTime_ReachesFirstPersonTargetThenBack`, `CommandCollectorTests.ToggleViewAndPause_Gamepad_AreConsumedSeparately`
- [x] 전환 전후의 yaw/pitch가 같다 (Unity). — 증거: `CameraTests.Toggle_BeforeAndAfter_YawPitchUnchanged`
- [x] 같은 입력 시퀀스를 두 시점에서 재생하면 플레이어 최종 위치가 같다 (Unity: 시점 독립성). — 증거: `ViewIndependenceTests.SameInputSequence_ThirdVsFirstPerson_SameFinalPosition`
- [x] 1인칭에서 플레이어 메시의 렌더러가 Shadows Only이고, 3인칭으로 돌아오면 원래대로 복구된다 (Unity). — 증거: `CameraTests.FirstPerson_PlayerRenderers_ShadowsOnlyThenRestored`
- [ ] 1인칭 부착 상태에서 시선이 법선 기준 `camera.fp.attachedLookLimit`를 벗어나지 않는다 (Unity). — 진행: `LookConstraint` + `CameraTests.AttachedLook_AnyInput_StaysWithinLimitOfNormal`. 부착 상태 연결 검증은 M4 (D-028)
- [x] 1인칭에서 벽에 최대한 붙어도 화면에 벽 뒤가 보이지 않는다 (캡처 검토: 벽 접촉 포즈 1장). — 증거: `Captures/2026-10-01_131326/Sandbox_Flight_fp_wall_contact.png`, `..._fp_wall_contact_angled.png` (시뮬레이션으로 앞 벽까지 비행 후 캡처, 캡처 검토 기록 참조)
- [x] 선택한 시점이 재시작 후에도 유지된다 (Unity). — 증거: `CameraTests.SelectedView_PlayerPrefsStore_SurvivesRestart`, `SelectedView_NewControllerWithSameStore_IsRestored`

### spec/01 비행 · 대시 · 스태미나 · 입력
- [x] 최고 속도에서 입력을 놓으면 0.15초에 정지하고, 그동안 4.5u(±2%) 미끄러진다 (Core). — 증거: `FlightTests.Release_AtTopSpeed_StopsIn015SecondsAfterSliding45u`
- [x] 정지 상태에서 1초 동안 W 입력 시 이동 거리가 56.4u(±1%)이다 (Core). — 증거: `FlightTests.Forward_OneSecondFromRest_Travels564u`
- [x] 반대 방향 입력 시 속도가 가속 규칙에 따라 부드럽게 반전된다 (Core). — 증거: `FlightTests.ReverseInput_AtTopSpeed_ReversesSmoothly`
- [x] 대시 종료 직후 속도가 대시 방향 60u/s이고 이후 감속한다 (Core). — 증거: `DashTests.Dash_JustFinished_HasFlightSpeedAlongDashThenDecelerates`
- [x] 바람 외력은 입력과 무관하게 즉시 더해진다 (Core). — 증거: `ExternalForceTests.Wind_NoInput_AddedImmediatelyWithoutInertia`, `Wind_WithInput_AddsToInputMovement`
- [x] 대각선 입력 속도가 단일 방향 속도와 같다 (Core). — 증거: `FlightTests.DiagonalInput_TopSpeed_EqualsSingleDirectionSpeed`
- [x] 대시가 0.12초 동안 60u를 이동한다 (Core, ±1u). — 증거: `DashTests.Dash_FromRest_Moves60uIn012Seconds` (7틱, D-026)
- [x] 좌우·상하 입력이 없을 때 대시는 위쪽이다 (Core: DashDirectionResolver). — 증거: `DashTests.DashDirection_NoLateralOrVerticalInput_IsUp`, `DashDirection_LargestAxisWins`, `DashDirection_TieBetweenLateralAndVertical_PrefersLateral`
- [x] 스태미나 < 25이면 대시가 실행되지 않는다 (Core). — 증거: `DashTests.Dash_StaminaBelowCost_DoesNotExecute`
- [x] 쿨타임 안의 재입력은 무시된다 (Core). — 증거: `DashTests.Dash_PressedDuringCooldown_IsIgnored`
- [x] 대시 시 NoiseEvent가 정확히 1회, 반경 150u로 발생한다 (Core). — 증거: `DashTests.Dash_Executed_EmitsSingleNoiseEventWith150uRadius`
- [x] 스태미나가 마지막 소모 1초 후부터 20/s로 회복한다 (Core). — 증거: `StaminaTests.Stamina_AfterDash_RegeneratesFrom1SecondAt20PerSecond`
- [x] 스태미나 0이면 2초간 속도 50%, 대시 불가 (Core). — 증거: `StaminaTests.Stamina_ReachesZero_Exhausted2SecondsWithHalfSpeedAndNoDash`, `Exhaustion_WhileHidden_RecoversTwiceAsFast`
- [x] 대시가 벽을 관통하지 않는다 (Core). — 증거: `DashTests.Dash_IntoWall_StopsWithoutPenetrating`
- [x] 위 입력 매핑이 Input Actions 에셋에 존재하고, 게임패드로도 동일하게 동작한다 (Unity: 가상 Gamepad). — 증거: `Assets/_Project/Input/MoquiControls.inputactions`(Gameplay 맵), `CommandCollectorTests.Asset_GameplayAction_HasKeyboardAndGamepadBindings`(11개 액션), `Gamepad_AllGameplayInputs_ProduceCommand`, `KeyboardMouse_AllGameplayInputs_ProduceCommand`, `ToggleViewAndPause_Gamepad_AreConsumedSeparately`, `GamepadStick_FullRightOneSecond_RotatesYawByLookSpeed`

### 마일스톤 산출물
- [x] `Sandbox_Flight` 씬 (빌드 제외) — 증거: `Assets/_Project/Scenes/Sandbox_Flight.unity` (`Tools/build-sandboxes.ps1`로 생성), `SandboxFlightSceneTests.SandboxFlight_Play_RunsSimulationAndPlacesCameraNearPlayer`, `WorldViewTests`
