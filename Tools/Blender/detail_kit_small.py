"""Round 46: shared helpers for the close-up detail of the small (non-boss) enemies (Remco, after the boss pass: "do the
same for the small enemies": eyes, teeth/mandibles and claws that look cool and dangerous up close).

It builds on detail_kit.py (round 45, agent A; imported read-only) and adds what the small species need:
- Round-46 bookkeeping: backup to ArtSource/Exports/_backup_r46/<Name>/, the report block "r46_detail", the body-map
  edit flag "r46_body_maps_edited", QA renders as ArtSource/Previews/QA/r46_<name>_*.png.
- `organic_eye`: a wet lens sunk under a fleshy brow fold and a lower lid (the boss review's lesson: eye sockets are
  organic, the skin overlaps the rim; never a torus ring that reads as a button or goggles).
- `skin_style`: lids, brows and gums in the species' own chitin colours (the lid reads as body, not as a part).
- Tight budgets: small enemies are many on screen, so the parts get at most +15% of the body's triangles (LOD0) and
  +10% (LOD1); `budget(creature)` returns both.
- QA: close-ups, the game camera at the species' in-game size and a clip sheet, all with the extra parts.

The parts are exported in <Name>.fbx / <Name>_LOD1.fbx next to the body (detail_kit.export: the same rigs, bones,
clips, timings and events) and dressed in Unity as BlenderArtEntry.ExtraParts { "Face", "Claws" }.
"""

import json
import math
import os
import shutil

import bpy
import numpy as np
from mathutils import Matrix, Vector

import common as C
import detail_kit as D

ROUND = 46
QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")
BACKUP = os.path.join(C.EXPORTS, "_backup_r46")
REPORT_KEY = "r46_detail"
DIM_KEY = "r46_body_maps_edited"
LOD0_SHARE, LOD1_SHARE = 0.15, 0.10


# ---------------------------------------------------------------------------------------------------------------
# Creature, budget, backup
# ---------------------------------------------------------------------------------------------------------------

class Creature(D.Boss):
    """detail_kit.Boss for a small species (same .blend layout: <Name>_Rig, <Name>, <Name>_LOD1_Rig, <Name>_LOD1)."""

    def __init__(self, name, part_names=()):
        super().__init__(name, part_names)
        rep = report(name)
        self.scale = unity_scale(name, rep)


def report(name):
    with open(os.path.join(C.EXPORTS, name, name + "_report.json"), encoding="utf-8") as fh:
        return json.load(fh)


def unity_scale(name, rep=None):
    """The model's in-game scale: the gait report's unity_model_scale, else the EnemyDefinition's _scale."""
    rep = rep or report(name)
    for v in rep.values():
        if isinstance(v, dict) and "unity_model_scale" in v:
            return float(v["unity_model_scale"])
    asset = os.path.join(C.REPO, "Assets", "_Project", "Data", "Enemies", name + ".asset")
    if os.path.exists(asset):
        with open(asset, encoding="utf-8") as fh:
            for line in fh:
                if line.strip().startswith("_scale:"):
                    return float(line.split(":")[1])
    return 0.5


def budget(creature):
    """(LOD0, LOD1) triangles both parts together may add."""
    body, lod = creature.tris()
    return int(body * LOD0_SHARE), int(lod * LOD1_SHARE)


def backup(name):
    """A copy of the species' current export and .blend (before round 46) in _backup_r46/<name>; never overwritten."""
    dst = os.path.join(BACKUP, name)
    if os.path.isdir(dst):
        C.log("backup exists", dst)
        return dst
    shutil.copytree(os.path.join(C.EXPORTS, name), dst)
    blend = os.path.join(C.BLEND, name + ".blend")
    if os.path.exists(blend):
        shutil.copy2(blend, os.path.join(dst, name + "_before_r46.blend"))
    C.log("backup", dst)
    return dst


# ---------------------------------------------------------------------------------------------------------------
# Styles
# ---------------------------------------------------------------------------------------------------------------

