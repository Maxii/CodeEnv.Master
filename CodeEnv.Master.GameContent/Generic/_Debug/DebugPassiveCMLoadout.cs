// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugPassiveCMLoadout.cs
// The desired Passive Countermeasure load of each element or command in the unit. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired Passive Countermeasure load of each element or command in the unit. 
    /// <remarks>Used for debug settings in the editor.</remarks> 
    /// </summary>
    public enum DebugPassiveCMLoadout {

        /// <summary>
        /// No Passive CMs will be carried by the element or command.
        /// </summary>
        None,

        /// <summary>
        /// One Passive CM will be carried by the element/command.
        /// </summary>
        One,

        /// <summary>
        /// The number of Passive CMs carried by the element/command will 
        /// be a random value between 0 and the maximum allowed by the element category or command, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of Passive CMs carried by the element/command will 
        /// be the maximum allowed by the element category or command.
        /// </summary>
        Max


    }
}

