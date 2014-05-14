﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidData.cs
// All the data associated with a particular Planetoid in a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular Planetoid in a System.
    /// </summary>
    public class PlanetoidData : AMortalItemData {

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
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class.
        /// </summary>
        /// <param name="category">The category of planet.</param>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="parentName">Name of the parent.</param>
        //public PlanetoidData(PlanetoidCategory category, string name, float maxHitPoints, float mass, string parentName)
        //    : base(name, maxHitPoints, mass, parentName) {
        //    Category = category;
        //}

        public PlanetoidData(PlanetoidStat stat)
            : base(stat.Name, stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

