// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugControls.cs
// Interface for access to the DebugControls MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;

    /// <summary>
    /// Interface for access to the DebugControls MonoBehaviour.
    /// </summary>
    public interface IDebugControls {

        event EventHandler validatePlayerKnowledgeNow;

        UniverseSize UniverseSize { get; }

    }
}

