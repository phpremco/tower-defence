"""Round 33: Gravemaw's gate-crossing clips (Remco: "the Gravemaw animation on the gate looks very odd, the body moves
as if it stands on flat ground"). Imported by build_gravemaw_mech.py, which keys them with the other mechanic clips.

The spine rides an arch over the gate (the round-14 machinery: nodes ride a path in the forward-up plane). The spades
follow `gate_kit.FootPlan`s and the legs `gate_kit.plan_steps` on the real gate surfaces (Gate_G2 measured, see
gate_kit), in model units (the boss plays at 0.8, the gate at 1.0):

- `Gate_Mount`: from the road in front (gate centre `MOUNT_GATE_F` ahead), it rears, braces its spades on the crowns'
  near faces, hooks them over the crowns' far edges, hauls itself up the arch (the legs scrabble in the air and on the
  pylon faces), the belly comes down on the beam and the weight goes onto the gate. Ends in Gate_Straddle_Loop frame 0
  with the pivot on the gate centre.
- `Gate_Straddle_Loop`: draped over the beam between the pylons, belly on the beam, the spades dug into the ground on
  the far side, legs 1 on the crowns' far faces, legs 2 on the crown tops, legs 3 braced on the crowns' near faces; it
  hauls with one spade, then
  the other, and slips back each time: a `weight` event per heavy push (two per loop).
- `Gate_Dismount`: pours head first down the far side, the spades catch the ground, the legs scramble over the crowns
  and the tail slides off the beam. Ends in the Crawl pose `end_offset_forward_m` past the gate centre.

Gate space: x across, up, forward with the gate's road centre at forward 0; Gate_Mount's pivot starts MOUNT_GATE_F
before it (`shift`).
"""

import math

from mathutils import Matrix, Vector

import build_gravemaw as BG
import deep_kit as DK
import boss_mech_kit as M
import gate_kit as GK
from boss_mech_kit import ease, pulse
from common import G

R = math.radians
HB = GK.m(GK.BEAM_TOP)            # 1.464: the beam top in model metres
CROWN = GK.m(GK.CROWN_TOP)        # 1.5: the crown tops
CROWN_XM = 1.25                   # where a foot stands on a crown (model x; the flat top spans 1.1..1.4)
FACE = GK.m(GK.PYLON_FACE)        # 0.244: the pylons' faces along the road
MOUNT_GATE_F = 3.2                # the gate centre ahead of Gate_Mount's starting pivot (maw tip 0.2 short of the beam)
APEX_NODE_F = 0.2                 # the body point (rest forward) on the beam: just behind node 3, legs 2 above it
BELLY = -0.04                     # the path's apex this far below the beam top: negative, the chords over the fold
#                                   dip the belly, so the path is raised

# Straddle feet (gate space, L side; R mirrors x).
ARM_GROUND = G(-1.2, 0.0, 1.62)           # spades dug into the far side
CROWN_FACE = GK.m(GK.CROWN_FACE) + 0.07   # 0.36: a foot pressed on a crown's face (the face at 0.29, the foot is thick)
LEG_GATE = {1: G(-CROWN_XM, 1.3, CROWN_FACE),     # legs 1 grip the crowns' far faces
            2: G(-CROWN_XM, CROWN, -0.1),         # legs 2 stand on the crown tops
            3: G(-CROWN_XM, 1.28, -CROWN_FACE)}   # legs 3 brace against the crowns' near faces

BASE_SAG = 0.08                   # model metres the beam is pressed down while it lies on it
PUSH_SAG = 0.2                    # extra at a heavy push

STRADDLE_FRAMES = 96
WEIGHT = (22, 70)                 # the heavy pushes in Gate_Straddle_Loop
MOUNT_FRAMES = 168
MOUNT_BELLY = 146                 # the belly lands on the beam (first weight)
DISMOUNT_FRAMES = 150


def arch_path(shift=0.0, apex=None, height=1.0):
    """The surface the belly rides over the gate (forward, up), gate centre at `shift`; `height` scales it (0 = flat
    ground, while it rears up to the gate)."""
    a = HB - BELLY if apex is None else apex
    # Near side: a long ramp (the body is hauled up from behind, the tail drags); a tight fold over the beam (the
    # weight hangs on it); far side: a steep drop (the front pours down to the claws).
    pts = [(-20.0, 0.0), (-2.4, 0.0), (-1.75, 0.16), (-1.15, 0.62), (-0.55, 1.14), (-0.22, a - 0.07), (0.0, a),
           (0.2, a - 0.1), (0.5, 1.02), (0.9, 0.5), (1.35, 0.12), (2.0, 0.0), (20.0, 0.0)]
    dense = GK.monotone_samples(pts, step=0.05)
    return DK.PathFZ([(f + shift, z * height) for f, z in dense], anchor="start", smooth=False)


