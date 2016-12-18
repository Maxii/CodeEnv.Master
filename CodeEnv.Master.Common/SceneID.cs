// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SceneID.cs
// Enum containing both the name and index of a scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum containing both the name and index of a scene.
    /// </summary>
    public enum SceneID {
        // No None as Unity would require that there is a None scene set to 0 in build settings.
        LobbyScene = 0,
        GameScene = 1
    }

}

