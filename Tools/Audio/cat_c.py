"""Monsters, hun boss-versies en de extra boss-geluiden."""
import numpy as np
import sfxlib as s
from sfxlib import sound, tax, n_of, norm, place, lp, hp, bp, env_ad, env_points, mix

BOSS_LUFS_BONUS = 2.0


def monster(creature, name, title, desc, variants=1, lufs=-14, loop=False, loop_xfade=0.3,
            boss=None, comp=True):
    """Registreer een monstergeluid + automatisch een grotere boss-versie.
    boss: dict(factor, sub, grit) of False om geen boss-versie te maken."""
    def deco(fn):
        def base(v):
            x = fn(v)
            return s.make_loop(x, loop_xfade) if loop else x
        s.REGISTRY.append(dict(path=f"Monsters/{creature}/{name}", title=title, desc=desc,
                               variants=variants, fn=base,
                               save_kw=dict(lufs=lufs, loop=loop, comp=comp)))
        if boss is not False:
            b = dict(factor=0.7, sub=0.5, grit=0.25)
            b.update(boss or {})

            def bossfn(v):
                y = s.bossify(fn(v), **b)
                return s.make_loop(y, loop_xfade / b["factor"]) if loop else y
            s.REGISTRY.append(dict(path=f"Bosses/{creature}/{name}_boss", title=f"BOSS – {title}",
                                   desc=f"Bigger, lower and heavier boss version of: {desc}",
                                   variants=variants, fn=bossfn,
                                   save_kw=dict(lufs=lufs + BOSS_LUFS_BONUS, loop=loop, comp=comp)))
        return fn
    return deco


def chitin_clicks(n, rate, lo, hi, dur_lo=0.0008, dur_hi=0.003, amp_pow=1.0):
    dur = n / s.SR
    tt = s.poisson_times(lambda q: rate, dur)
    return s.grains(n, tt, (dur_lo, dur_hi), (lo, hi), s.rng.uniform(0.1, 1, len(tt)) ** amp_pow, q=1.4)


def gait_clicks(n, step_rate, legs, lo, hi, spread=0.02, size=1.0):
    """Ritmisch tikken van meerdere poten (insecten)."""
    out = np.zeros(n)
    dur = n / s.SR
    t = 0.0
    while t < dur:
        for leg in range(legs):
            tt = t + leg / legs / step_rate + s.rng.normal(0, spread)
            if 0 < tt < dur - 0.01:
                c = s.click(0.012 * size, lo, hi, 0.0012 * size)
                c = c + 0.5 * s.reson(c, s.rng.uniform(lo, hi), 6)
                place(out, c, tt, s.rng.uniform(0.3, 1))
        t += 1 / step_rate
    return out


# ============================================================ SLAK
@monster("Slug", "slug_move_loop", "Slug moving (loop)",
         "Wet, snotty sliding with squelches and little slime bubbles. Seamless loop.",
         lufs=-20, loop=True, boss=dict(factor=0.65, sub=0.6, grit=0.15))
def slug_move(v):
    dur = 4.6
    n = n_of(dur)
    slide = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 150), np.arange(n), 500 + 900 * s.smooth_rand(n, 1.5)), width=0.7)
    slide *= s.smooth_rand(n, 1.2, 0.4, 1.0)
    sq = np.zeros(n)
    for t0 in np.arange(0.1, dur - 0.5, 0.55) + s.rng.uniform(-0.1, 0.1, len(np.arange(0.1, dur - 0.5, 0.55))):
        place(sq, s.squelch_sound(0.35, 0.5), t0, s.rng.uniform(0.5, 1))
    pops = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 6, dur - 0.1):
        place(pops, s.bubble(s.rng.uniform(500, 1400), 0.04), t0, s.rng.uniform(0.2, 0.6))
    return norm(slide) * 0.6 + norm(sq) * 0.8 + norm(pops) * 0.3


@sound("Bosses/Slug/slug_boss_growl", "BOSS – Slug slimy growl",
       "Deep gurgling growl, as if through a throat full of slime: low rough voice with bubbling.",
       variants=2, lufs=-11)
def slug_growl(v):
    dur = 2.2
    n = n_of(dur)
    t = tax(dur)
    pitch = env_points(t, [(0, 55), (0.3, 75 + 5 * v), (1.4, 68), (2.2, 48)])
    vc = env_points(t, [(0, 0), (1.0, 0.5), (2.2, 1)])
    g = s.voice(dur, pitch, [s.VOWEL_O, s.VOWEL_U], rough=0.6, breath=0.3, sub=0.6, growl_hz=35, vowel_curve=vc)
    gurgle = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 35, dur - 0.1):
        place(gurgle, s.bubble(s.rng.uniform(150, 500), s.rng.uniform(0.03, 0.08)), t0, s.rng.uniform(0.3, 1))
    wet = s.squelch_sound(dur, 1.0) * 0.3
    x = norm(g) * (0.6 + 0.4 * s.rumble_mod(n, 15, 0.8)) + norm(gurgle) * 0.45 + s.pad(wet, n)
    return x * env_points(t, [(0, 0), (0.15, 1), (1.8, 0.8), (2.2, 0)])


# ============================================================ VLEUGELS
def flaps(dur, rate, size, jitter=0.03):
    n = n_of(dur)
    out = np.zeros(n)
    t = 0.0
    while t < dur:
        place(out, s.wing_flap(size), t, s.rng.uniform(0.8, 1.0))
        t += 1 / rate + s.rng.normal(0, jitter / rate)
    return out


