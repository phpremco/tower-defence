"""Round 28 helpers: the continent bosses get the clips their signature mechanics need (Docs/BOSSES.md). Kept apart
from the shared modules (common, boss_kit, titan_kit, deep_kit, creature_kit2/3), which this module only imports.

The round-28 scripts do not rebake anything. Each one opens the boss's saved `.blend` (game mesh, rig, LOD1 rig and
the round 14/16 clips), adds foot marker bones, keys new clips with the boss build script's own pose functions,
keys the LOD1 rig from the full rig's pose, exports both FBX files with the old and the new clips, merges the
round-28 facts into the report and saves the `.blend` again. Running a script twice gives the same result: clips
and markers it made before are removed first.

- `Boss`: open, add markers, key clips (full + LOD), export (optionally with extra skinned meshes), report, save.
- `Legged`: a legged boss described for the generic clips: its build script's `specs(...)` wrapped as
  `body(off, pitch, roll, yaw, **amounts)`, its leg groups, its gait. Generic clips over it: `climb_clip` (up, loop,
  down over a ledge with planted feet), `dive_clip`, `submerged_clip`, `burst_clip`, `stride_clip`.
- Terrain profiles (`Profile`) for clips whose feet walk on a height field (the ledge, the bank).
- Preview staging: `ledge_props`, `pool_props`, `gate_prop`, `tower_prop`, side/3-4 cameras, `strip` renders and a
  `turntable` contact sheet.

Frames are Blender's: game forward is -Y (common.G), so a clip's forward travel is `off.y = -distance`. In Unity
(FBX -Z forward, Y up, Bake Axis Conversion) forward is the prefab's +Z before the creature-import turn; Unity's
creature import turns the model 180 degrees about Y (head at -Z), and every forward offset in the round-28 reports
is along the creature's own facing, whichever way the prefab is turned.
"""

import json
import math
import os

import bpy
import bmesh
from mathutils import Matrix, Vector

import common as C
import creature_kit2 as K2
import creature_kit3 as K3

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
SIDES = (("L", -1), ("R", 1))
ROUND = 28


def ease(a, b, t):
    return K2.ease(a, b, t)


def pulse(t, a, p, b):
    return K2.pulse(t, a, p, b)


def sstep(a, b, x):
    if b == a:
        return 1.0 if x >= a else 0.0
    s = min(max((x - a) / (b - a), 0.0), 1.0)
    return s * s * (3 - 2 * s)


def fwd(v):
    """Game forward of a Blender-space point."""
    return -v.y


def at(x, forward, up):
    """Blender-space point from (x, game forward, up)."""
    return Vector((x, -forward, up))


# ---------------------------------------------------------------------------------------------------------------
# Opening, keying, exporting
# ---------------------------------------------------------------------------------------------------------------

