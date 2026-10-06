"""3D 애셋·텍스처·VFX 스펙 문서 생성기 (spec/assets, D-062).

스테이지를 추가할 때마다 이 스크립트의 ASSETS 표를 갱신하고 다시 실행한다:
    python Tools/asset_specs.py
판정 형상의 크기와 사용처(방·레벨·스테이지)는 data/에서 계산하므로 손으로 고치지 않는다.
Core 테스트(AssetIndexTests)가 모든 방·레벨 형상이 INDEX.md의 애셋에 속하는지 검사한다.
"""
import fnmatch
import glob
import json
import os

ROOT = os.path.join(os.path.dirname(__file__), '..')
DATA = os.path.join(ROOT, 'data')
OUT = os.path.join(ROOT, 'spec', 'assets')

STYLE = ('툰 셰이딩(`Moqui/Toon`, 셀 3단 + 남보라 그림자 + 외곽선). 밤 실내 팔레트(라벤더 벽, 나무 바닥, 남색 환경광, 따뜻한 조명). '
         '현실 비율의 생활 가구를 약간 둥글고 부드럽게 단순화한다(모서리 반경 1~3u). 플레이어가 1u 크기 모기 시점이므로 가까이서 보이는 면(윗면·모서리·손잡이)에 디테일을 몰아준다.')

