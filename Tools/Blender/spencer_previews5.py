"""Previews for Spencer pass 4 (build_mercer5.py): spencer_previews' set (front, 3/4, back, bust, aim, game camera with
a Cragback and Skitters) with the face and the visor glass riding along, plus a face close-up (cutscene framing), a
game-camera shot with him in the air, a contact sheet of the jet clips, the draw/stow sheet (the sheath moved) and
pass 3 / pass 4 side by side.

Run on a saved pass-4 hero:
  blender --background --factory-startup --python Tools/Blender/spencer_previews5.py -- --blend <Mercer.blend> --out <dir>
          [--prefix pass4]
"""

import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import common as C  # noqa: E402
import mercer_previews as MP  # noqa: E402
import spencer_previews as SP  # noqa: E402

EXTRAS = ("Mercer_Face", "Mercer_Visor")
JET_CLIPS = (("Jet_Takeoff", (0, 7, 10, 12, 16, 20, 30)), ("Jet_Fly", (0, 6, 12, 18, 24, 30, 36)), ("Jet_Land", (0, 4, 8, 13, 16, 20, 28)))

_init, _hide, _remove = MP.Rig.__init__, MP.Rig.hide, MP.Rig.remove


def _rig_init(self, arm, mesh, weapons, copy=False):
    _init(self, arm, mesh, weapons, copy)
    self.extras = []
    for n in EXTRAS:
        o = bpy.data.objects.get(n)
        if o is None:
            continue
        if copy:
            o = C.link(o.copy())
            o.parent = self.arm
            o.modifiers["Armature"].object = self.arm
        o.hide_render = False
        self.extras.append(o)


def _rig_hide(self, hidden=True):
    _hide(self, hidden)
    for o in getattr(self, "extras", ()):
        o.hide_render = hidden


def _rig_remove(self):
    if self.copied:
        for o in getattr(self, "extras", ()):
            bpy.data.objects.remove(o)
    _remove(self)


MP.Rig.__init__ = _rig_init
MP.Rig.hide = _rig_hide
MP.Rig.remove = _rig_remove


def open_scene(blend, creature_blend=None):
    bpy.ops.wm.open_mainfile(filepath=blend)
    if creature_blend:
        SP.CREATURE_BLEND = creature_blend
    arm = bpy.data.objects["Mercer_Rig"]
    mesh = bpy.data.objects["Mercer"]
    weapons = {n: bpy.data.objects[n] for n in SP.WEAPONS}
    for o in bpy.data.objects:
        if o.type == "MESH" and o.name != "Mercer" and o.name not in weapons:
            o.hide_render = True
    C.preview_world()
    lights = C.light_rig(scale=1.7)
    C.ground()
    rig = MP.Rig(arm, mesh, weapons)
    for o in weapons.values():
        o.hide_render = True
    return rig, {a.name: a for a in bpy.data.actions}, lights


def face_closeups(blend, out, prefix):
    rig, acts, _ = open_scene(blend)
    MP.pose(rig, acts["Idle"], 10, "none")
    bpy.context.view_layer.update()
    head = rig.arm.matrix_world @ rig.arm.pose.bones["Head"].matrix @ Vector((0, 0.07, 0.0))
    paths = []
    for name, az, el, dist in (("face", -18, 3, 0.62), ("face_front", 0, 0, 0.6), ("face_side", -55, 6, 0.62)):
        cam = C.closeup_camera(head, dist, azimuth=az, elevation=el, lens=85)
        paths.append(C.render(cam, os.path.join(out, f"{prefix}_{name}.png"), 900, 900))
        bpy.data.objects.remove(cam)
    return paths


def jet_sheet(blend, out, prefix):
    rig, acts, _ = open_scene(blend)
    cells = []
    for clip, frames in JET_CLIPS:
        if clip not in acts:
            continue
        for f in frames:
            MP.pose(rig, acts[clip], f, "none")
            cam = C.closeup_camera(Vector((0, 0, 1.0)), 3.3, azimuth=-62, elevation=10, lens=50)
            cells.append(C.render(cam, os.path.join(out, f"_jet_{clip}_{f}.png"), 400, 480, samples=24))
            bpy.data.objects.remove(cam)
    path = os.path.join(out, f"{prefix}_jet_clips.png")
    C.contact_sheet(cells, path, 7, 300)
    for p in cells:
        os.remove(p)
    # The back with the pods lit, mid-flight.
    MP.pose(rig, acts["Jet_Fly"], 10, "none")
    cam = C.closeup_camera(Vector((0, 0, 1.15)), 3.0, azimuth=160, elevation=22, lens=55)
    C.render(cam, os.path.join(out, f"{prefix}_jet_back.png"), 960, 1000)
    bpy.data.objects.remove(cam)
    return path


def fly_game(blend, out, prefix, creature_blend):
    rig, acts, lights = open_scene(blend, creature_blend)
    crag = SP.append_creature("Cragback", "Walk")
    skit = SP.append_creature("Skitter", "Walk")
    heroes = [(acts["Jet_Fly"], 10, (-0.9, 0.5, 0.8), -0.9, "none"), (acts["Jet_Takeoff"], 14, (1.0, 0.7, 0.0), -2.4, "none"),
              (acts["Idle"], 10, (0.2, -0.3, 0.0), 0.3, "none")]
    creatures = [(crag, (1.6, -1.1, 0), 2.6, SP.CRAG_SCALE, 6), (skit, (2.6, -0.4, 0), 2.9, SP.SKITTER_SCALE, 4),
                 (skit, (-0.6, -1.4, 0), 2.4, SP.SKITTER_SCALE, 9)]
    a = SP.game_shot(rig, heroes, creatures, os.path.join(out, f"{prefix}_game_fly.png"), 1.0, lights, 1280, 720, (0.5, -0.3, 0.4))
    b = SP.game_shot(rig, heroes, creatures, os.path.join(out, f"{prefix}_game_fly_zoom.png"), 3.0, lights, 1280, 720, (-0.2, 0.0, 0.7))
    return [a, b]


def compare(out, prefix, before="pass3"):
    for view, width in (("sheet", 1600), ("game", 1280), ("game_zoom", 1280), ("bust", 1024), ("front", 960), ("34", 960),
                        ("back34", 960)):
        a, b = os.path.join(out, f"{before}_{view}.png"), os.path.join(out, f"{prefix}_{view}.png")
        if os.path.exists(a) and os.path.exists(b):
            C.contact_sheet([a, b], os.path.join(out, f"{prefix}_compare_{view}.png"), 2, width)


def render_all(blend, out, prefix, creature_blend, fast=False, draw_clips=None):
    os.makedirs(out, exist_ok=True)
    SP.render_file(blend, [out], prefix, creature_blend)
    face_closeups(blend, out, prefix)
    jet_sheet(blend, out, prefix)
    fly_game(blend, out, prefix, creature_blend)
    if not fast:
        if draw_clips:
            SP.render_draw_sheet(blend, os.path.join(out, f"{prefix}_draw_stow.png"), draw_clips)
    compare(out, prefix)


def main():
    args = C.cli_args()
    import build_mercer3 as M3
    render_all(args["blend"], args["out"], args.get("prefix", "pass4"), args.get("creatures") or M3.ORIGINAL_BLEND,
               fast=bool(args.get("fast")), draw_clips=list(M3.DRAW_CLIPS.items()))


if __name__ == "__main__":
    main()
