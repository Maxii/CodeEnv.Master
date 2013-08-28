// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Framerate.cs
// Temporary enum holding target framerate settings, a surrogate for quality levels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Temporary enum holding target framerate settings, a surrogate for quality levels.
    /// </summary>
    public enum Framerate {

        None,

        Minimum,

        Normal,

        Maximum

    }
}

