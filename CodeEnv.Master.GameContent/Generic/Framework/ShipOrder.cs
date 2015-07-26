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

        private static string _toStringFormat = "Directive: {0}, Source: {1}, Target: {2}, Speed: {3}, StandingOrder: {4}.";

        public ShipOrder StandingOrder { get; set; }

        public Speed Speed { get; private set; }

        public INavigableTarget Target { get; private set; }

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
        public ShipOrder(ShipDirective directive, OrderSource source = OrderSource.ElementCaptain, INavigableTarget target = null, Speed speed = Speed.None) {
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
            string targetText = Target != null ? Target.FullName : "null";
            string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "null";
            return _toStringFormat.Inject(Directive.GetValueName(), Source.GetValueName(), targetText, Speed.GetValueName(), standingOrderText);
        }

    }
}

