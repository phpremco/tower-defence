"""Diagnostic (round 17): looks for tangent reversals (loops/cusps) in Gravemaw's mechanic paths."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import build_gravemaw_mech as GM  # noqa: E402

paths = {"climb_up": GM.step_path(GM.EDGE, GM.CLIMB_H), "climb_down": GM.step_path(GM.EDGE, -GM.CLIMB_H),
         "plunge": GM.plunge_path(GM.EDGE, 1.844), "burrow": GM.plunge_path(GM.BG.DIVE_HOLE, 1.294, slope=55.0),
         "rise": GM.rise_path(1.844, 3.4), "throw": GM.throw_path(1.5, 1.294), "throw_low": GM.throw_path(0.02, 1.294),
         "emerge": GM.BG.EMERGE_PATH, "dive": GM.BG.DIVE_PATH}
for name, p in paths.items():
    worst = 0.0
    where = None
    for i in range(1, len(p.f) - 2):
        a = math.atan2(p.z[i] - p.z[i - 1], p.f[i] - p.f[i - 1])
        b = math.atan2(p.z[i + 1] - p.z[i], p.f[i + 1] - p.f[i])
        d = abs((b - a + math.pi) % (2 * math.pi) - math.pi)
        if d > worst:
            worst, where = d, (round(float(p.f[i]), 3), round(float(p.z[i]), 3))
    back = sum(1 for i in range(len(p.f) - 1) if p.f[i + 1] < p.f[i] - 1e-6)
    print(f"PATH {name}: max turn between samples {math.degrees(worst):.1f} deg at {where}, backward steps {back}")

# Round 18: the spine nodes ride the path offset along its normal by their height; where a concave corner is tighter
# than that height the offset curve folds back (a swallowtail) and the bone between two nodes flips.
heights = [h for _, h in GM.BG.NODES]
for name, p in paths.items():
    folds = 0
    worst_u = None
    back = 0.0
    for h in heights:
        prev = None
        run = 0.0
        u = p.offset - 5.0
        while u < p.offset + p.total + 5.0:
            pf, pz, tf, tz = p.at(u)
            q = (pf - tz * h, pz + tf * h)
            step = (q[0] - prev[0]) * tf + (q[1] - prev[1]) * tz if prev is not None else 0.0
            run = run + step if step < 0 else 0.0
            back = min(back, run)
            if step < 0:
                folds += 1
                worst_u = worst_u or (round(pf, 2), round(pz, 2), h)
            prev = q
            u += 0.01
    print(f"FOLD {name}: {folds} backward offset steps, deepest fold {back:.3f} m, first at {worst_u}")
