# Plan: modellen en personages moderner maken

Status: voorstel (2026-11-03), nog niet gebouwd. Hoort bij mijlpaal 10 (look and feel) en loopt naast het
verhaalplan in `Docs/STORY_ROADMAP.md`. Beslissingen die bij Remco liggen staan onderaan.

## 1. Waarom het nu simpel oogt

Wat er nu is (feiten uit de code):

- Elk personage is een **hiërarchie van losse, starre vormen**: bollen, cilinders, capsules en `PropMeshBuilder`-stukken
  (`EnemyModelFactory` 1300 regels, `HeroView`, `TowerModelFactory`, `DroneModelFactory`). Er is geen enkele
  `SkinnedMeshRenderer` in het project; animatie is het draaien van losse onderdelen.
- De huid (`CreatureSkin`) is een **getegelde** textuur (chitine, schubben, slijm …) met normal map en gloedmasker. Mooi
  op zich, maar overal even groot en zonder relatie tot de vorm.
- Shader `Game/UnitLit` heeft rim light, normal map en emissie. Geen ambient occlusion per model, geen sporen van
  slijtage op randen, geen doorschijnendheid.

Daardoor zie je wat Remco beschrijft: *vormen met een skin*. Vier dingen verraden het:

| Kenmerk | Wat je ziet | Wat een modern low-poly/stylized model wél heeft |
| --- | --- | --- |
| Naden | Bollen die in cilinders steken, harde snijlijnen bij elk gewricht | Eén doorlopend oppervlak, vloeiende overgangen |
| Silhouet | Symmetrische, gladde primitieven | Taps toelopende poten, gebogen rugschilden, stekels, gekartelde randen |
| Schaduw | Elk onderdeel even licht, geen schaduw in holtes | Donkere oksels en naden (AO), lichte randen (edge wear) |
| Beweging | Starre segmenten die draaien | Buigende lijven, poten die de grond raken, na-wiegen van staarten en antennes |

## 2. Wat we niet veranderen

De architectuur is al goed voorbereid en blijft:

- Logica en uiterlijk zijn gescheiden (`EnemyView`, `TowerView`, `HeroView` reageren op events). Een nieuw model
  vervangt alleen de view-kant.
- Alles wordt gegenereerd vanuit code en data, zonder klikwerk (`ProjectGenerator`, idempotent).
- Kleurcodes (eigen kant koel, aliens warm), `VisualTheme`, `VfxDefinition` en de profielen High (PC) / Low (Switch).

## 3. Opties voor de modellen zelf

### A. Organische vormen uit code: SDF-modellen, gebakken tot één mesh (aanbevolen als basis)

Een soort beschrijf je als een lijstje **vormen die zacht in elkaar overlopen** (signed distance fields: ellipsoïden,
taps toelopende capsules, gebogen buizen, met "smooth union" en "smooth subtract"). De generator rekent dat om naar
één gesloten mesh (marching cubes of dual contouring), versimpelt die tot het polybudget en slaat hem op als asset.

- **Wat het oplevert:** geen naden meer, een kop die vloeiend in de borst overgaat, poten die smaller worden, holtes
  tussen platen. Precies het verschil tussen "vormen" en "een beest".
- **Data in plaats van code:** de vormen per soort komen in een nieuwe `CreatureShapeDefinition` (ScriptableObject).
  Remco kan een poot dikker of een kaak langer maken zonder code. Dat vervangt het grootste deel van de
  1300 regels in `EnemyModelFactory`.
- **Mee bakken bij het genereren:** ambient occlusion en "curvature" (holte/rand) in de vertexkleuren. De shader gebruikt
  die voor donkere naden en lichte, versleten randen. Gratis gedetailleerd, kost in-game niets.
- **Skinning uit dezelfde data:** elke SDF-vorm hoort bij een bot. Botgewichten volgen vanzelf uit de afstand tot de
  vormen, dus we krijgen zonder rigwerk een `SkinnedMeshRenderer` die buigt.
