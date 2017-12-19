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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An order for a fleet holding specific info required by the order.
    /// </summary>
    public class FleetOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Target = {3}, FollowonOrder = {4}]";

        private static readonly FleetDirective[] DirectivesWithNullTarget = new FleetDirective[]    {
                                                                                                        FleetDirective.Scuttle,
                                                                                                        FleetDirective.Cancel
                                                                                                    };

        private static readonly FleetDirective[] DirectivesWithNonNullTarget = new FleetDirective[] {
                                                                                                        FleetDirective.Attack,
                                                                                                        FleetDirective.Disband,
                                                                                                        FleetDirective.Explore,
                                                                                                        FleetDirective.FullSpeedMove,
                                                                                                        FleetDirective.Guard,
                                                                                                        FleetDirective.Join,
                                                                                                        FleetDirective.Move,
                                                                                                        FleetDirective.Patrol,
                                                                                                        FleetDirective.Refit,
                                                                                                        FleetDirective.Repair
                                                                                                    };

        public string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, followonOrderText);
            }
        }

        /// <summary>
        /// The Unique OrderID of this CmdOrder.
        /// <remarks>Can be used by Cmd to construct an Element order. If used, it indicates to the element that 1) the element order 
        /// originated from its Cmd, and 2) the Cmd expects an order outcome callback. 
        /// </remarks>
        /// <remarks>Also used by the Cmd to determine whether the order outcome callback it has just received should be passed onto 
        /// the executing state. If this OrderID is the same as the OrderID returned in the callback, then the callback is
        /// intended for the currently executing state and will be passed onto it. This accounts for the condition where the Cmd's
        /// CurrentOrder has just changed and concurrently it is receiving an order callback from one or more elements intended for
        /// the previous state.</remarks>
        /// </summary>
        public Guid OrderID { get; private set; }

        [Obsolete]
        public FleetOrder StandingOrder { get; set; }

        public FleetOrder FollowonOrder { get; set; }

        public IFleetNavigableDestination Target { get; private set; }

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
        public FleetOrder(FleetDirective directive, OrderSource source, IFleetNavigableDestination target = null) {
            Directive = directive;
            Source = source;
            Target = target;
            OrderID = Guid.NewGuid();
            __Validate();
        }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __Validate() {
            if (DirectivesWithNullTarget.Contains(Directive)) {
                D.AssertNull(Target);
            }
            if (DirectivesWithNonNullTarget.Contains(Directive)) {
                D.AssertNotNull(Target);
            }
            if (Directive == FleetDirective.Cancel) {
                D.AssertEqual(OrderSource.User, Source);
            }
            if (Directive == FleetDirective.ChangeHQ) {
                D.Error("{0}: {1} not implemented as an order.", DebugName, Directive.GetValueName());
            }
        }

        #endregion

    }
}

