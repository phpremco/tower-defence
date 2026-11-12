"""Shardback: the ice beetle whose crust of ice prisms is its armour (round 15, see ArtSource/DESIGN_NOTES.md).

A low, wide slate-violet carapace on six stocky legs; six clusters of twisted ice prisms grow from glowing amber
sockets on the back (two on the midline, two pairs on the flanks). Each cluster is its own mesh island on its own
bone (Shard_1..6, the shedding order), rigid, so Unity hides a chipped cluster by scaling its bone to 0 and the
amber socket shows. A plough head: a wide brow plate with a frosted horn, two side-closing frost mandibles round an
amber throat (the frost spit), four amber eyes. Fixed frost crystals along the flank edges.

Clips: Walk (loop), Idle (loop), Hit, Death, Shatter (the six clusters burst off in order). Only Shatter keys the
shard bones' scale. Also exports Shardback_LOD1 (shard bones kept, rigid).

Run: blender --background --python Tools/Blender/build_shardback.py -- [--preview <dir>] [--norender]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
import creature_kit3 as K3  # noqa: E402
import crystal_growth as G49  # noqa: E402
import uvlayout  # noqa: E402
from common import G  # noqa: E402

NAME = "Shardback"
K = 1.0
GAME_SCALE = 0.8  # EnemyDefinition scale

# Data colours (ProjectGenerator.Bestiary): pale blue body, ice-white eyes and accent. The element lives in the ice;
# the body is dark slate-violet so the ice reads against it, and the glow is warm amber (alien rule).
PAL = H.Palette(top=(0.24, 0.24, 0.38), side=(0.085, 0.085, 0.15), belly=(0.03, 0.03, 0.05), seam=(0.02, 0.02, 0.035),
                dark=(0.05, 0.05, 0.09), glow=(1.0, 0.55, 0.16), eye=(1.0, 0.72, 0.3), membrane=(0.38, 0.14, 0.05),
                bone=(0.78, 0.86, 0.98))
LEGP = H.Palette(top=(0.17, 0.17, 0.28), side=(0.07, 0.07, 0.12), belly=(0.03, 0.03, 0.05), seam=(0.02, 0.02, 0.035),
                 dark=(0.04, 0.04, 0.07), glow=PAL.glow, eye=PAL.eye, bone=PAL.bone)

SIDES = K3.SIDES
X, Yv, Z = K3.X, K3.Yv, K3.Z

# The back: the elytra ellipsoid (centre in game coords, radii x / length / height).
ELY_C = (0.0, 0.29, -0.07)
ELY_R = (0.27, 0.37, 0.12)

# Clusters in shedding order: (bone, socket centre (x, fwd) right side or midline, mirrored side, prisms).
# A prism: (dx, dfwd) from the socket, lean (x, up, fwd), radius, length, twist (radians).
CLUSTERS = [
    ("Shard_1", (0.0, -0.27), None, [((0.035, 0.0), (0.22, 1.0, -0.5), 0.034, 0.23, 0.6),
                                     ((-0.035, 0.0), (-0.22, 1.0, -0.5), 0.034, 0.23, -0.6)]),
    ("Shard_2", (0.165, -0.16), "L", [((0.0, 0.0), (0.55, 1.0, -0.45), 0.036, 0.25, 0.7),
                                      ((0.04, 0.03), (1.0, 0.6, -0.05), 0.021, 0.12, -0.5),
                                      ((-0.02, -0.04), (0.25, 1.0, -0.75), 0.019, 0.11, 0.4)]),
    ("Shard_3", (0.165, -0.16), "R", None),
    ("Shard_4", (0.0, 0.0), None, [((0.042, 0.0), (0.16, 1.0, -0.45), 0.046, 0.36, 0.5),
                                   ((-0.042, 0.0), (-0.16, 1.0, -0.45), 0.046, 0.36, -0.5),
                                   ((0.07, -0.06), (0.55, 1.0, -0.35), 0.024, 0.15, -0.4),
                                   ((-0.07, -0.06), (-0.55, 1.0, -0.35), 0.024, 0.15, 0.4)]),
    ("Shard_5", (0.185, 0.1), "L", [((0.0, 0.0), (0.5, 1.0, -0.3), 0.038, 0.27, 0.6),
                                    ((0.045, 0.02), (1.0, 0.55, 0.15), 0.022, 0.13, -0.6)]),
    ("Shard_6", (0.185, 0.1), "R", None),
]
SHARD_NAMES = [c[0] for c in CLUSTERS]


def surface(x, f):
    """Height and normal of the elytra at (x, forward)."""
    cx, cu, cf = ELY_C
    rx, ry, rz = ELY_R
    q = 1 - (x / rx) ** 2 - ((f - cf) / ry) ** 2
    h = cu + rz * math.sqrt(max(q, 0.0))
    n = G(x / rx ** 2, (h - cu) / rz ** 2, (f - cf) / ry ** 2).normalized()
    return h, n


def clusters():
    """Expanded clusters: [(bone, sx, socket (game x, fwd), prisms)] with mirrored flank pairs."""
    out = []
    for name, socket, side, prisms in CLUSTERS:
        if prisms is None:
            prisms = [c for c in CLUSTERS if c[3] is not None and c[1] == socket][0][3]
        sx = -1 if side == "L" else 1
        out.append((name, sx, (socket[0] * sx, socket[1]), prisms))
    return out


def pitch(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


LEG_TABLE = [((0.16, 0.19, 0.2), (0.4, 0.0, 0.32), (0.2, 0.22)),
             ((0.185, 0.19, 0.03), (0.44, 0.0, 0.04), (0.21, 0.23)),
             ((0.17, 0.19, -0.15), (0.41, 0.0, -0.27), (0.2, 0.22))]


def legs():
    return K3.limbs(LEG_TABLE)


# Round 49D (Remco: "those circles are too perfect, it doesn't look scary enough"): the back is a faceted carapace
# with a saw-toothed rim, every socket a torn crater with jagged chitin lips (no ring collars), the clusters
# irregular crystal growth (crystal_growth.py), the front a serrated pronotum with forward hooks and a crystal ram
# horn over longer, hooked mandibles. Seeds fix every random choice, so a rebuild is identical.
CLUSTER_SIZE = {"Shard_1": 0.27, "Shard_2": 0.27, "Shard_3": 0.25, "Shard_4": 0.4, "Shard_5": 0.3, "Shard_6": 0.28}
CLUSTER_COUNT = {"Shard_1": 4, "Shard_2": 5, "Shard_3": 4, "Shard_4": 6, "Shard_5": 4, "Shard_6": 5}
SPREAD = 0.2
# The crater floor's centre sits SOCKET_SINK under the carapace line (round 49F).
SOCKET_SINK = 0.016
# Round 49G: the cluster's base sits CLUSTER_SEAT under the carapace line (inside the crater floor) and every shard
# starts CLUSTER_SINK deeper still (crystal_growth.growth_cluster sink): no gap under any shard, from any side.
CLUSTER_SEAT = 0.004
CLUSTER_SINK = 0.024
CLUSTER_PARTS = ("Prism", "PrismFragment", "PrismRoot")
MANDIBLE = [(0.095, 0.16, 0.42), (0.115, 0.152, 0.5), (0.085, 0.14, 0.575), (0.03, 0.146, 0.615)]


def mandible_points(sx):
    return [K3.gp(p, sx) for p in MANDIBLE]


def cluster_footprint(name):
    return SPREAD * CLUSTER_SIZE[name] + 0.03


def elytra_deform():
    """Faceted (a hewn polyhedron, not an egg), with an uneven saw-toothed rim raked backwards."""
    facets = C.chiselled(26, 15, depth=0.07)
    saw = G49.serrated_outline(15, 21, depth=0.24)

    def deform(v):
        u = facets(v)
        a = math.atan2(u.y, u.x)
        rim = max(0.0, 1.0 - abs(u.z) * 1.7)
        k = 1.0 + (saw(a) - 1.0) * rim
        # Only the rim moves outwards; the top stays at or under the ellipsoid, so the shard clusters keep their gap.
        return Vector((u.x * k, u.y * k, u.z))
    return deform


def plate_deform(teeth, seed, depth):
    """A plate with a jagged front edge (local -y is forward) and facets."""
    facets = C.chiselled(18, seed, depth=0.08)
    saw = G49.serrated_outline(teeth, seed + 2, depth=depth)

    def deform(v):
        u = facets(v)
        a = math.atan2(u.y, u.x)
        front = C.smoothstep(-0.2, 0.6, -u.y)
        rim = max(0.0, 1.0 - abs(u.z) * 1.5)
        k = 1.0 + (saw(a) - 1.0) * rim * (0.35 + 0.65 * front)
        return Vector((u.x * k, u.y * k, u.z))
    return deform


def torn_socket(seed):
    """A crater floor with a ragged outline (no disc)."""
    saw = G49.serrated_outline(7, seed, depth=0.4, jitter=0.9)

    def deform(v):
        k = saw(math.atan2(v.y, v.x))
        return Vector((v.x * k, v.y * k, v.z))
    return deform


def shell_parts(b, k=K):
    """The closed volumes every spike grows out of: underside, carapace, pronotum, head and brow plate."""
    b.ellipsoid("Under", G(0, 0.2, -0.03), (0.22, 0.34, 0.1), bone="Body",
                style=H.glow_flesh(PAL, k, glow=0.55, flank=(0.6, 0.95), folds=10.0), segments=(40, 20))
    b.ellipsoid("Elytra", G(*ELY_C), ELY_R, bone="Body", segments=(120, 48), deform=elytra_deform(),
                style=K2.armour_plate(PAL, k, rim_glow=0.7, chevron=0.0, lip=0.5, cells=18, seed=3))
    b.ellipsoid("Pronotum", G(0, 0.3, 0.22), (0.24, 0.13, 0.09), pitch(-10), bone="Body", segments=(96, 32),
                deform=plate_deform(11, 41, 0.34),
                style=K2.armour_plate(PAL, k, rim_glow=0.8, chevron=0.9, chevron_angle=1.1, lip=0.8, cells=10, seed=5))
    b.ellipsoid("Head", G(0, 0.2, 0.36), (0.14, 0.1, 0.08), bone="Head", style=H.band_plates(PAL, k, 2, height=0.002),
                segments=(48, 24), deform=C.chiselled(14, 23, depth=0.12))
    b.ellipsoid("Brow", G(0, 0.265, 0.39), (0.2, 0.09, 0.045), pitch(-14), bone="Head", segments=(64, 20),
                deform=plate_deform(9, 47, 0.25),
                style=K2.armour_plate(PAL, k, rim_glow=0.4, chevron=0.0, lip=1.0, cells=8, seed=7))


class Shell:
    """Round 49G (Remco: "you can see the spikes floating!!"). The 49F spikes were placed from the ideal ellipsoid
    (surface()), but the built carapace has a saw-toothed rim that recedes between its teeth and facets up to 7 % deep:
    rim spikes and flank frost started in the air beside a receding tooth or under the rim and only touched the shell
    through a thin remesh bridge (one island, so no island check could see it), and from the side they read as hovering
    thorns. Now every spike is placed on the shell as built: a ray finds where its line breaks out of the volume, the
    spike starts ROOT_DEPTH inside, and a lumpy chitin mound (crystal_growth.lumpy_root) swells round its foot, so it
    visibly grows out of the plate. `rooted` lists every spike's depth for the report."""

    ROOT_DEPTH = 0.022

    def __init__(self):
        tmp = C.PartBuilder()
        shell_parts(tmp)
        self.tree = tmp.bvh()
        self._keep = tmp
        self.rooted = []

    def exit(self, inner, d):
        """The point and outward normal where a line from `inner` along `d` leaves the shell (the outermost hit)."""
        d = Vector(d).normalized()
        loc, nrm, _, _ = self.tree.ray_cast(Vector(inner) + d * 0.5, -d, 1.0)
        if loc is None:
            return None, None
        return loc, (nrm if nrm.dot(d) > 0 else -nrm)

    def spike(self, b, kind, inner, d, radius, length, style, bone, mound_style, seed, shard=None, bend=None,
              mound=2.3):
        """A spike leaving the shell along `d` from a point `inner` inside it: `length` shows outside, ROOT_DEPTH
        (or 2.5 x radius) is buried, a mound of `mound` x radius rings the foot. `shard` (kwargs) builds a
        fractured crystal shard, else a tapered chitin hook."""
        d = Vector(d).normalized()
        p, n = self.exit(inner, d)
        if p is None:
            C.log("spike skipped (no shell)", kind, tuple(round(v, 3) for v in inner))
            return None
        depth = max(self.ROOT_DEPTH, 2.5 * radius)
        start = p - d * depth
        if shard is not None:
            G49.fractured_shard(b, kind, start, d, radius, length + depth, seed, bone=bone, chain=[bone], style=style,
                                bend=bend, **shard)
        else:
            bend = bend if bend is not None else Vector()
            b.tube(kind, [start, p + d * length * 0.5 + bend * 0.4, p + d * length + bend],
                   [radius * 1.1, radius * 0.65, 0.001], sides=8, chain=[bone], style=style)
        G49.lumpy_root(b, kind + "Root", p - n * radius * 0.45, n, radius * mound, radius * 1.05, seed + 3,
                       bone=bone, style=mound_style, lobes=5, segments=(20, 10))
        b.parts[-1].chain = [bone]
        self.rooted.append((kind, round(depth, 4), round(float(n.dot(d)), 2)))
        return p


