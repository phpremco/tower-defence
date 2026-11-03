"""Surface styles for our own side (round 6: the hero; reusable for towers and drones later).

Same contract as chitin.py: a style(s, idx, part) colours, displaces and lights the vertices a part owns. The
micro-detail families are surface.SUIT_FAMILIES (Fam R weave, G ceramic, B metal, A rubber); surface.py turns them
into stitched seams, scratched and chipped plates, brushed metal and moulded rubber at bake time.

Look rules (CLAUDE.md, look feedback): cool technical colours (navy blue, white, yellow, cyan light), three clearly
separated values (white plates, mid-blue suit, gunmetal gear), a light top and a darker underside, and visible wear.
"""

import numpy as np

import common as C

FAMILY_SET = ("weave", "ceramic", "metal", "rubber")
# Stitched quilting seams: the per-pixel stitch lines in surface.weave_detail repeat every 1 / STITCH_FREQ units, so
# the geometric quilt bands use the same spacing and both line up.
STITCH_FREQ = 20.0


class Palette:
    def __init__(self, **kw):
        for key, value in kw.items():
            setattr(self, key, np.array(value, dtype=float))


# sRGB, as in a paint program (common.detail converts to linear).
PAL = Palette(
    plate_top=(0.9, 0.91, 0.89), plate_side=(0.74, 0.76, 0.76), plate_under=(0.42, 0.45, 0.5),
    suit_top=(0.24, 0.37, 0.62), suit_side=(0.16, 0.26, 0.46), suit_under=(0.07, 0.11, 0.2), suit_seam=(0.05, 0.08, 0.15),
    metal_top=(0.27, 0.29, 0.33), metal_side=(0.17, 0.18, 0.21), metal_under=(0.08, 0.085, 0.1),
    rubber=(0.075, 0.08, 0.09),
    yellow=(0.98, 0.74, 0.14), cyan=(0.32, 0.93, 1.0), glass=(0.02, 0.035, 0.055), glass_sheen=(0.25, 0.42, 0.55),
    dust=(0.43, 0.4, 0.37), crysium=(0.86, 0.72, 0.84), crysium_glint=(1.0, 0.75, 0.95), substrate=(0.35, 0.37, 0.4),
)


def family(s, idx, weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0, flow=None):
    w = np.c_[np.broadcast_to(weave, len(idx)), np.broadcast_to(ceramic, len(idx)),
              np.broadcast_to(metal, len(idx)), np.broadcast_to(rubber, len(idx))]
    s.fam[idx] = w
    if flow is not None:
        s.flow[idx] = flow


def mottle(s, idx, amount, scale, seed):
    return 1.0 + C.value_noise(s.p[idx], scale, seed)[:, None] * amount


class Ctx:
    """What a feature mask may look at: world points and normals, the part's local frame (unit sphere for
    ellipsoids) and, for tubes, the position along the tube, its radius and the distance from its axis."""

    def __init__(self, s, idx, part):
        self.p = s.p[idx]
        self.n = s.n[idx]
        self.L = C.local_points(part, self.p)
        norm = np.linalg.norm(self.L, axis=1, keepdims=True)
        self.u = self.L / np.maximum(norm, 1e-9)
        self.part = part
        if part.axis is not None and part.kind in ("tube", "shard"):
            self.t, self.radius, self.dist = C.tube_param(part, self.p)
            self.length = C.axis_length(part)
        else:
            self.t = None


BASES = {
    # name: (top, side, under, roughness, family weights)
    "suit": ("suit_top", "suit_side", "suit_under", 0.8, dict(weave=1.0)),
    "plate": ("plate_top", "plate_side", "plate_under", 0.42, dict(ceramic=1.0)),
    "metal": ("metal_top", "metal_side", "metal_under", 0.46, dict(metal=1.0)),
    "rubber": ("rubber", "rubber", "rubber", 0.86, dict(rubber=1.0)),
}


