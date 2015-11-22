// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidData.cs
// Abstract base class for data associated with a Planet or Moon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for data associated with a Planet or Moon.
    /// </summary>
    public abstract class APlanetoidData : AMortalItemData {

        public PlanetoidCategory Category { get; private set; }

        public float OrbitalSpeed { get; set; }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private OpeResourceYield _resources;
        public OpeResourceYield Resources {
            get { return _resources; }
            set { SetProperty<OpeResourceYield>(ref _resources, value, "Resources"); }
        }

        private RareResourceYield _specialResources;
        public RareResourceYield SpecialResources {
            get { return _specialResources; }
            set { SetProperty<RareResourceYield>(ref _specialResources, value, "SpecialResources"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public APlanetoidData(APlanetoidStat stat)
            : base(stat.Category.GetValueName(), stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.OpeResources;
            SpecialResources = stat.RareResources;
            Topography = Topography.System;
        }

        protected override AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new ImprovingIntel();
            beginningIntel.CurrentCoverage = IntelCoverage.None;
            return beginningIntel;
        }

    }
}

