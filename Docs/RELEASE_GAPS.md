# Release gaps

Written 2026-11-15 from a read-only review of the code, project settings and docs. Balance and visual polish are tracked elsewhere and are left out. Items already parked in `Docs/PUBLISH_TODO.md` are referenced as **(PT)** and not repeated in detail.

How this was checked: by reading code and settings, not by running the game. Nothing here was tested on a real gamepad, a Steam Deck or a console. Store and console requirements (section 12 and 13) come from general knowledge, not from a fresh lookup: check the current terms with each platform before acting on them.

Legend:

- **Status:** missing / partial / done
- **Effort:** S (up to a day), M (a few days), L (a week or more)
- **Priority:** DEMO (needed for a Steam demo), STEAM (needed for the Steam release), CONSOLE (needed for Switch or another console), NICE (nice to have)

## Summary

**The foundation is in better shape than the feature list suggests.** The platform seams exist and are used: one input provider, one save service, one achievement service and one platform-settings object, all registered in a single place (`Boot/Bootstrapper.cs`). Device hot-swap, prompt names per device, pause on focus loss and on controller disconnect, a safe-area frame with a player margin, save versioning with migrations, pooling, and a performance test with a Low profile are all built.

**What is missing is almost everything the player sees as "settings", and everything that touches a store.**

| Area | State |
| --- | --- |
| Options menu | Only audio, camera, radar and HUD margin. No display, graphics, language or accessibility settings. |
| Controls | Hot-swap and prompts work. No remapping, no PlayStation names, text labels instead of button icons, one hardcoded controls page. |
| Gamepad navigation | Every screen has focus handling in code. Never verified on hardware; the radar click is mouse-only. |
| Saves | Versioned and migrated. Not crash-safe: a corrupt file throws at boot. One slot, no reset, no cloud. |
| Localisation | Nothing. All text is English literals in code and generated assets. The font has no Cyrillic or CJK. |
| Pause and suspend | Done for PC. Console suspend/resume untested. |
| Performance | Fine on PC (about 8 ms). 1,100 to 1,900 draw calls and 2.3 to 3.2 million triangles on the Low profile is too heavy for a Switch 1. |
| Builds | One Windows Mono build script. No IL2CPP build has ever been made. Platform is picked by a checkbox, not by build target. |
| Stores | No Steamworks, no Epic, no console SDK. Achievements are a 40-item proposal with a null service. |
| Legal | Credits file exists; one audio file has no known licence; AI-generated music needs a disclosure decision. |
| Promised features | Tower upgrade paths (A/B at level 3) and the endless mode are not built. Only continent 1 of 6 has levels. |

### Suggested order

1. **Decide the identity now** (company name, product name): it is in the save path, so it gets more expensive every week (PT).
2. **Make saves crash-safe** (1.1, 1.2) and split settings from progress (1.4). Small, and it protects every playtester.
3. **Make an IL2CPP Windows build and run the PlayMode smoke test against it** (8.1). This is the cheapest way to find AOT and stripping problems while the code base is still moving.
4. **Options menu, PC part** (2.1 to 2.4): display mode, resolution, v-sync, quality profile, screen shake. A demo without these gets negative reviews.
5. **Hardware gamepad pass on a Steam Deck or a pad** (4.1), then fix what it finds. Add PlayStation names and button icons (3.2, 3.3).
6. **Demo scope and build** (14.1): which levels, a demo save that carries over, a release build without developer tools (8.4).
7. **Steamworks**: app, SteamPipe upload, Auto-Cloud, achievements, overlay pause (PT plus 10.x).
8. **Localisation plumbing before more text is written** (6.1). Every week of new dialogue makes this bigger. Translation itself can wait.
9. **Remapping and accessibility** (3.1, 5.x) before the full Steam release.
10. **Console**: apply to the Nintendo developer programme early (it is slow and free to ask), then do the Switch performance work (7.x) and the platform services (13.x) once a devkit exists.

---

## 1. Saves

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 1.1 | **A corrupt or half-written save stops the game at boot.** `TryLoad` calls `JsonUtility.FromJson` with no try/catch; `ProgressService` is constructed in `Bootstrapper.Awake`, so the exception leaves the Boot scene with no services. | missing | `Platform/FileSaveService.cs:33`, `Economy/ProgressService.cs:17` | S | DEMO |
| 1.2 | Atomic write (temp file, replace, `.bak` fallback). Today `File.WriteAllText` writes straight over the only copy. (PT) | missing | `Platform/FileSaveService.cs:21` | S | DEMO |
| 1.3 | Save version and migrations. | done | `Economy/ProgressMigrator.cs` (two steps), `ProgressData.Version` | - | - |
| 1.4 | **Settings live in the progress file.** Volumes, camera speed, radar and HUD margin are fields of `ProgressData`. A "new game" would wipe them, and a cloud save would copy one machine's settings to another. Split into `settings.json` (local) and `progress.json` (cloud). | missing | `Economy/ProgressData.cs:14-35` | S | STEAM |
| 1.5 | **Every slider step writes the save file.** Each setter in `ProgressService` calls `Save()`. Harmless on PC; consoles limit how often a game may write, so settings should save when the page closes. | partial | `Economy/ProgressService.cs:185-270` | S | CONSOLE |
| 1.6 | New game / reset progress. The only way today is deleting the file by hand (`Docs/COMMANDS.md:113`). | missing | no reset in `ProgressService`, no button in `OptionsOverlay` | S | STEAM |
| 1.7 | Multiple save slots. One fixed key, `"progress"`. Not required by any store; on consoles saves are per user account anyway. A design choice for Remco. | missing | `ProgressService.cs:10` | M | NICE |
| 1.8 | Mid-level save and resume. Quitting a level loses it (the pause menu warns about this). Levels are long by design, so this matters most on a handheld that gets put to sleep; sleep itself is covered by suspend. | missing | `UI/PauseMenuView.cs` ("Progress in this level will be lost.") | L | NICE |
| 1.9 | Steam Cloud (Auto-Cloud), Switch save implementation. (PT) | missing | only `FileSaveService` exists | S / M | STEAM / CONSOLE |
| 1.10 | `ISaveService` is synchronous and has no delete or error result. Fine for files; console save APIs can fail (storage full, user signed out) and need a way to report that. | partial | `Platform/ISaveService.cs` | S | CONSOLE |

