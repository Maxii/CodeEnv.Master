// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidData.cs
// Abstract Data associated with a APlanetoidItem.
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
    using UnityEngine;

    /// <summary>
    /// Abstract Data associated with a APlanetoidItem.
    /// </summary>
    public abstract class APlanetoidData : AMortalItemData {

        public PlanetoidCategory Category { get; private set; }

        public float Radius { get; private set; }

        public new CameraFollowableStat CameraStat { get { return base.CameraStat as CameraFollowableStat; } }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName"); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        /// <summary>
        /// The speed at which this planetoid is orbiting in Units/Hr.
        /// </summary>
        public float OrbitalSpeed { get; set; }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourceYield _resources;
        public ResourceYield Resources {
            get { return _resources; }
            set { SetProperty<ResourceYield>(ref _resources, value, "Resources"); }
        }

        public sealed override Topography Topography {  // avoids CA2214
            get { return base.Topography; }
            set { base.Topography = value; }
        }

        private Index3D _sectorIndex;
        public override Index3D SectorIndex { get { return _sectorIndex; } }

        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public APlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, APlanetoidStat planetoidStat, CameraFollowableStat cameraStat)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, TempGameValues.NoPlayer, cameraStat, Enumerable.Empty<PassiveCountermeasure>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public APlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, APlanetoidStat planetoidStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, TempGameValues.NoPlayer, cameraStat, passiveCMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public APlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, APlanetoidStat planetoidStat, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(planetoidTransform, planetoidStat.Category.GetValueName(), planetoidStat.MaxHitPoints, owner, cameraStat, passiveCMs) {
            Mass = planetoidStat.Mass;
            planetoidRigidbody.mass = planetoidStat.Mass;
            Category = planetoidStat.Category;
            Radius = planetoidStat.Radius;
            Capacity = planetoidStat.Capacity;
            Resources = planetoidStat.Resources;
            Topography = Topography.System;
            _sectorIndex = References.SectorGrid.GetSectorIndex(Position);
        }

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            return new ImprovingIntel(initialcoverage);
        }

    }
}

