"""Round 45: shared helpers for the close-up detail of the bosses (Remco: "restyle the detail features on all the
bosses: claws, teeth, eyes"; "the ant queen looks plain af from close up").

The technique is gravemaw_maw.py's (round 38): the detail is built as extra skinned meshes with their own material
and maps, exported in the boss's FBX next to the body and dressed in Unity as BlenderArtEntry.ExtraParts. Per boss
there are two parts:
- `<Boss>_Face`: eyes (compound and simple, with wet corneas, iris/pupil or facet patterns, dark socket rims and a
  faint glow), teeth, mandible serrations and mouth parts.
- `<Boss>_Claws`: claw tips, talons, spurs and blade serrations (keratin: dark root to a pale or glowing tip).
Both have a LOD1 version (`_LOD1`) in the boss's LOD1 FBX, skinned to the LOD rig; parts flagged lod=False (small
teeth) are left out of it.

Weights: every detail part follows the body at its anchor point (the weights of the body surface there, interpolated
over the nearest body face), or a fixed bone. The body already follows every clip, so the details do too, rigidly
(teeth and claws never bend). Bones, clips, timings and events are never touched: the existing actions are exported
again as they are (boss_mech_kit.export_rig, which guards every other action out of the FBX).

Coordinates are Blender's (Z up, creatures face -Y); use common.G(x, up, forward) for game terms.

API (kept stable once HiveQueen was done; other scripts may import it read-only):
  use_gpu()                                     Cycles bakes on the GPU when there is one (CPU otherwise).
  Boss(name)                                    opens ArtSource/Blend/<name>.blend: .arm .mesh .lod_arm .lod_mesh
    .head(bone) .tail(bone) .along(bone, t)     rest positions of a bone
    .hit(origin, direction) / .nearest(p)       rest body surface: (point, normal) or None
  Palettes: Eye(...), Keratin(...), Tooth(...), Flesh(...)
  Styles: eye_style, compound_eye_style, socket_style, keratin_style, tooth_style, flesh_style
  Shapes (all take a PartBuilder; every part gets .anchor and optional .bone_fixed / .lod):
    add_eye, add_compound_eye, add_tooth, tooth_row, add_claw, add_serration, add_palp, add_ring_teeth
  build_part(boss, part_name, builder, tris, lod_tris, voxel)  -> (obj, lod_obj, facts)
  export(boss, extras, lod_extras)              FBX + LOD1 FBX with the existing clips
  backup(name)                                  copy of the export to _backup_r45/<name> (once)
  qa_* helpers                                  before/after renders in ArtSource/Previews/QA (r45_<boss>_*.png)
  update_report(boss, facts)                    the report's "r45_detail" block
"""

import json
import math
import os
import shutil

import bmesh
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import boss_mech_kit as M
import common as C
import deep_kit as DK

TEX, LOW_TEX = 1024, 512
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
BACKUP = os.path.join(C.EXPORTS, "_backup_r45")
FAMILIES = ("chitin", "membrane", "crystal")
REPORT_KEY = "r45_detail"


# ---------------------------------------------------------------------------------------------------------------
# GPU bakes
# ---------------------------------------------------------------------------------------------------------------

_cpu_cycles = C._cycles


def use_gpu():
    """Bakes and AO on the GPU (OptiX or CUDA) when the machine has one; the shared bake code sets CPU every time,
    so this wraps it. Results match the CPU bake up to sampling noise."""
    prefs = bpy.context.preferences.addons["cycles"].preferences
    found = None
    for kind in ("OPTIX", "CUDA", "HIP", "ONEAPI", "METAL"):
        try:
            prefs.compute_device_type = kind
            prefs.refresh_devices()
        except Exception:
            continue
        devs = [d for d in prefs.devices if d.type == kind]
        if devs:
            for d in prefs.devices:
                d.use = d.type == kind
            found = kind
            break
    if found is None:
        C.log("detail_kit: no GPU, CPU bakes")
        return False

    def gpu_cycles(samples):
        _cpu_cycles(samples)
        bpy.context.scene.cycles.device = "GPU"
    C._cycles = gpu_cycles
    C.log("detail_kit: GPU bakes on", found)
    return True


# ---------------------------------------------------------------------------------------------------------------
# The boss as saved by its build script
# ---------------------------------------------------------------------------------------------------------------

class Boss:
    """The boss's saved .blend: rig, body, LOD rig and LOD body, plus rest-pose lookups on the body surface."""

    def __init__(self, name, part_names=()):
        self.name = name
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
        self.lod_mesh = bpy.data.objects.get(name + "_LOD1")
        self.folder = os.path.join(C.EXPORTS, name)
        # A rerun starts from the body alone: earlier detail objects (and their meshes) go.
        for pn in part_names:
            for suffix in ("", "_LOD1", "_High", "_Parts", "_Block"):
                o = bpy.data.objects.get(f"{name}_{pn}{suffix}")
                if o is not None:
                    bpy.data.objects.remove(o)
        # QA renders show the full model; the LOD1 rig and meshes stay out of them (qa_lod shows them on their own).
        for o in bpy.data.objects:
            if (self.lod_arm is not None and o.parent == self.lod_arm) or o == self.lod_arm:
                o.hide_render = True
        for o in self.arm, self.mesh:
            assert o.matrix_world.to_translation().length < 1e-4, "detail_kit assumes the rig and body at the origin"
        self._bvh = None

    # Bones (rest, armature space = world space here)
    def head(self, bone):
        return self.arm.data.bones[bone].head_local.copy()

    def tail(self, bone):
        return self.arm.data.bones[bone].tail_local.copy()

    def along(self, bone, t):
        return self.head(bone).lerp(self.tail(bone), t)

    def axis(self, bone):
        return (self.tail(bone) - self.head(bone)).normalized()

    def bvh(self):
        if self._bvh is None:
            me = self.mesh.data
            verts = [v.co.copy() for v in me.vertices]
            polys = [list(p.vertices) for p in me.polygons]
            self._bvh = BVHTree.FromPolygons(verts, polys)
        return self._bvh

    def hit(self, origin, direction, distance=10.0):
        """First body surface hit along a ray (rest pose): (point, outward normal) or None."""
        loc, nor, _, _ = self.bvh().ray_cast(Vector(origin), Vector(direction).normalized(), distance)
        return (loc, nor) if loc is not None else None

    def nearest(self, p):
        loc, nor, _, _ = self.bvh().find_nearest(Vector(p))
        return loc, nor

    def onto(self, origin, direction, sink=0.0):
        """Point on the body hit from `origin` towards `direction`, moved `sink` metres into it, and the normal."""
        h = self.hit(origin, direction)
        if h is None:
            raise RuntimeError(f"{self.name}: no body surface from {tuple(origin)} towards {tuple(direction)}")
        p, n = h
        return p - n * sink, n

    def out(self, inside, direction, sink=0.0):
        """Where a ray from a point inside the body leaves it (the skin over that point) and the outward normal.
        Robust where an outside ray would hit a hood, a horn or a limb first."""
        d = Vector(direction).normalized()
        loc, nor, _, _ = self.bvh().ray_cast(Vector(inside), d, 10.0)
        if loc is None:
            raise RuntimeError(f"{self.name}: no skin from {tuple(inside)} towards {tuple(d)}")
        if nor.dot(d) < 0:
            nor = -nor
        return loc - nor * sink, nor

    def tris(self):
        return C.tri_count(self.mesh), C.tri_count(self.lod_mesh) if self.lod_mesh else 0


