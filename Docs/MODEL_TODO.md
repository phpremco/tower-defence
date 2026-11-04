# Modellen — takenlijst voor Blender

Alle 3D-modellen die we in Blender gaan (her)maken, van de oude vormen uit code naar echte modellen. We werken de
lijst langzaam af, naast het andere werk. Bijgewerkt: 2026-11-03.

Werkwijze en regels:

- Elk model heeft een eigen build-script in `Tools/Blender/build_<naam>.py`. De output gaat naar `ArtSource/Exports/`.
  Rebuild alles met `blender --background --python Tools/Blender/build_all.py`. Zie `ArtSource/README.md`.
- **Ontwerpregel:** het aardse voorbeeld is alleen het startpunt. Het eindresultaat moet anders, gevaarlijk en
  duidelijk buitenaards zijn. Dat geldt ook voor planten en kristallen. Eigen kant (mensen) is koel en technisch
  (blauw, wit, geel). Aliens zijn warm en organisch (rood, oranje, groen). Zie `ArtSource/DESIGN_NOTES.md`.
- **Budget:** zie de tabel onderaan. Max 1024-textures (512 voor Low/Switch), vier maps per model (Albedo, Normal,
  Emission, Mask), één materiaal, gedeelde detail-normals per materiaalsoort. Fijn detail komt uit textures, niet uit
  geometrie.
- **Leesbaar op de spelcamera:** de vorm en de tekening op middelgrote schaal (naden, banden, randen, gloed) moeten op
  ~20 px werken. Poriën zijn voor de close-up en de cutscenes.
- **Stappen per model:** ontwerpnotitie → bouwen → renders (spelcamera + close-up + vergelijking oud/nieuw) →
  Remco akkoord → import in Unity (view-component, logica blijft ongemoeid) → screenshot in het spel.

Status: ☐ te doen · ◐ in Blender klaar, wacht op import · ☑ in het spel

Prioriteit:

- **P1:** komt voor in Crystal Plains level 1–6 (nu speelbaar)
- **P2:** rest van continent 1 (levels 7–20), of in het spel maar nu niet in de campagne
- **P3:** latere continenten

---

## 1. Vijanden (Burrowers)

Per vijand zijn de animaties **Walk, Idle, Hit en Death** nodig, en voor elke vijand ook een LOD1. Vliegers hebben
**Fly** in plaats van Walk.

