# Research: hair-bun licence, Gemini music, crash reporting

Written 2026-11-16 (round 72 follow-up). All web sources were accessed on **2026-11-16** unless a line says otherwise. This is research, not legal advice: where a verdict carries risk, the risk is named. Related: `Docs/LICENCES.md` (open items 4 and 5), `Docs/PUBLISH_TODO.md` (AI disclosure draft).

---

## 1. Dr Voss's hair bun (`rehmanpolanski_hair_bun_brown`)

### What is on this machine

Installed MPFB 2 data: `%APPDATA%\Blender Foundation\Blender\5.2\extensions\.user\blender_org\mpfb\data\`.

| File | What it says |
| --- | --- |
| `hair/rehmanpolanski_hair_bun_brown/rehmanpolanski_hair_bun_brown.mhclo` and `hair_bun_brown.obj` | `# Exported from MakeClothes (TM)` / `# author Rehman Polanski` / `# license AGPL3 (see also http://www.makehuman.org/doc/node/external_tools_license.html)` |
| `packs/hair01.json` (pack metadata MPFB installed) | `"license": "CC0"`, author `RehmanPolanski`, source `makehumancommunity.org/node/2477`, description "This model is adapted from the MH Ponytail01" |
| `hair/ponytail01/ponytail01.mhclo` (the parent asset, a MakeHuman system asset) | "This asset was explicitly released as CC0 in september 2020" |
| `packs/makehuman_system_assets.json` | all ten system hairs (afro01, bob01, bob02, braid01, long01, ponytail01, short01-04): `CC0`, author `makehuman_system` |

The same split shows on other community hairs: 10 of the 24 hairs in `hair01` carry the identical `AGPL3 (see also ... external_tools_license.html)` header (or the older `node/320` variant) while the pack lists every one of them as CC0. That pattern points at a tool-generated header, not at 10 authors choosing AGPL.

What ships: `Tools/Blender/build_voss3.py` takes the hair mesh (decimated to 68 %), its resampled texture and bakes them into `Assets/_Project/Art/Models/Blender/Voss`. The `.mhclo` header itself does not ship.

### What the official sources say

- The **hair01 asset-pack page** lists `rehmanpolanski_hair_bun_brown | RehmanPolanski | asset repo | CC0` in its "Included assets" table. <https://static.makehumancommunity.org/assets/assetpacks/hair01.html>
- **MakeHuman licence page**: code is AGPL (MakeHuman) / GPL (MPFB); "All core assets are shared under Creative Commons, CC0" with no attribution needed. <https://static.makehumancommunity.org/about/license.html>
- **"What changed regarding the license in 2020?"**: assets are by default CC0 (before: AGPL by default); targets, proxies and base mesh are graphical assets under CC0; and "if you find remaining strangenesses, particularly where assets are listed as AGPL, please report these". <https://static.makehumancommunity.org/oldsite/faq/what_changed_regarding_the_license_in_2020.html>
- **Legal page (old site)**: MakeHuman's external tools, including the MakeClothes Blender add-on, are AGPL 3 "unless otherwise explicitly stated". The header line's link (`external_tools_license.html`) is that tool licence. A search snippet of the same old documentation adds that the original author of clothes "is free to choose any license for them" (snippet only, the sentence was not visible on the fetched page). <https://static.makehumancommunity.org/oldsite/documentation/legal.html>
- **Asset-pack FAQ**: CC0 = donated to the public domain, no restrictions; CC-BY packs are marked separately. <https://static.makehumancommunity.org/assets/assetpacks/faq.html>
- **"Can I sell models created with MakeHuman?"**: yes, exported models are CC0, commercial use without permission or payment. <https://static.makehumancommunity.org/oldsite/faq/can_i_sell_models_created_with_makehuman.html>
- The asset's own repository page (`makehumancommunity.org/node/2477`) returned **404** on 2026-11-16, so the author's upload-time licence field could not be read directly.

### Verdict

