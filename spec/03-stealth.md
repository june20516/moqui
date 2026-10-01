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
- 플레이어가 `attachable` 플래그를 가진 표면(가구, 벽, 인간 피부 `SkinSite`)에서 `suck.attachRange` 이내일 때 Attach 입력으로 부착한다.
- 부착 시: 표면 법선에 맞춰 정렬, 이동 정지, 소음 0 (`noise.attachedRadius`), 시각 증가율에 `vision.attachedMul` 적용 (스킬 그림자 동화로 감소). 표면이 움직이면 그 로컬 좌표를 따라간다.
- 이동 입력이나 Attach 입력으로 이탈한다. 이탈 시 법선 방향으로 2u 떨어진다.
- 부착 가능할 때 HUD에 프롬프트 "F: 착지"를 표시한다 (spec/08).

## 수용 기준
- [ ] Shadow Zone 안에서는 Yellow Zone이어도 시각 증가가 0이다 (Core).
- [ ] Shadow Zone 안에서 경계가 25/s로 감소한다 (Core).
- [ ] Shadow Zone 안에서 대시하면 경계가 +40 오른다 (Core).
- [ ] 진입 시 비네트가 0.3초에 걸쳐 0.4가 된다 (Unity).
- [ ] Shadow Zone 안에서 중독·젖은 날개·습기·탈진이 2배 빠르게 회복된다 (Core).
- [ ] 부착 상태에서는 비행 소음이 발생하지 않는다 (Core: 반경 안 인간 경계 증가 0).
- [ ] 부착 상태의 시각 증가율이 비부착 대비 0.3배이다 (Core).
- [ ] 2u보다 먼 표면에서는 부착되지 않는다 (Core).
- [ ] 부착 시 캐릭터 up 벡터가 표면 법선과 5° 이내로 정렬된다 (Core).
- [ ] 레벨 데이터의 모든 가구 형상에 obstacle 플래그가 있다 (Core: 레벨 데이터 검사).

## 범위 외
- 빛의 밝기 기반 동적 은신 계산 (Shadow Zone은 수작업 볼륨으로만 처리)
