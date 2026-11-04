"""Helpers for the continent environment sets (round 17: continents 2-6). Built on env_kit.py (round 8, Crystal
Plains) without changing it: the same tiling-set channel layout, the same placement frame and the same edge and
corner rules, but every continent writes into its own folder with its own asset prefix, so nothing collides with
the Crystal Plains exports.

- Tiling sets: Exports/Env/<Continent>/Tiling/<Prefix>_<Set>/<Prefix>_<Set>_{Albedo,Normal,Mask[,Emission]}.png,
  a 512 copy in Low/. Mask: R AO, G smoothness, B height 0..1 (height blending), A dust/sparkle mask.
- Meshes: Exports/Env/<Continent>/<Prefix>_<Asset>/ with FBX, the four prop maps and a report.
- Edge pieces (kerbs, banks): centred on the middle of a cell side, the road or liquid on local -Y. OuterCorner: the
  road or liquid is the (-X, -Y) cell of the four round the corner point. InnerCorner: the dry/ground cell is
  (+X, +Y). Cliffs: Straight is 2 cells along X with the field on -Y; OuterCorner has the wall in the (+X, +Y)
  quadrant (arms 1 cell along +X and +Y); InnerCorner has the field in the (-X, -Y) quadrant (arms 1 cell along -X
  and -Y). Straights continue where the arms end.

Each build_env_<continent>.py defines a Continent, its tiling functions, its looks and its decor builders, and
calls run().
"""

import json
import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

import common as C
import crysium as Y
import env_kit as E

# Extra crystal palettes (registered at import; the shared module stays untouched).
Y.PALETTES.setdefault("frost", ((np.array((0.18, 0.24, 0.34)), np.array((0.62, 0.78, 0.92)), np.array((0.92, 0.97, 1.0))),
                                (np.array((1.0, 0.62, 0.25)), np.array((0.8, 0.92, 1.0)))))
Y.PALETTES.setdefault("overcharge", ((np.array((0.35, 0.05, 0.3)), np.array((1.0, 0.4, 0.85)), np.array((1.0, 0.95, 0.98))),
                                     (np.array((1.0, 0.3, 0.8)), np.array((1.0, 0.92, 0.98)))))
Y.PALETTES.setdefault("seal", ((np.array((0.25, 0.16, 0.05)), np.array((0.95, 0.75, 0.35)), np.array((1.0, 0.96, 0.82))),
                               (np.array((1.0, 0.72, 0.3)), np.array((1.0, 0.95, 0.75)))))
Y.PALETTES.setdefault("corrupt", ((np.array((0.05, 0.02, 0.08)), np.array((0.28, 0.08, 0.42)), np.array((0.6, 0.35, 0.85))),
                                  (np.array((0.62, 0.2, 1.0)), np.array((0.85, 0.55, 1.0)))))
Y.PALETTES.setdefault("mire", ((np.array((0.04, 0.12, 0.12)), np.array((0.3, 0.75, 0.7)), np.array((0.8, 1.0, 0.92))),
                               (np.array((0.25, 1.0, 0.8)), np.array((0.75, 1.0, 0.9)))))

PLATEAU = 0.55


class Continent:
    """Where a continent's sets go and what they are called. `tiles`: set name -> world units one tile covers."""

    def __init__(self, key, prefix, tiles):
        self.key = key
        self.prefix = prefix
        self.tiles = dict(tiles)
        self.dir = os.path.join(E.ENV, key)
        self.tiling = os.path.join(self.dir, "Tiling")

    def name(self, short):
        return f"{self.prefix}_{short}"

    def activate(self):
        """Points the shared bake pipeline at this continent's folder (only for this process)."""
        C.EXPORTS = self.dir
        os.makedirs(self.dir, exist_ok=True)


# ---------------------------------------------------------------------------------------------------------------
# Tiling sets
# ---------------------------------------------------------------------------------------------------------------

