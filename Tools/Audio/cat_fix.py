"""Ronde 2: aanpassingen op basis van Remco's review.

Vervangt de functie van de afgekeurde geluiden in het register; goedgekeurde geluiden
blijven precies zoals ze waren (hun code wordt niet aangeraakt).
Belangrijkste lessen uit de feedback:
- geen bel-/ding-klanken (geen tonale metalen resonanties), metaal = doffe 'clank'
- voetstappen: echte stap op de grond (hiel-teen, gruis), niet alleen een bonk
- slijm/modder: dichte natte klikjes en plopjes, geen zwiepende resonantie ('sonar')
- insectenpootjes: losse dunne tikjes, geen sissende ruisbedding
- vleugels: rustiger, natuurlijk aanzwellend en wegzakkend
- bosses: duidelijk meer bas
"""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points, mix
import cat_a, cat_b, cat_c  # noqa: F401  (registreert de originelen)

ENTRIES = {e["path"]: e for e in s.REGISTRY}
FIXED = []


def replace(path, fn, **save_kw):
    e = ENTRIES[path]
    e["fn"] = fn
    e["save_kw"].update(save_kw)
    FIXED.append(path)


# ------------------------------------------------------------ helpers
def boss_bass(x, factor=0.65, sub=1.0, grit=0.2, shelf=1.4):
    """Sterkere boss-versie: lager, langer, sub-laag en een flinke lage plank."""
    y = s.bossify(x, factor=factor, sub=sub, grit=grit)
    y = y + shelf * lp(y, 220, 2)
    return norm(y)


def clank(dur=0.25, f_lo=300, f_hi=1800, weight=0.5):
    """Doffe metalen klap ZONDER naklinkende toon: ruisstoot door een paar sterk
    gedempte resonanties, plus een korte lage tik."""
    n = n_of(dur)
    t = tax(dur)
    burst = s.white(n) * np.exp(-t / (0.006 + 0.01 * weight))
    out = bp(burst, f_lo, f_hi) * 0.6
    for _ in range(3):
        out += s.reson(burst, s.rng.uniform(f_lo, f_hi), 3.0) * 0.5 * np.exp(-t / 0.03)
    out += s.thump(dur, 180 - 60 * weight, 80, 40, 0.02 + 0.03 * weight, 0.4, 900) * weight
    return lp(out, 3500)


def grit_crunch(n, t0, dur, dens, lo=700, hi=5000, amp=1.0):
    tt = s.poisson_times(lambda q: dens * np.exp(-max(q - t0, 0) / (dur / 3)) if q >= t0 else 0, t0 + dur)
    return s.grains(n, tt, (0.001, 0.006), (lo, hi), s.rng.uniform(0.2, 1, len(tt)) * amp)


def step(weight=0.5, kind="boot", surface="dirt"):
    """Echte voetstap: hiel/teen (of kussentjes), contact-'tok' en duidelijk hoorbaar gruis.
    kind: boot | paw | claw (tweevoeter met klauwen) | padheavy (zware poot)."""
    dur = 0.25 + 0.3 * weight
    n = n_of(dur)
    t = tax(dur)
    out = np.zeros(n)

    def contact(level, lpf):
        L = n_of(0.05)
        tt = np.arange(L) / s.SR
        c = lp(s.white(L), lpf) * np.exp(-tt / (0.006 + 0.01 * weight))
        c += s.thump(0.05, 140 - 50 * weight, 70, 50, 0.01 + 0.02 * weight, 0.0)[:L] * (0.3 + 0.5 * weight)
        return c * level

    if kind == "boot":
        place(out, contact(1.0, 2500), 0.0)
        place(out, contact(0.6, 3500), 0.045 + 0.02 * weight)
    elif kind == "paw":
        place(out, contact(0.8, 1800), 0.0)
        place(out, contact(0.5, 2000), 0.03)
        for k in range(s.rng.integers(2, 4)):
            place(out, s.click(0.01, 2500, 8000, 0.0008), 0.004 + 0.012 * k + s.rng.uniform(0, 0.006), 0.35)
    elif kind == "claw":
        place(out, contact(1.0, 2200), 0.0)
        place(out, contact(0.7, 2600), 0.06)
        for k in range(3):
            place(out, s.click(0.015, 1500, 6000, 0.0015), 0.065 + 0.01 * k, 0.3)
        scr = bp(s.white(n_of(0.08)), 1500, 5000) * np.sin(np.linspace(0, np.pi, n_of(0.08))) ** 2
        place(out, scr, 0.08, 0.15)
    elif kind == "padheavy":
        place(out, contact(1.0, 1200), 0.0)
        place(out, contact(0.6, 1500), 0.07)
    lo, hi, dens = {"dirt": (600, 4500, 900), "gravel": (1200, 7000, 1400), "stone": (900, 5000, 500)}[surface]
    out += norm(grit_crunch(n, 0.002, 0.12 + 0.1 * weight, dens, lo, hi)) * (0.45 + 0.1 * weight)
    if kind in ("boot", "claw"):
        out += norm(grit_crunch(n, 0.05 + 0.02 * weight, 0.08, dens * 0.6, lo, hi)) * 0.25
    return out


