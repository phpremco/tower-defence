"""Palette-driven surface styles for insect-like Burrowers. A style colours, displaces and lights the vertices a
part owns (see common.detail). `k` is the creature's size factor: displacement depths scale with it and noise
frequencies shrink with it, so a boss gets the same look as a swarm creature, just bigger.

Round 2 look: matte, layered chitin. Plates are rough (0.6-0.8); the bake lowers roughness only on raised rims and
ridges (common.set_high_channel), so the sheen sits on the edges. `weathering` adds pitting, scratches and dusty
undersides over everything."""

import math

import numpy as np

import common as C


class Palette:
    def __init__(self, top, side, belly, seam, dark, glow, eye, membrane=None, bone=None, dust=(0.24, 0.2, 0.17)):
        self.top = np.array(top)
        self.side = np.array(side)
        self.belly = np.array(belly)
        self.seam = np.array(seam)
        self.dark = np.array(dark)
        self.glow = np.array(glow)
        self.eye = np.array(eye)
        self.membrane = np.array(membrane if membrane is not None else side)
        self.bone = np.array(bone if bone is not None else top)
        self.dust = np.array(dust)


def family(s, idx, chitin=0.0, membrane=0.0, crystal=0.0, rock=0.0, flow=None):
    """Round 3: which micro-detail family a vertex gets (surface.py) and its growth-line coordinate (world units)."""
    w = np.c_[np.broadcast_to(chitin, len(idx)), np.broadcast_to(membrane, len(idx)),
              np.broadcast_to(crystal, len(idx)), np.broadcast_to(rock, len(idx))]
    s.fam[idx] = w
    if flow is not None:
        s.flow[idx] = flow


def mottle(s, idx, amount, scale, seed):
    return 1.0 + C.value_noise(s.p[idx], scale, seed)[:, None] * amount


def band_plates(pal, k, bands, height=0.004, glow=0.0, axis=1, shingle=False, rough=0.68):
    """Plates in bands along the part's local axis (1 = length). Shingled plates tuck under the one in front."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        n = s.n[idx]
        along = (1 + L[:, axis]) * 0.5
        frac = (along * bands) % 1.0
        if shingle:
            profile = C.smoothstep(0.0, 0.3, frac) * (0.55 + 0.45 * frac)
            edge = frac
        else:
            edge = np.minimum(frac, 1 - frac)
            profile = C.smoothstep(0.0, 0.12, edge)
        s.disp[idx] = height * k * (profile - 0.5)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.14, 60 / k, 4)
        seam = 1 - C.smoothstep(0.0, 0.08, edge)
        s.albedo[idx] = C.lerp3(col, pal.seam, seam * 0.9)
        s.rough[idx] = rough + seam * 0.15
        family(s, idx, chitin=1.0, flow=along * 2 * part.radii[axis])
        if glow:
            s.emit[idx] = pal.glow[None, :] * (seam * C.smoothstep(-0.2, 0.5, n[:, 2]) * glow)[:, None]
    return style


def crest(pal, k, ribs=7, height=0.004):
    """A shovel crest: radial ribs fanning from the back to a pale, worn front edge."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        n = s.n[idx]
        u = L / np.linalg.norm(L, axis=1, keepdims=True)
        ang = np.arctan2(u[:, 0], -u[:, 1])  # 0 straight ahead (local -Y)
        rib = np.abs(np.sin(ang * ribs * 0.5))
        groove = 1 - C.smoothstep(0.0, 0.25, rib)
        front = C.smoothstep(0.2, 0.95, -u[:, 1]) * C.smoothstep(0.75, 0.98, np.hypot(u[:, 0], u[:, 1]))
        s.disp[idx] = height * k * (C.smoothstep(0.0, 0.4, rib) - 0.5)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.12, 60 / k, 5)
        col = C.lerp3(col, pal.seam, groove * 0.8)
        col = C.lerp3(col, pal.bone, front * 0.7)
        s.albedo[idx] = col
        s.rough[idx] = 0.66 + groove * 0.15
        # Growth lines run parallel to the worn front edge.
        family(s, idx, chitin=1.0, flow=np.hypot(L[:, 0], L[:, 1]) * part.radii[1] + 0.3 * L[:, 1] * part.radii[1])
    return style


