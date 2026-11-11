"""Dialogue box sounds: incoming broadcast, typing blips per speaker, transmission end. Candidates for Remco.

"Think The Sims or Pokemon, but spacey." Text types at 45 characters/s; a blip plays every ~2 visible characters,
so a blip may repeat ~22 times a second: blips are short, round, quiet and low-passed (no harsh highs).

Usage (system Python with numpy/scipy/pyloudnorm):
  python cat_dialogue.py     render everything to out/dialogue/, print checks, write out/dialogue/manifest.md

Levels: a blip is levelled so that a train of it at 22.5 blips/s measures -24 LUFS (the worst-case typing density);
broadcast and end sounds are levelled to -20 LUFS. All peaks at or under -3 dBFS, mono, 44.1 kHz 16-bit.
"""
import os
import zlib

import numpy as np
from scipy.io import wavfile

import sfxlib as s
from sfxlib import SR, n_of, tax, norm, pad, place, lp, hp, bp

OUT_DIR = os.path.join(s.OUT, "dialogue")
PEAK_CEILING = 10 ** (-3.1 / 20)
BLIP_RATE = 22.5                 # blips per second at 45 characters/s, one blip per two characters
BLIP_LUFS = -24.0
EVENT_LUFS = -20.0
VARIANTS = 6
SEMITONES = (0.0, 1.5, -1.0, 2.0, -2.0, 0.5)          # pitch spread over the 6 samples of a set
VOWELS = (s.VOWEL_A, s.VOWEL_E, s.VOWEL_O, s.VOWEL_I, s.VOWEL_U, s.VOWEL_A)

# Per-speaker character. note: base pitch for the tonal styles (S1/S2); f0: voice pitch for S3;
# formant: formant scale for S3; timbre: 'warm' | 'bright' | 'pure' | 'metal' | 'machine'.
SPEAKERS = {
    "spencer": dict(note=392.0, f0=125.0, formant=1.0, timbre="warm"),
    "voss": dict(note=659.0, f0=220.0, formant=1.17, timbre="bright"),
    "ora": dict(note=523.0, f0=175.0, formant=1.08, timbre="pure"),
    "nti": dict(note=294.0, f0=100.0, formant=0.9, timbre="metal"),
    "pioneer1": dict(note=440.0, f0=140.0, formant=1.0, timbre="machine"),
}

ENTRIES = []                     # dict(path, group, desc, kind, fn)


def entry(path, group, desc, kind):
    def deco(fn):
        ENTRIES.append(dict(path=path, group=group, desc=desc, kind=kind, fn=fn))
        return fn
    return deco


# ================================================================== helpers
def semis(f, st):
    return f * 2 ** (st / 12)


def fade(x, fin=0.002, fout=0.01):
    x = np.array(x, float)
    for length, rev in ((fin, False), (fout, True)):
        k = min(n_of(length), len(x) // 2)
        if k < 2:
            continue
        w = 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, k))
        if rev:
            x[-k:] *= w[::-1]
        else:
            x[:k] *= w
    return x


def crush(x, bits=5, hold_hz=7000):
    """Bitcrush: sample-and-hold to a low rate and coarse quantisation (machine voice)."""
    step = max(1, int(round(SR / hold_hz)))
    y = np.repeat(x[::step], step)[:len(x)]
    q = 2 ** (bits - 1)
    return np.round(norm(y) * q) / q


def radio(x, drive=1.8):
    return np.tanh(bp(x, 400, 3400) * drive) / np.tanh(drive)


def tone(freq, dur, partials=((1, 1.0),)):
    """Sum of partials over a pitch curve (scalar or array)."""
    n = n_of(dur)
    f = np.full(n, float(freq)) if np.isscalar(freq) else pad(np.asarray(freq, float), n)
    ph = s.phase_of(f)
    return sum(a * np.sin(k * ph) for k, a in partials)


def env_blip(n, attack=0.003, decay=0.016):
    t = np.arange(n) / SR
    return s.env_ad(t, attack, decay)


def timbre_partials(timbre):
    return {"warm": ((1, 1.0), (2, 0.22), (3, 0.06)),
            "bright": ((1, 1.0), (2, 0.12), (4, 0.05)),
            "pure": ((1, 1.0),),
            "metal": ((1, 1.0), (2.76, 0.22), (5.4, 0.07)),
            "machine": ((1, 1.0), (3, 0.3), (5, 0.15))}[timbre]


