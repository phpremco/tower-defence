"""Procedural 2D VFX sprites and flipbooks (round 20): projectiles, trails, beams, arcs, muzzle flashes, sparks,
shards, rings, distortion, scorch decals, cryo, acid and energy impacts.

Every builder returns (image, meta) where image is H x W x 4 float in 0..1 and meta goes into VFX_report.json.
Most emissive textures use the "CG pack":  R = core (white-hot, stays white under any tint), G = glow (multiply by
the weapon tint), B = a secondary layer named per texture, A = coverage max(R, G, B) for alpha-blended fallbacks.
Everything is linear data (import with sRGB off). Seeds are fixed, so a rebuild gives the same pixels.
"""

import numpy as np

import vfx_kit as K

TAU = 2.0 * np.pi


def pack_cg(core, glow, extra=None):
    core = np.clip(core, 0, 1)
    glow = np.clip(glow, 0, 1)
    extra = np.zeros_like(core) if extra is None else np.clip(extra, 0, 1)
    cov = np.maximum(np.maximum(core, glow), extra)
    return np.stack([core, glow, extra, cov], axis=-1).astype(np.float32)


def ridged(n, power):
    """Thin bright lines where the noise crosses zero, normalised so the result spans 0..1."""
    r = 1.0 - np.abs(n) / max(float(np.abs(n).max()), 1e-6)
    return r ** power


def px_grid(h, w):
    """Pixel coordinates in a frame whose height is 1 unit: x in [0, w/h), y in [0, 1) top-down."""
    y, x = np.mgrid[0:h, 0:w].astype(np.float64)
    return (x + 0.5) / h, (y + 0.5) / h


def capsule(px, py, ax, ay, bx, by):
    d, t = K.seg_dist(px, py, ax, ay, bx, by)
    return d, t


def voronoi(x, y, cells, seed):
    """Periodic voronoi on the unit square: (F1, F2, cell value)."""
    rng = np.random.default_rng(seed)
    off = rng.uniform(0.05, 0.95, (cells, cells, 2))
    val = rng.uniform(0, 1, (cells, cells))
    gx, gy = x * cells, y * cells
    ix, iy = np.floor(gx).astype(int), np.floor(gy).astype(int)
    f1 = np.full(x.shape, 9.0)
    f2 = np.full(x.shape, 9.0)
    v1 = np.zeros(x.shape)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            wx, wy = cx % cells, cy % cells
            d = np.hypot(gx - (cx + off[wy, wx, 0]), gy - (cy + off[wy, wx, 1]))
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            v1 = np.where(closer, val[wy, wx], v1)
            f1 = np.where(closer, d, f1)
    return f1, f2, v1


# ------------------------------------------------------------------------------------------------- projectiles

def bolt_core():
    """The glowing rounded bar every energy round uses: head at +U (direction of travel)."""
    h, w = 64, 256
    px, py = px_grid(h, w)
    a, b = (0.75, 0.5), (3.25, 0.5)
    d, t = capsule(px, py, *a, *b)
    r = 0.15
    core = K.smoothstep(r, r * 0.45, d) * (0.75 + 0.25 * t)
    # The halo leans backwards: a longer, thinner glow behind the bar reads as speed even without a trail.
    d_tail, _ = capsule(px, py, 0.12, 0.5, *b)
    glow = 0.75 * np.exp(-(d / 0.17) ** 2) + 0.35 * np.exp(-(d_tail / 0.09) ** 2)
    tail = np.exp(-(np.abs(py - 0.5) / 0.05) ** 2) * np.clip((px - 0.05) / (a[0] - 0.05), 0, 1) ** 2 * (px < a[0])
    return pack_cg(core, glow, tail * 0.8), dict(
        kind="sprite", layout="single", channels="CG: R core, G halo, B built-in tail streak, A coverage",
        blend="Additive", tint="weapon colour on G (Bolt, Rail, Flak tracer, BaseTurret); R stays white",
        notes="Head at +U. Use on a stretched billboard (length ~3x width) aligned to velocity.")


def trail_ribbon():
    """Stretch-mode trail: U = 0 at the projectile (newest), U = 1 at the tail; tapers and fades."""
    h, w = 64, 512
    u, v = K.grid(h, w)
    vv = np.abs(v - 0.5) * 2.0
    width = (1.0 - u) ** 0.7 + 0.02
    fade = (1.0 - u) ** 1.6
    core = np.exp(-(vv / (0.16 * width)) ** 2) * fade * K.smoothstep(0.0, 0.03, u)
    glow = np.exp(-(vv / (0.55 * width)) ** 2) * (1.0 - u) ** 1.1
    return pack_cg(core, glow), dict(
        kind="trail", layout="single, Texture Mode Stretch", channels="CG: R core, G glow, A coverage",
        blend="Additive", tint="weapon colour on G",
        notes="U=0 head (newest point), U=1 tail. If Unity's trail runs the other way set the material tiling X to -1."
              " Multiply with Trail_Noise scrolled along U for a living trail.")


def trail_noise():
    h, w = 64, 256
    u, v = K.grid(h, w)
    n = K.Perlin(11)
    strands = ridged(n.fbm(u * 4, v * 4, 0, 4, period=(4, 4, 256)), 5.0)
    wisps = 0.5 + 0.5 * K.Perlin(12).fbm(u * 2, v * 3, 0, 5, period=(2, 3, 256))
    return np.stack([strands, wisps, np.zeros_like(u), np.ones_like(u)], -1).astype(np.float32), dict(
        kind="tiling", layout="single, tiles in U and V", channels="R ridged strands, G soft wisps (0.5 mean), "
        "B unused, A 1", blend="multiply into a trail or beam (scroll U at 1-3 tiles/s)", tint="none (mask)")


