// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetOrder.cs
//  An order for a fleet holding specific info required by the order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An order for a fleet holding specific info required by the order.
    /// </summary>
    public class FleetOrder {

        public float StandoffDistance { get; private set; }

        public Speed Speed { get; private set; }

        public IDestinationTarget Target { get; private set; }

        public FleetOrders Order { get; private set; }

        public FleetOrder(FleetOrders order, IDestinationTarget target = null, Speed speed = Speed.None, float standoffDistance = Constants.ZeroF) {
            Order = order;
            Target = target;
            Speed = speed;
            StandoffDistance = standoffDistance;
        }

        public FleetOrder(FleetOrders order, Vector3 destination, Speed speed) :
            this(order, new StationaryLocation(destination), speed) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

