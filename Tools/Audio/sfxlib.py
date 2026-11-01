"""Gedeelde bouwstenen voor alle game-geluiden van Tower defense.

Smaak (afgestemd op feedback):
- droog: geen hoorbare echo (behalve waar expliciet gevraagd, zoals de boss-howl)
- niet dof: genoeg hoog, een minimale "lucht" (grondreflectie van enkele ms)
- geen grove vervorming; warmte mag
- luid genoeg, behalve waar subtiel gevraagd (drones)
- in-wereld geluiden mono (voor 3D-positionering in Unity), UI/ambience stereo
"""
import os
import numpy as np
import pyloudnorm as pyln
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, fftconvolve, resample_poly, iirpeak, lfilter
from scipy.ndimage import maximum_filter1d

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "out")
_meter = pyln.Meter(SR)
rng = np.random.default_rng(1)


def seed(s):
    global rng
    rng = np.random.default_rng(s)


# ------------------------------------------------------------------ basis
def n_of(sec):
    return max(1, int(round(SR * sec)))


def tax(dur):
    return np.arange(n_of(dur)) / SR


def white(n):
    return rng.standard_normal(n)


def pink(n):
    X = np.fft.rfft(rng.standard_normal(n))
    f = np.arange(len(X)); f[0] = 1
    y = np.fft.irfft(X / np.sqrt(f), n)
    return y / (np.std(y) + 1e-12)


def brown(n):
    y = np.cumsum(rng.standard_normal(n))
    y = hp(y, 20)
    return y / (np.std(y) + 1e-12)


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-12)


def pad(x, n):
    if len(x) >= n:
        return x[:n]
    return np.concatenate([x, np.zeros(n - len(x))])


def place(dst, src, at, gain=1.0):
    """Mix src in dst vanaf tijd `at` (s)."""
    s = n_of(at) if at > 0 else 0
    if s >= len(dst):
        return dst
    L = min(len(src), len(dst) - s)
    dst[s:s + L] += src[:L] * gain
    return dst


# ------------------------------------------------------------------ filters
def _sos(kind, f, order):
    if np.ndim(f):
        f = [min(max(f[0], 10), SR / 2 - 200), min(max(f[1], 20), SR / 2 - 100)]
    else:
        f = min(max(f, 10), SR / 2 - 100)
    return butter(order, f, kind, fs=SR, output="sos")


def lp(x, f, order=2):
    return sosfilt(_sos("low", f, order), x)


def hp(x, f, order=2):
    return sosfilt(_sos("high", f, order), x)


def bp(x, lo, hi, order=2):
    return sosfilt(_sos("band", [lo, hi], order), x)


def reson(x, f, q=10):
    """Resonantiepiek (bijv. metalen klank, formant)."""
    b, a = iirpeak(min(f, SR / 2 - 200), q, fs=SR)
    return lfilter(b, a, x)


def sweep(x, kind, curve, blocks=200, order=2, width=0.6):
    """Tijdvariabel filter. curve: array (lengte blocks) of (f0, f1) voor geom. sweep.
    kind 'low' | 'high' | 'band' (band: centrum, breedte = factor)."""
    if not hasattr(curve, "__len__") or len(curve) == 2:
        curve = np.geomspace(curve[0], curve[1], blocks)
    blocks = len(curve)
    out = np.zeros_like(x)
    edges = np.linspace(0, len(x), blocks + 1, dtype=int)
    zi = None
    for i in range(blocks):
        f = curve[i]
        if kind == "band":
            sos = _sos("band", [f * (1 - width / 2), f * (1 + width / 2)], order)
        else:
            sos = _sos(kind, f, order)
        if zi is None:
            zi = np.zeros((sos.shape[0], 2))
        seg = x[edges[i]:edges[i + 1]]
        out[edges[i]:edges[i + 1]], zi = sosfilt(sos, seg, zi=zi)
    return out


def curve_of(n_blocks, fn):
    """Maak een filtercurve uit een functie van t (0..1)."""
    u = np.linspace(0, 1, n_blocks)
    return np.array([fn(v) for v in u])


def brighten(x, f=2500, g=0.6):
    return x + g * hp(x, f)


