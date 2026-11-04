"""Round 24b: sweep-ready shore banks, so pools can have rounded, organic outlines in Unity while gameplay stays on
the grid. Per continent bank style (Crystal Plains round 8, Ash/North/Mire/South/Core round 17) three segment
variants and one scatter accent:

  <Prefix>_BankSweep_A/B/C   1.0 unit long along X (x = -0.5..0.5), liquid on -Y, dry ground on +Y; the shore lip
                             line (shore curve) at y = 0, its top at z = 0.05 (same section as Bank_Straight); the
                             ends are the plain section, so any variant follows any other; vertices at least every
                             1/16 unit along X (bisected after the bake) so a sweep bends without kinks.
                             UV0 = baked atlas (unique per variant), UV1 "UVSweep" = (x + 0.5, y + 0.5) for tiling
                             detail along the run.
  <Prefix>_BankAccent        a few shore rocks / chips / plates to scatter on the curve and hide repeats.

Crystal Plains goes to Exports/Env/<Name>/ like round 8, the others to Exports/Env/<Continent>/<Name>/.
Then (unless --norender) renders the organic vs square pool test (bank_sweep_preview.py).

Run: blender --background --factory-startup --python Tools/Blender/build_bank_sweeps.py --
       [--only CrystalPlains,AshenReach] [--preview <dir>] [--norender] [--previewonly] [--compare-dir <dir>]
Reads (never edits) the continent scripts for their bank styles and accents.
"""

import importlib
import math
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import env_kit as E  # noqa: E402
import env_kit2 as K  # noqa: E402

SEGMENT = 1.0
SLICES = 16          # minimum vertex rows along X per segment
LIP_Z = 0.05
TRIS = 900
ACCENT_TRIS = 400


# ---------------------------------------------------------------------------------------------- Crystal Plains

def rot_z(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


def tilt(rng, yaw=6, roll=5):
    return (rot_z(rng.uniform(-yaw, yaw)) @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "X")
            @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "Y"))


def cp_bank_style(seed):
    """Round 8 bank style (build_env_kit.bank_style, copied: that script runs on import): wet basalt at the
    waterline, darker and glossier low down, a crust line where the liquid rises and falls."""
    base = E.stone(E.BASALT, seed, crack=0.4, dust=0.3)

    def style(s, idx, part):
        base(s, idx, part)
        z = s.p[idx][:, 2]
        wet = 1 - C.smoothstep(-0.02, 0.05, z)
        line = 1 - C.smoothstep(0.0, 0.012, np.abs(z - 0.045))
        s.albedo[idx] = s.albedo[idx] * (1 - 0.45 * wet)[:, None]
        s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.55, 0.52, 0.6)), line * 0.6)
        s.rough[idx] = s.rough[idx] - 0.45 * wet
    return style


def cp_segment(seed, variant):
    """Round 8 look: a row of rounded wet stones on a crust shelf. Stones are spaced so the row continues across
    the segment joint (the end stones overhang it by a few cm)."""
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    count = (4, 5, 3)[variant]
    span = SEGMENT / count
    for i in range(count):
        cx = -0.5 + (i + 0.5) * span + rng.uniform(-0.03, 0.03)
        b.ellipsoid("Stone", Vector((cx, -0.04 + rng.uniform(-0.035, 0.035), -0.03)),
                    (span * rng.uniform(0.55, 0.68), rng.uniform(0.09, 0.14), rng.uniform(0.06, 0.1)), tilt(rng, 25, 8),
                    style=cp_bank_style(seed + i), segments=(24, 12), deform=C.chiselled(12, seed + i, 0.2))
    for i in range(variant):
        x = rng.uniform(-0.35, 0.35)
        K.chip(b, (x, rng.uniform(-0.16, -0.1), -0.02), (0.05, 0.04, 0.035), seed + 30 + i, cp_bank_style(seed + 30 + i))

    def out(t, s):
        return -0.12 + 0.3 * s, -0.16 + 0.17 * C.smoothstep(0.0, 1.0, np.array(s)).item()

    def inner(t, s):
        return -0.12 + 0.3 * s, -0.26

    E.sweep(b, "Shelf", E.line_path(-0.5, 0.5, 0.0, (0.0, 1.0)), out, inner, 48, 12, cp_bank_style(seed + 20),
            lambda t, s, d, z: (0.03 * E.periodic(-0.5 + t, 1.0, 3, seed + 21) * K.window(t, 0.06), 0.0))
    return b


def cp_accent(seed):
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    for i in range(3):
        c = Vector((rng.uniform(-0.12, 0.12), rng.uniform(-0.1, 0.1), -0.02))
        b.ellipsoid("Stone", c, (rng.uniform(0.06, 0.1), rng.uniform(0.05, 0.08), rng.uniform(0.04, 0.07)),
                    tilt(rng, 40, 12), style=cp_bank_style(seed + i), segments=(20, 10),
                    deform=C.chiselled(10, seed + i, 0.22))
    return b


# ----------------------------------------------------------------------------------------- round-17 continents

def r17_segment(mod, style_name, accent_name, seed, variant):
    style = getattr(mod, style_name)
    accent = getattr(mod, accent_name) if accent_name else None
    b = C.PartBuilder()
    K.bank_slab(b, "Shelf", K.straight_path(0.5), SEGMENT, style(seed), seed, nu=64)
    rng = np.random.default_rng(seed + 50)
    if accent and variant != 2:
        accent(b, rng, "straight")
    # Variant B/C: loose chips on the lip and in the shallows, in different places, so repeats do not line up.
    for i in range((0, 2, 4)[variant]):
        x = rng.uniform(-0.4, 0.4)
        size = rng.uniform(0.03, 0.055)
        K.chip(b, (x, rng.uniform(-0.12, 0.06), rng.uniform(-0.02, 0.03)), (size * 1.3, size, size * 0.8), seed + 70 + i,
               style(seed + 70 + i))
    return b


