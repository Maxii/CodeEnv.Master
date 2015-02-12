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

    /// <summary>
    /// Immutable report for APlanetoidItems.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        public string ParentName { get; protected set; }

        public PlanetoidCategory Category { get; private set; }

        public float? OrbitalSpeed { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }

        public PlanetoidReport(PlanetoidData data, Player player)
            : base(data, player) {
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageComprehensive(data);
            var planetoidData = data as PlanetoidData;
            CurrentHitPoints = planetoidData.CurrentHitPoints;
            Health = planetoidData.Health;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageModerate(data);
            var planetoidData = data as PlanetoidData;
            MaxHitPoints = planetoidData.MaxHitPoints;
            DefensiveStrength = planetoidData.DefensiveStrength;
            Mass = planetoidData.Mass;
            Capacity = planetoidData.Capacity;
            Resources = planetoidData.Resources;
            SpecialResources = planetoidData.SpecialResources;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageMinimal(data);
            var planetoidData = data as PlanetoidData;
            Owner = planetoidData.Owner;
            Category = planetoidData.Category;
            OrbitalSpeed = planetoidData.OrbitalSpeed;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            base.AssignIncrementalValues_IntelCoverageAware(data);
            var planetoidData = data as PlanetoidData;
            Name = planetoidData.Name;
            ParentName = planetoidData.ParentName;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

