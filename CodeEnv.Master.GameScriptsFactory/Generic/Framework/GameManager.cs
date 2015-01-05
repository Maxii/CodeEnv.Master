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
using System.Collections.Generic;
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
public class GameManager : AFSMSingleton_NoCall<GameManager, GameState>, IGameManager {

    /// <summary>
    /// Fires when GameState changes to Running, then clears all subscribers.
    /// WARNING: This event will fire each time the GameState changes to Running, 
    /// but as it clears its subscribers each time, clients will need to resubscribe if
    /// they want to receive the event again. Clients which persist across scene changes
    /// should pay particular attention as they won't automatically resubscribe since Awake
    /// (or a Constructor) is only called once in the life of the client.
    /// </summary>
    /// <remarks>
    /// Current clients SelectionManager, AGuiEnumSliderBase and DebugHud have been checked.
    /// </remarks>
    public event Action onIsRunningOneShot;

    /// <summary>
    /// Occurs just before a scene starts loading.
    /// Note: Event is not fired when the first scene is about to start loading as a result of the Application starting.
    /// </summary>
    public event Action<SceneLevel> onSceneLoading;

    /// <summary>
    /// Occurs just after a scene finishes loading, aka immediately after OnLevelWasLoaded is received.
    /// Note: Event is not fired when the first scene is loaded as a result of the Application starting.
    /// </summary>
    public event Action onSceneLoaded;

    public event Action onNewGameBuilding;

    public GameSettings GameSettings { get; private set; }

    private bool _isPaused;
    public bool IsPaused {
        get { return _isPaused; }
        set { SetProperty<bool>(ref _isPaused, value, "IsPaused"); }
    }

    public Player HumanPlayer { get; private set; }

    public IList<Player> AIPlayers { get; private set; }

    public SceneLevel CurrentScene { get; private set; }