# ================================================================== typing styles
def s1_soft_blip(spk, v):
    """Round sine blip with a tiny pitch lift, ~42 ms."""
    p = SPEAKERS[spk]
    dur = 0.042
    n = n_of(dur)
    t = np.arange(n) / SR
    f0 = semis(p["note"], SEMITONES[v])
    lift = (1.0, 1.04, 0.97, 1.03, 1.0, 0.98)[v]
    f = f0 * (1 + (lift - 1) * np.clip(t / 0.02, 0, 1))
    x = tone(f, dur, timbre_partials(p["timbre"])) * env_blip(n, 0.003, 0.014)
    if p["timbre"] == "machine":
        x = crush(x, 5, 6000)
    return lp(x, 5000, 4)


def s2_radio_chirp(spk, v):
    """Short FM bleep (two micro-steps) through a radio band, ~38 ms."""
    p = SPEAKERS[spk]
    dur = 0.038
    n = n_of(dur)
    t = np.arange(n) / SR
    f0 = semis(p["note"] * 2, SEMITONES[v])
    step = (1.12, 0.9, 1.19, 0.94, 1.06, 0.84)[v]
    f = np.where(t < 0.017, f0, f0 * step)
    ratio, index = {"warm": (1.0, 1.2), "bright": (2.0, 0.9), "pure": (1.0, 0.4),
                    "metal": (1.41, 2.2), "machine": (3.0, 1.6)}[p["timbre"]]
    mod = np.sin(s.phase_of(f * ratio)) * index * np.exp(-t / 0.02)
    x = np.sin(s.phase_of(f) + mod)
    if p["timbre"] == "metal":
        x *= np.sin(2 * np.pi * 173 * t) * 0.5 + 0.5
    if p["timbre"] == "machine":
        x = crush(x, 4, 5000)
    x *= env_blip(n, 0.002, 0.013)
    return radio(x)


def s3_voice_blip(spk, v):
    """Gibberish syllable: a pitched source through vowel formants (Animal Crossing / Pokemon style), ~60 ms."""
    p = SPEAKERS[spk]
    dur = (0.058, 0.064, 0.052, 0.06, 0.066, 0.055)[v]
    n = n_of(dur)
    t = np.arange(n) / SR
    f0 = semis(p["f0"], SEMITONES[v])
    bend = (1.06, 0.95, 1.1, 0.97, 1.0, 1.04)[v]
    pitch = f0 * (1 + (bend - 1) * t / dur)
    shape = {"warm": "saw", "bright": "saw", "pure": "tri", "metal": "saw", "machine": "square"}[p["timbre"]]
    if p["timbre"] == "machine":
        pitch = np.full(n, f0)                            # monotone
    formants = [(f * p["formant"], bw * p["formant"], g) for f, bw, g in VOWELS[v]]
    x = s.voice(dur, pitch, formants, breath={"pure": 0.0, "machine": 0.0}.get(p["timbre"], 0.12),
                jitter=0.0 if p["timbre"] in ("pure", "machine") else 0.015, shape=shape)
    if p["timbre"] == "metal":
        x = 0.6 * x + 0.4 * x * np.sin(2 * np.pi * 61 * t)
    if p["timbre"] == "machine":
        x = crush(x, 5, 8000)
    x = x * s.env_points(t, [(0, 0), (0.006, 1), (dur - 0.02, 0.85), (dur, 0)])
    return lp(hp(x, 120), 5000, 4)


STYLES = {"S1_soft_blip": (s1_soft_blip, "round sine blip, small pitch lift, per-speaker partials (warm / bright / "
                                         "pure / inharmonic metal / bitcrushed machine), low-passed 5 kHz"),
          "S2_radio_chirp": (s2_radio_chirp, "FM bleep with a pitch micro-step, through a 400-3400 Hz radio band "
                                             "with soft drive; NTI ring-modulated, Pioneer-1 bitcrushed"),
          "S3_voice_blip": (s3_voice_blip, "gibberish syllable: saw/tri/square source through A/E/O/I/U formants "
                                           "scaled per speaker, NTI ring-modulated, Pioneer-1 monotone + bitcrushed, "
                                           "low-passed 5 kHz")}

