"""Round 45 (Blender agent B): helpers for the boss detail passes (eyes, teeth, mandibles, claws).

Remco (2026-11-11): "Restyle the detail features on all the bosses - think claws, teeth, eyes etc." The technique is
gravemaw_maw.py (round 38): the detail is its own skinned mesh with its own material and maps, built from parts,
fused, baked (1024, Low 512), weighted to the bones the boss already has, and exported with the boss. No bone, clip
or event changes: every action in the report is exported again as it is.

Each boss has its own `<boss>_detail.py` that places parts on its body; this module holds what they share:

- `Boss`: opens ArtSource/Blend/<Name>.blend, finds the game mesh, the LOD, the extras already exported with it,
  removes this round's parts from a previous run (idempotent), backs the old export up once to
  ArtSource/Exports/_backup_r45/<Name>/.
- Body surface probes on the rest mesh (`ray`, `nearest`), so parts sit on the skin wherever the build put it.
- Part makers on a common.PartBuilder: `eye` (a wet lens with iris/pupil or facets in the maps, a dark lid ring),
  `fang`, `claw` (hooked shards with a keratin/ember/ice/crystal ramp), `mandible` (a flat hooked blade with a
  serrated inner edge). Parts are always built in mirrored pairs (the bake mirrors UVs across x = 0).
- Styles: eye_style, facet_style, lid_style, keratin_style, gum_style (numpy, like gravemaw_maw).
- `bake_part`: deep_kit.build_mesh_asset_dk + rigid skin (each vertex to the bone of the part it came from) +
  LOD1 (decimated copy on the LOD rig, bones the LOD lacks fall back to the nearest ancestor it has).
- `export`: <Name>.fbx and <Name>_LOD1.fbx with the body, the old extras and the new parts, all report actions.
- `qa`: close-ups (front, dramatic three-quarter, game camera) at an Idle frame, plus a clip strip of the head.

Cycles bakes run on the GPU (common._cycles is wrapped at import, the shared module itself is not edited).
"""

import json
import math
import os
import shutil

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

import boss_mech_kit as M
import common as C
import deep_kit as DK

ROUND = 45
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
BACKUP = os.path.join(C.EXPORTS, "_backup_r45")
TEX, LOW_TEX = 1024, 512


# ---------------------------------------------------------------------------------------------------------------
# GPU
# ---------------------------------------------------------------------------------------------------------------

def use_gpu():
    """Cycles on the GPU for the AO and map bakes (common._cycles pins the CPU)."""
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        for kind in ("OPTIX", "CUDA", "HIP", "ONEAPI", "METAL"):
            try:
                prefs.compute_device_type = kind
                prefs.get_devices()
                if any(d.type == kind for d in prefs.devices):
                    break
            except TypeError:
                continue
        for d in prefs.devices:
            d.use = True
        C.log("cycles devices", prefs.compute_device_type, [d.name for d in prefs.devices if d.use])
    except Exception as exc:  # noqa: BLE001 - a CPU bake still works
        C.log("gpu setup failed", exc)
        return
    base = C._cycles

    def gpu_cycles(samples):
        base(samples)
        bpy.context.scene.cycles.device = "GPU"
    if getattr(base, "__name__", "") != "gpu_cycles":
        C._cycles = gpu_cycles


# ---------------------------------------------------------------------------------------------------------------
# The boss
# ---------------------------------------------------------------------------------------------------------------

