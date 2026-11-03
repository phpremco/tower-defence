"""The Crysium look, shared by the level crystals and the crystals fused into Burrower chitin.

A Crysium shard is dark violet at the root, pink through the body and icy white at the tip. The light sits inside:
it leaks out of the flutes (the grooves of a fluted shard) and through bright node rings along the length, while
the facets themselves stay a step darker so the shape still reads under bloom."""

import numpy as np

import common as C

ROOT = np.array((0.3, 0.07, 0.34))
PINK = np.array((0.88, 0.32, 0.66))
ICE = np.array((0.84, 0.93, 1.0))
GLOW_PINK = np.array((1.0, 0.45, 0.8))
GLOW_ICE = np.array((0.75, 0.92, 1.0))

PALETTES = {
    # name: (root, body, tip) for the albedo, (body, tip) for the glow
    "pink": ((ROOT, PINK, ICE), (GLOW_PINK, GLOW_ICE)),
    "ice": ((ROOT * 0.8 + np.array((0.0, 0.05, 0.1)), np.array((0.6, 0.75, 0.98)), ICE), (np.array((0.6, 0.8, 1.0)), GLOW_ICE)),
    "rose": ((ROOT, np.array((1.0, 0.5, 0.62)), np.array((1.0, 0.9, 0.95))), (np.array((1.0, 0.45, 0.55)), np.array((1.0, 0.85, 0.92)))),
    # Crysium that grew through the stone layer and took its heat (Cragback, other Stone species): same shapes and
    # inner light, warm palette so the glow stays in the species colour.
    "ember": ((np.array((0.22, 0.06, 0.04)), np.array((0.92, 0.42, 0.12)), np.array((1.0, 0.86, 0.6))),
              (np.array((1.0, 0.48, 0.14)), np.array((1.0, 0.82, 0.5)))),
}


def shard_style(palette="pink", strength=1.0, nodes=(0.32, 0.58, 0.8), flute_glow=0.6, seed=0):
    (root, body, tip), (glow_body, glow_tip) = PALETTES[palette]

    def style(s, idx, part):
        # Measured against the shard's real axis, so bent and helical shards get their flutes right.
        z, radius, dist = C.tube_param(part, s.p[idx])
        rho = dist / np.maximum(radius, part.radii[0] * 0.05)
        L = C.local_points(part, s.p[idx])
        flute = 1 - C.smoothstep(0.62, 0.9, rho)
        lower = C.smoothstep(0.0, 0.45, z)
        upper = C.smoothstep(0.5, 1.0, z)
        col = C.lerp3(C.lerp3(root, body, lower), tip, upper)
        streak = 0.5 + 0.5 * C.value_noise(np.c_[L[:, 0] * 2, L[:, 1] * 2, z * 0.3], 5, seed)
        # Dark inclusions: thin fracture lines inside the crystal, so it reads as stone, not candy.
        crack = 1 - C.smoothstep(0.0, 0.06, np.abs(C.value_noise(s.p[idx], 14, seed + 7)))
        crack *= C.smoothstep(-0.3, 0.3, C.value_noise(s.p[idx], 3, seed + 8))
        col = C.lerp3(col, root * 0.4, crack * 0.75)
        s.albedo[idx] = col * (0.58 + 0.2 * streak)[:, None]
        node = np.zeros(len(idx))
        for c in nodes:
            node = np.maximum(node, 1 - C.smoothstep(0.0, 0.035, np.abs(z - c)))
        glow = C.lerp3(glow_body, glow_tip, upper)
        amount = 0.08 + 0.3 * lower + flute * flute_glow * lower + node * 0.7 + upper * 0.35
        s.emit[idx] = np.clip(glow * (amount * strength * (1 - 0.6 * crack))[:, None], 0, 1)
        # Frosted mineral, not glass: a moderate sheen on the ridges, matte flutes and cracks.
        s.rough[idx] = 0.32 + 0.35 * flute + 0.2 * crack
        s.disp[idx] = 0.0
        s.fam[idx] = (0.0, 0.0, 1.0, 0.0)
        # Growth layers run across the shard axis.
        s.flow[idx] = z * C.axis_length(part)
    return style
