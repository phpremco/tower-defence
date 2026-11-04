"""Dr. Voss round 19: gear. Headset (band over the hair, a cyan-ringed earpiece on her right ear, a pad on the left, a
mic boom along the cheek), the pin through the bun, the NTI ID badge on her left lapel, coat buttons, two pens in the
breast pocket. Head gear is built in head-local space (portrait_kit tracing on the head field), body gear in game
space; both return voss2_body.Piece objects plus the space they are in.
"""

import math

import numpy as np

import portrait_kit as pk
import voss2_body as B
import voss2_head as H


def hair_point(direction, lift):
    d = np.asarray(direction, float)
    d = d / np.linalg.norm(d)
    t = pk.trace_outermost(H.head_sdf, H.RAY_CENTRE, d[None], 0.35)[0]
    return H.RAY_CENTRE + d * (t + lift)


def disc(centre, axis, radius, thickness, sides=20, bevel=0.25):
    """A short rounded cylinder (earpiece, button): a swept profile round `axis`."""
    axis = np.asarray(axis, float) / np.linalg.norm(axis)
    pts = [np.asarray(centre) - axis * thickness * 0.5, np.asarray(centre) - axis * thickness * 0.15,
           np.asarray(centre) + axis * thickness * 0.15, np.asarray(centre) + axis * thickness * 0.5]
    prof = [1 - bevel, 1.0, 1.0, 1 - bevel]
    return B.sweep(pts, lambda t, a: radius * float(np.interp(t, [0, 0.35, 0.65, 1], prof)), sides, cap_start=True, cap_end=True)


def headset():
    """Head-local pieces: (band, earpiece, ring, pad, boom, mic)."""
    # Band: an arc over the crown from ear to ear, a little behind the top.
    ear_r = np.array((-0.0805, -0.006, -0.015))
    ear_l = np.array((0.0775, -0.004, -0.016))
    # The band lies on the hair in the plane of the two earpieces and the crown (traced, so it hugs the locks).
    arc = []
    for k in range(17):
        f = -1.2 + 2.4 * k / 16
        arc.append(hair_point((math.sin(f), math.cos(f), -0.1), 0.0026))
    arc = np.array(arc)
    pts = B.catmull(np.vstack([ear_r + np.array((0.0, 0.014, 0.0)), arc, ear_l + np.array((0.0, 0.012, 0.0))]), 28)
    band = B.sweep(pts, lambda t, a: 0.0042, 8, squash=lambda t: (0.45, 1.0))
    ear = disc(ear_r, (-1, 0, 0.05), 0.019, 0.013, sides=22)
    ring = disc(ear_r + np.array((-0.0072, 0, 0.0)), (-1, 0, 0.05), 0.0135, 0.0022, sides=22, bevel=0.1)
    pad = disc(ear_l, (1, 0, 0.05), 0.012, 0.009, sides=16)
    # The boom keeps a finger's width off the cheek; the mic sits in front of the mouth corner, not on it.
    boom_pts = B.catmull([ear_r + np.array((-0.006, -0.01, 0.01)), np.array((-0.086, -0.036, 0.042)),
                          np.array((-0.07, -0.056, 0.08)), np.array((-0.046, -0.062, 0.1))], 12)
    boom = B.sweep(boom_pts, lambda t, a: 0.0017, 6)
    mic = B.sweep(B.catmull([boom_pts[-1] + np.array((-0.003, 0, -0.001)), boom_pts[-1] + np.array((0.005, -0.0005, 0.0015))], 4),
                  lambda t, a: 0.0026 * math.sin(math.pi * (0.15 + 0.7 * t)) ** 0.5, 10, cap_start=True, cap_end=True)
    return band, ear, ring, pad, boom, mic


def hair_pin():
    a = H.BUN + np.array((-0.048, 0.016, 0.006))
    b = H.BUN + np.array((0.05, -0.004, -0.01))
    rod = B.sweep([a, a * 0.5 + b * 0.5, b], lambda t, a_: 0.0021, 8, cap_start=True, cap_end=True)
    tip = B.sweep([b, b + (b - a) / np.linalg.norm(b - a) * 0.006], lambda t, a_: 0.0034 * (1 - 0.6 * t), 8, cap_start=True, cap_end=True)
    return rod, tip


