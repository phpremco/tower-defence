"""Diagnostic (round 18): rides Gravemaw's spine nodes along the Climb_Up / Climb_Down / Burrow_Down paths frame by
frame (no rig) and prints, per spine bone, the largest per-frame turn of its direction (node a -> b) and of its up
vector, with the frame and the node positions there."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import build_gravemaw_mech as GM  # noqa: E402
import deep_kit as DK  # noqa: E402

BG = GM.BG


def check(name, path, travel, frames):
    prev = None
    worst = {}
    for f in range(frames + 1):
        pos, ups = DK.ride(path, BG.NODES, travel * f / frames)
        cur = {}
        for bone, a, b in BG.SPINE:
            d = (pos[b] - pos[a]).normalized()
            u = (ups[a] + ups[b]).normalized()
            cur[bone] = (d, u, pos[a], pos[b])
        if prev:
            for bone in cur:
                ad = math.degrees(prev[bone][0].angle(cur[bone][0], 0))
                au = math.degrees(prev[bone][1].angle(cur[bone][1], 0))
                if max(ad, au) > worst.get(bone, (0,))[0]:
                    worst[bone] = (max(ad, au), f, round(ad, 1), round(au, 1), tuple(round(x, 2) for x in cur[bone][2]))
        prev = cur
    for bone, w in worst.items():
        print(f"SPINE {name} {bone}: {w[0]:.1f} deg at frame {w[1]} (dir {w[2]}, up {w[3]}) node a {w[4]}")


up = GM.step_path(GM.EDGE, GM.CLIMB_H)
per = GM.CLIMB_STRIDE / BG.STANCE
cyc = math.ceil((GM.EDGE + 1.3 - GM.TAIL_F) / per)
check("climb_up", up, cyc * per, cyc * GM.CLIMB_CYCLE)
down = GM.step_path(GM.EDGE, -GM.CLIMB_H)
check("climb_down", down, cyc * per, cyc * GM.CLIMB_CYCLE)
for label, path, frames in (("burrow", GM.plunge_path(BG.DIVE_HOLE, 1.294, slope=55.0), 110),
                            ("plunge", GM.plunge_path(GM.EDGE, 1.844), 96),
                            ("rise", GM.rise_path(1.844, 3.4), 100)):
    check(label, path, GM.end_travel(path, 12.0), frames)
