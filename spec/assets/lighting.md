# 조명 스펙 (M14)

> 방 분위기 조명과 빛을 내는 애셋의 요구를 한곳에 모은다. 애셋 인덱스(`INDEX.md`)의 `light-*`는 이 문서를 가리킨다.
> 데이터: 방 파일 `data/rooms/*.json`의 `lights`(표현 전용, 스키마 `room.schema.json`). 규칙이 있는 조명 스위치는 레벨 데이터의 `lights`다(spec/06, 아래 §5).
> 검사: Core `RoomLightingTests`(스테이지에 쓰는 방은 모두 조명이 있음, 방 안 위치, 발광 형상·쿠키 참조, 이 문서에 모든 조명 ID가 있음), EditMode `RoomLightingViewTests`.

## 1. 원칙
1. **밤의 집.** 모든 스테이지는 밤이다. 주 조명(달빛 방향의 약한 키 라이트)과 환경광 위에 집 안의 작은 빛들이 웅덩이를 만든다. 방 전체를 고르게 밝히지 않는다.
2. **빛은 정보다.** 빛 웅덩이와 어둠은 "보이는 곳 / 숨는 곳"을 미리 읽히게 한다(gulf §8). 은신처(Shadow Zone) 안에는 분위기 조명을 직접 비추지 않는다. 은신처 쪽으로 빛을 두면 데이터 검토 때 캡처로 확인한다.
3. **실제 빛처럼 흔들리되 산만하지 않게.** 백열등은 거의 일정하고, TV는 장면마다 툭 바뀌고, 형광등은 드물게 끊기고, 불꽃은 일렁인다(§3). 같은 시각이면 같은 값(재현 가능)이다.
4. **색 온도로 공간을 나눈다.** 달빛·TV·휴대폰 = 차가운 파랑, 스탠드·복도·등불 = 따뜻한 주황, 냉장고 LED = 청록 점. 모키(분홍·하트)는 어느 빛에서도 배경과 구분되어야 한다(spec/10 가독성, 툰 셰이더 자체 밝기).
5. **툰 화풍.** 추가 조명도 셀 단계로 끊어 더한다(툰 셰이더 `ToonAdditionalLights`, 단계 경계는 `_AdditionalBandSoftness`만큼 부드럽게). 1u ≈ 1cm라 물리 감쇠(1/거리²)는 상쇄하고 범위 창 감쇠만 쓴다. 그래서 데이터 `intensity`는 "가까운 면을 주 조명의 몇 배로 밝히나"에 가깝다(0.3~1.2).

## 2. 구조
| 단계 | 위치 | 하는 일 |
|---|---|---|
| 데이터 | `data/rooms/*.json` `lights[]` | id, type(point·spot), position, direction, color, intensity, range, spotAngle, flicker, shadows, glowShape, cookie, attachPart. 방 단위 `ambient` |
| 읽기 | Core `RoomLightDefinition`(Unity 비의존) | 검사: 세기 ≥ 0, 범위 > 0, 스포트는 방향 필요 |
| 표현 | Unity `RoomLightingView` | Unity Light 생성(스포트 안쪽 원뿔 55%, 그림자 진하기 0.75), 매 프레임 `AmbientLightCurves`로 세기·색 변화, 발광 형상 칠하기 |
| 셰이더 | `Moqui/Toon` | 주 조명 + 추가 조명(Forward+ 클러스터 루프, 추가 조명 그림자, 쿠키) |
| 연결 | `StageBootstrap`, `CaptureTool.CaptureStage` | 레벨의 방으로 조명을 만든다(캡처는 t=0) |

조명 ID → 애셋 ID: `tv_screen` → `light-tv-screen` (`_`를 `-`로, 앞에 `light-`).

## 2.1 방 환경광 (`ambient`)
방 파일의 `ambient`(선형 RGB)가 장면 환경광(Flat)을 정한다. 밤 공기의 색으로 방을 구분한다(gulf §13).