- **Geen nieuwe tools of pakketten.** Alles in C# in de editor, past bij "ik klik niets".
- **Grenzen:** heel fijn snijwerk (scherpe mechanische details) gaat minder goed. Daarvoor houden we bij torens en drones
  de huidige harde vormen, maar met afgeschuinde randen (zie C).

### B. Blender op de achtergrond, aangestuurd met Python-scripts

Blender (gratis) kan zonder venster draaien. Een script in de repo bouwt het model, past subdivision en sculpt-achtige
modifiers toe, bakt normal- en AO-maps, maakt een rig en exporteert FBX. Unity importeert dat automatisch.

- **Sterker** dan A voor details: echte normal-map-bakes van een hoog naar laag model, mooie UV's, gewichten schilderen.
- **Nadelen:** extra installatie op Remco's pc, een tweede toolketen om te onderhouden, en Blender-scripts zijn
  lastiger om met een agent te testen.
- **Rol in dit plan:** pas als A op een grens loopt (bijvoorbeeld bazen of de hero in close-up in cutscenes).

### C. Harde vormen netter maken (torens, drones, gebouwen, gate)

Deze objecten mogen technisch blijven ogen, ze hebben vooral nodig:

- **Afgeschuinde randen** (bevel) in `PropMeshBuilder`: één ringetje extra vertices per rand vangt licht en haalt het
  "Unity-kubus"-gevoel weg. Dit is de grootste winst per uur werk.
- **Panelen en inkepingen** in de mesh (inset/extrude) in plaats van alleen in de textuur.
- **Gebakken AO** per onderdeel, net als bij A.
- Eén consistente **trim-sheet-textuur** (strepen, bouten, waarschuwingsstrepen, lampjes) in plaats van tegelen.

### D. Kant-en-klare of gegenereerde modellen (alleen ter vergelijking)

- **CC0-packs** (zoals de Kenney Space Kit die er al is): snel, maar generiek. Ze passen niet bij onze eigen insecten.
- **Gekochte packs** (bijvoorbeeld Synty): verzorgd, maar herkenbaar als "dat pack" en niet ons ontwerp.
- **AI-3D-generatoren** (tekst of plaatje naar mesh): wisselende kwaliteit en stijl, rommelige topologie, en de
  licentie per dienst moet uitgezocht worden.

Advies: niet voor personages. Hooguit voor losse decorstukken, en alleen met Remco's akkoord.

## 4. Animatie

Zodra de modellen geskind zijn (A), kan de beweging ook een stap omhoog. Alles procedureel in code, geen
animatieclips nodig:

- **Poten die de grond raken** (simpele IK met twee botten): voeten blijven staan tot de volgende stap, het lijf
  deint mee. Zes poten in de juiste looppas (driepoot-gang) is wat insecten echt geloofwaardig maakt.
- **Ruggengraat die buigt** in bochten en bij versnellen of afremmen.
- **Na-wiegen** (spring bones) van antennes, staarten en vleugelpunten.
- **Treffers en dood:** kort indeuken bij een treffer, daarna een korte omvalanimatie in plaats van verdwijnen.
- **Hero:** echte loop- en richtanimatie met een bovenlijf dat los van de benen draait.

De bestaande "golvende vleugels" via de shader en het lage, platte loopje van de lopers blijven het uitgangspunt
(zie de look-feedback van 2026-11-02), alleen nu met buigende in plaats van starre delen.

## 5. Materialen en licht (onafhankelijk van A–D, kan meteen)

Uitbreiding van `Game/UnitLit`:

- **Triplanaire detailtextuur** in plaats van gewoon tegelen. De huid volgt dan de vorm en zie je niet meer
  herhalen of uitrekken.
- **AO en curvature uit de vertexkleuren** (zie A en C): donkere holtes en lichte randen.
- **Per huidtype een eigen trucje:** licht doorschijnende membranen en vleugels (achterlicht schijnt erdoor),
  iriserende chitine (kleur verschuift met de kijkhoek), nat glanzend slijm.
