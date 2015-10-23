﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetDirective.cs
//  The directives that can be issued to a fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  The directives that can be issued to a fleet.
    /// </summary>
    public enum FleetDirective {

        None,

        Attack,

        Disband,

        //DisbandAt,

        Guard,

        Join,

        /// <summary>
        /// Move to an IDestinationTarget.
        /// </summary>
        Move,

        Patrol,

        Refit,

        Repair,

        //RepairAt,

        Retreat,

        //RetreatTo,

        StopAttack,

        Explore,

        Scuttle

    }
}

