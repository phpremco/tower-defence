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
| ☑ | P3 | **Shardback** | IJskever met een rug van ijsprisma's die afspringen | Import stage 4b: in het spel; de zes prismaclusters springen af met het pantser (`ModelArtDefinition.ShedBones`, ijsscherven per cluster, het lijk ligt zonder), gloed 2,0 zodat het ijs voorgaat. Ronde 15 klaar: donker leisteenviolet schild met zes clusters gedraaide ijsprisma's, elk een los eiland op een eigen bot (`Shard_1..6` in afwerpvolgorde), in amber gloeiende kassen die zichtbaar worden als een cluster weg is. Ploegkop met ijshoorn, zijwaartse kaken rond een amber keel (vorstspuug), vier amber ogen, vaste rijpkristallen langs de flanken. Clips Walk/Idle/Hit/Death/**Shatter**. 3.940 driehoeken, 23 botten, LOD1 1.698 / 15 |
| ☑ | P3 | **Mireleech** | Moerasbloedzuiger: lang en laag, met zuigmond | Import stage 4b: in het spel; Latch is de eigen beet (frame 13). Modderspoor nog niet. Ronde 15 klaar: zeven segmenten met donkere rugringen, haken en limoen chevrons, olijf vlees met limoen gloed ertussen, gele zintuigputjes, een zuignap van drie lipbladen (eigen botten) met haaktanden en een achterzuignap. Clips **Crawl** (een bult loopt van staart naar kop, blijft op de grond)/Idle/Hit/Death/**Latch** (beet op frame 13). 3.906 driehoeken, 11 botten, LOD1 1.700 / 11. Modderspoor blijft VFX |
| ◐ | P3 | **Knitback** (naamvoorstel) | Hout: herstelt HP tijdens het lopen, camouflage (teaser level 15, Mirewood 16–19) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset` + `_Corrupted`); bijzondere clips als rollen (Burrow, Emerge, Swim, Regrow) vastgelegd, gedrag volgt met de soort. Ronde 15 klaar: schildpadachtig lijf onder gespleten schorsplaten met mos (twee dicht bij elkaar liggende waarden: camouflage), drie sappeulen met jonge scheuten op eigen botten (`Pod_1..3`), wortelpoten, schorsmasker met amber ogen. Clips Walk/Idle/Hit/Death/**Regrow** (peulen zwellen). 3.952 driehoeken, 18 botten, LOD1 1.698 / 12 |
| ◐ | P3 | **Slagstrider** (naamvoorstel) | Vuur: loopt door lava, laat een brandend spoor achter (level 17, Ashen Reach 6) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset` + `_Corrupted`); bijzondere clips als rollen (Burrow, Emerge, Swim, Regrow) vastgelegd, gedrag volgt met de soort. Ronde 15 klaar: kroesvormig lijf tussen vier lange steltpoten met hittebestendige slakkegels als voeten, houtskoolplaten met gesmolten naden, drie ovenschoorstenen, gloeiende buikzak (bot `Drip` voor het spoor), spleetkop zonder ogen. Clips Walk/Idle/Hit/Death. 3.957 driehoeken, 13 botten, LOD1 1.699 / 8 |
| ◐ | P3 | **Siftjaw** (naamvoorstel) | Zand: graaft zich even in en ontwijkt vuur (Ashen Reach 8–9) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset` + `_Corrupted`); bijzondere clips als rollen (Burrow, Emerge, Swim, Regrow) vastgelegd, gedrag volgt met de soort. Ronde 15 klaar: lage wig met een schepkop van twee gekartelde kaken (eigen botten), vijf rode ogen, twee graafarmen met getande schoppen, okerplaten met rood-oranje naden, boorstaart. Clips Walk/Idle/Hit/Death/**Burrow** (onder de grond vanaf frame 24)/**Emerge** (door de grond op frame 8). 3.950 driehoeken, 19 botten, LOD1 1.698 / 12 |
| ◐ | P3 | **Surgefin** (naamvoorstel) | Water: duikt op uit het water, zwemt sneller op natte stukken (Mirewood 6–7) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset` + `_Corrupted`); bijzondere clips als rollen (Burrow, Emerge, Swim, Regrow) vastgelegd, gedrag volgt met de soort. Ronde 15 klaar: salamanderlijf in petrol-teal met koraal gloed, vier zeilvinnen met gloeiende rand, lokorgaan op een steel, kieuwspleten, peddelpoten, staartvin. Clips Walk/**Swim**/Idle/Hit/Death/**Emerge** (kop door het oppervlak op frame 10). 3.947 driehoeken, 16 botten, LOD1 1.699 / 10 |
| ◐ | P3 | Verdorven skin-set | Donkere versies van alle soorten met paars kristal (The Core) | Import stage 4b: art klaar, wacht op data: `Data/Art/<Id>_Corrupted.asset` voor alle 27 exports met een skin (zelfde mesh en clips, eigen maps). Ronde 15 klaar: `build_corrupted_skins.py` schrijft `Exports/<Soort>/Skins/Corrupted/` voor alle 17 soorten plus 10 levelbazen (`--bosses`; Gravemaw niet, twee materialen): bijna zwart violetgrijs, eigen gloedpatroon in violet, een 3D-netwerk van paarse Crysium-aders. Zelfde mesh en UV's: in Unity alleen een tweede materiaal |

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
| ◐ | P2 | **Gravemaw** (continent 1) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 14 klaar: boormuil van vijf pantserbladen (dicht een gloeiende graafkegel, open een ster rond een witgloeiende keel met Crysium-tanden), zes brede pantsersegmenten met Crysium-platen en een naar achter geveegde kristalkam, twee schopklauwen, drie paar korte stekelpoten, staartschop met kristalvinnen. Twee materialen (lijf + kristal). Clips Crawl/Idle/Hit/Death/Roar/**Dive**/**Emerge** met puinbrokken op eigen botten. Los: `Env/Gravemaw_Hole` (krater). Zie README ronde 14 |
| ◐ | P3 | **Cinder Matriarch** (2) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 16 klaar: moeder van de Slagstrider-lijn: vier steltpoten met knieën boven de rug en hittevaste voetkegels, een slakmantel met chevrons, een lange wigkop met vier gloeiende kaken onder een kroon van ember-Crysium-hoorns, twee ember-zeisen, en een enorme ovenzak van onregelmatige slakschotsen met smalle rode scheuren en wijde gele kloven, drie larveschoorstenen (`Vent_1..3`, bot-schaal) en een druppelpunt (`Drip`, brandend spoor). Twee materialen. Clips Walk/Idle/Hit/Death/**Spawn** (larven op frame 30, +3 per schoorsteen)/**Roar**. 23.051 driehoeken (17.995 + 5.056), 26 botten, LOD1 8.997 / 21. Plus de **vuurlarve** `FireLarva`: korte slakgrub met gloeiende naden, gloeiende kop, stompe pootjes. Crawl/Idle/Hit/Death, 1.458 driehoeken, LOD1 700, Unity-schaal 0,6 |
| ◐ | P3 | **Frost Warden** (3) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 16 klaar: de enige rechtopstaande baas, een bidsprinkhaan-kentaur: vier zuilpoten, een gepantserde sleep met chevrons, een hoge romp met een borstplaat waarop het **zegelteken** (amber-goud, versleten) staat, ook op de schouder, een kap met een spleetgezicht en een ring amberkleurige ogen, een gebroken halo van ijsprisma's (`Halo`), onderarmen die ijsglaives worden. Twee materialen (lijf, ijs). Clips Walk/**WalkEnraged** (gebogen, 1,3x pas)/Idle/Hit/Death (knielt)/**FreezePulse** (inslag frame 30)/**Enrage**. 23.880 driehoeken (17.507 + 6.373), 23 botten, LOD1 9.496 / 22 |
| ◐ | P3 | **Rotbloom** (4) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 16 klaar: een brede lage heuvel van rottende schorsplaten en mos op zes wortelpoten met een slijmrok (`Trail`), op de rug een reuzenaasbloem: vijf puntige, gekartelde wijnrode bloembladen met haakpunten en doornen (`Petal_F/FL/FR/BL/BR`), koraal tot limoen binnenin, een limoen keelput met klauwvormige meeldraden (`Bloom`). Vier sappeulen (`Pod_*`, bot-schaal), een schorsschedel met schepkaak, amber ogen en dood-hout-gewei. Twee materialen (lijf, bloem). Clips Crawl/Idle/Hit/Death (bloem verwelkt)/**Regrow** (piek frame 28)/**Bloom** (sporen frame 34). 23.968 driehoeken (16.989 + 6.979), 27 botten, LOD1 9.498 / 25 |
| ◐ | P3 | **The Shattered** (5) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 16 klaar: een gebochelde knokkelloper (oude Burrower-bronzen platen) met spadevuisten, slagtanden en een gebarsten kaak met kristaltanden. Acht losse pantserplaten (`Shell_1..8`, aparte eilanden boven een gloeiende kern) en zes kristalclusters binnenin (`Crys_1..6`): **drie fases via bot-schaal** (heel, gebarsten: bultplaten weg, opengebarsten: alle platen weg en een witgloeiende kristalster). Clips Walk/Idle/Hit (fase 1)/**PhaseBreak1** (frame 12)/**PhaseBreak2** (frame 14)/Death (eindexplosie frame 46). 24.070 driehoeken (17.493 + 6.577), 27 botten, LOD1 9.498 / 26. Unity: fasewaarden in LateUpdate (zie README ronde 16) |
| ◐ | P3 | **The Sealed One** (6) | Import stage 4b: art klaar, wacht op data (`Data/Art/<Naam>.asset`, twee materialen High/Low, krachten als clips met moment); fasen, onderduiken en opstijgen vragen nog code (zie STATUS). Ronde 16 klaar: alleen het bovenlijf is uit de grond: een ribbenkast van as-ivoren botsteen met violet licht, violette corruptiekristallen, een hoge oogloze kop van twee gezichtsplaten die openen op een violette lichtzuil (`Plate_L/R`), een kroon van drie gebroken ringen met zegeltekens (`Ring_1..3`), twee reusachtige onderarmen geplant op de grond, twee dunne bovenarmen in gebed (zegelteken in de handpalm), puin en wortelranken rond de spleet, en zeven gebroken zegelstenen (`Seal_1..7`) met het zegelteken in goud. Clips Idle/**Rise** (zegel breekt frame 22, handen slaan neer frame 92)/**Summon** (piek frame 44)/Hit/Death (zegel dicht frame 128). 34.478 driehoeken (25.486 + 8.992), 29 botten, LOD1 13.998 / 26. Begint 5,2 m onder de grond: terrein moet afdekken |

---

## 2. Hero en personages

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☑ | P1 | **Cole "Pathfinder" Mercer** | Import stage 3b: in het spel (alle clips op rol, Shoot/Reload/Slash/Hit op het bovenlijf-masker vanaf `Spine1`, looppas op snelheid, aim via de rug, lichten doven bij Down) en in menu, portret en cutscene. Ronde 6 klaar: navy gewatteerd pak, witte keramische platen, helm met rookglas-vizier en gele kruinstreep, gelaagde schouderstukken met gele rand, rugpak met twee cyaan cellen, holsters op beide dijen, Crysium-stof op de laarzen. 9.800 driehoeken, LOD1 4.000 op hetzelfde rig (31 botten, sockets `Weapon_L/R`, `Holster_L/R`, `Back`). Clips Idle/Walk/Run/Shoot/Reload/Slash/Shockwave/Repair/Hit/Down/Revive; Shoot, Reload en Slash met bovenlijf-masker (vanaf `Spine1`). Portretbuste gerenderd. Zie README ronde 6. **Ronde 16 (polish):** platen zijn nu losse harde schalen met afgeschuinde randen en echte paneelgroeven over het gewatteerde pak (eigen remesh-laag, eigen bake per laag), handschoenen met losse vingergroepen, steunhand omvat de pistoolhand van links, grotere Shoot-terugslag en Reload-boog, Down eindigt met de heupen 0,39 naast de oorsprong (was 0,89). 9.752 driehoeken, LOD1 3.996; botten, sockets en clipnamen ongewijzigd |
| ☑ | P1 | Mercers **pistool** en **laserzwaard** | Import stage 3b: in het spel (pistool in `Holster_R`/`Weapon_R`, gevest op `Back`/`Weapon_R`, kling additief in de wapenkleur, flits en schot uit de loop; Quick- en Wide-varianten verborgen tot de perks er zijn). Ronde 6 klaar: pistool 1.250 + los magazijn 120 (Quick Reload-magazijn 200), gevest 760 (Wide Slash-gevest 800), twee kling-meshes met UV-verloop voor een emissieve shader. Eén 512-materiaal. Alles in "grip space" van de socket: identity onder `Weapon_R`. Dual Wield via `Weapon_L`/`Holster_L`; Crysium Blade volgt |
| ☑ | P2 | **Dr. Elena Voss** (volledige figuur) | **Ronde 20 (MPFB 2, `build_voss3.py`):** gebouwd op een MakeHuman-basis (MPFB 2, CC0): volwassen vrouw van rond de 40 met natuurlijke vrouwelijke verhoudingen (taille, heupen, buste), zacht ovaal gezicht met natuurlijke kaak, iets grotere ogen en een fijnere neus via MakeHuman-targets, vriendelijke rustmimiek. MPFB-huid (vereenvoudigd: geen poriën of stoppels), ogen, wenkbrauwen (in de huid geschilderd), tanden, haarknot (hair01) en schoenen; kleding (labjas tot de knie, revers, kraag, zakken met de handen erin, borstzak, knopen, NTI-patch, navy col met cyaan rand, broek, headset, gele badge) opgemeten aan haar lichaam. 25.228 driehoeken, één materiaal, 2048-maps (Low 1024), zelfde 10 botten en clips, shape keys Blink/Smile/JawOpen/Frown uit faceunits01. Unity: maxTextureSize voor Voss naar 2048 en importBlendShapes aan. Zwak: het haar is MPFB's knot-schil (streep op de kruin, rafelige rand boven de oren), de huid is vrij glad, de microfoon werpt een schaduw op de kaak, geen 3D-wimpers, lage schoenen in plaats van laarzen, lengte 1,56 m (ogen op 1,44 gehouden) |
| ☑ | P2 | **ORA** (hologram) | Import stage 3b: dialoogportret (zonder hologram-shader). Ronde 13 klaar: geen gezicht: facetkern (icosaëder met binnenste octaëder), drie gyroscoopringen van bogen, een kroon van acht scherven, één horizontale "blikbalk" die verbreedt als ze praat, en een emitterschijf. Cyaan. UV1 = banden voor een glitch-shader. 1.622 driehoeken. Clips Idle/Talk/Glitch |
| ☑ | P2 | **NTI-embleem** | Import stage 3b: portret van het NTI-bericht (gloed x0,35, de ster gaat naar goud). Ronde 13 klaar: eigen ontwerp: afgeschuinde zeshoekige badge (gunmetal rand, koloniaal blauw veld), witte horizonboog, opkomende vierpuntige ster (lichtgevend), letters N T I. 412 driehoeken, statisch |
| ☑ | P2 | **Pioneer-1** (sonde, heel) | Import stage 3b: portret van de Pioneer-1-log. Ronde 13 klaar: zeshoekige lander op drie poten, goudkleurige isolatiefolie onder, wit boven met NTI-embleem en "P-1", grote schotel, camera-mast met mint lens, twee zonnevleugels, kernboor onder de romp. 3.300 driehoeken, statisch |
| ◐ | P3 | **Pioneer-1 wrak** | Import stage 3b: geïmporteerd (`Data/Art/Story/Pioneer1_Wreck`), stage 4b: als landmark in `Mirewood.asset` (`EnvironmentSet.Landmark("Pioneer1_Wreck")`), plaatsing volgt met level 8. Ronde 13 klaar: gekanteld en half weggezakt in een modderheuvel, poot afgebroken, een vleugel afgescheurd en de andere geknikt, schotel gebroken op de grond, schroeiplekken; Crysium groeit uit de romp en de modder, pruimkleurige varens met gloeiende punten en ranken. Statuslampje gloeit nog. 4,980 driehoeken, statisch decor |

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
| ◐ | P3 | Nieuwe hybrides uit het script: **Shatter**, **Arc Laser**, **Corrosive Blast**, **Storm Flak**, **Thermal Shock** | Ronde 18 klaar (T1–T3, rig en sterren als ronde 7/11). Shatter = Cryo + Railgun: rails met plasmakanaal op een berijpte koelcilinder, Cryo-bussen, vinflappen als mondingskroon. Arc Laser = Laser + Tesla: vorkjuk en lens met Tesla-spoelen om de zender, Crysium-cellen, boogpennen rond de lens (sockets `Arc_n`), T3 brede halo. Corrosive Blast = Cannon + Acid: wigromp en blokrem, zuurtank met kijkglas achterop (bot `Slosh`), T3 bolle druktank en dubbele loop. Storm Flak = Flak + Tesla: geheven lopen met spoelen, schild, spoelspits achterop (sockets `Arc_n`), T3 halo. Thermal Shock = Cryo + Lava Ball: berijpte cilinder, thermietbal als hittekern achterop, T3 koude en hete loop naast elkaar. 3.378–4.992 driehoeken, LOD1 1.502–2.550 |
| ◐ | P1 | **Sterren** (upgradeniveau) en **bouwplek-sokkel** | Ronde 7 klaar: zeshoekige sokkel met gevarenrand, ventilatie en lichtring. Een schuine rangplaat aan de camerakant met drie sterren op de botten `Star_1..3` (in elke toren). Losse `Tower_Star.fbx` |
| ◐ | P1 | **Wrak en scrap-stapel** | Ronde 7 klaar: verbrande sokkel, omgevallen romp, schroot en gloeiende sintels in `Tower_Wreck.fbx`, met `Debris_Gun/Cannon/Laser/Flak/GateGun` |

---

## 4. Gadgets en padobjecten

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☑ | P1 | **Land Mine** | Import stage 3a: in het spel (Arm bij plaatsen, Idle; datakleur nu amber). Ronde 9 klaar: lage gunmetal schijf met zes ankerklauwen, blauwe band, witte drukkap met gevarenrand en een amber ring als hij scherp staat (bot `Ring`). Clips Arm/Idle/Trigger, socket `Blast`. 1.212 driehoeken, LOD1 632. Open punt: datakleur is rood |
| ☑ | P1 | **Chrono Field**-emitter | Import stage 3a: in het spel (Deploy, Idle; de code-ringen zijn weg, schijf en motes blijven). Ronde 9 klaar: driepoot, blauwe kolom, Crysium-scherf tussen twee pinnen in twee tegendraaiende gyroscoopringen (botten `RingA/RingB`), projectorring bij de voet, socket `Field`. Clips Deploy/Idle. 2.008 driehoeken |
| ☑ | P2 | **Lava Ball** | Import stage 3a: in het spel (Drop, bal rolt op afstand, kijkt in de rijrichting). Ronde 11 klaar: onze machine (thermietroller), geen steen: acht witte pantserplaten en drie gunmetal banden met gele naven rond een gloeiende gesmolten kern (oranje: het wapen zelf, de schil blijft in ons palet). Bot `Ball` rolt om X, sockets `Core`, `Trail`. Clips Drop/Roll/Burst. 1.680 driehoeken |
| ◐ | P2 | **R.A.T.** (Remote Assault Tank) | Import stage 3a: art klaar in Unity (`Data/Art/Colony/Gadget_RAT`, `Gadget_RATCarrier`), maar er is nog geen padobject in de data dat hem draagt. Ronde 11 klaar: rupstankje (familie van de Ground Drone) met witte schorten, ronde koepel met cyaan oog, één loop; wielbotten, sockets `Muzzle`, `Lug`. Clips Drive/Fire/Idle/Land (2.180). Plus de **carrier-drone** `Gadget_RATCarrier` (ronde-9-dronefamilie, vier ducts op armen, twee haakklemmen, socket `Cargo`), clips Carry/Release/Fly (2.384) |
| ☑ | P2 | **Barrel** en **Air Net** | Import stage 3a: in het spel (Air Net met gloeiende draden tussen `Strand_Ln` en `Strand_Rn`). Ronde 9 klaar (statisch): vat in een gunmetal kooi met gevarenbanden en amber ruitjes (1.208); twee palen met gele spoelen op een drempel, sockets `Strand_L1..R3` voor de VFX-draden (1.460) |
| ☑ | P2 | **Ground Drone** (padobject) | Import stage 3a: in het spel (Drive als hij rijdt, kijkt in de rijrichting). Ronde 9 klaar: vierwielige ramrobot met witte ploeg, cyaan oog. Clips Drive/Idle, wielbotten. 928 driehoeken |
| ◐ | P3 | **Decoy Beacon** (2), **Barricade** (4), **Spike Strip** en **Rollback Field** (5) | Ronde 18 klaar. Decoy: seismische stamper op een driepoot met draaiende lokkop (signaalgeel), clips Deploy/Idle/Pulse, sockets `Pulse`, `Lure` (1.448). Barricade: vijf scharnierende keramische platen op een drempel over de weg (1,9 breed, botten `Slab_1..5`), clips Deploy/Hit/Break (1.592). Spike Strip: drie platen met pinnenrijen op botten `Spikes_1..3`, clips Deploy/Idle/Trigger (1.212). Rollback: hexvoet met repulsorschotel naar de vijand en een tegendraaiende chevronring (teal), clips Deploy/Idle/Pulse, sockets `Push`, `Field` (1.746) |
| ◐ | P3 | Superwapens: **Payload Drop**-bom, **Overcharged Crystal**, **Toxin Bomb** | Ronde 18 klaar (`Super_*`). Bom: neus omlaag, pivot op de neuspunt, vinnen in een ring (bot `Fins`), clips Fall/Arm (1.170). Crystal: draaiend vijfhoekig Crysium in drie klemarmen op een driepoot, drie zwevende splinters, clips Deploy/Idle/Overload, sockets `Core`, `Blast`, `Arc_1..3` (1.312). Toxin Bomb: bus met gifgroene vensters en vier ventielen (sockets `Vent_1..4`), clips Fall/Vent (1.516) |

---

## 5. Drones

Animaties: **Fly, Hover, Work** (lichtbundel, vuren of graven).

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☑ | P1 | **Repair-drone** | Import stage 3a: in het spel (Idle/Fly/Work, rotors op snelheid, straal en vonken uit `Beam`). Ronde 9 klaar: ronde quad met vier kleine ducts, cyaan plus-in-zeshoek (geen rood kruis), arm met emitter, socket `Beam`. Clips Idle/Fly/Work. ~3.080 driehoeken (boven 2.500: vier ducts), LOD1 ~1.970 |
| ☑ | P1 | **Harvest-drone** | Import stage 3a: in het spel (Work bij het boren, straal uit `Mine`, vrachtcilinder gloeit als hij geladen is). Ronde 9 klaar: brede hauler op twee grote ducts, boor met drie klauwen, losse vrachtcilinder `Drone_Harvest_Cargo` (amber). Clips Idle/Fly/Work. 2.294 driehoeken |
| ☑ | P1 | **Attack-drone** | Import stage 3a: in het spel (Work tijdens het vechten, flitsen op `Muzzle_1/2`, spoor uit `Thruster`; datakleur nu azuur). Ronde 9 klaar: pijl met pijlvleugels, ducts op de vleugeltips, dubbele kanonkap, staartstraal (azuur). Clips Idle/Fly/Work (om en om vuren), sockets `Muzzle_1/2`, `Thruster`. 2.208 driehoeken. Open punt: datakleur is koraal (warm) |
| ☑ | P2 | **Builder-drone** en **hauler** (cutscene) | Import stage 3b: in de cutscene (builders lassen rond de basis die opkomt, vonken uit `Weld_L/R`; haulers vliegen erts, vrachtcapsule gloeit beladen). Ronde 18 klaar (familie van ronde 9). Builder: drierotor met twee lasarmen en een geel bouwbaken, sockets `Weld_L/R`, clip Work = lassen (2.204). Hauler: lichte tandem (één grote duct voor en achter), vrachtcapsule met amber venster als eigen mesh `Drone_Hauler_Cargo`, Work = loslaten (1.376, licht voor zwermen) |
| ◐ | P3 | Bombardement-drone (Pathfinder Strike) | Ronde 18 klaar: vliegende vleugel met twee ingebouwde ducts, twee stuwers, bommenluik met twee deuren en twee paar bommetjes (botten `Bay_L/R`, `Bomb_1/2`), sockets `Drop_1..4`, `Thruster_L/R`. Work = één aanval (2.148) |

---

## 6. Basis, gate en blokkades

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☑ | P1 | **Gate** (blokkade op het pad) | Import stage 3a: in het spel (G1–G3 per niveau, Hit/Repair/Build/Collapse, overlay en losse panelen onder 50 %, vonken uit `Spark_n`, wrak, Gate Gun op de balk). Ronde 9 klaar: G1/G2/G3 (kaal, steunplaten en hoekpantser, vestingversie met hoorns en tankvallen), 18 hexpanelen op eigen botten (clip `Repair` en `Build`), overlay `_Damage`, losse `Gate_Wreck`, clips Hit/Collapse. Sockets `GunMount` (Gate Gun-zadel, 1,17), `Field`, `HeroPost`, `Spark_n`. 5.200–7.600 driehoeken |
| ☑ | P1 | **Basis** (eindpunt, 800 HP) | Import stage 3a: in het spel (schotel draait, kristal ademt, licht uit `Light_Core`/`Light_Command`, rook uit `Smoke_*`, overlay onder 50 %, wrak in de flits; ook in het upgradescherm). Ronde 9 klaar: acht modules met eigen pivot (dek, commandopost, schotel, landingsplatform, kern, kristal, habitat, aansluitkasten) voor de timelapse, overlay `Base_Damage`, `Base_Wreck`, sockets als empties. 7.250 driehoeken, LOD1 3.190 |
| ◐ | P1 | **Basiswapens / -turrets** | Ronde 9 klaar: `Tower_BaseTurret` T1/T2/T3 = Gun Turret-kop op een vierkante bastion-sokkel in basiskleuren, zelfde rig en clips als Tower_Gun. 3.204–4.440 driehoeken |
| ☐ | P2 | Modules van het upgradescherm (diorama) | Hergebruikt de modellen van de basis |
| ☑ | P2 | **Grondstofpunt** (Crysium-ader voor de harvest-drone) | Import stage 3a: in het spel (Full, Half bij de helft, Empty bij leeg; 1,2x geschaald, gloed 3). Ronde 14 klaar: `Env/ResourceNode` met drie meshes (Full, Half, Empty) op één 1024-set: goud-amber erts-Crysium uit een gespleten basaltblok; Half met afgezaagde, gloeiende snijvlakken, Empty alleen doffe stompen |

---

## 7. Omgeving per level en continent

### Crystal Plains (P1)

| Status | Model | Extra |
| --- | --- | --- |
| ☑ | **Crysium-kristallen** (3 varianten) | Import stage 4a: vervangen de procedurele Crystal-props (op de oude maat, x0,87). Gloed 1,2 in plaats van 2,2, zodat de facetten lezen; buiten het veld gedimd |
| ☑ | **Rotsen** (3 varianten) | Import stage 4a: PropKind Rock (x0,76), gloed 1,3. Rock_C is wat druk |
| ☑ | **Nestkrater / burrow** | Import stage 4a: `Burrow_A` bij elk nest op grond, verhoging of rift (0,75x), met het licht, de sporen en de gloedpoel van het oude nest |
| ☑ | **Rotsspleet (Rift)** voor het element Steen | Import stage 4a: `Rift_*` op rechte spleten van één cel breed (kap, lange stukken, kap). De rifts van continent 1 zijn vooral vlekken van 2-5 cellen: die houden het uitgegraven terrein met de RiftRock-laag in de grond-shader |
| ☑ | **Scree** (puinveld) | Import stage 4a: PropKind Scree (riftranden) |
| ☑ | **Boulder, Column, Spike, Spire** | Import stage 4a: de PropKinds Boulder, Column, Spike en Spire |
| ☑ | **Planten:** Frond, Tendril, GlowTree, Mushroom | Import stage 4a: de PropKinds; wiegen in de shader (vertexkleur `Sway`, in Unity opnieuw opgebouwd, zie "Na import stage 4a") |
| ☑ | **Egg** (eierclusters bij nesten) | Import stage 4a: PropKind Egg; schelpscherven (`Scatter_ShellFragments`) in een ring om elk nest |
| ☑ | **Kristalader in de grond** | Import stage 4a: `CrystalVein` als strooisel op open grond (4 % van de cellen) |
| ☑ | **Padrand en bouwplek-tegels** | Import stage 4a: zie hieronder |

### Level-omgeving als geheel (P1, Remco 2026-11-03: "de hele environment, de weg, de gates en alles")

Het terrein wordt nu in code gebouwd en geverfd (`TerrainPainter`, `LevelVisualBuilder`, `PropScatterBuilder`). Uit
Blender komen twee dingen: modulaire kits (meshes) en naadloze materiaalsets (Albedo, Normal en Mask, 1024, tilend).
De code zet die neer volgens de layout. De spellogica en het raster veranderen niet.

| Status | Model | Extra |
| --- | --- | --- |
| ☑ | **Weg: wegdek-materiaal** | Import stage 4a: tiling-set `Road` in de grond-shader (`Game/EnvironmentGround`), precies op de celrand. Varianten per thema nog niet |
| ☑ | **Weg: rand-kit** | Import stage 4a: `RoadEdge_*` op elke open wegzijde, hoeken per rasterpunt, eindkappen naast elke gate, plein-overgang (gespiegelde kopieën uit de generator) |
| ☑ | **Pleinen** (wide roads en plazas) | Import stage 4a: set `Plaza` met de tegels vanuit het midden van elk plein (sigil gecentreerd) + `PlazaEdge_*` |
| ☑ | **Gate-ondergrond** | Import stage 4a: `GatePlinth` onder elke gate (staat grotendeels onder het gate-model) |
| ☑ | **Bouwplek-tegel** | Import stage 4b (Remco: "overal vierkantjes is lelijk"): de tegel wordt niet meer getekend (`EnvironmentSet.ShowBuildPads` uit); in rust is bouwgrond gewoon grond, tijdens het plaatsen gloeien de vrije cellen zacht in de grondshader rond de cursor (straal ~4,5 cellen). Mesh blijft geimporteerd |
| ☑ | **Grond-materialen** per terreinsoort | Import stage 4a: `Ground` met `GroundFlora` in vlekken van de weg af, `Cliff` op steile grond (plateauwanden, riftwanden, randheuvels), `RiftRock` in de rifts; height blend uit de mask (B), stof (A) glinstert. Andere elementen later |
| ☑ | **Kaartrand en kliffen** | Import stage 4a: `Cliff_Straight` en `Cliff_InnerCorner` achter het veld (achterkant en de verste 75 % van de flanken), opengelaten bij de wegeinden en de basis; de voorkant blijft open (de goedgekeurde randvulling blijft). `Cliff_OuterCorner` en `Chasm_Edge` niet gebruikt |
| ☑ | **Oevers** van vloeistoffen | Import stage 4a: `Bank_*` rond water en drijfzand (elke vloeistof behalve de rift), niet aan een wegzijde |
| ☑ | **Hoogteverschil-stukken** | Import stage 4a: `TerraceStep` op elke rand van verhoogde grond, `TerraceStep_Outer` op de buitenhoeken. `Slope` en `Ledge` niet gebruikt (geen hellingen in de data) |
| ☑ | **Kleine strooiers** | Import stage 4a: `Scatter_Pebbles` en `_CrystalSplinters` op vrije natuurgrond rond het veld, nooit op een bouwplek |
| ☑ | **Achtergrond-silhouetten** | Import stage 4a: 7 per level, 6-12 cellen achter de achterrand in de mist |

### Overige spawnpunten (EnemyOrigin)

| Status | Prio | Model |
| --- | --- | --- |
| ☑ | P2 | Import stage 4a: per terreinsoort in `EnvironmentSet.Origin` (de gloed getint met de elementkleur van het nest); continent 1 heeft nog geen nest in een vloeistof, dus in het spel nog niet gezien. Spawnpunten in water, lava, drijfzand, ijs en moeras (opening waaruit vijanden kruipen). Ronde 14 klaar: `Env/Origin_Water/_Lava/_Quicksand/_Ice/_Bog` (≤ 1.800 driehoeken, 512), pivot op het vloeistofoppervlak, Emission is een wit masker (Unity kleurt met NestColor). Grond blijft `Burrow_A` |
| ☐ import | P2 | **Ronde 23: levende spawnpunten** (Remco: "onzichtbaar bij het begin, gaten openen uit de grond, daarna een vette spawn"). `Env/Origin2_Burrow` (grond, 3.900 / LOD1 1.750, 62 bones, 6 camo-skins per continent in `Skins/`) en `Env/Origin2_Water/_Lava/_Quicksand/_Ice/_Bog` (2.400 / LOD1 1.080, 34-41 bones, wit gloedmasker). Geskinde FBX met clips `Closed`, `Marked` (lus), `Open` (eenmalig, uitbarsting frame 54), `Idle` (lus), `Spawn`; sockets `Emerge`, `Glow`, `Dust`, `Debris_n`. Vervangt `Burrow_A` en `Origin_*` als nestmodel; vraagt een SkinnedMeshRenderer + Animator per nest (niet via de instancer). Unity-notities in `ArtSource/README.md` ronde 23 |

### Latere continenten (P3)

| Status | Model | Extra |
| --- | --- | --- |
| ◐ | **Ashen Reach** (vuur en zand) | Import stage 4b: `Data/Art/Environment/<Continent>.asset` gebouwd en getest, nog op geen thema (geen levels). Ronde 17: `Env/AshenReach/Ash_*`. Sets Road (bleke gesinterde zandsteen), Ground (houtskool-scoria met gloeiende scheuren), GroundAlt (as), Sand (roestig duinzand), Cliff. Kit: doorlopende stoeprand met sintellijn, lava-oevers (touwlava, gloeiende waterlijn), mesa-kliffen in strata. Decor: LavaRock, AshDune (met half begraven ribbenkast), CharredTree, EmberBloom, Fulgurite, Scatter_Cinders |
| ◐ | **Northern Crown** (ijs, steen en vuur) | Import stage 4b: `Data/Art/Environment/<Continent>.asset` gebouwd en getest, nog op geen thema (geen levels). Ronde 17: `Env/NorthernCrown/North_*`. Sets Road (verwarmde gelegde platen), Ground (blauwzwarte lei met rijp), GroundAlt (bevroren grind), Snow (sastrugi, waarde onder de weg), Cliff. Kit: granieten stoeprand met sneeuwlijn, ijsmeer-oevers, kliffen van zeshoekige basaltzuilen met sneeuwkapjes en amber hitte. Decor: IceShard, Snowdrift, FrozenCrystal, RimeBoulder, FrostFern, Scatter_IceChips |
| ◐ | **Mirewood** (hout, water en moeras) | Import stage 4b: `Data/Art/Environment/<Continent>.asset` gebouwd en getest, nog op geen thema (geen levels). Ronde 17: `Env/Mirewood/Mire_*`. Sets Road (dijk van slib en platte stenen), Ground (veen), GroundAlt (mos met teal sporen), Mud (nat, plassen), Cliff. Kit: bleke slibsteen-stoeprand, veen-oevers met wortels, aarden wal met wortels en stronk. Decor: DeadTree, Reed, Fungus, FogVent (rapport geeft het mistpunt), MossStone, Scatter_Twigs. Pioneer-1-wrak: ronde 13, hergebruikt |
| ◐ | **Southern Crown** (ijs en gemengd) | Import stage 4b: `Data/Art/Environment/<Continent>.asset` gebouwd en getest, nog op geen thema (geen levels). Ronde 17: `Env/SouthernCrown/South_*`. Sets Road (koele platen met enkele geladen scheuren), Ground (zwart ijs met magenta aders), GroundAlt (gemengd puin), CrackedIce (gebroken pakijs), Cliff. Kit: graniet met geladen scheuren, oevers van opgestuwde ijsplaten, kruiende ijsrug. Decor: UnstableCrystal (zwevende splinters), CrackedVein, PressureRidge, ShatteredPillar, MixedBoulder, Scatter_Shards |
| ◐ | **The Core** (ondergronds) | Import stage 4b: `Data/Art/Environment/<Continent>.asset` gebouwd en getest, nog op geen thema (geen levels). Ronde 17: `Env/TheCore/Core_*`. Sets Road (zeshoekige lila platen), Ground (indigo kristalglas met gouden naden), GroundAlt (kristalgruis), Plateau, Cliff. Kit: glazen stoeprand met goudlijn, rand van corruptiepoelen, plateauranden die in de afgrond vallen. **Het zegel**: Seal_Intact/_Cracked/_Broken (8.000 driehoeken, 1024) en SealVein_Straight/_Branch. Decor: CorruptedCrystal, CrystalPillar, SealShard, HexOutcrop, Scatter_CrystalGrit |

---

## 8. Cutscene en menu

| Status | Prio | Model | Extra |
| --- | --- | --- | --- |
| ☑ | P2 | **Landingscapsule** | Import stage 3b: in de cutscene (vallend vanaf het moederschip, landing met Land, loopplank met Open, Mercer stapt uit bij `Exit`, hitteschild met eigen gloed). Ronde 13 klaar: zeshoekige capsule (wit, blauwe band, gevarenstreep), hitteschild als eigen mesh met gloeirand, vier retro-jets, drie landingspoten, loopplank die tot de grond zakt met verlicht interieur. 4.900 driehoeken. Clips **Land** en **Open** |
| ☑ | P2 | **Boor (derrick)** | Import stage 3b: in de cutscene (Sink op de voortgang, daarna Spin; de procedurele boorstang verborgen). Ronde 13 klaar: stalen vakwerktoren op een dek met gevarenranden, blauw motorhuis met lichtroosters, kroonblok met baken, takelblok, kelly en een grote gegroefde boorkop. 3.630 driehoeken. Clips **Spin** en **Sink** (3 eenheden de grond in) |
| ☑ | P2 | **Moederschip** | Import stage 3b: in de orbit-shot (Spin, capsules uit `Bay`, gloed bij `Nozzle_n`). Ronde 13 klaar: vakwerkruggengraat, wit commandomodule met lichtband, draaiende woonring met ramen, vrachtcontainers, capsulebaai, radiatoren met cyaan lijnen, motorblok met drie straalpijpen. 9.302 driehoeken. Clip **Spin** (ring) |
| ☑ | P2 | **Baken** van Mercer | Import stage 3b: in de cutscene (Deploy op de voortgang, dan Idle, licht op `Lamp`). Ronde 13 klaar: driepoot die uitklapt, telescoopmast, pulserende lamp, draaiende ring. 2.162 driehoeken. Clips **Deploy** en **Idle** |
| ◐ | P2 | **Radarscherm / commandopost-interieur** | Import stage 3b: geïmporteerd (`CutsceneCast.ConsoleArt`), maar de radar-shot is nog een UI-overlay op het veld; een shot in de commandopost is een ontwerpkeuze (open punt). Ronde 13 klaar: radarconsole (bureau, rond radarscherm, twee zijschermen, toetsen, NTI-embleem). Schermen als eigen meshes met 0..1 UV voor een shader. 3.026 driehoeken. Geen volledig interieur |
| ◐ | P2 | **Aarde** en **S-69-CRYS-globe** | Ronde 20: S-69-CRYS-textureset klaar (`build_planet_s69.py`, `ArtSource/Exports/Planet_S69/`, 4096 + Low 2048): albedo, hoogte (16-bit), normal, mask, regio-ID-mask, emissie en wolken, op de MapRegion-indeling. Nog: koppelen in Unity (`Game/Planet`), Aarde niet gedaan |
| ◐ | P3 | Ogenveld ondergronds | Ronde 26: Burrower-ogen gemaakt, zie sectie 12 |

---

## 9. VFX-texturen (projectielen, mondingsvuur, explosies)

Ronde 20 (2026-11-04, `Tools/Blender/build_vfx.py`): texturen in `ArtSource/Exports/VFX/` (+ `Low/`), indeling per
textuur in `VFX_report.json` en `ArtSource/README.md`. Nog niet in het spel: de Unity-VFX-ronde bouwt de shaders
(additief met frame blending en soft particles, six-way rook, vervorming) en de particle-opzet.

| Status | Prio | Set | Inhoud | Extra |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Explosies** | `Explosion_Small/Large/Boss` (Mantaflow vuur + rook, six-way lightmaps, 8 x 8 flipbooks) | Klein: kanon, mijnen; groot: mortier, raket, vat, lavabal, toren kapot; baas: drie opeenvolgende ontploffingen |
| ◐ | P1 | **Rook en stof** | `Smoke_Puff`, `Smoke_Linger`, `Dust_Burst` (sims, six-way) | Donker en licht kleurbaar per levelpalet; stof voor inslagen op de grond |
| ◐ | P1 | **Projectielen** | `Bolt_Core` (afgeronde staaf), `Trail_Ribbon` + `Trail_Noise`, `Plasma_Bolt`, `Rocket_Exhaust` + `Smoke_Trail`, `Beam_Core` + `Beam_Noise`, `Tesla_Arcs` | Kern wit, gloed in de torenkleur; bogen en stralen beginnen bij de loop |
| ◐ | P1 | **Mondingsvuur** | `Muzzle_Side` en `Muzzle_Front`: geweer, kanon, flak, rail/plasma, elk 4 varianten | 1-3 frames per schot |
| ◐ | P2 | **Inslagen** | `Spark_Burst`, `Spark_Streaks`, `Debris_Shards`, `Energy_Impact`, `Cryo_Burst`, `Acid_Splash` | Vonken en scherven ook als losse particles |
| ◐ | P2 | **Ringen, vervorming, decals** | `Shockwave_Ring`, `Shockwave_Distort`, `HeatHaze_Distort`, `Scorch_Decal` (4 varianten met dovende sintels) | Lokale rimpel in plaats van schermschok |

## 10. Gameplay-markers en kleine onderdelen

Ronde 25b (2026-11-04, `Tools/Blender/build_spawngate.py`, `build_projectiles.py`, `build_markers.py`): alles wat nog
uit code-primitieven of runtime-texturen kwam. Exports in `ArtSource/Exports/<Asset>/` (+ `Low/`), Unity-notities per
asset in `ArtSource/README.md` (ronde 25b). Nog niet in het spel: een Unity-ronde vervangt de code-sites hieronder
(één `Game/MarkerPack`-shader voor de getinte kanaalpakketten).

| Status | Prio | Onderdeel | Inhoud | Vervangt |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Spawn-gate grond** | `SpawnGate_Ground`: krater van schubben en vlees, gloeiende keel, zes lippen, vijf hoorns, draaiende klauwring; 3.800 driehoeken, 512 (Low 256), clips Idle/Spin/Spawn, sockets Emerge/Spores/Light | `StructureModelFactory.EnemyGate` + ring (`LevelVisualBuilder.cs:194`) |
| ◐ | P1 | **Spawn-gate lucht** | `SpawnGate_Air`: zwevende zaadpeul met haken, schuine klauwring, vier tentakels; 3.600 driehoeken, zelfde clips en sockets | idem, vliegroutes |
| ◐ | P1 | **Levensbalken** | `UI_HealthBar`: plaat, vulling (clip op UV2, ronde uiteinden), pantser in acht segmenten; atlas 256x64 | balk-kubussen in `EnemyView`, `TowerView`, `HeroView` |
| ◐ | P1 | **Plaatsingscursor en vrije vakjes** | `Marker_PlacementCursor` (1,5 vak met zachte halo) + `Marker_FreeCell` (zachte gloed, alleen bij de cursor tijdens plaatsen) | cursor-kubus `PlacementController.cs:88` |
| ◐ | P1 | **Bereikring** | `Marker_RangeRing`: band met scherpe rand en draaiend streepje + smalle randgloed | LineRenderer `RangeIndicator.cs:21` |
| ◐ | P1 | **Richting op weg en vliegroute** | `Marker_RoadCue`: zachte lichtstreep die meestroomt (één per drie vakken), lichtkralen voor vliegroutes, pulsring; geen chevrons of pijlpunten | `LevelVisualBuilder.cs:163-176`, `:513-533` |
| ◐ | P1 | **Projectielen** | `Proj_Bolt` (afgeronde staaf + gloedschil), `Proj_RailSlug`, `Proj_Missile` (gebakken, sockets), `Proj_EnemyBolt` (alien klodder met weerhaken), `Proj_PulseRing` | `ProjectileView.cs:76-110`, `EnemyBoltView.cs:37-43`, `BossAbilityView.cs:278` |
| ◐ | P2 | **Hero-markers** | `Marker_HeroSpot` (rails + embleem), `Marker_HeroStand` (ring in drie bogen, baken van gekruiste quads, gefacetteerd embleem) | `HeroController.cs:113`, `HeroView.cs:336,430-439` |
| ◐ | P2 | **Decals** | `Decal_GlowPool` (pool, zwaardgloed, energieveld + gedeelde quads), `Decal_BlobShadow` | `GlowDecal.cs:101-174`, `GateView.cs:144-148`, `EnemyView.cs:1473` |
| ◐ | P2 | **Brokstukken** | `Debris_Chunks`: tien stukken (platen, hex, balk, pijp, beugel, gevaarstreep, steen) in één atlas 512 | debris-kubussen `GateView.cs:155,196`, `BaseView.cs:102` |
| ◐ | P2 | **Drone-straal, net, verkenner** | `Beam_Tool` (open kegel), `Net_Strand` (gevlochten draad), `Recon_Scout` (kleine sonde + `Trail_Scout`) | `DroneView.cs:81`, `PathObjectArtRig.cs:118`, `ReconView.cs:261-264` |
| ◐ | P2 | **Deeltjesatlas** | `Particles_Atlas` 4 x 2: spore, mote, sintel, druppel, ichorspat, slijmplek, vorstkristal, chrono-mote | standaard particle-materiaal in `LevelVisualBuilder`, `AmbientParticlesBuilder`, `PropScatterBuilder`, `EnemyView` |
| ☐ | P3 | LOD1 voor de spawn-gates, een throat-bulge in `Spawn`, verschroeide brokstuk-varianten | zie DESIGN_NOTES ronde 25b, zwakke punten | |

## 11. UI-art

Ronde 25 (2026-11-04, `Tools/Blender/build_ui.py`): eigen UI-set in `ArtSource/Exports/UI/`, gemaakt in Blender
(geometrie met afschuining, orthografisch gerenderd); vervangt de in code getekende UI-texturen en het Kenney-pakket.
Indeling en import-instellingen in `UI_report.json` en `ArtSource/README.md` (ronde 25). Nog niet in het spel: de
Unity-ronde maakt een icon-set-asset (naam = `IconType`), zet de UiSkin-sprites om en schakelt knoppen naar SpriteSwap.

| Status | Prio | Set | Inhoud | Extra |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Iconen** | `UI_Icons` (1x en 2x): de 28 `IconType`-iconen + 12 extra (StarEmpty, BossMarker, EnemyOrigin, Drone, Base, Gear, Close, Research, Power, Enemy, Credits, Info) | Wit, gekleurd in code; aliens-markers warm voorgekleurd |
| ◐ | P1 | **UI-kit** | `UI_Kit`: panelen, kaarten (blauw/groen/geel/rood/geselecteerd/uit), knoppen in vier toestanden, balken, slider, tabs, tooltip, scrollbalk, vinkje, focusframe, hoekornament, scheidingslijn | 9-slice randen in de json; focus met lichte rand, gloed en gele hoeken |
| ◐ | P1 | **Dialoog en radar** | `UI_Dialogue` (paneel + gloedrand, naamplaat, portretlijst, pijl, radarsweep en contact), ruis/scanlines/gloed/schaduw, `UI_Radar` | Wit en kleurbaar, behalve de radar |
| ◐ | P2 | **Achtergronden** | `UI_Starfield` (menu), `UI_PreviewBackdrop` (modelvoorbeeld), `UI_MapHalo` (planeetkaart) | Volgen de oude regels (alfa = licht, halo wit) |
| ◐ | P2 | **Upgrade-scherm podium** | Ronde 27 (`build_ui_display.py`): `UI_Display/UI_DisplayPedestal` (draaitafel, 2.946 driehoeken, losse bovenring die langzaam draait, 1024/Low 512, emissie wit en kleurbaar), `UI_DisplayRing` (grijze additieve halo + quad), `UI_DisplayBackdrop` (2048 x 1024 donkere onderzoekshal) | `ModelPreview.cs:207-250` (vloerschijf, torus, gradient-quad) |
| ☐ | P2 | **Lettertype geldpopup** | `MoneyPopup` gebruikt nog het ingebouwde LegacyRuntime-font | Geen art nodig: `UiKit.Font` gebruiken |

## 12. Menu, cutscene en luchten

Ronde 26 (2026-11-04, B3 uit de visual audit): de laatste visuals die nog in code gemaakt werden, nu uit Blender.
Scripts `build_menu_diorama.py`, `build_underground.py`, `build_planet_earth.py`, `build_cutscene_fx.py`,
`build_skies.py`, `build_decor_c1.py`; import-notities in `ArtSource/README.md` (ronde 26). Niets is nog geïmporteerd.

| Status | Prio | Onderdeel | Inhoud | Vervangt |
| --- | --- | --- | --- | --- |
| ◐ | P1 | **Menu-eiland** | `Menu_Island` (13.800 driehoeken: bovenkant ~8.400, onderkant ~5.400; twee materialen): zwevend stuk S-69-CRYS-grond met korfheuvel, vijf poelbekkens op de oude plekken, voetpad rond de heuvel, verhoging voor een kleine NTI-post achterin, onderkant met strata, Crysium-aders en drie hangende spitsen. Top-maps 2048 (planaire UV), Cliff-maps 4096 × 512. Hoogtekaart (16-bit, van het spelmesh) voor `OnGround`, layout-json met 105 geplaatste exports, poelcontouren en lichten | `MenuTerrain.cs:196-236`, `MenuDiorama.cs:271-319` |
| ◐ | P1 | **Burrow-mond** | `Menu_NestMouth` (2.400, 1024): overkapte chitine-ingang met ribben, tanden, stekels en een gloeiende keel; staat over de inkeping van de veranda zodat de kruipers het donker in lopen | `MenuDiorama.cs:220-228` (zwart ovaal + torus) |
| ◐ | P2 | **Grotkit ondergronds** | `Underground/Cave_*`: Wall_A, Wall_B (met spleet), Ceiling, Stalactites, Floor_A, Floor_B (gloeiende spleet), Boulder, CrystalCluster, Vein; 300–1.950 driehoeken, 512/1024. The Core-stijl (indigo kristalglas, sporen goud) met cyaan Crysium-licht | `UndergroundSet.cs:49,125,177` (PropFactory-rotsen en -kristallen) |
| ◐ | P2 | **Burrower-ogen** | `Underground/Eye_Burrower_A/B/C` (~420 driehoeken, 512): oogleden, oogbol met geschilderde iris (spleet, balk, drie pupillen), natte cornea als tweede materiaal. Shape keys Blink en Look×4, plus vertex-kleur-rig (R/G leden, B oogbol) voor een instanced shader | `EyeField.cs:74,122` (ruitvormige quads) |
| ◐ | P2 | **Aarde (cutscene)** | `Planet_Earth` (4096 + Low 2048): albedo, normal, hoogte (16-bit), mask (R land, G natheid, B gloed, A ijs), emissie (doffe amber steden, de meeste al donker, mijnlichten), smog-wolken (RGBA bruin). Aardse continenten, zelf getekend; 52 terrasmijnen met afvalvijvers, troebele zeeën, gekrompen vuil ijs. Gloss.A = 0,05 + 0,22 × Mask.G | `EarthPainter.cs:54` (`EarthSet.cs:37`) |
| ◐ | P2 | **Cutscene-gloed en -sprites** | `Cutscene_FX/`: zon (kern, halo, streak), glint, motorgloed, uitlaatpluim, navigatielicht, plasmaspoor, beam-band; grijs en kleurbaar, additief | `SpaceKit.cs:94-143`, `SpaceBackdrop.cs:34-36`, `SurveySet.cs:59-76`, `OrbitSet.cs:189,229`, `CapsuleDrop.cs:179-182`, `FieldSet.cs:412-424` |
| ◐ | P2 | **Cutscene-decals** | `FX_DrillCracks` (2×2), `FX_CrackStrip_Drill_A/B`, `FX_CrackStrip_Swarm_A/B`, `FX_ScreamRing`, `FX_GateField` | `FieldDrill.cs:75-84`, `FieldSwarm.cs:69-70,285`, `FieldBase.cs:389,483` |
| ◐ | P2 | **Luchten per thema** | `Skies/Sky_<Thema>` (2048 + Low 512): zes continenten + Earth + Underground, donker met nevel, poollicht (Northern/Southern Crown), rook, mist, gloed van onderen; The Core en Underground als grot. Exposure en trilight-ambient per lucht in `Skies_report.json` | `SkyProbe.cs:40` (cubemap uit drie kleuren) |
| ◐ | P1 | **Decor-terugval Crystal Plains** | IceShard = `North_Decor_IceShard`, DeadTree = `Mire_Flora_DeadTree`, Reed = `Mire_Flora_Reed` (passen); Snowdrift nieuw: `Env/Decor_Snowdrift` (998, `build_decor_c1.py`) | `PropScatterBuilder.cs:283-323` (PropFactory-terugval) |
| ☐ | P3 | VisualThemes voor continent 2-6 | De luchten voor C2-C6 volgen DESIGN_NOTES ronde 17; als hun themes er zijn, de kleuren naast elkaar leggen. Zonsrichting gelijkzetten met het key light | |
| ☐ | P2 | Import ronde 26 | `MenuTerrain.Height` uit `Menu_Island_Height.png`, `PoolCells` uit de poelcontouren, props uit de layout-json; grot opbouwen uit de kit; ogen instanced met de EyeRig-shader | |

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

### Continentbazen ronde 16 (Cinder Matriarch, Frost Warden, Rotbloom, The Shattered, The Sealed One)

| Model | Verbeterpunt |
| --- | --- |
| Cinder Matriarch | Van boven en op spelzoom nog wat spinachtig (vier lange steltpoten). De ovenzak is het sterkste deel; eventueel dikkere dijen of een derde gewricht. Kaken lezen als vliegend-hertkaken |
| Fire larva | Klein op spelzoom (Unity-schaal 0,6); als ze verdwijnen tussen de Skitters, 0,7 |
| Frost Warden | Romp en armen nog wat robotachtig/humanoïde van voren; de sleep en de halo maken hem wel buitenaards. Het zegelteken op de borst is groot en fel (bewust: de hint), in Unity eventueel emissie iets lager |
| Rotbloom | Bloem leest nog als lelie van opzij. Slijmrok is een egale limoen schijf; in Unity eventueel als VFX-decal in plaats van de mesh-rand. Bloom-opening is bescheiden (bladen raken anders de heuvel) |
| The Shattered | Fase 1 gloeit al vrij roze (kern zichtbaar tussen de platen): gebruik de emissie per fase (2,6 / 3,4 / 4,5). Kop van voren wat rond en vriendelijk |
| The Sealed One | Kop van voren een eivormige helm met een violette spleet; kan griezeliger (meer lagen, scheuren). Rise en Death vragen dat het terrein alles onder z = 0 afdekt |
| Alle vijf | `titan_kit.previews` liet in eerdere testronden het LOD1-model meerenderen; opgelost vóór de laatste renders. Phase/zegel-standen zijn in de laatste renders correct |

### build_all.py (gedaan 2026-11-03)

- Toegevoegd na de basissoorten: `build_broodmother.py`, `build_packalpha.py`, `build_stormmatriarch.py`,
  `build_corewisp.py`.

### Hero ronde 6 (Mercer)

| Model | Verbeterpunt |
| --- | --- |
| Mercer | ✔ Ronde 16: platen als losse schalen met afgeschuinde randen en scherpe groeven; vingergroepen. Nog open: van onderaf is de holle binnenkant van een schouderstuk zichtbaar (spelcamera nooit); vizier nog steeds vrij teal. Oorspronkelijk: Platen zacht en "kussenachtig" (lijf uit simpele vormen versmolten). Paneellijnen zacht. Handschoenen wantachtig. Leest van dichtbij wat speelgoedachtig: meer harde randen, scherpere afschuiningen en echte vingers voor close-ups. Vizier vrij teal |
| Mercer clips | ✔ Ronde 16: Shoot/Reload groter, steunhand gerepareerd, Down eindigt bij de oorsprong. Nog open: wapen-socket-oriëntatie in Unity bevestigen. Oorspronkelijk: Reload en Shoot subtiel op spelzoom. Steunhand overlapt de pistoolhand bij tweehandig richten. Down eindigt ~0,85 naast de oorsprong. Wapen-socket-oriëntatie pas in Unity te bevestigen |

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
- **Bat-skins Tide en Frost** zijn koel van element en nog niet gebakken. Volgen later, met een warme gloed. (Ronde 14: gebakken met warme gloed via `build_bat_skins.py`: Tide zee-teal met koraalgloed, Frost ijswit met sintelgloed, in `Exports/Bat/Skins/Tide` en `/Frost`.)

### Ronde 14 (Gravemaw, spawnpunten, grondstofpunt)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Gravemaw | De binnenkant van de bloembladen en de keel is van dichtbij vlak felroze: emissie in Unity lager. Van boven nog wat pissebed-achtig. Poten zijn simpele buizen. Bij Dive rijden de voeten mee, en aan het eind van Emerge glijden ze. UV-dekking van het lijf verschilt per run (fallback niet deterministisch) |
| Drijfzand-opening | Leest meer als ring dan als zinkgat |
| IJs-opening | Gloed nauwelijks zichtbaar met de ijswitte tint |


### Ronde 13 (Voss, ORA, NTI-embleem, Pioneer-1 en wrak, cutscene-modellen)

| Model | Verbeterpunt |
| --- | --- |
| Voss | ✔ Ronde 16 herbouwd (zie rij hierboven). Nog open: de mond toont bij openen roze lipbinnenkant in plaats van een donkere mondholte; het gezicht is nog vrij glad/vlak (geen echte wangvolumes), schouders van de jas iets blokkig; navy van de top piept op enkele plekken bij de kraag. Oorspronkelijk: Gestileerd, maar nog vrij simpel en "poppig": ronde volumes, armen als buizen, de jas leest als een dikke trui. Het gezicht leest op portretgrootte (ogen, wenkbrauwen, mond), maar de knipper laat van dichtbij facetten zien rond het ooglid en een randje oog bij de ooghoek (te weinig driehoeken rond de ogen na het decimeren). De Talk-kaak rekt de onderkant van het gezicht; dat werkt op portretgrootte, niet in een close-up. Beter later: aparte ooglid-meshes en een lokaal dichter gezicht |
| ORA | De blikbalk is wit-heet en sterk; de ringen lezen als gyroscoop. Het eigen karakter komt pas met de hologram-shader (UV1-banden) |
| NTI-embleem | De ster brandt bijna wit; in Unity de emissie iets lager voor een gouden toon |
| Pioneer-1 | Folie leest als goudgele tegels, niet als gekreukte folie |
| Pioneer-1 wrak | Het afgebroken pootstuk en de modderpoelen zijn klein op spelgrootte. De heuvel is een vrij vlakke schijf; in Unity laten aansluiten op het terrein |
| Capsule | Het bovenvlak van het hitteschild gloeit oranje mee (één emissie-map voor het schild); in Unity uitfaden. In Land frame 0 steken de opgeklapte poten nog wat uit |
| Drill | De boorkop staat deels achter het motorhuis vanuit de standaardhoek |
| Mothership | ✔ Ronde 19 herbouwd (`build_mothership2.py`, na Remco's speeltest "amateuristisch"): hoekige, taps toelopende commandosectie met brug en pantserplaten, ring met kokerprofiel, schotten en raamrijen, containerrekken, laadruim met open deuren, tankring, radiatoren, zwaar motorblok met drie straalpijpen; naam NEW DAWN, rompcode, NTI-embleem en nummer 01 als decals; 19,4k driehoeken, 2048-maps, LOD1. Nog open: in Unity de 2048-limiet en de LOD1 aanzetten (zie README ronde 19); het laadruim is van binnen donker zonder eigen licht; het schip is overwegend wit en wordt in de schaduw een vlakke grijze massa; containers en rekken zijn nog eenvoudig. Oorspronkelijk: Straalpijpen branden bijna wit (bloom); de NTI-badges op de neus zijn klein |
| Radarconsole | Alleen de console, geen commandopost-interieur. Het achterpaneel zweeft een fractie boven het bureau |
| Kleurvraag | Pioneer-1 gebruikt de mint logkleur (0.55, 1, 0.8) voor lens en status; dicht bij het groen van de Acid Sprayer |

### Na import stage 1 (2026-11-04, eerste Unity-screenshots op spelgrootte)

Gezien op `imp1_*`-screenshots (lineups van alle soorten en bazen op spelzoom en ingezoomd). In Unity is per materiaal al
bijgesteld (helderheid, gloed, rim in `BlenderArtCatalog`); dit zijn de punten die in de Blender-scripts horen:

| Model | Verbeterpunt (middelste schaal, op ~20 px) |
| --- | --- |
| Skitter | Lijf en poten lezen donkerrood tegen de nachtgrond; in Unity 1,6x opgelicht. In de albedo zelf lichtere, oranje platen en een lichtere rugstreep, zodat hij zonder opheffen leest |
| Prowler | Nog steeds vooral een donkerrood silhouet (in Unity 1,45x opgelicht, gloed 3,5). Fellere ribspleten en een lichte rugtekening in de maps |
| Skyray | Koel blauw (open kleurvraag). Vintekening op spelgrootte nauwelijks te zien; grovere, lichtere chevrons |
| Wisp | Klein op spelgrootte (0,7 groot); de knop leest als een lichte bol zonder vorm. Grotere, donkere bladranden voor contrast |
| Bulwark | Van boven vooral zwaard en schild, het lijf verdwijnt. Lichtere schouderplaten |
| Alle soorten | De detail-normals vallen op spelzoom (terecht) weg; de middelste laag (naden, banden) draagt de look. Donkerdere naden en lichtere randen in de albedo helpen meer dan meer porie-detail |
| Skitter / Queen | Geen `walk_distance_per_loop` in het rapport: in Unity geschat (0,36 en 0,9). Graag in het rapport zetten |
| Queen | Geen Hit- en Death-clip (alleen Walk/Idle/Scream) en geen LOD1 |
| RayBoss (export `StormMatriarch`) | Op de Sky Sovereign (rogbaas) gezet; haar macht is een staartlaser, het model heeft daar geen clip voor (alleen Release). Een `Laser`-clip zou passen |
| BatBoss (export `SkySovereign`) | Op de Storm Matriarch (vleermuisbaas): haar Laser-clip toont nu de Element Storm (volley). Voor de vleermuizenzwerm (Spawn) is er geen clip |
| Bat-skin Mire | Niet gebakken; de Mirebat draagt nu de Moss-skin |

### Na import stage 2 (2026-11-04, torens in Unity op spelgrootte)

Gezien op `imp2_*`-screenshots (alle torens op niveau 1 en 3 naast de weg, spelzoom en ingezoomd, wrak). In Unity is al
bijgesteld: gloed 2,0 (was 1,5), koele rim 0,75, wrak-gloed 0,6. Dit zijn de punten voor de Blender-kant:

| Model | Verbeterpunt |
| --- | --- |
| Alle torens | Op spelzoom (ortho 7) ongeveer zo groot als de hero. De vorm leest goed (lichte kop, blauwe voet, gevarenrand), maar T1 en T2 verschillen van boven weinig. Een duidelijker silhouetverschil per niveau (bredere kop, extra loop, hogere mast) helpt meer dan fijn detail |
| Rangplaat en sterren | De sterren op de rangplaat zijn op spelzoom maar een paar pixels. Grotere of fellere sterren, of de plaat iets hoger |
| Wrak | De sintels bloeiden bij gloed 1,5 op als gele vlammen (in Unity nu 0,6). De `Debris_<Toren>`-stukken zijn klein (~0,2) en vallen weg naast de romp. Alleen Gun, Cannon, Laser, Flak en Gate Gun hebben debris; Tesla, Railgun, Cryo, Support en de hybrides lenen er een (Laser, Gun of Cannon). Eigen debris per P2-toren zou mooier zijn |
| Gate Gun | Klein (0,28 hoog) op de balk; de eigen rangplaat staat uit, want de gate toont zijn sterren boven de balk (ook zonder geschut) |
| Acid Sprayer en Rocket | Geïmporteerd (`Data/Art/Towers/Tower_Acid`, `Tower_Rocket`), maar er is nog geen torendefinitie die ze draagt. De additieve `Slosh`-clip wordt nog niet gebruikt |

### Na import stage 3a (2026-11-04, gate, basis, drones, gadgets en grondstofpunt in Unity)

Gezien op `imp3_*`-screenshots (spelzoom en ingezoomd, beschadigd, wrak, herbouwd, drones in een showcase op zoom 9).
In Unity al geregeld: elk model 180 graden gedraaid (zie de eerste regel), grondstofpunt 1,2x en gloed 3, gloed van de
rest 2,0 met koele rim 0,75 zoals de torens. Dit zijn de punten voor de Blender-kant:

| Model | Verbeterpunt |
| --- | --- |
| Alle exports | Na de import kijken ze naar **-Z** in Unity, niet naar +Z zoals README ("faces -Y in Blender (Unity +Z)") zegt: de hero-post van de gate staat op +Z, de geweren van de Attack-drone op -Z. Unity draait ze nu 180 graden (`Yaw`); graag de README rechtzetten of de exports omdraaien (dan `Yaw` terug naar 0) |
| Gate | Van boven lezen de pylonen als twee blauwe blokken met een dunne balk; de hexpanelen zitten op de binnenkant van de pylonen en zijn van de spelcamera nauwelijks te zien, dus het afvallen bij schade (en de `_Damage`-overlay) valt weinig op. Schade op de bovenkant (kappen, balk) zou meer helpen. G1 en G2 blijven op spelzoom erg gelijk |
| Gate wrak | Leest goed (omgevallen pylonen, balk op de weg); 4,4 breed, dus het steekt over de wegrand: prima |
| Basis | Leest goed op spelgrootte (dek, commandopost, schotel, kern). De beschadigde staat verschilt nog matig; de rook uit de sockets draagt het. Het wrak leest goed |
| Grondstofpunt | 0,8 breed is klein naast de oude gouden kristalcluster; in Unity 1,2x en gloed 3. Iets groter bouwen of feller goud in de emissie, dan kan dat terug naar 1x |
| Repair-drone | Mooi in de showcase. Nog steeds 3.084 driehoeken (budget 2.500) |
| Attack-drone | Op spelzoom klein (0,88 breed, 0,31 hoog); de staartstraal draagt de herkenning |
| Chrono-emitter | Klein (0,75 hoog) midden in zijn blauwe schijf; de ringen lezen op spelzoom nauwelijks. Iets hoger of grotere ringen |
| Land Mine, Lava Ball | Mine `Trigger` en Lava Ball `Burst` worden niet gespeeld: de logica haalt het object weg op het moment dat het afgaat (alleen de blast-VFX blijft). Daarvoor zou de view het object moeten overleven; geen Blender-punt, wel een open punt |
| Ground Drone | Leest als rijdend autootje; nog steeds het minst uitgesproken gadget |
| R.A.T. en carrier | Geïmporteerd, maar er is geen padobject in de data (Remco/ontwerp) |
| Niet geïmporteerd (geen data) | Decoy Beacon, Barricade, Spike Strip, Rollback Field, de superwapens; Drone_Builder en Drone_Hauler horen bij de cutscene (latere stage, de cutscene gebruikt nog de procedurele drones en basis) |

### Na import stage 3b (2026-11-04, Mercer, portretten en cutscene-modellen in Unity)

Gezien op `imp3b_*`-screenshots (Mercer in het level idle, rennend, schietend, slaand, shockwave, neer en weer op;
de vijf portretten; cutscene-frames van orbit, landing, baken, basis-timelapse, boor, haulers, kern en de gate).
In Unity al geregeld: alles 180 graden gedraaid (zoals 3a), de wapens niet (grip space klopt: het pistool ligt in de
hand), Mercer-buste 50 graden bijgedraaid, Voss dichterbij en meer driekwart. Punten voor de Blender-kant:

| Model | Verbeterpunt |
| --- | --- |
| Mercer | Leest goed op spelzoom en in de cutscene. Idle staat zo schuin dat de buste 50 graden moet bijdraaien; het vizier blijft vrij teal en fel in het portretlicht |
| Zwaardkling | De kling-mesh is 0,04 breed: op spelzoom een dun lijntje. Unity tekent de gloed nu 2,5x breder; een bredere kling-mesh (of een aparte gloed-schil) maakt dat overbodig |
| Voss | Het gezicht blijft het zwakste punt (zie curator na ronde 16); de witte jas brandt in het portretlicht bijna uit. Een iets donkerder albedo voor de jas helpt het gezicht |
| ORA | Leest als belicht model; de hologram-shader op UV1 is er nog niet (Unity-kant, open) |
| NTI-embleem | De ster brandt wit; in Unity gloed x0,35 voor goud. Een gouden emissie in de bake maakt dat overbodig |
| Pioneer-1 | 3,4 breed; in het portret op 0,24x. Prima |
| Capsule | Hitteschild en landing lezen goed. Na de landing blijft de schildrand oranje tot hij is afgekoeld (zo bedoeld) |
| Boor | Het vakwerk leest donker tegen de nacht; een paar lampjes langs de toren zouden helpen |
| Builder | `Work` wordt niet als loop geïmporteerd (de cutscene wikkelt de tijd zelf); geen probleem |
| Radarconsole | Geïmporteerd maar niet gebruikt: de radar-shot is een overlay op het veld (ontwerpkeuze voor Remco) |
| Niet gebruikt | Voss/ORA Talk, Blink en Glitch (de portretten zijn stills); Mercer Repair speelt alleen bij Gate Repair |

### Na import stage 4a (2026-11-04, omgeving van Crystal Plains in Unity)

Gezien op `imp4_*`-screenshots (alle zes levels van continent 1: overzicht, spelzoom, wegrand, plein, oever, nest,
rand, basis, gate, achterwand, hoek; High en Low). In Unity al geregeld: de as-wissel zit op de FBX-node en gaat mee
onder elke plaatsing; gespiegelde eindkappen en plein-overgangen uit de generator; de Sway-kleuren opnieuw opgebouwd;
gloed van kristallen 1,2, rotsen 1,3-1,4, planten 1,6, bouwplekken 1,5; decor verder dan 2 cellen buiten het veld
donkerder (albedo x0,65, gloed x0,35) en zonder schaduw. Punten voor de Blender-kant:

| Onderdeel | Verbeterpunt |
| --- | --- |
| Kleurlagen (flora ronde 8) | Unity leest alleen de eerste kleurset; bij ronde 8 is dat een bake-hulplaag. Unity bouwt `Sway` nu zelf op uit de vorm (buiging naar hoogte, fase naar de richting waarin een blad wijst, fladderen aan de buitenpunten). Netter: `env_kit2.strip_colour_layers` ook op de vier `Flora_*` van ronde 8 draaien en opnieuw exporteren, dan neemt Unity de echte `Sway` over (de importer herkent dat: rood loopt dan mee met de hoogte) |
| Assen in de README | Na de import ligt Blender -Y op Unity **-Z**: een randstuk heeft de weg op zijn lokale -Z, dus `LookRotation(naar buiten)`. Ronde 8 zegt `LookRotation(-side)` (de andere kant op); graag rechtzetten, net als bij stage 3a |
| Stoeprand | Leest op spelzoom nog steeds als een rij tanden (punt uit ronde 8). De doorlopende rand van ronde 17 ook voor Crystal Plains bouwen, met dezelfde namen |
| Rift | Alleen rechte stukken; de rifts van continent 1 zijn vlekken, dus de kit wordt bijna nergens gebruikt. Een rift-randset zoals de oevers (recht, buiten- en binnenhoek), of hoek- en vlekstukken, zou alle rifts een rand geven |
| Terras | Geen binnenhoek (drie van vier cellen verhoogd): twee rechte stukken sluiten daar niet. Een `TerraceStep_Inner` erbij |
| Driehoeken | De verre wildernis is het grootste deel van de omgeving: per level ~370 `Decor_Spire` (1.104 tris, samen ~560k) en ~720 kristallen (~1M). Een far LOD (~300 tris) voor `Decor_Spire` en `Crystal_A/B/C` halveert dat. Low houdt nu 35 % van het verre decor over |
| Bouwplek | Stage 4b: geen tegels meer in rust; alleen een zachte gloed rond de cursor tijdens het plaatsen (grondshader). Blender-kant: geen actie nodig |
| Stoeprand (Unity) | In Unity iets zachter gemaakt (albedo x0,8, gloed 0,9 in plaats van 2,2); de tanden blijven zichtbaar, zie de rij Stoeprand hierboven |
| Achterwand | De kliffen met roze aders en kristallen erop zijn van boven wat druk; minder kristallen op de bovenrand |
| Gate-sokkel | Valt grotendeels onder het gate-model; alleen aan de zijkanten zichtbaar. Prima, of iets breder dan de weg |

### Na import stage 4b (2026-11-04, continenten 2-6, P3-soorten, continentbazen)

Gezien op `env_*`/`env2_*` (elke continentset op level C1_02, met de C1-schildering, -licht en -mist), `cre_*`
(Shardback en Mireleech) en `pads_*` (bouwplekken). In Unity al geregeld: gloed Ash_Flora_EmberBloom en
Ash_Decor_LavaRock 1,6, North_Decor_FrozenCrystal 1,4, South_Decor_UnstableCrystal 1,5, Core_Decor_CorruptedCrystal
1,0 (The Core was te fel violet), Core_Decor_CrystalPillar 1,6, lichtpoelen van The Core minder verzadigd; Shardback
gloed 2,0. Punten voor de Blender-kant:

| Onderdeel | Verbeterpunt |
| --- | --- |
| Continenten 2-6 | Geen plein-set, geen eindkap/pleinovergang, geen terras-, rift- of silhouetstukken. In Unity staan de doorlopende rechte rand (eindkap, pleinrand) en de binnenhoeksteen (pleinhoek) in; pleinen gebruiken de weg (The Core: Plateau). Een eigen pleinset (met teken in het midden) per continent zou de basis meer laten spreken |
| Ashen Reach | De lava-oevers gloeien ook rond water (de C1-testlevels hebben alleen water); in een echt Ashen-level horen ze bij lava. Ember-bloemen zijn op spelzoom druk |
| The Core | Corrupted crystals zijn zeer verzadigd violet; met gloed 1,0 tonen de facetten. De plateau-randen vragen een donker violet "leegte"-vlak eronder (geen code nu) |
| Shardback | Amber kassen domineren nog steeds op spelzoom; meer ijs-blauw in de prisma's of minder amber in de kassen |
| Continentbazen | Gebouwd op spelgrootte (Unity-factor 1 op de schaal van de baas). Gravemaw-hoogte voor de levensbalk komt uit de rustpose (1,6); bij een EnemyDefinition controleren |

### Ronde 15 (P3-vijanden, nieuwe soorten, verdorven skins)

| Model | Verbeterpunt |
| --- | --- |
| Namen | **Knitback, Slagstrider, Siftjaw, Surgefin** zijn voorstellen voor Remco (eigen namen, niet uit een bestaande game). Schaal in de data ook voorstel (0,85 / 0,8 / 0,75 / 0,75) |
| Shardback | Poten nog wat dun en spinachtig voor een "stevige" kever. De amber kassen gloeien ook als het ijs er nog staat (bewust: licht onder het ijs), maar dat is vrij veel geel op spelgrootte. Afwerpen: bot-schaal op 0 in LateUpdate (alle clips zetten de schaal op 1) |
| Mireleech | De voorkant (drie lipbladen) leest van opzij als een snavel of oortjes. Lijkt van boven op een rups; de bult-kruip helpt |
| Knitback | De sappeulen lezen nog wat als vruchten (paprika). Schorsplaten zijn hoekige blokken; van dichtbij meer steen dan hout |
| Slagstrider | Bovenpoten lopen vrij vlak naar buiten; de knieën staan maar net boven het lijf (tafelpoten). Gloeiringen op de poten wat regelmatig |
| Siftjaw | Platen glad en "koekachtig"; de kaken lezen van boven als twee ronde schotels in plaats van een gekartelde wig |
| Surgefin | Loopgang weinig uitgesproken (korte poten); oranje vlekken op spelgrootte vrij groot |
| Verdorven skins | Alleen textures: de vorm blijft die van de soort. Paarse aders zijn een procedureel 3D-netwerk; op kleine soorten (Oozer) is de dekking laag |


### Curator na ronde 16

- **Mercer:** duidelijk beter. Harde platen, vingers en betere clips. Goed genoeg voor nu.
- **Voss:** de jas, het haar en de oogleden zijn een grote stap vooruit. Het gezicht blijft het zwakste punt van het hele project: lange onderkant, platte neus, een te lange hals met pezen en een mannequin-achtige huid. Procedurele gezichten zijn moeilijk. Opties voor later: een gestileerder ontwerp (grotere ogen, kortere neus en kin, minder realisme), of het gezicht in Blender opbouwen uit een basismesh met shape keys. Te bespreken met Remco. **Opgelost in ronde 19** (`build_voss2.py`): gestileerd gezicht met echte oog- en mondlussen, kortere neus en kin, natuurlijke nek, warme matte huid en shape keys; zie de Voss-rij hierboven en DESIGN_NOTES ronde 19.

### Omgeving ronde 17 (continenten 2-6)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Algemeen | Elke set heeft een eigen map en prefix (`Env/<Continent>/<Prefix>_*`), dezelfde kitnamen en dezelfde plaatsingsregels als ronde 8. De stoeprand is nu één doorlopende rand (punt uit ronde 8 opgelost). Bouwplek en gate-sokkel blijven die van ronde 8 |
| Kleurlagen | De FBX'en van ronde 17 hebben alleen nog de `Sway`-kleurlaag (flora) of geen. Ronde 8 (`Flora_*`) heeft er zes, met `Sway` als laatste; Unity leest alleen de eerste kleurset, dus daar komt `Sway` niet aan. Bij de import controleren |
| Ashen Reach | Mesa-kliffen hebben nog iets van tribunetreden (de richels variëren nu wel). Ground-sintels zijn sfeervol, maar druk op grote vlakken. Fulguriet leest als glazen pijpen |
| Northern Crown | Zuilenwand is van boven nog vrij druk. IJsoevers zijn bleke klonten. De weg is een bewust regelmatig legpatroon (onze weg), het enige rechte patroon in de set |
| Mirewood | Aarden wal blijft wat blokvormig (rechte snede), wortels helpen. Modderplassen renderen zwart in Blender; in Unity spiegelen ze de lucht |
| Southern Crown | Kruiende ijsrug: hoekige platen, van boven nog wat bladvormig. MixedBoulder: de verschroeide helft gloeit nauwelijks |
| The Core | Het zegel leest goed op spelgrootte. De glyphs zijn procedurele streepjes, geen echt schrift. Het hart (kristal in het midden) is klein. De vertakkende ader heeft een naadje waar de tak begint |

### Ronde 18 (P3-hybrides, gadgets, superwapens, drones)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Hybrides algemeen | Geen van de vijf staat al in de data; de gloedkleuren zijn een voorstel (Shatter ijsblauw, Arc Laser cyaan lens met violette spoelen, Corrosive Blast zuurgroen, Storm Flak violet, Thermal Shock vorstcyaan met lava-oranje). Arc Laser en Storm Flak delen het Tesla-violet (bewust, zelfde ouder) |
| Thermal Shock | Het script maakt dit een combo van de Cryo met een **gadget** (Lava Ball); de roadmap noemt dat een open vraag. Het model is gebouwd als toren (Cryo + thermietbal); als het een interactie wordt in plaats van een toren, vervalt het. Het oranje is het enige warme op onze kant (zoals bij de Lava Ball: het is het wapen) |
| Shatter | Het meest gelijkend op zijn ouder (Railgun) van boven; de Cryo-kant leest vooral via de bussen en de witte koelcilinder |
| Storm Flak | T3 is de lange zware dubbele loop (zoals Flak T3B), geen vierloop: het budget. De spoelen op de lopen vallen weg in LOD1 |
| LOD1 | Boven ~2.000: Thermal Shock T3 2.550 / T2 2.316, Arc Laser T3 2.454, Corrosive Blast T3 2.450. Rollback LOD1 is 70% van LOD0 (veel kleine chevrons) |
| Barricade | Op spelgrootte vooral een witte lijn over de weg met een lichtrand; de gevarenband zit op beide kanten. Getest zonder echte `BlockadeSpot`; de breedte (1,9) volgt de weg van 2 |
| Bombard | De buik is in de previews donker; luik en bommen zijn alleen van onder te zien, in het spel waarschijnlijk alleen als de VFX/projectielen vallen |
| Overcharged Crystal | Het kristal gloeit roze-lila (Crysium zelf, overladen). Dat zit dicht bij de kleur van The Shattered; dat is verhaaltechnisch juist, maar let op leesbaarheid naast aliens |
| Niet gedaan | **Aarde / S-69-CRYS-globe** en de **oogvorm** van het ogenveld (optioneel) zijn niet gemaakt: de globe wordt in Unity al procedureel geschilderd (`GlobePainter`); een Blender-versie zou dat dupliceren |

### Ronde 24 (vloeistoffen en mist)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Algemeen | Nieuwe set in `ArtSource/Exports/Liquids/` (`build_liquids.py`), nog niet in Unity. Vervangt `LiquidTextures.cs` zodra er één `Game/Liquid`-shader is met diepte (depth texture) of een afstand-tot-de-oever-textuur per poel; recept in `ArtSource/README.md` ronde 24 en `Liquids_report.json` |
| Water | Diepte, caustics in het ondiepe en schuim langs de oever lezen goed. De deining is op spelzoom subtiel; schuimlijnen volgen nog grotendeels de rechte celrand |
| Lava | Korstplaten breken en vormen opnieuw (twee lagen). In de open gesmolten plekken zijn soms zwakke ringen van het onderpatroon te zien |
| Moeras | Kroosachtige film met gloeiende stipjes verzamelt zich aan de oever. Belletjes-flipbook alleen op een sheet gecontroleerd |
| Drijfzand | Banden en flowmap horen bij elkaar (zand stroomt langs de banden). Van dichtbij lijken de banden wat op houtnerf |
| IJs | Melkige lagen, bellen, breukvlakken en rijp vanaf de oever. De kleur is vrij licht; per thema terugdraaien op donkere levels |
| Corruptie | Violet met kloppende aders; er zitten nog kleine gesloten lusjes in de fijne adertjes |
| Mist | Drie lagen met gaten; in het spel lagere alpha (0,16 / 0,1 / 0,06) dan in de previews. Wisp- en stoomkaarten nog niet in een scène gerenderd |

### Ronde 23 (levende spawnpunten)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Burrow Closed | Bijna onzichtbaar op spelzoom; wie zoekt ziet een vage donkere ster (de afgeronde plaatranden en hun haarlijn). De camo klopt alleen als het gat niet gedraaid wordt (yaw 0) en de grond in Unity dezelfde wereld-xy / 3,0 gebruikt; de grondshader verft er nog een tint en splat overheen, dus in Unity controleren. Veiligst: renderer uit tot Marked |
| Burrow Open | De opengeklapte platen tonen hun gloeiende zijkanten als vrij grafische roze randen. De flank van de muil is wat glad en glanzend (meer chitineplaten of korst zou beter zijn). De flank ademt mee met de kraag, dus de voet schuift 2-6 cm over de grond |
| Alle | De muil komt door de grond omhoog zonder gat in het terrein: wat nog onder z = 0 zit, wordt door het terrein bedekt. Bij doorzichtig water is de muil tijdens het stijgen even onder water te zien (bedoeld). Het wervelen van water/moeras, het borrelen van lava en het zandgestuif moeten uit VFX komen (`Dust`, `Glow`) |
| Quicksand | Zonder kraag leest het nu als heuvel met haken en een gloeiend oog, niet als ring; een echt zinkgat (trechter onder het oppervlak) kan niet zolang drijfzand ondoorzichtig is |
| IJs | Netjes maar wat kaal en "kroon-achtig"; de gloed is bijna wit (NestColor ijs) en leest alleen dankzij de donkerder, blauwere schotsen |
| Lava | De gesmolten kern zelf is alleen de gloeiende keel; een opwelling of fontein moet VFX zijn |
| Marked | Water, moeras en drijfzand: kleine gloeiende bolletjes in een ring; op spelzoom klein (3-4 px). Unity kan het lichtpoeltje van het nest (`GlowDecal.PlacePool`) laten meepulseren |


### Ronde 24b (ronde poelen: bank-sweeps)

| Onderdeel | Verbeterpunt |
| --- | --- |
| Algemeen | `<Prefix>_BankSweep_A/B/C` + `<Prefix>_BankAccent` voor alle zes stijlen (`build_bank_sweeps.py`); Unity moet de poelcontour gladmaken en de segmenten buigen (README ronde 24b). Nog niet geïmporteerd |
| Naden | Elke variant heeft een eigen gebakken atlas: op de gladde stijlen (Mirewood, Core) is de las tussen twee segmenten van dichtbij een zwakke lijn; accenten op de lassen verbergen het meeste |
| Bochten | Minimale straal 0,5 (preview haalt 0,38 op twee plekken, nog netjes). Smalle armen van één cel worden een ronde lob |
| Budget | ~2.400 tris per segment na het snijden, ~50k per poel van 20 segmenten: per poel samenvoegen, voor Low decimeren |

### Ronde 28 (continentbazen klaar voor hun mechaniek, 2026-11-05)

Nieuwe clips voor alle zes bazen (klimmen, poelen, eigen mechaniek), props en een verbeterronde van The Shattered.
Details per baas in `ArtSource/README.md` ronde 28, kritiek in `ArtSource/DESIGN_NOTES.md` ronde 28. Nog niets
geïmporteerd. Regels voor `build_all.py` staan in de README.

| Onderdeel | Verbeterpunt |
| --- | --- |
| Alle zes | Klim-clips zijn lang (Gravemaw ~11 s, Rotbloom ~8 s, rest 6-7 s): het hele lijf moet over de rand met geplante voeten. In Unity sneller afspelen mag (voeten blijven geplant, alleen de cadans verandert). Eén randhoogte per baas (1,2-2,0 m); andere hoogtes via de `IK_*`-markers |
| Alle zes | Duik/onder water/opduiken is voor de vier pootbazen dezelfde choreografie; Gravemaw (pad) en The Sealed One (handen) hebben een eigen. Onderwaterdieptes zijn groot (Cinder 3,3, Warden 4,7, Shattered 4,7, Sealed One 5,6 m): de vloeistofshader moet alles eronder verbergen |
| Gravemaw | Burrow_Travel laat alleen de kristalkam boven de grond (0,3 m): de gloeiende scheurlijn moet in Unity langs die kam lopen. Surface_Throw gooit de toren rechts van het gat; Unity draait de baas zo dat het doel rechts staat |
| Cinder Matriarch | Ei (`CinderEgg`) leest als gloeiende kern in drie korstklauwen; Hatch laat een FireLarva los op frame 32. Smash raakt een toren 3,1 m vooruit |
| Frost Warden | IJspantser is een tweede mesh in dezelfde FBX (`FrostWarden_IceArmour`); het bedekt het zegelteken tot Armour_Break (bewust). IJspegels (rage) via schaal in LateUpdate |
| Rotbloom | Overwoekering (`RotOvergrowth`) groeit via blend shape `Retracted` + kleurkanaal `Growth`; in Unity nog een shader-drempel nodig. Rooted_Idle zet de voeten 0,2 m in de grond (goed op veen, niet op harde grond) |
| The Shattered | Verbeterd (lichte ijsgraniet-platen, kristalkroon, kristalhakmessen, drie scherfspitsen) maar nog steeds een knokkelloper; volledige herontwerp zou rig en alle clips raken. De drie scherven zijn eenvoudig (2.000-2.400 tris); Split/Rejoin leunen op een flits-VFX. PhaseBreak1/2 zijn vervangen door Split/Rejoin |
| The Sealed One | Romp ploegt door de grond bij Walk/Climb/Rise_OntoBase: voor plausibiliteit een voor-decal en stof. Rise_OntoBase eindigt over de voorrand van de basis, niet er helemaal op. Puin-ring (`Rubble`) en zegelstenen verdwijnen zodra hij van het zegel weggaat |

### Ronde 4a Unity (Gravemaw speelbaar, 2026-11-05)

Wat Unity nu met code-plaatsing oplost en beter uit Blender kan komen:

| Onderdeel | Verbeterpunt |
| --- | --- |
| Boorinstallatie | Geen wrak-export: bij Gravemaws entree kantelt Unity de hele boortoren 78 graden en laat hem wegzakken; een `Cutscene_Drill`-wrak (geknakte toren, losse buizen, puin) zou beter lezen |
| Gravemaw_Hole | `Exports/Env/Gravemaw_Hole` bestaat maar wordt niet geïmporteerd (BlenderArtBuilder heeft geen pad voor statische env-props met eigen maps). Nu: een gloeipoel en een aardplof bij elk gat |
| Scheurlijn | Getekend met de geverfde gloei-kling (`MarkerKind.GlowBlade`) als oranje naad. Een eigen scheurstrook (donkere spleet, gloeiende kern, opgeworpen randbrokken, tegelbaar langs een pad) zou er als echte grondscheur uitzien |
| Wake | Onder water toont Unity een gloeipoel plus gegenereerde belletjes; een bel-/kolk-sheet per vloeistof (drijfzand: zand dat in een trechter zakt) ontbreekt |
| Geworpen toren | Geen eigen clip: de toren wordt als geheel opgetild, gekanteld en weer rechtgezet (TowerDisplacementView). Een "ontworteld"-pose (losgerukte voet, brokken) per toren zou het leesbaarder maken |

### Ronde 4b Unity (Cinder Matriarch en Frost Warden speelbaar, 2026-11-05)

Wat nu met bestaande assets of code-plaatsing is opgelost en beter uit Blender kan komen:

| Onderdeel | Verbeterpunt |
| --- | --- |
| Frost Warden clips | De oudere clips (Walk, Idle, Hit, Death, Enrage, FreezePulse, WalkEnraged) zetten de botten `Armour_1..8` op schaal 0; alleen de ronde-28-clips hebben ze op 1. Unity houdt de platen nu zelf op schaal 1 zolang het pantser er is (`BoneScaleView`). Beter: in Blender die keys uit de oude acties halen of op 1 zetten |
| Lavaspoor | Geen eigen strook: elk spoorstukje is een geverfde gloeipoel die in 6 s afkoelt (`LavaTrailView`). Een tegelbare lavaspoor-decal (gestold korstje met gloeiende scheuren, afkoelvarianten) zou echt als spoor lezen |
| Hittelijn | Getekend met de gloei-kling (`CrackStyle.Heat`), dun oranje. Een eigen "hittedraad"-sheet (trillende lucht, vonkjes) mag |
| Rijp/vorst voor de Warden | Een bleke gloeipoel met nevel 2,2 m voor hem uit. Een vorstdecal (ijskristallen die over de grond kruipen) ontbreekt |
| IJsblok-poort | Een kubus in het Blender-ijsmateriaal van de vloeistoffen (`Liquid_Ice`) rond de poort; leest als glas. Een eigen ijsblok-mesh (ruwe randen, ingesloten poortdelen, scheuren) en scherven-VFX bij het breken ontbreken |
| Warme gloed / blauwe waas op een toren | Gloeipoel + puntlicht + nevel (`TowerAuraView`); een eigen "verhit"-overlay op het torenmateriaal zou sterker zijn |
| Cinder Matriarch in de Codex | Op het weergavepodium zweeft ze boven de schijf (pivot onder het borststuk, lange poten); de podiumcamera moet haar lager zetten of de pivot klopt niet met de voeten |
| Cinder Matriarch poten | Haar poten spreiden ~4,6 m: op een vol veld (torens 2,5 cel uit elkaar) raken ze zichtbaar buurtorens, ook al blijft haar lijf er vrij van. Smallere loop-houding tijdens Hunt_Stride zou helpen |
| CinderEgg | Geïmporteerd en gebruikt (Idle, Hatch op frame 32, Burst als dood). Geen geluid van zichzelf: het borrelt met de lava-lus |

### Ronde 4c Unity (Rotbloom, the Shattered en the Sealed One speelbaar, 2026-11-05)

Alles uit de ronde-28-exports is geïmporteerd (Rotbloom met RotSporePod, RotOvergrowth en RotbloomRoots, the Shattered met ShardRunner/ShardFlyer/ShardCrawler, the Sealed One met Walk, Roar, Climb_*, pool-clips, Rise_OntoBase, Adapt, Release en de Phase1-4/Release-emissiesets). Wat nu met bestaande assets of code-plaatsing is opgelost en beter uit Blender kan komen:

| Onderdeel | Verbeterpunt |
| --- | --- |
| Verzegelde ruïnepoort (the Shattered) | Geen export: de entree is een plek in de data (`BossEntrance` Ground op de arena-kopie van Fault Line) met lichtscheuren, gerommel en een kristalflits; de reus verschijnt in de flits. Nodig: een `RuinGate`-prop (verzegelde stenen poort met Open-clip, eenmalig) die in de decor van een ruïne-level staat |
| Scherven klimmen | ShardRunner/ShardFlyer/ShardCrawler hebben geen Climb_*-clips; ze lopen hun route als gewone wezens. BOSSES.md wil dat de scherven de ruïnes beklimmen: Climb_Up/Loop/Down voor runner en crawler (zelfde conventies als de bazen) |
| Rotbloom-rotspoor | Een groene gloeipoel per spoorstukje (`LavaTrailView` in rotkleuren). Een eigen rot-decal (slijm, schimmelvlekken, sporen) ontbreekt |
| Groene waas op doelplekken | Gloeipoel + puntlicht + nevel (`TowerAuraView`). Een "sporenwolk"-sheet die op de plek neerdaalt zou sterker zijn |
| Overgroeide pad bij branden | De ranken krimpen terug (blend shape) met een emissie-flits; een vuur-VFX dat langs het `Growth`-attribuut omhoog brandt (de README-clipdrempel) ontbreekt, Unity gebruikt het Growth-attribuut nog niet |
| Wortels die vooruit kruipen | Rotbloom zet in zijn wortelfase elke 3 m een Stage1-wortelplukje voor zich neer (hergebruik van RotbloomRoots). Een kruipende wortel-clip of een wortel-strook die meegroeit zou beter lezen |
| Nieuwe weg van the Sealed One | Er verandert geen terrein: een brede violette lichtnaad (`CrackStyle.Seal`) loopt van de scheur in het midden naar de basis en blijft branden; de oude weg zakt niet. Voor echt "de kaart komt naar hem toe": een terrein-strook die oprijst (weg-tegels met een Rise-clip of een vervormbare grond-mesh) en een zak/verdonker-decal voor de oude weg |
| Voor van the Sealed One | Een donkere scheurlijn (`CrackStyle.Furrow`) langs zijn geplande route, die opengaat waar hij sleept. De README vraagt een voor-decal met stof: een tegelbare ploegvoor-strook ontbreekt |
| Verzegelde kristallen | Er staan geen zegelkristallen in de decor: een gloeipoel met puntlicht bij de scheur kleurt mee met het geleerde wapen. Nodig: een `SealCrystal`-prop (3-5 stuks rond de scheur) met een emissiekleur die Unity kan zetten |
| Sealed One op de basis | Rise_OntoBase eindigt over de voorrand van de basis; Unity houdt het laatste frame vast en laat hem van daar slaan. Een `OnBase_Loop`-clip (op het dak, slaan) ontbreekt; Adapt wordt op de basis zonder clip gespeeld |
| Leeggezogen poort | Violette waas op de poort (`TowerAuraView`) en een dunne violette draad bij het leegzuigen; het poortmodel zelf laat geen "stroom eruit" zien. Een gedoofde-loop-emissie op de poortkanonnen zou helpen |
| Gedempt-icoon op de bossbalk | Het torenicoon is de HUD-kaartfoto, grijs getint. Een eigen "gedempt"-overlay (doorgestreepte ring) uit de UI-set zou duidelijker zijn |

Opgelost in ronde 4c: "Cinder Matriarch in de Codex" (ronde 4b) lag niet aan de pivot maar aan de podiumcamera, die haar in de bind-pose en met de verre LOD mat; Unity meet nu de geposeerde nabije LOD.
