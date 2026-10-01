# M4 스텔스 — 체크리스트 (보관)

태그 `m4-done` (2026-10-01). 이월: 젖은 날개·습기 2배 회복 → M6, 중독 → M9, 레벨 데이터 obstacle 검사 → M7 (D-034).

- [x] Shadow Zone 안에서는 Yellow Zone이어도 시각 증가가 0이다 (Core). — 증거: `StealthTests.ShadowZone_InsideYellowZone_NoVisionGainAndHidden`, `ShadowZone_LeaveZone_NoLongerHidden`, `HumanVisionTests.Hidden_InYellowZone_NotVisibleAndNoGain`
- [x] Shadow Zone 안에서 경계가 25/s로 감소한다 (Core). — 증거: `StealthTests.ShadowZone_NoStimulus_AwarenessDecays25PerSecond`
- [x] Shadow Zone 안에서 대시하면 경계가 +40 오른다 (Core). — 증거: `StealthTests.ShadowZone_DashInside_AwarenessPlus40`
- [x] 진입 시 비네트가 0.3초에 걸쳐 0.4가 된다 (Unity). — 증거: `StealthPresentationTests.Vignette_EnterShadow_Reaches04In03Seconds`, `Vignette_Component_DrivesVolumeVignetteIntensity` (ShadowVignette, 샌드박스 씬에 전역 Volume)
- [ ] Shadow Zone 안에서 중독·젖은 날개·습기·탈진이 2배 빠르게 회복된다 (Core). (M4: 탈진. 젖은 날개·습기는 M6, 중독은 M9 — D-034) — 진행: 탈진 `StaminaTests.Exhaustion_WhileHidden_RecoversTwiceAsFast`(실제 Shadow Zone 볼륨)
- [x] 부착 상태에서는 비행 소음이 발생하지 않는다 (Core: 반경 안 인간 경계 증가 0). — 증거: `StealthTests.Attached_NearEars_NoFlightNoise`
- [x] 부착 상태의 시각 증가율이 비부착 대비 0.3배이다 (Core). — 증거: `StealthTests.Attached_InYellowZone_VisionRateIs03Times`
- [x] 2u보다 먼 표면에서는 부착되지 않는다 (Core). — 증거: `AttachTests.Attach_SurfaceFartherThan2u_DoesNotAttach`, `Attach_NonAttachableSurface_DoesNotAttach`
- [x] 부착 시 캐릭터 up 벡터가 표면 법선과 5° 이내로 정렬된다 (Core). — 증거: `AttachTests.Attach_WithinRange_AttachesAlignedToNormal`
- [ ] 레벨 데이터의 모든 가구 형상에 obstacle 플래그가 있다 (Core: 레벨 데이터 검사). (레벨 포맷이 생기는 M7에서 검증 — D-034)
- [x] (M1 이월, D-028) 1인칭 부착 상태에서 시선이 법선 기준 `camera.fp.attachedLookLimit`를 벗어나지 않는다 (Unity). — 증거: `StealthPresentationTests.FirstPersonAttached_LookIntoWall_StaysWithinLimitOfNormal`(실제 부착 시뮬레이션, 240프레임 입력 누적), `ThirdPersonAttached_LookNotConstrained`

