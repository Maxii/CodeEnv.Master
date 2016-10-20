// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdReport.cs
// Immutable report on a fleet reflecting a specific player's IntelCoverage of
// the fleet's command and its elements.
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
    ///Immutable report for FleetCmdItems.
    /// </summary>
    public class FleetCmdReport : AUnitCmdReport {

        public Reference<float> __ActualSpeedValue { get; private set; }

        public FleetCategory Category { get; private set; }

        /// <summary>
        /// The Composition of the Unit this report is about. The unit's elements
        /// reported will be limited to those elements the Player requesting
        /// the report has knowledge of. 
        /// <remarks>Can be null - even though the Player will 
        /// always know about the HQElement of the Unit (since he knows about
        /// the UnitCmd itself), he may not know the Category of the HQElement.
        /// </remarks>
        /// </summary>
        public FleetComposition UnitComposition { get; private set; }

        public INavigable Target { get; private set; }

        public Vector3? CurrentHeading { get; private set; }

        public Speed CurrentSpeedSetting { get; private set; }

        public float? UnitFullSpeed { get; private set; }

        public float? UnitMaxTurnRate { get; private set; }

        // 7.10.16 Eliminated usage of ElementReports to calculate partial Unit values.
        // 7.18.16 If Cmd's IntelCoverage does not allow full view of selected values
        // a partial value is calculated from element's data and their infoAccessCntlr. 

        public FleetCmdReport(FleetCmdData cmdData, Player player, IFleetCmd_Ltd item)
            : base(cmdData, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var fData = data as FleetCmdData;
            var accessCntlr = fData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitDefense)) {
                UnitDefensiveStrength = fData.UnitDefensiveStrength;
            }
            else {
                UnitDefensiveStrength = CalcPartialUnitDefensiveStrength(GetElementsData(fData));
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitOffense)) {
                UnitOffensiveStrength = fData.UnitOffensiveStrength;
            }
            else {
                UnitOffensiveStrength = CalcPartialUnitOffensiveStrength(GetElementsData(fData));
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitMaxHitPts)) {
                UnitMaxHitPoints = fData.UnitMaxHitPoints;
            }
            else {
                UnitMaxHitPoints = CalcPartialUnitMaxHitPoints(GetElementsData(fData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitCurrentHitPts)) {
                UnitCurrentHitPoints = fData.UnitCurrentHitPoints;
            }
            else {
                UnitCurrentHitPoints = CalcPartialUnitCurrentHitPoints(GetElementsData(fData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitHealth)) {
                UnitHealth = fData.UnitHealth;
            }
            else {
                // Calculate HitPts before attempting calculation of partial unit health
                UnitHealth = CalcPartialUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitScience)) {
                UnitScience = fData.UnitScience;
            }
            else {
                UnitScience = CalcPartialUnitScience(GetElementsData(fData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitNetIncome)) {
                UnitIncome = fData.UnitIncome;
                UnitExpense = fData.UnitExpense;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitCulture)) {
                UnitCulture = fData.UnitCulture;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = fData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.ParentName)) {
                ParentName = fData.ParentName;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = fData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = fData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentCmdEffectiveness)) {
                CurrentCmdEffectiveness = fData.CurrentCmdEffectiveness;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitSensorRange)) {
                UnitSensorRange = fData.UnitSensorRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitWeaponsRange)) {
                UnitWeaponsRange = fData.UnitWeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = fData.SectorID;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Formation)) {
                UnitFormation = fData.UnitFormation;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Target)) {
                Target = fData.Target;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Composition)) { // must precede Category
                UnitComposition = fData.UnitComposition;
            }
            else {
                UnitComposition = CalcPartialUnitComposition(fData);
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = fData.Category;
            }
            else {
                Category = CalcPartialCmdCategory(fData);
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentHeading)) {
                CurrentHeading = fData.CurrentHeading;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentSpeedSetting)) {
                CurrentSpeedSetting = fData.CurrentSpeedSetting;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitFullSpeed)) {
                UnitFullSpeed = fData.UnitFullSpeedValue;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitMaxTurnRate)) {
                UnitMaxTurnRate = fData.UnitMaxTurnRate;
            }

            __ActualSpeedValue = (Item as IFleetCmd_Ltd).ActualSpeedValue_Debug;
        }

        private FleetComposition CalcPartialUnitComposition(FleetCmdData cmdData) {
            var elementsData = GetElementsData(cmdData).Cast<ShipData>();
            IList<ShipHullCategory> knownElementCategories = new List<ShipHullCategory>();
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                    knownElementCategories.Add(eData.HullCategory);
                }
            }
            if (knownElementCategories.Count > Constants.Zero) {
                // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                return new FleetComposition(knownElementCategories);
            }
            return null;
        }

        private FleetCategory CalcPartialCmdCategory(FleetCmdData cmdData) {
            if (UnitComposition != null) {
                return cmdData.GenerateCmdCategory(UnitComposition);
            }
            return FleetCategory.None;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //private void AssignValuesFromElementReports(FleetCmdData cmdData) {
        //    var knownElementCategories = ElementReports.Select(r => r.Category).Where(cat => cat != default(ShipHullCategory));
        //    if (knownElementCategories.Any()) { // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
        //        UnitComposition = new FleetComposition(knownElementCategories);
        //    }
        //    Category = UnitComposition != null ? cmdData.GenerateCmdCategory(UnitComposition) : FleetCategory.None;
        //    AssignValuesFrom(ElementReports);
        //}

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    FleetCmdData fData = data as FleetCmdData;
        //    UnitFullSpeed = fData.UnitFullSpeedValue;
        //    UnitMaxTurnRate = fData.UnitMaxTurnRate;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    FleetCmdData fData = data as FleetCmdData;
        //    Target = fData.Target;
        //    CurrentSpeed = fData.ActualSpeedValue;
        //}

        #endregion

    }
}

