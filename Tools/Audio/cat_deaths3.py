"""Boss-doodgeluiden dino + gordeldier: zelfde geluid, flink meer bas, ook ONDER de schreeuw
(niet alleen in de val), via een lage bromlaag die de schreeuw volgt."""
import numpy as np
import sfxlib as s
from sfxlib import n_of, norm, lp, tax, env_points
import cat_deaths2 as D2

REG = []


def heavy_bass(x, voice_end):
    x = norm(x)
    n = len(x)
    t = np.arange(n) / s.SR
    env = lp(np.abs(x), 25)
    env = env / (env.max() + 1e-9)
    voice = np.clip((voice_end - t) / 0.1, 0, 1)                 # alleen tijdens de schreeuw
    rumble = norm(lp(s.brown(n), 110, 4)) * env * voice
    drone = np.sin(s.phase_of(s.glide(t, 75, 48, 1.5))) * env * voice
    low = norm(lp(x, 220, 4))
    return norm(x + 1.6 * low + 1.3 * norm(rumble) + 0.9 * norm(drone))


for name, fn, title, vend in [("DinoSoldier/dino_soldier_death", D2.dino_death, "Dino soldier dies", 0.9),
                              ("Armadillo/armadillo_death", D2.armadillo_death, "Armadillo creature dies", 0.75)]:
    REG.append(dict(path=f"Bosses/{name}_boss", title="BOSS – " + title, desc="Same sound with a lot more bass, also under the scream.", variants=2,
                    fn=(lambda f, ve: (lambda v: heavy_bass(f(v), ve)))(fn, vend), save_kw=dict(lufs=-11)))
