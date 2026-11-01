"""Omgeving en UI."""
import numpy as np
import sfxlib as s
from sfxlib import sound, tax, n_of, norm, place, lp, hp, bp, env_ad, env_points

E = "Environment/"


def bubbles(n, rate, f_lo, f_hi, dur_lo, dur_hi, rise=1.0):
    out = np.zeros(n)
    for t0 in s.poisson_times(lambda q: rate, n / s.SR - dur_hi):
        f = np.exp(s.rng.uniform(np.log(f_lo), np.log(f_hi)))
        b = s.bubble(f, s.rng.uniform(dur_lo, dur_hi), rise)
        place(out, b, t0, s.rng.uniform(0.2, 1.0))
    return out


@sound(E + "water_bubbling_loop", "Water bubbling (loop)",
       "Gently bubbling water: many small bubbles over a soft watery wash. Seamless loop.",
       lufs=-22, loop=True)
def water(v):
    dur = 5.3
    n = n_of(dur)
    x = bubbles(n, 28, 350, 1400, 0.03, 0.09)
    wash = bp(s.pink(n), 400, 3000) * s.smooth_rand(n, 2, 0.4, 1.0)
    return s.make_loop(norm(x) + norm(wash) * 0.15, 0.3)


@sound(E + "lava_bubbling_loop", "Lava bubbling (loop)",
       "Thick, slow lava: deep heavy 'blorp' bubbles, occasional pops and a hot crackle. Seamless loop.",
       lufs=-19, loop=True)
def lava(v):
    dur = 6.3
    n = n_of(dur)
    t = tax(dur)
    blorp = bubbles(n, 3.5, 70, 180, 0.15, 0.35, rise=0.6)
    blorp = lp(np.tanh(norm(blorp) * 2), 1500)
    pops = s.grains(n, s.poisson_times(lambda q: 4, dur), (0.01, 0.04), (300, 1800), np.ones(200), q=1.4)
    crackle = s.grains(n, s.poisson_times(lambda q: 60, dur), (0.0005, 0.003), (2000, 9000), s.rng.uniform(0.1, 1, 2000) ** 2)
    roar = lp(s.brown(n), 200) * s.smooth_rand(n, 1.5, 0.5, 1)
    x = norm(blorp) + norm(pops) * 0.4 + norm(crackle) * 0.25 + norm(roar) * 0.3
    return s.make_loop(x, 0.35)


@sound(E + "earthquake", "Earthquake",
       "Deep rolling rumble that builds and fades, with rocks cracking and grit rattling.",
       lufs=-12)
def quake(v):
    dur = 5.0
    n = n_of(dur)
    t = tax(dur)
    shape = env_points(t, [(0, 0), (0.6, 0.7), (1.5, 1), (3.5, 0.8), (5, 0)])
    rum = lp(s.brown(n), 90, 4) * s.rumble_mod(n, 7, 0.7) * shape
    mid = bp(s.brown(n), 90, 500) * s.rumble_mod(n, 14, 0.8) * shape
    cracks = np.zeros(n)
    for t0 in s.rng.uniform(0.5, 3.8, 7):
        c = s.explosion(size=0.25, distance=0.8, debris_amt=0.5, dur=0.8, crack=0.2)
        place(cracks, c, t0, s.rng.uniform(0.2, 0.5))
    grit = s.grains(n, s.poisson_times(lambda q: 200 * np.interp(q, t, shape), dur), (0.002, 0.01), (800, 5000),
                    s.rng.uniform(0.1, 1, 3000))
    return norm(rum) * 1.2 + norm(mid) * 0.5 + norm(cracks) * 0.5 + norm(grit) * 0.2


@sound(E + "monster_heavy_step", "Heavy monster footstep",
       "Huge creature step: deep ground thud with rattling stones and a short ground shake.",
       variants=4, lufs=-13, comp=False)
