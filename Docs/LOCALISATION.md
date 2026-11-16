# Localisation

Added in round 69 (2026-11-16). The game ships in English only; this is the plumbing so other languages can be added
without touching code. No Unity package is used: a small string-table system of our own (IL2CPP-safe, no reflection at
runtime, no Addressables).

## How it works

- **Keys, not text.** Code asks for text by key: `Loc.Get("pause.resume")`, or `Loc.Format("hud.wave", wave, total)` for
  a text with `{0}` placeholders (`Game.Core.Loc`). Keys are written as literals inside those calls, so the tests can
  find them.
- **Definitions hold keys.** The player-facing text fields of the ScriptableObjects (tower, enemy, upgrade, drone, gadget,
  hero, level and difficulty names and descriptions, dialogue lines, loading tips, boss texts and barks, campaign region
  and node names, cutscene title cards, level rewards) store a key such as `data.tower.PulseRifle.displayName`. Their
  properties (`DisplayName`, `Description`, `DialogueSegment.Text`, ...) return the text in the current language. The list
  of converted fields is `Game.Editor.LocalisationTables.Fields`.
- **Tables** are plain text files in `Assets/_Project/Resources/Localization/`, named `<language>.<part>.txt`. All parts
  with the same language code form one table:

  | File | Written by | Holds |
  | --- | --- | --- |
  | `en.core.txt` | hand | `language.name` (the language's own name, shown under Options) |
  | `en.ui_menus.txt` | hand | main menu, map, loadout, briefing, loading, result, upgrade screen, codex, base panel, story viewer, sound test |
  | `en.ui_level.txt` | hand | HUD, tower menu, hero, dialogue box, bosses, cutscene overlay, radar, placement, sound-test labels |
  | `en.ui_options.txt` | hand | options, controls page, pause menu, controller notice |
  | `en.data.txt` | generator | every definition text field (`data.` keys) |

- **Format:** one `key = text` per line; `#` starts a comment line. After `= ` the rest of the line is the text exactly as
  written (leading spaces after the first one and trailing spaces count). `\n` is a line break, `\t` a tab, `\\` a
  backslash. Rich-text tags (`<color=#FFD45C>`) and `{0}` placeholders are written as they are. Keys are lowercase,
  dot-separated, without spaces, unique over all files of a language; `data.` is reserved for the generated table.
- **Lookup order:** current language, then English, then the key itself (a visible gap, never an empty label).
- **Service:** `StringTableService` implements `ILocalizationService` and is registered in `Bootstrapper.Awake`; the
  chosen language is `SettingsData.LanguageCode` in `settings.json`. Before boot (EditMode tests, editor tools) `Loc`
  loads the English tables by itself.

## The generated data table

The generator still writes English into the definitions; its last data step, **"Localisation"**
(`LocalisationTables.Run`, also `Tools/Generate/Localisation Tables`), moves each text into `en.data.txt` and leaves the
key in the asset. Running it again is safe: a field that already holds its key keeps the table's text. So:

- To change English data text for good, change it in the generator spec (or in `en.data.txt` for a quick fix: the next
  full generator run writes the spec's text again).
- A new definition field with player text: add its serialized path to `LocalisationTables.Fields`, return
  `Loc.Get(_field)` from its property, run the menu item.
- Keys follow the asset: `data.<kind>.<asset name>.<field path>`. Renaming an asset or reordering dialogue lines gives new
  keys on the next run; translations of the old keys must then be moved along.

## Adding a language

1. Copy every `en.*.txt` file to `<code>.*.txt` (for example `de.core.txt`, `de.ui_menus.txt`, `de.data.txt`, ...).
2. Translate the text after `= `; keep keys, placeholders (`{0}`, `{1:0.0}`), `\n` and rich-text tags. Set
   `language.name` to the language's own name ("Deutsch").
3. Open Options: the **Language** button now steps through the languages; a change reloads the main menu (every screen
   builds its text when it opens).
4. Run the EditMode tests: `LocalisationTests.OtherLanguages_CarryExactlyTheEnglishKeys` lists missing and extra keys.

## Checks (EditMode, `LocalisationTests`)

- Every table parses without bad lines or duplicate keys.
- Every key in code (`Loc.Get/Format/Has("...")`) has an English text.
- Every converted definition field holds a `data.` key with an English text.
- Other languages carry exactly the English keys.
- **Scan for new hardcoded UI text:** in the runtime scripts (not `Editor/`, not `Balance/`), a line that sets UI text
  (`UiKit.CreateText/CreateFixedText/CreateButton/CreateSlider/SetLabel/Keyed`, `SetText(`, `.text =`, `.text +=`) may not
  hold a string literal with two letters in a row, outside `Loc` keys and interpolation holes. A deliberate exception ends
  the line with `// loc: keep` and a reason.

## Limits and known gaps

- **Fonts.** Chakra Petch (the UI font) covers Latin, Thai and Vietnamese only: no Cyrillic, Greek, Chinese, Japanese or
  Korean. Those languages need fallback fonts of a matching weight, for both text systems (legacy `UnityEngine.UI.Text`
  on most screens, TextMeshPro in dialogue and cutscenes), see `Docs/RELEASE_GAPS.md` 6.2 and 6.3. No font was changed.
- **Longer text.** Many labels have fixed boxes and best-fit shrinking; German or French run 20 to 35% longer
  (`RELEASE_GAPS.md` 6.4).
- **Dialogue segments** are translated per segment (a struck or redacted word is its own segment), so a translator must
  keep the split; word order inside a styled line cannot move across segments.
- **Not converted** (developer-facing or not text): validation messages, Debug logs, balance and probe reports, the
  story viewer's developer labels where they are asset ids, creature names in the sound test (from asset names), enemy
  element names in the Codex (enum names), symbol-only labels (`+50`, `-20%`). Button prompt labels come from the glyph
  assets (`Settings/Input/Glyphs_*.asset`) and are per pad layout, not per language.
- **Number formatting** follows the current culture as before (`string.Format`).
