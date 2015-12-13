// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelFreshness.cs
// How current the IntelLevel held about an object is.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// How current the IntelLevel held about an object is.
    /// </summary>
    public enum IntelFreshness {

        None,

        OutOfDate,

        //Current,  // IMPROVE Install timer that auto moves from Current to OutOfDate?

        Realtime


    }
}

