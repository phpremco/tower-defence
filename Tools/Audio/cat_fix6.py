"""Ronde 8: slak volledig opnieuw, schoon en natuurlijk.
Lessen: geen vervorming (geen tanh/grit, geen compressor, geen snelle 'kleverige' amplitude-
modulatie die als gekraak klinkt), alles zacht gefilterd, rustige bewegingen, diepe bellen."""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp
import cat_fix5 as F5
from cat_fix import replace, ENTRIES
import cat_fix as F

F.FIXED.clear()
FIXED = F.FIXED


def soft_plip(size=1.0):
    d = s.rng.uniform(0.03, 0.07) * size
    n = n_of(d)
    t = tax(d)
    exc = s.pink(n) * np.sin(np.pi * t / d) ** 2
    y = F5.F4.F3.F2.resonator(exc, s.rng.uniform(450, 1000) / size, 350)
    return lp(norm(y), 2200 / size)


def peel(size=1.0):
    d = s.rng.uniform(0.12, 0.2)
    n = n_of(d)
    t = tax(d)
    env = np.clip(t / (d * 0.8), 0, 1) ** 2 * np.clip((d - t) / (d * 0.2), 0, 1)
    return lp(bp(s.pink(n), 500 / size, 1600 / size) * env, 2500)


def clean_slime(dur, size=1.0):
    n = n_of(dur)
    swell = s.smooth_rand(n, 0.5, 0.25, 1.0) ** 1.5
    slide = lp(bp(s.pink(n), 180 / size, 1300 / size), 1500 / size) * swell
    out = norm(slide) * 0.5
    for t0 in s.poisson_times(lambda q: 6 * swell[min(int(q * s.SR), n - 1)] + 1, dur - 0.1):
        place(out, soft_plip(size), t0, s.rng.uniform(0.2, 0.6))
    for t0 in s.poisson_times(lambda q: 0.8, dur - 0.3):
        place(out, norm(peel(size)), t0, s.rng.uniform(0.15, 0.3))
    out += 0.6 * norm(F5.deep_bubbles(dur, 0.55, size))
    return lp(hp(out, 60), 3000)


def boss_clean(x, factor=0.75):
    y = s.pitch_shift(x, factor)          # groter = lager en trager, verder geen vervorming
    return norm(y + 0.35 * lp(y, 220))


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(clean_slime(6.0, 1.0), 0.6), lufs=-27, comp=False)
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_clean(clean_slime(6.0, 1.15)), 0.8), lufs=-25, comp=False)
