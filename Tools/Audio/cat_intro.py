"""Opening cutscene sound effects (Docs/AUDIO.md, "Wanted: opening cutscene music and sounds", 2026-11-11).

Built from Remco's source recordings (decoded to src_intro/ by decode_sources.py, run in Blender), the approved
creature sounds, Kenney Sci-Fi (CC0) and sfxlib synthesis. Music is out of scope.

Usage (system Python with numpy/scipy/pyloudnorm):
  python cat_intro.py            render everything, write out/intro/manifest.md and print the checks
  python cat_intro.py --only X   render only entries whose path contains X (manifest is not rewritten)

Conventions: in-world one-shots mono, beds and ambience stereo, 44.1 kHz 16-bit. Loops are built circularly
(wrap-around placement, FFT-periodic noise, filters run over three copies) or from recordings with an
equal-power crossfade at the seam, then mastered with master(loop=True). One-shot peaks stay at or under -1 dBFS.
"""
import os
import sys
import warnings
import zlib
from fractions import Fraction

import numpy as np
from scipy.io import wavfile
from scipy.signal import fftconvolve, resample_poly

import sfxlib as s
from sfxlib import SR, n_of, tax, norm, pad, place, lp, hp, bp, env_points

warnings.filterwarnings("ignore", message=r"Chunk \(non-data\) not understood")

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src_intro")
AUDIO = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Audio"))
APPROVED = os.path.join(AUDIO, "Approved")
OUT_DIR = os.path.join(s.OUT, "intro")
PEAK_CEILING = 10 ** (-1.05 / 20)      # one-shots: at or under -1 dBFS after 16-bit rounding

INTRO = []                              # (path, checklist item, sources, processing, loop, lufs, fn, variants)


def intro(path, item, sources, processing, lufs, loop=False, variants=1, comp=True, exact=False):
    """exact: no silence trimming in mastering, so event times inside the file stay where they were built."""
    def deco(fn):
        INTRO.append(dict(path=path, item=item, sources=sources, processing=processing, lufs=lufs,
                          loop=loop, variants=variants, comp=comp, exact=exact, fn=fn))
        return fn
    return deco


# ================================================================== loading
_cache = {}


def _read(path, mono):
    key = (path, mono)
    if key not in _cache:
        sr, x = wavfile.read(path)
        x = x.astype(float) / (32768.0 if x.dtype == np.int16 else 1.0)
        if sr != SR:
            fr = Fraction(SR, sr).limit_denominator(1000)
            x = resample_poly(x, fr.numerator, fr.denominator, axis=0)
        if mono and x.ndim == 2:
            x = x.mean(axis=1)
        if not mono and x.ndim == 1:
            x = np.stack([x, x], 1)
        _cache[key] = x
    return _cache[key]


def src(name, t0=0.0, t1=None, mono=True):
    """Slice of a decoded source recording (src_intro/<name>.wav)."""
    x = _read(os.path.join(SRC, name + ".wav"), mono)
    a = n_of(t0) if t0 > 0 else 0
    b = len(x) if t1 is None else min(len(x), n_of(t1))
    return x[a:b].copy()


def approved(rel, mono=True):
    return _read(os.path.join(APPROVED, rel), mono).copy()


# ================================================================== small tools
def fade(x, fin=0.005, fout=0.03):
    """Raised-cosine fades at both cut points (no clicks)."""
    x = np.array(x, float)
    for length, rev in ((fin, False), (fout, True)):
        k = min(n_of(length), len(x) // 2) if length > 0 else 0
        if k < 2:
            continue
        w = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, k))
        if rev:
            w = w[::-1]
        sl = slice(len(x) - k, None) if rev else slice(0, k)
        x[sl] *= w if x.ndim == 1 else w[:, None]
    return x


def per_ch(fn, x):
    return fn(x) if x.ndim == 1 else np.stack([fn(x[:, c]) for c in range(x.shape[1])], 1)


def circ(fn, x):
    """Run a (stateful) filter over three copies and keep the middle one: the result is still periodic."""
    n = len(x)
    y = fn(np.concatenate([x, x, x]))
    return y[n:2 * n]


def loop_xfade(x, xf, power=True):
    """Seamless loop from a longer take: the last `xf` seconds fade over the start."""
    k = n_of(xf)
    body = np.array(x[:-k], float)
    tail = x[-k:]
    w = np.linspace(0, 1, k)
    a, b = (np.sqrt(w), np.sqrt(1 - w)) if power else (w, 1 - w)
    if body.ndim == 2:
        a, b = a[:, None], b[:, None]
    body[:k] = body[:k] * a + tail * b
    return body


def place_wrap(dst, x, at, gain=1.0, pan=None):
    """Mix x into a loop buffer at sample `at`, wrapping around the end. Mono x into stereo dst pans equal-power."""
    n = len(dst)
    at = int(at) % n
    if dst.ndim == 2 and x.ndim == 1:
        p = 0.5 if pan is None else pan
        x = np.stack([x * np.cos(p * np.pi / 2), x * np.sin(p * np.pi / 2)], 1)
    x = x[:n]
    first = min(len(x), n - at)
    dst[at:at + first] += x[:first] * gain
    if first < len(x):
        dst[:len(x) - first] += x[first:] * gain
    return dst


def place_clip(dst, x, at, gain=1.0, pan=None):
    """Like place_wrap, but whatever runs past the end is dropped (one-shots)."""
    room = len(dst) - int(at)
    return place_wrap(dst, x[:room], at, gain, pan) if room > 0 else dst