def wet_texture(dur, rate=180, f_lo=300, f_hi=2200, size=1.0, swell_hz=1.5):
    """Slijm: heel veel kleine natte plopjes/klikjes die over elkaar heen vallen (zoals roeren
    in iets kleverigs), met aanzwellende golven."""
    n = n_of(dur)
    swell = s.smooth_rand(n, swell_hz, 0.15, 1.0) ** 1.5
    out = np.zeros(n)
    tt = s.poisson_times(lambda q: rate * swell[min(int(q * s.SR), n - 1)], dur - 0.05)
    for t0 in tt:
        f = np.exp(s.rng.uniform(np.log(f_lo), np.log(f_hi))) / size
        L = n_of(s.rng.uniform(0.006, 0.02) * size)
        q = np.arange(L) / s.SR
        b = np.sin(s.phase_of(f * (1 + s.rng.uniform(0.5, 2.0) * q / q[-1]) * np.ones(L))) * np.exp(-q / (L / s.SR / 4))
        place(out, b, t0, s.rng.uniform(0.15, 1.0))
    stick = lp(s.pink(n), 1800 / size) * swell * 0.25          # kleverige ondergrond
    return lp(out, 6000 / size) + stick


def mud_texture(dur, size=1.0):
    n = n_of(dur)
    slosh = lp(s.brown(n), 500 / size) * (0.4 + 0.6 * s.rumble_mod(n, 8, 1.0))
    suck = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 2.5, dur - 0.3):
        L = n_of(0.18)
        q = np.arange(L) / s.SR
        sk = bp(s.white(L), 180, 900) * np.sin(np.pi * q / q[-1]) ** 3
        place(suck, sk, t0, 0.6)
    pops = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 6, dur - 0.1):
        place(pops, s.bubble(s.rng.uniform(110, 320) / size, s.rng.uniform(0.05, 0.1), 0.8), t0, s.rng.uniform(0.4, 1))
    return norm(slosh) * 0.5 + norm(suck) * 0.5 + norm(pops) * 0.6 + norm(wet_texture(dur, 120, 150, 900, size * 1.4)) * 0.6


def legs(dur, step_rate, n_legs, f_lo, f_hi, tick=0.004, scrape=0.0):
    """Dunne pootjes: losse droge tikjes (chitine op grond) in een loopritme."""
    n = n_of(dur)
    out = np.zeros(n)
    t = 0.0
    while t < dur:
        for leg in range(n_legs):
            tt = t + (leg / n_legs) / step_rate + s.rng.normal(0, 0.006)
            if 0 < tt < dur - 0.02:
                L = n_of(tick * 4)
                q = np.arange(L) / s.SR
                c = s.white(L) * np.exp(-q / tick)
                c = s.reson(c, s.rng.uniform(f_lo, f_hi), 4) + 0.3 * bp(c, f_lo, f_hi)
                place(out, c, tt, s.rng.uniform(0.35, 1))
        t += 1 / step_rate * s.rng.uniform(0.9, 1.1)
    out = lp(out, f_hi * 1.6)
    if scrape > 0:
        out += norm(bp(s.pink(n), 800, 3000) * s.smooth_rand(n, 3, 0, 1) ** 2) * scrape
    return out


def flap(size=0.5, strength=1.0, dur=None):
    """Één natuurlijke vleugelslag: zachte opslag, krachtige neerslag ('whoomf'), korte nazwiep."""
    dur = dur or 0.18 + 0.5 * size
    n = n_of(dur)
    t = tax(dur)
    up_end = dur * 0.35
    up = bp(s.pink(n), 600, 3000) * np.where(t < up_end, np.sin(np.pi * t / up_end) * 0.25, 0)
    down_env = np.clip((t - up_end) / (dur - up_end), 0, 1)
    down_env = np.sin(np.pi * down_env) ** 1.3 * (t >= up_end)
    centre = (700 - 450 * size)
    down = s.sweep(s.pink(n), "band", s.curve_of(60, lambda u: centre * (0.7 + 0.8 * np.sin(np.pi * u))), width=1.3) * down_env
    body = lp(s.brown(n), 180 + 200 * (1 - size)) * down_env ** 2 * (0.3 + 0.7 * size)
    leather = bp(s.white(n), 1500, 5000) * down_env * 0.12 * (0.5 + 0.5 * np.sin(2 * np.pi * (35 - 15 * size) * t))
    return (norm(up) * 0.4 + norm(down) + norm(body) * 0.6 + norm(leather) * 0.4) * strength


def flapping(dur, rate, size, bursts=False, flap_dur=None):
    """Reeks slagen met natuurlijke variatie: tempo en kracht golven; met bursts=True
    fladdert hij in vlagen met korte glijpauzes (kleine dieren)."""
    n = n_of(dur)
    out = np.zeros(n)
    t = 0.0
    strength = s.smooth_rand(n, 0.6, 0.55, 1.0)
    while t < dur - 0.05:
        if bursts and s.rng.random() < 0.12:
            t += s.rng.uniform(0.15, 0.35)  # glijden
            continue
        k = min(int(t * s.SR), n - 1)
        place(out, flap(size, strength[k], flap_dur), t)
        t += (1 / rate) * s.rng.uniform(0.92, 1.08)
    return out