def heavy_step(v):
    st = s.footstep(1.0, "gravel", dur=1.0)
    n = len(st)
    t = tax(1.0)
    shake = lp(s.brown(n), 70, 4) * env_ad(t, 0.01, 0.25)
    rattle = s.grains(n, s.poisson_times(lambda q: 300 * np.exp(-q / 0.2), 1.0, 0.03), (0.002, 0.008), (1500, 7000),
                      s.rng.uniform(0.1, 1, 1000))
    return s.air(st + norm(shake) * 0.6 + norm(rattle) * 0.15)


@sound(E + "item_freeze", "Item freezing",
       "Cold whoosh while ice crystals crackle and grow, ending in a crisp frozen 'crack'.",
       lufs=-14)
def freeze(v):
    dur = 1.6
    n = n_of(dur)
    t = tax(dur)
    grow = env_points(t, [(0, 0), (1.1, 1), (1.25, 0)])
    rate = lambda q: 50 + 1500 * np.interp(q, t, grow)
    crk = s.grains(n, s.poisson_times(rate, 1.25), (0.0005, 0.003), (3000, 12000), s.rng.uniform(0.1, 1, 4000), q=1.3)
    cold = s.sweep(s.pink(n), "band", (800, 6000), width=0.8) * grow
    shimmer = np.zeros(n)
    for k in range(8):
        f = s.rng.uniform(3000, 7000)
        place(shimmer, s.metal_ring(0.8, [f, f * 1.37], [0.3, 0.2], [1, .5], 0.0005), s.rng.uniform(0.3, 1.1), 0.15)
    snap = np.zeros(n)
    place(snap, s.click(0.08, 1500, 12000, 0.012) + s.pad(s.metal_ring(0.4, [2600, 5300, 8100], [0.08, .05, .03]) * 0.4, n_of(0.08)), 1.2)
    return norm(crk) * 0.6 + norm(cold) * 0.3 + norm(shimmer) * 0.3 + norm(snap) * 0.9


@sound(E + "cash", "Cash / coins",
       "Bright coin clinks dropping into a pile – a satisfying 'money earned' sound.",
       variants=2, lufs=-15)
def cash(v):
    dur = 0.9
    n = n_of(dur)
    x = np.zeros(n)
    for k in range(5 + v):
        f = s.rng.uniform(2400, 4200)
        coin = s.metal_ring(0.6, [f, f * 1.69, f * 2.43, f * 3.1], [0.18, 0.12, 0.08, 0.05], [1, .7, .4, .25], 0.0008)
        place(x, coin, 0.04 * k + s.rng.uniform(0, 0.03), s.rng.uniform(0.4, 1.0) * (1 - 0.08 * k))
    return s.stereo(s.air(x))


@sound(E + "tower_break", "Weapon tower breaks",
       "Technical tower destroyed: electric sparks, a power-down whine, metal crunching and parts falling.",
       lufs=-11)
def tower_break(v):
    dur = 2.6
    n = n_of(dur)
    t = tax(dur)
    x = np.zeros(n)
    place(x, s.explosion(size=0.6, distance=0.45, metal=0.6, dur=1.8), 0.0, 0.7)
    sparks = s.grains(n, s.poisson_times(lambda q: 900 * np.exp(-q / 0.5), dur), (0.0005, 0.004), (2500, 12000),
                      s.rng.uniform(0.1, 1, 4000) ** 2)
    whine_f = s.glide(t, 1400, 80, 2.2)
    whine = (np.sin(s.phase_of(whine_f)) + 0.3 * s.osc(whine_f * 0.5, "square")) * env_points(t, [(0, 0), (0.05, 1), (1.8, 0.2), (2.2, 0)])
    crunch = np.zeros(n)
    for _ in range(8):
        f0 = s.rng.uniform(200, 900)
        hit = s.mix(s.metal_ring(0.4, [f0, f0 * 2.7], [0.06, 0.03]) * 0.4, s.thump(0.25, 150, 70, 30, 0.04, 0.6, 1500))
        place(crunch, hit, s.rng.uniform(0.15, 1.8), s.rng.uniform(0.3, 0.8))
    return s.air(x + norm(sparks) * 0.35 + norm(whine) * 0.3 + norm(crunch) * 0.55)


