"""Dr. Elena Voss, chief scientist: round 20, built on MPFB 2 (MakeHuman for Blender). REQUIRES the MPFB extension
and its CC0 asset packs makehuman_system_assets, hair01 and faceunits01 (install steps: Docs/COMMANDS.md, section
"Blender: MPFB 2"). Replaces build_voss2.py's exports (same file names, mesh `Voss`, armature `Voss_Rig`, the same 10
bones, clips and shape keys); see ArtSource/DESIGN_NOTES.md, round 20.

Pipeline:
1. voss3_human.capture(): the MPFB human (adult woman ~40, natural proportions, mild stylisation through MakeHuman
   targets), skin/eyes/eyebrows/eyelashes/teeth/hair-bun/shoes assets, the default rig; the expressions Blink, JawOpen,
   Smile, Frown from the faceunits01 targets (every proxy refitted to them); posed with her hands in the coat pockets;
   scaled so the eyes sit on y 1.44. Cached as .npz (--cache) so the later stages iterate without MPFB.
2. voss3_cloth: the coat, collar, lapels, sleeves, pockets, mock-neck top, trousers and gear, measured off her body.
3. One mesh (portrait_kit.Assembly): the body is culled to what shows (head, ears, neck), the MPFB parts keep their own
   UV islands, everything packed into one 2048 atlas (face islands at 2.4x density). voss3_paint bakes the atlas:
   MPFB's skin/eye/hair/teeth/shoe textures resampled (the skin simplified, brows painted in from the brow cards),
   the clothes painted procedurally as in round 19. One material.
4. Voss_Rig (10 bones) with weights mapped from MPFB's default rig, the lids from the blink target; clips Idle 120,
   Talk 90, Blink 8; shape keys Blink, Smile, JawOpen, Frown; FBX + maps + report.

Run: blender --background --factory-startup --python Tools/Blender/build_voss3.py -- [--preview <dir>] [--norender]
     [--stage <dir>] [--size 2048] [--cache <file.npz>] [--recapture] [--geo]
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
from mathutils.kdtree import KDTree  # noqa: E402

import base_kit as bk  # noqa: E402
import common as C  # noqa: E402
import portrait_kit as pk  # noqa: E402
import tower_kit as tk  # noqa: E402
import voss3_cloth as VC  # noqa: E402
import voss3_human as VH  # noqa: E402

NAME = "Voss"
TEX, LOW_TEX = 2048, 1024
XAX = Vector((1, 0, 0))
UPV = Vector((0, 0, 1))
FACE_WEIGHT = 2.4

# Part ids (painters and rig regions). 1-9 MPFB parts, 10+ clothes and gear (as round 19).
P_SKIN, P_EYE, P_LASH, P_HAIR, P_TEETH, P_SHOE = 1, 2, 3, 4, 5, 6
P_COAT, P_SLEEVE, P_TOP, P_TROUSERS, P_COLLAR, P_LAPEL, P_POCKET = 10, 11, 12, 13, 15, 16, 17
P_METAL, P_CARD, P_GLOW, P_BUTTON, P_PEN = 20, 21, 22, 23, 24
TEX_ID = {P_SKIN: 1, P_EYE: 2, P_LASH: 3, P_HAIR: 4, P_TEETH: 5, P_SHOE: 6}

DECIMATE = {"teeth": 0.12, "shoes": 0.4, "hair": 0.68}
EXPRESSIONS = list(VH.EXPRESSIONS)
REST_SMILE = 0.3


def game_to_blender(p):
    p = np.asarray(p, float)
    return np.stack([p[..., 0], -p[..., 2], p[..., 1]], -1)


def blender_to_game(b):
    b = np.asarray(b, float)
    return np.stack([b[..., 0], b[..., 2], -b[..., 1]], -1)


# -------------------------------------------------------------------------------------------------------------
# Mesh data helpers
# -------------------------------------------------------------------------------------------------------------

class Mesh:
    """A part in Blender space with per-vertex weights ({bone: (n,)}), deltas ({expr: (n, 3)}), per-corner source
    UVs and per-vertex params."""

    def __init__(self, co, faces, cuv, weights, deltas, params=None):
        self.co = np.asarray(co, float)
        self.faces = [tuple(f) for f in faces]
        self.cuv = cuv
        self.weights = weights
        self.deltas = deltas
        self.params = params if params is not None else np.zeros((len(self.co), 3))

    def subset(self, keep_v):
        """Keeps the faces whose vertices are all kept; drops unused vertices."""
        keep_v = np.asarray(keep_v, bool)
        fi = [i for i, f in enumerate(self.faces) if all(keep_v[j] for j in f)]
        used = np.zeros(len(self.co), bool)
        for i in fi:
            used[list(self.faces[i])] = True
        remap = -np.ones(len(self.co), int)
        remap[used] = np.arange(used.sum())
        faces = [tuple(remap[j] for j in self.faces[i]) for i in fi]
        cuv = [self.cuv[i] for i in fi] if self.cuv is not None else None
        return Mesh(self.co[used], faces, cuv, {k: w[used] for k, w in self.weights.items()},
                    {k: d[used] for k, d in self.deltas.items()}, self.params[used])


def from_capture(cap):
    W = dict(cap.weights)
    for b in VH.GAME_BONES:
        W.setdefault(b, np.zeros(len(cap.co)))
    return Mesh(cap.co, cap.faces(), cap.corner_uv(), W, dict(cap.deltas))


def components(faces, n):
    parent = np.arange(n)

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    for f in faces:
        r = find(f[0])
        for v in f[1:]:
            rv = find(v)
            if rv != r:
                parent[rv] = r
    return np.array([find(i) for i in range(n)])


def decimate(mesh, ratio, name):
    """Collapse decimation in Blender; weights, deltas and params follow the nearest original vertex (the decimated
    parts are rigid per region: teeth, shoes, hair)."""
    obj = pk.mesh_object(name, mesh.co, mesh.faces, mesh.cuv)
    mod = obj.modifiers.new("Dec", "DECIMATE")
    mod.ratio = ratio
    C.activate(obj)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    me = obj.data
    co = pk.get_verts(obj)
    faces = [tuple(p.vertices) for p in me.polygons]
    uv = np.zeros((len(me.loops), 2))
    me.uv_layers[0].data.foreach_get("uv", uv.ravel())
    cuv = [[tuple(uv[li]) for li in p.loop_indices] for p in me.polygons]
    kd = KDTree(len(mesh.co))
    for i, p in enumerate(mesh.co):
        kd.insert(p, i)
    kd.balance()
    near = np.array([kd.find(p)[1] for p in co])
    bpy.data.objects.remove(obj)
    return Mesh(co, faces, cuv, {k: w[near] for k, w in mesh.weights.items()},
                {k: d[near] for k, d in mesh.deltas.items()}, mesh.params[near])


def piece_mesh(piece, weights_fn=None):
    co = game_to_blender(piece.verts)
    n = len(co)
    W = {b: np.zeros(n) for b in VH.GAME_BONES}
    D = {e: np.zeros((n, 3)) for e in EXPRESSIONS}
    m = Mesh(co, piece.faces, piece.uv, W, D, piece.params if piece.params is not None else np.zeros((n, 3)))
    m.metric_uv = True
    return m


# -------------------------------------------------------------------------------------------------------------
# Body: masks, culling, figure
# -------------------------------------------------------------------------------------------------------------

def bone_points(info):
    b = info["bones"]
    out = {}
    for src, dst in (("shoulder.L", "shoulder_L"), ("shoulder.R", "shoulder_R"), ("elbow.L", "elbow_L"), ("elbow.R", "elbow_R"),
                     ("wrist.L", "wrist_L"), ("wrist.R", "wrist_R"), ("clavicle.L", "clavicle_L"), ("clavicle.R", "clavicle_R"),
                     ("neck01", "neck01"), ("jaw", "jaw"), ("jaw_tail", "jaw_tail"), ("eye.L", "eye_L"), ("eye.R", "eye_R"),
                     ("head", "head"), ("spine01", "spine01"), ("spine03", "spine03")):
        out[dst] = blender_to_game(b[src])
    return out


def body_masks(cap, g, shoes_g):
    W, G = cap.weights, cap.groups
    head = W["Head"] + W["Jaw"]
    arm = (G["_arm"] + G["_hand"]) > 0.35
    leg = (G["_leg"] + G["_foot"]) > 0.5
    return {"torso": (~arm) & (head < 0.5) & (g[:, 1] > 0.25),
            "neck": (W["Neck"] > 0.35) & (head < 0.6),
            "arm_L": arm & (g[:, 0] > 0), "arm_R": arm & (g[:, 0] < 0),
            "leg_L": leg & (g[:, 0] > 0), "leg_R": leg & (g[:, 0] < 0),
            "shoe": shoes_g}


def cull_body(mesh, cap, g, fig, neck_top, hair):
    """What shows of the skin: head, ears and the neck above the mock collar (with a margin under it); the scalp under
    the hair goes (kept one ring wider than covered, so no gaps at the hairline)."""
    W = cap.weights
    G = cap.groups
    arm = (G["_arm"] + G["_hand"]) > 0.2
    f = fig.neck
    th = np.arctan2(g[:, 0], g[:, 2] - np.array([f.cz(y) for y in g[:, 1]]))
    top = neck_top + 0.016 * (0.5 - 0.5 * np.cos(th))
    keep = (g[:, 1] > top - 0.022) & ~arm
    # Scalp under the hair: covered if a ray from the head centre through the vertex meets the hair within 2.5 cm.
    from mathutils.bvhtree import BVHTree
    bvh = BVHTree.FromPolygons([tuple(map(float, p)) for p in hair.co], hair.faces)
    centre = Vector(game_to_blender(fig.b["eye_L"] * 0.5 + fig.b["eye_R"] * 0.5 + np.array((0, 0.02, -0.07))))
    covered = np.zeros(len(g), bool)
    for i in np.nonzero(keep & ((W["Head"] + W["Jaw"]) > 0.5))[0]:
        p = Vector(mesh.co[i])
        d = (p - centre).normalized()
        hit = bvh.ray_cast(p, d, 0.025)
        covered[i] = hit[0] is not None
    # Erode the covered set twice (a vertex next to an uncovered one stays).
    nb = [[] for _ in range(len(g))]
    for fc in mesh.faces:
        for a in fc:
            nb[a].extend(fc)
    for _ in range(2):
        c2 = covered.copy()
        for i in np.nonzero(covered)[0]:
            if not covered[nb[i]].all():
                c2[i] = False
        covered = c2
    keep &= ~covered
    C.log("body cull: kept", int(keep.sum()), "of", len(g), "scalp culled", int(covered.sum()))
    return mesh.subset(keep)


def lid_weights(mesh, eye_c):
    """Upper-lid weights from the Blink delta: the rotation about the eye's x axis that carries each vertex to its
    blinked position, over the largest such angle."""
    d = mesh.deltas["Blink"]
    angles = np.zeros(len(mesh.co))
    side = np.zeros(len(mesh.co), int)
    for s, key in ((1, "L"), (-1, "R")):
        e = np.asarray(eye_c[key])
        q = mesh.co - e
        near = (np.linalg.norm(q, axis=1) < 0.03) & (q[:, 0] * s > -0.02) & (np.linalg.norm(d, axis=1) > 2e-4) & (q[:, 2] > -0.004)
        a0 = np.arctan2(q[:, 2], -q[:, 1])
        q1 = q + d
        a1 = np.arctan2(q1[:, 2], -q1[:, 1])
        ang = np.clip(a0 - a1, 0, None)
        angles = np.where(near, ang, angles)
        side = np.where(near, s, side)
    return angles, side


# -------------------------------------------------------------------------------------------------------------
# Parts
# -------------------------------------------------------------------------------------------------------------

def shoe_part(cap):
    """shoes04 without its sock: the trousers cover everything above the boot's collar."""
    m = from_capture(cap)
    return m.subset(m.co[:, 2] < 0.1)


