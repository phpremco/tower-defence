# Ontwerpdocument – Tower Defence (werktitel)

14 oktober 2026

## Visie

Een tower defence waarin je de eerste kolonie op een vijandige alienplaneet verdedigt, en per level sterker wordt door nieuwe wapens, een upgradebare hero en drones. Het spel komt eerst uit op Steam (pc, controller) en wordt vanaf dag één console-vriendelijk gebouwd.

Vier pijlers sturen alle keuzes:

- **Uitdaging:** elk level heeft vier moeilijkheden (easy, medium, hard, psycho). Je speelt levels opnieuw om genoeg geld te verdienen om door te kunnen.
- **Ontdekking:** na elk level krijg je een nieuw wapen, als gestolen alientech of als intel van de thuisplaneet.
- **Groei:** torens, hero, blokkades, basis en drones zijn upgradebaar.
- **Levend slagveld:** torens gaan stuk, je koopt en plaatst padobjecten zoals vertragingen en lavaballen, en elk level heeft een eigen pad en uiterlijk.

Het genre en het gevoel van Sentinel 4 zijn de inspiratie. Namen, kunst, levels en uitwerking zijn volledig eigen werk.

## Verhaal

De aarde raakt uitgeput en de mensheid komt naar deze planeet voor haar grondstoffen: metaal, water en energie. Wat niemand wist: de wereld zit vol leven, en dat leven vecht terug om zijn planeet te behouden. Jij bent commandant van de eerste landingsbasis.

- **Aanleiding:** het kolonieschip landt. De bewoners, een insectachtig ras dat de planeet als één levend netwerk beschouwt, zien de kolonisten als een besmetting en vallen de basis aan.
- **Verloop:** in elk level breid je uit naar een nieuw gebied, bouw je een buitenpost en vind je technologie, vaak in de resten van een oudere beschaving. Na het level ontvang je die technologie, of intel van de thuisplaneet waar nieuwe wapens zijn ontwikkeld, als nieuw wapen.
- **Escalatie:** de golven worden zwaarder naarmate je dieper in hun gebied doordringt, met een baswave in sommige levels.
- **Einde van de eerste wereld:** in het laatste level blijkt dat de aliens iets beschermen dat onder de planeet ligt. Dat is de eindbaas en de opening voor een volgende wereld.

De verhaaltekst blijft kort en staat in data:

- **Intro (eerste keer starten):** een korte cutscene over sterren, de naderende planeet, de oplichtende gebieden en de landing (`StoryDefinition`, te bewerken in `Data/Story/Story.asset`). Elke knop slaat over.
- **Briefing bij levelstart:** een paar regels per level (`LevelDefinition.Briefing`) op een paneel, het level wacht tot je op Deploy drukt.
- **De kaart** blijft rustig: een gebied kiezen zoomt in op de planeet en toont pas dan de opties, zodat daar later extra's bij kunnen.

## Stijl en sfeer

De gameplay is 2D (een vlak, vaste paden), maar het beeld oogt 3D: simpele 3D-vormen, een vaste orthografische camera van boven schuin, echt licht en veel gloed. Zo wordt "mooier maken" later een kwestie van assets wisselen, niet van herschrijven.

- **Camera:** orthografisch, vast, schuin van boven. Geen vrije camera.
- **Vormen:** torens, vijanden en omgeving beginnen als blokken, cilinders en bollen. Later vervangen we ze door low-poly modellen zonder de spellogica aan te passen.
- **Licht en gloed:** emissive materialen (lava, lasers, energie), bloom, zachte schaduwen en rook of mist over het veld geven diepte.
- **Kleurcodes:** jouw torens en drones in koele, technische kleuren (blauw, wit, geel). De aliens in warme, organische kleuren (rood, groen, oranje).
- **Per level een eigen palet en gevaar:** bijvoorbeeld landingszone, bioluminescent bos, grotten met lava, ruïne en het nest.
- **Interface:** wave en score bovenin, snelheid en pauze rechtsboven, torenbalk onderin met prijzen, vaardigheden links en rechts, geld linksonder. Sterren op torens tonen het upgradeniveau.
- **Per platform instelbaar:** bloom, mist en deeltjes hebben een zware en een lichte stand (pc en Switch).
- **Geluid:** vier volumegroepen (master, muziek, effecten, omgeving) in de opties en het pauzemenu. De muziek is een lichte rockloop die bij een baaswave overgaat in een zwaardere, snellere loop. Geluidsspam wordt beperkt met een stemmenbudget per soort geluid, bundelen van salvo's, ducking onder belangrijke geluiden, een loop voor continu-wapens en demping bij uitgezoomde camera.