def skin_style(dark, skin, wet=(0.2, 0.05, 0.05), seed=0, rough=0.42):
    """Lids, brows, lip folds: the species' chitin/skin colour with fine wrinkles, darkening into a wet crease where
    the fold meets the lens (local -z side and the inner rim; the baked AO deepens it)."""
    dark, skin, wet = map(np.array, (dark, skin, wet))

    def style(s, idx, part):
        p = s.p[idx]
        wr = 0.5 + 0.5 * np.sin(C.value_noise(p, 140, seed) * 8)
        mott = 0.5 + 0.5 * C.value_noise(p, 50, seed + 1)
        u = D._unit_local(part, p)
        crease = C.smoothstep(0.1, -0.6, u[:, 2])
        col = C.lerp3(dark, skin, 0.3 + 0.55 * mott) * (0.82 + 0.28 * wr)[:, None]
        col = C.lerp3(col, wet, crease * 0.7)
        s.albedo[idx] = np.clip(col, 0, 1)
        s.emit[idx] = 0.0
        s.rough[idx] = rough + 0.18 * wr - 0.22 * crease
        s.disp[idx] = 0.0005 * wr
        s.fam[idx] = (0.7, 0.0, 0.0, 0.0)
    return style


# ---------------------------------------------------------------------------------------------------------------
# Shapes
# ---------------------------------------------------------------------------------------------------------------

def organic_eye(b, centre, normal, radius, style, lid_style, up=Vector((0, 0, 1)), bone=None, anchor=None, lod=True,
                squash=(1.0, 0.8, 0.7), brow=(1.25, 0.62, 0.55), brow_lift=0.5, brow_tilt=0.5, lower=(1.1, 0.4, 0.4),
                lower_drop=0.62, segments=(20, 10), lid_segments=(16, 8), name="Eye"):
    """A wet lens (ellipsoid; local +z the look, +y `up`) under a heavy brow fold that overlaps the top of its rim and
    a thinner lower lid that overlaps the bottom: an almond opening that sinks the eye into the skin, so it reads as
    an eye in a living head (the round-45 curator note: no torus rings, no goggles). `brow`/`lower` are the folds'
    radii in units of the lens radius; `brow_lift`/`lower_drop` how far up/down the lens they sit; `brow_tilt`
    pitches the brow forward over the lens. Pass lower=None for a brow only."""
    rot = D.frame_rot(normal, up)
    x, y, z = (Vector(rot.col[i]) for i in range(3))
    r = (radius * squash[0], radius * squash[1], radius * squash[2])
    c = Vector(centre)
    parts = [b.ellipsoid(name, c, r, rot, style=style, segments=segments)]
    if brow is not None:
        bc = c + y * r[1] * brow_lift * 1.35 + z * r[2] * 0.05
        brot = rot @ Matrix.Rotation(-brow_tilt, 3, "X")
        parts.append(b.ellipsoid(name + "Brow", bc, (r[0] * brow[0], r[1] * brow[1] * 1.2, r[2] * brow[2] * 1.4),
                                 brot, style=lid_style, segments=lid_segments))
    if lower is not None:
        lc = c - y * r[1] * lower_drop * 1.35 - z * r[2] * 0.1
        lrot = rot @ Matrix.Rotation(0.35, 3, "X")
        parts.append(b.ellipsoid(name + "Lid", lc, (r[0] * lower[0], r[1] * lower[1] * 1.2, r[2] * lower[2] * 1.4),
                                 lrot, style=lid_style, segments=lid_segments))
    return D._tag(parts, anchor or centre, bone, lod)


def mirrored(fn):
    """fn(sx) for the right (+1) and left (-1) side: the parts stay symmetric (mirrored UV unwrap)."""
    for sx in (1, -1):
        fn(sx)


def flip(v, sx):
    v = Vector(v)
    return Vector((v.x * sx, v.y, v.z))


def side_name(bone, sx, left="L", right="R"):
    """Bone names with an _L/_R or L1/R1 side: the right-hand name for sx > 0, the left-hand one otherwise."""
    if bone is None:
        return None
    return bone.replace(f"_{right}", f"_{left}") if sx < 0 else bone


# ---------------------------------------------------------------------------------------------------------------
# Build, export, report
# ---------------------------------------------------------------------------------------------------------------