## 2. Options menu

What exists: master, music, effects and ambience sliders; camera speed; edge scrolling; radar on/off and size; HUD margin; a controls page; a sound test; replay intro (`UI/OptionsOverlay.cs`, `VolumeControls.cs`, `CameraOptionControls.cs`, `HudMarginControl.cs`). The pause menu has only Sound and Camera pages.

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 2.1 | Display mode (full screen, borderless, windowed) and resolution on PC. No runtime code sets either; the build script fixes borderless at desktop resolution and the window is not resizable. (PT) | missing | no `Screen.SetResolution` anywhere; `Editor/BuildScript.cs:29`; `ProjectSettings.asset` `resizableWindow: 0` | M | DEMO |
| 2.2 | V-sync and frame-rate cap. Nothing sets `vSyncCount` or `targetFrameRate`, so a fast PC runs unlimited and laptops get hot. | missing | grep finds neither | S | DEMO |
| 2.3 | **Quality profile choice.** The Low profile exists but is a checkbox on the Boot scene (`_lowQuality: 0`), not a player option and not chosen from the build target. PC players on weak hardware and Steam Deck users cannot reach it. | partial | `Boot/Bootstrapper.cs:31,53`, `Scenes/Boot.unity:157` | M | DEMO |
| 2.4 | Individual graphics switches (bloom, depth of field, mist, particles, shadows). All are read-only properties of the two profile classes. | missing | `Platform/PcPlatformSettings.cs`, `SwitchPlatformSettings.cs` | M | STEAM |
| 2.5 | Full options inside the pause menu. Only Sound and Camera are reachable mid-level. | partial | `UI/PauseMenuView.cs` | S | STEAM |
| 2.6 | Language selector. See section 6. | missing | - | S (once 6.1 exists) | STEAM |
| 2.7 | Volume sliders. | done | `UI/VolumeControls.cs`, `Core/AudioMix.cs` | - | - |
| 2.8 | HUD safe margin option. | done | `UI/HudMarginControl.cs`, `UI/SafeFrame.cs` | - | - |

## 3. Controls: remapping and prompts

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 3.1 | Key and button remapping. No rebinding code and no saved overrides. The prompt cache also assumes bindings never change (`InputSystemProvider` comment on `_prompts`), so it needs a reset when remapping arrives. (PT) | missing | no `PerformInteractiveRebinding` or `SaveBindingOverridesAsJson` in the project | M | STEAM |
| 3.2 | **PlayStation names.** `PadLayout` has Xbox and Nintendo only, and the layout comes from the platform, not from the pad in the player's hands. A DualSense on PC shows A/B/X/Y. | missing | `Platform/PadLayout.cs`, `Settings/Input/Glyphs_*.asset` (Xbox, Nintendo, Keyboard) | S | STEAM |
| 3.3 | **Button icons instead of text.** Prompts are strings ("A", "LT", "Esc"). Console platforms expect their own button artwork, and icons read faster. Needs a sprite per control per layout and a way to draw one inline. | missing | `Platform/ButtonGlyph.cs` holds a label string only | M | STEAM |
| 3.4 | Hot-swap between keyboard and pad. | done | `Platform/ActiveDeviceTracker.cs`, `IInputProvider.ActiveDeviceChanged`; used by `HeroQuickBar`, `HeroAbilityHud`, `DialogueView`, `CutsceneOverlay`, `WorldMapScreen` | - | - |
| 3.5 | **The controls page is a hardcoded table with Xbox names.** It prints "A", "B", "LB / RB", "View", "Start" on every platform and would be wrong on a Switch and after remapping. It should be built from the bindings. | partial | `UI/OptionsOverlay.cs:46-65` (`ControlRows`) | S | STEAM |
| 3.6 | Confirm and cancel on Nintendo. Confirm is bound to the south button, which is B on a Switch; players there expect A (east) to confirm. Needs a per-layout swap. A decision for Remco, then small. | missing | `GameInput.inputactions` (Confirm = `buttonSouth`), `Glyphs_Nintendo.asset` | S | CONSOLE |
| 3.7 | Two actions share the Y button (game speed and Codex). It works because the Codex only listens during a scan hint, but it will confuse a remapping screen. | partial | `GameInput.inputactions` (Speed and Codex both `buttonNorth`) | S | NICE |
| 3.8 | No control schemes in the action asset. Not needed today; the remapping UI and Steam Input both work better with a Gamepad and a Keyboard&Mouse scheme. | missing | `GameInput.inputactions` (`controlSchemes: []`) | S | STEAM |
| 3.9 | Controller rumble. None. | missing | no `SetMotorSpeeds` in the project | S | NICE |
| 3.10 | Left-handed or one-handed layouts, hold-versus-toggle for the overview. Covered by remapping for most players. | missing | - | S | NICE |

