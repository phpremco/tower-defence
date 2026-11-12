"""Round 56: re-key only the Fire Larva's Crawl (build_cindermatriarch.larva_crawl, the planted tripod crawl) on the
saved ArtSource/Blend/FireLarva.blend and export FireLarva.fbx / FireLarva_LOD1.fbx again with every clip.

Mesh, UVs, maps, bones and the other clips (Idle, Hit, Death) are untouched (a full `--larva-only` rebuild would remesh
and re-bake the larva). The report's crawl_distance_per_loop becomes build_cindermatriarch.LARVA_CRAWL_DISTANCE.

Run: blender -b --factory-startup --python Tools/Blender/r56_larva_rekey.py
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import boss_mech_kit as BMK  # noqa: E402
import build_cindermatriarch as BC  # noqa: E402
import common as C  # noqa: E402
import creature_kit3 as K3  # noqa: E402

NAME = BC.LARVA
LOD_AIMS = {f"Leg_{s}{i}_Upper": (f"Leg_{s}{i}_Upper", f"Leg_{s}{i}_Lower") for s, _ in BC.SIDES for i in (1, 2, 3)}


def main():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, NAME + ".blend"))
    arm, mesh = bpy.data.objects[NAME + "_Rig"], bpy.data.objects[NAME]
    lod_arm, lod_mesh = bpy.data.objects[NAME + "_LOD1_Rig"], bpy.data.objects[NAME + "_LOD1"]
    path = os.path.join(C.EXPORTS, NAME, NAME + "_report.json")
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    order = list(rep["actions"])
    poser, lod_poser = K3.ClipPoser(arm, []), K3.ClipPoser(lod_arm, [])
    frames, spec, loop = BC.larva_crawl(poser)
    for n in ("Crawl", "Crawl_LOD"):
        old = bpy.data.actions.get(n)
        if old is not None:
            bpy.data.actions.remove(old)
    poser.use("Crawl")
    lod_poser.use("Crawl")
    poser.action("Crawl", frames, spec, loop=loop)
    C.lod_action(poser, lod_poser, "Crawl_LOD", frames, spec, loop, LOD_AIMS)
    folder = os.path.join(C.EXPORTS, NAME)
    BMK.export_rig(os.path.join(folder, NAME + ".fbx"), arm, [mesh], {n: bpy.data.actions[n] for n in order})
    BMK.export_rig(os.path.join(folder, NAME + "_LOD1.fbx"), lod_arm, [lod_mesh],
                   {n: bpy.data.actions[n + "_LOD"] for n in order})
    rep["crawl_distance_per_loop"] = round(BC.LARVA_CRAWL_DISTANCE, 3)
    rep["round56_crawl"] = {
        "gait": "planted tripod crawl, %d steps per loop, stance %.2f, stride %.3f model units, feet held in the world "
                "while planted" % (BC.LARVA_CRAWL_CYCLES, BC.LARVA_CRAWL_STANCE, BC.LARVA_CRAWL_STRIDE),
        "crawl_distance_per_loop_was": 0.2, "clip_frames": frames}
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2)
    C.save_blend(NAME)
    C.log("larva crawl re-keyed", frames, "distance per loop", rep["crawl_distance_per_loop"])


if __name__ == "__main__":
    main()
