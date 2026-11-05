"""Round 30: grown-ice helpers for the Frost Warden redesign and the ice props (Gate_IceBlock, seal crystals).

- `Facets`: a hewn convex polyhedron (like common.chiselled) that also remembers its planes, so a style can find
  the facet ridges (frost on the edges, the dark depth in the middle of a face). `mirror=True` flips the planes in
  local x, so a left part built with the same seed is the exact mirror of the right one.
- `frost_ice`: deep blue-green ice with a dark core (as if you look into it), white rime only on ridges, rims and
  upward faces, inner fracture planes in the normal map and thin cyan light in a few of them (no glowing areas).
- `kite_blade`: a blade along a curve with a six-point cross-section (sharp edge, two flanks, a faceted spine with a
  ridge), i.e. triangular in section, so it catches light from every side instead of reading as a flat card.
- `chunk`: a faceted ice block (ellipsoid + Facets) in one call.
- `scale_builder`: scales a finished PartBuilder (verts, part frames, axes, radii) about the origin.
"""

import numpy as np
from mathutils import Matrix, Vector

import common as C

ss = C.smoothstep

# Paint-space (sRGB) colours of grown ice.
DEEP = np.array((0.04, 0.13, 0.19))
BODY = np.array((0.15, 0.39, 0.51))
CLEAR = np.array((0.34, 0.62, 0.74))
FROST = np.array((0.8, 0.9, 0.96))
FRACTURE = np.array((0.62, 0.82, 0.9))
GLOW = np.array((0.38, 0.85, 1.0))


class Facets:
    def __init__(self, count, seed, depth=0.15, squash=None, mirror=False):
        rng = np.random.default_rng(seed)
        dirs = C.fibonacci_sphere(count, seed)
        dirs /= np.linalg.norm(dirs, axis=1, keepdims=True)
        heights = 1.0 - rng.uniform(0, depth, count)
        if squash is not None:
            heights = heights * np.interp(dirs[:, 2], (-1, 1), squash)
        if mirror:
            dirs[:, 0] *= -1
        self.dirs = dirs
        self.heights = heights

    def deform(self, v):
        u = np.array((v.x, v.y, v.z))
        dots = self.dirs @ u
        ok = dots > 1e-3
        r = np.min(self.heights[ok] / dots[ok]) if ok.any() else 1.0
        return Vector(u * min(r, 1.6))

    def ridge(self, local):
        """(ridge 0..1 near a facet edge, inner 0..1 deep in a face) for points in the part's unit space."""
        a = (local @ self.dirs.T) / self.heights[None, :]
        top = np.sort(a, axis=1)
        a1, a2 = top[:, -1], top[:, -2]
        gap = (a1 - a2) / np.maximum(a1, 1e-6)
        return 1 - ss(0.0, 0.04, gap), ss(0.04, 0.3, gap)


