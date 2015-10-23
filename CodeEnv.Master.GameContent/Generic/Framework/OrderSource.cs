// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrderSource.cs
// The originating source of an order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The originating source of an order.
    /// </summary>
    public enum OrderSource {

        None,

        ElementCaptain,

        UnitCommand

        // User  // Dropped as ShipHelm needs to know which type of Speed
        // used based off of OrderSource

    }
}

