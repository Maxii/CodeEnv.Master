// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdItem.cs
// Abstract class for AMortalItem's that are Unit Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract class for AMortalItem's that are Unit Commands.
/// </summary>
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmd, IUnitCmd_Ltd, IFleetNavigable, IUnitAttackable, IFormationMgrClient,
    ISensorDetector {

    public event EventHandler isAvailableChanged;

    public Formation UnitFormation { get { return Data.UnitFormation; } }

    public string UnitName { get { return Data.ParentName; } }

    /// <summary>
    /// The maximum radius of this Unit's current formation, independent of the number of elements currently assigned a
    /// station in the formation or whether the Unit's elements are located on their formation station. 
    /// Value encompasses each element's "KeepoutZone" (Facility: AvoidableObstacleZone, Ship: CollisionDetectionZone) 
    /// when the element is OnStation. 
    /// </summary>
    public float UnitMaxFormationRadius {
        get { return Data.UnitMaxFormationRadius; }
        set { Data.UnitMaxFormationRadius = value; }
    }

    private Transform _unitContainer;
    /// <summary>
    /// The transform that normally contains all elements and commands assigned to the Unit.
    /// </summary>
    public Transform UnitContainer {
        get { return _unitContainer; }
        private set { SetProperty<Transform>(ref _unitContainer, value, "UnitContainer"); }
    }

    /// <summary>
    /// Indicates whether this element is available for a new assignment.
    /// <remarks>Typically, an element that is available is Idling.</remarks>
    /// </summary>
    private bool _isAvailable;
    public bool IsAvailable {
        get { return _isAvailable; }
        protected set { SetProperty<bool>(ref _isAvailable, value, "IsAvailable", IsAvailablePropChangedHandler); }
    }

    public bool IsAttackCapable { get { return Elements.Where(e => e.IsAttackCapable).Any(); } }

    public IconInfo IconInfo {
        get {
            if (DisplayMgr == null) {
                D.Warn("{0}.DisplayMgr is null when attempting access to IconInfo.", GetType().Name);
                return default(IconInfo);
            }
            return DisplayMgr.IconInfo;
        }
    }

    public override float Radius { get { return Data.Radius; } }

    public new AUnitCmdData Data {
        get { return base.Data as AUnitCmdData; }
        set { base.Data = value; }
    }

    private AUnitElementItem _hqElement;
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", HQElementPropChangedHandler, HQElementPropChangingHandler); }
    }

    public IList<AUnitElementItem> AvailableNonHQElements { get { return NonHQElements.Where(e => e.IsAvailable).ToList(); } }

    public IList<AUnitElementItem> AvailableElements { get { return Elements.Where(e => e.IsAvailable).ToList(); } }

    public IList<AUnitElementItem> NonHQElements { get { return Elements.Except(HQElement).ToList(); } }    // OPTIMIZE

    public IList<AUnitElementItem> Elements { get; private set; }

    /// <summary>
    /// Unified Monitor for SRSensors that combines the results of all element SRSensorMonitors.
    /// </summary>
    public UnifiedSRSensorMonitor UnifiedSRSensorMonitor { get; private set; }

    /// <summary>
    /// The required Medium Range SensorMonitor. 
    /// <remarks>3.31.17 One MRSensor now reqd but can be damaged. Monitor may not be operational but will never be null.</remarks>
    /// </summary>
    public ICmdSensorRangeMonitor MRSensorMonitor { get; private set; }

    public IList<ICmdSensorRangeMonitor> SensorMonitors { get; private set; }

    public new CmdCameraStat CameraStat {
        protected get { return base.CameraStat as CmdCameraStat; }
        set { base.CameraStat = value; }
    }

    protected new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }
    protected AFormationManager FormationMgr { get; private set; }

    protected Job _repairJob;

    private IFtlDampenerRangeMonitor _ftlDampenerRangeMonitor;
    private ITrackingWidget _trackingLabel;
    private Job _deferRedAlertStanddownAssessmentJob;
    private FixedJoint _hqJoint;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Elements = new List<AUnitElementItem>();
        SensorMonitors = new List<ICmdSensorRangeMonitor>(2);
        FormationMgr = InitializeFormationMgr();
    }

    protected abstract AFormationManager InitializeFormationMgr();

    protected override void InitializeOnData() {
        base.InitializeOnData();
        D.AssertNotNull(transform.parent);
        UnitContainer = transform.parent;
        // the only collider is for player interaction with the item's CmdIcon
        AttachEquipment();
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, Formation>(d => d.UnitFormation, UnitFormationPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, AlertStatus>(d => d.AlertStatus, AlertStatusPropChangedHandler));
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeTrackingLabel();
    }

    protected sealed override ADisplayManager MakeDisplayManagerInstance() {
        return new UnitCmdDisplayManager(this, TempGameValues.CmdMeshCullLayer);
    }

    protected sealed override void InitializeDisplayManager() {
        base.InitializeDisplayManager();
        DisplayMgr.MeshColor = Owner.Color;
        DisplayMgr.IconInfo = MakeIconInfo();
        SubscribeToIconEvents(DisplayMgr.Icon);
        DisplayMgr.ResizePrimaryMesh(Radius);
    }

    protected override EffectsManager InitializeEffectsManager() {
        return new UnitCmdEffectsManager(this);
    }

    private void SubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    protected sealed override HoverHighlightManager InitializeHoverHighlightMgr() {
        return new HoverHighlightManager(this, UnitMaxFormationRadius);
    }

    protected sealed override CircleHighlightManager InitializeCircleHighlightMgr() {
        var iconTransform = DisplayMgr.Icon.WidgetTransform;
        float radius = Screen.height * 0.03F;
        return new CircleHighlightManager(iconTransform, radius, isCircleSizeDynamic: false);
    }

    private void InitializeHQAttachmentSystem() {

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        var rigidbody = gameObject.AddComponent<Rigidbody>();   // OPTIMIZE add to prefab
        Profiler.EndSample();

        rigidbody.isKinematic = false; // FixedJoint needs a Rigidbody. If isKinematic acts as anchor for HQShip
        rigidbody.useGravity = false;
        rigidbody.mass = Constants.ZeroF;
        rigidbody.drag = Constants.ZeroF;
        rigidbody.angularDrag = Constants.ZeroF;

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        _hqJoint = gameObject.AddComponent<FixedJoint>();   // OPTIMIZE add to prefab
        Profiler.EndSample();
    }

    protected override bool InitializeOwnerAIManager() {
        bool isInitialized = base.InitializeOwnerAIManager();
        D.Assert(isInitialized);
        OwnerAIMgr.awarenessOfFleetChanged += AwarenessOfFleetChangedEventHandler;
        return true;
    }

    private void AttachEquipment() {
        Data.Sensors.ForAll(s => Attach(s));
        Attach(Data.FtlDampener);
    }

    private void Attach(CmdSensor sensor) {
        var monitor = sensor.RangeMonitor;
        if (!SensorMonitors.Contains(monitor)) {
            SensorMonitors.Add(monitor);
            if (monitor.RangeCategory == RangeCategory.Medium) {
                MRSensorMonitor = monitor;
            }
        }
    }

    private void Attach(FtlDampener dampener) {
        _ftlDampenerRangeMonitor = dampener.RangeMonitor;
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        InitializeUnifiedSRSensorMonitor();
        InitializeCmdRangeMonitors();
        AssessIcon();
    }

    private void InitializeUnifiedSRSensorMonitor() {
        var monitor = new UnifiedSRSensorMonitor(this);
        foreach (var element in Elements) {
            // 4.4.17 Deferring event subscription from here handles FerryFleet case where fully operational element 
            // is added to Cmd that is not yet operational without triggering EventHandlers. CommenceOperations will 
            // AssessAlertStatus so Cmd will be aware of element's knowledge of enemy presence, if any, and then subscribe.
            monitor.Add(element.SRSensorMonitor);
        }
        UnifiedSRSensorMonitor = monitor;
    }

    private void InitializeCmdRangeMonitors() {
        SensorMonitors.ForAll(srm => srm.InitializeRangeDistance());
        _ftlDampenerRangeMonitor.InitializeRangeDistance();
    }

    /// <summary>
    /// Subscribes to sensor events including events from the UnifiedSRSensorMonitor.
    /// <remarks>Must be called after initial runtime state is set, aka Idling. 
    /// Otherwise events can arrive immediately as sensors activate.</remarks>
    /// <remarks>OPTIMIZE Virtual to allow Assert of CurrentState to enforce above.</remarks>
    /// </summary>
    protected virtual void SubscribeToSensorEvents() {
        SensorMonitors.ForAll(sm => {
            sm.enemyCmdsInRangeChgd += EnemyCmdsInSensorRangeChangedEventHandler;
            sm.warEnemyElementsInRangeChgd += WarEnemyElementsInSensorRangeChangedEventHandler;
        });
        MRSensorMonitor.isOperationalChanged += MRSensorMonitorIsOperationalChangedEventHandler;
        UnifiedSRSensorMonitor.enemyCmdsInRangeChgd += EnemyCmdsInSensorRangeChangedEventHandler;
        UnifiedSRSensorMonitor.warEnemyElementsInRangeChgd += WarEnemyElementsInSensorRangeChangedEventHandler;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        RegisterForOrders();
    }

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public virtual void AddElement(AUnitElementItem element) {
        if (Elements.Contains(element)) {
            D.Error("{0} attempting to add {1} that is already present.", DebugName, element.DebugName);
        }
        if (element.IsHQ) {
            D.Error("{0} adding element {1} already designated as the HQ Element.", DebugName, element.DebugName);
        }
        if (IsOperational && !element.IsOperational) {
            // 4.4.17 Acceptable combos: Both not operational during construction, both operational during runtime
            // and non-operational Cmd with operational element when creating a ferryFleet using UnitFactory.
            D.Error("{0}: Adding element {1} with unexpected IsOperational state.", DebugName, element.DebugName);
        }
        Elements.Add(element);
        Data.AddElement(element.Data);
        element.Command = this;
        element.AttachAsChildOf(UnitContainer);

        // 3.31.17 CmdSensor attachment to monitors now takes place when the sensor is built in UnitFactory.

        if (!IsOperational) {
            // avoid the following extra work if adding during Cmd construction
            D.Assert(!IsDead);
            D.AssertNull(HQElement);    // During Cmd construction, HQElement will be designated AFTER all Elements are
            return;                         // added resulting in _formationMgr adding all elements into the formation at once
        }

        UnifiedSRSensorMonitor.Add(element.SRSensorMonitor);   // HACK added it to FinalInitialize, like AssessIcon
        AssessIcon();
        FormationMgr.AddAndPositionNonHQElement(element);
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        if (Elements.Count == Constants.One) {
            IsOperational = false;  // tell Cmd its dead
            D.Assert(IsDead);
            return;
        }

        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, element.DebugName);
        Data.RemoveElement(element.Data);


        if (!IsOperational) {
            // Cmd construction
            D.Assert(!IsDead);
            return;
        }

        UnifiedSRSensorMonitor.Remove(element.SRSensorMonitor);
        AssessIcon();
        if (!element.IsHQ) { // if IsHQ, restoring slot will be handled when HQElement changes
            FormationMgr.RestoreSlotToAvailable(element);
        }
    }

    /// <summary>
    /// Attempts to takeover this Cmd's ownership with player. Returns <c>true</c> if successful, <c>false</c> otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="takeoverStrength">The takeover strength. A value between 0 and 1.0.</param>
    /// <returns></returns>
    public bool __AttemptTakeover(Player player, float takeoverStrength) {
        D.AssertNotEqual(Owner, player);
        Utility.ValidateForRange(takeoverStrength, Constants.ZeroPercent, Constants.OneHundredPercent);
        if (takeoverStrength > Data.CurrentCmdEffectiveness) {   // TEMP takeoverStrength vs loyalty?
            Data.Owner = player;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Indicates whether this Unit is in the process of attacking <c>unit</c>.
    /// </summary>
    /// <param name="unitCmd">The unit command potentially under attack by this Unit.</param>
    /// <returns></returns>
    public abstract bool IsAttacking(IUnitCmd_Ltd unitCmd);

    internal void HandleSubordinateElementDeath(IUnitElement deadSubordinateElement) {
        // No ShowDebugLog as I always want this to report except when it doesn't compile
        if (deadSubordinateElement.IsHQ) {
            D.LogBold("{0} acknowledging {1} has been lost.", DebugName, deadSubordinateElement.DebugName);
        }
        else {
            D.Log("{0} acknowledging {1} has been lost.", DebugName, deadSubordinateElement.DebugName);
        }
        RemoveElement(deadSubordinateElement as AUnitElementItem);
        // state machine notification is after removal so attempts to acquire a replacement don't come up with same element
        if (IsDead) {
            return;    // no point in notifying Cmd's Dead state of the subordinate element's death that killed it
        }
        UponSubordinateElementDeath(deadSubordinateElement as AUnitElementItem);
    }

    private void AttachCmdToHQElement() {
        if (_hqJoint == null) {
            InitializeHQAttachmentSystem();
        }
        transform.position = HQElement.Position;
        // Note: Assigning connectedBody links the two rigidbodies at their current relative positions. Therefore the Cmd must be
        // relocated to the HQElement before the joint is made. Making the joint does not itself relocate Cmd to the newly connectedBody.
        _hqJoint.connectedBody = HQElement.gameObject.GetSafeComponent<Rigidbody>();
        //D.Log(ShowDebugLog, "{0}.Position = {1}, {2}.position = {3}.", HQElement.DebugName, HQElement.Position, DebugName, transform.position);
        //D.Log(ShowDebugLog, "{0} after attached by FixedJoint, rotation = {1}, {2}.rotation = {3}.", HQElement.DebugName, HQElement.transform.rotation, DebugName, transform.rotation);
    }

    #region Command Icon Management

    public void AssessIcon() {
        if (DisplayMgr != null) {
            var iconInfo = RefreshIconInfo();
            if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                UnsubscribeToIconEvents(DisplayMgr.Icon);
                //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                DisplayMgr.IconInfo = iconInfo;
                SubscribeToIconEvents(DisplayMgr.Icon);
            }
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract IconInfo MakeIconInfo();

    #endregion

    /// <summary>
    /// Assesses the alert status based on the proximity of enemy elements detected by sensors.
    /// </summary>
    /// <param name="wasAssessmentDeferred">if <c>true</c> this is a deferred assessment of standing down from RedAlert.</param>
    protected void AssessAlertStatus(bool wasAssessmentDeferred = false) {
        if (!wasAssessmentDeferred) {
            // normal assessment
            if (Data.AlertStatus == AlertStatus.Red) {

                if (_deferRedAlertStanddownAssessmentJob != null) {
                    // deferred assessment is already queued 
                    if (UnifiedSRSensorMonitor.AreWarEnemyCmdsInRange || UnifiedSRSensorMonitor.AreWarEnemyElementsInRange) {
                        // A deferred assessment is queued but more RedAlert criteria was detected so kill 
                        // the deferred assessment in case another loss of RedAlert criteria wants to start another deferred assessment. 
                        KillDeferRedAlertStanddownAssessmentJob();
                        // No reason to reassess as we already know we should be at RedAlert
                    }
                    // ...else already have assessment queued so wait for it
                    return;
                }

                D.AssertNull(_deferRedAlertStanddownAssessmentJob);
                // Already at RedAlert and no deferred assessment is underway, so consider deferring
                if (!UnifiedSRSensorMonitor.AreWarEnemyCmdsInRange) {
                    // We would normally stand down RedAlert so be cautious and defer assessment
                    D.Log(ShowDebugLog, "{0} is considering stand down of {1} from {2}.",
                        DebugName, typeof(AlertStatus).Name, Data.AlertStatus.GetValueName());
                    string jobName = "DelayStandDownRedAlertJob";
                    _deferRedAlertStanddownAssessmentJob = _jobMgr.WaitForHours(5F, jobName, (jobWasKilled) => {    // HACK
                        if (jobWasKilled) {
                            // Killed because more WarEnemy elements were detected at Short Range
                        }
                        else {
                            _deferRedAlertStanddownAssessmentJob = null;
                            AssessAlertStatus(wasAssessmentDeferred: true);
                        }
                    });
                }
                // ...else at RedAlert AND conditions don't allow a stand down so no point in assessing

                return; // The only way to stand down from RedAlert is thru deferred assessment
            }
            // not at RedAlert so continue with normal assessment
        }
        else {
            // this assessment was deferred so we must be at RedAlert
            D.AssertEqual(AlertStatus.Red, Data.AlertStatus);
            D.Log(ShowDebugLog, "{0} is standing down {1} from {2}.", DebugName, typeof(AlertStatus).Name, Data.AlertStatus.GetValueName());
        }

        // Do the assessment
        if (UnifiedSRSensorMonitor.AreWarEnemyCmdsInRange || UnifiedSRSensorMonitor.AreWarEnemyElementsInRange) {
            Data.AlertStatus = AlertStatus.Red;
        }
        else if (UnifiedSRSensorMonitor.AreEnemyCmdsInRange) {
            Data.AlertStatus = AlertStatus.Yellow;
        }
        else {
            if (!MRSensorMonitor.IsOperational) {
                D.Log(ShowDebugLog, "{0} is retaining YellowAlert as MRSensorMonitor is not operational.", DebugName);
                Data.AlertStatus = AlertStatus.Yellow;
            }
            else if (MRSensorMonitor.AreWarEnemyCmdsInRange) {
                Data.AlertStatus = AlertStatus.Yellow;
            }
            else {
                Data.AlertStatus = AlertStatus.Normal;
            }
        }
    }

    protected bool TryGetHQCandidatesOf(Priority priority, out IEnumerable<AUnitElementItem> hqCandidates) {
        D.AssertNotDefault((int)priority);
        hqCandidates = Elements.Where(e => e.Data.HQPriority == priority);
        return hqCandidates.Any();
    }

    protected override void PrepareForDeathNotification() {
        base.PrepareForDeathNotification();
        Data.FtlDampener.IsActivated = false;
        // 2.15.17 Moved here from Dead State in case Dead_EnterState becomes IEnumerator
        DeregisterForOrders();
    }

    protected abstract void ResetOrdersAndStateOnNewOwner();

    protected void RegisterForOrders() {
        OwnerAIMgr.RegisterForOrders(this);
    }

    private void DeregisterForOrders() {
        OwnerAIMgr.DeregisterForOrders(this);
    }

    // 7.20.16 HandleUserIntelCoverageChanged not needed as Cmd is not detectable. The only way Cmd IntelCoverage changes is when HQELement
    // Coverage changes. Icon needs to be assessed when any of Cmd's elements has its coverage changed as that can change which icon to show

    #region Event and Property Change Handlers

    private void MRSensorMonitorIsOperationalChangedEventHandler(object sender, EventArgs e) {
        HandleMRSensorMonitorIsOperationalChanged();
    }

    private void HandleMRSensorMonitorIsOperationalChanged() {
        AssessAlertStatus();
    }

    private void AwarenessOfFleetChangedEventHandler(object sender, PlayerAIManager.AwarenessOfFleetChangedEventArgs e) {
        if (IsOperational) {
            // no point in handling if not yet operational or dead
            IFleetCmd_Ltd fleet = e.Fleet;
            bool isAware = e.IsAware;
            if (!fleet.IsOperational && isAware) {
                // Filters out initial aware notifications generated by fleet.FinalInitialize before a fleet is operational
                // Works as we would never become aware of a fleet when it was already dead
                return;
            }
            //D.Log(ShowDebugLog, "{0} has just {1} of {2}.", DebugName, isAware ? "become aware" : "lost awareness", fleet.DebugName);
            HandleAwarenessOfFleetChanged(fleet, isAware);
        }
    }

    private void HandleAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        D.Assert(IsOperational);
        D.Assert(fleet.IsOperational);  // awareness changes not used when fleet dies
        D.AssertNotEqual(Owner, fleet.Owner_Debug); // should never be an awareness change from one of our own
        UponAwarenessOfFleetChanged(fleet, isAware);
    }

    private void EnemyCmdsInSensorRangeChangedEventHandler(object sender, EventArgs e) {
        HandleEnemyCmdsInSensorRangeChanged();
    }

    private void HandleEnemyCmdsInSensorRangeChanged() {
        AssessAlertStatus();
    }

    private void WarEnemyElementsInSensorRangeChangedEventHandler(object sender, EventArgs e) {
        HandleWarEnemyElementsInSensorRangeChanged();
    }

    private void HandleWarEnemyElementsInSensorRangeChanged() {
        AssessAlertStatus();
    }

    /// <summary>
    /// Handles a change in relations between players.
    /// <remarks> 7.14.16 Primary responsibility for handling Relations changes (existing relationship with a player changes) in Cmd
    /// and Element state machines rest with the Cmd. They implement HandleRelationsChanged and UponRelationsChanged.
    /// In all cases where the order is issued by either Cmd or User, the element FSM does not need to pay attention to Relations
    /// changes as their orders will be changed if a Relations change requires it, determined by Cmd. When the Captain
    /// overrides an order, those orders typically(so far) entail assuming station in one form or another, and/or repairing
    /// in place, sometimes in combination. A Relations change here should not affect any of these orders...so far.
    /// Upshot: Elements FSMs can ignore Relations changes.
    /// </remarks>
    /// </summary>
    /// <param name="player">The player whose relationship with our owner has changed.</param>
    public void HandleRelationsChangedWith(Player player) {
        SensorMonitors.ForAll(srm => srm.HandleRelationsChangedWith(player));
        Elements.ForAll(e => e.HandleRelationsChangedWith(player));
        UponRelationsChangedWith(player);
    }

    private void IsAvailablePropChangedHandler() {
        OnIsAvailable();
    }

    private void OnIsAvailable() {
        if (isAvailableChanged != null) {
            isAvailableChanged(this, EventArgs.Empty);
        }
    }

    private void HQElementPropChangingHandler(AUnitElementItem newHQElement) {
        HandleHQElementChanging(newHQElement);
    }

    protected virtual void HandleHQElementChanging(AUnitElementItem newHQElement) {
        Utility.ValidateNotNull(newHQElement);
        if (!Elements.Contains(newHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Error("{0} assigned HQElement {1} that is not already present in Unit.", DebugName, newHQElement.DebugName);
        }

        if (IsOperational) {
            // runtime assignment of HQ
            if (!newHQElement.transform.rotation.IsSame(transform.rotation)) {
                // Rotation of the newHQElement is different than the Cmd. UNCLEAR chg the Cmd or the newHQElement? 
                // 4.4.17 Aligning newHQElement to Cmd for now to avoid moving the Cmd's FormationStations
                newHQElement.transform.rotation = transform.rotation;
            }

            var previousHQElement = HQElement;
            D.AssertNotNull(previousHQElement);
            FormationMgr.RestoreSlotToAvailable(previousHQElement); // FormationMgr needs to know IsHQ to restore right slot
            previousHQElement.IsHQ = false;
            if (!previousHQElement.IsOperational) {
                return; // no reason to proceed further if previousHQElement is dead
            }
            FormationMgr.AddAndPositionNonHQElement(previousHQElement);
            previousHQElement.HandleChangeOfHQStatusCompleted();
        }
        else {
            // 4.5.17 First assignment of a HQ to an, as yet, non-operational Cmd. Can come from startup/new game or a FerryFleet
            D.Assert(!IsDead);
            D.AssertNull(HQElement);
        }

        float actualDeviation;  // OPTIMIZE
        if (!newHQElement.transform.rotation.IsSame(transform.rotation, out actualDeviation)) {
            D.Warn("{0}'s rotation differs from newHQElement {1}'s rotation by {2} degrees. Fixing newHQElement.",
                DebugName, newHQElement.DebugName, actualDeviation);
            newHQElement.transform.rotation = transform.rotation;
        }
    }

    private void HQElementPropChangedHandler() {
        HandleHQElementChanged();
    }

    private void HandleHQElementChanged() {
        float deviationAngle;
        if (!transform.rotation.IsSame(HQElement.transform.rotation, out deviationAngle)) {
            D.Error("{0}'s rotation is not the same as newHQElement {1}'s rotation. ActualDeviation = {2}.",
                DebugName, HQElement.DebugName, deviationAngle);
        }
        HQElement.IsHQ = true;
        Data.HQElementData = HQElement.Data;    // CmdData.Radius now returns Radius of new HQElement
        D.Log(/*ShowDebugLog,*/ "{0}'s HQElement is now {1}. Radius = {2:0.00}.", Data.ParentName, HQElement.Data.Name, Data.Radius);
        AttachCmdToHQElement(); // needs to occur before formation changed

        if (DisplayMgr != null) {
            DisplayMgr.ResizePrimaryMesh(Radius);
        }

        if (IsOperational) {
            // runtime so previous HQ has had its formation slot already restored to available
            FormationMgr.AddAndPositionHQElement(HQElement);
            HQElement.HandleChangeOfHQStatusCompleted();
            UponHQElementChanged();
        }
        else {
            FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
        }
    }

    private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem_Ltd deadFsmTgt = sender as IMortalItem_Ltd;
        UponFsmTgtDeath(deadFsmTgt);
    }

    private void FsmTgtInfoAccessChgdEventHandler(object sender, InfoAccessChangedEventArgs e) {
        IOwnerItem_Ltd fsmTgt = sender as IOwnerItem_Ltd;
        HandleFsmTgtInfoAccessChgd(e.Player, fsmTgt);
    }

    private void HandleFsmTgtInfoAccessChgd(Player playerWhoseInfoAccessChgd, IOwnerItem_Ltd fsmTgt) {
        if (playerWhoseInfoAccessChgd == Owner) {
            D.Log(ShowDebugLog, "{0}'s access to info about FsmTgt {1} has changed.", DebugName, fsmTgt.DebugName);
            UponFsmTgtInfoAccessChgd(fsmTgt);
        }
    }

    private void FsmTgtOwnerChgdEventHandler(object sender, EventArgs e) {
        IOwnerItem_Ltd fsmTgt = sender as IOwnerItem_Ltd;
        HandleFsmTgtOwnerChgd(fsmTgt);
    }

    private void HandleFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        OwnerAIMgr.awarenessOfFleetChanged -= AwarenessOfFleetChangedEventHandler;
        // TODO what to do about existing orders and availability?
        DeregisterForOrders();
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
        if (DisplayMgr != null) {
            DisplayMgr.MeshColor = Owner.Color;
        }
        AssessIcon();

        ResetOrdersAndStateOnNewOwner();
    }

    private void UnitFormationPropChangedHandler() {
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
    }

    private void AlertStatusPropChangedHandler() {
        UponAlertStatusChanged();
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessShowTrackingLabel();
    }

    protected override void HandleIsSelectedChanged() {
        base.HandleIsSelectedChanged();
        Elements.ForAll(e => e.AssessCircleHighlighting());
    }

    #endregion

    #region StateMachine Support Members

    /// <summary>
    /// The reported cause of a failure to complete execution of an Order.
    /// </summary>
    protected UnitItemOrderFailureCause _orderFailureCause;

    protected void KillRepairJob() {
        if (_repairJob != null) {
            _repairJob.Kill();
            _repairJob = null;
        }
    }

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
    }

    internal void HandleDamageIncurredBy(AUnitElementItem subordinateElement) {
        if (Data.UnitHealth == Constants.ZeroPercent) {
            if (!IsDead) {
                // 4.5.17 Just trying to confirm my suspicion that IsDead/IsOperational might not yet be set
                D.Warn("{0} is dead but IsDead not yet set!", DebugName);
            }
            return;
        }
        if (Data.UnitHealth < GeneralSettings.Instance.HealthThreshold_Damaged) {
            UponUnitDamageIncurred();
        }
    }

    /// <summary>
    /// Tries to get the UnitCmds found within the range of SRSensors.
    /// <remarks>Not currently used. A candidate for determining which direction to flee or which Cmd to attack.</remarks>
    /// </summary>
    /// <param name="enemyUnitCmds">The enemy unit Commands.</param>
    /// <param name="includeColdWarEnemies">if set to <c>true</c> [include cold war enemies].</param>
    /// <returns></returns>
    protected bool TryGetEnemyUnitCmds(out HashSet<IUnitCmd_Ltd> enemyUnitCmds, bool includeColdWarEnemies = false) {
        bool areEnemyCmdsDetectedBySRSensors = includeColdWarEnemies ? UnifiedSRSensorMonitor.AreEnemyCmdsInRange : UnifiedSRSensorMonitor.AreWarEnemyCmdsInRange;
        // RedAlert can continue after warTgts leave SRSensor range
        if (areEnemyCmdsDetectedBySRSensors) {
            enemyUnitCmds = includeColdWarEnemies ? UnifiedSRSensorMonitor.EnemyCmdsDetected : UnifiedSRSensorMonitor.WarEnemyCmdsDetected;
            return true;
        }
        enemyUnitCmds = null;
        return false;
    }

    /// <summary>
    /// Tries to determine the predominant direction that enemy elements within SRSensor range can be found.
    /// <remarks>If true, the direction returned is the mean of all the directions where enemy elements can be found.</remarks>
    /// <remarks>IMPROVE use a weighting that reflects the firepower of each element.</remarks>
    /// <remarks>Not currently used. A candidate for determining which direction to flee.</remarks>
    /// </summary>
    /// <param name="enemyDirection">The enemy direction.</param>
    /// <param name="includeColdWarEnemies">if set to <c>true</c> [include cold war enemies].</param>
    /// <returns></returns>
    protected bool TryGetPredominantEnemyDirection(out Vector3 enemyDirection, bool includeColdWarEnemies = false) {
        bool areEnemyElementsDetectedBySRSensors = includeColdWarEnemies ? UnifiedSRSensorMonitor.AreEnemyElementsInRange : UnifiedSRSensorMonitor.AreWarEnemyElementsInRange;
        // RedAlert can continue after warTgts leave SRSensor range
        if (areEnemyElementsDetectedBySRSensors) {
            var srEnemyElements = includeColdWarEnemies ? UnifiedSRSensorMonitor.EnemyElementsDetected : UnifiedSRSensorMonitor.WarEnemyElementsDetected;
            D.Assert(srEnemyElements.Any());

            var srEnemyElementLocations = srEnemyElements.Select(e => e.Position);
            enemyDirection = Position.FindMeanDirectionTo(srEnemyElementLocations);
            return true;
        }
        enemyDirection = Vector3.zero;
        return false;
    }

    protected void Dead_ExitState() {
        LogEventWarning();
    }

    #region Relays

    /// <summary>
    /// Called prior to entering the Dead state, this method notifies the current
    /// state that the unit is dying, allowing any current state housekeeping
    /// required before entering the Dead state.
    /// </summary>
    protected void UponDeath() { RelayToCurrentState(); }

    protected void UponEffectSequenceFinished(EffectSequenceID effectSeqID) { RelayToCurrentState(effectSeqID); }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    private void UponSubordinateElementDeath(AUnitElementItem deadSubordinateElement) { RelayToCurrentState(deadSubordinateElement); }

    private void UponEnemyDetected() { RelayToCurrentState(); }

    /// <summary>
    /// Called from the StateMachine just after a state
    /// change and just before state_EnterState() is called. When EnterState
    /// is a coroutine method (returns IEnumerator), the relayed version
    /// of this method provides an opportunity to configure the state
    /// before any other event relay methods can be called during the state.
    /// </summary>
    private void UponPreconfigureState() { RelayToCurrentState(); }

    private void UponRelationsChangedWith(Player player) { RelayToCurrentState(player); }

    // 2.15.17 No need for state-specific handling of owner change as existing orders are canceled and state returned to Idling

    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) { RelayToCurrentState(fleet, isAware); }

    private void UponHQElementChanged() { RelayToCurrentState(); }  // Called after Elements have been notified of HQChangeCompletion

    private void UponAlertStatusChanged() { RelayToCurrentState(); }

    /// <summary>
    /// Called whenever damage is incurred by this Unit, whether by a subordinate
    /// element or by the Cmd itself.
    /// </summary>
    private void UponUnitDamageIncurred() { RelayToCurrentState(); }

    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this Cmd's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// <remarks>Abstract to simply remind of need for functionality.</remarks>
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    protected abstract bool AssessNeedForRepair(float healthThreshold);

    /// <summary>
    /// Initiates repair.
    /// <remarks>Abstract to simply remind of need for functionality.</remarks>
    /// </summary>
    /// <param name="retainSuperiorsOrders">if set to <c>true</c> [retain superiors orders].</param>
    protected abstract void InitiateRepair(bool retainSuperiorsOrders);

    #endregion

    #region Combat Support

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit. Returns true if 
    /// the Command takes damage.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> the command's HQ element is still alive.</param>
    /// <param name="elementDamageSustained">The damage sustained by the HQ Element.</param>
    /// <param name="elementDamageSeverity">The severity of the damage sustained by the HQ Element.</param>
    /// <returns></returns>
    public bool __CheckForDamage(bool isHQElementAlive, DamageStrength elementDamageSustained, float elementDamageSeverity) {
        //D.Log(ShowDebugLog, "{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
        //DebugName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        var cmdMissedChance = Constants.OneHundredPercent - elementDamageSeverity;
        bool isMissed = isHQElementAlive ? RandomExtended.Chance(cmdMissedChance) : false;
        if (isMissed) {
            //D.Log(ShowDebugLog, "{0} avoided a hit.", DebugName);
        }
        else {
            TakeHit(elementDamageSustained);
        }
        return !isMissed;
    }

    public override void TakeHit(DamageStrength elementDamageSustained) {
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        DamageStrength damageToCmd = elementDamageSustained - Data.DamageMitigation;
        if (damageToCmd.Total > Constants.ZeroF) {
            float unusedDamageSeverity;
            bool isCmdAlive = ApplyDamage(damageToCmd, out unusedDamageSeverity);
            D.Assert(isCmdAlive, Data.DebugName);
            UponUnitDamageIncurred();
        }
    }

    /// <summary>
    /// Applies the damage to the command and returns true if the command survived the hit.
    /// </summary>
    /// <param name="damageToCmd">The damage sustained.</param>
    /// <param name="damageSeverity">The damage severity.</param>
    /// <returns>
    ///   <c>true</c> if the command survived.
    /// </returns>
    protected override bool ApplyDamage(DamageStrength damageToCmd, out float damageSeverity) {
        var __combinedDmgToCmd = damageToCmd.Total;
        var minAllowedCurrentHitPoints = 0.5F * Data.MaxHitPoints;
        var proposedCurrentHitPts = Data.CurrentHitPoints - __combinedDmgToCmd;
        if (proposedCurrentHitPts < minAllowedCurrentHitPoints) {
            Data.CurrentHitPoints = minAllowedCurrentHitPoints;
        }
        else {
            Data.CurrentHitPoints -= __combinedDmgToCmd;
        }
        D.Assert(Data.Health > Constants.ZeroPercent, "Should never fail as Commands can't die directly from a hit on the command");

        damageSeverity = Mathf.Clamp01(__combinedDmgToCmd / Data.CurrentHitPoints);
        AssessCripplingDamageToEquipment(damageSeverity);
        return true;
    }

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamageChance = damageSeverity;

        var undamagedDamageableSensors = Data.Sensors.Where(s => s.IsDamageable && !s.IsDamaged);
        undamagedDamageableSensors.ForAll(s => {
            s.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && s.IsDamaged, "{0}'s sensor {1} has been damaged.", DebugName, s.Name);
        });

        D.Assert(!Data.FtlDampener.IsDamageable);
    }

    /// <summary>
    /// Destroys any parents that are applicable to the Cmd. 
    /// <remarks>A Fleet and Starbase destroy their UnitContainer, but a Settlement 
    /// destroys its UnitContainer's parent, its CelestialOrbitSimulator.</remarks>
    /// </summary>
    /// <param name="delayInHours">The delay in hours.</param>
    protected virtual void DestroyApplicableParents(float delayInHours = Constants.ZeroF) {
        GameUtility.Destroy(UnitContainer.gameObject, delayInHours);
    }

    #endregion

    #endregion

    #region Show Tracking Label

    private void InitializeTrackingLabel() {
        DebugControls debugControls = DebugControls.Instance;
        debugControls.showUnitTrackingLabels += ShowUnitTrackingLabelsChangedEventHandler;
        if (debugControls.ShowUnitTrackingLabels) {
            EnableTrackingLabel(true);
        }
    }

    private void EnableTrackingLabel(bool toEnable) {
        if (toEnable) {
            if (_trackingLabel == null) {
                float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
                _trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.AboveRight, minShowDistance);
                _trackingLabel.Set(UnitName);
                _trackingLabel.Color = Owner.Color;
            }
            AssessShowTrackingLabel();
        }
        else {
            D.AssertNotNull(_trackingLabel);
            GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
            _trackingLabel = null;
        }
    }

    private void AssessShowTrackingLabel() {
        if (_trackingLabel != null) {
            bool toShow = IsDiscernibleToUser;
            _trackingLabel.Show(toShow);
        }
    }

    private void ShowUnitTrackingLabelsChangedEventHandler(object sender, EventArgs e) {
        EnableTrackingLabel(DebugControls.Instance.ShowUnitTrackingLabels);
    }

    private void CleanupTrackingLabel() {
        var debugControls = DebugControls.Instance;
        if (debugControls != null) {
            debugControls.showUnitTrackingLabels -= ShowUnitTrackingLabelsChangedEventHandler;
        }
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    #endregion

    private void KillDeferRedAlertStanddownAssessmentJob() {
        if (_deferRedAlertStanddownAssessmentJob != null) {
            _deferRedAlertStanddownAssessmentJob.Kill();
            _deferRedAlertStanddownAssessmentJob = null;
        }
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupTrackingLabel();
        KillDeferRedAlertStanddownAssessmentJob();
        if (UnifiedSRSensorMonitor != null) {
            UnifiedSRSensorMonitor.Dispose();
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }

        // Cmds can be destroyed before being initialized
        if (OwnerAIMgr != null) {
            OwnerAIMgr.awarenessOfFleetChanged -= AwarenessOfFleetChangedEventHandler;
        }
        if (MRSensorMonitor != null) {
            MRSensorMonitor.isOperationalChanged -= MRSensorMonitorIsOperationalChangedEventHandler;
        }
        if (SensorMonitors != null) {
            SensorMonitors.ForAll(sm => {
                sm.enemyCmdsInRangeChgd -= EnemyCmdsInSensorRangeChangedEventHandler;
                sm.warEnemyElementsInRangeChgd -= WarEnemyElementsInSensorRangeChangedEventHandler;
            });
        }
        if (UnifiedSRSensorMonitor != null) {
            UnifiedSRSensorMonitor.enemyCmdsInRangeChgd -= EnemyCmdsInSensorRangeChangedEventHandler;
            UnifiedSRSensorMonitor.warEnemyElementsInRangeChgd -= WarEnemyElementsInSensorRangeChangedEventHandler;
        }
    }

    private void UnsubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #endregion

    #region Debug

    private IDictionary<FsmTgtEventSubscriptionMode, bool> __subscriptionStatusLookup =
        new Dictionary<FsmTgtEventSubscriptionMode, bool>(FsmTgtEventSubscriptionModeEqualityComparer.Default) {
        {FsmTgtEventSubscriptionMode.TargetDeath, false },
        {FsmTgtEventSubscriptionMode.InfoAccessChg, false },
        {FsmTgtEventSubscriptionMode.OwnerChg, false }
    };

    /// <summary>
    /// Attempts subscribing or unsubscribing to <c>fsmTgt</c> in the mode provided.
    /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not.
    /// <remarks>Issues a warning if attempting to create a duplicate subscription.</remarks>
    /// </summary>
    /// <param name="subscriptionMode">The subscription mode.</param>
    /// <param name="fsmTgt">The target used by the State Machine.</param>
    /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected bool __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode subscriptionMode, IFleetNavigable fsmTgt, bool toSubscribe) {
        Utility.ValidateNotNull(fsmTgt);
        bool isSubscribeActionTaken = false;
        bool isDuplicateSubscriptionAttempted = false;
        IOwnerItem_Ltd itemFsmTgt = null;
        bool isSubscribed = __subscriptionStatusLookup[subscriptionMode];
        switch (subscriptionMode) {
            case FsmTgtEventSubscriptionMode.TargetDeath:
                var mortalFsmTgt = fsmTgt as IMortalItem_Ltd;
                if (mortalFsmTgt != null) {
                    if (!toSubscribe) {
                        mortalFsmTgt.deathOneShot -= FsmTargetDeathEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        mortalFsmTgt.deathOneShot += FsmTargetDeathEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.InfoAccessChg:
                itemFsmTgt = fsmTgt as IOwnerItem_Ltd;
                if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                    if (!toSubscribe) {
                        itemFsmTgt.infoAccessChgd -= FsmTgtInfoAccessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        itemFsmTgt.infoAccessChgd += FsmTgtInfoAccessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.OwnerChg:
                itemFsmTgt = fsmTgt as IOwnerItem_Ltd;
                if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                    if (!toSubscribe) {
                        itemFsmTgt.ownerChanged -= FsmTgtOwnerChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        itemFsmTgt.ownerChanged += FsmTgtOwnerChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(subscriptionMode));
        }
        if (isDuplicateSubscriptionAttempted) {
            D.Warn("{0}: Attempting to subscribe to {1}'s {2} when already subscribed.", DebugName, fsmTgt.DebugName, subscriptionMode.GetValueName());
        }
        if (isSubscribeActionTaken) {
            __subscriptionStatusLookup[subscriptionMode] = toSubscribe;
        }
        return isSubscribeActionTaken;
    }

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    #endregion

    #region IFleetNavigable Members

    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return UnitMaxFormationRadius + CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    public override bool IsRetainedFocusEligible { get { return UserIntelCoverage != IntelCoverage.None; } }

    #endregion

    #region IFormationMgrClient Members

    /// <summary>
    /// Positions the element in formation. This base class version simply places the 
    /// element at the designated offset location from the HQElement.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="stationSlotInfo">The slot information.</param>
    public virtual void PositionElementInFormation(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        AUnitElementItem unitElement = element as AUnitElementItem;
        unitElement.transform.localPosition = stationSlotInfo.LocalOffset;
        unitElement.__HandleLocalPositionManuallyChanged();
        //D.Log(ShowDebugLog, "{0} positioned at {1}, offset by {2} from {3} at {4}.",
        //element.DebugName, element.Position, stationSlotInfo.LocalOffset, HQElement.DebugName, HQElement.Position);
    }

    #endregion


}

