"""Dr. Elena Voss, chief scientist: round 19 rebuild (see ArtSource/DESIGN_NOTES.md, round 19). Replaces
build_voss.py's exports (same file names, mesh `Voss`, armature `Voss_Rig`, the same 10 bones and clips).

What changed against rounds 13/16 (fused primitives, decimated, baked):
- The head is a modelled surface (voss2_head.py): a sculpted signed distance field (cranium, tapered jaw, cheekbones,
  cheeks, lids over the eyeballs, a small nose, lips, ears, sculpted hair with locks, a side-swept fringe and a coiled
  bun) meshed with real edge loops: five concentric loops round each eye, five round the mouth, a size field that is
  dense at the eyes and lips. The eye openings roll over the eyeballs as lid margins and the mouth slit rolls into a
  mouth bag, so the Lid and Jaw bones move whole loops.
- Textures are painted in texel space (portrait_kit G-buffer + voss2_paint.py): one 2048 set, the face island at 2.4x
  the texel density of the rest; Low/ is 1024.
- A full figure (voss2_body.py): the lab coat to the knee, hands in its pockets, trousers and boots, for cutscenes;
  the bust framing is unchanged.
- Face shape keys for portraits: Blink, Smile, JawOpen, Frown.

Run: blender --background --factory-startup --python Tools/Blender/build_voss2.py -- [--preview <dir>] [--norender]
     [--stage <dir>] (exports and .blend there) [--size 2048]
"""

import json
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import portrait_kit as pk  # noqa: E402
import tower_kit as tk  # noqa: E402
import voss2_body as B  # noqa: E402
import voss2_gear as GR  # noqa: E402
import voss2_head as H  # noqa: E402
import voss2_paint as VP  # noqa: E402

NAME = "Voss"
TEX, LOW_TEX = 2048, 1024
XAX = Vector((1, 0, 0))
UPV = Vector((0, 0, 1))

FACE_WEIGHT = 2.4
HAIR_WEIGHT = 1.0
NECK_WEIGHT = 0.8

# Part ids: the painter of every texel and the rig region of every vertex.
P_HEAD, P_EYE, P_LASH = 1, 2, 3
P_COAT, P_SLEEVE, P_TOP, P_TROUSERS, P_BOOT, P_COLLAR, P_LAPEL, P_POCKET = 10, 11, 12, 13, 14, 15, 16, 17
P_METAL, P_CARD, P_GLOW, P_BUTTON, P_PEN = 20, 21, 22, 23, 24

PAINTERS = {P_HEAD: VP.head_paint, P_EYE: VP.eye_paint, P_LASH: VP.lash_paint, P_COAT: VP.coat_paint, P_SLEEVE: VP.sleeve_paint,
            P_TOP: VP.top_paint, P_TROUSERS: VP.trousers_paint, P_BOOT: VP.boot_paint, P_COLLAR: VP.collar_paint,
            P_LAPEL: VP.lapel_paint, P_POCKET: VP.pocket_paint, P_METAL: VP.metal_paint, P_CARD: VP.card_paint,
            P_GLOW: VP.glow_paint, P_BUTTON: VP.button_paint, P_PEN: VP.pen_paint}


def game_to_blender(p):
    p = np.asarray(p, float)
    return np.stack([p[..., 0], -p[..., 2], p[..., 1]], -1)


def blender_to_game(b):
    b = np.asarray(b, float)
    return np.stack([b[..., 0], b[..., 2], -b[..., 1]], -1)


# -------------------------------------------------------------------------------------------------------------
# Parts
# -------------------------------------------------------------------------------------------------------------

def head_part(hm):
    """The head mesh: LSCM islands cut along the hairline, round the face and down the back; the face is denser."""
    p = hm.p
    faces = hm.faces
    cen = np.array([p[list(f)].mean(0) for f in faces])
    hair_face = H.is_hair(cen)
    edge_faces = {}
    for fi, f in enumerate(faces):
        for a, b in zip(f, f[1:] + f[:1]):
            edge_faces.setdefault((min(a, b), max(a, b)), []).append(fi)
    q = cen - H.RAY_CENTRE
    az = np.degrees(np.arctan2(q[:, 0], q[:, 2]))
    el = np.degrees(np.arctan2(q[:, 1], np.hypot(q[:, 0], q[:, 2])))
    front = (~hair_face) & (np.abs(az) < 76) & (el > -40)
    seams = []
    for e, fs in edge_faces.items():
        if len(fs) != 2:
            continue
        a, b = fs
        if hair_face[a] != hair_face[b]:
            seams.append(e)
        elif e[0] in hm.seam and e[1] in hm.seam:
            seams.append(e)
        elif front[a] != front[b] and not hair_face[a] and not hair_face[b]:
            seams.append(e)
    weight = np.where(front, FACE_WEIGHT, np.where(hair_face, HAIR_WEIGHT, NECK_WEIGHT))
    params = np.stack([hm.tag.astype(float), np.zeros(len(p)), np.zeros(len(p))], 1)
    return pk.Part("Head", H.to_blender(p), faces, pid=P_HEAD, seams=seams, face_weight=weight, params=params)


