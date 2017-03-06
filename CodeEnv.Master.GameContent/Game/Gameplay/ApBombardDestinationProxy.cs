// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApBombardDestinationProxy.cs
// Proxy used by a Ship's AutoPilot to navigate to and bombard an IShipAttackable target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Proxy used by a Ship's AutoPilot to navigate to and bombard an IShipAttackable target.
    /// </summary>
    public class ApBombardDestinationProxy : ApMoveDestinationProxy {

        public ApBombardDestinationProxy(IShipNavigable destination, IShip ship, float innerRadius, float outerRadius)
            : base(destination, ship, innerRadius, outerRadius) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