# ============================================================ BELLEN ERUIT
def explosion_nobell(size, distance, dur):
    return s.explosion(size=size, distance=distance, metal=0.0, dur=dur)


def debris_clanks(n, t0, t1, count, amp=0.4, f_lo=250, f_hi=1500):
    out = np.zeros(n)
    for _ in range(count):
        place(out, clank(0.25, f_lo, f_hi, s.rng.uniform(0.3, 0.8)), s.rng.uniform(t0, t1), s.rng.uniform(0.3, 1) * amp)
    return out


def gate(v):
    dur = 3.4
    n = n_of(dur)
    t = tax(dur)
    x = s.pad(explosion_nobell(1.1, 0.5, dur), n)
    x += debris_clanks(n, 0.3, 1.6, 10, 0.5)
    creak = bp(s.brown(n), 70, 500) * env_points(t, [(0, 0), (0.4, 0), (0.7, 0.6), (2.0, 0.3), (3.0, 0)])
    rubble = grit_crunch(n, 0.4, 1.6, 400, 500, 4000)
    return s.air(x + norm(creak) * 0.4 + norm(rubble) * 0.25)


def base(v):
    dur = 5.5
    n = n_of(dur)
    t = tax(dur)
    x = s.pad(explosion_nobell(1.8, 0.5, dur), n)
    place(x, explosion_nobell(0.8, 0.6, 2.5), 0.9, 0.55)
    place(x, explosion_nobell(1.0, 0.7, 3.0), 1.7, 0.5)
    x += debris_clanks(n, 0.4, 3.0, 14, 0.35)
    rumble = lp(s.brown(n), 90, 4) * env_points(t, [(0, 0), (0.3, 1), (3.0, 0.6), (5.5, 0)]) * s.rumble_mod(n, 6, 0.6)
    rubble = grit_crunch(n, 0.5, 3.0, 300, 400, 3500)
    return s.air(x + norm(rumble) * 0.5 + norm(rubble) * 0.2)


replace("Explosions/explosion_gate", gate)
replace("Explosions/explosion_base", base)


def hero_fall(v):
    dur = 1.2
    n = n_of(dur)
    x = np.zeros(n)
    place(x, step(0.9, "boot", "dirt"), 0.0, 1.0)
    place(x, s.thump(0.4, 120, 50, 20, 0.08, 0.6, 600), 0.0, 1.0)      # lichaam
    place(x, s.thump(0.3, 140, 60, 25, 0.05, 0.5, 700), 0.17, 0.6)     # nabounce
    place(x, clank(0.2, 400, 1600, 0.3), 0.03, 0.3)                     # uitrusting, dof
    place(x, clank(0.2, 500, 1800, 0.2), 0.2, 0.2)
    place(x, grit_crunch(n, 0.0, 0.5, 700, 600, 4000), 0.0, 0.4)
    place(x, cat_a.suit_rustle(0.9, 0.6), 0.05)
    return s.air(x)


replace("Hero/hero_fall", hero_fall)


def freeze(v):
    dur = 1.5
    n = n_of(dur)
    t = tax(dur)
    grow = env_points(t, [(0, 0), (1.05, 1), (1.2, 0)])
    crk = s.grains(n, s.poisson_times(lambda q: 60 + 1400 * np.interp(q, t, grow), 1.2), (0.0005, 0.003), (2000, 10000),
                   s.rng.uniform(0.1, 1, 4000), q=1.4)
    creak = s.grains(n, s.poisson_times(lambda q: 25 * np.interp(q, t, grow), 1.2), (0.01, 0.03), (600, 2500),
                     np.ones(200), q=1.6)
    cold = s.sweep(s.pink(n), "band", (700, 5000), width=0.9) * grow
    snap = np.zeros(n)
    L = n_of(0.12)
    q = np.arange(L) / s.SR
    place(snap, lp(s.white(L), 9000) * np.exp(-q / 0.008) + s.thump(0.12, 400, 180, 40, 0.015, 0.3, 3000)[:L] * 0.5, 1.15)
    tail = grit_crunch(n, 1.16, 0.25, 900, 2500, 9000)
    return norm(crk) * 0.6 + norm(creak) * 0.35 + norm(cold) * 0.3 + norm(snap) * 0.9 + norm(tail) * 0.3


replace("Environment/item_freeze", freeze)


