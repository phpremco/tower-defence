using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A cutscene as data: its shots in order, the cast, and where it hands the player over when it ends. The asset
    /// name is its id. The first launch plays the opening cutscene and drops into the first campaign level on the
    /// handoff difficulty; that handoff skips the loadout panel, because the first level offers its own set.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Cutscene", fileName = "Cutscene_")]
    public sealed class CutsceneDefinition : ScriptableObject
    {
        [SerializeField] private CutsceneShot[] _shots = new CutsceneShot[0];
        [SerializeField] private CutsceneCast _cast;
        [SerializeField] private CutsceneHandoff _handoff = CutsceneHandoff.FirstCampaignLevel;
        [Tooltip("The difficulty the first campaign level starts on after the cutscene; must be one of the campaign's.")]
        [SerializeField] private DifficultyProfile _handoffDifficulty;
        [Tooltip("The camera's drift and how hard an impact cue may kick it; no shake may last.")]
        [SerializeField] private CutsceneCameraFeel _cameraFeel = CutsceneCameraFeel.Calm;
        [Tooltip("The cutscene's own sounds and music; empty borrows everything from the game's sound library and level music.")]
        [SerializeField] private CutsceneSoundBank _sounds;

        public string Id => name;
        public IReadOnlyList<CutsceneShot> Shots => _shots;
        public CutsceneCast Cast => _cast;
        public CutsceneHandoff Handoff => _handoff;
        public DifficultyProfile HandoffDifficulty => _handoffDifficulty;
        public CutsceneCameraFeel CameraFeel => _cameraFeel;
        public CutsceneSoundBank Sounds => _sounds;

        /// <summary>The run time when every dialogue closes at once, as in auto-advance.</summary>
        public float AutoSeconds(int fromShot = 0, int toShot = int.MaxValue)
        {
            float total = 0f;
            for (int i = Mathf.Max(0, fromShot); i < _shots.Length && i <= toShot; i++)
            {
                total += _shots[i].AutoLength;
            }

            return total;
        }

        /// <summary>The camera length of the first shot with this action, or zero; sets time their clocks across shots with it.</summary>
        public float CameraLengthOf(CutsceneAction action)
        {
            foreach (CutsceneShot shot in _shots)
            {
                if (shot.Action == action)
                {
                    return shot.CameraLength;
                }
            }

            return 0f;
        }

        /// <summary>The first level of the campaign: where the story plays out and where the first launch drops in.</summary>
        public static LevelDefinition FirstCampaignLevel(CampaignCatalog catalog)
        {
            if (catalog == null)
            {
                return null;
            }

            int first = catalog.FirstNode(0);
            return first >= 0 ? catalog.Nodes[first].Level : null;
        }

        /// <summary>The level the field shots play on: the cast's choice, otherwise the first campaign level.</summary>
        public LevelDefinition FieldLevel(CampaignCatalog catalog) => _cast != null && _cast.FieldLevel != null ? _cast.FieldLevel : FirstCampaignLevel(catalog);

        /// <summary>Adds every data problem to the list; returns true when there is none. The catalog is optional.</summary>
        public bool Validate(List<string> errors, CampaignCatalog catalog = null)
        {
            int before = errors.Count;
            if (_shots == null || _shots.Length == 0)
            {
                errors.Add("has no shots.");
                return false;
            }

            if (_cast == null)
            {
                errors.Add("has no cast.");
            }

            for (int i = 0; i < _shots.Length; i++)
            {
                ValidateShot(_shots[i], i + 1, errors);
                ValidateKicks(_shots[i], i + 1, errors);
            }

            if (_handoff == CutsceneHandoff.FirstCampaignLevel)
            {
                ValidateHandoff(errors, catalog);
            }

            if (_sounds != null)
            {
                _sounds.Validate(errors);
            }

            return errors.Count == before;
        }

        private void ValidateHandoff(List<string> errors, CampaignCatalog catalog)
        {
            CameraKey[] lastKeys = _shots[_shots.Length - 1].Camera;
            if (lastKeys == null || lastKeys.Length == 0 || lastKeys[lastKeys.Length - 1].Mode != CameraKeyMode.MatchLevelRig)
            {
                errors.Add("hands over to the first level, so its last camera key must aim at the level camera (MatchLevelRig).");
            }

            if (_handoffDifficulty == null)
            {
                errors.Add("hands over to the first level without a difficulty.");
            }
            else if (catalog != null && !Contains(catalog.Difficulties, _handoffDifficulty))
            {
                errors.Add($"hands over on '{_handoffDifficulty.name}', which is not a campaign difficulty.");
            }

            if (catalog != null && FirstCampaignLevel(catalog) == null)
            {
                errors.Add("hands over to the first level, but the campaign has none.");
            }
        }

        private static void ValidateShot(CutsceneShot shot, int number, List<string> errors)
        {
            if (!CutsceneActions.Supports(shot.Set, shot.Action))
            {
                errors.Add($"shot {number}: the {shot.Set} set cannot show {shot.Action}.");
            }

            if (shot.Timing == ShotTiming.AfterDialogue && shot.Dialogue == null)
            {
                errors.Add($"shot {number} waits for a dialogue but has none.");
            }

            if (shot.Timing == ShotTiming.Fixed && shot.Seconds <= 0f)
            {
                errors.Add($"shot {number} has no length.");
            }

            if (shot.Timing == ShotTiming.Fixed && shot.Dialogue != null && shot.DialogueDelay >= shot.Seconds)
            {
                errors.Add($"shot {number} ends before its dialogue starts.");
            }

            if (shot.Camera == null || shot.Camera.Length == 0)
            {
                errors.Add($"shot {number} has no camera key.");
                return;
            }

            for (int k = 1; k < shot.Camera.Length; k++)
            {
                if (shot.Camera[k].At < shot.Camera[k - 1].At)
                {
                    errors.Add($"shot {number}: camera keys are not sorted by time.");
                    break;
                }
            }

            if (shot.Layers != null)
            {
                foreach (CutsceneLayer layer in shot.Layers)
                {
                    layer.Validate(number, errors);
                }
            }

            if (shot.Cues == null)
            {
                return;
            }

            foreach (CutsceneCue cue in shot.Cues)
            {
                if (shot.Timing == ShotTiming.Fixed && cue.At > shot.Seconds)
                {
                    errors.Add($"shot {number}: a cue at {cue.At:0.##} s comes after the shot has ended.");
                }

                if (cue.Kind == CueKind.Sound && cue.Sound == CutsceneSound.None && cue.Clip == null)
                {
                    errors.Add($"shot {number}: a sound cue without a sound.");
                }
            }
        }

        // No sustained shake: one kick per shot at most, never stronger than the camera feel allows.
        private void ValidateKicks(CutsceneShot shot, int number, List<string> errors)
        {
            if (shot.Cues == null)
            {
                return;
            }

            int kicks = 0;
            foreach (CutsceneCue cue in shot.Cues)
            {
                if (cue.Kind != CueKind.Shake)
                {
                    continue;
                }

                kicks++;
                if (cue.Strength > _cameraFeel.MaxKick)
                {
                    errors.Add($"shot {number}: a kick of {cue.Strength:0.##} is stronger than the camera allows ({_cameraFeel.MaxKick:0.##}).");
                }
            }

            if (kicks > 1)
            {
                errors.Add($"shot {number} kicks the camera {kicks} times; one kick per shot at most.");
            }
        }

        private static bool Contains(IReadOnlyList<DifficultyProfile> list, DifficultyProfile item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
            {
                Debug.LogWarning($"Cutscene '{name}':\n{string.Join("\n", errors)}", this);
            }
        }
    }
}
