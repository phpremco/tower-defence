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
            new IntroSlot(CutsceneSound.WildlifeCall, IntroEnvironment, "env_wildlife_call", channel: CutsceneSoundChannel.Ambience),
        };

        // The shots of the opening in order; the sound tables below are written per shot and checked against it.
        private static readonly CutsceneAction[] OpeningOrder =
        {
            CutsceneAction.EarthDrift, CutsceneAction.ScanPlanet, CutsceneAction.CapsulesFall, CutsceneAction.PathfinderLands,
            CutsceneAction.BaseUnfolds, CutsceneAction.DrillDown, CutsceneAction.DronesHarvest, CutsceneAction.CoreIgnites,
            CutsceneAction.CrystalsTremble, CutsceneAction.EyesOpen, CutsceneAction.GroundShivers, CutsceneAction.GroundShivers,
            CutsceneAction.SwarmPours, CutsceneAction.MercerReacts, CutsceneAction.GateStand, CutsceneAction.Handoff,
        };

        // Levels of the held layers before the slot gains (which bring every file to one loudness) and the player's mix.
        private const float BedLevel = 0.55f;
        private const float WindLevel = 0.12f;
        private const float HumLevel = 0.45f;
        private const float BreathLevel = 0.35f;
        private const float FastBreathLevel = 0.45f;
        private const float ChitterLevel = 0.25f;
        private const float SwarmChitterLevel = 0.35f;
        // The drips are sparse, so their measured gain is high: a low level keeps them a background detail.
        private const float DripLevel = 0.2f;
        private const float StaticLevel = 0.15f;
        private const float IntroStepVolume = 0.4f;
        private const float DroneHumLevel = 0.3f;

        // The builders land beside the base over the last 0.08 of the 18 s timelapse (FieldBase.ParkShare); the landing
        // clip starts at the hum's level, so the hum fades out under its first half second.
        private const float BuildersLand = 16.6f;
        private const float DroneHandover = 0.5f;

        // The orbit shot: the bay doors open as it fades in, each capsule's clamps let go with a puff of cold gas, the booster
        // lights CapsuleIgnition later and separates CapsuleSeparation after its release; the burn loop covers the burns.
        private const float CapsuleSeparation = 3.3f;
        private const float BurnFrom = 2.9f;
        private const float BurnTo = 15f;
        private static readonly float[] ReleaseStrength = { 0.6f, 0.5f, 0.45f };

        // The landing (FieldSet: the fall is the first 0.2 of the 12 s shot, the retro jets fire over its last part, the
        // hatch steams from 0.255 and opens at 0.27, Spencer appears at 0.31).
        private const float TouchdownSeconds = 2.4f;
        private static readonly float[] RetroBursts = { 1.35f, 1.75f, 2.1f };
        private const float HatchSteamSeconds = 3.06f;
        private const float HatchOpenSeconds = 3.24f;
        private const float AppearSeconds = 3.9f;

        // When the brood has seen him (MercerReacts: the turn at 0.6 s), he arrives at the gate (GateStand: the run is the
        // first 0.42 of 10 s) and Voss opens the channel (the gate dialogue at 6.5 s).
        private const float FearFrom = 0.9f;
        private const float GateArrival = 4.3f;
        private const float RadioSeconds = 6.3f;
        private const float RetreatSeconds = 1f;

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

        // The one-shots of each shot (sounds and the camera's kicks), in the shot's own seconds.
        private static CutsceneCue[] OpeningCues(int shot)
        {
            switch (shot)
            {
                case 1:
                    return new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.5f), Sound(4f, CutsceneSound.RadarPing, 0.4f) };
                case 2:
                    return OrbitCues();
                case 3:
                    return Join(
                        new[]
                        {
                            Sound(RetroBursts[0], CutsceneSound.RetroBurst, 0.55f), Sound(RetroBursts[1], CutsceneSound.RetroBurst, 0.5f), Sound(RetroBursts[2], CutsceneSound.RetroBurst, 0.55f),
                            Sound(TouchdownSeconds, CutsceneSound.Touchdown, 0.9f), Shake(TouchdownSeconds, LandingKick),
                            Sound(HatchSteamSeconds, CutsceneSound.PressureRelease, 0.55f), Sound(HatchOpenSeconds, CutsceneSound.HatchOpen, 0.5f),
                            // The right boot on the ramp is metal, the rest is the plain's gravel.
                            Sound(LandingExitSteps[0], CutsceneSound.StepMetal, IntroStepVolume),
                            Sound(8.7f, CutsceneSound.BeaconBeep, 0.45f), Sound(10.2f, CutsceneSound.BeaconBeep, 0.4f), Sound(10.6f, CutsceneSound.WildlifeCall, 0.35f),
                        },
                        Footfalls(LandingExitSteps, 1, CutsceneSound.StepGravel, IntroStepVolume), Footfalls(LandingWalkOver, CutsceneSound.StepGravel, IntroStepVolume));
                case 4:
                    // As the flock settles beside the finished base, the landing spin-down takes over from the hum.
                    return new[] { Sound(12f, CutsceneSound.WildlifeCall, 0.3f), Sound(BuildersLand, CutsceneSound.DroneLand, 0.45f) };
                case 5:
                    return new[] { Sound(0.3f, CutsceneSound.DrillStart, 0.5f), Sound(0.6f, CutsceneSound.BuildClank, 0.45f), Sound(DrillContactSeconds, CutsceneSound.Earthquake, 0.8f), Shake(DrillContactSeconds, DrillKick) };
                case 6:
                    return new[] { Sound(0.5f, CutsceneSound.DroneHarvest, 0.4f) };
                case 7:
                    return new[] { Sound(3.4f, CutsceneSound.CoreIgnite, 1f) };
                case 8:
                    // The swell peaks with its rock cracks about 4.5 s in, where the old quake hit was.
                    return new[] { Sound(0.5f, CutsceneSound.QuakeSwell, 0.8f) };
                case 9:
                    return new[]
                    {
                        Sound(1.2f, CutsceneSound.EyesOpenWet, 0.5f), Sound(1.8f, CutsceneSound.EyesOpenWet, 0.4f), Sound(2.5f, CutsceneSound.Growl, 0.5f),
                        Sound(2.9f, CutsceneSound.EyesOpenWet, 0.45f), Sound(6f, CutsceneSound.Growl, 0.8f),
                    };
                case 10:
                    return new[] { Sound(0.3f, CutsceneSound.QuakeSwell, 0.6f) };
                case 11:
                    return new[] { Sound(0.8f, CutsceneSound.RadarPing, 0.6f), Sound(2.6f, CutsceneSound.RadarPing, 0.6f), Sound(4.5f, CutsceneSound.RadarPing, 0.7f), Sound(6.6f, CutsceneSound.BaseAlert, 0.8f) };
                case 12:
                    // The burrows tear open one after another (the first kicks the camera), the brood hisses into its places,
                    // the Queen roars. The war drums of the old cut are gone: no music until Remco picks it.
                    return new[]
                    {
                        Sound(BurstSeconds[0], CutsceneSound.GroundTear, 0.9f), Shake(BurstSeconds[0], BurstKick), Sound(BurstSeconds[1], CutsceneSound.GroundTear, 0.75f),
                        Sound(BurstSeconds[2], CutsceneSound.GroundTear, 0.8f), Sound(4.6f, CutsceneSound.HeavySteps, 0.45f), Sound(5.2f, CutsceneSound.Growl, 0.6f),
                        Sound(5.9f, CutsceneSound.HeavySteps, 0.4f), Sound(6.4f, CutsceneSound.Growl, 0.5f), Sound(QueenScreamSeconds, CutsceneSound.QueenRoar, 1f),
                    };
                case 13:
                    // The gasp, the holster and the charge follow his acting clips (FieldSet), not the clock.
                    return new[] { Sound(0.6f, CutsceneSound.Growl, 0.7f), Sound(1.8f, CutsceneSound.HeavySteps, 0.4f), Sound(3.7f, CutsceneSound.Growl, 0.85f) };
                case 14:
                    return Join(
                        new[] { Sound(0.4f, CutsceneSound.BossAlarm, 0.5f), Sound(GateArrival, CutsceneSound.Panting, 0.55f), Sound(5.4f, CutsceneSound.BuildClank, 0.35f), Sound(RadioSeconds, CutsceneSound.RadioOpen, 0.55f) },
                        Footfalls(GateRun, CutsceneSound.StepStone, IntroStepVolume));
                case 15:
                    return new[]
                    {
                        Sound(HandoffRoarSeconds, CutsceneSound.QueenRoar, 1f), Sound(0.8f, CutsceneSound.Growl, 0.7f), Sound(RetreatSeconds, CutsceneSound.SwarmRetreat, 0.7f),
                        Sound(1.7f, CutsceneSound.HeavySteps, 0.6f), Sound(2.2f, CutsceneSound.HeavySteps, 0.55f), Sound(2.7f, CutsceneSound.HeavySteps, 0.5f),
                        Sound(3.5f, CutsceneSound.Emerge, 0.65f), Sound(4.8f, CutsceneSound.Rumble, 0.5f),
                    };
                default:
                    return new CutsceneCue[0];
            }
        }

        private static CutsceneCue[] OrbitCues()
        {
            var cues = new List<CutsceneCue> { Sound(0.1f, CutsceneSound.BayDoors, 0.6f) };
            for (int i = 0; i < CapsuleRelease.Length; i++)
            {
                float release = CapsuleRelease[i];
                float strength = ReleaseStrength[i];
                cues.Add(Sound(release, CutsceneSound.ClampsRelease, strength));
                cues.Add(Sound(release, CutsceneSound.ColdGasPuff, strength * 0.8f));
                cues.Add(Sound(release + CapsuleIgnition, CutsceneSound.BoosterIgnition, strength + 0.1f));
                cues.Add(Sound(release + CapsuleSeparation, CutsceneSound.StageSeparation, strength));
            }

            // The first booster, cut loose, fades away below.
            cues.Add(Sound(CapsuleRelease[0] + CapsuleSeparation, CutsceneSound.BoosterFade, 0.55f));
            return Join(cues.ToArray());
        }

        // The sounds each shot holds; a layer the next shot lists again carries on, any other fades out at the cut.
        private static CutsceneLayer[] OpeningLayers(int shot)
        {
            switch (shot)
            {
                case 0:
                    return new[] { Hold(CutsceneSound.OrbitBed, 0f, BedLevel, 3f) };
                case 1:
                    return new[] { Hold(CutsceneSound.OrbitBed, 0f, BedLevel) };
                case 2:
                    return new[]
                    {
                        Hold(CutsceneSound.OrbitBed, 0f, BedLevel), Hold(CutsceneSound.MothershipHum, 0f, HumLevel, 1.5f),
                        Span(CutsceneSound.MothershipAlarm, 0.4f, 3.4f, 0.22f, 0.3f, 0.8f), Span(CutsceneSound.BoosterBurn, BurnFrom, BurnTo, 0.4f, 0.2f, 3f),
                        Hold(CutsceneSound.PlasmaRoar, 6f, 0.3f, 3f),
                    };
                case 3:
                    return new[]
                    {
                        Span(CutsceneSound.PlasmaRoar, 0f, TouchdownSeconds + 0.05f, 0.55f, 0f, 0.35f), Span(CutsceneSound.CapsuleRattle, 0f, TouchdownSeconds, 0.4f, 0f, 0.3f),
                        Span(CutsceneSound.WindRush, 0f, TouchdownSeconds + 0.2f, 0.6f, 0.3f, 0.4f), Hold(CutsceneSound.WindPlain, 2.2f, WindLevel, 2f),
                        Hold(CutsceneSound.BreathCalm, AppearSeconds, BreathLevel, 1f),
                    };
                case 4:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Span(CutsceneSound.DroneHum, 0.3f, BuildersLand + DroneHandover, DroneHumLevel, 1f, DroneHandover) };
                case 5:
                case 6:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel) };
                case 7:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.CrystalHum, 3.4f, 0.25f, 1.5f) };
                case 8:
                    return new[] { Hold(CutsceneSound.CaveDrips, 0f, DripLevel, 1.5f), Hold(CutsceneSound.CrystalHum, 0f, 0.5f, 1.5f), Hold(CutsceneSound.QuakeLoop, 0.3f, 0.45f, 2f) };
                case 9:
                    return new[] { Hold(CutsceneSound.CaveDrips, 0f, DripLevel), Hold(CutsceneSound.CrystalHum, 0f, 0.4f), Hold(CutsceneSound.QuakeLoop, 0f, 0.3f) };
                case 10:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.QuakeLoop, 0f, 0.4f, 1f), Hold(CutsceneSound.BreathCalm, 0f, 0.3f, 1f) };
                case 11:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.QuakeLoop, 0f, 0.3f) };
                case 12:
                    return new[] { Hold(CutsceneSound.WindPlain, 0f, WindLevel), Span(CutsceneSound.QuakeLoop, 0f, 3.2f, 0.45f, 0f, 1f), Hold(CutsceneSound.SwarmChitter, BurstSeconds[0], SwarmChitterLevel, 2f) };
                case 13:
                    return new[]
                    {
                        Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.SwarmChitter, 0f, ChitterLevel),
                        Span(CutsceneSound.BreathCalm, 0f, 1.2f, BreathLevel, 0f, 0.5f), Hold(CutsceneSound.BreathFast, FearFrom, FastBreathLevel, 0.6f),
                    };
                case 14:
                    return new[]
                    {
                        Hold(CutsceneSound.WindPlain, 0f, WindLevel), Hold(CutsceneSound.SwarmChitter, 0f, ChitterLevel),
                        Span(CutsceneSound.BreathFast, 0f, GateArrival + 0.3f, FastBreathLevel, 0f, 0.6f), Hold(CutsceneSound.BreathCalm, 7.5f, 0.3f, 2f),
                        Hold(CutsceneSound.RadioStatic, RadioSeconds + 0.2f, StaticLevel, 0.3f),
                    };
                case 15:
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

        private static CutsceneCue[] Footfalls(float[] steps, int first, CutsceneSound sound, float strength)
        {
            var cues = new CutsceneCue[steps.Length - first];
            for (int i = first; i < steps.Length; i++)
            {
                cues[i - first] = Sound(steps[i], sound, strength);
            }

            return cues;
        }

        private static CutsceneCue[] Footfalls((float From, float To, float Gap) walk, CutsceneSound sound, float strength)
        {
            var steps = new List<CutsceneCue>();
            for (float at = walk.From; at <= walk.To; at += walk.Gap)
            {
                steps.Add(Sound(at, sound, strength));
            }

            return steps.ToArray();
        }

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
