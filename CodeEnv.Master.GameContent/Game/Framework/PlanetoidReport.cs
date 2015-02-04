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

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable report on a planetoid reflecting a specific player's IntelCoverage.
    /// </summary>
    public class PlanetoidReport : AMortalItemReport {

        public PlanetoidCategory Category { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }

        public PlanetoidReport(APlanetoidData data, Player player)
            : base(data, player) {
        }

        protected override void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) {
            var planetoidData = data as APlanetoidData;
            CurrentHitPoints = planetoidData.CurrentHitPoints;
            Health = planetoidData.Health;
        }

        protected override void AssignIncrementalValues_IntelCoverageModerate(AItemData data) {
            var planetoidData = data as APlanetoidData;
            MaxHitPoints = planetoidData.MaxHitPoints;
            DefensiveStrength = planetoidData.DefensiveStrength;
            Mass = planetoidData.Mass;
            Capacity = planetoidData.Capacity;
            Resources = planetoidData.Resources;
            SpecialResources = planetoidData.SpecialResources;
        }

        protected override void AssignIncrementalValues_IntelCoverageMinimal(AItemData data) {
            var planetoidData = data as APlanetoidData;
            Owner = planetoidData.Owner;
            Category = planetoidData.Category;
        }

        protected override void AssignIncrementalValues_IntelCoverageAware(AItemData data) {
            var planetoidData = data as APlanetoidData;
            Name = planetoidData.Name;
            ParentName = planetoidData.ParentName;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

