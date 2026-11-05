# ArtSource: Blender pilot (Docs/GRAPHICS_UPGRADE_PLAN.md, route B)

Generated 3D art for the game. Everything here comes from Python scripts in `Tools/Blender/`; nothing is
modelled by hand. Delete `Exports/` and `Blend/` and rebuild them at any time.

Round 2 (2026-11-03) redesigned the Skitter, the Queen and the Crysium crystals to read as alien and dangerous
instead of as an Earth ant, spider and quartz. The design rules and every departure are in `DESIGN_NOTES.md`.

Round 3 (2026-11-03) is a surface-detail pass only: the shapes, silhouettes and colours of round 2 are unchanged.
New: a cleaner unwrap (mirrored halves, large islands, one texel density), per-pixel micro-detail baked from a
shader on the source (pores, cuticle cells, growth lines, scratches, chipped edges, membrane veins and wrinkles,
crystal facets, frost, growth layers and fractures, rock grain, pits, cracks and dust), packed maps, and four
shared tiling detail normals for close-ups.

## Rebuild

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python Tools/Blender/build_all.py -- --preview <folder>
```

- Rebuilds everything in about 2 minutes (CPU Cycles for the bakes, EEVEE for the previews).
- `--preview <folder>`: where preview renders go. Without it they go to `ArtSource/Previews/`.
- `--compare <folder>`: a folder with an earlier round's previews; writes `compare3_<asset>.png` sheets (left the
  earlier render, right the new one with the same file name): `compare3_skitter`, `compare3_queen`,
  `compare3_crystals`, `compare3_rocks`. Macro close-ups: `*_macro_*.png` (the `_bakeonly` variant leaves out the
  tiling detail normals), UV layouts: `*_uv.png`.
- `detailmaps.py` rebuilds only the shared detail normals (seconds); `build_all.py` runs it first.
- `--norender`: build and export only, no previews.
- One asset: `build_skitter.py`, `build_queen.py`, `build_props.py` (`--only Rock_A` builds a single prop).
- `inspect_fbx.py -- --fbx <path>` re-imports an FBX and prints its meshes, triangles, bones and actions.

## How an asset is made

1. **Parts** (`common.PartBuilder`): ellipsoids, tapered tubes, Crysium shards (`shard`: fluted star section,
   twist, bend, helix, any profile such as the double-pointed floating shard) and "chiselled" convex rocks. Each part
   has a style function and a bone.
2. **Fuse**: a voxel remesh merges all parts into one closed surface, so joints blend and nothing intersects.
3. **Detail** (`common.detail`, `chitin.py`, `crysium.py`): each style displaces the vertices of its part (shingled
   plates, serrated crests, sensor slits, membrane sacs with glowing blobs, blade edges, strata, cracks) and paints
   colour, glow and roughness into vertex attributes. Round 3: each style also writes a material family weight
   (`Fam`: chitin, membrane, crystal, rock) and a `Flow` coordinate whose iso-lines are growth lines (along the
   plates, across a shard's axis, rock strata). `chitin.weathering` adds pitting, scratches, dark markings and
   dusty undersides over a whole creature. `crysium.shard_style` is the one Crysium look for props and for the
   crystals fused into the creatures (violet root, pink body, icy tip, glow in the flutes and node rings, dark
   fracture lines).
4. **AO**: Cycles bakes ambient occlusion into the source vertices.
5. **Game mesh**: the surface is decimated to the triangle budget. Creatures are then made exactly mirror-symmetric
   (`common.symmetrize`, re-decimated so the count stays on budget). `uvlayout.py` unwraps: seams on part
   boundaries and along the underside of every tube and shard, minimum-stretch unwrap, one texel density, packed.
   Creatures unwrap only the right half; the left half copies its twin's UVs, so a 1024 map holds the creature at
   about twice the round-2 density. Rocks keep a smart projection of their hewn facets as seams.
6. **Micro-detail** (`surface.py`): a shader on the source turns `Fam` and `Flow` into a height field plus cavity,
   wear and roughness terms, evaluated per bake sample (8 per texel, so sub-texel detail averages instead of
   aliasing). The height becomes the normal through a Bump node; cavity darkens the albedo and the AO (grime in
   pores, dust in rock cracks), wear lightens chipped edges and scratches. Detail lives only in the textures: the
   game mesh, tri counts and bones are unchanged.
7. **Bake** source to game mesh (`common.bake_maps`): Albedo (with AO, edge wear and micro cavity), Normal,
   Emission and the packed Mask. With mirrored UVs only the right half is baked, so the twins never overwrite it.
   Surfaces are matte (chitin 0.6 to 0.8); rims and ridges get a lower roughness, so the sheen sits on edges.
   Crystals get split normals above 30 degrees for hard facets.
8. **Rig** (creatures): bones built from the same numbers as the parts. Weights are generated per part (a leg vertex can
   only follow its leg bones and the thorax), which avoids the leg-to-belly bleeding that bone heat gives on fused meshes.
9. **Actions**: keyed from code. Legs use analytic IK: the coxa yaws towards the foot, femur and tibia solve a two-bone
   IK with the knee up. The walk is a tripod gait (L1 R2 L3 against R1 L2 R3).
10. **Export**: FBX plus PNG maps in `Exports/<Asset>/`, the `.blend` in `Blend/`, and a `<Asset>_report.json` with counts.

## Naming

| Thing | Pattern | Example |
| --- | --- | --- |
| FBX | `Exports/<Asset>/<Asset>.fbx` | `Exports/Skitter/Skitter.fbx` |
| Mesh object | `<Asset>` | `Skitter` |
| Armature | `<Asset>_Rig` | `Skitter_Rig` |
| Clips (take names in Unity) | `<Asset>_Rig\|<Action>` | `Skitter_Rig\|Walk` |
| Maps | `<Asset>_Albedo/_Normal/_Emission/_Mask.png` | `Queen_Mask.png` |
| Low (Switch) maps, creatures | `<Asset>/Low/<Asset>_Albedo/_Normal/_Emission/_Mask.png` (512) | `Skitter/Low/Skitter_Mask.png` |
| Shared detail normals | `Exports/Detail/Detail_<Family>_Normal.png` (512, tiling) | `Detail_Chitin_Normal.png` |
| Bones | `Root, Thorax, Chest, Head, Abdomen_n, Jaw_UL/UR/LL/LR, Petal_UL/..., Blade_L_n, Arm_L_n, Leg_L1_Coxa/Femur/Tibia` | |

## Performance budget (Remco, 2026-11-03: "not a triple-A game, but it must look nice and cool")

| Rule | Value |
| --- | --- |
| Map size, creatures | 1024 max (the Queen included), plus a 512 set in `<Asset>/Low/` for the Low (Switch) profile |
| Map size, props | 512 |
| Maps per material | at most 4: Albedo, Normal, Emission, Mask (packed) |
| Shared maps | one tiling 512 detail normal per material family (chitin, membrane, crystal, rock), shared by all assets |
| Materials | one per asset, no multi-material splits |
| Geometry | tri and bone counts as in the table below; micro-detail comes from textures only, never from geometry |

## Budgets (game mesh)

| Asset | Triangles | Bones | Maps (Low) | Clips (frames @30 fps) |
| --- | --- | --- | --- | --- |
| Skitter | 3,800 (budget 4,000) | 25 | 1024 (512) | Walk 24 (loop), Idle 60 (loop), Hit 12, Death 36 |
| Skitter_LOD1 | 1,700 | 16 | same maps | same four clips |
| Queen | 14,500 (budget 15,000) | 26 | 1024 (512) | Walk 40 (loop), Idle 80 (loop), Scream 50 |
| Roller | 3,900 (budget 4,000) | 20 | 1024 (512) | Walk 20 (loop), Idle 60 (loop), Hit 12, Death 36, Curl 14, Roll 24 (loop), Uncurl 14 |
| Roller_LOD1 | 1,700 | 14 | same maps | same seven clips |
| Cragback | 3,930 | 19 | 1024 (512) | Walk 32 (loop), Idle 72 (loop), Hit 14, Death 40 |
| Cragback_LOD1 | 1,700 | 10 | same maps | same four clips |
| Prowler | 3,900 | 22 | 1024 (512) | Run 16 (loop), Idle 60 (loop), Hit 12, Death 36, Pounce 26 |
| Prowler_LOD1 | 1,700 | 14 | same maps | same five clips |
| Crystal_A / B / C | 1,450 / 1,350 / 1,480 (budget 1,500) | – | 512 | – |
| Rock_A / B / C | 500 / 800 / 700 | – | 512 | – |
| Burrow_A | 2,600 | – | 512 (was 1024 in round 2) | – |

`Exports/Skitter/Skitter_LOD1.fbx` is the Low (Switch) / far LOD: the game mesh decimated again (same UVs, so it
shares the Skitter maps), 2 bones per leg (coxa folded into the femur), no jaw bones and one abdomen bone. Its clips
are keyed from the full rig's pose every frame, so both LODs move the same.

## Maps per asset (round 3)

Every asset has the same four maps in `Exports/<Asset>/` (creatures also in `Exports/<Asset>/Low/` at 512):

| Map | Colour space | Contents |
| --- | --- | --- |
| `<Asset>_Albedo.png` | sRGB | base colour with the vertex AO, edge wear and micro cavity (grime in pores, dust in cracks) multiplied in |
| `<Asset>_Normal.png` | linear, tangent space, +Y up (OpenGL, Unity's convention) | plates and shapes of the source plus the micro-detail bump |
| `<Asset>_Emission.png` | sRGB | glow mask in the glow colour (0..1); the material's emission colour sets how hot it burns |
| `<Asset>_Mask.png` | linear (sRGB off) | **R** ambient occlusion, **G** smoothness (1 − roughness), **B** primary detail mask, **A** secondary detail mask |

The detail masks say where a family's tiling detail normal applies (0 = nowhere, e.g. glowing eyes and slits):

| Asset | B (primary) detail | A (secondary) detail | Tiling B / A (tiles per UV unit) |
| --- | --- | --- | --- |
| Skitter | chitin | membrane | see `Skitter_report.json` → `detail` |
| Queen | chitin | membrane | see `Queen_report.json` |
| Crystal_A / B / C | crystal | rock (the base) | see the reports |
| Rock_A / B / C | rock | crystal (fused shards) | see the reports |
| Burrow_A | rock | chitin (ribs) | see the reports |

Each `<Asset>_report.json` lists `maps`, the `uv` stats (islands, coverage, world units per UV unit) and `detail`:
per family the shared map, its UV tiling and its mask channel. The tiling is computed so a tile covers the same
patch of surface on every asset (`detailmaps.TILE_WORLD`: chitin 0.06, membrane 0.08, crystal 0.07, rock 0.12
Skitter units; the Queen's detail is scaled by its detail factor 3).:

| Asset | Primary (B) tiling | Secondary (A) tiling | Islands | UV coverage |
| --- | --- | --- | --- | --- |
| Skitter (LOD1 shares it) | chitin 13.7 | membrane 10.3 | 31 (mirrored) | 0.61 of the half |
| Queen | chitin 24.2 | membrane 18.1 | 91 (mirrored) | 0.66 |
| Crystal_A / B / C | crystal 22.9 / 20.8 / 25.9 | rock 13.4 / 12.1 / 15.1 | 93 / 43 / 165 | 0.57 / 0.68 / 0.59 |
| Rock_A / B / C | rock 14.5 / 15.9 / 14.8 | crystal 24.9 / 27.3 / 25.3 | 29 / 68 / 41 | 0.63 / 0.64 / 0.64 |
| Burrow_A | rock 44.2 | chitin 88.5 | 277 | 0.54 |

In Unity the tiling goes into the detail map's scale (`_DetailNormalMap_ST.xy`). A value off by 20 % is invisible;
one value per family for all assets would also do if per-asset tiling is too fiddly (chitin 16, membrane 12,
crystal 22, rock 15), except the Burrow, whose big ground disc needs its own.

Removed in round 3: the separate `_AO.png` and `_Roughness.png` (now packed into the Mask).

### Shared detail normals (`Exports/Detail/`)

`Detail_Chitin_Normal.png`, `Detail_Membrane_Normal.png`, `Detail_Crystal_Normal.png`, `Detail_Rock_Normal.png`:
512, seamless, tangent space, linear (import as Normal map, Wrap Mode Repeat, mipmaps on, Aniso 2+). Content is
isotropic only (fine punctures and cuticle cells, veinlets and wrinkles, frost pits and micro-fractures, grain, pits
and hairline cracks) because packed islands are rotated; directional detail (growth lines, striations, strata) is in
the per-asset normal map. Their job is close-ups (cutscenes, the map zoom): at the default game zoom the mipmaps
average them out, which is intended, so they never add noise at ~20 pixels.

## Round 4: Roller, Cragback, Prowler (2026-11-03)

Design notes in `DESIGN_NOTES.md` (round 4). Builds: `build_roller.py`, `build_cragback.py`, `build_prowler.py`
(also run by `build_all.py`). Shared code added for them:

- `common.PartBuilder.slab`: a closed thick plate between two parametric surfaces (the Roller's lune plates).
- `common.Limb`: two- or three-bone limbs with a pole (sprawling stone legs, digitigrade runner legs), solved
  analytically; `common.aim_matrix` keeps the bend plane so limbs do not roll. `common.step_curve` is the shared
  stance/swing foot curve.
- `common.skin(..., rigid=..., blend=n)`: armour parts follow one bone only and meet their neighbours in an n-ring
  soft crease (the Roller's plates, the Cragback's slabs), so plates never bend and the stretch happens in the seams.
- `common.build_lod`: a generic LOD1 (drop bones into a parent, aim one bone over a folded chain, key it from the
  full rig every frame). The Skitter keeps its own older version.
- `common.key_clips`, `common.set_action`, `common.game_view_mixed` (copies with different clips and frames, e.g.
  walking and rolling Rollers), `common.strip_sheet`, `common.flat_proxy` / `game_view_static` (a primitive
  stand-in of the old `EnemyModelFactory` look for the compare sheets).
- `stone.py`: stone styles (tectonic slab, hot core with fissures, basalt pillar drums, stone spikes).
  `chitin.glow_flesh`: lit flesh between armour ribs. `crysium` palette `ember`: Crysium grown through the stone
  layer, same shapes, warm glow (the Stone species' colour code).

Detail tiling (report → `detail`): Roller chitin 30.8 / membrane 23.1, Cragback rock 12.4 / crystal 21.3 (Mask B =
rock, A = crystal), Prowler chitin 18.4 / membrane 13.8.

### Unity notes beyond the Skitter's

- **Roller curl and roll are clips now.** `EnemyView` today spins a procedural shell. With the model: play `Curl`
  (14 frames = 0.47 s, matches `CurlSeconds` 0.45) when `IsRolling` turns on, then loop `Roll`, and `Uncurl` when it
  turns off. `Roll` is one full turn of the ball in place (no root motion): set its playback speed from the ground
  speed, `speed = velocity / (roll_distance_per_loop × scale)` with `roll_distance_per_loop` 1.95 (ball radius
  0.31, ball centre 0.34 above the ground including a 0.02 lift so the hooks only graze). Do not also spin the
  transform. The seams' glow is in the map: it opens up by itself when the plates spread.
- **Gait playback from speed:** loops are in place; scale clip speed by `velocity / (distance_per_loop × scale)`:
  Roller Walk 0.48, Cragback Walk 0.49, Prowler Run 1.19 (model units, see each report).
- **Cragback footfalls** at Walk frames 0 and 16 (dust puffs, thud). **Death emission fade:** animate the material's
  `_EmissionColor` (via a property block) from its normal value to 0 between Death frames 8 and 40 (0.27 s to
  1.33 s), ease-out; the veins, faults, eyes and Crysium all live in the emission map, so one fade covers them.
- **Families per asset:** the Cragback's Mask B is rock and A is crystal (not chitin/membrane): it needs the rock and
  crystal detail normals.
- **Scale:** at their current `EnemyDefinition` scales the new models are about the old size: Roller 0.7 (1.5 long
  walking, ball 0.62 across), Cragback 0.85 (1.0 long, 0.75 high to the shard tips), Prowler 0.62 (1.4 long with
  the tail). Check against the Skitter in the level.
- **Prowler rage:** Run is authored at 1.19 units per loop; rage speed (×1.6) only needs a faster playback.

## Round 5: Skyray, Shalebat, Wisp (air units, 2026-11-03)

Design notes in `DESIGN_NOTES.md` (round 5). Builds: `build_skyray.py`, `build_shalebat.py`, `build_wisp.py` (also
run by `build_all.py`). Shared code added for them:

- `common.PartBuilder.ring`: a closed ring built from overlapping tube arcs (the arc ends become UV seams), with a
  radius profile (beads) and a reference axis so patterns sit mirror-symmetric.
- `common.game_view` / `game_view_mixed` / `game_view_static`: `target` (camera aim) and `lift` (flyers at the
  game's air height 1.6, so the renders show the ground shadow the way the player sees it). `build_lod` no longer
  needs an `Idle` clip.
- `chitin.fin_sheet` (fin or wing membrane over raised rays: dark back, pale chevrons, glowing leading edge, pale
  underside), `chitin.gill_belly` (lit gill chevrons under a flyer), `chitin.bud_petal` and `chitin.ring_band`
  (the Wisp's petals and rings), `stone.skin` (crazed stone membrane with glowing craze lines).
- Thin sheets (fins, wing skin) are built as an upper and a lower slab, so the rim is a UV seam and each half
  unrolls flat.

| Asset | Triangles | Bones | Clips (frames @30 fps) |
| --- | --- | --- | --- |
| Skyray | 3,900 | 22 | Fly 42 (loop), Idle 60 (loop), Hit 12, Death 40, Dive 24 |
| Skyray_LOD1 | 1,700 | 12 | same five clips |
| Shalebat | 3,900 | 16 | Fly 16 (loop), Glide 48 (loop), Hit 12, Death 36 |
| Shalebat_LOD1 | 1,700 | 10 | same four clips |
| Wisp | 2,400 | 14 | Float 60 (loop), Land 24, Grounded 60 (loop), Hit 12, Death 30 |
| Wisp_LOD1 | 1,000 | 7 | same five clips |

Detail families (Mask B / A): Skyray membrane / chitin, Shalebat rock / membrane, Wisp chitin / membrane; tiling in
each report's `detail`.

### Unity notes beyond the earlier ones

- **Pivot.** Skyray and Shalebat: the armature origin is the body centre (place it at the flight height, as the
  factory bodies are). Wisp: the origin is on the ground; Float holds the bud 0.42 up, Grounded 0.24. With the
  model, set the Wisp's `HoverHeight` to 0 (the clip carries the float) or keep only the small bob.
- **Wing and fin motion is in the clips.** Turn the `SurfaceWave` property blocks off for these meshes (or keep a
  very light one on the Skyray fins only); the procedural wing/fin hinge rotation in `EnemyView` must not run on
  the model. Fly loops are in place; Skyray beat 1.4 s (matches `FinBeatSpeed` 4.5), Shalebat flap 0.53 s.
- **Skyray Dive** is the tail strike for `AttackStyle.Whip`: the tail hits at frame 10 (0.33 s), play it on the
  gate strike. **Shalebat** has no spit clip yet: the Hit clip's jaw drop can stand in, or ask for a Spit clip.
- **Death.** Both flyers' Death clips roll the body in place (Skyray a turn and a quarter, Shalebat 300 degrees)
  and fold the wings; Unity's `Crash` still moves them to the ground, but drop its `CrashSpin` (or lower it a lot)
  while the clip plays, or the two spins add up. The Wisp's Death bursts the petals open and drops the rings to the
  ground (no root motion); run the existing `Burst` flash near frame 10 to 14.
- **Wisp landing.** Play Land (0.8 s) when it becomes a ground unit, then loop Grounded; for take-off play Land
  backwards. The rings' spin is keyed (one turn per 2 s each, opposite ways): do not also spin them in code.
- **Wisp VFX** (not in the mesh): an additive halo billboard on the heart in the species colour, the ground light
  pool (exists), short arcs between the heart and the ring nodes, motes orbiting with the rings, a dust ring and a
  ground glow on Land, and the Burst sparks on Death.
- **Shadows.** Flyers need shadow casting on (the renders show the ground shadow from the key light); keep the
  belly visible to the shadow pass (no special settings: one opaque material).

## Round 6: the hero, Cole "Pathfinder" Mercer (2026-11-03)

Design note in `DESIGN_NOTES.md` (round 6). Build: `build_mercer.py` (also run by `build_all.py`); it builds his
weapons first (`build_mercer_weapons.py`, which also runs on its own). Previews come from `mercer_previews.py`.
Output: `Exports/Mercer/` (body, `Low/` 512 maps) and `Exports/Mercer/Weapons/` (weapons), `Blend/Mercer.blend` (body and
weapons; `Blend/MercerWeapons.blend` when the weapons script runs on its own).

Shared code added (all optional, the Burrower assets build exactly as before):

- `suit.py`: styles for our own side (padded quilted fabric, ceramic plates, gunmetal, rubber, visor glass, light
  strips), `suit.wrap` (a plate wrapping one side of a limb tube, the rest of the tube stays cloth) and a weathering
  pass (scuffed plates, grime, dust up the shins, pale pink Crysium dust with faint glints on the boots).
- `surface.SUIT_FAMILIES` (`weave`, `ceramic`, `metal`, `rubber` in the Fam channels) with their micro-detail:
  stitched quilting seams and fabric grain, scratched and chipped plates (damage reads darker on white), brushed
  metal with bright scratches, moulded rubber. `surface.build(..., family_set)` and
  `common.build_mesh_asset(..., family_set=, edge_gain=, border_smooth=)` select them.
- `common.smooth_borders` (`border_smooth=n`): blends the style outputs across part borders so plates meet cloth in a
  clean line instead of a voxel zigzag in close-ups.
- Two new tiling detail normals: `Detail_Weave_Normal.png` (ripstop weave, tile 2.5 cm) and
  `Detail_Ceramic_Normal.png` (micro-scratches, tile 6 cm). `detailmaps.ensure()` now builds only missing maps.

### Budgets

| Asset | Triangles | Bones | Maps (Low) | Notes |
| --- | --- | --- | --- | --- |
| Mercer (LOD0) | 9,800 (budget 10,000) | 31 | 1024 (512), one material | mirrored UVs (right half baked) |
| Mercer_LOD1 | 4,000 | same 31 (same rig, same FBX) | same maps | for a LODGroup |
| Pistol + Pistol_Mag | 1,250 + 120 (budget 1,500) | – | `MercerWeapons_*` 512, shared | Pistol_Mag_Quick 200 (Quick Reload) |
| Sword_Hilt / Sword_Hilt_Wide | 760 / 800 (budget 800) | – | same 512 set | Wide = Wide Slash, forked emitter |
| Sword_Blade / Sword_Blade_Wide | 360 / 360 | – | none (emissive shader) | UV0.x 0 core..1 rim, UV0.y 0 base..1 tip |

Mask families: body B = weave, A = ceramic (tiling in `Mercer_report.json` -> `detail`, about weave 60 and ceramic
25 per UV unit); weapons B = metal (no tiling map), A = ceramic (`MercerWeapons_report.json`).

### Rig and sockets

`Root, Hips, Spine1, Spine2, Neck, Head`; per side `Clavicle, UpperArm, Forearm, Hand, Fingers, Thumb, Thigh, Shin,
Foot, Toe`; sockets `Weapon_L, Weapon_R` (in the fists), `Holster_L, Holster_R` (outer thighs), `Back` (the hilt
clip on the pack, right side). Sockets are exported as bones without weights. `_L` is the figure's left.

**Grip space.** Every weapon is modelled in its socket's frame: origin the centre of the fist, **+Z the barrel
(pistol) or the cutting edge (sword), +Y the top of the pistol (thumb side) and the blade of the sword.** Parent a
weapon to its socket with localPosition 0 and localRotation identity. Pistol muzzle at (0, 0.062, 0.192); sword
blade from y = 0.118 to the tip at y = 0.978. Holstered: the pistol hangs barrel-down in `Holster_R` (`Holster_L`
for Dual Wield), the hilt rides in `Back` without its blade. The left fist also aims the repair beam: in Repair,
`Weapon_L`'s +Z points at the target (beam origin = `Weapon_L`). Check once after import: the pistol must lie
along the forearm with its slide up in the Shoot clip; if it is turned, the FBX axis settings differ from the
ones below.

### Clips (30 fps, in place, no root motion)

| Clip | Frames | Loop | Layer | Events |
| --- | --- | --- | --- | --- |
| Idle | 72 | yes | full | |
| Walk | 32 | yes | full | 0.97 units per loop |
| Run | 16 | yes | full | 2.53 units per loop (flight phase) |
| Shoot | 14 | no | upper | muzzle flash frame 0 (recoil peaks at 2) |
| Reload | 36 | no | upper | magazine out frame 4, in frame 23 |
| Slash | 22 | no | upper (authored over the stance) | hit frame 10 |
| Shockwave | 30 | no | full | impact frame 13 (fist on the ground) |
| Repair | 48 | yes | full (kneeling) | beam from `Weapon_L` |
| Hit | 12 | no | upper or full | |
| Down | 40 | no | full | hits the ground at 26, ends lying on the right side |
| Revive | 36 | no | full | from Down's last frame to Idle's first |

Loop playback speed: `animator speed = velocity * clip_seconds / distance_per_loop` (Run: 5 u/s * 0.533 s / 2.53 =
1.05; Walk for slow moves and cutscenes). **Upper-body mask:** `Spine1` and everything below it (spine, neck, head,
clavicles, arms, hands, fingers, `Weapon_*`, `Back`); the legs, `Hips`, `Root` and the holsters come from the base
layer. Shoot, Reload and Slash keep the chest square to the hips (net chest yaw 0), so they layer over Walk and Run;
the aim at a target is a code yaw on `Spine1`/`Spine2` after the Animator (the old view's free-turning torso).

Down rolls the body about the feet to the right (the hips end about 0.85 to the hero's right of the origin); keep
the beacon, ring and bar on the transform as today.

### Unity import (adds to the rules below)

Same FBX settings as the creatures (Scale 1, Bake Axis Conversion on, Generic rig, root `Root`, Mikktspace
tangents). The figure is at game scale: 1.56 units to the helmet top in the bind pose (about 1.53 in his stance),
about the old figure (0.95 x 1.6),
so its transform scale is 1 (drop `FigureScale`). Materials: one `Game/UnitLit` material for both LODs with the
four `Mercer_*` maps; one for the weapons with `MercerWeapons_*`; the blade needs an additive/emissive shader that
reads UV0.x as the core-to-rim falloff (cyan, `Weapon_EnergySword` colour) and UV0.y for a tip fade. Emission
x 2.5. Powered down (Down): fade `_EmissionColor` to 0 (visor, cells, strips go dark), as the old dead materials did.

How `HeroView` should switch: keep `HeroView`'s logic, beacon, ring, bar and particles, and replace the jointed
primitive figure with a `HeroModelView` that instantiates the imported prefab and drives an Animator: base layer
Idle / Walk / Run by speed (Walk below about 1.5 u/s); an upper-body layer (mask above) with Shoot, Reload and Slash
triggers; full-body states Shockwave, Repair (bool), Hit, Down (bool) and Revive. The weapon rigs attach their meshes
to `Weapon_R` / `Holster_R` / `Back` instead of the forearm and pack; Quick Reload swaps `Pistol_Mag` for
`Pistol_Mag_Quick`, Wide Slash swaps the hilt and blade for the `_Wide` meshes. `CutsceneHeroRig` can sample the same
clips by time (`AnimationClip.SampleAnimation`) so a sought moment matches a played one; `PortraitModels.HeroBust`
instantiates the model in Idle (frame 10) and frames head and shoulders 3/4 from the figure's right
(`mercer_bust.png`: camera aimed at y 1.33, 1.45 units away, 70 mm lens, 28 degrees off the front).

## Plugging into Unity later (not done in this pilot)

The FBX files replace only the view side. `EnemyView` (and the prop builders) keep their logic; a new view component
instantiates the imported model instead of building primitives in `EnemyModelFactory`.

- **Import:** Scale Factor 1, Convert Units on, **Bake Axis Conversion on** (every FBX keeps the axis change on its root
  node). Creatures face +Z in Unity. Rig: Generic, root node `Root`. Animation: import all takes, loop Walk and Idle,
  rename clips to their action name (`Walk`, `Idle`, `Hit`, `Death`, `Scream`). No root motion (walk in place; the
  `Mover` moves the creature). Normals: Import; Tangents: Calculate Mikktspace (the mirrored UVs rely on the
  tangent sign; Blender baked with MikkTSpace as well).
- **Scale:** the Skitter is at the size of today's factory model (about 1.13 long), so its `EnemyDefinition` scale
  0.45 still applies. The round-2 Queen is bigger than round 1 (2.9 high to the crown tip, 4.3 long with the
  scythes and sac); the previews use 2.7 × boss factor 0.6, and the Unity side may want a slightly smaller factor.
- **Texture import:** Albedo and Emission sRGB; Normal as Normal map; Mask with sRGB **off** and alpha as-is
  (not "alpha is transparency"). Low (Switch): use the `Low/` maps, or a platform max-size override of 512 (same
  result).
- **Death:** the Death clip ends on the back with the legs curled. The existing fade or sink then runs as it does now.

### What `Game/UnitLit` needs to use these maps

Today the shader (`Assets/_Project/Art/Shaders/UnitLit.shader`) reads `_BaseMap`, `_BumpMap` (+ `_BumpScale`),
`_EmissionMap` (+ `_EmissionColor`) and a constant `_Smoothness`; occlusion is fixed at 1. Wiring that works now:
`_BaseMap` = Albedo, `_BumpMap` = Normal, `_EmissionMap` = Emission with `_EmissionColor` white × 2.5 (creatures)
or × 2.2 (props), `_Smoothness` about 0.3 for chitin, 0.25 for rock, 0.55 for crystal (the Mask is ignored).

To use everything, the shader needs these slots (names follow URP Lit so the material inspector feels familiar):

| Property | Type | Use |
| --- | --- | --- |
| `_MaskMap` | 2D, linear, default white | packed Mask: R occlusion, G smoothness, B detail mask, A second detail mask |
| `_OcclusionStrength` | Range 0..1, default 0.5 | `surfaceData.occlusion = lerp(1, mask.r, _OcclusionStrength)`. Keep it about 0.5: the albedo already carries the AO, so full strength would darken cavities twice (the AO slot then mainly shapes ambient light) |
| `_Smoothness` (existing) | Range 0..1 | becomes a multiplier: `smoothness = mask.g * _Smoothness` with default 1 when `_MaskMap` is set (keyword `_MASKMAP`) |
| `_DetailNormalMap` | 2D, Normal map, default "bump" | primary family detail normal, sampled with its own `_ST` (tiling from the report) on UV0 |
| `_DetailNormalScale` | Float, default 1 (0.6–1.0 looks right) | strength of the detail normal |
| `_DetailNormalMap2` + `_DetailNormalScale2` | optional, same as above | second family (membrane, rock base, ribs), weighted by mask.a. Leave out on Low (Switch) |
| keywords | `_MASKMAP`, `_DETAIL` (and `_DETAIL2`) | shader_feature_local so assets without them pay nothing |

Blending (what the previews do): `detail = UnpackNormalScale(sampleDetail, _DetailNormalScale * mask.b)`, then
whiteout: `normalTS = normalize(float3(base.xy + detail.xy, base.z * detail.z))`; repeat for the second detail with
mask.a. Cost: one extra texture fetch for the Mask, one per detail map. The Low profile can drop `_DETAIL2` (and even
`_DETAIL`) without the look falling apart at game zoom, since the detail maps only matter close up. The
`DepthNormals` pass may keep the base normal only.

## Licence

All meshes, textures, rigs and animations here are generated by our own scripts in `Tools/Blender/`, without
third-party models, textures or scans. They belong to the project like the code does. Blender (GPL) is only the tool;
its output carries no licence restrictions.

## Round 6: P1 level bosses (BroodMother, PackAlpha, StormMatriarch, CoreWisp, 2026-11-03)

Design notes in `DESIGN_NOTES.md` (round 6). Builds: `build_broodmother.py`, `build_packalpha.py`,
`build_stormmatriarch.py`, `build_corewisp.py` (not yet in `build_all.py`). Boss helpers live in their own module,
`boss_kit.py`, so the shared modules stay untouched:

- `ScalePoser`: a `common.Poser` that also keys bone **scale** (`{"scale": s}` in a spec) for organs that swell:
  the BroodMother's side sacs and birth vent, the CoreWisp's heart and three rings. Only the bones listed get
  scale curves; they are leaves, so nothing inherits the scale. `boss_kit.build_lod` keeps those keys on the LOD.
- `append_base`: appends the base species from `ArtSource/Blend/<Species>.blend` for side-by-side renders.
- `boss_previews`: the standard boss sheet (3/4, side, front, top, power pose, death, strips, LOD1, macros with
  and without detail normals, game view among the species at play zoom and zoomed, boss vs base at the same scale).
- `palette_variant` (`--palette <name>`): StormMatriarch and CoreWisp take their colours from one `PALETTES` table
  at the top of the script (`cool` = default = the base species' colours, `warm` = Remco's open question). A
  non-default palette bakes into `<preview>/_variant_exports` and never touches `Exports/` or `Blend/`; it renders
  `<boss>_warm_*.png` and `<boss>_palette_compare.png` (left the default, right the variant).
- `scars`: glowing healed scar lines as an `extra` surface pass (PackAlpha).

| Asset | Triangles | Bones | Clips (frames @30 fps) |
| --- | --- | --- | --- |
| BroodMother | 14,506 | 29 | Walk 48 (loop), Idle 80 (loop), Hit 14, Death 48, Spawn 60 |
| BroodMother_LOD1 | 6,000 | 21 | same five clips |
| PackAlpha | 14,018 | 24 | Run 20 (loop), Idle 72 (loop), Hit 14, Death 40, Howl 54 |
| PackAlpha_LOD1 | 6,000 | 18 | same five clips |
| StormMatriarch | 13,446 | 26 | Fly 56 (loop), Idle 72 (loop), Hit 14, Death 48, Release 54 |
| StormMatriarch_LOD1 | 6,000 | 16 | same five clips |
| CoreWisp | 8,992 | 22 | Float 72 (loop), Land 30, Grounded 72 (loop), Hit 14, Death 36, Pulse 40 |
| CoreWisp_LOD1 | 4,000 | 11 | same six clips |

All four: one material, 1024 maps plus the 512 `Low/` set, Mask B/A = chitin/membrane (StormMatriarch
membrane/chitin), tiling in each report's `detail`. The CoreWisp stays under the boss budget on purpose (like the
Wisp, its look is mostly light and VFX).

### Unity notes for the bosses

- **Scale.** Each model is built so that at Unity scale `EnemyDefinition.Scale` x `BossScaleFactor` (0.6) it is as
  big next to its species as the factory boss: BroodMother 0.78, PackAlpha 0.78, StormMatriarch 0.72, CoreWisp 0.78
  (`unity_scale` in each report).
- **Scale curves.** BroodMother (`Sac_L`, `Sac_R`, `Vent`) and CoreWisp (`Heart`, `Ring_A/B/C`) animate bone scale.
  The Generic rig imports scale curves as they are; do not turn on "Remove constant scale curves" for these clips.
- **Power timing** (start the ability's VFX and the spawn on these frames; the clip is the "wind-up and release"):
  - BroodMother `Spawn`: broodlings appear at frame **40** (1.33 s) at the `Vent` bone (rear tip, near the
    ground). Play the clip when `BossAbilities` fires Brood; delay the actual spawn by 1.33 s or start the clip
    1.33 s early. VenomBurst has no own clip: the Hit clip's jaw flare or Idle will do.
  - PackAlpha `Howl`: peak at frame **22** (0.73 s), held to frame 42; start the Rally ring and the howl sound at
    the peak.
  - StormMatriarch `Release`: the flyers leave at frame **30** (1.0 s) from about 0.25 above the `Body` bone (the
    open hatches). The data still has her as a bat boss that calls Mossbats; the model follows `MODEL_TODO.md`
    (a Skyray boss releasing small flyers), so the minion type is a data decision.
  - CoreWisp `Pulse`: the shock leaves at frame **14** (0.47 s): start the CorePulse ring there. Fission (split on
    death) uses the Death clip: spawn the wisps around frame 10 to 14, when the petals burst.
- **Gait playback from speed:** loops are in place. BroodMother Walk covers 0.513 model units per loop, PackAlpha Run
  1.907 (see the reports); scale clip speed by `velocity / (distance_per_loop x scale)`.
- **Pivots.** BroodMother and PackAlpha: on the ground. StormMatriarch: the body centre (flight height), like the
  Skyray. CoreWisp: on the ground, Float holds the bud 0.84 up and Grounded 0.5 (`float_height` /
  `grounded_height`); as for the Wisp, play Land when it becomes a ground unit and the rings' spin is keyed.
- **Flyers.** As for the Skyray: turn the `SurfaceWave` fin wave off for the StormMatriarch, drop `CrashSpin` while
  its Death clip rolls it, keep shadow casting on.
- **Rigid parts.** The StormMatriarch hatches and the CoreWisp petals and rings follow one bone each (they open and
  spin without bending).

## Round 7: P1 towers, build pad, stars and wreck (2026-11-03)

Design notes in `DESIGN_NOTES.md` (round 7). Build: `build_towers.py` (`--only Gun,Cannon,Laser,Flak,GateGun,Wreck`,
`--norender`, `--nobake`), 5 to 10 minutes with previews. Hard-surface helpers live in their own module,
`tower_kit.py`; the shared modules are only imported.

**How a machine is made (different from the creatures):** no remesh and no high-poly source. Pieces (chamfered
blocks, lathed bodies, tori, extruded plates) are written at game resolution, each rigid on one bone and tagged with
the tiers it belongs to. The union of all tiers' pieces is unwrapped once (one atlas per tower, so all tiers share
one map set), then a Cycles shader on the game mesh **self-bakes** the maps: rounded edges from the Bevel node,
chipped paint on the edges, panel seams and bolt heads in each piece's own frame, hazard stripes, ring grooves,
scratches, AO grime, rain streaks and dust on the foot. During the bake each tier's pieces are moved apart, so one
tier never shadows another tier's pieces that occupy the same space. LOD1 rebuilds every piece without chamfers,
drops tiny pieces (slats, bolts) and maps its UVs onto the LOD0 islands, so it shares the maps.

| Asset | Tiers | Triangles LOD0 (LOD1) | Clips (frames @30 fps) |
| --- | --- | --- | --- |
| Tower_Gun | T1, T2, T3A rotary, T3B marksman | 3,040 / 4,092 / 4,276 / 3,984 (1,420 / 2,112 / 2,000 / 1,852) | IdleSweep 120 (loop), Fire_n 10, T3A Spin 12 (loop) |
| Tower_Cannon | T1, T2, T3A twin, T3B siege | 2,756 / 3,468 / 4,080 / 3,496 (1,120 / 1,448 / 1,836 / 1,444) | IdleSweep, Fire_n 10 |
| Tower_Laser | T1, T2, T3A overcharge, T3B long focus | 3,202 / 3,818 / 4,210 / 3,990 (1,494 / 1,710 / 2,142 / 1,962) | IdleSweep, ChargeUp 24, Beam 12 (loop) |
| Tower_Flak | T1, T2, T3A quad, T3B long | 3,356 / 4,088 / 4,928 / 3,980 (1,576 / 2,092 / 2,580 / 2,000) | IdleSweep, Fire_n 10, FireAlternate 16 (loop) |
| Tower_GateGun | T1, T2, T3 | 1,736 / 2,216 / 2,412 (652 / 956 / 1,056) | IdleSweep, Fire_n 10 |
| Tower_Wreck | – | `Wreck` plus `Debris_<Tower>` | static |
| Tower_Star | – | one star (also inside every tower mesh) | static |

Files per tower: `Exports/Tower_<Name>/Tower_<Name>_<Tier>.fbx` (meshes `Tower_<Name>_<Tier>` and
`..._LOD1`, both skinned to the armature `Tower_<Name>_Rig`, plus the clips), `Tower_<Name>_Albedo/_Normal/_Emission/
_Mask.png` (1024, shared by all tiers), `Low/` (512), `Tower_<Name>_report.json` (tris, bones, clips, muzzle
positions, detail tiling). Mask: R AO, G smoothness, **B metal detail, A ceramic detail**. New shared map:
`Exports/Detail/Detail_Metal_Normal.png` (made by `tower_kit.metal_detail_map`, tile 0.05 units); the ceramic one is
the existing `Detail_Ceramic_Normal.png`.

### Rig (the same on every tower)

| Bone | Parent | Deforms | Use |
| --- | --- | --- | --- |
| `Root` | – | yes | the pad (static); the Gate Gun's saddle clamp |
| `Star_1..3` | Root | yes | one star each, on the rank plate facing the camera. Hide a star by scaling its bone to 0 |
| `Yaw` | Root | yes | turns the head; points up, so the turn is about its own long axis (local Y) |
| `Smoke` | Yaw | no | socket for damage smoke and sparks |
| `Pitch` | Yaw | yes | elevation at the trunnion; points along +X in Blender (the trunnion axis), so it also turns about its own long axis |
| `Barrel_n` | Pitch | yes | recoil (move along its own -Y), spin for the rotary gun (about its own Y) |
| `Muzzle_n` | Barrel_n | no | shot origin and flash; local +Y points out of the barrel |
| `Prong_1..3` | Barrel_1 | yes | Laser T2/T3A focusing prongs (ChargeUp) |

### Unity notes

- **Import:** as the creatures (Scale 1, Convert Units, Bake Axis Conversion, Generic rig, root `Root`, all takes,
  Normals Import, Tangents Mikktspace). Keep non-deforming bones (the sockets). The pivot is the pad's centre on the
  ground (Gate Gun: the top centre of the gate crossbeam). The models are at final size: do not apply
  `TowerModelFactory.TowerScale`, `PedestalScale` or `LevelGrowth` (the tiers grow by themselves).
- **The base must not turn:** the rank plate with the stars faces Unity -Z (the camera side). `TowerView` turns only
  the `Yaw` bone; the tower's root transform stays unrotated.
- **Aim:** drive `Yaw` and `Pitch` in code (LateUpdate if an Animator plays the clips): rotate `Yaw` about the tower's
  up axis and `Pitch` about the `Yaw`'s right axis in world space, on top of their rest rotations
  (`bone.rotation = Quaternion.AngleAxis(a, axis) * restWorldRotation`). Flak rests at 40 degrees elevation (that is
  its modelled pose). `TowerView.MuzzleProvider` should return the `Muzzle_n` of the barrel that fired; for twin and
  quad guns alternate n. Recoil can stay in code (move `Barrel_n` along its local -Y by the distance in the clip:
  Gun 0.0375, Cannon 0.0875, Flak 0.05, Gate Gun 0.025) or play `Fire_n` on an additive layer.
- **Tier swap:** each tier is its own prefab (one SkinnedMeshRenderer + LODGroup with LOD0/LOD1). On a level change
  swap the prefab, or keep all tier prefabs under the tower and enable one. T3A is the level-3 look until the data
  has A/B paths. Stars: scale `Star_k` to 0 for k > level + 1, and all to 0 on a wreck.
- **Wreck:** on `IsWrecked` hide the tier model and show `Tower_Wreck` (`Wreck` + the tower's `Debris_<Tower>`), keep
  the smoke at the old `Smoke` socket height. Rebuild swaps back.
- **Material:** `Game/UnitLit` with Albedo, Normal, Emission (`_EmissionColor` white x about 1.5; the glow colour is
  in the map), Mask and the metal/ceramic detail normals (tiling in the report). One material per tower, shared by its
  tiers. Damage smoke and sparks stay VFX (`ParticleKit`).

## Round 8: Crystal Plains level environment kit (2026-11-03)

Design notes: `DESIGN_NOTES.md`, round 8. Scripts (new, the shared modules are untouched): `env_kit.py` (helpers),
`build_env_materials.py` (tiling sets, numpy, about 40 s), `build_env_kit.py`, `build_env_rift.py`,
`build_env_decor.py` (meshes, the usual bake pipeline, 5 to 10 min each) and `build_env_diorama.py` (renders a
12 x 8 cell level from the exports; run it last). Output: `Exports/Env/`, `Blend/Env_*.blend`.

### Tiling material sets (`Exports/Env/Tiling/<Set>/`, 1024, Low 512 in `Low/`)

| Set | Tile covers (units) | Maps | Look |
| --- | --- | --- | --- |
| Road | 2 | Albedo, Normal, Mask | pale fused slab plates (value about 0.5), dark joints, claw scuffs |
| Plaza | 3 | + Emission | square slabs, glowing engraved ring sigil (one per tile) |
| Ground | 3 | + Emission | dark basalt crackle (value about 0.17), gravel, glinting Crysium grit |
| GroundFlora | 2 | + Emission | plum lichen polyps with pink and lime pinpoints (a splat layer) |
| Cliff | 3 | + Emission | wall rock, V up: strata ledges, columnar joints, thin veins |
| RiftRock | 2 | + Emission | near-black glassy basalt with amber fissures |

Mask of a tiling set (differs from the props): **R** AO, **G** smoothness, **B** height 0..1 (for height
blending), **A** Crysium-dust mask (sparkle). Normal: tangent, +Y up. Seamless by construction; the 3 x 3 test
renders are `env_tiling_<set>.png`.

### Meshes (`Exports/Env/<Asset>/`: FBX, 512 Albedo/Normal/Emission/Mask as for the props, report)

| Asset | Tris | Footprint | Placement frame |
| --- | --- | --- | --- |
| RoadEdge_Straight | 700 | one cell side | pivot mid-side, road on local -Y |
| RoadEdge_OuterCorner / _InnerCorner | 500 / 400 | corner point | road quadrant (-X, -Y) / ground quadrant (+X, +Y) |
| RoadEdge_EndCap | 500 | one cell side | as Straight; the kerb ends towards +X |
| RoadEdge_PlazaJoin | 600 | one cell side | as Straight; plaza kerb on +X (mirror X when the plaza is on -X) |
| PlazaEdge_Straight / _Corner | 300 / 400 | side / corner point | as the road pieces (one corner post for both cases) |
| BuildPadTile | 200 | 0.92 x 0.92, 0.045 high | cell centre; the tower stands on top (+0.045) |
| GatePlinth | 1,500 | 3.2 x 1.0 | gate centre; pylon footings at X = +-1 (two-cell road), road along Y |
| Cliff_Straight | 1,500 | 2 cells, 2 high | pivot on the field edge, field on -Y |
| Cliff_InnerCorner / _OuterCorner | 1,500 / 1,498 | corner point | field quadrant (-X, -Y) / wall quadrant (+X, +Y) |
| Chasm_Edge | 1,498 | 2 cells, drops 2.4 | field on -Y, void on +Y |
| Bank_Straight / _OuterCorner / _InnerCorner | 500 / 400 / 300 | side / corner | liquid on -Y / liquid quadrant (-X, -Y) / dry quadrant (+X, +Y) |
| Slope | 600 | one cell | low at -Y, 0.55 (PlateauHeight) at +Y |
| TerraceStep / _Outer / Ledge | 700 / 800 / 498 | side / corner / side | plateau on +Y / plateau quadrant (+X, +Y) / lip at z 0.55 |
| Rift_1x1 / _2x1 / _3x1 / _Cap | 900 / 1,200 / 1,500 / 800 | 1-3 cells along X | chain along X; Cap is full at -X, closed by +0.3 |
| CrystalVein | 300 | 2 x 0.6 | lies on the ground, any yaw |
| Scree_Patch | 600 | 0.95 x 0.7 | any yaw |
| Scatter_Pebbles / _CrystalSplinters / _ShellFragments | 150 / 198 / 200 | 0.2-0.35 | instancing, any yaw and 0.7-1.3 scale |
| Decor_Boulder / Column / Spike / Spire | 1,200 / 1,000 / 1,200 / 1,500 | 0.8-1.3 wide; Spire 2.9 high | PropKind Boulder, Column, Spike, Spire |
| Flora_Frond / Tendril / GlowTree / Mushroom | 1,300 / 1,300 / 1,500 / 1,400 | 0.5-1.2 wide | PropKind Frond, Tendril, GlowTree, Mushroom; vertex colour `Sway` |
| Egg_Cluster | 1,300 | 0.84 x 0.75 | near nests |
| Silhouette_CrystalTower / Ridge / Mesa | 900 / 1,200 / 1,000 | 5.7 x 10.7 / 24 x 5.8 / 7.4 x 6.6 | far outside the field, in the mist, scale 1 |

### How the level code should consume the kit (proposal, nothing imported yet)

- **Ground** (`TerrainSurfaceBuilder`): one terrain shader that samples Ground, GroundFlora, Road and Plaza in world
  space (`uv = worldXZ / tile size`) and blends them by a splat map the painter already knows (road mask,
  plaza cells, a flora noise mask), with a height blend: `w = saturate((splat + (h_layer - h_base) * 0.35 - 0.5)
  * sharpness + 0.5)`, sharpness about 7 for road and plaza, 4 for flora. Grit then fills the road joints first
  and the edge breaks up naturally. Cliff and RiftRock are for triplanar walls (the rim hills, the rift walls).
  Keep the current painted texture as a low-frequency tint over it (theme colour per level). Align the Plaza
  UV origin to each plaza's centre cell so the sigil sits in the middle of the plaza.
- **Kerbs per cell neighbourhood** (tested in the diorama): for each road or plaza cell and each of its four sides
  whose neighbour is not road (and inside the grid): place `PlazaEdge_Straight` if the cell is a plaza, else
  `RoadEdge_PlazaJoin` if the next cell along that edge line is a plaza whose same side is also open (+X towards
  the plaza, mirror X when needed), else `RoadEdge_Straight`, at the middle of that side, rotated with
  `LookRotation(-side)` (the piece's Unity +Z, Blender -Y, points back into the road cell). For each grid corner point count the road cells among its
  four: 1 = `RoadEdge_OuterCorner` (turn the road quadrant onto (-X, -Y)), 3 = `RoadEdge_InnerCorner` (turn the
  ground quadrant onto (+X, +Y)); `PlazaEdge_Corner` instead when any of the four is a plaza; 2 = nothing.
  Skip the sides of a gate's cells (the plinth's footings stand there) and put an `EndCap` on the side before.
  The painted road mask should follow the cell outline when kerbs are on (RoadShape's 0.6 cell corner rounding
  would leave ground inside the outer kerb corners).
- **Build pads:** `BuildPadTile` on every buildable cell (also on raised cells at +0.55), under the tower.
- **Gate:** `GatePlinth` at the gate's centre, X across the corridor (scale X by width / 2 for other widths).
- **Rift cells:** keep TerrainField's sink (or stop carving and cut the ground) and lay the rift pieces over each
  straight run: Cap (turned 180) + 3x1/2x1/1x1 + Cap. Bends and blobs are not in the kit yet.
- **Banks and terraces** follow the same side/corner rules as the kerbs, with the liquid or the low side as "road".
- **Border:** `Cliff_*` along sides that end in the rim, `Chasm_Edge` where the field should fall away (put a
  dark violet fog plane 3 units below it); silhouettes 6-15 units outside the field in the mist.
- **Decor** (`PropScatterBuilder` / `PropFactory`): swap the procedural PropKinds for the meshes one to one,
  keeping the existing scales and hashes; Rift rims get Scree_Patch and Rift shards; scatter the three Scatter
  meshes with GPU instancing (about one per two free cells), shells only near nests.
- **Sway:** flora carries `Sway` vertex colours (R bend weight 0 root to 1 top, G phase 0..1, B flutter). Vertex
  shader: `offset = windDir * R * amp * sin(t * speed + G * 2pi) + normal * B * flutterAmp * sin(t * 7 + G * 13)`,
  amp about 0.04 units, speed 0.7-1.3 (today's SwayDegrees 2.5 equivalent). This replaces the whole-object sway.
- **Emission:** kit maps are 0..1 masks in the glow colour; use about 2.2 (rift 2.4), theme Bloom as now. The
  rift glow is authored amber (theme Rift colour of the Stone tint).

## Round 9: gate, base, base turret, drones, gadgets (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 9). Same hard-surface pipeline as round 7 (`tower_kit.py` pieces, atlas,
self-bake, four maps, packed Mask R AO / G smoothness / B metal detail / A ceramic detail, shared
`Detail_Metal_Normal` and `Detail_Ceramic_Normal`). New helpers in `base_kit.py` (hex panels, shards, clips with
scale keys, module objects with their own pivot, comparison staging). Builds: `build_gate.py`, `build_base.py`
(`--only Base|Turret`), `build_drones.py`, `build_gadgets.py`; all take `--preview`, `--norender`, `--nobake`.

**Export note:** `base_kit.export_fbx` updates the view layer before `tower_kit.export_fbx`; without it objects made
with `bpy.data.objects.new` are not selected and the FBX holds only the armature (the round-7 bug).

| Asset | Files | Triangles LOD0 (LOD1) | Rig and clips |
| --- | --- | --- | --- |
| Gate | `Gate/Gate_G1..G3.fbx` (+ `_LOD1`, `_Damage` overlay), `Gate_Wreck.fbx` | 5,224 / 6,304 / 7,552 (1,364 / 1,660 / 1,996); overlay 1,148; wreck 2,068 | `Gate_Rig`, 30 bones: `Pylon_L/R`, `Beam`, `Hex_L1..9`, `Hex_R1..9`; sockets `GunMount`, `Field`, `HeroPost`, `Smoke_L/R`, `Spark_1..3`. Clips Repair 30, Build 45, Hit 8, Collapse 36 |
| Base | `Base/Base.fbx` (8 modules + LOD1 + socket empties), `Base_Damage.fbx`, `Base_Wreck.fbx` | 7,250 (3,190); overlay 1,188; wreck 3,720 | static; modules Deck, Command, Dish, Pad, Core, Crystal, Habitat, Hardpoints, each pivot on its foot |
| Tower_BaseTurret | `Tower_BaseTurret_T1..T3.fbx` | 3,204 / 4,256 / 4,440 (1,496 / 2,188 / 2,076) | as Tower_Gun (T3 = the rotary, mesh named `_T3A`) |
| Drone_Repair | `Drone_Repair/Drone_Repair.fbx` | 3,084 (1,972) | 8 deform bones: Root, Body, Rotor_1..4, ToolArm, Emitter; socket `Beam`. Idle 60, Fly 60, Work 60 |
| Drone_Harvest | `Drone_Harvest.fbx` (+ `Drone_Harvest_Cargo`) | 2,294 (1,262), cargo 340 | Root, Body, Rotor_1..2, Drill, Claw_1..3; sockets `Mine`, `Cargo`. Idle, Fly, Work |
| Drone_Attack | `Drone_Attack.fbx` | 2,208 (1,248) | Root, Body, Rotor_1..2, Gun_1..2; sockets `Muzzle_1/2`, `Thruster`. Idle, Fly, Work |
| Gadget_Mine | `Gadget_Mine.fbx` | 1,212 (632) | Root, Cap, Ring; socket `Blast`. Arm 24, Idle 60, Trigger 8 |
| Gadget_Chrono | `Gadget_Chrono.fbx` | 2,008 (1,328) | Root, Core, RingA, RingB; socket `Field`. Deploy 30, Idle 90 |
| Gadget_Barrel | `Gadget_Barrel.fbx` | 1,208 (656) | static; empties `Blast`, `Fuse` |
| Gadget_AirNet | `Gadget_AirNet.fbx` | 1,460 (1,156) | static; empties `Strand_L1..3`, `Strand_R1..3` |
| Gadget_GroundDrone | `Gadget_GroundDrone.fbx` | 928 (544) | Root, Body, Wheel_FL/FR/RL/RR; socket `Ram`. Drive 30, Idle 60 |

Maps: gate, base and base turret 1024 (+ `Low/` 512); drones and gadgets 512 (+ `Low/` 256). Reports
`*_report.json` hold pivots, sockets, UV density and detail tiling.

### Unity notes

- **Import** as round 7 (Scale 1, Bake Axis Conversion, Generic rig, keep non-deforming bones and empties). All
  models are at final size: do not apply `LevelVisualBuilder.BaseScale` or drone/path-object scales.
- **Gate (GateView):** replace `GatePylon`/`GateBeam` with the `Gate_Gn` prefab of the gate's level (L0 = G1, L1 = G2,
  L2 = G3). Mount `Tower_GateGun` on `GunMount` (identity). Keep the field shader; its sheet and floor strip go at
  `Field`. Damaged (below about 50% HP): show `Gate_Gn_Damage` and scale the bones in
  `damaged_state_hidden_hexes` (report) to 0, sparks at `Spark_n`. Repair: play `Repair` (pops those panels back),
  hide the overlay. Destroyed: play `Collapse` (or keep the code collapse), then swap to `Gate_Wreck`; rebuild plays
  `Build`.
- **Base (LevelVisualBuilder / BaseView / FieldBase):** instantiate the module objects instead of `BaseOutpost`; the
  `Spinner` goes on `Base_Dish`; give `Base_Crystal` its own material instance to pulse and ignite. The cutscene
  timelapse raises the modules in `timelapse_order`. Damaged: enable `Base_Damage`; destroyed: swap to `Base_Wreck`
  after the explosion. Lights at `Light_Core`, `Light_Command`; smoke and sparks at the sockets.
- **Base turret:** `BaseTurret` tower prefab uses `Tower_BaseTurret_T1..T3` exactly as Tower_Gun (Yaw, Pitch,
  Muzzle_n, stars).
- **Drones (DroneModelBuilder / DroneView):** the view keeps spinning `Rotor_n` in code (the clips spin slowly),
  bobbing and banking stay in code or use `Idle`/`Fly`; `Work` while repairing, mining or firing. Beam from `Beam`,
  shots from `Muzzle_n`, thruster trail at `Thruster`, cargo glow on `Drone_Harvest_Cargo` (own material instance).
- **Path objects (PathObjectView / PathObjectModels):** Mine plays `Arm` on placement, `Idle`, `Trigger` before the
  blast VFX at `Blast`; Chrono plays `Deploy` then `Idle`, the field shader at `Field` (the code's two VFX rings can go,
  the model has its own); Air Net strands between `Strand_Ln` and `Strand_Rn`; Ground Drone `Drive`.

## Round 11: P2 towers and gadgets (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 11). Same hard-surface pipeline as rounds 7 and 9; nothing in the shared
modules or the round-7/9 scripts changed. New scripts (they import `build_towers.py`, `build_gadgets.py` and
`build_drones.py` read-only):

- `build_towers2.py` (`--only Tesla,Railgun,Acid,Artillery,StormRifle,CryoBeam,Cryo,Support,Rocket`, `--nobake`,
  `--norender`, `--preview <dir>`; `--lineup` renders the whole roster, round 7 and round 11, from the maps on disk);
- `build_gadgets2.py` (`--only LavaBall,RAT,RATCarrier`, same flags).

| Asset | Tiers | Triangles LOD0 (LOD1) | Extra bones and clips (on top of the round-7 rig and IdleSweep / Fire_n) |
| --- | --- | --- | --- |
| Tower_Tesla | T1, T2, T3A chain, T3B overload | 3,564 / 4,220 / 4,840 / 4,120 (1,540 / 1,780 / 2,212 / 1,820) | sockets `Arc_1..n` on the prong tips (3 / 4 / 6 / 3); clips IdleSweep 240 (full turn), ChargeUp 24, Fire_1 10 (discharge, head scale) |
| Tower_Railgun | T1, T2, T3A twin, T3B lance | 2,768 / 3,364 / 3,936 / 4,320 (1,048 / 1,448 / 1,684 / 1,960) | ChargeUp 24 (rails draw back); T3A fires Barrel_1/2 in turn |
| Tower_Acid | T1, T2, T3A fan, T3B pressure lance | 3,298 / 3,996 / 4,920 / 4,072 (1,490 / 1,888 / 2,396 / 2,012) | `Slosh` (meniscus in the sight glass); Spray 12 (loop), Slosh 60 (loop); T3A has Barrel_1..3 |
| Tower_Artillery | T1, T2, T3 (Cannon + Flak) | 3,182 / 3,986 / 4,852 (1,378 / 1,878 / 2,352) | rests at 40 degrees elevation like the Flak |
| Tower_StormRifle | T1, T2, T3 (Gun + Tesla) | 3,728 / 4,804 / 4,984 (1,812 / 2,620 / 2,312) | T3 Spin 12 (loop) |
| Tower_CryoBeam | T1, T2, T3 (Laser + Cryo) | 3,906 / 4,230 / 4,630 (1,862 / 2,106 / 2,226) | T3 `Prong_1..3`; ChargeUp 24, Beam 12 (loop), no Fire_n (continuous) |
| Tower_Cryo | T1, T2, T3 | 3,560 / 3,790 / 4,470 (1,792 / 1,974 / 2,398) | – |
| Tower_Support | T1, T2, T3 | 3,382 / 3,650 / 3,918 (1,594 / 1,798 / 2,002) | core on `Barrel_1`; IdleSweep is the core's bob and spin, Fire_1 a pulse |
| Tower_Rocket | T1, T2, T3 | 2,408 / 2,736 / 3,706 (1,052 / 1,316 / 1,982) | rests at 30 degrees; sockets `Tube_1..n` on every tube mouth (4 / 6 / 9) |
| Gadget_LavaBall | – | 1,680 (1,560) | Root, `Ball` (rolls about X); sockets `Core`, `Trail`. Drop 24, Roll 30 (one turn, loop), Burst 12 |
| Gadget_RAT | – | 2,180 (1,256) | Root, Body, Turret, Gun, Wheel_FL/FR/RL/RR; sockets `Muzzle`, `Lug`. Drive 30, Fire 10, Idle 90, Land 16 |
| Gadget_RATCarrier | – | 2,384 (1,296) | Root, Body, Rotor_1..4, Clamp_L/R; socket `Cargo`. Carry 60, Release 20, Fly 60 |

Files as round 7 (towers: `Exports/Tower_<Name>/Tower_<Name>_<Tier>.fbx` + 1024 maps + `Low/` 512 + report) and
round 9 (gadgets: one FBX, 512 maps + `Low/` 256, report). Reports hold muzzles, sockets, the glow colour and the
pivot. Small pieces (insulator discs, prong tips, cages, conduits, caps) are LOD0 only.

### Unity notes

- **Import** exactly as rounds 7 and 9. Final size; do not apply the factory scales.
- **Tesla and Support do not aim:** drive only `Yaw` (or play IdleSweep); never pitch them (their heads sit on
  `Pitch`/`Barrel_1` above the column). Chain lightning starts at `Arc_n` (pick the nearest to the target); the
  Tesla's `Muzzle_1` is the tip of the lightning rod.
- **Railgun:** the plasma channel glows on its own (Emission); for the charge, drive `_EmissionColor` up during
  ChargeUp. **Acid:** play Spray while firing and Slosh on an additive layer when idle; the acid spray is VFX from
  `Muzzle_n` (fan: all three).
- **Artillery and Rocket** rest elevated (40 and 30 degrees): that is their modelled pose. Rocket missiles leave from
  `Tube_n` in turn (`Muzzle_1` is the pod centre).
- **Hybrids** have three levels (T1..T3, no A/B) and the same stars rule. Swap the prefab on merge.
- **Lava Ball (PathObjectView):** pivot on the ground; play Drop on placement, then rotate `Ball` about its X by
  `distance / 0.27` rad as it rolls (or loop Roll at 2.2 / (2 pi 0.27) = 1.3 turns per second), Burst at the end with
  the fire VFX at `Core`. **R.A.T.:** pivot on the ground; Land on touchdown, Drive while moving (wheel turns per
  metre = 1 / (2 pi 0.068)), Fire with the shot from `Muzzle`. **Carrier:** pivot at the drone centre; parent the
  tank so its `Lug` sits on `Cargo`, play Carry, then Release and unparent the tank at frame 6.
- **Open colour points:** the data colours of Artillery (orange) and Support (green) are warm; the models glow amber
  and signal yellow. The Lava Ball's molten core is orange on purpose (the weapon itself); the shell is ours.

## Round 10: P2 enemies and their bosses (Stinger, Bat, Bulwark, Oozer, AcidEmpress, CrystalWarlord, SkySovereign, SlimeMonarch, RollerKing, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 10). Builds: `build_stinger.py`, `build_bat.py`, `build_bulwark.py`,
`build_oozer.py`, then the bosses `build_acidempress.py`, `build_crystalwarlord.py`, `build_skysovereign.py`,
`build_slimemonarch.py`, `build_rollerking.py` (each boss imports its species' script, and its previews load the
species' `.blend`, so the species run first). Not yet in `build_all.py`. Helpers in the new `creature_kit2.py`
(the shared modules are only imported):

- Styles: `armour_plate` (dark plate, worn front lip, hot rear rim, forward chevron, optional crackle),
  `ring_segment` (ringed tail segment with glowing joints), `ribbed_bladder` (a gourd-like gland, glow between the
  ribs), `bladder` (veined sac), `scale_hide`, `bone_shield` (a natural shield), `slug_hide` (glands of mixed size),
  `slime_foot`, `crysium_blade`.
- `build_mesh_asset_ex` + `rebake_skin`: bake another palette onto the same game mesh (the element bats).
- `scale_rig` + `export_rig`: bosses are built in their species' units and scaled as a whole at the end (mesh,
  bones and location keys); `export_rig` exports exactly the given clips (other actions get a guard F-curve for the
  time of the export, otherwise the FBX exporter adds the LOD's actions as extra takes).
- `enemy_previews`: the standard sheet for a common enemy.

| Asset | Triangles | Bones | LOD1 (tris / bones) | Clips (frames @30 fps) |
| --- | --- | --- | --- | --- |
| Stinger | 3,961 | 25 | 1,697 / 14 | Walk 28 (loop), Idle 60 (loop), Hit 12, Death 40, Strike 24 |
| Bat | 3,896 | 16 | 1,698 / 10 | Fly 13 (loop), Glide 48 (loop), Hit 12, Death 36 |
| Bulwark | 3,950 | 19 | 1,700 / 16 | Walk 32 (loop), Idle 72 (loop), Hit 14, Death 44, Block 24 |
| Oozer | 3,801 | 9 | 1,699 / 7 | Crawl 40 (loop), Idle 72 (loop), Hit 12, Death 40 |
| AcidEmpress | 14,206 | 28 | 5,987 / 17 | Walk, Idle, Hit, Death, Strike (as the Stinger), Spray 48 |
| CrystalWarlord | 13,991 | 19 | 5,991 / 16 | Walk, Idle, Hit, Death, Block (as the Bulwark), Sweep 40 |
| SkySovereign | 12,603 | 19 | 5,996 / 13 | Fly, Glide, Hit, Death (as the Bat), Laser 72 |
| SlimeMonarch | 12,995 | 13 | 5,994 / 11 | Crawl, Idle, Hit, Death (as the Oozer), Flood 44, Spawn 48 |
| RollerKing | 13,831 | 20 | 5,993 / 14 | Walk, Idle, Hit, Death, Curl, Roll, Uncurl (as the Roller), Quake 30 |

Maps: one material, 1024 Albedo/Normal/Emission/Mask plus the 512 `Low/` set. Mask B/A families: Stinger,
AcidEmpress, Oozer and SlimeMonarch chitin/membrane (Oozer and SlimeMonarch membrane/chitin), Bat and SkySovereign
membrane/chitin, Bulwark and CrystalWarlord chitin/crystal, RollerKing chitin/membrane; tiling in each report's
`detail`. Boss tilings are computed in species units, so the micro-detail scales with the body (multiply the
tiling by the report's `model_scale_from_*` for the species' finer grain).

### Element bat skins

`build_bat.py` bakes the default element (Ember) into `Exports/Bat/`. `--element Moss,Dust` (any of Ember, Moss,
Dust, Tide, Frost, Mire, see `ELEMENTS`) bakes those skins onto the same game mesh and UVs into
`Exports/Bat/Skins/<Element>/Bat_<Element>_Albedo/_Normal/_Emission/_Mask.png` (+ `Low/`). One FBX and one rig for
all bats: in Unity each element is a material on the same prefab (Emberbat, Mossbat, Dustbat, Tidebat, Frostbat,
Mirebat). Baked now: Ember, Moss, Dust. Rebake a skin whenever the bat mesh changes (the skins must share its UVs).

### Unity notes for round 10

- **Import** as the earlier creatures (Scale 1, Bake Axis Conversion, Generic rig, root `Root`, Mikktspace). Take
  names: the species' FBX files carry `<Asset>_Rig|<Clip>` takes like the earlier ones; the boss FBX files were
  written by `export_rig` with the same naming.
- **Scale** (`unity_scale` in each report): Stinger 0.8, Bat 0.65, Bulwark 0.9, Oozer 0.7 (the data scales).
  Bosses at `Scale x BossScaleFactor`: AcidEmpress 0.9, CrystalWarlord 0.96, SkySovereign 0.84, SlimeMonarch 0.9,
  RollerKing 0.78.
- **Pivots:** ground units on the ground (Bulwark between the feet). Bat and SkySovereign: the body centre at flight
  height, like the Shalebat; turn off the `SurfaceWave` wing wave and drop `CrashSpin` while their Death clip rolls
  them.
- **Power timing** (start the VFX / damage on these frames):
  - Stinger `Strike`: the needle hits at frame **10** (0.33 s), ahead of the head at head height. Play it on the
    Sting attack against towers.
  - Bulwark `Block`: shield up at frame **7**, then held to the end (keep the last frame while blocking).
  - AcidEmpress `Spray`: acid leaves the nozzles at frame **26** (0.87 s), about 0.15 x scale above the `Gland`
    bone, upwards (acid rain). PlateRegrowth has no clip (its VFX on the plates).
  - CrystalWarlord `Sweep`: the blade passes the front at frame **20** (0.67 s); start the SwordSweep ring there.
    CrystalRegrowth: VFX on the crown and shoulder crystals.
  - SkySovereign `Laser`: beam on from frame **20** to **60** (0.67 to 2.0 s), from the `Emitter` bone along its
    +Y (the tail swings under the body and aims forward and down; the clip sweeps it side to side).
  - SlimeMonarch `Flood`: the front slams down at frame **24** (0.8 s), start the slime ring there. `Spawn`: young
    oozers leave the `Sac_L` / `Sac_R` bones at frame **30** (1.0 s).
  - RollerKing `Quake`: played rolled up (after `Curl` or during `Roll`), impact at frame **16** (0.53 s); it ends
    rolled, so `Roll` or `Uncurl` follows.
- **Scale curves:** Oozer stalks, SlimeMonarch stalks and sacs, AcidEmpress `Gland` animate bone scale (keep
  "Remove constant scale curves" off).
- **Gait playback:** `velocity / (distance_per_loop x scale)`: Stinger 0.236, Bulwark 0.5, Oozer 0.3 (a slug glides,
  the pedal wave is the cue), AcidEmpress 0.52, CrystalWarlord 1.05, SlimeMonarch 0.6, RollerKing walk 1.008 and roll
  4.09 (model units per loop, see the reports).
- **Bulwark and CrystalWarlord are not mirror-symmetric** (shield left, sword right): no mirrored UVs, so the
  whole body sits in the 1024 map at a lower texel density than the mirrored species.
- **Oozer trail:** VFX from under `Body_1` (the rear) on the ground.
- **SkySovereign data:** in the data the SkySovereign is the ray boss and the StormMatriarch the bat boss; the models
  are the other way round (round 6 StormMatriarch is a Skyray boss, this SkySovereign is a bat boss). Swap the data
  shapes, or swap which model each boss uses (curator decision).

## Round 14: Gravemaw, burrow hole, spawn origins, resource node, warm Tide/Frost bat skins (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 14). New scripts (shared modules untouched, only imported):
`deep_kit.py` (helpers), `build_gravemaw.py`, `build_env_origins.py`, `build_bat_skins.py`. Not yet in
`build_all.py`; proposed lines, after `build_broodmother.py` (the Gravemaw previews load the BroodMother and Skitter
`.blend` files and the hole FBX) and with `build_env_origins.py` before `build_gravemaw.py`:

```
"build_env_origins.py", "build_gravemaw.py",   # round 14, after the level bosses
"build_bat_skins.py",                          # round 14, after build_bat.py (bakes Tide/Frost onto the shipped Bat mesh)
```

`deep_kit.py`: `PathFZ` / `ride` / `spine_specs` (a spine riding a path in the forward-up plane: dive and breach),
`crystal_plate`, `petal`, `belly_rings`, `mask_glow` (white emission masks), `unwrap_dk` + `build_mesh_asset_dk`
(uvlayout's unwrap plus a mid-plane cut of closed one-sided islands; without it the Gravemaw body packed at 0.0 to
0.3 coverage), `join_skinned` / `build_lod2` (two-material creature), preview helpers (`ground_with_holes`,
`liquid_sheet`, `tint_emission`). `build_gravemaw.py --blockout` renders raw parts on the rig in a minute (no bakes).

### Gravemaw (`Exports/Gravemaw/`)

| Item | Value |
| --- | --- |
| Mesh | one mesh `Gravemaw`, two materials: `Gravemaw_Mat` (body, ~17,200 tris) and `Gravemaw_Crystal_Mat` (~6,900 tris), total ~24,100 |
| Maps | `Gravemaw_*.png` and `Gravemaw_Crystal_*.png`, 1024 + `Low/` 512 each (Albedo, Normal, Emission, Mask as usual) |
| Rig | `Gravemaw_Rig`, 38 bones (Root non-deforming, Seg_1..6, Head, Tail, Jaw_T/UR/UL/LR/LL, Arm_L/R_Upper/Fore/Spade, Leg_L/R1..3_Upper/Lower, Debris_L/R1..3) |
| LOD1 | `Gravemaw_LOD1.fbx`, ~10,000 tris, 30 bones (lower legs and spades folded), same clips and maps |
| Size | about 5.9 long, 2.8 wide (claws), 2.5 high (crest), Unity scale 1 (BroodMother in game is ~2.5 long) |
| Clips @30 fps | Crawl 64 (loop, 0.692 per loop), Idle 90 (loop), Hit 16, Death 60, Roar 66, Dive 120, Emerge 112 |

Unity notes:

- **Import:** Scale 1, Bake Axis Conversion, Generic rig, root `Root`, two material slots (keep the order). Debris
  bones animate **scale** (hidden at 0.001 outside Dive and Emerge): keep "Remove constant scale curves" off.
- **Dive** (play while the boss stands still): wind-up to frame 24, the drill hits the ground at frame **31**
  (start the dust/impact VFX and the rumble; debris flies from there), all of it is under the plain by frame
  **94**; hide the renderer at the end (frame 120). The hole is **2.95 m ahead** of the pivot along its forward
  axis (x Unity scale): put `Gravemaw_Hole` there when the dive starts. The body travels ~8 m forward along a path
  under the ground, so the terrain must occlude it (no see-through liquids at the dive spot).
- **Emerge** (at the new road position, boss logically still): the head breaks the surface at frame **20** (start
  the burst VFX, sound and debris there), the body arches out and comes to the rest pose at frame **98**; switch to
  Crawl after 112. The breach hole is **5.66 m behind** the pivot (`emerge.hole_forward` -5.655): place
  `Gravemaw_Hole` there, so the boss reappears with its pivot 5.66 m further along the road than the hole.
  The direction cue for "where it surfaces" (STORY_ROADMAP) is that hole position.
- Roar has its jaws wide from frame ~12 to 50 (the star maw); good for the boss intro and the alarm.
- Emission: body pink-magenta (petal seams, gullet, segment rims, chevrons), crystal pink to ice; about 2.0 to 2.5.

### Environment (`Exports/Env/`)

| Asset | Tris | Maps | Placement |
| --- | --- | --- | --- |
| Gravemaw_Hole | 2,400 | 1024 | ground pivot; 3.5 wide; pit floor at -1.0; pink emission baked |
| Origin_Water / _Lava / _Quicksand / _Ice / _Bog | 1,800 / 1,800 / 1,700 / 1,800 / 1,800 | 512 | pivot on the liquid surface at the cell centre (`TerrainLiquidBuilder.SurfaceLevel(kind)`), any yaw; Emission is a **white mask**, tint with `NestColor(element)` |
| ResourceNode (`ResourceNode_Full`, `_Half`, `_Empty` in one FBX) | ~1,800 each (5,398 total) | 1024 shared | ground pivot at the mine cell centre; Half at 50 % stock, Empty on `MineDepleted` (replaces the scale-down in `MiningNodeView`); gold emission baked |

### Bat skins

`build_bat_skins.py` rebuilds the Ember game mesh in a scratch folder, checks it against `Exports/Bat/Bat.fbx`
(vertices, triangles, UVs; max UV difference 0.0) and bakes `Skins/Tide/` and `Skins/Frost/` with warm glows
(report: `Skins/Skins_warm_report.json`). `Exports/Bat/Bat.fbx` and its maps are not touched.


## Round 13: story characters and cutscene models (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 13). Builds (not yet in `build_all.py`):

- `build_voss.py`: Dr. Elena Voss, creature pipeline (fused parts, styles, bake, skinned bust).
- `build_story_props.py` (`--only Ora,Emblem,Pioneer,Wreck,Console`): ORA, the NTI emblem, Pioneer-1, its wreck, the
  radar console. Hard-surface kit.
- `build_cutscene.py` (`--only Capsule,Drill,Mothership,Beacon`): the opening cutscene's machines. Hard-surface kit.
- `story_kit.py`: shared helpers (looks, partial tori, icospheres, broken lathes, mounds, Crysium shards, fronds,
  tendrils, the NTI emblem as pieces, glitch-band and planar screen UVs, portrait camera, strips, space backdrop).

All take `--preview <dir>`, `--norender`; the kit builds also `--nobake`. Cutscene-only and portrait-only assets have
no LOD1 (the report says so); the Pioneer-1 wreck is static level decor.

| Asset | Files (`Exports/<Asset>/`) | Triangles | Maps | Rig and clips |
| --- | --- | --- | --- | --- |
| Voss | `Voss.fbx` | 6,000 (budget 6,000), no LOD1 | 1024 (+ `Low/` 512), Mask B weave, A ceramic | `Voss_Rig`, 10 bones: Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R. Idle 120 (loop, blink at 70), Talk 90 (loop, blink at 44), Blink 8 |
| Ora | `Ora.fbx` | 1,622 | 1024 (+ 512) | `Ora_Rig`, 9 bones: Root, Float, Core, Inner, Gaze, Halo, Ring_1..3. Idle 120, Talk 60 (loops), Glitch 24. **UV1 "Bands"** for the glitch shader |
| NTI_Emblem | `NTI_Emblem.fbx` | 412 | 512 (+ 256) | static; empty `Centre` |
| Pioneer1 | `Pioneer1.fbx` | 3,300 | 1024 (+ 512) | static; empties `StatusLight`, `Lens`, `Centre` |
| Pioneer1_Wreck | `Pioneer1_Wreck.fbx` | 4,980 | 1024 (+ 512) | static decor; empties `StatusLight`, `Smoke`, `Log`, `Centre` |
| RadarConsole | `RadarConsole.fbx` (+ meshes `_Radar`, `_Screen_L`, `_Screen_R`) | 2,846 + 156 + 12 + 12 | 1024 (+ 512) | static; empties `Radar`, `Operator`; screen meshes have planar 0..1 UVs |
| Cutscene_Capsule | `Cutscene_Capsule.fbx` (+ mesh `Cutscene_Capsule_Shield`) | 4,262 + 638 | 1024 (+ 512) | 5 bones: Root, Ramp, Leg_1..3. Land 30, Open 48. Empties `Exit`, `Jet_1..4`, `Beacon`, `ShieldCentre` |
| Cutscene_Drill | `Cutscene_Drill.fbx` | 3,630 | 1024 (+ 512) | 5 bones: Root, Block, String, BitTip (socket), Cable. Spin 30 (loop), Sink 150. Empties `Beacon`, `Exhaust`, `Dust` |
| Cutscene_Mothership | replaced in round 19 (`build_mothership2.py`, see below) | ~~9,302~~ 19,442 + LOD1 8,594 | 2048 (+ 1024) | 2 bones: Root, Ring. Spin 360 (loop). Empties `Bay`, `Nozzle_1..3`, round 19 adds `Light_1..10` |
| Cutscene_Beacon | `Cutscene_Beacon.fbx` | 2,162 | 512 (+ 256) | 8 bones: Root, Leg_1..3, Mast, Mast2, Ring, Lamp (socket). Deploy 54, Idle 60 (loop). Empty `Ground` |


### Unity notes

- **Import** as rounds 7 and 9 (Scale 1, Bake Axis Conversion, Generic rig, keep non-deforming bones and empties,
  Mikktspace tangents; Voss as Mercer). Albedo/Emission sRGB, Normal as normal map, Mask linear. One `Game/UnitLit`
  material per asset with its four maps; emission x 2.2.
- **Portraits (`PortraitModels`, `ModelIconRenderer`):** `Scientist` instantiates `Voss.fbx` sampled at Idle frame 10
  (or plays Idle; Talk while her line types; layer Blink at random 3-6 s) instead of the code bust. `Hologram`
  instantiates `Ora.fbx` (Idle; Talk while ORA speaks; Glitch for the corrupted Directive-7 lines) with an additive
  hologram shader that reads **UV1** (U = angle round the vertical axis, V = height 0 at the emitter, 1 at the crown)
  for scanlines, slice offsets and tear bands; the emitter disc may stay opaque. `Emblem` instantiates
  `NTI_Emblem.fbx` (badge centre at the origin, facing +Z). `Probe` instantiates `Pioneer1.fbx` (origin on the
  ground, the body centre at about y 0.9). Framing: the renders use Mercer's bust camera (70 mm, 28 degrees off
  the front, 5 degrees up, 1.45 away; Voss aimed at y 1.36, ORA at its core, y 0.34, from 1.25). The speaker's rim
  light can stay as today.
- **Cutscene cast (`CutsceneCast` prefab overrides):** `Capsule` = `Cutscene_Capsule.fbx`; its heat shield is the
  separate mesh `Cutscene_Capsule_Shield` (give it its own material instance and fade `_EmissionColor` after re-entry,
  as `CapsuleModelFactory`'s shield did). Play `Land` on touchdown, then `Open`; Mercer steps out at the `Exit`
  empty; retro-jet flames at `Jet_1..4` (empties, +Z = exhaust direction). `Drill` = `Cutscene_Drill.fbx`: the model
  has its own drill string, so `FieldDrill` should hide its procedural `Bit` when the override has a `String` bone
  and play `Sink` (150 frames, the string drives 3 units down while it turns; sample it by the shot's progress) and
  `Spin` (loop) afterwards; dust at `Dust`, the bit tip at the `BitTip` bone. `Mothership` = `Cutscene_Mothership.fbx`
  (about 15 long, nose +Z): play `Spin` (the habitat ring, one turn per 12 s) or turn the `Ring` bone in code; capsules
  leave from `Bay`, exhaust glow at `Nozzle_1..3`. `Beacon` = `Cutscene_Beacon.fbx`: `BeaconModel.Pose(deploy)` should
  sample `Deploy` at `deploy` (0..1 over 54 frames) instead of scaling the hidden procedural parts, then loop `Idle`;
  the point light goes on the `Lamp` bone.
- **Radar console:** a command-post prop for shot 7 (today a UI overlay). `RadarConsole_Radar` is a disc with a planar
  0..1 UV (centre 0.5, 0.5) for a sweep shader (`RadarOverlay`'s look on a material); `RadarConsole_Screen_L/_R` are
  quads with 0..1 UVs for screen textures. The baked maps give them a dim lit look when no shader is set.
- **Pioneer-1 wreck (Mirewood, level 8, Rotbloom arena):** static decor on the ground with its own mire mound (about
  4.8 across); a status light still glows at `StatusLight` (a small blinking point light), smoke at `Smoke`, the
  logbook pickup at `Log`.

## Round 15: P3 creatures and the corrupted skin set (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 15). New scripts (shared modules and earlier build scripts untouched, only
imported): `creature_kit3.py` (helpers), `build_shardback.py`, `build_mireleech.py`, `build_knitback.py`,
`build_slagstrider.py`, `build_siftjaw.py`, `build_surgefin.py`, `build_corrupted_skins.py`. Not yet in `build_all.py`;
proposed lines, after the round-10 species and after `build_bat_skins.py` (the corrupted wrapper reads every species'
`.blend` and maps, so it runs last):

```
"build_shardback.py", "build_mireleech.py",                       # round 15, P3 species
"build_knitback.py", "build_slagstrider.py", "build_siftjaw.py", "build_surgefin.py",  # round 15, new species (names proposed)
"build_corrupted_skins.py",                                        # round 15, last: reads all species' .blend and maps
```

`creature_kit3.py`: styles `ice_prism` (cold prism, warm root glow), `socket_floor`, `bark` (two-value camouflage),
`sap_pod`, `wet_skin`, `fin_sail`, `molten_sac`; `ClipPoser` (scale keys per clip; by default every clip keys every
scale bone, 1 where it does not animate it); `build_lod` (LOD1 with rigid parts and scale keys); leg helpers
(`limbs`, `place_legs`, `gait_feet`, `splay_feet`); `run_creature` (the whole common-enemy pipeline, with
`creature_kit2.enemy_previews`).

| Asset | Triangles | Bones | LOD1 (tris / bones) | Clips (frames @30 fps) |
| --- | --- | --- | --- | --- |
| Shardback | 3,940 | 23 | 1,698 / 15 | Walk 32 (loop), Idle 72 (loop), Hit 12, Death 40, Shatter 40 |
| Mireleech | 3,906 | 11 | 1,700 / 11 | Crawl 30 (loop), Idle 72 (loop), Hit 12, Death 40, Latch 36 |
| Knitback | 3,952 | 18 | 1,698 / 12 | Walk 40 (loop), Idle 72 (loop), Hit 14, Death 44, Regrow 48 |
| Slagstrider | 3,957 | 13 | 1,699 / 8 | Walk 36 (loop), Idle 72 (loop), Hit 12, Death 42 |
| Siftjaw | 3,950 | 19 | 1,698 / 12 | Walk 24 (loop), Idle 60 (loop), Hit 12, Death 40, Burrow 36, Emerge 36 |
| Surgefin | 3,947 | 16 | 1,699 / 10 | Walk 28 (loop), Swim 24 (loop), Idle 72 (loop), Hit 12, Death 40, Emerge 48 |

All: one material, 1024 Albedo/Normal/Emission/Mask plus the 512 `Low/` set, mirrored UVs, `<Asset>_report.json` with
the detail tiling, gait distances and power frames.

### Unity notes for round 15

- **Import** as the earlier creatures: Scale 1, Bake Axis Conversion, Generic rig, root `Root`, Mikktspace. Takes
  `<Asset>_Rig|<Clip>`. Keep "Remove constant scale curves" **off** (Shardback shards, Knitback pods).
- **Shardback shedding:** the prism clusters are bones `Shard_1..Shard_6` in shedding order (rear-mid, rear-left,
  rear-right, crest, front-left, front-right), each cluster a rigid island. Map `EnemyView`'s plate index i to
  `Shard_(i+1)`: to shed, set that bone's `localScale` to 0 in **LateUpdate** (every clip keys the shard scale at 1,
  so the Animator would restore it) and spawn the ice debris at the bone. The amber socket under it shows by
  itself. `Shatter` bursts all six (bursts at frames 3, 8, 13, 18, 23, 28; all gone by frame 38): play it for a
  full shatter (Thermal Shock) or skip it when shedding one by one. Frost spit from the throat (about 0.46 forward,
  0.17 up, model units, on `Head`).
- **Mireleech:** `Latch` clamps at frame 13 (bite damage there), then sucks in pulses to frame 36. Mud trail VFX
  from under `Seg_1` (rear sucker). Crawl is a hump that runs tail to head: playback speed
  `velocity / (0.32 x scale)`.
- **Knitback:** `Regrow` swells the pods (bones `Pod_1..3`, scale keys) for the regeneration tick; pair it with an
  emission pulse. Camouflage: fade the albedo towards the ground tint and the emission down while hidden; the skin is
  two close values on purpose.
- **Slagstrider:** burning-trail VFX from bone `Drip` (under the belly, 0.32 above the ground; the LOD1 folds it into `Body`). Footfalls
  (heat puffs) at Walk frames 0 (L1, R2) and 18 (R1, L2). Emission strength 3.0 (vents and seams); heat shimmer from
  the vents in Unity.
- **Siftjaw:** `Burrow` is fully under the ground plane from frame 24 (hide the renderer then; it moves 0.12 forward
  and 0.5 down); `Emerge` starts 0.5 under and 0.1 behind the pivot, breaks the surface at frame 8 (sand burst VFX).
  Both are root-motion-free in Unity terms: the offsets are on the `Body` bone, the pivot stays.
- **Surgefin:** `Swim` for wet road (the body rides 0.03 lower: put the pivot on the water surface), `Walk` on dry
  road. `Emerge` starts 0.45 under the surface, the head breaks it at frame 10 (splash VFX), lands at ~frame 30.
  The lure bulb and gill slits are in the emission map (coral).
- **Scale** (`unity_scale` in each report): Shardback 0.8 and Mireleech 0.7 (the data scales); the new species are
  proposals: Knitback 0.85, Slagstrider 0.8, Siftjaw 0.75, Surgefin 0.75.

### Corrupted skins (`Exports/<Species>/Skins/Corrupted/`)

`build_corrupted_skins.py` writes `<Species>_Corrupted_Albedo/_Normal/_Emission/_Mask.png` (+ `Low/`) for every
species with a shipped `.blend` and maps (`--bosses` adds the one-material level bosses, `--only` picks). Same mesh and
UVs as the species: in Unity a corrupted enemy is the species prefab with a second material. Report:
`Exports/Corrupted_skins_report.json`. The `.blend` files are opened read-only and never saved; the species' own maps
are not touched. Rerun it whenever a species' mesh or maps change. Suggested emission colour: unchanged (the map is
already violet), strength as the species.

Mask B/A families (tiling in each report): Shardback chitin/crystal, Mireleech membrane/chitin, Knitback chitin/membrane,
Slagstrider rock/membrane, Siftjaw chitin/membrane, Surgefin membrane/chitin. Gait playback
`velocity / (distance_per_loop x scale)`: Shardback 0.2, Knitback 0.194, Slagstrider 0.387, Siftjaw 0.2, Surgefin walk
0.2 and swim 0.5, Mireleech 0.32 (model units per loop).

## Round 16: polish pass on Voss and Mercer (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 16). Both are rebuilt with `suit.build_layered_asset` (parts fused per layer:
own remesh, own triangle share, per-layer bake) instead of one fused blob. File names, bone names, sockets and clip
names are unchanged, so the import plans hold.

| Asset | Files | Triangles | Rig and clips |
| --- | --- | --- | --- |
| Mercer | `Exports/Mercer/Mercer.fbx` (+ maps, `Low/`, `Weapons/` unchanged) | see report (budget 9,800), LOD1 4,000 | 31 bones as before; clips as before. Shoot, Reload and Down re-keyed (bigger kick, wider reload arc, Down ends near the origin); two-handed aim fixed |
| Voss | `Exports/Voss/Voss.fbx` (+ maps, `Low/`) | 10,000 (budget raised from 6,000: portrait and cutscene only), no LOD1 | 10 bones as before; Idle 120, Talk 90 (jaw up to 7 degrees, was 4), Blink 8 |

New options: both scripts take `--stage <dir>` (exports and .blend go there, for iterations while Unity imports);
`build_voss.py --fast` renders the styled source without baking (shape checks in about two minutes).

### Unity notes

- Voss now has seven separate shells in one mesh (head, eyes, lids, hair, top, coat, gear); one material as before.
  The UVs are a smart projection (more islands), so keep the 1024 maps; the Low set is 512.
- Mercer's plates are separate shells on the suit: plates are rigid to one bone each (as before), the hollow inside
  of a pauldron can show from a low camera, which the game camera never has.
- The Down end pose moved: hips about 0.39 and head 0.84 to the figure's right of the root (was 0.89 and 1.34). If a
  health bar or selection ring follows the hips bone nothing changes; if it uses a fixed offset, use 0.4.


## Round 16: continent bosses 2-6 (Cinder Matriarch + fire larva, Frost Warden, Rotbloom, The Shattered, The Sealed One, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 16). New scripts (shared modules and earlier build scripts untouched, only
imported): `titan_kit.py` (helpers), `build_cindermatriarch.py` (also builds `FireLarva`), `build_frostwarden.py`,
`build_rotbloom.py`, `build_shattered.py`, `build_sealedone.py`. Not yet in `build_all.py`; proposed lines, after
`build_gravemaw.py` (the previews load the Gravemaw and Skitter `.blend` files for scale):

```
# Round 16: continent bosses 2-6 (titan_kit.py); previews load Gravemaw and Skitter.
"build_cindermatriarch.py", "build_frostwarden.py", "build_rotbloom.py", "build_shattered.py", "build_sealedone.py",
```

`titan_kit.py`: the seal glyph (`seal_mask`, `seal_rune`, `rune_band`: engraved, lit glyph passes), styles
(`kiln_crust`: molten organ under uneven chilled floes, never a honeycomb; `flesh_glow`; `leaf_petal`; `scale_band`;
`solid_glow`; `shell_plate`), `ellipsoid_patch` (a plate conforming to an ellipsoid, lifted off it by a gap so the
remesh keeps it a separate island: shells that vanish by bone scale), `island_rigid` (welds such islands fully onto
their bone after skinning), `build_titan` (bake one or two materials, skin, per-clip scale keys, LOD1, join, export,
report, `.blend`), `previews` (the set below) and `blockout` (`--blockout`: raw parts on the rig in ~15 s).

Previews (all with `--preview <dir>`): `sheet_<prefix>.png` (3/4, side, front, top, back, power poses, death, LOD1,
macros, the game view at play size 1280x720 next to Gravemaw and six Skitters, and zoomed x2) and
`strips_<prefix>.png` (clip strips). Prefixes: `cinder`, `firelarva`, `warden`, `rotbloom`, `shattered`, `sealed`.

| Asset | Triangles (body + second) | Bones | LOD1 (tris / bones) | Size (m, w x l x h) | Clips (frames @30 fps) |
| --- | --- | --- | --- | --- | --- |
| CinderMatriarch | 23,051 (17,995 + 5,056 Crystal) | 26 | 8,997 / 21 | 4.7 x 5.8 x 3.0 | Walk 56 (loop), Idle 90 (loop), Hit 16, Death 72, Spawn 64, Roar 70 |
| FireLarva | 1,458 (one material) | 21 | 700 / 13 | 0.35 x 1.0 x 0.23 (Unity scale 0.6) | Crawl 20 (loop), Idle 48 (loop), Hit 10, Death 30 |
| FrostWarden | 23,880 (17,507 + 6,373 Ice) | 23 | 9,496 / 22 | 3.1 x 4.7 x 4.9 | Walk 64 (loop), WalkEnraged 40 (loop), Idle 120 (loop), Hit 16, Death 84, FreezePulse 64, Enrage 48 |
| Rotbloom | 23,968 (16,989 + 6,979 Bloom) | 27 | 9,498 / 25 | 3.8 x 5.0 x 2.4 | Crawl 64 (loop), Idle 96 (loop), Hit 16, Death 84, Regrow 60, Bloom 72 |
| Shattered | 24,070 (17,493 + 6,577 Crystal) | 27 | 9,498 / 26 | 3.4 x 4.5 x 3.6 (crystals at rest scale) | Walk 56 (loop), Idle 72 (loop), Hit 14, PhaseBreak1 40, PhaseBreak2 44, Death 80 |
| SealedOne | 34,478 (25,486 + 8,992 Seal) | 29 | 13,998 / 26 | 6.9 x 6.9 x 6.7 (seal ring 5.5 across) | Idle 120 (loop), Rise 150, Summon 90, Hit 18, Death 150 |

Gait playback `velocity / (distance_per_loop x scale)`: CinderMatriarch Walk 1.288, FrostWarden Walk 1.029 and
WalkEnraged 1.338, Rotbloom Crawl 0.714, Shattered Walk 1.452 (model units per loop; reports).

All bosses: two materials (`<Asset>_Mat` + `<Asset>_<Second>_Mat`, keep the slot order), each 1024 Albedo, Normal,
Emission, Mask plus the 512 `Low/` set, mirrored UVs, `<Asset>_report.json` (tris per material, detail tiling, power
frames, phase tables). Unity scale 1 (built at game size; Gravemaw is 5.9 long).

### Unity notes for round 16

- **Import** as Gravemaw: Scale 1, Bake Axis Conversion, Generic rig, root `Root`, two material slots in order.
  Keep "Remove constant scale curves" **off** (the Vent, Halo, Pod, Bloom, Shell, Crys and Ring bones animate scale).
  Emission strengths: the `emission` value per material in each report is what the previews used.
- **Cinder Matriarch.** `Spawn`: one larva per chimney at frames **30, 33, 36** (`Vent_1`, `Vent_2`, `Vent_3`; spawn
  at the bone's tail, the chimney mouth, then let the larva drop and crawl); the chimneys swell by bone scale. Burning
  trail VFX from bone `Drip` (sac tip). `Roar` rears on the hind stilts (mandibles wide frames ~20-50): boss intro and
  alarm. Footfalls (heat puffs) at Walk frames 0 and 28. Immune to the Lava Ball is data only; the heat-proof foot
  cones are the visual cue. Minion `FireLarva` (Unity scale 0.6): one material, Crawl/Idle/Hit/Death, LOD1 700.
- **Frost Warden.** `FreezePulse`: glaives overhead by frame 20, driven into the ground at frame **30** (start the
  frost ring and the tower freeze there, centred ~1.5 ahead of the pivot between the glaive tips); the `Halo` flares
  to 1.4 at frame 32. Low HP: play `Enrage` once (48 frames, it ends in the hunch) and then loop `WalkEnraged` (the
  same hunch, 1.3x stride; playback `velocity / (1.338 x scale)`) instead of `Walk` (`velocity / (1.029 x scale)`). The seal glyphs are in the Emission map (amber-gold): pulse the emission on FreezePulse.
- **Rotbloom.** Slime-trail VFX from bone `Trail` (the rear slime lobe, on the ground). `Regrow`: pods peak at frame
  **28** (pair it with the heal tick and an emission pulse). `Bloom`: petals open frames 6-26, spore burst from bone
  `Bloom` at frame **34**. Death: the bloom wilts shut.
- **The Shattered: phases by bone scale.** Walk, Idle and Hit are keyed in phase 1. For phases 2 and 3 a small
  component writes the phase table (report `phases`) over the Animator in **LateUpdate** (bones `Shell_1..8`,
  `Crys_1..6`, uniform `localScale`), except while `PhaseBreak1`, `PhaseBreak2` or `Death` play (they key the
  transition themselves):
  - phase 1: Shell_1..8 = 1, Crys_1..6 = 0.25
  - phase 2: Shell_1..4 = 0 (the hump), Shell_5..8 = 1; Crys_1, 2, 3, 6 = 1, Crys_4, 5 = 0.55
  - phase 3: all Shell = 0, all Crys = 1.35
  `PhaseBreak1` blows the hump plates off from frame **12** (one every 2 frames: plate debris VFX at the plates),
  `PhaseBreak2` the rest from frame **14**; the crystals grow with an overshoot. Death: the crystals swell, the final
  explosion at frame **46** (crystals gone by 54: shard VFX and the big flash there). Raise the emission per phase
  (report `emission_per_phase`: 2.6, 3.4, 4.5) so the leaking light grows. The plates are rigid to their bones; at
  scale 0 they collapse to a point inside the core.
- **The Sealed One.** The pivot is the centre of the seal on the ground. `Rise` starts with the body **5.2 m under
  the ground** and the seven stones closed; the seal breaks at frame **22** (stones burst outwards: shockwave, dust),
  the hands slam down at frame **92**, the crown unfolds and it roars at ~124. The terrain must occlude everything
  under z = 0 (no see-through liquid at the boss spot); the lowest ~0.7 m of the torso is always under the ground.
  `Summon`: palms up and plates open, the corrupted enemies appear at frame **44** (from the violet column, bone
  `Head`, or round the seal stones). `Death`: the plates close, the rings shrink away, it sinks, and the seal stones
  close over the fissure by frame **128** (the "seal restored" beat; the body is fully under the ground from ~124).
  The seal stones stay on `Root` (they do not move with the body) and are part of the boss mesh.

## Round 17: environment sets for continents 2-6 (2026-11-04)

Design notes: `DESIGN_NOTES.md`, round 17. Scripts (new; the round 8 modules are imported, not changed):
`env_kit2.py` (continent folders and prefixes, tiling-set writer, the continuous kerb, banks, swept cliffs, the
diorama) and one build per continent: `build_env_ashenreach.py`, `build_env_northerncrown.py`,
`build_env_mirewood.py`, `build_env_southerncrown.py`, `build_env_core.py`.

Run one continent (about 25 to 40 min, most of it baking): `blender --background --factory-startup --python
Tools/Blender/build_env_<continent>.py -- --preview <dir> [--part materials|kit|decor|diorama|all] [--only A,B]`.
`build_env_core.py` also has `--part seal` (the three seal states and the vein pieces; `all` includes them).

### Folders and names

| Continent | Folder (`Exports/Env/...`) | Prefix | Tiling sets (`Tiling/<Prefix>_<Set>/`) |
| --- | --- | --- | --- |
| Ashen Reach | `AshenReach/` | `Ash_` | Road (2), Ground (3), GroundAlt (2), Sand (3), Cliff (3) |
| Northern Crown | `NorthernCrown/` | `North_` | Road (2), Ground (3), GroundAlt (2), Snow (3), Cliff (3) |
| Mirewood | `Mirewood/` | `Mire_` | Road (2), Ground (3), GroundAlt (2), Mud (3), Cliff (3) |
| Southern Crown | `SouthernCrown/` | `South_` | Road (2), Ground (3), GroundAlt (2), CrackedIce (3), Cliff (3) |
| The Core | `TheCore/` | `Core_` | Road (2), Ground (3), GroundAlt (2), Plateau (3), Cliff (3) |

The number is the world size one tile covers (UV = worldXZ / size). Every set has Albedo, Normal and Mask (R AO,
G smoothness, B height, A dust/sparkle) at 1024 plus `Low/` at 512, and Emission where something glows; the report
has `value_mean` (mean sRGB luminance of the albedo), which keeps the bands honest: ground 0.12-0.17, GroundAlt and
the element set 0.16-0.42, road 0.45-0.49.

Every continent has the same kit names (so one placement code serves all five):
`RoadEdge_Straight/_OuterCorner/_InnerCorner`, `Bank_Straight/_OuterCorner/_InnerCorner`,
`Cliff_Straight/_OuterCorner/_InnerCorner`, plus its decor (table below). Kit pieces are 512 maps, one material.

### Placement frames (same rules as round 8, made exact)

- **RoadEdge_Straight / Bank_Straight**: centred on the middle of a cell side, the road or liquid on local -Y (Unity
  +Z, `LookRotation(towards the road cell)`). One continuous kerb, ends flush at X = +-0.5: neighbours chain
  without gaps.
- **RoadEdge_OuterCorner / Bank_OuterCorner**: on the corner point, the road or liquid cell in the (-X, -Y)
  quadrant (1 road cell of the 4). A short kerb arc closing the two straights; it stands 5 mm higher so the
  overlap does not z-fight.
- **RoadEdge_InnerCorner / Bank_InnerCorner**: on the corner point, the ground cell in (+X, +Y) (3 road cells of 4):
  a corner stone over the joint where the two straights cross.
- **Cliff_Straight**: 2 cells along X, pivot on the field edge, field on -Y, wall behind on +Y.
  **Cliff_OuterCorner**: wall in the (+X, +Y) quadrant, arms 1 cell along +X and +Y (straights continue from 1.0).
  **Cliff_InnerCorner**: field in the (-X, -Y) quadrant, arms 1 cell along -X and -Y.
  The Core's `Cliff_*` are plateau edges: flush with the field, the faces drop 2.4 into the void on +Y
  (as round 8 `Chasm_Edge`). Put a dark violet fog or void plane about 3 below.
- Build pads and the gate plinth stay round 8's `BuildPadTile` and `GatePlinth` on every continent (ours).
- The diorama in `env_kit2.Diorama` applies exactly these rules (kerbs per cell side, corners per corner point,
  banks round the pool, the border wall with an inner and an outer corner).

### Set pieces (The Core)

| Asset | Tris | Size | Notes |
| --- | --- | --- | --- |
| Core_Seal_Intact / _Cracked / _Broken | 8,000 each | 7.2 across | pivot at the centre on the ground; 1024 maps per state; swap the mesh as the story advances (levels 1-9 intact, 10 cracked, after 10 broken, restored with the ring of crystals in 15-19). Gold glyph inlay and radial veins in the Emission map; violet cracks in Cracked/Broken |
| Core_SealVein_Straight | 600 | 2 x 0.5 | flat, any yaw, chain along X |
| Core_SealVein_Branch | 800 | 1.6 x 1.1 | Y junction, branch at 40 degrees towards +Y |

### Unity consumption (proposal, nothing imported)

- **Theme per continent**: add one `EnvironmentSet` reference per continent (or a `Prefix` string on the
  `VisualTheme`) so `TerrainSurfaceBuilder`, the kerb/bank/cliff placer and `PropFactory` resolve
  `<Prefix>_<Piece>`. Suggested mapping by the level spec's `Tint`:
  - Ashen Reach: Ground = `Ash_Ground`, GroundAlt = `Ash_GroundAlt` (ash), element layer = `Ash_Sand` (Sand
    levels and quicksand rims) or more `Ash_Ground` cracks (Fire levels); liquid banks = lava.
  - Northern Crown: `North_Snow` as the element layer on Ice levels, `North_GroundAlt` on Stone levels, embers of
    the Fire levels come from `North_Ground`'s amber cracks; banks = ice lake.
  - Mirewood: `Mire_GroundAlt` (moss) on Wood levels, `Mire_Mud` on Water/Bog levels; banks = bog and water.
  - Southern Crown: `South_CrackedIce` on Ice levels, `South_GroundAlt` (mixed rubble) on the mixed levels.
  - The Core: `Core_Plateau` for the base area and plazas, `Core_GroundAlt` round corrupted nests; banks = the
    violet corruption pools; the border is `Core_Cliff_*` edges with a void below, not walls.
- **PropKind mapping** (keep the hashes and scales): IceShard -> `North_Decor_IceShard`, Snowdrift ->
  `North_Decor_Snowdrift`, DeadTree -> `Mire_Flora_DeadTree`, Reed -> `Mire_Flora_Reed`; per continent the generic
  kinds map to its own decor (Boulder -> Ash_Decor_LavaRock / North_Decor_RimeBoulder / Mire_Decor_MossStone /
  South_Decor_MixedBoulder / Core_Decor_HexOutcrop; Crystal -> North_Decor_FrozenCrystal /
  South_Decor_UnstableCrystal / Core_Decor_CorruptedCrystal; Mushroom -> Mire_Decor_Fungus; GlowTree ->
  Ash_Flora_CharredTree; Frond -> Ash_Flora_EmberBloom / North_Flora_FrostFern; Spire -> Core_Decor_CrystalPillar;
  Spike -> Ash_Decor_Fulgurite / South_Decor_ShatteredPillar). New kinds worth adding: AshDune, FogVent,
  CrackedVein, PressureRidge, SealShard.
- **Sway**: every `Flora_*` has the `Sway` vertex colour exactly as round 8 (R bend, G phase, B flutter).
- **Mirewood FogVent**: its report has `fog_emitter` (local position) for a rising fog particle system.
- **Emission**: as round 8, about 2.2; the seal's gold about 2.4, lower it on Cracked/Broken from data if the
  story wants the light to fade.
- **Scatter**: `Scatter_*` (about 200 tris) with GPU instancing, one per two free cells.

### Kit and decor per continent

| Continent | Kit (tris) | Decor (tris) |
| --- | --- | --- |
| Ashen Reach | RoadEdge 700 / 498 / 400; Bank (lava) 600 / 398 / 300; Cliff (mesa strata) 1,500 x 3 | Decor_LavaRock 1,200, Decor_AshDune 1,200, Flora_CharredTree 1,500 (sway 1.4), Flora_EmberBloom 1,300 (sway 0.36), Decor_Fulgurite 1,200, Scatter_Cinders 200 |
| Northern Crown | RoadEdge 700 / 500 / 400; Bank (ice lake) 600 / 400 / 300; Cliff (hex basalt columns) 1,500 x 3 | Decor_IceShard 1,200, Decor_Snowdrift 1,000, Decor_FrozenCrystal 1,300, Decor_RimeBoulder 1,300, Flora_FrostFern 1,300 (sway 0.62), Scatter_IceChips 200 |
| Mirewood | RoadEdge 700 / 500 / 400; Bank (peat, roots) 600 / 400 / 300; Cliff (earth bank, roots) 1,500 x 3 | Flora_DeadTree 1,500 (sway 1.2), Flora_Reed 1,200 (sway 0.95), Decor_Fungus 1,300, Decor_FogVent 1,000 (`fog_emitter` in the report), Decor_MossStone 1,000, Scatter_Twigs 200 |
| Southern Crown | RoadEdge 700 / 500 / 400; Bank (tilted ice plates) 600 / 400 / 300; Cliff (pressure ridge) 1,500 x 3 | Decor_UnstableCrystal 1,300, Decor_CrackedVein 600 (2 x 0.9, flat), Decor_PressureRidge 1,200, Decor_ShatteredPillar 1,200, Decor_MixedBoulder 1,100, Scatter_Shards 200 |
| The Core | RoadEdge 700 / 498 / 400; Bank (corruption pool) 600 / 400 / 300; Cliff (plateau edge, drops 2.4) 1,500 x 3 | Seal_Intact / _Cracked / _Broken 8,000 each (1024), SealVein_Straight 600, SealVein_Branch 800, Decor_CorruptedCrystal 1,300, Decor_CrystalPillar 1,198, Decor_SealShard 1,200, Decor_HexOutcrop 1,200, Scatter_CrystalGrit 200 |

Cliff pieces are wider than their 2-cell footprint (masses and plates reach up to 0.4 past the ends, they overlap
the neighbours on purpose). Scatter footprints 0.1-0.33.

### FBX colour layers (differs from round 8)

The bake leaves helper colour layers (Albedo, Emit, Rough, Fam, AO) on the game mesh. Round 17 strips them before
export (`env_kit2.strip_colour_layers`): flora FBX files carry exactly one colour set, `Sway`; the other pieces
carry none. Unity imports only the first vertex-colour set, so round 8's `Flora_*` (six sets, `Sway` last) would
lose their sway data on import; check that when importing round 8.


## Round 18: P3 hybrids, gadgets, superweapons and drones (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 18). Same hard-surface pipeline as rounds 7, 9 and 11; nothing in the shared
modules or earlier scripts changed. New scripts (they import `build_towers.py`, `build_towers2.py`,
`build_gadgets.py`, `build_gadgets2.py` and `build_drones.py` read-only):

- `build_towers3.py` (`--only Shatter,ArcLaser,CorrosiveBlast,StormFlak,ThermalShock`, `--nobake`, `--norender`,
  `--preview <dir>`; `--lineup` renders all five at game zoom from the maps on disk). Its previews put each hybrid's
  two parents beside its three tiers (`<name>_parents_game/_zoom.png`).
- `build_gadgets3.py` (`--only Decoy,Barricade,SpikeStrip,Rollback,PayloadBomb,OverchargedCrystal,ToxinBomb`).
- `build_drones2.py` (`--only Builder,Hauler,Bombard`).

About 1 minute per tower and 30 s per gadget or drone with previews.

| Asset | Tiers | Triangles LOD0 (LOD1) | Bones, sockets and clips (towers: on top of the round-7 rig) |
| --- | --- | --- | --- |
| Tower_Shatter | T1, T2, T3 (Cryo + Railgun) | 3,582 / 4,166 / 4,706 (1,502 / 1,854 / 2,134) | IdleSweep 120, Fire_1 10, ChargeUp 24 (rails draw back) |
| Tower_ArcLaser | T1, T2, T3 (Laser + Tesla) | 3,682 / 4,138 / 4,870 (1,750 / 1,982 / 2,454) | `Prong_1..n` (3 / 4 / 6) with sockets `Arc_1..n` on the tips; IdleSweep, ChargeUp 24, Beam 12 (loop); no Fire_n (continuous) |
| Tower_CorrosiveBlast | T1, T2, T3 (Cannon + Acid) | 3,378 / 4,380 / 4,830 (1,546 / 2,104 / 2,450) | `Slosh` (meniscus); IdleSweep, Fire_n 10 (T3 two tubes), Slosh 60 (loop) |
| Tower_StormFlak | T1, T2, T3 (Flak + Tesla) | 4,068 / 4,692 / 4,992 (1,652 / 1,904 / 2,144) | rests at 40 degrees; sockets `Arc_1..n` on the spire prongs (3 / 4 / 6, on `Yaw`); IdleSweep, Fire_1/2, FireAlternate 16 (loop) |
| Tower_ThermalShock | T1, T2, T3 (Cryo + Lava Ball) | 4,086 / 4,748 / 4,966 (2,086 / 2,316 / 2,550) | IdleSweep, Fire_n (T3: Barrel_1 frost, Barrel_2 thermite) |
| Gadget_Decoy | – | 1,448 (840) | Root, Hammer, Head; sockets `Pulse` (ground), `Lure`. Deploy 24, Idle 60 (two thumps, loop), Pulse 20 |
| Gadget_Barricade | – | 1,592 (656) | Root, `Slab_1..5` (hinge at the slab foot); sockets `Spark_1..3`, `Smoke`. Deploy 30, Hit 8, Break 24 |
| Gadget_SpikeStrip | – | 1,212 (728) | Root, `Spikes_1..3`; socket `Hit`. Deploy 24, Idle 60 (loop), Trigger 10 |
| Gadget_Rollback | – | 1,746 (1,230) | Root, Dish, Ring; sockets `Push` (dish front), `Field` (ground, 0.45 up the road). Deploy 30, Idle 90 (loop), Pulse 12 |
| Super_PayloadBomb | – | 1,170 (754) | Root, Body, Fins; sockets `Blast` (nose), `Trail`. Fall 30 (loop), Arm 12 |
| Super_OverchargedCrystal | – | 1,312 (790) | Root, Crystal, Orbit, `Clamp_1..3`; sockets `Core`, `Blast`, `Arc_1..3` (clamp jaws). Deploy 30, Idle 60 (loop), Overload 36 |
| Super_ToxinBomb | – | 1,516 (796) | Root, Body, `Valve_1..4`; sockets `Vent_1..4`, `Cloud`, `Blast`. Fall 30 (loop), Vent 16 |
| Drone_Builder | – | 2,204 (1,428) | Root, Body, Rotor_1..3, Arm_L/R (7 deform); sockets `Weld_L/R`, `Beacon`. Idle, Fly, Work 60 (welding, loop) |
| Drone_Hauler | – | 1,376 (708), cargo 136 | Root, Body, Rotor_1/2, Grip; socket `Cargo`; `Drone_Hauler_Cargo` own mesh. Idle, Fly, Work 60 (drop and close) |
| Drone_Bombard | – | 2,148 (1,252) | Root, Body, Rotor_1/2, Bomb_1/2, Bay_L/R (8 deform); sockets `Drop_1..4`, `Thruster_L/R`. Idle, Fly, Work 60 (one strike) |

Files as rounds 7, 9 and 11: towers `Exports/Tower_<Name>/Tower_<Name>_T1..T3.fbx` + 1024 maps + `Low/` 512 + report
(muzzles, sockets, glow colours, `hybrid_of`); gadgets, superweapons and drones one FBX each, 512 maps + `Low/` 256,
report with pivot and sockets. Sources: `Blend/Towers3.blend`, `Gadgets3.blend`, `Drones2.blend`.

### Unity notes

- **Import** exactly as rounds 7 and 9 (Scale 1, Bake Axis Conversion, Generic rig, root `Root`, keep non-deforming
  bones). Final size; do not apply factory scales. As in earlier rounds the exporter warns that n-gon caps stop it
  writing tangents: let Unity calculate them (Mikktspace).
- **Hybrids:** three levels, the stars rule of round 7, swap the prefab on merge. Arc Laser: the beam leaves
  `Muzzle_1`; the jump to the next enemy starts at the nearest `Arc_n`; play ChargeUp then Beam as the Laser.
  Storm Flak: shells from `Muzzle_n`, chain lightning from `Arc_n` on the spire. Corrosive Blast: Slosh on an
  additive layer as the Acid. Thermal Shock T3: Barrel_1 shoots frost, Barrel_2 heat.
- **Decoy:** pivot on the ground; Deploy, then Idle; a ground ripple VFX at `Pulse` on each hammer strike (frames 21
  and 51 of Idle); the lure light at `Lure`. **Barricade:** pivot at the sill centre, local X across the road, the
  slabs lean to Unity +Z: face that side towards the enemies. Deploy on placement, Hit when struck, Break at 0 HP
  (then remove). **Spike Strip:** spans 1.8 across the road; Trigger when an enemy crosses. **Rollback:** face the
  dish (Unity +Z) up the road towards the enemies; the push field VFX at `Field`, a pulse at `Push`.
- **Superweapons:** the bombs stand nose down with the pivot on the nose: drop them from above so the pivot lands on
  the target; Fall loops in the air, the blast at `Blast`. Toxin: Vent on impact, gas at `Vent_n` and `Cloud`.
  Crystal: pivot on the ground; Deploy, Idle, Overload before the explosion (VFX at `Core`/`Blast`, arcs from `Arc_n`).
- **Drones:** as round 9 (spin `Rotor_n` in code). Builder: weld sparks at `Weld_L/R` during Work. Hauler: parent the
  cargo to `Cargo`, give `Drone_Hauler_Cargo` its own material instance for the glow; Work releases. Bombard: Work is
  one strike: the doors open by frame 9, spawn the bomb projectiles at `Drop_1/2` on frame 15 and `Drop_3/4` on
  frame 24 (the modelled bombs are hidden from then until frame 48).
- **Colours (open):** none of these are in the data yet; the glow colours are proposals (towers: report key `glow`;
  the round's shared palette under `glows`; gadgets and drones: `GL` in their scripts).

## Round 19: the mothership rebuilt (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 19). Build: `build_mothership2.py` (own lofted hull pieces, weighted normals
and a ship-scale self-bake with plating, windows, livery and projected decals; reuses `tower_kit` pieces, unwrap
helpers and the FBX export read-only). It **replaces** the `Mothership` spec of `build_cutscene.py`: same file names,
bones, clip and sockets, so the round-13 import keeps working.

```
blender --background --factory-startup --python Tools/Blender/build_mothership2.py -- [--preview <dir>] [--norender]
        [--nobake] [--fast] [--old <folder with a previous export, for compare_*.png>]