| 방 | 환경광 | 느낌 |
|---|---|---|
| `livingKitchen` | (0.28, 0.27, 0.44) | 남보라 (TV·스탠드와 대비) |
| `bedroom` | (0.22, 0.23, 0.40) | 짙은 남색 (가장 어둡다, 휴대폰·협탁 빛이 돋보임) |
| `bathroom` | (0.30, 0.33, 0.40) | 차가운 청회색 (형광등 방) |
| `veranda` | (0.31, 0.26, 0.36) | 도시 불빛이 섞인 자주 |

## 2.2 부위를 따라가는 조명 (`attachPart`)
조명이 첫 인간의 그 부위 캡슐 끝(b, 손목 쪽)에서 4u 위를 따라간다. 휴대폰을 드는 동작이면 화면 빛이 얼굴을 아래에서 비춘다. 모델이 들어오면 손 소켓(`prop-phone`)으로 바꾼다.

## 2.3 모키 자신의 빛
흡혈 중(세션 + Suck 누름) 지팡이 하트가 1.2초 주기로 맥동하며 분홍 점 조명(범위 20u, 세기 0.6)을 낸다(`PlayerView.RefreshWandLight`, 큐 `wand.drink`).

## 3. 흔들림 종류 (`flicker`)
| 값 | 실제 빛 | 곡선 (`AmbientLightCurves`) | 평균 |
|---|---|---|---|
| none | 달빛, LED | 일정 | 1 |
| lamp | 백열등·스탠드 | 느린 펄린 노이즈 ±3% (0.3Hz) | 1 |
| tv | TV 화면 | 장면 길이 1.5~5초마다 밝기(0.55~1.15)·색(차가움↔따뜻함) 컷 전환 + 장면 안 잔물결 ±6% (4Hz) | ≈0.85 |
| fluorescent | 형광등 | 일정, 0.25초 창마다 1.5% 확률로 24Hz 끊김(0.2↔0.9) | ≈1 |
| phone | 휴대폰 화면 | 스크롤 ±12% (1.2Hz), 0.4초 창마다 3% 확률로 화면 전환 어둠(×0.55) | ≈1 |
| ember | 촛불·등불 | 느린 숨(1.7Hz) + 빠른 떨림(9Hz) 두 겹 0.75~1.25, 어두울 때 더 붉게 | ≈1 |
| city | 창밖 도시 | 아주 느린 흐름 ±8% (0.1Hz) + 6초 창마다 35% 확률로 지나가는 차의 하얀 빛(+45%, 약 0.5초) | ≈1 |

발광 형상(`glowShape`)은 같은 빛 색으로 칠하고 툰 셰이더 `_SelfIllumination` = 1로 둔다(TV 화면이 방을 비추는 빛과 함께 바뀐다). 모델이 들어오면 발광 텍스처(`tex-tv-screen` 플립북 등)의 밝기를 같은 곡선으로 조절한다.

## 4. 방별 분위기 조명
위치는 방 바닥 중앙 기준(u). 세기는 §1-5의 상대 세기.

### 거실·주방 (`livingKitchen`, 1장)
| 애셋 ID | 종류 | 위치 | 색 | 세기·범위 | 흔들림 | 그림자 | 연결 애셋 |
|---|---|---|---|---|---|---|---|
| `light-tv-screen` | 스포트 110° | TV 앞 (−150, 85, −84), 방 안쪽(+Z)으로 | 차가운 파랑 (0.55, 0.68, 1.0) | 1.2 · 450 | tv | 있음 | `tv-set`(발광 형상 `tv`), `tex-tv-screen` |
| `light-floor-lamp` | 점 | 스탠드 갓 (−350, 150, 250) | 따뜻한 주황 (1.0, 0.74, 0.45) | 0.9 · 230 | lamp | - | `floor-lamp`(발광 형상 `floor_lamp`), `tex-lampshade` |
| `light-moonlight-window` | 스포트 45° | 서쪽 창, 반쯤 친 커튼 옆 틈 (−396, 200, −115) | 달빛 (0.55, 0.66, 1.0) | 0.6 · 650 | none | 있음 | `room-living-kitchen` 창, 쿠키 `tex-light-cookie-window` |
| `light-under-cabinet` | 점 | 위 찬장 아래 (370, 158, 0) | 백색 조금 따뜻하게 | 0.4 · 140 | lamp | - | `kitchen-counter`(찬장 밑 LED 바) |
| `light-fridge-led` | 점 | 냉장고 문 디스플레이 (324, 150, 290) | 청록 (0.4, 0.8, 1.0) | 0.3 · 45 | none | - | `fridge`(문 디스플레이 발광) |

