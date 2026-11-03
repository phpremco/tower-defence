"""Round 3: per-pixel micro-detail for the bake (pores, growth lines, scratches, chips, veins, facets, grain).

Vertex styles (chitin.py, crysium.py, build_props.py) say *what* a surface is: they write a material-family weight
per vertex (`Fam`: R chitin, G membrane, B crystal, A rock) and a `Flow` coordinate in world units whose iso-lines
are growth lines (along the plates for chitin, along the shard axis for crystal, height for rock strata).

This module builds the shader graph on the detailed source that turns those into high-frequency detail. The graph
is evaluated per bake sample, so detail is limited by the texture, not by the source vertex density, and the
8 samples per texel average sub-texel detail instead of aliasing it. One height field drives four outputs:
the baked normal (via Bump), cavity grime and edge wear in the albedo, micro occlusion in the AO and roughness.

All sizes are in "Skitter units" (P / k): a boss with k = 4 gets the same look, scaled up, so it reads the same at
its own game size. Height comes out in world units (multiplied by k) so the bump slopes stay scale-invariant.
"""

import math

import bpy

FAMILIES = ("chitin", "membrane", "crystal", "rock")
# Round 6: our own side (the hero, later towers and drones) has its own four families in the same Fam channels.
SUIT_FAMILIES = ("weave", "ceramic", "metal", "rubber")


class Graph:
    """Small expression helper over a node tree, so the detail graph reads as maths and not as wiring."""

    def __init__(self, nt):
        self.nt = nt

    def set(self, socket, value):
        if isinstance(value, bpy.types.NodeSocket):
            self.nt.links.new(value, socket)
        elif value is not None:
            socket.default_value = value

    def node(self, bl_idname, **props):
        n = self.nt.nodes.new(bl_idname)
        for key, value in props.items():
            setattr(n, key, value)
        return n

    def math(self, op, a, b=0.0, c=None, clamp=False):
        n = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        self.set(n.inputs[0], a)
        self.set(n.inputs[1], b)
        if c is not None:
            self.set(n.inputs[2], c)
        return n.outputs[0]

    def add(self, *xs):
        out = xs[0]
        for x in xs[1:]:
            out = self.math("ADD", out, x)
        return out

    def mul(self, a, b):
        return self.math("MULTIPLY", a, b)

    def sub(self, a, b):
        return self.math("SUBTRACT", a, b)

    def inv(self, a):
        return self.math("SUBTRACT", 1.0, a)

    def map(self, x, a, b, c=0.0, d=1.0, smooth=True):
        n = self.node("ShaderNodeMapRange", interpolation_type="SMOOTHSTEP" if smooth else "LINEAR", clamp=True)
        self.set(n.inputs["Value"], x)
        self.set(n.inputs["From Min"], a)
        self.set(n.inputs["From Max"], b)
        self.set(n.inputs["To Min"], c)
        self.set(n.inputs["To Max"], d)
        return n.outputs["Result"]

    def less(self, a, b):
        return self.math("LESS_THAN", a, b)

    def vec(self, op, a, b=None, scale=None):
        n = self.node("ShaderNodeVectorMath", operation=op)
        self.set(n.inputs[0], a)
        if b is not None:
            self.set(n.inputs[1], b)
        if scale is not None:
            self.set(n.inputs["Scale"], scale)
        return n

    def vscale(self, v, s):
        return self.vec("SCALE", v, scale=s).outputs[0]

    def mapping(self, v, rotation=(0, 0, 0), scale=(1, 1, 1), location=(0, 0, 0)):
        n = self.node("ShaderNodeMapping")
        self.set(n.inputs["Vector"], v)
        n.inputs["Location"].default_value = location
        n.inputs["Rotation"].default_value = rotation
        n.inputs["Scale"].default_value = scale
        return n.outputs[0]

    def noise(self, v, scale, detail=2.0, rough=0.5, w=0.0, distortion=0.0):
        n = self.node("ShaderNodeTexNoise", noise_dimensions="4D")
        self.set(n.inputs["Vector"], v)
        n.inputs["W"].default_value = w
        n.inputs["Scale"].default_value = scale
        n.inputs["Detail"].default_value = detail
        n.inputs["Roughness"].default_value = rough
        n.inputs["Distortion"].default_value = distortion
        return n.outputs["Factor"]

    def voronoi(self, v, scale, feature="F1", w=0.0, randomness=1.0):
        n = self.node("ShaderNodeTexVoronoi", voronoi_dimensions="4D", feature=feature)
        self.set(n.inputs["Vector"], v)
        n.inputs["W"].default_value = w
        n.inputs["Scale"].default_value = scale
        n.inputs["Randomness"].default_value = randomness
        return n

    def add_vec(self, a, b):
        return self.vec("ADD", a, b).outputs[0]

    def node_noise_color(self, p, scale, w):
        """A noise colour centred on zero: a 3D offset field for warping coordinates."""
        n = self.node("ShaderNodeTexNoise", noise_dimensions="4D")
        self.set(n.inputs["Vector"], p)
        n.inputs["W"].default_value = w
        n.inputs["Scale"].default_value = scale
        n.inputs["Detail"].default_value = 2.0
        return self.vec("SUBTRACT", n.outputs["Color"], (0.5, 0.5, 0.5)).outputs[0]

    def rgb(self, color):
        n = self.node("ShaderNodeSeparateColor")
        self.set(n.inputs[0], color)
        return n.outputs[0], n.outputs[1], n.outputs[2]

    def attr(self, name):
        n = self.node("ShaderNodeAttribute", attribute_name=name)
        return n