# 애셋 표: id, kind(model|texture|vfx|volume), group, name, patterns(형상 ID 패턴), 그 밖의 서술.
ASSETS = [
    # ---------------- 캐릭터
    dict(id='moki', kind='model', group='캐릭터', name='모키 (플레이어, 마법소녀 모기)', patterns=[], hook='`MokiBuilder`(지금은 프리미티브) → Player 아래 `MokiRig`. Animator 파라미터 `State`(정수, `MokiPose` 7종)와 큐 표시기(`IMokiCuePresenter`)를 그대로 쓴다.',
         size='키 1.0u(`player.visualHeight`), 충돌 구 반지름 0.4u. 2.5등신 데포르메.',
         form='핑크 머리 소녀, 큰 리본 두 개, 짧은 드레스와 스커트, 등에 반투명 요정 날개 2장, 오른손에 하트 보석이 달린 지팡이(= 모기 주둥이, 흡혈 도구). 가슴에 하트 브로치(포만 표시). 모기임을 암시하는 디테일: 머리 위 더듬이 모양 아호게 2가닥, 날개맥 무늬, 줄무늬 스타킹.',
         parts='몸통, 머리, 머리카락(아호게 2가닥은 따로 흔들림), 리본 L/R, 드레스, 스커트(천 본 2단), 팔 L/R, 손(지팡이 쥠), 다리 L/R(발끝), 날개 L/R(각 2장: 앞·뒤), 지팡이(손잡이 + 하트 보석), 하트 브로치.',
         rig='휴머노이드 최소 본 + 날개 본 4개 + 스커트 본 2단 + 아호게 본 2개 + 지팡이 소켓(오른손). 하트 보석과 브로치는 발광 강도 머티리얼 파라미터를 가진다.',
         anim='`MokiPose` 7종(Idle 호버, Move 앞으로 기울어 날기, Dash 몸을 말고 돌진, Attach 발끝 착지 대기, Suck 지팡이를 꽂고 맥동, Trapped 물방울 안에서 허우적, Death 별이 되어 흩어짐) + 큐 동작(`spec/12-moki-cues.md` 전부, `spec/assets/vfx/moki-cues.md`). 날갯짓은 속도에 따라 30~60Hz 흐림(블러 카드로 대체 가능).',
         textures='`tex-moki-atlas`(피부·드레스·머리카락 단색 영역 + 줄무늬 스타킹), `tex-moki-wing`(날개맥 알파 마스크).',
         effects='`vfx-moki-cues`, 날갯짓 잔상, 대시 별가루 꼬리, 흡혈 하트 맥동, 포만 브로치 빛.',
         budget='3,000~5,000 삼각형, 머티리얼 3개 이하(불투명 툰, 날개 반투명, 발광 보석).'),
    dict(id='human-adult', kind='model', group='캐릭터', name='성인 인간 (타겟이자 적)', patterns=[], hook='`HumanView`가 Core 몸 캡슐(부위 ID: head, neck, torso, upperArmL/R, forearmL/R, thighL/R, calfL/R, footL/R)을 따른다. 모델은 같은 부위 이름의 본에 피부를 입히고, 본 위치·방향을 매 프레임 Core 캡슐 끝점에 맞춘다(판정 형상 그대로).',
         size='키 약 170cm(=170u). 부위 길이는 레벨 데이터 캡슐과 같다(위팔 약 29u, 아래팔 약 29u, 손바닥까지 +9u).',
         form='모기 시점에서 거대한 생활인. 자세: 소파에 앉음(1장), 누움(2장), 변기에 앉음(3장), 의자에 기댐·서서 걷기(4장). 맨살(팔뚝·종아리·발등·목·볼)과 옷의 경계가 분명해야 한다(흡혈 가능/불가가 형태로 읽힘). 얼굴은 눈·코·입이 큰 단순 형태로, 시선 방향과 표정(평온/의심/광분)이 멀리서도 읽혀야 한다.',
         parts='머리(얼굴: 눈꺼풀 깜빡임·눈동자 방향·입), 귀 L/R(소리 반응 움찔), 목, 몸통(호흡 팽창), 위팔·아래팔·손 L/R(손가락 쥠/펴짐 2상태), 허벅지·종아리·발 L/R.',
         rig='Core 부위 이름과 같은 본. 추가 본: 눈꺼풀, 눈동자 L/R, 귀 L/R, 손가락 묶음 L/R, 가슴(호흡). 의상은 피부 메시 위 별도 메시.',
         anim='Core가 자세(팔·다리·상체·걷기)를 정하므로 본 애니메이션은 얼굴·손가락·귀·호흡 같은 부가 동작만: 깜빡임(3~6초 무작위), 호흡(`BreathPhase`), 의심 시 눈 가늘게, 광분 시 눈 부릅·이 악물기, 졸음 시 눈 감김·고개 끄덕, 시선 이벤트 때 눈동자가 문 자리로, 귀 움찔(`heard`).',
         variants='의상 4종: 1장 반팔·반바지 실내복, 2장 잠옷(소매 짧음), 3장 상의 + 내린 바지(종아리 노출), 4장 반팔·반바지 + 슬리퍼(취함: 볼 붉음). 4장 동행자 1명 추가(체형·의상 다름).',
         textures='`tex-human-skin`(피부 톤 2~3종, 모기 체온 표시와 어울리는 따뜻한 색), `tex-human-outfits`(의상 4종 + 동행자).',
         effects='`vfx-heat-shimmer`(체온 일렁임), `vfx-bite-mark`, `vfx-attack-telegraph`, 땀(광분), 술기운 볼 홍조(취함).',
         budget='8,000~15,000 삼각형(얼굴·손 위주), 머티리얼 3개(피부, 의상, 눈).'),
    # ---------------- 도구·소품
    dict(id='prop-swatter', kind='model', group='도구·소품', name='전기 모기채', patterns=[], hook='`HumanView.BuildSwatter`(지금은 손잡이 캡슐 + 원판). 채 머리 중심 = Core `Human.Palm(ToolArm)`(판정 중심), 손잡이는 손목에서 시작.',
         size='전체 약 50u: 손잡이 15u, 머리 지름 약 26u(판정 반경 `attack.swatter.radius` 22u보다 약간 큼).',
         form='노란 플라스틱 테두리와 은색 금속 그물 3겹, 손잡이에 빨간 버튼과 작은 LED.',
         anim='버튼을 누르면 그물에 파란 전기가 흐름(광분 중 상시), 휘두를 때 잔상.', textures='`tex-swatter-mesh`(그물 알파)', effects='`vfx-swatter-zap`(파란 전기, 맞으면 섬광 + 탁 소리)', budget='500~1,000 삼각형'),
    dict(id='prop-phone', kind='model', group='도구·소품', name='휴대폰', patterns=[], hook='인간 손 소켓(표현 전용). 2장·3장 무작위 동작(liftPhone, scrollPhone)에서 손에 든다.', size='15 × 7 × 1u', form='화면이 켜진 스마트폰. 화면 빛이 얼굴을 비춘다(조명 `light-phone-screen`).', anim='화면 스크롤 깜빡임', textures='`tex-phone-screen`(밝은 화면, 발광)', effects='-', budget='200 삼각형'),
    dict(id='prop-remote', kind='model', group='도구·소품', name='TV 리모컨', patterns=[], hook='인간 손 소켓(1장 grabRemote).', size='18 × 5 × 2u', form='검은 리모컨, 위쪽 빨간 전원 버튼', anim='-', textures='-', effects='-', budget='150 삼각형'),
    dict(id='prop-beer-can', kind='model', group='도구·소품', name='맥주 캔', patterns=['beer_can'], hook='레벨 형상(베란다 테이블 위) + 4장 drinkBeer 동작 때 손 소켓.', size='판정 형상 참조', form='찌그러지지 않은 알루미늄 캔, 이슬 맺힘', anim='-', textures='`tex-beer-label`', effects='캔 표면 물방울(습기 단서)', budget='300 삼각형'),
    dict(id='prop-snack-plate', kind='model', group='도구·소품', name='안주 접시', patterns=['snack_plate'], hook='레벨 형상', size='판정 형상 참조', form='흰 접시 위 과자·땅콩', anim='-', textures='-', effects='-', budget='500 삼각형'),
    dict(id='prop-spray-can', kind='model', group='도구·소품', name='모기약 스프레이 캔', patterns=['spray_can'], hook='레벨 형상(침실 협탁 위) + 광분 분사 때 손 소켓.', size='판정 형상 참조', form='초록 캔, 모기 경고 그림(모키 실루엣 패러디)', anim='분사 때 노즐 눌림', textures='`tex-spray-label`', effects='`vfx-spray-cloud`', budget='400 삼각형'),
    # ---------------- 거실·주방 (1장)
    dict(id='room-living-kitchen', kind='model', group='1장 거실·주방', name='거실·주방 방 껍데기', patterns=['floor', 'ceiling', 'wall_*'], rooms=['livingKitchen'], hook='`LevelView`가 형상 ID로 시각 오브젝트를 만든다. 모델은 같은 ID 이름의 오브젝트에 붙이고 판정 형상(상자)은 그대로 둔다.',
         form='바닥: 나무 마루(주방 쪽은 타일). 벽: 라벤더 벽지 + 걸레받이, 남쪽 벽 현관 쪽 문, 서쪽 벽 창(커튼 뒤, 달빛). 천장: 몰딩 + 거실 천장등(꺼짐).', anim='-', textures='`tex-floor-wood`, `tex-floor-tile`, `tex-wallpaper-lavender`', effects='창으로 드는 달빛(`light-moonlight-window`)', budget='2,000 삼각형'),
    dict(id='sofa', kind='model', group='1장 거실·주방', name='소파', patterns=['sofa_*'], rooms=['livingKitchen'], form='3인용 패브릭 소파, 쿠션 3개, 팔걸이. 앉은 자리에 눌린 주름.', anim='-', textures='`tex-fabric-sofa`', effects='-', budget='1,500 삼각형'),
    dict(id='tv-set', kind='model', group='1장 거실·주방', name='TV와 TV장', patterns=['tv', 'tv_stand'], rooms=['livingKitchen'], form='얇은 평면 TV(화면 발광) + 낮은 원목 TV장(서랍 2개, 셋톱박스).', anim='화면 영상 깜빡임(조명 `light-tv-screen`과 동기)', textures='`tex-tv-screen`(밤 드라마 화면, 발광, 프레임 넘김), `tex-wood-oak`', effects='화면 빛이 방을 일렁이게 비춤', budget='800 삼각형'),
    dict(id='coffee-table', kind='model', group='1장 거실·주방', name='커피 테이블', patterns=['coffee_table_*'], rooms=['livingKitchen'], form='원목 상판 + 네 다리, 아래는 은신처(어두운 그늘). 상판 위 잡지·컵.', anim='-', textures='`tex-wood-oak`', effects='-', budget='600 삼각형'),
    dict(id='bookshelf', kind='model', group='1장 거실·주방', name='책장', patterns=['bookshelf'], rooms=['livingKitchen'], form='5단 책장, 책·상자·액자. 뒤쪽 틈은 은신처. 거실과 주방 사이 칸막이 역할.', anim='-', textures='`tex-books`', effects='-', budget='1,500 삼각형'),
    dict(id='floor-lamp', kind='model', group='1장 거실·주방', name='스탠드 조명', patterns=['floor_lamp'], rooms=['livingKitchen'], form='원통 갓 플로어 스탠드, 따뜻한 빛(조명 `light-floor-lamp`).', anim='-', textures='`tex-lampshade`(빛이 비치는 천)', effects='갓 아래 원뿔 빛', budget='400 삼각형'),
    dict(id='curtain', kind='model', group='1장 거실·주방', name='창 커튼', patterns=['curtain'], rooms=['livingKitchen'], form='바닥까지 내려온 두꺼운 커튼, 주름. 뒤쪽 틈은 은신처. 틈새로 달빛.', anim='에어컨·선풍기 바람에 미세하게 흔들림(표현)', textures='`tex-fabric-curtain`', effects='-', budget='1,000 삼각형'),
    dict(id='aircon-indoor', kind='model', group='1장 거실·주방', name='벽걸이 에어컨', patterns=['air_conditioner'], rooms=['livingKitchen'], form='흰 벽걸이 에어컨, 아래 송풍 날개(에어컨 타이머 기믹일 때 열림), 작은 초록 LED.', anim='송풍 날개 열림/닫힘(켜지기 1초 전 예고), LED 깜빡', textures='-', effects='`vfx-wind-dust`(켜진 동안)', budget='500 삼각형'),
    dict(id='hanging-plant', kind='model', group='1장 거실·주방', name='걸이 화분', patterns=['hanging_plant'], rooms=['livingKitchen'], form='천장에 매단 둥근 화분, 늘어진 덩굴', anim='바람에 흔들림(표현)', textures='`tex-leaves`', effects='-', budget='800 삼각형'),
    dict(id='kitchen-counter', kind='model', group='1장 거실·주방', name='조리대와 위 찬장', patterns=['kitchen_counter', 'kitchen_upper_cabinet'], rooms=['livingKitchen'], form='동쪽 벽 ㄱ자 조리대(싱크·가스레인지), 위 찬장(문 4개). 조리대와 찬장 사이는 은신처(어두운 틈).', anim='-', textures='`tex-countertop`, `tex-wood-white`', effects='찬장 아래 작은 주방등(꺼짐/켜짐 가능)', budget='1,500 삼각형'),
    dict(id='fridge', kind='model', group='1장 거실·주방', name='냉장고', patterns=['fridge'], rooms=['livingKitchen'], form='양문형 냉장고, 문에 자석 메모. 낮게 웅웅거림.', anim='(선택) 인간이 문을 열면 안쪽 빛', textures='`tex-fridge-metal`', effects='문틈 빛(열릴 때)', budget='600 삼각형'),
    dict(id='kitchen-island', kind='model', group='1장 거실·주방', name='아일랜드 식탁', patterns=['kitchen_island'], rooms=['livingKitchen'], form='상판이 튀어나온 아일랜드, 위에 과일 바구니', anim='-', textures='`tex-countertop`', effects='-', budget='600 삼각형'),
    dict(id='dining-set', kind='model', group='1장 거실·주방', name='식탁 세트', patterns=['dining_table_*', 'dining_chair_*'], rooms=['livingKitchen'], form='4인 원목 식탁과 의자 2개, 식탁보 없음. 아래는 은신처.', anim='-', textures='`tex-wood-oak`', effects='-', budget='1,200 삼각형'),
    dict(id='dining-lamp', kind='model', group='1장 거실·주방', name='식탁 펜던트 조명', patterns=['dining_lamp'], rooms=['livingKitchen'], form='천장에 매단 둥근 유리 펜던트(조명 스위치 기믹의 전구로도 씀)', anim='켜짐/꺼짐, 켜질 때 짧은 깜빡임', textures='-', effects='`light-pendant`', budget='300 삼각형'),
    dict(id='shoe-cabinet', kind='model', group='1장 거실·주방', name='신발장', patterns=['shoe_cabinet'], rooms=['livingKitchen'], form='현관 쪽 낮은 신발장, 위에 열쇠 그릇', anim='-', textures='`tex-wood-white`', effects='-', budget='300 삼각형'),
    # ---------------- 침실 (2장)
    dict(id='room-bedroom', kind='model', group='2장 침실', name='침실 방 껍데기', patterns=['floor', 'ceiling', 'wall_*', 'door_frame'], rooms=['bedroom'], form='마루 바닥, 연한 벽지, 문틀(열린 문), 창(달빛).', anim='-', textures='`tex-floor-wood`, `tex-wallpaper-cream`', effects='`light-moonlight-window`', budget='1,500 삼각형'),
    dict(id='bed', kind='model', group='2장 침실', name='침대', patterns=['bed_*'], rooms=['bedroom'], form='더블 침대, 구겨진 이불, 베개 2개, 헤드보드. 아래는 은신처.', anim='이불이 인간 동작(kickBlanket)에 따라 들썩임(표현)', textures='`tex-fabric-bedding`', effects='-', budget='2,000 삼각형'),
    dict(id='nightstand', kind='model', group='2장 침실', name='협탁', patterns=['nightstand'], rooms=['bedroom'], form='서랍 협탁, 위에 스탠드·안경·스프레이 캔', anim='-', textures='`tex-wood-oak`', effects='협탁 스탠드 빛(`light-bedside`)', budget='400 삼각형'),
    dict(id='wardrobe', kind='model', group='2장 침실', name='옷장', patterns=['wardrobe'], rooms=['bedroom'], form='2문 옷장, 뒤 틈은 은신처', anim='-', textures='`tex-wood-white`', effects='-', budget='400 삼각형'),
    dict(id='ceiling-light', kind='model', group='2장 침실', name='천장 등', patterns=['ceiling_light'], rooms=['bedroom'], form='납작한 원형 천장등(조명 스위치 기믹의 전구)', anim='켜짐/꺼짐', textures='-', effects='`light-ceiling`', budget='200 삼각형'),
    dict(id='fan', kind='model', group='기믹', name='선풍기', patterns=['fan'], form='스탠드형 선풍기, 원형 망 안 날개 3장, 머리가 좌우로 회전. 본체는 장애물.', hook='레벨 데이터 fans[]의 본체 상자 + `GimmickView`의 머리(지금은 원판). 머리 피벗 = 바람이 나오는 점, 정면 = 바람 방향.', anim='날개 회전(블러 원판), 머리 회전(Core 방향과 동기)', textures='-', effects='`vfx-wind-dust`', budget='1,200 삼각형'),
    dict(id='mosquito-net', kind='model', group='기믹', name='모기장', patterns=['net_*', 'netgap_*'], form='침대를 덮는 사각 모기장, 고운 흰 그물. 틈은 그물이 찢어진 자리(실밥이 나풀거림).', hook='레벨 데이터 nets[](그물 판, 유리처럼 충돌·시야 통과)와 netGaps[](틈 판정 볼륨, 그리지 않음).', anim='공기 흐름·인간 동작에 출렁임, 빠르게 다가가면 미리 출렁(예고)', textures='`tex-net-mesh`(알파 격자)', effects='-', budget='판 1장당 2 삼각형 + 틈 테두리 실밥'),
    # ---------------- 화장실 (3장)
    dict(id='room-bathroom', kind='model', group='3장 물가', name='화장실 방 껍데기', patterns=['floor', 'ceiling', 'wall_*', 'vent'], rooms=['bathroom'], form='작은 타일 바닥과 벽, 환풍기(돌아감). 형광등 하나(차가운 빛).', anim='환풍기 날개 회전', textures='`tex-tile-small`', effects='`light-bathroom-fluorescent`', budget='1,000 삼각형'),
    dict(id='toilet', kind='model', group='3장 물가', name='변기', patterns=['toilet_*'], rooms=['bathroom'], form='도자기 변기와 물탱크, 뒤 틈은 은신처', anim='-', textures='`tex-ceramic`', effects='-', budget='1,500 삼각형'),
    dict(id='sink', kind='model', group='3장 물가', name='세면대와 수납장 거울', patterns=['sink_*', 'mirror_cabinet'], rooms=['bathroom'], form='받침대 세면대, 수도꼭지(물방울 발생원), 거울 수납장. 아래는 은신처. 거울은 방을 비춘다(선택).', anim='수도꼭지에서 물방울이 맺혀 떨어짐(Core 물방울과 동기)', textures='`tex-ceramic`, `tex-mirror`', effects='`vfx-water-drop`', budget='1,200 삼각형'),
    dict(id='shower-booth', kind='model', group='3장 물가', name='샤워 부스', patterns=['shower_*'], rooms=['bathroom'], form='유리 칸막이 두 면(유리: 충돌하지만 시야 통과)과 샤워기. 안은 증기.', anim='샤워기 물줄기(선택)', textures='`tex-glass-frosted`(아래쪽 반투명 띠)', effects='`vfx-steam`', budget='600 삼각형'),
    dict(id='towel-rack', kind='model', group='3장 물가', name='수건과 수건걸이', patterns=['towel', 'towel_bar'], rooms=['bathroom'], form='걸린 수건(주름), 뒤 틈은 은신처', anim='-', textures='`tex-fabric-towel`', effects='-', budget='600 삼각형'),
    # ---------------- 베란다 (4장)
    dict(id='room-veranda', kind='model', group='4장 베란다', name='베란다 방 껍데기', patterns=['floor', 'ceiling', 'wall_*', 'screen_window'], rooms=['veranda'], form='타일 바닥, 바깥쪽은 방충망 창(밤 도시 불빛이 비침). 천장 빨래 건조대 고리.', anim='-', textures='`tex-tile-large`, `tex-screen-mesh`, `tex-night-city`(창밖)', effects='`light-city-glow`(창밖 은은한 주황)', budget='1,000 삼각형'),
    dict(id='veranda-table', kind='model', group='4장 베란다', name='접이식 테이블', patterns=['table_*'], rooms=['veranda'], form='캠핑용 접이식 테이블', anim='-', textures='-', effects='-', budget='600 삼각형'),
    dict(id='icebox', kind='model', group='4장 베란다', name='아이스박스', patterns=['icebox'], rooms=['veranda'], form='파란 아이스박스, 뒤 틈은 은신처', anim='-', textures='-', effects='-', budget='300 삼각형'),
    dict(id='plant-shelf', kind='model', group='4장 베란다', name='화분 선반', patterns=['plant_shelf'], rooms=['veranda'], form='3단 선반, 화분 여러 개. 뒤는 은신처.', anim='잎이 바람에 흔들림(표현)', textures='`tex-leaves`', effects='-', budget='1,500 삼각형'),
    dict(id='drying-rack', kind='model', group='4장 베란다', name='빨래 건조대', patterns=['drying_rack'], rooms=['veranda'], form='X자 빨래 건조대와 걸린 옷', anim='옷이 바람에 흔들림(표현)', textures='`tex-fabric-laundry`', effects='-', budget='1,200 삼각형'),
    dict(id='ac-outdoor', kind='model', group='4장 베란다', name='에어컨 실외기', patterns=['ac_unit'], rooms=['veranda'], form='실외기, 큰 원형 팬 그릴. 뒤 틈은 은신처. 돌아갈 때 진동.', anim='팬 회전', textures='-', effects='뜨거운 바람 아지랑이(선택)', budget='600 삼각형'),
    dict(id='veranda-lantern', kind='model', group='4장 베란다', name='캠핑 등불', patterns=[], rooms=['veranda'], hook='판정 형상 없음(표현 전용). 테이블 위 건조대 고리에 매단다. 조명 `light-lantern`의 위치(0, 120, 10)에 둔다.', size='15 × 25 × 15u', form='한지 갓을 씌운 작은 LED 캠핑 등불, 손잡이 고리', anim='바람에 아주 느리게 흔들림(±3°), 빛 일렁임과 함께', textures='`tex-lantern-paper`(빛이 비치는 한지, 투과 마스크)', effects='`light-lantern`(일렁이는 주황빛), 주위를 도는 작은 날벌레 2~3마리(선택)', budget='300 삼각형'),
    dict(id='spray-dispenser', kind='model', group='기믹', name='자동 모기약 분사기', patterns=['spray_dispenser', 'dispenser'], form='벽걸이 자동 분사기, 분사 전 LED 깜빡', hook='레벨 데이터 sprayDispensers[] 위치 + 방 형상.', anim='분사 직전 LED 깜빡(예고), 분사 때 노즐 움찔', textures='-', effects='`vfx-spray-cloud`', budget='300 삼각형'),
    dict(id='mosquito-coil', kind='model', group='기믹', name='모기향', patterns=['coil'], form='나선 모기향과 금속 받침, 끝이 붉게 탐', hook='레벨 데이터 coils[] 위치 + `GimmickView`(지금은 원기둥).', anim='타는 끝 빛 깜빡', textures='`tex-coil`', effects='`vfx-coil-smoke`', budget='600 삼각형'),
    dict(id='spider-web', kind='model', group='기믹', name='거미줄', patterns=['web_*'], form='모서리 거미줄, 이슬이 맺힌 실', hook='레벨 데이터 webs[](Hazard 상자)', anim='바람에 흔들림', textures='`tex-web`(알파)', effects='걸리면 실이 휘감기는 이펙트', budget='판 + 실 몇 가닥'),
    dict(id='water-drop', kind='model', group='기믹', name='물방울', patterns=[], form='지름 3u 물방울, 굴절·반사', hook='`WaterView`(지금은 구)', anim='맺힘 → 떨어짐 → 튀김, 갇힌 모키 몸부림에 막이 늘어남(탈출 진행도)', textures='-', effects='`vfx-water-drop`', budget='구 + 셰이더'),
    # ---------------- 판정 볼륨 (모델 없음)
    dict(id='volume-shadow-zone', kind='volume', group='판정 볼륨 (모델 없음)', name='은신처', patterns=['shadow_*'], form='판정 볼륨. 시각은 `vfx-shadow-cue`(푸른 은신처 표시)와 실제 그늘(조명)로 보인다.', anim='-', textures='-', effects='`vfx-shadow-cue`', budget='-'),
    dict(id='volume-humid', kind='volume', group='판정 볼륨 (모델 없음)', name='습기·증기 영역', patterns=['steam_*', 'humid_*'], form='판정 볼륨. 시각은 `vfx-steam`.', anim='-', textures='-', effects='`vfx-steam`', budget='-'),
]

