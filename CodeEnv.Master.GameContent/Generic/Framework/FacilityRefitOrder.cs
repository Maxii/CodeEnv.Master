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

    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a facility to refit to a Design.
    /// </summary>
    public class FacilityRefitOrder : FacilityOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, Design = {3}, FollowonOrder = {4}, StandingOrder = {5}]";

        public override string DebugName {
            get {
                string followonOrderText = FollowonOrder != null ? FollowonOrder.DebugName : "none";
                string standingOrderText = StandingOrder != null ? StandingOrder.DebugName : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), RefitDesign.DebugName, followonOrderText, standingOrderText);
            }
        }

        public FacilityDesign RefitDesign { get; private set; }

        public FacilityRefitOrder(FacilityDirective directive, OrderSource source, FacilityDesign refitDesign)
            : base(directive, source) {
            D.AssertEqual(FacilityDirective.Refit, directive);
            RefitDesign = refitDesign;
        }

    }
}

