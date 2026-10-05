# 11. 모기의 감각 — 흐린 시야 · CO₂ · 체온 · 은신처 표시

## 목적
플레이어는 인간의 눈이 아니라 모기의 감각으로 세상을 본다. 시야는 흐리지만 숨결과 체온이 보이고, 숨을 곳이 친절하게 드러난다. (D-009)

## 레이어 배분
- **Core:** 감각 정보의 **원천 데이터**를 계산해 스냅샷으로 내보낸다. 인간의 호흡 위상과 날숨 위치/세기, 바람 영역, 노출 피부 목록과 자국 여부, 은신처 목록, 플레이어의 은신/가려짐 상태.
- **Unity:** 이 데이터로 시각 효과를 그린다. 감각 효과는 게임 판정에 영향을 주지 않는다.

## 1. 흐린 시야
- 카메라로부터 `senses.clearRange`까지는 선명하고, `senses.fogFullRange`에서 최대로 흐려진다 (깊이 기반 안개 + 블러, 툰 셰이딩과 어울리는 색 안개).
- 최대 흐림에서도 큰 가구의 실루엣은 보여야 한다 (길을 잃지 않게).
- 흐림 강도 (M12): 시작 위치에서 인간의 위치가 흐림만으로는 잘 드러나지 않고 CO₂ 흐름·체온 표시가 찾는 단서가 될 만큼 흐리게 한다. 확정값은 `spec/tuning.md` senses.* (clearRange 50, fogFullRange 160, fogMaxDensity 0.88, fogBlurPixels 6).
- **관망 (M13):** 어디든 붙은 지 `perch.delay`가 지나면 `perch.blendTime`에 걸쳐 선명 거리가 `perch.clearRangeMul`배, 최대 흐림 거리가 `perch.fogFullRangeMul`배로 넓어진다(50 → 150u, 160 → 400u). 떨어지면 같은 시간에 돌아온다. 천장·벽에 붙어 방을 정찰하는 플레이에 보상을 준다 (D-055).
- 스킬 "겹눈 각성"이 선명 범위를 늘리고, 증기 속에서는 `humid.steamClearRangeMul`배로 줄어든다 (spec/05).
- 1인칭과 3인칭 모두 같은 거리 기준을 쓴다 (3인칭은 카메라가 아니라 캐릭터 기준 거리).

## 2. CO₂ 시각화
- 인간은 `human.breathPeriod` 주기로 숨을 쉬고, 날숨 구간(`human.exhaleDuration`)마다 코/입에서 CO₂ 흐름을 내보낸다. 세기는 `human.co2Strength`이다 (취한 타겟은 `drunk.co2Mul` 배).
- CO₂ 흐름은 **안개의 영향을 받지 않고** `senses.co2VisibleRange` 안에서 보인다. 멀리서 타겟을 찾는 주 수단이다.
- 흐름은 위로 퍼지며 사라지고, 바람 영역(선풍기) 안에서는 바람 방향으로 빠르게 흩어진다 (후각 교란). 흩어질수록 흐려져 출처를 알기 어려워진다.
- 표현: 반투명 파스텔톤 연기 띠 (마법소녀 톤에 맞춘 은은한 반짝임).

## 3. 체온 감지
- 노출 피부(SkinSite)는 `senses.heatRange` 안에서 따뜻한 빛으로 보인다. 가까울수록 진하다.
- 체온 표시의 위상 (M12) (D-052): 부피 있는 반투명 막(굵게 키운 캡슐)처럼 "물질"로 보이면 안 된다. 피부 윤곽을 따라 흐르는 얇은 림/윤곽선이나 아지랑이처럼 미세하게 일렁이는 가는 선으로, 존재감은 낮되 눈에는 띄게 한다. 정보 위상은 공격 예고(spec/02 §7) < 그보다 낮다.
- 물린 자국은 실제로 문 자리(spec/04 §4 부착 지점)에 붉은 점으로 표시한다 (M12).
- 부위의 위험도(민감도)는 직접 표시하지 않는다. 플레이어가 경험으로 익히게 한다.

## 4. 은신처 표시
- Shadow Zone은 `hiding.cueRange` 안에서 가장자리가 은은하게 빛난다 (차가운 푸른빛).
- 인간의 어그로가 높을수록 표시가 진해진다: Safe 약하게, Suspicious 보통, Frenzy 강하게 + 가장 가까운 은신처 방향을 화면 가장자리에 표시.
- 은신처 안에 들어가면 비네트(spec/03)와 HUD의 "은신" 표시가 켜진다.
- 시선 차단: Yellow Zone 안이지만 장애물에 가려진 상태이면 HUD 눈 아이콘에 "가려짐" 표시를 한다 (spec/08).

