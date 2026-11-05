"""The Shattered, round 28 improvement pass (see ArtSource/DESIGN_NOTES.md round 28). Replaces the round-16 export
of build_shattered.py (run after it; build_shattered_mech.py runs after this one).

Review finding: the round-16 brute read as a brown gorilla with Gravemaw's plum-bronze armour, a small low head and
no weapon, and nothing on it told the player it would split into three. This pass keeps the rig and the round-16
clips (Walk/Idle/Hit/PhaseBreak1/PhaseBreak2/Death, phase table by bone scale) and changes what is read at play size:

- palette: pale shattered-ice granite plates over a black glass under-layer (the Southern Crown's pale ice over dark
  rock), magenta-white overcharge light; no longer Gravemaw's plum-bronze;
- silhouette: a tall crown of crystal blades on the head (the head now reads above the hump), long crystal cleavers
  grown out of both fists (its weapon), and three big crystal spires on the back (shoulders, hump, pelvis), each in
  the silhouette of one of the three shards it splits into (ShardRunner, ShardFlyer, ShardCrawler);
- the three spire bones `Core_Road`, `Core_Air`, `Core_Land` (scale) let Unity show which shards died before the
  rejoin (a dead shard's spire at 0.35).

Run: blender --background --factory-startup --python Tools/Blender/build_shattered2.py -- [--preview <dir>]
     [--norender] [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import build_shattered as BS  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = BS.NAME
LOD_NAME = BS.LOD_NAME
K = BS.K

# Pale shattered-ice granite over black glass; the overcharge light stays the continent's magenta-white.
BS.PAL = H.Palette(top=(0.64, 0.66, 0.72), side=(0.29, 0.3, 0.36), belly=(0.06, 0.06, 0.08), seam=(0.03, 0.025, 0.04),
                   dark=(0.1, 0.1, 0.13), glow=(1.0, 0.42, 0.78), eye=(1.0, 0.86, 0.96), membrane=(0.42, 0.1, 0.3),
                   bone=(0.93, 0.93, 0.97))
BS.CORE = H.Palette(top=(0.14, 0.06, 0.12), side=(0.07, 0.03, 0.07), belly=(0.04, 0.015, 0.035), seam=(0.03, 0.01, 0.03),
                    dark=BS.PAL.dark, glow=(1.0, 0.8, 0.96), eye=BS.PAL.eye, membrane=(0.6, 0.12, 0.4), bone=BS.PAL.bone)
Y.PALETTES["overcharge_w"] = ((np.array((0.3, 0.04, 0.3)), np.array((1.0, 0.5, 0.86)), np.array((1.0, 0.97, 1.0))),
                              (np.array((1.0, 0.6, 0.9)), np.array((1.0, 0.97, 1.0))))

# The three shard spires: (bone, parent, base, [(direction, length, radius)]) - runner = a forward-raked spear
# fan (shoulders), flyer = two swept wing blades (hump), crawler = a low ring of hooked claws (pelvis).
CORES = [("Core_Road", "Chest", G(0, 2.55, 0.35), [((0, 1.0, 0.55), 1.05, 0.12), ((0.35, 0.95, 0.4), 0.8, 0.09),
                                                    ((-0.35, 0.95, 0.4), 0.8, 0.09)]),
         ("Core_Air", "Chest", G(0, 2.75, -0.35), [((0.9, 0.55, -0.45), 1.2, 0.11), ((-0.9, 0.55, -0.45), 1.2, 0.11),
                                                   ((0.0, 1.0, -0.2), 0.75, 0.1)]),
         ("Core_Land", "Pelvis", G(0, 1.95, -1.35), [((0.6, 0.8, -0.2), 0.65, 0.09), ((-0.6, 0.8, -0.2), 0.65, 0.09),
                                                     ((0.0, 0.85, -0.6), 0.7, 0.1), ((0.0, 1.0, 0.2), 0.55, 0.08)])]
CORE_NAMES = [c[0] for c in CORES]


def crystal_parts():
    b = BS.crystal_parts()
    # Crown of crystal blades on the head, raked back so the head stands proud of the hump.
    hc = BS.HEAD_C
    for j, (x, d, l, r) in enumerate(((0.0, (0, 1.0, 0.1), 1.7, 0.14), (0.2, (0.4, 1.0, 0.0), 1.35, 0.11),
                                      (-0.2, (-0.4, 1.0, 0.0), 1.35, 0.11), (0.36, (0.85, 0.75, 0.15), 0.9, 0.08),
                                      (-0.36, (-0.85, 0.75, 0.15), 0.9, 0.08))):
        b.shard("Crest", hc + G(x, 0.2, 0.05), G(*d).normalized(), r, l, sides=7, flute=0.45, twist=0.8 * (1 if x >= 0 else -1),
                bone="Head", style=Y.shard_style("overcharge_w", 1.0, seed=80 + j))
    # Cleavers grown out of the fists: one long blade forward and down past the knuckles, a short spur behind.
    for (side, i), lg in BS.arms().items():
        sx = -1 if side == "L" else 1
        sh, el, wr = lg.rest_points
        base = wr + Vector((0, 0, 0.32))
        b.shard("Blade", base + G(0.08 * sx, 0.0, 0.1), G(0.12 * sx, -0.25, 1.0).normalized(), 0.12, 1.25, sides=6,
                flute=0.25, twist=0.3 * sx, bone=lg.names[1], style=Y.shard_style("overcharge_w", 1.0, seed=90))
        b.shard("Blade", base + G(0.1 * sx, 0.15, -0.1), G(0.5 * sx, 0.5, -0.7).normalized(), 0.07, 0.55, sides=6,
                flute=0.25, bone=lg.names[1], style=Y.shard_style("overcharge_w", 0.9, seed=91))
    # The three shard spires.
    for name, parent, base, shards in CORES:
        for j, (d, length, radius) in enumerate(shards):
            b.shard("Spire", base, G(*d).normalized(), radius, length, sides=7, flute=0.5, twist=0.9,
                    bone=name, style=Y.shard_style("overcharge_w", 1.0, seed=100 + j))
    return b


def bones():
    out = BS.bones()
    for name, parent, base, shards in CORES:
        out.append((name, base - G(0, 0.15, 0), base + G(0, 0.25, 0), parent, True))
    return out


def main():
    args = C.cli_args()
    out = C.preview_dir()
    scale_bones = BS.SCALE_BONES + CORE_NAMES
    rigid = BS.CRYSTAL_RIGID | {"Crest", "Blade", "Spire"}
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("shattered2", [(BS.body_parts(), (0.6, 0.62, 0.68), None), (crystal_parts(), (1.0, 0.5, 0.85), (1.0, 0.5, 0.85))],
                   bones(), BS.CLIPS, out, (0, 0, 1.5), 13.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                    ("p3", "close", "PhaseBreak2", BS.BREAK2_FRAMES)], [], scale_bones=scale_bones,
                   rigid=BS.BODY_RIGID | rigid)
        return
    C.reset_scene()
    mats = [T.Mat(BS.body_parts(), 17500, ("chitin", "membrane", "rock"), voxel=0.012, ao=0.35, k=K, emission=2.6,
                  specular=0.3, extra=H.weathering(K, scratches=0.6, dust=0.35, seed=191, markings=BS.PAL.dark,
                                                   marking_amount=0.2), rigid=BS.BODY_RIGID, blend=0, lod_share=0.7,
                  islands=BS.SHELL_NAMES),
            T.Mat(crystal_parts(), 6500, ("crystal", "rock"), suffix="Crystal", voxel=0.009, ao=0.2, extrusion=0.04,
                  ray=0.12, k=K * 0.6, emission=2.8, specular=0.45, rigid=rigid, blend=0, smooth=2, lod_share=0.3)]
    report = {"walk_distance_per_loop": round(BS.STRIDE / BS.STANCE, 3), "pivot": "ground, under the chest",
              "phases": {str(p): {"shells": v[0], "crystals": v[1]} for p, v in BS.PHASE.items()},
              "phase_break_1": {"burst_frame": BS.BREAK1_BURST, "shells_off": BS.SHELL_NAMES[:4], "frames": BS.BREAK1_FRAMES},
              "phase_break_2": {"burst_frame": BS.BREAK2_BURST, "shells_off": BS.SHELL_NAMES[4:], "frames": BS.BREAK2_FRAMES},
              "death": {"explosion_frame": BS.DEATH_BLAST, "crystals_gone_frame": BS.DEATH_BLAST + 8},
              "emission_per_phase": {"1": 2.6, "2": 3.4, "3": 4.5},
              "round28": {"pass": "improvement (palette, crown, cleavers, three shard spires)",
                          "shard_spires": {"Core_Road": "ShardRunner", "Core_Air": "ShardFlyer", "Core_Land": "ShardCrawler"}}}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), BS.CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=BS.LOD_DROP, scale_bones=scale_bones,
        clip_scales={c: BS.SCALE_BONES for c in BS.CLIPS}, report=report, unity_scale=BS.UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("shattered2", arm, mesh, actions, lod, out, assets, (0, 0, 1.6), 13.5, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40)],
               strips=[], macros=[("head", G(0, 1.6, 1.5), 3.2, -30, 12, "Idle", 20)])


if __name__ == "__main__":
    main()
