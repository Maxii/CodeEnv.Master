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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable report for APlanetoidItems.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        public string ParentName { get; private set; }

        public PlanetoidCategory Category { get; private set; }

        public float? OrbitalSpeed { get; private set; }

        public int? Capacity { get; private set; }

        public ResourceYield? Resources { get; private set; }

        public PlanetoidReport(APlanetoidData data, Player player, IPlanetoidItem item)
            : base(data, player, item) {
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var planetoidData = data as APlanetoidData;
            CurrentHitPoints = planetoidData.CurrentHitPoints;
            Health = planetoidData.Health;
        }

        protected override void AssignIncrementalValues_IntelCoverageBroad(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBroad(data);
            var planetoidData = data as APlanetoidData;
            MaxHitPoints = planetoidData.MaxHitPoints;
            DefensiveStrength = planetoidData.DefensiveStrength;
            Mass = planetoidData.Mass;
            Capacity = planetoidData.Capacity;
            Resources = planetoidData.Resources;
        }

        protected override void AssignIncrementalValues_IntelCoverageEssential(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageEssential(data);
            var planetoidData = data as APlanetoidData;
            Owner = planetoidData.Owner;
            Category = planetoidData.Category;
            OrbitalSpeed = planetoidData.OrbitalSpeed;
            Position = planetoidData.Position;
        }

        protected override void AssignIncrementalValues_IntelCoverageBasic(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageBasic(data);
            var planetoidData = data as APlanetoidData;
            Name = planetoidData.Name;
            ParentName = planetoidData.ParentName;
            SectorIndex = planetoidData.SectorIndex;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

