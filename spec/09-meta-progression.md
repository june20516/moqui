# 09. 메타 성장 (스킬 트리) · 저장

## 목적
스테이지 사이의 성장으로 재도전 동기와 플레이 스타일 선택지를 준다. 스킬은 원킬 긴장감을 없애지 않는 범위에서 "실수의 여유"와 "숙련의 보상"을 준다. (D-013)

## 1. 혈액 포인트
- 클리어 시 `meta.clearReward`를 지급한다.
- 보너스:
  - 광분 0회: `meta.noFrenzyBonus`
  - 물린 자국 `meta.carefulBiteMax`개 이하: `meta.carefulBiteBonus` (신중한 흡혈)
  - 스테이지별 `meta.parTime` 이내: `meta.parTimeBonus`
- 반복 클리어해도 매번 지급한다. 실패 시 지급하지 않으며 잃는 것도 없다.

## 2. 스킬
- 패시브 스킬은 레벨별로 구매한다. 효과는 레벨마다 누적되며, "(합)" 표기가 없으면 곱연산이다.
- 액티브 스킬은 하나만 장착하고, Skill 입력(spec/01)으로 사용한다.
- 비용 단계: **A** = 60 / 120 / 200, **B** = 80 / 160 / 260.
- 수치 키는 `spec/tuning.md`의 skill 섹션에 있다.

### 저항
| ID | 이름 | 효과 / 레벨 | 레벨 | 비용 |
|---|---|---|---|---|
| resistSpray | 해독 체질 | 중독 증가와 모기향 하한 −20% | 3 | A |
| resistWet | 발수 코팅 | 젖은 날개 지속시간 −25%, 습기 증가 −20%. 3레벨에서 물방울 탈출 입력 −1회 | 3 | A |
| resistSatiety | 소화 촉진 | 포만 감속 폭 −25% | 3 | A |

### 능력치
| ID | 이름 | 효과 / 레벨 | 레벨 | 비용 |
|---|---|---|---|---|
| silentWings | 고요한 날개 | 모든 소음 반경 −12% | 3 | B |
| swiftWings | 순풍 | 비행 속도 +8% | 3 | A |
| vortexControl | 와류 제어 (방향 전환) | 가속·감속 시간 −20% (관성 감소, 더 날카로운 방향 전환). 3레벨: 대시 쿨타임 −30% (M12: 대시가 진행 방향 전체를 쓰게 되어 "대각선 대시" 효과를 대체, D-051) | 3 | B |
| stamina | 지구력 | 최대 스태미나 +15(합), 회복 +10% | 3 | A |
| featherLanding | 깃털 착지 (조심히 앉기) | 착지 반응 확률 −30% | 3 | A |
| numbingSaliva | 마취 타액 (조심히 빨기) | 흡혈 중 가려움 증가 −15% | 3 | B |
| shadowBlend | 그림자 동화 (숨기) | 부착 시 시각 배율 −0.05(합), 광분 진정 시간 −1s(합) | 3 | B |

### 연속 회피
| ID | 이름 | 효과 | 레벨 | 비용 |
|---|---|---|---|---|
| chainVortex | 연속 와류 | 1레벨: 대시 후 `dash.chainWindow` 안에 쿨타임을 무시하고 1회 추가 대시. 2레벨: 그 추가 대시는 스태미나를 쓰지 않음 | 2 | B |

### 마법봉
| ID | 이름 | 효과 / 레벨 | 레벨 | 비용 |
|---|---|---|---|---|
| magicWand | 마법봉 강화 | 최대 흡혈 속도 +15%, 가속 시간 −1s(합) | 3 | B |

