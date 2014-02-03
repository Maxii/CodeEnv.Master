// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityOrders.cs
// The orders that can be issued to a facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The orders that can be issued to a facility.
    /// </summary>
    public enum FacilityOrders {

        None,

        Attack,

        StopAttack,

        Repair,

        Refit,

        Disband

    }
}

