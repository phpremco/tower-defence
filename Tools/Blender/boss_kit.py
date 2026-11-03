"""Helpers for the level-boss builds (round 6: build_broodmother.py, build_packalpha.py, build_stormmatriarch.py,
build_corewisp.py). Kept apart from common.py so the boss work never touches the shared modules.

- ScalePoser: a Poser that also keys bone scale ({"scale": s} in a spec). Bosses use it for organs that swell
  (brood sacs, a heart, expanding rings). Scaled bones should be leaves: their children would inherit the scale.
- build_lod: common.build_lod with a ScalePoser, so the LOD keeps the scale keys.
- append_base: the base species' current model from ArtSource/Blend, for side-by-side renders at game scale.
- stage / game_view_multi / closeup_multi: renders of several rigs (boss and base) with their own clip, frame,
  position and scale.
- palette_variant: the `--palette <name>` switch; a variant run bakes into a scratch folder, never into Exports.
- scars: glowing healed scar lines over a creature (an `extra` for common.build_mesh_asset).
"""

import os

import bpy
import numpy as np
from mathutils import Matrix, Vector

import common as C


# ---------------------------------------------------------------------------------------------------------------
# Posing with scale
# ---------------------------------------------------------------------------------------------------------------

class ScalePoser(C.Poser):
    """Poser with uniform or per-axis bone scale. `scale_bones`: the bones that get scale keys (others keep none,
    so the FBX stays small)."""

    def __init__(self, arm, scale_bones=()):
        super().__init__(arm)
        self.scale_bones = set(scale_bones)

    def solve(self, specs):
        pose = super().solve(specs)
        for name, spec in specs.items():
            s = spec.get("scale") if isinstance(spec, dict) else None
            if s is None or name not in pose:
                continue
            s = Vector((s, s, s)) if isinstance(s, (int, float)) else Vector(s)
            pose[name] = pose[name] @ Matrix.Diagonal((s.x, s.y, s.z, 1.0))
        return pose

    def key(self, pose, frame):
        super().key(pose, frame)
        for name in self.order:
            if name not in self.scale_bones:
                continue
            basis = self.inherited(name, pose).inverted() @ pose[name]
            _, _, sc = basis.decompose()
            pb = self.arm.pose.bones[name]
            pb.scale = sc
            pb.keyframe_insert("scale", frame=frame, group=name)


def build_lod(name, lod_name, asset, builder, bones, clips, full_poser, actions_full, lod_tris, drop, aims, scale_bones=()):
    """common.build_lod, keyed with a ScalePoser (the LOD's scaled organs pulse like the full rig's)."""
    full = {b[0]: b for b in bones}
    lod_bones = []
    for bname, head, tail, parent, deform in bones:
        if bname in drop:
            continue
        if bname in aims:
            first, last = aims[bname]
            head, tail = full[first][1], full[last][2]
        while parent in drop:
            parent = drop[parent]
        lod_bones.append((bname, head, tail, parent, deform))
    lod_mesh = C.decimate(asset.low.data.copy(), lod_tris, lod_name, symmetric=True)
    C.shade_smooth(lod_mesh)
    lod = C.link(bpy.data.objects.new(lod_name, lod_mesh))
    lod.data.materials.clear()
    lod.data.materials.append(asset.low.data.materials[0])
    arm = C.build_armature(lod_name + "_Rig", lod_bones)

    def remap(b):
        while b in drop:
            b = drop[b]
        return b
    saved = [(p, list(p.chain), p.bone) for p in builder.parts]
    for p in builder.parts:
        p.chain = list(dict.fromkeys(remap(c) for c in p.chain))
        p.bone = p.chain[0] if p.chain else p.bone
    C.skin(lod, arm, builder)
    for p, chain, bone in saved:
        p.chain, p.bone = chain, bone
    poser = ScalePoser(arm, [b for b in scale_bones if b not in drop])
    actions = {}
    for clip, fn in clips.items():
        frames, spec, loop = fn(full_poser)
        actions[clip] = C.lod_action(full_poser, poser, clip + "_LOD", frames, spec, loop, aims)
    arm.animation_data.action = next(iter(actions.values()))
    for clip, act in actions_full.items():
        act.name = clip + "_Full"
    for clip, act in actions.items():
        act.name = clip
    C.export_fbx(os.path.join(C.EXPORTS, name, lod_name + ".fbx"), [arm, lod], animated=True)
    for clip, act in actions.items():
        act.name = clip + "_LOD"
    for clip, act in actions_full.items():
        act.name = clip
    return arm, lod, actions


# ---------------------------------------------------------------------------------------------------------------
# Base species for compare renders
# ---------------------------------------------------------------------------------------------------------------