# ---------------------------------------------------------------------------------------------------------------
# Palettes
# ---------------------------------------------------------------------------------------------------------------

def _c(v):
    return np.array(v, dtype=float)


class Eye:
    """Wet eye colours (stored sRGB like the PNGs): sclera/lens body, iris, iris rim, pupil, glow."""

    def __init__(self, lens=(0.08, 0.02, 0.02), iris=(1.0, 0.45, 0.2), rim=(0.25, 0.03, 0.02), pupil=(0.01, 0.0, 0.0),
                 glow=(1.0, 0.5, 0.25), vein=(0.4, 0.04, 0.04)):
        self.lens, self.iris, self.rim, self.pupil, self.glow, self.vein = map(_c, (lens, iris, rim, pupil, glow, vein))


class Keratin:
    """Claw and tooth material: root (where it grows from the body), body, tip, optional glow towards the tip."""

    def __init__(self, root=(0.05, 0.02, 0.02), body=(0.2, 0.09, 0.06), tip=(0.85, 0.78, 0.66), glow=None,
                 stain=(0.25, 0.04, 0.04)):
        self.root, self.body, self.tip, self.stain = map(_c, (root, body, tip, stain))
        self.glow = _c(glow) if glow is not None else None


class Flesh:
    """Gums, throat, sockets: dark wet flesh, a lighter crest colour and the deep glow."""

    def __init__(self, dark=(0.06, 0.01, 0.015), flesh=(0.3, 0.05, 0.08), glow=(1.0, 0.35, 0.45)):
        self.dark, self.flesh, self.glow = map(_c, (dark, flesh, glow))


# ---------------------------------------------------------------------------------------------------------------
# Styles (vertex styles on the fused source: see common.Surface)
# ---------------------------------------------------------------------------------------------------------------

def _unit_local(part, p):
    L = C.local_points(part, p)
    return L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-9)


def eye_style(pal, pupil="slit", iris_size=0.48, pupil_size=0.16, glow=0.9, seed=0):
    """A big wet eye seen through a glossy cornea: a near-black lens, a glowing iris ring (fine radial fibres, a hot
    band round the pupil, a dark limbal rim), a black pupil (vertical `slit`, horizontal `bar`, `round`, or `cross`
    for a four-lobed alien pupil) and dark veins where it meets the socket. Local +z looks out of the eye.
    The light comes from the emission; the albedo stays dark so the eye reads as wet and deep, not as a bead."""
    def style(s, idx, part):
        u = _unit_local(part, s.p[idx])
        theta = np.arccos(np.clip(u[:, 2], -1, 1)) / (math.pi / 2)      # 0 centre .. 1 at the equator
        ang = np.arctan2(u[:, 1], u[:, 0])
        fib = 0.8 + 0.2 * np.sin(ang * 38 + 3 * C.value_noise(u * 3, 2, seed))
        iris = 1 - C.smoothstep(iris_size - 0.06, iris_size + 0.02, theta)
        limbus = C.smoothstep(iris_size - 0.14, iris_size - 0.02, theta) * iris
        x, y = u[:, 0], u[:, 1]
        if pupil == "slit":
            pd = np.abs(x) / pupil_size * 3.0 + (y / (iris_size * 1.25)) ** 2
        elif pupil == "bar":
            pd = np.abs(y) / pupil_size * 3.0 + (x / (iris_size * 1.25)) ** 2
        elif pupil == "cross":
            pd = np.minimum(np.abs(x), np.abs(y)) / pupil_size * 3.0 + (theta / (iris_size * 0.95)) ** 2
        else:
            pd = theta / pupil_size
        pup = 1 - C.smoothstep(0.85, 1.05, pd)
        hot = (1 - C.smoothstep(0.0, 0.7, np.abs(pd - 1.4))) * iris * (1 - pup)
        veins = C.smoothstep(0.7, 1.0, theta) * C.smoothstep(0.3, 0.8, C.value_noise(u, 14, seed + 1))
        col = C.lerp3(pal.lens, pal.vein, veins * 0.7)
        iris_col = pal.iris * 0.35 * fib[:, None]
        col = C.lerp3(col, iris_col, iris * (1 - limbus))
        col = C.lerp3(col, pal.rim * 0.5, limbus)
        col = C.lerp3(col, pal.pupil, pup)
        s.albedo[idx] = col
        e = glow * (iris * (1 - limbus * 0.85) * (0.35 + 0.45 * fib) + 0.7 * hot) * (1 - pup)
        s.emit[idx] = np.clip(pal.glow[None, :] * e[:, None] + pal.iris[None, :] * (0.4 * glow * hot)[:, None], 0, 1)
        # Wet cornea: near mirror smooth over the whole eye; a touch rougher where it meets the lid.
        s.rough[idx] = 0.04 + 0.1 * C.smoothstep(0.85, 1.05, theta)
        s.disp[idx] = 0.0004 * fib * iris - 0.0006 * pup
        s.fam[idx] = 0.0
    return style


