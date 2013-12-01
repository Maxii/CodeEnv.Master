// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraLOSChangedClient
// Interface used on a client that wants to know about any change in whether
// another object's mesh is in/out of the Line Of Sight of the main camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface used on a client that wants to know about any change in whether
    /// another object's mesh is in/out of the Line Of Sight of the main camera.
    /// <remarks>Commonly used on a parent GameObject that is separated from its mesh and renderer.</remarks>
    /// </summary>
    public interface ICameraLOSChangedClient {

        bool InCameraLOS { get; }

        void NotifyCameraLOSChanged(Transform sender, bool inLOS);

    }
}

