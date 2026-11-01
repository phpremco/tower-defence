"""Explosies, drones en hero."""
import numpy as np
import sfxlib as s
from sfxlib import sound, tax, n_of, norm, place, lp, hp, bp, env_ad, env_points

G = "Explosions/"


# ============================================================ EXPLOSIES
def collapse_metal(n, t0, t1, count, base=1.0):
    out = np.zeros(n)
    for _ in range(count):
        f0 = s.rng.uniform(250, 1400)
        ring = s.metal_ring(0.6, [f0, f0 * 2.4, f0 * 4.9], [0.18, 0.09, 0.04], [1, .5, .25])
        th = s.thump(0.3, 160, 70, 30, 0.05, 0.6, 900)
        hit = s.pad(s.mix(ring * 0.18 + bp(s.white(len(ring)), 800, 5000) * np.exp(-np.arange(len(ring)) / 1500) * 0.4, th), n_of(0.6))
        place(out, hit, s.rng.uniform(t0, t1), s.rng.uniform(0.3, 1.0) * base)
    return out


@sound(G + "explosion_gate", "Gate explodes",
       "Large blast with metal parts flying off and a collapsing, clattering gate afterwards.",
       lufs=-10)
def gate(v):
    x = s.explosion(size=1.1, distance=0.5, metal=0.8, dur=3.4)
    x = s.pad(x, n_of(3.4))
    x += collapse_metal(len(x), 0.35, 1.6, 9, 0.5)
    # krakende instorting: lage schuif-rommel
    t = tax(3.4)
    creak = bp(s.brown(len(t)), 80, 600) * env_points(t, [(0, 0), (0.4, 0), (0.7, 0.6), (2.0, 0.3), (3.0, 0)])
    return s.air(x + creak * 0.4)


@sound(G + "explosion_base", "Main base explodes",
       "The big one: a heavy blast, two follow-up explosions, metal debris and a long rolling rumble.",
       lufs=-9)
def base(v):
    dur = 5.5
    n = n_of(dur)
    x = s.pad(s.explosion(size=1.8, distance=0.5, metal=1.0, dur=dur), n)
    place(x, s.explosion(size=0.8, distance=0.6, metal=0.4, dur=2.5), 0.9, 0.55)
    place(x, s.explosion(size=1.0, distance=0.7, metal=0.3, dur=3.0), 1.7, 0.5)
    x += collapse_metal(n, 0.4, 3.0, 16, 0.35)
    t = tax(dur)
    rumble = lp(s.brown(n), 90, 4) * env_points(t, [(0, 0), (0.3, 1), (3.0, 0.6), (5.5, 0)]) * s.rumble_mod(n, 6, 0.6)
    return s.air(x + norm(rumble) * 0.5)


@sound(G + "explosion_air_rocket", "Air rocket explodes",
       "Mid-air burst of the anti-air rocket: sharp crack and boom, little ground debris.",
       variants=3, lufs=-11)
def air_rocket(v):
    x = s.explosion(size=0.75 + 0.1 * v, distance=0.55, brightness=1.15, debris_amt=0.25,
                    crack=0.6, dur=2.0)
    # fragmentatie: korte heldere knetters in de lucht
    n = len(x)
    tt = s.poisson_times(lambda q: 400 * np.exp(-q / 0.12), 0.6, 0.01)
    frag = s.grains(n, tt, (0.001, 0.004), (2500, 9000), s.rng.uniform(0.2, 1, len(tt)))
    return s.air(x + norm(frag) * 0.18)


@sound(G + "explosion_cannon_shell", "Cannon shell explodes",
       "Small explosive from the cannon turret hitting the ground: punchy thud with dirt and stones kicked up.",
       variants=3, lufs=-12)
def cannon(v):
    dur = 1.4
    n = n_of(dur)
    t = tax(dur)
    x = s.pad(s.explosion(size=0.45, distance=0.35, brightness=0.9, debris_amt=0.4, dur=dur), n)
    x += s.pad(s.thump(0.4, 120, 45, 25, 0.08, 0.5, 400), n) * 0.9
    # aarde en steentjes die neerkomen
    tt = s.poisson_times(lambda q: 180 * np.exp(-((q - 0.25) / 0.2) ** 2), dur, 0.08)
    dirt = s.grains(n, tt, (0.002, 0.012), (500, 4500), s.rng.uniform(0.2, 1, len(tt)))
    return s.air(x + norm(dirt) * 0.25)