def u_at(path, f_target):
    """Arc parameter where the path reaches forward `f_target` (on its rising part from the start)."""
    lo, hi = -40.0, 60.0
    for _ in range(60):
        mid = (lo + hi) * 0.5
        if path.at(mid)[0] < f_target:
            lo = mid
        else:
            hi = mid
    return (lo + hi) * 0.5


def d_straddle(path, gate_f=0.0):
    return u_at(path, gate_f) - APEX_NODE_F


def far_excess(path):
    """u - forward on the far flat (the arch's extra arc length)."""
    u = u_at(path, 30.0)
    return u - path.at(u)[0]


def body(path, D, rear=0.0, press=0.0, gate_f=0.0, roll=0.0, sway=0.0, t=0.0, head=0.0):
    """Spine node positions and ups. rear: the head end lifted (BG.rear); press: nodes near the gate pushed down
    (the beam sags under it); roll: the body rolled about its axis; sway: a small lateral wave; head: the head node
    turned down (> 0) or up about node 1."""
    lat = (lambda j: sway * math.sin(2 * math.pi * t - j * 0.6)) if sway else None
    pos, ups = DK.ride(path, BG.NODES, D, lateral=lat)
    if rear:
        pos = BG.rear(pos, rear, 0.0)
    if press:
        for j, p in enumerate(pos):
            # The nodes next to the beam go down with it in full, the ends stay on the ground.
            k = 1.0 - ease(0.7, 2.2, abs(-p.y - gate_f))
            pos[j] = p - Vector((0, 0, press * k))
    if roll:
        for j in range(len(pos)):
            a = (pos[max(j - 1, 0)] - pos[min(j + 1, len(pos) - 1)]).normalized()
            ups[j] = Matrix.Rotation(roll, 3, a) @ ups[j]
    if head:
        pos[0] = GK.rot_about(pos[0], pos[1], Vector((1, 0, 0)), head)
    return pos, ups


def pose(poser, pos, ups, jaw, arms, legs, arm_dirs=None, debris=None):
    """BG.assemble with the spades turned along `arm_dirs` (hooked over a crown, dug into the ground)."""
    sp = BG.assemble(poser, pos, ups, jaw, arms, legs, debris)
    if arm_dirs:
        for side, lg in BG.ARMS.items():
            if side not in arm_dirs:
                continue
            root = DK.carried(poser, sp, "Seg_1", lg.root)
            pole = DK.carried_dir(poser, sp, "Seg_1", lg.pole)
            sp.update(lg.pose(poser, root, arms[side], pole, arm_dirs[side]))
    return sp


def side_x(p, side):
    """A left-side point mirrored to `side`."""
    return Vector((p.x if side == "L" else -p.x, p.y, p.z))


def fwd(p, d):
    return p + G(0, 0, d)


DIG = {"L": G(0.12, -0.75, 0.62).normalized(), "R": G(-0.12, -0.75, 0.62).normalized()}


def leg_keys():
    return list(BG.LEGS.keys())


# ---------------------------------------------------------------------------------------------------------------
# Gate_Straddle_Loop
# ---------------------------------------------------------------------------------------------------------------

def straddle_sag(f):
    """Model metres the beam is pressed down at frame f of the loop."""
    s = BASE_SAG
    for e in WEIGHT:
        s += PUSH_SAG * pulse(f, e - 10, e, e + 16)
    return s


def straddle_state(f, frames=STRADDLE_FRAMES):
    """Body travel offset at loop frame f: at each weight event a spade slams in and rakes the body forward, then the
    weight drags it back over the beam."""
    haul = 0.0
    for e in WEIGHT:
        g = (f - e) % frames
        haul += 0.16 * ease(0, 16, g) * (1 - ease(18, 42, g))
    return haul, R(7) * math.sin(2 * math.pi * f / frames)