def build_parts(b=None):
    b = b or C.PartBuilder()
    k = K
    rng = np.random.default_rng(49)
    shell = Shell()
    b.shell = shell

    def ice(seed, warm=0.85):
        return G49.uneven_glow(K3.ice_prism(k, warm_glow=warm, seed=seed), seed, 0.35, 1.25)
    spike = H.hook(PAL, k, glow=0.5, glow_from=0.8)
    mound = K2.armour_plate(PAL, k, rim_glow=0.0, chevron=0.0, lip=0.6, seed=37)
    shell_parts(b, k)
    # Hooks raked forward off the pronotum's front edge, uneven (the left inner one is the longest).
    for i, (x, ln, rake) in enumerate(((-0.16, 0.08, 0.5), (-0.06, 0.11, 0.25), (0.05, 0.085, 0.3), (0.15, 0.065, 0.55))):
        d = G(x * rake * 1.4, -0.01, ln).normalized()
        shell.spike(b, "FrontHook", G(x, 0.31, 0.24), d, 0.015, ln, spike, "Body", mound, 900 + i,
                    bend=G(0, 0.02, 0))
    # Serrated chitin spikes round the rear carapace rim, raked back, of uneven length (a saw outline from above).
    # Round 49G: each grows out of the rim plate as built (Shell.spike), not from a point under the ideal ellipsoid.
    for i in range(14):
        a = math.radians(-160 + i * 23 + rng.uniform(-6, 6))
        x, f = math.cos(a) * ELY_R[0] * 0.8, ELY_C[2] + math.sin(a) * ELY_R[1] * 0.8
        if f > 0.12:
            continue
        h, n = surface(x, f)
        out = G(math.cos(a), 0.12, math.sin(a) - 0.6).normalized()
        ln = rng.uniform(0.05, 0.1)
        shell.spike(b, "RimSpike", G(x, h - 0.03, f), out, 0.013, ln, spike, "Body", mound, 920 + i,
                    bend=G(0, -0.006, -0.012))
    # The plough head: jagged brow plate, a crystal ram horn, hooked mandibles round an amber throat, four eyes.
    shell.spike(b, "Horn", G(0.01, 0.25, 0.37), G(0.05, 0.75, 0.75), 0.032, 0.15, ice(9, 0.6), "Head", mound, 101,
                shard=dict(sides=5, flat=0.7, tip="point"), bend=G(-0.01, -0.03, 0.05), mound=1.8)
    for sx, ln in ((-1, 0.07), (1, 0.05)):
        shell.spike(b, "BrowHorn", G(0.08 * sx, 0.27, 0.39), G(0.05 * sx, 0.0, ln + 0.02), 0.013, ln, spike, "Head",
                    mound, 940 + (sx > 0), bend=G(0.01 * sx, 0.012, 0))
    b.ellipsoid("Throat", G(0, 0.165, 0.45), (0.045, 0.025, 0.03), bone="Head", style=H.inner_glow(PAL, 1.0), segments=(16, 8))
    for side, sx in SIDES:
        b.tube("Mandible", mandible_points(sx), [0.032, 0.026, 0.016, 0.003], sides=10, chain=[f"Jaw_{side}"],
               style=H.blade(PAL, k, glow=0.9, teeth=4), flatten=0.5)
        # A back-hooked barb on the outer edge of each mandible.
        root = K3.gp((0.118, 0.152, 0.52), sx)
        b.tube("JawBarb", [root, root + K3.gp((0.025, 0.004, -0.004), sx), root + K3.gp((0.03, -0.002, -0.03), sx)],
               [0.009, 0.006, 0.001], sides=8, chain=[f"Jaw_{side}"], style=spike)
        for p, r in (((0.08, 0.232, 0.447), 0.017), ((0.125, 0.222, 0.42), 0.013)):
            b.ellipsoid("Eye", K3.gp(p, sx), (r, r, r * 0.9), bone="Head", style=H.eye(PAL, k, 0.95), segments=(12, 6))
        # Fixed frost crust along the flank edge, irregular (the outline stays spiky when the clusters are gone).
        # Round 49G: the frost used to start under the rim and poke out sideways below it (Remco's "pale-blue spikes
        # floating off the lower rear flank"); it now breaks out of the rim's upper outer face, raked out and up.
        for i in range(7):
            f = 0.17 - i * 0.075 + rng.uniform(-0.02, 0.02)
            h, n = surface(0.2, f)
            d = K3.gp((1.0, 0.55 + rng.uniform(0, 0.5), -0.25 - rng.uniform(0, 0.35)), sx).normalized()
            r_, ln_ = rng.uniform(0.013, 0.02), rng.uniform(0.04, 0.085)
            shell.spike(b, "Frost", K3.gp((0.19, h - 0.035, f), sx), d, r_, ln_, ice(20 + i, 0.45), "Body", mound,
                        int(rng.integers(0, 1 << 30)), shard={}, mound=2.0)
    # Sockets, torn lips and ice clusters. A cluster is its own island on its own bone: its shards and root sit a gap
    # above the socket floor, so the remesh keeps them apart and Unity can scale the bone to 0.
    # Round 49F (Remco: "I see spikes that are not attached to the body"): no hovering fragments; the cluster sits down
    # in its crater (the gap to the floor is a few remesh voxels, below the lips) on a wider root mound; the crater
    # floor is sunk to the carapace line and the lips are rooted deeper, so from the side the gap is hidden. Each
    # cluster must stay its own island (check_floating_49f.py --allow Shard_ lists any island that mixes bones).
    lip = K2.armour_plate(PAL, k, rim_glow=0.0, chevron=0.0, lip=0.6, seed=33)
    for ci, (name, sx, (x, f), _prisms) in enumerate(clusters()):
        h, n = surface(x, f)
        centre = G(x, h, f)
        foot = cluster_footprint(name)
        frame = n.to_track_quat("Z", "Y").to_matrix()
        b.ellipsoid("Socket", centre - n * SOCKET_SINK, (foot * 0.85, foot * 0.72, 0.022), frame, bone="Body",
                    style=K3.socket_floor(PAL, k, glow=0.55, seed=31), segments=(40, 12), deform=torn_socket(60 + ci))
        # Jagged lips: broken chitin shards round the crater, leaning out, uneven in size and spacing.
        a_ = n.orthogonal().normalized()
        b_ = n.cross(a_)
        lips = int(rng.integers(5, 8))
        # Round 49G: the lips are broken chitin plates bursting up out of a raised crater rim (a ring of mounds round
        # the crater, Shell.spike), no longer thorns standing on the flat carapace.
        for j in range(lips):
            az = 2 * math.pi * (j + rng.uniform(-0.3, 0.3)) / lips
            out = a_ * math.cos(az) + b_ * math.sin(az)
            inner = centre + out * (foot + 0.012 + rng.uniform(0, 0.01)) - n * 0.03
            d = (out * rng.uniform(0.55, 0.75) + n * rng.uniform(0.6, 0.8)).normalized()
            shell.spike(b, "Lip", inner, d, rng.uniform(0.011, 0.016), rng.uniform(0.022, 0.04), lip, "Body", mound,
                        int(rng.integers(0, 1 << 30)), shard=dict(flat=0.45, tip="point", chips=1), mound=2.6)
        # Round 49G: the cluster grows out of its crater: its root mound and every shard start inside the crater
        # floor (CLUSTER_SINK), so nothing stands over it. It stays its own island on its own bone because the
        # clusters are remeshed apart from the body (separate_cluster_fuse) and skinned to their bone only
        # (cluster_weights), so Unity still sheds a cluster by scaling its bone to 0.
        G49.growth_cluster(b, centre - n * CLUSTER_SEAT, n, CLUSTER_SIZE[name], 700 + ci * 13, bone=name,
                           chain=[name], style=ice(40 + ci), count=CLUSTER_COUNT[name],
                           spread=SPREAD * (0.7 if CLUSTER_SIZE[name] > 0.35 else 1.0),
                           lean=(0.15, 0.6), sideways=K3.gp((0.5 if x else 0.0, 0.0, -0.35), sx), floating=0,
                           spiral=1, bent=1, radius_ratio=0.14, fragment_gap=0.3, name="Prism",
                           root=(ice(80 + ci, 1.0), foot * 0.62, 0.016), sink=CLUSTER_SINK)
    for (side, i), lg in legs().items():
        hip, knee, foot = lg.rest_points
        sx = -1 if side == "L" else 1
        b.tube("LegUpper", [hip, hip.lerp(knee, 0.5) + Vector((0, 0, 0.014)), knee], [0.055, 0.05, 0.036], sides=10,
               chain=[lg.names[0]], style=H.leg(LEGP, k, "Femur", spines=3), flatten=0.75)
        b.ellipsoid("Knee", knee, (0.04,) * 3, bone=lg.names[0], style=H.band_plates(LEGP, k, 1, height=0.0), segments=(14, 7))
        b.parts[-1].chain = [lg.names[0], lg.names[1]]
        tip = foot + (foot - knee).normalized() * 0.012
        b.tube("LegLower", [knee, knee.lerp(foot, 0.45) + K3.gp((0.014, 0.012, 0), sx), tip], [0.036, 0.026, 0.004],
               sides=10, chain=[lg.names[1]], style=H.leg(LEGP, k, "Tibia"))
        # Round 49G: the knee spur points out and back from the outside of the knee (it pointed up into the gap
        # under the carapace rim and read as a crystal hovering beside the flank), on a mound of knee chitin.
        sd = K3.gp((1.0, 0.5, -0.35 - 0.1 * i), sx).normalized()
        G49.fractured_shard(b, "Spur", knee + sd * 0.012, sd, 0.012, 0.06 + 0.01 * ((i * 7 + (sx > 0)) % 3),
                            60 + i * 2 + (sx > 0), bone=lg.names[0], chain=[lg.names[0]], style=ice(60 + i, 0.4))
        G49.lumpy_root(b, "SpurRoot", knee + sd * 0.03, sd, 0.022, 0.013, 70 + i * 2 + (sx > 0), bone=lg.names[0],
                       style=H.band_plates(LEGP, k, 1, height=0.0), lobes=5, segments=(20, 10))
        b.parts[-1].chain = [lg.names[0]]
    return b


