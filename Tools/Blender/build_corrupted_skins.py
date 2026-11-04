"""Corrupted skin set for The Core (round 15, see ArtSource/DESIGN_NOTES.md): every exported species as a dark
variant with purple glowing Crysium veins, texture-only (same mesh, same UVs: a material swap in Unity).

How it works, per species, without touching the species' build script or its main exports:
1. Opens ArtSource/Blend/<Species>.blend read-only (the file is never saved) and takes the game mesh `<Species>` in its
   rest pose.
2. Bakes the mesh's own object-space position into its UV layout (a cheap emission bake of the game mesh onto itself),
   so every texel knows where it sits on the body. Mirrored UV halves get |x|, so both twins agree.
3. Reads the shipped maps (Exports/<Species>/<Species>_Albedo/_Emission/_Normal/_Mask.png) and recolours them:
   Albedo to near-black violet-grey (value kept, so AO, plates and pores survive) plus a 3D network of purple Crysium
   veins (continuous across UV seams because it is computed from positions); Emission: the species' own glow pattern
   turned violet, plus the veins with magenta-white cores; Mask: smoother on the veins; Normal: copied.
4. Writes Exports/<Species>/Skins/Corrupted/<Species>_Corrupted_Albedo/_Normal/_Emission/_Mask.png and the 512 set in
   Low/, and renders a close-up per species for one contact sheet (corrupted) and a pair sheet (shipped | corrupted).

Run: blender --background --factory-startup --python Tools/Blender/build_corrupted_skins.py -- [--preview <dir>]
     [--only Skitter,Oozer] [--bosses] [--norender]
"""

import json
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402

SPECIES = ["Skitter", "Roller", "Cragback", "Prowler", "Skyray", "Shalebat", "Wisp", "Stinger", "Bat", "Bulwark", "Oozer",
           "Shardback", "Mireleech", "Knitback", "Slagstrider", "Siftjaw", "Surgefin"]
# One-material level bosses (the Gravemaw has two materials and is left out).
BOSSES = ["Queen", "BroodMother", "PackAlpha", "StormMatriarch", "CoreWisp", "AcidEmpress", "CrystalWarlord",
          "SkySovereign", "SlimeMonarch", "RollerKing"]
POSE_CLIPS = ("Idle", "Glide", "Float", "Fly", "Walk", "Crawl")

# The corrupted palette: violet-grey body, purple Crysium, magenta-white vein cores (apart from the pink Crysium of
# Crystal Plains and from our cool cyan).
BODY = np.array((0.56, 0.48, 0.78))
VEIN = np.array((0.42, 0.1, 0.72))
VEIN_CORE = np.array((0.92, 0.7, 1.0))
GLOW = np.array((0.62, 0.2, 1.0))
GLOW_CORE = np.array((1.0, 0.72, 1.0))
SIZE = 1024
LOW = 512


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def load_species(name):
    path = os.path.join(C.BLEND, name + ".blend")
    bpy.ops.wm.open_mainfile(filepath=path, load_ui=False)
    mesh = bpy.data.objects.get(name)
    if mesh is None or mesh.type != "MESH":
        raise RuntimeError(f"{path}: no game mesh named {name}")
    if len(mesh.data.materials) != 1:
        raise RuntimeError(f"{name}: {len(mesh.data.materials)} materials, the wrapper handles one")
    arm = mesh.parent if mesh.parent is not None and mesh.parent.type == "ARMATURE" else None
    return mesh, arm


