// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipState.cs
// Enum defining the states a Ship can operate in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining the states a Ship can operate in.
    /// </summary>
    public enum ShipState {

        None,

        Idling,

        ProcessOrders,

        MovingTo,

        Chasing,

        GoAttack,

        Attacking,

        ShowAttacking,

        TakingDamage,

        ShowHit,

        Entrenching,

        Repairing,

        Refitting,

        Joining,

        //Docking,

        //Embarking,

        Withdrawing,

        Disbanding,

        Dying,

        ShowDying,

        Dead


    }
}

