// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdReport.cs
// Abstract class for all Command Reports.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Abstract class for all Command Reports.
    /// </summary>
    public abstract class ACmdReport : AItemReport {

        public string ParentName { get; protected set; }

        public Formation UnitFormation { get; protected set; }

        public int? CurrentCmdEffectiveness { get; protected set; }

        //public int? MaxCmdEffectiveness { get; protected set; }
        //public float? MaxHitPoints { get; protected set; }
        //public float? CurrentHitPoints { get; protected set; }
        //public float? Health { get; protected set; }
        //public CombatStrength? DefensiveStrength { get; protected set; }

        public float? UnitMaxWeaponsRange { get; protected set; }

        public float? UnitMaxSensorRange { get; protected set; }

        public CombatStrength? UnitOffensiveStrength { get; protected set; }

        public CombatStrength? UnitDefensiveStrength { get; protected set; }

        public float? UnitMaxHitPoints { get; protected set; }

        public float? UnitCurrentHitPoints { get; protected set; }

        public float? UnitHealth { get; protected set; }


        public AElementItemReport[] ElementReports { get; private set; }

        public ACmdReport(ACommandData data, Player player, AElementItemReport[] elementReports)
            : base(data, player) {
            ElementReports = elementReports;
            AssignValuesFrom(elementReports, data);
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            ACommandData cmdData = data as ACommandData;
            UnitFormation = cmdData.UnitFormation;
            CurrentCmdEffectiveness = cmdData.CurrentCmdEffectiveness;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            Owner = data.Owner;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageMinimal(data);
            Name = data.Name;
        }

        protected virtual void AssignValuesFrom(AElementItemReport[] elementReports, ACommandData cmdData) {
            UnitDefensiveStrength = elementReports.Select(er => er.DefensiveStrength).Sum();
            UnitOffensiveStrength = elementReports.Select(er => er.OffensiveStrength).Sum();

            UnitMaxHitPoints = elementReports.Select(er => er.MaxHitPoints).Sum();
            UnitCurrentHitPoints = elementReports.Select(er => er.CurrentHitPoints).Sum();

            UnitHealth = CalcUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);

            UnitMaxSensorRange = elementReports.Select(er => er.MaxSensorRange).Max();
            UnitMaxWeaponsRange = elementReports.Select(er => er.MaxWeaponsRange).Max();
        }

        private float? CalcUnitHealth(float? unitCurrentHitPts, float? unitMaxHitPts) {
            if (!UnitMaxHitPoints.HasValue || !UnitCurrentHitPoints.HasValue) {
                return null;
            }
            return UnitMaxHitPoints.Value > Constants.ZeroF ? UnitCurrentHitPoints.Value / UnitMaxHitPoints.Value : Constants.ZeroF;
        }

    }
}

