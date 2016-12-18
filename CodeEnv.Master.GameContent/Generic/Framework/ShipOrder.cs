// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrder.cs
// An order for a ship that is not a Move.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship that is not a Move.
    /// </summary>
    public class ShipOrder {

        private const string ToStringFormat = "[{0}: Directive = {1}, Source = {2}, ToNotify = {3}, Target = {4}, FollowonOrder = {5}, StandingOrder = {6}]";

        private static readonly ShipDirective[] DirectivesWithNullTarget = new ShipDirective[] {
                                                                                                    ShipDirective.AssumeStation,
                                                                                                    ShipDirective.Entrench,
                                                                                                    ShipDirective.Refit,
                                                                                                    ShipDirective.Repair,
                                                                                                    ShipDirective.Retreat,
                                                                                                    ShipDirective.Scuttle,
                                                                                                    ShipDirective.StopAttack,
                                                                                                    ShipDirective.Disengage
                                                                                                };

        public ShipOrder StandingOrder { get; set; }

        public ShipOrder FollowonOrder { get; set; }

        public IShipNavigable Target { get; private set; }

        public bool ToNotifyCmd { get; private set; }

        /// <summary>
        /// The source of this order. 
        /// </summary>
        public OrderSource Source { get; private set; }

        public ShipDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="toNotifyCmd">if set to <c>true</c> the ship will notify its Command of the outcome.</param>
        /// <param name="target">The target of this order. No need for FormationStation. Default is null.</param>
        public ShipOrder(ShipDirective directive, OrderSource source, bool toNotifyCmd = false, IShipNavigable target = null) {
            if (directive == ShipDirective.Move) {
                D.AssertEqual(typeof(ShipMoveOrder), GetType());
                D.Assert(!toNotifyCmd);
            }
            if (directive.EqualsAnyOf(DirectivesWithNullTarget)) {
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

