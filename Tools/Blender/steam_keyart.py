"""Steam key art (2026-11-10): one battle scene, framed per Steam slot.

Round 2 (Remco: "the towers are cool, the rest feels stale and bare"): a richer, more spectacular version of a real
Landfall map. The approved CrysiumField logo environment stays underneath (logo_v4 on logo_v3: the Gravemaw_Fissure
modules with the v4 lava shader, heat haze, embers, the Crystal_A/B/C formations, crystal-tower spires, mist), and
on top of it:
- the level itself from the game's environment kit (ArtSource/Exports/Env): the Ground, Road and Plaza tiling sets,
  RoadEdge_Straight modules along the road, BuildPadTile pads on the open ground beside it (never on the road), flora
  (GlowTree, Frond, Mushroom, Tendril), rocks and boulders, eggs, crystal remains and splinters, scree, rifts the
  wave pours from, Cliff_Straight and Silhouette_Ridge closing the map, and out-of-focus foreground framing;
- the colony (cool blue, white, yellow): Tower_Laser/Gun/Cannon/Flak x2/Tesla/Railgun on pads, Gate_G2 across the
  road, two Drone_Repair with beams, Spencer (the `Mercer` export) on his jetpack;
- the wave (warm red, green, orange): Skitters, armoured Cragbacks, Rollers, Prowlers, a PackAlpha, Shalebats,
  Skyrays and Wisps, with the region boss Gravemaw rearing over the road behind them;
- the fight: laser beam with hit sparks on Cragback armour, gun tracers, muzzle flashes, a Tesla arc chaining through
  Skitters, a railgun streak, a cannon impact with debris and a smoke column, flak bursts round the flyers, jet
  flames, battle smoke, ground mist, moonlight shafts in the haze and depth of field. Emission stays well below white
  so AgX keeps it in colour.
Round 3 (Remco: still too empty; towers never stand on the road; square Skyray wings; blocky small hero): every open
patch dressed (dense flora, crystal clusters, rocks, scatter, rifts with eggs, lava and bog pools from the game's
liquid maps, more cliffs and mesas); the paved terrace is gone and the towers stand on BuildPadTile pads on the ground
beside the road, a battery left of the crack and two on the right shoulder; Skyray and Shalebat wings get the game's
SurfaceWave ripple (Game/UnitLit) baked into the mesh plus a little dihedral, tilted to show their planform; Spencer
is the full pass-4 export (body, Mercer_Face, Mercer_Visor, 2048 maps, jetpack) at 1.6x, three-quarters to camera,
flying with the Shoot clip on his arms and the pistol in Weapon_R; less pink, cooler mist; Gravemaw's head gets a
glowing maw, eyes and a cool rim; Skitters bigger, with a flank climbing out of the crack in front.
All game models are the real exports, posed from their own clips and baked to static meshes.

The renders are text-free masters; steam_compose.py puts the logo on and cuts the final Steam sizes.

Run (all framings in one process, or a subset):
  blender -b --factory-startup --python Tools/Blender/steam_keyart.py -- --shot wide|std|header|vert|all
          [--samples 96] [--draft | --half] [--threads 8]
Out: ArtSource/Previews/Steam/_masters/keyart_<shot>.png
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Euler, Matrix, Vector, noise as mnoise  # noqa: E402

import common as C  # noqa: E402
import logo_v4 as V4  # noqa: E402  (patches logo_v3 with the v4 lava, haze and embers)

V = V4.V
L2 = V.L2
EX = C.EXPORTS
OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Steam", "_masters")
FLOOR = V.FLOOR

# Colony colours (cool, technical) and alien/fire colours (warm), linear light.
LASER = (0.10, 0.45, 1.0)
LASER_CORE = (0.55, 0.85, 1.0)
TRACER = (1.0, 0.62, 0.16)
TRACER_CORE = (1.0, 0.85, 0.5)
TESLA = (0.25, 0.5, 1.0)
TESLA_CORE = (0.7, 0.85, 1.0)
RAIL = (0.55, 0.75, 1.0)
JET = (0.2, 0.5, 1.0)
JET_CORE = (0.7, 0.88, 1.0)
FIRE = (1.0, 0.36, 0.06)
FIRE_HOT = (1.0, 0.7, 0.3)
FLAK = (1.0, 0.72, 0.3)
COLONY_LAMP = V.COLONY

# Scene scale: towers and gates a little heavier than in the game so they hold their own next to Spencer (1.6 m).
TOWER_SCALE = 2.0
GATE_SCALE = 1.8


# =============================================================================================================
# Game models, posed from their own clips and baked to static meshes
# =============================================================================================================

_MATS = {}


def tex_mat(prefix, emit):
    """Game material for an export texture prefix (ArtSource/Exports/<prefix>_Albedo.png ...); a simpler glossy
    stand-in when the set is incomplete (Spencer's visor has only albedo and emission)."""
    key = (prefix, emit)
    if key in _MATS:
        return _MATS[key]
    maps = {k: os.path.join(EX, prefix + "_" + s + ".png") for k, s in
            (("albedo", "Albedo"), ("mask", "Mask"), ("emission", "Emission"), ("normal", "Normal"))}
    if all(os.path.exists(p) for p in maps.values()):
        mat = C.game_material(os.path.basename(prefix) + "_K", maps, emission_strength=emit)
    else:
        mat = C.new_material(os.path.basename(prefix) + "_K")
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        bsdf.inputs["Roughness"].default_value = 0.08
        for k, sock in (("albedo", "Base Color"), ("emission", "Emission Color")):
            if os.path.exists(maps[k]):
                img = nt.nodes.new("ShaderNodeTexImage")
                img.image = bpy.data.images.load(maps[k], check_existing=True)
                nt.links.new(img.outputs["Color"], bsdf.inputs[sock])
        bsdf.inputs["Emission Strength"].default_value = emit
    _MATS[key] = mat
    return mat


class Posed:
    """A baked pose: static mesh objects plus the world matrices of named bones (muzzles, jet sockets), all in the
    import's own frame; place() moves them into the scene."""

    def __init__(self, objs, bones):
        self.objs = objs
        self.bones = bones


_POSE_CACHE = {}


def bake_pose(rel, folder_prefix, clip=None, frame=0, keep=None, drop=("LOD1", "Damage", "Wreck"), aim=None,
              emit=2.0, stars=3, overlay=None):
    """Imports an FBX, plays `clip` at `frame` (or aims Yaw/Pitch with `aim`=(yaw_deg, pitch_deg)), evaluates the
    armature and keeps the deformed meshes as static copies. Materials map slot `<X>_Mat` -> textures `<X>_*.png`
    in the export folder."""
    key = (rel, clip, frame, aim, stars, overlay)
    if key in _POSE_CACHE:
        return _POSE_CACHE[key]
    before_obs = set(bpy.data.objects)
    before_acts = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=os.path.join(EX, rel))
    new = [o for o in bpy.data.objects if o not in before_obs]
    acts = [a for a in bpy.data.actions if a not in before_acts]
    arm = next((o for o in new if o.type == "ARMATURE"), None)
    meshes = [o for o in new if o.type == "MESH" and not any(d in o.name for d in drop)
              and (keep is None or any(k in o.name for k in keep))]
    if arm is not None:
        arm.animation_data_create()
        arm.animation_data.action = None

        def use_clip(name):
            act = next(a for a in acts if a.name.split("|")[-1].split(".")[0] == name and not a.name.startswith("Key|"))
            arm.animation_data.action = act
            slots = [sl for sl in getattr(act, "slots", ()) if getattr(sl, "target_id_type", "OBJECT") == "OBJECT"]
            if hasattr(arm.animation_data, "action_slot") and slots:
                arm.animation_data.action_slot = slots[0]

        over = {}
        if overlay:
            # (clip, frame, bone-name fragments): those bones take the other clip's pose (e.g. aim the pistol in flight).
            use_clip(overlay[0])
            bpy.context.scene.frame_set(overlay[1])
            over = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones if any(f in pb.name for f in overlay[2])}
        if clip:
            use_clip(clip)
        bpy.context.scene.frame_set(frame)
        if over:
            base = {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones}
            arm.animation_data.action = None
            for pb in arm.pose.bones:
                pb.matrix_basis = over.get(pb.name, base[pb.name])
        if aim is not None:
            pose_tower(arm, aim[0], aim[1], stars)
        bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    objs = []
    for m in meshes:
        me = bpy.data.meshes.new_from_object(m.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
        me.name = m.name + "_baked"
        for i, slot in enumerate(me.materials):
            stem = (slot.name if slot else m.name).split(".")[0]
            stem = stem[:-4] if stem.endswith("_Mat") else stem
            me.materials[i] = tex_mat(folder_prefix + "/" + stem, emit)
        ob = bpy.data.objects.new(m.name + "_P", me)
        ob.matrix_world = m.matrix_world.copy()
        objs.append(ob)
    bones = {}
    if arm is not None:
        for pb in arm.pose.bones:
            bones[pb.name] = arm.matrix_world @ pb.matrix
    for o in new:
        bpy.data.objects.remove(o, do_unlink=True)
    for a in acts:
        bpy.data.actions.remove(a)
    res = Posed(objs, bones)
    _POSE_CACHE[key] = res
    return res


def pose_tower(arm, yaw_deg, pitch_deg, stars):
    """Tower aim the way build_towers.pose does it, but with the FBX import's armature transform taken into account."""
    to_arm = arm.matrix_world.to_3x3().inverted()
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    from mathutils import Quaternion

    def axis(bone, world_axis):
        rest = arm.data.bones[bone].matrix_local.to_3x3()
        return (rest.inverted() @ (to_arm @ Vector(world_axis))).normalized()

    if "Yaw" in arm.pose.bones:
        arm.pose.bones["Yaw"].rotation_quaternion = Quaternion(axis("Yaw", (0, 0, 1)), math.radians(yaw_deg))
    if "Pitch" in arm.pose.bones:
        arm.pose.bones["Pitch"].rotation_quaternion = Quaternion(axis("Pitch", (1, 0, 0)), -math.radians(pitch_deg))
    for i in range(3):
        name = f"Star_{i + 1}"
        if name in arm.pose.bones:
            arm.pose.bones[name].scale = (1, 1, 1) if i < stars else (0, 0, 0)


def place(posed, col, loc, rot_z=0.0, scale=1.0, tilt=(0.0, 0.0)):
    """Linked copies of a baked pose at loc; returns (objects, bone matrices in the scene)."""
    T = (Matrix.Translation(loc) @ Euler((tilt[0], tilt[1], rot_z)).to_matrix().to_4x4()
         @ Matrix.Diagonal((scale, scale, scale, 1.0)))
    obs = []
    for src in posed.objs:
        ob = bpy.data.objects.new(src.name + "_i", src.data)
        col.objects.link(ob)
        ob.matrix_world = T @ src.matrix_world
        obs.append(ob)
    return obs, {k: T @ m for k, m in posed.bones.items()}


def attach_pistol(col, socket, corr):
    """Spencer's pistol (Mercer/Weapons, modelled in the Weapon_R grip space) at the posed socket."""
    obs = V.import_fbx("Mercer/Weapons/Mercer_Pistol.fbx", "Mercer/Weapons/MercerWeapons", 3.0)
    out = []
    for o in obs:
        if "Quick" in o.name:
            bpy.data.objects.remove(o)
            continue
        o.matrix_world = socket @ corr @ o.matrix_world
        L2.put(o, col)
        out.append(o)
    return out


def yaw_to(src, dst):
    """Rotation about Z that turns a model facing -Y towards dst."""
    d = Vector(dst) - Vector(src)
    return math.atan2(d.x, -d.y)


# =============================================================================================================
# VFX geometry
# =============================================================================================================

def emit_only(name, colour, strength, core=None, power=1.5, transparent=True):
    """Additive glow: brightest where the surface faces the camera (a soft round tube or ball), see-through rim."""
    mat, nt, out = L2.new_mat(name)
    lw = L2.N(nt, "ShaderNodeLayerWeight", {"Blend": 0.5})
    facing = L2.M(nt, "POWER", L2.M(nt, "SUBTRACT", 1.0, L2.o(lw, "Facing")), power)
    col = L2.MIXC(nt, facing, colour, core or colour)
    em = L2.N(nt, "ShaderNodeEmission", {"Color": col, "Strength": L2.M(nt, "MULTIPLY", facing, strength)})
    if transparent:
        tr = L2.N(nt, "ShaderNodeBsdfTransparent")
        add = L2.N(nt, "ShaderNodeAddShader")
        nt.links.new(L2.o(em), add.inputs[0])
        nt.links.new(L2.o(tr), add.inputs[1])
        nt.links.new(add.outputs[0], out.inputs["Surface"])
    else:
        nt.links.new(L2.o(em), out.inputs["Surface"])
    return mat


def fx_obj(name, bm, mat, col):
    ob = L2.bm_object(name, bm, [mat])
    ob.visible_shadow = False
    L2.put(ob, col)
    return ob


def tube(bm, a, b, r0, r1=None, sides=12):
    """A capped tube from a to b in an existing bmesh."""
    a, b = Vector(a), Vector(b)
    d = b - a
    rot = d.normalized().to_track_quat("Z", "Y").to_matrix().to_4x4()
    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=sides, radius1=r0, radius2=r1 if r1 is not None else r0,
                                depth=d.length)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((a + b) / 2) @ rot, verts=res["verts"])


