# Story-uitbreiding: plan

Versie 2, 2026-11-03. Bron: `Docs/STORY.md` (Remco). Dit plan is getoetst door twee critici (architectuur en codebase, ontwerp en omvang). Elke fase wacht op akkoord van Remco voordat er gebouwd wordt.

## Principes

- **Het script is de bron voor verhaal en content.** `GAME_DESIGN.md` wordt per fase bijgewerkt waar het script iets anders zegt, en alleen na akkoord.
- **Uitbreiden, niet vervangen.** Verhaal bouwt voort op `StoryDefinition`, `IntroScreen` en het `Intro`-scene. Continenten gebruiken `CampaignCatalog`, `MapRegion` en `PlanetGlobe`. Gadgets zijn `PathObjectDefinition`s. Combo's lopen via `ISupportSource` of `CombineRecipe`.
- **Asset-id's veranderen nooit.** Hernoemen gaat alleen via `DisplayName`, zoals Gun Turret, Flak, Mercer en Crazy. Enums krijgen alleen waarden achteraan. Saves blijven geldig.
- **Een systeem komt pas in de fase waarvan de content het nodig heeft.** We bouwen geen systemen vooruit voor continent 2 t/m 6.
- **Look vanaf dag één.**
  - Alles wat je ziet krijgt een view-component plus een skin of definitie in data. Kale vormen bestaan niet.
  - Animaties lopen via eigen rigs (zoals `SwordRig` en `BlasterRig`), zodat er later een mooi model in kan.
  - Portretten zijn gerenderde busts van de 3D-modellen. Een getekend portret vervangt ze later alleen via data.
- **Trillingen** gebruiken de bestaande `CameraShaker` met een nieuw quake-event, plus een lokale grondgolf en een dreun. Er komt geen schermbrede puls.

## Fase F0: technisch fundament (voor alles)

De critici vonden vier dingen die anders bij 120 levels of bij verhaal-triggers misgaan.

1. **Waves per level in één asset.** Nu zijn er 992 losse wave-assets voor 7 levels. Bij 120 levels worden dat er ongeveer 17.000, ruwweg 110 MB.
   - De nieuwe vorm is één `LevelWaves`-asset per level, met de waves als structs.
   - De generator, de BalanceLab-reader en de tests gaan in dezelfde stap mee.
   - De balans verandert niet: dezelfde getallen, andere opslag.
2. **Saveversie en migratie.** `ProgressData` krijgt `Version`, `SeenStoryIds` en een migratiestap in `ProgressService`.
3. **`TimeController.Hold` met tellers.** Nu sluit de briefing een dialoog die nog openstaat. Met tokens kunnen briefing, Codex en dialoog tegelijk openstaan.
4. **De generator in stappen.** `GenerateAll` wordt een lijst geregistreerde stappen, met één partial-bestand per feature (`ProjectGenerator.Story.cs`, `.Crystal.cs`). Zo kunnen agents in aparte bestanden werken zonder botsingen.
   - De PlayMode-runs over alle levels worden per continent gesplitst: een snelle smoke-test, en de volledige run op verzoek.

## Fase S1: verhaalmotor

- **Sprekers en dialogen.** `SpeakerDefinition` en `DialogueDefinition` staan in `Game.Levels` (Levels kan UI niet zien).
  - Een `SpeakerDefinition` bevat de naam, de portretbron en de kleur. Sprekers zijn ORA, Voss, Mercer, NTI-bericht en Pioneer-1-log.
  - Effecten staan als data per regel: glitch, `[REDACTED]`-balk en doorgestreept woord.
- **Triggers** in `LevelDefinition`: LevelStart, WaveStart(n), FirstSighting(soort), BossSpawn, BossDefeated, LevelWon, FirstUse(gadget) en BetweenLevels.
  - Ze lezen bestaande `LevelContext`-events.
  - Een trigger speelt één keer per save (`SeenStoryIds`), of altijd op Easy. Dat is instelbaar.
  - Er komt geen `[SerializeReference]`, zodat alles IL2CPP-veilig blijft.
- **`StoryDirector`** in UI luistert naar de triggers en houdt het spel stil met een Hold-token.
- **`DialogueView`** is een tekstvak in Pokémon-stijl:
  - portret links, de tekst typt zichzelf uit;
  - A, Enter of klik gaat verder; B of Esc slaat alles over;
  - er staat een zichtbare knop "Verder" en "Overslaan" bij;
  - het mag het pauzemenu niet openen.
- **Portretten** komen uit `ModelIconRenderer` met een buste-kader en rim-licht. Voor ORA (geen lichaam) en NTI (een logo) bouwen we een eigen hologram-icoon.
- **`LevelDefinition.Briefing`** blijft de standaard LevelStart-regel.
- **Testen.** Het systeem wordt getest op de bestaande 7 levels:
  - EditMode voor de triggerlogica;
  - PlayMode: level 1 met dialogen zonder errors.