class Detail:
    """Outputs of one family's detail: height (Skitter units), cavity 0..1 (grime, micro AO), wear 0..1 (lighter
    chipped or scratched edges), roughness delta and an emission factor (fractures dim the crystal glow)."""

    def __init__(self, height, cavity, wear, rough, emit=None):
        self.height = height
        self.cavity = cavity
        self.wear = wear
        self.rough = rough
        self.emit = emit


def pores(g, p, scale, depth, keep, w, region):
    """Pits of varied size in a random subset of voronoi cells."""
    v = g.voronoi(p, scale, w=w)
    r, gg, _ = g.rgb(v.outputs["Color"])
    radius = g.add(0.08, g.mul(gg, 0.22))
    # A conical pit of `radius` (cell units): no flat floor and no rim, so it reads as a puncture, not a crater.
    cone = g.math("POWER", g.map(g.math("DIVIDE", v.outputs["Distance"], radius), 0.0, 1.0, 1.0, 0.0, smooth=False), 1.6)
    mask = g.mul(g.less(r, keep), region)
    pore = g.mul(cone, mask)
    return pore, g.mul(pore, -depth)


def scratches(g, p, density, w):
    """Thin straight-ish lines in two directions, only in patches."""
    out = None
    for i, rot in enumerate(((0.3, 1.1, 0.2), (1.2, -0.4, 2.1))):
        m = g.mapping(p, rotation=rot, scale=(density, density * 0.04, density * 0.04))
        n = g.noise(m, 1.0, detail=1.0, w=w + i * 3.1)
        line = g.map(g.math("ABSOLUTE", g.sub(n, 0.5)), 0.0, 0.018, 1.0, 0.0)
        patch = g.map(g.noise(p, 5.0, detail=1.0, w=w + 7 + i), 0.52, 0.64)
        s = g.mul(line, patch)
        out = s if out is None else g.math("MAXIMUM", out, s)
    return out


def chips(g, p, pointiness, scale, w, amount=0.45):
    """Chipped plate edges: whole voronoi cells broken out of the most convex rims."""
    edge = g.map(pointiness, 0.53, 0.62)
    f1 = g.voronoi(p, scale, w=w)
    rnd, _, _ = g.rgb(f1.outputs["Color"])
    dte = g.voronoi(p, scale, feature="DISTANCE_TO_EDGE", w=w)
    wall = g.map(dte.outputs["Distance"], 0.0, 0.08)
    chip = g.mul(g.mul(edge, g.less(rnd, amount)), wall)
    return chip


