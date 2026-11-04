using System.Collections.Generic;

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

        public enum EntryKind
        {
            Cutscene,
            Dialogue,
            Briefing,
        }

        /// <summary>One thing the viewer can play.</summary>
        public readonly struct Entry
        {
            public Entry(string label, EntryKind kind, CutsceneDefinition cutscene, DialogueDefinition dialogue, string text)
            {
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
        }

        public IReadOnlyList<Page> Pages => _pages;

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