## Fase S2: openingscutscene en proloog

- **`CutsceneDefinition`** is een lijst shots. Elke shot heeft een set-builder-id, een camerapad, een duur, een dialoog, een geluid en effecten.
  - Een `CutscenePlayer` speelt de shots af in het bestaande `Intro`-scene, alles vanuit data.
- **De proloog** is een grijze aarde met de ORA-tekstvakken.
- **De 10 shots uit het script:**
  - het moederschip;
  - de capsules als vallende sterren;
  - Mercer en het baken;
  - de timelapse van de basis;
  - de boor;
  - de drones en de kern die oplicht;
  - ogen onder de grond;
  - de radar;
  - de wezens uit de spleten;
  - de gate met "Defend the base".
- **Hergebruik.** De shots gebruiken de bestaande builders voor basis, drones, hero en wezens. Nieuw zijn het schip, de capsule, de boor en de aarde.
- **Overslaan** kan altijd. Het laatste shot gaat over in level 1.

## Fase S3: campagnestructuur

- **De globe.** `MapRegion` wordt 5 oppervlakte-continenten plus The Core.
  - Elk continent krijgt een verhaalfase, ingebouwde elementzones op breedtegraad en een kaarteffect: aders, later het zegel.
  - Continent 2 t/m 6 staan op "coming soon".
- **Levelsjabloon.** Per level komt een klein spec-record in een bestand per continent (`LevelSpecs.Crystal.cs`):
  - pad-sjabloon en seed;
  - elementen en soorten;
  - teaser;
  - unlock en hero-unlock;
  - bazen;
  - dialoog-id's.
  - De wave-solver blijft hetzelfde.
- **Unlock-timing.** Het wapen staat in `LevelReward.NewTower` van level N-1, zodat Voss het aan het begin van level N introduceert. Hero-unlocks gaan op dezelfde manier.
- **De onderzoekscurve per continent.** Nu groeit het onderzoek 18% per level, wat bij level 20 rond 350 uitkomt en daarna explodeert. De nieuwe regel wordt een curve per continent die het totaal begrensd houdt. `BalanceLedger`, `BALANCE.md` en de tests gaan mee.
- **Bestaande saves** krijgen een migratie naar de nieuwe volgorde.

## Fase S4: continent 1, eerste speelbare verhaalplak (levels 1 t/m 6)

Dit is het nieuwe moment "speel het en beslis of het leuk is". Levels 7 t/m 20 volgen pas na dat oordeel.

- **Torens:** Gun (PulseRifle), Cannon, Laser en Flak (Rocket omgebouwd).
- **Gadgets:** Land Mine (`Mine`) en Chrono Field (`SlowField`) in level 6.
- **Hero.** Mercer op een `HeroAbilityDefinition`-raamwerk met twee knopslots. Dat vervangt de vaste shockwave- en repair-velden en de vaste input. Quick Reload komt na level 5.
- **Teasers:** de vliegers laat in level 3 en de snelle vijand in level 5.
- **Dialogen** van level 1 t/m 6.
- **Element steen** (komt in continent 1 voor):
  - een nieuw `Element.Stone` met terrein;
  - een rotskloof-nest;
  - een steen-soort, zwaar gepantserd en traag, en zo nodig een steenvleermuis;
  - audio via een `CreatureVoice`-asset die tijdelijk naar bestaande clips wijst. Remco's eigen geluid vervangt die later alleen in data.

## Fase S5: continent 1, levels 7 t/m 20

- **Torens:** Tesla, Railgun en Acid Sprayer (een `ConditionDamage`-component die ook herstel tegengaat).
- **Gadgets:** Lava Ball (bestaat) en R.A.T. (de bestaande Roller/Patrol plus een drone-drop).
- **Nieuwe vijandtrekken:** gepantserd (een bestaande soort) en herstellend (een `Regrowth`-trek).
- **Hero:** Wide Slash en Field Repair.
- **Continentbaas Gravemaw** met een nieuwe baaskracht `Burrow`: hij duikt onder en komt verderop weer boven, met een richtingssignaal waar hij opduikt.
- **De rest van de dialogen** van continent 1.
- **Balans:** de ledger-check en de probe-bot draaien over alle 20 levels.

## Later (alleen grote lijn, per continent een eigen plan met critics)

- **Continent 2:** Cryo terug, Decoy Beacon (nieuwe `PathObjectKind`), Charged Slash en Dash, Cinder Matriarch, ORA-hapering en NTI-berichten.
- **Continent 3:** combo's, Frost Warden en de aders op de kaart.
  - Thermal Shock is een interactie tussen een toestand en een gadget, geen naast-elkaar-combo.