    private bool _isRunning;
    /// <summary>
    /// Indicates whether the game is in GameState.Running or not.
    /// </summary>
    public bool IsRunning {
        get { return _isRunning; }
        private set { SetProperty<bool>(ref _isRunning, value, "IsRunning", OnIsRunningChanged); }
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

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    private IDictionary<GameState, IList<MonoBehaviour>> _gameStateProgressionReadinessLookup;
    private GameTime _gameTime;
    private PlayerPrefsManager _playerPrefsMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        InitializeStaticReference();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        InitializeGameStateProgressionReadinessSystem();
        // TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);
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

    private void InitializeStaticReference() {
        References.GameManager = Instance;

        // force initialization so they populate References
#pragma warning disable 0168
        var dummy1 = GameInputHelper.Instance;
        var dummy2 = GeneralFactory.Instance;
        var dummy3 = InputManager.Instance;
#pragma warning restore 0168
    }

    private void InitializeValuesAndReferences() {
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameTime = GameTime.Instance;
        UpdateRate = FrameUpdateFrequency.Infrequent;
        _pauseState = PauseState.NotPaused; // initializes value without initiating change event
        CurrentScene = (SceneLevel)Application.loadedLevel;
    }

    protected override void Start() {
        base.Start();
        __SimulateStartup();
    }

    #region Startup Simulation

    private bool __isStartupSimulation;

    private void __SimulateStartup() {
        //D.Log("{0}{1}.SimulateStartup() called.", GetType().Name, InstanceCount);
        switch (CurrentScene) {
            case SceneLevel.LobbyScene:
                CurrentState = GameState.Lobby;
                break;
            case SceneLevel.GameScene:
                __isStartupSimulation = true;
                CurrentState = GameState.Lobby;
                CurrentState = GameState.Loading;
                CurrentState = GameState.Building;
                CurrentState = GameState.Waiting;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentScene));
        }
    }

    #endregion

    #region GameState Progression Readiness System

    private Job __progressCheckJob;

    private void InitializeGameStateProgressionReadinessSystem() {
        _gameStateProgressionReadinessLookup = new Dictionary<GameState, IList<MonoBehaviour>>();
        _gameStateProgressionReadinessLookup.Add(GameState.Loading, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Building, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Restoring, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Waiting, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildAndDeploySystems, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.GeneratingPathGraphs, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.PrepareUnitsForDeployment, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DeployingUnits, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.RunningCountdown_1, new List<MonoBehaviour>());
    }

    private void StartGameStateProgressionReadinessChecks() {
        //D.Log("{0}_{1} is starting GameState Progression System Readiness Checks.", GetType().Name, InstanceCount);
        __ValidateGameStateProgressionReadinessSystemState();
        __progressCheckJob = new Job(AssessReadinessToProgressGameState(), toStart: true, onJobComplete: (wasJobKilled) => {
            if (wasJobKilled) {
                D.Error("{0}'s GameState Progression Readiness System has timed out.", GetType().Name);
            }
            else {
                D.Assert(CurrentState == GameState.Running, "{0}_{1}.{2} = {3}.".Inject(GetType().Name, InstanceCount, typeof(GameState).Name, CurrentState.GetName()), true);
                //D.Log("{0}'s GameState Progression Readiness System has successfully completed.", GetType().Name);
            }
        });
    }

    private void __ValidateGameStateProgressionReadinessSystemState() {
        var keys = _gameStateProgressionReadinessLookup.Keys;
        keys.ForAll(k => D.Assert(_gameStateProgressionReadinessLookup[k].Count == Constants.Zero));
    }

    private IEnumerator AssessReadinessToProgressGameState() {
        //D.Log("Entering AssessReadinessToProgressGameState.");
        float startTime = Time.time;
        while (CurrentState != GameState.Running) {
            D.Assert(CurrentState != GameState.Lobby);
            D.Assert(_gameStateProgressionReadinessLookup.ContainsKey(CurrentState), "{0} key not found.".Inject(CurrentState), pauseOnFail: true);
            // this will tell me what state failed, whereas failing while accessing the dictionary won't
            IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[CurrentState];
            //D.Log("{0}_{1}.AssessReadinessToProgressGameState() called. GameState = {2}, UnreadyElements count = {3}.", GetType().Name, InstanceCount, CurrentState.GetName(), unreadyElements.Count);
            if (unreadyElements != null && unreadyElements.Count == Constants.Zero) {
                //D.Log("State prior to ProgressState = {0}.", CurrentState.GetName());
                ProgressState();
                //D.Log("State after ProgressState = {0}.", CurrentState.GetName());
            }
            __CheckTime(startTime);
            yield return null;
        }
        //D.Log("Exiting AssessReadinessToProgressGameState.");
    }

    // FYI - does not catch delay caused by PathfindingMgr's scan, probably because AstarPath sets Time.timeScale = 0 while scanning
    private void __CheckTime(float startTime) {
        //D.Log("{0}.GameStateProgressionSystem time waiting = {1}.", GetType().Name, Time.time - startTime);
        if (Time.time - startTime > 5F) {
            __progressCheckJob.Kill();
            __progressCheckJob = null;
        }
    }

    public void RecordGameStateProgressionReadiness(MonoBehaviour source, GameState maxGameStateUntilReady, bool isReady) {
        IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[maxGameStateUntilReady];
        if (!isReady) {
            D.Assert(!unreadyElements.Contains(source), "UnreadyElements for {0} already has {1} registered!".Inject(maxGameStateUntilReady.GetName(), source.name));
            unreadyElements.Add(source);
            //D.Log("{0} has registered as unready to progress beyond {1}.", source.name, maxGameStateUntilReady.GetName());
        }
        else {
            D.Assert(unreadyElements.Contains(source), "UnreadyElements for {0} has no record of {1}!".Inject(maxGameStateUntilReady.GetName(), source.name));
            unreadyElements.Remove(source);
            //D.Log("{0} is now ready to progress beyond {1}. Remaining unready elements: {2}.",
            //source.name, maxGameStateUntilReady.GetName(), unreadyElements.Any() ? unreadyElements.Select(m => m.gameObject.name).Concatenate() : "None");
        }
    }

    #endregion

    #region New Game

    public void InitiateNewGame(GameSettings newGameSettings) {
        LoadAndBuildNewGame(newGameSettings);
    }

    private void LoadAndBuildNewGame(GameSettings settings) {
        D.Log("LoadAndBuildNewGame() called.");

        GameSettings = settings;
        HumanPlayer = CreateHumanPlayer(settings);
        AIPlayers = CreateAIPlayers(settings);

        CurrentState = GameState.Loading;

        //D.Log("Application.LoadLevel({0}) being called.", SceneLevel.GameScene.GetName());
        Application.LoadLevel((int)SceneLevel.GameScene);
    }

    private Player CreateHumanPlayer(GameSettings gameSettings) {
        return new Player(gameSettings.HumanPlayerRace, IQ.Normal, isPlayer: true);
    }

    private IList<Player> CreateAIPlayers(GameSettings gameSettings) {
        var aiPlayerRaces = gameSettings.AIPlayerRaces;
        var aiPlayers = new List<Player>(aiPlayerRaces.Length);
        aiPlayerRaces.ForAll(aiRace => {
            var aiPlayer = new Player(aiRace, IQ.Normal);
            D.Log("AI Player {0} created.", aiPlayer.LeaderName);
            aiPlayers.Add(aiPlayer);
        });
        return aiPlayers;
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
        if (_isExtraCopy) { return; }
        //D.Log("{0}_{1}.OnLevelWasLoaded({2}) received. Current State = {3}.", this.name, InstanceCount, ((SceneLevel)level).GetName(), CurrentState.GetName());
        RelayToCurrentState(level);
    }

    private void OnIsRunningChanged() {
        D.Log("{0}.IsRunning changed to {1}.", GetType().Name, IsRunning);
        if (IsRunning) {
            if (onIsRunningOneShot != null) {
                onIsRunningOneShot();
                onIsRunningOneShot = null;
            }
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        _gameTime.CheckForDateChange(); // CheckForDateChange() will ignore the call if clock is not enabled or is paused
    }

    #region Saving and Restoring

    public void SaveGame(string gameName) {
        GameSettings.IsNewGame = false;
        _gameTime.PrepareToSaveGame();
        LevelSerializer.SaveGame(gameName);
    }

    public void LoadSavedGame(string gameID) {
        LoadAndRestoreSavedGame(gameID);
    }

    private void LoadAndRestoreSavedGame(string gameID) {
        var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
        var gamesWithID = from g in savedGames where g.Caption == gameID select g;
        if (gamesWithID.IsNullOrEmpty<LevelSerializer.SaveEntry>()) {
            D.Warn("No saved game matches selected game caption {0}. \nLoad Saved Game not currently implemented.", gameID);
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

        CurrentState = GameState.Loading;
        selectedGame.Load();
    }

    protected override void OnDeserialized() {
        RelayToCurrentState();
    }

    #endregion

    /// <summary>
    /// Resets any conditions required for normal game startup. For instance, PauseState
    /// is normally NotPaused while setting up the first game. This may or maynot be the
    /// current state of PauseState given the numerous ways one can initiate the startup
    /// of a game instance.
    /// </summary>
    private void ResetConditionsForGameStartup() {
        if (IsPaused) {
            RequestPauseStateChange(toPause: false, toOverride: true);
        }
    }

    #region Pausing System

    /// <summary>
    /// Requests a pause state change. A request to resume [!toPause] from a pause without 
    /// overriding may not be accommodated if the current pause was set without overriding.
    /// </summary>
    /// <param name="toPause">if set to <c>true</c> [to pause].</param>
    /// <param name="toOverride">if set to <c>true</c> [to override].</param>
    public void RequestPauseStateChange(bool toPause, bool toOverride = false) {
        if (toOverride) {
            PauseState = toPause ? PauseState.Paused : PauseState.NotPaused;
        }
        else if (PauseState != PauseState.Paused) {
            PauseState = toPause ? PauseState.AutoPaused : PauseState.NotPaused;
        }
    }

    private void OnPauseStateChanged() {
        switch (PauseState) {
            case PauseState.NotPaused:
                IsPaused = false;
                break;
            case PauseState.AutoPaused:
            case PauseState.Paused:
                IsPaused = true;
                break;
            case PauseState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PauseState));
        }
    }

    #endregion

    #region GameState System

    // ************************************************************************************************************
    // NOTE: The sequencing when a change of state is initiated by setting CurrentState = newState
    //
    // 1. the state we are changing from is recorded as lastState
    // 2. the 2 events indicating a state is about to change are sent to subscribers
    // 3. the value of the CurrentState enum is changed to newState
    // 4. the lastState_ExitState() method is called 
    //          - while in this method, realize that the CurrentState enum has already changed to newState
    // 5. the CurrentState's delegates are updated 
    //          - meaning the EnterState delegate is changed from lastState_EnterState to newState_EnterState
    // 6. the newState_EnterState() method is called
    //          - as the event in 7 has not yet been called, you CANNOT set CurrentState = nextState within newState_EnterState()
    //              - this would initiate the whole cycle above again, BEFORE the event in 7 is called
    //              - you also can't just use a coroutine to wait then change it as the event is still held up
    //          - instead, change it a frame later after the EnterState method has completed, and the events have been fired
    // 7. the 2 events indicating a state has just changed are sent to subscribers
    //          - when this event is received, a get_CurrentState property inquiry will properly return newState
    // *************************************************************************************************************

    // ***********************************************************************************************************
    // WARNING: IEnumerator State_EnterState methods are executed when the frame's Coroutine's are run, 
    // not when the state itself is changed. The order in which those state execution coroutines 
    // are run has nothing to do with the order in which the item's state is changed, aka if item1's state
    // is changed before item2's state, that DOES NOT mean item1's enterState will be called before item2's enterState.
    // ***********************************************************************************************************

    #region Lobby

    void Lobby_EnterState() {
        LogEvent();
    }

    void Lobby_ExitState() {
        LogEvent();
        // Transitioning to Loading (the level) whether a new or saved game
        D.Assert(CurrentState == GameState.Loading);
    }

    #endregion

    #region Loading

    void Loading_EnterState() {
        LogEvent();
        // Start state progression checks here as Loading is always called whether a new game, loading saved game or startup simulation
        StartGameStateProgressionReadinessChecks();

        if (__isStartupSimulation) {
            var universeSize = _playerPrefsMgr.UniverseSizeSelection.Convert();

            int aiPlayerCount = universeSize.DefaultAIPlayerCount();
            //int aiPlayerCount = _playerPrefsMgr.UniverseSize.DefaultAIPlayerCount();
            var aiPlayerRaces = new Race[aiPlayerCount];
            for (int i = 0; i < aiPlayerCount; i++) {
                var aiSpecies = Enums<Species>.GetRandomExcept(Species.None, Species.Human);
                aiPlayerRaces[i] = new Race(aiSpecies);
            }

            var gameSettings = new GameSettings {
                IsNewGame = true,
                UniverseSize = universeSize,
                //UniverseSize = _playerPrefsMgr.UniverseSize,
                HumanPlayerRace = new Race(new RaceStat(_playerPrefsMgr.PlayerSpeciesSelection.Convert(), "Maxii", "Maxii description", _playerPrefsMgr.PlayerColor)),
                AIPlayerRaces = aiPlayerRaces
            };
            GameSettings = gameSettings;
            HumanPlayer = CreateHumanPlayer(gameSettings);
            AIPlayers = CreateAIPlayers(gameSettings);
            return;
        }

        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: false);

        // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
        if (onSceneLoading != null) {
            onSceneLoading(SceneLevel.GameScene);
        }
    }

    void Loading_OnLevelWasLoaded(int level) {
        D.Assert(!__isStartupSimulation);
        LogEvent();

        CurrentScene = (SceneLevel)level;
        D.Assert(CurrentScene == SceneLevel.GameScene, "Scene transition to {0} not implemented.".Inject(CurrentScene.GetName()));

        if (onSceneLoaded != null) {
            //D.Log("{0}.onSceneLoaded event dispatched.", GetType().Name);
            onSceneLoaded();
        }

        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: true);
    }

    void Loading_ProgressState() {
        LogEvent();
        CurrentState = (LevelSerializer.IsDeserializing || !GameSettings.IsNewGame) ? GameState.Restoring : GameState.Building;
    }

    void Loading_ExitState() {
        LogEvent();
        // Transitioning to Building if new game, or Restoring if saved game
        D.Assert(CurrentState == GameState.Building || CurrentState == GameState.Restoring);
    }

    #endregion

    #region Building

    void Building_EnterState() {
        LogEvent();
        if (__isStartupSimulation) { return; }

        RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: false);

        // Building is only for new games
        D.Assert(GameSettings.IsNewGame);
        if (onNewGameBuilding != null) {
            onNewGameBuilding();
        }
        ResetConditionsForGameStartup();
        _gameTime.PrepareToBeginNewGame();

        RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: true);
    }

    void Building_ProgressState() {
        LogEvent();
        CurrentState = GameState.Waiting;
    }

    void Building_ExitState() {
        LogEvent();
        // Building is only for new games, so next state is Waiting
        D.Assert(CurrentState == GameState.Waiting);
    }

    #endregion

    #region Restoring

    void Restoring_EnterState() {
        LogEvent();
        RecordGameStateProgressionReadiness(Instance, GameState.Restoring, isReady: false);
    }

    void Restoring_OnDeserialized() {
        LogEvent();

        ResetConditionsForGameStartup();
        _gameTime.PrepareToResumeSavedGame();

        RecordGameStateProgressionReadiness(Instance, GameState.Restoring, isReady: true);
    }

    void Restoring_ProgressState() {
        LogEvent();
        CurrentState = GameState.Waiting;
    }

    void Restoring_ExitState() {
        LogEvent();
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
        LogEvent();
    }

    void BuildAndDeploySystems_ProgressState() {
        LogEvent();
        CurrentState = GameState.GeneratingPathGraphs;
    }

    void BuildAndDeploySystems_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.GeneratingPathGraphs);
    }

    #endregion

    #region GeneratingPathGraphs

    void GeneratingPathGraphs_EnterState() {
        LogEvent();
    }

    void GeneratingPathGraphs_ProgressState() {
        LogEvent();
        CurrentState = GameState.PrepareUnitsForDeployment;
    }

    void GeneratingPathGraphs_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.PrepareUnitsForDeployment);
    }

    #endregion

    #region PrepareUnitsForDeployment

    void PrepareUnitsForDeployment_EnterState() {
        LogEvent();
    }

    void PrepareUnitsForDeployment_ProgressState() {
        LogEvent();
        CurrentState = GameState.DeployingUnits;
    }

    void PrepareUnitsForDeployment_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.DeployingUnits);
    }

    #endregion

    #region DeployingUnits

    void DeployingUnits_EnterState() {
        LogEvent();
    }

    void DeployingUnits_ProgressState() {
        LogEvent();
        CurrentState = GameState.RunningCountdown_1;
    }

    void DeployingUnits_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.RunningCountdown_1);
    }

    #endregion

    #region RunningCountdown_1

    void RunningCountdown_1_EnterState() {
        LogEvent();
    }

    void RunningCountdown_1_ProgressState() {
        LogEvent();
        CurrentState = GameState.Running;
    }

    void RunningCountdown_1_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Running);
    }

    #endregion

    #region Running

    void Running_EnterState() {
        LogEvent();
        _gameTime.EnableClock(true);
        if (_playerPrefsMgr.IsPauseOnLoadEnabled) {
            RequestPauseStateChange(toPause: true, toOverride: true);
        }
        __isStartupSimulation = false;
        IsRunning = true;
    }

    void Running_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Lobby || CurrentState == GameState.Loading);
        IsRunning = false;
    }

    #endregion

    #region State Machine Support Methods

    private void ProgressState() { RelayToCurrentState(); }

    #endregion

    #endregion

    public void ExitGame() { Shutdown(); }  // TODO Confirmation Dialog

    private void Shutdown() {
        _playerPrefsMgr.Store();
        Application.Quit(); // ignored inside Editor or WebPlayer
    }

    protected override void Cleanup() {
        References.GameManager = null;
        _gameTime.Dispose();
        if (__progressCheckJob != null) {
            __progressCheckJob.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