- **Contact-schaduw onder elke eenheid** (een zachte blob), zodat niets meer zweeft. Luchtvijanden houden hun eigen
  grondschaduw.
- SSAO staat al in de renderer en blijft. Op Low gaan triplanair en doorschijnendheid uit via het kwaliteitsprofiel.

## 6. Budget (Switch is de lat)

| Soort | Driehoeken Low | Driehoeken High | Botten |
| --- | --- | --- | --- |
| Gewone vijand | ≈ 1.500 | ≈ 4.000 | ≤ 16 |
| Baas | ≈ 6.000 | ≈ 15.000 | ≤ 32 |
| Hero | ≈ 4.000 | ≈ 10.000 | ≤ 24 |
| Toren | ≈ 2.000 | ≈ 5.000 | n.v.t. |

- Twee LOD-niveaus per model; de generator bakt beide.
- GPU-skinning en gedeelde materialen. Pooling blijft zoals nu.
- Een PlayMode-test met de drukste wave meet de frametijd op beide profielen en faalt boven het budget.

## 7. Fasering

Elke stap is los te testen en te stoppen. Na elke stap volgen screenshots, en bij personages een kort filmpje
(serie stills) van de loopanimatie.

| Stap | Wat | Waarom eerst |
| --- | --- | --- |
| G0 | Shader: triplanair, AO/curvature-kanaal, contact-schaduw, huidtrucs | Alles wordt direct mooier, ook de huidige modellen |
| G1 | SDF-mesher + bakker in de editor + `CreatureShapeDefinition`. Pilot: **Skitter** (mier) | Kleinste beest, veel op het scherm: laat de winst en de prestaties zien |
| G2 | Skinning + procedurele pootanimatie (IK, driepoot-gang) voor de Skitter | Pas met beweging zie je of het "leeft" |
| — | **Beslismoment met Remco:** pilot naast oud model op screenshots en in het spel | Doorgaan, bijsturen of stoppen |
| G3 | Alle vijanden en bazen omzetten, inclusief de Queen | Grootste visuele impact |
| G4 | Hero (en zijn cutscene-rig en portret) | Hero is in cutscenes van dichtbij te zien |
| G5 | Torens, drones, gate, basis: bevels, panelen, trim-sheet, AO | Mechanisch: andere techniek, minder risico |
| G6 | Low-profiel: LOD's, budgettest, finetunen | Switch-klaar |
| G7 | Pas als nodig: Blender-route (B) voor bazen/hero in close-up | Alleen als A tekortschiet |

Agents werken net als bij het verhaalplan: spec, dan critic, dan bouwen. Bouwen gebeurt achter elkaar, nooit tegelijk
met een andere bouwer in de editor.

## 8. Risico's

- **De mesher maakt "blobbige" vormen.** Tegenmaatregelen: scherpe vormen met kleine smooth-radius, en
  subtract-vormen voor holtes en groeven. Dit beoordelen we bij de pilot.
- **Skinning kost prestaties bij grote waves.** Tegenmaatregelen: LOD, minder botten op Low, en de budgettest uit §6.
- **De look verandert ongemerkt van karakter.** Daarom gaan we per soort pas verder na Remco's akkoord op de pilot. De
  silhouetten en kleurcodes uit het bestiarium blijven leidend.
- **Het loopt door het verhaalplan heen.** Bouwers mogen niet tegelijk in de editor. Voorstel: G0–G2 na S4 (level 1–6
  speelbaar), of eerder als Remco de look belangrijker vindt.

## 9. Beslissingen voor Remco

1. **Route:** akkoord met A (SDF uit code) als basis, en B (Blender) alleen als vervolg? Of meteen Blender?
2. **Blender installeren** (alleen nodig voor B): mag dat later op deze pc?
3. **Externe of AI-modellen:** helemaal niet, of alleen voor decor?
4. **Timing:** G0–G2 na S4, of nu tussen S3 en S4 in?
5. **Pilot:** Skitter als eerste soort goed, of liever de hero of een baas?
