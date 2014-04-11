// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitElementOrder.cs
// Order holding order-specific info for UnitElements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Order holding order-specific info for UnitElements.
    /// </summary>
    /// <typeparam name="T">The enum type of the order.</typeparam>
    public class UnitElementOrder<T> where T : struct {

        public float StandoffDistance { get; private set; }

        public Speed Speed { get; private set; }

        public IDestinationTarget Target { get; private set; }

        /// <summary>
        /// Flag indicating whether this order was issued by the element captain or by a superior.
        /// Superiors are UnitCommands and Players.
        /// </summary>
        public bool IsSuperiorOrder { get; private set; }

        public T Order { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitElementOrder{T}"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="isSuperiorOrder">Flag indicating whether this order was issued by the element captain or by a superior.
        /// Superiors are UnitCommands and Players.</param>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="standoffDistance">The standoff distance.</param>
        public UnitElementOrder(T order, bool isSuperiorOrder = false, IDestinationTarget target = null, Speed speed = Speed.None,
            float standoffDistance = Constants.ZeroF) {
            Order = order;
            IsSuperiorOrder = isSuperiorOrder;
            Target = target;
            Speed = speed;
            StandoffDistance = standoffDistance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitElementOrder{T}"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="isSuperiorOrder">Flag indicating whether this order was issued by the element captain or by a superior.
        /// Superiors are UnitCommands and Players.</param>
        /// <param name="targetLocation">The target location.</param>
        /// <param name="speed">The speed.</param>
        public UnitElementOrder(T order, bool isSuperiorOrder, Vector3 targetLocation, Speed speed)
            : this(order, isSuperiorOrder, new StationaryLocation(targetLocation), speed) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

