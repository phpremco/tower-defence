"""Round 49D: irregular alien crystal growth (Remco 2026-11-12: "the crystals are too perfect", "those circles are
too perfect, it doesn't look scary enough").

The round-10..28 crystals were regular n-gon prisms (PartBuilder.shard / prism) repeated at one size in fans. Here a
shard is built from scratch so no two are alike:

- an irregular cross-section: 4..7 faces at jittered angles and radii (faces of unequal width), often blade-flat;
- a profile with a flared root (it grew out of the flesh), one or two fracture steps (the radius drops between two
  rings: chipped faces) and either a sharp off-centre point or a broken, slanted fracture face at the tip;
- chips: notches pushed into a few faces along the length;
- bent and spiral axes (bend, helix), so a cluster mixes straight blades with curling horns.

`growth_cluster` grows one dominant shard and satellites of falling size from a lumpy organic root (flesh or chitin
fused with crystal), with a few small floating fragments hovering off the cluster (the alien rule: floating shards).
`uneven_glow` wraps a crystal style so the inner light comes in patches and differs per shard.

Only used by the round-49D creatures (Shardback, Bulwark, CrystalWarlord, BroodMother, Queen, Shattered); the shared
kits (crysium.py, creature_kit*.py) are not changed.
"""

import math

import numpy as np
from mathutils import Matrix, Vector

import common as C


def _rng(seed):
    return np.random.default_rng(seed)


def _frame(direction):
    d = Vector(direction).normalized()
    return d, d.to_track_quat("Z", "Y").to_matrix()


def fractured_shard(b, name, base, direction, radius, length, seed, *, sides=None, flat=None, bend=None, helix=None,
                    tip=None, steps=None, chips=None, root_flare=1.25, rings=14, bone=None, chain=None, style=None):
    """One irregular crystal shard, tagged as a PartBuilder part of kind "shard" (styles that read tube_param work).
    `flat` < 1 squashes the cross-section into a blade; `tip` is "point" (off-centre spike) or "broken" (a slanted
    fracture face); `steps` the number of fracture steps along the length; `chips` the number of notches."""
    rng = _rng(seed)
    d, rot = _frame(direction)
    base = Vector(base)
    n = int(sides or rng.choice((4, 5, 5, 6, 6, 7)))
    flat = float(flat if flat is not None else rng.uniform(0.55, 1.0))
    tip = tip or ("broken" if rng.uniform() < 0.4 else "point")
    steps = int(steps if steps is not None else rng.integers(1, 3))
    chips = int(chips if chips is not None else rng.integers(1, 4))
    bend = Vector(bend) if bend is not None else Vector((0, 0, 0))
    # Irregular polygon: jittered angles, per-corner radius.
    angles = np.sort((np.arange(n) + rng.uniform(-0.32, 0.32, n)) * 2 * math.pi / n) + rng.uniform(0, 2 * math.pi)
    corner = rng.uniform(0.72, 1.22, n)
    step_at = np.sort(rng.uniform(0.3, 0.8, steps))
    step_drop = rng.uniform(0.1, 0.24, steps)
    chip_list = [(int(rng.integers(0, n)), rng.uniform(0.15, 0.85), rng.uniform(0.06, 0.14), rng.uniform(0.25, 0.5))
                 for _ in range(chips)]
    off = Vector((rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.35), 0)) * radius
    tilt = rng.uniform(0.12, 0.3)
    tilt_dir = rng.uniform(0, 2 * math.pi)
    twist = rng.uniform(-0.6, 0.6)

    def profile(t):
        r = 1.0 + (root_flare - 1.0) * max(0.0, 1.0 - t / 0.14) ** 2
        for at, drop in zip(step_at, step_drop):
            r *= 1.0 - drop * C.smoothstep(at - 0.015, at + 0.015, np.array([t]))[0]
        end = 0.62 if tip == "broken" else 0.0
        taper = 1.0 - (1.0 - end) * max(0.0, (t - 0.45) / 0.55) ** 1.4
        return r * taper * (1.0 - 0.18 * t)

    def centre(t):
        c = base + rot @ Vector((0, 0, length * t)) + bend * (t * t)
        if helix:
            hr, turns, start = helix
            a = start + 2 * math.pi * turns * t
            c = c + rot @ Vector((math.cos(a), math.sin(a), 0)) * hr * (1 - 0.6 * t)
        return c

    ring_verts = []
    for i in range(rings):
        t = i / (rings - 1) if tip == "broken" else i / rings
        r = radius * profile(t)
        c = centre(t)
        ring = []
        for s in range(n):
            a = angles[s] + twist * t
            k = corner[s]
            for cs, ct, depth, width in chip_list:
                if s == cs and abs(t - ct) < width * 0.5:
                    k *= 1.0 - depth * 4.0 * (1.0 - abs(t - ct) / (width * 0.5))
            local = Vector((math.cos(a) * r * k, math.sin(a) * r * k * flat, 0))
            if tip == "broken":
                # The slanted fracture face: every ring is clipped to one tilted plane through the top, so the end is
                # a flat broken face (rings above the plane collapse onto it instead of folding through each other).
                cut = 1.0 - tilt * (0.5 + 0.5 * math.cos(a - tilt_dir))
                local.z = -length * max(0.0, t - cut)
            ring.append(b.bm.verts.new(c + rot @ local))
        ring_verts.append(ring)
    bottom = b.bm.verts.new(centre(0.0) - d * radius * 0.2)
    if tip == "broken":
        top_c = sum((v.co for v in ring_verts[-1]), Vector()) / n
        apex = b.bm.verts.new(top_c + d * radius * 0.05)
    else:
        apex = b.bm.verts.new(centre(1.0) + rot @ off)
    faces = []
    for s in range(n):
        s2 = (s + 1) % n
        faces.append(b.bm.faces.new((ring_verts[0][s2], ring_verts[0][s], bottom)))
        for i in range(len(ring_verts) - 1):
            faces.append(b.bm.faces.new((ring_verts[i][s], ring_verts[i][s2], ring_verts[i + 1][s2], ring_verts[i + 1][s])))
        faces.append(b.bm.faces.new((ring_verts[-1][s], ring_verts[-1][s2], apex)))
    frame = (Matrix.Translation(base) @ rot.to_4x4() @ Matrix.Diagonal((radius, radius, length, 1.0))).inverted()
    part = C.Part(len(b.parts), name, "shard", frame, bone=bone, chain=chain, radii=(radius, length, 0.0), style=style)
    ts = np.linspace(0, 1, 12)
    part.axis = np.array([tuple(centre(t)) for t in ts])
    part.radii = np.array([radius * max(profile(t), 0.05) for t in ts])
    if chain is None and bone is not None:
        part.chain = [bone]
    return b._tag(faces, part)


