"""Explosies, opnieuw opgebouwd vanuit hoe een echte explosie werkt.

Lagen:
1. Punch  - de drukgolf (Friedlander-golfvorm: direct omhoog, dan uitzakken
            met een kleine onderdruk erna). Geeft de klap in je borst.
2. Boom   - diep laag, met snelle onregelmatige "brokkeligheid" erin.
3. Body   - het vuur/de steekvlam: ruis in het middengebied die hevig en
            onregelmatig golft en langzaam donkerder wordt.
4. Rumble - het nagerommel dat iets later aanzwelt en wegrolt.
5. Debris - wegvliegende en neervallende brokstukken.

Gebruik: python3 explosion.py -> explosion_A.wav, explosion_B.wav, explosion_C.wav
"""
import numpy as np
from scipy.io import wavfile
import sfx_real as fx
from sfx_real import SR, n_of, filt, sweep_low, grains, compress, air


def rumble_mod(n, hz, depth):
    """Onregelmatige, wat 'piekerige' amplitude-golving (brokkelig, niet glad)."""
    m = np.abs(filt(fx.rng.standard_normal(n), "low", hz, order=2)) ** 1.4
    m /= m.max() + 1e-9
    return (1 - depth) + depth * m


def explosion(seed, size=1.0, distance=0.0):
    fx.rng = np.random.default_rng(seed)
    rng = fx.rng
    dur = 1.4 + 1.4 * size
    n = n_of(dur)
    t = np.arange(n) / SR

    # 1. punch: Friedlander-drukgolf
    T = 0.006 + 0.012 * size
    fried = np.where(t < 8 * T, (1 - t / T) * np.exp(-1.6 * t / T), 0.0)
    punch = filt(fried, "low", 2500 - 1500 * distance) * 1.4
    punch += filt(rng.standard_normal(n), "band", [800, 6000]) * np.exp(-t * (90 - 50 * distance)) * 0.35

    # 2. boom: diep laag, brokkelig
    boom = filt(fx.brown(n), "low", 110, order=4)
    boom /= np.max(np.abs(boom))
    boom *= (1 - np.exp(-t * 120)) * np.exp(-t * 2.4 / size) * rumble_mod(n, 30, 0.6)

    # 3. body: steekvlam in het middengebied
    body = filt(fx.pink(n), "band", [120, 6000])
    body = sweep_low(body, 7000 - 3000 * distance, 500)
    body /= np.max(np.abs(body))
    body *= (1 - np.exp(-t * 60)) * np.exp(-t * 3.2 / size) * rumble_mod(n, 18, 0.8)

    # 4. rumble: iets later aanzwellend nagerommel
    rum = filt(fx.brown(n), "band", [50, 350])
    rum /= np.max(np.abs(rum))
    rum_env = (1 - np.exp(-np.clip(t - 0.08, 0, None) * 8)) * np.exp(-t * 1.6 / size)
    rum *= rum_env * rumble_mod(n, 9, 0.7)

    # 5. debris: brokstukken, gedempt op afstand
    k = int(120 * size)
    times = np.concatenate([rng.exponential(0.15 * size, k) + 0.03,
                            rng.uniform(0.4, 0.4 + 1.0 * size, int(10 * size))])
    amps = np.where(times < 0.5, 1.0, 0.45) * rng.uniform(0.2, 1.0, len(times))
    debris = grains(n, times, (0.002, 0.025), (700, 8000 - 4000 * distance), amps)
    debris /= np.max(np.abs(debris)) + 1e-9

    mix = (punch * 1.0 + boom * (1.1 + 0.3 * size) + body * (0.9 - 0.3 * distance)
           + rum * (0.6 + 0.3 * distance) + debris * (0.35 - 0.2 * distance))

    # afstand: hoge tonen verdwijnen eerst door de lucht
    mix = filt(mix, "low", 14000 - 10000 * distance, order=2)
    mix = np.tanh(mix / np.max(np.abs(mix)) * 1.4)        # lichte warmte, geen gruis
    return air(mix, wet=0.15)


def finish(x, peak=0.89):
    env = np.max(np.abs(x), axis=1)
    last = np.nonzero(env > env.max() * 1e-3)[0][-1] + n_of(0.05)
    x = x[:last].copy()
    f = min(n_of(0.2), len(x) // 4)
    x[-f:] *= np.linspace(1, 0, f)[:, None]
    x = compress(x, thr_db=-24, ratio=4, rel_ms=80)
    # limiter: alleen de pieken zacht afronden, de rest blijft schoon
    x = x / np.max(np.abs(x)) * 2.2
    k = 0.6
    a = np.abs(x)
    x = np.where(a > k, np.sign(x) * (k + (1 - k) * np.tanh((a - k) / (1 - k))), x)
    return x / np.max(np.abs(x)) * peak


VARIANTS = {
    "A": dict(seed=11, size=0.6, distance=0.0),   # dichtbij, kort en punchy
    "B": dict(seed=23, size=1.2, distance=0.15),  # groot en zwaar
    "C": dict(seed=37, size=1.0, distance=0.8),   # op afstand
}

if __name__ == "__main__":
    for name, kw in VARIANTS.items():
        x = finish(explosion(**kw))
        wavfile.write(f"explosion_{name}.wav", SR, (x * 32767).astype(np.int16))
        print("saved", f"explosion_{name}.wav")
