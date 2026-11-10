"""Deepwake logo, round 3 (2026-11-10): the FAULTLINE series lockup Remco picked, rebuilt around the game's own assets.

Feedback on v2 and what this round does about it:
- Crystals: the crystals are now the game's Crysium. The field crystals are the real Crystal_A/B/C formations, rebuilt
  at source resolution from Tools/Blender/build_props.py (its builder functions are executed from that file, not
  copied) and coloured by crysium.shard_style (violet root, pink body, icy tip, light leaking from flutes and node
  rings, dark inclusions). The shards in the title's fault and in the emblem use the same PartBuilder.shard + style.
- Lava crack: the game's Gravemaw_Fissure modules (ArtSource/Exports/Gravemaw_Fissure: molten core with cooled crust
  plates, stepped walls, heaved slab lips, ground skirt) are chained along a path with their own textures; the ground
  veins around it use the game's EMBER colour.
- Other game elements, kept subtle: colony base and gates as horizon lights, a repair drone with a light beam,
  Skitter silhouettes on the lips, pairs of alien eyes in the dark, the Silhouette_CrystalTower spires in the haze.
- Background: no canyon walls. Variants: a Crysium field receding into the game's Crystal Plains mist, a dark
  plateau with the colony on the horizon, and a wide crack with the planet's glow underneath (no series line).
- v2 weaknesses: the faces are gunmetal with panel seams and a value gradient instead of mirror chrome; every
  emission is capped well below white (AgX stays in colour); the alpha logo's glow only becomes alpha where it is
  strong and close to the letters.

Out (ArtSource/Previews/Logo/v3/): <Variant>_header_920x430.png, <Variant>_capsule_616x353.png,
<Variant>_library_logo.png (1280x720, alpha), <Variant>_emblem_512.png / _256.png, Deepwake_v3_contact_sheet.png.

Run (one variant per Blender process; while Unity workers share the machine add --threads 4 and below-normal
priority; the finals of 2026-11-10 used 80 samples for the emblems/colony set and 128 for field/deep):
  blender -b --factory-startup --threads 4 --python Tools/Blender/logo_v3.py -- --variant field|colony|deep
          [--only header,capsule,library,emblem] [--samples 80] [--half | --draft]
  blender -b --factory-startup --python Tools/Blender/logo_v3.py -- --sheet
"""

import ast
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
import crysium as Y  # noqa: E402
import logo_v2 as L2  # noqa: E402

OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Logo", "v3")
CACHE = os.path.join(OUT, "_cache")
FONTS = os.path.join(C.REPO, "ArtSource", "Previews", "Logo", "v2", "_fonts")
EX = C.EXPORTS
SERIES_LINE = "FAR HARVEST"

HEADER, CAPSULE, LIBRARY = (920, 430), (616, 353), (1280, 720)
FLOOR = -1.8


def lin(*c):
    return tuple(float(v) for v in C.srgb_to_linear(np.array(c)))


# Game colours (env_kit.py, crysium.py, build_liquids.py), converted to linear light.
EMBER = lin(1.0, 0.6, 0.28)
EMBER_HOT = lin(1.0, 0.8, 0.55)
CRYS_PINK = lin(*Y.GLOW_PINK)
CRYS_ICE = lin(*Y.GLOW_ICE)
COLONY = lin(0.62, 0.86, 1.0)
COLONY_BLUE = (0.05, 0.25, 1.0)
MIST_PLAINS = (0.55, 0.60, 0.75)
ALIEN_EYE = lin(1.0, 0.32, 0.12)


# =============================================================================================================
# Game assets
# =============================================================================================================

def props_namespace():
    """build_props.py runs main() at import; execute only its definitions so the real Crystal_A/B/C builders are used."""
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build_props.py")
    with open(path, encoding="utf-8") as f:
        tree = ast.parse(f.read(), path)
    tree.body = [n for n in tree.body if not (isinstance(n, ast.Expr) and isinstance(n.value, ast.Call))]
    ns = {"__file__": path, "__name__": "build_props_defs"}
    exec(compile(tree, path, "exec"), ns)  # noqa: S102
    return ns


def mat_crysium(name, strength=3.0, edge_gain=0.35):
    """The Crysium source surface as the game bakes it (vertex Albedo/Emit/Rough written by crysium.shard_style),
    shaded live: lighter ridges from mesh convexity, frosted sheen, glow from the Emit mask."""
    mat, nt, out = L2.new_mat(name)
    alb = L2.N(nt, "ShaderNodeAttribute", attribute_name="Albedo", attribute_type="GEOMETRY")
    emi = L2.N(nt, "ShaderNodeAttribute", attribute_name="Emit", attribute_type="GEOMETRY")
    rgh = L2.N(nt, "ShaderNodeAttribute", attribute_name="Rough", attribute_type="GEOMETRY")
    geo = L2.N(nt, "ShaderNodeNewGeometry")
    edge = L2.MR(nt, L2.o(geo, "Pointiness"), 0.5, 0.58, 1.0, 1.0 + edge_gain, smooth=False)
    mul = L2.N(nt, "ShaderNodeVectorMath", operation="SCALE")
    nt.links.new(L2.o(alb, "Color"), mul.inputs[0])
    nt.links.new(edge, L2.sock(mul.inputs, "Scale"))
    principled_ = L2.principled(nt, out, base=mul.outputs[0], metal=0.0, rough=L2.o(rgh, "Factor"),
                                emit=L2.o(emi, "Color"), emit_s=strength, coat=0.25, coat_rough=0.12, spec=0.55)
    return mat, principled_


def fused(builder, name, voxel, smooth=1, strength=3.0, mat=None):
    """PartBuilder parts -> the game's voxel fuse + style pass (common.fuse / common.detail) -> a shaded object."""
    parts = builder.to_object(name + "_Parts")
    me = C.fuse(parts, voxel, smooth)
    bpy.data.objects.remove(parts)
    C.detail(me, builder)
    C.shade_smooth(me)
    ob = C.link(bpy.data.objects.new(name, me))
    me.materials.append(mat or mat_crysium(name + "_M", strength)[0])
    return ob


def field_crystal_meshes():
    """Crystal_A/B/C at source resolution, cached as meshes (the fuse is the slow part)."""
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, "crysium_props.blend")
    names = ("Crystal_A", "Crystal_B", "Crystal_C")
    if not os.path.exists(path):
        ns = props_namespace()
        meshes = set()
        for n in names:
            ob = fused(ns[n.lower()](), n, 0.007, 1)
            ob.data.name = n
            meshes.add(ob.data)
            bpy.data.objects.remove(ob)
        bpy.data.libraries.write(path, meshes, fake_user=True)
        for m in list(meshes):
            bpy.data.meshes.remove(m)
    with bpy.data.libraries.load(path) as (src, dst):
        dst.meshes = [n for n in src.meshes if n in names]
    mat = mat_crysium("FieldCrysium", 1.6)[0]
    out = {}
    for m in dst.meshes:
        m.materials.clear()
        m.materials.append(mat)
        out[m.name] = m
    return out


def game_mat(name, tex, emit=2.0, alpha=False):
    t = {k: os.path.join(EX, tex + "_" + s + ".png") for k, s in
         (("albedo", "Albedo"), ("mask", "Mask"), ("emission", "Emission"), ("normal", "Normal"))}
    mat = C.game_material(name, t, emission_strength=emit)
    if alpha:
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        img = next(n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image and n.image.filepath.endswith("Albedo.png"))
        nt.links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
    return mat


