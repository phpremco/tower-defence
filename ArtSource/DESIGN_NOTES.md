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

# Round 13: story characters and cutscene models (Voss, ORA, NTI emblem, Pioneer-1, capsule, drill, mothership, beacon, radar console, 2026-11-04)

Brief (MODEL_TODO sections 2 and 8; `Docs/STORY.md` prologue, opening cutscene and continent 4; `Docs/specs/S2_CUTSCENES.md`):
the faces and machines of the story. They are seen in dialogue portraits and in cutscene shots, closer and longer
than anything on the field, so silhouettes stay simple and readable, and the detail sits in the maps. Everything here
is **our side** (NTI, the colony): the round-7/9 workshop language (white ceramic tops, colony blue, gunmetal undersides,
yellow hazard trim, one cool glow per machine). The only warm or organic things are the alien growth on the Pioneer-1
wreck and the re-entry glow of the capsule's heat shield. Nothing copies a real company, film or game design.

Builds: `story_kit.py` (shared helpers, new), `build_voss.py` (creature pipeline: fused parts, bake, skinned bust),
`build_story_props.py` (hard-surface kit: ORA, NTI emblem, Pioneer-1 and its wreck, radar console) and
`build_cutscene.py` (capsule, drill, mothership, beacon). Cutscene-only items get no LOD1; the Pioneer-1 wreck is the
only one that stands in a level (Mirewood, level 8 and the Rotbloom fight), and it is static decor (no LOD needed at
its size; the in-level decor budget is raised to the prop budget because it is a set piece).

## Dr. Elena Voss (chief scientist; dialogue portraits and cutscene close-ups)

**Character:** a woman in her forties, curious and sharp, sometimes doubting. Stylised in Mercer's family: soft
rounded volumes, clean value groups, a slightly large head and eyes, no pores and no realistic skin shading, so she
never tips into the uncanny. Her face must carry the expression (Mercer hides his behind a visor; she is the human
face of the colony).

1. **Silhouette (portrait size):** a high dark hair mass pinned into a bun at the back with a side-swept fringe, a
   white stand-up coat collar framing the face, and a headset with a lit cyan earpiece on her right ear. Three value
   groups: white coat, mid-navy undersuit collar, dark auburn hair; skin in between.
2. **Face:** an oval head with a slightly pointed chin, defined cheekbones, a straight nose, a small mouth with a dark
   mouth line (it stretches into an open mouth when the jaw drops), dark brow strokes and lash lines painted in the
   albedo, steel-blue irises (cool palette accent) with a dark pupil and a glossy eye (low roughness for a real
   highlight). Upper eyelids are their own shapes so a blink bone can close them.
3. **Clothes:** a white field/lab coat (cloth, not armour: soft folds, a stitched seam along the collar edge), lapels
   folded back over a navy high-collared undersuit with a thin cyan lit seam on the collar rim, a colony-blue yoke
   stripe on the coat shoulders, a yellow ID card clipped to the left lapel (the only yellow), and a small hex NTI
   patch on the left shoulder.
4. **Headset:** a slim gunmetal band over the crown under the hair, an earpiece with a cyan ring light on the right
   ear and a mic boom along the cheek: she is always on the line with Mercer and ORA.

Rig (10 bones): `Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R`. Clips: **Idle** (two slow breaths,
a small head drift, one blink), **Talk** (jaw syllables, two nods, a blink), **Blink** (lids only, for a random-blink
layer). Budget 6,000 triangles, one material, 1024 maps (+ 512 Low). No LOD1 (portrait and cutscene only).

## ORA (the base AI: a presence, not a person)

ORA has no body and must never look like a human face. **Form:** a floating faceted crystal core (an icosahedron
with an inner octahedron) inside three gyroscope rings made of arc segments with gaps, a crown of eight triangular
"thought" shards circling above, and one horizontal **gaze bar** across the core facing the viewer: a scanning slit of
light that widens when she speaks (her only "expression"). A thin emitter disc below is where the projection comes
from. All glow in ORA's cyan (0.35, 0.85, 1.0) with white-hot edges.

**Glitch-ready mesh:** UV0 is the atlas for the four maps; **UV1** holds bands for a Unity glitch shader: U = angle
round the vertical axis (0..1), V = height through the whole hologram (0 at the emitter, 1 at the crown), so a shader
can offset horizontal slices, scroll scanlines and tear bands without knowing the parts. Bones for the motion:
`Root, Core, Inner, Ring_1..3, Halo, Gaze`; clips **Idle**, **Talk** (gaze pulses), **Glitch** (snaps and jitters).

## NTI emblem (Nova Terra Industries, the corporate sender)

An original mark: a rounded hexagonal badge (gunmetal rim, deep colony-blue field), a white **horizon arc** (the new
earth, "Terra") and a four-pointed **rising star** above it (the "Nova", lit pale gold-white) whose long lower ray
touches the arc; under it the letters **N T I** as raised white plates on a dark bar. Cold and corporate, slightly
glossy. No globe-with-orbits, no swoosh, no meatball: nothing that reads as an existing agency or company. Static.

## Pioneer-1 (2047 NTI scout probe) and its wreck (Mirewood, level 8 and the Rotbloom fight)

**Intact (log portraits):** an older generation of our machines: the same white and blue, but simpler and more
"space probe": a hexagonal instrument body on three landing legs with round pads, the lower half wrapped in
gold-amber insulation foil, a large high-gain dish on a short mast, two solar wings of dark blue cells in thin yellow
frames, a sensor mast with a camera head and a mint status lens (the Pioneer log colour, 0.55, 1, 0.8), a core-sample
**drill** under the body (story: Pioneer-1 drilled too, and then the attack began), the NTI emblem plate and a yellow
"P-1" hull mark.

**Wreck:** the same parts, crashed and six years old: tilted and half sunk in a mire mound, one leg snapped and
lying, one solar wing torn off and lying flat, the other bent down, the dish cracked with a shard missing, the camera
mast bent over, scorch and burnt panels. Pink-violet Crysium shards grow out of the hull cracks and the ground, and
alien flora (plum fronds with pink and lime glowing tips, tendrils over the legs) is taking it back. One mint status
light still glows (socket `StatusLight`): the probe is still logging. Sockets `Smoke`, `StatusLight`, `Log` (the
logbook pickup). Static decor, 1024 maps.

## Cutscene machines

- **Landing capsule** (~1.75 tall, as `CapsuleModelFactory`): a faceted hexagonal pod (white ceramic panels over a
  blue band, gunmetal waist ring), a broad dark **heat shield** underneath with an ember rim (its own mesh, so Unity
  can fade its glow after re-entry), four **retro-jet** pods on the waist (sockets `Jet_1..4`), three landing legs that
  swing out (clip **Land**), a front **ramp** hinged at its foot that opens down to the ground and shows a lit interior
  (clip **Open**; socket `Exit` where Mercer steps out), a parachute cap and an antenna with a beacon light.
- **Drill derrick** (~4.4 tall, base 2.3 as `DrillModelFactory`): a gunmetal deck with yellow hazard edges, a blue
  motor house with lit vents and an exhaust stack, a four-legged steel lattice derrick with cross braces, a white
  crown block with a beacon, and a drill **string** (pipe, lit collar, fluted bit) on its own bones: clip **Spin**
  (loop) and **Sink** (the string drives 3 units into the ground while it spins).
- **Mothership** (~14 long, 15,000 triangles at most): a long truss spine, a white command module with a lit bridge
  band in front, a big rotating habitat **ring** on four spokes (bone `Ring`, clip **Spin**), blue and white cargo pods
  along the spine, a capsule bay underneath (socket `Bay`), dark radiator fins with cyan lines, and an engine block
  with three nozzles (sockets `Nozzle_1..3`). Readable from far: needle, ring, engine glow.
- **Beacon** (as `BeaconModel`): a tripod whose legs fold out, a telescoping mast, a pulsing lamp (socket `Lamp`) and
  a turning ring; clips **Deploy** and **Idle**.
- **Radar console** (command post): a sloped operator desk with a round radar screen in a bezel (its own mesh with a
  0..1 UV disc for a sweep shader), two small side screens (own meshes, 0..1 UVs), keys and handles, colony blue and
  white with gunmetal. Static.

# Round 14: Gravemaw, spawn origins, resource node, warm Tide and Frost bats (2026-11-04)

Brief (MODEL_TODO: continent bosses, "Overige spawnpunten", "Grondstofpunt", curator decision on the bat skins;
STORY.md: Gravemaw is "the first giant from the deep", a giant burrower with crystal armour that dives under the
ground and surfaces further along, skipping towers). Same rules as rounds 2 to 10 (Earth only as a starting point,
matte layered surfaces, read at the game camera first, warm alien light). Helpers in the new `Tools/Blender/deep_kit.py`.

## Gravemaw (continent-1 boss: the climax of Crystal Plains)

**Earth starting point:** a sandfish or mole cricket crossed with a tunnel-boring machine (a long, low, armoured
digger with spade forelimbs). Not a worm, not a mole: the departures below change every one of those reads.

1. **A drill-maw.** The whole front is five armoured jaw petals that close into a blunt digging cone (the "drill"
   it bores with) and open into a five-pointed star around a white-hot gullet ringed with Crysium teeth. Closed, the
   seams between the petals glow, so even the shut maw is a bright arrowhead. This is the facing cue: hot pointed
   front, dark tapering rear.
2. **Crystal armour, not a shell.** Six broad, overlapping body segments (dark violet basalt-chitin, each with the
   round-10 armour plate: pale worn front lip, glowing rear rim, forward chevron) carry slabs of Crysium armour:
   a pair of angled crystal plates per segment plus a crest of shards along the spine, tallest at the front and
   swept back, and two big crystal horns on the head collar. The crystal is the second material.
3. **Spade forelimbs.** Two massive three-jointed digging arms flank the maw, each ending in three hooked spade
   claws with crystal tips. They pull the body forward in the crawl and dig in the Dive.
4. **Short heavy legs.** Three pairs of short, spiked, column legs under the segments (it is a digger, not a
   runner); the body heaves on a slow lateral wave, so it reads as huge and heavy.
5. **A tail rudder.** The last segment flattens into a spade tail with two crystal fins (the digging rudder).
6. **No eyes on the maw.** Six amber sensor pits sit in a crown on the head collar, behind the jaws.

Glow: hot magenta-pink Crysium light (the Crystal Plains colour, warm) in the petal seams, the gullet, the segment
rims and the crystal armour; amber sensor pits. Front brightest, tail dimmest (rim glow fades along the body).
Size: about 5.5 units long and 2.5 wide in the game (BroodMother 2.5 long): the largest thing on the field.

Clips: Crawl (loop), Idle (loop), Hit, Death, Roar, **Dive** (rears, slams the drill into the ground at a hole
ahead of the jaws, the body follows the head down the same path until all of it is under the surface; debris
bursts at the impact) and **Emerge** (the head bursts vertically out of a hole behind the tail position, debris
flies, the body arches out like a breaching whale and comes down into the rest pose). Debris chunks are part of
the model (rock and crystal on their own bones, hidden at rest), so the clip carries its own burst.

**Burrow hole** (`Gravemaw_Hole`, env-kit style): the crater it dives into and bursts out of, about 3.4 wide: a ring
of slabs heaved up and outwards, a debris ring of broken crust, Crysium shards cracked loose at the rim and a dark
pit with pink-magenta light rising from it.

## Spawn origins (EnemyOrigin Water, Lava, Quicksand, Ice, Bog)

The openings creatures climb out of in a liquid pool. Each is a single static prop, pivot on the liquid surface,
about one cell wide, with the opening sunk below the surface (seen through the translucent liquids) and a low rim
above it. One language for all five: an alien wound in the element, not a man-made pipe: a ring of the element's
own crust heaved around a dark throat, a few Burrower ribs or hooks curling over it, and light rising from below.
The **Emission map is a white mask** (0..1): Unity tints it with the element colour (`NestColor`), so one map set
works for any palette. Ground spawns keep `Burrow_A` (the generic ground nest); rifts keep the round-8 rift.

- **Water:** a sunken funnel with a fleshy, ridged lip just above the surface and five translucent hooked ribs
  curling in over it like a closing hand; pores on the lip glow.
- **Lava:** a broken chimney of chilled black crust plates round a molten throat; cracks glow, ember Crysium splinters.
- **Quicksand:** a sinkhole: slumped concentric sand ridges falling into a dark eye, half-buried shell plates and
  two bone hooks; a faint glow from the bottom.
- **Ice:** a breathing hole: fractured ice plates pushed up and tilted in a ring, frosted, the crack lines and the
  hole lit from below.
- **Bog:** a rotten gas mound: a puckered, ribbed sphincter on a heap of mire flesh with gas blisters and three
  limp tendrils; the blisters and the throat glow.

## Resource node (the Crysium vein the harvest drone mines)

**Earth starting point:** an ore outcrop. Departures: the vein is living Crysium in a gold-amber "ore" palette (the
game's MineColor, so the resource never reads as decor crystal), grown as fat fluted shards that spiral out of a
split basalt boulder, with gold veins running through the rock. Three stages in one FBX on one map set:
**Full** (the cluster whole), **Half** (the tallest shards sawn off flat, cut faces glowing: the drones' cuts are the
only straight lines on it) and **Empty** (only cut stumps and the cracked boulder, cut faces dull, a last weak glow
deep in the crack).

## Bat skins Tide and Frost (warm glow)

Curator decision (2026-11-04): the element stays in the body colour, the light stays warm. **Tide:** deep sea-teal
body and membrane with a coral-orange glow and pale amber eyes. **Frost:** pale frost-white body with a bluish cast
and an ember-orange glow. Baked onto the shipped Bat mesh by a wrapper (`build_bat_skins.py`); `build_bat.py` is
unchanged.

# Round 15: P3 creatures (2026-11-04)

Six common enemies for the later continents and a "corrupted" skin set for The Core. Helpers in the new
`Tools/Blender/creature_kit3.py`; the shared modules and the earlier build scripts are only imported. Budgets as for
every common enemy: ≤ 4,000 triangles, LOD1 ≈ 1,700 with ≤ 16 bones, one material, 1024 maps plus the 512 `Low/` set.

The element rule (STORY.md "trucje" table) is shown on the body, so the trick reads before it happens. Ice and water
species keep the element in the body colour and a **warm glow** (amber, coral), like the Tide and Frost bat skins.

## Shardback (ice beetle: a crust of ice prisms is its armour; the prisms shatter off as the armour is chipped)

**Earth starting point:** a rhinoceros beetle. Data: Ice, pale blue body, ice-white eyes and accent; Sting attack
(frost spit at towers); the armour is chipped off plate by plate (`EnemyView` sheds `Plate` parts in index order).

1. **The ice is not the shell.** A low, wide, dark slate-violet carapace under a crust of ice prisms that grew out
   of it: six removable prism clusters (two on the midline, two pairs on the flanks), each cluster on its own bone
   (`Shard_1..6`) and its own mesh island, so Unity hides one by setting its bone scale to 0 (and spawns the shards
   as debris). Prisms are not textbook quartz: hexagonal, twisted, leaning back like a swept crest, with a hot
   amber core line and frosted rims.
2. **Sockets that glow when bare.** Every cluster stands in a raised socket collar; the socket floor is raw amber
   flesh (warm glow), hidden under the ice at rest and visible as soon as a cluster is gone: a chipped Shardback
   reads as wounded.
3. **A plough head.** A wide blunt brow plate with a short frosted horn, a split jaw of two side-closing frost
   mandibles around an amber throat (the frost spit), four small amber eyes under the brow, no antennae.
4. **Six stocky legs** with a low wide stance and frost-crusted spurs; fixed small frost crystals along the flank
   edges (not removable, so the outline stays spiky after all clusters are gone).

Colour: dark slate-violet chitin, pale ice prisms with a faint blue glow, warm amber in the joints, sockets, eyes and
throat. Clips: Walk, Idle, Hit, Death, **Shatter** (all six clusters burst off in index order and shrink away; the
same order Unity uses to shed them one by one).

## Mireleech (marsh leech: quick and low, leaves a mud slick that hurries everything behind it)

**Earth starting point:** a leech. Data: Bog, mud-brown body, yellow eyes, lime accent, trail VFX.

1. **Armoured rings.** A long, low body of seven segments; each segment carries a hard dark dorsal ring plate with two
   backward hooks, and the soft swollen flesh between the plates glows lime (a lit bellows as it contracts).
2. **A three-petal sucker.** No round lamprey mouth (the Oozer has one): the front is a sucker disc of three hinged
   lip petals (own bones) that open into a Y, each lined with hooked teeth around a glowing gullet.
3. **Sensor pits, not eyes.** A row of small yellow sensor pits along each flank and two on the head plate.
4. **A rear sucker** (a leech's anchor) as a flat ringed disc, plus a mud-caked, wet underside.

Colour: dark mud-brown plates, olive flesh, lime glow between the plates, yellow pits. Clips: **Crawl** (an
inchworm loop: the body arches and stretches while it travels), Idle, Hit, Death, **Latch** (rears up, petals open,
lunges and clamps on, then sucks in pulses: for the bite on gates and the hero). The mud trail is VFX in Unity,
from under the rear sucker.

## Four new species (names are proposals for Remco)

Original names, not from any existing game: **Knitback** (wood healer), **Slagstrider** (lava walker), **Siftjaw**
(sand digger), **Surgefin** (water emerger). Asset ids are the same names.

### Knitback (wood: regrows HP while it walks; camouflaged until close; teaser level 15 and Mirewood 16–19)

**Earth starting point:** a tortoise crossed with a stick insect.

1. **Regrowth organs on the back.** A high arched back of split bark plates; between them three translucent sap
   pods (own bones `Pod_1..3`) with new shoots inside, glowing amber-green. The pods swell in the **Regrow** clip:
   healing is something you see.
2. **Camouflage-capable skin.** Disruptive bark-and-moss mottling in two close values (no high-contrast markings
   except the pods and eyes), so a Unity camouflage fade (tint towards the ground or dither) works on it; the pods
   are the only warm light, and they dim with the emission fade.
3. **Root legs.** Four legs like twisted roots with splayed root claws, knees out, a slow heavy gait.
4. **A bark mask head.** A low head under a bark mask with amber eye slits and two curled horn shoots.
5. **Trailing tendrils** from the rear (two hanging root whips).

Colour: dark bark-brown and moss-olive, amber-green sap glow. Clips: Walk, Idle, Hit, Death, **Regrow**.

### Slagstrider (fire: walks through lava unharmed, leaves a burning trail; teaser level 17, Ashen Reach 6)

**Earth starting point:** a harvestman (long-legged arachnid) / a water strider.

1. **Stilts above the heat.** A compact crucible body hung between four long stilt legs whose knees rise above the
   back; each foot is a heat-proof cone of chilled slag plates (the parts that touch the lava).
2. **Heat-proof plating.** Overlapping charcoal slag plates over the whole body, molten orange in the seams; a row of
   furnace vents on the back glow white-hot (heat shimmer and embers in Unity).
3. **Dripping belly.** The underside is a molten glowing sac (the burning trail starts here, bone `Drip`).
4. **A slit-jaw head** on a short neck: a long narrow wedge with a glowing slit and a crown of back spikes; no
   visible eyes, only the slit.

Colour: charcoal black plates, orange-yellow molten glow. Clips: Walk, Idle, Hit, Death.

### Siftjaw (sand: digs in for a moment and dodges turret fire; Ashen Reach 8–9)

**Earth starting point:** a mole cricket / an antlion larva.

1. **A shovel jaw.** The head is a wide flat scoop of two serrated jaw plates that close into a wedge and open sideways
   (own bones), a row of red eye beads behind them.
2. **Digging spades.** Two heavy front arms end in toothed spades; four short rear legs push.
3. **Sand plating.** Ochre-tan overlapping plates with darker bands and red-orange glowing seams; a segmented abdomen
   ending in a drill-like tail cone (it screws itself in).
4. **Low wedge silhouette**, widest at the shoulders: from above an arrowhead.

Colour: sand-ochre and dark umber, red-orange glow. Clips: Walk, Idle, Hit, Death, **Burrow** (noses down, spades
churn, sinks fully under the ground) and **Emerge** (bursts up out of the sand and shakes off).

### Surgefin (water: swims faster on wet parts of the road, surfaces out of water; Mirewood 6–7)

**Earth starting point:** a newt / a mudskipper.

1. **A sail crest.** A row of tall translucent fin sails along the back on spines, coral-orange glowing edges.
2. **A wide flat head** with a lure organ: a short forward stalk with a glowing coral bulb, a broad mouth with
   needle teeth, gill slits behind the head that glow.
3. **Paddle limbs.** Four short paddle limbs splayed out (tucked to the body when it swims), and a long flat finned
   tail.
4. **Wet skin:** dark petrol-teal with a paler belly, fine spots, glossy.

Colour: deep teal body (the element), coral-orange glow and lure (warm). Clips: Walk (sprawling, side-to-side spine
wave), **Swim** (limbs tucked, strong wave, for the wet parts), Idle, Hit, Death, **Emerge** (rises from under the
water surface, head first, and shakes off).

## Corrupted skin set (The Core: the Sealed One's corrupted Burrowers)

"Same behaviour, but dark with purple glowing crystals" (STORY.md, continent 6). Texture-only: the same mesh and UVs
as each species, so a corrupted enemy is a material swap. The wrapper reads each species' shipped maps and its game
mesh (from its `.blend`, read-only) and writes `Exports/<Species>/Skins/Corrupted/`:

- **Albedo:** the species' own colour pushed to near-black violet-grey (value kept so the baked AO, plates and pores
  survive), plus a network of purple Crysium veins that runs over the body in 3D (continuous across UV seams).
- **Emission:** every native glow turned purple-violet (the species' glow pattern stays, so it still reads as that
  species), plus the veins glowing; the veins' bright cores lean magenta-white.
- **Normal:** the species' own. **Mask:** the species' own, smoother (more glassy) on the veins.

The purple sits apart from the pink Crysium of Crystal Plains (violet, not pink) and from our cool side (no cyan).

# Round 16: continent bosses 2-6 (Cinder Matriarch, Frost Warden, Rotbloom, The Shattered, The Sealed One, 2026-11-04)

Brief (MODEL_TODO section 1 "Continentbazen", STORY.md "Continentbazen"): each continent ends in a climax, so each
boss must be the biggest, most readable thing on its field, clearly not Gravemaw (a long, low, pink crystal
centipede) and clearly not each other. Remco: bosses big, glowing, slow, with a clear facing. Alien, not Earth.

**Silhouette map** (from the 44 degree game camera, so no two collide):
Gravemaw = a long low segmented arrow with a crystal crest; **Cinder Matriarch = a tall four-stilt walker carrying a
huge glowing kiln-sac behind it** (a teardrop with long splayed legs); **Frost Warden = the only upright one**, a
tall hooded sentinel on four column legs with a broken halo of ice; **Rotbloom = a wide low mound wearing a giant
five-petalled flower** (a star from above); **The Shattered = a humped knuckle-walking brute** whose armour bursts
into a crystal star in three steps; **The Sealed One = a vast torso, head and four arms rising out of a broken ring
of seal stones** (no legs, no tail: it is half in the ground).

**The seal glyph** (shared by the Warden's markings and the Sealed One's seal stones, so the player can connect them
on the last continent): a ring of seven broken arcs inside an outer ring, seven spokes, seven inward chevrons and a
ringed eye in the centre. Built in `titan_kit.seal_mask`. Seven is the seal's number throughout (seven spokes,
seven seal stones; Directive 7 is the human echo of it).

