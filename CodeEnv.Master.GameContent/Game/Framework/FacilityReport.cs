// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityReport.cs
// Immutable report for FacilityItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for FacilityItems.
    /// </summary>
    public class FacilityReport : AUnitElementReport {

        public FacilityHullCategory Category { get; private set; }

        public FacilityReport(FacilityData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var fData = data as FacilityData;
            var accessCntlr = fData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Defense)) {
                DefensiveStrength = fData.DefensiveStrength;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Offense)) {
                OffensiveStrength = fData.OffensiveStrength;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Health)) {
                Health = fData.Health;  // Element Health only matters on Element displays
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = fData.AlertStatus;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = fData.CurrentHitPoints;
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
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = fData.HullCategory;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.ConstructionCost)) {
                ConstructionCost = fData.Design.ConstructionCost;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = fData.MaxHitPoints;  // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Mass)) {
                Mass = fData.Mass;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.WeaponsRange)) {
                WeaponsRange = fData.WeaponsRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = fData.SectorID;
            }

            Outputs = AssessOutputs(fData.Outputs);
        }

    }
}