def bones():
    out = [("Root", G(0, 0, 0), G(0, 0, 0.12), None, True),
           ("Body", G(0, 0.26, -0.12), G(0, 0.28, 0.24), "Root", True),
           ("Head", G(0, 0.22, 0.3), G(0, 0.22, 0.48), "Body", True)]
    for side, sx in SIDES:
        out.append((f"Jaw_{side}", K3.gp((0.095, 0.16, 0.42), sx), K3.gp((0.03, 0.14, 0.56), sx), "Head", True))
    for name, sx, (x, f), prisms in clusters():
        h, n = surface(x, f)
        out.append((name, G(x, h, f), G(x, h, f) + n * 0.15, "Body", True))
    for lg in legs().values():
        out += lg.rest_bones("Body")
    return out


# --------------------------------------------------------------------------------------------------------------
# Animation
# --------------------------------------------------------------------------------------------------------------

def body_specs(offset=None, pitch_=0.0, roll=0.0, yaw=0.0, head=0.0, head_yaw=0.0, jaws=0.0):
    specs = {"Root": {"rot": []},
             "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch_)], "loc": offset if offset is not None else Vector()},
             "Head": {"rot": [(Z, head_yaw), (X, head)]}}
    for side, sx in SIDES:
        specs[f"Jaw_{side}"] = {"rot": [(Z, -sx * jaws)]}
    return specs