def compound_eye_style(pal, facets=220, glow=0.55, seed=0):
    """An insect compound eye: hundreds of domed facets (dark borders, each facet with a hot fleck), a dark
    pseudo-pupil band that seems to follow the viewer, glossy all over."""
    seeds = C.fibonacci_sphere(facets, seed)
    seeds = seeds / np.linalg.norm(seeds, axis=1, keepdims=True)

    def style(s, idx, part):
        u = _unit_local(part, s.p[idx])
        edge = C.voronoi_edge(u, seeds) / (2.4 / math.sqrt(facets))
        border = 1 - C.smoothstep(0.03, 0.16, edge)
        centre = C.smoothstep(0.25, 0.6, edge)
        theta = np.arccos(np.clip(u[:, 2], -1, 1)) / (math.pi / 2)
        pseudo = 1 - C.smoothstep(0.12, 0.3, theta)
        mott = 0.5 + 0.5 * C.value_noise(u, 4, seed + 2)
        col = C.lerp3(pal.lens, pal.iris * (0.35 + 0.3 * mott)[:, None], centre)
        col = C.lerp3(col, pal.rim * 0.4, border)
        col = C.lerp3(col, pal.pupil, pseudo * 0.85)
        s.albedo[idx] = col
        e = glow * (0.15 + 0.85 * centre) * (0.55 + 0.45 * mott) * (1 - 0.8 * pseudo) * (1 - border)
        s.emit[idx] = np.clip(pal.glow[None, :] * e[:, None], 0, 1)
        s.rough[idx] = 0.06 + 0.2 * border
        s.disp[idx] = -0.0012 * border
        s.fam[idx] = 0.0
    return style


def socket_style(flesh, seed=0, lid=None):
    """The lid ring an eye sits in: armour-coloured wrinkled flesh (`lid`, default flesh.flesh) darkening into the
    crease against the eye (the baked AO deepens it); satin, not plastic."""
    lid = _c(lid) if lid is not None else flesh.flesh

    def style(s, idx, part):
        p = s.p[idx]
        wr = 0.5 + 0.5 * np.sin(C.value_noise(p, 90, seed) * 7)
        mott = 0.5 + 0.5 * C.value_noise(p, 40, seed + 1)
        s.albedo[idx] = C.lerp3(flesh.dark, lid, 0.35 + 0.45 * mott) * (0.8 + 0.3 * wr)[:, None]
        s.emit[idx] = 0.0
        s.rough[idx] = 0.4 + 0.2 * wr
        s.disp[idx] = 0.0006 * wr
        s.fam[idx] = (0.6, 0.0, 0.0, 0.0)
    return style


def keratin_style(pal, glow=0.0, glow_from=0.75, seed=0, wet=0.0):
    """Claw/talon keratin: dark at the root, a streaked mid, pale (or glowing) worn tip; growth rings across it,
    fine fibres along it, chips near the tip. Satin, not glass: roughness 0.3-0.55 (`wet` lowers it)."""
    def style(s, idx, part):
        p = s.p[idx]
        z, radius, dist = C.tube_param(part, p)
        L = C.local_points(part, p)
        fib = 0.5 + 0.5 * C.value_noise(np.c_[L[:, 0] * 3, L[:, 1] * 3, z * 0.4], 7, seed)
        rings = 1 - C.smoothstep(0.0, 0.06, np.abs(((z * 9) % 1.0) - 0.5) - 0.4)
        col = C.lerp3(pal.root, pal.body, C.smoothstep(0.0, 0.35, z))
        col = C.lerp3(col, pal.tip, C.smoothstep(0.45, 0.95, z))
        col = col * (0.8 + 0.3 * fib)[:, None] * (1 - 0.12 * rings)[:, None]
        chip = C.smoothstep(0.55, 0.75, C.value_noise(p, 40, seed + 3)) * C.smoothstep(0.6, 0.9, z)
        col = C.lerp3(col, pal.tip * 1.1, chip * 0.5)
        stain = (1 - C.smoothstep(0.0, 0.12, z)) * (0.5 + 0.5 * fib)
        col = C.lerp3(col, pal.stain, stain * 0.6)
        s.albedo[idx] = np.clip(col, 0, 1)
        if glow and pal.glow is not None:
            g = C.smoothstep(glow_from, 1.0, z) * glow
            s.emit[idx] = np.clip(pal.glow[None, :] * g[:, None], 0, 1)
        else:
            s.emit[idx] = 0.0
        s.rough[idx] = (0.32 + 0.22 * fib + 0.1 * chip) * (1 - 0.5 * wet)
        s.disp[idx] = -0.0006 * rings - 0.0008 * chip
        s.fam[idx] = (0.5, 0.0, 0.0, 0.0)
        s.flow[idx] = z * C.axis_length(part)
    return style


def tooth_style(pal, glow=0.0, seed=0):
    """Hooked tooth (gravemaw_maw's look with a palette): stained root, translucent-looking band near the root,
    enamel body, pale tip; striations, growth bands, dark cracks; wet but streaky."""
    def style(s, idx, part):
        p = s.p[idx]
        z, radius, dist = C.tube_param(part, p)
        L = C.local_points(part, p)
        striae = 0.5 + 0.5 * C.value_noise(np.c_[L[:, 0] * 3, L[:, 1] * 3, z * 0.5], 6, seed)
        bands = 1 - C.smoothstep(0.0, 0.05, np.abs(((z * 7) % 1.0) - 0.5) - 0.42)
        col = C.lerp3(C.lerp3(pal.root, pal.body, C.smoothstep(0.0, 0.18, z)), pal.tip, C.smoothstep(0.2, 0.6, z))
        col = col * (0.86 + 0.18 * striae)[:, None] * (1 - 0.1 * bands)[:, None]
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 120, seed + 3)))
        crack *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 25, seed + 4))
        col = C.lerp3(col, pal.root * 0.6, crack * 0.5)
        stain = (1 - C.smoothstep(0.0, 0.14, z)) * (0.6 + 0.4 * striae)
        col = C.lerp3(col, pal.stain, stain * 0.7)
        s.albedo[idx] = np.clip(col, 0, 1)
        root_band = C.smoothstep(0.02, 0.1, z) * (1 - C.smoothstep(0.18, 0.4, z))
        g = pal.glow if pal.glow is not None else pal.tip
        s.emit[idx] = np.clip(g[None, :] * (glow * (0.05 + 0.45 * root_band) * (1 - 0.7 * crack))[:, None], 0, 1)
        s.rough[idx] = 0.24 + 0.2 * striae + 0.18 * crack + 0.1 * C.smoothstep(0.85, 1.0, z)
        s.disp[idx] = -0.0006 * crack - 0.0004 * bands
        s.fam[idx] = 0.0
        s.flow[idx] = z * C.axis_length(part)
    return style


