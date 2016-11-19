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
        TgtUncatchable,
        TgtUnreachable,
        TgtRelationship,
        TgtDeath,
        UnitItemNeedsRepair,
        UnitItemDeath

    }
}