def lumpy_root(b, name, centre, normal, radius, height, seed, bone=None, style=None, lobes=6, segments=(28, 14)):
    """The organic mound a cluster grows from: a squat ellipsoid with uneven lobes and a ragged skirt (never a
    disc)."""
    rng = _rng(seed)
    n = Vector(normal).normalized()
    rot = n.to_track_quat("Z", "Y").to_matrix()
    amp = rng.uniform(0.12, 0.3, lobes)
    ph = rng.uniform(0, 2 * math.pi, lobes)
    fr = rng.integers(2, 7, lobes)

    def deform(v):
        a = math.atan2(v.y, v.x)
        rim = 1.0 - abs(v.z)
        k = 1.0 + sum(am * math.sin(f * a + p) for am, f, p in zip(amp, fr, ph)) * 0.5 * rim
        z = v.z * (1.0 + 0.35 * math.sin(3 * a + ph[0]) * max(v.z, 0.0))
        return Vector((v.x * k, v.y * k * rng_fixed[0], z))
    rng_fixed = [rng.uniform(0.7, 1.0)]
    part = b.ellipsoid(name, Vector(centre), (radius, radius, height), rot, bone=bone, style=style, segments=segments,
                       deform=deform)
    # A short axis along the normal, so crystal styles (which read tube_param) colour the mound as a shard root.
    c = Vector(centre)
    part.axis = np.array([tuple(c - n * height), tuple(c + n * height * 2.0)])
    part.radii = np.array([radius, radius * 0.6])
    return part


