"""Doodgeluiden ronde 2: dino en gordeldier zonder menselijk stemmodel.
Gebaseerd op de goedgekeurde monster-schreeuwen (s.screech) + dierlijke lagen."""
import numpy as np
import sfxlib as s
from sfxlib import tax, n_of, norm, place, lp, hp, bp, env_ad, env_points
import cat_deaths as D
from cat_fix import clank
import cat_c

REG = []


def snort(dur=0.12, f=400):
    """Neusgeluid/snuif: korte ruisstoot door een lage resonantie (varken-/gordeldierachtig)."""
    n = n_of(dur)
    t = tax(dur)
    x = s.pink(n) * env_points(t, [(0, 0), (0.01, 1), (dur, 0)])
    return norm(s.reson(x, f, 4) + 0.3 * bp(x, f * 2, f * 5))


def dino_death(v):
    dur = 1.7
    n = n_of(dur)
    x = np.zeros(n)
    scr = s.screech(0.85, [560, 500][v], 230, rough=0.85, formant_scale=0.75, harsh=0.55)
    place(x, norm(scr), 0.0, 1.0)
    hiss = bp(s.white(n_of(0.9)), 2500, 8000) * env_points(tax(0.9), [(0, 0), (0.05, 0.6), (0.5, 0.4), (0.9, 0)])
    place(x, norm(hiss), 0.05, 0.35)                                # reptielen-sis door de schreeuw
    rattle = cat_c.chitin_clicks(n_of(0.5), 90, 300, 1500, 0.002, 0.006) * env_points(tax(0.5), [(0, 0.3), (0.2, 1), (0.5, 0)])
    place(x, norm(rattle), 0.55, 0.3)                               # rochelende keel
    place(x, D.body_fall(0.75), 0.95, 1.0)
    place(x, clank(0.2, 300, 1200, 0.4), 0.97, 0.3)
    return x


def armadillo_death(v):
    dur = 1.5
    x = np.zeros(n_of(dur))
    sq = s.screech(0.4, [1300, 1150][v], 650, rough=0.55, formant_scale=1.3, harsh=0.35)
    place(x, norm(sq), 0.0, 0.85)
    for k, t0 in enumerate([0.42, 0.55, 0.66]):                    # snuivende laatste ademtochten
        place(x, snort(0.1, [380, 340, 300][k]), t0, 0.55 - 0.12 * k)
    for k in range(6):
        place(x, cat_c.shell_knock(s.rng.uniform(600, 1200)), 0.75 + 0.05 * k + s.rng.uniform(0, 0.02), 0.35 - 0.04 * k)
    place(x, D.body_fall(0.7), 0.75, 1.0)
    return x


for name, fn, title, desc in [
    ("DinoSoldier/dino_soldier_death", dino_death, "Dino soldier dies",
     "A rough, hissing reptile shriek with a rattling throat, then a heavy fall with a dull gear knock. No human voice."),
    ("Armadillo/armadillo_death", armadillo_death, "Armadillo creature dies",
     "A short animal squeal, a few snorting last breaths, then the armour rings clattering as it collapses. No human voice."),
]:
    REG.append(dict(path=f"Monsters/{name}", title=title, desc=desc, variants=2, fn=fn, save_kw=dict(lufs=-13)))
    REG.append(dict(path=f"Bosses/{name}_boss", title="BOSS – " + title, desc="Same sound with more bass.", variants=2,
                    fn=(lambda f: (lambda v: D.boss_more_bass(f(v))))(fn), save_kw=dict(lufs=-11)))
