// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityReport.cs
// Immutable report on a facility reflecting a specific player's IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    ///  Immutable report on a facility reflecting a specific player's IntelCoverage.
    /// </summary>
    public class FacilityReport : AElementItemReport {

        public FacilityCategory Category { get; private set; }

        public FacilityReport(FacilityData data, Player player) : base(data, player) { }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            var fData = data as FacilityData;
            CurrentHitPoints = fData.CurrentHitPoints;
            Health = fData.Health;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            var fData = data as FacilityData;
            DefensiveStrength = fData.DefensiveStrength;
            OffensiveStrength = fData.OffensiveStrength;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            var fData = data as FacilityData;
            ParentName = fData.ParentName;
            Owner = fData.Owner;
            Category = fData.Category;
            MaxHitPoints = fData.MaxHitPoints;
            Mass = fData.Mass;
            MaxSensorRange = fData.MaxSensorRange;
            MaxWeaponsRange = fData.MaxWeaponsRange;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            var fData = data as FacilityData;
            Name = fData.Name;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

