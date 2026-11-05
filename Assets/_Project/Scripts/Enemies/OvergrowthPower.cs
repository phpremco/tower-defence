using System;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a boss that overgrows the build pads (Rotbloom, Docs/BOSSES.md "Overgrowth"): its spore volleys and
    /// their warning, what an overgrown pad costs, how it creeps rooted and walks uprooted, and the roots that announce it
    /// over the waves before it comes.
    /// </summary>
    [Serializable]
    public sealed class OvergrowthPower
    {
        [Header("Spore volley")]
        [Tooltip("Seconds between two volleys.")]
        [SerializeField] [Min(1f)] private float _interval = 12f;
        [Tooltip("Pads one volley overgrows at most.")]
        [SerializeField] [Min(1)] private int _maxPads = 3;
        [Tooltip("Seconds the target pads glow green before the pods land.")]
        [SerializeField] [Min(0.5f)] private float _warnSeconds = 2f;
        [Tooltip("How far from it a pad may be to be spored (world units).")]
        [SerializeField] [Min(1f)] private float _reach = 16f;
        [Tooltip("Pads within this many world units of each other count as one cluster: it goes for the fullest one.")]
        [SerializeField] [Min(0.5f)] private float _clusterRadius = 2.6f;
        [Tooltip("The pod it fires: a small flyer the air towers can shoot down before it lands.")]
        [SerializeField] private EnemyDefinition _pod;
        [Tooltip("Height of the pods' arc at its top (world units).")]
        [SerializeField] [Min(0.5f)] private float _arcHeight = 3.5f;
        [Tooltip("Seconds a pod takes in the air at the least, whatever the warning leaves.")]
        [SerializeField] [Min(0.2f)] private float _minFlightSeconds = 0.7f;

        [Header("Overgrown pad")]
        [Tooltip("Fire rate share of a tower on an overgrown pad, until splash or the hero burns the vines off.")]
        [SerializeField] [Range(0.1f, 1f)] private float _fireRate = 0.7f;
        [Tooltip("The vines that grow over an overgrown pad (README round 28: RotOvergrowth, blend shape Retracted).")]
        [SerializeField] private ModelArtDefinition _vines;
        [Tooltip("Seconds the vines take to grow over the pad.")]
        [SerializeField] [Min(0.1f)] private float _growSeconds = 1f;
        [Tooltip("Seconds a pad stays overgrown before the player burns it clean, for the balance ledger.")]
        [SerializeField] [Min(0f)] private float _expectedOvergrownSeconds = 20f;
        [Tooltip("Volleys in one fight, for the balance ledger.")]
        [SerializeField] [Min(0)] private int _expectedVolleys = 4;

        [Header("Rooted and uprooted")]
        [Tooltip("The phase from which it tears its roots out and walks (Uprooted, below half its health).")]
        [SerializeField] [Min(0)] private int _uprootPhase = 1;
        [Tooltip("Speed factor while it creeps rooted, its roots growing ahead of it.")]
        [SerializeField] [Range(0.05f, 1f)] private float _rootedPace = 0.35f;
        [Tooltip("World units it creeps between two fresh root clumps ahead of it.")]
        [SerializeField] [Min(0.5f)] private float _rootSpacing = 3f;
        [Tooltip("Seconds a patch of the rot it leaves uprooted lasts.")]
        [SerializeField] [Min(0f)] private float _rotSeconds = 8f;

        [Header("Roots before it comes")]
        [Tooltip("The roots that spread over the waves before it comes (README round 28: RotbloomRoots Stage1..3).")]
        [SerializeField] private ModelArtDefinition _roots;
        [Tooltip("The roots' stages in order, by object name; one shows per wave before the boss wave, the last the wave before it.")]
        [SerializeField] private string[] _rootStages = { "RotbloomRoots_Stage1", "RotbloomRoots_Stage2", "RotbloomRoots_Stage3" };

        public float Interval => _interval;
        public int MaxPads => _maxPads;
        public float WarnSeconds => _warnSeconds;
        public float Reach => _reach;
        public float ClusterRadius => _clusterRadius;
        public EnemyDefinition Pod => _pod;
        public float ArcHeight => _arcHeight;
        public float MinFlightSeconds => _minFlightSeconds;
        public float FireRate => _fireRate;
        public ModelArtDefinition Vines => _vines;
        public float GrowSeconds => _growSeconds;
        public float ExpectedOvergrownSeconds => _expectedOvergrownSeconds;
        public int ExpectedVolleys => _expectedVolleys;
        public int UprootPhase => _uprootPhase;
        public float RootedPace => _rootedPace;
        public float RootSpacing => _rootSpacing;
        public float RotSeconds => _rotSeconds;
        public ModelArtDefinition Roots => _roots;
        public string[] RootStages => _rootStages ?? new string[0];

        /// <summary>The root stage to show with that many waves still to come before the boss wave, or -1 for none.</summary>
        public int StageFor(int wavesBefore)
        {
            int stages = RootStages.Length;
            return wavesBefore >= 0 && wavesBefore < stages + 1 ? Mathf.Clamp(stages - wavesBefore, 0, stages - 1) : -1;
        }
    }
}