## Gameplay-loop en moeilijkheden

Je verdient geld en punten in een level, besteedt die aan upgrades, en speelt levels opnieuw op hogere moeilijkheid om genoeg te verzamelen voor het volgende level.

1. Kies een level op de kaart en een moeilijkheid.
2. Bouw torens langs het vaste pad, zet de hero bij een blokkade en stuur drones.
3. Versla de golven (en de baswave, als die er is). Torens en blokkades kunnen stuk gaan.
4. Ontvang geld, onderzoekspunten en power currency, afhankelijk van moeilijkheid en prestatie. De eerste keer dat je een level verslaat, krijg je ook een nieuw wapen.
5. Upgrade in het basisscherm: torens, hero, blokkades, basis en drones.
6. Ga door naar het volgende level, of speel een level opnieuw voor meer beloning.

| Moeilijkheid | Doel | Beloning |
| --- | --- | --- |
| Easy | Leren spelen, verhaal volgen | Laag |
| Medium | Standaard speelervaring | Gemiddeld |
| Hard | Slim bouwen en combineren nodig | Hoog |
| Psycho | Volledig uitgeupgrade, geen foutjes | Zeer hoog, plus unieke beloning |

Hogere moeilijkheid geeft meer vijanden, meer HP, snellere golven en extra baswaves. De exacte getallen leggen we later vast in data (zie Levels makkelijk bouwen), zodat balanceren geen code kost.

## Torens

Acht standaardtorens. Je begint met het pulsgeweer; de andere wapens krijg je als beloning na het verslaan van een level, als gestolen alientech of als intel van de thuisplaneet waar nieuwe wapens zijn ontwikkeld (zie De vijf levels). Elke toren schiet op land, lucht of beide. De waarden zijn startpunten om later te tweaken.

| Toren | Rol | Schiet op | Sterk tegen | Zwak tegen |
| --- | --- | --- | --- | --- |
| Pulsgeweer | Snel, weinig schade, goedkoop | Land en lucht | Scouts | Pantser |
| Kanon | Langzaam, veel schade, splash | Land | Grunts, groepen | Vliegers (kan ze niet raken) |
| Laser | Continue straal | Land | Rijen vijanden | Pantser |
| Cryo | Vertraagt vijanden | Land en lucht | Alles, als ondersteuning | Bazen (deels immuun) |
| Raket | Groot bereik, schade in een gebied | Lucht | Vliegers, luchtzwermen | Grondvijanden (kan ze niet raken) |
| Tesla | Bliksem springt over meerdere vijanden | Land | Zwermen, groepen | Brutes |
| Railgun | Enorm veel schade op één doel | Land en lucht | Bazen, brutes | Zwermen |
| Support | Meer bereik of vuursnelheid voor torens in de buurt | Geen | Versterkt anderen | Doet zelf geen schade |

**Upgrades.** Elke toren heeft drie niveaus (sterren). Op niveau 3 kies je een van twee paden, bijvoorbeeld meer schade of meer snelheid. Je betaalt met geld uit het level, plus onderzoekspunten voor permanente verbeteringen tussen levels.

**Combineren.** Twee naastgelegen torens voeg je samen tot een hybride volgens een vaste receptenlijst, bijvoorbeeld Laser + Cryo = Cryostraal, Tesla + Support = Stroomnet, Kanon + Raket = Artillerie. Vaste recepten zijn makkelijker te balanceren dan vrije combinaties.

**Kapot gaan.** Vijanden vallen ook torens aan. Een beschadigde toren kun je laten repareren (door jou met geld, of door drones). Een volledig kapotte toren blijft als wrak staan: klik erop voor geld uit de scrap, daarna bouw je een nieuwe op die plek.

## Hero

De hero is de commandant en verdedigt de blokkades: hij staat bij een blokkade, houdt vijanden tegen en beschermt zo de doorgang naar de basis.