def plasma_bolt(frames=16, size=128):
    """A looping plasma round: a white-hot rounded bar inside a churning mantle that streams back into tendrils."""
    n = K.Perlin(21)
    out = []
    px, py = px_grid(size, size)
    hx, cy = 0.78, 0.5
    d_core, _ = capsule(px, py, 0.5, cy, hx, cy)
    behind = np.clip((hx - px) / 0.7, 0, 1)
    for f in range(frames):
        t = f / frames * 4.0
        flow = n.fbm(px * 5 + t, py * 6, t, 4, period=(256, 256, 4))
        tend = ridged(n.fbm(px * 3 + t * 1.0, py * 7 + flow * 0.6, t, 3, period=(256, 256, 4)), 3.0)
        mantle_r = 0.11 + 0.1 * behind + 0.04 * flow
        dm, _ = capsule(px, py, 0.18, cy, hx, cy)
        mantle = K.smoothstep(mantle_r, mantle_r * 0.2, dm)
        core = K.smoothstep(0.075, 0.03, d_core)
        streams = tend * mantle * (0.4 + 0.6 * behind)
        glow = np.clip(np.exp(-(dm / 0.13) ** 2) * 0.5 + mantle * 0.45 + streams * 0.6, 0, 1)
        ang = np.arctan2(py - cy, px - hx)
        bands = (0.5 + 0.5 * np.sin(ang * 3 + d_core * 40 - f / frames * TAU * 2)) ** 3 * mantle * (1 - core)
        out.append(pack_cg(np.maximum(core, streams * 0.55), glow, np.maximum(bands * 0.6, streams * 0.8)))
    return K.atlas(out, 4, 4), dict(
        kind="flipbook", rows=4, cols=4, frames=frames, fps=24, loop=True, cell=[size, size],
        channels="CG: R core bar + hot streams, G mantle glow, B swirl bands and tendrils, A coverage",
        blend="Additive", tint="plasma colour on G, a second (lighter) colour on B",
        notes="Head at +U (U 0.78); loops seamlessly every 16 frames.")


def rocket_exhaust(frames=16):
    """Looping rocket flame seen from the side: nozzle at U=0 (left centre), flame towards +U, shock diamonds."""
    h, w = 128, 256
    n = K.Perlin(31)
    px, py = px_grid(h, w)
    out = []
    x = px / (w / h)  # 0..1 along the flame
    vv = np.abs(py - 0.5)
    for f in range(frames):
        t = f / frames * 4.0
        # Turbulence scrolls down the plume; its loop period matches the 16-frame cycle.
        flick = np.clip(2.5 * n.fbm(x * 5 - t, py * 4, t, 4, period=(256, 256, 4)), -1, 1)
        tongue = n.fbm(x * 2.5 - t * 0.5, py * 2, t + 7, 2, period=(256, 256, 4))
        length = 0.78 + 0.12 * tongue
        xl = np.clip(x / length, 0, 1.2)
        rad = (0.05 + 0.13 * np.sqrt(np.clip(xl, 0, 1))) * np.clip(1.15 - xl, 0, 1) ** 0.5 * (1 + 0.35 * flick)
        rad = np.maximum(rad, 1e-4)
        outer = K.smoothstep(rad, rad * 0.25, vv) * K.smoothstep(0.0, 0.03, x)
        heat = np.clip(1.0 - xl, 0, 1) ** 1.3
        outer = outer * heat * (0.75 + 0.5 * (0.5 + 0.5 * flick))
        cl = 0.3
        crad = np.maximum(0.055 * (1 - (x / cl) ** 2), 1e-4)
        core = K.smoothstep(crad + 0.01, crad * 0.3, vv) * (x < cl)
        dx = (x * 6.0) % 1.0
        diamond = np.clip(1 - np.abs(dx - 0.5) / 0.22 - vv / 0.028, 0, 1) * (x > 0.12) * (x < 0.62)
        diamond *= np.clip(1.0 - x / 0.62, 0, 1)
        smoke = K.smoothstep(0.22, 0.0, vv - 0.04 * flick) * K.smoothstep(0.5, 1.0, x) * (0.5 + 0.5 * flick)
        out.append(pack_cg(np.maximum(core, diamond), np.clip(np.maximum(outer, core) * 1.1, 0, 1), smoke * 0.5))
    return K.atlas(out, 4, 4), dict(
        kind="flipbook", rows=4, cols=4, frames=frames, fps=30, loop=True, cell=[w, h],
        channels="CG: R core + shock diamonds, G flame (bright at the nozzle, cooling to the tip), B heat/smoke "
                 "start (alpha-blend grey), A coverage",
        blend="Additive (R, G); B optional alpha layer", tint="G orange-yellow (1, 0.55, 0.2); R white",
        notes="Nozzle at U=0 centre, flame to +U: put the quad behind the rocket with +U opposite to velocity.")


def smoke_trail():
    """Tiling smoke ribbon for the rocket trail: R pre-lit from above, A density."""
    h, w = 128, 512
    u, v = K.grid(h, w)
    n = K.Perlin(41)
    puff = 0.5 + 0.5 * n.fbm(u * 4, v * 1.0, 0, 5, period=(4, 256, 256))
    prof = np.exp(-((v - 0.5) / (0.22 + 0.06 * puff)) ** 2)
    dens = np.clip(prof * (0.35 + 0.9 * puff) - 0.1, 0, 1)
    # Light from the top of the ribbon: integrate density downward like a one-direction six-way map.
    occl = np.cumsum(dens, axis=0) / h * 6.0
    lit = np.exp(-occl) * 0.8 + 0.2
    return np.stack([lit, lit * 0 + puff, np.zeros_like(u), dens], -1).astype(np.float32), dict(
        kind="trail/tiling", layout="single, tiles in U", channels="R lighting from the ribbon's top edge (0..1), "
        "G puff noise, B unused, A density", blend="Alpha blend (premultiply in shader: rgb = tint*R*A)",
        tint="smoke grey (0.35, 0.34, 0.33); fade the end of the trail with vertex alpha",
        notes="Tile mode in U (~1 tile per 2 m); scroll slowly so the trail churns.")


def beam_core():
    h, w = 64, 128
    u, v = K.grid(h, w)
    vv = np.abs(v - 0.5) * 2
    n = K.Perlin(51)
    wob = 1.0 + 0.12 * n.noise(u * 4, 0.3, 0, period=(4, 256, 256))
    core = K.smoothstep(0.14 * wob, 0.05, vv)
    glow = np.exp(-(vv / (0.42 * wob)) ** 2)
    return pack_cg(core, glow), dict(kind="beam", layout="single, tiles in U", channels="CG: R core, G glow",
                                     blend="Additive", tint="beam colour on G (Laser, CryoBeam)",
                                     notes="V across the beam, U along it; tile ~1 per beam width x4.")


