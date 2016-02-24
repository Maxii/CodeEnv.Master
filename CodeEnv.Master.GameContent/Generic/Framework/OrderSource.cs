// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrderSource.cs
// The originating source of an order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The originating source of an order.
    /// </summary>
    public enum OrderSource {

        None,

        /// <summary>
        /// The Captain of a Unit Element.
        /// </summary>
        Captain,

        /// <summary>
        /// The Command Staff of a Unit.
        /// </summary>
        CmdStaff,

        /// <summary>
        /// The User.
        /// </summary>
        User

    }
}