# ------------------------------------------------------------------ envelopes
def env_ad(t, a, d, curve=1.0):
    """Attack (s) lineair, decay exponentieel met tijdconstante d."""
    e = np.where(t < a, t / max(a, 1e-6), np.exp(-(t - a) / max(d, 1e-6)))
    return e ** curve


def env_ahd(t, a, h, d):
    return np.where(t < a, t / max(a, 1e-6),
                    np.where(t < a + h, 1.0, np.exp(-(t - a - h) / max(d, 1e-6))))


def env_points(t, pts):
    """Breekpunt-envelope: pts = [(tijd, niveau), ...]."""
    ts, vs = zip(*pts)
    return np.interp(t, ts, vs)


def smooth_rand(n, hz, lo=0.0, hi=1.0):
    x = lp(rng.standard_normal(n), hz, order=2)
    x = (x - x.min()) / (np.ptp(x) + 1e-12)
    return lo + (hi - lo) * x


def rumble_mod(n, hz, depth):
    m = np.abs(lp(rng.standard_normal(n), hz, order=2)) ** 1.4
    m /= m.max() + 1e-9
    return (1 - depth) + depth * m


# ------------------------------------------------------------------ oscillatoren
def phase_of(freq):
    """freq: array (Hz) per sample -> fase (rad)."""
    return 2 * np.pi * np.cumsum(freq) / SR


def osc(freq, shape="sine", n=None):
    if np.isscalar(freq):
        freq = np.full(n, float(freq))
    ph = phase_of(freq)
    if shape == "sine":
        return np.sin(ph)
    if shape == "saw":
        # bandbeperkte zaagtand via additieve synthese (tot 8 kHz)
        out = np.zeros_like(ph)
        fmax = np.max(freq) + 1
        K = int(min(60, 8000 / fmax)) + 1
        for k in range(1, K + 1):
            out += np.sin(k * ph) / k * (k * freq < SR / 2 - 1000)
        return out * 0.6
    if shape == "square":
        out = np.zeros_like(ph)
        fmax = np.max(freq) + 1
        K = int(min(40, 8000 / fmax)) + 1
        for k in range(1, K + 1, 2):
            out += np.sin(k * ph) / k * (k * freq < SR / 2 - 1000)
        return out * 0.8
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    raise ValueError(shape)


def glide(t, f0, f1, rate):
    """Exponentiele glijding van f0 naar f1."""
    return f1 + (f0 - f1) * np.exp(-t * rate)


def dispersion(n, stages, a):
    """Klik door allpass-keten: 'pew'-chirp (a<0: daalt)."""
    imp = np.zeros(n); imp[0] = 1
    w = np.linspace(0, np.pi, n // 2 + 1)
    z = np.exp(-1j * w)
    H = ((a + z) / (1 + a * z)) ** stages
    return np.fft.irfft(np.fft.rfft(imp) * H, n)


def grains(n, times, dur_range, freq_range, amps, q=1.6, decay=6):
    out = np.zeros(n)
    for t0, a in zip(times, amps):
        L = n_of(rng.uniform(*dur_range))
        s = n_of(t0)
        if s + L >= n or L < 8:
            continue
        fc = rng.uniform(*freq_range)
        g = bp(rng.standard_normal(L), fc / q, min(fc * q, SR / 2 - 200))
        g *= np.exp(-np.linspace(0, decay, L))
        out[s:s + L] += g * a / (np.std(g) + 1e-9)
    return out


def poisson_times(rate_fn, dur, t0=0.0):
    """Willekeurige tijdstippen met een (tijdafhankelijke) dichtheid (per s)."""
    ts, t = [], t0
    while t < dur:
        r = rate_fn(t)
        if r < 0.5:          # (bijna) geen dichtheid: kleine stap vooruit i.p.v. ver weg springen
            t += 0.01
            continue
        t += rng.exponential(1 / r)
        if t < dur:
            ts.append(t)
    return np.array(ts)


# ------------------------------------------------------------------ bouwblokken
def thump(dur, f0, f1, rate, decay, noise_amt=0.3, noise_lp=800):
    """Doffe klap: zakkende sinus + gedempte ruis."""
    t = tax(dur)
    s = np.sin(phase_of(glide(t, f0, f1, rate)))
    nz = lp(white(len(t)), noise_lp) * noise_amt
    return (s + nz) * env_ad(t, 0.0015, decay)


def click(dur=0.01, lo=2000, hi=9000, decay=0.002):
    t = tax(dur)
    return bp(white(len(t)), lo, hi) * np.exp(-t / decay)


def metal_ring(dur, freqs, decays, amps=None, strike=0.004):
    """Metalen resonanties (modale synthese)."""
    t = tax(dur)
    amps = amps or [1.0] * len(freqs)
    out = np.zeros(len(t))
    for f, d, a in zip(freqs, decays, amps):
        out += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t / d)
    out *= 1 - np.exp(-t / 0.0005)
    out += click(dur, 1500, 10000, strike) * 0.5
    return out


