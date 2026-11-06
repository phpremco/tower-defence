"""Diagnostic (round 17): looks for tangent reversals (loops/cusps) in Gravemaw's mechanic paths."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import build_gravemaw_mech as GM  # noqa: E402

paths = {"climb_up": GM.step_path(GM.EDGE, GM.CLIMB_H), "climb_down": GM.step_path(GM.EDGE, -GM.CLIMB_H),
         "plunge": GM.plunge_path(GM.EDGE, 1.844), "burrow": GM.plunge_path(GM.BG.DIVE_HOLE, 1.294, slope=55.0),
         "rise": GM.rise_path(1.844, 3.4), "throw": GM.throw_path(1.5, 1.294)}
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
