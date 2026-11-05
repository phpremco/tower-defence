"""Round 36: smoother attacks for the basic species, built the way the slugs' Strike was (build_slug_strike.py).

Remco (2026-11-05) on the slug fix: "De attack is wel beter nu". The basic species still attacked by bobbing: most had
no strike clip, so Unity threw the whole body about with its procedural attack pose (EnemyView.AttackPose at model
share 0.6), and the three clips that existed eased in and out without a real blow. Every attack below now has:

- anticipation: a short coil away from the target (body back, striking part cocked);
- a fast lunge of the striking part (jaws, blades, sword, tail, sting, mouth) with an ease-out power curve, so it
  arrives at speed;
- a held contact: the striking part stays on the target for two frames from the impact frame;
- an eased follow-through: a damped spring back with one small overshoot, settled before the clip ends.

The root never moves and the feet stay planted (the legs are solved by each builder's own IK), so a lunge is the
spine, neck and limbs reaching; flyers dive and swoop on the Body bone instead (that is their striking motion). Each
clip starts and ends exactly on the frame-0 pose of the clip Unity loops while it stands still (Idle, Glide), blended
in over the first frames of the coil and out over the last frames of the settle.

Clips (impact = the hit event frame):
- rebuilt, same name, length and impact: Stinger Strike (24, 10), Skyray Dive (24, 10), Mireleech Latch (36, 13);
- new `Strike` (24, impact 10, like the Oozer's) for the species that had none: Skitter, Prowler, Roller, Cragback,
  Bulwark, Shardback, Shalebat, Bat. Unity needs a catalog row (ModelClipRole.Attack, event frame 10) for each;
- the Wisp gets two (curator decision 2026-11-05), one per stance, Unity picks by stance: `Strike_Float` (24, 10)
  starts and ends on Float frame 0, `Strike_Ground` (24, 10) on Grounded frame 0.

The Mireleech's mouth reaches by stretching its front segments (slug squash and stretch: length L, width L^-0.65,
height L^-0.35), so its chain bones now carry scale keys and every old clip holds them at scale 1. The Shardback's
shard bones keep their constant scale-1 keys in the new clip (Unity hides shed shards by scale).

Opens ArtSource/Blend/<Name>.blend (made by the species' build script; run that first), keys the full and the LOD1
rig through the builder's own pose functions, exports <Name>.fbx and <Name>_LOD1.fbx with every clip, merges a
`round36` block into the report and saves the .blend. Idempotent: the clip is re-keyed on every run.

Run: blender --background --factory-startup --python Tools/Blender/build_attacks36.py --
     [--only Skitter,Prowler] [--preview <dir>] [--norender] [--before]
     (previews go to <preview>/Attacks36; --before also renders the old attack: the old clip, or a stand-in of
     Unity's procedural pose over the standing clip; run it before the first rebuild.)
"""

import json
import math
import os
import re
import sys
import types

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import boss_mech_kit as BMK  # noqa: E402
import common as C  # noqa: E402
import creature_kit3 as K3  # noqa: E402

X = Vector((1, 0, 0))
Z = Vector((0, 0, 1))
d = math.radians


def sstep(a, b, x):
    return BMK.sstep(a, b, x)


def pulse(f, a, p, b):
    return sstep(a, p, f) * (1 - sstep(p, b, f))


def load_builder(script):
    """The species' build script as a module, without running its main (build_skitter.py calls main() at the top
    level, so that call is cut from the source)."""
    path = os.path.join(HERE, script)
    with open(path, encoding="utf-8") as fh:
        src = fh.read()
    src = re.sub(r"^main\(\)\s*$", "", src, flags=re.M)
    mod = types.ModuleType("td36_" + os.path.splitext(script)[0])
    mod.__file__ = path
    exec(compile(src, path, "exec"), mod.__dict__)
    return mod


# ---------------------------------------------------------------------------------------------------------------
# The beat every attack shares
# ---------------------------------------------------------------------------------------------------------------

