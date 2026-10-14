# CLAUDE.md – Tower Defence (werktitel)

Dit bestand staat in de root van het Unity-project. Lees het volledig voordat je code schrijft, en lees ook `Docs/GAME_DESIGN.md` voor wat het spel moet worden.

## Communicatie

- Praat met mij in het Nederlands. Code, namen van classes, variabelen, bestanden en commentaar zijn in het Engels.
- Ik wil zelf niets in de Unity-editor doen. Jij voert alles uit: code, scenes, prefabs, instellingen, tests en builds. Alleen als iets echt niet zonder mij kan (bijvoorbeeld een licentie of inloggen), zeg je dat met één concrete stap.
- Ik ben wel een ervaren programmeur (PHP/Laravel, Vue, Node). Schrijf dus nette, onderhoudbare code en leg architectuurkeuzes in een paar zinnen uit.

## Doel van dit project

Eerst een kleine, speelbare **vertical slice** om te zien of het spel leuk is. Daarna uitbreiden. Dus: bouw wat nu nodig is, maar zo dat uitbreiden en tweaken makkelijk blijft. Bouw geen features die ik niet gevraagd heb.

Techniek: Unity (actuele LTS), URP, C#. Gameplay is 2D (raster en vaste paden) maar het beeld is simpele 3D met een vaste orthografische camera. Doel: Steam (pc, controller), later consoles (Switch). Alles moet daarom met een gamepad bedienbaar zijn.

## Spelconcept in het kort

De volledige uitwerking staat in `Docs/GAME_DESIGN.md`. Dit is de samenvatting zodat je het geheel altijd voor ogen hebt.

- **Verhaal:** de mensheid koloniseert een alienplaneet (denk aan Avatar). Een insectachtig alienras ziet ons als besmetting en valt onze basis aan. In elk level breid je uit en vind je nieuwe technologie. Het laatste level van de eerste wereld onthult een groter geheim onder de planeet.
- **Inspiratie:** het genre en het gevoel van Sentinel 4: Dark Star, maar volledig eigen uitwerking. Kopieer geen kunst, namen, levels of ontwerpen.
- **Eerste wereld:** 5 levels met elk een eigen pad, eigen uiterlijk en eigen padobjecten. Paden zijn vast.
- **Moeilijkheden:** easy, medium, hard en psycho per level. Hogere moeilijkheid betaalt meer, dus je speelt levels opnieuw om door te kunnen.
- **Torens:** acht standaardtorens met drie upgradeniveaus, twee paden vanaf niveau 3 en combineren via vaste recepten. Torens schieten op land, lucht of beide. Ze raken beschadigd en kunnen volledig stuk gaan (wrak met scrap, daarna opnieuw bouwen).
- **Wapens krijg je als beloning** na het verslaan van een level, als gestolen alientech of als intel van de thuisplaneet. Je begint met alleen het pulsgeweer. Niet alle wapens zitten in de eerste wereld.
- **Vijanden:** land en lucht, met baswaves in sommige levels.
- **Hero:** verdedigt de blokkades, permanent upgradebaar, met vaardigheden.
- **Blokkades en basis:** upgradebaar met punten en kunnen stuk.
- **Drones:** krijg je met een aparte power currency. Ze repareren, verzamelen geld en meer.
- **Padobjecten:** lavaballen, grounddrones, vertragingsvelden enzovoort. Je koopt en plaatst ze zelf, net als torens.

## Look en feel

De gameplay is 2D (raster en vaste paden), maar het beeld moet 3D ogen, zoals bij Sentinel 4. Het uiterlijk begint simpel en moet later mooier te maken zijn zonder de spellogica aan te raken.

- **Camera:** vast, orthografisch, schuin van boven. Geen vrije camera.
- **Vormen:** begin met simpele 3D-vormen (kubus, cilinder, bol) met echt licht en schaduw. Later vervang je ze door low-poly modellen. Daarom zit het uiterlijk altijd in een aparte view-component op de prefab.
- **Licht en gloed zijn het belangrijkste effect:** emissive materialen (lava, lasers, energie), bloom, zachte schaduwen, rook of mist over het veld voor diepte, en deeltjes (vonken, rook, stralen met een lichte kern en een zachte rand).
- **Torens:** lichte bovenkant en donkere onderkant, zodat ze uit het vlak lijken te steken. **Sterren** op de toren tonen het upgradeniveau.
- **Kleurcodes:** jouw torens, drones en hero in koele, technische kleuren (blauw, wit, geel). De aliens in warme, organische kleuren (rood, groen, oranje). Luchtvijanden zijn herkenbaar aan een schaduw op de grond.
- **Donkere achtergrond** met per level een eigen palet:

| Level | Palet en sfeer |
| --- | --- |
| Landingszone | Grijs en blauw, open vlakte |
| Gloeiend woud | Turquoise en paars, bioluminescent |
| Lavagrotten | Oranje en rood, donker, lavagloed |
| Oude ruïne | Goud en groen, oud metaal |
| Het nest | Donkerrood en zwart, organisch |

- **Per level een `VisualTheme` in data:** kleurenpalet, lichtkleur, mist, bloom en omgevingseffecten staan in een ScriptableObject die aan de `LevelDefinition` hangt. Effecten (schoten, explosies, rook) staan in een `VfxDefinition`. Zo verander ik de look zonder code.
- **Levend slagveld:** gezondheidsbalkjes boven vijanden, schadegetallen, drones met een lichtbundel, en beschadigde torens die zichtbaar rook en vonken laten zien.
- **UI-indeling:** wave en score bovenin, snelheid en pauze rechtsboven, torenbalk onderin met prijzen (en de padobjecten), hero-vaardigheden links en rechts, geld linksonder. Alles bedienbaar met een controller.
- **Per platform instelbaar:** het profiel "High (PC)" gebruikt alle effecten, het profiel "Low (Switch)" gebruikt minder bloom, mist en deeltjes.
- **Eerst gameplay, dan mooi:** tot mijlpaal 10 zijn placeholdervormen voldoende, maar de structuur (view-componenten, `VisualTheme`, `VfxDefinition`) wordt vanaf het begin goed gebouwd.

## Inspiratie en onderzoek

Mijn inspiratie is Sentinel 4: Dark Star. Je mag op het web zoeken naar hoe dat spel werkt (Steam-pagina, recensies, communityguides en een eventuele wiki) om de structuur en het gevoel te begrijpen. Dit is wat daar al uit naar voren kwam, in eigen woorden:

- Permanente upgrades zitten in een apart menu, los van de upgrades tijdens een level.
- Kapotte torens kun je slopen voor geld, waardoor je in het spel blijft als torens sneuvelen.
- Drones en eenheden worden gekocht met energie die bij de basis langzaam opbouwt. Dat geeft een ander tempo dan alleen geld.
- Je kiest vooraf een loadout uit de beschikbare torens.
- Er zijn commander-helden, eigen eenheden op het pad en steun vanuit de lucht.
- Er zijn vier moeilijkheden en naast de campagne een endless-modus.
- Aliens kunnen torens beschadigen, uitschakelen en vernietigen.
- Een wens uit de community: laat zien welke vijanden in de volgende wave komen.

Regels voor dit onderzoek:

- Noteer wat je leert in je eigen woorden in `Docs/INSPIRATION.md`, met bronlinks. Kopieer geen tekst, afbeeldingen, namen, getallen of leveluitwerkingen.
- Gebruik geen namen of termen van dat spel in code, UI of data. Alles krijgt een eigen naam.
- Zie het als inspiratie, niet als specificatie. `Docs/GAME_DESIGN.md` gaat voor. Heb je een idee uit je onderzoek dat nog niet in het ontwerp staat, stel het dan aan mij voor en bouw het niet zonder mijn akkoord.

## Werkwijze (altijd)

1. **Plan eerst.** Geef bij elke taak die meer is dan een kleine aanpassing eerst een kort plan (welke bestanden, welke classes) en wacht op mijn akkoord.
2. **Kleine stappen.** Eén feature tegelijk. Na elke stap compileert alles zonder errors of warnings en is het in Play Mode te testen.
3. **Vraag bij twijfel.** Vraag het mij bij ontwerpkeuzes die in `Docs/GAME_DESIGN.md` onder "Open punten" staan. Verzin die niet zelf.
4. **Niet buiten de opdracht.** Geen ongevraagde refactors, geen hernoemde of verplaatste bestanden, geen nieuwe packages zonder te vragen.
5. **Scenes en prefabs.** Bewerk `.unity`- en `.prefab`-bestanden niet met de hand en maak geen `.meta`-bestanden zelf. Zie "Zonder handmatig editorwerk" hieronder.
6. **Klaar betekent:** compileert, de tests zijn gedraaid en slagen, geen errors in de log, en een korte samenvatting van wat er veranderd is en hoe ik het kan uitproberen (alleen: open het project en druk op Play).
7. **Commit klein** met een duidelijke Engelse boodschap, alleen als ik daarom vraag.

## Projectopzet en git