@sound(E + "scrap_pickup", "Scrap metal picked up",
       "A few loose metal pieces rattling and clinking together as they are scooped up.",
       variants=2, lufs=-15)
def scrap(v):
    dur = 0.6
    n = n_of(dur)
    x = np.zeros(n)
    for k in range(6):
        f0 = s.rng.uniform(600, 2200)
        piece = s.metal_ring(0.3, [f0, f0 * 2.3, f0 * 3.9], [0.05, 0.03, 0.015], [1, .6, .3], 0.002)
        place(x, piece, s.rng.uniform(0, 0.28), s.rng.uniform(0.3, 1))
    rattle = s.grains(n, s.poisson_times(lambda q: 120 * np.exp(-q / 0.15), dur), (0.002, 0.006), (1000, 5000),
                      np.ones(500))
    return s.air(x + norm(rattle) * 0.3)


def wind(n, center, width, gust_hz, whistle=0.0, whistle_f=900):
    gust = s.smooth_rand(n, gust_hz, 0.25, 1.0)
    base = s.pink(n)
    curve = np.interp(np.linspace(0, n, 150), np.arange(n), center * (0.7 + 0.6 * gust))
    w = s.sweep(base, "band", curve, width=width) * gust
    out = norm(w)
    if whistle > 0:
        f = whistle_f * (0.8 + 0.5 * s.smooth_rand(n, 0.5))
        wh = s.sweep(s.white(n), "band", np.interp(np.linspace(0, n, 150), np.arange(n), f), width=0.05) * gust ** 2
        out += norm(wh) * whistle
    return out


@sound(E + "leaves_wind_loop", "Leaves blowing in the wind (loop)",
       "Soft gusting wind with leaves rustling and skittering along. Stereo, seamless loop.",
       lufs=-22, loop=True)
def leaves(v):
    dur = 8.3
    n = n_of(dur)
    out = []
    for ch in range(2):
        w = wind(n, 500, 1.4, 0.4)
        gust = s.smooth_rand(n, 0.5, 0.2, 1)
        rus = s.grains(n, s.poisson_times(lambda q: 350, dur), (0.002, 0.012), (1500, 7000), s.rng.uniform(0.1, 1, 4000))
        out.append(s.make_loop(w * 0.6 + norm(rus) * gust * 0.5, 0.5))
    return np.stack(out, 1)


@sound(E + "icy_wind_loop", "Icy wind (loop)",
       "Cold, thin howling wind with a whistling edge and fine blowing ice. Stereo, seamless loop.",
       lufs=-22, loop=True)
def icy(v):
    dur = 8.3
    n = n_of(dur)
    out = []
    for ch in range(2):
        w = wind(n, 1100, 0.8, 0.3, whistle=0.5, whistle_f=1100 + 200 * ch)
        ice = s.grains(n, s.poisson_times(lambda q: 120, dur), (0.0005, 0.002), (5000, 12000), s.rng.uniform(0.1, 1, 2000))
        out.append(s.make_loop(w + norm(ice) * 0.12, 0.5))
    return np.stack(out, 1)


def steam(dur, n=None):
    n = n or n_of(dur)
    hiss = s.sweep(s.white(n), "band", np.full(100, 4500.0) * (1 + 0.1 * np.random.default_rng(1).standard_normal(100)), width=1.4)
    roar = bp(s.pink(n), 300, 1500)
    flutter = 0.85 + 0.15 * s.rumble_mod(n, 30, 1.0)
    return (norm(hiss) + norm(roar) * 0.35) * flutter


@sound(E + "steam_burst", "Steam blowing (burst)",
       "A pressurised jet of steam that blasts out and tails off.",
       lufs=-15)
