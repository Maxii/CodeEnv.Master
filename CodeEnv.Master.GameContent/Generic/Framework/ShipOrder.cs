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
    using System;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship that is not a Move.
    /// </summary>
    public class ShipOrder {

        private const string DebugNameFormat = "[{0}: Directive = {1}, Source = {2}, ToNotify = {3}, Target = {4}, FollowonOrder = {5}, StandingOrder = {6}]";

        private static readonly ShipDirective[] DirectivesWithNullTarget = new ShipDirective[] {
                                                                                                    ShipDirective.AssumeStation,
                                                                                                    ShipDirective.Entrench,
                                                                                                    ShipDirective.Retreat,
                                                                                                    ShipDirective.Scuttle,
                                                                                                    ShipDirective.StopAttack,
                                                                                                    ShipDirective.Disengage
                                                                                                };

        public virtual string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), ToCallback, targetText, followonOrderText, standingOrderText);
            }
        }

        /// <summary>
        /// The Unique OrderID of the CmdOrder that originated this ElementOrder. If default value this element order
        /// does not require an order outcome callback from this element to the Cmd, either because the order isn't 
        /// from Cmd, or the CmdOrder does not require a callback.
        /// <remarks>Used to determine whether the element receiving this order should respond to Cmd with the outcome 
        /// of the order's execution. If the element calls back with the outcome, the Cmd uses the ID to determine whether the
        /// callback it received is still relevant, aka the current CmdOrder has the same ID as the ID returned from the element.
        /// </remarks>
        /// </summary>
        public Guid CmdOrderID { get; private set; }

        public ShipOrder StandingOrder { get; set; }

        public ShipOrder FollowonOrder { get; set; }

        public IShipNavigableDestination Target { get; private set; }

        public bool ToCallback { get { return CmdOrderID != default(Guid); } }

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
        /// <param name="cmdOrderID">The unique ID of the CmdOrder that caused this element order to be generated. If assigned
        /// it indicates that the element receiving this order should callback to Cmd with the outcome of the order's execution.</param>
        /// <param name="target">The target of this order. No need for FormationStation. Default is null.</param>
        public ShipOrder(ShipDirective directive, OrderSource source, Guid cmdOrderID = default(Guid), IShipNavigableDestination target = null) {
            if (directive == ShipDirective.Move) {
                D.AssertEqual(typeof(ShipMoveOrder), GetType());
                D.AssertDefault(cmdOrderID);
            }
            Directive = directive;
            Source = source;
            CmdOrderID = cmdOrderID;
            Target = target;
            __Validate();
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        protected virtual void __Validate() {
            if (DirectivesWithNullTarget.Contains(Directive)) {
                D.AssertNull(Target);
            }
        }

        #endregion

    }
}