def straddle_feet(f, frames=STRADDLE_FRAMES):
    """Spades: each in turn lifts, reaches forward and slams in at its weight event, then rakes back home while the
    body hauls; legs 1/2 on the crowns (down with the sag), legs 3 slip down the crowns' near faces as the weight
    drags the body back and re-grip higher."""
    sag = straddle_sag(f)
    arms, dirs = {}, {}
    for side, e in zip(("L", "R"), WEIGHT):
        g = (f - e + 22) % frames          # 22 = this spade's slam
        home = side_x(ARM_GROUND, side)
        lift = 0.34 * pulse(g, 4, 14, 22)
        reach = 0.24 * ease(4, 20, g) * (1 - ease(23, 44, g))
        arms[side] = home + G(0, lift, reach)
        dirs[side] = DIG[side].lerp(G(0, -0.2, 1.0).normalized(), pulse(g, 4, 14, 22)).normalized()
    legs = {}
    for key in leg_keys():
        side, i = key
        if i in (1, 2):
            legs[key] = side_x(LEG_GATE[i], side) - Vector((0, 0, sag))
            continue
        e = WEIGHT[0] if side == "L" else WEIGHT[1]
        g = (f - e) % frames
        slip = 0.07 * ease(18, 40, g) * (1 - ease(44, 52, g))
        regrip = 0.08 * pulse(g, 42, 47, 52)
        legs[key] = side_x(LEG_GATE[3], side) + G(0, regrip - slip - sag, -1.5 * regrip)
    return arms, dirs, legs


def straddle_spec_at(poser, path, D_s, f, frames=STRADDLE_FRAMES, gate_f=0.0):
    haul, roll = straddle_state(f, frames)
    sag = straddle_sag(f)
    t = f / frames
    # Before each push the front heaves up (the spade reaching), at the push it slams down onto the claws.
    heave = sum(pulse(f, e - 22, e - 8, e) for e in WEIGHT)
    slam = sum(pulse(f, e - 4, e, e + 12) for e in WEIGHT)
    # rear() lifts node 3 a little too: press it back so the belly stays on the beam while the front heaves.
    pos, ups = body(path, D_s + haul, rear=0.32 * heave - 0.06 * slam, press=sag + 0.07 * heave, gate_f=gate_f,
                    roll=roll, sway=0.02, t=t, head=R(10) - R(18) * heave + R(10) * slam)
    arms, dirs, legs = straddle_feet(f, frames)
    if gate_f:
        arms = {s: fwd(p, gate_f) for s, p in arms.items()}
        legs = {kk: fwd(p, gate_f) for kk, p in legs.items()}
    jaw = R(6) + R(16) * sum(pulse(f, e - 8, e, e + 10) for e in WEIGHT)
    return pose(poser, pos, ups, jaw, arms, legs, dirs)


def straddle():
    path = arch_path()
    D_s = d_straddle(path)

    def fn(poser):
        PLANTED["Straddle"] = straddle_planted()

        def spec(t, f):
            return straddle_spec_at(poser, path, D_s, f)
        return STRADDLE_FRAMES, spec, True
    curve = [round(GK.burden_for_sag(straddle_sag(f)), 3) for f in range(STRADDLE_FRAMES + 1)]
    facts = {"loop": True, "weight_frames": list(WEIGHT), "impact_frame": WEIGHT[0],
             "pivot": "the gate's road centre on the ground (beam centre straight above it), facing over the gate",
             "end_offset_forward_m": 0.0, "end_offset_up_m": 0.0,
             "gate_burden": curve, "base_sag_model_m": BASE_SAG, "push_sag_model_m": BASE_SAG + PUSH_SAG,
             "note": "in place; set Tower.SetBurden(gate_burden[frame]) each frame (TowerDisplacementView: the beam "
                     "top drops 0.307 x burden world), structure hit / camera kick at weight_frames"}
    return fn, facts


def marker_of(key):
    """IK marker bone of an arm side ('L') or a leg key (('L', 1))."""
    return f"IK_Arm_{key}" if isinstance(key, str) else f"IK_Leg_{key[0]}{key[1]}"


def straddle_planted():
    """QA: which feet stand at each loop frame (a spade is up from 4 to 22 before its slam, a hind foot re-grips at
    42..52 after its push)."""
    out = {}
    for side, e in zip(("L", "R"), WEIGHT):
        out[marker_of(side)] = [not (4 < (f - e + 22) % STRADDLE_FRAMES < 22) for f in range(STRADDLE_FRAMES + 1)]
        out[marker_of((side, 3))] = [not (42 < (f - e) % STRADDLE_FRAMES < 52) for f in range(STRADDLE_FRAMES + 1)]
    return out