class Boss:
    """An opened boss .blend with this round's part names `parts` (suffixes, e.g. ("Eyes", "Claws"))."""

    def __init__(self, name, parts, opened=False):
        self.name = name
        self.part_names = [f"{name}_{p}" for p in parts]
        if not opened:
            bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
        self.lod_mesh = bpy.data.objects.get(name + "_LOD1")
        self.forget()
        mine = set(self.part_names) | {n + "_LOD1" for n in self.part_names}
        self.extras = sorted([o for o in self.arm.children if o.type == "MESH" and o is not self.mesh
                              and o.name not in mine and not o.name.endswith("_High")], key=lambda o: o.name)
        self.lod_extras = []
        if self.lod_arm is not None:
            self.lod_extras = sorted([o for o in self.lod_arm.children if o.type == "MESH" and o is not self.lod_mesh
                                      and o.name not in mine], key=lambda o: o.name)
        self.new, self.new_lod, self.facts = [], [], {}
        self._bvh = None
        self.report_path = os.path.join(C.EXPORTS, name, name + "_report.json")
        with open(self.report_path, encoding="utf-8") as fh:
            self.rep = json.load(fh)
        self.tris_before = {"body": C.tri_count(self.mesh), "lod1": C.tri_count(self.lod_mesh) if self.lod_mesh else 0,
                            "extras": sum(C.tri_count(o) for o in self.extras),
                            "lod1_extras": sum(C.tri_count(o) for o in self.lod_extras)}

    @classmethod
    def from_mech(cls, mboss, parts):
        """Wraps an opened boss_mech_kit.Boss (the build/mech script hook): same objects, no reopen."""
        return cls(mboss.name, parts, opened=True)

    def hand_over(self, mboss):
        """Puts the new parts into a boss_mech_kit.Boss's extras so its export() writes them."""
        mboss.extras.extend(self.new)
        mboss.lod_extras.extend(self.new_lod)

    def forget(self):
        for n in self.part_names:
            for full in (n, n + "_LOD1", n + "_High", n + "_Parts", n + "_Block"):
                o = bpy.data.objects.get(full)
                if o is not None:
                    bpy.data.objects.remove(o)

    def backup(self):
        """Copies the previous export (and the .blend) once; a rerun keeps the first backup."""
        dst = os.path.join(BACKUP, self.name)
        if os.path.exists(dst):
            C.log("backup exists", dst)
            return dst
        shutil.copytree(os.path.join(C.EXPORTS, self.name), dst)
        shutil.copy2(os.path.join(C.BLEND, self.name + ".blend"), os.path.join(dst, self.name + ".blend"))
        C.log("backup", dst)
        return dst

    # -- rest surface ---------------------------------------------------------------------------------------------

    def bvh(self):
        if self._bvh is None:
            bm = bmesh.new()
            bm.from_mesh(self.mesh.data)
            bm.transform(self.mesh.matrix_world)
            self._bvh = BVHTree.FromBMesh(bm)
            bm.free()
        return self._bvh

    def ray(self, origin, direction, far=20.0):
        """First body hit from `origin` along `direction` (rest pose): (point, normal) or (None, None)."""
        loc, nor, _, _ = self.bvh().ray_cast(Vector(origin), Vector(direction).normalized(), far)
        return loc, nor

    def nearest(self, point):
        loc, nor, _, _ = self.bvh().find_nearest(Vector(point))
        return loc, nor

    def bone(self, name):
        b = self.arm.data.bones[name]
        return b.head_local.copy(), b.tail_local.copy()

    # -- clips ------------------------------------------------------------------------------------------------------

    def clips(self):
        return [n for n in self.rep.get("actions", {}) if n in bpy.data.actions]

    def lod_clips(self):
        return {n: bpy.data.actions[n + "_LOD"] for n in self.clips() if (n + "_LOD") in bpy.data.actions}

    # -- export -----------------------------------------------------------------------------------------------------

    def export(self):
        folder = os.path.join(C.EXPORTS, self.name)
        full = {n: bpy.data.actions[n] for n in self.clips()}
        M.export_rig(os.path.join(folder, self.name + ".fbx"), self.arm, [self.mesh] + self.extras + self.new, full)
        if self.lod_arm is not None:
            M.export_rig(os.path.join(folder, self.name + "_LOD1.fbx"), self.lod_arm,
                         [self.lod_mesh] + self.lod_extras + self.new_lod, self.lod_clips())
        return list(full)

    def write_report(self, extra=None):
        body = self.tris_before["body"] + self.tris_before["extras"]
        lod = self.tris_before["lod1"] + self.tris_before["lod1_extras"]
        first = self.rep.get("round45_detail", {}).get("tris_before")
        if first:
            # A rerun opens a body already cut under the new eyes: keep the counts from before round 45.
            body, lod = first["lod0"], first["lod1"]
        added = sum(C.tri_count(o) for o in self.new)
        added_lod = sum(C.tri_count(o) for o in self.new_lod)
        r45 = {"parts": self.facts, "tris_before": {"lod0": body, "lod1": lod},
               "tris_after": {"lod0": C.tri_count(self.mesh) + self.tris_before["extras"] + added,
                              "lod1": (C.tri_count(self.lod_mesh) if self.lod_mesh else 0)
                              + self.tris_before["lod1_extras"] + added_lod},
               "increase": {"lod0": round(added / max(body, 1), 3), "lod1": round(added_lod / max(lod, 1), 3)},
               "extra_objects": [o.name for o in self.new + self.new_lod],
               "unity": "extra parts (BlenderArtEntry.ExtraParts): " + ", ".join(
                   f"'{o.name[len(self.name) + 1:]}' -> {o.name} / {o.name}_LOD1, maps {o.name}_*.png (+ Low/)"
                   for o in self.new),
               "note": "round 45: detail parts (eyes, teeth, claws); bones, clips and events unchanged"}
        if extra:
            r45.update(extra)
        self.rep["round45_detail"] = r45
        with open(self.report_path, "w", encoding="utf-8") as fh:
            json.dump(self.rep, fh, indent=2)
        C.log("report", self.report_path, json.dumps(r45["tris_before"]), json.dumps(r45["tris_after"]))
        return r45

    def save(self):
        for o in self.new_lod:
            o.hide_render = True
        C.save_blend(self.name)


# ---------------------------------------------------------------------------------------------------------------
# Geometry helpers
# ---------------------------------------------------------------------------------------------------------------

def mirror(v):
    v = Vector(v)
    return Vector((-v.x, v.y, v.z))


def side_bone(name, sx):
    """'_R' bone names for x > 0, '_L' for x < 0 (both conventions in the builds: _R1, _R_, R at the end)."""
    if sx > 0:
        return name
    for a, b in (("_R_", "_L_"), ("_R1", "_L1"), ("_R2", "_L2"), ("_R3", "_L3")):
        if a in name:
            return name.replace(a, b, 1)
    if name.endswith("_R"):
        return name[:-2] + "_L"
    if name.endswith("R"):          # Mand_UR, Petal_FR
        return name[:-1] + "L"
    return name


def frame_from(normal, up=Vector((0, 0, 1))):
    """Rotation whose local z is `normal` and local y as close to `up` as possible."""
    z = Vector(normal).normalized()
    up = Vector(up)
    x = up.cross(z)
    if x.length < 1e-4:
        x = Vector((1, 0, 0)).cross(z)
    x.normalize()
    y = z.cross(x).normalized()
    return Matrix((x, y, z)).transposed()


def hash01(*xs):
    h = 0.0
    for i, x in enumerate(xs):
        h += math.sin(x * (12.9898 + 7.1 * i) + 78.233 * (i + 1)) * 43758.5453
    return h - math.floor(h)


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def _arr(c):
    return np.array(c, dtype=float)