## 4. Gamepad navigation

In code every screen that builds buttons also sets or keeps a focus (`UiKit.Select`, `UiKit.KeepFocus`), and round 32 of `Docs/STATUS.md` records a full pad and keyboard pass with a simulated pad plus fixes. What remains:

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 4.1 | **A pass on real hardware.** All pad testing so far used a virtual pad driven by scripts (`STATUS.md` round 32, line 136 "Not verified on hardware: gamepad"). Stick dead zones, repeat rates, trigger thresholds and focus loss after a mouse touch only show on a real pad. | missing | `Docs/STATUS.md:136, 2525, 4717` | S (Remco plays, an agent fixes) | DEMO |
| 4.2 | Radar click (send the camera to a spot) is mouse-only. The controls page says so ("Radar: -"). The held overview covers the same need on a pad, so this is acceptable if it is a deliberate choice. | partial | `UI/RadarView.cs:150` (`IPointerClickHandler` only) | - | NICE |
| 4.3 | HUD speed, pause and Codex icons take no focus (`Navigation.Mode.None`). Each has a pad button instead (Y, Start, pause menu), so nothing is unreachable. | done | `UI/HudView.cs:341,410` | - | - |
| 4.4 | Upgrade, repair and sell have keyboard keys (U, R, X) but no pad buttons; a pad opens the tower menu and picks an icon. Reachable, one step slower. | done | `GameInput.inputactions` | - | - |
| 4.5 | Open items from the round-32 list that are a design choice, not a bug: no direct pad route from the region view to the action row; the upgrade screen's detail panel follows A, not the focus; result-screen button order differs between victory and defeat; weak focus frame on dark buttons. Check which were closed since. | partial | `Docs/STATUS.md:4880-4895` | S | STEAM |
| 4.6 | Hover-only content. The drone cards describe the hovered card but also follow the focus; `SelectNotifier` fires on focus as well as hover. No hover-only UI found. | done | `UI/PointerHover.cs`, `UI/SelectNotifier.cs`, `UI/BaseMenuView.cs:327` | - | - |
| 4.7 | No Quit button on consoles. | done | `IPlatformSettings.CanQuit`, `UI/MainMenuScreen.cs:67` | - | - |

## 5. Accessibility

Nothing in this section exists yet. None of it is a hard store requirement on Steam; several are close to required on Xbox and are what reviewers look for.

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 5.1 | **Screen shake strength.** `CameraShaker` has no option; quakes, boss pulses and explosions all shake. Also the simplest motion-sickness setting to add. | missing | `Levels/CameraShaker.cs` reads no setting | S | DEMO |
| 5.2 | Flashes and photosensitivity. Bosses, lasers and the upgrade celebration flash. A "reduce flashes" switch, plus a check of the worst moments against the usual three-flashes-per-second guideline. | missing | no option; flashes in `Combat/ImpactFlash.cs`, `UI/UpgradeCelebration.cs` | M | STEAM |
| 5.3 | Text size. The UI is laid out for 1920x1080 and scaled; round 32 measured about 13 px labels at 720p and round 64 notes 19 px labels on the loading screen. No player setting, and best-fit shrinking goes down to 14. | missing | `UI/UiKit.cs:18-19, 41`; `Docs/STATUS.md:4877, 9756` | M | STEAM (Deck), CONSOLE |
| 5.4 | Colour-blind support. Enemy roles, tower ranges, valid and invalid build cells and difficulty marks lean on colour (the design asks for strong colour coding). Needs shapes or icons alongside colour, or alternative palettes. | missing | no option | M | STEAM |
| 5.5 | Subtitles. Dialogue is text only, with no voice acting, so story is already readable. Missing: captions for sound-only cues (boss alarm, quake rumble) and a dialogue text-speed or instant-text option. | partial | `UI/DialogueView.cs` (no speed setting) | S | NICE |
| 5.6 | Game speed and pause-to-plan. Speed steps and a full pause exist, which is a real accessibility plus for this genre. | done | `Levels/TimeController.cs` | - | - |
| 5.7 | Remapping (see 3.1) and hold-to-toggle. | missing | - | - | STEAM |