### 침실 (`bedroom`, 2장)
| 애셋 ID | 종류 | 위치 | 색 | 세기·범위 | 흔들림 | 그림자 | 연결 애셋 |
|---|---|---|---|---|---|---|---|
| `light-phone-screen` | 점 | 오른 팔뚝 끝(손)을 따라감(`attachPart` forearmR, +4u, liftPhone 때 얼굴을 아래에서 비춤) | 차가운 백색 (0.75, 0.85, 1.0) | 0.8 · 90 | phone | - | `prop-phone`, `tex-phone-screen` |
| `light-moonlight-window` | 스포트 45° | 서쪽 창 (−198, 200, 40) | 달빛 | 0.6 · 500 | none | 있음 | `room-bedroom` 창, 쿠키 `tex-light-cookie-window` |
| `light-hallway-leak` | 점 | 열린 문 밖 복도 (120, 180, −215) | 따뜻한 주황 (1.0, 0.8, 0.55) | 0.35 · 200 | lamp | - | `room-bedroom` 문틀(문 밖은 밝은 복도 벽 카드) |
| `light-bedside` | 점 | 협탁 스탠드 (115, 75, 170) | 따뜻한 주황 (1.0, 0.72, 0.42) | 0.6 · 160 | lamp | - | `nightstand`(협탁 위 작은 스탠드, 갓 발광) |

### 화장실 (`bathroom`, 3장)
| 애셋 ID | 종류 | 위치 | 색 | 세기·범위 | 흔들림 | 그림자 | 연결 애셋 |
|---|---|---|---|---|---|---|---|
| `light-bathroom-fluorescent` | 점 | 천장 중앙 (0, 238, 0) | 차가운 백색 (0.85, 0.95, 1.0) | 0.7 · 320 | fluorescent | - (점 조명 그림자는 해상도가 낮아 인간 몸에 톱니 경계, 캡처 검토) | `room-bathroom`(천장 형광등 커버, 발광) |
| `light-mirror` | 점 | 거울 위 등 (95, 200, −30) | 따뜻한 백색 | 0.4 · 120 | fluorescent | - | `sink`(수납장 거울 위 등, 발광 띠) |
| `light-phone-screen` | 점 | 오른 팔뚝 끝(손)을 따라감(`attachPart` forearmR, +4u) | 차가운 백색 | 0.7 · 80 | phone | - | `prop-phone`(scrollPhone 동작 중 손에), `tex-phone-screen` |

### 베란다 (`veranda`, 4장)
| 애셋 ID | 종류 | 위치 | 색 | 세기·범위 | 흔들림 | 그림자 | 연결 애셋 |
|---|---|---|---|---|---|---|---|
| `light-city-glow` | 스포트 120° | 방충망 창 밖 (0, 140, −165), 안쪽으로 | 주황 (1.0, 0.6, 0.35) | 0.6 · 450 | city | - | `room-veranda` 창, `tex-night-city`, `tex-screen-mesh` |
| `light-moonlight-sky` | 스포트 40° | 창 밖 높이 (0, 240, −160), 비스듬히 아래로 | 달빛 | 0.5 · 500 | none | 있음 | `room-veranda`(방충망 그물 그림자가 바닥에 떨어짐) |
| `light-lantern` | 점 | 테이블 위 매단 등불 (0, 120, 10) | 주황 (1.0, 0.7, 0.4) | 0.8 · 200 | ember | - | `veranda-lantern`, `tex-lantern-paper` |

