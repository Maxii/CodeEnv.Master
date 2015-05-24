// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityReport.cs
// Immutable report for FacilityItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for FacilityItems.
    /// </summary>
    public class FacilityReport : AElementItemReport {

        public FacilityCategory Category { get; private set; }

        public FacilityReport(FacilityData data, Player player, IFacilityItem item) : base(data, player, item) { }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var fData = data as FacilityData;
            Culture = fData.Culture;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            var fData = data as FacilityData;
            DefensiveStrength = fData.DefensiveStrength;
            OffensiveStrength = fData.OffensiveStrength;
            Health = fData.Health;  // Element Health only matters on Element displays. UnitHealth calc'd using Element HitPts
            CurrentHitPoints = fData.CurrentHitPoints;
            Science = fData.Science;
            Income = fData.Income;
        }

        protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageEssential(data);
            var fData = data as FacilityData;
            ParentName = fData.ParentName;
            Position = fData.Position;
            Owner = fData.Owner;
            Category = fData.Category;
            MaxHitPoints = fData.MaxHitPoints;  // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
            Mass = fData.Mass;
            MaxSensorRange = fData.MaxSensorRange;
            MaxWeaponsRange = fData.MaxWeaponsRange;
            Expense = fData.Expense;
        }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            var fData = data as FacilityData;
            Name = fData.Name;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

