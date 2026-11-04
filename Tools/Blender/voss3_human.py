"""Dr. Voss round 20: the MPFB 2 (MakeHuman for Blender) stage. Requires the MPFB extension and the CC0 asset packs
`makehuman_system_assets`, `hair01` and `faceunits01` (install: Docs/COMMANDS.md, "Blender: MPFB 2").

`capture()` builds the human in MPFB (phenotype, stylisation targets, skin, eyes, eyebrows, eyelashes, teeth, hair bun,
shoes, default rig), records the face expressions from the faceunits01 targets (refitting every proxy to them), poses
her (hands in the coat pockets) and returns everything as plain numpy data in Blender space, scaled so her eyes sit
on game height EYE_Y. After this nothing touches MPFB objects again; build_voss3.py assembles its own mesh from it.
"""

import math
import os

import addon_utils
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

import common as C

MPFB = "bl_ext.blender_org.mpfb"
EYE_Y = 1.44                       # game height of the eye centres (the bust framing in Unity is aimed at this)

PHENOTYPE = dict(gender=0.0, age=0.615, muscle=0.5, weight=0.52, proportions=0.75, height=0.55, cupsize=0.52,
                 firmness=0.55)    # age 0.615 ~ 40 years in MakeHuman's scale (0.5 = 25, 1.0 = 90)
RACE = {"asian": 0.05, "caucasian": 0.9, "african": 0.05}

# Mild stylisation through MakeHuman's own targets: a soft oval face, a rounder natural chin, slightly larger eyes, a
# finer nose, fuller lips with corners a touch up (friendly at rest), a slightly larger head for a readable bust.
TARGETS = [
    ("head-oval", 0.55), ("chin-width-incr", 0.06), ("chin-prominent-incr", 0.05), ("chin-jaw-drop-decr", 0.15),
    ("chin-bones-decr", 0.15),
    ("l-cheek-volume-incr", 0.05), ("r-cheek-volume-incr", 0.05), ("l-cheek-bones-incr", 0.2), ("r-cheek-bones-incr", 0.2),
    ("l-eye-scale-incr", 0.24), ("r-eye-scale-incr", 0.24), ("l-eye-bag-decr", 0.4), ("r-eye-bag-decr", 0.4),
    ("nose-scale-horiz-decr", 0.12), ("nose-point-width-decr", 0.2), ("nose-nostrils-width-decr", 0.15),
    ("nose-hump-decr", 0.3),
    ("mouth-upperlip-volume-decr", 0.25), ("mouth-lowerlip-volume-decr", 0.3), ("mouth-angles-up", 0.2),
    ("mouth-scale-depth-decr", 0.35),
    ("mouth-laugh-lines-in", 0.4), ("mouth-scale-horiz-decr", 0.06),
    ("forehead-scale-vert-decr", 0.3), ("neck-scale-horiz-decr", 0.05),
]

ASSETS = {"eyes": "high-poly/high-poly.mhclo", "eyebrows": "eyebrow010/eyebrow010.mhclo",
          "eyelashes": "eyelashes02/eyelashes02.mhclo", "teeth": "teeth_base/teeth_base.mhclo",
          "hair": "rehmanpolanski_hair_bun_brown/rehmanpolanski_hair_bun_brown.mhclo"}
CLOTHES = ["shoes04/shoes04.mhclo"]
SKIN = "young_caucasian_female/young_caucasian_female.mhmat"

# Face expressions from faceunits01 (ARKit-style units); weights per unit.
EXPRESSIONS = {
    "Blink": {"eyeBlinkLeft": 1.0, "eyeBlinkRight": 1.0},
    "JawOpen": {"jawOpen": 0.7},
    "Smile": {"mouthSmileLeft": 0.75, "mouthSmileRight": 0.75, "cheekSquintLeft": 0.35, "cheekSquintRight": 0.35},
    "Frown": {"mouthFrownLeft": 0.7, "mouthFrownRight": 0.7, "browInnerUp": 0.35, "mouthPressLeft": 0.2,
              "mouthPressRight": 0.2},
}

# MPFB default-rig bones -> the game's Voss_Rig bones.
JAW_BONES = {"jaw", "special04", "oris01", "oris02", "oris06.L", "oris06.R", "oris07.L", "oris07.R"}
ARM_PARTS = ("clavicle", "shoulder", "upperarm", "lowerarm", "wrist", "finger", "metacarpal")
LEG_PARTS = ("root", "pelvis", "upperleg", "lowerleg", "foot", "toe")


def game_bone(name):
    if name in JAW_BONES or name.startswith("tongue"):
        return "Jaw"
    if name.startswith(LEG_PARTS):
        return "Root"
    if name in ("spine05", "spine04", "spine03"):
        return "Spine"
    if name in ("spine02", "spine01") or name.startswith("breast"):
        return "Chest"
    if name.startswith(ARM_PARTS):
        return "Clavicle_L" if name.endswith(".L") else "Clavicle_R"
    if name.startswith("neck"):
        return "Neck"
    return "Head"