def tower_break(v):
    dur = 2.6
    n = n_of(dur)
    t = tax(dur)
    x = np.zeros(n)
    place(x, explosion_nobell(0.6, 0.45, 1.8), 0.0, 0.7)
    sparks = s.grains(n, s.poisson_times(lambda q: 700 * np.exp(-q / 0.5), dur), (0.0005, 0.004), (2500, 11000),
                      s.rng.uniform(0.1, 1, 4000) ** 2)
    whine_f = s.glide(t, 900, 60, 2.2)
    whine = lp(s.osc(whine_f, "saw"), 1500) * env_points(t, [(0, 0), (0.05, 1), (1.6, 0.2), (2.0, 0)])
    crunch = debris_clanks(n, 0.15, 1.8, 10, 0.6, 200, 1200)
    grind = bp(s.brown(n), 80, 700) * env_points(t, [(0, 0), (0.2, 0.7), (1.2, 0.4), (2.2, 0)])
    return s.air(x + norm(sparks) * 0.3 + norm(whine) * 0.18 + norm(crunch) * 0.6 + norm(grind) * 0.3)


replace("Environment/tower_break", tower_break)


def scrap(v):
    dur = 0.7
    n = n_of(dur)
    x = np.zeros(n)
    for _ in range(6):
        place(x, clank(0.2, 300, 1400, s.rng.uniform(0.15, 0.5)), s.rng.uniform(0, 0.3), s.rng.uniform(0.4, 1))
    scrape = bp(s.white(n), 900, 3500) * env_points(tax(dur), [(0, 0), (0.05, 0.6), (0.3, 0.3), (0.45, 0)])
    rattle = s.grains(n, s.poisson_times(lambda q: 150 * np.exp(-q / 0.2), dur), (0.002, 0.008), (500, 3000), np.ones(500))
    return s.air(norm(x) + norm(scrape) * 0.2 + norm(rattle) * 0.35)


replace("Environment/scrap_pickup", scrap)


def cash(v):
    """Kassa 'ka-ching': mechanische klak + lade die openschuift, dan één korte kassabel."""
    dur = 1.1
    n = n_of(dur)
    x = np.zeros(n)
    place(x, clank(0.15, 800, 3000, 0.3), 0.0, 0.9)             # 'ka' toets/mechaniek
    place(x, clank(0.12, 1200, 4000, 0.2), 0.035, 0.6)
    slide = bp(s.white(n_of(0.25)), 600, 3000) * np.sin(np.linspace(0, np.pi, n_of(0.25))) ** 2
    slide += s.grains(n_of(0.25), s.poisson_times(lambda q: 120, 0.25), (0.002, 0.005), (800, 3000), np.ones(100)) * 0.3
    place(x, slide, 0.06, 0.35)
    place(x, clank(0.2, 400, 1500, 0.6), 0.3, 0.7)              # lade klapt open
    f = [2350, 2600][v]
    bell = s.metal_ring(0.6, [f, f * 2.32, f * 4.25], [0.09, 0.05, 0.02], [1, .35, .12], 0.001)
    place(x, lp(bell, 7000), 0.28, 0.45)                         # 'ching'
    return s.stereo(s.air(x))


replace("Environment/cash", cash)


# ============================================================ VOETSTAPPEN
def wolf_step(v):
    return step(0.3, "paw", "dirt")


def dino_step(v):
    return step(0.65, "claw", "gravel")


def armadillo_step(v):
    return step(0.6, "padheavy", "dirt")


for name, fn, boss_kw in [("Monsters/Wolf/wolf_step", wolf_step, dict(factor=0.72, sub=0.6, shelf=1.0)),
                          ("Monsters/DinoSoldier/dino_soldier_step", dino_step, dict(factor=0.7, sub=0.8, shelf=1.2)),
                          ("Monsters/Armadillo/armadillo_step", armadillo_step, dict(factor=0.7, sub=0.8, shelf=1.2))]:
    replace(name, fn)
    replace(name.replace("Monsters/", "Bosses/") + "_boss", (lambda f, k: (lambda v: boss_bass(f(v), grit=0.1, **k)))(fn, boss_kw))


# ============================================================ SLIJM & MODDER
def slug_move(v):
    dur = 4.6
    x = wet_texture(dur, 200, 350, 2400, 1.0, 1.2)
    n = len(x)
    stretch = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 1.2, dur - 0.5):  # kleverig loslaten
        L = n_of(0.3)
        q = np.arange(L) / s.SR
        st = s.sweep(s.pink(L), "band", (500, 1400), width=0.6) * np.sin(np.pi * q / q[-1]) ** 2
        place(stretch, st, t0, 0.5)
    return s.make_loop(x + norm(stretch) * 0.25, 0.3)


def leech_move(v):
    return s.make_loop(mud_texture(4.6, 1.0), 0.3)


replace("Monsters/Slug/slug_move_loop", slug_move)
replace("Monsters/Leech/leech_slime_loop", leech_move)


def slug_move_boss(v):
    dur = 4.6
    x = wet_texture(dur, 160, 350, 2400, 1.0, 1.0)
    return s.make_loop(boss_bass(x, 0.62, 1.0, 0.1, 1.6), 0.4)


def leech_move_boss(v):
    return s.make_loop(boss_bass(mud_texture(4.6, 1.0), 0.62, 1.0, 0.1, 1.6), 0.4)


