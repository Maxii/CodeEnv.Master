// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager_State.cs
// Singleton. The main manager for the game, implemented as a mono state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. The main manager for the game, implemented as a mono state machine.
/// </summary>
[SerializeAll]
public class GameManager : AMonoStateMachineSingleton<GameManager, GameState>, IGameManager, IDisposable {

    public static GameSettings Settings { get; private set; }

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
        _gameTime = GameTime.Instance;
        UpdateRate = FrameUpdateFrequency.Infrequent;
        // initialize values without initiating change events
        _pauseState = PauseState.NotPaused;

        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        InitializeStaticReferences(startScene);
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        SceneLevel startScene = (SceneLevel)Application.loadedLevel;
        SimulateStartup(startScene);
        // WARNING: enabled is used to determine if an extra GameMgr instance is being destroyed. Donot manipulate it for other purposes
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

    #region Language

    private void ChangeLanguage(string language) {
        CultureInfo newCulture = new CultureInfo(language);
        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        D.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
        D.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetName()));
    }

    #endregion

    private void Subscribe() {
        _eventMgr.AddListener<BuildNewGameEvent>(Instance, OnBuildNewGame);
        _eventMgr.AddListener<LoadSavedGameEvent>(Instance, OnLoadSavedGame);
        _eventMgr.AddListener<ExitGameEvent>(Instance, OnExitGame);
        _eventMgr.AddListener<GuiPauseRequestEvent>(Instance, OnGuiPauseChangeRequest);
        _eventMgr.AddListener<SaveGameEvent>(Instance, OnSaveGame);
    }

    private void InitializeStaticReferences(SceneLevel sceneLevel) {
        if (sceneLevel == SceneLevel.GameScene) {
            D.Log("Initializing Static References for {0}.", sceneLevel.GetName());
        }
        switch (sceneLevel) {
            case SceneLevel.IntroScene:
                // No use for References currently in IntroScene
                break;
            case SceneLevel.GameScene:
                References.GameManager = Instance;
                References.InputHelper = GameInputHelper.Instance;
                References.DynamicObjects = DynamicObjects.Instance;
                References.CameraControl = CameraControl.Instance;
                References.UnitFactory = UnitFactory.Instance;
                References.GeneralFactory = GeneralFactory.Instance;
                References.UsefulTools = UsefulTools.Instance;
                References.Universe = Universe.Instance;
                // GuiHudPublisher factory reference settings moved to GuiCursorHud
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sceneLevel));
        }
    }

    #region Startup Simulation

    private void SimulateStartup(SceneLevel scene) {
        switch (scene) {
            case SceneLevel.IntroScene:
                CurrentState = GameState.Lobby;
                break;
            case SceneLevel.GameScene:
                CurrentState = GameState.Lobby;  // avoids the Illegal state transition Error
                CurrentState = GameState.Building;
                GameSettings settings = new GameSettings {
                    IsNewGame = true,
                    UniverseSize = _playerPrefsMgr.UniverseSize,
                    PlayerRace = TempGameValues.HumanPlayersRace
                };
                Settings = settings;
                HumanPlayer = CreateHumanPlayer(settings);
                CurrentState = GameState.Loading;
                // GameState.Restoring only applies to loading saved games
                CurrentState = GameState.Waiting;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(scene));
        }
    }

    #endregion

    #region New Game

    private void OnBuildNewGame(BuildNewGameEvent e) {
        D.Log("BuildNewGameEvent received.");
        new Job(WaitForGuiManager(), toStart: true, onJobComplete: delegate {
            BuildAndLoadNewGame(e.Settings);
        });
        //the above is the anonymous method approach that allows you to ignore parameters if not used
        //the lambda equivalent: (jobWasKilled) => { BuildAndLoadNewGame(e.Settings); }
    }

    private IEnumerator WaitForGuiManager() {
        while (!GuiManager.Instance.ReadyForSceneChange) {
            yield return null;
        }
    }

    private void BuildAndLoadNewGame(GameSettings settings) {
        D.Log("BuildAndLoadNewGame() called.");
        CurrentState = GameState.Building;
        // building the level begins here when implemented
        Settings = settings;
        HumanPlayer = CreateHumanPlayer(settings);

        CurrentState = GameState.Loading;
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

    /// <summary>
    /// This substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    /// NOTE: Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    /// might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    /// NOTE: In fact, my experience is that OnLevelWasLoaded occurs AFTER all Awake calls for objects that are already in the level.
    /// </summary>
    /// <param name="level">The scene level.</param>
    void OnLevelWasLoaded(int level) {
        D.Log("{0}_{1}.OnLevelWasLoaded({2}) called.", this.name, InstanceID, ((SceneLevel)level).GetName());
        if (enabled) {
            // The earliest thing that happens after Destroy(gameObject) is component disablement so this is a good filter for gameObjects 
            // in the process of being destroyed. GameObject deactivation happens later, but before OnDestroy()
            D.Log("{0}_{1}.OnLevelWasLoaded({2}) initializing.", this.name, InstanceID, ((SceneLevel)level).GetName());
            SceneLevel newScene = (SceneLevel)level;
            if (newScene != SceneLevel.GameScene) {
                D.Error("A Scene change to {0} is currently not implemented.", newScene.GetName());
                return;
            }

            _eventMgr.Raise<SceneChangedEvent>(new SceneChangedEvent(this, newScene));
            if (LevelSerializer.IsDeserializing || !Settings.IsNewGame) {
                CurrentState = GameState.Restoring;
            }
            else {
                InitializeStaticReferences(newScene);
                ResetConditionsForGameStartup();
                _gameTime.PrepareToBeginNewGame();
                CurrentState = GameState.Waiting;
            }
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        // update is allowed to run all the time as I need enabled for detecting extra GameMgr instances
        // CheckForDateChange() will ignore the call if clock is not enabled or is paused
        _gameTime.CheckForDateChange();
    }

    #region Saving and Restoring

    private void OnSaveGame(SaveGameEvent e) {
        SaveGame(e.GameName);
    }

    private void SaveGame(string gameName) {
        Settings.IsNewGame = false;
        _gameTime.PrepareToSaveGame();
        LevelSerializer.SaveGame(gameName);
    }

    private void OnLoadSavedGame(LoadSavedGameEvent e) {
        D.Log("LoadSavedGameEvent received.");
        new Job(WaitForGuiManager(), toStart: true, onJobComplete: delegate {
            LoadAndRestoreSavedGame(e.GameID);
        });
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

        CurrentState = GameState.Building;
        CurrentState = GameState.Loading;
        // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
        _eventMgr.Raise<SceneChangingEvent>(new SceneChangingEvent(this, SceneLevel.GameScene));
        selectedGame.Load();
    }

    // Assumes GameTime.PrepareToResumeSavedGame() can only be called AFTER OnLevelWasLoaded
    protected override void OnDeserialized() {
        D.Log("GameManager.OnDeserialized() called.");
        // TODO InitializeStaticReferences(scene)?
        ResetConditionsForGameStartup();
        _gameTime.PrepareToResumeSavedGame();
        CurrentState = GameState.Waiting;
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
            ProcessPauseRequest(PauseRequest.PriorityResume);
        }
    }

    /// <summary>
    /// Enables the Ngui Event System in all UICamera's in the scene. 
    /// 
    /// Setting the eventReceiverMask to -1 means Everything (all layers) are visible to the event system. 
    /// 0 means Nothing - no layers are visible to the event system. The actual mask used in UICamera 
    /// to determine which layers the event system will actually 'see' is the AND of the Camera culling mask 
    /// that the UICamera script is attached too and this eventReceiverMask. This means that the event 
    /// system will only 'see' layers that both the camera can see AND the layer mask is allowed to see. 
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> all layers the camera can see will be visible to the event system.</param>
    private void EnableEvents(bool toEnable) {
        BetterList<UICamera> allEventDispatchersInScene = UICamera.list;
        foreach (var eventDispatcher in allEventDispatchersInScene) {
            eventDispatcher.eventReceiverMask = toEnable ? -1 : 0;
        }
    }

    #region Pausing System

    private void OnGuiPauseChangeRequest(GuiPauseRequestEvent e) {
        D.Assert(CurrentState == GameState.Running);
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
                PauseState = PauseState.NotPaused;
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

    private void OnPauseStateChanged() {
        switch (PauseState) {
            case PauseState.NotPaused:
                _gameStatus.IsPaused = false;
                break;
            case PauseState.GuiAutoPaused:
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

    protected override void OnCurrentStateChanging(GameState incomingState) {
        base.OnCurrentStateChanging(incomingState);
        D.Log("{0} changing from {1} to {2}.", typeof(GameState).Name, CurrentState.GetName(), incomingState.GetName());
    }

    protected override void OnCurrentStateChanged() {
        base.OnCurrentStateChanged();
    }

    // NOTE: The sequencing when a change of state is initiated by setting CurrentState = newState
    // 1. the state we are changing from is recorded as lastState
    // 2. the event OnCurrentStateChanging(newState) is sent to subscribers
    // 3. the value of the CurrentState enum is changed to newState
    // 4. the lastState_ExitState() method is called 
    //          - while in this method, realize that the CurrentState enum has already changed to newState
    // 5. the CurrentState's delegates are updated 
    //          - meaning the EnterState delegate is changed from lastState_EnterState to newState_EnterState
    // 6. the newState_EnterState() method is called
    //          - as the event in 7 has not yet been called, you CANNOT set CurrentState = nextState within newState_EnterState()
    //              - this would initiate the whole cycle above again, BEFORE the event in 7 is called
    //              - you also can't just use a coroutine to wait then change it as the event is still held up
    //          - instead, change it in newState_Update() which allows the event in 7 to complete before this change occurs again
    // 7. the event OnCurrentStateChanged() is sent to subscribers
    //          - when this event is received, a get_CurrentState property inquiry will properly return newState

    #region Lobby

    void Lobby_EnterState() {
        LogEvent();
        EnableEvents(true);
    }

    void Lobby_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Building);
        EnableEvents(false);
    }

    #endregion

    #region Building

    void Building_ExitState() {
        D.Assert(CurrentState == GameState.Loading);
    }

    #endregion

    #region Loading

    void Loading_ExitState() {
        D.Assert(CurrentState == GameState.Waiting || CurrentState == GameState.Restoring);
    }

    #endregion

    #region Restoring

    void Restoring_ExitState() {
        D.Assert(CurrentState == GameState.Waiting);
    }

    #endregion

    #region Waiting

    void Waiting_EnterState() {
        LogEvent();
    }

    void Waiting_ProgressState() {
        CurrentState = GameState.BuildAndDeploySystems;
    }

    void Waiting_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.BuildAndDeploySystems);
    }

    #endregion

    #region BuildAndDeploySystems

    void BuildAndDeploySystems_EnterState() {
        //LogEvent();
    }

    void BuildAndDeploySystems_ProgressState() {
        LogEvent();
        CurrentState = GameState.GeneratingPathGraphs;
    }

    void BuildAndDeploySystems_ExitState() {
        //LogEvent();
        D.Assert(CurrentState == GameState.GeneratingPathGraphs);
    }

    #endregion

    #region GeneratingPathGraphs

    void GeneratingPathGraphs_EnterState() {
        //LogEvent();
    }

    void GeneratingPathGraphs_ProgressState() {
        LogEvent();
        CurrentState = GameState.PrepareUnitsForDeployment;
    }

    void GeneratingPathGraphs_ExitState() {
        //LogEvent();
        D.Assert(CurrentState == GameState.PrepareUnitsForDeployment);
    }

    #endregion

    #region PrepareUnitsForDeployment

    void PrepareUnitsForDeployment_EnterState() {
        //LogEvent();
    }

    void PrepareUnitsForDeployment_ProgressState() {
        LogEvent();
        CurrentState = GameState.DeployingUnits;
    }

    void PrepareUnitsForDeployment_ExitState() {
        //LogEvent();
        D.Assert(CurrentState == GameState.DeployingUnits);
    }

    #endregion

    #region DeployingUnits

    void DeployingUnits_EnterState() {
        //LogEvent();
    }

    void DeployingUnits_ProgressState() {
        LogEvent();
        CurrentState = GameState.RunningCountdown_1;
    }

    void DeployingUnits_ExitState() {
        //LogEvent();
        D.Assert(CurrentState == GameState.RunningCountdown_1);
    }

    #endregion

    #region RunningCountdown_1

    void RunningCountdown_1_EnterState() {
        //LogEvent();
    }

    void RunningCountdown_1_ProgressState() {
        LogEvent();
        CurrentState = GameState.Running;
    }

    void RunningCountdown_1_ExitState() {
        //LogEvent();
        D.Assert(CurrentState == GameState.Running);
    }

    #endregion

    #region Running

    void Running_EnterState() {
        LogEvent();
        _gameTime.EnableClock(true);
        if (_playerPrefsMgr.IsPauseOnLoadEnabled) {
            ProcessPauseRequest(PauseRequest.PriorityPause);
        }
        EnableEvents(true);
        _gameStatus.IsRunning = true;
    }

    void Running_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Lobby || CurrentState == GameState.Building);
        _gameStatus.IsRunning = false;
        EnableEvents(false);
    }

    #endregion

    #region Callbacks

    public void ProgressState() {
        RelayToCurrentState();
    }

    #endregion

    #endregion

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

    #region IGameManager Members

    public HumanPlayer HumanPlayer { get; private set; }

    #endregion

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