def eye_style(iris, glow, sclera=(0.03, 0.02, 0.025), pupil="slit", glow_k=0.9, seed=0, rings=0.0):
    """A big wet eye seen through a clear cornea: a dark wet sclera, an iris with radial fibres and a hot inner ring
    (the glow that reads in the dark), a black pupil (vertical slit, round or a cross of four slits), a pale
    catch-light band and a very low roughness everywhere (the lens). Local frame: z is the look direction."""
    iris, glow, sclera = _arr(iris), _arr(glow), _arr(sclera)

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        r = np.hypot(u[:, 0], u[:, 1]) * (u[:, 2] > 0) + (u[:, 2] <= 0) * 1.0
        ang = np.arctan2(u[:, 1], u[:, 0])
        front = C.smoothstep(-0.1, 0.4, u[:, 2])
        fib = 0.5 + 0.5 * np.cos(ang * 31 + 3 * C.value_noise(s.p[idx], 30, seed))
        iris_m = (1 - C.smoothstep(0.6, 0.68, r)) * front
        if pupil == "slit":
            pm = (1 - C.smoothstep(0.08, 0.13, np.abs(u[:, 0]) / np.maximum(1 - (u[:, 1] / 0.55) ** 2, 0.05) ** 0.5))
            pm *= (1 - C.smoothstep(0.5, 0.56, np.abs(u[:, 1])))
        elif pupil == "cross":
            arms = np.minimum(np.abs(u[:, 0]), np.abs(u[:, 1]))
            pm = (1 - C.smoothstep(0.045, 0.08, arms)) * (1 - C.smoothstep(0.42, 0.5, r))
            pm = np.maximum(pm, 1 - C.smoothstep(0.1, 0.14, r))
        else:
            pm = 1 - C.smoothstep(0.2, 0.25, r)
        pm = pm * front
        hot = C.smoothstep(0.12, 0.3, r) * (1 - C.smoothstep(0.32, 0.5, r))
        col = sclera * (0.8 + 0.4 * C.value_noise(s.p[idx], 18, seed + 1)[:, None] * 0.5)
        iris_col = C.lerp3(iris * 0.55, iris, fib * 0.7 + 0.3 * hot)
        iris_col = C.lerp3(iris_col, glow, hot * 0.6)
        col = C.lerp3(col, iris_col, iris_m)
        limbus = C.smoothstep(0.58, 0.65, r) * (1 - C.smoothstep(0.68, 0.8, r)) * front
        col = C.lerp3(col, sclera * 0.3, limbus)
        if rings:
            band = 1 - C.smoothstep(0.0, 0.05, np.abs(((r * 6) % 1.0) - 0.5) - 0.38)
            col = C.lerp3(col, iris * 0.3, band * iris_m * rings)
        col = C.lerp3(col, np.zeros(3), pm)
        veins = (1 - C.smoothstep(0.0, 0.04, np.abs(C.value_noise(s.p[idx], 26, seed + 2)))) * (1 - iris_m)
        col = C.lerp3(col, glow * 0.35, veins * 0.5)
        s.albedo[idx] = col
        e = glow_k * (iris_m * (0.25 + 0.55 * hot + 0.2 * fib) * (1 - pm) + 0.18 * veins)
        s.emit[idx] = np.clip(C.lerp3(iris, glow, 0.5 + 0.5 * hot) * e[:, None], 0, 1)
        s.rough[idx] = 0.05 + 0.04 * (1 - front)
        s.disp[idx] = 0.0
        s.fam[idx] = (0.0, 0.0, 0.0, 0.0)
        s.flow[idx] = 0.0
    return style


def facet_style(base, glow, cells=260, glow_k=0.9, seed=0, inner=None):
    """A compound eye: hexagon-like facets (Voronoi on the sphere), each a glossy lens with a lit core and a dark
    rim; the light gathers towards the front. `inner` (round 45 polish): the eye burns from within, the facets
    towards the middle take this core colour and the whole lens glows (a small dark false pupil stays)."""
    base, glow = _arr(base), _arr(glow)
    inner = _arr(inner) if inner is not None else None
    seeds = C.fibonacci_sphere(cells, seed=seed)

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        e = C.voronoi_edge(u, seeds)
        rim = 1 - C.smoothstep(0.0, 0.05, e)
        core = C.smoothstep(0.06, 0.16, e)
        front = C.smoothstep(-0.2, 0.9, u[:, 2])
        col = C.lerp3(base * 0.6, base, core)
        col = C.lerp3(col, glow * 0.8, core * front * 0.5)
        col = C.lerp3(col, np.array((0.01, 0.01, 0.012)), rim * 0.85)
        # A small dark false pupil straight ahead (where the facets look at the viewer), the rest burns.
        pseudo = (1 - C.smoothstep(0.04, 0.14, np.hypot(u[:, 0], u[:, 1]))) * (u[:, 2] > 0)
        col = C.lerp3(col, np.zeros(3), pseudo * 0.7)
        light = glow[None, :].repeat(len(idx), 0)
        amount = glow_k * (0.15 + 0.85 * core) * (0.45 + 0.55 * front)
        if inner is not None:
            centre = 1 - C.smoothstep(0.2, 0.75, np.hypot(u[:, 0], u[:, 1])) * (u[:, 2] > 0)
            light = C.lerp3(light, inner, centre)
            col = C.lerp3(col, inner * 0.9, centre * core * 0.6)
            amount = glow_k * (0.45 + 0.55 * core) * (0.6 + 0.4 * front) + 0.35 * centre
        s.albedo[idx] = col
        s.emit[idx] = np.clip(light * (amount * (1 - 0.8 * pseudo) * (1 - 0.6 * rim))[:, None], 0, 1)
        s.rough[idx] = 0.06 + 0.25 * rim
        s.disp[idx] = -0.0012 * rim
        s.fam[idx] = (0.0, 0.0, 0.0, 0.0)
        s.flow[idx] = 0.0
    return style


