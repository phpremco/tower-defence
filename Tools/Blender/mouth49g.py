"""Round 49G: a coherent mouth with gums and rooted teeth, shared by the Bulwark, the Crystal Warlord (Remco on 49F:
"those teeth are strange with the new mouth") and the Shattered ("the teeth are glued against the head").

Why the 49F Bulwark teeth looked like tiles: bulwark_detail placed them where a ray from in front hit the snout, i.e.
ON the front face of the head and jaw (there was no mouth: the jaw ellipsoid sat wedged into the head), pointing out
of it, in a glassy tooth style with a translucent band; the decimation flattened each thin cone into a tile. The
Shattered's upper fangs grew from points inside its dark head sphere, close along its curve, so they read as glued on.

The pattern now (each builder supplies its own shapes as `Mouth`):
- the jaw hangs low enough at the front that a slot opens between the upper jaw and the lower jaw;
- a wet dark gum runs along the underside rim of the upper jaw and along the top rim of the lower jaw, from one corner
  of the mouth round the front to the other (`gum_path`, found by rays against the shapes as built);
- two interlocking rows of conical, hooked teeth are rooted in the gums (`add_teeth`): upper on the head bone pointing
  down and a little out, lower on the jaw bone pointing up and a little in; each starts in its gum's centre and stops
  `clearance` short of the other gum with the jaw at rest (the clips never close it further);
- teeth are matte ivory with dark roots (`ivory_style`), never glassy.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as DB  # noqa: E402
from common import G  # noqa: E402

# Matte ivory (sRGB): a dark brown root where it leaves the gum, an old-bone body, a paler worn tip.
IVORY_ROOT, IVORY_BODY, IVORY_TIP = (0.17, 0.12, 0.07), (0.74, 0.68, 0.54), (0.88, 0.84, 0.72)
GUM_FLESH, GUM_DEEP = (0.3, 0.07, 0.07), (0.07, 0.015, 0.02)
# Round 56 (the Bulwark's and the Warlord's teeth were a few pixels from the play camera): a paler ivory with a darker
# root, for a stronger value step against their darkened gums (opt-in; the Shattered keeps the defaults).
IVORY_BRIGHT = dict(root=(0.1, 0.06, 0.035), body=(0.86, 0.82, 0.7), tip=(0.96, 0.94, 0.87))


class Mouth:
    """A mouth's layout in a builder's units. `u`: (x, forward) points of the mouth line from the left corner round
    the front to the right one; `upper_fn(b)` / `lower_fn(b)` add the shapes whose underside / top carry the gums
    (head and lip; jaw); `ray_from` (below, above) the heights the vertical rays start from."""

    def __init__(self, u, upper_fn, lower_fn, gum_r, gum_sink, ray_from, lower_inset=0.92, clearance=0.0035,
                 upper_teeth=8, lower_teeth=7, tooth_r=0.0042, canine=1.3, out_upper=0.18, out_lower=-0.12):
        self.u, self.upper_fn, self.lower_fn = u, upper_fn, lower_fn
        self.gum_r, self.gum_sink, self.ray_from = gum_r, gum_sink, ray_from
        self.lower_inset, self.clearance = lower_inset, clearance
        self.upper_teeth, self.lower_teeth, self.tooth_r, self.canine = upper_teeth, lower_teeth, tooth_r, canine
        self.out_upper, self.out_lower = out_upper, out_lower


def ivory_style(seed=0, stain=None, root=IVORY_ROOT, body=IVORY_BODY, tip=IVORY_TIP):
    """Matte old ivory: rough (0.62..0.9), no glow, no translucency; growth bands and a dark root stain."""
    return DB.keratin_style(root, body, tip, glow=None, stain=stain, seed=seed, bands=4, translucent=0.0, wet=0.6,
                            family=(0.5, 0.0, 0.0, 0.0))


def gum_style(seed=0, flesh=GUM_FLESH, deep=GUM_DEEP, glow=None, glow_k=0.3, rough=0.48):
    """detail_kit_b.gum_style, less glossy: at 0.14..0.26 the mouth's inside mirrored the sky and read as silver
    metal in the bright previews; `rough` is the new floor (wet, not a mirror)."""
    plain = DB.gum_style(flesh, deep, glow=glow, glow_k=glow_k, seed=seed)

    def style(s, idx, part):
        plain(s, idx, part)
        s.rough[idx] = s.rough[idx] - 0.14 + rough
    return style


def gum_path(m, upper, scale=1.0):
    """The gum line (Blender points x `scale`): the underside of the upper shapes or the top of the lower ones along
    `m.u`, the gum centre `m.gum_sink` inside that surface."""
    tb = C.PartBuilder()
    (m.upper_fn if upper else m.lower_fn)(tb)
    tree = tb.bvh()
    below, above = m.ray_from
    out = []
    for x, f in m.u:
        if upper:
            hit = tree.ray_cast(G(x, below, f), Vector((0, 0, 1)), above - below)[0]
            if hit is None:
                raise RuntimeError(f"mouth: no upper jaw over {(x, f)}")
            p = hit + Vector((0, 0, m.gum_sink))
        else:
            hit = tree.ray_cast(G(x * m.lower_inset, above, f), Vector((0, 0, -1)), above - below)[0]
            if hit is None:
                raise RuntimeError(f"mouth: no lower jaw under {(x, f)}")
            p = hit - Vector((0, 0, m.gum_sink))
        out.append(p * scale)
    return out


def gaps(m):
    """The slot between the two gums' surfaces at every mouth-line point (for the build log)."""
    up, lo = gum_path(m, True), gum_path(m, False)
    return [round(a.z - b.z - 2 * m.gum_r, 4) for a, b in zip(up, lo)]


