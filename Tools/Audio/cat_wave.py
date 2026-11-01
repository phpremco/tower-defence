"""Geluid dat een nieuwe golf vijanden aankondigt. Drie stijlen om uit te kiezen."""
import numpy as np
import sfxlib as s
from sfxlib import n_of, norm, place, lp, hp, bp, tax, env_points

REG = []


def horn(dur, f0, open_to=2500, detune=(1.0, 1.004, 0.996, 1.5, 1.503, 2.0)):
    """Zware sci-fi hoorn ('braaam'): gestapelde zaagtanden, filter dat langzaam opengaat."""
    n = n_of(dur)
    t = tax(dur)
    x = sum(s.osc(np.full(n, f0 * d), "saw") * (0.6 if d > 1.4 else 1.0) for d in detune)
    curve = np.interp(np.linspace(0, n, 120), np.arange(n), 200 + open_to * np.clip(t / (dur * 0.5), 0, 1) ** 1.5)
    x = s.sweep(x, "low", curve)
    x *= env_points(t, [(0, 0), (0.25, 0.8), (dur * 0.6, 1), (dur, 0)]) * (0.9 + 0.1 * np.sin(2 * np.pi * 5 * t))
    return norm(x)


def boom(dur=1.2, f0=70):
    t = tax(dur)
    x = np.sin(s.phase_of(s.glide(t, f0 * 2, f0 * 0.6, 8))) * np.exp(-t / 0.35)
    x += 0.5 * lp(s.white(len(t)), 800) * np.exp(-t / 0.05)
    return norm(x)


def drum(f0=60):
    d = 0.7
    t = tax(d)
    x = np.sin(s.phase_of(s.glide(t, f0 * 2.5, f0, 25))) * np.exp(-t / 0.18)
    x += 0.4 * bp(s.white(len(t)), 200, 2500) * np.exp(-t / 0.02)
    return norm(x)


def tail(x, delays=(0.22, 0.44), gains=(0.3, 0.14)):
    return s.echo(x, list(delays), list(gains), lowpass=2500)


def wave_horn(v):
    """Stijl A: inslag + zware alien-hoorn die aanzwelt ('braaam')."""
    dur = 3.0
    x = np.zeros(n_of(dur))
    place(x, boom(1.4, 60), 0.0, 1.0)
    place(x, horn(2.6, 55), 0.05, 0.9)
    place(x, horn(2.6, 41.2, 1200), 0.05, 0.5)           # onderoctaaf-kwint: dreigender
    return s.stereo(tail(norm(x)), 0.012)


def wave_riser(v):
    """Stijl B: spanning die oploopt (ruis en toon stijgen) en eindigt in een harde klap."""
    dur = 2.6
    n = n_of(dur)
    t = tax(dur)
    hit_t = 1.6
    pre = t < hit_t
    riser = s.sweep(s.pink(n), "band", s.curve_of(100, lambda u: 300 + 5000 * min(u * dur / hit_t, 1) ** 2), width=0.6)
    riser *= np.where(pre, (t / hit_t) ** 2, 0)
    tone_f = 110 * 2 ** (2 * np.clip(t / hit_t, 0, 1) ** 2)
    tone = lp(s.osc(tone_f, "saw") + s.osc(tone_f * 1.01, "saw"), 3000) * np.where(pre, (t / hit_t) ** 3, 0)
    x = norm(riser) * 0.6 + norm(tone) * 0.4
    place(x, boom(1.0, 55), hit_t, 1.3)
    place(x, s.friedlander(0.5, 0.01) * 0.8, hit_t, 1.0)
    return s.stereo(tail(norm(x), (0.2,), (0.2,)), 0.012)


def wave_drums(v):
    """Stijl C: drie zware oorlogstrommen die naderen, met de hoorn eronder."""
    dur = 3.0
    x = np.zeros(n_of(dur))
    for i, (t0, g) in enumerate([(0.0, 0.6), (0.45, 0.8), (0.9, 1.0)]):
        place(x, drum(58 - 4 * i), t0, g)
    place(x, horn(2.0, 55, 1800), 0.9, 0.6)
    place(x, boom(1.4, 50), 0.9, 0.7)
    return s.stereo(tail(norm(x)), 0.012)


for name, fn, title, desc in [
    ("wave_start_horn", wave_horn, "New wave – war horn",
     "An impact followed by a heavy, swelling alien war horn, with a short echo."),
    ("wave_start_riser", wave_riser, "New wave – rising tension",
     "Noise and a tone rise in tension, then end in a hard hit."),
    ("wave_start_drums", wave_drums, "New wave – war drums",
     "Three heavy war drums getting closer, then the horn kicks in."),
]:
    REG.append(dict(path=f"Environment/{name}", title=title, desc=desc, variants=1, fn=fn, save_kw=dict(lufs=-13)))