def lid_style(dark, rim, wet=(0.25, 0.05, 0.06), seed=0):
    """The ring round an eye: dark chitin outside, ridged, turning to wet flesh at the inner lip."""
    dark, rim, wet = _arr(dark), _arr(rim), _arr(wet)

    def style(s, idx, part):
        p = s.p[idx]
        t, rad, dist = C.tube_param(part, p)
        mott = 0.5 + 0.5 * C.value_noise(p, 40, seed)
        col = C.lerp3(dark * 0.7, dark * 1.2, mott)
        ridge = 0.5 + 0.5 * np.cos(t * 60 + 3 * C.value_noise(p, 20, seed + 1))
        col = C.lerp3(col, rim, ridge * 0.25)
        s.albedo[idx] = col
        s.emit[idx] = 0.0
        s.rough[idx] = 0.55 - 0.2 * mott
        s.disp[idx] = 0.0008 * ridge
        s.fam[idx] = (0.8, 0.0, 0.0, 0.0)
        s.flow[idx] = t * C.axis_length(part)
    return style


def fold_style(skin, dark, wet=(0.12, 0.04, 0.05), facing=-1.0, glow=None, glow_k=0.0, seed=0, ridges=14,
               family=(0.7, 0.0, 0.0, 0.0)):
    """A lid fold or brow over an eye (organic socket, round 45 polish): mottled skin with creases running along
    the fold, darker and wet on the rim that faces the eye (local y towards `facing`) and in the front edge, so the
    eye looks sunk in under it. `glow` (optional) leaks along the wet rim (crystal sockets)."""
    skin, dark, wet = _arr(skin), _arr(dark), _arr(wet)
    glow = _arr(glow) if glow is not None else None

    def style(s, idx, part):
        p = s.p[idx]
        L = C.local_points(part, p)
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        mott = 0.5 + 0.5 * C.value_noise(p, 34, seed)
        crease = 0.5 + 0.5 * np.cos(u[:, 1] * ridges + 2.0 * C.value_noise(p, 12, seed + 1))
        if facing == 0:
            rim = C.smoothstep(0.8, 0.95, u[:, 2])         # a mound: wet only where the lens breaks through
        else:
            rim = C.smoothstep(0.1, 0.75, u[:, 1] * facing) * C.smoothstep(-0.4, 0.5, u[:, 2])
        col = C.lerp3(dark, skin, 0.35 + 0.5 * mott) * (1 - 0.25 * crease)[:, None]
        col = C.lerp3(col, wet, rim * 0.85)
        s.albedo[idx] = np.clip(col, 0, 1)
        s.emit[idx] = (np.clip(glow * (glow_k * rim * (0.6 + 0.4 * mott))[:, None], 0, 1)
                       if glow is not None else 0.0)
        s.rough[idx] = 0.62 - 0.22 * mott * 0.5 - 0.42 * rim
        s.disp[idx] = 0.0012 * crease * (1 - rim)
        s.fam[idx] = family
        s.flow[idx] = 0.0
    return style


def keratin_style(root, body, tip, glow=None, glow_at="tip", glow_k=0.6, stain=None, seed=0, bands=7,
                  translucent=0.0, wet=0.35, family=(0.6, 0.0, 0.0, 0.0)):
    """Tooth, fang, claw, spike: a dark root, the body colour, a pale (or hot) worn tip; striations along it, growth
    bands across, hairline cracks, a stain at the root; optional glow at the tip (ember, ice light) or a band at the
    root (light shining through, like gravemaw_maw). `translucent` brightens the body towards the tip like ice or
    crystal."""
    root, body, tip = _arr(root), _arr(body), _arr(tip)
    glow = _arr(glow) if glow is not None else None
    stain = _arr(stain) if stain is not None else root * 0.6

    def style(s, idx, part):
        p = s.p[idx]
        z, radius, dist = C.tube_param(part, p)
        L = C.local_points(part, p)
        striae = 0.5 + 0.5 * C.value_noise(np.c_[L[:, 0] * 3, L[:, 1] * 3, z * 0.5], 6, seed)
        band = 1 - C.smoothstep(0.0, 0.05, np.abs(((z * bands) % 1.0) - 0.5) - 0.42)
        col = C.lerp3(root, body, C.smoothstep(0.05, 0.35, z))
        col = C.lerp3(col, tip, C.smoothstep(0.55, 0.95, z))
        col = col * (0.84 + 0.22 * striae)[:, None] * (1 - 0.14 * band)[:, None]
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 24, seed + 3)))
        crack *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 5, seed + 4))
        col = C.lerp3(col, root * 0.45, crack * 0.5)
        col = C.lerp3(col, stain, (1 - C.smoothstep(0.0, 0.12, z)) * (0.6 + 0.4 * striae) * 0.7)
        if translucent:
            col = col * (1 + translucent * C.smoothstep(0.3, 1.0, z))[:, None]
        s.albedo[idx] = np.clip(col, 0, 1)
        if glow is not None:
            if glow_at == "tip":
                amount = C.smoothstep(0.4, 0.97, z) ** 1.2
            elif glow_at == "rise":
                # Dark gum at the root, lit body, bright tip: a smooth ramp (no band).
                amount = 0.06 + 0.94 * C.smoothstep(0.08, 1.0, z) ** 1.4
            elif glow_at == "root":
                amount = C.smoothstep(0.02, 0.1, z) * (1 - C.smoothstep(0.2, 0.42, z))
            else:
                amount = 0.4 + 0.6 * C.smoothstep(0.2, 1.0, z)
            amount = amount * (1 - 0.6 * crack) + 0.6 * crack * C.smoothstep(0.3, 0.8, z) * (glow_at == "tip")
            s.emit[idx] = np.clip(glow * (glow_k * amount)[:, None], 0, 1)
        else:
            s.emit[idx] = 0.0
        s.rough[idx] = wet + 0.2 * striae + 0.18 * crack + 0.1 * C.smoothstep(0.85, 1.0, z)
        s.disp[idx] = -0.0015 * crack - 0.001 * band
        s.fam[idx] = family
        s.flow[idx] = z * C.axis_length(part)
    return style