## 6. Localisation

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 6.1 | **No string table.** UI text is English literals passed to `UiKit.CreateButton/CreateText` (about 70 call sites in `Scripts/UI`, plus composed strings). Names and descriptions sit in ScriptableObjects (`_displayName`, `_description` in about 40 definitions). Story, cutscene, scan-hint, boss-intro and loading-tip text is written as literals in the generator (`Editor/ProjectGenerator.Story.Crystal.cs`, `.Cutscene.cs`, `.ScanHints.cs`, `.BossIntros.cs`) and baked into assets. Needs: a key per string, a table per language, a lookup service, and text that refreshes on a language change. Unity's Localization package would be a new package (ask first); a small own table is also enough for this amount of text. | missing | no "locali", "language" or "translate" in any runtime script | L | STEAM (if more than English ships) |
| 6.2 | **The font covers Latin, Thai and Vietnamese only.** Chakra Petch has no Cyrillic, Chinese, Japanese or Korean. Russian, Simplified Chinese and Japanese are among the biggest non-English audiences on Steam and Switch. Needs fallback fonts with matching weight. | missing | `Art/UI/Fonts/ChakraPetch-*.ttf` | M | STEAM (per language) |
| 6.3 | Two text systems. Most screens use legacy `UnityEngine.UI.Text`; dialogue and cutscenes use TextMeshPro. Font fallback, rich text and line breaking for Asian languages work differently in each, so a CJK release means moving the rest to TextMeshPro or maintaining two font setups. | partial | TMPro only in `UI/DialogueView.cs`, `UI/UiKit.cs`, `UI/Cutscene/*` | L | STEAM (CJK only) |
| 6.4 | Layout that survives longer text. Many labels have fixed pixel boxes and the controls page has fixed column positions; German and French run 20 to 35 % longer. Best-fit hides some of it by shrinking text, which makes 5.3 worse. | missing | `UI/OptionsOverlay.cs:40-41`, fixed sizes throughout `Scripts/UI` | M | STEAM (if translated) |
| 6.5 | Number and date formatting. Almost no culture-dependent formatting found in the UI (5 hits); low risk. | done | grep in `Scripts/UI` | - | - |
| 6.6 | Text baked into art (logo, any signs or labels painted in Blender). Check once the logo is final. | unknown | `ArtSource/` not reviewed | S | NICE |
| 6.7 | The dialogue voice is synthetic blips per character, not speech, so there is no voice-over to translate. | done | memory of audio decisions, `UI/DialogueVoice.cs` | - | - |

Decision for Remco: English only at launch is a valid choice for Steam. If so, 6.1 still pays off later, and the cheap part (keys instead of literals) is best done before continents 2 to 6 add their text.

## 7. Performance on Switch-class hardware

