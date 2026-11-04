"""Cole "Pathfinder" Mercer, the hero (round 6, see ArtSource/DESIGN_NOTES.md).

A human pathfinder in a padded navy expedition suit with white ceramic plates, a large rounded helmet with a smoked
wraparound visor, broad yellow-rimmed pauldrons, a gunmetal power pack with two cyan cells, thigh holsters and dusty
boots. One skinned mesh (LOD0 and LOD1 on the same 31-bone rig), one material, four 1024 maps (+ a 512 Low set).

Rig: Root, Hips, Spine1, Spine2, Neck, Head; per side Clavicle, UpperArm, Forearm, Hand, Fingers, Thumb, Thigh,
Shin, Foot, Toe; sockets (no weights) Weapon_L/R in the fists, Holster_L/R on the thighs, Back on the pack.
Clips: Idle, Walk, Run, Shoot, Reload, Slash, Shockwave, Repair, Hit, Down, Revive (in place, no root motion).
Upper-body mask for layering: Spine1 and everything under it.

Run: blender --background --python Tools/Blender/build_mercer.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Quaternion, Vector  # noqa: E402

import build_mercer_weapons as W  # noqa: E402
import common as C  # noqa: E402
import suit as S  # noqa: E402
from common import G  # noqa: E402

NAME = "Mercer"
LOD_NAME = "Mercer_LOD1"
GAME_TRIS = 9800
LOD_TRIS = 4000
TEX, LOW_TEX = 1024, 512
FAMILIES = ("weave", "ceramic", "metal")  # Mask B = weave (suit fabric), A = ceramic (plates)
GAME_SCALE = 1.0
SIDES = (("L", 1.0), ("R", -1.0))  # Blender +X is the figure's left (it faces -Y)

X = Vector((1, 0, 0))
Yb = Vector((0, 1, 0))
Z = Vector((0, 0, 1))
FWD = Vector((0, -1, 0))


def rad(d):
    return math.radians(d)


def nrm(v):
    return Vector(v).normalized()


# ------------------------------------------------------------------------------------------------------------
# Skeleton (game axes through G(x, up, forward); x positive = the figure's left)
# ------------------------------------------------------------------------------------------------------------

HIP_Y = 0.84
UPPER_ARM, FOREARM, HAND_LEN, FINGER_LEN = 0.265, 0.24, 0.085, 0.075
# Finger groups across the knuckles: (offset towards the thumb, radius, length); the ring and little finger overlap
# into one group.
FINGERS = ((0.03, 0.0105, 0.072), (0.005, 0.011, 0.078), (-0.019, 0.0102, 0.07), (-0.033, 0.0088, 0.058))
THIGH, SHIN = 0.38, 0.372
ANKLE_UP = 0.09


class Side:
    """Rest joints of one side, plus the hand frame (Y along the hand, Z the palm normal, T towards the thumb)."""

    def __init__(self, sx):
        self.sx = sx
        self.clav_head = G(0.03 * sx, 1.29, 0.0)
        self.clav_tail = G(0.165 * sx, 1.29, -0.01)
        self.shoulder = G(0.185 * sx, 1.275, -0.01)
        d_arm = nrm(G(0.5 * sx, -0.86, 0.04))
        elbow = self.shoulder + d_arm * UPPER_ARM
        self.wrist = elbow + nrm(G(0.36 * sx, -0.86, 0.24)) * FOREARM
        self.arm = C.Limb([f"UpperArm_{self.name}", f"Forearm_{self.name}"], self.shoulder, self.wrist, (UPPER_ARM, FOREARM),
                          G(0.35 * sx, -0.2, -1.0))
        self.elbow = self.arm.rest_points[1]
        d_fore = nrm(self.wrist - self.elbow)
        self.hand_y = d_fore
        palm = Vector((-sx, 0, 0))
        self.hand_z = nrm(palm - d_fore * palm.dot(d_fore))
        self.hand_x = self.hand_y.cross(self.hand_z)
        self.thumb = self.hand_x * -sx
        self.knuckle = self.wrist + d_fore * HAND_LEN
        curl = Quaternion(self.hand_x, rad(22))
        self.finger_dir = curl @ d_fore
        self.finger_tip = self.knuckle + self.finger_dir * FINGER_LEN
        self.thumb_head = self.wrist + self.thumb * 0.026 + d_fore * 0.022 + self.hand_z * 0.012
        self.thumb_dir = nrm(self.thumb * 0.55 + d_fore * 0.65 + self.hand_z * 0.35)
        self.thumb_tip = self.thumb_head + self.thumb_dir * 0.058
        # Weapon socket: the centre of the closed fist; Y towards the thumb (the pistol's top, the blade), tilted 15
        # degrees forward; Z along the hand (the barrel).
        t = W.GRIP_TILT
        self.sock = self.wrist + d_fore * 0.05 + self.hand_z * 0.022
        self.sock_y = nrm(self.thumb * math.cos(t) - d_fore * math.sin(t))
        self.sock_z = nrm(self.thumb * math.sin(t) + d_fore * math.cos(t))
        # Legs.
        self.hip = G(0.095 * sx, HIP_Y, 0.0)
        self.ankle = G(0.105 * sx, ANKLE_UP, -0.012)
        self.leg = C.Limb([f"Thigh_{self.name}", f"Shin_{self.name}"], self.hip, self.ankle, (THIGH, SHIN), G(0, 0, 1))
        self.knee = self.leg.rest_points[1]
        self.ankle = self.leg.rest_points[2]
        self.ball = G(0.108 * sx, 0.032, 0.105)
        self.toe = G(0.11 * sx, 0.03, 0.172)
        # Holster on the outer thigh: barrel down along the thigh, top forwards.
        down = nrm(self.knee - self.hip)
        self.holster = self.hip + down * 0.13 + G(0.088 * sx, 0, 0.004)
        self.holster_z = down
        self.holster_y = nrm(FWD - down * FWD.dot(down))

    @property
    def name(self):
        return "L" if self.sx > 0 else "R"


SIDE = {name: Side(sx) for name, sx in SIDES}
# The sword hilt on the pack: right-hand side, emitter up and tilted in over the shoulder, edge backwards.
BACK_SOCKET = G(-0.165, 1.17, -0.2)
BACK_Y = nrm(G(0.22, 0.97, 0.0))
BACK_Z = nrm(G(0, 0, -1))

SPINE = {"Hips": (G(0, 0.9, 0), G(0, 0.99, 0)), "Spine1": (G(0, 0.99, 0), G(0, 1.13, 0.0)),
         "Spine2": (G(0, 1.13, 0), G(0, 1.31, -0.01)), "Neck": (G(0, 1.31, -0.005), G(0, 1.39, 0.0)),
         "Head": (G(0, 1.39, 0), G(0, 1.55, 0.0))}


def bones():
    """(name, head, tail, parent, deform, roll vector or None). Sockets deform (so the FBX keeps them) but carry no
    weights."""
    out = [("Root", G(0, 0, 0), G(0, 0, 0.2), None, True, None)]
    parents = {"Hips": "Root", "Spine1": "Hips", "Spine2": "Spine1", "Neck": "Spine2", "Head": "Neck"}
    for n, (h, t) in SPINE.items():
        out.append((n, h, t, parents[n], True, FWD))
    for name, sd in SIDE.items():
        out += [(f"Clavicle_{name}", sd.clav_head, sd.clav_tail, "Spine2", True, Z),
                (f"UpperArm_{name}", sd.shoulder, sd.elbow, f"Clavicle_{name}", True, None),
                (f"Forearm_{name}", sd.elbow, sd.wrist, f"UpperArm_{name}", True, None),
                (f"Hand_{name}", sd.wrist, sd.knuckle, f"Forearm_{name}", True, sd.hand_z),
                (f"Fingers_{name}", sd.knuckle, sd.finger_tip, f"Hand_{name}", True, sd.hand_z),
                (f"Thumb_{name}", sd.thumb_head, sd.thumb_tip, f"Hand_{name}", True, sd.hand_z),
                (f"Weapon_{name}", sd.sock, sd.sock + sd.sock_y * 0.06, f"Hand_{name}", True, sd.sock_z),
                (f"Thigh_{name}", sd.hip, sd.knee, "Hips", True, None),
                (f"Shin_{name}", sd.knee, sd.ankle, f"Thigh_{name}", True, None),
                (f"Foot_{name}", sd.ankle, sd.ball, f"Shin_{name}", True, Z),
                (f"Toe_{name}", sd.ball, sd.toe, f"Foot_{name}", True, Z),
                (f"Holster_{name}", sd.holster, sd.holster + sd.holster_y * 0.06, f"Thigh_{name}", True, sd.holster_z)]
    out.append(("Back", BACK_SOCKET, BACK_SOCKET + BACK_Y * 0.06, "Spine2", True, BACK_Z))
    return out


SOCKETS = {"Weapon_L", "Weapon_R", "Holster_L", "Holster_R", "Back"}


def build_armature(name, bone_list):
    data = bpy.data.armatures.new(name)
    arm = C.link(bpy.data.objects.new(name, data))
    C.activate(arm)
    bpy.ops.object.mode_set(mode="EDIT")
    for bname, head, tail, parent, deform, roll in bone_list:
        eb = data.edit_bones.new(bname)
        eb.head = head
        eb.tail = tail
        eb.use_deform = deform
        eb.roll = 0.0
        if roll is not None:
            eb.align_roll(roll)
        if parent:
            eb.parent = data.edit_bones[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return arm


# ------------------------------------------------------------------------------------------------------------
# Parts
# ------------------------------------------------------------------------------------------------------------

def boxy(e=0.3):
    return W.boxy(e)


def frame(y_axis, z_hint):
    return W.frame_rot(y_axis, z_hint)


def euler(pitch=0.0, roll=0.0, yaw=0.0):
    return (Matrix.Rotation(rad(yaw), 3, "Z") @ Matrix.Rotation(rad(roll), 3, "Y") @ Matrix.Rotation(rad(pitch), 3, "X"))


CYAN = dict(emit=S.PAL.cyan, glow=1.0, rough=0.3, family={})
YELLOW = dict(paint=S.PAL.yellow, wear=0.5, rough=0.5)
GROOVE = dict(groove=0.0012)
SPACING = 1.0 / S.STITCH_FREQ  # quilt band spacing (5 cm)


def suit_body(features=()):
    return S.styled("suit", features, quilt=("height", SPACING))


def plate(features=(), seed=0):
    return S.styled("plate", features, seed=seed)


def metal(features=(), seed=0):
    return S.styled("metal", features, seed=seed)


def rubber(features=()):
    return S.styled("rubber", features)


def R(x, up, fwd):
    """Ellipsoid radii in game terms (across, up, forward) to Blender's (x, y, z)."""
    return (x, fwd, up)


