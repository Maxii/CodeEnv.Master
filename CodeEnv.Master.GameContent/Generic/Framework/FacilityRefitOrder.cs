// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityRefitOrder.cs
// An order for a facility to refit to a Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a facility to refit to a Design.
    /// <remarks>6.8.18 I purposely pick RefitDesign before getting to the element so that the AI can make a better
    /// choice of what to refit too. It will make a better choice because it can see all the elements in this Cmd 
    /// that are candidates for a refit.</remarks>
    /// </summary>
    public class FacilityRefitOrder : FacilityOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, \nDesign = {3}, \nCmdOrderID = {4}, \nFollowonOrder = {5}]";

        public override string DebugName {
            get {
                string designText = RefitDesign != null ? RefitDesign.DebugName : "not yet assigned";
                string cmdOrderIdText = CmdOrderID != default(Guid) ? CmdOrderID.ToShortText() : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.DebugName : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), designText, cmdOrderIdText, followonOrderText);
            }
        }

        public FacilityDesign RefitDesign { get; private set; }

        public bool IncludeCmdModule { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityRefitOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is expected.</remarks>
        /// </summary>
        /// <param name="source">The source of this order.</param>
        /// <param name="cmdOrderID">The unique ID of the CmdOrder that caused this element order to be generated.</param>
        /// <param name="refitDesign">The design to use for the refit.</param>
        /// <param name="target">The target.</param>
        /// <param name="includeCmd">If <c>true</c>, also refit the CmdModule.</param>
        public FacilityRefitOrder(OrderSource source, Guid cmdOrderID, FacilityDesign refitDesign, IElementNavigableDestination target, bool includeCmd)
            : base(FacilityDirective.Refit, source, cmdOrderID, target) {
            D.Assert(target is IUnitBaseCmd);
            RefitDesign = refitDesign;
            IncludeCmdModule = includeCmd;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityRefitOrder" /> class.
        /// <remarks>11.24.17 For use when an order outcome callback is not expected. Typically used when the order does not
        /// emanate from the Unit.</remarks>
        /// </summary>
        /// <param name="source">The source of this order.</param>
        /// <param name="refitDesign">The design to use for the refit.</param>
        /// <param name="target">The target.</param>
        public FacilityRefitOrder(OrderSource source, FacilityDesign refitDesign, IElementNavigableDestination target)
            : this(source, default(Guid), refitDesign, target, false) {
        }

    }
}