def flesh_style(pal, glow=0.6, glow_axis=None, seed=0):
    """Gums, inner mouth, throat: dark maroon-wet flesh with folds; glows towards `glow_axis` = (point, direction,
    depth) (deeper = hotter), so the mouth reads as a lit gullet."""
    def style(s, idx, part):
        p = s.p[idx]
        mott = 0.5 + 0.5 * C.value_noise(p, 60, seed)
        folds = 0.5 + 0.5 * np.sin(C.value_noise(p, 25, seed + 1) * 9)
        col = C.lerp3(pal.dark, pal.flesh, mott * 0.7 + 0.3 * folds)
        depth = np.zeros(len(idx))
        if glow_axis is not None:
            o, d, span = glow_axis
            depth = np.clip(((p - np.array(o)) @ np.array(d)) / span, 0, 1)
        s.albedo[idx] = C.lerp3(col, pal.glow * 0.5, depth * 0.4)
        s.emit[idx] = np.clip(pal.glow[None, :] * (glow * (0.05 + depth ** 1.5) * (0.6 + 0.4 * folds))[:, None], 0, 1)
        s.rough[idx] = 0.15 + 0.2 * (1 - mott)
        s.disp[idx] = 0.0008 * folds
        s.fam[idx] = 0.0
    return style


# ---------------------------------------------------------------------------------------------------------------
# Shapes. Every helper tags its parts with .anchor (the rest point whose body weights the part takes), optional
# .bone_fixed (a bone the part follows 100%) and .lod (False: left out of LOD1).
# ---------------------------------------------------------------------------------------------------------------

def _tag(parts, anchor, bone=None, lod=True):
    if not isinstance(parts, (list, tuple)):
        parts = [parts]
    for p in parts:
        p.anchor = Vector(anchor)
        p.bone_fixed = bone
        p.lod = lod
    return parts


def frame_rot(normal, up_hint=Vector((0, 0, 1))):
    """Rotation whose local z is `normal` and local y as close to `up_hint` as possible (eye pupils stand up)."""
    z = Vector(normal).normalized()
    hint = Vector(up_hint)
    if abs(z.dot(hint.normalized())) > 0.98:
        hint = Vector((0, -1, 0))
    x = hint.cross(z).normalized()
    y = z.cross(x).normalized()
    from mathutils import Matrix
    return Matrix((x, y, z)).transposed()


def add_eye(b, centre, normal, radius, style, socket_style_=None, squash=(1.0, 1.0, 0.85), socket=0.22,
            up=Vector((0, 0, 1)), anchor=None, bone=None, lod=True, segments=(28, 14), socket_shift=0.12,
            socket_size=1.02):
    """A wet eye (ellipsoid, its pupil standing along `up`) sunk into a socket ring of dark flesh. `centre` is the
    eye's centre; it bulges `radius * squash.z` out along `normal`. The ring sits `socket_shift` (in units of the
    eye's depth) out along the normal, `socket_size` times the eye's radius: a negative shift and a smaller size make
    a cup that holds the eyeball from behind (stalk eyes)."""
    rot = frame_rot(normal, up)
    rx, ry, rz = (radius * squash[0], radius * squash[1], radius * squash[2])
    eye = b.ellipsoid("Eye", centre, (rx, ry, rz), rot, style=style, segments=segments)
    parts = [eye]
    if socket_style_ is not None and socket > 0:
        n = Vector(normal).normalized()
        rim_c = Vector(centre) + n * (rz * socket_shift)
        rim = b.ring("Socket", rim_c, n, max(rx, ry) * socket_size, radius * socket, arcs=3, sides=8, style=socket_style_,
                     ref=rot @ Vector((1, 0, 0)), profile=lambda th: 1.0 + 0.25 * math.cos(2 * th))
        parts += rim
    return _tag(parts, anchor or centre, bone, lod)


def add_compound_eye(b, centre, normal, radii, style, socket_style_=None, up=Vector((0, 0, 1)), anchor=None,
                     bone=None, lod=True, socket=0.12, deform=None):
    """A kidney-like compound eye: an ellipsoid (`radii` across, along `up`, out) with a socket ring."""
    rot = frame_rot(normal, up)
    eye = b.ellipsoid("CompoundEye", centre, radii, rot, style=style, segments=(36, 18), deform=deform)
    parts = [eye]
    if socket_style_ is not None and socket > 0:
        n = Vector(normal).normalized()
        rim = b.ring("Socket", Vector(centre) + n * radii[2] * 0.05, n, max(radii[0], radii[1]) * 0.98,
                     min(radii[0], radii[1]) * socket, arcs=3, sides=8, style=socket_style_,
                     ref=rot @ Vector((1, 0, 0)))
        parts += rim
    return _tag(parts, anchor or centre, bone, lod)


def tooth_profile(t):
    """Thick root tapering fast into a needle (gravemaw_maw)."""
    return (1 - t) ** 1.15 * (1.0 + 0.3 * math.exp(-((t - 0.1) / 0.12) ** 2))


def claw_profile(t):
    """A claw: broad flattened root, long even taper, sharp point."""
    return (1 - t) ** 0.9 * (1.0 + 0.15 * math.exp(-((t - 0.05) / 0.1) ** 2))


def add_tooth(b, base, direction, radius, length, style, hook=None, hook_amount=0.32, anchor=None, bone=None,
              lod=True, sides=7, name="Tooth"):
    hook = Vector(hook) if hook is not None else Vector((0, 0, 0))
    part = b.shard(name, base, direction, radius, length, sides=sides, flute=0.14, profile=tooth_profile,
                   bend=hook * length * hook_amount, rings=9, style=style)
    return _tag(part, anchor or base, bone, lod)


def tooth_row(b, bases, directions, radius, length, style, hooks=None, anchor_fn=None, bone=None, lod=True, jitter=0.15,
              seed=0, name="Tooth"):
    """Teeth on a row of base points; sizes vary a little (deterministic) so the row looks grown, not stamped."""
    out = []
    for i, (p, d) in enumerate(zip(bases, directions)):
        j = _hash01(i * 1.7 + seed, abs(p[0]) * 13.1)
        k = 1 - jitter + 2 * jitter * j
        h = hooks[i] if hooks is not None else None
        out += add_tooth(b, p, d, radius * k, length * k, style, hook=h, anchor=anchor_fn(p) if anchor_fn else p,
                         bone=bone, lod=lod, name=name)
    return out


def add_claw(b, base, direction, radius, length, style, hook, hook_amount=0.45, flatten=None, anchor=None, bone=None,
             lod=True, sides=8, name="Claw"):
    """A hooked claw: `hook` is the direction the point curls towards (bend grows with t^2)."""
    part = b.shard(name, base, direction, radius, length, sides=sides, flute=0.1, profile=claw_profile,
                   bend=Vector(hook).normalized() * length * hook_amount, rings=10, style=style)
    return _tag(part, anchor or base, bone, lod)


