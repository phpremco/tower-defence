using System.Collections;
using System.IO;
using System.Text;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using Game.Platform;
using Game.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Round 72: the scripted smoke test of a player build (<c>-autotest</c>, see <see cref="SmokeTestGate"/>). It walks
    /// the real flow: skip the opening cutscene with key presses, main menu, options (display, controls), credits, the
    /// map, level 1 on the first difficulty behind the loading screen, build a tower, measure the frame time, pause, quit
    /// to the map and back to the menu; then the same level on the Low quality profile; then Quit. Keys go through a
    /// virtual keyboard and the Input System (pause, cancel, cutscene skip); menu buttons are found by their localised
    /// label and pressed with a submit event, as Enter or the south button would. The map's globe is not steered: the
    /// level starts with the call the map makes. Every step logs "[AutoTest]"; a summary goes to autotest.txt next to
    /// the saves. Never part of a release build: the gate needs a development or smoke build.
    /// </summary>
    public sealed class SmokeTestDriver : MonoBehaviour
    {
        private const string LogPrefix = "[AutoTest] ";
        private const string SummaryFileName = "autotest.txt";
        private const float StepTimeout = 90f;
        private const float MeasureSeconds = 30f;
        private const int SampleCapacity = 30000;
        private const float PressGap = 0.4f;
        private const float Settle = 1.5f;
        private const int MaxBackPresses = 12;
        private const float MaxRunSeconds = 360f;
        private const float StoryPressGap = 0.8f;

        private static bool _started;

        private readonly StringBuilder _summary = new StringBuilder();
        private readonly FrameTimeSampler _sampler = new FrameTimeSampler(SampleCapacity);
        private Keyboard _keyboard;
        private int _exceptions;
        private int _errors;
        private int _failures;
        private bool _crashProbe;
        private bool _finishing;

        /// <summary>Starts the driver once per process when the gate allows it.</summary>
        public static void StartIfRequested()
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            if (_started || !SmokeTestGate.Resolve(arguments, Application.isEditor, Debug.isDebugBuild, SmokeTestGate.SmokeBuild))
            {
                return;
            }

            _started = true;
            var go = new GameObject("SmokeTestDriver");
            DontDestroyOnLoad(go);
            var driver = go.AddComponent<SmokeTestDriver>();
            driver._crashProbe = SmokeTestGate.HasArgument(arguments, SmokeTestGate.CrashProbeArgument);
        }

        private void Awake()
        {
            Application.logMessageReceived += CountProblems;
            // The player may start without the window focus; virtual key presses must still arrive.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>("AutoTestKeyboard");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= CountProblems;
        }

        private void CountProblems(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                _exceptions++;
            }
            else if (type == LogType.Error || type == LogType.Assert)
            {
                _errors++;
            }
        }

        private IEnumerator Start()
        {
            Note($"start: version {BuildVersion.Current}, data {Application.persistentDataPath}");
            yield return WaitForScene(s => s != SceneNames.Boot, "leave Boot");
            if (_crashProbe)
            {
                yield return RunCrashProbe();
                yield break;
            }

            if (SceneManager.GetActiveScene().name == SceneNames.Intro)
            {
                yield return SkipIntro();
            }

            if (SceneManager.GetActiveScene().name == SceneNames.Level)
            {
                Note("the intro handed over to level 1 (first launch); leaving it for the menu");
                yield return WaitForLevel();
                yield return QuitLevelToMap();
                yield return BackToMenu();
            }

            yield return WaitForScene(s => s == SceneNames.MainMenu, "main menu");
            yield return new WaitForSecondsRealtime(Settle);
            yield return VisitOptions();
            yield return VisitCredits();
            yield return PlayLevel(false);
            yield return PlayLevel(true);
            yield return Finish();
        }

        // The player must never linger: the first failed check ends the route, and so does the overall time limit. Either
        // way the summary is written and the player quits by itself.
        private void Update()
        {
            if (_finishing)
            {
                return;
            }

            bool overTime = Time.realtimeSinceStartup > MaxRunSeconds;
            if (_failures > 0 || overTime)
            {
                Note(overTime ? $"watchdog: the route took longer than {MaxRunSeconds:0} s, quitting" : "a check failed, quitting");
                StopAllCoroutines();
                StartCoroutine(Finish());
            }
        }

        // ---- the route --------------------------------------------------------------------------------------------

        private IEnumerator SkipIntro()
        {
            float until = Time.realtimeSinceStartup + StepTimeout;
            int presses = 0;
            while (SceneManager.GetActiveScene().name == SceneNames.Intro && Time.realtimeSinceStartup < until)
            {
                yield return Press(Key.Escape);
                presses++;
                yield return new WaitForSecondsRealtime(PressGap);
            }

            Check(SceneManager.GetActiveScene().name != SceneNames.Intro, $"intro skipped with {presses} Esc presses -> {SceneManager.GetActiveScene().name}");
        }

        private IEnumerator VisitOptions()
        {
            yield return Submit("menu.options");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Submit("options.display");
            yield return new WaitForSecondsRealtime(Settle);
            Check(FindButton("display.apply") != null || FindLabelled("display.title"), "display and graphics page open");
            yield return Press(Key.Escape);
            yield return new WaitForSecondsRealtime(Settle);
            yield return Submit("options.controls");
            yield return new WaitForSecondsRealtime(Settle);
            Check(FindLabelled("controls.title") || FindButton("controls.reset") != null, "controls page open");
            yield return Press(Key.Escape);
            yield return new WaitForSecondsRealtime(Settle);
            yield return Press(Key.Escape);
            yield return new WaitForSecondsRealtime(Settle);
            Check(FindButton("menu.start") != null, "options closed, back on the main menu");
        }

        private IEnumerator VisitCredits()
        {
            yield return Submit("menu.credits");
            yield return new WaitForSecondsRealtime(Settle);
            var credits = FindAnyObjectByType<CreditsView>();
            Check(credits != null && credits.IsOpen, "credits open");
            if (credits != null)
            {
                credits.ScrollTo(0f);
                yield return new WaitForSecondsRealtime(Settle);
                Check(credits.ScrollPosition < 0.01f, "credits scrolled to the end");
            }

            yield return Press(Key.Escape);
            yield return new WaitForSecondsRealtime(Settle);
            Check(FindButton("menu.start") != null, "credits closed, back on the main menu");
        }

        private IEnumerator PlayLevel(bool lowQuality)
        {
            string profile = lowQuality ? "Low" : "High";
            if (Services.TryGet(out QualityProfileSwitch quality))
            {
                quality.Set(lowQuality);
            }

            Note($"quality profile {profile}");
            yield return Submit("menu.start");
            yield return WaitForScene(s => s == SceneNames.WorldMap, "world map");
            yield return new WaitForSecondsRealtime(Settle * 2f);

            var campaign = Services.Get<CampaignCatalog>();
            LevelDefinition level = CutsceneDefinition.FirstCampaignLevel(campaign);
            DifficultyProfile difficulty = campaign.Difficulties[0];
            LevelLoader.Begin(level, difficulty, new PathObjectDefinition[0]);
            Check(LevelLoader.Busy, $"loading screen up for {level.name} / {difficulty.name}");
            yield return WaitForLevel();
            yield return BuildTower();
            yield return Measure(profile);
            yield return QuitLevelToMap();
            yield return BackToMenu();
        }

        private IEnumerator WaitForLevel()
        {
            float started = Time.realtimeSinceStartup;
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneNames.Level && !LevelLoader.Busy && Services.TryGet(out LevelContext _), "level built");
            Note($"level ready after {Time.realtimeSinceStartup - started:0.0} s");
            yield return new WaitForSecondsRealtime(Settle);

            // The briefing card holds the clock until Deploy; the story lines after it skip with Cancel.
            if (FindButton("briefing.deploy") != null)
            {
                yield return Submit("briefing.deploy");
            }

            yield return new WaitForSecondsRealtime(Settle);
            yield return EnsureRunning("after Deploy");
        }

        // A conversation can open a moment after Deploy or after the first tower and holds the clock; the first runs
        // measured a frozen level because they looked too early. Cancel skips a conversation, P resumes when a stray
        // Cancel opened the pause menu.
        private IEnumerator EnsureRunning(string when)
        {
            LevelContext context = Services.Get<LevelContext>();
            float until = Time.realtimeSinceStartup + StepTimeout;
            int presses = 0;
            while (!Running(context) && Time.realtimeSinceStartup < until)
            {
                yield return Press(NextKey(context));
                presses++;
                yield return new WaitForSecondsRealtime(StoryPressGap);
            }

            Check(Running(context), $"level running {when} after {presses} presses (story busy {context.StoryBusy}, paused {context.Paused}, held {context.Held}, time scale {Time.timeScale:0.##})");
        }

        private static bool Running(LevelContext context) => !context.StoryBusy && !context.Paused && !context.Held && Time.timeScale > 0f;

        private static Key NextKey(LevelContext context) => context.Paused ? Key.P : Key.Escape;

        private IEnumerator BuildTower()
        {
            LevelContext context = Services.Get<LevelContext>();
            var placement = FindAnyObjectByType<PlacementController>();
            TowerDefinition affordable = null;
            for (int i = 0; i < context.AvailableTowers.Count && affordable == null; i++)
            {
                if (context.Wallet.CanAfford(context.AvailableTowers[i].BuildCost))
                {
                    affordable = context.AvailableTowers[i];
                }
            }

            int before = context.Wallet.Money;
            Vector2Int cell = default;
            bool built = placement != null && affordable != null && placement.TryBuildNearCentre(affordable, out cell);
            Check(built, $"tower {(affordable != null ? affordable.name : "none")} built at {cell}, money {before} -> {context.Wallet.Money}");
            yield return new WaitForSecondsRealtime(Settle);
            yield return EnsureRunning("after the tower");
        }

        // Uncapped for the measurement (v-sync off, no frame cap), so the number is the frame cost, not the monitor.
        private IEnumerator Measure(string profile)
        {
            int vSync = QualitySettings.vSyncCount;
            int target = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            yield return new WaitForSecondsRealtime(Settle);
            LevelContext context = Services.Get<LevelContext>();
            _sampler.Clear();
            int frames = 0;
            int frozen = 0;
            float nextPress = 0f;
            float until = Time.realtimeSinceStartup + MeasureSeconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                _sampler.Add(Time.unscaledDeltaTime);
                frames++;
                // A late conversation must not freeze the level being measured: skip it as soon as it holds the clock.
                if (!Running(context))
                {
                    frozen++;
                    if (Time.realtimeSinceStartup >= nextPress)
                    {
                        nextPress = Time.realtimeSinceStartup + StoryPressGap;
                        StartCoroutine(Press(NextKey(context)));
                    }
                }
            }

            QualitySettings.vSyncCount = vSync;
            Application.targetFrameRate = target;
            Note($"FPS {profile} at {Screen.width}x{Screen.height}: {_sampler.Summary()} (level running {100f * (frames - frozen) / Mathf.Max(1, frames):0}% of the frames, wave {context.WaveNumber} of {context.WaveCount}, targets alive {context.Targets.Count}, time scale {Time.timeScale:0.##})");
        }

        private IEnumerator QuitLevelToMap()
        {
            LevelContext context = Services.Get<LevelContext>();
            yield return Press(Key.P);
            yield return new WaitForSecondsRealtime(Settle);
            Check(context.Paused, "paused with P");
            yield return Submit("pause.quit_to_map");
            yield return new WaitForSecondsRealtime(Settle * 0.5f);
            yield return Submit("pause.quit_yes");
            yield return WaitForScene(s => s == SceneNames.WorldMap, "quit to the map");
        }

        private IEnumerator BackToMenu()
        {
            yield return new WaitForSecondsRealtime(Settle);
            for (int i = 0; i < MaxBackPresses && SceneManager.GetActiveScene().name != SceneNames.MainMenu; i++)
            {
                yield return Press(Key.Escape);
                yield return new WaitForSecondsRealtime(Settle);
            }

            Check(SceneManager.GetActiveScene().name == SceneNames.MainMenu, "back on the main menu with Esc");
            yield return new WaitForSecondsRealtime(Settle);
        }

        private IEnumerator RunCrashProbe()
        {
            yield return WaitForScene(s => s == SceneNames.MainMenu || s == SceneNames.Intro, "menu or intro");
            yield return new WaitForSecondsRealtime(Settle);
            Debug.LogException(new System.InvalidOperationException("AutoTest crash-log probe: a deliberate exception to prove crash.log writes in this build."));
            yield return new WaitForSecondsRealtime(Settle);
            string path = Path.Combine(Application.persistentDataPath, CrashLog.FileName);
            Check(File.Exists(path) && File.ReadAllText(path).Contains("crash-log probe"), $"crash.log written at {path}");
            yield return Finish();
        }

        private IEnumerator Finish()
        {
            _finishing = true;
            string crashPath = Path.Combine(Application.persistentDataPath, CrashLog.FileName);
            Note($"done: {_failures} failed checks, {_exceptions} exceptions, {_errors} errors logged, crash.log {(File.Exists(crashPath) ? "present" : "absent")}");
            WriteSummary();
            InputSystem.RemoveDevice(_keyboard);
            yield return new WaitForSecondsRealtime(Settle);
            Application.Quit();
        }

        // ---- helpers ----------------------------------------------------------------------------------------------

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        // Selects the button and sends it a submit event, the same path as Enter or the south button on a focused button.
        private IEnumerator Submit(string labelKey)
        {
            Button button = null;
            float until = Time.realtimeSinceStartup + StepTimeout * 0.1f;
            while ((button = FindButton(labelKey)) == null && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            if (!Check(button != null, $"button '{Loc.Get(labelKey)}'"))
            {
                yield break;
            }

            EventSystem.current.SetSelectedGameObject(button.gameObject);
            yield return null;
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return null;
        }

        private static Button FindButton(string labelKey)
        {
            string label = Loc.Get(labelKey);
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            for (int i = 0; i < buttons.Length; i++)
            {
                Text text = buttons[i].GetComponentInChildren<Text>();
                if (buttons[i].IsInteractable() && text != null && string.Equals(text.text.Trim(), label, System.StringComparison.OrdinalIgnoreCase))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static bool FindLabelled(string labelKey)
        {
            string label = Loc.Get(labelKey);
            Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Exclude);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].text.Contains(label))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator WaitForScene(System.Func<string, bool> wanted, string what)
        {
            yield return WaitUntil(() => wanted(SceneManager.GetActiveScene().name), what);
        }

        private IEnumerator WaitUntil(System.Func<bool> condition, string what)
        {
            float until = Time.realtimeSinceStartup + StepTimeout;
            while (!condition() && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Check(condition(), what);
        }

        private bool Check(bool ok, string what)
        {
            if (!ok)
            {
                _failures++;
            }

            Note((ok ? "ok   " : "FAIL ") + what);
            return ok;
        }

        private void Note(string line)
        {
            string stamped = $"{Time.realtimeSinceStartup,7:0.0}s {line}";
            _summary.AppendLine(stamped);
            Debug.Log(LogPrefix + stamped);
        }

        private void WriteSummary()
        {
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, SummaryFileName), _summary.ToString());
            }
            catch (IOException exception)
            {
                Debug.LogWarning(LogPrefix + "summary not written: " + exception.Message);
            }
        }
    }
}
