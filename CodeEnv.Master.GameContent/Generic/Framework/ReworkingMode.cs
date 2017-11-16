// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ReworkingMode.cs
// Enum indicating the different kinds of rework that an element can undergo.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum indicating the different kinds of rework that an element can undergo.
    /// <remarks>Used primarily for control of in-game graphics along with UI icons and buttons.</remarks>
    /// </summary>
    public enum ReworkingMode {

        None, // not just for error detection
        Constructing,
        Disbanding,
        Refitting,
        Repairing

    }
}