def blob(bm, p, r, stretch_dir=None, stretch=1.0, subdiv=2, lump=0.0, seed=0):
    res = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    sd = Vector(stretch_dir).normalized() if stretch_dir is not None else None
    for v in res["verts"]:
        co = v.co.copy()
        if lump:
            co *= 1 + lump * mnoise.noise(co / r * 1.3 + Vector((seed, seed * 0.7, 0)))
        if sd is not None:
            along = co.dot(sd)
            co = co + sd * along * (stretch - 1)
        v.co = co + Vector(p)


def light(name, loc, colour, energy, radius, col):
    return L2.point(name, loc, colour, energy, radius, col)


def beam(name, a, b, col, colour, core, width, strength, lights=4, light_energy=60.0):
    """Laser: a hot core inside a soft glow sleeve, lighting what is near it."""
    bm = bmesh.new()
    tube(bm, a, b, width * 0.28)
    fx_obj(name + "Core", bm, emit_only(name + "CoreM", colour, strength * 1.4, core, power=0.6, transparent=False),
           col)
    bm = bmesh.new()
    tube(bm, a, b, width, sides=20)
    fx_obj(name + "Glow", bm, emit_only(name + "GlowM", colour, strength * 0.35, colour, power=2.2), col)
    a, b = Vector(a), Vector(b)
    for k in range(lights):
        t = (k + 0.5) / lights
        light(f"{name}L{k}", a.lerp(b, t), colour, light_energy, 0.2, col)


def glow_ball(name, p, r, col, colour, core, strength, light_energy=0.0, light_radius=0.3):
    bm = bmesh.new()
    blob(bm, p, r, subdiv=3)
    fx_obj(name, bm, emit_only(name + "M", colour, strength, core, power=1.2), col)
    if light_energy:
        light(name + "L", p, colour, light_energy, light_radius, col)


def muzzle_flash(name, p, direction, col, colour=TRACER, core=TRACER_CORE, size=0.35, strength=8.0, energy=150.0,
                 seed=0):
    """A flash: a stretched hot spike along the barrel, a few short side petals, a little glow ball, a lamp."""
    rng = random.Random(seed)
    d = Vector(direction).normalized()
    bm = bmesh.new()
    blob(bm, Vector(p) + d * size * 0.5, size * 0.22, d, 3.2, subdiv=2)
    side = d.orthogonal().normalized()
    for k in range(5):
        q = Matrix.Rotation(k * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3), 3, d) @ side
        sd = (d * 0.55 + q).normalized()
        blob(bm, Vector(p) + sd * size * 0.3, size * 0.07, sd, 3.5, subdiv=1)
    fx_obj(name, bm, emit_only(name + "M", colour, strength, core, power=0.9), col)
    glow_ball(name + "Ball", Vector(p) + d * size * 0.15, size * 0.45, col, colour, core, strength * 0.25)
    light(name + "L", Vector(p) + d * size * 0.4, colour, energy, 0.15, col)


def tracers(name, a, b, col, count=4, length=0.9, width=0.035, colour=TRACER, core=TRACER_CORE, strength=9.0,
            seed=0, spread=0.12):
    rng = random.Random(seed)
    a, b = Vector(a), Vector(b)
    d = (b - a)
    L = d.length
    dn = d.normalized()
    side = dn.orthogonal().normalized()
    up = dn.cross(side)
    bm = bmesh.new()
    for k in range(count):
        t = (k + rng.uniform(0.2, 0.8)) / count
        off = side * rng.gauss(0, spread) + up * rng.gauss(0, spread)
        c = a + d * t + off * t
        blob(bm, c, width, dn, length / (2 * width), subdiv=2)
    fx_obj(name, bm, emit_only(name + "M", colour, strength, core, power=0.8), col)
    for k in range(2):
        light(f"{name}L{k}", a.lerp(b, 0.3 + 0.4 * k), colour, 25.0, 0.2, col)
    return L


def arc_points(a, b, rng, depth=6, amp=0.22):
    pts = [Vector(a), Vector(b)]
    for lvl in range(depth):
        new = [pts[0]]
        for p, q in zip(pts[:-1], pts[1:]):
            m = (p + q) / 2
            seg = (q - p).length
            m += Vector((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))) * seg * amp
            new += [m, q]
        pts = new
    return pts