class Boss:
    """A boss opened from ArtSource/Blend. `original`: the clip names its build script made (they are exported
    unchanged); `scale_bones`/`clip_scales`: as in the build script (round-28 clips may add bones)."""

    def __init__(self, name, original, scale_bones=(), clip_scales=None, lod_aims=None):
        self.name = name
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects.get(name + "_LOD1_Rig")
        self.lod_mesh = bpy.data.objects.get(name + "_LOD1")
        self.original = list(original)
        self.scale_bones = list(scale_bones)
        self.clip_scales = clip_scales
        self.lod_aims = lod_aims or {}
        self.new = {}
        self.new_lod = {}
        self.frames = {}
        self.info = {}
        self.extras = []
        self.lod_extras = []
        self._poser = None
        self._lod_poser = None
        self._forget_round28()

    def _forget_round28(self):
        """Removes what a previous round-28 run left in the .blend (clips, markers, extra meshes)."""
        path = os.path.join(C.EXPORTS, self.name, self.name + "_report.json")
        old = {}
        if os.path.exists(path):
            with open(path, encoding="utf-8") as fh:
                old = json.load(fh).get("round28", {})
        for clip in old.get("clips", {}):
            for n in (clip, clip + "_LOD"):
                act = bpy.data.actions.get(n)
                if act is not None:
                    bpy.data.actions.remove(act)
        for n in old.get("extra_objects", []):
            o = bpy.data.objects.get(n)
            if o is not None:
                bpy.data.objects.remove(o)
        markers = old.get("markers", [])
        for arm in (self.arm, self.lod_arm):
            if arm is None:
                continue
            present = [m for m in markers if m in arm.data.bones]
            if present:
                C.activate(arm)
                bpy.ops.object.mode_set(mode="EDIT")
                for m in present:
                    arm.data.edit_bones.remove(arm.data.edit_bones[m])
                bpy.ops.object.mode_set(mode="OBJECT")
        self.markers = []

    # -- bones ----------------------------------------------------------------------------------------------------

    def add_bones(self, bones, scale=False):
        """Adds bones [(name, head, tail, parent)] to both rigs (the LOD rig re-parents to the nearest bone it has).
        They deform nothing (no vertex group) but are flagged deform so the FBX exporter keeps them."""
        for arm in (self.arm, self.lod_arm):
            if arm is None:
                continue
            full_parent = {b.name: (b.parent.name if b.parent else None) for b in self.arm.data.bones}
            C.activate(arm)
            bpy.ops.object.mode_set(mode="EDIT")
            eb = arm.data.edit_bones
            for name, head, tail, parent in bones:
                if name in eb:
                    continue
                p = parent
                while p is not None and p not in eb:
                    p = full_parent.get(p)
                b = eb.new(name)
                b.head, b.tail = Vector(head), Vector(tail)
                b.roll = 0.0
                b.use_deform = True
                if p:
                    b.parent = eb[p]
                    b.use_connect = False
            bpy.ops.object.mode_set(mode="OBJECT")
            for pb in arm.pose.bones:
                pb.rotation_mode = "QUATERNION"
        for name, *_ in bones:
            if name not in self.markers:
                self.markers.append(name)
            if scale and name not in self.scale_bones:
                self.scale_bones.append(name)
        self._poser = self._lod_poser = None

    def add_foot_markers(self, limbs, up=0.25):
        """One `IK_<limb>` bone per limb, at the rest foot contact, pointing up; parent = the limb's last bone.
        Unity reads it as the contact to raycast and pin (foot IK on the climb layer)."""
        bones = []
        for lg in limbs:
            last = lg.names[-1]
            tip = self.arm.data.bones[last].tail_local
            bones.append((ik_name(lg), tip.copy(), tip + Vector((0, 0, up)), last))
        self.add_bones(bones)
        return [b[0] for b in bones]

    # -- posers ---------------------------------------------------------------------------------------------------

    @property
    def poser(self):
        if self._poser is None:
            self._poser = K3.ClipPoser(self.arm, self.scale_bones, self.clip_scales)
        return self._poser

    @property
    def lod_poser(self):
        if self._lod_poser is None and self.lod_arm is not None:
            names = {b.name for b in self.lod_arm.data.bones}
            cs = None
            if self.clip_scales is not None:
                cs = {k: [n for n in v if n in names] for k, v in self.clip_scales.items()}
            self._lod_poser = K3.ClipPoser(self.lod_arm, [b for b in self.scale_bones if b in names], cs)
        return self._lod_poser

    def key(self, clip, fn, info=None):
        """Keys `clip` from `fn(poser) -> (frames, spec, loop)` on both rigs."""
        for n in (clip, clip + "_LOD"):
            act = bpy.data.actions.get(n)
            if act is not None:
                bpy.data.actions.remove(act)
        frames, spec, loop = fn(self.poser)
        self.poser.use(clip)
        self.new[clip] = self.poser.action(clip, frames, spec, loop=loop)
        if self.lod_poser is not None:
            self.lod_poser.use(clip)
            self.new_lod[clip] = C.lod_action(self.poser, self.lod_poser, clip + "_LOD", frames, spec, loop,
                                              self.lod_aims)
        self.frames[clip] = frames
        if info:
            self.info[clip] = dict(info, frames=frames, loop=loop)
        else:
            self.info[clip] = {"frames": frames, "loop": loop}
        C.log(self.name, "keyed", clip, frames)
        return self.new[clip]

    def hold(self, clips, values):
        """Round 29: keys constant bone scales {bone: uniform scale} into `clips` (full and LOD actions). The FBX
        exporter bakes a bone that a clip does not key with whatever pose the previous clip left behind, so bones added
        after a clip was made (armour plates, seal stones, rubble) must be keyed in every clip explicitly."""
        lod_names = {b.name for b in self.lod_arm.data.bones} if self.lod_arm is not None else set()
        for clip in clips:
            act = bpy.data.actions.get(clip)
            if act is not None:
                hold_scale(act, values)
            lod = bpy.data.actions.get(clip + "_LOD")
            if lod is not None:
                hold_scale(lod, {b: v for b, v in values.items() if b in lod_names})
        C.log(self.name, "held scales", len(values), "bones in", len(clips), "clips")

    def actions(self):
        out = {n: bpy.data.actions[n] for n in self.original}
        out.update(self.new)
        return out

    def lod_actions(self):
        out = {n: bpy.data.actions[n + "_LOD"] for n in self.original if (n + "_LOD") in bpy.data.actions}
        out.update(self.new_lod)
        return out

    # -- export ---------------------------------------------------------------------------------------------------

    def export(self):
        folder = os.path.join(C.EXPORTS, self.name)
        export_rig(os.path.join(folder, self.name + ".fbx"), self.arm, [self.mesh] + self.extras, self.actions())
        if self.lod_arm is not None:
            export_rig(os.path.join(folder, self.name + "_LOD1.fbx"), self.lod_arm, [self.lod_mesh] + self.lod_extras,
                       self.lod_actions())

    def report(self, data):
        path = os.path.join(C.EXPORTS, self.name, self.name + "_report.json")
        with open(path, encoding="utf-8") as fh:
            rep = json.load(fh)
        rep["actions"] = {n: int(round(bpy.data.actions[n].frame_range[1])) for n in self.original}
        rep["actions"].update(self.frames)
        rep["bones"] = len(self.arm.data.bones)
        if "lod1" in rep and self.lod_arm is not None:
            rep["lod1"]["bones"] = len(self.lod_arm.data.bones)
        r28 = {"clips": self.info, "markers": self.markers,
               "extra_objects": [o.name for o in self.extras + self.lod_extras]}
        r28.update(data)
        rep["round28"] = r28
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=2)
        C.log("report", path)

    def save(self):
        C.save_blend(self.name)


def hold_scale(act, values):
    """Constant scale keys {bone: uniform scale} over the action's frame range (replacing any scale curves)."""
    a, b = act.frame_range
    for bag in K2._channelbags(act)[:1]:
        for bone, v in values.items():
            path = f'pose.bones["{bone}"].scale'
            for i in range(3):
                fc = bag.fcurves.find(path, index=i)
                if fc is not None:
                    bag.fcurves.remove(fc)
                fc = bag.fcurves.new(path, index=i)
                for f in (a, b):
                    k = fc.keyframe_points.insert(f, v)
                    k.interpolation = "CONSTANT"