@sound(G + "explosion_mine", "Road mine explodes",
       "Metallic trigger click, then an upward ground blast and a rain of dirt falling back down.",
       variants=2, lufs=-10)
def mine(v):
    dur = 2.6
    n = n_of(dur)
    x = np.zeros(n)
    place(x, s.metal_ring(0.15, [2100, 4300, 6900], [0.02, 0.012, 0.006]), 0.0, 0.35)
    place(x, s.explosion(size=0.85, distance=0.45, debris_amt=0.6, dur=2.4), 0.12)
    place(x, s.thump(0.6, 110, 38, 18, 0.15, 0.6, 350), 0.12, 1.0)
    tt = s.poisson_times(lambda q: 260 * np.exp(-((q - 0.9) / 0.45) ** 2), dur, 0.35)
    dirt = s.grains(n, tt, (0.002, 0.015), (400, 3500), s.rng.uniform(0.2, 1, len(tt)))
    return s.air(x + norm(dirt) * 0.3)


# ============================================================ DRONES (subtiel)
D = "Drones/"


@sound(D + "drone_fan_loop", "Drone fan blades (loop)",
       "Soft, seamless rotor hum of a small drone. Very quiet on purpose.",
       lufs=-30, loop=True)
def fan(v):
    dur = 4.25
    n = n_of(dur)
    t = tax(dur)
    bpf = 210 * (1 + 0.012 * np.sin(2 * np.pi * 0.7 * t) + 0.006 * (s.smooth_rand(n, 3) - 0.5))
    tone = sum(np.sin(s.phase_of(bpf * k)) / k ** 1.3 for k in range(1, 7))
    chop = 0.6 + 0.4 * (0.5 + 0.5 * np.sin(s.phase_of(bpf)))
    air_n = bp(s.pink(n), 300, 5000) * chop
    x = tone * 0.35 + norm(air_n) * 0.6
    return s.make_loop(lp(x, 7000), 0.25)


@sound(D + "drone_repair_loop", "Drone repairing (welding, loop)",
       "Small welding arc: crackle and sputter over a soft electric buzz. Seamless loop.",
       lufs=-25, loop=True)
def repair(v):
    dur = 3.3
    n = n_of(dur)
    t = tax(dur)
    level = s.smooth_rand(n, 4, 0.4, 1.0)
    tt = s.poisson_times(lambda q: 900, dur)
    crack = s.grains(n, tt, (0.0005, 0.003), (2500, 12000), s.rng.uniform(0.1, 1, len(tt)) ** 2, q=1.3)
    sput = s.grains(n, s.poisson_times(lambda q: 25, dur), (0.01, 0.04), (800, 3000), np.ones(200), q=1.5)
    buzz = sum(np.sin(2 * np.pi * 100 * k * t) / k for k in range(1, 12))
    hiss = hp(s.white(n), 5000)
    x = norm(crack) * level + norm(sput) * 0.3 + norm(buzz) * 0.08 + norm(hiss) * 0.12 * level
    return s.make_loop(x, 0.2)


@sound(D + "drone_harvest_loop", "Drone harvesting crystal (loop)",
       "Soft mining beam hum with glassy crystal pings and tiny chips breaking off. Seamless loop.",
       lufs=-25, loop=True)
def harvest(v):
    dur = 4.2
    n = n_of(dur)
    t = tax(dur)
    beam_f = 330 * (1 + 0.01 * np.sin(2 * np.pi * 5 * t))
    beam = np.sin(s.phase_of(beam_f)) + 0.4 * np.sin(s.phase_of(beam_f * 2.01)) + 0.15 * np.sin(s.phase_of(beam_f * 3.02))
    beam = beam * (0.8 + 0.2 * s.smooth_rand(n, 6)) + bp(s.white(n), 2000, 6000) * 0.15
    pings = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 4, dur - 0.6):
        f = s.rng.uniform(1800, 4200)
        pings = place(pings, s.metal_ring(0.6, [f, f * 1.51, f * 2.39], [0.25, 0.15, 0.08], [1, .6, .3], 0.001), t0,
                      s.rng.uniform(0.2, 0.6))
    chips = s.grains(n, s.poisson_times(lambda q: 30, dur), (0.001, 0.004), (4000, 11000), np.ones(400))
    x = norm(beam) * 0.35 + norm(pings) * 0.7 + norm(chips) * 0.2
    return s.make_loop(x, 0.3)


