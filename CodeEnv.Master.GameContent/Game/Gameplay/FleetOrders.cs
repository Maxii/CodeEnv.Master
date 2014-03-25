﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetOrders.cs
//  The orders that can be issued to a fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  The orders that can be issued to a fleet.
    /// </summary>
    public enum FleetOrders {

        None,

        Attack,

        Disband,

        DisbandAt,

        Guard,

        JoinFleet,

        /// <summary>
        /// Move to an IDestinationTarget.
        /// </summary>
        MoveTo,

        Patrol,

        RefitAt,

        Repair,

        RepairAt,

        Retreat,

        RetreatTo,

        StopAttack

    }
}

