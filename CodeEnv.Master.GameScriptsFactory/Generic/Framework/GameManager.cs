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
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton. The main manager for the game, implemented as a mono state machine.
/// </summary>
public class GameManager : AFSMSingleton_NoCall<GameManager, GameState>, IGameManager {

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
    public event EventHandler isRunningOneShot;

    /// <summary>
    /// Occurs just before a scene starts loading.
    /// Note: Event is not fired when the first scene is about to start loading as a result of the Application starting.
    /// </summary>
    public event EventHandler sceneLoading;

    /// <summary>
    /// Occurs just after a scene finishes loading, aka immediately after OnLevelWasLoaded is received.
    /// Note: Event is not fired when the first scene is loaded as a result of the Application starting.
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
    /// </summary>
    public event EventHandler newGameBuilding;

    private GameSettings _gameSettings;
    /// <summary>
    /// The settings for this game instance.
    /// Warning: Values like UniverseSize and SystemDensity are better accessed from
    /// DebugControls as it picks between this GameSetting value and the debug value set in the editor.
    /// </summary>
    public GameSettings GameSettings {
        get { return _gameSettings; }
        private set { SetProperty<GameSettings>(ref _gameSettings, value, "GameSettings"); }
    }

    private bool _isSceneLoading;
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
    /// Indicates whether the game is in GameState.Running or not.
    /// </summary>
    public bool IsRunning {
        get { return _isRunning; }
        private set { SetProperty<bool>(ref _isRunning, value, "IsRunning", IsRunningPropChangedHandler); }
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

    private PlayersDesigns _playersDesigns;
    /// <summary>
    /// A collection of Element Designs for each player.
    /// </summary>
    public PlayersDesigns PlayersDesigns {
        get { return _playersDesigns; }
        set { SetProperty<PlayersDesigns>(ref _playersDesigns, value, "PlayersDesigns"); }
    }

    /// <summary>
    /// The User's AIManager instance.
    /// </summary>
    public UserPlayerAIManager UserAIManager { get { return _playerAiMgrLookup[UserPlayer] as UserPlayerAIManager; } }

    public AllKnowledge GameKnowledge { get; private set; }

    public override bool IsPersistentAcrossScenes { get { return true; } }

    private Scene CurrentScene {
        get {
            Scene scene = SceneManager.GetActiveScene();
            //D.Log("CurrentScene is {0}.", scene.name);
            D.Assert(scene != default(Scene));
            return scene;
        }
    }

    private NewGameUnitConfigurator _newGameUnitConfigurator;
    private IDictionary<Player, PlayerAIManager> _playerAiMgrLookup;
    private IDictionary<GameState, IList<MonoBehaviour>> _gameStateProgressionReadinessLookup;
    private GameTime _gameTime;
    private PlayerPrefsManager _playerPrefsMgr;
    private JobManager _jobMgr;

    #region Initialization

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.GameManager = Instance;
        RefreshCurrentSceneID();
        RefreshLastSceneID();
        RefreshStaticReferences(isBeingInitialized: true);
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        InitializeGameStateProgressionReadinessSystem();
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
            (References.InputHelper as IDisposable).Dispose();
            (References.GeneralFactory as IDisposable).Dispose();
            (References.TrackingWidgetFactory as IDisposable).Dispose();
            (References.FormationGenerator as IDisposable).Dispose();
        }

        References.InputHelper = GameInputHelper.Instance;
        if (CurrentSceneID == SceneID.GameScene) {
            // not used in LobbyScene
            References.GeneralFactory = GeneralFactory.Instance;
            References.TrackingWidgetFactory = TrackingWidgetFactory.Instance;
            References.FormationGenerator = FormationGenerator.Instance;
        }

