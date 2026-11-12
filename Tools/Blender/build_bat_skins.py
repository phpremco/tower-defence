"""Bat element skins Tide and Frost with a WARM glow (round 14, see ArtSource/DESIGN_NOTES.md), and (round 56) the bog
skin Mire for the Mirebat (`--element Mire`).

build_bat.py keeps the data colours for Tide and Frost (cool glow). The alien rule says the glow stays warm, so this
wrapper passes its own palettes without editing build_bat.py: the water bat keeps a deep sea-teal body with a coral
glow, the ice bat a pale frost-white body with an ember glow. The element still reads from the body colour; the
light says "alien".

How it stays on the shipped mesh: the default Ember asset is rebuilt in this process (the build is deterministic)
into a scratch folder, so Exports/Bat/Bat.fbx and its maps are never touched; the rebuilt game mesh is compared with
the exported Bat.fbx (vertex count, triangle count and UVs) and the run stops if they differ. Only then are the
skins baked into Exports/Bat/Skins/<Element>/ (same file layout as build_bat.py --element).

Run: blender --background --factory-startup --python Tools/Blender/build_bat_skins.py -- [--preview <dir>]
     [--element Tide,Frost] [--norender] [--onto-shipped]
(--onto-shipped, round 56: bake onto the shipped body mesh in Bat.blend instead of stopping on an unwrap mismatch.)
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402

import build_bat as BB  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402

# Warm-glow palettes (the cool body says the element, the warm light says alien).
WARM = {
    "Tide": BB.skin(top=(0.13, 0.33, 0.37), side=(0.04, 0.12, 0.14), belly=(0.015, 0.03, 0.035), glow=(1.0, 0.42, 0.26),
                    eye=(1.0, 0.86, 0.6), membrane=(0.08, 0.24, 0.27), under=(0.36, 0.5, 0.5), flush=(0.62, 0.22, 0.16),
                    bone=(0.86, 0.84, 0.74)),
    "Frost": BB.skin(top=(0.74, 0.8, 0.86), side=(0.3, 0.34, 0.4), belly=(0.07, 0.07, 0.09), glow=(1.0, 0.44, 0.1),
                     eye=(1.0, 0.8, 0.42), membrane=(0.52, 0.58, 0.64), under=(0.8, 0.82, 0.86), flush=(0.75, 0.32, 0.16),
                     bone=(0.95, 0.94, 0.9)),
}

# Round 56: the bog bat (Mirebat) wore the Moss skin. Its own: murky green-brown hide and membrane, yellowed bone and a
# pale, sickly yellow-green glow (Unity tints its parts' glow about (0.85, 1, 0.6)).
BOG = {
    "Mire": BB.skin(top=(0.22, 0.21, 0.1), side=(0.085, 0.075, 0.035), belly=(0.03, 0.028, 0.018), glow=(0.78, 0.9, 0.5),
                    eye=(0.92, 0.98, 0.62), membrane=(0.2, 0.17, 0.07), under=(0.33, 0.31, 0.18), flush=(0.3, 0.27, 0.1),
                    bone=(0.78, 0.76, 0.6)),
}
PALETTES = {**WARM, **BOG}

REAL_EXPORTS = C.EXPORTS


def mesh_signature(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    uv = np.empty(len(mesh.loops) * 2)
    mesh.uv_layers.active.data.foreach_get("uv", uv)
    co = np.empty(len(mesh.vertices) * 3)
    mesh.vertices.foreach_get("co", co)
    return len(mesh.vertices), len(mesh.loop_triangles), np.sort(np.round(uv, 4)), np.sort(np.round(co, 3))


def check_against_export(low):
    """Stops the run when the rebuilt game mesh is not the shipped Bat mesh (the skins must share its UVs)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(REAL_EXPORTS, "Bat", "Bat.fbx"))
    new = [o for o in bpy.data.objects if o not in before]
    # Round 46: Bat.fbx also carries the detail parts (Bat_Face, Bat_Claws); the body is the other mesh.
    shipped = next(o for o in new if o.type == "MESH" and not o.name.startswith(("Bat_Face", "Bat_Claws")))
    a, b = mesh_signature(low), mesh_signature(shipped)
    same_counts = a[0] == b[0] and a[1] == b[1]
    uv_diff = float(np.abs(a[2] - b[2]).max()) if same_counts and len(a[2]) == len(b[2]) else 1.0
    for o in new:
        bpy.data.objects.remove(o)
    C.log("bat mesh check", "verts", a[0], b[0], "tris", a[1], b[1], "max uv diff", uv_diff)
    if not same_counts or uv_diff > 2e-3:
        raise RuntimeError("rebuilt Bat mesh differs from Exports/Bat/Bat.fbx; rebuild build_bat.py first")
    return uv_diff


