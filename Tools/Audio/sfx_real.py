"""Realistischere procedurele game-geluiden (stereo, gelaagd, met galm).

Technieken:
- Laser: "dispersie" — een klik door honderden allpass-filters jagen. Zo werd
  ook de Star Wars-blaster gemaakt (klap op een gespannen staalkabel).
- Raket: turbulente ruis met willekeurige modulatie, uitlaat-geknetter, en
  doppler + luchtdemping terwijl hij wegvliegt.
- Explosie: drukgolf-transient, sub-bas, rollende middenlaag, brokstukken,
  echo's tegen de omgeving en een lange donkere galmstaart.

Gebruik: python3 sfx_real.py  -> laser_real.wav, rocket_real.wav, explosion_real.wav
"""
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, fftconvolve

SR = 44100
rng = np.random.default_rng(42)


# ------------------------------------------------------------ helpers
def n_of(sec):
    return int(SR * sec)


def t_axis(dur):
    return np.arange(n_of(dur)) / SR


def filt(x, kind, f, order=2):
    return sosfilt(butter(order, f, kind, fs=SR, output="sos"), x)


def brown(n):
    x = np.cumsum(rng.standard_normal(n))
    return filt(x, "high", 20)


def pink(n):
    # Voss-achtige benadering via spectrale vorm
    X = np.fft.rfft(rng.standard_normal(n))
    f = np.arange(len(X)); f[0] = 1
    return np.fft.irfft(X / np.sqrt(f), n)


def smooth_rand(n, hz):
    """Langzaam golvende willekeurige curve (0..1) voor organische modulatie."""
    x = filt(rng.standard_normal(n), "low", hz, order=2)
    x -= x.min()
    return x / (x.max() + 1e-9)


def sweep_low(x, f0, f1, blocks=300, curve=None):
    """Lowpass met verschuivende cutoff."""
    out = np.zeros_like(x)
    edges = np.linspace(0, len(x), blocks + 1, dtype=int)
    cut = np.geomspace(f0, f1, blocks) if curve is None else curve
    zi = np.zeros((1, 2))
    for i in range(blocks):
        sos = butter(2, min(cut[i], SR / 2 - 100), "low", fs=SR, output="sos")
        out[edges[i]:edges[i + 1]], zi = sosfilt(sos, x[edges[i]:edges[i + 1]], zi=zi)
    return out


def grains(n, times, dur_range, freq_range, amp):
    """Korte gefilterde ruisstootjes: knetters, brokstukken."""
    out = np.zeros(n)
    for t0, a in zip(times, amp):
        d = rng.uniform(*dur_range)
        L = n_of(d)
        s = n_of(t0)
        if s + L >= n or L < 8:
            continue
        fc = rng.uniform(*freq_range)
        g = filt(rng.standard_normal(L), "band", [fc * 0.6, min(fc * 1.6, 20000)])
        g *= np.exp(-np.linspace(0, 6, L))
        out[s:s + L] += g * a
    return out


def reverb_ir(dur, decay, dark_start=9000, dark_end=800, predelay=0.01, reflections=()):
    """Synthetische stereo impulsrespons: vroege reflecties + donker wordende staart."""
    n = n_of(dur)
    t = np.arange(n) / SR
    ir = np.zeros((n, 2))
    for ch in range(2):
        tail = rng.standard_normal(n) * np.exp(-t / decay)
        tail = sweep_low(tail, dark_start, dark_end, blocks=100)
        tail[: n_of(predelay)] = 0
        ir[:, ch] = tail / np.sqrt(np.sum(tail ** 2))  # staart = zelfde energie als droog
        for (dt, g) in reflections:
            k = n_of(dt * rng.uniform(0.9, 1.1))
            if k < n:
                ir[k, ch] += g
    return ir


def apply_reverb(x, ir, wet=0.35):
    """x mono -> stereo met galm."""
    out = np.zeros((len(x) + len(ir) - 1, 2))
    dry = np.zeros(len(out)); dry[: len(x)] = x
    for ch in range(2):
        out[:, ch] = dry + wet * fftconvolve(x, ir[:, ch])
    return out


def stereo_widen(x, ms=0.012):
    """Mono -> stereo met kleine decorrelatie."""
    d = n_of(ms)
    r = np.concatenate([np.zeros(d), x])[: len(x)]
    return np.stack([x * 0.9 + r * 0.1, x * 0.1 + r * 0.9], axis=1)


