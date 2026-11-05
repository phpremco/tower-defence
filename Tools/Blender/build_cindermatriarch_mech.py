"""Round 28: the Cinder Matriarch's mechanic clips and props (Docs/BOSSES.md "Cinder Matriarch").

Opens ArtSource/Blend/CinderMatriarch.blend (round 16: mesh, rig, LOD1, Walk/Idle/Hit/Death/Spawn/Roar) and adds,
with the round-16 pose functions (build_cindermatriarch.specs, legs, gait):

- Climb_Up / Climb_Loop / Climb_Down: over the rock ridges between the pads, feet planted (IK_* foot markers);
- Dive_Into_Liquid / Submerged_Loop / Surface_Burst: into one lava pool, out of the one nearer her cold target;
- Hunt_Stride: the off-road stride towards a Cryo tower (in place, longer steps, head low, scythes forward);
- Smash: rears and brings the scythes and fore stilts down on a tower 3.1 ahead (impact frame in the report);
- Lay_Eggs: the sac dips over the lava and three eggs drop from bone `Drip` (frames in the report).
- Round 33 (cinder_gate.py): Gate_Mount / Gate_Straddle_Loop / Gate_Dismount: she strides over a gate on her stilts
  and lowers her thorax onto the beam (Climb_* stay for the rock ridges).

Prop `CinderEgg` (own FBX, rig and clips Idle / Hatch / Burst): the egg she lays; Hatch releases a FireLarva (the
existing round-16 minion is the fire bug hatchling) at the frame in its report.

Run: blender --background --factory-startup --python Tools/Blender/build_cindermatriarch_mech.py --
     [--preview <dir>] [--norender] [--skip-props] [--gate-only]   (--gate-only: only the round-33 gate strips)
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Matrix, Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import cinder_gate as CG  # noqa: E402
import gate_kit as GK  # noqa: E402
import build_cindermatriarch as BC  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402

NAME = BC.NAME
EGG = "CinderEgg"
R = math.radians
CLIMB_H = 1.5
SMASH_FRAMES, SMASH_IMPACT, SMASH_TARGET = 64, 30, 3.1
EGG_FRAMES = (34, 52, 70)
LAY_FRAMES = 96
DIVE_TRAVEL, BURST_TRAVEL = 3.2, 3.0


def body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0,
         sac=0.0, sac2=0.0, vent=1.0, drip=0.0):
    """The generic amounts on the round-16 specs. Climbing pitch is partly taken back by the neck so the head stays
    on the target."""
    return BC.specs(off=off + at(0, 0, -0.42 * crouch), pitch=pitch, roll=roll, yaw=yaw,
                    neck=look * 0.6 + R(10) * rage - pitch * 0.35, head=look * 0.4 - pitch * 0.2,
                    mand=R(4 + 36 * max(mouth, 0.6 * rage)), sac=-R(6) * crouch + sac - abs(pitch) * 0.9,
                    sac2=R(5) * crouch + sac2 - abs(pitch) * 0.5, vent=vent,
                    scythe_up=R(18) * reach + R(45) * rage - R(30) * tuck, scythe_out=R(14) * rage + R(12) * tuck,
                    blade=-R(24) * rage + R(40) * tuck, drip=drip)


def adapter():
    return M.Legged(body, [(BC.legs(), "Body")], [BC.PHASES], BC.STANCE, BC.STRIDE, BC.LIFT, BC.WALK_FRAMES,
                    hip_span=(1.2, -1.4))


# ------------------------------------------------------------------------------------------------------------
# Boss-specific clips
# ------------------------------------------------------------------------------------------------------------

def hunt_extra(t, f, st):
    w = st["w"]
    return {"look": -R(16) + R(3) * math.sin(2 * w), "reach": 0.7 + 0.15 * math.sin(w), "crouch": 0.3,
            "mouth": 0.25 + 0.2 * max(0, math.sin(2 * w)),
            "sac": R(4) * math.sin(2 * w - 1.2), "sac2": R(5) * math.sin(2 * w - 1.8), "drip": R(8) * math.sin(2 * w - 2.0)}


def smash_spec(poser):
    """Rears on the hind stilts (to frame 20), then drops the scythes and fore stilts onto the tower ahead: impact at
    SMASH_IMPACT, a grinding shove, recovery from 44."""
    lg = BC.legs()

    def spec(t, f):
        rear = ease(2, 20, f) * (1 - ease(23, SMASH_IMPACT, f))
        slam = ease(23, SMASH_IMPACT, f) * (1 - ease(44, 62, f))
        grind = math.sin(f * 1.7) * pulse(f, SMASH_IMPACT, SMASH_IMPACT + 6, 44)
        sp = BC.specs(off=at(0.04 * grind, 0.2 * rear + 0.7 * slam, 0.42 * rear - 0.3 * slam),
                      pitch=R(24) * rear - R(13) * slam, roll=R(3) * grind,
                      neck=R(16) * rear - R(10) * slam, head=R(6) * rear - R(8) * slam,
                      mand=R(4 + 40 * max(rear, slam * 0.8)), sac=-R(14) * rear + R(6) * slam, sac2=-R(6) * rear,
                      scythe_up=R(85) * rear - R(14) * slam, scythe_out=R(26) * rear - R(6) * slam,
                      blade=-R(55) * rear + R(45) * slam + R(5) * grind)
        pose = poser.solve(sp)
        move = pose["Body"] @ poser.rest["Body"].inverted()
        feet = {k: l.foot.copy() for k, l in lg.items()}
        for (side, i), l in lg.items():
            if i != 1:
                continue
            sx = -1 if side == "L" else 1
            raised = (move @ l.foot).lerp(l.foot, 0.2) + at(0.2 * sx, -0.2, 0.25)
            stomp = l.foot + at(0.1 * sx, 0.75, 0.0)
            back = ease(44, 60, f)
            if f < 23:
                feet[(side, i)] = l.foot.lerp(raised, ease(2, 18, f))
            elif f < 44:
                feet[(side, i)] = raised.lerp(stomp, ease(23, SMASH_IMPACT, f))
            else:
                p = stomp.lerp(l.foot, back)
                feet[(side, i)] = p + Vector((0, 0, 0.35 * math.sin(math.pi * back)))
        return K3.place_legs(poser, sp, lg, feet)
    return SMASH_FRAMES, spec, False


def lay_spec(poser):
    """Crouches with the sac tip over the lava behind her and drops an egg from bone `Drip` at EGG_FRAMES: each
    one a heave of the sac and a swell of the chimneys."""
    lg = BC.legs()

    def spec(t, f):
        crouch = ease(4, 24, f) * (1 - ease(80, 94, f))
        heave = sum(pulse(f, e - 10, e - 2, e + 6) for e in EGG_FRAMES)
        push = sum(pulse(f, e - 3, e, e + 4) for e in EGG_FRAMES)
        vents = {n: 1.0 + 0.35 * heave * (0.8 + 0.2 * i) for i, n in enumerate(BC.VENT_NAMES)}
        sp = BC.specs(off=at(0, 0.15 * crouch, -0.5 * crouch + 0.05 * heave), pitch=R(7) * crouch,
                      neck=-R(4) * crouch + R(4) * heave, head=R(6) * crouch, head_yaw=R(10) * math.sin(f * 0.07),
                      mand=R(4 + 14 * heave), sac=R(16) * crouch - R(5) * heave, sac2=R(24) * crouch + R(8) * push,
                      sac_roll=R(3) * math.sin(f * 0.5) * heave, vent=vents, scythe_up=R(6) * crouch,
                      drip=R(30) * crouch + R(14) * push)
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()})
    return LAY_FRAMES, spec, False


# ------------------------------------------------------------------------------------------------------------
# The egg
# ------------------------------------------------------------------------------------------------------------

EGG_C = Vector((0, 0, 0.27))
EGG_R = (0.18, 0.18, 0.28)
EGG_PLATES = 3
EGG_SHELLS = [f"Shell_{i + 1}" for i in range(EGG_PLATES)]
EGG_SCALE = EGG_SHELLS + ["Core"]
EGG_SPAWN = 32
# Each plate is a curved claw of chilled crust over a third of the egg (rotated about the up axis), stopping short of
# the crown (plates that meet there fuse in the remesh and tear when they fly), so the egg reads as a clenched
# three-fingered bud of crust round a molten core that glows through the open crown.
PLATE_A, PLATE_B = (-0.5, 0.5), (-0.58, 0.64)


def plate_rot(i):
    return Matrix.Rotation(2 * math.pi * i / EGG_PLATES, 3, "Z")


def shell_dir(i):
    return (plate_rot(i) @ Vector((0, -1, 0.15))).normalized()


def egg_parts():
    b = C.PartBuilder()
    k = 0.6
    b.ellipsoid("Core", EGG_C, (EGG_R[0] * 0.92, EGG_R[1] * 0.92, EGG_R[2] * 0.95), bone="Core", segments=(32, 16),
                style=K3.molten_sac(BC.HOT, k * 0.35, cells=18, seed=301, glow=1.0))
    for i, name in enumerate(EGG_SHELLS):
        style = T.shell_plate(BC.PAL, k * 0.4, (0, 2), PLATE_A, PLATE_B, crack=0.9, rim_glow=1.0, seed=310 + i,
                              chevron=0.5)
        T.ellipsoid_patch(b, "EggShell", EGG_C, EGG_R, plate_rot(i), (0, 2), PLATE_A, PLATE_B, -1, 0.012, 0.03, name,
                          style, nu=12, nv=16, bulge=0.015, keel=0.05)
        # A hooked spur near the crown of each plate and a short one at its waist.
        d = shell_dir(i)
        for u, length, r in ((0.2, 0.11, 0.022), (0.05, 0.07, 0.016)):
            base = EGG_C + Vector((d.x * EGG_R[0] * 1.08, d.y * EGG_R[1] * 1.08, u))
            tip = base + (d * 0.8 + Vector((0, 0, 0.7))).normalized() * length
            b.tube("EggHook", [base - d * 0.012, base.lerp(tip, 0.5) + d * 0.012, tip], [r, r * 0.6, 0.002], sides=8,
                   chain=[name], style=H.hook(BC.PAL, k * 0.3, glow=0.7, glow_from=0.7))
    # The chilled lava collar it sits in.
    b.ellipsoid("Collar", Vector((0, 0, -0.04)), (0.25, 0.25, 0.025), bone="Root", segments=(28, 10),
                deform=C.chiselled(10, 330, 0.25, squash=(0.6, 1.0)),
                style=K3.molten_sac(BC.PAL, k * 0.5, cells=10, seed=331, glow=0.6))
    return b


def egg_bones():
    out = [("Root", Vector((0, 0, 0)), Vector((0, 0, 0.1)), None, True),
           ("Core", EGG_C - Vector((0, 0, 0.1)), EGG_C + Vector((0, 0, 0.1)), "Root", True)]
    for i, name in enumerate(EGG_SHELLS):
        d = shell_dir(i)
        p = EGG_C + Vector((d.x * EGG_R[0], d.y * EGG_R[1], -0.05))
        out.append((name, p - Vector((0, 0, 0.12)), p + Vector((0, 0, 0.1)), "Root", True))
    return out


def egg_specs(core=1.0, out=0.0, wobble=0.0, roll=0.0, fly=None, peel=0.0):
    """`peel` opens the plates outwards about their base (a bud opening); `fly` (per plate 0..1) throws them off."""
    sp = {"Root": {"rot": [(M.Yv, roll), (M.X, wobble)]}, "Core": {"rot": [], "scale": max(core, 0.001)}}
    for i, name in enumerate(EGG_SHELLS):
        d = shell_dir(i)
        hinge = Vector((0, 0, 1)).cross(d).normalized()
        spec = {"rot": [(hinge, -R(28) * peel)], "loc": d * out, "scale": 1.0}
        if fly is not None:
            t = fly[i]
            spec["loc"] = d * (out + 0.35 * t) + Vector((0, 0, 0.3 * math.sin(math.pi * min(t, 1.0))))
            spec["rot"] = [(hinge, -R(28) * peel - R(150) * t)]
            spec["scale"] = max(1.0 - t, 0.001)
        sp[name] = spec
    return sp


def egg_idle(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return egg_specs(core=1.0 + 0.06 * math.sin(w), out=0.004 * (0.5 + 0.5 * math.sin(w)), wobble=R(1.5) * math.sin(w))
    return 48, spec, True


def egg_hatch(poser):
    """Shakes harder and harder (to 28), the plates lift on glowing gaps, burst off one after another from 30 (the
    FireLarva spawns at EGG_SPAWN), the core sinks away."""
    def spec(t, f):
        shake = ease(0, 28, f) * (1 - ease(30, 34, f))
        fly = [ease(30 + 2 * i, 44 + 2 * i, f) for i in range(len(EGG_SHELLS))]
        return egg_specs(core=(1.0 + 0.15 * ease(10, 30, f)) * (1 - ease(32, 44, f)), out=0.015 * ease(8, 30, f),
                         wobble=R(9) * shake * math.sin(f * 2.1), roll=R(7) * shake * math.sin(f * 1.6 + 1.0), fly=fly,
                         peel=ease(12, 30, f))
    return 54, spec, False


def egg_burst(poser):
    """Shot before it hatches: the plates pop at once, the core collapses (no larva)."""
    def spec(t, f):
        fly = [ease(1, 9 + i, f) for i in range(len(EGG_SHELLS))]
        return egg_specs(core=(1 + 0.3 * pulse(f, 0, 2, 6)) * (1 - ease(3, 10, f)), fly=fly)
    return 20, spec, False


def build_egg():
    clips = {"Idle": egg_idle, "Hatch": egg_hatch, "Burst": egg_burst}
    M.small_rig(EGG, egg_parts(), egg_bones(), clips, 1400, families=("rock", "membrane", "chitin"),
                rigid={"EggShell", "EggHook", "Core"}, blend=0, scale_bones=EGG_SCALE, voxel=0.004, ao=0.05, extrusion=0.012,
                ray=0.03, emission=3.0, k=0.6, extra=H.weathering(0.6, scratches=0.3, dust=0.3, seed=333),
                report={"pivot": "the lava surface at the egg's centre (the collar sits in the lava)",
                        "hatch": {"larva_spawn_frame": EGG_SPAWN, "larva": "../FireLarva/FireLarva.fbx",
                                  "larva_unity_scale": BC.LARVA_SCALE, "shells_gone_frame": 50,
                                  "note": "BOSSES.md: hatch after 10 s unless shot: play Idle, then Hatch; Burst when shot"},
                        "laid_by": "CinderMatriarch Lay_Eggs (bone Drip)"})


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def fit_depth(boss, clip_fn_for, name, start, margin=0.2):
    """Keys the submerged loop at `start` depth, measures the mesh top, deepens once so it is `margin` under."""
    depth = start
    for _ in range(2):
        fn, facts = clip_fn_for(depth)
        act = boss.key(name, fn, facts)
        lo, hi = M.mesh_z_range(boss.mesh, boss.arm, act, M.frames_of(boss.frames[name], 6))
        if hi < -margin + 0.02:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    ad = adapter()
    climb = {}
    fn, climb["Climb_Up"] = M.climb_up(ad, CLIMB_H, trail=1.7)
    boss.key("Climb_Up", fn, climb["Climb_Up"])
    fn, climb["Climb_Loop"] = M.climb_loop(ad)
    boss.key("Climb_Loop", fn, climb["Climb_Loop"])
    fn, climb["Climb_Down"] = M.climb_down(ad, CLIMB_H, trail=0.3, lead=2.2)
    boss.key("Climb_Down", fn, climb["Climb_Down"])
    depth, top = fit_depth(boss, lambda d: M.submerged_clip(ad, d, frames=80), "Submerged_Loop", 3.0)
    boss.info["Submerged_Loop"]["mesh_top_m"] = top
    fn, facts = M.dive_clip(ad, depth, DIVE_TRAVEL, frames=66)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = M.burst_clip(ad, depth, BURST_TRAVEL, frames=64, breach=16, land=32)
    boss.key("Surface_Burst", fn, facts)
    fn, facts = M.stride_clip(ad, slow=0.9, stride_k=1.25, extra=hunt_extra)
    boss.key("Hunt_Stride", fn, dict(facts, note="in place; off-road hunt towards a cold tower (BOSSES.md)"))
    boss.key("Smash", smash_spec, {"impact_frame": SMASH_IMPACT, "target_forward_m": SMASH_TARGET,
                                   "note": "the tower 3.1 ahead becomes a wreck at the impact frame; shove to 44"})
    boss.key("Lay_Eggs", lay_spec, {"egg_frames": list(EGG_FRAMES), "socket": "Drip (tail: the sac tip)",
                                    "prop": f"../{EGG}/{EGG}.fbx",
                                    "note": "stand with the sac tip over the lava pool (the tip is ~2.8 behind the pivot)"})
    for clip, make, *_ in GATE_CLIPS:
        fn, facts = make()
        boss.key(clip, fn, facts)
    return ad, depth


# Round 33: over a gate (cinder_gate.py): (clip, maker, gate forward, sag per frame, preview centre, QA key).
GATE_CLIPS = (("Gate_Mount", CG.mount, CG.MOUNT_GATE_F, CG.mount_sag, CG.MOUNT_GATE_F * 0.55, "Mount"),
              ("Gate_Straddle_Loop", CG.straddle, 0.0, CG.straddle_sag, 0.0, "Straddle"),
              ("Gate_Dismount", CG.dismount, 0.0, CG.dismount_sag, 2.0, "Dismount"))


def previews(boss, ad, depth, out):
    arm = boss.arm
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    M.preview_scene()
    paths = []
    acts = boss.new
    up = boss.info["Climb_Up"]
    ground = C.ground()
    props = M.ledge_props(up["edge_forward_m"], CLIMB_H)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 1.8), 16)
    paths.append(M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"])))
    M.remove(props + [cam])
    down = boss.info["Climb_Down"]
    ground.location.z = -CLIMB_H
    props = M.ledge_props(down["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -down["end_offset_forward_m"] * 0.5, 0.6), 16)
    paths.append(M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"])))
    cam2 = C.closeup_camera(Vector((0, 0, 1.2)), 13, azimuth=-35, elevation=22, lens=50)
    paths.append(M.strip(NAME, arm, acts["Climb_Loop"], cam2, out, "Climb_Loop", M.frames_of(boss.frames["Climb_Loop"], 4)))
    M.remove(props + [cam, cam2])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("lava", ad.foot_front + 0.3, length=10)
    cam = M.side_cam((0, -DIVE_TRAVEL * 0.6, -0.4), 17, elevation=24, azimuth=-70)
    paths.append(M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid",
                         M.frames_of(boss.frames["Dive_Into_Liquid"])))
    M.remove(props + [cam])
    props = M.pool_props("lava", -10, length=10 + BURST_TRAVEL + ad.foot_back - 0.4)
    cam = M.side_cam((0, -BURST_TRAVEL * 0.5, -0.2), 17, elevation=24, azimuth=-70)
    paths.append(M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"])))
    cam2 = C.closeup_camera(Vector((0, 0.3, -1.6)), 12, azimuth=-35, elevation=55, lens=50)
    paths.append(M.strip(NAME, arm, acts["Submerged_Loop"], cam2, out, "Submerged_Loop",
                         M.frames_of(boss.frames["Submerged_Loop"], 4)))
    M.remove(props + [cam, cam2])
    ground = C.ground()
    cam = M.side_cam((0, 0, 1.3), 13)
    paths.append(M.strip(NAME, arm, acts["Hunt_Stride"], cam, out, "Hunt_Stride", M.frames_of(boss.frames["Hunt_Stride"] - 1)))
    M.remove([cam])
    props = M.tower_prop(0, SMASH_TARGET)
    cam = C.closeup_camera(Vector((0, -1.5, 1.4)), 14, azimuth=-60, elevation=16, lens=50)
    paths.append(M.strip(NAME, arm, acts["Smash"], cam, out, "Smash", [0, 10, 20, 26, SMASH_IMPACT, 38, 50, 64]))
    M.remove(props + [cam])
    M.remove([ground])
    props = M.pool_props("lava", -10, length=8.2)
    cam = C.closeup_camera(Vector((0, 1.0, 1.0)), 14, azimuth=-130, elevation=20, lens=50)
    paths.append(M.strip(NAME, arm, acts["Lay_Eggs"], cam, out, "Lay_Eggs", [0, 16, 26, EGG_FRAMES[0], 44, EGG_FRAMES[1], EGG_FRAMES[2], 92]))
    M.remove(props + [cam])
    C.ground()
    paths.append(M.turntable(NAME, arm, bpy_action("Idle"), 20, (0, 0, 1.3), 13, out))
    C.log("previews", paths)


def bpy_action(n):
    import bpy
    return bpy.data.actions[n]


def egg_previews(out):
    import bpy
    C.reset_scene()
    with bpy.data.libraries.load(os.path.join(C.BLEND, EGG + ".blend"), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in (EGG, EGG + "_Rig")]
        dst.actions = [n for n in src.actions]
    arm = mesh = None
    for o in dst.objects:
        C.link(o)
        if o.type == "ARMATURE":
            arm = o
        else:
            mesh = o
    arm.animation_data_create()
    C.preview_world()
    C.light_rig(scale=0.6)
    pool = M.pool_props("lava", -3, length=6, half=3, depth=1)
    pool[-1].location.z = 0.04
    cam = C.closeup_camera(Vector((0, 0, 0.3)), 2.6, azimuth=-35, elevation=22, lens=50)
    acts = {a.name: a for a in bpy.data.actions}
    M.strip(EGG, arm, acts["Hatch"], cam, out, "Hatch", [0, 12, 24, 30, 33, 36, 42, 52])
    M.strip(EGG, arm, acts["Burst"], cam, out, "Burst", [0, 2, 4, 6, 8, 10, 14, 20])
    M.strip(EGG, arm, acts["Idle"], cam, out, "Idle", [0, 12, 24, 36])
    M.remove(pool)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if not args.get("skip-props") and not args.get("gate-only"):
        build_egg()
        if not args.get("norender"):
            egg_previews(out)
    boss = M.Boss(NAME, list(BC.CLIPS), scale_bones=BC.VENT_NAMES)
    boss.add_foot_markers(adapter().limbs())
    ad, depth = key_all(boss)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "lava", "submerged_depth_m": depth,
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "props": {EGG: f"../{EGG}/{EGG}.fbx"},
                 "mechanic": "Hatred of the Cold: Hunt_Stride off the road, Climb_* over the ridges between pads, "
                             "Smash on the cold tower, Dive/Submerged/Surface_Burst through the lava pools, "
                             "Lay_Eggs below 50%",
                 "gate": dict(GK.gate_facts(), clips=[c[0] for c in GATE_CLIPS], usage=GK.USAGE)})
    boss.save()
    if args.get("norender"):
        return
    if not args.get("gate-only"):
        previews(boss, ad, depth, out)
    M.preview_scene()
    GK.write_check(NAME, GK.previews_and_check(boss, NAME, [(c, gf, sag, centre, key) for c, _, gf, sag, centre, key
                                                            in GATE_CLIPS], CG.PLANTED, out, ortho=11.0,
                                               distance=14.0))


if __name__ == "__main__":
    main()