# ---------------------------------------------------------------------------------------------------------------
# Legs on the gate (shared by Gate_Mount and Gate_Dismount)
# ---------------------------------------------------------------------------------------------------------------

LEG_REACH = 0.8
SIDE_X = {"L": -1.0, "R": 1.0}
LEG_BIAS = {("L", 1): 0, ("R", 1): 3, ("L", 2): 2, ("R", 2): 0, ("L", 3): 1, ("R", 3): 4}
HOOK = G(-1.25, 1.42, 0.41)      # gate space, L: the spade tip hooked down over the crown's far edge
HOOK_DIR = {"L": G(0.05, -1.0, 0.12).normalized(), "R": G(-0.05, -1.0, 0.12).normalized()}


def spine_only(poser, pos, ups):
    tmp = {"Root": {"rot": []}}
    tmp.update(DK.spine_specs(poser, BG.SPINE, pos, ups))
    return tmp


def leg_candidates(gf):
    """Where a leg may stand near a gate centred at forward `gf` (model units), nearest to its natural spot first:
    the ground clear of every tier's pylon foot, the pylon and crown faces, the crown top."""
    def cands(key, hip, nat):
        sx = SIDE_X[key[0]]
        rel = -hip.y - gf
        nf = -nat.y
        out = []
        ahead = nf + 0.12
        if ahead < gf - 0.72 or ahead > gf + 0.72:
            out.append(G(sx * 1.18, 0.0, ahead))
        if rel < 0.05:
            out.append(G(sx * 1.25, min(max(hip.z - 0.3, 0.6), 1.0), gf - (FACE + 0.03)))
            out.append(G(sx * 1.25, 1.3, gf - CROWN_FACE))
        out.append(G(sx * CROWN_XM, CROWN, gf + min(max(rel, -0.12), 0.12)))
        if rel > -0.05:
            out.append(G(sx * 1.25, 1.3, gf + CROWN_FACE))
            out.append(G(sx * 1.25, min(max(hip.z - 0.3, 0.6), 1.0), gf + FACE + 0.03))
        out.sort(key=lambda c: (c - nat).length)
        return out
    return cands


def gate_sagged(points, sag_fn):
    """Feet standing on the gate (above 0.4) go down with the beam."""
    out = {}
    for k, seq in points.items():
        res = []
        for f, (p, planted) in enumerate(seq):
            if p.z > 0.4:
                p = p - Vector((0, 0, sag_fn(f)))
            res.append((p, planted))
        out[k] = res
    return out


def leg_track(poser, frames, body_fn):
    """Hips, natural feet and tucked feet per (leg, frame) for a body motion `body_fn(f) -> (pos, ups)`."""
    hips, nats, tucks = {}, {}, {}
    for f in range(frames + 1):
        pos, ups = body_fn(f)
        tmp = spine_only(poser, pos, ups)
        for key, lg in BG.LEGS.items():
            seg = BG.LEG_SEGS[key[1]][0]
            sx = SIDE_X[key[0]]
            hips[(key, f)] = DK.carried(poser, tmp, seg, lg.root)
            nats[(key, f)] = DK.carried(poser, tmp, seg, lg.foot)
            # Scrabbling: a quick paddle while nothing is in reach.
            ph = 2 * math.pi * (f / 12.0 + 0.3 * key[1] + (0.5 if key[0] == "R" else 0.0))
            tucks[(key, f)] = DK.carried(poser, tmp, seg, lg.root + G(0.3 * sx, -0.32 + 0.1 * math.sin(ph),
                                                                       0.05 + 0.16 * math.cos(ph)))
    return hips, nats, tucks


def arm_dir(side, f, timing):
    """The spade's direction: rest (forward), hooked (down over the crown edge), dug in (down and forward)."""
    lift_at, hook_at, unhook, land = timing
    rest = BG.ARMS[side].foot_dir.normalized()
    k_hook = ease(lift_at, hook_at, f) * (1 - ease(unhook, land, f))
    d = rest.lerp(HOOK_DIR[side], k_hook)
    return d.lerp(DIG[side], ease(unhook, land, f)).normalized()


