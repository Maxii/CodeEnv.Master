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
    /// Simple source of useful static references to important Unity-compiled MonoBehaviour 
    /// and non-MonoBehaviour Singletons. Primary purpose is to allow the relocation of classes 
    /// that need these references out of loose scripts and into pre-compiled assemblies.
    /// 
    /// PATTERN NOTES: As a static class, References persists across scenes and so do its' static
    /// fields providing references to Singletons. Any reference to a Singleton that itself is 
    /// not persistent across scenes will need to refresh the References field assignment 
    /// when a new instance is created.
    ///     - Persistent MonoSingletons: 
    ///         - no issues as they all persist
    ///     - Non-persistent MonoSingletons: MainCameraControl, GuiCameraControl, DynamicObjectsFolder, SectorGrid, SphericalHighlight, 
    ///         Tooltip, MyPoolManager
    ///         - all need to refresh the reference on instantiation in InitializeOnInstance
    ///         - all should null the reference in Cleanup
    ///     - Non-persistent StdGenericSingletons: 
    ///         - all should implement IDisposable and call CallOnDispose from Cleanup to null _instance
    ///         - all should be disposed of and re-instantiated in GameManager.RefreshStaticReferences()
    /// 
    /// WARNING: These references should not be accessed from the using class's Awake()
    /// (or equivalent) method as they can be null during Awake() when a new scene is 
    /// loaded. This is because all new scene object Awake()s are called before these new
    /// references are established during OnSceneWasLoaded() in GameManager.
    /// 
    /// IMPROVE: Alternative ways of gaining these references without access to these loose
    /// scripts are 1) for non-MonoBehaviour classes use Constructor Dependency
    /// Injection - i.e. ClassConstructor(ICameraControl cameraCntl), 2) find the loose
    /// script instance gameObject using a tag or name, then access the instance using
    /// gameObject.GetInterface() and 3) use static Property Dependency Injection (like below),
    /// but on the class that has the dependency, rather than this intermediate class. This third
    /// alternative continues to have the restriction of not using the reference during Awake().
    /// </summary>
    public static class References {

        // Note: to add more references, see pattern notes above

        #region Persistent MonoBehaviour Singletons

        public static IGameManager GameManager { get; set; }
        public static IUsefulTools UsefulTools { get; set; }
        public static IInputManager InputManager { get; set; }
        public static ISFXManager SFXManager { get; set; }

        #endregion

        #region Non-persistent MonoBehaviour Singletons

        public static ICameraControl MainCameraControl { get; set; }
        public static IGuiCameraControl GuiCameraControl { get; set; }
        public static ISectorGrid SectorGrid { get; set; }
        public static ISphericalHighlight SphericalHighlight { get; set; }
        public static IDynamicObjectsFolder DynamicObjectsFolder { get; set; }
        public static ITooltipHudWindow TooltipHudWindow { get; set; }
        public static ISelectedItemHudWindow SelectedItemHudWindow { get; set; }
        public static IHoveredHudWindow HoveredItemHudWindow { get; set; }
        public static IMyPoolManager MyPoolManager { get; set; }

        #endregion

        #region Standard Generic Singletons

        public static IGeneralFactory GeneralFactory { get; set; }
        public static ITrackingWidgetFactory TrackingWidgetFactory { get; set; }
        public static IGameInputHelper InputHelper { get; set; }
        public static IFormationGenerator FormationGenerator { get; set; }

        #endregion

    }
}