def blade_style(dark, edge, glow=None, glow_k=0.5, seed=0, family=(0.8, 0.0, 0.0, 0.0)):
    """A mandible blade: dark glossy chitin with pale worn ridges, a lighter cutting edge, growth bands."""
    dark, edge = _arr(dark), _arr(edge)
    glow = _arr(glow) if glow is not None else None

    def style(s, idx, part):
        p = s.p[idx]
        t, rad, dist = C.tube_param(part, p)
        mott = 0.5 + 0.5 * C.value_noise(p, 30, seed)
        band = 1 - C.smoothstep(0.0, 0.08, np.abs(((t * 9) % 1.0) - 0.5) - 0.4)
        rim = C.smoothstep(0.6, 0.98, dist / np.maximum(rad, 1e-4))
        col = C.lerp3(dark * 0.7, dark * 1.25, mott)
        col = C.lerp3(col, edge, rim * 0.6 + C.smoothstep(0.75, 1.0, t) * 0.5)
        col = col * (1 - 0.18 * band)[:, None]
        s.albedo[idx] = np.clip(col, 0, 1)
        if glow is not None:
            s.emit[idx] = np.clip(glow * (glow_k * band * (1 - rim) * C.smoothstep(0.0, 0.5, t))[:, None], 0, 1)
        else:
            s.emit[idx] = 0.0
        s.rough[idx] = 0.32 + 0.25 * (1 - mott) - 0.12 * rim
        s.disp[idx] = -0.0015 * band
        s.fam[idx] = family
        s.flow[idx] = t * C.axis_length(part)
    return style


def gum_style(flesh, deep, glow=None, glow_k=0.4, seed=0):
    """Wet inner mouth: dark flesh with folds, getting darker (or hotter, with `glow`) inside."""
    flesh, deep = _arr(flesh), _arr(deep)
    glow = _arr(glow) if glow is not None else None

    def style(s, idx, part):
        p = s.p[idx]
        L = C.local_points(part, p)
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        inner = C.smoothstep(-0.2, 0.8, -u[:, 2])
        fold = 0.5 + 0.5 * np.cos(np.arctan2(u[:, 1], u[:, 0]) * 14 + 3 * C.value_noise(p, 16, seed))
        mott = 0.5 + 0.5 * C.value_noise(p, 36, seed + 1)
        col = C.lerp3(flesh, deep, inner) * (0.75 + 0.35 * mott)[:, None]
        col = C.lerp3(col, deep * 0.5, fold * 0.35)
        s.albedo[idx] = col
        s.emit[idx] = (np.clip(glow * (glow_k * (0.3 + 0.7 * fold) * (0.4 + 0.6 * inner))[:, None], 0, 1)
                       if glow is not None else 0.0)
        s.rough[idx] = 0.14 + 0.12 * mott
        s.disp[idx] = 0.0008 * fold
        s.fam[idx] = (0.0, 0.7, 0.0, 0.0)
        s.flow[idx] = 0.0
    return style


# ---------------------------------------------------------------------------------------------------------------
# Part makers (mirrored pairs; `bone` is the right-hand name, side_bone() gives the left)
# ---------------------------------------------------------------------------------------------------------------

def _pair(centre, fn, both=True):
    c = Vector(centre)
    if not both or abs(c.x) < 1e-4:
        return [fn(c, 1)]
    return [fn(c, 1), fn(mirror(c), -1)]


def flip(v, sx):
    v = Vector(v)
    return Vector((v.x * sx, v.y, v.z))


def eye(b, name, centre, look, radius, bone, style, squash=(1.0, 0.85, 0.7), lid=None, lid_style_fn=None,
        both=True, up=Vector((0, 0, 1)), segments=(28, 16), lid_tilt=0.0, socket=None):
    """A pair of eyes at `centre` (right side, mirrored) looking along `look`: an ellipsoid lens (radii = radius x
    squash, z along the look) and an optional lid ring (`lid` = (ring radius factor, tube radius factor, push))
    at the bulge's rim so it sits in a dark socket."""
    out = []

    def one(c, sx):
        d = flip(look, sx).normalized()
        rot = frame_from(d, up)
        r = (radius * squash[0], radius * squash[1], radius * squash[2])
        part = b.ellipsoid(name, c, r, rot=rot, bone=side_bone(bone, sx), style=style, segments=segments)
        out.append(part)
        if socket is not None:
            out.extend(lids(b, name, c, rot, r, side_bone(bone, sx), **socket))
        if lid is not None:
            rr, tr, push = lid
            ring_c = c + d * (radius * squash[2] * push)
            n = (d + rot @ Vector((0, lid_tilt, 0))).normalized()
            out.extend(b.ring(name + "Lid", ring_c, n, radius * rr, radius * tr, arcs=3, sides=8, samples=4,
                              chain=[side_bone(bone, sx)], style=lid_style_fn, ref=rot @ Vector((0, 1, 0)),
                              profile=lambda th: 1.0 + 0.25 * math.cos(th) ** 2))
        return part
    _pair(centre, one, both)
    return out


