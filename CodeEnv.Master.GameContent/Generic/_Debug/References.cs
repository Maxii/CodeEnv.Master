// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: References.cs
// Simple source of useful static references to important MonoBehaviour interfaces.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Simple source of useful static references to important MonoBehaviour interfaces.
    /// Primary purpose is to allow the relocation of classes that need these references
    /// out of loose scripts.
    /// WARNING: These references should not be accessed from the using class's Awake()
    /// (or equivalent) method as they can be null during Awake() when a new scene is 
    /// loaded. This is because all new scene object Awake()s are called before these new
    /// references are established during OnSceneWasLoaded() in GameManager.
    /// </summary>
    public static class References {

        public static IGameManager GameManager { get; set; }

        public static ICameraControl CameraControl { get; set; }

        public static IDynamicObjects DynamicObjects { get; set; }

        public static IGameInputHelper InputHelper { get; set; }

    }
}