GAME_BONES = ["Root", "Spine", "Chest", "Clavicle_L", "Clavicle_R", "Neck", "Head", "Jaw", "Lid_L", "Lid_R"]

# The pose: hands in the hip pockets of the coat. Wrist targets in MPFB space (before scaling), her left (+x).
WRIST_TARGET = Vector((0.172, -0.092, 0.905))
ELBOW_POLE = Vector((0.55, 0.85, 0.0))
LEG_IN = 3.0                       # degrees


def enable():
    addon_utils.enable(MPFB, default_set=True)
    from bl_ext.blender_org.mpfb.services.humanservice import HumanService  # noqa: F401  (import check)


def _services():
    from bl_ext.blender_org.mpfb.services import ClothesService, HumanService, LocationService, TargetService
    return HumanService, TargetService, ClothesService, LocationService


def make_human():
    HumanService, _, _, _ = _services()
    info = HumanService._create_default_human_info_dict()
    info["phenotype"].update(PHENOTYPE)
    info["phenotype"]["race"] = dict(RACE)
    info.update(ASSETS)
    info["clothes"] = list(CLOTHES)
    info["skin_mhmat"] = SKIN
    info["skin_material_type"] = "MAKESKIN"
    info["eyes_material_type"] = "MAKESKIN"
    info["targets"] = [dict(target=t, value=v) for t, v in TARGETS]
    info["rig"] = "default"
    settings = HumanService.get_default_deserialization_settings()
    settings["subdiv_levels"] = 0
    body = HumanService.deserialize_from_dict(info, settings)
    rig = body.parent
    proxies = {}
    for o in bpy.context.scene.objects:
        if o.type != "MESH" or o is body:
            continue
        for key, frag in list(ASSETS.items()) + [("shoes", CLOTHES[0])]:
            stem = os.path.splitext(os.path.basename(frag))[0]
            if o.name.endswith(stem):
                proxies[key] = o
    C.log("mpfb human", body.name, len(body.data.vertices), "proxies", {k: v.name for k, v in proxies.items()})
    return body, rig, proxies


def _evaluated(obj):
    """Evaluated vertices, polygons and corner UVs (with the mask and armature modifiers applied)."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    n = len(me.vertices)
    co = np.zeros(n * 3, np.float32)
    me.vertices.foreach_get("co", co)
    co = co.reshape(n, 3).astype(float)
    mw = np.array(obj.matrix_world)
    co = co @ mw[:3, :3].T + mw[:3, 3]
    sizes = np.zeros(len(me.polygons), int)
    me.polygons.foreach_get("loop_total", sizes)
    loops = np.zeros(len(me.loops), int)
    me.loops.foreach_get("vertex_index", loops)
    uv = np.zeros((len(me.loops), 2))
    if me.uv_layers:
        me.uv_layers[0].data.foreach_get("uv", uv.ravel())
    src = np.arange(n)
    if "SrcIdx" in me.attributes:
        a = np.zeros(n, np.int32)
        me.attributes["SrcIdx"].data.foreach_get("value", a)
        src = a.astype(int)
    ev.to_mesh_clear()
    return co, sizes, loops, uv, src


def _tag_sources(obj):
    me = obj.data
    if "SrcIdx" in me.attributes:
        me.attributes.remove(me.attributes["SrcIdx"])
    a = me.attributes.new("SrcIdx", "INT", "POINT")
    a.data.foreach_set("value", np.arange(len(me.vertices), dtype=np.int32))


def _weights(obj):
    """{group name: (n,) weights} for the object's (original) vertices."""
    n = len(obj.data.vertices)
    names = [g.name for g in obj.vertex_groups]
    out = {}
    for v in obj.data.vertices:
        for g in v.groups:
            if g.weight <= 0:
                continue
            nm = names[g.group]
            if nm not in out:
                out[nm] = np.zeros(n)
            out[nm][v.index] = g.weight
    return out


def _game_weights(groups, deform):
    n = len(next(iter(groups.values())))
    W = {b: np.zeros(n) for b in GAME_BONES}
    for name, w in groups.items():
        if name in deform:
            W[game_bone(name)] += w
    total = sum(W.values())
    empty = total < 1e-6
    W["Head"][empty] = 1.0
    total = np.where(empty, 1.0, total)
    return {b: w / total for b, w in W.items()}


def _set_expression(body, keys, units, value):
    for unit in units:
        kb = keys.get(unit)
        if kb is not None:
            kb.value = value * units[unit]


def _refit(body, proxies):
    _, _, ClothesService, _ = _services()
    for p in proxies.values():
        ClothesService.fit_clothes_to_human(p, body, set_parent=False)


