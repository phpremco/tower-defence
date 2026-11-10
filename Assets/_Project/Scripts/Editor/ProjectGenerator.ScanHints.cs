using Game.Enemies;
using Game.Levels;
using Game.Platform;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 33: Dr. Voss's creature scans (the first sighting of a creature in the campaign names its counter tower and
    /// links to the Codex) and the button-name tables the prompts print. Both read existing assets only, so the step
    /// also runs on its own from the menu without regenerating levels, waves or balance data.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string GlyphFolder = "Assets/_Project/Settings/Input";
        private const string EnemyDataFolder = DataRoot + "/Enemies";

        /// <summary>Writes the scans and the glyph tables, then saves. Safe to run again: assets are reused and refreshed.</summary>
        [MenuItem("Tools/Generate/Scan Hints and Button Prompts")]
        public static void GenerateScanHintsAndPrompts()
        {
            GenerateScanHints();
            GenerateButtonGlyphs();
            AssetDatabase.SaveAssets();
            Debug.Log("Generated the creature scans and the button prompt tables.");
        }

        // One scan per creature that Crystal Plains levels 1 to 6 send (bosses meet the hero through their own intros).
        // Proposed lines (2026-11-10), in Voss's voice: what the scan found, then the tower that answers it. The Cragback
        // keeps the level-1 sighting conversation it already had, now with its Codex link.
        private static void GenerateScanHints()
        {
            var ora = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Ora.asset");
            var voss = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Voss.asset");
            if (ora == null || voss == null)
            {
                throw new System.InvalidOperationException("The scans need the speakers: run the level story step first.");
            }

            ScanHint[] hints =
            {
                Scan("Skitter", "Dlg_Scan_Skitter",
                    Say(voss, "Scan's in. Ant-like swarmers, weak one by one, dangerous in numbers."),
                    Say(voss, "Packs bunch up, so the Cannon's shells thin them. Loose runners want the Gun Turret's rate of fire.")),
                ScanOn("Cragback", "Dlg_L01_Stone"),
                Scan("Wisp", "Dlg_Scan_Wisp",
                    Say(voss, "I scanned that glow. Pure energy, almost no mass: it dies fast, but it hits a gate hard."),
                    Say(voss, "Stop it early. The Laser Turret fires fast enough to catch it; ground and air guns both reach it.")),
                Scan("Skyray", "Dlg_Scan_Skyray",
                    Say(voss, "Scan done. All wing and a spiked tail, and it stays in the air the whole way."),
                    Say(voss, "Ground guns can't touch it. The Flak Turret can: build one where it crosses the gates.")),
                Scan("Shalebat", "Dlg_Scan_Shalebat",
                    Say(voss, "The bats live inside the stone nests. They spit at the turrets they fly past."),
                    Say(voss, "Air only. A Flak Turret beside the nests takes them before they reach the guns.")),
                Scan("Mossbat", "Dlg_Scan_Mossbat",
                    Say(voss, "That big flyer is shedding bats. Same species as the stone ones, I think. Fascinating."),
                    Say(voss, "Air only as well. Keep a Flak Turret in reach of wherever it goes.")),
                Scan("Roller", "Dlg_Scan_Roller",
                    Say(voss, "I barely got a scan. It curls into a ball and rolls three times faster than anything else."),
                    Say(voss, "The Laser Turret's beam is instant, so it keeps up. Better still: slow it down before it rolls past.")),
                Scan("Prowler", "Dlg_Scan_Prowler",
                    Say(voss, "Pack hunters. When one falls, the rest go into a rage and run."),
                    Say(voss, "So don't pick them off one by one. The Cannon's splash hits the whole pack at once.")),
            };

            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(StoryAssetPath);
            if (story == null)
            {
                throw new System.InvalidOperationException($"The scans belong to the story asset at {StoryAssetPath}: run the story step first.");
            }

            var so = new SerializedObject(story);
            SerializedProperty array = so.FindProperty("_scanHints");
            array.arraySize = hints.Length;
            for (int i = 0; i < hints.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Enemy").objectReferenceValue = hints[i].Enemy;
                element.FindPropertyRelative("Dialogue").objectReferenceValue = hints[i].Dialogue;
            }

            so.FindProperty("_rearNestWarning").objectReferenceValue = CreateRearNestWarning(voss);
            WriteGateTips(so.FindProperty("_gateTips"), CreateGateTips(voss));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(story);
        }

        // Remco 2026-11-10: rifts that join the road behind the defence gate stay where they are (they keep the player
        // watchful, and later maps may open them anywhere); the first one that opens in a save gets this warning, with a ping
        // at the rift (LevelStoryListener, RearNests). Proposed lines in Voss's voice.
        public const string RearNestWarningId = "Dlg_Warn_RearRift";

        private static DialogueDefinition CreateRearNestWarning(SpeakerDefinition voss) =>
            CreateDialogue(RearNestWarningId,
                Say(voss, "Wait, that reading is behind our gate. A rift is opening past the defences."),
                Say(voss, "Whatever crawls out never meets the gate. Keep a turret in reach of it, or near the base."),
                Say(voss, "And watch the whole map. I doubt this is the last one that opens where we don't expect it."));

        // Remco 2026-11-08 (playtest note 12): Voss teaches the in-level gate upgrade and tells Spencer to move to the gate under
        // pressure, each once per save at the moment it can be acted on (GateTipRule). Proposed lines in Voss's voice.
        public const string GateUpgradeTipId = "Dlg_Tip_GateUpgrade";
        public const string MoveHeroTipId = "Dlg_Tip_MoveSpencer";

        // The upgrade tip: a gate below this share of its health that the player can pay to upgrade.
        private const float UpgradeTipHurtBelow = 0.75f;

        // The move tip: another gate below this share while Spencer's own gate is still at or above the second one.
        private const float MoveTipHurtBelow = 0.6f;
        private const float MoveTipQuietAbove = 0.9f;

        private static GateTip[] CreateGateTips(SpeakerDefinition voss) => new[]
        {
            new GateTip(GateTipKind.UpgradeGate, CreateDialogue(GateUpgradeTipId,
                Say(voss, "That gate is taking hits. You can reinforce it right now: select the gate and upgrade it."),
                Say(voss, "Every gate level makes it tougher and adds a small turret of its own. A gate that holds longer gives every gun around it more time to shoot."),
                Say(voss, "Upgrade the gate where they pile up first. A repair patches it; an upgrade makes it worth defending.")),
                UpgradeTipHurtBelow, 0f),
            new GateTip(GateTipKind.MoveHero, CreateDialogue(MoveHeroTipId,
                Say(voss, "Spencer, your gate is quiet, but another one is being hammered."),
                Say(voss, "Go where the fight is. The jetpack gets you there in seconds: Next post, or select that gate and send Spencer."),
                Say(voss, "When the pressure shifts, shift with it. A blade at the right gate beats a turret at the wrong one.")),
                MoveTipHurtBelow, MoveTipQuietAbove),
        };

        private static void WriteGateTips(SerializedProperty array, GateTip[] tips)
        {
            array.arraySize = tips.Length;
            for (int i = 0; i < tips.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Kind").intValue = (int)tips[i].Kind;
                element.FindPropertyRelative("Dialogue").objectReferenceValue = tips[i].Dialogue;
                element.FindPropertyRelative("HurtBelow").floatValue = tips[i].HurtBelow;
                element.FindPropertyRelative("QuietAbove").floatValue = tips[i].QuietAbove;
            }
        }

        private static ScanHint Scan(string enemyId, string dialogueId, params LineSpec[] lines)
        {
            CreateDialogue(dialogueId, lines);
            return ScanOn(enemyId, dialogueId);
        }

        // A scan on an existing conversation: the creature, and the Codex link written onto the dialogue.
        private static ScanHint ScanOn(string enemyId, string dialogueId)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyDataFolder}/{enemyId}.asset");
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"{DialogueFolder}/{dialogueId}.asset");
            if (enemy == null || dialogue == null)
            {
                throw new System.InvalidOperationException($"Scan '{dialogueId}': creature '{enemyId}' or the dialogue does not exist.");
            }

            var so = new SerializedObject(dialogue);
            so.FindProperty("_codexEntry").objectReferenceValue = enemy;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialogue);
            return new ScanHint(enemy, dialogue);
        }

        // Xbox-style names by default; the Nintendo table is there for the Switch profile (its south button is "B").
        private static void GenerateButtonGlyphs()
        {
            ButtonGlyphTable xbox = WriteGlyphs("Glyphs_Xbox", PadLayout.Xbox,
                new ButtonGlyph("buttonSouth", "A"), new ButtonGlyph("buttonEast", "B"), new ButtonGlyph("buttonWest", "X"), new ButtonGlyph("buttonNorth", "Y"),
                new ButtonGlyph("leftShoulder", "LB"), new ButtonGlyph("rightShoulder", "RB"), new ButtonGlyph("leftTrigger", "LT"), new ButtonGlyph("rightTrigger", "RT"),
                new ButtonGlyph("start", "Menu"), new ButtonGlyph("select", "View"), new ButtonGlyph("leftStickPress", "L3"), new ButtonGlyph("rightStickPress", "R3"),
                new ButtonGlyph("leftStick", "L-stick"), new ButtonGlyph("rightStick", "R-stick"), new ButtonGlyph("dpad", "D-pad"));
            ButtonGlyphTable nintendo = WriteGlyphs("Glyphs_Nintendo", PadLayout.Nintendo,
                new ButtonGlyph("buttonSouth", "B"), new ButtonGlyph("buttonEast", "A"), new ButtonGlyph("buttonWest", "Y"), new ButtonGlyph("buttonNorth", "X"),
                new ButtonGlyph("leftShoulder", "L"), new ButtonGlyph("rightShoulder", "R"), new ButtonGlyph("leftTrigger", "ZL"), new ButtonGlyph("rightTrigger", "ZR"),
                new ButtonGlyph("start", "+"), new ButtonGlyph("select", "-"), new ButtonGlyph("leftStickPress", "L-click"), new ButtonGlyph("rightStickPress", "R-click"),
                new ButtonGlyph("leftStick", "L-stick"), new ButtonGlyph("rightStick", "R-stick"), new ButtonGlyph("dpad", "D-pad"));
            ButtonGlyphTable keys = WriteGlyphs("Glyphs_Keyboard", PadLayout.Xbox,
                new ButtonGlyph("escape", "Esc"), new ButtonGlyph("enter", "Enter"), new ButtonGlyph("space", "Space"), new ButtonGlyph("tab", "Tab"),
                new ButtonGlyph("leftButton", "Click"), new ButtonGlyph("rightButton", "Right-click"));

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinAssetPath);
            if (skin == null)
            {
                throw new System.InvalidOperationException($"The prompt tables hang on the UI skin at {SkinAssetPath}: run the UI skin step first.");
            }

            var so = new SerializedObject(skin);
            SerializedProperty pads = so.FindProperty("_padGlyphs");
            pads.arraySize = 2;
            pads.GetArrayElementAtIndex(0).objectReferenceValue = xbox;
            pads.GetArrayElementAtIndex(1).objectReferenceValue = nintendo;
            so.FindProperty("_keyGlyphs").objectReferenceValue = keys;
            so.FindProperty("_minReadableFontSize").intValue = MinReadableFontSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
        }

        // 27 canvas units are 18 px on a 720p handheld screen (the canvas scales 1080p to 720p by two thirds).
        private const int MinReadableFontSize = 27;

        private static ButtonGlyphTable WriteGlyphs(string assetName, PadLayout layout, params ButtonGlyph[] glyphs)
        {
            var table = LoadOrCreate<ButtonGlyphTable>($"{GlyphFolder}/{assetName}.asset");
            var so = new SerializedObject(table);
            so.FindProperty("_layout").enumValueIndex = (int)layout;
            SerializedProperty array = so.FindProperty("_glyphs");
            array.arraySize = glyphs.Length;
            for (int i = 0; i < glyphs.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Control").stringValue = glyphs[i].Control;
                element.FindPropertyRelative("Label").stringValue = glyphs[i].Label;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }
    }
}
