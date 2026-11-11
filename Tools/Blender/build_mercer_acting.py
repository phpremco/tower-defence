"""Spencer acting pass (opening cutscene, Remco 2026-11-11: "the animation of spencer getting out is non existing,
just poof he's out" / "there is no facial change when he sees the enemies, and the animation that follows when he backs
up is so static, it looks fake. Maybe you should see fear and him reaching for his gun").

Opens ArtSource/Blend/Mercer.blend (pass 4: mesh, LOD1, face, visor, rig, 23 clips) and adds, without touching any
existing bone, socket, clip, mesh triangle or map:

1. Face blend shapes on Mercer_Face (and, transferred, on Mercer_Face_LOD1, which also gets Blink): Fear, Surprise,
   Determined, Breath, LookL, LookR. They are sums of MPFB faceunits01 targets (ARKit-style units, CC0) captured on
   Spencer's own MPFB head (spencer_face.py's phenotype, one unit at weight 1 each, see capture_units()), mapped onto
   the face mesh vertex by vertex. Exaggerated past 1 where the visor tint ate them.
2. Clips (30 fps, keyed with build_mercer's pose language, Hermite-splined through the key poses so the motion flows
   through them instead of stopping at each one):
   Capsule_Exit, Startle_Backstep, Draw_Aim_Nervous, Aim_Hold_Nervous_Loop, Turn_Look_L, Turn_Look_R.
   No clip keys root motion (the Root bone stays at rest, as in every other clip). The clips that travel or turn
   instead START offset from the root and END in the stance on it: Unity moves (or turns) the root once, at the
   clip's first frame, by the amount in the report, and nothing pops.

Exports go to ArtSource/Exports/_staging/Mercer/ (same layout as ArtSource/Exports/Mercer/, the maps and the Low set
copied unchanged); the result blend to ArtSource/Exports/_staging/Mercer/blend/Mercer.blend. The original blend and
exports are never written.

Run: blender --background --factory-startup --threads 8 --python Tools/Blender/build_mercer_acting.py --
     [--stage <dir>] [--units <npz>] [--preview <dir>] [--norender] [--faceonly]
"""

import json
import math
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402
from mathutils.kdtree import KDTree  # noqa: E402

import build_mercer5 as M5  # noqa: E402  (imports and patches build_mercer, _3, _4, reactions: the pass-4 pose language)
import build_mercer as BM  # noqa: E402
import build_mercer3 as M3  # noqa: E402
import build_mercer_reactions as RX  # noqa: E402
import common as C  # noqa: E402
import spencer_face as F  # noqa: E402
import suit as S  # noqa: E402
from build_mercer import SIDE, STANCE, UP, ease, foot_at, rad, sock_frame  # noqa: E402
from build_mercer_reactions import idle0  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
ROUND = "spencer-acting-2026-11-11 (build_mercer_acting.py)"
FPS = 30
SRC_BLEND = os.path.join(C.BLEND, NAME + ".blend")
SRC_EXPORT = os.path.join(C.EXPORTS, NAME)
STAGE = os.path.join(C.EXPORTS, "_staging", NAME)
UNITS_CACHE = os.path.join(STAGE, "blend", "SpencerFace_units.npz")
FACE, FACE_LOD = M5.FACE, M5.FACE_LOD

# ------------------------------------------------------------------------------------------------------------
# 1. Face shapes
# ------------------------------------------------------------------------------------------------------------

UNITS = ["browDownLeft", "browDownRight", "browInnerUp", "browOuterUpLeft", "browOuterUpRight", "eyeWideLeft", "eyeWideRight",
         "eyeSquintLeft", "eyeSquintRight", "eyeBlinkLeft", "eyeBlinkRight", "jawOpen", "jawForward", "mouthStretchLeft",
         "mouthStretchRight", "mouthLowerDownLeft", "mouthLowerDownRight", "mouthUpperUpLeft", "mouthUpperUpRight", "mouthFunnel",
         "mouthPressLeft", "mouthPressRight", "mouthFrownLeft", "mouthFrownRight", "noseSneerLeft", "noseSneerRight",
         "cheekSquintLeft", "cheekSquintRight", "eyeLookOutLeft", "eyeLookOutRight", "eyeLookInLeft", "eyeLookInRight",
         "mouthRollLower", "mouthClose", "mouthShrugUpper", "mouthShrugLower", "eyeLookDownLeft", "eyeLookDownRight",
         "eyeLookUpLeft", "eyeLookUpRight"]


def both(unit, w):
    return {unit + "Left": w, unit + "Right": w}


def mix(*parts):
    out = {}
    for p in parts:
        for k, v in p.items():
            out[k] = out.get(k, 0.0) + v
    return out


# Weights per faceunit. Past 1 is allowed (the targets are linear): the visor glass (12-62 % opaque, tinted) and the
# helmet's shadow flatten small moves, so the brows and lids are pushed further than a bare face would need.
SHAPES = {
    # Brows up and pulled together (inner raise dominant, outer brows barely up), eyes wide, lips stretched back and
    # parted, jaw a little open.
    "Fear": mix({"browInnerUp": 1.9}, both("browOuterUp", 0.15), both("browDown", 0.25), both("eyeWide", 1.6), {"jawOpen": 0.1},
                both("mouthStretch", 1.7), both("mouthLowerDown", 0.35), both("mouthUpperUp", 0.35), both("noseSneer", 0.2)),
    # Whole brows high, eyes wide, jaw dropped, lips loose and a little rounded.
    "Surprise": mix({"browInnerUp": 1.3}, both("browOuterUp", 1.5), both("eyeWide", 1.6), {"jawOpen": 0.42, "mouthFunnel": 0.25},
                    both("mouthLowerDown", 0.2)),
    # Brows down and in, lids narrowed, lips pressed, the chin pushed up (jaw set), a hint of a sneer.
    "Determined": mix(both("browDown", 1.5), both("eyeSquint", 1.0), both("cheekSquint", 0.35), both("mouthPress", 1.0),
                      {"mouthRollLower": 0.25, "mouthShrugLower": 0.45}, both("noseSneer", 0.35)),
    # A gasp: jaw open, lips rounded and parted (mouth only, so it layers over any brow shape).
    "Breath": mix({"jawOpen": 0.36, "mouthFunnel": 0.4}, both("mouthLowerDown", 0.3), both("mouthUpperUp", 0.2)),
    # Eyes to his left / right (LookL = to his left: the left eye looks out, the right eye in).
    "LookL": {"eyeLookOutLeft": 1.0, "eyeLookInRight": 1.0},
    "LookR": {"eyeLookOutRight": 1.0, "eyeLookInLeft": 1.0},
}
SHAPE_NOTES = {
    "Fear": "brows up and together, eyes wide, lips pulled back and parted, jaw slightly open",
    "Surprise": "brows high, eyes wide, jaw dropped",
    "Determined": "brows down and in, eyes narrowed, lips pressed, jaw set",
    "Breath": "a gasp: jaw open, lips rounded (mouth only; layers over Fear/Surprise/Determined)",
    "LookL": "both eyes turn about 25 degrees to his left (screen right when he faces the camera)",
    "LookR": "both eyes turn about 25 degrees to his right",
    "Blink": "upper lids closed (unchanged from pass 4)",
}
MAX_TRAVEL = 0.05                 # a vertex moving further than this in one shape is a mapping error, not an expression


def capture_units(cache):
    """{unit: (body deltas, eye deltas)} and the capture, from the cache or a fresh MPFB build (deterministic: the same
    phenotype, targets and assets as spencer_face.capture; the cached body matches the pass-4 cache to 0.0)."""
    import voss3_human as VH
    if not os.path.exists(cache):
        F.EXPRESSIONS = {u: {u: 1.0} for u in UNITS}
        F.capture(cache)
    caps, info = VH.load_cache(cache)
    return caps, info


def face_sources(face_obj, caps):
    """For each Mercer_Face vertex: ('body' | 'eyes', index in that capture). The face is build_face's placed subset of
    the capture (scale FACE_SCALE about the eye centre, the eyes at (0, y, EYE_Z), y found by the fit), skin first,
    then the eyeballs."""
    eyes = F.eye_pieces(caps)
    comp = F.components(eyes.faces, len(eyes.co))
    e = np.mean([eyes.co[comp == c].mean(axis=0) for c in np.unique(comp)], axis=0)
    n_eye = len(eyes.co)
    co = np.zeros(len(face_obj.data.vertices) * 3)
    face_obj.data.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    s = M5.FACE_SCALE
    placed_eyes = (eyes.co - e) * s
    y = float(co[-n_eye:, 1].mean() - placed_eyes[:, 1].mean())
    target = np.array((0.0, y, M5.EYE_Z))
    out = []
    worst = 0.0
    trees = {}
    for key in ("body", "eyes"):
        pts = (caps[key].co - e) * s + target
        t = KDTree(len(pts))
        for i, p in enumerate(pts):
            t.insert(p, i)
        t.balance()
        trees[key] = t
    for i, p in enumerate(co):
        key = "eyes" if i >= len(co) - n_eye else "body"
        _, j, d = trees[key].find(p)
        worst = max(worst, d)
        out.append((key, j))
    C.log("face map: eye centre y", round(y, 4), "worst vertex match mm", round(worst * 1000, 3))
    if worst > 0.0005:
        raise RuntimeError("face map: the face mesh no longer matches the MPFB capture (%.2f mm)" % (worst * 1000))
    return out