def import_fbx(rel, tex, emit=2.0, keep=None, alpha=False):
    """Import a game FBX, drop LODs/variants not in `keep`, give the meshes the game material. Returns objects."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(EX, rel))
    new = [o for o in bpy.data.objects if o not in before]
    mat = game_mat(os.path.basename(tex), tex, emit, alpha)
    obs = []
    for o in new:
        if o.type != "MESH" or "LOD1" in o.name or (keep and not any(k in o.name for k in keep)):
            bpy.data.objects.remove(o)
            continue
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
        o.data.materials.clear()
        o.data.materials.append(mat)
        obs.append(o)
    for o in new:
        try:
            if o.type == "EMPTY" or o.type == "ARMATURE":
                bpy.data.objects.remove(o)
        except ReferenceError:
            pass
    return obs


def instance(src, collection, loc, rot_z=0.0, scale=1.0, tilt=(0.0, 0.0)):
    """A linked copy of an imported object; the import transform (FBX axes, part offsets) is kept underneath."""
    ob = bpy.data.objects.new(src.name + "_i", src.data)
    collection.objects.link(ob)
    ob.matrix_world = (Matrix.Translation(loc) @ Euler((tilt[0], tilt[1], rot_z)).to_matrix().to_4x4()
                       @ Matrix.Diagonal((scale, scale, scale, 1.0)) @ src.matrix_world)
    return ob


def instance_mesh(me, collection, loc, rot_z=0.0, scale=1.0, tilt=(0.0, 0.0)):
    ob = bpy.data.objects.new(me.name + "_i", me)
    collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (tilt[0], tilt[1], rot_z)
    ob.scale = (scale,) * 3
    return ob


# =============================================================================================================
# Lava crack: the game's Gravemaw_Fissure modules chained along a path
# =============================================================================================================

FISSURE_MODULES = {  # end offset (Unity x, z) and end yaw, from Gravemaw_Fissure_report.json
    "Start": ((0.0, 2.0), 0.0), "Head": ((0.0, 2.0), 0.0),
    "Straight_A": ((0.0, 2.0), 0.0), "Straight_B": ((0.0, 3.0), 0.0), "Straight_C": ((0.0, 2.0), 0.0),
    "Curve_L": ((-0.3455, 1.9596), -20.0), "Curve_R": ((0.3455, 1.9596), 20.0),
}
GAP_HALF = 0.27     # gap_top_width_m / 2
FULL_HALF = 0.72    # skirt edge


def fissure_material(slot_name, mat_rock):
    if "Punch" in slot_name:
        mat, nt, out = L2.new_mat("FissurePunch")
        nt.links.new(L2.o(L2.N(nt, "ShaderNodeBsdfTransparent")), out.inputs["Surface"])
        return mat
    return mat_rock


def fissure_chain(collection, start, heading_deg, scale, seq, emit=5.0):
    """Places the modules end to end; returns the centre-line polyline (x, y) in world space."""
    objs = import_fbx("Gravemaw_Fissure/Gravemaw_Fissure.fbx", "Gravemaw_Fissure/Gravemaw_Fissure", emit, alpha=True)
    rock = objs[0].data.materials[0]
    # The vertex colour's alpha fades the skirt into the terrain (footprint fade).
    nt = rock.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    alpha_link = bsdf.inputs["Alpha"].links[0].from_socket
    vc = L2.N(nt, "ShaderNodeVertexColor")
    nt.links.new(L2.M(nt, "MULTIPLY", alpha_link, L2.o(vc, "Alpha")), bsdf.inputs["Alpha"])
    # Unity shows the map through its own tonemapper; under AgX the same ember mask at strength turns peach, so the
    # melt is pushed towards the game's deep lava ramp (build_liquids.py, Ashen Reach) before it gets bright.
    em_link = bsdf.inputs["Emission Color"].links[0].from_socket
    tint = L2.N(nt, "ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
    L2.setv(nt, tint, "Factor", 1.0)
    nt.links.new(em_link, L2.sock(tint.inputs, "A"))
    L2.setv(nt, tint, "B", (1.0, 0.42, 0.12))
    nt.links.new(L2.o(tint, "Result"), bsdf.inputs["Emission Color"])
    mods = {}
    for o in objs:
        key = o.name.replace("Fissure_", "").split(".")[0]
        mods[key] = o
        if o.data.color_attributes:
            vc.layer_name = o.data.color_attributes[0].name
        o.data.materials.clear()
        if o.data.shape_keys:
            for kb in o.data.shape_keys.key_blocks:
                kb.value = 0.0
    T = Matrix.Translation((start[0], start[1], FLOOR)) @ Matrix.Rotation(math.radians(heading_deg), 4, "Z") \
        @ Matrix.Diagonal((scale, scale, scale, 1.0))
    path = []
    for name in seq:
        src = mods[name]
        ob = bpy.data.objects.new("Fissure_" + name, src.data)
        collection.objects.link(ob)
        ob.matrix_world = T @ src.matrix_world
        (ux, uz), yaw = FISSURE_MODULES[name]
        off = Vector((ux, -uz, 0.0))
        path.append(tuple((T @ Vector((0, 0, 0)))[:2]))
        path.append(tuple((T @ (off * 0.5))[:2]))
        T = T @ Matrix.Translation(off) @ Matrix.Rotation(math.radians(yaw), 4, "Z")
    path.append(tuple((T @ Vector((0, 0, 0)))[:2]))
    # Material slots: the FBX gives two (rock, punch); the data is shared, so set them once.
    for src in mods.values():
        names = [s for s in ("Gravemaw_Fissure_Mat", "Gravemaw_Fissure_Punch")]
        src.data.materials.clear()
        for n in names:
            src.data.materials.append(fissure_material(n, rock))
        bpy.data.objects.remove(src)
    return path, scale


def seg_dist(px, py, path):
    best = np.full(px.shape, 1e9, np.float32)
    for (ax, ay), (bx, by) in zip(path[:-1], path[1:]):
        dx, dy = bx - ax, by - ay
        L2_ = max(dx * dx + dy * dy, 1e-9)
        t = np.clip(((px - ax) * dx + (py - ay) * dy) / L2_, 0, 1)
        best = np.minimum(best, np.hypot(px - (ax + t * dx), py - (ay + t * dy)))
    return best


def terrain(cfg, path, fscale, collection):
    """A plain (no canyon walls) that rises gently to a far plateau edge; flat round the fissure so its lips and skirt
    sit on it, carved away under the gap so the module walls show."""
    nx, ny = 320, 330
    xs = np.linspace(-60, 60, nx, dtype=np.float32)
    # Denser rows near the camera, where the crack is big.
    ys = (-4 + 104 * np.linspace(0, 1, ny) ** 1.6).astype(np.float32)
    X, Y_ = np.meshgrid(xs, ys)
    seed = cfg.get("seed", 3)
    fb = np.vectorize(lambda x, y: mnoise.fractal(Vector((x * 0.16 + seed, y * 0.16, 0.0)), 0.55, 2.1, 6))
    Z = np.full(X.shape, FLOOR, np.float32) + fb(X, Y_).astype(np.float32) * cfg.get("bumps", 0.3)
    rise = cfg.get("plateau", (70.0, 95.0, 3.0))
    Z += rise[2] * np.clip((Y_ - rise[0]) / (rise[1] - rise[0]), 0, 1) ** 2
    d = seg_dist(X, Y_, path)
    flat = np.clip((d - FULL_HALF * fscale * 1.1) / (FULL_HALF * fscale * 1.5), 0, 1)
    Z = FLOOR + (Z - FLOOR) * flat
    Z = np.where(d < GAP_HALF * fscale * 0.85, FLOOR - 3.0, Z)
    glow = np.exp(-(d / (fscale * cfg.get("glow_reach", 1.6))) ** 2).astype(np.float32)
    bm = bmesh.new()
    verts = [bm.verts.new((float(X[j, i]), float(Y_[j, i]), float(Z[j, i]))) for j in range(ny) for i in range(nx)]
    for j in range(ny - 1):
        for i in range(nx - 1):
            a = j * nx + i
            bm.faces.new((verts[a], verts[a + 1], verts[a + nx + 1], verts[a + nx]))
    me = bpy.data.meshes.new("Terrain")
    bm.to_mesh(me)
    bm.free()
    me.attributes.new("Glow", "FLOAT", "POINT").data.foreach_set("value", glow.ravel())
    me.shade_smooth()
    ob = bpy.data.objects.new("Terrain", me)
    collection.objects.link(ob)
    me.materials.append(mat_ground("Ground", cfg.get("rock", (0.028, 0.03, 0.045)), cfg.get("vein_glow", 2.6)))
    return ob


def mat_ground(name, base, glow_strength):
    """Crystal Plains crust at night: blue-grey basalt with broken slab seams; near the crack the seams carry the
    lava's light (game EMBER), fading out and breaking up with distance instead of a uniform glowing band."""
    mat, nt, out = L2.new_mat(name)
    co = L2.objco(nt)
    n1 = L2.noise(nt, co, 0.6, 10.0, 0.62)
    n2 = L2.noise(nt, co, 7.0, 8.0, 0.6)
    col = L2.MIXC(nt, L2.o(n1, "Factor"), tuple(c * 0.55 for c in base), tuple(c * 1.6 for c in base))
    seams = L2.crack_lines(nt, co, 0.9, 0.022, warp=0.35)
    attr = L2.o(L2.N(nt, "ShaderNodeAttribute", attribute_name="Glow", attribute_type="GEOMETRY"), "Factor")
    breakup = L2.MR(nt, L2.o(L2.noise(nt, co, 0.8, 3.0), "Factor"), 0.4, 0.62, 0.15, 1.0)
    near = L2.MR(nt, attr, 0.12, 0.85, 0.0, 1.0)
    veins = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", seams, near), breakup)
    col = L2.MIXC(nt, L2.M(nt, "MULTIPLY", seams, 0.7), col, tuple(c * 0.25 for c in base))
    h = L2.M(nt, "ADD", L2.o(n1, "Factor"), L2.M(nt, "MULTIPLY", L2.o(n2, "Factor"), 0.3))
    nrm = L2.bump(nt, L2.M(nt, "SUBTRACT", h, L2.M(nt, "MULTIPLY", seams, 0.5)), 0.6, 0.2)
    flush = L2.M(nt, "MULTIPLY", L2.M(nt, "POWER", attr, 4.0), 0.06)
    e = L2.M(nt, "MULTIPLY", L2.M(nt, "ADD", L2.M(nt, "POWER", veins, 1.5), flush), glow_strength)
    L2.principled(nt, out, base=col, metal=0.0, rough=0.84, normal=nrm, emit=EMBER, emit_s=e, spec=0.4)
    return mat


