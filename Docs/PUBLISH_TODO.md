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