def write_set(cont, short, maps, normal_strength=1.0):
    """Writes a tiling set (1024 plus a 512 Low copy); `maps` as returned by a set function (albedo/emission in sRGB
    0..1, height in world units, smooth, dust, ao)."""
    name = cont.name(short)
    out = os.path.join(cont.tiling, name)
    low = os.path.join(out, "Low")
    os.makedirs(low, exist_ok=True)
    albedo, height = maps["albedo"], maps["height"]
    n = albedo.shape[0]
    tile = cont.tiles[short]
    nrm = E.normal_from_height(height, tile, normal_strength)
    hn = (height - height.min()) / max(np.ptp(height), 1e-9)
    packs = {
        "Albedo": (np.dstack([np.clip(albedo, 0, 1), np.ones((n, n))]), "sRGB", False),
        "Normal": (np.dstack([nrm * 0.5 + 0.5, np.ones((n, n))]), "Non-Color", True),
        "Mask": (np.dstack([maps["ao"], maps["smooth"], hn, maps["dust"]]).clip(0, 1), "Non-Color", False),
    }
    if maps.get("emission") is not None:
        packs["Emission"] = (np.dstack([np.clip(maps["emission"], 0, 1), np.ones((n, n))]), "sRGB", False)
    for key, (arr, space, is_normal) in packs.items():
        C.array_to_image(arr.astype(np.float32), os.path.join(out, f"{name}_{key}.png"), colorspace=space)
        small = C.downsample(arr.astype(np.float32), 2, normal=is_normal)
        C.array_to_image(small, os.path.join(low, f"{name}_{key}.png"), colorspace=space)
    lum = (albedo * np.array((0.2126, 0.7152, 0.0722))).sum(axis=2)
    stats = {"set": name, "continent": cont.key, "size": n, "low_size": n // 2, "tile_world": tile,
             "maps": sorted(packs), "albedo_mean_srgb": [round(float(x), 3) for x in albedo.reshape(-1, 3).mean(axis=0)],
             "value_mean": round(float(lum.mean()), 3), "smoothness_mean": round(float(maps["smooth"].mean()), 3),
             "height_range_m": round(float(np.ptp(height)), 4)}
    with open(os.path.join(out, f"{name}_report.json"), "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2)
    C.log("tiling set", name, stats)
    return stats


def set_paths(cont, short):
    name = cont.name(short)
    out = os.path.join(cont.tiling, name)
    paths = {k.lower(): os.path.join(out, f"{name}_{k}.png") for k in ("Albedo", "Normal", "Mask", "Emission")}
    if not os.path.exists(paths["emission"]):
        paths.pop("emission")
    return paths


def _tex(nt, path, vec, non_color=False):
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = bpy.data.images.load(path, check_existing=True)
    if non_color:
        t.image.colorspace_settings.name = "Non-Color"
        t.image.alpha_mode = "CHANNEL_PACKED"
    nt.links.new(vec, t.inputs["Vector"])
    return t


def tiling_material(cont, short, tile_scale=3.0, emission_strength=2.0):
    paths = set_paths(cont, short)
    mat = C.new_material(cont.name(short) + "_Tiling")
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (tile_scale, tile_scale, 1)
    nt.links.new(tc.outputs["UV"], mp.inputs["Vector"])
    vec = mp.outputs["Vector"]
    alb = _tex(nt, paths["albedo"], vec)
    mask = _tex(nt, paths["mask"], vec, True)
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(mask.outputs["Color"], sep.inputs[0])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(alb.outputs["Color"], mul.inputs[6])
    nt.links.new(sep.outputs[0], mul.inputs[7])
    nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(sep.outputs[1], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(_tex(nt, paths["normal"], vec, True).outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    if "emission" in paths:
        nt.links.new(_tex(nt, paths["emission"], vec).outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def tiling_render(cont, short, out, world=(0.03, 0.03, 0.05), key_color=(0.95, 0.93, 1.0)):
    """A 3 x 3 tiling test (seams and repetition show here), lit like a level."""
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    E.level_world(world, 1.0)
    tile = cont.tiles[short]
    E.plane("Test", tile * 3, tile * 3, tiling_material(cont, short), uv_scale=1.0)
    E.level_lights(key=3.0, rim=0.8, key_color=key_color)
    cam = C.game_camera(Vector((0, 0, 0)), tile * 3 * 1.02, pitch=70, yaw=0)
    return C.render(cam, os.path.join(out, f"{cont.key.lower()}_tiling_{short.lower()}.png"), 700, 700)


def run_materials(cont, sets, out, size=1024, only=None, render=True, world=(0.03, 0.03, 0.05)):
    renders = []
    for short, fn in sets.items():
        if only and short not in only.split(","):
            continue
        write_set(cont, short, fn(size))
        if render:
            renders.append(tiling_render(cont, short, out, world))
    if len(renders) > 1:
        C.contact_sheet(renders, os.path.join(out, f"{cont.key.lower()}_sheet_tiling.png"), len(renders), 360)
    return renders


# ---------------------------------------------------------------------------------------------------------------
# Kit assets
# ---------------------------------------------------------------------------------------------------------------

def strip_colour_layers(obj):
    """Removes the bake's helper colour layers (Albedo, Emit, Rough, Fam, AO) from a game mesh before export. Unity
    imports only the first vertex-colour set of an FBX, so the flora's `Sway` must be the only one (and props carry
    none). The maps are already baked, so nothing visible changes."""
    mesh = obj.data
    for attr in [a for a in mesh.color_attributes if a.name != "Sway"]:
        mesh.color_attributes.remove(attr)
    if "Sway" in mesh.color_attributes:
        mesh.color_attributes.active_color = mesh.color_attributes["Sway"]


def finish(cont, asset, extra=None, tex=512):
    name = asset.name
    strip_colour_layers(asset.low)
    C.export_fbx(os.path.join(cont.dir, name, name + ".fbx"), [asset.low], animated=False)
    dims = asset.low.dimensions
    report = {"asset": name, "continent": cont.key, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
              "texture_size": tex, "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats,
              "detail": asset.details, "size_m": [round(dims.x, 3), round(dims.y, 3), round(dims.z, 3)]}
    if extra:
        report.update(extra)
    C.write_report(name, report)
    return report


def run_assets(cont, assets, out, only=None, render=True, blend_suffix="Kit"):
    """assets: short name -> dict(fn, tris, voxel=0.01, smoothing=2, sharp=None, families=('rock','crystal'),
    k=1.0, tex=512, sway=None (height), extra=None, emission=2.2)."""
    previous = C.EXPORTS
    cont.activate()
    try:
        return _run_assets(cont, assets, out, only, render, blend_suffix)
    finally:
        # build_all.py runs every script in one session: leave the shared export folder as it was.
        C.EXPORTS = previous


def _run_assets(cont, assets, out, only, render, blend_suffix):
    built = {}
    reports = {}
    for short, spec in assets.items():
        if only and short not in only.split(","):
            continue
        name = cont.name(short)
        k = spec.get("k", 1.0)
        tex = spec.get("tex", 512)
        asset = E.build(name, spec["fn"](), spec["tris"], voxel=spec.get("voxel", 0.01),
                        smoothing=spec.get("smoothing", 2), sharp=spec.get("sharp"),
                        families=spec.get("families", ("rock", "crystal")), emission=spec.get("emission", 2.2),
                        tex=tex, k=k, ao=0.3 * max(1.0, k / 3), extrusion=0.03 * max(1.0, k / 2),
                        ray=0.1 * max(1.0, k / 2))
        extra = dict(spec.get("extra") or {})
        if spec.get("sway"):
            E.write_sway(asset.low, spec["sway"], seed=len(built))
            extra["sway"] = {"attribute": "Sway", "height": spec["sway"],
                             "channels": "R bend weight (root 0, top 1), G phase 0..1, B flutter"}
        reports[short] = finish(cont, asset, extra, tex)
        built[short] = asset
    if built:
        C.save_blend(f"Env_{cont.key}_{blend_suffix}" + ("" if not only else "_" + only.replace(",", "_")))
    if render and built:
        asset_previews(cont, built, out, blend_suffix)
    return built, reports


def asset_previews(cont, built, out, tag):
    C.preview_world((0.02, 0.02, 0.035))
    ground = C.ground(color=(0.05, 0.05, 0.07))
    rig = C.light_rig(scale=1.5)
    paths = []
    for short, asset in built.items():
        for other in built.values():
            other.low.hide_render = other is not asset
        dims = asset.low.dimensions
        bb = asset.low.bound_box
        centre = Vector((sum(c[0] for c in bb) / 8, sum(c[1] for c in bb) / 8, sum(c[2] for c in bb) / 8))
        size = max(dims.x, dims.y, dims.z * 1.3)
        cam = C.closeup_camera(centre, size * 1.9 + 0.3, azimuth=-30, elevation=34, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"{cont.key.lower()}_{short.lower()}.png"), 560, 420))
        bpy.data.objects.remove(cam)
    if paths:
        C.contact_sheet(paths, os.path.join(out, f"{cont.key.lower()}_sheet_{tag.lower()}.png"), 4, 360)
    C.hide_lights(rig, True)
    bpy.data.objects.remove(ground)
    for asset in built.values():
        asset.low.hide_render = False


# ---------------------------------------------------------------------------------------------------------------
# Palettes and styles
# ---------------------------------------------------------------------------------------------------------------

def pal(top, side, bottom, seam):
    return dict(top=np.array(top, dtype=float), side=np.array(side, dtype=float), bottom=np.array(bottom, dtype=float),
                seam=np.array(seam, dtype=float))


def vn(p, scale, seed):
    return C.value_noise(p, scale, seed)


def ss(e0, e1, x):
    return C.smoothstep(e0, e1, x)


def rock(p_, seed=0, veins=0.0, glow=None, strata=18.0, period=None, crack=0.6, dust=0.0, rough=0.8,
         crack_glow=0.0, crack_color=None, cap=None, wet=None, depth_glow=None, smooth_top=0.0, foot=None,
         moss=None, frost=0.0):
    """E.stone plus the element extras.
    `crack_glow` lets light (crack_color) burn in the crack network (ember, overcharge, seal);
    `cap` = (colour, amount, threshold): snow, ash or moss lying on up faces;
    `wet` = (z0, z1): darker and glossy below z1 (a waterline);
    `moss` = (colour, amount): patches on top and sides from noise;
    `frost`: pale rime on edges and up faces (0..1)."""
    base = E.stone(p_, seed, veins=veins, strata=strata, period=period, crack=crack, dust=dust, rough=rough,
                   glow=glow, depth_glow=depth_glow, smooth_top=smooth_top, foot=foot)
    ccol = np.asarray(crack_color if crack_color is not None else (1.0, 0.5, 0.18))

    def style(s, idx, part):
        base(s, idx, part)
        p = s.p[idx]
        n = s.n[idx]
        q = E.nq(p, period)
        if crack_glow:
            line = (1 - ss(0.0, 0.035, np.abs(vn(q, 5.5, seed + 31)))) * ss(-0.25, 0.3, vn(q, 1.6, seed + 32))
            line = line * crack_glow
            s.albedo[idx] = C.lerp3(s.albedo[idx], ccol * 0.5, line)
            s.emit[idx] = np.clip(s.emit[idx] + ccol[None, :] * line[:, None], 0, 1)
            s.disp[idx] -= 0.004 * line
        if moss is not None:
            mcol, amount = moss
            m = ss(-0.1, 0.4, vn(q, 3.0, seed + 41) + 0.5 * n[:, 2] - 0.2) * amount
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.asarray(mcol) * (0.8 + 0.4 * ss(-0.5, 0.5, vn(q, 18, seed + 42)))[:, None], m)
            s.rough[idx] = np.maximum(s.rough[idx], 0.85 * m)
            s.disp[idx] += 0.006 * m
        if frost:
            f = (ss(0.3, 0.9, n[:, 2]) * 0.6 + ss(0.2, 0.6, vn(q, 9, seed + 51)) * 0.5) * frost
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.array((0.78, 0.84, 0.92)), np.clip(f, 0, 1) * 0.7)
        if cap is not None:
            ccap, amount, thr = cap
            c = ss(thr, thr + 0.18, n[:, 2] + 0.12 * vn(q, 5, seed + 61)) * amount
            s.albedo[idx] = C.lerp3(s.albedo[idx], np.asarray(ccap) * (0.92 + 0.12 * vn(q, 20, seed + 62))[:, None], c)
            s.emit[idx] = s.emit[idx] * (1 - c)[:, None]
            s.rough[idx] = np.clip(s.rough[idx] * (1 - c) + 0.8 * c, 0, 1)
            s.disp[idx] += 0.01 * c
        if wet is not None:
            z0, z1 = wet
            w = 1 - ss(z0, z1, p[:, 2])
            s.albedo[idx] = s.albedo[idx] * (1 - 0.45 * w)[:, None]
            s.rough[idx] = s.rough[idx] - 0.45 * w
    return style