Ik maak het Unity-project zelf aan in Unity Hub (3D URP-template, Unity 6 of nieuwer). Daarna regel jij de rest. Doe dit aan het begin, in deze volgorde, en meld wat je gedaan hebt:

1. **Controleer de projectlocatie.** De map mag niet in een gesynchroniseerde map staan (OneDrive, Dropbox, iCloud, Google Drive), want de `Library`-map is groot en raakt daar beschadigd of traag. Staat hij er toch in, waarschuw me dan en ga niet verder.
2. **Controleer `git status`.** Is er nog geen repo, ga dan door met de volgende stappen. Bestaat er al een repo, vraag dan wat ik wil.
3. **Maak eerst een Unity-`.gitignore`**, voordat je iets toevoegt. Die moet `Library`, `Temp`, `Obj`, `Logs`, `UserSettings` en `Builds` negeren en de `.meta`-bestanden **niet** negeren.
4. **Controleer de serialisatie-instellingen** in `ProjectSettings/EditorSettings.asset`: assets als tekst (Force Text) en zichtbare `.meta`-bestanden (Visible Meta Files). Staan ze anders, zet ze dan goed en vertel het me.
5. **Maak de repo en de eerste commit:** `git init -b main`, `git add .`, commit "Initial Unity project". Controleer eerst met `git status` dat er geen `Library`- of `Temp`-bestanden in de commit zitten.
6. **Zet `CLAUDE.md` en `Docs/GAME_DESIGN.md` in een aparte tweede commit.**
7. **Installeer het Pipeline-pakket** (`unity pipeline install`) en commit dat ook apart, zodat de geschiedenis laat zien wat het pakket toevoegde.
8. **GitHub:** vraag me om de URL van een lege **privé**-repo en push `main` daarheen. Als `gh` geïnstalleerd en ingelogd is, mag je de privé-repo zelf aanmaken. Maak een repo nooit openbaar.
9. **Git LFS** voor grote binaire assets is later pas aan de orde. Vraag me eerst.

Na deze eerste opzet commit en push je alleen als ik erom vraag.

## Zonder handmatig editorwerk

Ik klik niets in de editor. Daarom werk je code-first en voer je Unity zelf uit via de commandoregel.

- **Alles wordt in code opgebouwd uit data.** Er is één `Boot`-scene en één generieke levelscene. Bij het starten bouwt de code het level, de torens, de vijanden, de padobjecten en de UI op uit de `LevelDefinition` en de andere definitions. Placeholdervormen (kubus, cilinder, bol) en hun materialen maak je in code of met een generator, geen handwerk.
- **Assets genereer je met Editor-scripts**, aangeroepen via `Tools/Generate/...` en vanaf de commandoregel. Dat geldt voor ScriptableObject-assets, prefabs, materialen en de scene-configuratie. Een script moet opnieuw uitgevoerd kunnen worden zonder dubbele assets te maken.
- **Eerste keuze: de Unity CLI met het Pipeline-pakket** (`com.unity.pipeline`, Unity 6.0 of nieuwer). De editor staat open op de achtergrond en jij stuurt hem aan met het commando `unity`: play mode, tests, builds, scenes en assets. Draai `unity` en `unity command` om te zien wat er beschikbaar is en vertrouw op de actuele opdrachten in plaats van op oude documentatie, want de CLI is nog in bèta en de opdrachten veranderen tussen versies. Installeer de Unity-plugin voor Claude Code als die er nog niet is (`/plugin marketplace add Unity-Technologies/unity-agent-plugin`, daarna `/plugin install unity@unity-agent-plugin`) en vraag me eerst om akkoord voor installaties. De CLI en de editor draaien op deze computer, niet in een cloudsessie.
- **Terugvaloptie: Unity draaien in batchmode** als de CLI niet werkt (de editor mag dan niet geopend zijn op hetzelfde project):
  - script uitvoeren: `Unity -batchmode -quit -projectPath . -executeMethod <Namespace.Class.Method> -logFile -`
  - tests: `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile -` (en `PlayMode`)
  - build: via een `BuildScript` met `-executeMethod`.
  Het pad naar de Unity-installatie en de exacte opties leg je eenmalig vast in `Docs/COMMANDS.md`.
- **`.meta`-bestanden** laat je Unity zelf aanmaken: draai na het toevoegen van bestanden één keer een batchmode-import en neem de gegenereerde `.meta`-bestanden mee in de commit.
- **Controleren zonder te kijken.** Schrijf PlayMode-tests die een heel level versneld simuleren (waves, geld, win en verlies) en lees de log op errors en warnings. Ik doe zelf de visuele controle door te spelen. Vraag me daarvoor alleen om te spelen en feedback te geven, niet om iets in te stellen.
- **Schermafbeeldingen:** controleer of de CLI een screenshot van de Game-view of Play Mode kan maken. Zo ja, gebruik dat om je eigen werk visueel te controleren en vertel me wat je zag.