def capture_expressions(body, proxies, objs):
    """{expression: {object key: (n_eval, 3) delta}} in MPFB space, the rig at rest."""
    _, TargetService, _, LocationService = _services()
    folder = os.path.join(LocationService.get_user_data("targets"), "faceunits")
    units = sorted({u for spec in EXPRESSIONS.values() for u in spec})
    keys = {}
    for u in units:
        path = os.path.join(folder, u + ".target")
        if not os.path.exists(path):
            C.log("faceunit missing", u)
            continue
        keys[u] = TargetService.load_target(body, path, weight=0.0, name="fu_" + u)
    rest = {k: _evaluated(o)[0] for k, o in objs.items()}
    out = {}
    for name, spec in EXPRESSIONS.items():
        _set_expression(body, keys, spec, 1.0)
        _refit(body, proxies)
        out[name] = {k: _evaluated(o)[0] - rest[k] for k, o in objs.items()}
        _set_expression(body, keys, spec, 0.0)
        C.log("expression", name, "max", {k: round(float(np.abs(d).max()), 4) for k, d in out[name].items()})
    _refit(body, proxies)
    for kb in keys.values():
        body.shape_key_remove(kb)
    return out


def _align(pb, rig, direction):
    """Rotates pose bone `pb` (about its current head) so its axis points along `direction` (armature space)."""
    bpy.context.view_layer.update()
    m = pb.matrix.copy()
    cur = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    q = cur.rotation_difference(direction.normalized())
    head = m.translation.copy()
    new = q.to_matrix().to_4x4() @ m
    new.translation = head
    pb.matrix = new
    bpy.context.view_layer.update()


def pose(rig):
    """Hands in the hip pockets: upper arms hang down and a little back, forearms run forward to the pockets.
    Fingers curl slightly (the hands are hidden in the pockets, but the cuffs read the wrist angle)."""
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for side, s in (("L", 1), ("R", -1)):
        sh = rig.pose.bones["upperarm01." + side]
        el = rig.pose.bones["lowerarm01." + side]
        wr = rig.pose.bones["wrist." + side]
        root = sh.head.copy()
        l1 = (rig.data.bones["lowerarm01." + side].head_local - rig.data.bones["upperarm01." + side].head_local).length
        l2 = (rig.data.bones["wrist." + side].head_local - rig.data.bones["lowerarm01." + side].head_local).length
        target = Vector((WRIST_TARGET.x * s, WRIST_TARGET.y, WRIST_TARGET.z))
        pole = Vector((ELBOW_POLE.x * s, ELBOW_POLE.y, ELBOW_POLE.z))
        elbow, wrist = C.two_bone(root, target, l1, l2, pole)
        _align(sh, rig, elbow - root)
        _align(el, rig, wrist - elbow)
        hand_dir = (wrist - elbow).normalized() * 0.6 + Vector((0, 0, -1)) * 0.4
        _align(wr, rig, hand_dir)
        # Feet a little closer than MakeHuman's A-stance: the thighs turn in about the hip.
        leg = rig.pose.bones["upperleg01." + side]
        axis = rig.data.bones[leg.name].matrix_local.to_3x3().inverted() @ Vector((0, 1, 0))
        leg.rotation_quaternion = Quaternion(axis.normalized(), math.radians(LEG_IN * s)) @ leg.rotation_quaternion
        for pb in rig.pose.bones:
            if pb.name.startswith("finger") and pb.name.endswith("." + side) and not pb.name.startswith("finger1"):
                pb.rotation_quaternion = Matrix.Rotation(math.radians(22), 4, "X").to_quaternion()
    bpy.context.view_layer.update()


class Captured:
    """One mesh of the captured human (Blender space, final scale)."""

    def __init__(self, key, co, sizes, loops, uv, src, weights, texture, groups=None):
        self.key = key
        self.co = co
        self.sizes = sizes
        self.loops = loops
        self.uv = uv
        self.src = src
        self.weights = weights            # {game bone: (n,)}
        self.texture = texture
        self.groups = groups or {}        # {vertex group name: (n,)} (the body's extra groups: ears, lips, scalp ...)
        self.deltas = {}                  # {expression: (n, 3)}

    def faces(self):
        out, k = [], 0
        for s in self.sizes:
            out.append(tuple(self.loops[k:k + s]))
            k += s
        return out

    def corner_uv(self):
        out, k = [], 0
        for s in self.sizes:
            out.append([tuple(self.uv[k + i]) for i in range(s)])
            k += s
        return out


def _texture_of(obj):
    for m in obj.data.materials:
        if m is None or not m.use_nodes:
            continue
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image is not None and "diffuse" in n.name.lower() + n.label.lower() + n.image.name.lower():
                return bpy.path.abspath(n.image.filepath)
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE" and n.image is not None:
                return bpy.path.abspath(n.image.filepath)
    return None