def friedlander(dur, T):
    t = tax(dur)
    return np.where(t < 8 * T, (1 - t / T) * np.exp(-1.6 * t / T), 0.0)


def explosion(size=1.0, distance=0.6, brightness=1.0, debris_amt=1.0, metal=0.0,
              dur=None, crack=0.35):
    """Gebaseerd op de goedgekeurde 'explosie op afstand' (variant C)."""
    dur = dur or 1.2 + 1.5 * size
    n = n_of(dur)
    t = np.arange(n) / SR
    T = 0.005 + 0.012 * size
    punch = lp(friedlander(dur, T), 2600 - 1500 * distance) * 1.4
    punch += bp(white(n), 800, 6000) * np.exp(-t * (95 - 50 * distance)) * crack

    boom = lp(brown(n), 110, order=4)
    boom = norm(boom) * (1 - np.exp(-t * 120)) * np.exp(-t * 2.4 / size) * rumble_mod(n, 30, 0.6)

    body = bp(pink(n), 120, 6000)
    body = sweep(body, "low", (7000 * brightness - 3000 * distance, 500))
    body = norm(body) * (1 - np.exp(-t * 60)) * np.exp(-t * 3.2 / size) * rumble_mod(n, 18, 0.8)

    rum = norm(bp(brown(n), 50, 350))
    rum *= (1 - np.exp(-np.clip(t - 0.08, 0, None) * 8)) * np.exp(-t * 1.6 / size) * rumble_mod(n, 9, 0.7)

    k = int(120 * size * debris_amt) + 1
    times = np.concatenate([rng.exponential(0.15 * size, k) + 0.03,
                            rng.uniform(0.4, 0.4 + size, int(10 * size * debris_amt) + 1)])
    amps = np.where(times < 0.5, 1.0, 0.45) * rng.uniform(0.2, 1.0, len(times))
    debris = norm(grains(n, times, (0.002, 0.025), (700, 8000 * brightness - 4000 * distance), amps))

    mix = (punch + boom * (1.1 + 0.3 * size) + body * (0.9 - 0.3 * distance)
           + rum * (0.6 + 0.3 * distance) + debris * (0.35 - 0.2 * distance) * debris_amt)

    if metal > 0:  # metalen delen die rondvliegen/kraken
        for i in range(int(3 + 4 * metal)):
            f0 = rng.uniform(300, 2500)
            ring = metal_ring(0.8, [f0, f0 * 2.76, f0 * 5.4], [0.25, 0.12, 0.05], [1, 0.5, 0.25])
            place(mix, ring * 0.1 * metal, rng.uniform(0.02, 0.6 * size + 0.1))
    mix = lp(mix, 14000 * min(brightness, 1.2) - 10000 * distance)
    return np.tanh(norm(mix) * 1.4)


