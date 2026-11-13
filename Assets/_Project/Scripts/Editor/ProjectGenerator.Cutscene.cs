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
        // The camera only kicks on a real impact, once in a shot and briefly: the capsule hitting the ground, the drill bit
        // biting into it, the ground bursting open at the entrance. Every other rumble is told by sound and dust alone.
        private const float LandingKick = 0.25f;
        private const float DrillKick = 0.18f;
        private const float BurstKick = 0.3f;

        // When the drill bit reaches the ground in its shot (FieldSet: the sink runs 0.2..0.85 of 10 s, contact at 0.27 of it).
        private const float DrillContactSeconds = 3.75f;

        // When the burrows burst in the attack shot (FieldSwarm: three, they start to open at 0.4 s, 0.8 s apart, burst 1.8 s in),
        // and when the Queen screams at the head of her brood (FieldSwarm.ScreamAt).
        private static readonly float[] BurstSeconds = { 2.2f, 3f, 3.8f };
        private const float QueenScreamSeconds = 7.8f;
        // The last roar in the handoff shot, before the brood runs back into the ground (FieldSet.HandoffRoarSeconds).
        private const float HandoffRoarSeconds = 0.3f;
        // Remco, 2026-11-05: no horde. The angry Queen and a few of her brood standing and glaring at the colony are enough.
        private const int BroodWalkers = 5;
        private const int BroodFlyers = 1;

        // Round 44 (Remco: "the launching should only launch once"): the mothership launches once. Round 59 (Remco chose the
        // salvo): in that launch it releases the cargo capsules first, ReleaseGap apart, and Spencer's capsule last
        // (CapsuleDrop.ReleaseAt gives each moment); his release and everything after it move by the salvo's length.
        private const int OrbitCapsules = 5;
        // 0.47 s keeps every cargo cue at least 0.18 s off each of Spencer's (0.5 s put a cargo separation 0.1 s after his ignition).
        private const float ReleaseGap = 0.47f;
        // The round-44 launch shot, which released Spencer's capsule first.
        private const float SingleLaunchSeconds = 9.5f;
        // The cargo capsules: smaller than Spencer's, a warm cargo livery and a marker light; the base modules for his landing site
        // carry the colony's cyan, the supplies for the other continents amber. Each aim (planet radii across and away from the
        // lens) lies behind the planet's rim, so it sinks out of sight; Spencer's is the round-44 lane set a little further back
        // (round-44 aim -0.078, 0.251), so he too is behind the rim before his fall ends.
        private static readonly Vector2 PathfinderAim = new Vector2(-0.08f, 0.32f);
        private const float CargoScale = 0.72f;
        private const float BaseModulesScale = 0.8f;
        private static readonly Color CargoHull = new Color(1f, 0.88f, 0.66f);
        private static readonly Color BaseModulesMarker = new Color(1.2f, 2.55f, 3f);
        private static readonly Color SuppliesMarker = new Color(3f, 2.04f, 0.6f);
        // The arrival shot: the ship's fly-in, then ORA's lines over it holding still; the launch shot after it has no text.
        private const float ArrivalReadDelay = 0.5f;
        private const float ArrivalCameraTail = 1f;
        // Round 44 (Remco: "the queen is screaming, but he doesn't react to it"): the attack shot cuts to Spencer this long into
        // her scream, so his startle answers it on screen.
        private const float ScreamCut = 0.8f;
        // Seconds after its release that a capsule's booster lights, once it has coasted clear of the hull (CapsuleDrop.IgniteAge).
        private const float CapsuleIgnition = 1.7f;
        // The NTI livery on the mothership's cargo frame (Tools/Blender/build_hull_livery.py), read crisp at a glancing angle.
        private const string HullLiveryPath = "Assets/_Project/Art/Textures/Brand/NTI_Hull_Livery.png";
        private const int HullLiveryAniso = 8;
        private const int HullLiverySize = 2048;

        // Round 39 (Remco: the brood must not leave until the gate line is read): the stand shot ends this soon after its dialogue,
        // so the roar and the retreat (the handoff shot) follow the click at once.
        private const float GateStandAfterRead = 0.6f;

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
            public CutsceneLayer[] Layers = new CutsceneLayer[0];
            public float ShakeRumble;
            public float QuakeHold;
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
                Say(ora, "The plan: build bases on several continents, mine the resources and ship them home. Field scout Cole Spencer, call sign Pathfinder, goes down first."),
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
                Say(voss, "We can't go back. Earth is counting on us. Spencer, hold those gates!"),
                Say(ora, "Standard defence systems ready. AI turrets can be deployed immediately."));

            CutsceneCast cast = CreateCutsceneCast(context);
            ShotSpec[] shots = OpeningShots(earth, survey, orbit, landing, drill, shiver, radar, gate);
            var cutscene = LoadOrCreate<CutsceneDefinition>(OpeningCutscenePath);
            var so = new SerializedObject(cutscene);
            so.FindProperty("_cast").objectReferenceValue = cast;
            so.FindProperty("_handoff").intValue = (int)CutsceneHandoff.FirstCampaignLevel;
            so.FindProperty("_handoffDifficulty").objectReferenceValue = HandoffDifficulty(context.Difficulties);
            so.FindProperty("_cameraFeel").boxedValue = CutsceneCameraFeel.Calm;
            so.FindProperty("_sounds").objectReferenceValue = CreateOpeningSoundBank();
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
            // One of each, different silhouettes; nothing that rolls.
            var crawlers = new[] { context.Bestiary.Stinger, context.Bestiary.Prowler, context.Bestiary.Bulwark, context.Bestiary.Cragback, context.Bestiary.Skitter };
            swarm.arraySize = crawlers.Length;
            for (int i = 0; i < crawlers.Length; i++)
            {
                swarm.GetArrayElementAtIndex(i).objectReferenceValue = crawlers[i];
            }

            so.FindProperty("_screamer").objectReferenceValue = context.Bestiary.Queen;
            SerializedProperty flyers = so.FindProperty("_flyers");
            var wings = new[] { context.Bestiary.Shalebat };
            flyers.arraySize = wings.Length;
            for (int i = 0; i < wings.Length; i++)
            {
                flyers.GetArrayElementAtIndex(i).objectReferenceValue = wings[i];
            }

            so.FindProperty("_flyerCount").intValue = BroodFlyers;
            so.FindProperty("_capsuleCount").intValue = OrbitCapsules;
            so.FindProperty("_releaseGap").floatValue = ReleaseGap;
            WriteFlight(so.FindProperty("_pathfinderFlight"), new CapsuleFlight("Spencer, the pathfinder", PathfinderAim, 1f, Color.white, Color.black));
            CapsuleFlight[] cargo = CargoFlights();
            SerializedProperty flights = so.FindProperty("_cargoFlights");
            flights.arraySize = cargo.Length;
            for (int i = 0; i < cargo.Length; i++)
            {
                WriteFlight(flights.GetArrayElementAtIndex(i), cargo[i]);
            }

            so.FindProperty("_swarmCount").intValue = BroodWalkers;
            so.FindProperty("_earthTheme").objectReferenceValue = CreateEarthTheme();
            so.FindProperty("_undergroundTheme").objectReferenceValue = CreateUndergroundTheme();
            so.FindProperty("_earthPlanet").boxedValue = PlanetStyle.Earth();
            so.FindProperty("_hullLivery").objectReferenceValue = HullLivery();
            so.ApplyModifiedPropertiesWithoutUndo();
            return cast;
        }

        // The salvo before Spencer's capsule, in release order: the base modules go down first, to his landing site, then the
        // supplies peel off to the west, the east and the north.
        private static CapsuleFlight[] CargoFlights() => new[]
        {
            new CapsuleFlight("Base modules for the landing site", new Vector2(0.12f, 0.42f), BaseModulesScale, CargoHull, BaseModulesMarker),
            new CapsuleFlight("Supplies for the western continent", new Vector2(-1.1f, 0.35f), CargoScale, CargoHull, SuppliesMarker),
            new CapsuleFlight("Supplies for the eastern continent", new Vector2(0.95f, 0.5f), CargoScale, CargoHull, SuppliesMarker),
            new CapsuleFlight("Supplies for the northern continent", new Vector2(-0.35f, 0.9f), CargoScale, CargoHull, SuppliesMarker),
        };

        private static void WriteFlight(SerializedProperty property, CapsuleFlight flight)
        {
            property.FindPropertyRelative("_cargo").stringValue = flight.Cargo;
            property.FindPropertyRelative("_aim").vector2Value = flight.Aim;
            property.FindPropertyRelative("_scale").floatValue = flight.Scale;
            property.FindPropertyRelative("_hullTint").colorValue = flight.HullTint;
            property.FindPropertyRelative("_markings").colorValue = flight.Markings;
        }

        // The launch shot is the round-44 one plus as long as the cargo capsules hold Spencer's release back.
        private static float PathfinderRelease => Game.UI.CapsuleDrop.PathfinderRelease(OrbitCapsules, ReleaseGap);

        private static float LaunchSeconds => SingleLaunchSeconds + PathfinderRelease - Game.UI.CapsuleDrop.ReleaseAt(0, ReleaseGap);

        // The livery texture with mipmaps and anisotropic filtering, so the wordmark stays sharp on the flank seen at an angle.
        private static Texture2D HullLivery()
        {
            if (!(AssetImporter.GetAtPath(HullLiveryPath) is TextureImporter importer))
            {
                return null;
            }

            if (importer.anisoLevel != HullLiveryAniso || !importer.mipmapEnabled || importer.maxTextureSize != HullLiverySize)
            {
                importer.anisoLevel = HullLiveryAniso;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = HullLiverySize;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(HullLiveryPath);
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
            var shots = new[]
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
                },
                new ShotSpec
                {
                    // Round 44 (Remco: "the space ship itself flies into the screen, stops, then launches"): the mothership comes in
                    // from beside the lens, brakes on its forward thrusters and holds; ORA speaks over it holding still, and the lens
                    // pans with it onto the launch shot's first frame.
                    Set = CutsceneSet.Orbit, Action = CutsceneAction.MothershipArrives, Timing = ShotTiming.AfterDialogue, Seconds = ArrivalReadDelay,
                    CameraSeconds = Game.UI.OrbitSet.ArrivalSeconds + ArrivalCameraTail, Dialogue = orbit, DialogueDelay = Game.UI.OrbitSet.ArrivalSeconds + ArrivalReadDelay, FadeIn = 1f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 2f, -6f), new Vector3(-16f, -3f, 32f), 52f), Key(1f, CameraAnchor.World, new Vector3(0f, 2f, -6f), new Vector3(0f, -9f, 40f), 46f) },
                },
                new ShotSpec
                {
                    // The launch, once and without text: the capsules leave the bay one after another, burn, separate and fall into
                    // the air, Spencer's last (round 59: the salvo). Round 36: the lens pushes in a little and tips down into the drop
                    // over the shot.
                    Set = CutsceneSet.Orbit, Action = CutsceneAction.CapsulesFall, Seconds = LaunchSeconds, FadeOut = 1.5f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 2f, -6f), new Vector3(0f, -9f, 40f), 46f), Key(1f, CameraAnchor.World, new Vector3(1f, 1.2f, -0.5f), new Vector3(-0.5f, -9.4f, 40f), 43f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.PathfinderLands, Timing = ShotTiming.AfterDialogue, Seconds = 2.5f, CameraSeconds = 12f,
                    Dialogue = landing, DialogueDelay = 8.5f, FadeIn = 1.5f,
                    // Round 40 (round 39 open point 2: the lens saw his back): after the touchdown the lens swings round in front of
                    // the hatch, to his left, so his face reads through the visor as he climbs out, comes down the ramp and looks
                    // round, and stays on that side as he walks over and the beacon goes up in front of him. (The capsule faces the
                    // beacon, about 82 degrees to the right of the landing's facing; it sits back by what it grew, its door step 1.35 m out.)
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Landing, new Vector3(-3f, 0.8f, 4.5f), new Vector3(2.5f, 12f, -6f), 55f),
                        Key(0.2f, CameraAnchor.Landing, new Vector3(-2.6f, 1.1f, 4.2f), new Vector3(0.3f, 0.9f, 0f), 46f),
                        Key(0.34f, CameraAnchor.Landing, new Vector3(3.7f, 1.25f, 2.2f), new Vector3(0.75f, 1.05f, 0.12f), 38f),
                        Key(0.55f, CameraAnchor.Landing, new Vector3(4f, 1.2f, 2f), new Vector3(1.3f, 0.95f, 0.2f), 38f),
                        Key(1f, CameraAnchor.Landing, new Vector3(4.5f, 1.45f, 2.4f), new Vector3(2.1f, 0.9f, 0.3f), 40f),
                    },
                },
                new ShotSpec
                {
                    // The builders bring every module down from above; FieldBase sounds the clank as each one touches down.
                    Set = CutsceneSet.Field, Action = CutsceneAction.BaseUnfolds, Seconds = 18f, FadeIn = 0.6f, FadeOut = 0.6f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Base, new Vector3(-5f, 4.2f, 6.5f), new Vector3(0f, 1.2f, 0f), 46f),
                        Key(0.5f, CameraAnchor.Base, new Vector3(0f, 5f, 7.5f), new Vector3(0f, 1.2f, -0.3f), 44f),
                        Key(1f, CameraAnchor.Base, new Vector3(5f, 4.2f, 6f), new Vector3(0.5f, 1.3f, 0f), 44f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.DrillDown, Timing = ShotTiming.AfterDialogue, Seconds = 4f, CameraSeconds = 10f,
                    Dialogue = drill, DialogueDelay = 4f, FadeIn = 0.4f,
                    Keys = new[]
                    {
                        // The drill stands at the colony rig beside the field (playtest note 3), with the border rocks behind it, so
                        // the lens stays on the base side: low under the derrick as it rises, then back to watch the string go in.
                        Key(0f, CameraAnchor.Drill, new Vector3(-3f, 0.7f, 3.2f), new Vector3(0f, 2.2f, 0f), 52f),
                        Key(1f, CameraAnchor.Drill, new Vector3(-4.4f, 2.1f, 4.6f), new Vector3(0f, 0.7f, 0f), 45f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.DronesHarvest, Seconds = 10f, FadeIn = 0.4f,
                    Keys = new[]
                    {
                        // The heap beside the drill, where the haulers dip down for their ore.
                        Key(0f, CameraAnchor.Drill, new Vector3(-6.5f, 2.8f, 4.5f), new Vector3(-1.2f, 1.4f, 0.8f), 46f),
                        Key(1f, CameraAnchor.Drill, new Vector3(-7.5f, 3.4f, 6.5f), new Vector3(-1f, 1.6f, 1.5f), 44f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.CoreIgnites, Seconds = 8f, FadeOut = 1.2f,
                    Keys = new[]
                    {
                        // Written from the core, so the shot finds it whether the procedural cage or the imported base carries it.
                        Key(0f, CameraAnchor.Core, new Vector3(-4f, 3f, 6.1f), new Vector3(0f, 2f, 0f), 42f),
                        Key(1f, CameraAnchor.Core, new Vector3(-2.3f, 2.3f, 4.4f), new Vector3(0f, 1.4f, 0f), 36f),
                    },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Underground, Action = CutsceneAction.CrystalsTremble, Seconds = 8f, Music = CutsceneMusic.Tension, FadeIn = 1.2f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 0.4f, 0f), new Vector3(0f, 0.3f, 10f), 55f), Key(1f, CameraAnchor.World, new Vector3(0f, 0.2f, 3f), new Vector3(0.5f, 0f, 14f), 52f) },
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Underground, Action = CutsceneAction.EyesOpen, Seconds = 9f, FadeOut = 1.2f,
                    Keys = new[] { Key(0f, CameraAnchor.World, new Vector3(0f, 0.3f, 3f), new Vector3(0.3f, 0.3f, 14f), 52f), Key(1f, CameraAnchor.World, new Vector3(0f, 0.4f, 6.5f), new Vector3(0f, 0.6f, 20f), 48f) },
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
                },
                new ShotSpec
                {
                    Set = CutsceneSet.Field, Action = CutsceneAction.SwarmPours, Seconds = QueenScreamSeconds + ScreamCut, Music = CutsceneMusic.Attack, FadeIn = 0.3f,
                    Keys = new[]
                    {
                        // Low on the road, down it from the entrance: wide on the bursts, then a slow push in on the Queen as she
                        // takes the head of her brood, the lens below her eyes so she towers over it when she screams.
                        Key(0f, CameraAnchor.Entrance, new Vector3(1.6f, 1.3f, 12f), new Vector3(0f, 0.6f, 3f), 50f),
                        Key(0.45f, CameraAnchor.Entrance, new Vector3(1.3f, 1f, 11.2f), new Vector3(0f, 0.85f, 4.4f), 46f),
                        Key(1f, CameraAnchor.Entrance, new Vector3(1.1f, 0.7f, 10.8f), new Vector3(0f, 1f, 5f), 40f),
                    },
                },
                new ShotSpec
                {
                    // Close on the commander at his dig site past the gate: he stares at them and cannot believe it.
                    Set = CutsceneSet.Field, Action = CutsceneAction.MercerReacts, Seconds = 5.5f,
                    Keys = new[]
                    {
                        // Docs/CUTSCENES_CONTINENTS.md section 8: between him and the creatures, about 27 degrees to his right at
                        // eye height, so the visor faces the lens; it follows his steps back with a slight push in.
                        // Round 40: he looks aside, turns to them at the growl, startles 0.5 m back (the hand to the gun), then draws
                        // as the Queen roars; the lens follows the shorter step back.
                        Key(0f, CameraAnchor.Commander, new Vector3(1.2f, 1.4f, 2.3f), new Vector3(0f, 1.3f, 0f), 32f),
                        Key(1f, CameraAnchor.Commander, new Vector3(1.15f, 1.4f, 1.4f), new Vector3(0.1f, 1.25f, -0.5f), 30f),
                    },
                },
                new ShotSpec
                {
                    // Back to the gate at a run, round behind its field to face the brood glaring across it, the blaster up.
                    Set = CutsceneSet.Field, Action = CutsceneAction.GateStand, Timing = ShotTiming.AfterDialogue, Seconds = GateStandAfterRead, CameraSeconds = 10f,
                    Dialogue = gate, DialogueDelay = 6.5f,
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Gate, new Vector3(4.2f, 2.6f, -5.5f), new Vector3(0f, 0.8f, -2f), 46f),
                        Key(0.5f, CameraAnchor.Gate, new Vector3(1.8f, 2.1f, -5.2f), new Vector3(-0.2f, 1.1f, 0.5f), 44f),
                        Key(1f, CameraAnchor.Gate, new Vector3(1.1f, 1.9f, -4.8f), new Vector3(-0.3f, 1.1f, 2.5f), 42f),
                    },
                },
                new ShotSpec
                {
                    // Playtest note 4: the Queen roars once more and the brood runs back into the ground before play starts
                    // (FieldSwarm retreat, the roar FieldSet.HandoffRoarSeconds in); then the burrows close and the view pulls out.
                    Set = CutsceneSet.Field, Action = CutsceneAction.Handoff, Seconds = 8.5f, FadeOut = 1.4f, Letterbox = false, Title = "Defend the base",
                    Keys = new[]
                    {
                        Key(0f, CameraAnchor.Gate, new Vector3(1.1f, 1.9f, -4.8f), new Vector3(-0.3f, 1.1f, 2.5f), 42f),
                        Key(1f, CameraAnchor.Gate, Vector3.zero, Vector3.zero, HandoffFieldOfView, CameraKeyMode.MatchLevelRig),
                    },
                },
            };

            // The sound of every shot comes from one table (ProjectGenerator.CutsceneSounds), which the sound step also writes.
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i].Cues = OpeningCues(i);
                shots[i].Layers = OpeningLayers(i);
                shots[i].ShakeRumble = OpeningShakeRumble(i);
                shots[i].QuakeHold = OpeningQuakeHold(i);
            }

            return shots;
        }

        private static CameraKey Key(float at, CameraAnchor anchor, Vector3 position, Vector3 lookAt, float fieldOfView, CameraKeyMode mode = CameraKeyMode.Free)
        {
            return new CameraKey { At = at, Anchor = anchor, Position = position, LookAt = lookAt, FieldOfView = fieldOfView, Ease = CameraEase.InOut, Mode = mode };
        }

        private static CutsceneCue Sound(float at, CutsceneSound sound, float strength) => new CutsceneCue { At = at, Kind = CueKind.Sound, Sound = sound, Strength = strength };

        private static CutsceneCue Shake(float at, float strength) => new CutsceneCue { At = at, Kind = CueKind.Shake, Strength = strength };

        // Cue lists in time order, as the sequencer plays them.
        private static CutsceneCue[] Join(params CutsceneCue[][] parts)
        {
            var all = new System.Collections.Generic.List<CutsceneCue>();
            foreach (CutsceneCue[] part in parts)
            {
                all.AddRange(part);
            }

            all.Sort((a, b) => a.At.CompareTo(b.At));
            return all.ToArray();
        }

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
                WriteLighting(shot.FindPropertyRelative("Lighting"), LightingFor(spec.Action));
                WriteKeys(shot.FindPropertyRelative("Camera"), spec.Keys);
                WriteCues(shot.FindPropertyRelative("Cues"), spec.Cues);
                WriteLayers(shot.FindPropertyRelative("Layers"), spec.Layers);
                shot.FindPropertyRelative("ShakeRumble").floatValue = spec.ShakeRumble;
                shot.FindPropertyRelative("QuakeHold").floatValue = spec.QuakeHold;
            }
        }

        // Moonlight from the camera side, a rim from behind (warm where the burrows glow, cool elsewhere), a faint violet
        // fill, and the exposure lifted so the subject reads while the field still feels like night. Space keeps its own
        // light; the cave gets only a cool rim on top of its crystals.
        private static readonly Color Moon = new Color(0.62f, 0.72f, 1f);
        private static readonly Color CoolRim = new Color(0.75f, 0.85f, 1f);
        private static readonly Color WarmRim = new Color(1f, 0.55f, 0.3f);
        private static readonly Color VioletFill = new Color(0.42f, 0.34f, 0.6f);
        private static readonly Color CaveRim = new Color(0.4f, 0.85f, 1f);

        private static ShotLighting LightingFor(CutsceneAction action)
        {
            switch (action)
            {
                case CutsceneAction.PathfinderLands:
                    return Night(0.5f, 0.6f, CoolRim, 0.9f, false);
                case CutsceneAction.BaseUnfolds:
                case CutsceneAction.DronesHarvest:
                case CutsceneAction.CoreIgnites:
                    return Night(0.8f, 0.8f, CoolRim, 1f, false);
                case CutsceneAction.DrillDown:
                    // Rocks stand round the colony rig's ground: the ones on the line of sight make way.
                    return Night(0.8f, 0.8f, CoolRim, 1f, true);
                case CutsceneAction.CrystalsTremble:
                case CutsceneAction.EyesOpen:
                    return new ShotLighting { Exposure = 1.2f, KeyColour = Moon, KeyIntensity = 0.25f, RimColour = CaveRim, RimIntensity = 0.7f };
                case CutsceneAction.GroundShivers:
                    return Night(0.9f, 1f, CoolRim, 1.1f, true);
                case CutsceneAction.SwarmPours:
                    return Night(1.25f, 1.2f, WarmRim, 1.8f, false);
                case CutsceneAction.MercerReacts:
                    return Night(1.1f, 1.3f, WarmRim, 1.4f, true);
                case CutsceneAction.GateStand:
                    return Night(1f, 1f, WarmRim, 1.4f, true);
                case CutsceneAction.Handoff:
                    return Night(0.5f, 0.6f, CoolRim, 0.8f, false);
                default:
                    return default;
            }
        }

        private static ShotLighting Night(float exposure, float key, Color rim, float rimIntensity, bool clearView) => new ShotLighting
        {
            Exposure = exposure,
            KeyColour = Moon,
            KeyIntensity = key,
            RimColour = rim,
            RimIntensity = rimIntensity,
            FillColour = VioletFill,
            FillIntensity = 0.35f,
            ClearView = clearView,
        };

        private static void WriteLighting(SerializedProperty lighting, ShotLighting value)
        {
            lighting.FindPropertyRelative("Exposure").floatValue = value.Exposure;
            lighting.FindPropertyRelative("KeyColour").colorValue = value.KeyColour;
            lighting.FindPropertyRelative("KeyIntensity").floatValue = value.KeyIntensity;
            lighting.FindPropertyRelative("RimColour").colorValue = value.RimColour;
            lighting.FindPropertyRelative("RimIntensity").floatValue = value.RimIntensity;
            lighting.FindPropertyRelative("FillColour").colorValue = value.FillColour;
            lighting.FindPropertyRelative("FillIntensity").floatValue = value.FillIntensity;
            lighting.FindPropertyRelative("ClearView").boolValue = value.ClearView;
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
                cue.FindPropertyRelative("FadeIn").floatValue = cues[i].FadeIn;
            }
        }
    }
}