def styled(base, features=(), quilt=None, flow=None, disp=None, seed=0):
    """A surface of one base material with painted, grooved or lit features.
    `features`: (mask_fn(ctx) -> 0..1, effect) with effect keys: paint (sRGB colour), paint_amount, groove (depth),
    emit (sRGB colour), glow (strength), rough (absolute), raise (height), family (dict, replaces the base family).
    `quilt`: ("tube" | "height", spacing) padded bands with seams. `flow(ctx)`: growth / brush coordinate."""
    top_key, side_key, under_key, rough0, fam0 = BASES[base]

    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        n = ctx.n
        col = C.chitin_gradient(n, getattr(PAL, top_key), getattr(PAL, side_key), getattr(PAL, under_key))
        col = col * mottle(s, idx, 0.05 if base == "plate" else 0.08, 40.0, 3 + seed)
        d = np.zeros(len(idx))
        rough = np.full(len(idx), rough0)
        emit = np.zeros((len(idx), 3))
        fw = {k: np.full(len(idx), v) for k, v in {**dict(weave=0.0, ceramic=0.0, metal=0.0, rubber=0.0), **fam0}.items()}
        fl = flow(ctx) if flow else (ctx.t * ctx.length if ctx.t is not None else ctx.p[:, 2])
        if quilt:
            mode, spacing = quilt
            coord = ctx.t * ctx.length if mode == "tube" else ctx.p[:, 2]
            frac = (coord / spacing) % 1.0
            edge = np.minimum(frac, 1 - frac)
            pillow = C.smoothstep(0.0, 0.22, edge)
            d += 0.003 * (pillow - 0.7)
            seam = 1 - C.smoothstep(0.0, 0.07, edge)
            col = C.lerp3(col, PAL.suit_seam, seam * 0.3)
            # Stitch lines in the bake sit on the seams: sin(2 pi F flow) peaks where coord / spacing is whole.
            fl = coord * (1.0 / (spacing * STITCH_FREQ)) + 0.25 / STITCH_FREQ
        elif base == "suit" and flow is None:
            fl = np.zeros(len(idx))  # unquilted fabric: no stitch rows
        for mask_fn, fx in features:
            m = np.clip(mask_fn(ctx), 0, 1)
            if not m.any():
                continue
            if "paint" in fx:
                # Paint chips off in patches, so stripes look worn instead of printed.
                worn = C.smoothstep(0.25, 0.55, C.value_noise(ctx.p, 90.0, 11 + seed)) * fx.get("wear", 0.0)
                amount = m * fx.get("paint_amount", 1.0) * (1 - worn)
                paint = np.asarray(fx["paint"]) * (0.8 + 0.2 * C.smoothstep(-0.4, 0.8, n[:, 2]))[:, None]
                col = C.lerp3(col, paint, amount)
            if "groove" in fx:
                d -= fx["groove"] * m
                col = col * (1 - 0.45 * m)[:, None]
            if "raise" in fx:
                d += fx["raise"] * m
            if "emit" in fx:
                e = np.asarray(fx["emit"])
                emit = np.maximum(emit, e[None, :] * (m * fx.get("glow", 1.0))[:, None])
                col = C.lerp3(col, e * 0.55, m)
            if "rough" in fx:
                rough = rough + (fx["rough"] - rough) * m
            if "family" in fx:
                for k in fw:
                    fw[k] = fw[k] * (1 - m) + fx["family"].get(k, 0.0) * m
        if disp:
            d += disp(ctx)
        s.albedo[idx] = col
        s.disp[idx] = d
        s.rough[idx] = rough
        s.emit[idx] = emit
        family(s, idx, fw["weave"], fw["ceramic"], fw["metal"], fw["rubber"], flow=fl)
    return style


def glass(hud_mask=None, glow=0.22):
    """The visor: smoked glass, very smooth, a cool sheen band across the upper half (reads as glass even in a dark
    level), a faint inner glow and a bright HUD line where `hud_mask` says."""
    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        u = ctx.u
        sheen = np.exp(-((u[:, 2] - 0.35) / 0.18) ** 2) * C.smoothstep(-0.2, 0.6, -u[:, 1])
        col = C.lerp3(np.broadcast_to(PAL.glass, (len(idx), 3)), PAL.glass_sheen, sheen * 0.55)
        e = np.broadcast_to(PAL.cyan * glow, (len(idx), 3)).copy()
        if hud_mask is not None:
            h = np.clip(hud_mask(ctx), 0, 1)
            e = np.maximum(e, PAL.cyan[None, :] * h[:, None])
            col = C.lerp3(col, PAL.cyan * 0.6, h)
        s.albedo[idx] = col
        s.emit[idx] = e
        s.rough[idx] = 0.06
        s.fam[idx] = 0.0
    return style


def light(color=None, strength=1.0, bands=None, band_dark=0.85):
    """A lit part (power cells, lamps): glowing, with optional dark ring bands along a tube (`bands` per unit)."""
    color = np.asarray(color if color is not None else PAL.cyan)

    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        m = np.ones(len(idx))
        if bands and ctx.t is not None:
            frac = (ctx.t * ctx.length * bands) % 1.0
            m = 1 - band_dark * (1 - C.smoothstep(0.0, 0.18, np.minimum(frac, 1 - frac)))
        s.albedo[idx] = C.lerp3(PAL.metal_side, color * 0.7, m)
        s.emit[idx] = color[None, :] * (m * strength)[:, None]
        s.rough[idx] = 0.3
        s.fam[idx] = np.c_[np.zeros(len(idx)), np.zeros(len(idx)), 1 - m, np.zeros(len(idx))]
    return style