class Beat:
    """The timing of one attack in frames.

    r(f): the signed reach: 0 at rest, -coil at the end of the anticipation, 1 from the impact for `hold` frames,
    then a damped spring back (one small overshoot below zero) that has settled by the end.
    coil(f): 0..1 while the body gathers (only before the impact), contact(f): a short pulse around the impact."""

    def __init__(self, frames=24, impact=10, coil_end=5, hold=2, coil=0.25, power=2.2, decay=0.3, freq=0.48):
        self.frames, self.impact, self.coil_end, self.hold = frames, impact, coil_end, hold
        self.coil_amt, self.power, self.decay, self.freq = coil, power, decay, freq

    def r(self, f):
        if f <= self.coil_end:
            return -self.coil_amt * sstep(0, self.coil_end, f)
        if f <= self.impact:
            u = (f - self.coil_end) / (self.impact - self.coil_end)
            return -self.coil_amt + (1 + self.coil_amt) * (1 - (1 - u) ** self.power)
        if f <= self.impact + self.hold:
            return 1.0
        g = f - self.impact - self.hold
        return math.exp(-self.decay * g) * math.cos(self.freq * g) * (1 - sstep(self.frames - 5, self.frames - 1, f))

    def ext(self, f):
        return max(0.0, self.r(f))

    def coil(self, f):
        return max(0.0, -self.r(f)) / self.coil_amt if f < self.impact else 0.0

    def back(self, f):
        """The overshoot after the blow (0..1): the body sags back past its rest for a moment."""
        return max(0.0, -self.r(f)) / 0.15 if f > self.impact else 0.0

    def contact(self, f):
        return pulse(f, self.impact - 2, self.impact, self.impact + self.hold + 4)

    def snap(self, f, before=2, after=1):
        """Jaws and pincers: open through the lunge, shut fast on the impact frame."""
        return sstep(1, self.impact - before, f) * (1 - sstep(self.impact - after, self.impact + 1, f))

    def envelope(self, f):
        """Blend weight from the standing pose into the attack and back: 0 on the first and last frame."""
        return sstep(0, 3, f) * (1 - sstep(self.frames - 4, self.frames, f))


# ---------------------------------------------------------------------------------------------------------------
# Pose maths
# ---------------------------------------------------------------------------------------------------------------

def basis(poser, pose, name):
    return poser.inherited(name, pose).inverted() @ pose[name]


def blend_poses(poser, a, b, w):
    """Pose `a` to pose `b` by w, per bone in parent space (rotation slerp, location and scale lerp)."""
    out = {}
    for name in poser.order:
        la, ra, sa = basis(poser, a, name).decompose()
        lb, rb, sb = basis(poser, b, name).decompose()
        if ra.dot(rb) < 0:
            rb.negate()
        m = Matrix.LocRotScale(la.lerp(lb, w), ra.slerp(rb, w), sa.lerp(sb, w))
        out[name] = poser.inherited(name, out) @ m
    return out


def stretch_chain(poser, pose, chain, lengths, riders):
    """Rebuilds `chain` (bones in order, each the parent of the next) so bone n is lengths[n] times as long, thinner
    as it reaches (width L^-0.65, height L^-0.35, the slugs' volume rule); later bones shift by the extra length.
    `riders` ({bone: parent}) follow their parent's stretched frame without being squashed themselves."""
    out = dict(pose)
    shift = Vector()
    for name in chain:
        m = pose[name]
        length = lengths.get(name, 1.0)
        rot = m.to_3x3().normalized()
        out[name] = (Matrix.Translation(m.translation + shift) @ rot.to_4x4()
                     @ Matrix.Diagonal((length ** -0.65, length, length ** -0.35, 1.0)))
        bone_len = (poser.tail[name] - poser.rest[name].translation).length
        shift = shift + rot.col[1] * bone_len * (length - 1.0)
    for name, parent in riders.items():
        local = pose[parent].inverted() @ pose[name].translation
        out[name] = Matrix.Translation(out[parent] @ local) @ pose[name].to_3x3().normalized().to_4x4()
    return out


# ---------------------------------------------------------------------------------------------------------------
# The attacks. Each returns spec(f) -> a pose dict ({bone: armature-space matrix}) of the attack itself.
# Signs: rotation about X positive dips the front (nose down); game forward is -Y.
# ---------------------------------------------------------------------------------------------------------------

def skitter_strike(m, poser, bt):
    """A mantis stab: the thorax rocks back and the scythe-blades cock high, then the front drives forward and down
    while both blades stab out, the four jaws splay open and clamp on the impact frame."""
    def spec(f):
        r, c, k, s = bt.r(f), bt.coil(f), bt.contact(f), bt.snap(f)
        e = max(0.0, r)
        specs = m.body_specs(offset=Vector((0, -0.06 * r + 0.015 * c, -0.008 * e + 0.01 * c)),
                             thorax_pitch=d(4) * e - d(6) * c, head_pitch=d(3) * e + d(6) * k, jaw=d(34) * s + d(4) * c,
                             blade=d(15) * c + d(4) * e - d(6) * k, blade_spread=d(8) * c - d(3) * e,
                             reach=-d(14) * c + d(10) * e, belly=-d(5) * e + d(4) * c, sway=d(3) * c)
        return poser.solve(m.place_legs(poser, specs, {key: lg.foot for key, lg in m.legs().items()}))
    return spec


