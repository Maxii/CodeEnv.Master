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
    public class PlanetData : APlanetoidData {

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public PlanetData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetStat planetoidStat, CameraFollowableStat cameraStat)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, TempGameValues.NoPlayer, cameraStat, Enumerable.Empty<PassiveCountermeasure>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public PlanetData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetStat planetoidStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
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
        public PlanetData(Transform planetoidTransform, Rigidbody planetoidRigidbody, PlanetStat planetoidStat, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(planetoidTransform, planetoidRigidbody, planetoidStat, owner, cameraStat, passiveCMs) {
            LowOrbitRadius = planetoidStat.LowOrbitRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