def capture():
    """Builds the MPFB human and returns ({key: Captured}, info) at final scale (eyes on EYE_Y)."""
    enable()
    body, rig, proxies = make_human()
    objs = dict(proxies)
    objs["body"] = body
    for o in objs.values():
        _tag_sources(o)
    deform = {b.name for b in rig.data.bones if b.use_deform}
    exprs = capture_expressions(body, proxies, objs)
    rest_eyes = _evaluated(proxies["eyes"])[0]
    pose(rig)
    eye_z = float(rest_eyes[:, 2].mean())
    scale = EYE_Y / eye_z
    C.log("eye height", round(eye_z, 4), "scale", round(scale, 4), "stature", round(body.dimensions.z * scale, 3))
    out = {}
    for key, o in objs.items():
        co, sizes, loops, uv, src = _evaluated(o)
        groups = _weights(o)
        W = _game_weights(groups, deform)
        W = {b: w[src] for b, w in W.items()}
        extra = {}
        if key == "body":
            for g in ("ears", "lips", "scalp", "eyebrows", "head", "neck", "fingernails", "toenails"):
                if g in groups:
                    extra[g] = groups[g][src]
            for part in ("hand", "arm", "leg", "foot"):
                extra["_" + part] = np.zeros(len(src))
            for name, w in groups.items():
                if name not in deform:
                    continue
                if name.startswith(("wrist", "finger", "metacarpal")):
                    extra["_hand"] += w[src]
                elif name.startswith(("upperarm", "lowerarm", "shoulder")):
                    extra["_arm"] += w[src]
                elif name.startswith(("upperleg", "lowerleg", "pelvis")):
                    extra["_leg"] += w[src]
                elif name.startswith(("foot", "toe")):
                    extra["_foot"] += w[src]
        cap = Captured(key, co * scale, sizes, loops, uv, src, W, _texture_of(o), extra)
        for name, per in exprs.items():
            cap.deltas[name] = per[key] * scale
        out[key] = cap
        C.log("captured", key, len(co), "verts", int(sum(sizes) - 2 * len(sizes)), "tris", cap.texture)
    bones = {}
    for b in ("jaw", "head", "neck01", "spine01", "spine03", "clavicle.L", "clavicle.R", "eye.L", "eye.R", "upperarm01.L",
              "upperarm01.R"):
        bones[b] = np.array(rig.data.bones[b].head_local) * scale
    for side in ("L", "R"):
        pb = rig.pose.bones["lowerarm01." + side]
        bones["elbow." + side] = np.array(pb.head) * scale
        bones["wrist." + side] = np.array(rig.pose.bones["wrist." + side].head) * scale
        bones["shoulder." + side] = np.array(rig.pose.bones["upperarm01." + side].head) * scale
    bones["jaw_tail"] = np.array(rig.data.bones["jaw"].tail_local) * scale
    info = {"scale": scale, "bones": bones, "stature": body.dimensions.z * scale}
    return out, info


def save_cache(path, caps, info):
    data = {}
    for k, c in caps.items():
        for attr in ("co", "sizes", "loops", "uv", "src"):
            data[f"{k}|{attr}"] = getattr(c, attr)
        data[f"{k}|texture"] = np.array(c.texture or "")
        for b, w in c.weights.items():
            data[f"{k}|w|{b}"] = w
        for g, w in c.groups.items():
            data[f"{k}|g|{g}"] = w
        for e, d in c.deltas.items():
            data[f"{k}|d|{e}"] = d
    for b, v in info["bones"].items():
        data[f"_bone|{b}"] = v
    data["_scale"] = np.array(info["scale"])
    data["_stature"] = np.array(info["stature"])
    np.savez_compressed(path, **data)


def load_cache(path):
    z = np.load(path, allow_pickle=False)
    caps = {}
    raw = {}
    for name in z.files:
        raw[name] = z[name]
    keys = {n.split("|")[0] for n in raw if not n.startswith("_")}
    for k in keys:
        c = Captured(k, raw[f"{k}|co"], raw[f"{k}|sizes"], raw[f"{k}|loops"], raw[f"{k}|uv"], raw[f"{k}|src"], {},
                     str(raw[f"{k}|texture"]) or None)
        for n, v in raw.items():
            parts = n.split("|")
            if parts[0] != k or len(parts) != 3:
                continue
            {"w": c.weights, "g": c.groups, "d": c.deltas}[parts[1]][parts[2]] = v
        caps[k] = c
    info = {"scale": float(raw["_scale"]), "stature": float(raw["_stature"]),
            "bones": {n.split("|")[1]: v for n, v in raw.items() if n.startswith("_bone|")}}
    return caps, info
