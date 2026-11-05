"""Round 33: the Cinder Matriarch's gate-crossing clips (imported by build_cindermatriarch_mech.py).

She stands on four long stilts whose feet (x +-2.0 model) land outside the gate's pylons, so she does not climb the
gate like Gravemaw: she rises on her stilts, strides over it and lowers her thorax onto the beam, the sac hanging
down on the near side, and hacks at the beam with her scythes while her weight bends it. Same contract as Gravemaw's
(gate_kit, model units, the gate at 1/0.8 of its world size):

- `Gate_Mount`: from the road (gate centre `MOUNT_GATE_F` ahead) she rises, strides in (no foot lands within
  `FOOT_CLEAR` of the gate along the road) and settles her thorax on the beam (weight at `MOUNT_SETTLE`). Ends in
  Gate_Straddle_Loop frame 0 with the pivot on the gate centre.
- `Gate_Straddle_Loop`: thorax on the beam, stilts braced fore and aft; a scythe rises and hacks down on the beam's far
  edge as she bears down, the other side next (a `weight` event each).
- `Gate_Dismount`: strides off, the sac slides over the beam (it rides up and drops off the far side), she steps
  down to her walking height; ends in the Walk pose `end_offset_forward_m` past the gate centre.
"""

import math

import boss_mech_kit as M
import build_cindermatriarch as BC
import creature_kit3 as K3
import gate_kit as GK
from boss_mech_kit import at, ease, pulse

R = math.radians
ON_BEAM = 0.15            # the body point (rest forward) over the beam: the thorax's lowest part
LIFT = 0.47               # body raised so the thorax underside (1.0 at rest) lies on the beam (1.46 model)
CLEAR = 0.32              # extra lift while she strides over (the head and mandibles clear the crowns)
FOOT_CLEAR = 0.85         # no foot lands closer than this to the gate centre along the road (G3 buttresses 0.65)
BASE_SAG, PUSH_SAG = 0.08, 0.2
MOUNT_GATE_F = 3.6
MOUNT_FRAMES, MOUNT_SETTLE = 150, 128
STRADDLE_FRAMES = 96
WEIGHT = (24, 72)
DISMOUNT_FRAMES, D_SHOVE = 140, 12
D_END_F = 4.4
LEG_REACH = 3.9
PLANTED = {}
LOG = {}


def legs():
    return BC.legs()


def specs(fwd_off, up, pitch=0.0, roll=0.0, neck=0.0, head=0.0, mand=R(4), sac=0.0, sac2=0.0, scythe=None, t=0.0):
    """BC.specs with the body at (forward, up) in pivot space; scythe: {side: (up, out, blade)}."""
    sc = scythe or {}
    sp = BC.specs(off=at(0, fwd_off, up), pitch=pitch, roll=roll, neck=neck, head=head, mand=mand, sac=sac,
                  sac2=sac2, sac_roll=R(4) * math.sin(2 * math.pi * t), scythe_up=0.0)
    for side, sx in BC.SIDES:
        u, o, b = sc.get(side, (0.0, 0.0, 0.0))
        sp[f"Scythe_{side}_Arm"] = {"rot": [(BC.Z, -o * sx), (BC.X, -u)]}
        sp[f"Scythe_{side}_Blade"] = {"rot": [(BC.X, -b)]}
    return sp


def hips(poser, sp):
    pose = poser.solve(sp)
    move = pose["Body"] @ poser.rest["Body"].inverted()
    return {k: move @ lg.root for k, lg in legs().items()}, {k: move @ lg.foot for k, lg in legs().items()}


def ground_candidates(gf):
    def cands(key, hip, nat):
        f = -nat.y + 0.2
        if abs(f - gf) < FOOT_CLEAR:
            f = gf - FOOT_CLEAR if f < gf else gf + FOOT_CLEAR
        return [at(nat.x, f, 0.0)]
    return cands


def straddle_feet(gf=0.0):
    return {k: lg.foot + at(0, gf - ON_BEAM, 0) for k, lg in legs().items()}


# ---------------------------------------------------------------------------------------------------------------
# Gate_Straddle_Loop
# ---------------------------------------------------------------------------------------------------------------

def straddle_sag(f):
    return BASE_SAG + PUSH_SAG * sum(pulse(f, e - 6, e, e + 18) for e in WEIGHT)


