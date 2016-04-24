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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An order for a ship to move to a target at a specific speed.
    /// </summary>
    public class ShipMoveOrder : ShipOrder {

        private const string ToStringFormat = "Directive: {0}, Source: {1}, Target: {2}, Speed: {3}, Mode: {4}, StandingOrder: {5}, Standoff: {6:0.#}.";

        /// <summary>
        /// The speed of this move.
        /// </summary>
        public Speed Speed { get; private set; }

        public ShipMoveMode Mode { get; private set; }

        /// <summary>
        /// When the ship arrives at the target, this is the distance 
        /// from the target it should strive to achieve.
        /// </summary>
        public float TargetStandoffDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipMoveOrder" /> class.
        /// </summary>
        /// <param name="source">The source of the order.</param>
        /// <param name="target">The move target.</param>
        /// <param name="speed">The move speed.</param>
        /// <param name="mode">The mode the ship is to move in.</param>
        /// <param name="targetStandoffDistance">When the ship arrives at the target, this is the distance 
        /// from the target it should strive to achieve.</param>
        public ShipMoveOrder(OrderSource source, IShipNavigable target, Speed speed, ShipMoveMode mode, float targetStandoffDistance)
            : base(ShipDirective.Move, source, target) {
            Utility.ValidateNotNull(target);
            D.Assert(speed != Speed.None);
            D.Assert(mode != ShipMoveMode.None);
            Utility.ValidateNotNegative(targetStandoffDistance);
            Speed = speed;
            Mode = mode;
            TargetStandoffDistance = targetStandoffDistance;
        }

        public override string ToString() {
            string targetText = Target != null ? Target.FullName : "null";
            string standingOrderText = StandingOrder != null ? StandingOrder.ToString() : "null";
            return ToStringFormat.Inject(Directive.GetValueName(), Source.GetValueName(), targetText, Speed.GetValueName(), Mode.GetValueName(), standingOrderText, TargetStandoffDistance);
        }

    }
}