def bake_positions(mesh, arm):
    """Object-space rest positions per texel (H x W x 3) and a coverage mask (texels the bake wrote)."""
    if arm is not None:
        arm.data.pose_position = "REST"
    bpy.context.view_layer.update()
    co = np.empty(len(mesh.data.vertices) * 3)
    mesh.data.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    lo, hi = co.min(axis=0) - 1e-3, co.max(axis=0) + 1e-3
    mat = bpy.data.materials.new("PositionBake")
    mat.use_nodes = True
    nt = mat.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.vector_type = "POINT"
    span = hi - lo
    mapping.inputs["Location"].default_value = tuple(-lo / span)
    mapping.inputs["Scale"].default_value = tuple(1.0 / span)
    nt.links.new(coord.outputs["Object"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], emit.inputs["Color"])
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    image = bpy.data.images.new("PositionBake", SIZE, SIZE, alpha=True, float_buffer=True)
    image.generated_color = (0, 0, 0, 0)
    image.colorspace_settings.name = "Non-Color"
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    nt.nodes.active = tex
    saved = list(mesh.data.materials)
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    bake = scene.render.bake
    bake.use_selected_to_active = False
    bake.target = "IMAGE_TEXTURES"
    bake.margin = 6
    bake.use_clear = True
    for o in scene.objects:
        o.select_set(False)
    mesh.hide_render = False
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.bake(type="EMIT")
    px = np.array(image.pixels[:], dtype=np.float32).reshape(SIZE, SIZE, 4)
    mesh.data.materials.clear()
    for m in saved:
        mesh.data.materials.append(m)
    bpy.data.images.remove(image)
    if arm is not None:
        arm.data.pose_position = "POSE"
    pos = lo + px[..., :3] * span
    covered = px[..., 3] > 0.5
    return pos, covered, float(np.linalg.norm(span))


def veins(pos, covered, size):
    """Vein strength and core (H x W) from 3D positions: lines that come and go, a coarse network and fine twigs."""
    vein = np.zeros(pos.shape[:2], dtype=np.float32)
    core = np.zeros_like(vein)
    p = pos[covered].astype(np.float64)
    p[:, 0] = np.abs(p[:, 0])  # mirrored halves share texels: make the pattern symmetric
    k = 1.0 / max(size, 1e-3)
    n1 = C.value_noise(p, 9.0 * k, 501)
    gate1 = smooth(-0.3, 0.2, C.value_noise(p, 3.5 * k, 502))
    n2 = C.value_noise(p, 24.0 * k, 503)
    gate2 = smooth(0.05, 0.45, C.value_noise(p, 5.0 * k, 504))
    line1 = (1 - smooth(0.0, 0.09, np.abs(n1))) * gate1
    line2 = (1 - smooth(0.0, 0.05, np.abs(n2))) * gate2 * 0.7
    vein[covered] = np.maximum(line1, line2)
    core[covered] = (1 - smooth(0.0, 0.025, np.abs(n1))) * gate1
    return vein, core


