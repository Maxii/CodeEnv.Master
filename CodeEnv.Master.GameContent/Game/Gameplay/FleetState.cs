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

        /// <summary>
        /// State that executes the FleetOrder MoveTo. Upon move completion
        /// the state reverts to Idling.
        /// </summary>
        ExecuteMoveOrder,

        /// <summary>
        /// Call-only state that exists while an entire fleet is moving from one position to another.
        /// This can occur as part of the execution process for a number of FleetOrders.
        /// </summary>
        Moving,

        GoPatrol,
        Patrolling,

        GoGuard,
        Guarding,

        /// <summary>
        /// State that executes the FleetOrder Attack which encompasses Moving
        /// and Attacking.
        /// </summary>
        ExecuteAttackOrder,
        Attacking,

        Entrenching,

        GoRepair,
        Repairing,

        GoRefit,
        Refitting,

        GoRetreat,

        GoJoin,

        GoDisband,
        Disbanding,

        Dead

        // ShowHit no longer applicable to Cmds as there is no mesh
        // TODO Docking, Embarking, etc.
    }

}

