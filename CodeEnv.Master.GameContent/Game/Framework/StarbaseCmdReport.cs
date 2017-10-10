// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdReport.cs
// Immutable report for StarbaseCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for StarbaseCmdItems.
    /// </summary>
    public class StarbaseCmdReport : AUnitCmdReport {

        public StarbaseCategory Category { get; private set; }

        public int? Capacity { get; private set; }
        public float? UnitFood { get; private set; }
        public float? UnitProduction { get; private set; }

        public ConstructionInfo CurrentConstruction { get; private set; }
        public ResourceYield? Resources { get; private set; }

        /// <summary>
        /// The Composition of the Unit this report is about. The unit's elements
        /// reported will be limited to those elements the Player requesting
        /// the report has knowledge of. 
        /// <remarks>Can be null - even though the Player will 
        /// always know about the HQElement of the Unit (since he knows about
        /// the UnitCmd itself), he may not know the Category of the HQElement.
        /// </remarks>
        /// </summary>
        public BaseComposition UnitComposition { get; private set; }

        // 7.10.16 Eliminated usage of ElementReports to calculate partial Unit values.
        // 7.18.16 If Cmd's IntelCoverage does not allow full view of selected values
        // a partial value is calculated from element's data and their infoAccessCntlr. 

        public StarbaseCmdReport(StarbaseCmdData cmdData, Player player, IStarbaseCmd_Ltd item)
            : base(cmdData, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var sbData = data as StarbaseCmdData;
            var accessCntlr = sbData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Hero)) {
                Hero = sbData.Hero;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentConstruction)) {
                CurrentConstruction = sbData.CurrentConstruction;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = sbData.AlertStatus;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitDefense)) {
                UnitDefensiveStrength = sbData.UnitDefensiveStrength;
            }
            else {
                UnitDefensiveStrength = CalcPartialUnitDefensiveStrength(GetElementsData(sbData));
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitOffense)) {
                UnitOffensiveStrength = sbData.UnitOffensiveStrength;
            }
            else {
                UnitOffensiveStrength = CalcPartialUnitOffensiveStrength(GetElementsData(sbData));
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitMaxHitPts)) {
                UnitMaxHitPoints = sbData.UnitMaxHitPoints;
            }
            else {
                UnitMaxHitPoints = CalcPartialUnitMaxHitPoints(GetElementsData(sbData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitCurrentHitPts)) {
                UnitCurrentHitPoints = sbData.UnitCurrentHitPoints;
            }
            else {
                UnitCurrentHitPoints = CalcPartialUnitCurrentHitPoints(GetElementsData(sbData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitHealth)) {
                UnitHealth = sbData.UnitHealth;
            }
            else {
                // Calculate HitPts before attempting calculation of partial unit health
                UnitHealth = CalcPartialUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitScience)) {
                UnitScience = sbData.UnitScience;
            }
            else {
                UnitScience = CalcPartialUnitScience(GetElementsData(sbData));
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitCulture)) {
                UnitCulture = sbData.UnitCulture;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitNetIncome)) {
                UnitIncome = sbData.UnitIncome;
                UnitExpense = sbData.UnitExpense;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = sbData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitName)) {
                UnitName = sbData.UnitName;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = sbData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = sbData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentCmdEffectiveness)) {
                CurrentCmdEffectiveness = sbData.CurrentCmdEffectiveness;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitSensorRange)) {
                UnitSensorRange = sbData.UnitSensorRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitWeaponsRange)) {
                UnitWeaponsRange = sbData.UnitWeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = sbData.SectorID;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Formation)) {
                UnitFormation = sbData.UnitFormation;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                Capacity = sbData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitFood)) {
                UnitFood = sbData.UnitFood;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitProduction)) {
                UnitProduction = sbData.UnitProduction;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                Resources = sbData.Resources;
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Composition)) { // must precede Category
                UnitComposition = sbData.UnitComposition;
            }
            else {
                UnitComposition = CalcPartialUnitComposition(sbData);
            }

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = sbData.Category;
            }
            else {
                Category = CalcPartialCmdCategory(sbData);
            }
        }

        private BaseComposition CalcPartialUnitComposition(StarbaseCmdData cmdData) {
            var elementsData = GetElementsData(cmdData).Cast<FacilityData>();
            IList<FacilityHullCategory> knownElementCategories = new List<FacilityHullCategory>();
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                    knownElementCategories.Add(eData.HullCategory);
                }
            }
            if (knownElementCategories.Count > Constants.Zero) {
                // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                return new BaseComposition(knownElementCategories);
            }
            return null;
        }

        private StarbaseCategory CalcPartialCmdCategory(StarbaseCmdData cmdData) {
            if (UnitComposition != null) {
                return cmdData.GenerateCmdCategory(UnitComposition);
            }
            return StarbaseCategory.None;
        }

        #region Archive

        //private void AssignValuesFromElementReports(StarbaseCmdData cmdData) {
        //    var knownElementCategories = ElementReports.Select(r => r.Category).Where(cat => cat != default(FacilityHullCategory));
        //    if (knownElementCategories.Any()) { // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
        //        UnitComposition = new BaseComposition(knownElementCategories);
        //    }
        //    Category = UnitComposition != null ? cmdData.GenerateCmdCategory(UnitComposition) : StarbaseCategory.None;
        //    AssignValuesFrom(ElementReports);
        //}

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    var sData = data as StarbaseCmdData;
        //    Capacity = sData.Capacity;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    var sData = data as StarbaseCmdData;
        //    Resources = sData.Resources;
        //}

        #endregion

    }
}

