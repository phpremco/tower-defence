"""Shared helpers for round 9: our side's structures and machines (gate, base, base turret, drones, gadgets).

Everything here builds on the round-7 hard-surface kit (`tower_kit.py`: pieces, atlas, self-bake, skinned export)
and reuses the tower build's pad and rank plate (`build_towers.py`), so the gate, the base, the drones and the gadgets
come out of the same workshop as the towers. Only additions live here:

- `hex_poly`, `shards`: hex armour panels and plates broken into a few shards (damage states);
- `clip`: like `tower_kit.clip`, plus per-bone **scale** keys (hex panels popping in, rings unfolding);
- `module_object`: assemble a set of pieces as one object whose origin is its own pivot (the base modules);
- `bake_set`: atlas + self-bake + preview material + report block in one call;
- comparison staging: imported round-7 towers and Skitters at true scale beside the new models.

Coordinates as in tower_kit: Blender world, Z up, the machine faces -Y (Unity +Z), the game camera looks from +Y.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

import common as C
import tower_kit as tk

UP = Vector((0, 0, 1))
FWD = Vector((0, -1, 0))
XAX = Vector((1, 0, 0))

GLOW = {
    "gate": (0.55, 0.75, 1.0),
    "base": (0.3, 0.7, 1.0),
    "cyan": (0.35, 0.95, 1.0),
    "azure": (0.32, 0.66, 1.0),
    "amber": (1.0, 0.78, 0.3),
    "chrono": (0.5, 0.78, 1.0),
    "crysium": (0.88, 0.82, 1.0),
    "yellow": (1.0, 0.9, 0.45),
    "ember": (1.0, 0.42, 0.12),
    "star": (1.0, 0.88, 0.35),
}


def V(x, y, z):
    return Vector((x, y, z))


def glow(name, strength=1.0, lens=0.0):
    return tk.glow(GLOW.get(name, name) if isinstance(name, str) else name, strength, lens)


def burn(look):
    """A burnt-out version of a look (soot-black with a hint of the colour, no glow), as the tower wreck."""
    paint = tuple(np.asarray(look.paint) * 0.4 + 0.025)
    return look.but(paint=paint, emit=None, glow=0.0, fam="metal" if look.fam == "glow" else look.fam,
                    rough=0.75, wear=1.0, lens=0.0)


def scorch(look, amount=0.55):
    """Soot over a paint: darker and rougher, keeps its family (for scorch patches on intact structures)."""
    paint = tuple(np.asarray(look.paint) * (1 - amount) + 0.03 * amount)
    return look.but(paint=paint, rough=min(0.9, look.rough + 0.25), wear=1.0)


# -------------------------------------------------------------------------------------------------------------
# Shapes
# -------------------------------------------------------------------------------------------------------------

def hex_poly(r, flat=True):
    """A regular hexagon (counter-clockwise), corner radius r; flat sides top and bottom when `flat`."""
    off = 0.0 if flat else math.pi / 6
    return [(math.cos(off + i * math.pi / 3) * r, math.sin(off + i * math.pi / 3) * r) for i in range(6)]


def rect_poly(w, h, chamfer=0.0):
    x, y, c = w / 2, h / 2, chamfer
    if c <= 0:
        return [(-x, -y), (x, -y), (x, y), (-x, y)]
    return [(-x + c, -y), (x - c, -y), (x, -y + c), (x, y - c), (x - c, y), (-x + c, y), (-x, y - c), (-x, -y + c)]


def octagon(w, h, chamfer):
    return rect_poly(w, h, chamfer)


def _clip_poly(poly, n, d):
    """Keeps the part of a convex polygon with dot(p, n) <= d (Sutherland-Hodgman, one plane)."""
    out = []
    m = len(poly)
    for i in range(m):
        a, b = poly[i], poly[(i + 1) % m]
        da = a[0] * n[0] + a[1] * n[1] - d
        db = b[0] * n[0] + b[1] * n[1] - d
        if da <= 0:
            out.append(a)
        if (da < 0) != (db < 0) and abs(da - db) > 1e-12:
            t = da / (da - db)
            out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return out


def shards(k, name, poly, thickness, m, look, bone, on=None, count=3, gap=0.012, lift=0.01, tilt=9.0, seed=0,
           bevel=0.003, drop=()):
    """A convex plate broken into `count` wedge shards round a point near its centre: each wedge is shrunk away from
    the crack lines by `gap`, lifted and tilted a little in the plate's frame. `drop`: indices of shards left out
    (fallen off). Returns the shard pieces."""
    rng = np.random.default_rng(seed)
    cx = sum(p[0] for p in poly) / len(poly)
    cy = sum(p[1] for p in poly) / len(poly)
    span = max(max(abs(p[0] - cx), abs(p[1] - cy)) for p in poly)
    c = (cx + rng.uniform(-0.2, 0.2) * span, cy + rng.uniform(-0.2, 0.2) * span)
    angles = np.sort(rng.uniform(0, 2 * math.pi, 1)[0] + np.arange(count) * 2 * math.pi / count
                     + rng.uniform(-0.35, 0.35, count))
    out = []
    for i in range(count):
        if i in drop:
            continue
        a0, a1 = angles[i], angles[(i + 1) % count] + (2 * math.pi if i == count - 1 else 0.0)
        wedge = [(p[0] - c[0], p[1] - c[1]) for p in poly]
        # Keep the wedge between the rays a0 and a1 (each ray cuts with a half-plane through the centre).
        # Left of the ray a0 and right of the ray a1, each pulled in by half the crack gap.
        wedge = _clip_poly(wedge, (math.sin(a0), -math.cos(a0)), -gap / 2)
        wedge = _clip_poly(wedge, (-math.sin(a1), math.cos(a1)), -gap / 2)
        if len(wedge) < 3:
            continue
        mid = (a0 + a1) / 2
        shift = V(math.cos(mid), math.sin(mid), 0) * gap
        rot = Matrix.Rotation(math.radians(rng.uniform(-tilt, tilt)), 4, V(-math.sin(mid), math.cos(mid), 0))
        local = Matrix.Translation(V(c[0], c[1], lift * rng.uniform(0.3, 1.0)) + shift) @ rot
        out.append(k.plate(f"{name}_Shard", wedge, thickness, m @ local, look, bone, on, bevel=bevel))
    return out


# -------------------------------------------------------------------------------------------------------------
# Clips with scale keys
# -------------------------------------------------------------------------------------------------------------

def clip(arm, name, frames, keys, loop=False):
    """`tower_kit.clip` with an extra "scale" key (a float or a 3-tuple in the bone's own axes) per bone."""
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    bones = [b.name for b in arm.data.bones]
    for f in range(frames + 1):
        t = (f % frames) / frames if loop else f / frames
        spec = keys(t, f)
        for bname in bones:
            pb = arm.pose.bones[bname]
            s = spec.get(bname, {})
            q = Quaternion()
            if "rot" in s:
                axis, angle = s["rot"]
                q = Quaternion(tk.local_axis(arm, bname, axis), angle)
            loc = Vector()
            if "loc" in s:
                rest = arm.data.bones[bname].matrix_local.to_3x3()
                loc = rest.inverted() @ Vector(s["loc"])
            sc = s.get("scale", 1.0)
            sc = (sc, sc, sc) if isinstance(sc, (int, float)) else tuple(sc)
            pb.rotation_quaternion = q
            pb.location = loc
            pb.scale = sc
            pb.keyframe_insert("rotation_quaternion", frame=f, group=bname)
            pb.keyframe_insert("location", frame=f, group=bname)
            pb.keyframe_insert("scale", frame=f, group=bname)
    return act


def pop(t, start, length=0.18, overshoot=0.22):
    """0 before `start`, then a quick grow with an overshoot that settles at 1 (a panel snapping into place)."""
    u = (t - start) / length
    if u <= 0:
        return 0.0
    if u >= 1:
        return 1.0
    if u < 0.6:
        return (u / 0.6) * (1.0 + overshoot)
    return 1.0 + overshoot * (1.0 - (u - 0.6) / 0.4)


def ease(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def rest_pose(arm):
    if arm.animation_data:
        arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)


# -------------------------------------------------------------------------------------------------------------
# Objects, bake, export
# -------------------------------------------------------------------------------------------------------------

def module_object(pieces, name, pivot, mat, lod=False):
    """Pieces as one static object whose origin is `pivot` (world): the mesh is moved so the pivot is (0, 0, 0)."""
    obj = tk.assemble(pieces, name, lod=lod)
    obj.vertex_groups.clear()
    p = Vector(pivot)
    obj.data.transform(Matrix.Translation(-p))
    obj.location = p
    obj.data.materials.append(mat)
    return obj


def bake_set(kit, out_dir, size=1024, low_size=512, bake=True, samples=12):
    """Unwraps and self-bakes the kit (or reuses the maps on disk with bake=False) and returns
    (preview material, report block)."""
    uv = tk.unwrap_kit(kit)
    paths = {s.lower(): os.path.join(out_dir, f"{kit.name}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
    if bake or not all(os.path.exists(p) for p in paths.values()):
        paths = tk.bake(kit, out_dir, size=size, low_size=low_size, samples=samples)
    entries, detail = tk.detail_entries(uv["world_per_uv"])
    mat = C.game_material(kit.name + "_Mat", paths, 1.5, 0.45, entries)
    report = {"asset": kit.name, "texture_size": size, "low_size": low_size,
              "maps": ["Albedo", "Normal", "Emission", "Mask"],
              "mask": "R AO, G smoothness, B metal detail mask, A ceramic detail mask", "uv": uv, "detail": detail}
    return mat, report


def skinned(kit, variant, bones, mat, name, lod=True, arm_name=None):
    """Armature + LOD0 (+ LOD1) of one variant, every piece rigid on its bone."""
    arm = tk.build_armature(arm_name or f"{name}_Rig", bones)
    objs = [arm]
    pieces = kit.of(variant)
    lod0 = tk.assemble(pieces, name)
    lod0.data.materials.append(mat)
    tk.skin_to(lod0, arm)
    objs.append(lod0)
    if lod:
        lod1 = tk.assemble(pieces, name + "_LOD1", lod=True)
        lod1.data.materials.append(mat)
        tk.skin_to(lod1, arm)
        objs.append(lod1)
    return objs


def extra_mesh(kit, variant, arm, mat, name, lod=False):
    """Another mesh of `variant` skinned to an existing armature (e.g. a damage overlay or a cargo pod)."""
    o = tk.assemble(kit.of(variant), name, lod=lod)
    o.data.materials.append(mat)
    tk.skin_to(o, arm)
    return o


def remove(objs):
    for o in objs:
        if o is not None and o.name in bpy.data.objects:
            bpy.data.objects.remove(o)


def clear_actions():
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)


# -------------------------------------------------------------------------------------------------------------
# Preview staging
# -------------------------------------------------------------------------------------------------------------

def road_stage(width=2.0, length=40.0, axis="y", centre=(0, 0)):
    """Dark ground with a lighter road strip `width` wide running along `axis` (as a level's road)."""
    g = C.ground(color=(0.035, 0.04, 0.05))
    import bmesh
    road = bpy.data.meshes.new("Road")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    bm.to_mesh(road)
    bm.free()
    r = C.link(bpy.data.objects.new("Road", road))
    r.scale = (width, length, 1) if axis == "y" else (length, width, 1)
    r.location = (centre[0], centre[1], 0.002)
    road.materials.append(C.principled("RoadMat", color=(0.075, 0.08, 0.095), rough=0.9))
    return [g, r]


def render_game(target, out, zoom=1.0, width=1280, height=720, bloom=True):
    lights = tk.game_lights()
    cam = tk.game_camera(target, width / tk.PX_PER_UNIT / zoom)
    C.render(cam, out, width, height, bloom=bloom)
    remove(lights + [cam])


def render_close(target, out, distance, azimuth=28, elevation=26, lens=50, width=960, height=720, light_scale=1.0,
                 bloom=True):
    rig = tk.close_lights(light_scale)
    cam = tk.closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=lens)
    C.render(cam, out, width, height, bloom=bloom)
    remove(rig + [cam])


_TOWERS = {}


def tower(name, tier, location, yaw_deg=0.0, head_deg=115.0, stars=None):
    """A round-7 tower rebuilt in the scene from `build_towers.py` (no bake, no export: its maps on disk are only
    read) at true scale, LOD0 only. The tower FBX files are not imported because round 7's export holds no meshes
    (see `export_fbx` below). Returns the objects; the armature is first."""
    import build_towers as bt
    if name not in _TOWERS:
        spec = bt.SPECS[name]()
        kit = spec.kit()
        uv = tk.unwrap_kit(kit)
        folder = os.path.join(C.EXPORTS, kit.name)
        tex = {s.lower(): os.path.join(folder, f"{kit.name}_{s}.png") for s in ("Albedo", "Normal", "Emission", "Mask")}
        entries, _ = tk.detail_entries(uv["world_per_uv"])
        _TOWERS[name] = (spec, kit, C.game_material(kit.name + "_CmpMat", tex, 1.5, 0.45, entries))
    spec, kit, mat = _TOWERS[name]
    objs = bt.build_variant(spec, kit, tier, mat, name=f"{kit.name}_{tier}_Cmp{len(bpy.data.objects)}", lod=False)
    arm = objs[0]
    arm.location = Vector(location)
    arm.rotation_euler.z = math.radians(yaw_deg)
    bt.pose(arm, head_deg, spec.preview_pitch, bt.stars_for(tier) if stars is None else stars)
    return objs


def export_fbx(path, objects, animated):
    """`tower_kit.export_fbx` after a view-layer update. Objects made with `bpy.data.objects.new` are not in
    `view_layer.objects` until the layer updates, so tower_kit's selection loop skipped every mesh and the round-7
    FBX files hold only the armature. Updating first selects (and exports) the meshes."""
    bpy.context.view_layer.update()
    tk.export_fbx(path, objects, animated)


def skitters(positions, yaw=math.radians(90), scale=0.45):
    """Walking Skitters (from Skitter.blend, read only) for scale; returns staged copies or [] if unavailable."""
    import boss_kit
    try:
        arm, mesh, actions = boss_kit.append_base("Skitter")
    except Exception as e:  # renders still work without enemies
        C.log("no Skitter", e)
        return []
    walk = actions.get("Walk") or next(iter(actions.values()))
    entries = [(arm, mesh, walk, 3 * j, p, yaw if not isinstance(p, tuple) or len(p) < 4 else p[3], scale)
               for j, p in enumerate(positions)]
    entries = [(a, m, w, f, tuple(p[:3]), y, s) for a, m, w, f, p, y, s in entries]
    copies = boss_kit.stage(entries)
    bpy.context.scene.frame_set(1000)
    return copies


def unstage(copies):
    import boss_kit
    if copies:
        boss_kit.unstage(copies)