for _style, (_fn, _desc) in STYLES.items():
    for _spk in SPEAKERS:
        for _v in range(VARIANTS):
            entry(f"typing/{_style}/{_spk}_{_v + 1:02d}", "typing", _desc, "blip")(
                (lambda f, sp, vv: (lambda: f(sp, vv)))(_fn, _spk, _v))


# ================================================================== broadcast (box opens)
@entry("broadcast/broadcast_A_comm_chirp", "broadcast",
       "two quick rising sine tones (1150->1450, 1550->1950 Hz) with a tiny static/carrier burst in front, radio band, "
       "short soft tail", "event")
def broadcast_a():
    out = np.zeros(n_of(0.38))
    t = tax(0.035)
    static = bp(s.white(len(t)), 1000, 4000) * s.env_ad(t, 0.002, 0.012)
    place(out, static * 0.25, 0.0)
    for at, f0, f1, d in ((0.012, 1150, 1450, 0.06), (0.095, 1550, 1950, 0.085)):
        tt = tax(d)
        x = tone(f0 + (f1 - f0) * tt / d, d, ((1, 1.0), (2, 0.08))) * fade(np.ones(len(tt)), 0.004, 0.025)
        place(out, x * 0.8, at)
    y = radio(out, 1.4)
    ir = s.reverb_ir(0.25, 0.06, 6000, 2500, 0.01)
    return pad(y, len(y)) + 0.25 * np.convolve(y, ir)[:len(y)]


@entry("broadcast/broadcast_B_data_trill", "broadcast",
       "fast soft arpeggio of 5 glassy blips (E5 G5 A5 C6 E6, sine + 2.76 partial) over a short filtered noise "
       "swoosh rising 600->3000 Hz", "event")
def broadcast_b():
    out = np.zeros(n_of(0.48))
    t = tax(0.3)
    sw = s.sweep(s.pink(len(t)), "band", (600, 3000), width=0.9) * np.sin(np.pi * t / t[-1]) ** 2
    place(out, norm(sw) * 0.18, 0.0)
    for k, f in enumerate((659.3, 784.0, 880.0, 1046.5, 1318.5)):
        d = 0.09
        x = tone(f, d, ((1, 1.0), (2.76, 0.18))) * env_blip(n_of(d), 0.002, 0.03)
        place(out, x * (0.7 + 0.06 * k), 0.045 * k)
    return lp(out, 6000)


@entry("broadcast/broadcast_C_sonar_pulse", "broadcast",
       "soft sonar ping (880 Hz, slight downward glide) over a short resonant filter sweep 300->1800 Hz on a saw/noise "
       "bed, a hint of radio band and a short tail", "event")
def broadcast_c():
    dur = 0.65
    out = np.zeros(n_of(dur))
    d = 0.16
    n = n_of(d)
    src = s.osc(np.full(n, 110.0), "saw") * 0.5 + s.white(n) * 0.3
    sw = s.sweep(src, "band", (300, 1800), width=0.25) * s.env_ad(tax(d), 0.004, 0.06)
    place(out, norm(sw) * 0.45, 0.0)
    t = tax(0.6)
    ping = tone(s.glide(t, 900, 880, 20), 0.6, ((1, 1.0), (2, 0.06))) * s.env_ad(t, 0.003, 0.14)
    place(out, ping * 0.8, 0.01)
    y = 0.6 * out + 0.4 * radio(out, 1.2)
    ir = s.reverb_ir(0.4, 0.1, 5000, 1500, 0.02)
    return y + 0.3 * np.convolve(y, ir)[:len(y)]


# ================================================================== radio-static openers (Remco's choice)
STATIC_SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src_intro", "radio-static.wav")


def static(t0, dur):
    """Mono slice of Remco's radio-static recording, through a soft radio band with no highs above ~5 kHz."""
    _, x = wavfile.read(STATIC_SRC)
    x = x.astype(float) / 32768
    x = x.mean(axis=1) if x.ndim == 2 else x
    seg = x[n_of(t0):n_of(t0) + n_of(dur)]
    return lp(bp(seg, 350, 3800), 4500, 4)


@entry("broadcast/open_static_short", "broadcast",
       "radio-static.mp3: a 0.25 s crackle burst (fast crackle AM) through the radio band, 3 ms attack, quick decay",
       "event")
