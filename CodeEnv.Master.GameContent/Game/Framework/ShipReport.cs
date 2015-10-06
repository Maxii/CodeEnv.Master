// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipReport.cs
// Immutable report for ShipItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for ShipItems.
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
        //public float FullFtlSpeed { get; private set; }
        //public float FullStlSpeed { get; private set; }

        public float? FullSpeed { get; private set; }

        public float? MaxTurnRate { get; private set; }

        public ShipHullCategory Category { get; private set; }

        public ShipReport(ShipData data, Player player, IShipItem item) : base(data, player, item) { }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var sData = data as ShipData;
            CombatStance = sData.CombatStance;
            Target = sData.Target;
            Culture = sData.Culture;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            var sData = data as ShipData;
            DefensiveStrength = sData.DefensiveStrength;
            OffensiveStrength = sData.OffensiveStrength;
            Health = sData.Health;  // Element Health only matters on Element displays. UnitHealth calc'd using Element HitPts
            CurrentHitPoints = sData.CurrentHitPoints;
            Science = sData.Science;
            Income = sData.Income;
        }

        protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageEssential(data);
            var sData = data as ShipData;
            ParentName = sData.ParentName;
            Position = sData.Position;
            Owner = sData.Owner;
            Category = sData.HullCategory;
            WeaponsRange = sData.WeaponsRange;
            SensorRange = sData.SensorRange;
            MaxHitPoints = sData.MaxHitPoints; // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
            Mass = sData.Mass;
            MaxTurnRate = sData.MaxTurnRate;
            FullSpeed = sData.FullSpeed;
            Expense = sData.Expense;
        }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            var sData = data as ShipData;
            Name = sData.Name;
            CurrentSpeed = sData.CurrentSpeed;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