LIMB_QUILT = 0.065


def limb_suit(quilt=True):
    return S.styled("suit", (), quilt=("tube", LIMB_QUILT) if quilt else None)


# --- Round 16: hard plates are separate shells (own remesh layer; suit.dome_patch / suit.limb_shell) -------------

dome_patch = S.dome_patch
limb_shell = S.limb_shell


def along(c):
    """Position 0..1 along a limb shell's limb (for grooves and stripes across the plate)."""
    a, d, length, _, _ = c.part.limb
    return ((c.p - np.array(a)) @ np.array(d)) / length


def around(c):
    """Angle (degrees) round a limb shell's limb, 0 at the plate's centre line."""
    a, d, _, ref, side = c.part.limb
    rel = c.p - np.array(a)
    return np.degrees(np.arctan2(rel @ np.array(side), rel @ np.array(ref)))


def build_parts():
    b = C.PartBuilder()
    # --- Torso -------------------------------------------------------------------------------------------
    b.ellipsoid("Pelvis", G(0, 0.9, 0.0), R(0.15, 0.105, 0.112), bone="Hips", style=suit_body(), segments=(36, 18))
    b.ellipsoid("Abdomen", G(0, 1.045, 0.006), R(0.134, 0.1, 0.106), bone="Spine1", style=suit_body(), segments=(36, 18))
    b.parts[-1].chain = ["Spine1", "Hips"]
    b.ellipsoid("Chest", G(0, 1.19, -0.006), R(0.172, 0.13, 0.122), bone="Spine2", style=suit_body(), segments=(40, 20))
    b.parts[-1].chain = ["Spine2", "Spine1"]
    # The cuirass: a separate ceramic shell over the chest, narrower at the bottom, with a bevelled rim.
    chest_c, chest_r = G(0, 1.19, -0.006), R(0.172, 0.13, 0.122)
    dome_patch(b, "ChestPlate", chest_c, chest_r, euler(pitch=-6), lambda a, v: (a * 1.95, -1.0, v * 0.95 + 0.18), 0.016, "Spine2",
               plate([(lambda c: S.line(c.u[:, 2], -0.3, 0.022) * (c.u[:, 1] < -0.3), GROOVE),
                      (lambda c: S.line(np.abs(c.u[:, 0]), 0.5, 0.02) * (c.u[:, 2] > -0.3) * (c.u[:, 1] < -0.3), GROOVE),
                      (lambda c: S.band(c.u[:, 0], 0.0, 0.11) * S.band(c.u[:, 2], 0.1, 0.06) * (c.u[:, 1] < -0.5), CYAN),
                      (lambda c: S.band(np.abs(c.u[:, 0]), 0.42, 0.05) * S.band(c.u[:, 2], 0.36, 0.045) * (c.u[:, 1] < -0.5),
                       dict(emit=S.PAL.yellow, glow=0.8, rough=0.3, family={}))], seed=1),
               outline=lambda a, v: (a * (0.72 + 0.28 * (v + 1) / 2), v), nu=28, nv=16)
    b.ellipsoid("Belt", G(0, 0.96, 0.0), R(0.156, 0.03, 0.118), bone="Hips", style=metal([(lambda c: S.line(c.u[:, 2], 0.0, 0.06), GROOVE)]),
                segments=(36, 12), deform=boxy(0.6))
    b.ellipsoid("Buckle", G(0, 0.96, 0.116), R(0.038, 0.027, 0.014), bone="Hips",
                style=plate([(lambda c: S.band(c.u[:, 2], 0.0, 0.35), YELLOW)]), deform=boxy(0.35), segments=(16, 8))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.ellipsoid("Pouch", G(0.1 * sx, 0.935, 0.098), R(0.036, 0.046, 0.026), bone="Hips",
                    style=metal([(lambda c: S.band(c.u[:, 2], 0.55, 0.12), GROOVE)], seed=2), deform=boxy(0.3), segments=(16, 10))
        b.ellipsoid("Pouch", G(0.118 * sx, 0.935, -0.092), R(0.044, 0.044, 0.03), euler(yaw=28 * sx), bone="Hips",
                    style=metal(seed=3), deform=boxy(0.3), segments=(16, 10))
    # Collar ring the helmet sits in, lit along its upper rim.
    b.ring("Collar", G(0, 1.312, -0.004), Z, 0.086, 0.027, arcs=3, sides=10, chain=["Spine2"],
           style=metal([(lambda c: C.smoothstep(0.5, 0.9, c.n[:, 2]) * S.band(c.p[:, 2], 1.336, 0.008), CYAN)]))
    b.tube("Neck", [G(0, 1.29, -0.004), G(0, 1.35, 0.0), G(0, 1.405, 0.004)], [0.062, 0.058, 0.06], sides=14,
           chain=["Neck"], style=rubber([(lambda c: (np.abs(np.sin(c.t * math.pi * 5)) < 0.2).astype(float), dict(groove=0.002))]))
    # --- Helmet: a ceramic shell with a yellow crown stripe and a smoked visor band from ear to ear ----------
    b.ellipsoid("Helmet", G(0, 1.455, -0.008), R(0.126, 0.131, 0.134), bone="Head",
                style=plate([(lambda c: S.band(c.u[:, 0], 0.0, 0.15) * C.smoothstep(0.3, 0.55, c.u[:, 2]), YELLOW),
                             (lambda c: S.line(np.abs(c.u[:, 0]), 0.19, 0.022) * C.smoothstep(0.35, 0.6, c.u[:, 2]), GROOVE),
                             (lambda c: S.line(c.u[:, 2], -0.5, 0.03) * C.smoothstep(-0.3, 0.3, c.u[:, 1]), GROOVE)], seed=4),
                segments=(48, 24))

    def visor_hud(c):
        return S.band(c.u[:, 2], -0.62, 0.07) * C.smoothstep(0.2, 0.6, -c.u[:, 1])
    b.ellipsoid("Visor", G(0, 1.448, 0.03), R(0.125, 0.068, 0.12), bone="Head", style=S.glass(visor_hud, glow=0.24),
                segments=(48, 24))
    b.ellipsoid("Chin", G(0, 1.378, 0.066), R(0.066, 0.03, 0.05), euler(pitch=12), bone="Head",
                style=metal([(lambda c: S.line(c.u[:, 0], 0.0, 0.05), GROOVE)], seed=5), deform=boxy(0.6), segments=(24, 12))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Ear", [G(0.108 * sx, 1.444, -0.012), G(0.144 * sx, 1.444, -0.012)], [0.04, 0.035], sides=16, chain=["Head"],
               style=metal([(lambda c: S.band(c.t, 0.95, 0.1), GROOVE)], seed=6), cap=0.35)
        b.ellipsoid("Lamp", G(0.135 * sx, 1.47, 0.03), R(0.011, 0.011, 0.011), bone="Head", style=S.light(strength=1.3),
                    segments=(12, 8))
    # --- Pack: a gunmetal power pack with two cyan cells in its flanks ------------------------------------
    b.ellipsoid("Pack", G(0, 1.15, -0.19), R(0.15, 0.175, 0.085), bone="Spine2",
                style=metal([(lambda c: S.line(c.u[:, 2], 0.25, 0.025) + S.line(c.u[:, 2], -0.4, 0.025), GROOVE),
                             (lambda c: S.band(c.u[:, 2], 0.0, 0.08) * (c.u[:, 1] > 0.6) * S.band(c.u[:, 0], 0.0, 0.5), YELLOW)], seed=7),
                deform=boxy(0.3), segments=(36, 18))
    b.ellipsoid("PackLid", G(0, 1.33, -0.18), R(0.125, 0.026, 0.074), bone="Spine2", style=metal(seed=8), deform=boxy(0.4),
                segments=(24, 10))
    b.ellipsoid("Vent", G(0, 0.98, -0.19), R(0.12, 0.028, 0.07), bone="Spine2",
                style=metal([(lambda c: (np.abs(np.sin(c.u[:, 0] * 14)) < 0.35) * (c.u[:, 1] > 0.3), dict(groove=0.003))], seed=9),
                deform=boxy(0.4), segments=(28, 10))
    for name, sd in SIDE.items():
        sx = sd.sx
        b.tube("Cell", [G(0.098 * sx, 1.03, -0.262), G(0.098 * sx, 1.15, -0.268), G(0.098 * sx, 1.27, -0.262)], [0.034, 0.035, 0.034],
               sides=14, chain=["Spine2"], style=S.light(strength=1.0, bands=14, band_dark=0.75), cap=0.4)
        for y in (1.025, 1.275):
            b.ellipsoid("CellCap", G(0.098 * sx, y, -0.262), R(0.04, 0.016, 0.04), bone="Spine2", style=metal(seed=10),
                        segments=(16, 8))
        b.tube("Antenna", [G(0.125 * sx, 1.335, -0.22), G(0.13 * sx, 1.41, -0.232), G(0.134 * sx, 1.45, -0.238)],
               [0.011, 0.009, 0.008], sides=8, chain=["Spine2"], style=metal([(lambda c: S.band(c.t, 1.0, 0.15),
                                                                                dict(emit=S.PAL.yellow, glow=0.9, rough=0.3, family={}))]))
        b.ellipsoid("HiltClip", G(0.158 * sx, 1.17, -0.2), R(0.018, 0.05, 0.028), bone="Spine2", style=metal(seed=11),
                    deform=boxy(0.35), segments=(12, 8))
    # --- Shoulders and arms -----------------------------------------------------------------------------
    for name, sd in SIDE.items():
        sx = sd.sx
        b.ellipsoid("Yoke", G(0.168 * sx, 1.25, -0.006), R(0.084, 0.07, 0.086), bone=f"Clavicle_{name}", style=suit_body(),
                    segments=(24, 12))
        b.parts[-1].chain = [f"Clavicle_{name}", "Spine2"]
        # Pauldron: a broad shell sloping down and out over the shoulder, a narrower lame under its outer edge whose
        # lower rim is striped yellow. Both are separate hard shells (bevelled rims) on the padded yoke.
        dome_patch(b, "Pauldron", G(0.2 * sx, 1.215, -0.008), R(0.13, 0.112, 0.132), euler(roll=22 * sx),
                   lambda a, v, sx=sx: (a * 0.98 * sx, v * 0.95, 1.0), 0.018, None,
                   plate([(lambda c, sx=sx: S.line(c.u[:, 0] * sx, 0.42, 0.03), GROOVE)], seed=12),
                   chain=[f"Clavicle_{name}", f"UpperArm_{name}"])
        dome_patch(b, "Lame", G(0.235 * sx, 1.215, -0.008), R(0.08, 0.075, 0.104), euler(roll=62 * sx),
                   lambda a, v, sx=sx: (a * 0.95 * sx, v * 1.1, 1.0), 0.012, None,
                   plate([(lambda c, sx=sx: C.smoothstep(0.38, 0.52, c.u[:, 0] * sx), YELLOW)], seed=24),
                   chain=[f"UpperArm_{name}", f"Clavicle_{name}"], nu=16, nv=12)
        s_, e_, w_ = sd.shoulder, sd.elbow, sd.wrist
        b.tube("UpperArm", [s_, s_.lerp(e_, 0.5), e_], [0.062, 0.057, 0.05], sides=16, chain=[f"UpperArm_{name}"],
               style=S.styled("suit", (), quilt=("tube", LIMB_QUILT)))
        b.ellipsoid("ElbowPad", e_ + G(0, 0.0, -0.024), R(0.046, 0.046, 0.042), bone=f"UpperArm_{name}", style=rubber(), segments=(16, 8))
        b.parts[-1].chain = [f"UpperArm_{name}", f"Forearm_{name}"]
        out = nrm(-sd.hand_z)  # back of the forearm (away from the palm)
        b.tube("Forearm", [e_, e_.lerp(w_, 0.5), w_], [0.056, 0.053, 0.046], sides=16, chain=[f"Forearm_{name}"], style=limb_suit(False))
        limb_shell(b, "ForearmPlate", e_, w_, 0.057, 0.048, out, 92, 0.22, 0.95, 0.011, [f"Forearm_{name}"],
                   plate([(lambda c: (np.abs(around(c)) < 12) * S.band(along(c), 0.6, 0.24), CYAN),
                          (lambda c: S.line(along(c), 0.3, 0.012), GROOVE)], seed=13))
        b.ellipsoid("Emitter", w_ + sd.hand_y * -0.02 + out * 0.056, R(0.014, 0.01, 0.014), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=S.light(strength=1.2), segments=(12, 8))
        b.ellipsoid("Cuff", w_ + sd.hand_y * 0.006, (0.046, 0.02, 0.046), frame(sd.hand_y, out), bone=f"Forearm_{name}",
                    style=metal(seed=14), segments=(16, 8))
        b.parts[-1].chain = [f"Forearm_{name}", f"Hand_{name}"]
        # Gloves: palm block, a back-of-hand plate, finger groups and the thumb.
        pc = sd.wrist + sd.hand_y * 0.046
        b.ellipsoid("Palm", pc, (0.045, 0.052, 0.029), frame(sd.hand_y, sd.hand_z), bone=f"Hand_{name}",
                    style=rubber([(lambda c: C.smoothstep(0.3, 0.7, -c.u[:, 2]), dict(family=dict(metal=1.0)))]), deform=boxy(0.75),
                    segments=(20, 10))
        b.ellipsoid("Knuckles", pc + sd.hand_y * 0.014 - sd.hand_z * 0.024, (0.04, 0.03, 0.013), frame(sd.hand_y, -sd.hand_z),
                    bone=f"Hand_{name}", style=metal(seed=15), deform=boxy(0.5), segments=(16, 8))
        # Three finger groups (index, middle, ring and little finger together) with real gaps between them, all on
        # the one Fingers bone: a glove, not a mitten.
        k_ = sd.knuckle
        for off, r0, length in FINGERS:
            o = sd.thumb * off
            base = k_ - sd.hand_y * 0.014 + o + sd.hand_z * 0.002
            tip = k_ + sd.finger_dir * length + o * 1.08
            b.tube("Finger", [base, base.lerp(tip, 0.5) + sd.hand_z * 0.003, tip], [r0, r0 * 0.95, r0 * 0.8], sides=10,
                   chain=[f"Fingers_{name}"], style=rubber([(lambda c: S.band(c.t, 0.5, 0.05), dict(groove=0.0012))]), cap=0.8)
        b.tube("Thumb", [sd.thumb_head - sd.thumb_dir * 0.01, sd.thumb_head.lerp(sd.thumb_tip, 0.5), sd.thumb_tip], [0.02, 0.019, 0.015],
               sides=10, chain=[f"Thumb_{name}"], style=rubber())
    # --- Legs --------------------------------------------------------------------------------------------
    for name, sd in SIDE.items():
        sx = sd.sx
        h_, k_, a_ = sd.hip, sd.knee, sd.ankle
        down = nrm(k_ - h_)
        outer_front = nrm(FWD + Vector((sx * 0.55, 0, 0)))
        b.tube("Thigh", [h_ + G(0, 0.03, 0), h_.lerp(k_, 0.5), k_], [0.096, 0.086, 0.07], sides=18, chain=[f"Thigh_{name}"],
               style=limb_suit())
        limb_shell(b, "ThighPlate", h_, k_, 0.097, 0.072, outer_front, 60, 0.3, 0.86, 0.012, [f"Thigh_{name}"],
                   plate([(lambda c: S.line(along(c), 0.52, 0.01), GROOVE),
                          (lambda c: (np.abs(around(c)) < 9) * S.band(along(c), 0.58, 0.2), YELLOW)], seed=16))
        b.ellipsoid("Holster", sd.holster + down * 0.06 + G(0.004 * sx, 0, 0), (0.026, 0.078, 0.046), frame(-down, FWD), bone=f"Thigh_{name}",
                    style=metal([(lambda c: S.line(c.u[:, 1], 0.55, 0.05), GROOVE)], seed=17), deform=boxy(0.3), segments=(16, 10))
        dome_patch(b, "KneePad", k_ + G(0, 0.004, 0.012), R(0.062, 0.066, 0.058), None, lambda a, v: (a * 1.0, -1.0, v * 1.05), 0.013,
                   f"Shin_{name}", plate([(lambda c: S.line(c.u[:, 2], -0.05, 0.03) * (c.u[:, 1] < 0), GROOVE)], seed=19), nu=16, nv=14)
        b.tube("Shin", [k_, k_.lerp(a_, 0.5), a_ + G(0, 0.03, 0)], [0.066, 0.06, 0.052], sides=16, chain=[f"Shin_{name}"],
               style=limb_suit(False))
        limb_shell(b, "ShinPlate", k_, a_, 0.068, 0.055, FWD, 70, 0.12, 0.8, 0.012, [f"Shin_{name}"],
                   plate([(lambda c: S.line(along(c), 0.55, 0.01), GROOVE)], seed=20))
        # Boot: a cuff, a gunmetal shell from heel to toe, a thick rubber sole and a yellow heel tab.
        b.ellipsoid("BootCuff", a_ + G(0, 0.035, 0.0), R(0.066, 0.05, 0.07), bone=f"Shin_{name}", style=metal(seed=21), deform=boxy(0.7),
                    segments=(20, 10))
        b.parts[-1].chain = [f"Shin_{name}", f"Foot_{name}"]
        heel = G(0.105 * sx, 0.062, -0.06)
        b.tube("Boot", [heel, a_ + G(0, -0.025, 0.03), sd.ball + G(0, 0.024, 0), sd.toe + G(0, 0.014, -0.012)], [0.058, 0.062, 0.054, 0.044],
               sides=16, chain=[f"Foot_{name}", f"Toe_{name}"], style=metal([(lambda c: S.band(c.t, 0.0, 0.12) * C.smoothstep(-0.2, 0.3, c.n[:, 1]), YELLOW)],
                                                                            seed=22), flatten=0.8)
        b.ellipsoid("Sole", G(0.107 * sx, 0.018, 0.058), R(0.064, 0.02, 0.142), bone=f"Foot_{name}", style=rubber(), deform=boxy(0.6),
                    segments=(24, 10))
        b.parts[-1].chain = [f"Foot_{name}", f"Toe_{name}"]
    return b