def r17_accent(mod, style_name, seed):
    style = getattr(mod, style_name)
    b = C.PartBuilder()
    rng = np.random.default_rng(seed)
    for i in range(3):
        size = rng.uniform(0.04, 0.075)
        K.chip(b, (rng.uniform(-0.12, 0.12), rng.uniform(-0.1, 0.1), 0.0), (size * 1.4, size, size * 0.9), seed + i,
               style(seed + i), depth=0.28)
    return b


STYLES = {
    # name: (module, style fn, accent fn, base seed, families, folder key, prefix)
    "CrystalPlains": (None, None, None, 12410, None, None, ""),
    "AshenReach": ("build_env_ashenreach", "lava_bank_style", "bank_accent", 22410, None, "AshenReach", "Ash_"),
    "NorthernCrown": ("build_env_northerncrown", "ice_style", "bank_accent", 33410, ("crystal", "rock"), "NorthernCrown",
                      "North_"),
    "Mirewood": ("build_env_mirewood", "peat_style", "bank_accent", 55410, None, "Mirewood", "Mire_"),
    "SouthernCrown": ("build_env_southerncrown", "pack_style", "bank_accent", 77410, ("crystal", "rock"),
                      "SouthernCrown", "South_"),
    "TheCore": ("build_env_core", "pool_style", None, 99410, None, "TheCore", "Core_"),
}


# --------------------------------------------------------------------------------------------- post-processing

def bisect_along_x(obj, slices=SLICES, half=SEGMENT / 2):
    """Cuts the game mesh at x = k / slices so every segment has vertex rows to bend on (decimation leaves long
    triangles). UVs and the baked maps stay valid: the surface does not move."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    xs = [v.co.x for v in bm.verts]
    lo, hi = min(xs), max(xs)
    for k in range(int(math.floor(lo * slices)), int(math.ceil(hi * slices)) + 1):
        x = k / slices
        if lo + 1e-4 < x < hi - 1e-4:
            geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
            bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(x, 0, 0), plane_no=(1, 0, 0))
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def add_sweep_uv(obj):
    mesh = obj.data
    uv = mesh.uv_layers.get("UVSweep") or mesh.uv_layers.new(name="UVSweep")
    co = np.zeros(len(mesh.vertices) * 3, np.float32)
    mesh.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    loops = np.zeros(len(mesh.loops), np.int32)
    mesh.loops.foreach_get("vertex_index", loops)
    data = np.stack([co[loops, 0] + 0.5, co[loops, 1] + 0.5], axis=1).astype(np.float32)
    uv.data.foreach_set("uv", data.ravel())
    mesh.uv_layers.active_index = 0


def sweep_report(asset, variant):
    co = np.array([v.co[:] for v in asset.low.data.vertices])
    xs = np.unique(np.round(co[:, 0] * SLICES) / SLICES)
    return {"sweep": {"variant": "ABC"[variant], "segment_length": SEGMENT, "axis": "+X (x = -0.5..0.5)",
                      "liquid_side": "-Y", "shore_line": "y = 0", "lip_top_z": LIP_Z,
                      "extent_y": [round(float(co[:, 1].min()), 3), round(float(co[:, 1].max()), 3)],
                      "extent_z": [round(float(co[:, 2].min()), 3), round(float(co[:, 2].max()), 3)],
                      "min_bend_radius": 0.5, "rows_along_x": int(len(xs)),
                      "uv": "UV0 baked atlas, UV1 'UVSweep' = (x + 0.5, y + 0.5)",
                      "game_tris_after_bisect": C.tri_count(asset.low)}}


def build_style(key, only_assets=None):
    mod_name, style_name, accent_name, seed, families, folder, prefix = STYLES[key]
    mod = importlib.import_module(mod_name) if mod_name else None
    cont = mod.CONT if mod else None
    previous = C.EXPORTS
    if cont:
        cont.activate()
    else:
        E.use_env_exports()
    fams = families or ("rock", "crystal")
    built = []
    try:
        jobs = [(f"{prefix}BankSweep_{'ABC'[v]}", v) for v in range(3)] + [(f"{prefix}BankAccent", None)]
        for name, variant in jobs:
            if only_assets and name not in only_assets:
                continue
            t0 = time.time()
            if variant is None:
                builder = r17_accent(mod, style_name, seed + 900) if mod else cp_accent(seed + 900)
                tris = ACCENT_TRIS
            else:
                s = seed + variant * 100
                builder = r17_segment(mod, style_name, accent_name, s, variant) if mod else cp_segment(s, variant)
                tris = TRIS
            asset = E.build(name, builder, tris, voxel=0.008, smoothing=2, families=fams, emission=2.2)
            extra = {"round": "24b"}
            if variant is not None:
                bisect_along_x(asset.low)
                add_sweep_uv(asset.low)
                extra.update(sweep_report(asset, variant))
            else:
                extra["accent"] = "scatter on the bank curve, any yaw, pivot on the ground; scale 0.8-1.2"
            if cont:
                K.finish(cont, asset, extra)
            else:
                E.finish(asset, extra)
            built.append(name)
            C.log("bank sweep", name, f"{time.time() - t0:.0f}s")
    finally:
        C.EXPORTS = previous
    return built


def main():
    args = C.cli_args()
    keys = [k for k in STYLES if not args.get("only") or k in args["only"].split(",")]
    if not args.get("previewonly"):
        for key in keys:
            C.reset_scene()
            build_style(key)
    if not args.get("norender"):
        import bank_sweep_preview as P
        P.run(args.get("preview"), args.get("compare-dir"), only=args.get("only"))


if __name__ == "__main__":
    main()
