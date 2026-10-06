"""모든 사운드 ID를 코드로 합성해 WAV로 쓴다 (tech/asset-pipeline.md 조달 1순위: 직접 생성).

사용: python tools/gen_audio.py
출력: Assets/_Project/Audio/Generated/<id>.wav (22.05 kHz, 모노, 16비트). 같은 입력이면 같은 파일이 나온다 (고정 시드).
"""

import math
import os
import random
import zlib
import struct
import wave

SAMPLE_RATE = 22050
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), '..', 'Assets', '_Project', 'Audio', 'Generated')
TAU = 2.0 * math.pi


# ---- 기본 도구 ----

def samples(seconds):
    return int(round(seconds * SAMPLE_RATE))


def sweep(start_hz, end_hz, seconds, wave_fn=math.sin):
    """주파수가 선형으로 바뀌는 진동 (위상 누적)."""
    count = samples(seconds)
    out = []
    phase = 0.0
    for i in range(count):
        t = i / count
        phase += (start_hz + (end_hz - start_hz) * t) / SAMPLE_RATE
        out.append(wave_fn(TAU * phase))
    return out


def square(x):
    return 1.0 if math.sin(x) >= 0.0 else -1.0


def saw(x):
    return ((x / TAU) % 1.0) * 2.0 - 1.0


def triangle(x):
    return 2.0 * abs(saw(x)) - 1.0


def noise(seconds, rng):
    return [rng.uniform(-1.0, 1.0) for _ in range(samples(seconds))]


def lowpass(signal, cutoff_hz):
    """1극 저역 통과. cutoff_hz는 상수 또는 진행률(0~1) → Hz 함수."""
    out = []
    state = 0.0
    count = len(signal)
    for i, x in enumerate(signal):
        cutoff = cutoff_hz(i / count) if callable(cutoff_hz) else cutoff_hz
        alpha = 1.0 - math.exp(-TAU * cutoff / SAMPLE_RATE)
        state += alpha * (x - state)
        out.append(state)
    return out


def highpass(signal, cutoff_hz):
    low = lowpass(signal, cutoff_hz)
    return [x - l for x, l in zip(signal, low)]


def envelope(signal, attack, release):
    """attack·release는 전체 길이에 대한 비율. 그 사이는 1."""
    count = len(signal)
    out = []
    for i, x in enumerate(signal):
        t = i / count
        gain = 1.0
        if attack > 0.0 and t < attack:
            gain = t / attack
        if release > 0.0 and t > 1.0 - release:
            gain = min(gain, (1.0 - t) / release)
        out.append(x * gain)
    return out


def decay(signal, rate):
    """지수 감쇠 (rate: 1/s)."""
    return [x * math.exp(-rate * i / SAMPLE_RATE) for i, x in enumerate(signal)]


def mix(*signals):
    length = max(len(s) for s in signals)
    out = [0.0] * length
    for s in signals:
        for i, x in enumerate(s):
            out[i] += x
    return out


def gain(signal, amount):
    return [x * amount for x in signal]


def modulate(signal, fn):
    """진행 시간(초) → 배율 함수로 진폭을 바꾼다."""
    return [x * fn(i / SAMPLE_RATE) for i, x in enumerate(signal)]


def offset(signal, seconds):
    return [0.0] * samples(seconds) + signal


def silence(seconds):
    return [0.0] * samples(seconds)


def fit(signal, seconds):
    count = samples(seconds)
    return (signal + [0.0] * count)[:count]


def normalize(signal, peak):
    top = max(abs(x) for x in signal) or 1.0
    return [x * peak / top for x in signal]


def loop_crossfade(signal, seconds):
    """끝 구간을 앞 구간에 겹쳐 이어 붙인다 (루프 이음매 제거). 길이는 crossfade만큼 줄어든다."""
    fade = samples(seconds)
    body = signal[:-fade]
    tail = signal[-fade:]
    for i in range(fade):
        w = i / fade
        body[i] = body[i] * w + tail[i] * (1.0 - w)
    return body


def note_hz(semitones_from_a4):
    return 440.0 * (2.0 ** (semitones_from_a4 / 12.0))


