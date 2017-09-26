// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDirective.cs
// The directives that can be issued to a facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The directives that can be issued to a facility.
    /// </summary>
    public enum FacilityDirective {

        None,

        /// <summary>
        /// Cancels any element orders issued while paused.
        /// <remarks>8.14.17 Must be issued by OrderSource.User and only during pause.</remarks>
        /// </summary>
        Cancel,

        /// <summary>
        /// Facilities can attack Ships in range owned by enemies of the Facility's owner.
        /// Only Base Cmd may order a Facility to attack.
        /// </summary>
        Attack,

        /// <summary>
        /// Facilities can repair in place. Only Base Cmd or Captain may order a Facility to repair itself.
        /// </summary>
        Repair,

        /// <summary>
        /// Facilities can refit in place when not under attack. Only Base Cmd may order a Facility to refit.
        /// UNCLEAR What about when base is under attack?
        /// </summary>
        Refit,

        /// <summary>
        /// Facilities can disband in place when not under attack. Facilities can only be ordered to Disband by Base Cmd or
        /// the User. When a Facility is disbanded, the owner of the Facility retains a percentage of the resources used to build it.
        /// </summary>
        Disband,

        /// <summary>
        /// Facilities can be scuttled in place at any time. Scuttle orders are only
        /// issuable by the User. No resources are retained by the owner. 
        /// </summary>
        Scuttle,

        ChgOwner,

        StopAttack

    }
}

