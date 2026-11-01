"""Ronde 6: slak iets langzamer, boss-slak minder bas."""
import numpy as np
import sfxlib as s
from sfxlib import n_of, place, lp, hp, bp
import cat_fix4 as F4
from cat_fix import replace, boss_bass, ENTRIES
import cat_fix as F

F.FIXED.clear()
FIXED = F.FIXED


def slither_slow(dur, size=1.0, density=65, glorps=1.0, wave_hz=0.55):
    n = n_of(dur)
    wave = s.smooth_rand(n, wave_hz, 0.3, 1.0)
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: density * wave[min(int(q * s.SR), n - 1)], dur - 0.05):
        place(out, F4.smack(size * 1.15), t0, s.rng.uniform(0.2, 1.0) ** 1.3)
    for t0 in s.poisson_times(lambda q: density * 0.4 * wave[min(int(q * s.SR), n - 1)], dur - 0.02):
        place(out, F4.wet_click(), t0, s.rng.uniform(0.1, 0.5))
    for t0 in s.poisson_times(lambda q: glorps, dur - 0.1):
        place(out, F4.glorp(size), t0, s.rng.uniform(0.3, 0.7))
    sheen = bp(s.pink(n), 1200, 4000) * wave * F4.F3.F2.sticky(n, 60, 0.9) * 0.12
    return lp(hp(out, 200), 6000) + sheen


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(slither_slow(5.6, 1.0), 0.4))
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(slither_slow(5.6, 1.25, 55, 0.9, 0.45), 0.75, 0.3, 0.0, 0.45), 0.5))


# ------------------------------------------------------------ ronde 7: veel zachter en rustiger, met diepe bellen
def deep_bubbles(dur, rate=0.6, size=1.0):
    n = n_of(dur)
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: rate, dur - 0.4):
        f = s.rng.uniform(85, 150) / size
        b = s.bubble(f, s.rng.uniform(0.18, 0.32) * size, 0.8)
        place(out, lp(b, 500), t0, s.rng.uniform(0.6, 1.0))
    return out


def gentle_slime(dur, size=1.0):
    x = slither_slow(dur, size, density=42, glorps=0.5, wave_hz=0.4)
    x = lp(x, 3200)                       # zachter, minder scherp
    b = deep_bubbles(dur, 0.6, size)
    from sfxlib import norm
    return norm(x) + 0.55 * norm(b)


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(gentle_slime(6.0, 1.0), 0.5), lufs=-27)
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(gentle_slime(6.0, 1.2), 0.8, 0.25, 0.0, 0.35), 0.6), lufs=-25)
