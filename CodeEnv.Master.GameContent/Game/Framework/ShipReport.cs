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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report for ShipItems.
    /// </summary>
    public class ShipReport : AUnitElementReport {

        public Reference<float> __ActualSpeedValue { get; private set; }

        public INavigableDestination Target { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public Speed CurrentSpeedSetting { get; private set; }

        public float? FullSpeed { get; private set; }

        public float? MaxTurnRate { get; private set; }

        public ShipHullCategory Category { get; private set; }

        public ShipReport(ShipData data, Player player, IShip_Ltd item) : base(data, player, item) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as ShipData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Culture)) {
                Culture = sData.Culture;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Defense)) {
                DefensiveStrength = sData.DefensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Offense)) {
                OffensiveStrength = sData.OffensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Health)) {
                Health = sData.Health;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = sData.AlertStatus;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = sData.CurrentHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Science)) {
                Science = sData.Science;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.NetIncome)) {
                Income = sData.Income;
                Expense = sData.Expense;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = sData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.UnitName)) {
                UnitName = sData.UnitName;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = sData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = sData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = sData.HullCategory;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = sData.MaxHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Mass)) {
                Mass = sData.Mass;
            }
            // SensorRange on an element makes little sense
            ////if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SensorRange)) {
            ////    SensorRange = sData.SensorRange;
            ////}
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.WeaponsRange)) {
                WeaponsRange = sData.WeaponsRange;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = sData.SectorID;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Target)) {
                Target = sData.Target;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CombatStance)) {
                CombatStance = sData.CombatStance;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentSpeedSetting)) {
                CurrentSpeedSetting = sData.CurrentSpeedSetting;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.FullSpeed)) {
                FullSpeed = sData.FullSpeedValue;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.MaxTurnRate)) {
                MaxTurnRate = sData.MaxTurnRate;
            }

            __ActualSpeedValue = (Item as IShip_Ltd).ActualSpeedValue_Debug;
        }


    }
}

