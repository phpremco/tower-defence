"""Mercer's weapons (round 6, see ArtSource/DESIGN_NOTES.md): a compact pistol with a detachable magazine (plus the
Quick Reload extended magazine), a laser sword hilt (plus the Wide Slash forked emitter) and two blade meshes.

Every weapon is modelled in its socket's space, the "grip space" of the hand bones `Weapon_L` / `Weapon_R`:
  +Z  the pistol's barrel / the sword's edge direction,
  +Y  the top of the pistol (thumb side of the fist) / the sword's blade,
  origin  the centre of the closed fist.
Exported with the shared FBX settings, each mesh arrives in Unity with that frame as its own local frame, so a weapon
parented to a socket bone with an identity local transform sits in the hand (see ArtSource/README.md, round 6).

The pistol, magazines and hilts share one material and one 512 map set (`MercerWeapons_*`). The blades have no
textures: UV0.x runs 0 on the core to 1 on the rim, UV0.y 0 at the emitter to 1 at the tip, for an emissive shader.

Run: blender --background --python Tools/Blender/build_mercer_weapons.py -- [--preview <dir>] [--norender]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import detailmaps  # noqa: E402
import suit as S  # noqa: E402
import uvlayout  # noqa: E402

NAME = "MercerWeapons"
TEX = 512
FAMILIES = ("metal", "ceramic")  # Mask B = metal (no tiling map), A = ceramic (Detail_Ceramic_Normal)
GRIP_TILT = math.radians(15)
# Where each piece is built (apart, so the remesh keeps them separate) and its triangle budget.
PIECES = {
    "Pistol": (Vector((0.0, 0.0, 0.0)), 1250),
    "Pistol_Mag": (Vector((0.45, 0.0, 0.0)), 120),
    "Pistol_Mag_Quick": (Vector((0.9, 0.0, 0.0)), 200),
    "Sword_Hilt": (Vector((0.0, 0.0, 0.6)), 760),
    "Sword_Hilt_Wide": (Vector((0.45, 0.0, 0.6)), 800),
}
MUZZLE = Vector((0.0, 0.062, 0.192))
BLADE_BASE = 0.118
BLADE_LENGTH = 0.85
BLADE_HALF_WIDTH = (0.021, 0.034)  # normal, Wide Slash
BLADE_HALF_THICK = 0.009
# The grip handle runs through the fist from the pinky (bottom) to the thumb (top), tilted forward.
HANDLE_AXIS = Vector((0.0, math.cos(GRIP_TILT), math.sin(GRIP_TILT)))


def boxy(e=0.3):
    """Ellipsoid deform: a sphere pushed towards a rounded box (the remesh and smoothing soften the edges)."""
    def deform(v):
        return Vector(tuple(math.copysign(abs(c) ** e, c) for c in v))
    return deform


def frame_rot(y_axis, z_hint):
    """Rotation whose local Y is `y_axis` and local Z as close to `z_hint` as possible (columns X, Y, Z)."""
    y = Vector(y_axis).normalized()
    z = (Vector(z_hint) - y * Vector(z_hint).dot(y)).normalized()
    x = y.cross(z)
    return Matrix((x, y, z)).transposed()


def metal(features=(), **kw):
    return S.styled("metal", features, **kw)


def plate(features=(), **kw):
    return S.styled("plate", features, **kw)


def rubber(features=(), **kw):
    return S.styled("rubber", features, **kw)


CYAN = dict(emit=S.PAL.cyan, glow=1.0, rough=0.3, family={})
YELLOW = dict(paint=S.PAL.yellow, wear=0.6, rough=0.5)


def pistol(b, o, mag_only=None):
    """The pistol body (mag_only None), or just a magazine ("normal" / "quick") at offset `o`."""
    down = -HANDLE_AXIS
    if mag_only:
        length = 0.075 if mag_only == "normal" else 0.11
        top = o + down * 0.0
        centre = top + down * (length * 0.5 + 0.005)
        b.ellipsoid("Mag", centre, (0.0125, length * 0.5, 0.019), frame_rot(HANDLE_AXIS, (0, 0, 1)), style=metal(), deform=boxy(0.28),
                    segments=(20, 10))
        base_c = top + down * (length + 0.008)
        base_style = plate([(lambda c: np.ones(len(c.p)), YELLOW)]) if mag_only == "quick" else metal()
        b.ellipsoid("MagBase", base_c, (0.0165, 0.008, 0.025), frame_rot(HANDLE_AXIS, (0, 0, 1)), style=base_style,
                    deform=boxy(0.3), segments=(16, 8))
        if mag_only == "quick":
            # A cyan charge window down the side of the long magazine.
            b.ellipsoid("MagCell", centre + Vector((0.0, 0.0, 0.0)), (0.0135, length * 0.3, 0.008), frame_rot(HANDLE_AXIS, (0, 0, 1)),
                        style=S.light(strength=1.0), segments=(12, 8))
        return
    # Handle: rubber-wrapped, tilted, wider at the palm swell.
    b.ellipsoid("Handle", o + down * 0.012, (0.0165, 0.052, 0.025), frame_rot(HANDLE_AXIS, (0, 0, 1)),
                style=rubber([(lambda c: S.band(c.u[:, 1], 0.0, 1.0) * (np.abs(np.sin(c.u[:, 1] * 9.0)) > 0.85), dict(groove=0.0012))]),
                deform=boxy(0.35), segments=(24, 12))
    # Frame under the slide, with the trigger guard.
    b.ellipsoid("Frame", o + Vector((0.0, 0.045, 0.04)), (0.0165, 0.016, 0.075), style=metal(), deform=boxy(0.3), segments=(24, 12))
    b.tube("Guard", [o + Vector((0, 0.035, 0.008)), o + Vector((0, 0.004, 0.03)), o + Vector((0, 0.008, 0.065)), o + Vector((0, 0.034, 0.075))],
           [0.0055, 0.0055, 0.0055, 0.0055], sides=8, style=metal())
    b.tube("Trigger", [o + Vector((0, 0.03, 0.04)), o + Vector((0, 0.016, 0.044)), o + Vector((0, 0.01, 0.038))],
           [0.0035, 0.003, 0.0028], sides=6, style=metal())
    # The slide: white ceramic top with a cyan charge window on both sides and a yellow stripe on top.
    slide_c = o + Vector((0.0, 0.069, 0.052))
    b.ellipsoid("Slide", slide_c, (0.019, 0.022, 0.105),
                style=plate([(lambda c: S.band(c.u[:, 2], 0.05, 0.42) * S.band(c.u[:, 1], -0.05, 0.38) * (np.abs(c.u[:, 0]) > 0.6), CYAN),
                             (lambda c: S.band(c.u[:, 0], 0.0, 0.16) * (c.u[:, 1] > 0.6) * S.band(c.u[:, 2], 0.2, 0.5), YELLOW),
                             (lambda c: S.line(c.u[:, 2], -0.55, 0.03) + S.line(c.u[:, 2], 0.62, 0.03), dict(groove=0.0008))]),
                deform=boxy(0.25), segments=(32, 16))
    b.ellipsoid("RearSight", o + Vector((0, 0.094, -0.035)), (0.012, 0.007, 0.012), style=metal(), deform=boxy(0.3), segments=(12, 8))
    b.ellipsoid("FrontSight", o + Vector((0, 0.093, 0.15)), (0.004, 0.006, 0.006), style=metal(), segments=(8, 6))
    # Barrel shroud and a yellow muzzle ring, with the cyan muzzle bore.
    b.tube("Shroud", [o + Vector((0, 0.062, 0.14)), o + Vector((0, 0.062, 0.178))], [0.0175, 0.0165], sides=14, style=metal())
    b.tube("Muzzle", [o + Vector((0, 0.062, 0.176)), o + Vector((0, 0.062, 0.19))], [0.0185, 0.0185], sides=14,
           style=plate([(lambda c: np.ones(len(c.p)), YELLOW)]))
    b.ellipsoid("Bore", o + Vector((0, 0.062, 0.19)), (0.009, 0.009, 0.004), style=S.light(strength=1.2), segments=(12, 8))
    # The magazine well's lip at the bottom of the handle (the magazine itself is a separate mesh).
    b.ellipsoid("Well", o + down * 0.062, (0.018, 0.007, 0.027), frame_rot(HANDLE_AXIS, (0, 0, 1)), style=metal(), deform=boxy(0.3),
                segments=(16, 8))


def hilt(b, o, wide=False):
    """The laser sword hilt along +Y: pommel, rubber-ringed grip, white guard with a yellow stripe, emitter."""
    up = Vector((0, 1, 0))
    b.ellipsoid("Pommel", o + up * -0.078, (0.022, 0.018, 0.022), style=metal([(lambda c: S.band(c.u[:, 1], -0.2, 0.18), CYAN)]),
                segments=(20, 10))
    b.tube("Grip", [o + up * -0.068, o + up * 0.0, o + up * 0.056], [0.0175, 0.0168, 0.0175], sides=14,
           style=rubber([(lambda c: (np.abs(np.sin(c.t * math.pi * 7)) < 0.25).astype(float), dict(groove=0.0014))]))
    b.ellipsoid("Guard", o + up * 0.066, (0.046 if not wide else 0.052, 0.013, 0.03),
                style=plate([(lambda c: S.band(c.u[:, 2], 0.0, 0.3) * (c.u[:, 1] > -0.2), YELLOW)]), deform=boxy(0.4),
                segments=(24, 12))
    b.tube("Emitter", [o + up * 0.072, o + up * BLADE_BASE], [0.021, 0.018], sides=14,
           style=metal([(lambda c: S.band(c.t, 0.9, 0.12) + S.band(c.t, 0.45, 0.06), CYAN)]))
    b.ellipsoid("Lens", o + up * BLADE_BASE, (0.013, 0.004, 0.013), style=S.light(strength=1.4), segments=(12, 8))
    if wide:
        # Wide Slash: two prongs flare out of the emitter and spread the blade.
        for sx in (-1, 1):
            b.tube("Prong", [o + Vector((0.016 * sx, 0.085, 0.0)), o + Vector((0.034 * sx, 0.11, 0.0)), o + Vector((0.042 * sx, 0.142, 0.0))],
                   [0.0075, 0.006, 0.004], sides=8, style=metal([(lambda c: S.band(c.t, 1.0, 0.25), CYAN)]))


def build_parts():
    b = C.PartBuilder()
    o = PIECES
    pistol(b, o["Pistol"][0])
    pistol(b, o["Pistol_Mag"][0], mag_only="normal")
    pistol(b, o["Pistol_Mag_Quick"][0], mag_only="quick")
    hilt(b, o["Sword_Hilt"][0])
    hilt(b, o["Sword_Hilt_Wide"][0], wide=True)
    return b


def blade_mesh(name, half_width, prongs=False):
    """A flattened tapered beam along +Y from the emitter. UV0.x: 0 core .. 1 rim; UV0.y: 0 base .. 1 tip."""
    rings, sides = 14, 12
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    grid = []
    for i in range(rings + 1):
        t = i / rings
        y = BLADE_BASE + BLADE_LENGTH * t
        # Full width most of the way, a long point in the last fifth; the Wide blade flares from the prongs.
        taper = 1.0 if t < 0.78 else math.cos((t - 0.78) / 0.22 * math.pi * 0.5) ** 0.7
        flare = (0.65 + 0.35 * min(1.0, t / 0.08)) if prongs else (0.8 + 0.2 * min(1.0, t / 0.05))
        w = half_width * taper * flare
        ring = []
        for s in range(sides):
            a = 2 * math.pi * s / sides
            ring.append((bm.verts.new((math.cos(a) * w, y, math.sin(a) * BLADE_HALF_THICK * max(taper, 0.3))), abs(math.cos(a)), t))
        grid.append(ring)
    tip = bm.verts.new((0.0, BLADE_BASE + BLADE_LENGTH + 0.01, 0.0))
    base = bm.verts.new((0.0, BLADE_BASE, 0.0))
    for i in range(rings):
        for s in range(sides):
            s2 = (s + 1) % sides
            quad = [grid[i][s], grid[i][s2], grid[i + 1][s2], grid[i + 1][s]]
            f = bm.faces.new([q[0] for q in quad])
            for loop, q in zip(f.loops, quad):
                loop[uv].uv = (q[1], q[2])
    for s in range(sides):
        s2 = (s + 1) % sides
        f = bm.faces.new((grid[-1][s][0], grid[-1][s2][0], tip))
        for loop, u in zip(f.loops, (grid[-1][s][1], grid[-1][s2][1], 0.0)):
            loop[uv].uv = (u, 1.0)
        f = bm.faces.new((grid[0][s2][0], grid[0][s][0], base))
        for loop, u in zip(f.loops, (grid[0][s2][1], grid[0][s][1], 0.0)):
            loop[uv].uv = (u, 0.0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.shade_smooth()
    obj = C.link(bpy.data.objects.new(name, mesh))
    mat = C.principled(name + "_Preview", color=(0.3, 0.85, 1.0), rough=0.3, emission=(0.25, 0.85, 1.0), strength=1.6)
    mesh.materials.append(mat)
    return obj


def split_piece(mesh, centre, radius):
    """Copy of the faces of `mesh` within `radius` of `centre`."""
    bm = bmesh.new()
    bm.from_mesh(mesh)
    far = [v for v in bm.verts if (v.co - centre).length > radius]
    bmesh.ops.delete(bm, geom=far, context="VERTS")
    out = bpy.data.meshes.new("piece")
    bm.to_mesh(out)
    bm.free()
    return out


def build():
    """Builds, bakes and exports the weapons into the current scene. Returns {piece or blade name: object}."""
    builder = build_parts()
    parts_obj = builder.to_object(NAME + "_Parts")
    high_mesh = C.fuse(parts_obj, 0.0011, smooth_iterations=4)
    bpy.data.objects.remove(parts_obj)
    smooth_copy = high_mesh.copy()
    C.detail(high_mesh, builder)
    C.shade_smooth(high_mesh)
    high = C.link(bpy.data.objects.new(NAME + "_High", high_mesh))
    high.data.materials.append(C.high_bake_material(edge_gain=0.5, families=FAMILIES, family_set=S.FAMILY_SET))
    C.bake_vertex_ao(high, 0.02)

    lows = []
    for name, (offset, tris) in PIECES.items():
        piece = split_piece(smooth_copy, offset, 0.22)
        lows.append(C.decimate(piece, tris, name, symmetric=False))
    bm = bmesh.new()
    for m in lows:
        bm.from_mesh(m)
    joined = bpy.data.meshes.new(NAME)
    bm.to_mesh(joined)
    bm.free()
    C.shade_smooth(joined)
    joined.set_sharp_from_angle(angle=math.radians(50))
    low = C.link(bpy.data.objects.new(NAME, joined))
    uv_stats, _ = uvlayout.unwrap(low, builder, mirror=False)
    out_dir = os.path.join(C.EXPORTS, "Mercer", "Weapons")
    textures = C.bake_maps(high, low, out_dir, NAME, TEX, extrusion=0.004, ray=0.012)
    tiles = detailmaps.ensure()
    tiling = {f: C.detail_tiling(uv_stats["world_per_uv"], f, 1.0) for f in ("ceramic",)}
    mat = C.game_material(NAME + "_Mat", textures, 2.5, 0.4, [(tiles["ceramic"], tiling["ceramic"], 0.6, "A")])
    high.hide_render = high.hide_viewport = True

    objs = {}
    for name, (offset, _) in PIECES.items():
        piece = split_piece(low.data, offset, 0.22)
        piece.name = name
        piece.transform(Matrix.Translation(-offset))
        piece.materials.clear()
        piece.materials.append(mat)
        obj = C.link(bpy.data.objects.new(name, piece))
        objs[name] = obj
    bpy.data.objects.remove(low)
    objs["Sword_Blade"] = blade_mesh("Sword_Blade", BLADE_HALF_WIDTH[0])
    objs["Sword_Blade_Wide"] = blade_mesh("Sword_Blade_Wide", BLADE_HALF_WIDTH[1], prongs=True)

    tris = {n: C.tri_count(o) for n, o in objs.items()}
    export(objs)
    report = {"asset": NAME, "texture_size": TEX, "maps": ["Albedo", "Normal", "Emission", "Mask"], "tris": tris,
              "pistol_total_tris": tris["Pistol"] + tris["Pistol_Mag"],
              "uv": uv_stats, "detail": {"ceramic": {"map": "Detail_Ceramic_Normal.png", "tiling": tiling["ceramic"], "mask": "A"}},
              "grip_space": "+Z barrel / sword edge, +Y pistol top / sword blade, origin = centre of the fist",
              "muzzle": [round(c, 4) for c in MUZZLE], "blade_base": BLADE_BASE,
              "blade_tip": BLADE_BASE + BLADE_LENGTH + 0.01, "blade_uv": "x 0 core .. 1 rim, y 0 base .. 1 tip"}
    path = os.path.join(out_dir, NAME + "_report.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    C.log("report", report)
    return objs


EXPORT_GROUPS = {
    "Mercer_Pistol": ["Pistol", "Pistol_Mag", "Pistol_Mag_Quick"],
    "Mercer_SwordHilt": ["Sword_Hilt", "Sword_Hilt_Wide"],
    "Mercer_SwordBlade": ["Sword_Blade", "Sword_Blade_Wide"],
}


def export(objs):
    """Each group as one FBX. The meshes are turned +90 degrees about X for the export only, which is what makes
    the grip space arrive as the Unity mesh's own frame (z barrel, y top) under the shared axis conversion."""
    out_dir = os.path.join(C.EXPORTS, "Mercer", "Weapons")
    turn = Matrix.Rotation(math.radians(90), 4, "X")
    for fbx, names in EXPORT_GROUPS.items():
        copies = []
        for n in names:
            src = objs[n]
            cp = C.link(src.copy())
            cp.data = src.data.copy()
            cp.data.transform(turn)
            cp.name = n
            copies.append(cp)
        # The copies take the plain names in the FBX; the originals step aside for the export.
        for n in names:
            objs[n].name = n + "_src"
        for cp, n in zip(copies, names):
            cp.name = n
        C.export_fbx(os.path.join(out_dir, fbx + ".fbx"), copies, animated=False)
        for cp in copies:
            bpy.data.objects.remove(cp)
        for n in names:
            objs[n].name = n


