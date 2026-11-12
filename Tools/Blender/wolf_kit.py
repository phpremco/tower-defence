"""Round 49E: the alien-wolf anatomy shared by the Prowler and its boss, the PackAlpha.

Remco on the round-49A wolves: "everything is smooth as if made of metal, it looks like a children's robot dog".
The wolf idea stays; the execution is rebuilt from the muscles out:

- A gaunt, starved predator. A deep, narrow rib cage whose ribs show through the hide, a waist tucked up to the
  spine, hip bones and shoulder blades that stand out under the skin, a row of vertebrae along the lumbar.
- Muscle, not tubes. Every limb is shoulder/thigh mass, a bulging upper muscle (triceps, biceps femoris, gaskin)
  and a sinewy, tendon-corded lower leg that thins to the joint. Pale bony protrusions at the joints: an elbow spur,
  a hooked hock spur (the alien touch), knuckled wrists and big splayed paws.
- Silhouette breakup: shaggy locks of quill-fur in clumps (a hanging throat ruff, a mane down the neck and spine,
  feathering behind the elbows, tufts on the haunches, a ragged brush tail), every lock tapered, bent and varied.
- A low-slung head on a thick neck: a long wolf skull with a sagittal crest, heavy brow ridges and flared cheek
  bones, laid-back ragged ears (a second, smaller pair behind them), a long muzzle wrinkled in a snarl with the
  upper lip pulled up off the gums, and the split lower jaw (two mandible halves) kept from round 49A.
- Matte, layered surfaces: a rough hide with fur striation that follows the body, dark mottling and stripes,
  raised pale scars; near-black quills with rust tips; pale cracked bone; glow only in the eyes (Face part), the
  throat and thin broken seams in the flank stripes.

Coordinates are game axes in Prowler units (x, up, forward), scaled by `s`. Bone names are the round-4 rig's
(Root, Pelvis, Chest, Neck, Head, Jaw_L/R, Tail_1..3, Leg_{L,R}{F,H}_{Upper,Lower,Foot}); the alpha adds Mane_L/R.
"""

import math
import random

import numpy as np
from mathutils import Matrix, Vector

import chitin as H
import common as C
from common import G

SIDES = (("L", -1), ("R", 1))

# Legs (Prowler units): hip (x, up, forward), foot (toe on the ground), bone lengths, knee pole, foot direction.
# Unchanged from round 49A (the gait is tuned to them).
FORE = dict(hip=(0.085, 0.5, 0.19), foot=(0.1, 0.0, 0.27), lengths=(0.225, 0.235, 0.1), pole=(0, 0.15, -1.0),
            foot_dir=(0, -0.9, 0.42), parent="Chest")
HIND = dict(hip=(0.085, 0.5, -0.31), foot=(0.1, 0.0, -0.3), lengths=(0.22, 0.25, 0.17), pole=(0, 0.1, 1.0),
            foot_dir=(0, -0.86, 0.5), parent="Pelvis")
LEG_KINDS = {"F": FORE, "H": HIND}

# Spine and head (Prowler units, x = 0): (up, forward) head -> tail of each bone. Round 49E: the neck runs forward
# and a little down from the withers, the head is held low and long (a stalking predator, not a show dog).
PELVIS = ((0.53, -0.36), (0.555, -0.06))
CHEST = ((0.555, -0.06), (0.6, 0.25))
NECK = ((0.6, 0.25), (0.6, 0.44))
HEAD = ((0.6, 0.44), (0.52, 0.8))
TAIL = [(0.53, -0.38), (0.48, -0.52), (0.4, -0.63), (0.31, -0.71)]

# The skull (Prowler units): cranium, muzzle, jaw line (also read by the detail scripts for eyes and teeth).
CRANIUM = ((0.0, 0.602, 0.5), (0.08, 0.092, 0.074))       # centre (x, up, fwd), radii (side, length, height)
MUZZLE = ((0.0, 0.565, 0.632), (0.044, 0.098, 0.039))
NOSE = (0.0, 0.553, 0.724)
GUM_UP = 0.535          # height of the upper gum line along the muzzle (the snarl lifts the lip above it)
EYES = ((0.05, 0.6, 0.575), (0.066, 0.612, 0.535))
GUM = (0.24, 0.05, 0.05)    # dark wet gums (sRGB)   # front (bigger) and rear eye, right side


def _rot_along(d, roll=0.0):
    """A rotation whose local +Y runs along `d` (for ellipsoids laid along a bone)."""
    q = Vector(d).normalized().to_track_quat("Y", "Z")
    return (q.to_matrix() @ Matrix.Rotation(roll, 3, "Y"))


def _smooth(e0, e1, x):
    return C.smoothstep(e0, e1, x)


def sinew(seed, amp=0.08, flat=0.0):
    """Deform for a muscle ellipsoid: uneven bulges (no perfect egg), a slightly flattened side against the bone."""
    rng = random.Random(seed)
    waves = [(Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))).normalized(), rng.uniform(0, 6.3))
             for _ in range(4)]

    def deform(v):
        bump = sum(math.sin(3.1 * v.dot(d) + ph) for d, ph in waves) / len(waves)
        r = 1.0 + amp * bump
        x = v.x * r * (1 - flat * max(0.0, -v.x))
        return Vector((x, v.y * r, v.z * r))
    return deform


# ------------------------------------------------------------------------------------------------------------
# Surface styles
# ------------------------------------------------------------------------------------------------------------

def _anisotropic(points, flow, scale, seed, stretch=0.88):
    """Noise stretched along `flow` (fur that lies along the body): squash the coordinate along the flow."""
    f = np.asarray(flow, dtype=float)
    f = f / np.linalg.norm(f)
    along = points @ f
    q = points - np.outer(along, f) * stretch
    return C.value_noise(q, scale, seed)


