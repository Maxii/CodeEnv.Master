// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager.cs
// Singleton. The main Manager for the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Linq;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. The main Manager for the game.
/// </summary>
[SerializeAll]
public class GameManager : AMonoBaseSingleton<GameManager>, IDisposable {

    public static GameSettings Settings { get; private set; }

    public HumanPlayer HumanPlayer { get; private set; }

    private GameState _gameState;
    public GameState GameState {
        get { return _gameState; }
        private set { SetProperty<GameState>(ref _gameState, value, "GameState", OnGameStateChanged, OnGameStateChanging); }
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

    private GameEventManager _eventMgr;
    private GameTime _gameTime;
    private PlayerPrefsManager _playerPrefsMgr;
    private GameStatus _gameStatus;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }

        // TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);
        _eventMgr = GameEventManager.Instance;
        _playerPrefsMgr = PlayerPrefsManager.Instance;

        _gameStatus = GameStatus.Instance;
        _gameTime = GameTime.Instance;   // delay until Instance is initialized
        // initialize values without initiating change events
        _pauseState = PauseState.NotPaused;
        Subscribe();
        __AwakeBasedOnStartScene();
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each scene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    //private void ChangeLanguage(string language) {
    //    CultureInfo newCulture = new CultureInfo(language);
    //    Thread.CurrentThread.CurrentCulture = newCulture;
    //    Thread.CurrentThread.CurrentUICulture = newCulture;
    //    D.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
    //    D.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetName()));
    //}

    private void Subscribe() {
        _eventMgr.AddListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.AddListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
        _eventMgr.AddListener<ExitGameEvent>(Instance, OnExitGame);
        _eventMgr.AddListener<GuiPauseRequestEvent>(Instance, OnGuiPauseChangeRequest);
        _eventMgr.AddListener<SaveGameEvent>(Instance, OnSaveGame);
    }

    protected override void Start() {
        base.Start();
        __StartBasedOnStartScene();
    }

    #region Startup Simulation

