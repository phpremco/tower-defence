# Design notes: round 2 of the Blender pilot

Brief (Remco, 2026-11-03): the ideas may start from Earth creatures, but the result must be different, distinct and
dangerous, or you do not believe you are on another planet. The same goes for the crystals.

Round-1 verdict: the Skitter read as an orange Earth ant (round head, elbowed antennae, bead waist, striped gaster),
the Queen as an Earth spider (eight-ish radial legs, round abdomen) and the crystals as textbook hexagonal quartz.
Every surface was smooth and glossy, like moulded plastic.

Rules for every asset below:

- Read at game size first: a fixed orthographic camera at about 44 degrees, a Skitter is about 20 pixels long. Push
  two or three big silhouette features instead of fine detail, and keep a strong value contrast: a dark body, hot
  glowing accents.
- Dangerous: blades, hooks and serrations in the silhouette, a low, forward-leaning stance.
- Surfaces are matte and layered: rough chitin (0.6 to 0.8), a sheen only on raised rims and ridges (the bake lowers
  roughness where the source surface is convex), pitting and scratches in the normal map, dusty undersides.
- Colour codes stay: Burrowers are warm and organic (Skitter orange with lime venom, Queen crimson with pink glow),
  Crysium is icy white to pink.

## Skitter (swarm runner)

**Earth starting point:** an ant (head, thorax, waist, gaster, six legs, antennae).

**Alien departures:**

1. **Four walking legs plus two blade fore-limbs.** The front pair is not a leg: it is a raised two-joint arm that
   ends in a long serrated blade, held forward past the head like a pair of lances. Seen from above the Skitter is an
   arrowhead with two prongs, not an ant. The walking legs end in spikes, have a kink like an extra backward joint and
   a hooked spur at the knee.
2. **A head that opens in four.** No round head, no eyes, no antennae. A low wedge sits under a shovel crest, and the
   mouth is four hooked jaws in an X that splay apart (idle, hit, death).
3. **Eyeless sensor slits.** Three glowing slits on each side of the wedge replace the eyes.
4. **No waist.** Thorax and abdomen form one low, rising line. The abdomen is a flat sac of shingled plates with
   lime venom glowing through the membrane between them; the plates grow into backward hooks along the spine and the
   flanks.
5. **Crysium fused into the chitin.** Two small pink-white crystal shards grow out of the back: the Burrowers live in
   the Crysium layer.

Stance: head and blades low, back and abdomen rising towards the rear, legs spread wide: about to lunge.

## Queen (boss)

**Earth starting point:** an ant queen with a spider's long legs (round 1).

**Alien departures:**

1. **Upright forebody.** The front half rears up almost vertically above the legs, like a centaur, so the boss
   towers over the field and shows its face to the camera.
2. **Four huge walking legs and two scythes.** The legs are armoured stilts with a kinked lower segment, hooked
   knee spurs of Crysium and spike feet. The raised scythe arms carry a row of barbs on the inner edge.
3. **A head that splits in four.** Four petal jaws close into a beak and open into an X around a glowing throat
   (Scream). Clusters of small glowing eyes sit at the base of the petals.
4. **Crown fused into the body.** The crown is twisted Crysium shards growing out of the back of the head and down
   the ridge of the raised forebody, in the same helical language as the level crystals.
5. **Egg sac dragged behind.** The swollen abdomen hangs low behind the legs, a translucent membrane over glowing
   eggs, armoured on top with ribs that grow into rows of hooks.

## Crysium crystals (level props)

**Earth starting point:** quartz clusters (hexagonal prisms with pyramid tips on a rock).

**Alien departures:**

1. **Fluted, twisted shards.** Star-shaped (fluted) cross-sections twisted along the length, so the ridges spiral.
   No hexagonal prisms, no flat pyramid tips: the shards thin into needle points.
2. **Hovering shards.** Crystal_B is a double-pointed monolith that floats above its base with a glowing gap and a
   pool of light on the rock below, ringed by small shards.
3. **Organic growth.** Crystal_C grows from a dark mineral stalk that forks twice, like coral, and every branch ends
   in a shard.
4. **Inner light.** The glow sits in the flutes (light leaking from inside) and in bright node rings along each
   shard, with darker violet roots. The facets themselves stay a step darker so the shape reads.
5. **Palette:** deep violet roots, pink body, icy-white tips.

Rocks keep their hewn shape, but their veins are Crysium (pink-white instead of cyan), and two rocks carry small
fused shards. The burrow becomes a broken crater with curved hooked chitin ribs curling over a warm glowing pit and
a few shards cracked loose at the rim.

# Round 4: Roller, Cragback, Prowler (2026-11-03)

Same rules as above, plus round 3's surface standard (matte layered chitin or stone, micro-detail baked into the
maps, nothing fine in the geometry). Each keeps its gameplay role and its colour code from
`ProjectGenerator.Bestiary` (Roller sand with amber glow, Cragback grey-brown stone with orange veins and amber eyes,
Prowler red-orange with red sensors and orange accents).

## Roller (fastest enemy: walks, then curls into a ball and rolls)

**Earth starting point:** an armadillo / pill bug (a row of shell bands that close into a ball).

**Alien departures:**

