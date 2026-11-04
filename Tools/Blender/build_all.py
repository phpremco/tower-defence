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

SCRIPTS = ["detailmaps.py", "build_skitter.py", "build_queen.py", "build_props.py",
           # Round 8: Crystal Plains environment (tiling sets, kit, rift, decor).
           "build_env_materials.py", "build_env_kit.py", "build_env_rift.py", "build_env_decor.py",
           "build_roller.py", "build_cragback.py",
           "build_prowler.py", "build_skyray.py", "build_shalebat.py", "build_wisp.py",
           # Round 10: P2 species (creature_kit2.py).
           "build_stinger.py", "build_bat.py", "build_bat_skins.py", "build_bulwark.py", "build_oozer.py",
           # Round 15: P3 species (creature_kit3.py).
           "build_shardback.py", "build_mireleech.py",
           "build_knitback.py", "build_slagstrider.py", "build_siftjaw.py", "build_surgefin.py",
           # The hero; it also builds and exports his weapons (build_mercer_weapons.py).
           "build_mercer.py",
           # Level bosses; their compare renders load the base species' .blend files built above.
           "build_broodmother.py", "build_packalpha.py", "build_stormmatriarch.py", "build_corewisp.py",
           # Round 10 bosses; each loads its species' .blend built above.
           "build_acidempress.py", "build_crystalwarlord.py", "build_skysovereign.py", "build_slimemonarch.py",
           "build_rollerking.py",
           # Round 14: spawn origins, resource node, then Gravemaw (its previews load the hole and BroodMother).
           "build_env_origins.py", "build_gravemaw.py",
           # Round 16: continent bosses 2-6 (titan_kit.py); previews load Gravemaw and Skitter.
           "build_cindermatriarch.py", "build_frostwarden.py", "build_rotbloom.py", "build_shattered.py",
           "build_sealedone.py",
           # Round 7: towers, build pad, stars and wreck (imports tower_kit.py).
           "build_towers.py",
           # Round 9: gate, base and base turret, drones, gadgets (import base_kit.py; need build_towers first).
           "build_gate.py", "build_base.py", "build_drones.py", "build_gadgets.py",
           # Round 11: P2 towers, hybrids and gadgets (import build_towers/build_gadgets/build_drones read-only).
           "build_towers2.py", "build_gadgets2.py",
           # Round 13: story characters and cutscene models (story_kit.py).
           "build_voss.py", "build_story_props.py", "build_cutscene.py",
           # Round 15: corrupted (The Core) skins; reads every species' .blend and maps, so it runs after them.
           "build_corrupted_skins.py",
           # Round 17: continent environment sets 2-6 (env_kit2.py); each renders its own diorama.
           "build_env_ashenreach.py", "build_env_northerncrown.py", "build_env_mirewood.py",
           "build_env_southerncrown.py", "build_env_core.py",
           # Renders a 12 x 8 cell Crystal Plains level from the exports above; exports nothing. Keep last.
           "build_env_diorama.py"]

for script in SCRIPTS:
    started = time.time()
    print(f"[td] ==== {script}", flush=True)
    runpy.run_path(os.path.join(HERE, script), run_name="__main__")
    print(f"[td] ==== {script} done in {time.time() - started:.0f}s", flush=True)
