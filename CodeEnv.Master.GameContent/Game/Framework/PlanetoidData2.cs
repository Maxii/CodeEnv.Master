// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidData2.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class PlanetoidData2 : AMortalItemData2 {

        public PlanetoidCategory Category { get; private set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public PlanetoidData2(PlanetoidStat stat)
            : base(stat.Category.GetName(), stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            Topography = Topography.System;
        }

        protected override AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new ImprovingIntel();
            beginningIntel.CurrentCoverage = IntelCoverage.None;
            return beginningIntel;
        }

    }
}

