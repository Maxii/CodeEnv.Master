// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetState.cs
// Enum defining the states a Fleet can operate in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining the states a Fleet can operate in.
    /// </summary>
    public enum FleetState {

        None,

        Idling,

        ProcessOrders,

        MovingTo,

        GoPatrol,

        Patrolling,

        GoGuard,

        Guarding,

        GoAttack,

        Attacking,

        // These 2 allow Commands to optionally take hits and show them, seperate from their HQElement
        TakingDamage,
        ShowHit,

        Entrenching,

        GoRepair,

        Repairing,

        GoRefit,

        Refitting,

        GoRetreat,

        GoJoin,

        //Docking,

        //Embarking,

        GoDisband,

        Disbanding,

        Dying,

        ShowDying,

        Dead


    }

}