def tone(hz, seconds, wave_fn=math.sin):
    return sweep(hz, hz, seconds, wave_fn)


def pluck(hz, seconds, rate, wave_fn=triangle):
    return decay(envelope(tone(hz, seconds, wave_fn), 0.01, 0.1), rate)


# ---- 효과음 ----

def sfx_wing_loop(rng):
    # 모기 날갯소리: 600 Hz 톱니 + 약한 진폭 떨림. 1초에 정수 주기라 이음매가 없다.
    base = tone(600.0, 1.0, saw)
    second = gain(tone(1200.0, 1.0, math.sin), 0.3)
    buzz = lowpass(mix(base, second), 2500.0)
    return modulate(buzz, lambda t: 0.8 + 0.2 * math.sin(TAU * 30.0 * t))


def sfx_dash(rng):
    whoosh = lowpass(noise(0.35, rng), lambda t: 400.0 + 3000.0 * math.sin(math.pi * t))
    return envelope(whoosh, 0.15, 0.6)


def sfx_attach(rng):
    thump = decay(sweep(180.0, 90.0, 0.12), 35.0)
    click = decay(lowpass(noise(0.02, rng), 3000.0), 200.0)
    return mix(thump, gain(click, 0.4))


def sfx_detach(rng):
    return decay(envelope(sweep(140.0, 300.0, 0.12), 0.05, 0.3), 20.0)


def sfx_suck_loop(rng):
    # 꿀꺽이는 흡혈: 90 Hz 저음이 초당 4번 부풀고, 초당 2번 거품 소리.
    low = modulate(tone(90.0, 1.0), lambda t: 0.5 + 0.5 * math.sin(TAU * 4.0 * t))
    bubbles = modulate(lowpass(noise(1.0, rng), 900.0), lambda t: max(0.0, math.sin(TAU * 2.0 * t)) ** 4)
    return mix(low, gain(bubbles, 1.5))


def sfx_slap(rng):
    crack = decay(lowpass(noise(0.25, rng), 4000.0), 30.0)
    body = decay(sweep(140.0, 70.0, 0.25), 18.0)
    return mix(crack, gain(body, 0.7))


def sfx_clap(rng):
    burst = lambda: decay(highpass(noise(0.12, rng), 800.0), 45.0)
    return fit(mix(burst(), offset(burst(), 0.012), offset(gain(burst(), 0.6), 0.025)), 0.3)


def sfx_frenzy(rng):
    a = sweep(note_hz(0), note_hz(-5), 0.6, square)
    b = sweep(note_hz(1), note_hz(-4), 0.6, square)
    return decay(envelope(lowpass(mix(a, b), 2500.0), 0.01, 0.3), 3.0)


def sfx_frenzy_loop(rng):
    # 긴장 루프: 55 Hz 톱니 저음 + 심장 박동처럼 초당 2번 조인다.
    drone = lowpass(tone(55.0, 2.0, saw), 400.0)
    beat = lambda t: 0.35 + 0.65 * (max(0.0, math.sin(TAU * 2.0 * t)) ** 6)
    return modulate(drone, beat)


def sfx_telegraph(rng):
    # 짧고 날카롭게 내려가는 사각파 경고음 (M7 HUD 경고음과 같은 설계).
    return envelope(sweep(1900.0, 1200.0, 0.18, square), 0.05, 0.9)


def sfx_spray(rng):
    hiss = highpass(noise(0.8, rng), 2500.0)
    return envelope(hiss, 0.05, 0.5)


def sfx_toxin(rng):
    wobble = []
    phase = 0.0
    count = samples(0.4)
    for i in range(count):
        t = i / count
        hz = (600.0 - 300.0 * t) * (1.0 + 0.04 * math.sin(TAU * 12.0 * t * 0.4))
        phase += hz / SAMPLE_RATE
        wobble.append(math.sin(TAU * phase))
    return envelope(wobble, 0.05, 0.4)


def sfx_breath(rng):
    air = lowpass(noise(1.2, rng), 700.0)
    return modulate(air, lambda t: math.sin(math.pi * t / 1.2) ** 2)