def weathering(seed=7, ground_dust=0.36, crysium_boot=0.13):
    """Over the whole figure: scuffed patches on the plates, grime in the folds below, dust rising up the shins and
    boots, and pale pink Crysium dust with faint glints on the boots (he walks the crystal plains)."""
    def extra(s, pid):
        p, n = s.p, s.n
        lit = s.emit.max(axis=1) < 0.05
        h = p[:, 2]
        plate = s.fam[:, 1]
        scuff = C.smoothstep(0.2, 0.5, C.value_noise(p, 18.0, seed)) * C.smoothstep(0.1, 0.5, C.value_noise(p, 70.0, seed + 1))
        s.albedo = C.lerp3(s.albedo, s.albedo * 0.72, (scuff * plate * 0.6)[:, None] * lit[:, None])
        below = C.smoothstep(0.0, -0.8, n[:, 2])
        s.albedo = C.lerp3(s.albedo, s.albedo * 0.6, (below * 0.35)[:, None])
        dust = (1 - C.smoothstep(0.02, ground_dust, h)) * (0.55 + 0.45 * C.smoothstep(-0.3, 0.6, C.value_noise(p, 12.0, seed + 2)))
        s.albedo = np.where(lit[:, None], C.lerp3(s.albedo, PAL.dust, (dust * 0.5)[:, None]), s.albedo)
        boot = (1 - C.smoothstep(0.03, crysium_boot, h)) * C.smoothstep(0.1, 0.45, C.value_noise(p, 30.0, seed + 3))
        s.albedo = np.where(lit[:, None], C.lerp3(s.albedo, PAL.crysium, (boot * 0.55)[:, None]), s.albedo)
        glint = boot * C.smoothstep(0.62, 0.72, C.value_noise(p, 400.0, seed + 4))
        s.emit = np.maximum(s.emit, PAL.crysium_glint[None, :] * (glint * 0.55)[:, None])
        s.rough = np.clip(s.rough + dust * 0.12 + scuff * plate * 0.15, 0.04, 0.95)
    return extra


# -------------------------------------------------------------------------------------------------------------
# Feature masks (ctx -> 0..1)
# -------------------------------------------------------------------------------------------------------------

def band(values, centre, width, soft=0.35):
    """1 inside |values - centre| < width, with a soft edge of `soft` * width."""
    d = np.abs(values - centre)
    return 1 - C.smoothstep(width * (1 - soft), width, d)


def line(values, centre, width):
    return 1 - C.smoothstep(0.0, width, np.abs(values - centre))


def angle_around(ctx, ref):
    """Angle of each point around a tube's axis, 0 towards `ref` (a world direction)."""
    axis = ctx.part.axis
    tangent = axis[-1] - axis[0]
    tangent = tangent / np.linalg.norm(tangent)
    ref = np.asarray(ref, dtype=float)
    ref = ref - tangent * (ref @ tangent)
    ref /= np.linalg.norm(ref)
    side = np.cross(tangent, ref)
    centre = axis[0] + np.outer(ctx.t * np.linalg.norm(axis[-1] - axis[0]), tangent)
    radial = ctx.p - centre
    return np.arctan2(radial @ side, radial @ ref)


def rivets(ctx, points, radius):
    """Small domes at given world points."""
    out = np.zeros(len(ctx.p))
    for q in points:
        d = np.linalg.norm(ctx.p - np.asarray(q), axis=1)
        out = np.maximum(out, np.clip(1 - (d / radius) ** 2, 0, 1) ** 0.5)
    return out



def wrap(plate_style, under_style, mask_fn, depth=0.012, soft=0.0):
    """A plate that wraps part of a limb: the part is a tube a little thicker than the limb; where `mask_fn(ctx)` is
    1 it is the plate (`plate_style`), elsewhere it sinks by `depth` back to the limb and wears `under_style` (the
    suit). The step between the two is the plate's rim."""
    def style(s, idx, part):
        ctx = Ctx(s, idx, part)
        m = np.clip(mask_fn(ctx), 0, 1)
        under_style(s, idx, part)
        saved = (s.albedo[idx].copy(), s.disp[idx].copy(), s.rough[idx].copy(), s.emit[idx].copy(), s.fam[idx].copy(), s.flow[idx].copy())
        plate_style(s, idx, part)
        a, d, r, e, f, fl = saved
        s.albedo[idx] = C.lerp3(a, s.albedo[idx], m)
        s.disp[idx] = d * (1 - m) + s.disp[idx] * m - depth * (1 - m)
        s.rough[idx] = r * (1 - m) + s.rough[idx] * m
        s.emit[idx] = C.lerp3(e, s.emit[idx], m)
        s.fam[idx] = C.lerp3(f, s.fam[idx], m)
        s.flow[idx] = np.where(m > 0.5, s.flow[idx], fl)
        # A dark seam where the plate meets the cloth.
        rim = np.clip(1 - np.abs(m - 0.5) * 2, 0, 1) ** 2
        s.albedo[idx] = s.albedo[idx] * (1 - 0.5 * rim)[:, None]
    return style


def sector(ctx, front, half_angle, t0=0.0, t1=1.0, soft_deg=6.0, soft_t=0.04):
    """1 on the part of a tube within `half_angle` degrees of `front` and between t0 and t1 along it."""
    a = np.degrees(np.abs(angle_around(ctx, front)))
    m = 1 - C.smoothstep(half_angle - soft_deg, half_angle, a)
    m = m * C.smoothstep(t0, t0 + soft_t, ctx.t) * (1 - C.smoothstep(t1 - soft_t, t1, ctx.t))
    return m