def build_all(creature, specs, voxel, note, body_maps=None):
    """specs: {part: (builder, tris, lod_tris, boost)}. Bakes, skins and LODs every part (detail_kit.build_part),
    exports both FBX files with the existing clips, writes the r46 report block and saves the .blend."""
    body, body_lod = creature.tris()
    objs, lods, parts = [], [], {}
    for part, (builder, tris, lod_tris, boost) in specs.items():
        obj, lod, facts = D.build_part(creature, part, builder, tris, lod_tris, voxel, boost=boost)
        objs.append(obj)
        lods.append(lod)
        parts[part] = facts
    clip_names = D.export(creature, objs, lods)
    extra = sum(f["tris"] for f in parts.values())
    extra_lod = sum(f["lod1_tris"] for f in parts.values())
    names = ", ".join(f'"{p}"' for p in specs)
    facts = {"parts": parts, "body_tris": body, "extra_tris": extra, "extra_share": round(extra / body, 3),
             "body_lod1_tris": body_lod, "extra_lod1_tris": extra_lod,
             "extra_lod1_share": round(extra_lod / body_lod, 3) if body_lod else None, "clips": clip_names,
             "unity": f"ExtraParts {{ {names} }}: objects {creature.name}_<Part> (+ _LOD1 in {creature.name}_LOD1.fbx), "
                      f"maps {creature.name}_<Part>_*.png (+ Low/)",
             "body_maps": body_maps or "unchanged", "note": note}
    path = os.path.join(creature.folder, creature.name + "_report.json")
    rep = report(creature.name)
    rep[REPORT_KEY] = facts
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.save_blend(creature.name)
    C.log(creature.name, "r46 detail", extra, "tris", facts["extra_share"], "lod1", extra_lod,
          facts["extra_lod1_share"])
    return facts


def dim_body(creature, spheres, emission_keep=0.08, darken=None, darken_amount=0.6):
    """detail_kit.dim_body with the round-46 flag (once per body bake)."""
    old = D.DIM_KEY
    D.DIM_KEY = DIM_KEY
    try:
        return D.dim_body(creature, spheres, emission_keep, darken, darken_amount)
    finally:
        D.DIM_KEY = old


# ---------------------------------------------------------------------------------------------------------------
# QA
# ---------------------------------------------------------------------------------------------------------------

def _path(creature, tag, label=None):
    low = creature.name.lower()
    return os.path.join(QA, f"r{ROUND}_{low}_{tag}" + (f"_{label}" if label else "") + ".png")


def scene(creature, scale=1.0, ground_z=0.0):
    """Preview world, light rig and floor (`ground_z`: a flyer's floor lies below its body)."""
    D.qa_scene(scale, ground_z)


def shot(creature, act, frame, bone, focus, path, kind, distance=1.0, azimuth=-38, elevation=14, zoom=1.0, lens=55,
         size=(1280, 720)):
    """'close': perspective round the posed focus. 'game': the play camera (orthographic, 44 degrees) at the
    species' in-game size: zoom 1 is the default play zoom (about 51 px per unit at 720p)."""
    if kind == "game":
        ortho = size[0] / C.GAME_PIXELS_PER_UNIT / zoom / creature.scale
        return D.shot(creature.arm, act, frame, bone, focus, path, "game", ortho=ortho, size=size)
    return D.shot(creature.arm, act, frame, bone, focus, path, "close", distance=distance, azimuth=azimuth,
                  elevation=elevation, lens=lens, size=size)


def qa_set(creature, tag, shots, cols=2):
    """[(label, act, frame, bone, focus, kind, kwargs)] -> r46_<name>_<tag>_<label>.png and the sheet r46_<name>_<tag>.png."""
    os.makedirs(QA, exist_ok=True)
    paths = [shot(creature, act, frame, bone, focus, _path(creature, tag, label), kind, **kw)
             for label, act, frame, bone, focus, kind, kw in shots]
    C.contact_sheet(paths, _path(creature, tag), cols, 640)
    for p in paths:
        os.remove(p)
    return _path(creature, tag)


def qa_clips(creature, tag, bone, focus, distance, azimuth=-38, elevation=14, frames_per_clip=2):
    """Weight check: the head at two frames of every clip (the parts must follow), one sheet r46_<name>_<tag>_clips."""
    full, _ = D.clips(creature)
    paths = []
    for clip, act in full.items():
        a, b = int(act.frame_range[0]), int(act.frame_range[1])
        for i in range(frames_per_clip):
            f = a + (b - a) * (i + 1) // (frames_per_clip + 1)
            p = _path(creature, f"_{tag}_clip_{clip}_{f}")
            paths.append(shot(creature, clip, f, bone, focus, p, "close", distance=distance, azimuth=azimuth,
                              elevation=elevation, size=(640, 360)))
    sheet = _path(creature, tag, "clips")
    C.contact_sheet(paths, sheet, 4, 480)
    for p in paths:
        os.remove(p)
    return sheet


