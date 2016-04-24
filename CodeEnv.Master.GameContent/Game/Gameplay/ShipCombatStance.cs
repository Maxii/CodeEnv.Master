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
        /// The ship will attempt to dis-engage from the battlefield, minimizing any damage.
        /// </summary>
        Retreat,

        /// <summary>
        /// The ship will attempt to engage its target at a distance where all 
        /// of its offensive weapons can be brought to bear.
        /// </summary>
        PointBlank,

        /// <summary>
        /// The ship will attempt to engage its target at a distance where its 
        /// long and medium range weapons can be brought to bear.
        /// IMPROVE should pick distance based on what its primary weapons are.
        /// </summary>
        Optimal,

        /// <summary>
        /// The ship will attempt to engage its target at a distance where only 
        /// its longest range offensive weapons can be brought to bear.
        /// </summary>
        Standoff

    }
}

