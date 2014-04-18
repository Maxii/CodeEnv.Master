// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityOrder.cs
// An order for a facility holding specific info required by the order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a facility holding specific info required by the order.
    /// </summary>
    public class FacilityOrder {

        public FacilityOrder StandingOrder { get; set; }

        public IMortalTarget Target { get; private set; }

        /// <summary>
        /// Flag indicating the source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public FacilityOrders Order { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityOrder" /> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target.</param>
        public FacilityOrder(FacilityOrders order, OrderSource source = OrderSource.ElementCaptain, IMortalTarget target = null) {
            Order = order;
            Source = source;
            Target = target;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

