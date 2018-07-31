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

        public ShipDirective __OrderDirective { get; private set; }

        public INavigableDestination Target { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public Speed CurrentSpeedSetting { get; private set; }

        public IntVector3 SectorID { get; private set; }

        public float? FullSpeed { get; private set; }

        public float? TurnRate { get; private set; }

        public ShipHullCategory Category { get; private set; }

        public ShipReport(ShipData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var sData = data as ShipData;
            var accessCntlr = sData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Defense)) {
                DefensiveStrength = sData.DefensiveStrength;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Offense)) {
                OffensiveStrength = sData.OffensiveStrength;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Health)) {
                Health = sData.Health;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.AlertStatus)) {
                AlertStatus = sData.AlertStatus;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = sData.CurrentHitPoints;
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
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = sData.HullCategory;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = sData.MaxHitPoints;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Mass)) {
                Mass = sData.Mass;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.ConstructionCost)) {
                ConstructionCost = sData.Design.ConstructionCost;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.WeaponsRange)) {
                WeaponsRange = sData.WeaponsRange;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                IntVector3 sectorID;
                sData.TryGetSectorID(out sectorID);
                SectorID = sectorID;    // 7.15.18 DisplayInfoFactory will handle if default(IntVector3)
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Target)) {
                Target = sData.Target;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CombatStance)) {
                CombatStance = sData.CombatStance;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentSpeedSetting)) {
                CurrentSpeedSetting = sData.CurrentSpeedSetting;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.FullSpeed)) {
                FullSpeed = sData.FullSpeedValue;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.TurnRate)) {
                TurnRate = sData.TurnRate;
            }

            __ActualSpeedValue = (Item as IShip_Ltd).ActualSpeedValue_Debug;
            __OrderDirective = (Item as IShip).CurrentOrder != null ? (Item as IShip).CurrentOrder.Directive : ShipDirective.None;

            Outputs = AssessOutputs(sData.Outputs);
        }


    }
}