def helmet_plates(pal, k, height=0.006):
    """A crown plate with a centre seam whose rim dips towards the mouth, over darker cheek plates."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        n = s.n[idx]
        u = L / np.linalg.norm(L, axis=1, keepdims=True)
        rim_height = 0.12 + 0.3 * u[:, 1]
        crown = u[:, 2] > rim_height
        rim = np.abs(u[:, 2] - rim_height)
        centre = np.where(crown, np.abs(u[:, 0]), 1.0)
        cheek = np.where(~crown, np.abs(u[:, 1] + 0.15) + 0.6 * C.smoothstep(0.0, -0.6, u[:, 2]), 1.0)
        e = np.minimum(np.minimum(rim, centre * 1.4), cheek)
        raised = np.where(crown, 1.0, 0.55)
        s.disp[idx] = height * k * (raised * C.smoothstep(0.0, 0.1, e) - 0.5)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.12, 60 / k, 8)
        col = np.where(crown[:, None], col, C.lerp3(col, pal.dark, 0.5))
        seam = 1 - C.smoothstep(0.0, 0.06, e)
        s.albedo[idx] = C.lerp3(col, pal.seam, seam * 0.9)
        s.rough[idx] = 0.66 + seam * 0.15
        family(s, idx, chitin=1.0, flow=e * part.radii[0])
    return style


def wedge_head(pal, k, slits=3, slit_width=0.05, height=0.004):
    """An eyeless wedge: dark plates and rows of glowing sensor slits slanting back along both sides."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        n = s.n[idx]
        u = L / np.linalg.norm(L, axis=1, keepdims=True)
        side = C.smoothstep(0.35, 0.75, np.abs(u[:, 0]))
        band = (u[:, 1] * 2.2 + u[:, 2] * 1.3 + 3.0) * slits * 0.5
        frac = band % 1.0
        line = 1 - C.smoothstep(0.0, slit_width * slits, np.minimum(frac, 1 - frac))
        window = C.smoothstep(-0.55, -0.15, u[:, 2]) * (1 - C.smoothstep(0.45, 0.7, u[:, 2]))
        window *= C.smoothstep(-0.9, -0.3, u[:, 1]) * (1 - C.smoothstep(0.35, 0.7, u[:, 1]))
        slit = line * side * window
        s.disp[idx] = -height * k * slit + 0.0015 * k * C.value_noise(s.p[idx], 90 / k, 6)
        col = C.chitin_gradient(n, pal.top * 0.75, pal.side * 0.7, pal.belly) * mottle(s, idx, 0.12, 60 / k, 7)
        col = C.lerp3(col, pal.dark, 0.35)
        s.albedo[idx] = C.lerp3(col, pal.eye * 0.5, slit)
        s.emit[idx] = pal.eye[None, :] * slit[:, None]
        s.rough[idx] = 0.7 - slit * 0.5
        family(s, idx, chitin=1.0 - slit, flow=band / slits * part.radii[1])
    return style


