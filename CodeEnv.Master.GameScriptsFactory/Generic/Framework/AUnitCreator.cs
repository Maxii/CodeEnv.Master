// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCreator.cs
// Abstract base class for Unit Creators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Unit Creators.
/// </summary>
public abstract class AUnitCreator : AMonoBase, IDateMinderClient {

    private const string DebugNameFormat = "{0}.{1}";

    private string _debugName;
    public string DebugName {
        get {
            if (_debugName == null) {
                _debugName = DebugNameFormat.Inject(UnitName, GetType().Name);
            }
            return _debugName;
        }
    }

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName {
        get { return transform.name; }
        private set { transform.name = value; }
    }

    public IntVector3 SectorID { get { return SectorGrid.Instance.GetSectorIDThatContains(transform.position); } }

    private UnitCreatorConfiguration _configuration;
    public UnitCreatorConfiguration Configuration {
        get { return _configuration; }
        set {
            D.AssertNull(_configuration);   // currently one time only
            SetProperty<UnitCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationSetHandler);
        }
    }

    protected Player Owner { get { return Configuration.Owner; } }

    protected bool ShowDebugLog { get { return DebugControls.Instance.ShowDeploymentDebugLogs; } }

    protected JobManager _jobMgr;
    protected GameManager _gameMgr;
    protected UnitFactory _factory;
    ////protected bool _isUnitPositioned;

    protected override void Awake() {
        base.Awake();
        __ValidateStaticSetting();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _factory = UnitFactory.Instance;
        _gameMgr = GameManager.Instance;
        _jobMgr = JobManager.Instance;
    }

    protected override void Start() {
        base.Start();
        AttemptToAuthorizeDeploymentOnStart();
    }

    protected virtual void AttemptToAuthorizeDeploymentOnStart() {
        if (_gameMgr.IsRunning) {
            BuildAndPositionUnit();
            AuthorizeDeployment();
        }
    }

    #region Event and Property Change Handlers

    protected void ConfigurationSetHandler() {
        UnitName = Configuration.UnitName;
    }

    #endregion

    /// <summary>
    /// Builds and positions the Unit in preparation for deployment and operations.
    /// </summary>
    public void BuildAndPositionUnit() {
        D.AssertNotNull(Configuration);    // would only be called with a Configuration
        D.Log(ShowDebugLog, "{0} is building and positioning {1}. Targeted DeployDate = {2}.", DebugName, Configuration.UnitName, Configuration.DeployDate);
        MakeUnitAndPrepareForPositioning();
        ////_isUnitPositioned = PositionUnit();
        PositionUnit();
    }

    /// <summary>
    /// Authorizes the creator to deploy and commence operations of the Unit on the DeployDate specified by the Configuration.
    /// If the unit was not able to be pre-positioned when built, it will be destroyed by this method.
    /// <remarks>If this creator is present in the scene before the game IsRunning then UniverseCreator will call 
    /// BuildAndPosition(). It will be followed by AuthorizeDeployment() when the game begins running but after all 
    /// CelestialObjects have commenced operations. This way, celestial objects are operational before they can be 
    /// detected by units. If this creator is placed in the scene during runtime, then it will wake, detect the game 
    /// is already running and call BuildAndPosition() followed by AuthorizeDeployment() itself.</remarks>
    /// </summary>
    public void AuthorizeDeployment() {
        ////if (!_isUnitPositioned) {
        ////    Destroy(gameObject);
        ////    return;
        ////}

        D.AssertNotNull(Configuration);    // would only be called with a Configuration
        D.Log(ShowDebugLog, "{0} is authorizing deployment of {1}. Targeted DeployDate = {2}.", DebugName, Configuration.UnitName, Configuration.DeployDate);
        var currentDate = GameTime.Instance.CurrentDate;
        D.Assert(currentDate >= GameTime.GameStartDate, currentDate.ToString());
        if (currentDate >= Configuration.DeployDate) {
            if (_gameMgr.IsPaused) {
                // defer deployment until unpaused
                _gameMgr.isPausedChanged += DeployAfterPauseEventHandler;
            }
            else {
                Deploy();
            }
        }
        else {
            DeployOnDeployDate();
        }
    }

    private void DeployAfterPauseEventHandler(object sender, EventArgs e) {
        D.Assert(!_gameMgr.IsPaused);
        _gameMgr.isPausedChanged -= DeployAfterPauseEventHandler;
        D.Log(ShowDebugLog, "{0} is deploying now after resuming from pause.", DebugName);
        Deploy();
    }

    private void DeployOnDeployDate() {
        D.Assert(_gameMgr.IsRunning);
        GameTime.Instance.DateMinder.Add(Configuration.DeployDate, this);
    }

    private void Deploy() { // 3.25.16 wait approach changed from dateChanged event handler to WaitForDate utility method
        D.Assert(!_gameMgr.IsPaused);
        GameDate currentDate = GameTime.Instance.CurrentDate;
        GameDate dateToDeploy = Configuration.DeployDate;
        if (currentDate < dateToDeploy) {
            D.Error("{0}: {1} should not be < {2}.", DebugName, currentDate, dateToDeploy);
        }
        //D.Log(ShowDebugLog, "{0} is about to begin ops of {1}'s {2} on {3}.", DebugName, Owner, Configuration.UnitName, currentDate);

        if (currentDate > dateToDeploy) {
            D.Log(ShowDebugLog, "{0} exceeded DeployDate {1}, so actually deployed on {2}.", DebugName, dateToDeploy, currentDate);
        }
        else {
            D.Log(ShowDebugLog, "{0} was deployed on intended date {1}.", DebugName, currentDate);
        }
        PrepareForUnitOperations();
        ////BeginUnitOperations();

        // 5.3.17 Added bool return to avoid clearing element ref if beginning CmdOperations was deferred
        BeginElementsOperations();
        var isCmdOperationsBegun = BeginCommandOperations();
        if (isCmdOperationsBegun) {
            ClearElementReferences();
        }
    }

    private void MakeUnitAndPrepareForPositioning() {
        LogEvent();
        MakeElements();
        MakeCommand(Configuration.Owner);
        AddElementsToCommand();
        AssignHQElement();
    }

    protected abstract void MakeElements();

    protected abstract void MakeCommand(Player owner);

    protected abstract void AddElementsToCommand();

    /// <summary>
    /// Assigns the HQ element to the command. The assignment itself regenerates the formation,
    /// resulting in each element assuming the proper position.
    /// Note: This method must not be called before AddElementsToCommand().
    /// </summary>
    protected abstract void AssignHQElement();

    /// <summary>
    /// Deploys the unit. Most creators deploy the unit they create where they are located.
    /// The only exception is the SettlementCreator which deploys its unit into orbit around
    /// its parent system.
    /// <remarks>5.3.17 Removed isPositioned return value as it is no longer reqd since
    /// DebugSettlementCreators are Destroy()ed in UniverseCreator if there is no System to deploy them too.</remarks>
    /// </summary>
    /// <returns></returns>
    protected abstract void PositionUnit();

    private void PrepareForUnitOperations() {
        LogEvent();
        CompleteUnitInitialization();   // 10.19.16 Moved up from last as Knowledge organizes Cmds by their sectorID which this initializes
        AddUnitToGameKnowledge();
        ////AddUnitToOwnerAndAllysKnowledge();
        ////RegisterCommandForOrders();
    }

    protected abstract void CompleteUnitInitialization();

    /// <summary>
    /// Adds the unit to game knowledge.
    /// </summary>
    protected abstract void AddUnitToGameKnowledge();

    [Obsolete]
    private void BeginUnitOperations() {
        LogEvent();
        BeginElementsOperations();
        BeginCommandOperations();
        // 3.25.16 I eliminated the 1 frame delay onCompletion delegate to allow Element and Command Idling_EnterState to execute
        // as I think this is a bad practice. Anyway, 1 frame wouldn't be enough for an IEnumerator Idling_EnterState
        // to finish if it had more than one set of yields in it. In addition, if there is going to be a problem with 
        // changing state (from issued orders here) when Idling is the state, but its EnterState hasn't run yet, I
        // should find it out now as this can easily happen during the game.

        InitializeUnitDebugControl();
        ////__IssueFirstUnitOrder(onCompleted: delegate {
        ////    // issuing the first unit order can sometimes require access to this creator script so remove it after the order has been issued
        ////    //RemoveCreatorScript();
        ////});
    }

    /// <summary>
    /// Starts the state machine of each element in this Unit.
    /// </summary>
    protected abstract void BeginElementsOperations();

    /// <summary>
    /// Starts the state machine of this Unit's Command. Returns <c>true</c> if CmdOperations was begun,
    /// <c>false</c> if it was deferred because an owner change was still underway.
    /// </summary>
    /// <returns></returns>
    protected abstract bool BeginCommandOperations();

    protected abstract void ClearElementReferences();

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _gameMgr.isPausedChanged -= DeployAfterPauseEventHandler;
    }

    #region Debug

    private void __ValidateStaticSetting() {
        if (gameObject.isStatic) {
            D.Warn("{0} should never be marked static as they must be relocatable. Correcting.", DebugName);
            // UNCLEAR Is there any advantage marking them static after they are relocated?
            gameObject.isStatic = false;
        }
    }

    [Obsolete]
    private void InitializeUnitDebugControl() {
        var unitDebugCntl = gameObject.GetComponent<UnitDebugControl>();
        if (unitDebugCntl != null) {    // 4.21.17 Getting ready to remove these from Cmds
            unitDebugCntl.Initialize();
        }
    }

    #endregion

    public sealed override string ToString() {
        return DebugName;
    }

    #region Archive

    #region Static Member Management in non-persistent MonoBehaviours Archive

    // By definition, static members are persistent even if their MonoBehaviours are not.
    // When the MonoBehaviour is created again in a new game instance, the static members from
    // the previous instance of the MonoBehaviour will still retain its previous values
    // thereby requiring careful management (clearing and repopulating).

    //private static IEnumerable<IUnitCmd> _allUnitCommands;

    //private void Subscribe() {
    //    ...
    //    SubscribeStaticallyOnce();
    //}

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    //private static bool _isStaticallySubscribed;

    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    //private void SubscribeStaticallyOnce() {
    //    if (!_isStaticallySubscribed) {
    //        //D.Log("{0} is subscribing statically to {1}.", DebugName, _gameMgr.GetType().Name);
    //        _gameMgr.sceneLoaded += SceneLoadedEventHandler;
    //        _isStaticallySubscribed = true;
    //    }
    //}

    //private static void UnitDeathHandler(object sender, EventArgs e) {
    //    CommandType command = sender as CommandType;
    //    _allUnitCommands.Remove(command);
    //}

    //private void SceneLoadedEventHandler(object sender, EventArgs e) {
    //    CleanupStaticMembers();
    //}

    /// <summary>
    /// Records the Command in its static collection holding all instances.
    /// Note: The Assert tests are here to make sure instances from a prior scene are not still present, as the collections
    /// these items are stored in are static and persist across scenes.
    /// </summary>
    //private void RecordCommandInStaticCollections() {
    //    _command.deathOneShot += UnitDeathHandler;
    //    var cmdNamesStored = _allUnitCommands.Select(cmd => cmd.DisplayName);
    //    // Can't use a Contains(item) test as the new item instance will never equal the old instance from the previous scene, even with the same name
    //    D.Assert(!cmdNamesStored.Contains(_command.DisplayName), "{0}.{1} reports {2} already present.".Inject(UnitName, GetType().Name, _command.DisplayName));
    //    _allUnitCommands.Add(_command);
    //}

    //protected override void Cleanup() {
    //    Unsubscribe();
    //    if (IsApplicationQuiting) {
    //        CleanupStaticMembers();
    //        UnsubscribeStaticallyOnceOnQuit();
    //    }
    //}

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes or after quiting.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    //private static void CleanupStaticMembers() {
    //    _allUnitCommands.ForAll(cmd => cmd.deathOneShot -= UnitDeathHandler);
    //    _allUnitCommands.Clear();
    //    _elementInstanceIDCounter = Constants.One;
    //}

    /// <summary>
    /// Unsubscribe this class from all events that use a static event handler on Quit.
    /// </summary>
    //private void UnsubscribeStaticallyOnceOnQuit() {
    //    if (_isStaticallySubscribed) {
    //        if (_gameMgr != null) {
    //            _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
    //        }
    //        _isStaticallySubscribed = false;
    //    }
    //}

    #endregion

    #endregion

    #region IDateMinderClient Members

    void IDateMinderClient.HandleDateReached(GameDate date) {
        D.AssertEqual(Configuration.DeployDate, date);
        Deploy();
    }

    #endregion

}