def sfx_dislodge(rng):
    whoosh = envelope(lowpass(noise(0.3, rng), 1800.0), 0.05, 0.6)
    drop = decay(sweep(500.0, 150.0, 0.3), 8.0)
    return mix(whoosh, gain(drop, 0.6))


def sfx_decoy(rng):
    chime = mix(*[offset(pluck(note_hz(n), 0.6, 6.0, math.sin), 0.08 * k) for k, n in enumerate((15, 19, 22, 27))])
    whine = gain(envelope(tone(700.0, 0.8, saw), 0.2, 0.5), 0.15)
    return fit(mix(chime, lowpass(whine, 2000.0)), 0.8)


def sfx_drop_trap(rng):
    return decay(envelope(sweep(300.0, 900.0, 0.3), 0.02, 0.2), 9.0)


def sfx_escape(rng):
    pop = decay(sweep(900.0, 1500.0, 0.25), 20.0)
    burst = decay(highpass(noise(0.05, rng), 1500.0), 80.0)
    return mix(pop, gain(burst, 0.4))


def sfx_ui_select(rng):
    return envelope(tone(880.0, 0.05), 0.05, 0.6)


def sfx_ui_confirm(rng):
    return mix(envelope(tone(880.0, 0.06), 0.05, 0.4), offset(envelope(tone(1320.0, 0.08), 0.05, 0.6), 0.05))


def sfx_ui_cancel(rng):
    return mix(envelope(tone(660.0, 0.06), 0.05, 0.4), offset(envelope(tone(440.0, 0.08), 0.05, 0.6), 0.05))


def sfx_snore(rng):
    # 들숨에 코골이(30 Hz 떨림), 날숨은 조용한 바람.
    inhale = modulate(lowpass(noise(1.2, rng), 500.0), lambda t: (0.6 + 0.4 * math.sin(TAU * 30.0 * t)) * math.sin(math.pi * t / 1.2))
    exhale = gain(modulate(lowpass(noise(1.3, rng), 900.0), lambda t: math.sin(math.pi * t / 1.3)), 0.35)
    return inhale + exhale


def sfx_wake(rng):
    hm = mix(sweep(200.0, 300.0, 0.4, triangle), gain(sweep(400.0, 600.0, 0.4), 0.3))
    return envelope(lowpass(hm, 1500.0), 0.1, 0.4)


def sfx_drip(rng):
    return decay(sweep(1400.0, 700.0, 0.15), 30.0)


def sfx_steam(rng):
    hiss = highpass(lowpass(noise(2.2, rng), 6000.0), 1500.0)
    return loop_crossfade(hiss, 0.2)


def sfx_wind_loop(rng):
    # 바람에 밀리는 소리: 낮은 대역 잡음이 천천히 일렁인다. 1.5초 주기 변조라 3초 루프가 이어진다.
    rush = lowpass(noise(3.2, rng), lambda p: 500.0 + 250.0 * math.sin(TAU * p * 3.2 / 1.5))
    rush = modulate(rush, lambda t: 0.75 + 0.25 * math.sin(TAU * t / 1.5))
    return loop_crossfade(rush, 0.2)


def sfx_wind_gust(rng):
    # 바람에 처음 밀릴 때: 부풀었다 빠지는 짧은 "휙".
    gust = lowpass(noise(0.6, rng), lambda p: 300.0 + 1800.0 * math.sin(math.pi * p))
    return modulate(gust, lambda t: math.sin(math.pi * t / 0.6))



def sfx_footstep(rng):
    # 맨발 발소리: 낮은 쿵 + 짧은 잡음.
    thump = decay(sweep(110.0, 60.0, 0.18), 18.0)
    scuff = decay(lowpass(noise(0.18, rng), 900.0), 30.0)
    return mix(thump, gain(scuff, 0.4))

# ---- 환경음 (4초 루프) ----

