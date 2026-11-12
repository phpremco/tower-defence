"""Helpers for the round-15 creatures (build_shardback.py, build_mireleech.py, build_knitback.py,
build_slagstrider.py, build_siftjaw.py, build_surgefin.py) and the corrupted skin set (build_corrupted_skins.py).
Kept apart from the shared modules and from creature_kit2.py / boss_kit.py, which this module only imports.

- Surface styles: ice prisms with a warm root glow, glowing sockets, bark plates and moss hide (camouflage-ready, two
  close values), sap pods, molten slag seams, sand plates, wet spotted skin, translucent fin sails.
- ClipPoser: a ScalePoser that keys bone scale only in the clips that name scale bones, so Unity can drive the same
  bones from code in every other clip (the Shardback's prisms, the Knitback's pods).
- build_lod: a LOD1 that keeps rigid parts rigid (common.build_lod skins without `rigid`) and keys scale per clip.
- run_creature: the whole pipeline of a common enemy (asset, rig, skin, clips, FBX, LOD1, report, .blend, previews).
- Leg helpers: limbs from a table, feet placed after the body moved, a gait from per-leg phases.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Vector

import boss_kit as B
import chitin as H
import common as C
import creature_kit2 as K2

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
SIDES = (("L", -1), ("R", 1))


def _unit(L):
    return L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)


def arr(c):
    return np.array(c, dtype=float)


# ---------------------------------------------------------------------------------------------------------------
# Surface styles
# ---------------------------------------------------------------------------------------------------------------

def ice_prism(k, root=(0.16, 0.24, 0.42), body=(0.55, 0.72, 0.92), tip=(0.9, 0.96, 1.0), ice_glow=(0.45, 0.72, 1.0),
              warm=(1.0, 0.55, 0.16), warm_glow=0.85, ice_amount=0.3, seed=0):
    """An ice prism (a shard part): deep blue at the root, frosted pale towards the tip, dark inner fracture planes
    and frost rims on the ridges. The warm flesh it grew from lights the root amber and runs up a thin core line, so
    the ice reads as alive and the glow stays warm (alien rule) while the body of the prism stays cold."""
    root, body, tip, ice_glow, warm = (arr(c) for c in (root, body, tip, ice_glow, warm))

    def style(s, idx, part):
        z, radius, dist = C.tube_param(part, s.p[idx])
        rho = dist / np.maximum(radius, part.radii[0] * 0.05)
        p = s.p[idx]
        col = C.lerp3(C.lerp3(root, body, C.smoothstep(0.0, 0.45, z)), tip, C.smoothstep(0.55, 1.0, z))
        ridge = C.smoothstep(0.85, 1.0, rho)
        frost = C.smoothstep(0.1, 0.6, C.value_noise(p, 30 / k, seed)) * (0.4 + 0.6 * ridge)
        col = C.lerp3(col, tip, frost * 0.55)
        plane = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 11 / k, seed + 3)))
        plane *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 3 / k, seed + 4))
        col = C.lerp3(col, root * 0.5, plane * 0.7)
        base = 1 - C.smoothstep(0.0, 0.3, z)
        core = (1 - C.smoothstep(0.0, 0.3, rho)) * (1 - C.smoothstep(0.3, 0.85, z))
        col = C.lerp3(col, warm * 0.7, np.clip(base * 0.65 + core * 0.3, 0, 1))
        s.albedo[idx] = col * (0.86 + 0.12 * C.value_noise(p, 6 / k, seed + 5))[:, None]
        cold = ice_glow[None, :] * (ice_amount * (0.3 + 0.7 * ridge) * C.smoothstep(0.2, 1.0, z) * (1 - 0.6 * plane))[:, None]
        hot = warm[None, :] * (np.clip(base * 1.0 + core * 0.6, 0, 1) * warm_glow)[:, None]
        s.emit[idx] = np.clip(cold + hot, 0, 1)
        s.rough[idx] = 0.28 + 0.3 * frost + 0.15 * plane
        s.disp[idx] = 0.0
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        s.flow[idx] = z * C.axis_length(part)
    return style


def socket_floor(pal, k, glow=1.0, seed=0):
    """Raw flesh in a socket (under an ice cluster, a pod): veined, glowing; seen only when what grew there is gone."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        p = s.p[idx]
        vein = 1 - C.smoothstep(0.0, 0.06, np.abs(C.value_noise(p, 40 / k, seed)))
        top = C.smoothstep(-0.2, 0.5, u[:, 2])
        col = C.lerp3(pal.membrane, pal.glow * 0.8, top * 0.8)
        s.albedo[idx] = C.lerp3(col, pal.seam, vein * 0.6)
        s.emit[idx] = pal.glow[None, :] * (top * glow * (1 - 0.5 * vein))[:, None]
        s.disp[idx] = -0.002 * k * vein
        s.rough[idx] = 0.35
        H.family(s, idx, membrane=1.0, flow=u[:, 2] * part.radii[2])
    return style