replace("Bosses/Slug/slug_move_loop_boss", slug_move_boss)
replace("Bosses/Leech/leech_slime_loop_boss", leech_move_boss)


def slime_spit(v):
    dur = 1.4
    n = n_of(dur)
    t = tax(dur)
    hork = lp(s.pink(n_of(0.25)), 1200) * env_points(tax(0.25), [(0, 0), (0.05, 1), (0.25, 0)])
    pt = bp(s.white(n), 300, 2500) * env_ad(t, 0.004, 0.06)
    slime = wet_texture(dur, 400, 200, 1500, 1.3, 3) * env_points(t, [(0, 0), (0.05, 0), (0.12, 1), (0.8, 0.5), (1.4, 0)])
    blobs = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 25 * np.exp(-q / 0.5), dur - 0.2, 0.08):
        place(blobs, s.bubble(s.rng.uniform(120, 450), s.rng.uniform(0.06, 0.14), 0.9), t0, s.rng.uniform(0.3, 1))
    x = s.pad(norm(hork), n) * 0.6 + norm(pt) * 0.5 + norm(slime) + norm(blobs) * 0.5
    return boss_bass(x, 0.85, 0.6, 0.1, 1.0)


replace("Bosses/Special/boss_slime_spit", slime_spit)


# ============================================================ POOTJES
def ant_walk(v):
    x = legs(3.3, 7, 6, 1800, 4500, 0.0025)
    return s.make_loop(x, 0.25)


def scorpion_walk(v):
    x = legs(3.3, 3.5, 8, 900, 2800, 0.005, scrape=0.15)
    return s.make_loop(x, 0.25)


replace("Monsters/Ant/ant_walk_loop", ant_walk)
replace("Monsters/Scorpion/scorpion_walk_loop", scorpion_walk)
replace("Bosses/Ant/ant_walk_loop_boss",
        lambda v: s.make_loop(boss_bass(legs(3.3, 5, 6, 1500, 4000, 0.003), 0.6, 0.6, 0.1, 1.2), 0.3))
replace("Bosses/Scorpion/scorpion_walk_loop_boss",
        lambda v: s.make_loop(boss_bass(legs(3.3, 2.6, 8, 800, 2500, 0.006, scrape=0.2), 0.62, 0.8, 0.1, 1.4), 0.3))


# ============================================================ VLEUGELS
replace("Monsters/Flying/wings_flapping_loop", lambda v: s.make_loop(flapping(4.4, 3.2, 0.45), 0.3))
replace("Monsters/Flying/large_wings_flapping_loop", lambda v: s.make_loop(flapping(5.5, 1.3, 0.95), 0.4))
replace("Monsters/Bat/bat_wings_loop", lambda v: s.make_loop(flapping(3.3, 7, 0.2, bursts=True, flap_dur=0.1), 0.25))
replace("Bosses/Flying/wings_flapping_loop_boss",
        lambda v: s.make_loop(boss_bass(flapping(5.5, 2.0, 0.6), 0.75, 0.8, 0.05, 1.4), 0.4))
replace("Bosses/Flying/large_wings_flapping_loop_boss",
        lambda v: s.make_loop(boss_bass(flapping(6.6, 1.1, 1.0), 0.8, 0.9, 0.05, 1.4), 0.5))
replace("Bosses/Bat/bat_wings_loop_boss",
        lambda v: s.make_loop(boss_bass(flapping(4.4, 4.5, 0.3, bursts=True, flap_dur=0.15), 0.6, 0.9, 0.05, 1.6), 0.35))


# ============================================================ IJS
def icicle_pop(v):
    """IJspegel die uit een gat ploept: kort ijsgekraak, dan een holle 'plop' en een fwip."""
    dur = 0.7
    n = n_of(dur)
    t = tax(dur)
    crack = s.grains(n, s.poisson_times(lambda q: 900 if q < 0.08 else 0, 0.08), (0.0005, 0.003), (2000, 9000),
                     s.rng.uniform(0.2, 1, 200))
    L = n_of(0.09)
    q = np.arange(L) / s.SR
    f0 = [520, 600][v % 2]
    plop = np.sin(s.phase_of(f0 * (1 + 1.2 * q / q[-1]) * np.ones(L))) * np.exp(-q / 0.02) * (1 - np.exp(-q / 0.001))
    plop = plop + 0.4 * lp(s.white(L), 2500) * np.exp(-q / 0.004)
    pop = np.zeros(n)
    place(pop, plop, 0.08)
    fwip = s.sweep(s.white(n), "band", (5000, 1500), width=0.5) * env_points(t, [(0, 0), (0.09, 0), (0.12, 0.6), (0.35, 0)])
    return norm(crack) * 0.4 + norm(pop) + norm(fwip) * 0.35


