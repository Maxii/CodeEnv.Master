// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrderOutcome.cs
// Enum used to communicate the order outcome when a Unit member attempts to execute an order from their superior,
// either a Command or PlayerAI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum used to communicate the order outcome when a Unit member attempts to execute an order from their superior,
    /// either a Command or PlayerAI.
    /// </summary>
    public enum OrderOutcome {

        /// <summary>
        /// Error detection.
        /// </summary>
        None = 0,

        Success,

        /// <summary>
        /// A new order has just been received, causing this unit member to attempt to execute the new order.
        /// Successful completion of the previous order was not accomplished.
        /// </summary>
        NewOrderReceived,

        /// <summary>
        /// The target has been determined to be uncatchable. 
        /// <remarks>4.15.17 Currently there are 3 sources of this Failure Cause.
        /// 1) A TgtFleet is progressively getting further away from our pursuing fleet,
        /// 2) our Cmd has lost awareness of the TgtFleet, aka our sensors can no longer detect it, and
        /// 3) the TgtShip of our pursuing ship has moved outside the distance envelope that our
        /// FleetCmd can support. While 3)'s approach was developed when SRSensors were located on Cmds, 
        /// it still is a useful way of keeping ship's on individual assignments from getting too far
        /// away from their FleetCmd.</remarks>
        /// </summary>
        TgtUncatchable,

        /// <summary>
        /// The target has been determined to be unreachable.
        /// <remarks>4.15.17 Currently this refers to the inability to plot a course to reach the target.</remarks>
        /// </summary>
        TgtUnreachable,

        /// <summary>
        /// The target has been determined to be unjoinable, aka does not have room to join.
        /// <remarks>11.21.17 Currently this refers to the inability to join a hanger after arriving close by.</remarks>
        /// <remarks>Could also be used by a fleet when it finds it can't join another fleet after arriving close by.
        /// This would require an order from PlayerAI that was looking for an order outcome callback.</remarks>
        /// </summary>
        TgtUnjoinable,

        /// <summary>
        /// Our relationship with the target has changed in a way that no longer allows execution of the current order.
        /// </summary>
        TgtRelationship,

        /// <summary>
        /// The target has died.
        /// </summary>
        TgtDeath,

        /// <summary>
        /// This Unit Cmd or Element needs repair.  
        /// </summary>
        NeedsRepair,

        /// <summary>
        /// This Unit Cmd or Element lacks or has lost the qualifications needed to execute the assignment.
        /// </summary>
        Disqualified,

        /// <summary>
        /// This Unit Element had construction rework occurring on it canceled.
        /// </summary>
        ConstructionCanceled,

        /// <summary>
        /// This Unit Cmd or Element's current owner is about to lose ownership.
        /// </summary>
        Ownership,

        /// <summary>
        /// This Unit Cmd or Element has died.
        /// </summary>
        Death

    }
}

