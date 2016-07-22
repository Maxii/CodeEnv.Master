// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdReport.cs
// Abstract class for Reports associated with UnitCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Reports associated with UnitCmdItems.
    /// </summary>
    public abstract class AUnitCmdReport : AMortalItemReport {

        public string ParentName { get; protected set; }

        public float? CurrentCmdEffectiveness { get; protected set; }

        public Formation UnitFormation { get; protected set; }

        public RangeDistance? UnitWeaponsRange { get; protected set; }

        public RangeDistance? UnitSensorRange { get; protected set; }

        public CombatStrength? UnitOffensiveStrength { get; protected set; }

        public CombatStrength? UnitDefensiveStrength { get; protected set; }

        public float? UnitMaxHitPoints { get; protected set; }

        public float? UnitCurrentHitPoints { get; protected set; }

        public float? UnitHealth { get; protected set; }

        public float? UnitScience { get; protected set; }

        public float? UnitCulture { get; protected set; }

        public float? UnitIncome { get; protected set; }

        public float? UnitExpense { get; protected set; }

        private IEnumerable<AUnitElementData> _cachedElementsData;
        protected IEnumerable<AUnitElementData> GetElementsData(AUnitCmdData cmdData) {
            if (_cachedElementsData == null) {
                _cachedElementsData = cmdData.ElementsData;
            }
            return _cachedElementsData;
        }

        public AUnitCmdReport(AUnitCmdData data, Player player, IUnitCmd_Ltd item)
            : base(data, player, item) {
        }

        protected CombatStrength? CalcPartialUnitOffensiveStrength(IEnumerable<AUnitElementData> elementsData) {
            IList<CombatStrength> elementsStrength = new List<CombatStrength>(elementsData.Count());
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Offense)) {
                    elementsStrength.Add(eData.OffensiveStrength);
                }
            }
            if (elementsStrength.Any()) {
                return elementsStrength.Sum();
            }
            return null;
        }

        protected CombatStrength? CalcPartialUnitDefensiveStrength(IEnumerable<AUnitElementData> elementsData) {
            IList<CombatStrength> elementsStrength = new List<CombatStrength>(elementsData.Count());
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Defense)) {
                    elementsStrength.Add(eData.DefensiveStrength);
                }
            }
            if (elementsStrength.Any()) {
                return elementsStrength.Sum();
            }
            return null;
        }

        protected float? CalcPartialUnitMaxHitPoints(IEnumerable<AUnitElementData> elementsData) {
            float elementsHitPts = Constants.ZeroF;
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.MaxHitPoints)) {
                    elementsHitPts += eData.MaxHitPoints;
                }
            }
            if (elementsHitPts > Constants.ZeroF) {
                return elementsHitPts;
            }
            return null;
        }

        protected float? CalcPartialUnitCurrentHitPoints(IEnumerable<AUnitElementData> elementsData) {
            float elementsHitPts = Constants.ZeroF;
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CurrentHitPoints)) {
                    elementsHitPts += eData.CurrentHitPoints;
                }
            }
            if (elementsHitPts > Constants.ZeroF) {
                return elementsHitPts;
            }
            return null;
        }

        protected float? CalcPartialUnitHealth(float? unitCurrentHitPts, float? unitMaxHitPts) {
            if (!unitMaxHitPts.HasValue || !unitCurrentHitPts.HasValue) {
                return null;
            }
            D.Warn(unitCurrentHitPts > unitMaxHitPts, "{0}.CurrentHitPts {1} > MaxHitPts {2}.", ParentName, unitCurrentHitPts, unitMaxHitPts);
            // The above warning can occur if Element CurrentHitPts is made available at a more restricted IntelCoverage than MaxHitPts
            if (unitMaxHitPts.Value > Constants.ZeroF) {
                return unitCurrentHitPts.Value / unitMaxHitPts.Value;
            }
            return null;
        }

        protected float? CalcPartialUnitScience(IEnumerable<AUnitElementData> elementsData) {
            IList<float> elementsScience = new List<float>(elementsData.Count());
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Science)) {
                    elementsScience.Add(eData.Science);
                }
            }
            if (elementsScience.Any()) {
                return elementsScience.Sum();
            }
            return null;
        }

        // IMPROVE other partial calcs can be added like income, expense, culture, etc.

        #region Archive

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    AUnitCmdData cmdData = data as AUnitCmdData;
        //    UnitFormation = cmdData.UnitFormation;
        //    UnitMaxFormationRadius = cmdData.UnitMaxFormationRadius;
        //    CurrentCmdEffectiveness = cmdData.CurrentCmdEffectiveness;
        //    UnitCulture = cmdData.UnitCulture;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    AUnitCmdData cmdData = data as AUnitCmdData;
        //    Owner = cmdData.Owner;
        //    UnitIncome = cmdData.UnitIncome;
        //    UnitSensorRange = cmdData.UnitSensorRange;
        //    UnitWeaponsRange = cmdData.UnitWeaponsRange;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageEssential(data);
        //    AUnitCmdData cmdData = data as AUnitCmdData;
        //    Name = cmdData.Name;
        //    ParentName = cmdData.ParentName;
        //    Position = cmdData.Position;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBasic(data);
        //    AUnitCmdData cmdData = data as AUnitCmdData;
        //    SectorIndex = cmdData.SectorIndex;
        //}

        //protected void AssignValuesFrom(AUnitElementReport[] elementReports) {
        //   UnitDefensiveStrength = elementReports.Select(er => er.DefensiveStrength).NullableSum();
        //    UnitOffensiveStrength = elementReports.Select(er => er.OffensiveStrength).NullableSum();

        //    UnitMaxHitPoints = elementReports.Select(er => er.MaxHitPoints).NullableSum();
        //    UnitCurrentHitPoints = elementReports.Select(er => er.CurrentHitPoints).NullableSum();
        //    //elementReports.ForAll(er => {
        //    //    D.Log("{0}.{1}.{2} IntelCoverage = {3}, CurrentHitPts = {4}, MaxHitPts = {5}, Health = {6}.", 
        //    //        cmdData.FullName, er.GetType().Name, er.Name, er.IntelCoverage.GetValueName(), er.CurrentHitPoints, er.MaxHitPoints, er.Health);
        //    //});

        //    UnitHealth = CalcUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);

        //    UnitScience = elementReports.Select(er => er.Science).NullableSum();
        //    UnitExpense = elementReports.Select(er => er.Expense).NullableSum();
        //}

        //private float? CalcUnitHealth(float? unitCurrentHitPts, float? unitMaxHitPts) {
        //    if (!UnitMaxHitPoints.HasValue || !UnitCurrentHitPoints.HasValue) {
        //        return null;
        //    }
        //    D.Warn(UnitCurrentHitPoints > UnitMaxHitPoints, "{0}.CurrentHitPts {1} > MaxHitPts {2}.", ParentName, UnitCurrentHitPoints, UnitMaxHitPoints);
        //    // The above warning can occur if Element CurrentHitPts is made available at a more restricted IntelCoverage than MaxHitPts
        //    return UnitMaxHitPoints.Value > Constants.ZeroF ? UnitCurrentHitPoints.Value / UnitMaxHitPoints.Value : Constants.ZeroF;
        //}

        #endregion

    }
}