## 수용 기준
- [x] 스냅샷에 호흡 위상, 날숨 위치·세기, 바람 영역, 피부 목록·자국 여부, 은신처 목록, InShadow·PlayerVisibleToHuman이 포함된다 (Core). — 증거: `HumanModifierTests.Snapshot_ContainsSensesSourceData`
- [x] 호흡 주기와 날숨 구간이 tuning 값을 따르고, 취한 타겟의 세기가 1.6배이다 (Core). — 증거: `HumanModifierTests.Breathing_PeriodAndExhaleFollowTuning_DrunkStrength16x`
- [x] clearRange 안의 물체는 흐림이 0이고, fogFullRange 밖은 최대 흐림이다 (Unity: 셰이더 파라미터 검사 + 캡처). — 증거: EditMode `SensesFogTests.Amount_InsideClearRange_IsZero_BeyondFullRange_IsMax`, `ClearRange_InSteam_ScaledBySteamMultiplier`, `Apply_SetsGlobalShaderParameters_FromPlayerOrigin`, `PcRenderer_HasFogPassBeforeTransparents_WithCompilingShader`, 캡처 `Captures/2026-10-01_145312/Stage_stage01_start.png` (D-039)
- [x] CO₂ 흐름이 최대 흐림 거리 밖에서도 보인다 (캡처 검토: Stage 1 시작 위치 1장). — 증거: `Captures/2026-10-01_150001/Stage_stage01_start.png`(머리 위 연기, 약 300u), EditMode `SensesViewTests.Co2_PuffsDuringExhale_VisibleBeyondFullFogUpToCo2Range`, `Co2Plume_RisesFadesAndExpires_StrongerBreathIsLarger` (D-040)
- [x] 바람 영역 안에서 CO₂ 흐름이 바람 방향으로 흩어진다 (캡처 검토: Stage 3). — 증거: `Captures/2026-10-01_165113/Gas_co2_wind_stage03.png`(옅지만 바람 방향 +Z로 기울어 흩어짐), EditMode `SensesViewTests.Co2Plume_InWind_DriftsWithWindAndDispersesSooner`
- [x] 체온 표시가 heatRange 안에서만 나타나고, 자국 부위에 표시가 붙는다 (Unity). — 증거: EditMode `SensesViewTests.Heat_OnlyInsideHeatRange_StrongerWhenCloser`, `BiteMark_DotOnlyOnMarkedSite_InsideHeatRange`, 캡처 `Stage_stage01_human_close.png`
- [x] 은신처 표시 강도가 어그로 상태에 따라 3단계로 바뀐다 (Unity). — 증거: EditMode `SensesViewTests.ShadowCue_IntensityStepsWithAwarenessState_HiddenBeyondCueRange`
- [x] 겹눈 각성 레벨에 따라 선명 범위가 늘어난다 (Unity). — 증거: EditMode `SensesFogTests.CompoundEyes_ExtendsClearRangePerLevel` (Stage는 스킬 반영 Tuning으로 SensesSettings를 만든다, D-043)
- [x] 다섯 스테이지 시작 위치 캡처에서 인간은 흐림 속에 묻히고 CO₂ 흐름은 보이며, 큰 가구 실루엣은 남는다 (캡처 검토). (M12) — 증거: `Captures/2026-10-01_223838/Stage_stage0*_start.png`·`_overview.png` 검토: 인간은 흐림 속 희미한 형체, 머리 위 CO₂ 분홍 줄기는 또렷, 소파·테이블·침대 윤곽은 남음. tuning `senses.clearRange` 50·`fogFullRange` 160·`fogMaxDensity` 0.88·`fogBlurPixels` 6, EditMode `SensesFogTests`(값 연동) 통과
- [x] 자국 점이 문 자리에 붙고 부위를 따라 움직인다 (Unity). (M12) — 증거: EditMode `SensesViewTests.BiteMark_DotAtTheBiteSpot_OnlyInsideHeatRange`, 캡처 `Captures/2026-10-01_223733/Stage_stage01_human_close.png`(손목 쪽 윗면의 붉은 점)
- [x] 체온 표시가 부피 막이 아닌 얇은 윤곽/일렁이는 선으로 그려지고, 피부 형상보다 두껍지 않다 (Unity + 캡처 검토). (M12) — 증거: EditMode `SensesViewTests.Heat_IsThinShimmer_NotAVolumeAroundTheSkin`(셰이더 `Moqui/HeatShimmer`, 굵기 ≤ 피부 반지름 × 1.05, `senses.heatGlowScale` 1.04), 캡처 `Captures/2026-10-01_223733/Stage_stage01_human_close.png`(팔뚝 윤곽을 따라 흐르는 가는 선)
- [x] 붙은 지 perch.delay 뒤 perch.blendTime에 걸쳐 선명·최대 흐림 거리가 넓어지고, 떨어지면 돌아온다 (Unity). (M13) — 증거: EditMode `SensesFogTests.Perch_AttachedAndStill_WidensClearAndFullRanges`
- [x] 천장에 붙어 내려다보면 방과 인간이 보인다 (캡처 검토). (M13) — 증거: `Captures/2026-10-05_191556/Stage_stage01_ceiling_tp.png`·`_ceiling_fp.png`(관망 1: 소파·인간 피부색·CO₂·바닥까지 보임)

## 범위 외
- 냄새(젖산 등) 별도 시각화, 색상 선호도 기반 타겟팅
