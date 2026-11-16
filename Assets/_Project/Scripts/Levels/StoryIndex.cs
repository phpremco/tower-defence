using System.Collections.Generic;
using Game.Core;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// Every piece of story the campaign data holds, sorted for the developer story viewer: a first page with the
    /// cutscenes (and each conversation inside them), then one page per level with its briefing and every dialogue
    /// its story triggers start, labelled with the moment that starts it. Built from data alone, so a new level or
    /// dialogue shows up without code.
    /// </summary>
    public sealed class StoryIndex
    {
        // Properties, not constants: the titles are shown in the current language, and checks compare pages by them.
        public static string CutscenePageTitle => Loc.Get("story.page_cutscenes");
        public static string ArenaPageTitle => Loc.Get("story.page_arena");
        public static string ScanPageTitle => Loc.Get("story.page_scans");

        public enum EntryKind
        {
            Cutscene,
            Dialogue,
            Briefing,

            /// <summary>A fight against one boss in the developer boss arena.</summary>
            BossArena,
        }

        /// <summary>One thing the viewer can play.</summary>
        public readonly struct Entry
        {
            public Entry(string label, EntryKind kind, CutsceneDefinition cutscene, DialogueDefinition dialogue, string text, EnemyDefinition boss = null)
            {
                Boss = boss;
                Label = label;
                Kind = kind;
                Cutscene = cutscene;
                Dialogue = dialogue;
                Text = text;
            }

            public string Label { get; }
            public EntryKind Kind { get; }
            public CutsceneDefinition Cutscene { get; }
            public DialogueDefinition Dialogue { get; }

            /// <summary>The briefing's text (briefings are plain text on the level, not dialogue assets).</summary>
            public string Text { get; }

            /// <summary>The boss to fight in the arena.</summary>
            public EnemyDefinition Boss { get; }
        }

        public sealed class Page
        {
            public Page(string title) => Title = title;

            public string Title { get; }
            public List<Entry> Entries { get; } = new List<Entry>();
        }

        private readonly List<Page> _pages = new List<Page>();

        public StoryIndex(CampaignCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            AddCutscenes(catalog.Story != null ? catalog.Story.OpeningCutscene : null);
            var seen = new HashSet<LevelDefinition>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && seen.Add(node.Level))
                {
                    AddLevel(node.Level, seen.Count);
                }
            }

            AddScans(catalog.Story);
            AddArena(catalog.BossArena, catalog.Story);
        }

        public IReadOnlyList<Page> Pages => _pages;

        /// <summary>The arena the boss entries are fought in, or null.</summary>
        public BossArenaDefinition Arena { get; private set; }

        /// <summary>Every dialogue the index can play, for checks that nothing in the data is left out.</summary>
        public IEnumerable<DialogueDefinition> Dialogues()
        {
            foreach (Page page in _pages)
            {
                foreach (Entry entry in page.Entries)
                {
                    if (entry.Dialogue != null)
                    {
                        yield return entry.Dialogue;
                    }
                }
            }
        }

        private void AddCutscenes(CutsceneDefinition opening)
        {
            var page = new Page(CutscenePageTitle);
            if (opening != null)
            {
                page.Entries.Add(new Entry(Loc.Format("story.entry_play_cutscene", opening.Id), EntryKind.Cutscene, opening, null, null));
                var listed = new HashSet<DialogueDefinition>();
                for (int i = 0; i < opening.Shots.Count; i++)
                {
                    DialogueDefinition dialogue = opening.Shots[i].Dialogue;
                    if (dialogue != null && listed.Add(dialogue))
                    {
                        page.Entries.Add(new Entry(Loc.Format("story.entry_shot", i + 1, dialogue.Id), EntryKind.Dialogue, null, dialogue, null));
                    }
                }
            }

            _pages.Add(page);
        }

        // Voss's first-sighting scans play in whichever level a creature first shows up, so they get their own page; his warning
        // for the first rift behind a gate and his gate tips play wherever that happens first, so they are listed there too.
        private void AddScans(StoryDefinition story)
        {
            if (story == null || (story.ScanHints.Count == 0 && story.RearNestWarning == null && story.GateTips.Count == 0 && story.HighGroundTip == null && story.UpgradeScreenTip == null))
            {
                return;
            }

            var page = new Page(ScanPageTitle);
            foreach (ScanHint hint in story.ScanHints)
            {
                if (hint.Dialogue != null)
                {
                    string label = hint.Enemy != null ? Loc.Format("story.moment_first_of", hint.Enemy.DisplayName) : Loc.Get("story.moment_first_sighting");
                    page.Entries.Add(new Entry(Loc.Format("story.entry_dialogue", label, hint.Dialogue.Id), EntryKind.Dialogue, null, hint.Dialogue, null));
                }
            }

            if (story.RearNestWarning != null)
            {
                page.Entries.Add(new Entry(Loc.Format("story.entry_rear_nest", story.RearNestWarning.Id), EntryKind.Dialogue, null, story.RearNestWarning, null));
            }

            foreach (GateTip tip in story.GateTips)
            {
                if (tip.Dialogue != null)
                {
                    page.Entries.Add(new Entry(Loc.Format("story.entry_gate_tip", tip.Kind, tip.Dialogue.Id), EntryKind.Dialogue, null, tip.Dialogue, null));
                }
            }

            if (story.HighGroundTip != null)
            {
                page.Entries.Add(new Entry(Loc.Format("story.entry_high_ground", story.HighGroundTip.Id), EntryKind.Dialogue, null, story.HighGroundTip, null));
            }

            if (story.UpgradeScreenTip != null)
            {
                page.Entries.Add(new Entry(Loc.Format("story.entry_upgrade_screen", story.UpgradeScreenTip.Id), EntryKind.Dialogue, null, story.UpgradeScreenTip, null));
            }

            _pages.Add(page);
        }

        // One entry per boss the arena lists, fought in the arena's level, and the answer of its intro (Docs/BOSS_INTROS.md).
        private void AddArena(BossArenaDefinition arena, StoryDefinition story)
        {
            if (arena == null || arena.Level == null || arena.Bosses.Count == 0)
            {
                return;
            }

            Arena = arena;
            var page = new Page(ArenaPageTitle);
            foreach (EnemyDefinition boss in arena.Bosses)
            {
                LevelDefinition level = arena.LevelFor(boss);
                page.Entries.Add(new Entry(Loc.Format("story.entry_fight", boss.DisplayName, level.DisplayName), EntryKind.BossArena, null, null, null, boss));
                DialogueDefinition answer = story != null && story.IntroFor(boss) != null ? story.IntroFor(boss).Answer : null;
                if (answer != null)
                {
                    page.Entries.Add(new Entry(Loc.Format("story.entry_boss_intro", boss.DisplayName, answer.Id), EntryKind.Dialogue, null, answer, null));
                }
            }

            _pages.Add(page);
        }

        private void AddLevel(LevelDefinition level, int number)
        {
            var page = new Page(Loc.Format("story.page_level", number, level.DisplayName));
            if (!string.IsNullOrWhiteSpace(level.Briefing))
            {
                page.Entries.Add(new Entry(Loc.Get("story.entry_briefing"), EntryKind.Briefing, null, null, level.Briefing));
            }

            var listed = new HashSet<DialogueDefinition>();
            foreach (StoryTrigger trigger in level.StoryTriggers)
            {
                if (trigger.Dialogue != null && listed.Add(trigger.Dialogue))
                {
                    page.Entries.Add(new Entry(Loc.Format("story.entry_dialogue", Moment(trigger), trigger.Dialogue.Id), EntryKind.Dialogue, null, trigger.Dialogue, null));
                }
            }

            _pages.Add(page);
        }

        /// <summary>When in the level the dialogue plays, in the words the viewer shows.</summary>
        public static string Moment(StoryTrigger trigger)
        {
            switch (trigger.Kind)
            {
                case StoryTriggerKind.LevelStart: return Loc.Get("story.moment_level_start");
                case StoryTriggerKind.WaveStart: return Loc.Format("story.moment_wave", trigger.Wave);
                case StoryTriggerKind.FirstSighting: return trigger.Enemy != null ? Loc.Format("story.moment_first_of", trigger.Enemy.DisplayName) : Loc.Get("story.moment_first_sighting");
                case StoryTriggerKind.BossSpawn: return Loc.Get("story.moment_boss_appears");
                case StoryTriggerKind.BossDefeated: return Loc.Get("story.moment_boss_defeated");
                case StoryTriggerKind.LevelWon: return Loc.Get("story.moment_level_won");
                case StoryTriggerKind.FirstUse: return trigger.PathObject != null ? Loc.Format("story.moment_first_of", trigger.PathObject.DisplayName) : Loc.Get("story.moment_first_gadget");
                default: return trigger.Kind.ToString();
            }
        }
    }
}
