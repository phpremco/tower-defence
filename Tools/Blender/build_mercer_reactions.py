"""Round 35: Mercer's reaction clips (Docs/BOSS_INTROS.md, Docs/CUTSCENES_CONTINENTS.md).

Opens ArtSource/Blend/Mercer.blend (round 16: mesh, LOD1, rig and the eleven clips), keys five new clips with
build_mercer's own pose language (`compose`, `stance`, `aim_pose`), exports Mercer.fbx with all sixteen clips, merges
the facts into Mercer_report.json and saves the .blend. Nothing is rebaked; the mesh, maps, rig and the old clips stay
as they are. Running it twice gives the same result (the round-35 clips are removed first).

- Startled: a sharp flinch (shoulders up, head jerks), half a step back, the pistol snaps up two-handed and points
  high, the head tilts up at something huge; ends in the tense hold.
- Startled_Hold: loop of that hold (shallow fast breathing, a tremble on the gun, a small scan with the head).
- Disbelief: cutscene beat; leans back, the free (left) hand rises to the jaw below the visor, the head shakes once.
- Point_Shout: bark; the chest drives forward, the left arm points flat-handed at the target, two jabs. Legs and hips
  stay in the stance, so it layers on the upper body over Idle or Walk.
- Lower_Weapon: from the aim (Shoot frame 0) the gun comes down, the support hand lets go, a deep breath in and the
  shoulders drop (relief). Ends near Idle frame 0 with the pistol hanging in the right hand.

Run: blender --background --factory-startup --python Tools/Blender/build_mercer_reactions.py --
     [--preview <dir>] [--norender]
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import build_mercer as BM  # noqa: E402
import common as C  # noqa: E402
from build_mercer import SIDE, STANCE, UP, compose, ease, foot_at, lerp_m, rad, sock_frame, stance  # noqa: E402
from common import G  # noqa: E402

NAME = BM.NAME
ROUND = 35


# ------------------------------------------------------------------------------------------------------------
# Key-pose machinery: body numbers blend as in build_mercer; an arm that is IK in either neighbouring key is driven
# by IK in armature space between the two achieved socket frames, so FK <-> IK changes have no elbow or hand jump.
# ------------------------------------------------------------------------------------------------------------

ARM_FIELDS = ("mode", "sock", "space", "pole")


def snap(s):
    """Fast out, soft landing (a reflex)."""
    s = min(1.0, max(0.0, s))
    return 1 - (1 - s) ** 3


def accel(s):
    s = min(1.0, max(0.0, s))
    return s * s


EASINGS = {"ease": ease, "snap": snap, "in": accel, "linear": lambda s: min(1.0, max(0.0, s))}


class Arms:
    """Achieved socket frame and elbow pole of each arm for one key pose (cached per pose)."""

    def __init__(self, poser, P):
        pose = poser.solve(compose(poser, dict(P)))
        chest_move = pose["Spine2"] @ poser.rest["Spine2"].inverted()
        inv = chest_move.to_3x3().inverted()
        self.sock, self.pole, self.ik = {}, {}, {}
        for name in SIDE:
            self.sock[name] = pose[f"Weapon_{name}"].copy()
            shoulder = pose[f"UpperArm_{name}"].translation
            elbow = pose[f"Forearm_{name}"].translation
            hand = pose[f"Hand_{name}"].translation
            self.pole[name] = inv @ (elbow - (shoulder + hand) * 0.5).normalized()
            self.ik[name] = P.get(f"arm.{name}.mode", "fk") == "ik"


class KeyClip:
    """[(frame, P, easing into this key)] -> per-frame pose dicts; `extra(f, P, arms)` adds motion on top."""

    def __init__(self, poser, keys, extra=None):
        self.poser = poser
        self.keys = keys
        self.arms = [Arms(poser, P) for _, P, _ in keys]
        self.extra = extra

    def pose(self, f):
        keys = self.keys
        if f <= keys[0][0]:
            i, e = 0, 0.0
        elif f >= keys[-1][0]:
            i, e = len(keys) - 2, 1.0
        else:
            i = max(j for j in range(len(keys) - 1) if keys[j][0] <= f)
            f0, f1 = keys[i][0], keys[i + 1][0]
            e = EASINGS[keys[i + 1][2]]((f - f0) / (f1 - f0))
        P0, P1 = keys[i][1], keys[i + 1][1]
        a0, a1 = self.arms[i], self.arms[i + 1]
        P = BM.blend(P0, P1, e)
        for name in SIDE:
            if a0.ik[name] or a1.ik[name]:
                for k in ARM_FIELDS:
                    P.pop(f"arm.{name}.{k}", None)
                P[f"arm.{name}.mode"] = "ik"
                P[f"arm.{name}.space"] = "world"
                P[f"arm.{name}.sock"] = lerp_m(a0.sock[name], a1.sock[name], e)
                P[f"arm.{name}.pole"] = a0.pole[name].lerp(a1.pole[name], e)
        if self.extra:
            self.extra(f, P)
        return P

    def make(self, frames, loop=False):
        def fn(poser):
            def spec(t, f):
                return compose(poser, self.pose(f))
            return frames, spec, loop
        return fn


def nudge_socket(P, name, offset=Vector(), pitch=0.0):
    """Moves an IK socket in armature space (offset) and tips its muzzle up by `pitch` degrees about its own X."""
    m = P[f"arm.{name}.sock"]
    t = m.translation.copy()
    r = Matrix.Translation(t) @ Matrix.Rotation(rad(pitch), 4, m.to_3x3().col[0]) @ Matrix.Translation(-t) @ m
    r.translation = t + Vector(offset)
    P[f"arm.{name}.sock"] = r


# ------------------------------------------------------------------------------------------------------------
# Shared poses
# ------------------------------------------------------------------------------------------------------------

def idle0():
    """Idle frame 0, where the standing clips start and end (so Unity's cross-fades are short)."""
    return BM.idle_p(0.0, 0)


def aim_at(elev=0.0, raise_=0.0, back=0.0, right=0.0):
    """build_mercer.aim_pose with the muzzle `elev` degrees higher, the grip `raise_` higher, `back` closer and
    `right` further to his right (so a high aim does not hide the visor from a front camera)."""
    e = rad(elev)
    r = sock_frame(BM.AIM_GRIP + G(-right, raise_, -back), G(0.02, math.sin(e), math.cos(e)), UP)
    left = r @ Matrix.Translation(BM.SUPPORT_OFFSET) @ Matrix.Rotation(rad(BM.SUPPORT_TILT), 4, "X") @ \
        Matrix.Rotation(rad(BM.SUPPORT_ROLL), 4, "Z")
    return {"arm.R.mode": "ik", "arm.R.sock": r, "arm.R.space": "chest", "arm.L.mode": "ik", "arm.L.sock": left,
            "arm.L.space": "chest", "arm.L.pole": G(0.6, -0.5, -0.6), "arm.R.pole": G(-0.6, -0.4, -0.7),
            "grip.R.fingers": 0.92, "grip.R.thumb": 0.55, "grip.L.fingers": 0.9, "grip.L.thumb": 0.5}


def low_ready():
    """Both hands on the pistol in front of the hips, muzzle down at the ground ahead."""
    r = sock_frame(G(-0.05, 0.99, 0.3), G(0.08, -0.75, 0.66), G(-0.1, 0.6, 0.6))
    left = r @ Matrix.Translation(BM.SUPPORT_OFFSET) @ Matrix.Rotation(rad(BM.SUPPORT_TILT), 4, "X") @ \
        Matrix.Rotation(rad(BM.SUPPORT_ROLL), 4, "Z")
    return {"arm.R.mode": "ik", "arm.R.sock": r, "arm.R.space": "chest", "arm.L.mode": "ik", "arm.L.sock": left,
            "arm.L.space": "chest", "arm.L.pole": G(0.6, -0.5, -0.8), "arm.R.pole": G(-0.6, -0.4, -0.8),
            "grip.R.fingers": 0.92, "grip.R.thumb": 0.55, "grip.L.fingers": 0.85, "grip.L.thumb": 0.5}


def P(base, d):
    out = dict(base)
    out.update(d)
    return out


# ------------------------------------------------------------------------------------------------------------
# Startled + Startled_Hold
# ------------------------------------------------------------------------------------------------------------

STARTLED_FRAMES = 24
HOLD_FRAMES = 48
STARTLED_EVENTS = {"flinch": 2, "foot_plant": 9, "weapon_up": 11, "hold": 20}


def startled_hold_pose():
    """Bladed, knees bent, weight on the back foot, leaning back; gun high in both hands; looking up."""
    p = P(stance(), {
        "hips.loc": G(0.0, -0.11, -0.14), "hips.yaw": -16.0, "hips.pitch": -4.0, "hips.roll": 0.0,
        "spine1.pitch": -7.0, "spine1.yaw": 8.0, "spine2.pitch": -12.0, "spine2.yaw": 8.0,
        "neck.pitch": -12.0, "head.pitch": -24.0, "head.yaw": 0.0,
        "clav.L.raise": 6.0, "clav.R.raise": 6.0,
        "foot.L.pos": foot_at("L", fwd=0.02, side=0.1), "foot.L.yaw": 12.0,
        "foot.R.pos": foot_at("R", fwd=-0.42, side=0.08), "foot.R.yaw": 28.0,
    })
    p.update(aim_at(elev=40.0, raise_=-0.08, back=-0.07, right=0.08))
    return p


def startled_keys():
    start = idle0()
    flinch = P(start, {
        "hips.loc": STANCE["hips.loc"] + G(0.0, -0.07, -0.05), "hips.pitch": 0.0,
        "spine1.pitch": -6.0, "spine2.pitch": -12.0, "neck.pitch": -10.0, "head.pitch": -14.0, "head.yaw": 0.0,
        "clav.L.raise": 16.0, "clav.R.raise": 16.0, "clav.L.fwd": 6.0, "clav.R.fwd": 6.0,
        "arm.L.swing": 22.0, "arm.L.out": -8.0, "arm.L.elbow": 50.0, "grip.L.fingers": 0.2, "grip.L.thumb": 0.1,
        "arm.R.swing": 15.0, "arm.R.out": -18.0, "arm.R.elbow": 60.0,
    })
    lift = P(flinch, {
        "hips.loc": G(0.0, -0.065, -0.08), "hips.yaw": -13.0,
        "foot.R.pos": foot_at("R", fwd=-0.27, side=0.05, up=0.07), "foot.R.pitch": 8.0, "foot.R.yaw": 22.0,
    })
    lift.update(aim_at(elev=-10.0, raise_=-0.22, back=0.06, right=0.06))
    hold = startled_hold_pose()
    plant = P(hold, {"hips.loc": hold["hips.loc"] + G(0, -0.025, -0.02), "spine2.pitch": -15.0, "head.pitch": -28.0})
    plant.update(aim_at(elev=46.0, raise_=-0.06, back=-0.08, right=0.08))
    settle = P(hold, {"hips.loc": hold["hips.loc"] + G(0, 0.008, 0), "spine2.pitch": -11.0, "head.pitch": -22.0})
    settle.update(aim_at(elev=37.0, raise_=-0.09, back=-0.07, right=0.08))
    return [(0, start, "ease"), (3, flinch, "snap"), (6, lift, "ease"), (11, plant, "snap"), (15, settle, "ease"),
            (20, hold, "ease"), (STARTLED_FRAMES, hold, "linear")]


def hold_extra(f, P):
    """Startled_Hold: two quick shallow breaths per loop, a tremble on the muzzle, a slow look along the huge thing."""
    w = 2 * math.pi * f / HOLD_FRAMES
    breath = math.sin(2 * w)
    P["spine2.pitch"] -= 1.6 * breath
    P["clav.L.raise"] += 2.2 * breath
    P["clav.R.raise"] += 2.2 * breath
    P["head.yaw"] += 7.0 * math.sin(w)
    P["head.pitch"] += 2.5 * math.sin(2 * w)
    P["neck.yaw"] = P.get("neck.yaw", 0.0) + 3.0 * math.sin(w)
    tremble = 0.9 * math.sin(6 * w) + 0.5 * math.sin(10 * w)
    for name in SIDE:
        nudge_socket(P, name, G(0.0, 0.004 * breath, 0.0), pitch=tremble)


def hold_clip(poser):
    hold = startled_hold_pose()
    clip = KeyClip(poser, [(0, hold, "linear"), (HOLD_FRAMES, hold, "linear")], extra=hold_extra)
    return clip.make(HOLD_FRAMES, loop=True)(poser)


# ------------------------------------------------------------------------------------------------------------
# Disbelief
# ------------------------------------------------------------------------------------------------------------

DISBELIEF_FRAMES = 84
DISBELIEF_EVENTS = {"hand_up": 22, "shake_start": 26, "shake_left": 38, "shake_right": 52, "shake_end": 62}


def jaw_hand(drop=0.0):
    """The left hand in front of the chin guard, just below the visor: fingers up and back towards the jaw, palm in."""
    return {"arm.L.mode": "ik", "arm.L.space": "chest",
            "arm.L.sock": sock_frame(G(0.06, 1.28 - drop, 0.25), G(-0.2, 0.8, -0.35), G(1.0, 0.0, -0.1)),
            "arm.L.pole": G(0.7, -0.7, 0.1), "grip.L.fingers": 0.35, "grip.L.thumb": 0.3}


def disbelief_keys():
    start = idle0()
    lean = {"hips.loc": STANCE["hips.loc"] + G(0.0, -0.01, -0.06), "hips.pitch": -1.0,
            "spine1.pitch": -4.0, "spine2.pitch": -9.0, "neck.pitch": -3.0, "head.pitch": -7.0, "head.yaw": 0.0,
            "foot.L.pos": foot_at("L", fwd=0.03, side=0.03),
            "arm.R.swing": 8.0, "arm.R.out": -16.0, "arm.R.elbow": 30.0, "arm.R.wrist": 12.0,
            "clav.R.raise": -3.0}
    rising = P(start, lean)
    rising.update({"spine2.pitch": -6.0, "arm.L.mode": "ik", "arm.L.space": "chest",
                   "arm.L.sock": sock_frame(G(0.2, 1.04, 0.2), G(-0.15, 0.8, 0.5), G(0.3, 0.2, -1.0)),
                   "arm.L.pole": G(0.8, -0.6, -0.1), "grip.L.fingers": 0.4})
    up = P(start, lean)
    up.update(jaw_hand())
    up["clav.L.raise"] = 4.0
    shaken = P(up, {"spine2.pitch": -10.0, "head.pitch": -9.0})
    left = P(shaken, {"head.yaw": 20.0, "neck.yaw": 8.0})
    right = P(shaken, {"head.yaw": -20.0, "neck.yaw": -8.0})
    settle = P(up, {"head.pitch": -4.0, "spine2.pitch": -8.0})
    settle.update(jaw_hand(drop=0.025))
    return [(0, start, "ease"), (12, rising, "ease"), (22, up, "ease"), (26, shaken, "ease"), (38, left, "ease"),
            (52, right, "ease"), (62, shaken, "ease"), (74, settle, "ease"), (DISBELIEF_FRAMES, settle, "linear")]


# ------------------------------------------------------------------------------------------------------------
# Point_Shout
# ------------------------------------------------------------------------------------------------------------

POINT_FRAMES = 42
POINT_EVENTS = {"point": 9, "shout": 9, "jab": 18}


def point_keys():
    start = idle0()
    legs = {k: v for k, v in start.items() if k.startswith(("hips.", "foot."))}
    wind = P(start, {"spine1.pitch": -1.0, "spine2.pitch": -4.0, "spine2.yaw": -2.0, "head.pitch": -2.0,
                     "clav.L.fwd": -8.0, "arm.L.swing": 45.0, "arm.L.out": -4.0, "arm.L.elbow": 110.0,
                     "arm.L.twist": 30.0, "grip.L.fingers": 0.6})
    point = P(start, {"spine1.pitch": 7.0, "spine2.pitch": 12.0, "spine1.yaw": 11.0, "spine2.yaw": 12.0,
                      "neck.pitch": -10.0, "head.pitch": -10.0, "head.yaw": -6.0,
                      "clav.L.fwd": 14.0, "clav.L.raise": 6.0, "clav.R.fwd": -4.0,
                      "arm.L.swing": 104.0, "arm.L.out": 22.0, "arm.L.elbow": 4.0, "arm.L.twist": 0.0, "arm.L.wrist": -6.0,
                      "grip.L.fingers": 0.0, "grip.L.thumb": 0.05,
                      "arm.R.swing": -6.0, "arm.R.out": -24.0, "arm.R.elbow": 48.0, "arm.R.wrist": 10.0})
    recoil = P(point, {"spine2.pitch": 8.0, "arm.L.elbow": 16.0, "arm.L.swing": 98.0, "clav.L.fwd": 8.0, "head.pitch": -6.0})
    jab = P(point, {"spine2.pitch": 14.0, "spine1.pitch": 8.0, "arm.L.swing": 106.0, "clav.L.fwd": 16.0, "head.pitch": -12.0})
    hold = P(point, {"spine2.pitch": 9.0, "spine1.pitch": 5.0, "arm.L.swing": 100.0, "clav.L.fwd": 10.0, "head.pitch": -7.0})
    for p in (wind, point, recoil, jab, hold):
        p.update(legs)
    return [(0, start, "ease"), (5, wind, "ease"), (9, point, "snap"), (14, recoil, "ease"), (18, jab, "snap"),
            (28, hold, "ease"), (POINT_FRAMES, start, "ease")]


# ------------------------------------------------------------------------------------------------------------
# Lower_Weapon
# ------------------------------------------------------------------------------------------------------------

LOWER_FRAMES = 70
LOWER_EVENTS = {"lowered": 22, "release": 30, "breath_in": 40, "relief": 48}


def lower_keys():
    aim = dict(BM.AIM_STANCE)
    aim.update(aim_at())
    beat = P(aim, {"head.pitch": 5.0, "spine2.pitch": 1.5})
    lowered = P(aim, {"hips.yaw": -11.0, "spine1.yaw": 5.0, "spine2.yaw": 5.0, "head.pitch": 13.0, "neck.pitch": 6.0,
                      "spine2.pitch": 4.0})
    lowered.update(low_ready())
    rest_arms = {"arm.L.mode": "fk", "arm.R.mode": "fk",
                 "arm.L.swing": 8.0, "arm.L.out": -18.0, "arm.L.elbow": 22.0, "arm.L.twist": 10.0, "arm.L.wrist": 6.0,
                 "arm.R.swing": 6.0, "arm.R.out": -17.0, "arm.R.elbow": 26.0, "arm.R.twist": 10.0, "arm.R.wrist": 14.0,
                 "grip.L.fingers": 0.45, "grip.L.thumb": 0.3, "grip.R.fingers": 0.92, "grip.R.thumb": 0.55}
    release = P(stance(), {"head.pitch": 10.0, "neck.pitch": 4.0, "spine2.pitch": 3.0})
    release.update(rest_arms)
    breath = P(release, {"spine1.pitch": -2.0, "spine2.pitch": -7.0, "clav.L.raise": 8.0, "clav.R.raise": 8.0,
                         "head.pitch": -9.0, "neck.pitch": -3.0, "arm.L.out": -22.0, "arm.R.out": -21.0})
    relief = P(release, {"hips.loc": STANCE["hips.loc"] + G(0, -0.03, 0), "spine1.pitch": 5.0, "spine2.pitch": 10.0,
                         "clav.L.raise": -9.0, "clav.R.raise": -9.0, "clav.L.fwd": 4.0, "clav.R.fwd": 4.0,
                         "neck.pitch": 8.0, "head.pitch": 16.0, "arm.L.elbow": 12.0, "arm.R.elbow": 16.0,
                         "arm.L.out": -14.0, "arm.R.out": -14.0, "grip.L.fingers": 0.3})
    end = P(stance(), rest_arms)
    end.update({"clav.L.raise": -4.0, "clav.R.raise": -4.0, "spine2.pitch": 3.0, "head.pitch": 5.0})
    return [(0, aim, "ease"), (8, beat, "ease"), (22, lowered, "ease"), (30, release, "ease"), (40, breath, "ease"),
            (52, relief, "ease"), (LOWER_FRAMES, end, "ease")]


# ------------------------------------------------------------------------------------------------------------
# Clips, export, report
# ------------------------------------------------------------------------------------------------------------

CLIPS = {
    "Startled": (STARTLED_FRAMES, False, startled_keys, STARTLED_EVENTS, "full", "flinch"),
    "Startled_Hold": (HOLD_FRAMES, True, None, {}, "full", None),
    "Disbelief": (DISBELIEF_FRAMES, False, disbelief_keys, DISBELIEF_EVENTS, "full", "hand_up"),
    "Point_Shout": (POINT_FRAMES, False, point_keys, POINT_EVENTS, "upper (hips and legs stay in the stance)", "point"),
    "Lower_Weapon": (LOWER_FRAMES, False, lower_keys, LOWER_EVENTS, "full", "lowered"),
}
WEAPON = {"Startled": "pistol", "Startled_Hold": "pistol", "Disbelief": "pistol", "Point_Shout": "pistol",
          "Lower_Weapon": "pistol"}


def clip_fns():
    out = {}
    for name, (frames, loop, keys, _, _, _) in CLIPS.items():
        if keys is None:
            out[name] = hold_clip
        else:
            out[name] = (lambda k, n, lp: (lambda poser: KeyClip(poser, k()).make(n, lp)(poser)))(keys, frames, loop)
    return out


def report(path, actions):
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    rep["actions"] = {a.name: int(round(a.frame_range[1])) for a in actions}
    events = rep.setdefault("events", {})
    for name, (_, _, _, ev, _, primary) in CLIPS.items():
        if primary:
            events[name] = {primary: ev[primary]}
    rep["round35"] = {
        "clips": {name: {"frames": frames, "loop": loop, "layer": layer, "events": ev, "weapon": "pistol in Weapon_R"}
                  for name, (frames, loop, _, ev, layer, _) in CLIPS.items()},
        "chain": {"Startled": "ends in Startled_Hold frame 0 (play Startled, then loop Startled_Hold)",
                  "Lower_Weapon": "starts in Shoot frame 0 (the aim), ends near Idle frame 0 with the pistol in the "
                                  "right hand (holster it after, or snap it to Holster_R)",
                  "Disbelief": "starts at Idle frame 0, ends holding the hand at the jaw (clamp the last frame)",
                  "Point_Shout": "starts and ends at Idle frame 0"},
        "visor": "the left hand stays below and beside the visor in Disbelief; the visor is never covered",
    }
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.log("report", path)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm = bpy.data.objects[NAME + "_Rig"]
    mesh = bpy.data.objects[NAME]
    lod = bpy.data.objects[BM.LOD_NAME]
    for name in CLIPS:
        act = bpy.data.actions.get(name)
        if act is not None:
            bpy.data.actions.remove(act)
    poser = C.Poser(arm)
    new = C.key_clips(poser, clip_fns())
    actions = [a for a in bpy.data.actions]
    arm.animation_data.action = bpy.data.actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, mesh, lod], animated=True)
    report(os.path.join(C.EXPORTS, NAME, NAME + "_report.json"), actions)
    C.save_blend(NAME)
    if args.get("norender"):
        return
    import mercer_reaction_previews as RP
    RP.render(arm, mesh, lod, new, out, WEAPON, {n: c[3] for n, c in CLIPS.items()})


if __name__ == "__main__":
    main()