def height_at(path, fscale, x, y, cfg):
    """Approximate terrain height (no noise) for placing props."""
    rise = cfg.get("plateau", (70.0, 95.0, 3.0))
    return FLOOR + rise[2] * min(max((y - rise[0]) / (rise[1] - rise[0]), 0), 1) ** 2


def lava_lights(path, collection, strength, every=3):
    """Light from the melt for the ground, crystals and creatures around it. The fissure itself is excluded: lit by
    a lamp a metre above it, its molten albedo washes out to pale planks; it carries its own emission instead."""
    skip = bpy.data.collections.new("LavaExclude")
    for ob in collection.objects:
        if ob.name.startswith("Fissure_"):
            skip.objects.link(ob)
    for k, (x, y) in enumerate(path[::every]):
        lamp = L2.point(f"Lava{k}", (x, y, FLOOR - 0.45), EMBER, strength * (1 + y * 0.03), 0.4, collection)
        lamp.light_linking.receiver_collection = skip
        for item in skip.collection_objects:
            item.light_linking.link_state = "EXCLUDE"


# =============================================================================================================
# Title
# =============================================================================================================

FAULT = dict(y0=0.50, slope=-0.012, gap=0.05, shift=(0.05, 0.05, 0.04))


def mat_face(name, base, glow, fault_strength):
    """Colony gunmetal, not chrome: brushed, a value gradient over the cap height, two engraved panel seams, grime
    in patches and light leaking from fine cracks near the fault."""
    mat, nt, out = L2.new_mat(name)
    co = L2.objco(nt)
    _, y, _ = L2.SEP(nt, co)
    streak = L2.noise(nt, co, 3.0, 6.0, 0.6, stretch=(2.0, 90.0, 2.0))
    rough = L2.MR(nt, L2.o(streak, "Factor"), 0.3, 0.7, 0.30, 0.46, smooth=False)
    g = L2.noise(nt, co, 2.2, 8.0, 0.62)
    col = L2.MIXC(nt, L2.MR(nt, L2.o(g, "Factor"), 0.45, 0.72, 0.0, 0.5), base, tuple(c * 0.4 for c in base))
    grad = L2.MR(nt, y, 0.0, 1.0, 0.42, 1.12)
    cm = L2.N(nt, "ShaderNodeCombineXYZ")
    for i in range(3):
        nt.links.new(grad, cm.inputs[i])
    mixn = L2.N(nt, "ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
    L2.setv(nt, mixn, "Factor", 1.0)
    L2.setv(nt, mixn, "A", col)
    nt.links.new(L2.o(cm), L2.sock(mixn.inputs, "B"))
    col = L2.o(mixn, "Result")
    seam = L2.M(nt, "MAXIMUM", L2.MR(nt, L2.M(nt, "ABSOLUTE", L2.M(nt, "SUBTRACT", y, 0.2)), 0.0, 0.009, 1.0, 0.0),
                L2.MR(nt, L2.M(nt, "ABSOLUTE", L2.M(nt, "SUBTRACT", y, 0.81)), 0.0, 0.009, 1.0, 0.0))
    col = L2.MIXC(nt, L2.M(nt, "MULTIPLY", seam, 0.85), col, (0.01, 0.01, 0.012))
    height = L2.M(nt, "SUBTRACT", L2.M(nt, "MULTIPLY", L2.o(streak, "Factor"), 0.25), L2.M(nt, "MULTIPLY", seam, 0.5))
    mask = L2.vein_mask(nt, "fault", (FAULT["y0"], FAULT["slope"], 0.36))
    lines = L2.crack_lines(nt, co, 3.2, 0.016, seed_offset=(3.1, 7.7, 1.3))
    breakup = L2.MR(nt, L2.o(L2.noise(nt, co, 1.4, 3.0), "Factor"), 0.42, 0.58, 0.0, 1.0)
    dens = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", lines, mask), breakup)
    col = L2.MIXC(nt, L2.M(nt, "MINIMUM", L2.M(nt, "MULTIPLY", dens, 1.6), 1.0), col, (0.01, 0.006, 0.008))
    height = L2.M(nt, "SUBTRACT", height, dens)
    nrm = L2.bump(nt, height, 0.12, 0.01, L2.bevel_normal(nt, 0.012))
    L2.principled(nt, out, base=col, metal=1.0, rough=rough, aniso=0.3, normal=nrm, emit=glow,
                  emit_s=L2.M(nt, "MULTIPLY", dens, fault_strength))
    return mat


def mat_fault_core(name, colour, hot, strength):
    """Light inside the break: varies along the fault and never reaches white."""
    mat, nt, out = L2.new_mat(name)
    co = L2.objco(nt)
    n = L2.MR(nt, L2.o(L2.noise(nt, co, 4.0, 3.0, 0.5), "Factor"), 0.35, 0.65, 0.0, 1.0)
    col = L2.MIXC(nt, n, colour, hot)
    s = L2.MR(nt, n, 0.0, 1.0, strength * 0.55, strength)
    em = L2.N(nt, "ShaderNodeEmission", {"Color": col, "Strength": s})
    nt.links.new(L2.o(em), out.inputs["Surface"])
    return mat


def title(name, cfg, seed=4):
    rng = random.Random(seed)
    polys, outlines, width = L2.custom_word("DEEPWAKE")
    ext, bev = 0.13, 0.04
    front = ext + bev
    glow, hot = cfg["fault_col"], cfg["fault_hot"]
    mats = [mat_face(name + "Face", (0.26, 0.275, 0.31), glow, cfg.get("face_glow", 3.0)),
            L2.mat_polished(name + "Edge", (0.80, 0.82, 0.86), rough=0.2, wear=0.5),
            L2.mat_side(name + "Side", (0.035, 0.04, 0.05)),
            L2.mat_fracture(name + "Fracture", glow, 0.8),
            L2.mat_chip(name + "Chip")]
    wordm = L2.poly_curve(name + "Word", polys, ext, bev)
    L2.assign_by_normal(wordm, mats)
    L2.chip(wordm, outlines, front, rng, 0.22, (0.025, 0.06))
    line = L2.jagged_line(-0.5, width + 0.5, FAULT["y0"], FAULT["slope"], 0.16, 0.055, rng)
    upper, lower, band = L2.split_along(wordm, line, FAULT["gap"], name=name)
    band.data.materials.clear()
    band.data.materials.append(mat_fault_core(name + "Gap", glow, hot, cfg.get("gap_strength", 3.2)))
    for p in band.data.polygons:
        p.material_index = 0
    band.data.transform(Matrix.Translation((0, 0, -0.025)))
    upper.data.transform(Matrix.Translation(FAULT["shift"]))
    # Crysium breaking out of the fault: the game's fluted shards, a few large enough to read as crystal.
    b = C.PartBuilder()
    pal = cfg["fault_palettes"]
    spots = [(0.95, 0.5, 0.30), (2.55, 0.42, 0.26), (4.05, 0.48, 0.34), (5.45, 0.4, 0.22), (6.75, 0.46, 0.28)]
    for i, (x, lean, length) in enumerate(spots):
        y = FAULT["y0"] + FAULT["slope"] * x
        side = -1 if i % 2 else 1
        b.shard("Fault", Vector((x, y, front * 0.3)), Vector((side * 0.35, lean, 1.0)), 0.045, length, sides=6,
                twist=1.6, flute=0.45, rings=10, style=Y.shard_style(pal[i % len(pal)], 1.0, seed=70 + i))
    for i in range(9):
        x = rng.uniform(0.3, width - 0.3)
        y = FAULT["y0"] + FAULT["slope"] * x
        b.shard("Chip", Vector((x, y, front * 0.35)), Vector((rng.uniform(-0.7, 0.7), rng.uniform(-0.3, 0.8), 1.0)),
                rng.uniform(0.018, 0.03), rng.uniform(0.07, 0.14), sides=6, twist=1.2, flute=0.35, rings=6,
                style=Y.shard_style(pal[(i + 1) % len(pal)], 1.0, seed=90 + i))
    shards = fused(b, name + "Shards", 0.0035, 1, strength=cfg.get("shard_strength", 3.0))
    obs = [upper, lower, band, shards]
    for ob in obs:
        ob.data.transform(Matrix.Translation((-width / 2, 0, 0)))
    return obs, width, 1.0


def series_line(tw, th, col):
    font = os.path.join(FONTS, "michroma_Michroma-Regular.ttf")
    t = L2.text_mesh("Series", SERIES_LINE, font, 0.30, 0.03, 0.008, spacing=1.7)
    bb = [v.co for v in t.data.vertices]
    x0, x1 = min(v.x for v in bb), max(v.x for v in bb)
    y0 = min(v.y for v in bb)
    t.data.transform(Matrix.Translation((-(x0 + x1) / 2, -y0, 0)))
    mats = [L2.mat_metal_face("SerFace", (0.80, 0.82, 0.86), rough=0.24, grime=0.1),
            L2.mat_polished("SerEdge", (0.95, 0.96, 0.98)), L2.mat_side("SerSide", (0.05, 0.06, 0.08))]
    L2.assign_by_normal(t, mats)
    w = x1 - x0
    rules = []
    for side in (-1, 1):
        a = side * (w / 2 + 0.2)
        b = side * (tw / 2)
        lo, hi = sorted((a, b))
        # Taper towards the outer end: a colony HUD rule, not a plain bar.
        inner, outer = (hi, lo) if side < 0 else (lo, hi)
        poly = [(inner, 0.125), (outer - side * 0.05, 0.135), (outer, 0.145), (outer - side * 0.05, 0.155),
                (inner, 0.165)]
        if side < 0:
            poly = list(reversed(poly))
        r = L2.poly_curve(f"SerRule{side}", [poly], 0.01, 0.0)
        r.data.materials.append(L2.mat_emit("SerRuleM", COLONY_BLUE, 4.0, core=COLONY))
        rules.append(r)
    obs = [t] + rules
    for ob in obs:
        L2.put(ob, col)
    L2.stand(obs, (0, 0.02, th + 0.22))
    return obs


# =============================================================================================================
# Emblem: the colony hex, broken by a Crysium shard
# =============================================================================================================

def emblem(name, cfg, seed=8):
    rng = random.Random(seed)
    glow, hot = cfg["fault_col"], cfg["fault_hot"]
    ring_mats = [L2.mat_metal_face(name + "RingFace", (0.62, 0.64, 0.69), rough=0.3, grime=0.35,
                                   gradient=(-1.0, 1.0, 0.55, 1.1)),
                 L2.mat_polished(name + "RingEdge", (0.92, 0.93, 0.96), rough=0.16, wear=0.35),
                 L2.mat_side(name + "RingSide", (0.04, 0.05, 0.07), seam=(COLONY_BLUE, 6.0, 0.09, 0.022)),
                 L2.mat_fracture(name + "RingFrac", glow, 0.8), L2.mat_chip(name + "RingChip")]
    core_mats = [L2.mat_metal_face(name + "CoreFace", (0.09, 0.095, 0.11), rough=0.36, grime=0.3,
                                   veins=dict(color=glow, strength=3.0, scale=2.6, width=0.02,
                                              mask=("radial", (0, 0, 0.2, 0.75)), breakup=1.1)),
                 L2.mat_polished(name + "CoreEdge", (0.55, 0.57, 0.62), rough=0.22, wear=0.4),
                 L2.mat_side(name + "CoreSide", (0.03, 0.03, 0.04)),
                 L2.mat_fracture(name + "CoreFrac", glow, 1.2), L2.mat_chip(name + "CoreChip")]
    outer = L2.chamfer_poly(L2.hex_pts(1.0), 0.09)
    inner = L2.chamfer_poly(L2.hex_pts(0.72), 0.05)
    ring = L2.poly_curve(name + "Ring", [outer, inner], 0.16, 0.045)
    L2.assign_by_normal(ring, ring_mats)
    L2.chip(ring, [outer], 0.205, rng, 0.35, (0.035, 0.07))
    core = L2.poly_curve(name + "Core", [L2.chamfer_poly(L2.hex_pts(0.6), 0.04)], 0.1, 0.035, z0=-0.03)
    L2.assign_by_normal(core, core_mats)
    plate = L2.join([ring, core], name + "Plate")
    ang = math.radians(58)
    d = Vector((math.cos(ang), math.sin(ang)))
    line = []
    for k in range(15):
        t = -1.4 + 2.8 * k / 14
        jag = rng.uniform(-0.06, 0.06) if 0 < k < 14 else 0
        line.append((d.x * t - d.y * jag, d.y * t + d.x * jag))
    R = Matrix.Rotation(-ang, 4, "Z")
    plate.data.transform(R)
    hline = [((R @ Vector((x, y, 0))).x, (R @ Vector((x, y, 0))).y) for x, y in line]
    upper, lower, band = L2.split_along(plate, hline, 0.1, name=name)
    L2.remove(band)
    upper.data.transform(Matrix.Translation((0.0, 0.09, 0.03)))
    lower.data.transform(Matrix.Translation((0.0, -0.05, 0.0)))
    Ri = R.inverted()
    for ob in (upper, lower):
        ob.data.transform(Ri)
    # One big shard through the break, breaking out of the frame top right; two small ones splayed beside it.
    b = C.PartBuilder()
    pal = cfg["emblem_palettes"]
    b.shard("Main", Vector((-0.34, -0.5, 0.02)), Vector((0.5, 1.0, 0.32)), 0.2, 1.62, sides=6, twist=1.4, flute=0.5,
            rings=14, style=Y.shard_style(pal[0], 1.1, nodes=(0.3, 0.55, 0.78), seed=11))
    b.shard("Side", Vector((-0.1, -0.32, 0.02)), Vector((1.0, 0.25, 0.4)), 0.085, 0.62, sides=6, twist=1.6, flute=0.4,
            rings=10, style=Y.shard_style(pal[1 % len(pal)], 1.0, seed=12))
    b.shard("Side2", Vector((-0.38, -0.3, 0.02)), Vector((-0.55, 1.0, 0.45)), 0.075, 0.5, sides=6, twist=1.3,
            flute=0.4, rings=10, style=Y.shard_style(pal[0], 1.0, seed=13))
    crystal = fused(b, name + "Crystal", 0.008, 1, strength=cfg.get("emblem_shard_strength", 3.2))
    gl = L2.poly_curve(name + "Glow", [L2.hex_pts(0.62)], 0.01, 0.0, z0=-0.06)
    gl.data.materials.append(L2.mat_emit(name + "GlowM", glow, 1.6, core=hot))
    return [upper, lower, crystal, gl], 2.0, 2.0


# =============================================================================================================
# Environment
# =============================================================================================================

def world_sky(zenith, horizon, strength=1.0):
    """Dark gradient sky; the faint horizon glow is what the title's faces reflect."""
    w = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = w
    try:
        w.use_nodes = True
    except AttributeError:
        pass
    nt = w.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    tc = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (*horizon, 1)
    ramp.color_ramp.elements[1].position = 0.35
    ramp.color_ramp.elements[1].color = (*zenith, 1)
    m = nt.nodes.new("ShaderNodeMath")
    m.operation = "ABSOLUTE"
    nt.links.new(sep.outputs[2], m.inputs[0])
    nt.links.new(m.outputs[0], ramp.inputs[0])
    bg = nt.nodes.new("ShaderNodeBackground")
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = strength
    nt.links.new(bg.outputs[0], out.inputs["Surface"])


def far_from(path, x, y, d):
    return seg_dist(np.array([x], np.float32), np.array([y], np.float32), path)[0] > d


def crystal_field(cfg, path, fscale, col, meshes):
    rng = random.Random(cfg.get("seed", 3) + 5)
    names = list(meshes)
    placed = 0
    tries = 0
    while placed < cfg.get("field_count", 40) and tries < 2000:
        tries += 1
        y = 5 + 70 * rng.random() ** 1.3
        x = rng.uniform(-6 - y * 0.75, 6 + y * 0.75)
        if not far_from(path, x, y, fscale * 1.4):
            continue
        # Keep the space right behind the title quiet near the camera.
        # Keep a quiet corridor behind the title so the crack leads the eye and the letters stay clean.
        if abs(x) < 4.5 + y * 0.3 and y < 40:
            continue
        s = rng.uniform(0.9, 1.8) * (1 + y * 0.02)
        instance_mesh(meshes[rng.choice(names)], col, (x, y, height_at(path, fscale, x, y, cfg) - 0.05),
                      rng.uniform(0, 6.28), s, (rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08)))
        placed += 1
    # Formations on the crack lips, catching the lava light.
    for k, (x, y) in enumerate(path[2:-2:cfg.get("lip_every", 3)]):
        if y < 4:
            continue
        side = 1 if k % 2 else -1
        ox = side * fscale * rng.uniform(0.9, 1.4)
        instance_mesh(meshes[names[k % len(names)]], col, (x + ox, y + rng.uniform(-0.5, 0.5), FLOOR - 0.05),
                      rng.uniform(0, 6.28), rng.uniform(1.0, 1.7), (0.0, side * 0.15))
    # Framing crystals at the edges of the frame, low and dark-ish against the haze.
    for x, y, s, nm in cfg.get("frame_crystals", ()):
        instance_mesh(meshes[nm], col, (x, y, FLOOR - 0.05), rng.uniform(0, 6.28), s)