def lids(b, name, c, rot, r, bone, mound, brow=None, size=(1.35, 1.9, 0.8), sink=0.25, brow_lift=1.15,
         brow_size=(1.45, 0.75, 1.0), brow_tilt=0.45, brow_deform=None):
    """Organic socket (round 45 polish, replaces the torus lid ring): a fleshy mound round the eye, centred behind
    the lens, so the lens only breaks through in its middle and the skin closes over its rim as an almond (no
    uniform ring), and an optional heavy brow ridge over it. `rot` is the eye's frame (z the look, y up), `r` its
    radii; `mound`/`brow` are styles (fold_style; the mound with facing 0, wet towards the lens)."""
    x, y, z = (Vector(rot.col[i]) for i in range(3))
    rx, ry, rz = r
    out = [b.ellipsoid(name + "Socket", c - z * rz * sink, (rx * size[0], ry * size[1], rz * size[2]), rot=rot,
                       bone=bone, style=mound, segments=(28, 14))]
    if brow is not None:
        out.append(b.ellipsoid(name + "Brow", c + y * ry * brow_lift * size[1] * 0.7 - z * rz * 0.05,
                               (rx * brow_size[0], ry * brow_size[1], rz * brow_size[2]),
                               rot=rot @ Matrix.Rotation(-brow_tilt, 3, "X"), bone=bone, style=brow,
                               segments=(24, 12), deform=brow_deform))
    return out


def tooth_profile(t):
    return (1 - t) ** 1.15 * (1.0 + 0.3 * math.exp(-((t - 0.1) / 0.12) ** 2))


def claw_profile(t):
    """Thick keeled base, a long curve, a needle point."""
    return (1 - t) ** 0.9 * (1.0 + 0.25 * math.exp(-((t - 0.05) / 0.1) ** 2))


def fang(b, name, base, direction, radius, length, bone, style, hook=None, hook_k=0.3, both=True, sides=7,
         flute=0.12, profile=tooth_profile, rings=10, twist=0.0):
    """Mirrored hooked shard(s); `hook` is the direction the tip curls towards."""
    out = []

    def one(c, sx):
        d = flip(direction, sx)
        bend = flip(hook, sx).normalized() * length * hook_k if hook is not None else None
        part = b.shard(name, c, d, radius, length, sides=sides, flute=flute, profile=profile, bend=bend, rings=rings,
                       bone=side_bone(bone, sx), style=style, twist=twist * sx)
        out.append(part)
        return part
    _pair(base, one, both)
    return out


def claw(b, name, base, direction, radius, length, bone, style, hook, hook_k=0.45, both=True):
    return fang(b, name, base, direction, radius, length, bone, style, hook=hook, hook_k=hook_k, both=both, sides=8,
                flute=0.08, profile=claw_profile, rings=12)


def tube_pair(b, name, points, radii, bone, style, both=True, sides=10, flatten=1.0):
    """A mirrored tube (mandible blade, horn, tendril) through `points` (right side)."""
    out = []

    def one(c, sx):
        pts = [flip(p, sx) for p in points]
        part = b.tube(name, pts, radii, sides=sides, samples=5, chain=[side_bone(bone, sx)], style=style,
                      cap=0.4, flatten=flatten)
        out.append(part)
        return part
    _pair(points[0], one, both)
    return out


# ---------------------------------------------------------------------------------------------------------------
# Bake, skin, LOD
# ---------------------------------------------------------------------------------------------------------------

def _lod_bone(boss, bone):
    names = {b.name for b in boss.lod_arm.data.bones}
    b = boss.arm.data.bones.get(bone)
    while b is not None and b.name not in names:
        b = b.parent
    return b.name if b is not None else next(iter(names))


def skin_rigid(obj, arm, builder, remap=None, blend=None):
    """Each vertex to the bone of the part it came from (a part's `bone`, or the first of its chain).
    `blend(part, point) -> {bone: w}` (optional) overrides the weights for a part (e.g. a lid that spans two bones)."""
    pts, _ = C.mesh_arrays(obj.data)
    pid = C.assign_parts(pts, builder)
    for vg in list(obj.vertex_groups):
        obj.vertex_groups.remove(vg)
    groups = {}

    def group(n):
        n = remap(n) if remap else n
        if n not in groups:
            groups[n] = obj.vertex_groups.new(name=n)
        return groups[n], n
    for i, p in enumerate(pts):
        part = builder.parts[pid[i]]
        weights = blend(part, p) if blend else None
        if not weights:
            weights = {part.bone or part.chain[0]: 1.0}
        merged = {}
        for n, w in weights.items():
            g, key = group(n)
            merged[key] = (g, merged.get(key, (g, 0.0))[1] + w)
        for g, w in merged.values():
            if w > 0.005:
                g.add([i], float(w), "REPLACE")
    for m in list(obj.modifiers):
        if m.type == "ARMATURE":
            obj.modifiers.remove(m)
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    obj.matrix_parent_inverse.identity()
    return sorted(groups)