def footstep(weight=0.5, surface="ground", claws=0.0, squelch=0.0, dur=None):
    """weight 0..1: licht..zwaar. surface: ground|gravel|metal|stone|ice|mud."""
    dur = dur or 0.18 + 0.5 * weight
    n = n_of(dur)
    t = np.arange(n) / SR
    f0 = 140 - 90 * weight
    th = thump(dur, f0 * 1.8, f0, 40, 0.02 + 0.08 * weight, noise_amt=0.5, noise_lp=500 + 900 * (1 - weight))
    out = th * (0.7 + 0.6 * weight)
    if surface in ("ground", "gravel", "stone"):
        dens = {"ground": 250, "gravel": 900, "stone": 400}[surface]
        tt = poisson_times(lambda x: dens * np.exp(-x / 0.05), dur, 0.002)
        lo, hi = {"ground": (600, 3500), "gravel": (1500, 8000), "stone": (1200, 6000)}[surface]
        out += norm(grains(n, tt, (0.001, 0.006), (lo, hi), rng.uniform(0.3, 1, len(tt)))) * 0.35
    if surface == "metal":
        out += metal_ring(dur, [rng.uniform(380, 520), 1150, 2400], [0.08, 0.05, 0.03], [1, .4, .2]) * 0.4
    if surface == "ice":
        tt = poisson_times(lambda x: 500 * np.exp(-x / 0.04), dur, 0.002)
        out += norm(grains(n, tt, (0.001, 0.004), (3000, 11000), rng.uniform(0.3, 1, len(tt)), q=1.3)) * 0.4
    if surface == "mud" or squelch > 0:
        out += squelch_sound(dur, 0.5 + 0.5 * weight) * max(squelch, 0.6)
    if claws > 0:
        for k in range(rng.integers(2, 4)):
            place(out, click(0.02, 2500, 9000, 0.0015), rng.uniform(0.0, 0.03), 0.6 * claws)
    return out


def squelch_sound(dur, size=0.5):
    """Nat/snotterig: gefilterde ruis met snel bewegende resonantie + bubbeltjes."""
    n = n_of(dur)
    t = np.arange(n) / SR
    base = pink(n) * env_ad(t, 0.01, 0.06 + 0.1 * size)
    curve = curve_of(120, lambda u: 300 + 1600 * np.exp(-u * 4) * (1 + 0.5 * np.sin(u * 30)))
    wet = sweep(base, "band", curve * (1.2 - 0.6 * size), width=0.5)
    pops = np.zeros(n)
    for _ in range(int(3 + 5 * size)):
        f = rng.uniform(250, 900) * (1.2 - 0.5 * size)
        L = n_of(rng.uniform(0.01, 0.03))
        tt = np.arange(L) / SR
        b = np.sin(phase_of(f * (1 + 1.5 * tt / tt[-1]) * np.ones(L))) * np.exp(-tt / 0.008)
        place(pops, b, rng.uniform(0, dur * 0.6), rng.uniform(0.2, 0.6))
    return norm(wet) + pops


def bubble(f0=400, dur=0.06, rise=1.0):
    """Losse bel: sinus waarvan de toon stijgt terwijl hij uitsterft."""
    t = tax(dur)
    f = f0 * (1 + rise * t / dur * 1.5)
    return np.sin(phase_of(f)) * np.exp(-t / (dur / 4)) * (1 - np.exp(-t / 0.002))


def voice(dur, pitch, formants, rough=0.0, breath=0.15, sub=0.0, jitter=0.02,
          vowel_curve=None, shape="saw", growl_hz=0.0):
    """Eenvoudige vocale synthese (bron + formantfilters).
    pitch: array of scalar (Hz). formants: lijst (f, bw, gain) of
    lijst van twee sets waartussen vowel_curve (0..1) interpoleert."""
    n = n_of(dur)
    t = np.arange(n) / SR
    p = np.full(n, float(pitch)) if np.isscalar(pitch) else pad(np.asarray(pitch, float), n)
    p = p * (1 + jitter * (smooth_rand(n, 12) - 0.5) * 2)
    src = osc(p, shape)
    if rough > 0:   # ruwe stem: amplitudemodulatie op subharmonische
        src *= 1 - rough + rough * (0.5 + 0.5 * np.sin(phase_of(p * 0.5)))
    if growl_hz > 0:
        src *= 0.55 + 0.45 * rumble_mod(n, growl_hz, 0.9)
    if sub > 0:
        src += sub * osc(p * 0.5, "sine")
    src += breath * pink(n) * 0.3
    if isinstance(formants[0][0], (list, tuple)):
        A, B = formants
        vc = np.zeros(n) if vowel_curve is None else pad(vowel_curve, n)
        out = np.zeros(n)
        blocks = 80
        edges = np.linspace(0, n, blocks + 1, dtype=int)
        for (fa, bwa, ga), (fb, bwb, gb) in zip(A, B):
            curve = np.array([fa + (fb - fa) * vc[min(edges[i], n - 1)] for i in range(blocks)])
            bw = (bwa + bwb) / 2
            g = (ga + gb) / 2
            out += g * sweep(src, "band", curve, width=bw / np.mean(curve) * 2, order=1)
        return out
    out = np.zeros(n)
    for f, bw, g in formants:
        out += g * bp(src, f - bw / 2, f + bw / 2, order=1)
    return out


