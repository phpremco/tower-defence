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