def ik_name(lg):
    base = lg.names[0].rsplit("_", 1)[0]
    return "IK_" + base


def export_rig(path, arm, meshes, names):
    """creature_kit2.export_rig with several meshes (all skinned to `arm`)."""
    keep = set(names.values())
    guards = []
    for act in bpy.data.actions:
        if act in keep:
            continue
        for bag in K2._channelbags(act):
            if bag.fcurves:
                guards.append((bag, bag.fcurves.new(K2.GUARD_PATH, index=0)))
    taken = {}
    for clip in names:
        other = bpy.data.actions.get(clip)
        if other is not None and other is not names[clip]:
            taken[clip] = other
            other.name = clip + "_Away"
    old = {act: act.name for act in names.values()}
    for clip, act in names.items():
        act.name = clip
    C.set_action(arm, next(iter(names.values())), 0)
    bpy.context.view_layer.update()
    C.export_fbx(path, [arm] + list(meshes), animated=True)
    for act, n in old.items():
        act.name = n
    for clip, other in taken.items():
        other.name = clip
    for bag, fc in guards:
        bag.fcurves.remove(fc)


def export_static(path, objs):
    bpy.context.view_layer.update()
    C.export_fbx(path, list(objs), animated=False)


# ---------------------------------------------------------------------------------------------------------------
# Terrain profiles
# ---------------------------------------------------------------------------------------------------------------

class Profile:
    """Ground height along game forward: 0 before the edge at `edge`, `height` after it (negative = a drop), the
    face `face` wide. `body(f)` is a wider ramp the body's height follows (it starts rising before the face)."""

    def __init__(self, edge, height, face=0.16, lead=1.1, trail=0.6):
        self.edge = edge
        self.height = height
        self.face = face
        self.lead = lead
        self.trail = trail

    def h(self, f):
        return self.height * sstep(self.edge - self.face * 0.5, self.edge + self.face * 0.5, f)

    def body(self, f):
        if self.height >= 0:
            return self.height * sstep(self.edge - self.lead, self.edge + self.trail, f)
        return self.height * sstep(self.edge - self.trail, self.edge + self.lead, f)

    def snap(self, f, before=0.42, after=0.3):
        """A foot never lands on the face or the lip: it lands short of it or past it."""
        if self.edge - before < f < self.edge + after:
            return self.edge - before if f < self.edge - 0.05 else self.edge + after
        return f


FLAT = Profile(1e6, 0.0)


# ---------------------------------------------------------------------------------------------------------------
# A legged boss for the generic clips
# ---------------------------------------------------------------------------------------------------------------

class Legged:
    """`body(off, pitch, roll, yaw, **amounts) -> specs`; amounts the boss understands: crouch (0..1, body low and
    braced), look (radians, head up > 0), mouth (0..1), rage (0..1), tuck (0..1, limbs drawn in), reach (0..1, fore
    limbs forward). `groups`: [(legs dict, body bone)]; `phases`: {key: phase} per group; gait as the boss's Walk."""

    def __init__(self, body, groups, phases, stance, stride, lift, cycle, hip_span=None):
        self.body = body
        self.groups = groups
        self.phases = phases
        self.stance = stance
        self.stride = stride
        self.lift = lift
        self.cycle = cycle
        self.legs = [((gi, key), lg, bone) for gi, (legs, bone) in enumerate(groups) for key, lg in legs.items()]
        fs = [fwd(lg.root) for _, lg, _ in self.legs]
        self.hip_front, self.hip_back = (hip_span or (max(fs), min(fs)))
        feet = [fwd(lg.foot) for _, lg, _ in self.legs]
        self.foot_front, self.foot_back = max(feet), min(feet)

    def limbs(self):
        return [lg for _, lg, _ in self.legs]

    def phase(self, key):
        gi, k = key
        return self.phases[gi][k]

    def place(self, poser, sp, feet, poles=None):
        for gi, (legs, bone) in enumerate(self.groups):
            pose = poser.solve(sp)
            move = pose[bone] @ poser.rest[bone].inverted()
            for key, lg in legs.items():
                pole = poles[(gi, key)] if poles and (gi, key) in poles else move.to_3x3() @ lg.pole
                sp.update(lg.pose(poser, move @ lg.root, feet[(gi, key)], pole))
        return sp

    def carried(self, poser, sp, tuck=0.0, spread=0.0, reach=0.0, paddle=None):
        """Feet carried by their body bone (rest offset), drawn in towards the hip by `tuck`, pushed out by
        `spread`, the fore limbs forward by `reach`; `paddle(key) -> Vector` adds a stroke."""
        feet = {}
        for gi, (legs, bone) in enumerate(self.groups):
            pose = poser.solve(sp)
            move = pose[bone] @ poser.rest[bone].inverted()
            for key, lg in legs.items():
                rest = lg.foot.copy()
                d = rest - lg.root
                sx = 1 if lg.foot.x >= 0 else -1
                p = lg.root + d * (1 - 0.55 * tuck) + Vector((0, 0, 0.35 * tuck * d.length * 0.4))
                p = p + Vector((sx * spread * 0.4, 0, 0))
                if fwd(lg.root) >= (self.hip_front + self.hip_back) / 2:
                    p = p + at(0, reach * 0.6, reach * 0.35)
                if paddle:
                    p = p + paddle((gi, key))
                feet[(gi, key)] = move @ p
        return feet

    def planted(self, travel=0.0, rise=0.0):
        return {k: lg.foot + at(0, travel, rise) for k, lg, _ in self.legs}

    def gait(self, t, cycles=1, stride=None, lift=None, width=1.0):
        """In-place feet (the boss's own gait shape), feet pushed out by `width`."""
        stride = stride or self.stride
        lift = lift or self.lift
        out = {}
        for key, lg, _ in self.legs:
            fw, up = C.step_curve((t * cycles + self.phase(key)) % 1.0, self.stance, stride, lift)
            p = lg.foot + at(0, fw, up)
            p.x = lg.root.x + (p.x - lg.root.x) * width
            out[key] = p
        return out

    def world_feet(self, t, cycles, travel_total, profile, stride=None, lift=None, width=1.0):
        """Planted world feet for a clip that travels `travel_total` forward at a constant speed over `cycles`
        gait cycles; feet heights follow `profile` and never land on its face."""
        stride = stride or self.stride
        lift = lift or self.lift
        speed = travel_total  # metres per unit t
        out = {}
        for key, lg, _ in self.legs:
            p = self.phase(key)
            r = fwd(lg.foot)
            g_abs = t * cycles + p
            n = math.floor(g_abs)
            g = g_abs - n

            def anchor(m):
                t_td = (m - p) / cycles
                return profile.snap(r + stride * 0.5 + speed * t_td)
            a0 = anchor(n)
            x = lg.root.x + (lg.foot.x - lg.root.x) * width
            if g < self.stance:
                out[key] = at(x, a0, profile.h(a0))
                continue
            s = (g - self.stance) / (1 - self.stance)
            a1 = anchor(n + 1)
            e = (1 - math.cos(math.pi * s)) * 0.5
            f = a0 + (a1 - a0) * e
            z0, z1 = profile.h(a0), profile.h(a1)
            zl = z0 + (z1 - z0) * e
            peak = max(z0, z1, profile.h(f))
            out[key] = at(x, f, zl + (peak - zl + lift) * math.sin(math.pi * s))
        return out

    def body_on(self, profile, x):
        """Body height and pitch over `profile` with the body's front hips at x + hip_front."""
        hf = profile.body(x + self.hip_front)
        hb = profile.body(x + self.hip_back)
        span = max(self.hip_front - self.hip_back, 0.5)
        return (hf + hb) * 0.5, math.atan2(hf - hb, span)


