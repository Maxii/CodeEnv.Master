// --------------------------------------------------------------------------------------------------------------------
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

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Order for a Base - Settlement or Starbase.
    /// </summary>
    public class BaseOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Target = {3}, OrderID = {4}, FollowonOrder = {5}]";

        private static readonly BaseDirective[] DirectivesWithNullTarget = new BaseDirective[]  {
                                                                                                    BaseDirective.Cancel,
                                                                                                    BaseDirective.Scuttle
                                                                                                };

        private static readonly BaseDirective[] DirectivesWithNonNullTarget = new BaseDirective[]   {
                                                                                                        BaseDirective.Attack,
                                                                                                        BaseDirective.Disband,
                                                                                                        BaseDirective.Refit,
                                                                                                        BaseDirective.Repair,
                                                                                                        BaseDirective.ChangeHQ
                                                                                                    };
        public string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string orderIdText = OrderID.ToShortText();
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, orderIdText, followonOrderText);
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
        public BaseOrder StandingOrder { get; set; }

        public BaseOrder FollowonOrder { get; set; }

        public INavigableDestination Target { get; private set; }

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
        public BaseOrder(BaseDirective directive, OrderSource source, INavigableDestination target = null) {
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
            if (Directive == BaseDirective.Cancel) {
                D.AssertEqual(OrderSource.User, Source);
            }
        }
    }

    #endregion

}

