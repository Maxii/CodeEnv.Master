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
        /// </summary>
        NewOrderReceived,

        /// <summary>
        /// The target has been determined to be uncatchable. 
        /// <remarks>Typically this means the target is moving faster than we can move,
        /// or we have lost awareness of the target, aka our sensors can no longer detect it.</remarks>
        /// </summary>
        TgtUncatchable,

        /// <summary>
        /// The target has been determined to be unreachable.
        /// <remarks>Typically this refers to the inability to plot a course to reach the target.</remarks>
        /// </summary>
        TgtUnreachable,

        /// <summary>
        /// Our relationship with the target has changed in a way that no longer
        /// allows execution of the current state.
        /// </summary>
        TgtRelationship,

        TgtDeath,

        /// <summary>
        /// This Unit Cmd or Element needs repair.  
        /// UNCLEAR Cmd or whole Unit?
        /// </summary>
        NeedsRepair,

        /// <summary>
        /// This Unit Cmd or Element has died.
        /// </summary>
        Death

    }

}

