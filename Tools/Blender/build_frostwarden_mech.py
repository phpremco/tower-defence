"""Round 28: the Frost Warden's mechanic clips and its ice armour (Docs/BOSSES.md "Frost Warden").

Opens ArtSource/Blend/FrostWarden.blend (round 16: mesh, rig, LOD1, Walk/WalkEnraged/Idle/Hit/Death/FreezePulse/
Enrage) and adds:

- an **ice armour overlay**, `FrostWarden_IceArmour` (a second skinned mesh in the same FBX, own 512 material
  `FrostWarden_IceArmour_Mat`): eight plates of grown ice over chest, shoulders, back and train on bones
  `Armour_1..8`, plus five back icicles on scale bones `Icicle_1..5` (the rage telegraph: Unity scales them
  0.2 -> 1.0 with rage in LateUpdate). Phase "armoured" = renderer on; "exposed" (below 40%, or the 5 s after a
  Lava Ball) = renderer off after Armour_Break (or Armour_Break played and the renderer left on: every plate ends at
  scale 0);
- clips Climb_Up / Climb_Loop / Climb_Down (onto a frozen gate), Dive_Into_Liquid (breaks through an ice sheet:
  the glaives smash it first) / Submerged_Loop / Surface_Burst, Freeze_Gate (breath and aura cast at a gate 2.4
  ahead), Shatter_Through (walks through a frozen gate with travel), Armour_Break (the plates burst off);
- bone `Socket_Breath` (the face slit, pointing forward) for the freeze breath.

Run: blender --background --factory-startup --python Tools/Blender/build_frostwarden_mech.py --
     [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_frostwarden as BW  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import deep_kit as DK  # noqa: E402
import ice_kit as IK  # noqa: E402
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BW.NAME
ARMOUR = NAME + "_IceArmour"
R = math.radians
CLIMB_H = 1.5
GATE_F = 2.6  # round 30: the Warden is 15 % bigger, its glaive tips reach 2.05 ahead
FREEZE_FRAMES, FREEZE_PEAK = 64, 30
SHATTER_CYCLES = 2
BREAK_FRAMES, BREAK_START = 56, 12
DIVE_TRAVEL, BURST_TRAVEL = 2.6, 2.6


# ------------------------------------------------------------------------------------------------------------
# Pose adapter
# ------------------------------------------------------------------------------------------------------------

def body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0,
         halo=None, halo_spin=0.0, arms=None, armour=None, icicles=None, lean=0.0):
    """Generic amounts on the round-16 specs. The hips get the pitch (the round-16 rig has none there) and the torso
    takes most of it back, so the sentinel stays upright on a slope."""
    h = HUNCH_K * rage
    fwd_, out_, el_, bl_ = (R(30) * reach + R(18) * h - R(10) * tuck, R(4) + R(26) * h + R(10) * tuck,
                            R(60) * tuck + R(4) * h, R(10) + R(30) * h - R(40) * tuck)
    sp = BW.specs(off=off + at(0, 0, -0.32 * crouch - 0.12 * h), roll=roll, yaw=yaw,
                  lean=BW.HUNCH["lean"] * h + R(16) * crouch + pitch * 0.75 + lean,
                  neck=BW.HUNCH["neck"] * h - look * 0.5, head=BW.HUNCH["head"] * h - look * 0.5,
                  halo=halo if halo is not None else 1.0 + 0.18 * h, halo_spin=halo_spin,
                  arms=arms or {s: (fwd_, out_, el_, bl_) for s, _ in M.SIDES}, tail=0.0, tail_lift=-pitch * 0.5)
    sp["Hips"]["rot"].append((M.X, -pitch))
    for i in range(len(PLATES)):
        sp[f"Armour_{i + 1}"] = (armour or {}).get(i, {"rot": [], "scale": 1.0})
    for i in range(ICICLES):
        sp[f"Icicle_{i + 1}"] = {"rot": [], "scale": (icicles or {}).get(i, 1.0)}
    return sp


HUNCH_K = 1.0


def adapter():
    return M.Legged(body, [(BW.legs(), "Hips")], [BW.PHASES], BW.STANCE, BW.STRIDE, BW.LIFT, BW.WALK_FRAMES)


# ------------------------------------------------------------------------------------------------------------
# The ice armour (geometry and bones)
# ------------------------------------------------------------------------------------------------------------

SC = BW.SC


def _plates():
    """Round 30 (FW1): eight solid plates of grown ice (hewn `ice_kit.chunk` volumes 15-35 cm deep that sink into
    the body, so no plate floats or shows a card edge), in design space like build_frostwarden (scaled by SC).
    Each plate: parent bone, anchor on its surface, outward direction, chunks (centre, radii, rotation, seed, mirror)
    and spikes (base, direction, radius, length). Armour_1 chest (covers the lower half of the seal glyph),
    Armour_2/3 the left/right pauldrons with a crown of spikes, Armour_4 the back, Armour_5/6 the left/right forearms,
    Armour_7/8 the left/right front shins."""
    out = [{"parent": "Chest", "anchor": G(0, 2.1, 0.72), "dir": G(0, 0.15, 1.0).normalized(),
            "chunks": [(G(0, 2.08, 0.5), (0.52, 0.2, 0.34), T.rot(pitch=-14), 401, False),
                       (G(0, 2.0, 0.62), (0.14, 0.16, 0.34), T.rot(pitch=-14), 402, False)],
            "spikes": [(G(0.36 * sx, 2.32, 0.6), G(0.6 * sx, 0.8, 0.5), 0.06, 0.34) for sx in (-1, 1)]}]
    for side, sx in T.SIDES:
        out.append({"parent": "Chest", "anchor": G(1.02 * sx, 3.26, 0.0), "dir": G(0.45 * sx, 1.0, 0.0).normalized(),
                    "chunks": [(G(0.98 * sx, 3.0, 0.0), (0.6, 0.55, 0.3), T.rot(roll=-22 * sx, pitch=-6), 410, sx < 0),
                               (G(1.32 * sx, 2.68, 0.04), (0.34, 0.46, 0.2), T.rot(roll=-58 * sx), 411, sx < 0)],
                    "spikes": [(G(0.86 * sx, 3.2, -0.08), G(0.25 * sx, 1.0, -0.35), 0.11, 0.85),
                               (G(1.16 * sx, 3.14, 0.12), G(0.6 * sx, 1.0, 0.1), 0.095, 0.66),
                               (G(1.42 * sx, 2.96, -0.12), G(1.0 * sx, 0.7, -0.3), 0.085, 0.56)]})
    out.append({"parent": "Chest", "anchor": G(0, 2.45, -0.6), "dir": G(0, 0.3, -1.0).normalized(),
                "chunks": [(G(0, 2.4, -0.42), (0.5, 0.22, 0.42), T.rot(pitch=12), 420, False)],
                "spikes": [(G(0.2 * sx, 2.3, -0.56), G(0.4 * sx, 0.3, -1.0), 0.055, 0.32) for sx in (-1, 1)]})
    for side, sx in T.SIDES:
        sh, el, wr, tip = BW.arm_pts(sx)
        c = el.lerp(wr, 0.5) + G(0.1 * sx, 0.06, 0.02)
        out.append({"parent": f"Arm_{side}_Fore", "anchor": c + G(0.15 * sx, 0.05, 0), "dir": G(sx, 0.3, 0).normalized(),
                    "chunks": [(c, (0.2, 0.17, 0.36), T.frame_from(wr - el), 430, sx < 0)],
                    "spikes": [(el + G(0.06 * sx, 0.05, -0.04), G(0.3 * sx, 0.3, -1.0), 0.07, 0.42)]})
    lg = BW.legs(1.0)
    for side, sx in T.SIDES:
        leg = lg[(side, 1)]
        hip, knee, foot = leg.rest_points
        c = knee.lerp(foot, 0.4) + G(0.5 * sx, 0, 1.0).normalized() * 0.1
        out.append({"parent": leg.names[1], "anchor": c + G(0.4 * sx, 0, 1.0).normalized() * 0.14,
                    "dir": G(0.4 * sx, 0, 1.0).normalized(),
                    "chunks": [(c, (0.19, 0.15, 0.38), T.frame_from(foot - knee), 440, sx < 0)],
                    "spikes": [(knee + G(0.03 * sx, 0.04, 0.1), G(0.2 * sx, 0.6, 1.0), 0.07, 0.36)]})
    return out


PLATES = _plates()
PLATE_NAMES = ["chest", "left pauldron", "right pauldron", "back", "left forearm", "right forearm",
               "left front shin", "right front shin"]
ICICLES = 5
# The rage icicles on the upper back and between the shoulders (root, direction, length, radius), design space.
ICICLE_ROOTS = [(G(0, 3.0, -0.38), G(0, 1.0, -0.5), 0.95, 0.085),
                (G(-0.28, 3.04, -0.42), G(-0.35, 1.0, -0.5), 0.8, 0.075),
                (G(0.28, 3.04, -0.42), G(0.35, 1.0, -0.5), 0.8, 0.075),
                (G(-0.2, 2.98, -0.55), G(-0.2, 0.8, -0.8), 0.6, 0.07),
                (G(0.2, 2.98, -0.55), G(0.2, 0.8, -0.8), 0.6, 0.07)]


def armour_parts():
    b = C.PartBuilder()
    k = BW.K * 0.5
    for i, pl in enumerate(PLATES):
        bone = f"Armour_{i + 1}"
        for c, r, rot_m, seed, mirror in pl["chunks"]:
            IK.chunk(b, "IcePlate", c, r, rot_m, bone, seed, count=18, depth=0.22, squash=(0.8, 1.0), mirror=mirror, k=k)
        for j, (base, d, rad, ln) in enumerate(pl["spikes"]):
            b.shard("IceSpur", base, d.normalized(), rad, ln, sides=6, flute=0.3, twist=0.5, bone=bone,
                    style=IK.frost_ice(k, seed=450 + 10 * i + j))
    for i, (root, d, ln, rad) in enumerate(ICICLE_ROOTS):
        b.shard("Icicle", root, d.normalized(), rad, ln, sides=6, flute=0.35, twist=0.6, bone=f"Icicle_{i + 1}",
                style=IK.frost_ice(k, seed=480 + i))
    return IK.scale_builder(b, SC)


def armour_bones():
    out = []
    for i, pl in enumerate(PLATES):
        a, d = pl["anchor"], pl["dir"]
        out.append((f"Armour_{i + 1}", BW.sc(a - d * 0.15), BW.sc(a + d * 0.15), pl["parent"]))
    for i, (root, d, ln, rad) in enumerate(ICICLE_ROOTS):
        out.append((f"Icicle_{i + 1}", BW.sc(root), BW.sc(root + d.normalized() * 0.3), "Chest"))
    out.append(("Socket_Breath", BW.sc(G(0, 3.17, 0.42)), BW.sc(G(0, 3.14, 0.75)), "Head"))
    return out


def island_check(obj):
    """Logs the loose islands of the armour and the bones that weigh on each (a plate fused to its neighbour would
    show two Armour bones on one island and could not burst off alone)."""
    mesh = obj.data
    n = len(mesh.vertices)
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", edges)
    parent = np.arange(n)

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for a, b in edges.reshape(-1, 2):
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb
    roots = np.array([find(i) for i in range(n)])
    names = {vg.index: vg.name for vg in obj.vertex_groups}
    per = {}
    for v in mesh.vertices:
        for g in v.groups:
            if g.weight > 0.2:
                per.setdefault(int(roots[v.index]), set()).add(names[g.group])
    mixed = {r: sorted(s) for r, s in per.items() if len(s) > 1}
    C.log(obj.name, "islands", len(set(roots.tolist())), "mixed", list(mixed.values()))
    return len(set(roots.tolist())), list(mixed.values())


def build_armour(boss):
    folder = os.path.join(C.EXPORTS, NAME)
    builder = armour_parts()
    asset = DK.build_mesh_asset_dk(ARMOUR, builder, voxel=0.008, target_tris=3800, tex_size=512, ao_distance=0.2,
                                   extrusion=0.04, ray=0.12, emission_strength=2.0, specular=0.5, k=BW.K * 0.5,
                                   families=("crystal", "rock"), low_size=256, out_dir=folder, smooth_iterations=2)
    bpy.data.objects.remove(bpy.data.objects[ARMOUR + "_High"])
    rigid = {"IcePlate", "IceSpur", "Icicle"}
    C.skin(asset.low, boss.arm, builder, rigid=rigid, blend=0)
    T.island_rigid(asset.low, [f"Armour_{i + 1}" for i in range(len(PLATES))] + [f"Icicle_{i + 1}" for i in range(ICICLES)])
    boss.armour_islands = island_check(asset.low)
    lod_mesh = C.decimate(asset.low.data.copy(), 1700, ARMOUR + "_LOD1", symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(ARMOUR + "_LOD1", lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    C.skin(lod, boss.lod_arm, builder, rigid=rigid, blend=0)
    boss.extras.append(asset.low)
    boss.lod_extras.append(lod)
    return asset, lod


# ------------------------------------------------------------------------------------------------------------
# Boss-specific clips
# ------------------------------------------------------------------------------------------------------------

def freeze_gate(poser):
    """Rises, the chest swells, the glaives open wide behind it and the halo flares; at FREEZE_PEAK it breathes a
    cone of frost from Socket_Breath at the gate GATE_F ahead and drives the glaive tips into the frost line; held to
    44, then it settles."""
    lg = BW.legs()

    def spec(t, f):
        draw = ease(2, 22, f) * (1 - ease(FREEZE_PEAK - 2, FREEZE_PEAK + 2, f))
        cast = ease(FREEZE_PEAK - 4, FREEZE_PEAK, f) * (1 - ease(44, 60, f))
        shake = math.sin(f * 2.4) * pulse(f, FREEZE_PEAK, FREEZE_PEAK + 6, 44)
        arms = {s: (R(-20) * draw + R(40) * cast, R(55) * draw + R(18) * cast, R(20) * draw, R(30) * draw - R(25) * cast)
                for s, _ in M.SIDES}
        sp = body(at(0, -0.1 * draw + 0.18 * cast, 0.08 * draw - 0.1 * cast), lean=-R(14) * draw + R(20) * cast + R(1.5) * shake,
                  look=R(16) * draw - R(10) * cast, halo=1.0 + 0.25 * draw + 0.45 * cast, halo_spin=R(50) * ease(10, 50, f),
                  arms=arms)
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()}, body="Hips")
    return FREEZE_FRAMES, spec, False


def shatter_through(ad):
    """Hunched charge through a frozen gate GATE_F ahead: crossed glaives in front, crest down, two long strides with
    travel; the chest meets the gate at the impact frame (computed), the glaives sweep apart as it breaks."""
    cycles = SHATTER_CYCLES
    cycle = int(round(BW.RAGE_FRAMES * 1.05))
    frames = cycles * cycle
    stride = ad.stride * 1.3
    travel = cycles * stride / ad.stance
    chest_f = -BW.CHEST_C.y * BW.SC + 0.3
    impact_t = min(max((GATE_F - chest_f) / travel, 0.05), 0.95)
    impact = int(round(impact_t * frames))

    def fn(poser):
        def spec(t, f):
            x = travel * t
            w = 2 * math.pi * t * cycles
            brace = ease(0, impact - 6, f) * (1 - ease(impact + 2, impact + 10, f))
            sweep = pulse(f, impact - 1, impact + 5, impact + 20)
            follow = ease(impact + 10, frames, f)
            arms = {s: (R(55) * brace + R(10) * sweep + R(18) * follow, (-R(20) * brace + R(70) * sweep + R(26) * follow) ,
                        R(30) * brace, R(60) * brace + R(10) * sweep + R(40) * follow) for s, _ in M.SIDES}
            sp = body(at(0, x, 0.04 * math.cos(2 * w)), rage=1.0, roll=R(2) * math.sin(w),
                      lean=R(10) * brace - R(8) * sweep, look=-R(12) * brace + R(14) * sweep, arms=arms,
                      halo=1.18 + 0.3 * sweep, halo_spin=R(30) * sweep)
            return ad.place(poser, sp, ad.world_feet(t, cycles, travel, M.FLAT, stride=stride, lift=ad.lift * 1.3))
        return frames, spec, False
    facts = {"gate_forward_m": GATE_F, "impact_frame": impact, "end_offset_forward_m": round(travel, 3),
             "note": "start with the frozen gate GATE_F ahead of the pivot; at impact_frame the gate shatters (wreck, "
                     "shards VFX); at the end move the transform by end_offset_forward and loop WalkEnraged"}
    return fn, facts


def armour_break(poser):
    """A heavy blow staggers it, the plates crack (frames 0-12), burst off one by one from BREAK_START (outwards and
    up, spinning, shrinking to scale 0 within 9 frames), the halo flares, then it roars and drops into the hunch of
    WalkEnraged (the exposed phase)."""
    lg = BW.legs()

    def spec(t, f):
        stagger = K2.recoil(min(t * 3.0, 1.0)) if f < 20 else 0.0
        shudder = math.sin(f * 2.7) * pulse(f, 2, BREAK_START, BREAK_START + 16)
        roar = pulse(f, BREAK_START + 14, BREAK_START + 24, BREAK_FRAMES - 4)
        hunch = ease(BREAK_START + 20, BREAK_FRAMES, f)
        armour = {}
        order = [0, 3, 1, 2, 4, 5, 6, 7]
        for j, i in enumerate(order):
            s0 = BREAK_START + 2 * j
            u = ease(s0, s0 + 9, f)
            d = PLATES[i]["dir"]
            crack = 0.02 * ease(0, BREAK_START, f) * (1 - u)
            armour[i] = {"rot": [(Vector((0.3, 0.2, 1.0)).normalized(), R(160) * u)],
                         "loc": d * (crack + 1.1 * u) + Vector((0, 0, 0.5)) * math.sin(math.pi * min(u, 1.0)),
                         "scale": max(1.0 - u, 0.001)}
        arms = {s: (R(10) * stagger + R(18) * hunch + R(30) * roar, R(8) * stagger + R(26) * hunch + R(40) * roar,
                    R(4) * hunch, R(10) + R(30) * hunch + R(20) * roar) for s, _ in M.SIDES}
        sp = body(at(0, 0.06 * stagger, -0.12 * hunch), lean=-R(8) * stagger + BW.HUNCH["lean"] * hunch + R(2) * shudder,
                  look=R(10) * stagger + R(20) * roar - R(10) * hunch, halo=1.0 + 0.18 * hunch + 0.4 * pulse(f, BREAK_START, BREAK_START + 8, BREAK_START + 30),
                  halo_spin=R(6) * shudder, arms=arms, armour=armour)
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()}, body="Hips")
    return BREAK_FRAMES, spec, False


def ice_dive_extra(t, f, st):
    """Breaking through the ice sheet first: the glaives rise and smash down just before the take-off."""
    if f is None:
        return {}
    rise = ease(0, 12, f) * (1 - ease(14, 18, f))
    smash = ease(14, 18, f) * (1 - ease(26, 36, f))
    return {"arms": {s: (R(140) * rise + R(30) * smash, R(10) * rise, R(30) * rise, R(60) * rise - R(30) * smash)
                     for s, _ in M.SIDES}, "lean": R(24) * smash - R(10) * rise}


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def hold_rest(boss, clips, bones):
    """Constant rest location (0) and rotation (identity) keys for `bones` in `clips` (full and LOD actions), so a
    clip that never animates them does not inherit the pose another clip left behind."""
    lod_names = {b.name for b in boss.lod_arm.data.bones} if boss.lod_arm is not None else set()
    for clip in clips:
        for name, names in ((clip, None), (clip + "_LOD", lod_names)):
            act = bpy.data.actions.get(name)
            if act is None:
                continue
            a, b = act.frame_range
            for bag in K2._channelbags(act)[:1]:
                for bone in bones:
                    if names is not None and bone not in names:
                        continue
                    for prop, values in (("location", (0.0, 0.0, 0.0)), ("rotation_quaternion", (1.0, 0.0, 0.0, 0.0))):
                        path = f'pose.bones["{bone}"].{prop}'
                        for i, v in enumerate(values):
                            fc = bag.fcurves.find(path, index=i)
                            if fc is not None:
                                bag.fcurves.remove(fc)
                            fc = bag.fcurves.new(path, index=i)
                            for f in (a, b):
                                fc.keyframe_points.insert(f, v).interpolation = "CONSTANT"
    C.log(NAME, "held rest pose of", len(bones), "bones in", len(clips), "clips")


def fit_depth(boss, make, name, start, margin=0.2):
    depth = start
    for _ in range(2):
        fn, facts = make(depth)
        act = boss.key(name, fn, facts)
        hi = max(M.mesh_z_range(m, boss.arm, act, M.frames_of(boss.frames[name], 6))[1] for m in [boss.mesh] + boss.extras)
        if hi < -margin + 0.02:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    ad = adapter()
    fn, facts = M.climb_up(ad, CLIMB_H, trail=1.3)
    boss.key("Climb_Up", fn, facts)
    fn, facts = M.climb_loop(ad)
    boss.key("Climb_Loop", fn, facts)
    fn, facts = M.climb_down(ad, CLIMB_H, trail=0.3, lead=1.8)
    boss.key("Climb_Down", fn, facts)
    depth, top = fit_depth(boss, lambda d: M.submerged_clip(ad, d, frames=96), "Submerged_Loop", 3.0)
    boss.info["Submerged_Loop"]["mesh_top_m"] = top
    fn, facts = M.dive_clip(ad, depth, DIVE_TRAVEL, frames=70, takeoff=22, under=56, extra=ice_dive_extra)
    facts["ice_break_frame"] = 18
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = M.burst_clip(ad, depth, BURST_TRAVEL, frames=64, breach=16, land=32)
    boss.key("Surface_Burst", fn, facts)
    boss.key("Freeze_Gate", freeze_gate, {"cast_frame": FREEZE_PEAK, "gate_forward_m": GATE_F, "socket": "Socket_Breath",
                                          "note": "the gate becomes an ice block at cast_frame (8 s, BOSSES.md)"})
    fn, facts = shatter_through(ad)
    boss.key("Shatter_Through", fn, facts)
    boss.key("Armour_Break", armour_break, {"first_plate_frame": BREAK_START, "all_off_frame": BREAK_START + 2 * 7 + 9,
                                            "note": "then hide FrostWarden_IceArmour (or keep it: every plate ends at "
                                                    "scale 0) and loop WalkEnraged; icicles leave with the armour"})
    return ad, depth


def previews(boss, ad, depth, out):
    arm = boss.arm
    if boss.lod_arm:
        for o in [boss.lod_arm, boss.lod_mesh] + boss.lod_extras:
            o.hide_render = True
    M.preview_scene()
    acts = boss.new
    ground = C.ground()
    up = boss.info["Climb_Up"]
    props = M.gate_prop(up["edge_forward_m"] + 0.4, frozen=True, height=CLIMB_H)
    props += M.ledge_props(up["edge_forward_m"], CLIMB_H)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 2.2), 17)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    down = boss.info["Climb_Down"]
    ground.location.z = -CLIMB_H
    props = M.ledge_props(down["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -down["end_offset_forward_m"] * 0.5, 1.2), 17)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    cam2 = C.closeup_camera(Vector((0, 0, 1.8)), 15, azimuth=-35, elevation=20, lens=50)
    M.strip(NAME, arm, acts["Climb_Loop"], cam2, out, "Climb_Loop", M.frames_of(boss.frames["Climb_Loop"], 4))
    M.remove(props + [cam, cam2])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("ice", ad.foot_front + 0.3, length=10)
    cam = M.side_cam((0, -DIVE_TRAVEL * 0.6, 0.0), 18, elevation=22, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("ice", -10, length=10 + BURST_TRAVEL + ad.foot_back - 0.4)
    cam = M.side_cam((0, -BURST_TRAVEL * 0.5, 0.0), 18, elevation=22, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    cam2 = C.closeup_camera(Vector((0, 0.3, -1.8)), 13, azimuth=-35, elevation=55, lens=50)
    M.strip(NAME, arm, acts["Submerged_Loop"], cam2, out, "Submerged_Loop", M.frames_of(boss.frames["Submerged_Loop"], 4))
    M.remove(props + [cam, cam2])
    ground = C.ground()
    props = M.gate_prop(GATE_F)
    cam = C.closeup_camera(Vector((0, -1.2, 1.9)), 15, azimuth=-55, elevation=14, lens=50)
    M.strip(NAME, arm, acts["Freeze_Gate"], cam, out, "Freeze_Gate", [0, 10, 20, 26, FREEZE_PEAK, 38, 48, 62])
    M.remove(props + [cam])
    sh = boss.info["Shatter_Through"]
    props = M.gate_prop(GATE_F, frozen=True)
    cam = M.side_cam((0, -sh["end_offset_forward_m"] * 0.5, 1.9), 17, elevation=10, azimuth=-75)
    imp = sh["impact_frame"]
    M.strip(NAME, arm, acts["Shatter_Through"], cam, out, "Shatter_Through",
            [0, imp // 3, 2 * imp // 3, imp - 2, imp, imp + 6, imp + 14, boss.frames["Shatter_Through"]])
    M.remove(props + [cam])
    cam = C.closeup_camera(Vector((0, 0, 2.0)), 14, azimuth=-40, elevation=16, lens=50)
    M.strip(NAME, arm, acts["Armour_Break"], cam, out, "Armour_Break", [0, 8, BREAK_START + 2, BREAK_START + 6,
                                                                       BREAK_START + 12, BREAK_START + 20, 44, BREAK_FRAMES])
    M.remove([cam])
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, -0.4, 2.1), 17, out, label="turntable_armoured")


def main():
    args = C.cli_args()
    out = C.preview_dir()
    boss = M.Boss(NAME, list(BW.CLIPS), scale_bones=["Halo"])
    ad = adapter()
    boss.add_foot_markers(ad.limbs())
    boss.add_bones(armour_bones())
    boss.scale_bones += [f"Armour_{i + 1}" for i in range(len(PLATES))] + [f"Icicle_{i + 1}" for i in range(ICICLES)]
    asset, lod = build_armour(boss)
    ad, depth = key_all(boss)
    # Round 29: the round-16 clips never keyed the armour plates and icicles, so the FBX exporter baked them with the
    # pose the previous clip left (Armour_Break's end: plates at scale 0). Key them at 1; only Armour_Break removes them.
    boss.hold(list(BW.CLIPS), {n: 1.0 for n in [f"Armour_{i + 1}" for i in range(len(PLATES))] +
                               [f"Icicle_{i + 1}" for i in range(ICICLES)]})
    # Round 30: the same holds for location and rotation (the exporter also baked Armour_Break's last pose, plates
    # flown off, into the old clips for the unkeyed channels).
    hold_rest(boss, list(BW.CLIPS), [f"Armour_{i + 1}" for i in range(len(PLATES))] + [f"Icicle_{i + 1}" for i in range(ICICLES)])
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "ice (water under an ice sheet)", "submerged_depth_m": depth,
                 "ice_armour": {"object": ARMOUR, "material": f"{ARMOUR}_Mat", "maps": f"{ARMOUR}_*.png (512, Low 256)",
                                "tris": C.tri_count(asset.low), "lod1_object": ARMOUR + "_LOD1",
                                "lod1_tris": C.tri_count(lod), "plates": [f"Armour_{i + 1}" for i in range(len(PLATES))],
                                "icicles": [f"Icicle_{i + 1}" for i in range(ICICLES)],
                                "icicle_rage_scale": "0.2 (calm) to 1.0 (max rage), uniform localScale in LateUpdate",
                                "round30_plate_map": {f"Armour_{i + 1}": f"{n} (parent {p['parent']})" for i, (n, p) in
                                                      enumerate(zip(PLATE_NAMES, PLATES))},
                                "islands": boss.armour_islands[0],
                                "note": "round 30: solid faceted ice volumes sunk into the body; plates, icicles and "
                                        "the rest pose are keyed in every old clip (only Armour_Break moves them)"},
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "mechanic": "Gate Breaker: Freeze_Gate, Climb_Up onto the frozen gate (it sags), Shatter_Through; "
                             "ice armour soaks damage until Armour_Break (below 40% or a Lava Ball)"})
    boss.save()
    if not args.get("norender"):
        previews(boss, ad, depth, out)


if __name__ == "__main__":
    main()