# ---------------------------------------------------------------------------------------------------------------
# Generic clips for a Legged boss
# ---------------------------------------------------------------------------------------------------------------

def cycles_for(ad, distance, slow, stride_k=1.0):
    cycle = int(round(ad.cycle * slow))
    per = ad.stride * stride_k / ad.stance
    return max(1, math.ceil(distance / per)), cycle


def climb_up(ad, height, slow=1.1, margin=0.35, crouch=0.35, look=0.25, extra=None, stride_k=1.3, lead=1.1, trail=0.6):
    """Over a ledge `height` high whose face is `edge` ahead (the front feet start 0.45 short of it): N full gait
    cycles at the boss's stride, so the last frame is the boss's gait pose at phase 0, `travel` forward and `height`
    up. `extra(t, f, state) -> amounts` adds the boss's own effort (claws, head). Returns (fn, facts)."""
    edge = ad.foot_front + 0.45
    need = edge + margin + 0.3 - ad.foot_back
    cycles, cycle = cycles_for(ad, need, slow, stride_k)
    stride = ad.stride * stride_k
    frames = cycles * cycle
    travel = cycles * stride / ad.stance
    prof = Profile(edge, height, lead=lead, trail=trail)

    def fn(poser):
        def spec(t, f):
            x = travel * t
            z, pitch = ad.body_on(prof, x)
            near = pulse(x + ad.hip_front, edge - 1.0, edge, edge + 1.2)
            amounts = {"crouch": crouch * near, "look": look * near, "reach": 0.4 * near}
            if extra:
                amounts.update(extra(t, f, {"near": near, "pitch": pitch}))
            sp = ad.body(off=at(0, x, z), pitch=pitch, **amounts)
            feet = ad.world_feet(t, cycles, travel, prof, stride=stride)
            return ad.place(poser, sp, feet)
        return frames, spec, False
    facts = {"end_offset_forward_m": round(travel, 3), "end_offset_up_m": height, "edge_forward_m": round(edge, 3),
             "ledge_height_m": height, "cycles": cycles,
             "note": "authored with the pivot fixed; at the last frame move the transform by the end offset and "
                     "continue with Climb_Loop (pivot on the top surface) or Walk"}
    return fn, facts


def climb_down(ad, height, slow=1.1, margin=0.35, crouch=0.3, extra=None, stride_k=1.3, lead=1.1, trail=0.6):
    """Down a drop of `height` whose lip is 0.45 ahead of the front feet; ends `travel` forward, `height` lower."""
    edge = ad.foot_front + 0.45
    need = edge + margin + 0.3 - ad.foot_back
    cycles, cycle = cycles_for(ad, need, slow, stride_k)
    stride = ad.stride * stride_k
    frames = cycles * cycle
    travel = cycles * stride / ad.stance
    prof = Profile(edge, -height, lead=lead, trail=trail)

    def fn(poser):
        def spec(t, f):
            x = travel * t
            z, pitch = ad.body_on(prof, x)
            near = pulse(x + ad.hip_front, edge - 0.6, edge + 0.6, edge + 2.0)
            amounts = {"crouch": crouch * near, "look": -0.2 * near}
            if extra:
                amounts.update(extra(t, f, {"near": near, "pitch": pitch}))
            sp = ad.body(off=at(0, x, z), pitch=pitch, **amounts)
            return ad.place(poser, sp, ad.world_feet(t, cycles, travel, prof, stride=stride))
        return frames, spec, False
    facts = {"end_offset_forward_m": round(travel, 3), "end_offset_up_m": -height, "edge_forward_m": round(edge, 3),
             "drop_height_m": height, "cycles": cycles,
             "note": "starts with the pivot on the top surface (Climb_Loop / Climb_Up end pose); at the last frame "
                     "move the transform by the end offset"}
    return fn, facts