def append_base(asset):
    """Appends `<asset>` and `<asset>_Rig` with all their actions from ArtSource/Blend/<asset>.blend. Returns
    (arm, mesh, {clip name: action}); action names clash with the boss's, so they are mapped by source name."""
    path = os.path.join(C.BLEND, asset + ".blend")
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in (asset, asset + "_Rig")]
        names = [n for n in src.actions if not n.endswith("_LOD")]
        dst.actions = list(names)
    actions = dict(zip(names, dst.actions))
    arm = mesh = None
    for o in dst.objects:
        C.link(o)
        if o.type == "ARMATURE":
            arm = o
        else:
            mesh = o
    mesh.parent = arm
    mesh.modifiers["Armature"].object = arm
    arm.animation_data_create()
    for a in actions.values():
        a.use_fake_user = True
    arm.hide_render = mesh.hide_render = True
    return arm, mesh, actions


# ---------------------------------------------------------------------------------------------------------------
# Multi-rig renders
# ---------------------------------------------------------------------------------------------------------------

def stage(entries, base_frame=1000):
    """Copies of rigs, each playing its own action at its own frame (NLA strips shifted so the scene frame
    `base_frame` shows it). entries: (arm, mesh, action, frame, position, yaw, scale)."""
    copies = []
    for arm, mesh, act, frame, pos, yaw, scale in entries:
        a2 = C.link(arm.copy())
        a2.animation_data.action = None
        track = a2.animation_data.nla_tracks.new()
        strip = track.strips.new(act.name + "_strip", int(base_frame - frame), act)
        strip.extrapolation = "HOLD"
        if hasattr(strip, "action_slot") and len(getattr(act, "slots", ())):
            strip.action_slot = act.slots[0]
        m2 = C.link(mesh.copy())
        m2.parent = a2
        m2.modifiers["Armature"].object = a2
        m2.hide_render = a2.hide_render = False
        a2.scale = (scale, scale, scale)
        a2.location = Vector(pos)
        a2.rotation_euler.z = yaw
        copies.append((a2, m2))
    return copies


def unstage(copies):
    for a2, m2 in copies:
        bpy.data.objects.remove(m2)
        bpy.data.objects.remove(a2)


def _render_staged(entries, camera, out_path, width, height, hide=(), base_frame=1000):
    scene = bpy.context.scene
    hidden = [(o, o.hide_render) for o in hide]
    for o in hide:
        o.hide_render = True
    copies = stage(entries, base_frame)
    keep = scene.frame_current
    scene.frame_set(base_frame)
    C.render(camera, out_path, width, height)
    scene.frame_set(keep)
    unstage(copies)
    for o, h in hidden:
        o.hide_render = h
    return out_path


def game_view_multi(entries, out_path, zoom=1.0, close_lights=(), target=(0, 0, 0.1), width=640, height=360, hide=()):
    """The game camera and game lights over several staged rigs (boss and base side by side at game scale)."""
    C.hide_lights(close_lights, True)
    lights = C.game_lights()
    cam = C.game_camera(Vector(target), width / C.GAME_PIXELS_PER_UNIT / zoom)
    _render_staged(entries, cam, out_path, width, height, hide)
    for o in lights + [cam]:
        bpy.data.objects.remove(o)
    C.hide_lights(close_lights, False)
    return out_path


def closeup_multi(entries, camera, out_path, width=1280, height=720, hide=()):
    """A close-up camera (with the scene's own light rig) over several staged rigs."""
    return _render_staged(entries, camera, out_path, width, height, hide)


# ---------------------------------------------------------------------------------------------------------------
# Palette variants
# ---------------------------------------------------------------------------------------------------------------

def palette_variant(palettes, default):
    """The palette chosen with `--palette <name>` (default `default`). A variant other than the default bakes into
    `<preview>/_variant_exports` instead of ArtSource/Exports, so it never replaces the real asset."""
    name = C.cli_args().get("palette", default)
    if name not in palettes:
        raise ValueError(f"unknown palette {name}, choose from {list(palettes)}")
    if name != default:
        C.EXPORTS = os.path.join(C.preview_dir(), "_variant_exports")
        os.makedirs(C.EXPORTS, exist_ok=True)
    return name, palettes[name]


# ---------------------------------------------------------------------------------------------------------------
# Surface extras
# ---------------------------------------------------------------------------------------------------------------

