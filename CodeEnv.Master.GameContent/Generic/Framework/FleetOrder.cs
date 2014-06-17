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

        public FleetDirective Directive { get; private set; }

        public FleetOrder(FleetDirective directive, IDestinationTarget target = null, Speed speed = Speed.None, float standoffDistance = Constants.ZeroF) {
            D.Assert(target == null || !(target is StationaryLocation));    // Fleet targets should never be a StationaryLocation
            Directive = directive;
            Target = target;
            Speed = speed;
            StandoffDistance = standoffDistance;
        }

        //public FleetOrder(FleetDirective order, Vector3 destination, Speed speed) :
        //    this(order, new StationaryLocation(destination), speed) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

