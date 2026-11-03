"""Rebuilds every pilot asset in one Blender session: the shared detail normals, FBX + textures in
ArtSource/Exports, .blend sources in ArtSource/Blend and preview renders (default ArtSource/Previews, or
--preview <dir>).

Run: blender --background --factory-startup --python Tools/Blender/build_all.py -- [--preview <dir>] [--norender]
"""

import os
import runpy
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

SCRIPTS = ["detailmaps.py", "build_skitter.py", "build_queen.py", "build_props.py", "build_roller.py", "build_cragback.py",
           "build_prowler.py", "build_skyray.py", "build_shalebat.py", "build_wisp.py",
           # The hero; it also builds and exports his weapons (build_mercer_weapons.py).
           "build_mercer.py",
           # Level bosses; their compare renders load the base species' .blend files built above.
           "build_broodmother.py", "build_packalpha.py", "build_stormmatriarch.py", "build_corewisp.py",
           # Round 7: towers, build pad, stars and wreck (imports tower_kit.py).
           "build_towers.py"]

for script in SCRIPTS:
    started = time.time()
    print(f"[td] ==== {script}", flush=True)
    runpy.run_path(os.path.join(HERE, script), run_name="__main__")
    print(f"[td] ==== {script} done in {time.time() - started:.0f}s", flush=True)
