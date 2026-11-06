"""The Frost Warden's mechanic clips and its ice armour (Docs/BOSSES.md "Frost Warden"). Round 28, rebuilt in round 32
for the Rime Colossus body (build_frostwarden.py).

Opens ArtSource/Blend/FrostWarden.blend (mesh, rig, LOD1, Walk/WalkEnraged/Idle/Hit/Death/FreezePulse/Enrage) and adds:

- an **ice armour overlay**, `FrostWarden_IceArmour` (a second skinned mesh in the same FBX, own 512 material
  `FrostWarden_IceArmour_Mat`): eight hewn ice floes on bones `Armour_1..8` (1 front back floe between the shoulders,
  2/3 left/right pauldron, 4 rear back floe over the hips, 5/6 left/right gauntlet over the forearm, 7/8 left/right
  thigh floe) plus the five rage icicles of the hump crest on scale bones `Icicle_1..5` (Unity scales them 0.2 -> 1.0
  with rage in LateUpdate). Phase "armoured" = renderer on; "exposed" = after Armour_Break (every floe ends at scale 0);
- foot markers `IK_Arm_L/R` (knuckle contacts) and `IK_Leg_L/R` (hind toes), `Socket_Breath` (the maw, forward);
- clips Climb_Up / Climb_Loop / Climb_Down (onto the frozen gate: the fists hook over the top first),
  Dive_Into_Liquid (the fists smash the ice sheet first) / Submerged_Loop / Surface_Burst, Freeze_Gate (inhales,
  thrusts the head low and breathes at the gate), Shatter_Through (rears up with the fists curled over its head and
  smashes down through the frozen gate, then lunges through it), Armour_Break (the floes burst off).

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
import deep_kit as DK  # noqa: E402
import ice_kit as IK  # noqa: E402
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BW.NAME
ARMOUR = NAME + "_IceArmour"
R = math.radians
CLIMB_H = 1.5
GATE_F = 4.3          # the gate (and the ice block, 2.07 deep) this far ahead of the pivot; the head reaches 2.9
FREEZE_FRAMES, FREEZE_PEAK = 64, 30
SHATTER_FRAMES, SHATTER_IMPACT, SHATTER_END = 100, 35, 3.8
BREAK_FRAMES, BREAK_START = 56, 12
DIVE_TRAVEL, BURST_TRAVEL = 2.8, 2.8
SIDES = T.SIDES


# ------------------------------------------------------------------------------------------------------------
# Pose adapter
# ------------------------------------------------------------------------------------------------------------

def plates_spec(armour=None, icicles=None):
    sp = {}
    for i in range(len(PLATES)):
        sp[f"Armour_{i + 1}"] = (armour or {}).get(i, {"rot": [], "scale": 1.0})
    for i in range(ICICLES):
        sp[f"Icicle_{i + 1}"] = {"rot": [], "scale": (icicles or {}).get(i, 1.0)}
    return sp


def body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0,
         armour=None, icicles=None, lean=0.0, chest=0.0):
    """Generic amounts (boss_mech_kit) on the build script's specs. `pitch` > 0 is nose up here (a slope); the
    shoulders take a little of it back so the head stays on the target."""
    sp = BW.specs(off=off + at(0, -0.1 * rage - 0.08 * crouch, -0.32 * crouch - 0.18 * rage), roll=roll, yaw=yaw,
                  pitch=-pitch + R(6) * rage + R(7) * crouch + lean, spine=R(4) * rage + R(4) * crouch,
                  chest=pitch * 0.3 + R(4) * rage + chest, neck=R(6) * rage - look * 0.45,
                  head=-R(4) * rage - look * 0.55, jaw=R(32) * mouth + R(10) * rage)
    sp.update(plates_spec(armour, icicles))
    return sp


def adapter():
    groups = [(BW.ARMS, "Chest"), (BW.LEGS, "Hips")]
    phases = [{s: BW.PHASES[("arm", s)] for s, _ in SIDES}, {s: BW.PHASES[("leg", s)] for s, _ in SIDES}]
    return M.Legged(body, groups, phases, BW.STANCE, BW.STRIDE, BW.LIFT, BW.WALK_FRAMES)


def to_bw(feet):
    """Legged feet {(group, side)} -> build_frostwarden feet {("arm"|"leg", side)}."""
    return {(("arm", "leg")[g], s): p for (g, s), p in feet.items()}


# ------------------------------------------------------------------------------------------------------------
# The ice armour (geometry and bones), game space G(x, up, forward)
# ------------------------------------------------------------------------------------------------------------

def _plates():
    """Eight floes of grown ice (hewn `ice_kit.chunk` volumes sunk into the body). Each: parent bone, anchor,
    outward direction, chunks (centre, radii, rotation, seed, mirror) and spikes (base, direction, radius, length)."""
    out = [{"parent": "Chest", "anchor": G(0, 3.98, 0.55), "dir": G(0, 1.0, 0.25).normalized(),
            "chunks": [(G(0, 3.84, 0.6), (0.92, 0.66, 0.34), T.rot(pitch=24), 401, False)] +
                      [(G(0.52 * sx, 3.62, 0.72), (0.34, 0.36, 0.22), T.rot(pitch=16, roll=-20 * sx), 402, sx < 0)
                       for sx in (-1, 1)],
            "spikes": [(G(0.3 * sx, 3.92, 0.72), G(0.35 * sx, 1.0, 0.5), 0.09, 0.42) for sx in (-1, 1)]}]
    for side, sx in SIDES:
        out.append({"parent": f"Arm_{side}_Upper", "anchor": G(1.68 * sx, 3.86, 0.78), "dir": G(0.5 * sx, 1.0, 0).normalized(),
                    "chunks": [(G(1.64 * sx, 3.56, 0.8), (0.9, 0.82, 0.46), T.rot(roll=-28 * sx), 410, sx < 0),
                               (G(2.08 * sx, 3.02, 0.82), (0.46, 0.66, 0.36), T.rot(roll=-62 * sx), 411, sx < 0)],
                    "spikes": [(G(1.64 * sx, 3.98, 0.6), G(0.35 * sx, 1.0, -0.3), 0.15, 0.85),
                               (G(2.15 * sx, 3.5, 0.95), G(1.0 * sx, 0.6, 0.25), 0.13, 0.62)]})
    out.append({"parent": "Spine", "anchor": G(0, 3.28, -1.4), "dir": G(0, 0.6, -1.0).normalized(),
                "chunks": [(G(0, 3.1, -1.38), (0.9, 0.66, 0.34), T.rot(pitch=-40), 420, False)],
                "spikes": [(G(0.3 * sx, 3.2, -1.55), G(0.4 * sx, 0.6, -1.0), 0.08, 0.36) for sx in (-1, 1)]})
    for side, sx in SIDES:
        sh, el, wr, kn = BW.arm_pts(sx)
        c = el.lerp(wr, 0.56) + G(0.24 * sx, 0.02, 0.14)
        out.append({"parent": f"Arm_{side}_Fore", "anchor": c + G(0.45 * sx, 0, 0), "dir": G(sx, 0.1, 0.3).normalized(),
                    "chunks": [(c, (0.66, 0.6, 0.9), BW.along(el, wr), 430, sx < 0),
                               (el.lerp(wr, 0.88) + G(0.18 * sx, 0.0, 0.3), (0.42, 0.36, 0.34), BW.along(el, wr), 431, sx < 0)],
                    "spikes": [(el.lerp(wr, 0.3) + G(0.42 * sx, 0.05, -0.12), G(0.9 * sx, 0.35, -0.55), 0.11, 0.5),
                               (el.lerp(wr, 0.62) + G(0.5 * sx, 0.0, 0.0), G(1.0 * sx, 0.2, -0.2), 0.1, 0.4)]})
    for side, sx in SIDES:
        hp, kn, an, toe = BW.leg_pts(sx)
        c = hp.lerp(kn, 0.45) + G(0.5 * sx, 0.12, -0.05)
        out.append({"parent": f"Leg_{side}_Upper", "anchor": c + G(0.3 * sx, 0, 0), "dir": G(sx, 0.3, -0.2).normalized(),
                    "chunks": [(c, (0.36, 0.62, 0.7), BW.along(hp, kn) @ T.rot(roll=0), 440, sx < 0)],
                    "spikes": [(c + G(0.2 * sx, 0.3, -0.25), G(0.7 * sx, 0.5, -0.7), 0.09, 0.4)]})
    return out


PLATES = _plates()
PLATE_NAMES = ["front back floe (between the shoulders)", "left pauldron", "right pauldron",
               "rear back floe (over the hips)", "left gauntlet (forearm)", "right gauntlet (forearm)",
               "left thigh floe", "right thigh floe"]
ICICLES = 5
# The rage-icicle crest on the hump ridge (root, direction, length, radius): centre, left, right, rear left, rear right.
ICICLE_ROOTS = [(G(0, 4.06, -0.15), G(0, 1.0, -0.35), 1.15, 0.15),
                (G(-0.4, 4.0, -0.3), G(-0.35, 1.0, -0.45), 0.92, 0.13),
                (G(0.4, 4.0, -0.3), G(0.35, 1.0, -0.45), 0.92, 0.13),
                (G(-0.3, 3.86, -0.78), G(-0.3, 0.9, -0.7), 0.72, 0.11),
                (G(0.3, 3.86, -0.78), G(0.3, 0.9, -0.7), 0.72, 0.11)]
BREATH = (G(0, 2.32, 2.62), G(0, 2.2, 2.98))


def armour_parts():
    b = C.PartBuilder()
    k = BW.K * 0.5
    for i, pl in enumerate(PLATES):
        bone = f"Armour_{i + 1}"
        for c, r, rot_m, seed, mirror in pl["chunks"]:
            IK.chunk(b, "IcePlate", c, r, rot_m, bone, seed, count=18, depth=0.22, squash=(0.8, 1.0), mirror=mirror, k=k,
                     **BW.ICE_LOOK)
        for j, (base, d, rad, ln) in enumerate(pl["spikes"]):
            b.shard("IceSpur", base, d.normalized(), rad, ln, sides=6, flute=0.3, twist=0.5, bone=bone,
                    style=IK.frost_ice(k, seed=450 + 10 * i + j, **BW.ICE_LOOK))
    for i, (root, d, ln, rad) in enumerate(ICICLE_ROOTS):
        b.shard("Icicle", root, d.normalized(), rad, ln, sides=6, flute=0.35, twist=0.6, bone=f"Icicle_{i + 1}",
                style=IK.frost_ice(k, seed=480 + i, **dict(BW.ICE_LOOK, crack_glow=0.55)))
    return b


def armour_bones():
    out = []
    for i, pl in enumerate(PLATES):
        a, d = pl["anchor"], pl["dir"]
        out.append((f"Armour_{i + 1}", a - d * 0.15, a + d * 0.15, pl["parent"]))
    for i, (root, d, ln, rad) in enumerate(ICICLE_ROOTS):
        out.append((f"Icicle_{i + 1}", root, root + d.normalized() * 0.3, "Spine"))
    out.append(("Socket_Breath", BREATH[0], BREATH[1], "Head"))
    return out


def island_check(obj):
    """Logs the loose islands of the armour and the bones that weigh on each (a floe fused to its neighbour would
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
    asset = DK.build_mesh_asset_dk(ARMOUR, builder, voxel=0.009, target_tris=3800, tex_size=512, ao_distance=0.25,
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
    """Gate Breaker, the cast: plants the fists wide, rears back and fills its chest (frames 2-22, the head lifts,
    the maw shut), then thrusts the wedge head low and forward and breathes at the gate GATE_F ahead at FREEZE_PEAK
    (maw wide, held and shaking to 44), then settles."""
    def spec(t, f):
        draw = ease(2, 22, f) * (1 - ease(FREEZE_PEAK - 5, FREEZE_PEAK, f))
        cast = ease(FREEZE_PEAK - 5, FREEZE_PEAK, f) * (1 - ease(44, 62, f))
        shake = math.sin(f * 2.4) * pulse(f, FREEZE_PEAK, FREEZE_PEAK + 4, 44)
        sp = BW.specs(off=G(0, 0.18 * draw - 0.22 * cast, -0.25 * draw + 0.3 * cast),
                      pitch=-R(14) * draw + R(9) * cast + R(1) * shake, spine=-R(5) * draw + R(3) * cast,
                      chest=-R(8) * draw + R(6) * cast, neck=-R(12) * draw + R(16) * cast,
                      head=-R(14) * draw - R(10) * cast + R(1.5) * shake, jaw=R(40) * cast + R(4) * draw)
        sp.update(plates_spec())
        feet = BW.planted()
        spread = ease(0, 10, f) * (1 - ease(50, 62, f))
        for side, sx in SIDES:
            feet[("arm", side)] = BW.arc(feet[("arm", side)], feet[("arm", side)] + G(0.35 * sx, 0, 0.15), spread, 0.25)
        return BW.place(poser, sp, feet)
    return FREEZE_FRAMES, spec, False


def shatter_through(poser):
    """Gate Breaker, the smash: crouches (to 10), rears up on the hind legs with both fists curled over its head
    (8-26), holds, hammers both fists down onto the frozen gate GATE_F ahead (they hit its top at SHATTER_IMPACT and
    drive through to the ground), lunges through after them and walks out SHATTER_END ahead in the WalkEnraged
    carriage."""
    end = SHATTER_END
    hit_top = {sx: G(0.55 * sx, 2.45, GATE_F - 0.55) for _, sx in SIDES}
    hit_ground = {sx: G(0.62 * sx, 0.0, GATE_F - 0.3) for _, sx in SIDES}
    leg_steps = {"L": [(44, 60, 1.65), (76, 92, 3.8 - 1.65)], "R": [(52, 68, 1.85), (84, 98, 3.8 - 1.85)]}
    arm_steps = {"L": (64, 80), "R": (72, 88)}

    def leg_at(side, rest, f):
        p = rest.copy()
        for a, b, d in leg_steps[side]:
            if f >= b:
                p = p + G(0, 0, d)
            elif f > a:
                return BW.arc(p, p + G(0, 0, d), (f - a) / (b - a), 0.3)
        return p

    def spec(t, f):
        gather = pulse(f, 0, 8, 14)
        rear = ease(8, 26, f) * (1 - ease(31, SHATTER_IMPACT, f))
        lunge = ease(31, 37, f) * (1 - ease(44, 64, f))
        travel = 0.6 * ease(31, 40, f) + (end - 0.6) * ease(40, 96, f)
        rage = ease(56, 90, f)
        shiver = math.sin(f * 3.1) * pulse(f, 24, 28, 32)
        sp = BW.specs(off=G(0, 0.32 * rear - 0.12 * gather - 0.25 * lunge - 0.18 * rage,
                            travel - 0.2 * gather - 0.4 * rear - 0.1 * rage),
                      pitch=R(5) * gather - R(32) * rear + R(16) * lunge + R(6) * rage + R(1) * shiver,
                      spine=-R(8) * rear + R(6) * lunge + R(4) * rage, chest=-R(12) * rear + R(10) * lunge + R(4) * rage,
                      neck=R(10) * rear + R(6) * rage, head=-R(18) * rear + R(6) * lunge - R(4) * rage,
                      jaw=R(34) * pulse(f, 18, 30, 44) + R(10) * rage)
        sp.update(plates_spec())
        feet, fists = {}, {}
        for side, sx in SIDES:
            rest = BW.ARMS[side].foot
            top = BW.overhead(sx, up=5.3, fwd=0.2, spread=0.42)
            up_dir, down_dir = BW.gdir((-0.35 * sx, 1.0, -0.5)), BW.gdir((0.0, -1.0, 0.3))
            a0, a1 = arm_steps[side]
            if f <= 26:
                feet[("arm", side)] = BW.arc(rest, top, ease(8, 26, f), 0.5)
                fists[side] = BW.turn(BW.gdir(BW.FIST_DIR, sx), up_dir, ease(8, 22, f), BW.FIST_OVER)
            elif f <= SHATTER_IMPACT:
                s = ease(31, SHATTER_IMPACT, f)
                feet[("arm", side)] = top.lerp(hit_top[sx], s) + G(0, 0.7 * math.sin(math.pi * s), 0.5 * math.sin(math.pi * s))
                fists[side] = BW.turn(up_dir, down_dir, s, BW.FIST_OVER)
            elif f <= 40:
                feet[("arm", side)] = hit_top[sx].lerp(hit_ground[sx], ease(SHATTER_IMPACT, 40, f))
                fists[side] = down_dir
            elif f <= a1:
                feet[("arm", side)] = BW.arc(hit_ground[sx], rest + G(0, 0, end), (f - a0) / (a1 - a0), 0.55) if f > a0 \
                    else hit_ground[sx]
                fists[side] = BW.turn(down_dir, BW.gdir(BW.FIST_DIR, sx), ease(a0, a1, f))
            else:
                feet[("arm", side)] = rest + G(0, 0, end)
                fists[side] = BW.gdir(BW.FIST_DIR, sx)
            feet[("leg", side)] = leg_at(side, BW.LEGS[side].foot, f)
        return BW.place(poser, sp, feet, fists)
    facts = {"gate_forward_m": GATE_F, "impact_frame": SHATTER_IMPACT, "end_offset_forward_m": end,
             "fists_overhead_frames": "22-31", "fists_through_to_ground_frame": 40,
             "note": "start with the frozen gate (ice block) GATE_F ahead of the pivot; the fists hit its top at "
                     "impact_frame: shatter it into the wreck (shards VFX, camera kick); at the end move the "
                     "transform by end_offset_forward and loop WalkEnraged"}
    return SHATTER_FRAMES, spec, False, facts


def armour_break(poser):
    """A heavy blow staggers it, the floes crack (0-12) and burst off one by one from BREAK_START (outwards and up,
    spinning, shrinking to scale 0 within 9 frames), it rears and roars, and drops into the WalkEnraged hunch."""
    def spec(t, f):
        stagger = K2.recoil(min(t * 3.0, 1.0)) if f < 20 else 0.0
        shudder = math.sin(f * 2.7) * pulse(f, 2, BREAK_START, BREAK_START + 16)
        roar = pulse(f, BREAK_START + 12, BREAK_START + 22, BREAK_FRAMES - 4)
        hunch = ease(BREAK_START + 24, BREAK_FRAMES, f)
        armour = {}
        order = [0, 3, 1, 2, 4, 5, 6, 7]
        for j, i in enumerate(order):
            s0 = BREAK_START + 2 * j
            u = ease(s0, s0 + 9, f)
            d = PLATES[i]["dir"]
            crack = 0.02 * ease(0, BREAK_START, f) * (1 - u)
            armour[i] = {"rot": [(Vector((0.3, 0.2, 1.0)).normalized(), R(160) * u)],
                         "loc": d * (crack + 1.3 * u) + Vector((0, 0, 0.6)) * math.sin(math.pi * min(u, 1.0)),
                         "scale": max(1.0 - u, 0.001)}
        sp = BW.specs(off=G(0, 0.05 * stagger + 0.22 * roar - 0.18 * hunch, -0.12 * stagger - 0.2 * roar - 0.1 * hunch),
                      pitch=-R(6) * stagger - R(18) * roar + R(6) * hunch + R(2) * shudder, roll=R(3) * stagger,
                      spine=-R(4) * roar + R(4) * hunch, chest=-R(6) * stagger - R(8) * roar + R(4) * hunch,
                      neck=-R(8) * stagger + R(10) * roar + R(6) * hunch, head=-R(8) * stagger - R(22) * roar - R(4) * hunch,
                      jaw=R(14) * max(stagger, 0) + R(38) * roar + R(10) * hunch)
        sp.update(plates_spec(armour))
        feet = BW.planted()
        for side, sx in SIDES:
            feet[("arm", side)] = feet[("arm", side)] + G(0.25 * sx * roar, 0.35 * roar, -0.15 * roar)
        return BW.place(poser, sp, feet)
    return BREAK_FRAMES, spec, False


def ice_dive_extra(t, f, st):
    if f is None:
        return {}
    return {"look": R(10) * pulse(f, 0, 10, 18)}


def dive_with_smash(ad, depth, takeoff=22):
    """The generic dive (boss_mech_kit.dive_clip), with the fists curled over the head (0-12) and hammered down onto
    the ice sheet just past the bank at 18 (it breaks); from the take-off they blend back into the generic dive."""
    fn, facts = M.dive_clip(ad, depth, DIVE_TRAVEL, frames=70, takeoff=takeoff, under=56, extra=ice_dive_extra)
    smash_f = ad.foot_front + 0.55

    def wrapped(poser):
        frames, spec0, loop = fn(poser)

        def spec(t, f):
            sp = spec0(t, f)
            if f >= takeoff + 8:
                return sp
            w = 1.0 - ease(takeoff, takeoff + 8, f)
            pose = poser.solve(sp)
            chest = pose["Chest"] @ poser.rest["Chest"].inverted()
            for side, sx in SIDES:
                a = BW.ARMS[side]
                rest = a.foot
                top = BW.overhead(sx, up=4.9, fwd=0.6)
                hit = G(0.62 * sx, 0.0, smash_f)
                if f <= 12:
                    p, d = BW.arc(rest, top, ease(0, 12, f), 0.4), BW.turn(BW.gdir(BW.FIST_DIR, sx), BW.gdir((-0.4 * sx, 1, -0.4)), ease(0, 11, f))
                elif f <= 18:
                    s = ease(12, 18, f)
                    p, d = top.lerp(hit, s) + G(0, 0.6 * math.sin(math.pi * s), 0.3 * math.sin(math.pi * s)), \
                        BW.turn(BW.gdir((-0.4 * sx, 1, -0.4)), BW.gdir((0, -1, 0.3)), s)
                else:
                    p, d = hit, BW.gdir((0, -1, 0.3))
                tip = pose[a.names[-1]] @ poser.rest[a.names[-1]].inverted() @ (rest)
                p = tip.lerp(p, w) if w < 1 else p
                saved = a.foot_dir
                a.foot_dir = d
                sp.update(a.pose(poser, chest @ a.root, p, chest.to_3x3() @ a.pole))
                a.foot_dir = saved
            return sp
        return frames, spec, loop
    facts["ice_break_frame"] = 18
    return wrapped, facts


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
    hi = 0.0
    for _ in range(3):
        fn, facts = make(depth)
        act = boss.key(name, fn, facts)
        hi = max(M.mesh_z_range(m, boss.arm, act, M.frames_of(boss.frames[name], 6))[1] for m in [boss.mesh] + boss.extras)
        if hi < -margin + 0.02:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    ad = adapter()
    fn, facts = M.climb_up(ad, CLIMB_H, trail=1.3, extra=lambda t, f, st: {"reach": 0.8 * st["near"]})
    facts["note"] += "; the fists hook over the top of the frozen gate first, the hind legs push it up after them"
    boss.key("Climb_Up", fn, facts)
    fn, facts = M.climb_loop(ad)
    boss.key("Climb_Loop", fn, facts)
    fn, facts = M.climb_down(ad, CLIMB_H, trail=0.3, lead=1.8)
    boss.key("Climb_Down", fn, facts)
    depth, top = fit_depth(boss, lambda d: M.submerged_clip(ad, d, frames=96), "Submerged_Loop", 3.0)
    boss.info["Submerged_Loop"]["mesh_top_m"] = top
    fn, facts = dive_with_smash(ad, depth)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = M.burst_clip(ad, depth, BURST_TRAVEL, frames=64, breach=16, land=32)
    boss.key("Surface_Burst", fn, facts)
    boss.key("Freeze_Gate", freeze_gate, {"cast_frame": FREEZE_PEAK, "gate_forward_m": GATE_F, "socket": "Socket_Breath",
                                          "breath_hold_frames": f"{FREEZE_PEAK}-44",
                                          "note": "the gate becomes an ice block at cast_frame (8 s, BOSSES.md); the "
                                                  "breath cone leaves Socket_Breath (the maw) along its +Y"})
    frames, spec, loop, facts = shatter_through(boss.poser)
    boss.key("Shatter_Through", lambda poser: shatter_through(poser)[:3], facts)
    boss.key("Armour_Break", armour_break, {"first_plate_frame": BREAK_START, "all_off_frame": BREAK_START + 2 * 7 + 9,
                                            "note": "then hide FrostWarden_IceArmour (or keep it: every floe ends at "
                                                    "scale 0) and loop WalkEnraged; the icicles stay (they are the "
                                                    "rage telegraph and belong to the armour mesh)"})
    # Round 18: the base FreezePulse turned its fists with a straight lerp between nearly opposite directions (a
    # 119-degree flip in one frame); it is keyed again here with BW.turn, without rebuilding the mesh. It stays an
    # original clip, so its info leaves the round-28 list.
    boss.key("FreezePulse", BW.CLIPS["FreezePulse"])
    boss.info.pop("FreezePulse", None)
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
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 2.4), 19)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    down = boss.info["Climb_Down"]
    ground.location.z = -CLIMB_H
    props = M.ledge_props(down["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -down["end_offset_forward_m"] * 0.5, 1.4), 19)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    cam2 = C.closeup_camera(Vector((0, 0, 1.9)), 16, azimuth=-35, elevation=20, lens=50)
    M.strip(NAME, arm, acts["Climb_Loop"], cam2, out, "Climb_Loop", M.frames_of(boss.frames["Climb_Loop"], 4))
    M.remove(props + [cam, cam2])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("ice", ad.foot_front + 0.3, length=10)
    cam = M.side_cam((0, -DIVE_TRAVEL * 0.6, 0.3), 19, elevation=22, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("ice", -10, length=10 + BURST_TRAVEL + ad.foot_back - 0.4)
    cam = M.side_cam((0, -BURST_TRAVEL * 0.5, 0.3), 19, elevation=22, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    cam2 = C.closeup_camera(Vector((0, 0.3, -1.8)), 14, azimuth=-35, elevation=55, lens=50)
    M.strip(NAME, arm, acts["Submerged_Loop"], cam2, out, "Submerged_Loop", M.frames_of(boss.frames["Submerged_Loop"], 4))
    M.remove(props + [cam, cam2])
    C.ground()
    props = M.gate_prop(GATE_F)
    cam = C.closeup_camera(Vector((0, -1.6, 2.0)), 16, azimuth=-60, elevation=14, lens=50)
    M.strip(NAME, arm, acts["Freeze_Gate"], cam, out, "Freeze_Gate", [0, 10, 20, 26, FREEZE_PEAK, 38, 48, 62])
    M.remove(props + [cam])
    props = M.gate_prop(GATE_F, frozen=True, height=2.4)
    cam = M.side_cam((0, -SHATTER_END * 0.55, 2.2), 20, elevation=10, azimuth=-75)
    imp = SHATTER_IMPACT
    M.strip(NAME, arm, acts["Shatter_Through"], cam, out, "Shatter_Through",
            [0, 12, 26, imp - 2, imp, 42, 70, SHATTER_FRAMES])
    M.remove(props + [cam])
    cam = C.closeup_camera(Vector((0, 0, 2.2)), 16, azimuth=-40, elevation=16, lens=50)
    M.strip(NAME, arm, acts["Armour_Break"], cam, out, "Armour_Break", [0, 8, BREAK_START + 2, BREAK_START + 6,
                                                                       BREAK_START + 12, BREAK_START + 20, 44, BREAK_FRAMES])
    M.remove([cam])
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, -0.3, 2.2), 17, out, label="turntable_armoured")


def main():
    args = C.cli_args()
    out = C.preview_dir()
    boss = M.Boss(NAME, list(BW.CLIPS), scale_bones=[])
    ad = adapter()
    boss.add_foot_markers(ad.limbs())
    boss.add_bones(armour_bones())
    plate_bones = [f"Armour_{i + 1}" for i in range(len(PLATES))] + [f"Icicle_{i + 1}" for i in range(ICICLES)]
    boss.scale_bones += plate_bones
    asset, lod = build_armour(boss)
    ad, depth = key_all(boss)
    # The base clips never key the floes and icicles: key them at scale 1 and at rest (the FBX exporter would bake
    # the pose another clip left behind, e.g. Armour_Break's floes flown off at scale 0). Only Armour_Break moves them.
    boss.hold(list(BW.CLIPS), {n: 1.0 for n in plate_bones})
    hold_rest(boss, list(BW.CLIPS), plate_bones)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "ice (water under an ice sheet)", "submerged_depth_m": depth,
                 "ice_armour": {"object": ARMOUR, "material": f"{ARMOUR}_Mat", "maps": f"{ARMOUR}_*.png (512, Low 256)",
                                "tris": C.tri_count(asset.low), "lod1_object": ARMOUR + "_LOD1",
                                "lod1_tris": C.tri_count(lod), "plates": [f"Armour_{i + 1}" for i in range(len(PLATES))],
                                "icicles": [f"Icicle_{i + 1}" for i in range(ICICLES)],
                                "icicle_rage_scale": "0.2 (calm) to 1.0 (max rage), uniform localScale in LateUpdate",
                                "round32_plate_map": {f"Armour_{i + 1}": f"{n} (parent {p['parent']})" for i, (n, p) in
                                                      enumerate(zip(PLATE_NAMES, PLATES))},
                                "icicle_map": "Icicle_1 centre (tallest), 2 left, 3 right, 4 rear left, 5 rear right; "
                                              "the crest on the hump ridge, parent Spine",
                                "islands": boss.armour_islands[0], "mixed_islands": boss.armour_islands[1],
                                "note": "round 32: hewn ice floes sunk into the body; floes, icicles and the rest pose "
                                        "are keyed in every base clip (only Armour_Break moves them)"},
                 "foot_markers": "IK_Arm_L/R at the knuckle contacts, IK_Leg_L/R at the hind toes (deform flag on, "
                                 "no weights)",
                 "mechanic": "Gate Breaker: Freeze_Gate (breath), Climb_Up onto the frozen gate (it sags), "
                             "Shatter_Through (fists smash down through it); ice armour soaks damage until "
                             "Armour_Break (below 40% or a Lava Ball)"})
    boss.save()
    if not args.get("norender"):
        previews(boss, ad, depth, out)


if __name__ == "__main__":
    main()
