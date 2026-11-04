"""Round 25 review scene: the markers, bars, cues and spawn gates under the game camera, next to a real tower and
walking Skitters, so they can be judged for subtlety and colour code (cool tech vs warm alien).

Called by build_markers.py (needs its meshes and textures); appends SpawnGate_Ground/Air from Blend/SpawnGate.blend
and the bolt from Blend/Projectiles.blend when they exist (read-only).
"""

import math
import os

import bpy
from mathutils import Vector

import base_kit as bk
import common as C
import marker_kit as M
import tower_kit as tk

COOL = (0.35, 0.75, 1.0)
WARM = (1.0, 0.42, 0.14)
VALID = (0.3, 1.0, 0.8)
HERO = (1.0, 0.8, 0.35)
SLOW = (0.45, 0.6, 1.0)
PITCH, YAW = tk.GAME_PITCH, tk.GAME_YAW


def _append(blend, names):
    path = os.path.join(C.BLEND, blend + ".blend")
    if not os.path.exists(path):
        return {}
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in names]
    out = {}
    for o in dst.objects:
        if o is not None:
            C.link(o)
            out[o.name] = o
    return out


def _mat(textures, name, recipe, mode="add", strength=1.0, uv="UVMap"):
    arr = textures[name][1]
    return M.fx_material("Scene_" + name + str(len(bpy.data.materials)), M.preview_image("SceneImg_" + name, M.tinted(arr, recipe)),
                         mode, strength, uv)


def _bar_material(textures, tint, fill, alpha_gain=1.0, name="Bar"):
    """Preview of the bar shader: the atlas row tinted, clipped where UV2.x > fill."""
    arr = textures["UI_HealthBar"][1]
    img = M.preview_image("SceneBar", M.tinted(arr, [(1, tint, 1.0), (0, (1, 1, 1), 0.35), ("alpha", 3, alpha_gain)]))
    mat = M.fx_material(name, img, "alpha", 1.0)
    nt = mat.node_tree
    mix = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeMixShader")
    tex = next(n for n in nt.nodes if n.bl_idname == "ShaderNodeTexImage")
    uv2 = nt.nodes.new("ShaderNodeUVMap")
    uv2.uv_map = "UV2"
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(uv2.outputs["UV"], sep.inputs[0])
    lt = nt.nodes.new("ShaderNodeMath")
    lt.operation = "LESS_THAN"
    lt.inputs[1].default_value = fill
    nt.links.new(sep.outputs["X"], lt.inputs[0])
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(lt.outputs[0], mul.inputs[1])
    nt.links.new(mul.outputs[0], mix.inputs["Fac"])
    return mat


def _copy(obj, loc, mat=None, rot=None, scale=None, placed=None):
    o = obj.copy()
    if obj.data is not None:
        o.data = obj.data.copy()
    C.link(o)
    o.hide_render = False
    o.location = loc
    if rot is not None:
        o.rotation_euler = rot
    if scale is not None:
        o.scale = scale
    if mat is not None:
        M.assign(o, mat)
    if placed is not None:
        placed.append(o)
    return o


def _facing():
    # Upright quads face Blender +Y; turn them to face the game camera (pitch about X, then yaw about Z).
    return (math.radians(PITCH), 0.0, -math.radians(YAW))


