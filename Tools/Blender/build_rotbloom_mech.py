"""Round 28: Rotbloom's mechanic clips and props (Docs/BOSSES.md "Rotbloom").

Opens ArtSource/Blend/Rotbloom.blend (round 16: mesh, rig, LOD1, Crawl/Idle/Hit/Death/Regrow/Bloom) and adds:

- Rooted_Idle (loop: sunk into the bog, the root legs dug in), Uproot (tears its legs out and stands: phase 2),
  Spore_Volley (petals open, the body pods pump, three spore pods leave bone `Socket_Spore` at the frames in the
  report), Roar (the boss intro / alarm), Climb_Up / Climb_Loop / Climb_Down, Dive_Into_Liquid (sinks into the
  bog) / Submerged_Loop / Surface_Burst;
- props: `RotSporePod` (rigged projectile: Fly loop, Burst), `RotOvergrowth` (vine overlay for a build pad and the
  tower on it, shape key `Retracted` + colour attribute `Growth`), `RotbloomRoots` (the roots that announce it over
  three waves: objects RotbloomRoots_Stage1/2/3 in one FBX, one material).

Run: blender --background --factory-startup --python Tools/Blender/build_rotbloom_mech.py --
     [--preview <dir>] [--norender] [--skip-props]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_rotbloom as BR  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import titan_kit as T  # noqa: E402
from boss_mech_kit import at, ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BR.NAME
POD = "RotSporePod"
VINES = "RotOvergrowth"
ROOTS = "RotbloomRoots"
R = math.radians
CLIMB_H = 1.2
VOLLEY_FRAMES, VOLLEY_SHOTS = 84, (32, 40, 48)
UPROOT_FRAMES = 64
ROOTED_SINK, ROOTED_DIG = 0.22, 0.2
DIVE_TRAVEL, BURST_TRAVEL = 2.4, 2.4


# ------------------------------------------------------------------------------------------------------------
# Pose adapter
# ------------------------------------------------------------------------------------------------------------

def body(off, pitch=0.0, roll=0.0, yaw=0.0, crouch=0.0, look=0.0, mouth=0.0, rage=0.0, tuck=0.0, reach=0.0,
         petals=None, bloom=None, bloom_lift=0.0, pods=None, trail=0.0):
    p = petals if petals is not None else R(16) * tuck - R(18) * rage
    return BR.specs(off=off + at(0, 0, -0.22 * crouch), pitch=pitch, roll=roll, yaw=yaw, head=look - pitch * 0.4,
                    jaw=R(4 + 30 * max(mouth, 0.6 * rage)), petals=p,
                    bloom=bloom if bloom is not None else 1.0 + 0.12 * rage - 0.1 * tuck, bloom_lift=bloom_lift,
                    pods=pods if pods is not None else 1.0, trail=trail)


def adapter():
    return M.Legged(body, [(BR.legs(), "Body")], [BR.PHASES], BR.STANCE, BR.STRIDE, BR.LIFT, BR.CRAWL_FRAMES,
                    hip_span=(1.4, -1.3))


def rooted_feet(dig=1.0):
    """Feet pushed out and down into the bog (the legs root)."""
    out = {}
    for key, l in BR.legs().items():
        d = l.foot - l.root
        p = l.root + Vector((d.x * 1.15, d.y * 1.08, 0.0))
        out[key] = Vector((p.x, p.y, -ROOTED_DIG * dig))
    return out


def rooted_body(t, f):
    w = 2 * math.pi * t
    breathe = math.sin(w)
    return BR.specs(off=at(0, 0, -ROOTED_SINK + 0.02 * breathe), pitch=R(1) * breathe, roll=R(1.5) * math.sin(w + 1.0),
                    head=-R(6) + R(3) * math.sin(2 * w), head_yaw=R(14) * math.sin(w + 0.7),
                    jaw=R(5) + R(6) * max(0.0, math.sin(2 * w + 1.0)) ** 2,
                    petals={n: -R(10) - R(6) * breathe + R(3) * math.sin(3 * w - i) for i, (n, _) in enumerate(BR.PETALS)},
                    bloom=1.04 + 0.06 * breathe, bloom_lift=0.03 * breathe,
                    pods={n: 1.0 + 0.08 * math.sin(w + i * 1.6) for i, n in enumerate(BR.POD_NAMES)},
                    trail=R(4) * math.sin(w - 1.5))


def rooted_idle(poser):
    lg = BR.legs()

    def spec(t, f):
        return K3.place_legs(poser, rooted_body(t, f), lg, rooted_feet())
    return 120, spec, True


def uproot(poser):
    """From Rooted_Idle frame 0: a shudder, the root legs tear out pair by pair (frames 14, 22, 30: mud bursts at the
    feet), the mound heaves up, a roar at 36, and it ends in the Crawl/Idle stance (phase 2: it walks)."""
    lg = BR.legs()
    idle = BR.idle_spec(poser)[1]
    tears = {1: 14, 2: 22, 3: 30}

    def spec(t, f):
        shudder = math.sin(f * 2.2) * pulse(f, 0, 10, 34)
        rise = ease(12, 40, f)
        roar = pulse(f, 30, 38, 56)
        sp = BR.specs(off=at(0.03 * shudder, 0.05 * roar, -ROOTED_SINK * (1 - rise) + 0.08 * pulse(f, 26, 36, 50)),
                      pitch=R(10) * roar + R(2) * shudder, roll=R(3) * shudder, head=R(12) * roar - R(6) * (1 - rise),
                      jaw=R(4 + 34 * roar), petals={n: -R(10) * (1 - rise) - R(24) * roar + R(4) * shudder for n, _ in BR.PETALS},
                      bloom=1.0 + 0.2 * roar, bloom_lift=0.12 * roar, trail=R(10) * shudder)
        feet = {}
        rooted = rooted_feet()
        for key, l in lg.items():
            f0 = tears[key[1]] + (0 if key[0] == "L" else 3)
            u = ease(f0 - 2, f0 + 6, f)
            yank = 0.3 * math.sin(math.pi * u)
            feet[key] = rooted[key].lerp(l.foot, u) + Vector((0, 0, yank))
        sp = K3.place_legs(poser, sp, lg, feet)
        if f >= UPROOT_FRAMES - 12:
            sp = M.blend_specs(poser, sp, idle(0.0, 0), ease(UPROOT_FRAMES - 12, UPROOT_FRAMES, f))
        return sp
    return UPROOT_FRAMES, spec, False


def spore_volley(poser):
    """Rooted or standing: the petals open flat (frames 6-26) with a rising sweet sound, the flank pods pump in a
    wave towards the bloom, the throat swells and a spore pod leaves Socket_Spore at each VOLLEY_SHOTS frame (the
    bloom kicks back each time); the petals close by the end."""
    lg = BR.legs()

    def spec(t, f):
        open_ = ease(6, 26, f) * (1 - ease(62, 82, f))
        kick = sum(pulse(f, s - 3, s, s + 6) for s in VOLLEY_SHOTS)
        pump = {n: 1.0 + 0.45 * sum(pulse(f, s - 14 + 3 * (i // 2), s - 6 + 3 * (i // 2), s) for s in VOLLEY_SHOTS)
                for i, n in enumerate(BR.POD_NAMES)}
        sp = BR.specs(off=at(0, 0, -0.06 * open_ - 0.04 * kick), pitch=-R(3) * open_ + R(2) * kick,
                      head=-R(8) * open_, jaw=R(4 + 12 * open_),
                      petals={n: -R(30) * open_ + R(10) * kick for n, _ in BR.PETALS},
                      bloom=1.0 + 0.3 * open_ + 0.35 * kick, bloom_lift=0.2 * open_ - 0.1 * kick, pods=pump)
        return K3.place_legs(poser, sp, lg, {k: l.foot.copy() for k, l in lg.items()})
    return VOLLEY_FRAMES, spec, False


def roar(poser):
    lg = BR.legs()

    def spec(t, f):
        lift = ease(4, 20, f) * (1 - ease(50, 64, f))
        jaw = ease(12, 22, f) * (1 - ease(46, 58, f))
        tremble = math.sin(f * 2.4) * pulse(f, 18, 30, 48)
        sp = BR.specs(off=at(0, 0.1 * lift, 0.18 * lift), pitch=R(14) * lift, roll=R(2) * tremble,
                      head=R(16) * lift + R(2) * tremble, jaw=R(4 + 40 * jaw),
                      petals={n: -R(30) * lift + R(5) * tremble for n, _ in BR.PETALS}, bloom=1.0 + 0.25 * lift,
                      bloom_lift=0.15 * lift, pods={n: 1.0 + 0.3 * lift for n in BR.POD_NAMES}, trail=R(8) * tremble)
        pose = poser.solve(sp)
        move = pose["Body"] @ poser.rest["Body"].inverted()
        feet = {k: l.foot.copy() for k, l in lg.items()}
        for (side, i), l in lg.items():
            if i == 1:
                feet[(side, i)] = l.foot.lerp(move @ l.foot, 0.7 * lift) + at(0, 0.1, 0.12) * lift
        return K3.place_legs(poser, sp, lg, feet)
    return 66, spec, False


# ------------------------------------------------------------------------------------------------------------
# Props
# ------------------------------------------------------------------------------------------------------------

POD_C = Vector((0, 0, 0))
BRACTS = 3


def bract_rot(i):
    return Matrix.Rotation(2 * math.pi * i / BRACTS, 3, "Y")


def pod_parts():
    """A spore pod (0.42 long, flies nose first along -Y): a glowing spore core gripped by three hooked bark bracts,
    two trailing slime tendrils."""
    b = C.PartBuilder()
    k = 0.5
    b.ellipsoid("Spore", POD_C, (0.095, 0.2, 0.095), bone="Core", segments=(28, 16),
                style=K3.sap_pod(BR.POD, k * 0.4, glow=1.0, seed=501))
    for i in range(BRACTS):
        name = f"Bract_{i + 1}"
        T.ellipsoid_patch(b, "Bract", POD_C, (0.12, 0.24, 0.12), bract_rot(i), (0, 1), (-0.62, 0.62), (-0.98, 0.8), 1,
                          0.012, 0.025, name, T.shell_plate(BR.PAL, k * 0.4, (0, 1), (-0.62, 0.62), (-0.98, 0.8),
                                                            crack=0.5, rim_glow=0.6, seed=510 + i), nu=10, nv=12,
                          bulge=0.01, keel=0.03)
        d = bract_rot(i) @ Vector((0, 0, 1))
        base = POD_C + d * 0.13 + Vector((0, -0.12, 0))
        tip = base + d * 0.05 + Vector((0, -0.1, 0))
        b.tube("BractHook", [base, base.lerp(tip, 0.5) + d * 0.02, tip], [0.018, 0.012, 0.002], sides=8, chain=[name],
               style=H.hook(BR.STAMEN, k * 0.3, glow=0.8, glow_from=0.6))
    for j, sx in enumerate((-1, 1)):
        r0 = POD_C + Vector((0.03 * sx, 0.15, 0))
        b.tube("Tendril", [r0, r0 + Vector((0.04 * sx, 0.12, 0.02)), r0 + Vector((0.0, 0.26, -0.02))], [0.02, 0.015, 0.004],
               sides=8, chain=["Tail"], style=K2.slime_foot(BR.SLIME, k * 0.3, waves=3.0, edge_glow=0.9))
    return b


def pod_bones():
    out = [("Root", Vector((0, 0, -0.05)), Vector((0, 0, 0.05)), None, True),
           ("Core", POD_C + Vector((0, 0.1, 0)), POD_C - Vector((0, 0.1, 0)), "Root", True),
           ("Tail", POD_C + Vector((0, 0.15, 0)), POD_C + Vector((0, 0.4, 0)), "Root", True)]
    for i in range(BRACTS):
        d = bract_rot(i) @ Vector((0, 0, 1))
        p = POD_C + d * 0.12
        out.append((f"Bract_{i + 1}", p + Vector((0, 0.15, 0)), p - Vector((0, 0.1, 0)), "Root", True))
    return out


def pod_specs(core=1.0, open_=0.0, wiggle=0.0, spin=0.0, gone=0.0):
    sp = {"Root": {"rot": [(M.Yv, spin)]}, "Core": {"rot": [], "scale": max(core, 0.001)},
          "Tail": {"rot": [(M.Z, wiggle)], "scale": max(1 - gone, 0.001)}}
    for i in range(BRACTS):
        d = bract_rot(i) @ Vector((0, 0, 1))
        axis = Vector((0, 1, 0)).cross(d).normalized()
        sp[f"Bract_{i + 1}"] = {"rot": [(axis, R(70) * open_)], "loc": d * 0.05 * open_, "scale": max(1 - gone, 0.001)}
    return sp


def pod_fly(poser):
    def spec(t, f):
        w = 2 * math.pi * t
        return pod_specs(core=1.0 + 0.08 * math.sin(2 * w), open_=0.06 + 0.04 * math.sin(2 * w), wiggle=R(18) * math.sin(w),
                         spin=w)
    return 24, spec, True


def pod_burst(poser):
    """On impact (or shot down): the bracts snap open, the core swells and pops at frame 6 (spore cloud VFX)."""
    def spec(t, f):
        return pod_specs(core=(1.0 + 0.6 * ease(0, 5, f)) * (1 - ease(5, 8, f)), open_=ease(0, 5, f),
                         gone=ease(6, 14, f), wiggle=R(30) * math.sin(f))
    return 16, spec, False


def build_pod():
    M.small_rig(POD, pod_parts(), pod_bones(), {"Fly": pod_fly, "Burst": pod_burst}, 900, families=("membrane", "chitin", "rock"),
                rigid={"Bract", "BractHook"}, blend=0, scale_bones=["Core", "Tail"] + [f"Bract_{i + 1}" for i in range(BRACTS)],
                voxel=0.0035, ao=0.04, extrusion=0.01, ray=0.03, emission=2.8, k=0.5, mirror=False,
                report={"pivot": "the spore core's centre; flies along its forward axis (nose = bract hooks)",
                        "burst": {"pop_frame": 6, "note": "impact on a pad or shot down in the air (BOSSES.md)"},
                        "fired_by": "Rotbloom Spore_Volley from bone Socket_Spore"})


def vine_parts(rng):
    """Vines that climb a build pad (1 x 1) and the tower on it (radius ~0.42, up to 1.75): thick roots from the pad
    corners spiralling up, thorns, glowing pustules, a mat of rootlets over the pad."""
    b = C.PartBuilder()
    k = 0.5
    vines = 7
    for v in range(vines):
        a0 = 2 * math.pi * v / vines + rng.uniform(-0.2, 0.2)
        r0 = 0.62 + rng.uniform(-0.05, 0.08)
        top = 1.0 + 0.75 * rng.uniform(0.3, 1.0)
        turns = 0.45 + 0.35 * rng.uniform()
        pts = []
        n = 9
        for j in range(n):
            s = j / (n - 1)
            a = a0 + turns * 2 * math.pi * s * (1 if v % 2 else -1)
            r = r0 * (1 - s) + (0.44 + 0.04 * math.sin(7 * s + v)) * s
            if s > 0.12:
                r = min(r, 0.47 + 0.03 * math.sin(5 * s + v))
            pts.append(Vector((math.cos(a) * r, math.sin(a) * r, 0.02 + top * s ** 1.1)))
        radii = [0.06 - 0.04 * (j / (n - 1)) for j in range(n)]
        radii[-1] = 0.006
        b.tube("Vine", pts, radii, sides=8, samples=4, chain=["Root"],
               style=K3.bark(BR.PAL, k * 0.35, grain=30, splits=0.8, moss=0.6, glow=0.8, seed=600 + v))
        for j in range(2, n - 1, 2):
            p = pts[j]
            out = Vector((p.x, p.y, 0)).normalized()
            tip = p + out * 0.1 + Vector((0, 0, 0.05))
            b.tube("VineThorn", [p, p.lerp(tip, 0.5) + out * 0.01, tip], [0.016, 0.01, 0.002], sides=6, chain=["Root"],
                   style=H.hook(BR.STAMEN, k * 0.3, glow=0.5, glow_from=0.8))
        for j in (3, 6):
            p = pts[j]
            out = Vector((p.x, p.y, 0)).normalized()
            b.ellipsoid("Pustule", p + out * 0.04, (0.055, 0.055, 0.07), bone="Root", segments=(14, 8),
                        style=K3.sap_pod(BR.POD, k * 0.3, glow=1.0, seed=620 + v + j))
    for j in range(16):
        a = 2 * math.pi * j / 16 + rng.uniform(-0.15, 0.15)
        r1 = 0.5 + rng.uniform(-0.05, 0.05)
        p0 = Vector((math.cos(a) * 0.15, math.sin(a) * 0.15, 0.015))
        p2 = Vector((math.cos(a + 0.4) * r1, math.sin(a + 0.4) * r1, 0.02))
        b.tube("Rootlet", [p0, p0.lerp(p2, 0.5) + Vector((0, 0, 0.03)), p2], [0.025, 0.02, 0.004], sides=6, chain=["Root"],
               style=K3.bark(BR.PAL, k * 0.3, grain=24, splits=0.6, moss=0.7, glow=0.6, seed=640 + j))
    return b


def build_vines():
    rng = np.random.default_rng(66)
    asset = M.static_asset(VINES, vine_parts(rng), 2600, families=("chitin", "membrane", "rock"), voxel=0.005, ao=0.06,
                           extrusion=0.012, ray=0.03, emission=2.4, k=0.5, mirror=False)
    low = asset.low
    me = low.data
    # Growth mask: R = order along the vines (0 at the pad, 1 at the highest tips), G = a per-vertex jitter so the
    # front of the growth is ragged; the shape key Retracted flattens everything onto the pad.
    zs = np.array([v.co.z for v in me.vertices])
    top = max(zs.max(), 1e-3)
    order = np.clip(zs / top, 0, 1)
    rad = np.array([math.hypot(v.co.x, v.co.y) for v in me.vertices])
    order = np.where(zs < 0.06, np.clip(rad / 0.7, 0, 1) * 0.12, 0.1 + 0.9 * order)
    jitter = np.array([C.value_noise(np.array([[v.co.x, v.co.y, v.co.z]]), 6.0, 7)[0] for v in me.vertices]) * 0.5 + 0.5
    attr = me.color_attributes.new("Growth", "BYTE_COLOR", "POINT")
    for i in range(len(me.vertices)):
        attr.data[i].color = (float(order[i]), float(jitter[i]), 0.0, 1.0)
    me.color_attributes.active_color = attr
    low.shape_key_add(name="Basis", from_mix=False)
    key = low.shape_key_add(name="Retracted", from_mix=False)
    for i, v in enumerate(me.vertices):
        co = v.co
        squash = 0.06
        key.data[i].co = Vector((co.x * (1 - 0.25 * order[i]), co.y * (1 - 0.25 * order[i]), co.z * squash))
    key.value = 0.0
    low.active_shape_key_index = 0
    M.export_static(os.path.join(C.EXPORTS, VINES, VINES + ".fbx"), [low])
    C.write_report(VINES, {"asset": VINES, "round": M.ROUND, "game_tris": C.tri_count(low), "texture_size": 512,
                           "low_texture_size": 256, "maps": ["Albedo", "Normal", "Emission", "Mask"],
                           "material": f"{VINES}_Mat", "size_m": [round(d, 3) for d in low.dimensions],
                           "pivot": "the build pad's centre on its top surface; fits a 1 x 1 pad and a tower up to "
                                    "~0.45 radius and 1.75 high (scale to the tower)",
                           "growth": {"shape_key": "Retracted (1 = flat on the pad, 0 = grown)",
                                      "color_attribute": "Growth: R = growth order 0..1 (pad first, tips last), "
                                                         "G = jitter for a ragged front",
                                      "recipe": "grow over ~1 s: blend shape 100 -> 0 while a clip threshold on "
                                                "Growth.R runs 0 -> 1; burn away: reverse with an emission flash"}})
    C.save_blend(VINES)
    return low


STAGES = [(-5.0, 5, 1.2, 0.05, 2, False), (0.0, 8, 2.0, 0.075, 4, False), (5.0, 11, 2.6, 0.1, 6, True)]


def root_parts(rng):
    """Three growth stages side by side (split after the bake): a few thin roots; more and thicker with pods; a big
    arching root mass with pods round a closed wine-red bud (the bloom about to break out of the bog)."""
    b = C.PartBuilder()
    k = 0.7
    for si, (cx, count, length, thick, pods, bud) in enumerate(STAGES):
        c = Vector((cx, 0, 0))
        for j in range(count):
            a = 2 * math.pi * j / count + rng.uniform(-0.25, 0.25)
            ln = length * rng.uniform(0.6, 1.0)
            d = Vector((math.cos(a), math.sin(a), 0))
            side = Vector((-d.y, d.x, 0))
            arch = 0.25 * thick / 0.1 * (1 if (bud and j % 3 == 0) else 0.2)
            pts = []
            for i in range(7):
                s = i / 6
                wob = side * 0.18 * math.sin(s * 5 + j) * s
                pts.append(c + d * (0.12 + ln * s) + wob + Vector((0, 0, 0.03 + arch * math.sin(math.pi * s) - 0.04 * s)))
            radii = [thick * (1 - 0.8 * i / 6) for i in range(7)]
            b.tube("Root", pts, radii, sides=8, samples=4, chain=["Root"],
                   style=K3.bark(BR.PAL, k * 0.4, grain=30, splits=0.9, moss=0.6, glow=0.7, seed=700 + 20 * si + j))
            if si >= 1 and j % 2 == 0:
                p = pts[3]
                b.tube("RootThorn", [p, p + Vector((0, 0, 0.12)) + side * 0.04, p + Vector((0, 0, 0.2)) - d * 0.03],
                       [thick * 0.35, thick * 0.2, 0.003], sides=6, chain=["Root"],
                       style=H.hook(BR.STAMEN, k * 0.3, glow=0.4, glow_from=0.8))
        for j in range(pods):
            a = 2 * math.pi * (j + 0.5) / max(pods, 1) + rng.uniform(-0.3, 0.3)
            r = length * rng.uniform(0.3, 0.6)
            s = 0.08 + 0.03 * si
            b.ellipsoid("RootPod", c + Vector((math.cos(a) * r, math.sin(a) * r, s * 0.6)), (s, s, s * 1.2), bone="Root",
                        segments=(16, 10), style=K3.sap_pod(BR.POD, k * 0.3, glow=1.0, seed=760 + 10 * si + j))
        b.ellipsoid("RootKnot", c + Vector((0, 0, 0.06)), (0.18 + 0.08 * si, 0.18 + 0.08 * si, 0.08 + 0.05 * si),
                    bone="Root", segments=(24, 12), deform=C.chiselled(10, 790 + si, 0.15),
                    style=K3.bark(BR.PAL, k * 0.4, grain=24, splits=1.0, moss=0.7, glow=0.8, seed=795 + si))
        if bud:
            for p_i in range(5):
                ang = 2 * math.pi * p_i / 5
                d = Vector((math.cos(ang), math.sin(ang), 0))
                base = c + d * 0.12 + Vector((0, 0, 0.12))
                pts = [base, base + d * 0.12 + Vector((0, 0, 0.3)), c + d * 0.05 + Vector((0, 0, 0.62))]
                b.tube("BudPetal", pts, [0.12, 0.1, 0.01], sides=10, chain=["Root"], flatten=0.45,
                       style=T.leaf_petal(BR.PETAL, k * 0.4, glow=0.8, veins=5, seed=800 + p_i))
    return b


def split_by_x(obj, centres, half=2.4):
    """Splits the baked low mesh into one object per stage (faces whose centre lies within `half` of a stage x) and
    moves each stage to the origin."""
    out = []
    for i, cx in enumerate(centres):
        me = obj.data.copy()
        bm = bmesh.new()
        bm.from_mesh(me)
        kill = [f for f in bm.faces if abs(f.calc_center_median().x - cx) > half]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        for v in bm.verts:
            v.co.x -= cx
        bm.to_mesh(me)
        bm.free()
        name = f"{ROOTS}_Stage{i + 1}"
        me.name = name
        o = C.link(bpy.data.objects.new(name, me))
        out.append(o)
    return out


def build_roots():
    rng = np.random.default_rng(77)
    asset = M.static_asset(ROOTS, root_parts(rng), 6500, families=("chitin", "membrane", "rock"), voxel=0.007, ao=0.1,
                           extrusion=0.015, ray=0.04, emission=2.4, k=0.7, mirror=False)
    stages = split_by_x(asset.low, [s[0] for s in STAGES])
    bpy.data.objects.remove(asset.low)
    M.export_static(os.path.join(C.EXPORTS, ROOTS, ROOTS + ".fbx"), stages)
    C.write_report(ROOTS, {"asset": ROOTS, "round": M.ROUND, "objects": {o.name: C.tri_count(o) for o in stages},
                           "texture_size": 512, "low_texture_size": 256, "material": f"{ROOTS}_Mat (shared)",
                           "maps": ["Albedo", "Normal", "Emission", "Mask"],
                           "pivot": "each stage's centre on the ground (bog bank or in the bog)",
                           "use": "BOSSES.md: roots spread over three waves before Rotbloom appears; show Stage1 at "
                                  "wave N-3, Stage2 at N-2, Stage3 at N-1 (swap, or scale up from 0.6 while swapping)",
                           "spread_m": {f"Stage{i + 1}": s[2] * 2 for i, s in enumerate(STAGES)}})
    C.save_blend(ROOTS)
    return stages


def prop_previews(out):
    C.reset_scene()
    with bpy.data.libraries.load(os.path.join(C.BLEND, POD + ".blend"), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in (POD, POD + "_Rig")]
        dst.actions = list(src.actions)
    arm = next(o for o in dst.objects if o.type == "ARMATURE")
    for o in dst.objects:
        C.link(o)
    C.preview_world()
    C.light_rig(scale=0.5)
    C.ground(z=-0.4)
    acts = {a.name: a for a in bpy.data.actions}
    cam = C.closeup_camera(Vector((0, 0, 0)), 1.6, azimuth=-60, elevation=20, lens=50)
    M.strip(POD, arm, acts["Fly"], cam, out, "Fly", [0, 6, 12, 18])
    M.strip(POD, arm, acts["Burst"], cam, out, "Burst", [0, 2, 4, 6, 8, 10, 12, 16])
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    with bpy.data.libraries.load(os.path.join(C.BLEND, VINES + ".blend"), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n == VINES]
    vines = dst.objects[0]
    C.link(vines)
    C.ground()
    C.light_rig(scale=1.0)
    tower = M.tower_prop(0, 0, height=1.4)
    pad = M.box("Pad", -0.5, 0.5, -0.5, 0.5, -0.05, 0.0, M.mat("PadMat", (0.3, 0.32, 0.36), rough=0.6), bevel=0.02)
    paths = []
    for i, w in enumerate((1.0, 0.6, 0.25, 0.0)):
        vines.data.shape_keys.key_blocks["Retracted"].value = w
        cam = C.closeup_camera(Vector((0, 0, 0.8)), 3.6, azimuth=-35, elevation=20, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"_vines_{i}.png"), 640, 360, samples=16))
        bpy.data.objects.remove(cam)
    C.contact_sheet(paths, os.path.join(out, f"{VINES}_Growth.png"), 4, 420)
    for p in paths:
        os.remove(p)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    with bpy.data.libraries.load(os.path.join(C.BLEND, ROOTS + ".blend"), link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n.startswith(ROOTS + "_Stage")]
    paths = []
    C.ground(color=(0.04, 0.05, 0.03))
    C.light_rig(scale=2.0)
    for o in dst.objects:
        C.link(o)
        o.hide_render = True
    for o in sorted(dst.objects, key=lambda o: o.name):
        o.hide_render = False
        cam = C.closeup_camera(Vector((0, 0, 0.3)), 7, azimuth=-35, elevation=35, lens=50)
        paths.append(C.render(cam, os.path.join(out, f"_roots_{o.name}.png"), 640, 360, samples=16))
        bpy.data.objects.remove(cam)
        o.hide_render = True
    C.contact_sheet(paths, os.path.join(out, f"{ROOTS}_Stages.png"), 3, 480)
    for p in paths:
        os.remove(p)


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def fit_depth(boss, make, name, start, margin=0.2):
    depth = start
    for _ in range(2):
        fn, facts = make(depth)
        act = boss.key(name, fn, facts)
        hi = M.mesh_z_range(boss.mesh, boss.arm, act, M.frames_of(boss.frames[name], 6))[1]
        if hi < -margin + 0.02:
            break
        depth += hi + margin
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    ad = adapter()
    boss.key("Rooted_Idle", rooted_idle, {"sink_m": ROOTED_SINK, "feet_depth_m": ROOTED_DIG,
                                          "note": "phase 1 (rooted): loops on the bog; creeps forward by moving the "
                                                  "transform slowly while the RotbloomRoots decor grows ahead"})
    boss.key("Uproot", uproot, {"tear_frames": [14, 17, 22, 25, 30, 33], "roar_frame": 36,
                                "note": "phase 2 below 50%: ends in the Idle/Crawl stance"})
    boss.key("Spore_Volley", spore_volley, {"petals_open_frames": [6, 26], "shot_frames": list(VOLLEY_SHOTS),
                                            "socket": "Socket_Spore", "prop": f"../{POD}/{POD}.fbx",
                                            "note": "one pod per target pad (max 3, BOSSES.md); the green haze on the "
                                                    "pads starts 2 s before impact"})
    boss.key("Roar", roar, {"jaw_wide_frames": [16, 50]})
    fn, facts = M.climb_up(ad, CLIMB_H, trail=1.2, lead=1.3, slow=0.9, stride_k=1.6)
    boss.key("Climb_Up", fn, facts)
    fn, facts = M.climb_loop(ad)
    boss.key("Climb_Loop", fn, facts)
    fn, facts = M.climb_down(ad, CLIMB_H, trail=0.3, lead=1.8, slow=0.9, stride_k=1.6)
    boss.key("Climb_Down", fn, facts)
    depth, top = fit_depth(boss, lambda d: M.submerged_clip(ad, d, frames=96), "Submerged_Loop", 2.0)
    boss.info["Submerged_Loop"]["mesh_top_m"] = top
    fn, facts = M.dive_clip(ad, depth, DIVE_TRAVEL, frames=72, takeoff=22, under=58)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = M.burst_clip(ad, depth, BURST_TRAVEL, frames=64, breach=18, land=34)
    boss.key("Surface_Burst", fn, facts)
    return ad, depth


def previews(boss, ad, depth, out):
    arm = boss.arm
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    M.preview_scene()
    acts = boss.new
    ground = C.ground(color=(0.04, 0.05, 0.03))
    cam = C.closeup_camera(Vector((0, 0, 1.0)), 12, azimuth=-38, elevation=22, lens=50)
    M.strip(NAME, arm, acts["Rooted_Idle"], cam, out, "Rooted_Idle", [0, 30, 60, 90])
    M.strip(NAME, arm, acts["Uproot"], cam, out, "Uproot", [0, 10, 16, 24, 30, 38, 50, 64])
    props = M.tower_prop(-1.5, 5.0) + M.tower_prop(0.3, 6.0) + M.tower_prop(1.8, 5.2)
    cam2 = C.closeup_camera(Vector((0, -1.5, 1.2)), 14, azimuth=-30, elevation=30, lens=50)
    M.strip(NAME, arm, acts["Spore_Volley"], cam2, out, "Spore_Volley", [0, 12, 24, VOLLEY_SHOTS[0], 36, VOLLEY_SHOTS[2], 64, 84])
    M.remove(props + [cam2])
    M.strip(NAME, arm, acts["Roar"], cam, out, "Roar", [0, 12, 20, 30, 40, 54])
    M.remove([cam])
    up = boss.info["Climb_Up"]
    props = M.ledge_props(up["edge_forward_m"], CLIMB_H)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 1.3), 14)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    down = boss.info["Climb_Down"]
    ground.location.z = -CLIMB_H
    props = M.ledge_props(down["edge_forward_m"], CLIMB_H, down=True)
    cam = M.side_cam((0, -down["end_offset_forward_m"] * 0.5, 0.4), 14)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    cam2 = C.closeup_camera(Vector((0, 0, 1.0)), 11, azimuth=-35, elevation=22, lens=50)
    M.strip(NAME, arm, acts["Climb_Loop"], cam2, out, "Climb_Loop", M.frames_of(boss.frames["Climb_Loop"], 4))
    M.remove(props + [cam, cam2])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("bog", ad.foot_front + 0.3, length=10)
    cam = M.side_cam((0, -DIVE_TRAVEL * 0.6, 0.0), 14, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("bog", -10, length=10 + BURST_TRAVEL + ad.foot_back - 0.4)
    cam = M.side_cam((0, -BURST_TRAVEL * 0.5, 0.0), 14, elevation=24, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    cam2 = C.closeup_camera(Vector((0, 0.3, -1.2)), 11, azimuth=-35, elevation=55, lens=50)
    M.strip(NAME, arm, acts["Submerged_Loop"], cam2, out, "Submerged_Loop", M.frames_of(boss.frames["Submerged_Loop"], 4))
    M.remove(props + [cam, cam2])
    C.ground(color=(0.04, 0.05, 0.03))
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, 0, 1.0), 12, out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    if not args.get("skip-props"):
        build_pod()
        build_vines()
        build_roots()
        if not args.get("norender"):
            prop_previews(out)
    boss = M.Boss(NAME, list(BR.CLIPS), scale_bones=BR.SCALE_BONES)
    ad = adapter()
    boss.add_foot_markers(ad.limbs())
    boss.add_bones([("Socket_Spore", BR.BLOOM_C + G(0, 0.35, 0), BR.BLOOM_C + G(0, 0.8, 0), "Bloom")])
    ad, depth = key_all(boss)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "bog", "submerged_depth_m": depth,
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "props": {POD: f"../{POD}/{POD}.fbx", VINES: f"../{VINES}/{VINES}.fbx", ROOTS: f"../{ROOTS}/{ROOTS}.fbx"},
                 "mechanic": "Overgrowth: Spore_Volley at up to 3 pads every 12 s (RotSporePod, then RotOvergrowth on "
                             "the pad); phases Rooted_Idle -> Uproot -> Crawl; RotbloomRoots announce it"})
    boss.save()
    if not args.get("norender"):
        previews(boss, ad, depth, out)


if __name__ == "__main__":
    main()