def corrupt(name, vein, core, out_dir):
    src = os.path.join(C.EXPORTS, name)
    maps = {key: C.image_to_array(os.path.join(src, f"{name}_{key}.png")) for key in ("Albedo", "Emission", "Mask", "Normal")}
    if maps["Albedo"].shape[0] != SIZE:
        raise RuntimeError(f"{name}: maps are {maps['Albedo'].shape[0]}, expected {SIZE}")
    v = vein[..., None]
    c = core[..., None]
    alb = maps["Albedo"].copy()
    lum = (alb[..., :3] @ np.array((0.3, 0.59, 0.11)))[..., None]
    body = np.clip(lum, 0, 1) ** 1.15 * 0.62 * BODY + 0.012
    body = body + (alb[..., :3] - lum) * 0.06  # a trace of the species' own hue
    body = body * (1 - v * 0.85) + VEIN * v * 0.85
    body = body * (1 - c * 0.6) + VEIN_CORE * c * 0.6
    alb[..., :3] = np.clip(body, 0, 1)
    emi = maps["Emission"].copy()
    e = emi[..., :3].max(axis=2)[..., None]
    glow = e * GLOW + (smooth(0.75, 1.0, e) * 0.35) * GLOW_CORE
    glow = np.maximum(glow, GLOW * v * 0.85)
    glow = np.maximum(glow, GLOW_CORE * c)
    emi[..., :3] = np.clip(glow, 0, 1)
    mask = maps["Mask"].copy()
    mask[..., 1] = np.clip(mask[..., 1] + vein * 0.35, 0, 1)
    mask[..., 2] = mask[..., 2] * (1 - vein * 0.8)  # no chitin detail normal on the glassy veins
    out = {"Albedo": (alb, "sRGB"), "Emission": (emi, "sRGB"), "Mask": (mask, "Non-Color"), "Normal": (maps["Normal"], "Non-Color")}
    os.makedirs(os.path.join(out_dir, "Low"), exist_ok=True)
    paths = {}
    for key, (arr, space) in out.items():
        path = os.path.join(out_dir, f"{name}_Corrupted_{key}.png")
        if key == "Normal":
            shutil.copyfile(os.path.join(src, f"{name}_Normal.png"), path)
            low_src = os.path.join(src, "Low", f"{name}_Normal.png")
            if os.path.exists(low_src):
                shutil.copyfile(low_src, os.path.join(out_dir, "Low", f"{name}_Corrupted_Normal.png"))
        else:
            C.array_to_image(arr, path, colorspace=space)
            C.array_to_image(C.downsample(arr, SIZE // LOW), os.path.join(out_dir, "Low", f"{name}_Corrupted_{key}.png"),
                             colorspace=space)
        paths[key.lower()] = path
    return paths


def pose_for_render(arm):
    if arm is None:
        return
    for clip in POSE_CLIPS:
        act = bpy.data.actions.get(clip)
        if act is not None:
            C.set_action(arm, act, 15)
            return


def render_pair(name, mesh, arm, textures, out):
    """Close-ups of the shipped skin and the corrupted one from the same camera."""
    for o in bpy.context.scene.objects:
        o.hide_render = o is not mesh
    mesh.hide_render = False
    pose_for_render(arm)
    bpy.context.view_layer.update()
    C.preview_world()
    C.light_rig(scale=1.0)
    C.ground()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    ev = mesh.evaluated_get(depsgraph)
    pts = np.array([tuple(mesh.matrix_world @ v.co) for v in ev.data.vertices])
    lo, hi = pts.min(axis=0), pts.max(axis=0)
    target = Vector(((lo + hi) * 0.5).tolist())
    size = float(np.linalg.norm(hi - lo))
    cam = C.closeup_camera(target, size * 1.55, azimuth=-38, elevation=26, lens=55)
    shipped = C.render(cam, os.path.join(out, f"corrupted_{name.lower()}_shipped.png"), 960, 540)
    mat = C.game_material(name + "_Corrupted_Mat", textures, 2.5, 0.35, ())
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)
    corrupted = C.render(cam, os.path.join(out, f"corrupted_{name.lower()}.png"), 960, 540)
    return shipped, corrupted


def main():
    args = C.cli_args()
    names = list(SPECIES) + (list(BOSSES) if args.get("bosses") else [])
    if args.get("only"):
        names = [n for n in args["only"].split(",") if n]
    out = C.preview_dir()
    renders, pairs, report = [], [], {}
    for name in names:
        if not os.path.exists(os.path.join(C.BLEND, name + ".blend")) or not os.path.exists(
                os.path.join(C.EXPORTS, name, f"{name}_Albedo.png")):
            C.log("skip (not exported)", name)
            continue
        mesh, arm = load_species(name)
        pos, covered, size = bake_positions(mesh, arm)
        vein, core = veins(pos, covered, size)
        out_dir = os.path.join(C.EXPORTS, name, "Skins", "Corrupted")
        textures = corrupt(name, vein, core, out_dir)
        report[name] = {"folder": os.path.relpath(out_dir, C.REPO).replace("\\", "/"),
                        "vein_coverage": round(float(vein[covered].mean()), 3)}
        C.log("corrupted", name, report[name])
        if not args.get("norender"):
            shipped, corrupted = render_pair(name, mesh, arm, textures, out)
            renders.append(corrupted)
            pairs += [shipped, corrupted]
    with open(os.path.join(C.EXPORTS, "Corrupted_skins_report.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    # Never save: reopen an empty scene so nothing writes back into a species .blend.
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if renders:
        C.contact_sheet(renders, os.path.join(out, "sheet_corrupted.png"), 4, 480)
        C.contact_sheet(pairs, os.path.join(out, "sheet_corrupted_pairs.png"), 4, 480)


if __name__ == "__main__":
    main()