def tv_murmur(rng, seconds=4.2):
    """알아들을 수 없는 TV 말소리: 대역 잡음에 음절처럼 들쭉날쭉한 크기."""
    voice = highpass(lowpass(noise(seconds, rng), 1800.0), 250.0)
    levels = [rng.uniform(0.1, 1.0) for _ in range(int(seconds / 0.15) + 2)]
    smooth = lambda t: levels[int(t / 0.15)] * (1 - (t / 0.15 % 1)) + levels[int(t / 0.15) + 1] * (t / 0.15 % 1)
    return loop_crossfade(modulate(voice, smooth), 0.2)


def amb_stage1(rng):
    return tv_murmur(rng)


def amb_stage2(rng):
    hum = gain(tone(60.0, 4.2), 0.15)
    return mix(tv_murmur(rng), hum[:samples(4.0)])


def amb_stage3(rng):
    # 선풍기: 저역 바람 + 날개 회전(초당 25번) 웅웅거림.
    air = lowpass(noise(4.2, rng), 600.0)
    whir = modulate(air, lambda t: 0.75 + 0.25 * math.sin(TAU * 25.0 * t))
    return loop_crossfade(whir, 0.2)


def amb_stage4(rng):
    vent = lowpass(noise(4.2, rng), 350.0)
    drips = mix(*[offset(gain(decay(sweep(1300.0, 700.0, 0.15), 30.0), 0.5), at) for at in (0.7, 1.9, 2.6, 3.4)])
    return loop_crossfade(mix(vent, gain(fit(drips, 4.2), 0.25)), 0.2)


def amb_stage5(rng):
    # 풀벌레: 4.5 kHz 짧은 울음이 3번씩 묶여 반복.
    out = silence(4.2)
    for group_start in (0.2, 1.1, 2.0, 2.9, 3.6):
        for k in range(3):
            chirp = envelope(tone(4500.0 + rng.uniform(-200.0, 200.0), 0.04), 0.2, 0.5)
            start = samples(group_start + k * 0.07)
            for i, x in enumerate(chirp):
                if start + i < len(out):
                    out[start + i] += x * 0.6
    night = gain(lowpass(noise(4.2, rng), 300.0), 0.3)
    return loop_crossfade(mix(out, night), 0.2)


# ---- 음악 (짧은 루프) ----

def music(progression, seconds_per_chord, bass_pattern, arpeggio_rate, rng):
    """화음 진행: 패드(사인+삼각) + 저음 패턴 + 아르페지오. progression은 반음 목록들(A4 기준)."""
    total = []
    for chord in progression:
        pad = mix(*[gain(tone(note_hz(n - 12), seconds_per_chord, triangle), 0.12) for n in chord])
        pad = envelope(pad, 0.1, 0.1)
        bass = silence(0.0)
        step = seconds_per_chord / len(bass_pattern)
        for k, use in enumerate(bass_pattern):
            if use:
                bass = mix(bass, offset(pluck(note_hz(chord[0] - 24), step, 6.0, math.sin), k * step))
        arp = silence(0.0)
        count = int(seconds_per_chord * arpeggio_rate)
        for k in range(count):
            n = chord[k % len(chord)] + (12 if k % 4 == 3 else 0)
            arp = mix(arp, offset(gain(pluck(note_hz(n), 0.3, 10.0), 0.18), k / arpeggio_rate))
        total += fit(mix(pad, gain(bass, 0.5), arp), seconds_per_chord)
    return total


def bgm_title(rng):
    # 느긋한 밤 분위기: Am - F - C - G, 화음당 4초.
    progression = [(0, 3, 7), (-4, 0, 3), (3, 7, 10), (-2, 2, 5)]
    return music(progression, 4.0, (1, 0, 1, 0), 3, rng)


def bgm_stage(rng):
    # 긴장된 잠입: Dm - Bb - Gm - A, 화음당 4초, 저음이 8분 음표로 걷는다.
    progression = [(-7, -4, 0), (-11, -7, -4), (-2, 1, 5), (0, 4, 7)]
    return music(progression, 4.0, (1, 1, 0, 1, 1, 0, 1, 1), 4, rng)


