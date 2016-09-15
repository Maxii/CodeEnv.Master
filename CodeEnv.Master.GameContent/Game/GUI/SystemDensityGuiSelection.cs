// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDensityGuiSelection.cs
// The System Density choices that can be selected from the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The System Density choices that can be selected from the Gui.
    /// </summary>
    public enum SystemDensityGuiSelection {

        /// <summary>
        /// Error detection.
        /// </summary>
        None,

        Random,

        /// <summary>
        /// Very few sectors will have a system.
        /// </summary>
        Sparse,

        /// <summary>
        /// A few sectors will have a system.
        /// </summary>
        Low,

        /// <summary>
        /// Roughly one in seven sectors will have a system.
        /// </summary>
        Normal,

        /// <summary>
        /// Quite a few sectors will have a system.
        /// </summary>
        High,

        /// <summary>
        /// A lot of sectors will have a system.
        /// </summary>
        Dense,


    }
}

