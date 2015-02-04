// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipReport.cs
//  Immutable report on a ship reflecting a specific player's IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Immutable report on a ship reflecting a specific player's IntelCoverage.
    /// </summary>
    public class ShipReport : AElementItemReport {

        //public bool IsFtlOperational { get; private set; }
        //public bool IsFtlDampedByField { get; private set; }
        //public bool IsFtlAvailableForUse { get; private set; }
        //public bool IsFlapsDeployed { get; private set; }

        public INavigableTarget Target { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public float? CurrentSpeed { get; private set; }

        //public float RequestedSpeed { get; private set; }
        //public float Drag { get; private set; }
        //public float FullThrust { get; private set; }
        //public float FullStlThrust { get; private set; }
        //public float FullFtlThrust { get; private set; }
        //public Vector3 RequestedHeading { get; private set; }
        //public Vector3 CurrentHeading { get; private set; }

        public float? FullSpeed { get; private set; }

        //public float FullFtlSpeed { get; private set; }
        //public float FullStlSpeed { get; private set; }

        public float? MaxTurnRate { get; private set; }

        public ShipCategory Category { get; private set; }

        public ShipReport(ShipData data, Player player) : base(data, player) { }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            var sData = data as ShipData;
            CurrentHitPoints = sData.CurrentHitPoints;
            Health = sData.Health;
            CombatStance = sData.CombatStance;
            Target = sData.Target;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            var sData = data as ShipData;
            DefensiveStrength = sData.DefensiveStrength;
            OffensiveStrength = sData.OffensiveStrength;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            var sData = data as ShipData;
            ParentName = sData.ParentName;
            Owner = sData.Owner;
            Category = sData.Category;
            MaxSensorRange = sData.MaxSensorRange;
            MaxHitPoints = sData.MaxHitPoints;
            MaxWeaponsRange = sData.MaxWeaponsRange;
            Mass = sData.Mass;
            MaxTurnRate = sData.MaxTurnRate;
            FullSpeed = sData.FullSpeed;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            var sData = data as ShipData;
            Name = sData.Name;
            CurrentSpeed = sData.CurrentSpeed;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