def render_scene(meshes, textures, out):
    for group in meshes.values():
        for o in group:
            o.hide_render = True
    C.preview_world()
    placed = []
    stage = bk.road_stage(2.0, 40, axis="x")

    # Spawn gates at the road start (Blender +X is screen-left under the game camera).
    gates = _append("SpawnGate", ["SpawnGate_Ground", "SpawnGate_Ground_Rig", "SpawnGate_Air", "SpawnGate_Air_Rig"])
    if "SpawnGate_Ground_Rig" in gates:
        rig = gates["SpawnGate_Ground_Rig"]
        rig.location = (5.2, 0, 0)
        rig.scale = (1.2, 1.2, 1.2)
        placed += [rig, gates["SpawnGate_Ground"]]
    if "SpawnGate_Air_Rig" in gates:
        rig = gates["SpawnGate_Air_Rig"]
        rig.location = (4.6, 2.6, 1.6)
        rig.scale = (0.96, 0.96, 0.96)
        placed += [rig, gates["SpawnGate_Air"]]
        _copy(meshes["quads"][0], (4.6, 2.6, 0.03), _mat(textures, "Decal_BlobShadow", [("alpha", 3, 0.6), (0, (0, 0, 0), 0.0)], "alpha"),
              scale=(1.4, 1.4, 1), placed=placed)
    _copy(meshes["quads"][0], (5.2, 0, 0.02), _mat(textures, "Decal_GlowPool", [(0, WARM, 0.5), (1, WARM, 0.15)]),
          scale=(3.2, 3.2, 1), placed=placed)

    # Road cue: strips end to end along the road centre from the gate towards the base (travel = -X here).
    strip = meshes["cues"][0]
    trail = _mat(textures, "Cue_Trail", [(0, WARM, 0.5), (1, WARM, 0.25), (2, (1, 0.8, 0.5), 0.25)])
    for i in range(16):
        x = 4.0 - i * 1.0
        o = _copy(strip, (x, 0, 0.012), trail, rot=(0, 0, -math.pi / 2), placed=placed)
        # Cell index along the road shifts U by a third (in Unity: a property block or world-space U).
        for d in o.data.uv_layers["UVMap"].data:
            d.uv = (d.uv[0] + i / 3.0, d.uv[1])
    # Flight route: beads from the air gate across the field.
    fstrip = meshes["cues"][1]
    dash = _mat(textures, "Cue_Dash", [(0, (0.55, 0.95, 0.95), 0.6), (1, (0.55, 0.95, 0.95), 0.3)])
    for i in range(8):
        x = 3.6 - i * 1.0
        _copy(fstrip, (x, 2.6 - i * 0.12, 0.12), dash, rot=(0, 0, -math.pi / 2 + 0.12), placed=placed)

    # A tower beside the road, its range ring and a placement cursor next to it (the player is placing).
    placed += bk.tower("Gun", "T1", (0.5, 2.0, 0), 0)
    cursor = meshes["cursor"][0]
    _copy(cursor, (-1.5, 2.0, 0.02), _mat(textures, "Marker_PlacementCursor", [(0, VALID, 0.7), (1, VALID, 0.25), (2, VALID, 0.3)]),
          placed=placed)
    free = _mat(textures, "Marker_FreeCell", [(0, VALID, 0.22), (1, VALID, 0.22)])
    for dx, dy, fade in ((-1, 0, 1.0), (1, 0, 0.8), (0, 1, 0.8), (-1, 1, 0.5), (1, 1, 0.5), (0, -1, 0.5)):
        if (dx, dy) == (1, 0):
            continue   # the tower stands there
        o = _copy(meshes["cursor"][1], (-1.5 + dx * 1.0, 2.0 + dy * 1.0, 0.015), free, scale=(fade, fade, 1), placed=placed)
    band = meshes["range"][0]
    _copy(band, (-1.5, 2.0, 0.03), _mat(textures, "Marker_RangeRing", [(0, VALID, 0.55), (1, VALID, 0.25), (2, VALID, 0.2)]),
          scale=(3.0, 3.0, 1), placed=placed)
    _copy(meshes["range"][1], (-1.5, 2.0, 0.025), _mat(textures, "Marker_RangeFill", [(0, VALID, 0.25), (1, VALID, 0.25)]),
          scale=(3.0, 3.0, 1), placed=placed)

    # Skitters on the road, two of them hurt and showing their bars.
    sk = bk.skitters([(2.2, -0.3, 0), (1.0, 0.35, 0), (-0.4, -0.2, 0), (-2.0, 0.3, 0)], yaw=math.radians(-90))
    plate_mat = _bar_material(textures, (0.02, 0.02, 0.04), 1.0, 0.5, "BarPlate")
    for (x, y), health, armour in (((1.0, 0.35), 0.55, 0.0), ((-2.0, 0.3), 0.3, 0.6)):
        base = Vector((x, y, 0.62))
        _copy(meshes["bar"][0], base, plate_mat, rot=_facing(), placed=placed)
        _copy(meshes["bar"][1], base, _bar_material(textures, (0.4, 1.0, 0.4), health, 1.0, "BarFill"), rot=_facing(), placed=placed)
        if armour:
            _copy(meshes["bar"][2], base, _bar_material(textures, (0.7, 0.85, 1.0), armour, 1.0, "BarArmour"), rot=_facing(),
                  placed=placed)

    # Hero post on the road: the spot strip across the road, the stand ring, beacon and emblem.
    spot = meshes["spot"][0]
    _copy(spot, (-4.2, 0, 0.013), _mat(textures, "Marker_HeroSpot", [(0, HERO, 0.6), (1, HERO, 0.15), (2, HERO, 0.5)]),
          scale=(0.8, 1.8, 1), placed=placed)
    ring, beacon, emblem = meshes["stand"]
    _copy(ring, (-3.0, -0.5, 0.02), _mat(textures, "Marker_HeroRing", [(0, HERO, 1.0), (1, HERO, 0.3), (2, (1, 1, 1), 0.4)]),
          placed=placed)
    _copy(beacon, (-3.0, -0.5, 1.7), _mat(textures, "Marker_Beacon", [(0, HERO, 0.6), (1, HERO, 0.25), (2, HERO, 0.15)]),
          placed=placed)
    em = M.fx_material("EmblemMat", M.preview_image("EmblemImg", M.tinted(textures["Marker_Emblem"][1], [(0, HERO, 2.0), (1, (1, 1, 1), 1.0)])),
                       "alpha", 1.0)
    _copy(emblem, (-3.0, -0.5, 3.0), em, rot=(0, 0, 0.6), placed=placed)

    # A slow field on the road near the base end.
    _copy(meshes["slow"][0], (-6.0, 0.1, 0.05), _mat(textures, "Marker_SlowField", [(0, SLOW, 0.8), (1, SLOW, 0.3), (2, SLOW, 0.25)]),
          scale=(1.2, 1.2, 1), placed=placed)

    # A bolt in flight from the tower to a Skitter.
    proj = _append("Projectiles", ["Proj_Bolt", "Proj_Bolt_Glow"])
    if "Proj_Bolt" in proj:
        glow = C.image_to_array(os.path.join(C.EXPORTS, "Proj_Bolt", "Proj_Glow.png"))
        M.assign(proj["Proj_Bolt"], M.fx_material("SceneBolt", M.preview_image("SceneBoltImg", M.tinted(
            glow, [(0, (1, 1, 1), 3.0), (1, COOL, 2.5)])), "add"))
        core = os.path.join(C.EXPORTS, "VFX", "Bolt_Core.png")
        if "Proj_Bolt_Glow" in proj and os.path.exists(core):
            M.assign(proj["Proj_Bolt_Glow"], M.fx_material("SceneShell", M.preview_image("SceneShellImg", M.tinted(
                C.image_to_array(core), [(1, COOL, 1.4), (0, (1, 1, 1), 0.6)])), "add"))
        for o in proj.values():
            o.location = (0.8, 1.1, 0.45)
            o.rotation_euler = (0, 0, math.radians(20))
            placed.append(o)

    bk.render_game(Vector((0.0, 0.8, 0.3)), os.path.join(out, "markers_game.png"), zoom=1.0)
    bk.render_game(Vector((0.0, 0.9, 0.3)), os.path.join(out, "markers_game_zoom.png"), zoom=2.2)
    bk.render_game(Vector((0.0, 0.9, 0.3)), os.path.join(out, "markers_game_zoom_nobloom.png"), zoom=2.2, bloom=False)
    bk.render_game(Vector((4.6, 1.0, 0.6)), os.path.join(out, "markers_gate_zoom.png"), zoom=2.6)
    bk.unstage(sk)
    bk.remove(placed + stage)