RIGID = {"Helmet", "Visor", "Chin", "Ear", "Lamp", "ChestPlate", "Pack", "PackLid", "Vent", "Cell", "CellCap", "Antenna",
         "HiltClip", "Collar", "Belt", "Buckle", "Pouch", "Emitter", "Holster", "Knuckles", "KneePad", "ForearmPlate", "ShinPlate",
         "ThighPlate", "Finger"}

# Round 16 remesh layers (suit.build_layered_asset): the padded suit stays soft; plates, gear and gloves are fused
# on their own, finer and with less smoothing, so they keep bevelled edges, crisp grooves and finger gaps.
LAYERS = [S.Layer("suit", None, voxel=0.0034, smooth=6, density=0.75),
          S.Layer("plates", {"ChestPlate", "Pauldron", "Lame", "ForearmPlate", "ThighPlate", "ShinPlate", "KneePad", "Helmet"},
                  voxel=0.0019, smooth=2, density=1.0),
          S.Layer("gear", {"Belt", "Buckle", "Pouch", "Collar", "Visor", "Chin", "Ear", "Lamp", "Pack", "PackLid", "Vent", "Cell",
                           "CellCap", "Antenna", "HiltClip", "Emitter", "Cuff", "Holster", "BootCuff", "Boot", "Sole"},
                  voxel=0.0026, smooth=3, density=0.8),
          S.Layer("hands", {"Palm", "Knuckles", "Finger", "Thumb"}, voxel=0.0015, smooth=3, density=2.2)]