def steam_burst(v):
    dur = 1.8
    t = tax(dur)
    return steam(dur) * env_points(t, [(0, 0), (0.03, 1), (0.6, 0.8), (1.8, 0)])


@sound(E + "steam_loop", "Steam blowing (loop)",
       "Continuous venting steam, for pipes or vents. Seamless loop.",
       lufs=-20, loop=True)
def steam_loop(v):
    return s.make_loop(steam(4.3), 0.3)


@sound(E + "upgrade", "Upgrade (towers & gates)",
       "Positive power-up: a rising sparkly synth arpeggio, a mechanical 'clunk' locking in, and a shimmer.",
       variants=2, lufs=-14)
def upgrade(v):
    dur = 1.3
    n = n_of(dur)
    t = tax(dur)
    x = np.zeros(n)
    notes = [523.25, 659.25, 783.99, 1046.5] if v == 0 else [392.0, 523.25, 659.25, 783.99, 1046.5]
    for i, f in enumerate(notes):
        L = n_of(0.5)
        tt = np.arange(L) / s.SR
        tone = (np.sin(s.phase_of(np.full(L, f))) + 0.3 * np.sin(s.phase_of(np.full(L, f * 2.0))) +
                0.15 * s.osc(np.full(L, f), "square")) * np.exp(-tt / 0.18) * (1 - np.exp(-tt / 0.003))
        place(x, tone, 0.07 * i, 0.6)
    clunk = s.mix(s.thump(0.2, 180, 90, 40, 0.03, 0.5, 2000), s.metal_ring(0.2, [900, 2100], [0.03, 0.02]) * 0.3)
    place(x, clunk, 0.0, 0.8)
    shimmer = s.sweep(s.white(n), "band", (2000, 10000), width=0.5) * env_points(t, [(0, 0), (0.25, 0.6), (1.3, 0)])
    return s.stereo(x + norm(shimmer) * 0.25, 0.008)


@sound(E + "acid_sizzle", "Acid sizzle",
       "Corrosive acid eating into something: dense fizzing, tiny popping bubbles and a hiss.",
       lufs=-16)
def acid(v):
    dur = 1.8
    n = n_of(dur)
    t = tax(dur)
    sh = env_points(t, [(0, 0), (0.05, 1), (1.0, 0.7), (1.8, 0)])
    fizz = s.grains(n, s.poisson_times(lambda q: 2500, dur), (0.0003, 0.002), (3000, 12000), s.rng.uniform(0.05, 1, 6000) ** 2)
    pops = bubbles(n, 40, 900, 3000, 0.01, 0.025)
    hiss = bp(s.white(n), 3500, 9000)
    return (norm(fizz) * 0.7 + norm(pops) * 0.35 + norm(hiss) * 0.25) * sh


@sound(E + "metal_dull_hit", "Metal takes a dull hit",
       "Something heavy thumps into thick metal: a solid low 'dunk' with a short muted resonance.",
       variants=3, lufs=-14, comp=False)
def dull_hit(v):
    dur = 0.45
    th = s.thump(dur, 200, 85, 30, 0.06, 0.6, 1200)
    f0 = [180, 210, 160][v]
    ring = s.metal_ring(dur, [f0, f0 * 2.31, f0 * 3.9, f0 * 5.6], [0.12, 0.07, 0.04, 0.02], [1, .5, .3, .15], 0.003)
    return s.air(th + lp(ring, 3000) * 0.35)


@sound(E + "base_alert", "Base under attack – alert",
       "Short sci-fi warning: three urgent two-tone pulses.",
       lufs=-14)
def alert(v):
    dur = 1.0
    n = n_of(dur)
    x = np.zeros(n)
    for i in range(3):
        for j, f in enumerate([880, 660]):
            L = n_of(0.11)
            tt = np.arange(L) / s.SR
            tone = (s.osc(np.full(L, f), "square") * 0.5 + np.sin(s.phase_of(np.full(L, f)))) * \
                np.minimum(1, np.minimum(tt / 0.005, (tt[-1] - tt) / 0.01))
            place(x, lp(tone, 5000), 0.3 * i + 0.12 * j, 1.0)
    return s.stereo(x, 0.004)


