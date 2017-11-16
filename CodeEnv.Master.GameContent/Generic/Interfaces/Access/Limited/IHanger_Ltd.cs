// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHanger_Ltd.cs
// Interface for easy but limited access to the Hanger MonoBehaviour in Bases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy but limited access to the Hanger MonoBehaviour in Bases.
    /// </summary>
    [System.Obsolete("Not currently used, pending Hanger becoming an AItem.")]
    public interface IHanger_Ltd {

        string DebugName { get; }

    }
}