# formanten (f, bandbreedte, gain)
VOWEL_A = [(750, 160, 1.0), (1200, 180, 0.6), (2600, 300, 0.25)]
VOWEL_O = [(500, 140, 1.0), (850, 160, 0.5), (2500, 300, 0.15)]
VOWEL_U = [(320, 120, 1.0), (800, 160, 0.3), (2300, 300, 0.1)]
VOWEL_E = [(450, 140, 1.0), (1900, 220, 0.5), (2600, 300, 0.25)]
VOWEL_I = [(300, 120, 1.0), (2300, 250, 0.4), (3000, 300, 0.2)]


def screech(dur, f0, f1, rough=0.5, formant_scale=1.0, harsh=0.4, vib=0.0, voices=3):
    """Wezen-schreeuw: meerdere licht ontstemde, onregelmatige stemmen (ruw, levend),
    toonhoogte die opschiet en wegzakt, bewegende formanten en een ademruis-laag."""
    n = n_of(dur)
    t = np.arange(n) / SR
    contour = env_points(t, [(0, f0 * 0.7), (dur * 0.1, f0 * 1.05), (dur * 0.35, f0),
                             (dur * 0.75, (f0 + f1) / 2), (dur, f1 * 0.8)])
    out = np.zeros(n)
    for k in range(voices):
        det = 1 + (k - (voices - 1) / 2) * 0.025
        p = contour * det
        p *= 1 + 0.08 * (smooth_rand(n, 8) - 0.5) + 0.04 * (smooth_rand(n, 30) - 0.5)
        p *= 1 + vib * np.sin(2 * np.pi * rng.uniform(5, 9) * t)
        fm = np.sin(phase_of(p * rng.uniform(1.3, 1.6))) * harsh * p * 0.6
        v = osc(p + fm, "saw")
        v *= 1 - rough * 0.5 + rough * 0.5 * rumble_mod(n, rng.uniform(30, 60), 0.9)
        out += v * (1.0 if k == 0 else 0.6)
    out += pink(n) * 0.35 * rough
    fs = formant_scale
    open_ = env_points(t, [(0, 0.8), (dur * 0.15, 1.15), (dur, 0.85)])
    curve = lambda base: np.interp(np.linspace(0, n, 60), np.arange(n), base * fs * open_)
    res = (sweep(out, "band", curve(1200), width=0.6, order=1)
           + 0.8 * sweep(out, "band", curve(2700), width=0.5, order=1)
           + 0.4 * sweep(out, "band", curve(5000), width=0.5, order=1))
    env = env_points(t, [(0, 0), (0.025, 1), (dur * 0.55, 0.85), (dur, 0)])
    return res * env


def wing_flap(size=0.5, dur=None):
    """Één vleugelslag: 'whoomp' van lucht + licht fladderen van vlies."""
    dur = dur or 0.12 + 0.35 * size
    n = n_of(dur)
    t = np.arange(n) / SR
    puff = sweep(pink(n), "band", curve_of(60, lambda u: (900 - 600 * size) * (1 + 1.2 * np.sin(np.pi * u))),
                 width=1.2) * np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 1.5
    flutter = bp(white(n), 1500, 6000) * (0.5 + 0.5 * np.sin(2 * np.pi * (60 - 30 * size) * t)) \
        * env_ad(t, dur * 0.3, dur * 0.25) * (0.25 - 0.15 * size)
    low = lp(brown(n), 120 + 100 * (1 - size)) * np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 2 * size * 0.8
    return norm(puff) + flutter + low * 0.5


# ------------------------------------------------------------------ ruimte & effecten
def reverb_ir(dur, decay, dark0=9000, dark1=800, predelay=0.005, reflections=()):
    n = n_of(dur)
    t = np.arange(n) / SR
    tail = white(n) * np.exp(-t / decay)
    tail = sweep(tail, "low", (dark0, dark1), blocks=60)
    tail[: n_of(predelay)] = 0
    tail /= np.sqrt(np.sum(tail ** 2)) + 1e-12
    for dt, g in reflections:
        k = n_of(dt)
        if k < n:
            tail[k] += g
    return tail


