// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorDetector.cs
// Interface for Items that have the ability to detect ISensorDetectable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that have the ability to detect ISensorDetectable Items.
    /// </summary>
    public interface ISensorDetector {

        string DebugName { get; }

        Player Owner { get; }
    }
}

