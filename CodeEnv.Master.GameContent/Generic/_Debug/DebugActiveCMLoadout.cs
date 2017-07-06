// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugActiveCMLoadout.cs
// The desired Active Countermeasure load of each element in the unit. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired Active Countermeasure load of each element in the unit. 
    /// <remarks>Used for debug settings in the editor.</remarks> 
    /// </summary>
    public enum DebugActiveCMLoadout {

        /// <summary>
        /// No Active CMs will be carried by the element.
        /// </summary>
        None,

        /// <summary>
        /// One Active CM will be carried by the element.
        /// </summary>
        One,

        /// <summary>
        /// The number of Active CMs carried by the element will 
        /// be a random value between 0 and the maximum allowed by the element category, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of Active CMs carried by the element will 
        /// be the maximum allowed by the element category.
        /// </summary>
        Max
    }
}

