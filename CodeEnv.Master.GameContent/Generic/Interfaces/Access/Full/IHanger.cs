// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHanger.cs
// Interface for easy access to the Hanger MonoBehaviour in Bases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to the Hanger MonoBehaviour in Bases.
    /// </summary>
    [System.Obsolete("Not currently used, pending Hanger becoming an AItem.")]
    public interface IHanger {

        string DebugName { get; }

    }
}

