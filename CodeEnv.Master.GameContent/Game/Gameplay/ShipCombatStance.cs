// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCombatStance.cs
// A ship's primary tactical approach to engaging in combat with its target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// A ship's primary tactical approach to engaging in combat 
    /// with its target.
    /// </summary>
    public enum ShipCombatStance {

        None,

        /// <summary>
        /// The ship will attempt to dis-engage from the battlefield to minimize damage,
        /// withdrawing if necessary.   //TODO Disengage and withdrawal not yet implemented
        /// </summary>
        Disengage,

        /// <summary>
        /// The ship will not choose to pursue a target but will actively defend itself
        /// without withdrawing.
        /// </summary>
        Defensive,

        /// <summary>
        /// The ship will aggressively pursue and engage its target at a distance where all 
        /// of its weapons can be brought to bear.
        /// </summary>
        PointBlank,

        /// <summary>
        /// The ship will pursue and engage its target at a distance where its 
        /// long and medium range weapons can be brought to bear.
        /// IMPROVE should pick distance based on what its primary weapons are
        /// along with other factors.
        /// </summary>
        Balanced,

        /// <summary>
        /// The ship will pursue and engage its target at a distance where only 
        /// its longest range weapons can be brought to bear.
        /// </summary>
        Standoff

    }
}