def bake_part(boss, suffix, builder, tris, lod_tris, voxel=0.004, families=("chitin", "membrane"), k=1.2,
              emission_strength=2.5, specular=0.5, ao=0.08, extrusion=0.015, ray=0.04, island_scale=None,
              smooth_iterations=1, blend=None, note=""):
    """Bakes `<Boss>_<suffix>` (+ Low maps), skins it, makes its LOD1; returns the game object."""
    name = f"{boss.name}_{suffix}"
    folder = os.path.join(C.EXPORTS, boss.name)
    asset = DK.build_mesh_asset_dk(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=ao,
                                   extrusion=extrusion, ray=ray, emission_strength=emission_strength,
                                   specular=specular, k=k, families=families, low_size=LOW_TEX, out_dir=folder,
                                   smooth_iterations=smooth_iterations, island_scale=island_scale)
    high = bpy.data.objects.get(name + "_High")
    if high is not None:
        bpy.data.objects.remove(high)
    obj = asset.low
    bones = skin_rigid(obj, boss.arm, builder, blend=blend)
    boss.new.append(obj)
    facts = {"object": name, "material": name + "_Mat", "maps": f"{name}_*.png ({TEX}, Low {LOW_TEX})",
             "tris": C.tri_count(obj), "source_tris": asset.high_tris, "uv": asset.uv_stats, "detail": asset.details,
             "families": list(families[:2]), "bones": bones, "note": note}
    if boss.lod_arm is not None:
        lod_mesh = C.decimate(obj.data.copy(), lod_tris, name + "_LOD1", symmetric=True)
        C.shade_smooth(lod_mesh)
        lod = C.link(bpy.data.objects.new(name + "_LOD1", lod_mesh))
        lod.data.materials.clear()
        lod.data.materials.append(obj.data.materials[0])
        skin_rigid(lod, boss.lod_arm, builder, remap=lambda n: _lod_bone(boss, n), blend=blend)
        boss.new_lod.append(lod)
        facts.update(lod1_object=lod.name, lod1_tris=C.tri_count(lod))
    boss.facts[suffix] = facts
    C.log(name, "tris", facts["tris"], "lod1", facts.get("lod1_tris"))
    return obj


def detail_island_scale(builder, prefixes, factor):
    """Texel density boost for islands mostly made of parts whose names start with `prefixes` (the hero detail)."""
    def fn(bm, island, face_pid):
        names = [builder.parts[face_pid[fi]].name for fi in island]
        hit = sum(1 for n in names if n.startswith(prefixes))
        return factor if hit * 2 > len(names) else 1.0
    return fn


def cut_spheres(boss, spheres):
    """Removes body faces (game mesh and LOD1) lying wholly inside any (centre, radius) sphere: the build's old
    eye balls under the new sunk eyes (the socket mounds cover the holes). Rest coordinates, UVs and weights of the
    rest untouched; idempotent. Returns {object: triangles removed}."""
    out = {}
    for obj in (boss.mesh, boss.lod_mesh):
        if obj is None:
            continue
        before = C.tri_count(obj)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        doomed = [f for f in bm.faces
                  if any(all((v.co - Vector(c)).length < r for v in f.verts) for c, r in spheres)]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
        out[obj.name] = before - C.tri_count(obj)
    boss._bvh = None
    boss.facts["body_cut_tris"] = out
    C.log(boss.name, "cut old eyes", out)
    return out


def run(boss, makers, args, qa_fn=None, prep=None):
    """The standard pass: back up, bake each (suffix, builder_fn, bake kwargs), export, report, save, QA.
    `makers`: [(suffix, fn(boss) -> PartBuilder, dict of bake_part kwargs)]."""
    boss.backup()
    if prep is not None:
        prep(boss)
    for suffix, fn, kw in makers:
        bake_part(boss, suffix, fn(boss), **kw)
    boss.export()
    r45 = boss.write_report()
    boss.save()
    C.log("round45", boss.name, json.dumps({k: v for k, v in r45.items() if k != "parts"}))
    if not args.get("norender") and qa_fn is not None:
        qa_fn(boss)
    return r45


def attach(mboss, parts, makers, prep=None):
    """Build/mech script hook (like gravemaw_maw.attach): bake the parts on the opened boss_mech_kit.Boss, hand them
    to its export() and note them in the report (its report() keeps the round45_detail key)."""
    use_gpu()
    boss = Boss.from_mech(mboss, parts)
    if prep is not None:
        prep(boss)
    for suffix, fn, kw in makers:
        bake_part(boss, suffix, fn(boss), **kw)
    boss.hand_over(mboss)
    boss.write_report()
    return boss


def blockout(boss, builder, suffix, color=(0.8, 0.7, 0.55), glow=(1.0, 0.4, 0.2)):
    """Raw parts in a flat material on the rig (fast placement check, nothing saved)."""
    raw = builder.to_object(f"{boss.name}_{suffix}_Block")
    raw.data.materials.append(C.principled("Block" + suffix, color, rough=0.35, emission=glow, strength=0.4))
    skin_rigid(raw, boss.arm, builder)
    return raw


# ---------------------------------------------------------------------------------------------------------------
# QA renders
# ---------------------------------------------------------------------------------------------------------------

def bone_space(arm, bone):
    bpy.context.view_layer.update()
    pb = arm.pose.bones[bone]
    return arm.matrix_world @ pb.matrix @ arm.data.bones[bone].matrix_local.inverted()


