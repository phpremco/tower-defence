"""Roller: the fastest Burrower, walks and then curls into a ball and rolls (round 4, see ArtSource/DESIGN_NOTES.md).

The eight shell plates are designed as lune-shaped slices of the rolled-up ball and then opened out into the walking
dome (`plate_transform`), so the curl closes them back into an exact, stepped sphere. Each plate's rear rim stands
proud of the next and carries a hooked keel; two rows of smaller hooks run down the flanks; a glowing hub sits on each
flank where all plate tips meet in the ball. Under the toothed scoop of the front plate: an eyeless wedge head with
two hooked mandibles and glowing sensor pits. Front legs with three digging claws, rear legs with spikes.

Clips: Walk, Idle, Hit, Death, Curl, Roll (loop: one turn of the ball), Uncurl. Also exports Roller_LOD1.

Run: blender --background --python Tools/Blender/build_roller.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
from common import G  # noqa: E402

NAME = "Roller"
LOD_NAME = "Roller_LOD1"
K = 1.0
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("chitin", "membrane", "crystal")
GAME_SCALE = 0.7  # EnemyDefinition scale of the Roller

# Species colours from ProjectGenerator.Bestiary (sand body, amber eyes and accent), pushed apart in value so the
# plates read at ~20 px: pale sand plates, umber flanks, near-black seams and belly, amber glow.
PAL = H.Palette(top=(0.84, 0.67, 0.4), side=(0.34, 0.21, 0.09), belly=(0.06, 0.04, 0.025), seam=(0.04, 0.025, 0.014),
                dark=(0.13, 0.075, 0.035), glow=(1.0, 0.6, 0.16), eye=(1.0, 0.78, 0.3), membrane=(0.36, 0.2, 0.06),
                bone=(0.96, 0.9, 0.72))

# Shell geometry, designed in the rolled-up "ball space" (origin = ball centre, Blender axes).
N = 8                      # plates: 0 = front scoop plate, N-1 = tail plate
DELTA = 2 * math.pi / N    # angle of one plate around the ball
RB = 0.29                  # outer radius at a plate's middle
EPS = 0.02                 # each plate's rear rim stands EPS proud, its front edge EPS low (shingles)
THICK = 0.04
RING = 0.185               # radius of the bone ring (the joints) in the ball
PHI_MAX = math.radians(80)  # plates stop short of the side poles; the hubs close the rest
OVERLAP = math.radians(7)  # front edge of each plate tucked under the plate in front
WALK_TURN = math.radians(8)  # joint angle between plates when walking (DELTA when rolled)
CENTRE_UP = 0.19           # walking: height of the middle plates' side line (the shell's lower edge)
SIDES = (("L", -1), ("R", 1))

X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))


def d(theta):
    """Unit direction around the ball: theta 0 = up, positive = forward (Blender -Y)."""
    return Vector((0.0, -math.sin(theta), math.cos(theta)))


def plate_angle(k):
    return (N / 2 - k - 0.5) * DELTA


def walk_radius(turn):
    """Radius of the arc the joints sit on, for a joint angle `turn`, so neighbouring plates share their joint."""
    if turn >= DELTA - 1e-6:
        return RING
    return RING + RING * math.sin((DELTA - turn) / 2) / math.sin(turn / 2)


def _raw_transform(k, c, centre_up):
    turn = WALK_TURN + (DELTA - WALK_TURN) * c
    rw = walk_radius(turn)
    alpha = (N / 2 - k - 0.5) * turn
    centre = Vector((0.0, 0.0, centre_up))
    return Matrix.Translation(centre + d(alpha) * (rw - RING)) @ Matrix.Rotation(alpha - plate_angle(k), 4, "X")


def _lowest(c, centre_up=0.0):
    """Lowest shell point at curl c with the arc centre at `centre_up` (sampled on the plate outlines)."""
    low = 1e9
    for k in range(N):
        m = _raw_transform(k, c, centre_up)
        for f in (0.0, 0.5, 1.0):
            for phi in np.linspace(-PHI_MAX, PHI_MAX, 9):
                p = m @ plate_point(k, f, phi, 0.0)
                low = min(low, p.z)
    return low


_H0 = None


def arc_centre(c):
    """Height of the arc centre: walking it holds the side line at CENTRE_UP, rolled the ball rests on the ground."""
    global _H0
    if _H0 is None:
        turn = WALK_TURN
        rw = walk_radius(turn)
        _H0 = CENTRE_UP - (rw - RING) * math.cos(0.5 * turn)
    ground = -_lowest(c)
    ground0 = -_lowest(0.0)
    return ground + (1 - c) * (_H0 - ground0)


def plate_transform(k, c=0.0):
    """Ball space -> armature space for plate k at curl c (0 walking, 1 rolled up)."""
    return _raw_transform(k, c, arc_centre(c))


# ------------------------------------------------------------------------------------------------------------
# Plate outlines in ball space
# ------------------------------------------------------------------------------------------------------------

def plate_radius(f):
    """Outer radius along a plate: f 0 at the front edge, 1 at the rear rim; f < 0 is the tucked front lip."""
    if f < 0:
        return RB - EPS + 0.4 * EPS * f / (OVERLAP / DELTA)
    return RB - EPS + 2 * EPS * f ** 1.4


def front_edge(k, phi):
    """Theta of a plate's front edge; the scoop plate's edge is cut into teeth."""
    theta = plate_angle(k) + DELTA / 2
    if k == 0:
        saw = (phi / PHI_MAX * 2.5 + 0.5) % 1.0
        theta += math.radians(9) * (1 - saw) * (1 - abs(phi) / PHI_MAX) ** 0.5
    return theta


def rear_edge(k, phi):
    """Theta of a plate's rear rim, cut into a saw of backward teeth (not on the tail plate, which has prongs)."""
    theta = plate_angle(k) - DELTA / 2
    if k < N - 1:
        saw = (phi / PHI_MAX * 2.0 + 0.5) % 1.0
        theta -= math.radians(6) * saw ** 1.5 * (1 - (abs(phi) / PHI_MAX) ** 2)
    return theta


