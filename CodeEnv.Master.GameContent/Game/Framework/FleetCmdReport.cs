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

        public INavigableDestination Target { get; private set; }

        public Vector3? CurrentHeading { get; private set; }

        public Speed CurrentSpeedSetting { get; private set; }

        public float? UnitFullSpeed { get; private set; }

        public float? UnitMaxTurnRate { get; private set; }

        // 7.10.16 Eliminated usage of ElementReports to calculate partial Unit values.
        // 7.18.16 If Cmd's IntelCoverage does not allow full view of selected values
        // a partial value is calculated from element's data and their infoAccessCntlr. 

        public FleetCmdReport(FleetCmdData cmdData, Player player) : base(cmdData, player) { }

        protected override void AssignValues(AItemData data) {
            var fData = data as FleetCmdData;
            var accessCntlr = fData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Hero)) {
                Hero = fData.Hero;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = fData.AlertStatus;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitDefense)) {
                UnitDefensiveStrength = fData.UnitDefensiveStrength;
            }
            else {
                UnitDefensiveStrength = CalcUnitDefensiveStrengthFromKnownElements(GetElementsData(fData));
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitOffense)) {
                UnitOffensiveStrength = fData.UnitOffensiveStrength;
            }
            else {
                UnitOffensiveStrength = CalcUnitOffensiveStrengthFromKnownElements(GetElementsData(fData));
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitMaxHitPts)) {
                UnitMaxHitPoints = fData.UnitMaxHitPoints;
            }
            else {
                UnitMaxHitPoints = CalcUnitMaxHitPointsFromKnownElements(GetElementsData(fData));
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitCurrentHitPts)) {
                UnitCurrentHitPoints = fData.UnitCurrentHitPoints;
            }
            else {
                UnitCurrentHitPoints = CalcUnitCurrentHitPointsFromKnownElements(GetElementsData(fData));
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitHealth)) {
                UnitHealth = fData.UnitHealth;
            }
            else {
                // Calculate HitPts before attempting calculation of partial unit health
                UnitHealth = CalcUnitHealthFromKnownElements(UnitCurrentHitPoints, UnitMaxHitPoints);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Name)) {
                Name = fData.Name;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitName)) {
                UnitName = fData.UnitName;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Position)) {
                Position = fData.Position;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Owner)) {
                Owner = fData.Owner;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentCmdEffectiveness)) {
                CurrentCmdEffectiveness = fData.CurrentCmdEffectiveness;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitSensorRange)) {
                UnitSensorRange = fData.UnitSensorRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitWeaponsRange)) {
                UnitWeaponsRange = fData.UnitWeaponsRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = fData.SectorID;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Formation)) {
                Formation = fData.Formation;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Target)) {
                Target = fData.Target;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Composition)) { // must precede Category
                UnitComposition = fData.UnitComposition;
            }
            else {
                UnitComposition = CalcUnitCompositionFromKnownElements(fData);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = fData.Category;
            }
            else {
                Category = CalcCmdCategoryFromKnownElements(fData);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentHeading)) {
                CurrentHeading = fData.CurrentHeading;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentSpeedSetting)) {
                CurrentSpeedSetting = fData.CurrentSpeedSetting;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitFullSpeed)) {
                UnitFullSpeed = fData.UnitFullSpeedValue;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitMaxTurnRate)) {
                UnitMaxTurnRate = fData.UnitMaxTurnRate;
            }

            __ActualSpeedValue = (Item as IFleetCmd_Ltd).ActualSpeedValue_Debug;

            UnitOutputs = AssessOutputs(fData.UnitOutputs);
        }

        private FleetComposition CalcUnitCompositionFromKnownElements(FleetCmdData cmdData) {
            var elementsData = GetElementsData(cmdData).Cast<ShipData>();
            IList<ShipHullCategory> knownElementCategories = new List<ShipHullCategory>();
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                    knownElementCategories.Add(eData.HullCategory);
                }
            }
            if (knownElementCategories.Count > Constants.Zero) {
                // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                return new FleetComposition(knownElementCategories);
            }
            return null;
        }

        private FleetCategory CalcCmdCategoryFromKnownElements(FleetCmdData cmdData) {
            if (UnitComposition != null) {
                return cmdData.GenerateCmdCategory(UnitComposition);
            }
            return FleetCategory.None;
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

