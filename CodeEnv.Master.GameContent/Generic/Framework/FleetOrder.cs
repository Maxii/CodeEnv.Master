// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetOrder.cs
//  An order for a fleet holding specific info required by the order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An order for a fleet holding specific info required by the order.
    /// </summary>
    public class FleetOrder {

        private const string ToStringFormat = "Directive: {0}, Source: {1}, Target: {2}";

        public INavigableTarget Target { get; private set; }

        /// <summary>
        /// The source of this order.
        /// </summary>
        public OrderSource Source { get; private set; }

        public FleetDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public FleetOrder(FleetDirective directive, OrderSource source, INavigableTarget target = null) {
            D.Assert(target == null || (!(target is IFleetFormationStation) && !(target is IUnitElementItem)));
            D.Assert(source != OrderSource.Captain);
            Directive = directive;
            Source = source;
            Target = target;
        }

        public override string ToString() {
            string targetText = Target != null ? Target.FullName : "null";
            return ToStringFormat.Inject(Directive.GetValueName(), Source.GetValueName(), targetText);
        }

    }
}