1. **The bands are built as a sphere first.** Eight lune-shaped plates are modelled as slices of the rolled ball and
   then opened out into the walking dome, so the curl is exact: rolled, the plates close into a faceted, stepped
   sphere (each plate's rear rim stands proud of the next one), not a smooth ball.
2. **A buzzsaw keel.** Every plate carries a raised keel with a backward hook, and two smaller rows of hooks run down
   the flanks. Walking they read as a serrated ridge; rolled they become three spinning saw rings around the ball.
3. **Glowing hub eyes.** Each flank has a round boss with a ring of amber light where all the plate tips meet when
   it rolls, so the ball reads as a wheel with spokes of glowing seams; the seams between the plates open and spin.
4. **A digger, not a grazer.** No snout or ears: the front plate ends in a toothed scoop lip over an eyeless wedge
   head with two sideways-closing hooked mandibles and rows of glowing sensor pits. The front legs end in three
   hooked digging claws, the rear legs in single spikes.
5. **Tail shield with two prongs** that close against the scoop lip when it rolls.

Stance: low, head down under the scoop, rear plates high: it looks like it is about to launch. Colour: pale sand
plates with dark umber bands and markings, near-black seams and belly, amber glow (seams, hubs, sensor pits).

## Cragback (Stone element: heavy, slow, very tough)

**Earth starting point:** a hunched boulder on legs (a tortoise or rhino-sized beetle made of basalt).

**Alien departures:**

1. **Tectonic plates.** The back is five separate slabs of hewn stone on their own bones; they grind and lift
   against each other on every step, and the gaps between them glow orange like a lava fault.
2. **No head, a maw.** The front is one huge brow slab (the battering ram that slams gates) over a horizontal crack:
   a stone lower jaw drops to show a glowing throat lined with stone teeth. Three amber eyes sit in a vertical slit
   in the brow.
3. **Crysium grown through the rock.** Fluted, twisted shards in an amber "heated" Crysium push out of the faults on
   the back, leaning backwards like the spines of a crest.
4. **Pillar legs.** Six short columnar legs of stacked basalt (hewn facets, not tubes) end in flat splayed toe
   slabs; the weight shows in a ground-shaking gait with a body dip on every footfall.
5. **Stalactite belly.** Short stone spikes hang under the belly and the brow.

Death: the veins fade (emission fades in Unity, see `ArtSource/README.md`) while it slumps and its plates drop.
Colour: grey-brown stone, lighter on top, near-black in the faults, orange vein glow, amber eyes.

## Prowler (fast pack hunter on four legs)

**Earth starting point:** a wolf.

**Alien departures:**

1. **A split muzzle.** The long low head has no eyes and no nose: the muzzle is two hooked jaw halves that open
   sideways like a pair of shears around a glowing throat. Red sensor slits run back along both sides of the head.
2. **A spined back instead of fur.** A crest of tall, backward-curving spines runs from the neck to the hips, the
   longest over the shoulders, with glowing tips (they ride on the chest and hip bones; no bones of their own).
3. **Rib armour.** The flanks are open armour ribs over a dark body; the gaps glow orange (the pack's colour code
   that the old flank stripes carried).
4. **Reversed hind ankles and hooked toes.** All four legs end in two hooked toes on a long raised foot (no paws);
   the hind legs have a high reverse ankle, the forelegs a blade spur at the wrist.
5. **A segmented whip tail** ending in a forked blade, held low and straight out as a counterweight.

Gait (Remco 2026-11-02: "a pack should run"): low and flat, long stride, little bounce, a forward lean, spine
flexing between chest and hips, head held level and low. Colour: red-orange plates, dark umber body and saddle,
pale bone tips, red sensors, orange rib glow.

# Round 5: Skyray, Shalebat, Wisp (P1 air units, 2026-11-03)

Same rules as rounds 2 to 4, plus three that only flyers have:

- **Seen from above.** Flyers cruise at about 1.6 units over the road, so the player sees their back: the
  silhouette and the markings on top carry the read. Every flyer gets a wide, flat outline, bold pale bands across
  dark wings and a glowing leading edge, so the beat or the wave shows as a moving light line at 20 px.
- **Readable underside.** The belly is a step lighter than the back and carries its own glow (gill slits, throat,
  heart), so a banking or falling flyer still reads; the ground shadow comes from Unity.
- **Motion in the bones.** Rigid fins on a hinge read as static (2026-11-02 feedback). The wave and the flap are
  baked into bone chains (span and chord), so Unity's shader wave can be switched off or kept very light.

Colours follow `ProjectGenerator.Bestiary`. Two of the three are cool in the data (Skyray blue with cyan glow, Wisp
pale ice-blue), which goes against "aliens are warm". The models keep the data colours so the species stay
readable, but the Skyray's back gets a violet flush and the Wisp's organism inside is dark umber-violet flesh
(warm, organic) around the cool light. To decide with Remco: keep, or move both to warm palettes.

## Skyray (flying melee: lashes gates with its tail)

**Earth starting point:** a manta / eagle ray.

**Alien departures:**

1. **A ram prow with pincers instead of a ray's head.** No cephalic lobes and no mouth in front: a pointed chitin
   ram juts forward and two hooked pincers curl in ahead of it. Six small cyan eye pits sit in two rows on top of
   the head, facing up at the player.
2. **Fins with a skeleton.** Each fin is a membrane stretched over a fan of raised chitin rays, with a glowing
   cyan leading edge, three pale bone chevrons across the dark top, a serrated trailing edge with hooked points
   and a hooked claw curling down at each wingtip.
3. **Dorsal hooks and jet spiracles.** A ridge of backward hooks runs down the spine, growing towards the tail,
   with two pairs of glowing vents between them (it jets as much as it swims through the air).
4. **A trident whip.** The tail is twice an Earth ray's: segmented, barbed along its length, and it ends in a
   three-pronged glowing stinger, the weapon it lashes gates with.
5. **A lit belly.** Pale underside with two chevrons of glowing gill slits, so a banking ray flashes light.

Motion: Fly is a slow travelling wave (front to back and root to tip, about 1.4 s per beat), the body heaving
against it and the tail trailing in an S. Dive pitches the nose down, sweeps the fins back and whips the tail over.

## Shalebat (Stone element bat: spits bolts at towers)

**Earth starting point:** a bat.

**Alien departures:**

1. **Wings of stone blades.** No finger bones: three long, flat shale blades fan out from a boulder wrist, with a
   thin dark stone skin between them, crazed with glowing amber veins. The trailing edge hangs in scallops between
   the blade tips, so the outline still says "bat" at 20 px while the close-up says "rock".
2. **A geode back.** The small body is a split stone shell with a cluster of ember Crysium shards growing out of
   the crack (the stone species carry heated Crysium, like the Cragback).
3. **No ears, no face.** A blunt stacked-slab wedge with a spike for a nose, two clusters of three amber eyes on
   the sides, two stone horns swept back where the ears would be, and a lower jaw that drops to show the glowing
   throat it spits from.
4. **Crystal thumb claws.** A hooked ember Crysium claw juts forward from each wrist.
5. **Talons and a blade tail.** Two hooked stone talons tucked under the belly and a short spiked tail ending in a
   slate blade.

Motion: a heavy flap (0.53 s, slower than the element bats' 0.42 s because it is stone), the blades folding on the
upstroke; Glide holds the wings out with a slight dihedral and small corrections.

## Wisp (energy creature: walks the road, can land; hit by ground and air guns)

**Earth starting point:** a comb jelly (a glowing ball that drifts, with rows of light).

**Alien departures:**

1. **A solid organism inside the light.** The core is a bud of four dark chitin petals closed around a white-hot
   heart; the light leaks out of the seams between the petals and from a slit at the top. When it is hit the
   petals flinch open; when it dies they burst open.
2. **Rings that are part of its body.** Two gyroscope rings turn around the bud: an inner ring of linked vertebra
   beads with glowing joints, an outer band with six outward hooks and three glowing nodes. They spin in
   different planes and lie down flat around it when it lands.
3. **Three tendril legs.** Hooked, glowing-tipped tendrils trail under it in flight; to land they reach down and
   plant as a tripod, so a grounded Wisp stands on the road like a ground unit.
4. **Very little mesh, a lot of light.** About 2,400 triangles; the shape is in the dark petals and rings, the
   rest is emission and Unity VFX.

Unity VFX the model expects (not in the mesh): an additive halo billboard on the heart in the species colour, a
point light or ground light pool (exists), short arcs between the heart and the ring nodes, motes orbiting with the
rings, a dust ring and ground glow on Land, and the Burst flash and sparks on Death (exists).

# Round 6: P1 level bosses (BroodMother, PackAlpha, StormMatriarch, CoreWisp, 2026-11-03)

Same rules as rounds 2 to 5, plus four that only bosses have (Remco: bosses are big, glowing and slow, and show
clearly which way they are going; he wants a readable head and weapon on a boss, not "a big ant"):

- **Kin, not a scaled copy.** Each boss keeps its species' signature (the Skitter's blades and X-jaws, the
  Prowler's shears and spine crest, the Skyray's ram prow and trident, the Wisp's petal bud and rings), so the
  player knows at once whose mother or leader it is, but its proportions change: heavier, older, more armour.
- **A crown.** Every boss carries a crown of Crysium or bone that no common enemy has, in the species' glow colour.
- **The power is in the body.** The boss's power has an organ you can see (brood sacs, a howl collar, brood hatches,
  a heart with a shock ring) and a clip that uses it, so the VFX in Unity has something to come out of.
- **Direction at 20 px.** Bright, hot features at the front (face, weapon, crown edge), dark mass and trailing parts
  at the back, and a pale or glowing chevron on the back that points forward. A boss reads as an arrowhead or a
  comet from the game camera, never as a round blob.

Sizes: built so that at Unity scale = `EnemyDefinition.Scale` x `BossScaleFactor` 0.6 they are as big next to their
species as the procedural bosses (BroodMother about 3.2 x a Skitter, PackAlpha 2.1 x a Prowler, StormMatriarch
1.9 x a Skyray, CoreWisp 2.9 x a Wisp).

## BroodMother (Skitter boss, boss of level 1: endless brood and a venom burst)

**Starting point:** the Skitter, swollen into a breeding queen (an Earth termite queen is the far starting point).

1. **The Skitter's weapons, grown old.** Four walking legs and two raised fore-limbs with serrated blades, the X of
   four hooked jaws and the sensor slits all stay, but the blades are long sickles with a row of barbs and the legs
   are thick, armoured columns with knee spurs: she is slow because she is heavy.
2. **A crowned crest.** The shovel crest becomes a broad hood with backward teeth, and a crown of five twisted
   Crysium shards grows from its back edge, pointing up and back (the arrowhead's barbs from above). A pale bone
   chevron runs along the crest's ridge, pointing forward.
3. **Brood sacs.** Behind the legs the abdomen is a huge three-lobed brood sac dragged low: a translucent membrane
   with amber-lime eggs glowing through it, armoured on top with shingled plates that grow into hooks. Two lateral
   sacs hang off its flanks on their own bones; they swell and squeeze (bone scale) in the Spawn clip.
4. **Birth vent.** The sac ends in a ringed, glowing ovipositor vent with hooks around it, where broodlings come out.
5. **Glow.** Lime venom in the membranes and jaws (the Skitter's colour code), amber eggs (the brood ability's
   colour), hot pale-yellow sensor slits and throat at the front.

Clips: Walk (slow, heavy, the sac swaying), Idle (sacs breathing, jaws snapping), Hit, Death (rolls on her side, the
sac sags), **Spawn** (she braces, the sacs squeeze twice, the vent lowers to the ground and opens: broodlings at
the release frame).

## PackAlpha (Prowler boss: the howl that sends the pack running)

**Starting point:** the Prowler as an old pack leader (an Earth wolf alpha is the far starting point).

1. **Front-heavy build.** Massive armoured shoulders and a deep rib cage over relatively narrow hips, the head
   held low: it looks like it charges through things.
2. **A mane of spines.** The spine crest becomes a collar: two fans of long, backward-sweeping spines on their own
   bones flare out around the neck (behind the head) plus a double row down the back. On the Howl the collar
   rises and flares wide like a frill; the tips glow orange.
3. **Bigger shears.** The muzzle shears are longer and broader, with a row of teeth and a second, short inner pair
   of hooks; the throat glows hot when they open.
4. **Scars.** Healed claw scars cut diagonally across the rib armour and shoulders, glowing a dull orange from
   inside (healed by heat, not by skin); the brow horns are notched like old battle damage (the mesh is
   mirror-symmetric, so the scars repeat on both flanks).
5. **Back marking.** A pale bone chevron along the saddle points forward, readable from above.

Clips: Run (heavier and longer than the Prowler's, more bounce), Idle, Hit, Death, **Howl** (plants the forelegs,
rears the chest, throws the head up with the shears wide, collar flared, tail raised and shaking).

## StormMatriarch (Skyray boss, air: releases small flyers; storm and charge organs)

**Starting point:** the Skyray as a brood mother of the air (an Earth manta is the far starting point).

Note: in the game data `StormMatriarch` is still a bat boss (it calls Mossbats). The model follows
`Docs/MODEL_TODO.md`: a Skyray boss that releases small flyers.

1. **Twice the span, a second fin pair.** Broad sickle fins with the glowing leading edge, plus a smaller pair of
   aft fins behind them, so the outline from above is a layered arrowhead.
2. **Storm organs.** Two long charge horns sweep back from the ram prow, each with three glowing node rings, and a
   row of capacitor beads glows along the leading edge of each fin. The trident tail is longer and its prongs carry
   charge nodes.
3. **Brood hatches.** Two armoured dorsal plates on their own bones cover a glowing brood chamber on the back. In
   the Release clip they open up like beetle elytra and snap shut: the small flyers come out of the back.
4. **Crown prow.** The ram prow carries three backward horns of chitin and the pincers are bigger and barbed.
5. **Palette as a parameter.** Default: the Skyray's cool indigo with cyan glow. A warm alternative (plum body,
   magenta to orange leading edge, amber charge) is rendered for Remco to choose.

Clips: Fly (a slower, heavier wave), Idle, Hit, Death, **Release** (pitches up, the hatches open, a heave, they
close).

## CoreWisp (Wisp boss, air and ground: pulses of damage, splits into wisps as it dies)

**Starting point:** the Wisp's petal bud and rings, as a larger and more dangerous energy organism (an Earth comb
jelly is still the far starting point).