def qa_lod(creature, tag, bone, focus, distance, azimuth=-38, elevation=14):
    """The LOD1 rig with its LOD1 parts at the face close-up (r46_<name>_<tag>_lod1.png)."""
    if creature.lod_arm is None:
        return None
    full = [creature.arm] + [o for o in bpy.data.objects if o.parent == creature.arm and o.type == "MESH"]
    lod = [creature.lod_arm] + [o for o in bpy.data.objects if o.parent == creature.lod_arm and o.type == "MESH"]
    for o in full:
        o.hide_render = True
    for o in lod:
        o.hide_render = False
    _, lods = D.clips(creature)
    act = "Idle" if "Idle" in lods else next(iter(lods))
    b = bone if bone in creature.lod_arm.data.bones else D.lod_remap(creature).get(bone, bone)
    path = D.shot(creature.lod_arm, lods[act].name, 10, b, focus, _path(creature, tag, "lod1"), "close",
                  distance=distance, azimuth=azimuth, elevation=elevation)
    for o in full:
        o.hide_render = False
    for o in lod:
        o.hide_render = True
    return path


def blockout(creature, builders):
    """Raw parts in flat colours, skinned from the body; nothing baked or saved."""
    D.blockout(creature, builders)


def standard_main(name, parts, build_fn, shots_fn, clip_focus, scale=1.0, ground_z=0.0):
    """CLI of a small-enemy detail script: [--before | --blockout | --qa] [--norender].
    build_fn(creature) bakes/exports (calls build_all) or, with blockout_only=True, returns {part: builder};
    shots_fn(creature) -> the QA shot list; clip_focus = (bone, point, distance)."""
    args = C.cli_args()
    if args.get("before"):
        c = Creature(name, parts)
        scene(c, scale, ground_z)
        qa_set(c, "before", shots_fn(c))
        return
    if args.get("blockout"):
        c = Creature(name, parts)
        blockout(c, build_fn(c, blockout_only=True))
        scene(c, scale, ground_z)
        qa_set(c, "blockout", shots_fn(c))
        return
    if args.get("qa"):
        c = Creature(name)
        render_after(c, shots_fn, clip_focus, scale, ground_z)
        return
    run(name, parts, build_fn, shots_fn, clip_focus, scale, bool(args.get("norender")), ground_z)


def render_after(c, shots_fn, clip_focus, scale=1.0, ground_z=0.0):
    scene(c, scale, ground_z)
    qa_set(c, "after", shots_fn(c))
    qa_clips(c, "after", *clip_focus)
    qa_lod(c, "after", *clip_focus)


def run(name, parts, build_fn, shots_fn, clip_focus, scale=1.0, norender=False, ground_z=0.0):
    """Build-script hook and default run: backup, build, export, report, save, after renders."""
    D.use_gpu()
    backup(name)
    c = Creature(name, parts)
    build_fn(c)
    if not norender:
        render_after(c, shots_fn, clip_focus, scale, ground_z)


def standard_shots(face_bone, face, claw_bone, claw, foot_bone, foot, size, walk="Walk", idle="Idle", frame=15,
                   face_dist=None, claw_dist=None, foot_dist=None, root_focus=None):
    """The round-46 sheet: 3/4 face, a macro of the eyes, front, side, claws, foot, game camera close (zoom 3) and play zoom (zoom 1).
    `size` is the creature's length in metres (scales the camera distances). The play shot is 640 wide at twice the
    default zoom (real pixels), the game close-up the same camera zoomed in 10x. Claw and foot focus on the left
    side (x < 0), which the cameras see."""
    fd = face_dist or size * 0.75
    cd = claw_dist or size * 0.9
    td = foot_dist or size * 0.7
    root = root_focus or Vector((0, 0, size * 0.25))
    return [
        ("face34", idle, frame, face_bone, face, "close", dict(distance=fd, azimuth=-26, elevation=16)),
        ("face_macro", idle, frame, face_bone, face, "close", dict(distance=fd * 0.32, azimuth=-42, elevation=6)),
        ("front", idle, frame, face_bone, face, "close", dict(distance=fd * 1.1, azimuth=-4, elevation=12)),
        ("side", idle, frame, face_bone, face, "close", dict(distance=fd, azimuth=-75, elevation=14)),
        ("claws", idle, frame, claw_bone, claw, "close", dict(distance=cd, azimuth=-50, elevation=14)),
        ("foot", walk, 6, foot_bone, foot, "close", dict(distance=td, azimuth=-60, elevation=18)),
        ("game_close", walk, 6, "Root", root, "game", dict(zoom=10.0)),
        ("game_play", walk, 6, "Root", root, "game", dict(zoom=2.0, size=(640, 360))),
    ]
