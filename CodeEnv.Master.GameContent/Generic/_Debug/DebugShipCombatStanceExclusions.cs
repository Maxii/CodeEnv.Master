// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugShipCombatStanceExclusions.cs
// The combat stances that are excluded from use by ships in a fleet. Used for debug settings in the editor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The combat stances that are excluded from use by ships in a fleet. Used for debug settings in the editor. 
    /// </summary>
    public enum DebugShipCombatStanceExclusions {

        /// <summary>
        /// ShipCombatStance choice will be random.
        /// </summary>
        None,

        /// <summary>
        /// ShipCombatStance choice will be random, excluding Disengage.
        /// </summary>
        Disengage,

        /// <summary>
        /// ShipCombatStance choice will be random, excluding Disengage and Defensive.
        /// </summary>
        DefensiveAndDisengage,

        AllExceptStandoff,

        AllExceptBalanced,

        AllExceptPointBlank


    }
}

