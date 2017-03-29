// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AlertStatus.cs
// Unit Alert Status.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unit Alert Status.
    /// </summary>
    public enum AlertStatus {

        /// <summary>
        /// For error detection.
        /// </summary>
        None = 0,

        Normal = 1,

        Yellow = 2,

        Red = 3

    }
}

