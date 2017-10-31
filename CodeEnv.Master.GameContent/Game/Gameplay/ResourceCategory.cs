// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceCategory.cs
// Resource category associated with a ResourceID.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Resource category associated with a ResourceID.
    /// </summary>
    public enum ResourceCategory {

        /// <summary>
        /// Used for error detection.
        /// </summary>
        None = 0,

        Common = 1,

        Strategic = 2,

        Luxury = 3,

        All = 4

    }
}

