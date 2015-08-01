// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdReport.cs
// Abstract class for Reports associated with UnitCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports associated with UnitCmdItems.
    /// </summary>
    public abstract class ACmdReport : AMortalItemReport {

        public string ParentName { get; private set; }

        public int? CurrentCmdEffectiveness { get; private set; }

        //public int? MaxCmdEffectiveness { get; protected set; }
        //public float? MaxHitPoints { get; protected set; }
        //public float? CurrentHitPoints { get; protected set; }
        //public float? Health { get; protected set; }
        //public CombatStrength? DefensiveStrength { get; protected set; }

        public Formation UnitFormation { get; private set; }

        public RangeDistance? UnitWeaponsRange { get; private set; }

        public RangeDistance? UnitSensorRange { get; private set; }

        public CombatStrength? UnitOffensiveStrength { get; private set; }

        public CombatStrength? UnitDefensiveStrength { get; private set; }

        public float? UnitMaxHitPoints { get; private set; }

        public float? UnitCurrentHitPoints { get; private set; }

        public float? UnitHealth { get; private set; }

        public float? UnitScience { get; private set; }

        public float? UnitCulture { get; private set; }

        public float? UnitIncome { get; private set; }

        public float? UnitExpense { get; private set; }

        public ACmdReport(AUnitCmdItemData data, Player player, IUnitCmdItem item)
            : base(data, player, item) {
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            AUnitCmdItemData cmdData = data as AUnitCmdItemData;
            UnitFormation = cmdData.UnitFormation;
            CurrentCmdEffectiveness = cmdData.CurrentCmdEffectiveness;
            UnitCulture = cmdData.UnitCulture;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            AUnitCmdItemData cmdData = data as AUnitCmdItemData;
            Owner = cmdData.Owner;
            UnitIncome = cmdData.UnitIncome;
            UnitSensorRange = cmdData.UnitSensorRange;
            UnitWeaponsRange = cmdData.UnitWeaponsRange;
        }

        protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageEssential(data);
            AUnitCmdItemData cmdData = data as AUnitCmdItemData;
            Name = cmdData.Name;
            ParentName = cmdData.ParentName;
            Position = cmdData.Position;
        }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            AUnitCmdItemData cmdData = data as AUnitCmdItemData;
            SectorIndex = cmdData.SectorIndex;
        }

        protected void AssignValuesFrom(AElementItemReport[] elementReports) {
            UnitDefensiveStrength = elementReports.Select(er => er.DefensiveStrength).NullableSum();
            UnitOffensiveStrength = elementReports.Select(er => er.OffensiveStrength).NullableSum();

            UnitMaxHitPoints = elementReports.Select(er => er.MaxHitPoints).NullableSum();
            UnitCurrentHitPoints = elementReports.Select(er => er.CurrentHitPoints).NullableSum();
            //elementReports.ForAll(er => {
            //    D.Log("{0}.{1}.{2} IntelCoverage = {3}, CurrentHitPts = {4}, MaxHitPts = {5}, Health = {6}.", 
            //        cmdData.FullName, er.GetType().Name, er.Name, er.IntelCoverage.GetName(), er.CurrentHitPoints, er.MaxHitPoints, er.Health);
            //});

            UnitHealth = CalcUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);

            UnitScience = elementReports.Select(er => er.Science).NullableSum();
            UnitExpense = elementReports.Select(er => er.Expense).NullableSum();
        }

        private float? CalcUnitHealth(float? unitCurrentHitPts, float? unitMaxHitPts) {
            if (!UnitMaxHitPoints.HasValue || !UnitCurrentHitPoints.HasValue) {
                return null;
            }
            D.Warn(UnitCurrentHitPoints > UnitMaxHitPoints, "{0}.CurrentHitPts {1} > MaxHitPts {2}.", ParentName, UnitCurrentHitPoints, UnitMaxHitPoints);
            // The above warning can occur if Element CurrentHitPts is made available at a more restricted IntelCoverage than MaxHitPts
            return UnitMaxHitPoints.Value > Constants.ZeroF ? UnitCurrentHitPoints.Value / UnitMaxHitPoints.Value : Constants.ZeroF;
        }

    }
}

