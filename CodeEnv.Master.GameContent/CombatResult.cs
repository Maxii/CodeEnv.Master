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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Record of the results of combat between a Weapon or ActiveCM and its target.
    /// </summary>
    public class CombatResult {

        private static string _toStringFormat = "Equipment: {0}, Target: {1}, Shots: {2}, Hits: {3}, Misses: {4}, Interdictions: {5}, Accuracy: {6:P00}, RatedAccuracy: {7:P00}, HitPercentage: {8:P00}.";

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

        public float EquipmentAccuracy { get; private set; }

        public CombatResult(string equipmentName, string targetName, float equipmentAccuracy) {
            EquipmentName = equipmentName;
            TargetName = targetName;
            EquipmentAccuracy = equipmentAccuracy;
        }


        public override string ToString() {
            float actualAccuracy = Hits / (float)(Hits + Misses);
            float hitPercentage = Hits / (float)(Hits + Misses + Interdictions);
            return _toStringFormat.Inject(EquipmentName, TargetName, ShotsTaken, Hits, Misses, Interdictions, actualAccuracy, EquipmentAccuracy, hitPercentage);
        }

    }
}

