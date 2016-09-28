// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitData.cs
// Data used by an OrbitSimulator to implement an orbit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Data used by an OrbitSimulator to implement an orbit.
    /// </summary>
    public class OrbitData {

        public GameObject OrbitedItem { get; set; }
        //public GameObject OrbitedItem { get; private set; }

        /// <summary>
        /// Indicates whether the OrbitSimulator created by this OrbitData should rotate
        /// when activated.
        /// </summary>
        public bool ToOrbit { get; private set; }

        /// <summary>
        /// The closest distance from the orbited body available for orbiting.
        /// </summary>
        public float InnerRadius { get; private set; }

        /// <summary>
        /// The furthest distance from the orbited body available for orbiting.
        /// </summary>
        public float OuterRadius { get; private set; }

        /// <summary>
        /// The mean distance from the orbited body available for orbiting.
        /// </summary>
        public float MeanRadius { get; private set; }

        /// <summary>
        /// The slot's depth, aka OuterRadius - InnerRadius.
        /// </summary>
        public float Depth { get; private set; }

        public int SlotIndex { get; private set; }

        public bool IsOrbitedItemMobile { get; private set; }

        public GameTimeDuration OrbitPeriod { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class without a
        /// designated OrbitedItem. The OrbitPeriod defaults to OneYear.
        /// </summary>
        /// <param name="slotIndex">Index of the slot.</param>
        /// <param name="innerRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="outerRadius">The radius at this slot's highest orbit.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited item mobile].</param>
        public OrbitData(int slotIndex, float innerRadius, float outerRadius, bool isOrbitedItemMobile)
                        : this(slotIndex, innerRadius, outerRadius, isOrbitedItemMobile, GameTimeDuration.OneYear) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class without a
        /// designated OrbitedItem.
        /// </summary>
        /// <param name="slotIndex">Index of the slot.</param>
        /// <param name="innerRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="outerRadius">The radius at this slot's highest orbit.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited item mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public OrbitData(int slotIndex, float innerRadius, float outerRadius, bool isOrbitedItemMobile, GameTimeDuration orbitPeriod)
            : this(slotIndex, innerRadius, outerRadius, isOrbitedItemMobile, orbitPeriod, toOrbit: true) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class without a
        /// designated OrbitedItem.
        /// </summary>
        /// <param name="slotIndex">Index of the slot.</param>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited object mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="toOrbit">if set to <c>true</c> the orbitSimulator will rotate if activated.</param>
        public OrbitData(int slotIndex, float innerRadius, float outerRadius, bool isOrbitedItemMobile, GameTimeDuration orbitPeriod, bool toOrbit) {
            Utility.Validate(innerRadius != outerRadius);
            Utility.ValidateForRange(innerRadius, Constants.ZeroF, outerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity);
            Utility.Validate(orbitPeriod != default(GameTimeDuration));
            SlotIndex = slotIndex;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            MeanRadius = innerRadius + (outerRadius - innerRadius) / 2F;
            Depth = outerRadius - innerRadius;
            IsOrbitedItemMobile = isOrbitedItemMobile;
            OrbitPeriod = orbitPeriod;
            ToOrbit = toOrbit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class.
        /// The OrbitPeriod defaults to OneYear.
        /// <remarks>Used to create a single ShipCloseOrbitSlot during runtime around <c>orbitedItem</c>.
        /// The slotIndex is by definition 0.</remarks>
        /// </summary>
        /// <param name="orbitedItem">The orbited item.</param>
        /// <param name="innerRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="outerRadius">The radius at this slot's highest orbit.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited item mobile].</param>
        public OrbitData(GameObject orbitedItem, float innerRadius, float outerRadius, bool isOrbitedItemMobile)
            : this(orbitedItem, innerRadius, outerRadius, isOrbitedItemMobile, GameTimeDuration.OneYear) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class that moves in orbit.
        /// <remarks>Used to create a single ShipCloseOrbitSlot during runtime around <c>orbitedItem</c>.
        /// The slotIndex is by definition 0.</remarks>
        /// </summary>
        /// <param name="orbitedItem">The orbited item.</param>
        /// <param name="innerRadius">The radius at this slot's lowest orbit.</param>
        /// <param name="outerRadius">The radius at this slot's highest orbit.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited item mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        public OrbitData(GameObject orbitedItem, float innerRadius, float outerRadius, bool isOrbitedItemMobile, GameTimeDuration orbitPeriod)
            : this(orbitedItem, innerRadius, outerRadius, isOrbitedItemMobile, orbitPeriod, toOrbit: true) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitData" /> class.
        /// <remarks>Used to create a single ShipCloseOrbitSlot during runtime around <c>orbitedItem</c>.
        /// The slotIndex is by definition 0.</remarks>
        /// </summary>
        /// <param name="orbitedItem">The orbited item.</param>
        /// <param name="innerRadius">The closest distance to the body orbited.</param>
        /// <param name="outerRadius">The furthest distance from the body orbited.</param>
        /// <param name="isOrbitedItemMobile">if set to <c>true</c> [is orbited object mobile].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="toOrbit">if set to <c>true</c> the orbitSimulator will rotate if activated.</param>
        public OrbitData(GameObject orbitedItem, float innerRadius, float outerRadius, bool isOrbitedItemMobile, GameTimeDuration orbitPeriod, bool toOrbit) {
            Utility.ValidateNotNull(orbitedItem);
            Utility.Validate(innerRadius != outerRadius);
            Utility.ValidateForRange(innerRadius, Constants.ZeroF, outerRadius);
            Utility.ValidateForRange(outerRadius, innerRadius, Mathf.Infinity);
            Utility.Validate(orbitPeriod != default(GameTimeDuration));
            OrbitedItem = orbitedItem;
            SlotIndex = Constants.Zero;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            MeanRadius = innerRadius + (outerRadius - innerRadius) / 2F;
            Depth = outerRadius - innerRadius;
            IsOrbitedItemMobile = isOrbitedItemMobile;
            OrbitPeriod = orbitPeriod;
            ToOrbit = toOrbit;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