def crystal_towers(cfg, col):
    if not cfg.get("towers"):
        return
    obs = import_fbx("Env/Silhouette_CrystalTower/Silhouette_CrystalTower.fbx",
                     "Env/Silhouette_CrystalTower/Silhouette_CrystalTower", cfg.get("tower_emit", 2.5))
    src = obs[0]
    for x, y, s, rz in cfg["towers"]:
        instance(src, col, (x, y, height_at(None, 1, x, y, cfg) - 1.0), rz, s)
    bpy.data.objects.remove(src)


def colony(cfg, col):
    spots = cfg.get("colony", ())
    if not spots:
        return
    base = import_fbx("Base/Base.fbx", "Base/Base", cfg.get("colony_emit", 6.0), keep=None)
    base = [o for o in base if "Damage" not in o.name and "Wreck" not in o.name]
    gate = import_fbx("Gate/Gate_G1.fbx", "Gate/Gate", cfg.get("colony_emit", 6.0), keep=["Gate_G1"])
    gate = [o for o in gate if "Damage" not in o.name]
    for kind, x, y, s, rz in spots:
        z = height_at(None, 1, x, y, cfg)
        parts = base if kind == "base" else gate
        for p in parts:
            instance(p, col, (x, y, z), rz, s)
        # A cold pool of light so the silhouette reads at a distance.
        L2.point(f"ColonyLight{x:.0f}", (x, y - 1.5, z + 1.5 * s), COLONY, cfg.get("colony_light", 600.0) * s, 1.5,
                 col)
    for p in base + gate:
        bpy.data.objects.remove(p)


