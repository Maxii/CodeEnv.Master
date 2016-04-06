// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOrbitSlot.cs
// Abstract base class for Celestial and Ship orbit slots that know how to place
// (and remove if applicable) objects into orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Celestial and Ship orbit slots that know how to place
    /// (and remove if applicable) objects into orbit.
    /// </summary>
    [System.Obsolete]
    public abstract class AOrbitSlot {

        /// <summary>
        /// Indicates whether the OrbitSimulator created by this OrbitSlot should rotate
        /// when activated.
        /// </summary>
        public bool ToOrbit { get; private set; }

        /// <summary>
        /// The slot's closest distance from the body orbited.
        /// </summary>
        public float InnerRadius { get; private set; }

        /// <summary>
        /// The slot's furthest distance from the body orbited.
        /// </summary>
        public float OuterRadius { get; private set; }

        /// <summary>
        /// The slot's mean distance from the body orbited.
        /// </summary>
        public float MeanRadius { get; private set; }

        /// <summary>
        /// The slot's depth, aka OutsideRadius - InsideRadius.
        /// </summary>
        public float Depth { get; private set; }

        public bool IsOrbitedObjectMobile { get; private set; }

        public GameTimeDuration OrbitPeriod { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AOrbitSlot" /> struct.
        /// </summary>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedObjectMobile">if set to <c>true</c> [is orbited object mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="toOrbit">if set to <c>true</c> the orbitSimulator will rotate if activated.</param>
        public AOrbitSlot(float innerRadius, float outerRadius, bool isOrbitedObjectMobile, GameTimeDuration orbitPeriod, bool toOrbit) {
            Utility.Validate(innerRadius != outerRadius);
            Utility.ValidateForRange(innerRadius, Constants.ZeroF, outerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity);
            Utility.Validate(orbitPeriod != default(GameTimeDuration));
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            MeanRadius = innerRadius + (outerRadius - innerRadius) / 2F;
            Depth = outerRadius - innerRadius;
            IsOrbitedObjectMobile = isOrbitedObjectMobile;
            OrbitPeriod = orbitPeriod;
            ToOrbit = toOrbit;
        }

    }
}