def climb_loop(ad, slow=1.45, crouch=0.6, width=1.12, stride_k=0.8, extra=None):
    """The braced crawl on top of a rock, gate or roof: lower, wider, shorter steps, in place (no root motion)."""
    cycle = int(round(ad.cycle * slow))
    stride = ad.stride * stride_k

    def fn(poser):
        def spec(t, f):
            w = 2 * math.pi * t
            amounts = {"crouch": crouch, "look": -0.05 + 0.08 * math.sin(w)}
            if extra:
                amounts.update(extra(t, f, {}))
            sp = ad.body(off=at(0, 0, 0.03 * math.cos(2 * w)), pitch=0.0, roll=math.radians(3) * math.sin(w), **amounts)
            return ad.place(poser, sp, ad.gait(t, 1, stride=stride, lift=ad.lift * 1.1, width=width))
        return cycle, spec, True
    facts = {"distance_per_loop_m": round(stride / ad.stance, 3),
             "playback": f"speed / ({round(stride / ad.stance, 3)} x scale) loops per second x {cycle} frames",
             "note": "pivot on the top surface; in place; pin the IK_* markers to the climb layer"}
    return fn, facts


def stride_clip(ad, slow=0.9, stride_k=1.25, lift_k=1.1, extra=None, cycles=1):
    """A purposeful in-place stride (longer steps, a touch faster cadence)."""
    cycle = int(round(ad.cycle * slow)) * cycles
    stride = ad.stride * stride_k

    def fn(poser):
        def spec(t, f):
            w = 2 * math.pi * t * cycles
            amounts = {}
            if extra:
                amounts.update(extra(t, f, {"w": w}))
            sp = ad.body(off=at(0, 0, 0.05 * math.cos(2 * w) - 0.06), pitch=-math.radians(3),
                         roll=math.radians(3) * math.sin(w), yaw=math.radians(2) * math.sin(w), **amounts)
            return ad.place(poser, sp, ad.gait(t, cycles, stride=stride, lift=ad.lift * lift_k))
        return cycle, spec, True
    return fn, {"distance_per_loop_m": round(stride * cycles / ad.stance, 3)}


def submerged_pose(ad, poser, t, depth, extra=None):
    w = 2 * math.pi * t
    amounts = {"crouch": 1.0, "tuck": 0.7, "look": 0.1}
    if extra:
        amounts.update(extra(t, None, {"w": w}))
    sp = ad.body(off=at(0.06 * math.sin(w), 0.05 * math.sin(w), -depth + 0.06 * math.sin(2 * w)),
                 pitch=math.radians(2) * math.sin(w), roll=math.radians(5) * math.sin(w + 0.7),
                 yaw=math.radians(4) * math.sin(w), **amounts)

    def paddle(key):
        ph = 2 * math.pi * (t + ad.phase(key))
        return at(0, 0.18 * math.cos(ph), 0.1 * math.sin(ph))
    return ad.place(poser, sp, ad.carried(poser, sp, tuck=0.7, paddle=paddle))


def submerged_clip(ad, depth, frames=72, extra=None):
    def fn(poser):
        def spec(t, f):
            return submerged_pose(ad, poser, t, depth, extra)
        return frames, spec, True
    return fn, {"depth_m": depth, "note": "pivot on the liquid surface; everything stays under it; in place"}


def dive_clip(ad, depth, travel, frames=64, takeoff=20, under=52, extra=None):
    """From the bank (pivot on the liquid level, the bank edge just ahead of the front feet): crouch, a short hop,
    a plunge nose first; ends in Submerged_Loop frame 0, `travel` forward."""
    def fn(poser):
        def spec(t, f):
            crouch = pulse(f, 0, takeoff - 4, takeoff + 6)
            go = ease(takeoff - 2, under, f)
            x = travel * go
            hop = 0.35 * pulse(f, takeoff - 2, takeoff + 6, takeoff + 16)
            z = -depth * ease(takeoff + 2, under, f) ** 1.4 + hop - 0.15 * crouch
            pitch = -math.radians(38) * pulse(f, takeoff, takeoff + 14, under + 4) + math.radians(8) * crouch
            settle = ease(under - 6, frames, f)
            amounts = {"crouch": max(crouch, settle), "tuck": 0.7 * ease(takeoff + 2, takeoff + 18, f),
                       "look": -0.25 * pulse(f, takeoff, takeoff + 10, under), "mouth": 0.6 * pulse(f, 4, 14, takeoff + 8)}
            if extra:
                amounts.update(extra(t, f, {"go": go}))
            sp = ad.body(off=at(0, x, z), pitch=pitch, **amounts)
            planted = ad.planted()
            if f <= takeoff:
                return ad.place(poser, sp, planted)
            carried = ad.carried(poser, sp, tuck=0.7 * ease(takeoff, takeoff + 18, f))
            k = ease(takeoff, takeoff + 8, f)
            feet = {key: planted[key].lerp(carried[key], k) for key in planted}
            if f >= under:
                # Blend into the Submerged_Loop pose (shifted by travel) so the clips join.
                b = ease(under, frames, f)
                sp_end = submerged_pose(ad, poser, 0.0, depth, extra)
                sp_end = shift(poser, sp_end, travel)
                return blend_specs(poser, ad.place(poser, sp, feet), sp_end, b)
            return ad.place(poser, sp, feet)
        return frames, spec, False
    return fn, {"end_offset_forward_m": travel, "depth_m": depth, "takeoff_frame": takeoff,
                "splash_frame": takeoff + 8, "under_frame": under,
                "note": "at the last frame move the transform by end_offset_forward and loop Submerged_Loop"}