def membrane_sac(pal, k, bands=4, plate_line=0.05, cells=60, cell_size=0.12, glow=1.0, seed=31, plate_glow=0.7):
    """A swollen sac: shingled armour plates over the back, a translucent membrane on the flanks with glowing
    blobs (venom or eggs) pressed against it, and glow leaking through the gaps between the plates."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        n = s.n[idx]
        u = L / np.linalg.norm(L, axis=1, keepdims=True)
        along = (1 + u[:, 1]) * 0.5
        line = plate_line + 0.1 * np.cos(along * math.pi * 2.0)
        armour = u[:, 2] > line
        edge = np.abs(u[:, 2] - line)
        frac = (along * bands) % 1.0
        shingle = C.smoothstep(0.0, 0.3, frac) * (0.55 + 0.45 * frac)
        plate_disp = 0.006 * k * shingle + 0.004 * k * C.smoothstep(0.0, 0.06, edge)
        cell = C.voronoi_edge(u, C.fibonacci_sphere(cells, seed=seed))
        size = cell_size * (0.7 + 0.6 * (0.5 + 0.5 * C.value_noise(u, 3.0, seed + 1)))
        blob = np.sqrt(np.clip((cell - 0.03) / size, 0, 1))
        blob_disp = 0.008 * k * blob - 0.002 * k
        s.disp[idx] = np.where(armour, plate_disp, blob_disp)

        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.14, 15 / k * 4, 4)
        gap = 1 - C.smoothstep(0.0, 0.12, frac)
        plate_col = C.lerp3(col, pal.membrane * 0.6, gap * 0.9)
        rim = C.smoothstep(0.82, 1.0, frac)
        plate_col = C.lerp3(plate_col, pal.bone, rim * 0.35)
        saddle = np.exp(-(u[:, 0] / 0.3) ** 2) * C.smoothstep(0.3, 0.9, u[:, 2])
        plate_col = C.lerp3(plate_col, pal.dark, saddle * 0.5)
        flesh = C.lerp3(pal.belly, pal.membrane, C.smoothstep(0.0, 0.7, blob))
        lip = 1 - C.smoothstep(0.0, 0.05, edge)
        s.albedo[idx] = np.where(armour[:, None], plate_col, C.lerp3(flesh, pal.seam, lip * 0.9))
        blob_glow = C.smoothstep(0.35, 1.0, blob) ** 1.5 * (~armour)
        vein = (1 - C.smoothstep(0.0, 0.02, cell)) * (~armour) * 0.25
        s.emit[idx] = pal.glow[None, :] * ((blob_glow + vein) * glow + gap * armour * plate_glow)[:, None]
        s.rough[idx] = np.where(armour, 0.66 + gap * 0.1, 0.3)
        plate = armour.astype(float)
        family(s, idx, chitin=plate, membrane=1.0 - plate, flow=along * 2 * part.radii[1])
    return style


def glow_flesh(pal, k, glow=0.8, flank=(0.35, 0.85), folds=9.0):
    """Dark flesh under open armour (between ribs): folded membrane that glows towards the flanks, so the gaps
    between the ribs read as lit stripes and the back stays dark."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        side = C.smoothstep(flank[0], flank[1], np.abs(u[:, 0])) * (1 - C.smoothstep(0.2, 0.7, u[:, 2]))
        fold = np.abs(np.sin(u[:, 1] * folds))
        crease = 1 - C.smoothstep(0.0, 0.3, fold)
        col = C.lerp3(pal.belly, pal.membrane, 0.35 + 0.4 * side)
        s.albedo[idx] = C.lerp3(col, pal.seam, crease * 0.7)
        s.emit[idx] = pal.glow[None, :] * (side * glow * (1 - 0.6 * crease))[:, None]
        s.disp[idx] = -0.002 * k * crease
        s.rough[idx] = 0.45 + 0.2 * crease
        family(s, idx, membrane=1.0, flow=(1 + u[:, 1]) * part.radii[1])
    return style


def tube(pal, k, base, tip, tip_glow=0.0, rings=(), ring_dark=0.7, glow_from=0.82, rough=0.62):
    base = np.array(base)
    tip = np.array(tip)

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.lerp3(base, tip, C.smoothstep(0.35, 1.0, t))
        shade = 0.55 + 0.45 * C.smoothstep(-0.6, 0.7, n[:, 2])
        col = col * shade[:, None] * mottle(s, idx, 0.1, 150 / k, 9)
        dark = np.zeros(len(idx))
        for r in rings:
            dark = np.maximum(dark, 1 - C.smoothstep(0.0, 0.05, np.abs(t - r)))
        s.albedo[idx] = C.lerp3(col, pal.seam, dark * ring_dark)
        s.disp[idx] = -0.0015 * k * dark
        s.rough[idx] = rough
        family(s, idx, chitin=1.0, flow=t * C.axis_length(part))
        if tip_glow:
            s.emit[idx] = pal.glow[None, :] * (C.smoothstep(glow_from, 1.0, t) * tip_glow)[:, None]
    return style