def prowler_strike(m, poser, bt):
    """A pack hunter's lunge-bite: weight rocks back onto the haunches with the head drawn in, then the chest and
    neck shoot forward low and the shear-jaws gape and slam shut on contact; the whip tail counter-lifts."""
    def spec(f):
        r, c, k, s = bt.r(f), bt.coil(f), bt.contact(f), bt.snap(f)
        e = max(0.0, r)
        specs = m.body_specs(offset=Vector((0, -0.1 * r + 0.03 * c, m.CROUCH - 0.03 * c - 0.01 * e)),
                             pitch_=d(3) + d(2) * e - d(3) * c, flex=d(6) * r - d(4) * c, neck=-d(14) - d(16) * c + d(6) * e,
                             head=d(6) + d(14) * c - d(10) * e + d(5) * k, jaw=d(4) + d(50) * s,
                             tail=(d(6) * e, 1.5 + 2.0 * e), tail_lift=d(4) + d(7) * e - d(5) * c)
        return poser.solve(m.place_legs(poser, specs, {key: lg.foot for key, lg in m.legs().items()}))
    return spec


def roller_strike(m, poser, bt):
    """The scoop plate tips up as the ball half-curls to gather, then the shell uncurls and the head shoots out of
    the front plate, jaws snapping shut on the impact frame."""
    P = m.Pose(poser)

    def spec(f):
        r, c, k, s = bt.r(f), bt.coil(f), bt.contact(f), bt.snap(f)
        e = max(0.0, r)
        place = m.body_place(offset=Vector((0, -0.04 * r + 0.015 * c, 0.008 * c)), pitch=d(3) * e - d(4) * c)
        return poser.solve(P.specs(0.0425 + 0.22 * c, place, head_pitch=-d(6) * e + d(12) * c + d(6) * k,
                                   head_pull=0.03 * c - 0.1 * e, jaw=d(10) + d(40) * s))
    return spec


def cragback_strike(m, poser, bt):
    """A rock-hide slam: it rears onto its hind legs with the fore pair lifting and the jaw-block raised, then
    crashes the front down, the fore feet stamping and the slabs jolting on the impact frame."""
    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        specs = m.body_specs(offset=Vector((0, -0.05 * r, 0.03 * c - 0.02 * e)), pitch=-d(10) * c + d(5) * e,
                             front=-d(18) * c + d(20) * e, jaw=d(22) * c + d(4),
                             plates=(d(5) * c + d(6) * k, d(5) * c + d(6) * k, -d(5) * c + d(5) * k))
        feet = {}
        for key, lg in m.legs().items():
            if key[1] == 1:
                feet[key] = lg.foot + Vector((0, 0.02 * c - 0.04 * e, 0.09 * c))
            elif key[1] == 2:
                feet[key] = lg.foot + Vector((0, 0, 0.03 * c))
            else:
                feet[key] = lg.foot
        return poser.solve(m.place_legs(poser, specs, feet))
    return spec


def bulwark_strike(m, poser, bt):
    """An overhead chop: the spine twists away and leans back as the sword arm cocks high behind the head, then the
    torso unwinds and leans in, the blade comes down through the target and holds there, the shield tucks in."""
    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        specs = m.body_specs(offset=Vector((0, -0.04 * r, m.CROUCH - 0.03 * e - 0.01 * c)), lean=d(6) + d(10) * r,
                             twist=-d(20) * c + d(14) * e, neck=-d(3) - d(4) * c + d(3) * e, head=d(4) * e,
                             jaw=d(14) * c, tail=(d(8) * e, 1.0 + 2.0 * r, -d(4) + d(10) * e),
                             sword=(d(5) + d(115) * c + d(22) * e, d(6) + d(18) * c, d(10) + d(70) * c - d(4) * e),
                             shield=(d(10) + d(20) * c + d(8) * e, d(4) + d(10) * c, d(15) + d(20) * c))
        return poser.solve(m.place_legs(poser, specs, {key: lg.foot for key, lg in m.legs().items()}))
    return spec


def shardback_strike(m, poser, bt):
    """A ram-and-spit: the armoured front rears back with the jaws gaping, then the body drives forward and dips,
    the head snapping down and the jaws flaring wide on the impact frame (the shard spit leaves the throat)."""
    lg = m.legs()

    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        specs = m.body_specs(offset=Vector((0, -0.09 * r + 0.02 * c, -0.015 * e + 0.015 * c)),
                             pitch_=d(6) * e - d(9) * c, head=d(14) * e - d(14) * c + d(8) * k,
                             jaws=d(4) + d(24) * c + d(34) * k)
        return poser.solve(K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()}))
    return spec