def beam_noise():
    h, w = 128, 512
    u, v = K.grid(h, w)
    n1, n3 = K.Perlin(52), K.Perlin(54)
    fil = ridged(n1.fbm(u * 4, v * 3, 0, 4, period=(4, 3, 256)), 6.0)
    cr = voronoi_crackle(u, v)
    sw = 0.5 + 0.5 * n3.fbm(u * 2, v * 2, 0, 3, period=(2, 2, 256))
    return np.stack([fil, cr, sw, np.ones_like(u)], -1).astype(np.float32), dict(
        kind="tiling", layout="single, tiles in U and V",
        channels="R energy filaments (stretched along U), G crackle network, B soft swirl, A 1",
        blend="multiply into Beam_Core; scroll R at 2-4 tiles/s and G at -1 tile/s for two layers",
        tint="none (mask)")


def voronoi_crackle(u, v):
    """Thin bright cell borders on a 4:1 tile (8 x 2 cells, aspect corrected), periodic."""
    cells_x, cells_y = 8, 2
    rng = np.random.default_rng(55)
    off = rng.uniform(0.1, 0.9, (cells_y, cells_x, 2))
    gx, gy = u * cells_x, v * cells_y
    ix, iy = np.floor(gx).astype(int), np.floor(gy).astype(int)
    f1 = np.full(u.shape, 9.0)
    f2 = np.full(u.shape, 9.0)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx, cy = ix + dx, iy + dy
            d = np.hypot(gx - (cx + off[cy % cells_y, cx % cells_x, 0]), gy - (cy + off[cy % cells_y, cx % cells_x, 1]))
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
    return K.smoothstep(0.12, 0.0, f2 - f1)


# --------------------------------------------------------------------------------------------- lightning arcs

def _bolt_path(rng, a, b, depth, rough):
    pts = [np.array(a), np.array(b)]
    for lvl in range(depth):
        new = [pts[0]]
        amp = rough * 0.85 ** lvl  # offset is relative to the segment length (nrm is not normalised)
        for p, q in zip(pts[:-1], pts[1:]):
            mid = (p + q) / 2
            d = q - p
            nrm = np.array([-d[1], d[0]])
            mid = mid + nrm * rng.normal(0, amp)
            new += [mid, q]
        pts = new
    return pts


def _fit_band(pts, limit=0.36):
    """Scales a path's sideways deviation into the cell (clamping would leave flat runs along the border)."""
    dev = max(abs(y - 0.5) for _, y in pts)
    k = min(1.0, limit / max(dev, 1e-6))
    return [(x, 0.5 + (y - 0.5) * k) for x, y in pts]


