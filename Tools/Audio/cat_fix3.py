"""Ronde 4: laatste aanpassingen na review 3."""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points
import cat_fix2 as F2
from cat_fix import replace, boss_bass, legs, ENTRIES
import cat_fix as F

F.FIXED.clear()
FIXED = F.FIXED


# ------------------------------------------------------------ slijm v3 (slak)
# Wat 'slijm' herkenbaar maakt (zoals in slime-ASMR): een knisperende laag van piepkleine,
# droge-natte knapjes (luchtzakjes die openbreken), plus korte 'squish'-geluidjes waarbij
# twee resonanties snel bewegen (zoals een natte mond), en af en toe kleverig loslaten.
# Geen tonale belletjes en geen enkele zwiepende resonantie (dat klonk als sonar).
def crackle_bursts(dur, rate=3.0, density=900):
    n = n_of(dur)
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: rate, dur - 0.3):
        bl = s.rng.uniform(0.08, 0.3)
        tt = s.poisson_times(lambda q: density * np.sin(np.pi * min(q / bl, 1)) ** 2, bl)
        for c in tt:
            L = n_of(s.rng.uniform(0.0003, 0.0012))
            k = s.white(L) * np.exp(-np.arange(L) / (L / 3 + 1))
            place(out, k, t0 + c, s.rng.uniform(0.1, 1) ** 1.5)
    return bp(out, 900, 7000)


def squish(dur=0.12, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    exc = s.pink(n) * env_points(t, [(0, 0), (0.008, 1), (dur * 0.6, 0.5), (dur, 0)])
    f1a, f1b = s.rng.uniform(300, 700) / size, s.rng.uniform(250, 650) / size
    f2a, f2b = s.rng.uniform(900, 2000) / size, s.rng.uniform(800, 2200) / size
    blocks = 30
    y = np.zeros(n)
    edges = np.linspace(0, n, blocks + 1, dtype=int)
    for i in range(blocks):
        u = i / (blocks - 1)
        seg = exc[edges[i]:edges[i + 1]]
        if not len(seg):
            continue
        f1 = f1a + (f1b - f1a) * u
        f2 = f2a + (f2b - f2a) * u
        y[edges[i]:edges[i + 1]] = F2.resonator(seg, f1, 120) + 0.6 * F2.resonator(seg, f2, 180)
    pop = np.zeros(n)
    place(pop, lp(s.white(n_of(0.004)), 3000) * np.hanning(n_of(0.004)), dur * s.rng.uniform(0.5, 0.9), 0.6)
    return norm(y) + pop * 0.5


def slime_v3(dur, size=1.0):
    n = n_of(dur)
    out = norm(crackle_bursts(dur, 3.2, 1100)) * 0.55
    for t0 in s.poisson_times(lambda q: 5.0, dur - 0.2):
        place(out, squish(s.rng.uniform(0.07, 0.16) * size, size), t0, s.rng.uniform(0.35, 1))
    for t0 in s.poisson_times(lambda q: 0.7, dur - 0.4):
        place(out, norm(F2.suction(s.rng.uniform(0.2, 0.35), size)), t0, s.rng.uniform(0.25, 0.45))
    bed = lp(s.pink(n), 900 / size) * F2.sticky(n, 60, 0.85) * s.smooth_rand(n, 0.6, 0.2, 0.8)
    return out + norm(bed) * 0.2


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(slime_v3(4.6, 1.0), 0.4))
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(slime_v3(4.6, 1.0), 0.68, 0.9, 0.05, 1.4), 0.5))


# ------------------------------------------------------------ slijmspuug: met lage 'blob' bij inslag
def slime_spit(v):
    x = F2.slime_spit(v)            # al bossified (vorige versie)
    n = len(x) + n_of(0.6)
    x = s.pad(x, n)
    hit_t = 0.32
    blob = s.bubble([95, 110][v], 0.4, 0.7) * 1.0               # zware, lage 'blob'
    blob2 = s.bubble([140, 160][v], 0.25, 0.9) * 0.6
    sq = squish(0.18, 1.6)
    place(x, norm(lp(blob, 600)), hit_t, 0.9)
    place(x, norm(lp(blob2, 900)), hit_t + 0.12, 0.6)
    place(x, sq, hit_t, 0.5)
    return norm(x)


replace("Bosses/Special/boss_slime_spit", slime_spit)


# ------------------------------------------------------------ alarm: iets meer bas
def alert(v):
    x = F2.alert(v)
    mono = x[:, 0]
    n = len(mono)
    out = np.zeros(n)
    for i in range(3):
        L = n_of(0.3)
        q = np.arange(L) / s.SR
        f = 275 * 2 ** (1.0 * (q / q[-1]) ** 0.7)           # octaaf lager, meebuigend
        sub = np.sin(s.phase_of(f)) * np.minimum(1, np.minimum(q / 0.01, (q[-1] - q) / 0.04))
        place(out, sub, 0.4 * i)
    y = norm(mono) + 0.7 * norm(out)
    y = y + 0.5 * lp(y, 350)
    return s.stereo(norm(y), 0.004)


replace("Environment/base_alert", alert)


# ------------------------------------------------------------ tempo's
replace("Monsters/Ant/ant_walk_loop", lambda v: s.make_loop(legs(4.4, 2.5, 6, 1800, 4500, 0.0025), 0.25))
replace("Bosses/Ant/ant_walk_loop_boss",
        lambda v: s.make_loop(boss_bass(legs(4.4, 2.4, 6, 1500, 4000, 0.003), 0.6, 0.6, 0.1, 1.2), 0.3))
replace("Monsters/Scorpion/scorpion_walk_loop",
        lambda v: s.make_loop(legs(5.6, 0.9, 8, 900, 2800, 0.005, scrape=0.05), 0.25))
replace("Monsters/Bat/bat_wings_loop", lambda v: F2.periodic_flaps(4.5, 10, 0.2, flap_dur=0.1, var=0.2), lufs=-24)


# ------------------------------------------------------------ slijm v4: 'roeren in een pan mac & cheese'
# Ritmische roerhalen (~1,3 per seconde). Elke haal zwelt aan en bestaat uit heel veel
# korte, kleverige 'smakjes' (korte natte squishes van 10-40 ms) over een dikke, lage
# massa; aan het eind van de haal een kleverig loslaten/trekken.
def stir_stroke(dur=0.7, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    env = np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 1.3
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 25 + 110 * np.sin(np.pi * min(q / dur, 1)) ** 1.3, dur - 0.04):
        sm = squish(s.rng.uniform(0.012, 0.04) * size, size)
        place(out, sm, t0, s.rng.uniform(0.25, 1) * (0.3 + 0.7 * env[min(n_of(t0), n - 1)]))
    mass = lp(s.brown(n) * 0.6 + s.pink(n), 450 / size) * env * F2.sticky(n, 45, 0.7)
    pull = np.zeros(n)
    place(pull, norm(F2.suction(0.22, size)), dur * 0.7, 0.4)
    return norm(lp(out, 3500 / size)) + norm(mass) * 0.45 + pull


def mac_stir(dur, rate=1.3, size=1.0):
    n = n_of(dur)
    out = np.zeros(n)
    t = 0.0
    while t < dur - 0.2:
        place(out, stir_stroke(s.rng.uniform(0.55, 0.8) / rate * 1.1, size), t, s.rng.uniform(0.7, 1.0))
        t += (1 / rate) * s.rng.uniform(0.85, 1.15)
    return out


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(mac_stir(4.8, 1.3, 1.0), 0.4))
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(mac_stir(4.8, 1.3, 1.0), 0.68, 0.9, 0.05, 1.4), 0.5))