# ------------------------------------------------------------------------------------------------------------
# Posing: a frame is a flat dict of parameters (P); compose() turns it into Poser specs (FK trunk, IK limbs)
# ------------------------------------------------------------------------------------------------------------

def ypr(yaw=0.0, pitch=0.0, roll=0.0):
    """Hinge list: yaw about Z (+ turns left), pitch about X (+ bends forward), roll about Y (+ leans left)."""
    return [(Z, rad(yaw)), (X, rad(pitch)), (Yb, rad(roll))]


def sock_frame(origin, z_dir, y_hint):
    """A socket pose in armature space: Z along `z_dir` (barrel / edge), Y towards `y_hint` (top / blade)."""
    z = nrm(z_dir)
    y = nrm(Vector(y_hint) - z * Vector(y_hint).dot(z))
    x = y.cross(z)
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = origin
    return m


def blade_frame(origin, blade_dir, edge_hint):
    """A socket pose for the sword: Y along the blade, Z (the edge) towards `edge_hint`."""
    y = nrm(blade_dir)
    z = nrm(Vector(edge_hint) - y * Vector(edge_hint).dot(y))
    x = y.cross(z)
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = origin
    return m


def lerp_m(a, b, t):
    la, ra, _ = a.decompose()
    lb, rb, _ = b.decompose()
    if ra.dot(rb) < 0:
        rb.negate()
    m = ra.slerp(rb, t).to_matrix().to_4x4()
    m.translation = la.lerp(lb, t)
    return m


def blend(a, b, t):
    """Interpolates two parameter dicts (numbers, Vectors, 4x4 Matrices); strings and missing keys come from a/b."""
    out = dict(a)
    for k, vb in b.items():
        va = a.get(k)
        if va is None or isinstance(vb, str):
            out[k] = vb if t >= 0.5 or va is None else va
        elif isinstance(vb, Matrix):
            out[k] = lerp_m(va, vb, t)
        elif isinstance(vb, Vector):
            out[k] = va.lerp(vb, t)
        else:
            out[k] = va + (vb - va) * t
    return out