def brighten(x, f=2500, g=0.8):
    """High-shelf-achtige boost: meer helderheid en 'bite'."""
    return x + g * filt(x, "high", f)


def air(x, wet=0.22):
    """Minimale buitenruimte: grondreflectie + staart van ~0,1 s. Geen hoorbare echo."""
    ir = reverb_ir(0.15, 0.03, 12000, 5000, predelay=0.002,
                   reflections=[(0.006, 0.35), (0.011, 0.2)])
    return apply_reverb(x, ir, wet=wet)


def compress(x, thr_db=-16, ratio=4, att_ms=1.0, rel_ms=60):
    """Compressor: maakt het geluid voller en luider (kleiner verschil tussen piek en lichaam)."""
    from scipy.ndimage import maximum_filter1d
    from scipy.signal import lfilter
    lvl = np.max(np.abs(x), axis=1) if x.ndim == 2 else np.abs(x)
    lvl = maximum_filter1d(lvl, n_of(att_ms / 1000) * 2 + 1)
    # piekvolger: direct omhoog (attack), langzaam omlaag (release)
    a = np.exp(-1 / (SR * rel_ms / 1000))
    env = np.empty_like(lvl)
    e = 0.0
    for i, v in enumerate(lvl):
        e = v if v > e else a * e + (1 - a) * v
        env[i] = e
    env += 1e-9
    level_db = 20 * np.log10(env / env.max())
    gain_db = np.where(level_db > thr_db, (thr_db - level_db) * (1 - 1 / ratio), 0)
    g = 10 ** (gain_db / 20)
    return x * (g[:, None] if x.ndim == 2 else g)