def shorten_lashes(lashes, body, amount=0.72):
    """The opaque game shader cannot fade the lash cards' tips, so every card is pulled towards its root row on the
    lid (shorter, no make-up wings); the root row stays where MPFB fitted it."""
    kd = KDTree(len(body.co))
    for i, p in enumerate(body.co):
        kd.insert(p, i)
    kd.balance()
    d = np.array([kd.find(p)[2] for p in lashes.co])
    root = d < 0.0012
    rk = KDTree(int(root.sum()))
    for j, i in enumerate(np.nonzero(root)[0]):
        rk.insert(lashes.co[i], j)
    rk.balance()
    for i in np.nonzero(~root)[0]:
        q = np.array(rk.find(lashes.co[i])[0])
        lashes.co[i] = lashes.co[i] + (q - lashes.co[i]) * amount


def mpfb_parts(caps, info):
    """{key: Mesh} for the MPFB parts (Blender space), plus the brow cards (painted, not exported) and the figure."""
    g_body = blender_to_game(caps["body"].co)
    shoes_g = blender_to_game(caps["shoes"].co)
    fig = VC.Figure(g_body, body_masks(caps["body"], g_body, shoes_g), bone_points(info))
    body = from_capture(caps["body"])
    G = caps["body"].groups
    body.params = np.stack([G.get("lips", np.zeros(len(g_body))), G.get("ears", np.zeros(len(g_body))),
                            G.get("scalp", np.zeros(len(g_body)))], 1)
    hair = from_capture(caps["hair"])
    top_piece, neck_top = VC.mock_top(fig)
    fig.neck_top = neck_top
    body = cull_body(body, caps["body"], g_body, fig, neck_top, hair)
    # A friendly rest face: a fifth of the smile is part of the neutral shape (the Smile key adds the rest).
    body.co = body.co + REST_SMILE * body.deltas["Smile"]
    body.deltas["Smile"] = body.deltas["Smile"] * (1 - REST_SMILE)
    # Eyes: keep the eyeballs (the cornea shells are transparent in MPFB; the game shader is opaque).
    eyes = from_capture(caps["eyes"])
    comp = components(eyes.faces, len(eyes.co))
    keep = np.zeros(len(eyes.co), bool)
    for s in (1, -1):
        cs = [c for c in np.unique(comp) if np.mean(eyes.co[comp == c][:, 0]) * s > 0]
        fwd = {c: -eyes.co[comp == c][:, 1].min() for c in cs}       # Blender forward is -y
        keep |= comp == min(cs, key=lambda c: fwd[c])
    eyes = eyes.subset(keep)
    # Upper lashes only (the lower lash line is painted).
    lashes = from_capture(caps["eyelashes"])
    comp = components(lashes.faces, len(lashes.co))
    eye_z = info["bones"]["eye.L"][2]
    keep = np.zeros(len(lashes.co), bool)
    for c in np.unique(comp):
        if lashes.co[comp == c][:, 2].mean() > eye_z:
            keep |= comp == c
    lashes = lashes.subset(keep)
    shorten_lashes(lashes, body)
    # The lash cards stay out of the export: the opaque game shader shows them as solid strips (wings when long,
    # stripes on the lid when shortened). The lash line is painted along the lid rim instead (voss3_paint).
    del lashes
    parts = {"body": body, "eyes": eyes,
             "hair": decimate(hair, DECIMATE["hair"], "HairDec"),
             "teeth": decimate(from_capture(caps["teeth"]), DECIMATE["teeth"], "TeethDec"),
             "shoes": decimate(shoe_part(caps["shoes"]), DECIMATE["shoes"], "ShoesDec")}
    brows = from_capture(caps["eyebrows"])
    return parts, brows, fig, top_piece


