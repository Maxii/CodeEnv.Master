// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipMoveOrder.cs
// An order for a ship to move to a target at a specific speed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship to move to a target at a specific speed.
    /// </summary>
    public class ShipMoveOrder : ShipOrder {

        private const string DebugNameFormat = @"{0}: Directive = {1}, Source = {2}, Target = {3}, Speed = {4}, Fleetwide = {5}, 
            FollowonOrder = {6}, Standoff = {7:0.#}.";

        public override string DebugName {
            get {
                string targetText = Target != null ? Target.DebugName : "none";
                string followonOrderText = FollowonOrder != null ? FollowonOrder.ToString() : "none";
                return DebugNameFormat.Inject(GetType().Name, Directive.GetValueName(), Source.GetValueName(), targetText, Speed.GetValueName(),
                    IsFleetwide, followonOrderText, TargetStandoffDistance);
            }
        }

        /// <summary>
        /// The speed of this move.
        /// </summary>
        public Speed Speed { get; private set; }

        public bool IsFleetwide { get; private set; }

        /// <summary>
        /// When the ship arrives at the target, this is the distance 
        /// from the target it should strive to achieve.
        /// </summary>
        public float TargetStandoffDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipMoveOrder" /> class.
        /// </summary>
        /// <param name="source">The source of the order.</param>
        /// <param name="cmdOrderID">The unique ID of the CmdOrder that caused this element order to be generated.</param>
        /// <param name="target">The move target.</param>
        /// <param name="speed">The move speed.</param>
        /// <param name="isFleetwide">if set to <c>true</c> the move should be coordinated as a fleet.</param>
        /// <param name="targetStandoffDistance">When the ship arrives at the target, this is the distance
        /// from the target it should strive to achieve.</param>
        [Obsolete("Ship.ExecuteMoveOrder does not expect an order outcome callback")]
        public ShipMoveOrder(OrderSource source, Guid cmdOrderID, IShipNavigableDestination target, Speed speed, bool isFleetwide, float targetStandoffDistance)
            : base(ShipDirective.Move, source, cmdOrderID, target) {
            Utility.ValidateNotNull(target);
            D.AssertNotDefault((int)speed);
            Utility.ValidateNotNegative(targetStandoffDistance);
            Speed = speed;
            IsFleetwide = isFleetwide;
            TargetStandoffDistance = targetStandoffDistance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipMoveOrder" /> class.
        /// <remarks>A ship's ExecuteMoveOrder state does not expect a order outcome callback.</remarks>
        /// </summary>
        /// <param name="source">The source of the order.</param>
        /// <param name="target">The move target.</param>
        /// <param name="speed">The move speed.</param>
        /// <param name="isFleetwide">if set to <c>true</c> the move should be coordinated as a fleet.</param>
        /// <param name="targetStandoffDistance">When the ship arrives at the target, this is the distance
        /// from the target it should strive to achieve.</param>
        public ShipMoveOrder(OrderSource source, IShipNavigableDestination target, Speed speed, bool isFleetwide, float targetStandoffDistance)
            : base(ShipDirective.Move, source, target) {
            D.AssertNotNull(target);
            D.AssertNotDefault((int)speed);
            Utility.ValidateNotNegative(targetStandoffDistance);
            Speed = speed;
            IsFleetwide = isFleetwide;
            TargetStandoffDistance = targetStandoffDistance;
        }


    }
}