def blade(pal, k, glow=0.8, teeth=0):
    """A flat blade: dark core, a pale worn cutting edge all round the rim, a glowing edge towards the point and
    optional notches along its length."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        rim = C.smoothstep(0.55, 0.95, dist / np.maximum(radius, 1e-5))
        col = C.lerp3(pal.dark, pal.side * 0.8, C.smoothstep(-0.3, 0.8, n[:, 2]))
        col = col * mottle(s, idx, 0.1, 150 / k, 13)
        col = C.lerp3(col, pal.bone, rim * C.smoothstep(0.2, 0.7, t) * 0.8)
        disp = np.zeros(len(idx))
        if teeth:
            notch = np.abs(np.sin(t * math.pi * teeth)) ** 6
            disp = -0.003 * k * notch * rim
        s.disp[idx] = disp
        s.albedo[idx] = col
        hot = C.smoothstep(0.45, 1.0, t) * rim
        s.emit[idx] = pal.glow[None, :] * (hot * glow)[:, None]
        s.rough[idx] = 0.62 - rim * 0.25
        family(s, idx, chitin=1.0 - 0.5 * hot, flow=t * C.axis_length(part))
    return style


def leg(pal, k, seg, spines=0):
    """Leg segment: lit on top, dark below, dark joints, a darker spiked shin and optional rows of short ridges."""
    top = pal.top * 0.85

    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, top, pal.side, pal.belly) * mottle(s, idx, 0.1, 140 / k, 12)
        joint = 1 - C.smoothstep(0.0, 0.12, np.minimum(t, 1 - t))
        if seg == "Tibia":
            col = C.lerp3(col, pal.dark, C.smoothstep(0.3, 0.8, t))
            col = C.lerp3(col, pal.bone * 0.8, C.smoothstep(0.85, 1.0, t))  # worn bone-pale spike tip
        s.albedo[idx] = C.lerp3(col, pal.seam, joint * 0.75)
        disp = -0.001 * k * joint
        if spines:
            ridge = np.abs(np.sin(t * np.pi * spines)) ** 8 * C.smoothstep(0.1, 0.6, n[:, 2])
            disp = disp + 0.004 * k * ridge
        s.disp[idx] = disp
        s.rough[idx] = 0.66
        family(s, idx, chitin=1.0, flow=t * C.axis_length(part))
    return style


def hook(pal, k, glow=0.0, glow_from=0.85):
    """A spine or hook grown from the armour: dark root, pale worn point."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, pal.side, pal.dark, pal.belly)
        col = C.lerp3(col, pal.bone, C.smoothstep(0.45, 1.0, t) * 0.85)
        s.albedo[idx] = col * mottle(s, idx, 0.08, 150 / k, 14)
        s.rough[idx] = 0.6
        family(s, idx, chitin=1.0, flow=t * C.axis_length(part))
        if glow:
            s.emit[idx] = pal.glow[None, :] * (C.smoothstep(glow_from, 1.0, t) * glow)[:, None]
    return style