def finish(x, peak=0.95, limit=True):
    if x.ndim == 1:
        x = np.stack([x, x], axis=1)
    # kap stilte af
    env = np.max(np.abs(x), axis=1)
    thr = env.max() * 1e-3
    last = np.nonzero(env > thr)[0][-1] + n_of(0.05)
    x = x[:last]
    f = min(n_of(0.15), len(x) // 4)
    x[-f:] *= np.linspace(1, 0, f)[:, None]
    if limit == "comp":
        x = compress(x)
        x = np.tanh(x / np.max(np.abs(x)) * 1.3)  # limiter
    elif limit:
        x = np.tanh(x / np.max(np.abs(x)) * 1.2)  # zachte limiter (tilt staarten op)
    return x / np.max(np.abs(x)) * peak


def save(name, x, limit=True):
    wavfile.write(name, SR, (finish(x, limit=limit) * 32767).astype(np.int16))
    print("saved", name)


# ------------------------------------------------------------ laser (blaster)
def dispersion_chirp(n, stages, a):
    """Klik door N first-order allpass-filters (in het frequentiedomein, exact)."""
    imp = np.zeros(n); imp[0] = 1
    w = np.linspace(0, np.pi, n // 2 + 1)
    z = np.exp(-1j * w)
    H = ((a + z) / (1 + a * z)) ** stages
    return np.fft.irfft(np.fft.rfft(imp) * H, n)


def laser():
    n = n_of(0.9)
    # twee iets verschillende "kabels" geven een rijkere, levende klank
    c1 = dispersion_chirp(n, 700, -0.92)
    c2 = dispersion_chirp(n, 560, -0.90)
    c3 = dispersion_chirp(n, 900, -0.935) * 0.5
    pew = filt(c1 + 0.7 * c2 + c3, "band", [250, 9000])
    t = np.arange(n) / SR
    pew *= np.exp(-t * 4)
    # metalen klik bij de aanzet
    click = filt(rng.standard_normal(n), "band", [2500, 12000]) * np.exp(-t * 300) * 0.15
    # energie-"thump" in het laag
    thump = np.sin(2 * np.pi * (180 * np.exp(-t * 30) + 60) * t) * np.exp(-t * 25) * 0.25
    dry = pew / np.max(np.abs(pew)) + click + thump
    ir = reverb_ir(0.8, 0.18, 7000, 1500, reflections=[(0.03, 0.4), (0.055, 0.25)])
    return apply_reverb(dry, ir, wet=0.15)


# ------------------------------------------------------------ raket
def rocket(dur=2.8):
    n = n_of(dur)
    t = np.arange(n) / SR
    # ontsteking: pof + sis die aanzwelt
    pop = filt(rng.standard_normal(n), "low", 1200) * np.exp(-t * 35) * 1.5
    pop += np.sin(2 * np.pi * (90 * np.exp(-t * 20) + 40) * t) * np.exp(-t * 18)
    hiss = filt(rng.standard_normal(n), "high", 3000) * np.clip(t / 0.15, 0, 1) * 0.35

    # turbulente brul: mix van bruine + roze ruis, onregelmatig gemoduleerd
    turb = 0.5 + 0.5 * smooth_rand(n, 18)
    roar = (brown(n) / 40 + pink(n) * 12) * turb
    crackle_times = []
    tt = 0.12
    while tt < dur:
        tt += rng.exponential(0.006 + 0.03 * (tt / dur))
        crackle_times.append(tt)
    crackle = grains(n, crackle_times, (0.001, 0.006), (900, 7000),
                     rng.uniform(0.3, 1.0, len(crackle_times)) * 3)

    thrust_env = (1 - np.exp(-np.clip(t - 0.08, 0, None) * 12))
    src = (roar + crackle * 1.4 + hiss) * thrust_env

    # afstand: raket vliegt weg -> zachter, doffer, doppler
    dist = 4 + 30 * np.clip(t - 0.4, 0, None) ** 1.7       # meters
    gain = 4 / dist
    cutoff = np.clip(18000 * (4 / dist) ** 0.5, 3000, 18000)  # luchtdemping
    blocks = 300
    curve = np.interp(np.linspace(0, n, blocks), np.arange(n), cutoff)
    src = sweep_low(src, 0, 0, blocks=blocks, curve=curve) * gain
    # doppler: variabele vertraging = afstand / geluidssnelheid
    delay = dist / 343.0
    read = np.clip(np.arange(n) - delay * SR, 0, n - 1)
    src = np.interp(read, np.arange(n), src)

    # strakke uitloop zodat er geen lange "zaal"-staart blijft hangen
    src *= np.exp(-np.clip(t - 1.6, 0, None) * 3.5)
    return air(brighten(src + pop, 3000, 0.9))


# ------------------------------------------------------------ explosie
def explosion(dur=3.0):
    n = n_of(dur)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    turb = 0.6 + 0.4 * smooth_rand(n, 12)

    # 1. blast: ~0,12 s volle breedband knal, daarna wegzakkend; zwaar vervormd
    #    zoals een echte opname waarbij de microfoon de druk niet aankan
    blast_env = np.where(t < 0.12, 1.0, np.exp(-(t - 0.12) * 5)) * (1 - np.exp(-t * 2000))
    blast = sweep_low(pink(n) * 12 + noise * 0.3, 18000, 900) * blast_env
    blast = np.tanh(blast * 4)

    # 2. laag lichaam: bruine ruis, ook vervormd (geen schone sinus = geen "bonk")
    low = filt(brown(n), "low", 160, order=4)
    low = low / np.max(np.abs(low)) * (1 - np.exp(-t * 150)) * np.exp(-t * 2.5)
    low = np.tanh(low * 2.5) * turb

    # 3. rollend gerommel dat langzaam donkerder wordt
    roll = (pink(n) * 10 + brown(n) / 60) * turb
    roll = sweep_low(roll, 12000, 400) * (1 - np.exp(-t * 40)) * np.exp(-t * 2.2)

    # 4. brokstukken
    times = np.concatenate([rng.exponential(0.18, 260) + 0.03, rng.uniform(0.4, 1.6, 20)])
    amps = np.where(times < 0.6, 1.0, 0.5) * rng.uniform(0.2, 1.0, len(times))
    debris = grains(n, times, (0.002, 0.03), (900, 12000), amps * 1.5)

    # 5. sis/gruis bovenin
    sizzle = filt(noise, "high", 4000) * (1 - np.exp(-t * 300)) * np.exp(-t * 5)
    sizzle *= 0.5 + 0.5 * smooth_rand(n, 25)

    dry = blast + low * 1.3 + roll * 0.8 + debris * 0.8 + sizzle * 0.5
    dry = np.tanh(dry * 1.5)
    return air(brighten(dry, 2500, 0.5))


if __name__ == "__main__":
    save("laser_real.wav", laser())
    save("rocket_real.wav", rocket(), limit="comp")
    save("explosion_real.wav", explosion(), limit="comp")