def shipped_target(asset):
    """Round 56 (--onto-shipped): the shipped game mesh as the bake target. The rebuilt mesh has the shipped geometry
    (same vertices and triangles) but its unwrap no longer lands on the shipped UVs (max 0.095 since the round-49C
    redesign), so the skin is baked onto the body mesh saved in ArtSource/Blend/Bat.blend (the one in Bat.fbx), with
    its own UVs and its own mirror half (faces at x >= 0, as uvlayout.unwrap bakes). Stops if the geometry differs."""
    import types
    from mathutils import Vector  # noqa: F401
    with bpy.data.libraries.load(os.path.join(C.BLEND, BB.NAME + ".blend")) as (src, dst):
        dst.objects = [BB.NAME]
    mesh = dst.objects[0].data
    low = C.link(bpy.data.objects.new(BB.NAME + "_Shipped", mesh))
    low.data.materials.clear()
    a, b = mesh_signature(asset.low), mesh_signature(low)
    co_diff = float(np.abs(a[3] - b[3]).max()) if a[0] == b[0] and len(a[3]) == len(b[3]) else 1.0
    C.log("bat shipped mesh check", "verts", a[0], b[0], "tris", a[1], b[1], "max co diff", co_diff)
    if a[0] != b[0] or a[1] != b[1] or co_diff > 0.01:  # projection bake: a few mm are inside its ray
        raise RuntimeError("the rebuilt Bat geometry differs from the shipped mesh in Bat.blend")
    faces = [f.index for f in mesh.polygons if f.center.x >= -1e-5]
    return types.SimpleNamespace(settings=asset.settings, low=low, bake_faces=faces), co_diff


def main():
    C.reset_scene()
    out = C.preview_dir()
    names = [e for e in (C.cli_args().get("element") or "Tide,Frost").split(",") if e]
    scratch = os.path.join(out, "_bat_base")
    os.makedirs(scratch, exist_ok=True)
    C.EXPORTS = scratch
    builder = BB.build_parts(BB.ELEMENTS[BB.DEFAULT_ELEMENT])
    asset = BB.build_asset(BB.NAME, builder, BB.GAME_TRIS)
    C.EXPORTS = REAL_EXPORTS
    target, check_key = asset, "mesh_check_max_uv_diff"
    if C.cli_args().get("onto-shipped"):
        target, uv_diff = shipped_target(asset)
        check_key = "round56_baked_onto_shipped_mesh_max_co_diff"
    else:
        uv_diff = check_against_export(asset.low)
    arm = C.build_armature(BB.NAME + "_Rig", BB.bones())
    C.skin(asset.low, arm, builder, rigid=BB.RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, BB.CLIPS)
    skins = {}
    for el in names:
        sk_builder = BB.build_parts(PALETTES[el])
        folder = os.path.join(REAL_EXPORTS, BB.NAME, "Skins", el)
        skins[el] = K2.rebake_skin(target, sk_builder, folder, f"{BB.NAME}_{el}",
                                   extra=H.weathering(BB.K, scratches=0.4, dust=0.35, seed=71))
    import json
    path = os.path.join(REAL_EXPORTS, BB.NAME, "Skins", "Skins_warm_report.json")
    # Round 56: merge into the report (a run with --element Mire used to drop the Tide and Frost entries).
    report = {"asset": "Bat skins (warm glow; round 56: Mire, bog)", "skins": {}, "palettes": {}}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            report.update(json.load(f))
    report["asset"] = "Bat skins (warm glow; round 56: Mire, bog)"
    report["skins"].update({el: f"Skins/{el}/" for el in skins})
    report["palettes"].update({el: {"top": PALETTES[el]["pal"].top.tolist(), "glow": PALETTES[el]["pal"].glow.tolist(),
                                    "eye": PALETTES[el]["pal"].eye.tolist()} for el in skins})
    report[check_key] = uv_diff
    with open(path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    if C.cli_args().get("norender"):
        return
    renders(asset, arm, actions, skins, out)


def renders(asset, arm, actions, skins, out):
    """Per skin: game view zoomed (gliding at flight height) and a 3/4 close-up; a sheet with the other skins."""
    from mathutils import Vector
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    default = asset.low.data.materials[0]
    glide = actions["Glide"]
    paths = []
    all_skins = dict(skins)
    for el in ("Moss", "Dust"):
        folder = os.path.join(REAL_EXPORTS, BB.NAME, "Skins", el)
        tex = {k.lower(): os.path.join(folder, f"{BB.NAME}_{el}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
        if all(os.path.exists(p) for p in tex.values()):
            all_skins[el] = tex
    all_skins = {"Ember": None, **all_skins}
    arm.location.z = 0.7
    close = C.closeup_camera(Vector((0, 0, 0.7)), 2.0, azimuth=-38, elevation=30, lens=55)
    for el, tex in all_skins.items():
        asset.low.data.materials[0] = default if tex is None else K2.skin_material(asset, f"{BB.NAME}_{el}_Mat", tex)
        C.set_action(arm, glide, 10)
        arm.location.z = 0.7
        paths.append(C.render(close, os.path.join(out, f"bat_warm_{el.lower()}_close.png"), 960, 540))
        arm.location.z = 0
        single = [(glide, 10, (0, 0, BB.AIR_HEIGHT), 0.0), (actions["Fly"], 4, (-0.9, 0.5, BB.AIR_HEIGHT), 0.3)]
        paths.append(C.game_view_mixed(arm, asset.low, os.path.join(out, f"bat_warm_{el.lower()}_game.png"), BB.GAME_SCALE,
                                       single, zoom=3.0, close_lights=rig, target=(0, 0.2, BB.AIR_HEIGHT + 0.1)))
    asset.low.data.materials[0] = default
    C.contact_sheet(paths, os.path.join(out, "sheet_bat_skins_warm.png"), 2, 640)


if __name__ == "__main__":
    main()