def add_serration(b, points, out_dirs, radius, length, style, hook=None, anchor=None, bone=None, lod=True,
                  name="Serration"):
    """Saw teeth along an edge (blades, mandibles): small hooked shards from `points` towards `out_dirs`."""
    out = []
    for i, (p, d) in enumerate(zip(points, out_dirs)):
        k = 0.85 + 0.3 * _hash01(i * 2.3, abs(p[0]) * 7.7)
        out += add_tooth(b, p, d, radius * k, length * k, style, hook=hook, hook_amount=0.25, anchor=anchor or p,
                         bone=bone, lod=lod, sides=6, name=name)
    return out


def add_palp(b, points, radii, style, anchor=None, bone=None, lod=True, name="Palp"):
    """A segmented feeler/mouth palp: a thin tube through points."""
    part = b.tube(name, points, radii, sides=8, style=style)
    return _tag(part, anchor or points[0], bone, lod)


def add_ring_teeth(b, centre, axis, radius, count, length, tooth_r, style, inward=0.65, offset=0.0, anchor=None,
                   bone=None, lod=True, name="RingTooth", skip=None):
    """A lamprey ring: `count` teeth on a circle round `axis` pointing inward and down the throat."""
    a = Vector(axis).normalized()
    ref = a.orthogonal().normalized()
    if abs(a.z) < 0.9:
        ref = (Vector((0, 0, 1)) - a * a.z).normalized()
    side = a.cross(ref)
    out = []
    for k in range(count):
        phi = offset + 2 * math.pi * k / count
        if skip and skip(phi):
            continue
        r = ref * math.cos(phi) + side * math.sin(phi)
        base = Vector(centre) + r * radius
        d = (-r * inward + a * (1 - inward)).normalized()
        j = _hash01(round(abs(math.sin(phi)), 3), radius)
        out += add_tooth(b, base, d, tooth_r * (0.9 + 0.2 * j), length * (0.85 + 0.3 * j), style, hook=a,
                         anchor=anchor or base, bone=bone, lod=lod, name=name)
    return out


def curve_points(points, n=40, samples=8):
    """`n` evenly indexed points of the Catmull-Rom curve through `points` (as the build scripts' tubes use)."""
    pts = C.catmull_rom([Vector(p) for p in points], samples)
    return [pts[int(round(i / (n - 1) * (len(pts) - 1)))] for i in range(n)]


def inner_teeth(boss, b, curve, mouth_from, mouth_to, rows, style, bone, max_off=0.2, rake=0.35, hook_amount=0.4,
                lod=False, name="Tooth", across=None):
    """Teeth on the inner face of a jaw/petal/mandible tube (`curve` = its axis points): for every (t, radius,
    length, offset) in `rows` a ray from the mouth axis (`mouth_from` -> `mouth_to`) out to the tube finds its inner
    face; the tooth points back in at the axis, raked towards the throat. `offset` shifts the row sideways along
    `across` (a direction, e.g. towards the neighbouring jaw). Returns the number placed."""
    axis = (Vector(mouth_to) - Vector(mouth_from)).normalized()
    placed = 0
    for t, radius, length, offset in rows:
        c = curve[int(t * (len(curve) - 1))]
        if offset and across is not None:
            c = c + Vector(across).normalized() * offset
        a = Vector(mouth_from) + axis * (c - Vector(mouth_from)).dot(axis)
        hit = boss.hit(a + (c - a) * 0.1, c - a)
        if hit is None or hit[1].dot(c - a) > 0 or (hit[0] - c).length > max_off:
            C.log(name, "skipped at", round(t, 2), bone)
            continue
        base, n = hit
        d = (n * (1 - rake) + (-axis) * rake).normalized()
        add_tooth(b, base - n * radius * 0.5, d, radius, length, style, hook=-axis, hook_amount=hook_amount,
                  bone=bone, lod=lod, name=name)
        placed += 1
    return placed


def edge_saw(boss, b, curve, ts, inward, radius, length, style, bone, rake=0.45, max_off=0.3, lod=False, name="Saw",
             start=0.6):
    """Saw teeth on the inner edge of a blade (`curve` its axis, `inward` the side the edge faces, e.g. -x for a
    blade's cutting edge towards the midline), raked towards the blade's point."""
    placed = 0
    for t in ts:
        j = max(1, min(len(curve) - 2, int(t * (len(curve) - 1))))
        c = curve[j]
        tangent = (curve[j + 1] - curve[j - 1]).normalized()
        w = Vector(inward)
        w = (w - tangent * w.dot(tangent)).normalized()
        hit = boss.hit(c + w * start, -w)
        if hit is None or (hit[0] - c).length > max_off:
            C.log(name, "skipped at", round(t, 2), bone)
            continue
        p, n = hit
        d = (n * (1 - rake) + tangent * rake).normalized()
        add_tooth(b, p - n * radius * 0.5, d, radius, length, style, hook=tangent, hook_amount=0.3, bone=bone,
                  lod=lod, sides=6, name=name)
        placed += 1
    return placed


def _hash01(*xs):
    h = 0.0
    for i, x in enumerate(xs):
        h += math.sin(x * (12.9898 + 7.1 * i) + 78.233 * (i + 1)) * 43758.5453
    return h - math.floor(h)


def mirror_x(fn):
    """Calls fn(sx) for sx in (-1, 1): helpers stay symmetric, which the mirrored UV unwrap needs."""
    for sx in (-1, 1):
        fn(sx)


def check_symmetry(builder, tol=1e-4):
    """Largest distance from a mirrored part vertex to its nearest vertex (0 for a symmetric build)."""
    bm = builder.bm
    co = np.array([tuple(v.co) for v in bm.verts])
    if len(co) == 0:
        return 0.0
    mir = co * np.array((-1, 1, 1))
    from mathutils.kdtree import KDTree
    kd = KDTree(len(co))
    for i, c in enumerate(co):
        kd.insert(c, i)
    kd.balance()
    worst = max(kd.find(Vector(m))[2] for m in mir)
    return worst


# ---------------------------------------------------------------------------------------------------------------
# Weights from the body
# ---------------------------------------------------------------------------------------------------------------

