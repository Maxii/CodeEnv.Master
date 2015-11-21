﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidData.cs
// Class for Data associated with a PlanetoidItem.
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
    /// Class for Data associated with a PlanetoidItem.
    /// </summary>
    public class PlanetoidData : AMortalItemData {

        public PlanetoidCategory Category { get; private set; }

        public float Radius { get; private set; }

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

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

        private Index3D _sectorIndex;
        public override Index3D SectorIndex { get { return _sectorIndex; } }

        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public PlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetoidStat planetoidStat, CameraFollowableStat cameraStat)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, cameraStat, TempGameValues.NoPlayer, Enumerable.Empty<PassiveCountermeasure>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public PlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetoidStat planetoidStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, cameraStat, TempGameValues.NoPlayer, passiveCMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public PlanetoidData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetoidStat planetoidStat, CameraFollowableStat cameraStat, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(planetoidTransform, planetoidStat.Category.GetValueName(), planetoidStat.MaxHitPoints, owner, cameraStat, passiveCMs) {
            Mass = planetoidStat.Mass;
            planetoidRigidbody.mass = planetoidStat.Mass;
            Category = planetoidStat.Category;
            Radius = planetoidStat.Radius;
            LowOrbitRadius = planetoidStat.LowOrbitRadius;
            Capacity = planetoidStat.Capacity;
            Resources = planetoidStat.Resources;
            Topography = Topography.System;
            _sectorIndex = References.SectorGrid.GetSectorIndex(Position);
        }

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            return new ImprovingIntel(initialcoverage);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