def icicle_crack(v):
    """IJs dat breekt: harde krak, dan knisperend scheuren en splinters (geen gerinkel)."""
    dur = 0.9
    n = n_of(dur)
    L = n_of(0.08)
    q = np.arange(L) / s.SR
    snap = np.zeros(n)
    place(snap, lp(s.white(L), 10000) * np.exp(-q / 0.006), 0.0)
    place(snap, s.thump(0.15, 320, 150, 40, 0.02, 0.3, 2500), 0.0, 0.5)
    fracture = s.grains(n, s.poisson_times(lambda q: 1100 * np.exp(-q / 0.15), dur, 0.005), (0.0005, 0.004), (1500, 9000),
                        s.rng.uniform(0.1, 1, 3000), q=1.5)
    creak = s.grains(n, s.poisson_times(lambda q: 40 * np.exp(-q / 0.2), dur), (0.01, 0.03), (500, 2000), np.ones(100))
    splinters = grit_crunch(n, 0.03, 0.4, 700, 3000, 10000)
    return norm(snap) + norm(fracture) * 0.6 + norm(creak) * 0.3 + norm(splinters) * 0.3


replace("Monsters/IceBeetle/ice_beetle_icicle_shot", icicle_pop)
replace("Monsters/IceBeetle/icicle_impact", icicle_crack)
replace("Bosses/IceBeetle/ice_beetle_icicle_shot_boss", lambda v: boss_bass(icicle_pop(v), 0.65, 0.9, 0.1, 1.4))
replace("Bosses/IceBeetle/icicle_impact_boss", lambda v: boss_bass(icicle_crack(v), 0.65, 1.0, 0.1, 1.5))


# ============================================================ BOSS: MEER BAS (zelfde basis, approved klank)
for path, base_fn, kw in [
    ("Bosses/Ant/ant_screech_boss", cat_c.ant_screech, dict(factor=0.55, sub=0.9, grit=0.3, shelf=1.6)),
    ("Bosses/Bat/bat_screech_boss", cat_c.bat_screech, dict(factor=0.5, sub=0.9, grit=0.3, shelf=1.6)),
    ("Bosses/IceBeetle/ice_beetle_screech_boss", cat_c.beetle_screech, dict(factor=0.62, sub=0.8, grit=0.25, shelf=1.2)),
    ("Bosses/Scorpion/scorpion_screech_boss", cat_c.scorpion_screech, dict(factor=0.62, sub=0.8, grit=0.3, shelf=1.2)),
    ("Bosses/Scorpion/scorpion_acid_shot_boss", cat_c.scorpion_acid, dict(factor=0.65, sub=1.0, grit=0.2, shelf=1.6)),
    ("Bosses/EnergyBall/energy_ball_death_boss", cat_c.energy_death, dict(factor=0.68, sub=0.8, grit=0.2, shelf=1.0)),
    ("Bosses/EnergyBall/energy_ball_fire_boss", cat_c.energy_fire, dict(factor=0.68, sub=0.8, grit=0.2, shelf=1.0)),
]:
    replace(path, (lambda f, k: (lambda v: boss_bass(f(v), **k)))(base_fn, kw))


# ============================================================ STINGRAY
def stingray_fly(v, period=1.9, dur=5.7):
    n = n_of(dur)
    t = tax(dur)
    und = 0.5 + 0.5 * np.sin(2 * np.pi * t / period)
    glide_n = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 150), np.arange(n), 380 + 600 * und), width=1.0)
    ripple = bp(s.white(n), 1000, 3500) * (0.5 + 0.5 * np.sin(2 * np.pi * 12 * t)) * und ** 2
    low = lp(s.brown(n), 150) * und
    return norm(glide_n) * (0.5 + 0.5 * und) + norm(ripple) * 0.12 + norm(low) * 0.45


replace("Monsters/Stingray/stingray_fly_loop", lambda v: s.make_loop(stingray_fly(v), 0.4))
replace("Bosses/Stingray/stingray_fly_loop_boss",
        lambda v: s.make_loop(boss_bass(stingray_fly(v, 2.2, 6.6), 0.75, 0.8, 0.05, 1.3), 0.5))


def tailwhip(v, slow=1.0):
    dur = 0.6 * slow
    n = n_of(dur)
    t = tax(dur)
    tc = (0.14 + 0.02 * v) * slow
    sw = np.exp(-((t - tc) / (0.07 * slow)) ** 2) * (t < tc) + np.exp(-((t - tc) / (0.04 * slow)) ** 2) * (t >= tc)
    swoosh = s.sweep(s.pink(n), "band", np.interp(np.linspace(0, n, 100), np.arange(n), 300 + 1500 * sw), width=1.1) * sw
    thwack = np.zeros(n)
    place(thwack, s.thump(0.15, 220, 90, 30, 0.03, 0.8, 1500), tc, 1.0)
    rattle = np.zeros(n)
    for k in range(5):
        place(rattle, clank(0.06, 800, 2200, 0.1), tc - 0.06 * slow + 0.02 * k * slow, 0.25)
    return norm(lp(swoosh, 4000)) + norm(thwack) * 0.6 + norm(rattle) * 0.25


replace("Monsters/Stingray/stingray_tailwhip", tailwhip)
replace("Bosses/Stingray/stingray_tailwhip_boss", lambda v: boss_bass(tailwhip(v, 1.4), 0.78, 0.8, 0.15, 1.2))