class BodyWeights:
    """Bone weights of a skinned body at any rest point: interpolated over the nearest body face."""

    def __init__(self, body):
        me = body.data
        self.names = [vg.name for vg in body.vertex_groups]
        self.W = np.zeros((len(me.vertices), len(self.names)))
        for v in me.vertices:
            for g in v.groups:
                self.W[v.index, g.group] = g.weight
        self.co = np.array([tuple(v.co) for v in me.vertices])
        self.polys = [list(p.vertices) for p in me.polygons]
        self.bvh = BVHTree.FromPolygons([Vector(c) for c in self.co], self.polys)

    def at(self, p, max_influences=4):
        loc, _, fi, _ = self.bvh.find_nearest(Vector(p))
        vs = self.polys[fi]
        d = np.linalg.norm(self.co[vs] - np.array(tuple(loc)), axis=1)
        w = 1.0 / np.maximum(d, 1e-5) ** 2
        w /= w.sum()
        bw = (self.W[vs] * w[:, None]).sum(axis=0)
        order = np.argsort(-bw)[:max_influences]
        out = {self.names[i]: float(bw[i]) for i in order if bw[i] > 0.01}
        tot = sum(out.values())
        return {k: v / tot for k, v in out.items()}


def skin_from_body(obj, arm, body, builder, remap=None):
    """Each vertex takes the weights of its part: the body's weights at the part's anchor, or its fixed bone
    (`remap` maps a bone missing from this rig to the one that replaces it, for the LOD)."""
    pts, _ = C.mesh_arrays(obj.data)
    pid = C.assign_parts(pts, builder)
    bw = BodyWeights(body)
    rig_bones = {b.name for b in arm.data.bones if b.use_deform}
    per_part = {}
    for part in builder.parts:
        bone = getattr(part, "bone_fixed", None)
        if bone:
            while bone not in rig_bones and remap and bone in remap:
                bone = remap[bone]
            if bone not in rig_bones:
                raise RuntimeError(f"{obj.name}: bone {bone} not in {arm.name}")
            per_part[part.pid] = {bone: 1.0}
        else:
            per_part[part.pid] = bw.at(part.anchor)
    for vg in list(obj.vertex_groups):
        obj.vertex_groups.remove(vg)
    groups = {}
    for i, p in enumerate(pid):
        for n, w in per_part[int(p)].items():
            if n not in groups:
                groups[n] = obj.vertex_groups.new(name=n)
            groups[n].add([i], w, "REPLACE")
    for m in list(obj.modifiers):
        if m.type == "ARMATURE":
            obj.modifiers.remove(m)
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    obj.matrix_parent_inverse.identity()
    return per_part


# ---------------------------------------------------------------------------------------------------------------
# Build, LOD, export
# ---------------------------------------------------------------------------------------------------------------

def _island_scale(builder, boost):
    def fn(bm, island, face_pid):
        names = [builder.parts[face_pid[fi]].name for fi in island]
        best = max(set(names), key=names.count)
        return boost.get(best, 1.0)
    return fn


def build_part(boss, part, builder, tris, lod_tris, voxel, boost=None, emission_strength=2.5, ao=0.05, k=0.6,
               smooth=2, extrusion=0.012, ray=0.04):
    """Bakes `<Boss>_<part>` (maps <Boss>_<part>_*.png, Low set) from the builder, skins it from the body and builds
    its LOD1 (parts with lod=False dropped, decimated) skinned from the LOD body. Returns (obj, lod_obj, facts)."""
    name = f"{boss.name}_{part}"
    lod_name = name + "_LOD1"
    sym = check_symmetry(builder)
    C.log(name, "symmetry error", round(sym, 6))
    asset = DK.build_mesh_asset_dk(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=ao,
                                   extrusion=extrusion, ray=ray, emission_strength=emission_strength, specular=0.5, k=k,
                                   families=FAMILIES, low_size=LOW_TEX, out_dir=boss.folder, smooth_iterations=smooth,
                                   island_scale=_island_scale(builder, boost or {}))
    high = bpy.data.objects.get(name + "_High")
    if high is not None:
        bpy.data.objects.remove(high)
    obj = asset.low
    skin_from_body(obj, boss.arm, boss.mesh, builder)
    lod = None
    if boss.lod_arm is not None and lod_tris:
        lod = make_lod(obj, lod_name, builder, lod_tris)
        remap = lod_remap(boss)
        skin_from_body(lod, boss.lod_arm, boss.lod_mesh, builder, remap)
        lod.hide_render = True
    facts = {"object": name, "material": name + "_Mat", "maps": f"{name}_*.png ({TEX}, Low {LOW_TEX})",
             "tris": C.tri_count(obj), "lod1_object": lod_name if lod else None,
             "lod1_tris": C.tri_count(lod) if lod else 0, "source_tris": asset.high_tris, "uv": asset.uv_stats,
             "symmetry_error": round(sym, 6)}
    return obj, lod, facts


def make_lod(obj, lod_name, builder, lod_tris):
    """LOD1 copy: faces of parts with lod=False go, the rest is decimated to `lod_tris` (keeps UVs and material)."""
    me = obj.data.copy()
    pts = np.array([tuple(v.co) for v in me.vertices])
    pid = C.assign_parts(pts, builder)
    drop = {p.pid for p in builder.parts if not getattr(p, "lod", True)}
    if drop:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        doomed = [f for f in bm.faces if all(int(pid[v.index]) in drop for v in f.verts)]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        loose = [v for v in bm.verts if not v.link_faces]
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
        bm.to_mesh(me)
        bm.free()
    me.calc_loop_triangles()
    if len(me.loop_triangles) > lod_tris:
        dec = C.decimate(me, lod_tris, lod_name, symmetric=True)
    else:
        dec = me
        dec.name = lod_name
    C.shade_smooth(dec)
    lod = C.link(bpy.data.objects.new(lod_name, dec))
    lod.data.materials.clear()
    lod.data.materials.append(obj.data.materials[0])
    return lod


def lod_remap(boss):
    """Full-rig bones missing from the LOD rig -> their nearest surviving ancestor."""
    keep = {b.name for b in boss.lod_arm.data.bones}
    out = {}
    for b in boss.arm.data.bones:
        if b.name in keep:
            continue
        p = b.parent
        while p is not None and p.name not in keep:
            p = p.parent
        if p is not None:
            out[b.name] = p.name
    return out


def clips(boss):
    """{clip: action} of the full rig and of the LOD rig as the build script exported them."""
    full, lod = {}, {}
    for act in bpy.data.actions:
        n = act.name
        if n.endswith("_LOD"):
            lod[n[:-4]] = act
        elif not n.endswith(("_Full", "_Away")):
            full[n] = act
    lod = {k: v for k, v in lod.items() if k in full}
    return full, lod


