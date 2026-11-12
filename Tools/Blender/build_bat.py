"""Bat: the element bat (Ember, Moss, Dust, Tide, Frost, Mire) that rises from element nests and spits element bolts
(round 10, see ArtSource/DESIGN_NOTES.md). Air only.

A small plated body under wide membrane wings; each wing is a membrane over three long hooked spines fanning from a
knobbed wrist, scalloped between them, with a glowing leading edge and pale bands across the top; a short wedge head
with a dropping jaw around a glowing bolt throat, four eyes in a row on each side and two swept-back sensor horns; a
glowing bolt gland on the chest; a dorsal plate with a forward chevron; a whip tail with a forked vane; two talons.

One mesh and one rig for every element: the element is a palette (`ELEMENTS`). The default export bakes Ember into
Exports/Bat/; `--element Moss,Dust` also bakes those skins onto the same game mesh into Exports/Bat/Skins/<Element>/
(maps named Bat_<Element>_Albedo/_Normal/_Emission/_Mask.png).

Clips: Fly (flap loop), Glide (loop), Hit, Death. Also exports Bat_LOD1.

Run: blender --background --python Tools/Blender/build_bat.py -- [--preview <dir>] [--norender] [--element Moss,Dust]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import chitin as H  # noqa: E402
import common as C  # noqa: E402
import creature_kit2 as K2  # noqa: E402
from common import G  # noqa: E402

NAME = "Bat"
LOD_NAME = "Bat_LOD1"
K = 1.0
GAME_TRIS = 3900
LOD_TRIS = 1700
TEX, LOW_TEX = 1024, 512
FAMILIES = ("membrane", "chitin", "crystal")
GAME_SCALE = 0.65
AIR_HEIGHT = 1.6
DEFAULT_ELEMENT = "Ember"


def skin(top, side, belly, glow, eye, membrane, under, bone=(0.9, 0.82, 0.7), flush=None):
    pal = H.Palette(top=top, side=side, belly=belly, seam=tuple(c * 0.25 for c in belly), dark=tuple(c * 0.6 for c in side),
                    glow=glow, eye=eye, membrane=membrane, bone=bone)
    return {"pal": pal, "under": np.array(under), "flush": np.array(flush) if flush is not None else None}


# Element palettes from ProjectGenerator.Bestiary (CreateBat colour, eyes, accent), set on a dark body so the glow
# carries. Tide and Frost are cool by element (water and ice); they keep the data colours (curator's call).
ELEMENTS = {
    "Ember": skin(top=(0.42, 0.13, 0.06), side=(0.16, 0.05, 0.03), belly=(0.05, 0.025, 0.02), glow=(1.0, 0.45, 0.12),
                  eye=(1.0, 0.88, 0.45), membrane=(0.4, 0.1, 0.04), under=(0.5, 0.2, 0.1), flush=(0.6, 0.16, 0.05)),
    "Moss": skin(top=(0.2, 0.3, 0.1), side=(0.07, 0.11, 0.04), belly=(0.03, 0.04, 0.02), glow=(0.55, 1.0, 0.3),
                 eye=(0.9, 1.0, 0.55), membrane=(0.18, 0.32, 0.08), under=(0.3, 0.38, 0.16), flush=(0.3, 0.45, 0.1)),
    "Dust": skin(top=(0.55, 0.42, 0.2), side=(0.22, 0.15, 0.07), belly=(0.06, 0.045, 0.03), glow=(1.0, 0.85, 0.42),
                 eye=(1.0, 0.95, 0.7), membrane=(0.45, 0.32, 0.12), under=(0.6, 0.5, 0.3), flush=(0.65, 0.45, 0.15)),
    "Tide": skin(top=(0.12, 0.25, 0.42), side=(0.05, 0.09, 0.17), belly=(0.02, 0.03, 0.05), glow=(0.4, 0.9, 1.0),
                 eye=(0.8, 1.0, 1.0), membrane=(0.08, 0.2, 0.36), under=(0.25, 0.4, 0.55), flush=(0.1, 0.3, 0.55)),
    "Frost": skin(top=(0.5, 0.6, 0.7), side=(0.2, 0.25, 0.32), belly=(0.05, 0.06, 0.08), glow=(0.8, 0.95, 1.0),
                  eye=(0.9, 1.0, 1.0), membrane=(0.35, 0.45, 0.55), under=(0.6, 0.7, 0.8), flush=(0.5, 0.65, 0.8)),
    "Mire": skin(top=(0.25, 0.28, 0.12), side=(0.09, 0.1, 0.04), belly=(0.03, 0.035, 0.02), glow=(0.6, 0.9, 0.3),
                 eye=(0.95, 1.0, 0.6), membrane=(0.22, 0.25, 0.07), under=(0.35, 0.36, 0.16), flush=(0.35, 0.3, 0.1)),
}

SIDES = (("L", -1), ("R", 1))
X = Vector((1, 0, 0))
Yv = Vector((0, 1, 0))
Z = Vector((0, 0, 1))

CFG = {
    "shoulder": (0.075, 0.04, 0.03),
    "wrist": (0.3, 0.09, 0.08),
    "tips": ((0.74, 0.06, 0.03), (0.66, 0.02, -0.17), (0.42, 0.0, -0.3)),
    "flank": (0.07, 0.0, -0.14),
    "tail": [G(0, 0.0, -0.13), G(0, -0.015, -0.29), G(0, -0.03, -0.44)],
}


def gp(p, sx):
    return G(p[0] * sx, p[1], p[2])


def wing_edge(sx, v, cfg):
    """Outer edge of the skin, v 0..1: tip 1 -> tip 2 -> tip 3 -> flank, scalloped between the tips."""
    pts = [gp(t, sx) for t in cfg["tips"]] + [gp(cfg["flank"], sx)]
    seg = min(int(v * 3), 2)
    f = v * 3 - seg
    p = pts[seg].lerp(pts[seg + 1], f)
    hub = gp(cfg["wrist"], sx).lerp(gp(cfg["shoulder"], sx), 0.5)
    return p.lerp(hub, (0.22 if seg < 2 else 0.12) * math.sin(math.pi * f))


def wing_point(sx, u, v, side, cfg):
    inner = gp(cfg["wrist"], sx).lerp(gp(cfg["shoulder"], sx), v)
    p = inner.lerp(wing_edge(sx, v, cfg), u)
    p = p + Vector((0, 0, 0.015 * math.sin(math.pi * u) * math.sin(math.pi * v)))
    half = 0.004 + 0.005 * (1 - u)
    return p + Vector((0, 0, side * half))


def wing_uv_fn(cfg):
    """(u, v) for the fin style: u from the body out to the tip, v from the leading edge to the trailing edge."""
    sh, wr, tips, fl = cfg["shoulder"], cfg["wrist"], cfg["tips"], cfg["flank"]
    lead_x = [sh[0], wr[0], tips[0][0]]
    lead_f = [sh[2], wr[2], tips[0][2]]
    trail_x = [fl[0], tips[2][0], tips[1][0], tips[0][0]]
    trail_f = [fl[2], tips[2][2], tips[1][2], tips[0][2] - 0.02]
    span = tips[0][0] - sh[0]

    def fn(points):
        x = np.abs(points[:, 0])
        f = -points[:, 1]
        u = np.clip((x - sh[0]) / span, 0, 1)
        lo = np.interp(x, lead_x, lead_f)
        hi = np.interp(x, trail_x, trail_f)
        v = np.clip((lo - f) / np.maximum(lo - hi, 0.02), 0, 1)
        return u, v
    return fn


def head_rot(deg):
    return Matrix.Rotation(math.radians(deg), 3, "X")


def build_parts(sk, cfg=CFG, b=None):
    b = b or C.PartBuilder()
    k = K
    pal = sk["pal"]
    b.ellipsoid("Body", G(0, 0.0, 0.0), (0.085, 0.14, 0.075), bone="Body", segments=(32, 16),
                style=H.gill_belly(pal, k, sk["under"], slits=3, start=-0.3, spacing=0.16, width=0.03, side=(0.25, 0.7), glow=0.8))
    b.ellipsoid("Back", G(0, 0.055, -0.01), (0.07, 0.125, 0.03), head_rot(4), bone="Body", segments=(32, 14),
                style=K2.armour_plate(pal, k, rim_glow=0.6, chevron=1.0, chevron_angle=1.1, lip=0.5))
    b.ellipsoid("Gland", G(0, -0.05, 0.06), (0.05, 0.07, 0.042), bone="Body", segments=(28, 14),
                style=K2.ribbed_bladder(pal, k, ribs=6, seed=11, plate=0.0))
    b.ellipsoid("Head", G(0, 0.03, 0.16), (0.06, 0.085, 0.048), head_rot(-8), bone="Head", segments=(28, 14),
                style=H.wedge_head(pal, k, slits=2, slit_width=0.04))
    b.ellipsoid("Brow", G(0, 0.065, 0.16), (0.065, 0.07, 0.022), head_rot(-18), bone="Head", segments=(24, 10),
                style=H.crest(pal, k, ribs=5))
    b.ellipsoid("Jaw", G(0, -0.01, 0.18), (0.045, 0.07, 0.022), head_rot(10), bone="Jaw", segments=(20, 10),
                style=H.band_plates(pal, k, 2, height=0.002))
    b.ellipsoid("Throat", G(0, 0.008, 0.19), (0.018, 0.045, 0.01), bone="Head", style=H.inner_glow(pal, 0.8), segments=(16, 8))
    for side, sx in SIDES:
        for i in range(4):
            b.ellipsoid("Eye", G((0.045 + 0.008 * i) * sx, 0.045 - 0.004 * i, 0.215 - 0.025 * i), (0.008, 0.008, 0.007),
                        bone="Head", style=H.eye(pal, k, 0.45), segments=(10, 6))
        root = G(0.03 * sx, 0.07, 0.13)
        b.tube("Horn", [root, root + G(0.03 * sx, 0.05, -0.06), root + G(0.065 * sx, 0.06, -0.16)], [0.016, 0.011, 0.0015],
               sides=8, chain=["Head"], style=H.hook(pal, k, glow=0.7, glow_from=0.7))
        root = G(0.04 * sx, -0.055, -0.03)
        b.tube("Talon", [root, root + G(0.005 * sx, -0.045, 0.0), root + G(0.0, -0.06, 0.04)], [0.014, 0.009, 0.0015],
               sides=8, chain=["Body"], style=H.hook(pal, k))
        # Wing: arm and wrist knob, three hooked spines, the membrane between.
        arm, hand = f"Arm_{side}", f"Hand_{side}"
        shoulder, wrist = gp(cfg["shoulder"], sx), gp(cfg["wrist"], sx)
        b.tube("Arm", [shoulder, shoulder.lerp(wrist, 0.5) + Vector((0, 0, 0.01)), wrist], [0.032, 0.024, 0.02],
               sides=10, chain=[arm], style=H.leg(pal, k, "Femur"))
        b.ellipsoid("Wrist", wrist, (0.026, 0.026, 0.022), bone=arm, style=H.band_plates(pal, k, 1, height=0.0), segments=(14, 7))
        b.parts[-1].chain = [arm, hand]
        b.tube("Thumb", [wrist, wrist + G(0.0, 0.02, 0.04), wrist + G(-0.01 * sx, 0.0, 0.07)], [0.013, 0.008, 0.001],
               sides=8, chain=[hand], style=H.hook(pal, k, glow=0.8, glow_from=0.6))
        for i, tip in enumerate(cfg["tips"]):
            t = gp(tip, sx)
            mid = wrist.lerp(t, 0.5) + Vector((0, 0, 0.012))
            hook = t + (t - mid).normalized() * 0.03 + Vector((0, 0, -0.025))
            b.tube("Spine", [wrist, mid, t, hook], [0.018, 0.013, 0.007, 0.0012], sides=8, chain=[f"Finger_{side}{i + 1}"],
                   style=H.tube(pal, k, pal.side, pal.bone, tip_glow=0.6, glow_from=0.85))
        fingers = [f"Finger_{side}{i + 1}" for i in range(3)]
        fin = H.fin_sheet(pal, k, wing_uv_fn(cfg), sk["under"], rays=3, chevrons=3, chevron_width=0.17, lead_glow=1.0,
                          lead_width=0.13, flush=sk["flush"], dashed=False)
        for name, upper, lower in (("Skin", 1.0, 0.0), ("SkinUnder", 0.0, -1.0)):
            b.slab(name, lambda u, v, sx=sx, h=upper: wing_point(sx, u, v, h, cfg),
                   lambda u, v, sx=sx, h=lower: wing_point(sx, u, v, h, cfg), 22, 28, chain=[arm, hand] + fingers, style=fin)

    tail = cfg["tail"]
    b.tube("Tail", tail, [0.03, 0.016, 0.009], sides=8, chain=["Tail_1", "Tail_2"],
           style=H.tube(pal, k, pal.side, pal.dark, rings=(0.3, 0.55, 0.8)))
    end = tail[-1]
    for sx in (-1, 1):
        b.tube("Vane", [end, end + G(0.03 * sx, 0.0, -0.04), end + G(0.06 * sx, -0.005, -0.1)], [0.022, 0.016, 0.0015],
               sides=8, chain=["Tail_2"], style=H.blade(pal, k, glow=0.9), flatten=0.3)
    return b


def bones(cfg=CFG):
    tail = cfg["tail"]
    out = [
        ("Root", G(0, -0.5, 0), G(0, -0.5, 0.2), None, True),
        ("Body", G(0, 0.0, -0.13), G(0, 0.02, 0.1), "Root", True),
        ("Head", G(0, 0.03, 0.1), G(0, 0.03, 0.26), "Body", True),
        ("Jaw", G(0, 0.0, 0.12), G(0, -0.015, 0.25), "Head", True),
        ("Tail_1", tail[0], tail[1], "Body", True),
        ("Tail_2", tail[1], tail[2], "Tail_1", True),
    ]
    for side, sx in SIDES:
        shoulder, wrist = gp(cfg["shoulder"], sx), gp(cfg["wrist"], sx)
        out.append((f"Arm_{side}", shoulder, wrist, "Body", True))
        out.append((f"Hand_{side}", wrist, wrist + G(0.04 * sx, 0.0, -0.02), f"Arm_{side}", True))
        for i, tip in enumerate(cfg["tips"]):
            out.append((f"Finger_{side}{i + 1}", wrist, gp(tip, sx), f"Hand_{side}", True))
    return out


# ------------------------------------------------------------------------------------------------------------
# Animation
# ------------------------------------------------------------------------------------------------------------

def specs_for(loc=None, pitch=0.0, roll=0.0, yaw=0.0, head=0.0, jaw=0.0, arm=0.0, sweep=0.0, hand=0.0, fold=0.0,
              finger_lift=0.0, tail=(0.0, 0.0)):
    """arm: wing up (rad), sweep: arm swept back, hand: wrist bend up, fold: spines folded back to the body,
    finger_lift: spines bent up; tail: (pitch, yaw)."""
    specs = {
        "Root": {"rot": []},
        "Body": {"rot": [(Z, yaw), (Yv, roll), (X, pitch)], "loc": loc if loc is not None else Vector()},
        "Head": {"rot": [(X, head)]},
        "Jaw": {"rot": [(X, jaw)]},
        "Tail_1": {"rot": [(Z, -tail[1]), (X, tail[0])]},
        "Tail_2": {"rot": [(Z, -tail[1] * 1.4), (X, tail[0] * 1.3)]},
    }
    for side, sx in SIDES:
        flap = Vector((0, -sx, 0))
        back = Vector((0, 0, sx))
        specs[f"Arm_{side}"] = {"rot": [(back, sweep), (flap, arm)]}
        specs[f"Hand_{side}"] = {"rot": [(flap, hand)]}
        for i in range(3):
            specs[f"Finger_{side}{i + 1}"] = {"rot": [(back, fold * (0.5 + 0.35 * i)), (flap, finger_lift * (1 - 0.2 * i))]}
    return specs


FLY_FRAMES = 13  # 0.43 s per beat (the element bats' 0.42 s)


def fly_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        up = math.cos(w)
        return specs_for(loc=Vector((0, 0, -0.02 * math.cos(w))), pitch=d(4) * math.sin(w), head=-d(5) * math.sin(w),
                         jaw=d(4), arm=d(10) + d(44) * math.sin(w), sweep=d(10) * up, hand=d(26) * math.sin(w - 0.9),
                         fold=d(30) * max(0.0, up), finger_lift=d(12) * math.sin(w - 1.5),
                         tail=(d(10) * math.sin(w - 1.2), d(4) * math.sin(w)))
    return FLY_FRAMES, spec, True


def glide_spec(poser):
    d = math.radians

    def spec(t, f):
        w = 2 * math.pi * t
        return specs_for(loc=Vector((0, 0, 0.012 * math.sin(w))), roll=d(6) * math.sin(w), yaw=d(3) * math.sin(w + 1),
                         pitch=d(2) * math.sin(2 * w), head=d(4) * math.sin(w + 0.5), jaw=d(3),
                         arm=d(8) + d(3) * math.sin(2 * w), hand=-d(4) + d(2) * math.sin(2 * w + 1), fold=-d(4),
                         finger_lift=d(3) * math.sin(2 * w + 2), tail=(d(5) * math.sin(w), d(14) * math.sin(w + 0.6)))
    return 48, spec, True


def hit_spec(poser):
    d = math.radians

    def spec(t, f):
        k = K2.recoil(t, 0.3, 6.0, 12.0)
        return specs_for(loc=Vector((0, 0.02 * k, 0.03 * k)), pitch=-d(14) * k, roll=d(12) * k, head=-d(14) * k,
                         jaw=d(4) + d(28) * max(k, 0.0), arm=d(8) + d(38) * k, sweep=-d(8) * k, hand=d(15) * k,
                         fold=d(15) * k, tail=(d(18) * k, -d(12) * k))
    return 12, spec, False


def death_spec(poser):
    d = math.radians

    def spec(t, f):
        spin = K2.ease(0.05, 1.0, t)
        fold = K2.ease(0.0, 0.5, t)
        flail = math.exp(-t * 5) * math.sin(t * 40)
        return specs_for(roll=d(320) * spin, pitch=d(40) * fold, head=d(25) * fold, jaw=d(35) * fold,
                         arm=d(60) * fold + d(25) * flail, sweep=d(40) * fold, hand=d(35) * fold, fold=d(60) * fold,
                         finger_lift=d(15) * fold, tail=(-d(30) * fold, d(20) * fold))
    return 36, spec, False


CLIPS = {"Fly": fly_spec, "Glide": glide_spec, "Hit": hit_spec, "Death": death_spec}
# LOD1: jaw into the head, one tail bone, the second and third spine fold into the hand. 10 bones.
LOD_DROP = {"Jaw": "Head", "Tail_2": "Tail_1", **{f"Finger_{s}{i}": f"Hand_{s}" for s, _ in SIDES for i in (2, 3)}}
LOD_AIMS = {"Tail_1": ("Tail_1", "Tail_2")}
RIGID = {"Back", "Head", "Brow", "Jaw", "Horn", "Wrist"}


def build_asset(name, builder, tris, k=K, voxel=0.0028):
    return K2.build_mesh_asset_ex(name, builder, voxel=voxel, target_tris=tris, tex_size=TEX, ao_distance=0.07,
                                  extrusion=0.014, ray=0.04, emission_strength=2.5,
                                  extra=H.weathering(k, scratches=0.4, dust=0.35, seed=71), specular=0.3, k=k,
                                  families=FAMILIES, low_size=LOW_TEX)


def previews(arm, mesh, actions, lod, out, asset):
    fly, glide = "Fly", "Glide"
    h = AIR_HEIGHT
    layout = [(fly, 0, (-1.4, 0.9), 0.25), (fly, 4, (0.0, 1.3), -0.1), (glide, 20, (1.4, 0.6), 0.3),
              (fly, 9, (-0.6, -0.3), -0.2), (fly, 7, (0.8, -0.7), 0.05), (glide, 3, (-1.7, -0.6), 0.4)]
    return K2.enemy_previews("bat", arm, mesh, actions, lod, out, asset, target=(0, 0.0, 0.0), distance=2.4,
                             game_scale=GAME_SCALE, move=(fly, 3), layout=layout, idle=glide,
                             strips=[(fly, "front", range(0, FLY_FRAMES, 2)), (fly, "close", range(0, FLY_FRAMES, 2)),
                                     ("Death", "close", range(0, 37, 6))],
                             macros=[("wing", G(-0.45, 0.06, -0.06), 0.5, -40, 45), ("head", G(0.0, 0.04, 0.17), 0.4, -35, 15),
                                     ("gland", G(0.0, -0.05, 0.05), 0.35, -20, -10)],
                             rig_scale=1.2, lift=0.7, game_lift=h, game_target=(0, 0.3, 0.9), death_frame=18)


def element_skins(asset, arm, names, out):
    """Bakes the requested element skins onto the game mesh and renders each in the game view zoomed."""
    import bpy
    done = {}
    for el in names:
        sk_builder = build_parts(ELEMENTS[el])
        folder = os.path.join(C.EXPORTS, NAME, "Skins", el)
        tex = K2.rebake_skin(asset, sk_builder, folder, f"{NAME}_{el}", extra=H.weathering(K, scratches=0.4, dust=0.35, seed=71))
        done[el] = tex
        if out:
            default = asset.low.data.materials[0]
            asset.low.data.materials[0] = K2.skin_material(asset, f"{NAME}_{el}_Mat", tex)
            act = bpy.data.actions["Glide"]
            single = [(act, 10, (0, 0, AIR_HEIGHT), 0.0)]
            C.preview_world()
            C.game_view_mixed(arm, asset.low, os.path.join(out, f"bat_skin_{el.lower()}.png"), GAME_SCALE, single, zoom=4.0,
                              target=(0, 0, AIR_HEIGHT + 0.1))
            asset.low.data.materials[0] = default
    return done


def main():
    C.reset_scene()
    out = C.preview_dir()
    builder = build_parts(ELEMENTS[DEFAULT_ELEMENT])
    asset = build_asset(NAME, builder, GAME_TRIS)
    bone_list = bones()
    arm = C.build_armature(NAME + "_Rig", bone_list)
    C.skin(asset.low, arm, builder, rigid=RIGID, blend=2)
    poser = C.Poser(arm)
    actions = C.key_clips(poser, CLIPS)
    arm.animation_data.action = actions["Fly"]
    C.export_fbx(os.path.join(C.EXPORTS, NAME, NAME + ".fbx"), [arm, asset.low], animated=True)
    lod = C.build_lod(NAME, LOD_NAME, asset, builder, bone_list, CLIPS, poser, actions, LOD_TRIS, LOD_DROP, LOD_AIMS)
    extra = [e for e in (C.cli_args().get("element") or "").split(",") if e and e != DEFAULT_ELEMENT]
    norender = C.cli_args().get("norender")
    if not norender:
        previews(arm, asset.low, actions, lod, out, asset)
    skins = element_skins(asset, arm, extra, None if norender else out)
    C.write_report(NAME, {"asset": NAME, "game_tris": asset.low_tris, "source_tris": asset.high_tris,
                          "bones": len(arm.data.bones), "texture_size": TEX, "low_texture_size": LOW_TEX,
                          "maps": ["Albedo", "Normal", "Emission", "Mask"], "uv": asset.uv_stats, "detail": asset.details,
                          "actions": {n: CLIPS[n](poser)[0] for n in CLIPS}, "flap_seconds": round(FLY_FRAMES / 30, 2),
                          "pivot": "body centre (flight height)", "unity_scale": GAME_SCALE,
                          "default_element": DEFAULT_ELEMENT, "elements": list(ELEMENTS),
                          "skins_baked": {el: f"Skins/{el}/" for el in skins},
                          "lod1": {"fbx": LOD_NAME + ".fbx", "tris": C.tri_count(lod[1]), "bones": len(lod[0].data.bones)}})
    C.save_blend(NAME)
    # Round 46: the close-up detail (element-neutral eyes, teeth, talons) as extra parts, exported again with the body.
    import bat_detail
    bat_detail.run(norender=bool(C.cli_args().get("norender")))


if __name__ == "__main__":
    main()
