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
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a facility holding specific info required by the order.
    /// </summary>
    public class FacilityOrder {

        private const string ToStringFormat = "[{0}: Directive = {1}, Source = {2}, ToNotify = {3}, Target = {4}, FollowonOrder = {5}, StandingOrder = {6}]";

        private static readonly FacilityDirective[] DirectivesWithNullTarget = new FacilityDirective[] {
                                                                                                    FacilityDirective.Refit,
                                                                                                    FacilityDirective.Repair,
                                                                                                    FacilityDirective.Disband,
                                                                                                    FacilityDirective.Scuttle,
                                                                                                    FacilityDirective.StopAttack,
                                                                                                };
        public FacilityOrder StandingOrder { get; set; }

        public FacilityOrder FollowonOrder { get; set; }

        public IUnitAttackable Target { get; private set; }

        public bool ToNotifyCmd { get; private set; }

        /// <summary>
        /// The source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public FacilityDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="toNotifyCmd">if set to <c>true</c> the facility will notify its Cmd of the outcome.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public FacilityOrder(FacilityDirective directive, OrderSource source, bool toNotifyCmd = false, IUnitAttackable target = null) {
            if (DirectivesWithNullTarget.Contains(directive)) {
                D.AssertNull(target, ToString());
            }
            Directive = directive;
            Source = source;
            ToNotifyCmd = toNotifyCmd;
            Target = target;
        }

        public override string ToString() {
            string targetText = Target != null ? Target.DebugName : "none";
            string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
            string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "none";
            return ToStringFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), ToNotifyCmd, targetText, followonOrderText, standingOrderText);
        }

    }
}

