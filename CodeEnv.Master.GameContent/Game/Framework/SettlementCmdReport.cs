// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdReport.cs
// Immutable report for SettlementCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for SettlementCmdItems.
    /// </summary>
    public class SettlementCmdReport : AUnitCmdReport {

        public SettlementCategory Category { get; private set; }

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

        public int? Population { get; private set; }

        public float? Approval { get; private set; }

        public Construction CurrentConstruction { get; private set; }

        public int? Capacity { get; private set; }

        public ResourcesYield Resources { get; private set; }

        // 7.10.16 Eliminated usage of ElementReports to calculate partial Unit values.
        // 7.18.16 If Cmd's IntelCoverage does not allow full view of selected values
        // a partial value is calculated from element's data and their infoAccessCntlr. 

        public SettlementCmdReport(SettlementCmdData cmdData, Player player) : base(cmdData, player) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as SettlementCmdData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Hero)) {
                Hero = sData.Hero;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentConstruction)) {
                CurrentConstruction = sData.CurrentConstruction;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = sData.AlertStatus;
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitDefense)) {
                UnitDefensiveStrength = sData.UnitDefensiveStrength;
            }
            else {
                UnitDefensiveStrength = CalcUnitDefensiveStrengthFromKnownElements(GetElementsData(sData));
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitOffense)) {
                UnitOffensiveStrength = sData.UnitOffensiveStrength;
            }
            else {
                UnitOffensiveStrength = CalcUnitOffensiveStrengthFromKnownElements(GetElementsData(sData));
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitMaxHitPts)) {
                UnitMaxHitPoints = sData.UnitMaxHitPoints;
            }
            else {
                UnitMaxHitPoints = CalcUnitMaxHitPointsFromKnownElements(GetElementsData(sData));
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitCurrentHitPts)) {
                UnitCurrentHitPoints = sData.UnitCurrentHitPoints;
            }
            else {
                UnitCurrentHitPoints = CalcUnitCurrentHitPointsFromKnownElements(GetElementsData(sData));
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitHealth)) {
                UnitHealth = sData.UnitHealth;
            }
            else {
                // Calculate HitPts before attempting calc of partial unit health
                UnitHealth = CalcUnitHealthFromKnownElements(UnitCurrentHitPoints, UnitMaxHitPoints);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitName)) {
                UnitName = sData.UnitName;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentCmdEffectiveness)) {
                CurrentCmdEffectiveness = sData.CurrentCmdEffectiveness;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitSensorRange)) {
                UnitSensorRange = sData.UnitSensorRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.UnitWeaponsRange)) {
                UnitWeaponsRange = sData.UnitWeaponsRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Formation)) {
                Formation = sData.Formation;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Composition)) { // must precede Category
                UnitComposition = sData.UnitComposition;
            }
            else {
                UnitComposition = CalcUnitCompositionFromKnownElements(sData);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = sData.Category;
            }
            else {
                Category = CalcCmdCategoryFromKnownElements(sData);
            }

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Population)) {
                Population = sData.Population;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Approval)) {
                Approval = sData.Approval;
            }

            Resources = AssessResources(sData.Resources);
            UnitOutputs = AssessOutputs(sData.UnitOutputs);
        }

        private BaseComposition CalcUnitCompositionFromKnownElements(SettlementCmdData cmdData) {
            var elementsData = GetElementsData(cmdData).Cast<FacilityData>();
            IList<FacilityHullCategory> knownElementCategories = new List<FacilityHullCategory>();
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                    knownElementCategories.Add(eData.HullCategory);
                }
            }
            if (knownElementCategories.Count > Constants.Zero) {
                // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                return new BaseComposition(knownElementCategories);
            }
            return null;
        }

        private SettlementCategory CalcCmdCategoryFromKnownElements(SettlementCmdData cmdData) {
            if (UnitComposition != null) {
                return cmdData.GenerateCmdCategory(UnitComposition);
            }
            return SettlementCategory.None;
        }

        #region Archive

        //private void AssignValuesFromElementReports(SettlementCmdData cmdData) {
        //    var knownElementCategories = ElementReports.Select(r => r.Category).Where(cat => cat != default(FacilityHullCategory));
        //    if (knownElementCategories.Any()) { // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
        //        UnitComposition = new BaseComposition(knownElementCategories);
        //    }
        //    Category = UnitComposition != null ? cmdData.GenerateCmdCategory(UnitComposition) : SettlementCategory.None;
        //    AssignValuesFrom(ElementReports);
        //}

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    var sData = data as SettlementCmdData;
        //    Capacity = sData.Capacity;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    var sData = data as SettlementCmdData;
        //    Population = sData.Population;
        //    Resources = sData.Resources;
        //    Approval = sData.Approval;
        //}

        #endregion

    }
}

