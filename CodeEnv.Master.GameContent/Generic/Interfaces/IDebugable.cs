// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugable.cs
// Base interface for objects that support debug logging on the Editor Console.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Base interface for objects that support debug logging on the Editor Console.
    /// </summary>
    public interface IDebugable {

        /// <summary>
        /// The full name of the object for debugging use on the editor console.
        /// </summary>
        string FullName { get; }

    }
}

