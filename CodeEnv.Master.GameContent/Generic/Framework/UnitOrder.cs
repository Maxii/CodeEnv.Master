// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitOrder.cs
// Generic wrapper class holding values associated with an order for a Unit's Command or Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Generic wrapper class holding values associated with an order for a Unit's Command or Elements.
    /// </summary>
    /// <typeparam name="T">The enum type of the order.</typeparam>
    public class UnitOrder<T> where T : struct {

        public float Speed { get; set; }
        //public Speed Speed { get; set; }

        public IDestinationTarget Target { get; protected set; }

        public T Order { get; private set; }

        public UnitOrder(T order, IDestinationTarget target = null, float speed = Constants.ZeroF) {
            Order = order;
            Target = target;
            Speed = speed;
        }

        public UnitOrder(T order, Vector3 targetLocation)
            : this(order, new StationaryLocation(targetLocation), 2f) {
        }


        //public UnitOrder(T order, IDestinationTarget target, Speed speed) {
        //    Order = order;
        //    Target = target;
        //    Speed = speed;
        //}


        //public UnitOrder(T order, Vector3 targetLocation, Speed speed)
        //    : this(order, new StationaryLocation(targetLocation), speed) {
        //}

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

