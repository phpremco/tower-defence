using System.Collections.Generic;
using Game.Balance;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The balance model's measured numbers, apart from the gameplay data (round 22, Docs/BALANCE.md "The balance pipeline"):
    /// per tower type the absolute in-play efficiency of the ledger's capacity, fitted on replayed plans, and the utilisation
    /// of the player the waves are solved for. Written only by Tools/PlayTest/Scripts/ApplyCalibration.cs from the
    /// measurement's table, never by hand. <see cref="InputHash"/> fingerprints the data the fit was made on; a different hash
    /// today means the calibration is stale and every stage says so.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Balance Calibration", fileName = "BalanceCalibration")]
    public sealed class BalanceCalibration : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Balance/BalanceCalibration.asset";

        [System.Serializable]
        public struct TowerEfficiency
        {
            public string TowerId;
            public float Efficiency;
        }

        [SerializeField] private TowerEfficiency[] _towers = new TowerEfficiency[0];

        [Tooltip("Share of its paper capacity a competent player's gun really uses (the human lens). The solver and the bands use the bot share of it.")]
        [SerializeField] [Min(0.01f)] private float _humanUtilisation = 0.34f;
        [SerializeField] [Min(0.01f)] private float _botUtilisation = 0.27f;

        [Tooltip("Structure damage per second of a siege over the paper hammer (BalanceRules.SiegeFactor); fitted on the records.")]
        [SerializeField] [Min(0.01f)] private float _siegeFactor = 1f;

        [Tooltip("Share of a wave a repair drone really spends repairing (BalanceRules.RepairDroneBusyShare); measured from the probe's drone records.")]
        [SerializeField] [Range(0f, 1f)] private float _repairDroneBusyShare = 0.5f;

        [Tooltip("InputHash of the data the fit was made on; stale when it differs from the current data's hash.")]
        [SerializeField] private string _inputHash = string.Empty;
        [SerializeField] private string _measuredOn = string.Empty;
        [SerializeField] private string _seeds = string.Empty;
        [SerializeField] [Min(0f)] private float _step;

        public IReadOnlyList<TowerEfficiency> Towers => _towers;
        public float HumanUtilisation => _humanUtilisation;
        public float BotUtilisation => _botUtilisation;
        public float SiegeFactor => _siegeFactor;
        public float RepairDroneBusyShare => _repairDroneBusyShare;
        public string InputHashValue => _inputHash;
        public string MeasuredOn => _measuredOn;
        public string Seeds => _seeds;
        public float Step => _step;

        public float EfficiencyOf(string towerId)
        {
            foreach (TowerEfficiency tower in _towers)
            {
                if (tower.TowerId == towerId)
                {
                    return tower.Efficiency;
                }
            }

            return 1f;
        }

        /// <summary>Puts the fitted numbers on a campaign model's towers and rules.</summary>
        public void Apply(CampaignModel model)
        {
            foreach (KeyValuePair<string, TowerStats> pair in model.Towers)
            {
                pair.Value.Efficiency = EfficiencyOf(pair.Key);
            }

            // A copy: the generator shares one rules instance (BalanceSheet.Rules) and must not see the fit as a hand change.
            model.Rules = model.Rules.Copy();
            model.Rules.Utilisation = _botUtilisation;
            model.Rules.HumanUtilisation = _humanUtilisation;
            model.Rules.SiegeFactor = _siegeFactor;
            model.Rules.RepairDroneBusyShare = _repairDroneBusyShare;
        }

        /// <summary>True when the data differs from what the fit was made on (or nothing was ever fitted).</summary>
        public bool IsStaleFor(string currentHash) => string.IsNullOrEmpty(_inputHash) || _inputHash != currentHash;

        /// <summary>One line for every stage's report header.</summary>
        public string Describe(string currentHash) =>
            $"calibration {(string.IsNullOrEmpty(_measuredOn) ? "never measured" : _measuredOn)}, seeds {_seeds}, step {_step}, inputs {_inputHash} ({(IsStaleFor(currentHash) ? "STALE, data now " + currentHash : "current")})";
    }
}
