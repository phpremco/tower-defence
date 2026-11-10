using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The story of the campaign as data: the opening cutscene of the first launch and how the hero meets each continent
    /// boss. Level briefings live on the levels themselves, so a level carries its own piece of the story.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story", fileName = "Story")]
    public sealed class StoryDefinition : ScriptableObject
    {
        [Tooltip("The cutscene the first launch opens with: prologue, landing, the first attack.")]
        [SerializeField] private CutsceneDefinition _openingCutscene;

        [Tooltip("The hero's first meeting with each continent boss and his barks in the fight (Docs/BOSS_INTROS.md).")]
        [SerializeField] private BossIntroDefinition[] _bossIntros = new BossIntroDefinition[0];

        [Tooltip("Dr. Voss's scan of each creature: plays the first time it appears in the campaign and names its counter tower.")]
        [SerializeField] private ScanHint[] _scanHints = new ScanHint[0];

        [Tooltip("Dr. Voss's warning the first time a rift opens behind the defence gate (once per save; Remco 2026-11-10).")]
        [SerializeField] private DialogueDefinition _rearNestWarning;

        [Tooltip("Dr. Voss's gate tips: the in-level gate upgrade and moving the hero to the gate under pressure (once per save; Remco 2026-11-08).")]
        [SerializeField] private GateTip[] _gateTips = new GateTip[0];

        /// <summary>The opening cutscene; null while there is none.</summary>
        public CutsceneDefinition OpeningCutscene => _openingCutscene;

        /// <summary>The creature scans, in the order the generator wrote them.</summary>
        public System.Collections.Generic.IReadOnlyList<ScanHint> ScanHints => _scanHints ?? NoHints;

        /// <summary>The warning for a nest that joins the road behind the defence gate; null while there is none.</summary>
        public DialogueDefinition RearNestWarning => _rearNestWarning;

        /// <summary>The gate tips, in the order they are weighed.</summary>
        public System.Collections.Generic.IReadOnlyList<GateTip> GateTips => _gateTips ?? NoTips;

        private static readonly ScanHint[] NoHints = new ScanHint[0];
        private static readonly GateTip[] NoTips = new GateTip[0];

        private void OnValidate()
        {
            if (_gateTips == null)
            {
                return;
            }

            for (int i = 0; i < _gateTips.Length; i++)
            {
                GateTip tip = _gateTips[i];
                if (tip.Dialogue == null)
                {
                    Debug.LogWarning($"Story '{name}': gate tip {i} ({tip.Kind}) has no dialogue.", this);
                }

                // A quiet gate that also counts as hurt would send the hero away from a gate under attack.
                if (tip.Kind == GateTipKind.MoveHero && tip.QuietAbove <= tip.HurtBelow)
                {
                    Debug.LogWarning($"Story '{name}': gate tip {i} needs QuietAbove above HurtBelow.", this);
                }
            }
        }

        /// <summary>The intro of a boss, or null when it has none.</summary>
        public BossIntroDefinition IntroFor(EnemyDefinition boss)
        {
            if (boss == null || _bossIntros == null)
            {
                return null;
            }

            foreach (BossIntroDefinition intro in _bossIntros)
            {
                if (intro != null && intro.Boss == boss)
                {
                    return intro;
                }
            }

            return null;
        }
    }
}