def eye_parts():
    parts = []
    for s in (1, -1):
        v, f, ang = H.eyeball(s)
        q = v - H.eye_c(s)
        ph = np.arctan2(q[:, 1], q[:, 0])
        uv_v = np.stack([ang * np.cos(ph), ang * np.sin(ph)], 1) * H.R_EYE
        uv = [[tuple(uv_v[i]) for i in face] for face in f]
        parts.append(pk.Part("Eye_" + ("L" if s > 0 else "R"), H.to_blender(v), f, pid=P_EYE, weight=1.6, uv=uv))
    return parts


def lash_part(hm):
    """Upper lashes: a thin dark wedge along each upper lid rim, longer and lifting at the outer corner."""
    verts, faces = [], []
    for side, s in (("L", 1), ("R", -1)):
        rim = hm.eye_rings[side][0][:H.EYE_N]
        c = H.eye_c(s)
        base = len(verts)
        n = len(rim)
        for k, i in enumerate(rim):
            t = k / (n - 1)
            p = hm.p[i]
            d = p - c
            d /= np.linalg.norm(d)
            fwd = np.array((s * 0.25 * t, 0.35 + 0.25 * t, 1.0))
            fwd /= np.linalg.norm(fwd)
            out = d * 0.35 + fwd * 0.65
            out /= np.linalg.norm(out)
            length = 0.0009 + 0.0019 * t ** 1.1
            verts += [p + d * 0.00015, p - d * 0.0006 + np.array((0, -0.0004, 0)), p + out * length + np.array((0, 0.0013 * t * t, 0))]
        for k in range(n - 1):
            a0, b0, t0 = base + 3 * k, base + 3 * k + 1, base + 3 * k + 2
            a1, b1, t1 = a0 + 3, b0 + 3, t0 + 3
            faces += [(a0, a1, t1, t0), (b1, b0, t0, t1), (a1, a0, b0, b1)]
    return pk.Part("Lashes", H.to_blender(np.array(verts)), faces, pid=P_LASH, weight=0.5)


def piece_part(name, piece, pid, weight, space="game"):
    conv = game_to_blender if space == "game" else H.to_blender
    return pk.Part(name, conv(piece.verts), piece.faces, pid=pid, weight=weight, uv=piece.uv, params=piece.params)


def body_parts():
    parts = [piece_part("Coat", B.coat_body(), P_COAT, 0.8), piece_part("Collar", B.collar(), P_COLLAR, 1.0),
             piece_part("Top", B.mock_top(), P_TOP, 1.0)]
    for s, side in ((1, "L"), (-1, "R")):
        parts += [piece_part("Lapel_" + side, B.lapel(s), P_LAPEL, 1.0), piece_part("Sleeve_" + side, B.sleeve(s), P_SLEEVE, 0.8),
                  piece_part("Pocket_" + side, B.pocket(s), P_POCKET, 0.8), piece_part("Trousers_" + side, B.trousers(s), P_TROUSERS, 0.45),
                  piece_part("Boot_" + side, B.boot(s), P_BOOT, 0.55)]
    return parts


def gear_parts():
    band, ear, ring, pad, boom, mic = GR.headset()
    rod, tip = GR.hair_pin()
    card, clip = GR.card()
    parts = [piece_part("Band", band, P_METAL, 1.0, "head"), piece_part("Earpiece", ear, P_METAL, 1.0, "head"),
             piece_part("EarRing", ring, P_GLOW, 0.6, "head"), piece_part("EarPad", pad, P_METAL, 1.0, "head"),
             piece_part("Boom", boom, P_METAL, 0.6, "head"), piece_part("Mic", mic, P_GLOW, 1.0, "head"),
             piece_part("Pin", rod, P_METAL, 0.8, "head"), piece_part("PinTip", tip, P_GLOW, 1.0, "head"),
             piece_part("Card", card, P_CARD, 2.6), piece_part("CardClip", clip, P_METAL, 1.0),
             piece_part("Buttons", GR.buttons(), P_BUTTON, 1.0), piece_part("Pens", GR.pens(), P_PEN, 1.0),
             piece_part("BreastPocket", GR.breast_pocket(), P_POCKET, 0.8)]
    return parts


def assemble(hm):
    asm = pk.Assembly(NAME)
    asm.add(head_part(hm))
    for part in eye_parts():
        asm.add(part)
    asm.add(lash_part(hm))
    for part in body_parts() + gear_parts():
        asm.add(part)
    obj = asm.build()
    return asm, obj