**The true licence is CC0; shipping the mesh in a commercial game is OK.** The AGPL3 line is the boiler-plate header that MakeClothes 1.x wrote into every export (it links to the *tool's* licence page, not to an asset licence), the official pack page and pack metadata both say CC0, the bun is a reworking of `ponytail01`, which is itself an explicit CC0 system asset, and MakeHuman's own 2020 FAQ treats AGPL listings on assets as leftovers to be reported. Residual risk is low: the only gap is that the author's original upload page is offline.

### Recommended actions

1. Keep the bun. Save a PDF/print of the hair01 pack page (showing the CC0 row) into `ArtSource/Licences/` as evidence, and change the status in `Docs/LICENCES.md` and `Docs/CREDITS.md` from "check" to "ok (CC0 per official pack page; AGPL header = MakeClothes tool header)". Keep crediting Rehman Polanski (not required by CC0, but free goodwill).
2. Only if Remco wants zero doubt: swap to a **system** hair whose file header itself says CC0, i.e. `ponytail01` (the bun's own parent) or `braid01`, and shape the bun in the build script; or model the bun in `build_voss3.py` from scratch (a torus/sphere knot with the existing hair texture). Do **not** pick `elvs_reverse_french_braid_bun` (header says CC_by), `o4saken_long01` (header CC BY 4.0) or the other AGPL-header community hairs: they carry the same or a worse mismatch.

---

## 2. Gemini-generated music in a commercial game

### How the music was made (facts from the files)

- `Assets/_Project/Audio/music.mp3` (2026-10-29), `menu-music.mp3`, `game-music.mp3`, `boss-music.mp3` (2026-11-01): ~180 s each, 192 kbps, 44.1 kHz.
- Each carries a signed **C2PA manifest** from "Google C2PA Core Generator Library": action `c2pa.created` "Created by Google Generative AI" and `c2pa.edited` "Applied imperceptible SynthID watermark", digital source type `trainedAlgorithmicMedia`.
- Three-minute tracks in the Gemini app = **Lyria 3 Pro in the Gemini app** (Lyria 3 makes 30 s clips; Lyria 3 Pro, launched 2026-03-25, makes tracks "up to 3 minutes long", starting with paid subscribers). <https://blog.google/innovation-and-ai/technology/ai/lyria-3-pro/>, <https://blog.google/innovation-and-ai/products/gemini-app/lyria-3/>
- `Docs/LICENCES.md` and `CREDITS.md` say "generated by Remco with Google Gemini". Confirm with Remco it was the consumer Gemini app (not AI Studio/Vertex, which have the separate Gemini API terms).

### Which terms apply

- **Gemini Apps** are governed by the **Google Terms of Service** plus the **Generative AI Prohibited Use Policy** (Gemini Apps Privacy Hub: "The Google Terms of Service and the Generative AI Prohibited Use Policy apply to Gemini Apps"). Work/school accounts get different data-handling terms. <https://support.google.com/gemini/answer/13594961?hl=en>
- The old "Generative AI Additional Terms" stopped applying on 2024-05-22. <https://policies.google.com/terms/generative-ai>
- **Google Terms of Service** (effective 2026-07-30): "Some of our services allow you to generate original content. Google won't claim ownership over that content." Prohibited: "misleading others into thinking that generative AI content was created by a human" and using AI output to develop ML models. Note: the ToS defines a "consumer" as personal, non-commercial use for *consumer-law* purposes; that is not a ban on commercial use of outputs, and businesses may use the services if an authorised representative agrees to the terms. <https://policies.google.com/terms?hl=en>
- **Prohibited Use Policy** (last modified 2024-12-17): no infringing others' IP; no "misrepresenting the provenance of generated content by claiming it was created solely by a human, in order to deceive". Nothing about keeping SynthID or C2PA metadata, and no attribution duty. <https://policies.google.com/terms/generative-ai/use-policy>
- The Lyria 3 Pro announcement only repeats the ToS and prohibited-use policy and says every Lyria output carries SynthID; it makes no ownership or commercial-use statement either way.
- No Google page found grants or forbids commercial use of Gemini-app music explicitly. Secondary sources disagree on this (e.g. <https://terms.law/ai-output-rights/gemini/> says commercial use is allowed on all tiers, but cites no clause); treat them as opinion.

### Copyright reality

Purely AI-generated music has no human author, so in the US (US Copyright Office, *Copyright and Artificial Intelligence, Part 2: Copyrightability*, January 2025, <https://www.copyright.gov/ai/>; known from prior reading, not re-fetched today) and very likely in the EU it is **not protected by copyright**. Consequences: Remco cannot stop others from reusing the loops, Google may produce similar output for other users, and nothing in the terms gives him an indemnity if a track resembles an existing song.

### Steam's rules (current)

- **Steamworks Content Survey**, Generative AI section: *Pre-Generated* = "any kind of content that ships with your game and is consumed by players" made with AI tools during development; *Live-Generated* = content created with AI while the game runs (needs guardrails). "Efficiency gains through the use of these tools is not the focus of this section." <https://partner.steamgames.com/doc/gettingstarted/contentsurvey>
- **January 2026 revision**: disclose AI-generated content that is in the game or its store page / marketing assets; AI-powered productivity tools such as code helpers do not need disclosure. <https://www.gamedeveloper.com/business/valve-tweaks-and-clarifies-ai-disclosure-rules-for-steam> (2026-01-16)
- The disclosure text is shown on the store page; AI content is judged by the same content rules as anything else.

### Verdict

**Remco can ship the music.** Google claims no ownership, the terms add no attribution, watermark or labelling duty and do not forbid commercial use; the only hard rules are: don't pass it off as human-made, don't use it to train AI, don't infringe. **He must disclose it on Steam** as pre-generated AI content (music). Risks, in order: (1) no copyright protection, so no exclusivity; (2) a chance that a track resembles existing music and draws a YouTube/Twitch Content-ID claim against streamers, with no indemnity from Google; (3) a minority of players react negatively to AI music (the store disclosure makes it visible); (4) terms may change, so keep evidence of what applied.

### Recommended actions

1. Keep the original mp3s (with their C2PA manifests) and note the date, the account type (paid Gemini subscription?) and the prompts in `Docs/AUDIO.md` as a provenance record. Save a PDF of the Google ToS (2026-07-30 version) next to them.
2. Fill the Steam survey with the music in the pre-generated box. The draft in `PUBLISH_TODO.md` already does this; per the 2026 rules the "code written with Claude" sentence is optional (productivity tool), while the scripted models are a judgement call (cautious: keep the sentence).
3. Keep "Music generated with Google Gemini" in the credits (not required, but it satisfies the "don't mislead about provenance" rule).
4. Before release, run each loop past a music-recognition check (e.g. a YouTube unlisted upload to see if Content ID matches anything). If the game is a success, consider replacing the three loops with commissioned human music, which also solves exclusivity.

---

## 3. Crash reporting (Steam now, Switch/consoles later)

Project facts: Unity **6000.6.5f1**, IL2CPP Windows build exists (round 71-72), a local `crash.log` already exists (`Assets/_Project/Scripts/Core/CrashLog.cs`, `Boot/CrashLogHook.cs`). `ProjectSettings/UnityConnectSettings.asset` has `m_EnableCloudDiagnosticsReporting: 0` but `InsightsSettings.m_EngineDiagnosticsEnabled: 1`, and `ProjectSettings.asset` has `submitAnalytics: 1` (hardware statistics); both send data to Unity and belong in the privacy policy or should be switched off.

| Option | Platforms | Native / IL2CPP | Cost | Privacy | Fit |
| --- | --- | --- | --- | --- | --- |
| **Sentry Unity SDK** | C# errors on Windows, Linux, macOS, Android, iOS, WebGL; native crashes on Windows/Linux (minidumps via sentry-native) and **PlayStation, Xbox, Switch 1 and 2** | IL2CPP C# line numbers (managed line numbers for Unity 6.5+), native minidumps, automatic symbol upload, consoles included | Developer (free): 5k errors/month, 1 user, 30-day retention; Team $26-29/month for 50k. **Consoles need the Enterprise plan** plus middleware verification with Sony/Microsoft/Nintendo | EU data storage (Frankfurt) chosen at org creation, DPA available, `SendDefaultPii` off by default, `Enabled` flag to start only after consent | Best overall; Remco already knows it |
| Unity Diagnostics (new, Unity 6.2+; replaces deprecated Cloud Diagnostics) | Developer Data Framework support article lists iOS, Android, macOS, Windows Desktop; no consoles listed | Crashes, exceptions, ANRs, symbols | Bundled with the Unity plan; limits not published | Developer owns the data; can be disabled per platform / build profile / project; consent handling is the developer's job | Zero setup for Steam, but no console path and few integrations |
| Backtrace (Sauce Labs; was sold by Unity as Cloud Diagnostics Advanced, now deprecated there) | PC, mobile, Xbox, PlayStation, Switch | Strong native/minidump focus, the console industry standard | Per-seat yearly plans (official page: mobile-only from EUR 546/yr; console tier higher per third-party lists); a small free tier is reported but not confirmed | Configurable, EU hosting on request | Excellent but heavier and pricier than an indie needs now |
| Steam error reporting (`SteamAPI_WriteMiniDump`) | **Windows 32-bit only** | minidumps to the partner site | free | via Steam | Not usable: the Unity build is 64-bit |

Sources: <https://docs.sentry.io/platforms/unity/>, <https://docs.sentry.io/platforms/unity/game-consoles/>, <https://docs.sentry.io/platforms/unity/configuration/options/>, <https://github.com/getsentry/sentry-unity/releases> (latest 4.13.0, 13 Nov), <https://sentry.io/pricing/>, <https://docs.sentry.io/organization/data-storage-location/>, <https://sentry.io/trust/privacy/gdpr-best-practices/>, <https://docs.unity.com/en-us/cloud-diagnostics/migration>, <https://docs.unity.com/en-us/cloud/developer-data-framework/project-diagnostics>, <https://support.unity.com/hc/en-us/articles/41257212347156-Which-platforms-are-supported-by-the-Developer-Data-Framework>, <https://discussions.unity.com/t/deprecation-announcement-cloud-diagnostics-advanced-cda/1488742/1>, <https://unity.com/products/backtrace>, <https://partner.steamgames.com/doc/features/error_reporting>.

### Recommendation: Sentry Unity SDK

Free tier is enough for a Steam playtest/early release, one tool covers PC now and consoles later (once a console dev account exists and the Enterprise plan is worth it), and Remco already runs Sentry for web apps. What it takes:

1. **Package** (new package: needs Remco's OK per CLAUDE.md): UPM git URL `https://github.com/getsentry/unity.git#4.13.0` (pin the tag). Create a new Sentry organisation in the **EU region** (cannot be moved later); in the project settings turn on "Prevent storing of IP addresses" and data scrubbing.
2. **Code behind the platform rule**: an `ICrashReporter` interface in `Core` (or `Platform`), `SentryCrashReporter` in `Platform`, `NullCrashReporter` as default; register it in `Services`. Set `Enabled = false` in the Sentry options asset so nothing starts automatically; the bootstrapper calls `SentrySdk.Init` only after consent. Keep the existing `CrashLog` (works offline, no consent needed).
3. **Consent screen**: one first-launch question ("Send anonymous crash reports? Yes / No", gamepad-friendly) plus a toggle under Options, stored in `settings.json`. `SendDefaultPii = false`, `AttachScreenshot = false`, a `SetBeforeSend` that strips the Windows user name from file paths in stack traces. Add a crash-reporting paragraph to the privacy policy (Steam requires one anyway, see PUBLISH_TODO).
4. **DSN and config per platform**: DSN, environment (`dev`, `playtest`, `release`) and `Release = Application.version` via a Sentry options-configuration script that reads the build profile, so the Switch/console builds can get their own Sentry project or be disabled until certified. The DSN is not a secret (it only allows sending), but the **sentry-cli auth token** for symbol upload is: pass it as an environment variable to `BuildScript`, never commit it.
5. **Later, consoles**: Enterprise plan, middleware verification (Microsoft GDK, PlayStation Partners, Nintendo CRPORTAL forwarding), private console SDK repos from Sentry, `TemporaryStorageSize` in the Switch `.nmeta`. Note: on Switch, IL2CPP line numbers are not available.
6. **Now, regardless of choice**: decide on `m_EngineDiagnosticsEnabled` and `submitAnalytics` (Unity engine diagnostics / hardware stats) so the privacy policy matches what the build actually sends.