def mat_beam(name, colour, strength):
    """Additive light shaft: brightest along the axis, fading towards the rim and the far end."""
    mat, nt, out = L2.new_mat(name)
    lw = L2.N(nt, "ShaderNodeLayerWeight", {"Blend": 0.5})
    facing = L2.M(nt, "POWER", L2.o(lw, "Facing"), 2.0)
    tc = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "Generated")
    _, _, z = L2.SEP(nt, tc)
    along = L2.MR(nt, z, 0.0, 1.0, 1.0, 0.2)
    s = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", facing, along), strength)
    em = L2.N(nt, "ShaderNodeEmission", {"Color": colour, "Strength": s})
    tr = L2.N(nt, "ShaderNodeBsdfTransparent")
    add = L2.N(nt, "ShaderNodeAddShader")
    nt.links.new(L2.o(em), add.inputs[0])
    nt.links.new(L2.o(tr), add.inputs[1])
    nt.links.new(add.outputs[0], out.inputs["Surface"])
    return mat


def drone(cfg, col):
    spec = cfg.get("drone")
    if not spec:
        return
    x, y, z, s, target = spec
    obs = import_fbx("Drone_Repair/Drone_Repair.fbx", "Drone_Repair/Drone_Repair", 5.0)
    d = obs[0]
    d.location = (x, y, z)
    d.scale = (s,) * 3
    d.rotation_euler = (math.radians(10), 0, math.radians(25))
    L2.put(d, col)
    # The beam: a cone from the drone's belly to the ground target.
    tip = Vector((x, y, z - 0.15 * s))
    tgt = Vector(target)
    length = (tgt - tip).length
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.04 * s, radius2=0.55, depth=length, end_fill_type="NOTHING")
    cone = bpy.context.active_object
    cone.name = "DroneBeam"
    # Cone is built along +Z centred; radius1 at -Z. Point -Z at the drone, +Z at the target.
    dirv = (tgt - tip).normalized()
    cone.rotation_euler = dirv.to_track_quat("Z", "Y").to_euler()
    cone.location = tip + dirv * length * 0.5
    cone.data.materials.append(mat_beam("BeamM", COLONY, cfg.get("beam", 0.28)))
    cone.visible_shadow = False
    L2.put(cone, col)
    ld = bpy.data.lights.new("DroneSpot", "SPOT")
    ld.energy = cfg.get("beam_light", 900.0)
    ld.color = COLONY
    ld.spot_size = math.radians(24)
    ld.spot_blend = 0.6
    ld.shadow_soft_size = 0.05
    sp = bpy.data.objects.new("DroneSpot", ld)
    col.objects.link(sp)
    sp.location = tip
    C.look_at(sp, tgt)
    L2.point("DroneGlow", (x, y, z), COLONY, 15.0, 0.2, col)


