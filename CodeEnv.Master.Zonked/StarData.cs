﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarData.cs
// All the data associated with a particular star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular star.
    /// </summary>
    public class StarData : AItemData {

        public StarCategory Category { get; private set; }

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
        /// Initializes a new instance of the <see cref="StarItemData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public StarItemData(StarStat stat)
            : base(stat.Category.GetName()) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            base.Topography = Topography.System;
        }

        protected override AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new ImprovingIntel();
            beginningIntel.CurrentCoverage = IntelCoverage.Aware;
            return beginningIntel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