def glow_seam(color, strength=1.0, body=None):
    return E.glowing(np.asarray(color), strength, body)


# ---------------------------------------------------------------------------------------------------------------
# Shapes
# ---------------------------------------------------------------------------------------------------------------

def rz(deg):
    return Matrix.Rotation(math.radians(deg), 3, "Z")


def tilt(rng, yaw=6, roll=5):
    return (rz(rng.uniform(-yaw, yaw)) @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "X")
            @ Matrix.Rotation(math.radians(rng.uniform(-roll, roll)), 3, "Y"))


def chip(b, centre, size, seed, style, depth=0.25, segments=(16, 8)):
    b.ellipsoid("Chip", Vector(centre), size, style=style, segments=segments, deform=C.chiselled(9, seed, depth))


def window(t, edge=0.08):
    """1 inside, 0 at both path ends: jitter fades out so neighbouring pieces meet on the plain profile."""
    return float(ss(0.0, edge, np.array(t)) * ss(1.0, 1.0 - edge, np.array(t)))


def polyline(points, s):
    """Point along a 2D polyline at fraction s (0..1) of its length."""
    pts = np.asarray(points, dtype=float)
    seg = np.hypot(*(pts[1:] - pts[:-1]).T)
    total = seg.sum()
    target = min(max(s, 0.0), 1.0) * total
    acc = 0.0
    for i, L in enumerate(seg):
        if acc + L >= target or i == len(seg) - 1:
            u = (target - acc) / max(L, 1e-9)
            return tuple(pts[i] + (pts[i + 1] - pts[i]) * min(u, 1.0))
        acc += L
    return tuple(pts[-1])