def scars(cuts, color, glow, width=0.012, depth=0.004):
    """Healed claw scars: each cut is (start, end, facing) in Blender space; vertices within `width` of the segment
    whose normal points along `facing` (so the cut stays on one surface and does not reach parts behind it) get a
    dark groove with a dull glow at its floor. Mirrored creatures pass the cuts of one side; the mirror bake copies
    them to the other."""
    color = np.asarray(color, dtype=float)

    def extra(s, pid):
        for a, b, facing in cuts:
            a = np.asarray(a, dtype=float)
            b = np.asarray(b, dtype=float)
            f = np.asarray(facing, dtype=float)
            f /= np.linalg.norm(f)
            dist = C.segment_distance(s.p, a, b)
            ab = b - a
            t = np.clip(((s.p - a) @ ab) / max(ab @ ab, 1e-12), 0, 1)
            taper = np.sin(np.pi * t) ** 0.5
            face = C.smoothstep(0.1, 0.5, s.n @ f)
            line = (1 - C.smoothstep(width * 0.35 * taper, width * taper + 1e-6, dist)) * face
            core = (1 - C.smoothstep(0.0, width * 0.4 * taper + 1e-6, dist)) * face
            s.disp -= depth * line
            s.albedo = C.lerp3(s.albedo, s.albedo * 0.25, line * 0.9)
            s.emit = np.maximum(s.emit, color[None, :] * (core * glow)[:, None])
            s.rough = np.clip(s.rough - 0.2 * line, 0.08, 0.95)
    return extra


def chain_extras(*fns):
    """Several `extra` passes in order (e.g. weathering, then scars)."""
    def extra(s, pid):
        for fn in fns:
            if fn is not None:
                fn(s, pid)
    return extra


# ---------------------------------------------------------------------------------------------------------------
# The standard boss preview set
# ---------------------------------------------------------------------------------------------------------------

def boss_previews(prefix, arm, mesh, actions, lod, out, asset, target, distance, game_scale, base, move, power,
                  strips, macros, rig_scale=1.0, lift=0.0, game_lift=0.0, game_target=(0, 0, 0.1), idle="Idle",
                  minions=(), extra_shots=(), compare_span=None, compare_zoom=3.0, compare_distance=1.25):
    """Renders the round-6 boss set and returns the paths:
    close-up 3/4, side, front, top, power pose, death; clip strips; LOD1; macros (with and without detail normals);
    game view at play zoom and zoomed with minions of the base species around the boss; a compare of boss and base
    side by side at game scale (game camera and close-up). `base`: dict(asset, scale, clip, frame); `move` and
    `power`: (clip, frame); `strips`: [(clip, camera key, frames)]; `macros`: [(file suffix, aim, distance, azimuth,
    elevation)]; `minions`: [(x, y, yaw, clip frame)] positions of base-species copies in the game views (world
    units); `extra_shots`: [(file suffix, camera key, clip, frame)]."""
    C.preview_world()
    rig = C.light_rig(scale=rig_scale)
    C.ground()
    arm.location.z = lift
    lod_arm, lod_mesh, lod_actions = lod
    lod_arm.hide_render = lod_mesh.hide_render = True
    lod_arm.location.z = lift
    t = Vector(target) + Vector((0, 0, lift))
    cams = {
        "close": C.closeup_camera(t, distance, azimuth=-38, elevation=24, lens=55),
        "side": C.closeup_camera(t, distance * 1.05, azimuth=-90, elevation=8, lens=55),
        "front": C.closeup_camera(t, distance * 0.85, azimuth=-15, elevation=12, lens=55),
        "top": C.closeup_camera(t, distance * 1.05, azimuth=-160, elevation=58, lens=55),
        "top_front": C.closeup_camera(t, distance * 1.05, azimuth=-12, elevation=60, lens=55),
    }
    paths = []

    def shot(name, cam, clip, frame):
        C.set_action(arm, actions[clip], frame)
        paths.append(C.render(cams[cam], os.path.join(out, f"{prefix}_{name}.png"), 1280, 720))

    shot("close", "close", idle, 15)
    shot("side", "side", move[0], move[1])
    shot("front", "front", power[0], power[1])
    shot("top", "top", move[0], move[1] + 4)
    shot("power", "close", power[0], power[1])
    shot("dead", "close", "Death", C_last_frame(actions["Death"]))
    for name, cam, clip, frame in extra_shots:
        shot(name, cam, clip, frame)
    strip_paths = [C.strip_sheet(arm, actions[clip], cams[cam], out, f"{prefix}_{clip.lower()}_{cam}", frames)
                   for clip, cam, frames in strips]

    C.set_action(lod_arm, lod_actions[move[0]], move[1])
    arm.hide_render = mesh.hide_render = True
    lod_arm.hide_render = lod_mesh.hide_render = False
    paths.append(C.render(cams["close"], os.path.join(out, f"{prefix}_lod1_close.png"), 1280, 720))
    lod_arm.hide_render = lod_mesh.hide_render = True
    arm.hide_render = mesh.hide_render = False

    C.set_action(arm, actions[idle], 15)
    for suffix, aim, dist, az, el in macros:
        aim = Vector(aim) + Vector((0, 0, lift))
        paths.append(C.render(C.macro_camera(aim, dist, az, el), os.path.join(out, f"{prefix}_macro_{suffix}.png"), 1280, 720))
    if macros:
        C.set_detail_preview(asset, False)
        suffix, aim, dist, az, el = macros[0]
        aim = Vector(aim) + Vector((0, 0, lift))
        paths.append(C.render(C.macro_camera(aim, dist, az, el),
                              os.path.join(out, f"{prefix}_macro_{suffix}_bakeonly.png"), 1280, 720))
        C.set_detail_preview(asset, True)

    # Game views: the boss among its species, as the player sees a boss wave.
    b_arm, b_mesh, b_actions = append_base(base["asset"])
    b_act = b_actions[base["clip"]]
    entries = [(arm, mesh, actions[move[0]], move[1], (0, 0, game_lift), 0.0, game_scale)]
    for x, y, yaw, frame in minions:
        entries.append((b_arm, b_mesh, b_act, frame, (x, y, game_lift), yaw, base["scale"]))
    hide = [arm, mesh]
    paths.append(game_view_multi(entries, os.path.join(out, f"{prefix}_game.png"), 1.0, rig, game_target, hide=hide))
    paths.append(game_view_multi(entries, os.path.join(out, f"{prefix}_game_zoomed.png"), 2.0, rig, game_target, hide=hide))
    single = [(arm, mesh, actions[move[0]], move[1], (0, 0, game_lift), 0.0, game_scale)]
    paths.append(game_view_multi(single, os.path.join(out, f"{prefix}_single_zoomed.png"), 3.0, rig, game_target, hide=hide))

    # Compare: boss and base side by side at the same (game) scale.
    span = compare_span if compare_span is not None else game_scale * distance * 0.33
    pair = [(arm, mesh, actions[move[0]], move[1], (-span * 0.45, 0, game_lift), 0.0, game_scale),
            (b_arm, b_mesh, b_act, base["frame"], (span * 0.75, 0, game_lift), 0.0, base["scale"])]
    cmp_game = game_view_multi(pair, os.path.join(out, f"{prefix}_compare_game.png"), compare_zoom, rig, game_target, hide=hide)
    ct = Vector(target) * game_scale + Vector((span * 0.15, 0, game_lift))
    cam = C.closeup_camera(ct, distance * game_scale * compare_distance, azimuth=-30, elevation=22, lens=55)
    cmp_close = closeup_multi(pair, cam, os.path.join(out, f"{prefix}_compare_close.png"), hide=hide)
    C.contact_sheet([cmp_close, cmp_game], os.path.join(out, f"{prefix}_compare.png"), 2, 640)
    C.remove_objects([b_mesh, b_arm])

    C.contact_sheet(paths, os.path.join(out, f"sheet_{prefix}.png"), 2, 640)
    C.contact_sheet(strip_paths, os.path.join(out, f"strips_{prefix}.png"), 1, 960)
    C.uv_layout(mesh, os.path.join(out, f"{prefix}_uv.png"), background=asset.textures["albedo"])
    arm.location.z = 0
    return paths + strip_paths


