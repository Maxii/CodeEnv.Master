// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseDirective.cs
// The directives that can be issued to a Base.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a Base.
    /// </summary>
    public enum BaseDirective {

        None,

        /// <summary>
        /// Bases can attack Fleets owned by enemies of the Base owner if they are in range. 
        /// Used primarily to focus fire on a single fleet when multiple enemy fleets are in range
        /// since Bases will always fire on enemy fleets in range but may spread that fire across multiple
        /// fleets. Only Base Cmd or the User can issue an Attack order.
        /// </summary>
        Attack,

        /// <summary>
        /// Like Fleets, Bases can repair in place. This in place repair occurs much faster than a fleet due to the
        /// larger amount of infrastructure and resources available to the base. Only Base Cmd can issue a Repair order.
        /// </summary>
        Repair,

        /// <summary>
        /// Bases can be refit in place when not under attack by an enemy fleet. Only Base Cmd or the User can issue a Refit order.
        /// </summary>
        Refit,

        /// <summary>
        /// Bases can be disbanded in place when not under attack by an enemy fleet.
        /// Similar effect to scuttling except the owner of the base retains a percentage of the resources used to build it.
        /// Only Base Cmd or the User can issue a Disband order.
        /// </summary>
        Disband,

        /// <summary>
        /// Bases can be scuttled at any time. Similar effect to disbanding but no resources are retained by the owner.
        /// The principal time this would be chosen over Disband is when the base was under attack and unlikely to survive.
        /// Only Base Cmd or the User can issue a Scuttle order.
        /// </summary>
        Scuttle,

        StopAttack,


    }
}