```

- Full bake (2048, 16 samples) takes about 35 minutes on the CPU; `--fast` (1024, 6 samples) is for look-dev only and
  must not be left in `Exports/`. `--nobake` re-exports the meshes on the maps on disk (the atlas is deterministic).
- Previews: `ms2_hero`, `_side`, `_under`, `_bay`, `_rear`, `_nose`, `_ring`, `_top`, `_far` (320 x 180), the LOD1 at
  `ms2_lod1_hero/_far`, and with `--old` the side-by-sides `compare_hero/_side/_under/_rear` (left round 13).
- **build_all.py:** `build_cutscene.py` still builds its own (round-13) mothership into the same folder, so
  `build_mothership2.py` must run **after** it: add it right after `"build_cutscene.py"` in `SCRIPTS`.

| Asset | Files (`Exports/Cutscene_Mothership/`) | Triangles | Maps | Rig, clips, sockets |
| --- | --- | --- | --- | --- |
| Cutscene_Mothership | `Cutscene_Mothership.fbx`, **new** `Cutscene_Mothership_LOD1.fbx` | 19,442 (budget 40,000); LOD1 8,594 (44 %: no chamfers, no greebles, no ring liner) | **2048** (+ `Low/` 1024) | `Cutscene_Mothership_Rig`: Root, Ring (both deform). Spin 360 (loop, one turn per 12 s about +Y, Unity +Z). Empties `Bay` (+Z down out of the bay), `Nozzle_1..3` (+Z aft), **new** `Light_1..10` |

Hull length 14.95 (Blender y -7.35 nose to 7.6 bell exits), 15.7 with the sensor booms (round 13: 13.6); ring radius 3.42 outside (was 3.0), so it is
about 10 % bigger in the same scale. Pivot at the ship centre (between the cargo racks), nose -Y (Unity +Z).
`Bay` moved from (0, 1.0, -0.96) to (0, 0.54, -1.17); `Nozzle_n` sit 1.1 aft of the gimbals at y 7.78.

New sockets `Light_n` (static, on Root; for blinkers or small point lights): 1-2 the sensor boom tips (white),
3 the dorsal beacon (yellow), 4 the nose cap (white), 5-6 the bay mouth corners (amber), 7-8 the radiator tips (cyan),
9-10 the engine block top and bottom (white). The glow pips are in the mesh and the Emission map already; the ring's
own lights turn with it and have no sockets.

### Unity notes

- **Import** as round 13 (Scale 1, Bake Axis Conversion, Generic rig, keep empties, Mikktspace tangents). The FBX
  carries **custom (weighted) normals**; `BlenderModelImporter` already imports normals from the file
  (`ModelImporterNormals.Import`). Keep it that way: recalculated normals would no longer match the normal map.
- **2048 maps:** `BlenderModelImporter.FullMapSize` is 1024, so Unity clamps these maps to 1024 today. For the
  hero asset raise the limit for this asset only (High 2048, `Low/` 1024), e.g. a per-asset override in the importer.
- **LOD1:** `BlenderStoryBuilder.BuildArt` sets `_lod1` to null for cutscene models. To use the new
  `Cutscene_Mothership_LOD1.fbx` (synced by `BlenderSync` automatically), assign it there as `BlenderArtBuilder` does.
  Without that it is simply unused.
- **Emission:** windows, ring lights and the bells' throats are in the Emission map (pale blue-white, throats
  weaker and bluer than round 13); `OrbitSet`'s exhaust sprites at `Nozzle_n` stay. A small cool point light at
  `Bay` during the capsule drop would light the racked capsules and the doors' inner sides.
- The mask layout is the usual one (R AO, G smoothness, B metal detail, A ceramic detail); the report's detail tiling
  numbers are computed from the atlas density as before.


## Round 19: Dr. Voss rebuilt (`build_voss2.py`, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 19). Replaces `build_voss.py`'s exports (same paths and names);
`build_voss.py` stays for reference but should no longer run in `build_all.py` (it would overwrite these).

| Asset | Files (`Exports/Voss/`) | Triangles | Maps | Rig and clips |
| --- | --- | --- | --- | --- |
| Voss | `Voss.fbx` (mesh `Voss`, armature `Voss_Rig`) | 20,144 (budget ~20k), no LOD1 | **2048** (+ `Low/` **1024**), one material, Mask B weave, A ceramic; the face island at 2.4x density | 10 bones as before (heads moved to the new anatomy); Idle 120, Talk 90, Blink 8 (blink at 70/44 as before); shape keys Blink, Smile, JawOpen, Frown |

Run: `blender --background --factory-startup --python Tools/Blender/build_voss2.py -- [--preview <dir>] [--norender]
[--stage <dir>] [--size 2048]` (about 1 minute plus previews).

### Unity notes

- **Same names, sockets and clips** (Voss had no sockets): `Voss.fbx`, `Voss_Rig`, the 10 bones, `Idle`/`Talk`/`Blink`;
  the portrait pose (Idle frame 10) is open-eyed, mouth closed. The eyes stay on y 1.44, so the catalog framing
  (centre 1.4, turn -14, zoom 1.25) holds; the face is a little larger in frame (head drawn 6 % large).
- **Full body now:** feet on y 0, coat to the knee, hands in pockets; usable as a cutscene figure (stands, breathes,
  talks; no walk).
- **Texture size:** the maps are 2048 (Low 1024). `BlenderModelImporter` caps full maps at 1024 and Low at 512; raise
  the cap for `Voss` (or for story busts) to get the face detail, otherwise Unity downsizes them (still better than
  before).
- **Shape keys** are in the FBX but `BlenderModelImporter` has `importBlendShapes = false`; turn it on for Voss to use
  Smile/Frown in portraits. The bone-driven Blink and Talk work without them. The FBX also carries the (zero) blend
  shape channels per take; Unity ignores them while blend shapes are off.
- Detail tiling in the report: weave 111.4 (B), ceramic 25.3 (A).

## Round 20: S-69-CRYS planet texture set

`blender --background --factory-startup --python Tools/Blender/build_planet_s69.py -- [--res 4096] [--renders <dir>]
[--compare <screenshot.png>] [--norender]` (~3 min at 4096, numpy only, no downloads). Output
`ArtSource/Exports/Planet_S69/` + `Low/`: `Planet_S69_Albedo`, `_Height` (16-bit), `_Normal`, `_Mask`, `_Regions`,
`_Emission`, `_Clouds`, `_report.json`.

### Unity notes

- **UV:** identical to `PlanetCoordinates.ToUv` (u = (lon+180)/360, east right; v = (lat+90)/180; PNG top = north), so
  the existing `PlanetGlobe` mesh, level points and zoom line up unchanged. Wrap U Repeat, V Clamp.
- **Import:** Albedo and Emission sRGB; Normal as Normal map (R east, G north, like `GlobePainter.Normals`); Height,
  Mask, Regions, Clouds linear (sRGB off). Regions: Point filter, no mips (or sample with LOD 0).
- **Regions:** id = round(value * 255 / 40): 1 Crystal Plains, 2 Ashen Reach, 3 Northern Crown, 4 Mirewood,
  5 Southern Crown, 6 The Core (crater), 0 sea. R = land only, G = land + its sea territory (good for a highlight),
  B = soft membership for the edge. Mask.A carries the land ids too, but in `Low/` Mask is box-filtered, so read ids
  from Regions.
- **`Game/Planet` shader:** Albedo -> `_AlbedoMap` with `_TextureTint` 1 (or `_Alien` 0) so the painted biomes are not
  replaced by the procedural palette; Normal -> `_NormalMap` (`_BumpStrength` 1-1.5 map, 2 cutscene); Emission ->
  `_LightsMap` (neutral `_CityColor` ~ 1.6 white, since the map is already coloured; ~0.4 by day on the map view);
  Clouds -> `_CloudMap` (.a). `_GlossMap` reads the sea from .a: either bake a gloss texture with A = Mask.G or read
  Mask.r/g in the shader. `GlobePainter`'s colony lights can stay as a second emission layer.

## Round 20: Dr. Voss on MPFB 2 (`build_voss3.py`, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 20). Replaces round 19's exports (same paths and names). **Requires** the MPFB
extension and its CC0 packs (makehuman_system_assets, hair01, faceunits01); install: `Docs/COMMANDS.md`, section
"Blender: MPFB 2". `build_all.py` now runs `build_voss3.py` instead of `build_voss.py`.

| Asset | Files (`Exports/Voss/`) | Triangles | Maps | Rig and clips |
| --- | --- | --- | --- | --- |
| Voss | `Voss.fbx` (mesh `Voss`, armature `Voss_Rig`) | 25,228, no LOD1 | 2048 (+ `Low/` 1024), **one material** (atlas); Mask R AO, G smoothness, B weave, A ceramic | the same 10 bones (Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R); Idle 120 (blink at 70), Talk 90 (jaw up to 8 degrees, blink at 44), Blink 8; shape keys Blink, Smile, JawOpen, Frown |

Run: `blender --background --factory-startup --python Tools/Blender/build_voss3.py -- [--preview <dir>] [--norender]
[--stage <dir>] [--size 2048] [--cache <file.npz>] [--recapture] [--geo] [--quick] [--old <round-19 export dir>]`
(about 1.5 minutes plus previews). `--cache` keeps the MPFB capture so later runs skip MPFB, and `--geo` renders flat
part colours for fit checks. The script enables MPFB itself, so `--factory-startup` works.

### Unity notes

- **Same names, bones and clips** as rounds 13/16/19; no sockets. Feet on y 0 and eyes on y 1.44, so the catalog
  framing (centre 1.4, turn -14, zoom 1.25) holds. Idle frame 10 is open-eyed with the mouth closed.
- **Weights** come from MPFB's default rig, mapped onto the 10 bones: the legs to Root, spine05-03 to Spine,
  spine02-01 and the breasts to Chest, each arm to its clavicle, the neck bones to Neck, the jaw, tongue and lower-lip
  bones to Jaw, the rest of the face to Head. The lids come from the blink target (the Lid bones rotate +X by
  48 degrees). Clothes weights as in round 19.
- **One material**: `BlenderStoryBuilder` builds one material per export, so everything is in one atlas (no
  `SecondMaterial`).
- Texture size and blend shapes as in round 19: raise `BlenderModelImporter`'s cap for Voss to 2048 (Low 1024) and
  turn `importBlendShapes` on to use Smile/Frown in portraits. Blink and Talk work through the bones without them.
- The FBX also lists `Key|Voss_Rig|<clip>` takes (the zero shape-key channels per take, as in round 19).

## Round 20: VFX texture set (`build_vfx.py`, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 20). Texture source for the Unity VFX rebuild (projectiles, muzzle flashes,
explosions, smoke, sparks, rings, distortion, decals); nothing is imported yet.

Run: `blender --background --factory-startup --python Tools/Blender/build_vfx.py -- [--preview <dir>] [--cache <dir>]
[--only Name,Name] [--no-volumes] [--norender]`. Modules: `vfx_kit.py` (PNG writer, periodic noise, resampling,
volume renderer, preview shading), `vfx_2d.py` (procedural sprites and flipbooks), `vfx_sim.py` (Mantaflow sims).
A full build from scratch takes about 40 minutes, almost all of it the six sims (Explosion_Boss alone ~15 min). The
sims' rendered maps are cached in `%TEMP%/td_vfx_cache/<Name>.npz`, so a rerun takes a few minutes; delete a `.npz`
after changing that sim's spec in `build_vfx.volume_specs()`. With the cache in place (or `--no-volumes`) the script
also runs in Blender's bundled Python without Blender:
`"C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe" Tools/Blender/build_vfx.py -- ...`.
`VFX_report.json` lists every texture with size, layout, frame count, fps, lifetime, pivot, channels, blend mode and
tint; the tables below summarise it.

### Conventions

- **All textures are linear data**: import with sRGB off (also the colour-looking ones); colour comes from tints and
  gradients. Do not import the distortion maps as Normal Map (Unity would drop B). Wrap Clamp except the tiling ones.
- **Flipbook order**: frame 0 top-left, then row by row (Unity Texture Sheet Animation order).
- **CG pack** (most emissive textures): R = core (white-hot; stays white under any tint), G = glow (multiply by the
  weapon tint, `TowerDefinition._color`), B = a named secondary layer, A = coverage max(R, G, B). Additive:
  `col = tint * G * glow + white * R * core`.
- **Six-way pack** (fireballs, smoke, dust; the Unity VFX Graph "Six-way Smoke Lit" layout): `_SixWayPos` = R light
  from right, G from top, B from back (behind the smoke), A alpha (straight); `_SixWayNeg` = R from left, G from
  bottom, B from front (camera side), A fire temperature 0..1. Lighting = sum of the six maps weighted by the squared
  components of the light direction in billboard space (they sum to 1); ambient = mean of the six x ambient colour.
- **Low/**: every texture at half size (box filter) for the Low (Switch) profile.

### Textures

| Texture | Size | Layout | Channels | Blend, tint, timing |
| --- | --- | --- | --- | --- |
| `Explosion_Small_SixWayPos/Neg` | 1024 | 8 x 8 flipbook, 128 px | six-way | alpha + fire gradient; 60 fps, 1.07 s; cannon shell, mines, small gadgets |
| `Explosion_Large_SixWayPos/Neg` | 2048 | 8 x 8, 256 px | six-way | 48 fps, 1.33 s; mortar, rocket, barrel, lava ball, tower destroyed |
| `Explosion_Boss_SixWayPos/Neg` | 2048 | 8 x 8, 256 px | six-way | 40 fps, 1.6 s; three chained sub-bursts; boss death, base, superweapons |
| `Smoke_Puff_SixWayPos/Neg` | 1024 | 8 x 8, 128 px | six-way (Neg.A = 0) | 30 fps, 2.1 s; dark billow after explosions, wrecks, damaged towers |
| `Smoke_Linger_SixWayPos/Neg` | 1024 | 8 x 8, 128 px | six-way | 24 fps, 2.7 s; rising column over craters and burning wrecks (chain 2-3 particles) |
| `Dust_Burst_SixWayPos/Neg` | 1024 | 8 x 8, 128 px | six-way | 40 fps, 1.6 s; low ground-hugging burst; tint tan (0.45, 0.38, 0.3) per level palette |
| `Bolt_Core` | 256 x 64 | single | CG, B = tail streak | additive; the rounded bar of Bolt/Rail/Flak/BaseTurret rounds; head at +U |
| `Trail_Ribbon` | 512 x 64 | single (Stretch) | CG | additive; U 0 = head, U 1 = tail, tapers |
| `Trail_Noise` | 256 x 64 | tiles U and V | R strands, G wisps | multiply into trails/beams, scroll U |
| `Plasma_Bolt` | 512 | 4 x 4 loop, 128 px | CG, B = swirl bands | additive, 24 fps loop; head at +U |
| `Rocket_Exhaust` | 1024 x 512 | 4 x 4 loop, 256 x 128 | CG (R core + shock diamonds), B = smoke start | additive, 30 fps loop; nozzle at U 0, flame to +U |
| `Smoke_Trail` | 512 x 128 | tiles U | R lit from the top edge, G puff noise, A density | alpha blend, grey (0.35); rocket trail ribbon |
| `Beam_Core` | 128 x 64 | tiles U | CG | additive; Laser, CryoBeam |
| `Beam_Noise` | 512 x 128 | tiles U and V | R filaments, G crackle, B swirl | multiply into the beam, two scroll speeds |
| `Tesla_Arcs` | 2048 x 512 | 4 x 4, 512 x 128 | CG, B = branches | additive, 20 fps or a random frame per discharge; U 0.02 to 0.98 at V 0.5 |
| `Muzzle_Side` | 1024 x 512 | 4 x 4, 256 x 128 | CG, B = smoke (cannon) | rows Gun, Cannon, Flak, Rail/Plasma; columns = variants; barrel at U 0.05 V 0.5; 1-3 frames |
| `Muzzle_Front` | 1024 | 4 x 4, 256 | CG, B = smoke (cannon) | same rows; random roll |
| `Spark_Burst` | 1536 | 6 x 6, 256 | CG | additive, 60 fps, 0.6 s; pivot U 0.5 V 0.42 (impact point) |
| `Spark_Streaks` | 512 x 256 | 4 rows, 512 x 64 | CG | stretched-billboard spark particles, random row; head at +U |
| `Debris_Shards` | 512 | 4 x 4 sprites, 128 | R hot edge, G body alpha, B facet shading | alpha body (dark tint x B) + additive edge; random frame |
| `Shockwave_Ring` | 512 | single | CG, B = dust | additive or alpha dust ring; scale 0.2 to 1 with ease-out over 0.25-0.4 s |
| `Shockwave_Distort` | 512 | single | RG UV offset (0.5 +- 0.5, +Y up), B mask | refraction: screenUV + (RG*2-1) * B * strength (0.02-0.05) |
| `HeatHaze_Distort` | 256 | tiles U and V | RG offset, B height | refraction, scroll V up, strength 0.004-0.01 |
| `Scorch_Decal` | 1024 | 2 x 2 variants, 512 | R soot opacity, G ash lightness, B ember mask, A ember fade threshold | decal; emission = B * step(fade, A); embers ~2 s, soot ~8 s |
| `Energy_Impact` | 1536 | 6 x 6, 256 | CG, B = inner ring | additive, 60 fps, 0.6 s; weapon tint |
| `Cryo_Burst` | 1536 | 6 x 6, 256 | CG (R glints + ridges, G shards), B = frost mist | additive + alpha mist, 48 fps, 0.75 s; ice cyan |
| `Acid_Splash` | 1536 | 6 x 6, 256 | R lit (premultiplied), G thickness, B specular, A alpha | premultiplied alpha, 40 fps, 0.9 s; lime; pivot U 0.5 V 0.34 |

Sizes on disk: see `total_bytes` in the report. Previews (`--preview`): `vfx_<Name>.png` (16 frames),
`vfx_<Name>_relight.png` (one frame lit from left, right, top and front: proves the six-way maps relight) and
`vfx_<Name>_channels.png` (the eight six-way channels).

### Unity notes (for the VFX rebuild)

- **Shaders needed**:
  1. `Game/VfxAdditive` (CG pack): flipbook UV with **frame blending** (sample frame n and n+1, lerp by the fraction;
     Shuriken: Texture Sheet Animation + the UV2 and AnimBlend custom vertex streams), `col = _Tint * G * _Glow +
     _Core * R`, times the particle colour; **soft particles** (depth fade against `_CameraDepthTexture`, 0.3-0.6 m)
     so ground hits do not cut a hard line.
  2. `Game/VfxSixWaySmoke`: alpha blend; light direction in billboard space (main light dotted with the particle's
     right/up/forward), weights = squared components; lit = sum(maps x weights) x light colour x albedo + ambient
     (SH or the theme ambient) x mean of the six; emission = gradient(Neg.A) x intensity (fire gradient like
     `vfx_kit.fire_ramp`: deep red, orange, yellow, white core); frame blending and soft particles as above. Keep
     boss explosions on their own emission curve so a boss death never flashes the screen white. VFX Graph's
     built-in Six-way Smoke Lit output takes these two maps directly if VFX Graph is used.
  3. `Game/VfxDistort`: transparent queue after opaques, samples `_CameraOpaqueTexture` at screenUV +
     (tex.rg*2-1) * tex.b * _Strength * particle alpha. `Game/ImpactRipple` already does this with a procedural
     ring; it can switch to `Shockwave_Distort` for a richer edge. Low profile: skip the pass.
  4. A premultiplied-alpha variant (Blend One OneMinusSrcAlpha) for `Acid_Splash`, `Debris_Shards`, `Smoke_Trail`.
- **Particle setups**: explosion = Explosion_* (1 particle, small random roll, size from `world_size` in the report)
  + Spark_Streaks (stretched billboard, gravity, 20-40) + Debris_Shards (8-16, gravity, rotation) + Shockwave_Ring
  (flat on the ground) + Shockwave_Distort + Scorch_Decal + a Smoke_Linger tail; a point light with a short curve.
  Energy hits = Energy_Impact + a few Spark_Streaks in the weapon tint. Muzzle = one random cell of Muzzle_Side
  aligned to the barrel, pivot at the barrel, plus Muzzle_Front when the barrel faces the camera; 50 ms.
- **Pivots**: explosions, smoke and dust put the sim floor at `pivot` V (from the cell bottom) in the report; set the
  particle pivot so the blast sits on the impact point. Every flipbook ends empty (the last frames fade), so
  particles can die on the last frame without a pop.
- **Budgets**: High uses the full set; Low uses `Low/`, no distortion pass and half the spark/debris counts.

## Round 21: S-69-CRYS planet, balanced layout (`build_planet_s69.py` + `layout.json`, 2026-11-04)

`blender --background --factory-startup --python Tools/Blender/build_planet_s69.py -- [--res 4096] [--renders <dir>]
[--compare-dir <old export dir>] [--layout <layout.json>] [--layoutonly]` (~3 min at 4096; `--layoutonly` writes a
flat layout preview in ~5 s). Same files, names, UV convention and region-id values as round 20. Design notes in
`DESIGN_NOTES.md` (round 21).

### Unity notes

- **Apply `ArtSource/Exports/Planet_S69/layout.json` to the data:** region centres are unchanged; set The Core's
  `RadiusDegrees` to 14 (crater id radius). Replace the 20 Crystal Plains `MapNode` Longitude/Latitude with
  `nodes[]` (same order: Landing Site first), e.g. in `ProjectGenerator.Campaign` instead of `NodePlacer.Route` for C1,
  so a regenerate does not overwrite them. All nodes lie within 26.5 deg of the C1 centre (radius 30 stays valid),
  min spacing 8.25 deg, >= 4.3 deg from the coast.
- `GlobePainter.Ruggedness` no longer matches the painted mountains; do not use it to re-place nodes on this texture.
- `Planet_S69_report.json` now has `surface` (land % and p95 extent per region), `coast_biases`, `nodes_ok`.

## Round 22: S-69-CRYS planet, smaller Crystal Plains (2026-11-04)

Same command, files and region ids as round 21; `layout.json` version 22. Unity notes: apply the new 20 Crystal
Plains node lon/lat values from `layout.json` (order unchanged, Landing Site first) and lower the node spacing rule
(`PlanetCoordinates.MinimumSpacingDegrees` / `NodePlacer.MinimumSpacingDegrees`) to 6 deg; nodes are >= 6.23 deg
apart, >= 3.5 deg from the coast, within the 30 deg C1 radius. The Core `RadiusDegrees` 14 as in round 21.

## Round 22b: organic Crystal Plains node route (2026-11-04)

`--placenodes [--seed N]` re-places the 20 Crystal Plains nodes into `layout.json` (seeded, deterministic for the same
layout); `--rendernodes` renders the node close-up with the dotted route from the current exports. Unity notes: apply
the new `nodes[]` lon/lat (order unchanged, Landing Site first); spacing >= 6.18 deg, rule 6 deg as in round 22.

## Round 24: liquid and mist texture set (`build_liquids.py`, 2026-11-04)

Remco: the water (and lava, mist) still looks fake. Procedural replacement for the textures that `LiquidTextures.cs`
paints in code today, designed for one URP liquid surface shader (to be written in Unity; nothing is imported yet).
Design notes in `DESIGN_NOTES.md` (round 24); the shader recipe is also in `Liquids_report.json` (`shader`,
`conventions`, per-kind `palettes`).

Run: `blender --background --factory-startup --python Tools/Blender/build_liquids.py -- [--only Water,Lava]
[--preview <dir>] [--norender] [--previewonly] [--samples 32] [--compare-dir <dir;dir>]` (~1 min textures, ~4 min
previews). Textures only, without Blender: `"C:\Program Files\Blender Foundation\Blender 5.2\5.2\python\bin\python.exe"
Tools/Blender/build_liquids.py -- --norender`. Modules: `liquid_kit.py` (periodic spectral noise, periodic Worley, FFT
ocean with a loopable time axis, photon-splat caustics, normals, PNG), `liquid_textures.py` (one builder per kind),
`liquid_preview.py` (shore scenes with the exported bank pieces, ground tiling sets and the game camera; preview shaders
that follow the recipe), `liquid_shader_notes.py` (the recipe as data). In `build_all.py` before the diorama.
Output: `ArtSource/Exports/Liquids/<Kind>/*.png`, `<Kind>/Low/` (half size), `Liquids_report.json`; 32 MB full size
(40 MB with Low). All tiling maps are seamless (seam ratio ~1 in the report); every flipbook loops.

### Conventions

- Only `*_Albedo` are sRGB. Normal maps OpenGL (G up), import as Normal map. All masks, heat, flow, flipbooks and mist
  are linear (sRGB off). Wrap Repeat, except flipbooks, `Quicksand_Vortex` (Clamp) and `Water_ShoreProfile` (Repeat U,
  Clamp V).
- Flipbooks: 4 x 4 frames of 256 px, frame 0 top-left, row by row (Unity Texture Sheet Animation order). The caustic
  frames also tile: sample frac(uv) inside the frame cell (scale the cell UV by 254/256 or bias the mip against edge
  bleeding).
- Flow maps: RG = vector * 0.5 + 0.5 (R = +x, G = +y of the texture = world +z when uv = worldPos.xz), B = speed.
- Sample everything with **world-space UVs** (`uvW = worldPos.xz / tile`): cell-shaped pools then tile without mesh UVs.
  Tile sizes below are world units (CellSize 1).
- Textures are neutral; tints per continent are in the report (`palettes`), meant for theme data, not code.
- **Shore and depth**: `depth` = scene depth behind the surface minus the surface depth (`_CameraDepthTexture`; with the
  ortho camera use linear01 * (far - near)); `depth01 = saturate(depth / range)`; `shore_dist` = depth scaled to cells
  (the basin floor slopes, so depth grows with the distance to the bank). Low profile (no depth texture): bake a
  distance-to-bank texture per pool from the cell mask (8 px per cell, CPU, once at level build) and sample it with
  pool-local UVs; the Blender previews do exactly that.

### Textures

| Kind | Texture | Size | Channels | Use |
| --- | --- | --- | --- | --- |
| Water | `Water_Normal_Swell` | 1024 | normal | FFT wind sea, a few long crests; tile 4.8, scroll (0.035, 0.012) tiles/s |
| Water | `Water_Normal_Ripple` | 1024 | normal | capillary chop; tile 1.65, rotated 40 deg, scroll (-0.05, 0.07), strength 0.7 |
| Water | `Water_Caustics_Flip` | 4 x 4 x 256 | grey | real caustics (photons through an animated ocean onto a flat floor), each frame tiles; 8 fps, loops |
| Water | `Water_Caustics` | 1024 | grey | static caustic tile for Low: two scrolled samples, `min()` |
| Water | `Water_Foam` | 1024 | R lace foam, G bubbles, B dissolve noise, A preview | tile 1.8 |
| Water | `Water_ShoreProfile` | 256 x 16 | grey | breaking crest over one wave period: sharp front, frothy tail |
| Lava | `Lava_CrustA/B_Albedo` | 1024 | RGB sRGB basalt plates, A crust coverage (0 = seam) | A tile 5.5, B tile 4.4 rotated 63 deg |
| Lava | `Lava_CrustA/B_Normal` | 1024 | normal | domed plates, pahoehoe ropes, cracks |
| Lava | `Lava_CrustA/B_Heat` | 1024 | R seam heat, G halo, B molten underflow | emission through the palette ramp |
| Lava | `Lava_Flow` | 1024 | RG curl-noise distortion, B speed | tile 8.25, offset 0.06 |
| Lava | `Lava_Bubble_Flip` | 4 x 4 x 256 | R emission, G skin shade, B height, A alpha | swell 0-9, burst 10, crater 11-15; 10 fps |
| Bog | `Bog_Normal` | 1024 | normal | viscous swell, two samples (4.5 and 2.45 rotated 75 deg), very slow |
| Bog | `Bog_Scum` | 1024 | R film lightness, G glow spots, B gloss, A coverage | duckweed-like alien film; tile 3.5 |
| Bog | `Bog_Bubble_Flip` | 4 x 4 x 256 | R highlight, G film shade (0.5 neutral), B height, A alpha | swell 0-8, pop 9, rings 10-15; 10 fps |
| Quicksand | `Quicksand_Albedo` | 1024 | RGB sRGB near-neutral sand, A trough wetness | tile 4.5 |
| Quicksand | `Quicksand_Normal` | 1024 | normal | swirled ripple bands (contours of the stream function behind the flow) + grains |
| Quicksand | `Quicksand_Flow` | 1024 | RG flow along the bands, B speed | two-phase flow map, strength 0.1, 0.35 cycles/s |
| Quicksand | `Quicksand_Vortex` | 512 | RG inward spiral, B speed, A sink mask | optional single vortex per pool (pool-local UV, Clamp) |
| Quicksand | `Quicksand_Ring_Flip` | 4 x 4 x 256 | RG normal xy, B height, A alpha | dimple + three rings where something sinks; 8 fps |
| Quicksand | `Quicksand_Sparkle` | 1024 | grey | rare crystal grains; `* pow(N.H, 200)` |
| Ice | `Ice_Albedo` | 1024 | RGB sRGB pale ice, A milkiness (0 = clear, show the depth colour) | tile 4.5 |
| Ice | `Ice_Normal` | 1024 | normal | gentle undulation + crack grooves (static) |
| Ice | `Ice_Cracks` | 1024 | R crack lines, G fracture planes, B trapped bubbles | R = glint/emission mask |
| Ice | `Ice_Frost` | 1024 | R frost density, G sparkle points, B frost height | frost = density beating a threshold that rises with `shore_dist` |
| Corruption | `Corruption_Normal` | 1024 | normal | heavy viscous blobs, raised veins; two slow samples |
| Corruption | `Corruption_Veins` | 1024 | R vein emission, G pulse phase, B mottle | pulses travel as `sin(2 pi (G * 3 - t * 0.35))` |
| Mist | `Mist_Layer_A/B/C` | 1024 | grey density | banks with wide gaps / long wind streaks / fine haze |
| Mist | `Mist_Wisp_Flip` | 4 x 4 x 256 | R top-lit shade, A density | rolling wisp card; 6 fps, loops |
| Mist | `Steam_Flip` | 4 x 4 x 256 | R shade, A density | rising lava/vent steam column; 10 fps, loops |
| Mist | `Heat_Distortion` | 1024 | RG screen offset (0.5 = none) | vertical shimmer over lava, scroll up; offset 0.004 |

### Unity shader recipe (one `Game/Liquid` shader with a keyword per kind)

- **Water**: N = whiteout(Swell, Ripple * 0.7); colour = lerp(shallow, deep, depth01), range ~0.8; alpha lerp(0.72,
  0.96, depth01); refraction offset N.xy * 0.03 * depth01 on `_CameraOpaqueTexture` (High). Caustics flipbook at the
  reconstructed floor xz / 2.1, * tint * 0.22 * (1 - depth01), added as emission. Foam: F = Foam(uvW / 0.6);
  phase = frac(shore_dist * 2.2 - t * 0.28 + F.b * 0.45); band = ShoreProfile(phase) * smoothstep(0.75, 0.05,
  shore_dist); fringe = smoothstep(0.1, 0, shore_dist); cover = max(band * 0.85, fringe);
  foam = saturate((F.r + cover - 1) * 3) max F.g * cover * 0.9; smoothness 0.95 -> 0.45 under foam. The existing ring
  and glint particles stay.
- **Lava**: offset = (Flow.rg - 0.5) * 0.06; crusts A and B scroll (0.018, 0.006) and (-0.012, 0.014);
  w = smoothstep(0.42, 0.58, slow 3D noise (or a third scrolled Flow.b)) picks the top crust, so plates break up and
  re-form; heat = (seam * (0.55 + 0.6 * under) + halo * 0.1) * (0.55 + 0.6 * smoothstep(0.1, 1.4, shore_dist));
  molten = smoothstep(0.7, 0.95, under) * smoothstep(0.6, 1.2, shore_dist), heat = max(heat, molten * 0.75);
  emission = ramp(heat) * 1.6 (ramp black until 0.3, peak ~4 HDR) with the current +-15 % pulse. Bubble flipbook
  sprites replace the procedural lava bubbles; Steam_Flip cards + Heat_Distortion above it (High).
- **Bog**: two normal samples; water = lerp(shallow, deep, depth01), alpha 0.85-0.97; scum a = saturate(S.a + shore *
  0.35) (it gathers at the bank); albedo lerp(water, scumTint * S.r, a); emission glow * S.g * a * 1.4.
- **Quicksand**: opaque; two-phase flow map (p0 = frac(t * 0.35), p1 = frac(p0 + 0.5), w = |1 - 2 p0|) over Albedo and
  Normal; darker and damper towards the middle (45 % * 0.35 * smoothstep(0.2, 1.6, shore_dist)); sparkle glints; ring
  flipbook where units sink.
- **Ice**: colour = lerp(deep, Albedo * tint, milky * 0.85 + 0.1); a parallax second sample of Cracks.g and Albedo.a at
  uvW + viewDir.xz * 0.04 for depth (High); frost from the bank (threshold `shore_dist * 2.6`); emission cracks * 0.35
  + frost sparkle * 0.3 (* IceGlow); static normal.
- **Corruption**: two slow normal samples, smoothness 0.82; albedo darkened by mottle; emission = vein tint * R * pulse
  * smoothstep(0, 0.25, shore_dist) * 1.0-1.3.
- **Mist**: three world-space layers at 0.12 / 0.32 / 0.6, uvW / 7, 5, 3.5, scroll (0.012, 0.003), (0.025, 0.004),
  (0.04, -0.01); in-game alpha 0.16 / 0.1 / 0.06 (the previews use 0.22 / 0.14 / 0.09), theme mist colour, soft depth
  fade 0.3 against geometry; keep total alpha <= 0.3 over road and pads. Wisp cards at the border and nests.
- **Low (Switch)**: water one normal + static caustics (two samples, min), foam fringe only, no refraction; lava layer
  A only, no distortion or steam shimmer; bog and corruption one normal; quicksand one sample; ice without parallax and
  glints; mist layer A only at alpha 0.12. All from `Low/`. Distance texture instead of the depth texture.

### Previews

Into `--preview` (default `ArtSource/Previews/Liquids`): `liq_<Kind>_game.png` (game camera, ortho 7.5),
`liq_<Kind>_close.png`, `liq_<Kind>_strip.png` (four moments 0.8 s apart), `liq_<Kind>_compare.png` (current Unity
screenshot | Blender), `liq_Mist_compare.png` (without | with mist), `liq_Mist_vs_unity.png`, `liq_Mist_strip.png`.
The preview scenes are lit a little brighter than the game (no fog, no grading).

## Round 23: living spawn origins (`build_origins2.py`, 2026-11-04)

`blender --background --factory-startup --python Tools/Blender/build_origins2.py -- [--preview <dir>] [--norender]
[--only Burrow,Water,Lava,Quicksand,Ice,Bog]` (burrow ~8 min, each liquid one ~4 min with renders). Own helpers only
(imports common, env_kit, deep_kit, boss_kit, tower_kit; edits none). Design notes in `DESIGN_NOTES.md` (round 23).
They replace `Burrow_A` and the round-14 `Env/Origin_*` as the nest models. Those stay in place until Unity switches.

### Files (`Exports/Env/Origin2_<Kind>/`)

`Origin2_<Kind>.fbx` (LOD0 + rig + 5 takes), `Origin2_<Kind>_LOD1.fbx` (same rig and takes), Albedo / Normal /
Emission / Mask 1024 + `Low/` 512 (same packed format: Mask R AO, G smoothness, B/A detail families from the report),
`_report.json` (tris, bones, clips, key frames, socket positions open and closed, sizes). Burrow only:
`Skins/<Continent>/` Albedo, Normal, Mask (+ `Low/`) painted from that continent's ground set; Emission is shared.
`.blend` in `ArtSource/Blend/Origin2_<Kind>.blend`.

| Asset | Tris | LOD1 | Bones (deform) | Emission | Size open (ground footprint incl. debris) |
| --- | --- | --- | --- | --- | --- |
| Origin2_Burrow | 3,900 | 1,750 | 62 (53) | baked warm | maw 2.0 across, 3.8 with thrown slabs; 0.72 high (claw) |
| Origin2_Water | 2,400 | 1,080 | 38 (31) | white mask | 1.2; 0.52 high |
| Origin2_Lava | 2,400 | 1,080 | 38 (30) | white mask | chimney 1.1, 2.0 with plates and bombs; 0.53 high |
| Origin2_Quicksand | 2,400 | 1,080 | 34 (27) | white mask | 1.27; 0.5 high (hooks) |
| Origin2_Ice | 2,400 | 1,080 | 39 (32) | white mask | 1.2, 2.25 with thrown floes and chunks; 0.39 high |
| Origin2_Bog | 2,400 | 1,080 | 41 (34) | white mask | 1.2; 0.3 high |

### Unity notes

- **Import:** Scale 1, Bake Axis Conversion, Generic rig, root `Root`, one material (Game/EnvironmentLit-style maps
  but **skinned**: a SkinnedMeshRenderer plus an Animator or `Animation`; the env instancer cannot draw these). Keep
  "Remove constant scale curves" off (`Throat` keys scale in every clip). Set the renderer bounds by hand or use
  *Update When Offscreen* (the bind-pose mesh reaches 1.25 below the pivot).
- **Pivot / placement:** burrow on the ground at the nest cell centre, **scale 1** (not `Burrow_A`'s 0.75); liquid
  ones on the liquid surface (`TerrainLiquidBuilder.SurfaceLevel(kind)`), scale 1. Any yaw for the liquid ones. For
  the burrow, keep yaw 0 if you want its camo to line up with the ground texture (the tops are painted at model
  xy / 3.0; with a yaw the colours still match but the pattern does not continue). Tint the liquid ones' emission
  with `NestColor(element)` as now.
- **Clips (30 fps):** `Closed` (1 frame: the bind pose), `Marked` (90, loop), `Open` (96, one-shot), `Idle` (105, loop),
  `Spawn` (30). Open starts on Marked frame 0. Idle starts on Open's last frame. Spawn starts and ends on Idle frame 0
  (cross-fade 0.1 s or play it on an additive layer over Idle).
- **Closed vs Marked (burrow):** same pose. Closed = material emission 0 (or the renderer off; both are invisible),
  Marked = emission pulsing about 0.35-1.0 x the material's normal strength on a 2-3 s sine (only the hairline cracks
  on the plate tops carry emission in that pose, so the pulse draws a red ring and a five-armed star). Play the
  `Marked` clip too (a 2 mm breath). From Open frame 0, ramp emission to 1.0 by frame 30. **Liquids:** Closed and
  Marked differ in pose (plates or marker organs come up to the surface in Marked); emission stays on.
- **Open key frames:** tremor 0-30 (start a low rumble and the `Dust` socket's ground-crack dust at 0; the plate
  cracks glow from ~10), bulge 30-54 (rumble up, small pebbles), **burst = frame 54 (1.8 s)**: the impact sound, a
  big `Dust` burst, debris trails on `Debris_1..n` (they ride the thrown chunks; for water and quicksand they sit on
  the collar as spray points) and a flash light on `Glow`. Maw fully up at 66, settled at 96. Thrown plates and chunks
  land between 70 and 82 (small dust puffs). **First creature from frame 76** (2.5 s after the trigger) at `Emerge`.
- **Sockets** (non-deforming bones, follow the clips): `Emerge` (open: 0.12 below the pivot at the maw centre, 0.066
  for the liquid ones; creatures rise from here, so the existing EnemyView spawn depth still works), `Glow` (just
  above the gullet: light and heat-haze), `Dust` (pivot), `Debris_1..n`.
- **Burrow skins:** pick `Skins/<Continent>/` (CrystalPlains, AshenReach, NorthernCrown, Mirewood, SouthernCrown,
  TheCore) for Albedo/Normal/Mask by the level's `EnvironmentSet`; Emission and the FBX are shared. If a continent's
  ground set changes, rerun `--only Burrow` (the camo is sampled from `Exports/Env/<set>` at build time).
- **Spawn:** play `Spawn` as each creature leaves the nest (peak frame 6 = 0.2 s: the throat swell; time it with the
  EnemyView burst).

## Round 25: UI art set (`build_ui.py`, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 25). Replaces the code-painted UI textures (IconFactory, DialogueArt,
StarfieldPainter, ModelPreview gradient, PlanetPainter halo, RadarOverlay rings) and the Kenney UI sprites in
`UiSkin.asset`. Nothing is imported yet.

Run: `blender --background --factory-startup --python Tools/Blender/build_ui.py -- [--only icons,kit,dialogue,radar,
starfield,backdrop,halo,mockups] [--preview <dir>]` (about 2 minutes without mockups). Modules: `ui_art.py`
(pixel-unit scene, 2D curve shapes, bevel-shaded materials, render to numpy, compositing, packing, 9-slice) and
`ui_mockups.py` (preview sheets in `ArtSource/Previews/UI/`; labels rendered from the game's Kenney fonts). Uses
`vfx_kit.py` read-only for PNG writing, blur and resampling. `UI_report.json` lists every file with size and the code
site it replaces.

### Files (`ArtSource/Exports/UI/`)

| File | Size | Content | Replaces |
| --- | --- | --- | --- |
| `UI_Icons.png` / `UI_Icons@2x.png` + `.json` | 1024 x 640 / 2048 x 1280 | 40 icons, 8 x 5 grid, 128 (256) px cells | `IconFactory.cs:58-73` |
| `UI_Kit.png` + `.json` | 1024 wide, 2x master | panels, cards, buttons in 4 states (+ Neutral), bars, slider handle, tabs, tooltip, scroll bar, toggle, focus frame, corner ornament, divider | Kenney sprites in `UiSkin.asset` |
| `UI_Dialogue.png` + `.json` | 1024 wide, 2x | Dialogue_Panel, Dialogue_Edge, Dialogue_NameTag, Dialogue_PortraitFrame, Dialogue_Arrow, Radar_Sweep, Radar_Contact | `DialogueArt.cs:55-217`, RadarOverlay arms/dots |
| `UI_Dialogue_Noise/Scanlines/Radial/Shade.png` | 256x32 / 8x8 / 128 / 4x128 | tiling textures (RawImage uvRect) | `DialogueArt.Noise/Scanlines/Radial/Shade` |
| `UI_Radar.png` | 1024 | radar scope, pre-coloured teal (scope radius 471 px) | `RadarOverlay.cs:66-75` |
| `UI_Starfield.png` | 2048 x 1152 | menu sky, alpha = 3 x brightness | `StarfieldPainter` via `UiKit.cs:278-291` |
| `UI_PreviewBackdrop.png` | 1024 | opaque gradient + hex lattice + vignette | `ModelPreview.Gradient` (`ModelPreview.cs:239-249`) |
| `UI_MapHalo.png` | 512 | white radial halo with a faint limb shoulder | `PlanetPainter.PaintHalo` (`WorldMapScreen.cs:230`) |

### Unity import notes

- **Sprites** (`UI_Icons`, `UI_Kit`, `UI_Dialogue`): Texture Type *Sprite (2D and UI)*, Sprite Mode *Multiple*, sRGB
  on, Alpha Is Transparency on, Wrap Clamp, Filter Bilinear, mip maps on for the icons (shown from 24 to 96 px) and
  off for the kit and dialogue, compression BC7 on PC / ASTC 4x4 on Switch. Slice from the json: an editor importer
  can read `unity_rect` (bottom-left based), `border_lbrt` and `pivot` per sprite (`x, y` are top-left based).
- **Scale**: kit and dialogue are 2x masters. Use **Pixels Per Unit 200** (or `Image.pixelsPerUnitMultiplier = 0.5`)
  so the 9-slice borders read at their 1x size. `glow_pad` is the transparent margin around each frame (2x px):
  grow the RectTransform by half of it at 1x, or accept the frame sitting slightly inside the rect. `Dialogue_Edge`
  keeps the old contract: it is drawn `EdgeOverhang` (18 px at 1x) outside the panel, same outline as Dialogue_Panel.
- **9-slice**: Image Type *Sliced*, Fill Center on (off for `FocusFrame`). For a bar fill keep a Sliced image whose
  width follows the value (Image Type Filled would cut the chamfer).
- **Buttons**: switch `Selectable.transition` to *SpriteSwap* with `*_Normal` (target graphic), `*_Focus`
  (highlighted and selected), `*_Pressed`, `*_Disabled`. `*_Neutral` is a light grey version if the ColorBlock tint
  path (`UiKit.ButtonColors`) stays. UiSkin mapping: `_panel` = Panel, `_panelSquare` = PanelSquare, `_button` =
  Button_Normal, `_buttonSquare` = ButtonSquare_Normal, `_barBack` = Bar_Back, `_barBlue/Green/Yellow/Red` = Bar_*,
  `_cardBlue/Green/Yellow/Red` = Card_*; new: Card_Selected, Card_Disabled, Panel_Selected, Panel_Disabled, Tab,
  Tab_Selected, Tooltip(+Pointer), Scroll_Track/Thumb(+Focus), Toggle_Off/On, Slider_Handle(+Focus), FocusFrame,
  CornerOrnament, Divider.
- **Icons**: sprite name = `IconType` member name, so `IconFactory.Get(type)` can become a lookup by
  `type.ToString()` in a small icon-set ScriptableObject; `index` in the json equals the enum value for the first 28
  (`enum: true`). The 12 extras need new enum members with the same names. White icons keep the current tints;
  `BossMarker` and `EnemyOrigin` are pre-coloured (`precoloured: true`, use white). BossMarker points right like
  Arrow, so BossMarkerView's rotation keeps working. Disc stays the map route dot.
- **Tiling textures**: `UI_Dialogue_Noise` and `UI_Dialogue_Scanlines` Wrap Repeat, Filter Point, no mips (scanlines
  are 8 rows = the old 4 px period at 2x, so set the uvRect height to rect height / 8); `Radial` and `Shade` Clamp,
  Bilinear. Texture Type Default, sRGB on.
- **Radar**: one RawImage of `UI_Radar.png` drawn at `Size * 1.087` replaces Back, Ring1-3 and the cross; the sweep is
  `Radar_Sweep` (pivot centre, leading edge up, trail on the left for the clockwise turn), contacts `Radar_Contact`.
- **Starfield / backdrop / halo**: Texture Type Default, Clamp, Bilinear, no mips, sRGB on. The halo is white;
  WorldMapScreen keeps tinting it with `HaloColor`.
- **Fonts** unchanged (KenneyFuture, CC0). `Drones/MoneyPopup.cs:25-27` still uses the built-in LegacyRuntime font;
  it should use `UiKit.Font` like the rest of the UI (no art needed).

## Round 24b: sweep-ready banks for organic pool outlines (`build_bank_sweeps.py`, 2026-11-04)

Remco: "ik hoop ook dat de wateren met ronde hoeken gemaakt kan worden, niet alleen vierkant." Gameplay stays on the
grid; only the visual outline becomes organic. Unity smooths the pool contour and bends bank segments along it.

Run: `blender --background --factory-startup --python Tools/Blender/build_bank_sweeps.py -- [--only
CrystalPlains,AshenReach] [--preview <dir>] [--norender] [--previewonly]` (~2.5 min per style, previews ~2 min).
Reads the continent scripts' bank styles and accents (never edits them); the Crystal Plains style is copied from
`build_env_kit.py`, which runs on import. Preview module: `bank_sweep_preview.py` (contour smoothing, sweep, scenes;
uses `liquid_preview.py` for the liquids). In `build_all.py` after `build_liquids.py`.

### Pieces (per style)

| Style | Folder | Pieces |
| --- | --- | --- |
| Crystal Plains (round 8) | `Env/BankSweep_A/B/C`, `Env/BankAccent` | wet basalt stone row on a crust shelf (4 / 5 / 3 stones, B and C with loose chips) |
| Ashen Reach | `Env/AshenReach/Ash_BankSweep_*`, `Ash_BankAccent` | rope-lava shelf, A/B with pahoehoe rolls, B/C with chips |
| Northern Crown | `Env/NorthernCrown/North_BankSweep_*`, `North_BankAccent` | ice shelf, A/B with pushed-up ice blocks |
| Mirewood | `Env/Mirewood/Mire_BankSweep_*`, `Mire_BankAccent` | peat shelf, A/B with roots over the lip |
| Southern Crown | `Env/SouthernCrown/South_BankSweep_*`, `South_BankAccent` | packed-ice shelf, A/B with tilted plates |
| The Core | `Env/TheCore/Core_BankSweep_*`, `Core_BankAccent` | corruption-pool rim, B/C with chips |

Each segment: 1.0 unit along local X (x = -0.5..0.5), liquid on -Y, dry on +Y, same section as `Bank_Straight`:
the shore line (lip) at y = 0 with its top at z = 0.05; the shelf reaches 0.14-0.21 into the liquid and down to
z = -0.18 (Crystal Plains -0.26), and ~0.18-0.21 back onto the ground; accents rise to z = 0.06-0.1 (extents per
variant in the reports, `sweep`). The section's noise fades out over the last 6 % at both ends, so the ends are the plain
section and **any variant follows any other** without a step. Game mesh 900 tris before slicing; it is then cut at
every 1/16 unit along X (17-19 vertex rows, 2,300-2,800 tris after; `rows_along_x`, `game_tris_after_bisect`), so
it bends without kinks. A pool of ~20 segments is ~50k tris: combine per pool, and for Low decimate the combined
mesh or drop every other cut. UV0 = the
baked 512 atlas (unique per variant; Albedo/Normal/Emission/Mask like the other kit pieces), UV1 `UVSweep` =
(x + 0.5, y + 0.5) for a tiling detail map along the run. `BankAccent`: 3 shore rocks/chips, pivot on the ground,
scatter freely (any yaw, scale 0.8-1.2) on the bank to hide joints and repeats.

### Unity: how to sweep

1. **Contour** (per pool, at level build): take the cell-union outline as a CCW loop of corner points (liquid on the
   left), resample every 0.02, smooth it with a circular Gaussian (sigma 0.55 world units along the loop; FFT or a
   repeated [1 2 1] kernel), resample again, then offset along the normal by a small periodic wobble (amplitude
   0.04, a few low harmonics seeded per pool). The smoothed line stays within ~0.25 of the cell edges: convex corners
   move into the liquid cells, concave corners ~0.2 into the dry cell (no tower stands on a liquid cell's neighbour
   edge closer than that, so pads and banks do not collide; check against `BuildPadTile` footprints if needed).
2. **Segments**: n = round(perimeter / 1.0), each stretched to perimeter / n (0.9-1.1). Pick variants A/B/C at
   random, never the same twice in a row.
3. **Bend** (CPU, once): for each vertex (x, y, z) of the segment i, u = (i + x + 0.5) * L along the loop;
   position = P(u) + outward(u) * y + up * z; rotate normals and tangents by the frame at u. (Or Unity Splines'
   `SplineExtrude`-style deformer; the math is the same.) The bent mesh of a pool can be one combined mesh.
4. **Bend limit**: radius >= 0.5 at the shore line (the preview contour reaches 0.38 at the two tightest spots,
   still clean). Round a liquid bulge and the dry side (+0.2) stretches by up to 40 % while the liquid side
   compresses ~40 %; round a dry peninsula and it is the other way round. Below ~0.35 the stones of the inner side
   start to overlap: smooth more (sigma up) rather than allowing it.
5. **Liquid surface and shore effects**: the liquid sheet follows the same smoothed polygon (triangulated), or stays
   a cell-rect sheet clipped by the terrain. The round-24 shader's `shore_dist` must be measured from the shelf
   edge: the distance to the smoothed contour minus ~0.06 (the previews use 0.06). With the depth-texture recipe this is
   automatic; for the Low distance texture bake the distance to the smoothed polygon (not to the cells), which also
   makes the foam ring and the frost/scum rings follow the round outline (no extra texture needed).
6. **Terrain**: dig the basin from the same distance field (as `liquid_preview.terrain`): ground at 0 under the
   shelf, then at once a wall to -0.3 (below every liquid level, or a dry band of basin shows between lip and
   liquid), then the floor deepens to -0.75.
7. **Accents**: on ~60 % of the segment joints (offset 0-0.08 to the dry side) plus 3 loose ones per pool. The
   baked maps of two variants meet at a joint, which shows as a faint cut on the smooth styles (Mirewood peat, Core);
   the accents hide most of them.

### Previews

`bank_<Style>_compare.png` (square cell kit | organic sweep), `bank_<Style>_organic.png`, `bank_<Style>_close.png`,
`bank_all.png`, for Crystal Plains (water), Ashen Reach (lava), Mirewood (bog).

## Round 25b: gameplay markers, bars, projectiles, debris and spawn gates (2026-11-04)

```
blender --background --factory-startup --python Tools/Blender/build_spawngate.py -- [--preview <dir>] [--norender] [--only Ground|Air]
blender --background --factory-startup --python Tools/Blender/build_projectiles.py -- [--preview <dir>] [--norender] [--nobake]
blender --background --factory-startup --python Tools/Blender/build_markers.py -- [--preview <dir>] [--norender] [--noscene]
```

Spawn gates ~5 min (two Cycles bakes), projectiles ~2.5 min (three small tower-kit bakes; `--nobake` reuses the maps,
~20 s), markers ~15 s plus ~1 min for the review scene. Run them in this order: the markers' review scene
(`marker_scene.py`) appends `Blend/SpawnGate.blend` and `Blend/Projectiles.blend`. Own helper module `marker_kit.py`
(numpy texture painting, tiny meshes with explicit UVs, preview materials); shared kits imported read-only. Design notes
in `DESIGN_NOTES.md` (round 25b). Every FBX checked with `inspect_fbx.py`.

Everything here replaces a code-made primitive or a texture generated at runtime. Code keeps placing, scaling,
tinting and animating them.

### Conventions for this round

- **Marker textures are linear data (sRGB off) channel packs**, not colour pictures: R/G/B are masks, A coverage. The
  Unity shader multiplies them by a tint (the code's existing colours: `ValidColor`, `InvalidColor`, the hero trim,
  `theme.SpawnColor`, `DroneDefinition.Color`...), so one texture serves every theme and state. Each report lists
  the channel meaning per texture. Every texture has a half-size copy in `Low/`.
- **Blend modes:** "add" = additive (`Blend One One`, ZWrite off), "alpha" = straight alpha
  (`Blend SrcAlpha OneMinusSrcAlpha`). Both Cull Off, no shadows, render queue Transparent. Ground decals sit
  0.01-0.05 above the surface (as the code does now) or use a small depth offset.
- **One shader can serve them all:** `Game/MarkerPack` with `_MainTex`, `_Tint` (HDR), `_ChannelGain` (vector: gain per
  R, G, B), `_Scroll` (UV speed), optional second sample with its own scroll for B (beacon bands, energy-field
  shimmer), `_Fill` (clip on UV2.x) and a blend-mode keyword. Colour = `_Tint * dot(tex.rgb, _ChannelGain)`.
- **Axes:** flat decals lie in Unity XZ (normal +Y); upright quads face Unity -Z (towards the camera); projectiles,
  cues and beams point along Unity +Z (forward). Static meshes have no rig; pivots are listed per mesh.
- Bars, markers < 200 triangles, projectiles < 400, spawn gates <= 4,000 (budgets held).
- The spawn gates are the route-start portals (`EnemyGate`), not the element nests / ambush burrows of round 23
  (`Origin2_*`, `ElementNest`): different job, different silhouette (horns and a turning claw ring vs plated lids).

### Assets

| Asset (folder) | FBX meshes (tris) | Textures (size) | Replaces |
| --- | --- | --- | --- |
| `SpawnGate_Ground` | rigged mound (3,800), 17 bones (13 deform, 3 sockets) | baked Albedo/Normal/Emission/Mask 512 (Low 256) | `StructureModelFactory.EnemyGate` + `GateRing` (`LevelVisualBuilder.cs:194`) |
| `SpawnGate_Air` | rigged floating pod (3,600), 19 bones (15 deform, 3 sockets) | baked 512 (Low 256) | the same, `path.Flying` gates (`AirGateHeight` 1.6, `AirGateScale` 0.8) |
| `UI_HealthBar` | `HealthBar_Plate`, `_Fill`, `_Armour` (2 each) | `UI_HealthBar.png` atlas 256x64 | bar cubes in `EnemyView.cs:1460-1469`, `TowerView.cs:383`, `HeroView.cs:332` |
| `Decal_GlowPool` | `Decal_Quad.fbx`: `Quad_Flat`, `Quad_Upright` (2 each) | `Decal_GlowPool` 128, `Glow_Blade` 128, `Field_Energy` 128 | `GlowDecal.Radial/Blade/Scanlines` (`Core/GlowDecal.cs:101-174`), `GateView.cs:144-148` field |
| `Decal_BlobShadow` | uses `Quad_Flat` | `Decal_BlobShadow` 128 | flyer shadow cylinder `EnemyView.cs:1473` |
| `Marker_PlacementCursor` | `Marker_PlacementCursor` (2, 1.5 cells), `Marker_FreeCell` (2) | `Marker_PlacementCursor` 256, `Marker_FreeCell` 128 | cursor cube `PlacementController.cs:88` (+ the 2026-11-04 "free cells only near the cursor" wish) |
| `Marker_RangeRing` | `RangeRing_Band` (192, 2 UV sets), `RangeRing_Fill` (48) | `Marker_RangeRing` 256x64, `Marker_RangeFill` 128 | LineRenderer ring `RangeIndicator.cs:21` |
| `Marker_HeroSpot` | `Marker_HeroSpot` (2) | `Marker_HeroSpot` 256x128 | spot cube `HeroController.cs:113` |
| `Marker_HeroStand` | `HeroStand_Ring` (96), `HeroStand_Beacon` (6), `HeroStand_Emblem` (136) | `Marker_HeroRing` 256x32, `Marker_Beacon` 64x256, `Marker_Emblem` 64 | stand torus, beacon cylinder, emblem cube `HeroView.cs:336,430-439` |
| `Marker_SlowField` | `Marker_SlowField` (40) | `Marker_SlowField` 256 | reach cylinder `PathObjectView.cs:123` |
| `Marker_RoadCue` | `RoadCue_Strip` (2), `FlightCue_Strip` (2) | `Cue_Trail` 256x64, `Cue_Dash` 256x32, `Cue_Pulse` 128 | cube chevrons `LevelVisualBuilder.cs:163-176`, route dashes + arrowheads `:513-533` |
| `Proj_Bolt` | `Proj_Bolt` (112), `Proj_Bolt_Glow` (4) | `Proj_Glow` 64x128 (+ `VFX/Bolt_Core`) | core sphere + bolt capsule `ProjectileView.cs:76-79` |
| `Proj_RailSlug` | `Proj_RailSlug` (48), `Proj_RailSlug_Glow` (4) | `Proj_Bolt/Proj_Glow` (B rings) | rail capsule (`SlugScale`) |
| `Proj_Missile` | `Proj_Missile` (338) + `Socket_Exhaust`, `Socket_Nose` | baked 256 (Low 128) | `BuildMissile` (`ProjectileView.cs:96-110`) |
| `Proj_EnemyBolt` | `Proj_EnemyBolt` (89), `Proj_EnemyBolt_Glow` (4) | `Proj_EnemyBolt` 128 | core + halo spheres `EnemyBoltView.cs:37-43` |
| `Proj_PulseRing` | `Proj_PulseRing` (384) | `Proj_PulseRing` 256x64 | torus `BossAbilityView.BuildRing` (`:278-287`) |
| `Beam_Tool` | `Beam_ToolCone` (48) | `Beam_Tool` 64x128 | beam cube `DroneView.cs:81` |
| `Net_Strand` | `Net_Strand` (4) | `Net_Strand` 128x32 | strand cubes `PathObjectArtRig.cs:118` |
| `Recon_Scout` | `Recon_Scout` (332) + `Socket_Trail`, `Socket_Light` | baked 256 (Low 128), `Trail_Scout` 256x64 | scout sphere + default trail `ReconView.cs:261-264` |
| `Debris_Chunks` | 10 chunks, 24-236 each (1,032 total) | baked atlas 512 (Low 256) | debris cubes `GateView.cs:155,196`, `BaseView.cs:102` |
| `Particles_Atlas` | - | `Particles_Atlas` 512x256 (4 x 2) | default particle material in `LevelVisualBuilder.cs:300-340`, `AmbientParticlesBuilder.cs:31`, `PropScatterBuilder.cs:414`, `EnemyView.cs:471,652-667` |

Not re-made (already covered by `Exports/VFX`, see `VFX_report.json`): hero blaster bolt (`Bolt_Core` on a stretched
billboard), hero ability beam and laser (`Beam_Core` + `Beam_Noise`), rocket exhaust (`Rocket_Exhaust`,
`Smoke_Trail`), sparks/impacts (`Spark_*`, `Energy_Impact`), flying debris sprites (`Debris_Shards`), recon trail
alternative (`Trail_Ribbon`).

### Unity notes per asset

- **SpawnGate_Ground / _Air** (skinned, Generic rig, root `Root`, Bake Axis Conversion, one material in the creature
  format: Albedo sRGB, Normal, Emission sRGB, Mask R AO / G smoothness / B membrane detail / A chitin detail, tiling in
  the report). Pivot: ground = centre of the mound on the ground; the model is 1.89 across (roots included) and 1.29 high
  (the tallest horn), so **`GateScale` 1.0** keeps the old 1.9 footprint (1.2 makes it 2.3). Air = centre of the
  pod (1.34 across, 1.86 high with the tendrils hanging 1.25 below the pivot), placed at `AirGateHeight` 1.6 with
  `GateScale * AirGateScale` (0.8 at `GateScale` 1.0); renderer bounds: *Update When Offscreen* or set by hand. Bones: `Body`
  (whole hide, breathes), `Ring` (the claw ring; rigid), `Lip_0..5` (ground) / `Lip_0..4` (air), `Spine_0..4` (ground
  horns), `Tendril_n_0/1` (air, n 0..3). Sockets (non-deforming bones): `Socket_Emerge` (where a creature appears; +Z
  up; ground 0.12, air 0.3 above the pivot), `Socket_Spores` (move `AddSpores` here and give it `Particles_Atlas`
  frame Spore), `Socket_Light` (the gate point light). Clips (30 fps): `Idle` 360 f loop (breathing, lip twitch, horn
  / tendril sway, one full ring turn = 30 deg/s, the same as `GateRingSpin`), `Spin` 360 f loop (only the ring: for an
  additive layer or when Idle is not wanted), `Spawn` 40 f (lips peel open 55 deg at frame ~14, throat swells,
  ring lurches 90 deg and lifts 6 cm; play it per wave or per creature, cross-fade 0.1 s back to Idle). Drop the code
  `Spinner` on the ring when the clip runs. Emission is baked ember orange with sickly green eye/pustule glints:
  `_EmissionColor` white x 1.5-2.4 keeps the look; multiplying by `theme.SpawnColor` still works for themed tints.
  Keep `EmissionPulse` on the material for the breathing glow. Both share the same palette, so ground and air read
  as one species.
- **UI_HealthBar** (unlit, alpha, ZTest LEqual or Always on an overlay layer). The three quads are upright, face Unity
  -Z and share one pivot = the centre of the health line (`EnemyView` `_barY`): place the root where the cube bar was
  and billboard it to the camera (or keep the camera's fixed rotation). Sizes are the 2026-11-01 night values: fill
  0.85 x 0.085, armour 0.85 x 0.06 at +0.136, plate 0.935 x 0.272 at +0.068 and 0.004 behind. Scale the root's X for a
  wider bar (`_barWidth / 0.85`); the tower (1.0 x 0.1) and hero (0.8 x 0.04) bars use the same meshes with their
  own root scale. **Fill without scaling:** UV2.x runs 0..1 along each quad; the shader discards `UV2.x > _Fill` so the
  bar is cut from the right and the rounded ends stay round. Colours: plate `PlateColor` (0.02, 0.02, 0.04) at
  alpha 0.5 x A (+ rim R x 0.25), fill `BarColor` (0.4, 1, 0.4) x G + R x 0.35 sheen, armour `ArmorBarColor`
  (0.7, 0.85, 1) with eight segments; the fill's B ticks (10 %) darken by 0.25. Emission 0.7 as now. Show only when
  damaged (unchanged rule).
- **Decal_GlowPool**: `Quad_Flat` replaces `GlowDecal.Quad()` for `PlacePool` (scale = diameter, additive; R is the old
  radial curve, G a wider halo, B a thin ring for pulsing pools). `Glow_Blade` keeps the blade UV convention (U core 0
  to rim 1, V base 0 to tip 1). `Field_Energy` replaces `ScanlineMaterial`: tiles in V (keep scrolling V), R
  scanlines as before, G hex lattice, B shimmer; `Quad_Upright` for the sheet, `Quad_Flat` for the floor strip.
- **Decal_BlobShadow**: `Quad_Flat`, alpha blend black, alpha = A x 0.6; shrink with altitude by lerping to R.
  Replaces the black cylinder (`ShadowSize` 0.6 -> scale 0.6-0.9).
- **Marker_PlacementCursor**: the quad is 1.5 cells (the halo spills over the neighbours); scale by `CellSize`; tint
  `ValidColor` / `InvalidColor` / `TowerColor` as now, additive, emission 1.6. For the 2026-11-04 rule "free cells only
  while placing, soft near the cursor": draw `Marker_FreeCell` on free cells within ~2.5 cells of the cursor with
  alpha = 1 - distance / 2.5 (one instanced draw).
- **Marker_RangeRing**: `RangeRing_Band` (radius 0.93-1.0) + `RangeRing_Fill` (disc 0.93), scale XZ by the range.
  Use the same gain for the fill's R and the band's G (both 0.22 at the shared edge, so they meet without a seam);
  keep the rim's emission below the bloom threshold (~1): an HDR rim makes bloom fill the whole disc.
  The band's U tiles 24 times round the ring (B dash per tile: scroll U ~0.02/s for a slowly turning dash). For a
  constant line width at any range, move vertices in the shader: `pos.xz = normalize(pos.xz) * (_Radius -
  (1 - uv2.y) * _Width)` with the mesh at scale 1.
- **Marker_HeroSpot**: unit quad, X across the road, Z along it (`LookRotation(along)` as now); scale X = cells x
  (width - `SpotMargin`), Z = cell x (1 - `SpotMargin`). Hero trim tint, alpha `SpotAlpha`; pulse B slowly.
- **Marker_HeroStand**: `HeroStand_Ring` at the feet (radius 0.47-0.53; U = 3 arcs), `HeroStand_Beacon` (three crossed
  quads, bottom pivot, 1.3 high, place at `BeaconBottom` 1.7; second sample of B scrolled upwards), `HeroStand_Emblem`
  (faceted crest with a halo, centre pivot, spin about Y and bob as now; R facet ramp, G rim; opaque emissive or
  alpha with A = 1). `DownColor` swaps the tint as now.
- **Marker_SlowField**: disc radius 1 (40 tris), scale XZ by `definition.Radius`, 0.05 up, additive in the field
  colour. Ripples: a second sample of G with `uv' = 0.5 + (uv - 0.5) * frac(-t * 0.3)` runs them outwards.
- **Marker_RoadCue** (no chevrons, no arrowheads): `RoadCue_Strip` 1 x 0.42 along Unity +Z, pivot centre, U along
  the travel direction; place one per road cell at `Road.CentreOf(cell)` with `LookRotation(forward)` (the old chevron
  loop), additive, tint `ArrowColor` or the theme spawn colour at ~0.5, `uv.x -= t * 0.35`. The strip's U spans a
  third of a tile (one light streak every three cells), so offset U by `cellIndex / 3` (property block) or use a
  world-space U along the road. Better: one LineRenderer
  ribbon along `level.Road.Route(i)` with `textureMode = Tile` and the same material (no seams, bends follow the road).
  `FlightCue_Strip` (0.16 wide) + `Cue_Dash` for flight routes at `RouteHeight`, scale Z by the leg length and tile U
  by the length. `Cue_Pulse` on `Quad_Flat` = an expanding ring (scale 0.4 to 3, fade out) for route starts and for
  `ReconView` pulses (instead of its torus).
- **Proj_Bolt / Proj_RailSlug / Proj_EnemyBolt**: additive emissive meshes pointing along Unity +Z, pivot at the
  centre; HDR tint x 3-4 for bloom. `Proj_Glow`: colour = white x R + tint x G (+ tint x B rings on the slug). Each
  has a `_Glow` shell (two crossed quads, U along, head at +U) for `VFX/Bolt_Core.png` (additive, tint on G); its
  pivot sits so the bright head lies over the nose. Keep the existing TrailRenderer (with `VFX/Trail_Ribbon`). The
  enemy bolt texture uses spherical UVs: colour = tint x G + hot x R, darkened by B spots.
- **Proj_Missile**: lit (tower material format, 256 maps); `Socket_Exhaust` for the flame particles / `Rocket_Exhaust`
  and the smoke trail, `Socket_Nose` for the seeker glint. 0.33 long, nose to +Z.
- **Proj_PulseRing**: radius 0.9-1.07, crest at 1.0, 4 cm high; scale XZ by the pulse radius only (keep Y 1). U tiles 8
  per turn. Tint the boss colour, fade with age (alpha or emission).
- **Beam_Tool**: open cone, tip at the drone (pivot), 1 long along +Z, mouth radius 0.7: rotate with
  `LookRotation(target - drone)`, scale Z = distance, XY = spot size / 0.7. Additive, tint `DroneDefinition.Color`,
  scroll V towards the target (G scan rings). Keep the glow pool under it.
- **Net_Strand**: two crossed strips, 1 long along X, centre pivot (as the cube: `FromToRotation(Vector3.right, b -
  a)`, scale X = length). U tiles 4 per metre in the mesh; set the material tiling to the strand length (or world-space
  U) so the braid does not stretch. Additive.
- **Recon_Scout**: lit probe (256 maps), ~0.3 across, lens to +Z; replace the 0.9 sphere and keep a point light at
  `Socket_Light`; the TrailRenderer starts at `Socket_Trail` with `Trail_Scout` (Stretch mode, additive, the
  ground/air colour), width 0.7 as now.
- **Debris_Chunks**: ten static meshes in one atlas (tower material format): `Debris_PlateWhite`, `_PlateBlue`,
  `_HexPanel` (glowing inset), `_HexHalf` (burnt, ember edge), `_Strut`, `_Pipe`, `_Bracket`, `_Hazard`, `_RockA`,
  `_RockB`. Centre pivots, 0.1-0.2 across at scale 1: replace the random cube sizes by a random uniform scale
  0.6-1.6 and a random chunk. "Hot" chunks = the two hex pieces (they replace `EmberColor` cubes); the rock bits are
  for ground hits and the base. Pool them as now.
- **Particles_Atlas**: Texture Sheet Animation 4 x 2, *Random Between Two Constants* or a fixed frame: 0 Spore (gate
  and nest spores, `AddSpores`), 1 Mote (ambient motes, chrono/light motes), 2 Ember (embers on fire creatures and
  lava), 3 Drip (blood/acid drips), 4 IchorSplat (impact and death splats), 5 GooDecal (slime trails as flat
  particles), 6 FrostMote (frost over chilled creatures), 7 ChronoMote (the Chrono field). R core / highlight, G body,
  B detail, A coverage. Use additive for 1, 2, 6, 7 and alpha for 3, 4, 5 (two materials on the same texture).

## Round 26: menu diorama, underground cave and eyes, Earth, cutscene FX, skies (2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 26). These are the last code-made visuals from the visual audit (batch
B3). Nothing is imported yet. New helper modules: `stage_kit.py` (Unity/Blender frames, 2D noise, PNG I/O,
placing exports) and `planet_kit.py` (equirect noise and PNG helpers, a read-only copy of the S-69 toolbox).

**Frame rule (important for every placed layout):** the FBX export (forward -Z, up Y) plus Unity's x flip on
import turn Blender (x, y, z) into Unity (-x, z, -y). A Unity world point (x, y, z) is Blender (-x, -z, y), and a
Unity yaw of a degrees is a Blender Z rotation of -a. `common.G` keeps x unflipped, which only mirrors symmetric
creatures; `stage_kit.u2b` uses the true mapping, so the layout json below can be applied in Unity as written.

### Menu diorama (`build_menu_diorama.py`)

Run: `blender --background --factory-startup --python Tools/Blender/build_menu_diorama.py -- [--preview <dir>]
[--norender] [--skipmouth] [--quick]` (about 9 minutes; `--quick` halves every resolution).

| File | Content | Replaces |
| --- | --- | --- |
| `Menu_Island/Menu_Island.fbx` | 13,800 tris (top about 8,400, underside about 5,400), pivot at the world origin, built in Unity world space. Materials `Menu_Island_Top` and `Menu_Island_Cliff` | `MenuTerrain.cs:196-236` (BuildMesh / BuildMaterial) |
| `Menu_Island_Top_{Albedo,Normal,Emission,Mask}.png` | 2048 (Low 1024). UV u = (x - 3.6 + 13) / 26, v = (z - 2.2 + 13) / 26. Mask R AO (props baked in), G smoothness, B height, A Crysium dust | painted albedo/emission of `MenuTerrain.Paint` |
| `Menu_Island_Cliff_{...}.png` | 4096 x 512 (Low 2048 x 256). u = angle round (3.6, 2.2), v = 1 - depth below the rim / 11. Mask R AO, G smoothness, B strata band, A vein | new (the old island had no underside) |
| `Menu_Island_Height.png` | 512, 16-bit ground height ray cast onto the game mesh; decode with `min_y`/`max_y` from the layout json | `MenuTerrain.Height` for `OnGround`/`Normal` |
| `Menu_Island_layout.json` | island, camera, height map, 5 pool outlines (72 points each, with surface level), lights, 105 placed exports (asset, folder, group, position, yaw, scale, sway, glow_pool, flip) | `MenuDiorama.BuildGrowth` / `Ring` / `PlaceProp`, `MenuTerrain.PoolCells` |
| `Menu_NestMouth/Menu_NestMouth.fbx` + maps 1024 (Low 512) | 2,379 tris, the hooded burrow over the porch notch; pivot on the porch floor at the opening, opening faces local +Z, placed with yaw 180 | `MenuDiorama.cs:220-228` (MouthHole quad + MouthRim torus) |

Unity notes:
- Import `Menu_Island` with Bake Axis Conversion, place at (0, 0, 0). Top and Cliff use the env Lit setup
  (Albedo sRGB, Normal, Emission sRGB with the shared pulse, Mask linear). The mesh replaces `MenuTerrain.Build`.
- `MenuTerrain.Height(x, z)` becomes a bilinear lookup in `Menu_Island_Height.png` (same UV as the top), so the
  creatures stand on what is drawn. `PoolCells` becomes a point-in-polygon test against `pools[].outline_xz`; the
  pool centres, radii and bank arcs are unchanged, so `BuildCreatures` and `PoolDweller` keep working.
- Pools: keep `TerrainLiquidBuilder.BuildPool` with the round-24 liquid textures (`Exports/Liquids/<Kind>`);
  the basins (0.8 deep) sit under the surface levels of `TerrainLiquidBuilder`.
- Props: one prefab per `asset` (the exports already imported for the levels); `sway` true adds `WindSway`,
  `glow_pool` true keeps `GlowDecal.PlacePool`. Group `outpost` is new (two pads with Gun T1 and Laser T1, the
  survey beacon, a crystal) on the rise at the back; group `underside` hangs crystals under the island. Both are
  optional. The routes of `BuildCreatures` are kept clear (0.45 m plus each footprint).
- Lights: `lights[]` lists the mouth light (now inside the hood), pond and crystal lights as before, plus two new
  ones (lava glow, outpost lamp).
- Starfield and spores: `UI/UI_Starfield.png` and the `Particles_Atlas` Spore / Mote cells (round 25) replace the
  default particle material of `BuildStarfield` / `BuildMotes`.

### Underground cave and Burrower eyes (`build_underground.py`)

Run: `blender --background --factory-startup --python Tools/Blender/build_underground.py -- [--preview <dir>]
[--part kit|eyes|scene|all] [--only Cave_Wall_A,...] [--norender]` (kit about 12 minutes, eyes seconds).

| Asset (`Exports/Underground/<Asset>/`) | Tris | Maps | Use (replaces in `UI/Cutscene/UndergroundSet.cs`) |
| --- | --- | --- | --- |
| `Cave_Wall_A` | 1,900 | 1024 | wall section about 5 m long, 6 m tall, cavern side local +Z (`BuildCave` boulders, :125-132) |
| `Cave_Wall_B` | 1,950 | 1024 | same with a deep crevice for eye pairs |
| `Cave_Ceiling` | 1,950 | 1024 | slab with rock drips and crystal points; pivot at the top (`BuildCave` spikes, :140-145) |
| `Cave_Stalactites` | 998 | 512 | hanging crystal cluster; pivot at the top |
| `Cave_Floor_A`, `Cave_Floor_B` | 1,500 / 1,700 | 1024 | rubble floor; B with a glowing cyan fissure (`BuildCave` rocks, :134-138) |
| `Cave_Boulder` | 898 | 512 | loose boulder |
| `Cave_CrystalCluster` | 1,500 | 512 | the trembling light sources (`BuildCrystals`, :149-175; keep the point lights) |
| `Cave_Vein` | 300 | 512 | flat glowing vein to lay over rock |
| `Eye_Burrower_A`, `_B`, `_C` | about 420 | 512 | one eye each; replaces `EyeField.cs:74,122` |

Cave pieces: four maps like every env piece (Mask R AO, G smoothness, B rock / A crystal detail), Low/ at half
size. Eyes: material `<name>_Eye` (Albedo, Normal, Emission, Mask; emission on the iris only, meant for
`EyeField.Emission` of about 2.5 to 5 HDR with bloom) and `<name>_Cornea` (clear, smoothness 0.97, alpha about
0.12; can be dropped on the Low profile). Unit: the slit is 1 m wide, so scale = 2 x `EyeWidth` x size (0.15 per
unit size); pivot at the eyeball centre, the eye looks along local +Z. A pair is two instances, the second with
scale x -1, `EyeGap` apart. Variants: A amber with a vertical slit, B ember with a horizontal bar and brow spines,
C sulphur with three pupils.
- Shape keys `Blink`, `LookLeft`, `LookRight`, `LookUp`, `LookDown` (the eye's own left). Fine for the two lead
  pairs as SkinnedMeshRenderers.
- For the hundreds in the wave, use MeshRenderers with GPU instancing and a vertex shader: vertex colour
  `EyeRig` R = upper lid, G = lower lid, B = eyeball and cornea. Turn R vertices about the eye's local right axis
  through the pivot so the upper lid edge moves down by `rig.Blink.upper_lid_deg` x blink, G vertices so the lower
  edge moves up by `lower_lid_deg` x blink, B vertices by `Look.deg` for a look (values in each report). The
  existing open / blink timing of `EyeField.Pose` drives the per-instance blink value.
- The scene layout in `build_underground.py` (`build_scene`) follows UndergroundSet: walls at radius 6.1 on both
  sides, ceiling at 5.6, floor at -1.6, five clusters, 120 eye pairs.

### Earth (`build_planet_earth.py`)

The exhausted Earth of 2053 as a texture set in the Planet_S69 conventions. Replaces the runtime painter
`UI/EarthPainter.cs:54` (`Paint(width)`: `Maps.Albedo/Normal/Gloss/Emission` + `Smog`), which
`UI/Cutscene/EarthSet.cs:37` calls. Procedural numpy (helpers in `planet_kit.py`). The continents are hand-placed,
domain-warped lobes in roughly the real arrangement; nothing is traced from imagery. Run: `blender --background
--factory-startup --python Tools/Blender/build_planet_earth.py -- [--res 4096] [--out <dir>] [--renders <dir>]
[--norender]` (about 8 minutes).

| File (`Exports/Planet_Earth/`, + `Low/` 2048) | Content | Unity |
| --- | --- | --- |
| `Planet_Earth_Albedo.png` 4096 x 2048 | sRGB, unlit, no smog: grey-brown land, dead forest, dust belts, salt flats, terraced pits with rust/toxic tailings ponds, murky seas with silt plumes and algae, shrunken dirty ice | `Maps.Albedo` (and `CutsceneCast.EarthSurface`). sRGB, Wrap U Repeat / V Clamp, aniso 8 |
| `Planet_Earth_Normal.png` | land relief only, sea flat, R = east, G = north (OpenGL) | `Maps.Normal`, Texture Type Normal map |
| `Planet_Earth_Height.png` 16-bit | 0.5 = sea level, terraced pits | optional (parallax, sea-depth tint) |
| `Planet_Earth_Mask.png` | linear: R land, G wetness (sea 1, silted 0.85, ice 0.7, ponds 0.6, salt 0.37, land 0.12), B glow, A ice | `Maps.Gloss`: EarthPainter keeps smoothness in Gloss.a; use smoothness = 0.05 + 0.22 x Mask.G (sea 0.27, land 0.08, ice 0.20), baked into an RGBA gloss texture by an editor step or remapped in the shader. sRGB off |
| `Planet_Earth_Emission.png` | sRGB: dim amber cities (about two thirds dark), white mine work lights, orange gas flares | `Maps.Emission`, HDR multiplier on the night side |
| `Planet_Earth_Clouds.png` | RGBA smog: RGB = smog tint (brown, yellow dust over deserts, soot over industry), A = cover (max 0.75) | `Smog`. The shader must read RGB as the deck colour (not white like Planet_S69_Clouds) and A as cover; keep `SmogAlpha` |

UV: u = (lon + 180) / 360 with east to the right, top row = north, identical to `PlanetCoordinates.ToUv` and
GlobePainter. `Planet_Earth_report.json` lists the files, the code mapping and the scar positions.

### Skies (`build_skies.py`)

Equirectangular reflection and ambient skies per theme. They replace the 32 px cubemap that `Levels/SkyProbe.cs:40`
paints from SkyColor / LightColor x LightIntensity / FogColor x 0.25. Same structure (dark zenith, a bright band in
the light colour at the horizon, a dark ground hemisphere), plus faint nebula in the palette, aurora over Northern
and Southern Crown, smoke (Ashen Reach), mist (Mirewood), a low glow from below and sparse stars. The Core and
Underground are caverns: a rough ceiling with crystal glints. Run: `blender --background --factory-startup --python
Tools/Blender/build_skies.py -- [--out <dir>] [--renders <dir>] [--only CrystalPlains,Earth]` (about 3 minutes).

| File (`Exports/Skies/`, + `Low/` 512 x 256) | Theme |
| --- | --- |
| `Sky_CrystalPlains.png` 2048 x 1024 | Theme_C1_01..06 (indigo, lavender horizon, pink/blue nebula) |
| `Sky_AshenReach.png` | smoke-brown night, ember horizon |
| `Sky_NorthernCrown.png` | blue-black, green aurora, amber heat at the horizon |
| `Sky_Mirewood.png` | foggy teal, violet nebula |
| `Sky_SouthernCrown.png` | pale cold sky, magenta aurora and charged veins |
| `Sky_TheCore.png` | cavern: indigo ceiling, gold-white seal light from below, violet nebula |
| `Sky_Earth.png` | Theme_Earth: space, warm sun, smog-brown Earth filling the lower sky |
| `Sky_Underground.png` | Theme_Underground: near-black cave, cyan and ember crystal glints |

Unity:
- Import each PNG as Texture Shape **Cube**, Mapping **Latitude-Longitude Layout (Cylindrical)**, sRGB on, mip maps
  on (for reflection roughness), BC1 is fine, and the Low (512) set on Switch.
- SkyProbe.Apply becomes `RenderSettings.customReflectionTexture = theme.SkyCubemap` with `reflectionIntensity =
  exposure` from `Skies_report.json` (1.3 to 2.0). The PNGs store linear values through a soft shoulder above 0.6,
  so exposure restores the glow range.
- For ambient, keep Flat ambient or switch to Trilight with `ambient_trilight_srgb` (sky / equator / ground).
- Suggested data: a `Cubemap` field on VisualTheme, falling back to the painted cubemap when it is empty. The six C1
  themes share `Sky_CrystalPlains` (their theme sky colours are identical). `brightest_direction` in the report
  gives the sun glow's azimuth/elevation; rotate the probe if it should line up with the key light.

### Cutscene FX (`build_cutscene_fx.py`)

Run: `blender --background --factory-startup --python Tools/Blender/build_cutscene_fx.py -- [--preview <dir>]`
(numpy only, about 30 s; uses `vfx_kit.py` read-only). Files in `ArtSource/Exports/Cutscene_FX/` (+ `Low/` half
size), listed with use and code site in `Cutscene_FX_report.json`. All sprites are greyscale RGBA with RGB = A: the
code keeps tinting them with its existing HDR colours (additive One One, like the old runtime textures).

| File | Size | Use | Replaces |
| --- | --- | --- | --- |
| `FX_GlowSoft` | 256 | every round glow quad (`SpaceKit.Glow`) | `SpaceKit.SoftDot` (`SpaceKit.cs:94-103, 115-132`) |
| `FX_BeamSoft` | 256x64 | LineRenderer texture, U along (tiles), V across | `SpaceKit.SoftBand` (`SpaceKit.cs:105-113`), SurveySet scan/echo/meridians (`SurveySet.cs:59-68`) |
| `FX_SunCore` | 512 | sun disc (limb darkening, corona) | SoftDot on SunCore (`SpaceBackdrop.cs:35`) |
| `FX_SunHalo` | 512 | wide halo with faint rays and limb shoulder | SoftDot on SunHalo (`SpaceBackdrop.cs:34`) |
| `FX_SunStreak` | 512x128 | anamorphic flare | SoftDot on SunStreak (`SpaceBackdrop.cs:36`) |
| `FX_Glint` | 256 | crystal glints, 4 long + 4 short spikes | SoftDot on Glint quads (`SurveySet.cs:70-77`) |
| `FX_EngineGlow` | 256 | nozzle glow: hot core, lit ring | SoftDot on Exhaust quads (`OrbitSet.cs:189`) |
| `FX_ExhaustPlume` | 128x512 | optional directional plume (nozzle at the top edge) | new, for `OrbitSet.cs:189` if a plume quad is added |
| `FX_RunningLight` | 128 | nav-light dot with a tiny cross | SoftDot on RunningLight quads (`OrbitSet.cs:229`) |
| `FX_PlasmaTrail` | 512x128 | re-entry trail, LineRenderer Stretch, U 0 = capsule | default particle texture on the FieldSet streak (`FieldSet.cs:412-424`), SoftBand on CapsuleDrop trails (`CapsuleDrop.cs:179-182`) |
| `FX_DrillCracks` | 1024 (2x2, 512 cells) | radial bore-hole cracks, one quad r about 2.6 m, tiling 0.5 + offset per cell | the 6 `GlowDecal.Radial` crack quads (`FieldDrill.cs:75-84`) |
| `FX_CrackStrip_Drill_A/B` | 256x1024 | one crack per quad tapering to both ends | same quads if the 6-quad rig stays |
| `FX_CrackStrip_Swarm_A/B` | 256x1024 | opening fissure with molten slit and crust | `GlowDecal.Radial` on the FieldSwarm cracks (`FieldSwarm.cs:69`) |
| `FX_ScreamRing` | 512 | shock ring, leading edge at 90 % radius, ripples, fading wake | `GlowDecal.Radial` on ScreamRing (`FieldSwarm.cs:70, 285`) |
| `FX_GateField` | 256 | gate energy sheet: U across (edge fade + rims), V tiles (scanlines, hex rows, shimmer) | `GlowDecal.Scanlines` via `ScanlineMaterial` (`FieldBase.cs:389, 483`) |

Unity import: Texture Type Default, **sRGB off**, Alpha Is Transparency off, mip maps on, Bilinear, Wrap Clamp,
except `FX_BeamSoft` (Repeat U / Clamp V) and `FX_GateField` (Clamp U / Repeat V). One channel is enough (BC4/R8)
unless the particle shader samples alpha. `SpaceKit.Additive(texture, colour)` and `GlowDecal.Additive(texture,
colour)` already take a texture, so a small `CutsceneFxSet` ScriptableObject (or fields on `CutsceneCast`) replaces
the `SoftDot()`, `SoftBand()`, `Radial()` and `Scanlines()` calls; `GlowDecal.PlacePool` stays on `Decal_GlowPool`.

### Decor fallbacks for Crystal Plains (`build_decor_c1.py`)

PropScatterBuilder (`Levels/PropScatterBuilder.cs:283-323`) asks the level's EnvironmentSet for a decor per PropKind
and falls back to PropFactory when the set has none. Rendered under the Crystal Plains look next to Decor_Boulder,
Flora_Frond and Crystal_A, three round-17 exports fit and one did not:

| PropKind | Crystal Plains EnvironmentSet entry | Note |
| --- | --- | --- |
| IceShard | `Env/NorthernCrown/North_Decor_IceShard` | fits |
| Snowdrift | `Env/Decor_Snowdrift` (**new**, 998 tris, 512 maps) | the North drift reads as a white sheet with straight edges; the C1 drift is pale violet frost over Crysium dust |
| DeadTree | `Env/Mirewood/Mire_Flora_DeadTree` | fits (teal glow a touch off-palette); keeps its Sway colours |
| Reed | `Env/Mirewood/Mire_Flora_Reed` | fits well; keeps its Sway colours |

Run: `blender --background --factory-startup --python Tools/Blender/build_decor_c1.py -- [--preview <dir>]
[--norender]`. Import `Decor_Snowdrift` like the round-8 decor (pivot on the ground).

### build_all.py

Not edited here (shared). Lines to add before `"build_env_diorama.py"` (the decor fallback after
`build_env_decor.py` is also fine):

```
           # Round 26: dying Earth and theme skies (numpy, planet_kit.py).
           "build_planet_earth.py", "build_skies.py",
           # Round 26: cutscene glow / trail / crack sprites (numpy) and the Crystal Plains snowdrift.
           "build_cutscene_fx.py", "build_decor_c1.py",
           # Round 26: menu diorama (places the exports above) and the underground cave kit and eyes.
           "build_menu_diorama.py", "build_underground.py",
```

## Round 27: upgrade-screen display stage (`build_ui_display.py`, 2026-11-04)

Design notes in `DESIGN_NOTES.md` (round 27). Replaces the code-made stage in `ModelPreview.BuildStage`
(`Assets/_Project/Scripts/UI/ModelPreview.cs:207-250`). Nothing is imported yet. Output: `Exports/UI_Display/`
(+ `Low/`), `UI_Display_report.json`, `Blend/UI_Display.blend`.

Run: `blender --background --factory-startup --python Tools/Blender/build_ui_display.py -- [--preview <dir>]
[--only pedestal,ring,backdrop,preview] [--nobake] [--norender]` (about 1.5 minutes with previews). Uses
`tower_kit`, `base_kit`, `story_kit`, `marker_kit` and `stage_kit` read-only.

### Files

| File | Content | Replaces |
| --- | --- | --- |
| `UI_DisplayPedestal.fbx` | `UI_DisplayPedestal` (2,366 tris, static base) + `UI_DisplayPedestal_Ring` (580 tris, top ring) | `Floor` (PropMeshBuilder tube, `ModelPreview.cs:209-217`) |
| `UI_DisplayPedestal_Albedo/_Normal/_Emission/_Mask.png` | 1024 (Low 512), tower-kit self-bake, one atlas for both meshes | `SurfaceMaterials.Create(FloorColor ...)` x4 (`:215`) |
| `UI_DisplayRing.fbx` + `UI_DisplayRing.png` | flat quad 2.6 x 2.6 + greyscale halo 1024 (Low 512) | `Ring` torus (`:219-226`) |
| `UI_DisplayBackdrop.png` | 2048 x 1024 (Low 1024 x 512), opaque sRGB research-bay wall | `Backdrop` quad texture `Gradient()` (`:228-250`) and round 25's `UI/UI_PreviewBackdrop.png` |

### Unity notes

- **Pedestal:** import as the towers (Scale 1, Convert Units, Bake Axis Conversion, Normals Import, Tangents
  Mikktspace, no rig). **Pivot = centre of the top face** for both meshes (top radius 1.0, foot radius 1.08,
  0.2 high), so the model's feet sit on the pivot. In `PlaceStage` replace the floor placement with
  `_floor.localPosition = (0, y, 0)` and a **uniform** `_floor.localScale = Vector3.one * radius` (the pedestal keeps
  its proportions; drop `FloorThickness`). Put `UI_DisplayPedestal_Ring` under the floor at the same origin and turn
  it slowly about its local Y (e.g. -6 deg/s, against the turntable so it reads as machinery; three white lamps and
  twelve yellow ticks show the turn). The model now stands 0.2 x radius above the pedestal foot, so `Frame()` should
  include the pedestal (add about `0.1 * radius` to `tall` and lower the camera by the same, as the preview render
  does), otherwise the foot is cut off for flat models.
- **Pedestal material:** `Game/UnitLit` like a tower: Albedo, Normal, Emission, Mask (R AO, G smoothness, B metal
  detail, A ceramic detail; sRGB off), detail tiling in the report. The Emission map is **white**: the rim channel,
  the deck light ring, four seam lamps and the three ring lamps take `_EmissionColor`, e.g.
  `SurfaceMaterials.ColonyRim * 1.3` (the old `RingEmission` 1.1 is a fine start). Shadow casting off (as now).
- **Ring:** quad lies flat (normal Unity +Y), pivot at its centre; texture radius 1.0 = 1.3 units: main line at
  1.1 (just outside the foot), segmented arcs at 1.2, ticks between. Place it at the pedestal's foot:
  `_ring.localPosition = (0, y - 0.2 * radius + 0.003, 0)`, `_ring.localScale = (radius, 1, radius)`. Texture sRGB
  **off**, Clamp, mip maps on; RGB = A = grey. Additive unlit material (particle-additive or
  `GlowDecal.Additive(texture, colour)`) tinted `ColonyRim` x about 1.1. A slow spin (2-4 deg/s) suits it.
- **Backdrop:** texture sRGB on, Clamp, opaque. Keep the unlit quad on the camera pivot but scale it
  **(BackdropSize, BackdropSize / 2, 1) = 40 x 20** to keep the 2:1 aspect. At the preview's distance (~32) the
  square RenderTexture sees the centre ~43 % of the width and ~86 % of the height: the lit bay with ribs, light bar
  and a dim service line; the floor line sits at v 0.32 so the pedestal lands roughly on it. The outer panel wall and
  the amber pillar lamps only show in wider windows. Display values: mean 0.10, 99th percentile 0.58 (light bar).
  `ModelPreview.Background` (camera clear colour) can stay; it is hidden.
- `FloorColor`, `BackdropTop/Bottom`, `GradientSteps` and `RingThicknessShare` become unused after the swap.

### build_all.py

Not edited here (shared). Line to add after `"build_ui.py",`:

```
           # Round 27: upgrade-screen display stage (pedestal, halo ring, backdrop).
           "build_ui_display.py",
```

## Round 28: continent bosses ready for their signature mechanics (2026-11-05)

Brief: `Docs/BOSSES.md` (climb layer, pools, one signature mechanic per boss). Design notes and the review in
`DESIGN_NOTES.md` (round 28). New scripts (shared modules untouched, only imported): `boss_mech_kit.py` (helpers),
`build_gravemaw_mech.py`, `build_cindermatriarch_mech.py`, `build_frostwarden_mech.py`, `build_rotbloom_mech.py`,
`build_shattered2.py` (improvement pass, replaces the round-16 Shattered export), `build_shattered_mech.py`,
`build_sealedone_mech.py`. Nothing is imported into Unity yet.

**How the mech scripts work:** each opens `ArtSource/Blend/<Boss>.blend`, adds marker bones, keys the new clips
with the boss build script's own pose functions (no rebake: mesh, maps and the old clips are unchanged), keys the
LOD1 rig from the full rig, re-exports `<Boss>.fbx` and `<Boss>_LOD1.fbx` with **all** clips (old + new), merges a
`round28` block into `<Boss>_report.json` (every clip's frames, loop flag, event frames, offsets, depths) and saves
the `.blend`. Re-running is idempotent (the previous round-28 clips, markers and extra meshes are removed first).
So the boss build script must run first and its mech script after it. Run any of them with
`blender --background --factory-startup --python Tools/Blender/<script> -- [--preview <dir>] [--norender]`
(about 1-3 minutes each, props included; `build_shattered2.py` about 7 minutes, it bakes).

### Conventions for every boss (read this first)

- **Axes and facing:** built like every creature: Blender -Y forward, Z up; exported FBX -Z forward, Y up (Bake
  Axis Conversion). Unity found the creature FBX turned 180 degrees about Y (head at -Z): keep the same import
  correction as the other creatures. Every "forward" in the reports is along the creature's own facing.
- **Loops are in place** (no root motion; frame 0 = last frame). `Climb_Loop`, `Submerged_Loop`, `Burrow_Travel`,
  `Hunt_Stride`, `Walk`, `Rooted_Idle`, `Fly`, `Idle`.
- **Travel clips are authored with the pivot fixed** (the Root bone never moves): `Climb_Up`, `Climb_Down`,
  `Dive_Into_Liquid`, `Surface_Burst`, `Burrow_Down`, `Surface_Throw`, `Shatter_Through`, `Rise_OntoBase`. The report
  gives `end_offset_forward_m` / `end_offset_up_m`: on the last frame move the transform by that offset and start the
  next clip (its first frame is the same pose relative to the new pivot). Apply Root Motion stays off.
- **Climb_Up / Climb_Loop / Climb_Down:** over a ledge of the reported height (`ledge_height_m`) whose face is
  `edge_forward_m` ahead of the pivot at the start. Feet are planted (the body moves at exactly the stance speed of
  the gait), no foot lands on the face or the lip. `Climb_Loop` is the braced crawl on top (pivot on the top
  surface; lower, wider, slower than Walk). Different heights: pin the `IK_*` markers to the climb layer and blend
  the transform height.
- **Foot markers `IK_<limb>`** (e.g. `IK_Leg_L1`, `IK_Arm_R`, `IK_LArm_L`): one bone per limb at the rest foot
  contact, child of the limb's last bone, pointing up 0.25. Deform flag on (so the FBX keeps them) but no vertex
  weights. Use them to raycast and pin feet (two-bone IK) on rocks, gate tops and the base roof.
- **Pools:** `Dive_Into_Liquid` starts on the bank (pivot on the liquid level, bank edge just ahead of the front
  feet) and ends in `Submerged_Loop`'s first pose; `Submerged_Loop` keeps the whole mesh 0.2 under the surface at
  `submerged_depth_m` (measured on the deformed mesh); `Surface_Burst` starts in that pose under the exit point and
  lands on the bank `end_offset_forward_m` ahead. The liquid must hide everything under the surface (or the pool must
  be that deep): only the glow, a wake and bubbles show (BOSSES.md). Event frames: `splash_frame`, `breach_frame`,
  `land_frame`.
- **Scale keys:** clips that hide parts key bone **scale** (keep "Remove constant scale curves" off). New clips key
  only the scale bones they use (listed per boss below); Unity's phase overrides in LateUpdate keep working.
- **Previews** (`--preview <dir>`): `<Boss>_<Clip>.png` strips (8 key poses, 4 for loops) with stand-in props
  (ledge, gate, tower, translucent pool), `<Boss>_turntable.png`, prop strips, `SealedOne_Phases.png`.

### Gravemaw (`build_gravemaw_mech.py`)

Files: `Gravemaw.fbx` / `_LOD1.fbx` (24,093 / 9,999 tris, unchanged), 47 bones (39 LOD). New bones: `IK_Arm_L/R`,
`IK_Leg_L1..R3`, `Socket_Throw` (maw tip). Crawl stays its Walk, Roar its Rage; Dive/Emerge (round 14) stay for the
entrance. Scale keys: `Debris_*` (as round 14).

| Clip | Frames | Loop | Mechanic (BOSSES.md) | Key facts (report `round28.clips`) |
| --- | --- | --- | --- | --- |
| Climb_Up | 336 | no | surfaces on rocks, climbs over the edge | ledge 1.2 at 3.2 ahead; end offset 3.30 fwd, 1.2 up; the spine lies up the face like a centipede |
| Climb_Loop | 80 | yes | crawl on a rock top | 0.588 per loop |
| Climb_Down | 336 | no | off the rock | end offset 3.32 fwd, -1.2 |
| Dive_Into_Liquid | 96 | no | quicksand as a burrow shortcut | bank 3.2 ahead, splash 42, end offset 9.20 fwd |
| Submerged_Loop | 96 | yes | hidden in the pool | path depth 1.84, mesh top -0.25 |
| Surface_Burst | 100 | no | out at the bank | breaks the surface 3.4 ahead at frame 24, end offset 6.93 |
| Burrow_Down | 110 | no | Burrow at 75/50/25 % | drill impact (debris, rumble) 31 at the hole 2.95 ahead; ends in Burrow_Travel, end offset 7.48 |
| Burrow_Travel | 72 | yes | the crack line | under the ground at path depth 1.29; only the crest ridge breaks the surface (top +0.3): run the glowing crack along it |
| Surface_Throw | 96 | no | surfaces beside the top-damage tower and throws it | from Burrow_Travel; hole 1.0 ahead, burst 14 (debris), throw 30 (`Socket_Throw`; the tower ~1.0 right of the hole is flung up and right), crashes down; end offset 2.86 |

### Cinder Matriarch (`build_cindermatriarch_mech.py`)

Files: `CinderMatriarch.fbx` / `_LOD1.fbx` (23,051 / 8,997 tris, unchanged), 30 bones (25 LOD), new `IK_Leg_L1..R2`.
Props: `Exports/CinderEgg/` (new). The fire bug hatchling is the round-16 `FireLarva` (unchanged). Scale keys: `Vent_1..3`.

| Clip | Frames | Loop | Mechanic | Key facts |
| --- | --- | --- | --- | --- |
| Climb_Up / Climb_Down | 186 / 186 | no | climbs the rock ridges between the pads | ledge 1.5, face 2.0 ahead; end offset 5.02 fwd, +-1.5 |
| Climb_Loop | 81 | yes | on the ridge | 1.03 per loop |
| Dive_Into_Liquid | 66 | no | into one lava pool ... | take-off 20, splash 28, under 52; end offset 3.2 |
| Submerged_Loop | 80 | yes | hidden in the lava | depth 3.34 |
| Surface_Burst | 64 | no | ... out of the one nearer the cold target | breach 16, land 32, end offset 3.0 |
| Hunt_Stride | 50 | yes | Hatred of the Cold: off-road hunt | head low, scythes forward, crouched; 1.61 per loop |
| Smash | 64 | no | wrecks the cold tower | rears to 20, impact 30 on a tower 3.1 ahead (make it a wreck), shove to 44 |
| Lay_Eggs | 96 | no | eggs in the lava below 50 % | eggs leave bone `Drip` (sac tip, ~2.8 behind the pivot: stand with it over the lava) at 34, 52, 70 |

**CinderEgg** (`CinderEgg.fbx`, 1,420 tris, one 512 material `CinderEgg_Mat` + Low 256, 5 bones, Unity scale 1, 0.5 x
0.6 high): a molten core gripped by three crust claw-plates. Pivot on the lava surface (the collar floats in it).
Clips: Idle 48 (loop, pulse), Hatch 54 (shakes to 28, plates peel and fly 30-50; spawn a `FireLarva` at frame 32,
Unity scale 0.6), Burst 20 (shot: plates pop, the core collapses by 10, no larva). Scale keys `Shell_1..3`, `Core`.
BOSSES.md: hatch after 10 s unless shot.

### Frost Warden (`build_frostwarden_mech.py`)

Files: `FrostWarden.fbx` / `_LOD1.fbx` now hold **two meshes**: `FrostWarden` (23,880, unchanged) and the new
**`FrostWarden_IceArmour`** (3,187 tris; LOD `FrostWarden_IceArmour_LOD1` 1,497), skinned to the same rig, material
`FrostWarden_IceArmour_Mat` with `FrostWarden_IceArmour_Albedo/_Normal/_Emission/_Mask.png` (512, Low 256) in
`Exports/FrostWarden/`. 41 bones (40 LOD). New bones: `IK_Leg_L1..R2`, `Armour_1..8` (plates: chest, shoulders,
back, train x3, hip), `Icicle_1..5` (back icicles), `Socket_Breath` (face slit, forward). Scale keys: `Halo`,
`Armour_*`, `Icicle_*` (all clips key them at 1 except Armour_Break). FreezePulse/Enrage/WalkEnraged stay.

| Clip | Frames | Loop | Mechanic | Key facts |
| --- | --- | --- | --- | --- |
| Climb_Up / Climb_Down | 210 / 210 | no | climbs onto a frozen gate before it smashes it | ledge 1.5, face 1.5 ahead; end offset 4.02 fwd, +-1.5; the gate sags under it (Unity) |
| Climb_Loop | 93 | yes | on the gate | 0.824 per loop |
| Dive_Into_Liquid | 70 | no | breaks through an ice sheet | glaives smash the ice at 18, take-off 22, splash 30, under 56; end offset 2.6 |
| Submerged_Loop | 96 | yes | swims under the ice | depth 4.73 |
| Surface_Burst | 64 | no | out through the ice | breach 16, land 32, end offset 2.6 |
| Freeze_Gate | 64 | no | Gate Breaker: freezes the gate solid | gate 2.4 ahead; breath cone from `Socket_Breath` and the halo flare at 30 (the gate becomes an ice block for 8 s) |
| Shatter_Through | 84 | no | walks straight through the frozen gate | hunched charge, glaives crossed; the chest hits the gate (2.4 ahead) at 51: shatter it into the wreck; end offset 2.68, then WalkEnraged |
| Armour_Break | 56 | no | armour breaks below 40 % / Lava Ball | stagger, plates fly off one by one 12-35 (scale 0), roar, ends in the WalkEnraged hunch |

**Ice armour in Unity:** phase "armoured" = `FrostWarden_IceArmour` renderer on; after `Armour_Break` turn it off (or
leave it: every plate ends at scale 0). The Lava Ball's 5 s of double damage: play Armour_Break and show the armour
again afterwards (renderer on, plates back at 1). Rage telegraph: set `Icicle_1..5` uniform scale 0.2 (calm) to 1.0
(max rage) in LateUpdate; the icicles belong to the armour mesh.

### Rotbloom (`build_rotbloom_mech.py`)

Files: `Rotbloom.fbx` / `_LOD1.fbx` (23,968 / 9,498, unchanged), 34 bones (32 LOD), new `IK_Leg_L1..R3`,
`Socket_Spore` (above the bloom throat, child of `Bloom`). Scale keys: `Pod_*`, `Bloom`. Props: `Exports/RotSporePod/`,
`Exports/RotOvergrowth/`, `Exports/RotbloomRoots/` (new).

| Clip | Frames | Loop | Mechanic | Key facts |
| --- | --- | --- | --- | --- |
| Rooted_Idle | 120 | yes | phase rooted: stands still on the bog | body 0.22 down, feet 0.2 into the bog; creep forward by moving the transform slowly |
| Uproot | 64 | no | phase uprooted below 50 % | legs tear out at 14/17/22/25/30/33 (mud bursts at the feet), roar 36; ends in the Idle/Crawl stance |
| Spore_Volley | 84 | no | Overgrowth every 12 s | petals open 6-26 (sweet sound), pods pump, a `RotSporePod` leaves `Socket_Spore` at 32, 40, 48 (max 3 pads) |
| Roar | 66 | no | intro / alarm | jaw wide 16-50 |
| Climb_Up / Climb_Down | 232 / 232 | no | (all bosses climb) | ledge 1.2, face 1.6 ahead; end offset 4.57 fwd, +-1.2 |
| Climb_Loop | 93 | yes | | 0.571 per loop |
| Dive_Into_Liquid | 72 | no | sinks into the bog | take-off 22, splash 30, under 58; end offset 2.4 |
| Submerged_Loop | 96 | yes | its roots grow under water to the next bank | depth 2.29 |
| Surface_Burst | 64 | no | out at the next bank | breach 18, land 34, end offset 2.4 |

**RotSporePod** (`RotSporePod.fbx`, 900 tris, 512/256 material, 6 bones, 0.68 long): a glowing spore core in three
hooked bark bracts, two slime tendrils; flies nose first. Clips Fly 24 (loop: spins, bracts breathe), Burst 16
(bracts snap open, the core pops at 6: spore cloud VFX). Fly it ballistic from `Socket_Spore` to the pad; Burst on
impact or when shot down (BOSSES.md: shooting the pods prevents the overgrowth). Scale keys `Core`, `Tail`,
`Bract_1..3`.

**RotOvergrowth** (`RotOvergrowth.fbx`, 2,592 tris, 512/256, static, no rig): seven thorned vines with glowing
pustules spiral up from the pad edge over a tower (radius ~0.45, up to 1.75 high) plus a mat of rootlets on the pad.
Pivot: the pad centre on its top. Growth: blend shape **`Retracted`** (weight 100 = flat on the pad, 0 = grown) and
the colour attribute **`Growth`** (R = growth order 0 at the pad to 1 at the highest tips, G = jitter). Recipe: blend
shape 100 -> 0 over ~1 s while a clip threshold on `Growth.R` runs 0 -> 1; burning it away (explosion, hero) is the
reverse with an emission flash. Scale it to the tower if needed.

**RotbloomRoots** (`RotbloomRoots.fbx`: `RotbloomRoots_Stage1/2/3`, 734 / 1,844 / 3,894 tris, one shared 512/256
material `RotbloomRoots_Mat`, static): the roots that announce Rotbloom over three waves (BOSSES.md entrance). Stage 1:
five thin roots (~2 m across); stage 2: eight thicker roots with thorns and four glowing pods (~3.7 m); stage 3: a big
arching root mass, six pods and a closed wine-red bud in the middle (~4.9 m). Pivot: the stage centre on the ground.
Swap the stage each wave (or scale the next one up from 0.6 while swapping).

### The Shattered (`build_shattered2.py`, then `build_shattered_mech.py`)

**Improvement pass** (`build_shattered2.py`, overwrites the round-16 `Shattered.fbx`, `_LOD1.fbx`, maps and
`.blend`): pale shattered-ice granite plates over black glass instead of Gravemaw's plum-bronze, a crown of crystal
blades on the head, a crystal cleaver out of each fist, and three crystal spires on the back on scale bones
`Core_Road`, `Core_Air`, `Core_Land` (each shaped like its shard). Same rig plus those three bones, same budgets
(24,025 tris: 17,493 body + 6,532 crystal; LOD1 9,499), same round-16 clips and phase table. Run it after
`build_shattered.py` and before `build_shattered_mech.py`.

Files after the mech script: 34 bones (33 LOD), new `IK_Arm_L1/R1`, `IK_Leg_L1/R1`. Shards in `Exports/ShardRunner/`,
`Exports/ShardFlyer/`, `Exports/ShardCrawler/` (new). Scale keys: Split and Rejoin key `Shell_*`, `Crys_*`, `Core_*`,
`Pelvis` and the limb bones; the other new clips key no scale (the phase table override in LateUpdate keeps working).

| Clip | Frames | Loop | Mechanic | Key facts |
| --- | --- | --- | --- | --- |
| Split | 64 | no | Splitting at 66 % | light cracks, crystals swell, it rises (to 26); burst at **28**: plates thrown off, the three spires fly to the spawn points, the body collapses into the flash (gone by 36). Spawn the shards at 28 at their offsets (x right, up, forward): ShardRunner (1.6, 0, 2.2), ShardFlyer (0, 3.2, -0.4), ShardCrawler (-1.6, 0, -1.8). Hide the giant after the clip |
| Rejoin | 80 | no | rejoin at 33 % where they are | starts empty; fly the surviving shards in during 0-24, remove them at **30** (form): the spires land, the body grows out of them (overshoot), it roars; ends in the phase-3 (rejoined) look. A dead shard: set its `Core_*` scale to 0.35 after the clip |
| Roar | 70 | no | intro / alarm | rears on the hind legs, cleavers up, jaw wide 18-54 |
| Climb_Up / Climb_Down | 186 / 186 | no | its shards scale the ruins (the giant climbs too) | ledge 1.5, face 1.95 ahead; end offset 5.66 fwd, +-1.5 |
| Climb_Loop | 81 | yes | | 1.161 per loop |
| Dive_Into_Liquid / Submerged_Loop / Surface_Burst | 66 / 80 / 64 | no / yes / no | pools | depth 4.70; splash 28; breach 16, land 32; end offsets 2.8 |

Phases (round 28 design): 1 whole (phase-1 table), 2 split (giant hidden, three shards on the land, air and second
road), 3 rejoined (phase-3 table, Rejoin ends in it). `PhaseBreak1/2` (round 16) are superseded by Split/Rejoin;
keep them as a heavy damage stagger or drop them.

**Shards** (one 512/256 material each, Unity scale 1, the giant's palette and light; report: route, spawn offset,
HP share):

| Asset | Tris / bones | Size (w x l x h) | Clips (frames) | Notes |
| --- | --- | --- | --- | --- |
| ShardRunner | 2,215 / 9 | 0.75 x 2.6 x 1.8 | Walk 20 (loop, 2.62 per loop), Idle 48, Hit 12, Death 40, Spawn 30, Rejoin 30 | the second road: a two-legged strider with a spear head and a blade crest |
| ShardFlyer | 2,003 / 7 | 2.6 x 1.85 x 0.45 | Fly 24 (loop), Idle 32 (hover), Hit 12, Death 44, Spawn 30, Rejoin 30 | the air route: a kite of crystal feather-blades; pivot = body centre (give it the flyer ground shadow); Death drops 3.0 and the blades shatter (scale 0 by 14) |
| ShardCrawler | 2,425 / 17 | 2.1 x 2.1 x 1.25 | Walk 26 (loop, 1.0 per loop), Idle 48, Hit 12, Death 40, Spawn 30, Rejoin 30 | the ground route: six blade legs under a pale shell |

Spawn unfolds the shard from a curled ball at scale 0.3; Rejoin curls it up and springs (Unity then flies it to the
giant in ~0.6 s). Scale keys: `Root` and the limb bones (crawler, runner), `Root` and `Wing_*_1` (flyer).

### The Sealed One (`build_sealedone_mech.py`)

Files: `SealedOne.fbx` / `_LOD1.fbx` (34,478 / 13,998, unchanged mesh), 32 bones (29 LOD), new `IK_LArm_L/R` (the
lower hands) and **`Rubble`** (the heaved ground ring round the fissure was skinned to Root; it is now on its own
bone at the pivot). Scale keys: `Ring_1..3` in every new clip; `Seal_1..7` and `Rubble` (scale 0, dropped 1.5 under
the ground) in the clips that leave the seal: Walk, Climb_*, Dive_Into_Liquid, Submerged_Loop, Surface_Burst,
Rise_OntoBase. Idle/Rise/Summon/Hit/Death (round 16) stay; Rise is still the entrance from the seal.

| Clip | Frames | Loop | Mechanic | Key facts |
| --- | --- | --- | --- | --- |
| Walk | 80 | yes | follows the risen road | hand over hand, the torso surges and ploughs the ground (add a furrow decal and dust); 2.36 per loop |
| Roar | 72 | no | intro / phase change | rears, both hands slam at 30, plates open 14-60 |
| Climb_Up / Climb_Down | 132 / 132 | no | climbs a ledge | hands grip the top at 30, the torso is hauled over; ledge 2.0, face 2.8 ahead; end offset 3.2 fwd, +-2.0 |
| Climb_Loop | 100 | yes | on top | the haul, slower and lower |
| Dive_Into_Liquid | 90 | no | sinks into a pool | plates close, rings shrink to 0.45; end offset 2.0 |
| Submerged_Loop | 96 | yes | hidden | depth 5.58 (the rings glow through) |
| Surface_Burst | 100 | no | out at the bank | hands slam on the bank at 24, the torso is hauled out, roar; end offset 2.4 |
| Rise_OntoBase | 130 | no | pulls itself up onto the base | the base 3.4 ahead, top 0.9: hands grab at 34, the torso heaves on and the base takes the hit at **96**, then it looms with the plates open |
| Adapt | 64 | no | Recognition (halved damage per phase) | goes still and listens, rings align, the violet pulse at **30**: ORA's line and the dimmed icon there |
| Release | 170 | no | the last phase: it opens | rings unfold to 1.8 and slow, plates wide, it kneels by 150; swap to the Release emission maps at **64** and fade the emission up; the epilogue starts on the last frame |

**Phase looks:** emission variants next to the shipped maps (1024 and `Low/` 512):
`SealedOne_Phase1..4_Emission.png`, `SealedOne_Seal_Phase1..4_Emission.png`, `SealedOne_Release_Emission.png`,
`SealedOne_Seal_Release_Emission.png`. Phase 1 = the shipped violet and gold; 2 brighter violet; 3 hot
magenta-violet with the gold glyphs dimming (the seal fails); 4 lilac-white blaze, glyphs almost out; Release white.
Swap both materials' emission texture per phase (Adapt marks the change); suggested emission strengths 2.6 / 3.0 /
3.4 / 3.9 / 4.5 (report `round28.phase_looks`). The decor crystals turning the immune type's colour is Unity's job.

### build_all.py

Not edited here (shared). Lines to add right after the round-16 continent bosses (`"build_sealedone.py",`):

```
           # Round 28: the Shattered improvement pass (overwrites build_shattered.py's export), then the mechanic
           # clips and props of the six continent bosses (boss_mech_kit.py; each opens its boss's .blend).
           "build_shattered2.py", "build_gravemaw_mech.py", "build_cindermatriarch_mech.py",
           "build_frostwarden_mech.py", "build_rotbloom_mech.py", "build_shattered_mech.py", "build_sealedone_mech.py",
```

## Round 29: missing boss art for Unity's round 4a/4b/4c (2026-11-05)

Brief: the Blender items in `Docs/MODEL_TODO.md` "Ronde 4a/4b/4c Unity". Design notes in `DESIGN_NOTES.md` (round
29). Nothing is imported into Unity. New scripts: `build_bosses29_props.py` (ruin gate, seal crystals, gate ice
block, drill-rig wreck, drained gate emission), `build_bosses29_fx.py` (ground decals, flipbooks, the dampened HUD
overlay), `check_clip_scales.py` (QA). Changed: `build_shattered_mech.py` (shard climbs, `--only-shards`),
`build_sealedone_mech.py` (OnBase_Loop, seal keys), `build_frostwarden_mech.py` (armour keys) and
`boss_mech_kit.py` (`Boss.hold` / `hold_scale`).

### Clip scale fix (Frost Warden, Sealed One)

The FBX exporter bakes a bone that a clip does not key with whatever pose the previous clip left behind. Bones
added after a clip existed therefore came out at scale 0:

- **Frost Warden:** `Armour_1..8` were at scale 0 in Walk, WalkEnraged, Idle, Hit, Death, Enrage and FreezePulse.
  These clips now key the plates and `Icicle_1..5` at 1. Armour_Break is the only clip that removes the plates.
  Unity's `BoneScaleView` workaround can go.
- **Sealed One:** `Seal_1..7` and `Rubble` were at scale 0 in every clip, Rise included. They are now keyed in
  every clip: scale 1 in **Rise** only (the stones burst at frame 22, as built in round 16), 0.001 everywhere
  else. That includes Idle, Summon, Hit and Death, which are now played away from the seal.

`boss_mech_kit.Boss.hold(clips, {bone: scale})` writes those constant keys into the full and LOD actions.
`check_clip_scales.py -- --fbx <path> [--bones Armour_,Seal_]` re-imports an FBX and lists the bones whose scale is
not 1, per clip. Run it after any rig change. The other bosses are as intended: Gravemaw's `Debris_*` stay hidden
outside their bursts, and Cinder, Rotbloom and Shattered scale only what their clips key.

### Shard climbs (`build_shattered_mech.py -- --only-shards`)

ShardRunner and ShardCrawler get Climb_Up, Climb_Loop and Climb_Down over a **1.0 m** ruin ledge. They follow the
round-28 boss conventions. The ShardFlyer does not climb. The facts are in `<Shard>_report.json` → `round29`.

| Shard | Clip | Frames | Loop | Facts |
| --- | --- | --- | --- | --- |
| ShardRunner | Climb_Up | 32 | no | face 1.15 ahead; end offset 2.62 fwd, +1.0; a crouched vault, one stride onto the top |
| ShardRunner | Climb_Loop | 26 | yes | 2.10 per loop, low and wide on the top |
| ShardRunner | Climb_Down | 32 | no | lip 1.15 ahead; end offset 2.62 fwd, -1.0 |
| ShardCrawler | Climb_Up | 93 | no | face 1.25 ahead; end offset 3.60 fwd, +1.0; front legs reach up the face, body tips over the lip |
| ShardCrawler | Climb_Loop | 34 | yes | 0.80 per loop |
| ShardCrawler | Climb_Down | 93 | no | lip 1.25 ahead; end offset 3.60 fwd, -1.0 |

- **Transform:** the pivot is authored fixed. On the last frame, move the transform by the end offset.
- **Root bone:** unlike the bosses, the climb travel is keyed on the **Root bone**. A few fused vertices are
  weighted to Root and would otherwise stay behind. That is safe in Unity: the importer sets no root motion node
  and `applyRootMotion` is off, so Root plays as an ordinary bone.
- **Markers:** there are no IK markers on the shards (baked clips plus a slope tilt, as for the bosses).
- **Rebuild:** the run rebuilds and re-bakes all three shards, the flyer unchanged.

### Sealed One `OnBase_Loop` (`build_sealedone_mech.py`)

| Clip | Frames | Loop | Events |
| --- | --- | --- | --- |
| OnBase_Loop | 96 | yes | strike frames **34** (left lower hand) and **82** (right) |

- **Pose:** frame 0 (= the last frame) is Rise_OntoBase's last pose at the same transform, so Unity switches into
  the loop straight from that clip.
- **Motion:** each hand is pulled up and back, the torso rears into it, then the hand slams onto the roof and the
  torso drops onto it. Put the base hit, dust and a camera kick on each strike frame. The rings make one full turn
  per loop.
- **Adapt:** on the base it still has no clip of its own. Play Adapt, or keep the loop running.

### Props (`build_bosses29_props.py`)

Run: `blender --background --factory-startup --python Tools/Blender/build_bosses29_props.py -- [--preview <dir>]
[--norender] [--nobake] [--only RuinGate,SealCrystal,IceBlock,DrillWreck,Drained]` (about 6-7 minutes). It reuses
`build_cutscene.Drill` pieces in code and reads the shipped gate emission maps, so it runs after `build_gate.py`,
`build_towers*.py` and `build_cutscene.py`.

| Asset | Contents | Maps | Size (m) | Pivot |
| --- | --- | --- | --- | --- |
| `RuinGate/RuinGate.fbx` | `RuinGate` 6,385 tris, `RuinGate_Rig` 11 bones (Root, Seal, Seal_1..4, Petal_1..5) | 1024 (Low 512) | 8.68 x 2.18 x 7.77; opening 4.0 wide x 4.3 high | threshold centre on the ground; the giant walks out towards Blender -Y (Unity +Z) |
| `SealCrystal/SealCrystal.fbx` | `SealCrystal_A` 1,644 (spire in a collar), `_B` 881 (cluster in a socket), `_C` 1,674 (crystal in three claws); one shared material, static | 512 (Low 256) | ~1.0 x 0.9 x 1.35-1.68 | centre on the ground |
| `Gate_IceBlock/Gate_IceBlock.fbx` | 2,398 tris, static; Albedo RGBA (alpha = opacity) | 512 (Low 256) | 3.65 x 2.07 x 2.56; encloses Gate_G1..G3 | road centre on the ground, like the Gate prefab |
| `Cutscene_Drill_Wreck/Cutscene_Drill_Wreck.fbx` | 4,716 tris, static, tower-kit self-bake | 1024 (Low 512) | 7.81 x 4.16 x 2.67 | rig deck centre, like `Cutscene_Drill` (swaps in place) |
| `Gate/Gate_Drained_Emission.png`, `Tower_GateGun/Tower_GateGun_Drained_Emission.png` (+ `Low/`) | same atlas as the shipped emission | | | |

**RuinGate clips** (`RuinGate_Rig|<clip>`). Scale keys: `Seal`, `Seal_1..4`.

| Clip | Frames | Loop | Events |
| --- | --- | --- | --- |
| Closed_Idle | 72 | yes | the seal knot breathes |
| Open | 160 | no | rumble 0-30; **seal_crack 30** (the knot collapses, four seal shards fly and vanish by 48); **first_light 38** (seams open); **swing_start 52** (petals fold back 98 degrees, bottom first and flat at 88); **fully_open 130**; settles to 160 |
| Opened_Idle | 72 | yes | open petals sway; seal at scale 0 |

Unity notes:

- **RuinGate:** import like CinderEgg, at scale 1.
  - Emission (magenta glyph grooves, seams, sigil, crystals): about 0.6 while closed, rising to about 2.5 from
    first_light to fully_open, then about 1.6.
  - Put a flash and point light inside the opening at first_light and a shard burst at the `Seal` bone at
    seal_crack. The giant appears in that flash.
- **SealCrystal:** the emission map is a **white mask**. Set `_EmissionColor` to the immune tower's colour x ~2.4;
    dormant lilac (0.62, 0.45, 1.0) x 1.6; Release white x 3. Place 3-5 round the rift at any yaw, scale 0.8-1.2.
- **Gate_IceBlock:** alpha-blended with depth write, drawn after the gate, on the gate's own transform.
  - Albedo alpha runs from 0.35 in clear ice to 0.95 in the fractures. Smoothness is in Mask G; the faint crack
    emission is about x 1.2.
  - Show it at the Freeze_Gate cast (scale 0.9 to 1 over 0.3 s). Hide it with an ice-shard burst on
    Shatter_Through. It replaces the `Liquid_Ice` cube.
- **Cutscene_Drill_Wreck:**
  - The shape: the derrick snaps at 1.3 m and folds about 110 degrees over the +X legs (angle in the report).
    Pipes, rubble and three Crysium crystals lie in the heave, and the glow is dead apart from a few embers.
  - At Gravemaw's smash frame, hide `Cutscene_Drill` and show the wreck in the dust, yawed with the top away from
    the hole.
- **Drained gate:** while a gate is drained, swap `_EmissionMap` to the `*_Drained_Emission` maps (lit texels at
  14 %, an uneven flicker pattern, a faint violet residue). Swap back when the gate recovers.

### Boss decals, flipbooks and the dampened icon (`build_bosses29_fx.py`)

Run: `blender --background --factory-startup --python Tools/Blender/build_bosses29_fx.py -- [--preview <dir>]
[--only decals,sheets,icon,previews]` (about 2 minutes; numpy, plus one Cycles render for the icon). Files go to
`ArtSource/Exports/Boss_FX/` (+ `Low/` at half size) with `Boss_FX_report.json`: per file the size, layout, use,
colour space, wrap, threshold, tint and what it replaces. The icon goes to `ArtSource/Exports/UI/UI_Dampened.png`
(+ `@2x`); the existing atlases are unchanged.

Conventions:

- **Decals** have two files:
  - `BossFX_<Name>_Albedo.png`: sRGB, straight alpha = coverage, top-left light baked in.
  - `BossFX_<Name>_Emission.png`: sRGB off. **R** glow mask, **G** a progress threshold, B 0, A 1.
- **Strips** are 1024 x 256. U runs along the path (Repeat), V runs across (Clamp, soft edges). Use LineRenderer
  Tile mode, or quads with a random U offset.
- **Flipbooks** are 4 x 4 frames of 256 (1024), sRGB off, frame 0 top-left. Two packs:
  - CG pack (additive, tinted): R core, G glow, B extra, A coverage.
  - LIT pack (the round-20 Acid_Splash recipe): R lit shading x A, G thickness/heat, B specular, A alpha.

| File | Size / layout | Use | Threshold / tint | Replaces |
| --- | --- | --- | --- | --- |
| BossFX_LavaTrail | strip, 1.4 m wide, 5.6 m per tile | Cinder Matriarch trail: basalt plates, ropy flow, glowing cracks, molten channel | G = heat: lit while G > cool (0 to 1 over 6 s; the edges cool first). Emission (1, 0.35, 0.08) x 3 | LavaTrailView glow pools |
| BossFX_GroundCrack | strip, 2 m wide, 8 m per tile | Gravemaw crack line / entrance: fissure, molten core, heaved plates | show where G > 1 - open. Emission (1, 0.42, 0.1) x 3-4 | MarkerKind.GlowBlade seam (also CrackStyle.Heat) |
| BossFX_Furrow | strip, 3 m wide, 12 m per tile | Sealed One haul: dark trench, drag scratches, heaved lips, dust | R = faint violet seep (0.6, 0.3, 1) x 1.5 | CrackStyle.Furrow |
| BossFX_RotTrail | strip, 1.6 m wide, 6.4 m per tile | Rotbloom trail: slime, mould rings, pustules, spores | visible while G > decay. Emission (0.55, 1, 0.2) x 1.5 | LavaTrailView in rot colours |
| BossFX_RoadSink | strip, ~3.5 m wide, 14 m per tile | old road sinking when the Sealed One's road rises | show where G > 1 - sink (~2 s, middle first); R void seep (0.55, 0.25, 1) | nothing yet |
| BossFX_FrostCreep | 1024, 2 x 2 radial cells of ~2.5 m | Frost Warden rime ahead of it and under a frozen gate | show where G < grow (0 to 1 in ~1 s). Emission (0.5, 0.85, 1) x 1.2; tiling 0.5 + offset per cell | pale glow pool + mist |
| BossFX_SporeCloud | 4x4, 12 fps, CG, pivot (0.5, 0.2) | spore pod lands on a pad: cloud falls, spreads, thins | G tint (0.5, 1, 0.25) | TowerAuraView green haze |
| BossFX_VineBurn | 4x4, 16 fps, CG, pivot (0.5, 0) | burning the overgrowth off a pad | G (1, 0.45, 0.1); 3-5 quads round the tower while `Retracted` runs 0 to 100 | emission flash on the vines |
| BossFX_ShardFlash | 4x4, 24 fps, CG | Shattered Split burst (frame 28, ~6 m); **Rejoin = the same sheet played backwards**, ending on form frame 30 | G (0.95, 0.45, 1); B ring (0.6, 0.9, 1) | generic flash |
| BossFX_DiveWake_Water / _Lava / _Bog / _Quicksand / _Ice | 4x4 loop, 12 fps, LIT | flat on the surface over a submerged boss (top = forward), ~1.2 x boss width; quicksand is a spinning funnel, ice bobbing slush plates | per-liquid tints in the report; lava emission G x (1, 0.4, 0.08) x 4 | glow pool + generated bubbles |
| UI/UI_Dampened (+@2x) | 128 (256), sRGB, straight alpha | boss bar: broken three-piece ring with an "off" slash over the greyed tower picture | white, tint violet (0.72, 0.45, 1.0) | grey-tinted HUD card |

Unity import:

- Albedo: sRGB on, Alpha Is Transparency on. Emission maps and all flipbooks: sRGB off.
- Strips: Wrap Repeat U / Clamp V. Everything else: Clamp. Mip maps on, Bilinear.
- Decals: a URP Decal Projector, or a ground quad with an alpha-blended decal material. Each one needs a single
  float (cool / open / decay / sink / grow) for its threshold.
- Flipbooks: Texture Sheet Animation 4 x 4.

### Previews

Previews are in the session scratchpad, `bosses29/` (not kept in the repo):

- Shard climbs: `ShardRunner_Climb_*.png`, `ShardCrawler_Climb_*.png`, plus the `_Climb_Up_game` views.
- Sealed One: `sealed/SealedOne_OnBase_Loop(_game).png`.
- Frost Warden: `frost/FrostWarden_*.png`.
- Props: `props/RuinGate_*.png`, `SealCrystal(_tints).png`, `Gate_IceBlock.png`, `Cutscene_Drill_Wreck.png`.
- FX: `fx/bossfx_field.png`, `bossfx_decals.png`, `bossfx_sheets.png`, `bossfx_icon.png`.

### build_all.py

Not edited here (shared). The shard climbs, OnBase_Loop and the scale fix live in scripts that are already listed
(round 28). Add these lines:

```
           # Round 29 (after the round-28 boss block): ruin gate, seal crystals, gate ice block, drill-rig wreck and
           # drained gate glow (needs build_gate.py, build_towers*.py and build_cutscene.py first).
           "build_bosses29_props.py",
           # Round 29 (after "build_cutscene_fx.py", "build_decor_c1.py"): boss ground decals, boss flipbooks and
           # the dampened HUD overlay.
           "build_bosses29_fx.py",
```

## Round 30: Frost Warden redesign, crack slabs, ice block, seal crystals (2026-11-05)

Brief: `Docs/MODEL_TODO.md` "Ronde 5 Unity" (FW1-FW7, GW1, IB1, SC1). Design notes and weak points in `DESIGN_NOTES.md`
(round 30). Nothing is imported into Unity.

**Scripts:**

- New:
  - `ice_kit.py`: `Facets`, `frost_ice`, `chunk`, `kite_blade`, `scale_builder`.
  - `build_bosses30_props.py`: GW1.
  - `round30_views.py`: QA turntable and game-camera views with a G1 gate.
- Changed:
  - `build_frostwarden.py`: body redesign.
  - `build_frostwarden_mech.py`: new armour, rest-pose holds.
  - `build_bosses29_props.py`: IB1 and SC1.
- The shared modules are untouched.

Run order is unchanged: `build_frostwarden.py`, then `build_frostwarden_mech.py`. Each takes about 5 and 3 minutes.

### Frost Warden (`FrostWarden.fbx` / `_LOD1.fbx`, same names, same rig layout)

- **Look:** a cold slate blue-grey body with ice-blue seams. Amber shows only in the eyes, the face slit and the chest
  seal glyph. Solid, faceted ice armour, a crown of ice spikes, the halo tilted back 46 degrees to face the game
  camera, and triangular-section glaives.
- **Scale:** the model and rig are **15 % larger** than round 29 (`SC = 1.15`). Keep Unity scale 1 and `TitanFactor`
  as it is to get the bigger Warden; size is now 3.6 x 5.7 x 5.7 m (w x l x h to the halo tips), shoulder 3.0 m.
- **Meshes:** `FrostWarden` (body + ice, ~24,000 tris, two materials as before) and `FrostWarden_IceArmour` (~3,800
  tris, LOD ~1,700, its 512/256 maps redone). 41 bones (40 LOD), same names.
- **Armour plate mapping changed** (the bones and their names are kept; some parents changed):

| Bone | Plate | Parent |
| --- | --- | --- |
| Armour_1 | chest (covers the lower half of the glyph) | Chest |
| Armour_2 / Armour_3 | left / right pauldron with three spikes | Chest |
| Armour_4 | back | Chest |
| Armour_5 / Armour_6 | left / right forearm | Arm_L_Fore / Arm_R_Fore |
| Armour_7 / Armour_8 | left / right front shin | Leg_L1_Lower / Leg_R1_Lower |
| Icicle_1..5 | rage icicles on the upper back between the shoulders | Chest |

- **Clips:** all old and round-28 clips are kept, with the same names, frame counts and event frames, except
  Shatter_Through's impact frame. Armour plates and icicles are keyed at scale 1 **and at their rest location and
  rotation** in Walk, WalkEnraged, Idle, Hit, Death, Enrage and FreezePulse. Before, Blender left them where
  Armour_Break had thrown them, and the FBX exporter can bake that. Only Armour_Break moves and hides them.
  `check_clip_scales.py` reports no off-scale `Armour_`/`Icicle_` bones outside Armour_Break.
- **Re-measured clip facts** (report `round28.clips`):

| Clip | Round 29 | Round 30 |
| --- | --- | --- |
| Walk / WalkEnraged distance per loop | 1.03 / 1.34 | 1.184 / 1.539 |
| Climb_Up / Climb_Down (ledge 1.5, unchanged) | face 1.5, end 4.02 fwd | face 1.657 ahead, end 4.617 fwd, +-1.5 |
| Dive_Into_Liquid / Surface_Burst | end 2.6 | end 2.6 (unchanged) |
| Submerged_Loop depth | 4.73 | 5.972 |
| Freeze_Gate | gate 2.4 ahead | gate **2.6** ahead (the glaive tips reach 2.05) |
| Shatter_Through | gate 2.4, impact 51, end 2.68 | gate **2.6**, impact **48**, end **3.078** |
| Armour_Break | plates 12-35 | unchanged |

- **Unity notes:**
  - Drop the round-5 `IceLook` override (one flat cold tint) for the ice slot and the armour. The baked maps carry no
    warm colour now; keep the cold rim and the faint glow (emission x ~2).
  - The rage icicles still scale 0.2 to 1.0 from their roots.
  - If anything placed the armour by plate index, check the new mapping above.

### GW1 Gravemaw crack slabs (`Exports/Gravemaw_CrackSlabs/`, `build_bosses30_props.py`)

- **Mesh:** `Gravemaw_CrackSlabs.fbx` holds four static objects `CrackSlab_A..D`: 250, 198, 308 and 144 tris, about
  0.4-0.6 m long, tilted 22-38 degrees. They share one material `Gravemaw_CrackSlabs_Mat` (256 maps, `Low/` 128).
- **Look:** the strip's cracked basalt, with molten glow on the raised edge, its underside and the seams near it.
- **Pivot:** the hinge, i.e. the low edge on the ground. The slab rises and glows towards Blender +X (the fissure
  side).
- **Use:** 3-6 per opened crack along both lips.
  - Yaw them so the raised edge faces the fissure; random pick, scale 0.8-1.3, sunk 0.03-0.08 m.
  - Show them as the crack opens, with emission about x3, cooling with the strip.
- **Run:** `blender --background --factory-startup --python Tools/Blender/build_bosses30_props.py -- [--preview
  <dir>] [--norender]` (about 1 minute). It needs `Exports/Boss_FX` only for the preview.

### IB1 ice block and SC1 seal crystals (`build_bosses29_props.py --only IceBlock,SealCrystal`)

- **`Gate_IceBlock`** (2,898 tris, same size and pivot):
  - The albedo alpha is now 0.25 in the clear middle of the big facets and up to 0.9 in fractures, facet ridges,
    up-face frost and icicles.
  - A frost lip runs along both long top edges, with seven icicles hanging from each.
  - Import as before.
- **`SealCrystal_A/B/C`** (2,247 / 1,250 / 1,700 tris, same sizes and pivots):
  - Flat-faced crystals with hard edges; A has two branch crystals and side crystals, B has side crystals.
  - The emission is still a white mask. The middle of every face glows a step brighter than its edges.

### Previews

In the session scratchpad, `blender30/`:

- `before/`: round-29 Warden turntable and game views, ice block and seal crystals.
- `after4/`: Warden turntable, close-ups and game views with a G1 gate.
- `mech4/`: clip strips and the armoured turntable.
- `fw4/`: body sheets and strips.
- `props*/`, `slabs3/`: prop previews.

### build_all.py

Not edited here (shared). Add this line after `"build_bosses29_props.py", "build_bosses29_fx.py",`:

```
           # Round 30: Gravemaw's crack slabs (GW1; imports build_bosses29_props, preview reads Exports/Boss_FX).
           "build_bosses30_props.py",
