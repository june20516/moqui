# 사람 팔·몸 동작 근거 (M12, D-052·D-053)

인간이 모기를 때리는 동작을 "사람이 실제로 낼 수 있는 동작"으로 만들기 위한 배경 지식. 게임 단위는 1u ≈ 1cm.

## 1. 팔 길이 (인체 측정)
- Drillis & Contini(1966, Winter의 생체역학 교재에서 인용) 체절 길이 비율: 위팔 ≈ 0.186H, 아래팔 ≈ 0.146H, 손 ≈ 0.108H(문헌에 따라 0.189·0.145·0.128). H = 키.
- 키 170cm 기준: 위팔 ≈ 32cm, 아래팔 ≈ 25cm, 손 ≈ 18cm → 어깨~손끝 ≈ 75cm, 어깨~손바닥 중심 ≈ 66cm.
- 현재 레벨 데이터(stage01): 위팔 캡슐 길이 ≈ 29u, 아래팔(손목까지) ≈ 29u. 손은 따로 없다 → 손바닥 중심까지 손 길이의 약 절반(≈ 9u)을 더하면 어깨~손바닥 ≈ 67u로 위 값과 맞는다.
- 반면 현재 공격 사거리 `attack.reach` 80u + 광분 `frenzy.reachBonus` 40u = 120u는 팔 길이보다 훨씬 길다 → 몸을 움직이지 않으면 닿을 수 없는 거리.

## 2. 관절 가동 범위 (AAOS 정상값)
- 팔꿈치: 굽힘 0~150° (펴짐 0°, 과신전 없음).
- 어깨: 굽힘(앞으로 들기) 0~180°, 폄(뒤로) 0~60°, 벌림(옆으로) 0~180°.
- 근위 → 원위 순서(어깨가 먼저, 팔꿈치가 펴지고, 손목이 마지막)로 속도가 이어져 손은 호를 그린다.

## 3. 손 속도
- 손바닥 치기(palm strike) 연구: 충돌 직전 평균 손 속도 5.5 ± 1.0 m/s(주 사용 손 5.8, 반대 손 5.2), 빠른 경우 6~6.5 m/s. 격투 종목 손 공격 최대 속도 중앙값 5.7~7.1 m/s, 숙련 가라테 ≈ 7.7 m/s.
- 일반인이 모기를 잡으려 휘두르는 손은 훈련된 타격보다 느리다고 보고, 최고 속도를 광분 ≈ 5 m/s, 반사적 자기 몸 치기 ≈ 4 m/s, 취한 사람 ≈ 3 m/s로 둔다(추정, 아래 제안값).
- 타격 구간의 평균 속도는 최고 속도보다 낮다(가속·감속). 탄도성 동작의 평균/최고 비율을 약 0.6으로 두면, 60cm를 최고 5 m/s로 칠 때 약 0.2초가 걸린다. 현재 `attack.*.activeTime` 0.1초보다 느리다.

## 4. 반응·자세 전환 시간
- 단순 시각 반응 시간: 건강한 성인 약 200~250ms(대부분 190~300ms). 예고(치켜듦) 시간은 이보다 짧을 수 없다.
- 앉은 자리에서 일어서기(sit-to-stand): 건강한 성인 평균 약 2.2초(굽힘 단계 0.8초 + 폄 단계 1.4초). 급히 일어나면 더 짧을 수 있으나 1초 아래는 어렵다고 본다.
- 상체 기울이기·허리 돌리기는 일어서기보다 빠르다(추정 0.3~0.6초).

## 5. 게임 수치 제안 (`spec/tuning.md`에 확정)
| 키 | 제안값 | 근거 |
|---|---|---|
| human.handReachExtra | 9u | 손목 → 손바닥 중심(손 길이 18cm의 절반) |
| human.elbowFlexMax | 150° | AAOS |
| human.shoulderFlexMax / ExtensionMax / AbductionMax | 180° / 60° / 180° | AAOS |
| attack.handPeakSpeed.frenzy / reaction / drunk | 500 / 400 / 300 u/s | §3 (일반인 추정) |
| attack.averageToPeakSpeed | 0.6 | 탄도성 동작 평균/최고 비율(추정) |
| attack.minTelegraph | 0.2 s | 반응 시간 하한 |
| posture.leanTime / turnTime / riseTime | 0.4 / 0.4 / 1.2 s | §4 (급히 일어남, 추정) |
| posture.leanReach / riseReach | 30 / 50 u | 상체 기울임·일어서며 늘어나는 손 도달 거리(추정) |

추정값은 플레이 감각과 봇 결과로 조정할 수 있다. 조정하면 decisions에 사유를 남긴다.

## 출처
- 손 속도: [Force and velocity of impact during upper limb strikes in combat sports (systematic review)](https://www.researchgate.net/publication/343020286_Force_and_velocity_of_impact_during_upper_limb_strikes_in_combat_sports_a_systematic_review_and_meta-analysis), [The effect of hand dominance on martial arts strikes (PMC3274566)](https://pmc.ncbi.nlm.nih.gov/articles/PMC3274566/), [Biomechanical assessment of various punching techniques (PMC8036214)](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8036214/)
- 체절 길이: [Winter, Anthropometry (Drillis & Contini 비율 그림)](https://courses.grainger.illinois.edu/me481/sp2021/Anthro-Winter.pdf), [Body segment lengths as proportion of height (Drillis & Contini)](https://www.researchgate.net/figure/Body-segment-lengths-expressed-as-proportion-of-body-height-H-by-Drillis-and-Contini_fig12_325960988)
- 관절 가동 범위: [Physiopedia — Range of Motion Normative Values](https://www.physio-pedia.com/Range_of_Motion_Normative_Values), [AAOS ROM chart](https://goniometer.io/range-of-motion)
- 반응 시간: [Diurnal variation in visual simple reaction time (PMC7846399)](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC7846399/)
- 일어서기: [Kinematic analysis of the human body during sit-to-stand in healthy young adults](https://doi.org/10.1097/MD.0000000000026208), [Biomechanical analysis of the sit-to-stand motion in elderly persons](https://www.archives-pmr.org/article/0003-9993(92)90124-F/abstract)