def stage(hide=()):
    for o in list(bpy.data.objects):
        if o.type in ("LIGHT", "CAMERA") or o.name in ("Ground",):
            bpy.data.objects.remove(o)
    C.preview_world()
    C.light_rig(scale=2.5)
    for m in bpy.data.materials:
        m.use_backface_culling = True
    for o in bpy.data.objects:
        if o.type == "MESH" and (o.name.endswith(("_High", "_LOD1")) or o.name in hide):
            o.hide_render = True
    lod_rig = [o for o in bpy.data.objects if o.type == "ARMATURE" and o.name.endswith("_LOD1_Rig")]
    for r in lod_rig:
        for c in r.children:
            c.hide_render = True
    C.ground()


def _cam(location, target, lens):
    data = bpy.data.cameras.new("QACam")
    data.lens = lens
    data.clip_start = 0.01
    cam = C.link(bpy.data.objects.new("QACam", data))
    cam.location = location
    C.look_at(cam, target)
    return cam


def face_camera(arm, bone, face, distance, azimuth, elevation, lens=55):
    """A camera `distance` from the posed point `face` (rest coordinates, carried by `bone`), `azimuth` degrees
    round from straight ahead (the creature looks along -Y), `elevation` up."""
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    m = bone_space(arm, bone)
    target = m @ Vector(face)
    fwd = (m.to_3x3() @ Vector((0, -1, 0)))
    fwd = Vector((fwd.x, fwd.y, 0)).normalized()
    side = Vector((0, 0, 1)).cross(fwd)
    a, e = math.radians(azimuth), math.radians(elevation)
    view = (fwd * math.cos(a) + side * math.sin(a)) * math.cos(e) + Vector((0, 0, math.sin(e)))
    return _cam(target + view * distance, target, lens)


def qa(boss, tag, bone, face, distance, extra_shots=(), idle="Idle", frame=None, game_scale=None, key=None):
    """Front, three-quarter (dramatic, low), high three-quarter and game-camera renders of the face, plus a game view
    at play zoom; a contact sheet ArtSource/Previews/QA/r45_<key>_<tag>.png."""
    os.makedirs(QA, exist_ok=True)
    key = key or boss.name.lower()
    stage()
    if idle not in bpy.data.actions:
        idle = next(n for n in ("Float", "Idle", "Walk") if n in bpy.data.actions)
    act = bpy.data.actions[idle]
    f = frame if frame is not None else int(act.frame_range[1] * 0.3)
    shots = [("front", idle, f, (0, 6)), ("threequarter", idle, f, (38, -4)), ("high", idle, f, (-50, 28))]
    shots += list(extra_shots)
    paths = []
    for label, clip, fr, (az, el) in shots:
        C.set_action(boss.arm, bpy.data.actions[clip], fr)
        cam = face_camera(boss.arm, bone, face, distance, az, el)
        paths.append(C.render(cam, os.path.join(QA, f"r45_{key}_{tag}_{label}.png"), 1280, 720))
    C.set_action(boss.arm, act, f)
    m = bone_space(boss.arm, bone)
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    cam = C.game_camera(m @ Vector(face), distance * 0.9)
    paths.append(C.render(cam, os.path.join(QA, f"r45_{key}_{tag}_game.png"), 1280, 720))
    for o in [o for o in bpy.data.objects if o.type == "CAMERA"]:
        bpy.data.objects.remove(o)
    cam = C.game_camera(Vector((0, 0, 0.5)), game_scale or max(boss.mesh.dimensions) * 1.6)
    paths.append(C.render(cam, os.path.join(QA, f"r45_{key}_{tag}_game_play.png"), 1280, 720))
    C.contact_sheet(paths, os.path.join(QA, f"r45_{key}_{tag}.png"), 3, 640)
    return paths


def clip_check(boss, bone, face, distance, clips, key=None, az=35, el=10):
    """One mid frame of each clip from a three-quarter view of the face (parts must follow, nothing floats)."""
    key = key or boss.name.lower()
    stage()
    paths = []
    for clip in clips:
        act = bpy.data.actions.get(clip)
        if act is None:
            continue
        fr = int(act.frame_range[1] * 0.5)
        C.set_action(boss.arm, act, fr)
        cam = face_camera(boss.arm, bone, face, distance, az, el)
        paths.append(C.render(cam, os.path.join(QA, f"r45_{key}_clip_{clip}.png"), 640, 360, samples=16))
    C.contact_sheet(paths, os.path.join(QA, f"r45_{key}_after_clips.png"), 4, 480)
    for p in paths:
        os.remove(p)
    return os.path.join(QA, f"r45_{key}_after_clips.png")


def qa_lod(boss, bone, face, distance, key=None):
    """LOD1 (body + extras + new parts) at the three-quarter view."""
    key = key or boss.name.lower()
    if boss.lod_arm is None:
        return None
    stage()
    for o in [boss.mesh] + boss.extras + boss.new:
        o.hide_render = True
    for o in [boss.lod_mesh] + boss.lod_extras + boss.new_lod:
        o.hide_render = False
    act = (bpy.data.actions.get("Idle_LOD") or bpy.data.actions.get("Float_LOD")
           or next(iter(boss.lod_clips().values())))
    C.set_action(boss.lod_arm, act, int(act.frame_range[1] * 0.3))
    cam = face_camera(boss.lod_arm, bone if bone in boss.lod_arm.data.bones else boss.lod_arm.data.bones[0].name,
                      face, distance, 38, -4)
    path = C.render(cam, os.path.join(QA, f"r45_{key}_after_lod1.png"), 1280, 720)
    for o in [boss.mesh] + boss.extras + boss.new:
        o.hide_render = False
    for o in [boss.lod_mesh] + boss.lod_extras + boss.new_lod:
        o.hide_render = True
    return path
