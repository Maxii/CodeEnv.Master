// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidDataRecord.cs
// Immutable report for APlanetoidItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable report for APlanetoidItems.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        ////public string ParentName { get; private set; }

        public PlanetoidCategory Category { get; private set; }

        public float? OrbitalSpeed { get; private set; }

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public float? Mass { get; private set; }

        public PlanetoidReport(PlanetoidData data, Player player, IPlanetoid_Ltd item)
            : base(data, player, item) {
        }

        protected override void AssignValues(AItemData data) {
            var pData = data as PlanetoidData;
            var accessCntlr = pData.InfoAccessCntlr;

            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.OrbitalSpeed)) {
                OrbitalSpeed = pData.OrbitalSpeed;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Defense)) {
                DefensiveStrength = pData.DefensiveStrength;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Capacity)) {
                Capacity = pData.Capacity;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Health)) {
                Health = pData.Health;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = pData.CurrentHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Resources)) {
                Resources = pData.Resources;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Name)) {
                Name = pData.Name;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Position)) {
                Position = pData.Position;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Owner)) {
                Owner = pData.Owner;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Category)) {
                Category = pData.Category;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = pData.MaxHitPoints;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.Mass)) {
                Mass = pData.Mass;
            }
            if (accessCntlr.HasAccessToInfo(Player, ItemInfoID.SectorID)) {
                SectorID = pData.SectorID;
            }
        }

    }
}