def cloth_parts(fig, hair_g, ears, mouth_corner):
    out = []
    out.append(("Coat", P_COAT, 0.8, VC.coat_body(fig)))
    out.append(("Collar", P_COLLAR, 1.0, VC.collar(fig)))
    for side, s in (("L", 1), ("R", -1)):
        out.append(("Lapel_" + side, P_LAPEL, 1.0, VC.lapel(fig, s)))
        out.append(("Sleeve_" + side, P_SLEEVE, 0.8, VC.sleeve(fig, side)))
        out.append(("Pocket_" + side, P_POCKET, 0.8, VC.pocket(fig, side)))
        out.append(("Trousers_" + side, P_TROUSERS, 0.45, VC.trousers(fig, side)))
    btn, fig.button_ys = VC.buttons(fig)
    out.append(("BreastPocket", P_POCKET, 0.8, VC.breast_pocket(fig)))
    out.append(("Buttons", P_BUTTON, 1.0, btn))
    out.append(("Pens", P_PEN, 1.0, VC.pens(fig)))
    card, clip = VC.card(fig)
    out += [("Card", P_CARD, 2.6, card), ("CardClip", P_METAL, 1.0, clip)]
    band, ear, ring, pad, boom, mic = VC.headset(hair_g, ears, mouth_corner)
    out += [("Band", P_METAL, 1.0, band), ("Earpiece", P_METAL, 1.0, ear), ("EarRing", P_GLOW, 0.6, ring),
            ("EarPad", P_METAL, 1.0, pad), ("Boom", P_METAL, 0.6, boom), ("Mic", P_GLOW, 1.0, mic)]
    return out


