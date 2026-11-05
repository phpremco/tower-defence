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
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BW.NAME
ARMOUR = NAME + "_IceArmour"
R = math.radians
CLIMB_H = 1.5
GATE_F = 2.4
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

def train_ellipsoid(i):
    (f0, u0), (f1, u1) = BW.TRAIN[i], BW.TRAIN[i + 1]
    front, back = G(0, u0, f0), G(0, u1, f1)
    c = front.lerp(back, 0.5)
    return (c + G(0, 0.16, 0.08), (BW.TRAIN_W[i] * 1.12, (front - back).length * 0.64, 0.24), T.forward_frame(front, back))


# (parent bone, centre, radii, rotation, axes, a range, b range, sign, bulge, keel)
PLATES = [("Chest", BW.CHEST_C - G(0, 0, 0.08), (0.5, 0.2, 0.6), T.rot(pitch=-6), (0, 2), (-0.72, 0.72), (-0.7, 0.8), -1, 0.05, 0.08),
          ("Chest", G(-0.66, 2.88, 0.04), (0.4, 0.42, 0.23), T.rot(roll=28, pitch=-6), (0, 1), (-0.78, 0.78), (-0.78, 0.78), 1, 0.05, 0.12),
          ("Chest", G(0.66, 2.88, 0.04), (0.4, 0.42, 0.23), T.rot(roll=-28, pitch=-6), (0, 1), (-0.78, 0.78), (-0.78, 0.78), 1, 0.05, 0.12),
          ("Chest", G(0, 2.48, -0.3), (0.52, 0.3, 0.62), T.rot(pitch=10), (0, 2), (-0.7, 0.7), (-0.55, 0.8), 1, 0.04, 0.0),
          ("Hips", *train_ellipsoid(0), (0, 1), (-0.72, 0.72), (-0.78, 0.7), 1, 0.05, 0.14),
          ("Abdomen_1", *train_ellipsoid(1), (0, 1), (-0.72, 0.72), (-0.78, 0.7), 1, 0.05, 0.12),
          ("Abdomen_2", *train_ellipsoid(2), (0, 1), (-0.7, 0.7), (-0.75, 0.6), 1, 0.04, 0.1),
          ("Hips", G(0, 1.34, 0.12), (0.62, 0.56, 0.42), T.rot(), (0, 2), (-0.62, 0.62), (-0.35, 0.55), -1, 0.04, 0.0)]
ICICLES = 5
ICICLE_ROOTS = [(0.0, 2.92, -0.5, (0.0, 0.85, -0.55), 0.62), (0.22, 2.75, -0.52, (0.35, 0.8, -0.55), 0.5),
                (-0.22, 2.75, -0.52, (-0.35, 0.8, -0.55), 0.5), (0.3, 2.45, -0.55, (0.5, 0.65, -0.6), 0.4),
                (-0.3, 2.45, -0.55, (-0.5, 0.65, -0.6), 0.4)]
ICE_ARMOUR = dict(root=(0.08, 0.14, 0.32), body=(0.3, 0.46, 0.74), tip=(0.72, 0.84, 0.97), ice_glow=(0.45, 0.72, 1.0),
                  warm=(1.0, 0.6, 0.2))


def plate_anchor(entry):
    parent, c, r, rot_m, axes, ar, br, sign, *_ = entry
    w = [0.0, 0.0, 0.0]
    w[axes[0]], w[axes[1]] = (ar[0] + ar[1]) / 2, (br[0] + br[1]) / 2
    w[3 - axes[0] - axes[1]] = sign * math.sqrt(max(1 - w[axes[0]] ** 2 - w[axes[1]] ** 2, 0.01))
    return Vector(c) + rot_m @ Vector((w[0] * r[0], w[1] * r[1], w[2] * r[2]))


def ice_slab(k, seed=0, warm_glow=0.8):
    """Grown ice armour: frosted pale blue, dark inner fracture planes, a cold rim glow, the warm amber flesh light
    on the face that lies against the body (it shows when a plate lifts off)."""
    root, bodyc, tip, glow, warm = (np.array(ICE_ARMOUR[n]) for n in ("root", "body", "tip", "ice_glow", "warm"))

    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        centre = np.array(part.frame.inverted().translation)
        d = p - centre
        d /= np.maximum(np.linalg.norm(d, axis=1, keepdims=True), 1e-9)
        outward = C.smoothstep(-0.1, 0.35, np.einsum("ij,ij->i", n, d))
        frost = C.smoothstep(0.1, 0.65, C.value_noise(p, 26 / k, seed))
        plane = (1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 10 / k, seed + 3)))) * \
            C.smoothstep(-0.2, 0.3, C.value_noise(p, 3 / k, seed + 4))
        col = C.lerp3(bodyc, tip, frost * 0.4)
        col = C.lerp3(col, root * 0.55, plane * 0.75)
        col = C.lerp3(col, warm * 0.6, (1 - outward) * 0.85)
        s.albedo[idx] = col * (0.88 + 0.1 * C.value_noise(p, 6 / k, seed + 5))[:, None]
        cold = glow[None, :] * (0.18 + 0.35 * frost * (1 - plane))[:, None] * outward[:, None]
        hot = warm[None, :] * ((1 - outward) * warm_glow)[:, None]
        s.emit[idx] = np.clip(cold + hot, 0, 1)
        s.rough[idx] = 0.22 + 0.35 * frost + 0.15 * plane
        s.disp[idx] = -0.004 * k * plane
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        s.flow[idx] = 0.0
    return style