def tesla_arcs(frames=16):
    """Branching arcs from (0, 0.5) to (1, 0.5) in each cell, stretched between tower and target in Unity."""
    h, w = 128, 512
    px, py = px_grid(h, w)
    asp = w / h
    out = []
    rng = np.random.default_rng(61)
    for f in range(frames):
        core = np.zeros(px.shape)
        glow = np.zeros(px.shape)
        branch = np.zeros(px.shape)
        main = _bolt_path(rng, (0.02 * asp, 0.5), (0.98 * asp, 0.5), 7, 0.3)
        main = _fit_band(main)
        m = len(main) - 1
        c, g = K.polyline_field(px, py, main, [0.012] * m, [0.032] * m, [1.0] * m)
        g = g * 0.75
        core, glow = np.maximum(core, c), np.maximum(glow, g)
        # A faint second strand (the previous discharge) keeps the arc from looking like one thin line.
        ghost = _fit_band(_bolt_path(rng, (0.02 * asp, 0.5), (0.98 * asp, 0.5), 6, 0.3))
        c, g = K.polyline_field(px, py, ghost, [0.008] * (len(ghost) - 1), [0.05] * (len(ghost) - 1),
                                [0.45] * (len(ghost) - 1))
        glow = np.maximum(glow, g * 0.7)
        branch = np.maximum(branch, c * 0.6)
        for _ in range(rng.integers(2, 5)):
            i = int(rng.integers(m // 6, m - m // 6))
            start = np.array(main[i])
            ang = rng.choice([-1, 1]) * rng.uniform(0.35, 1.0) + (0 if rng.random() < 0.8 else np.pi)
            ln = rng.uniform(0.25, 0.7)
            end = start + ln * np.array([np.cos(ang), np.sin(ang)])
            end[1] = np.clip(end[1], 0.08, 0.92)
            bp = _bolt_path(rng, start, end, 5, 0.3)
            k = len(bp) - 1
            fall = np.linspace(1.0, 0.3, k)
            c, g = K.polyline_field(px, py, bp, list(0.011 * fall), list(0.045 * fall), list(0.85 * fall))
            branch = np.maximum(branch, c)
            glow = np.maximum(glow, g * 0.8)
        end_glow = np.exp(-(np.hypot(px - 0.02 * asp, py - 0.5) / 0.09) ** 2) + \
            np.exp(-(np.hypot(px - 0.98 * asp, py - 0.5) / 0.09) ** 2)
        out.append(pack_cg(np.maximum(core, branch * 0.9), np.maximum(glow, end_glow), branch))
    return K.atlas(out, 4, 4), dict(
        kind="flipbook", rows=4, cols=4, frames=frames, fps=20, loop=True, cell=[w, h],
        channels="CG: R core (main + branches), G glow (+ end flares), B branches only, A coverage",
        blend="Additive", tint="Tesla violet (0.7, 0.5, 1) / StormRifle (0.6, 0.8, 1) on G",
        notes="Each cell runs from U=0.02 to U=0.98 at V=0.5: stretch a quad between muzzle and target; pick a "
              "random frame per discharge or play at 20 fps for a crackling stream.")


# ------------------------------------------------------------------------------------------- sparks and shards

def spark_burst(frames=36, size=256):
    """Ballistic sparks from an impact point slightly below the centre, motion-blurred, cooling as they fly."""
    rng = np.random.default_rng(71)
    nsp = 56
    ang = rng.uniform(0, TAU, nsp)
    up = -np.abs(np.sin(ang)) * 0.4  # bias upwards (image y grows down)
    spd = rng.uniform(0.6, 1.8, nsp) * np.where(rng.random(nsp) < 0.25, 1.6, 1.0)
    vx, vy = np.cos(ang) * spd, np.sin(ang) * spd * 0.8 + up
    life = rng.uniform(0.35, 1.0, nsp)
    width = rng.uniform(0.004, 0.009, nsp)
    origin = np.array([0.5, 0.58])
    grav, drag = 1.4, 1.3
    px, py = px_grid(size, size)
    out = []
    dur = 0.6

    def pos(t):
        k = (1 - np.exp(-drag * t)) / drag
        return origin[0] + vx * k * 0.75, origin[1] + vy * k * 0.75 + 0.5 * grav * t * t * 0.75

    for f in range(frames):
        t = (f + 0.5) / frames * dur
        t0 = max(t - 0.06, 0.0)
        x1, y1 = pos(t)
        x0, y0 = pos(t0)
        lf = t / (life * dur)
        alive = lf < 1
        core = np.zeros(px.shape)
        glow = np.zeros(px.shape)
        for i in np.nonzero(alive)[0]:
            heat = (1 - lf[i]) ** 1.5 * (0.7 + 0.3 * rng.random())
            c, g = K.polyline_field(px, py, [(x0[i], y0[i]), (x1[i], y1[i])], [width[i] * (1.2 - 0.5 * lf[i])],
                                    [width[i] * 3.5], [heat])
            core, glow = np.maximum(core, c), np.maximum(glow, g)
        flash = np.exp(-(np.hypot(px - origin[0], py - origin[1]) / (0.04 + 0.1 * t)) ** 2) * np.exp(-t * 40)
        out.append(pack_cg(np.maximum(core, flash), np.maximum(glow, flash * 0.8)))
    return K.atlas(out, 6, 6), dict(
        kind="flipbook", rows=6, cols=6, frames=frames, fps=60, loop=False, cell=[size, size],
        lifetime=0.6, pivot=[0.5, 0.42], channels="CG: R spark cores + flash, G glow, A coverage",
        blend="Additive", tint="hot amber (1, 0.6, 0.2) for shells; weapon colour for energy hits",
        notes="Pivot = impact point (U 0.5, V 0.42 from the bottom). Pair with mesh/GPU sparks using Spark_Streaks.")


def spark_streaks():
    """Four streak sprites for stretched-billboard spark particles: hot head at +U, cooling tail."""
    rows = []
    rng = np.random.default_rng(72)
    h, w = 64, 512
    px, py = px_grid(h, w)
    asp = w / h
    for i in range(4):
        head = asp * (0.9 + 0.04 * i * 0)
        tail = asp * (0.06 + 0.1 * i)
        x = np.clip((px - tail) / (head - tail), 0, 1)
        wid = 0.06 * (0.3 + 0.7 * x ** 0.7) * (1.0 + 0.3 * i / 3)
        dv = np.abs(py - 0.5)
        inside = (px > tail) & (px < head)
        dcap = np.hypot(np.maximum(px - head, 0), dv)
        d = np.where(inside | (px >= head), np.where(px >= head, dcap, dv), 9.0)
        core = K.smoothstep(wid, wid * 0.3, d) * (0.25 + 0.75 * x ** 2) * (inside | (px >= head))
        if i == 3:  # broken, flickering streak (a spark tumbling)
            core *= 0.55 + 0.45 * np.sin(px * 9 + rng.uniform(0, 6)) ** 2
        glow = np.exp(-(d / (wid * 3.2)) ** 2) * (0.15 + 0.85 * x ** 1.5)
        rows.append(pack_cg(core, glow))
    return np.concatenate(rows, axis=0), dict(
        kind="sprite atlas", rows=4, cols=1, frames=4, cell=[w, h],
        channels="CG: R core, G glow, A coverage", blend="Additive",
        tint="amber (1, 0.6, 0.2) or weapon colour; fade R and G with particle life",
        notes="Head at +U. Stretched Billboard render mode, speed scale ~0.05, random row per particle "
              "(Texture Sheet Animation, rows mode, random row).")


def debris_shards(size=128):
    """4 x 4 shrapnel silhouettes for alpha-blended debris: dark metal/rock with a glowing hot edge."""
    rng = np.random.default_rng(73)
    out = []
    px, py = px_grid(size, size)
    n = K.Perlin(74)
    for i in range(16):
        k = int(rng.integers(4, 8))
        ang = np.sort(rng.uniform(0, TAU, k))
        rad = rng.uniform(0.18, 0.42, k) * np.array([1.0 if j % 2 == 0 else rng.uniform(0.5, 0.9) for j in range(k)])
        stretch = rng.uniform(1.0, 1.8)
        rot = rng.uniform(0, TAU)
        pts = [(0.5 + r * np.cos(a + rot) * stretch * 0.75, 0.5 + r * np.sin(a + rot) * 0.75) for a, r in
               zip(ang, rad)]
        # Signed distance to the polygon (negative inside).
        inside = np.zeros(px.shape, bool)
        dmin = np.full(px.shape, 9.0)
        for j in range(k):
            (ax, ay), (bx, by) = pts[j], pts[(j + 1) % k]
            d, _ = K.seg_dist(px, py, ax, ay, bx, by)
            dmin = np.minimum(dmin, d)
            cond = ((ay > py) != (by > py)) & (px < (bx - ax) * (py - ay) / (by - ay + 1e-12) + ax)
            inside ^= cond
        sd = np.where(inside, -dmin, dmin)
        body = K.smoothstep(0.008, -0.008, sd)
        # Faceted shading: a flat normal per polygon wedge, lit from the top left.
        wedge = np.floor(((np.arctan2(py - 0.5, px - 0.5) - rot) % TAU) / TAU * k)
        facet = 0.35 + 0.4 * np.cos(wedge * 2.4 + i) ** 2 + 0.1 * n.noise(px * 12, py * 12, i)
        hot_side = np.clip(0.5 + 0.5 * np.cos(np.arctan2(py - 0.5, px - 0.5) - rng.uniform(0, TAU)), 0, 1)
        edge = K.smoothstep(-0.045, -0.005, sd) * body * hot_side ** 1.5 * (0.6 + 0.4 * n.noise(px * 20, py * 20, i))
        out.append(np.stack([edge, body, facet * body, body], -1))
    return K.atlas(out, 4, 4), dict(
        kind="sprite atlas", rows=4, cols=4, frames=16, cell=[size, size],
        channels="R hot edge (emission), G body alpha, B facet shading, A body alpha",
        blend="Alpha blend body (colour = dark tint * B) + additive emission from R",
        tint="body dark metal/rock (0.12-0.2); edge amber, fade the edge to 0 over the particle life",
        notes="Random frame per particle, rotate over life, gravity on. For flying chunks behind explosions.")


# ----------------------------------------------------------------------------------- rings, distortion, decals

def shockwave_ring(size=512):
    x, y = K.grid(size, size)
    dx, dy = x - 0.5, y - 0.5
    r = np.hypot(dx, dy) * 2
    ang = np.arctan2(dy, dx)
    n = K.Perlin(81)
    br = 0.5 + 0.5 * n.fbm(np.cos(ang) * 3 + 10, np.sin(ang) * 3 + 10, r * 4, 4)
    rr = 0.9
    edge = np.where(r < rr, np.exp(-((rr - r) / 0.03) ** 2), np.exp(-((r - rr) / 0.012) ** 2))
    inner = np.where(r < rr, np.exp(-((rr - r) / 0.13) ** 2), np.exp(-((r - rr) / 0.02) ** 2))
    dust = K.smoothstep(0.35, 0.8, br) * inner
    return pack_cg(edge * (0.5 + 0.5 * br), inner * (0.35 + 0.65 * br) ** 1.5, dust), dict(
        kind="sprite", layout="single", channels="CG: R sharp front, G soft inner glow, B dusty breakup, A coverage",
        blend="Additive (energy) or alpha-blend B as a dust ring", tint="weapon colour on G; dust tan on B",
        notes="Scale the quad from ~0.2 to full size with an ease-out over 0.25-0.4 s; flat on the ground for "
              "ground hits, camera-facing for air bursts.")


def _encode_offset(ox, oy, mask):
    m = max(float(np.abs(ox).max()), float(np.abs(oy).max()), 1e-6)
    return np.stack([0.5 + 0.5 * ox / m, 0.5 + 0.5 * oy / m, mask, np.ones_like(ox)], -1).astype(np.float32)


def shockwave_distort(size=512):
    """Refraction offsets for the local ripple: a ring lens that pushes pixels outward at its front."""
    x, y = K.grid(size, size)
    dx, dy = x - 0.5, y - 0.5
    r = np.hypot(dx, dy) * 2 + 1e-6
    rr, wd = 0.8, 0.12
    s = (r - rr) / wd
    hgt = np.exp(-s * s)
    dh = -2 * s * hgt  # radial derivative of the bump (per wd)
    env = K.smoothstep(1.0, 0.92, r) * K.smoothstep(0.3, 0.55, r)
    ox, oy = dh * dx / r * env, -dh * dy / r * env  # +Y up in the texture (OpenGL)
    mask = np.clip(hgt * 1.2, 0, 1) * env
    return _encode_offset(ox, oy, mask), dict(
        kind="distortion", layout="single", channels="R,G UV offset x/y encoded 0.5 +- 0.5 (+Y up), B ring mask, A 1",
        blend="Refraction pass: sample _CameraOpaqueTexture at screenUV + (RG*2-1) * B * strength (0.02-0.05)",
        tint="none", notes="Import as Default, sRGB off, NOT as a Normal Map (Unity would drop B). Scale the quad "
                           "outward like the shockwave ring; fade strength to 0.")


def heat_haze(size=256):
    x, y = K.grid(size, size)
    n = K.Perlin(83)
    hgt = n.fbm(x * 4, y * 4, 0, 4, period=(4, 4, 256))
    gx = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1))
    gy = -(np.roll(hgt, -1, 0) - np.roll(hgt, 1, 0))
    return _encode_offset(gx, -gy, 0.5 + 0.5 * hgt), dict(
        kind="distortion/tiling", layout="single, tiles in U and V",
        channels="R,G UV offset x/y encoded 0.5 +- 0.5, B height (for a mask), A 1",
        blend="Refraction pass; scroll V upward 0.3-0.6 tiles/s over hot spots, strength 0.004-0.01",
        tint="none", notes="Import as Default, sRGB off. Fade with a radial mask on the quad (vertex alpha).")