def shape_deltas(caps, sources, spec):
    d = np.zeros((len(sources), 3))
    for unit, w in spec.items():
        per = {k: caps[k].deltas[unit] for k in ("body", "eyes")}
        d += w * np.array([per[k][j] for k, j in sources])
    d *= M5.FACE_SCALE
    far = np.linalg.norm(d, axis=1) > MAX_TRAVEL
    d[far] = 0.0
    return d


def set_shape(obj, name, deltas):
    if obj.data.shape_keys is None:
        obj.shape_key_add(name="Basis", from_mix=False)
    kb = obj.data.shape_keys.key_blocks.get(name)
    if kb is None:
        kb = obj.shape_key_add(name=name, from_mix=False)
    base = np.zeros(len(obj.data.vertices) * 3)
    obj.data.shape_keys.key_blocks["Basis"].data.foreach_get("co", base)
    kb.data.foreach_set("co", (base.reshape(-1, 3) + deltas).ravel())
    kb.value = 0.0
    kb.slider_min, kb.slider_max = 0.0, 1.0
    return kb


def shape_delta_of(obj, name):
    kb = obj.data.shape_keys.key_blocks
    a = np.zeros(len(obj.data.vertices) * 3)
    b = np.zeros(len(obj.data.vertices) * 3)
    kb["Basis"].data.foreach_get("co", a)
    kb[name].data.foreach_get("co", b)
    return (b - a).reshape(-1, 3)


def transfer(src, dst, names):
    """Shape deltas from the full face to the decimated LOD face: each LOD vertex takes the barycentric mix of the
    nearest full-face triangle's deltas (the LOD is a decimation of the same surface)."""
    me = src.data
    base = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get("co", base)
    base = base.reshape(-1, 3)
    me.calc_loop_triangles()
    tris = [tuple(t.vertices) for t in me.loop_triangles]
    tree = BVHTree.FromPolygons([Vector(p) for p in base], tris)
    deltas = {n: shape_delta_of(src, n) for n in names}
    dco = np.zeros(len(dst.data.vertices) * 3)
    dst.data.vertices.foreach_get("co", dco)
    dco = dco.reshape(-1, 3)
    out = {n: np.zeros_like(dco) for n in names}
    worst = 0.0
    for i, p in enumerate(dco):
        loc, _, ti, dist = tree.find_nearest(Vector(p))
        worst = max(worst, dist)
        a, b, c = (Vector(base[j]) for j in tris[ti])
        w = barycentric(loc, a, b, c)
        for n in names:
            out[n][i] = sum(w[k] * deltas[n][tris[ti][k]] for k in range(3))
    C.log("LOD face transfer: worst surface distance mm", round(worst * 1000, 3))
    for n in names:
        set_shape(dst, n, out[n])


def barycentric(p, a, b, c):
    v0, v1, v2 = b - a, c - a, p - a
    d00, d01, d11 = v0.dot(v0), v0.dot(v1), v1.dot(v1)
    d20, d21 = v2.dot(v0), v2.dot(v1)
    den = d00 * d11 - d01 * d01
    if abs(den) < 1e-14:
        return (1.0, 0.0, 0.0)
    v = (d11 * d20 - d01 * d21) / den
    w = (d00 * d21 - d01 * d20) / den
    return (1.0 - v - w, v, w)


def build_face_shapes(units_cache):
    face, lod = bpy.data.objects[FACE], bpy.data.objects[FACE_LOD]
    caps, _ = capture_units(units_cache)
    sources = face_sources(face, caps)
    stats = {}
    for name, spec in SHAPES.items():
        d = shape_deltas(caps, sources, spec)
        set_shape(face, name, d)
        stats[name] = round(float(np.linalg.norm(d, axis=1).max()) * 1000, 2)
        C.log("face shape", name, "max travel mm", stats[name])
    names = ["Blink"] + list(SHAPES)
    transfer(face, lod, names)
    return stats


# ------------------------------------------------------------------------------------------------------------
# Face clearance: every shape at full weight (and the loud pairs) against the helmet, neck, collar and visor
# ------------------------------------------------------------------------------------------------------------

SINGLE_CHECK = ("Idle", "Lower_Weapon", "Down")   # every shape alone at 1 in the clips that bow or tip the head most


def face_clearance(step=3):
    """The face against the helmet, neck, collar and visor: in each new clip with its own face track (the weights it
    really uses), and every shape alone at full weight in SINGLE_CHECK."""
    import spencer_face_check as FC
    arm, face = bpy.data.objects[NAME + "_Rig"], bpy.data.objects[FACE]
    body, visor = bpy.data.objects[NAME], bpy.data.objects[M5.VISOR]
    out = {"fails": [], "worst_clearance_mm": 1000.0, "frames": 0}

    def run(clip, values_at, label):
        act = bpy.data.actions[clip]
        for f in range(0, int(round(act.frame_range[1])) + 1, step):
            set_face(values_at(f))
            FC.set_frame(arm, act, f)
            r = FC.check_frame(arm, face, body, visor)
            out["frames"] += 1
            out["worst_clearance_mm"] = min(out["worst_clearance_mm"], r["clearance_mm"])
            if r["overlap"] or r["visor_overlap"]:
                out["fails"].append({"shapes": label, "clip": clip, "frame": f, **r})
    for clip in CLIP_FNS:
        run(clip, lambda f, c=clip: face_at(c, f), "track")
    for clip in SINGLE_CHECK:
        for name in ["Blink"] + list(SHAPES):
            run(clip, lambda f, n=name: {n: 1.0}, name)
    set_face({})
    out["pass"] = not out["fails"]
    C.log("face clearance", "PASS" if out["pass"] else "FAIL", len(out["fails"]), "worst mm", out["worst_clearance_mm"],
          sorted({(x["shapes"], x["clip"]) for x in out["fails"]}))
    return out


# ------------------------------------------------------------------------------------------------------------
# Main (filled in below)
# ------------------------------------------------------------------------------------------------------------

SHEET_SHAPES = [("Neutral", {}), ("Fear", {"Fear": 1.0}), ("Surprise", {"Surprise": 1.0}), ("Determined", {"Determined": 1.0}),
                ("Breath", {"Breath": 1.0}), ("Fear+Breath", {"Fear": 1.0, "Breath": 0.6}), ("LookL", {"LookL": 1.0}),
                ("LookR", {"LookR": 1.0}), ("Blink", {"Blink": 1.0})]


def set_face(values):
    for o in (bpy.data.objects[FACE], bpy.data.objects[FACE_LOD]):
        for k in o.data.shape_keys.key_blocks[1:]:
            k.value = values.get(k.name, 0.0)


def face_sheet(blend, out_dir, prefix, size=420):
    """Close-ups through the visor (glass on) at the cutscene's face distance (0.65 m, 85 mm, the head-bone camera of
    spencer_face_check) and a mid shot (1.5 m, waist up), one column per shape; rows: close front, close 3/4, mid."""
    import mercer_previews as MP
    import spencer_face_check as FC
    import spencer_previews5 as SP5
    rig, acts, _ = SP5.open_scene(blend)
    MP.pose(rig, acts["Idle"], 10, "none")
    bpy.context.view_layer.update()
    rows = (("close", -12, 3, 0.65, 85), ("close34", -35, 6, 0.65, 85), ("mid", -20, 4, 1.5, 70))
    cells = []
    for row, az, el, dist, lens in rows:
        for name, values in SHEET_SHAPES:
            set_face(values)
            cam = FC.head_camera(rig.arm, az, el, dist, lens=lens)
            cells.append(C.render(cam, os.path.join(out_dir, f"_face_{row}_{name}.png"), size, size, samples=24))
            bpy.data.objects.remove(cam)
    set_face({})
    path = os.path.join(out_dir, f"{prefix}_faces.png")
    C.contact_sheet(cells, path, len(SHEET_SHAPES), size)
    for p in cells:
        os.remove(p)
    C.log("face sheet", path)
    return path


# ------------------------------------------------------------------------------------------------------------
# 2. Clip machinery: Hermite splines through key poses, a foot-step schedule, offsets for travel and turns
# ------------------------------------------------------------------------------------------------------------

def hermite_weights(i, s, frames, stops):
    """Weights on key indices for segment i at s (0..1): cubic Hermite with Catmull-Rom tangents scaled to the uneven
    key spacing; a key in `stops` has zero velocity."""
    n = len(frames)
    h00, h10 = 2 * s ** 3 - 3 * s ** 2 + 1, s ** 3 - 2 * s ** 2 + s
    h01, h11 = -2 * s ** 3 + 3 * s ** 2, s ** 3 - s ** 2
    t1, t2 = frames[i], frames[i + 1]
    w = {i: h00, i + 1: h01}
    if i > 0 and i not in stops:
        a = (t2 - t1) / (t2 - frames[i - 1])
        w[i - 1] = w.get(i - 1, 0.0) - h10 * a
        w[i + 1] += h10 * a
    if i + 2 < n and (i + 1) not in stops:
        b = (t2 - t1) / (frames[i + 2] - t1)
        w[i] -= h11 * b
        w[i + 2] = w.get(i + 2, 0.0) + h11 * b
    return w