def ranges(asm):
    """name -> (start, end) vertex range in the assembled object."""
    out = {}
    for k, part in enumerate(asm.parts):
        out[part.name] = (asm.offsets[k], asm.offsets[k] + len(part.verts))
    return out


# -------------------------------------------------------------------------------------------------------------
# Painting
# -------------------------------------------------------------------------------------------------------------

def paint_maps(obj, size, out_dir, prefix, low_size=None):
    """G-buffer bake, the painters per part id, AO; writes Albedo, Normal, Emission and Mask (+ Low/)."""
    t0 = time.time()
    pk.gbuffer_attrs(obj)
    gb, cover = pk.bake_gbuffer(obj, size, ["GPos", "GNrm", "Pid", "Prm"])
    C.log("gbuffer", round(time.time() - t0, 1))
    P = gb["GPos"] - 2.0
    N = gb["GNrm"] * 2 - 1
    pid = np.rint(gb["Pid"][..., 0]).astype(int).ravel()
    flat = cover.ravel()
    Pl = H.from_blender(P.reshape(-1, 3))
    Nl = N.reshape(-1, 3)
    Nl = np.stack([Nl[:, 0], Nl[:, 2], -Nl[:, 1]], 1)
    Nl /= np.maximum(np.linalg.norm(Nl, axis=1, keepdims=True), 1e-9)
    prm = gb["Prm"].reshape(-1, 3)
    o = VP.Out(size * size)
    for k, fn in PAINTERS.items():
        idx = np.nonzero(flat & (pid == k))[0]
        if len(idx):
            fn(o, idx, Pl[idx], Nl[idx], prm[idx])
    C.log("painted", round(time.time() - t0, 1))
    ao = pk.bake_ao(obj, size, distance=0.035, samples=40)
    C.log("ao", round(time.time() - t0, 1))
    du, dv = pk.texel_world(P, cover)
    albedo = pk.dilate(o.albedo.reshape(size, size, 3), cover)
    normal = pk.height_normal(o.height.reshape(size, size), du, dv)
    emit = pk.dilate(o.emit.reshape(size, size, 3), cover)
    rough = o.rough.reshape(size, size)
    fam = o.fam.reshape(size, size, 2)
    ao_floor = np.where((pid == P_HEAD).reshape(size, size), 0.6, 0.3)
    mask = np.dstack([np.clip(ao_floor + (1 - ao_floor) * ao, 0, 1), 1 - rough, fam[..., 0], fam[..., 1]])
    mask = pk.dilate(mask, cover)
    os.makedirs(out_dir, exist_ok=True)
    maps = {"Albedo": (albedo, True), "Normal": (normal, False), "Emission": (emit, True), "Mask": (mask, False)}
    paths = {}
    for key, (arr, srgb) in maps.items():
        paths[key.lower()] = pk.save_png(arr, os.path.join(out_dir, f"{prefix}_{key}.png"), srgb)
    if low_size:
        low_dir = os.path.join(out_dir, "Low")
        os.makedirs(low_dir, exist_ok=True)
        f = size // low_size
        for key, (arr, srgb) in maps.items():
            pk.save_png(pk.downsample(arr, f, normal=key == "Normal"), os.path.join(low_dir, f"{prefix}_{key}.png"), srgb)
    C.log("maps", round(time.time() - t0, 1))
    return paths


# -------------------------------------------------------------------------------------------------------------
# Rig
# -------------------------------------------------------------------------------------------------------------

def head_game(local):
    return H.HEAD_ORIGIN[1] + local * H.HEAD_SCALE


EYE_GAME = {s: H.eye_c(s) * H.HEAD_SCALE + np.array(H.HEAD_ORIGIN) for s in (1, -1)}
JAW_LOCAL = np.array((0.0, -0.013, -0.012))
JAW_HINGE = C.G(0, head_game(JAW_LOCAL[1]), JAW_LOCAL[2] * H.HEAD_SCALE)
CHIN = C.G(0, head_game(-0.073), 0.06 * H.HEAD_SCALE)


def bones():
    return [tk.Bone("Root", (0, 0, 0), UPV, length=0.1),
            tk.Bone("Spine", C.G(0, 0.98, 0), UPV, "Root", length=0.1),
            tk.Bone("Chest", C.G(0, 1.12, 0), UPV, "Spine", length=0.1),
            tk.Bone("Clavicle_L", C.G(0.02, 1.235, -0.012), XAX, "Chest", length=0.12),
            tk.Bone("Clavicle_R", C.G(-0.02, 1.235, -0.012), -XAX, "Chest", length=0.12),
            tk.Bone("Neck", C.G(0, 1.27, -0.02), UPV, "Chest", length=0.08),
            tk.Bone("Head", C.G(0, head_game(-0.058), -0.016), UPV, "Neck", length=0.12),
            tk.Bone("Jaw", JAW_HINGE, CHIN - JAW_HINGE, "Head", length=0.08),
            tk.Bone("Lid_L", C.G(*EYE_GAME[1]), Vector((0, -1, 0)), "Head", length=0.02),
            tk.Bone("Lid_R", C.G(*EYE_GAME[-1]), Vector((0, -1, 0)), "Head", length=0.02)]