# -------------------------------------------------------------------------------------------------------------
# Body gear (game space)
# -------------------------------------------------------------------------------------------------------------

def surface_frame(x, y, off):
    """Point on the coat's front at (x, y) pushed out by `off`, with (right, up, out) axes."""
    hw = B.section(y)[0] + B.coat_ease(y)
    th = math.asin(max(-0.98, min(0.98, x / hw)))
    p = B.coat_point(th, y)
    p2 = B.coat_point(th + 0.01, y)
    right = p2 - p
    right /= np.linalg.norm(right)
    up = np.array((0.0, 1.0, 0.0))
    out = np.cross(right, up)
    out /= np.linalg.norm(out)
    if out[2] < 0:
        out = -out
    return p + out * off, right, up, out


def card():
    """The NTI ID badge clipped to her left lapel: a rounded card with real thickness; params (u, v, face)."""
    c, right, up, out = surface_frame(0.088, 1.155, 0.0145)
    tilt = math.radians(-8)
    r2 = right * math.cos(tilt) + up * math.sin(tilt)
    u2 = -right * math.sin(tilt) + up * math.cos(tilt)
    w, h, t = 0.021, 0.029, 0.0011
    nu, nv = 6, 8
    pieces = None
    for side in (1, -1):
        def pt(u, v, side=side):
            # Rounded rectangle via a superellipse squeeze of the corners.
            x = (u * 2 - 1)
            y = (v * 2 - 1)
            k = (abs(x) ** 6 + abs(y) ** 6) ** (1 / 6)
            m = max(abs(x), abs(y))
            s = m / max(k, 1e-9) if k > 0 else 1.0
            return c + r2 * x * w * s + u2 * y * h * s + out * t * side
        piece = B.loft(pt, np.linspace(0, 1, nu), np.linspace(0, 1, nv), flip=side < 0,
                       params_fn=lambda u, v, side=side: (u, v, 0.0 if side > 0 else 1.0))
        pieces = piece if pieces is None else pieces.merge(piece)
    # Edge band joining front and back.
    n = nu * nv
    ring = [(0, i) for i in range(nu)] + [(j, nu - 1) for j in range(1, nv)] + [(nv - 1, i) for i in range(nu - 2, -1, -1)] + \
        [(j, 0) for j in range(nv - 2, 0, -1)]
    for k in range(len(ring)):
        j0, i0 = ring[k]
        j1, i1 = ring[(k + 1) % len(ring)]
        a, b = j0 * nu + i0, j1 * nu + i1
        pieces.faces.append((a, n + a, n + b, b))
        pieces.uv.append([(0, 0), (0.002, 0), (0.002, 0.003), (0, 0.003)])
    clip = B.sweep([c + u2 * (h + 0.004) + out * 0.0, c + u2 * (h - 0.004) + out * 0.0], lambda tt, a: 0.0035, 8,
                   cap_start=True, cap_end=True, squash=lambda tt: (1.0, 0.45))
    return pieces, clip


def buttons():
    out = None
    for y in (1.062, 0.968, 0.874, 0.78, 0.686):
        c, right, up, n = surface_frame(-0.006, y, 0.0028)
        d = disc(c, n, 0.0068, 0.0032, sides=14)
        out = d if out is None else out.merge(d)
    return out


def pens():
    out = None
    for x, h in ((0.066, 0.05), (0.078, 0.044)):
        base, right, up, n = surface_frame(x, 1.06, 0.004)
        p = B.sweep([base, base + up * h], lambda t, a: 0.0036, 8, cap_start=True, cap_end=True)
        clip = B.sweep([base + up * (h - 0.002) + n * 0.003, base + up * (h - 0.02) + n * 0.0036], lambda t, a: 0.0012, 4,
                       cap_start=True, cap_end=True)
        p = p.merge(clip)
        out = p if out is None else out.merge(p)
    return out


def breast_pocket():
    """A patch on her left chest behind the pens."""
    nu, nv = 7, 5

    def pt(u, v):
        y = 1.03 + 0.062 * v
        x = 0.056 + 0.05 * u
        c, right, up, n = surface_frame(x, y, 0.0035)
        return c
    return B.loft(pt, np.linspace(0, 1, nu), np.linspace(0, 1, nv), params_fn=lambda u, v: (u, v, 0.0))