def chitin_detail(g, p, flow, pointiness, q):
    region = g.map(g.noise(p, 3.0, detail=2.0, w=1.0), 0.38, 0.62, 0.35, 1.25, smooth=False)
    big_pore, h1 = pores(g, p, q["pore_scale"], q["pore_depth"], 0.72, 2.0, region)
    fine_pore, h2 = pores(g, p, q["pore_scale"] * 2.6, q["pore_depth"] * 0.45, 0.6, 5.0, region)
    # Micro-reticulation: a shallow polygonal network like the cells of real cuticle.
    net = g.voronoi(p, q["pore_scale"] * q["net_scale"], feature="DISTANCE_TO_EDGE", w=7.0)
    groove = g.map(net.outputs["Distance"], 0.0, 0.12, 1.0, 0.0)
    tv = g.voronoi(p, q["pore_scale"] * 0.38, w=9.0)
    _, tg, _ = g.rgb(tv.outputs["Color"])
    tub_region = g.map(g.noise(p, 2.2, detail=1.0, w=11.0), 0.46, 0.6)
    tub = g.math("POWER", g.map(tv.outputs["Distance"], 0.0, 0.42, 1.0, 0.0), 2.0)
    tub = g.mul(g.mul(tub, g.less(tg, 0.4)), tub_region)
    wobble = g.mul(g.noise(p, 22.0, detail=2.0, w=13.0), 5.0)
    s = g.math("SINE", g.add(g.mul(flow, 2 * math.pi * q["growth_freq"]), wobble))
    growth = g.mul(g.map(s, 0.45, 1.0), g.map(g.noise(p, 7.0, detail=1.0, w=15.0), 0.4, 0.58))
    scratch = scratches(g, p, q["scratch_density"], 17.0)
    chip = chips(g, p, pointiness, q["chip_scale"], 19.0)
    height = g.add(h1, h2, g.mul(groove, -q["net"]), g.mul(tub, q["tubercle"]), g.mul(growth, q["growth"]),
                   g.mul(scratch, -q["scratch"]), g.mul(chip, -q["chip"]))
    cavity = g.math("MINIMUM", g.add(big_pore, g.mul(fine_pore, 0.6), g.mul(scratch, 0.25), g.mul(groove, 0.2)), 1.0)
    wear = g.math("MINIMUM", g.add(g.mul(chip, 0.9), g.mul(scratch, 0.5), g.mul(tub, 0.25)), 1.0)
    rough = g.add(g.mul(cavity, 0.12), g.mul(scratch, -0.12), g.mul(growth, -0.03))
    return Detail(height, cavity, wear, rough)


def membrane_detail(g, p, flow, pointiness, q):
    # Veins meander: the cell borders are warped by a noise field.
    warped = g.add_vec(p, g.vscale(g.node_noise_color(p, 5.0, 20.0), 0.04))
    big = g.voronoi(warped, q["vein_scale"], feature="DISTANCE_TO_EDGE", w=21.0)
    vein = g.map(big.outputs["Distance"], 0.0, 0.07, 1.0, 0.0)
    fine = g.voronoi(p, q["vein_scale"] * 2.8, feature="DISTANCE_TO_EDGE", w=23.0)
    veinlet = g.mul(g.map(fine.outputs["Distance"], 0.0, 0.05, 1.0, 0.0), 0.55)
    wr = g.mapping(p, rotation=(0.4, 0.2, 1.0), scale=(1.0, 1.0, 5.0))
    wrinkle = g.sub(g.noise(wr, q["wrinkle_scale"], detail=3.0, rough=0.6, w=25.0), 0.5)
    bumps = g.sub(g.noise(p, q["wrinkle_scale"] * 6.0, detail=1.0, w=27.0), 0.5)
    height = g.add(g.mul(vein, q["vein"]), g.mul(veinlet, q["vein"] * 0.5), g.mul(wrinkle, q["wrinkle"]),
                   g.mul(bumps, q["wrinkle"] * 0.35))
    cavity = g.math("MINIMUM", g.add(g.mul(vein, 0.55), g.mul(veinlet, 0.35)), 1.0)
    wear = g.mul(g.map(wrinkle, 0.1, 0.3), 0.35)
    rough = g.add(g.mul(vein, 0.1), g.mul(g.map(wrinkle, -0.2, 0.2), -0.06))
    return Detail(height, cavity, wear, rough)