def mix_value(vals, w, nearest):
    first = vals[nearest]
    if isinstance(first, str):
        return first
    if isinstance(first, Matrix):
        q0 = first.to_quaternion()
        acc = [0.0, 0.0, 0.0, 0.0]
        acc_t = Vector()
        for k, wk in w.items():
            loc, q, _ = vals[k].decompose()
            if q.dot(q0) < 0:
                q.negate()
            for j in range(4):
                acc[j] += q[j] * wk
            acc_t += loc * wk
        m = Quaternion(acc).normalized().to_matrix().to_4x4()
        m.translation = acc_t
        return m
    if isinstance(first, Vector):
        acc_v = Vector((0.0, 0.0, 0.0))
        for k, wk in w.items():
            acc_v += vals[k] * wk
        return acc_v
    return sum(vals[k] * wk for k, wk in w.items())


ARM_FIELDS = ("mode", "sock", "space", "pole")


def arm_field(k):
    parts = k.split(".")
    return parts[0] == "arm" and len(parts) == 3 and parts[2] in ARM_FIELDS


class Spline:
    """[(frame, P, mode)] -> per-frame pose dicts. mode of a key: "smooth" (the curve flows through it), "stop" (zero
    velocity there: a held beat) or "snap" (the segment INTO it is a reflex: fast out, soft landing). An arm that is IK
    in any key weighing on a frame is driven by IK between the keys' achieved socket frames (as reactions.KeyClip), so
    FK <-> IK changes do not jump. `feet` (Steps) overrides the feet; `extra(f, P)` adds motion on top."""

    def __init__(self, poser, keys, feet=None, extra=None):
        self.frames = [k[0] for k in keys]
        allk = set()
        for _, P, _ in keys:
            allk |= {k for k in P if not arm_field(k)}
        self.P = []
        for j, (_, P, _) in enumerate(keys):
            full = {k: v for k, v in P.items() if not arm_field(k)}
            for k in allk - set(full):
                near = min((abs(j - m), m) for m in range(len(keys)) if k in keys[m][1])[1]
                full[k] = keys[near][1][k]
            self.P.append(full)
        self.modes = [k[2] for k in keys]
        self.stops = {0, len(keys) - 1} | {j for j, m in enumerate(self.modes) if m in ("stop", "snap")}
        self.arms = [RX.Arms(poser, P) for _, P, _ in keys]
        self.feet = feet
        self.extra = extra

    def pose(self, f):
        fr = self.frames
        if f <= fr[0]:
            i, s = 0, 0.0
        elif f >= fr[-1]:
            i, s = len(fr) - 2, 1.0
        else:
            i = max(j for j in range(len(fr) - 1) if fr[j] <= f)
            s = (f - fr[i]) / (fr[i + 1] - fr[i])
        if self.modes[i + 1] == "snap":
            e = RX.snap(s)
            w = {i: 1.0 - e, i + 1: e}
        else:
            w = hermite_weights(i, s, fr, self.stops)
        nearest = i if s < 0.5 else i + 1
        P = {k: mix_value({j: self.P[j][k] for j in w}, w, nearest) for k in self.P[nearest]}
        for name in SIDE:
            if any(self.arms[j].ik[name] for j in w):
                P[f"arm.{name}.mode"] = "ik"
                P[f"arm.{name}.space"] = "world"
                P[f"arm.{name}.sock"] = mix_value({j: self.arms[j].sock[name] for j in w}, w, nearest)
                P[f"arm.{name}.pole"] = mix_value({j: self.arms[j].pole[name] for j in w}, w, nearest)
        if self.feet:
            self.feet.apply(f, P)
        if self.extra:
            self.extra(f, P)
        return P

    def make(self, frames, loop=False):
        def fn(poser):
            def spec(t, f):
                return BM.compose(poser, self.pose(f))
            return frames, spec, loop
        return fn


class Steps:
    """Per foot: a start plant (pos, yaw, pitch) and steps (lift, land, pos, yaw, pitch, height, kind). Between lift and
    land the ankle travels on an arc, fastest mid-swing; "fwd" peels the heel and lands heel first, "back" reaches back
    toe first, "turn" pivots on a low arc."""

    def __init__(self, plan):
        self.plan = plan

    def apply(self, f, P):
        for name, (start, steps) in self.plan.items():
            pos, yaw, pitch = start
            out = (pos, yaw, pitch, 0.0)
            for lift, land, npos, nyaw, npitch, height, kind in steps:
                if f < lift:
                    break
                if f >= land:
                    pos, yaw, pitch = npos, nyaw, npitch
                    out = (pos, yaw, pitch, 0.0)
                    continue
                s = (f - lift) / (land - lift)
                u = 0.35 * s + 0.65 * ease(s)
                p = pos.lerp(npos, u) + Vector((0.0, 0.0, height * math.sin(math.pi * s) ** 0.85))
                if kind == "fwd":
                    pt = -22.0 * math.sin(math.pi * min(1.0, s / 0.55)) * (1 - s) + 11.0 * math.sin(math.pi * max(0.0, (s - 0.45) / 0.55))
                    toe = 20.0 * max(0.0, 1 - s / 0.3)
                elif kind == "back":
                    pt = -18.0 * math.sin(math.pi * s ** 0.8)
                    toe = 10.0 * max(0.0, 1 - s / 0.4)
                else:
                    pt, toe = -6.0 * math.sin(math.pi * s), 0.0
                out = (p, yaw + (nyaw - yaw) * ease(s), pitch + (npitch - pitch) * u + pt, toe)
                break
            p, y, pt, toe = out
            P[f"foot.{name}.pos"] = p
            P[f"foot.{name}.yaw"] = y
            P[f"foot.{name}.pitch"] = pt
            P[f"foot.{name}.toe"] = toe


def over(base, d):
    out = dict(base)
    out.update(d)
    return out


def hips_at(x=0.0, joint=BM.HIP_Y, fwd=0.0):
    """hips.loc that puts the hip joints (rest height HIP_Y) at height `joint`, `fwd` and `x` in root space."""
    return G(x, joint - BM.HIP_Y, fwd)


def rotate_pose(P, deg):
    """The whole pose turned `deg` about the root's vertical axis (+ = to his left)."""
    R = Matrix.Rotation(rad(deg), 4, "Z")
    out = dict(P)
    out["hips.yaw"] = P.get("hips.yaw", 0.0) + deg
    out["hips.loc"] = R.to_3x3() @ P.get("hips.loc", Vector())
    for name in SIDE:
        if f"foot.{name}.pos" in P:
            out[f"foot.{name}.pos"] = R.to_3x3() @ P[f"foot.{name}.pos"]
        out[f"foot.{name}.yaw"] = P.get(f"foot.{name}.yaw", 0.0) + deg * SIDE[name].sx
    return out


def shift_pose(P, d):
    out = dict(P)
    out["hips.loc"] = P.get("hips.loc", Vector()) + d
    for name in SIDE:
        if f"foot.{name}.pos" in P:
            out[f"foot.{name}.pos"] = P[f"foot.{name}.pos"] + d
    return out


def plant(P, name):
    return (P[f"foot.{name}.pos"].copy(), P.get(f"foot.{name}.yaw", 0.0), P.get(f"foot.{name}.pitch", 0.0))


def fk_arm(side, **d):
    base = {f"arm.{side}.mode": "fk", f"arm.{side}.swing": M3.REST_ARMS[f"arm.{side}.swing"], f"arm.{side}.out": -20.0,
            f"arm.{side}.elbow": M3.REST_ARMS[f"arm.{side}.elbow"], f"arm.{side}.twist": 10.0, f"arm.{side}.wrist": 4.0,
            f"arm.{side}.wrist_side": 32.0}
    base.update({f"arm.{side}.{k}": v for k, v in d.items()})
    return base


def holster_grip(poser, P):
    """The Holster_R frame for P's body (the right arm plays no part)."""
    Q = {k: v for k, v in P.items() if not (arm_field(k) and k.startswith("arm.R."))}
    return M3.solve(poser, Q)["Holster_R"]


def achieved(poser, P, side):
    return M3.solve(poser, P)[f"Weapon_{side}"]


def smooth01(a, b, f):
    return ease((f - a) / (b - a)) if b > a else float(f >= b)


# ------------------------------------------------------------------------------------------------------------
# Capsule_Exit
# ------------------------------------------------------------------------------------------------------------

# Cutscene_Capsule at scale 1 (build_cutscene.Capsule): the ramp hinge (the hatch sill) is 0.46 m up and 0.58 m in
# front of the capsule centre; the open ramp runs 0.63 m forward down to the ground; the Exit socket is 1.35 m in front
# of the centre. The clip's root is the Exit socket, so the sill is HATCH_BACK behind the root.
# Round 40 (Unity): at scale 1 the doorway is 0.4 x 0.74 m and Spencer's crouched back and pack cut through the hull, so the
# landed capsule is shown at CAPSULE_SCALE (CutsceneCast._landedCapsuleScale must match). The hatch and the clip's path
# down the ramp scale with it; his body does not. At CAPSULE_SCALE 1 every value is the original one.
CAPSULE_SCALE = 1.4
HATCH_UP = 0.46 * CAPSULE_SCALE
HATCH_BACK = 0.77 * CAPSULE_SCALE
RAMP_RUN = 0.63 * CAPSULE_SCALE
DOOR_POST_X = 0.2 * CAPSULE_SCALE
EXIT_FRAMES = 90
EXIT_EVENTS = {"grip": 0, "duck": 14, "step_down": 28, "release_R": 30, "ground": 42, "release_L": 46, "stand": 66,
               "look_left": 72, "look_right": 81, "end": 90}


