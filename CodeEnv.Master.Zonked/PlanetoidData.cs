// --------------------------------------------------------------------------------------------------------------------
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

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular Planetoid in a System.
    /// </summary>
    public class PlanetoidData : AMortalItemData {

        //private OrbitalSlot _systemOrbitSlot;
        ///// <summary>
        ///// The OrbitSlot that this planet occupies in the system.
        ///// </summary>
        //public OrbitalSlot SystemOrbitSlot {
        //    get { return _systemOrbitSlot; }
        //    set { SetProperty<OrbitalSlot>(ref _systemOrbitSlot, value, "SystemOrbitSlot", OnSystemOrbitSlotChanged); }
        //}

        public OrbitalSlot ShipOrbitSlot { get; set; }

        public PlanetoidCategory Category { get; private set; }

        public override Topography Topography {
            get { return base.Topography; }
            set { throw new NotImplementedException(); }
        }

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
        /// Initializes a new instance of the <see cref="PlanetoidData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public PlanetoidData(PlanetoidStat stat)
            : base(stat.Name, stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            base.Topography = Topography.System;
        }

        //private void OnSystemOrbitSlotChanged() {
        //    Transform.localPosition = SystemOrbitSlot.GenerateRandomLocalPositionWithinSlot();
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

