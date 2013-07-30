// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager.cs
// Primary Game Manager 'God' class for the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR
#define DEBUG_LOG


namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;


    /// <summary>
    /// SingletonPattern. Primary Game Manager 'God' class for the game.
    /// </summary>
    [SerializeAll]
    public class GameManager : APropertyChangeTracking, IInstanceIdentity, IDisposable {

        public HumanPlayer HumanPlayer { get; private set; }

        private GameState _gameState;
        public GameState GameState {
            get { return _gameState; }
            private set { SetProperty<GameState>(ref _gameState, value, "GameState", InitializeOnGameStateChanged, ValidateConditionsForChangeInGameState); }
        }

        public static GameSettings Settings { get; private set; }

        private bool _isGameRunning;
        public bool IsGameRunning {
            get { return _isGameRunning; }
            private set { SetProperty<bool>(ref _isGameRunning, value, "IsGameRunning"); }
        }

        /// <summary>
        /// To set use ProcessPauseRequest().
        /// </summary>
        /// <sb>
        /// <c>true</c> if the game is paused; otherwise, <c>false</c>.
        /// </sb>
        private bool _isGamePaused;
        public bool IsGamePaused {
            get {
                return _isGamePaused;
            }
            private set {
                SetProperty<bool>(ref _isGamePaused, value, "IsGamePaused");
            }
        }

        private GameEventManager _eventMgr;
        private GameTime _gameTime;
        private PlayerPrefsManager _playerPrefsMgr;

        #region SingletonPattern
        private static readonly GameManager _instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameManager() {
            // try, catch and resolve any possible exceptions here
            _instance = new GameManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameManager"/>.
        /// </summary>
        private GameManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameManager Instance {
            get { return _instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Warning! Instance is null until after this is complete!
            IncrementInstanceCounter();
            _eventMgr = GameEventManager.Instance;
            _playerPrefsMgr = PlayerPrefsManager.Instance;
        }

        public void CompleteInitialization() {
            Subscribe();    // delay until Instance is initialized
            _gameTime = GameTime.Instance;   // delay until Instance is initialized
        }

        #region Startup Simulation
        public void __AwakeBasedOnStartScene(SceneLevel startScene) {
            switch (startScene) {
                case SceneLevel.IntroScene:
                    GameState = GameState.Lobby;
                    break;
                case SceneLevel.GameScene:
                    __SimulateBuildGameFromLobby_Step1();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
            }
        }

        /// <summary>
        /// Temporary method that simulates the launch of a game from the Lobby, for use when
        /// starting in GameScene.
        /// </summary>
        private void __SimulateBuildGameFromLobby_Step1() {
            GameState = GameState.Lobby;  // avoids the Illegal state transition Error
            GameState = GameState.Building;
            GameSettings settings = new GameSettings {
                IsNewGame = true,
                UniverseSize = _playerPrefsMgr.UniverseSize,
                PlayerRace = new Race(new RaceStat(_playerPrefsMgr.PlayerRace, "Maxii", new StringBuilder("Maxii description"), _playerPrefsMgr.PlayerColor))
            };
            Settings = settings;
            HumanPlayer = CreateHumanPlayer(settings);
            GameState = GameState.Loading;
        }

        public void StartBasedOnStartScene(SceneLevel startScene) {
            switch (startScene) {
                case SceneLevel.IntroScene:
                    break;
                case SceneLevel.GameScene:
                    __SimulateBuildGameFromLobby_Step2();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
            }
        }

        private void __SimulateBuildGameFromLobby_Step2() {
            // GameState.Restoring only applies to loading saved games
            GameState = GameState.Waiting;
        }
        #endregion

        private void Subscribe() {
            _eventMgr.AddListener<ExitGameEvent>(this, OnExitGame);
            _eventMgr.AddListener<BuildNewGameEvent>(this, OnBuildNewGame);
            _eventMgr.AddListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            _eventMgr.AddListener<SaveGameEvent>(this, OnSaveGame);
            _eventMgr.AddListener<LoadSavedGameEvent>(this, OnLoadSavedGame);
        }

        private void OnBuildNewGame(BuildNewGameEvent e) {
            D.Log("BuildNewGameEvent received.");
            BuildAndLoadNewGame(e.Settings);
        }

        private void BuildAndLoadNewGame(GameSettings settings) {
            GameState = GameState.Building;
            // building the level begins here when implemented
            Settings = settings;
            HumanPlayer = CreateHumanPlayer(settings);

            GameState = GameState.Loading;
            // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
            _eventMgr.Raise<SceneChangingEvent>(new SceneChangingEvent(this, SceneLevel.GameScene));
            Application.LoadLevel((int)SceneLevel.GameScene);
        }

        private HumanPlayer CreateHumanPlayer(GameSettings gameSettings) {
            HumanPlayer humanPlayer = new HumanPlayer(gameSettings.PlayerRace);
            return humanPlayer;
        }

        private void OnSaveGame(SaveGameEvent e) {
            SaveGame(e.GameName);
        }

        private void SaveGame(string gameName) {
            Settings.IsNewGame = false;
            _gameTime.PrepareToSaveGame();
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

            GameState = GameState.Building;
            GameState = GameState.Loading;
            // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
            _eventMgr.Raise<SceneChangingEvent>(new SceneChangingEvent(this, SceneLevel.GameScene));
            selectedGame.Load();
        }

        //MonoGameManager relays the scene value that was loaded when it receives OnLevelWasLoaded
        public void OnSceneChanged(SceneLevel newScene) {
            if (newScene != SceneLevel.GameScene) {
                D.Error("A Scene change to {0} is currently not implemented.", newScene.GetName());
                return;
            }
            _eventMgr.Raise<SceneChangedEvent>(new SceneChangedEvent(this, newScene));
            if (LevelSerializer.IsDeserializing || !Settings.IsNewGame) {
                GameState = GameState.Restoring;
            }
            else {
                ResetConditionsForGameStartup();
                _gameTime.PrepareToBeginNewGame();
                GameState = GameState.Waiting;
            }
        }

        public void OnDeserialized() {  // Assumes PrepareToResumeSavedGame can only be called AFTER OnLevelWasLoaded
            D.Log("GameManager.OnDeserialized() called.");
            ResetConditionsForGameStartup();
            _gameTime.PrepareToResumeSavedGame();
            GameState = GameState.Waiting;
        }

        private void OnGuiPauseChangeRequest(GuiPauseRequestEvent e) {
            D.Assert(IsGameRunning);
            ProcessPauseRequest(e.NewValue);
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
            IsGamePaused = toPause;
        }

        /// <summary>
        /// Called from Loader when all conditions are met to run.
        /// Conditions include GameState.Waiting, no UnreadyElements and Update()
        /// has started.
        /// </summary>
        public void Run() {
            GameState = GameState.Running;
        }

        //[Conditional("UNITY_EDITOR")]
        private void ValidateConditionsForChangeInGameState(GameState proposedNewState) {
            bool isError = false;
            switch (GameState) {
                case GameState.None:
                    if (proposedNewState != GameState.Lobby) { isError = true; }
                    break;
                case GameState.Lobby:
                    if (proposedNewState != GameState.Building) { isError = true; }
                    break;
                case GameState.Building:
                    if (proposedNewState != GameState.Loading) { isError = true; }
                    break;
                case GameState.Loading:
                    if (proposedNewState != GameState.Restoring && proposedNewState != GameState.Waiting) { isError = true; }
                    break;
                case GameState.Restoring:
                    if (proposedNewState != GameState.Waiting) { isError = true; }
                    break;
                case GameState.Waiting:
                    if (proposedNewState != GameState.Running) { isError = true; }
                    break;
                case GameState.Running:
                    if (proposedNewState != GameState.Lobby && proposedNewState != GameState.Building) { isError = true; }
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(GameState));
            }
            if (isError) {
                D.Error("Erroneous GameState transition. Current State = {0}, proposed State = {1}.", GameState, proposedNewState);
            }
        }

        private void InitializeOnGameStateChanged() {
            switch (GameState) {
                case GameState.Lobby:
                case GameState.Building:
                case GameState.Loading:
                case GameState.Restoring:
                case GameState.Waiting:
                    IsGameRunning = false;
                    break;
                case GameState.Running:
                    IsGameRunning = true;
                    _gameTime.EnableClock(true);
                    if (_playerPrefsMgr.IsPauseOnLoadEnabled) {
                        ProcessPauseRequest(PauseRequest.PriorityPause);
                    }
                    break;
                case GameState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(GameState));
            }
            D.Log("GameState changed to {0}.", GameState.GetName());
        }

        /// <summary>
        /// Resets any conditions required for normal game startup. For instance, IsGamePaused
        /// is normally false while setting up the first game. This may or maynot be the
        /// current state of IsgamePaused given the numerous ways one can initiate the startup
        /// of a game instance.
        /// </summary>
        private void ResetConditionsForGameStartup() {
            if (IsGamePaused) {
                ProcessPauseRequest(PauseRequest.PriorityResume);
            }
            _isPriorityPause = false;
        }

        private void OnExitGame(ExitGameEvent e) {
            Shutdown();
        }

        private void Shutdown() {
            _playerPrefsMgr.Store();
            if (Application.isEditor || Application.isWebPlayer) {
                D.Log("Game Shutdown initiated in Editor or WebPlayer.");
                return;
            }
            // UNDONE MonoBehaviours will all have OnDestroy() called on Quit, but what about non-MonoBehaviours?
            // Should each use the ExitGameEvent to release their Listeners too?
            _gameTime.Dispose();
            Dispose();
            Application.Quit(); // ignored inside Editor or WebPlayer
        }

        private void Unsubscribe() {
            _eventMgr.RemoveListener<ExitGameEvent>(this, OnExitGame);
            _eventMgr.RemoveListener<BuildNewGameEvent>(this, OnBuildNewGame);
            _eventMgr.RemoveListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            _eventMgr.RemoveListener<SaveGameEvent>(this, OnSaveGame);
            _eventMgr.RemoveListener<LoadSavedGameEvent>(this, OnLoadSavedGame);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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
                Unsubscribe();
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

        #region IInstanceIdentity Members

        private static int instanceCounter = 0;
        public int InstanceID { get; set; }

        protected void IncrementInstanceCounter() {
            InstanceID = System.Threading.Interlocked.Increment(ref instanceCounter);
        }

        #endregion
    }
}