@monster("Flying", "wings_flapping_loop", "Wings flapping (loop)",
         "Medium creature flapping its wings steadily. Seamless loop.",
         lufs=-18, loop=True, boss=dict(factor=0.7, sub=0.5, grit=0.1))
def wings(v):
    return flaps(3.4, 5.5, 0.4)


@monster("Flying", "large_wings_flapping_loop", "Large wings flapping (loop)",
         "Big, slow, powerful wing beats pushing a lot of air. Seamless loop.",
         lufs=-16, loop=True, boss=dict(factor=0.75, sub=0.6, grit=0.1))
def large_wings(v):
    return flaps(4.4, 1.8, 0.95, 0.02)


# ============================================================ VOETSTAPPEN
@monster("Wolf", "wolf_step", "Wolf-like creature footstep",
         "Light, quick paw with claws ticking on the ground.",
         variants=4, lufs=-19, comp=False, boss=dict(factor=0.72, sub=0.6, grit=0.1))
def wolf_step(v):
    return s.footstep(0.3, "ground", claws=0.9, dur=0.22)


@monster("DinoSoldier", "dino_soldier_step", "Dino soldier footstep",
         "Two-legged, fairly heavy step with a claw scrape and a leathery creak.",
         variants=4, lufs=-17, comp=False, boss=dict(factor=0.7, sub=0.7, grit=0.15))
def dino_step(v):
    st = s.footstep(0.65, "gravel", claws=0.6, dur=0.45)
    n = len(st)
    t = tax(0.45)
    scrape = s.sweep(s.white(n), "band", (5000, 2000), width=0.4) * env_points(t, [(0, 0), (0.03, 0), (0.06, 1), (0.16, 0)])
    creak = bp(s.osc(s.glide(t, 140, 110, 10), "saw"), 300, 1200) * env_points(t, [(0, 0), (0.02, 0.6), (0.12, 0)])
    return st + norm(scrape) * 0.12 + norm(creak) * 0.08


def shell_knock(f, dur=0.12):
    """Holle klop op een pantserplaat (schildpad/gordeldier)."""
    c = s.click(dur, 300, 4000, 0.004)
    return s.reson(c, f, 8) * 2 + c * 0.5


@monster("Armadillo", "armadillo_step", "Armadillo creature footstep",
         "Slow four-legged step: a soft heavy paw, and the armour plates knocking together.",
         variants=4, lufs=-17, comp=False, boss=dict(factor=0.7, sub=0.7, grit=0.15))
def armadillo_step(v):
    dur = 0.6
    st = s.pad(s.footstep(0.6, "ground", claws=0.3, dur=0.4), n_of(dur))
    for k in range(3):
        place(st, shell_knock(s.rng.uniform(600, 1300)), 0.03 + 0.04 * k + s.rng.uniform(0, 0.02), 0.25)
    return st


def roll_loop(dur, rate, plates=6, weight=0.5):
    n = n_of(dur)
    t = tax(dur)
    out = np.zeros(n)
    k = 0
    step = 1 / (rate * plates)
    tt = 0.0
    while tt < dur:
        f = 700 + 300 * np.sin(k * 1.7)
        place(out, shell_knock(f * (1.2 - 0.5 * weight)), tt + s.rng.normal(0, step * 0.08), 0.6 + 0.4 * (k % plates == 0))
        place(out, s.thump(0.08, 160 - 60 * weight, 70, 40, 0.02, 0.3), tt, 0.4 * weight)
        tt += step
        k += 1
    grind = bp(s.brown(n), 60, 500) * (0.7 + 0.3 * np.sin(2 * np.pi * rate * t))
    grit = s.grains(n, s.poisson_times(lambda q: 200, dur), (0.001, 0.005), (900, 5000), np.ones(2000))
    return norm(out) + norm(grind) * (0.4 + 0.4 * weight) + norm(grit) * 0.15


@monster("Armadillo", "armadillo_roll_loop", "Armadillo rolled up, rolling (loop)",
         "Curled-up creature rolling along: armour rings clacking in rhythm over a grinding roll. Replaces footsteps.",
         lufs=-16, loop=True, boss=dict(factor=0.72, sub=0.6, grit=0.15))
def armadillo_roll(v):
    return roll_loop(3.3, 2.4, 6, 0.5)


# ============================================================ MIEREN
@monster("Ant", "ant_walk_loop", "Ants walking (loop)",
         "Fine, dry skittering of tiny chitin legs. Seamless loop.",
         lufs=-23, loop=True, boss=dict(factor=0.6, sub=0.3, grit=0.1))
def ant_walk(v):
    dur = 3.3
    n = n_of(dur)
    x = gait_clicks(n, 9, 6, 3000, 9000, 0.01, 0.6) + norm(chitin_clicks(n, 300, 4000, 11000)) * 0.4
    shuffle = bp(s.pink(n), 2000, 8000) * s.smooth_rand(n, 3, 0.3, 1) * 0.15
    return norm(x) + norm(shuffle) * 0.2


@monster("Ant", "ant_screech", "Ant screech (dying)",
         "Small, high chittering shriek of a dying ant.",
         variants=2, lufs=-14, boss=dict(factor=0.6, sub=0.4, grit=0.3))
def ant_screech(v):
    dur = 0.65
    n = n_of(dur)
    t = tax(dur)
    x = s.screech(dur, 2300 + 200 * v, 1500, rough=0.6, formant_scale=1.8, harsh=0.5)
    chitter = 0.4 + 0.6 * (np.sin(2 * np.pi * 55 * t) > 0)
    return lp(x * chitter, 12000)


