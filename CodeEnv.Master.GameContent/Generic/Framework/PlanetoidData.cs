// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidData.cs
// Data associated with a APlanetoidItem.
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
    /// Data associated with a APlanetoidItem.
    /// </summary>
    public class PlanetoidData : AMortalItemData {

        private const string FullNameFormat = "{0}_{1}";

        public PlanetoidCategory Category { get; private set; }

        public float Radius { get; private set; }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName"); }
        }

        // Planetoid FullNames already include their System's name (Regulas6, Regulas6a)

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

        public override IntVector3 SectorID { get { return _sectorID; } }
        private IntVector3 _sectorID;

        /// <summary>
        /// The mass of this Planetoid.
        /// <remarks>7.26.16 Primarily here for user HUDs as planetoids have kinematic
        /// rigidbodies which don't interact with the physics engine.</remarks>
        /// </summary>
        public float Mass { get; private set; }

        public new PlanetoidInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as PlanetoidInfoAccessController; } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoid">The planetoid.</param>
        /// <param name="planetoidStat">The stat.</param>
        public PlanetoidData(IPlanetoid planetoid, PlanetoidStat planetoidStat)
            : this(planetoid, TempGameValues.NoPlayer, Enumerable.Empty<PassiveCountermeasure>(), planetoidStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoid">The planetoid.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetoidStat">The stat.</param>
        public PlanetoidData(IPlanetoid planetoid, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetoidStat planetoidStat)
            : this(planetoid, TempGameValues.NoPlayer, passiveCMs, planetoidStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class.
        /// </summary>
        /// <param name="planetoid">The planetoid.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetoidStat">The stat.</param>
        public PlanetoidData(IPlanetoid planetoid, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetoidStat planetoidStat)
            : base(planetoid, owner, planetoidStat.MaxHitPoints, passiveCMs) {
            Mass = planetoidStat.Mass;
            Category = planetoidStat.Category;
            Radius = planetoidStat.Radius;
            Capacity = planetoidStat.Capacity;
            Resources = planetoidStat.Resources;
            Topography = Topography.System;
            _sectorID = InitializeSectorID();
        }

        private IntVector3 InitializeSectorID() {
            IntVector3 sectorID = References.SectorGrid.GetSectorIdThatContains(Position);
            D.AssertNotDefault(sectorID);
            MarkAsChanged();
            return sectorID;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new PlanetoidInfoAccessController(this);
        }

        #endregion

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        #region Event and Property Change Handlers

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