def aliens(cfg, path, fscale, col):
    rng = random.Random(cfg.get("seed", 3) + 9)
    sk = cfg.get("skitters", ())
    if sk:
        obs = import_fbx("Skitter/Skitter.fbx", "Skitter/Skitter", 3.0)
        src = obs[0]
        for x, y, s, rz in sk:
            instance(src, col, (x, y, FLOOR + 0.02), rz, s)
        bpy.data.objects.remove(src)
    # Eyes in the dark: pairs of small hot points, low over the ground at the frame edges.
    bm = bmesh.new()
    for x, y, z, s in cfg.get("eyes", ()):
        for dx in (-1, 1):
            res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.045 * s)
            for v in res["verts"]:
                v.co = Vector((v.co.x * 1.4, v.co.y, v.co.z * 0.6)) + Vector((x + dx * 0.075 * s, y, z))
        # A second, smaller pair a little behind: insects, not mammals.
        if rng.random() < 0.6:
            for dx in (-1, 1):
                res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.02 * s)
                for v in res["verts"]:
                    v.co = v.co + Vector((x + dx * 0.05 * s, y + 0.02, z + 0.06 * s))
    if bm.verts:
        ob = L2.bm_object("AlienEyes", bm, [L2.mat_emit("EyeM", ALIEN_EYE, cfg.get("eye_strength", 30.0),
                                                       core=lin(1.0, 0.7, 0.3))])
        ob.visible_shadow = False
        L2.put(ob, col)
    else:
        bm.free()


def ground_veins(cfg, col, path, fscale):
    spots = cfg.get("veins", ())
    if not spots:
        return
    obs = import_fbx("Env/CrystalVein/CrystalVein.fbx", "Env/CrystalVein/CrystalVein", 3.0)
    src = obs[0]
    for x, y, s, rz in spots:
        instance(src, col, (x, y, height_at(path, fscale, x, y, cfg) + 0.01), rz, s)
    bpy.data.objects.remove(src)


def mat_haze(name, colour, density, low_z, high_z):
    mat, nt, out = L2.new_mat(name)
    tc = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "Object")
    _, _, z = L2.SEP(nt, tc)
    patch = L2.MR(nt, L2.o(L2.noise(nt, tc, 2.5, 3.0, 0.5), "Factor"), 0.35, 0.65, 0.4, 1.3)
    height = L2.MR(nt, z, low_z, high_z, 1.0, 0.05)
    dens = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", patch, height), density)
    vol = L2.N(nt, "ShaderNodeVolumePrincipled", {"Color": colour, "Density": dens, "Anisotropy": 0.25})
    nt.links.new(L2.o(vol), out.inputs["Volume"])
    return mat


def haze(cfg, col):
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = "Haze"
    ob.scale = (130, 110, 14)
    ob.location = (0, 51, 4.5)
    # Object z runs -0.5..0.5 over the box: dense at the ground, thin above the title.
    ground = (FLOOR - 4.5) / 14
    ob.data.materials.append(mat_haze("Haze", cfg.get("haze_col", MIST_PLAINS), cfg.get("haze", 0.012),
                                      ground, ground + cfg.get("haze_height", 0.3)))
    L2.put(ob, col)


def build_env(cfg, col):
    world_sky(cfg.get("zenith", (0.0008, 0.0011, 0.0024)), cfg.get("horizon", (0.004, 0.003, 0.008)))
    path, fs = fissure_chain(col, cfg["crack_start"], 180.0, cfg["crack_scale"], cfg["crack_seq"],
                             cfg.get("lava_emit", 3.5))
    terrain(cfg, path, fs, col)
    lava_lights(path, col, cfg.get("lava_light", 45.0))
    meshes = field_crystal_meshes()
    crystal_field(cfg, path, fs, col, meshes)
    for k, (x, y, e) in enumerate(cfg.get("crystal_lights", ())):
        L2.point(f"CrysLight{k}", (x, y, FLOOR + 1.2), CRYS_PINK, e, 1.5, col)
    crystal_towers(cfg, col)
    colony(cfg, col)
    drone(cfg, col)
    aliens(cfg, path, fs, col)
    ground_veins(cfg, col, path, fs)
    haze(cfg, col)
    L2.embers(cfg, col, ((-7, 7), (1.5, 30), (FLOOR, 5)), cfg.get("embers", 110), size=(0.005, 0.014), colour=EMBER,
              strength=5.0, seed=cfg.get("seed", 3))
    L2.embers(cfg, col, ((-9, 9), (3, 25), (FLOOR, 4)), cfg.get("motes", 40), size=(0.005, 0.011), colour=CRYS_PINK,
              strength=4.0, stretch=1.0, seed=cfg.get("seed", 3) + 1, name="Motes")
    return path, fs


# =============================================================================================================
# Variants
# =============================================================================================================

SEQ_FIELD = ["Start", "Straight_A", "Curve_L", "Straight_B", "Curve_R", "Curve_R", "Straight_C", "Curve_L",
             "Straight_B", "Straight_A", "Curve_L", "Curve_R", "Straight_B", "Straight_C", "Curve_R", "Head"]

PINK_FAULT = dict(fault_col=CRYS_PINK, fault_hot=lin(1.0, 0.75, 0.92), fault_palettes=("pink", "ice", "pink"),
                  emblem_palettes=("pink", "ice"))
EMBER_FAULT = dict(fault_col=EMBER, fault_hot=EMBER_HOT, fault_palettes=("ember", "ember", "pink"),
                   emblem_palettes=("ember", "pink"))

