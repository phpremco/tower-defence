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
           # Round 35: Mercer reaction clips (Startled, Startled_Hold, Disbelief, Point_Shout, Lower_Weapon).
           "build_mercer_reactions.py",
           # 2026-11-08: Spencer's look upgrade; overwrites the Mercer export above (same rig, clips and names, 2048
           # maps, unmirrored) and re-runs the reaction clips on it.
           "build_mercer2.py",
           # Level bosses; their compare renders load the base species' .blend files built above.
           "build_broodmother.py", "build_packalpha.py", "build_stormmatriarch.py", "build_corewisp.py",
           # Round 10 bosses; each loads its species' .blend built above.
           "build_acidempress.py", "build_crystalwarlord.py", "build_skysovereign.py", "build_slimemonarch.py",
           # Round 31: the slugs strike by stretching (Strike clip; the SlimeMonarch's Flood rebuilt the same way).
           "build_slug_strike.py",
           # Round 36: smoother attacks for the basic species (coil, lunge, held contact, follow-through; new Strike
           # clips for the species without one, Strike_Float/Strike_Ground for the Wisp; Stinger Strike, Skyray Dive
           # and Mireleech Latch rebuilt).
           "build_attacks36.py",
           "build_rollerking.py",
           # Round 14: spawn origins, resource node, then Gravemaw (its previews load the hole and BroodMother).
           "build_env_origins.py", "build_gravemaw.py",
           # Round 23: living spawn origins (Closed/Marked/Open/Idle/Spawn); needs the ground sets and Skitter.blend.
           "build_origins2.py",
           # Round 16: continent bosses 2-6 (titan_kit.py); previews load Gravemaw and Skitter.
           "build_cindermatriarch.py", "build_frostwarden.py", "build_rotbloom.py", "build_shattered.py",
           "build_sealedone.py",
           # Round 28: the Shattered improvement pass (overwrites build_shattered.py's export), then the mechanic
           # clips and props of the six continent bosses (boss_mech_kit.py; each opens its boss's .blend).
           "build_shattered2.py", "build_gravemaw_mech.py", "build_cindermatriarch_mech.py",
           "build_frostwarden_mech.py", "build_rotbloom_mech.py", "build_shattered_mech.py", "build_sealedone_mech.py",
           # Round 7: towers, build pad, stars and wreck (imports tower_kit.py).
           "build_towers.py",
           # Round 9: gate, base and base turret, drones, gadgets (import base_kit.py; need build_towers first).
           "build_gate.py", "build_base.py",
           # Round 37: the expanded colony base (radar, landing zone, drone bays, storage)
           "build_base2.py",
           "build_drones.py", "build_gadgets.py",
           # Round 11: P2 towers, hybrids and gadgets (import build_towers/build_gadgets/build_drones read-only).
           "build_towers2.py", "build_gadgets2.py",
           # Round 18: P3 hybrids, gadgets, superweapons and drones (import round 7/9/11 scripts read-only).
           "build_towers3.py", "build_gadgets3.py", "build_drones2.py",
           # Round 13: story characters and cutscene models (story_kit.py).
           # Round 20: Voss on MPFB 2 (needs the MPFB extension + CC0 packs, Docs/COMMANDS.md); replaces
           # build_voss.py, whose round-13 export would overwrite it.
           "build_voss3.py", "build_story_props.py", "build_cutscene.py",
           # Round 19: the rebuilt mothership; overwrites the round-13 one from build_cutscene.py.
           "build_mothership2.py",
           # Round 15: corrupted (The Core) skins; reads every species' .blend and maps, so it runs after them.
           "build_corrupted_skins.py",
           # Round 17: continent environment sets 2-6 (env_kit2.py); each renders its own diorama.
           "build_env_ashenreach.py", "build_env_northerncrown.py", "build_env_mirewood.py",
           "build_env_southerncrown.py", "build_env_core.py",
           # Round 20: the S-69-CRYS planet texture set (numpy, no scene).
           "build_planet_s69.py",
           # Round 20: VFX flipbooks (Mantaflow sims cached in %TEMP%/td_vfx_cache; ~40 min from scratch).
           "build_vfx.py",
           # Round 24: liquid and mist textures (numpy) + shore previews with the bank pieces built above.
           "build_liquids.py",
           # Round 25b: spawn gates, projectiles/debris, then markers (its review scene appends the first two blends).
           "build_spawngate.py", "build_projectiles.py", "build_markers.py",
           # Round 24b: sweep-ready bank segments per style + organic pool previews (needs the round 8/17 kits).
           "build_bank_sweeps.py",
           # Round 25: UI art set (icons, kit, dialogue, radar, starfield, backdrop, halo + mockups).
           "build_ui.py",
           # Round 27: upgrade-screen display stage (pedestal, halo ring, backdrop).
           "build_ui_display.py",
           # Round 38: the comic frame of the boss-intro cut-in and answer box.
           "build_ui_comic.py",
           # Round 26: dying Earth and theme skies (numpy, planet_kit.py).
           "build_planet_earth.py", "build_skies.py",
           # Round 26: cutscene glow / trail / crack sprites (numpy) and the Crystal Plains snowdrift.
           "build_cutscene_fx.py", "build_decor_c1.py",
           # Round 38: the remains a boss leaves of a smashed crystal, plant or rock (round 13b).
           "build_decor_remains.py",
           # Round 26: menu diorama (places the exports above) and the underground cave kit and eyes.
           "build_menu_diorama.py", "build_underground.py",
           # Renders a 12 x 8 cell Crystal Plains level from the exports above; exports nothing. Keep last.
           # Round 29: boss props (ruin gate, seal crystals, ice block, drill wreck, drained maps), then the boss
           # decals, flipbooks and the dampened HUD overlay. Needs gate, towers, cutscene and cutscene_fx above.
           "build_bosses29_props.py", "build_bosses29_fx.py",
           # Round 30: Gravemaw's crack slabs (GW1; imports build_bosses29_props, preview reads Exports/Boss_FX).
           "build_bosses30_props.py",
           # Round 34: Gravemaw's modular 3D fissure kit (preview reads Env/Tiling, Tower_Gun and Gate exports).
           "build_fissure34.py",
           "build_env_diorama.py"]

for script in SCRIPTS:
    started = time.time()
    print(f"[td] ==== {script}", flush=True)
    runpy.run_path(os.path.join(HERE, script), run_name="__main__")
    print(f"[td] ==== {script} done in {time.time() - started:.0f}s", flush=True)