def lid_columns():
    """Per upper-lid column: the rotation (radians, about the eye's x axis) that brings it onto the lower lid."""
    h, up, lo = H.lid_curves(0.0)
    hr, ur, lr = np.radians(h), np.radians(up), np.radians(lo)
    pitch_u = np.arctan2(np.sin(ur), np.cos(hr) * np.cos(ur))
    pitch_l = np.arctan2(np.sin(lr), np.cos(hr) * np.cos(lr))
    return np.clip(pitch_u - pitch_l, 0, None)


BLINK_COLUMNS = lid_columns()
BLINK = float(BLINK_COLUMNS.max())
LID_FALLOFF = (1.0, 0.82, 0.42, 0.1)        # rim, ring 1, ring 2, ring 3 (margin and tuck follow the rim)


def lid_weights(hm):
    """{side: {vertex index in hm: weight}} for the upper lids (and the lashes, by rim column)."""
    out = {}
    col_w = BLINK_COLUMNS / BLINK
    for side in ("L", "R"):
        w = {}
        rings = hm.eye_rings[side]
        margin, tuck = hm.eye_margin[side]
        for k, fall in enumerate(LID_FALLOFF):
            for c in range(H.EYE_N):
                w[rings[k][c]] = col_w[c] * fall
        for c in range(H.EYE_N):
            w[margin[c]] = col_w[c]
            w[tuck[c]] = col_w[c]
        out[side] = w
    return out


def jaw_weight(p, tag):
    """Lower face by position: a hard split across the lips, fading over the cheeks, towards the ears and down the
    throat. The mouth bag's lower half follows the jaw."""
    x, y, z = p[:, 0], p[:, 1], p[:, 2]
    ax = np.abs(x)
    t = np.clip(ax / H.MOUTH_W, 0, 1)
    slit = H.MOUTH_Y + 0.0009 * t * t
    dz = slit - y
    near = (ax < H.MOUTH_W + 0.003) & (z > 0.068)
    w = np.where(near, (dz > 0).astype(float), pk.sstep(-0.004, 0.016, dz))
    w = np.where((tag >= 3) & (dz > 0.0002), 1.0, np.where(tag >= 3, 0.0, w))
    w *= pk.sstep(-0.035, 0.03, z)
    w *= 1 - 0.65 * pk.sstep(0.042, 0.064, ax) * pk.sstep(-0.01, 0.03, dz)
    w *= 1 - pk.sstep(-0.098, -0.125, y)
    return np.clip(w, 0, 1)


def vertex_weights(asm, hm):
    """{bone: (n,) weights} for the assembled object."""
    me = asm.obj.data
    n = len(me.vertices)
    co = pk.get_verts(asm.obj)
    g = blender_to_game(co)
    local = H.from_blender(co)
    W = {b.name: np.zeros(n) for b in bones()}
    rg = ranges(asm)

    def sl(name):
        a, b = rg[name]
        return slice(a, b)
    # Head mesh.
    hs = sl("Head")
    lp = local[hs]
    head_share = pk.sstep(-0.165, -0.095, lp[:, 1])
    jw = jaw_weight(lp, hm.tag)
    W["Jaw"][hs] = jw * head_share
    W["Head"][hs] = (1 - jw) * head_share
    W["Neck"][hs] = 1 - head_share
    lids = lid_weights(hm)
    base = rg["Head"][0]
    for side, wmap in lids.items():
        bone = "Lid_" + side
        for i, w in wmap.items():
            vi = base + i
            rest = 1 - w
            W[bone][vi] = w
            W["Head"][vi] *= rest
            W["Jaw"][vi] *= rest
            W["Neck"][vi] *= rest
    # Lashes follow their rim columns.
    ls = rg["Lashes"][0]
    k = 0
    for side in ("L", "R"):
        for c in range(H.EYE_N):
            w = lids[side][hm.eye_rings[side][0][c]]
            for j in range(3):
                W["Lid_" + side][ls + k] = w
                W["Head"][ls + k] = 1 - w
                k += 1
    for name in ("Eye_L", "Eye_R", "Band", "Earpiece", "EarRing", "EarPad", "Boom", "Mic", "Pin", "PinTip"):
        W["Head"][sl(name)] = 1.0
    # Torso cloth and the things on it: Spine to Chest by height, the hem a little towards the root, the shoulders
    # towards the clavicles.
    for name in ("Coat", "Collar", "Top", "Lapel_L", "Lapel_R", "Pocket_L", "Pocket_R", "Card", "CardClip", "Buttons", "Pens",
                 "BreastPocket"):
        s_ = sl(name)
        y = g[s_, 1]
        ch = pk.sstep(0.98, 1.13, y)
        root = 0.45 * pk.sstep(0.78, 0.52, y)
        W["Chest"][s_] = ch
        W["Spine"][s_] = (1 - ch) * (1 - root)
        W["Root"][s_] = (1 - ch) * root
        if name in ("Coat", "Collar"):
            for side, sx in (("L", 1), ("R", -1)):
                cl = 0.55 * pk.sstep(0.105, 0.15, g[s_, 0] * sx) * pk.sstep(1.13, 1.21, y)
                W["Clavicle_" + side][s_] += cl * W["Chest"][s_]
                W["Chest"][s_] *= 1 - cl
        if name in ("Top", "Collar"):
            nk = pk.sstep(1.285, 1.315, y) * (0.75 if name == "Top" else 0.3)
            W["Neck"][s_] = nk
            for b_ in ("Chest", "Spine", "Root"):
                W[b_][s_] *= 1 - nk
    for side in ("L", "R"):
        s_ = sl("Sleeve_" + side)
        t = asm.parts[[p.name for p in asm.parts].index("Sleeve_" + side)].params[:, 0]
        arm = 1 - 0.5 * (1 - pk.sstep(0.02, 0.12, t))
        spine = 0.75 * pk.sstep(0.62, 1.0, t)
        W["Clavicle_" + side][s_] = arm * (1 - spine)
        W["Chest"][s_] = (1 - arm) * (1 - spine)
        W["Spine"][s_] = spine
        for name in ("Trousers_" + side, "Boot_" + side):
            W["Root"][sl(name)] = 1.0
    total = sum(W.values())
    for k_ in W:
        W[k_] = W[k_] / np.maximum(total, 1e-9)
    return W