def open_static_short():
    d = 0.25
    x = static(0.6, d)
    t = np.arange(len(x)) / SR
    crackle = s.rumble_mod(len(x), 45, 0.35)
    return x * crackle * s.env_points(t, [(0, 0), (0.003, 1.0), (0.07, 0.75), (d, 0.0)])


@entry("broadcast/open_static_squelch", "broadcast",
       "radio-static.mp3: static swelling in over 50 ms, held, then a squelch cut at 0.4 s (8 ms raised-cosine drop, "
       "walkie-talkie key-up)", "event")
def open_static_squelch():
    d = 0.42
    x = static(1.8, d)
    t = np.arange(len(x)) / SR
    envl = s.env_points(t, [(0, 0.25), (0.05, 1.0), (0.39, 0.9), (0.4, 0.9)])
    cut = np.clip((t - 0.4) / 0.008, 0, 1)
    envl *= 0.5 + 0.5 * np.cos(np.pi * cut)
    return x * envl


@entry("broadcast/open_static_tune", "broadcast",
       "radio-static.mp3: static through a band-pass sliding 2800 -> 900 Hz and settling at 1400 Hz, with a faint "
       "falling tuning whistle, fading out from 0.3 s as the voice would start", "event")
def open_static_tune():
    d = 0.6
    x = static(3.2, d)
    t = np.arange(len(x)) / SR
    centre = s.curve_of(120, lambda u: 1400 + 1400 * np.exp(-u * 6) * np.cos(u * 9))
    y = s.sweep(x, "band", centre, width=0.7)
    whistle = np.sin(s.phase_of(s.glide(t, 1700, 1050, 9))) * s.env_points(t, [(0, 0), (0.03, 1), (0.25, 0.3), (0.4, 0)])
    y = norm(y) + 0.06 * whistle
    return y * s.env_points(t, [(0, 0), (0.004, 1.0), (0.3, 0.85), (d, 0.0)])


# ================================================================== transmission end (box closes)
@entry("end/end_A_blip_down", "end", "two short falling tones (1450->1250, 1100->900 Hz) through the radio band with "
       "a tiny static tick at the end", "event")
def end_a():
    out = np.zeros(n_of(0.22))
    for at, f0, f1, d in ((0.0, 1450, 1250, 0.05), (0.065, 1100, 900, 0.07)):
        tt = tax(d)
        place(out, tone(f0 + (f1 - f0) * tt / d, d) * fade(np.ones(len(tt)), 0.003, 0.025) * 0.8, at)
    tick = bp(s.white(n_of(0.012)), 1500, 4000) * np.exp(-np.arange(n_of(0.012)) / n_of(0.003))
    place(out, tick * 0.2, 0.14)
    return radio(out, 1.3)


@entry("end/end_B_soft_click", "end", "a soft muted click and one round sine blip gliding down 880->560 Hz", "event")
def end_b():
    out = np.zeros(n_of(0.2))
    place(out, lp(s.click(0.012, 800, 4000, 0.002), 3000) * 0.5, 0.0)
    t = tax(0.17)
    place(out, tone(s.glide(t, 880, 560, 18), 0.17, ((1, 1.0), (2, 0.1))) * s.env_ad(t, 0.004, 0.05) * 0.8, 0.008)
    return lp(out, 6000)


# ================================================================== render and checks
def train_loudness(x):
    """Loudness of a blip repeated at the worst-case typing rate for 2 s."""
    out = np.zeros(n_of(2.0))
    k = 0
    while k / BLIP_RATE < 1.95:
        place(out, x, k / BLIP_RATE)
        k += 1
    return s.loudness(out)


def finish(x, kind):
    x = hp(np.asarray(x, float), 80)
    x = fade(x, 0.0015, 0.008 if kind == "blip" else 0.03)
    if kind == "blip":
        x *= 10 ** ((BLIP_LUFS - train_loudness(x)) / 20)
    else:
        x *= 10 ** ((EVENT_LUFS - s.loudness(x)) / 20)
    pk = np.max(np.abs(x))
    if pk > PEAK_CEILING:
        x *= PEAK_CEILING / pk
    return x - np.mean(x) * np.hanning(len(x)) / np.mean(np.hanning(len(x)))