1. **A bigger heart, half exposed.** Six petals instead of four, shorter, so the heart shows at the crown and
   through a wide glowing slit at the front. The heart is on its own bone and pulses (bone scale).
2. **Three rings.** An inner ring of vertebra beads, a middle band with hooks and an outer crown ring with four big
   glowing nodes and a row of spikes; each ring spins in its own plane and all three expand in the Pulse clip.
3. **Five tendrils, a comet tail.** In flight the five tendrils stream backward behind the bud, so it reads as a
   comet with a hot front. Landed, they plant around it as five legs.
4. **Facing.** The front slit and a hooked brow crest on the front petal point the way it moves; the rear petals
   carry the tendril roots.
5. **Palette as a parameter.** Default: the Wisp's palette (umber-violet flesh, ice-blue light). A warm alternative
   (amber heart and glow, lime-green ring nodes) is rendered for Remco to choose.

Clips: Float, Land, Grounded, Hit, Death (petals burst, rings drop), **Pulse** (the heart swells, the petals snap open,
the rings flash outward and spin up, then contract).

# Round 6: Cole "Pathfinder" Mercer, the hero (2026-11-03)

Brief: the human colonist soldier and pathfinder of `Docs/STORY.md`: first down, scouts, repairs, holds the gates
with a pistol and a laser sword. Our side, so cool and technical (blue, white, yellow, cyan light), never warm.
Grounded and capable, a bit rugged. Not a copy of any existing game or film character.

Lessons carried over (`tower-defense-look-feedback`): three clearly separated values (white plates, mid-blue suit,
gunmetal gear) so the figure never reads as a dark blob on a dark road; every joint has a ball or a pad at its
pivot; a stance with weight in it (bent knees, staggered feet, a slight hunch, head forward), never a plank.

**Starting point:** a field-expedition suit (padded pressure suit with hard plates), not a space marine and not a
knight: the old cloak goes, the pack and the tools come.

1. **Silhouette from the game camera (about 30 px):** a large rounded white helmet with a yellow crown stripe,
   two broad white pauldrons with yellow rims, and a boxy gunmetal power pack with two glowing cyan cells on the
   back. From above that is "white dome, two white shoulders, two cyan dots": readable at 20 to 30 px and nothing
   like a Burrower (no warm colour anywhere on him).
2. **Helmet:** a smooth ceramic shell with a wide wraparound visor of smoked glass, a cyan HUD line along its lower
   rim, side modules with small lamps, a gunmetal chin guard and a lit collar ring it sits in. The face is never
   shown: the visor carries the expression (glow, reflections).
3. **Suit:** mid navy-blue padded fabric, quilted in bands (rings round the limbs, horizontal bands on the belly)
   with stitched seams, so it reads as cloth next to the plates. White ceramic plates on chest, pauldrons,
   forearms (bracers with a cyan strip and a small emitter lens: the repair tool), thighs, knees and shins.
   Gunmetal on the belt, pouches, collar, gloves, pack and boot shells; dark rubber on soles and joint pads.
4. **Gear:** a holster on each thigh (the second one is for Dual Wield later), a belt with magazine pouches, the
   sword hilt clipped to the pack.
5. **Rugged:** scratched and chipped plates (darker grey substrate in the chips), worn bright edges on the metal,
   grime in the crevices, dust on the shins and boots, and pale pink Crysium dust with faint glints on the boots:
   he has been walking the crystal plains.