VARIANTS = {
    # A: the Crysium field receding into the Crystal Plains mist; colony lights far right; a drone over the field.
    "field": dict(PINK_FAULT, label="CrysiumField", series=True, seed=3,
                  crack_start=(0.4, 1.4), crack_scale=1.9, crack_seq=SEQ_FIELD,
                  field_count=30, frame_crystals=[(-10.5, 6.5, 2.6, "Crystal_A"), (11.0, 8.0, 2.9, "Crystal_C"),
                                                  (-14.0, 11.0, 3.2, "Crystal_B")],
                  crystal_lights=[(-9.0, 6.0, 120.0), (10.5, 7.5, 120.0)],
                  towers=[(-26, 78, 1.4, 0.3), (-15, 88, 1.1, 2.0), (21, 84, 1.3, 1.0), (33, 74, 1.0, 4.0)],
                  colony=[("base", 40, 82, 1.6, -0.5), ("gate", 30, 80, 1.4, -0.3)], colony_light=500.0,
                  drone=(6.8, 10.0, 3.6, 1.3, (5.4, 12.5, FLOOR)),
                  skitters=[(-2.4, 13.5, 1.6, 0.4), (3.6, 18.0, 1.5, -0.6)],
                  eyes=[(-8.5, 5.0, FLOOR + 0.35, 1.2), (-6.8, 7.5, FLOOR + 0.3, 1.0), (8.0, 6.0, FLOOR + 0.4, 1.2)],
                  veins=[(-5.0, 7.0, 2.0, 0.4), (6.5, 9.0, 2.0, 2.2), (-12.0, 16.0, 2.5, 1.0), (9.0, 20.0, 2.4, 2.6)],
                  haze=0.012, haze_col=MIST_PLAINS),
    # B: a dark plateau; the colony's base and gates on the horizon; few crystals; ember Crysium in the fault (closest
    # to the v2 lockup Remco picked).
    "colony": dict(EMBER_FAULT, label="ColonyPlateau", series=True, seed=11,
                   crack_start=(0.2, 1.4), crack_scale=1.9, crack_seq=SEQ_FIELD,
                   field_count=14, plateau=(40.0, 60.0, 2.2), lip_every=4,
                   frame_crystals=[(-10.0, 6.0, 2.4, "Crystal_B"), (10.5, 7.0, 2.6, "Crystal_A")],
                   crystal_lights=[(-9.5, 6.0, 90.0), (10.0, 7.0, 90.0)],
                   towers=[(-34, 80, 1.2, 0.3)],
                   colony=[("base", 30, 64, 1.8, -0.5), ("gate", 21, 61, 1.6, -0.3), ("gate", 38, 62, 1.6, -0.7),
                           ("base", -34, 68, 1.5, 0.5), ("gate", -24, 63, 1.5, 0.3)], colony_light=900.0,
                   drone=(-6.5, 11.0, 3.4, 1.3, (-5.0, 14.0, FLOOR)),
                   skitters=[(-1.6, 9.5, 1.5, 0.2), (2.8, 15.0, 1.5, -0.5), (-3.4, 21.0, 1.4, 0.8)],
                   eyes=[(-7.5, 4.5, FLOOR + 0.3, 1.2), (7.5, 5.0, FLOOR + 0.35, 1.1), (9.5, 8.0, FLOOR + 0.3, 1.0)],
                   veins=[(-6.0, 8.0, 2.0, 0.8), (7.0, 11.0, 2.2, 2.0)],
                   haze=0.010, haze_col=(0.5, 0.55, 0.72), zenith=(0.0006, 0.0009, 0.0022),
                   horizon=(0.003, 0.004, 0.009)),
    # C: no series line; a wider crack with the planet's glow underneath and Crysium crowding its lips.
    "deep": dict(PINK_FAULT, label="DeepGlow", series=False, seed=21,
                 crack_start=(0.0, 1.0), crack_scale=2.8, crack_seq=SEQ_FIELD[:12] + ["Head"],
                 lava_emit=3.0, lava_light=70.0, vein_glow=2.0, glow_reach=2.2, lip_every=2,
                 field_count=24, frame_crystals=[(-9.5, 5.5, 2.8, "Crystal_C"), (10.0, 6.5, 3.0, "Crystal_B"),
                                                 (-13.0, 10.0, 3.0, "Crystal_A"), (14.0, 12.0, 3.3, "Crystal_A")],
                 crystal_lights=[(-9.0, 5.5, 140.0), (10.0, 6.5, 140.0)],
                 towers=[(-22, 70, 1.3, 1.2), (25, 75, 1.4, 0.2)],
                 skitters=[(-3.2, 11.0, 1.6, 0.5), (3.9, 14.0, 1.6, -0.7), (-4.4, 19.0, 1.5, 0.9)],
                 eyes=[(-8.0, 4.5, FLOOR + 0.35, 1.3), (-10.5, 7.5, FLOOR + 0.3, 1.1), (8.5, 5.0, FLOOR + 0.35, 1.2),
                       (11.0, 9.0, FLOOR + 0.3, 1.0)],
                 haze=0.013, haze_col=(0.62, 0.5, 0.7), horizon=(0.006, 0.003, 0.007)),
}

GRADE = dict(bloom=0.4, bloom_size=0.75, bloom_threshold=1.2, fog=0.16, fog_threshold=3.0,
             lift=(0.99, 1.0, 1.03), gamma=(1.0, 1.0, 1.02), gain=(1.03, 1.0, 0.98), dispersion=0.008)


# =============================================================================================================
# Scene assembly
# =============================================================================================================

def studio_lights(col, title_w, receivers, cfg):
    """Lights for the lettering only (light linking): cool rims from behind, a soft key, lava from below and a pink
    kiss from the crystal field, so the faces carry the scene's colours instead of a studio chrome band."""
    L2.area("Rim", (-title_w * 0.4, 7.0, 5.5), (0, 0, 0.5), (0.55, 0.72, 1.0), 2200, (6, 1.5), col, 30)
    L2.area("RimR", (title_w * 0.55, 6.0, 3.0), (0, 0, 0.5), (0.55, 0.72, 1.0), 1000, (3, 1.5), col, 30)
    L2.area("Key", (title_w * 0.25, -9.0, 6.0), (0, 0, 0.4), (0.85, 0.9, 1.0), 340, (8, 3), col, 35)
    L2.area("Under", (0, -1.5, -2.2), (0, 0, 0.6), EMBER, 420, (title_w, 0.6), col, 50)
    L2.area("Crys", (-title_w * 0.6, 2.0, -1.0), (0, 0, 0.5), CRYS_PINK, 380, (2, 2), col, 60)
    L2.area("CrysR", (title_w * 0.65, 2.5, -0.8), (0, 0, 0.5), CRYS_PINK if cfg["fault_col"] == CRYS_PINK else EMBER,
            300, (2, 2), col, 60)
    for ob in col.objects:
        if ob.type == "LIGHT":
            ob.light_linking.receiver_collection = receivers
    L2.reflector(col, 0.45)


def sky_light(col, col_env, energy=0.6):
    ld = bpy.data.lights.new("SkyRim", "SUN")
    ld.color = (0.45, 0.6, 1.0)
    ld.energy = energy
    ld.angle = math.radians(3)
    ob = bpy.data.objects.new("SkyRim", ld)
    col.objects.link(ob)
    ob.rotation_euler = (math.radians(-68), 0, math.radians(160))
    ob.light_linking.receiver_collection = col_env


def build(key, want_env=True):
    C.reset_scene()
    cfg = VARIANTS[key]
    col_title, col_emb, col_env, col_lit = L2.coll("Title"), L2.coll("Emblem"), L2.coll("Env"), L2.coll("Lights")
    tobs, tw, th = title("T", cfg)
    L2.place(tobs, col_title, (0, 0, 0), 1.0)
    eobs, _, _ = emblem("E", cfg)
    L2.place(eobs, col_emb, (0, 0.05, 0), 1.0)
    (x0, _, z0), (x1, _, z1) = L2.world_bbox(eobs)
    e_scale = (th * 1.32) / max(z1 - z0, (x1 - x0) * 0.8)
    for ob in eobs:
        ob.scale = (e_scale,) * 3
    (x0, _, z0), (x1, _, z1) = L2.world_bbox(eobs)
    dx = -(tw / 2) - th * 0.35 - x1
    dz = th * 0.5 - (z0 + z1) / 2
    for ob in eobs:
        ob.location.x += dx
        ob.location.z += dz
    (x0, _, z0), (x1, _, z1) = L2.world_bbox(eobs)
    if cfg["series"]:
        series_line(tw, th, col_title)
    if want_env:
        build_env(cfg, col_env)
    receivers = bpy.data.collections.new("StudioReceivers")
    receivers.children.link(col_title)
    receivers.children.link(col_emb)
    studio_lights(col_lit, tw, receivers, cfg)
    sky_light(col_lit, col_env)
    # Cycles shows area lights to the camera: in v2 the rim/under/kiss panels drew the cloud round the alpha logo.
    for ob in bpy.data.objects:
        if ob.type == "LIGHT":
            ob.visible_camera = False
    return dict(cfg=cfg, tw=tw, th=th, emb_x=(x0 + x1) / 2, emb_z=(z0 + z1) / 2, emb_w=x1 - x0, emb_h=z1 - z0,
                cols=(col_title, col_emb, col_env, col_lit))