@sound(D + "drone_attack_laser", "Attack drone fires small laser",
       "Small, light blaster 'pew' – a smaller cousin of the approved laser.",
       variants=3, lufs=-21)
def drone_laser(v):
    n = n_of(0.35)
    t = tax(0.35)
    st = [260, 300, 230][v]
    c = s.dispersion(n, st, -0.86) + 0.6 * s.dispersion(n, int(st * 0.8), -0.83)
    pew = bp(c, 600, 11000) * np.exp(-t * 9)
    x = norm(pew) + s.pad(s.click(0.01, 3000, 12000, 0.0015), n) * 0.2
    return s.air(x, 0.1)


# ============================================================ HERO
H = "Hero/"


@sound(H + "hero_laser_gun", "Hero laser gun",
       "Punchy rifle blaster: dispersion 'pew' with a solid low kick, heavier than the drone laser.",
       variants=3, lufs=-12)
def hero_laser(v):
    n = n_of(0.7)
    t = tax(0.7)
    a = [-0.91, -0.905, -0.915][v]
    c = s.dispersion(n, 520, a) + 0.7 * s.dispersion(n, 430, a + 0.02) + 0.4 * s.dispersion(n, 650, a - 0.01)
    pew = bp(c, 300, 10000) * np.exp(-t * 5.5)
    kick = np.sin(s.phase_of(s.glide(t, 220, 55, 35))) * np.exp(-t * 22) * 0.45
    x = norm(pew) + kick + s.pad(s.click(0.01, 2500, 12000, 0.002), n) * 0.2
    return s.air(x, 0.12)


def saber_hum(dur, n=None):
    n = n or n_of(dur)
    t = np.arange(n) / s.SR
    f = 88 * (1 + 0.003 * np.sin(2 * np.pi * 0.5 * t))
    a = s.osc(f, "saw") + 0.8 * s.osc(f * 1.007, "saw")
    buzz = np.tanh(3 * s.osc(f * 2, "square")) * 0.15
    x = lp(a, 1800) + buzz + bp(s.white(n), 2000, 5000) * 0.03
    return x


@sound(H + "hero_saber_hum_loop", "Hero energy sword – idle hum (loop)",
       "Steady electric hum of the energy blade while it is drawn. Seamless loop.",
       lufs=-22, loop=True)
def saber_loop(v):
    return s.make_loop(saber_hum(3.2), 0.3)


@sound(H + "hero_saber_swing", "Hero energy sword – swing",
       "Swoosh of the energy blade: hum bends in pitch and swells as it cuts through the air.",
       variants=3, lufs=-13)
def saber_swing(v):
    dur = 0.55 + 0.1 * v
    n = n_of(dur)
    t = tax(dur)
    tc = dur * (0.35 + 0.1 * v)
    sw = np.exp(-((t - tc) / (dur * 0.18)) ** 2)
    f = 88 * (1 + 0.35 * sw)
    a = s.osc(f, "saw") + 0.8 * s.osc(f * 1.01, "saw")
    hum = s.sweep(a, "low", np.interp(np.linspace(0, n, 80), np.arange(n), 1200 + 4500 * sw))
    whoosh = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 80), np.arange(n), 600 + 2500 * sw), width=1.0)
    return hp(norm(hum) * (0.35 + 0.65 * sw) + norm(whoosh) * sw * 0.45, 50)


@sound(H + "hero_saber_hit", "Hero energy sword – hit",
       "Blade connects: a crackling energy clash with a short sizzle.",
       variants=2, lufs=-12)
def saber_hit(v):
    dur = 0.6
    n = n_of(dur)
    t = tax(dur)
    hum = saber_hum(dur) * np.exp(-t * 4)
    zap = s.grains(n, s.poisson_times(lambda q: 2500 * np.exp(-q / 0.08), dur), (0.0005, 0.003), (2000, 12000),
                   np.ones(3000))
    crack = np.tanh(4 * s.osc(s.glide(t, 900, 120, 20), "square")) * np.exp(-t * 18)
    sizzle = hp(s.white(n), 4000) * env_ad(t, 0.005, 0.15)
    return norm(hum) * 0.4 + norm(zap) * 0.6 + crack * 0.5 + sizzle * 0.25


