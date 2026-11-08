using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the boss intros (Docs/BOSS_INTROS.md): per continent boss the hero's line, ORA's and Dr. Voss's
    /// answer, the barks for a later meeting and for its signature moments, and the look of the beat; hung on the story.
    /// The lines follow the doc; the barks for later meetings that the doc does not give are curator proposals.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string IntroFolder = DataRoot + "/Story/Intros";

        private static readonly Color MoltenTint = new Color(1f, 0.52f, 0.18f);
        private static readonly Color LavaTint = new Color(1f, 0.36f, 0.1f);
        private static readonly Color FrostTint = new Color(0.55f, 0.85f, 1f);
        private static readonly Color BogTint = new Color(0.62f, 1f, 0.3f);
        private static readonly Color CrystalTint = new Color(0.85f, 0.6f, 1f);
        private static readonly Color SealTint = new Color(0.66f, 0.36f, 1f);

        private readonly struct IntroSpec
        {
            public IntroSpec(string boss, Color tint, string hero, string ora, string voss, string[] again, params BossBark[] barks)
            {
                Boss = boss;
                Tint = tint;
                Hero = hero;
                Ora = ora;
                Voss = voss;
                Again = again;
                Barks = barks;
            }

            public string Boss { get; }
            public Color Tint { get; }
            public string Hero { get; }
            public string Ora { get; }
            public string Voss { get; }
            public string[] Again { get; }
            public BossBark[] Barks { get; }
        }

        private static BossBark Bark(BossMoment moment, string line) => new BossBark { Moment = moment, Line = line };

        private static IntroSpec[] BossIntros() => new[]
        {
            new IntroSpec("Gravemaw", MoltenTint,
                "That's... that's our drill. That WAS our drill.",
                "Subterranean contact. It tracks the turret hurting it most. Spread your fire.",
                "It went straight for the drill. Spencer, I think it heard us digging.",
                new[] { "Oh good. The drill-eater's back.", "It's back. Keep the guns apart." },
                Bark(BossMoment.Throw, "Did it just THROW my turret?!"),
                Bark(BossMoment.Burrow, "Where'd it go? ...I hate it when they go quiet.")),
            new IntroSpec("CinderMatriarch", LavaTint,
                "Okay. The lava has legs now. Great.",
                "Thermal signature hunting cold emitters. Your Cryo towers are bait.",
                "Cold kills her brood. She'll cross the whole map to stop it. Use that.",
                new[] { "She's back for the cold ones.", "Lava with legs. Again." },
                Bark(BossMoment.Smash, "She really doesn't like the cold, huh."),
                Bark(BossMoment.Eggs, "Are those... eggs? Shoot the eggs! SHOOT THE EGGS!")),
            new IntroSpec("FrostWarden", FrostTint,
                "Those fists are bigger than my gate. Why are they bigger than my gate?",
                "It freezes barricades and walks through them. Fire cracks its armour.",
                "Those glyphs on its armour... it isn't hunting. It's guarding something.",
                new[] { "Big, cold and back. Light it up.", "The fists are back. Fire on the armour!" },
                Bark(BossMoment.GateFreeze, "My gate's an ice cube. Fantastic."),
                Bark(BossMoment.ArmourBreak, "Armour's off! Hit it with everything!")),
            new IntroSpec("Rotbloom", BogTint,
                "Has that flower been watching us this whole time?",
                "Spore launcher. It overgrows clustered turrets. Splash damage burns it clean.",
                "The whole bog is one organism. And it's very, very angry.",
                new[] { "The flower again. Burn it clean.", "It grew back. Of course it grew back." },
                Bark(BossMoment.Overgrow, "Get these weeds off my guns!"),
                Bark(BossMoment.Uproot, "It's walking. The flower is walking.")),
            new IntroSpec("Shattered", CrystalTint,
                "Doc... was that door supposed to open?",
                "Crystal construct. It splits under stress. Cover every route.",
                "Those crystals carry our weapon signatures. Did we... make this?",
                new[] { "Glass giant. Cover every road.", "It's back in one piece. For now." },
                Bark(BossMoment.Split, "Three of them. Of course there are three of them."),
                Bark(BossMoment.Rejoin, "And they're... putting themselves back together. Wonderful.")),
            new IntroSpec("SealedOne", SealTint,
                "...Doc. The ground is moving. The whole ground.",
                "Scale exceeds all records. It adapts to our strongest weapon. Mix your arsenal.",
                "This is what the crystals were holding down. Don't give it one thing to learn.",
                new[] { "It's awake again. Mix it up.", "The ground's moving. Here we go." },
                Bark(BossMoment.Adapt, "It learned. It actually learned."),
                Bark(BossMoment.OnBase, "Get OFF my base!")),
        };

        // Each intro asset and its answer dialogue (ORA, then Voss), and the story's list of them.
        private static void GenerateBossIntros()
        {
            EnsureFolder(IntroFolder);
            var ora = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Ora.asset");
            var voss = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/Speaker_Voss.asset");
            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(StoryAssetPath);
            if (ora == null || voss == null || story == null)
            {
                throw new System.InvalidOperationException("Boss intros: the speakers and the story must be generated first.");
            }

            var intros = new List<BossIntroDefinition>();
            foreach (IntroSpec spec in BossIntros())
            {
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{spec.Boss}.asset");
                if (boss == null)
                {
                    continue;
                }

                DialogueDefinition answer = CreateDialogue($"Dlg_Intro_{spec.Boss}", Say(ora, spec.Ora), Say(voss, spec.Voss));
                var intro = LoadOrCreate<BossIntroDefinition>($"{IntroFolder}/Intro_{spec.Boss}.asset");
                var so = new SerializedObject(intro);
                so.FindProperty("_boss").objectReferenceValue = boss;
                so.FindProperty("_heroLine").stringValue = spec.Hero;
                so.FindProperty("_answer").objectReferenceValue = answer;
                so.FindProperty("_cutInTint").colorValue = spec.Tint;
                SerializedProperty again = so.FindProperty("_repeatBarks");
                again.arraySize = spec.Again.Length;
                for (int i = 0; i < spec.Again.Length; i++)
                {
                    again.GetArrayElementAtIndex(i).stringValue = spec.Again[i];
                }

                SerializedProperty barks = so.FindProperty("_barks");
                barks.arraySize = spec.Barks.Length;
                for (int i = 0; i < spec.Barks.Length; i++)
                {
                    SerializedProperty bark = barks.GetArrayElementAtIndex(i);
                    bark.FindPropertyRelative("Moment").enumValueIndex = (int)spec.Barks[i].Moment;
                    bark.FindPropertyRelative("Line").stringValue = spec.Barks[i].Line;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(intro);
                intros.Add(intro);
            }

            var storyObject = new SerializedObject(story);
            SerializedProperty list = storyObject.FindProperty("_bossIntros");
            list.arraySize = intros.Count;
            for (int i = 0; i < intros.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = intros[i];
            }

            storyObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(story);
        }
    }
}
