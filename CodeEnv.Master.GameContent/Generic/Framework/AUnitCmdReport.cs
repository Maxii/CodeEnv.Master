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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        public string UnitName { get; protected set; }

        public float? CurrentCmdEffectiveness { get; protected set; }

        public Formation Formation { get; protected set; }

        public AlertStatus AlertStatus { get; protected set; }

        public RangeDistance? UnitWeaponsRange { get; protected set; }

        public RangeDistance? UnitSensorRange { get; protected set; }

        public CombatStrength? UnitOffensiveStrength { get; protected set; }

        public CombatStrength? UnitDefensiveStrength { get; protected set; }

        public float? UnitMaxHitPoints { get; protected set; }

        public float? UnitCurrentHitPoints { get; protected set; }

        public float? UnitHealth { get; protected set; }

        public OutputsYield UnitOutputs { get; protected set; }

        public Hero Hero { get; protected set; }

        public new IUnitCmd_Ltd Item { get { return base.Item as IUnitCmd_Ltd; } }

        private IEnumerable<AUnitElementData> _cachedElementsData;
        protected IEnumerable<AUnitElementData> GetElementsData(AUnitCmdData cmdData) {
            if (_cachedElementsData == null) {
                _cachedElementsData = cmdData.ElementsData;
            }
            return _cachedElementsData;
        }

        public AUnitCmdReport(AUnitCmdData data, Player player) : base(data, player) { }

        protected CombatStrength? CalcUnitOffensiveStrengthFromKnownElements(IEnumerable<AUnitElementData> elementsData) {
            IList<CombatStrength> elementsStrength = new List<CombatStrength>(elementsData.Count());
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Offense)) {
                    elementsStrength.Add(eData.OffensiveStrength);
                }
            }
            if (elementsStrength.Any()) {
                return elementsStrength.Sum();
            }
            return null;
        }

        protected CombatStrength? CalcUnitDefensiveStrengthFromKnownElements(IEnumerable<AUnitElementData> elementsData) {
            IList<CombatStrength> elementsStrength = new List<CombatStrength>(elementsData.Count());
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Defense)) {
                    elementsStrength.Add(eData.DefensiveStrength);
                }
            }
            if (elementsStrength.Any()) {
                return elementsStrength.Sum();
            }
            return null;
        }

        protected float? CalcUnitMaxHitPointsFromKnownElements(IEnumerable<AUnitElementData> elementsData) {
            //D.Log("{0}.CalcPartialUnitMaxHitPoints called.", Name + GetType().Name);
            float elementsHitPts = Constants.ZeroF;
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.MaxHitPoints)) {
                    elementsHitPts += eData.MaxHitPoints;
                }
            }
            if (elementsHitPts > Constants.ZeroF) {
                return elementsHitPts;
            }
            return null;
        }

        protected float? CalcUnitCurrentHitPointsFromKnownElements(IEnumerable<AUnitElementData> elementsData) {
            float elementsHitPts = Constants.ZeroF;
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentHitPoints)) {
                    elementsHitPts += eData.CurrentHitPoints;
                }
            }
            if (elementsHitPts > Constants.ZeroF) {
                return elementsHitPts;
            }
            return null;
        }

        protected float? CalcUnitHealthFromKnownElements(float? unitCurrentHitPts, float? unitMaxHitPts) {
            if (!unitMaxHitPts.HasValue || !unitCurrentHitPts.HasValue) {
                return null;
            }
            if (unitCurrentHitPts > unitMaxHitPts) {
                D.Warn("{0}.CurrentHitPts {1} > MaxHitPts {2}.", UnitName, unitCurrentHitPts, unitMaxHitPts);
            }
            // The above warning can occur if Element CurrentHitPts is made available at a more restricted IntelCoverage than MaxHitPts
            if (unitMaxHitPts.Value > Constants.ZeroF) {
                return unitCurrentHitPts.Value / unitMaxHitPts.Value;
            }
            return null;
        }

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
        //    SectorID = cmdData.SectorID;
        //}

        //protected void AssignValuesFrom(AUnitElementReport[] elementReports) {
        //   UnitDefensiveStrength = elementReports.Select(e => e.DefensiveStrength).NullableSum();
        //    UnitOffensiveStrength = elementReports.Select(e => e.OffensiveStrength).NullableSum();

        //    UnitMaxHitPoints = elementReports.Select(e => e.MaxHitPoints).NullableSum();
        //    UnitCurrentHitPoints = elementReports.Select(e => er.CurrentHitPoints).NullableSum();
        //    //elementReports.ForAll(e => {
        //    //    D.Log("{0}.{1}.{2} IntelCoverage = {3}, CurrentHitPts = {4}, MaxHitPts = {5}, Health = {6}.", 
        //    //        cmdData.DebugName, e.GetType().Name, e.Name, e.IntelCoverage.GetValueName(), e.CurrentHitPoints, e.MaxHitPoints, e.Health);
        //    //});

        //    UnitHealth = CalcUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);

        //    UnitScience = elementReports.Select(e => e.Science).NullableSum();
        //    UnitExpense = elementReports.Select(e => e.Expense).NullableSum();
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