def bat_strike(m, poser, bt, base_arm):
    """A spit from the air: the wings lift high and the body rears, then a hard downbeat throws it into a short
    dive with the head thrust out and the jaw wide on the impact frame (the bolt leaves), and it climbs back with
    one more beat."""
    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        g = max(0.0, f - bt.impact - bt.hold)
        beat2 = math.sin(0.55 * g) * math.exp(-0.18 * g) * (f > bt.impact + bt.hold) * (1 - sstep(bt.frames - 5, bt.frames - 1, f))
        specs = m.specs_for(loc=Vector((0, -0.12 * e + 0.03 * c, -0.07 * e + 0.05 * c)), pitch=d(22) * r,
                            roll=d(6) * e, yaw=d(4) * e, head=d(12) * r, jaw=d(3) + d(36) * k + d(8) * e,
                            arm=base_arm + d(38) * c - d(34) * e + d(28) * beat2, sweep=-d(8) * c + d(10) * e,
                            hand=d(22) * c - d(16) * e + d(14) * beat2, fold=-d(4) + d(10) * e,
                            finger_lift=d(10) * c, tail=(d(14) * c - d(10) * e, d(6) * e))
        return poser.solve(specs)
    return spec


def stinger_strike(m, poser, bt):
    """The tail coils down and back over the abdomen while the claws spread, then the tail whips up over the back
    and the sting drives forward into the target and holds, quivering; the body leans into it with the feet set."""
    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        # A whip: the tail base leads, the sting arrives last (each segment a frame later).
        lag = [max(0.0, bt.r(f - i * 0.45)) for i in range(4)]
        hold = sstep(bt.impact, bt.impact + 1, f) * (1 - sstep(bt.impact + bt.hold + 1, bt.impact + bt.hold + 6, f))
        specs = m.body_specs(offset=Vector((0, -0.03 * r, -0.012 * e)), pitch_=d(6) * r, belly=d(6) * r,
                             tail=(-d(8) * c - d(26) * lag[2], 0.0, 0.0), tail_base=-d(10) * c + d(82) * lag[0],
                             sting=-d(12) * c + d(20) * lag[3] + d(4) * hold * math.sin(f * 2.2),
                             claws=d(10) * e, claw_spread=d(16) * c + d(6) * e, pinch=d(28) * c + d(10) * k,
                             claw_raise=d(10) * c)
        return poser.solve(m.place_legs(poser, specs, {key: lg.foot for key, lg in m.legs(m.CFG).items()}, m.CFG))
    return spec


def skyray_dive(m, poser, bt):
    """A swooping tail lash: it climbs a little with the nose up and the tail curled over its back, then dives down
    and forward with the fins swept back while the tail cracks through like a whip (the tip lands on the impact
    frame), and it pulls up with one beat of the fins."""
    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        g = max(0.0, f - bt.impact - bt.hold)
        beat = math.sin(0.5 * g) * math.exp(-0.16 * g) * (f > bt.impact + bt.hold) * (1 - sstep(bt.frames - 5, bt.frames - 1, f))
        whip = [bt.r(f - 0.9 * i) for i in range(5)]
        fins = {(sd, "A"): [-d(10) * c - d(16) * e + d(9) * beat, -d(4) * e + d(5) * beat, d(10) * e + d(6) * beat]
                for sd, _ in m.SIDES}
        fins.update({(sd, "B"): [-d(8) * c - d(12) * e + d(7) * beat, -d(4) * e + d(6) * beat, d(14) * e + d(7) * beat]
                     for sd, _ in m.SIDES})
        return poser.solve(m.specs_for(loc=Vector((0, -0.14 * e + 0.03 * c, -0.11 * e + 0.05 * c)), pitch=d(24) * r,
                                       head=-d(6) * e, pincer=d(5) + d(30) * k, fins=fins, sweep=d(24) * e,
                                       tail_pitch=[d(10) * c * (0.6 + 0.2 * i) - d(12) * max(0.0, w) for i, w in enumerate(whip)],
                                       tail_yaw=[d(9) * max(0.0, w) * (i - 1) / 3 for i, w in enumerate(whip)]))
    return spec


LEECH_SHARE = {"Seg_3": 0.08, "Seg_4": 0.18, "Seg_5": 0.3, "Seg_6": 0.36, "Head": 0.22}


