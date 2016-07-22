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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public float? Approval { get; private set; }

        // 7.10.16 Eliminated usage of ElementReports to calculate partial Unit values.
        // 7.18.16 If Cmd's IntelCoverage does not allow full view of selected values
        // a partial value is calculated from element's data and their infoAccessCntlr. 

        public SettlementCmdReport(SettlementCmdData cmdData, Player player, ISettlementCmd_Ltd item)
            : base(cmdData, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var sData = data as SettlementCmdData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitDefense)) {
                UnitDefensiveStrength = sData.UnitDefensiveStrength;
            }
            else {
                UnitDefensiveStrength = CalcPartialUnitDefensiveStrength(GetElementsData(sData));
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitOffense)) {
                UnitOffensiveStrength = sData.UnitOffensiveStrength;
            }
            else {
                UnitOffensiveStrength = CalcPartialUnitOffensiveStrength(GetElementsData(sData));
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitMaxHitPts)) {
                UnitMaxHitPoints = sData.UnitMaxHitPoints;
            }
            else {
                UnitMaxHitPoints = CalcPartialUnitMaxHitPoints(GetElementsData(sData));
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitCurrentHitPts)) {
                UnitCurrentHitPoints = sData.UnitCurrentHitPoints;
            }
            else {
                UnitCurrentHitPoints = CalcPartialUnitCurrentHitPoints(GetElementsData(sData));
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitHealth)) {
                UnitHealth = sData.UnitHealth;
            }
            else {
                // Calculate HitPts before attempting calc of partial unit health
                UnitHealth = CalcPartialUnitHealth(UnitCurrentHitPoints, UnitMaxHitPoints);
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitScience)) {
                UnitScience = sData.UnitScience;
            }
            else {
                UnitScience = CalcPartialUnitScience(GetElementsData(sData));
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitCulture)) {
                UnitCulture = sData.UnitCulture;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitNetIncome)) {
                UnitIncome = sData.UnitIncome;
                UnitExpense = sData.UnitExpense;
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.ParentName)) {
                ParentName = sData.ParentName;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CurrentCmdEffectiveness)) {
                CurrentCmdEffectiveness = sData.CurrentCmdEffectiveness;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitSensorRange)) {
                UnitSensorRange = sData.UnitSensorRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.UnitWeaponsRange)) {
                UnitWeaponsRange = sData.UnitWeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SectorIndex)) {
                SectorIndex = sData.SectorIndex;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Formation)) {
                UnitFormation = sData.UnitFormation;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Capacity)) {
                Capacity = sData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Resources)) {
                Resources = sData.Resources;
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Composition)) { // must preceed Category
                UnitComposition = sData.UnitComposition;
            }
            else {
                UnitComposition = CalcPartialUnitComposition(sData);
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Category)) {
                Category = sData.Category;
            }
            else {
                Category = CalcPartialCmdCategory(sData);
            }

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Population)) {
                Population = sData.Population;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Approval)) {
                Approval = sData.Approval;
            }
        }

        private BaseComposition CalcPartialUnitComposition(SettlementCmdData cmdData) {
            var elementsData = GetElementsData(cmdData).Cast<FacilityData>();
            IList<FacilityHullCategory> knownElementCategories = new List<FacilityHullCategory>();
            foreach (var eData in elementsData) {
                var accessCntlr = eData.InfoAccessCntlr;
                if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Category)) {
                    knownElementCategories.Add(eData.HullCategory);
                }
            }
            if (knownElementCategories.Count > Constants.Zero) {
                // Player will always know about the HQElement (since knows Cmd) but Category may not yet be revealed
                return new BaseComposition(knownElementCategories);
            }
            return null;
        }

        private SettlementCategory CalcPartialCmdCategory(SettlementCmdData cmdData) {
            if (UnitComposition != null) {
                return cmdData.GenerateCmdCategory(UnitComposition);
            }
            return SettlementCategory.None;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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

