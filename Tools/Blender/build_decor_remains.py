"""Round 38: what is left of a decoration piece a boss walks through (Docs/BOSSES.md round 13b).

A continent boss off the road smashes the small decor it overlaps (crystals, flora, small rocks). The piece is swapped
for one of these remains, which stay broken for the rest of the level. They are low (they never block a later walk),
about a metre across at scale 1 (Unity scales them to the footprint of the piece they replace) and share the palettes of
the round-2/8 decor so a smashed crystal still reads as the same crystal:

- Remains_Crystal: a basalt socket with snapped Crysium stumps (blunt, fractured tops) and shards strewn flat around it.
- Remains_Flora: a torn bulb with broken stalks and limp blades flattened on the ground, the lit tips dimmed.
- Remains_Stone: a low spill of chiselled rubble, the veins showing on the fresh faces.

Exports ArtSource/Exports/Env/Remains_* (same four maps and report as the round-8 decor).

Run: blender --background --factory-startup --python Tools/Blender/build_decor_remains.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import env_kit as E  # noqa: E402

PLUM = np.array((0.36, 0.13, 0.34))
PLUM_DARK = np.array((0.07, 0.025, 0.08))
LIME = np.array((0.72, 1.0, 0.3))

# Half the width the remains span at scale 1: Unity fits this to the broken piece's footprint.
SPAN = 0.5


def rz(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


def torn_flesh(seed, glow=LIME):
    """Bruised plum flesh, the light only a faint glimmer at the torn ends."""

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        if part.axis is not None:
            t, _, _ = C.tube_param(part, p)
        else:
            t = C.smoothstep(0.0, 0.2, p[:, 2])
        col = C.lerp3(PLUM_DARK, PLUM, C.smoothstep(0.0, 0.6, t))
        col = col * (0.8 + 0.25 * C.value_noise(p, 24, seed))[:, None]
        bruise = C.smoothstep(0.2, 0.7, C.value_noise(p, 9, seed + 1))
        col = C.lerp3(col, PLUM_DARK * 0.8, bruise * 0.5)
        torn = C.smoothstep(0.8, 1.0, t) * 0.35
        s.albedo[idx] = C.lerp3(col, glow * 0.5, torn)
        s.emit[idx] = np.asarray(glow)[None, :] * torn[:, None]
        s.disp[idx] = 0.002 * C.value_noise(p, 30, seed + 2)
        s.rough[idx] = 0.6 + 0.0 * n[:, 2]
        s.fam[idx] = E.FAM_MEMBRANE
        s.flow[idx] = t * 0.3
    return style


def socket(b, seed, radius, height):
    b.ellipsoid("Socket", Vector((0, 0, height * 0.3)), (radius, radius * 0.85, height),
                style=E.stone(E.BASALT, seed, veins=0.6, dust=0.6), segments=(28, 12), deform=C.chiselled(12, seed, 0.22))


def remains_crystal():
    b = C.PartBuilder()
    rng = np.random.default_rng(3801)
    socket(b, 3802, 0.22, 0.07)
    # Snapped stumps: short and thick, so the fracture face, not a point, is what shows.
    for i in range(5):
        a = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3)
        r = rng.uniform(0.02, 0.12)
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0.04))
        lean = Vector((math.cos(a) * rng.uniform(0.2, 0.6), math.sin(a) * rng.uniform(0.2, 0.6), 1))
        b.shard("Stump", base, lean, rng.uniform(0.035, 0.055), rng.uniform(0.1, 0.17), sides=6, twist=0.9, flute=0.5,
                profile=lambda z: 1.0 - 0.45 * z, style=Y.shard_style("pink", 0.3, seed=3810 + i))
    # The broken-off tops, strewn flat round the socket.
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.2, SPAN * 0.95)
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0.012))
        d = Vector((math.cos(a + rng.uniform(-1, 1)), math.sin(a + rng.uniform(-1, 1)), rng.uniform(0.05, 0.25)))
        b.shard("Shard", base, d, rng.uniform(0.018, 0.035), rng.uniform(0.07, 0.16), sides=6, twist=0.8, flute=0.3,
                style=Y.shard_style("pink" if i % 3 else "ice", 0.45, seed=3830 + i))
    return b


def remains_flora():
    b = C.PartBuilder()
    rng = np.random.default_rng(3851)
    b.ellipsoid("Bulb", Vector((0, 0, 0.03)), (0.17, 0.15, 0.07), style=torn_flesh(3852), segments=(24, 12),
                deform=C.chiselled(10, 3852, 0.25, squash=(0.7, 1.0)))
    for i in range(4):
        a = i * math.pi / 2 + rng.uniform(-0.4, 0.4)
        base = Vector((math.cos(a) * 0.06, math.sin(a) * 0.06, 0.05))
        d = Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, 1))
        b.shard("Stalk", base, d, rng.uniform(0.022, 0.032), rng.uniform(0.06, 0.12), sides=8, twist=0.0, flute=0.1,
                profile=lambda z: 1.0 - 0.1 * z, style=torn_flesh(3860 + i))
    # Limp blades lying flat, pointing out from where they fell.
    for i in range(6):
        a = i * math.pi / 3 + rng.uniform(-0.3, 0.3)
        r = rng.uniform(0.24, SPAN * 0.8)
        c = Vector((math.cos(a) * r, math.sin(a) * r, 0.01))
        b.ellipsoid("Blade", c, (rng.uniform(0.12, 0.2), rng.uniform(0.035, 0.05), 0.012),
                    rz(math.degrees(a) + rng.uniform(-20, 20)), style=torn_flesh(3870 + i), segments=(18, 6),
                    deform=C.chiselled(5, 3870 + i, 0.3))
    return b


def remains_stone():
    b = C.PartBuilder()
    rng = np.random.default_rng(3901)
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.0, SPAN * 0.8) if i else 0.0
        size = rng.uniform(0.06, 0.12) * (1.5 if i == 0 else 1.0)
        c = Vector((math.cos(a) * r, math.sin(a) * r, size * 0.35))
        b.ellipsoid("Rubble", c, (size, size * rng.uniform(0.7, 1.0), size * rng.uniform(0.5, 0.8)), rz(rng.uniform(0, 360)),
                    style=E.stone(E.BASALT, 3910 + i, veins=0.7, dust=0.4), segments=(16, 8),
                    deform=C.chiselled(8, 3910 + i, 0.3))
    return b


ASSETS = {
    "Remains_Crystal": (remains_crystal, 900, 0.006, 1, 30, ("rock", "crystal")),
    "Remains_Flora": (remains_flora, 700, 0.006, 1, None, ("membrane", "chitin")),
    "Remains_Stone": (remains_stone, 700, 0.006, 1, 30, ("rock", "crystal")),
}


def previews(built, out):
    C.preview_world((0.02, 0.02, 0.035))
    C.ground(color=(0.05, 0.05, 0.07))
    rig = C.light_rig(scale=1.5)
    paths = []
    for name, asset in built.items():
        for other in built.values():
            other.low.hide_render = other is not asset
        cam = C.closeup_camera(Vector((0, 0, 0.05)), 1.5, azimuth=-30, elevation=40, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"env_{name.lower()}.png"), 640, 480))
        bpy.data.objects.remove(cam)
    if len(paths) > 1:
        C.contact_sheet(paths, os.path.join(out, "sheet_decor_remains.png"), 3, 400)
    C.hide_lights(rig, True)


def main():
    C.reset_scene()
    E.use_env_exports()
    out = C.preview_dir()
    built = {}
    for name, (fn, tris, voxel, smoothing, sharp, families) in ASSETS.items():
        asset = E.build(name, fn(), tris, voxel=voxel, smoothing=smoothing, sharp=sharp, families=families, emission=2.2)
        E.finish(asset, {"round": 38, "role": "remains of a smashed decor piece (Docs/BOSSES.md round 13b)",
                         "span_half": SPAN})
        built[name] = asset
    C.save_blend("Env_Decor_Remains")
    if not C.cli_args().get("norender"):
        previews(built, out)


main()
