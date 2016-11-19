// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatResult.cs
// Record of the results of combat between a single Weapon and its target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Record of the results of combat between a single Weapon and its target.
    /// </summary>
    public class CombatResult {

        private static string _toStringFormat = "COMBAT REPORT: Weapon: {0}, Target: {1}, Shots: {2}, Hits: {3}, Misses: {4}, Interdictions: {5}, Accuracy: {6}, HitPercentage: {7}.";

        public string WeaponName { get; private set; }

        /// <summary>
        /// The name of the target when this round of combat was initiated.
        /// <remarks>The name of the target can change during or after combat
        /// due to a user renaming action or the addition or removal of the HQ addendum.
        /// </remarks>
        /// </summary>
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

        public CombatResult(string weaponName, string targetName) {
            WeaponName = weaponName;
            TargetName = targetName;
        }

        public override string ToString() {
            int hitsAndMisses = Hits + Misses;
            string actualAccyMsg = hitsAndMisses != Constants.Zero ? "{0:P00}".Inject(Hits / (float)hitsAndMisses) : "NA";
            int hitsAndMissesAndInterdictions = hitsAndMisses + Interdictions;
            string hitPercentMsg = hitsAndMissesAndInterdictions != Constants.Zero ? "{0:P00}".Inject(Hits / (float)(hitsAndMissesAndInterdictions)) : "NA";
            return _toStringFormat.Inject(WeaponName, TargetName, ShotsTaken, Hits, Misses, Interdictions, actualAccyMsg, hitPercentMsg);
        }

    }
}

