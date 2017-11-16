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

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Design = {3}, Target = {4}, FollowonOrder = {5}, StandingOrder = {6}]";

        public override string DebugName {
            get {
                string designText = RefitDesign != null ? RefitDesign.DebugName : "not yet assigned";
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.DebugName : "none";
                string standingOrderText = StandingOrder != null ? StandingOrder.DebugName : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), designText,
                    targetText, followonOrderText, standingOrderText);
            }
        }

        public new IUnitBaseCmd Target { get { return base.Target as IUnitBaseCmd; } }

        public ShipDesign RefitDesign { get; private set; }

        public ShipRefitOrder(ShipDirective directive, OrderSource source, ShipDesign refitDesign, IShipNavigableDestination target)
            : base(directive, source, default(Guid), target) {
            D.AssertEqual(ShipDirective.Refit, directive);
            D.Assert(target is IUnitBaseCmd);
            RefitDesign = refitDesign;
        }


    }
}