## Cinder Matriarch (continent 2, Ashen Reach: burning trail, releases fire larvae, immune to the Lava Ball)

**Earth starting point:** a harvestman carrying an egg sac, crossed with a kiln. She is the mother of the
Slagstrider line (same stilt-and-slag language, grown huge and old), not a scaled Slagstrider.

1. **A kiln-sac brood furnace.** The rear is a huge low-slung sac of chilled slag floes broken by molten cracks,
   hanging between the hind legs; three **larva chimneys** rise from its back (bones `Vent_1..3`, they flare open in
   Spawn; larvae leave from their mouths), and its tip drips the burning trail (bone `Drip`).
2. **Four stilt legs** whose knees rise above her back, each foot a heat-proof cone of slag drums (she wades lava;
   the Lava Ball does nothing), plus **two ember scythes** folded under the head like raptorial arms.
3. **A forward arrow head:** a long slag wedge whose jaw splits into four glowing mandibles, under a crown of
   back-swept ember-Crysium horns. Hot front, the sac glows behind: the facing cue is the head's arrow and the
   bright mouth.
4. **A mantle of slag shingles** over the thorax with molten seams, and a ridge of ember-Crysium spines down the
   back (the second material).

Colour: charcoal slag, orange-yellow molten glow, white-hot chimney mouths, ember Crysium (amber to pale gold).
Clips: Walk, Idle, Hit, Death (the furnace cracks and floods), Spawn (sac heaves, chimneys flare, larvae out),
Roar (rears up, scythes spread, mandibles wide).

**Fire larva** (her minion, under 1,500 triangles): a short slag grub of five shingled segments with molten seams,
a hot ember head with two hooked mandibles, three pairs of stub legs. Clips Crawl (a wave front to back), Idle, Hit,
Death (it cools and curls).

## Frost Warden (continent 3, Northern Crown: freezes towers in a radius, faster as it loses HP, "a guardian")

**Earth starting point:** a praying mantis standing guard, crossed with a hooded statue at a gate. It does not hunt;
it stands for something.

1. **Sentinel bearing.** The only upright boss: four column legs carry a tall torso (about 3.4 high), the two long
   arms end in grown ice glaives held crossed in front of the chest at rest, like a guard at a door.