def mireleech_latch(m, poser, bt):
    """The rear sucker stays put. The front rears and bunches up with the petals spread, then the front half
    stretches thin and lunges, slapping the mouth onto the target on the bite frame; it stays latched, the stretched
    body pumping three sucks, then lets go and snaps back with one bunching overshoot."""
    release = 28

    def reach(f):
        if f <= bt.impact:
            return bt.r(f)
        if f <= release:
            return 1.0 - 0.12 * sstep(bt.impact, bt.impact + 4, f)
        g = f - release
        return 0.88 * math.exp(-0.32 * g) * math.cos(0.55 * g) * (1 - sstep(bt.frames - 4, bt.frames - 1, f))

    def spec(f):
        e0 = reach(f)
        e = max(0.0, e0)
        c = bt.coil(f) if f < bt.impact else 0.0
        latched = sstep(bt.impact - 1, bt.impact + 1, f) * (1 - sstep(release - 1, release + 3, f))
        suck = latched * max(0.0, math.sin((f - bt.impact - 1) * 2 * math.pi / 5.0))
        rear_lift = sstep(0, bt.coil_end, f) * (1 - sstep(bt.coil_end, bt.impact, f))
        pose = poser.solve(m.body_specs(lift=d(34) * rear_lift - d(5) * latched, head=-d(12) * rear_lift + d(10) * latched,
                                        petals=d(55) * sstep(0, bt.coil_end, f) * (1 - sstep(bt.impact - 1, bt.impact, f))
                                        - d(8) * latched + d(12) * suck,
                                        throb=(d(1.2) * latched, (f - bt.impact) * 2 * math.pi / 5.0),
                                        wave=(d(4) * c, 1.5)))
        lengths = {n: 1.0 + s * (0.55 * e0 - 0.06 * suck) for n, s in LEECH_SHARE.items()}
        return stretch_chain(poser, pose, m.CHAIN, lengths, {p: "Head" for p in m.PETALS})
    return spec


TIP_CLEAR = 0.01


def wisp_strike(m, poser, bt, grounded):
    """An energy slam in two stances (curator 2026-11-05: Unity picks the clip by stance). The bud draws up and back
    with the petals closing and the rings tilting away, then throws itself forward and down: the petals burst open
    on the impact frame and both rings whirl a quarter turn. Grounded, the tendrils stay planted and the bud slams
    onto them; floating, it dives a little and the tendrils trail from it."""
    drop = -(m.FLOAT_UP - m.GROUND_UP) if grounded else 0.0
    tilt_a, tilt_b = (m.FLAT_A, m.FLAT_B) if grounded else (0.0, 0.0)

    def spec(f):
        r, c, k = bt.r(f), bt.coil(f), bt.contact(f)
        e = max(0.0, r)
        spin = d(90) * sstep(bt.coil_end, bt.impact + bt.hold + 4, f) * (1 - sstep(bt.frames - 8, bt.frames - 1, f))
        if grounded:
            lift = drop + 0.05 * c - 0.03 * k - 0.01 * e
        else:
            lift = 0.04 * c - 0.07 * e
        specs = m.core_specs(lift=lift, sway=(-d(12) * c + d(18) * e, 0.0), petals=d(10 if grounded else 5) - d(12) * c
                             + d(38) * k + d(8) * e, petal_wave=d(3) * e, w=f * 0.6,
                             ring_a=(spin, tilt_a - d(8) * c + d(8) * e, None),
                             ring_b=(-spin, tilt_b - d(6) * c + d(9) * e, None))
        specs["Core"]["loc"] = specs["Core"]["loc"] + Vector((0, -(0.04 if grounded else 0.08) * r, 0))
        if grounded:
            feet = [m.plant_point(i) for i in range(3)]
        else:
            # The trailing tips brush the road as it dives instead of passing through it.
            feet = [Vector((p.x, p.y, max(p.z, TIP_CLEAR))) for p in m.hanging_feet(poser, specs, 0.0, 0.0)]
        return poser.solve(m.place_tendrils(poser, specs, feet))
    return spec


# ---------------------------------------------------------------------------------------------------------------
# The species
# ---------------------------------------------------------------------------------------------------------------

SPECIES = {
    # script, clip, base (the clip Unity loops while it stands at a target), body bone, beat, attack, procedural
    # style of the old look (None: it had its own clip), LOD keying ("common" or the builder's own).
    "Skitter": dict(script="build_skitter.py", clip="Strike", base="Idle", body="Thorax", beat={}, attack=skitter_strike,
                    old="Bite", lod="skitter"),
    "Prowler": dict(script="build_prowler.py", clip="Strike", base="Idle", body="Pelvis", beat={}, attack=prowler_strike,
                    old="Bite"),
    "Roller": dict(script="build_roller.py", clip="Strike", base="Idle", body="Band_4", beat={}, attack=roller_strike,
                   old="Bite"),
    "Cragback": dict(script="build_cragback.py", clip="Strike", base="Idle", body="Body", beat=dict(coil=0.3, coil_end=6),
                     attack=cragback_strike, old="Slam"),
    "Bulwark": dict(script="build_bulwark.py", clip="Strike", base="Idle", body="Pelvis", beat=dict(coil=0.3, coil_end=6),
                    attack=bulwark_strike, old="Slash"),
    "Shardback": dict(script="build_shardback.py", clip="Strike", base="Idle", body="Body", beat={},
                      attack=shardback_strike, old="Sting", hold_scale="shard_bones_in_shed_order"),
    "Shalebat": dict(script="build_shalebat.py", clip="Strike", base="Glide", body="Body", beat={}, flyer=True,
                     attack=lambda m, p, b: bat_strike(m, p, b, d(10)), old="Bolt"),
    "Bat": dict(script="build_bat.py", clip="Strike", base="Glide", body="Body", beat={}, flyer=True,
                attack=lambda m, p, b: bat_strike(m, p, b, d(8)), old="Bolt"),
    "Stinger": dict(script="build_stinger.py", clip="Strike", base="Idle", body="Body", beat=dict(coil_end=5),
                    attack=stinger_strike, old=None),
    "Skyray": dict(script="build_skyray.py", clip="Dive", base="Idle", body="Body", beat={}, flyer=True,
                   attack=skyray_dive, old=None),
    "Mireleech": dict(script="build_mireleech.py", clip="Latch", base="Idle", body="Seg_1",
                      beat=dict(frames=36, impact=13, coil_end=7, coil=0.2), attack=mireleech_latch, old=None,
                      stretch=True),
    # Two stances, two clips (curator decision 2026-11-05): the same beats, each from and back to its own pose.
    "Wisp": dict(script="build_wisp.py", body="Core", beat={}, old="Slam",
                 variants=[dict(clip="Strike_Float", base="Float", attack=lambda m, p, b: wisp_strike(m, p, b, False),
                                hovering=True),
                           dict(clip="Strike_Ground", base="Grounded",
                                attack=lambda m, p, b: wisp_strike(m, p, b, True))]),
}

