// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitItemOrderFailureCause.cs
// The possible causes of a UnitItem's failure to execute an Order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The possible causes of a UnitItem's failure to execute an Order.
    /// </summary>
    public enum UnitItemOrderFailureCause {

        None,

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
        UnitItemNeedsRepair,

        /// <summary>
        /// This Unit Cmd or Element has died.
        /// </summary>
        UnitItemDeath

    }
}

