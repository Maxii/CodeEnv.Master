// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectable.cs
// Base Interface for objects that are detectable by Monitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Base Interface for objects that are detectable by Monitors.
    /// </summary>
    public interface IDetectable {

        Player Owner { get; }

        bool IsOperational { get; }

        string FullName { get; }

        Vector3 Position { get; }


    }
}