Known numbers (`Docs/STATUS.md:1569-1573, 1649`, from `Tests/PlayMode/ModelPerformanceTests.cs`: 60 towers, 8 drones, densest Crazy wave, in the editor on Remco's PC):

- Frame time 7 to 10 ms average, p95 up to 11 ms, on both profiles. The Low profile is barely faster, which means the PC is not GPU-bound and the test says little about a console.
- 1,100 to 1,900 draw calls, about 180 set-pass calls, 2.2 to 3.2 million triangles per frame on the Low profile.

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 7.1 | **Triangle and draw-call budget for Switch 1.** A common working budget there is a few hundred thousand triangles and a few hundred draw calls at 30 fps. The Low profile is five to ten times over. Needs real LODs or lower-poly creature and tower meshes, instancing for scenery, and probably a creature cap. Switch 2 and Steam Deck have far more room. | missing | numbers above; `SwitchPlatformSettings.LodBias 0.6`, `LowDetailModels` | L | CONSOLE |
| 7.2 | **No measurement on weak hardware.** Every number is from one desktop in the editor. A Steam Deck or an old laptop running a player build is the cheapest stand-in for a console. (PT) | missing | - | S | STEAM |
| 7.3 | Memory. Assets are 2.0 GB of art and 223 MB of audio on disk; two music loops are 30 MB of Vorbis each and were set to decompress on load until round 64. No memory budget or measurement exists; a Switch 1 leaves a game roughly 3 GB. | missing | `Docs/STATUS.md:9710` | M | CONSOLE |
| 7.4 | Runtime-generated meshes and textures. 22 runtime scripts create `Mesh` or `Texture2D` objects in code. Fine on PC; on a console it costs load time and memory and needs a check that each is created once and freed. | partial | grep `new Mesh(` / `new Texture2D(` outside `Editor/` | M | CONSOLE |
| 7.5 | Pooling for enemies, projectiles, death effects and boss effects. | done | `Levels/EnemySpawner.cs:533` (`ObjectPool<Enemy>` per definition), `Combat/ProjectilePool.cs`, `Enemies/EnemyDeathEffectPool.cs` | - | - |
| 7.6 | No LINQ in runtime code; `Find...` calls only at set-up time, not per frame. | done | grep: no `System.Linq` outside `Editor/`; 8 `FindAnyObjectByType` calls, all in initialisation | - | - |
| 7.7 | Per-frame allocations. Not measured. The performance test reports frame time and draw calls but not GC allocations. Add an allocation count to the same test. | missing | `Tests/PlayMode/ModelPerformanceTests.cs` | S | STEAM |
| 7.8 | Load times. A loading screen with async level build exists (round 64). Not measured on a hard disk or a console. | partial | `UI/LoadingScreen.cs` | S | CONSOLE |
| 7.9 | Frame-rate target per platform (30 on Switch 1, 60 elsewhere) and dynamic resolution. Not decided, not set. | missing | see 2.2 | S | CONSOLE |

## 8. Build pipeline and IL2CPP

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 8.1 | **No IL2CPP build has ever been made.** The Windows player uses the default Mono backend (`scriptingBackend` lists only Android; `Builds/Windows` contains `MonoBleedingEdge`). Consoles are IL2CPP only. The code looks AOT-friendly, but that is a reading, not a test. | missing | `ProjectSettings/ProjectSettings.asset:840`, `Builds/Windows/` | S to try, M if it finds problems | STEAM |
| 8.2 | Reflection and dynamic code. None found in runtime scripts: no `System.Reflection`, `Activator`, `Type.GetType`, `dynamic`, expression trees or `[SerializeReference]`. `Services` uses a `Dictionary<Type, object>` with generics over reference types only, which is AOT-safe. `JsonUtility` is AOT-safe. | done | grep over `Scripts/` excluding `Editor/` | - | - |
| 8.3 | Managed stripping. No stripping level set and no `link.xml`. The shader-variant keeper materials already guard the runtime-built materials. Test with the IL2CPP build in 8.1. | partial | `Bootstrapper._shaderKeepers`; no `link.xml` in `Assets/` | S | STEAM |
| 8.4 | **Release build configuration.** One build method, always the same options. Missing: a release variant (no development flag), a version stamp, and a check that developer switches are off. `PlaytestSettings.asset` currently has `_developerMode: 1`; `DeveloperMode.Resolve` ignores it outside development builds, which is the right guard, but the wave-skip key (N, View tap) and `InfiniteMoney` / `UnlockAllWeapons` have no such guard and rely on the asset values. | partial | `Editor/BuildScript.cs`, `Settings/PlaytestSettings.asset:15-17`, `Core/DeveloperMode.cs` | S | DEMO |
| 8.5 | **Platform chosen at build time.** `Bootstrapper` picks `SwitchPlatformSettings` from a serialized checkbox. It should come from the build target (or a define), together with the save, achievement and input implementations. | missing | `Boot/Bootstrapper.cs:53` | S | STEAM |
| 8.6 | Build targets beyond Windows: Linux or Steam Deck native (Proton usually suffices), macOS (needs notarisation and a Mac), demo build, console builds. (PT mentions SteamPipe.) | missing | `BuildScript` has `BuildWindows` only | S each for PC | STEAM / CONSOLE |
| 8.7 | Automated builds. No CI; builds run by hand with the editor closed. A script that builds, runs the PlayMode smoke test against the build and uploads to a Steam beta branch would remove a manual step from every playtest. | missing | no `.github/`, `Docs/COMMANDS.md:129-136` | M | NICE |
| 8.8 | Unity splash screen and licence. The splash screen is on. Console builds need Unity Pro seats and the console add-ons from the platform holder. Check what Remco's Unity plan allows. | unknown | `ProjectSettings.asset:20` | - | CONSOLE |
| 8.9 | Unused packages and modules. Visual Scripting, AI Navigation, Timeline, Collab Proxy and modules such as XR, vehicles, cloth and terrain are in the manifest. Visual Scripting in particular relies on reflection and adds build size; remove what nothing uses (ask first, per the project rules). | partial | `Packages/manifest.json` | S | STEAM |
| 8.10 | Repository size. `Assets/` is 2.3 GB, with LFS set up for models and textures. No action needed for release; a clean clone and build on a second machine has not been tried. | partial | `.gitattributes` | S | NICE |
| 8.11 | Company and product name, version number. (PT) Still `DefaultCompany` / `Tower defense` / `0.1.0`; the exe is `TowerDefense.exe`. | missing | `ProjectSettings.asset:15-16,152` | S | DEMO |

## 9. Pause, suspend and resume

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 9.1 | Pause when the window loses focus or the OS suspends the app. | done | `Levels/TimeController.cs:102-116` | - | - |
| 9.2 | Pause when the pad in use disconnects; resume stays manual. | done | `IInputProvider.DeviceLost`, `TimeController.cs:54` | - | - |
| 9.3 | **A "reconnect controller" message.** The game pauses, but nothing tells the player why or which pad to pick up; `DeviceRegained` has no listener. Consoles require a clear prompt. | partial | `DeviceRegained` is raised in `InputSystemProvider` and used nowhere else | S | CONSOLE |
| 9.4 | Interruptions outside a level. The pause logic lives in `TimeController`, which only exists in a level. Menus are static, so that is fine, but cutscenes and the intro keep playing (and keep their sound) when focus is lost. | partial | only `TimeController` implements `OnApplicationFocus` | S | STEAM |
| 9.5 | Steam overlay. Opening it does not always take focus away; pausing on the overlay callback needs Steamworks. (PT) | missing | - | S | STEAM |
| 9.6 | Audio on focus loss. `runInBackground` is on and nothing mutes audio when the window is in the background. | missing | `BuildScript.cs:31`; no `AudioListener.pause` | S | STEAM |
| 9.7 | Console suspend and resume, user sign-out, docking and undocking (resolution and safe-area change mid-game). `SafeAreaFitter` already follows resolution changes each frame. The rest cannot be tested without a devkit. | partial | `UI/SafeAreaFitter.cs` | M | CONSOLE |

## 10. Achievements, stats and crash reporting

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 10.1 | Achievement list. A 40-item proposal exists with trigger and tracking notes per item; waiting for Remco's decision. (PT) | partial | `Docs/ACHIEVEMENTS.md` | S | STEAM |
| 10.2 | Wiring. `IAchievementService.Unlock` is never called from anywhere; only `NullAchievementService` is registered. Needs an `AchievementDefinition` asset, a tracker that listens to `LevelContext` events, and the lifetime counters the proposal marks as "New". (PT) | missing | grep: `IAchievementService` appears only in `Bootstrapper.cs:60` and its own files | M | STEAM |
| 10.3 | The interface has only `Unlock(id)`. Progress achievements ("500 kills") need a stat or progress call, and platforms differ here (Steam stats, Xbox progress percent, PlayStation trophies, Epic stats). Extend the interface once, before the first implementation. | partial | `Platform/IAchievementService.cs` | S | STEAM |
| 10.4 | **Crash and error reporting.** None. Unity Cloud Diagnostics is switched off and no runtime code listens for unhandled exceptions. For a demo this is the main way to learn why it failed on someone else's machine. Options: Unity Cloud Diagnostics (needs a Unity project id), Sentry or Backtrace (new package, ask first), or at least a "copy the log" button and the player log path in the README. | missing | `ProjectSettings/UnityConnectSettings.asset` (`m_EnableCloudDiagnosticsReporting: 0`) | S to M | DEMO |
| 10.5 | Gameplay analytics (where players lose, which towers they buy). None. The balance model would benefit from real data, but it needs consent, a privacy policy and a decision. Steam's own stats give playtime and achievement rates for free. | missing | Unity Analytics disabled in the same file | M | NICE |
| 10.6 | In-game bug report or feedback link (opens the Steam discussion page or a form). | missing | - | S | NICE |
| 10.7 | Runtime logging. Only 6 `Debug.Log` calls in runtime code, so release logs stay clean. | done | grep | - | - |

## 11. Legal, credits and ratings

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 11.1 | **`warning.mp3` (the boss alarm) has no known source or licence.** The credits file says "free use assumed, check before release". Replace it or find the source before any public build. | missing | `Docs/CREDITS.md`, `Audio/Freesound/warning.mp3` | S | DEMO |
| 11.2 | **AI-generated content disclosure.** The music loops were generated with Gemini, sound effects were made with Claude, and the code and much of the art pipeline are AI-assisted. Steam's content survey asks about generative AI and shows the answer on the store page. Also read the Gemini terms for commercial use of generated audio. Console holders ask similar questions. | missing | `Docs/CREDITS.md` (rows "Music loops", "Game sound effects") | S | DEMO (store page) |
| 11.3 | In-game credits screen. None. The font licence (SIL OFL) asks that its text ships with the font; a credits screen plus a `Licenses` text in the build covers it. | missing | no credits screen in `Scripts/UI`; licence text at `Art/UI/License_Fonts.txt` | S | STEAM |
| 11.4 | Third-party list is otherwise in order: Chakra Petch (OFL), Kenney and Lentikula audio (CC0), one Pixabay clip. | done | `Docs/CREDITS.md` | - | - |
| 11.5 | MPFB / MakeHuman characters. Chosen as the base for humans (CC0 for exported models), but **not listed in `Docs/CREDITS.md`**. Add the row, and note which asset packs were used, because some community MakeHuman assets carry other licences. (PT mentions it.) | missing | `Docs/CREDITS.md` has no MPFB row | S | STEAM |
| 11.6 | Blender add-ons and any downloaded textures or HDRIs used in `ArtSource/`. Not reviewed here; list each with its licence. | unknown | - | S | STEAM |
| 11.7 | Trademark check on the name "Deepwake" and the company name, in the EU and US, before buying capsule art. | missing | `Docs/NAME_BRAINSTORM.md` | S | DEMO |
| 11.8 | Similarity to the inspiration game. The project rules already forbid copied names, numbers and designs; do one deliberate comparison pass on tower names, UI layout and level shapes before the store page goes up. | partial | `CLAUDE.md`, `Docs/INSPIRATION.md` | S | STEAM |
| 11.9 | Age rating. (PT) Steam does not need a formal rating for most countries; its content survey covers it. Console stores and Epic take an IARC rating from a free questionnaire in their portals. Expect a low-teen rating (fantasy violence against creatures). Germany, Korea and a few others have extra rules. | missing | - | S | STEAM / CONSOLE |
| 11.10 | Privacy policy and EULA. Needed as soon as any data leaves the machine (crash reports, analytics) and by console stores regardless. | missing | - | S | STEAM |
| 11.11 | Legal entity and tax. Selling on any store needs a business identity, bank details and tax forms (the US tax interview on Steam). The company name decision in PT is the first step. | missing | - | M (waiting time) | STEAM |

## 12. PC stores

General knowledge, to be checked against the current terms.

### Steam

| # | Item | Status | Effort | Priority |
| --- | --- | --- | --- | --- |
| 12.1 | Steamworks account, app fee, store page, SteamPipe upload, Auto-Cloud, achievements. (PT) Timing to plan for: a waiting period after paying the first fee, the store page must be public as "coming soon" for at least two weeks before release, and both the page and the build go through a review of a few working days. | missing | M | DEMO |
| 12.2 | Steamworks.NET (or Facepunch) behind the platform interfaces: a `SteamAchievementService`, overlay pause, and a graceful start when Steam is not running. New package, ask first. (PT) | missing | M | STEAM |
| 12.3 | Steam Input. With the Input System, pads work without it, but Steam Input gives correct glyphs for every pad and lets players remap. A decision: rely on own glyphs (3.2, 3.3) or use Steam Input's. (PT) | missing | M | STEAM |
| 12.4 | Steam Deck verification: readable text at 1280x800 (see 5.3), pad-only from first launch, correct glyphs, no launcher, default settings that hold the frame rate (see 2.3). (PT) | missing | M | STEAM |
| 12.5 | Demo as its own app, Next Fest entry (one festival per game, so pick the edition with care), wishlist build-up. See 14. | missing | M | DEMO |
| 12.6 | Rich presence, trading cards, cloud for settings. | missing | S | NICE |

### Epic Games Store

| # | Item | Status | Effort | Priority |
| --- | --- | --- | --- | --- |
| 12.7 | Self-publishing through the Epic Developer Portal: a submission fee per product, a store page with its own asset sizes, an IARC rating from the portal, and a build upload with Epic's tool. No hard technical integration is required for a single-player game. | missing | M | NICE |
| 12.8 | Epic Online Services for achievements and cloud saves. Optional, but if achievements ship on Steam, players expect them on Epic. This is a second `IAchievementService` and possibly a second save back end, which is what the interfaces are for. The Auto-Cloud approach on Steam has an equivalent (a cloud save folder set in the portal). | missing | M | NICE |
| 12.9 | The game must run without Steam present: no Steam calls outside the Steam implementations. True today (there are none); keep it true. | done | - | - |

Advice: ship Steam first. An Epic release is mostly store work plus one more service implementation and can follow without touching the game.

## 13. Consoles

General knowledge; each platform holder shares the real requirement lists only under NDA after registration.

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 13.1 | **Developer registration.** Nintendo, Sony and Microsoft each need an application (company details, a pitch, sometimes a build or video). Approval can take weeks to months and costs nothing to start. Microsoft's ID@Xbox and Nintendo's portal are the usual first steps for a solo developer. Devkits cost money (Switch) or are loaned (Xbox). | missing | - | S (plus waiting) | CONSOLE |
| 13.2 | Unity for consoles. Each console build target is a separate Unity add-on obtained through the platform holder and needs a paid Unity plan. Check the installed editor version (6000.6.5f1) against what each console SDK supports at that moment. | missing | `ProjectSettings/ProjectVersion.txt` | S | CONSOLE |
| 13.3 | Platform services per console: save (mount, commit, storage-full handling), achievements or trophies (PlayStation and Xbox require them; Switch has none), user account selection, and settings. The four interfaces are the right seam; `IPlatformSettings` exists for Switch only as a quality profile. | partial | `Platform/SwitchPlatformSettings.cs` is the only console file | L per platform | CONSOLE |
| 13.4 | **User and account handling.** Consoles tie saves and achievements to a signed-in user who can change or sign out while the game runs. Nothing in the code has a notion of "the current user". Xbox is the strictest here. | missing | no user concept in `Services` or `ISaveService` | M | CONSOLE |
| 13.5 | Certification themes that already apply (Nintendo lotcheck, Sony TRC, Xbox XR). In general terms they all test: no crash or hang, correct button names and icons for the platform, a prompt on controller disconnect, correct behaviour on suspend, resume and sign-out, save errors handled with clear messages, no writes too often, loading without long frozen screens, text readable in handheld and on a TV inside the safe area, no references to other platforms ("Esc", "click", Xbox letters on a Switch), no quit button, and the platform's own terminology in every message. | partial | covered: 4.7, 9.1, 9.2, 2.8; open: 1.5, 3.3, 3.5, 3.6, 5.3, 9.3, 13.4 | M | CONSOLE |
| 13.6 | **Text that names the wrong platform.** Besides the controls page (3.5), help and tip text mentions keys and mouse actions in literals ("Click a card", "Book icon (top right)"). Every such string needs a per-device variant. Do this together with 6.1. | missing | `UI/OptionsOverlay.cs:46-65`; check `LoadingTips`, scan hints, briefings | M | CONSOLE |
| 13.7 | Touch screen (Switch handheld). Not required. The mouse path through `Pointer` would mostly work if wanted. | missing | `GameInput.inputactions` (Point = `<Pointer>/position`) | M | NICE |
| 13.8 | Switch performance. See section 7; this is the largest single console task. | missing | - | L | CONSOLE |
| 13.9 | Console age ratings. Digital-only releases can use IARC on Nintendo and Xbox; check PlayStation's current rule per region. | missing | - | S | CONSOLE |
| 13.10 | A porting partner or publisher. Many solo developers hand the console versions to a porting studio; the clean platform seam makes that cheaper. A decision for later, worth knowing as an option. | - | - | - | NICE |

## 14. Demo, trailer and store presence

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 14.1 | **Demo scope.** Decide which levels (for example the intro plus the first three or four of Crystal Plains), which difficulties, and where it ends. In code: a data flag on the campaign node or a demo `CampaignCatalog`, an end-of-demo screen with a wishlist button, and locked markers on the map. Must be data, not `if (level == 4)`. | missing | `CampaignCatalog`, `UI/WorldMapScreen.cs` have no demo notion | M | DEMO |
| 14.2 | Demo save carries over to the full game. Cheap if the save path and format are shared; decide before the demo ships, because of the path issue in PT. | missing | - | S | DEMO |
| 14.3 | First-time experience. The intro cutscene on first launch, level briefings and Voss's scan hints exist. Missing for a demo: a short check that a new player learns placing, upgrading and the hero without reading the controls page. | partial | `Bootstrapper.Start`, `UI/BriefingView.cs`, scan hints | M | DEMO |
| 14.4 | Trailer capture. Needs: a way to hide the HUD, a free or scripted camera for beauty shots, and stable 60 fps capture. The cutscene system (`CutsceneDefinition`, shots with camera paths) could drive trailer shots. No HUD-hide switch exists. | missing | - | M | DEMO |
| 14.5 | Store assets: capsules in every required size, five or more screenshots, a logo on transparent background, a short and a long description. Branding is still open (logo retry pending per the notes). (PT) | missing | `Docs/NAME_BRAINSTORM.md` | M | DEMO |
| 14.6 | Press kit, a simple website or landing page, a community place (Steam discussions are enough to start). | missing | - | S | NICE |
| 14.7 | Playtest before the demo. Steam Playtest gives keys to strangers without touching the store page's reviews; it needs only 12.1 and a build. | missing | - | S | DEMO |

## 15. Promised in the design, not built

From `Docs/GAME_DESIGN.md`, `CLAUDE.md` and `Docs/STORY_ROADMAP.md`. Content volume and balance are out of scope here; these are features.

| # | Item | Status | Evidence | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 15.1 | **Two upgrade paths from tower level 3.** The design says the player picks path A or B. In the game the path is art only. | missing | `Docs/ACHIEVEMENTS.md` (TOWER_UPGRADE_PATH: "path choice is art only today"), `GAME_DESIGN.md` Torens | L | STEAM (or cut from the design) |
| 15.2 | **Endless mode.** Listed as an inspiration feature ("four difficulties and an endless mode") and asked about in this review. No code, and not in `GAME_DESIGN.md` as a decision. Needs a wave generator that scales without end, a score, and (optionally) leaderboards, which are a platform service of their own. | missing | no "endless" in runtime code | L | NICE (decide) |
| 15.3 | Continents 2 to 6. The globe shows six regions; only Crystal Plains has levels, the rest say "coming soon". Decide whether the first release is continent 1 only (early access or a smaller priced game) or the whole planet. This decision drives the store page, the price and the achievement list (which already names all six). | partial | `Docs/ACHIEVEMENTS.md` conventions, `GAME_DESIGN.md` Gebiedskiezer | L | STEAM (decide) |
| 15.4 | Show the next wave's enemies. A community wish noted in `CLAUDE.md`. The HUD shows a countdown and a boss warning, but no list of what the next wave contains. | missing | `UI/HudView.cs:550-624` (countdown only) | S | NICE |
| 15.5 | Test mode with infinite money and wave skip. | done | `Core/PlaytestSettings.cs`, `LevelContext.SkipWaveAllowed` | - | - |
| 15.6 | Open points still unticked in `GAME_DESIGN.md` (game name, hero behaviour, damaged-tower strength, path-object limits, weapon sources). Several were decided in practice; the list is stale and should be brought up to date so a later reader does not reopen them. | partial | `GAME_DESIGN.md:278-292` | S | NICE |
| 15.7 | Early access as a release form. Not mentioned anywhere; it fits a game with one finished continent out of six and an active balance loop. A decision for Remco, with consequences for 15.3 and the save-compatibility promise. | - | - | - | decide |

## Top ten

1. **Crash-safe saves** (1.1, 1.2): a corrupt file currently stops the game at boot. S, DEMO.
2. **Company and product name** (8.11, PT): it is in the save path. S, DEMO.
3. **First IL2CPP build plus smoke test** (8.1): never done; consoles depend on it. S to M, STEAM.
4. **Display, v-sync and quality options** (2.1 to 2.3): none exist. M, DEMO.
5. **Real-hardware gamepad pass** (4.1): all pad testing so far was simulated. S, DEMO.
6. **Unlicensed boss alarm and the AI-content disclosure** (11.1, 11.2). S, DEMO.
7. **Crash reporting** (10.4): nothing reports failures from other machines. S to M, DEMO.
8. **Localisation plumbing** (6.1, 6.2): no string table, and the font lacks Cyrillic and CJK. L, STEAM.
9. **Remapping, PlayStation names and button icons** (3.1 to 3.3, 3.5). M, STEAM.
10. **Switch render budget** (7.1): the Low profile draws five to ten times what a Switch 1 handles. L, CONSOLE.
