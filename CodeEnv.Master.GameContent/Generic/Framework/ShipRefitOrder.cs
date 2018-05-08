// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipRefitOrder.cs
// An order for a ship to refit to a Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship to refit to a Design.
    /// </summary>
    public class ShipRefitOrder : ShipOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Design = {3}, Target = {4}, FollowonOrder = {5}]";

        public override string DebugName {
            get {
                string designText = RefitDesign != null ? RefitDesign.DebugName : "not yet assigned";
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.DebugName : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), designText,
                    targetText, followonOrderText);
            }
        }

        public new IUnitBaseCmd Target { get { return base.Target as IUnitBaseCmd; } }

        public ShipDesign RefitDesign { get; private set; }

        public bool IncludeCmdModule { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipRefitOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is expected.</remarks>
        /// </summary>
        /// <param name="source">The source of this order.</param>
        /// <param name="cmdOrderID">The unique ID of the CmdOrder that caused this element order to be generated.</param>
        /// <param name="refitDesign">The design to use to refit the ship.</param>
        /// <param name="target">The target of this order.</param>
        /// <param name="includeCmd">If <c>true</c>, also refit the CmdModule.</param>
        public ShipRefitOrder(OrderSource source, Guid cmdOrderID, ShipDesign refitDesign, IShipNavigableDestination target, bool includeCmd)
            : base(ShipDirective.Refit, source, cmdOrderID, target) {
            D.Assert(target is IUnitBaseCmd);
            RefitDesign = refitDesign;
            IncludeCmdModule = includeCmd;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipRefitOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is not expected. Typically used when the order does not
        /// emanate from the Unit. The ships will be in a hanger.</remarks>
        /// </summary>
        /// <param name="source">The source of this order.</param>
        /// <param name="refitDesign">The design to use to refit the ship.</param>
        /// <param name="target">The target of this order.</param>
        public ShipRefitOrder(OrderSource source, ShipDesign refitDesign, IShipNavigableDestination target)
            : this(source, default(Guid), refitDesign, target, false) {
        }


    }
}

