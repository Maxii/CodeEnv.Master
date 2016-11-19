// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDensity.cs
// Enum indicating the density of deployed Systems in the Universe in a new game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum indicating the density of deployed Systems in the Universe in a new game.
    /// </summary>
    public enum SystemDensity {

        /// <summary>
        /// No systems in the Universe.
        /// </summary>
        None,

        /// <summary>
        /// Roughly 1% of deployable sectors will have a system.
        /// </summary>
        Sparse,

        /// <summary>
        /// Roughly 5% of deployable sectors will have a system.
        /// </summary>
        Low,

        /// <summary>
        /// Roughly 15% of deployable sectors will have a system.
        /// </summary>
        Normal,

        /// <summary>
        /// Roughly 25% of deployable sectors will have a system.
        /// </summary>
        High,

        /// <summary>
        /// Roughly 40% of deployable sectors will have a system.
        /// </summary>
        Dense,

    }
}

