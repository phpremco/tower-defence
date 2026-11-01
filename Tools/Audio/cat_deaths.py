"""Ontbrekende doodgeluiden: wolf, stingray, dino-soldaat, gordeldier (+ boss = zelfde, meer bas).
Stemmen via het glottale stemmodel dat bij de hero-death is goedgekeurd (cat_fix2.vocal)."""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points
import cat_fix2 as F2
from cat_fix import step, clank
import cat_c

REG = []


def register(path, title, desc, fn, variants=2, lufs=-13):
    e = dict(path=path, title=title, desc=desc, variants=variants, fn=fn, save_kw=dict(lufs=lufs))
    REG.append(e)
    return e


def boss_more_bass(x):
    """Boss: hetzelfde geluid, alleen meer bas (lichte sub-laag en lage plank), geen vervorming."""
    x = norm(x)
    env = lp(np.abs(x), 30)
    sub = norm(lp(s.brown(len(x)), 90, 4)) * env / (env.max() + 1e-9)
    y = x + 0.9 * norm(lp(x, 220, 2)) + 0.5 * sub
    return norm(y)


def body_fall(weight=0.6, dur=0.6):
    n = n_of(dur)
    x = np.zeros(n)
    place(x, s.thump(0.4, 130 - 40 * weight, 45, 18, 0.05 + 0.06 * weight, 0.6, 600), 0.0, 1.0)
    place(x, s.thump(0.3, 150 - 40 * weight, 55, 22, 0.04, 0.5, 700), 0.12 + 0.05 * weight, 0.45)
    return x


DOG = [(900, 150), (1700, 200), (2900, 300), (4000, 400)]
REPT = [(450, 120), (1100, 180), (2300, 260), (3300, 350)]


# ------------------------------------------------------------ wolf
def wolf_death(v):
    dur = 1.5
    x = np.zeros(n_of(dur))
    p0 = [700, 640][v]
    yelp = F2.vocal(0.2, [(0, p0 * 0.8), (0.04, p0 * 1.3), (0.2, p0 * 0.9)], DOG, 0.3,
                    amp_pts=[(0, 0), (0.01, 1), (0.14, 0.7), (0.2, 0)])
    place(x, norm(yelp), 0.0, 1.0)
    for k, t0 in enumerate([0.3, 0.6]):                      # twee wegstervende jankjes
        wh = F2.vocal(0.3, [(0, p0 * 1.4), (0.3, p0 * 1.0)], DOG, 0.55,
                      amp_pts=[(0, 0), (0.05, 1), (0.3, 0)])
        place(x, norm(wh), t0, 0.5 - 0.15 * k)
    place(x, step(0.4, "paw", "dirt"), 0.85, 0.35)
    place(x, body_fall(0.4), 0.9, 0.7)
    place(x, norm(F2.exhale(0.35)), 1.1, 0.2)
    return x


# ------------------------------------------------------------ dino-soldaat
def dino_death(v):
    dur = 1.9
    n = n_of(dur)
    x = np.zeros(n)
    p = [120, 105][v]
    roar = F2.vocal(1.0, [(0, p * 1.3), (0.15, p * 1.5), (0.6, p * 1.1), (1.0, p * 0.7)], REPT, 0.45, fry_from=0.65,
                    amp_pts=[(0, 0), (0.04, 1), (0.6, 0.8), (1.0, 0)])
    roar = roar * s.rumble_mod(len(roar), 40, 0.5)          # ruw, reptielachtig
    place(x, norm(roar), 0.0, 1.0)
    hiss = bp(s.white(n_of(0.6)), 2500, 8000) * env_points(tax(0.6), [(0, 0), (0.05, 1), (0.6, 0)])
    place(x, norm(hiss), 0.85, 0.35)
    place(x, body_fall(0.75), 1.05, 1.0)
    place(x, clank(0.2, 300, 1200, 0.4), 1.07, 0.3)          # uitrusting, dof
    return x


# ------------------------------------------------------------ gordeldier
def armadillo_death(v):
    dur = 1.7
    x = np.zeros(n_of(dur))
    p = [260, 230][v]
    squeal = F2.vocal(0.45, [(0, p), (0.08, p * 1.4), (0.45, p * 0.7)], F2.UH, 0.35,
                      amp_pts=[(0, 0), (0.02, 1), (0.3, 0.6), (0.45, 0)])
    place(x, norm(squeal * s.rumble_mod(len(squeal), 35, 0.4)), 0.0, 0.9)
    grunt = F2.vocal(0.35, [(0, 140), (0.35, 90)], F2.OH, 0.4, fry_from=0.15,
                     amp_pts=[(0, 0), (0.03, 0.8), (0.35, 0)])
    place(x, norm(grunt), 0.5, 0.6)
    for k in range(6):                                       # pantserringen klapperen bij het neervallen
        place(x, cat_c.shell_knock(s.rng.uniform(600, 1200)), 0.8 + 0.05 * k + s.rng.uniform(0, 0.02), 0.35 - 0.04 * k)
    place(x, body_fall(0.7), 0.8, 1.0)
    return x


# ------------------------------------------------------------ stingray
def stingray_death(v):
    dur = 1.6
    n = n_of(dur)
    x = np.zeros(n)
    shriek = s.screech(0.6, [950, 850][v], 420, rough=0.6, formant_scale=0.9, harsh=0.3)
    place(x, norm(shriek), 0.0, 0.8)
    fall = s.sweep(s.pink(n_of(0.6)), "band", (1800, 350), width=1.0) * env_points(tax(0.6), [(0, 0), (0.15, 1), (0.6, 0.3)])
    place(x, norm(fall), 0.3, 0.5)                           # valt uit de lucht
    flutter = bp(s.white(n_of(0.4)), 600, 2500) * (0.5 + 0.5 * np.sin(2 * np.pi * 14 * tax(0.4))) * env_ad(tax(0.4), 0.02, 0.15)
    place(x, norm(flutter), 0.35, 0.3)
    slap = lp(s.white(n_of(0.08)), 3000) * np.exp(-np.arange(n_of(0.08)) / s.SR / 0.012)
    place(x, norm(slap), 0.9, 0.7)                           # platte klap op de grond
    place(x, body_fall(0.5), 0.9, 0.8)
    place(x, norm(s.squelch_sound(0.35, 0.6)), 0.95, 0.25)
    return x


for name, fn, title, desc in [
    ("Wolf/wolf_death", wolf_death, "Wolf-like creature dies",
     "A sharp canine yelp, two fading whimpers, then the body dropping."),
    ("DinoSoldier/dino_soldier_death", dino_death, "Dino soldier dies",
     "A rough reptilian death roar that breaks into a croak, a hiss, then a heavy fall with a dull gear knock."),
    ("Armadillo/armadillo_death", armadillo_death, "Armadillo creature dies",
     "A short squeal and grunt, then the armour rings clattering as it collapses."),
    ("Stingray/stingray_death", stingray_death, "Stingray dies",
     "A gurgling shriek, a falling flutter out of the air and a flat wet slap on the ground."),
]:
    register(f"Monsters/{name}", title, desc, fn)
    register(f"Bosses/{name}_boss", "BOSS – " + title, "Same sound with more bass.",
             (lambda f: (lambda v: boss_more_bass(f(v))))(fn), lufs=-11)
