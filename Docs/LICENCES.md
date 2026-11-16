# Licences and third-party assets

Written in round 72 (2026-11-16) from a full read of `Assets/`, `ArtSource/`, `Tools/`, `Packages/` and the docs. Nothing was checked against the original download pages: those are links Remco has to open himself (see "Open items"). The in-game credits screen (main menu > Credits, data `Assets/_Project/Data/Menu/Credits.asset`, text `credits.*` in `Resources/Localization/en.ui_menus.txt`) is built from this list. `Docs/CREDITS.md` is the short version.

Status legend: **ok** (source and licence known, obligations met), **ok, credit given** (attribution optional or required and done), **check** (likely fine but one fact is missing), **open** (source or licence unknown: must be settled before a public build).

## 1. Fonts

| Asset | Where | Source | Licence | Attribution / obligation | Status |
| --- | --- | --- | --- | --- | --- |
| Chakra Petch Regular, Medium, SemiBold, Bold (UI font) | `Assets/_Project/Art/UI/Fonts/` | Cadson Demak, The Chakra Petch Project Authors, via google/fonts `ofl/chakrapetch`, downloaded 2026-11-09, unmodified | SIL OFL 1.1 | Copyright notice and licence must travel with the font. Done: the full text (`Art/UI/License_Fonts.txt`) is shown on the credits screen and ships in the build through the credits asset. Do not sell the font on its own; do not call a modified copy "Chakra Petch". | ok, credit given |
| Liberation Sans (TextMesh Pro fallback, SDF assets) | `Assets/TextMesh Pro/Fonts/`, `Assets/TextMesh Pro/Resources/Fonts & Materials/` | Red Hat / Liberation Fonts project, shipped by Unity in TMP Essentials | SIL OFL 1.1 (`LiberationSans - OFL.txt`) | Same as above; its licence text is the second licence on the credits screen. | ok, credit given |
| Michroma, Tomorrow (logo and Steam capsule renders only, not in the game) | `ArtSource/Previews/Logo/v2/_fonts/` | Google Fonts | SIL OFL 1.1 (licence files present) | None for rendered images. Nothing ships. | ok |
| Russo One, Black Ops One, Saira (logo experiments, not in the game) | same folder | Google Fonts | SIL OFL 1.1 (no licence file in the folder) | None for images. Add the OFL files if these fonts stay in the repo. | check |
| Arial (Windows system font) | used by `Tools/Blender/build_warden_concepts.py` for concept renders | Microsoft | Windows font EULA | Must not appear in shipped art or store images. Concept renders only today. | check |

## 2. Audio used in the game

| Asset | Where | Files | Source | Licence | Attribution | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Sci-fi Sounds 1.0 | `Audio/Kenney_SciFi/` | 73 .ogg | Kenney, kenney.nl (`License.txt`) | CC0 | Not required ("not mandatory"); credited | ok, credit given |
| Sci-Fi Weapon Shots | `Audio/Lentikula/` | 15 .wav | Lentikula, lentikula.itch.io (`Readme.txt`) | CC0 | Not required; credited | ok, credit given |
| Laser weld (boss laser loop) | `Audio/Freesound/freesound_community-laser-weld-103309.mp3` | 1 | "freesound_community" on Pixabay (id 103309) | Pixabay Content License | Not required; credited | ok, credit given |
| **Warning siren (boss alarm)** | `Audio/Freesound/warning.mp3` | 1 | **unknown**, downloaded by Remco 2026-10-29 | **unknown** ("free use assumed") | - | **open** (see section 7) |
| Lava bubble loop (lava and rift ambience) | `Audio/lavabubble.mp3` | 1 | **not recorded** (commit 31e0d9c1 "Remco's recorded clips") | unknown, or Remco's own recording | - | **open**: ask Remco whether he recorded it |
| Hero death | `Audio/hero-death.mp3` -> `Approved/Hero/hero_death.wav` | 1 | **not recorded** | unknown | - | **open** |
| Laser charge | `Audio/gregorquendel-laser-charge-175727.mp3` -> `Approved/laser_beam_loop.wav` and intro cuts | 1 | "gregorquendel" on Pixabay (id 175727) | Pixabay Content License (from the file name; page not checked) | Not required; credited | check |
| Intro and dialogue source clips (cave-water-drops, drill, earth-rumble, footsteps x3, gasp, ground-tearing, hydraulic-door-opening, long-hiss, male-breathing, mechanical-clamp, metal-creaking, metal-hammering, pistol-draw, radio-static, release-hiss, rocket-booster-crackle, space-rumble, spaceship-alarm, spaceship-door-opening, spaceship-rumble, spaceship-shuttle-firing, stone-drilling, wind-blowing) | `Audio/*.mp3` -> `Approved/Intro/**` (73 wav, mapping in `Tools/Audio/out/intro/manifest.md`) and the dialogue static | 25 | "free sound sites such as Pixabay" (`Docs/AUDIO.md`); no per-file page or author | Pixabay Content License assumed | Not required if Pixabay | **check**: note the Pixabay page of each clip (8 of them share the Pixabay transcode signature, see section 7) |
| Slime recordings (Slug creature and boss) | `Approved/Monsters/Slug`, `Approved/Bosses/Slug` (3 + 3) | from 3 mp3s | `freesound_community-slime-2-30099`, `floraphonic-goopy-slime-2-219775` (Pixabay names), `slime_dies.mp3` (source unknown) | Pixabay Content License for the first two | Not required; credited | check (`slime_dies.mp3` open) |
| Synthesised sound effects (explosions, drones, hero, environment, monsters, bosses, dialogue blips) | `Approved/` (rest of the 335 wav) | ~250 | Made in code: Python DSP scripts in `Tools/Audio/` (`sfx_real.py`, `cat_*.py`), written by Remco with Claude; some layers start from Kenney CC0 clips (`src_intro/kenney_*.wav`) | Project-owned (CC0 parts need nothing) | - | ok |
| Music loops (menu, game calm, game boss) | `Approved/Music/*_loop.wav` (from `music.mp3`, `menu-music.mp3`, `game-music.mp3`, `boss-music.mp3`) | 3 (+4 sources) | Generated by Remco with **Google Gemini** (the source mp3s carry Google C2PA manifests, "trainedAlgorithmicMedia", SynthID), looped by `Editor/LoopMaker.cs` | Google's generative-AI terms (not checked here) | Credited as "Music generated with Google Gemini" | **check**: read the terms that applied when the music was made (Gemini app / Lyria) for commercial use in a sold game; see PUBLISH_TODO "AI disclosure" |