def hide(pal, k, seed=0, flow=(0.0, 1.0, -0.35), stripes=0.0, stripe_axis=1, stripe_freq=26.0, ribs=0.0,
         scars=(), glow=0.0, fur=1.0, belly=0.5, wrinkle=0.0):
    """The wolf's hide: rough and matte, dark along the back, rust on the flanks, a dull pale belly; big dark
    mottles; fur striation lying along `flow` (Blender space; default back and a little down); optional dark stripes
    across the body (broken, with a thin glowing seam in some), ribs showing through, raised pale scars (a list of
    (a, b, normal) segments in Blender space) and wrinkles across the part (the snarl)."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        up = n[:, 2]
        col = C.lerp3(pal.side, pal.top, _smooth(0.0, 0.7, up))
        col = C.lerp3(col, pal.belly, _smooth(-0.15, -0.8, up) * belly)
        blot = C.value_noise(p, 9.0 / k, seed)
        col = col * (0.62 + 0.5 * (0.5 + 0.5 * blot))[:, None]
        streak = _anisotropic(p, flow, 150.0 / k, seed + 1)
        fine = _anisotropic(p, flow, 420.0 / k, seed + 2, stretch=0.93)
        col = col * (0.8 + 0.22 * streak + 0.12 * fine)[:, None]
        disp = (0.0016 * streak + 0.0007 * fine) * k * fur
        emit = np.zeros(len(idx))
        if stripes:
            along = p[:, stripe_axis] / k
            wav = along * stripe_freq + 1.6 * C.value_noise(p, 6.0 / k, seed + 3)
            band = _smooth(0.72, 0.95, np.abs(np.sin(wav)))
            broken = _smooth(-0.2, 0.3, C.value_noise(p, 14.0 / k, seed + 4))
            side = _smooth(-0.55, 0.35, up) * (1 - _smooth(0.55, 0.95, up))
            st = band * broken * side * stripes
            col = C.lerp3(col, pal.seam, st * 0.8)
            seam = _smooth(0.985, 1.0, np.abs(np.sin(wav))) * broken * side * _smooth(0.3, 0.7, broken)
            emit = np.maximum(emit, seam * glow)
            disp = disp - 0.0012 * k * st
        if ribs:
            # Ribs show through: ridges slanting down and forward over the barrel, deepest grooves dark.
            L = C.local_points(part, p)
            slant = L[:, 1] * 0.5 + L[:, 2] * 0.42
            r = np.sin(slant * math.pi * 8.0)
            flank = (_smooth(0.3, 0.8, np.abs(L[:, 0])) * _smooth(-0.75, -0.35, L[:, 2])
                     * (1 - _smooth(0.35, 0.7, L[:, 2])))
            disp = disp + 0.0065 * k * ribs * r * flank
            col = col * (1.0 - 0.28 * ribs * flank * _smooth(0.2, -0.9, r))[:, None]
        if wrinkle:
            L = C.local_points(part, p)
            w = np.sin(L[:, 1] * math.pi * 7.0 + 0.6 * np.sin(L[:, 0] * 9.0))
            top = _smooth(0.0, 0.6, L[:, 2])
            band = _smooth(-0.3, 0.2, L[:, 1]) * (1 - _smooth(0.6, 0.95, L[:, 1]))
            disp = disp + 0.0035 * k * wrinkle * w * top * band
            col = col * (1.0 - 0.25 * wrinkle * top * band * _smooth(0.0, -0.8, w))[:, None]
        if scars:
            near = np.full(len(idx), 9.0)
            for a, b, _ in scars:
                near = np.minimum(near, C.segment_distance(p, np.array(a), np.array(b)))
            width = 0.006 * k
            ridge = 1 - _smooth(0.0, width, near)
            rim = (1 - _smooth(width, width * 2.2, near)) * (1 - ridge)
            col = C.lerp3(col, pal.membrane, ridge * 0.85)
            col = col * (1 - 0.3 * rim)[:, None]
            disp = disp + 0.0018 * k * ridge
            emit = np.maximum(emit, ridge * glow * 0.6)
        s.albedo[idx] = col
        s.disp[idx] = disp
        s.rough[idx] = 0.84 + 0.08 * (0.5 + 0.5 * streak)
        if glow:
            s.emit[idx] = pal.glow[None, :] * np.clip(emit, 0, 1)[:, None]
        H.family(s, idx, membrane=0.75, chitin=0.25, flow=p @ np.asarray(flow) / np.linalg.norm(flow))
    return style


def quill(pal, k, root, tip, glow=0.0, rough=0.62):
    """A lock of quill-fur: dark root, near-black body, a rust or pale tip; fine grooves along it; matte."""
    root, tip = np.array(root), np.array(tip)

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.lerp3(root, pal.dark, _smooth(0.0, 0.35, t))
        col = C.lerp3(col, tip, _smooth(0.62, 1.0, t))
        shade = 0.6 + 0.4 * _smooth(-0.6, 0.7, n[:, 2])
        axis = part.axis[-1] - part.axis[0]
        groove = _anisotropic(s.p[idx], axis if np.linalg.norm(axis) > 1e-6 else (0, 1, 0), 300.0 / k, int(part.pid) % 17)
        col = col * (shade * (0.85 + 0.2 * groove))[:, None]
        s.albedo[idx] = col
        s.disp[idx] = 0.0006 * k * groove
        s.rough[idx] = rough + 0.1 * (0.5 + 0.5 * groove)
        if glow:
            s.emit[idx] = pal.glow[None, :] * (_smooth(0.82, 1.0, t) * glow)[:, None]
        H.family(s, idx, chitin=1.0, flow=t * C.axis_length(part))
    return style


def bone(pal, k, seed=0, dark_root=0.6, glow=0.0):
    """Pale, cracked bone pushing out through the hide: darker, hide-stained at the root, pale and worn at the tip."""
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        if part.kind in ("tube", "shard"):
            t = C.tube_param(part, p)[0]
        else:
            L = C.local_points(part, p)
            t = _smooth(-0.3, 0.9, L[:, 2] / np.maximum(np.linalg.norm(L, axis=1), 1e-6))
        crack = 1 - _smooth(0.0, 0.06, np.abs(C.value_noise(p, 70.0 / k, seed)))
        col = C.lerp3(pal.side * 0.75, pal.bone * 0.8, _smooth(0.35, 1.0, t) * (1.0 - 0.8 * dark_root))
        col = col * (0.85 + 0.15 * _smooth(-0.5, 0.8, n[:, 2]))[:, None] * (0.9 + 0.1 * C.value_noise(p, 30.0 / k, seed + 1))[:, None]
        col = C.lerp3(col, pal.seam, crack * 0.7)
        s.albedo[idx] = col
        s.disp[idx] = -0.001 * k * crack
        s.rough[idx] = 0.62 + 0.15 * crack
        if glow:
            s.emit[idx] = pal.glow[None, :] * (_smooth(0.85, 1.0, t) * glow)[:, None]
        H.family(s, idx, chitin=1.0, flow=t)
    return style


def flesh(pal, k, strength=0.35, gum=None):
    """Mouth flesh: dark wet gums and throat with a dim glow deep inside."""
    gum = np.array(gum if gum is not None else pal.membrane)

    def style(s, idx, part):
        p = s.p[idx]
        v = 0.75 + 0.25 * C.value_noise(p, 90.0 / k, 3)
        s.albedo[idx] = gum[None, :] * v[:, None]
        s.emit[idx] = pal.glow[None, :] * strength * (0.6 + 0.4 * v)[:, None]
        s.rough[idx] = 0.45
        H.family(s, idx, membrane=1.0, flow=p[:, 2])
    return style


def paw_pad(pal, k):
    def style(s, idx, part):
        p, n = s.p[idx], s.n[idx]
        col = C.lerp3(pal.seam * 1.6, pal.side * 0.6, _smooth(-0.6, 0.6, n[:, 2]))
        s.albedo[idx] = col * (0.85 + 0.15 * C.value_noise(p, 120.0 / k, 5))[:, None]
        s.disp[idx] = 0.0006 * k * C.value_noise(p, 220.0 / k, 6)
        s.rough[idx] = 0.82
        H.family(s, idx, membrane=1.0, flow=p[:, 2])
    return style


# ------------------------------------------------------------------------------------------------------------
# The anatomy
# ------------------------------------------------------------------------------------------------------------

class Wolf:
    """The anatomy at scale `s` (1 = Prowler). `alpha`: the pack leader (heavier front, a crest of long black spikes
    along the back, a collar of spikes on the Mane bones, bone horns, more scars)."""

    def __init__(self, s=1.0, alpha=False):
        self.s = s
        self.alpha = alpha
        self.rng = random.Random(49 + (7 if alpha else 0))

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
        return [self.P(0.045 * sx, 0.55, 0.5), self.P(0.032 * sx, 0.516, 0.58), self.P(0.02 * sx, 0.5, 0.655),
                self.P(0.007 * sx, 0.496, 0.705)]

    def tail_points(self):
        return [self.P(0, up, f) for up, f in TAIL]

    def mane_root(self, sx):
        return self.P(0.05 * sx, 0.66, 0.33)

    def eye_points(self):
        """(centre, radius) of the two eyes, right side (the Face part mirrors them)."""
        return [(self.P(*EYES[0]), self.R(0.019)), (self.P(*EYES[1]), self.R(0.014))]

    def upper_teeth(self, sx):
        """The upper row along the gum line under the snarling lip: (base, direction, length, radius, lod); a long
        canine near the front, carnassials behind it, small incisors at the tip. Prowler units scaled by `s`."""
        out = []
        for f, x, length, rad, lod in ((0.712, 0.01, 0.016, 0.005, False), (0.68, 0.026, 0.05, 0.0095, True),
                                       (0.638, 0.031, 0.026, 0.0072, True), (0.597, 0.035, 0.024, 0.0068, False),
                                       (0.556, 0.039, 0.02, 0.006, False)):
            up = GUM_UP - 0.01 * (f - 0.52) / 0.19
            base = self.P(x * sx, up - 0.004, f)
            d = Vector((-0.12 * sx, -0.18, -1.0)).normalized()
            out.append((base, d, self.R(length), self.R(rad), lod))
        return out

    def lower_teeth(self, sx):
        """The lower fangs standing on one mandible half (they open with the split jaw)."""
        crv = C.catmull_rom([Vector(p) for p in self.jaw_points(sx)], 10)
        out = []
        for t, length, rad, lod in ((0.86, 0.036, 0.0085, True), (0.68, 0.02, 0.0058, False), (0.52, 0.018, 0.0052, False),
                                    (0.36, 0.016, 0.0046, False)):
            c = crv[int(t * (len(crv) - 1))]
            base = c + Vector((-sx * self.R(0.004), 0, self.R(0.011)))
            d = Vector((-0.1 * sx, -0.12, 0.95)).normalized()
            out.append((base, d, self.R(length), self.R(rad), lod))
        return out

    def scar_cuts(self):
        """Healed claw slashes (Blender space segments + an outward normal): across the flanks and a shoulder."""
        cuts = []
        P = self.P
        sets = [((0.098, 0.56, 0.12), (0.08, 0.42, 0.0)), ((0.096, 0.55, 0.06), (0.078, 0.43, -0.05)),
                ((0.085, 0.6, 0.22), (0.1, 0.48, 0.14))]
        if self.alpha:
            sets += [((0.094, 0.57, -0.02), (0.07, 0.45, -0.12)), ((0.05, 0.64, 0.05), (0.075, 0.58, -0.08)),
                     ((0.04, 0.62, 0.53), (0.062, 0.575, 0.6))]
        for sx in (-1, 1):
            for j, (a, b) in enumerate(sets):
                if sx < 0 and j == 2:
                    continue  # asymmetric: the shoulder scar is on one side only
                cuts.append((P(a[0] * sx, a[1], a[2]), P(b[0] * sx, b[1], b[2]), Vector((sx, 0, 0.3))))
        return cuts

    # -- fur ------------------------------------------------------------------------------------------------------
    def lock(self, b, root, d, length, width, chain, style, droop=0.35, name="Lock"):
        """One tapered, bent lock of quill-fur from `root` along `d`, sagging by `droop`."""
        d = Vector(d).normalized()
        down = Vector((0, 0, -1))
        rng = self.rng
        bend = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.18
        pts = [root, root + d * length * 0.35 + down * length * droop * 0.04,
               root + (d + bend) * length * 0.7 + down * length * droop * 0.18,
               root + (d + bend * 1.6) * length + down * length * droop * 0.42]
        b.tube(name, pts, [width, width * 0.8, width * 0.45, self.R(0.0012)], sides=6, chain=chain, style=style,
               flatten=0.42, cap=0.4)

    def clump(self, b, root, normal, flow, count, length, width, chain, style, spread=0.55, droop=0.35, name="Lock"):
        """A clump of locks fanned round `flow`, roots sunk into the skin under `root`."""
        normal = Vector(normal).normalized()
        flow = Vector(flow).normalized()
        side = normal.cross(flow)
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        rng = self.rng
        for i in range(count):
            a = ((i / (count - 1)) - 0.5) * spread if count > 1 else 0.0
            lift = rng.uniform(0.12, 0.42)
            d = (flow + side * (a + rng.uniform(-0.12, 0.12)) + normal * lift).normalized()
            r0 = root - normal * width * 0.9 + side * a * width * 2.2 + flow * rng.uniform(-1, 1) * width
            self.lock(b, r0, d, length * rng.uniform(0.72, 1.25), width * rng.uniform(0.85, 1.15), chain, style,
                      droop=droop, name=name)

    # -- parts ---------------------------------------------------------------------------------------------------
    def build_parts(self, pal, legp, k):
        b = C.PartBuilder()
        P, R, a = self.P, self.R, self.alpha
        wide = 1.22 if a else 1.0
        scars = self.scar_cuts()
        body_flow = (0.0, 1.0, -0.3)
        coat = hide(pal, k, seed=3, flow=body_flow, stripes=1.0, glow=0.22, scars=scars, stripe_freq=17.0)
        ribs = hide(pal, k, seed=4, flow=(0.0, 0.5, -1.0), stripes=1.0, glow=0.22, scars=scars, ribs=1.0, stripe_freq=17.0)
        bone_st = bone(pal, k, seed=7, dark_root=0.85)

        # Rib cage: deep and narrow (a keel), the ribs show; the brisket hangs low between the forelegs.
        def keel(v):
            return Vector((v.x * (1 - 0.45 * max(0.0, -v.z)), v.y, v.z))
        b.ellipsoid("Ribs", P(0, 0.455, 0.09), R(0.086 * wide, 0.17, 0.15), Matrix.Rotation(math.radians(-6), 3, "X"),
                    bone="Chest", style=ribs, segments=(48, 24), deform=keel)
        b.ellipsoid("Brisket", P(0, 0.36, 0.19), R(0.042 * wide, 0.075, 0.06), bone="Chest", style=coat, segments=(24, 12))
        # The back: a long ridge of muscle along the spine, withers high; the waist tucked right up under it.
        b.tube("Back", [P(0, 0.6, 0.24), P(0, 0.6, 0.08), P(0, 0.585, -0.12), P(0, 0.575, -0.3)],
               [R(0.05 * wide), R(0.045), R(0.038), R(0.05)], sides=14, chain=["Chest", "Pelvis"], style=coat)
        b.ellipsoid("Waist", P(0, 0.535, -0.13), R(0.048 * wide, 0.13, 0.058), bone="Pelvis", style=ribs, segments=(32, 16))
        b.parts[-1].chain = ["Pelvis", "Chest"]
        b.ellipsoid("Croup", P(0, 0.535, -0.29), R(0.08, 0.13, 0.068), Matrix.Rotation(math.radians(12), 3, "X"),
                    bone="Pelvis", style=coat, segments=(32, 16), deform=sinew(31, amp=0.05))
        for sx in (-1, 1):
            side = "L" if sx < 0 else "R"
            # Shoulder blade under the skin, its spine a bony ridge; hip bones and sitting bones standing out.
            b.ellipsoid("Scapula", P(0.068 * wide * sx, 0.545, 0.17), R(0.026, 0.075, 0.1),
                        Matrix.Rotation(math.radians(-24), 3, "X") @ Matrix.Rotation(math.radians(-8 * sx), 3, "Y"),
                        bone="Chest", style=coat, segments=(24, 12))
            b.tube("ScapulaSpine", [P(0.078 * wide * sx, 0.62, 0.128), P(0.085 * wide * sx, 0.56, 0.16),
                                    P(0.083 * wide * sx, 0.49, 0.195)], [R(0.006), R(0.008), R(0.004)], sides=8,
                   chain=["Chest"], style=bone_st)
            b.ellipsoid("Ilium", P(0.048 * sx, 0.585, -0.235), R(0.016, 0.03, 0.012), bone="Pelvis", style=bone_st,
                        segments=(16, 8))
        for i in range(5):
            f = -0.02 - 0.045 * i
            b.ellipsoid("Vertebra", P(0, 0.612 - 0.004 * i, f), R(0.01, 0.014, 0.012), bone="Pelvis" if f < -0.07 else "Chest",
                        style=bone(pal, k, seed=20 + i, dark_root=0.3), segments=(12, 6))
        # Neck: thick and muscular, low; the skull hangs at shoulder height.
        b.tube("Neck", [P(0, 0.585, 0.2), P(0, 0.6, 0.32), P(0, 0.6, 0.42), P(0, 0.595, 0.47)],
               [R(0.082 * wide), R(0.074 * wide), R(0.062), R(0.05)], sides=16, chain=["Neck"],
               style=hide(pal, k, seed=5, flow=(0.0, 1.0, 0.2), stripes=0.6, glow=0.15, scars=scars, stripe_freq=17.0))
        b.ellipsoid("Throat", P(0, 0.535, 0.37), R(0.048 * wide, 0.08, 0.05), bone="Neck", style=coat, segments=(24, 12))
        self._head(b, pal, k)
        self._legs(b, legp, pal, k)
        self._tail(b, pal, k)
        self._fur(b, pal, k)
        if a:
            self._alpha(b, pal, k)
        return b

    def _head(self, b, pal, k):
        P, R, a = self.P, self.R, self.alpha
        bone_st = bone(pal, k, seed=11)
        skin = hide(pal, k, seed=12, flow=(0.0, 1.0, 0.1), stripes=0.0, scars=self.scar_cuts(), glow=0.3, belly=0.25)
        (cx, cu, cf), (rx, rf, ru) = CRANIUM
        b.ellipsoid("Cranium", P(cx, cu, cf), R(rx, rf, ru), Matrix.Rotation(math.radians(8), 3, "X"), bone="Head",
                    style=skin, segments=(36, 18))
        # A long muzzle that narrows and drops to the nose, its top wrinkled in a snarl.
        (mx, mu, mf), (mrx, mrf, mru) = MUZZLE

        def taper(v):
            # A blunt wolf muzzle: it narrows, but stays deep to the nose (no beak).
            front = max(0.0, -v.y)
            return Vector((v.x * (1 - 0.32 * front), v.y, v.z * (1 - 0.18 * front) - 0.1 * front * front))
        b.ellipsoid("Muzzle", P(mx, mu, mf), R(mrx, mrf, mru), Matrix.Rotation(math.radians(9), 3, "X"), bone="Head",
                    deform=taper, segments=(36, 18),
                    style=hide(pal, k, seed=13, flow=(0.0, 1.0, 0.0), belly=0.3, wrinkle=1.0))
        b.ellipsoid("Nose", P(*NOSE), R(0.016, 0.013, 0.013), bone="Head", style=paw_pad(pal, k), segments=(16, 8))
        # The skull's bones under the skin: a sagittal crest, heavy brows, flared cheek bones.
        b.tube("Crest", [P(0, 0.665, 0.43), P(0, 0.672, 0.48), P(0, 0.655, 0.55)], [R(0.008), R(0.011), R(0.005)],
               sides=8, chain=["Head"], style=bone_st)
        for sx in (-1, 1):
            # A heavy brow: a flattened ridge sunk into the skull over each eye pair (no separate rolls).
            b.ellipsoid("Brow", P(0.043 * sx, 0.638, 0.56), R(0.03, 0.05, 0.013),
                        Matrix.Rotation(math.radians(-14 * sx), 3, "Z") @ Matrix.Rotation(math.radians(-10), 3, "X")
                        @ Matrix.Rotation(math.radians(18 * sx), 3, "Y"), bone="Head", segments=(20, 10),
                        deform=sinew(70 + sx, amp=0.06), style=skin)
            b.ellipsoid("Jowl", P(0.058 * sx, 0.55, 0.49), R(0.03, 0.05, 0.036), bone="Head", style=skin, segments=(20, 10))
            # The snarl: the upper lip curls up off the gums along the muzzle (a fleshy rim), the gums show below it.
            lip = [P(0.052 * sx, 0.548, 0.5), P(0.047 * sx, 0.553, 0.565), P(0.038 * sx, 0.549, 0.63),
                   P(0.025 * sx, 0.538, 0.685), P(0.012 * sx, 0.532, 0.718)]
            b.tube("Lip", lip, [R(0.008), R(0.009), R(0.008), R(0.007), R(0.005)], sides=8, chain=["Head"],
                   style=hide(pal, k, seed=18, flow=(0.0, 1.0, 0.0), belly=0.0, fur=0.4))
            gum = [P(0.043 * sx, GUM_UP, 0.52), P(0.039 * sx, GUM_UP - 0.004, 0.585), P(0.031 * sx, GUM_UP - 0.008, 0.65),
                   P(0.016 * sx, GUM_UP - 0.01, 0.708)]
            b.tube("Gum", gum, [R(0.011), R(0.011), R(0.009), R(0.007)], sides=8, chain=["Head"],
                   style=flesh(pal, k, strength=0.08, gum=GUM))
            # Ears laid flat back (a predator about to strike), ragged; a smaller second pair behind (alien).
            for root, mid, tip, r0, w in (((0.044, 0.655, 0.47), (0.064, 0.705, 0.42), (0.08, 0.74, 0.35), 0.036, 0.3),
                                          ((0.062, 0.64, 0.44), (0.088, 0.665, 0.4), (0.11, 0.68, 0.35), 0.02, 0.35)):
                b.tube("Ear", [P(root[0] * sx, root[1], root[2]), P(mid[0] * sx, mid[1], mid[2]),
                               P(tip[0] * sx, tip[1], tip[2])], [R(r0), R(r0 * 0.75), R(0.002)], sides=8,
                       chain=["Head"], style=quill(pal, k, pal.side, pal.membrane * 0.6), flatten=w)
            # The split lower jaw: each mandible half on its own bone, gums along its top edge.
            b.tube("Jaw", self.jaw_points(sx), [R(0.022), R(0.017), R(0.012), R(0.007)], sides=10,
                   chain=[f"Jaw_{'L' if sx < 0 else 'R'}"], flatten=0.75,
                   style=hide(pal, k, seed=16, flow=(0.0, 1.0, 0.0), belly=0.6))
        b.ellipsoid("Mouth", P(0, 0.522, 0.595), R(0.024, 0.07, 0.016), bone="Head", style=flesh(pal, k, strength=0.5, gum=GUM),
                    segments=(20, 10))
        if a:
            # The leader's horns: two pale bone hooks swept back from the brow, scored and cracked.
            for sx in (-1, 1):
                h = P(0.05 * sx, 0.66, 0.53)
                b.tube("Horn", [h, h + P(0.03 * sx, 0.045, -0.05), h + P(0.06 * sx, 0.06, -0.15), h + P(0.07 * sx, 0.04, -0.21)],
                       [R(0.017), R(0.013), R(0.008), R(0.0015)], sides=10, chain=["Head"],
                       style=bone(pal, k, seed=17, dark_root=0.5))

    def _legs(self, b, legp, pal, k):
        R, a = self.R, self.alpha
        bone_st = bone(pal, k, seed=30, dark_root=0.45)
        for (side, kind), lg in self.legs().items():
            hip, knee, ankle, toe = lg.rest_points
            sx = -1 if side == "L" else 1
            heavy = (1.35 if kind == "F" else 1.2) if a else 1.0
            n0, n1, n2 = lg.names
            upper_d, lower_d = knee - hip, ankle - knee
            back = Vector((0, 1, 0))       # Blender +Y is the animal's back (rear)
            out = Vector((sx, 0, 0))
            leg_flow = (0.0, 0.25, -1.0)
            skin = hide(legp, k, seed=40 + (kind == "H") * 3 + (sx > 0), flow=leg_flow, belly=0.2,
                        scars=self.scar_cuts() if a else ())
            if kind == "F":
                # Upper arm: one muscled limb that swells behind the humerus (the triceps) and tapers to the elbow;
                # one continuous profile, so no stacked balls or rings.
                b.tube("UpperArm", [hip + Vector((0, 0, R(0.015))), hip.lerp(knee, 0.35) + back * R(0.014),
                                    hip.lerp(knee, 0.72) + back * R(0.016), knee],
                       [R(0.042 * heavy), R(0.036 * heavy), R(0.027 * heavy), R(0.019)], sides=14, chain=[n0], style=skin)
                # The elbow's bony spur sticks out backwards.
                b.tube("Elbow", [knee - back * R(0.005), knee + back * R(0.028) + Vector((0, 0, R(0.012))),
                                 knee + back * R(0.052) + Vector((0, 0, R(0.03)))], [R(0.015), R(0.01), R(0.0015)],
                       sides=8, chain=[n0], style=bone_st)
                # Forearm: a muscle belly high up, thinning to sinew and tendon cords above the wrist.
                b.tube("Forearm", [knee, knee.lerp(ankle, 0.25) + back * R(0.004), knee.lerp(ankle, 0.62), ankle],
                       [R(0.02 * heavy), R(0.022 * heavy), R(0.014), R(0.012)], sides=12, chain=[n1], style=skin)
                for off in (-0.007, 0.007):
                    o = Vector((off * self.s, 0, 0)) + back * R(0.011)
                    b.tube("Tendon", [knee.lerp(ankle, 0.35) + o, knee.lerp(ankle, 0.7) + o * 0.9, ankle + o * 0.6],
                           [R(0.006), R(0.005), R(0.0045)], sides=6, chain=[n1], style=skin)
            else:
                # Thigh: one mass from the hip bone down (the haunch grows out of the croup), heavy behind the femur
                # (the hamstrings), tapering into the stifle; a small knee cap.
                b.tube("Thigh", [hip + Vector((0, 0, R(0.03))) - out * R(0.012), hip.lerp(knee, 0.3) + back * R(0.022),
                                 hip.lerp(knee, 0.68) + back * R(0.016), knee],
                       [R(0.062 * heavy), R(0.056 * heavy), R(0.04 * heavy), R(0.021)], sides=16, chain=[n0], style=skin)
                b.parts[-1].chain = [n0, "Pelvis"]
                b.ellipsoid("Kneecap", knee - back * R(0.009), (R(0.011),) * 3, bone=n0, style=bone_st, segments=(14, 7))
                # Shank: the gaskin swells high behind the tibia, an Achilles cord runs down to the hock.
                b.tube("Shank", [knee, knee.lerp(ankle, 0.25) + back * R(0.012), knee.lerp(ankle, 0.62) + back * R(0.005),
                                 ankle], [R(0.021 * heavy), R(0.024 * heavy), R(0.015), R(0.013)], sides=12, chain=[n1],
                       style=skin)
                b.tube("Achilles", [knee.lerp(ankle, 0.35) + back * R(0.026), knee.lerp(ankle, 0.7) + back * R(0.021),
                                    ankle + back * R(0.013)], [R(0.007), R(0.006), R(0.007)], sides=6, chain=[n1], style=skin)
                # The hock's spur: a hooked bone blade pointing back and up (the alien wolf's mark).
                b.tube("HockSpur", [ankle, ankle + back * R(0.03) + Vector((0, 0, R(0.02))),
                                    ankle + back * R(0.055) + Vector((0, 0, R(0.055))),
                                    ankle + back * R(0.06) + Vector((0, 0, R(0.085)))],
                       [R(0.015), R(0.011), R(0.006), R(0.0012)], sides=8, chain=[n1], style=bone_st)
            # Pastern / metatarsus: lean and sinewy, a knuckled bone knob at the wrist or hock.
            b.ellipsoid("Knuckle", ankle + back * R(0.003), (R(0.0115 * heavy ** 0.5),) * 3, bone=n1, style=skin,
                        segments=(14, 7))
            b.parts[-1].chain = [n1, n2]
            b.tube("Pastern", [ankle, ankle.lerp(toe, 0.5), toe + Vector((0, 0, R(0.014)))],
                   [R(0.016 * heavy ** 0.5), R(0.014), R(0.018)], sides=10, chain=[n2], style=skin)
            # Ragged fur feathering down the back of the forearm / shank.
            self.clump(b, knee.lerp(ankle, 0.4) + back * R(0.018), back + out * 0.3, Vector((0, 0.5, -1.0)), 1, R(0.06),
                       R(0.009), [n1], quill(pal, k, pal.top * 0.9, pal.membrane * 0.8), spread=0.5, droop=0.5)
            self._paw(b, legp, pal, k, toe, n2, sx, kind, heavy)

    def _paw(self, b, legp, pal, k, toe, bone_name, sx, kind, heavy):
        """A big splayed paw: a pad under a knuckled foot, four thick toes spread wide (the talons are the Claws)."""
        R = self.R
        fwd = G(0, 0, 1)
        h = heavy ** 0.5
        b.ellipsoid("Paw", toe + fwd * R(0.02) + Vector((0, 0, R(0.016))), R(0.032 * h, 0.042, 0.018), bone=bone_name,
                    style=hide(legp, k, seed=50, flow=(0.0, -1.0, 0.0), belly=0.0), segments=(20, 10))
        b.ellipsoid("Pad", toe + fwd * R(0.012) + Vector((0, 0, R(0.006))), R(0.026 * h, 0.03, 0.008), bone=bone_name,
                    style=paw_pad(pal, k), segments=(16, 8))
        for j, ang in enumerate((-0.5, -0.17, 0.17, 0.5)):
            d = G(math.sin(ang) * 0.6, 0, 1).normalized()
            base = toe + fwd * R(0.02) + Vector((0, 0, R(0.014)))
            knuckle = base + d * R(0.032) + Vector((0, 0, R(0.008)))
            b.tube("Toe", [base, knuckle, base + d * R(0.06) - Vector((0, 0, R(0.008)))],
                   [R(0.013 * h), R(0.012 * h), R(0.008)], sides=8, chain=[bone_name],
                   style=hide(legp, k, seed=51 + j, flow=(0.0, -1.0, 0.0), belly=0.0))
        if kind == "F":
            b.tube("Dewclaw", [toe + Vector((0, 0, R(0.05))), toe + Vector((0, 0, R(0.04))) + G(0.012 * sx, 0, -0.02),
                               toe + Vector((0, 0, R(0.02))) + G(0.016 * sx, 0, -0.035)], [R(0.008), R(0.006), R(0.0012)],
                   sides=6, chain=[bone_name], style=bone(pal, k, seed=52, dark_root=0.4))

    def _tail(self, b, pal, k):
        """A drooping, ragged brush: a thin core under clumps of long locks."""
        R = self.R
        pts = self.tail_points()
        names = ["Tail_1", "Tail_2", "Tail_3"]
        b.tube("Tail", pts, [R(0.032), R(0.03), R(0.022), R(0.012)], sides=10, chain=names,
               style=hide(pal, k, seed=60, flow=(0.0, 1.0, -0.6), belly=0.2))
        lock = quill(pal, k, pal.side * 0.8, pal.membrane * 0.75)
        curve = C.catmull_rom([Vector(p) for p in pts], 8)
        n = len(curve)
        for i in range(1, 7):
            t = i / 7.0
            j = int(t * (n - 1))
            c = curve[j]
            tangent = (curve[min(n - 1, j + 1)] - curve[max(0, j - 1)]).normalized()
            bone_name = names[min(2, int(t * 3))]
            for ang in (-1.9, -0.95, 0.0, 0.95, 1.9):
                if (i + int(ang * 2)) % 3 == 0:
                    continue
                around = Vector((math.sin(ang), 0, math.cos(ang)))
                around = (around - tangent * around.dot(tangent)).normalized()
                self.clump(b, c + around * R(0.02), around, tangent * 0.85 + around * 0.3, 2, R(0.1 * (1 - 0.25 * t)),
                           R(0.011), [bone_name], lock, spread=0.5, droop=0.5)
        self.clump(b, pts[-1], (pts[-1] - pts[-2]).normalized(), (pts[-1] - pts[-2]).normalized(), 3, R(0.08), R(0.012),
                   ["Tail_3"], lock, spread=0.7, droop=0.4)

    def _fur(self, b, pal, k):
        """Shaggy quill-fur: a hanging ruff under the neck, a mane down the neck and spine, feathering behind the
        elbows and on the haunches, tufts at the cheeks."""
        P, R, a = self.P, self.R, self.alpha
        dark = quill(pal, k, pal.top * 0.9, pal.membrane * 0.8)
        back = G(0, 0, -1)
        down = Vector((0, 0, -1))
        L = 1.3 if a else 1.0
        # Mane: from behind the ears down the neck to the withers, then shorter along the spine.
        for i in range(9):
            f = 0.44 - i * 0.06
            up = 0.66 if f > 0.2 else 0.66 - (0.2 - f) * 0.15
            length = (0.11 if f > 0.1 else 0.085 - 0.01 * (0.1 - f) * 5) * L
            bone_name = "Neck" if f > 0.26 else ("Chest" if f > -0.05 else "Pelvis")
            for sx in (-1, 1):
                root = P(0.016 * sx, up, f)
                normal = (Vector((0, 0, 1)) + Vector((0.5 * sx, 0, 0))).normalized()
                self.clump(b, root, normal, back * 0.8 + Vector((0.35 * sx, 0, 0.1)), 2, R(length), R(0.012), [bone_name],
                           dark, spread=0.5, droop=0.3)
        # The hanging ruff under the throat and on the sides of the neck.
        for i, (f, up, xs) in enumerate(((0.42, 0.53, (0.0, 0.035)), (0.36, 0.52, (0.0, 0.045)), (0.3, 0.53, (0.02, 0.055)),
                                         (0.24, 0.55, (0.03, 0.065)))):
            for x in xs:
                for sx in ((1, -1) if x else (1,)):
                    root = P(x * sx, up, f)
                    normal = (down + Vector((0.6 * sx * (x > 0), 0, 0))).normalized()
                    self.clump(b, root, normal, down * 0.85 + back * 0.4 + Vector((0.3 * sx * (x > 0), 0, 0)), 3,
                               R(0.1 * L), R(0.012), ["Neck"], dark, spread=0.6, droop=0.6)
        for sx in (-1, 1):
            # Cheek tufts swept back from the jowls.
            self.clump(b, P(0.07 * sx, 0.56, 0.47), Vector((sx, 0, -0.2)), back + Vector((0.5 * sx, 0, -0.2)), 3, R(0.06 * L),
                       R(0.009), ["Head"], dark, spread=0.6, droop=0.3)
            # Feathering behind the elbows and on the back of the haunches.
            for kind, (f, up), bone_name in (("F", (0.13, 0.36), "Leg_{}F_Upper"), ("H", (-0.37, 0.44), "Leg_{}H_Upper")):
                root = P(0.08 * sx, up, f)
                self.clump(b, root, back + Vector((0.4 * sx, 0, 0)), down * 0.8 + back * 0.6, 3, R(0.08 * L), R(0.011),
                           [bone_name.format("L" if sx < 0 else "R")], dark, spread=0.6, droop=0.5)
            # Tufts on the haunches, lying back.
            self.clump(b, P(0.075 * sx, 0.56, -0.26), Vector((sx, 0, 0.5)), back * 0.8 + down * 0.4 + Vector((0.25 * sx, 0, 0)),
                       2, R(0.07 * L), R(0.011), [f"Leg_{'L' if sx < 0 else 'R'}H_Upper"], dark, spread=0.6, droop=0.4)
            # Shaggy tufts over the shoulders.
            self.clump(b, P(0.085 * sx, 0.6, 0.2), Vector((sx, 0, 0.6)), back * 0.7 + down * 0.5 + Vector((0.3 * sx, 0, 0)),
                       3, R(0.08 * L), R(0.011), ["Chest"], dark, spread=0.6, droop=0.4)

    def _alpha(self, b, pal, k):
        """The leader's marks: a crest of long, near-black spikes down the neck and back (dominant, swept back,
        pale-tipped), a collar of spikes on the Mane bones that fans out on the howl, a bone chevron on the withers."""
        P, R = self.P, self.R
        spike = quill(pal, k, pal.top * 0.6, pal.bone * 0.8, glow=0.35, rough=0.55)
        back = G(0, 0, -1)
        up = Vector((0, 0, 1))
        rng = random.Random(77)
        # The crest: spikes in two staggered rows, longest over the withers, splayed a little sideways.
        for i in range(11):
            f = 0.4 - i * 0.065
            height = 0.66 if f > 0.15 else 0.64 - (0.15 - f) * 0.1
            length = (0.15 + 0.17 * math.exp(-((f - 0.18) / 0.2) ** 2)) * rng.uniform(0.85, 1.12)
            bone_name = "Neck" if f > 0.26 else ("Chest" if f > -0.05 else "Pelvis")
            # Uneven: each station leans, splays and bends its own way (a grown crest, not a comb).
            lean, splay, bend = rng.uniform(0.55, 0.95), rng.uniform(0.12, 0.5), rng.uniform(0.004, 0.02)
            for sx in (-1, 1):
                x = 0.02 * sx * (1.4 if i % 2 else 0.7)
                root = P(x, height - 0.02, f)
                d = (up * 0.62 + back * lean + Vector((splay * sx, 0, 0))).normalized()
                tip = root + d * R(length * (0.8 + 0.25 * (i % 3) / 2))
                mid = root.lerp(tip, 0.5) + back * R(bend)
                b.tube("Spike", [root, mid, tip], [R(0.02), R(0.012), R(0.0015)], sides=8, chain=[bone_name],
                       style=spike)
        for side, sx in SIDES:
            root = self.mane_root(sx)
            for j, (ang, length) in enumerate(((0.3, 0.2), (0.75, 0.24), (1.2, 0.22), (1.6, 0.17))):
                out = G(math.sin(ang) * sx, math.cos(ang), 0)
                base = root + out * R(0.03) + back * R(0.012 * j)
                d = (back * 0.7 + out * 0.6 + G(0, 0.15, 0)).normalized()
                tip = base + d * R(length)
                b.tube("Collar", [base, base.lerp(tip, 0.5) + Vector((0, 0, R(0.01))), tip], [R(0.018), R(0.011), R(0.0015)],
                       sides=8, chain=[f"Mane_{side}"], style=spike)
            b.tube("Chevron", [P(0.065 * sx, 0.64, 0.02), P(0.04 * sx, 0.655, 0.1), P(0.01 * sx, 0.665, 0.19)],
                   [R(0.016), R(0.014), R(0.008)], sides=8, chain=["Chest"], style=bone(pal, k, seed=81, dark_root=0.4))

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
