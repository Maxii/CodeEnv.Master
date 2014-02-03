// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseState.cs
// Enum defining the states a Starbase can operate in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining the states a Starbase can operate in.
    /// </summary>
    public enum StarbaseState {

        None,
        Idling,
        ProcessOrders,
        GoAttack,   // ?
        Attacking,

        // These 2 allow Commands to optionally take hits and show them, seperate from their HQElement
        TakingDamage,
        ShowHit,

        GoRepair,   // ?
        Repairing,
        GoRefit,   // ?
        Refitting,
        GoDisband,   // ?
        Disbanding,
        Dying,
        ShowDying,
        Dead

    }
}