def crystal_detail(g, p, flow, pointiness, q):
    v = g.voronoi(p, q["facet_scale"], w=31.0)
    rel = g.vec("SUBTRACT", p, v.outputs["Position"]).outputs[0]
    tilt = g.vec("SUBTRACT", v.outputs["Color"], (0.5, 0.5, 0.5)).outputs[0]
    facet = g.vec("DOT_PRODUCT", rel, tilt).outputs["Value"]
    wobble = g.mul(g.noise(p, 9.0, detail=2.0, w=33.0), 3.0)
    s = g.math("SINE", g.add(g.mul(flow, 2 * math.pi * q["layer_freq"]), wobble))
    layer = g.mul(g.map(s, 0.6, 1.0), g.map(g.noise(p, 4.0, detail=1.0, w=35.0), 0.38, 0.6))
    frost_region = g.map(g.noise(p, 3.5, detail=2.0, w=37.0), 0.4, 0.62, 0.3, 1.2, smooth=False)
    frost, h_frost = pores(g, p, q["frost_scale"], q["frost"], 0.85, 39.0, frost_region)
    grain = g.sub(g.noise(p, q["frost_scale"] * 2.5, detail=1.0, w=41.0), 0.5)
    fr = g.voronoi(g.mapping(p, scale=(1.0, 1.0, 0.35)), q["fracture_scale"], feature="DISTANCE_TO_EDGE", w=43.0)
    fracture = g.mul(g.map(fr.outputs["Distance"], 0.0, 0.03, 1.0, 0.0),
                     g.map(g.noise(p, 2.5, detail=1.0, w=45.0), 0.45, 0.6))
    chip = chips(g, p, pointiness, q["chip_scale"], 47.0, amount=0.5)
    height = g.add(g.mul(facet, q["facet"]), g.mul(layer, q["layer"]), h_frost, g.mul(grain, q["frost"] * 0.25),
                   g.mul(fracture, -q["fracture"]), g.mul(chip, -q["chip"]))
    cavity = g.math("MINIMUM", g.add(g.mul(fracture, 0.7), g.mul(frost, 0.25)), 1.0)
    # Frost reads lighter (scattering white), chipped edges show fresh, bright crystal.
    wear = g.math("MINIMUM", g.add(g.mul(frost, 0.45), g.mul(chip, 0.8), g.mul(layer, 0.15)), 1.0)
    rough = g.add(g.mul(frost, 0.22), g.mul(fracture, 0.15), g.mul(chip, 0.1))
    # The inner light shows the structure: each facet glows a little brighter or dimmer, fractures and frost
    # block some of it. The average stays close to 1 so the prop's overall glow (the design) is unchanged.
    facet_rand, _, _ = g.rgb(v.outputs["Color"])
    emit = g.mul(g.mul(g.inv(g.mul(fracture, 0.6)), g.add(0.88, g.mul(facet_rand, 0.3))), g.inv(g.mul(frost, 0.35)))
    return Detail(height, cavity, wear, rough, emit)