def arc(name, a, b, col, rng, width=0.014, strength=3.0, branches=3):
    """Tesla discharge: a jagged main bolt with a few forks, as a polyline tube."""
    bm = bmesh.new()
    pts = arc_points(a, b, rng)
    for p, q in zip(pts[:-1], pts[1:]):
        tube(bm, p, q, width, sides=6)
    for k in range(branches):
        i = rng.randrange(len(pts) // 4, len(pts) * 3 // 4)
        p = pts[i]
        end = p + Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.6, 0.4))) * 0.9
        sub = arc_points(p, end, rng, depth=4)
        for u, w in zip(sub[:-1], sub[1:]):
            tube(bm, u, w, width * 0.6, sides=5)
    fx_obj(name, bm, emit_only(name + "M", TESLA, strength, TESLA_CORE, power=0.4, transparent=False), col)
    bm = bmesh.new()
    for p, q in zip(pts[:-1:2], pts[2::2]):
        tube(bm, p, q, width * 6, sides=10)
    fx_obj(name + "Glow", bm, emit_only(name + "GlowM", TESLA, strength * 0.12, TESLA, power=2.0), col)
    for k in range(3):
        light(f"{name}L{k}", pts[(k + 1) * len(pts) // 4], TESLA, 70.0, 0.2, col)


def mat_fireball(name, strength):
    """Fireball: hot yellow core through orange to a dark, sooty rim; noise breaks it into billows."""
    mat, nt, out = L2.new_mat(name)
    co = L2.objco(nt)
    n = L2.o(L2.noise(nt, co, 3.0, 6.0, 0.62), "Factor")
    lw = L2.N(nt, "ShaderNodeLayerWeight", {"Blend": 0.5})
    facing = L2.M(nt, "SUBTRACT", 1.0, L2.o(lw, "Facing"))
    heat = L2.M(nt, "ADD", L2.M(nt, "MULTIPLY", facing, 0.75), L2.MR(nt, n, 0.3, 0.7, -0.35, 0.35), clamp=True)
    ramp = L2.N(nt, "ShaderNodeValToRGB")
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.0, (0.02, 0.012, 0.01, 1)
    els[1].position, els[1].color = 0.42, (0.6, 0.08, 0.01, 1)
    e = els.new(0.7)
    e.color = (*FIRE, 1)
    e = els.new(0.92)
    e.color = (*FIRE_HOT, 1)
    nt.links.new(heat, ramp.inputs[0])
    s = L2.MR(nt, heat, 0.3, 1.0, 0.0, strength)
    L2.principled(nt, out, base=(0.03, 0.025, 0.022), metal=0.0, rough=0.9, emit=ramp.outputs["Color"], emit_s=s)
    return mat


def mat_smoke(name, colour, density, glow=None, glow_strength=0.0):
    """Spherical volume puff: density falls off from the centre and is broken up by noise; optional fire glow."""
    mat, nt, out = L2.new_mat(name)
    co = L2.objco(nt)
    r = L2.o(L2.N(nt, "ShaderNodeVectorMath", {"Vector": co}, operation="LENGTH"), "Value")
    fall = L2.MR(nt, r, 0.15, 0.5, 1.0, 0.0)
    n = L2.MR(nt, L2.o(L2.noise(nt, co, 4.0, 5.0, 0.6), "Factor"), 0.35, 0.7, 0.0, 1.0)
    dens = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", fall, n), density)
    inputs = {"Color": colour, "Density": dens, "Anisotropy": 0.2}
    vol = L2.N(nt, "ShaderNodeVolumePrincipled", inputs)
    if glow:
        L2.setv(nt, vol, "Emission Color", glow)
        L2.setv(nt, vol, "Emission Strength", L2.M(nt, "MULTIPLY", L2.MR(nt, r, 0.0, 0.35, 1.0, 0.0),
                                                   glow_strength))
    nt.links.new(L2.o(vol), out.inputs["Volume"])
    return mat


def smoke(name, p, r, col, colour=(0.05, 0.05, 0.055), density=6.0, glow=None, glow_strength=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = name
    ob.location = p
    ob.scale = (r * 2, r * 2, r * 2)
    ob.data.materials.append(mat_smoke(name + "M", colour, density, glow, glow_strength))
    L2.put(ob, col)
    return ob


def sparks(name, p, col, rng, count=40, reach=1.6, colour=FIRE, core=FIRE_HOT, strength=10.0, up=0.4, size=0.02):
    bm = bmesh.new()
    for _ in range(count):
        d = Vector((rng.gauss(0, 1), rng.gauss(0, 1), abs(rng.gauss(0, 1)) * up + rng.gauss(0, 0.3))).normalized()
        dist = reach * rng.random() ** 0.7
        blob(bm, Vector(p) + d * dist, size * rng.uniform(0.6, 1.3), d, rng.uniform(4, 9), subdiv=1)
    fx_obj(name, bm, emit_only(name + "M", colour, strength, core, power=0.6, transparent=False), col)


def debris(name, p, col, rng, count=14, reach=1.4):
    bm = bmesh.new()
    for _ in range(count):
        d = Vector((rng.gauss(0, 1), rng.gauss(0, 1), abs(rng.gauss(0, 1)) * 0.8)).normalized()
        c = Vector(p) + d * reach * rng.uniform(0.3, 1.0)
        res = bmesh.ops.create_cube(bm, size=rng.uniform(0.05, 0.14))
        R = Euler((rng.uniform(0, 6), rng.uniform(0, 6), rng.uniform(0, 6))).to_matrix().to_4x4()
        bmesh.ops.transform(bm, matrix=Matrix.Translation(c) @ R, verts=res["verts"])
    mat, nt, out = L2.new_mat(name + "M")
    L2.principled(nt, out, base=(0.03, 0.025, 0.02), metal=0.0, rough=0.8, emit=FIRE, emit_s=4.0)
    ob = L2.bm_object(name, bm, [mat])
    L2.put(ob, col)


def explosion(name, p, col, scale=1.0, seed=0):
    """Cannon shell hit: billowing fireball low on the ground, a sooty smoke cap, sparks, flung debris, a hot lamp."""
    rng = random.Random(seed)
    p = Vector(p)
    bm = bmesh.new()
    for k in range(9):
        off = Vector((rng.gauss(0, 0.35), rng.gauss(0, 0.35), abs(rng.gauss(0.35, 0.3)))) * scale
        blob(bm, p + off, scale * rng.uniform(0.35, 0.6), subdiv=3, lump=0.35, seed=seed * 10 + k)
    ob = L2.bm_object(name + "Fire", bm, [mat_fireball(name + "FireM", 9.0)])
    ob.visible_shadow = False
    L2.put(ob, col)
    smoke(name + "Smoke", p + Vector((0, 0.1, 1.15 * scale)), 1.1 * scale, col, density=3.5, glow=FIRE,
          glow_strength=0.0)
    sparks(name + "Sparks", p + Vector((0, 0, 0.4 * scale)), col, rng, 70, 2.2 * scale, size=0.022)
    debris(name + "Debris", p + Vector((0, 0, 0.4 * scale)), col, rng, 9, 0.8 * scale)
    light(name + "L", p + Vector((0, -0.4, 0.8 * scale)), FIRE, 2600.0 * scale, 0.6, col)
    # Scorched ground glow under the blast.
    light(name + "L2", p + Vector((0, 0, 0.15)), FIRE_HOT, 600.0 * scale, 0.3, col)


def flak_burst(name, p, col, scale=1.0, seed=0, smoke_only=False):
    """Flak: a sharp warm-white flash with a shell of fragments, and the black puff it leaves behind."""
    rng = random.Random(seed)
    p = Vector(p)
    smoke(name + "Puff", p + Vector((0, 0.1, 0.05)), 0.75 * scale, col, colour=(0.04, 0.04, 0.045), density=7.0,
          glow=FLAK if not smoke_only else None, glow_strength=0.0 if smoke_only else 1.2)
    if smoke_only:
        return
    glow_ball(name + "Core", p, 0.12 * scale, col, FLAK, (1.0, 0.85, 0.6), 10.0, light_energy=500.0 * scale,
              light_radius=0.2)
    sparks(name + "Frag", p, col, rng, 36, 1.0 * scale, colour=FLAK, core=(1.0, 0.85, 0.55), strength=8.0, up=0.0,
           size=0.014)


def jet_flame(name, m, col, length=0.5):
    """Jetpack exhaust along the socket's Y axis (the report's exhaust direction)."""
    p = m.to_translation()
    d = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    bm = bmesh.new()
    tube(bm, p, p + d * length, 0.075, 0.0, sides=16)
    fx_obj(name, bm, emit_only(name + "M", JET, 1.6, JET, power=1.6), col)
    bm = bmesh.new()
    tube(bm, p + d * 0.02, p + d * length * 0.4, 0.03, 0.0, sides=12)
    fx_obj(name + "Core", bm, emit_only(name + "CoreM", JET, 2.5, JET_CORE, power=1.0), col)
    light(name + "L", p + d * 0.3, JET, 90.0, 0.1, col)


# =============================================================================================================
# Layout (round 2, after Remco's feedback: a richer, more spectacular version of a real Landfall map)
# =============================================================================================================

SEQ = ["Start", "Straight_A", "Curve_R", "Straight_B", "Curve_L", "Straight_A", "Curve_L", "Straight_C", "Curve_R",
       "Straight_B", "Straight_A", "Curve_R", "Curve_L", "Straight_B", "Straight_C", "Curve_L", "Straight_A", "Head"]

CFG = dict(V4.FIELD)
CFG.update(label="KeyArt", series=False, seed=5, crack_start=(0.0, -6.0), crack_scale=2.2, crack_seq=SEQ,
           lava_emit=3.5, lava_light=42.0, field_count=26, bumps=0.06,
           frame_crystals=[(-17.0, 8.0, 3.2, "Crystal_B"), (-21.0, 20.0, 3.6, "Crystal_C"), (24.0, 30.0, 3.4, "Crystal_A")],
           crystal_lights=[(-16.0, 8.0, 90.0), (-20.0, 20.0, 80.0), (23.0, 30.0, 70.0)],
           towers=[(-30, 80, 1.5, 0.3), (-16, 90, 1.2, 2.0), (24, 86, 1.3, 1.0), (36, 72, 1.1, 4.0)],
           colony=[("base", -36, 70, 2.2, 0.6)], colony_light=1600.0, colony_emit=8.0,
           drone=None, skitters=[], eyes=[(17.0, 6.0, FLOOR + 0.35, 1.5), (19.5, 10.5, FLOOR + 0.3, 1.3),
                                         (-15.0, 14.0, FLOOR + 0.35, 1.3)],
           veins=[(-8.0, 3.0, 2.2, 0.4), (-12.0, 14.0, 2.5, 1.0), (16.0, 21.0, 2.4, 2.6)],
           haze=0.015, embers=220, motes=80, horizon=(0.010, 0.006, 0.016))

ROAD_OFF = 5.4       # road centre right of the crack
ROAD_HALF = 3.0      # about three game cells at the scene's 2x scale
ENV_SCALE = 2.0      # level props at the scene's 2x unit scale


def x_at(path, y):
    """The crack's centre-line x at depth y (the chain runs into +Y)."""
    for (ax, ay), (bx, by) in zip(path[:-1], path[1:]):
        if min(ay, by) <= y <= max(ay, by) and abs(by - ay) > 1e-6:
            return ax + (bx - ax) * (y - ay) / (by - ay)
    return path[-1][0] if y > path[-1][1] else path[0][0]


def bank(path, y, off, z=0.0):
    return Vector((x_at(path, y) + off, y, FLOOR + z))


def smooth_line(path, off, y0, y1, step=0.5, window=9):
    raw = [(x_at(path, y) + off, y) for y in [y0 + k * step for k in range(int((y1 - y0) / step) + 1)]]
    out = []
    for i in range(len(raw)):
        lo, hi = max(0, i - window // 2), min(len(raw), i + window // 2 + 1)
        out.append((sum(p[0] for p in raw[lo:hi]) / (hi - lo), raw[i][1]))
    return out


def tiled_mat(name, folder, emit=1.0, rough_gain=1.0, glow=None):
    """A level tiling set (ArtSource/Exports/Env/Tiling/<set>) on UVs in world metres / tile size. `glow` adds the
    crack's ember flush on the ground from the terrain's Glow attribute."""
    maps = {k: os.path.join(EX, "Env", "Tiling", folder, f"{folder}_{s}.png") for k, s in
            (("albedo", "Albedo"), ("mask", "Mask"), ("emission", "Emission"), ("normal", "Normal"))}
    if not os.path.exists(maps["emission"]):
        maps["emission"] = os.path.join(EX, "Env", "Tiling", "Ground", "Ground_Emission.png")
        emit = 0.0
    mat = C.game_material(name, maps, emission_strength=emit)
    if glow:
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        co = L2.objco(nt)
        attr = L2.o(L2.N(nt, "ShaderNodeAttribute", attribute_name="Glow", attribute_type="GEOMETRY"), "Factor")
        seams = L2.crack_lines(nt, co, 0.9, 0.022, warp=0.35)
        breakup = L2.MR(nt, L2.o(L2.noise(nt, co, 0.8, 3.0), "Factor"), 0.4, 0.62, 0.15, 1.0)
        veins = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", seams, L2.MR(nt, attr, 0.12, 0.85, 0.0, 1.0)), breakup)
        flush = L2.M(nt, "MULTIPLY", L2.M(nt, "POWER", attr, 3.0), 0.12)
        e = L2.M(nt, "MULTIPLY", L2.M(nt, "ADD", L2.M(nt, "POWER", veins, 1.5), flush), glow)
        old = bsdf.inputs["Emission Color"].links[0].from_socket
        mixed = L2.N(nt, "ShaderNodeMix", data_type="RGBA", blend_type="ADD")
        L2.setv(nt, mixed, "Factor", 1.0)
        nt.links.new(old, L2.sock(mixed.inputs, "A"))
        sc = L2.N(nt, "ShaderNodeVectorMath", operation="SCALE")
        sc.inputs[0].default_value = (*V.EMBER,)
        nt.links.new(e, L2.sock(sc.inputs, "Scale"))
        nt.links.new(sc.outputs[0], L2.sock(mixed.inputs, "B"))
        nt.links.new(L2.o(mixed, "Result"), bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.0
    return mat


def world_uv(ob, tile):
    me = ob.data
    uv = me.uv_layers.new(name="UVMap")
    mw = ob.matrix_world
    for loop in me.loops:
        co = mw @ me.vertices[loop.vertex_index].co
        uv.data[loop.index].uv = (co.x / tile, co.y / tile)


def ribbon(name, line, o0, o1, z, mat, col, tile, across=6):
    """A strip between offsets o0..o1 (metres, + = right of travel) of a centre line, UVs in metres / tile."""
    bm = bmesh.new()
    rows = []
    dist = 0.0
    for i, (x, y) in enumerate(line):
        a = Vector(line[max(i - 1, 0)])
        b = Vector(line[min(i + 1, len(line) - 1)])
        t = (b - a).normalized()
        n = Vector((t.y, -t.x))
        if i:
            dist += (Vector(line[i]) - Vector(line[i - 1])).length
        row = []
        for k in range(across + 1):
            o = o0 + (o1 - o0) * k / across
            p = Vector((x, y)) + n * o
            row.append((bm.verts.new((p.x, p.y, z)), (o / tile, dist / tile)))
        rows.append(row)
    uv = bm.loops.layers.uv.new("UVMap")
    for ra, rb in zip(rows[:-1], rows[1:]):
        for k in range(across):
            f = bm.faces.new((ra[k][0], ra[k + 1][0], rb[k + 1][0], rb[k][0]))
            for loop, (_, u) in zip(f.loops, (ra[k], ra[k + 1], rb[k + 1], rb[k])):
                loop[uv].uv = u
    ob = L2.bm_object(name, bm, [mat])
    for p in ob.data.polygons:
        p.use_smooth = True
    L2.put(ob, col)
    return ob


def along(line, off, spacing):
    """(position, tangent angle) every `spacing` metres along a centre line, `off` to its right."""
    out, carry = [], 0.0
    for (ax, ay), (bx, by) in zip(line[:-1], line[1:]):
        a, b = Vector((ax, ay)), Vector((bx, by))
        seg = (b - a).length
        t = (b - a).normalized()
        n = Vector((t.y, -t.x))
        s = carry
        while s < seg:
            p = a + t * s + n * off
            out.append((Vector((p.x, p.y, 0.0)), math.atan2(t.y, t.x)))
            s += spacing
        carry = s - seg
    return out


_PROPS = {}


def prop(name, emit=2.0):
    """A static level prop (ArtSource/Exports/Env/<name> or Exports/<name>) imported once, for linked instances."""
    if name in _PROPS:
        return _PROPS[name]
    rel = f"Env/{name}/{name}.fbx" if os.path.exists(os.path.join(EX, "Env", name)) else f"{name}/{name}.fbx"
    tex = os.path.dirname(rel) + "/" + name
    obs = V.import_fbx(rel, tex, emit)
    hold = L2.coll("_PropSources")
    hold.hide_render = True
    for o in obs:
        L2.put(o, hold)
    _PROPS[name] = obs
    return obs


def put_prop(name, col, loc, rz=0.0, scale=1.0, emit=2.0, tilt=(0.0, 0.0)):
    return [V.instance(o, col, Vector(loc), rz, scale, tilt) for o in prop(name, emit)]


def mat_lava_pool(name):
    """A pool of the game's lava (Exports/Liquids/Lava crust maps): dark crust plates, the Heat mask glowing through."""
    mat, nt, out = L2.new_mat(name)
    uv = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "UV")
    alb = L2.N(nt, "ShaderNodeTexImage")
    alb.image = bpy.data.images.load(os.path.join(EX, "Liquids", "Lava", "Lava_CrustA_Albedo.png"), check_existing=True)
    heat = L2.N(nt, "ShaderNodeTexImage")
    heat.image = bpy.data.images.load(os.path.join(EX, "Liquids", "Lava", "Lava_CrustA_Heat.png"), check_existing=True)
    heat.image.colorspace_settings.name = "Non-Color"
    for t in (alb, heat):
        nt.links.new(uv, t.inputs["Vector"])
    edge = L2.o(L2.N(nt, "ShaderNodeAttribute", attribute_name="Edge", attribute_type="GEOMETRY"), "Factor")
    h = L2.M(nt, "MULTIPLY", L2.o(heat, "Color"), L2.MR(nt, edge, 0.0, 1.0, 1.0, 0.25))
    ramp = L2.N(nt, "ShaderNodeValToRGB")
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.0, (*V4.LAVA_COOL, 1)
    els[1].position, els[1].color = 0.6, (*V4.LAVA_MID, 1)
    e = els.new(0.9)
    e.color = (*V4.LAVA_HOT, 1)
    nt.links.new(h, ramp.inputs[0])
    L2.principled(nt, out, base=L2.o(alb, "Color"), metal=0.0, rough=0.6, emit=ramp.outputs["Color"],
                  emit_s=L2.MR(nt, h, 0.1, 1.0, 0.0, 6.0))
    return mat


def mat_bog_pool(name):
    """Toxic alien bog: dark green scum (Exports/Liquids/Bog) with a sickly glow under it."""
    mat, nt, out = L2.new_mat(name)
    uv = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "UV")
    scum = L2.N(nt, "ShaderNodeTexImage")
    scum.image = bpy.data.images.load(os.path.join(EX, "Liquids", "Bog", "Bog_Scum.png"), check_existing=True)
    scum.image.colorspace_settings.name = "Non-Color"
    nt.links.new(uv, scum.inputs["Vector"])
    s = L2.o(scum, "Color")
    col = L2.MIXC(nt, s, (0.01, 0.03, 0.012), (0.06, 0.12, 0.03))
    L2.principled(nt, out, base=col, metal=0.0, rough=L2.MR(nt, s, 0.0, 1.0, 0.05, 0.6),
                  emit=(0.35, 1.0, 0.15), emit_s=L2.MR(nt, s, 0.0, 1.0, 1.6, 0.0))
    return mat


def pool(name, centre, radius, mat, col, rng, tile=2.0):
    """An irregular pool with soft, broken edges (no hard outline): a disc whose rim is pushed out by noise."""
    bm = bmesh.new()
    rings, segs = 6, 48
    cx, cy = centre
    seed = rng.uniform(0, 100)
    centre_v = bm.verts.new((cx, cy, FLOOR + 0.03))
    prev = None
    edge_attr = []
    rows = []
    for r in range(1, rings + 1):
        row = []
        for k in range(segs):
            a = 2 * math.pi * k / segs
            wob = 1.0 + 0.32 * mnoise.noise(Vector((math.cos(a) * 1.3 + seed, math.sin(a) * 1.3, r * 0.05)))
            rr = radius * (r / rings) * wob
            row.append(bm.verts.new((cx + math.cos(a) * rr, cy + math.sin(a) * rr, FLOOR + 0.03 + 0.02 * (r == rings))))
        rows.append(row)
    for k in range(segs):
        bm.faces.new((centre_v, rows[0][k], rows[0][(k + 1) % segs]))
    for ra, rb in zip(rows[:-1], rows[1:]):
        for k in range(segs):
            bm.faces.new((ra[k], rb[k], rb[(k + 1) % segs], ra[(k + 1) % segs]))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    vals = [0.0] + [r / rings for r in range(1, rings + 1) for _ in range(segs)]
    me.attributes.new("Edge", "FLOAT", "POINT").data.foreach_set("value", vals)
    ob = bpy.data.objects.new(name, me)
    col.objects.link(ob)
    world_uv(ob, tile)
    me.materials.append(mat)
    for p in me.polygons:
        p.use_smooth = True
    return ob


def level_dressing(path, col, rng):
    """The map round the battle, dressed like a real Landfall level: game ground tiling, the road with its edge
    modules (enemies walk there, towers never do), build pads on the open ground beside it, dense flora, crystal
    clusters, rocks and scatter on every open patch, rifts and eggs, lava and bog pools, cliffs and ridges."""
    terrain = bpy.data.objects["Terrain"]
    world_uv(terrain, 3.0 * ENV_SCALE)
    terrain.data.materials.clear()
    terrain.data.materials.append(tiled_mat("LevelGround", "Ground", emit=1.5, glow=2.4))
    road = smooth_line(path, ROAD_OFF, -12.0, 78.0)
    ribbon("Road", road, -ROAD_HALF, ROAD_HALF, FLOOR + 0.05, tiled_mat("LevelRoad", "Road"), col, 2.0 * ENV_SCALE)
    for side in (-1, 1):
        for p, ang in along(road, side * (ROAD_HALF + 0.1), 0.98 * ENV_SCALE):
            put_prop("RoadEdge_Straight", col, (p.x, p.y, FLOOR + 0.02), ang + (math.pi if side > 0 else 0.0),
                     ENV_SCALE)
    road_d = lambda x, y: V.seg_dist(np.array([x], np.float32), np.array([y], np.float32), road)[0]  # noqa: E731
    crack_d = lambda x, y: V.seg_dist(np.array([x], np.float32), np.array([y], np.float32), path)[0]  # noqa: E731
    keep_out = []   # (x, y, r): pads, pools, rifts

    def free(x, y, margin=0.6):
        if road_d(x, y) < ROAD_HALF + 0.8 + margin or crack_d(x, y) < 2.4 + margin:
            return False
        return all(math.hypot(x - kx, y - ky) > kr + margin for kx, ky, kr in keep_out)

    for x, y in TOWER_SPOTS(path, road):
        keep_out.append((x, y, 1.6))
    # Pools: lava by the rifts (alien side), toxic bog in the low ground.
    pools = [("LavaPool0", (21.0, 16.0), 3.2, "lava"), ("LavaPool1", (17.5, 34.0), 3.6, "lava"),
             ("LavaPool2", (-17.0, 30.0), 2.8, "lava"), ("BogPool0", (-13.0, 4.0), 2.6, "bog"),
             ("BogPool1", (26.0, 4.0), 3.0, "bog")]
    lava_m, bog_m = mat_lava_pool("LavaPoolM"), mat_bog_pool("BogPoolM")
    for name, c, r, kind in pools:
        pool(name, c, r, lava_m if kind == "lava" else bog_m, col, rng)
        keep_out.append((c[0], c[1], r))
        light(name + "L", (c[0], c[1], FLOOR + 1.0), V.EMBER if kind == "lava" else (0.35, 1.0, 0.2),
              900.0 * r if kind == "lava" else 300.0 * r, r * 0.6, col)
        # A rim of boulders and splinters so the pool sits in the ground.
        for k in range(7):
            a = rng.uniform(0, 6.28)
            put_prop(rng.choice(("Rock_A", "Rock_B", "Decor_Boulder", "Scatter_CrystalSplinters")), col,
                     (c[0] + math.cos(a) * r * 1.05, c[1] + math.sin(a) * r * 1.05, FLOOR), rng.uniform(0, 6.28),
                     rng.uniform(1.0, 2.0), 1.5)
    # Rifts the wave pours out of, glowing, with egg clusters round them.
    rifts = [(36.0, ROAD_HALF + 3.2, 0.3, "Rift_3x1"), (44.0, -ROAD_HALF - 3.0, -0.4, "Rift_2x1"),
             (29.0, ROAD_HALF + 4.2, 1.2, "Rift_2x1"), (12.0, ROAD_HALF + 9.0, 0.7, "Rift_3x1")]
    for k, (y, off, rz, nm) in enumerate(rifts):
        rp = Vector(road[min(range(len(road)), key=lambda i: abs(road[i][1] - y))]) + Vector((off, 0))
        put_prop(nm, col, (rp.x, rp.y, FLOOR + 0.02), rz, ENV_SCALE * 1.3, 5.0)
        light(f"RiftGlow{k}", (rp.x, rp.y, FLOOR + 1.2), (1.0, 0.35, 0.08), 1400.0, 1.0, col)
        keep_out.append((rp.x, rp.y, 3.0))
        for j in range(4):
            a = rng.uniform(0, 6.28)
            put_prop("Egg_Cluster", col, (rp.x + math.cos(a) * 3.4, rp.y + math.sin(a) * 2.6, FLOOR), rng.uniform(0, 6.28),
                     rng.uniform(1.8, 2.8), 4.0)
    # The logo scene's field crystals must not stand on the road, a pad or a pool.
    for ob in list(col.objects):
        if ob.name.startswith("Crystal_") and not free(ob.location.x, ob.location.y, 0.0):
            bpy.data.objects.remove(ob, do_unlink=True)
    crystals = [bpy.data.meshes[n] for n in ("Crystal_A", "Crystal_B", "Crystal_C")]
    # Dense dressing on every open patch: (prop, count, scale range, emission).
    scatter = [("Flora_GlowTree", 40, (2.0, 3.2), 4.0), ("Flora_Frond", 90, (1.8, 3.0), 3.0),
               ("Flora_Mushroom", 30, (1.2, 2.0), 4.0), ("Flora_Tendril", 60, (2.0, 3.4), 4.0),
               ("Decor_Boulder", 40, (1.4, 3.2), 1.5), ("Rock_A", 26, (1.2, 2.8), 1.5), ("Rock_B", 26, (1.2, 2.8), 1.5),
               ("Rock_C", 26, (1.2, 2.8), 1.5), ("Remains_Crystal", 30, (1.6, 2.6), 3.0),
               ("Scatter_CrystalSplinters", 120, (2.0, 3.2), 3.0), ("Scatter_Pebbles", 120, (1.8, 3.0), 1.0),
               ("Scatter_ShellFragments", 50, (1.8, 2.8), 1.0), ("Scree_Patch", 30, (2.0, 3.4), 1.0),
               ("Egg_Cluster", 16, (1.6, 2.4), 3.0), ("Decor_Spire", 8, (2.2, 3.2), 3.0), ("CRYSTAL", 70, (0.7, 1.9), 0)]
    for name, count, (s0, s1), emit in scatter:
        placed = tries = 0
        while placed < count and tries < count * 30:
            tries += 1
            y = -8.0 + 66.0 * rng.random() ** 1.1
            x = rng.uniform(-30.0, 40.0)
            big = name in ("Flora_GlowTree", "Flora_Mushroom", "Flora_Tendril", "Decor_Boulder", "Rock_A", "Rock_B",
                           "Rock_C", "CRYSTAL", "Egg_Cluster")
            # Keep the battery and the near ground readable: nothing tall in front of the towers or near the camera.
            near_battery = any(math.hypot(x - tx, y - ty) < 5.0 for tx, ty in TOWER_SPOTS(path, road))
            if not free(x, y, 0.2) or (name == "Decor_Spire" and y < 18.0) or                     (big and (y < 8.0 and x < 4.0 or near_battery or math.hypot(x + 9.0, y + 10.0) < 16.0)):
                continue
            if name == "CRYSTAL":
                V.instance_mesh(rng.choice(crystals), col, (x, y, FLOOR - 0.05), rng.uniform(0, 6.28),
                                rng.uniform(s0, s1), (rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1)))
            else:
                put_prop(name, col, (x, y, FLOOR), rng.uniform(0, 6.28), rng.uniform(s0, s1), emit)
            placed += 1
    # Road shoulders: litter and splinters where the fighting is.
    for p, _ in along(road, 0.0, 1.2):
        for side in (-1, 1):
            if rng.random() < 0.7:
                put_prop(rng.choice(("Scatter_Pebbles", "Scatter_CrystalSplinters", "Scatter_ShellFragments")), col,
                         (p.x + side * rng.uniform(ROAD_HALF + 0.9, ROAD_HALF + 2.2), p.y, FLOOR),
                         rng.uniform(0, 6.28), rng.uniform(1.6, 2.6), 2.0)
    # Background: cliffs and ridges closing the map, silhouettes against the mist.
    for x, y, rz, s in [(-30.0, 44.0, 0.4, 5.0), (-24.0, 56.0, 0.1, 5.5), (38.0, 46.0, -0.5, 5.0),
                        (30.0, 60.0, -0.2, 6.0), (-38.0, 30.0, 1.0, 4.5), (44.0, 30.0, -1.0, 5.0),
                        (8.0, 66.0, 0.0, 6.5), (-12.0, 64.0, 0.2, 6.0)]:
        put_prop("Cliff_Straight", col, (x, y, FLOOR - 0.2), rz, s, 2.0)
    for x, y, rz, s in [(-10.0, 92.0, 0.0, 5.0), (26.0, 96.0, 0.3, 6.0), (50.0, 80.0, -0.4, 5.0)]:
        put_prop("Silhouette_Ridge", col, (x, y, FLOOR - 1.0), rz, s, 2.0)
    for x, y, s in [(-40.0, 70.0, 4.0), (44.0, 66.0, 3.6)]:
        put_prop("Silhouette_Mesa", col, (x, y, FLOOR - 1.0), 0.0, s, 2.0)
    return road


def tower_spots(path, road):
    """Build pads beside the road like the levels' build spots: on open ground left of the crack (the battery) and on
    the right road shoulder, never on the road. Returns {name: (x, y)}; y and offsets are in metres."""
    spots = {}
    for name, y, off in (("L0", 1.0, -4.0), ("L1", 4.8, -4.4), ("L2", 9.0, -4.0), ("L3", 13.2, -4.6),
                         ("L4", 17.4, -4.2), ("L5", 21.4, -4.6), ("L6", 25.0, -4.4)):
        spots[name] = (x_at(path, y) + off, y)
    for name, y in (("R0", 8.0), ("R1", 15.5)):
        i = min(range(len(road)), key=lambda k: abs(road[k][1] - y))
        spots[name] = (road[i][0] + ROAD_HALF + 2.4, y)
    return spots


def TOWER_SPOTS(path, road):  # noqa: N802
    return list(tower_spots(path, road).values())


def undulate(posed, amp, wavelength, curl, phase=0.0):
    """The game ripples fins and membranes with a vertex wave in Game/UnitLit (SurfaceWave): a sine along the body,
    growing with the distance from the spine. The baked FBX pose has flat wings; this puts the same wave (and a
    little dihedral) into the mesh, in the creature's own frame (faces -Y, span along X, Z up)."""
    key = ("wave", id(posed), amp, wavelength, curl, phase)
    if key in _POSE_CACHE:
        return _POSE_CACHE[key]
    objs = []
    for src in posed.objs:
        me = src.data.copy()
        mw = src.matrix_world
        inv = mw.inverted()
        pts = [mw @ v.co for v in me.vertices]
        hs = max(abs(p.x) for p in pts) or 1.0
        for v, p in zip(me.vertices, pts):
            e = (abs(p.x) / hs) ** 2
            dz = amp * hs * e * math.sin(2 * math.pi * p.y / wavelength + abs(p.x) * 2.2 + phase) + curl * hs * e
            v.co = inv @ (p + Vector((0, 0, dz)))
        ob = bpy.data.objects.new(src.name + "_w", me)
        ob.matrix_world = mw.copy()
        objs.append(ob)
    res = Posed(objs, posed.bones)
    _POSE_CACHE[key] = res
    return res


def boss_face(name, bones, col, scale):
    """Gravemaw's head made readable: a hot glow in the open jaws, two pairs of eyes, a cool rim from behind."""
    jaws = [bones[k].to_translation() for k in ("Jaw_T", "Jaw_UL", "Jaw_UR", "Jaw_LL", "Jaw_LR") if k in bones]
    head = bones["Head"].to_translation()
    mouth = sum(jaws, Vector()) / len(jaws)
    fwd = (mouth - head).normalized()
    side = (bones["Jaw_UR"].to_translation() - bones["Jaw_UL"].to_translation()).normalized()
    up = side.cross(fwd).normalized()
    if up.z < 0:
        up = -up
    glow_ball(name + "Maw", mouth - fwd * 0.15 * scale, 0.28 * scale, col, V4.LAVA_MID, V4.LAVA_HOT, 7.0,
              light_energy=2500.0 * scale, light_radius=0.3 * scale)
    bm = bmesh.new()
    for k, (s, u, f, r) in enumerate([(0.42, 0.34, -0.1, 0.075), (-0.42, 0.34, -0.1, 0.075),
                                      (0.3, 0.46, -0.3, 0.05), (-0.3, 0.46, -0.3, 0.05)]):
        p = head.lerp(mouth, 0.55) + side * s * scale + up * u * scale + fwd * f * scale
        blob(bm, p, r * scale, side, 1.5, subdiv=2)
    fx_obj(name + "Eyes", bm, emit_only(name + "EyesM", V.ALIEN_EYE, 12.0, (1.0, 0.75, 0.35), power=0.6,
                                        transparent=False), col)
    light(name + "EyeL", head.lerp(mouth, 0.5) + up * 0.6 * scale, V.ALIEN_EYE, 600.0 * scale, 0.2, col)
    ld = bpy.data.lights.new(name + "Rim", "SPOT")
    ld.energy = 9000.0 * scale
    ld.color = (0.6, 0.75, 1.0)
    ld.spot_size = math.radians(30)
    ld.spot_blend = 0.7
    ld.shadow_soft_size = 0.5
    rim = bpy.data.objects.new(name + "Rim", ld)
    col.objects.link(rim)
    rim.location = head - fwd * 2.5 * scale + up * 2.0 * scale + side * 1.0 * scale
    C.look_at(rim, head)
    return mouth


def foreground(col, cam_loc, cam_tgt, rng):
    """Framing rocks and crystals near the camera, left and right of the view: out of focus, dark, rimmed."""
    loc, tgt = Vector(cam_loc), Vector(cam_tgt)
    fwd = (tgt - loc)
    fwd.z = 0
    fwd.normalize()
    right = Vector((fwd.y, -fwd.x, 0))
    for side, dist, lat, nm, s in [(-1, 6.5, 4.2, "Rock_B", 3.2), (-1, 8.0, 5.8, "Crystal", 2.2),
                                   (1, 7.0, 5.0, "Decor_Boulder", 3.6), (1, 5.8, 3.8, "Flora_Frond", 3.0)]:
        p = loc + fwd * dist + right * side * lat
        if nm == "Crystal":
            V.instance_mesh(bpy.data.meshes["Crystal_B"], col, (p.x, p.y, FLOOR - 0.1), rng.uniform(0, 6.28), s)
        else:
            put_prop(nm, col, (p.x, p.y, FLOOR - 0.15), rng.uniform(0, 6.28), s, 2.0)


def mist_layer(col):
    """Low, patchy ground mist over the whole map: it takes the lava, explosion and crystal light and gives depth."""
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = "GroundMist"
    ob.location = (4.0, 30.0, FLOOR + 1.2)
    ob.scale = (80, 90, 3.2)
    ob.data.materials.append(V.mat_haze("GroundMistM", (0.5, 0.62, 0.95), 0.045, -0.5, 0.1))
    L2.put(ob, col)


def light_shafts(col):
    """Moonlight breaking through the high haze in a few soft shafts."""
    for k, (x, y, tx, ty, e) in enumerate([(-16.0, 40.0, -6.0, 16.0, 120000.0), (24.0, 58.0, 18.0, 28.0, 100000.0)]):
        ld = bpy.data.lights.new(f"Shaft{k}", "SPOT")
        ld.energy = e
        ld.color = (0.55, 0.68, 1.0)
        ld.spot_size = math.radians(11)
        ld.spot_blend = 0.6
        ld.shadow_soft_size = 0.3
        ob = bpy.data.objects.new(f"Shaft{k}", ld)
        col.objects.link(ob)
        ob.location = (x, y, 34.0)
        C.look_at(ob, Vector((tx, ty, FLOOR)))


def build_scene(draft=False):
    C.reset_scene()
    col_env, col_lit = L2.coll("Env"), L2.coll("Lights")
    col_units, col_fx = L2.coll("Units"), L2.coll("FX")
    path, fs = V.build_env(CFG, col_env)
    # logo_v3.colony() filters the gate's damage-state meshes out of its list but leaves them in the scene at the
    # origin (hidden behind the title in the logo); here they would float over the crack.
    for ob in list(bpy.data.objects):
        if "_Damage" in ob.name or "_Wreck" in ob.name:
            bpy.data.objects.remove(ob, do_unlink=True)
    rng = random.Random(7)
    road = level_dressing(path, col_env, rng)
    spots = tower_spots(path, road)
    mist_layer(col_env)
    light_shafts(col_lit)
    V.sky_light(col_lit, col_env, 0.6)
    ld = bpy.data.lights.new("UnitRim", "SUN")
    ld.color = (0.5, 0.65, 1.0)
    ld.energy = 1.0
    ld.angle = math.radians(4)
    sun = bpy.data.objects.new("UnitRim", ld)
    col_lit.objects.link(sun)
    sun.rotation_euler = (math.radians(-62), 0, math.radians(200))
    sun.light_linking.receiver_collection = col_units

    def R(y, off, z=0.0):
        """A point on the road at depth y, `off` across it (+ right of travel towards the camera = +x)."""
        i = min(range(len(road)), key=lambda k: abs(road[k][1] - y))
        x = road[i][0] + off
        return Vector((x, y, FLOOR + 0.05 + z))

    B = lambda y, off, z=0.0: bank(path, y, off, z)  # noqa: E731
    targets = {}

    # ---- the wave: a swarm down the road towards the gate, flyers above, the boss behind ----------------------
    gate_y = 3.0
    for k in range(26):
        y = gate_y + 3.0 + 26.0 * (k / 25.0) ** 1.2 + rng.uniform(-0.6, 0.6)
        off = rng.uniform(-ROAD_HALF + 0.5, ROAD_HALF - 0.5)
        p = R(y, off)
        po = bake_pose("Skitter/Skitter.fbx", "Skitter", "Walk", (k * 7) % 42, emit=5.0)
        place(po, col_units, p, rng.uniform(-0.35, 0.35) + 0.12, 2.3)
        targets[f"sk{k}"] = p + Vector((0, 0, 0.4))
    # A flank breaking out of the crack towards the battery, close to the camera: they read as creatures.
    for k, (y, f) in enumerate([(-1.5, 3), (1.2, 17), (3.6, 29), (6.4, 9)]):
        p = B(y, -2.1, 0.05)
        po = bake_pose("Skitter/Skitter.fbx", "Skitter", "Walk", f, emit=5.0)
        place(po, col_units, p, math.radians(-90) + rng.uniform(-0.4, 0.4), 2.4)
        targets[f"fl{k}"] = p + Vector((0, 0, 0.4))
    for k, (y, off, f) in enumerate([(9.5, -1.2, 6), (13.0, 1.6, 20), (17.5, -0.4, 12), (22.0, 1.8, 26), (27.0, -1.4, 3)]):
        p = R(y, off)
        place(bake_pose("Cragback/Cragback.fbx", "Cragback", "Walk", f, emit=4.0), col_units, p, 0.15 + k * 0.08, 2.5)
        targets[f"crag{k}"] = p + Vector((0, 0, 1.4))
    for k, (y, off, f) in enumerate([(11.5, 2.3, 4), (15.5, -2.2, 14), (20.0, 2.4, 8)]):
        p = R(y, off)
        place(bake_pose("Roller/Roller.fbx", "Roller", "Roll", f, emit=4.0), col_units, p, 0.1, 2.0)
        targets[f"roll{k}"] = p + Vector((0, 0, 0.5))
    for k, (y, off, f) in enumerate([(24.5, 0.6, 4), (29.0, 2.0, 11), (19.0, -2.6, 16)]):
        p = R(y, off)
        place(bake_pose("Prowler/Prowler.fbx", "Prowler", "Run", f, emit=4.0), col_units, p, 0.3, 2.1)
        targets[f"prow{k}"] = p + Vector((0, 0, 0.7))
    p = R(31.0, -0.8)
    place(bake_pose("PackAlpha/PackAlpha.fbx", "PackAlpha", "Howl", 26, emit=4.0), col_units, p, 0.4, 2.4)
    # The right flank: a pack running in off the road from the near rift, led by a second alpha.
    p = R(13.0, 9.5)
    place(bake_pose("PackAlpha/PackAlpha.fbx", "PackAlpha", "Run", 6, emit=4.0), col_units, p, 0.9, 2.4)
    for k, (y, off, f) in enumerate([(15.5, 11.0, 3), (11.0, 11.5, 9), (17.0, 8.5, 15)]):
        place(bake_pose("Prowler/Prowler.fbx", "Prowler", "Run", f, emit=4.0), col_units, R(y, off), 0.8, 2.1)
    bats = [(8.5, -1.0, 3.8, 2, "Shalebat"), (12.0, 2.5, 4.6, 9, "Shalebat"), (15.0, -2.5, 5.2, 5, "Shalebat"),
            (10.5, 4.5, 5.6, 13, "Shalebat"), (19.0, 1.0, 5.8, 3, "Shalebat"), (17.0, 5.5, 4.2, 7, "Shalebat")]
    for k, (y, off, z, f, sp) in enumerate(bats):
        p = R(y, off, z)
        po = undulate(bake_pose(f"{sp}/{sp}.fbx", sp, "Fly", f, emit=4.0), 0.08, 1.0, 0.14, phase=k)
        place(po, col_units, p, rng.uniform(-0.4, 0.4) - 0.2, 2.1, (math.radians(24), rng.uniform(-0.25, 0.25)))
        targets[f"bat{k}"] = p
    for k, (y, off, z, f) in enumerate([(14.0, -4.0, 6.2, 10), (21.0, -1.5, 7.0, 24), (25.0, -5.0, 6.0, 34),
                                        (10.0, 10.0, 5.0, 18)]):
        p = R(y, off, z)
        po = undulate(bake_pose("Skyray/Skyray.fbx", "Skyray", "Fly", f, emit=4.0), 0.14, 1.3, 0.1, phase=k * 1.7)
        place(po, col_units, p, rng.uniform(-0.3, 0.3), 2.4, (math.radians(26), rng.uniform(-0.3, 0.3)))
        targets[f"ray{k}"] = p
    for k, (y, off, z, f) in enumerate([(9.0, 3.6, 2.6, 0), (16.0, -3.2, 3.2, 20), (23.0, 3.0, 3.0, 40)]):
        p = R(y, off, z)
        place(bake_pose("Wisp/Wisp.fbx", "Wisp", "Float", f, emit=5.0), col_units, p, 0.0, 1.9)
    # Gravemaw, the region boss: rearing over the road behind the wave, close enough to read at thumbnail size.
    gp = R(30.0, 15.0, -1.2)
    gobs, gbones = place(bake_pose("Gravemaw/Gravemaw.fbx", "Gravemaw", "Gate_Mount", 80, emit=6.0), col_units, gp,
                         math.radians(-68), 4.4)
    boss_face("Gravemaw", gbones, col_fx, 4.4)
    # The clip carries the body forward off its root: light and aim at where the body really is.
    lo, hi = L2.world_bbox(gobs)
    C.log("gravemaw bbox", tuple(round(v, 1) for v in lo), tuple(round(v, 1) for v in hi))
    gp = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, FLOOR))
    light("GravemawFront", gp + Vector((-4, -8, 3.0)), V.EMBER, 6000.0, 3.0, col_lit)
    light("GravemawKey", gp + Vector((-10, -14, 14.0)), (0.6, 0.72, 1.0), 26000.0, 4.0, col_lit)
    light("GravemawCrys", gp + Vector((0, 2, 11.0)), V.CRYS_PINK, 3500.0, 4.0, col_lit)
    light("GravemawBack", gp + Vector((3, 12, 7.0)), V.EMBER, 16000.0, 5.0, col_lit)
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = "BossMist"
    ob.location = gp + Vector((0, 8, 3.0))
    ob.scale = (50, 26, 12)
    ob.data.materials.append(V.mat_haze("BossMistM", (0.5, 0.56, 0.72), 0.025, -0.5, 0.15))
    L2.put(ob, col_env)
    ld = bpy.data.lights.new("WaveRim", "SPOT")
    ld.energy = 30000.0
    ld.color = (1.0, 0.45, 0.2)
    ld.spot_size = math.radians(45)
    ld.spot_blend = 0.8
    ld.shadow_soft_size = 1.0
    rim = bpy.data.objects.new("WaveRim", ld)
    col_lit.objects.link(rim)
    rim.location = R(48.0, 0.0, 4.0)
    C.look_at(rim, R(12.0, 0.0, 0.5))
    rim.light_linking.receiver_collection = col_units
    for k, y in enumerate((8.0, 13.0, 18.0, 23.0, 28.0)):
        light(f"AlienRim{k}", R(y + 1.5, 0.0, 0.6), (1.0, 0.3, 0.08), 180.0, 0.8, col_lit)

    # ---- colony: the gate across the road, towers on build pads beside it, drones ------------------------------
    gpos = R(gate_y, 0.0, -0.05)
    place(bake_pose("Gate/Gate_G2.fbx", "Gate", None, 0, keep=["Gate_G2"], emit=5.0), col_units, gpos, 0.0, 2.25)
    light("GateLamp", gpos + Vector((0, -2.0, 2.8)), COLONY_LAMP, 800.0, 1.0, col_lit)
    towers = [
        # (export, variant, build spot, target, pitch)
        ("Tower_Laser", "T3A", "L2", "crag0", 0.0),
        ("Tower_Gun", "T3A", "L1", "fl2", -8.0),
        ("Tower_Cannon", "T3A", "L3", "sk6", 6.0),
        ("Tower_Flak", "T3A", "L4", "bat1", 32.0),
        ("Tower_Tesla", "T3A", "L0", "sk0", 0.0),
        ("Tower_Railgun", "T3A", "L5", "crag3", 0.0),
        ("Tower_Flak", "T2", "L6", "ray1", 38.0),
        ("Tower_Gun", "T2", "R0", "sk3", -4.0),
        ("Tower_Laser", "T2", "R1", "crag1", 0.0),
    ]
    shots = {}
    for k, (name, var, spot, tgt, pitch) in enumerate(towers):
        p = Vector((spots[spot][0], spots[spot][1], FLOOR + 0.07))
        put_prop("BuildPadTile", col_env, p, 0.0, 2.4, 3.0)
        rz = yaw_to(p, targets[tgt])
        po = bake_pose(f"{name}/{name}_{var}.fbx", name, None, 0, aim=(0.0, pitch), emit=3.0)
        _, bones = place(po, col_units, p + Vector((0, 0, 0.12)), rz, TOWER_SCALE)
        muzzles = [m.to_translation() for kk, m in sorted(bones.items()) if kk.startswith("Muzzle")]
        shots[f"{name}{k}"] = (muzzles, targets[tgt])
        light(f"{name}Lamp{k}", p + Vector((0.6, -1.2, 2.4)), COLONY_LAMP, 160.0, 0.6, col_lit)
    sp = R(6.5, -5.2, 3.3)
    arms = ("Shoulder", "Arm", "Hand", "Weapon", "Fore", "Clav", "Finger", "Thumb", "Index")
    po = bake_pose("Mercer/Mercer.fbx", "Mercer", "Jet_Fly", 10, emit=3.0, overlay=("Shoot", 4, arms))
    _, sbones = place(po, col_units, sp, math.radians(20), 1.6)
    pistol = attach_pistol(col_units, sbones["Weapon_R"], Matrix.Rotation(-math.pi / 2, 4, "X"))
    for side in ("Jet_L", "Jet_R"):
        if side in sbones:
            jet_flame("Flame" + side, sbones[side], col_fx)
    light("SpencerKey", sp + Vector((-1.5, -2.0, 1.5)), (0.75, 0.85, 1.0), 160.0, 0.5, col_lit)
    for k, (dp, tgt) in enumerate([(gpos + Vector((3.4, -1.0, 3.6)), gpos + Vector((2.0, 0, 1.2))),
                                   (B(15.0, -6.5, 4.0), B(13.2, -4.6, 0.8))]):
        V.drone(dict(drone=(dp.x, dp.y, dp.z, 1.6, tuple(tgt)), beam=0.5, beam_light=1000.0), col_units)

    # ---- the shots ---------------------------------------------------------------------------------------------
    S = lambda key: shots[key]  # noqa: E731
    m, t = S("Tower_Gun7")
    d = (t - m[0]).normalized()
    muzzle_flash("Gun2Flash", m[0], d, col_fx, seed=21)
    tracers("Gun2Tracers", m[0] + d * 0.6, t, col_fx, count=5, seed=22)
    m, t = S("Tower_Laser8")
    beam("Laser2", m[0], t, col_fx, LASER, LASER_CORE, 0.07, 3.5, lights=3)
    sparks("Laser2Sparks", t, col_fx, rng, 40, 1.0, colour=(1.0, 0.7, 0.35), core=(1.0, 0.85, 0.6), strength=8.0,
           size=0.013)
    m, t = S("Tower_Laser0")
    beam("Laser", m[0], t, col_fx, LASER, LASER_CORE, 0.09, 4.0)
    glow_ball("LaserHit", t, 0.35, col_fx, LASER, LASER_CORE, 5.0, light_energy=400.0)
    sparks("LaserSparks", t, col_fx, rng, 50, 1.2, colour=(1.0, 0.7, 0.35), core=(1.0, 0.85, 0.6), strength=8.0,
           size=0.014)
    m, t = S("Tower_Gun1")
    d = (t - m[0]).normalized()
    muzzle_flash("GunFlash", m[0], d, col_fx, seed=1)
    tracers("GunTracers", m[0] + d * 0.6, t, col_fx, count=6, seed=2)
    glow_ball("GunHit", t, 0.18, col_fx, TRACER, TRACER_CORE, 5.0, light_energy=120.0)
    m, t = S("Tower_Cannon2")
    for k, mz in enumerate(m):
        d = (t - mz).normalized()
        muzzle_flash(f"CannonFlash{k}", mz, d, col_fx, size=0.6, strength=7.0, energy=300.0, seed=3 + k)
        smoke(f"CannonSmoke{k}", mz + d * 0.7 + Vector((0, 0, 0.2)), 0.5, col_fx, colour=(0.12, 0.12, 0.13),
              density=2.0)
    ep = Vector((t.x + 0.3, t.y - 0.3, FLOOR + 0.1))
    explosion("Shell", ep, col_fx, 1.8, seed=4)
    debris("ShellRocks", ep + Vector((0, 0, 0.9)), col_fx, rng, 14, 1.6)
    for k in range(3):
        smoke(f"ShellColumn{k}", ep + Vector((rng.uniform(-0.6, 0.6), 0.4 * k, 2.6 + 1.5 * k)), 1.3 + 0.5 * k, col_fx,
              colour=(0.08, 0.075, 0.08), density=2.2 - 0.5 * k, glow=FIRE, glow_strength=0.5 / (k + 1))
    for key in ("Tower_Flak3", "Tower_Flak6"):
        m, t = S(key)
        for k, mz in enumerate(m[:2]):
            d = (t - mz).normalized()
            muzzle_flash(f"{key}Flash{k}", mz, d, col_fx, colour=FLAK, size=0.4, strength=7.0, energy=150.0,
                         seed=6 + k)
            tracers(f"{key}Trail{k}", mz + d * 0.6, t - d * 1.0, col_fx, count=3, length=0.6, width=0.025,
                    colour=FLAK, seed=8 + k)
        flak_burst(key + "B0", t + Vector((0.6, -0.5, 0.3)), col_fx, 1.1, seed=10)
        flak_burst(key + "B1", t + Vector((-1.0, 0.6, -0.5)), col_fx, 0.9, seed=11)
    flak_burst("FlakB2", targets["bat2"] + Vector((1.2, 0.0, 0.8)), col_fx, 0.8, seed=12, smoke_only=True)
    flak_burst("FlakB3", targets["bat3"] + Vector((-0.9, -0.4, -0.5)), col_fx, 0.8, seed=13)
    flak_burst("FlakB4", targets["ray0"] + Vector((0.8, 0.3, 0.4)), col_fx, 1.0, seed=18)
    m, t = S("Tower_Tesla4")
    trng = random.Random(14)
    arc("Tesla", m[0], t, col_fx, trng)
    arc("TeslaChain1", t, targets["sk1"], col_fx, trng, width=0.011, branches=2)
    arc("TeslaChain2", targets["sk1"], targets["sk3"], col_fx, trng, width=0.009, branches=1)
    for k, key in enumerate(("sk0", "sk1", "sk3")):
        glow_ball(f"TeslaHit{k}", targets[key], 0.25, col_fx, TESLA, TESLA_CORE, 4.0, light_energy=160.0)
    m, t = S("Tower_Railgun5")
    beam("Rail", m[0], t, col_fx, RAIL, (0.8, 0.9, 1.0), 0.035, 8.0, lights=3, light_energy=40.0)
    muzzle_flash("RailFlash", m[0], (t - m[0]).normalized(), col_fx, colour=RAIL, core=(0.8, 0.9, 1.0), size=0.4,
                 strength=6.0, energy=120.0, seed=15)
    sparks("RailSparks", t, col_fx, rng, 60, 1.4, colour=(1.0, 0.72, 0.4), core=(1.0, 0.88, 0.6), strength=9.0,
           size=0.014)
    glow_ball("RailHit", t, 0.3, col_fx, RAIL, (0.8, 0.9, 1.0), 5.0, light_energy=300.0)
    # Spencer's pistol bolts into the boss's face.
    hand = sbones["Weapon_R"].to_translation()
    pts = [o.matrix_world @ v.co for o in pistol for v in o.data.vertices]
    hand = max(pts, key=lambda q: (q - hand).length)
    tgt = targets["bat0"]
    d = (tgt - hand).normalized()
    tracers("SpencerBolts", hand + d * 0.4, tgt, col_fx, count=4, length=0.8, width=0.03, colour=LASER,
            core=LASER_CORE, seed=16)
    muzzle_flash("SpencerFlash", hand + d * 0.15, d, col_fx, colour=LASER, core=LASER_CORE, size=0.25, strength=6.0,
                 energy=60.0, seed=17)
    # Battle smoke drifting over the road, catching the lava and fire light.
    for k, (y, off, r) in enumerate([(6.5, 1.0, 1.8), (10.0, 3.0, 1.6), (16.0, -1.0, 2.0), (21.0, 2.5, 2.2)]):
        smoke(f"BattleSmoke{k}", R(y, off, 1.6), r, col_fx, colour=(0.16, 0.15, 0.16), density=0.8)
    for ob in bpy.data.objects:
        if ob.type == "LIGHT":
            ob.visible_camera = False
    return path


