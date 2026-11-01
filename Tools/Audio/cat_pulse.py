"""Energie-pulse met brokstukken voor gewone vijanden (gebaseerd op de goedgekeurde power pulse)."""
import numpy as np
import sfxlib as s
from sfxlib import n_of, norm, place, lp, bp, tax, env_points
import cat_a

REG = []


def stone(size=1.0):
    d = 0.08 * size
    n = n_of(d)
    t = tax(d)
    knock = bp(s.white(n), 600 / size, 4000 / size) * np.exp(-t / (0.006 * size))
    body = s.thump(d, 260 / size, 140 / size, 40, 0.012 * size, 0.2, 2000)
    return norm(knock + 0.6 * s.pad(body, n))


def enemy_pulse(v):
    dur = 2.2
    n = n_of(dur)
    tc = 0.25                                  # moment van de ontlading in power_pulse(1.0)
    x = s.pad(norm(cat_a.power_pulse(1.0, 1.8)), n)
    # brokstukken: eerst een golf gruis dat wegspat, dan steentjes die terugvallen
    spray = s.grains(n, s.poisson_times(lambda q: 0 if q < tc else 900 * np.exp(-(q - tc) / 0.15), dur),
                     (0.001, 0.005), (1200, 7000), s.rng.uniform(0.1, 1, 3000))
    rubble = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 30 * np.exp(-((q - (tc + 0.55)) / 0.35) ** 2), dur - 0.1, tc + 0.1):
        place(rubble, stone(s.rng.uniform(0.6, 1.4)), t0, s.rng.uniform(0.3, 1))
    grit = s.grains(n, s.poisson_times(lambda q: 250 * np.exp(-((q - (tc + 0.6)) / 0.4) ** 2), dur, tc),
                    (0.002, 0.01), (500, 4000), s.rng.uniform(0.1, 1, 2000))
    return s.air(x + norm(spray) * 0.25 + norm(rubble) * 0.45 + norm(grit) * 0.2)


REG.append(dict(path="Monsters/Shared/enemy_power_pulse", title="Enemy power pulse (with debris)",
                desc="Energy pulse for regular enemies: a quick charge, the shockwave with its 'wub', then grit spraying out and small stones falling back down.",
                variants=2, fn=enemy_pulse, save_kw=dict(lufs=-12)))
