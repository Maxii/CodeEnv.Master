// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrder.cs
// An order for a ship that is not a Move.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship that is not a Move.
    /// </summary>
    public class ShipOrder {

        private const string ToStringFormat = "Directive: {0}, Source: {1}, Target: {2}, StandingOrder: {3}.";

        public ShipOrder StandingOrder { get; set; }

        public INavigableTarget Target { get; private set; }

        /// <summary>
        /// The source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public ShipDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public ShipOrder(ShipDirective directive, OrderSource source, INavigableTarget target = null) {
            if (directive == ShipDirective.Move) {
                D.Assert(GetType() == typeof(ShipMoveOrder));
            }
            Directive = directive;
            Source = source;
            Target = target;
        }

        public override string ToString() {
            string targetText = Target != null ? Target.FullName : "null";
            string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "null";
            return ToStringFormat.Inject(Directive.GetValueName(), Source.GetValueName(), targetText, standingOrderText);
        }

    }
}