def ramp_h(fwd):
    foot = -HATCH_BACK + RAMP_RUN
    if fwd <= -HATCH_BACK:
        return HATCH_UP
    return max(0.0, HATCH_UP * (foot - fwd) / RAMP_RUN)


def ramp_h1(fwd):
    """The ramp's height at scale 1 (the hatch the clip was first keyed for)."""
    foot = -0.77 + 0.63
    if fwd <= -0.77:
        return 0.46
    return max(0.0, 0.46 * (foot - fwd) / 0.63)


def on_ramp(fwd1, up1):
    """A point keyed on the scale-1 ramp (fwd1, up1), moved along the scaled ramp: as far along it and as high above it."""
    return fwd1 * CAPSULE_SCALE, ramp_h(fwd1 * CAPSULE_SCALE) + up1 - ramp_h1(fwd1)


def post_grip(side, up, fwd):
    sx = SIDE[side].sx
    return sock_frame(G(DOOR_POST_X * sx, up, fwd), G(-0.35 * sx, 0.0, 1.0), UP)


def exit_keys():
    start = idle0()
    crouch = over(start, {
        "hips.loc": hips_at(0.0, HATCH_UP + 0.09 + 0.36, -HATCH_BACK - 0.2), "hips.pitch": 34.0, "hips.yaw": 0.0, "hips.roll": 0.0,
        "spine1.pitch": 18.0, "spine1.yaw": 0.0, "spine1.roll": 0.0, "spine2.pitch": 14.0, "spine2.yaw": 0.0,
        "neck.pitch": -22.0, "neck.yaw": 0.0, "head.pitch": -18.0, "head.yaw": 4.0,
        "clav.L.raise": 8.0, "clav.R.raise": 8.0,
        "arm.L.mode": "ik", "arm.L.space": "world", "arm.L.sock": post_grip("L", HATCH_UP + 0.54, -HATCH_BACK + 0.14), "arm.L.pole": G(0.8, -0.6, -0.2),
        "arm.R.mode": "ik", "arm.R.space": "world", "arm.R.sock": post_grip("R", HATCH_UP + 0.59, -HATCH_BACK + 0.14), "arm.R.pole": G(-0.8, -0.6, -0.2),
        "grip.L.fingers": 0.9, "grip.R.fingers": 0.9, "grip.L.thumb": 0.6, "grip.R.thumb": 0.6})
    gather = over(crouch, {"hips.loc": hips_at(0.0, HATCH_UP + 0.09 + 0.33, -HATCH_BACK - 0.17), "head.pitch": -24.0, "neck.pitch": -18.0,
                           "spine2.pitch": 16.0, "clav.L.raise": 3.0, "clav.R.raise": 3.0})
    duck = over(crouch, {"hips.loc": hips_at(0.0, HATCH_UP + 0.09 + 0.4, -HATCH_BACK - 0.06), "hips.pitch": 40.0, "spine1.pitch": 20.0,
                         "spine2.pitch": 14.0, "neck.pitch": 4.0, "head.pitch": -6.0, "head.yaw": -6.0})
    lean_f, lean_u = on_ramp(-0.6, 0.86)
    down_f, down_u = on_ramp(-0.36, 0.73)
    lean = over(duck, {"hips.loc": hips_at(0.02, lean_u, lean_f), "hips.pitch": 26.0, "spine1.pitch": 12.0, "spine2.pitch": 8.0,
                       "neck.pitch": -12.0, "head.pitch": -14.0, "head.yaw": 2.0, "hips.roll": -4.0,
                       "arm.R.sock": post_grip("R", HATCH_UP + 0.56, -HATCH_BACK + 0.16)})
    down = over(lean, {"hips.loc": hips_at(-0.01, down_u, down_f), "hips.pitch": 14.0, "spine1.pitch": 8.0, "spine2.pitch": 6.0,
                       "neck.pitch": -6.0, "head.pitch": -8.0, "hips.roll": 3.0, "clav.R.raise": 0.0})
    down.update(fk_arm("R", swing=28.0, out=24.0, elbow=48.0, twist=18.0, wrist=-8.0))
    down.update({"grip.R.fingers": 0.45, "grip.R.thumb": 0.3})
    rise = over(down, {"hips.loc": hips_at(0.0, 0.79, -0.13 * CAPSULE_SCALE), "hips.pitch": 6.0, "spine1.pitch": 3.0, "spine2.pitch": 1.0,
                       "neck.pitch": -2.0, "head.pitch": -4.0, "hips.yaw": -6.0, "hips.roll": 0.0, "clav.L.raise": 0.0})
    rise.update(fk_arm("L", swing=16.0, out=12.0, elbow=30.0, twist=12.0))
    rise.update(fk_arm("R", swing=8.0, out=2.0, elbow=24.0))
    rise.update({"grip.L.fingers": 0.45, "grip.L.thumb": 0.3})
    tall = over(start, {"spine2.pitch": -5.0, "spine1.pitch": -1.0, "neck.pitch": -2.0, "head.pitch": -6.0, "clav.L.raise": 5.0,
                        "clav.R.raise": 5.0, "hips.loc": STANCE["hips.loc"] + G(0, 0.012, 0)})
    left = over(start, {"head.yaw": 42.0, "neck.yaw": 12.0, "spine2.yaw": 12.0, "spine1.yaw": 6.0, "head.pitch": -3.0})
    left2 = over(left, {"head.yaw": 46.0, "head.pitch": 1.0})
    right = over(start, {"head.yaw": -40.0, "neck.yaw": -12.0, "spine2.yaw": -6.0, "spine1.yaw": 0.0, "head.pitch": -2.0})
    right2 = over(right, {"head.yaw": -44.0, "head.pitch": 2.0})
    return [(0, crouch, "stop"), (9, gather, "smooth"), (18, duck, "smooth"), (28, lean, "smooth"), (42, down, "smooth"),
            (54, rise, "smooth"), (66, tall, "smooth"), (71, left, "smooth"), (75, left2, "stop"), (81, right, "smooth"),
            (85, right2, "stop"), (EXIT_FRAMES, start, "stop")]


def exit_steps():
    sL, sR = plant(STANCE, "L"), plant(STANCE, "R")
    sill_L = (foot_at("L", fwd=-HATCH_BACK - 0.1, up=HATCH_UP), 6.0, 0.0)
    sill_R = (foot_at("R", fwd=-HATCH_BACK - 0.16, up=HATCH_UP), 10.0, 0.0)
    slope = -math.degrees(math.atan2(HATCH_UP, RAMP_RUN)) * 0.7
    ramp_R = foot_at("R", fwd=-0.45 * CAPSULE_SCALE, side=0.03, up=ramp_h(-0.45 * CAPSULE_SCALE))
    ground_L = foot_at("L", fwd=-0.17 * CAPSULE_SCALE, side=0.04, up=ramp_h(-0.17 * CAPSULE_SCALE))
    return Steps({
        "R": (sill_R, [(17, 28, ramp_R, 14.0, slope, 0.09, "fwd"), (44, 54, sR[0], sR[1], 0.0, 0.07, "fwd")]),
        "L": (sill_L, [(30, 42, ground_L, 8.0, 0.0, 0.11, "fwd"), (56, 64, sL[0], sL[1], 0.0, 0.04, "fwd")]),
    })


def exit_extra(f, P):
    # Laboured breathing in the cramped hatch, easing off once he stands.
    b = math.sin(2 * math.pi * f / 22.0) * (1.0 - 0.7 * smooth01(50, 70, f))
    P["spine2.pitch"] -= 1.4 * b
    P["clav.L.raise"] += 1.6 * b
    P["clav.R.raise"] += 1.6 * b


def exit_clip(poser):
    return Spline(poser, exit_keys(), exit_steps(), exit_extra).make(EXIT_FRAMES)(poser)


# ------------------------------------------------------------------------------------------------------------
# Turn_Look_L / Turn_Look_R
# ------------------------------------------------------------------------------------------------------------

TURN_FRAMES = 24
TURN_DEG = 90.0
TURN_EVENTS = {"look": 3, "step_lead": 5, "step_trail": 11, "plant_lead": 12, "plant_trail": 18, "end": 24}
TURN_GAZE = ("head.yaw", "neck.yaw", "spine1.yaw", "spine2.yaw")


def turn_keys(sign):
    """Authored as a turn from the stance to TURN_DEG * sign, then turned back by that much: the clip starts facing
    the old way and ends in the stance on the root (Unity turns the root at frame 0)."""
    base = idle0()
    keys = [
        (0, 0.0, {}, "stop"),
        (4, 2.0, {"head.yaw": 40.0, "neck.yaw": 10.0, "spine2.yaw": 4.0, "head.pitch": -2.0,
                  "hips.loc": STANCE["hips.loc"] + G(-0.035 * sign, -0.012, 0.0)}, "smooth"),
        (10, 28.0, {"head.yaw": 25.0, "neck.yaw": 8.0, "spine2.yaw": 12.0, "spine1.yaw": 10.0,
                    "hips.loc": STANCE["hips.loc"] + G(-0.02 * sign, -0.03, 0.0), "arm.L.swing": 6.0 - 10.0 * sign,
                    "arm.R.swing": 2.0 + 10.0 * sign, "arm.L.out": -14.0, "arm.R.out": -14.0}, "smooth"),
        (16, 72.0, {"head.yaw": 8.0, "neck.yaw": 4.0, "spine2.yaw": 6.0, "spine1.yaw": 4.0,
                    "hips.loc": STANCE["hips.loc"] + G(0.02 * sign, -0.022, 0.0), "arm.L.swing": 6.0 + 6.0 * sign,
                    "arm.R.swing": 2.0 - 6.0 * sign}, "smooth"),
        (TURN_FRAMES, 90.0, {}, "stop"),
    ]
    out = []
    for f, theta, d, mode in keys:
        P = over(base, d)
        for k in TURN_GAZE:
            if k in d:
                P[k] = base.get(k, 0.0) + d[k] * sign
        out.append((f, rotate_pose(P, theta * sign - TURN_DEG * sign), mode))
    return out