def bark(pal, k, grain=38.0, splits=0.8, moss=0.55, glow=0.0, seed=0, axis=1, camo_cells=7.0):
    """Bark armour (ellipsoids or tubes): fibrous grain along `axis`, deep splits that glow faintly with sap when
    `glow` > 0, moss patches in a second close value (a disruptive camouflage pattern, no bright markings)."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        if part.kind == "tube" or part.kind == "shard":
            t, radius, dist = C.tube_param(part, p)
            along = t * C.axis_length(part)
            around = C.value_noise(p, 9 / k, seed)
        else:
            L = C.local_points(part, p)
            along = L[:, axis] * part.radii[axis]
            u = _unit(L)
            around = np.arctan2(u[:, 0], u[:, 2 if axis != 2 else 1])
        fib = np.abs(np.sin(around * grain * 0.25 + C.value_noise(p, 14 / k, seed + 1) * 3.0 + along * 4 / k))
        groove = 1 - C.smoothstep(0.0, 0.3, fib)
        split = (1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p * np.array([1.0, 0.35, 1.0]), 9 / k, seed + 2)))) * splits
        split *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 3 / k, seed + 3))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        camo = C.smoothstep(-0.15, 0.25, C.value_noise(p, camo_cells / k, seed + 4))
        col = C.lerp3(col, pal.membrane * (0.7 + 0.6 * C.smoothstep(-0.3, 0.6, n[:, 2]))[:, None], camo * moss)
        col = col * (0.85 + 0.15 * (1 - groove))[:, None] * H.mottle(s, idx, 0.12, 60 / k, seed + 5)
        col = C.lerp3(col, pal.seam, np.maximum(groove * 0.5, split * 0.9))
        sap = split * glow
        col = C.lerp3(col, pal.glow * 0.5, sap * 0.6)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * sap[:, None]
        s.disp[idx] = -0.003 * k * groove - 0.005 * k * split + 0.002 * k * camo * moss
        s.rough[idx] = 0.82 - 0.15 * sap
        H.family(s, idx, rock=0.4 * (1 - camo * moss), chitin=0.6, membrane=camo * moss * 0.5, flow=along)
    return style


def sap_pod(pal, k, glow=1.0, seed=0, veins=22):
    """A translucent sap pod (the regrowth organ): amber-green light inside, dark curled fibres pressed against the
    skin (the new shoot), a woody collar at the base."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        p = s.p[idx]
        # Veins of mixed cell size that come and go (never a uniform honeycomb).
        rng = np.random.default_rng(seed)
        cells = C.fibonacci_sphere(veins, seed=seed) * rng.uniform(0.7, 1.3, (veins, 1))
        e = C.voronoi_edge(u + 0.12 * C.value_noise(p, 30 / k, seed + 7)[:, None], cells / np.linalg.norm(cells, axis=1, keepdims=True))
        vein = (1 - C.smoothstep(0.0, 0.035, e)) * C.smoothstep(-0.3, 0.2, C.value_noise(p, 12 / k, seed + 8))
        coil = np.abs(np.sin(np.arctan2(u[:, 0], u[:, 1]) * 2 + u[:, 2] * 7 + 0.5 * C.value_noise(p, 20 / k, seed)))
        shoot = (1 - C.smoothstep(0.0, 0.2, coil)) * C.smoothstep(-0.3, 0.4, u[:, 2])
        collar = 1 - C.smoothstep(-0.75, -0.45, u[:, 2])
        # Lit from a core inside: bright where the skin faces the camera side-on, darker skin over the crown.
        light = (0.3 + 0.7 * (1 - C.smoothstep(0.3, 0.95, np.abs(u[:, 2])))) * C.smoothstep(-0.7, -0.2, u[:, 2])
        col = C.lerp3(pal.membrane, pal.glow * 0.6, light * 0.45)
        col = C.lerp3(col, pal.dark, np.maximum(shoot * 0.8, vein * 0.6))
        col = C.lerp3(col, pal.side, collar)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (light * glow * (1 - 0.85 * shoot) * (1 - 0.6 * vein) * (1 - collar))[:, None]
        s.disp[idx] = 0.002 * k * shoot - 0.0015 * k * vein
        s.rough[idx] = 0.3 + 0.4 * collar
        H.family(s, idx, membrane=1.0 - collar, chitin=collar, flow=u[:, 2] * part.radii[2])
    return style