WALK_FRAMES = 32
STRIDE, LIFT, STANCE = 0.12, 0.045, 0.6
TRIPOD_A = {("L", 1), ("R", 2), ("L", 3)}
PHASES = {key: (0.0 if key in TRIPOD_A else 0.5) for key in [(s, i) for s, _ in SIDES for i in (1, 2, 3)]}


def walk_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        specs = body_specs(offset=Vector((0, 0, 0.006 * math.cos(2 * w))), roll=math.radians(2.5) * math.sin(w),
                           yaw=math.radians(2) * math.sin(w), pitch_=math.radians(1) * math.sin(2 * w),
                           head=math.radians(2) * math.sin(2 * w + 0.6), head_yaw=math.radians(4) * math.sin(w + 0.4),
                           jaws=math.radians(6) * (0.5 + 0.5 * math.sin(2 * w)))
        return K3.place_legs(poser, specs, lg, K3.gait_feet(lg, t, PHASES, STANCE, STRIDE, LIFT))
    return WALK_FRAMES, spec, True


def idle_spec(poser):
    lg = legs()

    def spec(t, f):
        w = 2 * math.pi * t
        snap = max(0.0, math.sin(2 * w + 0.4)) ** 6
        specs = body_specs(offset=Vector((0, 0, 0.004 * math.sin(w))), roll=math.radians(1.2) * math.sin(w),
                           pitch_=math.radians(1.5) * math.sin(w + 0.5), head=math.radians(4) * math.sin(w) - math.radians(5) * snap,
                           head_yaw=math.radians(9) * math.sin(w + 1.0), jaws=math.radians(4) + math.radians(22) * snap)
        return K3.place_legs(poser, specs, lg, {k_: l.foot for k_, l in lg.items()})
    return 72, spec, True


