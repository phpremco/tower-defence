"""Deepwake logo, round 4 (2026-11-10): Remco picked v3's CrysiumField lockup; this round fixes its issues.

- Lava: the Gravemaw_Fissure atlas keeps its rock lips, stepped walls and the heat flush on the lower walls (the lit
  rim), but the molten channel (atlas V < 0.105, see build_fissure34.py) gets a dedicated lava shader instead of the
  crust-cell texture, which read as stacked plates from this low camera. The shader works in world space along the
  crack: drifting cooled crust plates (warped voronoi, stretched along the flow) on a hot viscous surface, white-yellow
  in the middle of the channel through orange to deep red at the cooler edges, glowing cracks between the plates and a
  slight bulge. Above it: a thin warm heat haze following the crack and embers rising from it. The lava lights now
  reach the fissure too (its channel is emission-dominated, so they only light the walls and lips).
- Small elements a touch more discoverable: colony lights, drone and beam, Skitters (with a warm kiss of light), eyes.
- Letters: reflection cards (ember below, Crysium pink at the sides, glossy only) so even the alpha library logo
  keeps the scene's colour in its faces instead of plain silver.
- Outputs to ArtSource/Previews/Logo/v4/: the CrysiumField set (header, capsule, library logo, emblem 512/256) and a
  version without the series line (header, capsule), plus Deepwake_v4_contact_sheet.png.

Run: blender -b --factory-startup --python Tools/Blender/logo_v4.py -- --variant field|field_ns
         [--only header,capsule,library,emblem] [--samples 128] [--half|--draft] [--lavacrop]
     blender -b --factory-startup --python Tools/Blender/logo_v4.py -- --sheet
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bmesh  # noqa: E402
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import logo_v3 as V  # noqa: E402

L2 = V.L2
OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Logo", "v4")
V.OUT = OUT
V.CACHE = os.path.join(C.REPO, "ArtSource", "Previews", "Logo", "v3", "_cache")

LAVA_HOT = (1.0, 0.66, 0.26)     # white-yellow core
LAVA_MID = (1.0, 0.32, 0.04)     # orange
LAVA_COOL = (0.45, 0.025, 0.004)  # deep red at the edges
CRUST = (0.035, 0.022, 0.02)


# =============================================================================================================
# Lava
# =============================================================================================================

def lava_nodes(nt, edge):
    """Lava surface in world space. `edge` 0 at the channel's centre line, 1 at its rim. Returns (base colour,
    roughness, emission colour, emission strength, height)."""
    pos = L2.o(L2.N(nt, "ShaderNodeNewGeometry"), "Position")
    # Stretched along the flow (the crack runs along +Y) and warped, so plates are rafts, not cells in rows.
    flow = L2.o(L2.N(nt, "ShaderNodeMapping", {"Vector": pos, "Scale": (1.0, 0.24, 1.0)}))
    warp = L2.noise(nt, flow, 0.9, 4.0, 0.55)
    wv = L2.N(nt, "ShaderNodeMix", data_type="VECTOR")
    wv.inputs[0].default_value = 0.55
    nt.links.new(flow, L2.sock(wv.inputs, "A"))
    nt.links.new(L2.o(warp, "Color"), L2.sock(wv.inputs, "B"))
    wpos = L2.o(wv, "Result")
    vor = L2.N(nt, "ShaderNodeTexVoronoi", {"Vector": wpos, "Scale": 3.2, "Randomness": 0.9},
               feature="DISTANCE_TO_EDGE")
    cell = L2.N(nt, "ShaderNodeTexVoronoi", {"Vector": wpos, "Scale": 3.2, "Randomness": 0.9}, feature="F1")
    d_edge = L2.o(vor, "Distance")
    # Per-plate: some plates have melted (none there), the rest float; more crust towards the cooler rim.
    plate_id = L2.o(cell, "Color")
    pid = L2.o(L2.N(nt, "ShaderNodeSeparateColor", {"Color": plate_id}), "Red")
    keep = L2.M(nt, "LESS_THAN", pid, L2.MR(nt, edge, 0.0, 1.0, 0.32, 0.85, smooth=False))
    plate_core = L2.MR(nt, d_edge, 0.05, 0.11, 0.0, 1.0)          # 0 in the crack, 1 inside the plate
    crust = L2.M(nt, "MULTIPLY", plate_core, keep)
    # Fine breakup of the plates' surface and the glowing hairline cracks inside them.
    fine = L2.noise(nt, pos, 9.0, 6.0, 0.6)
    hair = L2.crack_lines(nt, wpos, 7.0, 0.02, warp=0.3)
    # Heat: hottest down the middle of the channel, a slow flowing variation on top.
    heat_n = L2.MR(nt, L2.o(L2.noise(nt, wpos, 0.7, 3.0, 0.5), "Factor"), 0.3, 0.7, -0.18, 0.18, smooth=False)
    # Viscous flow streaks run along the crack (never across it), so the melt reads as moving, not as bands.
    streak = L2.noise(nt, pos, 2.0, 5.0, 0.6, stretch=(2.6, 0.6, 2.6))
    flow_n = L2.MR(nt, L2.o(streak, "Factor"), 0.35, 0.65, -0.2, 0.2, smooth=False)
    core_line = L2.M(nt, "SUBTRACT", 1.0, L2.M(nt, "MULTIPLY", L2.M(nt, "POWER", edge, 0.65), 0.95))
    heat = L2.M(nt, "ADD", L2.M(nt, "ADD", core_line, heat_n), flow_n, clamp=True)
    ramp = L2.N(nt, "ShaderNodeValToRGB")
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = 0.0, (*LAVA_COOL, 1)
    els[1].position, els[1].color = 0.55, (*LAVA_MID, 1)
    e3 = els.new(0.84)
    e3.color = (*LAVA_HOT, 1)
    nt.links.new(heat, ramp.inputs[0])
    hot_col = ramp.outputs["Color"]
    hot_s = L2.MR(nt, heat, 0.0, 1.0, 1.6, 6.5)
    # Crust plates glow a dull red only where they are thin (their edges) and along hairline cracks.
    rim_glow = L2.M(nt, "MULTIPLY", L2.MR(nt, d_edge, 0.03, 0.12, 1.0, 0.0), 1.0)
    plate_glow = L2.M(nt, "ADD", rim_glow, L2.M(nt, "MULTIPLY", hair, 0.9))
    em_s = L2.M(nt, "ADD", L2.M(nt, "MULTIPLY", L2.M(nt, "SUBTRACT", 1.0, crust), hot_s),
                L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", crust, plate_glow), L2.M(nt, "MULTIPLY", hot_s, 0.6)))
    em_col = L2.MIXC(nt, crust, hot_col, L2.MIXC(nt, 0.5, LAVA_COOL, LAVA_MID))
    base = L2.MIXC(nt, crust, (0.25, 0.04, 0.01), L2.MIXC(nt, L2.o(fine, "Factor"), CRUST, (0.08, 0.06, 0.055)))
    rough = L2.MR(nt, crust, 0.0, 1.0, 0.22, 0.85, smooth=False)
    # The plates ride a little high; the melt between them bulges gently.
    swell = L2.MR(nt, L2.o(L2.noise(nt, wpos, 1.6, 2.0, 0.5), "Factor"), 0.3, 0.7, 0.0, 0.4)
    height = L2.M(nt, "ADD", L2.M(nt, "ADD", L2.M(nt, "MULTIPLY", crust, 1.0), swell),
                  L2.M(nt, "MULTIPLY", L2.o(fine, "Factor"), L2.M(nt, "MULTIPLY", crust, 0.4)))
    return base, rough, em_col, em_s, height


def patch_fissure_lava():
    """Split the fissure material by the atlas V: the channel (V < 0.105) becomes lava, the rest keeps the game map."""
    mat = bpy.data.materials.get("Gravemaw_Fissure")
    nt = mat.node_tree
    out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
    rock_bsdf = out.inputs["Surface"].links[0].from_socket
    uv = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "UV")
    _, v, _ = L2.SEP(nt, uv)
    core = L2.MR(nt, v, 0.095, 0.112, 1.0, 0.0)
    edge = L2.MR(nt, v, 0.0, 0.105, 0.0, 1.0, smooth=False)
    base, rough, em_col, em_s, height = lava_nodes(nt, edge)
    nrm = L2.bump(nt, height, 0.35, 0.03)
    lava = L2.N(nt, "ShaderNodeBsdfPrincipled")
    for name, val in (("Base Color", base), ("Roughness", rough), ("Emission Color", em_col),
                      ("Emission Strength", em_s), ("Normal", nrm)):
        L2.setv(nt, lava, name, val)
    L2.setv(nt, lava, "Specular IOR Level", 0.6)
    mix = L2.N(nt, "ShaderNodeMixShader")
    nt.links.new(core, mix.inputs[0])
    nt.links.new(rock_bsdf, mix.inputs[1])
    nt.links.new(L2.o(lava, "BSDF"), mix.inputs[2])
    nt.links.new(mix.outputs[0], out.inputs["Surface"])


_orig_chain = V.fissure_chain


def fissure_chain(*a, **k):
    res = _orig_chain(*a, **k)
    patch_fissure_lava()
    return res


V.fissure_chain = fissure_chain


def lava_lights(path, collection, strength, every=3):
    """Light from the melt, now on the fissure as well: its channel is emission, the lamps light the walls and lips."""
    for k, (x, y) in enumerate(path[::every]):
        L2.point(f"Lava{k}", (x, y, V.FLOOR - 0.4), V.EMBER, strength * (1 + y * 0.03), 0.4, collection)


V.lava_lights = lava_lights


def mat_heat(name, density):
    """Warm, thin, low-lying haze over the crack: the heat in the air, glowing from below."""
    mat, nt, out = L2.new_mat(name)
    tc = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "Generated")
    _, _, z = L2.SEP(nt, tc)
    wisps = L2.MR(nt, L2.o(L2.noise(nt, L2.o(L2.N(nt, "ShaderNodeTexCoord"), "Object"), 3.0, 4.0, 0.6), "Factor"),
                  0.35, 0.7, 0.2, 1.0)
    fade = L2.MR(nt, z, 0.0, 1.0, 1.0, 0.0)
    dens = L2.M(nt, "MULTIPLY", L2.M(nt, "MULTIPLY", wisps, fade), density)
    # Emission only: seen along the crack at a grazing angle, any extinction swallowed the lava below it.
    vol = L2.N(nt, "ShaderNodeEmission", {"Color": V.EMBER, "Strength": dens})
    nt.links.new(L2.o(vol), out.inputs["Volume"])
    return mat


def heat_haze(path, fs, col):
    bm = bmesh.new()
    pts = [Vector((x, y, 0)) for x, y in path]
    h = 0.9 * fs
    rows = []
    for i, p in enumerate(pts):
        q = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        n = Vector((-q.y, q.x, 0)).normalized() * (0.9 * fs)
        z0 = V.FLOOR - 0.1
        rows.append([bm.verts.new(p + s * n + Vector((0, 0, z))) for s in (-1, 1) for z in (z0, z0 + h)])
    # Row verts: 0 left-low, 1 left-high, 2 right-low, 3 right-high; a closed tube so the volume is watertight.
    for a, b in zip(rows[:-1], rows[1:]):
        for f in ((a[0], b[0], b[1], a[1]), (a[2], a[3], b[3], b[2]), (a[1], b[1], b[3], a[3]),
                  (a[0], a[2], b[2], b[0])):
            bm.faces.new(f)
    for r in (rows[0], rows[-1]):
        bm.faces.new((r[0], r[1], r[3], r[2]))
    ob = L2.bm_object("HeatHaze", bm, [mat_heat("HeatM", 0.03)])
    # Generated z of the ribbon runs 0..1 from the lips to its top.
    ob.visible_shadow = False
    L2.put(ob, col)


def crack_embers(path, fs, col, count=140, seed=31):
    rng = random.Random(seed)
    bm = bmesh.new()
    for _ in range(count):
        i = rng.randrange(len(path) - 1)
        (ax, ay), (bx, by) = path[i], path[i + 1]
        t = rng.random()
        rise = rng.random() ** 1.8 * 3.2
        p = Vector((ax + (bx - ax) * t + rng.gauss(0, 0.25 * fs) * (1 + rise * 0.4),
                    ay + (by - ay) * t, V.FLOOR - 0.2 + rise))
        r = rng.uniform(0.006, 0.016) * (1 - 0.5 * rise / 3.2)
        res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r)
        for v in res["verts"]:
            v.co = Vector((v.co.x, v.co.y, v.co.z * 3.0)) + p
    ob = L2.bm_object("CrackEmbers", bm, [L2.mat_emit("CrackEmberM", (1.0, 0.36, 0.06), 9.0,
                                                    core=(1.0, 0.7, 0.35))])
    ob.visible_shadow = False
    L2.put(ob, col)


def skitter_kiss(cfg, col):
    for k, (x, y, s, _) in enumerate(cfg.get("skitters", ())):
        L2.point(f"SkitterKiss{k}", (x, y - 1.2, V.FLOOR + 0.8), V.EMBER, 35.0 * s, 0.5, col)


_orig_build_env = V.build_env


def build_env(cfg, col):
    path, fs = _orig_build_env(cfg, col)
    heat_haze(path, fs, col)
    crack_embers(path, fs, col)
    skitter_kiss(cfg, col)
    return path, fs


V.build_env = build_env


# =============================================================================================================
# Letters: the scene's colours in their reflections
# =============================================================================================================

def card(name, col, loc, target, size, colour, strength):
    bpy.ops.mesh.primitive_plane_add(size=1.0)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (size[0], size[1], 1)
    ob.location = loc
    C.look_at(ob, Vector(target))
    # The plane's normal is +Z; look_at points -Z at the target, so flip it to face the letters.
    ob.rotation_euler.rotate_axis("X", math.pi)
    mat, nt, out = L2.new_mat(name + "M")
    tc = L2.o(L2.N(nt, "ShaderNodeTexCoord"), "Generated")
    x, y, _ = L2.SEP(nt, tc)
    soft = L2.M(nt, "MULTIPLY", L2.MR(nt, L2.M(nt, "ABSOLUTE", L2.M(nt, "SUBTRACT", x, 0.5)), 0.2, 0.5, 1.0, 0.0),
                L2.MR(nt, L2.M(nt, "ABSOLUTE", L2.M(nt, "SUBTRACT", y, 0.5)), 0.15, 0.5, 1.0, 0.0))
    em = L2.N(nt, "ShaderNodeEmission", {"Color": colour, "Strength": L2.M(nt, "MULTIPLY", soft, strength)})
    nt.links.new(L2.o(em), out.inputs["Surface"])
    ob.data.materials.append(mat)
    for attr in ("visible_camera", "visible_shadow", "visible_diffuse", "visible_volume_scatter",
                 "visible_transmission"):
        setattr(ob, attr, False)
    L2.put(ob, col)


_orig_studio = V.studio_lights


def studio_lights(col, title_w, receivers, cfg):
    _orig_studio(col, title_w, receivers, cfg)
    # Behind and below the camera, where the faces' mirror directions point.
    card("CardEmber", col, (0, -13, -4.5), (0, 0, 0.5), (34, 5), V.EMBER, 0.9)
    card("CardPinkL", col, (-11, -11, -0.5), (0, 0, 0.5), (8, 5), V.CRYS_PINK, 0.75)
    card("CardPinkR", col, (12, -11, -1.0), (0, 0, 0.5), (8, 5), V.CRYS_PINK, 0.6)


V.studio_lights = studio_lights


# =============================================================================================================
# Variants
# =============================================================================================================

FIELD = V.VARIANTS["field"]
FIELD.update(lava_light=38.0, colony_light=950.0, colony_emit=10.0, beam=0.45, beam_light=1400.0, eye_strength=45.0,
             drone=(6.8, 10.0, 3.6, 1.6, (5.4, 12.5, V.FLOOR)),
             skitters=[(-2.4, 13.5, 2.0, 0.4), (3.6, 18.0, 1.9, -0.6), (-3.3, 9.0, 1.7, 0.9)],
             eyes=[(-8.5, 5.0, V.FLOOR + 0.35, 1.4), (-6.8, 7.5, V.FLOOR + 0.3, 1.2), (8.0, 6.0, V.FLOOR + 0.4, 1.4),
                   (9.6, 9.5, V.FLOOR + 0.3, 1.1)])
V.VARIANTS["field_ns"] = dict(FIELD, label="CrysiumField_NoSeries", series=False)


# =============================================================================================================
# Lava close-up and contact sheet
# =============================================================================================================

def lava_crop(samples):
    st = V.build("field")
    L2.set_visible(st["cols"], False, False, True)
    L2.SUPER = 1
    cam = L2.camera("CamLava", (0.4, 5.5, V.FLOOR - 0.3), 5.0, lens=45, rise=2.4, fstop=4.0)
    L2.render_png(cam, 900, 500, os.path.join(OUT, "_lava_crop.png"), samples, V.GRADE)


def contact_sheet():
    pad = 24
    W = pad + 920 + pad + 616 + pad + 256 + pad
    lib_h = 360
    H = pad + (430 + pad + lib_h + pad) + (430 + pad) + pad
    sheet = np.zeros((H, W, 4), np.float32)
    sheet[..., :3] = 0.03
    sheet[..., 3] = 1
    p = lambda s: os.path.join(OUT, s)  # noqa: E731
    y = pad
    L2.paste(sheet, C.image_to_array(p("CrysiumField_header_920x430.png")), pad, y)
    L2.paste(sheet, C.image_to_array(p("CrysiumField_capsule_616x353.png")), pad + 920 + pad, y)
    e = C.image_to_array(p("CrysiumField_emblem_256.png"))
    L2.paste(sheet, e, pad + 920 + pad + 616 + pad, y)
    L2.paste(sheet, L2.box_down(e, 4), pad + 920 + pad + 616 + pad, y + 256 + pad)
    y += 430 + pad
    lib = L2.box_down(C.image_to_array(p("CrysiumField_library_logo.png")), 2)
    L2.paste(sheet, L2.checker(lib.shape[1], lib.shape[0]), pad, y)
    L2.paste(sheet, lib, pad, y)
    x2 = pad + 640 + pad
    cardimg = np.ones((lib_h // 2, 320, 4), np.float32) * 0.8
    cardimg[..., 3] = 1
    L2.paste(sheet, cardimg, x2, y)
    L2.paste(sheet, L2.box_down(lib, 2), x2, y)
    L2.paste(sheet, L2.box_down(C.image_to_array(p("CrysiumField_capsule_616x353.png")), 2), x2, y + lib_h // 2 + 8)
    if os.path.exists(p("_lava_crop.png")):
        crop = C.image_to_array(p("_lava_crop.png"))
        crop = L2.box_down(crop, 2)
        L2.paste(sheet, crop, x2 + 320 + pad, y)
    y += lib_h + pad
    L2.paste(sheet, C.image_to_array(p("CrysiumField_NoSeries_header_920x430.png")), pad, y)
    L2.paste(sheet, C.image_to_array(p("CrysiumField_NoSeries_capsule_616x353.png")), pad + 920 + pad, y)
    path = p("Deepwake_v4_contact_sheet.png")
    C.array_to_image(sheet, path)
    C.log("sheet", path)


def main():
    os.makedirs(OUT, exist_ok=True)
    args = C.cli_args()
    if args.get("sheet"):
        contact_sheet()
        return
    L2.enable_gpu()
    samples = int(args.get("samples", 128))
    if args.get("lavacrop"):
        lava_crop(samples)
        return
    only = set(args["only"].split(",")) if args.get("only") else None
    V.outputs(args.get("variant", "field"), only, samples, bool(args.get("half")), bool(args.get("draft")))


if __name__ == "__main__":
    main()
