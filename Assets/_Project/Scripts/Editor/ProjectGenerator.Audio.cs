using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// The recorded sounds: the library of shared sounds and one voice asset per species, filled from the audio
    /// folders and measured for loudness so every set plays at the same level before the game mixes by kind.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string SfxLibraryPath = DataRoot + "/Audio/SfxLibrary.asset";
        private const string VoicesFolder = DataRoot + "/Audio/Voices";
        private const string AudioRoot = "Assets/_Project/Audio";
        private const string ApprovedFolder = "Approved";
        private const string Kenney = "Kenney_SciFi";
        private const string Lentikula = "Lentikula";
        private const string Freesound = "Freesound";

        // Loudness is the 90th percentile of the short-window RMS, so a long quiet tail does not make a bang count as
        // soft. Every set is brought to the same target; the player's volume constants then set the mix.
        private const float TargetLoudness = 0.1f;
        private const float LoudnessWindowSeconds = 0.05f;
        private const float LoudnessPercentile = 0.9f;

        // Which files fill which set: the folder under Assets/_Project/Audio and the file name without its variant
        // number (explosion_mine matches explosion_mine_01 and explosion_mine_02; a loop matches by its full name).
        private static readonly (string Field, string Folder, string Name)[] LibraryClips =
        {
            ("_pulseShots", Lentikula, "Laser Rifle"),
            ("_heavyShots", Kenney, "laserLarge"),
            ("_rocketLaunches", ApprovedFolder, "raket_lancering"),
            ("_cryoShots", Kenney, "laserRetro"),
            ("_teslaShots", Kenney, "forceField"),
            ("_beamLoops", ApprovedFolder, "laser_beam_loop"),
            ("_railShots", Lentikula, "Laser Beam"),
            // Flak: a sharp metallic crack for the shot and a crunching burst, unlike the cannon and the rocket (pitched below).
            ("_flakShots", Kenney, "impactMetal"),
            ("_flakBursts", Kenney, "explosionCrunch"),
            ("_cannonExplosions", ApprovedFolder + "/Explosions", "explosion_cannon_shell"),
            ("_rocketExplosions", ApprovedFolder + "/Explosions", "explosion_air_rocket"),
            ("_mineExplosions", ApprovedFolder + "/Explosions", "explosion_mine"),
            ("_gateExplosions", ApprovedFolder + "/Explosions", "explosion_gate"),
            ("_baseExplosions", ApprovedFolder + "/Explosions", "explosion_base"),
            ("_towerBreaks", ApprovedFolder + "/Environment", "tower_break"),
            ("_structureHits", ApprovedFolder + "/Environment", "metal_dull_hit"),
            ("_baseAlerts", ApprovedFolder + "/Environment", "base_alert"),
            ("_upgrades", ApprovedFolder + "/Environment", "upgrade"),
            ("_scrapPickups", ApprovedFolder + "/Environment", "scrap_pickup"),
            ("_cash", ApprovedFolder + "/Environment", "cash"),
            ("_freezes", ApprovedFolder + "/Environment", "item_freeze"),
            ("_acidSizzles", ApprovedFolder + "/Environment", "acid_sizzle"),
            ("_steamBursts", ApprovedFolder + "/Environment", "steam_burst"),
            ("_netShots", ApprovedFolder + "/Environment", "net_fire"),
            ("_earthquakes", ApprovedFolder + "/Environment", "earthquake"),
            ("_cartLoops", ApprovedFolder + "/Environment", "rc_cart_loop"),
            ("_fireballLoops", ApprovedFolder + "/Environment", "fireball_roll_loop"),
            ("_heroBlasters", ApprovedFolder + "/Hero", "hero_laser_gun"),
            ("_saberSwings", ApprovedFolder + "/Hero", "hero_saber_swing"),
            ("_saberHits", ApprovedFolder + "/Hero", "hero_saber_hit"),
            ("_saberHums", ApprovedFolder + "/Hero", "hero_saber_hum_loop"),
            ("_heroPulses", ApprovedFolder + "/Hero", "hero_power_pulse"),
            ("_heroSteps", ApprovedFolder + "/Hero", "hero_run_step"),
            ("_heroDeaths", ApprovedFolder + "/Hero", "hero_death"),
            ("_heroFalls", ApprovedFolder + "/Hero", "hero_fall"),
            ("_droneFanLoops", ApprovedFolder + "/Drones", "drone_fan_loop"),
            ("_droneRepairLoops", ApprovedFolder + "/Drones", "drone_repair_loop"),
            ("_droneHarvestLoops", ApprovedFolder + "/Drones", "drone_harvest_loop"),
            ("_droneLasers", ApprovedFolder + "/Drones", "drone_attack_laser"),
            ("_enemyDeaths", Kenney, "slime"),
            ("_heavySteps", ApprovedFolder + "/Environment", "monster_heavy_step"),
            ("_wingLoops", ApprovedFolder + "/Monsters/Flying", "wings_flapping_loop"),
            ("_largeWingLoops", ApprovedFolder + "/Bosses/Flying", "large_wings_flapping_loop_boss"),
            ("_waterLoops", ApprovedFolder + "/Environment", "water_bubbling_loop"),
            ("_lavaLoops", "", "lavabubble"),
            // Stand-in for the rift until Remco records its own: the lava bed, slower (pitched below).
            ("_riftLoops", "", "lavabubble"),
            ("_steamLoops", ApprovedFolder + "/Environment", "steam_loop"),
            ("_leavesLoops", ApprovedFolder + "/Environment", "leaves_wind_loop"),
            ("_icyWindLoops", ApprovedFolder + "/Environment", "icy_wind_loop"),
            ("_bossLaserLoops", Freesound, "freesound_community-laser-weld-103309"),
            ("_bossAlarms", Freesound, "warning"),
            ("_waveDrums", ApprovedFolder + "/Environment", "wave_start_drums"),
            ("_menuMusic", ApprovedFolder + "/Music", "menu_loop"),
            ("_gameMusic", ApprovedFolder + "/Music", "game_calm_loop"),
            ("_bossMusic", ApprovedFolder + "/Music", "game_boss_loop"),
        };

        // Playback speed of the library sets that borrow another sound; one for every set not listed.
        private static readonly Dictionary<string, float> LibraryPitches = new Dictionary<string, float>
        {
            ["_flakShots"] = 1.3f,
            ["_flakBursts"] = 1.25f,
            ["_riftLoops"] = 0.8f,
        };

        // One voice per species. Paths are folders under Approved plus the file name without its variant number.
        private sealed class VoiceSpec
        {
            public string Enemy;
            public string Move;
            public string Steps;
            public float StepSpacing = 0.5f;
            public float Presence = 1f;
            public string Roll;
            public string Screech;
            public string Growl;
            public string Attack;
            public string Impact;
            public string Pulse;
            public string VolleyLaunch;
            public string VolleyImpact;
            public string Rally;
            public string Regrow;
            public string Spawn;
            public string Laser;

            /// <summary>Playback speed of every set of the voice; a stand-in borrowed from another species is pitched to sound its own.</summary>
            public float Pitch = 1f;
        }

        private const string Special = "Bosses/Special/";
        private const float StoneVoicePitch = 0.85f;
        private const string EnvironmentFolder = "Environment/";

        private static readonly VoiceSpec[] Voices =
        {
            new VoiceSpec { Enemy = "Skitter", Move = "Monsters/Ant/ant_walk_loop", Screech = "Monsters/Ant/ant_screech" },
            new VoiceSpec { Enemy = "Stinger", Move = "Monsters/Scorpion/scorpion_walk_loop", Screech = "Monsters/Scorpion/scorpion_screech", Attack = "Monsters/Scorpion/scorpion_acid_shot", Impact = EnvironmentFolder + "acid_sizzle" },
            Bat("Emberbat", "fire"), Bat("Tidebat", "water"), Bat("Mossbat", "moss"), Bat("Dustbat", "sand"), Bat("Frostbat", "frost"), Bat("Mirebat", "forest"),
            new VoiceSpec { Enemy = "Shardback", Move = "Monsters/IceBeetle/ice_beetle_walk_loop", Screech = "Monsters/IceBeetle/ice_beetle_screech", Attack = "Monsters/IceBeetle/ice_beetle_icicle_shot", Impact = "Monsters/IceBeetle/icicle_impact" },
            // Stone (S4): stand-ins until Remco records his own; the ice beetle and the dust bat, pitched lower and heavier.
            new VoiceSpec { Enemy = "Cragback", Pitch = StoneVoicePitch, Move = "Monsters/IceBeetle/ice_beetle_walk_loop", Screech = "Monsters/IceBeetle/ice_beetle_screech", Steps = "Environment/monster_heavy_step", StepSpacing = 0.9f },
            Bat("Shalebat", "sand", StoneVoicePitch),
            new VoiceSpec { Enemy = "Mireleech", Move = "Monsters/Leech/leech_slime_loop", Screech = "Monsters/Leech/leech_screech" },
            new VoiceSpec { Enemy = "Bulwark", Steps = "Monsters/DinoSoldier/dino_soldier_step", StepSpacing = 0.7f, Screech = "Monsters/DinoSoldier/dino_soldier_death" },
            new VoiceSpec { Enemy = "Prowler", Steps = "Monsters/Wolf/wolf_step", StepSpacing = 0.55f, Screech = "Monsters/Wolf/wolf_death" },
            new VoiceSpec { Enemy = "Wisp", Presence = 3f, Move = "Monsters/EnergyBall/energy_ball_static_loop", Screech = "Monsters/EnergyBall/energy_ball_death", Attack = "Monsters/EnergyBall/energy_ball_fire" },
            new VoiceSpec { Enemy = "Skyray", Move = "Monsters/Stingray/stingray_fly_loop", Attack = "Monsters/Stingray/stingray_tailwhip", Screech = "Monsters/Stingray/stingray_death" },
            new VoiceSpec { Enemy = "Oozer", Move = "Monsters/Slug/slug_move_loop", Attack = "Monsters/Slug/slug_attack", Screech = "Monsters/Slug/slug_death" },
            new VoiceSpec { Enemy = "Roller", Presence = 7f, Steps = "Monsters/Armadillo/armadillo_step", StepSpacing = 0.5f, Roll = "Monsters/Armadillo/armadillo_roll_loop", Screech = "Monsters/Armadillo/armadillo_death" },
            new VoiceSpec { Enemy = "BroodMother", Move = "Bosses/Ant/ant_walk_loop_boss", Screech = "Bosses/Ant/ant_screech_boss", Growl = "Bosses/Ant/ant_screech_boss", Pulse = Special + "boss_poison_spray", Spawn = "Bosses/Ant/ant_screech_boss" },
            new VoiceSpec { Enemy = "HiveQueen", Move = "Bosses/Ant/ant_walk_loop_boss", Screech = "Bosses/Ant/ant_screech_boss", Growl = "Bosses/Ant/ant_screech_boss", Pulse = "Monsters/Shared/enemy_power_pulse", Spawn = "Bosses/Ant/ant_screech_boss" },
            new VoiceSpec { Enemy = "AcidEmpress", Move = "Bosses/Scorpion/scorpion_walk_loop_boss", Screech = "Bosses/Scorpion/scorpion_screech_boss", Growl = "Bosses/Scorpion/scorpion_screech_boss", Attack = "Bosses/Scorpion/scorpion_acid_shot_boss", Impact = EnvironmentFolder + "acid_sizzle", VolleyLaunch = Special + "boss_acid_rain_loop", VolleyImpact = EnvironmentFolder + "acid_sizzle", Regrow = Special + "boss_armor_regrow" },
            new VoiceSpec { Enemy = "StormMatriarch", Move = "Bosses/Bat/bat_wings_loop_boss", Screech = "Bosses/Bat/bat_screech_boss", Growl = "Bosses/Bat/bat_screech_boss", Attack = "Bosses/Bat/bat_attack_moss_boss", VolleyLaunch = Special + "boss_bat_bomb_launch", VolleyImpact = Special + "boss_bat_bomb_explode_moss", Spawn = "Bosses/Bat/bat_screech_boss" },
            new VoiceSpec { Enemy = "CrystalWarlord", Steps = "Bosses/DinoSoldier/dino_soldier_step_boss", StepSpacing = 1f, Screech = "Bosses/DinoSoldier/dino_soldier_death_boss", Pulse = "Monsters/Shared/enemy_power_pulse", Regrow = Special + "boss_armor_regrow" },
            new VoiceSpec { Enemy = "PackAlpha", Steps = "Bosses/Wolf/wolf_step_boss", StepSpacing = 0.8f, Screech = "Bosses/Wolf/wolf_death_boss", Growl = Special + "boss_howl", Rally = Special + "boss_howl" },
            new VoiceSpec { Enemy = "CoreWisp", Presence = 2f, Move = "Bosses/EnergyBall/energy_ball_static_loop_boss", Screech = "Bosses/EnergyBall/energy_ball_death_boss", Attack = "Bosses/EnergyBall/energy_ball_fire_boss", Pulse = "Monsters/Shared/enemy_power_pulse", Spawn = "Monsters/Shared/enemy_power_pulse" },
            new VoiceSpec { Enemy = "SkySovereign", Move = "Bosses/Stingray/stingray_fly_loop_boss", Screech = "Bosses/Stingray/stingray_death_boss", Attack = "Bosses/Stingray/stingray_tailwhip_boss", Laser = "Bosses/Stingray/stingray_tail_laser" },
            new VoiceSpec { Enemy = "SlimeMonarch", Move = "Bosses/Slug/slug_move_loop_boss", Attack = "Bosses/Slug/slug_attack_boss", Screech = "Bosses/Slug/slug_death_boss", Growl = "Bosses/Slug/slug_boss_growl", Pulse = Special + "boss_slime_spit", Spawn = Special + "boss_slime_spit" },
            new VoiceSpec { Enemy = "RollerKing", Presence = 4f, Steps = "Bosses/Armadillo/armadillo_step_boss", StepSpacing = 0.8f, Screech = "Bosses/Armadillo/armadillo_death_boss", Roll = "Bosses/Armadillo/armadillo_roll_heavy_loop", Pulse = EnvironmentFolder + "earthquake" },
        };

        // The element bats share wings and screech; only the attack carries the element.
        private static VoiceSpec Bat(string enemy, string element, float pitch = 1f) => new VoiceSpec
        {
            Enemy = enemy,
            Pitch = pitch,
            Move = "Monsters/Bat/bat_wings_loop",
            Screech = "Monsters/Bat/bat_screech",
            Attack = "Monsters/Bat/bat_attack_" + element,
        };

        private static void CreateSfxLibrary()
        {
            AssetDatabase.Refresh();
            var library = LoadOrCreate<SfxLibrary>(SfxLibraryPath);
            var so = new SerializedObject(library);
            foreach (var entry in LibraryClips)
            {
                FillSet(so.FindProperty(entry.Field), FindClips(string.IsNullOrEmpty(entry.Folder) ? AudioRoot : $"{AudioRoot}/{entry.Folder}", entry.Name), LibraryPitches.TryGetValue(entry.Field, out float pitch) ? pitch : 1f);
            }

            var voices = new List<Object>();
            foreach (VoiceSpec spec in Voices)
            {
                voices.Add(CreateVoice(spec));
            }

            SetReferences(so.FindProperty("_voices"), voices.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static CreatureVoice CreateVoice(VoiceSpec spec)
        {
            var voice = LoadOrCreate<CreatureVoice>($"{VoicesFolder}/Voice_{spec.Enemy}.asset");
            var so = new SerializedObject(voice);
            so.FindProperty("_presence").floatValue = spec.Presence;
            FillSet(so.FindProperty("_moveLoop"), ApprovedClips(spec.Move), spec.Pitch);
            FillSet(so.FindProperty("_steps"), ApprovedClips(spec.Steps), spec.Pitch);
            so.FindProperty("_stepSpacing").floatValue = spec.StepSpacing;
            FillSet(so.FindProperty("_rollLoop"), ApprovedClips(spec.Roll), spec.Pitch);
            FillSet(so.FindProperty("_screeches"), ApprovedClips(spec.Screech), spec.Pitch);
            FillSet(so.FindProperty("_growls"), ApprovedClips(spec.Growl), spec.Pitch);
            FillSet(so.FindProperty("_attacks"), ApprovedClips(spec.Attack), spec.Pitch);
            FillSet(so.FindProperty("_impacts"), ApprovedClips(spec.Impact), spec.Pitch);
            FillSet(so.FindProperty("_pulse"), ApprovedClips(spec.Pulse), spec.Pitch);
            FillSet(so.FindProperty("_volleyLaunch"), ApprovedClips(spec.VolleyLaunch), spec.Pitch);
            FillSet(so.FindProperty("_volleyImpact"), ApprovedClips(spec.VolleyImpact), spec.Pitch);
            FillSet(so.FindProperty("_rally"), ApprovedClips(spec.Rally), spec.Pitch);
            FillSet(so.FindProperty("_regrow"), ApprovedClips(spec.Regrow), spec.Pitch);
            FillSet(so.FindProperty("_spawn"), ApprovedClips(spec.Spawn), spec.Pitch);
            FillSet(so.FindProperty("_laserStart"), ApprovedClips(spec.Laser), spec.Pitch);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(voice);

            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{spec.Enemy}.asset");
            if (enemy == null)
            {
                Debug.LogWarning($"No enemy asset for voice {spec.Enemy}.");
                return voice;
            }

            var enemySo = new SerializedObject(enemy);
            enemySo.FindProperty("_voice").objectReferenceValue = voice;
            enemySo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            return voice;
        }

        private static List<AudioClip> ApprovedClips(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new List<AudioClip>();
            }

            int slash = path.LastIndexOf('/');
            return FindClips($"{AudioRoot}/{ApprovedFolder}/{path.Substring(0, slash)}", path.Substring(slash + 1));
        }

        // The clips in a folder whose name is the given name, or the name followed by a space or underscore and a number.
        private static List<AudioClip> FindClips(string folder, string name)
        {
            var clips = new List<AudioClip>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"Audio folder missing: {folder}");
                return clips;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetDirectoryName(path).Replace('\\', '/') != folder || !IsVariant(System.IO.Path.GetFileNameWithoutExtension(path), name))
                {
                    continue;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (clips.Count == 0)
            {
                Debug.LogWarning($"No audio clips named {name} in {folder}.");
            }

            return clips;
        }

        private static bool IsVariant(string fileName, string name)
        {
            if (fileName == name)
            {
                return true;
            }

            if (fileName.Length <= name.Length + 1 || !fileName.StartsWith(name, StringComparison.Ordinal))
            {
                return false;
            }

            char separator = fileName[name.Length];
            if (separator != '_' && separator != ' ')
            {
                return false;
            }

            for (int i = name.Length + 1; i < fileName.Length; i++)
            {
                if (!char.IsDigit(fileName[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void FillSet(SerializedProperty set, List<AudioClip> clips, float pitch = 1f)
        {
            SetReferences(set.FindPropertyRelative("_clips"), clips.ToArray());
            set.FindPropertyRelative("_pitch").floatValue = pitch;
            set.FindPropertyRelative("_gain").floatValue = MeasureGain(clips);
        }

        // One gain for the whole set, from the average loudness of its variants, so they keep their relative weight.
        private static float MeasureGain(List<AudioClip> clips)
        {
            float total = 0f;
            int measured = 0;
            foreach (AudioClip clip in clips)
            {
                float loudness = Loudness(clip);
                if (loudness > 0f)
                {
                    total += loudness;
                    measured++;
                }
            }

            if (measured == 0)
            {
                return 1f;
            }

            return Mathf.Clamp(TargetLoudness / (total / measured), SoundSet.MinGain, SoundSet.MaxGain);
        }

        private static float Loudness(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0))
            {
                Debug.LogWarning($"Could not read samples of {clip.name}; its set keeps gain 1.");
                return 0f;
            }

            int window = Mathf.Max(1, Mathf.RoundToInt(clip.frequency * LoudnessWindowSeconds)) * clip.channels;
            var windows = new List<float>(data.Length / window + 1);
            for (int start = 0; start + window <= data.Length; start += window)
            {
                double sum = 0d;
                for (int i = start; i < start + window; i++)
                {
                    sum += data[i] * data[i];
                }

                windows.Add((float)Math.Sqrt(sum / window));
            }

            if (windows.Count == 0)
            {
                return 0f;
            }

            windows.Sort();
            return windows[Mathf.Clamp(Mathf.FloorToInt(windows.Count * LoudnessPercentile), 0, windows.Count - 1)];
        }
    }
}