def burst_clip(ad, depth, travel, frames=60, breach=14, land=30, extra=None):
    """Out of the liquid: starts in the Submerged_Loop pose under the pivot, surges forward and up through the
    surface (breach), lands on the bank `travel` ahead (feet planted from `land`) and stands up."""
    def fn(poser):
        def spec(t, f):
            rumble = math.sin(f * 2.3) * pulse(f, 0, 6, breach)
            up = ease(4, breach + 6, f)
            x = travel * ease(breach - 4, land, f)
            arc = 0.55 * pulse(f, breach, breach + 8, land + 2)
            absorb = pulse(f, land - 2, land + 4, land + 18)
            z = -depth * (1 - up) + arc - 0.25 * absorb
            pitch = math.radians(30) * pulse(f, breach - 4, breach + 4, land) - math.radians(6) * absorb
            amounts = {"crouch": max(1 - ease(0, breach, f), absorb), "tuck": 0.7 * (1 - ease(breach - 2, breach + 8, f)),
                       "look": 0.3 * pulse(f, breach - 2, breach + 6, land + 10), "mouth": pulse(f, breach - 2, breach + 6, land + 12),
                       "rage": pulse(f, breach, land, frames)}
            if extra:
                amounts.update(extra(t, f, {"up": up}))
            sp = ad.body(off=at(0.03 * rumble, x, z), pitch=pitch, **amounts)
            if f < breach + 2:
                sub = submerged_pose(ad, poser, 0.0, depth, None)
                k = ease(0, breach + 2, f)
                carried = ad.carried(poser, sp, tuck=0.7 * (1 - k), reach=k, spread=0.3 * k)
                feet = carried
                spd = ad.place(poser, sp, feet)
                return blend_specs(poser, sub, spd, ease(0, 6, f))
            carried = ad.carried(poser, sp, reach=1 - ease(breach + 2, land, f), spread=0.3)
            planted = ad.planted(travel)
            k = ease(land - 6, land, f)
            feet = {key: carried[key].lerp(planted[key], k) for key in planted}
            return ad.place(poser, sp, feet)
        return frames, spec, False
    return fn, {"start_depth_m": depth, "end_offset_forward_m": travel, "breach_frame": breach, "land_frame": land,
                "note": "start under the exit point (the pivot on the liquid surface, facing the bank); the splash "
                        "on nearby towers at breach_frame, the camera kick at land_frame; move the transform by "
                        "end_offset_forward at the end and go to Walk"}


def shift(poser, specs, travel):
    """The same pose moved `travel` forward (abs-solved, so it can be blended)."""
    pose = poser.solve(specs)
    m = Matrix.Translation(at(0, travel, 0))
    return abs_specs({n: m @ p for n, p in pose.items()})


def abs_specs(pose):
    """Solved armature-space matrices back into specs: rotation and translation as `abs`, scale split out (the
    ScalePoser puts it back on top)."""
    out = {}
    for n, p in pose.items():
        loc, rot, sc = p.decompose()
        spec = {"abs": Matrix.LocRotScale(loc, rot, Vector((1, 1, 1)))}
        if max(abs(sc.x - 1), abs(sc.y - 1), abs(sc.z - 1)) > 1e-4:
            spec["scale"] = (sc.x, sc.y, sc.z)
        out[n] = spec
    return out


def blend_specs(poser, a, b, k):
    """Bone-wise blend of two spec sets (armature-space matrices: lerped translation, slerped rotation, lerped
    scale)."""
    pa, pb = poser.solve(a), poser.solve(b)
    mixed = {}
    for n in pa:
        la, ra, sa = pa[n].decompose()
        lb, rb, sb = pb[n].decompose()
        mixed[n] = Matrix.LocRotScale(la.lerp(lb, k), ra.slerp(rb, k), sa.lerp(sb, k))
    return abs_specs(mixed)


# ---------------------------------------------------------------------------------------------------------------
# Preview staging
# ---------------------------------------------------------------------------------------------------------------

def mat(name, color, rough=0.8, emission=None, strength=0.0, alpha=1.0):
    m = C.principled(name, color, rough=rough, emission=emission, strength=strength)
    if alpha < 1.0:
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Alpha"].default_value = alpha
        for attr, val in (("surface_render_method", "BLENDED"), ("blend_method", "BLEND")):
            try:
                setattr(m, attr, val)
            except (AttributeError, TypeError):
                pass
    return m


def box(name, x0, x1, f0, f1, z0, z1, material, bevel=0.06):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector(((x0 + x1) / 2 + v.co.x * (x1 - x0), -((f0 + f1) / 2 + v.co.y * (f1 - f0)),
                       (z0 + z1) / 2 + v.co.z * (z1 - z0)))
    if bevel:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=2, affect="EDGES")
    bm.to_mesh(me)
    bm.free()
    o = C.link(bpy.data.objects.new(name, me))
    o.data.materials.append(material)
    return o


