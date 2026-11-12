"""Round 49G: three concept options for the Sealed One (Remco, third attempt: "everything runs through each other; you
have no idea any more what is head and what isn't, where the mouth is, what runs through the mouth"). Nothing is
patched on the current model: three fresh, simple blockouts, each with one obvious head, one obvious mouth, nothing
passing through either, far fewer parts. Quick unbaked renders (r49_quick style: parts fused at a coarse voxel,
colour and glow per vertex), rest pose, from the play camera (orthographic, 44 degrees down) and a 3/4 close-up of the
head. Nothing is saved or exported; Remco picks one, then it is built properly.

Shared by all three (DESIGN_NOTES, The Sealed One): half out of the ground in a fissure ringed by seven broken seal
stones carved with the amber-gold glyph; ash-ivory bone-stone, deep violet light (the entity); it summons.

A  "Warden skull": a broad ribcage torso and two vast arms planted in front; one long heavy skull thrust forward
   on a thick neck, eyeless, with a single wide mouth across its front (bone-stone lips, a wet dark gum, big ivory
   teeth, violet light in the gullet); a broken halo stands upright BEHIND the shoulders, clear of the head.
B  "The opening seal": a hunched torso under a smooth hooded skull; the face IS the mouth: three jaw petals (two
   upper, one lower) that meet in a pointed snout and part to show rings of ivory teeth round a violet throat
   (the old face plates, made the mouth). Two arms only, a few big violet crystals on the back.
C  "Rising leviathan": a long arched neck rising out of the fissure between two huge forelimbs gripping the seal
   stones; a long narrow head with a deep lower jaw, one row of long fangs on each jaw, violet throat; a collar of
   three glyph plates round the base of the neck (they open when it summons).

Run: blender -b --factory-startup --python Tools/Blender/sealedone_concepts49g.py -- [--only A,B,C]
Writes ArtSource/Previews/QA/r49g_sealedone_concept_<X>_game.png and _close.png.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit_b as DB  # noqa: E402
import r49_quick as Q  # noqa: E402
import r49_views as V  # noqa: E402
from common import G  # noqa: E402

QA = V.QA
BONE, BONE_DARK = (0.6, 0.56, 0.5), (0.32, 0.3, 0.31)
STONE = (0.24, 0.22, 0.25)
VIOLET = (0.62, 0.26, 1.0)
GOLD = (1.0, 0.7, 0.28)
GUM, GUM_DEEP = (0.32, 0.06, 0.12), (0.06, 0.01, 0.05)
MAW = (0.04, 0.015, 0.05)
IVORY = ((0.2, 0.15, 0.1), (0.8, 0.75, 0.62), (0.92, 0.89, 0.8))


def flat(col, emit=(0.0, 0.0, 0.0), seed=0, noise=0.18, cracks=0.0):
    col, emit = np.array(col), np.array(emit)

    def style(s, idx, part):
        p = s.p[idx]
        n = 0.5 + 0.5 * C.value_noise(p, 3.0, seed)
        c = col[None, :] * (1 - noise + 2 * noise * n)[:, None]
        if cracks:
            line = 1 - C.smoothstep(0.0, 0.04, np.abs(C.value_noise(p, 2.2, seed + 5)))
            c = C.lerp3(c, np.array(VIOLET) * 0.6, line * cracks)
            s.emit[idx] = np.clip(np.array(VIOLET)[None, :] * (line * cracks * 0.8)[:, None] + emit[None, :], 0, 1)
        else:
            s.emit[idx] = emit[None, :]
        s.albedo[idx] = np.clip(c, 0, 1)
        s.rough[idx] = 0.7
    return style


def glow(col, k=1.0):
    return flat(tuple(c * 0.4 for c in col), emit=tuple(c * k for c in col), noise=0.05)


def teeth_row(b, points, down, length, radius, seed):
    style = DB.keratin_style(*IVORY, seed=seed, wet=0.6, bands=4)
    for i, p in enumerate(points):
        k = 0.8 + 0.4 * ((i * 7 + seed) % 5) / 4
        b.shard("Tooth", p, down, radius * k, length * k, sides=8, flute=0.05, profile=DB.tooth_profile, rings=8,
                style=style)


def arc(centre, rx, rf, up, a0, a1, n):
    return [Vector(centre) + G(rx * math.sin(a), up, rf * math.cos(a)) for a in np.linspace(a0, a1, n)]


def seal_ring(b):
    stone = flat(STONE, seed=3)
    for i in range(7):
        a = math.radians(180 + 360 / 7 * i)
        p = G(2.75 * math.sin(a), 0.55, 2.75 * math.cos(a) - 0.2)
        tilt = Matrix.Rotation(math.radians(18), 3, Vector((math.cos(a), -math.sin(a), 0)))
        b.ellipsoid("Stone", p, (0.55, 0.22, 0.75), tilt @ Matrix.Rotation(-a, 3, "Z"), style=stone,
                    deform=C.chiselled(8, 20 + i, 0.15), segments=(16, 8))
        b.ellipsoid("Glyph", p + G(0.0, 0.1, 0.0) + Vector((math.sin(a), math.cos(a), 0)) * -0.2,
                    (0.18, 0.04, 0.18), Matrix.Rotation(-a, 3, "Z"), style=glow(GOLD, 1.4), segments=(12, 6))
    b.ellipsoid("Fissure", G(0, -0.3, -0.2), (2.2, 2.0, 0.38), style=flat((0.12, 0.1, 0.13), seed=4),
                deform=C.chiselled(12, 9, 0.2), segments=(40, 16))
    b.ellipsoid("FissureGlow", G(0, -0.05, -0.2), (1.5, 1.4, 0.12), style=glow(VIOLET, 0.9), segments=(32, 8))


def claw_hand(b, wrist, fwd, size, style):
    b.ellipsoid("Hand", wrist, (0.32 * size, 0.36 * size, 0.18 * size), style=style, segments=(20, 10))
    claw = DB.keratin_style((0.1, 0.08, 0.1), (0.3, 0.27, 0.3), (0.85, 0.8, 0.9), seed=4, wet=0.5)
    for s_ in (-0.5, 0.0, 0.5):
        d = (Vector(fwd) + G(s_, -0.15, 0)).normalized()
        b.shard("Claw", wrist + d * 0.22 * size, d + G(0, -0.5, 0), 0.09 * size, 0.6 * size, sides=8,
                profile=DB.claw_profile, bend=G(0, -0.25, 0) * size, style=claw)


def concept_a():
    b = C.PartBuilder()
    bone = flat(BONE, seed=1, cracks=0.35)
    seal_ring(b)
    b.ellipsoid("Torso", G(0, 1.9, -0.2), (1.25, 0.9, 1.35), Matrix.Rotation(math.radians(-15), 3, "X"), style=bone,
                deform=C.chiselled(14, 5, 0.08), segments=(48, 24))
    for i in range(4):
        b.ellipsoid("Rib", G(0, 1.4 + 0.33 * i, 0.55 - 0.07 * i), (1.15 - 0.08 * i, 0.16, 0.12), style=bone,
                    segments=(32, 8))
    b.tube("Neck", [G(0, 2.6, 0.2), G(0, 2.85, 0.75), G(0, 2.8, 1.25)], [0.62, 0.55, 0.5], sides=16, style=bone)
    # The skull: long, heavy, eyeless; a brow plate with a violet seam; a single wide mouth across its front.
    b.ellipsoid("Skull", G(0, 2.95, 1.55), (0.7, 0.95, 0.42), Matrix.Rotation(math.radians(6), 3, "X"), style=bone,
                deform=C.chiselled(14, 8, 0.07), segments=(48, 24))
    b.ellipsoid("Brow", G(0, 3.3, 1.65), (0.72, 0.7, 0.14), Matrix.Rotation(math.radians(-4), 3, "X"),
                style=flat(BONE_DARK, seed=2), deform=C.chiselled(10, 11, 0.1), segments=(40, 12))
    b.tube("Seam", [G(0, 3.43, 1.0), G(0, 3.46, 1.7), G(0, 3.4, 2.25)], [0.05, 0.05, 0.03], sides=8,
           style=glow(VIOLET, 1.3))
    b.ellipsoid("Jaw", G(0, 2.42, 1.6), (0.66, 0.9, 0.24), Matrix.Rotation(math.radians(-8), 3, "X"), style=bone,
                deform=C.chiselled(12, 13, 0.08), segments=(40, 16))
    b.ellipsoid("Maw", G(0, 2.68, 1.65), (0.55, 0.78, 0.13), style=flat(MAW, emit=(0.1, 0.03, 0.2)), segments=(32, 10))
    b.ellipsoid("Gullet", G(0, 2.68, 1.2), (0.3, 0.3, 0.1), style=glow(VIOLET, 1.6), segments=(16, 8))
    gum = DB.gum_style(GUM, GUM_DEEP, seed=3)
    up = arc(G(0, 0, 1.55), 0.6, 0.92, 2.79, -1.45, 1.45, 9)
    lo = arc(G(0, 0, 1.55), 0.55, 0.88, 2.58, -1.45, 1.45, 9)
    b.tube("UpperGum", up, [0.07] * 9, sides=10, style=gum)
    b.tube("LowerGum", lo, [0.07] * 9, sides=10, style=gum)
    teeth_row(b, arc(G(0, 0, 1.55), 0.6, 0.92, 2.79, -1.3, 1.3, 9), G(0, -1, 0.05).normalized(), 0.16, 0.055, 1)
    teeth_row(b, arc(G(0, 0, 1.55), 0.55, 0.88, 2.58, -1.15, 1.15, 8), G(0, 1, -0.05).normalized(), 0.12, 0.05, 2)
    for sx in (-1, 1):
        sh = G(1.15 * sx, 2.3, 0.1)
        el = G(1.75 * sx, 1.2, 1.0)
        wr = G(1.55 * sx, 0.25, 2.0)
        b.ellipsoid("Shoulder", sh, (0.5,) * 3, style=bone, segments=(20, 10))
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5) + G(0.15 * sx, 0.1, 0), el], [0.42, 0.36, 0.3], sides=14, style=bone)
        b.tube("ForeArm", [el, el.lerp(wr, 0.5), wr], [0.3, 0.28, 0.26], sides=14, style=bone)
        claw_hand(b, wr, G(0.1 * sx, 0, 1), 1.3, bone)
    halo = []
    for a in np.linspace(-2.3, 2.3, 14):
        halo.append(G(1.7 * math.sin(a), 3.0 + 1.5 * math.cos(a), -1.0))
    b.tube("Halo", halo, [0.11] * len(halo), sides=10, style=flat(STONE, seed=7))
    for a in np.linspace(-2.0, 2.0, 5):
        b.ellipsoid("HaloGlyph", G(1.7 * math.sin(a), 3.0 + 1.5 * math.cos(a), -0.88), (0.12, 0.05, 0.12),
                    style=glow(GOLD, 1.3), segments=(10, 6))
    return b, G(0, 2.75, 1.6)


def concept_b():
    b = C.PartBuilder()
    bone = flat(BONE, seed=11, cracks=0.35)
    seal_ring(b)
    b.ellipsoid("Torso", G(0, 1.8, -0.3), (1.3, 1.0, 1.45), Matrix.Rotation(math.radians(-25), 3, "X"), style=bone,
                deform=C.chiselled(14, 15, 0.08), segments=(48, 24))
    b.ellipsoid("Hood", G(0, 3.2, 0.55), (0.85, 0.95, 0.75), Matrix.Rotation(math.radians(20), 3, "X"), style=bone,
                deform=C.chiselled(12, 17, 0.06), segments=(48, 24))
    # Three jaw petals meeting in a pointed snout: two upper (left, right), one lower. Each is a thick curved plate
    # rooted in the hood; between them a violet throat and rings of ivory teeth on the petal edges.
    tip = G(0, 2.65, 1.85)
    root = G(0, 2.9, 0.75)
    gap = 0.32
    for name, off in (("PetalL", G(-0.42, 0.25, 0.0)), ("PetalR", G(0.42, 0.25, 0.0)), ("PetalLow", G(0, -0.4, 0.0))):
        o = off.normalized()
        pts = [root + off * 1.1, root.lerp(tip, 0.5) + off * 0.95 + o * gap, tip + o * gap * 1.2]
        b.tube(name, pts, [0.34, 0.26, 0.05], sides=16, style=bone, flatten=0.5)
        teeth = [root.lerp(tip, t) + off * (0.95 - 0.55 * t) * 0.62 + o * gap * 0.6 for t in np.linspace(0.3, 0.85, 5)]
        teeth_row(b, teeth, -o, 0.18, 0.05, 3)
    b.ellipsoid("Throat", root.lerp(tip, 0.45) + G(0, 0.05, 0), (0.28, 0.5, 0.28),
                Matrix.Rotation(math.radians(20), 3, "X"), style=glow(VIOLET, 1.6), segments=(20, 10))
    b.ellipsoid("Gum", root.lerp(tip, 0.3) + G(0, 0.05, 0), (0.42, 0.25, 0.42), style=DB.gum_style(GUM, GUM_DEEP),
                segments=(20, 10))
    for sx in (-1, 1):
        sh = G(1.2 * sx, 2.2, 0.0)
        el = G(1.9 * sx, 1.0, 0.8)
        wr = G(1.6 * sx, 0.25, 1.8)
        b.ellipsoid("Shoulder", sh, (0.55,) * 3, style=bone, segments=(20, 10))
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5) + G(0.2 * sx, 0.1, 0), el], [0.45, 0.38, 0.3], sides=14, style=bone)
        b.tube("ForeArm", [el, el.lerp(wr, 0.5), wr], [0.3, 0.28, 0.26], sides=14, style=bone)
        claw_hand(b, wr, G(0.1 * sx, 0, 1), 1.3, bone)
    cry = flat((0.4, 0.15, 0.7), emit=(0.5, 0.2, 0.85), seed=21)
    for i, (x, f, ln) in enumerate(((0.0, -1.2, 1.2), (-0.6, -0.9, 0.9), (0.55, -0.8, 0.8))):
        b.shard("Crystal", G(x, 2.4, f), G(x * 0.5, 1.0, -0.6), 0.22, ln, sides=6, style=cry)
    return b, G(0, 2.8, 1.3)


def concept_c():
    b = C.PartBuilder()
    bone = flat(BONE, seed=31, cracks=0.35)
    seal_ring(b)
    # Shoulders just out of the ground, a long arched neck, a long narrow head with a deep lower jaw.
    b.ellipsoid("Shoulders", G(0, 0.9, -0.4), (1.5, 1.0, 0.8), style=bone, deform=C.chiselled(14, 25, 0.08),
                segments=(40, 20))
    neck = [G(0, 1.3, -0.4), G(0, 2.6, -0.3), G(0, 3.6, 0.3), G(0, 3.75, 1.0)]
    b.tube("Neck", neck, [0.75, 0.6, 0.5, 0.45], sides=18, style=bone)
    for i, t in enumerate(np.linspace(0.15, 0.85, 6)):
        b.ellipsoid("NeckPlate", C.catmull_rom(neck, 8)[int(t * 23)] + G(0, 0.0, -0.35), (0.35, 0.25, 0.2),
                    style=flat(BONE_DARK, seed=40 + i), segments=(16, 8))
    b.ellipsoid("Head", G(0, 3.75, 1.55), (0.5, 0.95, 0.38), Matrix.Rotation(math.radians(14), 3, "X"), style=bone,
                deform=C.chiselled(12, 27, 0.07), segments=(40, 20))
    b.ellipsoid("Jaw", G(0, 3.25, 1.55), (0.45, 0.9, 0.2), Matrix.Rotation(math.radians(-6), 3, "X"), style=bone,
                deform=C.chiselled(12, 29, 0.08), segments=(32, 14))
    b.ellipsoid("Maw", G(0, 3.45, 1.55), (0.36, 0.78, 0.14), Matrix.Rotation(math.radians(4), 3, "X"),
                style=flat(MAW, emit=(0.1, 0.03, 0.2)), segments=(24, 10))
    b.ellipsoid("Gullet", G(0, 3.45, 1.0), (0.22, 0.3, 0.1), style=glow(VIOLET, 1.6), segments=(16, 8))
    gum = DB.gum_style(GUM, GUM_DEEP, seed=5)
    up = arc(G(0, 0, 1.45), 0.4, 0.95, 3.56, -1.35, 1.35, 9)
    lo = arc(G(0, 0, 1.45), 0.36, 0.9, 3.34, -1.35, 1.35, 9)
    b.tube("UpperGum", up, [0.06] * 9, sides=10, style=gum)
    b.tube("LowerGum", lo, [0.06] * 9, sides=10, style=gum)
    teeth_row(b, arc(G(0, 0, 1.45), 0.4, 0.95, 3.56, -1.2, 1.2, 9), G(0, -1, 0.05).normalized(), 0.15, 0.045, 7)
    teeth_row(b, arc(G(0, 0, 1.45), 0.36, 0.9, 3.34, -1.1, 1.1, 8), G(0, 1, -0.05).normalized(), 0.12, 0.04, 8)
    b.tube("Seam", [G(0, 4.08, 0.9), G(0, 4.12, 1.6), G(0, 4.0, 2.2)], [0.04, 0.04, 0.025], sides=8,
           style=glow(VIOLET, 1.3))
    for sx in (-1, 1):
        sh = G(1.3 * sx, 1.1, -0.1)
        el = G(2.1 * sx, 1.2, 0.8)
        wr = G(2.2 * sx, 0.5, 1.9)
        b.tube("UpperArm", [sh, sh.lerp(el, 0.5) + G(0.1 * sx, 0.25, 0), el], [0.5, 0.42, 0.34], sides=14, style=bone)
        b.tube("ForeArm", [el, el.lerp(wr, 0.5), wr], [0.34, 0.3, 0.28], sides=14, style=bone)
        claw_hand(b, wr, G(0.15 * sx, 0, 1), 1.4, bone)
    for i, a in enumerate((-1.2, 0.0, 1.2)):
        d = G(math.sin(a), 0.35, math.cos(a) * 0.4 - 0.3).normalized()
        c = G(0, 1.9, -0.25) + d * 0.65
        b.ellipsoid("Collar", c, (0.62, 0.08, 0.5), Matrix.Rotation(-a, 3, "Z") @ Matrix.Rotation(math.radians(-55), 3, "X"),
                    style=flat(STONE, seed=50 + i), deform=C.chiselled(8, 51 + i, 0.12), segments=(20, 8))
        b.ellipsoid("CollarGlyph", c + d * 0.09, (0.2, 0.03, 0.2), Matrix.Rotation(-a, 3, "Z") @
                    Matrix.Rotation(math.radians(-55), 3, "X"), style=glow(GOLD, 1.3), segments=(12, 6))
    return b, G(0, 3.5, 1.5)


CONCEPTS = {"A": concept_a, "B": concept_b, "C": concept_c}


def render(key):
    C.reset_scene()
    b, head = CONCEPTS[key]()
    parts = b.to_object("Q_Parts")
    mesh = C.fuse(parts, 0.025, 3)
    bpy.data.objects.remove(parts)
    C.detail(mesh, b)
    C.shade_smooth(mesh)
    o = C.link(bpy.data.objects.new("Concept_" + key, mesh))
    o.data.materials.append(Q.vertex_material())
    co = np.array([v.co for v in mesh.vertices])
    lo, hi = Vector(co.min(axis=0)), Vector(co.max(axis=0))
    size = max(hi - lo)
    C.preview_world(color=(0.16, 0.17, 0.2), strength=1.2)
    for lt in C.light_rig(scale=max(1.0, size * 0.8)):
        lt.data.energy *= 1.3 if lt.data.type == "SUN" else 1.2
    C.ground(color=(0.2, 0.21, 0.24))
    centre = (lo + hi) * 0.5
    C.render(C.game_camera(centre, size * 1.5), os.path.join(QA, f"r49g_sealedone_concept_{key}_game.png"), 1280, 720)
    C.render(C.closeup_camera(head + Vector((0, -0.2, 0)), size * 0.9, azimuth=-32, elevation=12, lens=50),
             os.path.join(QA, f"r49g_sealedone_concept_{key}_close.png"), 1280, 720)


def main():
    for key in (C.cli_args().get("only") or "A,B,C").split(","):
        render(key)


if __name__ == "__main__":
    main()