# ============================================================ SCHORPIOEN
@monster("Scorpion", "scorpion_walk_loop", "Scorpion walking (loop)",
         "Eight legs clicking on hard ground with a dry sandy shuffle. Seamless loop.",
         lufs=-20, loop=True, boss=dict(factor=0.65, sub=0.5, grit=0.15))
def scorpion_walk(v):
    dur = 3.3
    n = n_of(dur)
    x = gait_clicks(n, 4.5, 8, 1500, 6000, 0.012, 1.2)
    sand = bp(s.pink(n), 1500, 7000) * s.smooth_rand(n, 4, 0.2, 1)
    return norm(x) + norm(sand) * 0.2


@monster("Scorpion", "scorpion_acid_shot", "Scorpion fires acid",
         "Wet 'ptuh' spit of acid flying out, with a gurgle and a short fizz.",
         variants=2, lufs=-14, boss=dict(factor=0.7, sub=0.4, grit=0.2))
def scorpion_acid(v):
    dur = 0.9
    n = n_of(dur)
    t = tax(dur)
    spit = bp(s.white(n), 600, 4000) * env_ad(t, 0.003, 0.05)
    liquid = s.squelch_sound(0.4, 0.4)
    fly = s.sweep(s.pink(n), "band", (2500, 900), width=0.8) * env_points(t, [(0, 0), (0.05, 0.8), (0.45, 0)])
    fizz = s.grains(n, s.poisson_times(lambda q: 1200 * np.exp(-q / 0.3), dur, 0.05), (0.0003, 0.002), (3000, 11000),
                    s.rng.uniform(0.05, 1, 3000) ** 2)
    return norm(spit) * 0.8 + s.pad(norm(liquid), n) * 0.6 + norm(fly) * 0.4 + norm(fizz) * 0.25


@monster("Scorpion", "scorpion_screech", "Scorpion screech (dying)",
         "Hissing, rattling shriek with a dry chitin clatter.",
         variants=2, lufs=-13, boss=dict(factor=0.65, sub=0.5, grit=0.3))
def scorpion_screech(v):
    dur = 0.9
    n = n_of(dur)
    t = tax(dur)
    x = s.screech(dur, 1300 + 100 * v, 650, rough=0.85, formant_scale=1.2, harsh=0.6)
    hiss = bp(s.white(n), 2500, 9000) * env_points(t, [(0, 0), (0.05, 1), (0.7, 0.6), (0.9, 0)])
    rattle = chitin_clicks(n, 160, 1500, 6000) * env_points(t, [(0, 1), (0.6, 0.6), (0.9, 0)])
    return norm(x) + norm(hiss) * 0.35 + norm(rattle) * 0.3


# ============================================================ IJSKEVER
@monster("IceBeetle", "ice_beetle_walk_loop", "Ice beetle walking (loop)",
         "Chitin legs clicking over crunchy ice and frost. Seamless loop.",
         lufs=-20, loop=True, boss=dict(factor=0.65, sub=0.5, grit=0.1))
def beetle_walk(v):
    dur = 3.3
    n = n_of(dur)
    x = gait_clicks(n, 4, 6, 1800, 7000, 0.015, 1.0)
    ice = s.grains(n, s.poisson_times(lambda q: 250, dur), (0.0005, 0.003), (4000, 12000), s.rng.uniform(0.1, 1, 2000), q=1.3)
    return norm(x) + norm(ice) * 0.35


def crystal_rings(n, count, t0, t1, f_lo=2500, f_hi=7000, amp=0.4):
    out = np.zeros(n)
    for _ in range(count):
        f = s.rng.uniform(f_lo, f_hi)
        place(out, s.metal_ring(0.7, [f, f * 1.43, f * 2.11], [0.25, 0.15, 0.08], [1, .6, .3], 0.0005),
              s.rng.uniform(t0, t1), s.rng.uniform(0.3, 1) * amp)
    return out


@monster("IceBeetle", "ice_beetle_screech", "Ice beetle screech (dying)",
         "Brittle, glassy shriek with crystalline ringing.",
         variants=2, lufs=-13, boss=dict(factor=0.65, sub=0.5, grit=0.25))
def beetle_screech(v):
    dur = 0.9
    n = n_of(dur)
    x = s.screech(dur, 1700 + 150 * v, 900, rough=0.5, formant_scale=1.5, harsh=0.35, vib=0.03)
    return norm(x) + norm(crystal_rings(n, 6, 0.0, 0.5)) * 0.4


def icicle_shot():
    dur = 0.7
    n = n_of(dur)
    t = tax(dur)
    fwip = s.sweep(s.white(n), "band", (7000, 1800), width=0.5) * env_points(t, [(0, 0), (0.01, 1), (0.25, 0.3), (0.5, 0)])
    form = s.grains(n, s.poisson_times(lambda q: 900 * np.exp(-q / 0.08), dur), (0.0005, 0.002), (4000, 12000), np.ones(2000))
    ping = s.metal_ring(0.7, [3100, 4630, 6950], [0.25, 0.15, 0.08])
    return norm(fwip) * 0.8 + norm(form) * 0.3 + s.pad(ping, n) * 0.3


