﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseOrder.cs
// Order for a Base - Settlement or Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order for a Base - Settlement or Starbase.
    /// </summary>
    public class BaseOrder {

        private const string ToStringFormat = "[{0}: Directive = {1}, Source = {2}, Target = {3}, FollowonOrder = {4}, StandingOrder = {5}]";

        public BaseOrder StandingOrder { get; set; }

        public BaseOrder FollowonOrder { get; set; }

        public IUnitAttackable Target { get; private set; }

        /// <summary>
        /// The source of this order.
        /// </summary>
        public OrderSource Source { get; private set; }

        public BaseDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public BaseOrder(BaseDirective directive, OrderSource source, IUnitAttackable target = null) {
            D.AssertNotEqual(OrderSource.Captain, source);
            Directive = directive;
            Source = source;
            Target = target;
        }

        public override string ToString() {
            string targetText = Target != null ? Target.DebugName : "none";
            string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
            string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "none";
            return ToStringFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, followonOrderText, standingOrderText);
        }


    }
}

