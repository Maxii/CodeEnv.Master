// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidItemData.cs
// Class for Data associated with a PlanetoidItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a PlanetoidItem.
    /// </summary>
    public class PlanetoidItemData : AMortalItemData {

        public PlanetoidCategory Category { get; private set; }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName"); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        public float OrbitalSpeed { get; set; }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private OpeYield _resources;
        public OpeYield Resources {
            get { return _resources; }
            set { SetProperty<OpeYield>(ref _resources, value, "Resources"); }
        }

        private XYield _specialResources;
        public XYield SpecialResources {
            get { return _specialResources; }
            set { SetProperty<XYield>(ref _specialResources, value, "SpecialResources"); }
        }

        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="stat">The stat.</param>
        public PlanetoidItemData(Transform planetoidTransform, PlanetoidStat stat)
            : base(planetoidTransform, stat.Category.GetName(), stat.MaxHitPoints) {
            Mass = stat.Mass;
            planetoidTransform.rigidbody.mass = stat.Mass;
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            Topography = Topography.System;
        }

        protected override AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new ImprovingIntel();
            beginningIntel.CurrentCoverage = Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.None;
            return beginningIntel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

