// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraLOSChangedRelay.cs
// Interface for easy access to CameraLOSChangedRelays.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to CameraLOSChangedRelays.
    /// </summary>
    [Obsolete]
    public interface ICameraLOSChangedRelay {

        void AddTarget(params Transform[] targets);

    }
}

