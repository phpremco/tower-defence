"""Round 28: The Sealed One's mechanic clips and phase looks (Docs/BOSSES.md "The Sealed One").

Opens ArtSource/Blend/SealedOne.blend (round 16: mesh, rig, LOD1, Idle/Rise/Summon/Hit/Death) and adds:

- Walk (it hauls itself hand over hand, the lower torso ploughing the ground; in place), Roar;
- Climb_Up / Climb_Loop / Climb_Down (hands on the edge, the torso hauled over a 2 m ledge),
  Dive_Into_Liquid / Submerged_Loop / Surface_Burst;
- Rise_OntoBase (both hands grab the base, the torso heaves up and over it: grab and crush frames in the report),
  Adapt (the recognition pulse: plates part, the rings align and flare, palms open), Release (the final phase: it
  opens, the rings unfold wide and still, it kneels; the crystals go white from the frame in the report);
- phase looks: emission maps `SealedOne[_Seal]_Phase1..4_Emission.png` and `..._Release_Emission.png` (1024 and
  Low 512): phase 1 = the shipped violet and gold; 2 brighter violet; 3 hot magenta-violet, the gold glyphs dimming
  (the seal fails); 4 white-violet blaze; Release white;
- the seven seal stones stay on Root: every clip that moves the body away from the seal keys them at scale 0
  (`Seal_1..7`, uniform), so Unity never has to hide them by hand;
- round 29: OnBase_Loop (it lies over the base after Rise_OntoBase and pounds it, strike frames in the report); the
  seal stones and the rubble ring are keyed in every clip (shown only in Rise).

Run: blender --background --factory-startup --python Tools/Blender/build_sealedone_mech.py --
     [--preview <dir>] [--norender] [--skip-maps]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_sealedone as BS  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BS.NAME
R = math.radians
SEALS = [f"Seal_{i + 1}" for i in range(7)]
CLIMB_H = 2.0
HAUL_FRAMES, HAUL_STRIDE, HAUL_STANCE = 80, 1.3, 0.55
BASE_F, BASE_H = 3.4, 0.9
RISE_GRAB, RISE_CRUSH, RISE_FRAMES = 34, 96, 130
ADAPT_FRAMES, ADAPT_PULSE = 64, 30
RELEASE_FRAMES, RELEASE_WHITE = 170, 64
ONBASE_FRAMES, ONBASE_STRIKES = 96, (34, 82)
MOBILE = ["Walk", "Climb_Up", "Climb_Loop", "Climb_Down", "Dive_Into_Liquid", "Submerged_Loop", "Surface_Burst",
          "Rise_OntoBase", "OnBase_Loop"]
STILL = ["Roar", "Adapt", "Release"]
CLIP_SCALES = {**{c: BS.RING_NAMES + SEALS + ["Rubble"] for c in MOBILE}, **{c: BS.RING_NAMES for c in STILL}}


def pose(sink=0.0, travel=0.0, hidden=False, **kw):
    """build_sealedone.specs plus forward travel of the body and hidden seal stones."""
    sp = BS.specs(sink=sink, **kw)
    sp["Body"]["loc"] = sp["Body"]["loc"] + at(0, travel, 0)
    if hidden:
        # Collapsed to a point and dropped under the ground, so nothing of the seal site is left at the pivot.
        for n in SEALS:
            sp[n]["scale"] = 0.001
            sp[n]["loc"] = Vector((0, 0, -1.5))
        sp["Rubble"] = {"rot": [], "scale": 0.001, "loc": Vector((0, 0, -1.5))}
    return sp


def split_rubble(boss):
    """The heaved rubble ring round the fissure was skinned to Root (it never moved in round 16). Its vertices move
    to their own bone `Rubble` (child of Root, at the pivot) so the clips that leave the seal can hide it with the
    seal stones."""
    boss.add_bones([("Rubble", Vector((0, 0, 0)), Vector((0, 0, 0.4)), "Root")], scale=True)
    for mesh in (boss.mesh, boss.lod_mesh):
        if mesh is None:
            continue
        root = mesh.vertex_groups.get("Root")
        rub = mesh.vertex_groups.get("Rubble") or mesh.vertex_groups.new(name="Rubble")
        if root is None:
            continue
        moved = []
        for v in mesh.data.vertices:
            for g in v.groups:
                if g.group == root.index and g.weight > 0:
                    moved.append((v.index, g.weight))
        for i, w in moved:
            rub.add([i], w, "REPLACE")
        root.remove([i for i, _ in moved])
        C.log(mesh.name, "rubble vertices moved", len(moved))


def rest_hand(side):
    return BS.lower_arms()[side].foot.copy()


def hands_at(off_f=0.0, off_z=0.0):
    return {s: rest_hand(s) + at(0, off_f, off_z) for s in ("L", "R")}


def carried_hands(poser, sp, tuck=0.0):
    p = poser.solve(sp)
    move = p["Chest"] @ poser.rest["Chest"].inverted()
    out = {}
    for s, lg in BS.lower_arms().items():
        h = move @ lg.foot
        root = move @ lg.root
        out[s] = h.lerp(root, 0.45 * tuck)
    return out


# ------------------------------------------------------------------------------------------------------------
# Locomotion
# ------------------------------------------------------------------------------------------------------------

def haul_hands(t, stride=HAUL_STRIDE, lift=0.7, base=None):
    out = {}
    for s in ("L", "R"):
        ph = (t + (0.0 if s == "L" else 0.5)) % 1.0
        fw, up = C.step_curve(ph, HAUL_STANCE, stride, lift)
        h = (base or hands_at())[s] + at(0, fw, up)
        out[s] = h
    return out


def walk(poser, sink=0.3, slow=1.0, tag=None):
    """Hand over hand: each lower hand reaches out, plants and pulls; the torso surges with each pull, leans into
    it, the head low and searching; the lower torso ploughs through the ground (leave a furrow). Every crown ring turns
    whole turns per loop (Unity round 16, Remco 2026-11-06: the outer ring turned half a turn and snapped back each loop)."""
    frames = int(round(HAUL_FRAMES * slow))

    def spec(t, f):
        w = 2 * math.pi * t
        surge = math.sin(2 * w)
        sp = pose(sink=sink + 0.06 * math.cos(2 * w), travel=0.12 * surge, hidden=True, lean=R(14) + R(4) * surge,
                  roll=R(5) * math.sin(w), yaw=R(4) * math.sin(w), head=R(6) + R(3) * math.sin(2 * w + 0.6),
                  head_yaw=R(8) * math.sin(w + 0.4), plates=R(4), spin=(w, -w, w), uarm_up=R(6) * math.sin(w))
        return BS.place_hands(poser, sp, haul_hands(t), curl=R(20))
    return frames, spec, True


def roar(poser):
    """Rears back, slams both lower hands down (frame 30), the plates fly open on the violet column, the rings flare
    and spin, the upper arms spread."""
    def spec(t, f):
        rear = ease(4, 24, f) * (1 - ease(54, 70, f))
        slam = pulse(f, 24, 30, 46)
        tremble = math.sin(f * 2.6) * pulse(f, 26, 36, 52)
        sp = pose(sink=-0.25 * rear + 0.1 * slam, lean=-R(14) * rear + R(10) * slam, roll=R(2) * tremble,
                  head=-R(20) * rear + R(2) * tremble, plates=R(40) * rear, rings=1.0 + 0.35 * rear,
                  spin=(3.0 * ease(4, 60, f), -3.6 * ease(4, 60, f), 2.0 * ease(4, 60, f)),
                  uarm_up=R(100) * rear, uarm_out=R(70) * rear, uelbow=-R(30) * rear)
        raised = hands_at(0.3, 1.1)
        h = {s: rest_hand(s).lerp(raised[s], ease(4, 22, f) * (1 - ease(24, 30, f))) for s in ("L", "R")}
        return BS.place_hands(poser, sp, h, curl=R(30) * slam)
    return 72, spec, False


def ledge_clip(kind, height=CLIMB_H, edge=2.8, travel=3.2):
    """Up: both hands reach up onto the top (to frame 30) and grip, the torso is hauled up and over (40-104), the
    hands re-plant ahead (104-128). Down: the hands reach down to the ground beyond the lip and the torso slides
    down after them. The pivot stays; ends `travel` forward and `height` up (or down)."""
    frames = 132
    dz = height if kind == "up" else -height

    def fn(poser):
        def spec(t, f):
            haul = ease(36, 104, f)
            reach = ease(2, 30, f)
            x = travel * haul
            z = dz * (haul ** (0.7 if kind == "up" else 1.4))
            heave = pulse(f, 40, 70, 104)
            sp = pose(sink=-z - 0.35 * heave * (1 if kind == "up" else -0.3), travel=x, hidden=True,
                      lean=R(18) * heave + R(8) * reach * (1 - haul) - R(6) * (1 - reach),
                      head=-R(14) * reach * (1 - haul) + R(10) * heave if kind == "up" else R(18) * reach * (1 - haul),
                      plates=R(6), spin=(f * 0.03, -f * 0.03, f * 0.02), uarm_up=R(20) * heave)
            top = {s: rest_hand(s) + at(0, edge - 1.75 + 0.6, dz) for s in ("L", "R")}
            final = hands_at(travel, dz)
            h = {}
            for s in ("L", "R"):
                lag = 0 if s == "L" else 4
                lifted = rest_hand(s).lerp(top[s], ease(2 + lag, 30 + lag, f))
                lifted = lifted + Vector((0, 0, 0.6 * math.sin(math.pi * ease(2 + lag, 30 + lag, f))))
                regrip = ease(104 + lag, 126 + lag, f)
                p = lifted.lerp(final[s], regrip) + Vector((0, 0, 0.5 * math.sin(math.pi * regrip)))
                h[s] = p
            return BS.place_hands(poser, sp, h, curl=R(28) * ease(26, 36, f) * (1 - ease(104, 116, f)))
        return frames, spec, False
    facts = {"end_offset_forward_m": travel, "end_offset_up_m": dz, "edge_forward_m": edge,
             ("ledge_height_m" if kind == "up" else "drop_height_m"): height, "grip_frame": 30,
             "note": "authored with the pivot fixed; at the last frame move the transform by the end offset"}
    return fn, facts


def climb_loop(poser):
    return walk(poser, sink=0.2, slow=1.25)


def submerged(depth):
    def fn(poser):
        def spec(t, f):
            w = 2 * math.pi * t
            sp = pose(sink=depth + 0.08 * math.sin(w), hidden=True, lean=R(10) + R(3) * math.sin(w),
                      roll=R(3) * math.sin(w + 0.5), head=R(14), plates=0.0, rings=0.45 + 0.05 * math.sin(2 * w),
                      spin=(w, -w, w), uarm_up=R(4) * math.sin(w))
            return BS.place_hands(poser, sp, carried_hands(poser, sp, tuck=0.6), curl=R(30))
        return 96, spec, True
    return fn


def dive(depth, travel=2.0):
    """Both hands press down on the bank edge, the plates close, the rings shrink to 0.45 and the torso slides
    forward and down into the liquid (the head under at the frame in the report)."""
    frames = 90

    def fn(poser):
        def spec(t, f):
            go = ease(14, 80, f)
            z = depth * go ** 1.3
            sp = pose(sink=z - 0.2 * pulse(f, 0, 12, 24), travel=travel * go, hidden=True,
                      lean=R(20) * pulse(f, 6, 30, 70) + R(10) * go, head=R(14) * go,
                      plates=R(10) * (1 - ease(8, 30, f)), rings=1.0 - 0.55 * ease(10, 50, f),
                      spin=(f * 0.03, -f * 0.03, f * 0.02))
            press = {s: rest_hand(s) + at(0, 0.6, 0) for s in ("L", "R")}
            h = {s: rest_hand(s).lerp(press[s], ease(0, 12, f)) for s in ("L", "R")}
            car = carried_hands(poser, sp, tuck=0.6)
            h = {s: h[s].lerp(car[s], ease(36, 70, f)) for s in h}
            sp = BS.place_hands(poser, sp, h, curl=R(30) * ease(4, 14, f))
            if f > frames - 8:
                end = M.shift(poser, submerged(depth)(poser)[1](0.0, 0), travel)
                sp = M.blend_specs(poser, sp, end, ease(frames - 8, frames, f))
            return sp
        return frames, spec, False
    return fn, {"end_offset_forward_m": travel, "depth_m": depth,
                "note": "ends in Submerged_Loop frame 0 moved end_offset forward"}


def burst(depth, travel=2.4):
    """From the depth: the hands burst out first (frame 14) and slam onto the bank ahead (24), the torso is hauled
    up and out with a roar, the rings unfold; ends standing `travel` ahead."""
    frames = 100

    def fn(poser):
        def spec(t, f):
            up = 1 - (1 - ease(16, 86, f)) ** 2
            z = depth * (1 - up)
            x = travel * ease(20, 86, f)
            roar_ = pulse(f, 60, 74, 96)
            sp = pose(sink=z, travel=x, hidden=True, lean=R(14) * pulse(f, 20, 50, 80) - R(10) * roar_,
                      head=R(14) * (1 - up) - R(16) * roar_, plates=R(34) * roar_, rings=0.45 + 0.55 * ease(50, 80, f),
                      spin=(f * 0.04, -f * 0.04, f * 0.03), uarm_up=R(20) * roar_)
            car = carried_hands(poser, sp, tuck=0.6)
            slam = {s: rest_hand(s) + at(0, travel, 0) for s in ("L", "R")}
            out = ease(6, 24, f)
            h = {s: car[s].lerp(slam[s], out) + Vector((0, 0, 0.8 * math.sin(math.pi * out))) for s in ("L", "R")}
            return BS.place_hands(poser, sp, h, curl=R(28) * ease(22, 28, f))
        return frames, spec, False
    return fn, {"start_depth_m": depth, "end_offset_forward_m": travel, "hands_slam_frame": 24,
                "note": "start under the exit point; at the end move the transform by end_offset_forward"}


def rise_onto_base(poser):
    """The finale's last threat: both hands rise and come down on the base BASE_F ahead (grab at RISE_GRAB), the
    torso heaves up and forward onto it (crush at RISE_CRUSH: the base takes the hit), it looms over it with the
    plates open."""
    def spec(t, f):
        heave = ease(RISE_GRAB + 6, RISE_CRUSH, f)
        loom = ease(RISE_CRUSH - 10, RISE_FRAMES, f)
        slam = pulse(f, RISE_CRUSH - 4, RISE_CRUSH, RISE_CRUSH + 14)
        sp = pose(sink=-BASE_H * heave - 0.4 * pulse(f, RISE_GRAB, 70, RISE_CRUSH) + 0.15 * slam,
                  travel=1.6 * heave, hidden=True, lean=R(26) * heave + R(10) * slam - R(10) * ease(4, 24, f) * (1 - heave),
                  head=-R(18) * ease(4, 30, f) * (1 - heave) + R(18) * loom, plates=R(36) * loom + R(10) * slam,
                  rings=1.0 + 0.3 * loom, spin=(f * 0.05, -f * 0.05, f * 0.04), uarm_up=R(60) * loom,
                  uarm_out=R(40) * loom)
        grab = {s: rest_hand(s) + at(0, BASE_F - 1.75 + 0.2, BASE_H) + at(-0.5 if s == "R" else 0.5, 0, 0) for s in ("L", "R")}
        lift = ease(4, RISE_GRAB, f)
        h = {s: rest_hand(s).lerp(grab[s], lift) + Vector((0, 0, 1.2 * math.sin(math.pi * lift))) for s in ("L", "R")}
        return BS.place_hands(poser, sp, h, curl=R(35) * ease(RISE_GRAB - 4, RISE_GRAB + 4, f))
    return RISE_FRAMES, spec, False


def on_base_loop(poser):
    """Round 29: it lies over the base where Rise_OntoBase ended and pounds it, one lower hand after the other: each
    hand is pulled up and back, the torso rears into it, then the hand slams down on the roof (strike frames
    ONBASE_STRIKES: the base takes a hit, dust and a camera kick) and the torso drops onto it. Frame 0 (= the last
    frame) is Rise_OntoBase's last pose, so Unity goes straight from that clip into this loop; the rings keep turning
    (one full turn per loop)."""
    grab = {s: rest_hand(s) + at(0, BASE_F - 1.75 + 0.2, BASE_H) + at(-0.5 if s == "R" else 0.5, 0, 0) for s in ("L", "R")}
    s0 = (RISE_FRAMES * 0.05, -RISE_FRAMES * 0.05, RISE_FRAMES * 0.04)

    def stroke(f, strike):
        g = (f - (strike - 34)) % ONBASE_FRAMES
        raise_ = ease(6, 26, g) * (1 - ease(28, 34, g))
        slam = pulse(g, 30, 34, 46)
        return raise_, slam

    def spec(t, f):
        w = 2 * math.pi * t
        (rl, sl), (rr, sr) = stroke(f, ONBASE_STRIKES[0]), stroke(f, ONBASE_STRIKES[1])
        rear, slam = max(rl, rr), max(sl, sr)
        sp = pose(sink=-BASE_H + 0.04 * math.sin(w) - 0.18 * rear + 0.14 * slam, travel=1.6 - 0.12 * rear + 0.1 * slam,
                  hidden=True, lean=R(26) - R(10) * rear + R(9) * slam, roll=R(6) * (rl - rr) + R(3) * (sl - sr),
                  yaw=R(5) * (rl - rr), head=R(18) - R(8) * rear + R(12) * slam, head_yaw=R(6) * (rr - rl),
                  plates=R(36) + R(10) * slam + R(4) * math.sin(w), rings=1.3 + 0.08 * slam,
                  spin=(s0[0] + w, s0[1] - w, s0[2] + w), uarm_up=R(60) + R(25) * rear - R(10) * slam,
                  uarm_out=R(40) + R(10) * rear)
        h = {}
        for side, (r_, s_) in (("L", (rl, sl)), ("R", (rr, sr))):
            up = grab[side] + at(0, -0.35, 1.5)
            h[side] = grab[side].lerp(up, r_) + at(0, 0.15 * s_, -0.05 * s_)
        curl = {"L": R(35) * (1 - rl) + R(10) * sl, "R": R(35) * (1 - rr) + R(10) * sr}
        return BS.place_hands(poser, sp, h, curl=(curl["L"] + curl["R"]) * 0.5)
    return ONBASE_FRAMES, spec, True


def adapt(poser):
    """Recognition: it goes still, the head tilts as if listening, the plates part, the three rings swing into one
    plane and flare at ADAPT_PULSE (the violet pulse: dim that tower's icon on the boss bar), palms turn out."""
    def spec(t, f):
        listen = ease(2, 20, f) * (1 - ease(48, 62, f))
        flare = pulse(f, ADAPT_PULSE - 4, ADAPT_PULSE, ADAPT_PULSE + 10)
        align = ease(6, ADAPT_PULSE - 2, f) * (1 - ease(44, 60, f))
        spins = [(1 - align) * s for s in (0.6, -0.8, 0.4)]
        sp = pose(sink=-0.06 * listen, roll=R(8) * listen, head=-R(6) * listen, head_yaw=R(14) * listen,
                  plates=R(16) * listen + R(18) * flare, rings=1.0 + 0.45 * flare + 0.05 * listen, spin=tuple(spins),
                  uarm_up=R(40) * listen, uarm_out=R(30) * listen, uelbow=-R(20) * listen)
        return BS.place_hands(poser, sp, curl=R(10) * listen)
    return ADAPT_FRAMES, spec, False


def release(poser):
    """The last phase ends: it rears, the plates open wide and stay open, the rings expand to 1.8 and slow to a stop,
    the upper arms open, the crystals go white from RELEASE_WHITE (swap to the Release emission and fade it up), it
    sinks to its knees with the head bowed and the hands open on the ground: the epilogue starts on the last frame."""
    def spec(t, f):
        rear = pulse(f, 4, 40, 90)
        open_ = ease(20, 60, f)
        kneel = ease(90, 150, f)
        shudder = math.sin(f * 2.0) * pulse(f, 30, 50, 80)
        spin_amt = 4.0 * ease(10, 60, f) + 1.5 * ease(60, 140, f)
        sp = pose(sink=-0.3 * rear + 0.45 * kneel, lean=-R(16) * rear + R(22) * kneel + R(2) * shudder,
                  head=-R(28) * rear + R(26) * kneel, plates=R(50) * open_, rings=1.0 + 0.8 * open_,
                  spin=(spin_amt, -spin_amt * 1.2, spin_amt * 0.7), uarm_up=R(110) * open_ * (1 - 0.6 * kneel),
                  uarm_out=R(80) * open_, uelbow=-R(30) * open_)
        h = {s: rest_hand(s) + at(0, 0.3 * kneel, 0.9 * rear) for s in ("L", "R")}
        return BS.place_hands(poser, sp, h, curl=-R(20) * kneel)
    return RELEASE_FRAMES, spec, False


# ------------------------------------------------------------------------------------------------------------
# Phase looks (emission maps)
# ------------------------------------------------------------------------------------------------------------

PHASES = {
    # violet gain, violet tint, gold gain, base glow floor (violet light leaking into dark areas)
    "Phase1": (1.0, None, 1.0, 0.0),
    "Phase2": (1.35, (0.72, 0.32, 1.0), 0.85, 0.03),
    "Phase3": (1.6, (1.0, 0.36, 0.86), 0.5, 0.06),
    "Phase4": (1.5, (0.7, 0.42, 1.0), 0.25, 0.1),
    "Release": (2.2, (1.0, 1.0, 1.0), 0.0, 0.14),
}


def phase_maps():
    """Emission variants from the shipped maps: violet light (blue > red > green) is lifted and tinted per phase,
    the amber-gold glyph light (red > green > blue) dims as the seal fails; Release turns everything white."""
    out = {}
    folder = os.path.join(C.EXPORTS, NAME)
    for sub in ("", "Low"):
        for mat in ("SealedOne", "SealedOne_Seal"):
            src = os.path.join(folder, sub, f"{mat}_Emission.png")
            if not os.path.exists(src):
                continue
            img = C.image_to_array(src)
            rgb = img[..., :3]
            lum = rgb.max(axis=2)
            violet = (rgb[..., 2] > rgb[..., 1]) & (lum > 0.02)
            gold = (rgb[..., 0] >= rgb[..., 2]) & (lum > 0.02)
            for name, (vg, tint, gg, floor) in PHASES.items():
                res = rgb.copy()
                if tint is not None:
                    t = np.array(tint)
                    res[violet] = np.clip(lum[violet][:, None] * t[None, :] * vg, 0, 1)
                else:
                    res[violet] = np.clip(rgb[violet] * vg, 0, 1)
                if name == "Release":
                    res[gold] = np.clip(lum[gold][:, None] * np.array((1.0, 1.0, 1.0)) * 1.4, 0, 1)
                else:
                    res[gold] = rgb[gold] * gg
                if floor:
                    col = np.array(tint if tint is not None else (0.62, 0.26, 1.0))
                    res = np.maximum(res, (lum[..., None] * 0 + floor) * col[None, None, :] * (lum[..., None] < 0.02))
                arr = img.copy()
                arr[..., :3] = res
                path = os.path.join(folder, sub, f"{mat}_{name}_Emission.png")
                C.array_to_image(arr, path)
                out.setdefault(name, []).append(os.path.relpath(path, folder).replace("\\", "/"))
    return out


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def fit_depth(boss, make, name, start, margin=0.2):
    depth = start
    for _ in range(3):
        act = boss.key(name, make(depth), {"note": "in place under the surface; only the violet glow shows"})
        hi = M.mesh_z_range(boss.mesh, boss.arm, act, M.frames_of(boss.frames[name], 6))[1]
        if hi < -margin + 0.03:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    boss.key("Walk", lambda p: walk(p), {"distance_per_loop_m": round(HAUL_STRIDE / HAUL_STANCE, 3),
                                        "note": "in place; the lower torso ploughs the ground (furrow + dust decal); "
                                                "seal stones at scale 0"})
    boss.key("Roar", roar, {"slam_frame": 30, "plates_open_frames": [14, 60]})
    fn, facts = ledge_clip("up")
    boss.key("Climb_Up", fn, facts)
    boss.key("Climb_Loop", climb_loop, {"distance_per_loop_m": round(HAUL_STRIDE / HAUL_STANCE, 3),
                                        "note": "the haul on top of a ledge or the base roof; in place"})
    fn, facts = ledge_clip("down")
    boss.key("Climb_Down", fn, facts)
    depth, top = fit_depth(boss, submerged, "Submerged_Loop", 5.4)
    boss.info["Submerged_Loop"].update({"depth_m": depth, "mesh_top_m": top})
    fn, facts = dive(depth)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = burst(depth)
    boss.key("Surface_Burst", fn, facts)
    boss.key("Rise_OntoBase", rise_onto_base, {"base_forward_m": BASE_F, "base_top_m": BASE_H, "grab_frame": RISE_GRAB,
                                               "crush_frame": RISE_CRUSH,
                                               "note": "start with the base BASE_F ahead; the base takes the hit at "
                                                       "crush_frame (BOSSES.md: it pulls itself up onto the base)"})
    boss.key("OnBase_Loop", on_base_loop, {"strike_frames": list(ONBASE_STRIKES), "base_forward_m": BASE_F,
                                           "base_top_m": BASE_H, "hands": {"L": ONBASE_STRIKES[0], "R": ONBASE_STRIKES[1]},
                                           "note": "loop after Rise_OntoBase (frame 0 = its last pose, same transform); "
                                                   "the base takes a blow at each strike frame (dust, kick); Adapt "
                                                   "on the base still has no clip of its own"})
    boss.key("Adapt", adapt, {"pulse_frame": ADAPT_PULSE,
                              "note": "play at each phase change; ORA's line and the dimmed icon at pulse_frame"})
    boss.key("Release", release, {"white_frame": RELEASE_WHITE, "kneel_done_frame": 150,
                                  "note": "swap both emission maps to the Release set at white_frame and fade the "
                                          "emission strength up; the epilogue starts at the last frame"})
    return depth


def hold_seal(boss):
    """Round 29: the seal stones and the rubble ring are keyed in every clip. Unkeyed, the FBX exporter baked them
    with the pose the previous clip left (scale 0 in all of them, Rise included). Rise (the entrance out of the seal)
    shows them; every other clip plays away from the seal, so they stay collapsed (scale 0.001)."""
    hidden = {n: 0.001 for n in SEALS + ["Rubble"]}
    boss.hold(["Rise"], {n: 1.0 for n in SEALS + ["Rubble"]})
    boss.hold([c for c in list(BS.CLIPS) + STILL if c != "Rise"], hidden)


def previews(boss, depth, out):
    arm = boss.arm
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    M.preview_scene()
    acts = boss.new
    ground = C.ground()
    cam = C.closeup_camera(Vector((0, -0.5, 2.6)), 21, azimuth=-60, elevation=16, lens=50)
    M.strip(NAME, arm, acts["Walk"], cam, out, "Walk", M.frames_of(HAUL_FRAMES - 1))
    M.strip(NAME, arm, acts["Roar"], cam, out, "Roar", [0, 12, 22, 30, 40, 72])
    M.strip(NAME, arm, acts["Adapt"], cam, out, "Adapt", [0, 14, 24, ADAPT_PULSE, 40, 64])
    M.strip(NAME, arm, acts["Release"], cam, out, "Release", [0, 24, 44, RELEASE_WHITE, 90, 120, 150, RELEASE_FRAMES])
    M.remove([cam])
    base = M.box("Base", -1.6, 1.6, BASE_F - 0.2, BASE_F + 2.6, 0, BASE_H,
                 M.mat("PropBase", (0.6, 0.64, 0.7), rough=0.4, emission=(0.3, 0.7, 1.0), strength=0.3))
    cam = M.side_cam((0, -2.0, 2.6), 22, elevation=12, azimuth=-70)
    M.strip(NAME, arm, acts["Rise_OntoBase"], cam, out, "Rise_OntoBase", [0, 16, RISE_GRAB, 56, 76, RISE_CRUSH, 112, RISE_FRAMES])
    M.strip(NAME, arm, acts["OnBase_Loop"], cam, out, "OnBase_Loop", [0, 16, 26, ONBASE_STRIKES[0], 48, 74, ONBASE_STRIKES[1], 95])
    game = C.game_camera(Vector((0, -BASE_F, 1.0)), 640 / C.GAME_PIXELS_PER_UNIT * 1.2)
    M.strip(NAME, arm, acts["OnBase_Loop"], game, out, "OnBase_Loop_game", [0, 26, ONBASE_STRIKES[0], 74])
    M.remove([base, cam, game])
    up = boss.info["Climb_Up"]
    props = M.ledge_props(up["edge_forward_m"], CLIMB_H)
    cam = M.side_cam((0, -1.6, 2.8), 22, elevation=10, azimuth=-80)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    ground.location.z = -CLIMB_H
    props = M.ledge_props(boss.info["Climb_Down"]["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -1.6, 1.4), 22, elevation=10, azimuth=-80)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    M.remove(props + [cam])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("void", 2.2, length=12, half=6, depth=depth + 2)
    cam = M.side_cam((0, -1.5, 0.5), 23, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("void", -12, length=12 + 2.4 + 0.6, half=6, depth=depth + 2)
    cam = M.side_cam((0, -1.5, 0.5), 23, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    M.remove(props + [cam])
    C.ground()
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, 0, 2.5), 19, out)


def phase_sheet(out):
    """The four phase looks and Release side by side (Idle frame 20, front 3/4), by swapping the emission images."""
    paths = []
    folder = os.path.join(C.EXPORTS, NAME)
    imgs = {}
    for img in bpy.data.images:
        base = os.path.basename(bpy.path.abspath(img.filepath))
        if base in ("SealedOne_Emission.png", "SealedOne_Seal_Emission.png"):
            imgs[base] = img
    cam = C.closeup_camera(Vector((0, 0, 2.6)), 19, azimuth=-30, elevation=14, lens=50)
    C.set_action(bpy.data.objects[NAME + "_Rig"], bpy.data.actions["Idle"], 20)
    for name in PHASES:
        for base, img in imgs.items():
            stem = base[:-len("_Emission.png")]
            img.filepath = os.path.join(folder, f"{stem}_{name}_Emission.png")
            img.reload()
        paths.append(C.render(cam, os.path.join(out, f"_sealed_{name}.png"), 640, 360, samples=16))
    for base, img in imgs.items():
        img.filepath = os.path.join(folder, base)
        img.reload()
    C.contact_sheet(paths, os.path.join(out, f"{NAME}_Phases.png"), 5, 380)
    for p in paths:
        os.remove(p)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    boss = M.Boss(NAME, list(BS.CLIPS), scale_bones=BS.SCALE_BONES + SEALS, clip_scales=CLIP_SCALES)
    boss.add_foot_markers(list(BS.lower_arms().values()))
    split_rubble(boss)
    maps = {} if args.get("skip-maps") else phase_maps()
    depth = key_all(boss)
    hold_seal(boss)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "void pool (any liquid)", "submerged_depth_m": depth,
                 "hand_markers": "IK_LArm_L / IK_LArm_R at the lower hands' rest contacts",
                 "seal_stones": "Seal_1..7 and Rubble (the heaved ground ring, moved off Root in round 28): scale 1 in "
                                "Rise only (the entrance out of the seal); scale 0 in every other clip (round 29: "
                                "keyed explicitly in all clips)",
                 "phase_looks": {"maps": maps, "emission_strength": {"Phase1": 2.6, "Phase2": 3.0, "Phase3": 3.4,
                                                                    "Phase4": 3.9, "Release": 4.5},
                                 "note": "swap SealedOne_Emission and SealedOne_Seal_Emission per phase (Adapt marks "
                                         "the change); the decor crystals take the immune tower's colour (BOSSES.md)"},
                 "mechanic": "Recognition: Adapt at each phase change, 4 phase looks, Release at the end; "
                             "Rise_OntoBase as its last threat; Walk/Climb/Dive to follow the risen road"})
    boss.save()
    if not args.get("norender"):
        previews(boss, depth, out)
        if maps:
            phase_sheet(out)


if __name__ == "__main__":
    main()