def plate_point(k, f, phi, depth):
    """A point on plate k: f along it (0 front, 1 rear rim), phi across it (0 the keel), depth inward from the outer
    surface."""
    rear = rear_edge(k, phi)
    front = front_edge(k, phi) + OVERLAP
    theta = front + (rear - front) * f
    ff = (front - OVERLAP - theta) / DELTA  # 0 at the true front edge, negative on the tucked lip
    r = plate_radius(ff) - depth
    if k == N - 1:
        r += 0.012 * max(0.0, ff - 0.6) / 0.4  # the tail plate flares a little at the rim
    return Vector((math.sin(phi) * r, 0, 0)) + d(theta) * (math.cos(phi) * r)


def cover_fraction(k):
    """How far (f) plate k is covered by the rear rim of the plate in front of it in the walking pose. Everything
    before it is hidden when walking and opens up as a glowing seam when the Roller rolls."""
    if k == 0:
        return 0.0
    rim = plate_transform(k - 1, 0.0) @ plate_point(k - 1, 1.0, 0.0, 0.0)
    q = plate_transform(k, 0.0).inverted() @ rim
    theta = math.atan2(-q.y, q.z)
    return 0.5 - (theta - plate_angle(k)) / DELTA


def keel_point(k, f, phi, lift=0.0):
    p = plate_point(k, f, phi, 0.0)
    return p + p.normalized() * lift


# ------------------------------------------------------------------------------------------------------------
# Styles
# ------------------------------------------------------------------------------------------------------------

def plate_style(k):
    """Pale sand plates with a raised keel, an umber band, a pale worn rear rim and the seam: the part of the plate
    that hides under the plate in front glows amber, so walking shows a thin line behind every rim and the rolled
    ball shows wide spokes of light. Shading follows the plate (keel light, flanks dark), not the world, so it stays
    right on the spinning ball."""
    cover = cover_fraction(k)

    def style(s, idx, part):
        b = C.local_points(part, s.p[idx])
        r = np.linalg.norm(b, axis=1)
        theta = np.arctan2(-b[:, 1], b[:, 2])
        phi = np.arcsin(np.clip(b[:, 0] / np.maximum(r, 1e-6), -1, 1))
        rel = (theta - plate_angle(k) + math.pi) % (2 * math.pi) - math.pi
        f = 0.5 - rel / DELTA
        across = np.abs(phi) / PHI_MAX
        keel = 1 - C.smoothstep(0.0, 0.07, across)
        outer = C.smoothstep(-0.015, -0.002, r - np.array([plate_radius(x) for x in np.clip(f, -0.3, 1.0)]))
        col = C.lerp3(PAL.top, PAL.side, C.smoothstep(0.25, 0.95, across))
        col = C.lerp3(col, PAL.belly, C.smoothstep(0.85, 1.0, across) * 0.7)
        col = col * H.mottle(s, idx, 0.12, 50 / K, 30 + k)
        if k == 0:
            tuck = np.zeros(len(idx))
            teeth = 1 - C.smoothstep(0.0, 0.12, f)  # the scoop's worn toothed lip
            col = C.lerp3(col, PAL.bone, teeth * 0.6)
        else:
            # The hidden part: dark umber, with a glowing band along its last stretch (thin when walking, a wide seam
            # on the ball). About 38 % of each plate hides under the plate in front when walking.
            hidden = 1 - C.smoothstep(cover + 0.02, cover + 0.06, f)
            tuck = hidden * C.smoothstep(cover - 0.17, cover - 0.09, f)
            col = C.lerp3(col, PAL.dark, hidden * 0.8)
        band = np.exp(-((f - (0.45 + 0.4 * cover)) / 0.1) ** 2) * (1 - keel * 0.5)  # a dark umber band
        col = C.lerp3(col, PAL.side * 0.45, band * 0.7)
        rim = C.smoothstep(0.86, 0.97, f) * outer
        col = C.lerp3(col, PAL.bone, rim * 0.75)
        col = C.lerp3(col, PAL.top * 1.08, keel * 0.4 * (1 - tuck))
        step = C.smoothstep(0.92, 1.0, f) * (1 - outer)
        glow = np.clip(step + tuck * 0.95, 0, 1) * (1 - C.smoothstep(0.9, 1.0, across) * 0.5)
        col = C.lerp3(col, PAL.membrane, glow * 0.85)
        s.albedo[idx] = col
        s.emit[idx] = PAL.glow[None, :] * glow[:, None]
        s.disp[idx] = 0.005 * K * keel * outer - 0.002 * K * band
        s.rough[idx] = 0.66 - rim * 0.15 + glow * 0.1
        H.family(s, idx, chitin=1.0 - 0.6 * glow, membrane=0.6 * glow, flow=f * DELTA * RB)
    return style