## 5. 기믹 조명 (규칙 있음, spec/06)
레벨 데이터 `lights`의 조명 스위치. 그림은 `GimmickView`가 만든다(전구 구 + 점 조명, 켜지면 같은 툰 추가 조명으로 비춤).

| 애셋 ID | 쓰는 곳 | 요구 |
|---|---|---|
| `light-ceiling` | stage09·12 천장등(`ceiling-light`, 경계하면 켜짐) | 백색, 켜질 때 형광 점등(0.3초 동안 2~3회 깜빡) |
| `light-bedside` | stage10 협탁 스탠드(주기) | 따뜻한 주황, 켜고 꺼질 때 딸깍 |
| `light-lantern` | stage20 캠핑 등불(주기) | 주황, 켜질 때 서서히 밝아짐 |
| `light-pendant` | (예비) 식탁 펜던트(`dining-lamp`) — 아직 이 조명을 쓰는 스테이지 없음 | 따뜻한 백색, 켜질 때 0.1초 두 번 깜빡이고 켜짐 |

같은 ID의 방 분위기 조명이 있으면 기믹 조명이 그 빛을 맡는다(`RoomLightingView`가 분위기 조명을 만들지 않음): 꺼진 스위치가 켜져 보이지 않게 한다 (리뷰 M14).
| (공통) | 켜기 예고 | 켜지기 전 팔이 스위치로 뻗는 동작 + 딸깍 소리(gulf §8), 켜지면 은신처 표시가 빛에 씻겨 옅어짐 |

## 6. 필요한 텍스처·VFX (애셋 인덱스에 등록됨)
| 애셋 ID | 무엇 | 지금 대신 쓰는 것 |
|---|---|---|
| `tex-light-cookie-window` | 창살 쿠키(흑백 256², 2×3 창살, 가장자리 흐림) | `RoomLightingView.CreateWindowCookie` 절차 쿠키(128²) |
| `tex-light-cookie-blinds` | 블라인드 줄무늬 쿠키(선택, 침실 대안) | 없음 |
| `tex-tv-screen` | TV 밤 드라마 플립북(발광), 장면 컷과 `tv` 곡선 장면 경계를 맞춘다 | 발광 형상 단색 |
| `tex-phone-screen` | 휴대폰 화면 4프레임(발광) | 없음(빛만) |
| `tex-lampshade` | 빛이 비치는 갓 천 | 발광 형상 단색 |
| `tex-lantern-paper` | 등불 한지 | 없음(빛만) |
| `tex-night-city` | 창밖 도시 보케(발광) | 없음 |
| `vfx-light-shaft` | 달빛 스포트 원뿔 안의 옅은 빛줄기와 천천히 떠다니는 먼지(입자 20~40, 크기 0.5~1u, 모키가 지나가면 날갯바람에 흩어짐) | 없음 |

## 7. 상태
- **블룸:** 후처리 볼륨의 약한 Bloom(세기 0.35, 임계 0.95, `PostProcessBuilder`)이 발광 면(TV 화면·전구·갓)에 걸린다. 완료.
- **방 환경광·부위를 따라가는 빛·모키 자신의 빛:** §2.1~2.3. 완료 (M14).
- **나중에:** 라이트 프로브·구운 간접광(모델이 들어온 뒤), 휴대폰 빛을 손 소켓으로(인간 모델).

## 8. 방을 추가할 때
1. 방 파일에 `lights`를 적는다(최소 하나의 주 분위기 빛 + 색 온도가 다른 보조 빛).
2. 이 문서 §4에 표를 추가한다(테스트가 모든 조명 ID를 요구한다).
3. 새 쿠키·발광 텍스처·등불 같은 표현 전용 소품은 `Tools/asset_specs.py`에 등록하고 인덱스를 다시 만든다.
4. `Tools/capture.ps1`로 캡처해 은신처가 빛에 씻기지 않는지, 모키가 구분되는지 본다.
