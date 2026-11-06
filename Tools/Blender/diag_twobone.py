"""Diagnostic (round 18): keys Gravemaw's Gate_Mount spec on the saved rig and reports, for frames around the spade
flips, how close each two-bone solve came to its pole lying along the hip-target line (|w| before normalising)."""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_gravemaw as BG  # noqa: E402
import common as C  # noqa: E402
import gravemaw_gate as GG  # noqa: E402

log = []
orig = C.two_bone


def traced(root, target, l1, l2, pole):
    to = target - root
    u = to.normalized()
    w = Vector(pole) - u * Vector(pole).dot(u)
    d = to.length
    log.append((round(w.length, 3), round(d / (l1 + l2), 3)))
    return orig(root, target, l1, l2, pole)


C.two_bone = traced
boss = M.Boss(BG.NAME, list(BG.CLIPS), scale_bones=BG.DEBRIS_NAMES, lod_aims=BG.LOD_AIMS)
for out in [0.0]:
  pass
  for name, make in (("mount", GG.mount), ("straddle", GG.straddle), ("dismount", GG.dismount)):
    frames, spec, loop = make()[0](boss.poser)
    worst = (9.0, None)
    for f in range(frames + 1):
        log.clear()
        spec(f / frames, f)
        for w, reach in log[8:]:
            if w < worst[0]:
                worst = (w, f)
    print("TWOBONE", out, name, "smallest spade |w|", worst)