def rock_detail(g, p, flow, pointiness, q):
    grain = g.add(g.mul(g.sub(g.noise(p, q["grain_scale"], detail=3.0, rough=0.6, w=51.0), 0.5), 1.0),
                  g.mul(g.sub(g.noise(p, q["grain_scale"] * 0.25, detail=2.0, w=53.0), 0.5), 1.6))
    region = g.map(g.noise(p, 2.5, detail=2.0, w=55.0), 0.4, 0.62, 0.3, 1.3, smooth=False)
    pit, h_pit = pores(g, p, q["pit_scale"], q["pit"], 0.55, 57.0, region)
    warped = g.add_vec(p, g.vscale(g.node_noise_color(p, 3.0, 59.0), q["crack_warp"]))
    cr = g.voronoi(warped, q["crack_scale"], feature="DISTANCE_TO_EDGE", w=61.0)
    crack = g.mul(g.map(cr.outputs["Distance"], 0.0, 0.022, 1.0, 0.0),
                  g.map(g.noise(p, 2.0, detail=1.0, w=63.0), 0.42, 0.58))
    wobble = g.mul(g.noise(p, 6.0, detail=2.0, w=65.0), 4.0)
    strata = g.map(g.math("SINE", g.add(g.mul(flow, 2 * math.pi * q["strata_freq"]), wobble)), 0.5, 1.0)
    chip = chips(g, p, pointiness, q["chip_scale"], 67.0, amount=0.4)
    height = g.add(g.mul(grain, q["grain"]), h_pit, g.mul(crack, -q["crack"]), g.mul(strata, q["strata"]),
                   g.mul(chip, -q["chip"]))
    cavity = g.math("MINIMUM", g.add(g.mul(crack, 0.9), g.mul(pit, 0.6), g.mul(g.map(grain, -0.25, 0.0, 0.25, 0.0), 1.0)), 1.0)
    wear = g.math("MINIMUM", g.add(g.mul(chip, 0.8), g.mul(g.map(grain, 0.1, 0.35), 0.3)), 1.0)
    rough = g.add(g.mul(cavity, 0.06), g.mul(chip, -0.05))
    return Detail(height, cavity, wear, rough)


def weave_detail(g, p, flow, pointiness, q):
    """Padded suit fabric: stitched quilting seams (iso-lines of Flow), a fine thread grain and puckering beside the
    seams. The weave itself is far below a texel; it lives in the tiling Detail_Weave_Normal."""
    s = g.math("SINE", g.mul(flow, 2 * math.pi * q["stitch_freq"]))
    seam = g.map(s, q["seam_from"], 1.0)
    dash = g.map(g.noise(p, q["dash_scale"], detail=0.0, w=71.0), 0.45, 0.55)
    stitch = g.mul(g.map(s, 0.985, 1.0), dash)
    pucker = g.mul(g.sub(g.noise(p, q["pucker_scale"], detail=2.0, w=73.0), 0.5), g.map(s, 0.6, 0.95))
    grain = g.sub(g.noise(p, q["grain_scale"], detail=2.0, rough=0.7, w=75.0), 0.5)
    height = g.add(g.mul(seam, -q["seam"]), g.mul(stitch, q["stitch"]), g.mul(pucker, q["pucker"]),
                   g.mul(grain, q["grain"]))
    cavity = g.math("MINIMUM", g.add(g.mul(seam, 0.55), g.mul(g.map(grain, -0.3, 0.0, 0.25, 0.0), 1.0)), 1.0)
    # Worn, slightly fluffed fabric on the most convex padding.
    wear = g.add(g.mul(g.map(pointiness, 0.52, 0.6), 0.35), g.mul(stitch, 0.4))
    rough = g.add(g.mul(seam, 0.05), 0.02)
    return Detail(height, cavity, wear, rough)