def hit_spec(poser):
    lg = legs()

    def spec(t, f):
        k = K2.recoil(t)
        specs = body_specs(offset=Vector((0, 0.02 * k, 0.012 * k)), pitch_=math.radians(-6) * k, roll=math.radians(4) * k,
                           head=math.radians(-10) * k, jaws=math.radians(25) * max(k, 0.0))
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return 12, spec, False


def death_spec(poser):
    lg = legs()

    def spec(t, f):
        jerk = math.sin(math.pi * min(1.0, t / 0.18)) if t < 0.18 else 0.0
        fall = K2.ease(0.08, 0.5, t)
        twitch = math.exp(-max(0.0, t - 0.55) * 6) * math.sin(t * 55) * (1 if t > 0.55 else 0)
        specs = body_specs(offset=Vector((0, 0, 0.02 * jerk - 0.12 * fall)), roll=math.radians(9) * fall,
                           pitch_=math.radians(5) * fall, head=math.radians(14) * fall + math.radians(4) * twitch,
                           jaws=math.radians(30) * fall)
        return K3.splay_feet(poser, specs, lg, fall, twitch, kick_keys=TRIPOD_A)
    return 40, spec, False


SHATTER_FRAMES = 40
SHATTER_START = 3       # Shard_1 bursts here
SHATTER_STEP = 5        # frames between clusters
SHATTER_FLIGHT = 10     # frames from burst to gone