def scorch_decals(size=512):
    """Four ground scorch variants, 2 x 2: soot, ash lightness, embers and a per-ember fade threshold."""
    out = []
    for i in range(4):
        rng = np.random.default_rng(90 + i)
        x, y = K.grid(size, size)
        dx, dy = (x - 0.5) * 2, (y - 0.5) * 2
        r = np.hypot(dx, dy)
        ang = np.arctan2(dy, dx)
        n = K.Perlin(91 + i)
        rays = n.fbm(np.cos(ang) * 4, np.sin(ang) * 4, 7.0 * i, 4)
        rays = np.clip(rays, 0, 1) ** 1.5
        blot = n.fbm(dx * 3, dy * 3, 3.3, 5)
        radius = 0.5 + 0.2 * blot + 0.45 * rays
        soot = K.smoothstep(radius, radius * 0.45, r) * (0.75 + 0.25 * n.fbm(dx * 9, dy * 9, 1.0, 3))
        splat = K.smoothstep(0.5, 0.75, n.fbm(dx * 7, dy * 7, 9.0 + i, 3)) * K.smoothstep(1.0, 0.5, r)
        soot = np.clip(np.maximum(soot, splat * 0.6), 0, 1)
        ash = np.clip(0.08 + 0.25 * K.smoothstep(0.1, 0.6, n.fbm(dx * 6, dy * 6, 4.0, 4)) * K.smoothstep(0.2, 0.6, r)
                      + 0.1 * K.smoothstep(0.25, 0.0, r), 0, 1)
        # Warped cells so the crack network meanders instead of reading as a tiled polygon pattern.
        wx = x + 0.035 * n.fbm(x * 6, y * 6, 20.0 + i, 3)
        wy = y + 0.035 * n.fbm(x * 6, y * 6, 40.0 + i, 3)
        f1, f2, cv = voronoi((wx + i * 0.31) % 1.0, (wy + i * 0.17) % 1.0, 7, 95 + i)
        broken = K.smoothstep(-0.05, 0.2, n.fbm(x * 10, y * 10, 60.0 + i, 3))
        cracks = K.smoothstep(0.035, 0.0, f2 - f1) * broken
        speck = K.smoothstep(0.08, 0.02, f1) * (cv > 0.55)
        core_zone = K.smoothstep(0.65, 0.15, r)
        embers = np.clip((cracks * 0.8 + speck) * core_zone * (0.5 + 0.5 * n.fbm(dx * 14, dy * 14, 2.0, 2)), 0, 1)
        # Threshold: per voronoi cell, higher near the centre, so the outer embers die first.
        thresh = np.clip(cv * 0.45 + core_zone * 0.55, 0, 1)
        out.append(np.stack([soot, ash, embers, thresh], -1))
    return K.atlas(out, 2, 2), dict(
        kind="decal atlas", rows=2, cols=2, frames=4, cell=[size, size],
        channels="R soot opacity, G albedo lightness (char 0.08 to ash 0.4), B ember emission mask, "
                 "A ember fade threshold (an ember is lit while A > fade, fade 0 to 1 over the decal life)",
        blend="URP Decal Projector or a ground quad (alpha blend): albedo = G * soot tint, alpha = R * fade-out; "
              "emission = B * step(fade, A) * ember colour (1, 0.35, 0.08) * 3",
        tint="soot near-black brown, embers orange", lifetime=8.0,
        notes="Pick a random cell and rotation per impact. Embers out in ~2 s, soot fades over ~8 s.")