def apply_weights(obj, arm, W):
    obj.vertex_groups.clear()
    for name, w in W.items():
        vg = obj.vertex_groups.new(name=name)
        idx = np.nonzero(w > 0.004)[0]
        # Group by rounded weight so the add() calls stay few.
        q = np.round(w[idx], 3)
        for val in np.unique(q):
            vg.add(idx[q == val].tolist(), float(val), "REPLACE")
    obj.parent = arm
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm


# -------------------------------------------------------------------------------------------------------------
# Shape keys (face, for portraits; computed as exact rotations and soft displacement fields)
# -------------------------------------------------------------------------------------------------------------

def rot_x(q, angle):
    c, s = np.cos(angle), np.sin(angle)
    return np.stack([q[:, 0], q[:, 1] * c - q[:, 2] * s, q[:, 1] * s + q[:, 2] * c], 1)


def shape_keys(obj, asm, hm, W):
    co = pk.get_verts(obj)
    local = H.from_blender(co)
    rg = ranges(asm)
    head = np.zeros(len(co), bool)
    for name in ("Head", "Lashes"):
        a, b = rg[name]
        head[a:b] = True
    keys = {}
    # Blink: each lid vertex turns about its eye's x axis by its weight times the full closing angle.
    blink = local.copy()
    for side, s in (("L", 1), ("R", -1)):
        w = W["Lid_" + side]
        m = w > 0
        e = H.eye_c(s)
        blink[m] = e + rot_x(local[m] - e, w[m] * BLINK)
    # The lower lids rise a little into the blink.
    for s in (1, -1):
        e = H.eye_c(s)
        q = local - e
        lower = head & (q[:, 1] < -0.001) & (np.linalg.norm(q, axis=1) < H.LID + 0.006) & (q[:, 2] > -0.004)
        blink[lower] = e + rot_x(q[lower], -math.radians(4) * (1 - pk.sstep(H.LID, H.LID + 0.006, np.linalg.norm(q[lower], axis=1))))
    keys["Blink"] = blink
    # JawOpen: the jaw weights turned about the hinge.
    jaw = local.copy()
    wj = W["Jaw"]
    m = wj > 0
    jaw[m] = JAW_LOCAL + rot_x(local[m] - JAW_LOCAL, wj[m] * math.radians(13))
    keys["JawOpen"] = jaw
    # Smile and Frown: soft displacement fields on the face.
    p = local
    d = np.zeros_like(p)
    for s in (1, -1):
        corner = (s * H.MOUTH_W, H.MOUTH_Y + 0.0009, 0.075)
        d += pk.gauss(p, corner, (0.009, 0.008, 0.012))[:, None] * np.array((s * 0.0017, 0.0034, -0.0013))
        d += pk.gauss(p, (s * 0.037, -0.022, 0.058), (0.015, 0.012, 0.02))[:, None] * np.array((s * 0.0004, 0.0019, 0.0009))
        d += pk.gauss(p, H.eye_c(s) + np.array((0, -0.012, 0.006)), (0.011, 0.005, 0.01))[:, None] * np.array((0, 0.0009, 0.0002))
    d += pk.gauss(p, (0, -0.0395, 0.083), (0.012, 0.004, 0.01))[:, None] * np.array((0, 0.0006, 0))
    keys["Smile"] = local + d * head[:, None]
    d = np.zeros_like(p)
    for s in (1, -1):
        d += pk.gauss(p, (s * 0.013, 0.02, 0.078), (0.01, 0.008, 0.015))[:, None] * np.array((-s * 0.0012, -0.0022, 0.0004))
        d += pk.gauss(p, (s * H.MOUTH_W, H.MOUTH_Y, 0.075), (0.008, 0.008, 0.012))[:, None] * np.array((0, -0.0022, -0.0004))
    d += pk.gauss(p, (0, -0.0525, 0.082), (0.01, 0.006, 0.008))[:, None] * np.array((0, 0.0008, 0.0008))
    keys["Frown"] = local + d * head[:, None]
    obj.shape_key_add(name="Basis", from_mix=False)
    for name, pos in keys.items():
        kb = obj.shape_key_add(name=name, from_mix=False)
        new = np.where(head[:, None], H.to_blender(pos), co)
        kb.data.foreach_set("co", new.astype(np.float32).ravel())
    for kb in obj.data.shape_keys.key_blocks:
        kb.value = 0.0
    return list(keys)


