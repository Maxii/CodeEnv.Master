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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a facility holding specific info required by the order.
    /// </summary>
    public class FacilityOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Target = {3}, FollowonOrder = {4}]";

        private static readonly FacilityDirective[] DirectivesWithNullTarget = new FacilityDirective[]  {
                                                                                                            FacilityDirective.Cancel,
                                                                                                            FacilityDirective.Construct,
                                                                                                            FacilityDirective.Scuttle
                                                                                                        };

        private static readonly FacilityDirective[] DirectivesWithNonNullTarget = new FacilityDirective[]   {
                                                                                                                FacilityDirective.Attack,
                                                                                                                FacilityDirective.Disband,
                                                                                                                FacilityDirective.Refit,
                                                                                                                FacilityDirective.Repair
                                                                                                            };
        public virtual string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, followonOrderText);
            }
        }

        /// <summary>
        /// The Unique OrderID of the CmdOrder that originated this ElementOrder.
        /// <remarks>Used by an Element to determine whether an order outcome callback should occur. If default, the order was not issued
        /// by the element's Cmd so no order outcome callback will occur.
        /// </remarks>
        /// <remarks>Also used by the Cmd to determine whether the order outcome callback it has just received should be passed onto 
        /// the executing state. If this OrderID is the same as the OrderID returned in the callback, then the callback is
        /// intended for the currently executing state and will be passed onto it. This accounts for the condition where the Cmd's
        /// CurrentOrder has just changed and concurrently it is receiving an order callback from one or more elements intended for
        /// the previous state.</remarks>
        /// </summary>
        public Guid CmdOrderID { get; private set; }

        [Obsolete]
        public FacilityOrder StandingOrder { get; set; }

        public FacilityOrder FollowonOrder { get; set; }

        public IElementNavigableDestination Target { get; private set; }

        public bool ToCallback { get { return CmdOrderID != default(Guid); } }

        /// <summary>
        /// The source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public FacilityDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is expected.</remarks>
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="cmdOrderID">The unique ID of the CmdOrder that caused this element order to be generated.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public FacilityOrder(FacilityDirective directive, OrderSource source, Guid cmdOrderID, IElementNavigableDestination target = null) {
            Directive = directive;
            Source = source;
            CmdOrderID = cmdOrderID;
            Target = target;
            __Validate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is not expected.</remarks>
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public FacilityOrder(FacilityDirective directive, OrderSource source, IElementNavigableDestination target = null)
            : this(directive, source, default(Guid), target) {
        }

        public sealed override string ToString() {
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
            if (Source == OrderSource.Captain) {
                D.Assert(!ToCallback);
            }
            if (Directive == FacilityDirective.Cancel) {
                D.AssertEqual(OrderSource.User, Source);
            }
        }

        #endregion

    }
}

