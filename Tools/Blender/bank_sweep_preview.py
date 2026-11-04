"""Round 24b preview: the same 'h' pool as liquid_preview.py, once square with the cell bank kit and once with an
organic outline (cell boundary -> resampled loop -> circular Gaussian smoothing -> a little normal wobble) and the
BankSweep segments bent along it, for three styles. The contour code is what the Unity side is meant to do; see
README round 24b.

Outputs: bank_<Style>_square.png, bank_<Style>_organic.png, bank_<Style>_compare.png (square | organic),
bank_<Style>_close.png, bank_all.png.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Vector

import common as C
import env_kit as E
import liquid_kit as LK
import liquid_preview as LP

F32 = np.float32

# style: (liquid kind, folder key or "", prefix, liquid_preview SETS key for ground and screenshot)
STYLES = {"CrystalPlains": ("Water", "", ""), "AshenReach": ("Lava", "AshenReach", "Ash_"),
          "Mirewood": ("Bog", "Mirewood", "Mire_")}

SMOOTH_SIGMA = 0.55     # world units along the contour; keeps the bend radius near or above 0.5
WOBBLE = 0.04
# The shelf reaches this far into the liquid: the shore effects (foam, depth) start at its edge, not at the curve.
SHELF_REACH = 0.06
STEP = 0.02


# -------------------------------------------------------------------------------------------------- contour

def boundary_loop(cells):
    """Outline of a cell union as one counter-clockwise loop of grid corner points (simply connected pools)."""
    edges = {}
    for (i, j) in cells:
        for (a, b), n in ((((i, j), (i + 1, j)), (i, j - 1)), (((i + 1, j), (i + 1, j + 1)), (i + 1, j)),
                          (((i + 1, j + 1), (i, j + 1)), (i, j + 1)), (((i, j + 1), (i, j)), (i - 1, j))):
            if n not in cells:
                edges[a] = b       # CCW around each cell, so the outline runs CCW with the liquid on its left
    start = next(iter(edges))
    loop = [start]
    while True:
        nxt = edges[loop[-1]]
        if nxt == start:
            break
        loop.append(nxt)
    return np.array(loop, F32)


def resample(loop, step):
    pts = np.vstack([loop, loop[:1]])
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)])
    total = s[-1]
    n = int(round(total / step))
    u = np.linspace(0, total, n, endpoint=False)
    return np.stack([np.interp(u, s, pts[:, 0]), np.interp(u, s, pts[:, 1])], axis=1).astype(F32)


def smooth_loop(p, sigma_pts):
    """Circular Gaussian smoothing (FFT) of a closed polyline: rounds corners, keeps it closed."""
    n = len(p)
    f = np.fft.fftfreq(n)
    kern = np.exp(-2 * math.pi ** 2 * sigma_pts ** 2 * f * f)
    return np.real(np.fft.ifft(np.fft.fft(p, axis=0) * kern[:, None], axis=0)).astype(F32)


def organic_contour(cells, seed=7):
    p = resample(boundary_loop(cells), STEP)
    p = smooth_loop(p, SMOOTH_SIGMA / STEP)
    p = resample(p, STEP)
    t, nrm = frames(p)
    rng = np.random.default_rng(seed)
    n = len(p)
    k = np.arange(n) / n
    wob = sum(rng.uniform(0.3, 1.0) * np.sin(2 * math.pi * (h * k + rng.random())) / h for h in (5, 9, 14, 23))
    p = p - nrm * (WOBBLE * wob / np.abs(wob).max())[:, None]
    return resample(p, STEP)


def frames(p):
    """Unit tangents and outward (dry-side) normals of a CCW loop."""
    t = np.roll(p, -1, axis=0) - np.roll(p, 1, axis=0)
    t /= np.linalg.norm(t, axis=1, keepdims=True)
    out = np.stack([t[:, 1], -t[:, 0]], axis=1)   # right of the direction of travel = outside for CCW
    return t, out


def radius_stats(p, span=8):
    """Smallest bend radius, measured over `span` steps (0.16 units, about a stone) so resampling noise does not
    count."""
    t, _ = frames(p)
    ang = np.unwrap(np.arctan2(t[:, 1], t[:, 0]))
    ang = np.concatenate([ang, ang[:span] + (ang[-1] - ang[0]) + (ang[1] - ang[0])])
    d = np.abs(ang[span:] - ang[:-span])
    return float(span * STEP / max(d.max(), 1e-6))


def contour_distance(p):
    """Distance (cells) inside the contour to the contour; 0 outside. Grid as liquid_preview.distance_map."""
    w, h = LP.FIELD[0] * LP.PX_PER_CELL, LP.FIELD[1] * LP.PX_PER_CELL
    x, y = LK.pixel_grid(h, w)
    px = (x + 0.5) / LP.PX_PER_CELL
    py = LP.FIELD[1] - (y + 0.5) / LP.PX_PER_CELL
    a = p
    b = np.roll(p, -1, axis=0)
    d = np.full((h, w), 99.0, F32)
    inside = np.zeros((h, w), bool)
    for (ax, ay), (bx, by) in zip(a, b):
        dx, dy = bx - ax, by - ay
        tt = np.clip(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(px - ax - tt * dx, py - ay - tt * dy))
        cross = ((ay > py) != (by > py)) & (px < (bx - ax) * (py - ay) / (by - ay + 1e-12) + ax)
        inside ^= cross
    return np.where(inside, d, 0).astype(F32)


# ---------------------------------------------------------------------------------------------------- sweep

def load_segment(folder, name):
    d = os.path.join(E.ENV, folder, name) if folder else os.path.join(E.ENV, name)
    objs = E.textured_import(d, name, emission_strength=1.2)
    obj = max((o for o in objs if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    for o in objs:
        if o is not obj:
            bpy.data.objects.remove(o)
    # Bake the FBX axis conversion into the vertices, then drop the imported custom normals (they would not bend).
    obj.data.transform(obj.matrix_world)
    obj.matrix_world.identity()
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except RuntimeError:
        pass
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj.hide_render = True
    return obj


def sweep_along(p, sources, seed=3):
    """Bends segment copies along the closed contour: n = round(perimeter), each stretched to perimeter / n.
    Local x -> arc length, local y -> along the outward normal (dry side), z stays up."""
    seg = np.linalg.norm(np.diff(np.vstack([p, p[:1]]), axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)])
    total = float(s[-1])
    n = max(3, int(round(total / 1.0)))
    length = total / n
    _, nrm = frames(p)
    pts = np.vstack([p, p[:1]])
    nr = np.vstack([nrm, nrm[:1]])
    rng = np.random.default_rng(seed)
    order, last = [], -1
    for _ in range(n):
        k = int(rng.integers(0, len(sources)))
        if k == last:
            k = (k + 1) % len(sources)
        order.append(k)
        last = k
    objs = []
    for i, k in enumerate(order):
        src = sources[k]
        mesh = src.data.copy()
        co = np.zeros(len(mesh.vertices) * 3, F32)
        mesh.vertices.foreach_get("co", co)
        co = co.reshape(-1, 3)
        u = (i * length + (co[:, 0] + 0.5) * length) % total
        px = np.interp(u, s, pts[:, 0])
        py = np.interp(u, s, pts[:, 1])
        nx = np.interp(u, s, nr[:, 0])
        ny = np.interp(u, s, nr[:, 1])
        ln = np.sqrt(nx * nx + ny * ny)
        nx, ny = nx / ln, ny / ln
        new = np.stack([px + nx * co[:, 1], py + ny * co[:, 1], co[:, 2]], axis=1)
        mesh.vertices.foreach_set("co", new.astype(F32).ravel())
        mesh.update()
        obj = C.link(bpy.data.objects.new(f"Sweep_{i}", mesh))
        objs.append(obj)
    return objs, n, length


def scatter_accents(p, accent, n, length, share=0.6, seed=11):
    """Accents on the segment joints (where the baked maps of two variants meet) plus a few in between."""
    rng = np.random.default_rng(seed)
    _, nrm = frames(p)
    seg = np.linalg.norm(np.diff(np.vstack([p, p[:1]]), axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)])
    spots = [i * length for i in range(n) if rng.random() < share]
    spots += list(rng.uniform(0, s[-1], 3))
    for u in spots:
        k = min(int(np.searchsorted(s, u)), len(p) - 1)
        off = rng.uniform(0.0, 0.08)
        o = C.link(bpy.data.objects.new("Accent", accent.data))
        o.location = (p[k, 0] + nrm[k, 0] * off, p[k, 1] + nrm[k, 1] * off, 0.0)
        o.rotation_euler.z = rng.uniform(0, 2 * math.pi)
        sc = rng.uniform(0.8, 1.2)
        o.scale = (sc, sc, sc)


# ---------------------------------------------------------------------------------------------------- scenes

def organic_scene(style, out_dir):
    kind, folder, prefix = STYLES[style]
    C.reset_scene()
    LP._SRC.clear()
    cells = LP.pool_cells()
    p = organic_contour(cells)
    dist = np.maximum(contour_distance(resample(p, 0.06)) - SHELF_REACH, 0.0)
    _, _, gfolder, gname, _, tint = LP.SETS[kind]
    img = bpy.data.images.new("Dist", dist.shape[1], dist.shape[0], float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    rgba = np.ones((dist.shape[0], dist.shape[1], 4), F32)
    rgba[..., 0] = rgba[..., 1] = rgba[..., 2] = dist
    img.pixels.foreach_set(rgba[::-1].ravel())
    img.pack()
    LP.terrain(dist, LP.tiling_material("Ground", gfolder, gname))
    sources = [load_segment(folder, f"{prefix}BankSweep_{v}") for v in "ABC"]
    _, n, length = sweep_along(p, sources)
    accent = load_segment(folder, f"{prefix}BankAccent")
    scatter_accents(p, accent, n, length)
    mat, g, bsdf = LP.new_liquid_material("Liquid_" + kind)
    g.value("time")
    tex = lambda nme: os.path.join(out_dir, kind, nme + ".png")  # noqa: E731
    LP.SHADERS[kind](g, bsdf, tex, LP.dist_value(g, img), LP.preview_palette(kind))
    LP.liquid_plane(mat, LP.LEVEL[kind])
    LP.world_setup(tint)
    C.game_lights(2.2, 0.8)
    C.log("organic pool", style, "segments", n, "length", round(length, 3), "min radius", round(radius_stats(p), 2))
    return [g]


def render_style(style, out_dir, prev, samples):
    kind = STYLES[style][0]
    target = Vector((5.5, 4.5, 0.0))
    mats, _ = LP.build_scene(kind, out_dir)
    LP.set_time(mats, 1.0)
    sq = C.render(C.game_camera(target, 7.5), os.path.join(prev, f"bank_{style}_square.png"), 1280, 720, samples=samples)
    mats = organic_scene(style, out_dir)
    LP.set_time(mats, 1.0)
    org = C.render(C.game_camera(target, 7.5), os.path.join(prev, f"bank_{style}_organic.png"), 1280, 720,
                   samples=samples)
    C.render(C.game_camera(Vector((6.3, 4.6, 0.0)), 3.2), os.path.join(prev, f"bank_{style}_close.png"), 1280, 720,
             samples=samples)
    C.contact_sheet([sq, org], os.path.join(prev, f"bank_{style}_compare.png"), 2, 960)
    return sq, org


def run(prev, compare_dir=None, samples=24, only=None):
    prev = prev or os.path.join(C.REPO, "ArtSource", "Previews", "BankSweep")
    os.makedirs(prev, exist_ok=True)
    out_dir = os.path.join(C.REPO, "ArtSource", "Exports", "Liquids")
    paths = []
    for style in [s for s in STYLES if not only or s in only.split(",")]:
        paths += list(render_style(style, out_dir, prev, samples))
    C.contact_sheet(paths, os.path.join(prev, "bank_all.png"), 2, 800)