@monster("IceBeetle", "ice_beetle_icicle_shot", "Ice beetle shoots icicle",
         "Crisp crackle as the icicle forms, then a sharp airy 'fwip' with a glassy ping.",
         variants=2, lufs=-14, boss=dict(factor=0.7, sub=0.4, grit=0.15))
def beetle_shot(v):
    return icicle_shot()


@monster("IceBeetle", "icicle_impact", "Icicle impact",
         "Icicle shatters on impact: sharp crack, glass-like shards and a tinkle.",
         variants=3, lufs=-13, boss=dict(factor=0.7, sub=0.5, grit=0.15))
def icicle_impact(v):
    dur = 0.9
    n = n_of(dur)
    hit = s.pad(s.click(0.05, 1500, 12000, 0.006), n) + s.pad(s.thump(0.15, 300, 140, 40, 0.02, 0.3, 3000), n) * 0.5
    shards = s.grains(n, s.poisson_times(lambda q: 900 * np.exp(-q / 0.12), dur), (0.001, 0.006), (3000, 12000),
                      s.rng.uniform(0.1, 1, 3000), q=1.3)
    return norm(hit) + norm(shards) * 0.5 + norm(crystal_rings(n, 10, 0.0, 0.25, 3000, 9000)) * 0.35


# ============================================================ BLOEDZUIGER
@monster("Leech", "leech_slime_loop", "Leech sliming through mud (loop)",
         "Thick, muddy squelching and sucking, darker and dirtier than the slug. Seamless loop.",
         lufs=-19, loop=True, boss=dict(factor=0.65, sub=0.6, grit=0.15))
def leech_move(v):
    dur = 4.6
    n = n_of(dur)
    mud = lp(s.brown(n), 400) * s.smooth_rand(n, 2, 0.4, 1)
    sq = np.zeros(n)
    for t0 in np.arange(0.05, dur - 0.5, 0.7):
        place(sq, s.squelch_sound(0.5, 1.0), t0 + s.rng.uniform(-0.1, 0.1), s.rng.uniform(0.6, 1))
    suck = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 3, dur - 0.3):
        L = n_of(0.15)
        tt = np.arange(L) / s.SR
        place(suck, bp(s.white(L), 200, 900) * np.sin(np.pi * tt / tt[-1]) ** 3, t0, 0.7)
    pops = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 8, dur - 0.1):
        place(pops, s.bubble(s.rng.uniform(150, 500), 0.06), t0, s.rng.uniform(0.3, 0.8))
    return norm(mud) * 0.5 + norm(lp(sq, 2500)) + norm(suck) * 0.4 + norm(pops) * 0.35


@monster("Leech", "leech_screech", "Leech screech (dying)",
         "Wet, gurgling squeal of a dying leech.",
         variants=2, lufs=-13, boss=dict(factor=0.65, sub=0.5, grit=0.25))
def leech_screech(v):
    dur = 0.9
    n = n_of(dur)
    x = s.screech(dur, 950 + 80 * v, 450, rough=0.6, formant_scale=0.8, harsh=0.3)
    x *= 0.6 + 0.4 * s.rumble_mod(n, 22, 1.0)
    gurgle = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 30, dur - 0.1):
        place(gurgle, s.bubble(s.rng.uniform(200, 700), 0.04), t0, 0.6)
    return norm(x) + norm(gurgle) * 0.4


# ============================================================ ENERGIEBAL
def electric(n, rate, amp_pow=2.0):
    dur = n / s.SR
    tt = s.poisson_times(lambda q: rate if np.isscalar(rate) else rate(q), dur)
    return s.grains(n, tt, (0.0003, 0.003), (2000, 12000), s.rng.uniform(0.05, 1, len(tt)) ** amp_pow, q=1.3)


@monster("EnergyBall", "energy_ball_static_loop", "Energy ball moving – static (loop)",
         "Crackling static and a wavering electric hum with the odd zap. Seamless loop.",
         lufs=-19, loop=True, boss=dict(factor=0.7, sub=0.5, grit=0.15))
def energy_static(v):
    dur = 3.3
    n = n_of(dur)
    t = tax(dur)
    f = 120 * (1 + 0.04 * np.sin(2 * np.pi * 0.8 * t))
    hum = np.tanh(2 * (np.sin(s.phase_of(f)) + 0.5 * np.sin(s.phase_of(f * 3.01)))) * s.smooth_rand(n, 3, 0.5, 1)
    crackle = electric(n, 400)
    zaps = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 1.5, dur - 0.2):
        L = n_of(0.08)
        tt = np.arange(L) / s.SR
        z = np.tanh(3 * s.osc(s.glide(tt, 2500, 300, 40), "square")) * np.exp(-tt / 0.02)
        place(zaps, z + electric(L, 3000), t0, 0.6)
    return norm(lp(hum, 3000)) * 0.5 + norm(crackle) * 0.5 + norm(zaps) * 0.4


@monster("EnergyBall", "energy_ball_death", "Energy ball dies",
         "Overload: a rising unstable whine, a sharp electric pop and fizzling sparks fading out.",
         variants=2, lufs=-12, boss=dict(factor=0.7, sub=0.6, grit=0.2))