def armour_parts():
    b = C.PartBuilder()
    k = BW.K * 0.5
    for i, entry in enumerate(PLATES):
        parent, c, r, rot_m, axes, ar, br, sign, bulge, keel = entry
        T.ellipsoid_patch(b, "IcePlate", c, r, rot_m, axes, ar, br, sign, 0.035, 0.07, f"Armour_{i + 1}",
                          ice_slab(k, seed=400 + i), nu=14, nv=14, bulge=bulge, keel=keel)
    # Ice spurs grown out of the shoulder and back plates (they leave with their plate).
    for i, sx in ((1, -1), (2, 1)):
        a = plate_anchor(PLATES[i])
        for j, (d, l, rad) in enumerate((((0.3, 1.0, -0.2), 0.55, 0.07), ((0.85, 0.5, 0.1), 0.38, 0.05))):
            b.shard("IceSpur", a + G(0.05 * sx, 0.05, 0), G(d[0] * sx, d[1], d[2]).normalized(), rad, l, sides=6, flute=0.3,
                    twist=0.5 * sx, bone=f"Armour_{i + 1}", style=K3.ice_prism(k, seed=420 + j, **ICE_ARMOUR))
    a = plate_anchor(PLATES[0])
    for j, sx in enumerate((-1, 1)):
        b.shard("IceSpur", a + G(0.28 * sx, 0.25, 0.04), G(0.4 * sx, 0.6, 0.7).normalized(), 0.05, 0.32, sides=6,
                flute=0.3, bone="Armour_1", style=K3.ice_prism(k, seed=430 + j, **ICE_ARMOUR))
    for j in range(3):
        a = plate_anchor(PLATES[4 + j])
        b.shard("IceSpur", a + G(0, 0.04, 0), G(0, 1.0, -0.45).normalized(), 0.06 - 0.01 * j, 0.42 - 0.08 * j, sides=6,
                flute=0.3, bone=f"Armour_{5 + j}", style=K3.ice_prism(k, seed=440 + j, **ICE_ARMOUR))
    # The rage icicles on the back (own scale bones).
    for i, (x, u, f, d, l) in enumerate(ICICLE_ROOTS):
        b.shard("Icicle", G(x, u, f), G(*d).normalized(), 0.05 + 0.02 * l, l, sides=6, flute=0.35, twist=0.6,
                bone=f"Icicle_{i + 1}", style=K3.ice_prism(k, seed=450 + i, **ICE_ARMOUR))
    return b


def armour_bones():
    out = []
    for i, entry in enumerate(PLATES):
        a = plate_anchor(entry)
        c = Vector(entry[1])
        d = (a - c).normalized()
        out.append((f"Armour_{i + 1}", a - d * 0.15, a + d * 0.15, entry[0]))
    for i, (x, u, f, d, l) in enumerate(ICICLE_ROOTS):
        root = G(x, u, f)
        out.append((f"Icicle_{i + 1}", root, root + G(*d).normalized() * 0.3, "Chest"))
    out.append(("Socket_Breath", G(0, 3.17, 0.42), G(0, 3.14, 0.75), "Head"))
    return out


def build_armour(boss):
    folder = os.path.join(C.EXPORTS, NAME)
    builder = armour_parts()
    asset = DK.build_mesh_asset_dk(ARMOUR, builder, voxel=0.008, target_tris=3200, tex_size=512, ao_distance=0.2,
                                   extrusion=0.04, ray=0.1, emission_strength=2.2, specular=0.45, k=BW.K * 0.5,
                                   families=("crystal", "rock"), low_size=256, out_dir=folder, smooth_iterations=2)
    bpy.data.objects.remove(bpy.data.objects[ARMOUR + "_High"])
    rigid = {"IcePlate", "IceSpur", "Icicle"}
    C.skin(asset.low, boss.arm, builder, rigid=rigid, blend=0)
    T.island_rigid(asset.low, [f"Armour_{i + 1}" for i in range(len(PLATES))] + [f"Icicle_{i + 1}" for i in range(ICICLES)])
    lod_mesh = C.decimate(asset.low.data.copy(), 1500, ARMOUR + "_LOD1", symmetric=True)
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
    chest_f = BW.CHEST_C.y * -1 + 0.3
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
            a = plate_anchor(PLATES[i])
            d = (a - Vector(PLATES[i][1])).normalized()
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
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, -0.4, 1.9), 15, out, label="turntable_armoured")


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
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "ice (water under an ice sheet)", "submerged_depth_m": depth,
                 "ice_armour": {"object": ARMOUR, "material": f"{ARMOUR}_Mat", "maps": f"{ARMOUR}_*.png (512, Low 256)",
                                "tris": C.tri_count(asset.low), "lod1_object": ARMOUR + "_LOD1",
                                "lod1_tris": C.tri_count(lod), "plates": [f"Armour_{i + 1}" for i in range(len(PLATES))],
                                "icicles": [f"Icicle_{i + 1}" for i in range(ICICLES)],
                                "icicle_rage_scale": "0.2 (calm) to 1.0 (max rage), uniform localScale in LateUpdate"},
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "mechanic": "Gate Breaker: Freeze_Gate, Climb_Up onto the frozen gate (it sags), Shatter_Through; "
                             "ice armour soaks damage until Armour_Break (below 40% or a Lava Ball)"})
    boss.save()
    if not args.get("norender"):
        previews(boss, ad, depth, out)


if __name__ == "__main__":
    main()
