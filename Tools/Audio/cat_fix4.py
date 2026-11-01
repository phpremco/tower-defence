"""Ronde 5: slak (opnieuw), slijmspuug en alarm."""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points
import cat_fix3 as F3
from cat_fix import replace, boss_bass, ENTRIES
import cat_fix as F

F.FIXED.clear()
FIXED = F.FIXED


# ------------------------------------------------------------ slijm v5
# Mac & cheese roeren / slijm dat over de grond glibbert: GEEN lage ruismassa (dat klonk als
# een steen die gesleept wordt). Wel: een doorlopende stroom glanzende natte smakjes en
# kliks in het midden/hoog, korte 'glorp'-jes, en golven in de dichtheid, zonder pauzes.
def wet_click():
    L = n_of(0.006)
    k = hp(s.white(L), 900) * np.exp(-np.arange(L) / (L / 5))
    return s.reson(k, s.rng.uniform(1800, 3200), 6) * 0.6 + k * 0.4


def smack(size=1.0):
    d = s.rng.uniform(0.012, 0.035) * size
    n = n_of(d)
    t = tax(d)
    exc = s.white(n) * env_points(t, [(0, 0), (0.002, 1), (d, 0)])
    f1 = s.rng.uniform(450, 950) / size
    f2 = s.rng.uniform(1300, 2700) / size
    y = F3.F2.resonator(exc, f1, 140) + 0.8 * F3.F2.resonator(exc, f2, 220)
    return norm(y)


def glorp(size=1.0):
    d = s.rng.uniform(0.035, 0.07) * size
    f = s.rng.uniform(220, 420) / size
    b = s.bubble(f, d, 1.4)
    return norm(lp(b, 1500)) * 0.8


def slither(dur, size=1.0, density=110, glorps=1.5):
    n = n_of(dur)
    wave = s.smooth_rand(n, 1.1, 0.35, 1.0)            # golven in de beweging, geen gaten
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: density * wave[min(int(q * s.SR), n - 1)], dur - 0.05):
        place(out, smack(size), t0, s.rng.uniform(0.2, 1.0) ** 1.3)
    for t0 in s.poisson_times(lambda q: density * 0.5 * wave[min(int(q * s.SR), n - 1)], dur - 0.02):
        place(out, wet_click(), t0, s.rng.uniform(0.1, 0.6))
    for t0 in s.poisson_times(lambda q: glorps, dur - 0.1):
        place(out, glorp(size), t0, s.rng.uniform(0.3, 0.7))
    sheen = bp(s.pink(n), 1200, 4000) * wave * F3.F2.sticky(n, 80, 0.9) * 0.15   # glanzend nat oppervlak
    return lp(hp(out, 200), 6000) + sheen


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(slither(4.8, 1.0), 0.4))
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(slither(4.8, 1.25, 95, 1.5), 0.72, 0.6, 0.0, 1.0), 0.5))


# ------------------------------------------------------------ slijmspuug: helemaal opnieuw
def slime_spit(v):
    """Rochel + spuug, een natte klodder die door de lucht vliegt en met een zware, slijmerige
    smak neerkomt (veel bas), waarna een paar slijmbellen knappen."""
    dur = 1.7
    n = n_of(dur)
    t = tax(dur)
    x = np.zeros(n)
    hork = slither(0.25, 1.4, 260, 0) * env_points(tax(0.25), [(0, 0), (0.06, 1), (0.25, 0)])
    place(x, norm(hork), 0.0, 0.6)
    place(x, norm(bp(s.white(n_of(0.08)), 400, 3000) * np.exp(-np.arange(n_of(0.08)) / s.SR / 0.02)), 0.2, 0.6)
    fly = slither(0.3, 1.2, 60, 0) * env_points(tax(0.3), [(0, 0.2), (0.3, 0.6)])
    place(x, norm(fly), 0.22, 0.35)
    hit = 0.52
    splat = slither(0.35, 1.5, 450, 0) * env_points(tax(0.35), [(0, 1), (0.06, 0.8), (0.35, 0)])
    place(x, norm(splat), hit, 1.0)
    L = n_of(0.5)
    q = np.arange(L) / s.SR
    bass = np.sin(s.phase_of(s.glide(q, 120, 45, 14))) * np.exp(-q / 0.12) * (1 - np.exp(-q / 0.003))
    place(x, bass, hit, 1.2)
    for k, t0 in enumerate([hit + 0.25, hit + 0.45, hit + 0.62]):          # bellen die knappen
        L2 = n_of(0.12)
        q2 = np.arange(L2) / s.SR
        inflate = np.sin(s.phase_of(np.full(L2, [160, 210, 130][k]) * (1 + 1.5 * q2 / q2[-1]))) * (q2 / q2[-1]) ** 2
        place(x, lp(inflate, 900) * 0.6, t0 + s.rng.uniform(-0.03, 0.03), 0.6)
        place(x, s.click(0.02, 600, 5000, 0.002), t0 + 0.12, 0.7)          # 'pop'
    x = norm(x)
    return norm(x + 0.9 * lp(x, 200))


replace("Bosses/Special/boss_slime_spit", slime_spit)


# ------------------------------------------------------------ alarm: beetje echo
def alert(v):
    x = F3.alert(v)
    mono = x[:, 0]
    y = s.echo(mono, [0.16, 0.32, 0.48], [0.35, 0.18, 0.08], lowpass=3000)
    left = y
    right = np.concatenate([np.zeros(n_of(0.012)), y])[: len(y)]
    return np.stack([left, right * 0.95 + left * 0.05], 1)


replace("Environment/base_alert", alert)
