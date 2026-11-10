using System.Collections.Generic;
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
        public const string CutscenePageTitle = "Cutscenes";
        public const string ArenaPageTitle = "Boss arena";
        public const string ScanPageTitle = "Voss scans";

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
                page.Entries.Add(new Entry($"Play cutscene: {opening.Id}", EntryKind.Cutscene, opening, null, null));
                var listed = new HashSet<DialogueDefinition>();
                for (int i = 0; i < opening.Shots.Count; i++)
                {
                    DialogueDefinition dialogue = opening.Shots[i].Dialogue;
                    if (dialogue != null && listed.Add(dialogue))
                    {
                        page.Entries.Add(new Entry($"Shot {i + 1}: {dialogue.Id}", EntryKind.Dialogue, null, dialogue, null));
                    }
                }
            }

            _pages.Add(page);
        }

        // Voss's first-sighting scans play in whichever level a creature first shows up, so they get their own page; his warning
        // for the first rift behind a gate and his gate tips play wherever that happens first, so they are listed there too.
        private void AddScans(StoryDefinition story)
        {
            if (story == null || (story.ScanHints.Count == 0 && story.RearNestWarning == null && story.GateTips.Count == 0))
            {
                return;
            }

            var page = new Page(ScanPageTitle);
            foreach (ScanHint hint in story.ScanHints)
            {
                if (hint.Dialogue != null)
                {
                    string label = hint.Enemy != null ? $"First {hint.Enemy.DisplayName}" : "First sighting";
                    page.Entries.Add(new Entry($"{label}: {hint.Dialogue.Id}", EntryKind.Dialogue, null, hint.Dialogue, null));
                }
            }

            if (story.RearNestWarning != null)
            {
                page.Entries.Add(new Entry($"First rift behind a gate: {story.RearNestWarning.Id}", EntryKind.Dialogue, null, story.RearNestWarning, null));
            }

            foreach (GateTip tip in story.GateTips)
            {
                if (tip.Dialogue != null)
                {
                    page.Entries.Add(new Entry($"Gate tip ({tip.Kind}): {tip.Dialogue.Id}", EntryKind.Dialogue, null, tip.Dialogue, null));
                }
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
                page.Entries.Add(new Entry($"Fight {boss.DisplayName} ({level.DisplayName})", EntryKind.BossArena, null, null, null, boss));
                DialogueDefinition answer = story != null && story.IntroFor(boss) != null ? story.IntroFor(boss).Answer : null;
                if (answer != null)
                {
                    page.Entries.Add(new Entry($"Intro of {boss.DisplayName}: {answer.Id}", EntryKind.Dialogue, null, answer, null));
                }
            }

            _pages.Add(page);
        }

        private void AddLevel(LevelDefinition level, int number)
        {
            var page = new Page($"Level {number}: {level.DisplayName}");
            if (!string.IsNullOrWhiteSpace(level.Briefing))
            {
                page.Entries.Add(new Entry("Briefing", EntryKind.Briefing, null, null, level.Briefing));
            }

            var listed = new HashSet<DialogueDefinition>();
            foreach (StoryTrigger trigger in level.StoryTriggers)
            {
                if (trigger.Dialogue != null && listed.Add(trigger.Dialogue))
                {
                    page.Entries.Add(new Entry($"{Moment(trigger)}: {trigger.Dialogue.Id}", EntryKind.Dialogue, null, trigger.Dialogue, null));
                }
            }

            _pages.Add(page);
        }

        /// <summary>When in the level the dialogue plays, in the words the viewer shows.</summary>
        public static string Moment(StoryTrigger trigger)
        {
            switch (trigger.Kind)
            {
                case StoryTriggerKind.LevelStart: return "Level start";
                case StoryTriggerKind.WaveStart: return $"Wave {trigger.Wave}";
                case StoryTriggerKind.FirstSighting: return trigger.Enemy != null ? $"First {trigger.Enemy.DisplayName}" : "First sighting";
                case StoryTriggerKind.BossSpawn: return "Boss appears";
                case StoryTriggerKind.BossDefeated: return "Boss defeated";
                case StoryTriggerKind.LevelWon: return "Level won";
                case StoryTriggerKind.FirstUse: return trigger.PathObject != null ? $"First {trigger.PathObject.DisplayName}" : "First gadget";
                default: return trigger.Kind.ToString();
            }
        }
    }
}