# =============================================================================================================
# Cameras and output
# =============================================================================================================

GRADE = dict(V.GRADE)
GRADE.update(bloom=0.35, fog=0.14)

SHOTS = {
    # name: (resolution, location, target, lens, fstop)
    "wide": ((3840, 1240), (-12.0, -17.0, 6.8), (4.0, 15.0, 3.6), 27.0, 4.0),
    "std": ((2876, 1620), (-9.0, -10.0, 6.5), (3.5, 14.0, 4.2), 26.0, 4.0),
    "header": ((1840, 860), (-9.0, -10.0, 6.3), (3.5, 14.0, 5.4), 26.0, 4.0),
    "vert": ((1500, 1800), (-5.0, -10.0, 8.0), (4.0, 14.0, 4.2), 28.0, 4.0),
}


def shot_camera(name, loc, target, lens, fstop):
    cd = bpy.data.cameras.new(name)
    cd.lens = lens
    cd.sensor_fit = "AUTO"
    cd.sensor_width = 36
    cd.clip_end = 400
    cam = C.link(bpy.data.objects.new(name, cd))
    cam.location = loc
    C.look_at(cam, Vector(target))
    cd.dof.use_dof = True
    cd.dof.focus_distance = (Vector(target) - Vector(loc)).length
    cd.dof.aperture_fstop = fstop
    return cam