# Unity's procedural attack pose (EnemyView.AttackPose; pitch/yaw degrees, lift and lunge in world units), for the
# stand-in of the old look.
OLD_STYLES = {"Bite": (14, -12, 9, -0.08, 0.42), "Slam": (-22, 30, 0, 0.35, 0.6), "Slash": (-28, 32, 18, 0.0, 0.28),
              "Sting": (10, 5, 0, 0.0, 0.105), "Bolt": (-20, 18, 0, 0.0, -0.105)}


class Species:
    def __init__(self, name):
        self.name = name
        self.species_cfg = SPECIES[name]
        self.variants = self.species_cfg.get("variants") or [{}]
        self.cfg = dict(self.species_cfg)
        self.use(0)
        self.done = {}
        self.mod = load_builder(self.cfg["script"])
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects[name + "_LOD1_Rig"]
        self.lod_mesh = bpy.data.objects[name + "_LOD1"]
        with open(self.report_path(), encoding="utf-8") as fh:
            self.rep = json.load(fh)
        old = self.rep.get("actions")
        self.original = list(old) if old else []
        self.scale_bones = list(self.rep.get("scale_keyed_bones") or [])
        if self.cfg.get("stretch"):
            self.scale_bones = list(dict.fromkeys(self.scale_bones + list(self.mod.CHAIN)))
        lod_names = {b.name for b in self.lod_arm.data.bones}
        self.poser = B.ScalePoser(self.arm, self.scale_bones)
        self.lod_poser = B.ScalePoser(self.lod_arm, [n for n in self.scale_bones if n in lod_names])
        self.beat = Beat(**self.cfg["beat"])
        self.new = self.new_lod = None
        self.unity_scale = self.rep.get("unity_scale") or getattr(self.mod, "GAME_SCALE", 0.8)

    def use(self, i):
        """Switches to clip variant i (most species have one)."""
        self.cfg = {**self.species_cfg, **self.variants[i]}
        self.cfg.pop("variants", None)

    def report_path(self):
        return os.path.join(C.EXPORTS, self.name, self.name + "_report.json")

    def spec_fn(self):
        """The keyed spec: the attack blended from and back to the base clip's frame 0, as absolute bone matrices."""
        bt = self.beat
        _, base_spec, _ = self.mod.CLIPS[self.cfg["base"]](self.poser)
        base = self.poser.solve(base_spec(0.0, 0))
        attack = self.cfg["attack"](self.mod, self.poser, bt)

        def spec(t, f):
            pose = blend_poses(self.poser, base, attack(f), bt.envelope(f))
            return {n: {"abs": m} for n, m in pose.items()}
        return spec

    def key(self):
        clip = self.cfg["clip"]
        for n in (clip, clip + "_LOD"):
            act = bpy.data.actions.get(n)
            if act is not None:
                bpy.data.actions.remove(act)
        spec = self.spec_fn()
        frames = self.beat.frames
        self.new = self.poser.action(clip, frames, spec)
        if self.cfg.get("lod") == "skitter":
            self.new_lod = self.mod.lod_action(self.poser, self.lod_poser, clip + "_LOD", frames, spec, False)
        else:
            self.new_lod = C.lod_action(self.poser, self.lod_poser, clip + "_LOD", frames, spec, False,
                                        getattr(self.mod, "LOD_AIMS", {}))
        self.done[clip] = (self.new, self.new_lod)
        C.log(self.name, "keyed", clip, frames)

    def hold_old_scales(self):
        """Constant scale 1 on newly scaled bones in every other clip (they had no scale keys before)."""
        if not self.cfg.get("stretch"):
            return
        lod_names = {b.name for b in self.lod_arm.data.bones}
        for clip in self.original:
            if clip in {v.get("clip", self.species_cfg.get("clip")) for v in self.variants}:
                continue
            BMK.hold_scale(bpy.data.actions[clip], {b: 1.0 for b in self.mod.CHAIN})
            BMK.hold_scale(bpy.data.actions[clip + "_LOD"], {b: 1.0 for b in self.mod.CHAIN if b in lod_names})

    def actions(self, lod=False):
        names = self.original + [c for c in self.done if c not in self.original]
        i = 1 if lod else 0
        return {n: (self.done[n][i] if n in self.done else bpy.data.actions[n + ("_LOD" if lod else "")])
                for n in names}

    def export(self):
        folder = os.path.join(C.EXPORTS, self.name)
        BMK.export_rig(os.path.join(folder, self.name + ".fbx"), self.arm, [self.mesh], self.actions())
        BMK.export_rig(os.path.join(folder, self.name + "_LOD1.fbx"), self.lod_arm, [self.lod_mesh], self.actions(True))

    def write_report(self, facts):
        rep = self.rep
        rep["actions"] = {n: int(round(a.frame_range[1])) for n, a in self.actions().items()}
        if self.cfg.get("stretch"):
            rep["scale_keyed_bones"] = self.scale_bones
        rep["round36"] = facts
        with open(self.report_path(), "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=2)
        C.log("report", self.report_path())

    # -- measurements ------------------------------------------------------------------------------------------

    def mesh_points(self, act, frame):
        dg = bpy.context.evaluated_depsgraph_get()
        C.set_action(self.arm, act, frame)
        dg.update()
        ev = self.mesh.evaluated_get(dg)
        me = ev.to_mesh()
        pts = [ev.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
        return pts

    def facts(self):
        bt, clip = self.beat, self.cfg["clip"]
        body = self.cfg["body"]
        spec = self.spec_fn()
        rest = self.poser.solve(spec(0.0, 0))[body].translation
        travel = [(self.poser.solve(spec(f / bt.frames, f))[body].translation - rest) for f in range(bt.frames + 1)]
        at_impact = travel[bt.impact]
        tip = {}
        for label, f in (("rest", 0), ("impact", bt.impact)):
            pts = self.mesh_points(self.new, f)
            tip[label] = {"tip_forward": round(-min(p.y for p in pts), 3), "top": round(max(p.z for p in pts), 3),
                          "bottom": round(min(p.z for p in pts), 3)}
        out = {clip: {"frames": bt.frames, "loop": False, "impact_frame": bt.impact,
                      "anticipation_frames": [0, bt.coil_end], "contact_frames": [bt.impact, bt.impact + bt.hold],
                      "settled_by_frame": bt.frames - 1, "starts_and_ends_on": f"{self.cfg['base']} frame 0",
                      "body_bone": body,
                      "body_travel_at_impact": {"forward": round(-at_impact.y, 3), "up": round(at_impact.z, 3)},
                      "body_travel_max": round(max(v.length for v in travel), 3),
                      "root_moves": False,
                      "feet_planted": not (self.cfg.get("flyer") or self.cfg.get("hovering")),
                      "mesh": tip}}
        out["note"] = ("anticipation coil, fast ease-out lunge of the striking part, held contact, damped spring "
                       "follow-through; root planted (flyers dive and swoop on the Body bone)")
        if self.cfg["old"]:
            out[clip]["new_clip"] = True
            out[clip]["unity"] = f"new clip: catalog row BlenderClipSpec(\"{clip}\", ModelClipRole.Attack, {bt.impact})"
        if self.cfg.get("stretch"):
            out["chain_scale_bones"] = list(self.mod.CHAIN)
        return out


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

class Stage:
    def __init__(self, sp):
        self.sp = sp
        for o in bpy.data.objects:
            if o.type == "MESH" and o is not sp.mesh:
                o.hide_render = True
        base = bpy.data.actions[sp.cfg["base"]]
        pts = sp.mesh_points(base, 0)
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        self.size = max(hi - lo)
        self.centre = (lo + hi) / 2
        floor = lo.z - 0.35 * self.size if sp.cfg.get("flyer") else 0.0
        C.preview_world()
        self.lights = C.light_rig(scale=1.2 * self.size)
        self.objs = [C.ground(size=40.0, z=floor)]
        # The target (a gate post) just past the front of the creature.
        u = self.size
        front = -lo.y
        self.objs.append(BMK.box("Target", -0.3 * u, 0.3 * u, front + 0.12 * u, front + 0.2 * u, floor, floor + 0.35 * u,
                                 BMK.mat("TargetMat", (0.35, 0.4, 0.5), rough=0.6)))
        target = self.centre + Vector((0, -0.15 * u, 0))
        self.side = BMK.side_cam(target, 1.9 * u, elevation=6.0, azimuth=-90.0)
        self.game = C.game_camera(target, 1.5 * u)

    def strips(self, act, label, frames, out):
        paths = []
        for key, cam in (("side", self.side), ("game", self.game)):
            paths.append(BMK.strip(self.sp.name, self.sp.arm, act, cam, out, f"{label}_{key}", frames, columns=4,
                                   cell=400, samples=12))
        return paths

    def procedural(self, label, out):
        """A stand-in of Unity's procedural attack over the base clip (model share 0.6, a 0.8 s attack)."""
        sp = self.sp
        pitch_rear, pitch_strike, yaw_amt, lift_amt, lunge = OLD_STYLES[sp.cfg["old"]]
        share, us = 0.6, sp.unity_scale
        act = bpy.data.actions[sp.cfg["base"]]
        frames = list(range(0, 25, 3))
        paths = {"side": [], "game": []}
        for i, f in enumerate(frames):
            t = f / 24
            if t < 0.4:
                rear, strike = sstep(0, 1, t / 0.4), 0.0
            else:
                k = (t - 0.4) / 0.6
                rear, strike = max(0.0, 1 - sstep(0, 1, k * 3)), math.sin(min(1.0, k) * math.pi)
            pitch = rear * pitch_rear + strike * pitch_strike
            yaw = strike * math.sin(strike * math.pi * 3) * yaw_amt if sp.cfg["old"] == "Bite" else (rear - strike) * yaw_amt
            lift = (-rear if sp.cfg["old"] == "Bite" else rear) * abs(lift_amt)
            along = strike * lunge * share - rear * 0.12 * share
            sp.arm.location = Vector((0, -along / us, lift * share / us))
            sp.arm.rotation_euler = (d(pitch * share), 0, d(yaw * share))
            C.set_action(sp.arm, act, f % int(act.frame_range[1]))
            for key, cam in (("side", self.side), ("game", self.game)):
                paths[key].append(C.render(cam, os.path.join(out, f"_{sp.name}_proc_{key}_{i}.png"), 640, 360, samples=12))
        sp.arm.location = Vector()
        sp.arm.rotation_euler = (0, 0, 0)
        out_paths = []
        for key, ps in paths.items():
            path = os.path.join(out, f"{sp.name}_{label}_{key}.png")
            C.contact_sheet(ps, path, 4, 400)
            for p in ps:
                os.remove(p)
            out_paths.append(path)
        return out_paths

    def remove(self):
        BMK.remove(self.objs + [self.side, self.game] + list(self.lights or []))


def before(sp, out):
    st = Stage(sp)
    if sp.cfg["old"]:
        suffix = f"_{sp.cfg['base']}" if len(sp.variants) > 1 else ""
        paths = st.procedural("before_procedural" + sp.cfg["old"] + suffix, out)
    else:
        act = bpy.data.actions[sp.cfg["clip"]]
        n = int(round(act.frame_range[1]))
        paths = st.strips(act, "before_" + sp.cfg["clip"], sorted(set(BMK.frames_of(n, 12))), out)
    st.remove()
    return paths


def after(sp, out):
    st = Stage(sp)
    bt = sp.beat
    i = bt.impact
    frames = sorted({0, bt.coil_end // 2, bt.coil_end, (bt.coil_end + i) // 2, i - 1, i, i + bt.hold, i + bt.hold + 3,
                     i + bt.hold + 7, (i + bt.frames) // 2 + 4, bt.frames - 2, bt.frames})
    paths = st.strips(sp.new, "after_" + sp.cfg["clip"], frames[:12], out)
    st.remove()
    return paths


# ---------------------------------------------------------------------------------------------------------------

def build(name, args, out):
    sp = Species(name)
    if args.get("before") and not args.get("norender"):
        for i in range(len(sp.variants)):
            sp.use(i)
            before(sp, out)
    sp.hold_old_scales()
    facts = {}
    for i in range(len(sp.variants)):
        sp.use(i)
        sp.key()
        facts.update(sp.facts())
    C.log(name, json.dumps(facts))
    sp.export()
    sp.write_report(facts)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"), compress=True)
    if not args.get("norender"):
        for i in range(len(sp.variants)):
            sp.use(i)
            sp.new = sp.done[sp.cfg["clip"]][0]
            after(sp, out)


def main():
    args = C.cli_args()
    out = os.path.join(C.preview_dir(), "Attacks36")
    os.makedirs(out, exist_ok=True)
    only = [n for n in (args.get("only") or ",".join(SPECIES)).split(",") if n]
    for name in only:
        build(name, args, out)


if __name__ == "__main__":
    main()
