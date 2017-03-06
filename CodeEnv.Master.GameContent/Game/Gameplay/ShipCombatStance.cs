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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// A ship's primary tactical approach to engaging in combat with its target.
    /// </summary>
    public enum ShipCombatStance {

        None,

        /// <summary>
        /// The ship will attempt to dis-engage from the battlefield to minimize damage.
        /// Practically, it withdraws to a more protected station in the formation, if needed.
        /// <remarks>Flagships cannot Disengage as they should already be located in a protected station
        /// of the formation. This means Flagships don't withdraw, which makes sense as moving to
        /// a different station in the formation would cause all other ships to move too.</remarks>
        /// </summary>
        Disengage,

        /// <summary>
        /// The ship will not choose to pursue a target. It will Entrench and defend itself
        /// without withdrawing.
        /// <remarks>This should be the setting for all Flagships.</remarks>
        /// </summary>
        Defensive,

        /// <summary>
        /// The ship will aggressively pursue and Bombard its target from a distance where all 
        /// of its weapons can be brought to bear.
        /// <remarks>Bombard behaviour involves finding a location relative to the target 
        /// where it can continuously fire and staying there until the target either moves out
        /// of range, is destroyed or the attacker decides to withdraw, typically for repair.</remarks>
        /// </summary>
        PointBlank,

        /// <summary>
        /// The ship will pursue and Bombard its target from a distance where its 
        /// long and medium range weapons can be brought to bear.
        /// <remarks>Bombard behaviour involves finding a location relative to the target 
        /// where it can continuously fire and staying there until the target either moves out
        /// of range, is destroyed or the attacker decides to withdraw, typically for repair.</remarks>
        /// IMPROVE should pick distance based on what its primary weapons are along with other factors.
        /// </summary>
        BalancedBombard,

        /// <summary>
        /// The ship will pursue and Strafe its target with its long and medium range weapons.
        /// <remarks>Strafe behaviour involves approaching a target at speed, firing, then
        /// withdrawing to a location out of range before repeating the behaviour.</remarks>
        /// IMPROVE should pick distance based on what its primary weapons are along with other factors.
        /// </summary>
        BalancedStrafe,

        /// <summary>
        /// The ship will pursue and Bombard its target from a distance where only 
        /// its longest range weapons can be brought to bear.
        /// <remarks>Bombard behaviour involves finding a location relative to the target 
        /// where it can continuously fire and staying there until the target either moves out
        /// of range, is destroyed or the attacker decides to withdraw, typically for repair.</remarks>
        /// </summary>
        Standoff

    }
}

