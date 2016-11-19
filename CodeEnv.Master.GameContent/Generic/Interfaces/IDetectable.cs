// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectable.cs
// Base Interface for GameObjects that are detectable by DectableRangeMonitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Base Interface for GameObjects that are detectable by DectableRangeMonitors.
    /// </summary>
    public interface IDetectable : IDebugable {

        bool IsOperational { get; }

        Vector3 Position { get; }


    }
}

