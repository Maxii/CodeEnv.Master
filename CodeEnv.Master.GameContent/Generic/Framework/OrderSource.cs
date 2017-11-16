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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The originating source of an order.
    /// </summary>
    public enum OrderSource {

        None = 0,

        /// <summary>
        /// The Captain of a Unit Element.
        /// </summary>
        Captain = 1,

        /// <summary>
        /// The Command Staff of a Unit.
        /// </summary>
        CmdStaff = 2,

        /// <summary>
        /// The AI of the player including the User's AI.
        /// </summary>
        PlayerAI = 3,

        /// <summary>
        /// The User via the UI.
        /// </summary>
        User = 4

    }
}

