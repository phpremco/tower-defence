using System.Collections.Generic;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 42: the opening cutscene's own sounds. Fills the sound bank from Remco's approved files in
    /// Audio/Approved/Intro (a slot whose file is still being redone stays empty and plays its fallback or nothing) and
    /// writes every shot's cues and held layers. The full generator writes the same tables; the menu step rewrites only
    /// the bank and the sound of the existing shots, so it runs on its own without touching anything else.
    /// No music: the space drone bed carries the prologue until Remco picks a theme, so the mood tracks stay empty.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string OpeningSoundsPath = CutsceneFolder + "/CutsceneSounds_Opening.asset";
        private const string IntroAudio = "Intro";

        private const string IntroOrbit = "Orbit";
        private const string IntroLaunch = "Launch";
        private const string IntroReentry = "Reentry";
        private const string IntroCapsule = "Capsule";
        private const string IntroColony = "Colony";
        private const string IntroThreat = "Threat";
        private const string IntroEnvironment = "Environment";

        // The music hook per mood, silent until a track is dropped in (volumes as the borrowed level music played).
        private static readonly (CutsceneMusic Mood, float Volume)[] OpeningMusicMoods =
        {
            (CutsceneMusic.Wonder, 0.5f), (CutsceneMusic.Tension, 0.42f), (CutsceneMusic.Attack, 0.55f),
        };

        /// <summary>One slot of the bank: where its files are, whether it loops, its mix group and its stand-in.</summary>
        private readonly struct IntroSlot
        {
            public readonly CutsceneSound Sound;
            public readonly string Folder;
            public readonly string Name;
            public readonly bool Loop;
            public readonly CutsceneSoundChannel Channel;
            public readonly CutsceneSound Fallback;

            public IntroSlot(CutsceneSound sound, string folder, string name, bool loop = false, CutsceneSoundChannel channel = CutsceneSoundChannel.Effects, CutsceneSound fallback = CutsceneSound.None)
            {
                Sound = sound;
                Folder = folder;
                Name = name;
                Loop = loop;
                Channel = channel;
                Fallback = fallback;
            }
        }

        // Names without the variant number (Docs/AUDIO.md, Tools/Audio/out/intro/manifest.md). A slot whose file is missing
        // stays empty (its fallback plays, or nothing) until a file lands under its name; rerun the step to fill it.
        private static readonly IntroSlot[] IntroSlots =
        {
            new IntroSlot(CutsceneSound.OrbitBed, IntroOrbit, "orbit_space_drone_loop", true, CutsceneSoundChannel.Ambience),
            new IntroSlot(CutsceneSound.MothershipHum, IntroOrbit, "orbit_mothership_hum_loop", true),
            new IntroSlot(CutsceneSound.BayDoors, IntroOrbit, "orbit_cargo_bay_doors"),
            new IntroSlot(CutsceneSound.ClampsRelease, IntroOrbit, "orbit_clamps_release"),
            new IntroSlot(CutsceneSound.MothershipAlarm, IntroOrbit, "orbit_mothership_alarm_loop", true),
            new IntroSlot(CutsceneSound.ColdGasPuff, IntroLaunch, "launch_cold_gas_puff", fallback: CutsceneSound.Thrusters),
            new IntroSlot(CutsceneSound.BoosterIgnition, IntroLaunch, "launch_booster_ignition", fallback: CutsceneSound.RocketLaunch),
            new IntroSlot(CutsceneSound.BoosterBurn, IntroLaunch, "launch_booster_burn_loop", true),
            new IntroSlot(CutsceneSound.StageSeparation, IntroLaunch, "launch_stage_separation"),
            new IntroSlot(CutsceneSound.BoosterFade, IntroLaunch, "launch_booster_fade_distance"),
            new IntroSlot(CutsceneSound.PlasmaRoar, IntroReentry, "reentry_plasma_roar_loop", true),
            new IntroSlot(CutsceneSound.CapsuleRattle, IntroReentry, "reentry_capsule_rattle_loop", true),
            // A one-shot held as a layer: it plays once from the start of the fall and fades out at the touchdown.
            new IntroSlot(CutsceneSound.WindRush, IntroReentry, "reentry_wind_rush"),
            new IntroSlot(CutsceneSound.RetroBurst, IntroReentry, "reentry_retro_burst", fallback: CutsceneSound.Thrusters),
            new IntroSlot(CutsceneSound.Touchdown, IntroReentry, "reentry_touchdown_heavy", fallback: CutsceneSound.Landing),
            new IntroSlot(CutsceneSound.PressureRelease, IntroCapsule, "capsule_pressure_release", fallback: CutsceneSound.Thrusters),
            new IntroSlot(CutsceneSound.HatchOpen, IntroCapsule, "capsule_hatch_open", fallback: CutsceneSound.BuildClank),
            new IntroSlot(CutsceneSound.StepMetal, IntroCapsule, "capsule_footstep_metal", fallback: CutsceneSound.HeroStep),
            new IntroSlot(CutsceneSound.StepGravel, IntroCapsule, "capsule_footstep_gravel", fallback: CutsceneSound.HeroStep),
            new IntroSlot(CutsceneSound.StepStone, IntroCapsule, "capsule_footstep_stone", fallback: CutsceneSound.HeroStep),
            new IntroSlot(CutsceneSound.BreathCalm, IntroCapsule, "capsule_breath_helmet_calm_loop", true),
            new IntroSlot(CutsceneSound.BreathFast, IntroCapsule, "capsule_breath_helmet_fast_loop", true),
            new IntroSlot(CutsceneSound.Panting, IntroCapsule, "capsule_panting_helmet"),
            new IntroSlot(CutsceneSound.Gasp, IntroCapsule, "capsule_gasp_helmet"),
            // The gasp without the helmet chain: an alternative take, not cued (swap it into Gasp by data if it reads better).
            new IntroSlot(CutsceneSound.GaspDry, IntroCapsule, "capsule_gasp_dry"),
            new IntroSlot(CutsceneSound.PistolDraw, IntroCapsule, "capsule_pistol_draw"),
            new IntroSlot(CutsceneSound.PistolCharge, IntroCapsule, "capsule_pistol_charge"),
            // Round 43: not cued any more (the dialogue box's opener plays when Voss starts); kept for an ear check against it.
            new IntroSlot(CutsceneSound.RadioOpen, IntroCapsule, "capsule_radio_call_open"),
            new IntroSlot(CutsceneSound.RadioStatic, IntroCapsule, "capsule_radio_static_loop", true),
            new IntroSlot(CutsceneSound.DroneHum, IntroColony, "colony_drone_hum_loop", true, fallback: CutsceneSound.DroneFans),
            new IntroSlot(CutsceneSound.DroneLand, IntroColony, "colony_drone_land"),
            new IntroSlot(CutsceneSound.DrillStart, IntroColony, "colony_drill_start"),
            new IntroSlot(CutsceneSound.DrillRun, IntroColony, "colony_drill_run_loop", true, fallback: CutsceneSound.Rumble),
            new IntroSlot(CutsceneSound.CrystalHum, IntroColony, "colony_crystal_hum_loop", true, CutsceneSoundChannel.Ambience),
            new IntroSlot(CutsceneSound.RadarPing, IntroColony, "colony_radar_ping"),
            new IntroSlot(CutsceneSound.BeaconBeep, IntroColony, "colony_beacon_beep", fallback: CutsceneSound.CoreIgnite),
            new IntroSlot(CutsceneSound.QuakeLoop, IntroThreat, "threat_quake_rumble_loop", true),
            new IntroSlot(CutsceneSound.QuakeSwell, IntroThreat, "threat_quake_swell", fallback: CutsceneSound.Earthquake),
            new IntroSlot(CutsceneSound.GroundTear, IntroThreat, "threat_ground_tear", fallback: CutsceneSound.Emerge),
            new IntroSlot(CutsceneSound.EyesOpenWet, IntroThreat, "threat_eyes_open_wet"),
            new IntroSlot(CutsceneSound.SwarmChitter, IntroThreat, "threat_swarm_chitter_loop", true),
            new IntroSlot(CutsceneSound.QueenRoar, IntroThreat, "threat_queen_roar", fallback: CutsceneSound.Scream),
            new IntroSlot(CutsceneSound.SwarmRetreat, IntroThreat, "threat_swarm_retreat"),
            // Remco: the wind is fine but quite loud, so it sits well under the other beds (WindLevel).
            new IntroSlot(CutsceneSound.WindPlain, IntroEnvironment, "env_wind_plain_loop", true, CutsceneSoundChannel.Ambience),
            new IntroSlot(CutsceneSound.CaveDrips, IntroEnvironment, "env_cave_drips_loop", true, CutsceneSoundChannel.Ambience),
            // Round 45a (Remco: a screaming monster only when someone reacts to it): not cued any more; kept for later use.
            new IntroSlot(CutsceneSound.WildlifeCall, IntroEnvironment, "env_wildlife_call", channel: CutsceneSoundChannel.Ambience),
            // Round 44: the burrows closing behind the brood (no fallback: the old "ploof" must not come back; the slot stays
            // silent while it has no file; round 45a: _01..03 landed, each with its slam 0.15 s before its end, FieldSwarm) and the
            // builders assembling the base.
            new IntroSlot(CutsceneSound.BurrowClose, IntroThreat, "threat_burrow_close"),
            new IntroSlot(CutsceneSound.BuildHammer, IntroColony, "colony_build_hammer"),
            new IntroSlot(CutsceneSound.BuildHammerLoop, IntroColony, "colony_build_hammer_loop", true),
            new IntroSlot(CutsceneSound.BuildDrill, IntroColony, "colony_build_drill"),
            new IntroSlot(CutsceneSound.BuildDrillLoop, IntroColony, "colony_build_drill_loop", true),
        };

        // The shots of the opening in order; the sound tables below are written per shot and checked against it.
        private static readonly CutsceneAction[] OpeningOrder =
        {
            CutsceneAction.EarthDrift, CutsceneAction.ScanPlanet, CutsceneAction.MothershipArrives, CutsceneAction.CapsulesFall,
            CutsceneAction.PathfinderLands, CutsceneAction.BaseUnfolds, CutsceneAction.DrillDown, CutsceneAction.DronesHarvest,
            CutsceneAction.CoreIgnites, CutsceneAction.CrystalsTremble, CutsceneAction.EyesOpen, CutsceneAction.GroundShivers,
            CutsceneAction.GroundShivers, CutsceneAction.SwarmPours, CutsceneAction.MercerReacts, CutsceneAction.GateStand,
            CutsceneAction.Handoff,
        };

        // Levels of the held layers before the slot gains (which bring every file to one loudness) and the player's mix.
        private const float BedLevel = 0.55f;
        private const float WindLevel = 0.12f;
        private const float HumLevel = 0.45f;
        private const float BrakeLevel = 0.35f;
        private const float BreathLevel = 0.35f;
        private const float FastBreathLevel = 0.45f;
        private const float ChitterLevel = 0.25f;
        private const float SwarmChitterLevel = 0.35f;
        // The drips are sparse, so their measured gain is high: a low level keeps them a background detail.
        private const float DripLevel = 0.2f;
        private const float StaticLevel = 0.15f;
        private const float DroneHumLevel = 0.3f;

        // The builders land beside the base over the last 0.08 of the 18 s timelapse (FieldBase.ParkShare); the landing
        // clip starts at the hum's level, so the hum fades out under its first half second.
        private const float BuildersLand = 16.6f;
        private const float DroneHandover = 0.5f;

        // The launch shot (round 44: one launch): the bay doors open as it begins, each capsule's clamps let go with a puff of
        // cold gas, its booster lights CapsuleIgnition later and separates CapsuleSeparation after its release; the burn loop
        // covers Spencer's burn. Round 59 (the salvo): the cargo capsules before his sound the same clips, quieter and each
        // swelling in from silence (no hard starts, no second loud hit), with a low burn loop under their burns; the spent
        // booster fading away is his alone.
        private const float CapsuleSeparation = 3.3f;
        private const float BurnTail = 0.2f;
        private const float CargoClampsLevel = 0.22f;
        private const float CargoGasLevel = 0.2f;
        private const float CargoIgnitionLevel = 0.28f;
        private const float CargoSeparationLevel = 0.2f;
        private const float CargoBurnLevel = 0.15f;
        private const float CargoClampsSwell = 0.08f;
        private const float CargoGasSwell = 0.12f;
        private const float CargoIgnitionSwell = 0.2f;
        private const float CargoSeparationSwell = 0.06f;
        private const float CargoBurnFadeIn = 0.5f;
        private const float CargoBurnFadeOut = 0.4f;

        // The landing (FieldSet: the fall is the first 0.2 of the 12 s shot, the retro jets fire over its last part and flare at
        // these bursts, the hatch steams from 0.255 and opens at 0.27, Spencer appears at 0.31).
        private const float TouchdownSeconds = 2.4f;
        private static readonly float[] RetroBursts = { 1.35f, 1.75f, 2.1f };
        private const float HatchSteamSeconds = 3.06f;
        private const float HatchOpenSeconds = 3.24f;
        private const float AppearSeconds = 3.9f;

        // The cave's eyes (UndergroundSet: the lead pair opens at 0.12 of the 9 s shot, the second at 0.28, the wave from 0.42):
        // the wet sound of each opening lands as it opens.
        private static readonly float[] EyeOpenings = { 1.08f, 2.52f, 3.78f };

        // Round 44: the reaction shot cuts in on the Queen's scream and he is frightened at once (FieldSet.ReactFlinchAt), he
        // arrives at the gate (GateStand: the run is the first 0.42 of 10 s) and Voss opens the channel (the gate dialogue at 6.5 s).
        private const float FearFrom = 0.3f;
        private const float GateArrival = 4.3f;
        private const float RadioSeconds = 6.3f;
        private const float RetreatSeconds = 1f;

        // Round 44 (Remco: things shook without a rumble): how loud the quake rumble follows each shot's shaking (the kicks, the
        // lens rumble, the trembling ground and props). Space keeps it low under the engine; the quakes get it full.
        private static float OpeningShakeRumble(int shot)
        {
            switch (shot)
            {
                case 3:
                    return 0.35f;
                case 4:
                case 16:
                    return 0.6f;
                case 6:
                    return 0.7f;
                case 9:
                case 13:
                    return 0.8f;
                case 10:
                case 12:
                    return 0.6f;
                case 11:
                    return 0.85f;
                case 14:
                    return 0.5f;
                default:
                    return 0f;
            }
        }

        /// <summary>Fills the opening's sound bank and rewrites the sound of its shots, then saves. Safe to run again.</summary>
        [MenuItem("Tools/Generate/Opening Cutscene Sounds")]
        public static void GenerateOpeningCutsceneSounds()
        {
            var cutscene = AssetDatabase.LoadAssetAtPath<CutsceneDefinition>(OpeningCutscenePath);
            if (cutscene == null)
            {
                throw new System.InvalidOperationException("The opening cutscene is missing: run the full generator once first.");
            }

            var so = new SerializedObject(cutscene);
            so.FindProperty("_sounds").objectReferenceValue = CreateOpeningSoundBank();
            SerializedProperty shots = so.FindProperty("_shots");
            if (shots.arraySize != OpeningOrder.Length)
            {
                throw new System.InvalidOperationException($"The opening has {shots.arraySize} shots; the sound tables are written for {OpeningOrder.Length}.");
            }

            for (int i = 0; i < shots.arraySize; i++)
            {
                SerializedProperty shot = shots.GetArrayElementAtIndex(i);
                if (shot.FindPropertyRelative("Action").intValue != (int)OpeningOrder[i])
                {
                    throw new System.InvalidOperationException($"Shot {i + 1} of the opening is not {OpeningOrder[i]}; the sound tables are out of date.");
                }

                WriteCues(shot.FindPropertyRelative("Cues"), OpeningCues(i));
                WriteLayers(shot.FindPropertyRelative("Layers"), OpeningLayers(i));
                shot.FindPropertyRelative("ShakeRumble").floatValue = OpeningShakeRumble(i);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cutscene);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated the opening cutscene's sounds.");
        }

        private static CutsceneSoundBank CreateOpeningSoundBank()
        {
            var bank = LoadOrCreate<CutsceneSoundBank>(OpeningSoundsPath);
            var so = new SerializedObject(bank);
            SerializedProperty slots = so.FindProperty("_slots");
            slots.arraySize = IntroSlots.Length;
            var empty = new List<string>();
            for (int i = 0; i < IntroSlots.Length; i++)
            {
                IntroSlot spec = IntroSlots[i];
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("Sound").intValue = (int)spec.Sound;
                slot.FindPropertyRelative("Loop").boolValue = spec.Loop;
                slot.FindPropertyRelative("Channel").intValue = (int)spec.Channel;
                slot.FindPropertyRelative("Fallback").intValue = (int)spec.Fallback;
                List<AudioClip> clips = FindClips($"{AudioRoot}/{ApprovedFolder}/{IntroAudio}/{spec.Folder}", spec.Name, false);
                FillSet(slot.FindPropertyRelative("Clips"), clips);
                if (clips.Count == 0)
                {
                    empty.Add(spec.Sound.ToString());
                }
            }

            SerializedProperty music = so.FindProperty("_music");
            music.arraySize = OpeningMusicMoods.Length;
            for (int i = 0; i < OpeningMusicMoods.Length; i++)
            {
                SerializedProperty track = music.GetArrayElementAtIndex(i);
                track.FindPropertyRelative("Mood").intValue = (int)OpeningMusicMoods[i].Mood;
                track.FindPropertyRelative("Volume").floatValue = OpeningMusicMoods[i].Volume;
                FillSet(track.FindPropertyRelative("Track"), new List<AudioClip>());
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            if (empty.Count > 0)
            {
                Debug.Log($"Opening sound slots still without a file (fallback or silent): {string.Join(", ", empty)}.");
            }

            return bank;
        }

        // The one-shots of each shot (sounds and the camera's kicks), in the shot's own seconds. Round 44: Spencer's footsteps
        // are not here any more (FieldSet plays them on his clips' footfalls), and no creature is heard that the shot does not
        // show: the cave has its eyes' wet opening and no growls, the reaction close-up keeps only the tail of the Queen's
        // scream it cuts in on, the burrows' closing sounds from the burrows themselves (FieldSwarm). Round 45a (Remco: a
        // screaming monster only when someone reacts to it): no distant wildlife calls and no growls; the Queen's two roars stay,
        // Spencer startles at the first and his aim jolts at the second.
        private static CutsceneCue[] OpeningCues(int shot)
        {
            switch (shot)
            {
                case 1:
                    return new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.5f), Sound(4f, CutsceneSound.RadarPing, 0.4f) };
                case 3:
                    return LaunchCues();
                case 4:
                    return new[]
                    {
                        Sound(RetroBursts[0], CutsceneSound.RetroBurst, 0.55f), Sound(RetroBursts[1], CutsceneSound.RetroBurst, 0.5f), Sound(RetroBursts[2], CutsceneSound.RetroBurst, 0.55f),
                        Sound(TouchdownSeconds, CutsceneSound.Touchdown, 0.9f), Shake(TouchdownSeconds, LandingKick),
                        Sound(HatchSteamSeconds, CutsceneSound.PressureRelease, 0.55f), Sound(HatchOpenSeconds, CutsceneSound.HatchOpen, 0.5f),
                        Sound(8.7f, CutsceneSound.BeaconBeep, 0.45f), Sound(10.2f, CutsceneSound.BeaconBeep, 0.4f),
                    };
                case 5:
                    // As the flock settles beside the finished base, the landing spin-down takes over from the hum.
                    return new[] { Sound(BuildersLand, CutsceneSound.DroneLand, 0.45f) };
                case 6:
                    return new[] { Sound(0.3f, CutsceneSound.DrillStart, 0.5f), Sound(0.6f, CutsceneSound.BuildClank, 0.45f), Sound(DrillContactSeconds, CutsceneSound.Earthquake, 0.8f), Shake(DrillContactSeconds, DrillKick) };
                case 7:
                    return new[] { Sound(0.5f, CutsceneSound.DroneHarvest, 0.4f) };
                case 8:
                    return new[] { Sound(3.4f, CutsceneSound.CoreIgnite, 1f) };
                case 9:
                    // The swell peaks with its rock cracks about 4.5 s in, as the crystals shake hardest.
                    return new[] { Sound(0.5f, CutsceneSound.QuakeSwell, 0.8f) };
                case 10:
                    return new[] { Sound(EyeOpenings[0], CutsceneSound.EyesOpenWet, 0.5f), Sound(EyeOpenings[1], CutsceneSound.EyesOpenWet, 0.45f), Sound(EyeOpenings[2], CutsceneSound.EyesOpenWet, 0.35f) };
                case 11:
                    return new[] { Sound(0.3f, CutsceneSound.QuakeSwell, 0.6f) };
                case 12:
                    return new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.6f), Sound(2.6f, CutsceneSound.RadarPing, 0.6f), Sound(4.5f, CutsceneSound.RadarPing, 0.7f), Sound(6.6f, CutsceneSound.BaseAlert, 0.8f) };
                case 13:
                    // The burrows tear open one after another (the first kicks the camera), the brood hisses into its places,
                    // the Queen screams; the shot cuts to Spencer in her scream. No music until Remco picks it.
                    return new[]
                    {
                        Sound(BurstSeconds[0], CutsceneSound.GroundTear, 0.9f), Shake(BurstSeconds[0], BurstKick), Sound(BurstSeconds[1], CutsceneSound.GroundTear, 0.75f),
                        Sound(BurstSeconds[2], CutsceneSound.GroundTear, 0.8f), Sound(4.6f, CutsceneSound.HeavySteps, 0.45f),
                        Sound(5.9f, CutsceneSound.HeavySteps, 0.4f), Sound(QueenScreamSeconds, CutsceneSound.QueenRoar, 1f),
                    };
                case 15:
                    // Round 43: no radio-call cue as Voss starts: the dialogue box's own squelch opens his line, the static holds under it.
                    return new[] { Sound(0.4f, CutsceneSound.BossAlarm, 0.5f), Sound(GateArrival, CutsceneSound.Panting, 0.55f), Sound(5.4f, CutsceneSound.BuildClank, 0.35f) };
                case 16:
                    return new[]
                    {
                        Sound(HandoffRoarSeconds, CutsceneSound.QueenRoar, 1f), Sound(RetreatSeconds, CutsceneSound.SwarmRetreat, 0.7f),
                        Sound(1.7f, CutsceneSound.HeavySteps, 0.6f), Sound(2.2f, CutsceneSound.HeavySteps, 0.55f), Sound(2.7f, CutsceneSound.HeavySteps, 0.5f),
                    };
                default:
                    return new CutsceneCue[0];
            }
        }

        // One launch: the doors, then each cargo capsule's clamps, cold gas, ignition and separation, quiet and swelling in, and
        // last Spencer's at the round-44 levels: his clamps and cold gas, his booster's ignition, the separation and the spent
        // booster fading away below.
        private static CutsceneCue[] LaunchCues()
        {
            var cues = new List<CutsceneCue> { Sound(0.1f, CutsceneSound.BayDoors, 0.6f) };
            for (int i = 0; i < OrbitCapsules - 1; i++)
            {
                float cargo = Game.UI.CapsuleDrop.ReleaseAt(i, ReleaseGap);
                cues.Add(Swell(cargo, CutsceneSound.ClampsRelease, CargoClampsLevel, CargoClampsSwell));
                cues.Add(Swell(cargo, CutsceneSound.ColdGasPuff, CargoGasLevel, CargoGasSwell));
                cues.Add(Swell(cargo + CapsuleIgnition, CutsceneSound.BoosterIgnition, CargoIgnitionLevel, CargoIgnitionSwell));
                cues.Add(Swell(cargo + CapsuleSeparation, CutsceneSound.StageSeparation, CargoSeparationLevel, CargoSeparationSwell));
            }

            float release = PathfinderRelease;
            cues.Add(Sound(release, CutsceneSound.ClampsRelease, 0.6f));
            cues.Add(Sound(release, CutsceneSound.ColdGasPuff, 0.5f));
            cues.Add(Sound(release + CapsuleIgnition, CutsceneSound.BoosterIgnition, 0.7f));
            cues.Add(Sound(release + CapsuleSeparation, CutsceneSound.StageSeparation, 0.6f));
            cues.Add(Sound(release + CapsuleSeparation, CutsceneSound.BoosterFade, 0.55f));
            return Join(cues.ToArray());
        }

        private static CutsceneCue Swell(float at, CutsceneSound sound, float strength, float fadeIn) => new CutsceneCue { At = at, Kind = CueKind.Sound, Sound = sound, Strength = strength, FadeIn = fadeIn };

        // The sounds each shot holds; a layer the next shot lists again carries on, any other fades out at the cut. The quake
        // rumble is no layer any more: it follows the shaking (CutsceneShot.ShakeRumble).
        private static CutsceneLayer[] OpeningLayers(int shot)
        {
            float release = PathfinderRelease;
            float firstCargo = Game.UI.CapsuleDrop.ReleaseAt(0, ReleaseGap);
            float lastCargo = Game.UI.CapsuleDrop.ReleaseAt(OrbitCapsules - 2, ReleaseGap);
            switch (shot)
            {
                case 0:
                    return new[] { Hold(CutsceneSound.OrbitBed, 0f, BedLevel, 3f) };
                case 1:
                    return new[] { Hold(CutsceneSound.OrbitBed, 0f, BedLevel) };
                case 2:
                    // The ship coming in: its hum swells as it nears, its forward thrusters roar while it brakes.
                    return new[]
                    {
                        Hold(CutsceneSound.OrbitBed, 0f, BedLevel), Hold(CutsceneSound.MothershipHum, 1f, HumLevel, 3f),
                        Span(CutsceneSound.BoosterBurn, 0.2f, Game.UI.OrbitSet.ArrivalSeconds, BrakeLevel, 0.4f, 3.5f),
                    };
                case 3:
                    return new[]
                    {
                        Hold(CutsceneSound.OrbitBed, 0f, BedLevel), Hold(CutsceneSound.MothershipHum, 0f, HumLevel),
                        Span(CutsceneSound.MothershipAlarm, 0.2f, release + 0.6f, 0.22f, 0.3f, 0.5f),
                        Span(CutsceneSound.BoosterBurn, firstCargo + CapsuleIgnition, lastCargo + CapsuleSeparation + BurnTail, CargoBurnLevel, CargoBurnFadeIn, CargoBurnFadeOut),
                        Span(CutsceneSound.BoosterBurn, release + CapsuleIgnition, release + CapsuleSeparation + BurnTail, 0.4f, 0.15f, 0.4f),
                        Hold(CutsceneSound.PlasmaRoar, release + CapsuleSeparation + 1f, 0.3f, 2.5f),
                    };
                case 4:
                    return new[]
                    {
                        Span(CutsceneSound.PlasmaRoar, 0f, TouchdownSeconds + 0.05f, 0.55f, 0f, 0.35f), Span(CutsceneSound.CapsuleRattle, 0f, TouchdownSeconds, 0.4f, 0f, 0.3f),
                        Span(CutsceneSound.WindRush, 0f, TouchdownSeconds + 0.2f, 0.6f, 0.3f, 0.4f), Hold(CutsceneSound.WindPlain, 2.2f, WindLevel, 2f),
                        Hold(CutsceneSound.BreathCalm, AppearSeconds, BreathLevel, 1f),
                    };
                case 5:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Span(CutsceneSound.DroneHum, 0.3f, BuildersLand + DroneHandover, DroneHumLevel, 1f, DroneHandover) };
                case 6:
                case 7:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel) };
                case 8:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.CrystalHum, 3.4f, 0.25f, 1.5f) };
                case 9:
                    return new[] { Hold(CutsceneSound.CaveDrips, 0f, DripLevel, 1.5f), Hold(CutsceneSound.CrystalHum, 0f, 0.5f, 1.5f) };
                case 10:
                    return new[] { Hold(CutsceneSound.CaveDrips, 0f, DripLevel), Hold(CutsceneSound.CrystalHum, 0f, 0.4f) };
                case 11:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.BreathCalm, 0f, 0.3f, 1f) };
                case 12:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel) };
                case 13:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.SwarmChitter, BurstSeconds[0], SwarmChitterLevel, 2f) };
                case 14:
                    // The brood is behind the lens in his close-up: its chitter only trails off from the cut.
                    return new[]
                    {
                        Hold(CutsceneSound.WindPlain, 0f, WindLevel), Span(CutsceneSound.SwarmChitter, 0f, 1.6f, ChitterLevel, 0f, 1.4f),
                        Span(CutsceneSound.BreathCalm, 0f, FearFrom + 0.3f, BreathLevel, 0f, 0.3f), Hold(CutsceneSound.BreathFast, FearFrom, FastBreathLevel, 0.4f),
                    };
                case 15:
                    return new[]
                    {
                        Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.SwarmChitter, 0f, ChitterLevel, 1f),
                        Span(CutsceneSound.BreathFast, 0f, GateArrival + 0.3f, FastBreathLevel, 0f, 0.6f), Hold(CutsceneSound.BreathCalm, 7.5f, 0.3f, 2f),
                        Hold(CutsceneSound.RadioStatic, RadioSeconds + 0.2f, StaticLevel, 0.3f),
                    };
                case 16:
                    return new[]
                    {
                        Hold(CutsceneSound.WindPlain, 0f, WindLevel), Span(CutsceneSound.SwarmChitter, 0f, 2f, ChitterLevel, 0f, 1.5f),
                        Span(CutsceneSound.BreathCalm, 0f, 4f, 0.3f, 0f, 2f), Span(CutsceneSound.RadioStatic, 0f, 1f, StaticLevel, 0f, 1f),
                    };
                default:
                    return new CutsceneLayer[0];
            }
        }

        private static CutsceneLayer Hold(CutsceneSound sound, float from, float volume, float fadeIn = 0f) => new CutsceneLayer { Sound = sound, From = from, Volume = volume, FadeIn = fadeIn };

        private static CutsceneLayer Span(CutsceneSound sound, float from, float to, float volume, float fadeIn, float fadeOut) => new CutsceneLayer { Sound = sound, From = from, To = to, Volume = volume, FadeIn = fadeIn, FadeOut = fadeOut };

        private static void WriteLayers(SerializedProperty array, CutsceneLayer[] layers)
        {
            array.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
            {
                SerializedProperty layer = array.GetArrayElementAtIndex(i);
                layer.FindPropertyRelative("Sound").intValue = (int)layers[i].Sound;
                layer.FindPropertyRelative("From").floatValue = layers[i].From;
                layer.FindPropertyRelative("To").floatValue = layers[i].To;
                layer.FindPropertyRelative("Volume").floatValue = layers[i].Volume;
                layer.FindPropertyRelative("FadeIn").floatValue = layers[i].FadeIn;
                layer.FindPropertyRelative("FadeOut").floatValue = layers[i].FadeOut;
            }
        }
    }
}
