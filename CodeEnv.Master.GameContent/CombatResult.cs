// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatResult.cs
// Record of the results of combat between a Weapon or ActiveCM and its target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Record of the results of combat between a Weapon or ActiveCM and its target.
    /// </summary>
    public class CombatResult {

        private static string _toStringFormat = "COMBAT REPORT: Equipment: {0}, Target: {1}, Shots: {2}, Hits: {3}, Misses: {4}, Interdictions: {5}, Accuracy: {6}, HitPercentage: {7}.";

        public string EquipmentName { get; private set; }

        public string TargetName { get; private set; }

        public int ShotsTaken { get; set; }

        public int Hits { get; set; }

        /// <summary>
        /// The number of times the shot missed the target.
        /// A shot that is fatally interdicted is not a miss.
        /// </summary>
        public int Misses { get; set; }

        /// <summary>
        /// The number of times a shot was fatally interdicted.
        /// </summary>
        public int Interdictions { get; set; }

        public CombatResult(string equipmentName, string targetName) {
            EquipmentName = equipmentName;
            TargetName = targetName;
        }

        public override string ToString() {
            int hitsAndMisses = Hits + Misses;
            string actualAccyMsg = hitsAndMisses != Constants.Zero ? "{0:P00}".Inject(Hits / (float)hitsAndMisses) : "NA";
            int hitsAndMissesAndInterdictions = hitsAndMisses + Interdictions;
            string hitPercentMsg = hitsAndMissesAndInterdictions != Constants.Zero ? "{0:P00}".Inject(Hits / (float)(hitsAndMissesAndInterdictions)) : "NA";
            return _toStringFormat.Inject(EquipmentName, TargetName, ShotsTaken, Hits, Misses, Interdictions, actualAccyMsg, hitPercentMsg);
        }

    }
}