TEXTURES = [
    ('tex-moki-atlas', '모키 색 아틀라스', '512², 단색 영역 위주 + 줄무늬 스타킹 무늬, 툰 셰이더 베이스 컬러'),
    ('tex-moki-wing', '모키 날개맥', '256², 알파 마스크(날개맥 선 + 가장자리 반짝), 양면'),
    ('tex-human-skin', '인간 피부', '1024², 피부 톤 2~3종 변형, 체온 표시와 어울리게 따뜻하게'),
    ('tex-human-outfits', '인간 의상', '1024², 의상 4종 + 동행자, 무늬는 크게(모기 시점에서 가까이 보임)'),
    ('tex-floor-wood', '나무 마루', '1024² 타일링, 나뭇결 + 이음새'),
    ('tex-floor-tile', '주방 타일', '512² 타일링'),
    ('tex-wallpaper-lavender', '라벤더 벽지', '512² 타일링, 은은한 패턴'),
    ('tex-wallpaper-cream', '크림 벽지', '512² 타일링'),
    ('tex-fabric-sofa', '소파 천', '512² 타일링, 직물 결'),
    ('tex-fabric-curtain', '커튼 천', '512² 타일링'),
    ('tex-fabric-bedding', '침구', '512² 타일링, 무늬'),
    ('tex-fabric-towel', '수건', '256² 타일링, 파일 결'),
    ('tex-fabric-laundry', '빨래', '512², 옷 무늬 몇 종'),
    ('tex-tv-screen', 'TV 화면', '512², 8~16프레임 밤 드라마 장면 플립북(발광), 조명 `light-tv-screen` 색과 맞춤'),
    ('tex-phone-screen', '휴대폰 화면', '256², 밝은 SNS 화면 4프레임(발광)'),
    ('tex-wood-oak', '원목', '512² 타일링'),
    ('tex-wood-white', '흰 도장 목재', '256² 타일링'),
    ('tex-books', '책 등', '512², 책등 묶음'),
    ('tex-lampshade', '스탠드 갓', '256², 빛이 비치는 천(투과 마스크)'),
    ('tex-leaves', '잎', '512² 알파 카드'),
    ('tex-countertop', '조리대 상판', '512² 타일링, 인조 대리석'),
    ('tex-fridge-metal', '냉장고 금속', '256², 헤어라인 금속'),
    ('tex-tile-small', '작은 욕실 타일', '512² 타일링'),
    ('tex-tile-large', '큰 베란다 타일', '512² 타일링'),
    ('tex-ceramic', '도자기', '256², 매끈한 흰색 + 하이라이트'),
    ('tex-mirror', '거울', '반사 프로브 또는 단순 밝은 그라데이션'),
    ('tex-glass-frosted', '반투명 유리', '256², 아래쪽 불투명 띠 알파'),
    ('tex-screen-mesh', '방충망', '256² 알파 격자'),
    ('tex-night-city', '창밖 밤 도시', '1024², 흐린 불빛 보케(발광)'),
    ('tex-net-mesh', '모기장 그물', '256² 알파 격자, 고운 흰 실'),
    ('tex-web', '거미줄', '512² 알파, 방사형 실 + 이슬'),
    ('tex-coil', '모기향', '256², 녹색 나선 + 탄 끝'),
    ('tex-swatter-mesh', '모기채 그물', '256² 알파 격자'),
    ('tex-beer-label', '맥주 캔 라벨', '256²'),
    ('tex-spray-label', '스프레이 라벨', '256², 모기 경고 그림'),
    ('tex-light-cookie-window', '창틀 빛 쿠키', '256² 흑백, 창살 모양(달빛 스팟 조명 쿠키)'),
    ('tex-light-cookie-blinds', '블라인드 빛 쿠키', '256² 흑백 줄무늬(선택)'),
    ('tex-lantern-paper', '등불 한지', '256², 빛이 비치는 한지 결(투과 마스크)'),
]

