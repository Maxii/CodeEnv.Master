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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, CameraFollowableStat cameraStat, PlanetStat planetStat)
            : this(planet, TempGameValues.NoPlayer, cameraStat, Enumerable.Empty<PassiveCountermeasure>(), planetStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class with no owner.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat)
            : this(planet, TempGameValues.NoPlayer, cameraStat, passiveCMs, planetStat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetData" /> class.
        /// </summary>
        /// <param name="planet">The planet.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        public PlanetData(IPlanet planet, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat)
            : base(planet, owner, cameraStat, passiveCMs, planetStat) {
            CloseOrbitInnerRadius = planetStat.CloseOrbitInnerRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

