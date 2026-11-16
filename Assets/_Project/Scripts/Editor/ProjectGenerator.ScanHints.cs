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

        /// <summary>
        /// Round 65 without the full generator: the story's hints again (the high-ground hint is new, and every hint
        /// carries its loading tip). Reads existing assets only, like the menu item above.
        /// </summary>
        public static string ApplyRound65Story()
        {
            GenerateScanHints();
            AssetDatabase.SaveAssets();
            return $"story: {LoadingTipTexts.Length} loading tips written, high-ground hint {HighGroundTipId} ({HighGroundPercent})\n";
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
            so.FindProperty("_highGroundTip").objectReferenceValue = CreateHighGroundTip(voss);
            so.FindProperty("_upgradeScreenTip").objectReferenceValue = CreateUpgradeScreenTip(voss);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(story);
            WriteLoadingTips();
        }

        /// <summary>Round 66 without the full generator: the story's hints again (the upgrade screen's explanation is new).</summary>
        public static string ApplyRound66Story()
        {
            GenerateScanHints();
            AssetDatabase.SaveAssets();
            return $"story: upgrade screen explanation {UpgradeScreenTipId} (first continent opens tier {FirstContinentTiers}), {LoadingTipTexts.Length} loading tips\n";
        }

        // Remco 2026-11-15 (round 66): the first time the upgrade screen opens in a save, Voss says how it works: research
        // points, tiers, the continents that open them, Spencer's gear (UpgradeScreenGuide). The tier number in his lines is
        // the rules' own cap for the first continent, so the line and the padlocks cannot drift apart. Proposed lines.
        public const string UpgradeScreenTipId = "Dlg_Tip_UpgradeScreen";

        private static int FirstContinentTiers => BalanceSheet.Rules.UpgradeTierCap(0);

        private static DialogueDefinition CreateUpgradeScreenTip(SpeakerDefinition voss) =>
            CreateDialogue(UpgradeScreenTipId,
                Say(voss, "Welcome to the research bay. Every level you clear pays research points, and a harder difficulty pays more."),
                Say(voss, "Spend them here on tiers. Each tier makes a turret, the base or the gates better for good; the drones take power instead."),
                Say(voss, $"A padlock means that tier opens on a later continent. Crystal Plains opens the first {FirstContinentTiers}; there are ten."),
                Say(voss, "And do look after Spencer: a repair drone that mends him faster, and armour that takes the edge off every hit."));

        // Remco 2026-11-15 (playtest 2): towers on raised ground reach further, and Voss says so once per save, after the
        // first tower is built in a level that has a raised build spot (LevelStoryListener). The number in his lines is the
        // generator's own (HighGroundRange), so the hint and the rule cannot drift apart. Proposed lines in Voss's voice.
        public const string HighGroundTipId = "Dlg_Tip_HighGround";

        private static string HighGroundPercent => $"{Mathf.RoundToInt((HighGroundRange - 1f) * 100f)}%";

        private static DialogueDefinition CreateHighGroundTip(SpeakerDefinition voss) =>
            CreateDialogue(HighGroundTipId,
                Say(voss, "Good, the first turret stands. Now look for the rises: ground with a glowing seam round its bank."),
                Say(voss, $"That is high ground. A turret on top of it sees further: {HighGroundPercent} more range. The range ring shows it before you build."),
                Say(voss, "The bank itself is too steep for a turret, so only the top counts. Take it where the road passes close."));

        // Round 65 (Remco: "Ground guns can't touch it" on the loading screen reads without context): every hint's advice
        // once more as a line that stands on its own, naming the creature or the thing it is about. The loading screen
        // shows only these (LoadingTips); each fits its box (LoadingTips.MaxLength).
        public static readonly (string Dialogue, string Tip)[] LoadingTipTexts =
        {
            ("Dlg_Scan_Skitter", "Skitters are weak alone and come in packs: the Cannon's shells thin a pack, the Gun Turret picks off loose runners."),
            ("Dlg_L01_Stone", "Cragbacks wear armour that bullets and beams barely scratch. A Cannon's heavy shells strip it."),
            ("Dlg_Scan_Wisp", "Wisps die fast but hit a gate hard. Stop them early: the Laser Turret fires fast enough to catch them."),
            ("Dlg_Scan_Skyray", "Skyrays never land, so ground guns can't touch them. Build a Flak Turret where they cross the gates."),
            ("Dlg_Scan_Shalebat", "Shalebats spit at the turrets they fly past. A Flak Turret beside the stone nests takes them first."),
            ("Dlg_Scan_Mossbat", "Mossbats are shed in flight by the big flyers. Keep a Flak Turret in reach of wherever those go."),
            ("Dlg_Scan_Roller", "Rollers curl up and roll three times faster than anything else. The Laser Turret's beam keeps up with them."),
            ("Dlg_Scan_Prowler", "When one Prowler falls, the rest of its pack rages and runs. Hit them together with the Cannon's splash."),
            (GateUpgradeTipId, "A gate under attack can be upgraded during the level: it gets tougher and grows a turret of its own."),
            (MoveHeroTipId, "When another gate is being hammered, send Spencer there: {action:HeroPost} for the next post, or select that gate."),
            (RearNestWarningId, "A rift can open behind the gates. Keep a turret in reach of it, or near the base."),
            (HighGroundTipId, $"Towers on high ground reach {HighGroundPercent} further. The bank round it is too steep to build on."),
            (UpgradeScreenTipId, "Research points buy permanent upgrade tiers between levels. A padlocked tier opens on a later continent."),
        };

        private static void WriteLoadingTips()
        {
            foreach ((string id, string tip) in LoadingTipTexts)
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"{DialogueFolder}/{id}.asset");
                if (dialogue == null)
                {
                    throw new System.InvalidOperationException($"Loading tip: the dialogue '{id}' does not exist.");
                }

                var so = new SerializedObject(dialogue);
                so.FindProperty("_loadingTip").stringValue = tip;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(dialogue);
            }
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
                Say(voss, "Go where the fight is. The jetpack gets you there in seconds: {action:HeroPost} for the next post, or select that gate and send Spencer."),
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

        /// <summary>Round 70: only the button name tables (Xbox, PlayStation, Nintendo, keyboard) and their place on the skin.</summary>
        [MenuItem("Tools/Generate/Button Prompt Tables")]
        public static void GenerateButtonPromptTables()
        {
            GenerateButtonGlyphs();
            AssetDatabase.SaveAssets();
            Debug.Log("Generated the button prompt tables.");
        }

        // One names table per pad family; the pad in the player's hands picks one (a DualSense prints Cross, a Switch pad
        // B for its south button). The keyboard table shortens key names for prompts and the drawn key caps.
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
            ButtonGlyphTable playStation = WriteGlyphs("Glyphs_PlayStation", PadLayout.PlayStation,
                new ButtonGlyph("buttonSouth", "Cross"), new ButtonGlyph("buttonEast", "Circle"), new ButtonGlyph("buttonWest", "Square"), new ButtonGlyph("buttonNorth", "Triangle"),
                new ButtonGlyph("leftShoulder", "L1"), new ButtonGlyph("rightShoulder", "R1"), new ButtonGlyph("leftTrigger", "L2"), new ButtonGlyph("rightTrigger", "R2"),
                new ButtonGlyph("start", "Options"), new ButtonGlyph("select", "Create"), new ButtonGlyph("leftStickPress", "L3"), new ButtonGlyph("rightStickPress", "R3"),
                new ButtonGlyph("leftStick", "L-stick"), new ButtonGlyph("rightStick", "R-stick"), new ButtonGlyph("dpad", "D-pad"));
            ButtonGlyphTable keys = WriteGlyphs("Glyphs_Keyboard", PadLayout.Xbox,
                new ButtonGlyph("escape", "Esc"), new ButtonGlyph("enter", "Enter"), new ButtonGlyph("space", "Space"), new ButtonGlyph("tab", "Tab"),
                new ButtonGlyph("leftButton", "Click"), new ButtonGlyph("rightButton", "Right-click"), new ButtonGlyph("middleButton", "Middle-click"),
                new ButtonGlyph("backspace", "Bksp"), new ButtonGlyph("leftShift", "Shift"), new ButtonGlyph("rightShift", "Shift"),
                new ButtonGlyph("leftCtrl", "Ctrl"), new ButtonGlyph("rightCtrl", "Ctrl"), new ButtonGlyph("leftAlt", "Alt"), new ButtonGlyph("rightAlt", "Alt"),
                new ButtonGlyph("capsLock", "Caps"), new ButtonGlyph("delete", "Del"), new ButtonGlyph("insert", "Ins"), new ButtonGlyph("pageUp", "PgUp"),
                new ButtonGlyph("pageDown", "PgDn"), new ButtonGlyph("home", "Home"), new ButtonGlyph("end", "End"), new ButtonGlyph("numpadEnter", "Num Enter"),
                new ButtonGlyph("upArrow", "↑"), new ButtonGlyph("downArrow", "↓"), new ButtonGlyph("leftArrow", "←"), new ButtonGlyph("rightArrow", "→"),
                new ButtonGlyph("backquote", "`"), new ButtonGlyph("contextMenu", "Menu"), new ButtonGlyph("printScreen", "PrtSc"), new ButtonGlyph("scrollLock", "ScrLk"),
                new ButtonGlyph("pause", "Pause"), new ButtonGlyph("numLock", "NumLk"));

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinAssetPath);
            if (skin == null)
            {
                throw new System.InvalidOperationException($"The prompt tables hang on the UI skin at {SkinAssetPath}: run the UI skin step first.");
            }

            var so = new SerializedObject(skin);
            SerializedProperty pads = so.FindProperty("_padGlyphs");
            pads.arraySize = 3;
            pads.GetArrayElementAtIndex(0).objectReferenceValue = xbox;
            pads.GetArrayElementAtIndex(1).objectReferenceValue = nintendo;
            pads.GetArrayElementAtIndex(2).objectReferenceValue = playStation;
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
