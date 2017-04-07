﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An order for a fleet holding specific info required by the order.
    /// </summary>
    public class FleetOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Target = {3}, FollowonOrder = {4}, StandingOrder = {5}]";

        public string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, followonOrderText, standingOrderText);
            }
        }

        public FleetOrder StandingOrder { get; set; }

        public FleetOrder FollowonOrder { get; set; }

        public IFleetNavigable Target { get; private set; }

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
        public FleetOrder(FleetDirective directive, OrderSource source, IFleetNavigable target = null) {
            Directive = directive;
            Source = source;
            Target = target;
        }

        public override string ToString() {
            return DebugName;
        }

    }
}