def straight_path(half, outward=(0.0, -1.0)):
    return E.line_path(-half, half, 0.0, outward)


def arc_path(centre, radius, a0, a1, towards_centre):
    """A circular arc (angles in degrees); the normal points to the centre or away from it."""
    cx, cy = centre

    def path(t):
        a = math.radians(a0 + (a1 - a0) * t)
        d = (math.cos(a), math.sin(a))
        p = (cx + radius * d[0], cy + radius * d[1])
        return p, ((-d[0], -d[1]) if towards_centre else d)
    return path


def edge_outer_path(radius):
    """Outer (convex) corner of a road or liquid cell in the (-X, -Y) quadrant: arc round (-r, -r) from the x = 0
    side to the y = 0 side, normal towards the cell."""
    return arc_path((-radius, -radius), radius, 0.0, 90.0, True)


def cliff_outer_path(radius, arm=1.0):
    """Wall in the (+X, +Y) quadrant, normal towards the field (E.corner_path)."""
    return E.corner_path(radius, arm)


def cliff_inner_path(radius, arm=1.0):
    """Field in the (-X, -Y) quadrant: the corner path mirrored through the point, normal towards the field."""
    base = E.corner_path(radius, arm)

    def path(t):
        (x, y), nrm = base(t)
        return (-x, -y), nrm
    return path


def path_length(path, samples=64):
    pts = [path(i / samples)[0] for i in range(samples + 1)]
    return sum(math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1]) for i in range(samples))


# ---- kerb (continuous rim; the round 8 review: it must read as one line, not a row of teeth) -----------------

def kerb_section(height=0.14, ground_w=0.12, road_w=0.07, lip=0.06):
    """(outer, inner) cross-section functions s -> (d, z); d > 0 towards the road."""
    outer_pts = [(-ground_w - 0.02, -0.06), (-ground_w, height * 0.78), (-ground_w + 0.035, height),
                 (road_w - 0.03, height * 0.98), (road_w, height * 0.72), (road_w + 0.01, 0.02),
                 (road_w + lip, 0.008), (road_w + lip + 0.015, -0.06)]
    inner_pts = [(-ground_w + 0.02, -0.07), (road_w + lip - 0.005, -0.07)]
    return (lambda s: polyline(outer_pts, s)), (lambda s: polyline(inner_pts, s))


def kerb_slab(b, name, path, length, style, seed, height=0.14, joints=3, raise_=0.0, period=None, nu=None):
    """A kerb swept along `path` (normal towards the road). Shallow joints every length / joints and a small
    height change per block; everything fades out at the ends so pieces chain."""
    outer, inner = kerb_section(height)
    rng = np.random.default_rng(seed)
    block_h = rng.uniform(-0.008, 0.008, joints + 2)

    def joint_and_block(t):
        x = t * joints
        k = int(min(math.floor(x), joints - 1))
        frac = x - k
        groove = math.exp(-((min(frac, 1 - frac) * length / joints) / 0.012) ** 2)
        groove = max(groove, math.exp(-((min(t, 1 - t) * length) / 0.01) ** 2))
        return groove, block_h[k]

    def out(t, s):
        d, z = outer(s)
        top = float(ss(0.15, 0.3, np.array(s)) * ss(0.75, 0.6, np.array(s)))
        groove, bh = joint_and_block(t)
        z += raise_ + top * (bh - 0.008 * groove)
        if z > 0.0:
            z += top * 0.006 * E.periodic(t * length, length, 4, seed + 1) * window(t, 0.05)
        return d, z

    def inn(t, s):
        return inner(s)

    def jitter(t, s, d, z):
        side = 1.0 - float(ss(0.15, 0.3, np.array(s)) * ss(0.75, 0.6, np.array(s)))
        return 0.006 * side * E.periodic(t * length * 2, length * 2, 3, seed + 2) * window(t, 0.05), 0.0

    nu = nu or max(24, int(length * 60))
    return E.sweep(b, name, path, out, inn, nu, 24, style, jitter)


def kerb_straight(style, seed=101, accent=None):
    b = C.PartBuilder()
    kerb_slab(b, "Kerb", straight_path(0.5), 1.0, style, seed)
    if accent:
        accent(b, np.random.default_rng(seed + 50), "straight")
    return b


def kerb_outer(style, seed=201, accent=None, radius=0.12):
    b = C.PartBuilder()
    path = edge_outer_path(radius)
    kerb_slab(b, "Kerb", path, path_length(path), style, seed, joints=1, raise_=0.005, nu=30)
    if accent:
        accent(b, np.random.default_rng(seed + 50), "outer")
    return b


def kerb_inner(style, seed=301, accent=None):
    """Inside of a bend (road on three cells): the two straights cross at the point; a squared corner stone fills
    the road-side notch and caps the joint."""
    b = C.PartBuilder()
    b.ellipsoid("Corner", Vector((0.0, 0.0, 0.0)), (0.12, 0.12, 0.155), rz(45), style=style, segments=(32, 16),
                deform=E.boxy(seed, 0.3, 4, 0.08))
    if accent:
        accent(b, np.random.default_rng(seed + 50), "inner")
    return b


# ---- bank (shore lip; the liquid on -Y) ----------------------------------------------------------------------

def bank_section(lip_h=0.05, back=0.16, reach=0.1, depth=0.16):
    outer_pts = [(-back - 0.02, -0.05), (-back, 0.0), (-0.04, lip_h * 0.8), (0.0, lip_h), (reach * 0.6, lip_h * 0.6),
                 (reach, 0.0), (reach + 0.03, -depth)]
    inner_pts = [(-back + 0.02, -0.07), (reach - 0.02, -depth - 0.02)]
    return (lambda s: polyline(outer_pts, s)), (lambda s: polyline(inner_pts, s))