def shatter_frames():
    return {n: SHATTER_START + i * SHATTER_STEP for i, n in enumerate(SHARD_NAMES)}


def shatter_spec(poser):
    lg = legs()
    starts = shatter_frames()
    info = {}
    for name, sx, (x, f), prisms in clusters():
        h, n = surface(x, f)
        out = Vector((n.x * 1.6 + 0.3 * sx * (x != 0), n.y + 0.4, 0.0))
        info[name] = (out, sx if x != 0 else (1 if name == "Shard_1" else -1))

    def spec(t, f):
        frame = t * SHATTER_FRAMES
        shake = sum(math.exp(-max(0.0, frame - s) * 0.5) * (frame >= s) for s in starts.values())
        jolt = math.sin(frame * 2.4) * min(1.0, shake)
        specs = body_specs(offset=Vector((0, 0, -0.012 * min(1.0, shake))), roll=math.radians(3) * jolt,
                           pitch_=math.radians(-3) * min(1.0, shake), head=math.radians(-8) * min(1.0, shake),
                           jaws=math.radians(20) * min(1.0, shake))
        for name, start in starts.items():
            out, spin_sign = info[name]
            tau = max(0.0, frame - start) / 30.0
            if frame < start:
                specs[name] = {"rot": [], "scale": 1.0}
                continue
            loc = out * (0.9 * tau) + Vector((0, 0, 1.4 * tau - 4.5 * tau * tau))
            gone = K2.ease(start + 2, start + SHATTER_FLIGHT, frame)
            specs[name] = {"rot": [(Yv, spin_sign * 9.0 * tau), (X, -5.0 * tau)], "loc": loc,
                           "scale": max(0.001, 1.0 - gone)}
        return K3.place_legs(poser, specs, lg, {key: l.foot for key, l in lg.items()})
    return SHATTER_FRAMES, spec, False


