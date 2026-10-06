# 03. 스텔스 — 시야 차단 · Shadow Zone · 벽면 부착

## 목적
플레이어가 경계를 낮추는 세 가지 수단을 제공한다. 숨기, 어둠, 멈추기.

## 규칙
### 시야 차단 (Line of Sight)
- `obstacle` 플래그를 가진 형상이 눈과 플레이어 사이를 막으면 시각 감지가 중단된다 (spec/02).
- 레벨 데이터의 모든 가구 형상은 `obstacle` 플래그를 가져야 한다.

### Shadow Zone
- `shadowZone` 볼륨 (레벨 데이터의 박스 영역) (책상 밑, 커튼 뒤, 침대 밑 등).
- 안에 있는 동안: 시각 증가율에 `vision.shadowMul`(0) 적용, 경계 감소율로 `awareness.shadowDecayRate` 적용.
- 진입/이탈 시 `shadow.transitionTime` 동안 비네트를 `shadow.vignetteIntensity`까지 보간한다. 비네트는 표현 레이어(Unity URP Volume)가 Core의 `InShadow` 상태를 읽어 처리한다.
- 청각 감지는 Shadow Zone 안에서도 유효하다 (대시하면 들킨다).
- 은신처는 멀리서도 보이도록 표시하며, 어그로가 높을수록 진해진다 (spec/11 §4).
- **디버프 회복 가속:** Shadow Zone 안(숨은 상태)에서는 디버프가 `hiding.debuffRecoveryMul`배 빠르게 풀린다. 대상은 중독 감소(spec/06), 젖은 날개 남은 시간과 습기 감소(spec/05), 탈진 남은 시간(spec/01)이다. (D-018)

### 벽면 부착
- 플레이어가 `attachable` 플래그를 가진 표면(가구, 벽, 인간 피부 `SkinSite`)에서 `attach.snapRange`(8u) 이내일 때 Attach 입력으로 가장 가까운 점에 부착한다. 표현은 그 자리까지 0.15초 미끄러져 붙는다(Core는 즉시, gulf §2, D-066). 착지할 수 있으면 붙을 자리에 작은 분홍 원이 보이고 모키 큐 `land.ready`가 뜬다.
- **정밀 비행 자동 착지 (M14, D-066):** 정밀 비행 + 이동 입력으로 표면 쪽(진행 방향과 −법선의 코사인 ≥ `attach.autoLandAlign`)으로 날다 `suck.attachRange` 안에 닿으면 F 없이 내려앉는다. 스치듯 지나가기·일반 비행은 붙지 않는다. 내려앉은 직후에는 그 이동 키를 놓을 때까지 그 입력으로 떨어지지 않는다(표면에서 멀어지는 입력이면 바로 뗀다).
- 벽·천장·가구 옆면처럼 방향과 무관하게 `attachable` 표면이면 부착할 수 있다. 캐릭터 그림도 표면 법선에 맞춰 회전해 벽·천장에 "앉은" 것이 보여야 한다 (M12).
- 부착 시: 표면 법선에 맞춰 정렬, 이동 정지, 소음 0 (`noise.attachedRadius`), 시각 증가율에 `vision.attachedMul` 적용 (스킬 그림자 동화로 감소). 표면이 움직이면 그 로컬 좌표를 따라간다.
- 이동 입력이나 Attach 입력으로 이탈한다. 이탈 시 법선 방향으로 2u 떨어진다.
- 부착 가능할 때 HUD에 프롬프트 "F: 착지"를 표시한다 (spec/08).

## 수용 기준
- [x] Shadow Zone 안에서는 Yellow Zone이어도 시각 증가가 0이다 (Core). — 증거: `StealthTests.ShadowZone_InsideYellowZone_NoVisionGainAndHidden`, `ShadowZone_LeaveZone_NoLongerHidden`, `HumanVisionTests.Hidden_InYellowZone_NotVisibleAndNoGain`
- [x] Shadow Zone 안에서 경계가 25/s로 감소한다 (Core). — 증거: `StealthTests.ShadowZone_NoStimulus_AwarenessDecays25PerSecond`
- [x] Shadow Zone 안에서 대시하면 경계가 +40 오른다 (Core). — 증거: `StealthTests.ShadowZone_DashInside_AwarenessPlus40`
- [x] 진입 시 비네트가 0.3초에 걸쳐 0.4가 된다 (Unity). — 증거: `StealthPresentationTests.Vignette_EnterShadow_Reaches04In03Seconds`, `Vignette_Component_DrivesVolumeVignetteIntensity` (ShadowVignette, 샌드박스 씬에 전역 Volume)
- [x] Shadow Zone 안에서 중독·젖은 날개·습기·탈진이 2배 빠르게 회복된다 (Core). — 증거: 탈진 `StaminaTests.Exhaustion_WhileHidden_RecoversTwiceAsFast`, 젖은 날개·습기 `WaterTests`(spec/05 "숨은 상태에서 젖은 날개 시간과 습기 게이지가 2배" 항목), 중독 `GimmickTests`(spec/06 "숨은 상태에서 중독이 2배" 항목)
- [x] 부착 상태에서는 비행 소음이 발생하지 않는다 (Core: 반경 안 인간 경계 증가 0). — 증거: `StealthTests.Attached_NearEars_NoFlightNoise`
- [x] 부착 상태의 시각 증가율이 비부착 대비 0.3배이다 (Core). — 증거: `StealthTests.Attached_InYellowZone_VisionRateIs03Times`
- [x] 2u보다 먼 표면에서는 부착되지 않는다 (Core). — 증거: `AttachTests.Attach_SurfaceFartherThan2u_DoesNotAttach`, `Attach_NonAttachableSurface_DoesNotAttach`
- [x] 부착 시 캐릭터 up 벡터가 표면 법선과 5° 이내로 정렬된다 (Core). — 증거: `AttachTests.Attach_WithinRange_AttachesAlignedToNormal`
- [x] 레벨 데이터의 모든 가구 형상에 obstacle 플래그가 있다 (Core: 레벨 데이터 검사). — 증거: `LevelDataTests.Level_AllFurnitureHasObstacleFlag`(stage01·02), `Validator_FurnitureWithoutObstacleFlag_Reported`
- [x] 벽 옆면과 천장 아랫면에 부착되고, 부착 중 위치가 표면에서 떨어지지 않는다 (Core). (M12) — 증거: `AttachTests.Attach_CeilingAndWallSide_StaysOnSurface`(천장·벽 옆면, 2초 유지)
- [x] 부착 중 캐릭터 그림의 up이 표면 법선과 5° 이내다(벽·천장 포함) (Unity). (M12) — 증거: EditMode `MokiTests.AttachedRotation_UpMatchesSurfaceNormal`(천장·벽·바닥·비스듬한 면), `PlayerView`가 부착 중 `AttachedRotation` 사용

- [x] F 착지는 snapRange 안의 가장 가까운 표면에 붙고, 정밀 비행으로 표면 쪽으로 닿으면 자동 착지하며 일반 비행·스치기는 붙지 않는다 (Core). (M14) — 증거: `AttachTests.Attach_SnapsWithinSnapRange_NotBeyond`, `PrecisionFlight_IntoSurface_AutoLands`, 봇 시나리오 전체 통과(`ScenarioTests`)
- [x] 착지 미끄러짐은 끝에서 감속해 0.15초에 붙은 자리에 도착한다 (Unity). (M14) — 증거: EditMode `HumanBodyViewTests.SnapGlide_EasesOutAndArrives`
## 범위 외
- 빛의 밝기 기반 동적 은신 계산 (Shadow Zone은 수작업 볼륨으로만 처리)