def bank_slab(b, name, path, length, style, seed, lip_h=0.05, raise_=0.0, nu=None):
    outer, inner = bank_section(lip_h)

    def out(t, s):
        d, z = outer(s)
        w = window(t, 0.06)
        return d + 0.025 * E.periodic(t * length, length, 3, seed) * w * float(ss(0.2, 0.6, np.array(s))), \
            z + raise_ + 0.012 * E.periodic(t * length * 2, length * 2, 4, seed + 1) * w * (z > -0.02)

    nu = nu or max(24, int(length * 50))
    return E.sweep(b, name, path, out, lambda t, s: inner(s), nu, 18, style, None)


def bank_straight(style, seed=1401, accent=None):
    b = C.PartBuilder()
    bank_slab(b, "Shelf", straight_path(0.5), 1.0, style, seed)
    if accent:
        accent(b, np.random.default_rng(seed + 50), "straight")
    return b


def bank_outer(style, seed=1501, accent=None, radius=0.14):
    b = C.PartBuilder()
    path = edge_outer_path(radius)
    bank_slab(b, "Shelf", path, path_length(path), style, seed, raise_=0.004, nu=30)
    if accent:
        accent(b, np.random.default_rng(seed + 50), "outer")
    return b


def bank_inner(style, seed=1601, accent=None):
    b = C.PartBuilder()
    b.ellipsoid("Corner", Vector((0.0, 0.0, -0.01)), (0.16, 0.16, 0.07), rz(45), style=style, segments=(28, 14),
                deform=C.chiselled(12, seed, 0.15))
    if accent:
        accent(b, np.random.default_rng(seed + 50), "inner")
    return b


# ---- cliffs (swept wall with an element profile; masses and accents added by the continent) --------------------

def cliff_sweep(b, name, kind, profile, inner, style, jitter=None, nu=None, nv=60, radius=0.6):
    """kind: 'straight' (x -1..1), 'outer', 'inner'. profile(t, s) -> (d, z) with d > 0 towards the field."""
    if kind == "straight":
        path = straight_path(1.0)
    elif kind == "outer":
        path = cliff_outer_path(radius)
    else:
        path = cliff_inner_path(radius)
    length = path_length(path)

    def jit(t, s, d, z):
        if jitter is None:
            return 0.0, 0.0
        dd, dz = jitter(t * length, s, d, z)
        w = window(t, 0.12)
        return dd * w, dz * w

    nu = nu or max(40, int(length * 50))
    E.sweep(b, name, path, profile, inner, nu, nv, style, jit)
    return path, length


def along(path, t, d=0.0, z=0.0):
    """A point at fraction t of a path, offset d along its normal (towards the field) and lifted z."""
    (x, y), (nx, ny) = path(t)
    return Vector((x + nx * d, y + ny * d, z))


def facing(path, t):
    (_, _), (nx, ny) = path(t)
    return math.atan2(ny, nx)


# ---------------------------------------------------------------------------------------------------------------
# Diorama (the round 8 approach, per continent): a 12 x 8 cell piece of a level built from the exports
# ---------------------------------------------------------------------------------------------------------------

W, H = 12, 8
ROAD = ({(x, y) for x in range(0, 5) for y in (5, 6)} | {(x, y) for x in (3, 4) for y in (2, 3, 4)}
        | {(x, y) for x in range(5, 12) for y in (2, 3)})
PADS = [(0, 4), (1, 4), (2, 4), (2, 3), (2, 2), (5, 4), (6, 4), (7, 4), (5, 1), (6, 1), (9, 4), (10, 4), (9, 1),
        (10, 1), (6, 6)]
POOL = {(8, 6), (9, 6)}
GATE_CELL = (10, 2.5)
X0, X1, Y0, Y1 = -10.0, 10.0, -9.0, 11.0
SPLAT_PX = 32
SIDES = [(1, 0), (0, 1), (-1, 0), (0, -1)]


def world(cell):
    return Vector(((cell[0] - (W - 1) / 2), (cell[1] - (H - 1) / 2), 0.0))


def angle_for(direction):
    """Rotation (degrees) that turns a piece's -Y (the bordered thing) towards `direction`."""
    return math.degrees(math.atan2(direction[0], -direction[1]))


