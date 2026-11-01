"""Ronde 3: laatste aanpassingen na review 2."""
import numpy as np
from scipy.signal import lfilter
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points
import cat_fix as F
from cat_fix import replace, boss_bass, clank, legs, flap, ENTRIES

F.FIXED.clear()
FIXED = F.FIXED


# ------------------------------------------------------------ slijm, opnieuw
# Slijm = gedempte laag-midden 'schlop' (ca. 100-500 Hz), kleverig zuigen/rekken en
# hooguit heel af en toe een klein ploppertje. GEEN tonale belletjes (dat gaf het
# 'borrelen' en het hartmonitor/sonar-gevoel) en geen regelmatig ritme.
def sticky(n, rate_hz=90, depth=0.85):
    """Snelle, onregelmatige amplitude-'kleverigheid' (natte micro-crackle)."""
    m = np.abs(lp(s.rng.standard_normal(n), rate_hz, 2))
    m = (m / (m.max() + 1e-9)) ** 0.7
    return (1 - depth) + depth * m


def schlop(dur=0.25, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    curve = s.curve_of(40, lambda u: (900 - 500 * u) / size + 120)
    body = s.sweep(s.brown(n) * 0.5 + s.pink(n), "low", curve)
    env = env_points(t, [(0, 0), (0.012, 1), (dur * 0.35, 0.55), (dur * 0.5, 0.75), (dur, 0)])
    wet = body * env * sticky(n, 120, 0.9)
    tick = lp(s.white(n), 2500) * env_ad(t, 0.001, 0.01) * 0.3
    return wet + tick


def suction(dur=0.3, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    x = s.sweep(s.pink(n), "band", (350 / size, 900 / size), width=1.2)
    return x * np.sin(np.pi * t / dur) ** 2 * sticky(n, 60, 0.7)


def slime_bed(dur, size=1.0, schlops=1.6, sucks=0.8):
    n = n_of(dur)
    bed = lp(s.pink(n) + 0.5 * s.brown(n), 700 / size) * sticky(n, 70, 0.8) * s.smooth_rand(n, 0.7, 0.35, 1.0)
    out = norm(bed) * 0.45
    for t0 in s.poisson_times(lambda q: schlops, dur - 0.4):
        place(out, norm(schlop(s.rng.uniform(0.18, 0.35) * size, size)), t0, s.rng.uniform(0.5, 1))
    for t0 in s.poisson_times(lambda q: sucks, dur - 0.4):
        place(out, norm(suction(s.rng.uniform(0.2, 0.4), size)), t0, s.rng.uniform(0.3, 0.6))
    return lp(out, 3500 / size ** 0.5)


replace("Monsters/Slug/slug_move_loop", lambda v: s.make_loop(slime_bed(4.6, 1.0, 1.4, 0.8), 0.4))
replace("Monsters/Leech/leech_slime_loop", lambda v: s.make_loop(slime_bed(4.6, 1.5, 1.8, 1.4), 0.4))
replace("Bosses/Slug/slug_move_loop_boss",
        lambda v: s.make_loop(boss_bass(slime_bed(4.6, 1.0, 1.2, 0.8), 0.65, 1.0, 0.05, 1.5), 0.5))
replace("Bosses/Leech/leech_slime_loop_boss",
        lambda v: s.make_loop(boss_bass(slime_bed(4.6, 1.5, 1.6, 1.4), 0.65, 1.0, 0.05, 1.5), 0.5))


def slime_spit(v):
    dur = 1.3
    n = n_of(dur)
    t = tax(dur)
    hork = lp(s.pink(n_of(0.22)), 900) * env_points(tax(0.22), [(0, 0), (0.06, 1), (0.22, 0)]) * sticky(n_of(0.22), 50, 0.6)
    spit = bp(s.white(n), 250, 2000) * env_ad(t, 0.003, 0.05) * sticky(n, 150, 0.8)
    x = np.zeros(n)
    place(x, norm(hork), 0.0, 0.6)
    place(x, norm(spit), 0.18, 0.7)
    place(x, norm(schlop(0.45, 1.3)), 0.2, 1.0)           # de klodder vliegt
    for t0 in [0.55, 0.75, 0.95]:                         # nadruipende spetters
        place(x, norm(schlop(0.15, 1.0)), t0 + s.rng.uniform(-0.05, 0.05), s.rng.uniform(0.25, 0.45))
    return boss_bass(x, 0.85, 0.7, 0.05, 1.2)


replace("Bosses/Special/boss_slime_spit", slime_spit)


# ------------------------------------------------------------ tempo's
replace("Monsters/Ant/ant_walk_loop", lambda v: s.make_loop(legs(3.3, 5, 6, 1800, 4500, 0.0025), 0.25))
replace("Bosses/Ant/ant_walk_loop_boss",
        lambda v: s.make_loop(boss_bass(legs(3.3, 3.2, 6, 1500, 4000, 0.003), 0.6, 0.6, 0.1, 1.2), 0.3))
replace("Monsters/Scorpion/scorpion_walk_loop",
        lambda v: s.make_loop(legs(4.6, 1.3, 8, 900, 2800, 0.005, scrape=0.06), 0.25))
replace("Bosses/Scorpion/scorpion_walk_loop_boss",
        lambda v: s.make_loop(boss_bass(legs(6.2, 1.0, 8, 800, 2500, 0.006, scrape=0.08), 0.62, 0.8, 0.1, 1.4), 0.3))


def periodic_flaps(rate, n_flaps, size, flap_dur=None, var=0.12):
    """Naadloze vleugel-loop: precies n slagen in de loop, staart loopt door naar het begin.
    Zo blijft het ritme gelijk en is er geen hoorbare 'set'-wissel."""
    L = n_of(n_flaps / rate)
    out = np.zeros(L)
    for k in range(n_flaps):
        f = flap(size, 1 - var + var * s.rng.random(), flap_dur)
        start = n_of(k / rate)
        for i in range(0, len(f), L):
            pass
        seg = f
        end = start + len(seg)
        if end <= L:
            out[start:end] += seg
        else:
            out[start:] += seg[:L - start]
            rest = seg[L - start:]
            out[:len(rest)] += rest[:L]
    return out


replace("Monsters/Flying/wings_flapping_loop", lambda v: periodic_flaps(2.2, 8, 0.45))
replace("Monsters/Flying/large_wings_flapping_loop", lambda v: periodic_flaps(0.9, 4, 0.95))
replace("Monsters/Bat/bat_wings_loop", lambda v: periodic_flaps(7, 16, 0.2, flap_dur=0.1, var=0.15))


# ------------------------------------------------------------ boss bat screech: lager
import cat_c  # noqa: E402
replace("Bosses/Bat/bat_screech_boss", lambda v: boss_bass(cat_c.bat_screech(v), factor=0.36, sub=0.9, grit=0.3, shelf=1.6))


# ------------------------------------------------------------ alarm: 'bweep bweep'
def alert(v):
    dur = 1.25
    n = n_of(dur)
    x = np.zeros(n)
    for i in range(3):
        L = n_of(0.3)
        q = np.arange(L) / s.SR
        f = 550 * 2 ** (1.0 * (q / q[-1]) ** 0.7)          # stijgt een octaaf: 'bweep'
        tone = s.osc(f, "square") * 0.6 + np.sin(s.phase_of(f)) + 0.3 * np.sin(s.phase_of(f * 2))
        tone = lp(tone, 4500)
        tone *= np.minimum(1, np.minimum(q / 0.01, (q[-1] - q) / 0.04))
        place(x, tone, 0.4 * i)
    return s.stereo(norm(x), 0.004)


replace("Environment/base_alert", alert)


# ------------------------------------------------------------ zware RC-tank
def rc_tank(v):
    dur = 4.4
    n = n_of(dur)
    t = tax(dur)
    f = 62 * (1 + 0.02 * np.sin(2 * np.pi * 0.4 * t) + 0.01 * (s.smooth_rand(n, 3) - 0.5))
    motor = np.tanh(1.2 * (s.osc(f, "saw") + 0.6 * s.osc(f * 2, "saw") + 0.3 * s.osc(f * 3, "square")))
    motor = lp(motor, 900)
    whine = np.sin(s.phase_of(f * 7.5)) * 0.12                                   # elektromotor
    tread_rate = 11.0
    clatter = np.zeros(n)
    k = 0
    while k / tread_rate < dur - 0.1:
        place(clatter, clank(0.06, 400, 1500, 0.25), k / tread_rate + s.rng.normal(0, 0.004), s.rng.uniform(0.5, 1))
        k += 1
    grind = bp(s.brown(n), 60, 500) * (0.75 + 0.25 * np.sin(2 * np.pi * tread_rate * t))
    gravel = s.grains(n, s.poisson_times(lambda q: 120, dur), (0.002, 0.008), (500, 2500), s.rng.uniform(0.2, 1, 1000))
    x = norm(motor) * 0.7 + whine + norm(clatter) * 0.45 + norm(grind) * 0.5 + norm(gravel) * 0.15
    return s.make_loop(lp(x, 5000), 0.3)


replace("Environment/rc_cart_loop", rc_tank)
ENTRIES["Environment/rc_cart_loop"]["title"] = "RC tank driving on a paved road (loop)"
ENTRIES["Environment/rc_cart_loop"]["desc"] = "Heavy little RC tank: deep electric motor, clattering tracks and a gritty grind on the road. Seamless loop."


# ------------------------------------------------------------ toren kapot = kleine explosie + schroot
def tower_break(v):
    dur = 2.2
    n = n_of(dur)
    x = np.zeros(n)
    place(x, s.explosion(size=0.45, distance=0.4, metal=0.0, dur=1.8), 0.0, 1.0)
    place(x, F.scrap(0), 0.18, 0.7)
    place(x, F.scrap(1), 0.45, 0.5)
    return s.air(x)


replace("Environment/tower_break", tower_break)


# ------------------------------------------------------------ hero valt: zonder zanderig gruis
def hero_fall(v):
    dur = 1.1
    n = n_of(dur)
    x = np.zeros(n)
    place(x, s.thump(0.45, 130, 48, 18, 0.09, 0.6, 500), 0.0, 1.0)
    place(x, s.thump(0.35, 150, 55, 22, 0.05, 0.5, 600), 0.16, 0.6)
    place(x, clank(0.2, 300, 1200, 0.4), 0.02, 0.35)
    place(x, clank(0.2, 350, 1300, 0.3), 0.18, 0.2)
    rustle = lp(F.cat_a.suit_rustle(0.8, 0.6), 2500)
    place(x, rustle, 0.04, 0.6)
    return s.air(lp(x, 4000))


replace("Hero/hero_fall", hero_fall)


# ------------------------------------------------------------ hero sterft: opnieuw
# Echte stem nadoen met synthese: glottale pulsen (Rosenberg-model) door een
# vocal tract van in serie geschakelde formant-resonatoren, met jitter/shimmer,
# ademruis en 'vocal fry' (kraak) aan het eind. Opbouw: harde 'uh!' (geraakt),
# kort happen naar adem, een dalende, brekende 'aaah' en uitademen.
def glottal(pitch, open_q=0.6, speed_q=2.5):
    n = len(pitch)
    ph = np.cumsum(pitch) / s.SR
    frac = ph - np.floor(ph)
    tp = open_q * speed_q / (1 + speed_q)
    tn = open_q / (1 + speed_q)
    g = np.where(frac < tp, 0.5 * (1 - np.cos(np.pi * frac / tp)),
                 np.where(frac < tp + tn, np.cos(np.pi * (frac - tp) / (2 * tn)), 0.0))
    return np.diff(g, prepend=0) * s.SR / 1000  # afgeleide = straling van de mond


def resonator(x, f, bw):
    r = np.exp(-np.pi * bw / s.SR)
    c = -r * r
    b = 2 * r * np.cos(2 * np.pi * f / s.SR)
    a0 = 1 - b - c
    return lfilter([a0], [1, -b, -c], x)


def tract(src, formants):
    y = src
    for f, bw in formants:
        y = resonator(y, f, bw)
    return y


AH = [(730, 90), (1090, 110), (2440, 160), (3400, 250)]
UH = [(520, 80), (1190, 100), (2390, 160), (3400, 250)]
OH = [(570, 80), (840, 100), (2410, 160), (3400, 250)]


def vocal(dur, pitch_pts, formants, breath=0.25, fry_from=None, jitter=0.03, shimmer=0.15, amp_pts=None):
    n = n_of(dur)
    t = tax(dur)
    p = env_points(t, pitch_pts)
    p *= 1 + jitter * (s.smooth_rand(n, 40) - 0.5) * 2
    if fry_from is not None:  # krakende, onregelmatige lage stem aan het eind
        fry = np.clip((t - fry_from) / 0.15, 0, 1)
        p = p * (1 - fry) + fry * (55 + 25 * s.smooth_rand(n, 25))
    src = glottal(p)
    src *= 1 + shimmer * (s.smooth_rand(n, 30) - 0.5) * 2
    asp = hp(s.white(n), 400) * breath * np.abs(src).mean() * 6
    y = tract(src + asp, formants)
    if amp_pts:
        y *= env_points(t, amp_pts)
    return y


def inhale(dur=0.25):
    n = n_of(dur)
    t = tax(dur)
    x = s.sweep(s.white(n), "band", (1400, 2600), width=1.0)
    return x * np.sin(np.pi * t / dur) ** 1.5


def exhale(dur=0.45):
    n = n_of(dur)
    t = tax(dur)
    x = tract(s.white(n), [(700, 300), (1200, 300), (2500, 400)])
    return x * env_points(t, [(0, 0), (0.04, 1), (dur, 0)])


def hero_death(v):
    if v == 0:   # geraakt 'uh!' - happen naar adem - dalende 'aaah' met kraak - uitademen
        dur = 1.9
        x = np.zeros(n_of(dur))
        hit = vocal(0.18, [(0, 150), (0.05, 175), (0.18, 140)], UH, 0.35,
                    amp_pts=[(0, 0), (0.015, 1), (0.12, 0.7), (0.18, 0)])
        place(x, norm(hit), 0.0, 0.9)
        place(x, norm(inhale(0.2)), 0.22, 0.25)
        groan = vocal(1.0, [(0, 190), (0.12, 205), (0.5, 160), (0.85, 120), (1.0, 100)], AH, 0.3, fry_from=0.7,
                      amp_pts=[(0, 0), (0.06, 1), (0.5, 0.8), (0.85, 0.45), (1.0, 0)])
        place(x, norm(groan), 0.45, 1.0)
        place(x, norm(exhale(0.45)), 1.42, 0.3)
    else:        # korter: 'ugh' - 'uhhh' wegzakkend
        dur = 1.4
        x = np.zeros(n_of(dur))
        hit = vocal(0.22, [(0, 130), (0.06, 150), (0.22, 115)], UH, 0.4,
                    amp_pts=[(0, 0), (0.012, 1), (0.15, 0.6), (0.22, 0)])
        place(x, norm(hit), 0.0, 1.0)
        groan = vocal(0.75, [(0, 140), (0.3, 120), (0.75, 85)], OH, 0.35, fry_from=0.45,
                      amp_pts=[(0, 0), (0.05, 0.9), (0.4, 0.6), (0.75, 0)])
        place(x, norm(groan), 0.3, 0.85)
        place(x, norm(exhale(0.35)), 1.0, 0.3)
    # licht gedempt door de helm
    return bp(x, 90, 6000) + 0.15 * s.reson(x, 1600, 3)


replace("Hero/hero_death", hero_death)
ENTRIES["Hero/hero_death"]["title"] = "Hero dies"
ENTRIES["Hero/hero_death"]["desc"] = "Rebuilt from scratch with a proper voice model: a hit 'uh!', a gasp, a falling, cracking groan and a last breath."