VFX = [
    ('vfx-moki-cues', '모키 표현 큐', 'moki-cues'),
    ('vfx-heat-shimmer', '체온 일렁임', None),
    ('vfx-bite-mark', '물린 자국 점', None),
    ('vfx-attack-telegraph', '공격 예고 고리와 손 접근 줄기', None),
    ('vfx-shadow-cue', '은신처 표시', None),
    ('vfx-steam', '증기', None),
    ('vfx-co2', 'CO₂ 날숨', None),
    ('vfx-spray-cloud', '모기약 연무', None),
    ('vfx-coil-smoke', '모기향 연기', None),
    ('vfx-wind-dust', '바람 먼지 줄기', None),
    ('vfx-water-drop', '물방울 맺힘·낙하·튀김', None),
    ('vfx-swatter-zap', '전기 모기채 스파크', None),
    ('vfx-light-shaft', '달빛 빛줄기와 떠다니는 먼지 (lighting.md)', None),
]


def load(path):
    with open(path, encoding='utf-8') as f:
        return json.load(f)


def catalog_levels():
    catalog = load(os.path.join(DATA, 'stages.json'))
    order = []
    for chapter in catalog['chapters']:
        for stage in chapter['stages']:
            order.append((stage['level'], chapter['title'], stage['title']))
    return order