2. **A hooded crest** of dark slate plates over a narrow face: no mouth, a vertical slit and amber eye-pits in a
   ring (the seal's eye).
3. **Ancient markings:** the seal glyph engraved large on the chest plate and small on both shoulder plates, lit warm
   amber-gold. They are older than the creature's armour (worn, partly frosted over).
4. **A broken halo of ice** behind the head (bone `Halo`) and ice prism clusters on the shoulders and back (ice
   with the warm root glow of the Shardback, its smaller kin). FreezePulse: the arms rise and drive both glaives into
   the ground, the halo flares (bone scale) and the frost ring leaves the impact point. Enrage: it drops from the
   upright guard into a forward hunch, the glaives come out wide and the halo flares: a visibly faster, angrier
   silhouette for the low-HP phase.

Colour: dark slate violet stone-chitin, frosted blue-white ice with amber roots, amber-gold rune light. Two
materials (body, ice). Clips: Walk, Idle, Hit, Death (kneels and bows, glaives planted), FreezePulse, Enrage.

## Rotbloom (continent 4, Mirewood: regenerates, slime trail that hastes enemies behind it, fought at Pioneer-1)

**Earth starting point:** a corpse flower (rafflesia) growing on the back of a bog tortoise. The creature is the
flower's walking soil.

1. **A wide low mound** of rotting bark plates and moss on six short root legs, dragging a translucent slime skirt
   whose rim glows lime (the trail starts under it: bone `Trail`).
2. **The bloom:** a giant five-petalled carrion flower covers the back (petals on bones `Petal_1..5`), wine-dark
   outside, glowing coral to lime inside, round a pit-maw ringed with stamen spikes. Bloom: the petals open flat, the
   stamens rise and spores burst. From above it is a star: unmistakable.
3. **Regrowth pods:** four sap pods on the flanks (bones `Pod_1..4`) swell in Regrow, so healing is something you
   see (Knitback language, bigger).
4. **A bark-skull head** low at the front with a broad dripping shovel mouth, amber eyes and dead-branch antlers
   swept back.

Colour: bark brown and moss olive, lime slime glow, wine and coral petals with a lime throat (warm, rotten). Two
materials (body, bloom). Clips: Crawl, Idle, Hit, Death (the bloom wilts shut, the mound sinks), Regrow, Bloom.

## The Shattered (continent 5, Southern Crown: a Burrower full of overcharged Crysium, three phases, explodes)

**Earth starting point:** a gorilla-like knuckle-walker with an ankylosaur's back. It used to be a Burrower; our
superweapons overcharged the crystal inside it ("we made this monster").

1. **A humped brute** that walks on four arms: a massive front pair with spade fists, a shorter hind pair, a high
   hump of armour plates, a small low head under the hump with a cracked jaw.
2. **Three states on one rig:** eight armour plates on their own bones (`Shell_1..8`) and six crystal clusters
   inside (`Crys_1..6`). Intact: all plates on, seams leaking light, crystals small and hidden. Cracked
   (PhaseBreak1): the four hump plates blow off, the crystals grow out through the gaps. Bursting (PhaseBreak2): the
   rest blow off, the crystals swell into a white-hot star. Death: the crystals swell past breaking and the body
   collapses into them (the final explosion).
3. **The light leaks more each phase:** the body under the plates is a seamed core of overcharged flesh (hot pink to
   white), so whatever is exposed burns brighter; Unity raises the emission per phase.

Colour: the old Burrower plum-bronze armour, overcharged Crysium with white-hot cores, magenta-pink bodies and violet
roots. Two materials (body, crystal). Clips: Walk, Idle, Hit, PhaseBreak1, PhaseBreak2, Death.

## The Sealed One (continent 6, The Core: the ancient being under the seal, summons corrupted enemies, final boss)

**Earth starting point:** none that should be recognisable: a vast, eyeless, praying figure. Partly emerged: only
torso, head and arms rise out of the ground; the rest is under the seal.

1. **Rising out of the seal:** the torso stands in a fissure ringed by **seven broken seal stones** (bones
   `Seal_1..7`) carved with the glyph in amber-gold; in Death they tilt inward and close over it (the seal is
   restored) while the body sinks.
2. **The head:** a tall smooth eyeless crest split into three plates that open (Summon) to show a column of violet
   light; behind it a **crown of three broken rings** carved with glyph bands (bones `Ring_1..3`) that turn and flare.
3. **Four arms:** two vast lower arms planted on the ground in front (it is pulling itself out), three-fingered
   hands with long claws; two thinner upper arms folded high, palms carrying the glyph, raised in Summon.
4. **Ancient body:** a ribcage of pale ash-ivory bone-stone with deep violet light between the ribs and violet
   corruption crystals breaking through shoulders and spine (the same violet as the corrupted skins).

Colour: ash-ivory bone-stone, deep violet glow (the entity), amber-gold glyphs on the seal stones and rings (the
seal). Two materials (body; seal and crystal). Clips: Idle, Rise (from under the ground), Summon, Hit, Death (sinks
back as the seal stones close).

# Round 16: polish pass on the two humans (Voss, Mercer, 2026-11-04)

Curator feedback on rounds 6 and 13: Voss read as a doll (tube arms, a coat like a padded sweater, one chunk of
fringe, plastic skin, a bobblehead on a thin neck above a giant collar, faceted eyelids, a Talk jaw that stretched the
lower face); Mercer's plates were soft pillows fused from simple shapes, with soft panel lines, mitten gloves, a
support hand inside the gun hand, a Down that ended 0.85 off the origin and Shoot/Reload too subtle at game zoom.

## The cause and the shared fix: layered shells

Both were built with the creature pipeline, which voxel-fuses every part into one surface. That suits chitin, but a
plate on cloth, a lapel on a coat, a lock of hair on a head and four fingers on a palm all melt into one rounded
volume. `suit.build_layered_asset` (new, used by both) fuses parts in **layers**: each layer is remeshed on its own
(own voxel size and smoothing), decimated to its own triangle share or fixed budget, baked from its own source only
(a ray from the suit can no longer pick up a plate hovering above it), and the part lookups for styles, skin weights
and UV seams are layer-aware. Separate shells now overlap like real garments and armour pieces.

`suit.dome_patch` and `suit.limb_shell` build hard plates as thickened surface patches with a bevelled rim (a chamfer
row at the edge, about half the thickness), so the plates have crisp, light-catching edges instead of pillows.

## Mercer (identity unchanged: white helmet with the yellow stripe, white shoulders, navy suit, cyan cells)

- Layers: padded suit (soft, quilted), plates (cuirass, pauldrons and lames, forearm bracers, thigh and shin guards,
  knee caps, helmet: fine voxel, little smoothing), gear (pack, belt, pouches, boots, visor, collar), gloves.
- The cuirass is a shell narrower at the waist; pauldrons are shallow sloped shells with a separate yellow-rimmed lame
  under the outer edge; limb plates wrap part of the limb with bevelled edges and real panel grooves.
- Gloves: palm, back-of-hand plate, index, middle and a ring-plus-little-finger group with real gaps, and the thumb;
  all on the existing Fingers/Thumb bones (no new bones).
- Clips: the support hand now cups the gun hand from the left (beside and slightly below the grip, tilted) instead of
  sitting inside it; Shoot kicks harder (24 degrees muzzle rise, 6.5 cm back, shoulders and head react); Reload has a
  wider arc (a flick, the pistol low at the chest, the head and chest turn to it); Down stumbles half a metre to the
  left before toppling right, so the hips end 0.39 from the origin (was 0.89) and the head 0.84 (was 1.34). Revive
  starts from the new end pose.

## Voss (a believable stylised scientist)

- Proportions: the head sits 3 cm lower on a thicker neck; the mock collar is higher and the coat collar rolls round
  the back of the neck, so the neck reads short and the head no longer floats.
- Head: one sculpted surface (eye sockets, cheekbones, brow, a soft jaw line to a small chin, a flatter face plane, a
  fuller back of the skull), a small straight nose, lips as their own volumes with a dark mouth inside behind them;
  dark tapered brow strokes as their own shells.
- Eyes and lids: eyeballs and lids are separate layers with their own budgets (440 and 760 triangles); the upper lid
  is a band concentric with the eye that runs over the top of the eyeball, so the blink slides it down without
  facets or gaps.
- Hair: a cap with a lower hairline, seventeen locks from the hairline and the side parting swept back, side and
  back locks gathered into a twisted bun with a pin, a two-lock fringe sweeping to her right temple.
- Clothes: navy ribbed utility top with the cyan-lit mock collar; a field-lab coat with body panels open in a V above
  the top button, a buttoned placket below, a rolled collar, notched lapels with real thickness, a yoke with the
  colony-blue shoulder stripe between stitched seams, set-in sleeves with an armhole seam and a back seam, a breast
  pocket with two pens, the yellow ID card on the left lapel, the NTI patch on the left sleeve.
- Skin: matte (roughness 0.66, specular 0.35) with painted warmth on cheeks, nose and ears in place of subsurface.
- Talk: the jaw (up to 7 degrees) moves only what lies below the lips, split hard across the mouth line and fading
  over the cheeks and towards the ears, so the lips part instead of the lower face stretching.
- Budget: 10,000 triangles (portrait and cutscene only; head 3,300, eyes 440, lids 760, hair 1,500, gear 750, top
  420, the coat the rest). Smart, layer-weighted UVs: the face and eyes get about twice the texel density of the coat.

# Round 17: environment sets for continents 2-6 (Ashen Reach, Northern Crown, Mirewood, Southern Crown, The Core, 2026-11-04)

Brief: MODEL_TODO section 7, "Latere continenten" (P3). Each continent gets its own ground, road, border, banks and
decor, but plugs into the round 8 system unchanged: tiling sets with the same Mask (R AO, G smoothness, B height,
A dust/sparkle), one grid cell = 1 unit, pivots on the ground, the bordered thing on local -Y (Unity +Z), the same
edge and corner rules. Scripts: `env_kit2.py` (shared continent helpers and the diorama) and
`build_env_<continent>.py`. Exports: `Exports/Env/<Continent>/<Prefix>_<Asset>/`, tiling sets in
`Exports/Env/<Continent>/Tiling/<Prefix>_<Set>/`. The Crystal Plains exports are untouched.

**The readability contract stays the same on every continent:** ground is the darkest band (15-22 % value), the
road the light band (45-58 %) with large calm shapes and a continuous kerb, build pads stay ours (round 8
`BuildPadTile`, gunmetal with a cool rim, shared by all continents), unbuildable terrain carries decor. The
element layer (sand, snow, moss, frost, crystal grit) sits between them in value but never reaches the road's
calm, smooth look: it is busy at a small scale and stays matte.

**Fixes from the round 8 review, applied here:** the kerb is one continuous swept rim with shallow joints (it read as
a row of teeth), cliffs have planar faces and a clear shape language per element instead of bumpy blobs, and every
continent has its own bank style for its own liquid.

## Ashen Reach (continent 2: fire and sand, an easier stretch, confidence)

Mood: a scorched desert under a smoke-brown night, embers in every crack, the warmest place yet.

- Ground: charcoal-brown scoria crust (glassy cinder plates) with thin ember-orange cracks that glow. GroundAlt:
  fine grey ash with charcoal flakes and buried cinders. Element set **Sand**: rust-ochre dune sand with wind
  ripples and black glass grit (a splat layer between ground and road in value).
- Road: pale bone-ash sandstone slabs (sintered by the colony's pavers), warm light grey with dark joints.
- Border: stepped mesa strata (ochre and rust bands), flat terrace ledges, ember fissures in the joints, ash fans
  at the foot. Banks (lava): black glassy rope-lava crust that rolls into the pool, its waterline glowing orange.
- Decor (alien, not an Earth desert): **LavaRock** (rope-crust boulder split by glowing seams), **AshDune** (a wind
  carved drift with a half-buried rib arch of an ancient creature), **CharredTree** (a spiral double trunk that
  splits into hooked, clawing branches, ember cracks in the bark, sways a little), **EmberBloom** (a rosette of
  ash-grey fleshy spoons cupping a glowing coal, sways), **Fulgurite** (branching hollow glass tubes fused from
  sand, their mouths glowing), scatter **Cinders**.

## Northern Crown (continent 3: ice to the north pole, stone and fire; the hard continent)

Mood: a hard, dark, frozen land; the cold is blue-white but the planet's heat glows amber under the ice
(stone and fire under the ice; the aliens' warm colour stays in the land).

- Ground: blue-black slate with frost crystals in the cracks. GroundAlt: frozen gravel and permafrost polygons.
  Element set **Snow**: wind-packed snow crust with sastrugi ridges and windows of blue ice (value capped about
  0.45 and blue-shadowed so the road still wins).
- Road: pale grey-tan slabs, the colony's heated road kept clear: warmer and calmer than the snow beside it.
- Border: hexagonal basalt columns of different heights, snow caps on their tops, icicles and frost, amber heat
  in the deepest joints. Banks (ice lake): a snow-capped shelf of ice blocks that steps down to the lake.
- Decor: **IceShard** (spiralling, leaning ice prisms with an amber heart: not textbook quartz), **Snowdrift**
  (sastrugi-carved drift with a crust lip), **FrozenCrystal** (pink Crysium locked in a cracked block of clear
  ice), **RimeBoulder** (stone furred with frost feathers and icicles), **FrostFern** (stiff frozen fronds with
  amber-glowing tips, sways a little), scatter **IceChips**.

## Mirewood (continent 4: wood, water and bog; Pioneer-1's wreck)

Mood: dark, wet, foggy; teal bioluminescence and violet fungus light (the old "glowing wood" palette).

- Ground: black-brown peat with root threads and twigs. GroundAlt: moss mat with tiny teal spore lights. Element
  set **Mud**: glossy wet mud with puddles (smooth in the puddles, so it reads wet).
- Road: a raised causeway of pale grey-green packed silt and flat stones, the light band.
- Border: a bank of peat and earth held by a tangle of exposed roots, moss on top. Banks (bog and water): roots
  that dip into the water, a mossy peat lip.
- Decor: **DeadTree** (bare twisted tree on arched spider-roots, beard moss, sways), **Reed** (segmented reeds with
  lantern seed heads, sways), **Fungus** (bracket fungi stacked on a stump, violet gills), **FogVent** (a mud
  chimney with pores; its report names the fog emitter point for Unity particles), **MossStone**, scatter
  **Twigs**. The Pioneer-1 wreck (round 13) is the set piece; it is reused, not rebuilt.

## Southern Crown (continent 5: ice to the south pole, everything mixed; the seal cracking)

Mood: unstable. Pale shattered ice over dark rock, and through it overcharged Crysium veins that glow hot magenta
to white. Where Northern Crown is solid and calm, everything here is cracked, tilted and charged.

- Ground: dark glassy black ice over rock, cut by a network of fine glowing veins. GroundAlt: broken rubble of
  mixed stone, ash and ice (all elements mixed). Element set **CrackedIce**: pale shattered pack ice with dark
  fracture lines and magenta light in the widest cracks.
- Road: pale grey slabs with a cool cast; a few cracks run across it (the ground is breaking).
- Border: heaved ice and rock plates pushed up at angles (a pressure ridge), glowing cracks between them. Banks:
  broken ice plates tilted up along the shore.
- Decor: **UnstableCrystal** (an overcharged Crysium cluster: cracked, glowing white-hot inside, splinters floating
  free above it), **CrackedVein** (the ground split open along a charged vein, lips heaved up), **PressureRidge**
  (heaved plates), **ShatteredPillar** (a broken ice-rock pillar with veins), **MixedBoulder** (half frozen, half
  scorched), scatter **Shards**.

## The Core (continent 6: underground crystal plateau; the seal)

Mood: a vast cavern; the plateau is lit from below by the seal. Indigo-black crystal glass, gold-white seal
light (ancient, not Burrower pink), and the corruption in sick violet.

- Ground: indigo-black crystal glass in hexagonal facets with fine gold seams. GroundAlt: crystal grit and shards.
  Element set **Plateau**: pale lilac-grey crystal plateau stone with growth rings (the "crystal plateau" the base
  stands on, used for open areas and plazas).
- Road: pale lilac slabs cut from the plateau, the light band.
- Border: plateau edges that fall into the abyss (the Cliff_* pieces drop instead of rise): faceted crystal-glass
  lips with gold veins running down. Banks: for the violet corruption pools, a crystal-glass rim with violet
  crust.
- **The Seal** (set piece, about 7 x 7 units): a ring of ancient carved stone and gold Crysium set in the plateau,
  concentric bands, radial veins, a central boss. Three states as separate meshes on one 1024 set each: **Intact**
  (whole, gold light), **Cracked** (cracks through the bands, violet light leaking), **Broken** (segments heaved,
  centre collapsed into a violet glowing pit). **SealVein** (flat vein meshes, straight and branching, gold) for
  the veins leading to it across the map.
- Decor: **CorruptedCrystal** (dark crystal with violet light, oozing and fractured), **CrystalPillar** (a tall gold
  vein pillar), **SealShard** (a fallen fragment of seal band), scatter **CrystalGrit**.

## Round 17 review: what changed after looking at the renders

Two or three passes per continent (tiling sheets, kit and decor sheets, the diorama at the game camera and a close
3/4). The question each time: distinct continent at a glance, alien, road and pads readable, moody not noisy.

- **Value bands enforced by numbers:** every tiling report carries `value_mean`. The first Snow (0.55) and
  CrackedIce (0.63) were brighter than their roads (0.48 / 0.49); both were darkened (0.42 / 0.38) and made busier
  and bluer, so they never read as paving. The first Mirewood ground layers were too dark (0.11) and lifted.
- **Ashen Reach:** the mesa wall first read as stadium steps; ledge depth and riser height now wander along the
  wall with clefts cut in. Lava rock was a glossy blob (now matte crust with a hot split), the dune's rib cage was
  buried (now arches out of the sand), the fulgurite read as candles (now knobbly tubes with glowing rims).
- **Northern Crown:** ice shards first glowed amber like candles, then went dark like obsidian; now pale ice
  prisms round one amber-hearted core. The frozen crystal was a toy cube; now crystal bursts through broken ice
  slabs. The column wall lost its round back masses and most snow caps on the back rows (calmer from above).
- **Mirewood:** moss on the kerb hid the road's light line (moss now only on the ground side); the earth bank was
  a cake with icing (now a bulging face, an overhanging turf lip, looping roots and a splintered stump); moss
  cushions read as cobbles (now finer and softer).
- **Southern Crown:** the ridge plates read as tombstones (now flattened chiselled shards at odd angles with dark
  broken blocks behind); the shattered pillar read as a candle; the ground's vein network was toned down.
- **The Core:** the seal's glyphs read as perforated metal (now sparse script on alternate rings), the broken
  state heaves more; the ground glass was too glossy (reflected the sky and read light); the plateau edges' gold
  veins were halved.


# Round 18: P3 hybrids, gadgets, superweapons and drones (2026-11-04)

Brief: the last open gear rows of `Docs/MODEL_TODO.md` (sections 3, 4 and 5). Everything is our side: the
round-7/9/11 workshop language (white ceramic tops, colony blue, gunmetal undersides, yellow hazard trim, one or two
saturated cool glows per machine). Nothing here is in the game data yet, so shapes follow `Docs/STORY.md`.

## Hybrids (continent 3 combos; `Docs/STORY.md` table "Combo's")

Recipes from the script: **Shatter = Cryo + Railgun**, **Arc Laser = Laser + Tesla Coil**, **Corrosive Blast = Cannon
+ Acid Sprayer**, **Storm Flak = Flak + Tesla Coil**, **Thermal Shock = Cryo + Lava Ball**. As in round 11 each hybrid
has three levels (T1..T3, no A/B), the pad, rank plate and stars of round 7, and visibly carries a big part of each
parent; the tier steps are silhouette steps (longer, wider, an extra barrel, a crown), not fine detail.

- **Shatter:** the Railgun's hull, glacis, capacitor block and its twin rails with the bright plasma channel and
  square collars, slung on the Cryo's **frosted coolant cylinder** (glowing frost rings) for the rear two thirds;
  the Cryo's canisters on the hull (1 / 3 / 3) and its **vane flaps** as the muzzle crown. T2 longer rails and a
  brace, T3 the longest rails, frost fins on the cylinder and the Railgun's heavy drum. Ice blue.
- **Arc Laser:** the Laser's fork yoke, white emitter and big cyan lens; **Tesla coils** laid round the emitter on
  dark insulator discs (2 / 3 / 4); the Tesla's caged Crysium cells on the yoke; a ring of **arc prongs** round the
  lens on bones (they close in on ChargeUp, chatter on Beam), the jump sockets `Arc_n` on their tips (3 / 4 / 6).
  T3: a wide halo (the Tesla T3A crown) and a small caged discharge ball on the emitter's back. Cyan lens, violet
  coils (bluer than the Tesla's own violet, which sat close to the aliens' pink).
- **Corrosive Blast:** the Cannon's wide wedge hull, glacis, side lights, skirts, mantlet, thick tube and box brake;
  the Acid Sprayer's **tank with a sight glass and level band** on the back (camera side) with the sloshing
  meniscus on `Slosh`, a feed hose into the mantlet, a gauge on the deck. Green acid rings on the tube, glowing
  brake ports. T2 twin tanks and an extractor, T3 the Acid T3B **spherical pressure tank** with its hazard band, twin
  tubes and a rack of acid shells. Acid green.
- **Storm Flak:** the Flak's carriage, cheeks, elevated guns (40 degrees), hazard-striped shield and ammunition drum;
  **coils** round the gun jackets, a forked arc muzzle, a Tesla **coil spire** on the back with a caged discharge ball
  and prongs (`Arc_n` sockets), a Crysium cell (T2+) and the Flak's radar (T2+). T3: the Flak T3B's long heavy pair
  with brakes and a halo round the spire top (a quad was over budget). Violet. Distinct from the Storm Rifle (Gun +
  Tesla) through the raised guns and the much taller spire.
- **Thermal Shock:** the Cryo's hull, cheeks, canisters and frosted cylinder gun with its vane nozzle; the **Lava
  Ball's plated thermite ball** (eight white/blue plates, gunmetal bands, hazard hubs, molten core) in a clawed
  cradle on the back as the heat core, a hot conduit into the breech, heat fins with a lava strip on the right
  flank. T2 a shock collar of hot slits behind the nozzle; T3 a frost barrel and a thermite barrel side by side.
  Frost cyan with lava orange (the orange is the weapon, as on the Lava Ball). Open point: the roadmap questions a
  combo whose partner is a gadget; the model exists in case it stays a tower.

## Gadgets

- **Decoy Beacon** (continent 2, lures enemies and draws the diggers up): a **seismic thumper**, not a radio mast:
  a gunmetal tripod staked into the road, an anvil plate with a hazard ring and a pulse ring, two guide rods with a
  heavy white **hammer weight** on its own bone that pounds the anvil twice per Idle loop, and a mast with a rotating
  **lure head** (core, three horn emitters, an aerial). Signal yellow: it must shout.
- **Barricade** (continent 4, blocks a stretch of road; enemies break through): five white ceramic slabs on hinges
  along a gunmetal sill (1.9 wide, the road is 2), leaning 14 degrees towards the enemies, braced from behind, a
  blue lower band, a hazard band on both faces and a lit gate-blue top edge. Low and long next to the gate's tall
  pylons, so the two never read alike. Deploy: the slabs fold up one by one; Break: they fall out of line.
- **Spike Strip** (continent 5, damage and slow): three hinged dark plates across the road with yellow hazard edges,
  a cyan edge light and two rows of steel spikes per plate on their own bones; at rest the tips stand a few cm
  out, Trigger punches them up. (The first version had blue decks and read as solar panels from the game camera.)
- **Rollback Field emitter** (continent 5, pushes enemies back along the road): a squat hexagonal emitter beside
  the road with a white **repulsor dish** in a yoke aimed up the road, two capacitor bottles on the back and a ring
  of eight backward-pointing **chevrons** that turns against the march (the rewind sign). Teal: apart from the
  Chrono's pale blue and the acid green.

## Superweapons (continent 5; small objects, the effect is VFX)

- **Payload bomb:** a fat colony bomb standing nose down (pivot on the nose tip, the impact point): white nose with a
  hazard ring, blue band, six amber arming windows, gunmetal tail cone, four fins in a ring shroud that spin.
- **Overcharged Crystal:** a containment rig (gunmetal tripod, blue drum, hazard ring, a cyan containment ring) whose
  three white clamp arms hold a **twisted pentagonal Crysium spire** with three fused side shards (not a quartz
  prism) and three splinters orbiting it. The crystal glows a hotter pink-lilac than our tamed Crysium: it is the
  unstable weapon. Overload: it shakes harder, swells, the clamps spring open, the splinters fly out.
- **Toxin bomb:** a stubby canister bomb, nose down: white shell with hazard bands, four sight windows of glowing
  toxin (the Acid Sprayer's chemical green), four vent valves on bones that pop and spin, blue tail fins.

## Drones (round-9 family: white shell, blue band, gunmetal belly, ducted rotors with white lips)

- **Builder** (cutscene, welds the base together): a **tri-rotor** (a Y of ducts, unlike the repair quad's X), a
  hazard ring, a signal-yellow beacon dome on top and two **welding arms** under the nose with blue-white torch tips.
- **Hauler** (cutscene, ferries Crysium to the base in a swarm): a light **tandem** lifter, one big duct fore and aft
  on a long spine, four claws gripping a cargo pod with amber ore windows (its own mesh, so Unity can light it).
  Kept at 1,376 triangles for swarms.
- **Bombard** (Pathfinder Strike, the hero's ultimate): a **flying wing**: white delta wings with cyan leading edges
  and yellow chevrons, two embedded ducts, twin thrusters, a bomb bay with two doors and two pairs of small bombs.

## Review passes

- Pass 1 (game zoom next to the parents): all five hybrids read as their two parents. Budget was the problem
  (Storm Flak T3 6,740, Thermal Shock T3 6,668): torus rings dominated. Coils and bands went to fewer segments, the
  Storm Flak T3 quad became the heavy pair, Thermal Shock lost duplicate glow strips and its T3 frost fins.
  Shatter's coolant cylinder was too thin to say "Cryo" and was made fatter and longer.
- Pass 2: the violet coil glow burned out to lilac-white under bloom; it is now more saturated and weaker.
  Gadgets: the Spike Strip read as solar panels (dark deck, taller brighter spikes now); the Barricade read as a
  thin line (taller slabs, a bolder hazard band on both faces, a brighter top light); small blue pieces lost their
  panel seams, which read as a grid; the Decoy's lure head grew.

# Round 19: the mothership rebuilt as a hero hard-surface asset (2026-11-04)

Brief: Remco after the opening cutscene playtest: "Het ruimteschip en de planeten waren erg amateuristisch. Dit moet
mooier." Round 13's ship was a truss stick with a capsule-shaped command pod, a torus and boxes: primitives on a
stick. Round 19 rebuilds it in `Tools/Blender/build_mothership2.py` (own pieces, own bake; `tower_kit` pieces, atlas
and export reused read-only) as the colony transport of Operation New Dawn, seen at medium to close range in orbit.
Same faction language as rounds 7, 9 and 13: white and light-grey panels, gunmetal structure, colony blue, a little
hazard yellow, cool cyan and white lights. Nothing copies a real ship, film or game design.

## Silhouette and hierarchy (big, medium, small)

Read from far as **wedge nose, big ring, boxy middle, heavy engine with three glows**; the old needle-and-pods read
is gone. Length 14.95 (nose -7.35 to bell exits 7.6), nose towards -Y as before.

1. **Command section** (y -7.35 .. -3.8): a lofted, tapered, angular hull (octagonal sections, flat deck, deep keel
   chamfer), not a lathe. On the deck a raked **bridge** with a dark glass canopy, gunmetal mullions and one thin cyan
   light line, and lit window rows along its flanks. Layered **armour plates** on both flanks follow the hull taper.
   A gunmetal sensor chin with a cyan eye, two forward sensor booms with nav lights, a big dorsal comm dish on a mast
   and an antenna farm with a yellow beacon. Lit deck windows along both flanks.
2. **Keel**: an octagonal gunmetal spine in segments with light-grey structural **collars** (hazard tags) between the
   modules and steel conduits along its flanks.
3. **Habitat ring** (bone `Ring`, radius 3.15): a box-section ring (not a torus) in eight sectors between raised
   pressure **bulkheads**, two rows of lit windows on the outer wall, colony-blue rails on both rims, a dark inner
   liner, cyan ring lights; four diagonal box-beam **spokes** with lift tubes and hazard-banded junction pods; a hub
   with a blue band and a glow ring between two static bearing collars on the keel.
4. **Cargo racks** (y -0.85 .. 1.95): three rows of ribbed containers on top and both flanks (white, light grey,
   colony blue, one yellow), held by light-grey rack frames and corner rails.
5. **Capsule bay** under the racks: an open hangar (gunmetal walls, lit ceiling strips, cyan wall strips) with three
   racked landing capsules and **clamshell doors** swung open 62 degrees, hazard-edged, amber lights at the mouth.
   Socket `Bay` at the mouth, pointing down.
6. **Tank ring**: four capsule tanks round the keel (white with blue bands, grey with hazard bands).
7. **Radiators**: on box booms, three dark hinged panels per side in a shallow V with white frames, dark spars and
   cyan coolant lines; cyan lights on the tips.
8. **Engine**: a light-grey shadow-shield disc with a hazard rim, then a lofted octagonal thrust block that widens
   aft (white over a grey belly with a blue pinstripe and band, a hazard band, the hull number and the NTI emblem on
   top), RCS blocks with small bells, a gunmetal thrust plate and **three bells** on gimbal collars with actuator rods,
   heat-tinted (straw to blue-violet towards the exit) with a pale-blue glowing throat (round 13's note "the bells
   burn almost white" is answered: the throat glow is weaker and bluer).

## Surface (all in the maps; one material)

- **Weighted normals**: edges above 50 degrees are sharp, everything else gets face-area weighted normals, so the big
  faces stay flat and the single-segment chamfers carry a soft highlight. Applied to the bake target and to both
  exported meshes, so the tangent-space normal map matches.
- **Plating** (ship-scale self-bake, seams about two texels wide at 2048): staggered rectangular panels (1.6:1), some
  split once more, a value and smoothness offset per panel, light-grey accent panels (white hull) or darker ones
  (blue, gunmetal), inset hatch outlines and vent slats in the normal and albedo, low mottling, AO grime.
- **Livery**: the command hull and the engine block are two-tone (white over a grey belly) with a colony-blue
  pinstripe on the line.
- **Windows**: lit rows from the bake (habitat outer wall, command flanks, bridge flanks), about a fifth of them dark
  so they read as rooms, not a stripe.
- **Decals** (text and flat shapes rendered to images in the build, projected in the bake): **NEW DAWN** with the hull
  code **NTI ND-01 COLONY TRANSPORT** under a blue rule on both command flanks, the NTI emblem ahead of it and on top
  of the engine block, and the engine number **01** with two bars on both engine flanks.
- **Material breakdown** in the packed mask: painted ceramic panels (smoothness varied per panel, accent panels a bit
  rougher), bare metal (gunmetal and steel, glossier on chipped edges), glossy glass and decals.

## Critique passes (preview renders, space lighting)

- Pass 1 (first full bake): the ring's grey-white patchwork was noisy and the white hull overexposed; the texel
  density was low (hidden lathe caps and two 6.8-wide annulus discs on the ring's side walls took the atlas). Fixes:
  ring and keel split into sectors and segments, per-piece texel weights (hidden and minor parts get fewer texels),
  seams widened to about two texels, fewer accent panels, hull value lowered, a dark inner liner on the ring. The
  ship's own shadow fell on the preview planet: the preview planet is now shaded in its material.
- Pass 2: the bridge window read as a white sticker and the bridge side light floated; the white masses had no value
  structure. Fixes: a dark canopy with one thin cyan line, shader windows on the bridge flanks, the two-tone livery.
- Pass 3: from below the bay was a black hole and the rack frames read as a heavy empty cage. Fixes: a lighter bay
  ceiling with two lit strips, thinner frames, brighter container mix; a dedicated `bay` preview camera.

## Known weak points

- The bay interior is dark when the sun is above the ship (no bounce light in the preview); in Unity a small point
  light at `Bay` would sell the launches.
- The ship is still mostly white: it reads clearly in space, but from the far side in shadow it is a flat grey mass.
- Texel density is about 0.017 units per texel at 2048: fine at cutscene distance, soft in a frame-filling close-up
  of the nose (the tiling metal and ceramic detail normals help in Unity).
- The containers are plain ribbed boxes; the rack frames are simple bars.


# Round 19: Dr. Elena Voss rebuilt as a modelled, stylised character (2026-11-04)

Playtest: "Die dr Voss is heel simpel en lelijk." Curator direction: stylised (Mercer's chunky readable family), an
appealing film-like face instead of the procedural blob, real eye/mouth topology, believable but matte skin, real hair
volume. Scripts: `build_voss2.py` (orchestration, rig, keys, clips, export, previews), `voss2_head.py` (head field and
topology), `voss2_body.py` (figure), `voss2_gear.py` (headset, pin, badge, buttons, pens), `voss2_paint.py`
(texel-space painters), `portrait_kit.py` (generic: SDF toolkit, parameter-space meshing, part assembly with weighted
UV packing, G-buffer painting). `build_voss.py`, `story_kit.py`, `suit.py`, `common.py` untouched.

## How the head is made

- **Shape:** a signed distance field sculpted from soft volumes with smooth unions: cranium, one tapered jaw ellipsoid
  (an oval to a small chin, no jaw lumps), chin point, soft jaw angles, frontal cheekbones, cheek fill (no gaunt
  hollow), shallow sockets, lid spheres round the eyeballs, a short straight nose with a defined tip and narrow wings,
  upper and lower lips, ears with a bowl and lobe, a neck of natural length. Landmarks: eyes on y 1.44 (as before),
  nose base 3.4 cm and mouth 4.8 cm below the eyes, chin 8.4 cm; the head is drawn 6 % large (stylised).
- **Topology:** laid out in a 2D parameter space round a centre in the head (azimuth x a metric-remapped elevation, plus
  an azimuthal patch for the crown), with five concentric constraint loops round each eye opening and five round the
  mouth, a size field (1.6 mm at the eyes, 1.7 mm at the lips, ~4 mm on the face, ~8 mm on the scalp), constrained
  Delaunay + relax, then every vertex shot outwards onto the field and relaxed on it. The eye openings are real holes
  whose rims roll onto the eyeballs (lid margin, tuck); the mouth slit rolls into the lips and closes in a mouth bag.
- **Eyes:** separate eyeballs with a cornea bulge (the highlight slides), steel-blue iris with fibres, warm collarette,
  limbal ring, lid shadow on the sclera; geometric upper lashes lifting at the outer corner; painted lash line and
  waterline.
- **Hair:** part of the same field: a cap that thins to nothing at the hairline (no ledge), 14 big sculpted locks combed
  to the bun with a finer second set, fuller at the temples, a side-swept fringe from a parting on her left (two
  locks) plus a lock sweeping back on the left, and a coiled bun (1.7 turns round a core) with a pin. Painted strands
  follow the comb lines (round the coil on the bun, along the lock on the fringe), clumps, dark roots, a soft sheen.
- **Skin:** matte, painted warmth on cheeks, nose, ears, chin, warm lids with a crease shadow, a cool touch under the
  eyes, soft painted brows (dense lower edge, feathered top), lips with a vermilion border, darker corners and soft
  vertical lines in the normal map. No pores.

## Figure (new: full body)

Lab coat to the knee (A-line, buttoned to the chest, notched lapels with thickness, rolled stand collar, set-in sleeves
with armhole seam and turned cuffs, hip patch pockets she keeps her hands in, breast pocket with two pens, five
buttons, side/back seams, hem stitch, the colony-blue stripe along each shoulder ridge, the NTI hexagon patch with gold
nova on her left sleeve), the navy ribbed mock-neck top with the cyan-lit rim, slim slate trousers, dark boots.
Headset: a band hugging the hair, cyan-ringed earpiece on her right ear, a pad on the left, a boom that keeps clear of
the cheek with the mic in front of the mouth corner. The yellow NTI badge (photo, navy hex with gold nova, text lines)
clips to her left lapel. The coat albedo is a touch below white (curator note: it burned out in the portrait light).

## Rig, keys, clips

Same 10 bones (`Root, Spine, Chest, Clavicle_L/R, Neck, Head, Jaw, Lid_L, Lid_R`), heads moved to the new anatomy;
weights are computed by region: upper lids per column (weight = that column's closing angle over the centre's, falling
off over three loops), the jaw by position (hard split across the lips, fading over the cheeks, towards the ears and
the throat), the hands-in-pockets forearms blend to Spine so the breath does not pull them out. Clips Idle 120, Talk 90
(jaw up to 8 degrees), Blink 8 as before (Lid bones rotate +X by 46 degrees). Face shape keys Blink, Smile, JawOpen,
Frown (exact lid/jaw rotations, soft displacement fields).

## Review passes (what was seen and changed)

1. Shape field: snout-like muzzle, weak chin, deep sockets -> rebuilt from landmark measurements; the nose undercut
   artefact fixed by relaxing on the field.
2. Topology: neck undersampled and anisotropic -> metric from true surface derivatives and an elevation remap; the
   crown patch was wound inward (dark seam) -> flipped.
3. Face read skeletal, then pear-shaped, then beak-like (overcorrected taper) -> tapered jaw at 0.72, cheek fill,
   smaller nose, larger and closer eyes, heavier lids (less white), lower and fuller brows.
4. Textures: the face had too little texel density (LSCM gave the neck most of the island) -> separate face island at
   2.4x; barcode brows -> soft painted brows; orange plastic hair -> darker, rougher, less sheen; a skin gap under the
   fringe -> hairline lowered there; the hairline zigzag -> the cap thins to nothing at the hairline.
5. Full build: shape-key values left non-zero (closed eyes, open mouth at rest) -> zeroed before export; mouth corners
   and chin blotches (texels on coarse triangles tested "inside" the mouth) -> region tags from the mesh; the mic sat
   on the skin -> boom kept off the cheek; yoke stripe smeared down the chest -> drawn along the shoulder ridge; neck
   still long in 3/4 -> mock collar 1.4 cm higher.

## Honest weak points

- The hair is a smooth sculpted helmet with painted strands: it reads as hair at portrait size but has no flyaways or
  strand cards and the strand texture is a little wood-grain-like close up.
- The jaw/cheek on her right still bulges slightly in a strict 3/4; the chin is a touch long and pointed.
- The mic and boom cast a visible shadow on the chin and jaw in key-lit portraits.
- Partial blink columns sink a little into the eyeball at mid-blink (linear skinning); the shape key is exact.
- The mouth bag is a simple pouch (no teeth, no tongue): fine for the 8-degree Talk, dark when wide open.
- Triangles (head ~7.8k) are a dense Delaunay soup with loops round eyes and mouth, not an all-quad animator's mesh.

## Round 20: the S-69-CRYS world-map planet (`build_planet_s69.py`, 2026-11-04)

The level-picker globe was a procedural Unity paint (round blobs, white splotches, scattered dot islands). Round 20 bakes a
real texture set in numpy inside Blender: `ArtSource/Exports/Planet_S69/` (4096 x 2048) and `Low/` (2048 x 1024).

- **Layout = CampaignCatalog.** Each surface continent is a set of hand-placed elliptical lobes (plus a few carved bays)
  around its MapRegion centre, domain-warped twice and broken up with 0.36 x mid-frequency and 0.11 x fine fractal coast
  noise (gain 0.55, so detail at every scale). All 20 Crystal Plains nodes are on land, in region C1, at least 5.8 degrees
  from the coast (checked every build, listed in the report). Northern Crown and Southern Crown reach their poles; the
  Core (C6, underground layer) shows on the surface as a ragged crater (radius ~9 degrees) on Southern Crown's northern
  arm at (150, -32).
- **Biomes per region:** Crystal Plains teal and lavender grassland, sage patches, slate-violet ranges with pale lilac
  (not white) peaks, meandering rivers and glowing cyan crystal fields; Ashen Reach charcoal and ash with ember/rust dune
  seas (striped dunes), dark basalt flows, broken lava rifts and vents; Northern Crown snow cap, blue glaciers, slate
  ridges, faint teal crystal veins (hint B); Mirewood dark green-teal canopy, olive bogs, many lakes and channels, faint
  green bioluminescence; Southern Crown black glassy ice, frost on the heights, magenta vein network; the Core dark
  violet floor, broken rim, a central spire, violet fractures.
- **Ocean:** depth from blurred land masks at 2, 4.5 and 13 degrees (turquoise shelves, navy deep), four trenches with a
  violet tint, a slight per-region sea tint, zonal current streaks, sea ice and floes beyond ~75 degrees.
- **Archipelagos only where meant:** the Shard Isles (Crystal Plains to Ashen Reach), a volcanic chain east of Ashen
  Reach, Mirewood's outer keys and a mid-ocean arc. No random dot islands.
- **Clouds:** a separate cover map, twelve cyclones (point twisted around each storm axis, CCW north / CW south),
  two warps, zonal stretch and latitude belts (equatorial band, clear subtropics, storm tracks).

### Review passes

1. Clouds covered most of the globe and storm eyes were rings; polar lobes wrapped into planet-wide bands; interior
   bays left holes; uniform cyan surf outline; straight orange rift strokes; the Core read as a target -> cover cut,
   eye ring gone, polar lobes smaller, carves moved/removed, surf 0.14 and broken by noise, rifts jittered and broken
   into segments, crater rings replaced by noisy fractures.
2. Many small cloud streaks; a lake in Northern Crown; the Core arm a perfect disc (guard too strong); white snow blobs
   on tropical Crystal Plains peaks -> wisps only near banks, carve removed, crater guard 0.22, lilac peaks.
3. Relief invisible under camera-side light -> mild baked relief cue in the albedo (highlands lighter, steep flanks
   darker); a black Core floor -> dark violet with structure.
4. Preview renderer only: night side too bright, wide twilight band, big soft sea glint -> fixed.

### Honest weak points

- Continents still read as "warped lobes" at a glance; no plate tectonics, rivers do not flow downhill (ridged-noise
  channels), and Crystal Plains is a broad sheet of two tints rather than distinct sub-biomes.
- Crystal fields at close zoom look like cyan worm networks; Southern Crown is very dark on the day side.
- The equirect pinches at the poles: the polar caps have some east-west streaking in the noise detail.
- The emission has no colony lights; Unity's `GlobePainter` adds them per built level, keep that as an overlay.
- Previews come from a numpy ray-caster (same UV maths as Unity), not Cycles.

# Round 20: Dr. Elena Voss on MPFB 2 (MakeHuman for Blender, 2026-11-04)

Playtest on round 19: "driehoekig gezicht, het lichaam heeft geen vrouwelijk figuur, heupen, borsten". Remco approved
MPFB 2 (GPLv3 add-on, CC0 assets and output). Scripts: `build_voss3.py` (orchestration, culling, atlas, rig, keys,
clips, export, previews), `voss3_human.py` (the MPFB stage: human, expressions, pose, numpy capture/cache),
`voss3_cloth.py` (clothes and gear measured off her body), `voss3_paint.py` (atlas painters). Round-19 modules are only
imported read-only (voss2_body lofts and sweeps, voss2_paint palette and badge painter).

## Human

- MPFB phenotype: female, age 0.615 (about 40 in MakeHuman's scale), average weight and muscle, proportions 0.75,
  height 0.55, cup 0.52, 90 % caucasian. Natural adult proportions (waist, hips, bust) straight from MakeHuman,
  non-sexualised.
- Mild stylisation only through MakeHuman targets (`voss3_human.TARGETS`): head-oval, softer chin and jaw drop,
  slightly fuller cheekbones, eyes +24 %, a finer nose (narrower tip and wings, no hump), slightly thinner and shallower
  lips with the corners a touch up, a slightly shorter forehead. 30 % of the faceunits smile is baked into the rest face.
- Assets: young_caucasian_female skin; high-poly eyes (eyeballs only, the transparent cornea shell is dropped for the
  opaque game shader); eyebrow010 (resampled into the skin from the cards' alpha, not exported); teeth_base
  (decimated); rehmanpolanski_hair_bun_brown (hair01 pack); shoes04 (sock cut off, under the trouser hem); the MPFB
  default rig. eyelashes02 was tried and dropped: opaque cards read as eyeliner wings, shortened ones as stripes on the
  lid, so a lash line is painted along the lid rim instead.
- Pose: hands in the coat's hip pockets (two-bone IK on MPFB's arms), thighs turned in 3 degrees. Scaled so the eyes
  sit on y 1.44 (stature 1.556 m with shoes) and the bust framing still fits.
- Expressions from faceunits01 (ARKit-style units), with every proxy refitted to them: Blink (eyeBlink L/R), JawOpen
  (jawOpen 0.7), Smile (mouthSmile 0.75 + cheekSquint 0.35), Frown (mouthFrown 0.7 + browInnerUp 0.35 + mouthPress 0.2).

## Clothes

Same design as round 19: coat to the knee with lapels, stand collar, hip pockets with her hands in them, breast pocket
and pens, five buttons, the colony-blue shoulder band and the NTI sleeve patch; the navy ribbed mock-neck with its cyan
rim, slate trousers, dark shoes; the headset with the cyan earpiece on her right ear and the yellow NTI badge on the
left lapel; the bun now has a navy tie. New: everything is measured off the MPFB body. `voss3_cloth.Field` takes
horizontal slabs of the body's vertices, builds the mirrored 2D convex hull of each slab and stores ray distances per
angle. The coat uses that field plus ease, with no concavity from bust to waist (the cloth spans it) and a monotone
skirt below the hips that flares A-line. Sleeves and trousers are swept along the posed limbs, with radii from the
limbs' support function.

## Texturing

One 2048 atlas (Low 1024), one material. The MPFB parts keep their own UV islands (packed with the clothes, face at
2.4x density), and their textures are resampled through a `SrcUV` G-buffer channel. The skin is simplified for the
game: colour from a 26 px blur (no pores, stubble or chin shadow), only a 2.5 % trace of the 3 px detail, painted warmth
on cheeks, nose, ears and chin, lips from the lips group, a lid tone, the lash line, the brows from the brow cards, a
dark mouth inside (texels whose forward ray meets the face again), the scalp group painted as combed hair where the
hair shell leaves it bare, and hair-coloured skin just under the hair's edge. Eyes: blue_eye tinted steel-blue, with a
lid shadow on the upper sclera. Hair: recoloured to her dark warm brown with damped strand contrast. AO is baked with
the headset moved away, so the boom leaves no stain on the cheek.

## Review passes (what was seen and changed)

1. Raw MPFB with the first stylisation read as a teenager (big cheeks, baby face) -> fewer cheek and eye targets, a
   natural chin, a slightly shorter forehead; now adult and oval.
2. Geometry fit: the coat was cut flat at the shoulders and the sleeve caps were pointy -> the coat body now runs up
   past the collar line and the sleeves start lower. The trousers flared like bell-bottoms from MPFB's sock and toe
   vertices -> sock cut, toe points excluded, radii capped, feet closer together.
3. First paint: panda eyes (no rim found), a dark box round the mouth (the mouth rule caught the cheeks), blue specks
   in the hair (tie detector), black eyeballs (the cornea shell was kept) -> rim from the blink target, an occlusion
   test for the mouth, a stricter tie test, the eyeballs kept.
4. Lashes read as wings, then as stripes -> dropped for a painted lash line along a binned, smoothed rim. Full lips ->
   lip volume and depth reduced. A notch at the temple next to the hair -> scalp group painted as hair. The cheek smudge
   turned out to be the boom's cast shadow (boom moved 1.5 cm further out; see weak points).
5. Final: the rest face carries 30 % of the smile; the nostrils are no longer caught by the mouth rule.

## Honest weak points

- The hair is MPFB's bun shell rendered opaque. It reads well at portrait size, but close up the texture shows a light
  streak with a dark crease on top, and the side edge above the ears is jagged (softened by painting, not modelled).
- The skin is deliberately simplified and can look a bit smooth or porcelain-like in close-ups; the skin has no normal
  detail.
- The headset boom still casts a soft shadow on her right jaw under a key light from camera-left (lighting, not
  texture).
- The lips are still on the full side, and the lower face reads slightly heavy in a strict 3/4 view.
- The painted lash line is thin; there are no 3D lashes (the opaque shader cannot do alpha cards).
- The shoes are MPFB's shoes04 low shoes in dark leather under the trouser hem, not tall boots.
- Stature 1.556 m: she was scaled to keep the eyes on y 1.44 for the existing bust framing, so she is short next to
  Mercer.
- 25,228 triangles: the MPFB head and ears stay undecimated for the face (hair at 0.68, teeth at 0.12, shoes at 0.4).

# Round 20: VFX texture set (projectiles, muzzle flashes, explosions, smoke, impacts), 2026-11-04

Remco: "Kun je ook de wapen projectiles en explosies mooi maken." This round makes the texture source the Unity VFX
rebuild will use; nothing in `Assets/` changes. Script: `Tools/Blender/build_vfx.py` (+ `vfx_kit.py`, `vfx_2d.py`,
`vfx_sim.py`). Output: `ArtSource/Exports/VFX/` (+ `Low/` at half size) and `VFX_report.json`.

## Design rules (from Remco's feedback, memory "user preferences")

- **Projectiles are glowing rounded bars with a light trail, never light balls.** `Bolt_Core` is a capsule with a
  white core and a halo that leans backwards; the plasma round is the same bar inside a churning mantle; trails are
  separate ribbons (`Trail_Ribbon` + scrolling `Trail_Noise`) so the bar stays crisp.
- **Beams and bolts leave the barrel**: the side-view muzzle flashes and the tesla arcs start at a fixed point
  (U 0.05 / U 0.02, V 0.5) so Unity can pin them to the muzzle socket.
- **Impact is felt locally**: every big hit has a ring (`Shockwave_Ring`) and a refraction map
  (`Shockwave_Distort`, `HeatHaze_Distort`) for the existing `Game/ImpactRipple` idea; no full-screen flash anywhere.
  Flashes in the flipbooks last 2-4 frames and stay small (energy impact, sparks, cryo).
- **Tintable**: emissive textures are white-hot core (R) + glow mask (G) so one texture serves every weapon colour from
  `TowerDefinition._color`; smoke is lit greyscale (six-way) so it takes each level's palette; fire is a temperature
  channel mapped through a gradient.
- **Cool vs warm colour code**: our weapons stay cyan/blue/white/yellow in the tints (bolts, rails, cryo, tesla violet),
  fire and acid are warm and organic.

## How it is made

- **Fireballs, smoke and dust** are Mantaflow gas sims (Replay cache, 112-192 voxels on the long axis). Each kept frame
  is read from the evaluated domain and rendered straight away by a numpy orthographic ray-marcher (camera on -Y):
  alpha, a fire temperature channel and six single-light maps (single scatter plus a soft multi-scatter lobe), packed
  as Unity's six-way lightmaps. Only the 2D maps are cached (`%TEMP%/td_vfx_cache/<Name>.npz`), never the 3D grids.
  Fire is seen through its own thin gas (only the non-burning soot occludes it), so a long path through the centre
  reads hotter than the rim: white core, orange body, red edge, dark soot as it rises. The emitters are lumpy spheres
  that grow over 4-8 frames (that, not initial velocity alone, is what makes the fireball bloom), the boss blast
  chains three sub-burst waves. The last 18 % of every flipbook fades to empty so particles never pop.
- **Everything else** is procedural numpy: periodic Perlin noise (tiles and time loops are seamless by construction),
  distance fields for bars, petals, segments and polygons, midpoint-displaced branching bolts, a small 2D particle sim
  for sparks, metaballs with a derived normal and Blinn highlight for the acid, warped voronoi for scorch cracks.
- Previews (contact sheets, relight sheets, channel strips) are shaded in numpy the way the intended Unity shaders
  will be (six-way weights = squared light direction components; CG pack = tint x G + white x R; ACES-ish tonemap).

## Critique passes (what was seen and changed)

1. Explosions, pass 1: the fire burned out in ~10 frames and the ball stayed small -> growing emitters, more fuel,
   lower burning rate. Pass 2: the fire read as a flat orange shell (smoke in front hid the emission) -> emission is
   occluded only by non-burning soot, so the core is hot and the rim cools. Pass 3: vertical hair in fire and smoke
   (single-voxel rising filaments) -> one-voxel 3D smoothing before the render; the fireball whited out for 5 frames
   -> lower emission gain; smoke clipped flat against the domain ceiling -> taller domains; six-way maps too similar
   (relight barely visible) -> stronger single-scatter share; preview smoke too pale -> dark albedo 0.17.
2. Projectiles: bolt halo had a notch at the tail -> halo built from two capsules; plasma round was a blob ->
   rounded bar + mantle + ridged tendrils streaming back, smaller halo; rocket flame was a uniform sausage -> heat
   falls off along the plume, stronger scrolling turbulence, diamond-shaped shock cells; tesla arcs were smooth curves
   flattened against the cell border -> displacement proportional to segment length (jagged at every scale) and
   paths scaled into the band instead of clamped, thinner glow; trail and beam noise were mostly white -> normalised
   ridged noise.
3. Muzzle flashes rendered black (NaN from a zero-width petal) -> epsilon; side petals were too long and white,
   flak jets left the cell -> shorter, sharper, dimmer cores; front-view halos filled the cell -> smaller.
4. Impacts: sparks clumped near the centre -> less drag, longer flight and motion blur, shorter flash; scorch cracks
   read as a tiled polygon pattern -> warped, broken crack network and longer soot rays; energy ring lingered ->
   thinner, faster fade.

## Honest weak points

- The sims run at 112-192 voxels: billows are soft at 256 px per frame, there is no wavelet-turbulence upres. Close
  to the camera (boss deaths on a zoomed map) the smoke can look a little smooth; a noise-upres pass would fix it at
  ~4x the bake time.
- Six-way maps are from an axis-aligned orthographic view; billboards seen at the game's 50 degree pitch are fine,
  but there is no "seen from above" variant.
- The fire temperature is an exposure of integrated flame, not a physical blackbody; the gradient in Unity decides
  the final colours (`fire_ramp` in `vfx_kit.py` is the suggested one).
- Muzzle flashes and the rocket flame are stylised 2D shapes, not sims; up close they read as graphic, which suits the
  look but is less rich than the fireballs.
- Acid splash droplets fall out of the bottom of the cell in the last frames (intended: they leave the frame) and the
  drips are simple; there is no puddle decal yet.
- The boss blast's three sub-burst waves merge into one large fireball instead of reading as separate pops; for a
  readable chain, spawn 2-3 Explosion_Large particles with delays in Unity on top of it.
- Dust_Burst is a rounded dome that settles, not a flat radial sheet; it reads as a ground puff more than a shock.
- Lightning arcs are 16 fixed shapes; at 20 fps the repetition is not visible, but a very long-lived arc should pick
  random frames.

# Round 21: S-69-CRYS planet, balanced world layout (2026-11-04)

Feedback (Remco): Crystal Plains "neemt bijna een kwart planeet in beslag", the world is out of balance.

## What changed

- **Layout is data:** `ArtSource/Exports/Planet_S69/layout.json` holds the region centres (unchanged from
  CampaignCatalog), surface targets, lobes, ranges, rifts, trenches, archipelagos, river counts, the Crystal Plains
  sub-biome seeds and a new proposal for the 20 Crystal Plains level nodes. `build_planet_s69.py` reads it; a per-region
  coast bias is solved by bisection so every region hits its target share.
- **Surface share (measured on Regions.png):** Crystal Plains 5.98 %, Ashen Reach 7.0 %, Northern Crown 6.78 %,
  Mirewood 6.77 %, Southern Crown 6.27 %, The Core 1.48 % (crater, radius 13 deg, id to 14 deg), ocean 65.7 %
  (round 20: Crystal Plains 8.8 %, Core 0.76 %). Crystal Plains is now the smallest major continent, not the biggest.
- **Shapes:** coast noise per region (`coast_roughness`) plus a ridged "tectonic grain" term; Ashen Reach is two
  plates split by a rift gulf from the south ending in a rift lake; Mirewood is a crescent around a western bay;
  Northern Crown has southward capes and one inland sea; random inland holes were removed.
- **Crystal Plains:** five sub-biomes (glass meadow, bluefern lowland, amethyst upland, shard field, salt basin) from
  a warped soft partition; crystals are clustered outcrops (cellular noise inside blob fields) instead of worm-like
  vein networks; rivers glow faintly cyan at night.
- **Rivers:** traced by steepest descent from mountain belts to the coast (21 rivers); only Northern Crown and
  Mirewood may end in basin lakes; widths grow downstream, valleys are cut into the relief.
- **Southern Crown** is lighter slate by day and its polar land is an ice sheet matching the sea ice beside it.
- **Polar streaking:** latitude-stretched noise (clouds, currents, glaciers, sheen) fades to isotropic near the poles;
  the sea-ice edge is lobed.
- **Nodes:** 4-row serpentine at bearing 14 deg, 9 deg steps; min spacing 8.25 deg (Unity rule 8), every node on C1
  land, min 4.3 deg from the coast (check in the report, `nodes_ok`).

## Critique passes

1. Smaller Crystal Plains, but the nodes' land guard made it a rounded rectangle; sub-biomes read as hard patches;
   lake-ending rivers left black holes; mountain belts lay offshore. -> guard radius 5.5, softer partition, lakes only
   C3/C4, belts moved inland.
2. Other continents still blobby with holes -> redesigned lobes (rift gulf, crescent bay), roughness up, holes removed;
   Core floor lifted, dune stripes softened.
3. South pole: grey land beside a white sea-ice patch -> polar ice sheet on the land; volcanic isles shrunk; crystal
   fields given a pale sheen so they read from orbit.

## Honest weak points

- Crystal Plains still has a fairly compact core because 20 nodes at >= 8 deg spacing need ~4 % of the planet; a
  smaller continent needs a smaller node spacing rule in Unity.
- Rivers are thin at globe distance; Crystal Plains has only a few short ones.
- The Core crater is large and dark; on the map it reads like an eye.
- Ocean 65.7 % is a hair over the 55-65 % brief.

# Round 22: S-69-CRYS planet, Crystal Plains as the small starting continent (2026-11-04)

Curator: round 21 still read as a big slab facing the camera. Node spacing rule may drop to 6 deg (Unity side).

- Surface share (Regions.png): Crystal Plains 4.02 %, Ashen Reach 6.49 %, Northern Crown 6.59 %, Mirewood 6.36 %,
  Southern Crown 6.07 %, The Core 1.48 %, ocean 69.0 %.
- Nodes: a 3-row serpentine (7 + 7 + 6) along a SW-NE band at bearing 18 deg with a slight arch, Landing Site in the
  south-west; min spacing 6.23 deg (`node_min_spacing_degrees` 6 in layout.json), min 3.5 deg from the coast, all
  within ~26 deg of the region centre.
- Shape: an elongated band of three lobes with a SW peninsula (Landing Site side), a long NE peninsula with a ridge,
  a NW cape, a southern horn and an inland bay biting into the south coast. Node land guard radius 4.6 deg.
- Ashen Reach dunes: a global sine pattern (which aliased into stripes) replaced by warped, stretched ridged noise.
- Weak points: the land is still mostly the area the 20 nodes need (about 3.3 % at 6 deg spacing with a 3 deg coast);
  crystal outcrops read a little like bubbles up close.

# Round 22b: organic Crystal Plains level route (2026-11-04)

Remco: "het is mooier om de levels iets willekeurig uit elkaar te hebben". The serpentine rows are replaced by a
seeded guided random walk (`build_planet_s69.py --placenodes`, settings and seed 2211 in `layout.json`
`node_placement`): it follows a loose three-lane sweep with a detour onto the north-east peninsula, steps 6.2-10.4 deg
(mean 7.6), every next node is the previous one's neighbour (no other node closer than 0.75 x the step), no route
crossings, no segment within 3 deg of an older node, every dotted segment over land, mountain belts avoided. It runs on
the guard-free layout field (Crystal Plains at 4.5 % for placement), so it depends only on layout.json + seed; the
texture build then keeps land around the nodes (final 4.0 %). Weak point: re-running the placer after a lobe change
gives a new route; the positions in layout.json are the source of truth.

# Round 24: liquids and mist (2026-11-04)

Remco: water, lava and mist look fake. The Unity versions are flat sheets with scrolling line patterns (water: blue with
white squiggles; quicksand: yellow with wavy stripes) and a uniform glow.

What makes a liquid read as real from the 44 deg ortho camera, and what this set gives the shader for it:
- **Depth colour**: shallow light, deep dark. Everything is designed around a depth / distance-to-bank value; the
  textures stay neutral.
- **Water**: two FFT-ocean normal maps (swell + ripple) for dual scrolling; real caustics (photon splat through an
  animated, loop-quantised ocean onto a flat floor) as a 16-frame flipbook, only in the shallows; foam as packed bubbles
  (round holes that close where the foam is dense; the first F2-F1 lace went spiky), moving shore crests from a profile
  ramp driven by distance to the bank and broken up by a dissolve noise. Critique 1: foam everywhere (the preview's
  distance image lost its pixels when its colour space changed: set it first). Critique 2: swimming-pool turquoise ->
  darker shallow, depth range 0.8, alpha 0.72-0.96; crest lines copied the bank outline -> phase noise. Critique 3:
  reads as a deep pond with shallow edges; the swell is subtle at game zoom.
- **Lava**: cooled basalt plates (warped periodic Worley, domed, ropes, shrink cracks) in two layers with different
  plate sizes; a slow noise picks which layer is on top, so plates break up and re-form; heat from seams, a halo and a
  molten underflow; hotter in the middle. Critique 1: the crust glowed orange everywhere with crackle-glaze cracks ->
  darker albedo, fine cracks 0.15, halo 0.1, ramp black until 0.3, bigger plates. Critique 2: open molten patches in
  the hot middle. Remaining: the underflow shows faint rings in the molten patches.
- **Bog**: murky normals, a duckweed-like alien film with glowing spots that collects at the bank. Critique: too much,
  too pale scum -> coverage threshold up, darker leaves and water.
- **Quicksand**: the ripple bands are contour lines of a periodic stream function and the flow map is its curl, so the
  two-phase flow moves sand along the bands (swirls). Critique: the first version was flat light grey -> bigger bands,
  stronger relief, sand tint. Remaining: close up the bands look a little like wood grain.
- **Ice**: milky layers (the first sine layering read as agate rings; now subtle), trapped bubble chains, a partial
  fracture network with soft fracture planes, frost growing from the bank through a distance threshold (fern-like edge).
  Critique: the frost fringe glinted like electricity -> steeper threshold, sparkle 0.3. Remaining: light ice; tint it
  down on dark Northern Crown levels.
- **Corruption**: viscous violet with vein networks (warped Worley borders; ridged noise gave contour loops) and
  travelling pulses. Remaining: a few small closed loops in the capillaries; veins fairly dense.
- **Mist**: three soft layers with real gaps (banks, wind streaks, haze), wisp and steam flipbooks with self-shadowed
  tops. Critique 1: ink-blot edges and cheese holes -> softer smoothsteps, beta 3; vertical streaks in the card shading
  -> blur before the shading integral. Critique 2: too dense in the preview -> preview alphas 0.22/0.14/0.09; in game
  keep 0.16/0.1/0.06.

Weak points: previews are lit brighter than the game; the bubble/ring/wisp flipbooks are checked on texture sheets, not
in a scene; Low previews not rendered.

# Round 23: living spawn origins (`build_origins2.py`, 2026-11-04)

Remco: "De spawnpoints moeten onzichtbaar zijn bij het begin, en die gaten moeten openen uit de grond, daarna is het
een vette spawn waar de enemies uit komen. Dit geldt ook voor de kleinere in het water, lava etc." Follow-up answers:
the openings cannot be destroyed (no Close clip), the player must see at level start where creatures will come from
while the ground is still closed (a subtle **Marked** look: pulsing rings and glows, no arrows), and an origin opens
only when something actually comes out of it in that wave (Open is a one-shot, a few seconds before its first
creature). Same rules as rounds 2-22: Earth only as a start, matte layered surfaces, warm alien light, read at the game
camera first.

## One rig language for six origins

Every origin is one skinned mesh on one rig. The **bind pose is Closed**: the maw (funnel, ridged collar, teeth,
claws, tendrils, gullet), the debris chunks and the marker organs wait `D` under the pivot (1.25 for the burrow, 0.9
for the liquid ones), where the terrain or the pool bed hides them. Only the plates sit at the surface. So a renderer
that plays no clip shows nothing but flush plates. Open lifts the maw by `D` out of the ground, so it really comes
out of the earth. The terrain hides the part that is still below z = 0. There is no hole in the terrain: the maw's
gullet sits a few centimetres above the pivot and the creature pops through it.

- **Origin2_Burrow** (land, about 2 cells of maw, 3.4-3.8 with what it throws). Earth start: an ant-lion pit crossed
  with a sea anemone. Departures: 13 ground plates (5 centre wedges in a star, 8 ring segments) whose tops are
  **painted with the continent's ground set** (camo); a fleshy oxblood funnel with raised radial chitin ribs, glowing
  pores in the grooves and a foot crusted with soil; a segmented collar with glowing joints; nine hooked bone-tipped
  teeth that close over the gullet like a cage and unfurl; two big asymmetric hooked claws (one much taller); four
  tendrils with glowing nodes that lash out over the ground; a white-hot amber gullet with concentric throat rings.
  Six basalt chunks fly at the burst. The 8 ring plates hinge at their outer edge and flip outward (150-170 deg), so
  their root-veined, warm-lit undersides form a star of petals round the maw. The 5 centre wedges are thrown and land
  upside down 1.5-1.75 out.
- **Origin2_Water**: a fleshy wet lip just above the surface, six translucent-tipped ribs curling over the throat
  like a closing hand, three short tendrils, glowing pores. Marked: seven glowing pore-beads bob at the surface in a
  ring. Open: the beads start to circle and are drawn in (the whirl begins), the maw rises turning 1.25 turns (the
  whirlpool), the ribs unfurl.
- **Origin2_Lava**: a broken obsidian chimney with molten cracks in the flank and ember-crystal teeth. Marked: a ring
  of chilled crust plates floats on the lava, its seams glowing (a dark patch with a burning outline on the bright
  pool). Open: the crust cracks, 6 plates fold outward and prop up, 3 centre plates and 5 glowing lava bombs are
  thrown, the chimney erupts.
- **Origin2_Quicksand**: a sinkhole. Marked: five bone-hook tips poke through the sand and bob. Open: a ring of six
  sand slabs rises to the surface, then slumps inward into the funnel (hinged at the outer edge, -30 to -45 deg)
  while the funnel turns half a turn; five bone hooks unfurl round a dark eye with a faint glow.
- **Origin2_Ice**: a breathing hole. Marked: a ring of ice floes lies at the surface with glowing seams. Open: the
  sheet fractures, 7 floes push up and stand at 60-85 deg, 4 centre floes and 4 ice chunks are thrown, a frost-chitin
  collar with eight icicle teeth rises.
- **Origin2_Bog**: a gas mound. Marked: five gas blisters bob at the surface. Open: the mound swells out of the mire
  with a quarter turn, seven fat lip lobes open like a sphincter, three limp tendrils, four mud chunks.

The liquid origins keep round 14's rule: the Emission map is a **white mask** that Unity tints with
`NestColor(element)`. The burrow's glow is baked warm (amber gullet, red-pink cracks and veins).

## The Closed and Marked looks

- **Burrow Closed**: plate tops lie 8 mm above the pivot (flush; the sides go 7 cm down into the ground). After the
  bake, a camo pass rasterises the up-facing plate-top triangles in UV space and replaces Albedo, Normal (turned into
  each texel's tangent frame), AO and smoothness with the ground set sampled at the model's own xy / 3.0 (the sets'
  tile size). One FBX, one map set per ground set: the default maps are Crystal Plains, `Skins/<Continent>/` holds
  Albedo/Normal/Mask (+ Low) for the other five continents (the Emission map is shared). The tops get no displacement.
  The gaps between plates show the real terrain. What still shows: a faint darker star (the rounded plate edges and
  their 40 % dark hairline) if you look for it.
- **Burrow Marked** = the same pose with the Emission on: the bevel round every plate top is a hairline crack with a
  faint glow (mask 0.35), which draws a ring and a five-armed star. The pulse is the material's emission strength
  (Unity), so Closed is emission 0 and Marked pulses it. The `Marked` clip adds only a 2 mm breath of the centre
  wedges.
- **Liquid Closed**: everything is under the surface (plates 0.14-0.2 down, markers 0.25-0.3, the maw 0.9). Lava and
  quicksand are opaque in Unity; water, ice and bog are translucent, so they show at most a dark shape at depth.
- **Liquid Marked** comes from geometry: plates (lava, ice) or marker organs (water, quicksand, bog) come up to the
  surface and bob, so the glow cue works on opaque pools too.

## Open, Idle, Spawn

Open (96 frames): **Stage 1 tremor** 0-30 (plates jitter and lift 1 cm; their sides glow just below the top edge, so
the cracks glow as they lift; liquid markers start to move); **Stage 2 bulge** 30-54 (centre plates heave up to 11 cm
and tilt, ring plates heave 4-10 deg, the maw climbs to 0.32 under the surface); **burst at frame 54** (plates slam
open with a rebound, wedges and chunks fly on ballistic arcs and skid when they land, the maw surges out, overshoots
7 cm at about 62-70); **Stage 3** 66-96 (teeth and claws unfurl from folded with an overshoot, tendrils uncurl and lash,
the gullet opens from 0.3 to 1.15 and settles to 1.0). Idle (105, loop) starts on Open's last frame: a wave of
breathing runs round the collar, the throat pulses, teeth flex, tendrils twitch at 1-3x the loop rate. Spawn (30)
starts and ends on Idle frame 0: the collar heaves out and up, the throat swells by 30 %, teeth flare 20 deg, tendrils
lash (peak frame 6).

## Critique passes (renders in the session scratchpad, `origins2/<kind>/`)

1. First burrow: the funnel flank read as a smooth red cloth skirt, the plate cracks were neon outlines once open,
   the collar had candy stripes, the teeth were straight posts, and the closed plates showed a dark star (40 % too
   much hairline). Fixes: raised radial chitin ribs in the geometry (same function as the paint), glowing pores in
   the grooves, a soil-crusted foot, crack glow 0.85 -> 0.5 on a narrower band, collar joints half as wide and
   0.45, hooked five-point teeth, hairline darkening 0.75 -> 0.4. Water: the ribs were vertical posts (now they
   curl over the throat), the lip sat high on the water (lowered), and the Marked beads were too small (seven, a
   little bigger, now circling inward as Stage 1).
2. Lava: the preview pool was blown out (fixed in staging) and the flank was plain glossy purple (now molten
   cracks), the ember teeth now have glowing nodes. Quicksand: the collar read as an orange life ring and the Marked
   hook tips were invisible on sand. Now there is no collar, six bigger bone hooks, and glowing shell beads as
   markers. Ice: the near-white glow vanished on white floes (round 14's problem). The floes are now bluer and
   darker, with dark seams, and the ring and star read. A plate-UV problem showed too: the closed slabs were
   shredded by the smart-project fallback (1,076 faces). The build now uses deep_kit's closed-island cut, so there
   are 0 fallback faces and each plate top is one island.

## Honest weak points

See MODEL_TODO "Ronde 23". In short: the burrow's closed camo depends on yaw 0 and the Unity ground shader's tint;
water, bog and quicksand need VFX for the whirl, the bubbling and the sand spray; the quicksand origin is a mound
with hooks rather than a true funnel; the flanks are still a bit smooth up close.

## Proposed Unity behaviour

- **Level start:** every origin that a wave of this level will use starts **Marked** (burrow: emission pulse 0.35-1.0
  on a 2.5 s sine plus the Marked clip; liquids: the Marked clip), with the nest's light pool pulsing on the same
  sine. Origins no wave uses stay **Closed** (burrow emission 0 or the renderer off).
- **Trigger:** when the wave plan knows the first creature that will leave an origin in the coming wave, start `Open`
  **2.5 s (76 frames) before that spawn**, so the first creature climbs out of a settled maw. A warning cue at Open
  frame 0: a rising rumble, a small minimap/edge ping in the nest colour, and the Marked pulse speeding up over the
  tremor. The burst sound and the camera-shake tick go at frame 54.
- **After:** switch to `Idle` at frame 96 and play `Spawn` with each creature. The origin stays open for the rest of
  the level (it cannot be destroyed). A level that ends with origins still Marked may simply leave them.

---

# Round 25: UI art (`build_ui.py`, 2026-11-04)

Remco's rule: every visual part is made in a design program. The audit found the HUD/menu icons, the dialogue and
cutscene textures, the radar scope, the menu starfield, the model-preview backdrop and the map halo still painted in
C#, and the frames/buttons/bars coming from the third-party Kenney UI pack. This round replaces all of it with our own
set, built in Blender.

## Method

- **Geometry, not pixels.** Every icon and frame is flat 2D curve geometry (polygons, capsules, rings, sectors,
  45-degree chamfers) with a small bevel, laid out in one scene in pixel units (1 BU = 1 px of the 2x master) and
  rendered top-down with an orthographic camera in Cycles (emission only, 24-32 samples, 1.2 px filter). Glows,
  backdrops, the sweep, noise and the sky are shader node trees on quads (gradient, noise, white noise, attribute
  colours on 4000 star quads).
- **One light for the whole UI.** The shaded material maps the bevel normal against a fixed top-left light
  (-0.5, 0.5, 0.71): flat tops = 1.0 x colour, lit bevels up to +45 %, far bevels down to 0.42. No lamps, so every
  piece gets the identical bevel, and the flat top of a white icon stays a clean tint target (0.88 after 12 %
  headroom for the highlight).
- **Compositing in numpy** only: glow = the edge layer blurred, a soft dark rim under every white icon (readable on
  light and dark), premultiplied over, packing and the 9-slice previews. Layers (fill / edge / ornaments) are separate
  collections rendered one at a time so the glow comes from the edge alone.

## Style decisions

- **Shape language** = our build pads, gates and the NTI emblem: chamfered rectangles with the big cut on the
  top-left and bottom-right (20 px at 2x on panels, 14 on buttons) and small cuts on the other corners; hex rivets,
  hex studs, hex slider handles, a hex lattice on the preview backdrop.
- **Palette**: navy fills (#08121F, #0D1B2E, buttons #10284A), cyan edges (#4FD2FF, dim #2A6E96), ice white
  (#E8F7FF) and signal yellow (#FFD040) reserved for focus. Card accents blue/green/yellow/red keep UiSkin's four card
  and bar colours. Alien markers warm: BossMarker orange (#FF8A24) and EnemyOrigin red (#FF4020) with a red glow.
- **Focus is unmistakable** (gamepad from the sofa): focused = brighter fill, a 4.5 px ice edge, a cyan glow and four
  signal-yellow corner brackets outside the frame. Pressed sinks darker with a cyan edge; disabled is grey at 80 %.
  All states of one element share the same glow pad, so a SpriteSwap never shifts the layout.
- **Icons**: 40 clean silhouettes on one stroke weight (0.075 of the cell main, 0.045 secondary), 6 px bevel at 2x,
  white so the existing `Image.color` tints keep working; drawn inside the central 112 of 128 px with the dark rim in
  the margin. The first 28 follow `IconType` one to one (same metaphors as the code shapes: wrench, coin with a hex
  core, shield with a chevron, snowflake with a hex core, ...); 12 extras for things the HUD now draws with text or
  borrowed icons (StarEmpty, BossMarker, EnemyOrigin, Drone, Base, Gear, Close, Research, Power, Enemy, Credits, Info).
- **Dialogue** pieces stay white (DialogueView tints them) but now use the kit's chamfers; Panel and Edge share one
  outline so the light sits exactly on the border, as before. Name tag and portrait frame are new.
- **Radar** is one pre-coloured scope (teal on dark, steel bezel with four hex clamps, ticks every 10 degrees) plus a
  sweep wedge and a contact dot instead of seven rotated lines.
- **Starfield** follows `SpaceStyle` (band, nebula A/B, dust lane, star colours) at 2048 x 1152 with alpha = 3 x
  brightness, the painter's rule, so it still only adds a faint glow over a running level.

## Critique passes

1. *First icon sheet and kit sheet*: the wrench head was a muddle (handle running into the jaw, gap too small) -
   handle shortened, jaw opened to 84 degrees. The flask's liquid line vanished (a hole in a concave outline fills in
   with the bevel offset) - the flask is now stencil-cut in two pieces. The inner rule on selected panels rendered as
   a light triangle (same fill problem) - inner rules are now chains of thin capsules. Atlas height trimmed.
2. *HUD / menu / dialogue mockups*: focus states read instantly at 1080p and the panels sit well over the field. The
   Base icon read as a bell - redrawn as a domed hall on a platform between two pylons. The BossMarker's three thin
   rakes blurred into a smudge at 32-48 px - now two bold rakes. Mockup layout fixes (score got its own plate, the
   tooltip and its pointer no longer cover a slider).

## Honest weak points

- The icons are clean and consistent but plain; they do not carry the small machined details of the 3D models. At
  24 px the Net, Roll and Patrol icons lose their inner detail (still recognisable by outline).
- The dark rim under white icons is baked; on a very dark button it reads as a slight shadow halo. A rim-less variant
  is a one-line change in `build_icons` if Remco prefers.
- 9-slice borders assume Pixels Per Unit 200 (2x masters). At 720p the 3 px (1.5 px at 1x) edge is crisp only with
  mip maps off on the kit.
- The Kenney fonts stay (CC0); they draw K and X like H, which is the font's style, not a bug.
- Mockups take about 3 minutes (numpy 9-slice and resampling on full 1080p canvases; the bundled numpy is slow at large resamples).

# Round 24b: organic pool outlines with swept banks (2026-11-04)

Remco wants rounded pools, not only square ones. Gameplay stays on cells; the outline is smoothed in Unity and a bank
is swept along it. Blender side: `<Prefix>_BankSweep_A/B/C` (1 unit, plain-section ends so any variant follows any
other, cut every 1/16 along X, UV1 along the run) and `<Prefix>_BankAccent` for all six styles; `bank_sweep_preview.py`
does the contour smoothing (cell outline -> Gaussian sigma 0.55 -> wobble 0.04) and the bending.

Critique 1: the organic pool reads round at once and the stones bend without kinks; but the minimum radius came out
0.33 (sigma 0.42) and a band of dry basin showed between the lava lip and the lava (the basin sloped from z = -0.08).
-> sigma 0.55 (radius 0.38 at the two tightest spots), the basin now drops straight to -0.3 inside the shore, shore
effects start 0.06 inside the curve. Critique 2: on the smooth peat (Mirewood) the joints between variants show as
faint cuts (each variant has its own baked atlas) -> accents on ~60 % of the joints; still faintly visible close up.
The smoothing turns the one-cell arm of the h into a rounded lobe and narrows the one-cell dry notch into a peninsula;
that is the intended look, but very thin features (one cell) lose their shape.

# Round 25b: gameplay markers, bars, projectiles, debris and spawn gates (2026-11-04)

Remco (2026-11-04): "alles moest via Blender, dus niets meer via code zelf". An audit found the small gameplay
visuals still made of code primitives (cubes, spheres, cylinders, LineRenderer rings) or textures painted at runtime
(`GlowDecal`). Round 25 makes them in the art pipeline: `build_spawngate.py`, `build_projectiles.py`,
`build_markers.py` (+ `marker_kit.py`, `marker_scene.py`). Unity notes in `README.md` (round 25b).

## Design rules used

- **Subtle by default** (Remco 2026-10-30/11-01): thin lines, soft edges, low fill; nothing that covers the map in
  squares. Brightness lives in one thin line per marker (cursor edge, range rim, hero ring); fills stay below ~0.2
  and fade towards the middle, because on the dark ground even a linear 0.02 additive is visible.
- **No chevrons or arrows.** Road and flight direction are flowing light: one soft streak with a long tail every three
  cells (road), light beads with short tails (flight), scrolled by the shader. The old arrowheads are gone; route starts
  get an expanding pulse ring (`Cue_Pulse`).
- **Colour codes:** every marker texture is a greyscale channel pack tinted in the shader, so ours stay cool
  (`ValidColor` teal, hero gold, slow-field blue) and the aliens warm. The spawn gates and the enemy bolt are the only
  assets with baked or painted warm colour: blood-red hide, ember-orange veins and throat, sickly green eye glints.
- **Projectiles** follow the 2026-10-30 wish: glowing rounded bars with a light trail, not balls of light. The bolt is
  a capsule bar with a white-hot nose and a tinted tail plus a crossed glow shell for the existing `Bolt_Core`
  sprite; the rail slug is a faceted dart with three rail rings; the enemy bolt is a spat ichor glob with three
  swept barbs (alien, not a sphere).
- **Health bars** keep the 2026-11-01 night numbers (0.85 x 0.085, translucent dark plate, emission 0.7) and fix the
  one thing a scaled cube cannot do: the fill is clipped in the shader on UV2, so rounded ends stay round and the
  bar is cut from the right without scaling.

## Spawn gates (alien design rule)

The old gate was a bumpy mound with ten cone teeth and a flat glowing hoop. Departures from anything Earthly:
- **Ground:** a crater mound of overlapping chitin scales over flesh (Voronoi plates with dark seams; no concentric
  rings), glowing veins that leave the throat along some seams and die out, pustules with a green glint, five horns of
  unequal height (two lean over the throat like claws, on one side only), six fleshy lips that peel open on `Spawn`,
  roots crawling into the soil, and a vertebra ring with seven glowing nodes and seven down-hooked claws that hovers
  over the throat and turns (Idle = 30 deg/s, the old `GateRingSpin`).
- **Air:** a floating seed pod (teardrop, narrowing to a neck round the throat), five curved spurs hooking down past a
  tilted claw ring, five lips on top and four tendrils of unequal length that sway. It shares the ground gate's
  palette and ring, so both read as one hive.
- Pipeline as the creatures: parts fused by voxel remesh, per-vertex styles, Cycles AO, decimated, unwrapped and
  baked (512, Low 256: about 100 px on screen at the default zoom), part-aware skinning (the ring rigid on its
  bone), clips keyed from code.

## Critique passes (renders in the session scratchpad, `markers/`)

1. **Textures sheet:** fine as channel packs, but the health-bar card hid the plate (fixed in the scene preview with
   the real recipe). Contact cells now fit any aspect.
2. **Spawn gates, first build:** concentric fold grooves made the mound read as a target / a coaster; roots stuck out
   like orange sun rays; lips invisible; the ring a thin hula hoop; the air pod read as a cauldron with handles ->
   Voronoi scales instead of rings, darker flattened curving roots (5, unequal), bigger lips with hot tips, chunkier
   beaded ring with larger claws, the pod redesigned (neck, spurs that hook down, no collar, ring lowered below the
   spurs so it turns clear of them).
3. **Review scene, first pass:** the road comets (two per cell, sharp heads) still read as a dotted arrow line; the
   range-ring fill was a solid teal disc; free cells were hard rounded squares; the slow field's three even ripples
   read as a target; the bar fill grew from the right (upright quads had U running the wrong way after the axis
   conversion) -> one long soft streak per three cells, a narrow rim-only fill, soft round free-cell glows, two
   wobbling ripples, U flipped on upright quads (U now starts at Unity -X, screen left).
4. **Review scene, second pass:** the ring's fill was still too wide (glow from 0.55 R) and its hard cut aliased ->
   glow only in the outer fifth, anti-aliased, fill and band meet at the same value; the rim kept below the bloom
   threshold (an HDR rim made the preview bloom fill the disc).

## Honest weak points

- The spawn gates have no LOD1 (3,800 / 3,600 tris is under budget, but the Low profile has no lighter mesh) and the
  air pod's close-up render sits on the ground (the scene renders show it floating). The ground gate's horns reach
  1.29 (1.55 at `GateScale` 1.2): check that they never hide a tower behind the gate.
- `Spawn` is a short lip peel; it does not yet show a creature-sized bulge in the throat. The emission is baked warm,
  so a theme tint is a multiply, not a re-colour.
- Marker looks are judged in EEVEE with a stand-in for the Unity shader; the gains in `marker_scene.py` are a
  starting point, not final values. The range fill and band meet with a hairline where the 48-segment disc and the
  96-segment band differ.
- Debris chunks are clean colony parts (white plate, blue panel, hex, strut, pipe, bracket, hazard, rocks): little
  scorch or bending; the wreck/scorch looks of the gate and base are not reused.
- The particle atlas sprites are 128 px and simple; the ichor splat and goo decal are flat masks with no normal.
- Not made here because round 20 already covers them: hero blaster bolt (`Bolt_Core`), ability beam (`Beam_Core`),
  rocket exhaust and smoke trail.

# Round 26: menu diorama, underground, Earth, cutscene FX, skies (2026-11-04)

Batch B3 of the visual audit: the last visuals that were still made by code. Scripts `build_menu_diorama.py`,
`build_underground.py`, `build_planet_earth.py`, `build_skies.py`, `build_cutscene_fx.py`, `build_decor_c1.py`;
helpers `stage_kit.py` and `planet_kit.py`. Unity notes in `README.md` (round 26).

## Menu diorama

Goal: the first thing a player sees should say "a living alien world, and we are the intruders". The composition
of MenuDiorama is kept exactly (island centre, hive mound, the five pools with their sizes and bank arcs, camera,
sun, creature routes), because the creatures and the camera drift are code that stays. What changed:
- **One designed island instead of a height-function mesh.** A floating chunk of S-69-CRYS ground: the plateau keeps
  its gentle undulation, the edge rolls over in a rounded lip and the underside hangs below as an inverted mountain
  with strata terraces, overhanging buttresses, Crysium veins glowing pink deeper down and three hanging spires with
  crystals at their tips. 13,800 tris, about 8,400 on the walkable top (decimation weighted so the top keeps its
  shape), the rest underneath where only the drift or a side view sees it.
- **The hive mound is a dome, not a cone,** of wine-dark resin in a honeycomb of cells, radial ribs catching a lighter
  sheen, pink veins, and a creep of the purple pod-moss (GroundFlora) spreading out from it over the plain. The
  walkers' lap round it is trodden darker. A crown spire, tendrils, spikes and egg clusters dress it.
- **The mouth is a real burrow.** The old black oval lay flat on the porch. Now a notch is cut into the mound at porch
  height and `Menu_NestMouth` covers it: a hooded chitin arch with five ribs (a heavy lip in front), hooked teeth
  hanging from the lip, spines raking back along the hood, buttress feet, and a wet throat membrane glowing ember
  from inside. The crawlers' "inside" point now lies under the hood, so they walk out of the dark.
- **Each corner is painted in its own element:** basalt and ash with cracks that glow up from the lava shore; teal
  wet moss with sparse glowing spores round the pond; pale drifting sand and grit round the quicksand pit; rime round
  the frozen lake; black-green glossy mud round the bog. Banks are wet and dark, the lava rim a cooled crust.
- **Our outpost** (new, optional group): on a small rise at the back left, across the island from the hive, two build
  pads with a gun and a laser turret, the survey beacon and a crystal, on cleared dusty ground with track marks. It
  is small and far, so the menu still belongs to the hive, but it tells the story at a glance.
- **Reuse:** every prop is an existing export (Crystal Plains decor and flora, crystals, rocks, round-17 elemental
  decor per corner, round-23 pool palettes through the liquid set). AO from all props is baked into the top, so
  contact shadows exist even where Unity's shadows are soft.

Critique passes (renders `b3/menu/`):
1. First build: the layout came out mirrored (lava left, outpost right). Cause: Blender (x, y, z) arrives in Unity as
   (-x, z, -y); `common.G` keeps x, which only mirrors symmetric creatures. Fixed in `stage_kit.u2b` (and the yaw
   sign). Decimation kept only 8 triangles on the top (the decimate vertex-group weight means "collapse more"); fixed
   with weights 0.25 top / 1 underside.
2. The mound read as a pyramid, the spire was taller than the frame, pond spores carpeted the ground, the pink
   crystal dust was confetti everywhere, the lava rim glowed as a thick halo, the ice corner washed the left half
   pale -> dome profile, spire at 0.62, sparse spores, crystal dust squared and rarer glints, a narrow crack-driven
   lava rim, darker and smaller rime.
3. The hood stood in front of the notch wall, so its throat was walled off, and a spike stood inside the porch ->
   the notch now runs under the whole hood (to z 5.3), the spike moved. The burrow glows from inside in the menu shot.

## Underground cave and Burrower eyes

Story beat (STORY.md intro shot 6): the dark under the field, crystals trembling with the drill, then eyes opening,
one pair, another, then hundreds. The eyes are the Burrowers in the walls; the scene is about scale and dread.
- **Cave kit in The Core's language:** indigo-black crystal glass (the Core palette, darker) with sparse gold veins,
  lit by the Underground theme's cold cyan Crysium. Walls are stacked chiselled boulders leaning out at the top, with
  ice-Crysium breaking out of the seams; Wall_B has a deep slot between two buttresses for eye pairs; the ceiling
  hangs rock drips and crystal points; floors are rubble, one with a glowing cyan fissure; a crystal cluster replaces
  the trembling PropKind.Crystal; a flat vein strip dresses rock.
- **The eye:** one eye per mesh, a pair is two instances. An almond opening, fuller at the inner corner and slanted
  inward (predator), in a knotted chitin socket that sinks into the wall; lids are spherical shells that close by
  turning about the eye's right axis (so a vertex shader can blink an instanced eye with one angle); the eyeball has
  a painted iris that glows hot gold round the pupil and deep orange at the rim, with fibres, a limbal ring, and a
  dark wet sclera with red veins; a clear cornea bulges over it. Three variants, so a wall of eyes is not one
  creature copied: amber with a vertical slit, ember with a horizontal bar and brow spines, sulphur with three
  pupils (Earth starting point: cat and goat eyes; the three-pupil eye is the alien turn). About 420 tris.

Critique passes (renders `b3/cave/`):
1. Eyes: the cornea rendered as an opaque grey ball (material indices were lost when slots were cleared), the lids
   blinked the wrong way (opening), the socket left a visible gap round the lids, and every imported shape key came in
   at weight 1 -> indices preserved through material changes, blink sign fixed, socket inner ring tucked under the
   lids, all keys written at 0 (FBX DeformPercent = Unity's default blend-shape weight).
2. Iris pale cream under bloom -> saturated gold-to-orange emission, darker albedo, sclera glow nearly off; lid
   margin darker.
3. Cave scene: gold veins scribbled over every face, the scene was nearly black and no eye showed (they sat inside
   the wall boulders) -> veins at 0.3 of the Core amount, walls moved out to radius 6.1 so the eye radius is the wall
   face, stronger crystal lights and fill; the socket's outer ring made a dark "mask" round each pair, so it was
   shrunk and pushed back into the wall.

## Earth (cutscene globe)

Goal: the Earth the colonists left, readable as Earth in one glance and as dying in the second. The arrangement of
the continents is real (hand-placed lobes, domain-warped, fractal coasts), the surface is not: grey-brown land, dead
forest in the old humid belts, wide dust where the deserts grew, dried lakes as salt flats, dry river beds, grey
sprawl where the cities were. NTI's industry is visible: 52 terraced open-pit mines with rust and toxic tailings
ponds, white work lights still burning in them at night. The seas are murky, with silt plumes and algae swirls along
the currents. The ice has shrunk to grey, soot-streaked caps. At night only about one city cluster in three still
glows, dim amber, plus a few gas flares. A brown smog deck (dust-yellow over the deserts, soot-dark over the
industrial belts, a thin veil everywhere) replaces white clouds, and the limb glows tan-orange instead of blue.
Palette taken from EarthPainter so the cutscene keeps its colours.

Critique: pass 1 found donut-shaped mines, a boxed Greenland ice edge, a broken-up Mediterranean, far too strong
relief and too heavy smog. Pass 2 found coast shadow lines, mines that still read as rings, salt flats that were too
bright and a twilight stripe across the disc. Fixes: polyline seas, relief that rises from the shore, broken
benches, a weaker normal map, a limb-only twilight, a thin smog veil.

## Skies (reflection and ambient per theme)

The old SkyProbe gave metal a three-colour gradient. These skies keep that gradient as the base (it is what reads at
game scale) and add things a polished turret, the water or the ice can catch: a faint nebula in the continent
palette, aurora curtains over the two ice continents (green and blue in the north, magenta in the charged south),
smoke bands over Ashen Reach, mist over Mirewood, and a low glow from below where the land lights the haze (embers,
amber heat under the ice, charged veins, the seal). The Core and the Underground cave have a rock ceiling with
crystal glints instead of a sky, so reflections say "underground". The Earth sky is space with a warm sun and the
brown Earth below. All of them stay dark: zenith 2-6 % value, horizon band 40-55 %. Critique: pass 1 found oversized
stars, a thin aurora stripe and busy cavern ceilings; pass 2 found an aurora that had become a grey blanket and a
Core glow band that was too strong. Both were toned down.

## Cutscene FX

The cutscene glows were one soft dot and one soft band painted at runtime, so the sun, a crystal glint, an engine
and a ground crack all looked like the same blur. Each now has its own shape: a sun disc with limb darkening and a
streaming corona, a wide halo with faint irregular rays and a limb shoulder, an anamorphic streak with a ghost line,
a four-plus-four glint star, a nozzle glow with a lit rim, a running light with a small cross, and a re-entry trail
whose braided filaments break up towards the tail. The ground decals are jagged midpoint-displaced crack lines that
pinch, swell and fork: radial bore cracks round a scorched hole (four variants), straight drill cracks tapering to
both ends, and swarm fissures with a narrow molten slit and crust edges. The scream ring is hollow: a sharp leading
edge, thin ripples and a short wake. The gate sheet combines sharpened scanlines, a faint hex lattice and vertical
shimmer, with bright rims at its side edges. All are greyscale so the existing HDR tints and bloom do the colour.
Critique: 1) halo ring and rays read as clip-art, engine rings as a target, cracks as thin lightning, scream ring as a
filled disc, gate sheet as a solid block -> fixed; 2) halo too plain, swarm fissure too close to the drill crack ->
sparser sharper rays, a molten opening; 3) the opening showed boxy blotches -> narrowed to a slit with a soft glow.

## Decor fallbacks for Crystal Plains

IceShard (North), DeadTree and Reed (Mire) sit well on Crystal Plains ground under the plains lighting and are mapped
as they are. The North Snowdrift does not: a bright white sheet with a straight cut edge reads as a tarp on dark
violet stone. Decor_Snowdrift is the plains version: pale violet frost over Crysium dust, a long windward back and a
sharp crest with a broken crust lip, an organic outline that thins into the ground, frost needles and two small pink
shards, faint glow only in the crust cracks.

## Honest weak points

- Menu: the island is judged in EEVEE with stand-in liquids (the lava preview is a crust texture, not the round-24
  lava shader); the lava rim still leaves warm blotches on the bank; pink spore flecks remain over the mound and the
  plain; the underside is dark and fairly uniform (it is barely seen); planar top UVs stretch on the steep notch
  walls, which the hood mostly hides. The front rib of the burrow hood is a smooth tube with little carving. The
  creatures in the renders are Skitter stand-ins; the queen guardian on the porch was not placed.
- The island top must be sampled from `Menu_Island_Height.png` in Unity; the old `MenuTerrain.Height` noise no longer
  matches the mesh.
- Cave: the walls are boulder stacks, convincing at cutscene speed but repetitive if a camera lingers; the back of a
  wall piece is a smooth mass. The preview lighting is brighter than Theme_Underground suggests; the Unity cave relies
  on the five crystal lights. Eye sockets still frame each pair a little like a mask at mid distance.
- Earth: mining scars read as pale ovals up close; faint coast lines on the shadow side; island chains are blobs.
- Skies: continents 2-6 have no VisualTheme assets yet (palettes from round 17); LDR PNG plus exposure, not true HDR.
- FX: previews only imitate Unity's additive shader and bloom; drill cracks may need a larger quad or glow.

## Round 27: upgrade-screen display stage

Goal: the upgrade screen showed towers, the hero, drones and the base on a code-made dark disc, a thin torus and a
two-colour gradient. The stage now looks like a colony research bay: the model turns on a technical turntable in
front of a dim hangar wall.

- **Pedestal:** a low drum, top radius 1, 0.2 high (one fifth of the radius, so small drones are not dwarfed).
  Gunmetal plinth foot, eight white ceramic cladding panels with real gaps (the seams show the dark core drum), two
  hazard tabs and four seam lamps, a blue lip framing a recessed light channel (the subtle emissive rim, white in the
  map, tinted in Unity), a dark gunmetal deck with a thin inner light ring and four faint radial seams, and a steel
  top ring as its own object with twelve yellow index ticks and three lamps so its slow counter-turn reads.
  Iterations: 3,346 tris on the first pass (bevelled lip, closed drum, four-sided channel) cut to 2,946 by opening
  the drum, dropping the lip bevel and using a three-sided channel; the deck had heavy scratches (wear 0.4), now
  0.12 so the model's own wear stays the story; the channel glow was lowered (1.5 -> 1.1) after the first stage render.
- **Halo ring:** greyscale and additive: a crisp main line just outside the foot with a soft glow, six segmented
  arcs further out, minor ticks every 5 degrees and major ones every 30. It sits under the foot, so in the void it
  reads as a hologram projector rather than a floor.
- **Backdrop:** a small 3D set rendered in EEVEE (panelled outer wall, pillars with blue strips and amber lamps, a
  chamfered bay with eleven ribs, an overhead light bar, a dim service line, a tiled floor with a threshold light),
  then softened (35 % sharp / 65 % blurred), bloomed, vignetted and dithered. Iterations: 1) a dark seam plate
  covered the bay by mistake, so only a gradient showed; 2) with the bay open it was far too bright (mean 0.42, blown
  back wall); 3) light and emission cut to a calm mean of 0.10, the service line dimmed so it does not cut through
  the model.

### Honest weak points

- The emission rim and lamps read almost white in the previews (pale tint plus bloom); in Unity a stronger
  `ColonyRim` tint or lower intensity shows more blue.
- The pedestal is scaled with the model's footprint (`FloorRadiusShare` 1.25 x the longest horizontal extent), so a
  long barrel gives a big pedestal and a tower that looks small on it (Cannon render). A cap on the radius or using
  the pad footprint would fix that in code.
- The bay light bar and the pillar strips are the brightest pixels behind the model; very bright models (white hero
  armour) may still compete. One backdrop for every model, no theme variants, no parallax.
- Faint rain-streak marks from the shared bake shader show on the flat deck.

## Round 28: the continent bosses get their signature mechanics (clips, props, one improvement pass)

Brief: Docs/BOSSES.md (2026-11-04). Every boss must climb (rocks, gates, the base), use pools (dive, hide, burst
out) and play its own signature mechanic; the field must stay consistent, so every effect needs a visible body
action with a clear frame to hang the gameplay event on.

### Review against the design sheet and the alien rule (turntables before any change)

| Boss | Verdict | Why |
| --- | --- | --- |
| Gravemaw | keep | A long low armoured drill-worm with a crystal crest; the star maw and the crest read at play size. Pink on dark plum is a single family, but that is its continent. |
| Cinder Matriarch | keep | The kiln sac and the stilts are the most distinct silhouette of the six. From above it is still a little spider/ant-like (round-16 note); the glowing sac carries it. |
| Frost Warden | keep, armour added | The only upright one; the halo and glaives read as head and weapon. The new ice armour overlay gives the armoured phase its own look (pale ice over the dark stone) and covers the seal glyph until it breaks. |
| Rotbloom | keep | The five-petalled flower is a star from above, the bark mound and root legs are not an Earth animal. The petals are glossy red at close range. |
| The Shattered | **improved** (build_shattered2.py) | Weakest of the six: a brown gorilla silhouette in Gravemaw's plum-bronze, a small low head, no weapon, nothing that said "three". |
| The Sealed One | keep | Eyeless praying torso, crown rings and seal stones: the most alien of the set. |

**The Shattered, what changed (visible at play size):** palette from plum-bronze to pale shattered-ice granite over a
black-glass under-layer (the Southern Crown's own "pale ice over dark rock"), the overcharge light stays magenta to
white; a crown of five crystal blades on the head (the head now reads above the hump); a crystal cleaver out of each
fist (its weapon); three crystal spires on the back, each shaped like the shard it becomes (spear fan = ShardRunner,
swept wing blades = ShardFlyer, low claw ring = ShardCrawler). Rig, budgets (17.5k + 6.5k tris, 1024 maps) and the
round-16 clips are unchanged. It is still a knuckle-walker underneath: a full redesign would change the rig and
every clip, which this round did not take on.

### How the clips are built (boss_mech_kit.py)

- **No rebake:** each `build_<boss>_mech.py` opens the boss's `.blend` and keys new clips with the boss script's own
  pose functions, then re-exports both FBX files with the old and the new clips. Re-running is idempotent.
- **Planted feet on a height profile:** the climbs move the body at exactly the gait's stance speed, so every foot
  is fixed on the ground while it is down; each footfall is placed on the profile (ground, face, top) and never on
  the face or the lip itself (it lands short of it or past it). The body height follows a wider ramp of the profile
  and pitches with the slope; necks and torsos take part of the pitch back so heads stay on target.
- **Gravemaw rides paths:** its spine follows a path in the forward-up plane (round-14 machinery), so it climbs by
  laying its body up the rock face like a centipede; the feet are planted on the path surface.
- **Depths are measured, not guessed:** each submerged loop is keyed, the deformed mesh's top is measured and the
  depth corrected until everything is 0.2 under the surface (Burrow_Travel: until only the crest's top 0.3 shows).
- **Loops are in place** (no root motion). Travel clips are authored with the pivot fixed and report an end offset;
  Unity moves the transform by it on the last frame and continues with the next clip, which starts in that pose.

### Iterations

1. Cinder Matriarch: the kiln sac scraped the ledge corner on the way up and down; the body ramp now trails behind
   the hips (1.7 m up, a long lead down) and the sac lifts with the slope. The first egg had box-mapped side plates
   (a barrel with slits) and its plates fused (with each other at the crown, the core with the collar) in the remesh,
   so they tore into long triangles when they flew. It is now a three-clawed bud of crust plates, open at the crown,
   every plate its own island on its own bone: they peel open and fly off cleanly (checked island by island).
2. Gravemaw: the first climb path ran through the rock (the body vanished into the ledge); the path now runs just
   outside the face. Surface_Throw started at a different depth than Burrow_Travel; both now share the fitted depth.
3. Frost Warden: the ice armour baked nearly white (it read as plaster); the ice is now deeper blue with less frost.
4. Rotbloom: the spore pod read as a can (blunt ends); the core and bracts are longer and close over the nose. The
   volley kick was invisible at play size; the bloom swells and recoils harder at each shot. Climbs shortened with a
   longer step.
5. The Shattered: the first crest was hidden behind the hump; the blades are longer and stand up.
6. The Sealed One: phase 4 and Release looked the same; phase 4 is now lilac-white, Release pure white.
7. The Sealed One: its submerged loop kept 0.46 m above the surface after three depth fits: the heaved rubble ring
   was skinned to Root and never moved with the body. It is now on its own bone `Rubble` and leaves with the seal
   stones (scale 0, dropped under the ground) in every clip that moves the body away from the seal.
8. RotOvergrowth: the blend shape was saved at weight 1 (the FBX opened flat); it is now 0 (grown) by default.
9. Liquid previews: the first liquid material was opaque and blown out, so the submerged clips showed nothing; the
   preview liquid is now a thin translucent plane (preview only, the game uses its own `Game/Liquid` shader).

### Honest weak points

- **Climb clips are long** for the big bosses (Gravemaw ~11 s, Rotbloom ~8 s, others 6-7 s): the whole body has
  to get over the edge at a stance-locked speed. Unity can play them faster (they are planted, so speed only
  changes the cadence) or cut the obstacle height.
- One ledge height per boss (1.2-2.0 m, in the reports). Other heights need the foot markers and a vertical blend
  of the transform; the hands of the Sealed One do not follow other heights.
- The generic dive/burst clips are the same choreography for the four legged bosses (crouch, hop, plunge; surge,
  arc, land): readable but not individual. Gravemaw (path) and the Sealed One (hands) have their own.
- Submerged depths are deep (Cinder ~3.4, Warden ~4.6, Sealed One ~5.6 m): the liquid shader must hide everything
  under the surface, or the pool must be dug that deep.
- The Sealed One's lower torso ploughs through the terrain when it moves (Walk, Climb, Rise_OntoBase); a furrow
  decal and dust are needed so it reads as wading through the ground, not clipping.
- The Shattered is still a knuckle-walker; its shards are simple (2,000-2,400 tris, 512 maps) and their Spawn and
  Rejoin are a curl/unfold with scale, so the flash VFX has to sell the split.
- Rise_OntoBase hauls the torso 1.6 m forward and 0.9 m up: it looms over the base's front edge rather than
  climbing all the way onto it; with a bigger base the offsets in the script (BASE_F, BASE_H) need a re-run.
- Rooted_Idle sinks the feet 0.2 into the ground; on a bog surface that is right, on hard ground it would clip.
- The ice armour covers the Warden's seal glyph in the armoured phase (deliberate: the glyph is revealed when the
  armour breaks), but the armoured Warden is less amber at play size.

## Round 29: missing boss art (2026-11-05)

**Scale fix.** Bones that a clip does not key are baked by the FBX exporter with the pose the previous clip left
behind. The round-28 bones were added after the round-16 clips existed: the Frost Warden's armour plates, the
Sealed One's seal stones and its rubble ring. So the round-16 clips came out with them at scale 0, depending on
export order. Every clip now keys them explicitly. The Sealed One shows its seal only in Rise; that is the one
clip that starts at the seal now that it moves around.

**Shard climbs.** The shards reuse the bosses' generic climb (planted feet over a ledge, pivot fixed, end offsets
in the report) with three changes for small, fast bodies:

- The body ramp starts later (lead 0.3-0.5 instead of 1.1). Otherwise the body was already lifting on frame 0.
- The runner, a biped whose two hips sit side by side, takes only half the climb pitch. Its full pitch made it rear
  up about 55 degrees.
- The runner's ledge face moves 0.6 further out, because its long stride swings the front foot past the default
  face and frame 0 would have stood on the top.

The travel is keyed on Root: a few fused vertices are skinned to Root and stretched back to the pivot (seen in the
first crawler strip as a magenta streak). The runner reads as a vault, the crawler as a spider tipping over a
step.

**OnBase_Loop.** It starts in Rise_OntoBase's last pose so the hand-over needs no blend. Two alternating hammer
blows per loop: the hand goes up and back while the torso rears against it, then both come down together, so the
blow reads from the top-down camera as a lunge onto the roof, not just an arm moving.

**Props.**

- **RuinGate:** reads as old and alien rather than as a door. A pointed arch of eleven pale-granite voussoirs (the
  Southern Crown granite and the Shattered's palette) carries eroded glyph grooves, so the magenta light burns
  unevenly instead of like a neon sign. Leaning buttresses are split by overcharge crystals, and three crystal
  horns on the crown mark it from the top-down camera.
  - The seal is an iris of five black-glass petals with concentric sigil rings and a crystal knot in the centre.
  - Opening breaks the knot and folds the petals back like a flower, the bottom one flat onto the ground.
- **SealCrystal:** a neutral lilac-grey body with a white glow mask and dimmed flutes, so the facets still show once
  Unity tints them to the immune type.
- **Ice block:** grown, hewn chunks with white fracture planes painted into the alpha, and five-sided frost prisms
  (twisted spikes read as drill bits).
- **Drill wreck:** reuses the colony derrick's own pieces, snapped at the first brace level and folded over
  Gravemaw's heave, with its crystals breaking through the ground as the story hint.

**2D set.** The decals are painted as height fields lit from the top-left, like the baked meshes, with the glow in a
separate mask.

- **Threshold:** one G threshold per decal turns the stand-ins' fades into motion. Lava cools from the edges to the
  channel, the crack opens out from the fissure, frost creeps along 60-degree fern branches, rot decays from the
  edges, and the old road sinks from its middle.
- **Colours:** each boss keeps its colour: lava orange, rot green, frost ice-blue, violet seep for the Sealed One.
- **Plates:** warped Voronoi cells keep the plate networks from tiling into a honeycomb.
- **Flipbooks:** they follow the round-20 CG and LIT packs, so the existing VFX shader recipes apply.

### Honest weak points

- **Shard climbs:**
  - One ledge height (1.0 m). The runner's vault is fast (32 frames); its rear foot is mid-swing at frame 0, so the
    hand-over from Walk is not exact.
  - The crawler's Climb_Up and Climb_Down are long (93 frames, 3.1 s); Unity may play them faster.
- **OnBase_Loop:** the lifted hand is partly hidden behind the torso from the side, but reads clearly from the game
  camera. Adapt on the base still has no clip of its own.
- **Sealed One seal:** the seal stones pop out at the Rise -> Walk switch, because they ride on Root; a static seal
  prop left at the rift would be cleaner.
- **RuinGate:**
  - The outer footprint is 8.7 m wide; scale it to about 0.8 on small maps.
  - Seen from the front, the opened petals read as flat dark slabs.
  - There is no light inside the opening (Unity's flash).
- **SealCrystal:** spire A is a fairly smooth cone.
- **Gate_IceBlock:** it is sized for G3 (a lot of ice over G1), and the apron's crack pattern is busy.
- **Drill wreck:** the thrown drill bit reads a bit like a torpedo, and the motor house keeps its cyan window
  glow.
- **Drained maps:** a numeric transform, not rendered on the models.
- **2D set:**
  - The hot lava trail glows over much of its crack net until it has cooled a little.
  - The rot trail's mould rings are even in size, so it reads spotted.
  - Frost has radial cells only, no strip.
  - The water wake's V arms can read as two streaks.
  - Rejoin is the Split sheet reversed.
  - The decal tiles repeat every 5-14 m: give each piece a random U offset.

## Round 30: Frost Warden redesign, crack slabs, ice block and seal crystals (2026-11-05)

Brief: MODEL_TODO "Ronde 5 Unity" (FW1-FW7, GW1, IB1, SC1). Remco judged the Frost Warden and Gravemaw's crack "niet
heel super". Seen from the game camera, the Warden's armour read as paper cards floating off a purple, amber-seamed
body. That was the lava boss's palette, and the amber undersides of the plates showed as yellow post-its. Before and
after renders are in the session scratchpad, `blender30/` (`before/`, `after4/`, `mech4/`, `fw4/`, `props*/`,
`slabs3/`).

**What changed in the Warden (silhouette and palette, the hero lesson):**

- **Cold body (FW5):** slate blue-grey stone-chitin with frosted pale rims, rime on the up faces, and seams and plate
  rims in pale ice blue. The amber is kept only in the six eye pits, the face slit and the chest seal glyph (the
  enemy colour code and the story hint). The weathering pass no longer adds warm grime.
- **Solid armour (FW1, FW2):** the eight plates are now hewn ice volumes (`ice_kit.chunk`: an ellipsoid cut by
  14-18 planes) 15-35 cm deep. They sink into the body, so no edge or underside shows. The mapping changed:
  - chest: covers the lower half of the glyph;
  - two heavy pauldrons, each with three big spikes;
  - back;
  - both forearms (on `Arm_*_Fore`);
  - both front shins (on `Leg_*1_Lower`).
  The train lost its armour and keeps a ridge of body ice.
- **Ice texture (FW2):** `ice_kit.frost_ice` is deep blue-green and darker in the middle of each facet (looking into
  the ice). The frost is white, broken rime along the facet ridges (the style knows the cutting planes) and on the
  up faces. Inner fracture planes go into the normal map, and thin cyan light runs in a few of them. There are no
  glowing areas and no warm root light.
- **Top-down silhouette (FW4):** shoulders about 25 % wider (a yoke, wider pauldrons and the armour on top). A crown
  of spikes sits on the pauldrons and hood. The five rage icicles stand on the upper back between the shoulders.
  The halo is bigger and thicker and tilted back 46 degrees, so the 44-degree game camera sees a full ring (a crown)
  instead of an edge.
- **Glaives (FW6):** `ice_kit.kite_blade`, a six-point section (cutting edge, flanks, faceted spine ridge), so the
  blade is triangular in section and catches light. It is longer, its tip almost on the ground in Idle, and it
  curves slightly like a scythe.
- **Scale (FW7):** the whole model and rig are 15 % larger. The round-16 design numbers stay; `ice_kit.scale_builder`
  and the scaled rig apply `SC`. Stride, lift and every measured clip offset follow automatically. The climb ledge
  (1.5 m, the frozen gate) does not scale.

**Found on the way:** the old clips did not key the armour bones' location and rotation either, only their scale
(round 29). Blender then showed the plates where Armour_Break leaves them, flown off, in Idle and Walk; the FBX
exporter can bake that too. `hold_rest` now keys the rest pose of `Armour_*` and `Icicle_*` in every old clip.

**Iterations:** blockout 1 had a halo that read as a tall blade from the side, and small, blob-like armour.
  - **Blockout 2:** halo tilted to face the camera, plates and limbs thicker.
  - **Bake 1:** plates flown off (the hold bug above).
  - **Bake 2:** the ice was nearly black and read as obsidian.
  - **Bake 3:** lighter ice, but the ridge frost read as white grout lines.
  - **Bake 4:** broken rime on the ridges, softer up-face frost, the torso glow lowered (it read as pale plastic).

**Props:**

- **GW1, crack slabs:** four thin tilted basalt plates, hinged on the ground and rising towards the fissure. The top
  shows the strip's own cracked-cell basalt (a 2D Voronoi in the slab plane). The raised edge, its underside and the
  seams near it carry the molten glow. A first version with a broken-off chip on the edge read as bottles, so the
  chip went and the plates got thinner.
- **IB1, ice block:**
  - Clear blue in the middle of the big facets (alpha 0.25); white frost on the facet ridges, fracture planes and
    up faces (alpha up to 0.9). The fine secondary crack web is halved.
  - An overhanging frost lip along both long top edges, with seven icicles hanging from each lip. The first try hung
    them inside the bulge of the block, where they did not show.
- **SC1, seal crystals:** flat-faced crystals (no flutes or twist, hard edges, light smoothing). `facet_glow` lights
  the middle of every face a step above its edges, so a tinted crystal reads as cut planes, not a lamp. Spire A has
  two branch crystals out of its flank and side crystals at its foot; B has side crystals. The small ones have no
  node rings, which made them read as drill bits.

### Honest weak points

- **Frost Warden:**
  - The column legs and arms are still the round-16 tubes, only thicker. From close by, the limbs are the plainest
    part.
  - The ridge rime still reads a little as a pattern of light lines on the big pauldron facets.
  - The body is dark against dark ground. From the game camera the ice and the halo carry the read; Unity's cold rim
    helps.
  - The chest glyph is half covered and still the brightest thing on the model (by design: amber only there).
  - The halo is large (the model is 5.7 m tall to the halo tips). On a cramped map, Unity can scale `Halo` to 0.85.
  - Spikes are separate islands in the armour mesh (32 islands, none mixed). They ride their plate's bone, so they
    leave with it.
- **Crack slabs:** static and tilted only one way. Slab C is 308 tris (brief 150-300). Seen from the camera side,
  the near-lip slabs show their face and read as small boulders. Yaw and sink them so mostly the raised glowing
  edge shows.
- **Ice block:** still sized for G3. The icicles are short at the ends of the lips. The transparency depends on
  Unity using the albedo alpha.
- **Seal crystals:** with the facet glow, a strongly tinted crystal can read as a striped lantern. Lower the
  emission multiplier to about 2 if so.

## Round 31: Frost Warden concepts, slug strike, drill rig collapse (2026-11-05)

### Frost Warden: three body plans

**Why:** Remco read the round-16/30 Warden (a humanoid torso on a six-legged body) as an "ice scorpion" with unclear
proportions ("ik weet niet waar ik naar kijk"). Each concept therefore has one anatomy that reads at once from the
44-degree game camera: an obvious front (eyes plus breath glow), one obvious weapon and one dominant mass. No mixed
plans.

- **A Rime Colossus** puts the mass in the arms: a fist-walker whose ice mauls are both weapon and armour. Alien
  cues: no neck, three eyes, four mandible plates round the maw, flank gills, a crystal crest on the hump.
- **B Crown Floe-beast** puts it in a horizontal slab: a ram with a crown-shield and tusks, six eyes under the crown,
  neck gills, floes on the back; the most natural swimmer.
- **C Lantern Stilt-warden** puts it in height: a tripod on stilts (knees above the hood), one central glaive arm,
  a hanging ice lantern as the cast source; the lantern is the strongest signal at play zoom.

Each supports all five mechanics: freeze-gate breath, climb and smash, losing armour plates, rage icicles, diving.
Cold palette only; amber is limited to small eyes, breath and crack glow are cyan.

Critique passes: metaball bodies were too thin, then too bloated; the Skin-modifier graph gives exact radii. B was
raised off the ground and its crown made bigger and more upright. C went from four stilts (read as a harvestman) to a
tripod.

Weak points: A is the closest to a gorilla/golem, its arms are smooth tubes, and the pauldron/gauntlet can overlap the
head in 3/4. At game angle B's legs read as flippers (turtle/ankylosaur echo), and from behind its crown is edge-on.
C's thin legs and small head get small at play zoom; a tripod can still hint "spider". All: tube limbs, even voronoi
cracks, gill glow clips to white. These are blockouts for choosing a direction, not models.

### Slug strike: stretch, not bob

Remco: "het hele lichaam beweegt wippend om iets te slaan, het zou mooier zijn als die lichamen rekken". A slug has
no legs to swing and no jaw to snap; its one dramatic move is to lengthen. The strike is classic squash and stretch:
gather (shorter and fatter, the front cocked up), a fast lunge in which the body grows ~38 % and thins with volume
kept (width L^-0.65, height L^-0.35), the mouth slapping down on the target, then a damped spring snap back that
overshoots once into a short fat bunch before it settles. The tail never moves, so the eye reads the reach against a
planted anchor. The front segments take most of the reach (shares 0.1 / 0.45 / 0.62 / 0.3). Eye stalks fold back
during the reach and spring out after. The boss plays the same strike 1.25x slower for weight. Flood turns the old
rear-and-drop into stretch-up, whip-down, slap flat and long, snap back.

Weak points: four chain bones only (the mantle stays fairly stiff at full reach); the foot's pedal stripes stretch
visibly when the front lifts (reads as slime); Flood's rear-up is foreshortened from the game camera; the snap past
rest is subtle on the small Oozer at game zoom; Unity's procedural Slam must be turned off for these two or it still
adds a small hop.

### Drill rig collapse

Remco found the tipping derrick silly: a rigid model falling over reads as a toy. A real derrick fails from below:
the legs buckle at their first joint, the lattice folds at a brace level, and the heavy top snaps off and hits the
ground beyond. The rig is cut at the brace levels into a deck, four bent legs, a middle section and the top. The
middle tips 72 degrees over the +X legs and the top folds until it rests on the ground (the fold angle is solved).
Eight braces tear off and land flat. The motor house is squashed to 42 % with its roof sheared, and the stack folds
over. Gravemaw's heave and three crystals push up under the deck at the impact. The drill string is thrown out of the
hole and snaps in two: the kelly lands on the far side and the bit lands tip-first, buried to the shoulder. Motion
follows gravity (angles grow with t^2, a small rebound on landing, debris in arcs). Each part's last frame is its
wreck transform, so wreck and clip end cannot drift. Round-29 notes fixed: the bit has cutter teeth, stands tip-down
with a bent kelly (no torpedo); every lit part is burnt, the only light is a few dim ember slivers in torn ends.

Weak points: the standing frame has no cyan glow (hidden under the impact dust); the crushed house sits under the
lattice from the game camera; the middle section rests a little high on the bent legs; some braces fly 3-4 m out,
possibly outside the dust; the kelly is a plain square bar with a small torn cap.

## Round 32: the Frost Warden becomes the Rime Colossus (2026-11-05)

Remco picked concept A of round 31. The brief: the anatomy must read instantly from the 44-degree game camera, with no
tube limbs, natural cracks, glow that never clips to white, and no plain gorilla or golem. Previews and the round-30
backup are in the session scratchpad, `blender32/` (`block1/`, `block2/`, `body1/`, `body2/`, `mech1/`, `mech2/`,
final `body/`, `clips/`, `game/`, backup in `backup/`).

**One anatomy:** a hunched knuckle-walker. Almost all of the mass sits in the shoulders and forearms:

- **Arms:** the forearms flare towards the wrist and carry grown-ice mauls on the fists. The fists are both the feet
  and the weapon.
- **Back and head:** the hump rises behind the shoulders. The head is a low, flat wedge slung forward between the
  fists, so from the camera the eyes and the maw are the obvious front.
- **Hind legs:** short and thick, pushing from behind.
- **Armour:** the floes sit where the mass is: the pauldrons, the gauntlets on the forearms, two back floes, and the
  thighs.
- **Rage telegraph:** the icicle crest stands on the hump ridge, the highest point, so it shows from every angle.

**Against the gorilla/golem read (the round-31 weak point of A):**

- **Carapace:** five shingled bands run over the hump and back, each tucked under the one in front. They are darker
  and bluer than the hide and have pale frosted rims. They make it an armoured alien (an insect echo for the alien
  race), not a furry ape.
- **Head:** three amber eyes in a triangle and four mandible plates round a cyan maw. Swept-back horn brows replace a
  face.
- **Vents:** three frost vents run along each flank of the hump (raised lips round a cyan throat). Crystals grow out
  of the vents.
- **Ice growing out of the body:** elbow spurs, a ridge on the outer forearm, knee spurs, brow crystals and a rump
  cluster. This ice is part of the body (the `Ice` material), so it stays when the armour breaks.
- **Hide, not stone:** leathery folds, pores and dark broken bands across the body.
- **Cracks:** a warped world-space Voronoi whose borders mostly stay shut, vary in width and send off only a few
  hairlines. They are denser on the carapace. One field covers the whole body, so a crack runs across part seams.
  The first bake cracked every border evenly and read as a stone golem.
- **Glow:** baked emission is at most ~0.5 in the vents and maw and ~0.4 in crack cores. The eyes are smaller and
  dimmer (0.55) after the first bake rendered them as yellow-white blobs.

**Massing (no tube limbs):**

- **Arms:** shoulder ball, a shorter and thinner upper arm with a bicep mass, a small elbow ball, then the
  forearm (tube plus an outer muscle mass, radius 0.42 at the elbow to 0.72 at the wrist), the fist and the maul.
- **Legs:** a big thigh mass, a knee ball, a short shin and a splayed foot with two claws.
- **Trunk:** the pelvis and belly were slimmed after blockout 2, where the rump read as a round backside.
- **Bend:** the arm lengths (1.48 + 1.7 + 0.5) leave a real elbow bend at rest, and the pole flares the elbows out
  and back, so from above the arms bow round the head instead of standing as columns.

**Ice:**

- **Look:** a lighter frosted version of `ice_kit.frost_ice` (more clear blue, rime on the facet ridges and up
  faces, crack light 0.42), shared by the mauls, the grown ice and the armour.
- **Why:** bake 2 read as dark teal glass with near-white crack lines.
- `ice_kit` itself is unchanged; its props keep their look.

**Motion:**

- **Gait:** a lateral-sequence knuckle walk (left hind, left fist, right hind, right fist). The body drops onto each
  landing fist, the shoulders yaw with the swinging arm, the head counter-yaws to stay on its target, and the fists
  swing round, not through.
- **Attacks are lunges, not bobs:**
  - FreezePulse and Shatter_Through rear the whole body back about the hips, curl the fists together over the head,
    then hammer them down with the body following the blow and squashing.
  - Enrage throws the fists wide and slams them.
  - Freeze_Gate inhales (rears, head up), then thrusts the wedge head low and forward with the maw wide.
- **Arm IK:** the fists are the arms' feet. Every clip poses them by IK, with the fist direction set per pose
  (knuckles down to walk, knuckles up overhead).

**Iterations:**

1. **Blockout 1:** gorilla read (a smooth balloon hump, pillar arms, a small hidden head).
2. **Blockout 2:** carapace bands, head pushed forward and enlarged, elbows flared.
3. **Bake 1:** an even crack network (golem), mauls hidden inside the fists, eyes clipping.
4. **Bake 2:** sparse cracks, a bluer carapace, big mauls, armour floes enlarged ~1.25x; the ice still dark teal.
5. **Bake 3 (final):** lighter frosted ice and dimmer crack light.

### Honest weak points

- **Silhouette:** from straight in front it is still an ape-like stance. The carapace, eyes and vents carry the
  alien read, and that gets weaker at true play zoom.
- **Joints:** the shoulder and elbow balls still show as rounded joints from the side. A muscle pass (deltoid into
  the carapace) would help.
- **Ice:** reads as glossy glass in the studio renders. In game the IceLook rim and glow decide it.
- **Size:** the Warden is large next to a G2 gate (2.68 m): 6.0 m wide from fist spike to fist spike, 5.7 m long,
  4.8 m to the icicle tips (round 30: 3.6 x 5.7 x 5.7). From the game camera the head can hide the gate at the cast
  distance. Unity's `BodyHalfWidth` (1.25) is now too small, about 2.0 fits.
- **Generic clips:** Climb_*, Submerged_Loop and Surface_Burst use the shared boss gait, so the fists lift no higher
  than the hind feet there.
- **Dive:** the ice smash blends into the generic dive between frames 22 and 30. The fists slide a little there.
- **Shatter_Through:** the rear (frames 8-31) lifts the head out of a tight game frame. The arm step at 64-88 is
  long (2.4 m), so the arms stretch.

## Round 33: gate-crossing clips (2026-11-05)

Remco: the Gravemaw on a gate moved "as if it stands on flat ground". The cause was that Unity played the rock clips
on a gate: Climb_Up, then Climb_Loop (a flat crawl) lifted to the beam height. Previews are in the session
scratchpad, `blender33/` (`gm/`, `cm/`, the round-32 backup in `backup/`).

**Measure first:**

- **The gate is small next to a boss.** The beam is a bar 1.17 m high and 0.21 m deep. The crowns stand only 3 cm
  higher, and the span is 2.7 m. The bosses play at 0.8, so in model units the beam is 1.46 high and the crowns 1.5.
- **Gravemaw cannot stand on top of it.** Its legs are 0.84 long on hips 0.3 high, so on top it would have nowhere
  for its feet. The only honest pose is draped over the bar: belly on the beam, the front half pouring down the far
  side, the rear half on the near side.
- **The Cinder Matriarch stands wider than the gate.** Her stilt feet land at x 2.0, outside the pylons, so she
  strides over and sits her thorax on the beam.

**Gravemaw, how the drape works:**

- **The arch:** the round-14 spine rides a path in the forward-up plane. Round 33 gives it an arch with a long ramp
  on the near side (the tail drags), a tight fold over the beam (the weight hangs there) and a steep far side (the
  head pours down to the claws).
- **Catmull-Rom looped:** the first arch with Catmull-Rom made loops on the uneven point spacing, and the body
  folded through itself. A monotone cubic (`gate_kit.monotone_samples`) cannot overshoot.
- **Feet on real surfaces:**
  - The spades dig into the far ground.
  - Legs 1 grip the crowns' far faces, legs 2 stand on the crown tops, legs 3 brace on the crowns' near faces.
  - I picked these because the legs are short: a hip 1.3 above the ground reaches nothing else. The crown faces and
    tops are the same in every tier, so one clip fits G1, G2 and G3.
- **Weight:**
  - The front heaves up, a spade slams in, the beam is pressed down (Unity sags it with the reported burden) and the
    body is raked forward. Then the weight drags it back over the beam, and the hind feet slip down the faces and
    re-grip.
  - The nodes next to the beam go down by the full sag, the ends stay on the ground. A first version let the belly
    rise 6 cm off the beam at every push: the head heave also lifted the middle node.
- **Mount, the order that works:**
  - The rear-up lifts the shoulders out of reach of the ground. So the spades first brace on the crowns' near faces,
    then hook over the far edges once the body has leaned in far enough.
  - Before the shoulders get past the gate, the hook was out of reach by 0.2 to 0.7.
  - The legs lose the ground while the hips climb, and scrabble in the air (a paddle) until a pylon face or crown is
    in reach. The step planner (`gate_kit.plan_steps`) chooses the nearest reachable surface every time a foot would
    over-stretch.
- **A check that sees the beam:** a thin bar can pass right through a body without one body vertex inside it. The
  contact check now also tests the gate's vertices inside the deformed boss mesh. That showed the Cinder
  Matriarch's low waist swallowing the beam on the way down (0.29 deep), so she now rises on her stilts again while
  the waist crosses.

### Honest weak points

- **Feet pressed into faces:** the spade claws bite up to 7 cm (model) into the crown edge while hooked, and a hind
  foot cuts a crown corner for a few frames on the way over. At play zoom it reads as gripping.
- **The Mount haul is long:** the legs dangle and scrabble from about frame 34 to 90 while the spades do the work. It
  reads as being hauled, but a viewer may notice the legs doing little.
- **Gravemaw's body is about as wide as the gate.** From the game camera the crowns show at its flanks, but the beam
  itself is hidden under the body while it lies there. The sag is mostly read from the crowns and the dust.
- **The Cinder Matriarch's weight is subtle:** she is tall, so her thorax pressing down reads less than Gravemaw's
  drape. The scythe hacks carry the loop.
- **Rotbloom has no gate clips.** Its rigid round body and short legs cannot drape or straddle. See the README
  proposal.
- **Not checked in Unity:** the burden curve assumes `TowerDisplacementView` keeps squash 0.16 and drop 0.12. If those
  change, the belly floats or sinks.