# ---------------------------------------------------------------------------------------------------------------
# Gate_Mount
# ---------------------------------------------------------------------------------------------------------------

M_RISE = (6, 44)          # the arch rises under the front (it rears against the gate)
M_LEAN = (16, 58)         # the head pushes over the gate
M_HAUL = (62, 140)        # hauled up and over by the hooked spades
M_D_LEAN = -1.45          # gate-space travel after the lean (shoulders within reach of the hooks)
M_ARMS = {"L": (36, 62, 104, 120), "R": (42, 68, 112, 128)}   # braced, hooked, unhooked, dug in on the far side
BRACE = G(-1.25, 1.05, -0.33)    # gate space, L: a spade braced on the crown's near face while it rears


def mount_sag(f):
    s = (BASE_SAG + PUSH_SAG * 0.9) * ease(MOUNT_BELLY - 10, MOUNT_BELLY, f)
    return s - (PUSH_SAG * 0.9) * ease(MOUNT_BELLY + 2, MOUNT_FRAMES, f)


def mount_body(f, D_s):
    G0 = MOUNT_GATE_F
    h = ease(M_RISE[0], M_RISE[1], f)
    D = -G0 + (M_D_LEAN + G0) * ease(M_LEAN[0], M_LEAN[1], f) + (D_s - M_D_LEAN) * ease(M_HAUL[0], M_HAUL[1], f)
    # While it is hauled over, the belly rides a little above the beam's edges (they would dig in), then drops.
    skim = 0.09 * ease(M_HAUL[0], M_HAUL[0] + 20, f) * (1 - ease(MOUNT_BELLY - 10, MOUNT_BELLY, f))
    path = arch_path(G0, apex=HB - BELLY + skim, height=max(h, 1e-3))
    rear = 0.3 * pulse(f, M_RISE[0], 34, M_HAUL[0] + 20)
    crouch = 0.08 * pulse(f, 0, 6, 14)
    head = -R(14) * pulse(f, 10, 30, 60) + R(10) * ease(M_HAUL[1] - 20, M_HAUL[1], f)
    return body(path, D + G0, rear=rear - crouch, press=mount_sag(f), gate_f=G0, sway=0.03 * (1 - h * 0.5),
                t=3 * f / MOUNT_FRAMES, head=head)


def mount_arms():
    """The spades: as it rears they brace on the crowns' near faces, then reach up and hook over the crowns' far
    edges, hold while the body is hauled over, and reach down to the far ground."""
    G0 = MOUNT_GATE_F
    plans = {}
    for side, (lift_at, hook_at, unhook, land) in M_ARMS.items():
        rest = side_x(G(-1.2, 0.0, 2.3), side)
        brace = fwd(side_x(BRACE, side), G0)
        hook = fwd(side_x(HOOK, side), G0)
        ground = fwd(side_x(ARM_GROUND, side), G0)
        plans[side] = GK.FootPlan([(lift_at - 18, rest), (lift_at, brace), (lift_at + 8, brace), (hook_at, hook),
                                   (unhook, hook), (land, ground)],
                                  swing=[0, 18, 0, hook_at - lift_at - 8, 0, land - unhook], lift=0.3)
    return plans


def mount_legs(poser, D_s):
    G0 = MOUNT_GATE_F
    hips, nats, tucks = leg_track(poser, MOUNT_FRAMES, lambda f: mount_body(f, D_s))
    end = {key: fwd(side_x(LEG_GATE[key[1]], key[0]), G0) for key in BG.LEGS}

    def forced(key, f):
        return end[key] if f >= MOUNT_FRAMES - 22 else None
    feet, log = GK.plan_steps(MOUNT_FRAMES, list(BG.LEGS.keys()), lambda k, f: hips[(k, f)],
                              lambda k, f: nats[(k, f)], leg_candidates(G0), LEG_REACH, stride=0.42, swing=8,
                              lift=0.2, forced=forced, tuck=lambda k, f: tucks[(k, f)], bias=LEG_BIAS)
    return gate_sagged(feet, mount_sag), log


MOUNT_LOG = []
PLANTED = {}              # QA: {clip: {marker: [planted per frame]}}