def landmarks(parts, caps, info):
    """Ear centres and her right mouth corner (game space) from the body's groups and the lips."""
    cap = caps["body"]
    g = blender_to_game(cap.co)
    ears = {}
    for s, k in ((1, "L"), (-1, "R")):
        m = (cap.groups.get("ears", np.zeros(len(g))) > 0.5) & (g[:, 0] * s > 0)
        ears[k] = g[m].mean(axis=0)
    lips = cap.groups.get("lips", np.zeros(len(g))) > 0.5
    lp = g[lips]
    corner = lp[np.argmin(lp[:, 0])]
    return ears, corner


# -------------------------------------------------------------------------------------------------------------
# Cloth weights (game space rules, as round 19)
# -------------------------------------------------------------------------------------------------------------

def cloth_weights(name, mesh, fig):
    g = blender_to_game(mesh.co)
    y = g[:, 1]
    W = mesh.weights
    spine_y, chest_y = float(fig.b["spine03"][1]), float(fig.b["spine01"][1])
    if name.startswith(("Band", "Ear", "Boom", "Mic", "Pin")):
        W["Head"][:] = 1.0
        return
    if name.startswith(("Trousers",)):
        W["Root"][:] = 1.0
        return
    if name.startswith("Sleeve"):
        side = name[-1]
        t = mesh.params[:, 0]
        arm = 1 - 0.5 * (1 - pk.sstep(0.02, 0.12, t))
        spine = 0.75 * pk.sstep(0.62, 1.0, t)
        W["Clavicle_" + side][:] = arm * (1 - spine)
        W["Chest"][:] = (1 - arm) * (1 - spine)
        W["Spine"][:] = spine
        return
    ch = pk.sstep(spine_y + 0.04, chest_y - 0.035, y)
    root = 0.45 * pk.sstep(fig.hip_y - 0.05, fig.hem_y + 0.05, y)
    W["Chest"][:] = ch
    W["Spine"][:] = (1 - ch) * (1 - root)
    W["Root"][:] = (1 - ch) * root
    if name in ("Coat", "Collar"):
        for side, sx in (("L", 1), ("R", -1)):
            cl = 0.55 * pk.sstep(0.1, 0.15, g[:, 0] * sx) * pk.sstep(chest_y - 0.035, fig.collar_y - 0.06, y)
            W["Clavicle_" + side] += cl * W["Chest"]
            W["Chest"] *= 1 - cl
    if name in ("Top", "Collar"):
        nk = pk.sstep(fig.collar_y + 0.01, fig.collar_y + 0.04, y) * (0.85 if name == "Top" else 0.3)
        W["Neck"][:] = nk
        for b_ in ("Chest", "Spine", "Root", "Clavicle_L", "Clavicle_R"):
            W[b_] *= 1 - nk