def turn_steps(sign):
    lead, trail = ("L", "R") if sign > 0 else ("R", "L")

    def turned(name, deg):
        P = rotate_pose({f"foot.{name}.pos": STANCE[f"foot.{name}.pos"], f"foot.{name}.yaw": STANCE[f"foot.{name}.yaw"]}, deg)
        return P[f"foot.{name}.pos"], P[f"foot.{name}.yaw"]
    plan = {}
    for name, lift, land, h in ((lead, 5, 12, 0.06), (trail, 11, 18, 0.05)):
        p0, y0 = turned(name, -TURN_DEG * sign)
        p1, y1 = turned(name, 0.0)
        plan[name] = ((p0, y0, 0.0), [(lift, land, p1, y1, 0.0, h, "turn")])
    return Steps(plan)


def turn_extra(f, P):
    # A quick breath in with the look (the shoulders lift and settle).
    k = math.sin(math.pi * min(1.0, f / TURN_FRAMES))
    P["clav.L.raise"] += 2.5 * k
    P["clav.R.raise"] += 2.5 * k


def turn_clip(sign):
    def fn(poser):
        return Spline(poser, turn_keys(sign), turn_steps(sign), turn_extra).make(TURN_FRAMES)(poser)
    return fn


# ------------------------------------------------------------------------------------------------------------
# Startle_Backstep
# ------------------------------------------------------------------------------------------------------------

STARTLE_FRAMES = 75
BACK_TRAVEL = 0.5
STARTLE_EVENTS = {"flinch": 2, "step_1": 13, "step_2": 23, "reach": 28, "stumble": 34, "step_3": 40, "grip": 40, "ready": 57,
                  "end": 75}


def ready_pose():
    """Low, bladed, weight back, head up at the big thing; the left hand out in front (fending, fingers spread), the
    right hand on the holstered pistol's grip (set by the clips). At the root."""
    p = over(STANCE, {
        "hips.loc": hips_at(0.0, 0.73, -0.06), "hips.yaw": -17.0, "hips.pitch": 9.0, "hips.roll": 1.0,
        "spine1.pitch": 5.0, "spine1.yaw": 7.0, "spine1.roll": 0.0, "spine2.pitch": 5.0, "spine2.yaw": 7.0,
        "neck.pitch": -4.0, "neck.yaw": 0.0, "head.pitch": -12.0, "head.yaw": 3.0,
        "clav.L.raise": 7.0, "clav.R.raise": 5.0, "clav.L.fwd": 6.0,
        "foot.L.pos": foot_at("L", fwd=0.14, side=0.1), "foot.L.yaw": 10.0,
        "foot.R.pos": foot_at("R", fwd=-0.22, side=0.08), "foot.R.yaw": 30.0})
    p.update(fk_arm("L", swing=38.0, out=-2.0, elbow=48.0, twist=24.0, wrist=-14.0, wrist_side=10.0))
    p.update(fk_arm("R", swing=-4.0, out=-10.0, elbow=30.0))
    p.update({"grip.L.fingers": 0.12, "grip.L.thumb": 0.1, "grip.R.fingers": 0.7, "grip.R.thumb": 0.45})
    return p


def startle_keys():
    T = BACK_TRAVEL
    start = idle0()
    rdy = ready_pose()
    flinch = over(start, {
        "hips.loc": hips_at(0.0, 0.79, -0.035), "hips.pitch": 0.0, "spine1.pitch": -5.0, "spine2.pitch": -11.0,
        "neck.pitch": -9.0, "head.pitch": -7.0, "head.yaw": 0.0, "neck.yaw": 0.0,
        "clav.L.raise": 19.0, "clav.R.raise": 19.0, "clav.L.fwd": 7.0, "clav.R.fwd": 7.0,
        "grip.L.fingers": 0.08, "grip.R.fingers": 0.1, "grip.L.thumb": 0.05, "grip.R.thumb": 0.05})
    flinch.update(fk_arm("L", swing=24.0, out=-6.0, elbow=58.0, twist=22.0, wrist=-12.0))
    flinch.update(fk_arm("R", swing=18.0, out=-12.0, elbow=52.0, twist=18.0, wrist=-10.0))
    freeze = over(flinch, {"hips.loc": hips_at(-0.035, 0.775, -0.07), "spine2.pitch": -8.0, "head.pitch": -13.0, "neck.pitch": -5.0,
                           "clav.L.raise": 15.0, "clav.R.raise": 14.0, "hips.roll": -2.0})
    s1 = over(freeze, {"hips.loc": hips_at(-0.03, 0.77, -0.15), "hips.roll": -3.5, "hips.yaw": -12.0, "spine2.pitch": -9.0,
                       "spine1.pitch": -2.0, "head.pitch": -14.0, "clav.L.raise": 11.0, "clav.R.raise": 10.0})
    s1.update(fk_arm("L", swing=16.0, out=34.0, elbow=38.0, twist=20.0, wrist=-10.0))
    s1.update(fk_arm("R", swing=10.0, out=30.0, elbow=44.0, twist=16.0))
    l1 = over(s1, {"hips.loc": hips_at(0.025, 0.755, -0.24), "hips.roll": 3.0, "spine2.pitch": -4.0, "head.pitch": -12.0,
                   "hips.yaw": -14.0})
    s2 = over(l1, {"hips.loc": hips_at(0.02, 0.765, -0.4), "hips.roll": 2.0, "spine1.pitch": 3.0, "spine2.pitch": 2.0,
                   "head.pitch": -15.0})
    s2.update(fk_arm("L", swing=22.0, out=44.0, elbow=30.0, twist=20.0, wrist=-8.0))
    s2.update(fk_arm("R", swing=4.0, out=16.0, elbow=36.0, twist=12.0))
    stumble = over(s2, {"hips.loc": hips_at(-0.045, 0.68, -0.5), "hips.roll": -4.0, "hips.pitch": 12.0, "spine1.pitch": 9.0,
                        "spine2.pitch": 8.0, "neck.pitch": -10.0, "head.pitch": -16.0, "hips.yaw": -18.0})
    stumble.update(fk_arm("L", swing=34.0, out=40.0, elbow=36.0, twist=22.0, wrist=-12.0))
    recover = over(stumble, {"hips.loc": hips_at(-0.02, 0.72, -0.52), "hips.roll": -1.0, "hips.pitch": 9.0, "spine1.pitch": 6.0,
                             "spine2.pitch": 5.0, "head.pitch": -13.0})
    recover.update(fk_arm("L", swing=36.0, out=14.0, elbow=44.0, twist=24.0, wrist=-14.0))
    shuffle = over(rdy, {"hips.loc": rdy["hips.loc"] + G(0.01, -0.005, -T + 0.01)})
    settle = over(rdy, {"hips.loc": rdy["hips.loc"] + G(0.0, -0.016, -T), "spine2.pitch": 7.0, "head.pitch": -14.0})
    end = over(rdy, {"hips.loc": rdy["hips.loc"] + G(0, 0, -T)})
    keys = [(0, start, "stop"), (3, flinch, "snap"), (9, freeze, "smooth"), (17, s1, "smooth"), (22, l1, "smooth"), (29, s2, "smooth"),
            (34, stumble, "smooth"), (40, recover, "smooth"), (47, shuffle, "smooth"), (57, settle, "smooth"),
            (STARTLE_FRAMES, end, "stop")]
    return [(f, shift_pose(P, G(0, 0, T)), m) for f, P, m in keys]


def startle_steps():
    d = G(0, 0, BACK_TRAVEL)
    rdy = ready_pose()
    sL, sR = plant(STANCE, "L"), plant(STANCE, "R")
    eL, eR = plant(rdy, "L"), plant(rdy, "R")
    return Steps({
        "L": ((sL[0] + d, sL[1], 0.0), [
            (13, 22, foot_at("L", fwd=-0.24, side=0.09) + d, 14.0, 0.0, 0.075, "back"),
            (40, 47, eL[0], eL[1], 0.0, 0.035, "back")]),
        "R": ((sR[0] + d, sR[1], 0.0), [(23, 33, eR[0], eR[1], 0.0, 0.1, "back")]),
    })