def main():
    args = C.cli_args()
    os.makedirs(OUT, exist_ok=True)
    L2.enable_gpu()
    if args.get("threads"):
        bpy.context.scene.render.threads_mode = "FIXED"
        bpy.context.scene.render.threads = int(args["threads"])
    draft = bool(args.get("draft"))
    samples = int(args.get("samples", 24 if draft else 96))
    shot = args.get("shot", "wide")
    names = list(SHOTS) if shot == "all" else shot.split(",")
    build_scene(draft)
    rng = random.Random(3)
    fg = {}
    for n in names:
        fg[n] = L2.coll("FG_" + n)
        foreground(fg[n], SHOTS[n][1], SHOTS[n][2], rng)
    L2.SUPER = 1
    for n in names:
        for k, c in fg.items():
            c.hide_render = k != n
        (w, h), loc, tgt, lens, fstop = SHOTS[n]
        if draft:
            w, h = w // 3, h // 3
        elif args.get("half"):
            w, h = w // 2, h // 2
        cam = shot_camera("Cam_" + n, loc, tgt, lens, fstop)
        sfx = "_draft" if draft else ""
        L2.render_png(cam, w, h, os.path.join(OUT, f"keyart_{n}{sfx}.png"), samples, GRADE)
    if args.get("save"):
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "_keyart.blend"))


if __name__ == "__main__":
    main()
