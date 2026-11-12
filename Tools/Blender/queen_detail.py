"""Round 45: the Hive Queen's close-up detail (Remco: "the ant queen looks plain af from close up").

Two extra skinned parts (detail_kit, gravemaw_maw technique), exported in Queen.fbx next to the body:
- Queen_Face (Queen_Face_Mat, Queen_Face_*.png): a brow of four wet spider eyes per side over the old bead brow
  (slit pupils, hot amber-pink irises, dark sockets), a big faceted compound eye on each side of the head, and rows
  of hooked teeth on the inner faces of the four petal jaws (they open with the petals).
- Queen_Claws (Queen_Claws_Mat, Queen_Claws_*.png): a grappling-hook foot of three keratin talons at the end of
  every leg, saw teeth down the inner edge of both scythes and a hooked, glowing point on each scythe.
The Queen has no LOD1 export, so these have none either. No bone, clip or event changes.

Run after build_queen.py (which calls run() itself at the end of a rebuild):
  blender -b --factory-startup --python Tools/Blender/queen_detail.py -- [--before | --qa] [--norender]
  --before: only the QA renders of the current export (ArtSource/Previews/QA/r45_queen_before_*.png)
  --qa: only the after renders from the saved .blend
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import detail_kit as D  # noqa: E402
from common import G  # noqa: E402

NAME = "Queen"
PARTS = ("Face", "Claws")
# Budget (round 45): at most +20% of the body's 14490 game triangles for both parts together.
FACE_TRIS, CLAW_TRIS = 1850, 980
VOXEL = 0.0032

PETALS = ("UL", "UR", "LL", "LR")
SIDES = (("L", -1), ("R", 1))

# Colours (sRGB): the species' crimson and pink-red glow, eyes amber-pink so they stand out from the crown.
EYE = D.Eye(lens=(0.04, 0.006, 0.012), iris=(1.0, 0.66, 0.26), rim=(0.3, 0.03, 0.04), pupil=(0.006, 0.0, 0.002),
            glow=(1.0, 0.5, 0.28), vein=(0.35, 0.03, 0.06))
COMPOUND = D.Eye(lens=(0.03, 0.006, 0.01), iris=(0.95, 0.25, 0.38), rim=(0.12, 0.01, 0.03), pupil=(0.0, 0.0, 0.0),
                 glow=(1.0, 0.3, 0.42))
SOCKET = D.Flesh(dark=(0.03, 0.005, 0.01), flesh=(0.4, 0.06, 0.08), glow=(1.0, 0.34, 0.46))
TOOTH = D.Keratin(root=(0.32, 0.05, 0.08), body=(0.8, 0.6, 0.54), tip=(0.97, 0.92, 0.86), glow=(1.0, 0.34, 0.46),
                  stain=(0.28, 0.02, 0.05))
CLAW = D.Keratin(root=(0.06, 0.012, 0.018), body=(0.22, 0.05, 0.06), tip=(0.92, 0.74, 0.7), glow=(1.0, 0.34, 0.46),
                 stain=(0.12, 0.01, 0.02))

HEAD_FOCUS = Vector((0.0, -0.95, 1.92))
SCYTHE_FOCUS = Vector((-0.75, -1.25, 1.5))
FOOT_FOCUS = Vector((-1.5, -0.95, 0.12))


# ------------------------------------------------------------------------------------------------------------
# The body's geometry (from build_queen.py, which runs on import, so the few numbers are repeated here)
# ------------------------------------------------------------------------------------------------------------

def petal_points(name):
    sx = -1 if name[1] == "L" else 1
    sy = 1 if name[0] == "U" else -1
    return [G(0.13 * sx, 1.86 + 0.1 * sy, 0.84), G(0.24 * sx, 1.82 + 0.17 * sy, 1.06),
            G(0.13 * sx, 1.7 + 0.09 * sy, 1.28), G(0.025 * sx, 1.62 + 0.02 * sy, 1.4)]


def blade_points(sx):
    elbow = G(0.8 * sx, 2.35, 0.55)
    return [elbow, G(0.92 * sx, 2.0, 0.95), G(0.78 * sx, 1.4, 1.4), G(0.45 * sx, 1.05, 1.55), G(0.25 * sx, 1.12, 1.4)]


def old_brow(sx, i):
    """The round-2 bead eyes (5 per side) the new brow covers."""
    x = 0.03 + 0.055 * i
    return G(x * sx, 2.02 + 0.045 * i, 0.86 - 0.035 * i)


THROAT = G(0, 1.83, 0.86)
MOUTH_TIP = G(0, 1.62, 1.4)


def curve(points, n=40):
    pts = C.catmull_rom([Vector(p) for p in points], 8)
    out = []
    for i in range(n):
        out.append(pts[int(round(i / (n - 1) * (len(pts) - 1)))])
    return out


# ------------------------------------------------------------------------------------------------------------
# Face
# ------------------------------------------------------------------------------------------------------------

def face_parts(boss):
    b = C.PartBuilder()
    eye_l = [D.eye_style(EYE, pupil="slit", iris_size=0.52, pupil_size=0.2, glow=1.0, seed=s) for s in range(4)]
    sock = D.socket_style(SOCKET, seed=3)
    head_c = boss.along("Head", 0.0) + Vector((0, -0.12, 0.08))
    # Brow: a wet eye on every round-2 bead (they glowed through), biggest in front: a spider's V of slit eyes.
    brow = (0.04, 0.037, 0.034, 0.03, 0.027)
    for side, sx in SIDES:
        for i, r in enumerate(brow):
            p = old_brow(sx, i)
            # Out from the head centre through the bead onto the surface.
            out = (p - head_c).normalized()
            hit, n = boss.onto(p + out * 0.4, -out)
            n = (n * 0.6 + out * 0.4).normalized()
            # The bead's centre lies on the head surface: the eye stands a little out of it and covers the bead.
            centre = p + n * r * 0.22
            D.add_eye(b, centre, n, r, eye_l[i % 4], sock, squash=(1.0, 1.1, 0.85), socket=0.18,
                      up=Vector((0, 0, 1)) - n * n.z, bone="Head")
        # Compound eye: a big faceted kidney on each side of the head, low and forward, behind the petal roots.
        origin = Vector((sx * 0.9, -0.8, 1.93))
        hit, n = boss.onto(origin, Vector((-sx, 0.12, 0.0)))
        n = (n + Vector((0, -0.35, 0.1))).normalized()

        def kidney(v):
            # Bean shape: the ends bend back, away from the face.
            return Vector((v.x + sx * 0.28 * v.y * v.y, v.y, v.z))
        D.add_compound_eye(b, hit - n * 0.025, n, (0.075, 0.105, 0.06),
                           D.compound_eye_style(COMPOUND, facets=260, glow=0.7, seed=7), sock,
                           up=Vector((0, -0.25, 1.0)), bone="Head", socket=0.14, deform=kidney)
    # Petal teeth: a row of hooked fangs on the inner face of every jaw petal and a row of smaller ones beside
    # it; they point in at the mouth axis and back towards the throat (rake inward as the petals close).
    tooth = [D.tooth_style(TOOTH, glow=0.35, seed=s) for s in range(3)]
    axis = (MOUTH_TIP - THROAT).normalized()
    for name in PETALS:
        crv = curve(petal_points(name))
        for t, scale, across in ((0.22, 1.0, 0.0), (0.34, 1.05, 0.0), (0.46, 0.95, 0.0), (0.58, 0.8, 0.0),
                                 (0.28, 0.6, 1.0), (0.4, 0.62, 1.0), (0.52, 0.55, 1.0)):
            c = crv[int(t * (len(crv) - 1))]
            a = THROAT + axis * (c - THROAT).dot(axis)
            inward = (a - c).normalized()
            # The second row sits a little towards the neighbouring petal across the jaw.
            if across:
                sx = -1 if name[1] == "L" else 1
                side = Vector((-sx, 0, 0))
                side = (side - inward * side.dot(inward)).normalized()
                c = c + side * 0.045
            hit = boss.hit(a + (c - a) * 0.15, c - a)
            if hit is None or hit[1].dot(c - a) > 0 or (hit[0] - c).length > 0.2:
                C.log("petal tooth skipped", name, t)
                continue
            base, n = hit
            d = (n * 0.75 + (-axis) * 0.35).normalized()
            D.add_tooth(b, base - n * 0.012, d, 0.021 * scale, 0.085 * scale, tooth[int(t * 10) % 3],
                        hook=-axis, hook_amount=0.4, bone=f"Petal_{name}", lod=False)
    # The throat: a lamprey ring of small hooked teeth on a dark gum round the glowing plug, so the glow reads as
    # a gullet and not as a small round mouth.
    gum_c = THROAT + axis * 0.07
    gum = D.flesh_style(D.Flesh(dark=(0.05, 0.006, 0.012), flesh=(0.32, 0.04, 0.07), glow=(1.0, 0.34, 0.46)),
                        glow=0.5, glow_axis=(gum_c, -axis, 0.06), seed=5)
    D._tag(b.ring("Gum", gum_c, axis, 0.088, 0.017, arcs=3, sides=8, style=gum, ref=Vector((1, 0, 0)),
                  profile=lambda th: 1.0 + 0.3 * math.cos(6 * th)), gum_c, bone="Head")
    D.add_ring_teeth(b, gum_c + axis * 0.01, -axis, 0.084, 12, 0.08, 0.018, tooth[1], inward=0.62, offset=0.0,
                     bone="Head", lod=False, name="RingTooth")
    return b


# ------------------------------------------------------------------------------------------------------------
# Claws
# ------------------------------------------------------------------------------------------------------------

def claw_parts(boss):
    b = C.PartBuilder()
    claw = [D.keratin_style(CLAW, glow=0.6, glow_from=0.82, seed=s, wet=0.3) for s in range(3)]
    saw = D.tooth_style(TOOTH, glow=0.25, seed=11)
    for side, sx in SIDES:
        for i in (1, 2):
            bone = f"Leg_{side}{i}_Tibia"
            knee, foot = boss.head(bone), boss.tail(bone)
            down = (foot - knee).normalized()
            out = Vector((sx, 0, 0))
            fwd = Vector((0, -1 if i == 1 else 1, 0)) * 0.6 + Vector((0, -0.4, 0))
            fwd = fwd.normalized()
            base = foot - down * 0.24
            # A grappling-hook foot: the main talon on along the leg, two side talons spread fore and aft, all
            # curling down so the points rest just on the ground.
            for k, (spread, ln, rad) in enumerate(((0.0, 0.26, 0.04), (1.0, 0.2, 0.032), (-1.0, 0.18, 0.03))):
                if spread == 0.0:
                    d = (down * 0.6 + out * 0.5 + Vector((0, 0, -0.2))).normalized()
                else:
                    d = (down * 0.3 + fwd * spread * 0.85 + out * 0.35 + Vector((0, 0, 0.12))).normalized()
                D.add_claw(b, base + out * 0.005, d, rad, ln, claw[k], hook=Vector((0, 0, -1)) - d * 0.3,
                           hook_amount=0.55, bone=bone)
        # Scythe: saw teeth down the inner edge, raked towards the point, and a glowing hooked tip.
        crv = curve(blade_points(sx), 60)
        bone = f"Arm_{side}_2"
        for t in (0.3, 0.37, 0.44, 0.51, 0.58, 0.65, 0.72, 0.79):
            j = int(t * (len(crv) - 1))
            c = crv[j]
            tangent = (crv[j + 1] - crv[j - 1]).normalized()
            inward = Vector((-sx, 0, 0))
            inward = (inward - tangent * inward.dot(tangent)).normalized()
            hit = boss.hit(c + inward * 0.6, -inward)
            if hit is None or (hit[0] - c).length > 0.3:
                C.log("scythe tooth skipped", side, t)
                continue
            p, n = hit
            d = (n * 0.8 + tangent * 0.45).normalized()
            D.add_tooth(b, p - n * 0.01, d, 0.02, 0.075, saw, hook=tangent, hook_amount=0.3, bone=bone, lod=False,
                        name="Saw")
        end = crv[-4]
        tangent = (crv[-1] - crv[-6]).normalized()
        D.add_claw(b, end - tangent * 0.06, tangent, 0.045, 0.2, claw[0], hook=Vector((-sx, 0.3, -0.6)),
                   hook_amount=0.5, bone=bone, name="ScytheTip")
    return b


# ------------------------------------------------------------------------------------------------------------
# Run
# ------------------------------------------------------------------------------------------------------------

def build(boss):
    tris_before = boss.tris()
    # The round-2 bead eyes glowed through between the new eyes: their glow goes from the body maps.
    dimmed = D.dim_body(boss, [(old_brow(sx, i), 0.046) for _, sx in SIDES for i in range(5)], emission_keep=0.04,
                        darken=(0.1, 0.015, 0.025), darken_amount=0.5)
    face, face_lod, face_facts = D.build_part(boss, "Face", face_parts(boss), FACE_TRIS, 0, VOXEL,
                                              boost={"Eye": 1.8, "CompoundEye": 1.4, "Tooth": 1.6, "RingTooth": 1.5})
    claws, claws_lod, claw_facts = D.build_part(boss, "Claws", claw_parts(boss), CLAW_TRIS, 0, VOXEL,
                                                boost={"Saw": 1.4, "ScytheTip": 1.3})
    clip_names = D.export(boss, [face, claws], [])
    facts = {"parts": {"Face": face_facts, "Claws": claw_facts}, "body_tris": tris_before[0],
             "extra_tris": face_facts["tris"] + claw_facts["tris"],
             "extra_share": round((face_facts["tris"] + claw_facts["tris"]) / tris_before[0], 3),
             "lod1": "none (the Queen has no LOD1 export)",
             "body_maps": f"old bead eyes' glow removed from Queen_Albedo/Emission ({dimmed} texels this run)", "clips": clip_names,
             "unity": "ExtraParts { \"Face\", \"Claws\" }: objects Queen_Face / Queen_Claws, maps Queen_Face_*.png and "
                      "Queen_Claws_*.png (+ Low/)",
             "note": "round 45: brow of wet eyes, compound eyes, petal teeth, talon feet, scythe saw teeth and tips; "
                     "bones, clips and events unchanged"}
    D.update_report(boss, facts)
    C.save_blend(NAME)
    return facts


def qa(boss, tag):
    D.qa_scene(3.0)
    shots = [
        ("face34", "Idle", 20, "Head", HEAD_FOCUS + Vector((0, 0.05, 0.12)), "close", dict(distance=1.3, azimuth=-26, elevation=26)),
        ("face_side", "Idle", 20, "Head", HEAD_FOCUS + Vector((-0.15, 0.1, 0)), "close", dict(distance=1.2, azimuth=-70, elevation=12)),
        ("mouth", "Scream", 25, "Head", THROAT + Vector((0, -0.05, 0)), "close", dict(distance=0.75, azimuth=-20, elevation=8)),
        ("face_open", "Scream", 25, "Head", HEAD_FOCUS, "close", dict(distance=1.7, azimuth=-14, elevation=4)),
        ("claws34", "Idle", 20, "Arm_L_2", SCYTHE_FOCUS, "close", dict(distance=2.0, azimuth=-60, elevation=16)),
        ("foot", "Walk", 10, "Leg_L1_Tibia", FOOT_FOCUS, "close", dict(distance=1.1, azimuth=-55, elevation=20)),
        ("game_close", "Walk", 10, "Head", HEAD_FOCUS, "game", dict(ortho=2.2)),
        ("game_play", "Walk", 10, "Root", Vector((0, 0, 1.0)), "game", dict(ortho=7.0)),
    ]
    return D.qa_set(boss, tag, shots)


def run(norender=False):
    """build_queen.py hook (and the default run): build, export, report, save, after renders."""
    D.use_gpu()
    D.backup(NAME)
    boss = D.Boss(NAME, PARTS)
    facts = build(boss)
    C.log("queen detail", facts["extra_tris"], "tris", facts["extra_share"])
    if not norender:
        qa(boss, "after")
        D.qa_clips(boss, "after", "Head", HEAD_FOCUS, 1.9)


def main():
    args = C.cli_args()
    if args.get("before"):
        qa(D.Boss(NAME, PARTS), "before")
        return
    if args.get("blockout"):
        boss = D.Boss(NAME, PARTS)
        D.blockout(boss, {"Face": face_parts(boss), "Claws": claw_parts(boss)})
        qa(boss, "blockout")
        return
    if args.get("qa"):
        boss = D.Boss(NAME)
        qa(boss, "after")
        D.qa_clips(boss, "after", "Head", HEAD_FOCUS, 1.9)
        return
    run(bool(args.get("norender")))


if __name__ == "__main__":
    main()