6. **Proportions:** heroic but human: about 1.55 units to the helmet top (the old figure's height), long legs,
   wide shoulders and pauldrons, a slightly large helmet and chunky gloves and boots so the hands and feet read at
   game size.

Weapons: a compact pistol (gunmetal and white with a cyan charge window and a yellow muzzle ring), and a laser
sword hilt (gunmetal grip with rubber rings, a white guard with yellow stripe, a cyan emitter). The blade is a
separate emissive mesh. Upgrade hints: Quick Reload adds an extended magazine with a yellow base plate; Wide
Slash adds a forked emitter with two prongs and a wider blade.

Motion: weight in every pose; the walk rolls heel to toe with a counter-swing of arms and shoulders; the run leans
in, low and long (Remco: the old run looked ridiculous), arms pumping; the upper-body clips (Shoot, Reload, Slash)
are authored over a stable stance so they layer over Walk and Run with an avatar mask.

# Round 7: P1 towers (Gun Turret, Cannon, Laser, Flak, Gate Gun), pad, stars and wreck (2026-11-03)

Brief (CLAUDE.md, MODEL_TODO section 3): our own side, so cool and technical; a light top and a dark underside so
a tower stands out from the ground; stars on the tower show the upgrade level; damaged towers smoke and spark (VFX
in Unity). Each tower's role must read from its silhouette alone at one grid cell from the 44 degree camera.

**One kit, one language: colonial defence works.** Every tower is the same family of machines, built by the same
colony workshop from the same parts, so a lineup reads as one army and nothing reads as alien:

- **Three values, like Mercer's suit:** white ceramic armour on everything that faces up (the light top), colony
  blue on the sides of the hull, gunmetal and near-black on the undersides, mounts, barrels and the pad's skirt
  (the dark underside). The top is always the brightest part of a tower, so it lifts off the dark ground.
- **Yellow is the trim colour:** hazard stripes on the pad's rim and the gun shields, yellow rings on muzzles.
  Yellow is ours (the stars and the hero's crown stripe are yellow), never orange.
- **Each tower has one glow colour** (the tower's colour in the data where it is cool): Gun Turret azure blue,
  Cannon yellow-amber, Laser cyan, Flak ice white, Gate Gun pale gate blue. Glow sits in slits, coils, sensor eyes,
  rank lights and the lens, never over a whole surface. Aliens stay warm and organic: no tower has an orange, red or
  green glow (see the open point on the Laser below).
- **Hard surface, worn by use:** chamfered edges that catch the light, panel seams, bolt rows on the panel corners,
  vents and heat sinks, chipped paint on the edges showing the grey under-layer, bright scratches on the gunmetal,
  grime in the corners and dust on the pad's foot. All of it lives in the textures; the meshes stay simple
  chamfered blocks, cylinders and plates.

**Rig, the same on every tower:** `Root` (the pad, static) > `Yaw` (turns) > `Pitch` (elevates, at the trunnion)
> `Barrel_n` (recoil, spin) > `Muzzle_n` (sockets). `Star_1..3` sit on the pad, `Smoke` on the head. One skinned
mesh per tier, one material per tower, every piece rigid on one bone.

## Shared: the build pad, the stars, the wreck

- **Build pad (socket):** a low hexagonal plinth (0.72 across, 0.24 high, so a tower stays inside its cell with a
  shoulder to the road): a dark gunmetal foot with yellow hazard chevrons on the rim, a colony-blue body with vent
  slots, a white top deck with a glowing turret race (the ring the head turns on). On the side that faces the camera
  (+Y in Blender, -Z in Unity) the deck drops into a sloped **rank plate** with three star recesses: the stars are
  always visible from the fixed camera, whatever way the gun points. Every tower model includes the pad, so a
  tower is still one mesh and one material.
- **Stars:** five-pointed, yellow-white emissive, slightly domed, 0.12 across, set into the rank plate and tilted
  to face the camera. They are part of each tier's mesh on bones `Star_1..3`; Unity hides a star by scaling its
  bone to zero. A loose `Tower_Star` mesh exists too.
- **Wreck:** the pad survives (blackened, cracked deck, a dead race ring) with a collapsed husk on it: the head's
  hull fallen over at an angle, a split armour plate, a bent pipe, and a scrap pile of plates and rods spilling over
  the edge. A few embers and one flickering cyan cell in the emission. Per tower one loose debris piece (snapped gun
  barrel, cannon muzzle brake, cracked laser lens housing, flak ammo drum with barrel stubs, gate-gun barrel) so the
  wreck still says which tower stood there.

## Gun Turret (PulseRifle, the starter, ground and air)

**Role in the silhouette:** the "rifle": a compact rounded head with one long, slim barrel wrapped in a ribbed
heat-sink jacket with azure coil rings. Light, quick, balanced. Tiers grow the number of barrels and the cooling:

1. **T1:** one jacketed barrel, a side cell box (power pack) on the left, a sensor eye in the front plate.
2. **T2:** twin barrels side by side, armoured cheek plates, power packs on both sides, a sensor block on top.
3. **T3 A (Rotary, rate):** a four-barrel rotary cluster with a big cooling shroud and a drum feed on the side;
   the cluster spins while firing.
   **T3 B (Marksman, damage and range):** one long heavy barrel with three capacitor rings and a muzzle brake, a
   long sensor scope on top and counterweight plates at the back.

## Cannon (ground, splash)

**Role:** the "tank": the widest and lowest head, a sloped wedge glacis, a short thick barrel with a big boxy
muzzle brake. Amber glow only in the vision slit and the breech light.

1. **T1:** one thick barrel in a mantlet, wedge hull, a vision slit.
2. **T2:** side armour skirts, a fume extractor (bulge) on a longer barrel, a shell rack on the back.
3. **T3 A (Twin, rate):** two barrels side by side in a wide mantlet.
   **T3 B (Siege, damage):** one huge barrel with a double-baffle brake, hydraulic recoil cylinders along it and a
   heavy counterweight bustle.

## Laser (ground, continuous beam)

**Role:** the "lamp": tall and slender, an emitter held in a **fork yoke** (the only fork mount in the kit) with a
big round lens at the front: from above a glowing disc in a ring. Cooling fins behind it.

1. **T1:** emitter tube in the fork, one lens, two fins.
2. **T2:** bigger lens with a focusing ring of three prongs, four radiator fins, capacitor cells on the yoke.
3. **T3 A (Overcharge, damage):** a huge lens in a crown of three long focusing prongs, capacitor coils that glow
   along the emitter.
   **T3 B (Long focus, range):** a long telescopic barrel with stacked focusing rings and two big radiator wings.
- Clips: IdleSweep, **ChargeUp** (prongs close, the emitter slides forward), **Beam** (a fine shiver while firing).
- **Open point (colour):** the data colour of the Laser is pink-red (1, 0.35, 0.45), a warm colour that the rules
  give to the aliens, and its beam uses it. The model glows cyan; ask Remco whether the beam should follow.

## Flak (air only)

**Role:** the "anti-air gun": twin barrels pointing **high** (40 degrees at rest), a gun shield in front, ammunition
drums on both sides. From above: two long parallel lines aimed at the sky, which no other tower has.

1. **T1:** twin barrels, a small shield, one drum.
2. **T2:** longer barrels with flash hiders, drums on both sides, a small radar dish.
3. **T3 A (Quad, rate):** four barrels (two by two) and a wide shield.
   **T3 B (Long range):** twin long heavy barrels and a big tracking radar dish.
- Clips: IdleSweep, Fire per barrel, **FireAlternate** (left, right, left, right).

## Gate Gun (on a gate's crossbeam)

**Role:** a small autocannon clamped on the gate: no pad, a saddle clamp that hugs the beam, a compact head with a
short barrel. Gate colour, pale blue glow.

1. **T1:** one short barrel and a small shield.
2. **T2:** twin barrels, an ammunition box.
3. **T3:** twin barrels with an armoured hood and a sensor mast.
- No A/B: the gate's gun has no paths. The data today arms the gate at its levels 1 and 2 (T1 and T2); T3 is for
  a later gate level.

**A/B note:** `GAME_DESIGN.md` says every tower chooses one of two paths at level 3, but the data has no paths yet
(`TowerLevel` has one optional `Model` per level). The A/B looks are ready for when the paths exist; until then T3 A
is the default level-3 look.

# Round 8: Crystal Plains level environment kit (2026-11-03)

Brief (Remco: "the whole environment, the road, the gates and everything"; MODEL_TODO section 7): the ground the
colony fights on, as seamless tiling material sets plus modular meshes that the level code places from the layout.
Gameplay and the grid do not change. One grid cell is 1 world unit; roads are two cells wide; raised ground stands
0.55 high; the camera is orthographic at 44 degrees, about 51 px per unit at the default zoom.

**Readability first: three value bands and two textures of "can't".**

- **Ground (not buildable) is the darkest band** (15 to 20 % value): blue-violet basalt crust with a fine crackle
  and pink-white Crysium gravel in the cracks that glints. Rough, matte, busy at a small scale only.
- **The road is the light band** (45 to 55 %): pale lilac-grey slabs, smoother (a soft sheen the light slides
  over), with big calm shapes. The road can never be confused with the ground because it is lighter, smoother and
  calmer, and a raised kerb with a lit lip separates them.
- **Build pads are ours:** a dark gunmetal survey plate with a cool white-blue light rim and four yellow corner
  ticks. Nothing on the alien side has a straight lit line, so "buildable" reads at a glance, also with the
  gamepad cursor. Non-buildable cells carry the things that say "no": rubble, flora, crystals, cliffs, the rift.

**The road: a fused slab, not cobbles.** The colony's thermal pavers melted a strip of the plain flat; as it cooled
it cracked into large polygonal plates. Burrower claws scuff it pale, dark grit fills the joints, and a few joints
hold Crysium dust. The kerb is the heaved, broken lip of that slab: chiselled blocks a hand high, lighter on top,
with a thin pink Crysium seam at their foot on the road side, so the road edge is a line of light from the camera.
The plaza is the same stone cut in larger square slabs with engraved channels full of glowing dust, so a meeting
point reads as a place.

**Alien departures for the decor (Earth only as a starting point):**

1. **Rock is shingled, not round.** Boulders and cliffs are stacks of leaning plates (like the Cragback's back),
   cut by Crysium veins. The Column is not a ruin pillar but a natural stack of flattened lenses with glowing seams
   between them; the Spike is a fan of hooked obsidian horns; the Spire a twisted needle with a crystal crown.
2. **Flora is plum-dark flesh with two lights:** Crysium pink and an acid lime (the Skitter venom colour), never
   Earth green. The Frond is a fan of serrated, ribbed blades from a bulb; the Tendril a tuft of feelers with eye-
   like bulbs; the GlowTree an umbrella of a flat disc canopy with glowing filaments hanging down; the Mushroom a
   stack of lantern caps with lit gills. All sway (vertex mask).
3. **The rift is a wound, not a pit:** a jagged fissure with interlocking saw-tooth lips, strata in its walls, pink
   Crysium shards pushed up along the lips and an amber glow rising from the depth (the theme's Rift colour), so
   enemies crawl out of heat. It chains in 1, 2 and 3 cell lengths with end caps.
4. **The map does not stop, it falls away:** the border is a wall of leaning cliff plates, or a chasm edge breaking
   into a violet void, with far crystal towers and ridges in the mist behind.
5. **Burrower traces:** shell fragments and egg clusters near nests in the warm Burrower palette (crimson, orange,
   pink glow), so the alien side's colour shows on the ground too.

**Budget:** modular meshes 1,500 triangles or less, scatter 200 or less, one material each, 512 maps (baked) or the
tiling sets via UV. Tiling sets 1024 (512 Low): Albedo, Normal and a Mask (R AO, G smoothness, B height for height
blending, A Crysium-dust mask), plus an Emission map only where something glows (ground grit, flora lichen,
plaza inlay, rift).

# Round 9: our side's structures and machines (gate, base, base turret, drones, gadgets, 2026-11-03)

Brief (MODEL_TODO sections 4, 5 and 6; CLAUDE.md look and feel): everything the colony builds besides the towers.
It must read as **the same workshop as the round-7 towers**, so a gate with its Gate Gun, a tower beside it and a drone
overhead look like one army: white ceramic on everything facing up, colony blue on the hull sides, gunmetal and
near-black underneath, yellow trim and hazard stripes, **one cool glow per machine** in slits, rings and lenses, never
over a whole surface. Same hard-surface kit (`tower_kit.py`), same self-bake, same four maps, so the textures look
alike too. Burrowers stay the only warm, organic things on the field.

## Gate (the barricade the hero defends)

**Role:** a wall across the road that holds the line. From the 44 degree camera it must read as "a gate", not as two
towers: a strong horizontal beam between two heavy pylons, and a field between them (the field stays a Unity shader).

- **Size from the code:** the road is 2 cells wide; `TowerView` passes `Span + 0.05 = 2.05` to `GateView`, so the pylon
  centres stand at x = +-1.025 (on the kerb line). The Gate Gun's saddle (round 7) is 0.2 deep with its jaws hanging
  0.08 below its pivot, and `GateHeadHeight` is 1.17: the beam is a 0.2 deep box girder whose **top centre is at
  1.17**, flat over the 0.34 of the saddle, so `Tower_GateGun` drops onto the `GunMount` socket with no offset.
- **Pylons:** a gunmetal foot with anchor bolts and a hazard band, a colony-blue tapered column, a white crown cap
  that the beam sits in, and a pale gate-blue beacon on top. The inner faces (towards the road) carry the field
  emitter: a dark rail with a glowing slot top to bottom, so the field visibly comes out of the pylons.
- **Hex armour:** each pylon is clad in white ceramic **hex panels** (six per pylon: two on the enemy face, two on
  the base face, two on the outer face), each on its own bone over a dark socket with a glowing conduit. Damage blows
  panels off (bone scaled to 0: you see the dark, glowing socket); a repair pops them back in one by one with an
  overshoot (clip `Repair`). This is the S4 "hex panels on repair" idea.
- **Levels (the Blockade has three in the data, 500 / +300 / +500 HP; the gun comes with levels 2 and 3):**
  1. **G1:** pylons, beam, braces. Plain and functional.
  2. **G2:** sloped ceramic **buttresses** lean against the outer faces of both pylons, blue armour skirts on the
     beam, a yellow hazard skirt round each foot. Wider at the base, visibly sturdier.
  3. **G3:** G2 plus angled **deflector slabs** at the pylon feet on both road faces (tank-trap teeth), a heavier
     crown with two glowing **shield horns** per pylon, and a second hazard band on the beam. The heaviest, most
     "fortress" silhouette.
- **Damage states:** *intact*; *damaged* = some hex panels gone (bones at 0) plus an overlay mesh with scorch plates,
  a cracked crown plate hanging off, torn cables, hot ember edges and debris at the feet, and spark sockets; *broken*
  = a separate wreck: two snapped, burnt stumps, the pylon tops lying outward, the beam fallen across the road broken
  in two, hex panels scattered, embers. A `Collapse` clip (pylons keel outward, beam drops) bridges intact and wreck.

## Base (end of the road, besieged like a gate)

**Role:** the colony's foothold: the last stand. It must look like the most important thing on the map, and read as
"ours" from far away: a white and blue compound on a lit deck with one tall feature (the comms mast) and one bright
heart (the Crysium core).

- **Footprint as the code has it** (2.3 x 1.9 deck at `BaseScale` 1.2, ramp towards the road): about 2.8 x 2.3 units
  plus a ramp to the road; the model is built at final size (no `BaseScale`). Front = towards the road (Unity +Z).
- **Modules** (each its own object with its pivot on its foot, so the cutscene timelapse can raise them one by one,
  and so the upgrade screen diorama can reuse them):
  1. **Deck:** an octagonal plinth: gunmetal skirt with hazard chevrons, blue body with vents, white deck plates,
     lit edge strips in the base colour, a ramp with yellow chevrons down to the road, four corner bollards.
  2. **Command post:** a two-storey prefab block, not a round tower: a sloped glass front band (the operations room)
     with a cool glow, white roof, blue walls, an upper storey with an antenna array and the **comms mast** with the
     turning **dish** (its own object, pivot on the mast top).
  3. **Crysium core:** a cage of four white and blue pillars with cyan containment rings round a tall **crystal**
     (its own object, so Unity can pulse and ignite it with its own material instance). Icy lilac-white: Crysium tamed
     by our cage, cool, never warm.
  4. **Landing pad:** a hexagonal pad with a lit ring, a yellow landing chevron and two floodlight posts.
  5. **Habitat:** two stacked blue-white habitat cylinders with porthole lights, a link tube to the command post.
  6. **Hardpoints:** two armoured junction boxes on the deck's left and right edges, facing the cells where the base
     turrets stand, with sockets `TurretLink_L/R`.
- **Base turret** (meta decision: the base weapon level places one or two `BaseTurret` towers on the cells beside the
  base; never sellable): the Gun Turret's head (same parts, same rig) on a **bastion pad** instead of the hex build
  pad: a square, heavier, base-coloured plinth with a white parapet, hazard corners and the same rank plate and stars.
  So a base turret reads as "part of the base", not as a tower the player placed. Tiers T1, T2, T3 (T3 = the rotary).
- **Damage states:** *intact*; *damaged* = overlay with scorch, smashed windows (dark plates over the glass band), a
  torn deck plate, a bent antenna, toppled crates, hot edges and spark sockets; *destroyed* = a wreck: cracked burnt
  deck, the command post caved in (roof slab tilted in, upper storey fallen beside it), the mast toppled, the core cage
  broken with a dark cracked crystal stub, the pad split.

## Drones (repair, harvest, attack)

**One family:** the same white ceramic hull shell with a blue skirt band and a gunmetal belly, the same glass sensor
visor, the same ducted rotor nacelles (gunmetal duct, white lip with a yellow stripe, dark blades), the same nav-light
pips. What changes is the layout and the tool, so the role reads at about 20 px from 2.2 units up:

1. **Repair** (cyan): a round, compact quad with four small ducts on an X (a round silhouette, like a flying
   medic kit) and an articulated arm under the belly that ends in a downward emitter dish: the light beam starts at
   `Beam`. On top a cyan "plus in a hexagon" mark (our own symbol; the code's red cross is a real-world emblem and warm,
   so it goes). Work: the arm swings down, the emitter turns and pulses.
2. **Harvest** (amber): the heavy hauler: a longer hull on two big side ducts (wide H silhouette), a drill under the
   nose ringed by three claw fingers, and a cargo canister slung under the belly in a gunmetal cradle (its own object
   so Unity can light it while carrying). Yellow hazard stripes on the booms. Work: the drill spins, the claws grip.
3. **Attack** (azure): a dart: a narrow hull, swept stub wings with white tops, lift ducts at the wingtips, a twin gun
   pod under the chin and a glowing thruster ring at the tail. Work: alternate recoil.

Open point: the data colour of the attack drone is coral (1, 0.45, 0.35), a warm colour; the model glows azure.

## Gadgets and path objects

- **Land Mine:** a low gunmetal disc (0.5 across) gripping the road with six hooked anchor claws, a blue band, a white
  pressure cap ringed with yellow hazard chevrons, and an **amber armed ring** between cap and body (amber, not red:
  red is the aliens'; the data colour is red, an open point). Clips: `Arm` (cap rises, claws bite), `Idle`.
- **Chrono Field emitter:** a post on a gunmetal tripod: blue column, white collar, and at the top a vertical shard of
  tamed Crysium held between two prongs inside **two counter-rotating gyro rings** (bones), pale chrono blue; a lit
  projector ring near the foot "paints" the field on the road (the field itself is the Unity shader). Clips: `Deploy`,
  `Idle`.
- **Barrel** (beside the road): a colony explosive canister in a gunmetal cage, white and blue shell with yellow
  hazard bands and amber sight windows. **Air Net:** two posts with emitter coils (yellow) on a low sill; the strands
  are VFX between the sockets. **Ground Drone:** a small four-wheeled ram robot with a white plough, blue body, cyan
  eye. Clips `Drive`, `Idle`.

**Budgets:** gate per tier and base (in-level) 8,000 triangles or less, LOD1 about 3,000, 1024 maps (512 Low); drones
and gadgets 2,500 or less, 8 bones or fewer, 512 maps (256 Low). One material per asset, packed Mask (R AO, G
smoothness, B metal detail, A ceramic detail), shared `Detail_Metal_Normal` and `Detail_Ceramic_Normal`.

# Round 10: P2 enemies and their level bosses (Stinger, Bat, Bulwark, Oozer and five bosses, 2026-11-04)

Same rules as rounds 2 to 6 (Earth animal only as a start; matte, layered surfaces with baked micro-detail; a strong
read at ~20 px through value contrast, bold markings and glow accents; warm organic colours, no cool blue or cyan,
which is our side; each species keeps its role and element from `ProjectGenerator.Bestiary`). New helpers live in
`Tools/Blender/creature_kit2.py`; the shared modules are only imported.

Two rules added this round, from the "Verbeterpunten" list of `Docs/MODEL_TODO.md`:

- **Asymmetric markings where the mesh is mirrored.** Mirrored UVs copy every marking to the other flank (PackAlpha's
  scars). Markings that should be read as one sign sit on the midline (the plate chevrons, the dorsal stripe).
- **No uniform honeycombs.** Egg and gland fields use cells of mixed size with gaps and veins, never one cell size.

## Stinger (armoured scorpion: its acid tail stings towers in passing; heavy shots crack the plates)

**Earth starting point:** a scorpion. Data colours: violet body, magenta eyes and accent, acid green blood.

1. **Six legs, not eight,** on a low wide stance, the front pair longer; feet end in single hooks.
2. **Trap claws.** Each claw is a swollen hand with a fixed blade-finger and a hinged hook-finger, both serrated, the
   inside edge glowing magenta; a back-turned barb on each hand. From above the claws make the front of an arrow.
3. **Glowing armour plates.** Five shingled dorsal plates over the abdomen and a carapace plate over the head, each
   plate dark plum with a pale worn front lip, a hot magenta rear rim and a forward chevron: at 20 px the back is
   dark with bright magenta bars pointing the way it walks. The plates are where the armour lives.
4. **An acid tail, not a sting.** Five ringed segments arc high over the back; acid glows lime-yellow at every joint,
   and the tail ends in a swollen, veined acid bladder with a long hooked needle that points forward over the head.
   The hottest acid light is at the tip, high up, so the tail reads from the game camera.
5. **A face of eyes and palps.** No scorpion eye pair: a raised mound with a crown of six magenta eye beads and two
   short hooked palps under the carapace lip.

Colour: plum-magenta plates (warmer than the data violet), near-black body and legs, magenta plate glow, acid
lime-yellow tail. Clips: Walk, Idle, Hit, Death, **Strike** (the tail whips forward over the head and stabs down).

## Bat (element bat: rises from element nests, spits element bolts, air)

**Earth starting point:** a bat. One mesh for every element; the element is a palette (and so a set of maps).

1. **Three-spined wings.** Each wing is a membrane over three long hooked spines fanning from a knobbed wrist, the
   skin scalloped between them; the leading edge glows in the element colour and the membrane carries pale bands
   (a wide, flat outline with a moving light line on every beat).
2. **No ears, no snout.** A short wedge head with a split lower jaw around a glowing bolt throat, four eyes in a row
   on each side, and two swept-back sensor horns where the ears would be.
3. **A bolt gland.** A glowing sac on the chest (seen from below and from the front) feeds the throat. A glowing
   dorsal stripe from the head to the tail points forward from above.
4. **Whip tail with a forked vane,** and two hooked hind talons tucked under the body.

The element skins differ only in colour (membrane, plates, veins, glow, eyes); the mesh and the rig are one.
Default export: Ember (fire). Other skins with `--element` (see README round 10). Clips: Fly, Glide, Hit, Death.

## Bulwark (upright shield bearer with a crystal sword, armoured, from level 11 to 12)

**Earth starting point:** a bipedal dinosaur ("dino-man") with a shield and a sword.

1. **The shield is its body.** No held shield: a huge curved bone plate grows from the left forearm, ribbed with
   growth ridges, a pale worn rim and a glowing boss: a natural shield, the reason it is armoured.
2. **The sword grows too.** A long Crysium blade grows out of the right forearm (fused, not held): the planet's own
   crystal, pink-white with a hot core, not cyan (the data accent is cyan, our side's colour; flagged).
3. **An armoured helm head.** A long low head under a bony visor plate with amber eye slits, tusked mandibles and a
   crest of backward horns.
4. **Plated, heavy body.** Shoulder plates, a chest plate and a back ridge of spines over dark olive scaled hide;
   lime goo glows in the joints and the throat (the species' goo). Thick digitigrade legs with hooked toes and a
   heavy tail.
5. **Natural posture** (Remco 2026-11-01): knees bent, a forward lean, the sword low at rest (not straight up).

Colour: deep olive-green scales, pale bone plate rims, lime goo glow, amber eyes, pink Crysium sword.
Clips: Walk, Idle, Hit, Death, **Block** (raises the shield in front, braces, holds).

## Oozer (slug on a toxic slime foot: its trail hastes other species and burns hero and gates)

**Earth starting point:** a slug.

1. **Four eye stalks** of different lengths (a long front pair, a short rear pair), each with a glowing eye bulb,
   moving independently.
2. **A gland back.** The mantle is a ridged leathery hood with glowing toxin glands of mixed size (not a honeycomb)
   that vent the slime, and a dorsal ridge of soft spines.
3. **A ring mouth.** The head front is a round lamprey mouth with a ring of hooked teeth around a glowing gullet.
4. **A glowing foot.** A wide translucent slime skirt, lime at the edge with pedal-wave stripes; the trail itself is
   VFX in Unity.

Colour: dark olive-brown mottled hide (so the lime glow carries), lime glands and foot, pale yellow eyes.
Clips: Crawl (pedal wave along the body bones), Idle, Hit, Death.

## The bosses (kin, not scaled copies)

Same boss rules as round 6: the species' signature stays, heavier and older, a crown no common enemy has, the power
as a visible organ with its own clip, and direction at 20 px (hot front, dark trailing mass, a forward chevron).
Each boss is built in its species' units and scaled up as a whole at the end (`creature_kit2.scale_rig`), so it can
share the species' rig code and still get its own proportions and organs.

- **AcidEmpress (Stinger boss: acid rain on towers, plates that regrow).** Wider and lower, more armour plates, a
  crown of magenta Crysium spikes on the carapace, bigger trap claws, and a tail that ends in a big acid **spray
  gland** on its own bone (it swells and squeezes, bone scale) with nozzles. Two short side tails flank the main
  one. Clip **Spray**: the tail rears up and back, the gland squeezes and the acid leaves the nozzles upwards.
- **CrystalWarlord (Bulwark boss: a sweeping sword, crystal that regrows).** Taller and broader, a bigger bone shield,
  a Crysium greatsword grown from the right arm, a crown of Crysium shards on the helm and Crysium breaking through
  the shoulder plates (the crystal regrowth). Clips **Block** and **Sweep** (a wide horizontal swing).
- **SkySovereign (Bat boss, air: a tail laser swept across the field).** A wider span, a second smaller wing pair, a
  crown of horns and a long tail ending in a lens organ (the laser emitter) between hooked prongs. Clip **Laser**
  (it rears, the tail swings forward under the body and sweeps side to side). Data note: in the data the
  SkySovereign is the ray boss and the StormMatriarch the bat boss; the round-6 StormMatriarch model is a Skyray
  boss, so this bat boss fits the SkySovereign slot once the data is swapped (curator decision).
- **SlimeMonarch (Oozer boss: slime floods and more oozers).** A huge slug with a crown of six eye stalks, a broader
  gland mantle, brood sacs on the back (bone scale) where young oozers come out, and a wide foot. Clips **Flood**
  (it rears up and slams the front down) and **Spawn** (the sacs squeeze).
- **RollerKing (Roller boss: a quake with every landing).** The Roller's eight-plate ball, grown and armoured: a crown
  of hooked keels and heavier plates. Clips Walk, Idle, Hit, Death, Curl, Roll, Uncurl and **Quake** (a hop in the
  ball, landing with a slam).

# Round 11: P2 towers and gadgets (Tesla Coil, Railgun, Acid Sprayer, hybrids, Cryo, Support, Rocket, Lava Ball, R.A.T., 2026-11-04)

Brief (MODEL_TODO sections 3 and 4, STORY.md Crystal Plains table): the rest of continent 1's weapons, in the round-7
**colonial defence works** kit with nothing new in the language: white ceramic tops, colony-blue hull sides,
gunmetal undersides, yellow hazard trim, **one cool glow per machine** in coils, slits and lenses. Same pad, rank
plate and stars, same rig (`Root` > `Yaw` > `Pitch` > `Barrel_n` > `Muzzle_n`, `Smoke`, `Star_1..3`), same budgets
(5,000 triangles per tier, LOD1 about 2,000, one 1024 map set per tower shared by its tiers). Every role must read
from the silhouette alone at one cell from the 44 degree camera, next to the round-7 five.

**Silhouette map of the whole roster** (what each tower is from above, so no two collide):
Gun = one slim jacketed line; Cannon = widest, lowest wedge with a stub; Laser = a lit disc in a fork; Flak = two
lines aimed at the sky; **Tesla = the only vertical spire** (no barrel); **Railgun = the longest line**, flat and
twin-railed; **Acid Sprayer = a fat round tank with a short flared nozzle**; Artillery = one thick tube aimed high
over a wedge; Storm Rifle = the Gun with a small coil spire on its back; Cryo = a fat frosted cylinder with a flared
vane nozzle; Cryo Beam = the Laser's fork and disc slung over a frost tank; Support = a mast with a floating core in
a ring of fins (no gun); Rocket = a tilted box of tubes.

## Glow colours (data colour, and where it is warm, our cool answer)

| Tower | Data colour | Model glow |
| --- | --- | --- |
| Tesla Coil | violet (0.7, 0.5, 1) | Crysium violet (0.72, 0.56, 1.0): the coils run on tamed Crysium, the same lilac family as the base core |
| Railgun | white | plasma white-blue (0.72, 0.88, 1.0), hottest in the rail channel |
| Acid Sprayer | (no tower in the data yet) | acid mint (0.55, 1.0, 0.55): the only green on our side, kept in the sight glass, the gauge and the nozzle ring; the hull stays white and blue |
| Artillery | orange (1, 0.6, 0.2) | the Cannon's amber (warm data colour: an open point, like the Laser's) |
| Storm Rifle | (0.6, 0.8, 1) | storm periwinkle (0.58, 0.66, 1.0), between the Gun's azure and the Tesla's violet |
| Cryo Turret | (0.55, 0.95, 1) | frost (0.62, 0.96, 1.0) |
| Cryo Beam | (0.7, 1, 1) | ice cyan (0.72, 1.0, 1.0) |
| Support | green (0.4, 1, 0.6) | signal yellow (1.0, 0.86, 0.42): green belongs to the aliens; open point |
| Rocket | (no tower uses the shape) | azure-white (0.55, 0.8, 1.0) |

## Tesla Coil (level 9, ground, chain lightning; "runs on Crysium")

**Role:** the spire, the tallest thing we build. No gun: a column of glowing coil rings stacked between white
insulator discs, a caged discharge sphere on top and arc prongs round it. Two **Crysium cells** stand in small cages
on the left and right of the base drum, with conduits into the column, so "it runs on crystal" reads from the fixed
camera whichever way the head turns. Arc sockets `Arc_1..n` (non-deforming, on the prong tips) are where the
chain-lightning VFX leaves the coil; `Muzzle_1` is the top of the sphere.

1. **T1:** three coils, a two-ring cage round the sphere, three short prongs.
2. **T2:** four coils, three capacitor pillars round the drum with glowing tips, a bigger sphere, four prongs.
3. **T3A (Chain, more jumps):** five coils and a wide **halo ring** of six prongs round the sphere: a crown much
   wider than the column, so the jump count reads as reach.
   **T3B (Overload, damage):** the sphere is replaced by a big **exposed Crysium crystal** held in three claws, with
   three heavy capacitor banks round the drum: a crystal spike on a column.
- Clips: IdleSweep (the head turns slowly), **ChargeUp** (the discharge head rises a little, the prongs open),
  **Fire_1** (Discharge: a sharp jolt of the head). Unity should not pitch the Tesla.

## Railgun (level 13, ground and air, pierces armour)

**Role:** the lance: the longest, flattest line in the roster. Two gunmetal rails with a glowing plasma channel
between them, held in square white accelerator collars, on a low armoured breech with a capacitor block behind.
From above: a long bright line between two dark ones.

1. **T1:** short rails, three collars, a capacitor block with glowing slits.
2. **T2:** longer rails, four collars with glowing studs, a brace under the rails, capacitor drums on both sides.
3. **T3A (Twin, rate):** two rail pairs side by side that fire in turn.
   **T3B (Lance, damage and range):** the longest rails, five collars, a muzzle crown of four vanes, a radiator
   spine of fins on top and a heavy capacitor drum with a hazard band at the back.
- Clips: IdleSweep, **ChargeUp** (the rails draw back, charging), **Fire_n** (the strongest recoil of the kit).

## Acid Sprayer (level 16, condition damage)

**Role:** the chemical tank: a fat round white tank on the back of the head with a blue band and a tall **sight
glass** on the camera side that shows the acid glowing, a hose to a short, **flared** nozzle with a ring, a pressure
gauge on the hull. The acid glow sits only in the glass, the gauge and the nozzle ring. A `Slosh` bone carries the
bright meniscus band in the glass (it bobs and tilts) so the liquid seems to move.

1. **T1:** one tank, one nozzle.
2. **T2:** twin tanks side by side on a manifold, a longer nozzle with a spray guard plate.
3. **T3A (Fan, area):** three nozzles fanned out from one manifold.
   **T3B (Pressure lance, damage):** one long thin lance with pressure collars, a big **spherical** pressure tank
   with a hazard band and two gas bottles.
- Clips: IdleSweep, **Fire_n** (Spray: a fast shiver of the nozzle while the glass sloshes, loop), **Slosh** (idle).

## Hybrids (two neighbouring towers merge into a new tower; each must show both parents)

Hybrids have three levels (T1 to T3, no A/B): the data has no paths for them.

- **Artillery = Cannon + Flak** (data recipe). The Cannon's wide wedge hull with its glacis, vision slit, skirts and
  boxy muzzle brake, carrying the Flak's **elevated** gun (40 degrees at rest) behind the Flak's hazard-striped shield,
  with the Flak's ammunition drums on the cheeks. One thick tube on T1 and T2 (T2 adds the skirts, a fume extractor
  and a radar mast), two thick tubes and a big radar dish on T3. Amber glow.
- **Storm Rifle = Gun Turret + Tesla Coil.** The Gun's compact head, glacis, sensor eye and cheek plates; its
  jacketed barrel (T1), twin barrels (T2) and rotary cluster in a shroud (T3, clip Spin), but the jacket coils are
  Tesla coils, and a **small Tesla spire** (coils, insulator discs, a discharge ball, prongs) stands on the back of
  the hull. The Gun's power cell becomes a caged Crysium cell. Periwinkle glow.
- **Cryo Beam = Laser + Cryo Turret.** The Laser's fork yoke and its white emitter with the big lens, slung over the
  Cryo's **frosted coolant tank**, with the Cryo's **vane flaps** round the lens housing and the Cryo's coolant
  canisters on the yoke arms instead of the Laser's cells. T3 adds the Laser's focusing prongs. Ice glow; clips
  ChargeUp and Beam as the Laser.

## Cryo Turret (continent 2; built now because the Cryo Beam implies it)

**Role:** the fridge: a fat horizontal frosted coolant cylinder (white with frost rings) as the gun, ending in a
wide nozzle ringed with four vane flaps; upright coolant canisters with glowing caps on the hull sides (two, three,
four). T3 adds a crown of white frost fins along the top of the cylinder. Three levels.

## Support and Rocket (in the code, not in the campaign; simpler, three levels each)

- **Support:** no gun. A mast on the drum with a floating field core (a glowing sphere on `Barrel_1`) inside a ring of
  white antenna fins, emitter studs round the drum and one halo ring per level. Signal yellow.
- **Rocket:** the missile battery: a box launcher tilted 30 degrees on a cradle, its face a grid of dark tube mouths
  with glowing nose caps (2 x 2, 3 x 2, then 3 x 3 with side racks of standby missiles), a small tracking dish.

## Gadgets

- **Lava Ball (level 18, path object; rolls over the road and burns every walker it touches).** The data says it is
  *dropped on the road and rolls on towards the base*, and the story has it fail against heat-proof enemies and
  combine with the Cryo (Thermal Shock), so it is **our machine, not a rock**: a thermite roller. A ball of eight
  white ceramic armour plates held by three gunmetal great-circle bands with yellow hazard marks, and through the
  gaps between the plates the **molten core** glows. The lava glow is the one hot colour our side gets, because it is
  the weapon itself (data colour orange); the hull stays in our palette. The bands make the roll read. Bones `Root`
  (ground) and `Ball` (rolls about X); sockets `Core` (fire VFX) and `Trail` (ground contact). Clips **Drop** (falls,
  bounces, settles), **Roll** (one turn in 30 frames; in Unity drive the angle from the distance), **Burst** (swells
  and is gone).
- **R.A.T. (Remote Assault Tank, level 19).** A small tracked tank, the same family as the Ground Drone: a low blue
  hull with a white top, rubber tracks under white side skirts with hazard stripes, a sprocket and an idler at each
  end (wheel bones), a small round white turret with a cyan eye and one gun with a yellow muzzle ring, and a lift lug
  on top where the drone holds it. Bones Root, Body, Turret, Gun, Wheel_FL/FR/RL/RR; sockets `Muzzle`, `Lug`. Clips
  **Drive**, **Fire**, **Idle**, **Land** (squashes on the drop).
- **R.A.T. carrier drone.** A drone of the round-9 family (white shell, blue band, gunmetal belly, ducted rotors with
  white lips and yellow stripes): a heavy lifter, four ducts on booms (a wide square silhouette), a short hull and
  under it two hook **clamps** that grip the tank's lug. Socket `Cargo` (put the tank's `Lug` there); clips **Carry**
  (hover with the load, clamps closed), **Release** (clamps swing open, the drone kicks up), **Fly** (empty).


**After the first render review (changes to the notes above).** The pale glow colours burned out to white under
bloom, so every round-11 glow is now saturated: Tesla (0.52, 0.28, 1.0) with Crysium cells (0.7, 0.48, 1.0), Railgun
(0.5, 0.8, 1.0), Acid (0.3, 1.0, 0.38), Storm Rifle (0.4, 0.46, 1.0), Cryo (0.4, 0.9, 1.0), Cryo Beam (0.5, 1.0, 0.95),
Support (1.0, 0.74, 0.2), Rocket (0.38, 0.7, 1.0); the Lava Ball core (1.0, 0.34, 0.05). The Tesla's insulator discs
are dark, not white (white discs read as a tiered cake and hid the coils). The Railgun's plasma channel rides level
with the rail tops (it was hidden between them), its collars are gunmetal and the rail tips yellow. The Acid tanks are
bigger and carry a glowing level band, so the acid reads from every side, not only through the glass. The Cryo Beam's
coolant tank rides piggyback on top of the emitter.