def energy_death(v):
    dur = 1.6
    n = n_of(dur)
    t = tax(dur)
    tp = 0.45 + 0.05 * v
    whine_f = np.where(t < tp, 300 + 2500 * (t / tp) ** 2, 0)
    whine = (np.sin(s.phase_of(whine_f)) * (1 + 0.3 * np.sin(2 * np.pi * 30 * t))) * (t < tp) * (t / tp)
    pop = np.zeros(n)
    place(pop, s.friedlander(0.2, 0.004) * 1.5 + s.click(0.2, 1000, 12000, 0.01), tp)
    fizz = electric(n, lambda q: 0 if q < tp else 3000 * np.exp(-(q - tp) / 0.25))
    crackle = electric(n, lambda q: 200 * (q / tp) if q < tp else 0)
    return norm(whine) * 0.5 + norm(pop) * 0.8 + norm(fizz) * 0.6 + norm(crackle) * 0.3


@monster("EnergyBall", "energy_ball_fire", "Energy ball fires energy",
         "Crackling electric bolt: a sharp zap with a falling tone and discharge sparks.",
         variants=3, lufs=-13, boss=dict(factor=0.7, sub=0.5, grit=0.2))
def energy_fire(v):
    dur = 0.6
    n = n_of(dur)
    t = tax(dur)
    tone = np.tanh(2.5 * s.osc(s.glide(t, 1800 + 200 * v, 200, 9), "saw")) * np.exp(-t / 0.15)
    bolt = electric(n, lambda q: 6000 * np.exp(-q / 0.08))
    body = s.sweep(s.white(n), "band", (6000, 800), width=0.7) * np.exp(-t / 0.1)
    return norm(lp(tone, 8000)) * 0.6 + norm(bolt) * 0.7 + norm(body) * 0.4


# ============================================================ STINGRAY
@monster("Stingray", "stingray_fly_loop", "Stingray flying (loop)",
         "Smooth gliding whoosh with slow, rippling wing undulation. Seamless loop.",
         lufs=-19, loop=True, boss=dict(factor=0.7, sub=0.5, grit=0.05))
def stingray_fly(v):
    dur = 4.4
    n = n_of(dur)
    t = tax(dur)
    und = 0.5 + 0.5 * np.sin(2 * np.pi * (1 / 1.1) * t)
    glide_n = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 150), np.arange(n), 400 + 700 * und), width=1.0)
    ripple = bp(s.white(n), 1200, 4000) * (0.5 + 0.5 * np.sin(2 * np.pi * 18 * t)) * und ** 2
    low = lp(s.brown(n), 150) * und
    return norm(glide_n) * (0.5 + 0.5 * und) + norm(ripple) * 0.15 + norm(low) * 0.4


@monster("Stingray", "stingray_tailwhip", "Stingray tail whip (spiked)",
         "Fast whip swoosh ending in a sharp crack, with the spikes ticking and rattling.",
         variants=2, lufs=-13, boss=dict(factor=0.72, sub=0.5, grit=0.2))
def stingray_whip(v):
    dur = 0.6
    n = n_of(dur)
    t = tax(dur)
    tc = 0.13 + 0.02 * v
    sw = np.exp(-((t - tc) / 0.06) ** 2) * (t < tc) + np.exp(-((t - tc) / 0.02) ** 2) * (t >= tc)
    swoosh = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 100), np.arange(n), 500 + 4000 * sw), width=1.0) * sw
    crack = np.zeros(n)
    place(crack, s.click(0.03, 1500, 10000, 0.003) * 1.5, tc)
    spikes = np.zeros(n)
    for k in range(6):
        place(spikes, mix(s.metal_ring(0.1, [s.rng.uniform(3000, 6000)], [0.02]), s.click(0.02, 3000, 9000, 0.001)),
              tc - 0.08 + 0.025 * k, 0.4)
    return norm(swoosh) + norm(crack) * 0.8 + norm(spikes) * 0.3


# ============================================================ VLEERMUIS
@monster("Bat", "bat_screech", "Bat screech (dying)",
         "High, piercing squeak-shriek.",
         variants=2, lufs=-14, boss=dict(factor=0.55, sub=0.4, grit=0.3))
def bat_screech(v):
    dur = 0.5
    x = s.screech(dur, 3600 + 300 * v, 2600, rough=0.35, formant_scale=2.2, harsh=0.3, vib=0.04)
    return lp(x, 14000)


@monster("Bat", "bat_wings_loop", "Bat wings flying (loop)",
         "Fast, leathery fluttering of small bat wings. Seamless loop.",
         lufs=-20, loop=True, boss=dict(factor=0.6, sub=0.5, grit=0.1))
def bat_wings(v):
    return flaps(2.3, 9, 0.22, 0.05)


