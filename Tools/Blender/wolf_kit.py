"""Round 49A: the alien-wolf anatomy shared by the Prowler and its boss, the PackAlpha (Remco: the Prowler "looks
rather reptile-like, it must be more alien-WOLF-like"; the Alpha "looks like a porcupine or hippo").

One species, two sizes: a lean pack hunter on long legs. A deep, narrow chest over a tucked-up waist; long forelegs
on raised paws and digitigrade hind legs (stifle forward, hock high and back); a thick neck under a ruff of
fur-like quill plates that runs on down the spine; a wolf skull with a long tapering muzzle, a split lower jaw (two
mandible halves that drop and splay apart, the alien touch) and two pairs of tall swept ear blades; a drooping
brush tail of layered quills. Orange glow lives in the seams between the plate rows (tiger-stripe bands) and in the
throat; the eyes (four, two per side) and the fangs are the round-46 Face part.

Coordinates are game axes in Prowler units (x, up, forward), scaled by `s`. The bone names are the round-4 rig's
(Root, Pelvis, Chest, Neck, Head, Jaw_L/R, Tail_1..3, Leg_{L,R}{F,H}_{Upper,Lower,Foot}); the alpha adds Mane_L/R.
"""

import math

from mathutils import Matrix, Vector

import chitin as H
import common as C
from common import G

SIDES = (("L", -1), ("R", 1))

# Legs (Prowler units): hip (x, up, forward), foot (toe on the ground), bone lengths, knee pole, foot direction.
FORE = dict(hip=(0.085, 0.5, 0.19), foot=(0.1, 0.0, 0.27), lengths=(0.225, 0.235, 0.1), pole=(0, 0.15, -1.0),
            foot_dir=(0, -0.9, 0.42), parent="Chest")
HIND = dict(hip=(0.085, 0.5, -0.31), foot=(0.1, 0.0, -0.3), lengths=(0.22, 0.25, 0.17), pole=(0, 0.1, 1.0),
            foot_dir=(0, -0.86, 0.5), parent="Pelvis")
LEG_KINDS = {"F": FORE, "H": HIND}

# Spine and head (Prowler units, x = 0).
PELVIS = ((0.52, -0.36), (0.535, -0.06))
CHEST = ((0.535, -0.06), (0.6, 0.26))
NECK = ((0.6, 0.26), (0.74, 0.47))
HEAD = ((0.74, 0.47), (0.692, 0.79))
TAIL = [(0.52, -0.38), (0.47, -0.53), (0.39, -0.66), (0.3, -0.76)]