# =============================================================================================================
# Output
# =============================================================================================================

def finish_alpha(arr, w, h, path, grade):
    """Linear premultiplied RGBA -> a tight bloom; only glow that is strong and close becomes alpha, so the logo sits
    on any background without a grey cloud round it."""
    rgb, a = arr[..., :3], arr[..., 3:4]
    bright = np.clip(rgb - grade.get("bloom_threshold", 1.0), 0, None)
    glow = np.zeros_like(rgb)
    sc = arr.shape[0] / 720.0
    for radius, weight in ((2.0, 0.45), (6.0, 0.3), (16.0, 0.14)):
        for c in range(3):
            glow[..., c] += C._blur(bright[..., c], radius * sc) * weight
    glow *= grade.get("bloom", 0.5)
    rgb = rgb + glow
    ga = np.clip((glow.max(axis=2, keepdims=True) - 0.04) / 0.5, 0, 0.8)
    alpha = np.clip(a + ga * (1 - a), 0, 1)
    straight = rgb / np.maximum(alpha, 1e-4)
    out = L2.box_down(np.dstack([straight, alpha]), L2.SUPER)
    img = bpy.data.images.new("alpha_out", w, h, alpha=True, float_buffer=True)
    img.pixels.foreach_set(out.astype(np.float32).ravel())
    s = bpy.context.scene
    s.render.image_settings.file_format = "PNG"
    s.render.image_settings.color_mode = "RGBA"
    s.render.image_settings.color_depth = "8"
    img.save_render(path, scene=s)
    bpy.data.images.remove(img)
    C.log("wrote", path)
    return path


L2.finish_alpha = finish_alpha


def outputs(key, only=None, samples=80, half=False, draft=False):
    want = lambda k: only is None or k in only  # noqa: E731
    st = build(key, want_env=any(want(k) for k in ("header", "capsule", "emblem")))
    cfg, tw, th = st["cfg"], st["tw"], st["th"]
    label = cfg["label"]
    f = 0.5 if half else 1.0
    L2.SUPER = 1 if (half or draft) else 2
    sfx = "_half" if half else ("_draft" if draft else "")
    title_h = th + (0.55 if cfg["series"] else 0)
    size = lambda wh: (int(wh[0] * f), int(wh[1] * f))  # noqa: E731
    if want("header"):
        L2.set_visible(st["cols"], True, True, True)
        left = st["emb_x"] - st["emb_w"] / 2
        cx = (left + tw / 2) / 2
        dist = L2.fit_distance(tw / 2 - left, 0.80)
        cam = L2.camera("CamHeader", (cx, 0, title_h * 0.5), dist, rise=0.25, fstop=2.2, shift=(0, -0.02))
        L2.render_png(cam, *size(HEADER), os.path.join(OUT, f"{label}_header_920x430{sfx}.png"), samples, GRADE)
    if want("capsule"):
        L2.set_visible(st["cols"], True, False, True)
        dist = L2.fit_distance(tw, 0.80)
        cam = L2.camera("CamCapsule", (0, 0, title_h * 0.5), dist, rise=0.25, fstop=2.2, shift=(0, -0.03))
        L2.render_png(cam, *size(CAPSULE), os.path.join(OUT, f"{label}_capsule_616x353{sfx}.png"), samples, GRADE)
    if want("library"):
        L2.set_visible(st["cols"], True, False, False)
        dist = L2.fit_distance(tw, 0.86)
        cam = L2.camera("CamLibrary", (0, 0, title_h * 0.5), dist, rise=0.2, dof=False)
        L2.render_png(cam, *size(LIBRARY), os.path.join(OUT, f"{label}_library_logo{sfx}.png"), samples, GRADE,
                      transparent=True)
    if want("emblem"):
        L2.set_visible(st["cols"], False, True, True)
        dist = L2.fit_distance(max(st["emb_w"], st["emb_h"]), 0.80, lens=50)
        cam = L2.camera("CamEmblem", (st["emb_x"], 0.05, st["emb_z"]), dist, lens=50, rise=0.1, fstop=1.6)
        keep = L2.SUPER
        L2.SUPER = 1
        n = int(1024 * f)
        big = os.path.join(OUT, f"_{label}_emblem_{n}.png")
        L2.render_png(cam, n, n, big, samples, GRADE)
        L2.SUPER = keep
        L2.downscale_file(big, os.path.join(OUT, f"{label}_emblem_512{sfx}.png"), max(1, int(2 * f)))
        L2.downscale_file(big, os.path.join(OUT, f"{label}_emblem_256{sfx}.png"), max(1, int(4 * f)))
        os.remove(big)


# =============================================================================================================
# Contact sheet
# =============================================================================================================

def contact_sheet():
    pad = 24
    keys = [k for k in VARIANTS if os.path.exists(os.path.join(OUT, f"{VARIANTS[k]['label']}_header_920x430.png"))]
    lib_w, lib_h = 640, 360
    W = pad + 920 + pad + 616 + pad + 256 + pad
    row_h = 430 + pad + lib_h
    H = pad + len(keys) * (row_h + pad)
    sheet = np.zeros((H, W, 4), np.float32)
    sheet[..., :3] = 0.03
    sheet[..., 3] = 1
    y = pad
    for k in keys:
        lab = VARIANTS[k]["label"]
        p = lambda s: os.path.join(OUT, f"{lab}_{s}")  # noqa: E731
        L2.paste(sheet, C.image_to_array(p("header_920x430.png")), pad, y)
        if os.path.exists(p("capsule_616x353.png")):
            L2.paste(sheet, C.image_to_array(p("capsule_616x353.png")), pad + 920 + pad, y)
            L2.paste(sheet, L2.box_down(C.image_to_array(p("capsule_616x353.png")), 2),
                     pad + lib_w + pad, y + 430 + pad)
        if os.path.exists(p("emblem_256.png")):
            e = C.image_to_array(p("emblem_256.png"))
            L2.paste(sheet, e, pad + 920 + pad + 616 + pad, y)
            L2.paste(sheet, L2.box_down(e, 4), pad + 920 + pad + 616 + pad, y + 256 + pad)
        if os.path.exists(p("library_logo.png")):
            lib = L2.box_down(C.image_to_array(p("library_logo.png")), 2)
            L2.paste(sheet, L2.checker(lib.shape[1], lib.shape[0]), pad, y + 430 + pad)
            L2.paste(sheet, lib, pad, y + 430 + pad)
            # The same logo on a light card: the alpha edge must hold on a bright store page too.
            x2 = pad + lib_w + pad + 308 + pad
            card = np.ones((lib_h // 2, lib_w // 2, 4), np.float32) * 0.8
            card[..., 3] = 1
            L2.paste(sheet, card, x2, y + 430 + pad)
            L2.paste(sheet, L2.box_down(lib, 2), x2, y + 430 + pad)
        y += row_h + pad
    path = os.path.join(OUT, "Deepwake_v3_contact_sheet.png")
    C.array_to_image(sheet, path)
    C.log("sheet", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    args = C.cli_args()
    if args.get("sheet"):
        contact_sheet()
        return
    L2.enable_gpu()
    only = set(args["only"].split(",")) if args.get("only") else None
    outputs(args.get("variant", "field"), only, int(args.get("samples", 80)), bool(args.get("half")),
            bool(args.get("draft")))


if __name__ == "__main__":
    main()
