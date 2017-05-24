// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetData.cs
// Data associated with a PlanetItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Data associated with a PlanetItem.
    /// </summary>
    public class PlanetData : PlanetoidData {

        public float CloseOrbitInnerRadius { get; private set; }

        public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, PlanetStat planetStat)
            : this(planet, TempGameValues.NoPlayer, Enumerable.Empty<PassiveCountermeasure>(), planetStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class with no owner.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat)
            : this(planet, TempGameValues.NoPlayer, passiveCMs, planetStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat)
            : base(planet, owner, passiveCMs, planetStat) {
            CloseOrbitInnerRadius = planetStat.CloseOrbitInnerRadius;
        }

        #endregion


    }
}