def ceramic_detail(g, p, flow, pointiness, q):
    """Hard white plates: fine scratches, chipped convex edges and sparse pits. On a white plate damage reads
    darker (dirt in the scratch, the grey composite under a chip), so it goes into the cavity term, not wear."""
    scratch = scratches(g, p, q["scratch_density"], 81.0)
    fine = scratches(g, p, q["scratch_density"] * 2.3, 83.0)
    chip = chips(g, p, pointiness, q["chip_scale"], 85.0, amount=q["chip_amount"])
    region = g.map(g.noise(p, 3.0, detail=1.0, w=87.0), 0.45, 0.6)
    pit, h_pit = pores(g, p, q["pit_scale"], q["pit"], 0.25, 89.0, region)
    peel = g.sub(g.noise(p, q["peel_scale"], detail=1.0, w=91.0), 0.5)
    height = g.add(g.mul(scratch, -q["scratch"]), g.mul(fine, -q["scratch"] * 0.5), g.mul(chip, -q["chip"]), h_pit,
                   g.mul(peel, q["peel"]))
    cavity = g.math("MINIMUM", g.add(g.mul(chip, 0.95), g.mul(scratch, 0.45), g.mul(fine, 0.25), g.mul(pit, 0.5)), 1.0)
    wear = g.mul(fine, 0.1)
    rough = g.add(g.mul(scratch, 0.12), g.mul(chip, 0.25), g.mul(fine, 0.06))
    return Detail(height, cavity, wear, rough)


def metal_detail(g, p, flow, pointiness, q):
    """Gunmetal gear: brushed lines (iso-lines of Flow), bright scratches and worn edges, a few dents."""
    wobble = g.mul(g.noise(p, 30.0, detail=1.0, w=93.0), 2.0)
    lines = g.math("SINE", g.add(g.mul(flow, 2 * math.pi * q["brush_freq"]), wobble))
    brushed = g.mul(lines, g.map(g.noise(p, q["brush_patch"], detail=1.0, w=95.0), 0.3, 0.7, 0.3, 1.0))
    scratch = scratches(g, p, q["scratch_density"], 97.0)
    chip = chips(g, p, pointiness, q["chip_scale"], 99.0, amount=0.5)
    dent = g.sub(g.noise(p, q["dent_scale"], detail=1.0, w=101.0), 0.5)
    height = g.add(g.mul(brushed, q["brush"]), g.mul(scratch, -q["scratch"]), g.mul(chip, -q["chip"]),
                   g.mul(dent, q["dent"]))
    cavity = g.math("MINIMUM", g.add(g.mul(scratch, 0.15), g.mul(g.map(dent, -0.3, -0.1), 0.2)), 1.0)
    wear = g.math("MINIMUM", g.add(g.mul(scratch, 0.9), g.mul(chip, 1.0)), 1.0)
    rough = g.add(g.mul(scratch, -0.1), g.mul(chip, -0.15), g.mul(brushed, 0.03))
    return Detail(height, cavity, wear, rough)


def rubber_detail(g, p, flow, pointiness, q):
    """Soles, gloves and joint pads: fine moulded grain and pores, scuffed lighter on the convex parts."""
    pore, h = pores(g, p, q["pore_scale"], q["pore"], 0.55, 103.0, 1.0)
    grain = g.sub(g.noise(p, q["grain_scale"], detail=2.0, w=105.0), 0.5)
    height = g.add(h, g.mul(grain, q["grain"]))
    cavity = g.math("MINIMUM", g.mul(pore, 0.6), 1.0)
    wear = g.mul(g.map(pointiness, 0.52, 0.62), 0.4)
    rough = g.mul(pore, 0.05)
    return Detail(height, cavity, wear, rough)


