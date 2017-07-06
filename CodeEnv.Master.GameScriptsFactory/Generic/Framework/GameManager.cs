// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager.cs
// Singleton. The main manager for the game, implemented as a mono state machine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton. The main manager for the game, implemented as a mono state machine.
/// </summary>
public class GameManager : AFSMSingleton_NoCall<GameManager, GameState>, IGameManager {

    private const string DebugNameFormat = "{0}_{1}";

    public static bool IsFirstStartup { // UNCLEAR, IMPROVE not used
        get {
            if (PlayerPrefs.HasKey("IsNotFirstStartup")) {
                return false;
            }
            else {
                PlayerPrefs.SetInt("IsNotFirstStartup", Constants.One);
                return true;
            }
        }
    }

    /// <summary>
    /// Fires when GameState changes to Running and is ready to play, then clears all subscribers.
    /// WARNING: This event will fire each time the GameState changes to Running, 
    /// but as it clears its subscribers each time, clients will need to resubscribe if
    /// they want to receive the event again. Clients which persist across scene changes
    /// should pay particular attention as they won't automatically resubscribe since Awake
    /// (or a Constructor) is only called once in the life of the client.
    /// </summary>
    /// <remarks>
    /// Current clients AGuiEnumSliderBase, FPSReadout and GameTime as of 10.14.16.
    /// </remarks>
    public event EventHandler isReadyForPlayOneShot;

    public event EventHandler isPausedChanged;

    /// <summary>
    /// Occurs just before a scene starts loading.
    /// <remarks>12.2.16 Event is not fired when the first scene is about to start loading as a result of the Application starting.</remarks>
    /// <remarks>GameSettings is valid.</remarks>
    /// </summary>
    public event EventHandler sceneLoading;

    /// <summary>
    /// Occurs just after a scene finishes loading, aka immediately after the SceneManager.sceneLoaded event is received.
    /// <remarks>3.8.17 This event can fire in the same frame as sceneLoading but will never precede it.</remarks>
    /// <remarks>Prior to Unity 5.3 was after OnLevelWasLoaded() was received.</remarks>
    /// <remarks>12.2.16 Event is not fired when the first scene is loaded as a result of the Application starting.</remarks>
    /// <remarks>GameSettings is valid.</remarks>
    /// </summary>
    public event EventHandler sceneLoaded;

    /// <summary>
    /// Occurs when GameState is about to change.
    /// </summary>
    public event EventHandler gameStateChanging;

    /// <summary>
    /// Occurs when the GameState has just changed. 
    /// </summary>
    public event EventHandler gameStateChanged;

    /// <summary>
    /// Occurs when a new game enters its Building state.
    /// <remarks>GameSettings is valid.</remarks>
    /// </summary>
    public event EventHandler newGameBuilding;

    public new bool IsApplicationQuiting { get { return AMonoBase.IsApplicationQuiting; } }

