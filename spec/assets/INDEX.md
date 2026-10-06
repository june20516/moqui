# 애셋 인덱스 (M14, D-062)

> 생성: `python Tools/asset_specs.py`. 스테이지를 추가하면 스크립트의 표를 갱신하고 다시 실행한다(`spec/07` 레벨 추가 방법 5번).
> Core 테스트 `AssetIndexTests`가 목록의 모든 방·레벨 형상이 아래 "형상 ID 패턴"에 속하는지 검사한다.

## 모델·판정 볼륨

| 애셋 ID | 종류 | 묶음 | 이름 | 문서 | 형상 ID 패턴 | 방 | 사용 스테이지 |
|---|---|---|---|---|---|---|---|
| moki | model | 캐릭터 | 모키 (플레이어, 마법소녀 모기) | [models/moki.md](models/moki.md) | - | - | (표현 오브젝트) |
| human-adult | model | 캐릭터 | 성인 인간 (타겟이자 적) | [models/human-adult.md](models/human-adult.md) | - | - | (표현 오브젝트) |
| prop-swatter | model | 도구·소품 | 전기 모기채 | [models/prop-swatter.md](models/prop-swatter.md) | - | - | (표현 오브젝트) |
| prop-phone | model | 도구·소품 | 휴대폰 | [models/prop-phone.md](models/prop-phone.md) | - | - | (표현 오브젝트) |
| prop-remote | model | 도구·소품 | TV 리모컨 | [models/prop-remote.md](models/prop-remote.md) | - | - | (표현 오브젝트) |
| prop-beer-can | model | 도구·소품 | 맥주 캔 | [models/prop-beer-can.md](models/prop-beer-can.md) | `beer_can` | - | stage05, stage17, stage18, stage19, stage20 |
| prop-snack-plate | model | 도구·소품 | 안주 접시 | [models/prop-snack-plate.md](models/prop-snack-plate.md) | `snack_plate` | - | stage05, stage17, stage18, stage19, stage20 |
| prop-spray-can | model | 도구·소품 | 모기약 스프레이 캔 | [models/prop-spray-can.md](models/prop-spray-can.md) | `spray_can` | - | stage03, stage09, stage10, stage11, stage12 |
| room-living-kitchen | model | 1장 거실·주방 | 거실·주방 방 껍데기 | [models/room-living-kitchen.md](models/room-living-kitchen.md) | `floor` `ceiling` `wall_*` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| sofa | model | 1장 거실·주방 | 소파 | [models/sofa.md](models/sofa.md) | `sofa_*` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| tv-set | model | 1장 거실·주방 | TV와 TV장 | [models/tv-set.md](models/tv-set.md) | `tv` `tv_stand` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| coffee-table | model | 1장 거실·주방 | 커피 테이블 | [models/coffee-table.md](models/coffee-table.md) | `coffee_table_*` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| bookshelf | model | 1장 거실·주방 | 책장 | [models/bookshelf.md](models/bookshelf.md) | `bookshelf` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| floor-lamp | model | 1장 거실·주방 | 스탠드 조명 | [models/floor-lamp.md](models/floor-lamp.md) | `floor_lamp` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| curtain | model | 1장 거실·주방 | 창 커튼 | [models/curtain.md](models/curtain.md) | `curtain` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| aircon-indoor | model | 1장 거실·주방 | 벽걸이 에어컨 | [models/aircon-indoor.md](models/aircon-indoor.md) | `air_conditioner` `aircon_*` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| hanging-plant | model | 1장 거실·주방 | 걸이 화분 | [models/hanging-plant.md](models/hanging-plant.md) | `hanging_plant` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| kitchen-counter | model | 1장 거실·주방 | 조리대와 위 찬장 | [models/kitchen-counter.md](models/kitchen-counter.md) | `kitchen_counter` `kitchen_upper_cabinet` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| fridge | model | 1장 거실·주방 | 냉장고 | [models/fridge.md](models/fridge.md) | `fridge` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| kitchen-island | model | 1장 거실·주방 | 아일랜드 식탁 | [models/kitchen-island.md](models/kitchen-island.md) | `kitchen_island` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| dining-set | model | 1장 거실·주방 | 식탁 세트 | [models/dining-set.md](models/dining-set.md) | `dining_table_*` `dining_chair_*` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| dining-lamp | model | 1장 거실·주방 | 식탁 펜던트 조명 | [models/dining-lamp.md](models/dining-lamp.md) | `dining_lamp` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| shoe-cabinet | model | 1장 거실·주방 | 신발장 | [models/shoe-cabinet.md](models/shoe-cabinet.md) | `shoe_cabinet` | livingKitchen | stage01, stage02, stage06, stage07, stage08 |
| room-bedroom | model | 2장 침실 | 침실 방 껍데기 | [models/room-bedroom.md](models/room-bedroom.md) | `floor` `ceiling` `wall_*` `door_frame` | bedroom | stage03, stage09, stage10, stage11, stage12 |
| bed | model | 2장 침실 | 침대 | [models/bed.md](models/bed.md) | `bed_*` | bedroom | stage03, stage09, stage10, stage11, stage12 |
| nightstand | model | 2장 침실 | 협탁 | [models/nightstand.md](models/nightstand.md) | `nightstand` | bedroom | stage03, stage09, stage10, stage11, stage12 |
| wardrobe | model | 2장 침실 | 옷장 | [models/wardrobe.md](models/wardrobe.md) | `wardrobe` | bedroom | stage03, stage09, stage10, stage11, stage12 |
| ceiling-light | model | 2장 침실 | 천장 등 | [models/ceiling-light.md](models/ceiling-light.md) | `ceiling_light` | bedroom | stage03, stage09, stage10, stage11, stage12 |
| fan | model | 기믹 | 선풍기 | [models/fan.md](models/fan.md) | `fan` `fan_*` | - | stage03, stage05, stage06, stage12, stage17, stage18, stage19, stage20 |
| mosquito-net | model | 기믹 | 모기장 | [models/mosquito-net.md](models/mosquito-net.md) | `net_*` `netgap_*` | - | stage12 |
| room-bathroom | model | 3장 물가 | 화장실 방 껍데기 | [models/room-bathroom.md](models/room-bathroom.md) | `floor` `ceiling` `wall_*` `vent` | bathroom | stage04, stage13, stage14, stage15, stage16 |
| toilet | model | 3장 물가 | 변기 | [models/toilet.md](models/toilet.md) | `toilet_*` | bathroom | stage04, stage13, stage14, stage15, stage16 |
| sink | model | 3장 물가 | 세면대와 수납장 거울 | [models/sink.md](models/sink.md) | `sink_*` `mirror_cabinet` | bathroom | stage04, stage13, stage14, stage15, stage16 |
| shower-booth | model | 3장 물가 | 샤워 부스 | [models/shower-booth.md](models/shower-booth.md) | `shower_*` | bathroom | stage04, stage13, stage14, stage15, stage16 |
| towel-rack | model | 3장 물가 | 수건과 수건걸이 | [models/towel-rack.md](models/towel-rack.md) | `towel` `towel_bar` | bathroom | stage04, stage13, stage14, stage15, stage16 |
| room-veranda | model | 4장 베란다 | 베란다 방 껍데기 | [models/room-veranda.md](models/room-veranda.md) | `floor` `ceiling` `wall_*` `screen_window` | veranda | stage05, stage17, stage18, stage19, stage20 |
| veranda-table | model | 4장 베란다 | 접이식 테이블 | [models/veranda-table.md](models/veranda-table.md) | `table_*` | veranda | stage05, stage17, stage18, stage19, stage20 |
| icebox | model | 4장 베란다 | 아이스박스 | [models/icebox.md](models/icebox.md) | `icebox` | veranda | stage05, stage17, stage18, stage19, stage20 |
| plant-shelf | model | 4장 베란다 | 화분 선반 | [models/plant-shelf.md](models/plant-shelf.md) | `plant_shelf` | veranda | stage05, stage17, stage18, stage19, stage20 |
| drying-rack | model | 4장 베란다 | 빨래 건조대 | [models/drying-rack.md](models/drying-rack.md) | `drying_rack` | veranda | stage05, stage17, stage18, stage19, stage20 |
| ac-outdoor | model | 4장 베란다 | 에어컨 실외기 | [models/ac-outdoor.md](models/ac-outdoor.md) | `ac_unit` | veranda | stage05, stage17, stage18, stage19, stage20 |
| veranda-lantern | model | 4장 베란다 | 캠핑 등불 | [models/veranda-lantern.md](models/veranda-lantern.md) | - | veranda | (표현 오브젝트) |
| spray-dispenser | model | 기믹 | 자동 모기약 분사기 | [models/spray-dispenser.md](models/spray-dispenser.md) | `spray_dispenser` `dispenser` | - | stage05, stage14, stage16, stage17, stage18, stage19, stage20 |
| mosquito-coil | model | 기믹 | 모기향 | [models/mosquito-coil.md](models/mosquito-coil.md) | `coil` | - | stage05, stage10, stage17, stage18, stage19, stage20 |
| spider-web | model | 기믹 | 거미줄 | [models/spider-web.md](models/spider-web.md) | `web_*` | - | stage05, stage17, stage18, stage19, stage20 |
| water-drop | model | 기믹 | 물방울 | [models/water-drop.md](models/water-drop.md) | - | - | (표현 오브젝트) |
| volume-shadow-zone | volume | 판정 볼륨 (모델 없음) | 은신처 | [models/volume-shadow-zone.md](models/volume-shadow-zone.md) | `shadow_*` | - | stage01, stage02, stage03, stage04, stage05, stage06, stage07, stage08, stage09, stage10, stage11, stage12, stage13, stage14, stage15, stage16, stage17, stage18, stage19, stage20 |
| volume-humid | volume | 판정 볼륨 (모델 없음) | 습기·증기 영역 | [models/volume-humid.md](models/volume-humid.md) | `steam_*` `humid_*` | - | stage04, stage13, stage14, stage15, stage16 |

