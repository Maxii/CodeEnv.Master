// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: References.cs
// Simple source of useful static references to important Unity-compiled MonoBehaviour scripts.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Simple source of useful static references to important Unity-compiled MonoBehaviour scripts.
    /// Primary purpose is to allow the relocation of classes that need these references
    /// out of loose scripts and into pre-compiled assemblies.
    /// 
    /// WARNING: These references should not be accessed from the using class's Awake()
    /// (or equivalent) method as they can be null during Awake() when a new scene is 
    /// loaded. This is because all new scene object Awake()s are called before these new
    /// references are established during OnSceneWasLoaded() in GameManager.
    /// 
    /// IMPROVE: Alternative ways of gaining these references without access to these loose
    /// scripts are 1) for non-MonoBehaviour classes use Constructor Dependency
    /// Injection - ie. ClassConstructor(ICameraControl cameraCntl), 2) find the loose
    /// script instance gameObject using a tag or name, then access the instance using
    /// gameObject.GetInterface() and 3) use static Property Dependency Injection (like below),
    /// but on the class that has the dependency, rather than this intermediate class. This third
    /// alternative continues to have the restriction of not using the reference during Awake().
    /// </summary>
    public static class References {

        public static IGameManager GameManager { get; set; }

        public static ICameraControl MainCameraControl { get; set; }

        public static IDynamicObjectsFolder DynamicObjectsFolder { get; set; }

        public static IGameInputHelper InputHelper { get; set; }

        public static IGeneralFactory GeneralFactory { get; set; }

        public static IUsefulTools UsefulTools { get; set; }

        public static ISectorGrid SectorGrid { get; set; }

        public static IInputManager InputManager { get; set; }

        public static ISphericalHighlight SphericalHighlight { get; set; }

    }
}


