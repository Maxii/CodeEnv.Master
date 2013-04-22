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

//#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
#define DEBUG_LOG


namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// SingletonPattern. Primary Game Manager 'God' class for the game.
    /// </summary>
    [SerializeAll]
    public class GameManager : AInstanceIdentity, IInstanceIdentity, IDisposable {

        public static GameState State { get; private set; }
        public static GameSettings Settings { get; private set; }
        public static bool IsGameRunning { get; private set; }
        /// <summary>
        /// To set use ProcessPauseRequest().
        /// </summary>
        /// <value>
        /// <c>true</c> if the game is paused; otherwise, <c>false</c>.
        /// </value>
        public static bool IsGamePaused { get; private set; }

        private GameEventManager eventMgr;
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
            get {
                return instance;
            }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Add initialization code here if any
            IncrementInstanceCounter();
            eventMgr = GameEventManager.Instance;
            AddEventListeners();
            gameTime = GameTime.Instance;
            playerPrefsMgr = PlayerPrefsManager.Instance;
        }

        #region Startup Simulation
        public void AwakeBasedOnStartScene(SceneLevel startScene) {
            switch (startScene) {
                case SceneLevel.IntroScene:
                    ChangeState(GameState.Lobby);
                    break;
                case SceneLevel.GameScene:
                    SimulateBuildGameFromLobby_Step1();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
            }
        }

        /// <summary>
        /// Temporary method that simulates the launch of a game from the Lobby, for use when
        /// starting in GameScene.
        /// </summary>
        private void SimulateBuildGameFromLobby_Step1() {
            State = GameState.Lobby;  // avoids the Illegal state transition Error
            Settings = new GameSettings {
                IsNewGame = true,
                SizeOfUniverse = UniverseSize.Normal,
                Player = Players.Opponent_1,
            };
            ChangeState(GameState.Building);
            ChangeState(GameState.Loading);
        }

        public void StartBasedOnStartScene(SceneLevel startScene) {
            switch (startScene) {
                case SceneLevel.IntroScene:
                    break;
                case SceneLevel.GameScene:
                    SimulateBuildGameFromLobby_Step2();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
            }
        }

        private void SimulateBuildGameFromLobby_Step2() {
            // GameState.Restoring only applies to loading saved games
            ChangeState(GameState.Waiting);
        }
        #endregion

        private void AddEventListeners() {
            eventMgr.AddListener<ExitGameEvent>(this, OnExitGame);
            eventMgr.AddListener<BuildNewGameEvent>(this, OnBuildNewGame);
            eventMgr.AddListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            eventMgr.AddListener<SceneLevelChangedEvent>(this, OnSceneLevelChanged);
            eventMgr.AddListener<SaveGameEvent>(this, OnSaveGame);
            eventMgr.AddListener<LoadSavedGameEvent>(this, OnLoadSavedGame);
        }

        private void OnBuildNewGame(BuildNewGameEvent e) {
            D.Log("BuildNewGameEvent received.");
            Settings = e.Settings;
            BuildAndLoadNewGame();
        }

        private void BuildAndLoadNewGame() {
            ChangeState(GameState.Building);
            // building the level begins here when implemented
            ChangeState(GameState.Loading);
            eventMgr.Raise<SceneLevelChangingEvent>(new SceneLevelChangingEvent(this, SceneLevel.GameScene));
            Application.LoadLevel((int)SceneLevel.GameScene);
        }

        private void OnSaveGame(SaveGameEvent e) {
            SaveGame(e.GameName);
        }

        private void SaveGame(string gameName) {
            Settings.IsNewGame = false;
            gameTime.PrepareToSaveGame();
            LevelSerializer.SaveGame(gameName);
        }

        private void OnLoadSavedGame(LoadSavedGameEvent e) {
            LoadAndRestoreSavedGame(e.GameID);
        }

        private void LoadAndRestoreSavedGame(string gameID) {
            var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
            var gamesWithID = from g in savedGames where g.Caption == gameID select g;
            if (gamesWithID.IsNullOrEmpty<LevelSerializer.SaveEntry>()) {
                D.Error("No saved game matches selected game caption {0}.", gameID);
                return;
            }

            // HACK to deal with multiple games with the same caption, ie. saved the same minute
            var idArray = gamesWithID.ToArray<LevelSerializer.SaveEntry>();
            LevelSerializer.SaveEntry selectedGame = null;
            if (idArray.Length > 1) {
                selectedGame = idArray[0];
            }
            else {
                selectedGame = gamesWithID.Single<LevelSerializer.SaveEntry>();
            }

            ChangeState(GameState.Building);
            ChangeState(GameState.Loading);
            // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
            eventMgr.Raise<SceneLevelChangingEvent>(new SceneLevelChangingEvent(this, SceneLevel.GameScene));
            selectedGame.Load();
        }

        // MonoGameManager raises this event when it receives the OnLevelWasLoaded
        private void OnSceneLevelChanged(SceneLevelChangedEvent e) {
            if (e.Level != SceneLevel.GameScene) {
                D.Error("A SceneLevel change to {0} is currently not implemented.", e.Level.GetName());
            }
            if (LevelSerializer.IsDeserializing || !Settings.IsNewGame) {
                ChangeState(GameState.Restoring);
            }
            else {
                PrepareForWaiting();
                gameTime.PrepareToBeginNewGame();
                ChangeState(GameState.Waiting);
            }
        }

        public void OnDeserialized() {  // Assumes PrepareToResumeSavedGame can only be called AFTER OnLevelWasLoaded
            D.Log("GameManager.PrepareToResumeSavedGame() called.");
            PrepareForWaiting();
            gameTime.PrepareToResumeSavedGame();
            ChangeState(GameState.Waiting);
        }

        private void OnGuiPauseChangeRequest(GuiPauseRequestEvent e) {
            D.Assert(IsGameRunning);
            ProcessPauseRequest(e.PauseRequest);
        }

        // flag indicating whether the current pause was requested directly by the user or program
        private bool _isPriorityPause;
        private void ProcessPauseRequest(PauseRequest request) {
            bool toPause = false;
            switch (request) {
                case PauseRequest.GuiAutoPause:
                    if (_isPriorityPause) { return; }
                    if (!IsGamePaused) {
                        toPause = true;
                    }
                    else {
                        D.Warn("Attempt to GuiAutoPause when already paused.");
                    }
                    break;
                case PauseRequest.GuiAutoResume:
                    if (_isPriorityPause) { return; }
                    if (IsGamePaused) {
                        toPause = false;
                    }
                    else {
                        D.Warn("Attempt to GuiAutoResume when not paused.");
                    }
                    break;
                case PauseRequest.PriorityPause:
                    if (!IsGamePaused) {
                        toPause = true;
                        _isPriorityPause = true;
                    }
                    else {
                        D.Warn("Attempt to PriorityPause when already paused.");
                    }
                    break;
                case PauseRequest.PriorityResume:
                    if (IsGamePaused) {
                        toPause = false;
                        _isPriorityPause = false;
                    }
                    else {
                        D.Warn("Atttempt to PriorityResume when not paused.");
                    }
                    break;
                case PauseRequest.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(request));
            }

            GamePauseState newPauseState = toPause ? GamePauseState.Paused : GamePauseState.Resumed;
            eventMgr.Raise<GamePauseStateChangingEvent>(new GamePauseStateChangingEvent(Instance, newPauseState));
            IsGamePaused = toPause;
            eventMgr.Raise<GamePauseStateChangedEvent>(new GamePauseStateChangedEvent(Instance, newPauseState));
        }

        /// <summary>
        /// Called from Loader when all conditions are met to run.
        /// Conditions include GameState.Waiting, no UnreadyElements and Update()
        /// has started.
        /// </summary>
        public void Run() {
            ChangeState(GameState.Running);
        }

        public void ChangeState(GameState newState) {
            CheckForErrorsPriorToStateChange(newState);
            InitializeNewState(newState);
        }

        [Conditional("UNITY_EDITOR")]
        private void CheckForErrorsPriorToStateChange(GameState newState) {
            bool isError = false;
            switch (State) {
                case GameState.None:
                    if (newState != GameState.Lobby) { isError = true; }
                    break;
                case GameState.Lobby:
                    if (newState != GameState.Building) { isError = true; }
                    break;
                case GameState.Building:
                    if (newState != GameState.Loading) { isError = true; }
                    break;
                case GameState.Loading:
                    if (newState != GameState.Restoring && newState != GameState.Waiting) { isError = true; }
                    break;
                case GameState.Restoring:
                    if (newState != GameState.Waiting) { isError = true; }
                    break;
                case GameState.Waiting:
                    if (newState != GameState.Running) { isError = true; }
                    break;
                case GameState.Running:
                    if (newState != GameState.Lobby && newState != GameState.Building) { isError = true; }
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(State));
            }
            if (isError) {
                D.Error("Erroneous GameState transition. Current State = {0}, proposed State = {1}.", State, newState);
            }
        }

        private void InitializeNewState(GameState newState) {
            eventMgr.Raise<GameStateChangingEvent>(new GameStateChangingEvent(this, newState));
            State = newState;
            D.Log("GameState is now {0}.", newState);
            IsGameRunning = false;
            switch (newState) {
                case GameState.Lobby:
                    break;
                case GameState.Building:
                    break;
                case GameState.Loading:
                    break;
                case GameState.Restoring:
                    break;
                case GameState.Waiting:
                    break;
                case GameState.Running:
                    IsGameRunning = true;
                    gameTime.EnableClock(true);
                    if (playerPrefsMgr.IsPauseOnLoadEnabled) {
                        ProcessPauseRequest(PauseRequest.PriorityPause);
                    }
                    break;
                case GameState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
            }
            eventMgr.Raise<GameStateChangedEvent>(new GameStateChangedEvent(this, newState));
        }

        private void PrepareForWaiting() {
            if (IsGamePaused) {
                ProcessPauseRequest(PauseRequest.PriorityResume);
            }
            _isPriorityPause = false;
        }

        private void OnExitGame(ExitGameEvent e) {
            Shutdown();
        }

        private void Shutdown() {
            playerPrefsMgr.Store();
            if (Application.isEditor || Application.isWebPlayer) {
                D.Log("Game Shutdown initiated in Editor or WebPlayer.");
                return;
            }
            // UNDONE MonoBehaviours will all have OnDestroy() called on Quit, but what about non-MonoBehaviours?
            // Should each use the ExitGameEvent to release their Listeners too?
            gameTime.Dispose();
            Dispose();
            Application.Quit(); // ignored inside Editor or WebPlayer
        }

        //private void CleanupDisposableObjects() {
        //    IList<IDisposable> disposableObjects = MonoBehaviourBase.FindObjectsOfInterface<IDisposable>();
        //    disposableObjects.ForAll<IDisposable>(d => d.Dispose());
        //}

        private void RemoveEventListeners() {
            eventMgr.RemoveListener<ExitGameEvent>(this, OnExitGame);
            eventMgr.RemoveListener<BuildNewGameEvent>(this, OnBuildNewGame);
            eventMgr.RemoveListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            eventMgr.RemoveListener<SceneLevelChangedEvent>(this, OnSceneLevelChanged);
            eventMgr.RemoveListener<SaveGameEvent>(this, OnSaveGame);
            eventMgr.RemoveListener<LoadSavedGameEvent>(this, OnLoadSavedGame);
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


