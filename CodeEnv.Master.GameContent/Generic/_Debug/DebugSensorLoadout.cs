// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSensorLoadout.cs
// The desired sensor load of each element and/or command in the unit. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired sensor load of each element or command in the unit. 
    /// <remarks>Used for debug settings in the editor.</remarks> 
    /// </summary>
    public enum DebugSensorLoadout {

        /// <summary>
        /// One sensor will be carried by the element/command.
        /// </summary>
        One,

        /// <summary>
        /// The number of sensors carried by the element/command will 
        /// be a random value between 1 and the maximum allowed by the element category or command, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of sensors carried by the element/command will 
        /// be the maximum allowed by the element category or command.
        /// </summary>
        Max


    }
}

