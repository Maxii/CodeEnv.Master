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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable report for APlanetoidItems.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        public PlanetoidCategory Category { get; private set; }

        public float? OrbitalSpeed { get; private set; }

        public int? Capacity { get; private set; }

        public ResourcesYield Resources { get; private set; }

        public float? Mass { get; private set; }

        public PlanetoidReport(PlanetoidData data, Player player) : base(data, player) { }

        protected override void AssignValues(AItemData data) {
            var pData = data as PlanetoidData;
            var accessCntlr = pData.InfoAccessCntlr;

            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.OrbitalSpeed)) {
                OrbitalSpeed = pData.OrbitalSpeed;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Defense)) {
                DefensiveStrength = pData.DefensiveStrength;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Capacity)) {
                Capacity = pData.Capacity;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Health)) {
                Health = pData.Health;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.CurrentHitPoints)) {
                CurrentHitPoints = pData.CurrentHitPoints;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Name)) {
                Name = pData.Name;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Position)) {
                Position = pData.Position;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Owner)) {
                Owner = pData.Owner;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Category)) {
                Category = pData.Category;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.MaxHitPoints)) {
                MaxHitPoints = pData.MaxHitPoints;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.Mass)) {
                Mass = pData.Mass;
            }
            if (accessCntlr.HasIntelCoverageReqdToAccess(Player, ItemInfoID.SectorID)) {
                SectorID = pData.SectorID;
            }

            Resources = AssessResources(pData.Resources);
        }


    }
}

