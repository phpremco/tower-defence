"""Round 28: Gravemaw's mechanic clips (Docs/BOSSES.md "Gravemaw").

Opens ArtSource/Blend/Gravemaw.blend (round 14: mesh, rig, LOD1, Crawl/Idle/Hit/Death/Roar/Dive/Emerge) and adds,
with the round-14 spine machinery (the spine rides a path in the forward-up plane, build_gravemaw.assemble):

- Climb_Up / Climb_Loop / Climb_Down: the spine rides over a rock edge, every foot planted on the path surface
  (IK_* markers);
- Dive_Into_Liquid / Submerged_Loop / Surface_Burst: quicksand (its burrow shortcut) or any pool;
- Burrow_Down: rears, drills into the ground and goes under to the burrow depth (debris);
- Burrow_Travel: loop under the ground, only the crystal crest ridge breaks the surface (the crack line);
- Surface_Throw: bursts straight up out of the ground beside the target tower, flings it with the maw (bone
  `Socket_Throw` at the maw tip) and crashes down onto the road.

Crawl stays its Walk and Roar its Rage; Dive/Emerge (round 14) stay for the entrance.

Run: blender --background --factory-startup --python Tools/Blender/build_gravemaw_mech.py --
     [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import boss_mech_kit as M  # noqa: E402
import build_gravemaw as BG  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import deep_kit as DK  # noqa: E402
from boss_mech_kit import ease, pulse  # noqa: E402
from common import G  # noqa: E402

NAME = BG.NAME
R = math.radians
CLIMB_H = 1.2
CLIMB_CYCLE, CLIMB_STRIDE = 56, 0.9
EDGE = 3.2                       # the ledge face / bank / hole, forward of the pivot
TAIL_F = BG.NODES[-1][0]         # -2.7
HEAD_F = BG.NODES[0][0]          # 2.25


def phases():
    out = {}
    for side in BG.ARMS:
        out[("arm", side)] = 0.0 if side == "L" else 0.5
    for (side, i) in BG.LEGS:
        out[("leg", (side, i))] = (0.22 * i + (0.0 if side == "L" else 0.5)) % 1.0
    return out


PHASES = phases()


def limbs():
    for side, lg in BG.ARMS.items():
        yield ("arm", side), lg
    for key, lg in BG.LEGS.items():
        yield ("leg", key), lg


def path_point(path, u, lift=0.0, x=0.0):
    pf, pz, tf, tz = path.at(u)
    nf, nz = -tz, tf
    return G(x, pz + nz * lift, pf + nf * lift)


def path_feet(path, t, cycles, travel, stride=CLIMB_STRIDE, lift=0.14, width=1.0, d0=0.0):
    """Planted feet on the path surface for a spine that rides `travel` (arc length) over `cycles` gait cycles."""
    arms, legs = {}, {}
    for key, lg in limbs():
        p = PHASES[key]
        r = -lg.foot.y
        g_abs = t * cycles + p
        n = math.floor(g_abs)
        g = g_abs - n

        def anchor(m):
            return r + stride * 0.5 + d0 + travel * (m - p) / cycles
        x = lg.root.x + (lg.foot.x - lg.root.x) * width
        if g < BG.STANCE:
            foot = path_point(path, anchor(n), 0.0, x)
        else:
            s = (g - BG.STANCE) / (1 - BG.STANCE)
            e = (1 - math.cos(math.pi * s)) * 0.5
            u = anchor(n) + (anchor(n + 1) - anchor(n)) * e
            foot = path_point(path, u, lift * math.sin(math.pi * s), x)
        (arms if key[0] == "arm" else legs)[key[1]] = foot
    return arms, legs


def wiggle(pos, t, amp=0.03, cycles=1):
    w = 2 * math.pi * t * cycles
    return [p + Vector((( amp + 0.012 * j) * math.sin(w - j * 0.55), 0, 0)) for j, p in enumerate(pos)]


def step_path(edge, height):
    """Flat ground, a steep rounded face at `edge`, flat again `height` higher (negative: a drop)."""
    if height >= 0:
        # The face path runs just outside the rock (the spine rides above it along the normal, i.e. off the wall).
        pts = [(-20.0, 0.0), (edge - 1.1, 0.0), (edge - 0.4, 0.05), (edge - 0.1, height * 0.35),
               (edge - 0.03, height * 0.8), (edge + 0.25, height + 0.02), (edge + 0.9, height), (edge + 20.0, height)]
    else:
        h = -height
        pts = [(-20.0, 0.0), (edge - 0.9, 0.0), (edge - 0.25, -0.02), (edge + 0.03, -h * 0.2), (edge + 0.1, -h * 0.65),
               (edge + 0.4, -h * 0.97), (edge + 1.1, -h), (edge + 20.0, -h)]
    return DK.PathFZ(pts, anchor="start", samples=14)


def climb(kind):
    height = CLIMB_H if kind == "up" else -CLIMB_H
    path = step_path(EDGE, height)
    need = EDGE + 1.3 - TAIL_F
    per = CLIMB_STRIDE / BG.STANCE
    cycles = math.ceil(need / per)
    frames = cycles * CLIMB_CYCLE
    travel = cycles * per

    def fn(poser):
        def spec(t, f):
            D = travel * t
            pos, ups = DK.ride(path, BG.NODES, D)
            pos = wiggle(pos, t, 0.02, cycles)
            arms, legs = path_feet(path, t, cycles, travel)
            near = pulse(D + HEAD_F, EDGE - 1.0, EDGE + 0.3, EDGE + 2.0)
            jaw = R(4) + R(10) * near
            return BG.assemble(poser, pos, ups, jaw, arms, legs)
        return frames, spec, False
    end_f, end_z, _, _ = path.at(travel)
    facts = {"end_offset_forward_m": round(end_f, 3), "end_offset_up_m": round(end_z, 3), "edge_forward_m": EDGE,
             "ledge_height_m": CLIMB_H, "cycles": cycles,
             "note": "authored with the pivot fixed; at the last frame move the transform by the end offset and "
                     "continue with Climb_Loop (pivot on the top surface) or Crawl"}
    return fn, facts


def climb_loop(poser):
    """The braced crawl on a rock top: lower, wider, slower than Crawl, in place."""
    frames = 80

    def spec(t, f):
        w = 2 * math.pi * t
        pos, ups = BG.rest_nodes()
        pos = [Vector((p.x + (0.03 + 0.012 * j) * math.sin(w - j * 0.55), p.y, p.z * 0.86 + 0.01 * math.sin(2 * w - j * 0.4)))
               for j, p in enumerate(pos)]
        arms, legs = {}, {}
        for side, lg in BG.ARMS.items():
            fw, up = C.step_curve((t + PHASES[("arm", side)]) % 1.0, BG.STANCE, BG.STRIDE * 0.85, BG.LIFT * 1.8)
            p = lg.foot + G(0, up, fw)
            p.x *= 1.1
            arms[side] = p
        for key, lg in BG.LEGS.items():
            fw, up = C.step_curve((t + PHASES[("leg", key)]) % 1.0, BG.STANCE, BG.STRIDE * 0.85, BG.LIFT * 1.2)
            p = lg.foot + G(0, up, fw)
            p.x *= 1.12
            legs[key] = p
        return BG.assemble(poser, pos, ups, R(4) + R(4) * math.sin(2 * w), arms, legs)
    return frames, spec, True


# ------------------------------------------------------------------------------------------------------------
# Under the surface: liquid, ground
# ------------------------------------------------------------------------------------------------------------

def deep_path(depth):
    return DK.PathFZ([(-20.0, -depth), (20.0, -depth)], anchor="start", smooth=False)


def tucked_feet(poser, pos, ups, k=1.0, paddle=0.0, t=0.0):
    arms, legs = BG.carried_feet(poser, pos, ups, tuck_arm=lambda s: k, tuck_leg=lambda key: k)
    if paddle:
        for side in arms:
            ph = 2 * math.pi * (t + PHASES[("arm", side)])
            arms[side] = arms[side] + G(0, 0.08 * math.sin(ph), 0.15 * math.cos(ph)) * paddle
    return arms, legs


def under_loop(depth, frames=80, sway=0.16, crest=False):
    """In place under the surface at `depth` (path depth): a slow serpentine sway; feet folded."""
    path = deep_path(depth)

    def fn(poser):
        def spec(t, f):
            w = 2 * math.pi * t
            pos, ups = DK.ride(path, BG.NODES, 0.0, lateral=lambda j: sway * math.sin(w - j * 0.7) * (0.4 + j / 10))
            pos = [p + Vector((0, 0, 0.03 * math.sin(2 * w - j * 0.5))) for j, p in enumerate(pos)]
            arms, legs = tucked_feet(poser, pos, ups, 1.0, paddle=0.6 if not crest else 0.0, t=t)
            jaw = R(3) + R(4) * math.sin(w)
            return BG.assemble(poser, pos, ups, jaw, arms, legs)
        return frames, spec, True
    return fn


def plunge_path(edge, depth, slope=38.0):
    """Down from the bank (or hole) at `edge` at `slope` degrees to `depth`, then flat."""
    s = math.radians(slope)
    run = depth / math.tan(s)
    return DK.PathFZ([(-20.0, 0.0), (edge - 0.6, 0.0), (edge, -0.08), (edge + run * 0.5, -depth * 0.5),
                      (edge + run, -depth * 0.97), (edge + run + 0.8, -depth), (edge + 30.0, -depth)],
                     anchor="start", samples=14)


def rise_path(depth, exit_f, slope=40.0):
    s = math.radians(slope)
    run = depth / math.tan(s)
    return DK.PathFZ([(-20.0, -depth), (exit_f - run - 0.8, -depth), (exit_f - run, -depth * 0.97),
                      (exit_f - run * 0.5, -depth * 0.5), (exit_f, -0.05), (exit_f + 0.6, 0.0), (exit_f + 30.0, 0.0)],
                     anchor="start", samples=14)


def end_travel(path, frontier):
    """Arc length D at which the tail node has passed `frontier` (forward) on the path."""
    D = 0.0
    while path.at(TAIL_F + D)[0] < frontier and D < 40:
        D += 0.05
    return D


def dive_liquid(depth, frames=96):
    path = plunge_path(EDGE, depth)
    run = depth / math.tan(math.radians(38))
    D_end = end_travel(path, EDGE + run + 0.9)
    d_end = path.at(D_end)[0]

    def fn(poser):
        def spec(t, f):
            go = ease(14, frames - 6, f) ** 1.15
            D = D_end * go
            rear = pulse(f, 0, 10, 20)
            pos, ups = DK.ride(path, BG.NODES, D)
            pos = BG.rear(pos, 0.35 * rear * (1 - go), 0.0)
            pos = wiggle(pos, t, 0.04 * go, 2)
            k = ease(12, 40, f)
            arms_t, legs_t = tucked_feet(poser, pos, ups, 1.0)
            arms_w, legs_w = BG.world_feet()
            arms = {s: arms_w[s].lerp(arms_t[s], k) for s in arms_w}
            legs = {key: legs_w[key].lerp(legs_t[key], ease(14, 50, f)) for key in legs_w}
            jaw = R(30) * pulse(f, 2, 10, 24) + R(4)
            sp = BG.assemble(poser, pos, ups, jaw, arms, legs)
            if f > frames - 8:
                sp_end = M.shift(poser, under_loop(depth)(poser)[1](0.0, 0), d_end)
                sp = M.blend_specs(poser, sp, sp_end, ease(frames - 8, frames, f))
            return sp
        return frames, spec, False
    head_in = next((f for f in range(frames + 1) if path.at(HEAD_F + D_end * ease(14, frames - 6, f) ** 1.15)[1] < -0.3), 0)
    return fn, {"end_offset_forward_m": round(d_end, 3), "depth_m": depth, "bank_forward_m": EDGE,
                "splash_frame": head_in, "note": "starts on the bank; ends in Submerged_Loop frame 0 moved "
                                                 "end_offset forward"}


def burst_liquid(depth, frames=100, exit_f=3.4):
    path = rise_path(depth, exit_f)
    D_end = end_travel(path, exit_f + 0.8)
    d_end = path.at(D_end)[0]

    def fn(poser):
        def spec(t, f):
            go = 1 - (1 - ease(4, frames - 10, f)) ** 2.2
            D = D_end * go
            pos, ups = DK.ride(path, BG.NODES, D)
            pos = wiggle(pos, t, 0.05 * (1 - go), 2)
            arms_t, legs_t = tucked_feet(poser, pos, ups, 1.0)
            arms_c, legs_c = BG.carried_feet(poser, pos, ups)
            arms = {}
            for side in arms_t:
                above = ease(-0.4, 0.1, pos[1].z)
                arms[side] = arms_t[side].lerp(arms_c[side], above)
            legs = {}
            for key in legs_t:
                seg = BG.LEG_SEGS[key[1]][0]
                j = [n for n, a, b in BG.SPINE].index(seg)
                above = ease(-0.4, 0.1, pos[min(j + 1, len(pos) - 1)].z)
                legs[key] = legs_t[key].lerp(legs_c[key], above)
            burst = pulse(f, 6, 18, 40)
            jaw = R(54) * burst + R(4)
            return BG.assemble(poser, pos, ups, jaw, arms, legs)
        return frames, spec, False
    breach = next((f for f in range(frames + 1)
                   if path.at(HEAD_F + D_end * (1 - (1 - ease(4, frames - 10, f)) ** 2.2))[1] > -0.1), 0)
    return fn, {"start_depth_m": depth, "exit_forward_m": exit_f, "end_offset_forward_m": round(d_end, 3),
                "breach_frame": breach, "note": "starts in the Submerged_Loop pose under the pivot; the head breaks "
                                                "the surface exit_forward ahead at breach_frame; move the transform "
                                                "by end_offset_forward at the end and go to Crawl"}


BURROW_IMPACT = 31


def burrow_down(depth, frames=110):
    """Rears and slams the drill into the ground at the hole (BURROW_IMPACT, debris), then the whole body pours down
    the shaft to the burrow depth, ending in Burrow_Travel frame 0 moved end_offset forward."""
    hole = BG.DIVE_HOLE
    path = plunge_path(hole, depth, slope=55.0)
    run = depth / math.tan(math.radians(55))
    D_end = end_travel(path, hole + run + 0.9)
    d_end = path.at(D_end)[0]
    slide = (28, frames - 8)

    def fn(poser):
        def spec(t, f):
            D = D_end * ease(slide[0], slide[1], f)
            windup = ease(0, 20, f) * (1 - ease(23, 31, f))
            slam = pulse(f, 23, 30, 46)
            pos, ups = DK.ride(path, BG.NODES, D)
            early = 1 - ease(0.0, 1.2, D)
            pos = BG.rear(pos, 0.55 * windup * early, 0.05 * windup)
            pos[0] = BG.rotate_about(pos[0], pos[1], M.X, (R(26) * windup - R(30) * slam) * early)
            jaw = R(42) * pulse(f, 6, 16, 28)
            plunge = ease(26, 40, f)

            def leg_tuck(key):
                hip_f = HEAD_F + D - (HEAD_F - BG.LEG_SEGS[key[1]][1])
                return ease(-1.0, -0.1, hip_f - hole)
            arms_c, legs_c = BG.carried_feet(poser, pos, ups, tuck_arm=lambda s: plunge, tuck_leg=leg_tuck)
            arms_w, legs_w = BG.world_feet()
            tmp = BG.assemble(poser, pos, ups, 0.0)
            arms = {}
            for side, lg in BG.ARMS.items():
                sx = -1 if side == "L" else 1
                raised = DK.carried(poser, tmp, "Seg_1", lg.foot) + G(0.25 * sx, 0.25, 0.1) * windup
                arms[side] = raised.lerp(arms_c[side], plunge)
            follow = ease(0.0, 0.5, D)
            legs = {key: legs_w[key].lerp(legs_c[key], follow) for key in BG.LEGS}
            debris = BG.debris_burst(poser, f, BURROW_IMPACT, hole - 0.05, sink=(frames - 20, frames - 4))
            sp = BG.assemble(poser, pos, ups, jaw, arms, legs, debris)
            if f > frames - 8:
                sp_end = M.shift(poser, under_loop(depth, crest=True)(poser)[1](0.0, 0), d_end)
                sp = M.blend_specs(poser, sp, sp_end, ease(frames - 8, frames, f))
            return sp
        return frames, spec, False
    return fn, {"hole_forward_m": hole, "impact_frame": BURROW_IMPACT, "debris_frame": BURROW_IMPACT,
                "end_offset_forward_m": round(d_end, 3), "burrow_depth_m": depth,
                "note": "place Gravemaw_Hole at hole_forward when it starts; ends in Burrow_Travel frame 0 moved "
                        "end_offset forward"}


THROW_FRAMES, THROW_BURST, THROW_APEX = 96, 14, 30
THROW_HOLE = 1.0


def throw_path(apex, depth=1.9):
    """From the burrow depth straight up out of a hole at THROW_HOLE to `apex` high, over and down onto the ground
    ahead (the arch sinks as the body lands)."""
    a = max(apex, 0.02)
    return DK.PathFZ([(-20.0, -depth), (THROW_HOLE - 1.6, -depth), (THROW_HOLE - 0.6, -depth * 0.72), (THROW_HOLE - 0.1, -0.3),
                      (THROW_HOLE + 0.15, a * 0.6), (THROW_HOLE + 0.7, a), (THROW_HOLE + 1.6, a * 0.75),
                      (THROW_HOLE + 2.6, a * 0.2), (THROW_HOLE + 3.4, 0.0), (THROW_HOLE + 30.0, 0.0)],
                     anchor="start", samples=12)


def surface_throw(burrow_depth):
    """From Burrow_Travel (the body under the ground): the ground bulges, the maw bursts straight up out of the hole
    at THROW_HOLE beside the target (THROW_BURST, debris), climbs to the apex with the jaws wide and flings the tower
    up and to the right at THROW_APEX (Socket_Throw), then the arch topples forward and Gravemaw crashes down onto
    the road and pulls the rest of its body out."""
    def fn(poser):
        D_end = end_travel(throw_path(0.0, burrow_depth), THROW_HOLE + 0.6)

        def spec(t, f):
            apex = 2.4 * pulse(f, THROW_BURST - 6, THROW_APEX, THROW_FRAMES - 22)
            path = throw_path(apex, burrow_depth)
            go = ease(THROW_BURST - 8, THROW_APEX - 2, f) * 0.42 + ease(THROW_APEX - 2, THROW_FRAMES - 6, f) * 0.58
            D = D_end * go
            fling = pulse(f, THROW_APEX - 6, THROW_APEX, THROW_APEX + 12)
            pos, ups = DK.ride(path, BG.NODES, D, lateral=lambda j: 0.6 * fling * max(0.0, 1 - j / 3))
            jaw = R(56) * ease(THROW_BURST - 2, THROW_BURST + 6, f) * (1 - ease(THROW_APEX + 8, THROW_APEX + 24, f)) + R(4)
            arms_t, legs_t = tucked_feet(poser, pos, ups, 1.0)
            arms_c, legs_c = BG.carried_feet(poser, pos, ups)
            land = ease(THROW_FRAMES - 30, THROW_FRAMES - 6, f)
            arms = {s: arms_t[s].lerp(arms_c[s], ease(THROW_APEX + 10, THROW_APEX + 30, f)) for s in arms_t}
            legs = {k: legs_t[k].lerp(legs_c[k], land) for k in legs_t}
            debris = BG.debris_burst(poser, f, THROW_BURST, THROW_HOLE, sink=(THROW_FRAMES - 18, THROW_FRAMES - 2))
            return BG.assemble(poser, pos, ups, jaw, arms, legs, debris)
        return THROW_FRAMES, spec, False
    D_end = end_travel(throw_path(0.0, burrow_depth), THROW_HOLE + 0.6)
    return fn, {"hole_forward_m": THROW_HOLE, "burst_frame": THROW_BURST, "throw_frame": THROW_APEX,
                "end_offset_forward_m": round(throw_path(0.0, burrow_depth).at(D_end)[0], 3), "socket": "Socket_Throw",
                "target": "the thrown tower stands ~1.0 to the right of the hole (it is flung up and to the right)",
                "note": "start from Burrow_Travel (same depth); the target ring pulses 3 s before (BOSSES.md)"}


# ------------------------------------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------------------------------------

def fit(boss, name, make, depth, target, tries=3):
    """Keys the loop at `depth`, moves it so the mesh top sits at `target` (z)."""
    for _ in range(tries):
        fn, facts = make(depth)
        act = boss.key(name, fn, facts)
        hi = M.mesh_z_range(boss.mesh, boss.arm, act, M.frames_of(boss.frames[name], 6))[1]
        if abs(hi - target) < 0.06:
            break
        depth += hi - target
    return round(depth, 3), round(hi, 3)


def key_all(boss):
    for kind, clip in (("up", "Climb_Up"), ("down", "Climb_Down")):
        fn, facts = climb(kind)
        boss.key(clip, fn, facts)
    boss.key("Climb_Loop", climb_loop, {"distance_per_loop_m": round(BG.STRIDE * 0.85 / BG.STANCE, 3),
                                        "note": "pivot on the top surface; in place"})
    sub_depth, top = fit(boss, "Submerged_Loop", lambda d: (under_loop(d, 96), {"note": "in place under the surface"}), 2.6, -0.25)
    boss.info["Submerged_Loop"].update({"path_depth_m": sub_depth, "mesh_top_m": top})
    bur_depth, crest = fit(boss, "Burrow_Travel", lambda d: (under_loop(d, 72, sway=0.22, crest=True),
                                                             {"note": "in place; only the crest ridge breaks the ground"}),
                           2.0, 0.3)
    boss.info["Burrow_Travel"].update({"path_depth_m": bur_depth, "crest_top_m": crest,
                                       "note": "in place; only the crest ridge (~0.3 high) breaks the ground: the "
                                               "glowing crack line follows it; Unity moves the transform along the "
                                               "crack path"})
    fn, facts = dive_liquid(sub_depth)
    boss.key("Dive_Into_Liquid", fn, facts)
    fn, facts = burst_liquid(sub_depth)
    boss.key("Surface_Burst", fn, facts)
    fn, facts = burrow_down(bur_depth)
    boss.key("Burrow_Down", fn, facts)
    fn, facts = surface_throw(bur_depth)
    boss.key("Surface_Throw", fn, facts)
    return sub_depth, bur_depth


def previews(boss, sub_depth, bur_depth, out):
    arm = boss.arm
    if boss.lod_arm:
        boss.lod_arm.hide_render = boss.lod_mesh.hide_render = True
    M.preview_scene()
    acts = boss.new
    ground = C.ground()
    up = boss.info["Climb_Up"]
    props = M.ledge_props(EDGE, CLIMB_H)
    cam = M.side_cam((0, -up["end_offset_forward_m"] * 0.5, 1.2), 17, elevation=14, azimuth=-80)
    M.strip(NAME, arm, acts["Climb_Up"], cam, out, "Climb_Up", M.frames_of(boss.frames["Climb_Up"]))
    M.remove(props + [cam])
    ground.location.z = -CLIMB_H
    props = M.ledge_props(EDGE, CLIMB_H, down=True)
    cam = M.side_cam((0, -boss.info["Climb_Down"]["end_offset_forward_m"] * 0.5, 0.0), 17, elevation=14, azimuth=-80)
    M.strip(NAME, arm, acts["Climb_Down"], cam, out, "Climb_Down", M.frames_of(boss.frames["Climb_Down"]))
    cam2 = C.closeup_camera(Vector((0, 0, 0.6)), 12, azimuth=-35, elevation=24, lens=50)
    M.strip(NAME, arm, acts["Climb_Loop"], cam2, out, "Climb_Loop", M.frames_of(boss.frames["Climb_Loop"], 4))
    M.remove(props + [cam, cam2])
    ground.location.z = 0
    M.remove([ground])
    props = M.pool_props("quicksand", EDGE, length=14, depth=sub_depth + 3)
    cam = M.side_cam((0, -5, -0.5), 18, elevation=26, azimuth=-70)
    M.strip(NAME, arm, acts["Dive_Into_Liquid"], cam, out, "Dive_Into_Liquid", M.frames_of(boss.frames["Dive_Into_Liquid"]))
    M.remove(props + [cam])
    props = M.pool_props("quicksand", -14, length=14 + 3.4, depth=sub_depth + 3)
    cam = M.side_cam((0, -3, -0.5), 18, elevation=26, azimuth=-70)
    M.strip(NAME, arm, acts["Surface_Burst"], cam, out, "Surface_Burst", M.frames_of(boss.frames["Surface_Burst"]))
    cam2 = C.closeup_camera(Vector((0, 0, -1.5)), 13, azimuth=-35, elevation=55, lens=50)
    M.strip(NAME, arm, acts["Submerged_Loop"], cam2, out, "Submerged_Loop", M.frames_of(boss.frames["Submerged_Loop"], 4))
    M.remove(props + [cam, cam2])
    C.ground()
    cam = C.closeup_camera(Vector((0, -3, 0.5)), 16, azimuth=-60, elevation=22, lens=50)
    M.strip(NAME, arm, acts["Burrow_Down"], cam, out, "Burrow_Down", [0, 12, 22, 31, 44, 62, 84, 110])
    M.remove([cam])
    cam = C.closeup_camera(Vector((0, 0, 0)), 12, azimuth=-35, elevation=35, lens=50)
    M.strip(NAME, arm, acts["Burrow_Travel"], cam, out, "Burrow_Travel", M.frames_of(boss.frames["Burrow_Travel"], 4))
    M.remove([cam])
    props = M.tower_prop(1.0, THROW_HOLE + 0.2)
    cam = C.closeup_camera(Vector((0, -1.5, 1.2)), 15, azimuth=-40, elevation=14, lens=50)
    M.strip(NAME, arm, acts["Surface_Throw"], cam, out, "Surface_Throw", [0, 10, THROW_BURST, 22, THROW_APEX, 44, 66, THROW_FRAMES])
    M.remove(props + [cam])
    M.turntable(NAME, arm, bpy.data.actions["Idle"], 20, (0, 0, 1.0), 14, out)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    boss = M.Boss(NAME, list(BG.CLIPS), scale_bones=BG.DEBRIS_NAMES, lod_aims=BG.LOD_AIMS)
    boss.add_foot_markers([lg for _, lg in limbs()])
    boss.add_bones([("Socket_Throw", BG.MAW_TIP, BG.MAW_TIP + (BG.MAW_TIP - BG.MAW_BASE).normalized() * 0.3, "Head")])
    sub_depth, bur_depth = key_all(boss)
    boss.export()
    boss.report({"climb_height_m": CLIMB_H, "liquid": "quicksand (its burrow shortcut) or any pool",
                 "submerged_path_depth_m": sub_depth, "burrow_path_depth_m": bur_depth,
                 "foot_markers": "IK_<limb> bones at the rest foot contacts (deform flag on, no weights)",
                 "walk_and_rage": "Crawl is its Walk, Roar its Rage (round 14)",
                 "mechanic": "Burrow at 75/50/25%: Burrow_Down, Burrow_Travel along the crack line, Surface_Throw "
                             "beside the top-damage tower; Climb_* over rock edges; quicksand via Dive_Into_Liquid"})
    boss.save()
    if not args.get("norender"):
        previews(boss, sub_depth, bur_depth, out)


if __name__ == "__main__":
    main()