def shape_size(shape):
    if shape['type'] == 'box':
        return '상자 {} × {} × {}u'.format(*[round(v, 1) for v in shape['size']])
    if shape['type'] == 'sphere':
        return '구 지름 {}u'.format(round(shape['radius'] * 2, 1))
    a, b = shape['a'], shape['b']
    length = sum((a[i] - b[i]) ** 2 for i in range(3)) ** 0.5
    return '캡슐 길이 {}u, 지름 {}u'.format(round(length, 1), round(shape['radius'] * 2, 1))


def usage(asset, levels):
    rooms = {}
    for level_id, chapter, title in levels:
        level = load(os.path.join(DATA, 'levels', level_id + '.json'))
        room = load(os.path.join(DATA, 'rooms', level['room'] + '.json'))
        shapes = [(s, room['id']) for s in room['shapes']]
        for key in ('fans', 'webs', 'nets', 'netGaps', 'humidZones', 'coils', 'sprayDispensers'):
            for item in level.get(key, []):
                if 'id' in item:
                    shapes.append(({'id': item['id'], 'type': 'box', 'size': item.get('size', item.get('bodySize', [0, 0, 0]))}, level_id))
        for shape, owner in shapes:
            if asset.get('rooms') and room['id'] not in asset['rooms'] and owner == room['id']:
                continue
            if any(fnmatch.fnmatchcase(shape['id'].lower(), p.lower()) for p in asset['patterns']):
                rooms.setdefault((level_id, chapter, title), []).append(shape)
    return rooms


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def model_doc(asset, levels):
    found = usage(asset, levels)
    lines = [f"# {asset['name']} (`{asset['id']}`)", '',
             '> 생성: `python Tools/asset_specs.py` (D-062). 내용을 고칠 때는 스크립트의 ASSETS 표를 고친다.', '',
             '## 사용처']
    if found:
        lines.append('| 스테이지 | 장 | 형상 ID | 판정 형상 |')
        lines.append('|---|---|---|---|')
        for (level_id, chapter, title), shapes in found.items():
            for shape in shapes:
                lines.append(f"| {level_id} {title} | {chapter} | `{shape['id']}` | {shape_size(shape)} |")
    else:
        lines.append('- 형상 데이터가 아닌 표현 오브젝트: ' + asset.get('hook', '-'))
    lines += ['', '## 형상 ID 패턴', ', '.join(f'`{p}`' for p in asset['patterns']) or '(없음 — 표현 오브젝트)', '',
              '## 형태', asset.get('form', '-'), '']
    if asset.get('size'):
        lines += ['## 크기·비율', asset['size'], '']
    if asset.get('parts'):
        lines += ['## 부분', asset['parts'], '']
    if asset.get('rig'):
        lines += ['## 뼈대·소켓', asset['rig'], '']
    if asset.get('variants'):
        lines += ['## 변형', asset['variants'], '']
    lines += ['## 움직임·상태', asset.get('anim', '-'), '',
              '## 텍스처', asset.get('textures', '-'), '',
              '## 연결된 이펙트·조명', asset.get('effects', '-'), '',
              '## 공통 규칙',
              f'- 스타일: {STYLE}',
              '- 판정 형상은 데이터 그대로 둔다. 모델은 판정 형상을 크게 벗어나지 않게(±10%) 만들어 보이는 것과 부딪히는 것이 어긋나지 않게 한다.',
              '- 피벗: 판정 형상 중심(상자·구) 또는 캡슐 끝점 A. 단위 1u = 1cm, Y-up, +Z 정면.',
              f"- 폴리곤·머티리얼 예산: {asset.get('budget', '-')}",
              f"- 교체 접점: {asset.get('hook', '`LevelView`가 형상 ID와 같은 이름의 시각 오브젝트를 만든다. 모델 프리팹을 같은 ID에 매핑해 교체한다.')}",
              '- 라이선스: `tech/asset-pipeline.md` 허용 목록, `Assets/_Project/CREDITS.md`에 기록.', '']
    return '\n'.join(lines)


