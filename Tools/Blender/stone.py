"""Stone styles for living-rock creatures (Cragback and later Stone species). Same contract as chitin.py: a style
colours, displaces and lights the vertices a part owns (common.detail) and writes the micro-detail family (rock)
and a Flow coordinate (strata) for surface.py.

The look: hewn basalt, light on top and near-black underneath, fine strata, dark cracks, and an orange fault glow
where the stone meets the hot core (the gaps between plates, the seams between leg drums, a few thin veins over the
top). The glow is kept to lines so the body still reads as dark rock and not as a molten blob."""

import numpy as np

import chitin as H
import common as C


def _veins(p, scale, seed, width=0.06):
    """Thin meandering lines that come and go: 1 on a vein."""
    line = 1 - C.smoothstep(0.0, width, np.abs(C.value_noise(p, scale, seed)))
    return line * C.smoothstep(-0.3, 0.25, C.value_noise(p, scale * 0.3, seed + 1))


def slab(pal, k, strata=24.0, veins=0.6, fault=1.0, seed=0, rim=-0.35):
    """A tectonic plate (a chiselled ellipsoid): strata across its height, dark cracks, thin glowing veins over the
    top, and the fault glow along its lower rim where it rests on the core. `rim`: local height of that rim."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        L = C.local_points(part, p)
        col = C.chitin_gradient(n, pal.top, pal.side, pal.belly)
        band = 0.5 + 0.5 * np.sin(p[:, 2] * strata / k + C.value_noise(p, 4 / k, seed) * 2.5)
        col = col * (0.72 + 0.28 * band)[:, None] * (1 + 0.14 * C.value_noise(p, 12 / k, seed + 1))[:, None]
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 7 / k, seed + 2)))
        crack *= C.smoothstep(-0.2, 0.3, C.value_noise(p, 2 / k, seed + 3))
        vein = _veins(p, 4.0 / k, seed + 4, width=0.09) * C.smoothstep(0.0, 0.5, n[:, 2]) * veins
        lip = 1 - C.smoothstep(rim - 0.12, rim + 0.08, L[:, 2])
        col = C.lerp3(col, pal.seam, np.maximum(crack * 0.8, lip * 0.85))
        col = C.lerp3(col, pal.glow * 0.45, vein)
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * np.clip(vein * 0.9 + lip * fault * 0.85 + crack * 0.15 * fault, 0, 1)[:, None]
        s.disp[idx] = 0.004 * k * C.value_noise(p, 16 / k, seed + 5) - 0.003 * k * band - 0.006 * k * crack - 0.004 * k * vein
        s.rough[idx] = 0.82 + 0.08 * C.value_noise(p, 6 / k, seed + 6) - 0.2 * vein
        hot = np.clip(vein + lip, 0, 1)
        H.family(s, idx, rock=1.0 - 0.7 * hot, flow=p[:, 2] + 0.03 * k * C.value_noise(p, 2 / k, seed + 7))
    return style


def core(pal, k, seed=0, cells=44, width=0.016, glow=0.7):
    """The hot core under the plates: dark rock broken by a network of thin glowing fissures (seen in the faults).
    Kept dark between the fissures so the creature reads as rock, not as a molten blob."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        u = C.local_points(part, p)
        u = u / np.maximum(np.linalg.norm(u, axis=1, keepdims=True), 1e-6)
        e = C.voronoi_edge(u, C.fibonacci_sphere(cells, seed=seed))
        fissure = 1 - C.smoothstep(0.0, width, e)
        col = C.chitin_gradient(n, pal.side * 0.8, pal.dark, pal.belly) * (1 + 0.15 * C.value_noise(p, 10 / k, seed))[:, None]
        s.albedo[idx] = C.lerp3(col, pal.glow * 0.5, fissure)
        s.emit[idx] = pal.glow[None, :] * (fissure * glow)[:, None]
        s.disp[idx] = -0.006 * k * fissure + 0.003 * k * C.value_noise(p, 20 / k, seed + 1)
        s.rough[idx] = 0.85 - 0.25 * fissure
        H.family(s, idx, rock=1.0 - fissure, flow=p[:, 2])
    return style