Correction to the old `Docs/CREDITS.md`: not all of `Approved/` is project-owned. `Approved/Intro`, `Approved/laser_beam_loop.wav`, `Approved/Hero/hero_death.wav` and the slime files are cut from downloaded clips; their licence is the licence of their source.

## 3. Models, textures and images

| Asset | Where | Source | Licence | Status |
| --- | --- | --- | --- | --- |
| Every model, icon, panel, terrain and painted texture | `Assets/_Project/Art/Models/Blender/` (375 FBX, 3010 PNG), `Art/Terrain/`, `Art/Textures/` | Built by Python scripts in `Tools/Blender/` with Blender ("nothing is modelled by hand", `ArtSource/README.md`) | Project-owned. Blender (GPL) is a tool; its licence does not reach the output. | ok |
| MakeHuman system assets: base mesh, skins (young_caucasian_female, middleage_caucasian_male), high-poly eyes, eyebrow010 / eyebrow001, eyelashes01, teeth, shoes04, faceunits01 expressions | inside the Voss and Spencer exports (`Art/Models/Blender/Voss`, `.../Mercer`) | MakeHuman / MPFB 2.0.18 (extensions.blender.org); packs `makehuman_system_assets_cc0.zip`, `faceunits01.zip` | CC0 ("explicitly released as CC0 in September 2020, Copyright (C) 2020 Data Collection AB") | ok, credit given |
| Hair `rehmanpolanski_hair_bun_brown` (Voss) | same | MakeHuman community, author Rehman Polanski, pack `hair01_cc0.zip`, makehumancommunity.org/node/2477, after Ponytail01 | Pack metadata says **CC0**; the `.mhclo` header says **AGPL3** | **check**: the two disagree. The AGPL line is probably MakeClothes' default header, but confirm on the node page or swap the hair for a system-asset hair |
| MPFB add-on itself | Blender extension, not shipped | MakeHuman team | GPLv3 (tool only) | ok |
| HDRIs, downloaded texture sets | none found | - | - | ok |
| Screenshots in `Assets/Temp/` (38 png) | project captures | project | - | ok (not third-party; consider moving them out of `Assets/`, they ship only if referenced) |

## 4. Code and shaders

- `Assets/_Project/Scripts`, `Tools/`: project code, written by Remco with AI assistance (Claude). No copied third-party code found (searched for licence headers, "adapted from", "based on", Stack Overflow, GitHub, Shadertoy). **ok**
- `Assets/_Project/Art/Shaders/` (23 shaders, 2 hlsl): project code, no third-party headers. **ok**
- `Assets/TextMesh Pro/` shaders and resources: Unity, Unity Companion License (covered by the Unity licence). **ok**
- No native plugins, DLLs or Steamworks code in the project.

## 5. Unity packages

