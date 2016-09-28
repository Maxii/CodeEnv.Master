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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Unit Creators.
/// </summary>
public abstract class AUnitCreator : AMonoBase {

    private const string NameFormat = "{0}.{1}";

    public string Name { get { return NameFormat.Inject(UnitName, GetType().Name); } }

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName {
        get { return transform.name; }
        private set { transform.name = value; }
    }

    public IntVector3 SectorIndex { get { return SectorGrid.Instance.GetSectorIndexThatContains(transform.position); } }

    private UnitCreatorConfiguration _configuration;
    public UnitCreatorConfiguration Configuration {
        get { return _configuration; }
        set { SetProperty<UnitCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationChangedHandler); }
    }

    protected Player Owner { get { return Configuration.Owner; } }

    protected JobManager _jobMgr;
    protected GameManager _gameMgr;
    protected UnitFactory _factory;
    protected bool _isUnitDeployed;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        InitializeDeploymentSystem();
    }

    private void InitializeValuesAndReferences() {
        _factory = UnitFactory.Instance;
        _gameMgr = GameManager.Instance;
        _jobMgr = JobManager.Instance;
    }

    protected abstract void InitializeDeploymentSystem();

    protected void Subscribe() {
        _gameMgr.isRunningOneShot += IsRunningEventHandler;
    }

    #region Event and Property Change Handlers

    private void IsRunningEventHandler(object sender, EventArgs e) {
        HandleGameIsRunning();
    }

    protected void HandleGameIsRunning() {
        if (GameTime.Instance.CurrentDate >= Configuration.DeployDate) {
            HandleDeployDateReached();
        }
        else {
            BuildDeployAndBeginUnitOpsOnDeployDate();
        }
    }

    private void ConfigurationChangedHandler() {
        UnitName = Configuration.UnitName;
    }

    #endregion

    protected void BuildDeployAndBeginUnitOpsOnDeployDate() {
        D.Assert(_gameMgr.IsRunning);
        string jobName = "{0}.WaitForDeployDate({1})".Inject(Name, Configuration.DeployDate);
        _jobMgr.WaitForDate(Configuration.DeployDate, jobName, waitFinished: (jobWasKilled) => {
            D.Assert(!jobWasKilled);
            HandleDeployDateReached();
        });
    }

    protected void HandleDeployDateReached() { // 3.25.16 wait approach changed from dateChanged event handler to WaitForDate utility method
        if (!ValidateConfiguration()) {
            return;
        }
        GameDate currentDate = GameTime.Instance.CurrentDate;
        //D.Log("{0}.HandleDeployDateReached() called. Date: {1}.", Name, currentDate);
        GameDate dateToDeploy = Configuration.DeployDate;
        if (currentDate >= dateToDeploy) {
            D.Warn(currentDate > dateToDeploy, "{0} recorded current date {1} beyond target date {2}.", Name, currentDate, dateToDeploy);
            //D.Log("{0} is about to build, deploy and begin ops during runtime. Date: {1}.", Name, currentDate);

            MakeUnitAndPrepareForDeployment();
            _isUnitDeployed = DeployUnit();
            if (!_isUnitDeployed) {
                Destroy(gameObject);
                return;
            }
            PrepareForUnitOperations();
            BeginUnitOperations();
        }
    }

    private bool ValidateConfiguration() {
        if (Configuration == default(UnitCreatorConfiguration)) {
            D.Warn("{0} found Configuration unassigned so destroying unit before created.", Name);
            Destroy(gameObject);
            return false;
        }
        return true;
    }

    protected void MakeUnitAndPrepareForDeployment() {
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
    /// Deploys the unit. Returns true if successfully deployed, false
    /// if not and therefore destroyed.
    /// </summary>
    /// <returns></returns>
    protected abstract bool DeployUnit();

    protected void PrepareForUnitOperations() {
        LogEvent();
        AddUnitToGameKnowledge();
        AddUnitToOwnerAndAllysKnowledge();
        RegisterCommandForOrders();
        CompleteUnitInitialization();
    }

    /// <summary>
    /// Adds the unit to game knowledge.
    /// </summary>
    protected abstract void AddUnitToGameKnowledge();

    /// <summary>
    /// Adds the unit to its owner's knowledge as well as any of the owner's allies. 
    /// <remarks>OPTIMIZE 8.2.16 Currently required as can't rely on Owner changed events to handle all this 
    /// since owner is set in data before owner changed events are wired.</remarks>
    /// </summary>
    protected abstract void AddUnitToOwnerAndAllysKnowledge();

    /// <summary>
    /// Registers the command with the Owner's AIMgr so it can receive orders.
    /// </summary>
    protected abstract void RegisterCommandForOrders();

    protected abstract void CompleteUnitInitialization();

    protected void BeginUnitOperations() {
        LogEvent();
        BeginElementsOperations();
        BeginCommandOperations();
        // 3.25.16 I eliminated the 1 frame delay onCompletion delegate to allow Element and Command Idling_EnterState to execute
        // as I think this is a bad practice. Anyway, 1 frame wouldn't be enough for an IEnumerator Idling_EnterState
        // to finish if it had more than one set of yields in it. In addition, if there is going to be a problem with 
        // changing state (from issued orders here) when Idling is the state, but its EnterState hasn't run yet, I
        // should find it out now as this can easily happen during the game.

        InitializeUnitDebugControl();
        //__IssueFirstUnitOrder(onCompleted: delegate {
        //    //// issuing the first unit order can sometimes require access to this creator script so remove it after the order has been issued
        //    //RemoveCreatorScript();
        //});
    }

    /// <summary>
    /// Starts the state machine of each element in this Unit.
    /// </summary>
    protected abstract void BeginElementsOperations();

    /// <summary>
    /// Starts the state machine of this Unit's Command.
    /// </summary>
    protected abstract void BeginCommandOperations();

    private void InitializeUnitDebugControl() {
        var unitDebugCntl = UnityUtility.ValidateComponentPresence<UnitDebugControl>(gameObject);
        unitDebugCntl.Initialize();
    }

    [Obsolete]
    protected abstract void __IssueFirstUnitOrder(Action onCompleted);

    [Obsolete]  // 8.7.16 destroying script replaced by disabling it so settings visible while playing
    private void RemoveCreatorScript() {
        Destroy(this);
    }

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
    //        //D.Log("{0} is subscribing statically to {1}.", Name, _gameMgr.GetType().Name);
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

}