## 텍스처

| 텍스처 ID | 이름 | 사양 |
|---|---|---|
| tex-moki-atlas | 모키 색 아틀라스 | 512², 단색 영역 위주 + 줄무늬 스타킹 무늬, 툰 셰이더 베이스 컬러 |
| tex-moki-wing | 모키 날개맥 | 256², 알파 마스크(날개맥 선 + 가장자리 반짝), 양면 |
| tex-human-skin | 인간 피부 | 1024², 피부 톤 2~3종 변형, 체온 표시와 어울리게 따뜻하게 |
| tex-human-outfits | 인간 의상 | 1024², 의상 4종 + 동행자, 무늬는 크게(모기 시점에서 가까이 보임) |
| tex-floor-wood | 나무 마루 | 1024² 타일링, 나뭇결 + 이음새 |
| tex-floor-tile | 주방 타일 | 512² 타일링 |
| tex-wallpaper-lavender | 라벤더 벽지 | 512² 타일링, 은은한 패턴 |
| tex-wallpaper-cream | 크림 벽지 | 512² 타일링 |
| tex-fabric-sofa | 소파 천 | 512² 타일링, 직물 결 |
| tex-fabric-curtain | 커튼 천 | 512² 타일링 |
| tex-fabric-bedding | 침구 | 512² 타일링, 무늬 |
| tex-fabric-towel | 수건 | 256² 타일링, 파일 결 |
| tex-fabric-laundry | 빨래 | 512², 옷 무늬 몇 종 |
| tex-tv-screen | TV 화면 | 512², 8~16프레임 밤 드라마 장면 플립북(발광), 조명 `light-tv-screen` 색과 맞춤 |
| tex-phone-screen | 휴대폰 화면 | 256², 밝은 SNS 화면 4프레임(발광) |
| tex-wood-oak | 원목 | 512² 타일링 |
| tex-wood-white | 흰 도장 목재 | 256² 타일링 |
| tex-books | 책 등 | 512², 책등 묶음 |
| tex-lampshade | 스탠드 갓 | 256², 빛이 비치는 천(투과 마스크) |
| tex-leaves | 잎 | 512² 알파 카드 |
| tex-countertop | 조리대 상판 | 512² 타일링, 인조 대리석 |
| tex-fridge-metal | 냉장고 금속 | 256², 헤어라인 금속 |
| tex-tile-small | 작은 욕실 타일 | 512² 타일링 |
| tex-tile-large | 큰 베란다 타일 | 512² 타일링 |
| tex-ceramic | 도자기 | 256², 매끈한 흰색 + 하이라이트 |
| tex-mirror | 거울 | 반사 프로브 또는 단순 밝은 그라데이션 |
| tex-glass-frosted | 반투명 유리 | 256², 아래쪽 불투명 띠 알파 |
| tex-screen-mesh | 방충망 | 256² 알파 격자 |
| tex-night-city | 창밖 밤 도시 | 1024², 흐린 불빛 보케(발광) |
| tex-net-mesh | 모기장 그물 | 256² 알파 격자, 고운 흰 실 |
| tex-web | 거미줄 | 512² 알파, 방사형 실 + 이슬 |
| tex-coil | 모기향 | 256², 녹색 나선 + 탄 끝 |
| tex-swatter-mesh | 모기채 그물 | 256² 알파 격자 |
| tex-beer-label | 맥주 캔 라벨 | 256² |
| tex-spray-label | 스프레이 라벨 | 256², 모기 경고 그림 |
| tex-light-cookie-window | 창틀 빛 쿠키 | 256² 흑백, 창살 모양(달빛 스팟 조명 쿠키) |
| tex-light-cookie-blinds | 블라인드 빛 쿠키 | 256² 흑백 줄무늬(선택) |
| tex-lantern-paper | 등불 한지 | 256², 빛이 비치는 한지 결(투과 마스크) |

## VFX·조명

| ID | 이름 | 문서 |
|---|---|---|
| vfx-moki-cues | 모키 표현 큐 | [vfx/moki-cues.md](vfx/moki-cues.md) |
| vfx-heat-shimmer | 체온 일렁임 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-bite-mark | 물린 자국 점 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-attack-telegraph | 공격 예고 고리와 손 접근 줄기 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-shadow-cue | 은신처 표시 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-steam | 증기 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-co2 | CO₂ 날숨 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-spray-cloud | 모기약 연무 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-coil-smoke | 모기향 연기 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-wind-dust | 바람 먼지 줄기 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-water-drop | 물방울 맺힘·낙하·튀김 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-swatter-zap | 전기 모기채 스파크 | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| vfx-light-shaft | 달빛 빛줄기와 떠다니는 먼지 (lighting.md) | `spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체 |
| light-* | 분위기 조명 | [lighting.md](lighting.md) |