## Architectuurregels

### 1. Alles wat ik wil kunnen tweaken staat in data, niet in code
Gebruik ScriptableObjects voor:

- `TowerDefinition` (kosten, schade, bereik, vuursnelheid, `TargetLayers`, upgradepaden, prefab)
- `EnemyDefinition` (HP, snelheid, type land of lucht, weerstanden, beloning)
- `WaveDefinition` en `DifficultyProfile` (aantallen, tempo, multipliers per moeilijkheid)
- `LevelDefinition` (raster, waypoints voor land en lucht, bouwplekken, blokkades, beschikbare torens, beschikbare padobjecten, palet, beloning na het level)
- `UpgradeDefinition`, `CombineRecipe`, `PathObjectDefinition`, `DroneDefinition`, `UnlockTable` (welk wapen na welk level, met bron)

Verboden: getallen, prijzen, namen van torens of levelnummers hardcoded in logica. Wat wanneer beschikbaar is, bepaalt data (`LevelDefinition` en `UnlockTable`), nooit een `if (level == 3)`.

### 2. Logica en uiterlijk zijn gescheiden
- Spellogica (schade, targeting, upgrades, economie) leeft in gewone C#-classes of dunne MonoBehaviours zonder verwijzingen naar modellen, materialen of effecten.
- Uiterlijk (model, gloed, deeltjes, geluid) zit in aparte "view"-componenten op de prefab die op events reageren. Zo wissel ik later basisvormen voor mooie modellen zonder logica aan te raken.

### 3. Componenten in plaats van grote classes
Bouw torens, vijanden, drones en padobjecten uit kleine componenten (`Health`, `Targeting`, `Weapon`, `Mover`, `Repairable`, enzovoort) plus een definition. Geen klassenhiërarchieën met diepe overerving. Land en lucht regel je met een `[Flags] enum TargetLayers { Ground, Air }`.

### 4. Losse koppeling
- Communiceer via C#-events of ScriptableObject-eventkanalen, niet via directe verwijzingen tussen systemen.
- Geen singletons, behalve één centrale `Services`-registry voor de platformdiensten hieronder.
- UI kent de gameplay; gameplay kent de UI niet.

### 5. Platform achter interfaces
Alles wat per platform verschilt zit achter een interface met één implementatie per platform: `ISaveService`, `IAchievementService`, `IInputProvider`, `IPlatformSettings`. Steam-code komt nooit in de gameplay-code.

### 6. Input
Gebruik uitsluitend het nieuwe Input System, met actions. Geen `Input.GetKey` of `Input.mousePosition`. Elke handeling moet met alleen een gamepad kunnen. Geen UI die alleen met hover werkt en geen "Afsluiten"-knop in het hoofdmenu.

### 7. Performance en console-vriendelijk
- Object pooling voor vijanden, projectielen en effecten.
- Geen `Find`, `GetComponent` of LINQ in `Update`. Cache verwijzingen. Geen allocaties per frame.
- IL2CPP-proof: geen zware reflection of dynamisch gegenereerde code.
- Kwaliteitsprofielen "High (PC)" en "Low (Switch)": bloom, mist en deeltjes zijn per profiel instelbaar.
- UI schaalt van 720p (handheld) tot 1080p (docked), met een veilig randgebied.
- Het spel pauzeert en hervat netjes bij onderbrekingen.

### 8. Codestijl
- Eén class per bestand, bestandsnaam gelijk aan de classnaam. Namespace per feature (`Game.Towers`, `Game.Enemies`, enz.).
- `[SerializeField] private` in plaats van publieke velden. Gebruik `readonly` en `const` waar het kan.
- Korte methodes met één taak. Commentaar legt uit **waarom**, niet wat de code doet.
- Geen magische getallen, geen dode code, geen ongebruikte `using`s.
- Foutafhandeling: valideer ScriptableObjects met `OnValidate` zodat fouten in data meteen zichtbaar zijn.

### 9. Tests
Schrijf EditMode-tests voor pure logica: schadeberekening, upgradekosten, combineerrecepten, wavegeneratie, unlocks. Tests staan in `Tests/EditMode`.

## Mappenstructuur

