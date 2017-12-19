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
        /// Cancels any unit orders issued while in the CURRENT pause. Orders issued in a prior pause will not be canceled.
        /// <remarks>8.14.17 Must be issued by the User and only during pause.</remarks>
        /// <remarks>12.12.17 Requires a null target.</remarks>
        /// </summary>
        Cancel,

        /// <summary>
        /// Facilities 'manage' their own initial construction in a Base. When they receive this order, they are of course already
        /// instantiated and operational and have an existing Construction present (with no progress having occurred yet) in the Base's 
        /// ConstructionManager.
        /// <remarks>12.1.17 Requires a null Target.</remarks>
        /// </summary>
        Construct,

        /// <summary>
        /// Facilities can attack Ships in range owned by enemies of the Facility's owner.
        /// Only Base Cmd may order a Facility to attack.
        /// <remarks>11.8.17 Requires a non-null Target.</remarks>
        /// </summary>
        Attack,

        /// <summary>
        /// Facilities can repair in place. Only Base Cmd, Captain or the User may order a Facility to repair.
        /// <remarks>12.14.17 Requires a non-null (facility's base) Target.</remarks>
        /// </summary>
        Repair,

        /// <summary>
        /// Facilities refit in place. Only Base Cmd or the User/PlayerAI may order a Facility to refit.
        /// <remarks>12.14.17 Requires a non-null (facility's base) Target.</remarks>
        /// </summary>
        Refit,

        /// <summary>
        /// Facilities disband in place. Only Base Cmd or the User/AIPlayer may order a Facility to disband.
        /// When a Facility is disbanded, the owner of the Facility retains a percentage of the resources used to build it.
        /// <remarks>12.14.17 Requires a non-null (facility's base) Target.</remarks>
        /// </summary>
        Disband,

        /// <summary>
        /// Facilities scuttle in place. Only Base Cmd or the User/AIPlayer may order a Facility to scuttle. 
        /// No resources are retained by the owner. 
        /// <remarks>11.8.17 Requires a null Target.</remarks>
        /// </summary>
        Scuttle,

        [System.Obsolete("Not needed as only implemented in AiShip/Facility CtxMenus for debug and not as an order")]
        ChgOwner,


    }
}

