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

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public APlanetoidData(PlanetoidStat stat)
            : base(stat.Category.GetName(), stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            base.Topography = Topography.System;
        }

    }
}

