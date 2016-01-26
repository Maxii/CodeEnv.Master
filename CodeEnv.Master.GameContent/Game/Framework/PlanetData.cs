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

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="planetStat">The stat.</param>
        /// <param name="planetRigidbody">The planetoid rigidbody.</param>
        public PlanetData(Transform planetoidTransform, CameraFollowableStat cameraStat, PlanetStat planetStat, Rigidbody planetRigidbody)
            : this(planetoidTransform, TempGameValues.NoPlayer, cameraStat, Enumerable.Empty<PassiveCountermeasure>(), planetStat, planetRigidbody) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        /// <param name="planetRigidbody">The planetoid rigidbody.</param>
        public PlanetData(Transform planetoidTransform, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat, Rigidbody planetRigidbody)
            : this(planetoidTransform, TempGameValues.NoPlayer, cameraStat, passiveCMs, planetStat, planetRigidbody) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        /// <param name="planetStat">The stat.</param>
        /// <param name="planetRigidbody">The planetoid rigidbody.</param>
        public PlanetData(Transform planetoidTransform, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, PlanetStat planetStat, Rigidbody planetRigidbody)
            : base(planetoidTransform, owner, cameraStat, passiveCMs, planetStat, planetRigidbody) {
            LowOrbitRadius = planetStat.LowOrbitRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

