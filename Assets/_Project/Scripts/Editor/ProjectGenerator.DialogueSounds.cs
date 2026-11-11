using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 43: the dialogue box's sounds from Remco's approved files in Audio/Approved/Dialogue: the walkie-talkie opener,
    /// the typing cadence and each speaker's six typing blips (Typing/&lt;voice&gt;_01..06). Reads and writes existing assets
    /// only, so it also runs on its own from the menu; the full generator runs it after the sound library.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string DialogueSoundsPath = DataRoot + "/Audio/DialogueSounds.asset";
        private const string DialogueAudio = ApprovedFolder + "/Dialogue";
        private const string TypingAudio = DialogueAudio + "/Typing";
        private const string OpenerName = "open_static_squelch";
        private const int BlipEveryLetters = 2;
        // A speaker without a set of its own types with ORA's blips (the neutral station voice).
        private const string DefaultVoice = "ora";

        // Which typing voice each speaker asset uses (the hero's asset keeps its old id, Mercer, and speaks as Spencer).
        private static readonly (string Speaker, string Voice)[] SpeakerVoices =
        {
            ("Speaker_Mercer", "spencer"), ("Speaker_Voss", "voss"), ("Speaker_Ora", "ora"), ("Speaker_Nti", "nti"), ("Speaker_Pioneer1", "pioneer1"),
        };

        /// <summary>Fills the dialogue sounds, hands them to the sound library and gives every speaker its blips. Safe to run again.</summary>
        [MenuItem("Tools/Generate/Dialogue Sounds")]
        public static void GenerateDialogueSounds()
        {
            var sounds = LoadOrCreate<DialogueSounds>(DialogueSoundsPath);
            var so = new SerializedObject(sounds);
            FillSet(so.FindProperty("_opener"), FindClips($"{AudioRoot}/{DialogueAudio}", OpenerName));
            FillSet(so.FindProperty("_defaultBlips"), FindClips($"{AudioRoot}/{TypingAudio}", DefaultVoice));
            so.FindProperty("_blipEveryLetters").intValue = BlipEveryLetters;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sounds);

            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryPath);
            if (library == null)
            {
                throw new System.InvalidOperationException("The sound library is missing: run the full generator once first.");
            }

            var librarySo = new SerializedObject(library);
            librarySo.FindProperty("_dialogue").objectReferenceValue = sounds;
            librarySo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);

            foreach ((string speakerName, string voice) in SpeakerVoices)
            {
                var speaker = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/{speakerName}.asset");
                if (speaker == null)
                {
                    Debug.LogWarning($"No speaker {speakerName} for the {voice} typing voice.");
                    continue;
                }

                var speakerSo = new SerializedObject(speaker);
                List<AudioClip> blips = FindClips($"{AudioRoot}/{TypingAudio}", voice);
                FillSet(speakerSo.FindProperty("_typingBlips"), blips);
                speakerSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(speaker);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Generated the dialogue sounds.");
        }
    }
}