# -------------------------------------------------------------------------------------------------------------
# Assembly
# -------------------------------------------------------------------------------------------------------------

def face_weights(mesh, fig):
    """Texel density per face of the skin: the face (front of the head) at FACE_WEIGHT."""
    g = blender_to_game(mesh.co)
    eye = 0.5 * (fig.b["eye_L"] + fig.b["eye_R"])
    out = []
    for f in mesh.faces:
        c = g[list(f)].mean(axis=0)
        q = c - eye + np.array((0, 0, 0.09))
        az = math.degrees(math.atan2(q[0], q[2]))
        out.append(FACE_WEIGHT if abs(az) < 70 and c[1] > fig.neck_top - 0.01 else 1.0)
    return out


def assemble(parts, cloth, fig):
    asm = pk.Assembly(NAME)
    meshes = []
    order = [("body", P_SKIN, None), ("eyes", P_EYE, 1.6), ("hair", P_HAIR, 1.0),
             ("teeth", P_TEETH, 0.5), ("shoes", P_SHOE, 0.5)]
    for key, pid, w in order:
        m = parts[key]
        fw = face_weights(m, fig) if key == "body" else None
        uv = [[(u * 0.5, v * 0.5) for u, v in c] for c in m.cuv]
        asm.add(pk.Part(key.capitalize(), m.co, m.faces, pid=pid, weight=w or 1.0, uv=uv, face_weight=fw, params=m.params))
        meshes.append((key, pid, m))
    for name, pid, w, piece in cloth:
        m = piece_mesh(piece)
        cloth_weights(name, m, fig)
        asm.add(pk.Part(name, m.co, m.faces, pid=pid, weight=w, uv=m.cuv, params=m.params))
        meshes.append((name, pid, m))
    obj = asm.build()
    # Source UVs (texture id in z) as a corner attribute for the resampling painters.
    src = []
    for key, pid, m in meshes:
        tid = TEX_ID.get(pid, 0)
        for f, c in zip(m.faces, m.cuv):
            if tid:
                src.extend([(u, v, tid) for u, v in c])
            else:
                src.extend([(0.0, 0.0, 0.0)] * len(f))
    pk.set_attr(obj, "SrcUV", np.asarray(src, float), domain="CORNER", kind="FLOAT_VECTOR")
    n = len(obj.data.vertices)
    W = {b: np.zeros(n) for b in VH.GAME_BONES}
    D = {e: np.zeros((n, 3)) for e in EXPRESSIONS}
    k = 0
    for key, pid, m in meshes:
        sl = slice(k, k + len(m.co))
        for b in W:
            W[b][sl] = m.weights[b]
        for e in D:
            D[e][sl] = m.deltas[e]
        k += len(m.co)
    return asm, obj, meshes, W, D


# -------------------------------------------------------------------------------------------------------------
# Rig, keys, clips
# -------------------------------------------------------------------------------------------------------------

def bones(info):
    b = {k: Vector(v) for k, v in info["bones"].items()}
    jaw_dir = (b["jaw_tail"] - b["jaw"]).normalized()
    return [tk.Bone("Root", (0, 0, 0), UPV, length=0.1),
            tk.Bone("Spine", b["spine03"], UPV, "Root", length=0.1),
            tk.Bone("Chest", b["spine01"], UPV, "Spine", length=0.1),
            tk.Bone("Clavicle_L", b["clavicle.L"], XAX, "Chest", length=0.12),
            tk.Bone("Clavicle_R", b["clavicle.R"], -XAX, "Chest", length=0.12),
            tk.Bone("Neck", b["neck01"], UPV, "Chest", length=0.08),
            tk.Bone("Head", b["head"], UPV, "Neck", length=0.12),
            tk.Bone("Jaw", b["jaw"], jaw_dir, "Head", length=0.08),
            tk.Bone("Lid_L", b["eye.L"], Vector((0, -1, 0)), "Head", length=0.02),
            tk.Bone("Lid_R", b["eye.R"], Vector((0, -1, 0)), "Head", length=0.02)]


