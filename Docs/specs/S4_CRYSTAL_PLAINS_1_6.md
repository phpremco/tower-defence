# S4 – Crystal Plains, levels 1–6 (spec)

Source: `Docs/STORY.md` (continent 1 table, dialogues, hero upgrades, element table), `Docs/STORY_ROADMAP.md` (S4, "Besloten"). Assumes F0, S1, S2 and S3 are merged, including their curator decisions: `CampaignLevelSpec` + `LevelSpecs.Crystal.cs`, the N-1 reward rule, the data-only `HeroPerkDefinition`, `LevelContext.InputBlocked`, `DialoguePlayer`/`LevelStoryListener`, stable line ids and `GlitchIntensity`. Ids never change; enums only get values appended.

## 1. Hero ability framework

- **`HeroAbilityKind`** (`Game.Hero`): `Shockwave = 0`, `GateRepair = 1`. Field Repair (S5) is appended later.
- **`HeroAbilityDefinition`** (SO, id = asset name, `Data/Hero/Abilities/`): `_displayName`, `_kind`, `_cooldown`, `_moneyCost`, `_damage`, `_radius`, `_fraction`, `_targets` (TargetLayers), `_requiresPerk` (HeroPerkDefinition, null = always), `_icon` (Sprite, optional; without one the UI uses its `IconFactory` icon for the kind), `_tint`, `_sound` (SoundSet), `_impactVfx` (ImpactVfxDefinition), `_description`. `OnValidate` checks that the kind has the values it needs.
- **`HeroDefinition`** loses the `_shockwave*`/`_repair*` fields and gains `_abilities : HeroAbilityDefinition[]` (slots 0 and 1) and `_perks : HeroPerkDefinition[]` (the perks this hero can earn). `_displayName = "Spencer"`; the asset stays `Commander`.
- **Runtime.** `HeroAbilityState` (pure C#: remaining cooldown, `Ready`, `Tick`). `Commander` exposes `Abilities`, `CanUse(slot)`, `TryUse(slot)` and dispatches through a `switch` on the kind in a static `HeroAbilityEffects`, so it stays IL2CPP-plain without reflection. `AbilityUsed(slot, definition, position)` replaces `ShockwaveReleased`.
- **Migration.** `Ability_Shockwave` (40 damage, 3.2 radius, 20 s, ground and air) and `Ability_GateRepair` (40 %, 25 s, costs 30) keep today's numbers exactly.
- **Input.** The actions `Shockwave`/`EmergencyRepair` in `GameInput.inputactions` become `HeroAbility1`/`HeroAbility2`, keeping their GUIDs and bindings (F / LT, G / RT). `IInputProvider` gains `event Action<int> HeroAbilityPressed` and drops the two fixed events in the same step (one implementation, no test fakes). `HeroController.OnAbility(slot)` pays only when `CanUse` and `!InputBlocked`.
- **HUD.** `LevelContext.HeroAbilityRequested : GameEvent<int>` replaces `HeroRepairRequested`. `HeroQuickBar` and `HeroPanelView` build one visible button per slot from data (name, key hint, cooldown, price), so mouse users click and gamepads use LT/RT. **Bug fixed here:** today the UI calls `TryShockwave()` directly, past the pause and outcome gates; every button raises the event instead.
- **Quick Reload (perk).** `HeroWeaponDefinition` gains `_magazine` (0 = no reload) and `_reloadSeconds`; the blaster gets 6 shots and 1.6 s. `HeroPerkDefinition` gains `_rangedIntervalMultiplier` and `_reloadMultiplier` (default 1; Quick Reload 0.85 and 0.5). A pure `HeroPerkModifiers.From(perks)` goes into `Commander.Initialize` like `PlayerModifiers`; `Commander` raises `Reloading(seconds)`.
- **Movement:** the hero stays on posts. Free movement is Remco's open question for continent 2 and is not built.

## 2. Towers 1–6

| Id | DisplayName | Unlock | Notes |
| --- | --- | --- | --- |
| PulseRifle | Gun Turret | start | unchanged, ground and air |
| Cannon | Cannon | start | unchanged |
| Laser | Laser Turret | reward of C1_01 | unchanged |
| Rocket | Flak Turret | reward of C1_02 | rebuilt as below |

**Flak rebuild** (air only, id `Rocket`):
- **New data:** `ProjectileStyle.Flak = 8`, `TowerShape.Flak = 13`, and `TowerDefinition._fuseRadius` (0 = impact fuse).
- **Fuse.** A pure `ProximityFuse.ShouldBurst(shell, target, aimPoint, fuse)` bursts the shell within the fuse distance of the target, or when it passes the aim point. The burst splashes the **Air layer only**.
- **Stats:** fuse 0.6, splash 1.1, shell speed 14 per level. Damage, range and interval are 14 / 5.5 / 0.55 at level 1, 22 / 5.9 / 0.5 at level 2 and 34 / 6.3 / 0.45 at level 3. Prices stay 120/140/200 in `BalanceSheet` until the ledger says otherwise. The old values live in `ProjectGenerator.Terrain.cs:116`; that code moves to `.Towers.cs`.
- **Unchanged:** stars, the three levels and the upgrades. The `Rocket` projectile style stays in code.
- **Not offered in L1–6:** hybrids, Support, Cryo, Tesla and Railgun. The debug "all weapons" setting still shows everything.
- **Result screen.** New strings `LevelReward.Credit` and `CampaignLevelSpec.Credit`; `ResultView` shows `Credit` when set and otherwise falls back to the `UnlockSource` text. Laser: "Rebuilt by Dr. Voss's lab". Flak: "Built on Dr. Voss's orders". Gadgets: "Crysium prototypes by Dr. Voss". Quick Reload: "Field upgrade from engineering".

## 3. Gadgets (reward of C1_05, debut in L6)

- **Land Mine** (`Mine`): the display name changes; the numbers stay (35 money, 1.4 radius, 140 damage).
- **Chrono Field** (`SlowField`):
  - The display name changes, and so does the description: time slows inside the field.
  - Tuning: slow factor 0.45, radius 1.6 (wide roads), lifetime 20 s, cost 45, ground only.
  - New look: `PathObjectLook.Chrono = 7`.
- **FirstUse.** S1 deferred it; S4 adds it. `LevelStoryListener` maps `PathObjectSpawned` to `FirstUse` with the definition as filter.

## 4. Enemies per level

**Fast teaser: the Roller.** Its roll (2.6) is about 3× the average walker, matching "three times the average"; the sudden roll reads as "what was that?!"; it has no armour; and its description, "slow it before it rolls", makes the Chrono Field the answer. The Prowler (rage on a pack mate's death) joins in L6 as the second fast type.

**No armour in L1–6:** Stinger, Bulwark, Shardback and the armoured bosses wait for the L11/12 armour story. **No flyers before L3** on any difficulty: air waves, per-difficulty air and the air sprinkle are empty in L1–2.

| Level | Waves | Troops (first wave) | Air | Teaser | Bosses (all / +Hard / +Psycho) |
| --- | --- | --- | --- | --- | --- |
| C1_01 | 16 | Skitter 1, Cragback 5 (rift nests) | none | – | CragElder 16 / BroodMother 10 / CragElder 8 |
| C1_02 | 18 | Skitter 1 (packs of 10), Wisp 6, Cragback 8 | none | – | BroodMother 18 / BroodMother 12 / CragElder 8 |
| C1_03 | 20 | Skitter 1, Cragback 4, Wisp 8, Skyray 14 | air waves 16, 19 (+1 per difficulty, all ≥14) | – | BroodMother 20 / CragElder 12 / BroodMother 9 |
| C1_04 | 20 | Skitter 1, Skyray 2, Shalebat 5 (rift nests), Wisp 7, Cragback 9 | regular | – | StormMatriarch 20 / BroodMother 13 / CragElder 7 |
| C1_05 | 22 | Skitter 1, Cragback 3, Skyray 4, Shalebat 6, Wisp 9 | regular | Roller ×2 at 17 and 20 | CragElder 22 / StormMatriarch 14 / BroodMother 8 |
| C1_06 | 22 | Roller 2, Skitter 1, Prowler 6 (pack 3), Shalebat 5, Skyray 8, Cragback 10 | regular | – | PackAlpha 22 / CragElder 15 / StormMatriarch 9 |

**Teaser rule** (`TeaserTests`): last third only, never on a boss wave, the same count on every difficulty, a species that is not among the level's troops, a `FirstSighting` dialogue, and killable with the roster the level owns. L6 must stay holdable on Easy **without** gadgets: they are the answer, not a gate.

## 5. Element Stone

- **Enums** (append only): `Element.Stone = 7`, `TerrainKind.Rift = 7`, `EnemyOrigin.Rift = 8`, `PropKind.Scree = 15`, and `EnemyShape.Cragback = 39` and `EnemyShape.CragElder = 40`.
- **Every switch on Bog gets a Stone/Rift case:** `LevelValidator.NestTerrain` (Stone → Rift), `EnemySpawner:172` (origin), `EnemyView:491` (death), `SfxPlayer:857`, `StructureModelFactory:334` (nest), `LevelVisualBuilder:334/362`, `TerrainMap.IsLiquid` (Rift is not buildable), `LiquidPools`, `TerrainLiquidBuilder` (level −0.35, no flow), `LiquidLifeBuilder`, `TerrainField`, `PropScatterBuilder` (rim: Boulder, Scree, warm Crystal), `MusicPlayer`, `MenuDiorama` (optional) and `Tools/PlayTest/zoom_liquid`. A test walks `Enum.GetValues` over them so a later element cannot be forgotten.
- **Terrain (curator default):** the rift, a liquid-like fissure that cannot be built on and serves as spawn terrain, written by the generator's `Terrain` share. The **stone nest** is a ring of cracked basalt plates with an orange seam.
- **Species: a new `Cragback`**, not a Bulwark or Shardback variant (both carry armour and their own skills): 160 HP, speed 0.6, **armour 0**, 4 base damage, a slam for 16 blockade damage, element Stone. **Why no armour yet:** the script reveals armour in L11 ("natural armour, formed from stone and crystal"), and in L12 Voss says "they have gained armour". So in L1–6 stone means *heavy and slow*, high HP that rewards the Cannon, and S5 gives the stone species its plates. That keeps the element table and the story both true.
- **Boss `CragElder`:** 900 HP, speed 0.4, the existing `Quake`, no armour; deliberately not the burrowing, crystal-armoured Gravemaw.
- **Stone bat `Shalebat`:** bat shape, 40 HP, speed 1.3, rises from rifts from L4.
- **Where stone appears** (Crystal Plains is mid-latitude and mixed):

  | Level | Rift share | Rift nests | Other element |
  | --- | --- | --- | --- |
  | L1 | 0.06 | 2 | – |
  | L2 | 0.03 | 1 | Wood 0.05 |
  | L3 | 0.03 | 1 | Sand 0.06 |
  | L4 | 0.07 | 3 | – |
  | L5 | 0.03 | 2 | Water 0.06 |
  | L6 | 0.04 | 1 | Wood 0.05 |

  The L1 rifts echo cutscene shot 8 ("creatures crawl out of cracks").
- **Audio:** `Voice_Cragback` (Shardback clips), `Voice_CragElder` (RollerKing and Shardback) and `Voice_Shalebat` (Dustbat), at a new `SoundSet._pitch` (default 1; the stand-ins use 0.85). Rift ambience `SfxLibrary.RiftLoops` uses the lava-bed loop at gain 0.08 and pitch 0.8. Remco's own sounds later replace them in data only; add the rows to `Docs/AUDIO.md`.

## 6. Dialogues

Speakers: `Speaker_Ora`, `Speaker_Voss`, `Speaker_Mercer`. Every dialogue is OncePerSave, and every line has `GlitchIntensity` 0. The ids follow S1 (`Dlg_L0n_*`, line ids `L0n_<Tag>_<k>`). Lines marked * are my own additions; the rest come from the script.

| Id | Trigger | Lines |
| --- | --- | --- |
| Dlg_L01_Start | LevelStart | ORA: "Hostile wave approaching. Place turrets along the path." / Spencer: "And whatever gets through is mine." |
| Dlg_L01_Wave2 | WaveStart 2 | (S1) ORA: "Second wave detected. Larger signature." / Spencer: "They're not testing us anymore." |
| Dlg_L01_Stone* | FirstSighting Cragback | ORA: "Heavy contact. Dense mineral hide, slow-moving." / Voss: "Use the cannon. Heavy shells for heavy hides." |
| Dlg_L01_Boss | BossSpawn | (S1) ORA: "Large contact on the main road." / Spencer: "That one's mine too. Keep the guns on it." |
| Dlg_L01_Won | LevelWon | (S1) ORA: "Perimeter holding. Wave repelled." / Spencer: "We woke something up down there." |
| Dlg_L02_Start | LevelStart | Voss: "We rebuilt the laser module. It fires fast, but light. Perfect against swarms." |
| Dlg_L02_Swarm* | WaveStart 6 | ORA: "Swarm signature. Estimated count: several hundred." / Spencer: "Light them up." |
| Dlg_L03_Start | LevelStart | Voss: "I had anti-air built. Just to be safe. You never know." |
| Dlg_L03_Air | FirstSighting Skyray | ORA: "Warning: air contacts." / Spencer: "They fly too?!" |
| Dlg_L03_Won | LevelWon | Spencer: "Doc, that 'just to be safe' just saved us. Thanks." / Voss: "Honestly, I was hoping I'd be wrong." |
| Dlg_L04_Start* | LevelStart | ORA: "Mixed signatures. Ground swarms and air contacts." / Spencer: "Guns low, flak high. Got it." |
| Dlg_L04_Boss* | BossSpawn StormMatriarch | ORA: "Large air contact. Smaller contacts detaching." |
| Dlg_L05_Fast | FirstSighting Roller | ORA: "Unknown signal. Speed: three times the average." / Spencer: "What was that?! I couldn't even track it." |
| Dlg_L06_Start | LevelStart | Voss: "The crystals never run dry. Never. This changes everything." / Voss: "For now: a mine with a serious blast, and a field that slows time around those fast little critters." |
| Dlg_L06_Mine* | FirstUse Mine | ORA: "Land Mine armed. Proximity trigger active." |
| Dlg_L06_Chrono* | FirstUse SlowField | ORA: "Chrono Field stable. Local time dilated." / Spencer: "Slow-motion bugs. I'll take it." |
| Dlg_L06_Fast* | WaveStart 6 | ORA: "Fast contacts inbound. Recommend a Chrono Field on the main road." |

The dialogues live in a new partial, `ProjectGenerator.Story.Crystal.cs`. The triggers go through the `CampaignLevelSpec.Story` tuples.

## 7. Look and animation

Each item has a view component and data (skins, `ImpactVfxDefinition`, tints), so it can be replaced later.

| Thing | Look and motion |
| --- | --- |
| Flak turret | Procedural `TowerShape.Flak` (Rocket's `_model` cleared): squat armoured ring with a gun shield and twin short barrels, light top and dark underside, stars as usual. Idle sweep pitched up 35°; barrels recoil in turn with a white-yellow muzzle star. |
| Flak shell | A white-hot tracer with an amber edge and no smoke, unlike the rocket's flame and smoke. `Vfx_FlakBurst` bursts at flyer height: 0.1 s amber flash, 10 shrapnel sparks, a grey-brown puff that drifts for 1.2 s; no ground ring, no shake. Sound: `FlakShot` (cannon clip, pitch 1.3) and `FlakBurst` (shell explosion, pitch 1.25, gain 0.35). |
| Cragback | Low, wide beetle-crab on six stubby legs, grey-ochre basalt plates (`CreatureSkin.Kind.Stone`) with warm orange veins glowing in the cracks (alien colour code), a brow plate and mandibles. Heavy gait: the body dips at each step, dust at the feet. Death: 5–7 tumbling chunks and dust; the veins fade over 0.6 s. |
| CragElder | 2.2× size, a ridge of orange crystal spires, brighter veins; Quake throws dust rings; existing boss direction cue. |
| Shalebat | Slate membranes with an ochre edge and vein glow; slower, heavier flaps; ground shadow. |
| Rift and nest | A sunken fissure with strata on the walls; veins at the bottom breathe slowly (7 s, no pulse); sparse dust motes; boulders, scree and warm crystals on the rims. A rift spawn splits the crack with a gravel spray and the creature claws its way up. |
| Shockwave and Gate Repair | The existing ring, tinted from the ability's data, plus a dust ring. Repair: a 2 s beam from Spencer's wrist to the gate, green weld sparks, hex panels that rise and fade. |
| Quick Reload | `BlasterRig` tips the pistol down, ejects a glowing cyan cell and clicks a new one in with a flash; with the perk it runs twice as fast with a yellow glint. |
| Chrono Field | Counter-rotating blue-white rings, particles drifting up in slow motion; enemies inside get the existing slowed tint. |

The Low profile halves the particles and turns off the dust motes.

## 8. Balance

- **Flak in the ledger.** The BalanceLab `GameData` reader takes Flak as an air-only splash tower; the fuse only makes sure the shell hits, so the ledger does not model it.
- **Hero gun in the ledger.** `HeroStats.GunInterval` becomes `(magazine × interval + reload) / magazine`. `Progression` applies Quick Reload from the reward of C1_04 through a small `HeroPerkStats`.
- **Waves** come from the solver; the C1_01–06 scripts live in `LevelSpecs.Crystal.cs`. Teasers stay inside the bands, and air waits for C1_03 (from wave 14).
- **Research and power** follow S3: a C1 budget of 748 RP with `MinResearchPerLevel`, about 6 RP for L1 and 7 for L6 on Easy.
- **Check:** `dotnet run --project Tools/BalanceLab -- solve` and `maps`, then `BalanceLedgerTests`; every wave inside its band on all four difficulties.
- **Probe bot:** `BalanceProbeTests` builds Flak when an air wave comes next and must clear C1_03 and C1_06 on Medium without gadgets.

## 9. Tests

- **EditMode:**
  - `HeroAbilityTests`: cooldown, money paid only when usable, slot mapping, the old numbers, blocked by `InputBlocked`.
  - `HeroPerkTests`: Quick Reload multiplies reload and interval; no perk gives the base values.
  - `ProximityFuseTests`: bursts within the fuse distance; the splash never hits ground.
  - `ElementCoverageTests`: every `Element`/`TerrainKind` value is handled.
  - `Continent1ContentTests`: only the four towers in L1–6, no armour, no air before L3, a FirstSighting per teaser, the display names, credits set.
  - `StoryDataTests` covers the new dialogues with unique line ids. In `HeroTests`, `TryEmergencyRepair` becomes `TryUse(1)`.
- **PlayMode:** `ContinentSimulation` Smoke on C1_01–06 (Easy); story smoke on C1_03 (Skyray, LevelWon) and C1_06 (FirstUse); the probe on C1_03 and C1_06; `ScreenshotTests` of a Flak burst, Cragback, a rift, Gate Repair and the HUD ability buttons; `LogAssert.NoUnexpectedReceived()`.

## 10. Build steps

| # | Step | Files |
| --- | --- | --- |
| 1 | Pure and data types (dotnet and EditMode; can run in parallel) | the enums, `HeroAbility*`, `HeroPerkModifiers`, `ProximityFuse`, balance `HeroPerkStats`, the C1_01–06 spec content |
| 2 | Hero runtime and input (one agent) | `IInputProvider`, `InputSystemProvider`, `GameInput.inputactions`, `LevelContext`, `HeroController`, `Commander`, `HeroView`, `BlasterRig`, `HeroQuickBar`, `HeroPanelView`, `ProjectGenerator.Hero.cs`, `SfxPlayer` |
| 3 | Flak | `Projectile`, `ProjectileView`, `ProjectilePool`, `TowerModelFactory`, `ProjectGenerator.Towers`/`.Terrain`/`.Vfx`, `SfxLibrary`, `BalanceSheet` |
| 4 | Stone | the switch cases in §5, `EnemyModelFactory`, `ProjectGenerator.Bestiary`/`.Audio`, `SoundSet._pitch` |
| 5 | Content | specs, dialogues, FirstUse hook, gadget names and tuning, `PathObjectView` Chrono, `LevelReward.Credit`, `ResultView` |
| 6 | Unity run | generate, EditMode, ledger, PlayMode Smoke and probe, screenshots; update `GAME_DESIGN`, `BALANCE`, `LEVEL_GUIDE`, `AUDIO`, `STATUS` |

**Collision hot spots** (one agent at a time):
- input and context: `IInputProvider`, `InputSystemProvider`, `GameInput.inputactions`, `LevelContext`;
- generator: `ProjectGenerator.Steps`/`.Context`, `.Bestiary.cs`, `LevelSpecs.Crystal.cs` (S3);
- story and rewards: `LevelReward`/`ResultView` (S3), `LevelStoryListener` (S1);
- large files: `SfxPlayer`, `HeroView`.

## 11. Open questions (defaults apply unless Remco objects)

1. **Stone means heavy but not armoured** in L1–6, and armour arrives in L11 as the script tells it. *Default: yes.*
2. **The Roller is the fast teaser** (L5), and the Prowler joins in L6. *Default: yes.*
3. **The lines marked * are proposals.** *Default: they ship as written.*
4. **Quick Reload adds a visible 6-shot magazine** to the pistol, so the perk has something to shorten. *Default: yes.*

---

## Critic review v1 and curator decisions (2026-11-03) — override everything above

1. **Stone in L1–6:** heavy and slow, no armour. Armour plates arrive in S5 (level 11), as the script reveals.
2. **Quick Reload must not nerf Spencer.** Without the perk, the magazine and reload keep DPS equal to today (base fire interval 0.35 s → effective 0.35 s including reload). Shorten the shot interval so the reload pause is paid for. The perk then gives +25–30 % DPS. Add an EditMode test on effective DPS.
3. **Roller teaser:** the teaser tuning gives an effective average speed of at least 2.5× Skitter (e.g. roll 3.5×, walk 1.5 s). Rewrite its Description (no "armoured"). Add a test on effective speed.
4. **FirstUse:** S4 appends `StoryTriggerKind.FirstUse = 6` and a `PathObjectDefinition` filter on `StoryTrigger`.
5. **Order:** S1 fully merged → S3 (pure layer + generator + C1_01–06 layouts; S3 leaves Stone OUT of its element list) → S4. S4 step 1 appends `Element.Stone`/`TerrainKind.Rift` and then adds Stone terrain to the C1 specs that use it. S4 edits S3's `LevelSpecs.Crystal.cs` content (species, unlocks, dialogue ids); S3 owns the layout fields.
6. **Code facts:** the Rocket/Flak stats live in `ProjectGenerator.Towers.cs:16-34` (set `ClearModel` instead of `satelliteDish_large`; `Terrain.cs:116` only sets the projectile style). Shell speed is per tower. `SoundSet` gets a new `_pitch`, applied in `SfxPlayer` (play and length maths), `LoopVoice` and `CreatureChorus`. `VfxLibrary` gets a Flak entry.
7. **Newcomer pacing:** L1–2 have 10–12 waves on Easy, L3–6 14–18. The L1 boss is BroodMother (existing), not a new boss.
8. **Flak identity:** Flak targets Air only; ground-walking Wisps are not valid Flak targets (exclude Ground|Air hybrids while grounded). L3–4 air waves come in packed Skyray/bat groups, so splash is clearly the answer. Add an Easy probe with a naive Gun-only build that must struggle in L3–4 air waves.
9. **L6:** the "Fast contacts inbound" line fires on the first wave that contains Rollers/Prowlers (FirstSighting), not a fixed wave number.
10. **Palette:** a palette table separates warm species by brightness and hue. Cragback is dark basalt with orange crystal veins; Roller is bright sand; Skitter is orange-red; Prowler is rust. Take a screenshot of all L1–6 creatures side by side at gameplay zoom.
11. **Cragback silhouette:** a hunched boulder/tortoise shape, distinct from Shardback, built with `PropMeshBuilder`. No bare primitives.
12. **Scope:** Cragback + Shalebat (via the `Bat(...)` helper). CragElder is NOT built in S4. Defer the gate-repair hex panels and the eject animation.
13. **Flak sound:** borrow a different clip than the Cannon (e.g. a short burst or crackle), pitched via `_pitch`.

---

## Builder notes (S4 build, 2026-11-03)

Choices made while building, at curator level, for Remco or the curator to overrule:

1. **Quick Reload numbers (decision 2).** The blaster holds 8 shots and reloads in 1.2 s; its shot interval is
   0.2 s, so a full cycle is (8 x 0.2 + 1.2) / 8 = 0.35 s a shot: the old pace exactly. Quick Reload halves the reload
   (`_reloadMultiplier` 0.5, `_rangedIntervalMultiplier` 1): 0.275 s a shot, +27% damage per second. The spec's
   6 shots / 1.6 s would have needed a 0.083 s shot interval (a machine gun), so it was not used. Tested in
   `HeroPerkTests.QuickReload_KeepsTheBaseDpsAndAddsAQuarter`. The ledger models the cycle and applies the perk from the
   reward of C1_04 on (`HeroStats.EffectiveGunInterval`, `CampaignLevel.RewardHeroPerkId`).
2. **Ability gate in one place.** Every way to use an ability (F/G, LT/RT, the quick bar, the hero panel) raises
   `LevelContext.HeroAbilityRequested`; `HeroController.TryAbility` checks the outcome and `InputBlocked`, readiness
   and the price, and pays only when the ability fires (EditMode-tested through that static).
3. **Ability slots.** `Ability_Shockwave` (slot 0) and `Ability_GateRepair` (slot 1, 30 money) keep the old numbers.
   A slot whose `_requiresPerk` is not unlocked stays empty. Repair look: a 2 s welding beam from Spencer to the gate
   with green sparks (`HeroAbilityView`); shockwave adds a dust ring. Hex panels and the cell-eject animation are
   deferred (decision 12); the reload shows a short cyan glint at the gun, yellow with Quick Reload.
4. **Air-only rule.** `TargetLayersExtensions.CanHit`: a weapon that aims only at the air never hits a creature that
   also has the ground layer (the Wisp on the road). This applies to the Flak and the air net alike.
5. **Flak.** Fuse 0.6, splash 1.1, shell speed 14, damage/range/interval 14/5.5/0.55, 22/5.9/0.5, 34/6.3/0.45,
   prices unchanged. Shot sound: Kenney `impactMetal` at pitch 1.3; burst: Kenney `explosionCrunch` at pitch 1.25
   (neither is the cannon's or the rocket's clip). `Vfx_FlakBurst`: 0.1 s flash, 10 sparks, 3 puffs, no ring, no shake.
6. **Packed air raids.** `WaveScriptSpec.FlyerInterval` (default 1.6 s); C1_03 and C1_04 use 0.45 s, C1_05/06 0.8 s.
7. **Roller teaser (decision 3).** Speed 1.4, walk 1 s, roll 3 s at x3.6: 4.13 average, 2.58x the Skitter (1.6).
   `EnemyDefinition.AverageSpeed` feeds the ledger for rollers. Description rewritten without armour. Colour bright sand.
8. **Bosses without the CragElder (decision 12).** L1 BroodMother (all) / BroodMother (Hard) / BroodMother (Psycho);
   L2 BroodMother / CoreWisp / BroodMother; L3 BroodMother / CoreWisp / BroodMother; L4 StormMatriarch / BroodMother /
   CoreWisp; L5 CoreWisp / StormMatriarch / BroodMother; L6 PackAlpha / CoreWisp / StormMatriarch. All unarmoured.
9. **Wave counts (decision 7).** 12, 12, 16, 16, 17, 18. Final budgets lowered with the shorter levels
   (95, 115, 150, 165, 185, 210). Troops follow the spec table minus armour: no Stinger, Bulwark, Oozer, Tidebat,
   Mossbat, Dustbat or Emberbat in L1 to 6. Cragbacks come out of the rifts in L1 and through the gate after that;
   Shalebats rise from the rifts in L4 to L6.
10. **Layouts.** Stone shares and rift nests as in the spec table. "Wood 0.05" cannot be a terrain share (wood has no
    ground of its own), so L2 and L6 have no wood. L3 dropped its water, L4 its water and wood nests (it is now the
    stone level, renamed **Fault Line**), L5 swapped sand for water. Names, areas and briefings stay proposals.
11. **Palette table.**

    | Level | Tint | Read |
    | --- | --- | --- |
    | C1_01 Landing Site | Crystal | grey-blue stone, violet sky, cyan crystal |
    | C1_02 Glass Meadows | Wood | teal ground, green-cyan accents, forest ambience |
    | C1_03 Shard Ridge | Sand | warm grey rock, pale sand, lilac accents |
    | C1_04 Fault Line | Stone (new) | grey-ochre basalt, amber rift glow |
    | C1_05 Quartz Flats | Water | blue ground and water |
    | C1_06 Crysium Vein | Vein (new) | dark blue ground, bright cyan veins, more bloom |

    Species: Skitter orange-red (1, 0.5, 0.14); Prowler rust (0.8, 0.28, 0.15); Roller bright sand (0.95, 0.8, 0.48);
    Cragback dark basalt (0.4, 0.36, 0.32) with amber veins (1, 0.55, 0.2); Shalebat slate (0.38, 0.4, 0.46) with an
    ochre edge; Wisp pale blue; Skyray deep blue.
12. **Rift.** A liquid-like terrain kind: not buildable, spawn terrain, surface at -0.35 over a 0.55-deep fissure.
    Floor: matte basalt (`LiquidTextures.RiftRock`) with an emissive vein map (`RiftVeins`) in `VisualTheme._riftColor`,
    breathing once every 7 s without flicker (`LiquidFlow.SetPulse(..., flicker: 0)`); painter: rubble rim and strata
    bands on the walls; sparse dust motes (High profile only); rim props scree and boulders, one cell in four a crystal.
    Stone nest: a ring of tilted basalt plates around an amber seam. Rift ambience: the lava bed at pitch 0.8.
13. **Cragback look.** A procedural boulder on six stubby legs (PropMeshBuilder): overlapping basalt plates, amber
    cracks, a brow plate over a low head with three eyes in a vertical slit, sideways jaws, shoulder spurs and warm
    crystal spires from the back. The heavy dip in the gait, foot dust and the fading veins on death are not built.
    Stand-in voices: ice beetle and heavy steps at pitch 0.85 (Cragback), dust bat at pitch 0.85 (Shalebat).
14. **Credits.** `LevelReward.Credit` shows under the weapon heading, or at the end of the unlock line for gadgets
    and perks. Rewards keep `UnlockSource.HomePlanetIntel` underneath.
15. **Dialogues.** As in section 6; L6 "Fast contacts" fires on the first Roller or Prowler (decision 9). Line ids
    follow the S1 convention `Dlg_X.NN`.
16. **Test fixture.** `RegionUnlockTests` and `CampaignTests` spread their fake sites (the ~1,770 spacing warnings).

---

## Curator decisions after balance (2026-11-04)

1. **The gun-only struggle check applies on Medium and up, not Easy** (narrows decision 8). Easy stays forgiving and has
   no struggle requirement. "Struggles" means the gun-only build takes more base damage, or more flyers reach the base,
   than the same bot with a Flak; it does not have to lose. The Flak bot must win. Test:
   `BalanceProbeTests.Medium_GunOnlyBot_StrugglesInThePackedAirRaids` (Shard Ridge C1_03 and Fault Line C1_04).
2. **Builder outcome.** A new wave-script field `WaveScriptSpec.AirRaidScale` (per difficulty, default 1) makes an air
   raid bigger: the flyer budget and the solver's target for that raid grow together, so there are more flyers of about
   the same toughness; a difficulty whose scale differs from Medium's has its raids solved on its own. Shard Ridge uses
   `{ 1, 1.6 }` (Easy keeps its raid size, now solved on its own Easy target; Medium and up 1.6x). Fault Line keeps its raids: its road joins the trunk late,
   so every gun stands near the base where the flight routes meet, and a gun-only bot downs every flyer with the base
   untouched at 1x, 1.6x and 1.8x the raid. The test asserts the struggle on Shard Ridge only and the Flak bot's win on
   both. For the curator: making Fault Line's air matter needs a layout or flight-route change (flyers that reach the
   base away from the trunk), not bigger raids.
3. **Open (balance):** the Flak-building probe bot loses Fault Line on Medium in 3 of 4 runs (wave 8 to 10, 2 to 5
   towers, overrun by Skitters on the ground), also with the raids unchanged; it had never been run there before (the
   old Easy check stopped at Shard Ridge). The gun-only bot wins the same level untouched with cheap Gun Turrets, so
   the bot's early Flak-plus-Laser opening is the weak point, not the air raids. Until that is settled the Medium air
   check fails on its Fault Line "Flak bot must hold" assert.
4. **Settled (2026-11-04, probe bot opening):** the bot was fixed, not the level (see STATUS "Probe bot opening"). It
   now judges its own towers with the ledger, holds the ground first, and saves for the Flak from the wave whose income
   still pays for it before the first flyer wave. Fault Line on Medium: Flak bot won 4/4, base untouched (it was lost
   in 3 or 4 of 4). Fault Line keeps no struggle requirement; the test checks the Flak bot's win there.
5. **Shard Ridge `AirRaidScale` `{ 1, 1.3 }`** (was `{ 1, 1.6 }`), the low end of the curator's 1.3 to 1.6 range. With
   the new bot, 1.6 lost the gun-only bot every run (4 of 17 flyers down, base 800 lost) and 1.3 is the only value in
   the range where it does not: over four Medium runs the gun-only bot lost twice (14 and 13 of 15 Skyrays down,
   base 800 lost at the boss wave) and won twice (base damage 18 and 786; 15 of 15 down, but the raid reached the
   base first), while the Flak bot won all four with 15 of 15 down and the base untouched. One Skyray that gets
   through and parks on the base is enough to destroy it (nothing at the base aims up), so the gun-only outcome is
   close to all-or-nothing; a softer "struggle" would need an anti-air answer at the base, not a smaller raid.
   Medium raid air health w12/w15: 631/1096 at 1.6 -> 438/868 at 1.3, air pressure 0.96/0.93 -> 0.70/0.83 (band
   1.16); 18 -> 15 flyers over the level. Every wave inside its band (Unity `BalanceLedgerTests`, BalanceLab `report`).
