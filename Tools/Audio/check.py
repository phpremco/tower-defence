"""Controle: duur, piek, luidheid, clipping en DC per bestand; optioneel spectrogrammen."""
import sys, os, glob
import numpy as np
import pyloudnorm as pyln
from scipy.io import wavfile

root = sys.argv[1] if len(sys.argv) > 1 else "out"
pattern = sys.argv[2] if len(sys.argv) > 2 else "**/*.wav"
meter = pyln.Meter(44100)
rows = []
for f in sorted(glob.glob(os.path.join(root, pattern), recursive=True)):
    sr, x = wavfile.read(f)
    x = x.astype(float) / 32767
    probe = x if len(x) >= 0.45 * sr else np.concatenate([x, np.zeros((int(0.45 * sr) - len(x),) + x.shape[1:])])
    try:
        L = meter.integrated_loudness(probe)
    except Exception:
        L = float("nan")
    pk = np.max(np.abs(x))
    clip = np.mean(np.abs(x) > 0.99)
    dc = np.mean(x)
    flag = []
    if pk > 0.95 or clip > 0: flag.append("CLIP")
    if abs(dc) > 0.01: flag.append("DC")
    if not np.isfinite(L) or L < -40: flag.append("QUIET")
    if len(x) / sr < 0.03: flag.append("SHORT")
    rows.append((os.path.relpath(f, root), len(x) / sr, pk, L, "ch" + str(1 if x.ndim == 1 else x.shape[1]), " ".join(flag)))
for r in rows:
    print(f"{r[0]:60s} {r[1]:6.2f}s pk {r[2]:.2f} {r[3]:6.1f} LUFS {r[4]} {r[5]}")
print(len(rows), "files")