# --------------------------------------------------------------------------------------------- muzzle flashes

def _petal(px, py, ox, oy, ang, length, width, n, seed, sharp=1.0):
    """A leaf-shaped flame tongue from (ox, oy) along `ang`: 0..1 mask with a soft edge and a hot base."""
    ca, sa = np.cos(ang), np.sin(ang)
    u = (px - ox) * ca + (py - oy) * sa
    v = -(px - ox) * sa + (py - oy) * ca
    t = u / length
    jag = 1.0 + 0.25 * n.noise(u * 18 + seed, v * 18, seed)
    half = width * np.clip(np.sin(np.pi * np.clip(t, 0, 1) ** 0.7), 0, 1) ** sharp * jag + 1e-4
    m = K.smoothstep(half, half * 0.3, np.abs(v)) * (t > 0) * (t < 1)
    return m * np.clip(1.0 - t, 0, 1) ** 0.6


def _muzzle(kind, view, seed, h, w):
    rng = np.random.default_rng(seed)
    n = K.Perlin(seed)
    px, py = px_grid(h, w)
    core = np.zeros(px.shape)
    glow = np.zeros(px.shape)
    smoke = np.zeros(px.shape)
    if view == "side":
        ox, oy = 0.05, 0.5
        main_len = {"gun": 1.3, "cannon": 1.65, "flak": 0.9, "rail": 1.85}[kind] * rng.uniform(0.85, 1.1)
        main_w = {"gun": 0.13, "cannon": 0.24, "flak": 0.16, "rail": 0.05}[kind]
        m = _petal(px, py, ox, oy, 0.0 if kind == "rail" else rng.normal(0, 0.03), main_len, main_w, n, 1)
        glow = np.maximum(glow, m)
        core = np.maximum(core, _petal(px, py, ox, oy, 0, main_len * 0.45, main_w * 0.45, n, 2, 0.6))
        sides = {"gun": 2, "cannon": 4, "flak": 4, "rail": 0}[kind]
        for j in range(sides):
            sgn = 1 if j % 2 == 0 else -1
            base_ang = {"gun": 0.6, "cannon": 0.55, "flak": 1.35}[kind] + 0.35 * (j // 2)
            a = sgn * (base_ang + rng.normal(0, 0.08))
            ln = min(main_len * rng.uniform(0.25, 0.38), 0.4 if kind == "flak" else 0.6)
            pm = _petal(px, py, ox + 0.03, oy, a, ln, main_w * 0.45, n, 3 + j, 1.4)
            glow = np.maximum(glow, pm * 0.8)
            core = np.maximum(core, pm * 0.12)
        if kind == "cannon":
            sm = n.fbm(px * 5, py * 5, seed, 4)
            smoke = K.smoothstep(0.42, 0.1, np.hypot((px - ox - 0.55) * 0.75, py - oy)) * (0.6 + 0.6 * sm)
        if kind == "rail":
            for j, xr in enumerate((0.35, 0.75, 1.2)):
                d = np.abs(np.hypot((px - ox - xr) / 0.035, (py - oy) / (0.16 + 0.05 * j)) - 1.0)
                ring = np.exp(-(d / 0.18) ** 2) * (1 - j * 0.25)
                glow = np.maximum(glow, ring * 0.9)
                core = np.maximum(core, ring * 0.5)
        core = np.maximum(core, np.exp(-(np.hypot(px - ox - 0.04, py - oy) / 0.05) ** 2))
    else:
        cx, cy = 0.5, 0.5
        r = np.hypot(px - cx, py - cy)
        ang = np.arctan2(py - cy, px - cx)
        count = {"gun": int(rng.integers(3, 6)), "cannon": int(rng.integers(6, 9)), "flak": 4, "rail": 0}[kind]
        rot = rng.uniform(0, TAU)
        for j in range(count):
            a = rot + j / max(count, 1) * TAU + rng.normal(0, 0.15)
            ln = {"gun": 0.42, "cannon": 0.46, "flak": 0.48}[kind] * rng.uniform(0.7, 1.05)
            wd = {"gun": 0.08, "cannon": 0.1, "flak": 0.035}[kind]
            pm = _petal(px, py, cx, cy, a, ln, wd, n, 5 + j, 1.0 if kind != "flak" else 0.5)
            glow = np.maximum(glow, pm)
            core = np.maximum(core, pm * (0.15 if kind != "flak" else 0.7))
        core_r = {"gun": 0.08, "cannon": 0.14, "flak": 0.07, "rail": 0.09}[kind]
        core = np.maximum(core, K.smoothstep(core_r, core_r * 0.3, r * (1 + 0.15 * n.noise(np.cos(ang) * 3,
                                                                                         np.sin(ang) * 3, seed))))
        glow = np.maximum(glow, 0.7 * np.exp(-(r / (core_r * 1.8)) ** 2))
        if kind == "cannon":
            sm = 0.5 + 0.5 * n.fbm(px * 6, py * 6, seed, 4)
            smoke = K.smoothstep(0.48, 0.25, r) * K.smoothstep(0.1, 0.3, r) * sm
        if kind == "rail":
            for j, rr in enumerate((0.18, 0.3, 0.42)):
                ring = np.exp(-((r - rr) / 0.012) ** 2) * (1 - 0.25 * j)
                glow = np.maximum(glow, ring)
                core = np.maximum(core, ring * 0.6)
        if kind == "flak":
            for j in range(10):
                a = rng.uniform(0, TAU)
                d0, d1 = rng.uniform(0.12, 0.25), rng.uniform(0.3, 0.46)
                c, g = K.polyline_field(px, py, [(cx + d0 * np.cos(a), cy + d0 * np.sin(a)),
                                                 (cx + d1 * np.cos(a), cy + d1 * np.sin(a))], [0.006], [0.02], [0.9])
                core, glow = np.maximum(core, c), np.maximum(glow, g * 0.8)
    return pack_cg(core, glow, smoke * 0.8)


def muzzle_flashes():
    kinds = ["gun", "cannon", "flak", "rail"]
    side = [_muzzle(k, "side", 200 + 10 * i + j, 128, 256) for i, k in enumerate(kinds) for j in range(4)]
    front = [_muzzle(k, "front", 300 + 10 * i + j, 256, 256) for i, k in enumerate(kinds) for j in range(4)]
    meta = dict(kind="sprite atlas", rows=4, cols=4, frames=16,
                channels="CG: R core, G flame glow, B smoke (cannon rows; alpha-blend grey), A coverage",
                blend="Additive (R, G)", tint="G per weapon: gun (1, 0.75, 0.35), cannon (1, 0.6, 0.22), "
                "flak white-amber (1, 0.85, 0.55), rail/plasma weapon colour; R stays white",
                lifetime=0.05, notes="Rows = Gun, Cannon, Flak, Rail/Plasma; columns = 4 random variants. "
                "Show 1-3 frames (50 ms), random column per shot, random roll for the front view.")
    side_meta = dict(meta, cell=[256, 128], view="side: barrel at U=0.05 V=0.5, firing towards +U")
    front_meta = dict(meta, cell=[256, 256], view="front: barrel axis through the centre (camera looks down it)")
    return (K.atlas(side, 4, 4), side_meta), (K.atlas(front, 4, 4), front_meta)


# ----------------------------------------------------------------------------------------- impact flipbooks

def energy_impact(frames=36, size=256):
    rng = np.random.default_rng(401)
    px, py = px_grid(size, size)
    cx, cy = 0.5, 0.5
    r = np.hypot(px - cx, py - cy)
    ang = np.arctan2(py - cy, px - cx)
    n = K.Perlin(402)
    nsp = 22
    sa = rng.uniform(0, TAU, nsp)
    ss = rng.uniform(0.6, 1.4, nsp)
    sl = rng.uniform(0.4, 1.0, nsp)
    out = []
    for f in range(frames):
        t = f / (frames - 1)
        e = 1 - (1 - t) ** 3
        flash = np.exp(-(r / (0.05 + 0.08 * t)) ** 2) * np.exp(-t * 9) * 1.2
        r1 = 0.06 + 0.38 * e
        brk = 0.75 + 0.25 * n.noise(np.cos(ang) * 4, np.sin(ang) * 4, t * 3)
        ring1 = np.exp(-((r - r1) / (0.008 + 0.012 * t)) ** 2) * (1 - t) ** 2.4 * brk
        r2 = 0.04 + 0.22 * (1 - (1 - t) ** 2)
        ring2 = np.exp(-((r - r2) / 0.03) ** 2) * (1 - t) ** 2 * 0.7
        core = np.maximum(flash, ring1 * 0.8)
        glow = np.maximum(np.maximum(flash, ring1), ring2) + np.exp(-(r / 0.16) ** 2) * (1 - t) ** 3 * 0.6
        for i in range(nsp):
            lt = t / (sl[i] * 0.6)
            if lt >= 1:
                continue
            d0 = 0.03 + 0.42 * ss[i] * (1 - (1 - min(t * 1.3, 1)) ** 2) * 0.8
            d1 = max(d0 - 0.05 * ss[i] * (1 - lt), 0.02)
            p0 = (cx + d1 * np.cos(sa[i]), cy + d1 * np.sin(sa[i]))
            p1 = (cx + d0 * np.cos(sa[i]), cy + d0 * np.sin(sa[i]))
            c, g = K.polyline_field(px, py, [p0, p1], [0.007], [0.025], [(1 - lt) ** 1.2])
            core, glow = np.maximum(core, c), np.maximum(glow, g)
        out.append(pack_cg(core, glow, ring2))
    return K.atlas(out, 6, 6), dict(
        kind="flipbook", rows=6, cols=6, frames=frames, fps=60, loop=False, cell=[size, size], lifetime=0.6,
        channels="CG: R flash + ring front + spark cores, G glow, B inner ring, A coverage", blend="Additive",
        tint="weapon colour on G (Bolt/Laser/Rail hits); B can take a second colour",
        notes="Camera-facing at the hit point; pair with Shockwave_Distort at a small strength.")


def cryo_burst(frames=36, size=256):
    rng = np.random.default_rng(501)
    px, py = px_grid(size, size)
    cx, cy = 0.5, 0.5
    r = np.hypot(px - cx, py - cy)
    ang = np.arctan2(py - cy, px - cx)
    n = K.Perlin(502)
    nsh = 14
    sa = np.sort(rng.uniform(0, TAU, nsh))
    sl = rng.uniform(0.22, 0.42, nsh)
    sw = rng.uniform(0.025, 0.05, nsh)
    glints = rng.uniform(0.08, 0.45, (18, 2)) * np.array([1, 1])
    ga = rng.uniform(0, TAU, 18)
    gt = rng.uniform(0.1, 0.7, 18)
    out = []
    for f in range(frames):
        t = f / (frames - 1)
        grow = 1 - (1 - min(t / 0.18, 1)) ** 3
        fade = 1 - K.smoothstep(0.35, 0.85, t)
        core = np.zeros(px.shape)
        glow = np.zeros(px.shape)
        for i in range(nsh):
            ln = sl[i] * grow
            if ln < 0.01:
                continue
            ca, sn = np.cos(sa[i]), np.sin(sa[i])
            u = (px - cx) * ca + (py - cy) * sn
            v = -(px - cx) * sn + (py - cy) * ca
            tt = u / ln
            half = sw[i] * np.clip(1 - tt, 0, 1) * (tt > 0)
            body = K.smoothstep(half, half * 0.5, np.abs(v)) * (tt > 0) * (tt < 1)
            # Facet ridge: the spine of each shard catches light.
            ridge = K.smoothstep(half * 0.35, 0.0, np.abs(v - half * 0.25)) * body
            shatter = 1 - K.smoothstep(0.45, 0.8, t + 0.25 * n.noise(u * 30, v * 30, i))
            core = np.maximum(core, ridge * shatter * fade)
            glow = np.maximum(glow, body * 0.85 * shatter * fade)
        mist_r = 0.1 + 0.33 * (1 - (1 - t) ** 2)
        mn = 0.5 + 0.5 * n.fbm(px * 6 + t, py * 6, t * 2, 4)
        mist = K.smoothstep(mist_r + 0.08, mist_r - 0.12, r) * K.smoothstep(mist_r - 0.35, mist_r, r) * mn
        mist *= (1 - t) ** 1.3 * 1.3
        flash = np.exp(-(r / 0.09) ** 2) * np.exp(-t * 10)
        for i in range(18):
            if t < gt[i] or t > gt[i] + 0.25:
                continue
            k = np.sin((t - gt[i]) / 0.25 * np.pi)
            gx, gy = cx + glints[i, 0] * np.cos(ga[i]), cy + glints[i, 0] * np.sin(ga[i])
            dxg, dyg = np.abs(px - gx), np.abs(py - gy)
            star = (np.exp(-(dxg / 0.004) ** 2 - (dyg / 0.03) ** 2) + np.exp(-(dyg / 0.004) ** 2 - (dxg / 0.03) ** 2))
            core = np.maximum(core, star * k)
        out.append(pack_cg(np.maximum(core, flash), np.maximum(glow, flash), np.clip(mist, 0, 1)))
    return K.atlas(out, 6, 6), dict(
        kind="flipbook", rows=6, cols=6, frames=frames, fps=48, loop=False, cell=[size, size], lifetime=0.75,
        channels="CG: R shard ridges + glints + flash, G shard bodies, B frost mist, A coverage",
        blend="Additive R/G; B alpha-blended pale mist", tint="G ice cyan (0.55, 0.95, 1); B mist (0.8, 0.95, 1)",
        notes="Flat on the ground at the hit point (or camera-facing for air targets).")


def acid_splash(frames=36, size=256):
    """Metaball splash: a burst of droplets on ballistic arcs, a crown, then drips falling off the main blob."""
    rng = np.random.default_rng(601)
    px, py = px_grid(size, size)
    nd = 26
    ang = rng.uniform(-np.pi * 0.95, -np.pi * 0.05, nd)  # upward hemisphere (y down)
    spd = rng.uniform(0.35, 1.0, nd)
    rad = rng.uniform(0.012, 0.03, nd)
    origin = np.array([0.5, 0.66])
    g = 2.4
    drips = [(rng.uniform(0.40, 0.60), rng.uniform(0.35, 0.65), rng.uniform(0.012, 0.02)) for _ in range(4)]
    out = []
    for f in range(frames):
        t = f / (frames - 1) * 0.9
        field = np.zeros(px.shape)
        main_r = 0.075 * np.exp(-t * 2.5) + 0.03
        field += np.exp(-((px - origin[0]) ** 2 / (main_r * 1.6) ** 2 + (py - origin[1] - 0.02) ** 2 / main_r ** 2))
        # Crown: a ring of lumps that rises and spreads early on.
        if t < 0.4:
            k = 0.15 + 0.85 * t / 0.4
            for j in range(9):
                a = -np.pi + j / 8 * np.pi
                cxj = origin[0] + np.cos(a) * (0.08 + 0.12 * k)
                cyj = origin[1] - 0.02 - np.abs(np.sin(a)) * 0.08 * np.sin(np.pi * k)
                field += np.exp(-((px - cxj) ** 2 + (py - cyj) ** 2) / (0.022 * (1 - k * 0.6)) ** 2)
        for i in range(nd):
            vx, vy = np.cos(ang[i]) * spd[i], np.sin(ang[i]) * spd[i]
            x = origin[0] + vx * t * 0.55
            y = origin[1] + vy * t * 0.55 + 0.5 * g * t * t * 0.55
            if y > 1.1:
                continue
            vyt = vy + g * t
            sp = np.hypot(vx, vyt) + 1e-6
            ux, uy = vx / sp, vyt / sp
            du = (px - x) * ux + (py - y) * uy
            dv = -(px - x) * uy + (py - y) * ux
            rr = rad[i] * (1 - 0.4 * t)
            field += 1.2 * np.exp(-(du / (rr * (1 + sp * 1.2))) ** 2 - (dv / rr) ** 2)
        for (dxp, start, dr) in drips:
            if t < start:
                continue
            k = (t - start) / (0.9 - start)
            yy = origin[1] + 0.02 + 0.7 * k * k
            field += np.exp(-((px - dxp) ** 2 / dr ** 2 + (py - yy) ** 2 / (dr * 1.8) ** 2))
            field += 0.6 * np.exp(-((px - dxp) ** 2 / (dr * 0.5) ** 2) - np.maximum(0, py - origin[1]) * 0
                                  ) * ((py > origin[1]) & (py < yy)) * (1 - k)
        alpha = K.smoothstep(0.35, 0.55, field)
        gx = np.gradient(field, axis=1) * size * 0.02
        gy = np.gradient(field, axis=0) * size * 0.02
        nz = 1.0 / np.sqrt(1 + gx * gx + gy * gy)
        nx, ny = -gx * nz, -gy * nz
        lx, ly, lz = -0.5, -0.6, 0.62  # light from the top-left (y down)
        diff = np.clip(nx * lx + ny * ly + nz * lz, 0, 1)
        hx, hy, hz = lx, ly, lz + 1.0
        hl = np.sqrt(hx * hx + hy * hy + hz * hz)
        spec = np.clip((nx * hx + ny * hy + nz * hz) / hl, 0, 1) ** 40
        shade = np.clip(0.25 + 0.55 * diff + 1.2 * spec, 0, 1)
        thick = np.clip(field / 1.5, 0, 1)
        fade = 1 - K.smoothstep(0.75, 0.9, t)
        out.append(np.stack([shade * alpha, thick * alpha, spec * alpha, alpha * fade], -1).astype(np.float32))
    return K.atlas(out, 6, 6), dict(
        kind="flipbook", rows=6, cols=6, frames=frames, fps=40, loop=False, cell=[size, size], lifetime=0.9,
        pivot=[0.5, 0.34],
        channels="R lit shading (diffuse + highlight, premultiplied by alpha), G thickness, B specular only, A alpha",
        blend="Alpha blend premultiplied: rgb = lerp(acid dark, acid bright, G) * R + B * 0.6, emission = G * glow",
        tint="acid lime (0.55, 1, 0.15) to dark olive; a little emission on thick parts",
        notes="Pivot = splash origin (U 0.5, V 0.34 from the bottom). Camera-facing.")