    private string _debugName;
    public string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(GetType().Name, InstanceCount);
            }
            return _debugName;
        }
    }

    private GameSettings _gameSettings;
    /// <summary>
    /// The settings for this game instance.
    /// </summary>
    public GameSettings GameSettings {
        get { return _gameSettings; }
        private set { SetProperty<GameSettings>(ref _gameSettings, value, "GameSettings"); }
    }

    private bool _isSceneLoading;
    /// <summary>
    /// Indicates whether a scene is in the process of loading. 
    /// <remarks>12.2.16 Value will be false when the first scene is loading as a result of the Application starting.</remarks>
    /// </summary>
    public bool IsSceneLoading {
        get { return _isSceneLoading; }
        private set { SetProperty<bool>(ref _isSceneLoading, value, "IsSceneLoading"); }
    }

    private bool _isPaused;
    public bool IsPaused {
        get { return _isPaused; }
        private set { SetProperty<bool>(ref _isPaused, value, "IsPaused", IsPausedPropChangedHandler); }
    }

    public IList<Player> AllPlayers { get; private set; }

    public Player UserPlayer { get; private set; }

    public IList<Player> AIPlayers { get { return AllPlayers.Where(p => !p.IsUser).ToList(); } }

    private SceneID _currentSceneID;
    /// <summary>
    /// The SceneID of the CurrentScene that is showing. 
    /// </summary>
    public SceneID CurrentSceneID {
        get { return _currentSceneID; }
        private set { SetProperty<SceneID>(ref _currentSceneID, value, "CurrentSceneID"); }
    }

    private SceneID _lastSceneID;
    /// <summary>
    /// The last SceneID that was showing before CurrentSceneID. If this is the
    /// first scene after initialization, LastSceneID will == CurrentSceneID.
    /// </summary>
    public SceneID LastSceneID {
        get { return _lastSceneID; }
        private set { SetProperty<SceneID>(ref _lastSceneID, value, "LastSceneID"); }
    }

    private bool _isRunning;
    /// <summary>
    /// Indicates whether the game is in GameState.Running.
    /// </summary>
    public bool IsRunning {
        get { return _isRunning; }
        private set { SetProperty<bool>(ref _isRunning, value, "IsRunning"); }
    }

    private PauseState _pauseState;
    /// <summary>
    /// Gets the PauseState of the game. Warning: IsPaused changes AFTER
    /// PauseState completes its changes and notifications.
    /// <remarks>To set use RequestPauseStateChange().</remarks>
    /// </summary>
    public PauseState PauseState {
        get { return _pauseState; }
        private set { SetProperty<PauseState>(ref _pauseState, value, "PauseState", PauseStatePropChangedHandler); }
    }

    /// <summary>
    /// The current GameState. 
    /// WARNING: Do not subscribe to this. 
    /// Use the events as I can find out who has subscribed to them.
    /// </summary>
    public override GameState CurrentState {
        get { return base.CurrentState; }
        protected set { base.CurrentState = value; }
    }

    private PlayerDesigns _playersDesigns;
    /// <summary>
    /// A collection of Element Designs for each player.
    /// </summary>
    public PlayerDesigns PlayersDesigns {
        get { return _playersDesigns; }
        private set { SetProperty<PlayerDesigns>(ref _playersDesigns, value, "PlayersDesigns"); }
    }

    private CelestialDesigns _celestialDesigns = new CelestialDesigns();
    /// <summary>
    /// A collection of CelestialDesigns (Stars, Planets and Moons).
    /// </summary>
    public CelestialDesigns CelestialDesigns { get { return _celestialDesigns; } }

    /// <summary>
    /// The User's AIManager instance.
    /// </summary>
    public UserPlayerAIManager UserAIManager { get { return _playerAiMgrLookup[UserPlayer] as UserPlayerAIManager; } }

    public AllKnowledge GameKnowledge { get; private set; }

    public override bool IsPersistentAcrossScenes { get { return true; } }

    public UniverseCreator UniverseCreator { get; private set; }

    private Scene CurrentScene {
        get {
            Scene scene = SceneManager.GetActiveScene();
            //D.Log("CurrentScene is {0}.", scene.name);
            D.AssertNotEqual(default(Scene), scene);
            return scene;
        }
    }

    private IDictionary<Player, PlayerAIManager> _playerAiMgrLookup;
    private IDictionary<GameState, HashSet<MonoBehaviour>> _gameStateProgressionReadinessLookup;
    private GameTime _gameTime;
    private PlayerPrefsManager _playerPrefsMgr;
    private JobManager _jobMgr;

    #region Initialization

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        // Note: Dummy InstanceCount as it does not get incremented until after InitializeOnInstance()
        //D.Log("{0}.{1}() called.", DebugNameFormat.Inject(GetType().Name, InstanceCount + 1), GetMethodName());
        GameReferences.GameManager = Instance;
        RefreshCurrentSceneID();
        RefreshLastSceneID();
        RefreshStaticReferences(isBeingInitialized: true);
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        InitializeGameStateProgressionReadinessSystem();
        // 9.19.16 Subscribe() moved to Start() to avoid receiving SceneLoaded event during startup. 
        // Previously, OnLevelWasLoaded() was not called during scene startup so this wasn't an issue.

        //TODO add choose language GUI
        //string language = "fr-FR";
        // ChangeLanguage(language);
    }

    #region Language

    private void ChangeLanguage(string language) {
        CultureInfo newCulture = new CultureInfo(language);
        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        D.Log("Current culture of thread is {0}.".Inject(Thread.CurrentThread.CurrentUICulture.DisplayName));
        D.Log("Current OS Language of Unity is {0}.".Inject(Application.systemLanguage.GetValueName()));
    }

    #endregion

    /// <summary>
    /// Refreshes the static fields of References.
    /// </summary>
    /// <param name="isBeingInitialized">if set to <c>true</c> this is being called from InitialzeOnInstance. Most Reference values will be null.</param>
    private void RefreshStaticReferences(bool isBeingInitialized = false) {
        // MonoBehaviour Singletons set their References field themselves when they are first called
#pragma warning disable 0168
        // HACK these two MonoBehaviour Singleton References fields get called immediately so make sure they are set
        var dummy1 = InputManager.Instance;
        var dummy2 = SFXManager.Instance;
#pragma warning restore 0168

        // Non-persistent (by definition) Generic Singletons need to be newly instantiated to refresh their References field
        if (!isBeingInitialized && LastSceneID != SceneID.LobbyScene) {
            // Note: These References will be null if being initialized or if LastScene was Lobby
            // Filter was needed to allow addition of Assert not null protection on References value gets
            (GameReferences.InputHelper as IDisposable).Dispose();
            (GameReferences.GeneralFactory as IDisposable).Dispose();
            (GameReferences.TrackingWidgetFactory as IDisposable).Dispose();
            (GameReferences.FormationGenerator as IDisposable).Dispose();
        }

        GameReferences.InputHelper = GameInputHelper.Instance;
        if (CurrentSceneID == SceneID.GameScene) {
            // not used in LobbyScene
            GameReferences.GeneralFactory = GeneralFactory.Instance;
            GameReferences.TrackingWidgetFactory = TrackingWidgetFactory.Instance;
            GameReferences.FormationGenerator = FormationGenerator.Instance;
        }

        // Non-persistent (by definition) non-Singleton MonoBehaviours need to be newly instantiated to refresh their References field
        if (CurrentSceneID == SceneID.GameScene) {
            // not used in LobbyScene
            GameReferences.HoverHighlight = EffectsFolder.Instance.Folder.gameObject.GetSingleComponentInChildren<SphericalHighlight>();
        }
    }

    private void InitializeValuesAndReferences() {
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameTime = GameTime.Instance;
        _jobMgr = JobManager.Instance;
        GameKnowledge = AllKnowledge.Instance;
        UniverseCreator = new UniverseCreator();
        _pauseState = PauseState.NotPaused; // initializes value without initiating change event
    }

    private void Subscribe() {
        SceneManager.sceneLoaded += SceneLoadedEventHandler;
    }

    #endregion

    protected override void Start() {
        base.Start();
        // 9.19.16 Subscribe() moved from InitializeOnAwake() to avoid receiving SceneLoaded event during startup.
        // Previously, OnLevelWasLoaded() was not called during scene startup so this wasn't an issue.
        Subscribe();
        enabled = false;    // 10.14.16 Added to keep Update() from starting until EnableGameTimeClock(true) called
    }

    /// <summary>
    /// Called by Loader to align the state machine with a LobbyScene startup.
    /// <remarks>10.5.16 Startup simulation has been replaced by Loader calling InitiateNewGame(isStartup: true)
    /// using a GameSettings generated by GameSettingsDebugControl using its editor settings.</remarks>
    /// </summary>
    public void LaunchInLobby() {
        D.AssertEqual(SceneID.LobbyScene, CurrentSceneID);
        // 1.17.17 ResetConditionsForGameStartup() not needed here as no game has yet been started to require reset
        CurrentState = GameState.Lobby;
    }

    #region GameState Progression Readiness System

    private Job _gameStateProgressCheckJob;

    private void InitializeGameStateProgressionReadinessSystem() {
        _gameStateProgressionReadinessLookup = new Dictionary<GameState, HashSet<MonoBehaviour>>(GameStateEqualityComparer.Default);
        _gameStateProgressionReadinessLookup.Add(GameState.None, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Lobby, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Loading, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Building, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Restoring, new HashSet<MonoBehaviour>());
        //_gameStateProgressionReadinessLookup.Add(GameState.Waiting, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DeployingSystemCreators, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildingSystems, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.GeneratingPathGraphs, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DesigningInitialUnits, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildingAndDeployingInitialUnits, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.PreparingToRun, new HashSet<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Running, new HashSet<MonoBehaviour>());
    }

    private void StartGameStateProgressionReadinessChecks() {
        D.Assert(!IsPaused, "Should not be paused.");
        //D.Log("{0} is preparing to start GameState Progression System Readiness Checks.", DebugName);
        __ValidateGameStateProgressionReadinessSystemState();
        string jobName = TempGameValues.__GameMgrProgressCheckJobName;
        _gameStateProgressCheckJob = _jobMgr.StartNonGameplayJob(AssessReadinessToProgressGameState(), jobName, jobCompleted: (jobWasKilled) => {
            if (jobWasKilled) {
                // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
            }
            else {
                _gameStateProgressCheckJob = null;
                if (CurrentState != GameState.Running) {
                    D.Error("{0}.{1} = {2}.", DebugName, typeof(GameState).Name, CurrentState.GetValueName());
                }
                //D.Log("{0}'s GameState Progression Readiness System has successfully completed.", DebugName);
            }
        });
    }

    private void __ValidateGameStateProgressionReadinessSystemState() {
        var keys = _gameStateProgressionReadinessLookup.Keys;
        keys.ForAll(k => D.AssertEqual(Constants.Zero, _gameStateProgressionReadinessLookup[k].Count));
    }

    private IEnumerator AssessReadinessToProgressGameState() {
        //D.Log("Entering AssessReadinessToProgressGameState.");
        while (CurrentState == GameState.Running) {
            // starting progression checks in Running so progress out of Running before proceeding
            if (CheckReadinessToProgressGameState()) {
                //D.Log("State prior to ProgressState = {0}.", CurrentState.GetValueName());
                ProgressState();
                //D.Log("State after ProgressState = {0}.", CurrentState.GetValueName());
            }
            yield return null;
        }

        while (CurrentState != GameState.Running) {
            if (CheckReadinessToProgressGameState()) {
                //D.Log("State prior to ProgressState = {0}.", CurrentState.GetValueName());
                ProgressState();
                //D.Log("State after ProgressState = {0}.", CurrentState.GetValueName());
            }
            yield return null;
        }
        //D.Log("Exiting AssessReadinessToProgressGameState.");
    }

    private bool CheckReadinessToProgressGameState() {
        D.Assert(_gameStateProgressionReadinessLookup.ContainsKey(CurrentState), CurrentState.GetValueName());
        // this will tell me what state failed, whereas failing while accessing the dictionary won't
        //D.Log("{0}.CheckReadinessToProgressGameState() called. GameState = {1}, UnreadyElements count = {2}.", DebugName, CurrentState.GetValueName(), _gameStateProgressionReadinessLookup[CurrentState].Count);
        return _gameStateProgressionReadinessLookup[CurrentState].Count == Constants.Zero;
    }

    public void RecordGameStateProgressionReadiness(MonoBehaviour source, GameState maxGameStateUntilReady, bool isReady) {
        HashSet<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[maxGameStateUntilReady];
        if (!isReady) {
            if (unreadyElements.Contains(source)) {
                D.Error("UnreadyElements for {0} already has {1} registered!", maxGameStateUntilReady.GetValueName(), source.name);
            }
            unreadyElements.Add(source);
            //D.Log("{0} has registered as unready to progress beyond {1}. UnreadyElement Count = {2}.", source.name, maxGameStateUntilReady.GetValueName(), unreadyElements.Count);
        }
        else {
            bool isRemoved = unreadyElements.Remove(source);
            if (!isRemoved) {
                D.Error("UnreadyElements for {0} has no record of {1}!", maxGameStateUntilReady.GetValueName(), source.name);
            }
            //D.Log("{0} is now ready to progress beyond {1}. Remaining unready elements: {2}.",
            //source.name, maxGameStateUntilReady.GetValueName(), unreadyElements.Any() ? unreadyElements.Select(m => m.gameObject.name).Concatenate() : "None");
        }
    }

    private void KillGameStateProgressionCheckJob() {
        if (_gameStateProgressCheckJob != null) {
            _gameStateProgressCheckJob.Kill();
            _gameStateProgressCheckJob = null;
        }
    }

    #endregion

    #region Players

    private void InitializePlayers() {
        //D.Log("{0} is initializing Players.", DebugName);

        IList<Player> allPlayers = new List<Player>(GameSettings.AIPlayers);
        allPlayers.Add(GameSettings.UserPlayer);
        AllPlayers = allPlayers;
        UserPlayer = GameSettings.UserPlayer;

        PlayersDesigns = new PlayerDesigns(AllPlayers);
    }

    #endregion

    /// <summary>
    /// Gets <c>player</c>'s AIManager instance.
    /// </summary>
    public PlayerAIManager GetAIManagerFor(Player player) {
        return _playerAiMgrLookup[player];
    }

    private void InitializePlayerAIManagers() {
        if (_playerAiMgrLookup == null) {
            _playerAiMgrLookup = new Dictionary<Player, PlayerAIManager>(AllPlayers.Count);
        }
        _playerAiMgrLookup.Clear();

        var uCenter = UniverseCreator.UniverseCenter;
        IEnumerable<IStar_Ltd> allStars = GameKnowledge.Stars.Cast<IStar_Ltd>();
        AllPlayers.ForAll(player => {
            PlayerAIManager plyrAiMgr;
            if (DebugControls.Instance.IsAllIntelCoverageComprehensive) {
                IEnumerable<IPlanetoid_Ltd> allPlanetoids = GameKnowledge.Planetoids.Cast<IPlanetoid_Ltd>();
                //D.Log("{0}: GameKnowledge knows about {1} planetoids. {2}.",
                //Name, allPlanetoids.Count(), allPlanetoids.Select(p => p.DebugName).Concatenate());
                if (player.IsUser) {
                    UserPlayerKnowledge plyrKnowledge = new UserPlayerKnowledge(uCenter, allStars, allPlanetoids);
                    plyrAiMgr = new UserPlayerAIManager(plyrKnowledge);
                }
                else {
                    PlayerKnowledge plyrKnowledge = new PlayerKnowledge(player, uCenter, allStars, allPlanetoids);
                    plyrAiMgr = new PlayerAIManager(player, plyrKnowledge);
                }
            }
            else {
                if (player.IsUser) {
                    UserPlayerKnowledge plyrKnowledge = new UserPlayerKnowledge(uCenter, allStars);
                    plyrAiMgr = new UserPlayerAIManager(plyrKnowledge);
                }
                else {
                    PlayerKnowledge plyrKnowledge = new PlayerKnowledge(player, uCenter, allStars);
                    plyrAiMgr = new PlayerAIManager(player, plyrKnowledge);
                }
            }
            _playerAiMgrLookup.Add(player, plyrAiMgr);
        });
    }

    #region Event and Property Change Handlers

    private void SceneLoadedEventHandler(Scene scene, LoadSceneMode mode) {
        //D.Log("{0}.SceneLoadedEventHandler({1}, {2}) called.", DebugName, scene.name, mode);
        HandleSceneLoaded(scene);
    }

    // void OnLevelWasLoaded(level) deprecated by Unity 5.4.1. Replaced it with SceneLoadedEventHandler()

    private void PauseStatePropChangedHandler() {
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

    private void IsPausedPropChangedHandler() {
        D.Log("{0}.IsPaused changed to {1}.", DebugName, IsPaused);
        OnIsPausedChanged();
        if (IsPaused) {
            RunGarbageCollector();
        }
    }

    private void OnIsPausedChanged() {
        if (isPausedChanged != null) {
            isPausedChanged(this, EventArgs.Empty);
        }
    }

    private void OnIsReadyForPlay() {
        D.Assert(IsRunning);
        if (isReadyForPlayOneShot != null) {
            //var targetNames = isReadyForPlayOneShot.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending isReadyForPlay event to {1}.", DebugName, targetNames.Concatenate());
            isReadyForPlayOneShot(this, EventArgs.Empty);
            isReadyForPlayOneShot = null;
        }
    }

    private void OnSceneLoading() {
        D.Assert(IsSceneLoading);
        if (sceneLoading != null) {
            sceneLoading(this, EventArgs.Empty);
        }
    }

    private void OnSceneLoaded() {
        D.Assert(!IsSceneLoading);
        if (sceneLoaded != null) {
            //D.Log("{0}.sceneLoaded event is firing.", DebugName);
            sceneLoaded(this, EventArgs.Empty);
        }
    }

    private void OnGameStateChanging() {
        if (gameStateChanging != null) {
            //var targetNames = gameStateChanging.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending gameStateChanging event to {1}.", DebugName, targetNames.Concatenate());
            gameStateChanging(this, EventArgs.Empty);
        }
    }

    private void OnGameStateChanged() {
        if (gameStateChanged != null) {
            //var targetNames = gameStateChanged.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending gameStateChanged event to {1}.", DebugName, targetNames.Concatenate());
            gameStateChanged(this, EventArgs.Empty);
        }
    }

    private void OnNewGameBuilding() {
        if (newGameBuilding != null) {
            newGameBuilding(this, EventArgs.Empty);
        }
    }

    #endregion

    /// <summary>
    /// Handles the scene loaded.
    /// <remarks>Replaces OnLevelWasLoaded() which Unity 5.4.1 deprecated.</remarks>
    /// </summary>
    /// <param name="scene">The scene.</param>
    private void HandleSceneLoaded(Scene scene) {
        D.Assert(!IsExtraCopy);
        D.AssertEqual(CurrentScene, scene);
        D.AssertNotDefault((int)CurrentState);   // if subscribed too early, can be called when GameState = None
        //D.Log("{0}.HandleSceneLoaded({1}) called. Current State = {2}.", DebugName, scene.name, CurrentState.GetValueName());
        RefreshCurrentSceneID();
        RefreshStaticReferences();
        UponSceneLoaded(scene);
        RunGarbageCollector();
    }

    #region Game Time Controls

    private void PrepareGameTimeForNewGame() {
        _gameTime.PrepareToBeginNewGame();
    }

    private void EnableGameTimeClock(bool toEnable) {
        //D.Log(toEnable, "{0} is starting the game clock. Frame = {1}.", DebugName, Time.frameCount);
        if (toEnable) {
            _gameTime.PrepareForClockEnabled();
        }
        enabled = toEnable; // controls Update() which calls _gameTime.CheckForDateChange()
    }

    protected override void Update() {
        base.Update();
        _gameTime.CheckForDateChange(); // CheckForDateChange() will ignore the call if the game is paused
    }

    #endregion

    #region New Game

    public void InitiateNewGame(GameSettings gameSettings) {
        //D.Log("{0}.InitiateNewGame() called.", DebugName);
        GameSettings = gameSettings;

        ResetConditionsToBeginLoadingNewGame();    // 1.17.17 moved here to catch all NewGame starts except from Lobby

        StartGameStateProgressionReadinessChecks(); // Begins progression from either Lobby or Running

        if (!gameSettings.__IsStartup) {
            // UNDONE when I allow in game return to Lobby, I'll need to call this just before I use SceneMgr to load the LobbyScene
            RefreshLastSceneID();
        }
    }

    #endregion

    #region Saving and Restoring

    public void SaveGame(string gameName) {
        D.Warn("{0}.SaveGame() not currently implemented.", DebugName);
    }

    public void LoadSavedGame(string gameID) {
        D.Warn("{0}.LoadSavedGame() not currently implemented.", DebugName);
    }

    #region WhyDoIDoIt.UnitySerializer Save/Restore Archive

    //public void SaveGame(string gameName) {
    //    GameSettings.IsSavedGame = true;
    //    GameSettings.__IsStartupSimulation = false;
    //    _gameTime.PrepareToSaveGame();
    //    LevelSerializer.SaveGame(gameName);
    //}

    //public void LoadSavedGame(string gameID) {
    //    LoadAndRestoreSavedGame(gameID);
    //}

    //private void LoadAndRestoreSavedGame(string gameID) {
    //    var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
    //    var gamesWithID = from g in savedGames where g.Caption == gameID select g;
    //    if (gamesWithID.IsNullOrEmpty<LevelSerializer.SaveEntry>()) {
    //        D.Warn("No saved game matches selected game caption {0}. \nLoad Saved Game not currently implemented.", gameID);
    //        return;
    //    }

    //    // HACK to deal with multiple games with the same caption, i.e. saved the same minute
    //    var idArray = gamesWithID.ToArray<LevelSerializer.SaveEntry>();
    //    LevelSerializer.SaveEntry selectedGame = null;
    //    if (idArray.Length > 1) {
    //        selectedGame = idArray[0];
    //    }
    //    else {
    //        selectedGame = gamesWithID.Single<LevelSerializer.SaveEntry>();
    //    }

    //    CurrentState = GameState.Loading;
    //    selectedGame.Load();
    //}

    //protected override void OnDeserialized() {
    //    RelayToCurrentState();
    //}

    #endregion

    #endregion

    /// <summary>
    /// Resets any conditions required for normal game startup. For instance, PauseState
    /// is normally NotPaused while setting up the first game. This may or may not be the
    /// current state of PauseState given the numerous ways one can initiate the startup
    /// of a game instance.
    /// </summary>
    private void ResetConditionsToBeginLoadingNewGame() {
        if (IsPaused) {
            RequestPauseStateChange(toPause: false, toOverride: true);
        }
        if (_playerAiMgrLookup != null) {
            _playerAiMgrLookup.Values.ForAll(pAiMgr => pAiMgr.Dispose());
        }
        GameKnowledge.Reset();
        CelestialDesigns.Reset();

        UniverseCreator.Reset();
    }

    private void RefreshCurrentSceneID() {
        CurrentSceneID = Enums<SceneID>.Parse(CurrentScene.name);
    }

    private void RefreshLastSceneID() {
        LastSceneID = CurrentSceneID;
    }

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

    #region GameState State Machine

    protected override void CurrentStatePropChangingHandler(GameState incomingState) {
        base.CurrentStatePropChangingHandler(incomingState);
        OnGameStateChanging();
    }
    protected override void CurrentStatePropChangedHandler() {
        base.CurrentStatePropChangedHandler();
        OnGameStateChanged();
    }

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

    #region None

    // 12.8.16 This is the state when the Editor starts up. Its been added here as a result of  
    // allowing the GameStateProgressionReadiness System to have full control of GameState transitions which 
    // builds in a minimum dwell time in each state of 1 frame. This came about as a result of recycling
    // Jobs which don't complete until 1 frame after they are killed.

    void None_EnterState() {
        LogEvent();
        // No use of RecordGameStateProgressionReadiness() as this state will only exist in the editor
    }

    void None_UponProgressState() {
        CurrentState = GameState.Loading;
    }

    void None_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.Loading, CurrentState);
    }

    #endregion

    #region Lobby

    void Lobby_EnterState() {
        LogEvent();
        RecordGameStateProgressionReadiness(Instance, GameState.Lobby, isReady: false);
        FpsReadout.Instance.IsReadoutToShow = true;
        RecordGameStateProgressionReadiness(Instance, GameState.Lobby, isReady: true);
    }

    void Lobby_UponProgressState() {
        LogEvent();
        CurrentState = GameState.Loading;
    }

    void Lobby_ExitState() {
        LogEvent();
        // Transitioning to Loading (the level) whether a new or saved game
        FpsReadout.Instance.IsReadoutToShow = false;
        D.AssertEqual(GameState.Loading, CurrentState);
    }

    #endregion

    // 8.9.16 Loading, Building, Restoring will need to be re-engineered when I re-introduce persistence

    #region Loading

    // 3.8.17 WARNING: The KEY to a graceful shutdown of an existing scene when transitioning to a new scene is to
    // rely on Unity to disable then destroy (and thus Cleanup()) existing scene gameObjects. That graceful
    // transition is started by SceneManager.LoadScene(). Any delay between Running_ExitState() and 
    // Loading_EnterState() creates opportunities for asynchronous events (Trigger events, Job completions) 
    // to occur during the delay. Making Loading_EnterState an IEnumerable creates such a delay.

    void Loading_EnterState() {  // 3.8.17 IEnumerator to create 1 frame delay to separate SceneUnloading from SceneLoading events
        LogEvent();
        __RecordDurationStartTime();
        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: false);

        if (GameSettings.__IsStartup) {
            RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: true);
            // no need to reload the scene that has just been loaded
        }
        else {
            //D.Log("SceneManager.LoadScene({0}) being called.", SceneID.GameScene.GetValueName());
            SceneManager.LoadScene(SceneID.GameScene.GetValueName(), LoadSceneMode.Single); //Application.LoadLevel(index) deprecated by Unity 5.3

            // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
            IsSceneLoading = true;
            OnSceneLoading();
        }
    }

    [Obsolete]
    void Loading_UponLevelLoaded(int level) {
        LogEvent();
        D.Assert(!GameSettings.__IsStartup);

        D.AssertEqual(SceneID.GameScene, CurrentSceneID, CurrentSceneID.GetValueName());
        IsSceneLoading = false;
        OnSceneLoaded();

        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: true);
    }

    void Loading_UponSceneLoaded(Scene scene) {
        LogEvent();
        D.Assert(!GameSettings.__IsStartup);
        D.AssertEqual(CurrentScene, scene);
        D.AssertEqual(SceneID.GameScene, CurrentSceneID, CurrentSceneID.GetValueName());
        IsSceneLoading = false;
        OnSceneLoaded();

        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: true);
    }

    void Loading_UponProgressState() {
        LogEvent();
        ////CurrentState = (LevelSerializer.IsDeserializing || GameSettings.IsSavedGame) ? GameState.Restoring : GameState.Building;
        CurrentState = GameState.Building;
    }

    void Loading_ExitState() {
        LogEvent();
        // Transitioning to Building if new game, or Restoring if saved game
        D.Assert(CurrentState == GameState.Building || CurrentState == GameState.Restoring);
        __LogDuration();
    }

    #endregion

    #region Building

    void Building_EnterState() {
        LogEvent();
        __RecordDurationStartTime();

        RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: false);

        // Building is only for new (or startup) games
        D.Assert(!GameSettings.IsSavedGame);

        _jobMgr.PreloadJobsDeterminedBy(GameSettings);

        OnNewGameBuilding();
        PrepareGameTimeForNewGame();    // Done here as this state is unique to new or simulated games
        GamePoolManager.Instance.Initialize(GameSettings);

        InitializePlayers();
        UniverseCreator.InitializeUniverseCenter(); // can't be earlier as Players are checked when Data is assigned
        GameKnowledge.Initialize(UniverseCreator.UniverseCenter);
        UniverseCreator.BuildSectors();             // can't be earlier as Players are checked when Data is assigned

        RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: true);
    }

    void Building_UponProgressState() {
        LogEvent();
        CurrentState = GameState.DeployingSystemCreators;
    }

    void Building_ExitState() {
        LogEvent();
        // Building is only for new (or startup) games, so next state is DeployingSystemCreators
        D.AssertEqual(GameState.DeployingSystemCreators, CurrentState);
        __LogDuration();
    }

    #endregion

    #region Restoring

    void Restoring_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
        RecordGameStateProgressionReadiness(Instance, GameState.Restoring, isReady: false);
    }

    #region WhyDoIDoIt.UnitySerializer Restore Archive

    //void Restoring_OnDeserialized() {
    //    LogEvent();
    //    D.Assert(GameSettings.IsSavedGame && !GameSettings.__IsStartupSimulation);

    //    ResetConditionsForGameStartup();
    //    _gameTime.PrepareToResumeSavedGame();

    //    RecordGameStateProgressionReadiness(Instance, GameState.Restoring, isReady: true);
    //}

    #endregion

    void Restoring_UponProgressState() {
        LogEvent();
        CurrentState = GameState.DeployingSystemCreators;
    }

    void Restoring_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.DeployingSystemCreators, CurrentState);
        __LogDuration();
    }

    #endregion

    #region DeployingSystemCreators

    void DeployingSystemCreators_EnterState() {
        LogEvent();
        __RecordDurationStartTime();

        RecordGameStateProgressionReadiness(Instance, GameState.DeployingSystemCreators, isReady: false);
        UniverseCreator.DeployAndConfigureSystemCreators();
        RecordGameStateProgressionReadiness(Instance, GameState.DeployingSystemCreators, isReady: true);
    }

    void DeployingSystemCreators_UponProgressState() {
        LogEvent();
        CurrentState = GameState.BuildingSystems;
    }

    void DeployingSystemCreators_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.BuildingSystems, CurrentState);
        __LogDuration();
    }

    #endregion

    #region BuildingSystems

    void BuildingSystems_EnterState() {
        LogEvent();
        __RecordDurationStartTime();

        RecordGameStateProgressionReadiness(Instance, GameState.BuildingSystems, isReady: false);
        UniverseCreator.BuildSystems();
        InitializePlayerAIManagers();   // Requires UCenter, Systems built and present in GameKnowledge
        RecordGameStateProgressionReadiness(Instance, GameState.BuildingSystems, isReady: true);
    }

    void BuildingSystems_UponProgressState() {
        LogEvent();
        CurrentState = GameState.GeneratingPathGraphs;
    }

    void BuildingSystems_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.GeneratingPathGraphs, CurrentState);
        __LogDuration();
    }

    #endregion

    #region GeneratingPathGraphs

    void GeneratingPathGraphs_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void GeneratingPathGraphs_UponProgressState() {
        LogEvent();
        CurrentState = GameState.DesigningInitialUnits;
    }

    void GeneratingPathGraphs_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.DesigningInitialUnits, CurrentState);
        __LogDuration();
    }

    #endregion

    #region DesigningInitialUnits

    void DesigningInitialUnits_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void DesigningInitialUnits_UponProgressState() {
        LogEvent();
        CurrentState = GameState.BuildingAndDeployingInitialUnits;
    }

    void DesigningInitialUnits_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.BuildingAndDeployingInitialUnits, CurrentState);
        __LogDuration();
    }

    #endregion

    #region BuildingAndDeployingInitialUnits

    void BuildingAndDeployingInitialUnits_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
        RecordGameStateProgressionReadiness(Instance, GameState.BuildingAndDeployingInitialUnits, isReady: false);
        UniverseCreator.DeployAndConfigureInitialUnitCreators();
        UniverseCreator.BuildAndPositionUnits();
        RecordGameStateProgressionReadiness(Instance, GameState.BuildingAndDeployingInitialUnits, isReady: true);
    }

    void BuildingAndDeployingInitialUnits_UponProgressState() {
        LogEvent();
        CurrentState = GameState.PreparingToRun;
    }

    void BuildingAndDeployingInitialUnits_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.PreparingToRun, CurrentState);
        __LogDuration();
    }

    #endregion

    #region PreparingToRun

    void PreparingToRun_EnterState() {
        LogEvent();
        __RecordDurationStartTime();

        RecordGameStateProgressionReadiness(Instance, GameState.PreparingToRun, isReady: false);
        UniverseCreator.CompleteInitializationOfAllCelestialItems();
        MainCameraControl.Instance.PrepareForActivation(GameSettings);
        RecordGameStateProgressionReadiness(Instance, GameState.PreparingToRun, isReady: true);
    }

    void PreparingToRun_UponProgressState() {
        LogEvent();
        CurrentState = GameState.Running;
    }

    void PreparingToRun_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.Running, CurrentState);
        __LogDuration();
    }

    #endregion

    #region Running

    void Running_EnterState() {
        LogEvent();
        RecordGameStateProgressionReadiness(Instance, GameState.Running, isReady: false);

        IsRunning = true;   // Note: My practice - IsRunning THEN pause changes
        // 10.14.16 moved IsPauseonLoadEnabled later in EnterState

        __RecordDurationStartTime();
        UniverseCreator.CommenceOperationOfAllCelestialItems(); // before units so units detect 'operational' celestial objects
        __LogDuration("{0}.CommenceOperationOfAllCelestialItems()".Inject(typeof(UniverseCreator).Name));
        __RecordDurationStartTime();
        UniverseCreator.CommenceUnitOperationsOnDeployDate();   // > 1 sec
        __LogDuration("{0}.CommenceUnitOperationsOnDeployDate()".Inject(typeof(UniverseCreator).Name));

        RunGarbageCollector();

        /************************************** TEMP Job *******************************************************************/
        __RecordDurationStartTime();
        _jobMgr.WaitWhile(() => FpsReadout.Instance.FramesPerSecond < TempGameValues.MinimumFramerate, "WaitToInitiateGameplayJob", isPausable: false, waitFinished: (jobWasKilled) => {
            __LogDuration("{0}.WaitToInitiateGameplay".Inject(DebugName));
            __InitiateGameplay();

            __ShowGameplay();

            if (DebugControls.Instance.IsAutoPauseChangesEnabled) {
                __InitializeAutoPauseChgSystem();
            }
        });
        /*******************************************************************************************************************/
    }

    private void __InitiateGameplay() {
        EnableGameTimeClock(true);
        _playerAiMgrLookup.Values.ForAll(aiMgr => aiMgr.CommenceOperations());
    }

    private void __ShowGameplay() {
        __RecordDurationStartTime();
        _jobMgr.WaitWhile(() => FpsReadout.Instance.FramesPerSecond < TempGameValues.MinimumFramerate, "WaitToShowGameplayJob", isPausable: false, waitFinished: (jobWasKilled) => {
            __LogDuration("{0}.WaitToShowGameplay".Inject(DebugName));
            MainCameraControl.Instance.Activate();

            FpsReadout.Instance.IsReadoutToShow = true;

            OnIsReadyForPlay();
            Debug.LogFormat("{0}: Game is now ready to show GamePlay. Frame = {1}. FPS = {2:0.#}.", DebugName, Time.frameCount, FpsReadout.Instance.FramesPerSecond);

            if (_playerPrefsMgr.IsPauseOnLoadEnabled) { // Note: My practice - IsRunning THEN pause changes
                RequestPauseStateChange(toPause: true, toOverride: true);
            }

            UniverseCreator.AttemptFocusOnPrimaryUserUnit();
            RecordGameStateProgressionReadiness(Instance, GameState.Running, isReady: true);
        });
    }

    void Running_UponProgressState() {
        LogEvent();
        CurrentState = GameState.Loading;
    }

    void Running_ExitState() {
        LogEvent();
        D.AssertEqual(GameState.Loading, CurrentState);
        FpsReadout.Instance.IsReadoutToShow = false;
        __KillAutoPauseChgJob();

        EnableGameTimeClock(false);
        IsRunning = false;
    }

    #endregion

    #region State Machine Support Methods

    private void ProgressState() { UponProgressState(); }

    private void UponProgressState() { RelayToCurrentState(); }

    [Obsolete]
    private void UponLevelLoaded(int level) { RelayToCurrentState(level); }

    private void UponSceneLoaded(Scene scene) { RelayToCurrentState(scene); }

    #endregion

    #endregion

    public void ExitGame() {
        Shutdown();
    }  //TODO Confirmation Dialog

    private void Shutdown() {
        _playerPrefsMgr.Store();
        Application.Quit(); // ignored inside Editor or WebPlayer
    }

    #region Cleanup

    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        _gameTime.DateMinder.__ReportUsage();
        _gameTime.RecurringDateMinder.__ReportUsage();
        AWeapon.__ReportPeakEnemiesInRange();
        ActiveCountermeasure.__ReportPeakThreatsInRange();
        UnifiedSRSensorMonitor.__ReportUsage();
    }

    private void RunGarbageCollector() {
        __RecordDurationStartTime();
        GC.Collect(0);  // http://gamedev.stackexchange.com/questions/25394/how-should-i-account-for-the-gc-when-building-games-with-unity
        __LogDuration("Garbage Collection");
    }

    protected override void Cleanup() {
        // 12.8.16 Job Disposal centralized in JobManager
        KillGameStateProgressionCheckJob();
        if (_playerAiMgrLookup != null) {
            _playerAiMgrLookup.Values.ForAll(pAiMgr => pAiMgr.Dispose());
        }
        GameKnowledge.Dispose();
        Unsubscribe();

        DisposeOfGlobals();
        GameReferences.GameManager = null;  // last, as Globals may use it when disposing
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// <remarks>Globals here refers to GenericSingletons that once instantiated,
    /// hang around thru new game transitions until Disposed here.</remarks>
    /// </summary>
    private void DisposeOfGlobals() {
        _gameTime.Dispose();
        GameInputHelper.Instance.Dispose();

        if (CurrentSceneID == SceneID.GameScene) {
            // not used in LobbyScene
            GeneralFactory.Instance.Dispose();
            TrackingWidgetFactory.Instance.Dispose();
            PlayerViews.Instance.Dispose();
            SelectionManager.Instance.Dispose();
            LeaderFactory.Instance.Dispose();
            SystemNameFactory.Instance.Dispose();
            FormationGenerator.Instance.Dispose();
        }
    }

    private void Unsubscribe() {
        SceneManager.sceneLoaded -= SceneLoadedEventHandler;
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    #region Debug Auto Pause Change System

    private Job __autoPauseChgJob;

    private void __InitializeAutoPauseChgSystem() {
        GameDate startDate = new GameDate(new GameTimeDuration(0F, days: RandomExtended.Range(1, 3)));
        GameTimeDuration durationBetweenChgs = new GameTimeDuration(hours: UnityEngine.Random.Range(0F, 10F), days: RandomExtended.Range(3, 5));
        D.LogBold("{0}: Initiating Auto Pause Changes beginning {1} with changes every {2}.", DebugName, startDate, durationBetweenChgs);
        __autoPauseChgJob = _jobMgr.WaitForDate(startDate, "AutoPauseChgStartJob", waitFinished: (jobWasKilled) => {
            if (jobWasKilled) {

            }
            else {
                __CyclePaused();
                __autoPauseChgJob = _jobMgr.RecurringWaitForHours(durationBetweenChgs, "AutoPauseChgRecurringJob", waitMilestone: () => {
                    __CyclePaused();
                });
            }
        });
    }

    private void __CyclePaused() {
        if (!IsPaused) {
            IsPaused = true;
            IsPaused = false;
        }
    }

    private void __KillAutoPauseChgJob() {
        if (__autoPauseChgJob != null) {
            __autoPauseChgJob.Kill();
            __autoPauseChgJob = null;
        }
    }

    #endregion


    protected override string __DurationLogIntroText { get { return "{0}.{1}".Inject(typeof(GameState).Name, LastState); } }

    #endregion


}