def startle_extra(poser):
    def extra(f, P):
        # Fast shallow breaths once the shock lands, a tremor in the fending hand, small darting head moves.
        k = smooth01(6, 14, f)
        b = math.sin(2 * math.pi * f / 12.0) * k
        P["spine2.pitch"] -= 1.6 * b
        P["clav.L.raise"] += 1.8 * b
        P["clav.R.raise"] += 1.8 * b
        P["head.yaw"] += k * (3.0 * math.sin(2 * math.pi * f / 31.0) + 1.5 * math.sin(2 * math.pi * f / 13.0))
        P["head.pitch"] += k * 1.2 * math.sin(2 * math.pi * f / 17.0)
        P["arm.L.wrist"] = P.get("arm.L.wrist", 0.0) + k * (1.5 * math.sin(2 * math.pi * f / 4.3) + 1.0 * math.sin(2 * math.pi * f / 2.7))
        # The right hand: free until `reach`, then to the holster, closing on the grip at `grip` (it follows the thigh).
        r = smooth01(26, 39, f)
        if r <= 0.0:
            return
        hol = holster_grip(poser, P)
        approach = M3.shifted(hol, G(-0.035, 0.07, 0.03))
        target = RX.lerp_m(approach, hol, smooth01(34, 40, f))
        fk = achieved(poser, P, "R")
        P["arm.R.mode"], P["arm.R.space"] = "ik", "world"
        P["arm.R.sock"] = RX.lerp_m(fk, target, r)
        P["arm.R.pole"] = M3.HOLSTER_POLE
        P["grip.R.fingers"] = 0.2 + 0.15 * smooth01(28, 38, f) + 0.35 * smooth01(38, 44, f)
        P["grip.R.thumb"] = 0.15 + 0.3 * smooth01(38, 44, f)
    return extra


def startle_clip(poser):
    return Spline(poser, startle_keys(), startle_steps(), startle_extra(poser)).make(STARTLE_FRAMES)(poser)


# ------------------------------------------------------------------------------------------------------------
# Draw_Aim_Nervous + Aim_Hold_Nervous_Loop
# ------------------------------------------------------------------------------------------------------------

DRAW_FRAMES = 45
HOLD_FRAMES = 90
DRAW_EVENTS = {"reach": 0, "grab": 3, "clear": 8, "support": 16, "aimed": 24, "steady": 45}
AIM_ELEV = 9.0


def nervous_aim():
    """The ready stance squared up a little to the threat, a two-handed aim held close (elbows bent: no confident
    lock-out), the muzzle a few degrees up at the big thing, shoulders up."""
    p = over(ready_pose(), {"hips.yaw": -15.0, "spine1.yaw": 8.0, "spine2.yaw": 8.0, "spine2.pitch": 5.0, "spine1.pitch": 4.0,
                            "neck.pitch": 4.0, "head.pitch": -7.0, "head.yaw": 0.0, "clav.L.raise": 7.0, "clav.R.raise": 7.0,
                            "clav.L.fwd": 0.0})
    p.update(RX.aim_at(elev=AIM_ELEV, raise_=-0.05, back=0.07, right=0.02))
    return p


def draw_keys(poser):
    rdy = ready_pose()
    start = M3.with_hand(rdy, "R", holster_grip(poser, rdy), M3.HOLSTER_POLE, 0.7, 0.45)
    grab = M3.with_hand(rdy, "R", holster_grip(poser, rdy), M3.HOLSTER_POLE, 0.92, 0.55)
    up = over(rdy, {"hips.roll": -1.0, "spine2.yaw": 9.0, "clav.R.raise": 8.0, "head.pitch": -10.0})
    clear = M3.with_hand(up, "R", M3.shifted(holster_grip(poser, up), local=(2, -0.17)), M3.HOLSTER_POLE, 0.92, 0.55)
    bring = over(up, {"spine2.yaw": 9.0, "spine2.pitch": 6.0, "head.pitch": -8.0,
                      "arm.R.mode": "ik", "arm.R.space": "chest", "arm.R.sock": sock_frame(G(-0.12, 1.1, 0.24), G(0.05, 0.05, 1.0), UP),
                      "arm.R.pole": G(-0.6, -0.4, -0.8), "grip.R.fingers": 0.92, "grip.R.thumb": 0.55})
    bring.update(fk_arm("L", swing=52.0, out=-6.0, elbow=78.0, twist=28.0, wrist=-10.0))
    bring.update({"grip.L.fingers": 0.4})
    na = nervous_aim()
    push = over(na, {"spine2.pitch": 7.0, "head.pitch": -9.0})
    push.update(RX.aim_at(elev=AIM_ELEV + 6.0, raise_=-0.03, back=0.02, right=0.02))
    return [(0, start, "stop"), (3, grab, "smooth"), (8, clear, "smooth"), (14, bring, "smooth"), (20, push, "smooth"),
            (27, over(na, {"spine2.pitch": 4.0}), "smooth"), (DRAW_FRAMES, na, "stop")]


def draw_extra(f, P):
    # The muzzle shakes once both hands are on it (adrenaline), settling a little towards the end.
    k = smooth01(18, 24, f) * (1.0 - 0.35 * smooth01(30, 45, f))
    tremble = k * (1.1 * math.sin(2 * math.pi * f / 3.7) + 0.6 * math.sin(2 * math.pi * f / 2.3))
    b = math.sin(2 * math.pi * f / 13.0) * smooth01(0, 10, f) * (1.0 - smooth01(36, 45, f))
    P["spine2.pitch"] -= 1.4 * b
    P["clav.L.raise"] += 1.5 * b
    P["clav.R.raise"] += 1.5 * b
    if k > 0.0:
        for name in SIDE:
            if P.get(f"arm.{name}.mode") == "ik":
                RX.nudge_socket(P, name, G(0.0, 0.0015 * tremble, 0.0), pitch=tremble)


def draw_clip(poser):
    return Spline(poser, draw_keys(poser), None, draw_extra).make(DRAW_FRAMES)(poser)


def hold_clip(poser):
    na = nervous_aim()

    def spec(t, f):
        w = 2 * math.pi * f / HOLD_FRAMES
        P = dict(na)
        b = math.sin(3 * w)
        P["spine2.pitch"] -= 1.8 * b
        P["clav.L.raise"] += 2.4 * b
        P["clav.R.raise"] += 2.4 * b
        P["hips.loc"] = na["hips.loc"] + G(0.012 * math.sin(w), 0.004 * b, 0.0)
        P["hips.roll"] += 1.3 * math.sin(w)
        P["spine1.roll"] = P.get("spine1.roll", 0.0) - 0.9 * math.sin(w)
        drift = 2.4 * math.sin(w) + 1.1 * math.sin(2 * w)
        P["spine2.yaw"] += drift
        P["head.yaw"] += 0.4 * drift + 1.5 * math.sin(4 * w)
        P["head.pitch"] += 1.0 * math.sin(5 * w)
        tremble = 0.8 * math.sin(23 * w) + 0.45 * math.sin(37 * w) + 0.6 * math.sin(2 * w)
        for name in SIDE:
            RX.nudge_socket(P, name, G(0.0, 0.003 * b + 0.0012 * math.sin(29 * w), 0.0), pitch=tremble)
        return BM.compose(poser, P)
    return HOLD_FRAMES, spec, True


CLIP_FNS = {"Capsule_Exit": exit_clip, "Turn_Look_L": turn_clip(1.0), "Turn_Look_R": turn_clip(-1.0),
            "Startle_Backstep": startle_clip, "Draw_Aim_Nervous": draw_clip, "Aim_Hold_Nervous_Loop": hold_clip}


def key_acting(arm):
    for name in CLIP_FNS:
        act = bpy.data.actions.get(name)
        if act is not None:
            bpy.data.actions.remove(act)
    acts = C.key_clips(C.Poser(arm), CLIP_FNS)
    arm.animation_data.action = bpy.data.actions["Idle"]
    return acts


# ------------------------------------------------------------------------------------------------------------
# Previews: key frames of each clip, with the capsule hatch as a stand-in block
# ------------------------------------------------------------------------------------------------------------

# Face tracks per clip: {shape: [(frame, weight), ...]} (smooth between keys). Unity drives the blend shapes with
# these (they are in the report, in seconds); the previews use them too.
FACE_TRACKS = {
    "Capsule_Exit": {
        "Breath": [(0, 0.5), (6, 0.8), (14, 0.2), (24, 0.65), (34, 0.15), (44, 0.55), (58, 0.0)],
        "Determined": [(14, 0.0), (22, 0.35), (40, 0.35), (56, 0.0)],
        "Blink": [(58, 0.0), (60, 1.0), (63, 0.0)],
        "LookL": [(67, 0.0), (71, 1.0), (75, 0.9), (78, 0.0)],
        "LookR": [(77, 0.0), (80, 1.0), (85, 0.9), (89, 0.0)]},
    "Turn_Look_L": {"LookL": [(0, 0.0), (3, 1.0), (14, 0.6), (24, 0.0)], "Blink": [(3, 0.0), (5, 1.0), (7, 0.0)]},
    "Turn_Look_R": {"LookR": [(0, 0.0), (3, 1.0), (14, 0.6), (24, 0.0)], "Blink": [(3, 0.0), (5, 1.0), (7, 0.0)]},
    "Startle_Backstep": {
        "Surprise": [(0, 0.0), (3, 1.0), (8, 0.35), (13, 0.0)],
        "Fear": [(2, 0.0), (8, 0.85), (20, 1.0), (75, 0.9)],
        "Breath": [(1, 0.0), (3, 0.2), (8, 0.9), (12, 0.4), (18, 0.7), (24, 0.35), (30, 0.65), (36, 0.3), (48, 0.45), (60, 0.25), (75, 0.35)],
        "LookL": [(41, 0.0), (44, 0.5), (49, 0.0)],
        "LookR": [(49, 0.0), (52, 0.45), (58, 0.0)]},
    "Draw_Aim_Nervous": {
        "Fear": [(0, 0.9), (24, 0.75), (45, 0.65)],
        "Breath": [(0, 0.35), (10, 0.55), (24, 0.3), (45, 0.25)],
        "Determined": [(14, 0.0), (45, 0.25)]},
    "Aim_Hold_Nervous_Loop": {
        "Fear": [(0, 0.65), (45, 0.55), (90, 0.65)],
        "Determined": [(0, 0.25), (45, 0.35), (90, 0.25)],
        "Breath": [(0, 0.25), (8, 0.5), (20, 0.25), (38, 0.5), (50, 0.25), (68, 0.5), (80, 0.25), (90, 0.25)],
        "Blink": [(52, 0.0), (54, 1.0), (57, 0.0)]},
}