def _along(curve, length, s_):
    target = s_ * length[-1]
    for i in range(len(curve) - 1):
        if length[i + 1] >= target:
            u = (target - length[i]) / max(length[i + 1] - length[i], 1e-9)
            return curve[i].lerp(curve[i + 1], u), (curve[i + 1] - curve[i]).normalized()
    return curve[-1], (curve[-1] - curve[-2]).normalized()


def add_teeth(b, m, scale, style, tag, name="Tooth"):
    """The two interlocking rows (see the module notes). Upper teeth sit in the middles of `upper_teeth` equal spans of
    the upper gum, lower teeth on the span borders of the lower gum, so a lower tooth stands between two upper ones.
    The two nearest the front corners are heavier (canines). `tag(part, bone, anchor)` records the bone for the
    caller's detail kit. Returns [(bone, length, gap, canine)] in builder units."""
    upper_path, lower_path = gum_path(m, True, scale), gum_path(m, False, scale)
    placed = []
    for upper, count, bone in ((True, m.upper_teeth, "Head"), (False, m.lower_teeth, "Jaw")):
        own, other = (upper_path, lower_path) if upper else (lower_path, upper_path)
        curve = C.catmull_rom(own, 8)
        other_curve = C.catmull_rom(other, 8)
        length = [0.0]
        for a, c in zip(curve, curve[1:]):
            length.append(length[-1] + (c - a).length)
        mid = sum(own, Vector()) / len(own)
        for i in range(count):
            s_ = (i + 0.5) / count if upper else (i + 1.0) / (count + 1)
            p, tangent = _along(curve, length, s_)
            q = min(other_curve, key=lambda v: (v.xy - p.xy).length)
            gap = abs(p.z - q.z) - 2 * m.gum_r * scale
            out = Vector((tangent.y, -tangent.x, 0.0)).normalized()
            if out.dot(Vector((p.x - mid.x, p.y - mid.y, 0.0))) < 0:
                out = -out
            down = Vector((0, 0, -1 if upper else 1))
            d = (down + out * (m.out_upper if upper else m.out_lower)).normalized()
            canine = abs(abs(s_ - 0.5) - 0.2) < 0.07
            reach = m.gum_r * scale + (gap - m.clearance * scale) / d.dot(down)
            ln = reach * (1.0 if canine else 0.86 + 0.08 * math.cos(11.0 * s_))
            r = m.tooth_r * scale * (m.canine if canine else 1.0)
            back = Vector((mid.x - p.x, mid.y - p.y, 0.0))
            back = back.normalized() if back.length > 1e-6 else Vector((0, 1, 0))
            part = b.shard(name, p, d, r, ln, sides=8, flute=0.06, profile=DB.tooth_profile,
                           bend=back * ln * 0.16, rings=10, bone=bone, style=style)
            tag(part, bone, p)
            placed.append((bone, round(ln / scale, 4), round(gap / scale, 4), canine))
    return placed