- **Taak:** een blokkade verdedigen. Je plaatst of verplaatst de hero naar een blokkade; hij vecht vijanden af die er aankomen.
- **Vaardigheden:** een paar actieve vaardigheden met cooldown, bijvoorbeeld een schokgolf, een schild voor de blokkade en een noodreparatie.
- **Upgrades:** permanent upgradebaar met punten tussen levels (gezondheid, schade, vaardigheden). De hero groeit mee met het verhaal.
- **Kwetsbaar:** gaat de hero neer, dan is hij tijdelijk uitgeschakeld en de blokkade staat open.

De exacte besturing van de hero (zelf lopen of aanwijzen) staat onder Open punten.

## Drones

Drones haal je uit de basis met een aparte, speciale valuta (power currency). Je geeft ze een taak en ze voeren die zelfstandig uit.

| Drone | Taak |
| --- | --- |
| Reparatiedrone | Repareert beschadigde torens, blokkades en de basis |
| Verzameldrone | Verzamelt geld en scrap in het level |
| Aanvalsdrone | Jaagt op de vijand die het verst is en beschiet hem vanuit de lucht |

- **Zichtbaar:** drones vliegen over het veld, met een lichtbundel zodat je ziet waar ze werken.
- **Aansturen:** je kiest een drone en geeft hem een taak of een doel. Daarna werkt hij zelf.
- **Upgrades:** snelheid, capaciteit en uithoudingsvermogen zijn upgradebaar.
- **Kwetsbaar:** drones zijn kwetsbaar voor vliegende vijanden en kunnen neergehaald worden.
- **Valuta:** power currency krijg je vooral op hogere moeilijkheden en via baswaves, zodat die levels aantrekkelijk blijven.

## Blokkades en basis

Blokkades houden vijanden tegen op het pad, de basis is wat je uiteindelijk verdedigt. Beide zijn upgradebaar met punten en kunnen beschadigd raken.

- **Blokkade:** houdt vijanden op en geeft torens tijd om te schieten. De hero verdedigt haar. Upgrades: meer HP, pantser en een reparatiesnelheid.
- **Basis:** heeft HP en gaat het spel verloren als die op is. Upgrades: HP, een eigen verdediging en een noodvaardigheid.
- **Reparatie:** door jou (geld), drones of de hero.
- **Volledig vernield:** een blokkade is dan weg tot je een nieuwe plaatst.

## Padobjecten

Padobjecten koop je in de balk en plaats je zelf, net als torens. Het pad ligt vast, maar met objecten op en langs het pad pas je je verdediging aan, per level anders. Ze zijn data-gedreven, zodat we ze makkelijk kunnen toevoegen.

| Object | Effect | Plaats | Werkt op |
| --- | --- | --- | --- |
| Lavabal | Rolt over het pad en beschadigt vijanden onderweg | Op het pad | Land |
| Grounddrone | Rijdt over het pad en valt vijanden aan of houdt ze op | Op het pad | Land |
| Vertragingsveld | Vertraagt alles dat erdoorheen loopt | Op of langs het pad | Land |
| Mijn | Eenmalige schade in een gebied bij contact | Op het pad | Land |
| Explosieve ton | Schade in een gebied als hij geraakt wordt | Langs het pad | Land |
| Luchtafweernet | Vertraagt vliegende vijanden in een gebied | Langs het pad | Lucht |

- **Kopen en plaatsen:** met geld uit het level, op een vrije plek op of langs het pad, net als torens.
- **Per level beschikbaar:** elk level bepaalt welke padobjecten je kunt kopen, zodat elk level anders speelt.
- **Kwetsbaar:** vijanden kunnen padobjecten beschadigen of vernietigen. Je repareert of vervangt ze zoals torens.
- **Upgrades en beperkingen:** nog te bepalen (zie Open punten).

## Vijanden en baswaves

Er zijn land- en luchtvijanden. Landvijanden volgen het pad, luchtvijanden vliegen een eigen route en kunnen alleen geraakt worden door torens die op lucht schieten. Kleur en vorm laten direct zien wat een vijand doet. De eigenschappen zijn startpunten om later te tweaken.

