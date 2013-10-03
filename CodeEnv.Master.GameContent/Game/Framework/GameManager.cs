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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

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
            private set { SetProperty<GameState>(ref _gameState, value, "GameState", OnGameStateChanged, OnGameStateChanging); }
        }

        public static GameSettings Settings { get; private set; }

        private bool _isGameRunning;
        public bool IsGameRunning {
            get { return _isGameRunning; }
            private set { SetProperty<bool>(ref _isGameRunning, value, "IsGameRunning"); }
        }

        private PauseState _pauseState;
        /// <summary>
        /// Gets the PauseState of the game. Warning: IsPaused changes AFTER
        /// PauseState completes its changes and notifications.
        /// </summary>
        public PauseState PauseState {
            get { return _pauseState; }
            private set {   // to set use ProcessPauseRequest
                SetProperty<PauseState>(ref _pauseState, value, "PauseState", OnPauseStateChanged);
            }
        }

        private void OnPauseStateChanged() {
            switch (PauseState) {
                case Common.PauseState.NotPaused:
                    IsPaused = false;
                    break;
                case Common.PauseState.GuiAutoPaused:
                case Common.PauseState.Paused:
                    IsPaused = true;
                    break;
                case Common.PauseState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PauseState));
            }
        }

        private bool _isPaused;
        /// <summary>
        /// Convenience Property indicating whether the game is paused. Automatically set
        /// as a result of OnPauseStateChanged. Warning: This means OnIsPausedChanging actually
        /// occurs AFTER the PauseState changes, but always before OnIsPausedChanged.
        /// </summary>
        public bool IsPaused {
            get { return _isPaused; }
            private set { SetProperty<bool>(ref _isPaused, value, "IsPaused"); }
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

            // initialize values without initiating change events
            _pauseState = PauseState.NotPaused;
            _isPaused = false;
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
            _eventMgr.AddListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            _eventMgr.AddListener<SaveGameEvent>(this, OnSaveGame);
        }

        // called by MonoGameManager when the existing scene is prepared
        public void BuildAndLoadNewGame(GameSettings settings) {
            GameState = GameState.Building;
            // building the level begins here when implemented
            Settings = settings;
            HumanPlayer = CreateHumanPlayer(settings);

            GameState = GameState.Loading;
            // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
            _eventMgr.Raise<SceneChangingEvent>(new SceneChangingEvent(this, SceneLevel.GameScene));
            D.Log("Application.LoadLevel({0}) being called.", SceneLevel.GameScene.GetName());
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

        // called by MonoGameManager when the existing scene is prepared
        public void LoadAndRestoreSavedGame(string gameID) {
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
        public void OnLevelHasCompletedLoading(SceneLevel newScene) {
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

        private void ProcessPauseRequest(PauseRequest request) {
            switch (request) {
                case PauseRequest.GuiAutoPause:
                    if (PauseState == PauseState.Paused) { return; }
                    if (PauseState == PauseState.GuiAutoPaused) {
                        D.Warn("Attempt to GuiAutoPause when already paused.");
                        return;
                    }
                    PauseState = PauseState.GuiAutoPaused;
                    break;
                case PauseRequest.GuiAutoResume:
                    if (PauseState == PauseState.Paused) { return; }
                    if (PauseState == PauseState.NotPaused) {
                        D.Warn("Attempt to GuiAutoResume when not paused.");
                        return;
                    }
                    PauseState = Common.PauseState.NotPaused;
                    break;
                case PauseRequest.PriorityPause:
                    if (PauseState == PauseState.Paused) {
                        D.Warn("Attempt to PriorityPause when already paused.");
                        return;
                    }
                    PauseState = PauseState.Paused;
                    break;
                case PauseRequest.PriorityResume:
                    if (PauseState == PauseState.NotPaused) {
                        D.Warn("Atttempt to PriorityResume when not paused.");
                        return;
                    }
                    PauseState = PauseState.NotPaused;
                    break;
                case PauseRequest.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(request));
            }
        }

        private void OnGameStateChanging(GameState newState) {
            ValidateConditionsForChangeInGameState(newState);
        }

        private void OnGameStateChanged() {
            InitializeOnGameStateChanged();
        }

        /// <summary>
        /// Called from Loader when all conditions are met to begin the progression to Running.
        /// Conditions include GameState.Waiting, no UnreadyElements and Update()
        /// has started.
        /// </summary>
        public void BeginCountdownToRunning() {
            GameState = GameState.RunningCountdown_3;
            GameState = GameState.RunningCountdown_2;
            GameState = GameState.RunningCountdown_1;
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
                    if (proposedNewState != GameState.RunningCountdown_3) { isError = true; }
                    break;
                case GameState.RunningCountdown_3:
                    if (proposedNewState != GameState.RunningCountdown_2) { isError = true; }
                    break;
                case GameState.RunningCountdown_2:
                    if (proposedNewState != GameState.RunningCountdown_1) { isError = true; }
                    break;
                case GameState.RunningCountdown_1:
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
                case GameState.RunningCountdown_3:
                case GameState.RunningCountdown_2:
                case GameState.RunningCountdown_1:
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
        /// Resets any conditions required for normal game startup. For instance, PauseState
        /// is normally NotPaused while setting up the first game. This may or maynot be the
        /// current state of PauseState given the numerous ways one can initiate the startup
        /// of a game instance.
        /// </summary>
        private void ResetConditionsForGameStartup() {
            if (IsPaused) {
                ProcessPauseRequest(PauseRequest.PriorityResume);
            }
        }

        private void OnExitGame(ExitGameEvent e) {
            Shutdown();
        }

        private void Shutdown() {
            _playerPrefsMgr.Store();
            Application.Quit(); // ignored inside Editor or WebPlayer
        }

        private void Cleanup() {
            Unsubscribe();
            _gameTime.Dispose();
        }

        private void Unsubscribe() {
            _eventMgr.RemoveListener<ExitGameEvent>(this, OnExitGame);
            _eventMgr.RemoveListener<GuiPauseRequestEvent>(this, OnGuiPauseChangeRequest);
            _eventMgr.RemoveListener<SaveGameEvent>(this, OnSaveGame);
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
                Cleanup();
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
        public int InstanceID { get; private set; }

        protected void IncrementInstanceCounter() {
            InstanceID = System.Threading.Interlocked.Increment(ref instanceCounter);
        }

        #endregion
    }
}