def rock_mat():
    return mat("PropRock", (0.16, 0.15, 0.17), rough=0.9)


def ground_hole(f0, f1, x0, x1, size=60.0, color=(0.03, 0.035, 0.045), z=0.0):
    """The preview ground with a rectangular hole (for a pool)."""
    m = mat("PropGround", color, rough=0.95)
    S = size
    parts = [(-S, S, -S, f0), (-S, S, f1, S), (-S, x0, f0, f1), (x1, S, f0, f1)]
    objs = []
    for i, (a, b, c, d) in enumerate(parts):
        objs.append(box(f"GroundPart{i}", a, b, c, d, z - 0.4, z, m, bevel=0))
    return objs


LIQUIDS = {"lava": ((0.85, 0.2, 0.03), 0.35, 0.72), "bog": ((0.12, 0.22, 0.05), 0.12, 0.72),
           "water": ((0.05, 0.14, 0.24), 0.05, 0.6), "ice": ((0.4, 0.55, 0.72), 0.08, 0.55),
           "quicksand": ((0.38, 0.28, 0.16), 0.0, 0.85), "void": ((0.2, 0.06, 0.35), 0.2, 0.7)}


def pool_props(kind, bank, length=9.0, half=4.5, depth=6.5):
    """A pool from `bank` forward to `bank + length` (liquid surface at z = 0) with a floor and walls."""
    col, glow, alpha = LIQUIDS[kind]
    objs = ground_hole(bank, bank + length, -half, half)
    floor = mat("PoolFloor", (0.04, 0.035, 0.04), rough=0.9)
    objs.append(box("PoolFloor", -half, half, bank, bank + length, -depth - 0.3, -depth, floor, bevel=0))
    for i, (a, b, c, d) in enumerate(((-half - 0.3, -half, bank, bank + length), (half, half + 0.3, bank, bank + length))):
        objs.append(box(f"PoolWall{i}", a, b, c, d, -depth, 0.0, floor, bevel=0))
    objs.append(box("PoolWallBank", -half, half, bank - 0.3, bank, -depth, 0.0, floor, bevel=0))
    liquid = mat("Liquid_" + kind, col, rough=0.15, emission=col, strength=glow, alpha=alpha * 0.6)
    objs.append(plane("Liquid", -half, half, bank, bank + length, 0.0, liquid))
    return objs


def plane(name, x0, x1, f0, f1, z, material):
    me = bpy.data.meshes.new(name)
    me.from_pydata([at(x0, f0, z), at(x1, f0, z), at(x1, f1, z), at(x0, f1, z)], [], [(0, 3, 2, 1)])
    o = C.link(bpy.data.objects.new(name, me))
    o.data.materials.append(material)
    return o


def ledge_props(edge, height, length=10.0, half=4.0, down=False):
    """A rock ledge: its face at `edge`; up: the top is `height` high beyond the face; down: the pivot side is the
    top and the ground `height` lower beyond the face (the preview ground then sits at -height)."""
    m = rock_mat()
    if down:
        return [box("LedgeTop", -half, half, edge - length, edge, -height - 0.4, 0.0, m)]
    return [box("Ledge", -half, half, edge, edge + length, -0.4, height, m)]


def gate_prop(forward, frozen=False, height=1.8, width=3.2):
    """Two pylons and a beam where a gate stands (frozen: a block of ice round it)."""
    m = mat("PropGate", (0.35, 0.38, 0.42), rough=0.5, emission=(0.4, 0.7, 1.0), strength=0.3)
    objs = [box("GatePylonL", -width / 2 - 0.3, -width / 2, forward - 0.3, forward + 0.3, 0, height, m),
            box("GatePylonR", width / 2, width / 2 + 0.3, forward - 0.3, forward + 0.3, 0, height, m),
            box("GateBeam", -width / 2, width / 2, forward - 0.12, forward + 0.12, height * 0.55, height * 0.75, m)]
    if frozen:
        ice = mat("PropIce", (0.6, 0.8, 1.0), rough=0.1, emission=(0.5, 0.8, 1.0), strength=0.6, alpha=0.55)
        objs.append(box("GateIce", -width / 2 - 0.5, width / 2 + 0.5, forward - 0.6, forward + 0.6, 0, height + 0.3, ice))
    return objs


def tower_prop(x, forward, height=1.4, name="Tower"):
    m = mat("PropTower", (0.75, 0.78, 0.82), rough=0.4, emission=(0.3, 0.7, 1.0), strength=0.4)
    d = mat("PropTowerDark", (0.12, 0.13, 0.16), rough=0.6)
    return [box(name + "Base", x - 0.45, x + 0.45, forward - 0.45, forward + 0.45, 0, 0.35, d),
            box(name + "Body", x - 0.3, x + 0.3, forward - 0.3, forward + 0.3, 0.35, height, m)]


def remove(objs):
    for o in objs:
        if o.name in bpy.data.objects:
            bpy.data.objects.remove(o)


def side_cam(target, distance, elevation=8.0, azimuth=-90.0, lens=50.0):
    return C.closeup_camera(Vector(target), distance, azimuth=azimuth, elevation=elevation, lens=lens)


