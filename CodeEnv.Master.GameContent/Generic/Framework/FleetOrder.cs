﻿// --------------------------------------------------------------------------------------------------------------------
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

        public INavigableTarget Target { get; private set; }

        public OrderSource Source { get; private set; }

        public FleetDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        /// <param name="speed">The speed this order should be executed at, if applicable.</param>
        /// <param name="standoffDistance">The standoff distance.</param>
        public FleetOrder(FleetDirective directive, OrderSource source, INavigableTarget target = null, Speed speed = Speed.None, float standoffDistance = Constants.ZeroF) {
            D.Assert(target == null || (!(target is IFleetFormationStation) && !(target is IUnitElementItem)));
            D.Assert(source != OrderSource.Captain);
            Directive = directive;
            Source = source;
            Target = target;
            Speed = speed;
            StandoffDistance = standoffDistance;
        }

        public override string ToString() {// IMPROVE
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