def frost_ice(k, seed=0, facets=None, crack_glow=0.7, rime=1.0, depth=1.0, top_frost=0.4, clear=0.35):
    """Grown ice (see the module doc). `k` sets the pattern scale like the other styles."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        if facets is not None:
            ridge, inner = facets.ridge(C.local_points(part, p))
        elif part.axis is not None and part.kind in ("tube", "shard"):
            _, radius, dist = C.tube_param(part, p)
            rho = dist / np.maximum(radius, 1e-4)
            ridge = ss(0.8, 1.0, rho)
            inner = 1 - ss(0.5, 0.9, rho)
        else:
            ridge = np.zeros(len(idx))
            inner = np.ones(len(idx))
        cloud = ss(-0.4, 0.5, C.value_noise(p, 2.5 / k, seed))
        col = C.lerp3(BODY, DEEP, np.clip((inner * 0.4 + cloud * 0.3) * depth, 0, 1))
        col = C.lerp3(col, CLEAR, ss(0.25, 0.7, C.value_noise(p, 5 / k, seed + 1)) * clear)
        crack = (1 - ss(0.0, 0.035, np.abs(C.value_noise(p, 6 / k, seed + 3)))) * \
            ss(-0.25, 0.25, C.value_noise(p, 2 / k, seed + 4))
        fine = (1 - ss(0.0, 0.02, np.abs(C.value_noise(p, 16 / k, seed + 6)))) * \
            ss(-0.1, 0.4, C.value_noise(p, 4 / k, seed + 7))
        col = C.lerp3(col, FRACTURE, np.clip(crack * 0.6 + fine * 0.3, 0, 1))
        up = ss(0.5, 0.95, n[:, 2]) * ss(0.1, 0.7, C.value_noise(p, 8 / k, seed + 5))
        broken = ss(-0.2, 0.35, C.value_noise(p, 7 / k, seed + 10))      # rime gathers on parts of a ridge, not all
        rim = np.clip((ridge * broken * (0.6 + 0.4 * ss(-0.3, 0.3, C.value_noise(p, 20 / k, seed + 9))) + up * top_frost)
                      * rime, 0, 1)
        col = C.lerp3(col, FROST, rim)
        s.albedo[idx] = col
        lit = crack * ss(0.15, 0.45, C.value_noise(p, 1.5 / k, seed + 8)) * (1 - rim)
        s.emit[idx] = GLOW[None, :] * np.clip(lit * crack_glow, 0, 1)[:, None]
        s.rough[idx] = np.clip(0.1 + 0.55 * rim + 0.2 * crack + 0.15 * fine, 0.05, 0.9)
        s.disp[idx] = -0.003 * k * crack - 0.0012 * k * fine + 0.001 * k * rim
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        s.flow[idx] = 0.0
    return style


def chunk(b, name, centre, radii, rot, bone, seed, count=16, depth=0.18, squash=None, mirror=False, k=2.0,
          segments=(36, 18), **style_kw):
    f = Facets(count, seed, depth, squash, mirror)
    return b.ellipsoid(name, centre, radii, rot, bone=bone, segments=segments, deform=f.deform,
                       style=frost_ice(k, seed=seed, facets=f, **style_kw))


def kite_blade(b, name, points, widths, thick, edge_dir, chain, style, samples=6):
    """A blade along a Catmull-Rom curve through `points`; `widths` (edge to axis) and `thick` (half thickness) are
    interpolated along it; `edge_dir` is the direction the cutting edge faces (projected per ring)."""
    curve = C.catmull_rom([Vector(p) for p in points], samples)
    m = len(curve)
    t_lin = np.linspace(0, 1, m)
    w = np.maximum(np.interp(t_lin, np.linspace(0, 1, len(widths)), widths), 0.004)
    th = np.maximum(np.interp(t_lin, np.linspace(0, 1, len(thick)), thick), 0.003)
    edge_dir = Vector(edge_dir).normalized()
    rings = []
    tangents = []
    for i, c in enumerate(curve):
        t = (curve[min(i + 1, m - 1)] - curve[max(i - 1, 0)]).normalized()
        e = (edge_dir - t * edge_dir.dot(t)).normalized()
        nrm = t.cross(e).normalized()
        W, T = float(w[i]), float(th[i])
        sec = [e * W, nrm * T - e * W * 0.2, nrm * T * 0.55 - e * W * 0.68, -e * W * 0.86,
               -nrm * T * 0.55 - e * W * 0.68, -nrm * T - e * W * 0.2]
        rings.append([b.bm.verts.new(c + o) for o in sec])
        tangents.append(t)
    sides = 6
    faces = []
    for i in range(m - 1):
        for s_ in range(sides):
            s2 = (s_ + 1) % sides
            faces.append(b.bm.faces.new((rings[i][s_], rings[i][s2], rings[i + 1][s2], rings[i + 1][s_])))
    start = b.bm.verts.new(curve[0] - tangents[0] * float(w[0]) * 0.4)
    end = b.bm.verts.new(curve[-1] + tangents[-1] * 0.03)
    for s_ in range(sides):
        s2 = (s_ + 1) % sides
        faces.append(b.bm.faces.new((rings[0][s2], rings[0][s_], start)))
        faces.append(b.bm.faces.new((rings[-1][s_], rings[-1][s2], end)))
    part = C.Part(len(b.parts), name, "tube", Matrix.Identity(4), chain=chain,
                  axis=np.array([tuple(c) for c in curve]), radii=w, style=style)
    part.bone = chain[0]
    return b._tag(faces, part)


def scale_builder(b, s):
    """Scales every vertex and part of a finished PartBuilder by `s` about the origin (a bigger creature built from
    the same design numbers). Part frames map world to local, so they take the inverse scale on the right."""
    if abs(s - 1.0) < 1e-9:
        return b
    for v in b.bm.verts:
        v.co *= s
    inv = Matrix.Scale(1.0 / s, 4)
    for p in b.parts:
        p.frame = p.frame @ inv
        if p.axis is not None:
            p.axis = np.asarray(p.axis, float) * s
        if p.radii is not None:
            p.radii = np.asarray(p.radii, float) * s
    return b

