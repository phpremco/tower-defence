# Publish TODO

Lijst met alles wat geregeld moet worden vóór een release op Steam en later consoles.
**Pas oppakken als Remco besluit dat het spel leuk genoeg is om uit te brengen.** Tot die tijd gaan gameplay en balans voor.

## Identiteit van het spel
- [ ] Definitieve spelnaam (`productName` staat nu op "Tower defense").
- [ ] Bedrijfsnaam kiezen (`companyName` staat nu op "DefaultCompany"). **Vroeg doen:** de naam zit in het opslagpad (`%USERPROFILE%\AppData\LocalLow\<bedrijf>\<spel>\Saves`). Na een release wijzigen betekent dat spelers hun saves kwijt zijn. Bij de wijziging de bestaande save van Remco meeverhuizen.
- [ ] Versienummering afspreken (`bundleVersion` staat op 0.1.0).

## Opslaan
- [ ] Veilig schrijven in `FileSaveService`: eerst naar een tijdelijk bestand, daarna in één stap vervangen (`File.Replace`), met een `.bak` van de vorige versie. Bij het laden valt het spel terug op de `.bak` als het hoofdbestand kapot is.
- [ ] Versieveld in de save, plus een migratiestap per versie, zodat oude saves blijven werken als de opbouw van de save verandert.
- [ ] **Steam Cloud via Auto-Cloud** (geen code nodig). Instellen in het Steamworks-partnerportaal: root `WinAppDataLocalLow`, pad `<bedrijf>/<spel>/Saves`, patroon `*.json`. Ook Mac, Linux en Steam Deck instellen als die builds er komen.
- [ ] Alternatief, alleen als dat nodig blijkt: `SteamSaveService` met de Steam Remote Storage API via Steamworks.NET. Dat is een nieuw package, dus eerst vragen.
- [ ] Switch: een eigen `ISaveService`-implementatie met de save-API van het platform.

## Achievements
- [ ] Lijst met achievements ontwerpen, bijvoorbeeld per level en moeilijkheid, baas verslagen, alle hybrides gemaakt, psycho gehaald of geen toren verloren. Ontwerpkeuze voor Remco.
- [ ] Achievements als data opslaan in een `AchievementDefinition`-ScriptableObject, met een id die overeenkomt met Steamworks.
- [ ] Gameplay-events koppelen aan `IAchievementService`. Die interface en `NullAchievementService` bestaan al.
- [ ] `SteamAchievementService` met Steamworks.NET (nieuw package, eerst vragen). Steam-code komt nooit in de gameplay-code.
- [ ] Achievements en iconen aanmaken in Steamworks. De iconen uit Blender, net als de rest van de art.

## Steam-integratie
- [ ] Steamworks-account, app-id en de bijdrage voor de Steam Direct-fee.
- [ ] Steamworks.NET (of een vergelijkbaar package) toevoegen achter de platform-interfaces.
- [ ] Steam Input en controllerconfiguratie; Steam Deck-verificatie (720p-UI, tekstgrootte, controller-only).
- [ ] Rich presence (optioneel).
- [ ] Builds uploaden via SteamPipe (`BuildScript` + depot-config).

## Store en presentatie
- [ ] Storepagina: capsule-art, screenshots, trailer en beschrijving.
- [ ] Leeftijdsclassificatie (IARC) en privacyverklaring.
- [ ] Credits en licenties controleren (`Docs/CREDITS.md`): MPFB (CC0), audio, fonts.

## Kwaliteit vóór release
- [ ] Balans door Remco als leuk beoordeeld, op alle moeilijkheden.
- [ ] Prestaties op een zwakkere pc en op Steam Deck (Low-profiel).
- [ ] Pauzeren en hervatten bij onderbrekingen (Steam-overlay, controller weg).
- [ ] Opties: resolutie, volledig scherm, volume-sliders, toetsen opnieuw toewijzen.
- [ ] Lokalisatie (optioneel).

## AI-content disclosure (draft, round 72 – Remco decides)

Facts behind it: `Docs/LICENCES.md` section 6. Steam's content survey asks whether the game contains content made with generative AI during development ("pre-generated") and whether anything is generated live while playing; the answer is shown on the store page. Check the current wording of the survey and of Valve's rules before filling it in (they changed in 2024 and may have changed again); console holders ask similar questions.

Draft answer, pre-generated content:

> Deepwake was made by one developer with the help of AI tools. The music was generated with Google Gemini and edited into loops for the game. The game's code, the scripts that build its 3D models, icons and interface art, and the code that synthesises its sound effects were written with the help of Claude (Anthropic); no image or 3D-model generator was used, and every model is built procedurally in Blender. Human characters start from open MakeHuman (CC0) base meshes. The story was written by the developer. All AI-assisted content was reviewed and directed by the developer.

Draft answer, live-generated content:

> None. The game does not generate any content with AI while it runs.

Choices for Remco before using it:
- [ ] Is the name right ("Deepwake", or the series title)? Is "one developer" how you want to present it?
- [ ] Music: read the Gemini / Google generative-AI terms that applied when the music was made (commercial use in a sold game, any attribution or labelling duty). The source mp3s carry Google's C2PA provenance and SynthID marks; the shipped loops do not (the WAV conversion dropped the metadata, the audio watermark may remain).
- [ ] Decide whether "AI-assisted code" belongs in the disclosure at all (Steam's survey is about content players see and hear; mentioning it is the cautious choice).
- [ ] Story and dialogue: confirm who wrote the in-game lines (your script vs lines drafted into the generator by Claude) and adjust the sentence.
- [ ] Same text on the store page "About" and in the press kit, so nothing contradicts.