def wet_skin(pal, k, spots=60.0, spot_amount=0.5, gloss=0.3, seed=0, belly_light=0.55, glow_spots=0.0):
    """Wet amphibian skin: dark top, paler belly, fine spots of mixed size (a few glowing when `glow_spots`), glossy."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        under = C.smoothstep(0.1, -0.6, n[:, 2]) * belly_light
        col = C.lerp3(col, pal.bone * 0.6, under)
        v = C.value_noise(p, spots / k, seed)
        spot = C.smoothstep(0.45, 0.6, v) * (1 - under)
        big = C.smoothstep(0.35, 0.55, C.value_noise(p, spots * 0.35 / k, seed + 1))
        col = C.lerp3(col, pal.dark, big * 0.4)
        col = C.lerp3(col, pal.seam, spot * spot_amount)
        col = col * H.mottle(s, idx, 0.08, 80 / k, seed + 2)
        s.albedo[idx] = col
        if glow_spots:
            gs = C.smoothstep(0.62, 0.72, C.value_noise(p, spots * 0.5 / k, seed + 3)) * (1 - under)
            s.emit[idx] = pal.glow[None, :] * (gs * glow_spots)[:, None]
            s.albedo[idx] = C.lerp3(col, pal.glow * 0.6, gs * glow_spots)
        s.disp[idx] = 0.0015 * k * C.value_noise(p, 90 / k, seed + 4) - 0.001 * k * spot
        s.rough[idx] = gloss + 0.1 * spot
        H.family(s, idx, membrane=1.0, flow=p[:, 2])
    return style


def fin_sail(pal, k, rays=5, edge_glow=1.0, edge_width=0.22, axis=1, height_axis=2, seed=0):
    """A translucent fin (a flattened ellipsoid, thin along x): dark spines (rays) along `height_axis`, membrane
    between them lit from inside, and a glowing outer edge (the top and back rim)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        along = u[:, axis]
        up = u[:, height_axis]
        ray = 1 - C.smoothstep(0.0, 0.18, np.abs(np.sin((along + 0.2 * up) * rays * math.pi * 0.5)))
        rim = C.smoothstep(1 - edge_width, 1.0, np.hypot(along, up)) * C.smoothstep(-0.3, 0.2, up)
        base = 1 - C.smoothstep(-0.9, -0.4, up)
        col = C.lerp3(pal.membrane, pal.side, ray * 0.8)
        col = C.lerp3(col, pal.glow * 0.8, rim * 0.8)
        col = C.lerp3(col, pal.side, base)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip(rim * edge_glow + (1 - ray) * 0.05 * edge_glow * (1 - base), 0, 1)[:, None]
        s.disp[idx] = 0.002 * k * ray
        s.rough[idx] = 0.35 - 0.1 * rim
        H.family(s, idx, membrane=1.0 - ray * 0.5, chitin=ray * 0.5, flow=along * part.radii[axis])
    return style