@sound(E + "rc_cart_loop", "RC cart driving on a paved road (loop)",
       "Small electric motor whine with gear chatter over tyres rolling on asphalt. Seamless loop.",
       lufs=-18, loop=True)
def rc(v):
    dur = 4.3
    n = n_of(dur)
    t = tax(dur)
    f = 620 * (1 + 0.03 * np.sin(2 * np.pi * 0.6 * t) + 0.01 * (s.smooth_rand(n, 5) - 0.5))
    motor = s.osc(f, "saw") * 0.5 + np.sin(s.phase_of(f * 2)) * 0.3 + np.sin(s.phase_of(f * 0.25)) * 0.4
    motor = lp(motor, 5000)
    gears = s.grains(n, s.poisson_times(lambda q: 140, dur), (0.001, 0.003), (2000, 6000), np.ones(1000))
    tyres = bp(s.pink(n), 150, 2500) * (0.8 + 0.2 * s.smooth_rand(n, 20))
    grit = s.grains(n, s.poisson_times(lambda q: 300, dur), (0.001, 0.004), (800, 4000), s.rng.uniform(0.1, 1, 3000))
    x = norm(motor) * 0.6 + norm(gears) * 0.15 + norm(tyres) * 0.5 + norm(grit) * 0.2
    return s.make_loop(x, 0.3)


@sound(E + "fireball_roll_loop", "Fireball rolling on a paved road (loop)",
       "A burning ball tumbling along: roaring flames, crackling fire and a grinding roll on stone. Seamless loop.",
       lufs=-16, loop=True)
def fireball(v):
    dur = 4.3
    n = n_of(dur)
    t = tax(dur)
    roll_rate = 3.2  # omwentelingen per s
    flame = bp(s.pink(n), 80, 2500) * (0.6 + 0.4 * s.rumble_mod(n, 12, 1.0))
    flame *= 0.8 + 0.2 * np.sin(2 * np.pi * roll_rate * t)
    crackle = s.grains(n, s.poisson_times(lambda q: 120, dur), (0.0005, 0.004), (1500, 9000), s.rng.uniform(0.1, 1, 2000) ** 2)
    grind = bp(s.brown(n), 60, 600) * (0.6 + 0.4 * np.abs(np.sin(np.pi * roll_rate * t)))
    stones = s.grains(n, s.poisson_times(lambda q: 60, dur), (0.002, 0.008), (700, 3500), np.ones(1000))
    x = norm(flame) + norm(crackle) * 0.4 + norm(grind) * 0.6 + norm(stones) * 0.2
    return s.make_loop(x, 0.3)


@sound(E + "net_fire", "Net fired from a gun",
       "Pneumatic 'thunk' of compressed air, then the whoosh of a net unfolding with ropes fluttering.",
       variants=2, lufs=-14)
def net(v):
    dur = 1.0
    n = n_of(dur)
    t = tax(dur)
    thunk = s.thump(0.25, 260, 90, 40, 0.03, 0.8, 2500)
    pss = bp(s.white(n), 1500, 8000) * env_ad(t, 0.002, 0.04)
    whoosh = s.sweep(s.pink(n), "band", curve_of_whoosh(), width=1.0) * env_points(t, [(0, 0), (0.06, 0), (0.25, 1), (0.9, 0)])
    flutter = bp(s.white(n), 800, 4000) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 45 * t))) * \
        env_points(t, [(0, 0), (0.1, 0), (0.3, 0.6), (0.9, 0)])
    x = s.pad(thunk, n) + norm(pss) * 0.5 + norm(whoosh) * 0.6 + norm(flutter) * 0.25
    return s.air(x)


def curve_of_whoosh():
    return s.curve_of(100, lambda u: 2500 * np.exp(-u * 2.5) + 400)
