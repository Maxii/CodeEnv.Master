// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager.cs
// SingletonPattern.  Primary Game Manager 'God' class for the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    using UnityEngine;

    /// <summary>
    /// SingletonPattern. Primary Game Manager 'God' class for the game.
    /// </summary>
    public class GameManager : IDisposable {

        private static bool isGamePaused;
        public static bool IsGamePaused {
            get { return isGamePaused; }
            set {
                isGamePaused = value;
                PauseGameCommand pauseCmd = value ? PauseGameCommand.Pause : PauseGameCommand.Resume;
                eventMgr.Raise<PauseGameEvent>(new PauseGameEvent(pauseCmd));
            }
        }

        public UniverseSize UniverseSize { get; set; }

        public GameState State { get; private set; }

        private static GameEventManager eventMgr;
        private GameTime gameTime;
        private PlayerPrefsManager playerPrefsMgr;

        #region SingletonPattern
        private static readonly GameManager instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameManager() {
            // try, catch and resolve any possible exceptions here
            instance = new GameManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameManager"/>.
        /// </summary>
        private GameManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameManager Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Add initialization code here if any
            eventMgr = GameEventManager.Instance;
            AddEventListeners();
            gameTime = GameTime.Instance;
            playerPrefsMgr = PlayerPrefsManager.Instance;
            ChangeState(GameState.PreGame);
            // I will probably need to be able to force the PauseGameEvent to occur at initialization
            IsGamePaused = playerPrefsMgr.IsPauseOnLoadEnabled;
            // TODO UniverseSize will need to be acquired from New Game Settings
            // the New Game Settings Gui itself will acquire its initial setting from Prefs
            UniverseSize = playerPrefsMgr.SizeOfUniverse;
        }

        private void AddEventListeners() {
            eventMgr.AddListener<ExitGameEvent>(OnExitGame);
            eventMgr.AddListener<LaunchNewGameEvent>(OnNewGame);
            eventMgr.AddListener<GuiPauseEvent>(OnGuiPause);
            eventMgr.AddListener<GameLoadedEvent>(OnGameLoaded);
        }

        private void OnGameLoaded(GameLoadedEvent e) {
            IsGamePaused = playerPrefsMgr.IsPauseOnLoadEnabled;
            ChangeState(GameState.Running);
        }

        // flag indicating whether the current pause was requested directly by a user
        private bool userPauseFlag;
        private void OnGuiPause(GuiPauseEvent e) {
            if (State != GameState.Running) { return; }
            switch (e.PauseCommand) {
                case GuiPauseCommand.GuiAutoPause:
                    if (userPauseFlag) { return; }
                    IsGamePaused = true;
                    break;
                case GuiPauseCommand.GuiAutoResume:
                    if (userPauseFlag) { return; }
                    IsGamePaused = false;
                    break;
                case GuiPauseCommand.UserPause:
                    IsGamePaused = true;
                    userPauseFlag = true;
                    break;
                case GuiPauseCommand.UserResume:
                    IsGamePaused = false;
                    userPauseFlag = false;
                    break;
                case GuiPauseCommand.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.PauseCommand));
            }
        }

        public void InitializeUniverseEdge(Transform parentFolder) {
            string universeEdgeName = Layers.UniverseEdge.GetName();
            GameObject universeEdgeGo = GameObject.Find(universeEdgeName);
            if (universeEdgeGo == null) {
                universeEdgeGo = new GameObject(universeEdgeName);
                // a new GameObject automatically starts enabled, with only a transform located at the origin
                SphereCollider universeEdgeCollider = universeEdgeGo.AddComponent<SphereCollider>();
                // adding a component like SphereCollider starts enabled
                universeEdgeCollider.radius = UniverseSize.GetUniverseRadius();
                //universeEdgeCollider.isTrigger = true;
            }
            universeEdgeGo.layer = (int)Layers.UniverseEdge;
            universeEdgeGo.isStatic = true;
            universeEdgeGo.transform.parent = parentFolder;
            //Debug.Log("UniverseEdge created.");
        }

        private void OnExitGame(ExitGameEvent e) {
            Shutdown();
        }

        private void OnNewGame(LaunchNewGameEvent e) {
            //Debug.Log("New Game Event received. Universe Size = " + e.GameSettings.SizeOfUniverse);
            ChangeState(GameState.Building);
            eventMgr.Raise<SceneLevelChangingEvent>(new SceneLevelChangingEvent(SceneLevel.GameScene));
            Application.LoadLevel((int)SceneLevel.GameScene);
        }

        public void ChangeState(GameState newState) {
            CheckForErrorsPriorToStateChange(newState);
            InitializeNewState(newState);
            eventMgr.Raise<GameStateChangeEvent>(new GameStateChangeEvent(newState));
        }

        private void CheckForErrorsPriorToStateChange(GameState newState) {
            bool isError = false;
            switch (State) {
                case GameState.None:
                    if (newState != GameState.PreGame) { isError = true; }
                    break;
                case GameState.PreGame:
                    if (newState != GameState.Building) { isError = true; }
                    break;
                case GameState.Building:
                    if (newState != GameState.WaitingForPlayers) { isError = true; }
                    break;
                case GameState.WaitingForPlayers:
                    if (newState != GameState.Launching) { isError = true; }
                    break;
                case GameState.Launching:
                    if (newState != GameState.Running) { isError = true; }
                    break;
                case GameState.Running:
                    if (newState != GameState.PreGame) { isError = true; }
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(State));
            }
            if (isError) {
                Debug.LogError("Erroneous GameState transition. Current State = {0}, proposed State = {1}.".Inject(State, newState));
            }
        }

        private void InitializeNewState(GameState newState) {
            State = newState;
            switch (newState) {
                case GameState.PreGame:

                    break;
                case GameState.Building:

                    break;
                case GameState.WaitingForPlayers:

                    break;
                case GameState.Launching:
                    eventMgr.Raise<GameLoadedEvent>(new GameLoadedEvent());
                    break;
                case GameState.Running:

                    break;
                case GameState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
            }
        }


        /// <summary>
        /// Temporary method that automatically progresses the GameState through
        /// unimplemented states.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void CheckGameStateProgression() {
            switch (State) {
                case GameState.None:
                case GameState.PreGame:
                case GameState.Launching:
                case GameState.Running:
                    // do nothing, progression handled by events
                    break;
                case GameState.Building:
                    ChangeState(GameState.WaitingForPlayers);
                    break;
                case GameState.WaitingForPlayers:
                    ChangeState(GameState.Launching);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(State));
            }
        }


        // UNDONE
        public void Shutdown() {
            playerPrefsMgr.Store();
            if (Application.isEditor || Application.isWebPlayer) {
                Debug.Log("Game Shutdown initiated in Editor or WebPlayer.");
                return;
            }
            CleanupDisposableObjects();
            Application.Quit(); // ignored inside Editor or WebPlayer
        }

        private void CleanupDisposableObjects() {
            IList<IDisposable> disposableObjects = MonoBehaviourBase.FindObjectsOfInterface<IDisposable>();
            disposableObjects.ForAll<IDisposable>(d => d.Dispose());
        }

        private void RemoveEventListeners() {
            eventMgr.RemoveListener<ExitGameEvent>(OnExitGame);
            eventMgr.RemoveListener<LaunchNewGameEvent>(OnNewGame);
        }

        #region IDisposable
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                RemoveEventListeners();
            }
            // free unmanaged resources here
            alreadyDisposed = true;
        }


        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