CLIPS = {"Walk": walk_spec, "Idle": idle_spec, "Hit": hit_spec, "Death": death_spec, "Shatter": shatter_spec}
CLIP_SCALES = None  # every clip keys the shard scale (1 outside Shatter)
RIGID = {"Elytra", "Pronotum", "Brow", "Prism", "PrismFragment", "PrismRoot", "Lip", "Socket", "LipRoot", "FrostRoot",
         "RimSpikeRoot", "FrontHookRoot"}
# LOD1: jaws fold into the head, each leg is one bone; the shard bones stay. 15 bones.
LOD_DROP = {"Jaw_L": "Head", "Jaw_R": "Head", **{f"Leg_{s}{i}_Lower": f"Leg_{s}{i}_Upper" for s, _ in SIDES for i in (1, 2, 3)}}
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in SIDES for i in (1, 2, 3)}


def chipped_render(arm, actions, out, hidden, path, target=(0, 0, 0.3), distance=2.2):
    """A Shardback with the first clusters shed (bone scale 0, as Unity does it): the amber sockets show."""
    muted = [fc for fc in K2.action_fcurves(actions["Idle"])
             if fc.data_path.endswith(".scale") and any(f'"{n}"' in fc.data_path for n in hidden)]
    for fc in muted:
        fc.mute = True
    C.set_action(arm, actions["Idle"], 15)
    for name in SHARD_NAMES:
        arm.pose.bones[name].scale = (0.001,) * 3 if name in hidden else (1.0, 1.0, 1.0)
    bpy_scene_update()
    cam = C.closeup_camera(Vector(target), distance, azimuth=-38, elevation=34, lens=55)
    C.render(cam, os.path.join(out, path), 1280, 720)
    for name in SHARD_NAMES:
        arm.pose.bones[name].scale = (1.0, 1.0, 1.0)
    for fc in muted:
        fc.mute = False


def bpy_scene_update():
    import bpy
    bpy.context.view_layer.update()


def drop_specks_after_bake():
    """Round 49F: every game mesh built in this process loses its islands of fewer than 12 vertices right after its
    bake (the remesh leaves single triangles where a needle tip is thinner than a voxel; they hover as specks off the
    spikes). The shard clusters (50+ vertices) are untouched; the UVs and maps stay as baked."""
    import build_bulwark as BW
    plain = C.build_mesh_asset

    def build_mesh_asset(*a, **kw):
        asset = plain(*a, **kw)
        asset.specks_dropped = BW.drop_specks(asset.low, min_verts=12)
        return asset
    C.build_mesh_asset = build_mesh_asset


def separate_cluster_fuse(builder):
    """Round 49G: the shard clusters now grow out of the crater floor (their roots inside it), so one voxel remesh
    would weld them to the body and Unity could no longer shed a cluster by scaling its bone. In this process the
    remesh of this builder's parts runs twice, once for the body and once for the clusters (CLUSTER_PARTS), and the
    two surfaces are joined: each cluster is a closed island of its own that passes into the floor."""
    import bmesh
    import bpy
    plain = C.fuse

    def part_mesh(obj, keep):
        o2 = C.link(bpy.data.objects.new(obj.name + "_split", obj.data.copy()))
        bm = bmesh.new()
        bm.from_mesh(o2.data)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in np.nonzero(~keep)[0]], context="FACES")
        bm.to_mesh(o2.data)
        bm.free()
        return o2

    def fuse(obj, voxel, smooth_iterations=6):
        if len(obj.data.polygons) != len(builder.face_pid):
            return plain(obj, voxel, smooth_iterations)
        cluster = np.array([builder.parts[p].name in CLUSTER_PARTS for p in builder.face_pid])
        verts, faces = [], []
        for keep in (~cluster, cluster):
            o2 = part_mesh(obj, keep)
            me = plain(o2, voxel, smooth_iterations)
            data = o2.data
            bpy.data.objects.remove(o2)
            bpy.data.meshes.remove(data)
            co = np.empty(len(me.vertices) * 3)
            me.vertices.foreach_get("co", co)
            base = sum(len(v) for v in verts)
            verts.append(co.reshape(-1, 3))
            faces += [[base + i for i in p.vertices] for p in me.polygons]
            bpy.data.meshes.remove(me)
        mesh = bpy.data.meshes.new(obj.name + "_fused")
        mesh.from_pydata(np.concatenate(verts).tolist(), [], faces)
        mesh.update()
        C.log("Shardback clusters remeshed apart:", len(faces), "faces")
        return mesh
    C.fuse = fuse


