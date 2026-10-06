# Tuning — 수치 단일 출처

> 모든 값은 **제안값 v0**이다. 플레이 검증 후 조정한다. 변경 시 `plan/decisions.md`에 사유를 기록한다.
> 코드가 읽는 값은 `data/tuning.json`에 같은 키로 둔다 (예: `dash.cooldown`). Core 테스트가 이 문서와 json의 수치가 일치하는지 검사한다 (`tech/verification.md` §1).
> 단위: 거리 `u` (1u = 실제 1cm), 시간 `s`, 각도 `°`. 게이지는 0~100.

## world / camera
| 키 | 값 | 설명 |
|---|---|---|
| world.unitScale | 1u = 1cm | 월드 단위 |
| world.gravity | 981 u/s² | 물체용 중력. 플레이어는 중력 없음 |
| player.visualHeight | 1.0u | 캐릭터 시각 크기 (실제 모기보다 약 2배 과장) |
| player.collisionRadius | 0.4u | 플레이어 충돌 구 반지름 |
| camera.distance | 6u | 3인칭 거리 |
| camera.heightOffset | 1.2u | 피벗 위쪽 오프셋 |
| camera.fov | 70° | 수직 FOV |
| camera.nearClip | 0.05u | |
| camera.collisionRadius | 0.3u | 카메라 벽 충돌 구 |
| camera.pitchLimit | ±85° | |
| camera.defaultView | ThirdPerson | 저장 데이터가 없을 때 |
| camera.switchTime | 0.25s | 시점 전환 보간 |
| camera.returnSpeed | 12u/s | 벽에 막혀 당겨진 3인칭 카메라가 원래 거리로 돌아가는 속도. 당길 때는 즉시 (M13) |
| camera.pivotBlendTime | 0.2s | 피벗 기준 방향(월드 위 ↔ 붙은 면 법선)이 바뀔 때 보간 시간 (M13) |
| camera.hidePlayerDistance | 1.5u | 3인칭 카메라가 플레이어 중심에서 이보다 가까우면 모키를 숨긴다(그림자만) (M13) |
| camera.fp.eyeOffset | (0, 0.15, 0.1)u | 캐릭터 머리 기준 로컬 오프셋. 충돌 구 반지름 안 |
| camera.fp.fov | 80° | 1인칭 수직 FOV |
| camera.fp.nearClip | 0.01u | |
| camera.fp.dashFovKick | +10° | 대시 중 FOV 펄스 |
| camera.fp.attachedLookLimit | 80° | 부착 시 법선 기준 시선 제한 |
| input.mouseSensitivity | 0.12 °/px | 설정에서 0.25~4배 조절 |
| input.gamepadLookSpeed | 180 °/s | |

## flight
| 키 | 값 | 설명 |
|---|---|---|
| flight.speed | 60 u/s | 수평/전후 이동 속도 |
| flight.verticalSpeed | 45 u/s | 상승/하강 속도 |
| flight.precisionSpeedMul | 0.35 | 정밀 비행 시 속도 배율 |
| flight.accelTime | 0.12s | 정지 → 최고 속도 (약한 관성) |
| flight.decelTime | 0.15s | 최고 속도 → 정지 |

## dash (볼텍스 대시)
| 키 | 값 | 설명 |
|---|---|---|
| dash.distance | 60u | |
| dash.duration | 0.12s | 등속 이동 |
| dash.staminaCost | 25 | |
| dash.cooldown | 0.5s | 대시 시작 시점 기준 |
| dash.noiseRadius | 150u | 소음 핑 반경 |
| dash.noiseAwareness | +40 | 반경 내 인간 경계 즉시 증가량 |
| dash.chainWindow | 0.3s | 연속 와류 스킬의 추가 대시 허용 시간 |

## stamina
| 키 | 값 | 설명 |
|---|---|---|
| stamina.max | 100 | |
| stamina.regenRate | 20 /s | |
| stamina.regenDelay | 1.0s | 마지막 소모 후 회복 시작까지 |
| stamina.exhaustedDuration | 2.0s | 0이 되면 탈진 |
| stamina.exhaustedSpeedMul | 0.5 | 탈진 중 이동 속도 배율 |

