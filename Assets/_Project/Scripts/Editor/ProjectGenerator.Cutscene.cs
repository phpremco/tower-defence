using Game.Core;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the opening cutscene: its dialogues (English, faithful to Docs/STORY.md "Proloog" and
    /// "Openingscutscene"), the cast and the shots with their camera moves, cues, music and framing. Camera keys are
    /// written relative to anchors on the field (landing, base, drill, dig site, gate, entrance), so the same shots
    /// frame the same moments if the field level changes. Runs after the level story, whose speakers it uses.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string CutsceneFolder = DataRoot + "/Story/Cutscene";
        private const string OpeningCutscenePath = CutsceneFolder + "/Cutscene_Opening.asset";
        private const string CutsceneCastPath = CutsceneFolder + "/CutsceneCast.asset";
        private const string HandoffDifficultyName = "Easy";
        private const float HandoffFieldOfView = 14f;

        /// <summary>One shot as the generator writes it.</summary>
        private sealed class ShotSpec
        {
            public CutsceneSet Set;
            public CutsceneAction Action;
            public ShotTiming Timing = ShotTiming.Fixed;
            public float Seconds;
            public float CameraSeconds;
            public DialogueDefinition Dialogue;
            public float DialogueDelay;
            public CutsceneMusic Music = CutsceneMusic.Keep;
            public float FadeIn;
            public float FadeOut;
            public bool Letterbox = true;
            public CutsceneOverlayKind Overlay;
            public string Title = string.Empty;
            public CameraKey[] Keys;
            public CutsceneCue[] Cues = new CutsceneCue[0];
        }

        private static void GenerateCutscene(GeneratorContext context)
        {
            SpeakerDefinition ora = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Ora.asset");
            SpeakerDefinition mercer = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Mercer.asset");
            SpeakerDefinition voss = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Voss.asset");

            DialogueDefinition earth = CreateDialogue("Dlg_Cut_Prologue_Earth",
                Say(ora, "The year is 2053. Earth is empty. Lithium, cobalt, rare earth metals: the last mines have been dug down to bedrock."),
                Say(ora, "No raw materials means no batteries, no chips, no energy. Humanity's hunger for technology has outgrown what this planet can still give."));
            DialogueDefinition survey = CreateDialogue("Dlg_Cut_Prologue_Survey",
                Say(ora, "Yet there is hope. With AI, spaceflight advanced more in a few years than in the century before: new drives, self-building ships, telescopes that scan thousands of worlds at once."),
                Say(ora, "One world stands out: S-69-CRYS. Its soil is full of the metals we need, and everywhere lie crystals we cannot place. They give off a faint, constant energy, as if they never run dry."),
                Say(ora, "Breathable air, liquid water, a stable climate: every ingredient for life. Yet no cities, no lights, no signals. Nothing moves. A few strange heat sources below the surface were written off as volcanic activity."));
            DialogueDefinition orbit = CreateDialogue("Dlg_Cut_Orbit",
                Say(ora, "Nova Terra Industries sends the first expedition: Operation New Dawn. After three years in cryosleep, the mothership reaches orbit."),
                Say(ora, "The plan: build bases on several continents, mine the resources and ship them home. Field scout Cole Mercer, call sign Pathfinder, goes down first."),
                Say(ora, "All systems nominal. No life forms detected. Expected complications: none. Welcome to S-69-CRYS, Commander."));
            DialogueDefinition landing = CreateDialogue("Dlg_Cut_Landing",
                Say(mercer, "Pathfinder here. Ground's stable, crystals everywhere. This is our spot."));
            DialogueDefinition drill = CreateDialogue("Dlg_Cut_Drill",
                Say(ora, "Extraction started. Yield above expectations."));
            DialogueDefinition shiver = CreateDialogue("Dlg_Cut_Shiver",
                Say(mercer, "ORA... what is this?"));
            DialogueDefinition radar = CreateDialogue("Dlg_Cut_Radar",
                Say(ora, "Correction. Life forms detected. Many life forms."));
            DialogueDefinition gate = CreateDialogue("Dlg_Cut_Gate",
                Say(voss, "We can't go back. Earth is counting on us. Mercer, hold those gates!"),
                Say(ora, "Standard defence systems ready. AI turrets can be deployed immediately."));

            CutsceneCast cast = CreateCutsceneCast(context);
            ShotSpec[] shots = OpeningShots(earth, survey, orbit, landing, drill, shiver, radar, gate);
            var cutscene = LoadOrCreate<CutsceneDefinition>(OpeningCutscenePath);
            var so = new SerializedObject(cutscene);
            so.FindProperty("_cast").objectReferenceValue = cast;
            so.FindProperty("_handoff").intValue = (int)CutsceneHandoff.FirstCampaignLevel;
            so.FindProperty("_handoffDifficulty").objectReferenceValue = HandoffDifficulty(context.Difficulties);
            WriteShots(so.FindProperty("_shots"), shots);
            so.ApplyModifiedPropertiesWithoutUndo();

            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(StoryAssetPath);
            var storySo = new SerializedObject(story);
            storySo.FindProperty("_openingCutscene").objectReferenceValue = cutscene;
            storySo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cutscene);
            EditorUtility.SetDirty(story);
        }

        private static CutsceneCast CreateCutsceneCast(GeneratorContext context)
        {
            var cast = LoadOrCreate<CutsceneCast>(CutsceneCastPath);
            var so = new SerializedObject(cast);
            so.FindProperty("_hero").objectReferenceValue = context.Hero;
            so.FindProperty("_hauler").objectReferenceValue = context.HarvestDrone;
            so.FindProperty("_builder").objectReferenceValue = context.RepairDrone;
            SerializedProperty swarm = so.FindProperty("_swarm");
            var crawlers = new[] { context.Bestiary.Skitter, context.Bestiary.Prowler, context.Bestiary.Roller };
            swarm.arraySize = crawlers.Length;
            for (int i = 0; i < crawlers.Length; i++)
            {
                swarm.GetArrayElementAtIndex(i).objectReferenceValue = crawlers[i];
            }

            so.FindProperty("_screamer").objectReferenceValue = context.Bestiary.Queen;
            so.FindProperty("_earthTheme").objectReferenceValue = CreateEarthTheme();
            so.FindProperty("_undergroundTheme").objectReferenceValue = CreateUndergroundTheme();
            so.ApplyModifiedPropertiesWithoutUndo();
            return cast;
        }

        // Exhausted Earth: a pale, dusty sun, a brown sky, washed-out colour; space needs no fog.
        private static VisualTheme CreateEarthTheme()
        {
            var theme = LoadOrCreate<VisualTheme>($"{CutsceneFolder}/Theme_Earth.asset");
            ConfigureThemePalette(theme, new Palette
            {
                Background = new Color(0.008f, 0.008f, 0.012f),
                Ground = new Color(0.3f, 0.26f, 0.2f),
                Path = new Color(0.5f, 0.45f, 0.38f),
                Slot = new Color(0.2f, 0.55f, 1f),
                Base = new Color(0.3f, 0.85f, 1f),
                Light = new Color(1f, 0.9f, 0.76f),
                LightIntensity = 2.5f,
                Fog = new Color(0.04f, 0.03f, 0.02f),
                FogDensity = 0f,
                Bloom = 1.1f,
                Rock = new Color(0.34f, 0.31f, 0.27f),
                Accent = new Color(1f, 0.62f, 0.25f),
                Water = new Color(0.1f, 0.12f, 0.12f),
                Lava = new Color(1f, 0.4f, 0.1f),
                Sand = new Color(0.5f, 0.42f, 0.3f),
                Sky = new Color(0.36f, 0.3f, 0.24f),
                Contrast = 16f,
                Saturation = -22f,
                ShadowTint = new Color(1f, 0.9f, 0.8f),
                HighlightTint = new Color(1f, 0.93f, 0.82f),
                Props = new[] { PropKind.Rock, PropKind.Boulder },
                Vein = 0f,
                AmbientRate = 0f,
                Ambience = AmbienceKind.Wind,
                Plant = new Color(0.3f, 0.28f, 0.2f),
            });
            return theme;
        }

        // The cave under the field: near-black fog, the faintest cold ambient; crystals and eyes are the only light.
        private static VisualTheme CreateUndergroundTheme()
        {
            var theme = LoadOrCreate<VisualTheme>($"{CutsceneFolder}/Theme_Underground.asset");
            ConfigureThemePalette(theme, new Palette
            {
                Background = new Color(0.005f, 0.004f, 0.006f),
                Ground = new Color(0.1f, 0.1f, 0.12f),
                Path = new Color(0.3f, 0.3f, 0.35f),
                Slot = new Color(0.2f, 0.55f, 1f),
                Base = new Color(0.3f, 0.85f, 1f),
                Light = new Color(0.12f, 0.1f, 0.15f),
                LightIntensity = 0.4f,
                Fog = new Color(0.02f, 0.015f, 0.02f),
                FogDensity = 0.05f,
                Bloom = 1.6f,
                Rock = new Color(0.26f, 0.27f, 0.31f),
                Accent = new Color(0.35f, 0.85f, 1f),
                Water = new Color(0.05f, 0.2f, 0.3f),
                Lava = new Color(1f, 0.4f, 0.1f),
                Sand = new Color(0.3f, 0.28f, 0.3f),
                Sky = new Color(0.04f, 0.042f, 0.055f),
                Rim = new Color(1f, 0.55f, 0.3f),
                Contrast = 18f,
                Saturation = 10f,
                ShadowTint = new Color(0.9f, 0.85f, 1f),
                HighlightTint = new Color(1f, 0.9f, 0.8f),
                Props = new[] { PropKind.Rock, PropKind.Boulder, PropKind.Crystal },
                Vein = 0.3f,
                AmbientRate = 0f,
                Ambience = AmbienceKind.Wind,
                Plant = new Color(0.2f, 0.4f, 0.4f),
            });
            return theme;
        }

        private static DifficultyProfile HandoffDifficulty(DifficultyProfile[] difficulties)
        {
            foreach (DifficultyProfile difficulty in difficulties)
            {
                if (difficulty.name == HandoffDifficultyName)
                {
                    return difficulty;
                }
            }

            return difficulties.Length > 0 ? difficulties[0] : null;
        }

        // The prologue over the planet, then the field from landing to the first gate, with the dive underground
        // between the core lighting up and the ground starting to shake.
        private static ShotSpec[] OpeningShots(DialogueDefinition earth, DialogueDefinition survey, DialogueDefinition orbit, DialogueDefinition landing, DialogueDefinition drill, DialogueDefinition shiver, DialogueDefinition radar, DialogueDefinition gate)
        {
            return new[]
            {
                new ShotSpec
                {
                    Set = CutsceneSet.Earth, Action = CutsceneAction.EarthDrift, Timing = ShotTiming.AfterDialogue, Seconds = 3f, CameraSeconds = 16f,
                    Dialogue = earth, DialogueDelay = 2f, Music = CutsceneMusic.Wonder, FadeIn = 2f, FadeOut = 1f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(-5f, 0f, -56f), new Vector3(0.5f, -2f, 0f), 36f), Key(1f, CameraAnchor.World, new Vector3(-2f, -0.5f, -52f), new Vector3(0.3f, -2f, 0f), 36f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Survey, Action = CutsceneAction.ScanPlanet, Timing = ShotTiming.AfterDialogue, Seconds = 3f, CameraSeconds = 20f,
                    Dialogue = survey, DialogueDelay = 1.2f, FadeIn = 1f, FadeOut = 1f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(4f, 0.5f, -56f), new Vector3(0f, -2.2f, 0f), 36f), Key(1f, CameraAnchor.World, new Vector3(-2f, 0.8f, -51f), new Vector3(0f, -2f, 0f), 36f) },
                    Cues = new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.5f), Sound(4f, CutsceneSound.RadarPing, 0.4f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Orbit, Action = CutsceneAction.CapsulesFall, Timing = ShotTiming.AfterDialogue, Seconds = 4f, CameraSeconds = 14f,
                    Dialogue = orbit, DialogueDelay = 1.5f, FadeIn = 1f, FadeOut = 1.5f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 2f, -6f), new Vector3(0f, -9f, 40f), 46f), Key(1f, CameraAnchor.World, new Vector3(1.5f, 1.6f, -4f), new Vector3(0.5f, -9.5f, 40f), 44f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.PathfinderLands, Timing = ShotTiming.AfterDialogue, Seconds = 2.5f, CameraSeconds = 12f,
                    Dialogue = landing, DialogueDelay = 8.5f, FadeIn = 1.5f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Landing, new Vector3(-3f, 0.8f, 4.5f), new Vector3(2.5f, 12f, -6f), 55f),
                        Key(0.2f, CameraAnchor.Landing, new Vector3(-2.6f, 1.1f, 4.2f), new Vector3(0.3f, 0.9f, 0f), 46f),
                        Key(0.5f, CameraAnchor.Landing, new Vector3(-1.2f, 1.3f, 3.6f), new Vector3(1f, 1f, 0.4f), 42f),
                        Key(1f, CameraAnchor.Landing, new Vector3(0.2f, 1.35f, 3f), new Vector3(2.3f, 0.9f, 0.3f), 38f),
                    },
                    Cues = new[] { Sound(2.4f, CutsceneSound.Landing, 0.9f), Shake(2.4f, 0.7f), Sound(3.2f, CutsceneSound.BuildClank, 0.5f), Sound(8.6f, CutsceneSound.CoreIgnite, 0.45f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.BaseUnfolds, Seconds = 14f, FadeIn = 0.6f, FadeOut = 0.6f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Base, new Vector3(-6f, 6.5f, 9f), new Vector3(0f, 0.6f, 0f), 44f),
                        Key(0.5f, CameraAnchor.Base, new Vector3(0f, 7.5f, 10.5f), new Vector3(0f, 0.6f, -0.3f), 42f),
                        Key(1f, CameraAnchor.Base, new Vector3(6.5f, 6.5f, 8f), new Vector3(0.5f, 0.8f, 0f), 40f),
                    },
                    Cues = new[]
                    {
                        Clank(0.7f), Clank(3.1f), Clank(5f), Clank(7.7f), Clank(9.1f), Clank(10.5f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.DrillDown, Timing = ShotTiming.AfterDialogue, Seconds = 4f, CameraSeconds = 10f,
                    Dialogue = drill, DialogueDelay = 4f, FadeIn = 0.4f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Drill, new Vector3(-3.5f, 0.8f, -3f), new Vector3(0f, 3.2f, 0f), 52f),
                        Key(1f, CameraAnchor.Drill, new Vector3(-5.5f, 2.6f, -5.5f), new Vector3(0f, 1f, 0f), 45f),
                    },
                    Cues = new[] { Sound(2f, CutsceneSound.Earthquake, 0.8f), Shake(2f, 0.35f), Shake(5f, 0.25f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.DronesHarvest, Seconds = 10f, FadeIn = 0.4f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Drill, new Vector3(-3f, 2.2f, 6.5f), new Vector3(0.5f, 2.8f, 3f), 44f),
                        Key(1f, CameraAnchor.Drill, new Vector3(-3.5f, 2.6f, 8.5f), new Vector3(0f, 2.6f, 4f), 42f),
                    },
                    Cues = new[] { Sound(0.5f, CutsceneSound.DroneHarvest, 0.4f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.CoreIgnites, Seconds = 8f, FadeOut = 1.2f,
                    Keys = new[]
                    {
                        // Written from the core, so the shot finds it whether the procedural cage or the imported base carries it.
                        Key(0f, CameraAnchor.Core, new Vector3(-4f, 3f, 6.1f), new Vector3(0f, 1f, 0f), 42f),
                        Key(1f, CameraAnchor.Core, new Vector3(-2.3f, 2.3f, 4.4f), new Vector3(0f, 1f, 0f), 36f),
                    },
                    Cues = new[] { Sound(3.4f, CutsceneSound.CoreIgnite, 1f), Shake(3.4f, 0.3f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Underground, Action = CutsceneAction.CrystalsTremble, Seconds = 8f, Music = CutsceneMusic.Tension, FadeIn = 1.2f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 0.4f, 0f), new Vector3(0f, 0.3f, 10f), 55f), Key(1f, CameraAnchor.World, new Vector3(0f, 0.2f, 3f), new Vector3(0.5f, 0f, 14f), 52f) },
                    Cues = new[] { Sound(0.5f, CutsceneSound.Rumble, 0.8f), Shake(1f, 0.2f), Sound(4f, CutsceneSound.Earthquake, 0.5f), Shake(4f, 0.25f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Underground, Action = CutsceneAction.EyesOpen, Seconds = 9f, FadeOut = 1.2f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 0.3f, 3f), new Vector3(0.3f, 0.3f, 14f), 52f), Key(1f, CameraAnchor.World, new Vector3(0f, 0.4f, 6.5f), new Vector3(0f, 0.6f, 20f), 48f) },
                    Cues = new[] { Sound(2.5f, CutsceneSound.Growl, 0.5f), Sound(6f, CutsceneSound.Growl, 0.8f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.GroundShivers, Timing = ShotTiming.AfterDialogue, Seconds = 2f, CameraSeconds = 7f,
                    Dialogue = shiver, DialogueDelay = 3f, FadeIn = 1f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.DigSite, new Vector3(1.2f, 1.5f, 3.2f), new Vector3(0f, 1.1f, 0f), 38f),
                        Key(1f, CameraAnchor.DigSite, new Vector3(0.8f, 1f, 2.2f), new Vector3(0f, 0.7f, 0f), 34f),
                    },
                    Cues = new[] { Sound(0.8f, CutsceneSound.Rumble, 0.7f), Shake(1f, 0.25f), Shake(4f, 0.35f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.GroundShivers, Timing = ShotTiming.AfterDialogue, Seconds = 2f, CameraSeconds = 9f,
                    Dialogue = radar, DialogueDelay = 6f, Overlay = CutsceneOverlayKind.Radar,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.DigSite, new Vector3(0.5f, 2.5f, -3f), new Vector3(0f, 1f, 3f), 42f),
                        Key(1f, CameraAnchor.DigSite, new Vector3(0f, 9f, -9f), new Vector3(0f, 0f, 6f), 45f),
                    },
                    Cues = new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.6f), Sound(2.6f, CutsceneSound.RadarPing, 0.6f), Sound(4.5f, CutsceneSound.RadarPing, 0.7f), Sound(6.6f, CutsceneSound.BaseAlert, 0.8f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.SwarmPours, Seconds = 11f, Music = CutsceneMusic.Attack, FadeIn = 0.3f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Entrance, new Vector3(6f, 3.2f, 10f), new Vector3(0.5f, 0.8f, 1.5f), 45f),
                        Key(0.55f, CameraAnchor.Entrance, new Vector3(5f, 2.2f, 8.5f), new Vector3(1.4f, 1.6f, 1.2f), 40f),
                        Key(1f, CameraAnchor.Entrance, new Vector3(4f, 3.5f, 10f), new Vector3(0f, 0.5f, 3f), 45f),
                    },
                    Cues = new[]
                    {
                        Sound(0.6f, CutsceneSound.Emerge, 0.7f), Sound(1.4f, CutsceneSound.Emerge, 0.7f), Sound(2.6f, CutsceneSound.Emerge, 0.8f),
                        Sound(6f, CutsceneSound.Scream, 1f), Shake(6f, 0.6f), Sound(8.5f, CutsceneSound.Drums, 0.8f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.GateStand, Timing = ShotTiming.AfterDialogue, Seconds = 2.5f, CameraSeconds = 10f,
                    Dialogue = gate, DialogueDelay = 6.5f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Gate, new Vector3(3.8f, 2.4f, -1.4f), new Vector3(-4f, 0.8f, 1f), 46f),
                        Key(0.5f, CameraAnchor.Gate, new Vector3(2.4f, 1.7f, -0.8f), new Vector3(0f, 1.1f, 1.5f), 42f),
                        Key(1f, CameraAnchor.Gate, new Vector3(2.2f, 1.6f, -0.6f), new Vector3(-0.6f, 1.1f, 4f), 42f),
                    },
                    Cues = new[] { Sound(0.4f, CutsceneSound.BossAlarm, 0.5f), Sound(5.4f, CutsceneSound.BuildClank, 0.35f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.Handoff, Seconds = 7f, FadeOut = 1.4f, Letterbox = false, Title = "Defend the base",
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Gate, new Vector3(2.2f, 1.6f, -0.6f), new Vector3(-0.6f, 1.1f, 4f), 42f),
                        Key(1f, CameraAnchor.Gate, Vector3.zero, Vector3.zero, HandoffFieldOfView, CameraKeyMode.MatchLevelRig),
                    },
                },
            };
        }

        private static CameraKey Key(float at, CameraAnchor anchor, Vector3 position, Vector3 lookAt, float fieldOfView, CameraKeyMode mode = CameraKeyMode.Free)
        {
            return new CameraKey { At = at, Anchor = anchor, Position = position, LookAt = lookAt, FieldOfView = fieldOfView, Ease = CameraEase.InOut, Mode = mode };
        }

        private static CutsceneCue Sound(float at, CutsceneSound sound, float strength) => new CutsceneCue { At = at, Kind = CueKind.Sound, Sound = sound, Strength = strength };

        private static CutsceneCue Shake(float at, float strength) => new CutsceneCue { At = at, Kind = CueKind.Shake, Strength = strength };

        // The clank of a base module springing up, timed to the unfold shares in FieldBase.
        private static CutsceneCue Clank(float at) => Sound(at, CutsceneSound.BuildClank, 0.6f);

        private static void WriteShots(SerializedProperty array, ShotSpec[] shots)
        {
            array.arraySize = shots.Length;
            for (int i = 0; i < shots.Length; i++)
            {
                ShotSpec spec = shots[i];
                SerializedProperty shot = array.GetArrayElementAtIndex(i);
                shot.FindPropertyRelative("Set").intValue = (int)spec.Set;
                shot.FindPropertyRelative("Action").intValue = (int)spec.Action;
                shot.FindPropertyRelative("Timing").intValue = (int)spec.Timing;
                shot.FindPropertyRelative("Seconds").floatValue = spec.Seconds;
                shot.FindPropertyRelative("CameraSeconds").floatValue = spec.CameraSeconds;
                shot.FindPropertyRelative("Dialogue").objectReferenceValue = spec.Dialogue;
                shot.FindPropertyRelative("DialogueDelay").floatValue = spec.DialogueDelay;
                shot.FindPropertyRelative("Music").intValue = (int)spec.Music;
                shot.FindPropertyRelative("FadeIn").floatValue = spec.FadeIn;
                shot.FindPropertyRelative("FadeOut").floatValue = spec.FadeOut;
                shot.FindPropertyRelative("Letterbox").boolValue = spec.Letterbox;
                shot.FindPropertyRelative("Overlay").intValue = (int)spec.Overlay;
                shot.FindPropertyRelative("TitleCard").stringValue = spec.Title;
                WriteKeys(shot.FindPropertyRelative("Camera"), spec.Keys);
                WriteCues(shot.FindPropertyRelative("Cues"), spec.Cues);
            }
        }

        private static void WriteKeys(SerializedProperty array, CameraKey[] keys)
        {
            array.arraySize = keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                SerializedProperty key = array.GetArrayElementAtIndex(i);
                key.FindPropertyRelative("At").floatValue = keys[i].At;
                key.FindPropertyRelative("Anchor").intValue = (int)keys[i].Anchor;
                key.FindPropertyRelative("Position").vector3Value = keys[i].Position;
                key.FindPropertyRelative("LookAt").vector3Value = keys[i].LookAt;
                key.FindPropertyRelative("FieldOfView").floatValue = keys[i].FieldOfView;
                key.FindPropertyRelative("Ease").intValue = (int)keys[i].Ease;
                key.FindPropertyRelative("Mode").intValue = (int)keys[i].Mode;
            }
        }

        private static void WriteCues(SerializedProperty array, CutsceneCue[] cues)
        {
            array.arraySize = cues.Length;
            for (int i = 0; i < cues.Length; i++)
            {
                SerializedProperty cue = array.GetArrayElementAtIndex(i);
                cue.FindPropertyRelative("At").floatValue = cues[i].At;
                cue.FindPropertyRelative("Kind").intValue = (int)cues[i].Kind;
                cue.FindPropertyRelative("Sound").intValue = (int)cues[i].Sound;
                cue.FindPropertyRelative("Clip").objectReferenceValue = null;
                cue.FindPropertyRelative("Strength").floatValue = cues[i].Strength;
            }
        }
    }
}