def previews(objs, out):
    C.preview_world()
    rig = C.light_rig(scale=0.3)
    layout = {"Pistol": Vector((-0.3, 0, 0.1)), "Pistol_Mag": Vector((-0.3, 0, 0.1)), "Pistol_Mag_Quick": Vector((-0.05, 0, 0.1)),
              "Sword_Hilt": Vector((0.15, 0, 0.0)), "Sword_Blade": Vector((0.15, 0, 0.0)),
              "Sword_Hilt_Wide": Vector((0.4, 0, 0.0)), "Sword_Blade_Wide": Vector((0.4, 0, 0.0))}
    # Lay them out standing up in grip space turned to face the camera (z barrel to the right of the frame).
    turn = Matrix.Rotation(math.radians(90), 4, "X") @ Matrix.Rotation(math.radians(-90), 4, "Y")
    keep = {}
    for n, obj in objs.items():
        obj.hide_render = False
        keep[n] = obj.matrix_world.copy()
        obj.matrix_world = Matrix.Translation(layout[n]) @ (turn if n.startswith("Pistol") else Matrix.Rotation(math.radians(90), 4, "X"))
    cam = C.closeup_camera(Vector((0.05, 0, 0.44)), 3.1, azimuth=-20, elevation=8, lens=60)
    C.render(cam, os.path.join(out, "mercer_weapons.png"), 1280, 720)
    cam2 = C.closeup_camera(Vector((-0.3, 0, 0.16)), 0.55, azimuth=-35, elevation=15, lens=70)
    C.render(cam2, os.path.join(out, "mercer_pistol_close.png"), 1280, 720)
    cam3 = C.closeup_camera(Vector((0.27, 0, 0.1)), 0.6, azimuth=-30, elevation=15, lens=70)
    C.render(cam3, os.path.join(out, "mercer_hilts_close.png"), 1280, 720)
    for o in (cam, cam2, cam3, *rig):
        bpy.data.objects.remove(o)
    for n, obj in objs.items():
        obj.matrix_world = keep[n]


def main():
    C.reset_scene()
    out = C.preview_dir()
    objs = build()
    C.save_blend("MercerWeapons")
    if not C.cli_args().get("norender"):
        previews(objs, out)


if __name__ == "__main__":
    main()