def eye(pal, k, strength=0.9):
    """A small hard glowing bead (eye clusters): dark rim, hot centre facing out."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.linalg.norm(L, axis=1, keepdims=True)
        out = np.clip(-u[:, 1] * 0.5 + np.abs(u[:, 0]) * 0.5 + u[:, 2] * 0.3, 0, 1)
        core = C.smoothstep(0.1, 0.8, out)
        s.albedo[idx] = C.lerp3(pal.dark * 0.5, pal.eye * 0.6, core)
        s.emit[idx] = pal.eye[None, :] * (0.25 + strength * core)[:, None]
        s.rough[idx] = 0.2
    return style


def inner_glow(pal, strength=1.0):
    """A throat or core: plain glow (sits inside the mouth)."""
    def style(s, idx, part):
        s.albedo[idx] = pal.glow * 0.6
        s.emit[idx] = pal.glow[None, :] * strength
        s.rough[idx] = 0.4
    return style


def weathering(k, pits=1.0, scratches=1.0, dust=0.55, seed=21, markings=None, marking_amount=0.75):
    """Over the whole body: hammered dents, fine pitting and scratches in the normal map, patchy roughness and
    dusty undersides, so the chitin reads as old armour and not as moulded plastic."""
    def extra(s, pid):
        p = s.p
        s.disp += 0.0009 * k * C.value_noise(p, 70 / k, seed)
        pit = C.smoothstep(0.35, 0.7, C.value_noise(p, 260 / k, seed + 1))
        # Fine pitting and scratches moved into the per-pixel bake (surface.py); the vertex pass keeps a faint
        # version so the colour patches still line up with real dents.
        s.disp -= 0.0005 * k * pit * pits
        rng = np.random.default_rng(seed + 2)
        scratch = np.zeros(len(p))
        for _ in range(3):
            d = rng.normal(size=3)
            d /= np.linalg.norm(d)
            wave = np.abs(np.sin(p @ d * (160 / k) + C.value_noise(p, 20 / k, seed + 3) * 2.0))
            mask = C.smoothstep(0.2, 0.6, C.value_noise(p, 9 / k, int(rng.integers(1000))))
            scratch = np.maximum(scratch, (1 - C.smoothstep(0.0, 0.12, wave)) * mask)
        s.disp -= 0.0002 * k * scratch * scratches
        s.albedo *= (1 + 0.18 * scratch * scratches)[:, None]
        s.albedo *= (1 - 0.15 * pit * pits)[:, None]
        s.albedo *= (1 + 0.06 * C.value_noise(p, 30 / k, seed + 4))[:, None]
        lit = s.emit.max(axis=1) < 0.05
        if markings is not None:
            # Dark blotches and a broken stripe down the spine: an alien camouflage that breaks up the big
            # orange areas and pushes the value contrast against the glow.
            blotch = C.smoothstep(0.15, 0.4, C.value_noise(p, 28 / k, seed + 7))
            spine = np.exp(-(p[:, 0] / (0.025 * k)) ** 2) * C.smoothstep(0.2, 0.6, s.n[:, 2])
            spine *= C.smoothstep(-0.2, 0.2, C.value_noise(p, 40 / k, seed + 8))
            mark = np.maximum(blotch, spine) * C.smoothstep(-0.2, 0.4, s.n[:, 2]) * marking_amount * lit
            s.albedo = C.lerp3(s.albedo, np.asarray(markings) * 1.3, mark)
        below = C.smoothstep(0.0, -0.75, s.n[:, 2])
        grime = below * (0.6 + 0.4 * (0.5 + 0.5 * C.value_noise(p, 25 / k, seed + 5)))
        s.albedo = np.where(lit[:, None], C.lerp3(s.albedo, s.albedo * 0.4 + np.array((0.2, 0.17, 0.14)) * 0.6,
                                                  grime * dust), s.albedo)
        s.rough = np.clip(s.rough + 0.08 * C.value_noise(p, 45 / k, seed + 6) + 0.1 * pit + grime * 0.2, 0.08, 0.95)
    return extra


# ---------------------------------------------------------------------------------------------------------------
# Round 5: flyers (fins, wing membranes, lit bellies) and energy creatures (bud petals, rings)
# ---------------------------------------------------------------------------------------------------------------

def fin_sheet(pal, k, uv_fn, under, rays=7, chevrons=3, chevron_width=0.12, lead_glow=1.0, lead_width=0.07,
              ray_height=0.003, flush=None, rough=0.62, dashed=False):
    """A fin or wing membrane stretched over raised rays, seen from above: dark back, pale chevron bands across the
    span, a glowing leading edge, a pale underside (`under`) so the flyer still reads when it banks.
    `uv_fn(points) -> (u, v)`: u 0..1 from root to tip, v 0..1 from the leading to the trailing edge.
    `flush`: an optional colour that floods the root of the back (a warm flush near the body). `dashed` breaks
    the chevrons at the rays into a row of pale cells (organic markings, not painted stripes)."""
    under = np.array(under)

    def style(s, idx, part):
        u, v = uv_fn(s.p[idx])
        n = s.n[idx]
        top = C.smoothstep(-0.25, 0.25, n[:, 2])
        # Rays fan from the root: their coordinate is the chord position.
        ray = np.abs(np.sin(math.pi * rays * np.clip(v, 0, 1)))
        ridge = C.smoothstep(0.75, 1.0, ray) * C.smoothstep(0.05, 0.3, u)
        # Chevrons: bands across the span that sweep back with the chord (they follow the fin's sweep).
        c = (u * 0.85 + v * 0.45) * chevrons
        frac = c % 1.0
        band = 1 - C.smoothstep(chevron_width * 0.6, chevron_width, np.minimum(frac, 1 - frac))
        if dashed:
            band *= C.smoothstep(0.15, 0.85, 1 - ray) ** 1.5 * (0.7 + 0.3 * C.value_noise(s.p[idx], 30 / k, 46))
        band *= C.smoothstep(0.08, 0.2, u) * C.smoothstep(lead_width, lead_width + 0.1, v) * (1 - C.smoothstep(0.85, 0.97, v))
        back = C.lerp3(pal.dark, pal.top, 0.35 + 0.65 * C.smoothstep(0.0, 0.9, v * 0.5 + u * 0.6))
        if flush is not None:
            back = C.lerp3(back, flush, (1 - C.smoothstep(0.0, 0.35, u)) * 0.6)
        back = back * mottle(s, idx, 0.12, 40 / k, 41)
        back = C.lerp3(back, pal.bone, band * 0.9)
        back = C.lerp3(back, pal.seam, (1 - C.smoothstep(0.0, 0.5, ray)) * 0.35 * C.smoothstep(0.1, 0.4, u))
        belly = C.lerp3(under, under * 0.6, C.smoothstep(0.6, 1.0, v)) * mottle(s, idx, 0.08, 40 / k, 42)
        col = C.lerp3(belly, back, top)
        lead = 1 - C.smoothstep(lead_width * 0.4, lead_width, v)
        trail = C.smoothstep(0.9, 1.0, v)
        col = C.lerp3(col, pal.seam, trail * 0.6)
        col = C.lerp3(col, pal.glow * 0.7, lead)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (lead * lead_glow * (0.6 + 0.4 * top))[:, None]
        s.disp[idx] = ray_height * k * ridge + 0.001 * k * band
        s.rough[idx] = rough - 0.15 * ridge - 0.25 * lead
        family(s, idx, chitin=ridge * 0.8 + lead * 0.5, membrane=1.0 - ridge * 0.8 - lead * 0.5, flow=v * 0.4 + u * 0.05)
    return style


def gill_belly(pal, k, under, slits=5, start=-0.55, spacing=0.17, width=0.035, side=(0.18, 0.75), glow=1.0):
    """A flyer's pale underside with chevrons of glowing gill slits, so the belly flashes light when it banks or
    falls. Works on an ellipsoid; slits sit in the local lower half, either side of the midline."""
    under = np.array(under)

    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        n = s.n[idx]
        below = C.smoothstep(0.1, -0.4, n[:, 2])
        ax = np.abs(u[:, 0])
        # Chevrons point forward (local -Y): the slit coordinate bends back towards the flanks.
        c = (-u[:, 1] - start) / spacing + ax * 1.6
        frac = c % 1.0
        line = 1 - C.smoothstep(width * 0.5, width * 1.6, np.minimum(frac, 1 - frac) * spacing)
        line *= (c > 0) * (c < slits) * C.smoothstep(side[0], side[0] + 0.08, ax) * (1 - C.smoothstep(side[1] - 0.1, side[1], ax))
        slit = line * below
        top = C.chitin_gradient(n, pal.top, pal.side, pal.side)
        col = C.lerp3(top, under, below) * mottle(s, idx, 0.1, 50 / k, 43)
        col = C.lerp3(col, pal.seam, slit * 0.7)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (slit * glow)[:, None]
        s.disp[idx] = -0.003 * k * slit
        s.rough[idx] = 0.6 - 0.3 * slit
        family(s, idx, chitin=1.0 - below * 0.6, membrane=below * 0.6, flow=-u[:, 1] * part.radii[1])
    return style


def bud_petal(pal, k, axis_angle, half_width, glow=0.9, rim_width=0.18, taper=None, bands=5):
    """One petal of a closed bud around a vertical axis (local frame of the bud's centre ellipsoid): dark chitin
    with ribs along the petal, a pale worn rim, light leaking along the seams to its neighbours and a glowing
    inside. `axis_angle`/`half_width`: the petal's azimuth (0 = forward) and half its angular width (radians)."""
    def style(s, idx, part):
        L = C.local_points(part, s.p[idx])
        u = L / np.maximum(np.linalg.norm(L, axis=1, keepdims=True), 1e-6)
        n = s.n[idx]
        az = np.arctan2(u[:, 0], -u[:, 1])
        # `taper(elevation)`: how the petal narrows towards its tip (the same rule that built it).
        hw = half_width * (taper(np.arcsin(np.clip(u[:, 2], -1, 1))) if taper else 1.0)
        d = np.abs((az - axis_angle + math.pi) % (2 * math.pi) - math.pi) / hw
        edge = C.smoothstep(1.0 - rim_width, 1.0, d)
        tip = C.smoothstep(0.6, 0.95, u[:, 2])
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.14, 50 / k, 44)
        # Growth bands across the petal (not lengthwise ribs, which read as a garlic clove).
        ribs = np.abs(np.sin(np.arcsin(np.clip(u[:, 2], -1, 1)) * bands))
        col = C.lerp3(col, pal.seam, (1 - C.smoothstep(0.0, 0.3, ribs)) * 0.5)
        col = C.lerp3(col, pal.bone, np.maximum(edge * 0.55, tip * 0.4))
        outward = np.einsum("ij,ij->i", n, u) > 0.0
        s.albedo[idx] = np.where(outward[:, None], col, pal.glow * 0.6)
        hot = np.where(outward, edge * glow * 0.8, 1.0)
        s.emit[idx] = pal.glow[None, :] * hot[:, None]
        s.disp[idx] = 0.002 * k * ribs * outward
        s.rough[idx] = np.where(outward, 0.62 - 0.2 * edge, 0.3)
        family(s, idx, chitin=outward * (1.0 - edge * 0.5), flow=u[:, 2] * part.radii[2])
    return style


