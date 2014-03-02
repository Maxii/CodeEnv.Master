// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitMoveOrder.cs
// Order to move to a IDestinationTarget at a provided speed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Order to move to a IDestinationTarget at a provided speed.
    /// </summary>
    public class UnitMoveOrder<T> : UnitDestinationTargetOrder<T> where T : struct {

        public float StandoffDistance { get; private set; }

        public Speed Speed { get; private set; }

        public UnitMoveOrder(T order, IDestinationTarget target, Speed speed, float standoffDistance = Constants.ZeroF)
            : base(order, target) {
            Speed = speed;
            StandoffDistance = standoffDistance;
        }

        public UnitMoveOrder(T order, Vector3 targetLocation, Speed speed)
            : this(order, new StationaryLocation(targetLocation), speed) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