```

`ice_kit.py` is a module, and `round30_views.py` is QA only (do not list them).

## Round 31: Frost Warden concepts, slug strike, drill rig collapse (2026-11-05)

Brief: Remco's feedback of 2026-11-05 (the Warden reads as a confusing "ice scorpion", the slugs bob their whole
body to strike, the toppling drill rig looks silly). Design notes and weak points in `DESIGN_NOTES.md` (round 31).
Nothing is imported into Unity. Shared modules are untouched.

### Frost Warden concepts (`build_warden_concepts.py`, QA only)

Three body-plan blockouts for Remco to choose from. Nothing is exported and nothing replaces `FrostWarden.fbx`.

- Run: `blender --background --factory-startup --python Tools/Blender/build_warden_concepts.py -- --preview <dir>
  [--only A,B,C] [--nosheet] [--save]` (about 25 s, EEVEE).
- **Build:** each body is one Skin-modifier graph (spine plus limbs, subdivided, noise-displaced); ice armour is the
  convex hull of random points (hard facets). Procedural materials: slate body (light top, dark underside), ice with a
  deep core, frost on glancing faces and thin cyan cracks; amber only in the small eyes.
- **Poses:** `rest` and `cast` (frost cone from the breath source). `--save` writes
  `ArtSource/Blend/FrostWarden_Concepts.blend` (one collection per concept and pose).
- **Output:** `Warden_<X>_game.png` (3/4 front at a Gate_G2 and a Tower_Gun_T2, walking away, the cast, true play
  zoom), `Warden_<X>_turntable.png` (front 3/4, side, back, top), `warden_concepts_sheet.png` (labelled comparison).
- Scale: at Unity scale 1 Gate_G2 is 2.68 x 0.73 x 1.28 m, so a 5-6 m Warden dwarfs it (the true proportion).

| Concept | Body plan | Size (w x l x h) | Breath | Weapon / smash | Armour | Rage icicles |
| --- | --- | --- | --- | --- | --- | --- |
| A Rime Colossus | hunched knuckle-walker | 5.1 x 4.5 x 6.2 | maw under the low head, flank gills | two ice-gauntlet fists | gauntlets, pauldrons, back floes, forearm plates | crest on the hump |
| B Crown Floe-beast | broad low quadruped with a tail | 5.1 x 7.4 x 5.2 | throat slit, neck gills | crown-shield ram and two tusks | three back floes, flank floes, knee caps | 3 x 3 field between the floes |
| C Lantern Stilt-warden | high carapace on three stilts | 4.7 x 6.5 x 6.0 | ice lantern under the belly | one glaive arm in front | carapace floes, knee caps, greaves | hang from the hood rim |

### Slug strike (`build_slug_strike.py`)

The Oozer and the SlimeMonarch had no strike clip, so Unity bobbed the whole body (procedural Slam). Both now get a
`Strike` that stretches the body towards the target, and the SlimeMonarch's `Flood` is rebuilt the same way. No new
bones; mesh, maps and tris unchanged (Oozer 9 / LOD 7 bones, SlimeMonarch 13 / 11). The script opens the `.blend`,
keys the clips, keys LOD1 from the full rig, re-exports both FBX with all clips, merges `round31` into the report and
saves the `.blend` (idempotent).

| Asset | Clip | Frames | Event | Notes |
| --- | --- | --- | --- | --- |
| Oozer | Strike (new) | 24 | impact 10 | gather 0-4, lunge, mouth slaps down at 10 (tip 0.50 -> 0.88 ahead), elastic snap back ~10 % shorter, rest by 24 |
| SlimeMonarch | Strike (new) | 30 | impact 13 | the same strike 1.25x slower (tip 1.00 -> 1.77) |
| SlimeMonarch | Flood (rebuilt) | 44 | slam 24 (unchanged) | stretches up into a column (0-17), the front lands flat and long at 24, snaps back, ripples, rest by 44 |

- The tail end stays planted; Body_1..3 and Head lengthen by L (shares 0.1 / 0.45 / 0.62 / 0.3 of the reach), width
  L^-0.65, height L^-0.35. Eye stalks and sacs ride on the stretched bones unsquashed.
- Body_1..3 and Head now carry scale keys: every old clip holds them at 1 (`check_clip_scales.py --bones Body_,Head`).
  Unity must not set their scale from code.
- Unity: add `new BlenderClipSpec("Strike", ModelClipRole.Attack, 10)` (Oozer) and `(..., 13)` (SlimeMonarch); Flood
  stays `("Flood", ModelClipRole.Power, 24, "Pulse")`. Then zero the procedural pitch/lift/lunge/squash when the model
  has its own strike, or add a `Stretch` style that keeps Slam's dust and impact moments; `Bite` alone still crouches
  and shakes, and Slam at the 25 % own-strike share still adds a small hop.
- Run after `build_oozer.py` and `build_slimemonarch.py` (re-running those wipes Strike and the new Flood):
  `blender --background --factory-startup --python Tools/Blender/build_slug_strike.py -- [--only Oozer,SlimeMonarch]
  [--preview <dir>] [--norender] [--before]`. Skins are maps only (no rebuild).

### Drill rig collapse (`build_drill_collapse.py`; `build_bosses29_props.py --only DrillWreck` calls it)

`Cutscene_Drill_Wreck.fbx` is now rigged: the same pieces as `Cutscene_Drill`, cut at 0.85 (knees), 1.3 (start of
the middle) and 2.2 (top), one rigid bone per part. The **bind pose is the wreck**; `Collapse` frame 0 is the
standing rig (string sunk, as after Sink), and each part's last frame is generated as its wreck transform, so the clip
ends in the wreck by construction. No separate collapse export; `Cutscene_Drill` is unchanged.

| Asset | Tris / bones | Maps | Size (m) | Pivot |
| --- | --- | --- | --- | --- |
| `Cutscene_Drill_Wreck` | 5,120 / 30 (Root + 29) | 1024 (Low 512), one material | 8.17 x 5.35 x 2.82 | rig deck centre on the ground, as Cutscene_Drill |

| Clip | Frames | Loop | Events (report `clips.Collapse.events`) |
| --- | --- | --- | --- |
| Collapse | 84 | no | impact 3 (heave, crystals), string_thrown 4, legs_buckle 7, mid_tips 12, top_snaps 17, braces_fly 18-39, block_lands 28, kelly_lands 32, house_crushed 34, bit_lands 36, top_hits_ground 40, collapse_end 60 (holds to 84) |
| Standing | 1 | no | the standing rig |
| Wrecked | 1 | no | the wreck (bind pose) |

- Bones: Deck, Stub_1..4, Mid, Top, House (scale key: crushed), Roof, Stack, Block, Kelly, Bit, KellyBent, Heave,
  Rubble_1..3, Crest_1..3, Debris_1..8, all children of Root.
- The fall goes towards +X, turned 22 degrees towards the motor house; the middle tips 72 degrees and the top folds a
  further 46. The string lands on -X, the hole side.
- Round-29 wreck notes fixed: no cyan left, only dim ember slivers in the torn leg ends and the crushed house (keep
  WreckEmission ~0.6). The bit lies tip-down, buried to the shoulder, with a ring of cutter teeth and the lower kelly
  bent (no more torpedo).
- Unity: in `BlenderBossFxBuilder.ApplyWreck` pass roles `{"Collapse": ModelClipRole.Death}` to `BuildProp` instead of
  `null`; `ColonyRigView.ShowWreck` already plays Death from 0 at the smash. Dust at impact, sparks and a camera kick at
  top_hits_ground and house_crushed. Without the mapping Unity shows the wreck (the bind pose), as before.
- Frame 0 has no live cyan glow (the lit parts are baked dead): hidden in the impact dust; if it shows, fade the hidden
  drill's emission over 2-3 frames before the swap.

### Previews

In the session scratchpad, `blender31/`: `warden_concepts/` (sheet, game strips, turntables), `slugs/`
(`*_before_SlamBob`, `*_before_Crawl/Hit`, `*_Strike`, `SlimeMonarch_Flood`, `*_after_Crawl`, side and game),
`rig/` (`DrillCollapse_game/side.png`, `DrillWreck_after.png`, `before/drill_and_wreck_round29.png`). Backups of the
old slug blends and FBX in `blender31/slug_backup/`.

### build_all.py

Not edited here (shared). Add this line right after `"build_slimemonarch.py",` (it must run after
`build_oozer.py` and `build_slimemonarch.py`):

```
           # Round 31: the slugs strike by stretching (Strike clip; the SlimeMonarch's Flood rebuilt the same way).
           "build_slug_strike.py",
```

The drill collapse needs no line (`build_bosses29_props.py` is listed and calls `build_drill_collapse.py`, a
module). `build_warden_concepts.py` is QA only (do not list it).
