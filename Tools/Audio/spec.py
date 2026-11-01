"""Contactvel met spectrogram + golfvorm per bestand (om te controleren zonder te luisteren)."""
import sys, glob, os
import numpy as np
from scipy.io import wavfile
from scipy.signal import stft
from PIL import Image, ImageDraw

files = []
for p in sys.argv[2:]:
    files += sorted(glob.glob(os.path.join("out", p), recursive=True))
out = sys.argv[1]
W, H = 420, 150
cols = 3
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGB", (W * cols, (H + 18) * rows), (20, 20, 20))
d = ImageDraw.Draw(sheet)
for i, f in enumerate(files):
    sr, x = wavfile.read(f)
    x = x.astype(float) / 32767
    if x.ndim == 2:
        x = x.mean(1)
    _, _, Z = stft(x, sr, nperseg=1024, noverlap=768)
    S = 20 * np.log10(np.abs(Z) + 1e-7)
    # log-frequentie as: 30 Hz .. 20 kHz
    freqs = np.linspace(0, sr / 2, S.shape[0])
    target = np.geomspace(30, 20000, H - 40)
    idx = np.clip(np.searchsorted(freqs, target), 0, S.shape[0] - 1)
    S = S[idx][::-1]
    S = np.clip((S + 90) / 80, 0, 1)
    img = np.zeros((H - 40, S.shape[1], 3))
    img[..., 0] = np.clip(S * 2, 0, 1)
    img[..., 1] = np.clip(S * 2 - 0.8, 0, 1)
    img[..., 2] = np.clip(S * 3 - 2.2, 0, 1) + 0.15 * (1 - S)
    im = Image.fromarray((img * 255).astype(np.uint8)).resize((W - 4, H - 40))
    r, c = divmod(i, cols)
    ox, oy = c * W, r * (H + 18)
    sheet.paste(im, (ox + 2, oy + 18))
    # golfvorm-envelope
    env = np.abs(x)
    bins = np.array_split(env, W - 4)
    pk = np.array([b.max() if len(b) else 0 for b in bins])
    for k, v in enumerate(pk):
        h = int(v * 36)
        d.line([(ox + 2 + k, oy + H - 2), (ox + 2 + k, oy + H - 2 - h)], fill=(120, 200, 255))
    name = os.path.relpath(f, "out")
    d.text((ox + 4, oy + 3), f"{os.path.basename(name)}  {len(x)/sr:.2f}s", fill=(230, 230, 230))
sheet.save(out)
print(out, len(files))