def cnoise(n, lo=20.0, hi=20000.0, slope=-1.0):
    """FFT-shaped noise that is exactly periodic in n. slope: 0 white, -1 pink, -2 brown (power per octave)."""
    X = s.rng.standard_normal(n // 2 + 1) + 1j * s.rng.standard_normal(n // 2 + 1)
    f = np.fft.rfftfreq(n, 1 / SR)
    f[0] = 1
    X *= f ** (slope / 2) * ((f >= lo) & (f <= hi))
    y = np.fft.irfft(X, n)
    return y / (np.std(y) + 1e-12)


def csmooth(n, hz, lo=0.0, hi=1.0):
    """Slow periodic random curve (lo..hi)."""
    y = cnoise(n, 0.01, hz, 0.0)
    y = (y - y.min()) / (np.ptp(y) + 1e-12)
    return lo + (hi - lo) * y


def snap(f, length_s):
    """Round a frequency to a whole number of cycles in the loop."""
    return max(1, round(f * length_s)) / length_s


def vread(x, rate):
    """Variable-speed playback (rate per output sample, 1 = original); pitch and speed change together."""
    pos = np.cumsum(rate) - rate[0]
    pos = pos[pos < len(x) - 1]
    return per_ch(lambda c: np.interp(pos, np.arange(len(c)), c), x)


def shift(x, factor):
    return per_ch(lambda c: s.pitch_shift(c, factor), x)


def stereo_verb(x, dur, decay, dark0, dark1, predelay, wet, dry=1.0):
    """Mono in, stereo out: two decorrelated dark tails (distance, big spaces)."""
    tails = []
    for _ in range(2):
        ir = s.reverb_ir(dur, decay, dark0, dark1, predelay)
        tails.append(fftconvolve(x, ir))
    n = len(tails[0])
    return np.stack([pad(x, n) * dry + tails[0] * wet, pad(x, n) * dry + tails[1] * wet], 1)


def onsets(x, rise_db=12.0, min_gap=0.22, frame=0.005, floor_db=40.0):
    """Sample positions where the level jumps by `rise_db` (footsteps, breaths)."""
    h = n_of(frame)
    m = len(x) // h
    db = 20 * np.log10(np.sqrt(np.mean(x[:m * h].reshape(m, h) ** 2, axis=1)) + 1e-9)
    top = db.max()
    out = []
    for i in range(6, m):
        if db[i] - db[i - 6:i].min() > rise_db and db[i] > top - floor_db:
            if not out or (i - out[-1]) * frame > min_gap:
                out.append(i)
    return [max(0, (i - 2) * h) for i in out]


def pick_steps(name, count, max_len=0.5, min_len=0.2):
    """Cleanest single steps of a footstep recording: loud attack over a quiet run-up, not cut short."""
    x = src(name)
    ons = onsets(x, min_gap=0.4)           # one onset per step, not per gravel crunch
    cands = []
    for k, o in enumerate(ons):
        nxt = ons[k + 1] if k + 1 < len(ons) else len(x)
        end = min(o + n_of(max_len), nxt - n_of(0.015))
        if end - o < n_of(min_len) or o < n_of(0.06):
            continue
        seg = x[o:end]
        pre = x[o - n_of(0.05):o]
        pk = 20 * np.log10(np.max(np.abs(seg[:n_of(0.06)])) + 1e-9)
        pre_db = 20 * np.log10(np.sqrt(np.mean(pre ** 2)) + 1e-9)
        cands.append((pk - pre_db + 0.3 * pk, o, end))
    cands.sort(reverse=True)
    chosen = sorted(cands[:count], key=lambda c: c[1])
    return [fade(x[max(0, o - n_of(0.004)):e], 0.003, 0.06) for _, o, e in chosen]


# ================================================================== shared building blocks
def creak(dur, freqs=(520, 1340, 2650), rate=(28, 70), q=9.0):
    """Stick-slip metal creak: an irregular pulse train through a few narrow resonances."""
    n = n_of(dur)
    t = np.arange(n) / SR
    r = rate[0] + (rate[1] - rate[0]) * s.smooth_rand(n, 3)
    ph = np.cumsum(r / SR)
    pulses = np.zeros(n)
    idx = np.nonzero(np.diff(np.floor(ph)) > 0)[0]
    pulses[idx] = s.rng.uniform(0.4, 1.0, len(idx))
    pulses = lp(pulses, 6000)
    body = sum(s.reson(pulses, f * s.rng.uniform(0.95, 1.05), q) * g for f, g in zip(freqs, (1.0, 0.6, 0.35)))
    return norm(body) * env_points(t, [(0, 0), (dur * 0.15, 1), (dur * 0.7, 0.8), (dur, 0)])


def booster_burn_raw(length=5.0):
    """Booster roar: the crackle recording plus a periodic synthetic roar and crackle (loop-ready)."""
    rec = src("rocket-booster-crackle", 0.3, 0.3 + length + 0.5)
    rec = loop_xfade(rec, 0.5)
    n = len(rec)
    roar = circ(lambda y: y, cnoise(n, 120, 3500, -1.0)) * csmooth(n, 9, 0.55, 1.0)
    crack = np.zeros(n)
    for t0 in np.sort(s.rng.uniform(0, n, int(28 * n / SR))):
        L = n_of(s.rng.uniform(0.001, 0.006))
        g = bp(s.white(L), 1500, 8000) * np.exp(-np.linspace(0, 5, L))
        place_wrap(crack, g, t0, s.rng.uniform(0.2, 1.0))
    return norm(rec) + 0.3 * norm(roar) + 0.22 * norm(crack)


def chitter_layers(n, stereo=True, density=1.0, wrap=True):
    """Insect swarm texture: the approved ant walk loop, pitched-up screech fragments and mandible click trains."""
    dst = np.zeros((n, 2)) if stereo else np.zeros(n)
    put = place_wrap if wrap else place_clip
    walk = approved("Monsters/Ant/ant_walk_loop.wav")
    reps = int(np.ceil(n / len(walk))) + 1
    walk = np.tile(walk, reps)[:n]
    if stereo:
        dst[:, 0] += 0.5 * np.roll(walk, n_of(0.9))
        dst[:, 1] += 0.5 * np.roll(walk, n_of(2.3))
    else:
        dst += 0.5 * walk
    screeches = [approved("Monsters/Ant/ant_screech_01.wav"), approved("Monsters/Ant/ant_screech_02.wav")]
    for t0 in np.sort(s.rng.uniform(0, n, int(14 * density * n / SR))):
        sc = screeches[s.rng.integers(2)]
        f = s.rng.uniform(1.3, 2.1)
        frag_len = n_of(s.rng.uniform(0.05, 0.2))
        a = s.rng.integers(0, max(1, len(sc) - n_of(0.3)))
        frag = fade(s.pitch_shift(sc[a:a + int(frag_len * f) + 8], f), 0.004, 0.03)
        put(dst, hp(frag, 900), t0, s.rng.uniform(0.08, 0.3), pan=s.rng.uniform(0.1, 0.9))
    for t0 in np.sort(s.rng.uniform(0, n, int(7 * density * n / SR))):
        k = s.rng.integers(3, 10)
        rate = s.rng.uniform(22, 48)
        fc = s.rng.uniform(2500, 6000)
        train = np.zeros(n_of(k / rate + 0.02))
        for j in range(k):
            place(train, s.click(0.008, fc * 0.7, fc * 1.4, 0.0012), j / rate, s.rng.uniform(0.4, 1.0))
        put(dst, train, t0, s.rng.uniform(0.15, 0.4), pan=s.rng.uniform(0.05, 0.95))
    return dst


def helmet(x, room=1.0):
    """Breath inside a closed helmet: band-limited, slightly muffled, a visor resonance and a few-ms box."""
    y = bp(x, 250, 4000)
    y = 0.65 * lp(y, 2600) + 0.35 * y + 0.18 * s.reson(y, 1150, 4)
    ir = np.zeros(n_of(0.03))
    ir[0] = 1.0
    for dt, g in ((0.0013, 0.42), (0.0027, -0.3), (0.0041, 0.22), (0.0063, -0.14), (0.0089, 0.08)):
        ir[n_of(dt)] += g * room
    ir += s.white(len(ir)) * np.exp(-np.arange(len(ir)) / n_of(0.004)) * 0.04 * room
    return fftconvolve(y, ir)[:len(y)]


def air_hiss(n, level):
    """Faint suit air supply under the breathing (periodic)."""
    return cnoise(n, 1500, 7000, -1.0) * csmooth(n, 0.4, 0.7, 1.0) * level


_breaths = None


def breaths():
    """Breath events of male-breathing as (inhale, exhale) pairs; the short gap marks a pair."""
    global _breaths
    if _breaths is None:
        x = src("male-breathing")
        band = bp(x, 600, 4000)
        h = n_of(0.02)
        m = len(band) // h
        db = 20 * np.log10(np.sqrt(np.mean(band[:m * h].reshape(m, h) ** 2, axis=1)) + 1e-9)
        on = db > np.percentile(db, 20) + 12
        segs, i = [], 0
        while i < m:
            if on[i]:
                j = i
                while j < m and on[j]:
                    j += 1
                segs.append([i, j])
                i = j
            else:
                i += 1
        merged = []
        for a, b in segs:
            if merged and (a - merged[-1][1]) * 0.02 < 0.08:
                merged[-1][1] = b
            else:
                merged.append([a, b])
        merged = [(a, b) for a, b in merged if (b - a) * 0.02 >= 0.2]
        clips = [fade(x[max(0, a * h - n_of(0.06)):min(len(x), b * h + n_of(0.12))], 0.04, 0.1) for a, b in merged]
        gaps = [(merged[k + 1][0] - merged[k][1]) * 0.02 for k in range(len(merged) - 1)]
        pairs, k = [], 0
        while k + 1 < len(clips):
            if gaps[k] < 0.45:
                pairs.append((clips[k], clips[k + 1]))
                k += 2
            else:
                k += 1
        _breaths = pairs
    return _breaths


def trim_breath(b, max_len, fout=0.08):
    return fade(b[:n_of(max_len)], 0.02, fout) if len(b) > n_of(max_len) else b


def breathing_loop(cycle, cycles, speed, max_in, max_out, gap, jitter=0.06):
    pairs = breaths()
    n = n_of(cycle * cycles)
    dst = np.zeros(n)
    t = 0.05
    for c in range(cycles):
        inh, exh = pairs[c % len(pairs)]
        inh = s.pitch_shift(trim_breath(inh, max_in), speed)
        exh = s.pitch_shift(trim_breath(exh, max_out), speed)
        place_wrap(dst, inh, n_of(t), s.rng.uniform(0.8, 1.0))
        place_wrap(dst, exh, n_of(t) + len(inh) + n_of(gap), s.rng.uniform(0.9, 1.05))
        t += cycle * s.rng.uniform(1 - jitter, 1 + jitter)
    y = circ(helmet, dst)
    return y + air_hiss(n, np.sqrt(np.mean(y ** 2)) * 0.12)


def radio_chain(x, drive=2.2):
    return np.tanh(bp(x, 320, 3400) * drive) / np.tanh(drive)


# ================================================================== Orbit
@intro("Orbit/orbit_space_drone_loop", "Space ambience: low dark drone bed",
       "space-rumble.mp3", "lowpassed rumble + FFT-periodic brown noise + slowly beating low sine partials, "
       "faint airy shimmer; crossfaded seam, stereo", -24, loop=True)
def orbit_space_drone(v):
    L = 24.0
    rec = loop_xfade(src("space-rumble", 3.0, 3.0 + L + 1.0, mono=False), 1.0)
    n = len(rec)
    Ls = n / SR
    rec = circ(lambda y: per_ch(lambda c: lp(c, 260, 4), y), rec)
    t = np.arange(n) / SR
    out = np.zeros((n, 2))
    for c in range(2):
        drone = sum(a * np.sin(2 * np.pi * snap(f, Ls) * t + s.rng.uniform(0, 6.28))
                    for f, a in ((36.7, 1.0), (36.7 + 0.25, 0.7), (55.0, 0.5), (73.4 + 0.15 * c, 0.3), (110.1, 0.12)))
        drone *= csmooth(n, 0.12, 0.6, 1.0)
        dark = cnoise(n, 20, 160, -2.0) * csmooth(n, 0.2, 0.5, 1.0)
        shimmer = cnoise(n, 1800, 4500, -1.0) * csmooth(n, 0.08, 0.0, 1.0) ** 2
        out[:, c] = norm(rec[:, c]) * 0.8 + norm(drone) * 0.55 + norm(dark) * 0.5 + norm(shimmer) * 0.04
    return out


@intro("Orbit/orbit_mothership_hum_loop", "Mothership engine hum (loop)",
       "spaceship-rumble.mp3", "engine rumble section (crossfaded seam) + periodic electrical hum with harmonics "
       "and a soft turbine whine that breathes; stereo", -22, loop=True)
def orbit_mothership_hum(v):
    rec = loop_xfade(src("spaceship-rumble", 20.0, 33.0, mono=False), 1.0)
    n = len(rec)
    Ls = n / SR
    t = np.arange(n) / SR
    f0 = snap(46.0, Ls)
    hum = sum(a * np.sin(2 * np.pi * f0 * k * t) for k, a in ((1, 1.0), (2, 0.55), (3, 0.35), (4, 0.2), (6, 0.1)))
    whine = np.sin(2 * np.pi * snap(f0 * 18, Ls) * t) * csmooth(n, 0.3, 0.2, 1.0)
    whine += 0.4 * np.sin(2 * np.pi * snap(f0 * 18 * 1.502, Ls) * t) * csmooth(n, 0.25, 0.0, 1.0)
    out = np.zeros((n, 2))
    for c in range(2):
        out[:, c] = norm(rec[:, c]) + 0.35 * norm(hum) + 0.035 * norm(whine) * (1 if c == 0 else 0.8)
    return out


@intro("Orbit/orbit_cargo_bay_doors", "Cargo bay doors opening (hydraulics, metal)",
       "hydraulic-door-opening.mp3", "full take trimmed, a sub layer that follows its envelope for size, "
       "a low metal unlatch thump at the start; mono", -18)
def orbit_cargo_doors(v):
    x = fade(src("hydraulic-door-opening", 0.2, 6.43), 0.01, 0.4)
    env = lp(np.abs(x), 12)
    sub = lp(s.brown(len(x)), 90, 4)
    sub = norm(sub) * env / (env.max() + 1e-9)
    out = norm(x) + 0.5 * sub
    place(out, norm(s.thump(0.6, 90, 40, 12, 0.18, 0.3, 500)) * 0.45, 0.02)
    place(out, s.metal_ring(1.2, [143, 371, 812], [0.45, 0.25, 0.12], [1, 0.5, 0.3]) * 0.12, 0.03)
    return out


@intro("Orbit/orbit_clamps_release", "Docking clamps releasing (clack, latch)",
       "mechanical-clamp.mp3, Kenney impactMetal_002", "cut to the release: 0.2 s of motor pre-roll, the clank "
       "burst and its decay; a tight latch tick in front; mono", -16)
def orbit_clamps(v):
    x = fade(src("mechanical-clamp", 2.18, 3.75), 0.02, 0.35)
    out = pad(norm(x), len(x) + n_of(0.05))
    tick = s.pitch_shift(src("kenney_impactMetal_002"), 1.3)
    place(out, norm(tick) * 0.35, 0.12)
    return out


@intro("Orbit/orbit_mothership_alarm_loop", "Mothership alarm (extra, usable loop)",
       "spaceship-alarm.mp3", "two alarm periods (period found by autocorrelation, ~1.9 s), cut in the quiet "
       "between tones, 40 ms linear crossfade, normalised; mono", -20, loop=True)
def orbit_alarm(v):
    x = src("spaceship-alarm")
    h = n_of(0.001)
    m = len(x) // h
    e = np.sqrt(np.mean(x[:m * h].reshape(m, h) ** 2, axis=1))
    e -= e.mean()
    ac = np.correlate(e, e, "full")[m - 1:]
    lo, hi = 1500, 2400
    period = (np.argmax(ac[lo:hi]) + lo) * h
    start = n_of(0.2)
    xf = n_of(0.04)
    return loop_xfade(x[start:start + 2 * period + xf], 0.04, power=False)


# ================================================================== Launch
@intro("Launch/launch_cold_gas_puff", "Cold-gas puff at release (short hiss)",
       "release-hiss.mp3", "trimmed hiss + a short low pneumatic puff under the attack; mono", -18)
def launch_puff(v):
    x = fade(src("release-hiss", 0.04, 1.1), 0.003, 0.25)
    t = tax(0.4)
    puff = lp(s.pink(len(t)), 450) * s.env_ad(t, 0.004, 0.07)
    out = norm(x)
    place(out, norm(puff) * 0.5, 0.0)
    return out


@intro("Launch/launch_booster_ignition", "Booster ignition (whump)",
       "spaceship-shuttle-firing.mp3, Kenney lowFrequency_explosion_000", "synth sub whump (falling sine + "
       "air pressure puff) + the shuttle-firing onset faded after 1.4 s + low explosion body; mono", -14)
def launch_ignition(v):
    dur = 2.4
    out = np.zeros(n_of(dur))
    place(out, norm(s.thump(1.6, 120, 32, 9, 0.45, 0.5, 600)), 0.0, 1.0)
    t = tax(0.6)
    whoomp = s.sweep(s.pink(len(t)), "band", (900, 120), width=1.2) * s.env_ad(t, 0.01, 0.12)
    place(out, norm(whoomp) * 0.6, 0.0)
    place(out, norm(fade(src("spaceship-shuttle-firing", 0.07, 2.4), 0.003, 0.9)) * 0.85, 0.01)
    body = lp(src("kenney_lowFrequency_explosion_000"), 300)
    place(out, norm(fade(body, 0.002, 0.8)) * 0.45, 0.0)
    return out


@intro("Launch/launch_booster_burn_loop", "Booster burn (loop, rumbling roar)",
       "rocket-booster-crackle.mp3", "5 s of the crackle recording (crossfaded seam) + periodic mid roar and "
       "sparse sharp crackle grains placed around the loop; mono", -16, loop=True)
def launch_burn(v):
    return booster_burn_raw(5.0)


@intro("Launch/launch_stage_separation", "Stage separation (bang, metal, pyro bolts)",
       "Kenney explosionCrunch_000, Kenney lowFrequency_explosion_001, mechanical-clamp.mp3",
       "two sharp pyro-bolt cracks 45 ms apart (pressure pulse + 4 ms broadband burst), a short explosive crunch "
       "and low thump, the clamp's mechanical release clank, a brief falling gas/debris whoosh; no tonal layers; mono",
       -14)
def launch_separation(v):
    out = np.zeros(n_of(1.8))
    for at, g in ((0.0, 1.0), (0.045, 0.85)):
        burst = s.white(n_of(0.004)) * np.exp(-np.arange(n_of(0.004)) / n_of(0.0012))
        crack = s.mix(hp(s.friedlander(0.12, 0.0015), 250), 0.7 * burst)
        place(out, norm(crack) * g, at)
    place(out, norm(fade(src("kenney_explosionCrunch_000"), 0.002, 0.3)) * 0.75, 0.01)
    low = lp(src("kenney_lowFrequency_explosion_001", 0.03, 1.0), 400)
    place(out, norm(fade(low, 0.003, 0.4)) * 0.7, 0.0)
    place(out, norm(fade(src("mechanical-clamp", 2.33, 2.95), 0.002, 0.2)) * 0.55, 0.07)
    t = tax(0.6)
    whoosh = s.sweep(s.pink(len(t)), "band", (2500, 350), width=1.2) * s.env_ad(t, 0.02, 0.18)
    place(out, norm(whoosh) * 0.3, 0.05)
    return out


@intro("Launch/launch_booster_fade_distance", "Booster sound fading into the distance",
       "rocket-booster-crackle.mp3 (via the burn loop)", "burn tiled to 9 s, speed/pitch easing down 7 %, "
       "lowpass closing 7 kHz to 300 Hz, level falling, a dark distance tail; mono", -20)
def launch_fade(v):
    burn = booster_burn_raw(5.0)
    x = np.tile(burn, 3)
    n_out = n_of(8.5)
    rate = np.linspace(1.0, 0.93, n_out) ** 1.0
    y = vread(x, rate)[:n_out]
    t = np.arange(len(y)) / SR
    y = s.sweep(y, "low", s.curve_of(150, lambda u: 7000 * (300 / 7000) ** (u ** 0.7)))
    y *= np.exp(-t / 2.0)
    y = fade(y, 0.002, 1.5)
    ir = s.reverb_ir(1.6, 0.45, 3000, 400, 0.03)
    return norm(y) + 0.35 * norm(fftconvolve(y, ir))[:len(y)]


# ================================================================== Re-entry
@intro("Reentry/reentry_plasma_roar_loop", "Re-entry plasma roar",
       "rocket-booster-crackle.mp3", "per channel: periodic pink roar through a wandering band-pass, deep "
       "brown rumble, fizzing high band and dense crackle grains; booster recording lowpassed under it; stereo",
       -18, loop=True)
def reentry_plasma(v):
    rec = loop_xfade(src("rocket-booster-crackle", 0.2, 6.0), 0.5)
    n = len(rec)
    rec = circ(lambda y: lp(y, 1500), rec)
    low = circ(lambda y: y, cnoise(n, 25, 180, -2.0)) * csmooth(n, 6, 0.4, 1.0)
    out = np.zeros((n, 2))
    blocks = 120
    for c in range(2):
        roar = cnoise(n, 60, 6000, -1.0) * csmooth(n, 3, 0.5, 1.0)
        cur = np.interp(np.linspace(0, n, blocks), np.arange(n), 450 + 1400 * csmooth(n, 0.8))
        roar = circ(lambda y: s.sweep(y, "band", np.tile(cur, 3), width=1.4), roar)
        fizz = cnoise(n, 5000, 14000, 0.0) * csmooth(n, 12, 0.15, 1.0)
        crack = np.zeros(n)
        for t0 in np.sort(s.rng.uniform(0, n, int(45 * n / SR))):
            L = n_of(s.rng.uniform(0.001, 0.005))
            g = bp(s.white(L), 2000, 9000) * np.exp(-np.linspace(0, 5, L))
            place_wrap(crack, g, t0, s.rng.uniform(0.2, 1.0))
        out[:, c] = (norm(roar) + 0.6 * norm(low) + 0.12 * norm(fizz) + 0.18 * norm(crack)
                     + 0.45 * norm(rec))
    return out


@intro("Reentry/reentry_capsule_rattle_loop", "Capsule rattling and creaking under load",
       "metal-creaking.mp3", "a low 30-150 Hz hull vibration as the bed, with the recording's two creak phrases on top "
       "(~7 dB lower than before relative to the bed) placed six times at pitches 0.78-1.06 with varied gains; "
       "all creaks sit between 0.4 s and 9.8 s so the seam falls on the bed only; mono", -24, loop=True)
def reentry_rattle(v):
    L = 10.0
    n = n_of(L)
    out = np.zeros(n)
    phrases = [fade(src("metal-creaking", 0.25, 1.35), 0.08, 0.15), fade(src("metal-creaking", 1.45, 3.15), 0.08, 0.25)]
    for k, (at, pitch, g) in enumerate(((0.4, 1.0, 1.0), (1.7, 0.82, 0.8), (3.9, 1.06, 0.7),
                                        (5.1, 0.78, 0.9), (7.4, 0.94, 0.75), (7.8, 0.86, 0.85))):
        place_wrap(out, s.pitch_shift(phrases[k % 2], pitch), n_of(at), g)
    vib = cnoise(n, 30, 150, -1.0) * csmooth(n, 5, 0.4, 1.0)
    return 0.6 * norm(out) + 0.16 * norm(vib)


@intro("Reentry/reentry_wind_rush", "Wind rush as it falls",
       "wind-blowing.mp3", "7.5 s of wind highpassed at 250 Hz, a pink rush whose band rises 400 -> 2400 Hz, "
       "buffeting AM; swells in and holds, fades at the end; stereo", -18)
def reentry_wind(v):
    x = src("wind-blowing", 10.0, 17.5, mono=False)
    n = len(x)
    t = np.arange(n) / SR
    x = per_ch(lambda c: hp(c, 250, 2), x)
    out = np.zeros((n, 2))
    for c in range(2):
        rush = s.sweep(s.pink(n), "band", (400, 2400), width=1.3)
        buff = s.smooth_rand(n, 7, 0.55, 1.0)
        out[:, c] = (norm(x[:, c]) * 0.8 + norm(rush) * 0.7) * buff
    envl = env_points(t, [(0, 0), (1.0, 0.25), (4.8, 1.0), (6.8, 1.0), (7.5, 0.0)])
    return fade(out * envl[:, None], 0.01, 0.3)


@intro("Reentry/reentry_retro_burst", "Retro rockets firing (short bursts)",
       "spaceship-shuttle-firing.mp3", "three bursts cut from the firing take (onset / steady part with a built "
       "attack), lengths 0.35-0.65 s, pitched 1.0/0.94/1.07, exponential release, a low chuff; mono", -16,
       variants=3)
def reentry_retro(v):
    starts, lengths, pitch = (0.08, 0.6, 1.1), (0.4, 0.55, 0.35), (1.0, 0.94, 1.07)
    x = src("spaceship-shuttle-firing", starts[v], starts[v] + lengths[v] + 0.3)
    x = s.pitch_shift(x, pitch[v])
    t = np.arange(len(x)) / SR
    hold = lengths[v] / pitch[v]
    x *= np.where(t < hold, 1.0, np.exp(-(t - hold) / 0.07))
    x = fade(x, 0.004, 0.05)
    out = norm(x)
    place(out, norm(s.thump(0.35, 140, 60, 25, 0.08, 0.5, 900)) * 0.55, 0.0)
    return out


@intro("Reentry/reentry_touchdown_heavy", "Heavy touchdown thud, metal creak, dust and gravel settling",
       "footsteps-gravel.mp3, Kenney impactMetal_000, Kenney lowFrequency_explosion_000",
       "sub thump + low explosion body + pitched-down metal impact + a low stick-slip creak + gravel tails "
       "cut from the footstep take spread over 2 s + dust noise; mono", -14)
def reentry_touchdown(v):
    out = np.zeros(n_of(3.6))
    place(out, norm(s.thump(1.8, 95, 28, 7, 0.5, 0.6, 500)), 0.0, 1.0)
    place(out, norm(fade(lp(src("kenney_lowFrequency_explosion_000"), 400), 0.002, 0.9)) * 0.4, 0.0)
    place(out, norm(s.pitch_shift(src("kenney_impactMetal_000"), 0.55)) * 0.45, 0.01)
    place(out, creak(1.1, freqs=(260, 690, 1480), rate=(14, 34), q=7) * 0.3, 0.3)
    gravel = src("footsteps-gravel")
    ons = onsets(gravel)
    for k, at in enumerate((0.05, 0.25, 0.55, 0.95, 1.45)):
        o = ons[k % len(ons)]
        tail = fade(gravel[o + n_of(0.04):o + n_of(0.45)], 0.01, 0.15)
        place(out, lp(tail, 6000), at, 0.5 * (0.75 ** k) * 3)
    t = tax(2.0)
    dust = bp(s.pink(len(t)), 300, 2500) * s.env_ad(t, 0.05, 0.5)
    place(out, norm(dust) * 0.18, 0.05)
    return out


# ================================================================== Capsule and Spencer
@intro("Capsule/capsule_pressure_release", "Pressure release (long hiss)",
       "long-hiss.mp3", "3.8 s of the hiss, fast attack, lowpass closing 11 kHz -> 4.5 kHz while the pressure "
       "drops, long fade; a small valve pop at the start; mono", -18)
def capsule_pressure(v):
    x = src("long-hiss", 2.0, 5.8)
    t = np.arange(len(x)) / SR
    x = s.sweep(x, "low", s.curve_of(100, lambda u: 11000 * (4500 / 11000) ** (u ** 1.5)))
    x *= env_points(t, [(0, 0), (0.03, 1), (1.6, 0.9), (3.0, 0.4), (3.8, 0.0)])
    out = norm(fade(x, 0.003, 0.2)) + 0.25 * bp(x, 800, 3000) / (np.max(np.abs(x)) + 1e-9)
    place(out, norm(s.thump(0.15, 260, 120, 30, 0.02, 0.6, 2000)) * 0.4 + 0, 0.0)
    return out


@intro("Capsule/capsule_hatch_open", "Hatch unlocking and swinging open",
       "mechanical-clamp.mp3, release-hiss.mp3, spaceship-door-opening.mp3",
       "one event: a dull unlock thunk (clamp clank low-passed at 900 Hz + low thump), a short pressure hiss, "
       "the door take ending on its own tail with a 0.1 s fade; mono", -18)
def capsule_hatch(v):
    out = np.zeros(n_of(1.95))
    clank = lp(fade(src("mechanical-clamp", 2.33, 2.75), 0.002, 0.15), 900, 4)
    place(out, norm(clank) * 0.7, 0.0)
    place(out, norm(s.thump(0.35, 110, 55, 15, 0.08, 0.4, 500)) * 0.6, 0.0)
    hiss = lp(fade(src("release-hiss", 0.04, 0.6), 0.01, 0.2), 7000)
    place(out, norm(hiss) * 0.35, 0.1)
    place(out, norm(fade(src("spaceship-door-opening", 0.08, 1.6), 0.005, 0.1)), 0.3)
    return out


for _surface, _src_name, _desc in (("metal", "footsteps-metal", "footsteps-metal.mp3 (91 s take)"),
                                   ("gravel", "footsteps-gravel", "footsteps-gravel.mp3"),
                                   ("stone", "footsteps-stone", "footsteps-stone.mp3")):
    def _make(src_name):
        def fn(v):
            return pick_steps(src_name, 4)[v]
        return fn
    intro(f"Capsule/capsule_footstep_{_surface}", "Footsteps on metal, then on rock and gravel", _desc,
          "onset detection; the 4 cleanest single steps (loud attack over a quiet run-up), cut <= 0.5 s, "
          "3 ms fade-in / 60 ms fade-out; mono", -20, variants=4, comp=False)(_make(_src_name))


@intro("Capsule/capsule_breath_helmet_calm_loop", "Breathing inside the helmet (calm)",
       "male-breathing.mp3", "breaths segmented into in/out pairs, slowed to 0.9x, ~4 s cycle (long rest), wrap-placed; "
       "helmet chain (250-4000 Hz, muffled, visor resonance, 1-9 ms box reflections) + faint air hiss; mono",
       -24, loop=True)
def breath_calm(v):
    return breathing_loop(cycle=4.0, cycles=4, speed=0.9, max_in=1.0, max_out=1.0, gap=0.18)


@intro("Capsule/capsule_breath_helmet_fast_loop", "Breathing inside the helmet (fast)",
       "male-breathing.mp3", "same pairs trimmed (in <= 0.42 s, out <= 0.48 s), 1.08x faster, ~1.25 s cycle, "
       "helmet chain + air hiss; mono", -22, loop=True)
def breath_fast(v):
    return breathing_loop(cycle=1.25, cycles=8, speed=1.08, max_in=0.42, max_out=0.48, gap=0.05, jitter=0.05)


@intro("Capsule/capsule_panting_helmet", "Frightened panting",
       "male-breathing.mp3", "short (<= 0.22 s) slices of each breath, 1.15x, ~0.5 s cycle, slowly calming over "
       "3.8 s, helmet chain + air hiss; mono one-shot", -22)
def panting(v):
    pairs = breaths()
    out = np.zeros(n_of(4.2))
    t = 0.0
    k = 0
    while t < 3.6:
        inh, exh = pairs[k % len(pairs)]
        g = 1.0 - 0.35 * t / 3.6
        a = s.pitch_shift(trim_breath(inh[n_of(0.08):], 0.2, 0.06), 1.15)
        b = s.pitch_shift(trim_breath(exh[n_of(0.06):], 0.22, 0.07), 1.15)
        place(out, a, t, g * 0.8)
        place(out, b, t + len(a) / SR + 0.02, g)
        t += 0.46 + 0.12 * t / 3.6 + s.rng.uniform(-0.03, 0.03)
        k += 1
    y = helmet(out)
    y += bp(s.pink(len(y)), 1500, 7000) * np.sqrt(np.mean(y ** 2)) * 0.12
    return fade(y, 0.01, 0.4)


@intro("Capsule/capsule_gasp_helmet", "Gasp (inside the helmet)", "gasp.mp3",
       "trimmed gasp through the helmet chain with a breath of air hiss; mono", -20)
def gasp_helmet(v):
    x = fade(src("gasp", 0.08, 0.84), 0.005, 0.2)
    y = helmet(pad(x, len(x) + n_of(0.05)))
    return y + bp(s.pink(len(y)), 1500, 7000) * np.sqrt(np.mean(y ** 2)) * 0.1


@intro("Capsule/capsule_gasp_dry", "Gasp (dry)", "gasp.mp3", "trimmed, highpass 80 Hz, fades; mono", -20)
def gasp_dry(v):
    return hp(fade(src("gasp", 0.08, 0.84), 0.005, 0.2), 80)


@intro("Capsule/capsule_pistol_draw", "Pistol drawn from the holster (leather, click)", "pistol-draw.mp3",
       "trimmed 0.05-1.05 s, fades; mono", -18, comp=False)
def pistol_draw(v):
    return fade(src("pistol-draw", 0.05, 1.05), 0.003, 0.15)


@intro("Capsule/capsule_pistol_charge", "Pistol charging or arming", "gregorquendel-laser-charge-175727.mp3",
       "the rising part 1.15-2.6 s (whine climbing ~750 -> 1600 Hz), 20 ms in, 300 ms out; mono", -18)
def pistol_charge(v):
    return fade(src("gregorquendel-laser-charge-175727", 1.15, 2.6), 0.02, 0.3)


@intro("Capsule/capsule_radio_call_open", "Radio beep and static (Voss calling in)", "radio-static.mp3",
       "two synth beeps (1320 / 1760 Hz) and a static burst with squelch tail, all through a 320-3400 Hz "
       "radio band with soft drive; mono", -20)
def radio_open(v):
    out = np.zeros(n_of(1.05))
    for at, f, d in ((0.0, 1320, 0.07), (0.11, 1760, 0.09)):
        t = tax(d)
        beep = (np.sin(2 * np.pi * f * t) + 0.25 * s.osc(f, "square", len(t))) * fade(np.ones(len(t)), 0.004, 0.01)
        place(out, beep * 0.7, at)
    st = src("radio-static", 0.4, 1.2)
    t = np.arange(len(st)) / SR
    st = st * env_points(t, [(0, 0), (0.01, 1.0), (0.12, 0.6), (0.5, 0.45), (0.8, 0.0)])
    place(out, norm(st) * 0.55, 0.22)
    return fade(radio_chain(out), 0.002, 0.08)


@intro("Capsule/capsule_radio_static_loop", "Radio beep and static (open channel)", "radio-static.mp3",
       "4.5 s of static, 0.3 s seam crossfade, radio band 320-3400 Hz with soft drive; mono", -26, loop=True)
def radio_static(v):
    x = loop_xfade(src("radio-static", 0.2, 5.0), 0.3)
    return circ(radio_chain, x)


# ================================================================== Colony
DRONE_LOOP = 4.0


FAN_RATES = (151.0, 158.0, 164.0, 172.0)     # blade-pass rates of the four rotors (Hz), slightly detuned


def fan(n, rates):
    """Rotor airflow: broadband noise chopped at each rotor's blade-pass rate, a soft 1-3 kHz whir and only a
    faint blade tone. rates: per rotor, a constant (Hz) or a per-sample array (spin-up/-down)."""
    out = np.zeros(n)
    for r in rates:
        r = np.full(n, float(r)) if np.isscalar(r) else r
        ph = s.phase_of(r)
        chop = 0.55 + 0.45 * (0.5 + 0.5 * np.sin(ph)) ** 2
        flow = cnoise(n, 150, 7000, -1.0) * chop
        whir = cnoise(n, 1000, 3000, -0.5) * (0.7 + 0.3 * np.sin(2 * ph))
        tone = 0.5 * np.sin(ph) + 0.25 * np.sin(2 * ph)
        out += norm(flow) + 0.3 * norm(whir) + 0.08 * tone
    return out


@intro("Colony/colony_drone_hum_loop", "Builder drones (hum)", "synthesis",
       "fan sound: four rotors (blade-pass 151-172 Hz, snapped to the loop) each chopping broadband pink airflow, a "
       "moderate 1-3 kHz whir, a faint blade tone, slow wobble; exactly periodic; mono", -24, loop=True)
def colony_drone_hum(v):
    n = n_of(DRONE_LOOP)
    return fan(n, [snap(r, DRONE_LOOP) for r in FAN_RATES]) * csmooth(n, 1.0, 0.85, 1.0)


DRONE_HUM_FILE = os.path.join(s.OUT, "intro", "Colony", "colony_drone_hum_loop.wav")


@intro("Colony/colony_drone_land", "Builder drones (landing: hum slowing to a stop)",
       "out/intro/Colony/colony_drone_hum_loop.wav (the approved hum, byte-identical)",
       "the approved hum loop at full level for 0.6 s, then varispeed easing out from 1.0 to 0.25 over 2.8 s "
       "(blade rate and pitch fall together) while the level eases to silence; nothing added; mono", -27, comp=False)
def colony_drone_land(v):
    hum = np.tile(_read(DRONE_HUM_FILE, True), 4)
    hold, ramp = 0.6, 2.8
    n = n_of(hold + ramp)
    u = np.clip((np.arange(n) / SR - hold) / ramp, 0, 1)
    ease = 1 - (1 - u) ** 2                         # ease-out: the drop is quick at first, then settles
    rate = 1.0 - 0.75 * ease
    y = vread(hum, rate)[:n]
    level = (1 - u[:len(y)]) ** 2
    return fade(y * level, 0.005, 0.05)


def drill_motor(n, f):
    return lp(s.osc(f, "saw"), 1500)


@intro("Colony/colony_drill_start", "Drill starting up", "stone-drilling.mp3, mechanical-clamp.mp3",
       "heavy motor/gearbox spin-up (25 -> 95 Hz harmonic motor at a fixed 1.2 kHz lowpass, gear-mesh whine "
       "rising 300 -> 1140 Hz, grinding noise), three gear-engage clanks cut from the clamp, then the bit engaging "
       "into the drilling take; ends at running level with a 0.3 s fade so the run loop can take over; mono", -18)
def colony_drill_start(v):
    dur = 3.8
    n = n_of(dur)
    t = np.arange(n) / SR
    f = 25 + 70 * (1 - np.exp(-t / 0.5))
    motor = lp(s.osc(f, "saw"), 1200, 4)
    mesh = np.sin(s.phase_of(f * 12)) * (0.8 + 0.2 * s.smooth_rand(n, 20))
    grind = bp(s.pink(n), 200, 2000) * s.smooth_rand(n, 15, 0.5, 1.0)
    run_up = env_points(t, [(0, 0), (0.15, 0.5), (1.2, 1.0), (1.5, 0.7), (dur, 0.45)])
    out = (0.6 * norm(motor) + 0.12 * norm(mesh) + 0.2 * norm(grind)) * run_up
    for at, a in ((0.22, 2.40), (0.62, 2.62), (1.0, 2.85)):
        clank = lp(fade(src("mechanical-clamp", a, a + 0.12), 0.002, 0.06), 2500)
        place(out, norm(clank) * 0.45, at)
    rec = src("stone-drilling", 1.0, 3.8)
    place(out, norm(fade(rec, 0.15, 0.3)), 1.3, 1.0)
    place(out, norm(s.thump(0.3, 140, 55, 20, 0.05, 0.7, 1200)) * 0.5, 1.3)
    return fade(out, 0.005, 0.3)


@intro("Colony/colony_drill_run_loop", "Drill running (loop)", "stone-drilling.mp3",
       "9 s of the drilling take (14-23.5 s, after the dropout), 0.5 s seam crossfade, a periodic 95 Hz motor "
       "layer to match the start-up; mono", -20, loop=True)
def colony_drill_run(v):
    rec = loop_xfade(src("stone-drilling", 14.0, 23.5), 0.5)
    n = len(rec)
    f = snap(95.0, n / SR)
    motor = circ(lambda y: lp(y, 1500), s.osc(f, "saw", n))
    return norm(rec) + 0.2 * norm(motor)


@intro("Colony/colony_crystal_hum_loop", "Crystal hum (loop)", "synthesis",
       "glassy inharmonic partials (196 Hz x 1 / 2.76 / 5.40 / 8.93), each with a twin 0.3-0.9 Hz off for slow "
       "beating, a soft sub and a faint airy shimmer; exactly periodic over 10 s; mono", -26, loop=True)
def colony_crystal(v):
    L = 10.0
    n = n_of(L)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for ratio, a, beat in ((1.0, 1.0, 0.3), (2.76, 0.5, 0.5), (5.40, 0.28, 0.7), (8.93, 0.14, 0.9)):
        f = snap(196 * ratio, L)
        fb = snap(196 * ratio + beat, L)
        out += a * (np.sin(2 * np.pi * f * t + s.rng.uniform(0, 6.28)) + 0.8 * np.sin(2 * np.pi * fb * t))
    out += 0.25 * np.sin(2 * np.pi * snap(98, L) * t)
    out *= csmooth(n, 0.3, 0.75, 1.0)
    shimmer = cnoise(n, 6000, 12000, -1.0) * csmooth(n, 0.5, 0.0, 1.0) ** 2
    return norm(out) + 0.02 * norm(shimmer)


@intro("Colony/colony_radar_ping", "Radar ping", "synthesis",
       "sine ping 1290 -> 1250 Hz with a quiet 2nd harmonic, fast attack, 0.25 s decay, short sonar-like tail; mono",
       -20)
def radar_ping(v):
    t = tax(0.6)
    f = s.glide(t, 1290, 1250, 12)
    ping = (np.sin(s.phase_of(f)) + 0.12 * np.sin(2 * s.phase_of(f))) * s.env_ad(t, 0.002, 0.18)
    ir = s.reverb_ir(1.2, 0.3, 6000, 2500, 0.03)
    return fade(np.concatenate([ping, np.zeros(n_of(0.8))]) + 0.4 * pad(fftconvolve(ping, ir), len(ping) + n_of(0.8)),
                0.001, 0.2)


@intro("Colony/colony_beacon_beep", "Beacon beep", "synthesis",
       "two short soft-square beeps at 1760 Hz, 80 ms each with 70 ms gap; mono", -20)
def beacon_beep(v):
    out = np.zeros(n_of(0.32))
    for at in (0.0, 0.15):
        t = tax(0.08)
        b = lp(s.osc(1760, "square", len(t)), 6000) * fade(np.ones(len(t)), 0.004, 0.012)
        place(out, b, at)
    return out


# ================================================================== Threat
def quake_layers(n, start_e, start_s, wrap):
    """Earth-rumble (sub) + space-rumble low end, with saturation for audible harmonics on small speakers."""
    er = src("earth-rumble", start_e, start_e + n / SR + (1.0 if wrap else 0))
    sr_ = src("space-rumble", start_s, start_s + n / SR + (1.0 if wrap else 0), mono=False)
    if wrap:
        er, sr_ = loop_xfade(er, 1.0), loop_xfade(sr_, 1.0)
    er, sr_ = er[:n], sr_[:n]
    f = circ if wrap else (lambda fn, y: fn(y))
    er_low = f(lambda y: lp(y, 120, 4), er)
    growl = f(lambda y: bp(np.tanh(norm(lp(y, 120, 4)) * 3), 50, 260), er)
    sp = f(lambda y: per_ch(lambda c: lp(c, 250, 4), y), sr_)
    out = np.zeros((n, 2))
    for c in range(2):
        out[:, c] = norm(er_low) + 0.5 * norm(growl) + 0.6 * norm(sp[:, c])
    return out


def crumbles(n, dens, wrap=True):
    out = np.zeros((n, 2))
    for t0 in np.sort(s.rng.uniform(0, n, int(40 * n / SR))):
        if s.rng.uniform() > dens[int(t0)]:
            continue
        k = s.rng.integers(3, 12)
        burst = s.grains(n_of(0.25), np.sort(s.rng.uniform(0, 0.2, k)), (0.002, 0.02), (300, 3000),
                         s.rng.uniform(0.3, 1, k))
        (place_wrap if wrap else place_clip)(out, burst, t0, s.rng.uniform(0.2, 0.7), pan=s.rng.uniform(0.15, 0.85))
    return out


@intro("Threat/threat_quake_rumble_loop", "Earthquake and deep ground rumble (loop)",
       "earth-rumble.mp3, space-rumble.mp3", "earth-rumble lowpassed + its saturated 50-260 Hz harmonics (audible on "
       "small speakers) + space-rumble low end, sparse rock crumbles; crossfaded seams; stereo", -20, loop=True)
def quake_loop(v):
    n = n_of(12.0)
    base = quake_layers(n, 5.0, 8.0, True)
    cr = crumbles(n, csmooth(n, 0.5, 0.0, 1.0))
    return base + 0.12 * norm(cr)


@intro("Threat/threat_quake_swell", "Earthquake and deep ground rumble (swelling one-shot)",
       "earth-rumble.mp3, space-rumble.mp3", "same layers, swelling in over 4.5 s, peaking with two rock cracks, "
       "crumbles following the swell, decaying out by 8.5 s; stereo", -16)
def quake_swell(v):
    dur = 8.5
    n = n_of(dur)
    t = np.arange(n) / SR
    envl = env_points(t, [(0, 0), (1.0, 0.12), (4.5, 1.0), (6.0, 0.85), (dur, 0.0)])
    base = quake_layers(n, 18.0, 3.0, False)
    cr = crumbles(n, envl, wrap=False)
    out = (base + 0.15 * norm(cr)) * envl[:, None]
    for at in (4.3, 4.75):
        crack = norm(s.mix(s.click(0.05, 900, 5000, 0.008), 0.8 * s.thump(0.5, 110, 40, 12, 0.12, 0.6, 1200)))
        place_clip(out, crack, n_of(at), 0.6, pan=0.5)
    return fade(out, 0.01, 0.6)


@intro("Threat/threat_ground_tear", "Ground tearing open, burrows bursting", "ground-tearing.mp3",
       "two 3 s cuts of the tearing take with a burst envelope + low thump + crack transient + ripping AM "
       "noise (800-4000 Hz) + falling clods; mono", -14, variants=2)
def ground_tear(v):
    start = (0.5, 4.0)[v]
    x = src("ground-tearing", start, start + 3.0)
    t = np.arange(len(x)) / SR
    x *= env_points(t, [(0, 0), (0.02, 1.0), (0.6, 0.8), (1.8, 0.45), (3.0, 0.0)])
    out = norm(fade(x, 0.002, 0.3))
    place(out, norm(s.thump(1.0, 85, 30, 8, 0.28, 0.5, 700)) * 0.8, 0.0)
    place(out, norm(s.mix(lp(s.friedlander(0.2, 0.003), 4000), 0.5 * s.click(0.02, 800, 6000, 0.004))) * 0.6, 0.0)
    rip_t = tax(1.6)
    rip = bp(s.white(len(rip_t)), 800, 4000) * s.rumble_mod(len(rip_t), 60, 0.95) * s.env_ad(rip_t, 0.01, 0.35)
    place(out, norm(rip) * 0.35, 0.02 + 0.1 * v)
    tt = s.poisson_times(lambda x_: 60 * np.exp(-x_ / 0.8), 2.4, 0.2)
    clods = s.grains(n_of(2.6), tt, (0.004, 0.03), (400, 4000), s.rng.uniform(0.2, 1, len(tt)), decay=5)
    place(out, norm(clods) * 0.3, 0.0)
    return out


@intro("Threat/threat_eyes_open_wet", "Eyes opening in the dark (wet, organic)",
       "Kenney slime_001", "only a slime_001 fragment (two different sections), 30 ms fade-in, lowpassed 7 kHz; "
       "no squelch, no transient; mono", -20, variants=2)
def eyes_open(v):
    a = (1.2, 2.6)[v]
    slime = s.pitch_shift(src("kenney_slime_001", a, a + 0.55), (1.15, 1.0)[v])
    return fade(lp(slime, 7000), 0.03, 0.15)


@intro("Threat/threat_swarm_chitter_loop", "Skitter swarm chittering (loop)",
       "Approved/Monsters/Ant (walk loop, screeches)", "ant walk loop (two offset copies L/R) + ~14/s pitched-up "
       "(1.3-2.1x) screech fragments + mandible click trains, wrap-placed with random pan; 8.3 s; stereo",
       -22, loop=True)
def swarm_chitter(v):
    n = 2 * len(approved("Monsters/Ant/ant_walk_loop.wav"))
    x = chitter_layers(n, True)
    return circ(lambda y: per_ch(lambda c: hp(c, 150), y), x)


@intro("Threat/threat_queen_roar", "Queen roar (big, alien)", "Approved/Bosses/Ant screeches, Approved/Monsters/Ant",
       "boss screeches pitched 0.72 and 0.6 and layered with a small screech at 0.85, bossified (sub following "
       "the envelope, light grit) + a dark long tail; mono", -14)
def queen_roar(v):
    a = s.pitch_shift(approved("Bosses/Ant/ant_screech_boss_01.wav"), 0.72)
    b = s.pitch_shift(approved("Bosses/Ant/ant_screech_boss_02.wav"), 0.6)
    c = s.pitch_shift(approved("Monsters/Ant/ant_screech_02.wav"), 0.85)
    x = s.mix(norm(a), np.concatenate([np.zeros(n_of(0.08)), norm(b) * 0.85]),
              np.concatenate([np.zeros(n_of(0.03)), hp(norm(c), 1200) * 0.3]))
    y = s.bossify(x, factor=1.0, sub=0.6, grit=0.25)
    ir = s.reverb_ir(2.8, 0.7, 5000, 500, 0.025)
    tail = fftconvolve(y, ir)
    return fade(pad(y, len(tail)) + 0.25 * norm(tail) * np.max(np.abs(y)), 0.003, 0.5)


@intro("Threat/threat_swarm_retreat", "Swarm retreating, chittering fading into the burrows",
       "Approved/Monsters/Ant (walk loop, screeches)", "chitter layers thinning out, lowpass closing 9 kHz -> 400 Hz, "
       "level falling, dirt scrabbling at the start; stereo", -20)
def swarm_retreat(v):
    dur = 7.0
    n = n_of(dur)
    t = np.arange(n) / SR
    x = chitter_layers(n, True, density=1.0, wrap=False)
    x = per_ch(lambda c: s.sweep(hp(c, 150), "low", s.curve_of(150, lambda u: 9000 * (400 / 9000) ** (u ** 0.8))), x)
    x *= env_points(t, [(0, 1.0), (1.5, 0.8), (4.0, 0.3), (6.0, 0.08), (dur, 0.0)])[:, None]
    tt = s.poisson_times(lambda x_: 120 * np.exp(-x_ / 1.2), 3.0, 0.0)
    dirt = s.grains(n, tt, (0.003, 0.02), (500, 4000), s.rng.uniform(0.2, 1, len(tt)))
    out = norm(x)
    out[:, 0] += 0.25 * norm(dirt)
    out[:, 1] += 0.25 * norm(np.roll(dirt, n_of(0.013)))
    return fade(out, 0.01, 0.5)


# ================================================================== Environment
@intro("Environment/env_wind_plain_loop", "Wind over the plain (loop)", "wind-blowing.mp3",
       "29 s of the recording (4-34 s), 1 s equal-power seam crossfade, highpass 40 Hz; stereo", -26, loop=True)
def env_wind(v):
    x = loop_xfade(src("wind-blowing", 4.0, 34.0, mono=False), 1.0)
    return circ(lambda y: per_ch(lambda c: hp(c, 40), y), x)


@intro("Environment/env_cave_drips_loop", "Drips in the cave (loop)", "cave-water-drops.mp3",
       "31 s of the recording, 0.5 s seam crossfade; stereo", -28, loop=True)
def env_drips(v):
    return loop_xfade(src("cave-water-drops", 0.5, 32.0, mono=False), 0.5)


@intro("Environment/env_wildlife_call", "Distant alien wildlife calls",
       "Approved/Monsters Bat, Leech, IceBeetle screeches", "creature voice pitched down (0.55-0.7), called twice "
       "(second softer, slightly higher), band 200-2200 Hz for distance, long dark stereo reverb (decay ~1.1 s, "
       "mostly wet); stereo", -22, variants=3)
def wildlife(v):
    rel, f = (("Monsters/Bat/bat_screech_01.wav", 0.55), ("Monsters/Leech/leech_screech_01.wav", 0.7),
              ("Monsters/IceBeetle/ice_beetle_screech_01.wav", 0.6))[v]
    call = norm(bp(s.pitch_shift(approved(rel), f), 200, 2200))
    again = norm(bp(s.pitch_shift(approved(rel), f * 1.06), 200, 2000)) * 0.6
    gap = len(call) / SR + 0.35
    x = s.mix(call, np.concatenate([np.zeros(n_of(gap)), again]))
    return fade(stereo_verb(x, 4.0, 1.1, 3000, 400, 0.04, wet=1.0, dry=0.35), 0.003, 0.8)


# ================================================================== base assembly (builder drones)
_hits = None


def hammer_hits():
    """Single hammer strikes of metal-hammering.mp3: onsets louder than -12 dBFS, each cut to <= 0.24 s."""
    global _hits
    if _hits is None:
        x = src("metal-hammering")
        ons = onsets(x, rise_db=10, min_gap=0.15)
        _hits = []
        for k, o in enumerate(ons):
            if 20 * np.log10(np.max(np.abs(x[o:o + 2000])) + 1e-12) < -12:
                continue
            end = min(o + n_of(0.24), ons[k + 1] - n_of(0.005) if k + 1 < len(ons) else len(x))
            _hits.append(fade(x[max(0, o - n_of(0.003)):end], 0.002, 0.06))
    return _hits


HAMMER_GROUPS = (
    ((0, 1.0, 1.0, 6),),                                                     # 01 single strike
    ((0, 1.0, 0.95, 9), (0.22, 0.85, 0.95, 10)),                             # 02 double
    ((0, 1.0, 1.03, 1), (0.21, 0.8, 1.03, 2), (0.41, 0.9, 1.03, 3)),         # 03 triple
    ((0, 0.9, 0.97, 5), (0.22, 1.0, 1.0, 7), (0.47, 0.7, 0.94, 8), (0.6, 0.85, 1.02, 4), (0.95, 1.0, 0.98, 11)),
)


@intro("Colony/colony_build_hammer", "Base assembly: hammer hits", "metal-hammering.mp3",
       "single strikes found by onset detection, re-sequenced into a single / double / triple / irregular 5-hit "
       "group with small pitch changes (0.94-1.03), low-passed 9 kHz; mono", -18, variants=4)
def build_hammer(v):
    hits = hammer_hits()
    group = HAMMER_GROUPS[v]
    out = np.zeros(n_of(group[-1][0] + 0.35))
    for at, g, pitch, k in group:
        place(out, s.pitch_shift(hits[k % len(hits)], pitch), at, g)
    return lp(out, 9000)


@intro("Colony/colony_build_hammer_loop", "Base assembly: distant hammering (loop)", "metal-hammering.mp3",
       "single strikes wrap-placed in irregular clusters of 1-4 (0.18-0.3 s apart, 0.4-1.1 s pauses) over 7 s, "
       "pitch 0.85-1.05, varied gains, low-passed 3.5 kHz for distance, a small room tail applied around the loop; "
       "mono", -24, loop=True)
def build_hammer_loop(v):
    hits = hammer_hits()
    n = n_of(7.0)
    out = np.zeros(n)
    t = 0.1
    while t < 6.9:
        for j in range(int(s.rng.integers(1, 5))):
            hit = s.pitch_shift(hits[int(s.rng.integers(len(hits)))], s.rng.uniform(0.85, 1.05))
            place_wrap(out, hit, n_of(t), s.rng.uniform(0.4, 1.0))
            t += s.rng.uniform(0.18, 0.3)
        t += s.rng.uniform(0.4, 1.1)
    out = circ(lambda y: lp(y, 3500), out)
    ir = s.reverb_ir(0.6, 0.15, 4000, 1200, 0.015)
    wet = fftconvolve(np.concatenate([out, out]), ir)[n:2 * n]
    return norm(out) + 0.35 * norm(wet)


@intro("Colony/colony_build_drill", "Base assembly: power-drill bursts", "drill.mp3",
       "0.6 / 1.0 / 1.4 s of the drill take played through a speed curve: spin-up from 0.55x over 90 ms, "
       "spin-down to 0.45x over the last 160 ms; mono", -20, variants=3)
def build_drill(v):
    start, dur = ((5.0, 0.6), (17.0, 1.0), (30.0, 1.4))[v]
    x = src("drill", start, start + dur + 0.5)
    n = n_of(dur)
    t = np.arange(n) / SR
    rate = np.minimum(0.55 + 0.45 * np.clip(t / 0.09, 0, 1), 1 - 0.55 * np.clip((t - (dur - 0.16)) / 0.16, 0, 1))
    y = vread(x, rate)[:n]
    level = np.clip((t[:len(y)] - 0) / 0.02, 0, 1) * (1 - 0.7 * np.clip((t[:len(y)] - (dur - 0.16)) / 0.16, 0, 1))
    return fade(y * level, 0.003, 0.05)


@intro("Colony/colony_build_drill_loop", "Base assembly: power drill running (loop)", "drill.mp3",
       "6.5 s of the steady drill take (10-17 s), 0.5 s equal-power seam crossfade; mono", -22, loop=True)
def build_drill_loop(v):
    return loop_xfade(src("drill", 10.0, 17.0), 0.5)


# ================================================================== burrow closing
BURROW_SETTLE = 0.15              # the close moment sits exactly this long before the end of the file
BURROW_SWELLS = (1.1, 1.35, 1.6)


@intro("Threat/threat_burrow_close", "Burrow closing (brood back underground)",
       "ground-tearing.mp3, footsteps-gravel.mp3, earth-rumble.mp3",
       "a forward crumble (ground-tearing with a 4 ms attack and decay, gravel tails, low earth rumble) played in "
       "reverse so it swells and sucks back in; its climax is the CLOSE MOMENT at exactly length - 0.15 s "
       "(01: 1.10 s, 02: 1.35 s, 03: 1.60 s); after it only a very soft soil settle (faint grains + low brush, "
       ">= 15 dB under the swell peak); no thump, no pop, no ring; not trimmed in mastering; mono",
       -18, variants=3, comp=False, exact=True)
def burrow_close(v):
    dur = BURROW_SWELLS[v]
    start, rum_at = ((1.0, 6.0), (3.6, 14.0), (5.2, 25.0))[v]
    n = n_of(dur)
    t = np.arange(n) / SR
    tear = src("ground-tearing", start, start + dur) * s.env_ad(t, 0.004, dur * 0.3)
    gravel = src("footsteps-gravel")
    debris = np.zeros(n)
    ons = onsets(gravel, min_gap=0.4)
    for k in range(4):
        o = ons[(k + v) % len(ons)]
        place(debris, fade(gravel[o:o + n_of(0.4)], 0.003, 0.1), 0.02 + 0.12 * k, 0.8 ** k)
    rumble = lp(src("earth-rumble", rum_at, rum_at + dur), 160, 4) * s.env_ad(t, 0.025, dur * 0.4)
    # highpass so the abrupt close leaves no low-frequency step (it would ring on as a soft boom)
    crumble = hp(norm(tear) + 0.5 * norm(debris) + 0.6 * norm(rumble), 60, 2)
    crumble[:n_of(0.003)] *= np.linspace(0, 1, n_of(0.003))        # reversed: a 3 ms close, no click
    rev = crumble[::-1] * np.exp((t - dur) / (0.2 * dur))          # monotonic swell: the climax is the close moment
    rev = fade(rev, 0.03, 0.0)
    out = np.concatenate([rev, np.zeros(n_of(BURROW_SETTLE))])
    m = n_of(BURROW_SETTLE)
    tt = s.poisson_times(lambda x_: 120 * np.exp(-x_ / 0.05), BURROW_SETTLE - 0.02, 0.0)
    grains = s.grains(m, tt, (0.002, 0.008), (500, 2500), s.rng.uniform(0.3, 1, len(tt)))
    brush = lp(s.pink(m), 700) * s.env_ad(np.arange(m) / SR, 0.004, 0.035)
    settle = fade(lp(norm(grains) * 0.7 + norm(brush), 3000), 0.002, 0.04)
    peak = np.max(np.abs(rev[-n_of(0.05):]))
    out[n:] += norm(settle) * peak * 10 ** (-26 / 20)
    return out


# ================================================================== render, checks, manifest
def write(path, y):
    full = os.path.join(s.OUT, "intro", path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    wavfile.write(full, SR, (np.clip(y, -1, 1) * 32767).round().astype(np.int16))
    return full


def finish(entry, x):
    x = np.asarray(x, float)
    if entry.get("exact"):
        y = s._master(x, entry["lufs"], entry["comp"], True)     # loop=True here only skips the trimming
    else:
        y = s.master(x, lufs=entry["lufs"], comp=entry["comp"], loop=entry["loop"])
    if not entry["loop"]:
        pk = np.max(np.abs(y))
        if pk > PEAK_CEILING:
            y = y * (PEAK_CEILING / pk)
        y = fade(y, 0.002, 0.01)
    return y


def stats(full, loop):
    sr, x = wavfile.read(full)
    x = x.astype(float) / 32767
    mono = x if x.ndim == 1 else x.mean(axis=1)
    d = np.abs(np.diff(x, axis=0))
    d = d if d.ndim == 1 else d.max(axis=1)
    p999 = np.percentile(d, 99.9)
    r = dict(dur=len(x) / sr, peak_db=20 * np.log10(np.max(np.abs(x)) + 1e-12), lufs=s.loudness(x),
             dc=float(np.max(np.abs(np.mean(x, axis=0)))), p999=p999)
    if loop:
        seam = np.max(np.abs(x[0] - x[-1]))
        k = n_of(0.1)

        def rms(a):
            return 20 * np.log10(np.sqrt(np.mean(a ** 2)) + 1e-12)

        def cen(a):
            A = np.abs(np.fft.rfft(a * np.hanning(len(a))))
            f = np.fft.rfftfreq(len(a), 1 / SR)
            return np.sum(f * A) / (np.sum(A) + 1e-12)
        r.update(seam=seam, seam_ratio=seam / (p999 + 1e-12), rms_diff=rms(mono[-k:]) - rms(mono[:k]),
                 cen_head=cen(mono[:k]), cen_tail=cen(mono[-k:]))
    else:
        r.update(edge=max(np.max(np.abs(x[0])), np.max(np.abs(x[-1]))))
    return r


def flags(r, loop):
    f = []
    if r["dc"] > 0.005:
        f.append("DC")
    if not loop and r["peak_db"] > -1.0:
        f.append("PEAK")
    if loop and r["seam_ratio"] > 1.0:
        f.append("SEAM-JUMP")
    if loop and abs(r["rms_diff"]) > 3.0:
        f.append("SEAM-RMS")
    if not loop and r["edge"] > 0.002:
        f.append("EDGE")
    return " ".join(f) or "ok"


def main():
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    rows = []
    for e in INTRO:
        if only and only not in e["path"]:
            continue
        for v in range(e["variants"]):
            s.seed(zlib.crc32(e["path"].encode()) + 7919 * v)
            y = finish(e, e["fn"](v))
            rel = e["path"] + (f"_{v + 1:02d}" if e["variants"] > 1 else "") + ".wav"
            r = stats(write(rel, y), e["loop"])
            rows.append((e, rel, r))
            extra = (f"seam {r['seam']:.4f} (x{r['seam_ratio']:.2f} of p99.9 step) rms d {r['rms_diff']:+.1f} dB "
                     f"cen {r['cen_head']:.0f}/{r['cen_tail']:.0f} Hz" if e["loop"] else f"edge {r['edge']:.4f}")
            print(f"{rel:52s} {r['dur']:6.2f}s pk {r['peak_db']:6.2f} dBFS {r['lufs']:6.1f} LUFS "
                  f"dc {r['dc']:.4f} {extra}  [{flags(r, e['loop'])}]", flush=True)
    if not only:
        write_manifest(rows)


def write_manifest(rows):
    lines = ["# Opening cutscene sounds (generated)", "",
             "Generated by `Tools/Audio/cat_intro.py` from the sources decoded by `Tools/Audio/decode_sources.py` "
             "(Blender `aud`). Re-run both to rebuild; seeds are fixed. Checklist: `Docs/AUDIO.md`, section "
             "\"Wanted: opening cutscene music and sounds\". Music is not included.", "",
             "Columns: length, loop or one-shot, channels, integrated loudness, true sample peak. Loops: seam jump "
             "is the sample step across the wrap relative to the file's 99.9th-percentile step (< 1 = no click), "
             "RMS delta is last 0.1 s vs first 0.1 s.", ""]
    group = None
    for e, rel, r in rows:
        g = rel.split("/")[0]
        if g != group:
            group = g
            lines += ["", f"## {g}", "", "| File | Checklist item | Source(s) | Processing | Length | Type | LUFS | Peak | Seam |",
                      "|---|---|---|---|---|---|---|---|---|"]
        kind = "loop" if e["loop"] else "one-shot"
        seam = f"x{r['seam_ratio']:.2f}, {r['rms_diff']:+.1f} dB" if e["loop"] else "-"
        lines.append(f"| `{rel}` | {e['item']} | {e['sources']} | {e['processing']} | {r['dur']:.2f} s | {kind} | "
                     f"{r['lufs']:.1f} | {r['peak_db']:.1f} dBFS | {seam} |")
    lines += ["", "## Sources and licences", "",
              "- Remco's recordings in `Assets/_Project/Audio/*.mp3` (downloaded by Remco; see his notes for licences).",
              "- `gregorquendel-laser-charge-175727.mp3` (Remco's download).",
              "- Kenney Sci-Fi Sounds (CC0): impactMetal, slime, lowFrequency_explosion.",
              "- Approved creature sounds in `Assets/_Project/Audio/Approved` (made in this project).", ""]
    with open(os.path.join(OUT_DIR, "manifest.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))


if __name__ == "__main__":
    main()