## noise (비행 소음)
| 키 | 값 | 설명 |
|---|---|---|
| noise.flightRadius | 50u | 공중에 있는 동안 상시 발생 |
| noise.flightAwarenessRate | +8 /s | 반경 내 인간 경계 증가율 |
| noise.precisionRadiusMul | 0.5 | 정밀 비행 시 반경 배율 |
| noise.attachedRadius | 0u | 부착 상태 |

## hearing (귀 기준 청각)
| 키 | 값 | 설명 |
|---|---|---|
| hearing.nearMul | 2.0 | 귀 거리 0에서 비행 소음 증가율 배율 |
| hearing.farMul | 0.5 | 반경 끝에서 배율 (선형 보간) |
| hearing.earZoneRadius | 15u | 귀 근접 구역 |
| hearing.earZoneRate | +30 /s | 귀 근접 구역 추가 경계 증가 |

## vision
| 키 | 값 | 설명 |
|---|---|---|
| vision.yellow.halfAngle | 50° | |
| vision.yellow.range | 300u | |
| vision.yellow.rateNear | +25 /s | 거리 0일 때 |
| vision.yellow.rateFar | +8 /s | 최대 거리일 때 (선형 보간) |
| vision.red.halfAngle | 8° | |
| vision.red.range | 35u | 진입 시 즉시 박수 공격 |
| vision.attachedMul | 0.3 | 부착 상태 시야 증가율 배율 |
| vision.shadowMul | 0 | Shadow Zone 안 |
| vision.losCheckInterval | 0.1s | |

## awareness (어그로)
| 키 | 값 | 설명 |
|---|---|---|
| awareness.suspiciousEnter | 40 | 이상이면 의심 |
| awareness.suspiciousExit | 20 | 미만이면 평온 (히스테리시스) |
| awareness.frenzyEnter | 100 | 도달 시 광분 |
| awareness.decayDelay | 2.0s | 자극이 없을 때 감소 시작까지 |
| awareness.decayRate | 10 /s | |
| awareness.shadowDecayRate | 25 /s | 플레이어가 Shadow Zone에 있을 때 |
| head.idleTurnSpeed | 60 °/s | |
| head.suspiciousTurnSpeed | 90 °/s | |
| head.frenzyTurnSpeed | 180 °/s | |
| head.yawLimit | ±100° | 몸통 정면 기준 |
| head.pitchLimit | ±60° | 몸통 정면 기준. 의심·광분 중 목표를 향해 고개를 드는 한계 (D-029) |
| head.suspiciousStareTime | 2s | 의심 상태에서 마지막 자극 위치를 응시하는 시간 (spec/02 §3) |
| head.searchAngle | ±45° | 응시 후 좌우 탐색 폭, 자극 방향 기준 (D-029) |
| head.turnAccelTime | 0.25s | 머리가 멈춘 상태에서 최고 회전 속도에 이르는 시간. 남은 각에 맞춰 감속해 멈춘다 (자연스러운 움직임, M14) |

## frenzy (광분)
| 키 | 값 | 설명 |
|---|---|---|
| frenzy.minDuration | 8s | 진입 후 최소 유지 |
| frenzy.calmTime | 6s | 연속으로 보지 못해야 진정 |
| frenzy.exitValue | 60 | 진정 시 경계 값 |
| frenzy.slapInterval | 0.4s | 손바닥 공격 사이 대기 |
| frenzy.slapTelegraph | 0.45s | 광분 중 손바닥·맹목 휘두르기 예고 |
| frenzy.blindSwatRadius | 30u | 마지막 위치 주변 |
| frenzy.blindSwatInterval | 1.0~1.5s | 무작위 |
| frenzy.reactionMul | 4.0 | 피부 반응 확률 배율 |

## reaction (확률 기반 피격 이벤트)
| 키 | 값 | 설명 |
|---|---|---|
| reaction.landChance | 0.10 | 착지 순간 반응 확률 (민감도 1 기준) |
| reaction.baseRate | 0.02 /s | 부착 중 기본 위험률 |
| reaction.itchRate | 0.25 /s | 가려움 100일 때 추가 위험률 (가려움 제곱 비례) |
| reaction.earRate | 0.3 /s | 귀 근접 구역 위험률 |