| Vijand | Type | Eigenschap |
| --- | --- | --- |
| Scout | Land | Snel, weinig HP |
| Grunt | Land | Gemiddeld in alles |
| Brute | Land | Traag, veel HP |
| Zwerm | Land | Veel kleine vijanden tegelijk |
| Healer | Land | Herstelt vijanden in de buurt |
| Pantser | Land | Immuun of bestand tegen bepaalde schade |
| Sapper | Land | Valt torens, blokkades en padobjecten aan in plaats van door te lopen |
| Vlieger | Lucht | Snel, weinig HP, valt torens en drones aan en duikt onderweg op een poort |
| Roller | Land | Gordeldierachtige die lopen en rollen afwisselt; rollend is hij een stuk sneller (vanaf level 2) |
| Drowner | Land | Klimt uit water en loopt naar de dichtstbijzijnde weg |
| Burrower | Land | Graaft zich uit drijfzand omhoog en loopt naar de dichtstbijzijnde weg |
| Cinder | Land | Magmakever uit de lava, laat vonken achter en bijt torens |
| Luchtzwerm | Lucht | Veel kleine vliegers tegelijk |
| Bommenwerper | Lucht | Traag, veel HP, bombardeert torens en blokkades |

**Wavestructuur.** Elk level heeft een eigen aantal waves (nu 20 tot 40, per level in te stellen) uit een eigen script in de generator: welke soorten meedoen en vanaf welke wave, een ritme van wavetypes (gewoon, stormloop, zwaar, hinderlaag uit de poelen), welke waves luchtaanvallen zijn, en per level een eigen reeks baaswaves (bijvoorbeeld 10 en 20, of 10, 20, 30 en 40). Een dreigingsbudget groeit per wave, en later in het level krijgen zwaardere soorten meer gewicht. De moeilijkheid schaalt aantallen, HP en tempo, en voegt luchtwaves en escortes toe: de landingszone heeft op easy geen luchteenheden, op psycho drie luchtwaves. Vijanden lopen traag, zodat je tussen waves kunt bouwen en upgraden.

**Baswaves.** Sommige waves eindigen met een baas: een grote vijand met veel HP, een eigen vaardigheid en een uniek uiterlijk. Elke baas dwingt een andere aanpak af (bijvoorbeeld een pantserbaas die railguns vraagt, of een zwermbaas die splashschade vraagt). Bazen geven extra geld en power currency. Het laatste level heeft de eindbaas van de eerste wereld.

**Baaskrachten en opkomst.** Elke baas draagt een of meer krachten uit data (`BossAbilityDefinition`): een **puls** die alle torens, poorten en drones in een straal beschadigt, een langzaam zwaaiende **laser** die de torens raakt die hij kruist, of het **broeden** van kleine wezens die de weg van de baas vervolgen. Bazen komen aan het eind van een level of halverwege een wave. Per level staat in data waar ze opkomen: door de poort, uit een poel (water, zand of lava) of door de grond heen, zodat elk level anders aanvoelt. Een baaswave begint met een alarm, een aardbevingsgerommel en zwaardere muziek.

**Poelen als spawnpunt.** Water, drijfzand en lava zijn niet alleen decor: een wavegroep kan er wezens uit laten opkomen. Die lopen naar de dichtstbijzijnde weg en volgen die naar de basis, dus ze kunnen bij poort twee of drie uitkomen in plaats van bij de eerste.

## De vijf levels van de eerste wereld

Vijf levels, elk met een eigen pad, uiterlijk en padobjecten. Nieuwe wapens krijg je als beloning na het verslaan van een level (de eerste keer), als gestolen alientech of als intel van de thuisplaneet. Daarom komen niet alle wapens in de eerste wereld: je begint met het pulsgeweer, en de railgun en support volgen pas in een volgende wereld. Dit is een eerste voorstel om te tweaken.