def pillar(pal, k, drums=3, seed=0, glow=0.6):
    """A columnar basalt leg: stacked drums split by dark seams that glow faintly, lighter at the top, dusty and
    near-black at the foot."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        p = s.p[idx]
        n = s.n[idx]
        col = C.chitin_gradient(n, pal.top * 0.85, pal.side, pal.belly)
        col = C.lerp3(col, pal.dark, C.smoothstep(0.4, 1.0, t) * 0.6)
        frac = (t * drums + 0.15 * C.value_noise(p, 9 / k, seed)) % 1.0
        seam = 1 - C.smoothstep(0.0, 0.08, np.minimum(frac, 1 - frac))
        crack = 1 - C.smoothstep(0.0, 0.05, np.abs(C.value_noise(p, 10 / k, seed + 2)))
        col = col * (1 + 0.12 * C.value_noise(p, 14 / k, seed + 1))[:, None]
        s.albedo[idx] = C.lerp3(col, pal.seam, np.maximum(seam * 0.85, crack * 0.6))
        s.emit[idx] = pal.glow[None, :] * (seam * glow * (1 - C.smoothstep(0.6, 1.0, t)))[:, None]
        s.disp[idx] = -0.005 * k * seam - 0.003 * k * crack + 0.002 * k * C.value_noise(p, 25 / k, seed + 3)
        s.rough[idx] = 0.84
        H.family(s, idx, rock=1.0 - 0.6 * seam, flow=t * C.axis_length(part))
    return style


def spike(pal, k, glow=0.0, seed=0):
    """A stone spike or tooth (stalactites under the belly, teeth in the maw): dark root, pale worn point."""
    def style(s, idx, part):
        t, radius, dist = C.tube_param(part, s.p[idx])
        n = s.n[idx]
        col = C.chitin_gradient(n, pal.side, pal.dark, pal.belly)
        col = C.lerp3(col, pal.bone, C.smoothstep(0.5, 1.0, t) * 0.75)
        s.albedo[idx] = col * (1 + 0.1 * C.value_noise(s.p[idx], 20 / k, seed))[:, None]
        s.rough[idx] = 0.78
        s.disp[idx] = 0.0
        if glow:
            s.emit[idx] = pal.glow[None, :] * ((1 - C.smoothstep(0.0, 0.35, t)) * glow)[:, None]
        H.family(s, idx, rock=1.0, flow=t * C.axis_length(part))
    return style


def skin(pal, k, seed=0, cells=40, width=0.05, glow=0.8, strata=40.0):
    """Thin stone skin stretched between stone blades (a Stone bat's wing membrane): dark, crazed like dried mud,
    with fine strata and the craze lines glowing in the species colour where the heat shows through. Lighter on
    top so the wing reads from above; the underside stays near-black with fainter cracks."""
    def style(s, idx, part):
        p = s.p[idx]
        n = s.n[idx]
        rng = np.random.default_rng(seed)
        seeds = p[rng.choice(len(p), size=min(cells, len(p)), replace=False)]
        e = C.voronoi_edge(p, seeds)
        span = max(float(np.ptp(p[:, 0])), 1e-3)
        crack = 1 - C.smoothstep(0.0, width * span / cells ** 0.5, e)
        # Craze lines come and go (a dried-mud crackle, not a honeycomb) and run hotter in a few places.
        crack *= C.smoothstep(-0.25, 0.35, C.value_noise(p, 5 / k, seed + 4))
        crack *= 0.55 + 0.45 * C.smoothstep(-0.2, 0.5, C.value_noise(p, 3 / k, seed + 5))
        top = C.smoothstep(-0.2, 0.3, n[:, 2])
        col = C.lerp3(pal.belly * 1.4, pal.side, top)
        band = 0.5 + 0.5 * np.sin(p[:, 0] * strata / k + C.value_noise(p, 6 / k, seed + 1) * 2.0)
        col = col * (0.8 + 0.2 * band)[:, None] * (1 + 0.12 * C.value_noise(p, 14 / k, seed + 2))[:, None]
        col = C.lerp3(col, pal.glow * 0.45, crack * (0.4 + 0.6 * top))
        s.albedo[idx] = col
        s.emit[idx] = pal.glow[None, :] * (crack * glow * (0.35 + 0.65 * top))[:, None]
        s.disp[idx] = -0.002 * k * crack + 0.0015 * k * C.value_noise(p, 30 / k, seed + 3)
        s.rough[idx] = 0.8 - 0.25 * crack
        H.family(s, idx, membrane=0.6 * (1 - crack), rock=0.4 * (1 - crack), flow=p[:, 0])
    return style