- **Continent 4:** Barricade (een tijdelijke `BlockadeSpot`), het Pioneer-1-wrak als decor, Directive 7 en Rotbloom.
- **Continent 5:** Spike Strip, Rollback Field (`Mover`-support), superwapens met een gevaarring voor het richten en The Shattered met 3 fases.
  - De vernielde basis in level 14 gebeurt in een tussenscène, niet midden in een run.
- **Continent 6:**
  - verdorven skins;
  - bondgenoot-Burrowers, wat nieuwe vriendelijke AI is en een groot item;
  - de herstelring;
  - The Sealed One;
  - de epiloog.
  - `SealState`: puur cosmetisch of met gevolgen.

## Werkwijze met agents

- **Curator (hoofdsessie)** bewaakt het script en het ontwerp, schrijft per fase de spec, verdeelt het werk, reviewt het en rapporteert aan Remco.
- **Per fase:**
  1. een spec;
  2. 2 critics (architectuur en ontwerp);
  3. akkoord van Remco;
  4. bouwen;
  5. tests en de balanscheck;
  6. een review-agent;
  7. een verslag met screenshots.
- **Unity-werk** (compileren, genereren, testen, play) doet steeds één agent tegelijk, altijd na `editor_status`.
- **Parallel kan:**
  - pure C#-modellen zoals de triggerlogica en balans (de engine-vrije `Game.Balance` draait met `dotnet`);
  - level-specs en dialoogdata in aparte bestanden;
  - critics en reviews.
  - Dat gebeurt in git-worktrees die na elkaar gemerged worden.
- **Botsingsgevoelige bestanden** pakt één agent tegelijk aan: `ProjectGenerator.cs`, `LevelContext`, `IInputProvider`, `ProgressData` en `LevelBootstrapper`.

## Besloten (2026-11-03)

- **Volgorde:** F0, S1, S2, S3, S4, S5.
- **De 7 bestaande levels** worden vervangen door nieuwe levels uit het levelsjabloon. De oude levels verdwijnen uit de campagne.
  - De code-ondersteuning blijft: ijs, moeras, nesten en poelen.
  - Saves worden gemigreerd.
- **Combo's werken als de huidige hybrides.** Je zet twee turrets naast elkaar en voegt ze samen tot een nieuwe toren.
  - De 5 combo's uit het script (Shatter, Arc Laser, Corrosive Blast, Storm Flak, Thermal Shock) worden `CombineRecipe`s in continent 3.
  - De huidige hybrides (Artillery, Cryo Beam, Storm Rifle) verdwijnen uit continent 1.
  - Thermal Shock (Cryo + Lava Ball) hoort een gadget niet als partner te hebben. Dat moet bij het plan voor continent 3 een aparte vraag zijn.
- **20 levels per continent, vast.**
- **Roster continent 1** volgt het script: Gun (PulseRifle), Cannon vanaf level 1, Laser, Flak (Rocket omgebouwd), Tesla, Railgun en Acid Sprayer.
  - Cryo komt in continent 2.
  - Support zit niet in de campagne, maar blijft in de code.

## Beslispunten voor Remco

| Wanneer | Vraag |
| --- | --- |
| Nu | Volgorde: F0, S1, S2, S3, S4 zoals hierboven? |
| Nu | Roster continent 1: Rocket wordt Flak, Support verdwijnt uit de campagne, Cryo naar continent 2, Cannon vanaf het begin |
| Nu | Wat gebeurt er met de 7 bestaande levels (opnieuw indelen of vervangen)? |
| Nu | Hybrides in continent 1: uit (combo's komen in continent 3 en vervangen ze), of houden? |
| Nu | Blijft het 20 levels per continent, of mag het minder voor continent 2 t/m 6? |
| Voor S4 | Element steen: vloeibaar terrein (rotskloof, niet bebouwbaar, spawnpunt) of vast (puin, bebouwbaar)? |
| Voor S4 | De wapenbron "gestolen tech / intel" op het resultaatscherm wordt "ontwikkeld door Voss / Crysium-tech"? |
| Voor S4 | Moeilijkheid "Psycho" hernoemen naar "Crazy"? (Beide kunnen bij een store-keuring opvallen.) |
| Continent 2 | Hero zelf besturen (Dash, Charged Slash en Overcharge vragen daarom) of op posts houden? |
| Continent 2 | Drones en power currency staan niet in het script: blijven ze zoals nu? |
| Later | Barrel en Air net houden? `SealState` cosmetisch of met gevolgen? Het eigen geluid voor steen |

Het script staat in `Docs/STORY.md`.