class Diorama:
    """Places a continent's exports. cfg keys: sets (ground, alt, element, road short names), liquid (material
    colour, emission, strength, roughness), lights, world, mist, decor list, scatter list, actors, extras."""

    def __init__(self, cont, cfg):
        self.cont = cont
        self.cfg = cfg
        self.sources = {}
        self.pads_list = cfg.get("pads", PADS)
        self.pool = set(cfg.get("pool", POOL))

    # -- sources
    def source(self, name, folder=None):
        if name in self.sources:
            return self.sources[name]
        folder = folder or os.path.join(self.cont.dir, name)
        fbx = os.path.join(folder, name + ".fbx")
        if not os.path.exists(fbx):
            C.log("missing asset", name)
            self.sources[name] = None
            return None
        objs = E.textured_import(folder, name, emission_strength=self.cfg.get("emission", 2.4))
        mesh = [o for o in objs if o.type == "MESH"][0]
        for o in objs:
            o.hide_render = True
            o.hide_viewport = True
        self.sources[name] = mesh
        return mesh

    def place(self, short, location, rot_deg=0.0, scale=1.0, folder=None, z=0.0, own=True):
        name = self.cont.name(short) if own else short
        src = self.source(name, folder)
        if src is None:
            return None
        obj = C.link(bpy.data.objects.new(name + "_inst", src.data))
        obj.matrix_world = src.matrix_world.copy()
        obj.location = Vector((location[0], location[1], z if len(location) < 3 else location[2] + z))
        euler = src.rotation_euler.copy()
        euler.z += math.radians(rot_deg)
        obj.rotation_euler = euler
        base = src.scale
        obj.scale = (base.x * scale, base.y * scale, base.z * scale)
        obj.hide_render = False
        return obj

    # -- terrain
    def splat(self, path):
        w = int((X1 - X0) * SPLAT_PX)
        h = int((Y1 - Y0) * SPLAT_PX)
        xs = X0 + (np.arange(w) + 0.5) / SPLAT_PX
        ys = Y0 + (np.arange(h) + 0.5) / SPLAT_PX
        X, Yy = np.meshgrid(xs, ys)
        cx = np.floor(X + (W - 1) / 2 + 0.5).astype(int)
        cy = np.floor(Yy + (H - 1) / 2 + 0.5).astype(int)
        road = np.zeros((h, w))
        for (x, y) in ROAD:
            road[(cx == x) & (cy == y)] = 1
            if x == 0:
                road[(cx < 0) & (cy == y)] = 1
            if x == W - 1:
                road[(cx > W - 1) & (cy == y)] = 1
        wob = 0.35 * np.sin(X * 2.1 + Yy * 1.3) + 0.3 * np.sin(X * 0.9 - Yy * 2.3) + 0.15 * np.sin(X * 5.3 + Yy * 4.1)
        alt = np.zeros((h, w))
        for (fx, fy), r in self.cfg.get("alt_areas", []):
            c = world((fx, fy))
            alt = np.maximum(alt, 1 - C.smoothstep(r * 0.55, r, np.hypot(X - c.x, Yy - c.y) + wob * 0.6))
        elem = np.zeros((h, w))
        for (fx, fy), r in self.cfg.get("element_areas", []):
            c = world((fx, fy))
            elem = np.maximum(elem, 1 - C.smoothstep(r * 0.55, r, np.hypot(X - c.x, Yy - c.y) - wob * 0.5))
        # Element drifts collect along the kerbs and in the lee of the walls a little.
        alt *= (1 - road)
        elem *= (1 - road)
        img = np.dstack([road, alt, elem, np.ones_like(road)]).astype(np.float32)
        C.array_to_image(img, path, colorspace="Non-Color")
        return path

    def layer(self, nt, short, pos):
        paths = set_paths(self.cont, short)
        mp = nt.nodes.new("ShaderNodeMapping")
        k = 1.0 / self.cont.tiles[short]
        mp.inputs["Scale"].default_value = (k, k, k)
        nt.links.new(pos, mp.inputs["Vector"])
        vec = mp.outputs["Vector"]
        alb = _tex(nt, paths["albedo"], vec)
        mask = _tex(nt, paths["mask"], vec, True)
        nrm = _tex(nt, paths["normal"], vec, True)
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(mask.outputs["Color"], sep.inputs[0])
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        nt.links.new(alb.outputs["Color"], mul.inputs[6])
        nt.links.new(sep.outputs[0], mul.inputs[7])
        out = {"col": mul.outputs[2], "smooth": sep.outputs[1], "height": sep.outputs[2], "nrm": nrm.outputs["Color"]}
        out["emit"] = _tex(nt, paths["emission"], vec).outputs["Color"] if "emission" in paths else None
        return out

    @staticmethod
    def _math(nt, op, a, b=None, clamp=False):
        m = nt.nodes.new("ShaderNodeMath")
        m.operation = op
        m.use_clamp = clamp
        for i, v in enumerate((a, b)):
            if v is None:
                continue
            if isinstance(v, (int, float)):
                m.inputs[i].default_value = v
            else:
                nt.links.new(v, m.inputs[i])
        return m.outputs[0]

    @staticmethod
    def _mix(nt, a, b, f, kind="RGBA"):
        m = nt.nodes.new("ShaderNodeMix")
        m.data_type = kind
        m.clamp_factor = True
        nt.links.new(f, m.inputs["Factor"])
        ia, ib, out = (6, 7, 2) if kind == "RGBA" else (2, 3, 0)
        for idx, v in ((ia, a), (ib, b)):
            if isinstance(v, (int, float, tuple)):
                m.inputs[idx].default_value = v
            else:
                nt.links.new(v, m.inputs[idx])
        return m.outputs[out]

    def terrain_material(self, splat_path):
        mat = C.new_material(self.cont.prefix + "_Terrain")
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        bsdf.inputs["Specular IOR Level"].default_value = 0.35
        pos = nt.nodes.new("ShaderNodeNewGeometry").outputs["Position"]
        smp = nt.nodes.new("ShaderNodeMapping")
        smp.inputs["Location"].default_value = (-X0 / (X1 - X0), -Y0 / (Y1 - Y0), 0)
        smp.inputs["Scale"].default_value = (1 / (X1 - X0), 1 / (Y1 - Y0), 1)
        nt.links.new(pos, smp.inputs["Vector"])
        sp = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(_tex(nt, splat_path, smp.outputs["Vector"], True).outputs["Color"], sp.inputs[0])
        sets = self.cfg["sets"]
        acc = dict(self.layer(nt, sets["ground"], pos))
        order = [(sets["alt"], 1, 4.0), (sets["element"], 2, 4.0), (sets["road"], 0, 7.0)]
        for short, ch, sharp in order:
            layer = self.layer(nt, short, pos)
            diff = self._math(nt, "SUBTRACT", layer["height"], acc["height"])
            v = self._math(nt, "ADD", sp.outputs[ch], self._math(nt, "MULTIPLY", diff, 0.35))
            v = self._math(nt, "MULTIPLY", self._math(nt, "SUBTRACT", v, 0.5), sharp)
            w = self._math(nt, "ADD", v, 0.5, clamp=True)
            acc["col"] = self._mix(nt, acc["col"], layer["col"], w)
            acc["nrm"] = self._mix(nt, acc["nrm"], layer["nrm"], w)
            acc["smooth"] = self._mix(nt, acc["smooth"], layer["smooth"], w, "FLOAT")
            acc["height"] = self._mix(nt, acc["height"], layer["height"], w, "FLOAT")
            le = layer["emit"] if layer["emit"] is not None else (0.0, 0.0, 0.0, 1.0)
            ae = acc["emit"] if acc["emit"] is not None else (0.0, 0.0, 0.0, 1.0)
            acc["emit"] = self._mix(nt, ae, le, w)
        nt.links.new(acc["col"], bsdf.inputs["Base Color"])
        nt.links.new(self._math(nt, "SUBTRACT", 1.0, acc["smooth"]), bsdf.inputs["Roughness"])
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(acc["nrm"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
        nt.links.new(acc["emit"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = self.cfg.get("terrain_emission", 1.6)
        return mat

    def grid_terrain(self, mat, skip, z=0.0):
        bm = bmesh.new()
        verts = {}

        def v(x, y):
            if (x, y) not in verts:
                verts[(x, y)] = bm.verts.new((x, y, z))
            return verts[(x, y)]

        for ix in range(int(X0), int(X1)):
            for iy in range(int(Y0), int(Y1)):
                cell = (ix + int((W - 1) / 2 + 0.5), iy + int((H - 1) / 2 + 0.5))
                if cell in skip or (self.cfg.get("front_edge") and cell[1] < 0):
                    continue
                # Plateau-type borders (The Core): nothing behind the back edge or the right wall, only the void.
                if self.cfg.get("cut_outside") and (cell[1] >= H or (cell[0] >= W and cell[1] >= 4)):
                    continue
                bm.faces.new((v(ix, iy), v(ix + 1, iy), v(ix + 1, iy + 1), v(ix, iy + 1)))
        mesh = bpy.data.meshes.new("Terrain")
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(mat)
        return C.link(bpy.data.objects.new("Terrain", mesh))

    # -- dressing
    def kerbs(self):
        gate = {(int(GATE_CELL[0]), 2), (int(GATE_CELL[0]), 3)}
        for c in sorted(ROAD):
            for d in SIDES:
                n = (c[0] + d[0], c[1] + d[1])
                if n in ROAD or not (0 <= n[0] < W and 0 <= n[1] < H) or c in gate:
                    continue
                # Piece centred on the side, its -Y (the road) turned towards the road cell, i.e. against d.
                self.place("RoadEdge_Straight", world(c) + Vector((d[0] * 0.5, d[1] * 0.5, 0)), angle_for((-d[0], -d[1])))
        for gx in range(1, W):
            for gy in range(1, H):
                quads = {(sx, sy): (gx + (sx - 1) // 2, gy + (sy - 1) // 2) for sx in (-1, 1) for sy in (-1, 1)}
                road = [q for q, cell in quads.items() if cell in ROAD]
                if len(road) not in (1, 3):
                    continue
                if any(cell in gate for cell in quads.values()):
                    continue
                point = world((gx - 0.5, gy - 0.5))
                if len(road) == 1:
                    target, name = road[0], "RoadEdge_OuterCorner"
                else:
                    g = [q for q in quads if q not in road][0]
                    target, name = (-g[0], -g[1]), "RoadEdge_InnerCorner"
                rot = math.degrees(math.atan2(target[1], target[0]) - math.atan2(-1, -1))
                self.place(name, point, rot)

    def banks(self):
        for c in self.pool:
            for d in SIDES:
                n = (c[0] + d[0], c[1] + d[1])
                if n in self.pool:
                    continue
                self.place("Bank_Straight", world(c) + Vector((d[0] * 0.5, d[1] * 0.5, 0)), angle_for((-d[0], -d[1])))
        xs = sorted({c[0] for c in self.pool})
        ys = sorted({c[1] for c in self.pool})
        for gx, q in ((xs[0], (1, 1)), (xs[-1] + 1, (-1, 1))):
            for gy, qy in ((ys[0], 1), (ys[-1] + 1, -1)):
                point = world((gx - 0.5, gy - 0.5))
                target = (q[0], qy)
                rot = math.degrees(math.atan2(target[1], target[0]) - math.atan2(-1, -1))
                self.place("Bank_OuterCorner", point, rot)

    def border(self):
        """Back wall along the top edge (field on -Y), an inner corner at the top right, a short wall down the right
        side and an outer corner where it turns off the map above the road exit; plateau-type edges along the front
        when cfg['front_edge'] is set."""
        top = H - 0.5
        right = W - 0.5
        for x in range(-1, W - 2, 2):
            self.place("Cliff_Straight", world((x + 0.5, top)), 0)
        self.place("Cliff_InnerCorner", world((right, top)), 0)
        self.place("Cliff_Straight", world((right, top - 2.0)), angle_for((-1, 0)))
        self.place("Cliff_OuterCorner", world((right, top - 4.0)), 0)
        if self.cfg.get("front_edge"):
            for x in range(-1, W + 1, 2):
                self.place("Cliff_Straight", world((x + 0.5, -0.5)), 180)

    def pads(self):
        pad = os.path.join(E.ENV, "BuildPadTile")
        for c in self.pads_list:
            self.place("BuildPadTile", world(c), 0, folder=pad, own=False)
        self.place("GatePlinth", world(GATE_CELL), 90, folder=os.path.join(E.ENV, "GatePlinth"), own=False)

    def liquid(self):
        liq = self.cfg["liquid"]
        mat = C.principled("Liquid", liq["color"], liq.get("rough", 0.05), emission=liq.get("emit"),
                           strength=liq.get("strength", 0.0))
        xs = [world(c).x for c in self.pool]
        y = world(next(iter(self.pool))).y
        E.plane("Liquid", max(xs) - min(xs) + 1.0, 1.0, mat, location=((max(xs) + min(xs)) / 2, y, liq.get("z", -0.03)))
        if liq.get("origin"):
            folder = os.path.join(E.ENV, liq["origin"])
            self.place(liq["origin"], ((max(xs) + min(xs)) / 2, y), 30, scale=0.6, folder=folder, own=False,
                       z=liq.get("z", -0.03))

    def decor(self, rng):
        for short, cell, rot, scale in self.cfg.get("decor", []):
            if short.startswith("@"):  # an ArtSource/Exports/<Name> asset
                short = short[1:]
                self.place(short, world(cell), rot, scale=scale, folder=os.path.join(_EXPORTS_ROOT, short), own=False)
            elif short.startswith("env:"):  # a Crystal Plains kit asset (Exports/Env/<Name>)
                short = short[4:]
                self.place(short, world(cell), rot, scale=scale, folder=os.path.join(E.ENV, short), own=False)
            else:
                self.place(short, world(cell), rot, scale=scale)
        blocked = set(ROAD) | set(self.pads_list) | self.pool
        for i in range(self.cfg.get("scatter_count", 30)):
            x, y = rng.uniform(-0.4, W - 0.6), rng.uniform(-0.3, H - 1.2)
            c = (int(round(x)), int(round(y)))
            if c in blocked:
                continue
            kinds = self.cfg["scatter"]
            self.place(kinds[i % len(kinds)], world((x, y)), rng.uniform(0, 360), scale=rng.uniform(0.8, 1.2))

    def actors(self):
        exp = _EXPORTS_ROOT
        for tower, tier, cell in self.cfg.get("towers", []):
            folder = os.path.join(exp, tower)
            fbx = os.path.join(folder, f"{tower}_{tier}.fbx")
            if not os.path.exists(fbx):
                continue
            objs = E.import_fbx(fbx)
            tex = {k.lower(): os.path.join(folder, f"{tower}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
            mat = C.game_material(f"{tower}_{tier}_Mat", tex, 2.2, 0.4, ())
            for o in objs:
                if o.type == "MESH":
                    o.data.materials.clear()
                    o.data.materials.append(mat)
            _holder(objs, world(cell) + Vector((0, 0, 0.045)), 0.0, 1.0)
        for name, scale, skin, spots in self.cfg.get("enemies", []):
            fbx = os.path.join(exp, name, name + ".fbx")
            if not os.path.exists(fbx):
                continue
            tdir = os.path.join(exp, name, "Skins", skin) if skin else os.path.join(exp, name)
            stem = f"{name}_{skin}" if skin else name
            tex = {k.lower(): os.path.join(tdir, f"{stem}_{k}.png") for k in ("Albedo", "Normal", "Emission", "Mask")}
            mat = C.game_material(stem + "_Mat", tex, 2.2, 0.4, ())
            for x, y, rot in spots:
                objs = E.import_fbx(fbx)
                for o in objs:
                    if o.type == "MESH":
                        o.data.materials.clear()
                        o.data.materials.append(mat)
                _holder(objs, world((x, y)), rot, scale)

    def mist(self):
        m = self.cfg.get("mist")
        if not m:
            return
        mesh = bpy.data.meshes.new("Mist")
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bm.to_mesh(mesh)
        bm.free()
        obj = C.link(bpy.data.objects.new("Mist", mesh))
        obj.location = ((X0 + X1) / 2, (Y0 + Y1) / 2 - 3.0, m.get("z", -1.6))
        obj.scale = (X1 - X0 + 6, Y1 - Y0 + 10, m.get("height", 4.2))
        mat = C.new_material("MistVolume")
        nt = mat.node_tree
        nt.nodes.remove(nt.nodes.get("Principled BSDF"))
        vol = nt.nodes.new("ShaderNodeVolumePrincipled")
        vol.inputs["Density"].default_value = m["density"]
        vol.inputs["Color"].default_value = (*m["color"], 1)
        vol.inputs["Anisotropy"].default_value = 0.2
        nt.links.new(vol.outputs[0], nt.nodes.get("Material Output").inputs["Volume"])
        mesh.materials.append(mat)

    def lights(self):
        L = self.cfg["lights"]
        E.level_lights(key=L.get("key", 2.0), rim=L.get("rim", 1.2), key_color=L.get("key_color", (0.92, 0.9, 1.0)),
                       rim_color=L.get("rim_color", (0.55, 0.5, 1.0)))
        for cell, col, energy in L.get("points", []):
            E.point_light("Glow", world(cell) + Vector((0, 0, 0.7)), col, energy, 0.3)

    def build(self, out, samples=64):
        C.reset_scene()
        rng = np.random.default_rng(9)
        E.level_world(self.cfg.get("world", (0.01, 0.01, 0.025)), 1.0)
        mat = self.terrain_material(self.splat(os.path.join(out, f"{self.cont.key.lower()}_splat.png")))
        self.grid_terrain(mat, self.pool | set(self.cfg.get("cut", [])))
        if self.cfg.get("void"):
            E.plane("Void", 40, 10, E.emissive("Void", self.cfg["void"], 0.5), location=(0, -9.5, -3.2))
        self.mist()
        self.kerbs()
        self.pads()
        self.banks()
        self.liquid()
        self.border()
        self.decor(rng)
        if self.cfg.get("extras"):
            self.cfg["extras"](self, rng)
        self.actors()
        self.lights()
        key = self.cont.key.lower()
        cam = C.game_camera(Vector((0, 0.5, 0)), 14.5, yaw=16)
        paths = [C.render(cam, os.path.join(out, f"{key}_diorama_game.png"), 1600, 900, samples=samples)]
        tc = self.cfg.get("close_target", (6.3, 2.6))
        cam2 = C.closeup_camera(world(tc) + Vector((0, 0, 0.3)), 6.5, azimuth=-38, elevation=28, lens=30)
        paths.append(C.render(cam2, os.path.join(out, f"{key}_diorama_close.png"), 1600, 900, samples=samples))
        C.save_blend(f"Env_{self.cont.key}_Diorama")
        return paths


_EXPORTS_ROOT = os.path.join(C.REPO, "ArtSource", "Exports")


def _holder(objs, location, rot_deg, scale):
    empty = C.link(bpy.data.objects.new("Holder", None))
    empty.location = location
    empty.rotation_euler.z = math.radians(rot_deg)
    empty.scale = (scale, scale, scale)
    for o in objs:
        if o.parent is None:
            o.parent = empty
    return empty


# ---------------------------------------------------------------------------------------------------------------
# Runner
# ---------------------------------------------------------------------------------------------------------------

def run(cont, sets, kit, decor, diorama_cfg, world=(0.03, 0.03, 0.05)):
    """CLI: -- --preview <dir> [--part materials|kit|decor|diorama|all] [--only A,B] [--norender] [--size 1024]"""
    args = C.cli_args()
    part = args.get("part", "all")
    only = args.get("only")
    out = C.preview_dir()
    render = not args.get("norender")
    if part in ("materials", "all"):
        C.reset_scene()
        run_materials(cont, sets, out, int(args.get("size", 1024)), only, render, world)
    if part in ("kit", "all"):
        C.reset_scene()
        run_assets(cont, kit, out, only, render, "Kit")
    if part in ("decor", "all"):
        C.reset_scene()
        run_assets(cont, decor, out, only, render, "Decor")
    if part in ("diorama", "all") and diorama_cfg:
        Diorama(cont, diorama_cfg).build(out, int(args.get("samples", 64)))