def C_last_frame(act):
    return int(act.frame_range[1])


def variant_previews(prefix, variant, arm, mesh, actions, out, target, distance, game_scale, move, rig_scale=1.0,
                     lift=0.0, game_lift=0.0, game_target=(0, 0, 0.1), idle="Idle"):
    """The few renders a palette variant needs to be judged: 3/4 close-up, top and the game view zoomed, named
    `<prefix>_<variant>_*.png`; then a sheet against the default palette's renders when those exist."""
    C.preview_world()
    rig = C.light_rig(scale=rig_scale)
    C.ground()
    arm.location.z = lift
    t = Vector(target) + Vector((0, 0, lift))
    close = C.closeup_camera(t, distance, azimuth=-38, elevation=24, lens=55)
    top = C.closeup_camera(t, distance * 1.05, azimuth=-160, elevation=58, lens=55)
    C.set_action(arm, actions[idle], 15)
    p_close = C.render(close, os.path.join(out, f"{prefix}_{variant}_close.png"), 1280, 720)
    C.set_action(arm, actions[move[0]], move[1] + 4)
    p_top = C.render(top, os.path.join(out, f"{prefix}_{variant}_top.png"), 1280, 720)
    arm.location.z = 0
    single = [(arm, mesh, actions[move[0]], move[1], (0, 0, game_lift), 0.0, game_scale)]
    p_game = game_view_multi(single, os.path.join(out, f"{prefix}_{variant}_single_zoomed.png"), 3.0, rig, game_target,
                             hide=[arm, mesh])
    cool = [os.path.join(out, f"{prefix}_{n}.png") for n in ("close", "top", "single_zoomed")]
    if all(os.path.exists(p) for p in cool):
        # Rows: default palette on the left, variant on the right.
        C.contact_sheet([cool[0], p_close, cool[1], p_top, cool[2], p_game],
                        os.path.join(out, f"{prefix}_palette_compare.png"), 2, 640)
    return [p_close, p_top, p_game]