## human (움직임 · 호흡)
| 키 | 값 | 설명 |
|---|---|---|
| human.actionInterval | 4~9s | 무작위 동작 간격 |
| human.dislodgeSpeed | 60 u/s | 부위 속도가 이를 넘으면 튕겨남 |
| human.dislodgePush | 5u | |
| human.dislodgeStun | 0.3s | 입력 무시 |
| human.walkSpeed | 70u/s | 걷는 인간의 평소 걸음 (M14, 실내에서 느긋하게 걷기 0.7 m/s) |
| human.walkTurnSpeed | 120°/s | 걷는 인간의 몸 회전 |
| human.chaseSpeed | 110u/s | 광분 추격 걸음 |
| human.walkRadius | 18u | 골반 높이에서 가구 충돌을 보는 구 반지름 |
| human.stepLength | 60u | 한 걸음 길이 (다리 흔들기 주기) |
| human.legSwing | 18u | 걸을 때 무릎·발 쪽이 앞뒤로 흔들리는 폭 (걷는 속도에서 최고 약 66u/s > human.dislodgeSpeed: 종아리·발에 붙으면 튕길 수 있음) |
| human.walkAccelTime | 0.5s | 걷기 가감속: 걷는 속도까지 걸리는 시간, 경로점·멈춤 앞에서 미리 줄인다 (M14) |
| human.walkBob | 2.5u | 걸음마다 골반이 위아래로 출렁이는 폭 (M14) |
| human.alarmShare | 70 | 두 사람: 한 사람이 광분하면 다른 사람의 경계를 이 값까지 올린다 (M14) |
| human.breathPeriod | 4s | 호흡 주기 |
| human.exhaleDuration | 1.5s | 날숨 구간 |
| human.co2Strength | 1.0 | CO₂ 흐름 세기 |

## attack
| 키 | 값 | 설명 |
|---|---|---|
| attack.clap.telegraph | 0.35s | Red Zone 즉사기 |
| attack.clap.radius | 15u | `attack.clap.offset` 지점 중심 |
| attack.clap.offset | 25u | 판정 중심: 얼굴 앞 거리 (spec/02 §7) |
| attack.clap.recovery | 1.0s | |
| attack.slap.radius | 12u | 손바닥 판정 구 반경. 목표는 예고 시작 시점의 플레이어 위치에 고정 |
| attack.slap.recovery | 1.5s | |
| attack.selfSlap.telegraph | 0.5s | 반응 때리기 예고 |
| attack.selfSlap.radius | 10u | 예고 시작 시점의 모기 위치 중심 |
| attack.selfSlap.recovery | 1.0s | |
| attack.swatter.length | 45u | 전기 모기채: 손목 앞으로 늘어나는 길이 (채 머리 중심까지, M14) |
| attack.swatter.radius | 22u | 전기 모기채 판정 반경 (손바닥 12u보다 넓다) |
| attack.handPeakSpeed.frenzy | 500u/s | 광분 손바닥·맹목 휘두르기·박수 손 최고 속도. 타격 시간 = 손 경로 ÷ (최고 속도 ÷ 1.5) (D-052, knowledge/human-arm-motion.md) |
| attack.handPeakSpeed.reaction | 400u/s | 반사적 자기 몸 치기 |
| attack.handPeakSpeed.drunk | 300u/s | 취한 사람 무작위 휘두르기 |
| attack.minTelegraph | 0.2s | 예고 최소 시간(사람 반응 시간 하한). 자세 전환 시간 + 이 값보다 짧은 예고는 늘린다 |

### 인간 몸 (D-052·D-053, knowledge/human-arm-motion.md)
| 키 | 값 | 설명 |
|---|---|---|
| human.handReachExtra | 9u | 손목 → 손바닥 중심 (팔 길이 = 위팔 + 아래팔 + 이 값, 데이터 캡슐에서 계산) |
| human.elbowFlexMax | 150° | 팔꿈치 최대 굽힘 (AAOS) |
| human.shoulderExtensionMax | 60° | 어깨 폄 한계. 몸 뒤쪽은 늘어뜨린 팔에서 이 각도 안만 닿는다 |
| posture.maxLeanAngle | 35° | 상체 최대 기울기 |
| posture.maxTwist | 60° | 상체 최대 비틀기 |
| posture.riseLift | 30u | 완전히 일어설 때 골반 상승 |
| posture.riseForward | 15u | 완전히 일어설 때 골반 전진 |
| posture.leanTime | 0.4s | 최대 기울기까지 |
| posture.turnTime | 0.4s | 최대 비틀기까지 |
| posture.riseTime | 1.2s | 완전히 일어서기까지 (급히 일어남) |