    private void __AwakeBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.LobbyScene:
                GameState = GameState.Lobby;
                break;
            case SceneLevel.GameScene:
                __SimulateBuildGameFromLobby_Step1();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startScene));
        }
    }

    private void __StartBasedOnStartScene() {
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        switch (startScene) {
            case SceneLevel.LobbyScene:
                break;
            case SceneLevel.GameScene:
                __SimulateBuildGameFromLobby_Step2();
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
        GameSettings = settings;
        HumanPlayer = CreateHumanPlayer(settings);
        GameState = GameState.Loading;
    }

    private void __SimulateBuildGameFromLobby_Step2() {
        // GameState.Restoring only applies to loading saved games
        GameState = GameState.Waiting;
    }

    #endregion

    #region New Game

    private void OnBuildNewGame(BuildNewGameEvent e) {
        D.Log("BuildNewGameEvent received.");
        StartCoroutine<GameSettings>(WaitUntilReadyThenBuildNewGame, e.Settings);
    }

    private IEnumerator WaitUntilReadyThenBuildNewGame(GameSettings settings) {
        while (!GuiManager.Instance.ReadyForSceneChange) {
            yield return null;
        }
        BuildAndLoadNewGame(settings);
    }

    private void BuildAndLoadNewGame(GameSettings settings) {
        GameState = GameState.Building;
        // building the level begins here when implemented
        GameSettings = settings;
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

    #endregion

    #region Saving and Restoring

    private void OnLoadSavedGame(LoadSavedGameEvent e) {
        D.Log("LoadSavedGameEvent received.");
        StartCoroutine<string>(WaitUntilReadyThenLoadAndRestoreSavedGame, e.GameID);
    }

    private IEnumerator WaitUntilReadyThenLoadAndRestoreSavedGame(string gameID) {
        while (!GuiManager.Instance.ReadyForSceneChange) {
            yield return null;
        }
        LoadAndRestoreSavedGame(gameID);
    }

    private void OnSaveGame(SaveGameEvent e) {
        SaveGame(e.GameName);
    }

    private void SaveGame(string gameName) {
        GameSettings.IsNewGame = false;
        _gameTime.PrepareToSaveGame();
        LevelSerializer.SaveGame(gameName);
    }

    // called by MonoGameManager when the existing scene is prepared
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

    // This substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    // Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    // might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    void OnLevelWasLoaded(int level) {
        if (enabled) {
            // OnLevelWasLoaded is called on all active components and at any time. The earliest thing that happens after Destroy(gameObject)
            // is component disablement. GameObject deactivation happens later, but before OnDestroy()
            D.Log("{0}_{1}.OnLevelWasLoaded(level = {2}) called.".Inject(this.name, InstanceID, ((SceneLevel)level).GetName()));
            SceneLevel newScene = (SceneLevel)level;
            if (newScene != SceneLevel.GameScene) {
                D.Error("A Scene change to {0} is currently not implemented.", newScene.GetName());
                return;
            }
            _eventMgr.Raise<SceneChangedEvent>(new SceneChangedEvent(this, newScene));
            if (LevelSerializer.IsDeserializing || !GameSettings.IsNewGame) {
                GameState = GameState.Restoring;
            }
            else {
                ResetConditionsForGameStartup();
                _gameTime.PrepareToBeginNewGame();
                GameState = GameState.Waiting;
            }
        }
    }

    // Assumes GameTime.PrepareToResumeSavedGame() can only be called AFTER OnLevelWasLoaded
    protected override void OnDeserialized() {
        D.Log("GameManager.OnDeserialized() called.");
        ResetConditionsForGameStartup();
        _gameTime.PrepareToResumeSavedGame();
        GameState = GameState.Waiting;
    }

    #endregion

    #region Pausing System

    private void OnGuiPauseChangeRequest(GuiPauseRequestEvent e) {
        D.Assert(GameState == GameState.Running);
        ProcessPauseRequest(e.NewValue);
    }

    private void ProcessPauseRequest(PauseCommand request) {
        switch (request) {
            case PauseCommand.AutoPause:
                if (PauseState == PauseState.Paused) { return; }
                if (PauseState == PauseState.AutoPaused) {
                    D.Warn("Attempt to GuiAutoPause when already paused.");
                    return;
                }
                PauseState = PauseState.AutoPaused;
                break;
            case PauseCommand.AutoResume:
                if (PauseState == PauseState.Paused) { return; }
                if (PauseState == PauseState.NotPaused) {
                    D.Warn("Attempt to GuiAutoResume when not paused.");
                    return;
                }
                PauseState = PauseState.NotPaused;
                break;
            case PauseCommand.ManualPause:
                if (PauseState == PauseState.Paused) {
                    D.Warn("Attempt to PriorityPause when already paused.");
                    return;
                }
                PauseState = PauseState.Paused;
                break;
            case PauseCommand.ManualResume:
                if (PauseState == PauseState.NotPaused) {
                    D.Warn("Atttempt to PriorityResume when not paused.");
                    return;
                }
                PauseState = PauseState.NotPaused;
                break;
            case PauseCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(request));
        }
    }

    private void OnPauseStateChanged() {
        switch (PauseState) {
            case PauseState.NotPaused:
                _gameStatus.IsPaused = false;
                break;
            case PauseState.AutoPaused:
            case PauseState.Paused:
                _gameStatus.IsPaused = true;
                break;
            case PauseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PauseState));
        }
    }

    #endregion

    #region GameState System

    private void OnGameStateChanging(GameState newState) {
        ValidateConditionsForChangeInGameState(newState);
    }

    private void OnGameStateChanged() {
        InitializeOnGameStateChanged();
    }

    /// <summary>
    /// Progresses the GameState from the current state in sequence to Running.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public void __ProgressToRunning() {
        switch (GameState) {
            case GameState.Waiting:
                GameState = GameState.GeneratingPathGraphs;
                GameState = GameState.DeployingUnits;
                GameState = GameState.RunningCountdown_1;
                GameState = GameState.Running;
                break;
            case GameState.GeneratingPathGraphs:
                GameState = GameState.DeployingUnits;
                GameState = GameState.RunningCountdown_1;
                GameState = GameState.Running;
                break;
            case GameState.DeployingUnits:
                GameState = GameState.RunningCountdown_1;
                GameState = GameState.Running;
                break;
            case GameState.RunningCountdown_1:
                GameState = GameState.Running;
                break;
            // the rest of the states should never change this way
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(GameState));
        }
    }

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
                if (proposedNewState != GameState.GeneratingPathGraphs) { isError = true; }
                break;
            case GameState.GeneratingPathGraphs:
                if (proposedNewState != GameState.DeployingUnits) { isError = true; }
                break;
            case GameState.DeployingUnits:
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
            return;
        }
    }

    /// <summary>
    /// Does any initialization called for by a transition to a new GameState. 
    /// WARNING: Donot change state directly from this method as this initialization
    /// step occurs prior to sending out the GameStateChanged event to subscribers. Directly changing
    /// the state within this method changes the state BEFORE the previous state has it's events sent.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void InitializeOnGameStateChanged() {
        switch (GameState) {
            case GameState.Lobby:
            case GameState.Building:
            case GameState.Loading:
            case GameState.Restoring:
            case GameState.Waiting:
            case GameState.GeneratingPathGraphs:
            case GameState.DeployingUnits:
            case GameState.RunningCountdown_1:
                _gameStatus.IsRunning = false;
                break;
            case GameState.Running:
                _gameStatus.IsRunning = true;
                _gameTime.EnableClock(true);
                if (_playerPrefsMgr.IsPauseOnLoadEnabled) {
                    ProcessPauseRequest(PauseCommand.ManualPause);
                }
                break;
            case GameState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(GameState));
        }
        D.Log("GameState changed to {0}.", Instance.GameState.GetName());
    }

    #endregion

    /// <summary>
    /// Resets any conditions required for normal game startup. For instance, PauseState
    /// is normally NotPaused while setting up the first game. This may or maynot be the
    /// current state of PauseState given the numerous ways one can initiate the startup
    /// of a game instance.
    /// </summary>
    private void ResetConditionsForGameStartup() {
        if (_gameStatus.IsPaused) {
            ProcessPauseRequest(PauseCommand.ManualResume);
        }
    }

    private void OnExitGame(ExitGameEvent e) {
        Shutdown();
    }

    private void Shutdown() {
        _playerPrefsMgr.Store();
        Application.Quit(); // ignored inside Editor or WebPlayer
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (enabled) { // The earliest thing that happens after Destroy(gameObject) is component disablement. GameObject deactivation happens later, but before OnDestroy()
            Unsubscribe();
            _gameTime.Dispose();
            // other cleanup here including any tracking Gui2D elements
        }
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.RemoveListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
        _eventMgr.RemoveListener<ExitGameEvent>(Instance, OnExitGame);
        _eventMgr.RemoveListener<GuiPauseRequestEvent>(Instance, OnGuiPauseChangeRequest);
        _eventMgr.RemoveListener<SaveGameEvent>(Instance, OnSaveGame);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
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

}