# -------------------------------------------------------------------------------------------------------------
# Clips (bk.clip: rotations in armature space about each bone's head)
# -------------------------------------------------------------------------------------------------------------

def blink_curve(f, start, length=7):
    u = (f - start) / length
    if u <= 0 or u >= 1:
        return 0.0
    return math.sin(math.pi * u) ** 0.6


def lids(k):
    return {"Lid_L": {"rot": (XAX, BLINK * k)}, "Lid_R": {"rot": (XAX, BLINK * k)}}


def breath(t, cycles, amount=1.0):
    b = 0.5 - 0.5 * math.cos(2 * math.pi * cycles * t)
    return {"Chest": {"loc": Vector((0, 0, 0.0035 * b * amount)), "rot": (XAX, -0.012 * b * amount)},
            "Clavicle_L": {"rot": (Vector((0, 1, 0)), -0.012 * b * amount)},
            "Clavicle_R": {"rot": (Vector((0, 1, 0)), 0.012 * b * amount)},
            "Neck": {"rot": (XAX, 0.008 * b * amount)}}


def idle(t, f):
    spec = breath(t, 2)
    spec["Head"] = {"rot": (Vector((0.15, 0.3, 1.0)).normalized(), 0.035 * math.sin(2 * math.pi * t))}
    spec.update(lids(blink_curve(f, 70)))
    return spec


SYLL = [0.0, 0.8, 0.35, 1.0, 0.2, 0.7, 0.0, 0.0, 0.9, 0.5, 0.85, 0.3, 0.6, 0.0, 0.75, 0.25, 1.0, 0.4, 0.0, 0.0]
JAW_OPEN = math.radians(8.0)


def talk(t, f):
    spec = breath(t, 1, 0.7)
    x = t * len(SYLL)
    i = int(x) % len(SYLL)
    u = x - int(x)
    v = SYLL[i] + (SYLL[(i + 1) % len(SYLL)] - SYLL[i]) * (u * u * (3 - 2 * u))
    nod = 0.05 * math.sin(2 * math.pi * 2 * t) * (0.5 + 0.5 * math.sin(2 * math.pi * t))
    spec["Head"] = {"rot": (XAX, nod + 0.015 * v)}
    spec["Jaw"] = {"rot": (XAX, JAW_OPEN * v)}
    spec.update(lids(blink_curve(f, 44)))
    return spec


def blink(t, f):
    return lids(blink_curve(f, 0, 8))


CLIPS = {"Idle": (120, idle, True), "Talk": (90, talk, True), "Blink": (8, blink, False)}


# -------------------------------------------------------------------------------------------------------------
# Export
# -------------------------------------------------------------------------------------------------------------