## suck (흡혈)
| 키 | 값 | 설명 |
|---|---|---|
| suck.rateStart | 2 %/s | 세션 시작 속도 |
| suck.rateMax | 6 %/s | 세션 최대 속도 |
| suck.rampTime | 6s | 시작 → 최대 가속 시간 |
| suck.itchRate | 6 /s | 흡혈 중 가려움 증가 (민감도 1 기준) |
| suck.itchDecay | 5 /s | 흡혈하지 않을 때 감소 |
| suck.itchThreshold | 100 | 도달 시 즉시 반응 |
| suck.attachRange | 2u | 표면 부착 가능 거리 |
| suckEvent.twitchItchStart | 40 | 긁으러 오는 손: 첫 움찔 가려움 (M13, D-056) |
| suckEvent.twitchItchStep | 20 | 움찔 단계 간격 (40·60·80) |
| suckEvent.twitchReachStart | 0.25 | 움찔할 때 손이 문 자리까지 가는 비율 (첫 단계) |
| suckEvent.twitchReachStep | 0.2 | 단계마다 더하는 비율 |
| suckEvent.twitchDuration | 0.6s | 움찔 한 번 |
| suckEvent.shiftRate | 0.08/s | 부위가 움직임: 흡혈 중 위험률 |
| suckEvent.shiftTelegraph | 0.8s | 움직이기 전 예고 |
| suckEvent.shiftDuration | 1.2s | 부위가 옆으로 갔다 돌아오는 시간 |
| suckEvent.shiftDistance | 30u | 부위 이동량 (최고 ≈ 78u/s > human.dislodgeSpeed) |
| suckEvent.gripMul | 2 | 움직이는 동안 Suck을 누르고 버티면 튕김 기준 속도 배율 |
| suckEvent.shiftRateMul | 1.5 | 버티는 동안 흡혈 속도 배율 (가려움은 오르지 않음) |
| suckEvent.glanceRate | 0.03/s | 시선: 흡혈 중 위험률 × (0.5 + 가려움/100) |
| suckEvent.glanceTurnTime | 0.8s | 문 자리로 머리를 돌리는 예고 |
| suckEvent.glanceTurnSpeed | 120°/s | 그때 머리 회전 속도 |
| suckEvent.glanceHold | 1.2s | 응시 시간 |
| suckEvent.glanceNoticeAwareness | 70 | 응시 중 흡혈하는 모기를 보면 오르는 경계 (평소 경계 30 이상이면 광분, D-057) |
| attach.detachOffset | 2u | 이탈 시 표면 법선 방향으로 떨어지는 거리 (spec/03, D-031) |

## site (부위 유형)
| 키 | 민감도 | 혈액량 |
|---|---|---|
| site.forearm | 1.0 | 1.0 |
| site.calf | 0.6 | 0.8 |
| site.footTop | 0.5 | 0.7 |
| site.neck | 1.6 | 1.4 |
| site.cheek | 2.2 | 1.6 |

## biteMark (물린 자국)
| 키 | 값 | 설명 |
|---|---|---|
| biteMark.minAmount | 5% | 세션에서 이 이상 빨아야 자국 |
| biteMark.awarenessBump | +15 | 세션 종료 시 경계 증가 |
| biteMark.gainMulPerBite | 0.2 | 경계 증가 배율 1 + 0.2n |
| biteMark.decayDivPerBite | 0.25 | 경계 감소 배율 1 / (1 + 0.25n) |
| biteMark.floorPerBite | 8 | 경계 하한 8n |
| biteMark.floorMax | 45 | 하한 최대 (의심 진입선 40보다 높음) |
| biteMark.reactionMulPerBite | 0.15 | 반응 확률 배율 1 + 0.15n |

## satiety (포만)
| 키 | 값 | 설명 |
|---|---|---|
| satiety.minSpeedMul | 0.6 | 흡혈 100%일 때 이동 속도 배율 |
| satiety.minDashMul | 0.75 | 흡혈 100%일 때 대시 거리 배율 |

