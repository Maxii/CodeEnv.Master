// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FsmOrderFailureCause.cs
// Enum representing the cause of failure when a Unit member fails to successfully execute an order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum representing the cause of failure when a Unit member fails to successfully execute an order.
    /// <remarks>Used to communicate the cause of an interrupted Call()ed state's Return to the
    /// Call()ing state, working in conjunction with FsmReturnHandler. Also used as the cause of an element's 
    /// failure to execute a Cmd's order when the Cmd requires an order outcome callback.</remarks>
    /// </summary>
    public enum FsmOrderFailureCause {

        None = 0,

        /// <summary>
        /// A new order has just been received.
        /// <remarks>FailureCause only.</remarks>
        /// </summary>
        NewOrderReceived,

        /// <summary>
        /// The target has been determined to be uncatchable. 
        /// <remarks>4.15.17 Currently there are 3 sources of this Return and Failure Cause.
        /// 1) A TgtFleet is progressively getting further away from our pursuing fleet,
        /// 2) our Cmd has lost awareness of the TgtFleet, aka our sensors can no longer detect it, and
        /// 3) the TgtShip of our pursuing ship has moved outside the distance envelope that our
        /// FleetCmd can support. While 3)'s approach was developed when SRSensors were located on Cmds, 
        /// it still is a useful way of keeping ship's on individual assignments from getting too far
        /// away from their FleetCmd.</remarks>
        /// <remarks>FailureCause and ReturnCause.</remarks>
        /// </summary>
        TgtUncatchable,

        /// <summary>
        /// The target has been determined to be unreachable.
        /// <remarks>4.15.17 Currently this refers to the inability to plot a course to reach the target.</remarks>
        /// <remarks>FailureCause and ReturnCause.</remarks>
        /// </summary>
        TgtUnreachable,

        /// <summary>
        /// Our relationship with the target has changed in a way that no longer
        /// allows execution of the current state.
        /// <remarks>FailureCause and ReturnCause.</remarks>
        /// </summary>
        TgtRelationship,

        /// <summary>
        /// The target has died.
        /// <remarks>FailureCause and ReturnCause.</remarks>
        /// </summary>
        TgtDeath,

        /// <summary>
        /// This Unit Cmd or Element needs repair.  
        /// <remarks>FailureCause and ReturnCause.</remarks>
        /// </summary>
        NeedsRepair,

        /// <summary>
        /// This Unit Cmd or Element's current owner is about to lose ownership.
        /// <remarks>FailureCause only.</remarks>
        /// </summary>
        Ownership,

        /// <summary>
        /// This Unit Cmd or Element has died.
        /// <remarks>FailureCause only.</remarks>
        /// </summary>
        Death

    }

}

