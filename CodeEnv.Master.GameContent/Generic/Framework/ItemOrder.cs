// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemOrder.cs
// Generic wrapper class holding values associated with an order for an Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Generic wrapper class holding values associated with an order for an Item.
    /// </summary>
    /// <typeparam name="T">The enum type of the order.</typeparam>
    public class ItemOrder<T> where T : struct {

        public float Speed { get; set; }

        public ITarget Target { get; private set; }

        public T Order { get; private set; }

        public ItemOrder(T order, ITarget target = null, float speed = Constants.ZeroF) {
            Order = order;
            Target = target;
            Speed = speed;
        }

        public ItemOrder(T order, Vector3 targetLocation)
            : this(order, new StationaryLocation(targetLocation), 2F) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