## senses (모기 감각, 표현 전용)
| 키 | 값 | 설명 |
|---|---|---|
| senses.clearRange | 50u | 선명한 거리 (M12: CO₂ 단서가 쓸모 있도록 80 → 50) |
| senses.fogFullRange | 160u | 최대 흐림 거리 (M12: 250 → 160) |
| senses.fogMaxDensity | 0.88 | 최대 흐림에서 안개색 비율 (1 미만이어야 큰 가구 실루엣이 남는다, M12: 0.75 → 0.88) |
| senses.fogBlurPixels | 6px | 최대 흐림에서 블러 반경 (1080p 기준, M12: 3 → 6) |
| perch.delay | 0.5s | 붙은 뒤 관망이 시작되기까지 (M13) |
| perch.blendTime | 1.0s | 관망 시야로 넓어지는/돌아오는 시간 (M13) |
| perch.clearRangeMul | 3 | 관망 중 선명 거리 배율 (50 → 150u) (M13) |
| perch.fogFullRangeMul | 2.5 | 관망 중 최대 흐림 거리 배율 (160 → 400u) (M13) |
| senses.co2VisibleRange | 450u | CO₂ 흐름 표시 거리 |
| senses.heatRange | 60u | 체온 표시 거리 |
| senses.co2PuffInterval | 0.12s | 날숨 중 CO₂ 연기 덩이 생성 간격 |
| senses.co2PuffLifetime | 3s | 연기 덩이가 사라질 때까지 시간 |
| senses.co2RiseSpeed | 12u/s | 연기 덩이 상승 속도 |
| senses.co2ForwardSpeed | 15u/s | 날숨 방향(머리 정면) 초기 속도, 수명 동안 0으로 줄어듦 |
| senses.co2PuffStartRadius | 2u | 연기 덩이 시작 반지름 (세기 배율 적용) |
| senses.co2PuffEndRadius | 10u | 연기 덩이 끝 반지름 (세기 배율 적용) |
| senses.heatGlowScale | 1.04 | 체온 윤곽의 굵기 배율 (피부 부위 반지름 기준). 피부를 덮는 막이 아니라 얇은 림·아지랑이 선 (M12, D-052) |
| senses.biteMarkDotRadius | 1.5u | 물린 자국 붉은 점 반지름 |
| hiding.cueRange | 120u | 은신처 표시 거리 |
| hiding.cueIntensitySafe | 0.35 | 은신처 표시 강도 — Safe |
| hiding.cueIntensitySuspicious | 0.65 | 은신처 표시 강도 — Suspicious |
| hiding.cueIntensityFrenzy | 1.0 | 은신처 표시 강도 — Frenzy |
| hiding.debuffRecoveryMul | 2.0 | 숨은 상태의 디버프 회복 배율 (중독·젖은 날개·습기·탈진) |

## shadow
| 키 | 값 | 설명 |
|---|---|---|
| shadow.vignetteIntensity | 0.4 | 진입 시 화면 어둡게 |
| shadow.transitionTime | 0.3s | |

## water (물방울)
| 키 | 값 | 설명 |
|---|---|---|
| water.dropInterval | 2.5s | 발생원별 |
| water.dropRadius | 1.5u | |
| water.trappedFallSpeed | 100 u/s | 갇힌 상태 낙하 속도 (등속) |
| water.escapePresses | 2 | 대시 입력 횟수 |
| water.minSourceHeight | 150u | 착지면 기준 최소 높이 |
| wetWings.duration | 10s | |
| wetWings.speedMul | 0.7 | |
| wetWings.regenMul | 0.5 | |
| wetWings.dashCostAdd | +10 | |

