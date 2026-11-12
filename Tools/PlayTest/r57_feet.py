# Round 57: how far a creature's planted feet slide in play, from the lines r57_feet.cs printed one frame apart.
# Usage: python -I r57_feet.py <lines file> [planted band, metres above the foot's lowest point; default 0.01]
# A foot counts as planted on a frame when its tip is within the band of its lowest height in the run (a walking creature
# keeps the ground level); its slide is the flat distance the tip moves on such frames, against the distance the body moves.
import math
import sys

path = sys.argv[1]
band = float(sys.argv[2]) if len(sys.argv) > 2 else 0.01
frames = []
for raw in open(path, encoding="utf-8"):
    parts = [p.strip() for p in raw.strip().split("|")]
    if len(parts) < 2:
        continue
    head = parts[0].split()
    if len(head) < 3:
        continue
    t, px, pz = (float(v) for v in head[:3])
    feet = {}
    for part in parts[1:]:
        if part.startswith("rig "):
            continue
        name, x, y, z = part.split()
        feet[name] = (float(x), float(y), float(z))
    frames.append((t, px, pz, feet))

if len(frames) < 3:
    print("too few frames", len(frames))
    sys.exit(0)

names = sorted(frames[0][3])
lowest = {n: min(f[3][n][1] for f in frames if n in f[3]) for n in names}
body = 0.0
planted_slide = 0.0
planted_steps = 0
lift = {n: 0.0 for n in names}
per_foot = {}
for a, b in zip(frames, frames[1:]):
    step = math.hypot(b[1] - a[1], b[2] - a[2])
    body += step
    for n in names:
        if n not in a[3] or n not in b[3]:
            continue
        ax, ay, az = a[3][n]
        bx, by, bz = b[3][n]
        lift[n] = max(lift[n], by - lowest[n])
        if ay - lowest[n] <= band and by - lowest[n] <= band:
            slide = math.hypot(bx - ax, bz - az)
            planted_slide += slide
            planted_steps += 1
            s, c, bs = per_foot.get(n, (0.0, 0, 0.0))
            per_foot[n] = (s + slide, c + 1, bs + step)

body_per_step = body / (len(frames) - 1)
print(f"frames {len(frames)} body moved {body:.3f} m ({body_per_step * 1000:.1f} mm/frame)")
if planted_steps:
    mean = planted_slide / planted_steps
    print(f"planted foot-frames {planted_steps}: mean slide {mean * 1000:.2f} mm/frame = {mean / max(body_per_step, 1e-9):.2f} of the body's step")
for n in names:
    s, c, bs = per_foot.get(n, (0.0, 0, 0.0))
    ratio = s / bs if bs > 0 else 0.0
    print(f"  {n}: planted {c} frames, slide {s * 1000:.1f} mm vs body {bs * 1000:.1f} mm ({ratio:.2f}), lift {lift[n] * 100:.1f} cm")
