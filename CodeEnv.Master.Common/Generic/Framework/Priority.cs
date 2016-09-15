// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Priority.cs
// Ascending Order Priority.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Ascending Order Priority.
    /// </summary>
    public enum Priority {

        /// <summary>
        /// For error detection.
        /// </summary>
        None = 0,

        Low = 1,

        Tertiary = 2,

        Secondary = 3,

        Primary = 4

    }
}