def straddle_body(f, gf=0.0):
    t = f / STRADDLE_FRAMES
    heave = sum(pulse(f, e - 20, e - 6, e) for e in WEIGHT)
    slam = sum(pulse(f, e - 3, e, e + 14) for e in WEIGHT)
    side_l = pulse(f, WEIGHT[0] - 22, WEIGHT[0] - 6, WEIGHT[0] + 16)
    side_r = pulse(f, WEIGHT[1] - 22, WEIGHT[1] - 6, WEIGHT[1] + 16)
    sc = {}
    for side, e in zip(("L", "R"), WEIGHT):
        up = pulse(f, e - 22, e - 6, e)
        hit = pulse(f, e - 3, e, e + 18)
        sc[side] = (R(70) * up - R(26) * hit, R(10) * up, -R(50) * up + R(50) * hit)
    up = LIFT - straddle_sag(f) + 0.05 * heave
    return specs(gf - ON_BEAM - 0.06 * heave + 0.08 * slam, up, pitch=R(5) * heave - R(7) * slam,
                 roll=R(5) * (side_r - side_l), neck=R(10) * heave - R(14) * slam, head=-R(6) * slam,
                 mand=R(6) + R(26) * slam, sac=R(8) * heave, sac2=R(6) * heave + R(3) * math.sin(2 * math.pi * t),
                 scythe=sc, t=t)


def straddle_spec_at(poser, f, gf=0.0):
    sp = straddle_body(f, gf)
    # The feet stay put at each blow: the stilts' knees take the bounce.
    return K3.place_legs(poser, sp, legs(), straddle_feet(gf))


def straddle():
    def fn(poser):
        PLANTED["Straddle"] = {f"IK_Leg_{k[0]}{k[1]}": [True] * (STRADDLE_FRAMES + 1) for k in legs()}

        def spec(t, f):
            return straddle_spec_at(poser, f)
        return STRADDLE_FRAMES, spec, True
    curve = [round(GK.burden_for_sag(straddle_sag(f)), 3) for f in range(STRADDLE_FRAMES + 1)]
    return fn, {"loop": True, "weight_frames": list(WEIGHT), "impact_frame": WEIGHT[0], "gate_burden": curve,
                "end_offset_forward_m": 0.0, "end_offset_up_m": 0.0,
                "pivot": "the gate's road centre on the ground, facing over the gate",
                "note": "in place; Tower.SetBurden(gate_burden[frame]); at weight_frames a scythe hacks the beam's far "
                        "edge (left then right): structure hit, sparks"}


# ---------------------------------------------------------------------------------------------------------------
# Gate_Mount
# ---------------------------------------------------------------------------------------------------------------

def mount_sag(f):
    return (BASE_SAG + PUSH_SAG) * ease(MOUNT_SETTLE - 8, MOUNT_SETTLE, f) - PUSH_SAG * ease(MOUNT_SETTLE + 2,
                                                                                            MOUNT_FRAMES, f)


def mount_body(f):
    gf = MOUNT_GATE_F
    t = f / MOUNT_FRAMES
    x = (gf - ON_BEAM) * ease(8, MOUNT_SETTLE - 6, f)
    rise = ease(2, 34, f)
    over = pulse(f, 20, 60, MOUNT_SETTLE - 4)
    settle = ease(MOUNT_SETTLE - 12, MOUNT_SETTLE, f)
    up = (LIFT + CLEAR * over) * rise - mount_sag(f) * settle
    sway = math.sin(2 * math.pi * t * 2.5)
    sc = {s: (R(40) * over + R(20) * pulse(f, MOUNT_SETTLE - 10, MOUNT_SETTLE, MOUNT_FRAMES), R(8) * over,
              -R(30) * over) for s in ("L", "R")}
    land = pulse(f, MOUNT_SETTLE - 6, MOUNT_SETTLE, MOUNT_SETTLE + 14)
    return specs(x, up, pitch=R(6) * pulse(f, 0, 24, 70) - R(4) * land, roll=R(3) * sway * (1 - settle),
                 neck=R(12) * over, head=-R(8) * over, mand=R(5) + R(30) * pulse(f, 6, 18, 34) + R(20) * land,
                 sac=-R(10) * rise, sac2=-R(6) * rise, scythe=sc, t=t)


def mount():
    gf = MOUNT_GATE_F

    def fn(poser):
        hp, nt = {}, {}
        for f in range(MOUNT_FRAMES + 1):
            h, n = hips(poser, mount_body(f))
            for k in legs():
                hp[(k, f)], nt[(k, f)] = h[k], n[k]
        end = straddle_feet(gf)
        feet, log = GK.plan_steps(MOUNT_FRAMES, list(legs().keys()), lambda k, f: hp[(k, f)],
                                  lambda k, f: nt[(k, f)], ground_candidates(gf), LEG_REACH, stride=0.75, swing=12,
                                  lift=0.45, forced=lambda k, f: end[k] if f >= MOUNT_FRAMES - 24 else None,
                                  start={k: lg.foot.copy() for k, lg in legs().items()}, gap=6,
                                  bias={("L", 1): 0, ("R", 2): 0, ("R", 1): 6, ("L", 2): 6})
        LOG["Mount"] = log
        PLANTED["Mount"] = {f"IK_Leg_{k[0]}{k[1]}": [pl for _, pl in seq] for k, seq in feet.items()}
        end_sp = M.shift(poser, straddle_spec_at(poser, 0), gf)

        def spec(t, f):
            sp = K3.place_legs(poser, mount_body(f), legs(), {k: feet[k][f][0] for k in legs()})
            if f > MOUNT_FRAMES - 10:
                sp = M.blend_specs(poser, sp, end_sp, ease(MOUNT_FRAMES - 10, MOUNT_FRAMES, f))
            return sp
        return MOUNT_FRAMES, spec, False
    curve = [round(GK.burden_for_sag(mount_sag(f)), 3) for f in range(MOUNT_FRAMES + 1)]
    return fn, {"gate_forward_m": MOUNT_GATE_F, "end_offset_forward_m": MOUNT_GATE_F, "end_offset_up_m": 0.0,
                "weight_frames": [MOUNT_SETTLE], "impact_frame": MOUNT_SETTLE, "gate_burden": curve,
                "note": "authored with the pivot fixed, the gate centre gate_forward_m ahead; she rises, strides in "
                        "and lowers her thorax onto the beam at impact_frame; then move the transform onto the gate "
                        "centre and loop Gate_Straddle_Loop"}