def mount():
    path = arch_path()
    D_s = d_straddle(path)

    def fn(poser):
        legs_plan, log = mount_legs(poser, D_s)
        MOUNT_LOG[:] = log
        PLANTED["Mount"] = {marker_of(k): [pl for _, pl in seq] for k, seq in legs_plan.items()}
        arm_plans = mount_arms()
        PLANTED["Mount"].update({marker_of(s): [arm_plans[s].at(f)[1] for f in range(MOUNT_FRAMES + 1)]
                                 for s in ("L", "R")})
        end_sp = M.shift(poser, straddle_spec_at(poser, path, D_s, 0), MOUNT_GATE_F)

        def spec(t, f):
            pos, ups = mount_body(f, D_s)
            arms = {s: arm_plans[s].at(f)[0] for s in ("L", "R")}
            dirs = {s: arm_dir(s, f, M_ARMS[s]) for s in ("L", "R")}
            legs = {k: legs_plan[k][f][0] for k in BG.LEGS}
            jaw = (R(5) + R(30) * pulse(f, 16, 30, 46)
                   + R(22) * pulse(f, MOUNT_BELLY - 6, MOUNT_BELLY, MOUNT_BELLY + 14))
            sp = pose(poser, pos, ups, jaw, arms, legs, dirs)
            if f > MOUNT_FRAMES - 10:
                sp = M.blend_specs(poser, sp, end_sp, ease(MOUNT_FRAMES - 10, MOUNT_FRAMES, f))
            return sp
        return MOUNT_FRAMES, spec, False
    curve = [round(GK.burden_for_sag(mount_sag(f)), 3) for f in range(MOUNT_FRAMES + 1)]
    facts = {"gate_forward_m": MOUNT_GATE_F, "end_offset_forward_m": MOUNT_GATE_F, "end_offset_up_m": 0.0,
             "hook_frames": [M_ARMS["L"][1], M_ARMS["R"][1]], "weight_frames": [MOUNT_BELLY],
             "impact_frame": MOUNT_BELLY, "gate_burden": curve,
             "note": "authored with the pivot fixed, the gate's road centre gate_forward_m ahead at the start; at the "
                     "last frame move the transform onto the gate centre (end offset, on the ground) and loop "
                     "Gate_Straddle_Loop; burden from gate_burden[frame]; hook_frames: the spades bite the crowns "
                     "(sparks, a clank); weight_frames: the belly lands on the beam (structure hit, camera kick)"}
    return fn, facts


# ---------------------------------------------------------------------------------------------------------------
# Gate_Dismount
# ---------------------------------------------------------------------------------------------------------------

D_SHOVE = 14              # it shoves off the beam (the last weight)
D_POUR = (8, 126)         # the body pours over and down the far side
D_END_F = 5.0             # the pivot's forward offset at the end (the tail clear of the far ramp)
ARM_REACH = 1.42
DISMOUNT_LOG = []


def dismount_sag(f):
    s = BASE_SAG + PUSH_SAG * pulse(f, 2, D_SHOVE, D_SHOVE + 14)
    return s * (1 - ease(70, 112, f))


def dismount_body(f, D_s, D_e):
    skim = 0.08 * ease(D_SHOVE, D_SHOVE + 12, f) * (1 - ease(70, 92, f))
    path = arch_path(apex=HB - BELLY + skim)
    go = ease(D_POUR[0], D_POUR[1], f) ** 1.15
    D = D_s + (D_e - D_s) * go
    gather = pulse(f, 0, 8, D_SHOVE + 4)
    t = f / DISMOUNT_FRAMES
    return body(path, D - 0.08 * gather, rear=0.18 * gather - 0.1 * pulse(f, 20, 40, 70), press=dismount_sag(f),
                sway=0.04 * ease(D_POUR[0], 40, f) * (1 - ease(100, DISMOUNT_FRAMES, f)), t=t * 2.5,
                head=R(10) + R(18) * pulse(f, 6, 30, 70) - R(10) * ease(90, DISMOUNT_FRAMES, f))


def end_feet(P):
    arms = {s: fwd(lg.foot, P) for s, lg in BG.ARMS.items()}
    legs = {k: fwd(lg.foot, P) for k, lg in BG.LEGS.items()}
    return arms, legs