## gimmick: fan
| 키 | 값 | 설명 |
|---|---|---|
| fan.windSpeed | 40 u/s | 플레이어 위치에 더해지는 밀림 속도 |
| fan.range | 250u | |
| fan.halfAngle | 25° | 바람 원뿔 |
| fan.oscillationAngle | ±45° | |
| fan.oscillationPeriod | 8s | |
| fan.noiseMaskRadius | 200u | 이 반경 안에서 플레이어 소음 반경 배율 적용 |
| fan.noiseMaskMul | 0.5 | |
| aircon.windSpeed | 70u/s | 에어컨 바람 (방향 고정, 선풍기보다 셈, M14) |
| aircon.range | 320u | 에어컨 바람이 닿는 거리 |
| aircon.halfAngle | 18° | 에어컨 바람 원뿔 반각 |
| light.visionMul | 1.6 | 켜진 조명 영역 안 플레이어에 대한 인간 시각 증가 배율 (M14) |
| light.switchDelay | 1.5s | 인간이 의심·광분한 뒤 불을 켜기까지 |
| light.offDelay | 6s | 평온이 이만큼 이어지면 불을 끈다 |
| net.rustleRadius | 80u | 모기장 틈을 정밀 비행 없이 지날 때 소음 반경 (M14) |
| net.rustleAwareness | 15 | 그 소음을 들은 인간의 경계 증가 |

## gimmick: web
| 키 | 값 | 설명 |
|---|---|---|
| web.struggleTime | 1.5s | 접촉 후 Game Over 연출 시간 (탈출 불가) |

## gimmick: drunk target
| 키 | 값 | 설명 |
|---|---|---|
| drunk.suckRateMul | 2.0 | |
| drunk.itchRateMul | 0.6 | |
| drunk.visionRateMul | 0.7 | |
| drunk.randomSwatInterval | 4~7s | 무작위 |
| drunk.randomSwatRadius | 40u | 몸 주변 무작위 지점 |
| drunk.slapTelegraph | 0.8s | |
| drunk.co2Mul | 1.6 | CO₂ 세기 배율 |

## gimmick: spray (모기약)
| 키 | 값 | 설명 |
|---|---|---|
| spray.telegraph | 0.6s | 캔을 드는 예고 |
| spray.useRange | 150u | 인간이 분사하는 최대 거리 |
| spray.cooldown | 8s | |
| spray.travel | 30u | 손에서 연무 중심까지 |
| spray.radiusStart | 25u | |
| spray.radiusMax | 60u | |
| spray.expandTime | 1.0s | |
| spray.lifetime | 8s | |
| spray.windDriftMul | 0.5 | 바람 속 떠밀림 = fan.windSpeed × 0.5 |
| spray.toxinRate | +30 /s | 연무 안 중독 증가 |
| spray.toxinDecay | 15 /s | 밖에서 감소 |
| spray.tier1 | 30 | 끊김 |
| spray.tier2 | 55 | 반전 |
| spray.tier3 | 80 | 랜덤 |
| spray.stutterInterval | 0.5s | |
| spray.stutterChanceMin | 0.1 | 중독 tier1에서 끊김 확률 |
| spray.stutterChance | 0.3 | 중독 tier2에서 끊김 확률 (선형 보간) |
| spray.stutterDuration | 0.25s | |
| spray.randomInterval | 0.4s | |
| spray.randomDuration | 0.2s | |
| spray.dispenserInterval | 12s | 자동 분사기 |

## gimmick: coil (모기향)
| 키 | 값 | 설명 |
|---|---|---|
| coil.range | 400u | 영향 범위 |
| coil.denseRadius | 60u | 이 안은 최대 하한 |
| coil.nearFloor | 60 | 중독 하한 (반전 단계) |
| coil.farFloor | 30 | 범위 끝 하한 (약한 끊김) |
| coil.lethalRadius | 20u | |
| coil.coreRate | +25 /s | lethalRadius 안 추가 중독 |
| coil.windFloorMul | 0.5 | 바람 원뿔 안 하한 배율 |
| coil.shadowFloorMul | 0.5 | Shadow Zone 안 하한 배율 |

## humid (습기)
| 키 | 값 | 설명 |
|---|---|---|
| humid.gainStrong | +15 /s | 강한 습기(증기) 영역 |
| humid.gainWeak | +5 /s | 약한 습기 영역 |
| humid.decay | 10 /s | 영역 밖 |
| humid.steamVisionMul | 0.6 | 증기 속 플레이어에 대한 인간 시각 증가율 배율 |
| humid.steamClearRangeMul | 0.5 | 증기 속 플레이어 선명 시야 배율 |