def air(x, wet=0.15):
    """Minimale buitenruimte (grondreflectie + ~0,1 s), niet hoorbaar als echo."""
    ir = reverb_ir(0.15, 0.03, 12000, 5000, 0.002, [(0.006, 0.35), (0.011, 0.2)])
    return np.concatenate([x, np.zeros(len(ir) - 1)]) + wet * fftconvolve(x, ir)


def echo(x, delays, gains, lowpass=3500):
    """Discrete echo's (alleen voor de boss-howl)."""
    total = len(x) + n_of(max(delays)) + 1
    out = pad(x, total)
    src = lp(x, lowpass)
    for d, g in zip(delays, gains):
        place(out, src, d, g)
        src = lp(src, lowpass * 0.8)
    return out


def pitch_shift(x, factor):
    """Toonhoogte én duur schalen (factor<1: lager en langer = groter)."""
    from fractions import Fraction
    fr = Fraction(1 / factor).limit_denominator(60)
    return resample_poly(x, fr.numerator, fr.denominator)


def bossify(x, factor=0.7, sub=0.5, grit=0.3):
    """Boss-versie: lager/langer, extra sub-laag die de envelope volgt, iets meer gruis."""
    y = pitch_shift(x, factor)
    env = maximum_filter1d(np.abs(y), n_of(0.01))
    env = lp(env, 40)
    subl = lp(brown(len(y)), 90, order=4)
    subl = norm(subl) * env / (env.max() + 1e-9)
    y = norm(y) + sub * subl
    y = np.tanh(norm(y) * (1 + 2 * grit)) / np.tanh(1 + 2 * grit)
    return y


def doppler_pass(x, speed=30, closest=3, t_closest=None):
    """Simpele voorbij-vlieg-doppler + afstandsvolume/-demping."""
    n = len(x)
    t = np.arange(n) / SR
    tc = t_closest if t_closest is not None else t[-1] / 2
    pos = (t - tc) * speed
    dist = np.sqrt(pos ** 2 + closest ** 2)
    delay = dist / 343
    read = np.clip(np.arange(n) - delay * SR, 0, n - 1)
    y = np.interp(read, np.arange(n), x) * (closest / dist)
    curve = np.interp(np.linspace(0, n, 100), np.arange(n), 16000 * (closest / dist) ** 0.5)
    return sweep(y, "low", curve)


def make_loop(x, xfade=0.25):
    """Naadloze loop: staart over het begin heen faden."""
    L = n_of(xfade)
    body = x[:-L].copy()
    tail = x[-L:]
    w = np.linspace(0, 1, L)
    body[:L] = body[:L] * np.sqrt(w) + tail * np.sqrt(1 - w)
    return body


# ------------------------------------------------------------------ mastering
def compress(x, thr_db=-20, ratio=3, rel_ms=80):
    lvl = np.abs(x)
    lvl = maximum_filter1d(lvl, n_of(0.001) * 2 + 1)
    a = np.exp(-1 / (SR * rel_ms / 1000))
    env = lfilter([1 - a], [1, -a], lvl)
    env = np.maximum(env, lvl * 0.0)
    # snelle attack: neem max van gefilterde en ruwe (vertraagde) piek
    env = np.maximum(env, maximum_filter1d(lvl, n_of(0.004)))
    env += 1e-9
    level = 20 * np.log10(env / env.max())
    gdb = np.where(level > thr_db, (thr_db - level) * (1 - 1 / ratio), 0)
    return x * 10 ** (gdb / 20)


def limit(x, ceiling=0.89, knee=0.6, drive=1.0):
    x = norm(x) * drive
    a = np.abs(x)
    x = np.where(a > knee, np.sign(x) * (knee + (1 - knee) * np.tanh((a - knee) / (1 - knee))), x)
    return norm(x) * ceiling


def master(x, lufs=-14.0, comp=True, keep_tail=False, loop=False, fade_ms=30):
    if loop:
        # loops: verwerk drie aaneengesloten kopieën en houd de middelste over, zodat filters
        # en compressor bij de naad in dezelfde toestand zijn -> naadloos, geen klik
        x = np.asarray(x, float)
        n = len(x)
        y = _master(np.concatenate([x, x, x]), lufs, comp, True, fade_ms)
        return y[n:2 * n]
    return _master(x, lufs, comp, False, fade_ms)


