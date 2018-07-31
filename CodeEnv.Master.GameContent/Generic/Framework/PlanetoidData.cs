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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data associated with a APlanetoidItem.
    /// </summary>
    public class PlanetoidData : AMortalItemData {

        private const string DebugNameFormat = "{0}_{1}";

        public PlanetoidCategory Category { get; private set; }

        public float Radius { get; private set; }

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

        private ResourcesYield _resources;
        public ResourcesYield Resources {
            get { return _resources; }
            set { SetProperty<ResourcesYield>(ref _resources, value, "Resources"); }
        }

        public IntVector3 SectorID { get; private set; }

        /// <summary>
        /// The mass of this Planetoid.
        /// <remarks>7.26.16 Primarily here for user HUDs as planetoids have kinematic
        /// rigidbodies which don't interact with the physics engine.</remarks>
        /// </summary>
        public float Mass { get; private set; }

        public new PlanetoidInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as PlanetoidInfoAccessController; } }

        private PlanetoidPublisher _publisher;
        public PlanetoidPublisher Publisher {
            get { return _publisher = _publisher ?? new PlanetoidPublisher(this); }
        }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

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
            : base(planetoid, owner, planetoidStat.HitPoints, passiveCMs) {
            Mass = planetoidStat.Mass;
            Category = planetoidStat.Category;
            Radius = planetoidStat.Radius;
            Capacity = planetoidStat.Capacity;
            Resources = planetoidStat.Resources;
            Topography = Topography.System;
            SectorID = InitializeSectorID();
        }

        private IntVector3 InitializeSectorID() {
            IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDContaining(Position);
            MarkAsChanged();
            return sectorID;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new PlanetoidInfoAccessController(this);
        }

        #endregion

        protected override AIntel MakeIntelInstance() {
            return new NonRegressibleIntel();
        }

        public PlanetoidReport GetReport(Player player) { return Publisher.GetReport(player); }

    }
}

