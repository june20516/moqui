# 은신처 (`volume-shadow-zone`)

> 생성: `python Tools/asset_specs.py` (D-062). 내용을 고칠 때는 스크립트의 ASSETS 표를 고친다.

## 사용처
| 스테이지 | 장 | 형상 ID | 판정 형상 |
|---|---|---|---|
| stage01 거실 · TV 보다 조는 인간 | 1장 · 거실 | `shadow_coffee_table` | 상자 88 × 38 × 38u |
| stage01 거실 · TV 보다 조는 인간 | 1장 · 거실 | `shadow_bookshelf` | 상자 10 × 180 × 100u |
| stage01 거실 · TV 보다 조는 인간 | 1장 · 거실 | `shadow_curtain` | 상자 5 × 230 × 120u |
| stage01 거실 · TV 보다 조는 인간 | 1장 · 거실 | `shadow_under_cabinet` | 상자 30 × 70 × 380u |
| stage01 거실 · TV 보다 조는 인간 | 1장 · 거실 | `shadow_dining_table` | 상자 108 × 70 × 68u |
| stage02 거실 · TV 보는 인간 | 1장 · 거실 | `shadow_coffee_table` | 상자 88 × 38 × 38u |
| stage02 거실 · TV 보는 인간 | 1장 · 거실 | `shadow_bookshelf` | 상자 10 × 180 × 100u |
| stage02 거실 · TV 보는 인간 | 1장 · 거실 | `shadow_curtain` | 상자 5 × 230 × 120u |
| stage02 거실 · TV 보는 인간 | 1장 · 거실 | `shadow_under_cabinet` | 상자 30 × 70 × 380u |
| stage02 거실 · TV 보는 인간 | 1장 · 거실 | `shadow_dining_table` | 상자 108 × 70 × 68u |
| stage03 열대야 침실 · 누워 휴대폰 보는 인간 | 2장 · 침실 | `shadow_under_bed` | 상자 140 × 28 × 170u |
| stage03 열대야 침실 · 누워 휴대폰 보는 인간 | 2장 · 침실 | `shadow_wardrobe` | 상자 8 × 196 × 96u |
| stage04 화장실 · 볼일 보는 인간 | 3장 · 물가 | `shadow_behind_toilet` | 상자 28 × 96 × 14u |
| stage04 화장실 · 볼일 보는 인간 | 3장 · 물가 | `shadow_under_sink` | 상자 36 × 72 × 52u |
| stage04 화장실 · 볼일 보는 인간 | 3장 · 물가 | `shadow_behind_towel` | 상자 46 × 56 × 2u |
| stage05 베란다 술자리 · 맥주 마시는 인간 | 4장 · 베란다 | `shadow_behind_icebox` | 상자 56 × 46 × 5u |
| stage05 베란다 술자리 · 맥주 마시는 인간 | 4장 · 베란다 | `shadow_behind_plants` | 상자 4 × 116 × 26u |
| stage05 베란다 술자리 · 맥주 마시는 인간 | 4장 · 베란다 | `shadow_behind_ac` | 상자 8 × 76 × 46u |

## 형상 ID 패턴
`shadow_*`

## 형태
판정 볼륨. 시각은 `vfx-shadow-cue`(푸른 은신처 표시)와 실제 그늘(조명)로 보인다.

## 움직임·상태
-

## 텍스처
-

## 연결된 이펙트·조명
`vfx-shadow-cue`

## 공통 규칙
- 스타일: 툰 셰이딩(`Moqui/Toon`, 셀 3단 + 남보라 그림자 + 외곽선). 밤 실내 팔레트(라벤더 벽, 나무 바닥, 남색 환경광, 따뜻한 조명). 현실 비율의 생활 가구를 약간 둥글고 부드럽게 단순화한다(모서리 반경 1~3u). 플레이어가 1u 크기 모기 시점이므로 가까이서 보이는 면(윗면·모서리·손잡이)에 디테일을 몰아준다.
- 판정 형상은 데이터 그대로 둔다. 모델은 판정 형상을 크게 벗어나지 않게(±10%) 만들어 보이는 것과 부딪히는 것이 어긋나지 않게 한다.
- 피벗: 판정 형상 중심(상자·구) 또는 캡슐 끝점 A. 단위 1u = 1cm, Y-up, +Z 정면.
- 폴리곤·머티리얼 예산: -
- 교체 접점: `LevelView`가 형상 ID와 같은 이름의 시각 오브젝트를 만든다. 모델 프리팹을 같은 ID에 매핑해 교체한다.
- 라이선스: `tech/asset-pipeline.md` 허용 목록, `Assets/_Project/CREDITS.md`에 기록.