def tail_laser(v):
    """Zware sci-fi straal: oplaad-zoem, harde inzet en een dikke, brommende, pulserende beam."""
    dur = 2.2
    n = n_of(dur)
    t = tax(dur)
    tc = 0.45
    on = (t >= tc).astype(float)
    tb = np.clip(t - tc, 0, None)
    charge_f = np.where(t < tc, 80 + 700 * (t / tc) ** 2, 0)
    charge = (s.osc(charge_f, "saw") * np.where(t < tc, (t / tc) ** 2, 0))
    charge = lp(charge, 3000) + s.sweep(s.white(n), "band", (500, 6000), width=0.4) * np.where(t < tc, (t / tc) ** 3, 0) * 0.5
    f = 55 * (1 + 0.015 * np.sin(2 * np.pi * 6 * t))
    stack = sum(s.osc(f * d, "saw") for d in (1.0, 1.006, 0.994, 2.003, 1.498))
    beam = np.tanh(1.8 * stack) * on
    beam = lp(beam, 2500) * (0.75 + 0.25 * np.sin(2 * np.pi * 11 * t))
    whine = np.sin(s.phase_of(1800 + 300 * np.sin(2 * np.pi * 0.7 * t))) * on * 0.25
    sizzle = s.grains(n, s.poisson_times(lambda q: 0 if q < tc else 2500, dur), (0.0003, 0.002), (3000, 12000),
                      s.rng.uniform(0.05, 1, 6000) ** 2)
    blast = np.zeros(n)
    place(blast, s.friedlander(0.4, 0.012) * 1.5 + lp(s.white(n_of(0.4)), 4000) * np.exp(-np.arange(n_of(0.4)) / s.SR / 0.05), tc)
    env = env_points(t, [(0, 0), (tc, 0), (tc + 0.03, 1), (1.8, 0.85), (2.2, 0)])
    x = norm(charge) * 0.4 * (t < tc + 0.05) + (norm(beam) + whine + norm(sizzle) * 0.3) * env + norm(blast) * 0.8
    return boss_bass(x, 0.95, 0.6, 0.1, 0.8)


replace("Bosses/Stingray/stingray_tail_laser", tail_laser)


# ============================================================ OVERIG
def armor_regrow(v):
    """Alleen het knisperend aangroeien (op verzoek), aanzwellend en dan klaar."""
    dur = 1.8
    n = n_of(dur)
    t = tax(dur)
    grow = env_points(t, [(0, 0), (0.2, 0.4), (1.5, 1), (1.7, 0)])
    crunch = s.grains(n, s.poisson_times(lambda q: 40 + 300 * np.interp(q, t, grow), 1.7), (0.002, 0.012), (500, 4000),
                      s.rng.uniform(0.1, 1, 3000))
    fine = s.grains(n, s.poisson_times(lambda q: 30 + 700 * np.interp(q, t, grow), 1.7), (0.0005, 0.003), (2000, 8000),
                    s.rng.uniform(0.1, 1, 4000))
    return boss_bass(norm(crunch) + norm(fine) * 0.5, 0.95, 0.4, 0.05, 0.6)


replace("Bosses/Special/boss_armor_regrow", armor_regrow)


def howl(v):
    """Wolvenhuil: ademende, ruwe stem die opschiet, aanhoudt met lichte trilling en wegzakt;
    begint met een korte grom-ademstoot. Echo blijft (was goed)."""
    dur = 2.6
    n = n_of(dur)
    t = tax(dur)
    b = 300 + 25 * v
    pitch = env_points(t, [(0, b * 0.55), (0.25, b * 1.15), (0.45, b * 1.3), (1.6, b * 1.35), (2.1, b * 1.0), (2.6, b * 0.6)])
    pitch *= 1 + 0.02 * np.sin(2 * np.pi * 5 * t) * np.clip((t - 0.5) / 0.5, 0, 1)
    vc = env_points(t, [(0, 0.0), (0.4, 0.6), (1.8, 1.0), (2.6, 0.8)])
    voice = s.voice(dur, pitch, [s.VOWEL_A, s.VOWEL_O], rough=0.35, breath=0.55, sub=0.15, jitter=0.025,
                    vowel_curve=vc, shape="saw")
    voice = norm(voice) * env_points(t, [(0, 0), (0.12, 0.6), (0.5, 1), (2.0, 0.85), (2.6, 0)])
    breath = bp(s.pink(n), 600, 4000) * env_points(t, [(0, 0), (0.1, 0.8), (0.5, 0.35), (2.2, 0.25), (2.6, 0)])
    rasp = bp(s.white(n), 1500, 5000) * s.rumble_mod(n, 60, 0.9) * env_points(t, [(0, 0), (0.3, 0.3), (2.0, 0.2), (2.6, 0)])
    x = voice + norm(breath) * 0.25 + norm(rasp) * 0.12
    big = boss_bass(x, 0.82, 0.4, 0.1, 0.8)
    return s.echo(big, [0.42, 0.86, 1.35, 1.9], [0.45, 0.3, 0.18, 0.1], lowpass=3000)


