﻿// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for FacilityItems.
    /// </summary>
    public class FacilityReport : AUnitElementReport {

        public FacilityHullCategory Category { get; private set; }

        public FacilityReport(FacilityData data, Player player, IFacility_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var fData = data as FacilityData;
            var accessCntlr = fData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Culture)) {
                Culture = fData.Culture;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Defense)) {
                DefensiveStrength = fData.DefensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Offense)) {
                OffensiveStrength = fData.OffensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Health)) {
                Health = fData.Health;  // Element Health only matters on Element displays
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CurrentHitPoints)) {
                CurrentHitPoints = fData.CurrentHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Science)) {
                Science = fData.Science;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.NetIncome)) {
                Income = fData.Income;
                Expense = fData.Expense;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Name)) {
                Name = fData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.ParentName)) {
                ParentName = fData.ParentName;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Position)) {
                Position = fData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Owner)) {
                Owner = fData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Category)) {
                Category = fData.HullCategory;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.MaxHitPoints)) {
                MaxHitPoints = fData.MaxHitPoints;  // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Mass)) {
                Mass = fData.Mass;
            }
            // SensorRange on an element makes little sense
            //if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SensorRange)) {
            //    SensorRange = fData.SensorRange;
            //}
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.WeaponsRange)) {
                WeaponsRange = fData.WeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SectorIndex)) {
                SectorIndex = fData.SectorIndex;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    var fData = data as FacilityData;
        //    Culture = fData.Culture;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    var fData = data as FacilityData;
        //    DefensiveStrength = fData.DefensiveStrength;
        //    OffensiveStrength = fData.OffensiveStrength;
        //    Health = fData.Health;  // Element Health only matters on Element displays. UnitHealth calc'd using Element HitPts
        //    CurrentHitPoints = fData.CurrentHitPoints;
        //    Science = fData.Science;
        //    Income = fData.Income;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageEssential(data);
        //    var fData = data as FacilityData;
        //    ParentName = fData.ParentName;
        //    Position = fData.Position;
        //    Owner = fData.Owner;
        //    Category = fData.HullCategory;
        //    MaxHitPoints = fData.MaxHitPoints;  // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
        //    Mass = fData.Mass;
        //    SensorRange = fData.SensorRange;
        //    WeaponsRange = fData.WeaponsRange;
        //    Expense = fData.Expense;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBasic(data);
        //    var fData = data as FacilityData;
        //    Name = fData.Name;
        //}

        #endregion

    }
}