        // Non-persistent (by definition) non-Singleton MonoBehaviours need to be newly instantiated to refresh their References field
        if (CurrentSceneID == SceneID.GameScene) {
            // not used in LobbyScene
            References.HoverHighlight = EffectsFolder.Instance.Folder.gameObject.GetSingleComponentInChildren<SphericalHighlight>();
        }
    }

    private void InitializeValuesAndReferences() {
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _gameTime = GameTime.Instance;
        _jobMgr = JobManager.Instance;
        GameKnowledge = AllKnowledge.Instance;
        _pauseState = PauseState.NotPaused; // initializes value without initiating change event
    }

    #endregion

    protected override void Start() {
        base.Start();
        __SimulateStartup();
    }

    #region Startup Simulation

    private void __SimulateStartup() {
        //D.Log("{0}{1}.SimulateStartup() called.", GetType().Name, InstanceCount);
        if (CurrentSceneID == SceneID.LobbyScene) {
            CurrentState = GameState.Lobby;
        }
        else {
            GameSettings = __CreateStartupSimulationGameSettings();
            CurrentState = GameState.Lobby;
            CurrentState = GameState.Loading;
            // CurrentState progression occurs automatically from here
        }
    }

    private GameSettings __CreateStartupSimulationGameSettings() {
        var universeSize = _playerPrefsMgr.UniverseSizeSelection.Convert();
        var systemDensity = _playerPrefsMgr.SystemDensitySelection.Convert();
        var userPlayerColor = _playerPrefsMgr.UserPlayerColor;
        IList<GameColor> unusedPlayerColors = TempGameValues.AllPlayerColors.Except(userPlayerColor).ToList();

        int playerCount = _playerPrefsMgr.PlayerCount;
        int aiPlayerCount = playerCount - 1;
        Player[] aiPlayers = new Player[aiPlayerCount];
        for (int i = 0; i < aiPlayerCount; i++) {
            var aiSpecies = Enums<Species>.GetRandom(excludeDefault: true);

            var aiColor = Enums<GameColor>.GetRandomFrom(unusedPlayerColors);
            unusedPlayerColors.Remove(aiColor);

            IQ aiIQ = Enums<IQ>.GetRandom(excludeDefault: true);
            TeamID aiTeam = (TeamID)(i + 2);
            SpeciesStat aiSpeciesStat = SpeciesFactory.Instance.MakeInstance(aiSpecies);
            LeaderStat aiLeaderStat = LeaderFactory.Instance.MakeInstance(aiSpecies);
            aiPlayers[i] = new Player(aiSpeciesStat, aiLeaderStat, aiIQ, aiTeam, aiColor);
        }

        var userPlayerSpecies = _playerPrefsMgr.UserPlayerSpeciesSelection.Convert();
        var userPlayerSpeciesStat = SpeciesFactory.Instance.MakeInstance(userPlayerSpecies);
        var userPlayerLeaderStat = LeaderFactory.Instance.MakeInstance(userPlayerSpecies);
        Player userPlayer = new Player(userPlayerSpeciesStat, userPlayerLeaderStat, IQ.None, TeamID.Team_1, userPlayerColor, isUser: true);
        var gameSettings = new GameSettings {
            __IsStartupSimulation = true,
            UniverseSize = universeSize,
            SystemDensity = systemDensity,
            PlayerCount = playerCount,
            UserPlayer = userPlayer,
            AIPlayers = aiPlayers
        };
        return gameSettings;
    }

    #endregion

    #region GameState Progression Readiness System

    private Job _progressCheckJob;

    private void InitializeGameStateProgressionReadinessSystem() {
        _gameStateProgressionReadinessLookup = new Dictionary<GameState, IList<MonoBehaviour>>();
        _gameStateProgressionReadinessLookup.Add(GameState.Loading, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Building, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Restoring, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.Waiting, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DeployingSystemCreators, new List<MonoBehaviour>());
        //_gameStateProgressionReadinessLookup.Add(GameState.BuildAndDeploySystems, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildingSystems, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.GeneratingPathGraphs, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.DesigningInitialUnits, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.BuildingAndDeployingInitialUnits, new List<MonoBehaviour>());
        _gameStateProgressionReadinessLookup.Add(GameState.PreparingToRun, new List<MonoBehaviour>());
    }

    private void StartGameStateProgressionReadinessChecks() {
        D.Assert(!IsPaused, "{0} should not be paused.", GetType().Name);
        //D.Log("{0}_{1} is preparing to start GameState Progression System Readiness Checks.", GetType().Name, InstanceCount);
        __ValidateGameStateProgressionReadinessSystemState();
        string jobName = "GameMgrProgressCheckJob";
        _progressCheckJob = _jobMgr.StartNonGameplayJob(AssessReadinessToProgressGameState(), jobName, jobCompleted: (wasJobKilled) => {
            if (wasJobKilled) {
                D.Error("{0}'s GameState Progression Readiness System has timed out.", GetType().Name);
            }
            else {
                D.Assert(CurrentState == GameState.Running, "{0}_{1}.{2} = {3}.", GetType().Name, InstanceCount, typeof(GameState).Name, CurrentState.GetValueName());
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
        DateTime startTime = Utility.SystemTime;
        while (CurrentState != GameState.Running) {
            D.Assert(CurrentState != GameState.Lobby);
            D.Assert(_gameStateProgressionReadinessLookup.ContainsKey(CurrentState), "{0} key not found.", CurrentState.GetValueName());
            // this will tell me what state failed, whereas failing while accessing the dictionary won't
            IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[CurrentState];
            //D.Log("{0}_{1}.AssessReadinessToProgressGameState() called. GameState = {2}, UnreadyElements count = {3}.", GetType().Name, InstanceCount, CurrentState.GetValueName(), unreadyElements.Count);
            if (unreadyElements != null && unreadyElements.Count == Constants.Zero) {
                //D.Log("State prior to ProgressState = {0}.", CurrentState.GetValueName());
                ProgressState();
                //D.Log("State after ProgressState = {0}.", CurrentState.GetValueName());
            }
            if (DebugSettings.Instance.EnableStartupTimeout) {
                __CheckTime(startTime);
            }
            yield return null;
        }
        //D.Log("Exiting AssessReadinessToProgressGameState.");
    }

    private void __CheckTime(DateTime startTime) {
        if ((Utility.SystemTime - startTime).TotalSeconds > 10F) {
            _progressCheckJob.Kill();
        }
    }

    public void RecordGameStateProgressionReadiness(MonoBehaviour source, GameState maxGameStateUntilReady, bool isReady) {
        IList<MonoBehaviour> unreadyElements = _gameStateProgressionReadinessLookup[maxGameStateUntilReady];
        if (!isReady) {
            D.Assert(!unreadyElements.Contains(source), "UnreadyElements for {0} already has {1} registered!".Inject(maxGameStateUntilReady.GetValueName(), source.name));
            unreadyElements.Add(source);
            //D.Log("{0} has registered as unready to progress beyond {1}.", source.name, maxGameStateUntilReady.GetValueName());
        }
        else {
            D.Assert(unreadyElements.Contains(source), "UnreadyElements for {0} has no record of {1}!".Inject(maxGameStateUntilReady.GetValueName(), source.name));
            unreadyElements.Remove(source);
            //D.Log("{0} is now ready to progress beyond {1}. Remaining unready elements: {2}.",
            //source.name, maxGameStateUntilReady.GetValueName(), unreadyElements.Any() ? unreadyElements.Select(m => m.gameObject.name).Concatenate() : "None");
        }
    }

    #endregion

    #region Players

    private void InitializePlayers() {
        D.Log("{0} is initializing Players.", GetType().Name);

        AllPlayers = DebugControls.Instance.Players;    // 9.14.16 allows editor control over number of players
        UserPlayer = AllPlayers.Single(p => p.IsUser);

        AssignPlayerInitialRelationships();
        PlayersDesigns = new PlayersDesigns(AllPlayers);
    }

    private void AssignPlayerInitialRelationships() {
        var allEditorUnitCreators = UniverseFolder.Instance.gameObject.GetComponentsInChildren<ADebugUnitCreator>();
        var aiOwnedEditorUnitCreators = allEditorUnitCreators.Where(uc => !uc.EditorSettings.IsOwnerUser);
        var desiredAiUserRelationships = aiOwnedEditorUnitCreators.Select(uc => uc.EditorSettings.DesiredRelationshipWithUser.Convert());

        HashSet<DiplomaticRelationship> uniqueDesiredAiUserRelationships = new HashSet<DiplomaticRelationship>(desiredAiUserRelationships);
        //D.Log("{0}: Unique desired AI/User Relationships = {1}.", GetType().Name, uniqueDesiredAiUserRelationships.Select(r => r.GetValueName()).Concatenate());

        // Setup initial AIPlayer <-> User relationships derived from editorCreators..
        Dictionary<DiplomaticRelationship, IList<Player>> aiPlayerInitialUserRelationsLookup = new Dictionary<DiplomaticRelationship, IList<Player>>(AIPlayers.Count);
        Stack<Player> unassignedAIPlayers = new Stack<Player>(AIPlayers);
        uniqueDesiredAiUserRelationships.ForAll(aiUserRelationship => {
            if (unassignedAIPlayers.Count > Constants.Zero) {
                var aiPlayer = unassignedAIPlayers.Pop();
                //D.Log("{0} about to set {1}'s user relationship to {2}.", GetType().Name, aiPlayer, aiUserRelationship.GetValueName());
                UserPlayer.SetInitialRelationship(aiPlayer, aiUserRelationship);  // will auto handle both assignments
                aiPlayerInitialUserRelationsLookup.Add(aiUserRelationship, new List<Player>() { aiPlayer });
            }
        });
        // ..then assign any aiPlayers that have not been assigned an initial user relationship to Neutral
        if (unassignedAIPlayers.Count > Constants.Zero) {
            IList<Player> neutralAiPlayers;
            if (!aiPlayerInitialUserRelationsLookup.TryGetValue(DiplomaticRelationship.Neutral, out neutralAiPlayers)) {
                neutralAiPlayers = new List<Player>(unassignedAIPlayers.Count);
                aiPlayerInitialUserRelationsLookup.Add(DiplomaticRelationship.Neutral, neutralAiPlayers);
            }
            unassignedAIPlayers.ForAll(aiPlayer => {
                //D.Log("{0} about to set {1}'s user relationship to {2}.", GetType().Name, aiPlayer, DiplomaticRelationship.Neutral.GetValueName());
                UserPlayer.SetInitialRelationship(aiPlayer); // Neutral, will auto handle both assignments
                neutralAiPlayers.Add(aiPlayer);
            });
        }

        // Set initial AIPlayer <-> AIPlayer relationships to Neutral
        int aiPlayerCount = AllPlayers.Count - 1;
        for (int j = 0; j < aiPlayerCount; j++) {
            for (int k = j + 1; k < aiPlayerCount; k++) {
                Player jAiPlayer = AIPlayers[j];
                Player kAiPlayer = AIPlayers[k];
                jAiPlayer.SetInitialRelationship(kAiPlayer);    // Neutral, will auto handle both assignments
            }
        }

        _newGameUnitConfigurator = new NewGameUnitConfigurator(aiPlayerInitialUserRelationsLookup, allEditorUnitCreators);
    }

    #endregion

    private void ConfigureUnitCreators() {
        var playersWithoutEditorCreator = _newGameUnitConfigurator.ConfigureExistingEditorCreators();

        foreach (var player in playersWithoutEditorCreator) {
            string debugText;
            int randomValue = RandomExtended.Range(1, 3);
            if (randomValue == 1) {
                debugText = "fleet";
                _newGameUnitConfigurator.GenerateRandomAutoFleetCreator(player);
            }
            else if (randomValue == 2) {
                debugText = "starbase";
                _newGameUnitConfigurator.GenerateRandomAutoStarbaseCreator(player);
            }
            else {
                D.Assert(randomValue == 3);
                debugText = "settlement";
                _newGameUnitConfigurator.GenerateRandomAutoSettlementCreator(player);
            }
            D.Log("{0}: Creating auto {1} creator for {2}.", GetType().Name, debugText, player);
        }
    }

    /// <summary>
    /// Gets <c>player</c>'s AIManager instance.
    /// </summary>
    public PlayerAIManager GetAIManagerFor(Player player) {
        return _playerAiMgrLookup[player];
    }

    private void InitializePlayerAIManagers() {
        var uCenter = UniverseBuilder.Instance.UniverseCenter;
        IEnumerable<IStar_Ltd> allStars = GameKnowledge.Stars.Cast<IStar_Ltd>();
        IDictionary<Player, PlayerAIManager> tempLookup = new Dictionary<Player, PlayerAIManager>(AllPlayers.Count);

        AllPlayers.ForAll(player => {
            PlayerAIManager plyrAiMgr;
            if (_debugSettings.AllIntelCoverageComprehensive) {
                IEnumerable<IPlanetoid_Ltd> allPlanetoids = GameKnowledge.Planetoids.Cast<IPlanetoid_Ltd>();
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
            tempLookup.Add(player, plyrAiMgr);
        });
        _playerAiMgrLookup = tempLookup;
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// This substitutes my own Event for OnLevelWasLoaded so I don't have to use OnLevelWasLoaded anywhere else
    /// NOTE: Wiki: OnLevelWasLoaded is NOT guaranteed to run before all of the Awake calls. In most cases it will, but in some 
    /// might produce some unexpected bugs. If you need some code to be executed before Awake calls, use OnDisable instead.
    /// NOTE: In fact, my experience is that OnLevelWasLoaded occurs AFTER all Awake calls for objects that are already in the level.
    /// </summary>
    /// <param name="level">The scene level.</param>
    void OnLevelWasLoaded(int level) {
        if (IsExtraCopy) { return; }
        D.Assert(CurrentScene.buildIndex == level);
        D.Log("{0}_{1}.OnLevelWasLoaded({2}) received. Current State = {3}.", this.name, InstanceCount, ((SceneID)level).GetValueName(), CurrentState.GetValueName());
        RefreshCurrentSceneID();
        RefreshStaticReferences();
        UponLevelLoaded(level);
    }

    private void IsRunningPropChangedHandler() {
        D.LogBold("{0}.IsRunning changed to {1}.", GetType().Name, IsRunning);
        if (IsRunning) {
            OnIsRunning();
        }
    }

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
        D.LogBold("{0}.IsPaused changed to {1}.", GetType().Name, IsPaused);
    }

    private void OnIsRunning() {
        D.Assert(IsRunning);
        if (isRunningOneShot != null) {
            //var targetNames = isRunningOneShot.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending onIsRunning event to {1}.", GetType().Name, targetNames.Concatenate());
            isRunningOneShot(this, new EventArgs());
            isRunningOneShot = null;
        }
    }

    private void OnSceneLoading() {
        D.Assert(IsSceneLoading);
        if (sceneLoading != null) {
            sceneLoading(this, new EventArgs());
        }
    }

    private void OnSceneLoaded() {
        D.Assert(!IsSceneLoading);
        if (sceneLoaded != null) {
            //D.Log("{0}.sceneLoaded event dispatched.", GetType().Name);
            sceneLoaded(this, new EventArgs());
        }
    }

    private void OnGameStateChanging() {
        if (gameStateChanging != null) {
            //var targetNames = gameStateChanging.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending gameStateChanging event to {1}.", GetType().Name, targetNames.Concatenate());
            gameStateChanging(this, new EventArgs());
        }
    }

    private void OnGameStateChanged() {
        if (gameStateChanged != null) {
            //var targetNames = gameStateChanged.GetInvocationList().Select(d => d.Target.GetType().Name);
            //D.Log("{0} is sending gameStateChanged event to {1}.", GetType().Name, targetNames.Concatenate());
            gameStateChanged(this, new EventArgs());
        }
    }

    private void OnNewGameBuilding() {
        if (newGameBuilding != null) {
            newGameBuilding(this, new EventArgs());
        }
    }

    protected override void Update() {
        base.Update();
        _gameTime.CheckForDateChange(); // CheckForDateChange() will ignore the call if a GameInstance isn't running or is paused
    }

    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        IsRunning = false;
    }

    #endregion

    #region New Game

    public void InitiateNewGame(GameSettings newGameSettings) {
        LoadAndBuildNewGame(newGameSettings);
    }

    private void LoadAndBuildNewGame(GameSettings settings) {
        D.Log("{0}.LoadAndBuildNewGame() called.", GetType().Name);

        GameSettings = settings;
        CurrentState = GameState.Loading;

        // UNDONE when I allow in game return to Lobby, I'll need to call this just before I use SceneMgr to load the LobbyScene
        RefreshLastSceneID();

        //D.Log("SceneManager.LoadScene({0}) being called.", SceneID.GameScene.GetValueName());
        SceneManager.LoadScene(SceneID.GameScene.GetValueName(), LoadSceneMode.Single); //Application.LoadLevel(index) deprecated by Unity 5.3
    }

    #endregion

    #region Saving and Restoring

    public void SaveGame(string gameName) {
        D.Warn("{0}.SaveGame() not currently implemented.", GetType().Name);
    }

    public void LoadSavedGame(string gameID) {
        D.Warn("{0}.LoadSavedGame() not currently implemented.", GetType().Name);
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
    private void ResetConditionsForGameStartup() {
        if (IsPaused) {
            RequestPauseStateChange(toPause: false, toOverride: true);
        }
        if (_playerAiMgrLookup != null) {
            _playerAiMgrLookup.Values.ForAll(pAiMgr => pAiMgr.Dispose());
        }
        GameKnowledge.Reset();
    }

    private void InitializeGameKnowledge() {
        D.Assert(GameKnowledge != null);
        var uCenter = UniverseBuilder.Instance.UniverseCenter;
        GameKnowledge.Initialize(uCenter);
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

    #region Lobby

    void Lobby_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void Lobby_ExitState() {
        LogEvent();
        // Transitioning to Loading (the level) whether a new or saved game
        D.Assert(CurrentState == GameState.Loading);
        __LogDuration();
    }

    #endregion

    // 8.9.16 Loading, Building, Restoring will need to be re-engineered when I re-introduce persistence

    #region Loading

    void Loading_EnterState() {
        LogEvent();
        __RecordDurationStartTime();

        ResetConditionsForGameStartup();    // 8.9.16 moved here from Building as ReadinessChecks don't like starting paused
        // Start state progression checks here as Loading is always called whether a new game, loading saved game or startup simulation
        StartGameStateProgressionReadinessChecks();

        //CreatePlayers(GameSettings);  // moved to Building to follow ResetConditionsForGameStartup()

        if (GameSettings.__IsStartupSimulation) {
            return;
        }

        RecordGameStateProgressionReadiness(Instance, GameState.Loading, isReady: false);
        // tell ManagementObjects to drop its children (including SaveGameManager!) before the scene gets reloaded
        IsSceneLoading = true;
        OnSceneLoading();
    }

    void Loading_UponLevelLoaded(int level) {
        D.Assert(!GameSettings.__IsStartupSimulation);
        LogEvent();

        D.Assert(CurrentSceneID == SceneID.GameScene, "Scene transition to {0} not implemented.", CurrentSceneID.GetValueName());
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

        // Building is only for new or simulated games
        D.Assert(!GameSettings.IsSavedGame);
        OnNewGameBuilding();
        ////ResetConditionsForGameStartup();    // moved to Loading
        _gameTime.PrepareToBeginNewGame();  // Done here as this state is unique to new or simulated games

        InitializeGameKnowledge();
        InitializePlayers();

        RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: true);
    }

    void Building_UponProgressState() {
        LogEvent();
        CurrentState = GameState.Waiting;
    }

    void Building_ExitState() {
        LogEvent();
        // Building is only for new games, so next state is Waiting
        D.Assert(CurrentState == GameState.Waiting);
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
        CurrentState = GameState.Waiting;
    }

    void Restoring_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Waiting);
        __LogDuration();
    }

    #endregion

    #region Waiting

    void Waiting_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void Waiting_UponProgressState() {
        LogEvent();
        CurrentState = GameState.DeployingSystemCreators;
    }

    void Waiting_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.DeployingSystemCreators);
        __LogDuration();
    }

    #endregion

    #region DeployingSystemCreators

    void DeployingSystemCreators_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void DeployingSystemCreators_UponProgressState() {
        LogEvent();
        CurrentState = GameState.BuildingSystems;
    }

    void DeployingSystemCreators_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.BuildingSystems);
        __LogDuration();
    }

    #endregion

    #region BuildingSystems

    void BuildingSystems_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void BuildingSystems_UponProgressState() {
        LogEvent();
        CurrentState = GameState.GeneratingPathGraphs;
    }

    void BuildingSystems_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.GeneratingPathGraphs);
        InitializePlayerAIManagers();   // HACK needs another state rather than using Exit method
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
        D.Assert(CurrentState == GameState.DesigningInitialUnits);
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
        D.Assert(CurrentState == GameState.BuildingAndDeployingInitialUnits);
        __LogDuration();
    }

    #endregion

    #region BuildingAndDeployingInitialUnits

    void BuildingAndDeployingInitialUnits_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
        ConfigureUnitCreators();
    }

    void BuildingAndDeployingInitialUnits_UponProgressState() {
        LogEvent();
        CurrentState = GameState.PreparingToRun;
    }

    void BuildingAndDeployingInitialUnits_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.PreparingToRun);
        __LogDuration();
    }

    #endregion

    #region PreparingToRun

    void PreparingToRun_EnterState() {
        LogEvent();
        __RecordDurationStartTime();
    }

    void PreparingToRun_UponProgressState() {
        LogEvent();
        CurrentState = GameState.Running;
    }

    void PreparingToRun_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Running);
        __LogDuration();
    }

    #endregion

    #region Running

    void Running_EnterState() {
        LogEvent();
        IsRunning = true;   // Note: My practice - IsRunning THEN pause changes
        if (_playerPrefsMgr.IsPauseOnLoadEnabled) {
            RequestPauseStateChange(toPause: true, toOverride: true);
        }
    }

    void Running_ExitState() {
        LogEvent();
        D.Assert(CurrentState == GameState.Lobby || CurrentState == GameState.Loading);
        IsRunning = false;
    }

    #endregion

    #region State Machine Support Methods

    private void ProgressState() { UponProgressState(); }

    private void UponProgressState() { RelayToCurrentState(); }

    private void UponLevelLoaded(int level) { RelayToCurrentState(level); }

    #endregion

    #endregion

    public void ExitGame() { Shutdown(); }  //TODO Confirmation Dialog

    private void Shutdown() {
        _playerPrefsMgr.Store();
        Application.Quit(); // ignored inside Editor or WebPlayer
    }

    #region Cleanup

    protected override void Cleanup() {
        if (_progressCheckJob != null) {
            _progressCheckJob.Dispose();
        }
        if (_playerAiMgrLookup != null) {
            _playerAiMgrLookup.Values.ForAll(pAiMgr => pAiMgr.Dispose());
        }
        GameKnowledge.Dispose();

        DisposeOfGlobals();
        References.GameManager = null;  // last, as Globals may use it when disposing
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

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    protected override string __DurationLogIntroText { get { return "{0}.{1}".Inject(typeof(GameState).Name, LastState); } }

    /// <summary>
    /// Gets the AIPlayers that the User has not yet met, that have been assigned the initialUserRelationship to begin with when they do meet.
    /// </summary>
    /// <param name="initialUserRelationship">The initial user relationship.</param>
    /// <returns></returns>
    public IEnumerable<Player> GetUnmetAiPlayersWithInitialUserRelationsOf(DiplomaticRelationship initialUserRelationship) {
        return _newGameUnitConfigurator.GetUnmetAiPlayersWithInitialUserRelationsOf(initialUserRelationship);
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum containing both the name and index of a scene.
    /// </summary>
    public enum SceneID {
        // No None as Unity would require that there is a None scene set to 0 in build settings.
        LobbyScene = 0,
        GameScene = 1
    }

    #endregion

}
