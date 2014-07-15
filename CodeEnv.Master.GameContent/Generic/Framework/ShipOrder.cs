// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitElementOrder.cs
// An order for a ship holding specific info required by the order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An order for a ship holding specific info required by the order.
    /// </summary>
    public class ShipOrder {

        public ShipOrder StandingOrder { get; set; }

        public Speed Speed { get; private set; }

        public IDestinationTarget Target { get; private set; }

        /// <summary>
        /// Flag indicating the source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public ShipDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrder" /> class.
        /// </summary>
        /// <param name="directive">The order.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public ShipOrder(ShipDirective directive, OrderSource source = OrderSource.ElementCaptain, IDestinationTarget target = null, Speed speed = Speed.None) {
            Directive = directive;
            Source = source;
            Target = target;
            Speed = speed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrder" /> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="destination">The destination location.</param>
        /// <param name="speed">The speed.</param>
        public ShipOrder(ShipDirective order, OrderSource source, Vector3 destination, Speed speed)
            : this(order, source, new StationaryLocation(destination), speed) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