### 기타 (제안)
| ID | 이름 | 효과 | 레벨 | 비용 |
|---|---|---|---|---|
| compoundEyes | 겹눈 각성 | 선명 시야 +40u(합), 체온 감지 +20u(합) | 2 | A |
| decoyCharm | 미끼 마법 (액티브) | 조준 방향 `skill.decoy.range` 지점에 `skill.decoy.duration` 동안 날갯소리 미끼를 만든다. 인간은 미끼를 진짜 소음처럼 듣는다. 2레벨에서 쿨타임 감소 | 2 | B |

- 타격을 무효화하는 방어 스킬은 원킬 원칙을 해치므로 두지 않는다 (D-017).

## 3. 저장
- 저장 포맷과 직렬화는 Core가, 파일 위치와 읽기/쓰기는 저장소 포트(`ISaveStorage`)의 Unity 구현이 맡는다 (Unity: `Application.persistentDataPath/save.json`). 저장 내용: 혈액 포인트, 스킬 레벨, 장착 액티브, 스테이지별 클리어 여부/최고 시간/광분 0회 여부/최소 자국 수, 저장 포맷 버전.
- 결과 화면 진입과 구매 시 즉시 저장한다.
- 파일이 없거나 손상되었으면 기본값으로 시작하고 손상 파일은 `save.corrupt.json`으로 이름을 바꿔 둔다.
- Title에 "데이터 초기화"(확인 대화상자 포함)를 둔다.

## 수용 기준
- [x] 보상 계산: 기본/광분 0회/신중한 흡혈/기준시간 조합 8가지가 맞다 (Core). — 증거: `MetaTests.Reward_AllEightCombinations`(8케이스)
- [x] 포인트가 부족하면 구매할 수 없고, 최대 레벨 이후 구매할 수 없다 (Core). — 증거: `MetaTests.Purchase_NeedsEnoughPoints_AndStopsAtMaxLevel`, `Costs_FollowTierTables`, `Equip_OnlyOwnedActiveSkills`
- [x] 모든 패시브 스킬 효과가 레벨별로 정확히 적용된다 (스킬당 1개 테스트) (Core). — 증거: `SkillTests.ResistSpray_*`, `ResistWet_*`, `ResistSatiety_*`, `SilentWings_*`, `SwiftWings_*`, `Stamina_*`, `FeatherLanding_*`, `NumbingSaliva_*`, `ShadowBlend_*`, `MagicWand_*`, `CompoundEyes_*`, `VortexControl_*` (D-043). 해독 체질의 중독·모기향 적용은 M9에서 기믹과 함께 검증
- [ ] 와류 제어가 레벨당 가감속 시간을 0.8배로 줄이고, 3레벨에서 대시 쿨타임이 0.7배가 된다 (Core). (M12 플레이테스트 반영, 미구현)
- [x] 연속 와류: 창 안의 추가 대시가 쿨타임을 무시하고 체인당 1회만 허용되며, 2레벨에서 스태미나가 줄지 않는다 (Core). — 증거: `SkillTests.ChainVortex_ExtraDashInsideWindowIgnoresCooldown_OncePerChain`, `ChainVortex_Level1CostsStamina_Level2Free`
- [x] 미끼가 지정 지점에서 지속시간 동안 NoiseEvent를 발생시키고, 인간의 마지막 자극 위치가 미끼로 바뀐다 (Core). — 증거: `SkillTests.Decoy_EmitsNoiseAtAimPointForDuration_LastStimulusMovesToDecoy`, `Decoy_NotEquipped_DoesNothing`
- [x] 저장 → 로드 왕복 후 모든 필드가 같다 (Core). — 증거: `MetaTests.SaveLoad_RoundTrip_AllFieldsEqual`
- [x] 손상된 save.json에서도 예외 없이 기본값으로 시작하고 손상 파일을 보존한다 (Core). — 증거: `MetaTests.CorruptSave_StartsWithDefaults_KeepsCorruptFile`(4케이스), `MissingSave_StartsWithDefaults_Reset_DeletesFile`

## 범위 외
- 스킬 초기화(리스펙), 다중 세이브 슬롯, 클라우드 저장, 리더보드
