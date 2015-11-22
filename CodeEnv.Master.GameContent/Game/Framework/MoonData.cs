// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonData.cs
// Data associated with a MoonItem.
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
    /// Data associated with a MoonItem.
    /// </summary>
    public class MoonData : APlanetoidData {

        public float ShipTransitBanRadius { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class
        /// with no countermeasures and no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public MoonData(Transform planetoidTransform, Rigidbody planetoidRigidbody, MoonStat planetoidStat, CameraFollowableStat cameraStat)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, cameraStat, Enumerable.Empty<PassiveCountermeasure>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="APlanetoidData" /> class with no owner.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public MoonData(Transform planetoidTransform, Rigidbody planetoidRigidbody, MoonStat planetoidStat, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : this(planetoidTransform, planetoidRigidbody, planetoidStat, TempGameValues.NoPlayer, cameraStat, passiveCMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="planetoidTransform">The planetoid transform.</param>
        /// <param name="planetoidRigidbody">The planetoid rigidbody.</param>
        /// <param name="planetoidStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive Countermeasures.</param>
        public MoonData(Transform planetoidTransform, Rigidbody planetoidRigidbody, MoonStat planetoidStat, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(planetoidTransform, planetoidRigidbody, planetoidStat, owner, cameraStat, passiveCMs) {
            ShipTransitBanRadius = planetoidStat.ShipTransitBanRadius;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