def cluster_weights():
    """Round 49G: with the cluster roots inside the crater floor, nearest-part skinning could give crater-floor
    vertices a Shard_n weight (the floor would collapse with a shed cluster) or a cluster vertex the Body. After every
    skinning in this process each mesh island is made pure: an island led by a Shard_n bone goes wholly to it, every
    other island loses its Shard_n weights (renormalised; Body if nothing is left)."""
    import check_floating_parts as F
    plain = C.skin

    def skin(mesh_obj, arm, builder, *a, **kw):
        out = plain(mesh_obj, arm, builder, *a, **kw)
        groups = {g.index: g.name for g in mesh_obj.vertex_groups}
        by_name = {g.name: g for g in mesh_obj.vertex_groups}
        body = by_name.get("Body") or mesh_obj.vertex_groups.new(name="Body")
        verts = mesh_obj.data.vertices
        fixed = 0
        for isl in F.islands(mesh_obj.data):
            lead = F.dominant_bone(mesh_obj, isl) or ""
            shard = lead if lead.startswith("Shard_") else None
            for i in isl:
                v = verts[int(i)]
                ws = {groups[g.group]: g.weight for g in v.groups}
                if shard:
                    new = {shard: 1.0}
                else:
                    new = {n: w for n, w in ws.items() if not n.startswith("Shard_")}
                    tot = sum(new.values())
                    new = {n: w / tot for n, w in new.items()} if tot > 1e-6 else {"Body": 1.0}
                if new == ws:
                    continue
                for g in list(v.groups):
                    mesh_obj.vertex_groups[g.group].remove([v.index])
                for n, w in new.items():
                    (by_name.get(n) or body).add([v.index], w, "REPLACE")
                fixed += 1
        C.log(mesh_obj.name, "cluster weights made pure on", fixed, "vertices")
        return out
    C.skin = skin


def main():
    drop_specks_after_bake()
    uvlayout.SPARSE_FIX = True  # round 49D: the crowded crystal back cuts into many small islands
    builder = build_parts()
    separate_cluster_fuse(builder)
    cluster_weights()
    rooted = builder.shell.rooted
    layout = [("Walk", 0, (-1.0, 0.6), 0.2), ("Walk", 10, (0.2, 0.9), -0.15), ("Shatter", 12, (1.2, 0.4), 0.3),
              ("Walk", 5, (-0.5, -0.4), 0.0), ("Walk", 20, (0.8, -0.6), -0.3), ("Idle", 20, (-1.6, -0.2), 0.4)]
    previews = dict(target=(0, 0.0, 0.3), distance=2.4, move=("Walk", 4), layout=layout,
                    strips=[("Walk", "side", range(0, WALK_FRAMES, 4)), ("Shatter", "close", range(0, SHATTER_FRAMES, 4)),
                            ("Death", "close", range(0, 41, 6))],
                    macros=[("ice", G(0.06, 0.55, 0.0), 0.5, -50, 30), ("head", G(0.0, 0.25, 0.45), 0.45, -25, 15),
                            ("socket", G(0.17, 0.42, -0.16), 0.4, -60, 50)],
                    rig_scale=1.2, power=("Shatter", 14))
    asset, arm, actions, lod = K3.run_creature(
        NAME, builder, bones(), CLIPS, tris=3950, families=("chitin", "crystal", "membrane"), game_scale=GAME_SCALE,
        rigid=RIGID, blend=2, lod_drop=LOD_DROP, lod_aims=LOD_AIMS, scale_bones=SHARD_NAMES, clip_scales=CLIP_SCALES,
        extra=H.weathering(K, markings=PAL.dark, marking_amount=0.4, dust=0.3, seed=71), specular=0.35,
        report={"shard_bones_in_shed_order": SHARD_NAMES, "shatter_burst_frames": shatter_frames(),
                "walk_distance_per_loop": round(STRIDE / STANCE, 3), "pivot": "ground, body centre",
                "spit_origin": "Throat, about (0, 0.17, 0.46) model units, from the Head bone",
                "round49g": {"spikes_on_shell": len(rooted), "min_root_depth": min(r[1] for r in rooted),
                             "clusters": "remeshed apart from the body, roots and shards sunk into the crater floor; "
                                         "one island per Shard_n bone, weights pure"}},
        previews=None if C.cli_args().get("norender") else previews, mirror=False)
    if not C.cli_args().get("norender"):
        out = C.preview_dir()
        chipped_render(arm, actions, out, set(SHARD_NAMES[:3]), "shardback_chipped.png")
        chipped_render(arm, actions, out, set(SHARD_NAMES), "shardback_bare.png")
    # Round 46: the close-up detail (eyes, mandible teeth, talons) as extra parts, exported again with the body.
    # `--nodetail` (round 49D) stops after the body (build_attacks36 and build_insect_gait run before the detail).
    if C.cli_args().get("nodetail"):
        return
    import shardback_detail
    shardback_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
