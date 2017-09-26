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

        public float? Production { get; private set; }

        public FacilityReport(FacilityData data, Player player, IFacility_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var fData = data as FacilityData;
            var accessCntlr = fData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Culture)) {
                Culture = fData.Culture;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Defense)) {
                DefensiveStrength = fData.DefensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Offense)) {
                OffensiveStrength = fData.OffensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Health)) {
                Health = fData.Health;  // Element Health only matters on Element displays
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = fData.AlertStatus;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = fData.CurrentHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Science)) {
                Science = fData.Science;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.NetIncome)) {
                Income = fData.Income;
                Expense = fData.Expense;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = fData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitName)) {
                UnitName = fData.UnitName;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = fData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = fData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = fData.HullCategory;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Production)) {
                Production = fData.Production;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.ConstructionCost)) {
                ConstructionCost = fData.ConstructionCost;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = fData.MaxHitPoints;  // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Mass)) {
                Mass = fData.Mass;
            }
            // SensorRange on an element makes little sense
            //if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SensorRange)) {
            //    SensorRange = fData.SensorRange;
            //}
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.WeaponsRange)) {
                WeaponsRange = fData.WeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = fData.SectorID;
            }
        }

    }
}

