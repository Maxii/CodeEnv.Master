// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidDataRecord.cs
// Immutable report on a planetoid reflecting a specific player's IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable report on a planetoid reflecting a specific player's IntelCoverage.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        public string ParentName { get; protected set; }

        public PlanetoidCategory Category { get; private set; }

        public float? OrbitalSpeed { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }

        public PlanetoidReport(PlanetoidItemData data, Player player)
            : base(data, player) {
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AIntelItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var planetoidData = data as PlanetoidItemData;
            CurrentHitPoints = planetoidData.CurrentHitPoints;
            Health = planetoidData.Health;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AIntelItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            var planetoidData = data as PlanetoidItemData;
            MaxHitPoints = planetoidData.MaxHitPoints;
            DefensiveStrength = planetoidData.DefensiveStrength;
            Mass = planetoidData.Mass;
            Capacity = planetoidData.Capacity;
            Resources = planetoidData.Resources;
            SpecialResources = planetoidData.SpecialResources;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AIntelItemData data) {
            base.AssignIncrementalValues_IntelCoverageMinimal(data);
            var planetoidData = data as PlanetoidItemData;
            Owner = planetoidData.Owner;
            Category = planetoidData.Category;
            OrbitalSpeed = planetoidData.OrbitalSpeed;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AIntelItemData data) {
            base.AssignIncrementalValues_IntelCoverageAware(data);
            var planetoidData = data as PlanetoidItemData;
            Name = planetoidData.Name;
            ParentName = planetoidData.ParentName;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

