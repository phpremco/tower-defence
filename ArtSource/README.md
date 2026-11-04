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
| Cutscene_Mothership | `Cutscene_Mothership.fbx` | 9,302 (budget 15,000) | 1024 (+ 512) | 2 bones: Root, Ring. Spin 360 (loop). Empties `Bay`, `Nozzle_1..3` |
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