def power_pulse(size=1.0, dur=1.8):
    n = n_of(dur)
    t = tax(dur)
    # korte oplaad-zoem (0,25 s) en dan de ontlading
    tc = 0.25 * size
    charge_f = np.where(t < tc, 120 + 900 * (t / tc) ** 2, 0)
    charge = np.sin(s.phase_of(charge_f)) * np.where(t < tc, (t / tc) ** 1.5, 0) * 0.5
    tr = np.clip(t - tc, 0, None)
    on = (t >= tc).astype(float)
    boom = np.sin(s.phase_of(s.glide(tr, 160 / size, 38 / size, 9))) * np.exp(-tr * 3 / size) * on
    wub = s.osc(s.glide(tr, 300 / size, 60 / size, 6), "saw") * np.exp(-tr * 2.5 / size) * on
    wub = lp(wub, 1500) * (0.6 + 0.4 * np.sin(2 * np.pi * 14 * tr))
    shimmer = s.sweep(s.white(n), "band", (9000, 900), width=0.8) * np.exp(-tr * 4 / size) * on
    blast = s.friedlander(dur, 0.01 * size)
    blast = np.roll(blast, n_of(tc)) * 0.8
    return charge + boom + norm(wub) * 0.6 + norm(shimmer) * 0.35 + lp(blast, 3000)


@sound(H + "hero_power_pulse", "Hero power pulse",
       "Quick charge-up whine, then an expanding energy shockwave with a deep 'wub' and shimmer.",
       lufs=-11)
def hero_pulse(v):
    return s.air(power_pulse(1.0))


def suit_rustle(dur, amt=1.0):
    n = n_of(dur)
    t = tax(dur)
    r = s.grains(n, s.poisson_times(lambda q: 700 * np.exp(-q / (dur / 3)), dur), (0.002, 0.01), (1500, 6000),
                 s.rng.uniform(0.1, 1, 3000), q=1.8)
    swish = bp(s.pink(n), 800, 4000) * env_ad(t, 0.02, dur / 3)
    return (norm(r) * 0.5 + norm(swish) * 0.5) * amt


@sound(H + "hero_run_step", "Hero running step (space suit)",
       "Heavy boot on the ground with suit fabric swish and a light clink of gear. Play one per step.",
       variants=4, lufs=-18)
def hero_step(v):
    st = s.footstep(0.55, "gravel", dur=0.35)
    n = len(st)
    x = st + s.pad(suit_rustle(0.3, 0.5), n)
    if v % 2 == 0:
        place(x, s.metal_ring(0.15, [1800 + 300 * v, 3700, 5900], [0.03, 0.02, 0.01]), 0.01, 0.12)
    return x


@sound(H + "hero_death", "Hero dies (human)",
       "Pained cry fading into a groan, slightly muffled by the helmet. Synthesised voice – see note.",
       variants=2, lufs=-13)
def hero_death(v):
    dur = 1.6
    n = n_of(dur)
    t = tax(dur)
    pitch = env_points(t, [(0, 230 + 30 * v), (0.15, 300 + 30 * v), (0.6, 210), (1.2, 140), (1.6, 110)])
    vc = env_points(t, [(0, 0), (0.7, 0.2), (1.6, 1.0)])
    x = s.voice(dur, pitch, [s.VOWEL_A, s.VOWEL_O], rough=0.25, breath=0.35, jitter=0.03, vowel_curve=vc)
    x *= env_points(t, [(0, 0), (0.05, 1), (0.6, 0.9), (1.3, 0.5), (1.6, 0)])
    # helm: lichte resonantie, hoog/laag afgeknepen
    x = bp(x, 180, 5500) + 0.3 * s.reson(x, 1400, 4)
    gasp = bp(s.pink(n), 1000, 5000) * env_points(t, [(0, 0), (1.25, 0), (1.35, 0.4), (1.6, 0)])
    return norm(x) + gasp * 0.25


@sound(H + "hero_fall", "Hero falls to the ground (space suit)",
       "Body hits the ground: heavy suit thud, a second bounce, gear clattering and fabric settling.",
       lufs=-13)
def hero_fall(v):
    dur = 1.3
    n = n_of(dur)
    x = np.zeros(n)
    place(x, s.footstep(0.9, "gravel", dur=0.6), 0.0, 1.0)
    place(x, s.footstep(0.6, "gravel", dur=0.4), 0.17, 0.6)
    place(x, s.metal_ring(0.3, [1300, 2900, 4700], [0.06, 0.04, 0.02]), 0.03, 0.25)
    place(x, s.metal_ring(0.3, [2100, 4500], [0.04, 0.02]), 0.21, 0.15)
    place(x, suit_rustle(0.9, 0.6), 0.05)
    return s.air(x)