def ring_band(pal, k, centre, normal, beads=0, nodes=0, node_glow=1.0, joint_glow=0.8, inner_glow=0.6, ref=(1.0, 0.0, 0.0),
              phase=0.0):
    """A ring of an energy creature: dark polished chitin, an inner edge that glows towards the heart, optional
    bead joints (`beads` per turn, glowing in the gaps) and glowing nodes (`nodes` per turn). Uses the angle
    around the ring, so a ring built from several arcs (PartBuilder.ring) gets one continuous pattern; angle 0 lies
    along `ref` (as in PartBuilder.ring) and `phase` (radians) shifts the beads and nodes."""
    centre = np.array(centre)
    nrm = np.array(normal, dtype=float)
    nrm /= np.linalg.norm(nrm)
    ref = np.array(ref, dtype=float)
    a = ref - nrm * (nrm @ ref)
    a /= np.linalg.norm(a)
    b = np.cross(nrm, a)

    def style(s, idx, part):
        p = s.p[idx] - centre
        n = s.n[idx]
        th = np.arctan2(p @ b, p @ a) - phase
        radial = p - np.outer(p @ nrm, nrm)
        radial /= np.maximum(np.linalg.norm(radial, axis=1, keepdims=True), 1e-6)
        inward = C.smoothstep(0.2, 0.9, -np.einsum("ij,ij->i", n, radial))
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly) * mottle(s, idx, 0.1, 60 / k, 45)
        joint = np.zeros(len(idx))
        if beads:
            frac = (th / (2 * math.pi) * beads) % 1.0
            joint = 1 - C.smoothstep(0.0, 0.12, np.minimum(frac, 1 - frac))
        node = np.zeros(len(idx))
        if nodes:
            frac = (th / (2 * math.pi) * nodes + 0.5) % 1.0
            node = 1 - C.smoothstep(0.0, 0.05, np.minimum(frac, 1 - frac))
        col = C.lerp3(col, pal.seam, joint * 0.8)
        col = C.lerp3(col, pal.glow * 0.7, np.maximum(node, inward * inner_glow * 0.6))
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip(joint * joint_glow + node * node_glow + inward * inner_glow, 0, 1)[:, None]
        s.disp[idx] = -0.002 * k * joint
        s.rough[idx] = 0.45 - 0.15 * node
        family(s, idx, chitin=1.0 - np.maximum(node, inward * 0.5), flow=th * 0.05)
    return style
