"""The NTI flag for the colony base (2026-11-08, Remco: the company logo "moet dan ook op de basis te zien zijn door
middel van een vlag"), plus a logo decal texture for base / hangar walls.

NTI_Flag: a flagpole on a bolted hex foot with a cyan beacon on top, and a cloth flag (cobalt field, white and yellow
hoist stripes, the NTI logo of build_brand.py, a gunmetal edge binding). The cloth is skinned to three rows of five
chained bones that a looping clip `Wave` (60 frames, 30 fps, 2 s) swings in phase-shifted sines, so a ripple runs
from the pole to the fly end. The rest pose is a flat flag (works without an Animator).

Both faces read correctly: the front and back of the cloth use their own UV regions (the back's art is mirrored), so
the logo is never seen in mirror writing whatever way Unity turns the prop.

Maps are painted directly (no bake): one 1024 set (+ Low 512) in the usual layout, Albedo (sRGB), Normal (tangent,
OpenGL +Y), Emission (sRGB), Mask (linear: R AO, G smoothness, B metal detail mask, A ceramic detail mask).
UV atlas: front cloth u 0..0.88, v 0.52..1; back cloth u 0..0.88, v 0.02..0.5; pole strip u 0.9..1.

Out: ArtSource/Exports/NTI_Flag/NTI_Flag.fbx + maps + Low/ + NTI_Flag_report.json,
     ArtSource/Exports/Brand/NTI_Decal_Wall.png (logo with a stencil frame for walls, RGBA 1024),
     previews in ArtSource/Previews/Brand/ (flag close, wave strip, flag at the base at play zoom and closer).

Run: blender --background --factory-startup --python Tools/Blender/build_flag.py -- [--norender] [--logo <png>]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import build_brand as BR  # noqa: E402
import common as C  # noqa: E402

NAME = "NTI_Flag"
OUT = os.path.join(C.EXPORTS, NAME)
PREVIEW = BR.PREVIEW
TEX, LOW = 1024, 512

POLE_H = 2.1
POLE_R = 0.024
FOOT_R = 0.17
FOOT_H = 0.06
FLAG_W, FLAG_H = 0.95, 0.52          # cloth size (aspect = its UV region's aspect, 0.88 x 0.48 of a square map)
FLAG_TOP = 2.0
HOIST_X = POLE_R + 0.008
COLS, ROWS = 5, 3                    # bone grid
NU, NV = 30, 16                      # cloth grid
WAVE_FRAMES = 60

FRONT_UV = (0.0, 0.52, 0.88, 1.0)
BACK_UV = (0.0, 0.02, 0.88, 0.5)
POLE_UV = (0.9, 0.0, 1.0, 1.0)

COBALT = np.array((0.11, 0.27, 0.66))
GUNMETAL = np.array(BR.PALETTE["gunmetal"])
WHITE = np.array(BR.PALETTE["white"])
YELLOW = np.array(BR.PALETTE["yellow"])
CYAN = np.array(BR.PALETTE["cyan"])


# ------------------------------------------------------------------------------------------------------------
# Flag art
# ------------------------------------------------------------------------------------------------------------

def flag_art(logo_path, w, h, mirror_logo=False):
    """(h, w, 3) albedo and (h, w) emission strength of the flag face, hoist on the left (column 0)."""
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = (x + 0.5) / w, (y + 0.5) / h
    col = np.broadcast_to(COBALT, (h, w, 3)).copy()
    # Hoist stripes: white then yellow, and a gunmetal sleeve where the cloth wraps the pole.
    col[u < 0.115] = WHITE
    col[(u >= 0.075) & (u < 0.1)] = YELLOW
    col[u < 0.035] = GUNMETAL
    edge = (v < 0.025) | (v > 0.975) | (u > 0.985)
    col[edge] = GUNMETAL * 1.2
    # A thin white pinstripe band at the fly end (a corporate livery accent).
    col[(u > 0.93) & (u < 0.945) & ~edge] = WHITE
    # Logo centred in the field.
    logo = C.image_to_array(logo_path)
    if mirror_logo:
        logo = logo[:, ::-1]
    size = int(h * 0.78)
    lh, lw = logo.shape[:2]
    ys = (np.arange(size) * lh // size)
    xs = (np.arange(size) * lw // size)
    small = logo[ys][:, xs]
    cx = int(w * (0.115 + 0.93) / 2)
    x0, y0 = cx - size // 2, (h - size) // 2
    a = small[..., 3:4]
    col[y0:y0 + size, x0:x0 + size] = col[y0:y0 + size, x0:x0 + size] * (1 - a) + small[..., :3] * a
    emit = np.zeros((h, w), np.float32)
    lu = (x[y0:y0 + size, x0:x0 + size] - x0) / size
    lv = (y[y0:y0 + size, x0:x0 + size] - y0) / size
    star = BR.sample(BR.classes(logo_path), (1 - lu if mirror_logo else lu).ravel(), lv.ravel())
    emit[y0:y0 + size, x0:x0 + size] = (star[:, BR.NAMES.index("star")] * 0.45).reshape(size, size)
    # Cloth weave and a little wear.
    rng = np.random.default_rng(5)
    weave = 0.95 + 0.05 * (((x // 2) + (y // 2)) % 2)
    col = col * (weave * (1 + 0.04 * rng.standard_normal((h, w))))[..., None]
    return np.clip(col, 0, 1), emit


def paint_maps(logo_path, size):
    alb = np.zeros((size, size, 4), np.float32)
    alb[..., 3] = 1
    emi = np.zeros((size, size, 4), np.float32)
    emi[..., 3] = 1
    nrm_ = np.zeros((size, size, 4), np.float32)
    nrm_[..., :] = (0.5, 0.5, 1.0, 1.0)
    mask = np.zeros((size, size, 4), np.float32)
    mask[...] = (1.0, 0.22, 0.0, 0.0)

    def region(r):
        u0, v0, u1, v1 = r
        return slice(int(v0 * size), int(v1 * size)), slice(int(u0 * size), int(u1 * size))

    for r, mirror in ((FRONT_UV, False), (BACK_UV, True)):
        ys, xs = region(r)
        h, w = ys.stop - ys.start, xs.stop - xs.start
        col, e = flag_art(logo_path, w, h, mirror_logo=mirror)
        if mirror:
            # The back's UVs run from the fly end, so its layout is mirrored to keep the hoist stripes at the pole; its
            # logo was mirrored once more above, so seen from behind it reads the right way round.
            col, e = col[:, ::-1], e[:, ::-1]
        alb[ys, xs, :3] = col
        emi[ys, xs, :3] = np.array(BR.PALETTE["star"])[None, None] * e[..., None]
        yy, xx = np.mgrid[0:h, 0:w]
        bump = 0.03 * np.sin(xx * math.pi / 1.5) * np.sin(yy * math.pi / 1.5)
        nrm_[ys, xs, 0] = 0.5 + bump
        nrm_[ys, xs, 1] = 0.5 - bump
    # Pole strip: v 0..0.08 foot (gunmetal, yellow hazard), 0.1..0.96 the pole (gunmetal low, white high with blue
    # bands), 0.97..1 the beacon (cyan, lit).
    ys, xs = region(POLE_UV)
    h, w = ys.stop - ys.start, xs.stop - xs.start
    v = (np.arange(h)[:, None] + 0.5) / size + 0.0
    uu = (np.arange(w)[None, :] + 0.5)
    col = np.broadcast_to(GUNMETAL, (h, w, 3)).copy()
    pole_v = (v - 0.1) / 0.86                       # 0 at the foot, 1 at the top of the pole
    hi = (pole_v > 0.32)[..., 0]
    col[hi] = WHITE * 0.92
    for c0, c1, c in ((0.3, 0.32, YELLOW), (0.68, 0.7, COBALT), (0.72, 0.725, COBALT), (0.0, 0.08, YELLOW)):
        m = ((pole_v >= c0) & (pole_v < c1))[..., 0]
        col[m] = c
    hazard = ((((uu + np.arange(h)[:, None]) // 6) % 2) == 0)
    foot = np.broadcast_to((v < 0.08), (h, w))
    col[foot & hazard] = YELLOW
    col[foot & ~hazard] = GUNMETAL * 0.7
    beacon = np.broadcast_to((v > 0.965), (h, w))
    col[beacon] = CYAN * 0.8
    alb[ys, xs, :3] = col
    emi[ys, xs, :3] = np.where(beacon[..., None], CYAN[None, None] * 1.0, 0.0)
    mask[ys, xs] = (1.0, 0.55, 1.0, 0.0)
    hi2 = np.broadcast_to(hi[:, None], (h, w))
    mask[ys, xs, 1] = np.where(hi2, 0.62, 0.5)
    mask[ys, xs, 2] = np.where(hi2, 0.0, 1.0)
    mask[ys, xs, 3] = np.where(hi2, 1.0, 0.0)
    return {"Albedo": alb, "Normal": nrm_, "Emission": emi, "Mask": mask}


def save_maps(logo_path):
    paths = {}
    os.makedirs(os.path.join(OUT, "Low"), exist_ok=True)
    for size, folder in ((TEX, OUT), (LOW, os.path.join(OUT, "Low"))):
        maps = paint_maps(logo_path, size)
        for k, arr in maps.items():
            p = os.path.join(folder, f"{NAME}_{k}.png")
            C.array_to_image(arr, p, colorspace="Non-Color" if k in ("Normal", "Mask") else None)
            if folder == OUT:
                paths[k.lower()] = p
    return paths


def wall_decal(logo_path, path):
    """The logo on a dark stencil plate with corner ticks and hazard tabs: a wall / hangar door decal (RGBA)."""
    cv = BR.Canvas(1024)
    plate = BR.poly(cv, BR.chamfer_rect(-0.96, -0.96, 0.96, 0.96, 0.12))
    cv.paint(plate, (0.08, 0.1, 0.15))
    cv.paint(plate & ~BR.poly(cv, BR.chamfer_rect(-0.9, -0.9, 0.9, 0.9, 0.09)), "white")
    for sx in (-1, 1):
        for x in np.arange(0.5, 0.8, 0.1):
            cv.paint(BR.poly(cv, [(sx * x, -0.9), (sx * (x + 0.05), -0.9), (sx * (x + 0.1), -0.8), (sx * (x + 0.05), -0.8)]), "yellow")
    img = cv.result()
    logo = C.image_to_array(logo_path)
    size = int(1024 * 0.72)
    idx = np.arange(size) * logo.shape[0] // size
    small = logo[idx][:, idx]
    o = (1024 - size) // 2 + 30
    a = small[..., 3:4]
    region = img[o:o + size, (1024 - size) // 2:(1024 - size) // 2 + size]
    region[..., :3] = region[..., :3] * (1 - a) + small[..., :3] * a
    region[..., 3:4] = np.maximum(region[..., 3:4], a)
    C.array_to_image(img, path)


# ------------------------------------------------------------------------------------------------------------
# Geometry and rig
# ------------------------------------------------------------------------------------------------------------

def uv_in(r, u, v):
    u0, v0, u1, v1 = r
    return (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)


def build_mesh():
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    tag = []   # (face, part): 'cloth' or 'pole'

    def face(verts, uvs, part):
        f = bm.faces.new(verts)
        for loop, uv in zip(f.loops, uvs):
            loop[uvl].uv = uv
        tag.append(part)
        return f

    # Cloth: two grids 1.2 mm apart, the front one on the -Y side facing -Y, the back one on +Y facing +Y.
    for side, r in ((1, FRONT_UV), (-1, BACK_UV)):
        grid = [[bm.verts.new((HOIST_X + FLAG_W * i / NU, -side * 0.0006, FLAG_TOP - FLAG_H * j / NV)) for i in range(NU + 1)]
                for j in range(NV + 1)]
        for j in range(NV):
            for i in range(NU):
                a, b, c, d = grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]
                us = [i / NU, (i + 1) / NU, (i + 1) / NU, i / NU]
                vs = [1 - j / NV, 1 - j / NV, 1 - (j + 1) / NV, 1 - (j + 1) / NV]
                if side < 0:
                    us = [1 - x for x in us]
                    face([a, d, c, b], [uv_in(r, us[k], vs[k]) for k in (0, 3, 2, 1)], "cloth")
                else:
                    face([d, c, b, a], [uv_in(r, us[k], vs[k]) for k in (3, 2, 1, 0)], "cloth")

    def ring(z, radius, sides, phase=0.0):
        return [bm.verts.new((radius * math.cos(phase + 2 * math.pi * k / sides), radius * math.sin(phase + 2 * math.pi * k / sides), z))
                for k in range(sides)]

    def band(r0, r1, z0, z1, v0, v1, sides, phase=0.0):
        lo, hi = ring(z0, r0, sides, phase), ring(z1, r1, sides, phase)
        for k in range(sides):
            k2 = (k + 1) % sides
            u_a, u_b = k / sides, (k + 1) / sides
            face([lo[k], lo[k2], hi[k2], hi[k]], [uv_in(POLE_UV, u_a, v0), uv_in(POLE_UV, u_b, v0), uv_in(POLE_UV, u_b, v1),
                                                  uv_in(POLE_UV, u_a, v1)], "pole")
        return lo, hi

    def cap(verts, v, up):
        c = bm.verts.new((0, 0, verts[0].co.z))
        for k in range(len(verts)):
            a, b = verts[k], verts[(k + 1) % len(verts)]
            vs = [a, b, c] if up else [b, a, c]
            face(vs, [uv_in(POLE_UV, 0.2, v), uv_in(POLE_UV, 0.8, v), uv_in(POLE_UV, 0.5, v)], "pole")

    # Foot: a hex plate with a bevelled top and a collar.
    _, top = band(FOOT_R, FOOT_R, 0.0, FOOT_H * 0.6, 0.0, 0.03, 6, math.pi / 6)
    _, top2 = band(FOOT_R, FOOT_R * 0.82, FOOT_H * 0.6, FOOT_H, 0.03, 0.06, 6, math.pi / 6)
    cap(top2, 0.065, True)
    band(POLE_R * 2.2, POLE_R * 1.6, FOOT_H, FOOT_H + 0.12, 0.06, 0.08, 12)
    # Pole, with a collar at the flag's bottom and top.
    band(POLE_R, POLE_R, FOOT_H, POLE_H, 0.1, 0.96, 12)
    for z in (FLAG_TOP - FLAG_H - 0.02, FLAG_TOP + 0.01):
        band(POLE_R * 1.5, POLE_R * 1.5, z, z + 0.035, 0.6, 0.62, 12)
    # Finial beacon.
    lo, hi = band(POLE_R * 1.7, POLE_R * 1.3, POLE_H, POLE_H + 0.07, 0.97, 1.0, 12)
    cap(hi, 0.99, True)
    mesh = bpy.data.meshes.new(NAME)
    bm.to_mesh(mesh)
    bm.free()
    for p, t in zip(mesh.polygons, tag):
        p.use_smooth = t == "pole"
    obj = C.link(bpy.data.objects.new(NAME, mesh))
    return obj, tag


def bone_name(row, col):
    return f"Flag_{'TMB'[row]}{col + 1}"


SEG = FLAG_W / COLS


def row_z(row):
    return FLAG_TOP - FLAG_H * row / (ROWS - 1)


def bones():
    out = [("Root", Vector((0, 0, 0)), Vector((0, 0, 0.4)), None, True)]
    for r in range(ROWS):
        for c in range(COLS):
            head = Vector((HOIST_X + SEG * c, 0, row_z(r)))
            out.append((bone_name(r, c), head, head + Vector((SEG, 0, 0)), "Root" if c == 0 else bone_name(r, c - 1), True))
    return out


def skin(obj, arm, tag):
    groups = {n: obj.vertex_groups.new(name=n) for n, *_ in bones()}
    cloth_verts = set()
    for p, t in zip(obj.data.polygons, tag):
        if t == "cloth":
            cloth_verts.update(p.vertices)
    for v in obj.data.vertices:
        if v.index not in cloth_verts:
            groups["Root"].add([v.index], 1.0, "REPLACE")
            continue
        s = (v.co.x - HOIST_X) / SEG
        c = min(COLS - 1, int(s))
        t = s - c
        rr = (FLAG_TOP - v.co.z) / FLAG_H * (ROWS - 1)
        r = min(ROWS - 2, int(rr))
        q = rr - r
        w = {}
        for (ri, wq) in ((r, 1 - q), (r + 1, q)):
            for (ci, wt) in ((c, 1 - t), (min(COLS - 1, c + 1), t)):
                if ci == c + 1 or ci == c:
                    w[bone_name(ri, ci)] = w.get(bone_name(ri, ci), 0.0) + wq * wt
        for n, wt in w.items():
            if wt > 1e-4:
                groups[n].add([v.index], wt, "REPLACE")
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm


AMP = (5.0, 8.0, 10.0, 12.0, 14.0)    # degrees of swing per column (pole -> fly end)
TWIST = (2.0, 3.0, 4.0, 5.0, 6.0)


def wave(t, f):
    spec = {}
    w = 2 * math.pi * t
    for r in range(ROWS):
        for c in range(COLS):
            ph = w - c * 1.05 - r * 0.55
            spec[bone_name(r, c)] = {"rot": ((0, 0, 1), math.radians(AMP[c] * math.sin(ph) * (0.6 if c == 0 else 1.0)))}
            # A small twist about the bone's own length gives the ripple a second, vertical component.
            spec[bone_name(r, c)]["rot2"] = ((1, 0, 0), math.radians(TWIST[c] * math.sin(ph * 1.0 + 1.3)))
    return spec


def key_wave(arm):
    from mathutils import Quaternion
    import tower_kit as tk
    arm.animation_data_create()
    act = bpy.data.actions.new("Wave")
    act.use_fake_user = True
    arm.animation_data.action = act
    for f in range(WAVE_FRAMES + 1):
        t = (f % WAVE_FRAMES) / WAVE_FRAMES
        spec = wave(t, f)
        for pb in arm.pose.bones:
            s = spec.get(pb.name)
            q = Quaternion()
            if s:
                for key in ("rot", "rot2"):
                    axis, ang = s[key]
                    q = q @ Quaternion(tk.local_axis(arm, pb.name, axis), ang)
            pb.rotation_quaternion = q
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            pb.location = (0, 0, 0)
            pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


# ------------------------------------------------------------------------------------------------------------
# Previews
# ------------------------------------------------------------------------------------------------------------

def set_frame(arm, f):
    arm.animation_data.action = bpy.data.actions["Wave"]
    bpy.context.scene.frame_set(f)


def previews(arm, obj, mat, logo):
    os.makedirs(PREVIEW, exist_ok=True)
    C.preview_world()
    lights = C.light_rig(scale=1.4)
    ground = C.ground()
    set_frame(arm, 12)
    paths = []
    for name, az, el in (("front", -20, 10), ("back", 160, 10), ("34", -50, 20)):
        cam = C.closeup_camera(Vector((0.4, 0, 1.3)), 4.4, azimuth=az, elevation=el, lens=50)
        paths.append(C.render(cam, os.path.join(PREVIEW, f"flag_{name}.png"), 900, 1100))
        bpy.data.objects.remove(cam)
    C.contact_sheet(paths, os.path.join(PREVIEW, "flag_sheet.png"), 3, 600)
    strip = []
    for f in range(0, WAVE_FRAMES, 10):
        set_frame(arm, f)
        cam = C.closeup_camera(Vector((0.5, 0, 1.6)), 2.6, azimuth=-15, elevation=12, lens=50)
        strip.append(C.render(cam, os.path.join(PREVIEW, f"_wave_{f}.png"), 480, 400, samples=24))
        bpy.data.objects.remove(cam)
    C.contact_sheet(strip, os.path.join(PREVIEW, "flag_wave_strip.png"), 6, 320)
    for p in strip:
        os.remove(p)
    # At the base: the round-37 base export with the flag on its spot (see the report), at play zoom and closer.
    C.hide_lights(lights, True)
    set_frame(arm, 18)
    import build_base2 as B2
    base_dir = os.path.join(C.EXPORTS, "Base")
    placed = B2.import_prop(os.path.join(base_dir, "Base.fbx"), base_dir, "Base", (0, 0, 0), 0) if os.path.exists(
        os.path.join(base_dir, "Base.fbx")) else []
    for o in placed:
        if o.type == "MESH" and ("Damage" in o.name or "Wreck" in o.name):
            o.hide_render = True
    arm.location = Vector(SPOT)
    arm.rotation_euler.z = math.radians(SPOT_YAW)
    bk.render_game(Vector((0.3, 0.5, 0.5)), os.path.join(PREVIEW, "flag_base_game.png"), zoom=1.6, width=1280, height=720)
    bk.render_game(Vector((1.6, 1.4, 0.9)), os.path.join(PREVIEW, "flag_base_zoom.png"), zoom=4.0, width=1280, height=720)
    bk.render_close(Vector((1.0, 1.2, 0.9)), os.path.join(PREVIEW, "flag_base_close.png"), 7.0, azimuth=20, elevation=24, lens=50,
                    width=1280, height=800, light_scale=2.0)
    bk.remove(placed)


# Where the flag stands at the base (base-local Blender metres; base pivot at the origin, -Y faces the road, the game
# camera looks from +Y): on the deck between the habitat and the landing pad, in front of the camera.
SPOT = (0.42, 2.16, 0.23)
SPOT_YAW = 180.0


def main():
    args = C.cli_args()
    C.reset_scene()
    logo = args.get("logo") or BR.LOGO
    if not os.path.exists(logo):
        BR.main()
    paths = save_maps(logo)
    wall_decal(logo, os.path.join(BR.OUT, "NTI_Decal_Wall.png"))
    obj, tag = build_mesh()
    arm = C.build_armature(NAME + "_Rig", bones())
    skin(obj, arm, tag)
    mat = C.game_material(NAME + "_Mat", paths, 1.5, 0.4)
    obj.data.materials.append(mat)
    key_wave(arm)
    arm.animation_data.action = bpy.data.actions["Wave"]
    C.export_fbx(os.path.join(OUT, NAME + ".fbx"), [arm, obj], animated=True)
    tris = C.tri_count(obj)
    report = {
        "asset": NAME, "texture_size": TEX, "low_size": LOW, "maps": ["Albedo", "Normal", "Emission", "Mask"],
        "mask": "R AO, G smoothness, B metal detail mask, A ceramic detail mask", "tris": tris, "lod1": "none (1 small prop)",
        "bones": [n for n, *_ in bones()], "clips": {"Wave": {"frames": WAVE_FRAMES, "fps": 30, "loop": True}},
        "rest_pose": "flat flag; the Wave clip is optional",
        "pivot": "foot of the pole on the ground; the cloth flies along +X in Blender (Unity -X after the axis conversion)",
        "size_m": {"pole_height": POLE_H + 0.07, "flag": [FLAG_W, FLAG_H]},
        "logo": os.path.relpath(logo, C.REPO).replace("\\", "/"),
        "emission": "beacon on the pole top (cyan) and the logo star (faint)",
        "two_sided": "front and back cloth faces have their own UVs; the logo reads correctly from both sides",
        "placement": {"base_local_blender": SPOT, "yaw_deg": SPOT_YAW,
                      "note": "on the base deck between the habitat and the landing pad, close to the camera side; "
                              "base-local Blender (x, y, z) -> Unity (-x, z, -y) with the usual FBX axis conversion"},
    }
    with open(os.path.join(OUT, NAME + "_report.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=2)
    C.save_blend(NAME)
    if not args.get("norender"):
        previews(arm, obj, mat, logo)


if __name__ == "__main__":
    main()