| # | Level | Palet en sfeer | Pad | Padobjecten te koop | Beloning na het level | Baas |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Landingszone | Grijs en blauw, open vlakte | Eén lange bocht | Vertragingsveld | Kanon (intel van de thuisplaneet) | Nee |
| 2 | Gloeiend woud | Turquoise en paars, bioluminescent | Kronkelend, twee takken die samenkomen | Vertragingsveld, mijn | Cryo (gestolen alientech) | Mini-baas |
| 3 | Lavagrotten | Oranje en rood, donker | Zigzag door smalle gangen | Lavabal, explosieve ton | Laser (intel van de thuisplaneet) | Nee |
| 4 | Oude ruïne | Goud en groen, oud metaal | Lus met een kruising | Grounddrone, luchtafweernet | Raket (gestolen ruïnetech) | Baas |
| 5 | Het nest | Donkerrood en zwart, organisch | Twee ingangen naar één basis | Alle padobjecten | Tesla (gestolen alientech) en de eerste combinatierecepten | Eindbaas |

Elk level krijgt vier moeilijkheden (easy, medium, hard, psycho). Hogere moeilijkheid verandert het aantal en de sterkte van golven, niet het pad. Elk level heeft zowel land- als luchtgolven, met meer luchtvijanden vanaf level 2.

## Economie en progressie

Drie valuta's houden de loop in beweging; de bedragen zijn nog niet vastgelegd.

| Valuta | Verdien je met | Besteed je aan | Geldt |
| --- | --- | --- | --- |
| Geld | Vijanden verslaan, scrap, verzameldrones | Torens bouwen en upgraden, reparaties | Alleen in het level |
| Onderzoekspunten | Levels afronden, hogere moeilijkheid | Permanente upgrades voor torens, hero, blokkades en basis | Tussen levels |
| Power currency | Baswaves, hoge moeilijkheden | Drones kopen en upgraden | Tussen levels |

- **Hogere moeilijkheid betaalt meer:** dat dwingt je levels opnieuw te spelen om verder te komen.
- **Scrap:** een kapotte toren geeft geld als je op het wrak klikt.
- **Balans:** alle getallen staan in data, zodat we ze zonder code kunnen aanpassen.

## Controls

Controller eerst, muis en toetsenbord erbij. Elke handeling moet met alleen een gamepad kunnen.

| Handeling | Controller | Muis en toetsenbord |
| --- | --- | --- |
| Cursor bewegen over het veld | Linker stick of d-pad | Muis |
| Toren plaatsen | Selecteer in de torenbalk, bevestig met A | Klik op de torenbalk, klik op de bouwplek |
| Toren upgraden of repareren | Cursor erop, kies actie | Klik op de toren |
| Scrap ophalen | Cursor op wrak, bevestig | Klik op het wrak |
| Hero verplaatsen | Selecteer hero, kies blokkade | Klik op hero, klik op blokkade |
| Drone een taak geven | Selecteer drone, kies doel | Klik op drone, klik op doel |
| Snelheid en pauze | Aparte knoppen | Knoppen rechtsboven en spatiebalk |

Geen menu mag alleen met hover of klikken te bedienen zijn, en op consoles is er geen afsluitknop.

## Schermen en flow

Eén complete ronde loopt van het hoofdmenu tot het upgradescherm en terug naar de kaart, en elk scherm is met een controller te bedienen.

0. Intro (alleen de eerste keer)
1. Hoofdmenu
2. Gebiedskiezer (de planeet)
3. Level
4. Resultaatscherm
5. Upgradescherm
6. Terug naar de kaart

| Scherm | Toont | Acties |
| --- | --- | --- |
| Hoofdmenu | Titel, Start en Opties (Afsluiten alleen op pc) voor een levend diorama: het nest met zijn bewoners, lava, zand en water, met eigen muziek | Start, opties |
| Gebiedskiezer | Een planeet met per level een gekleurd gebied en een baken (vergrendeld, open, voltooid). Een baken kiezen zoomt in en toont het regiopaneel: briefing, de vier moeilijkheden met vinkjes en sterren | Gebied en moeilijkheid kiezen, naar het upgradescherm, B zoomt uit |
| Briefing | Voor de eerste wave: regio, levelnaam en het stukje verhaal; de tijd staat stil | Deploy |
| Level (HUD) | Wave en score, snelheid en pauze, torenbalk met prijzen en padobjecten, hero-vaardigheden, geld | Bouwen, upgraden, repareren, scrap ophalen, pauzeren |
| Pauzemenu | Hervatten, opnieuw beginnen, terug naar de kaart | Kiezen |
| Resultaatscherm | Beloning: geld, onderzoekspunten, power currency en het nieuwe wapen met zijn bron (gestolen tech of intel van de thuisplaneet) | Doorgaan, opnieuw spelen, naar het upgradescherm |
| Upgradescherm (basis) | Permanente upgrades met hun kosten in onderzoekspunten of power currency | Kopen, terug naar de kaart |