def dismount_plan(poser, D_s, D_e):
    hips, nats, tucks = leg_track(poser, DISMOUNT_FRAMES, lambda f: dismount_body(f, D_s, D_e))
    arms_end, legs_end = end_feet(D_END_F)

    def forced(key, f):
        return legs_end[key] if f >= DISMOUNT_FRAMES - 20 else None
    start_arms, _, start_legs = straddle_feet(0)
    legs, log = GK.plan_steps(DISMOUNT_FRAMES, list(BG.LEGS.keys()), lambda k, f: hips[(k, f)],
                              lambda k, f: nats[(k, f)], leg_candidates(0.0), LEG_REACH, stride=0.42, swing=8,
                              lift=0.32, forced=forced, tuck=lambda k, f: tucks[(k, f)],
                              start={k: start_legs[k] + Vector((0, 0, BASE_SAG)) for k in BG.LEGS}, hold=12,
                              bias=LEG_BIAS)
    # The spades walk on the far ground.
    sh, an = {}, {}
    for f in range(DISMOUNT_FRAMES + 1):
        pos, ups = dismount_body(f, D_s, D_e)
        tmp = spine_only(poser, pos, ups)
        for s, lg in BG.ARMS.items():
            sh[(s, f)] = DK.carried(poser, tmp, "Seg_1", lg.root)
            an[(s, f)] = DK.carried(poser, tmp, "Seg_1", lg.foot)

    def ground(key, hip, nat):
        return [G(nat.x, 0.0, max(-nat.y + 0.15, 1.0))]

    def forced_arm(key, f):
        return arms_end[key] if f >= DISMOUNT_FRAMES - 20 else None
    arms, alog = GK.plan_steps(DISMOUNT_FRAMES, ["L", "R"], lambda k, f: sh[(k, f)], lambda k, f: an[(k, f)],
                               ground, ARM_REACH, stride=0.55, swing=10, lift=0.3, forced=forced_arm,
                               start={s: start_arms[s] for s in ("L", "R")}, gap=6, bias={"L": 0, "R": 4})
    return gate_sagged(legs, dismount_sag), arms, log + alog


def dismount():
    path = arch_path()
    D_s = d_straddle(path)
    D_e = D_END_F + far_excess(path)

    def fn(poser):
        legs_plan, arms_plan, log = dismount_plan(poser, D_s, D_e)
        DISMOUNT_LOG[:] = log
        PLANTED["Dismount"] = {marker_of(k): [pl for _, pl in seq] for k, seq in legs_plan.items()}
        PLANTED["Dismount"].update({marker_of(k): [pl for _, pl in seq] for k, seq in arms_plan.items()})
        start_sp = straddle_spec_at(poser, path, D_s, 0)
        end_sp = M.shift(poser, BG.crawl_spec(poser)[1](0.0, 0), D_END_F)

        def spec(t, f):
            pos, ups = dismount_body(f, D_s, D_e)
            arms = {s: arms_plan[s][f][0] for s in ("L", "R")}
            dig = 1 - ease(D_POUR[0], 40, f)
            dirs = {s: DIG[s].lerp(BG.ARMS[s].foot_dir.normalized(), 1 - dig).normalized() for s in ("L", "R")}
            legs = {k: legs_plan[k][f][0] for k in BG.LEGS}
            jaw = R(6) + R(26) * pulse(f, 4, D_SHOVE, 30)
            sp = pose(poser, pos, ups, jaw, arms, legs, dirs)
            if f < 6:
                sp = M.blend_specs(poser, start_sp, sp, ease(0, 6, f))
            if f > DISMOUNT_FRAMES - 12:
                sp = M.blend_specs(poser, sp, end_sp, ease(DISMOUNT_FRAMES - 12, DISMOUNT_FRAMES, f))
            return sp
        return DISMOUNT_FRAMES, spec, False
    curve = [round(GK.burden_for_sag(dismount_sag(f)), 3) for f in range(DISMOUNT_FRAMES + 1)]
    facts = {"end_offset_forward_m": D_END_F, "end_offset_up_m": 0.0, "weight_frames": [D_SHOVE],
             "impact_frame": D_SHOVE, "gate_burden": curve, "off_gate_frame": 112,
             "note": "starts in Gate_Straddle_Loop's pose (pivot on the gate centre); it shoves off at weight_frames, "
                     "pours head first down the far side, the spades catch the ground, the legs scramble over the "
                     "crowns and the tail slides off the beam (burden 0 from off_gate_frame); at the last frame move "
                     "the transform by end_offset_forward_m and go on with Crawl"}
    return fn, facts