def export_fbx(path, objects):
    """common.export_fbx's settings, but without evaluating modifiers so the shape keys survive (the armature still
    exports as the skin)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in bpy.context.view_layer.objects:
        o.select_set(o in objects)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=False,
                             use_mesh_modifiers=False, mesh_smooth_type="FACE", use_tspace=True, add_leaf_bones=False,
                             use_armature_deform_only=True, bake_anim=True, bake_anim_use_all_actions=True,
                             bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
                             path_mode="STRIP", embed_textures=False)
    C.log("exported", path)


def part_density(obj, asm, pids):
    """World metres per UV unit over the faces of the given part ids (for the detail-map tiling)."""
    me = obj.data
    pid_attr = np.zeros(len(me.vertices))
    me.attributes["Pid"].data.foreach_get("value", pid_attr)
    me.calc_loop_triangles()
    uv = np.zeros((len(me.loops), 2))
    me.uv_layers.active.data.foreach_get("uv", uv.ravel())
    co = pk.get_verts(obj)
    a3 = a2 = 0.0
    for t in me.loop_triangles:
        if int(round(pid_attr[t.vertices[0]])) not in pids:
            continue
        p = co[list(t.vertices)]
        q = uv[list(t.loops)]
        a3 += 0.5 * np.linalg.norm(np.cross(p[1] - p[0], p[2] - p[0]))
        a2 += 0.5 * abs((q[1, 0] - q[0, 0]) * (q[2, 1] - q[0, 1]) - (q[2, 0] - q[0, 0]) * (q[1, 1] - q[0, 1]))
    return math.sqrt(a3 / max(a2, 1e-12))


def report(obj, asm, arm, keys, exports, densities):
    tris = C.tri_count(obj)
    weave_wpu, ceramic_wpu = densities
    data = {
        "asset": NAME, "game_tris": tris, "lod1": "none: portrait and cutscene only (Unity's story builder uses no LOD1)",
        "bones": [b.name for b in arm.data.bones], "texture_size": TEX, "low_texture_size": LOW_TEX,
        "maps": ["Albedo", "Normal", "Emission", "Mask"],
        "uv": dict(pk.uv_stats(obj), face_density=FACE_WEIGHT, unwrap="LSCM head (face, neck, hair islands), metric UVs elsewhere"),
        "detail": {"weave": {"map": "Detail_Weave_Normal.png", "tiling": C.detail_tiling(weave_wpu, "weave", 0.6), "mask": "B"},
                   "ceramic": {"map": "Detail_Ceramic_Normal.png", "tiling": C.detail_tiling(ceramic_wpu, "ceramic", 0.6), "mask": "A"}},
        "clips": {n: fr for n, (fr, fn, lp) in CLIPS.items()}, "events": {"Idle": {"blink": 70}, "Talk": {"blink": 44}},
        "blink": "Lid_L/Lid_R rotate about +X by %.0f degrees to close (weights fall off from the lid rim)" % math.degrees(BLINK),
        "jaw": "Jaw rotates about +X up to %.0f degrees in Talk; the mouth is a real slit with a mouth bag" % math.degrees(JAW_OPEN),
        "shape_keys": keys, "shape_keys_note": "face only; Unity imports them once importBlendShapes is on (off today)",
        "figure": "full body (coat to the knee, hands in pockets, trousers, boots); feet on y 0",
        "portrait": {"target": [0, 1.4, 0.0], "distance": 1.45, "azimuth": -28, "elevation": 5, "lens_mm": 70,
                     "note": "game space (x, up, forward); eyes at y 1.44 as before, so the round-16 bust framing holds"},
    }
    path = os.path.join(exports, NAME + "_report.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, default=float)
    C.log("report", json.dumps(data, default=float))


# -------------------------------------------------------------------------------------------------------------
# Build
# -------------------------------------------------------------------------------------------------------------

def build(args):
    t0 = time.time()
    exports = os.path.join(C.EXPORTS, NAME)
    if args.get("stage"):
        C.EXPORTS = os.path.join(args["stage"], "Exports")
        C.BLEND = os.path.join(args["stage"], "Blend")
        exports = os.path.join(C.EXPORTS, NAME)
    size = int(args.get("size", TEX))
    hm = H.build_head()
    asm, obj = assemble(hm)
    C.log("assembled", len(obj.data.vertices), "verts", C.tri_count(obj), "tris", round(time.time() - t0, 1))
    paths = paint_maps(obj, size, exports, NAME, low_size=size // 2)
    densities = (part_density(obj, asm, {P_COAT, P_SLEEVE, P_COLLAR, P_LAPEL, P_POCKET}),
                 part_density(obj, asm, {P_CARD, P_METAL, P_BUTTON}))
    for a in ("GPos", "GNrm", "Pid", "Prm"):
        if a in obj.data.attributes:
            obj.data.attributes.remove(obj.data.attributes[a])
    arm = tk.build_armature(NAME + "_Rig", bones())
    W = vertex_weights(asm, hm)
    keys = shape_keys(obj, asm, hm, W)
    apply_weights(obj, arm, W)
    obj.data.materials.clear()
    obj.data.materials.append(C.game_material(NAME + "_Preview", paths, emission_strength=2.2, specular=0.35))
    acts = {n: bk.clip(arm, n, fr, fn, loop=lp) for n, (fr, fn, lp) in CLIPS.items()}
    bk.rest_pose(arm)
    export_fbx(os.path.join(exports, NAME + ".fbx"), [arm, obj])
    report(obj, asm, arm, keys, exports, densities)
    C.save_blend(NAME)
    C.log("built in", round(time.time() - t0, 1))
    return obj, arm, acts, paths


# -------------------------------------------------------------------------------------------------------------
# Previews
# -------------------------------------------------------------------------------------------------------------

def set_key(obj, name, value):
    for kb in obj.data.shape_keys.key_blocks[1:]:
        kb.value = value if kb.name == name else 0.0


def soft_shadows(rig):
    """Filtered, jittered shadows: Eevee's raw shadow maps speckle in the face's small hollows."""
    eevee = bpy.context.scene.eevee
    eevee.shadow_ray_count = 3
    eevee.shadow_step_count = 12
    for o in rig:
        o.data.shadow_filter_radius = 3.0
        o.data.use_shadow_jitter = True
        if o.data.type == "SUN":
            o.data.angle = math.radians(10)


def previews(obj, arm, acts, out, paths):
    C.preview_world((0.05, 0.055, 0.065))
    rig = C.light_rig(0.6)
    soft_shadows(rig)
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    shots = [("voss_portrait", C.G(0, 1.4, 0), 1.45, -28, 5, 70, 1024, 1024),
             ("voss_portrait_front", C.G(0, 1.4, 0), 1.45, 0, 4, 70, 1024, 1024),
             ("voss_face", C.G(0, 1.44, 0.04), 0.5, -22, 4, 70, 1024, 1024),
             ("voss_face_front", C.G(0, 1.44, 0.04), 0.5, 0, 2, 70, 1024, 1024),
             ("voss_profile", C.G(0, 1.43, 0.0), 0.8, -88, 3, 70, 1024, 1024),
             ("voss_back", C.G(0, 1.42, -0.02), 0.9, 150, 12, 70, 1024, 1024),
             ("voss_full", C.G(0, 0.8, 0), 2.9, -20, 6, 50, 800, 1200),
             ("voss_full_front", C.G(0, 0.8, 0), 2.9, 0, 4, 50, 800, 1200),
             ("voss_full_back", C.G(0, 0.8, 0), 2.9, 160, 8, 50, 800, 1200)]
    for name, target, dist, az, el, lens, w, h in shots:
        cam = C.closeup_camera(target, dist, azimuth=az, elevation=el, lens=lens)
        C.render(cam, os.path.join(out, name + ".png"), w, h)
        bpy.data.objects.remove(cam)
    # Dialogue-box size: rendered at 2x and box-filtered to 130 x 170, plus a 4x nearest blow-up to look at.
    cam = C.closeup_camera(C.G(0, 1.39, 0), 1.25, azimuth=-14, elevation=4, lens=70)
    small = os.path.join(out, "voss_dialogue_2x.png")
    C.render(cam, small, 260, 340)
    bpy.data.objects.remove(cam)
    arr = C.image_to_array(small)
    tiny = arr.reshape(170, 2, 130, 2, 4).mean(axis=(1, 3))
    C.array_to_image(tiny, os.path.join(out, "voss_dialogue_130x170.png"))
    big = np.repeat(np.repeat(tiny, 4, axis=0), 4, axis=1)
    C.array_to_image(big, os.path.join(out, "voss_dialogue_130x170_x4.png"))
    # Expressions (shape keys) and the clips.
    cam = C.closeup_camera(C.G(0, 1.425, 0.04), 0.5, azimuth=-18, elevation=3, lens=70)
    arm.animation_data.action = None
    bk.rest_pose(arm)
    tmp = []
    for k in ["", "Smile", "Frown", "JawOpen", "Blink"]:
        set_key(obj, k, 1.0)
        p = os.path.join(out, f"_key_{k or 'neutral'}.png")
        C.render(cam, p, 500, 500, samples=24)
        tmp.append(p)
    set_key(obj, "", 0.0)
    C.contact_sheet(tmp, os.path.join(out, "voss_shape_keys.png"), 5, 400)
    for p in tmp:
        os.remove(p)
    import story_kit as sk
    sk.strip(arm, acts["Talk"], cam, (0, 5, 10, 14, 22), os.path.join(out, "voss_talk_strip.png"), width=400, height=400)
    eye = C.closeup_camera(C.G(-0.032, 1.445, 0.07), 0.2, azimuth=-20, elevation=3, lens=70)
    sk.strip(arm, acts["Blink"], eye, (0, 2, 3, 4, 6), os.path.join(out, "voss_blink_strip.png"), width=400, height=400)
    bpy.data.objects.remove(cam)
    bpy.data.objects.remove(eye)
    arm.animation_data.action = None
    bk.rest_pose(arm)
    C.uv_layout(obj, os.path.join(out, "voss_uv.png"), 1024, paths["albedo"])
    for o in rig:
        bpy.data.objects.remove(o)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    obj, arm, acts, paths = build(args)
    if not args.get("norender"):
        previews(obj, arm, acts, out, paths)


if __name__ == "__main__":
    main()
