// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApBesiegeDestinationProxy.cs
// Proxy used by a Ship's AutoPilot to navigate to and besiege an IShipAttackable target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Proxy used by a Ship's AutoPilot to navigate to and besiege an IShipAttackable target.
    /// </summary>
    public class ApBesiegeDestinationProxy : ApMoveDestinationProxy {

        public ApBesiegeDestinationProxy(IShipNavigable destination, IShip ship, float innerRadius, float outerRadius)
            : base(destination, ship, innerRadius, outerRadius) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

