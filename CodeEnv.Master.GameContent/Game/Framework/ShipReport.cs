// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipReport.cs
// Immutable report for ShipItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for ShipItems.
    /// </summary>
    public class ShipReport : AUnitElementReport {

        public INavigable Target { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public float? CurrentSpeed { get; private set; }

        public float? FullSpeed { get; private set; }

        public float? MaxTurnRate { get; private set; }

        public ShipHullCategory Category { get; private set; }

        public ShipReport(ShipData data, Player player, IShip_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as ShipData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Culture)) {
                Culture = sData.Culture;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Defense)) {
                DefensiveStrength = sData.DefensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Offense)) {
                OffensiveStrength = sData.OffensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Health)) {
                Health = sData.Health;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CurrentHitPoints)) {
                CurrentHitPoints = sData.CurrentHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Science)) {
                Science = sData.Science;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.NetIncome)) {
                Income = sData.Income;
                Expense = sData.Expense;
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
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Category)) {
                Category = sData.HullCategory;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.MaxHitPoints)) {
                MaxHitPoints = sData.MaxHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Mass)) {
                Mass = sData.Mass;
            }
            // SensorRange on an element makes little sense
            //if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SensorRange)) {
            //    SensorRange = sData.SensorRange;
            //}
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.WeaponsRange)) {
                WeaponsRange = sData.WeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.SectorIndex)) {
                SectorIndex = sData.SectorIndex;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.Target)) {
                Target = sData.Target;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CombatStance)) {
                CombatStance = sData.CombatStance;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.CurrentSpeed)) {
                CurrentSpeed = sData.ActualSpeedValue;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.FullSpeed)) {
                FullSpeed = sData.FullSpeedValue;
            }
            if (accessCntlr.HasAccessToInfo(Player, AccessControlInfoID.MaxTurnRate)) {
                MaxTurnRate = sData.MaxTurnRate;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageComprehensive(data);
        //    var sData = data as ShipData;
        //    CombatStance = sData.CombatStance;
        //    Target = sData.Target;
        //    Culture = sData.Culture;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBroad(data);
        //    var sData = data as ShipData;
        //    DefensiveStrength = sData.DefensiveStrength;
        //    OffensiveStrength = sData.OffensiveStrength;
        //    Health = sData.Health;  // Element Health only matters on Element displays. UnitHealth calc'd using Element HitPts
        //    CurrentHitPoints = sData.CurrentHitPoints;
        //    Science = sData.Science;
        //    Income = sData.Income;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageEssential(data);
        //    var sData = data as ShipData;
        //    ParentName = sData.ParentName;
        //    Position = sData.Position;
        //    Owner = sData.Owner;
        //    Category = sData.HullCategory;
        //    WeaponsRange = sData.WeaponsRange;
        //    SensorRange = sData.SensorRange;
        //    MaxHitPoints = sData.MaxHitPoints; // should always be with or before CurrentHitPts as both are needed to calc CmdReport's UnitHealth
        //    Mass = sData.Mass;
        //    MaxTurnRate = sData.MaxTurnRate;
        //    FullSpeed = sData.FullSpeedValue;
        //    Expense = sData.Expense;
        //}

        //protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
        //    base.AssignIncrementalValues_IntelCoverageBasic(data);
        //    var sData = data as ShipData;
        //    Name = sData.Name;
        //    CurrentSpeed = sData.ActualSpeedValue;
        //}

        #endregion

    }
}