replace("Bosses/Special/boss_howl", howl)


def hero_grunt(v):
    """Korte pijnlijke grom/kreun i.p.v. een schreeuw, licht gedempt door de helm, met uitademing."""
    dur = 0.9
    n = n_of(dur)
    t = tax(dur)
    p0 = [125, 108][v]
    pitch = env_points(t, [(0, p0 * 1.15), (0.08, p0 * 1.25), (0.35, p0), (0.6, p0 * 0.8), (0.9, p0 * 0.7)])
    x = s.voice(dur, pitch, s.VOWEL_U if v else s.VOWEL_O, rough=0.55, breath=0.4, sub=0.2, jitter=0.04, growl_hz=25)
    x *= env_points(t, [(0, 0), (0.03, 1), (0.3, 0.8), (0.55, 0.3), (0.65, 0)])
    exhale = bp(s.pink(n), 500, 3500) * env_points(t, [(0, 0), (0.55, 0), (0.62, 0.5), (0.9, 0)])
    x = bp(norm(x), 120, 4500) + 0.25 * s.reson(norm(x), 1100, 4)
    return norm(x) + norm(exhale) * 0.25


replace("Hero/hero_death", hero_grunt)
ENTRIES["Hero/hero_death"]["title"] = "Hero dies (grunt)"


def alert(v):
    """Serieus waarschuwingssignaal: twee zware klaxon-stoten die licht omlaag buigen."""
    dur = 1.3
    n = n_of(dur)
    x = np.zeros(n)
    for i in range(2):
        L = n_of(0.5)
        q = np.arange(L) / s.SR
        f = 210 * (1 - 0.06 * q / q[-1])
        tone = s.osc(f, "saw") + 0.8 * s.osc(f * 1.5, "saw") + 0.4 * s.osc(f * 2.01, "square")
        tone = np.tanh(1.5 * tone)
        tone = bp(tone, 250, 3500) + 0.6 * s.reson(tone, 900, 3)
        tone *= np.minimum(1, np.minimum(q / 0.02, (q[-1] - q) / 0.06))
        place(x, tone, 0.62 * i)
    return s.stereo(norm(x), 0.004)


replace("Environment/base_alert", alert)


def fireball(v):
    """Zware rollende steenbal: lage rommel in het ritme van de rol, knarsend gruis, wat zacht vuur."""
    dur = 4.3
    n = n_of(dur)
    t = tax(dur)
    rate = 2.2
    rot = 0.6 + 0.4 * np.abs(np.sin(np.pi * rate * t)) ** 1.5
    rumble = lp(s.brown(n), 160, 4) * rot * s.rumble_mod(n, 20, 0.5)
    grind = bp(s.brown(n), 120, 900) * rot
    stones = s.grains(n, s.poisson_times(lambda q: 90, dur), (0.002, 0.01), (400, 2500), s.rng.uniform(0.2, 1, 1000))
    thuds = np.zeros(n)
    tt = 0.0
    while tt < dur - 0.3:
        place(thuds, s.thump(0.25, 110, 50, 25, 0.05, 0.6, 400), tt, s.rng.uniform(0.4, 0.8))
        tt += 1 / rate
    fire = lp(s.pink(n), 1500) * (0.6 + 0.4 * s.rumble_mod(n, 12, 1)) * 0.5
    crackle = s.grains(n, s.poisson_times(lambda q: 30, dur), (0.0005, 0.003), (1500, 6000), s.rng.uniform(0.1, 1, 300) ** 2)
    x = norm(rumble) + norm(grind) * 0.5 + norm(stones) * 0.35 + norm(thuds) * 0.6 + norm(fire) * 0.25 + norm(crackle) * 0.15
    return s.make_loop(lp(x, 6000), 0.3)


replace("Environment/fireball_roll_loop", fireball)


def rc(v):
    """Klein motortje (lager, brommerig) met wielen die over asfalt rollen."""
    dur = 4.3
    n = n_of(dur)
    t = tax(dur)
    f = 140 * (1 + 0.03 * np.sin(2 * np.pi * 0.5 * t) + 0.015 * (s.smooth_rand(n, 4) - 0.5))
    motor = np.tanh(1.5 * (s.osc(f, "saw") + 0.5 * s.osc(f * 2, "square") * 0.5)) * (0.85 + 0.15 * np.sin(s.phase_of(f / 2)))
    motor = lp(motor, 2200)
    tyres = bp(s.pink(n), 120, 1800) * (0.8 + 0.2 * s.smooth_rand(n, 15))
    grit = s.grains(n, s.poisson_times(lambda q: 250, dur), (0.001, 0.004), (600, 3500), s.rng.uniform(0.1, 1, 2500))
    x = norm(motor) * 0.6 + norm(tyres) * 0.6 + norm(grit) * 0.25
    return s.make_loop(x, 0.3)


replace("Environment/rc_cart_loop", rc)
