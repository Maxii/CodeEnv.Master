// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FsmCallReturnCause.cs
// Enum representing the cause of a Return() when a Unit member Return()s from an FSM Call()ed state.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum representing the cause of a Return() when a Unit member Return()s from an FSM Call()ed state.
    /// <remarks>Used to communicate the cause of a Call()ed state's Return to the
    /// Call()ing state, working in conjunction with FsmReturnHandler.</remarks>
    /// <remarks>IMPROVE should be broken down further</remarks>
    /// </summary>
    public enum FsmCallReturnCause {

        /// <summary>
        /// No cause for the Return() indicating successful completion of the Call()ed state without interruption.
        /// </summary>
        None = 0,

        /// <summary>
        /// The target has been determined to be uncatchable. 
        /// <remarks>4.15.17 Currently there are 3 sources of this Return Cause.
        /// 1) A TgtFleet is progressively getting further away from our pursuing fleet,
        /// 2) our Cmd has lost awareness of the TgtFleet, aka our sensors can no longer detect it, and
        /// 3) the TgtShip of our pursuing ship has moved outside the distance envelope that our FleetCmd can support. 
        /// While 3)'s approach was developed when SRSensors were located on Cmds, it still is a useful way of keeping 
        /// ship's on individual assignments from getting too far away from their FleetCmd.</remarks>
        /// </summary>
        TgtUncatchable,

        /// <summary>
        /// The target has been determined to be unreachable.
        /// <remarks>4.15.17 Currently this refers to the inability to plot a course to reach the target.</remarks>
        /// </summary>
        TgtUnreachable,

        /// <summary>
        /// Our relationship with the target has changed in a way that no longer allows execution of the Call()ed state.
        /// </summary>
        TgtRelationship,

        /// <summary>
        /// The target has died.
        /// </summary>
        TgtDeath,

        /// <summary>
        /// This Unit or Element needs repair.  
        /// </summary>
        NeedsRepair,

        /// <summary>
        /// This Unit Element had construction rework occurring on it canceled.
        /// </summary>
        ConstructionCanceled,

        /// <summary>
        /// This Unit or Element is no longer qualified to execute the Call()ed state. 
        /// <remarks>e.g. An element that becomes the HQ while Disengaging is no longer qualified to disengage.</remarks>
        /// <remarks>Can also be used as a catchall cause for a Return when knowing the specific OrderOutcome failure cause is not important.</remarks>
        /// </summary>
        Qualifications


    }

}