def molten_sac(pal, k, cells=30, seed=0, glow=1.0):
    """A molten belly sac: a dark chilled crust broken into floes by bright molten cracks of mixed width."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = _unit(L)
        rng = np.random.default_rng(seed)
        seeds = C.fibonacci_sphere(cells, seed=seed) * rng.uniform(0.8, 1.2, (cells, 1))
        e = C.voronoi_edge(u, seeds / np.linalg.norm(seeds, axis=1, keepdims=True))
        width = 0.03 + 0.04 * (0.5 + 0.5 * C.value_noise(u, 3.0, seed + 1))
        crack = 1 - C.smoothstep(0.0, width, e)
        under = C.smoothstep(0.3, -0.6, u[:, 2])
        hot = np.clip(crack + under * 0.6, 0, 1)
        crust = C.chitin_gradient(s.n[idx], pal.side, pal.dark, pal.belly)
        s.albedo[idx] = C.lerp3(crust, pal.glow * 0.7, hot)
        s.emit[idx] = pal.glow[None, :] * (hot * glow)[:, None]
        s.disp[idx] = -0.004 * k * crack
        s.rough[idx] = 0.85 - 0.4 * hot
        H.family(s, idx, rock=1.0 - hot, flow=u[:, 2] * part.radii[2])
    return style


# ---------------------------------------------------------------------------------------------------------------
# Posing: scale keys only in the clips that need them
# ---------------------------------------------------------------------------------------------------------------

class ClipPoser(B.ScalePoser):
    """A ScalePoser whose keyed scale bones are chosen per clip (`use(clip)`), so clips that never scale a bone
    leave it without scale curves and Unity can set its scale from code (hide a shed part) without a fight."""

    def __init__(self, arm, scale_bones=(), clip_scales=None):
        super().__init__(arm, scale_bones)
        self.all_scale = set(scale_bones)
        # None: every clip keys every scale bone (1 where it does not animate it), so a clip never inherits the
        # scale the previous clip left behind (Blender keeps an unkeyed channel; Unity writes defaults).
        self.clip_scales = clip_scales

    def use(self, clip):
        names = self.all_scale if self.clip_scales is None else self.clip_scales.get(clip, ())
        self.scale_bones = {n for n in names if n in self.all_scale}


def key_clips(poser, clips):
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(poser)
        if isinstance(poser, ClipPoser):
            poser.use(clip)
        actions[clip] = poser.action(clip, frames, spec, loop=loop)
    return actions


def build_lod(name, lod_name, asset, builder, bones, clips, full_poser, actions_full, lod_tris, drop, aims,
              rigid=(), scale_bones=(), clip_scales=None):
    """common.build_lod with rigid parts (a shed prism must collapse onto its own bone only) and per-clip scale keys."""
    full = {b[0]: b for b in bones}
    lod_bones = []
    for bname, head, tail, parent, deform in bones:
        if bname in drop:
            continue
        if bname in aims:
            first, last = aims[bname]
            head, tail = full[first][1], full[last][2]
        while parent in drop:
            parent = drop[parent]
        lod_bones.append((bname, head, tail, parent, deform))
    lod_mesh = C.decimate(asset.low.data.copy(), lod_tris, lod_name, symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(lod_name, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    arm = C.build_armature(lod_name + "_Rig", lod_bones)

    def remap(b):
        while b in drop:
            b = drop[b]
        return b
    saved = [(p, list(p.chain), p.bone) for p in builder.parts]
    for p in builder.parts:
        p.chain = list(dict.fromkeys(remap(c) for c in p.chain))
        p.bone = p.chain[0] if p.chain else p.bone
    C.skin(lod, arm, builder, rigid=rigid, blend=1)
    for p, chain, bone in saved:
        p.chain, p.bone = chain, bone
    poser = ClipPoser(arm, [b for b in scale_bones if b not in drop], clip_scales)
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(full_poser)
        poser.use(clip)
        actions[clip] = C.lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop, aims)
    arm.animation_data.action = actions.get("Idle") or next(iter(actions.values()))
    # export_rig guards every other action: with no bones dropped, the full rig's actions would also resolve on the
    # LOD rig and be exported as extra takes.
    K2.export_rig(os.path.join(C.EXPORTS, name, lod_name + ".fbx"), arm, lod, actions)
    return arm, lod, actions


# ---------------------------------------------------------------------------------------------------------------
# Legs
# ---------------------------------------------------------------------------------------------------------------

def gp(p, sx):
    return C.G(p[0] * sx, p[1], p[2])


def limbs(table, pole=(0.35, 1.0, 0.0), prefix="Leg"):
    """table: [(hip (x, up, fwd) right side, foot right side, (l1, l2[, l3]))] -> {(side, i): Limb}."""
    out = {}
    for side, sx in SIDES:
        for i, (hip, foot, lengths) in enumerate(table):
            segs = ("Upper", "Lower", "Foot")[:len(lengths)]
            names = [f"{prefix}_{side}{i + 1}_{s}" for s in segs]
            out[(side, i + 1)] = C.Limb(names, gp(hip, sx), gp(foot, sx), lengths, gp(pole, sx))
    return out


def place_legs(poser, specs, legs, feet, body="Body"):
    pose = poser.solve(specs)
    move = pose[body] @ poser.rest[body].inverted()
    for key, lg in legs.items():
        specs.update(lg.pose(poser, move @ lg.root, feet[key], move.to_3x3() @ lg.pole))
    return specs


def gait_feet(legs, t, phases, stance, stride, lift):
    feet = {}
    for key, lg in legs.items():
        fwd, up = C.step_curve((t + phases[key]) % 1.0, stance, stride, lift)
        feet[key] = lg.foot + Vector((0, -fwd, up))
    return feet


def splay_feet(poser, specs, legs, fall, twitch=0.0, out=1.2, body="Body", kick_keys=()):
    """Death: feet slide out flat as the body settles."""
    pose = poser.solve(specs)
    move = pose[body] @ poser.rest[body].inverted()
    for key, lg in legs.items():
        hip = move @ lg.root
        d = lg.foot - lg.root
        d = Vector((d.x * out, d.y * 1.05, 0.0))
        kick = Vector((0, 0, 0.02 * twitch)) if key in kick_keys else Vector()
        foot = lg.foot.lerp(Vector((hip.x + d.x, hip.y + d.y, 0.0)), fall) + kick
        specs.update(lg.pose(poser, hip, foot, move.to_3x3() @ lg.pole))
    return specs


# ---------------------------------------------------------------------------------------------------------------
# The pipeline of a common enemy
# ---------------------------------------------------------------------------------------------------------------

def run_creature(name, builder, bone_list, clips, *, tris=3900, lod_tris=1700, families=("chitin", "membrane", "crystal"),
                 game_scale=0.8, rigid=(), blend=2, lod_drop=None, lod_aims=None, scale_bones=(), clip_scales=None,
                 extra=None, voxel=0.0034, extrusion=0.012, ray=0.04, ao_distance=0.08, emission_strength=2.5,
                 specular=0.35, report=None, previews=None, k=1.0, mirror=True):
    """Builds, rigs, animates, exports and renders one common enemy. Returns (asset, arm, actions, lod).
    `mirror` False (round 49D, an asymmetric body): whole-body UVs and a plain decimation."""
    C.reset_scene()
    out = C.preview_dir()
    asset = C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=1024, ao_distance=ao_distance,
                               extrusion=extrusion, ray=ray, emission_strength=emission_strength,
                               extra=extra, specular=specular, mirror=mirror, symmetric=mirror, k=k, families=families,
                               low_size=512)
    arm = C.build_armature(name + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=rigid, blend=blend)
    poser = ClipPoser(arm, scale_bones, clip_scales)
    actions = key_clips(poser, clips)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, name, name + ".fbx"), [arm, asset.low], animated=True)
    lod = build_lod(name, name + "_LOD1", asset, builder, bone_list, clips, poser, actions, lod_tris,
                    lod_drop or {}, lod_aims or {}, rigid=rigid, scale_bones=scale_bones, clip_scales=clip_scales)
    data = {"asset": name, "game_tris": asset.low_tris, "source_tris": asset.high_tris, "bones": len(arm.data.bones),
            "texture_size": 1024, "low_texture_size": 512, "maps": ["Albedo", "Normal", "Emission", "Mask"],
            "uv": asset.uv_stats, "detail": asset.details,
            "actions": {n: clips[n](poser)[0] for n in clips}, "unity_scale": game_scale,
            "scale_keyed_bones": list(scale_bones),
            "lod1": {"fbx": name + "_LOD1.fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}}
    data.update(report or {})
    C.write_report(name, data)
    C.save_blend(name)
    if C.cli_args().get("norender") or previews is None:
        return asset, arm, actions, lod
    K2.enemy_previews(name.lower(), arm, asset.low, actions, lod, out, asset, game_scale=game_scale, **previews)
    return asset, arm, actions, lod