def hub_style():
    """The flank hub: a dark boss with a hot amber ring and a pale centre stud."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        radial = np.hypot(L[:, 1], L[:, 2])
        ring = np.exp(-((radial - 0.62) / 0.12) ** 2) * C.smoothstep(0.0, 0.4, np.abs(L[:, 0]))
        stud = 1 - C.smoothstep(0.2, 0.35, radial)
        col = C.lerp3(PAL.dark, PAL.bone, stud * 0.7)
        s.albedo[idx] = C.lerp3(col, PAL.glow * 0.6, ring)
        s.emit[idx] = PAL.glow[None, :] * np.clip(ring * 1.1, 0, 1)[:, None]
        s.rough[idx] = 0.55
        s.disp[idx] = -0.003 * K * ring
        H.family(s, idx, chitin=1.0 - ring, membrane=ring, flow=radial * 0.07)
    return style


def belly_style():
    def style(s, idx, part):
        n = s.n[idx]
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        fold = np.abs(np.sin(u[:, 1] * 9.0))
        col = C.lerp3(PAL.belly, PAL.membrane * 0.5, C.smoothstep(-0.5, 0.5, -n[:, 2]) * 0.6)
        s.albedo[idx] = C.lerp3(col, PAL.seam, (1 - C.smoothstep(0.0, 0.25, fold)) * 0.8)
        s.emit[idx] = PAL.glow[None, :] * ((1 - C.smoothstep(0.0, 0.12, fold)) * 0.06)[:, None]
        s.rough[idx] = 0.5
        s.disp[idx] = -0.002 * K * (1 - C.smoothstep(0.0, 0.25, fold))
        H.family(s, idx, membrane=1.0, flow=u[:, 1] * 0.1)
    return style


# ------------------------------------------------------------------------------------------------------------
# Model
# ------------------------------------------------------------------------------------------------------------

def tf(k):
    m = plate_transform(k, 0.0)
    return lambda p: m @ Vector(p)


def head_points():
    return G(0, 0.15, 0.47), G(0, 0.11, 0.74)


def jaw_points(sx):
    return [G(0.05 * sx, 0.1, 0.68), G(0.115 * sx, 0.085, 0.78), G(0.085 * sx, 0.075, 0.88), G(0.018 * sx, 0.08, 0.93)]


LEGS = {
    # (side, pair): band, hip, foot (x, up, forward), lengths
    1: ("Band_2", (0.15, 0.15, 0.2), (0.42, 0.0, 0.34), (0.18, 0.24)),
    2: ("Band_5", (0.15, 0.15, -0.2), (0.42, 0.0, -0.32), (0.18, 0.24)),
}


def legs():
    out = {}
    for side, sx in SIDES:
        for i, (band, hip, foot, lengths) in LEGS.items():
            names = [f"Leg_{side}{i}_Femur", f"Leg_{side}{i}_Tibia"]
            out[(side, i)] = C.Limb(names, G(hip[0] * sx, hip[1], hip[2]), G(foot[0] * sx, foot[1], foot[2]), lengths,
                                    G(sx * 1.0, 0.35, 0.0))
    return out


def build_parts():
    b = C.PartBuilder()
    k = K
    for i in range(N):
        m = plate_transform(i, 0.0)
        frame = m.inverted()
        b.slab("Plate", lambda u, v, i=i: plate_point(i, u, (v * 2 - 1) * PHI_MAX, 0.0),
               lambda u, v, i=i: plate_point(i, u, (v * 2 - 1) * PHI_MAX, THICK), 18, 30, frame=frame,
               chain=[f"Band_{i}"], style=plate_style(i), transform=lambda p, m=m: m @ p)
        # Underbody: a soft segment inside each plate (the belly when walking, hidden in the ball).
        mid = plate_angle(i)
        centre = d(mid) * 0.11
        rot = m.to_3x3().normalized() @ Matrix.Rotation(mid, 3, "X")
        b.ellipsoid("Belly", m @ centre, (0.2, 0.09, 0.14), rot, bone=f"Band_{i}", style=belly_style(),
                    segments=(24, 12))
        b.parts[-1].chain = [f"Band_{i}"]
        if 1 <= i <= N - 2:
            # The keel hook and two flank hooks, leaning back along the plate: saw rings on the ball.
            for phi, size in ((0.0, 1.35), (math.radians(58), 1.0), (-math.radians(58), 1.0)):
                base = keel_point(i, 0.72, phi, -0.008)
                out = base.normalized()
                back = (keel_point(i, 1.0, phi) - keel_point(i, 0.6, phi)).normalized()
                pts = [m @ base, m @ (base + out * 0.06 * size + back * 0.01 * size),
                       m @ (base + out * 0.085 * size + back * 0.06 * size)]
                b.tube("Hook", pts, [0.026 * size, 0.014 * size, 0.0012], sides=10, chain=[f"Band_{i}"],
                       style=H.hook(PAL, k, glow=0.7, glow_from=0.8))
    # Tail prongs off the rear rim of the last plate.
    m = plate_transform(N - 1, 0.0)
    for sx in (-1, 1):
        phi = math.radians(22) * sx
        base = keel_point(N - 1, 0.92, phi, -0.01)
        back = (keel_point(N - 1, 1.0, phi) - keel_point(N - 1, 0.8, phi)).normalized()
        pts = [m @ base, m @ (base + back * 0.07 + base.normalized() * 0.01),
               m @ (base + back * 0.13 - base.normalized() * 0.03 - Vector((0.02 * sx, 0, 0)))]
        b.tube("Prong", pts, [0.024, 0.015, 0.0015], sides=10, chain=[f"Band_{N - 1}"], style=H.hook(PAL, k, glow=0.9))
    # Hubs on the flanks, on the middle plate: where every plate tip meets when it rolls.
    m = plate_transform(3, 0.0)
    for sx in (-1, 1):
        centre = Vector((sx * (RB - 0.012), 0, 0))
        b.ellipsoid("Hub", m @ centre, (0.03, 0.075, 0.075), m.to_3x3().normalized(), bone="Band_3", style=hub_style(),
                    segments=(32, 16))

    # Head under the scoop.
    def head_deform(v):
        front = max(0.0, -v.y)
        return Vector((v.x * (1 - 0.35 * front), v.y, v.z * (1 - 0.3 * front)))
    b.ellipsoid("Head", G(0, 0.14, 0.6), (0.11, 0.13, 0.07), Matrix.Rotation(math.radians(8), 3, "X"), bone="Head",
                style=H.wedge_head(PAL, k, slits=3), segments=(36, 18), deform=head_deform)
    b.ellipsoid("Throat", G(0, 0.11, 0.73), (0.028, 0.024, 0.024), bone="Head", style=H.inner_glow(PAL, 0.9))
    for side, sx in SIDES:
        b.tube("Jaw", jaw_points(sx), [0.026, 0.024, 0.016, 0.002], sides=10, chain=[f"Jaw_{side}"],
               style=H.blade(PAL, k, glow=0.8), flatten=0.55)

    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("Femur", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.012)), knee], [0.04, 0.036, 0.028], sides=10,
               chain=[lg.names[0]], style=H.leg(PAL, k, "Femur", spines=2))
        b.tube("Tibia", [knee, knee.lerp(foot, 0.5), foot], [0.028, 0.022, 0.012], sides=10, chain=[lg.names[1]],
               style=H.leg(PAL, k, "Tibia"))
        if i == 1:
            # Three hooked digging claws fanned forward.
            for a in (-0.5, 0.0, 0.5):
                fwd = G(0.35 * a * sx + 0.1 * sx, 0, 1).normalized()
                pts = [foot, foot + fwd * 0.05 + Vector((0, 0, 0.012)), foot + fwd * 0.085 - Vector((0, 0, 0.012))]
                b.tube("Claw", pts, [0.013, 0.009, 0.001], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k))
        else:
            tip = foot + (foot - knee).normalized() * 0.03
            b.tube("Spike", [foot, tip], [0.012, 0.001], sides=8, chain=[lg.names[1]], style=H.hook(PAL, k))
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.15), None, True)]
    parent = {3: "Root", 2: "Band_3", 1: "Band_2", 0: "Band_1", 4: "Band_3", 5: "Band_4", 6: "Band_5", 7: "Band_6"}
    for i in (3, 2, 1, 0, 4, 5, 6, 7):
        m = plate_transform(i, 0.0)
        head = m @ (d(plate_angle(i) + DELTA / 2) * RING)
        tail = m @ (d(plate_angle(i) - DELTA / 2) * RING)
        out.append((f"Band_{i}", head, tail, parent[i], True))
    neck, tip = head_points()
    out.append(("Head", neck, tip, "Band_0", True))
    for side, sx in SIDES:
        pts = jaw_points(sx)
        out.append((f"Jaw_{side}", pts[0], pts[-1], "Head", True))
    for (side, i), lg in legs().items():
        out += lg.rest_bones(LEGS[i][0])
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def ball_centre():
    """Armature-space centre of the rolled ball."""
    return plate_transform(3, 1.0) @ Vector((0, 0, 0))


class Pose:
    """Shared pose maths: band matrices for a curl, plus where hips and tucked feet go."""

    def __init__(self, poser):
        self.poser = poser
        self.rest_inv = {i: plate_transform(i, 0.0).inverted() for i in range(N)}
        # Tucked legs fold across the belly: the foot crosses the midline towards the ball centre and the knee bends
        # inwards, so nothing pokes out of the ball. Both live in the ball space of the hip's plate.
        self.tuck, self.tuck_pole = {}, {}
        for key, lg in legs().items():
            band = int(LEGS[key[1]][0].split("_")[1])
            hip_ball = self.rest_inv[band] @ lg.root
            sx = 1 if key[0] == "R" else -1
            foot_ball = Vector((-0.06 * sx, hip_ball.y * 0.3, hip_ball.z * 0.3))
            self.tuck[key] = plate_transform(band, 0.0) @ foot_ball
            self.tuck_pole[key] = plate_transform(band, 0.0).to_3x3() @ (-hip_ball).normalized()

    def bands(self, c, place=None, wave=None):
        place = place if place is not None else Matrix.Identity(4)
        out = {}
        for i in range(N):
            extra = wave(i) if wave else Matrix.Identity(4)
            out[i] = place @ plate_transform(i, c) @ extra @ self.rest_inv[i]
        return out

    def specs(self, c=0.0, place=None, head_pitch=0.0, head_yaw=0.0, head_pull=0.0, jaw=0.0, feet=None, wave=None,
              root=None):
        moves = self.bands(c, place, wave)
        specs = {"Root": root or {"rot": []}}
        for i, m in moves.items():
            specs[f"Band_{i}"] = {"abs": m @ self.poser.rest[f"Band_{i}"]}
        # Head and jaws are children of the scoop plate: local rotations.
        specs["Head"] = {"rot": [(X, head_pitch), (Z, head_yaw)], "loc": Vector((0, 0, 0))}
        if head_pull:
            fwd = (moves[0].to_3x3() @ (self.poser.rest["Head"].to_3x3().col[1]))
            specs["Head"]["loc"] = -fwd * head_pull
        for side, sx in SIDES:
            specs[f"Jaw_{side}"] = {"rot": [(Z, -sx * jaw)]}
        ease = C.smoothstep(0.0, 0.7, np.array([c]))[0]
        for key, lg in legs().items():
            band = int(LEGS[key[1]][0].split("_")[1])
            m = moves[band]
            hip = m @ lg.root
            ground = feet[key] if feet else lg.foot
            tucked = m @ self.tuck[key]
            target = ground.lerp(tucked, ease)
            pole = (m.to_3x3() @ lg.pole.lerp(self.tuck_pole[key], ease)).normalized()
            specs.update(lg.pose(self.poser, hip, target, pole))
        return specs


BALL_LIFT = 0.02  # the hooks stand past the shell: lift the ball a little so they only graze the ground


def roll_place(angle, lift=BALL_LIFT):
    centre = ball_centre()
    return Matrix.Translation(centre + Vector((0, 0, lift))) @ Matrix.Rotation(angle, 4, "X") @ Matrix.Translation(-centre)


def body_place(bob=0.0, roll=0.0, pitch=0.0, yaw=0.0, offset=None):
    pivot = Vector((0, 0, CENTRE_UP))
    rot = Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(roll, 4, "Y") @ Matrix.Rotation(pitch, 4, "X")
    move = Vector(offset) if offset is not None else Vector((0, 0, bob))
    return Matrix.Translation(pivot + move) @ rot @ Matrix.Translation(-pivot)


STRIDE, LIFT = 0.24, 0.07  # the fastest walker: a long stride keeps the Unity playback rate sane
TROT_A = {("L", 1), ("R", 2)}


def walk_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        w = 2 * math.pi * t
        feet = {}
        for key, lg in legs().items():
            phase = (t + (0.0 if key in TROT_A else 0.5)) % 1.0
            fwd, up = C.step_curve(phase, 0.5, STRIDE, LIFT)
            feet[key] = lg.foot + Vector((0, -fwd, up))
        place = body_place(bob=0.008 * math.cos(2 * w), roll=math.radians(3) * math.sin(w),
                           pitch=math.radians(1.5) * math.sin(2 * w), yaw=math.radians(2.5) * math.sin(w))
        # A travelling ripple through the plates, like a rib cage breathing as it runs.
        wave = lambda i: Matrix.Rotation(math.radians(1.6) * math.sin(w * 2 - i * 0.8), 4, "X")  # noqa: E731
        return P.specs(0.03 + 0.02 * math.sin(2 * w), place, head_pitch=math.radians(4) * math.sin(2 * w + 1),
                       head_yaw=math.radians(5) * math.sin(w), jaw=math.radians(8) * (0.5 + 0.5 * math.sin(2 * w)),
                       feet=feet, wave=wave)
    return 20, spec, True


def idle_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w + 0.5)) ** 4
        place = body_place(bob=0.004 * math.sin(w), roll=math.radians(1.5) * math.sin(w))
        return P.specs(0.03 + 0.025 * (0.5 + 0.5 * math.sin(w)), place,
                       head_pitch=math.radians(5) * math.sin(2 * w), head_yaw=math.radians(14) * math.sin(w),
                       jaw=math.radians(10) + math.radians(22) * snap)
    return 60, spec, True


def hit_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        k = math.sin(math.pi * min(1.0, t / 0.35)) if t < 0.35 else math.exp(-(t - 0.35) * 7) * math.cos((t - 0.35) * 14) * 0.4
        place = body_place(offset=Vector((0, 0.025 * k, 0.01 * k)), pitch=math.radians(-6) * k)
        return P.specs(0.03 + 0.25 * max(k, 0.0), place, head_pitch=math.radians(-12) * k, head_pull=0.03 * max(k, 0),
                       jaw=math.radians(30) * max(k, 0.0))
    return 12, spec, False


CURL_FRAMES = 14


def curl_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        c = C.smoothstep(0.0, 1.0, np.array([t]))[0]
        tuck = C.smoothstep(0.0, 0.6, np.array([t]))[0]
        return P.specs(0.03 + 0.97 * c, roll_place(0.0, BALL_LIFT * c), head_pitch=math.radians(70) * tuck,
                       head_pull=0.12 * tuck, jaw=math.radians(-6) * tuck)
    return CURL_FRAMES, spec, False


def uncurl_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        c = 1 - C.smoothstep(0.0, 1.0, np.array([t]))[0]
        # A little overshoot as it lands back on its legs.
        c = max(0.0, c - 0.06 * math.sin(math.pi * C.smoothstep(0.6, 1.0, np.array([t]))[0]))
        tuck = C.smoothstep(0.0, 0.85, np.array([c]))[0]
        return P.specs(0.03 + 0.97 * c, roll_place(0.0, BALL_LIFT * c), head_pitch=math.radians(70) * tuck,
                       head_pull=0.12 * tuck,
                       jaw=math.radians(-6) * tuck + math.radians(18) * C.smoothstep(0.7, 1.0, np.array([t]))[0])
    return CURL_FRAMES, spec, False


ROLL_FRAMES = 24


def roll_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        return P.specs(1.0, roll_place(2 * math.pi * t), head_pitch=math.radians(70), head_pull=0.12,
                       jaw=math.radians(-6))
    return ROLL_FRAMES, spec, True


def death_spec(poser):
    P = Pose(poser)

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.2)) if t < 0.2 else 0.0
        flop = C.smoothstep(0.12, 0.6, np.array([t]))[0]
        twitch = math.exp(-max(0.0, t - 0.6) * 6) * math.sin(t * 55) * (1 if t > 0.6 else 0)
        # Topples onto its side, half curled, legs folding in.
        pivot = Vector((0.3, 0, 0))
        place = (Matrix.Translation(pivot + Vector((0, 0, 0.03 * jerk))) @ Matrix.Rotation(math.radians(95) * flop, 4, "Y")
                 @ Matrix.Translation(-pivot))
        specs = P.specs(0.03 + 0.4 * flop, place, head_pitch=math.radians(30) * flop + math.radians(6) * twitch,
                        jaw=math.radians(35) * flop)
        for key, lg in legs().items():
            band = int(LEGS[key[1]][0].split("_")[1])
            m = P.bands(0.03 + 0.4 * flop, place)[band]
            hip = m @ lg.root
            rest_vec = lg.foot - lg.root
            curled = hip + m.to_3x3() @ (rest_vec * 0.45 + Vector((0, 0, 0.08)) + rest_vec.normalized() * 0.02 * twitch)
            foot = (place @ lg.foot).lerp(curled, flop)
            specs.update(lg.pose(poser, hip, foot, m.to_3x3() @ lg.pole))
        return specs
    return 36, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Curl": curl_spec,
         "Roll": roll_spec, "Uncurl": uncurl_spec}

# LOD1: jaws fold into the head; each leg becomes one bone aimed from hip to foot. 14 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", **{f"Leg_{s}{i}_Tibia": f"Leg_{s}{i}_Femur" for s, _ in SIDES for i in (1, 2)}}
LOD_AIMS = {f"Leg_{s}{i}_Femur": (f"Leg_{s}{i}_Femur", f"Leg_{s}{i}_Tibia") for s, _ in SIDES for i in (1, 2)}


# ------------------------------------------------------------------------------------------------------------
# Old procedural stand-in (for the compare sheet): EnemyModelFactory.Roller in primitives, flat colours
# ------------------------------------------------------------------------------------------------------------

def old_proxy():
    body, dark, eyes = (0.95, 0.8, 0.48), (0.35, 0.27, 0.16), (1.0, 0.8, 0.3)
    groups = {body: C.PartBuilder(), dark: C.PartBuilder(), eyes: C.PartBuilder()}
    g = groups[body]
    g.ellipsoid("Body", G(0, 0.22, 0), (0.2, 0.42, 0.14))
    for i in range(7):
        z = [0.4, 0.28, 0.14, 0.0, -0.14, -0.28, -0.45][i]
        g.ellipsoid("Band", G(0, 0.2, z), (0.3, 0.075, 0.24), Matrix.Rotation(math.pi / 2, 3, "Y"))
    g.ellipsoid("Head", G(0, 0.18, 0.52), (0.14, 0.16, 0.1))
    groups[dark].ellipsoid("Snout", G(0, 0.15, 0.66), (0.08, 0.08, 0.05))
    groups[dark].tube("Tail", [G(0, 0.2, -0.45), G(0, 0.16, -0.65), G(0, 0.1, -0.81)], [0.06, 0.04, 0.005])
    for sx in (-1, 1):
        groups[eyes].ellipsoid("Eye", G(0.04 * sx, 0.22, 0.64), (0.02, 0.02, 0.02))
        for z in (0.22, -0.22):
            groups[dark].tube("Leg", [G(0.16 * sx, 0.2, z), G(0.26 * sx, 0.1, z), G(0.25 * sx, 0.0, z + 0.03)], [0.03, 0.025, 0.012])
    return C.flat_proxy(groups, "OldRoller")


# ------------------------------------------------------------------------------------------------------------
# Previews
# ------------------------------------------------------------------------------------------------------------

def previews(arm, mesh, actions, lod, out, asset):
    C.preview_world()
    rig = C.light_rig(scale=1.2)
    C.ground()
    target = Vector((0, -0.05, 0.18))
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    paths = []
    close = C.closeup_camera(target, 2.3, azimuth=-38, elevation=26, lens=55)
    C.set_action(arm, actions["Idle"], 15)
    paths.append(C.render(close, os.path.join(out, "roller_close.png"), 1280, 720))
    side = C.closeup_camera(target, 2.4, azimuth=-90, elevation=8, lens=55)
    C.set_action(arm, actions["Walk"], 0)
    paths.append(C.render(side, os.path.join(out, "roller_side.png"), 1280, 720))
    ball_cam = C.closeup_camera(Vector((0, 0, 0.3)), 2.0, azimuth=-55, elevation=24, lens=55)
    C.set_action(arm, actions["Roll"], 3)
    paths.append(C.render(ball_cam, os.path.join(out, "roller_ball.png"), 1280, 720))
    top = C.closeup_camera(target, 2.4, azimuth=-160, elevation=60, lens=55)
    C.set_action(arm, actions["Walk"], 5)
    paths.append(C.render(top, os.path.join(out, "roller_top.png"), 1280, 720))
    front = C.closeup_camera(target + Vector((0, -0.2, 0)), 1.6, azimuth=-15, elevation=14, lens=55)
    C.set_action(arm, actions["Hit"], 4)
    paths.append(C.render(front, os.path.join(out, "roller_front.png"), 1280, 720))
    C.set_action(arm, actions["Death"], 36)
    paths.append(C.render(close, os.path.join(out, "roller_dead.png"), 1280, 720))

    strips = [C.strip_sheet(arm, actions["Walk"], side, out, "roller_walk", range(0, 20, 3)[:8]),
              C.strip_sheet(arm, actions["Curl"], close, out, "roller_curl", [0, 2, 4, 6, 8, 10, 12, 14]),
              C.strip_sheet(arm, actions["Roll"], side, out, "roller_roll", range(0, 24, 3))]

    C.set_action(lod_arm, lod_actions["Roll"], 3)
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(ball_cam, os.path.join(out, "roller_lod1_ball.png"), 1280, 720))
    C.set_action(lod_arm, lod_actions["Walk"], 5)
    paths.append(C.render(close, os.path.join(out, "roller_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions["Idle"], 15)
    macros = [("roller_macro_back.png", G(-0.08, 0.42, 0.02), 0.5, -60, 30),
              ("roller_macro_head.png", G(0.0, 0.16, 0.68), 0.55, -22, 18),
              ("roller_macro_hub.png", G(-0.3, 0.19, 0.0), 0.45, -95, 12)]
    for file_name, aim, dist, az, el in macros:
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, file_name), 1280, 720))
    C.set_action(arm, actions["Roll"], 3)
    paths.append(C.render(C.macro_camera(Vector((0, 0, 0.3)) + Vector((0.18, -0.2, 0.1)), 0.55, -50, 25),
                          os.path.join(out, "roller_macro_ball.png"), 1280, 720))

    walk, roll = actions["Walk"], actions["Roll"]
    layout = [(walk, 3, (-1.3, 0.6, 0), 0.2), (roll, 0, (-0.2, 0.9, 0), 0.0), (walk, 11, (0.9, 0.5, 0), -0.3),
              (roll, 9, (-0.8, -0.5, 0), 0.4), (walk, 7, (0.4, -0.6, 0), 0.1), (roll, 17, (1.4, -0.3, 0), -0.2)]
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "roller_game.png"), GAME_SCALE, layout, close_lights=rig))
    paths.append(C.game_view_mixed(arm, mesh, os.path.join(out, "roller_game_zoomed.png"), GAME_SCALE, layout, zoom=2.5,
                                   close_lights=rig))

    # Old procedural stand-in next to the new model, same cameras.
    arm.hide_render = mesh.hide_render = True
    old = old_proxy()
    olds = [C.render(close, os.path.join(out, "roller_old_close.png"), 1280, 720),
            C.game_view_static(old, os.path.join(out, "roller_old_game_zoomed.png"), GAME_SCALE, zoom=2.5, close_lights=rig)]
    C.remove_objects(old)
    arm.hide_render = mesh.hide_render = False
    C.set_action(arm, actions["Idle"], 15)
    C.game_view(arm, mesh, os.path.join(out, "roller_single_zoomed.png"), GAME_SCALE, 1, (0, 0), zoom=2.5, close_lights=rig)
    C.contact_sheet([olds[0], paths[0], olds[1], os.path.join(out, "roller_single_zoomed.png")],
                    os.path.join(out, "compare4_roller.png"), 2, 640)
    C.contact_sheet(paths + strips, os.path.join(out, "sheet_roller.png"), 2, 640)
    C.uv_layout(mesh, os.path.join(out, "roller_uv.png"), background=asset.textures["albedo"])


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts()
    asset = C.build_mesh_asset(NAME, builder, voxel=0.0036, target_tris=GAME_TRIS, tex_size=TEX, ao_distance=0.08,
                               extrusion=0.014, ray=0.045, emission_strength=2.5,
                               extra=H.weathering(K, markings=PAL.dark, marking_amount=0.3), specular=0.3,
                               mirror=True, k=K, families=FAMILIES, low_size=LOW_TEX)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid={"Plate", "Belly", "Hub", "Hook", "Prong"}, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Idle"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    centre = ball_centre()
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS},
                          "walk_distance_per_loop": round(STRIDE / 0.5, 3),
                          "ball_radius": round(RB + EPS, 3), "ball_centre_height": round(centre.z, 3),
                          "roll_distance_per_loop": round(2 * math.pi * (RB + EPS), 3),
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    if not C.cli_args().get("norender"):
        previews(arm, asset.low, actions, lod, out, asset)


if __name__ == "__main__":
    main()