def growth_cluster(b, base, normal, size, seed, *, bone, style, count=5, spread=0.3, lean=(0.15, 0.6), sideways=None,
                   floating=2, spiral=1, bent=1, root=None, name="Crystal", blade=False, radius_ratio=0.17,
                   fragment_gap=0.25, chain=None, sink=0.0):
    """A cluster of `count` shards from `base` along `normal`: one dominant shard of length `size`, satellites of
    falling size (a power curve, so there are a few big and many small), leaning off the normal by `lean` (radians,
    min/max) and spread over `spread` x size around the base. `sideways` biases the lean (a world vector).
    `spiral` shards wind a helix, `bent` ones curl; `floating` small fragments hover off the cluster, separated
    by `fragment_gap` x their length (own islands on the same bone). `root` (style, radius, height) adds a lumpy
    organic mound under the cluster on the same bone. `sink` (round 49G, default 0 = as before) starts every shard
    that much deeper along -normal and longer by the same amount (tips unchanged), so satellites spread over a curved
    floor still grow out of it. Returns the list of (base, direction, length) placed."""
    rng = _rng(seed)
    n = Vector(normal).normalized()
    a_ = n.orthogonal().normalized()
    b_ = n.cross(a_)
    placed = []
    sizes = [1.0] + sorted((rng.uniform(0.18, 0.72) ** 1.3 for _ in range(count - 1)), reverse=True)
    kinds = ["plain"] * count
    order = list(range(1, count))
    rng.shuffle(order)
    for j in order[:spiral]:
        kinds[j] = "spiral"
    for j in order[spiral:spiral + bent]:
        kinds[j] = "bent"
    for j, s in enumerate(sizes):
        az = rng.uniform(0, 2 * math.pi) if j else rng.uniform(0, 2 * math.pi)
        rad = 0.0 if j == 0 else spread * size * rng.uniform(0.35, 1.0)
        out = a_ * math.cos(az) + b_ * math.sin(az)
        p = Vector(base) + out * rad
        tilt = rng.uniform(*lean) * (0.6 if j == 0 else 1.0)
        d = (n * math.cos(tilt) + out * math.sin(tilt))
        if sideways is not None:
            d = d + Vector(sideways) * rng.uniform(0.2, 0.6)
        d.normalize()
        length = size * s
        r = length * radius_ratio * rng.uniform(0.8, 1.2) * (1.15 if j == 0 else 1.0)
        kw = {}
        if kinds[j] == "spiral":
            kw["helix"] = (r * 1.6, rng.uniform(0.6, 1.1), rng.uniform(0, 2 * math.pi))
        elif kinds[j] == "bent":
            side = (d.cross(n) if d.cross(n).length > 1e-3 else a_).normalized()
            kw["bend"] = side * length * rng.uniform(0.25, 0.45) * (1 if rng.uniform() < 0.5 else -1)
        start = p - n * r * 0.3 - d * (sink / max(d.dot(n), 0.3))
        fractured_shard(b, name, start, d, r, length + sink / max(d.dot(n), 0.3), int(rng.integers(0, 1 << 30)),
                        flat=rng.uniform(0.35, 0.6) if blade else None, bone=bone, chain=chain, style=style, **kw)
        placed.append((p, d, length))
    for i in range(floating):
        # A fragment hovering beside the upper half of a big shard, its own island.
        p, d, length = placed[min(i, len(placed) - 1)]
        side = d.orthogonal().normalized()
        side.rotate(Matrix.Rotation(rng.uniform(0, 2 * math.pi), 3, d))
        flen = length * rng.uniform(0.24, 0.34)
        fr = flen * rng.uniform(0.16, 0.22)
        at = p + d * length * rng.uniform(0.55, 0.85) + side * (length * radius_ratio * 1.4 + fr * 2.0 + flen * fragment_gap)
        fd = (d + side * rng.uniform(-0.6, 0.6) + n * 0.3).normalized()
        fractured_shard(b, name + "Fragment", at - fd * flen * 0.5, fd, fr, flen, int(rng.integers(0, 1 << 30)),
                        tip="point", steps=0, chips=0, root_flare=1.0, rings=6, bone=bone, chain=chain, style=style)
    if root is not None:
        rstyle, rr, rh = root
        lumpy_root(b, name + "Root", Vector(base) - n * rh * 0.2, n, rr, rh, seed + 7, bone=bone, style=rstyle)
    return placed


def uneven_glow(style, seed, lo=0.3, hi=1.3, patch=7.0):
    """Wraps a crystal style: each shard gets its own light level and the light inside comes in patches (bright
    pockets and dead stretches), so a cluster pulses unevenly instead of glowing as one even lamp."""
    def wrapped(s, idx, part):
        style(s, idx, part)
        rng = _rng(seed * 7919 + part.pid)
        level = rng.uniform(lo, hi)
        pocket = 0.5 + 0.5 * C.value_noise(s.p[idx], patch / max(float(part.radii[0]) * 6.0, 0.02), seed + part.pid)
        s.emit[idx] = np.clip(s.emit[idx] * (level * (0.35 + 0.9 * pocket ** 1.5))[:, None], 0, 1)
    return wrapped


def serrated_outline(teeth, seed, depth=0.12, jitter=0.5):
    """A deform helper for ellipsoids: returns f(angle) -> radius factor with `teeth` saw teeth of uneven size and
    spacing (a back-raked saw: slow rise, sharp drop), used for torn rims, chipped shields and jagged plates."""
    rng = _rng(seed)
    edges = np.sort((np.arange(teeth) + rng.uniform(-jitter * 0.5, jitter * 0.5, teeth)) / teeth) * 2 * math.pi
    amps = depth * rng.uniform(0.35, 1.25, teeth)

    def f(a):
        a = a % (2 * math.pi)
        i = int(np.searchsorted(edges, a)) - 1
        a0 = edges[i % teeth] - (2 * math.pi if i < 0 else 0.0)
        a1 = edges[(i + 1) % teeth] + (2 * math.pi if i + 1 >= teeth else 0.0)
        u = (a - a0) / max(a1 - a0, 1e-6)
        return 1.0 + amps[i % teeth] * (u ** 1.8 - 0.5)
    return f