# ID → (생성 함수, 최대 진폭)
SOUNDS = {
    'sfx_wing_loop': (sfx_wing_loop, 0.5),
    'sfx_dash': (sfx_dash, 0.8),
    'sfx_attach': (sfx_attach, 0.7),
    'sfx_detach': (sfx_detach, 0.6),
    'sfx_suck_loop': (sfx_suck_loop, 0.6),
    'sfx_slap': (sfx_slap, 0.95),
    'sfx_clap': (sfx_clap, 0.95),
    'sfx_frenzy': (sfx_frenzy, 0.8),
    'sfx_frenzy_loop': (sfx_frenzy_loop, 0.5),
    'sfx_telegraph': (sfx_telegraph, 0.6),
    'sfx_spray': (sfx_spray, 0.7),
    'sfx_toxin': (sfx_toxin, 0.6),
    'sfx_breath': (sfx_breath, 0.5),
    'sfx_dislodge': (sfx_dislodge, 0.8),
    'sfx_decoy': (sfx_decoy, 0.6),
    'sfx_drop_trap': (sfx_drop_trap, 0.7),
    'sfx_escape': (sfx_escape, 0.7),
    'sfx_ui_select': (sfx_ui_select, 0.4),
    'sfx_ui_confirm': (sfx_ui_confirm, 0.5),
    'sfx_ui_cancel': (sfx_ui_cancel, 0.5),
    'sfx_snore': (sfx_snore, 0.5),
    'sfx_wake': (sfx_wake, 0.6),
    'sfx_drip': (sfx_drip, 0.5),
    'sfx_steam': (sfx_steam, 0.4),
    'sfx_wind_loop': (sfx_wind_loop, 0.45),
    'sfx_wind_gust': (sfx_wind_gust, 0.6),
    'sfx_footstep': (sfx_footstep, 0.6),
    'amb_stage1': (amb_stage1, 0.35),
    'amb_stage2': (amb_stage2, 0.35),
    'amb_stage3': (amb_stage3, 0.4),
    'amb_stage4': (amb_stage4, 0.4),
    'amb_stage5': (amb_stage5, 0.35),
    'bgm_title': (bgm_title, 0.6),
    'bgm_stage': (bgm_stage, 0.6),
}


def write_wav(path, signal):
    with wave.open(path, 'wb') as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(SAMPLE_RATE)
        frames = b''.join(struct.pack('<h', int(max(-1.0, min(1.0, x)) * 32767)) for x in signal)
        out.writeframes(frames)


# 시드 고정: M13까지 있던 소리는 그때의 정렬 순번 시드를 그대로 쓴다. 새 소리는 ID의 crc32를 쓴다.
# (소리를 추가해도 기존 소리가 다시 합성되어 바뀌지 않게 한다.)
LEGACY_IDS = sorted([
    'amb_stage1', 'amb_stage2', 'amb_stage3', 'amb_stage4', 'amb_stage5', 'bgm_stage', 'bgm_title',
    'sfx_attach', 'sfx_breath', 'sfx_clap', 'sfx_dash', 'sfx_decoy', 'sfx_detach', 'sfx_dislodge', 'sfx_drip',
    'sfx_drop_trap', 'sfx_escape', 'sfx_frenzy', 'sfx_frenzy_loop', 'sfx_slap', 'sfx_snore', 'sfx_spray',
    'sfx_steam', 'sfx_suck_loop', 'sfx_telegraph', 'sfx_toxin', 'sfx_ui_cancel', 'sfx_ui_confirm', 'sfx_ui_select',
    'sfx_wake', 'sfx_wind_gust', 'sfx_wind_loop', 'sfx_wing_loop',
])


def seed_for(sound_id):
    if sound_id in LEGACY_IDS:
        return 1000 + LEGACY_IDS.index(sound_id)
    return zlib.crc32(sound_id.encode('utf-8'))


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    for sound_id, (generate, peak) in sorted(SOUNDS.items()):
        rng = random.Random(seed_for(sound_id))
        signal = normalize(generate(rng), peak)
        write_wav(os.path.join(OUTPUT_DIR, sound_id + '.wav'), signal)
        print(f'{sound_id}: {len(signal) / SAMPLE_RATE:.2f}s')


if __name__ == '__main__':
    main()