All from the Unity registry (`packages.unity.com`), no git or local packages: ai.navigation 2.0.15, collab-proxy 2.13.6, ide.rider 3.0.38, ide.visualstudio 2.0.26, inputsystem 1.20.0, **pipeline 0.8.0-exp.1** (experimental, editor only), render-pipelines.universal 17.6.0, test-framework 1.8.0, timeline 6.6.0, ugui 2.6.0, visualscripting 1.9.12, plus their dependencies (burst, collections, shadergraph, render-pipelines.core, nunit, mono-cecil, newtonsoft-json, ...). They come under the Unity Companion License / Unity terms; the few that wrap open-source code (nunit, mono-cecil, newtonsoft-json) carry their own notices and are editor or test tools. Nothing to credit in the game beyond "Made with Unity" (the splash screen). **ok**

- Unity itself: the splash screen is on; whether it can be switched off and whether console add-ons are available depends on Remco's Unity plan (RELEASE_GAPS 8.8). **check**

## 6. AI-generated or AI-assisted content (facts, for the store disclosure)

| What | Tool | How | Ships in the game |
| --- | --- | --- | --- |
| Music (3 loops) | Google Gemini | Generated audio, cut into loops | yes |
| Sound effects | Claude (chat) | Claude wrote DSP code with Remco; the sounds are synthesised by that code, not by a generative audio model | yes |
| 3D models, icons, panels, textures | Claude | Claude wrote the Blender Python scripts that build them; no image or mesh generator was used | yes |
| Human characters | MakeHuman (CC0) + Claude-written scripts | Base meshes from MakeHuman, shaped and dressed by scripts | yes |
| Game code, tools, docs | Claude (Opus, Sonnet, Fable) | Written with AI assistance (git trailers: ~235 commits) | yes (code) |
| Story and dialogue text | Remco wrote the story script (S-69-CRYS); the in-game lines, briefings, hints and Codex texts were written into the generator by Claude from that script and Remco's feedback (to confirm with Remco) | - | yes |
| Voice | none (text only) | - | - |
| Live generation at runtime | none | - | - |

The draft disclosure text is in `Docs/PUBLISH_TODO.md` ("AI-content disclosure (draft)").

## 7. `warning.mp3` (boss alarm): what is known

- File: 108,960 bytes, MPEG-2 Layer III, 24 kHz, 160 kbps CBR joint stereo, 227 frames = about 5.45 s, encoded by LAME 3.100. No ID3v1 or ID3v2 tag, no title, author, comment or URL. Added in the first commit (2026-10-30, file date 2026-10-29 22:43), moved to LFS 2026-11-04.
- The sibling `freesound_community-laser-weld-103309.mp3` (a Pixabay download) has the identical encoder header (`ff f3 e4 64`, LAME 3.100, no tags), and so do eight intro clips from the same download session. That points to a **Pixabay download that was renamed to `warning.mp3`**, but it is not proof.
- Docs: `CREDITS.md` "downloaded by Remco, source and licence to be noted"; `STATUS.md` "source still to be noted"; `RELEASE_GAPS.md` 11.1.
- Used as the boss alarm (`ProjectGenerator.Audio.cs:90`, `_bossAlarms`).

Options for Remco (nothing was replaced):

1. **Find the page** (cheapest): search Pixabay sound effects for "warning", "alarm", "siren" around 5 seconds and compare by ear, or check the browser history of 2026-10-29. If found, note the page URL and author in `Docs/CREDITS.md` and the credits screen (Pixabay needs no attribution).
2. **Replace it with a known CC0 alarm** (for example from the Kenney packs already in the project, or `spaceship-alarm.mp3`, whose source is equally unrecorded, so that one would need its page too).
3. **Synthesise a new alarm** with the `Tools/Audio` scripts, like the other approved effects: no licence question at all. Needs Remco's ear before it replaces the approved sound.

## 8. Open items (before any public build)

1. `warning.mp3`: source unknown (section 7).
2. `lavabubble.mp3`, `hero-death.mp3`, `slime_dies.mp3`: source not recorded. Ask Remco (own recordings?).
3. The 25 intro and dialogue clips and `gregorquendel-laser-charge`: note the Pixabay page of each (a line per file in `Docs/AUDIO.md`).
4. MakeHuman hair bun: AGPL3 header against CC0 pack metadata.
5. Gemini music: check the commercial-use terms that applied when the music was generated.
6. Russo One, Black Ops One, Saira: add the OFL files next to them or delete the unused ones.
7. Steam AI-content disclosure: draft in PUBLISH_TODO, Remco decides the wording.
8. Name on the credits screen: "Made by Remco Borst" is a guess at how Remco wants to be credited (a studio name may replace it once the company name is chosen).