def track_value(keys, f):
    if f <= keys[0][0]:
        return keys[0][1]
    for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
        if f <= f1:
            return v0 + (v1 - v0) * ease((f - f0) / max(f1 - f0, 1e-6))
    return keys[-1][1]


def face_at(clip, f):
    return {n: track_value(k, f) for n, k in FACE_TRACKS.get(clip, {}).items()}


# clip: (frames, weapon mode, camera target, distance, views [(azimuth, elevation)])
SHEETS = {
    "Capsule_Exit": ((0, 9, 18, 24, 28, 34, 42, 48, 54, 62, 66, 72, 81, 90), "none", (0.0, 0.35, 0.75), 3.6, ((-80, 8), (-28, 10))),
    "Startle_Backstep": ((0, 3, 9, 14, 17, 22, 26, 29, 34, 40, 47, 57, 66, 75), "none", (0.0, -0.2, 0.85), 3.4, ((-75, 6), (-25, 8))),
    "Draw_Aim_Nervous": ((0, 3, 6, 8, 11, 14, 17, 20, 24, 27, 33, 45), "draw", (0.0, 0.0, 1.0), 2.7, ((-40, 6), (-90, 4))),
    "Aim_Hold_Nervous_Loop": ((0, 15, 30, 45, 60, 75), "pistol", (0.0, 0.0, 1.05), 2.5, ((-35, 6),)),
    "Turn_Look_L": ((0, 3, 6, 9, 12, 15, 18, 24), "none", (0.0, 0.0, 0.9), 3.0, ((-20, 12),)),
    "Turn_Look_R": ((0, 3, 6, 9, 12, 15, 18, 24), "none", (0.0, 0.0, 0.9), 3.0, ((-20, 12),)),
}


def hatch_proxy():
    """A grey stand-in for the capsule's sill, open ramp and door posts (root at the Exit socket)."""
    objs = []
    mat = C.principled("HatchProxy", color=(0.35, 0.37, 0.4), rough=0.6)

    def box(name, centre, size, rot=None):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=centre)
        o = bpy.context.active_object
        o.name = name
        o.scale = size
        if rot is not None:
            o.rotation_euler = rot
        o.data.materials.append(mat)
        objs.append(o)
    y_sill = HATCH_BACK
    box("Sill", (0, y_sill + 0.25, HATCH_UP / 2 - 0.01), (0.5, 0.5, HATCH_UP))
    run = math.hypot(RAMP_RUN, HATCH_UP)
    ang = math.atan2(HATCH_UP, RAMP_RUN)
    box("Ramp", (0, y_sill - RAMP_RUN / 2, HATCH_UP / 2 - 0.02), (0.42, run, 0.03), (ang, 0, 0))
    for x in (-DOOR_POST_X - 0.03, DOOR_POST_X + 0.03):
        box("Post", (x, y_sill - 0.14 + 0.03, HATCH_UP + 0.38 * CAPSULE_SCALE), (0.04, 0.04, 0.76 * CAPSULE_SCALE))
    box("Lintel", (0, y_sill - 0.11, HATCH_UP + 0.77 * CAPSULE_SCALE), (2 * DOOR_POST_X + 0.1, 0.06, 0.04))
    return objs


def clip_sheets(blend, out_dir, prefix, cell=330, only=None):
    import mercer_previews as MP
    import spencer_previews5 as SP5
    paths = []
    for clip, (frames, mode, target, dist, views) in SHEETS.items():
        if only and clip not in only:
            continue
        rig, acts, _ = SP5.open_scene(blend)
        proxies = hatch_proxy() if clip == "Capsule_Exit" else []
        cells = []
        for az, el in views:
            for f in frames:
                m = mode
                if mode == "draw":
                    m = "pistol" if f >= DRAW_EVENTS["grab"] else "none"
                MP.pose(rig, acts[clip], f, m)
                set_face(face_at(clip, f))
                cam = C.closeup_camera(Vector(target), dist, azimuth=az, elevation=el, lens=50)
                cells.append(C.render(cam, os.path.join(out_dir, f"_c_{clip}_{az}_{f}.png"), cell, int(cell * 1.2), samples=16))
                bpy.data.objects.remove(cam)
        set_face({})
        path = os.path.join(out_dir, f"{prefix}_{clip}.png")
        C.contact_sheet(cells, path, len(frames), cell)
        for p in cells:
            os.remove(p)
        for o in proxies:
            bpy.data.objects.remove(o)
        paths.append(path)
        C.log("clip sheet", path)
    return paths


# ------------------------------------------------------------------------------------------------------------
# Checks, export, report
# ------------------------------------------------------------------------------------------------------------

PISTOL_SOCKET = {"Startle_Backstep": lambda f: "Holster_R", "Capsule_Exit": lambda f: "Holster_R",
                 "Turn_Look_L": lambda f: "Holster_R", "Turn_Look_R": lambda f: "Holster_R",
                 "Draw_Aim_Nervous": lambda f: "Weapon_R" if f >= DRAW_EVENTS["grab"] else "Holster_R",
                 "Aim_Hold_Nervous_Loop": lambda f: "Weapon_R",
                 # Baselines: the holstered pistol always sits in the holster mesh; the shipped draw pulls it out the same way.
                 "Idle": lambda f: "Holster_R", "Draw_Pistol": lambda f: "Weapon_R" if f >= 7 else "Holster_R",
                 "Startled_Hold": lambda f: "Weapon_R"}
HAND_ZONE = 0.11                  # body triangles this close to the right hand's socket are the gripping hand
HOLSTER_ZONE = 0.12               # ... and this close to the holstered pistol, the holster it sits in


def pistol_check(step=2):
    """The pistol (at its socket) against the body mesh, ignoring the gripping hand and the holster: any other overlap
    is a clip (forearm, thigh, tabard, pack). Returns {clip: [(frame, overlapping pairs)]}."""
    arm, body, pistol = bpy.data.objects[NAME + "_Rig"], bpy.data.objects[NAME], bpy.data.objects["Pistol"]
    pfaces = [tuple(p.vertices) for p in pistol.data.polygons]
    out = {}
    for clip, sock_of in PISTOL_SOCKET.items():
        act = bpy.data.actions[clip]
        f1 = int(round(act.frame_range[1]))
        hits = []
        for f in range(0, f1 + 1, step):
            C.set_action(arm, act, f)
            bpy.context.view_layer.update()
            sock = sock_of(f)
            m = arm.matrix_world @ arm.pose.bones[sock].matrix
            pt = BVHTree.FromPolygons([m @ v.co for v in pistol.data.vertices], pfaces)
            dg = bpy.context.evaluated_depsgraph_get()
            ev = body.evaluated_get(dg)
            me = ev.to_mesh()
            bco = [body.matrix_world @ v.co for v in me.vertices]
            polys = [tuple(p.vertices) for p in me.polygons]
            ev.to_mesh_clear()
            centre = m.translation
            hand = (arm.matrix_world @ arm.pose.bones["Weapon_R"].matrix).translation
            keep = [p for p in polys if (bco[p[0]] - centre).length < 0.4 and (bco[p[0]] - hand).length > HAND_ZONE
                    and (sock == "Weapon_R" or (bco[p[0]] - centre).length > HOLSTER_ZONE)]
            if keep:
                n = len(pt.overlap(BVHTree.FromPolygons(bco, keep)))
                if n:
                    hits.append((f, n))
        out[clip] = hits
        C.log("pistol check", clip, hits or "clean")
    return out


def copy_maps(stage):
    """The maps, the Low set and the weapons folder, unchanged (the FBX and the report are written fresh)."""
    for root, _, files in os.walk(SRC_EXPORT):
        rel = os.path.relpath(root, SRC_EXPORT)
        for fn in files:
            if rel == "." and fn in (NAME + ".fbx", NAME + "_report.json"):
                continue
            dst = os.path.join(stage, rel, fn)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(os.path.join(root, fn), dst)


def export(stage):
    arm = bpy.data.objects[NAME + "_Rig"]
    objs = [arm, bpy.data.objects[NAME], bpy.data.objects[BM.LOD_NAME]] + M5.extra_objects()
    for o in objs[1:]:
        S.drop_layer_attr(o)
        if o.data.shape_keys:
            for k in o.data.shape_keys.key_blocks:
                k.value = 0.0
    C.set_action(arm, bpy.data.actions["Idle"], 0)
    C.export_fbx(os.path.join(stage, NAME + ".fbx"), objs, animated=True)


