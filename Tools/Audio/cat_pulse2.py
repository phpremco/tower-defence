"""Enemy power pulse v2: eigen, buitenaards karakter i.p.v. de hero-pulse.
- Opladen: energie wordt naar binnen gezogen (omgekeerde 'whoosh' + knetter die versnelt)
- Ontlading: een lage, wiebelende alien-'vwoorrm' (FM met snelle vibrato) + organische knal
- Daarna: knetterende restenergie en brokstukken (gruis + steentjes)."""
import numpy as np
import sfxlib as s
from sfxlib import n_of, norm, place, lp, hp, bp, tax, env_points
import cat_pulse as P1

REG = []


def suck_in(dur=0.45):
    n = n_of(dur)
    t = tax(dur)
    rise = (t / dur) ** 2.5
    whoosh = s.sweep(s.pink(n), "band", s.curve_of(80, lambda u: 300 + 3500 * u ** 2), width=0.8) * rise
    crack = s.grains(n, s.poisson_times(lambda q: 40 + 900 * (q / dur) ** 3, dur), (0.0004, 0.002), (2500, 9000),
                     s.rng.uniform(0.1, 1, 2000) ** 2)
    return norm(whoosh) + 0.5 * norm(crack) * rise


def alien_discharge(dur=1.4, v=0):
    n = n_of(dur)
    t = tax(dur)
    base = s.glide(t, [130, 115][v], 42, 3.5)
    vib = 1 + 0.18 * np.sin(2 * np.pi * (22 - 10 * t / dur) * t)          # snel wiebelend, vertragend
    mod = np.sin(s.phase_of(base * 1.5)) * base * 1.2                       # FM: grommend, niet zuiver
    vwoorm = np.sin(s.phase_of(base * vib + mod)) * np.exp(-t / 0.45) * (1 - np.exp(-t / 0.004))
    vwoorm = lp(vwoorm, 1800)
    thump = s.friedlander(dur, 0.012) * 1.2
    goo = s.sweep(s.pink(n), "band", (1500, 250), width=0.7) * np.exp(-t / 0.25)  # organische 'plop' in de knal
    return norm(vwoorm) + 0.6 * lp(thump, 2500) + 0.35 * norm(goo)


def residue(dur=1.2):
    n = n_of(dur)
    return s.grains(n, s.poisson_times(lambda q: 500 * np.exp(-q / 0.3), dur), (0.0003, 0.003), (2000, 10000),
                    s.rng.uniform(0.05, 1, 3000) ** 2)


def enemy_pulse(v):
    dur = 2.4
    n = n_of(dur)
    x = np.zeros(n)
    tc = 0.45
    place(x, suck_in(tc), 0.0, 0.6)
    place(x, alien_discharge(1.6, v), tc, 1.0)
    place(x, norm(residue(1.0)), tc + 0.05, 0.25)
    spray = s.grains(n, s.poisson_times(lambda q: 0 if q < tc else 900 * np.exp(-(q - tc) / 0.15), dur),
                     (0.001, 0.005), (1200, 7000), s.rng.uniform(0.1, 1, 3000))
    rubble = np.zeros(n)
    for t0 in s.poisson_times(lambda q: 30 * np.exp(-((q - (tc + 0.55)) / 0.35) ** 2), dur - 0.1, tc + 0.1):
        place(rubble, P1.stone(s.rng.uniform(0.6, 1.4)), t0, s.rng.uniform(0.3, 1))
    return s.air(x + norm(spray) * 0.22 + norm(rubble) * 0.45)


REG.append(dict(path="Monsters/Shared/enemy_power_pulse", title="Enemy power pulse (with debris)",
                desc="Alien energy pulse: energy is sucked inward, then a low wobbling 'vwoorrm' blast with an organic pop, crackling leftover energy, and debris falling back down.",
                variants=2, fn=enemy_pulse, save_kw=dict(lufs=-12)))