def _master(x, lufs=-14.0, comp=True, loop=False, fade_ms=30):
    """Opschonen, comprimeren, luidheid instellen, pieken begrenzen."""
    x = np.asarray(x, float)
    if x.ndim == 1:
        x = x - np.mean(x[: n_of(0.002)]) * 0  # placeholder (geen DC-shift nodig)
        x = hp(x, 25)
    if not loop:
        env = np.abs(x) if x.ndim == 1 else np.max(np.abs(x), axis=1)
        nz = np.nonzero(env > env.max() * 10 ** (-60 / 20))[0]
        last = nz[-1] + n_of(0.02)
        first = max(0, np.nonzero(env > env.max() * 10 ** (-45 / 20))[0][0] - n_of(0.003))
        x = x[first:last]
        f = min(n_of(fade_ms / 1000), len(x) // 4)
        fade = np.linspace(1, 0, f)
        x[-f:] *= fade if x.ndim == 1 else fade[:, None]
        s = min(n_of(0.001), len(x) // 10)
        x[:s] *= (np.linspace(0, 1, s) if x.ndim == 1 else np.linspace(0, 1, s)[:, None])
    if comp:
        x = compress(x, -18, 2) if x.ndim == 1 else np.stack(
            [compress(x[:, 0], -18, 2), compress(x[:, 1], -18, 2)], 1)
    x = norm(x) * 0.89
    for _ in range(4):
        L = loudness(x)
        if not np.isfinite(L):
            break
        x = x * 10 ** ((lufs - L) / 20)
        if np.max(np.abs(x)) <= 0.89:
            break
        x = softclip(x, 0.89)
    x = softclip(x, 0.89)
    # begrenzen kan bij scheve golfvormen een DC-offset geven: verwijder die
    x = hp(x, 20) if x.ndim == 1 else np.stack([hp(x[:, 0], 20), hp(x[:, 1], 20)], 1)
    pk = np.max(np.abs(x))
    return x * (0.9 / pk) if pk > 0.9 else x


def softclip(x, ceiling=0.89, knee=0.7):
    """Begrenst pieken op `ceiling` zonder de rest te verzachten."""
    k = ceiling * knee
    a = np.abs(x)
    over = a > k
    y = x.copy()
    y[over] = np.sign(x[over]) * (k + (ceiling - k) * np.tanh((a[over] - k) / (ceiling - k)))
    return y


def loudness(x):
    n = n_of(0.45)
    if len(x) < n:
        x = np.concatenate([x, np.zeros((n - len(x),) + x.shape[1:])])
    try:
        return _meter.integrated_loudness(x)
    except Exception:
        return float("nan")


def save(path, x, **mkw):
    y = master(x, **mkw)
    full = os.path.join(OUT, path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    wavfile.write(full, SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    return full


def stereo(x, width_ms=0.006, ratio=0.85):
    d = n_of(width_ms / 1000 * 1000) if False else n_of(width_ms)
    r = pad(np.concatenate([np.zeros(d), x]), len(x))
    return np.stack([x * ratio + r * (1 - ratio), x * (1 - ratio) + r * ratio], 1)


# ------------------------------------------------------------------ register
REGISTRY = []


def sound(path, title, desc="", variants=1, **save_kw):
    """Decorator: registreer een geluid. De functie krijgt (v) = variantnummer
    en geeft een array terug. path zonder .wav; varianten krijgen _01, _02, ..."""
    def deco(fn):
        REGISTRY.append(dict(path=path, title=title, desc=desc, variants=variants,
                             fn=fn, save_kw=save_kw))
        return fn
    return deco


def render(entry, seed_base=0):
    files = []
    import zlib
    for v in range(entry["variants"]):
        seed(zlib.crc32(entry["path"].encode()) + 7919 * v + seed_base)
        x = entry["fn"](v)
        p = entry["path"] + (f"_{v + 1:02d}" if entry["variants"] > 1 else "") + ".wav"
        save(p, x, **entry["save_kw"])
        files.append(p)
    return files


def mix(*parts):
    """Tel arrays van verschillende lengte bij elkaar op (paddend tot de langste)."""
    n = max(len(p) for p in parts)
    return sum(pad(np.asarray(p, float), n) for p in parts)
