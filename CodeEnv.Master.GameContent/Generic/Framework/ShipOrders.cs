// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrders.cs
// The orders that can be issued to a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The orders that can be issued to a ship.
    /// </summary>
    public enum ShipOrders {

        None,

        AllStop,

        MoveTo,

        Attack,

        Repair,

        Entrench,

        Refit,

        Disband,

        JoinFleetAt

    }
}

