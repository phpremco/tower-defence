using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the story: the story asset, the speakers and the level dialogues.</summary>
    public static partial class ProjectGenerator
    {
        private const string StoryAssetPath = DataRoot + "/Story/Story.asset";
        private const string SpeakerFolder = DataRoot + "/Story/Speakers";
        private const string DialogueFolder = DataRoot + "/Story/Dialogues";

        // Role colours stay in the cool, technical range of our side; the aliens never speak.
        private static readonly Color OraColour = new Color(0.35f, 0.85f, 1f);
        private static readonly Color VossColour = new Color(0.82f, 0.9f, 1f);
        private static readonly Color MercerColour = new Color(1f, 0.82f, 0.3f);
        private static readonly Color NtiColour = new Color(0.62f, 0.7f, 0.85f);
        private static readonly Color PioneerColour = new Color(0.55f, 1f, 0.8f);

        /// <summary>One line as the generator writes it: speaker plus styled segments.</summary>
        private readonly struct LineSpec
        {
            public LineSpec(SpeakerDefinition speaker, DialogueSegment[] segments, float glitch)
            {
                Speaker = speaker;
                Segments = segments;
                Glitch = glitch;
            }

            public SpeakerDefinition Speaker { get; }
            public DialogueSegment[] Segments { get; }
            public float Glitch { get; }
        }

        // The story asset the campaign carries; the "Cutscene" step fills in its opening cutscene.
        private static void CreateStory(CampaignCatalog campaign)
        {
            var story = LoadOrCreate<StoryDefinition>(StoryAssetPath);
            var catalog = new SerializedObject(campaign);
            catalog.FindProperty("_story").objectReferenceValue = story;
            catalog.ApplyModifiedPropertiesWithoutUndo();
        }

        // Speakers and the first level's dialogues. The English text follows Docs/STORY.md; lines marked "proposed"
        // are not in the script yet and wait for Remco's approval.
        private static void GenerateLevelStory(GeneratorContext context)
        {
            SpeakerDefinition ora = CreateSpeaker("Ora", "ORA", OraColour, SpeakerFrame.Hologram, PortraitModel.Hologram);
            SpeakerDefinition voss = CreateSpeaker("Voss", "Dr. Elena Voss", VossColour, SpeakerFrame.Portrait, PortraitModel.Scientist);
            SpeakerDefinition mercer = CreateSpeaker("Mercer", "Spencer", MercerColour, SpeakerFrame.Portrait, PortraitModel.Hero);
            CreateSpeaker("Nti", "Nova Terra Industries", NtiColour, SpeakerFrame.Transmission, PortraitModel.Emblem);
            CreateSpeaker("Pioneer1", "Pioneer-1", PioneerColour, SpeakerFrame.Log, PortraitModel.Probe);

            CreateDialogue("Dlg_L01_Start",
                Say(ora, "Hostile wave approaching. Place turrets along the path."),
                Say(mercer, "And whatever gets through is mine."));
            // Proposed.
            CreateDialogue("Dlg_L01_Wave2",
                Say(ora, "Second wave detected. Larger signature."),
                Say(mercer, "They're not testing us anymore."));
            // Proposed.
            CreateDialogue("Dlg_L01_Boss",
                Say(ora, "Large contact on the main road."),
                Say(mercer, "That one's mine too. Keep the guns on it."));
            // Proposed, after "the Burrowers are awake" in the script.
            CreateDialogue("Dlg_L01_Won",
                Say(ora, "Perimeter holding. Wave repelled."),
                Say(mercer, "We woke something up down there."));

            CreateCrystalDialogues(ora, voss, mercer, context);

            foreach (SpecLevel level in context.CampaignLevels)
            {
                WriteTriggers(level.Level, SpecTriggers(level.Spec, context));
            }
        }

        // A spec names its dialogues and creatures by asset name; a name that resolves to nothing stops the run.
        private static StoryTrigger[] SpecTriggers(CampaignLevelSpec spec, GeneratorContext context)
        {
            var triggers = new StoryTrigger[spec.Story.Length];
            for (int i = 0; i < triggers.Length; i++)
            {
                StorySpec story = spec.Story[i];
                string path = $"{DialogueFolder}/{story.Dialogue}.asset";
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
                if (dialogue == null)
                {
                    throw new System.InvalidOperationException($"{spec.Key}: story trigger {i + 1} names dialogue '{story.Dialogue}', which does not exist at {path}.");
                }

                Game.Enemies.EnemyDefinition enemy = null;
                if (!string.IsNullOrEmpty(story.Enemy))
                {
                    enemy = context.Bestiary.ById(story.Enemy);
                    if (enemy == null)
                    {
                        throw new System.InvalidOperationException($"{spec.Key}: story trigger {i + 1} names creature '{story.Enemy}', which does not exist.");
                    }
                }

                Game.PathObjects.PathObjectDefinition pathObject = null;
                if (!string.IsNullOrEmpty(story.PathObject))
                {
                    pathObject = System.Array.Find(context.PathObjects, p => p != null && p.name == story.PathObject);
                    if (pathObject == null)
                    {
                        throw new System.InvalidOperationException($"{spec.Key}: story trigger {i + 1} names path object '{story.PathObject}', which does not exist.");
                    }
                }

                var kind = (StoryTriggerKind)System.Enum.Parse(typeof(StoryTriggerKind), story.Kind);
                triggers[i] = Trigger(kind, dialogue, story.Wave);
                triggers[i].Enemy = enemy;
                triggers[i].PathObject = pathObject;
                triggers[i].Repeat = (StoryRepeat)System.Enum.Parse(typeof(StoryRepeat), story.Repeat);
            }

            return triggers;
        }

        private static SpeakerDefinition CreateSpeaker(string key, string displayName, Color colour, SpeakerFrame frame, PortraitModel portrait)
        {
            var speaker = LoadOrCreate<SpeakerDefinition>($"{SpeakerFolder}/Speaker_{key}.asset");
            var so = new SerializedObject(speaker);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_roleColour").colorValue = colour;
            so.FindProperty("_frame").enumValueIndex = (int)frame;
            so.FindProperty("_portrait").enumValueIndex = (int)portrait;
            so.ApplyModifiedPropertiesWithoutUndo();
            return speaker;
        }

        private static LineSpec Say(SpeakerDefinition speaker, string text) => Say(speaker, 0f, Seg(text));

        private static LineSpec Say(SpeakerDefinition speaker, float glitch, params DialogueSegment[] segments) => new LineSpec(speaker, segments, glitch);

        private static DialogueSegment Seg(string text) => new DialogueSegment(text, SegmentStyle.Normal);

        // Line ids are the dialogue id plus a two-digit number, stable as long as lines are only appended.
        private static DialogueDefinition CreateDialogue(string id, params LineSpec[] lines)
        {
            var dialogue = LoadOrCreate<DialogueDefinition>($"{DialogueFolder}/{id}.asset");
            var so = new SerializedObject(dialogue);
            SerializedProperty array = so.FindProperty("_lines");
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = array.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("Id").stringValue = $"{id}.{i + 1:00}";
                line.FindPropertyRelative("Speaker").objectReferenceValue = lines[i].Speaker;
                line.FindPropertyRelative("GlitchIntensity").floatValue = lines[i].Glitch;
                SerializedProperty segments = line.FindPropertyRelative("Segments");
                segments.arraySize = lines[i].Segments.Length;
                for (int s = 0; s < lines[i].Segments.Length; s++)
                {
                    SerializedProperty segment = segments.GetArrayElementAtIndex(s);
                    segment.FindPropertyRelative("Text").stringValue = lines[i].Segments[s].Text;
                    segment.FindPropertyRelative("Style").enumValueIndex = (int)lines[i].Segments[s].Style;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return dialogue;
        }

        // A dialogue that introduces a drone kind: the drone panel lights the card up while it plays (DialogueDefinition.IntroducedDrones).
        private static void CreateDroneIntro(string id, Game.Drones.DroneDefinition drone, params LineSpec[] lines)
        {
            if (drone == null)
            {
                throw new System.InvalidOperationException($"{id}: the drones must be generated before the level story.");
            }

            var so = new SerializedObject(CreateDialogue(id, lines));
            SerializedProperty drones = so.FindProperty("_introducedDrones");
            drones.arraySize = 1;
            drones.GetArrayElementAtIndex(0).objectReferenceValue = drone;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static StoryTrigger Trigger(StoryTriggerKind kind, DialogueDefinition dialogue, int wave = 0)
        {
            return new StoryTrigger { Kind = kind, Dialogue = dialogue, Wave = wave, Repeat = StoryRepeat.OncePerSave };
        }

        private static void WriteTriggers(LevelDefinition level, StoryTrigger[] triggers)
        {
            var so = new SerializedObject(level);
            SerializedProperty array = so.FindProperty("_story");
            array.arraySize = triggers.Length;
            for (int i = 0; i < triggers.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Kind").enumValueIndex = (int)triggers[i].Kind;
                element.FindPropertyRelative("Wave").intValue = triggers[i].Wave;
                element.FindPropertyRelative("Enemy").objectReferenceValue = triggers[i].Enemy;
                element.FindPropertyRelative("PathObject").objectReferenceValue = triggers[i].PathObject;
                element.FindPropertyRelative("Dialogue").objectReferenceValue = triggers[i].Dialogue;
                element.FindPropertyRelative("Repeat").enumValueIndex = (int)triggers[i].Repeat;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