DEFAULTS = {
    # Depths and heights in Skitter units (the Skitter is about 1.1 long); scales are cells per Skitter unit.
    "chitin": dict(pore_scale=110.0, pore_depth=0.0008, net_scale=0.9, net=0.00012, tubercle=0.0005,
                   growth_freq=70.0, growth=0.00025, scratch_density=260.0, scratch=0.00025, chip_scale=40.0,
                   chip=0.0009),
    "membrane": dict(vein_scale=26.0, vein=0.0007, wrinkle_scale=40.0, wrinkle=0.0012),
    "crystal": dict(facet_scale=28.0, facet=0.05, layer_freq=55.0, layer=0.0002, frost_scale=190.0, frost=0.0003,
                    fracture_scale=7.0, fracture=0.0005, chip_scale=30.0, chip=0.0008),
    "rock": dict(grain_scale=220.0, grain=0.0006, pit_scale=80.0, pit=0.0016, crack_scale=5.0, crack=0.004,
                 crack_warp=0.06, strata_freq=12.0, strata=0.0007, chip_scale=24.0, chip=0.0015),
    # Suit families (round 6): sizes in metres-like hero units (the hero is 1.55 tall, k = 1).
    "weave": dict(stitch_freq=26.0, seam_from=0.93, seam=0.0012, dash_scale=900.0, stitch=0.00025,
                  pucker_scale=60.0, pucker=0.0005, grain_scale=700.0, grain=0.00012),
    "ceramic": dict(scratch_density=140.0, scratch=0.00022, chip_scale=55.0, chip_amount=0.5, chip=0.0009,
                    pit_scale=140.0, pit=0.00035, peel_scale=90.0, peel=0.00012),
    "metal": dict(brush_freq=420.0, brush=0.00004, brush_patch=6.0, scratch_density=170.0, scratch=0.00018,
                  chip_scale=70.0, chip=0.0006, dent_scale=25.0, dent=0.0004),
    "rubber": dict(pore_scale=260.0, pore=0.00025, grain_scale=500.0, grain=0.00015),
}

BUILDERS = {"chitin": chitin_detail, "membrane": membrane_detail, "crystal": crystal_detail, "rock": rock_detail,
            "weave": weave_detail, "ceramic": ceramic_detail, "metal": metal_detail, "rubber": rubber_detail}


class DetailOutputs:
    """Combined sockets of all families, weighted by the per-vertex family attribute."""

    def __init__(self, height, cavity, wear, rough, emit, families):
        self.height = height
        self.cavity = cavity
        self.wear = wear
        self.rough = rough
        self.emit = emit
        self.families = families


def build(g, k=1.0, params=None, strength=1.0, family_set=FAMILIES):
    """Adds the micro-detail graph to a high-source material. `params` overrides DEFAULTS per family;
    `strength` scales every height (one knob per asset for readability at game size). `family_set` names the
    four families in Fam channel order (R, G, B, A): FAMILIES for Burrowers and props, SUIT_FAMILIES for our side."""
    params = params or {}
    tc = g.node("ShaderNodeTexCoord")
    p = g.vscale(tc.outputs["Object"], 1.0 / k)
    fam = g.attr("Fam")
    flow_attr = g.attr("Flow")
    flow = g.math("DIVIDE", flow_attr.outputs["Factor"], k)
    geo = g.node("ShaderNodeNewGeometry")
    pointiness = geo.outputs["Pointiness"]
    weights = list(g.rgb(fam.outputs["Color"])) + [fam.outputs["Alpha"]]
    height = cavity = wear = rough = None
    emit = None
    for family, weight in zip(family_set, weights):
        q = dict(DEFAULTS[family])
        q.update(params.get(family, {}))
        d = BUILDERS[family](g, p, flow, pointiness, q)
        h = g.mul(d.height, weight)
        height = h if height is None else g.add(height, h)
        c = g.mul(d.cavity, weight)
        cavity = c if cavity is None else g.add(cavity, c)
        w = g.mul(d.wear, weight)
        wear = w if wear is None else g.add(wear, w)
        r = g.mul(d.rough, weight)
        rough = r if rough is None else g.add(rough, r)
        if d.emit is not None:
            # lerp(1, emit, weight)
            e = g.add(g.inv(weight), g.mul(d.emit, weight))
            emit = e if emit is None else g.mul(emit, e)
    height = g.mul(height, k * strength)
    return DetailOutputs(height, g.math("MINIMUM", cavity, 1.0), g.math("MINIMUM", wear, 1.0), rough,
                         emit if emit is not None else 1.0, weights)
