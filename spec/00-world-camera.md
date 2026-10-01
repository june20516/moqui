# 00. 월드 스케일 & 카메라

## 목적
극소 스케일에서도 물리, 카메라, 부동소수 정밀도가 안정적으로 동작하는 단위계를 고정한다. 이후 모든 spec은 이 단위를 따른다.

## 규칙
- 1u = 실제 1cm (`world.unitScale`). 방 하나는 대략 500 × 250 × 400u이다.
- 플레이어 캐릭터는 시각 높이 `player.visualHeight`, 충돌 구 반지름 `player.collisionRadius`를 가진다.
- 좌표계는 Y-up, +Z 전방, 왼손 좌표계이다 (Unity와 같게 두어 어댑터 변환을 없앤다).
- 플레이어 이동과 충돌은 Core 충돌 월드에서 sweep으로 처리한다 (`tech/architecture.md` §4). 플레이어에는 중력이 없다.
- 물방울 등 낙하체에는 Core가 `world.gravity`를 적용한다. Unity 물리 엔진은 게임 규칙에 쓰지 않는다.
- 카메라는 표현 레이어(Unity)에 속한다. Core는 카메라를 모르고, 입력 커맨드에 담긴 yaw만 이동 방향 계산에 쓴다.
## 카메라 (D-007)
시점은 **3인칭(기본)**과 **1인칭** 두 가지이며, ToggleView 입력(spec/01)으로 언제든 전환한다. 두 시점은 같은 yaw/pitch 값을 공유한다. 시점은 표현만 바꾸고 게임 규칙은 바꾸지 않는다: 이동 방향, 대시 방향, 감지, 판정은 시점과 무관하게 같다.

### 공통
- 마우스/우스틱으로 yaw/pitch를 조작하고, pitch는 `camera.pitchLimit`로 제한한다. 롤은 없다.
- 플레이어 캐릭터의 정면은 카메라 yaw를 따른다 (부착 상태 제외).
- 전환할 때는 `camera.switchTime` 동안 위치와 FOV를 보간한다. 보간 중에도 조작이 가능하다.
- 시점 설정은 저장되며(spec/08 설정 "기본 시점"), 스테이지를 시작하면 저장된 시점으로 시작한다.

### 3인칭 (기본)
- Cinemachine 오빗 카메라.
- 피벗: 플레이어 중심 + `camera.heightOffset`, 거리 `camera.distance`, FOV `camera.fov`.
- 벽에 막히면 `camera.collisionRadius` 구 sweep으로 당겨온다.
- 부착 상태에서는 카메라가 캐릭터와 분리되어 자유 오빗한다.

### 1인칭
- 카메라 위치: 캐릭터 머리 + `camera.fp.eyeOffset`, FOV `camera.fp.fov`, near clip `camera.fp.nearClip`.
- 자기 캐릭터 메시는 Shadows Only로 렌더링해 화면을 가리지 않게 한다. 그림자는 유지한다.
- 날개 진동과 대시 같은 피드백은 화면 가장자리 효과(날개 잔상 오버레이, 대시 시 짧은 FOV 펄스 `camera.fp.dashFovKick`)로 대신 보여준다.
- 부착 상태: 카메라 up을 표면 법선에 맞추고, 시선은 법선 기준 `camera.fp.attachedLookLimit` 반구 안에서만 움직인다. 흡혈 중에는 주둥이 끝 근처의 피부가 보이도록 pitch를 표면 쪽으로 보간한다.
- 플레이어 충돌 구가 카메라를 포함하므로 별도의 카메라 충돌 처리는 없다. 단, 시야가 벽에 파묻히지 않도록 `camera.fp.eyeOffset`은 충돌 구 반지름 안에 둔다.

## 수용 기준
- [ ] 빈 테스트 씬에서 플레이어를 원점 기준 ±500u 범위 어디에 두어도 이동과 충돌이 정상 동작한다 (Core).
- [ ] 3인칭 카메라 near clip이 `camera.nearClip`이고, 벽에 붙어도 카메라가 벽을 관통하지 않는다 (Unity: 벽 앞 이동 후 카메라와 벽 사이 레이캐스트에 걸리는 것이 없음).
- [ ] pitch가 `camera.pitchLimit`를 넘지 않는다 (Unity: 입력 누적 테스트).
- [ ] 낙하체가 1초 동안 `world.gravity`로 가속되어 490.5u(±1%) 떨어진다 (Core).
- [ ] 게임 시작 시 기본 시점은 3인칭이다 (Unity, 저장 데이터 없음).
- [ ] ToggleView 입력으로 3인칭 ↔ 1인칭이 전환되고, `camera.switchTime` 후 위치와 FOV가 목표값에 도달한다 (Unity).
- [ ] 전환 전후의 yaw/pitch가 같다 (Unity).
- [ ] 같은 입력 시퀀스를 두 시점에서 재생하면 플레이어 최종 위치가 같다 (Unity: 시점 독립성).
- [ ] 1인칭에서 플레이어 메시의 렌더러가 Shadows Only이고, 3인칭으로 돌아오면 원래대로 복구된다 (Unity).
- [ ] 1인칭 부착 상태에서 시선이 법선 기준 `camera.fp.attachedLookLimit`를 벗어나지 않는다 (Unity).
- [ ] 1인칭에서 벽에 최대한 붙어도 화면에 벽 뒤가 보이지 않는다 (캡처 검토: 벽 접촉 포즈 1장).
- [ ] 선택한 시점이 재시작 후에도 유지된다 (Unity).

## 범위 외
- 롤 회전, 카메라 흔들림 옵션, 1인칭 전용 뷰모델(팔/주둥이 모델)