def export(boss, extras, lod_extras):
    """<Boss>.fbx and <Boss>_LOD1.fbx again: the same rigs and clips, the body plus the detail parts."""
    full, lod = clips(boss)
    M.export_rig(os.path.join(boss.folder, boss.name + ".fbx"), boss.arm, [boss.mesh] + list(extras), full)
    if boss.lod_arm is not None and lod:
        M.export_rig(os.path.join(boss.folder, boss.name + "_LOD1.fbx"), boss.lod_arm,
                     [boss.lod_mesh] + [o for o in lod_extras if o is not None], lod)
    return list(full)


def backup(name):
    """A copy of the boss's current export (before round 45) in _backup_r45/<name>; never overwritten."""
    src = os.path.join(C.EXPORTS, name)
    dst = os.path.join(BACKUP, name)
    if os.path.isdir(dst):
        C.log("backup exists", dst)
        return dst
    shutil.copytree(src, dst)
    blend = os.path.join(C.BLEND, name + ".blend")
    if os.path.exists(blend):
        shutil.copy2(blend, os.path.join(dst, name + "_before_r45.blend"))
    C.log("backup", dst)
    return dst


def update_report(boss, facts):
    path = os.path.join(boss.folder, boss.name + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    rep[REPORT_KEY] = facts
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", path)


# ---------------------------------------------------------------------------------------------------------------
# QA renders
# ---------------------------------------------------------------------------------------------------------------

def posed(arm, bone, p):
    """Rest point `p` carried by `bone` into the current pose (world)."""
    bpy.context.view_layer.update()
    pb = arm.pose.bones[bone]
    return arm.matrix_world @ pb.matrix @ arm.data.bones[bone].matrix_local.inverted() @ Vector(p)


def qa_scene(scale, ground_z=0.0):
    """Preview world, light rig and floor (`ground_z`: a flyer's floor is at its Root, below the body)."""
    for o in list(bpy.data.objects):
        if o.type in ("LIGHT", "CAMERA") or o.name.startswith("Ground"):
            bpy.data.objects.remove(o)
    C.preview_world()
    rig = C.light_rig(scale=scale)
    C.ground(z=ground_z)
    for m in bpy.data.materials:
        m.use_backface_culling = True
    return rig


def cam_close(target, distance, azimuth, elevation, lens=55):
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    return C.closeup_camera(target, distance, azimuth=azimuth, elevation=elevation, lens=lens)


def cam_game(target, ortho):
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    return C.game_camera(target, ortho)


def shot(arm, act, frame, bone, focus, path, kind, distance=1.0, azimuth=-38, elevation=14, ortho=1.0, lens=55,
         size=(1280, 720)):
    """One QA render: `kind` 'close' (perspective, round the posed focus) or 'game' (the play camera, orthographic
    44 degrees from above)."""
    C.set_action(arm, bpy.data.actions[act], frame)
    target = posed(arm, bone, focus)
    cam = cam_close(target, distance, azimuth, elevation, lens) if kind == "close" else cam_game(target, ortho)
    return C.render(cam, path, size[0], size[1])


def qa_set(boss, tag, shots, sheet_cols=2):
    """Renders [(label, act, frame, bone, focus, kind, kwargs)] as r45_<boss>_<tag>_<label>.png plus a sheet."""
    os.makedirs(QA, exist_ok=True)
    paths = []
    low = boss.name.lower()
    for label, act, frame, bone, focus, kind, kw in shots:
        paths.append(shot(boss.arm, act, frame, bone, focus, os.path.join(QA, f"r45_{low}_{tag}_{label}.png"), kind,
                          **kw))
    C.contact_sheet(paths, os.path.join(QA, f"r45_{low}_{tag}.png"), sheet_cols, 640)
    return paths


def qa_clips(boss, tag, bone, focus, distance, azimuth=-38, elevation=14, frames_per_clip=2):
    """Weight check: the face at two frames of every clip (parts must follow), one sheet."""
    full, _ = clips(boss)
    paths = []
    low = boss.name.lower()
    for clip, act in full.items():
        a, b = int(act.frame_range[0]), int(act.frame_range[1])
        for i in range(frames_per_clip):
            f = a + (b - a) * (i + 1) // (frames_per_clip + 1)
            p = os.path.join(QA, f"_r45_{low}_{tag}_clip_{clip}_{f}.png")
            paths.append(shot(boss.arm, clip, f, bone, focus, p, "close", distance=distance, azimuth=azimuth,
                              elevation=elevation, size=(640, 360)))
    sheet = os.path.join(QA, f"r45_{low}_{tag}_clips.png")
    C.contact_sheet(paths, sheet, 4, 480)
    for p in paths:
        os.remove(p)
    return sheet


def blockout(boss, builders):
    """Fast geometry check: raw parts in flat colours, skinned from the body, nothing baked or saved."""
    colours = [(0.85, 0.75, 0.6), (0.5, 0.8, 0.9), (0.9, 0.5, 0.5)]
    for i, (part, b) in enumerate(builders.items()):
        raw = b.to_object(f"{boss.name}_{part}_Block")
        raw.data.materials.append(C.principled("Block" + part, colours[i % 3], rough=0.3, emission=colours[i % 3],
                                               strength=0.15))
        skin_from_body(raw, boss.arm, boss.mesh, b)
        C.log(part, "raw tris", C.tri_count(raw), "symmetry", round(check_symmetry(b), 6))


# ---------------------------------------------------------------------------------------------------------------
# Body map edits (surface detail the new parts replace), once per body bake
# ---------------------------------------------------------------------------------------------------------------

DIM_KEY = "r45_body_maps_edited"


def _raster(tris, size):
    """Coverage mask of UV triangles on a size x size map (row 0 = v 0)."""
    mask = np.zeros((size, size), dtype=np.float32)
    for uvs in tris:
        p = uvs * size - 0.5
        x0, y0 = np.floor(p.min(axis=0)).astype(int)
        x1, y1 = np.ceil(p.max(axis=0)).astype(int)
        x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, size - 1), min(y1, size - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        (ax, ay), (bx, by), (cx, cy) = p
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12:
            continue
        w0 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / den
        w1 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / den
        w2 = 1 - w0 - w1
        hit = (w0 >= -0.02) & (w1 >= -0.02) & (w2 >= -0.02)
        mask[ys[hit], xs[hit]] = 1.0
    return mask


def _grow(mask, free, steps):
    for _ in range(steps):
        m2 = mask.copy()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sm = np.roll(np.roll(mask, dy, 0), dx, 1)
            take = (sm > m2) & free
            m2[take] = sm[take]
        mask = m2
    return mask


def dim_body(boss, spheres, emission_keep=0.08, darken=None, darken_amount=0.6):
    """Body faces inside any (centre, radius) sphere lose their glow (and optionally darken towards `darken`, sRGB)
    in <Boss>_Albedo/_Emission.png and the Low set: old glowing bead eyes or a plain throat that a new part now
    covers. Once per body bake (the build script writes a fresh report without DIM_KEY). Returns the texel count."""
    path = os.path.join(boss.folder, boss.name + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    if rep.get(DIM_KEY):
        C.log("body maps already edited")
        return 0
    me = boss.mesh.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active.data
    co = np.array([tuple(v.co) for v in me.vertices])
    inside = np.zeros(len(co), dtype=bool)
    for c, r in spheres:
        inside |= np.linalg.norm(co - np.array(tuple(c)), axis=1) < r
    hit, other = [], []
    for lt in me.loop_triangles:
        uvs = np.array([tuple(uv[i].uv) for i in lt.loops])
        (hit if all(inside[v] for v in lt.vertices) else other).append(uvs)
    total = 0
    for sub, size in (("", TEX), ("Low", LOW_TEX)):
        mask = _raster(hit, size)
        used = _raster(other, size)
        mask = _grow(mask, used < 0.5, max(2, size // 256))
        m = mask[..., None]
        base = os.path.join(boss.folder, sub, boss.name)
        emi = C.image_to_array(base + "_Emission.png")
        emi[..., :3] = emi[..., :3] * (1 - m + m * emission_keep)
        C.array_to_image(emi, base + "_Emission.png", "sRGB")
        if darken is not None:
            alb = C.image_to_array(base + "_Albedo.png")
            alb[..., :3] = alb[..., :3] * (1 - darken_amount * m) + np.array(darken) * darken_amount * m
            C.array_to_image(alb, base + "_Albedo.png", "sRGB")
        total += int(mask.sum())
    for img in bpy.data.images:
        if img.filepath and os.path.basename(img.filepath).startswith(boss.name + "_"):
            img.reload()
    rep[DIM_KEY] = True
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("body maps edited", len(hit), "tris", total, "texels")
    return total


# ---------------------------------------------------------------------------------------------------------------
# The standard run of a boss detail script
# ---------------------------------------------------------------------------------------------------------------

def build_all(boss, specs, voxel, note, dimmed=None):
    """specs: {part: (builder, tris, lod_tris, boost)}. Bakes and skins every part, exports both FBX files with the
    existing clips, writes the report block and saves the .blend. Returns the facts."""
    body, body_lod = boss.tris()
    objs, lods, parts = [], [], {}
    for part, (builder, tris, lod_tris, boost) in specs.items():
        obj, lod, facts = build_part(boss, part, builder, tris, lod_tris, voxel, boost=boost)
        objs.append(obj)
        lods.append(lod)
        parts[part] = facts
    clip_names = export(boss, objs, lods)
    extra = sum(f["tris"] for f in parts.values())
    extra_lod = sum(f["lod1_tris"] for f in parts.values())
    names = ", ".join(f'"{p}"' for p in specs)
    facts = {"parts": parts, "body_tris": body, "extra_tris": extra, "extra_share": round(extra / body, 3),
             "body_lod1_tris": body_lod, "extra_lod1_tris": extra_lod,
             "extra_lod1_share": round(extra_lod / body_lod, 3) if body_lod else None, "clips": clip_names,
             "unity": f"ExtraParts {{ {names} }}: objects {boss.name}_<Part> (+ _LOD1 in {boss.name}_LOD1.fbx), "
                      f"maps {boss.name}_<Part>_*.png (+ Low/)",
             "note": note}
    if dimmed is not None:
        facts["body_maps"] = dimmed
    update_report(boss, facts)
    C.save_blend(boss.name)
    C.log(boss.name, "detail", extra, "tris", facts["extra_share"], "lod1", extra_lod, facts["extra_lod1_share"])
    return facts


def standard_main(name, parts, build_fn, qa_fn, clip_focus):
    """CLI of a boss detail script: --before | --blockout | --qa | (build) [--norender].
    build_fn(boss) -> specs/facts (it calls build_all); qa_fn(boss, tag); clip_focus = (bone, point, distance)."""
    args = C.cli_args()
    if args.get("before"):
        qa_fn(Boss(name, parts), "before")
        return
    if args.get("blockout"):
        boss = Boss(name, parts)
        blockout(boss, build_fn(boss, blockout_only=True))
        qa_fn(boss, "blockout")
        return
    if args.get("qa"):
        boss = Boss(name)
        qa_fn(boss, "after")
        qa_clips(boss, "after", *clip_focus)
        return
    run(name, parts, build_fn, qa_fn, clip_focus, bool(args.get("norender")))


def run(name, parts, build_fn, qa_fn, clip_focus, norender=False):
    """Build-script hook and default run: backup, build, export, report, save, after renders."""
    use_gpu()
    backup(name)
    boss = Boss(name, parts)
    build_fn(boss)
    if not norender:
        qa_fn(boss, "after")
        qa_clips(boss, "after", *clip_focus)
        qa_lod(boss, *clip_focus)


def qa_lod(boss, bone, focus, distance, azimuth=-38, elevation=14):
    """The LOD1 rig with its LOD1 parts at the face close-up (r45_<boss>_after_lod1.png)."""
    if boss.lod_arm is None:
        return None
    full = [boss.arm, boss.mesh] + [o for o in bpy.data.objects if o.parent == boss.arm and o.type == "MESH"]
    lod = [boss.lod_arm, boss.lod_mesh] + [o for o in bpy.data.objects if o.parent == boss.lod_arm and o.type == "MESH"]
    for o in full:
        o.hide_render = True
    for o in lod:
        o.hide_render = False
    _, lods = clips(boss)
    act = "Idle" if "Idle" in lods else next(iter(lods))
    b = bone if bone in boss.lod_arm.data.bones else lod_remap(boss).get(bone, bone)
    path = shot(boss.lod_arm, lods[act].name, 10, b, focus, os.path.join(QA, f"r45_{boss.name.lower()}_after_lod1.png"),
                "close", distance=distance, azimuth=azimuth, elevation=elevation)
    for o in full:
        o.hide_render = False
    for o in lod:
        o.hide_render = True
    return path