class Wolf:
    """The anatomy at scale `s` (1 = Prowler). `alpha`: the pack leader's heavier front, collar and scars."""

    def __init__(self, s=1.0, alpha=False):
        self.s = s
        self.alpha = alpha

    # -- geometry helpers ---------------------------------------------------------------------------------------
    def P(self, x, up, fwd):
        return G(x * self.s, up * self.s, fwd * self.s)

    def R(self, *r):
        return tuple(v * self.s for v in r) if len(r) > 1 else r[0] * self.s

    def legs(self):
        out = {}
        for side, sx in SIDES:
            for kind, d in LEG_KINDS.items():
                names = [f"Leg_{side}{kind}_{seg}" for seg in ("Upper", "Lower", "Foot")]
                out[(side, kind)] = C.Limb(names, self.P(d["hip"][0] * sx, d["hip"][1], d["hip"][2]),
                                           self.P(d["foot"][0] * sx, d["foot"][1], d["foot"][2]),
                                           self.R(*d["lengths"]), G(*d["pole"]), foot_dir=G(*d["foot_dir"]))
        return out

    def jaw_points(self, sx):
        """One mandible half: hinge under the back of the skull to the chin, under the muzzle."""
        return [self.P(0.04 * sx, 0.708, 0.53), self.P(0.034 * sx, 0.67, 0.62), self.P(0.022 * sx, 0.655, 0.695),
                self.P(0.008 * sx, 0.66, 0.74)]

    def tail_points(self):
        return [self.P(0, up, f) for up, f in TAIL]

    def mane_root(self, sx):
        return self.P(0.05 * sx, 0.69, 0.36)

    # -- parts ---------------------------------------------------------------------------------------------------
    def build_parts(self, pal, legp, k):
        b = C.PartBuilder()
        P, R, a = self.P, self.R, self.alpha
        wide = 1.28 if a else 1.0
        # Trunk: a deep keel of a chest, a tucked-up waist, a lean high croup.
        def keel(v):
            # A deep, narrow chest: the sides close in towards the breastbone (no barrel belly).
            return Vector((v.x * (1 - 0.55 * max(0.0, -v.z)), v.y, v.z * (1 - 0.12 * max(0.0, -v.z))))
        b.ellipsoid("Chest", P(0, 0.5, 0.11), R(0.104 * wide, 0.2, 0.16), Matrix.Rotation(math.radians(-8), 3, "X"),
                    bone="Chest", style=H.band_plates(pal, k, 5, height=0.0022, glow=0.7, shingle=True), segments=(48, 24),
                    deform=keel)
        b.ellipsoid("Brisket", P(0, 0.46, 0.235), R(0.05 * wide, 0.07, 0.07), bone="Chest", deform=keel,
                    style=H.band_plates(pal, k, 3, height=0.003, glow=0.2), segments=(32, 16))
        b.ellipsoid("Waist", P(0, 0.55, -0.12), R(0.064 * wide, 0.15, 0.075), bone="Pelvis",
                    style=H.band_plates(pal, k, 4, height=0.002, glow=0.6, shingle=True), segments=(36, 18))
        b.parts[-1].chain = ["Pelvis", "Chest"]
        b.ellipsoid("Croup", P(0, 0.535, -0.3), R(0.09, 0.12, 0.1), Matrix.Rotation(math.radians(8), 3, "X"),
                    bone="Pelvis", style=H.band_plates(pal, k, 2, height=0.004), segments=(36, 18))
        for sx in (-1, 1):
            # Shoulder blades and thigh muscle: the limbs grow out of mass, not out of a tube.
            b.ellipsoid("Shoulder", P(0.085 * wide * sx, 0.55, 0.2), R(0.055 * wide, 0.085, 0.12),
                        Matrix.Rotation(math.radians(-18), 3, "X"), bone="Chest",
                        style=H.band_plates(pal, k, 3, height=0.004, glow=0.25 if a else 0.0, shingle=True),
                        segments=(28, 14))
            b.ellipsoid("Thigh", P(0.075 * sx, 0.47, -0.31), R(0.055, 0.085, 0.11), Matrix.Rotation(math.radians(20), 3, "X"),
                        bone=f"Leg_{'L' if sx < 0 else 'R'}H_Upper", style=H.band_plates(legp, k, 2, height=0.003),
                        segments=(28, 14))
            b.parts[-1].chain = [f"Leg_{'L' if sx < 0 else 'R'}H_Upper", "Pelvis"]
        # Neck and skull.
        b.tube("Neck", [P(0, 0.58, 0.24), P(0, 0.67, 0.36), P(0, 0.74, 0.46)], [R(0.085 * wide), R(0.072), R(0.06)],
               sides=16, chain=["Neck"], style=H.tube(pal, k, pal.top * 0.9, pal.top * 0.8, rings=(0.35, 0.7), rough=0.66))
        b.ellipsoid("Skull", P(0, 0.755, 0.535), R(0.08, 0.08, 0.07), Matrix.Rotation(math.radians(6), 3, "X"),
                    bone="Head", style=H.wedge_head(pal, k, slits=3, slit_width=0.05), segments=(36, 18))
        b.tube("Muzzle", [P(0, 0.75, 0.57), P(0, 0.724, 0.655), P(0, 0.706, 0.725), P(0, 0.7, 0.752)],
               [R(0.054), R(0.04), R(0.027), R(0.017)], sides=14, chain=["Head"],
               style=H.tube(pal, k, pal.top * 0.85, pal.dark * 1.4, rings=(0.55,), ring_dark=0.4, rough=0.62))
        # A bony brow ridge over each eye pair and cheek plates over the jaw hinge.
        for sx in (-1, 1):
            b.tube("Brow", [P(0.024 * sx, 0.795, 0.6), P(0.05 * sx, 0.81, 0.56), P(0.062 * sx, 0.8, 0.505)],
                   [R(0.012), R(0.016), R(0.008)], sides=8, chain=["Head"], style=H.hook(pal, k))
            b.ellipsoid("Cheek", P(0.054 * sx, 0.735, 0.555), R(0.036, 0.065, 0.042), bone="Head",
                        style=H.band_plates(pal, k, 2, height=0.002), segments=(20, 10))
        b.ellipsoid("Throat", P(0, 0.685, 0.65), R(0.022, 0.06, 0.018), bone="Head", style=H.inner_glow(pal, 0.9),
                    segments=(16, 8))
        for side, sx in SIDES:
            # The split lower jaw: each mandible half hinges on its own bone.
            b.tube("Jaw", self.jaw_points(sx), [R(0.026), R(0.024), R(0.016), R(0.006)], sides=10, chain=[f"Jaw_{side}"],
                   style=H.tube(pal, k, pal.top * 0.75, pal.dark * 1.3, rough=0.6), flatten=0.75)
            # Two pairs of tall ear blades, swept back: the wolf read from the top camera.
            for root, mid, tip, r0 in (((0.034, 0.815, 0.52), (0.048, 0.89, 0.505), (0.058, 0.975, 0.47), 0.042),
                                       ((0.056, 0.795, 0.49), (0.085, 0.85, 0.465), (0.115, 0.9, 0.41), 0.03)):
                b.tube("Ear", [P(root[0] * sx, root[1], root[2]), P(mid[0] * sx, mid[1], mid[2]),
                               P(tip[0] * sx, tip[1], tip[2])], [R(r0), R(r0 * 0.75), R(0.002)], sides=10,
                       chain=["Head"], style=H.blade(pal, k, glow=0.7), flatten=0.32)
        self._ruff(b, pal, k)
        if a:
            self._alpha(b, pal, k)
        self._tail(b, pal, k)
        self._legs(b, legp, pal, k)
        return b

    def _quill(self, b, pal, k, root, direction, length, radius, bone, glow=0.0, name="Quill"):
        d = Vector(direction).normalized()
        down = Vector((0, 0, -1))
        tip = root + d * length + down * length * 0.2
        # Body-coloured plates darkening to the tip (fur, not bone spikes); some tips glow.
        b.tube(name, [root, root + d * length * 0.5 + down * length * 0.04, tip], [radius, radius * 0.7, self.R(0.0015)],
               sides=8, chain=[bone] if isinstance(bone, str) else bone,
               style=H.tube(pal, k, pal.top * 0.95, pal.dark * 1.6, tip_glow=glow, glow_from=0.72, rough=0.7),
               flatten=0.4)

    def _ruff(self, b, pal, k):
        """Fur-like quill plates laid back over the neck, shoulders and spine (shaggy ruff, not a spike crest)."""
        P, R, a = self.P, self.R, self.alpha
        back = G(0, 0, -1)
        # Neck ruff: rings of plates round the top and sides of the neck.
        rows = [(0.44, 0.73, 0.055, 0.09), (0.38, 0.69, 0.065, 0.11), (0.32, 0.65, 0.072, 0.125),
                (0.26, 0.6, 0.082, 0.125), (0.2, 0.6, 0.09, 0.1)]
        for i, (f, up, rad, length) in enumerate(rows):
            n = 9
            for j in range(n):
                ang = (-1 + 2 * j / (n - 1)) * math.radians(105)
                if abs(ang) < 0.05 and i % 2:
                    continue
                out = G(math.sin(ang), math.cos(ang), 0)
                root = P(0, up, f) + out * R(rad * 0.85)
                d = (back * 0.9 + out * 0.3).normalized()
                bone = "Neck" if f > 0.3 else "Chest"
                self._quill(b, pal, k, root, d, R(length * (1.2 if a else 1.0)), R(0.02), bone,
                            glow=0.6 if j in (0, n - 1) else 0.0)
        # Down the spine: two staggered rows of short plates, longest over the withers.
        for i in range(9):
            f = 0.2 - i * 0.065
            up = 0.645 if f > 0.0 else (0.605 if f > -0.22 else 0.615)
            length = 0.085 - i * 0.005
            bone = "Chest" if f > -0.05 else "Pelvis"
            for sx in (-1, 1):
                x = 0.022 * sx * (1 if i % 2 else 0.5)
                root = P(x, up, f)
                d = (back * 0.9 + G(0.35 * sx, 0.35, 0)).normalized()
                self._quill(b, pal, k, root, d, R(length), R(0.014), bone, glow=0.9 if i % 3 == 0 else 0.0)

    def _alpha(self, b, pal, k):
        """The leader's marks: a collar of long glowing plates on the Mane bones (it flares on the howl), two pale
        bone horns swept back from the brow, and a pale forward chevron of bone over the withers."""
        P, R = self.P, self.R
        back = G(0, 0, -1)
        for side, sx in SIDES:
            root = self.mane_root(sx)
            for j, (ang, length) in enumerate(((0.35, 0.2), (0.75, 0.24), (1.15, 0.23), (1.55, 0.19), (1.95, 0.14))):
                out = G(math.sin(ang) * sx, math.cos(ang), 0)
                base = root + out * R(0.05) + back * R(0.01 * j)
                d = (back * 0.65 + out * 0.6 + G(0, 0.12, 0)).normalized()
                self._quill(b, pal, k, base, d, R(length), R(0.024), f"Mane_{side}", glow=1.0, name="Collar")
            horn = P(0.045 * sx, 0.8, 0.545)
            b.tube("Horn", [horn, horn + P(0.03 * sx, 0.045, -0.06), horn + P(0.055 * sx, 0.06, -0.17)],
                   [R(0.018), R(0.012), R(0.0015)], sides=8, chain=["Head"],
                   style=H.tube(pal, k, pal.bone * 0.8, pal.bone, tip_glow=0.6, glow_from=0.8, rough=0.5), flatten=0.8)
            b.tube("Chevron", [P(0.07 * sx, 0.655, 0.0), P(0.04 * sx, 0.672, 0.1), P(0.008 * sx, 0.68, 0.2)],
                   [R(0.022), R(0.018), R(0.01)], sides=8, chain=["Chest"],
                   style=H.tube(pal, k, pal.bone, pal.bone, tip_glow=1.0, glow_from=0.3, rough=0.5), flatten=0.5)

    def _tail(self, b, pal, k):
        """A drooping brush: a thick tapered core under layered quills, a glowing tip tuft."""
        R = self.R
        pts = self.tail_points()
        names = ["Tail_1", "Tail_2", "Tail_3"]
        b.tube("Tail", pts, [R(0.042), R(0.05), R(0.042), R(0.022)], sides=14, chain=names,
               style=H.tube(pal, k, pal.top * 0.85, pal.side, rings=(0.33, 0.66), ring_dark=0.5, rough=0.66))
        curve = C.catmull_rom([Vector(p) for p in pts], 8)
        n = len(curve)
        for i in range(1, 7):
            t = i / 7
            c = curve[int(t * (n - 1))]
            tangent = (curve[min(n - 1, int(t * (n - 1)) + 1)] - curve[max(0, int(t * (n - 1)) - 1)]).normalized()
            bone = names[min(2, int(t * 3))]
            for ang in (-1.4, -0.7, 0.0, 0.7, 1.4):
                side = Vector((math.sin(ang), 0, math.cos(ang)))
                side = (side - tangent * side.dot(tangent)).normalized()
                rad = R(0.042) * (1 - 0.45 * t)
                d = (tangent * 0.8 + side * 0.4).normalized()
                self._quill(b, pal, k, c + side * rad * 0.8, d, R(0.07 * (1 - 0.3 * t)), R(0.013), bone,
                            glow=1.0 if t > 0.7 else 0.0)
        tip = pts[-1]
        tangent = (pts[-1] - pts[-2]).normalized()
        self._quill(b, pal, k, tip, tangent, R(0.08), R(0.016), "Tail_3", glow=1.0)

    def _legs(self, b, legp, pal, k):
        R, a = self.R, self.alpha
        for (side, kind), lg in self.legs().items():
            hip, knee, ankle, toe = lg.rest_points
            sx = -1 if side == "L" else 1
            heavy = (1.5 if kind == "F" else 1.3) if a else 1.0
            upper_r = (0.05, 0.04, 0.03) if kind == "F" else (0.058, 0.045, 0.028)
            b.tube("Upper", [hip, hip.lerp(knee, 0.5), knee], [R(upper_r[0] * heavy), R(upper_r[1] * heavy), R(upper_r[2])],
                   sides=12, chain=[lg.names[0]], style=H.leg(legp, k, "Femur", spines=2))
            b.tube("Lower", [knee, knee.lerp(ankle, 0.5), ankle], [R(0.028 * heavy), R(0.024 * heavy ** 0.5), R(0.019)], sides=10,
                   chain=[lg.names[1]], style=H.leg(legp, k, "Femur"))
            b.ellipsoid("Joint", knee, (R(0.032 * heavy),) * 3, bone=lg.names[0],
                        style=H.band_plates(legp, k, 1, height=0.0), segments=(16, 8))
            b.parts[-1].chain = [lg.names[0], lg.names[1]]
            b.ellipsoid("Joint", ankle, (R(0.023),) * 3, bone=lg.names[1], style=H.band_plates(legp, k, 1, height=0.0),
                        segments=(16, 8))
            b.parts[-1].chain = [lg.names[1], lg.names[2]]
            b.tube("Pastern", [ankle, ankle.lerp(toe, 0.5), toe + Vector((0, 0, R(0.012)))], [R(0.018), R(0.016), R(0.02)],
                   sides=10, chain=[lg.names[2]], style=H.leg(legp, k, "Tibia"))
            # The paw: a pad on the ground with four toes; the talons are the Claws part.
            fwd = G(0, 0, 1)
            b.ellipsoid("Paw", toe + fwd * R(0.022) + Vector((0, 0, R(0.016))), R(0.03, 0.045, 0.018), bone=lg.names[2],
                        style=H.band_plates(legp, k, 2, height=0.002), segments=(20, 10))
            for j, ang in enumerate((-0.5, -0.17, 0.17, 0.5)):
                d = G(math.sin(ang) * 0.6, 0, 1).normalized()
                base = toe + fwd * R(0.02) + Vector((0, 0, R(0.014)))
                b.tube("Toe", [base, base + d * R(0.035) + Vector((0, 0, R(0.002))), base + d * R(0.06) - Vector((0, 0, R(0.008)))],
                       [R(0.012), R(0.01), R(0.007)], sides=8, chain=[lg.names[2]], style=H.leg(legp, k, "Tibia"))
            if kind == "F":
                # A dewclaw spur behind the wrist and a blade at the elbow.
                b.tube("Spur", [ankle, ankle + self.P(0.01 * sx, 0.02, -0.04), ankle + self.P(0.016 * sx, 0.035, -0.085)],
                       [R(0.012), R(0.007), R(0.001)], sides=8, chain=[lg.names[1]], style=H.hook(pal, k, glow=0.8))
                b.tube("ElbowSpur", [knee, knee + self.P(0.008 * sx, 0.03, -0.045), knee + self.P(0.014 * sx, 0.06, -0.1)],
                       [R(0.014 * heavy), R(0.008), R(0.001)], sides=8, chain=[lg.names[0]], style=H.hook(pal, k, glow=0.6))

    # -- rig -----------------------------------------------------------------------------------------------------
    def bones(self):
        P = self.P

        def seg(name, pair, parent):
            (u0, f0), (u1, f1) = pair
            return (name, P(0, u0, f0), P(0, u1, f1), parent, True)
        out = [("Root", P(0, 0, 0), P(0, 0, 0.2), None, True), seg("Pelvis", PELVIS, "Root"), seg("Chest", CHEST, "Pelvis"),
               seg("Neck", NECK, "Chest"), seg("Head", HEAD, "Neck")]
        for side, sx in SIDES:
            pts = self.jaw_points(sx)
            out.append((f"Jaw_{side}", pts[0], pts[-1], "Head", True))
            if self.alpha:
                root = self.mane_root(sx)
                out.append((f"Mane_{side}", root, root + P(0.16 * sx, 0.06, -0.12), "Chest", True))
        tp = self.tail_points()
        out += [("Tail_1", tp[0], tp[1], "Pelvis", True), ("Tail_2", tp[1], tp[2], "Tail_1", True),
                ("Tail_3", tp[2], tp[3], "Tail_2", True)]
        for (side, kind), lg in self.legs().items():
            out += lg.rest_bones(LEG_KINDS[kind]["parent"])
        return out


def jaw_rot(sx, jaw):
    """The split jaw opening: both halves drop (nose-down X rotation) and splay apart a little."""
    return [(Vector((0, 0, 1)), -sx * jaw * 0.06), (Vector((1, 0, 0)), jaw * 0.85)]