# ---------------------------------------------------------------------------------------------------------------
# Gate_Dismount
# ---------------------------------------------------------------------------------------------------------------

def dismount_sag(f):
    return (BASE_SAG + PUSH_SAG * pulse(f, 2, D_SHOVE, D_SHOVE + 14)) * (1 - ease(84, 104, f))


SAC_OVER = (40, 104)      # the sac slides over the beam


def dismount_body(f):
    t = f / DISMOUNT_FRAMES
    x = -ON_BEAM + (D_END_F + ON_BEAM) * ease(6, DISMOUNT_FRAMES - 14, f)
    over = pulse(f, SAC_OVER[0] - 20, (SAC_OVER[0] + SAC_OVER[1]) / 2, SAC_OVER[1])
    down = ease(SAC_OVER[1] - 10, DISMOUNT_FRAMES - 8, f)
    waist = pulse(f, 14, 36, 62)          # the narrow waist behind the thorax hangs low: she rises on her stilts
    up = (LIFT + 0.18 * over + 0.62 * waist) * (1 - down) - dismount_sag(f) * (1 - ease(0, 30, f))
    sway = math.sin(2 * math.pi * t * 2.5)
    shove = pulse(f, 0, D_SHOVE, D_SHOVE + 16)
    return specs(x, up, pitch=-R(5) * shove + R(4) * over, roll=R(3) * sway, neck=R(8) * shove,
                 mand=R(5) + R(22) * shove, sac=-R(26) * over, sac2=-R(22) * over, t=t,
                 scythe={s: (R(30) * shove, R(6) * shove, 0.0) for s in ("L", "R")})


def dismount():
    def fn(poser):
        hp, nt = {}, {}
        for f in range(DISMOUNT_FRAMES + 1):
            h, n = hips(poser, dismount_body(f))
            for k in legs():
                hp[(k, f)], nt[(k, f)] = h[k], n[k]
        end = {k: lg.foot + at(0, D_END_F, 0) for k, lg in legs().items()}
        feet, log = GK.plan_steps(DISMOUNT_FRAMES, list(legs().keys()), lambda k, f: hp[(k, f)],
                                  lambda k, f: nt[(k, f)], ground_candidates(0.0), LEG_REACH, stride=0.75, swing=12,
                                  lift=0.45, forced=lambda k, f: end[k] if f >= DISMOUNT_FRAMES - 22 else None,
                                  start=straddle_feet(0.0), gap=6, hold=8,
                                  bias={("L", 1): 0, ("R", 2): 0, ("R", 1): 6, ("L", 2): 6})
        LOG["Dismount"] = log
        PLANTED["Dismount"] = {f"IK_Leg_{k[0]}{k[1]}": [pl for _, pl in seq] for k, seq in feet.items()}
        start_sp = straddle_spec_at(poser, 0)
        end_sp = M.shift(poser, BC.walk_spec(poser)[1](0.0, 0), D_END_F)

        def spec(t, f):
            sp = K3.place_legs(poser, dismount_body(f), legs(), {k: feet[k][f][0] for k in legs()})
            if f < 6:
                sp = M.blend_specs(poser, start_sp, sp, ease(0, 6, f))
            if f > DISMOUNT_FRAMES - 12:
                sp = M.blend_specs(poser, sp, end_sp, ease(DISMOUNT_FRAMES - 12, DISMOUNT_FRAMES, f))
            return sp
        return DISMOUNT_FRAMES, spec, False
    curve = [round(GK.burden_for_sag(dismount_sag(f)), 3) for f in range(DISMOUNT_FRAMES + 1)]
    return fn, {"end_offset_forward_m": D_END_F, "end_offset_up_m": 0.0, "weight_frames": [D_SHOVE],
                "impact_frame": D_SHOVE, "gate_burden": curve, "off_gate_frame": 104,
                "note": "starts in Gate_Straddle_Loop's pose; she shoves off at impact_frame, strides off, the sac "
                        "slides over the beam (burden 0 from off_gate_frame); move the transform by the end offset "
                        "and go on with Walk"}