def check(x, kind):
    pk = np.max(np.abs(x))
    first = np.nonzero(np.abs(x) > pk * 0.05)[0][0] / SR * 1000
    A = np.abs(np.fft.rfft(x, 1 << 14)) ** 2
    f = np.fft.rfftfreq(1 << 14, 1 / SR)
    hi = 10 * np.log10(np.sum(A[f > 6000]) / np.sum(A) + 1e-12)
    lufs = train_loudness(x) if kind == "blip" else s.loudness(x)
    return dict(dur=len(x) / SR * 1000, peak=20 * np.log10(pk), lufs=lufs, start_ms=first, hi_db=hi,
                dc=abs(np.mean(x)), edge=max(abs(x[0]), abs(x[-1])))


def main():
    rows = []
    for e in ENTRIES:
        s.seed(zlib.crc32(e["path"].encode()))
        y = finish(e["fn"](), e["kind"])
        full = os.path.join(OUT_DIR, e["path"] + ".wav")
        os.makedirs(os.path.dirname(full), exist_ok=True)
        wavfile.write(full, SR, (np.clip(y, -1, 1) * 32767).round().astype(np.int16))
        _, z = wavfile.read(full)
        r = check(z.astype(float) / 32767, e["kind"])
        flags = [k for k, bad in (("PEAK", r["peak"] > -3.0), ("PREROLL", r["start_ms"] > 3.0),
                                  ("DC", r["dc"] > 0.002), ("EDGE", r["edge"] > 0.002)) if bad]
        rows.append((e, r))
        print(f"{e['path']:42s} {r['dur']:5.0f} ms pk {r['peak']:6.1f} dBFS {r['lufs']:6.1f} LUFS "
              f"start {r['start_ms']:.1f} ms >6k {r['hi_db']:6.1f} dB  [{' '.join(flags) or 'ok'}]")
    write_manifest(rows)


def write_manifest(rows):
    L = ["# Dialogue box sounds (candidates)", "",
         "Generated by `Tools/Audio/cat_dialogue.py` (pure synthesis, fixed seeds). Mono, 44.1 kHz, peaks <= -3 dBFS, "
         "sound starts within 3 ms.", "",
         "- **Typing blips**: levelled so a train at 22.5 blips/s (45 characters/s, one blip per 2 characters) "
         "measures -24 LUFS. 6 samples per speaker: pitch spread "
         + ", ".join(f"{st:+g}" for st in SEMITONES) + " semitones, S3 vowels A E O I U A.",
         "- **Broadcast / end**: -20 LUFS integrated.",
         "- Speakers: " + "; ".join(f"{k}: note {v['note']:.0f} Hz, voice f0 {v['f0']:.0f} Hz, {v['timbre']}"
                                    for k, v in SPEAKERS.items()), ""]
    for group, title in (("broadcast", "Incoming broadcast (box opens)"), ("end", "Transmission end (box closes)")):
        L += [f"## {title}", "", "| File | Length | LUFS | Peak | Energy > 6 kHz | Description |", "|---|---|---|---|---|---|"]
        for e, r in rows:
            if e["group"] == group:
                L.append(f"| `{e['path']}.wav` | {r['dur']:.0f} ms | {r['lufs']:.1f} | {r['peak']:.1f} dBFS | "
                         f"{r['hi_db']:.0f} dB | {e['desc']} |")
        L.append("")
    L += ["## Typing blips", "", "| Style | Speaker | Files | Length | Train LUFS | Peak (max) | Energy > 6 kHz |",
          "|---|---|---|---|---|---|---|"]
    for style, (_, desc) in STYLES.items():
        for spk in SPEAKERS:
            rs = [r for e, r in rows if e["path"].startswith(f"typing/{style}/{spk}_")]
            L.append(f"| {style} | {spk} | `typing/{style}/{spk}_01..06.wav` | {min(r['dur'] for r in rs):.0f}-"
                     f"{max(r['dur'] for r in rs):.0f} ms | {np.mean([r['lufs'] for r in rs]):.1f} | "
                     f"{max(r['peak'] for r in rs):.1f} dBFS | {max(r['hi_db'] for r in rs):.0f} dB |")
    L += ["", "Styles:", ""] + [f"- **{k}**: {d}" for k, (_, d) in STYLES.items()] + [""]
    with open(os.path.join(OUT_DIR, "manifest.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(L))


if __name__ == "__main__":
    main()