# --- element-aanvallen
def fire_burst(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    whoosh = s.sweep(s.pink(n), "band", (600 * size, 2500), width=1.4) * env_points(t, [(0, 0), (0.04, 1), (dur, 0)])
    roar = lp(s.brown(n), 300) * env_ad(t, 0.02, dur / 3) * s.rumble_mod(n, 20, 0.7)
    crackle = s.grains(n, s.poisson_times(lambda q: 300 * np.exp(-q / (dur / 2)), dur), (0.0005, 0.004), (1500, 9000),
                       s.rng.uniform(0.1, 1, 2000) ** 2)
    return norm(whoosh) * 0.8 + norm(roar) * 0.6 + norm(crackle) * 0.4


def water_splash(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    bloop = s.mix(s.bubble(260 / size, 0.12, 1.2) * 1.0, s.thump(0.15, 180, 90, 30, 0.04, 0.4, 600) * 0.5)
    spray = bp(s.white(n), 1500, 9000) * env_ad(t, 0.01, dur / 4)
    drops = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 60 * np.exp(-q / (dur / 2)), dur - 0.1, 0.02):
        place(drops, s.bubble(s.rng.uniform(600, 2200), s.rng.uniform(0.015, 0.04)), t0, s.rng.uniform(0.2, 0.8))
    return s.pad(norm(bloop), n) + norm(spray) * 0.4 + norm(drops) * 0.5


def moss_puff(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    thwump = s.thump(0.3, 150 / size, 70 / size, 25, 0.06, 0.8, 500)
    puff = bp(s.pink(n), 300, 2500) * env_ad(t, 0.01, dur / 4)
    rustle = s.grains(n, s.poisson_times(lambda q: 500 * np.exp(-q / (dur / 3)), dur), (0.002, 0.01), (1200, 5000),
                      s.rng.uniform(0.1, 1, 2000))
    damp = s.squelch_sound(0.3, 0.4)
    return s.pad(norm(thwump), n) + norm(puff) * 0.5 + norm(rustle) * 0.4 + s.pad(norm(damp), n) * 0.3


def sand_blast(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    hiss = s.sweep(s.white(n), "band", (5000, 2500), width=1.2) * env_points(t, [(0, 0), (0.02, 1), (dur * 0.5, 0.5), (dur, 0)])
    grains_ = s.grains(n, s.poisson_times(lambda q: 2500 * np.exp(-q / (dur / 2)), dur), (0.0005, 0.003), (1500, 8000),
                       s.rng.uniform(0.05, 1, 5000))
    thud = s.thump(0.2, 140 / size, 80, 30, 0.04, 0.6, 800)
    return norm(hiss) * 0.6 + norm(grains_) * 0.6 + s.pad(norm(thud), n) * 0.5


def frost_blast(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    cold = s.sweep(s.white(n), "band", (9000, 3000), width=0.9) * env_points(t, [(0, 0), (0.02, 1), (dur, 0)])
    crk = s.grains(n, s.poisson_times(lambda q: 1500 * np.exp(-q / (dur / 2)), dur), (0.0005, 0.003), (3000, 12000),
                   s.rng.uniform(0.1, 1, 3000), q=1.3)
    return norm(cold) * 0.6 + norm(crk) * 0.5 + norm(crystal_rings(n, 6, 0.0, dur * 0.4)) * 0.35


def twig_snap(dur=0.8, size=1.0):
    n = n_of(dur)
    t = tax(dur)
    snaps = np.zeros(n)
    for k in range(3):
        c = s.click(0.04, 800, 7000, 0.004)
        c = c + s.reson(c, s.rng.uniform(900, 2200) / size, 5) * 1.5
        place(snaps, c, 0.015 * k + s.rng.uniform(0, 0.01), s.rng.uniform(0.6, 1))
    thunk = s.thump(0.2, 260 / size, 140 / size, 30, 0.03, 0.4, 1500)
    whoosh = s.sweep(s.pink(n), "band", (2500, 900), width=1.0) * env_points(t, [(0, 0), (0.02, 0.6), (0.3, 0)])
    leaves = s.grains(n, s.poisson_times(lambda q: 400 * np.exp(-q / (dur / 3)), dur), (0.002, 0.01), (1500, 6000),
                      s.rng.uniform(0.1, 1, 2000))
    return norm(snaps) + s.pad(norm(thunk), n) * 0.5 + norm(whoosh) * 0.3 + norm(leaves) * 0.35


def _faded(fn):
    """Laat een element-laag rustig uitsterven i.p.v. abrupt te stoppen."""
    def g(dur=0.8, size=1.0):
        x = fn(dur, size)
        t = np.arange(len(x)) / s.SR
        return x * np.clip((dur - t) / (0.7 * dur), 0, 1) ** 3
    return g


fire_burst, water_splash, moss_puff, sand_blast, frost_blast, twig_snap = map(
    _faded, (fire_burst, water_splash, moss_puff, sand_blast, frost_blast, twig_snap))

ELEMENTS = {
    "fire": (fire_burst, "Fire", "a burst of fire: whoosh, roar and crackling flames"),
    "water": (water_splash, "Water", "a water blast: bloop, splash and droplets"),
    "moss": (moss_puff, "Moss", "a soft, damp organic thwump with a rustling spore puff"),
    "sand": (sand_blast, "Sand", "a sandblast: hiss and a spray of grains"),
    "frost": (frost_blast, "Frost", "an icy blast with crackling frost and crystal tinkles"),
    "forest": (twig_snap, "Forest (twig)", "sharp twig snaps, a wooden thunk and a rustle of leaves"),
}

for key, (fn, label, what) in ELEMENTS.items():
    def make(fn=fn):
        return lambda v: fn(0.8, 1.0)
    monster("Bat", f"bat_attack_{key}", f"Element bat attack – {label}",
            f"Attack of the {label.lower()} bat: {what}.",
            variants=2, lufs=-14, boss=dict(factor=0.7, sub=0.5, grit=0.2))(make())


# ============================================================ EXTRA BOSS-GELUIDEN
B = "Bosses/Special/"


@sound(B + "boss_poison_spray", "BOSS – Poison spray",
       "A wide hissing spray of poison: pressurised aerosol hiss with wet droplets and a gurgle.",
       variants=2, lufs=-12)
def poison_spray(v):
    dur = 1.8
    n = n_of(dur)
    t = tax(dur)
    sh = env_points(t, [(0, 0), (0.05, 1), (1.2, 0.8), (1.8, 0)])
    hiss = s.sweep(s.white(n), "band", np.full(80, 4000.0), width=1.3) * (0.85 + 0.15 * s.rumble_mod(n, 25, 1))
    wet = s.grains(n, s.poisson_times(lambda q: 500, dur), (0.002, 0.01), (500, 3000), s.rng.uniform(0.1, 1, 2000))
    gurgle = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 15, dur - 0.1):
        place(gurgle, s.bubble(s.rng.uniform(150, 400), 0.06), t0, 0.7)
    return (norm(hiss) + norm(wet) * 0.4 + norm(gurgle) * 0.35) * sh + s.pad(s.thump(0.2, 120, 60, 20, 0.05, 0.5), n) * 0.4


def droplet_sizzle():
    L = n_of(0.25)
    tt = np.arange(L) / s.SR
    splat = s.click(0.25, 500, 4000, 0.004) * 0.8
    fizz = s.grains(L, s.poisson_times(lambda q: 2000 * np.exp(-q / 0.08), 0.25), (0.0003, 0.002), (3000, 11000),
                    s.rng.uniform(0.05, 1, 1000) ** 2)
    return splat + norm(fizz) * 0.5


@sound(B + "boss_acid_rain_loop", "BOSS – Acid rain (loop)",
       "Steady rain of acid drops, each one splatting and sizzling, over a fizzing hiss. Seamless loop.",
       lufs=-16, loop=True)
def acid_rain(v):
    dur = 5.3
    n = n_of(dur)
    x = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 45, dur - 0.3):
        place(x, droplet_sizzle(), t0, s.rng.uniform(0.2, 1.0))
    rain = bp(s.pink(n), 1500, 9000) * s.smooth_rand(n, 0.8, 0.6, 1)
    fizz = s.grains(n, s.poisson_times(lambda q: 1500, dur), (0.0003, 0.002), (4000, 12000), s.rng.uniform(0.05, 1, 9000) ** 2)
    return s.make_loop(norm(x) + norm(rain) * 0.3 + norm(fizz) * 0.25, 0.4)


@sound(B + "boss_armor_regrow", "BOSS – Armour plate growing back",
       "Crunchy organic creaking as a plate grows, small clicks as it settles, then a solid clack locking in.",
       lufs=-12)
def armor_regrow(v):
    dur = 2.2
    n = n_of(dur)
    t = tax(dur)
    grow = env_points(t, [(0, 0), (0.2, 0.4), (1.6, 1), (1.75, 0)])
    creak = s.osc(s.glide(t, 90, 140, 1.2) * (1 + 0.1 * s.smooth_rand(n, 8)), "saw")
    creak = bp(creak * (0.5 + 0.5 * s.rumble_mod(n, 40, 1)), 200, 1500) * grow
    crunch = s.grains(n, s.poisson_times(lambda q: 30 + 250 * np.interp(q, t, grow), 1.7), (0.002, 0.012), (500, 4000),
                      s.rng.uniform(0.1, 1, 3000))
    clicks = np.zeros(n)
    for t0 in np.linspace(0.4, 1.5, 7):
        place(clicks, shell_knock(s.rng.uniform(800, 1500)), t0 + s.rng.uniform(-0.05, 0.05), 0.3)
    lock = s.mix(shell_knock(520, 0.3) * 1.2, s.thump(0.3, 200, 80, 30, 0.05, 0.5, 1500))
    place(clicks, lock, 1.72, 1.0)
    return norm(creak) * 0.4 + norm(crunch) * 0.4 + norm(clicks) * 0.8


@sound(B + "boss_howl", "BOSS – Howl (with echo)",
       "Long, eerie wolf-like howl rising and falling, echoing back from the landscape (echo on purpose).",
       variants=2, lufs=-11)
def howl(v):
    dur = 2.8
    n = n_of(dur)
    t = tax(dur)
    base = 260 + 30 * v
    pitch = env_points(t, [(0, base * 0.75), (0.35, base * 1.25), (1.6, base * 1.35), (2.4, base * 0.95), (2.8, base * 0.7)])
    pitch *= 1 + 0.012 * np.sin(2 * np.pi * 5.5 * t)
    vc = env_points(t, [(0, 0), (0.4, 0.8), (2.0, 1.0), (2.8, 0.3)])
    x = s.voice(dur, pitch, [s.VOWEL_U, s.VOWEL_O], rough=0.15, breath=0.25, sub=0.25, jitter=0.01,
                vowel_curve=vc, shape="saw")
    x = norm(x) * env_points(t, [(0, 0), (0.2, 0.8), (1.0, 1), (2.3, 0.8), (2.8, 0)])
    big = s.bossify(x, factor=0.85, sub=0.3, grit=0.1)
    return s.echo(big, [0.42, 0.86, 1.35, 1.9], [0.45, 0.3, 0.18, 0.1], lowpass=3000)


@sound("Bosses/Armadillo/armadillo_roll_heavy_loop", "BOSS – Armadillo heavy roll (loop)",
       "Extra-heavy rolling: big armour rings slamming down in rhythm, deep ground rumble. Seamless loop.",
       lufs=-12, loop=True)
def roll_heavy(v):
    x = roll_loop(4.4, 1.6, 6, 1.0)
    x = s.bossify(x, factor=0.65, sub=0.9, grit=0.2)
    return s.make_loop(x, 0.4)


@sound(B + "boss_power_pulse", "BOSS – Power pulse",
       "A massive version of the power pulse: longer charge, earth-shaking shockwave and rumble.",
       lufs=-10)
def boss_pulse(v):
    from cat_a import power_pulse
    x = power_pulse(1.6, 3.0)
    n = len(x)
    t = tax(3.0)
    rum = lp(s.brown(n), 80, 4) * env_points(t, [(0, 0), (0.4, 0), (0.5, 1), (3.0, 0)]) * s.rumble_mod(n, 8, 0.6)
    return s.bossify(norm(x) + norm(rum) * 0.6, factor=0.85, sub=0.6, grit=0.2)


@sound(B + "boss_slime_spit", "BOSS – Slime bubbles spit",
       "A heavy wet 'hork' spit, sending a stream of slime bubbles that pop as they fly.",
       variants=2, lufs=-12)
def slime_spit(v):
    dur = 1.5
    n = n_of(dur)
    t = tax(dur)
    hork = s.squelch_sound(0.5, 1.0)
    burst = bp(s.pink(n), 200, 2500) * env_ad(t, 0.005, 0.12)
    bub = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 40 * np.exp(-q / 0.5), dur - 0.2, 0.05):
        place(bub, s.bubble(s.rng.uniform(180, 900), s.rng.uniform(0.04, 0.12), 1.4), t0, s.rng.uniform(0.3, 1))
    return s.bossify(s.pad(norm(hork), n) + norm(burst) * 0.6 + norm(bub) * 0.6, factor=0.8, sub=0.4, grit=0.15)


@sound(B + "boss_bat_bomb_launch", "BOSS – Bat element bomb launched",
       "A thumping launch followed by a falling, whistling whoosh as the bomb flies.",
       variants=2, lufs=-12)
def bomb_launch(v):
    dur = 1.5
    n = n_of(dur)
    t = tax(dur)
    launch = s.thump(0.4, 220, 70, 25, 0.07, 0.8, 1500)
    whistle_f = s.glide(t, 2200 + 200 * v, 900, 1.5)
    whistle = s.sweep(s.white(n), "band", np.interp(np.linspace(0, n, 120), np.arange(n), whistle_f), width=0.12)
    whistle *= env_points(t, [(0, 0), (0.1, 0.5), (1.0, 1), (1.5, 0)])
    whoosh = bp(s.pink(n), 300, 2000) * env_points(t, [(0, 0), (0.08, 1), (1.5, 0.2)])
    return s.pad(norm(launch), n) + norm(whistle) * 0.5 + norm(whoosh) * 0.4


def element_bomb(key):
    fn = ELEMENTS[key][0]

    def make(v):
        dur = 2.6
        n = n_of(dur)
        base = s.explosion(size=0.9, distance=0.55, debris_amt={"sand": 1.5, "forest": 0.8}.get(key, 0.4), dur=dur)
        layer = fn(1.6, 1.4)
        extra = np.zeros(n)
        if key == "water":
            for t0 in s.poisson_times(lambda q: 40 * np.exp(-q / 0.6), 1.8, 0.1):
                place(extra, s.bubble(s.rng.uniform(300, 1500), s.rng.uniform(0.02, 0.06)), t0, 0.6)
        if key == "frost":
            extra = crystal_rings(n, 18, 0.05, 0.8, 2500, 9000, 0.5)
        if key == "fire":
            extra = fire_burst(2.2, 0.6)
        if key == "moss":
            extra = s.pad(s.squelch_sound(0.8, 1.0), n)
        return s.mix(norm(base) * 0.9, norm(layer) * 1.0, norm(extra) * 0.5 if np.any(extra) else np.zeros(n))
    return make


for key, (_, label, what) in ELEMENTS.items():
    sound(B + f"boss_bat_bomb_explode_{key}", f"BOSS – Bat element bomb explodes: {label}",
          f"Element bomb blast plus {what}.", lufs=-11)(element_bomb(key))


@sound("Bosses/Stingray/stingray_tail_laser", "BOSS – Stingray tail laser (from the air)",
       "Charges up, then a powerful buzzing beam fires downward from above, with a falling sweep.",
       variants=2, lufs=-11)
def tail_laser(v):
    dur = 1.8
    n = n_of(dur)
    t = tax(dur)
    tc = 0.35
    charge = np.sin(s.phase_of(np.where(t < tc, 200 + 1800 * (t / tc) ** 2, 0))) * np.where(t < tc, (t / tc) ** 2, 0)
    on = (t >= tc).astype(float)
    tb = np.clip(t - tc, 0, None)
    f = 160 * (1 + 0.02 * np.sin(2 * np.pi * 9 * t))
    beam = (s.osc(f, "saw") + 0.7 * s.osc(f * 1.5, "saw") + 0.4 * s.osc(f * 3.02, "square")) * on
    beam = s.sweep(beam, "low", np.interp(np.linspace(0, n, 100), np.arange(n), 600 + 6000 * np.exp(-tb * 2.5)))
    beam *= env_points(t, [(0, 0), (tc, 0), (tc + 0.02, 1), (1.4, 0.8), (1.8, 0)])
    pew = s.pad(s.dispersion(n_of(1.0), 900, -0.93), n)
    pew = np.roll(bp(pew, 300, 9000), n_of(tc)) * np.exp(-tb * 3) * on
    sizzle = s.grains(n, s.poisson_times(lambda q: 0 if q < tc else 1500, dur), (0.0003, 0.002), (3000, 12000),
                      s.rng.uniform(0.05, 1, 4000) ** 2)
    return norm(charge) * 0.4 + norm(beam) * 0.7 + norm(pew) * 0.7 + norm(sizzle) * 0.25