def clip_entry(frames, loop, events, **more):
    d = {"frames": frames, "seconds": round(frames / FPS, 3), "loop": loop, "events": events,
         "events_normalized": {k: round(v / frames, 3) for k, v in events.items()}}
    d.update(more)
    return d


def acting_report(stage, stats, checks):
    with open(os.path.join(SRC_EXPORT, NAME + "_report.json"), encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["round"] = ROUND
    rep["actions"] = {a.name: int(round(a.frame_range[1])) for a in sorted(bpy.data.actions, key=lambda a: a.name)}
    rep["events"].update({"Capsule_Exit": {"step_down": EXIT_EVENTS["step_down"]}, "Startle_Backstep": {"flinch": STARTLE_EVENTS["flinch"]},
                          "Draw_Aim_Nervous": {"grab": DRAW_EVENTS["grab"]}, "Turn_Look_L": {"plant_lead": TURN_EVENTS["plant_lead"]},
                          "Turn_Look_R": {"plant_lead": TURN_EVENTS["plant_lead"]}})
    rep["face"]["shape_keys"] = {n: SHAPE_NOTES[n] for n in ["Blink"] + list(SHAPES)}
    rep["face"]["shape_keys_lod1"] = ["Blink"] + list(SHAPES)
    rep["face"]["shape_range"] = ("0..1 each (Unity blend shape weight 0..100); they add up: Breath layers over Fear, Surprise "
                                  "or Determined; keep Fear + Surprise <= 1.2 together")
    rep["face"]["shape_max_travel_mm"] = stats
    clips = {
        "Capsule_Exit": clip_entry(
            EXIT_FRAMES, False, EXIT_EVENTS, starts="crouched on the hatch sill, both hands on the door posts", ends="Idle frame 0",
            weapon="pistol in Holster_R", root="the capsule's Exit socket (same facing); the root never moves",
            start_offset={"feet_up_m": HATCH_UP, "feet_behind_root_m": [round(HATCH_BACK + 0.1, 3), round(HATCH_BACK + 0.16, 3)],
                          "note": f"frame 0 stands him on the hatch sill, {HATCH_UP:.2f} m up and about {HATCH_BACK + 0.1:.2f}-"
                                  f"{HATCH_BACK + 0.16:.2f} m behind the root (Unity -Z), for Cutscene_Capsule at scale {CAPSULE_SCALE}. "
                                  "The ramp must be fully open (Cutscene_Capsule 'Open' finished) before frame 0."}),
        "Turn_Look_L": clip_entry(
            TURN_FRAMES, False, TURN_EVENTS, starts="Idle frame 0, turned 90 deg to his right of the root", ends="Idle frame 0",
            weapon="any (arms free)", rotation_deg=TURN_DEG, turn="to his left",
            root="at frame 0 turn the root 90 deg to his left (Unity: transform.Rotate(0, -90, 0)); the body starts turned back by "
                 "that much, so nothing pops"),
        "Turn_Look_R": clip_entry(
            TURN_FRAMES, False, TURN_EVENTS, starts="Idle frame 0, turned 90 deg to his left of the root", ends="Idle frame 0",
            weapon="any (arms free)", rotation_deg=-TURN_DEG, turn="to his right",
            root="at frame 0 turn the root 90 deg to his right (Unity: transform.Rotate(0, +90, 0))"),
        "Startle_Backstep": clip_entry(
            STARTLE_FRAMES, False, STARTLE_EVENTS, starts="Idle frame 0, 0.5 m in front of the root",
            ends="Draw_Aim_Nervous frame 0 (low ready, left hand fending, right hand resting on the holstered grip)",
            weapon="pistol stays in Holster_R", travel_m=BACK_TRAVEL,
            root="at frame 0 move the root 0.5 m backwards (local -Z); the body starts 0.5 m in front of it, so nothing pops"),
        "Draw_Aim_Nervous": clip_entry(
            DRAW_FRAMES, False, DRAW_EVENTS, starts="Startle_Backstep last frame", ends="Aim_Hold_Nervous_Loop frame 0",
            weapon="Pistol (+ Pistol_Mag)", from_socket="Holster_R", to_socket="Weapon_R",
            unity="reparent the pistol to Weapon_R (identity local transform) at 'grab', as Draw_Pistol"),
        "Aim_Hold_Nervous_Loop": clip_entry(HOLD_FRAMES, True, {}, starts="Draw_Aim_Nervous last frame", ends="loops",
                                            weapon="pistol in Weapon_R"),
    }
    for name, d in clips.items():
        d["face"] = {shape: [[round(f / FPS, 3), w] for f, w in keys] for shape, keys in FACE_TRACKS.get(name, {}).items()}
    rep["acting"] = {
        "clips": clips,
        "root_motion": "none: Root is never keyed. Travel and turns: move or turn the root once at frame 0 of Startle_Backstep / "
                       "Turn_Look_*; those clips start offset by exactly that amount. Start them with Play (no cross-fade): their "
                       "frame 0 matches the previous clip's end pose in the world.",
        "face_tracks": "per clip {shape: [[seconds, weight 0..1], ...]}, smoothstep between keys; drive "
                       "SkinnedMeshRenderer.SetBlendShapeWeight(index, weight * 100) on Mercer_Face (and Mercer_Face_LOD1). "
                       "Not baked into the FBX.",
        "hatch": {"sill_height_m": round(HATCH_UP, 4), "sill_behind_exit_m": round(HATCH_BACK, 4), "ramp_run_m": round(RAMP_RUN, 4),
                  "capsule_scale": CAPSULE_SCALE,
                  "capsule": f"Cutscene_Capsule at scale {CAPSULE_SCALE} (scale 1: ramp hinge 0.46 m up, Exit socket 1.35 m in front "
                             "of the centre)"},
        "checks": checks,
    }
    path = os.path.join(stage, NAME + "_report.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", path)


DETAIL = (("Startle_Backstep", (17, 26, 34, 40, 47, 57, 75)), ("Draw_Aim_Nervous", (0, 3, 6, 8, 11)), ("Capsule_Exit", (0, 18, 28, 42)))


def detail_sheet(blend, out_dir, prefix, cell=420):
    """Close on the right hip, hand, holster and tabard (the clipping-prone spots), from his right front and right back."""
    import mercer_previews as MP
    import spencer_previews5 as SP5
    rig, acts, _ = SP5.open_scene(blend)
    cells = []
    for az in (-60, -125):
        for clip, frames in DETAIL:
            for f in frames:
                m = "pistol" if clip == "Draw_Aim_Nervous" and f >= DRAW_EVENTS["grab"] else "none"
                MP.pose(rig, acts[clip], f, m)
                bpy.context.view_layer.update()
                hips = rig.arm.matrix_world @ rig.arm.pose.bones["Hips"].head
                cam = C.closeup_camera(hips + Vector((0, 0, 0.05)), 1.5, azimuth=az, elevation=10, lens=50)
                cells.append(C.render(cam, os.path.join(out_dir, f"_d_{clip}_{az}_{f}.png"), cell, cell, samples=16))
                bpy.data.objects.remove(cam)
    path = os.path.join(out_dir, f"{prefix}_detail_hip.png")
    C.contact_sheet(cells, path, len(cells) // 2, cell)
    for p in cells:
        os.remove(p)
    C.log("detail sheet", path)
    return path


def render_only(args):
    blend = os.path.join(args.get("stage") or STAGE, "blend", NAME + ".blend")
    out = args.get("out") or os.path.join(C.REPO, "ArtSource", "Previews", "QA")
    os.makedirs(out, exist_ok=True)
    prefix = args.get("prefix", "spencer_acting")
    only = args["only"].split(",") if args.get("only") else None
    if not only:
        face_sheet(blend, out, prefix)
        detail_sheet(blend, out, prefix)
    clip_sheets(blend, out, prefix, only=only)


def main():
    args = C.cli_args()
    if args.get("renderonly"):
        render_only(args)
        return
    stage = args.get("stage") or STAGE
    # --src: the pass-4 blend to build on (round 40: ArtSource/Blend/Mercer.blend already carries the acting pass).
    bpy.ops.wm.open_mainfile(filepath=args.get("src") or SRC_BLEND)
    stats = build_face_shapes(args.get("units") or os.path.join(stage, "blend", "SpencerFace_units.npz"))
    arm = bpy.data.objects[NAME + "_Rig"]
    key_acting(arm)
    checks = {}
    if not args.get("nocheck"):
        checks["face"] = face_clearance()
        checks["pistol_overlaps"] = pistol_check()
    os.makedirs(os.path.join(stage, "blend"), exist_ok=True)
    copy_maps(stage)
    export(stage)
    acting_report(stage, stats, checks)
    bpy.context.preferences.filepaths.save_version = 0
    blend = os.path.join(stage, "blend", NAME + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend, compress=True)
    C.log("shape stats", stats)
    only = args["only"].split(",") if args.get("only") else None
    if args.get("sheets"):
        clip_sheets(blend, args["sheets"], args.get("prefix", "spencer_acting"), only=only)
    if args.get("qa"):
        qa = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
        face_sheet(blend, qa, "spencer_acting")
        clip_sheets(blend, qa, "spencer_acting", only=only)


if __name__ == "__main__":
    main()
