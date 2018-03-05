// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Level.cs
// A generic indication of significance where Two is more significant than One.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// A generic indication of significance where Two is more significant than One.
    /// </summary>
    public enum Level {

        /// <summary>
        /// For error detection.
        /// </summary>
        None = 0,

        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5

    }
}