```
Assets/
  _Project/
    Scripts/
      Core/            events, services, gedeelde hulpklassen, TargetLayers
      Combat/          Health, schade, projectielen, status-effecten
      Towers/          tower-logica, upgrades, combineren, kapot gaan en scrap
      Enemies/         vijandlogica, bewegen (land en lucht), bazen
      Hero/            hero en vaardigheden
      Drones/          drones en taken
      PathObjects/     lavabal, grounddrone, vertraging, enzovoort
      Levels/          raster, waypoints, waves, win en verlies
      Economy/         geld, onderzoekspunten, power currency, unlocks
      UI/              HUD, torenbalk, menu's, kaartscherm
      Platform/        interfaces + implementaties (Steam, later console)
      Editor/          level-tools, validatie, build-scripts
    Data/
      Towers/ Enemies/ Waves/ Levels/ Upgrades/ PathObjects/ Drones/ Difficulty/ Unlocks/
    Prefabs/           Towers/ Enemies/ PathObjects/ Drones/ Effects/ UI/
    Art/               Materials/ Models/ Textures/ VFX/   (eerst placeholders)
    Audio/
    Scenes/            Boot, MainMenu, WorldMap, Base, Level (één scene die elke LevelDefinition laadt)
    Settings/          URP-assets, kwaliteitsprofielen, Input Actions
  Tests/
    EditMode/
Docs/
  GAME_DESIGN.md
CLAUDE.md
```

Maak per map in `Scripts/` een Assembly Definition (`.asmdef`). Afhankelijkheden lopen één kant op: `Core` kent niemand, alle features mogen `Core` kennen, `UI` mag de gameplay kennen en nooit andersom, `Platform` wordt alleen via interfaces gebruikt.

## Levels bouwen

Een nieuw level maken betekent een nieuwe `LevelDefinition`, zonder code te wijzigen en zonder handmatig scenewerk. Bouw een validator die een level controleert (loopt het pad door naar de basis, staat elke bouwplek op een geldige plek, zijn alle verwezen torens en padobjecten aanwezig) en draai die ook in de tests. Voeg een testmodus toe met oneindig geld en het overslaan van waves.

## Volgorde van bouwen

Werk één mijlpaal tegelijk af, en toon me na elke mijlpaal wat ik kan uitproberen.

1. **Fundament:** projectopzet en git (zie hierboven), mappen, asmdefs, Input System, camera, `Core`-events en services, `Boot`-scene en generieke levelscene, `Docs/COMMANDS.md` met de Unity CLI-commando's (en de batchmode-terugvaloptie), en een eerste test die draait.
2. **Level 1 speelbaar:** raster, pad met waypoints, bouwplekken, geld, win en verlies. Vijanden: Scout, Grunt en Brute (land) en Vlieger (lucht). Torens: pulsgeweer, kanon en raket (lucht), met upgrades en sterren. Wavedata per moeilijkheid (easy tot psycho). Alles via ScriptableObjects, met placeholders van simpele vormen. Een debug-optie maakt voor de test alle wapens beschikbaar.
3. **Menu en schermen rond level 1:** zodat ik een complete ronde kan spelen.
   - **Hoofdmenu** met Start en Opties (Afsluiten alleen op pc).
   - **Visuele levelkiezer:** een kaart van de planeet met de vijf levels als punten (vergrendeld, open, voltooid), en het kiezen van een moeilijkheid. Alleen level 1 is speelbaar zolang de rest niet bestaat.
   - **Resultaatscherm** na een level met de beloning: geld, onderzoekspunten, power currency en het nieuwe wapen, met een korte tekst of het gestolen alientech is of intel van de thuisplaneet.
   - **Upgradescherm** (de basis): permanente upgrades voor torens, blokkades en basis, betaald met onderzoekspunten, en opslag van de voortgang via `ISaveService`.
   - Flow: hoofdmenu, kaart, level, resultaat, upgradescherm, terug naar de kaart. Alles met een controller bedienbaar, met placeholder-uiterlijk dat wel consistent is.
4. **Kapot gaan:** beschadigde torens, repareren, wrak en scrap.
5. **Blokkades en hero.**
6. **Drones** en power currency.
7. **Padobjecten** kopen en plaatsen.
8. **Combineren** van torens met recepten.
9. **Meta:** kaartscherm, basisscherm, unlocks na elk level, moeilijkheden easy tot psycho.
10. **Look and feel:** licht, bloom, deeltjes en geluid, per level een eigen palet.

Na mijlpaal 3 speel ik een complete ronde, van menu tot upgradescherm, en beslissen we samen of de gameplay leuk genoeg is om door te gaan.
