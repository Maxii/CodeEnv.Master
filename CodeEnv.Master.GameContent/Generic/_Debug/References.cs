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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
    ///     - Non-persistent MonoSingletons:
    ///         - all need to refresh the reference on instantiation in InitializeOnInstance
    ///         - all should null the reference in Cleanup
    ///     - Non-persistent StdGenericSingletons: 
    ///         - all should implement IDisposable and call CallOnDispose from Cleanup to null _instance
    ///         - all should be disposed of and re-instantiated in GameManager.RefreshStaticReferences()
    /// 
    /// WARNING: Non-persistent references should not be accessed from the using class's Awake()
    /// (or equivalent) method as they can be null during Awake() when a new scene is 
    /// loaded. This is because all new scene object Awake()s are called before these new
    /// references are re-established during OnSceneWasLoaded() in GameManager.
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

        private static IGameManager _gameManager;
        public static IGameManager GameManager {
            get {
                D.AssertNotNull(_gameManager);
                return _gameManager;
            }
            set { _gameManager = value; }
        }

        private static IInputManager _inputManager;
        public static IInputManager InputManager {
            get {
                D.AssertNotNull(_inputManager);
                return _inputManager;
            }
            set { _inputManager = value; }
        }

        private static ISFXManager _sfxManager;
        public static ISFXManager SFXManager {
            get {
                D.AssertNotNull(_sfxManager);
                return _sfxManager;
            }
            set { _sfxManager = value; }
        }

        private static IJobManager _jobManager;
        public static IJobManager JobManager {
            get {
                D.AssertNotNull(_jobManager);
                return _jobManager;
            }
            set { _jobManager = value; }
        }

        private static IGameSettingsDebugControl _gameSettingsDebugControl;
        public static IGameSettingsDebugControl GameSettingsDebugControl {
            get {
                D.AssertNotNull(_gameSettingsDebugControl);
                return _gameSettingsDebugControl;
            }
            set { _gameSettingsDebugControl = value; }
        }

        #endregion

        #region Non-persistent MonoBehaviour Singletons

        private static ICameraControl _mainCameraControl;
        public static ICameraControl MainCameraControl {
            get {
                D.AssertNotNull(_mainCameraControl);
                return _mainCameraControl;
            }
            set { _mainCameraControl = value; }
        }

        private static IGuiCameraControl _guiCameraControl;
        public static IGuiCameraControl GuiCameraControl {
            get {
                D.AssertNotNull(_guiCameraControl);
                return _guiCameraControl;
            }
            set { _guiCameraControl = value; }
        }

        private static ISectorGrid _sectorGrid;
        public static ISectorGrid SectorGrid {
            get {
                D.AssertNotNull(_sectorGrid);
                return _sectorGrid;
            }
            set { _sectorGrid = value; }
        }

        private static ISphericalHighlight _hoverHighlight;
        public static ISphericalHighlight HoverHighlight {
            get {
                D.AssertNotNull(_hoverHighlight);
                return _hoverHighlight;
            }
            set { _hoverHighlight = value; }
        }

        private static IDynamicObjectsFolder _dynamicObjectsFolder;
        public static IDynamicObjectsFolder DynamicObjectsFolder {
            get {
                D.AssertNotNull(_dynamicObjectsFolder);
                return _dynamicObjectsFolder;
            }
            set { _dynamicObjectsFolder = value; }
        }

        private static ITooltipHudWindow _tooltipHudWindow;
        public static ITooltipHudWindow TooltipHudWindow {
            get {
                D.AssertNotNull(_tooltipHudWindow);
                return _tooltipHudWindow;
            }
            set { _tooltipHudWindow = value; }
        }

        private static ISelectedItemHudWindow _selectedItemHudWindow;
        public static ISelectedItemHudWindow SelectedItemHudWindow {
            get {
                D.AssertNotNull(_selectedItemHudWindow);
                return _selectedItemHudWindow;
            }
            set { _selectedItemHudWindow = value; }
        }

        private static IHoveredHudWindow _hoveredItemHudWindow;
        public static IHoveredHudWindow HoveredItemHudWindow {
            get {
                D.AssertNotNull(_hoveredItemHudWindow);
                return _hoveredItemHudWindow;
            }
            set { _hoveredItemHudWindow = value; }
        }

        [System.Obsolete]
        private static IMyPoolManager _myPoolManager;
        [System.Obsolete]
        public static IMyPoolManager MyPoolManager {
            get {
                D.AssertNotNull(_myPoolManager);
                return _myPoolManager;
            }
            set { _myPoolManager = value; }
        }

        private static IDebugControls _debugControls;
        public static IDebugControls DebugControls {
            get {
                D.AssertNotNull(_debugControls);
                return _debugControls;
            }
            set { _debugControls = value; }
        }

        private static IGamePoolManager _gamePoolManager;
        public static IGamePoolManager GamePoolManager {
            get {
                D.AssertNotNull(_gamePoolManager);
                return _gamePoolManager;
            }
            set { _gamePoolManager = value; }
        }

        // A 'defacto' Singleton as there is only 1 per scene
        // I could make it a singleton, but it would take work as it : AGuiLabelReadout
        private static IFpsReadout _fpsReadout;
        public static IFpsReadout FpsReadout {
            get {
                D.AssertNotNull(_fpsReadout);
                return _fpsReadout;
            }
            set { _fpsReadout = value; }
        }

        #endregion

        #region Standard Generic Singletons

        private static IGeneralFactory _generalFactory;
        public static IGeneralFactory GeneralFactory {
            get {
                D.AssertNotNull(_generalFactory);
                return _generalFactory;
            }
            set { _generalFactory = value; }
        }

        private static ITrackingWidgetFactory _trackingWidgetFactory;
        public static ITrackingWidgetFactory TrackingWidgetFactory {
            get {
                D.AssertNotNull(_trackingWidgetFactory);
                return _trackingWidgetFactory;
            }
            set { _trackingWidgetFactory = value; }
        }

        private static IGameInputHelper _inputHelper;
        public static IGameInputHelper InputHelper {
            get {
                D.AssertNotNull(_inputHelper);
                return _inputHelper;
            }
            set { _inputHelper = value; }
        }

        private static IFormationGenerator _formationGenerator;
        public static IFormationGenerator FormationGenerator {
            get {
                D.AssertNotNull(_formationGenerator);
                return _formationGenerator;
            }
            set { _formationGenerator = value; }
        }

        #endregion

    }
}


