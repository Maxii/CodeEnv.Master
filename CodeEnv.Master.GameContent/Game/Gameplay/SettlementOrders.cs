// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementOrders.cs
// The orders that can be issued to a Settlement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The orders that can be issued to a Settlement.
    /// </summary>
    public enum SettlementOrders {

        None,

        Attack,

        Disband,

        DisbandAt,

        RefitAt,

        Repair,

        RepairAt,

        StopAttack

    }
}