| Status | Prio | Model | Rol en basis (oud ontwerp) | Extra |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Skitter** | Kleine zwermloper (was: mier) | Ronde 3 klaar. LOD1 klaar |
| ◐ | P1 | **Roller** | Loopt en rolt zich op tot bal (was: gordeldier) | Ronde 4 klaar: acht platen sluiten tot een gekartelde bal met haken, gloeiende naden en naven. Clips Walk/Idle/Hit/Death/Curl/Roll/Uncurl, LOD1 klaar. Unity: Curl/Roll/Uncurl als clips (zie README) |
| ◐ | P1 | **Cragback** | Steenkruiper, zwaar en traag (basalt, gloeiende aders) | Ronde 4 klaar: tektonische platen, muil onder een rampenkop, amber Crysium, zuilpoten. Walk met dip per stap (voetstap op frame 0/16), LOD1 klaar. Aders doven via emissie-fade in Unity (frames 8–40) |
| ◐ | P1 | **Shalebat** | Steenvariant van de vleermuis (lucht) | Ronde 5 klaar: vleugels van drie leisteen-messen met gebarsten steenhuid en amber aders, geode-rug met ember-Crysium, brauwplaat, kaak met gloeiende keel. Clips Fly/Glide/Hit/Death, LOD1 klaar. Eigen rig (16 botten), niet gedeeld met de Bat |
| ◐ | P1 | **Skyray** | Vliegende rog met platte vinnen en stekelstaart (lucht) | Ronde 5 klaar: ramkop met tangen, zes ogen, sikkelvinnen met stralen en gloeiende voorrand, kieuwen op de buik, drietand-staart. Vingolf zit in de botten (voor- en achterketen). Clips Fly/Idle/Hit/Death/Dive, LOD1 klaar. Shader-golf mag uit |
| ◐ | P1 | **Wisp** | Energiebal met draaiende ringen (lucht, kan landen) | Ronde 5 klaar: knop van vier klauwbladen om een gloeiend hart, twee gyroscoopringen, drie tentakelpoten die landen als driepoot. Clips Float/Land/Grounded/Hit/Death, LOD1 klaar, 2.400 driehoeken. VFX in Unity (halo, bogen, stof bij landen) |
| ◐ | P1 | **Prowler** | Snelle roedeljager op vier poten | Ronde 4 klaar: schaarkaken, stekelkam, gloeiende ribben, hakken en zweepstaart. Clips Run (laag en plat)/Idle/Hit/Death/Pounce, LOD1 klaar |
| ◐ | P2 | **Stinger** | Schorpioen: scharen en staart over de rug | Ronde 10 klaar: zes poten, valklauwen met een scharnierende haakvinger, vijf dorsale platen met een magenta achterrand en een pijl naar voren, een geringde zuurstaart met gloeiende gewrichten, een geribde zuurblaas en een naald met weerhaken. Clips Walk/Idle/Hit/Death/**Strike** (raak op frame 10). 3.961 driehoeken, 25 botten, LOD1 1.700 / 14 botten |
| ◐ | P2 | **Bat** | Elementvleermuis met vliezen vleugels (lucht) | Ronde 10 klaar: één mesh en rig voor alle elementen, het element is een palet. Vleugels over drie haakstekels met een gloeiende voorrand en lichte banden, wigkop met vier ogen per kant, gloeiende boutklier op de borst, zweepstaart met vork. Standaard Ember; Moss en Dust als extra huid gebakken (`--element`). Clips Fly/Glide/Hit/Death. 3.896 driehoeken, 16 botten, LOD1 1.700 / 10 |
| ◐ | P2 | **Bulwark** | Rechtop staande schilddrager met kristalzwaard | Ronde 10 klaar: saurische tweevoeter, een benen schild gegroeid uit de linkeronderarm (groeiribbels, kleine gloeiende kern), een roze Crysium-zwaard gegroeid uit de rechteronderarm (laag in rust), vizierkop met amber ogen, schouder- en borstplaten, limoen goo in de naden. Niet gespiegeld (geen gespiegelde UV's). Clips Walk/Idle/Hit/Death/**Block** (schild omhoog op frame 7). 3.950 driehoeken, 19 botten, LOD1 1.700 / 16 |
| ◐ | P2 | **Oozer** | Slak op een slijmvoet met ogen op steeltjes | Ronde 10 klaar: vier oogsteeltjes (eigen botten, trekken in via bot-schaal), mantel met toxineklieren van wisselende grootte, ringmond met haaktanden, doorschijnende slijmvoet met gloeiende rand. Clips Crawl/Idle/Hit/Death. 3.801 driehoeken, 9 botten, LOD1 1.700 / 7. Slijmspoor blijft VFX |
| ☐ | P3 | **Shardback** | IJskever met een rug van ijsprisma's die afspringen | Breekbare prisma's als losse onderdelen |
| ☐ | P3 | **Mireleech** | Moerasbloedzuiger: lang en laag, met zuigmond | Modderspoor |
| ☐ | P3 | Hersteller (nog geen naam) | Vijand die HP terugkrijgt (teaser level 15) | Nog ontwerpen. Mogelijk een variant |
| ☐ | P3 | Lava-loper (nog geen naam) | Overleeft in lava (level 17, continent 2) | Nog ontwerpen |
| ☐ | P3 | Graver (nog geen naam) | Graaft zich in (zand, continent 2) | Clip **Burrow/Emerge** |
| ☐ | P3 | Waterwezen (nog geen naam) | Duikt op uit het water (continent 4) | Clip **Emerge** |
| ☐ | P3 | Verdorven skin-set | Donkere versies van alle soorten met paars kristal (The Core) | Alleen andere textures. Mesh blijft gelijk |

### Levelbazen

Bazen zijn groot, gloeiend en traag, en tonen duidelijk welke kant ze op gaan (zie de voorkeuren van Remco).
Animaties: Walk, Idle, Hit, Death, plus een clip per baaskracht.

| Status | Prio | Model | Basis | Extra |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Queen** (Hive Queen) | Eierbuik, kroon van gloeiende stekels | Ronde 3 klaar. Gloei van de eieren per pixel als vervolgstap. Schaal in Unity controleren |
| ◐ | P1 | **BroodMother** | Baasvariant van de Skitter. Baas van level 1 | Ronde 6 klaar: Skitter-sikkels met weerhaken, X-kaken onder een brede kap met een kroon van vijf Crysium-scherven en een lichte pijl naar voren, dikke lage poten, een enorme broedzak met amber eieren, twee zijzakken en een geboorte-opening met haken. Clips Walk/Idle/Hit/Death/**Spawn** (broed op frame 40 bij het bot `Vent`), zakken en opening zwellen via bot-schaal. 14.506 driehoeken, 29 botten, LOD1 6.000 |
| ◐ | P1 | **CoreWisp** | Baasvariant van de Wisp | Ronde 6 klaar: zes korte bloembladen om een groot, half open hart met een gloeiende spleet en twee hoorns aan de voorkant, drie ringen (kralen, haken, kroonring met knopen en stekels), vijf tentakels als komeetstaart die als vijf poten landen. Clips Float/Land/Grounded/Hit/Death/**Pulse** (schok op frame 14), hart en ringen schalen. 8.992 driehoeken, 22 botten, LOD1 4.000. Kleur is één parameter: koel (standaard) en warm (amber hart, limoengroene ringen) ter keuze. VFX in Unity blijft belangrijk (halo, schokring) |
| ◐ | P1 | **StormMatriarch** | Baasvariant van de Skyray. Laat kleine vliegers los | Ronde 6 klaar: brede sikkelvinnen met condensatorkralen op de gloeiende voorrand, kleine achtervinnen, gekroonde ramkop met grote tangen, twee laadhoorns met lichtknopen, twee rugluiken boven een gloeiende broedkamer, lange drietand. Clips Fly/Idle/Hit/Death/**Release** (luiken open, vliegers op frame 30). 13.446 driehoeken, 26 botten, LOD1 6.000. Kleur is één parameter: koel (standaard) en warm (pruim, magenta, oranje rand) ter keuze. Let op: in de data is ze nog een vleermuisbaas die Mossbats roept |
| ◐ | P1 | **PackAlpha** | Baasvariant van de Prowler | Ronde 6 klaar: zware schouders en diepe ribbenkast, grotere schaarkaken met tanden en binnenhaken, een kraag van gloeiende stekels in twee waaiers (eigen botten, gaat open bij de Howl), dubbele stekelrij, gloeiende littekens over de ribben, lichte pijl op het zadel. Clips Run/Idle/Hit/Death/**Howl** (piek op frame 22). 14.018 driehoeken, 24 botten, LOD1 6.000 |
| ◐ | P2 | **AcidEmpress** | Baasvariant van de Stinger | Ronde 10 klaar: zeven platen, kroon van magenta Crysium, grotere klauwen, twee zijstaarten, grote geribde sproeiklier (bot-schaal) met drie mondstukken. Clips Walk/Idle/Hit/Death/Strike/**Spray** (zuur op frame 26). 14.206 driehoeken, 28 botten, LOD1 6.000 |
| ◐ | P2 | **CrystalWarlord** | Baasvariant van de Bulwark | Ronde 10 klaar: groter schild, Crysium-slagzwaard, kroon van Crysium op de helm, kristal door de schouderplaten en langs de rug. Clips Walk/Idle/Hit/Death/Block/**Sweep** (horizontale zwaai, voorlangs op frame 20). 13.991 driehoeken, 19 botten, LOD1 6.000 |
| ◐ | P2 | **RollerKing** | Baasvariant van de Roller | Ronde 10 klaar: brons in plaats van zand, vijf zaagringen haken, flanknaven met een kroon van ember-Crysium. Clips van de Roller plus **Quake** (sprong als bal, inslag op frame 16). 13.831 driehoeken, 20 botten, LOD1 6.000 |
| ◐ | P2 | **SkySovereign** | Baasvariant van de Bat of Skyray | Ronde 10 klaar als **vleermuisbaas**: bredere vleugels plus een tweede vleugelpaar, hoornkroon, lange staart met een lensorgaan (bot `Emitter`). Wijnrood met magenta gloed. Clips Fly/Glide/Hit/Death/**Laser** (straal frame 20–60). 12.603 driehoeken, 19 botten, LOD1 6.000. In de data nog de rog-baas: zie verbeterpunten |
| ◐ | P2 | **SlimeMonarch** | Baasvariant van de Oozer | Ronde 10 klaar: kroon van zes oogsteeltjes en gloeiende stekels, bredere kliermantel, twee broedzakken (bot-schaal). Clips Crawl/Idle/Hit/Death/**Flood** (klap op frame 24)/**Spawn** (oozers op frame 30). 12.995 driehoeken, 13 botten, LOD1 6.000 |

### Continentbazen

Dit zijn de grootste modellen, met de meeste aandacht en animaties.

| Status | Prio | Model | Gedrag uit het script |
| --- | --- | --- | --- |
| ☐ | P2 | **Gravemaw** (continent 1) | Reusachtige graver met kristalpantser. Clips **Dive/Emerge** |
| ☐ | P3 | **Cinder Matriarch** (2) | Vuurbaas met brandend spoor. Laat vuurlarven los (larve = klein extra model) |
| ☐ | P3 | **Frost Warden** (3) | Bevriest turrets, wordt sneller. "Bewaker" |
| ☐ | P3 | **Rotbloom** (4) | Hout- en moerasbaas, herstelt zich en laat slijm achter |
| ☐ | P3 | **The Shattered** (5) | Burrower vol overladen Crysium. Drie fases, barst open (drie mesh-staten) |
| ☐ | P3 | **The Sealed One** (6) | Oeroud wezen onder het zegel. Eindbaas |

---

## 2. Hero en personages

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ◐ | P1 | **Cole "Pathfinder" Mercer** | Ronde 6 klaar: navy gewatteerd pak, witte keramische platen, helm met rookglas-vizier en gele kruinstreep, gelaagde schouderstukken met gele rand, rugpak met twee cyaan cellen, holsters op beide dijen, Crysium-stof op de laarzen. 9.800 driehoeken, LOD1 4.000 op hetzelfde rig (31 botten, sockets `Weapon_L/R`, `Holster_L/R`, `Back`). Clips Idle/Walk/Run/Shoot/Reload/Slash/Shockwave/Repair/Hit/Down/Revive; Shoot, Reload en Slash met bovenlijf-masker (vanaf `Spine1`). Portretbuste gerenderd. Zie README ronde 6 |
| ◐ | P1 | Mercers **pistool** en **laserzwaard** | Ronde 6 klaar: pistool 1.250 + los magazijn 120 (Quick Reload-magazijn 200), gevest 760 (Wide Slash-gevest 800), twee kling-meshes met UV-verloop voor een emissieve shader. Eén 512-materiaal. Alles in "grip space" van de socket: identity onder `Weapon_R`. Dual Wield via `Weapon_L`/`Holster_L`; Crysium Blade volgt |
| ☐ | P2 | **Dr. Elena Voss** (buste) | Nu een simpele buste in het portret. Ook bruikbaar in cutscenes |
| ☐ | P2 | **ORA** (hologram) | Nu ringen rond een bol. Vooral shader. Glitch-staat |
| ☐ | P2 | **NTI-embleem** | Logo-object voor de berichten |
| ☐ | P2 | **Pioneer-1** (sonde, heel) | Voor de logboek-portretten |
| ☐ | P3 | **Pioneer-1 wrak** | Decor in Mirewood (level 8) en de basis van het Rotbloom-gevecht |

---

## 3. Torens (eigen kant, koel en technisch)

Per toren:
- **3 upgradeniveaus** die zichtbaar verschillen, met sterren erop, plus een pad A/B vanaf niveau 3 als dat er is;
- een **beschadigde** staat (rook en vonken via VFX) en een **wrak**;
- lichte bovenkant en donkere onderkant;
- animaties: **Idle-sweep, Fire, Recoil**.

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ◐ | P1 | **Gun Turret** (PulseRifle) | Ronde 7 klaar: compacte kop met één lange loop in een koelmantel met blauwe spoelen. T2 twee lopen, T3A roterend viervoudig cluster met trommel (clip **Spin**), T3B lange scherpschuttersloop met condensatorringen en vizier. 3.040–4.276 driehoeken, LOD1 1.420–2.112 |
| ◐ | P1 | **Cannon** | Ronde 7 klaar: breedste, laagste kop met wigpantser, dikke korte loop met mondingsrem, amber gloed. T2 rokken, rookafzuiger en granaatrek, T3A dubbele loop, T3B reuzenloop met dubbele rem, terugloopcilinders en contragewicht. 2.668–3.992 driehoeken |
| ◐ | P1 | **Laser** | Ronde 7 klaar: zender in een vorkjuk met een grote cyaan lens. T2 grotere lens en drie focuspennen, T3A reuzenlens met lange pennen en spoelen, T3B telescoop met focusringen en koelvleugels. Clips **ChargeUp** en **Beam**. Open punt: de datakleur is roze-rood (warm) |
| ◐ | P1 | **Flak** | Ronde 7 klaar: twee lopen in rust 40 graden omhoog, schild met gevarenstreep, munitietrommels. T2 vlamdempers en radar, T3A vier lopen, T3B lange zware lopen en grote radar. Clip **FireAlternate**. Tot 4.928 driehoeken |
| ◐ | P1 | **Gate Gun** (geschut op de gate) | Ronde 7 klaar: zadelklem op de balk, kleine kop, T1 één loop, T2 twee lopen en munitiekist, T3 kap en sensormast. Geen A/B. 1.736–2.412 driehoeken |
| ◐ | P2 | **Tesla Coil** | Ronde 11 klaar: de enige spits zonder loop. Violette spoelen op donkere isolatorschijven, een ontlaadbol in een kooi met boogpennen (sockets `Arc_n` voor de kettingbliksem), twee Crysium-cellen in kooitjes links en rechts. T2 condensatorzuilen, T3A brede halo met zes pennen, T3B open Crysium-kristal in drie klauwen. Clips IdleSweep/ChargeUp/Fire_1. 3.564–4.840 driehoeken, LOD1 1.540–2.212 |
| ◐ | P2 | **Railgun** | Ronde 11 klaar: de langste lijn: twee gunmetal rails met een lichtgevend plasmakanaal ertussen, vierkante versnellerkragen, condensatorblok. T2 zijtrommels, T3A twee railparen om en om, T3B lans met mondingskroon, radiatorrug en zware trommel. Clips ChargeUp/Fire_n. 2.768–4.320 driehoeken |
| ◐ | P2 | **Acid Sprayer** | Ronde 11 klaar: dikke witte tank met kijkglas en een gloeiende mintgroene band, slang naar een korte uitlopende sproeikop. T2 twee tanks, T3A waaier van drie koppen, T3B bolle druktank met lans. Bot `Slosh` (vloeistof in het glas), clips Spray/Slosh. 3.298–4.920 driehoeken |
| ◐ | P2 | Hybride **Artillery** | Ronde 11 klaar: Cannon + Flak: wigromp, schort en blokrem van de Cannon, de geheven loop (40 graden), het schild en de trommels van de Flak. T3 dubbele loop en grote radar. Amber. 3.182–4.852 driehoeken |
| ◐ | P2 | Hybride **CryoBeam** | Ronde 11 klaar: Laser + Cryo: vorkjuk en lens van de Laser, een berijpte koeltank erbovenop, vinflappen rond de lens, koelbussen op de juk-armen. T3 focuspennen. Clips ChargeUp/Beam. 3.906–4.630 driehoeken |
| ◐ | P2 | Hybride **StormRifle** | Ronde 11 klaar: Gun + Tesla: kop en lopen van de Gun met Tesla-spoelen om de mantel, een kleine Tesla-spits achterop, een Crysium-cel als accu. T3 roterend cluster (clip Spin). Periwinkle. 3.728–4.984 driehoeken |
| ◐ | P2 | **Support** en **Rocket** | Ronde 11 klaar (eenvoudiger, T1–T3): Support = mast met zwevende kern in een ring van vinnen, één halo per niveau (signaalgeel). Rocket = gekantelde buizendoos (2x2, 3x2, 3x3 + reserveraketten), sockets `Tube_n`. 2.408–3.918 driehoeken |
| ◐ | P3 | **Cryo Turret** | Ronde 11 klaar (vooruit gebouwd voor de CryoBeam): dikke berijpte koelcilinder met ijsringen, uitlopende sproeier met vier vinflappen, koelbussen (2, 3, 4), T3 ijsvinnen. 3.560–4.470 driehoeken |
| ☐ | P3 | Nieuwe hybrides uit het script (Shatter, Arc Laser, Corrosive Blast, Storm Flak, Thermal Shock) | Twee torens worden samen één nieuwe toren (zie het besluit over combo's) |
| ◐ | P1 | **Sterren** (upgradeniveau) en **bouwplek-sokkel** | Ronde 7 klaar: zeshoekige sokkel met gevarenrand, ventilatie en lichtring. Een schuine rangplaat aan de camerakant met drie sterren op de botten `Star_1..3` (in elke toren). Losse `Tower_Star.fbx` |
| ◐ | P1 | **Wrak en scrap-stapel** | Ronde 7 klaar: verbrande sokkel, omgevallen romp, schroot en gloeiende sintels in `Tower_Wreck.fbx`, met `Debris_Gun/Cannon/Laser/Flak/GateGun` |

---

## 4. Gadgets en padobjecten

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ◐ | P1 | **Land Mine** | Ronde 9 klaar: lage gunmetal schijf met zes ankerklauwen, blauwe band, witte drukkap met gevarenrand en een amber ring als hij scherp staat (bot `Ring`). Clips Arm/Idle/Trigger, socket `Blast`. 1.212 driehoeken, LOD1 632. Open punt: datakleur is rood |
| ◐ | P1 | **Chrono Field**-emitter | Ronde 9 klaar: driepoot, blauwe kolom, Crysium-scherf tussen twee pinnen in twee tegendraaiende gyroscoopringen (botten `RingA/RingB`), projectorring bij de voet, socket `Field`. Clips Deploy/Idle. 2.008 driehoeken |
| ◐ | P2 | **Lava Ball** | Ronde 11 klaar: onze machine (thermietroller), geen steen: acht witte pantserplaten en drie gunmetal banden met gele naven rond een gloeiende gesmolten kern (oranje: het wapen zelf, de schil blijft in ons palet). Bot `Ball` rolt om X, sockets `Core`, `Trail`. Clips Drop/Roll/Burst. 1.680 driehoeken |
| ◐ | P2 | **R.A.T.** (Remote Assault Tank) | Ronde 11 klaar: rupstankje (familie van de Ground Drone) met witte schorten, ronde koepel met cyaan oog, één loop; wielbotten, sockets `Muzzle`, `Lug`. Clips Drive/Fire/Idle/Land (2.180). Plus de **carrier-drone** `Gadget_RATCarrier` (ronde-9-dronefamilie, vier ducts op armen, twee haakklemmen, socket `Cargo`), clips Carry/Release/Fly (2.384) |
| ◐ | P2 | **Barrel** en **Air Net** | Ronde 9 klaar (statisch): vat in een gunmetal kooi met gevarenbanden en amber ruitjes (1.208); twee palen met gele spoelen op een drempel, sockets `Strand_L1..R3` voor de VFX-draden (1.460) |
| ◐ | P2 | **Ground Drone** (padobject) | Ronde 9 klaar: vierwielige ramrobot met witte ploeg, cyaan oog. Clips Drive/Idle, wielbotten. 928 driehoeken |
| ☐ | P3 | **Decoy Beacon** (2), **Barricade** (4), **Spike Strip** en **Rollback Field** (5) | |
| ☐ | P3 | Superwapens: **Payload Drop**-bom, **Overcharged Crystal**, **Toxin Bomb** | Vooral VFX, plus een klein object |

---

## 5. Drones

Animaties: **Fly, Hover, Work** (lichtbundel, vuren of graven).

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ◐ | P1 | **Repair-drone** | Ronde 9 klaar: ronde quad met vier kleine ducts, cyaan plus-in-zeshoek (geen rood kruis), arm met emitter, socket `Beam`. Clips Idle/Fly/Work. ~3.080 driehoeken (boven 2.500: vier ducts), LOD1 ~1.970 |
| ◐ | P1 | **Harvest-drone** | Ronde 9 klaar: brede hauler op twee grote ducts, boor met drie klauwen, losse vrachtcilinder `Drone_Harvest_Cargo` (amber). Clips Idle/Fly/Work. 2.294 driehoeken |
| ◐ | P1 | **Attack-drone** | Ronde 9 klaar: pijl met pijlvleugels, ducts op de vleugeltips, dubbele kanonkap, staartstraal (azuur). Clips Idle/Fly/Work (om en om vuren), sockets `Muzzle_1/2`, `Thruster`. 2.208 driehoeken. Open punt: datakleur is koraal (warm) |
| ☐ | P2 | **Builder-drone** en **hauler** (cutscene) | Lassen en vliegen in een zwerm. Kunnen varianten van de drones hierboven zijn |
| ☐ | P3 | Bombardement-drone (Pathfinder Strike) | |

---

## 6. Basis, gate en blokkades

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ◐ | P1 | **Gate** (blokkade op het pad) | Ronde 9 klaar: G1/G2/G3 (kaal, steunplaten en hoekpantser, vestingversie met hoorns en tankvallen), 18 hexpanelen op eigen botten (clip `Repair` en `Build`), overlay `_Damage`, losse `Gate_Wreck`, clips Hit/Collapse. Sockets `GunMount` (Gate Gun-zadel, 1,17), `Field`, `HeroPost`, `Spark_n`. 5.200–7.600 driehoeken |
| ◐ | P1 | **Basis** (eindpunt, 800 HP) | Ronde 9 klaar: acht modules met eigen pivot (dek, commandopost, schotel, landingsplatform, kern, kristal, habitat, aansluitkasten) voor de timelapse, overlay `Base_Damage`, `Base_Wreck`, sockets als empties. 7.250 driehoeken, LOD1 3.190 |
| ◐ | P1 | **Basiswapens / -turrets** | Ronde 9 klaar: `Tower_BaseTurret` T1/T2/T3 = Gun Turret-kop op een vierkante bastion-sokkel in basiskleuren, zelfde rig en clips als Tower_Gun. 3.204–4.440 driehoeken |
| ☐ | P2 | Modules van het upgradescherm (diorama) | Hergebruikt de modellen van de basis |
| ☐ | P2 | **Grondstofpunt** (Crysium-ader voor de harvest-drone) | |

---

## 7. Omgeving per level en continent

### Crystal Plains (P1)

| Status | Model | Extra |
| --- | --- | --- |
| ◐ | **Crysium-kristallen** (3 varianten) | Gloed bij de import iets lager, zodat de facetten zichtbaar worden |
| ◐ | **Rotsen** (3 varianten) | Rock_C is wat druk |
| ◐ | **Nestkrater / burrow** | Was een vulkaanachtig nest. Textures zacht op 512 |
| ◐ | **Rotsspleet (Rift)** voor het element Steen | Ronde 8: `Rift_1x1/2x1/3x1/_Cap`, kartelranden, amber gloed, scherven. Alleen rechte stukken |
| ◐ | **Scree** (puinveld) | Ronde 8: `Scree_Patch` |
| ◐ | **Boulder, Column, Spike, Spire** | Ronde 8: `Decor_*` |
| ◐ | **Planten:** Frond, Tendril, GlowTree, Mushroom | Ronde 8: `Flora_*`, pruim met roze/limoen gloed, sway-vertexkleur |
| ◐ | **Egg** (eierclusters bij nesten) | Ronde 8: `Egg_Cluster` |
| ◐ | **Kristalader in de grond** | Ronde 8: `CrystalVein` (platte mesh) |
| ◐ | **Padrand en bouwplek-tegels** | Ronde 8: zie hieronder |

### Level-omgeving als geheel (P1, Remco 2026-11-03: "de hele environment, de weg, de gates en alles")

Het terrein wordt nu in code gebouwd en geverfd (`TerrainPainter`, `LevelVisualBuilder`, `PropScatterBuilder`). Uit
Blender komen twee dingen: modulaire kits (meshes) en naadloze materiaalsets (Albedo, Normal en Mask, 1024, tilend).
De code zet die neer volgens de layout. De spellogica en het raster veranderen niet.

| Status | Model | Extra |
| --- | --- | --- |
| ◐ | **Weg: wegdek-materiaal** | Ronde 8: tiling-set `Road` (gesmolten plaat). Varianten per thema nog niet |
| ◐ | **Weg: rand-kit** | Ronde 8: `RoadEdge_*` (recht, binnen-/buitenbocht, eind, plein-overgang) |
| ◐ | **Pleinen** (wide roads en plazas) | Ronde 8: set `Plaza` + `PlazaEdge_*` |
| ◐ | **Gate-ondergrond** | Ronde 8: `GatePlinth` |
| ◐ | **Bouwplek-tegel** | Ronde 8: `BuildPadTile` |
| ◐ | **Grond-materialen** per terreinsoort | Ronde 8: sets `Ground`, `GroundFlora`, `Cliff`, `RiftRock`. Andere elementen later |
| ◐ | **Kaartrand en kliffen** | Ronde 8: `Cliff_*`, `Chasm_Edge` |
| ◐ | **Oevers** van vloeistoffen | Ronde 8: `Bank_*` (één stijl voor alle vloeistoffen) |
| ◐ | **Hoogteverschil-stukken** | Ronde 8: `Slope`, `TerraceStep(_Outer)`, `Ledge` |
| ◐ | **Kleine strooiers** | Ronde 8: `Scatter_*` (≤ 200 tris) |
| ◐ | **Achtergrond-silhouetten** | Ronde 8: `Silhouette_*` |

### Overige spawnpunten (EnemyOrigin)

| Status | Prio | Model |
| --- | --- | --- |
| ☐ | P2 | Spawnpunten in water, lava, drijfzand, ijs en moeras (opening waaruit vijanden kruipen) |

### Latere continenten (P3)

| Status | Model | Extra |
| --- | --- | --- |
| ☐ | **Ashen Reach** (vuur en zand) | Lavarotsen, as, verkoolde bomen |
| ☐ | **Northern Crown** (ijs, steen en vuur) | IceShard, Snowdrift, bevroren kristal |
| ☐ | **Mirewood** (hout, water en moeras) | DeadTree, Reed, zwammen, mist-decor, Pioneer-1-wrak |
| ☐ | **Southern Crown** (ijs en gemengd) | Gescheurde aders, instabiel kristal |
| ☐ | **The Core** (ondergronds) | Kristalplateau, zegeladers, verdorven kristal, het zegel zelf |

---

## 8. Cutscene en menu

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☐ | P2 | **Landingscapsule** | Hitteschild, retro-jets, loopplank. Clip **Open** |
| ☐ | P2 | **Boor (derrick)** | Draaiende boorkop |
| ☐ | P2 | **Moederschip** | Ring en motoren (shot in de baan om de planeet) |
| ☐ | P2 | **Baken** van Mercer | |
| ☐ | P2 | **Radarscherm / commandopost-interieur** | |
| ☐ | P2 | **Aarde** en **S-69-CRYS-globe** | Vooral textures en shader. Lage prioriteit voor een mesh |
| ☐ | P3 | Ogenveld ondergronds | Is al instanced. Eventueel betere oogvorm |

---

## Budget per soort

| Soort | Driehoeken (High) | LOD1 / Low | Botten | Textures |
| --- | --- | --- | --- | --- |
| Gewone vijand | ≤ 4.000 | ≈ 1.700 | ≤ 25 (Low ≤ 16) | 1024 (Low 512) |
| Levelbaas | ≤ 15.000 | ≈ 6.000 | ≤ 32 | 1024 |
| Continentbaas | ≤ 25.000 | ≈ 10.000 | ≤ 48 | 1024 (eventueel 2 materialen) |
| Hero | ≤ 10.000 | ≈ 4.000 | ≤ 32 | 1024 |
| Toren (per niveau) | ≤ 5.000 | ≈ 2.000 | 2–6 (draaien en loop) | 1024 |
| Drone / gadget | ≤ 2.500 | ≈ 1.000 | ≤ 8 | 512 |
| Decor | ≤ 1.500 | — | — | 512 |
| Basis / gate | ≤ 8.000 | ≈ 3.000 | — | 1024 |

## Voorgestelde volgorde

1. **Import-pijplijn:** Skitter, Queen en het Crystal Plains-decor in Unity, plus de shader-slots (Mask, detail-normal).
   Daarna de leesbaarheid op de spelcamera bijstellen.
2. **De rest van P1:** eerst de vijanden (Roller, Cragback, Prowler, Skyray, Shalebat, Wisp), dan Mercer, dan de torens
   (Gun, Cannon, Laser, Flak), dan de gate en de basis, dan de drones, dan de bazen van level 1–6, en tot slot het decor
   (rift, planten, rotsdecor).
3. **P2:** de rest van continent 1, inclusief Gravemaw, en de cutscene-modellen.
4. **P3:** per continent, tegelijk met het bouwen van dat continent.

---

## Verbeterpunten voor later (curator-commentaar)

Opgeschreven op 2026-11-03. Remco: eerst modellen voor alles maken, deze punten pakken we later op. De
leesbaarheid op de spelcamera beoordelen we pas goed op echte Unity-screenshots, na de import.

### Open vraag aan Remco

- **Kleur van Skyray en Wisp.** Ze zijn nu koel (blauw en cyaan). Dat botst met de regel "aliens warm, eigen
  kant koel". Er is nu een compromis: een violette waas op de Skyray en een warm paarsbruin lijf voor de Wisp, maar de
  gloed is nog koel. Advies: warm maken, bijvoorbeeld een magenta of oranje rand op de Skyray en een amber of groene kern
  in de Wisp.
- **Laserkleur.** In de data is de laserstraal warm roze-rood, maar het torenmodel gloeit cyaan. Advies: de straal cyaan
  maken. Rood en warm zijn voor de aliens.
- **A/B-paden op niveau 3.** De torenmodellen hebben al twee looks voor niveau 3 (bijvoorbeeld Gun: rotatie-4-loop of
  sluipschutter). De data kent nog geen paden; T3A is voorlopig de standaard.

### Per model

| Model | Verbeterpunt |
| --- | --- |
| Skitter | Op ~20 px nog een vrij algemene oranje kever. Poten iets "rubberachtig" |
| Queen | Eierzak-patroon is een uniforme honingraat. Van boven nog licht spin- of mierachtig. De keelgloed kan in het spel als klein mondje lezen. Gloed van de eieren per pixel (kartelranden van dichtbij). Schaal in Unity verlagen: ze is groter dan in ronde 1 |
| Roller | Lopend van boven nog wat pissebed-achtig (ovale omtrek). Kop klein en grotendeels onder de schep. De haken schampen de grond als hij rolt |
| Cragback | Platen vrij effen grijs. Zuilpoten wat blokkerig en speelgoedachtig. Ember-scherven lijken op vlammetjes |
| Prowler | Van boven vooral een rood silhouet. De ribgloed is nauwelijks zichtbaar. Sterkere rugtekening of fellere ribspleten |
| Skyray | Vintekening (lichte chevrons) te subtiel op spelgrootte. Gloed van de drietand zwak |
| Shalebat | Op spelgrootte grijzer en kleiner dan het oude model. Lichtere bovenkant of bredere spanwijdte. Amber scheuren nog wat regelmatig, celachtig. Nog geen Spit-clip |
| Wisp | De knop lijkt van dichtbij een beetje op een ui. Naden van de bloembladen branden erg wit |
| Crystal_A | Van dichtbij nog wat zacht en snoepachtig |
| Crystal_C | Knopen te kraalachtig. Dun op spelzoom |
| Kristallen (alle) | Gloed zo fel dat facetten en berijping wegvallen. Bij de import de emissie iets lager zetten |
| Rock_C | Scheurennetwerk wat druk |
| Burrow_A | Textures zacht op 512 (veel eilanden). Leunt op de detail-normal |

### Algemeen

- Bij de import de middelste laag bijstellen naar wat je op de spelcamera ziet: donkerdere naden, grovere tekening en
  lichtere randen. Het fijne detail blijft subtiel en vervaagt met de afstand.
- Dunne delen (antenneknoppen, klauwpunten) worden grof na het verlagen van het polygonen-aantal.
- De vergelijkingsplaatjes van ronde 4 en 5 tonen links een namaak in effen kleur van het oude model, niet het echte
  Unity-model.

### Bazen ronde 6 (BroodMother, PackAlpha, StormMatriarch, CoreWisp)

| Model | Verbeterpunt |
| --- | --- |
| BroodMother | Eiercellen een vrij uniforme honingraat. Poten gladde buizen. De opening van de Spawn-vent is van opzij slecht te zien |
| PackAlpha | Van boven vooral een donkerrood silhouet. Littekens subtiel en gespiegeld (zelfde aan beide flanken) |
| StormMatriarch | Luiken lezen nog wat als panelen. Achtervinnen grotendeels verstopt. **Data-mismatch:** in `ProjectGenerator.Bestiary` is ze nog een vleermuisbaas (Wood, roept Mossbats). Het model is een Skyray-baas. Bij de import kiezen |
| CoreWisp | Zwakste richtingsaanwijzing: van boven nog rond. In Unity een kort bewegingsspoor toevoegen |
| Kleurvraag | Warme varianten van StormMatriarch en CoreWisp staan klaar (`blender7\*\*_palette_compare.png`). Curator vindt warm beter, vooral voor de CoreWisp (amber hart en limoengroene ringen) |

### build_all.py (gedaan 2026-11-03)

- Toegevoegd na de basissoorten: `build_broodmother.py`, `build_packalpha.py`, `build_stormmatriarch.py`,
  `build_corewisp.py`.

### Hero ronde 6 (Mercer)

| Model | Verbeterpunt |
| --- | --- |
| Mercer | Platen zacht en "kussenachtig" (lijf uit simpele vormen versmolten). Paneellijnen zacht. Handschoenen wantachtig. Leest van dichtbij wat speelgoedachtig: meer harde randen, scherpere afschuiningen en echte vingers voor close-ups. Vizier vrij teal |
| Mercer clips | Reload en Shoot subtiel op spelzoom. Steunhand overlapt de pistoolhand bij tweehandig richten. Down eindigt ~0,85 naast de oorsprong. Wapen-socket-oriëntatie pas in Unity te bevestigen |

### Torens ronde 7 (Gun Turret, Cannon, Laser, Flak, Gate Gun, sokkel, sterren, wrak)

| Model | Verbeterpunt |
| --- | --- |
| Alle torens | De A/B-looks van niveau 3 bestaan, maar de data kent nog geen paden. Tot die er zijn is T3A de standaardlook van niveau 3 |
| Laser | De datakleur (1, 0.35, 0.45) is warm. Het model gloeit cyaan. Moet de straal mee? Vraag voor Remco |
| Cannon | De lichtring van de sokkel valt grotendeels onder de brede romp. Amber zit vooral in de zijlichten en het kijkspleetje |
| Gate Gun | Getest op een nepbalk. De klem en de rangplaat moeten in Unity tegen de echte `GateView`-balk worden gecontroleerd |
| Wrak | Leest als "verbrande toren", nog vrij netjes. Rook en vonken komen uit VFX |
| Sokkel | Ongeveer 1.400 driehoeken (ventilatieroosters, sterren en de ring). Kan omlaag als het budget knelt |

### Omgeving ronde 8 (Crystal Plains-kit)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Stoeprand | Leest als een rij brokken en tanden (kantelen), niet als één doorlopende rand |
| Kaartrand | Het ravijn aan de voorkant is een grote mistige muur in beeld. Kliffen wat bobbelig |
| Rift | Lichte naad waar de bovenkant op het terrein aansluit. Alleen rechte stukken, nog geen bochten of kruisingen |
| Plein | Het sigil herhaalt per tegel van 3 m en zit niet in het midden van het plein. In Unity de UV-oorsprong per plein centreren |
| Oevers | Eén oeverstijl voor alle vloeistoffen. De vijver in de diorama is een cyaan placeholder-vlak |
| Algemeen | De diorama oogt sfeervol en rijk. De weg is duidelijk herkenbaar, de bouwplekken ook |

### Eigen kant ronde 9 (gate, basis, drones, gadgets)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Gate | Zonder het veld uit Unity leest hij als een doelpaal. G1 en G2 lijken op spelgrootte erg op elkaar. Lage UV-dichtheid, dus zacht in close-ups |
| Basis | De beschadigde staat verschilt op spelgrootte maar matig. Hij leunt op VFX en het flikkeren van de kern. Lage UV-dichtheid |
| Drone_Repair | 3.084 driehoeken, boven het budget van 2.500 (vier ducts) |
| Ground Drone | Het minst uitgesproken gadget |
| Kleurvragen | De Attack-drone is in de data koraal (warm), het model gloeit azuur. De Mine is in de data rood, de ring is amber. Het rode kruis op de Repair-drone is vervangen door een cyaan plus in een zeshoek (een rood kruis is een echt embleem en warm). Advies: de data volgt de modellen |

### Ronde 10 (P2-vijanden Stinger, Bat, Bulwark, Oozer en vijf bazen)

| Model | Verbeterpunt |
| --- | --- |
| Stinger | Van dichtbij nog duidelijk een (aardse) schorpioen; de platen zijn vrij gladde schijven. Magenta gloed is fel; de oogkraag leest als een witte streep |
| Bat | De kop blijft klein en rond. Tide en Frost zijn per element koel (blauw); dat is een datakeuze. Alleen Ember, Moss en Dust zijn gebakken |
| Bulwark | Schouderplaten en vizier lezen nog wat als schijven. Niet gespiegeld: een kwart minder texeldichtheid dan de gespiegelde soorten. Datakleur accent is cyaan; het zwaard is roze Crysium (warm) |
| Oozer | De mantel ligt als een tweede brood op het lijf. Oogbollen branden wit |
| AcidEmpress | Kroonscherven wat snoepachtig van dichtbij. De zijstaarten verdwijnen deels achter de hoofdstaart |
| CrystalWarlord | Het grote schild bedekt van 3/4 een groot deel van het lijf. Lichte kristalgloed; in Unity de emissie iets lager |
| SkySovereign | **Data-mismatch:** in `ProjectGenerator.Bestiary` is de SkySovereign de rog-baas en de StormMatriarch de vleermuisbaas. Het model is een vleermuisbaas (past bij het StormMatriarch-model als rog-baas). Het tweede vleugelpaar is van boven weinig zichtbaar |
| SlimeMonarch | Smal voor een baas (2,0 lang, 0,9 breed). De voet gloeit fel op de zool als ze opricht (Flood) |
| RollerKing | Naden gloeien breder dan bij de Roller (bewust, maar druk). De haken schampen de grond in de Roll, net als bij de Roller |


### P2-torens en gadgets ronde 11

| Onderdeel | Verbeterpunt |
| --- | --- |
| Cryo en CryoBeam | Het meest gelijkende paar in de roster: allebei witte en cyaan cilinders |
| Railgun | Van boven een felle lans (goed). Close-up en macro zijn slecht in beeld gebracht. De kroon van T3B is klein |
| Tesla | De magenta-violette gloed zit dicht bij het roze Crysium van de aliens |
| RATCarrier | Klemmen van opzij slecht zichtbaar. De armen lezen als één steel |
| LOD1 | Boven ~2.000: StormRifle T2 2.620, Cryo T3 2.398, Acid T3A 2.396, Artillery T3 2.352 |
| Kleurvragen | Artillery is in de data oranje, het model amber. Support is in de data groen, het model signaalgeel. De kern van de Lava Ball is bewust oranje (dat is het wapen), de schil heeft onze kleuren |

### Curator-besluiten voor de import (2026-11-04)

- **SkySovereign / StormMatriarch:** in de data is de StormMatriarch de vleermuisbaas en de SkySovereign de rogbaas.
  Bij de modellen is dat omgekeerd. Besluit: bij de import kiest elke baasdefinitie het model van haar eigen
  soortfamilie (vleermuisbaas → vleermuismodel, rogbaas → rogmodel). Asset-id's blijven gelijk. Daarbij nagaan of de
  dialoog in level 4 ("smaller contacts detaching") en de baaskracht nog kloppen.
- **Kleuren in de data volgen de modellen**, als de data een alien koel kleurt of onze kant warm. Dat geldt voor de
  laserstraal, de Attack-drone, de Mine, de Artillery, de Support, het Bulwark-accent en de Stinger. Remco kan dit
  terugdraaien.
- **Bat-skins Tide en Frost** zijn koel van element en nog niet gebakken. Volgen later, met een warme gloed.