def main():
    levels = catalog_levels()
    index = ['# 애셋 인덱스 (M14, D-062)', '',
             '> 생성: `python Tools/asset_specs.py`. 스테이지를 추가하면 스크립트의 표를 갱신하고 다시 실행한다(`spec/07` 레벨 추가 방법 5번).',
             '> Core 테스트 `AssetIndexTests`가 목록의 모든 방·레벨 형상이 아래 "형상 ID 패턴"에 속하는지 검사한다.', '',
             '## 모델·판정 볼륨', '',
             '| 애셋 ID | 종류 | 묶음 | 이름 | 문서 | 형상 ID 패턴 | 방 | 사용 스테이지 |',
             '|---|---|---|---|---|---|---|---|']
    for asset in ASSETS:
        doc = f"models/{asset['id']}.md"
        write(os.path.join(OUT, doc), model_doc(asset, levels))
        found = usage(asset, levels)
        stages = sorted({level_id for (level_id, _, _) in found}) if found else []
        patterns = ' '.join(f'`{p}`' for p in asset['patterns']) or '-'
        rooms = ' '.join(asset.get('rooms', [])) or '-'
        index.append(f"| {asset['id']} | {asset['kind']} | {asset['group']} | {asset['name']} | [{doc}]({doc}) | {patterns} | {rooms} | {', '.join(stages) or '(표현 오브젝트)'} |")
    index += ['', '## 텍스처', '', '| 텍스처 ID | 이름 | 사양 |', '|---|---|---|']
    for tid, name, detail in TEXTURES:
        index.append(f'| {tid} | {name} | {detail} |')
    index += ['', '## VFX·조명', '', '| ID | 이름 | 문서 |', '|---|---|---|']
    for vid, name, doc in VFX:
        link = f'[vfx/{doc}.md](vfx/{doc}.md)' if doc else '`spec/10`·`spec/11`의 현재 셰이더 표현을 정식 VFX로 교체'
        index.append(f'| {vid} | {name} | {link} |')
    index += ['| light-* | 분위기 조명 | [lighting.md](lighting.md) |', '']
    write(os.path.join(OUT, 'INDEX.md'), '\n'.join(index))
    print(f'{len(ASSETS)} asset docs, {len(TEXTURES)} textures, {len(VFX)} vfx')


if __name__ == '__main__':
    main()