def strip(boss_name, arm, act, cam, out, label, frames, columns=4, cell=420, samples=16):
    """`<out>/<Boss>_<label>.png`: the frames of `act` from `cam`, laid out in a row of `columns`."""
    paths = []
    for i, f in enumerate(frames):
        C.set_action(arm, act, f)
        paths.append(C.render(cam, os.path.join(out, f"_{boss_name}_{label}_{i}.png"), 640, 360, samples=samples,
                              bloom=True))
    path = os.path.join(out, f"{boss_name}_{label}.png")
    C.contact_sheet(paths, path, columns, cell)
    for p in paths:
        os.remove(p)
    return path


def turntable(boss_name, arm, act, frame, target, distance, out, label="turntable", elevation=20.0, samples=16,
              hide=()):
    paths = []
    for o in hide:
        o.hide_render = True
    C.set_action(arm, act, frame)
    for i, az in enumerate((-38, -90, -145, 180, 145, 90, 38, 0)):
        cam = C.closeup_camera(Vector(target), distance, azimuth=az, elevation=elevation, lens=55)
        paths.append(C.render(cam, os.path.join(out, f"_{boss_name}_tt_{i}.png"), 640, 360, samples=samples))
        bpy.data.objects.remove(cam)
    game = C.game_camera(Vector((0, 0, 0.5)), 640 / C.GAME_PIXELS_PER_UNIT)
    paths.append(C.render(game, os.path.join(out, f"_{boss_name}_tt_game.png"), 640, 360, samples=samples))
    bpy.data.objects.remove(game)
    path = os.path.join(out, f"{boss_name}_{label}.png")
    C.contact_sheet(paths, path, 3, 420)
    for p in paths:
        os.remove(p)
    for o in hide:
        o.hide_render = False
    return path


def preview_scene(hide_names=()):
    """World, lights; hides every object but the boss rigs and meshes we render (the High bake sources, LOD)."""
    for o in bpy.data.objects:
        if o.name.endswith("_High") or o.name in hide_names:
            o.hide_render = True
    C.preview_world()
    return C.light_rig(scale=4.0)


def frames_of(n, count=8):
    """`count` frames spread over 0..n."""
    return [int(round(n * i / (count - 1))) for i in range(count)]


# ---------------------------------------------------------------------------------------------------------------
# Small rigged props and static props (one material, 512 maps + 256 Low)
# ---------------------------------------------------------------------------------------------------------------

def small_rig(name, builder, bones, clips, tris, *, families=("rock", "membrane", "chitin"), rigid=(), blend=1,
              scale_bones=(), clip_scales=None, voxel=0.006, ao=0.08, extrusion=0.02, ray=0.05, emission=2.6,
              specular=0.3, k=1.0, extra=None, mirror=True, tex=512, low=256, report=None, unity_scale=1.0,
              symmetric=None):
    """Bake, skin, key and export a small rigged prop (egg, pod, shard). Resets the scene first. Returns
    (asset, arm, actions, poser)."""
    C.reset_scene()
    asset = C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=tex, ao_distance=ao,
                               extrusion=extrusion, ray=ray, emission_strength=emission, extra=extra,
                               specular=specular, mirror=mirror, k=k, families=families, low_size=low,
                               symmetric=mirror if symmetric is None else symmetric)
    arm = C.build_armature(name + "_Rig", bones)
    C.skin(asset.low, arm, builder, rigid=rigid, blend=blend)
    poser = K3.ClipPoser(arm, scale_bones, clip_scales)
    actions = K3.key_clips(poser, clips)
    export_rig(os.path.join(C.EXPORTS, name, name + ".fbx"), arm, [asset.low], actions)
    data = {"asset": name, "round": ROUND, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
            "bones": len(arm.data.bones), "texture_size": tex, "low_texture_size": low,
            "maps": ["Albedo", "Normal", "Emission", "Mask"], "material": f"{name}_Mat", "uv": asset.uv_stats,
            "detail": asset.details, "actions": {n: clips[n](poser)[0] for n in clips}, "unity_scale": unity_scale,
            "scale_keyed_bones": list(scale_bones), "size_m": [round(d, 3) for d in asset.low.dimensions]}
    data.update(report or {})
    C.write_report(name, data)
    C.save_blend(name)
    return asset, arm, actions, poser


def static_asset(name, builder, tris, *, families=("rock", "membrane", "chitin"), voxel=0.006, ao=0.1,
                 extrusion=0.02, ray=0.05, emission=2.4, specular=0.3, k=1.0, extra=None, mirror=False, tex=512,
                 low=256, reset=True):
    """Bake one static prop mesh (no rig); exported by the caller. Returns the asset."""
    if reset:
        C.reset_scene()
    return C.build_mesh_asset(name, builder, voxel=voxel, target_tris=tris, tex_size=tex, ao_distance=ao,
                              extrusion=extrusion, ray=ray, emission_strength=emission, extra=extra, specular=specular,
                              mirror=mirror, k=k, families=families, low_size=low, symmetric=mirror)


def mesh_z_range(mesh, arm, act, frames):
    """(min z, max z) of the deformed mesh over `frames` of `act` (to check that a submerged pose is under)."""
    lo, hi = 1e9, -1e9
    dg = bpy.context.evaluated_depsgraph_get()
    for f in frames:
        C.set_action(arm, act, f)
        dg.update()
        ev = mesh.evaluated_get(dg)
        me = ev.to_mesh()
        zs = [(ev.matrix_world @ v.co).z for v in me.vertices]
        lo, hi = min(lo, min(zs)), max(hi, max(zs))
        ev.to_mesh_clear()
    return lo, hi