In de eerste versie bevat het upgradescherm alleen torens, blokkades en basis. Hero en drones volgen met hun eigen mijlpaal.

## Technische richtlijnen

Unity met URP, gebouwd voor Steam en later consoles. Deze regels gelden vanaf de eerste regel code.

- **Logica en uiterlijk gescheiden:** een toren, vijand of drone heeft een logica-deel (schade, bereik, upgrades) en een apart uiterlijk-deel (model, effecten) dat je kunt vervangen.
- **Data-gedreven:** torens, vijanden, golven, upgrades en recepten staan in ScriptableObjects, niet in code.
- **Input System:** gamepad eerst, alles bedienbaar zonder muis.
- **Platform achter interfaces:** achievements, opslag en cloud saves via eigen lagen (bijvoorbeeld `ISaveService`, `IAchievements`), zodat per platform alleen de implementatie wisselt.
- **Lichte graphics:** URP met simpele instellingen, gecomprimeerde textures, bloom en mist instelbaar.
- **Object pooling:** voor vijanden, kogels en effecten.
- **IL2CPP-proof:** geen zware reflection of dynamisch gegenereerde code.
- **UI-schaling:** 720p handheld en 1080p docked, met veilig randgebied.
- **Pauzeren:** het spel pauzeert en hervat netjes bij onderbrekingen.
- **Switch:** buildmogelijkheden komen pas na goedkeuring door Nintendo als ontwikkelaar; controleer vooraf de actuele voorwaarden van Unity en Nintendo.

## Levels makkelijk bouwen

Een level is data, geen handwerk: Claude Code en jij kunnen een nieuw level toevoegen door een bestand te maken, zonder code te wijzigen.

- **Grid en tilemap:** het veld is een raster. Elke cel is pad, bouwplek, blokkade of decoratie.
- **Pad als waypoints:** een lijst punten die vijanden volgen. Een nieuw pad is een nieuwe lijst.
- **Level-bestand (ScriptableObject):** bevat het raster, de waypoints voor land en lucht, de bouwplekken, de blokkades, de beschikbare padobjecten, het palet, de beschikbare torens en de beloning na het level.
- **Golven in data:** per moeilijkheid een lijst golven met vijandtype, aantal, tempo en baswave.
- **Hulpmiddel in de editor:** een eenvoudige tool die het pad en de bouwplekken toont en op fouten controleert (bijvoorbeeld een pad dat nergens heen leidt).
- **Testen:** een snelle testmodus met oneindig geld om een level of een wave te proberen.

## Open punten en volgende stap

Volgende stap: de mappenstructuur van het Unity-project en een `CLAUDE.md` met de technische regels, zodat Claude Code zich er automatisch aan houdt.

- [ ] Naam van het spel (werktitel nu: Tower Defence)
- [ ] Hero: loopt hij zelf rond en vecht hij mee, of plaats je hem bij een blokkade en gebruik je vaardigheden?
- [x] Drones: één vaste drone per taak (reparatie, oogst, aanval), ze werken zelfstandig en worden in het level met geld gekocht; geen gratis drones bij de start, alleen hun snelheid is een permanente upgrade. Mijnknopen raken op (vaste voorraad per knoop), zodat oogstdrones een begrensd bedrag opleveren (besloten 2026-10-29/30).
- [ ] Kapotte torens: werken ze met minder kracht tot ze gerepareerd zijn, of helemaal niet meer?
- [ ] Padobjecten: hebben ze beperkt gebruik of een cooldown, en zijn ze upgradebaar?
- [ ] Welk wapen komt uit welk level, en is de bron gestolen tech of intel van de thuisplaneet?
- [ ] Welke wapens volgen in de tweede wereld (nu: railgun en support)?
- [ ] Bedragen en balans van de drie valuta's
- [ ] Namen en uiterlijk van torens, vijanden en bazen
- [ ] Verhaaltekst per level, inclusief de berichten van de thuisplaneet
- [ ] Of we eerst een snel browser-prototype maken om de gameplay te testen