def ease(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def keyed(keys, t, easing=ease):
    """Pose at time t from [(time, P), ...] (sorted), eased between neighbours."""
    if t <= keys[0][0]:
        return dict(keys[0][1])
    for (t0, p0), (t1, p1) in zip(keys, keys[1:]):
        if t <= t1:
            return blend(p0, p1, easing((t - t0) / max(t1 - t0, 1e-6)))
    return dict(keys[-1][1])


def compose(poser, P):
    rest = poser.rest
    specs = {"Root": {"rot": [(Z, rad(P.get("root.yaw", 0.0)))], "loc": P.get("root.loc", Vector())},
             "Hips": {"rot": ypr(P.get("hips.yaw", 0), P.get("hips.pitch", 0), P.get("hips.roll", 0)), "loc": P.get("hips.loc", Vector())}}
    for b in ("spine1", "spine2", "neck", "head"):
        specs[b.capitalize()] = {"rot": ypr(P.get(b + ".yaw", 0), P.get(b + ".pitch", 0), P.get(b + ".roll", 0))}
    for name, sd in SIDE.items():
        sx = sd.sx
        specs[f"Clavicle_{name}"] = {"rot": [(Z, -sx * rad(P.get(f"clav.{name}.fwd", 0))), (Yb, -sx * rad(P.get(f"clav.{name}.raise", 0)))]}
        if P.get(f"arm.{name}.mode", "fk") == "fk":
            hinge = nrm((sd.elbow - sd.shoulder).cross(FWD))
            specs[f"UpperArm_{name}"] = {"rot": [(X, -rad(P.get(f"arm.{name}.swing", 0))), (Yb, -sx * rad(P.get(f"arm.{name}.out", 0))),
                                                 (nrm(sd.elbow - sd.shoulder), sx * rad(P.get(f"arm.{name}.twist", 0)))]}
            specs[f"Forearm_{name}"] = {"rot": [(hinge, rad(P.get(f"arm.{name}.elbow", 0))),
                                                (sd.hand_y, sx * rad(P.get(f"arm.{name}.turn", 0)))]}
            specs[f"Hand_{name}"] = {"rot": [(sd.hand_x, rad(P.get(f"arm.{name}.wrist", 0))), (sd.hand_z, rad(P.get(f"arm.{name}.wrist_side", 0)))]}
        curl_f = P.get(f"grip.{name}.fingers", 0.4)
        curl_t = P.get(f"grip.{name}.thumb", 0.3)
        specs[f"Fingers_{name}"] = {"rot": [(rest[f"Fingers_{name}"].to_3x3().col[0], rad(95) * curl_f - rad(10))]}
        specs[f"Thumb_{name}"] = {"rot": [(rest[f"Thumb_{name}"].to_3x3().col[0], rad(55) * curl_t)]}
    pose = poser.solve(specs)
    chest_move = pose["Spine2"] @ rest["Spine2"].inverted()
    hips_move = pose["Hips"] @ rest["Hips"].inverted()
    # Arms by IK: the socket's target frame (in chest rest space, carried by the chest, or in armature space).
    for name, sd in SIDE.items():
        if P.get(f"arm.{name}.mode", "fk") != "ik":
            continue
        target = P[f"arm.{name}.sock"]
        if P.get(f"arm.{name}.space", "chest") == "chest":
            target = chest_move @ target
        sock_local = rest[f"Hand_{name}"].inverted() @ rest[f"Weapon_{name}"]
        hand = target @ sock_local.inverted()
        shoulder = pose[f"UpperArm_{name}"].translation
        pole = P.get(f"arm.{name}.pole", sd.arm.pole)
        pole = chest_move.to_3x3() @ Vector(pole)
        pts = sd.arm.points(shoulder, hand.translation, pole)
        specs.update(sd.arm.pose(poser, shoulder, hand.translation, pole))
        hand.translation = pts[2]
        specs[f"Hand_{name}"] = {"abs": hand}
    # Legs by IK: ankle targets in armature space, the foot aimed by pitch (toe up +) and yaw (toe out +).
    for name, sd in SIDE.items():
        hip = (hips_move @ rest[f"Thigh_{name}"]).translation
        ankle = P.get(f"foot.{name}.pos", sd.ankle)
        yaw = rad(P.get(f"foot.{name}.yaw", 0.0)) * sd.sx
        pole = P.get(f"foot.{name}.pole")
        if pole is None:
            hy = rad(P.get("hips.yaw", 0.0))
            pole = Matrix.Rotation(0.5 * (hy + yaw), 3, "Z") @ (FWD + X * (0.12 * sd.sx))
        pts = sd.leg.points(hip, ankle, pole)
        specs.update(sd.leg.pose(poser, hip, ankle, pole))
        fdir = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(-rad(P.get(f"foot.{name}.pitch", 0.0)), 3, "X") @ (sd.ball - sd.ankle)
        lateral = Matrix.Rotation(yaw, 3, "Z") @ X
        extra = P.get(f"foot.{name}.frame")
        if extra is not None:
            fdir, lateral = extra
        specs[f"Foot_{name}"] = {"abs": C.aim_matrix(pts[2], fdir, lateral, rest[f"Foot_{name}"], X)}
        specs[f"Toe_{name}"] = {"rot": [(X, -rad(P.get(f"foot.{name}.toe", 0.0)))]}
    return specs


def foot_at(name, fwd=0.0, side=0.0, up=0.0):
    """Rest ankle moved in game axes (forward, outward to that side, up)."""
    sd = SIDE[name]
    return sd.ankle + G(side * sd.sx, up, fwd)


# The stance every standing clip starts from: weight in it, right foot back and turned out, knees bent, a slight
# hunch, head forward, hips turned a little to the right with the chest turned back towards the front.
STANCE = {
    "hips.loc": G(0, -0.032, 0.0), "hips.yaw": -9.0, "hips.pitch": 2.0, "hips.roll": 1.0,
    "spine1.pitch": 1.0, "spine1.yaw": 4.0, "spine2.pitch": 0.0, "spine2.yaw": 4.0, "neck.pitch": 2.0, "head.pitch": 1.0,
    "head.yaw": 3.0,
    "clav.L.raise": 0.0, "clav.R.raise": 0.0,
    "arm.L.swing": 6.0, "arm.L.out": -21.0, "arm.L.twist": 10.0, "arm.L.elbow": 18.0, "arm.L.wrist": 8.0,
    "arm.R.swing": 2.0, "arm.R.out": -20.0, "arm.R.twist": 10.0, "arm.R.elbow": 22.0, "arm.R.wrist": 8.0,
    "grip.L.fingers": 0.5, "grip.L.thumb": 0.35, "grip.R.fingers": 0.55, "grip.R.thumb": 0.35,
    "foot.L.pos": foot_at("L", fwd=0.07, side=0.03), "foot.L.yaw": 8.0,
    "foot.R.pos": foot_at("R", fwd=-0.1, side=0.02), "foot.R.yaw": 20.0,
}


def stance(**over):
    p = dict(STANCE)
    p.update(over)
    return p


def clip(frames, fn, loop=False):
    def make(poser):
        def spec(t, f):
            return compose(poser, fn(t, f))
        return frames, spec, loop
    return make


# --- Idle ---------------------------------------------------------------------------------------------------

IDLE_FRAMES = 72


def idle_p(t, f):
    w = 2 * math.pi * t
    breath = math.sin(w)
    shift = math.sin(w + 0.6)
    scan = math.sin(w) * 0.7 + math.sin(2 * w + 1.1) * 0.3
    return stance(**{
        "hips.loc": STANCE["hips.loc"] + G(0.012 * shift, -0.004 * (1 + math.cos(2 * w)) * 0.5, 0),
        "hips.roll": 1.0 + 1.6 * shift, "spine1.roll": -0.9 * shift,
        "spine2.pitch": -1.4 * breath, "clav.L.raise": 1.6 * breath, "clav.R.raise": 1.6 * breath,
        "head.yaw": 3.0 + 13 * scan, "head.pitch": 1.0 + 2.5 * math.sin(2 * w + 0.3), "neck.yaw": 4 * scan,
        "arm.L.swing": 6 + 2.0 * breath, "arm.R.swing": 2 + 2.0 * math.sin(w + 1.3), "arm.L.elbow": 18 + 3 * breath,
    })


# --- Walk ---------------------------------------------------------------------------------------------------

WALK_FRAMES, WALK_STANCE, WALK_STRIDE, WALK_LIFT = 32, 0.62, 0.6, 0.085


def gait_foot(name, phase, stance_share, stride, lift, side=0.02, run=False):
    fwd, up = C.step_curve(phase, stance_share, stride, lift)
    pitch, toe = 0.0, 0.0
    if phase < stance_share:
        s = phase / stance_share
        pitch = 14 * (1 - ease(s / 0.18)) if s < 0.18 else 0.0
        if s > 0.62:
            roll = ease((s - 0.62) / 0.38)
            pitch = -26 * roll
            toe = 26 * roll
            up += 0.1 * math.sin(rad(26 * roll))
        if run:
            pitch *= 0.4
    else:
        s = (phase - stance_share) / (1 - stance_share)
        pitch = -26 * (1 - ease(s / 0.35)) + 12 * ease((s - 0.5) / 0.5)
        toe = 26 * (1 - ease(s / 0.3))
        if run:
            # The heel kicks up behind before the leg swings through.
            up += 0.3 * math.sin(math.pi * min(1.0, s / 0.75)) ** 2 * (1 - 0.6 * s)
            fwd -= 0.16 * math.sin(math.pi * min(1.0, s / 0.55)) * (1 - s)
    return {f"foot.{name}.pos": foot_at(name, fwd=fwd, side=side, up=up), f"foot.{name}.pitch": pitch, f"foot.{name}.toe": toe,
            f"foot.{name}.yaw": 5.0}


def walk_p(t, f):
    w = 2 * math.pi * t
    p = stance(**{
        "hips.loc": G(0, -0.03 - 0.016 * math.cos(2 * w), 0), "hips.yaw": 5 * math.cos(w), "hips.pitch": 4.0,
        "hips.roll": 2.5 * math.sin(w), "spine1.yaw": -4 * math.cos(w), "spine1.pitch": 3.0, "spine2.yaw": -3 * math.cos(w),
        "spine2.pitch": 4.0, "spine1.roll": -1.5 * math.sin(w), "head.yaw": 1.5 * math.cos(w), "neck.pitch": -2, "head.pitch": 2,
        "arm.L.swing": 4 - 20 * math.cos(w), "arm.R.swing": 4 + 20 * math.cos(w),
        "arm.L.elbow": 22 + 10 * max(0.0, -math.cos(w)), "arm.R.elbow": 22 + 10 * max(0.0, math.cos(w)),
        "arm.L.out": -20, "arm.R.out": -20,
    })
    p.update(gait_foot("L", t % 1.0, WALK_STANCE, WALK_STRIDE, WALK_LIFT))
    p.update(gait_foot("R", (t + 0.5) % 1.0, WALK_STANCE, WALK_STRIDE, WALK_LIFT))
    return p


# --- Run ----------------------------------------------------------------------------------------------------

RUN_FRAMES, RUN_STANCE, RUN_STRIDE, RUN_LIFT = 16, 0.34, 0.86, 0.17


def run_p(t, f):
    w = 2 * math.pi * t
    bob = -math.cos(2 * (w - 2 * math.pi * 0.17))
    p = stance(**{
        "hips.loc": G(0, -0.05 + 0.03 * bob, 0.0), "hips.yaw": 9 * math.cos(w), "hips.pitch": 11.0, "hips.roll": 2.0 * math.sin(w),
        "spine1.pitch": 10.0, "spine1.yaw": -8 * math.cos(w), "spine2.pitch": 3.0, "spine2.yaw": -7 * math.cos(w),
        "neck.pitch": -10.0, "head.pitch": -4.0, "head.yaw": 2 * math.cos(w),
        "arm.L.swing": 5 - 52 * math.cos(w), "arm.R.swing": 5 + 52 * math.cos(w), "arm.L.out": -17, "arm.R.out": -17,
        "arm.L.elbow": 88 + 12 * math.cos(w), "arm.R.elbow": 88 - 12 * math.cos(w), "arm.L.twist": 20, "arm.R.twist": 20,
        "arm.L.wrist": 0, "arm.R.wrist": 0,
        "grip.L.fingers": 0.85, "grip.R.fingers": 0.85, "grip.L.thumb": 0.6, "grip.R.thumb": 0.6,
    })
    p.update(gait_foot("L", t % 1.0, RUN_STANCE, RUN_STRIDE, RUN_LIFT, side=0.0, run=True))
    p.update(gait_foot("R", (t + 0.5) % 1.0, RUN_STANCE, RUN_STRIDE, RUN_LIFT, side=0.0, run=True))
    return p


# --- Weapon poses -------------------------------------------------------------------------------------------

AIM_STANCE = dict(stance(**{"hips.yaw": -14.0, "spine1.yaw": 7.0, "spine2.yaw": 7.0, "spine2.pitch": 0.0, "head.yaw": 0.0,
                            "neck.pitch": 2.0, "head.pitch": 1.0, "hips.pitch": 1.0, "spine1.pitch": 1.0}))
AIM_GRIP = G(-0.06, 1.275, 0.465)
AIM_DIR = G(0.02, 0.0, 1.0)
UP = Z


SUPPORT_OFFSET = Vector((0.052, -0.026, 0.012))
SUPPORT_TILT = 18.0
SUPPORT_ROLL = -25.0


def aim_pose(kick=0.0, back=0.0):
    """Two-handed pistol aim at shoulder height, in chest rest space; `kick` degrees muzzle rise, `back` units."""
    r = sock_frame(AIM_GRIP + G(0, 0.005 * kick, -back), AIM_DIR, UP)
    if kick:
        r = Matrix.Translation(r.translation) @ Matrix.Rotation(rad(-kick), 4, r.to_3x3().col[0]) @ Matrix.Translation(-r.translation) @ r
    # The support hand cups the gun hand from the left: beside the grip (socket +X is the figure's left), a little
    # lower and forward, its fingers tilted down round the front of the right fist instead of sitting inside it.
    left = r @ Matrix.Translation(SUPPORT_OFFSET) @ Matrix.Rotation(rad(SUPPORT_TILT), 4, "X") @ Matrix.Rotation(rad(SUPPORT_ROLL), 4, "Z")
    return {"arm.R.mode": "ik", "arm.R.sock": r, "arm.L.mode": "ik", "arm.L.sock": left,
            "arm.L.pole": G(0.6, -0.5, -0.6), "arm.R.pole": G(-0.6, -0.4, -0.7),
            "grip.R.fingers": 0.92, "grip.R.thumb": 0.55, "grip.L.fingers": 0.9, "grip.L.thumb": 0.5}


SHOOT_FRAMES = 14


def shoot_p(t, f):
    # Round 16: a bigger kick that reads at game zoom (muzzle up, gun and shoulders back, the head ducks a little).
    k = math.sin(0.5 * math.pi * min(1.0, f / 1.5)) if f < 1.5 else math.exp(-(f - 1.5) / 2.6)
    p = dict(AIM_STANCE)
    p.update(aim_pose(kick=24 * k, back=0.065 * k))
    p["spine2.pitch"] = -5.5 * k
    p["spine1.pitch"] = 1.0 - 2.0 * k
    p["head.pitch"] = 1.0 - 4.0 * k
    p["clav.R.fwd"] = -9 * k
    p["clav.L.fwd"] = -5 * k
    p["hips.loc"] = AIM_STANCE["hips.loc"] + G(0, 0, -0.012 * k)
    return p


RELOAD_FRAMES = 36


def reload_p(t, f):
    aim = aim_pose()
    r_aim, l_aim = aim["arm.R.sock"], aim["arm.L.sock"]
    # The pistol comes in to the chest and tilts so the empty grip faces the left hand.
    # Round 16: a wider arc so it reads at game zoom: the pistol comes in low to the chest, tipped far over.
    r_in = sock_frame(G(-0.02, 1.13, 0.26), G(0.35, 0.6, 0.7), G(-0.75, 0.6, 0.0))
    r_flick = sock_frame(G(-0.1, 1.2, 0.36), G(0.1, 0.75, 0.6), G(-0.9, 0.2, 0.2))
    pouch = sock_frame(G(0.11, 0.97, 0.13), G(0.0, -0.9, 0.3), G(0, 0.1, 1.0))
    below = r_in.translation - r_in.to_3x3().col[1] * 0.11
    mag_up = sock_frame(below, r_in.to_3x3().col[2], r_in.to_3x3().col[1])
    seated = mag_up.copy()
    seated.translation = below + r_in.to_3x3().col[1] * 0.03
    keysR = [(0, r_aim), (4, r_flick), (8, r_in), (27, r_in), (34, r_aim)]
    keysL = [(0, l_aim), (5, sock_frame(G(0.16, 1.04, 0.2), G(0.2, -0.6, 1), UP)), (11, pouch), (13, pouch), (20, mag_up), (23, seated),
             (25, seated), (31, l_aim)]

    def track(keys):
        for (f0, m0), (f1, m1) in zip(keys, keys[1:]):
            if f <= f1:
                return lerp_m(m0, m1, ease((f - f0) / (f1 - f0)))
        return keys[-1][1]
    p = dict(AIM_STANCE)
    p.update(aim)
    p["arm.R.sock"] = track(keysR)
    p["arm.L.sock"] = track(keysL)
    p["grip.L.fingers"] = 0.9 if 10 <= f <= 24 else 0.6
    p["head.pitch"] = 1 + 24 * ease((f - 2) / 6) * (1 - ease((f - 26) / 6))
    p["head.yaw"] = 14 * ease((f - 4) / 6) * (1 - ease((f - 15) / 5))
    p["spine2.pitch"] = 3 + 9 * math.sin(math.pi * t)
    p["spine2.yaw"] = AIM_STANCE["spine2.yaw"] + 8 * math.sin(math.pi * t)
    p["clav.L.fwd"] = 6 * math.sin(math.pi * min(1.0, f / 24))
    # The palm slaps the magazine home.
    if 22 <= f <= 25:
        p["spine2.pitch"] += 4.0
    return p


SLASH_FRAMES = 22


def slash_keys():
    guard = blade_frame(G(-0.19, 1.04, 0.24), G(0.12, 0.85, 0.55), G(0.0, 0.0, 1.0))
    wind = blade_frame(G(-0.3, 1.46, 0.02), G(0.3, 0.45, -0.85), G(0.6, -0.2, 0.7))
    strike = blade_frame(G(-0.02, 1.16, 0.43), G(0.62, -0.08, 0.78), G(0.8, -0.3, -0.5))
    follow = blade_frame(G(0.22, 0.98, 0.24), G(0.8, -0.45, -0.3), G(0.1, -0.6, -0.8))
    return guard, wind, strike, follow


def slash_p(t, f):
    guard, wind, strike, follow = slash_keys()
    keys = [(0, guard, 0.0, 0.0, -4.0), (6, wind, -32.0, -6.0, -2.0), (10, strike, 18.0, 9.0, 4.0), (13, follow, 36.0, 12.0, 6.0),
            (22, guard, 0.0, 0.0, -4.0)]
    for (f0, m0, y0, p0, h0), (f1, m1, y1, p1, h1) in zip(keys, keys[1:]):
        if f <= f1:
            # The strike accelerates (ease-in), wind-up and recovery ease out.
            s = (f - f0) / (f1 - f0)
            e = s * s if f0 == 6 else ease(s)
            sock = lerp_m(m0, m1, e)
            yaw = y0 + (y1 - y0) * e
            pitch = p0 + (p1 - p0) * e
            hips = h0 + (h1 - h0) * e
            break
    p = stance(**{"spine1.yaw": 4 + yaw * 0.4, "spine2.yaw": 4 + yaw * 0.6, "spine1.pitch": 4 + pitch * 0.5,
                  "spine2.pitch": 5 + pitch * 0.5, "hips.yaw": -9 + yaw * 0.25, "hips.loc": STANCE["hips.loc"] + G(0, -0.025 * abs(pitch) / 12, 0),
                  "head.yaw": -yaw * 0.5, "head.pitch": 3 + hips * 0.5})
    p.update({"arm.R.mode": "ik", "arm.R.sock": sock, "arm.R.pole": G(-0.8, -0.6, -0.3),
              "grip.R.fingers": 0.95, "grip.R.thumb": 0.6})
    # The free arm counters the swing: forward at the wind-up, back on the strike.
    a = math.sin(math.pi * min(1.0, f / 12.0)) if f < 12 else 0.0
    b = ease((f - 8) / 5) * (1 - ease((f - 15) / 7))
    p.update({"arm.L.swing": 6 + 40 * a - 45 * b, "arm.L.out": -21 + 30 * a + 25 * b, "arm.L.elbow": 18 + 40 * a - 10 * b,
              "grip.L.fingers": 0.8})
    return p


# --- Shockwave ----------------------------------------------------------------------------------------------

SHOCK_FRAMES, SHOCK_IMPACT = 30, 13


def shock_keys():
    base = stance()
    gather = stance(**{"hips.loc": G(0, -0.1, -0.02), "spine1.pitch": -6, "spine2.pitch": -10, "head.pitch": -8, "neck.pitch": -6,
                       "spine2.yaw": 10, "foot.L.pos": foot_at("L", fwd=0.2, side=0.05), "foot.R.pos": foot_at("R", fwd=-0.22, side=0.05),
                       "foot.R.yaw": 25,
                       "arm.L.swing": 165, "arm.L.out": -8, "arm.L.elbow": 35, "arm.L.wrist": 0, "arm.L.twist": 40,
                       "clav.L.raise": 12, "grip.L.fingers": 1.0, "grip.L.thumb": 0.8,
                       "arm.R.swing": -25, "arm.R.out": 20, "arm.R.elbow": 30})
    slam = stance(**{"hips.loc": G(0.0, -0.43, 0.04), "hips.pitch": 22, "spine1.pitch": 24, "spine2.pitch": 16, "head.pitch": -18,
                     "neck.pitch": -14, "spine2.yaw": -6, "hips.yaw": -4,
                     "foot.L.pos": foot_at("L", fwd=0.26, side=0.13), "foot.L.yaw": 14, "foot.R.pos": foot_at("R", fwd=-0.42, side=0.05, up=0.06),
                     "foot.R.pitch": -48, "foot.R.toe": 45, "foot.R.yaw": 10,
                     "arm.L.swing": 80, "arm.L.out": -8, "arm.L.elbow": 2, "arm.L.wrist": -25, "arm.L.twist": 30,
                     "clav.L.fwd": 10, "grip.L.fingers": 1.0, "grip.L.thumb": 0.8,
                     "arm.R.swing": -40, "arm.R.out": 35, "arm.R.elbow": 25, "arm.R.wrist": -10})
    return base, gather, slam


def shock_p(t, f):
    base, gather, slam = shock_keys()
    if f <= 9:
        p = blend(base, gather, ease(f / 9))
    elif f <= SHOCK_IMPACT:
        s = (f - 9) / (SHOCK_IMPACT - 9)
        p = blend(gather, slam, s * s)
    elif f <= 19:
        p = dict(slam)
        recoil = math.exp(-(f - SHOCK_IMPACT) / 1.5) * math.sin((f - SHOCK_IMPACT) * 1.6)
        p["hips.loc"] = slam["hips.loc"] + G(0, 0.012 * recoil, 0)
    else:
        p = blend(slam, base, ease((f - 19) / (SHOCK_FRAMES - 19)))
    return p


# --- Repair -------------------------------------------------------------------------------------------------

REPAIR_FRAMES = 48
REPAIR_TARGET = G(0.05, 0.35, 0.95)


def kneel(**over):
    p = stance(**{"hips.loc": G(0.0, -0.4, -0.02), "hips.yaw": -4, "hips.pitch": 4, "hips.roll": 0, "spine1.pitch": 8, "spine2.pitch": 6,
                  "spine1.yaw": 2, "spine2.yaw": 2, "neck.pitch": 4, "head.pitch": 10, "head.yaw": 4,
                  "foot.L.pos": foot_at("L", fwd=0.3, side=0.05), "foot.L.yaw": 5,
                  "foot.R.pos": foot_at("R", fwd=-0.35, side=0.03, up=0.03), "foot.R.pitch": -62, "foot.R.toe": 60, "foot.R.yaw": 6,
                  "foot.R.pole": G(0, -0.3, 1.0),
                  "arm.R.swing": 25, "arm.R.out": -14, "arm.R.elbow": 55, "arm.R.wrist": 15})
    p.update(over)
    return p


def repair_p(t, f):
    w = 2 * math.pi * t
    target = REPAIR_TARGET + G(0.07 * math.sin(w), 0.035 * math.sin(2 * w + 0.5), 0)
    p = kneel(**{"spine2.pitch": 6 + 1.2 * math.sin(w), "head.yaw": 4 + 6 * math.sin(w), "head.pitch": 12 + 3 * math.sin(2 * w + 0.5)})
    p["arm.L.mode"] = "ik"
    p["arm.L.space"] = "world"
    p["arm.L.pole"] = G(1.0, -0.4, -0.3)
    # Filled in by repair_compose (needs the posed shoulder): the left fist points the wrist emitter at the target.
    p["_repair_target"] = target
    return p


def compose_with_repair(poser, P):
    target = P.pop("_repair_target", None)
    if target is not None:
        tmp = dict(P)
        tmp["arm.L.mode"] = "fk"
        pose = poser.solve(compose(poser, tmp))
        shoulder = pose["UpperArm_L"].translation
        d = nrm(target - shoulder)
        P["arm.L.sock"] = sock_frame(shoulder + d * 0.47, d, Z)
        P["grip.L.fingers"] = 0.7
    return compose(poser, P)


# --- Hit, Down, Revive --------------------------------------------------------------------------------------

def hit_p(t, f):
    k = math.sin(math.pi * min(1.0, t / 0.3)) if t < 0.3 else math.exp(-(t - 0.3) * 6) * math.cos((t - 0.3) * 12) * 0.5
    return stance(**{"hips.loc": STANCE["hips.loc"] + G(0, -0.015 * k, -0.03 * k), "spine1.pitch": 4 - 7 * k, "spine2.pitch": 5 - 10 * k,
                     "spine2.yaw": 4 - 9 * k, "head.pitch": 3 - 14 * k, "head.yaw": 3 + 10 * k, "neck.pitch": -3 - 5 * k,
                     "arm.L.out": -21 + 18 * k, "arm.R.out": -20 + 18 * k, "arm.L.elbow": 18 + 25 * k, "arm.R.elbow": 22 + 25 * k,
                     "arm.L.swing": 6 - 10 * k, "arm.R.swing": 2 - 10 * k, "clav.L.raise": -3 + 8 * k, "clav.R.raise": -3 + 8 * k,
                     "grip.L.fingers": 0.5 + 0.4 * k, "grip.R.fingers": 0.55 + 0.4 * k})


DOWN_FRAMES = 40
FALL_ROLL = -84.0  # onto the right side
# Round 16: he stumbles half a metre to his left before he topples to his right, so the body ends round the origin
# (the root does not move: the old fall left the hips 0.89 and the head 1.34 to the side of it).
DOWN_SHIFT = G(0.5, 0.0, 0.0)
PIVOT = G(-0.06, 0.06, -0.02)


def lying(roll):
    """Hips location for a body rolled `roll` degrees about the forward axis around the feet (moved DOWN_SHIFT)."""
    hc = SPINE["Hips"][0]
    r = Matrix.Rotation(rad(roll), 3, "Y")
    return PIVOT + r @ (hc - PIVOT) - hc + DOWN_SHIFT


def down_end():
    return stance(**{"hips.loc": lying(FALL_ROLL) + G(0, 0.02, 0), "hips.roll": FALL_ROLL, "hips.yaw": -6, "hips.pitch": 10,
                     "spine1.pitch": 14, "spine2.pitch": 8, "spine1.roll": 6, "spine2.roll": 8, "neck.roll": 18, "head.roll": 12,
                     "head.pitch": 10, "head.yaw": -10,
                     "foot.L.pos": G(-0.12, 0.22, 0.32) + DOWN_SHIFT, "foot.L.pitch": 10, "foot.R.pos": G(-0.22, 0.07, 0.14) + DOWN_SHIFT,
                     "foot.R.pitch": 0,
                     "foot.L.pole": G(0.0, 0.3, 1.0), "foot.R.pole": G(0, -0.3, 1.0), "foot.L.yaw": -20, "foot.R.yaw": -20,
                     "arm.R.swing": 70, "arm.R.out": -10, "arm.R.elbow": 70, "arm.L.swing": 30, "arm.L.out": -5, "arm.L.elbow": 45,
                     "arm.L.twist": 30, "grip.L.fingers": 0.3, "grip.R.fingers": 0.3})


def down_p(t, f):
    stagger = stance(**{"hips.loc": STANCE["hips.loc"] + G(0.16, -0.03, -0.08), "spine1.pitch": -8, "spine2.pitch": -12,
                        "spine2.yaw": -10, "head.pitch": -16, "neck.pitch": -8, "hips.roll": 6,
                        "foot.R.pos": foot_at("R", fwd=-0.28, side=0.06) + G(0.12, 0, 0), "foot.L.pos": foot_at("L", fwd=0.02, side=0.04) + G(0.3, 0, 0),
                        "arm.L.out": 15, "arm.R.out": 20, "arm.L.elbow": 50, "arm.R.elbow": 45, "arm.L.swing": -15, "arm.R.swing": -20,
                        "clav.L.raise": 6, "clav.R.raise": 6, "grip.L.fingers": 0.2, "grip.R.fingers": 0.2})
    buckle = stance(**{"hips.loc": G(0.3, -0.22, -0.06), "hips.roll": -20, "spine1.pitch": 6, "spine2.pitch": 4, "spine1.roll": -6,
                       "head.pitch": 12, "head.roll": -10, "foot.R.pos": foot_at("R", fwd=-0.2, side=0.06) + G(0.4, 0, 0),
                       "foot.L.pos": foot_at("L", fwd=0.08, side=0.04) + G(0.42, 0, 0), "arm.R.swing": 40, "arm.R.out": 10, "arm.R.elbow": 50,
                       "arm.L.swing": 20, "arm.L.out": 25, "arm.L.elbow": 40})
    end = down_end()
    if f <= 8:
        p = blend(stance(), stagger, ease(f / 8))
    elif f <= 16:
        p = blend(stagger, buckle, ease((f - 8) / 8))
    elif f <= 26:
        s = (f - 16) / 10
        p = blend(buckle, end, s * s)
        roll = -20 + (FALL_ROLL + 20) * s * s
        p["hips.loc"] = lying(roll) * (s * s) + buckle["hips.loc"] * (1 - s * s) if s < 1 else end["hips.loc"]
        p["hips.roll"] = roll
    else:
        p = dict(end)
        bounce = math.exp(-(f - 26) / 2.0) * math.sin((f - 26) * 1.5)
        p["hips.loc"] = end["hips.loc"] + G(0, 0.015 * bounce, 0)
        p["head.roll"] = end["head.roll"] + 6 * bounce
    return p


REVIVE_FRAMES = 36


def revive_p(t, f):
    end = down_end()
    sit = stance(**{"hips.loc": G(-0.32, 0.2 - 0.9, 0.0) + DOWN_SHIFT * 0.7, "hips.roll": -38, "hips.pitch": 14, "hips.yaw": -10, "spine1.pitch": 16,
                    "spine2.pitch": 10, "spine1.roll": 16, "spine2.roll": 12, "neck.roll": 4, "head.pitch": 8,
                    "foot.L.pos": G(0.0, 0.09, 0.32) + DOWN_SHIFT * 0.7, "foot.L.pole": G(0.3, 0.2, 1),
                    "foot.R.pos": G(-0.3, 0.08, 0.12) + DOWN_SHIFT * 0.7,
                    "foot.R.pole": G(-0.6, -0.5, 1.0), "foot.R.yaw": -30, "foot.R.pitch": 0,
                    "arm.R.swing": 8, "arm.R.out": 32, "arm.R.elbow": 6, "arm.R.wrist": -45, "grip.R.fingers": 0.1,
                    "arm.L.swing": 45, "arm.L.out": -12, "arm.L.elbow": 60})
    knee = kneel(**{"head.pitch": -4, "spine1.pitch": 10, "arm.R.swing": 20, "arm.R.out": -10, "arm.R.elbow": 40,
                    "arm.L.swing": 25, "arm.L.out": -15, "arm.L.elbow": 60})
    keys = [(0, end), (12, sit), (22, knee), (REVIVE_FRAMES, stance())]
    return keyed(keys, f)


CLIPS = {"Idle": clip(IDLE_FRAMES, idle_p, True), "Walk": clip(WALK_FRAMES, walk_p, True), "Run": clip(RUN_FRAMES, run_p, True),
         "Shoot": clip(SHOOT_FRAMES, shoot_p), "Reload": clip(RELOAD_FRAMES, reload_p), "Slash": clip(SLASH_FRAMES, slash_p),
         "Shockwave": clip(SHOCK_FRAMES, shock_p), "Hit": clip(12, hit_p), "Down": clip(DOWN_FRAMES, down_p),
         "Revive": clip(REVIVE_FRAMES, revive_p)}


def repair_clip(poser):
    def spec(t, f):
        return compose_with_repair(poser, repair_p(t, f))
    return REPAIR_FRAMES, spec, True


CLIPS["Repair"] = repair_clip


# ------------------------------------------------------------------------------------------------------------
# Build, LOD, export
# ------------------------------------------------------------------------------------------------------------

def skin_all(mesh_obj, arm, builder):
    C.skin(mesh_obj, arm, builder, rigid=RIGID, blend=2)
    # Sockets keep an (empty) vertex group off: they are bones for attaching, not for deforming.
    for n in SOCKETS:
        vg = mesh_obj.vertex_groups.get(n)
        if vg:
            mesh_obj.vertex_groups.remove(vg)


def build_lod(asset, arm, builder, index):
    lod_mesh = C.decimate(asset.low.data.copy(), LOD_TRIS, LOD_NAME, symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(LOD_NAME, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    index.register(lod_mesh)
    skin_all(lod, arm, builder)
    return lod


def stage_exports():
    """`--stage <dir>`: write exports and the .blend there instead of ArtSource (iterations while Unity imports)."""
    stage = C.cli_args().get("stage")
    if stage:
        C.EXPORTS = os.path.join(stage, "Exports")
        C.BLEND = os.path.join(stage, "Blend")


def main():
    C.reset_scene()
    stage_exports()
    out = C.preview_dir()
    weapons = W.build()
    builder = build_parts()
    asset, index = S.build_layered_asset(NAME, builder, LAYERS, voxel=0.0034, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.06,
                                         extrusion=0.01, ray=0.03, emission_strength=2.5, extra=S.weathering(), specular=0.4,
                                         mirror=True, k=1.0, families=FAMILIES, family_set=S.FAMILY_SET, low_size=LOW_TEX,
                                         preview_detail=(0.8, 0.6), edge_gain=0.45, border_smooth=4)
    bone_list = bones()
    arm = build_armature(NAME + "_Rig", bone_list)
    skin_all(asset.low, arm, builder)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    lod = build_lod(asset, arm, builder, index)
    for o in (asset.low, lod):
        S.drop_layer_attr(o)
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low, lod], animated=True)
    C.write_report(NAME, {
        "asset": NAME, "game_tris": asset.low_tris, "lod1_tris": C.tri_count(lod), "source_tris": asset.high_tris,
        "bones": len(arm.data.bones), "sockets": sorted(SOCKETS), "texture_size": TEX, "low_texture_size": LOW_TEX,
        "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
        "layers": [layer.name for layer in LAYERS],
        "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
        "walk_distance_per_loop": round(WALK_STRIDE / WALK_STANCE, 3), "run_distance_per_loop": round(RUN_STRIDE / RUN_STANCE, 3),
        "events": {"Shoot": {"muzzle_flash": 0}, "Reload": {"mag_out": 4, "mag_in": 23}, "Slash": {"hit": 10},
                   "Shockwave": {"impact": SHOCK_IMPACT}, "Down": {"ground": 26}},
        "upper_body_mask": "Spine1 and all its children"})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        import mercer_previews
        mercer_previews.render_all(arm, asset, lod, actions, weapons, out)


if __name__ == "__main__":
    main()
