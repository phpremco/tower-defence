"""Round 24: Blender previews for the liquid set. Each liquid sits in a cell-shaped pool (the same 'h' pool as the
Unity bank screenshots) with the continent's exported bank pieces, its ground tiling set and the game camera; the
liquid shader is built from the exported textures the way the Unity shader is meant to combine them (see
liquid_shader_notes.py), so the renders show what the textures can do. A distance-to-bank map (numpy, from the cell
mask) stands in for the depth-buffer fade the URP shader uses.

Outputs into the preview directory: liq_<Kind>_game.png (game camera), liq_<Kind>_close.png, liq_<Kind>_strip.png
(four moments, 0.8 s apart), liq_<Kind>_compare.png (current Unity screenshot | this), liq_Mist_*.png.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Vector

import common as C
import env_kit as E
import liquid_kit as K

F32 = np.float32

# The pool, top row = far side (+Y). Matches the shape in the Unity bank screenshots.
POOL_ROWS = ["...X.",
             "...XX",
             "XXXXX",
             "XX.XX",
             "XX.XX"]
POOL_ORIGIN = (3, 2)          # cell of the lower-left character
FIELD = (11, 9)               # cells
PX_PER_CELL = 48              # distance map resolution
TILE_WORLD = {"Water": 3.0, "Lava": 5.5, "Bog": 3.5, "Quicksand": 4.5, "Ice": 4.5, "Corruption": 4.0}

LEVEL = {"Water": -0.2, "Lava": -0.22, "Bog": -0.14, "Quicksand": -0.12, "Ice": -0.06, "Corruption": -0.2}
BASIN_DEPTH = 0.75

ENV = E.ENV
SETS = {  # kind: (bank folder, bank prefix, ground tiling folder, ground name, Unity screenshot, sky/mist tint)
    "Water": ("", "", os.path.join(ENV, "Tiling", "Ground"), "Ground", "final_L4_bank.png", (0.05, 0.06, 0.09)),
    "Lava": ("AshenReach", "Ash_", os.path.join(ENV, "AshenReach", "Tiling", "Ash_Ground"), "Ash_Ground",
             "env_AshenReach_bank.png", (0.07, 0.04, 0.03)),
    "Bog": ("Mirewood", "Mire_", os.path.join(ENV, "Mirewood", "Tiling", "Mire_Mud"), "Mire_Mud",
            "env_Mirewood_bank.png", (0.04, 0.06, 0.05)),
    "Quicksand": ("", "", os.path.join(ENV, "Tiling", "Ground"), "Ground", "final_L2_bank.png", (0.05, 0.05, 0.08)),
    "Ice": ("NorthernCrown", "North_", os.path.join(ENV, "NorthernCrown", "Tiling", "North_Ground"), "North_Ground",
            "env_NorthernCrown_bank.png", (0.06, 0.07, 0.1)),
    "Corruption": ("TheCore", "Core_", os.path.join(ENV, "TheCore", "Tiling", "Core_Ground"), "Core_Ground",
                   "env_TheCore_bank.png", (0.05, 0.03, 0.08)),
}


def pool_cells():
    cells = set()
    for r, row in enumerate(reversed(POOL_ROWS)):
        for c, ch in enumerate(row):
            if ch == "X":
                cells.add((POOL_ORIGIN[0] + c, POOL_ORIGIN[1] + r))
    return cells


def distance_map(cells):
    """Distance (cells) from each point inside the pool to the nearest dry cell; 0 outside. Analytic per cell."""
    w, h = FIELD[0] * PX_PER_CELL, FIELD[1] * PX_PER_CELL
    x, y = K.pixel_grid(h, w)
    px = (x + 0.5) / PX_PER_CELL
    py = FIELD[1] - (y + 0.5) / PX_PER_CELL      # row 0 = top = far side
    d = np.full((h, w), 99.0, F32)
    for i in range(-1, FIELD[0] + 1):
        for j in range(-1, FIELD[1] + 1):
            if (i, j) in cells:
                continue
            dx = np.maximum(np.maximum(i - px, px - (i + 1)), 0)
            dy = np.maximum(np.maximum(j - py, py - (j + 1)), 0)
            d = np.minimum(d, np.sqrt(dx * dx + dy * dy))
    inside = np.zeros((h, w), bool)
    for (i, j) in cells:
        inside |= (px >= i) & (px < i + 1) & (py >= j) & (py < j + 1)
    # Rounded, slightly wobbly shoreline like the bank pieces make it, so the fade is not a perfect rectangle.
    wob = K.spectral(max(h, w), 9, 3, 12)[:h, :w] * 0.04
    return np.where(inside, np.maximum(d + wob, 0), 0).astype(F32)


# ------------------------------------------------------------------------------------------------- node helpers

class Graph:
    def __init__(self, mat):
        self.nt = mat.node_tree
        self.nodes = self.nt.nodes
        self.links = self.nt.links
        self.values = {}

    def node(self, kind, **props):
        n = self.nodes.new(kind)
        for k, v in props.items():
            setattr(n, k, v)
        return n

    def link(self, out, inp):
        if isinstance(out, (int, float)):
            inp.default_value = out
        elif isinstance(out, tuple):
            inp.default_value = out if len(inp.default_value) == len(out) else (*out, 1.0)
        else:
            self.links.new(out, inp)

    def value(self, name, v=0.0):
        n = self.node("ShaderNodeValue")
        n.outputs[0].default_value = v
        self.values[name] = n
        return n.outputs[0]

    def val(self, name):
        """Socket of a named driver value (created on first use)."""
        return (self.values[name].outputs[0] if name in self.values else self.value(name))

    def math(self, op, a, b=0.0, clamp=False):
        n = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        self.link(a, n.inputs[0])
        self.link(b, n.inputs[1])
        return n.outputs[0]

    def vmath(self, op, a, b=(0.0, 0.0, 0.0), scale=None):
        n = self.node("ShaderNodeVectorMath", operation=op)
        self.link(a, n.inputs[0])
        if op == "SCALE":
            self.link(b, n.inputs[3])
        else:
            self.link(b, n.inputs[1])
        return n.outputs[0] if op not in ("DOT_PRODUCT", "LENGTH") else n.outputs[1]

    def combine(self, x, y, z=0.0):
        n = self.node("ShaderNodeCombineXYZ")
        self.link(x, n.inputs[0])
        self.link(y, n.inputs[1])
        self.link(z, n.inputs[2])
        return n.outputs[0]

    def separate(self, v):
        n = self.node("ShaderNodeSeparateXYZ")
        self.link(v, n.inputs[0])
        return n.outputs

    def sep_rgb(self, c):
        n = self.node("ShaderNodeSeparateColor")
        self.link(c, n.inputs[0])
        return n.outputs

    def smooth(self, x, a, b):
        n = self.node("ShaderNodeMapRange", interpolation_type="SMOOTHSTEP")
        self.link(x, n.inputs["Value"])
        n.inputs["From Min"].default_value = a
        n.inputs["From Max"].default_value = b
        return n.outputs["Result"]

    def mixc(self, fac, a, b):
        n = self.node("ShaderNodeMix", data_type="RGBA")
        self.link(fac, n.inputs[0])
        self.link(a, n.inputs[6])
        self.link(b, n.inputs[7])
        return n.outputs[2]

    def mixf(self, fac, a, b):
        n = self.node("ShaderNodeMix", data_type="FLOAT")
        self.link(fac, n.inputs[0])
        self.link(a, n.inputs[2])
        self.link(b, n.inputs[3])
        return n.outputs[0]

    def mixv(self, fac, a, b):
        n = self.node("ShaderNodeMix", data_type="VECTOR")
        self.link(fac, n.inputs[0])
        self.link(a, n.inputs[4])
        self.link(b, n.inputs[5])
        return n.outputs[1]

    def colmul(self, c, k):
        n = self.node("ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
        n.inputs[0].default_value = 1.0
        self.link(c, n.inputs[6])
        self.link(k, n.inputs[7])
        return n.outputs[2]

    def tex(self, path, uv, srgb=False, extension="REPEAT", interp="Linear"):
        n = self.node("ShaderNodeTexImage")
        img = bpy.data.images.load(path, check_existing=True)
        img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
        n.image = img
        n.extension = extension
        n.interpolation = interp
        self.link(uv, n.inputs[0])
        return n

    def normal_map(self, color, strength=1.0):
        n = self.node("ShaderNodeNormalMap")
        self.link(color, n.inputs["Color"])
        n.inputs["Strength"].default_value = strength
        return n.outputs[0]

    def ramp(self, x, stops):
        n = self.node("ShaderNodeValToRGB")
        cr = n.color_ramp
        cr.interpolation = "LINEAR"
        while len(cr.elements) > 1:
            cr.elements.remove(cr.elements[-1])
        cr.elements[0].position = stops[0][0]
        cr.elements[0].color = (*stops[0][1], 1)
        for pos, col in stops[1:]:
            e = cr.elements.new(pos)
            e.color = (*col, 1)
        self.link(x, n.inputs[0])
        return n.outputs[0]


def world_uv(g, tile, offset_name=None, velocity=(0.0, 0.0), rotation=0.0):
    """World XY / tile, optionally rotated and scrolled by velocity * time (tiles per second)."""
    geo = g.node("ShaderNodeNewGeometry")
    p = g.vmath("MULTIPLY", geo.outputs["Position"], (1.0 / tile, 1.0 / tile, 0.0))
    if rotation:
        c, s = math.cos(rotation), math.sin(rotation)
        x, y, _ = g.separate(p)
        p = g.combine(g.math("SUBTRACT", g.math("MULTIPLY", x, c), g.math("MULTIPLY", y, s)),
                      g.math("ADD", g.math("MULTIPLY", x, s), g.math("MULTIPLY", y, c)))
    if velocity != (0.0, 0.0):
        t = g.val("time")
        p = g.vmath("ADD", p, g.vmath("SCALE", g.combine(*velocity), t))
    return p


def flip_uv(g, uv, cols=4):
    """Atlas UV of one flipbook frame (frame chosen by the 'fcol'/'frow' values, row 0 = top of the PNG)."""
    fcol = g.val("fcol")
    frow = g.val("frow")
    fr = g.vmath("FRACTION", uv)
    x, y, _ = g.separate(fr)
    u = g.math("DIVIDE", g.math("ADD", x, fcol), cols)
    v = g.math("DIVIDE", g.math("ADD", y, g.math("SUBTRACT", cols - 1, frow)), cols)
    return g.combine(u, v)


def dist_value(g, dist_img):
    geo = g.node("ShaderNodeNewGeometry")
    uv = g.vmath("MULTIPLY", geo.outputs["Position"], (1.0 / FIELD[0], 1.0 / FIELD[1], 0.0))
    n = g.node("ShaderNodeTexImage")
    n.image = dist_img
    n.extension = "EXTEND"
    g.link(uv, n.inputs[0])
    return g.sep_rgb(n.outputs["Color"])[0]


def new_liquid_material(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    try:
        mat.surface_render_method = "BLENDED"
    except AttributeError:
        pass
    g = Graph(mat)
    bsdf = g.nodes.get("Principled BSDF")
    return mat, g, bsdf


# ------------------------------------------------------------------------------------------------------ shaders

def shader_water(g, bsdf, tex, dist, pal):
    d = lambda name: tex(name)  # noqa: E731
    tile = TILE_WORLD["Water"]
    swell = g.tex(d("Water_Normal_Swell"), world_uv(g, tile * 1.6, velocity=(0.035, 0.012)))
    rip = g.tex(d("Water_Normal_Ripple"), world_uv(g, tile * 0.55, velocity=(-0.05, 0.07), rotation=0.7))
    n = g.vmath("NORMALIZE", g.vmath("ADD", g.normal_map(swell.outputs["Color"], 1.0),
                                     g.normal_map(rip.outputs["Color"], 0.7)))
    depth01 = g.smooth(dist, 0.0, 0.85)
    col = g.mixc(depth01, pal["shallow"], pal["deep"])
    # Caustics live on the floor: strongest in the shallows, gone in the deep middle.
    cau = g.tex(d("Water_Caustics_Flip"), flip_uv(g, world_uv(g, tile * 0.7, velocity=(0.01, 0.0))))
    cau_v = g.math("MULTIPLY", g.sep_rgb(cau.outputs["Color"])[0], g.math("SUBTRACT", 1.0, depth01))
    foam_t = g.tex(d("Water_Foam"), world_uv(g, tile * 0.6, velocity=(0.01, 0.006)))
    fr = g.sep_rgb(foam_t.outputs["Color"])
    # Shore band: a crest profile running towards the bank (phase falls with time), fading out by ~0.7 cells.
    t = g.val("time")
    # The dissolve noise (B) bends the phase so crest lines break up instead of copying the bank outline.
    phase = g.math("FRACT", g.math("ADD", g.math("SUBTRACT", g.math("MULTIPLY", dist, 2.2), g.math("MULTIPLY", t, 0.28)),
                                   g.math("MULTIPLY", fr[2], 0.45)))
    prof = g.tex(d("Water_ShoreProfile"), g.combine(phase, 0.5), extension="EXTEND")
    band = g.math("MULTIPLY", g.sep_rgb(prof.outputs["Color"])[0], g.smooth(dist, 0.75, 0.05))
    fringe = g.smooth(dist, 0.1, 0.0)
    cover = g.math("MAXIMUM", g.math("MULTIPLY", band, 0.85), fringe)
    foam = g.math("MULTIPLY", g.math("SUBTRACT", g.math("ADD", fr[0], cover), 1.0), 3.0, clamp=True)
    foam = g.math("MAXIMUM", foam, g.math("MULTIPLY", fr[1], g.math("MULTIPLY", cover, 0.9)))
    g.link(g.mixc(foam, col, pal["foam"]), bsdf.inputs["Base Color"])
    g.link(g.mixf(foam, 0.03, 0.55), bsdf.inputs["Roughness"])
    g.link(n, bsdf.inputs["Normal"])
    g.link(g.mixf(depth01, 0.72, 0.96), bsdf.inputs["Alpha"])
    emis = g.colmul(pal["caustic"], g.combine(g.math("MULTIPLY", cau_v, 0.22), g.math("MULTIPLY", cau_v, 0.22),
                                               g.math("MULTIPLY", cau_v, 0.22)))
    g.link(emis, bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.0
    bsdf.inputs["Specular IOR Level"].default_value = 0.6


def shader_lava(g, bsdf, tex, dist, pal):
    tile = TILE_WORLD["Lava"]
    flow = g.tex(tex("Lava_Flow"), world_uv(g, tile * 1.5, velocity=(0.01, 0.004)))
    off = g.vmath("SCALE", g.vmath("SUBTRACT", flow.outputs["Color"], (0.5, 0.5, 0.0)), 0.06)
    uva = g.vmath("ADD", world_uv(g, tile, velocity=(0.018, 0.006)), off)
    uvb = g.vmath("ADD", world_uv(g, tile * 0.8, velocity=(-0.012, 0.014), rotation=1.1), off)
    a_alb = g.tex(tex("Lava_CrustA_Albedo"), uva, srgb=True)
    b_alb = g.tex(tex("Lava_CrustB_Albedo"), uvb, srgb=True)
    a_h = g.sep_rgb(g.tex(tex("Lava_CrustA_Heat"), uva).outputs["Color"])
    b_h = g.sep_rgb(g.tex(tex("Lava_CrustB_Heat"), uvb).outputs["Color"])
    a_n = g.tex(tex("Lava_CrustA_Normal"), uva)
    b_n = g.tex(tex("Lava_CrustB_Normal"), uvb)
    under = g.sep_rgb(g.tex(tex("Lava_CrustA_Heat"), world_uv(g, tile * 0.6, velocity=(0.04, -0.02))).outputs["Color"])[2]
    # Which layer's plates are on top: a slow noise that drifts, so plates break up and re-form (layer swap).
    noise = g.node("ShaderNodeTexNoise", noise_dimensions="4D")
    geo = g.node("ShaderNodeNewGeometry")
    g.link(geo.outputs["Position"], noise.inputs["Vector"])
    noise.inputs["Scale"].default_value = 0.45
    g.link(g.math("MULTIPLY", g.val("time"), 0.05), noise.inputs["W"])
    w = g.smooth(noise.outputs["Fac"], 0.42, 0.58)
    alb = g.mixc(w, a_alb.outputs["Color"], b_alb.outputs["Color"])
    seam = g.mixf(w, a_h[0], b_h[0])
    halo = g.mixf(w, a_h[1], b_h[1])
    # Hotter towards the middle of the pool, cooler crust against the bank.
    centre = g.smooth(dist, 0.1, 1.4)
    heat = g.math("ADD", g.math("MULTIPLY", seam, g.math("ADD", 0.55, g.math("MULTIPLY", under, 0.6))),
                  g.math("MULTIPLY", halo, 0.1))
    heat = g.math("MULTIPLY", heat, g.math("ADD", 0.55, g.math("MULTIPLY", centre, 0.6)), clamp=True)
    # Open molten patches in the hottest middle, where the underflow pattern peaks (crust has not formed).
    molten = g.math("MULTIPLY", g.smooth(under, 0.7, 0.95), g.smooth(dist, 0.6, 1.2))
    heat = g.math("MAXIMUM", heat, g.math("MULTIPLY", molten, 0.75))
    alb = g.mixc(molten, alb, (0.3, 0.06, 0.02))
    emis = g.ramp(heat, pal["ramp"])
    g.link(alb, bsdf.inputs["Base Color"])
    g.link(g.mixf(seam, 0.75, 0.35), bsdf.inputs["Roughness"])
    g.link(g.mixv(w, g.normal_map(a_n.outputs["Color"]), g.normal_map(b_n.outputs["Color"])), bsdf.inputs["Normal"])
    g.link(emis, bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.6
    bsdf.inputs["Alpha"].default_value = 1.0


def shader_bog(g, bsdf, tex, dist, pal):
    tile = TILE_WORLD["Bog"]
    nrm = g.tex(tex("Bog_Normal"), world_uv(g, tile * 1.3, velocity=(0.008, 0.004)))
    nrm2 = g.tex(tex("Bog_Normal"), world_uv(g, tile * 0.7, velocity=(-0.006, 0.009), rotation=1.3))
    scum = g.tex(tex("Bog_Scum"), world_uv(g, tile, velocity=(0.004, 0.0015)))
    sc = g.sep_rgb(scum.outputs["Color"])
    a = scum.outputs["Alpha"]
    # Scum gathers along the bank (wind and still water): boost coverage near the shore.
    a = g.math("MINIMUM", g.math("ADD", a, g.math("MULTIPLY", g.smooth(dist, 0.5, 0.0), 0.35)), 1.0)
    a = g.math("MULTIPLY", a, g.smooth(dist, 0.0, 0.06))
    depth01 = g.smooth(dist, 0.0, 1.3)
    water = g.mixc(depth01, pal["shallow"], pal["deep"])
    scum_col = g.colmul(pal["scum"], g.combine(sc[0], sc[0], sc[0]))
    g.link(g.mixc(a, water, scum_col), bsdf.inputs["Base Color"])
    g.link(g.mixf(a, 0.12, g.mixf(sc[2], 0.75, 0.35)), bsdf.inputs["Roughness"])
    n = g.vmath("NORMALIZE", g.vmath("ADD", g.normal_map(nrm.outputs["Color"]), g.normal_map(nrm2.outputs["Color"], 0.6)))
    g.link(n, bsdf.inputs["Normal"])
    g.link(g.mixf(depth01, 0.85, 0.97), bsdf.inputs["Alpha"])
    glow = g.math("MULTIPLY", sc[1], a)
    g.link(g.colmul(pal["glow"], g.combine(glow, glow, glow)), bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.4


def shader_quicksand(g, bsdf, tex, dist, pal):
    tile = TILE_WORLD["Quicksand"]
    base = world_uv(g, tile)
    flow = g.tex(tex("Quicksand_Flow"), base)
    fv = g.vmath("SUBTRACT", flow.outputs["Color"], (0.5, 0.5, 0.0))
    fv = g.vmath("MULTIPLY", fv, (0.1, 0.1, 0.0))
    # Classic two-phase flow map: two samples pushed along the flow, each reset when its weight is zero.
    p0 = g.value("phase0")
    p1 = g.value("phase1")
    wgt = g.value("wgt")
    uv0 = g.vmath("ADD", base, g.vmath("SCALE", fv, p0))
    uv1 = g.vmath("ADD", g.vmath("ADD", base, (0.37, 0.21, 0.0)), g.vmath("SCALE", fv, p1))
    a0, a1 = g.tex(tex("Quicksand_Albedo"), uv0, srgb=True), g.tex(tex("Quicksand_Albedo"), uv1, srgb=True)
    n0, n1 = g.tex(tex("Quicksand_Normal"), uv0), g.tex(tex("Quicksand_Normal"), uv1)
    alb = g.mixc(wgt, a0.outputs["Color"], a1.outputs["Color"])
    wet = g.mixf(wgt, a0.outputs["Alpha"], a1.outputs["Alpha"])
    sp = g.sep_rgb(g.tex(tex("Quicksand_Sparkle"), base).outputs["Color"])[0]
    # Darker, damper sand towards the sinking middle.
    damp = g.smooth(dist, 0.2, 1.6)
    col = g.colmul(alb, pal["sand"])
    col = g.mixc(g.math("MULTIPLY", damp, 0.35), col, g.colmul(col, (0.55, 0.55, 0.6)))
    g.link(col, bsdf.inputs["Base Color"])
    g.link(g.mixf(g.math("MAXIMUM", wet, g.math("MULTIPLY", damp, 0.3)), 0.85, 0.35), bsdf.inputs["Roughness"])
    g.link(g.mixv(wgt, g.normal_map(n0.outputs["Color"]), g.normal_map(n1.outputs["Color"])), bsdf.inputs["Normal"])
    g.link(g.colmul(pal["sparkle"], g.combine(sp, sp, sp)), bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 0.8
    bsdf.inputs["Alpha"].default_value = 1.0


def shader_ice(g, bsdf, tex, dist, pal):
    tile = TILE_WORLD["Ice"]
    uv = world_uv(g, tile)
    alb = g.tex(tex("Ice_Albedo"), uv, srgb=True)
    cr = g.sep_rgb(g.tex(tex("Ice_Cracks"), uv).outputs["Color"])
    nrm = g.tex(tex("Ice_Normal"), uv)
    frost_t = g.sep_rgb(g.tex(tex("Ice_Frost"), world_uv(g, tile * 0.5)).outputs["Color"])
    milky = alb.outputs["Alpha"]
    depth01 = g.smooth(dist, 0.0, 1.5)
    deep = g.mixc(depth01, g.colmul(pal["deep"], (2.2, 2.2, 2.0)), pal["deep"])
    ice_col = g.colmul(alb.outputs["Color"], pal["ice"])
    col = g.mixc(g.math("ADD", g.math("MULTIPLY", milky, 0.85), 0.1), deep, ice_col)
    # Frost creeps from the bank: the frost density must beat a threshold that rises with distance.
    thr = g.math("MULTIPLY", dist, 2.6)
    frost = g.smooth(g.math("SUBTRACT", frost_t[0], thr), -0.05, 0.12)
    col = g.mixc(frost, col, pal["frost"])
    g.link(col, bsdf.inputs["Base Color"])
    g.link(g.mixf(frost, g.mixf(milky, 0.04, 0.25), 0.7), bsdf.inputs["Roughness"])
    g.link(g.normal_map(nrm.outputs["Color"]), bsdf.inputs["Normal"])
    glint = g.math("ADD", g.math("MULTIPLY", cr[0], 0.35), g.math("MULTIPLY", frost_t[1], g.math("MULTIPLY", frost, 0.3)))
    g.link(g.colmul(pal["crack_glow"], g.combine(glint, glint, glint)), bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 0.9
    bsdf.inputs["Alpha"].default_value = 1.0


def shader_corruption(g, bsdf, tex, dist, pal):
    tile = TILE_WORLD["Corruption"]
    uv = world_uv(g, tile, velocity=(0.006, 0.003))
    nrm = g.tex(tex("Corruption_Normal"), uv)
    nrm2 = g.tex(tex("Corruption_Normal"), world_uv(g, tile * 0.6, velocity=(-0.01, 0.006), rotation=2.0))
    v = g.sep_rgb(g.tex(tex("Corruption_Veins"), uv).outputs["Color"])
    depth01 = g.smooth(dist, 0.0, 1.4)
    base = g.mixc(depth01, pal["base"], pal["deep"])
    base = g.mixc(g.math("MULTIPLY", v[2], 0.5), base, g.colmul(base, (0.4, 0.4, 0.4)))
    t = g.val("time")
    pulse = g.math("ADD", 0.35, g.math("MULTIPLY", 0.65, g.math("ADD", 0.5, g.math(
        "MULTIPLY", 0.5, g.math("SINE", g.math("MULTIPLY", g.math("SUBTRACT", g.math("MULTIPLY", v[1], 3.0),
                                                                     g.math("MULTIPLY", t, 0.35)), 6.2832))))))
    glow = g.math("MULTIPLY", v[0], pulse)
    glow = g.math("MULTIPLY", glow, g.smooth(dist, 0.0, 0.25))
    g.link(base, bsdf.inputs["Base Color"])
    g.link(0.18, bsdf.inputs["Roughness"])
    n = g.vmath("NORMALIZE", g.vmath("ADD", g.normal_map(nrm.outputs["Color"]), g.normal_map(nrm2.outputs["Color"], 0.5)))
    g.link(n, bsdf.inputs["Normal"])
    g.link(g.colmul(pal["vein"], g.combine(glow, glow, glow)), bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.0
    bsdf.inputs["Alpha"].default_value = 1.0


SHADERS = {"Water": shader_water, "Lava": shader_lava, "Bog": shader_bog, "Quicksand": shader_quicksand,
           "Ice": shader_ice, "Corruption": shader_corruption}


def preview_palette(kind):
    import build_liquids as B
    p = B.PALETTES[kind]
    first = next(iter(p.values()))
    if kind == "Lava":
        return {"ramp": [(s[0], tuple(s[1])) for s in first["ramp"]]}
    return {k: tuple(v) for k, v in first.items()}


# --------------------------------------------------------------------------------------------------------- scene

def tiling_material(name, folder, set_name, tile=3.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    g = Graph(mat)
    bsdf = g.nodes.get("Principled BSDF")
    uv = world_uv(g, tile)
    alb = g.tex(os.path.join(folder, set_name + "_Albedo.png"), uv, srgb=True)
    nrm = g.tex(os.path.join(folder, set_name + "_Normal.png"), uv)
    g.link(alb.outputs["Color"], bsdf.inputs["Base Color"])
    g.link(g.normal_map(nrm.outputs["Color"]), bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.8
    em = os.path.join(folder, set_name + "_Emission.png")
    if os.path.exists(em):
        e = g.tex(em, uv, srgb=True)
        g.link(e.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.0
    return mat


def terrain(dist, mat):
    """Ground grid at z = 0 with the pool basin dug in (the bank pieces hide the cut)."""
    import bmesh
    res = 12
    nx, ny = FIELD[0] * res, FIELD[1] * res
    bm = bmesh.new()
    h, w = dist.shape
    verts = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x, y = i / res, j / res
            px = min(int(x * PX_PER_CELL), w - 1)
            py = min(int((FIELD[1] - y) * PX_PER_CELL), h - 1)
            d = dist[py, px]
            # A steep wall just inside the shore (below every liquid level), then the floor deepens.
            z = -(0.3 + (BASIN_DEPTH - 0.3) * (1 - math.exp(-d * 2.2))) if d > 0 else 0.0
            row.append(bm.verts.new((x, y, z)))
        verts.append(row)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
    mesh = bpy.data.meshes.new("Terrain")
    bm.to_mesh(mesh)
    for p in mesh.polygons:
        p.use_smooth = True
    obj = C.link(bpy.data.objects.new("Terrain", mesh))
    mesh.materials.append(mat)
    return obj


_SRC = {}


def bank_source(folder, name):
    key = (folder, name)
    if key not in _SRC:
        d = os.path.join(ENV, folder, name) if folder else os.path.join(ENV, name)
        objs = E.textured_import(d, name, emission_strength=1.2)
        mesh = max((o for o in objs if o.type == "MESH"), key=lambda o: len(o.data.vertices))
        for o in objs:
            o.hide_render = True
            o.hide_viewport = True
        _SRC[key] = mesh
    return _SRC[key]


def place(src, location, rot_deg):
    obj = C.link(bpy.data.objects.new(src.name + "_inst", src.data))
    obj.matrix_world = src.matrix_world.copy()
    obj.location = Vector((location[0], location[1], 0.0))
    e = src.rotation_euler.copy()
    e.z += math.radians(rot_deg)
    obj.rotation_euler = e
    obj.hide_render = False
    return obj


def place_banks(cells, folder, prefix):
    straight = bank_source(folder, prefix + "Bank_Straight")
    outer = bank_source(folder, prefix + "Bank_OuterCorner")
    inner = bank_source(folder, prefix + "Bank_InnerCorner")
    for (i, j) in cells:
        for d in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if (i + d[0], j + d[1]) in cells:
                continue
            p = (i + 0.5 + d[0] * 0.5, j + 0.5 + d[1] * 0.5)
            place(straight, p, math.degrees(math.atan2(-d[0], d[1])))
    xs = [c[0] for c in cells]
    ys = [c[1] for c in cells]
    for gx in range(min(xs), max(xs) + 2):
        for gy in range(min(ys), max(ys) + 2):
            quads = {(qx, qy): (gx + (0 if qx > 0 else -1), gy + (0 if qy > 0 else -1)) in cells
                     for qx in (-1, 1) for qy in (-1, 1)}
            wet = [q for q, v in quads.items() if v]
            if len(wet) == 1:
                q = wet[0]
                place(outer, (gx, gy), math.degrees(math.atan2(q[1], q[0]) - math.atan2(-1, -1)))
            elif len(wet) == 3:
                q = [q for q, v in quads.items() if not v][0]
                place(inner, (gx, gy), math.degrees(math.atan2(q[1], q[0]) - math.atan2(1, 1)))


def liquid_plane(mat, level):
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    mesh = bpy.data.meshes.new("Liquid")
    bm.to_mesh(mesh)
    obj = C.link(bpy.data.objects.new("Liquid", mesh))
    obj.scale = (FIELD[0], FIELD[1], 1)
    obj.location = (FIELD[0] / 2, FIELD[1] / 2, level)
    bpy.context.view_layer.update()
    # Bake the scale so the UV tangents (normal maps) are those of world X/Y.
    mesh.transform(obj.matrix_basis)
    obj.matrix_basis.identity()
    mesh.uv_layers.new(name="UVMap")
    mesh.materials.append(mat)
    return obj


def world_setup(tint):
    world = C.preview_world(tuple(c * 1.0 for c in tint), 1.0)
    nt = world.node_tree
    bg = nt.nodes.get("Background")
    # A soft vertical gradient so reflections have something to show (dark below, a lit sky above).
    grad = nt.nodes.new("ShaderNodeTexGradient")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    nt.links.new(coord.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs[2], ramp.inputs[0])
    ramp.color_ramp.elements[0].position = 0.45
    ramp.color_ramp.elements[0].color = (*tint, 1)
    ramp.color_ramp.elements[1].position = 0.9
    ramp.color_ramp.elements[1].color = (tint[0] * 5 + 0.08, tint[1] * 5 + 0.1, tint[2] * 5 + 0.14, 1)
    nt.links.new(ramp.outputs[0], bg.inputs["Color"])
    nt.nodes.remove(grad)


def set_time(mats, t, fps=8):
    frame = int(t * fps) % 16
    for g in mats:
        if "time" in g.values:
            g.values["time"].outputs[0].default_value = t
        if "fcol" in g.values:
            g.values["fcol"].outputs[0].default_value = frame % 4
            g.values["frow"].outputs[0].default_value = frame // 4
        if "phase0" in g.values:
            speed = 0.35
            p0 = (t * speed) % 1.0
            p1 = (t * speed + 0.5) % 1.0
            g.values["phase0"].outputs[0].default_value = p0
            g.values["phase1"].outputs[0].default_value = p1
            g.values["wgt"].outputs[0].default_value = abs(1.0 - 2.0 * p0)


def build_scene(kind, out_dir):
    C.reset_scene()
    _SRC.clear()
    folder, prefix, gfolder, gname, _, tint = SETS[kind]
    cells = pool_cells()
    dist = distance_map(cells)
    dist_img = bpy.data.images.new("Dist", dist.shape[1], dist.shape[0], float_buffer=True)
    rgba = np.ones((dist.shape[0], dist.shape[1], 4), F32)
    rgba[..., 0] = rgba[..., 1] = rgba[..., 2] = dist
    # Colour space first: changing it afterwards regenerates the (generated) image and drops the pixels.
    dist_img.colorspace_settings.name = "Non-Color"
    dist_img.pixels.foreach_set(rgba[::-1].ravel())
    dist_img.pack()
    terrain(dist, tiling_material("Ground", gfolder, gname))
    place_banks(cells, folder, prefix)
    mat, g, bsdf = new_liquid_material("Liquid_" + kind)
    g.value("time")
    tex = lambda n: os.path.join(out_dir, kind, n + ".png")  # noqa: E731
    SHADERS[kind](g, bsdf, tex, dist_value(g, dist_img), preview_palette(kind))
    liquid_plane(mat, LEVEL[kind])
    world_setup(tint)
    C.game_lights(2.2, 0.8)
    return [g], cells


def find_shot(compare_dir, name):
    """The Unity screenshot `name` in any of the ';'-separated compare directories."""
    for d in (compare_dir or "").split(";"):
        p = os.path.join(d, name)
        if d and os.path.exists(p):
            return p
    return None


def compare(kind, render_path, compare_dir, out_path):
    shot = find_shot(compare_dir, SETS[kind][4])
    if not shot:
        return None
    C.contact_sheet([shot, render_path], out_path, 2, 960)
    return out_path


def render_kind(kind, out_dir, prev, samples, compare_dir):
    mats, cells = build_scene(kind, out_dir)
    target = Vector((5.5, 4.5, 0.0))
    cam = C.game_camera(target, 7.5)
    set_time(mats, 1.0)
    game = C.render(cam, os.path.join(prev, f"liq_{kind}_game.png"), 1280, 720, samples=samples)
    close = C.game_camera(Vector((6.3, 4.2, 0.0)), 3.2)
    C.render(close, os.path.join(prev, f"liq_{kind}_close.png"), 1280, 720, samples=samples)
    strip = []
    for k in range(4):
        set_time(mats, 1.0 + k * 0.8)
        strip.append(C.render(close, os.path.join(prev, f"_strip_{kind}_{k}.png"), 640, 360, samples=max(8, samples // 2)))
    C.contact_sheet(strip, os.path.join(prev, f"liq_{kind}_strip.png"), 4, 640)
    for p in strip:
        os.remove(p)
    compare(kind, game, compare_dir, os.path.join(prev, f"liq_{kind}_compare.png"))


# ---------------------------------------------------------------------------------------------------------- mist

def mist_material(g_name, path, color, alpha, velocity, tile, mats):
    mat = bpy.data.materials.new(g_name)
    mat.use_nodes = True
    try:
        mat.surface_render_method = "BLENDED"
    except AttributeError:
        pass
    g = Graph(mat)
    g.value("time")
    out = g.nodes.get("Material Output")
    for n in list(g.nodes):
        if n.type == "BSDF_PRINCIPLED":
            g.nodes.remove(n)
    m = g.sep_rgb(g.tex(path, world_uv(g, tile, velocity=velocity)).outputs["Color"])[0]
    em = g.node("ShaderNodeEmission")
    g.link(color, em.inputs["Color"])
    em.inputs["Strength"].default_value = 1.0
    tr = g.node("ShaderNodeBsdfTransparent")
    mix = g.node("ShaderNodeMixShader")
    g.link(g.math("MULTIPLY", m, alpha), mix.inputs[0])
    g.link(tr.outputs[0], mix.inputs[1])
    g.link(em.outputs[0], mix.inputs[2])
    g.link(mix.outputs[0], out.inputs["Surface"])
    mats.append(g)
    return mat


def mist_plane(mat, z, size):
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size / 2)
    mesh = bpy.data.meshes.new("Mist")
    bm.to_mesh(mesh)
    obj = C.link(bpy.data.objects.new("Mist", mesh))
    obj.location = (FIELD[0] / 2, FIELD[1] / 2, z)
    mesh.materials.append(mat)
    obj.visible_shadow = False
    return obj


def render_mist(out_dir, prev, samples, compare_dir):
    C.reset_scene()
    cells = pool_cells()
    dist = distance_map(cells)
    terrain(np.zeros_like(dist), tiling_material("Ground", os.path.join(ENV, "Tiling", "Ground"), "Ground"))
    road = tiling_material("Road", os.path.join(ENV, "Tiling", "Road"), "Road")
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
    mesh = bpy.data.meshes.new("Road")
    bm.to_mesh(mesh)
    r = C.link(bpy.data.objects.new("Road", mesh))
    r.scale = (2.0, FIELD[1] + 2, 1)
    r.location = (4.0, FIELD[1] / 2, 0.01)
    r.rotation_euler.z = 0.0
    mesh.materials.append(road)
    world_setup((0.05, 0.06, 0.09))
    C.game_lights(3.0, 1.0)
    target = Vector((5.5, 4.5, 0.0))
    cam = C.game_camera(target, 9.0)
    plain = C.render(cam, os.path.join(prev, "liq_Mist_none.png"), 1280, 720, samples=samples)
    mats = []
    col = (0.55, 0.6, 0.75)
    d = os.path.join(out_dir, "Mist")
    mist_plane(mist_material("MistA", os.path.join(d, "Mist_Layer_A.png"), col, 0.22, (0.012, 0.003), 7.0, mats), 0.12, 30)
    mist_plane(mist_material("MistB", os.path.join(d, "Mist_Layer_B.png"), col, 0.14, (0.025, 0.004), 5.0, mats), 0.32, 30)
    mist_plane(mist_material("MistC", os.path.join(d, "Mist_Layer_C.png"), col, 0.09, (0.04, -0.01), 3.5, mats), 0.6, 30)
    set_time(mats, 2.0)
    with_mist = C.render(cam, os.path.join(prev, "liq_Mist_game.png"), 1280, 720, samples=samples)
    strip = []
    for k in range(4):
        set_time(mats, 2.0 + k * 3.0)
        strip.append(C.render(cam, os.path.join(prev, f"_strip_mist_{k}.png"), 640, 360, samples=max(8, samples // 2)))
    C.contact_sheet(strip, os.path.join(prev, "liq_Mist_strip.png"), 4, 640)
    for p in strip:
        os.remove(p)
    C.contact_sheet([plain, with_mist], os.path.join(prev, "liq_Mist_compare.png"), 2, 960)
    shot = find_shot(compare_dir, "final_L5_bank.png")
    if shot:
        C.contact_sheet([shot, with_mist], os.path.join(prev, "liq_Mist_vs_unity.png"), 2, 960)


def run(kinds, out_dir, prev, samples, compare_dir):
    prev = prev or os.path.join(C.REPO, "ArtSource", "Previews", "Liquids")
    os.makedirs(prev, exist_ok=True)
    for kind in kinds:
        if kind == "Mist":
            render_mist(out_dir, prev, samples, compare_dir)
        else:
            render_kind(kind, out_dir, prev, samples, compare_dir)
        C.log("liquid preview done", kind)