## doze (졸음 수정자)
| 키 | 값 | 설명 |
|---|---|---|
| doze.sleepDuration | 8~14s | 졸기 |
| doze.wakeDuration | 2~4s | 깜빡 깸 |
| doze.wakeTelegraph | 0.5s | 깨기 전 움찔 예고 |
| doze.hearingMul | 0.5 | 졸기 중 청각 증가 배율 |
| doze.reactionMul | 0.5 | 졸기 중 반응 확률 배율 |
| doze.resleepDelay | 5s | 경계 20 미만 유지 후 다시 졺 |
| doze.frenzyDurationMul | 0.5 | 광분 최소 유지 시간 배율 |

## meta
| 키 | 값 | 설명 |
|---|---|---|
| meta.clearReward | 100 | 혈액 포인트 |
| meta.noFrenzyBonus | +50 | 광분 0회 |
| meta.carefulBiteMax | 2 | 신중한 흡혈 기준 자국 수 |
| meta.carefulBiteBonus | +40 | |
| meta.parTimeBonus | +30 | 기준 시간 이내 |
| meta.parTime.default | 150s | 레벨별 키가 없을 때 기준 시간 (M14: 키 = meta.parTime.(레벨 ID)) |
| meta.parTime.stage01 | 150s | 튜토리얼 |
| meta.parTime.stage02 | 120s | |
| meta.parTime.stage03 | 150s | |
| meta.parTime.stage04 | 150s | |
| meta.parTime.stage05 | 180s | |
| skill.cost.A | 60 / 120 / 200 | |
| skill.cost.B | 80 / 160 / 260 | |

## skill (레벨당 효과, spec/09)
| 키 | 값 | 설명 |
|---|---|---|
| skill.resistSpray.toxinMul | 0.8 | 곱. 중독 증가와 모기향 하한 모두 |
| skill.resistWet.durationMul | 0.75 | 곱. 3레벨에서 탈출 입력 −1 |
| skill.resistWet.humidMul | 0.8 | 습기 증가에 곱 |
| skill.resistSatiety.penaltyMul | 0.75 | 감속 폭(1 − 배율)에 곱 |
| skill.silentWings.noiseMul | 0.88 | 곱 |
| skill.swiftWings.speedMul | 1.08 | 곱 |
| skill.vortexControl.accelTimeMul | 0.8 | 곱, 가속·감속 시간 모두 |
| skill.vortexControl.maxLevelDashCooldownMul | 0.7 | 최대 레벨(3)에서 대시 쿨타임에 곱 (D-051) |
| skill.stamina.maxAdd | +15 | 합 |
| skill.stamina.regenMul | 1.1 | 곱 |
| skill.featherLanding.landChanceMul | 0.7 | 곱 |
| skill.numbingSaliva.itchMul | 0.85 | 곱 |
| skill.shadowBlend.attachedMulAdd | −0.05 | 합 |
| skill.shadowBlend.calmTimeAdd | −1s | 합 |
| skill.magicWand.rateMaxMul | 1.15 | 곱 |
| skill.magicWand.rampTimeAdd | −1s | 합 |
| skill.compoundEyes.clearRangeAdd | +40u | 합 |
| skill.compoundEyes.heatRangeAdd | +20u | 합 |
| skill.decoy.range | 100u | |
| skill.decoy.duration | 3s | |
| skill.decoy.noiseRadius | 80u | |
| skill.decoy.noiseRate | +12 /s | |
| skill.decoy.cooldown | 20s / 14s | 레벨별 |

## hud (표현 전용, spec/08)
| 키 | 값 | 설명 |
|---|---|---|
| hud.satietyHighlightMul | 0.85 | 포만 감속 배율이 이 값 아래면 포만 아이콘 강조 |
| hud.resultDelay | 1.5s | 클리어·사망 후 Result 화면까지 (사망 연출 시간) |

## tutorial (spec/08 §튜토리얼)
| 키 | 값 | 설명 |
|---|---|---|
| tutorial.holdSeconds | 1s | 이동·정밀 비행 안내를 끝내는 입력 유지 시간 |
| tutorial.lookDegrees | 90° | 시점 안내를 끝내는 누적 회전 각도 |
| tutorial.infoTimeout | 30s | 설명형 안내(Stage 2)가 행동 없이 넘어가는 시간 |

## performance
| 키 | 값 | 설명 |
|---|---|---|
| perf.targetFps | 60 | 1080p 기준 |