BLINK = math.radians(40.0)          # replaced by the measured lid angle in rig_weights()


def rig_weights(obj, W, D, info):
    """Adds the lid bones from the Blink delta (upper lids and lashes) and normalises."""
    global BLINK
    co = pk.get_verts(obj)
    m = type("M", (), {})()
    m.co, m.deltas = co, D
    eye_c = {"L": info["bones"]["eye.L"], "R": info["bones"]["eye.R"]}
    ang, side = lid_weights(m, eye_c)
    upper = (side != 0) & (co[:, 2] > np.where(side > 0, eye_c["L"][2], eye_c["R"][2]) - 0.002)
    full = float(np.percentile(ang[upper], 97)) if upper.any() else BLINK
    BLINK = max(full, math.radians(20))
    for s, key in ((1, "L"), (-1, "R")):
        sel = upper & (side == s)
        w = np.clip(ang / BLINK, 0, 1) * sel
        rest = 1 - w
        for b in W:
            if not b.startswith("Lid"):
                W[b] *= rest
        W["Lid_" + key] = np.maximum(W["Lid_" + key], w)
    total = sum(W.values())
    total = np.where(total < 1e-9, 1, total)
    for b in W:
        W[b] = W[b] / total
    C.log("blink angle", round(math.degrees(BLINK), 1), "lid verts", int((W["Lid_L"] > 0.01).sum()), int((W["Lid_R"] > 0.01).sum()))
    return W


def apply_weights(obj, arm, W):
    obj.vertex_groups.clear()
    for name, w in W.items():
        vg = obj.vertex_groups.new(name=name)
        idx = np.nonzero(w > 0.004)[0]
        q = np.round(w[idx], 3)
        for val in np.unique(q):
            vg.add(idx[q == val].tolist(), float(val), "REPLACE")
    obj.parent = arm
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm


def shape_keys(obj, D):
    co = pk.get_verts(obj)
    obj.shape_key_add(name="Basis", from_mix=False)
    for name in ("Blink", "Smile", "JawOpen", "Frown"):
        kb = obj.shape_key_add(name=name, from_mix=False)
        kb.data.foreach_set("co", (co + D[name]).astype(np.float32).ravel())
    for kb in obj.data.shape_keys.key_blocks:
        kb.value = 0.0
    return ["Blink", "Smile", "JawOpen", "Frown"]


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
# Export and report
# -------------------------------------------------------------------------------------------------------------

def export_fbx(path, objects):
    """common.export_fbx's settings without evaluating modifiers, so the shape keys survive."""
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


def part_density(obj, pids):
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


def report(obj, arm, keys, exports, densities, info, size, parts_tris):
    weave_wpu, ceramic_wpu = densities
    data = {
        "asset": NAME, "game_tris": C.tri_count(obj),
        "lod1": "none: portrait and cutscene only (Unity's story builder uses no LOD1)",
        "bones": [b.name for b in arm.data.bones], "texture_size": size, "low_texture_size": size // 2,
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "materials": 1,
        "uv": dict(pk.uv_stats(obj), face_density=FACE_WEIGHT, unwrap="MPFB parts keep their own UV islands; clothes metric UVs"),
        "detail": {"weave": {"map": "Detail_Weave_Normal.png", "tiling": C.detail_tiling(weave_wpu, "weave", 0.6), "mask": "B"},
                   "ceramic": {"map": "Detail_Ceramic_Normal.png", "tiling": C.detail_tiling(ceramic_wpu, "ceramic", 0.6), "mask": "A"}},
        "clips": {n: fr for n, (fr, fn, lp) in CLIPS.items()}, "events": {"Idle": {"blink": 70}, "Talk": {"blink": 44}},
        "blink": "Lid_L/Lid_R rotate about +X by %.0f degrees to close (weights from the MPFB eyeBlink targets)" % math.degrees(BLINK),
        "jaw": "Jaw rotates about +X up to %.0f degrees in Talk; MPFB's jaw/lower-lip weights, teeth inside" % math.degrees(JAW_OPEN),
        "shape_keys": keys, "shape_keys_note": "face (faceunits01 targets); Unity imports them once importBlendShapes is on (off today)",
        "figure": "full body (coat to the knee, hands in pockets, trousers, boots); feet on y 0",
        "stature": round(float(info["stature"]), 3), "parts_tris": parts_tris,
        "source": "MPFB 2 (MakeHuman) base mesh, skin, eyes, eyebrows (painted in), eyelashes, teeth, hair bun "
                  "(rehmanpolanski_hair_bun_brown, hair01 pack) and shoes04; all CC0",
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

def capture(args):
    cache = args.get("cache")
    if cache and os.path.exists(cache) and not args.get("recapture"):
        C.log("capture from cache", cache)
        return VH.load_cache(cache)
    caps, info = VH.capture()
    if cache:
        VH.save_cache(cache, caps, info)
    # Start clean: only our own objects from here on.
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    return caps, info


def geo_material(obj):
    """--geo: flat colours per part id through a colour attribute (fit checks without painting)."""
    pal = {P_SKIN: (0.85, 0.65, 0.55), P_EYE: (0.9, 0.9, 0.95), P_LASH: (0.05, 0.03, 0.03), P_HAIR: (0.25, 0.13, 0.08),
           P_TEETH: (0.9, 0.88, 0.8), P_SHOE: (0.12, 0.1, 0.1), P_COAT: (0.85, 0.86, 0.88), P_SLEEVE: (0.8, 0.82, 0.86),
           P_TOP: (0.12, 0.18, 0.36), P_TROUSERS: (0.2, 0.22, 0.27), P_COLLAR: (0.75, 0.78, 0.82), P_LAPEL: (0.9, 0.9, 0.9),
           P_POCKET: (0.7, 0.75, 0.8), P_METAL: (0.2, 0.22, 0.26), P_CARD: (0.98, 0.76, 0.16), P_GLOW: (0.3, 0.9, 1.0),
           P_BUTTON: (0.9, 0.9, 0.9), P_PEN: (0.12, 0.19, 0.36)}
    pid = np.zeros(len(obj.data.vertices))
    obj.data.attributes["Pid"].data.foreach_get("value", pid)
    col = np.array([pal.get(int(round(p)), (1, 0, 1)) + (1.0,) for p in pid])
    pk.set_attr(obj, "GeoCol", col, kind="FLOAT_COLOR")
    mat = bpy.data.materials.new("Geo")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    a = nt.nodes.new("ShaderNodeAttribute")
    a.attribute_name = "GeoCol"
    nt.links.new(a.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.7
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def build(args):
    t0 = time.time()
    exports = os.path.join(C.EXPORTS, NAME)
    if args.get("stage"):
        C.EXPORTS = os.path.join(args["stage"], "Exports")
        C.BLEND = os.path.join(args["stage"], "Blend")
        exports = os.path.join(C.EXPORTS, NAME)
    size = int(args.get("size", TEX))
    caps, info = capture(args)
    parts, brows, fig, top_piece = mpfb_parts(caps, info)
    ears, mouth_corner = landmarks(parts, caps, info)
    cloth = [("Top", P_TOP, 1.0, top_piece)] + cloth_parts(fig, blender_to_game(parts["hair"].co), ears, mouth_corner)
    asm, obj, meshes, W, D = assemble(parts, cloth, fig)
    parts_tris = {}
    for key, pid, m in meshes:
        parts_tris[key] = parts_tris.get(key, 0) + sum(len(f) - 2 for f in m.faces)
    C.log("assembled", len(obj.data.vertices), "verts", C.tri_count(obj), "tris", parts_tris, round(time.time() - t0, 1))
    ctx = {"fig": fig, "brows": brows, "caps": caps, "info": info, "parts": parts, "ears": ears}
    if args.get("geo"):
        geo_material(obj)
        paths = None
    else:
        import voss3_paint as VP
        paths = VP.paint_maps(obj, size, exports, NAME, ctx, low_size=size // 2)
    densities = (part_density(obj, {P_COAT, P_SLEEVE, P_COLLAR, P_LAPEL, P_POCKET}), part_density(obj, {P_CARD, P_METAL, P_BUTTON}))
    for a in ("GPos", "GNrm", "Pid", "Prm", "SrcUV", "GeoCol" if not args.get("geo") else "_"):
        if a in obj.data.attributes:
            obj.data.attributes.remove(obj.data.attributes[a])
    arm = tk.build_armature(NAME + "_Rig", bones(info))
    W = rig_weights(obj, W, D, info)
    keys = shape_keys(obj, D)
    apply_weights(obj, arm, W)
    if paths:
        obj.data.materials.clear()
        obj.data.materials.append(C.game_material(NAME + "_Preview", paths, emission_strength=2.2, specular=0.35))
    acts = {n: bk.clip(arm, n, fr, fn, loop=lp) for n, (fr, fn, lp) in CLIPS.items()}
    bk.rest_pose(arm)
    if not args.get("geo"):
        export_fbx(os.path.join(exports, NAME + ".fbx"), [arm, obj])
        report(obj, arm, keys, exports, densities, info, size, parts_tris)
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
    eevee = bpy.context.scene.eevee
    eevee.shadow_ray_count = 3
    eevee.shadow_step_count = 12
    for o in rig:
        o.data.shadow_filter_radius = 3.0
        o.data.use_shadow_jitter = True
        if o.data.type == "SUN":
            o.data.angle = math.radians(10)


SHOTS = [("voss_portrait", (0, 1.4, 0), 1.45, -28, 5, 70, 1024, 1024),
         ("voss_portrait_front", (0, 1.4, 0), 1.45, 0, 4, 70, 1024, 1024),
         ("voss_face", (0, 1.44, 0.04), 0.5, -22, 4, 70, 1024, 1024),
         ("voss_face_front", (0, 1.44, 0.04), 0.5, 0, 2, 70, 1024, 1024),
         ("voss_profile", (0, 1.43, 0.0), 0.8, -88, 3, 70, 1024, 1024),
         ("voss_full", (0, 0.8, 0), 2.9, -20, 6, 50, 800, 1200),
         ("voss_full_front", (0, 0.8, 0), 2.9, 0, 4, 50, 800, 1200),
         ("voss_full_side", (0, 0.8, 0), 2.9, -90, 4, 50, 800, 1200),
         ("voss_full_back", (0, 0.8, 0), 2.9, 160, 8, 50, 800, 1200)]


def shots(out, names=None, samples=48):
    for name, target, dist, az, el, lens, w, h in SHOTS:
        if names and name not in names:
            continue
        cam = C.closeup_camera(C.G(*target), dist, azimuth=az, elevation=el, lens=lens)
        C.render(cam, os.path.join(out, name + ".png"), w, h, samples=samples)
        bpy.data.objects.remove(cam)


def dialogue(out, prefix="voss"):
    cam = C.closeup_camera(C.G(0, 1.39, 0), 1.25, azimuth=-14, elevation=4, lens=70)
    small = os.path.join(out, prefix + "_dialogue_2x.png")
    C.render(cam, small, 260, 340)
    bpy.data.objects.remove(cam)
    arr = C.image_to_array(small)
    tiny = arr.reshape(170, 2, 130, 2, 4).mean(axis=(1, 3))
    C.array_to_image(tiny, os.path.join(out, prefix + "_dialogue_130x170.png"))
    big = np.repeat(np.repeat(tiny, 4, axis=0), 4, axis=1)
    C.array_to_image(big, os.path.join(out, prefix + "_dialogue_130x170_x4.png"))


def compare(out, obj, arm, old_dir):
    """Old (round 19) next to new: portrait and full figure, same light."""
    if not old_dir or not os.path.exists(os.path.join(old_dir, "Voss.fbx")):
        return
    names = ["voss_portrait", "voss_full_front"]
    bpy.ops.import_scene.fbx(filepath=os.path.join(old_dir, "Voss.fbx"))
    old = [o for o in bpy.context.selected_objects]
    paths = {k.lower(): os.path.join(old_dir, f"Voss_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
    mat = C.game_material("Old_Preview", paths, emission_strength=2.2, specular=0.35)
    for o in old:
        if o.type == "MESH":
            o.data.materials.clear()
            o.data.materials.append(mat)
            for kb in (o.data.shape_keys.key_blocks if o.data.shape_keys else []):
                kb.value = 0.0
        if o.type == "ARMATURE" and o.animation_data:
            o.animation_data.action = None
            for pb in o.pose.bones:
                pb.rotation_quaternion = (1, 0, 0, 0)
                pb.location = (0, 0, 0)
    obj.hide_render = True
    old_out = os.path.join(out, "_old")
    os.makedirs(old_out, exist_ok=True)
    shots(old_out, names)
    for o in old:
        o.hide_render = True
    obj.hide_render = False
    for n in names:
        C.contact_sheet([os.path.join(old_out, n + ".png"), os.path.join(out, n + ".png")],
                        os.path.join(out, n.replace("voss_", "voss_compare_") + ".png"), 2, 700)


def previews(obj, arm, acts, out, paths, old_dir=None, quick=False):
    C.preview_world((0.05, 0.055, 0.065))
    rig = C.light_rig(0.6)
    soft_shadows(rig)
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    if quick:
        shots(out, ["voss_portrait", "voss_face", "voss_full", "voss_full_front", "voss_full_side"], samples=24)
        return
    shots(out)
    dialogue(out)
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
    arm.animation_data.action = acts["Idle"]
    bpy.context.scene.frame_set(10)
    compare(out, obj, arm, old_dir)
    arm.animation_data.action = None
    bk.rest_pose(arm)
    if paths:
        C.uv_layout(obj, os.path.join(out, "voss_uv.png"), 1024, paths["albedo"])
    for o in rig:
        bpy.data.objects.remove(o)


def main():
    C.reset_scene()
    args = C.cli_args()
    out = C.preview_dir()
    obj, arm, acts, paths = build(args)
    if not args.get("norender"):
        previews(obj, arm, acts, out, paths, old_dir=args.get("old"), quick=bool(args.get("geo") or args.get("quick")))


if __name__ == "__main__":
    main()
