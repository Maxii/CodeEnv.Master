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
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmd, IUnitCmd_Ltd, IFleetNavigableDestination, IUnitAttackable,
    IFormationMgrClient, ISensorDetector, IFsmEventSubscriptionMgrClient {

    private static float _healthThreshold_Damaged;
    protected static float HealthThreshold_Damaged {
        get {
            if (_healthThreshold_Damaged == Constants.ZeroF) {
                _healthThreshold_Damaged = GeneralSettings.Instance.UnitHealthThreshold_Damaged;
            }
            return _healthThreshold_Damaged;
        }
    }

    private static float _healthThreshold_BadlyDamaged;
    protected static float HealthThreshold_BadlyDamaged {
        get {
            if (_healthThreshold_BadlyDamaged == Constants.ZeroF) {
                _healthThreshold_BadlyDamaged = GeneralSettings.Instance.UnitHealthThreshold_BadlyDamaged;
            }
            return _healthThreshold_BadlyDamaged;
        }
    }

    private static float _healthThreshold_CriticallyDamaged;
    protected static float HealthThreshold_CriticallyDamaged {
        get {
            if (_healthThreshold_CriticallyDamaged == Constants.ZeroF) {
                _healthThreshold_CriticallyDamaged = GeneralSettings.Instance.UnitHealthThreshold_CriticallyDamaged;
            }
            return _healthThreshold_CriticallyDamaged;
        }
    }

    public event EventHandler isAvailableChanged;

    /// <summary>
    /// Fired when the receptiveness of this Unit to receiving new orders changes.
    /// </summary>
    [Obsolete("Use isAvailableChanged")]
    public event EventHandler availabilityChanged;

    public event EventHandler unitOutputsChanged;

    public Formation Formation { get { return Data.Formation; } }

    public string UnitName {
        get { return Data.UnitName; }
        set { Data.UnitName = value; }
    }

    /// <summary>
    /// The maximum radius of this Unit's current formation, independent of the number of elements currently assigned a
    /// station in the formation or whether the Unit's elements are located on their formation station. 
    /// Value encompasses each element's "KeepoutZone" (Facility: AvoidableObstacleZone, Ship: CollisionDetectionZone) 
    /// when the element is OnStation. 
    /// </summary>
    public float UnitMaxFormationRadius {
        get { return Data.UnitMaxFormationRadius; }
        private set { Data.UnitMaxFormationRadius = value; }
    }

    private Transform _unitContainer;
    /// <summary>
    /// The transform that normally contains all elements and commands assigned to the Unit.
    /// </summary>
    public Transform UnitContainer {
        get { return _unitContainer; }
        private set { SetProperty<Transform>(ref _unitContainer, value, "UnitContainer"); }
    }

    private NewOrderAvailability _availability = NewOrderAvailability.Available;
    /// <summary>
    /// Indicates how 'available' this Unit is to receiving new orders.
    /// </summary>
    public NewOrderAvailability Availability {
        get { return _availability; }
        private set {
            D.AssertNotEqual(_availability, value);     // filtering Availability_set values handled by ChangeAvailabilityTo()
            _availability = value;
        }
    }

    public bool IsAttackCapable { get { return Elements.Where(e => e.IsAttackCapable).Any(); } }

    /// <summary>
    /// Returns <c>true</c> if there is currently room in this Cmd for 1 element to join it.
    /// <remarks>For use only during operations (IsOperational == true) as it utilizes the FormationManager
    /// which is not initialized until after all elements have been added during construction.</remarks>
    /// </summary>
    public bool IsJoinable { get { return IsJoinableBy(Constants.One); } }

    public bool IsHeroPresent { get { return Data.Hero != TempGameValues.NoHero; } }

    public new bool IsOwnerChangeUnderway { get { return base.IsOwnerChangeUnderway; } }

    public override float Radius { get { return Data.Radius; } }

    public new AUnitCmdData Data {
        get { return base.Data as AUnitCmdData; }
        protected set { base.Data = value; }
    }

    public float CmdModuleHealth { get { return Data.CmdModuleHealth; } }

    public int ElementCount { get { return Elements.Count; } }

    protected AUnitElementItem _hqElement;
    /// <summary>
    /// The HQ Element.
    /// <remarks>Set this to initiate a change in the Commands HQ.</remarks>
    /// </summary>
    public AUnitElementItem HQElement {
        get { return _hqElement; }
        set { SetProperty<AUnitElementItem>(ref _hqElement, value, "HQElement", HQElementPropChangedHandler, HQElementPropChangingHandler); }
    }

    public IEnumerable<AUnitElementItem> NonHQElements { get { return Elements.Except(HQElement); } }

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

    public new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }

    protected AFormationManager FormationMgr { get; private set; }
    protected FsmEventSubscriptionManager FsmEventSubscriptionMgr { get; private set; }
    protected sealed override bool IsPaused { get { return _gameMgr.IsPaused; } }

    private IFtlDampenerRangeMonitor _ftlDampenerRangeMonitor;
    private ITrackingWidget _trackingLabel;
    private Job _deferRedAlertStanddownAssessmentJob;
    private FixedJoint _hqJoint;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
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
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, Formation>(d => d.Formation, UnitFormationPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, AlertStatus>(d => d.AlertStatus, AlertStatusPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, OutputsYield>(d => d.UnitOutputs, UnitOutputsPropChangedHandler));
    }

    // formations are now generated when an element is added and/or when a HQ element is assigned

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeTrackingLabel();
    }

    protected sealed override ADisplayManager MakeDisplayMgrInstance() {
        return new UnitCmdDisplayManager(this, TempGameValues.CmdMeshCullLayer);
    }

    protected sealed override void InitializeDisplayMgr() {
        base.InitializeDisplayMgr();
        DisplayMgr.MeshColor = Owner.Color;
        DisplayMgr.IconInfo = MakeIconInfo();
        SubscribeToIconEvents(DisplayMgr.TrackingIcon);
        DisplayMgr.ResizePrimaryMesh(Radius);
        //D.Log("{0} has initialized its DisplayMgr.", DebugName);
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
        var iconTransform = DisplayMgr.TrackingIcon.WidgetTransform;
        float radius = Screen.height * 0.03F;   // HACK
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
        InitializeFsmEventSubscriptionMgr();
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

    private void InitializeFsmEventSubscriptionMgr() {
        FsmEventSubscriptionMgr = new FsmEventSubscriptionManager(this);
    }

    /// <summary>
    /// Subscribes to sensor events including events from the UnifiedSRSensorMonitor.
    /// <remarks>Must be called after initial runtime state is set, otherwise events can arrive immediately as sensors activate.</remarks>
    /// </summary>
    protected void SubscribeToSensorEvents() {
        __ValidateStateForSensorEventSubscription();
        SensorMonitors.ForAll(sm => {
            sm.enemyCmdsInRangeChgd += EnemyCmdsInSensorRangeChangedEventHandler;
            sm.warEnemyElementsInRangeChgd += WarEnemyElementsInSensorRangeChangedEventHandler;
        });
        MRSensorMonitor.isOperationalChanged += MRSensorMonitorIsOperationalChangedEventHandler;
        UnifiedSRSensorMonitor.enemyCmdsInRangeChgd += EnemyCmdsInSensorRangeChangedEventHandler;
        UnifiedSRSensorMonitor.warEnemyElementsInRangeChgd += WarEnemyElementsInSensorRangeChangedEventHandler;
    }

    #endregion

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public void AddElement(AUnitElementItem element) {
        __ValidateAddElement(element);

        Elements.Add(element);
        Data.AddElement(element.Data);
        element.Command = this;
        element.AttachAsChildOf(UnitContainer);

        element.subordinateDeathOneShot += SubordinateDeathEventHandler;
        element.subordinateOwnerChanging += SubordinateOwnerChangingEventHandler;
        element.subordinateDamageIncurred += SubordinateDamageIncurredEventHandler;
        element.isAvailableChanged += SubordinateIsAvailableChangedEventHandler;
        element.subordinateOrderOutcome += SubordinateOrderOutcomeEventHandler;
        element.subordinateRepairCompleted += SubordinateRepairCompletedEventHandler;

        // 3.31.17 CmdSensor attachment to monitors now takes place when the sensor is built in UnitFactory.

        if (!IsOperational) {
            // Cmd construction
            D.Assert(!IsDead);
            D.AssertNull(HQElement);    // During Cmd construction, HQElement will be designated AFTER all Elements are
            return;                         // added resulting in _formationMgr adding all elements into the formation at once
        }

        UnifiedSRSensorMonitor.Add(element.SRSensorMonitor);   // HACK added it to FinalInitialize, like AssessIcon
        AssessIcon();
        FormationMgr.AddAndPositionNonHQElement(element);
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        element.PrepareForRemovalFromCmd();
        element.Command = null; // 4.21.17 Added to uncover issues before AddElement assigns new Cmd, if it occurs

        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, element.DebugName);
        Data.RemoveElement(element.Data);

        element.subordinateDeathOneShot -= SubordinateDeathEventHandler;
        element.subordinateOwnerChanging -= SubordinateOwnerChangingEventHandler;
        element.subordinateDamageIncurred -= SubordinateDamageIncurredEventHandler;
        element.isAvailableChanged -= SubordinateIsAvailableChangedEventHandler;
        element.subordinateOrderOutcome -= SubordinateOrderOutcomeEventHandler;
        element.subordinateRepairCompleted -= SubordinateRepairCompletedEventHandler;

        if (ElementCount == Constants.Zero) {
            D.Assert(element.IsHQ); // last element must be HQ
            element.IsHQ = false;
            IsDead = true;  // tell Cmd its dead
            D.LogBold("{0} has lost its last element and is dead.", DebugName);
            return;
        }

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
    /// Returns the elements that meet or exceed the minimum availability specified.
    /// </summary>
    /// <param name="minAvailability">The minimum availability.</param>
    /// <returns></returns>
    public IEnumerable<AUnitElementItem> GetElements(NewOrderAvailability minAvailability) {
        D.AssertNotDefault((int)minAvailability);
        return Elements.Where(e => e.Availability >= minAvailability);
    }

    /// <summary>
    /// Indicates whether this Unit is in the process of attacking <c>unit</c>.
    /// </summary>
    /// <param name="unitCmd">The unit command potentially under attack by this Unit.</param>
    /// <returns></returns>
    public abstract bool IsAttacking(IUnitCmd_Ltd unitCmd);

    /// <summary>
    /// Returns <c>true</c> if there is currently room in this Cmd for <c>elementCount</c> elements to join it.
    /// <remarks>For use only during operations (IsOperational == true) as it utilizes the FormationManager
    /// which is not initialized until after all elements have been added during construction.</remarks>
    /// </summary>
    /// <param name="elementCount">The element count.</param>
    /// <returns></returns>
    public abstract bool IsJoinableBy(int elementCount);

    /// <summary>
    /// Repairs the command module using the provided repair points and returns <c>true</c> if it has been fully repaired.
    /// </summary>
    /// <param name="repairPts">The repair points to use in restoring CurrentHitPts and damaged equipment.</param>
    /// <returns></returns>
    public bool RepairCmdModule(float repairPts) {
        return Data.RepairDamage(repairPts);
    }

    private void HandleSubordinateOwnerChanging(AUnitElementItem subordinateElement, Player incomingOwner) {
        if (ElementCount == Constants.One) {
            D.AssertEqual(subordinateElement, Elements.First());
            Data.Owner = incomingOwner;
        }
    }

    protected virtual void HandleSubordinateDeath(AUnitElementItem deadSubordinateElement) {
        // No ShowDebugLog as I always want this to report except when it doesn't compile
        if (deadSubordinateElement.IsHQ) {
            D.LogBold("{0} acknowledging {1} has been killed during State: {2}, Frame {3}.",
                DebugName, deadSubordinateElement.DebugName, CurrentState.ToString(), Time.frameCount);
        }
        else {
            D.Log("{0} acknowledging {1} has been killed during State: {2}, Frame {3}.",
                DebugName, deadSubordinateElement.DebugName, CurrentState.ToString(), Time.frameCount);
        }
        RemoveElement(deadSubordinateElement);
        // state machine notification is after removal so attempts to acquire a replacement don't come up with same element
        if (!IsDead) {
            // no point in notifying Cmd's Dead state of the subordinate element's death that killed it
            // UponSubordinateElementDeath(deadSubordinateElement);
        }
    }

    protected void AttachCmdToHQElement() {
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
                UnsubscribeToIconEvents(DisplayMgr.TrackingIcon);
                //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                DisplayMgr.IconInfo = iconInfo;
                SubscribeToIconEvents(DisplayMgr.TrackingIcon);
            }
        }
    }

    private TrackingIconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract TrackingIconInfo MakeIconInfo();

    #endregion

    /// <summary>
    /// Assesses the alert status based on the proximity of enemy elements detected by sensors.
    /// </summary>
    /// <param name="wasAssessmentDeferred">if <c>true</c> this is a deferred assessment of standing down from RedAlert.</param>
    protected void AssessAlertStatus(bool wasAssessmentDeferred = false) {
        D.Assert(!IsDead);
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
                            if (!IsDead) {
                                AssessAlertStatus(wasAssessmentDeferred: true);
                            }
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

    protected override void HideSelectedItemHud() {
        base.HideSelectedItemHud();
        UnitHudWindow.Instance.Hide();
    }

    protected override void PrepareForDeath() {
        base.PrepareForDeath();
        // 11.19.17 Must occur before Data.IsDead is set which deactivates all equipment generating events from SensorRangeMonitors
        UnsubscribeFromSensorEvents();
    }

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        __IsActivelyOperating = false;
        // 2.15.17 Moved here from Dead State in case Dead_EnterState becomes IEnumerator
        DeregisterForOrders();
    }

    protected override void PrepareForOnDeath() {
        base.PrepareForOnDeath();
        // 2.16.18 Notify FSM of death before notifying all other external parties of death
        ReturnFromCalledStates();
        UponDeath();    // 4.19.17 Do any reqd Callback before exiting current non-Call()ed state
    }

    public void HandleColdWarEnemyEngagementPolicyChanged() {
        Elements.ForAll(e => e.HandleColdWarEnemyEngagementPolicyChanged());
    }

    // 7.20.16 HandleUserIntelCoverageChanged not needed as Cmd is not detectable. The only way Cmd IntelCoverage changes is when HQELement
    // Coverage changes. Icon needs to be assessed when any of Cmd's elements has its coverage changed as that can change which icon to show

    #region Event and Property Change Handlers

    protected abstract void SubordinateOrderOutcomeEventHandler(object sender, AUnitElementItem.OrderOutcomeEventArgs e);

    protected void SubordinateIsAvailableChangedEventHandler(object sender, EventArgs e) {
        HandleSubordinateIsAvailableChanged(sender as AUnitElementItem);
    }

    protected void SubordinateRepairCompletedEventHandler(object sender, EventArgs e) {
        HandleSubordinateRepairCompleted(sender as AUnitElementItem);
    }

    protected void SubordinateDamageIncurredEventHandler(object sender, AUnitElementItem.SubordinateDamageIncurredEventArgs e) {
        HandleDamageIncurredBy(sender as AUnitElementItem, e.IsAlive, e.DamageIncurred, e.DamageSeverity);
    }

    protected void SubordinateDeathEventHandler(object sender, EventArgs e) {
        HandleSubordinateDeath(sender as AUnitElementItem);
    }

    protected void SubordinateOwnerChangingEventHandler(object sender, AUnitElementItem.SubordinateOwnerChangingEventArgs e) {
        HandleSubordinateOwnerChanging(sender as AUnitElementItem, e.IncomingOwner);
    }

    private void MRSensorMonitorIsOperationalChangedEventHandler(object sender, EventArgs e) {
        HandleMRSensorMonitorIsOperationalChanged();
    }

    private void EnemyCmdsInSensorRangeChangedEventHandler(object sender, EventArgs e) {
        HandleEnemyCmdsInSensorRangeChanged();
    }

    private void WarEnemyElementsInSensorRangeChangedEventHandler(object sender, EventArgs e) {
        HandleWarEnemyElementsInSensorRangeChanged();
    }

    [Obsolete]
    private void OnAvailabilityChanged() {
        if (availabilityChanged != null) {
            availabilityChanged(this, EventArgs.Empty);
        }
    }

    private void OnIsAvailableChanged() {
        if (isAvailableChanged != null) {
            isAvailableChanged(this, EventArgs.Empty);
        }
    }

    private void UnitOutputsPropChangedHandler() {
        OnUnitOutputsChanged();
    }

    private void OnUnitOutputsChanged() {
        if (unitOutputsChanged != null) {
            unitOutputsChanged(this, EventArgs.Empty);
        }
    }

    private void HQElementPropChangingHandler(AUnitElementItem newHQElement) {
        HandleHQElementChanging(newHQElement);
    }

    private void HQElementPropChangedHandler() {
        HandleHQElementChanged();
    }

    private void UnitFormationPropChangedHandler() {
        HandleFormationChanged();
    }

    private void AlertStatusPropChangedHandler() {
        HandleAlertStatusChanged();
    }

    #endregion

    /// <summary>
    /// Changes Availability to the provided value and selectively raises the isAvailableChanged event.
    /// <remarks>12.9.17 Handled this way to restrict firing availability changed events to only those times when Availability
    /// changes to/from Available. Otherwise, other Availability changes could atomically generate new orders which
    /// would fail the FSM condition that states can't be changed from void EnterState methods. Currently, changing 
    /// to Available is the only known generator of new orders and the IEnumerable Idling_EnterState method generates that change.
    /// All other states set Availability in their void PreconfigureState method so it occurs atomically after a new order is
    /// received following Idling changing to Available. This atomic change in Availability is necessary when a new order
    /// is received as that order can result in searches for elements or cmds that are 'Available'. If the value does not 
    /// atomically change, those searches will find candidates it thinks are available when in fact they are in the process
    /// of changing to another Availability value, creating errors. Accordingly, the other states can't use their IEnumerable
    /// EnterState to make the change as it wouldn't be atomic.</remarks>
    /// </summary>
    /// <param name="incomingAvailability">The availability value to change too.</param>
    protected void ChangeAvailabilityTo(NewOrderAvailability incomingAvailability) {
        if (Availability == incomingAvailability) {
            return;
        }
        bool toRaiseIsAvailableChgdEvent = Availability == NewOrderAvailability.Available || incomingAvailability == NewOrderAvailability.Available;
        Availability = incomingAvailability;
        if (toRaiseIsAvailableChgdEvent) {
            OnIsAvailableChanged();
        }
    }

    private void HandleSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) {
        UponSubordinateIsAvailableChanged(subordinateElement);
    }

    private void HandleSubordinateRepairCompleted(AUnitElementItem subordinateElement) {
        UponSubordinateRepairCompleted(subordinateElement);
    }

    protected virtual void HandleAlertStatusChanged() {
        UponAlertStatusChanged();
    }

    protected virtual void HandleFormationChanged() {
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>());
    }

    protected override void HandleNameChanged() {
        base.HandleNameChanged();
        UnitContainer.name = UnitName + GameConstants.CreatorExtension;
    }

    private void HandleMRSensorMonitorIsOperationalChanged() {
        if (!IsDead) {
            __ReportMRSensorStatus();
            AssessAlertStatus();
        }
    }

    private void HandleEnemyCmdsInSensorRangeChanged() {
        AssessAlertStatus();
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

    protected virtual void HandleHQElementChanging(AUnitElementItem incomingHQElement) {
        __ValidateHQElementChanging(incomingHQElement);

        if (!incomingHQElement.transform.rotation.IsSame(transform.rotation)) {
            // 4.4.17 Aligning newHQElement to Cmd for now to avoid moving the Cmd's FormationStations
            incomingHQElement.transform.rotation = transform.rotation;
        }

        if (IsOperational) {
            // runtime assignment of HQ
            var previousHQElement = HQElement;
            D.AssertNotNull(previousHQElement);
            FormationMgr.RestoreSlotToAvailable(previousHQElement); // FormationMgr needs to know IsHQ to restore right slot
            previousHQElement.IsHQ = false;
            if (previousHQElement.IsDead) {
                return; // no reason to proceed further if previousHQElement is dead
            }
            FormationMgr.AddAndPositionNonHQElement(previousHQElement); // assign new non-HQ slot
            previousHQElement.HandleChangeOfHQStatusCompleted();
        }
    }

    private void HandleHQElementChanged() {
        float deviationAngle;
        if (!transform.rotation.IsSame(HQElement.transform.rotation, out deviationAngle)) {
            D.Error("{0}'s rotation is not the same as newHQElement {1}'s rotation. ActualDeviation = {2}.",
                DebugName, HQElement.DebugName, deviationAngle);
        }
        HQElement.IsHQ = true;
        Data.HQElementData = HQElement.Data;    // CmdData.Radius now returns Radius of new HQElement
        //D.Log(/*ShowDebugLog, */"{0}'s HQElement is now {1}. Radius = {2:0.00}.", UnitName, HQElement.Name, Radius);
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
            FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>());
        }
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        DeregisterForOrders();
        ReturnFromCalledStates();
        UponLosingOwnership();  // 4.20.17 Do any reqd Callback before exiting current non-Call()ed state
        ResetOrderAndState();
    }

    protected sealed override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
        if (DisplayMgr != null) {
            DisplayMgr.MeshColor = Owner.Color;
        }
        AssessIcon();

        RegisterForOrders();
    }

    protected override void HandleIsDiscernibleToUserChanged() {
        base.HandleIsDiscernibleToUserChanged();
        AssessShowTrackingLabel();
    }

    protected override void HandleIsSelectedChanged() {
        base.HandleIsSelectedChanged();
        Elements.ForAll(e => e.AssessCircleHighlighting());
    }

    #region Orders Support Members

    /// <summary>
    /// The ID of the order currently being executed. Will be default(Guid) if no order is being executed.
    /// <remarks>This value represents the ID of the order currently being executed in the FSM. It is valid for that
    /// purpose from the time it is set in the new state's PreconfigureState() method until it is reset to default(Guid)
    /// in the state's ExitState() method. Accordingly, the current 
    /// state's _UponNewOrderReceived() and _ExitState() methods can all rely on it representing the order they have been 
    /// executing.</remarks>
    /// <remarks>The value is needed as CurrentOrder (with a different CmdOrderID) can change before the previous order
    /// finishes executing. It is used to support communication with this Cmd's elements, both incoming 
    /// cmd_UponOrderOutcomeCallbacks and outgoing element.CancelOrders() instructions.</remarks>
    /// </summary>
    protected Guid _executingOrderID;

    /// <summary>
    /// External access to ResetOrderAndState().
    /// Clears CurrentOrder (as well as any deferred order waiting for a pause to resume) and sets CurrentState to Idling. 
    /// If currently executing an order, each Element that was issued an order by the currently executing order may have that order cleared
    /// depending on this Cmd's executing order.
    /// <remarks>Used primarily by AUnitHud to allow easy cancellation of previously issued orders.</remarks>
    /// </summary>
    public void ClearOrdersAndIdle() {
        ReturnFromCalledStates();
        ResetOrderAndState();
    }

    /// <summary>
    /// Clears CurrentOrder (as well as any deferred order waiting for a pause to resume) and sets CurrentState to Idling. 
    /// If currently executing an order, each Element that was issued an order by the currently executing order will have that order cleared.
    /// </summary>
    protected virtual void ResetOrderAndState() {
        D.Assert(!IsDead);  // 1.25.18 Test to see if this can be called when already dead
        __ValidateCurrentStateNotACalledState();
        if (IsPaused) {
            D.Log("{0}.ResetOrderAndState called while paused.", DebugName);
        }
        // UponResetOrderAndState();
        ResetOrdersReceivedWhilePausedSystem();
    }

    protected abstract void ResetOrdersReceivedWhilePausedSystem();

    protected void ClearAllElementsOrders() {
        ClearElementsOrders(default(Guid));
    }

    /// <summary>
    /// Clears any element orders issued by the state(s) executing the order with the ID executingOrderID.
    /// <remarks>Will throw an error if executingOrderID doesn't represent an order, aka is default(Guid).</remarks>
    /// </summary>
    /// <param name="executingOrderID">The ID of the executing order.</param>
    protected void ClearAnyRemainingElementOrdersIssuedBy(Guid executingOrderID) {
        if (IsDead || IsOwnerChangeUnderway) {
            // _executingOrderID already set to default by ResetOrderAndState
            return;
        }
        D.AssertNotDefault(executingOrderID);
        ClearElementsOrders(executingOrderID);
    }

    /// <summary>
    /// Clears each Element's CurrentOrder and causes them to Idle if the Element's CurrentOrder has the provided cmdOrderID. 
    /// If cmdOrderID is the default, each Element's CurrentOrder will be cleared no matter what its cmdOrderID.
    /// </summary>
    private void ClearElementsOrders(Guid cmdOrderID) {
        IList<string> clearedOrderElementNames = new List<string>(Elements.Count);
        var elementsCopy = new List<AUnitElementItem>(Elements);    // e.ClearOrder() can result in death -> list modified while iterating exception
        foreach (var e in elementsCopy) {
            bool isCleared = e.ClearOrder(cmdOrderID);
            if (isCleared) {
                clearedOrderElementNames.Add(e.DebugName);
            }
        }

        if (clearedOrderElementNames.Any()) {
            D.Log("{0}.ClearElementsOrders() cleared {1} element's orders. Elements: {2}.",
                DebugName, clearedOrderElementNames.Count, clearedOrderElementNames.Concatenate());
        }
    }

    protected void RegisterForOrders() {
        OwnerAiMgr.RegisterForOrders(this);
    }

    private void DeregisterForOrders() {
        OwnerAiMgr.DeregisterForOrders(this);
    }

    #endregion

    #region StateMachine Support Members

    protected abstract bool IsCurrentStateCalled { get; }

    #region FsmReturnHandler System

    /// <summary>
    /// Stack of FsmReturnHandlers that are currently in use. 
    /// <remarks>Allows use of nested Call()ed states.</remarks>
    /// </summary>
    protected Stack<FsmReturnHandler> _activeFsmReturnHandlers = new Stack<FsmReturnHandler>();

    /// <summary>
    /// Removes the FsmReturnHandler from the top of _activeFsmReturnHandlers. 
    /// Throws an error if not on top.
    /// <remarks>4.12.17 Use when a Call()ed state Call()s another state to avoid warning in GetCurrentReturnHandler.</remarks>
    /// </summary>
    /// <param name="handlerToRemove">The handler to remove.</param>
    protected void RemoveReturnHandlerFromTopOfStack(FsmReturnHandler handlerToRemove) {
        var topHandler = _activeFsmReturnHandlers.Pop();
        D.AssertEqual(topHandler, handlerToRemove);
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the current Call()ed state.
    /// Throws an error if the CurrentState is not a Call()ed state or if not found.
    /// </summary>
    /// <returns></returns>
    protected FsmReturnHandler GetCurrentCalledStateReturnHandler() {
        D.Assert(IsCurrentStateCalled);
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        string currentStateName = CurrentState.ToString();
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != currentStateName) {
            // 4.11.17 This can occur in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, currentStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return GetCurrentCalledStateReturnHandler();
        }
        return peekHandler;
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the Call()ed state named <c>calledStateName</c>.
    /// Throws an error if not found.
    /// <remarks>TEMP version that allows use in CalledState_ExitState methods where CurrentState has already changed.</remarks>
    /// </summary>
    /// <param name="calledStateName">Name of the Call()ed state.</param>
    /// <returns></returns>
    [Obsolete("Not currently used")]
    protected FsmReturnHandler __GetCalledStateReturnHandlerFor(string calledStateName) {
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != calledStateName) {
            // 4.11.17 When an event occurs in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, calledStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return __GetCalledStateReturnHandlerFor(calledStateName);
        }
        return peekHandler;
    }

    #endregion

    /// <summary>
    /// Validates the common starting values of a State that is Call()able.
    /// </summary>
    protected virtual void ValidateCommonCallableStateValues(string calledStateName) {
        D.AssertNotEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        _activeFsmReturnHandlers.Peek().__Validate(calledStateName);
        D.Assert(!IsDead);
    }

    /// <summary>
    /// Validates the common starting values of a State that is not Call()able.
    /// </summary>
    protected virtual void ValidateCommonNonCallableStateValues() {
        D.AssertEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        D.AssertDefault(_executingOrderID);
        D.Assert(!IsDead);
    }

    protected virtual void ResetCommonCallableStateValues() { }

    protected virtual void ResetCommonNonCallableStateValues() {
        _activeFsmReturnHandlers.Clear();
        _executingOrderID = default(Guid);
    }

    protected void ReturnFromCalledStates() {
        while (IsCurrentStateCalled) {
            Return();
        }
        D.Assert(!IsCurrentStateCalled);
    }

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
    }

    private void HandleDamageIncurredBy(AUnitElementItem subordinateElement, bool isSubordinateAlive, DamageStrength damageIncurred, float damageSeverity) {
        D.Assert(!__debugSettings.AllPlayersInvulnerable);
        D.Assert(!IsDead);  // if subordinateElement didn't survive and its the last element, this Cmd should already have unsubscribed
        if (isSubordinateAlive && subordinateElement.IsHQ) {
            // check for damage to CmdModule
            var cmdModuleMissedChance = Constants.OneHundredPercent - damageSeverity;
            bool isCmdModuleMissed = RandomExtended.Chance(cmdModuleMissedChance);
            if (!isCmdModuleMissed) {
                DamageStrength damageToCmdModule = damageIncurred - Data.DamageMitigation;
                if (damageToCmdModule.Total > Constants.ZeroF) {
                    float unusedDamageSeverity;
                    bool isCmdAlive = Data.ApplyDamage(damageToCmdModule, out unusedDamageSeverity);
                    D.Assert(isCmdAlive);
                    StartEffectSequence(EffectSequenceID.CmdModuleHit);
                }
            }
            else {
                //D.Log(ShowDebugLog, "{0}'s CmdModule avoided a hit.", DebugName);
            }
        }
        UponUnitDamageIncurred();
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
    /// Called prior to nulling _currentOrder and setting CurrentState to Idling.
    /// Allows states to prepare themselves before their ExitState() method is called.
    /// <remarks>Not currently used as elements with construction underway is handled by the element's UponResetOrderAndState.</remarks>
    /// </summary>
    [Obsolete("Not currently used")]
    private void UponResetOrderAndState() { RelayToCurrentState(); }

    /// <summary>
    /// Called prior to entering the Dead state, this method notifies the current
    /// state that the unit is dying, allowing any current state housekeeping
    /// required before entering the Dead state.
    /// </summary>
    private void UponDeath() { RelayToCurrentState(); }

    /// <summary>
    /// Called prior to the Owner changing, this method notifies the current
    /// state that the unit is losing ownership, allowing any current state housekeeping
    /// required before the Owner is changed.
    /// </summary>
    private void UponLosingOwnership() { RelayToCurrentState(); }

    protected void UponEffectSequenceFinished(EffectSequenceID effectSeqID) { RelayToCurrentState(effectSeqID); }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    [Obsolete("Not currently used")]
    private void UponSubordinateDeath(AUnitElementItem deadSubordinateElement) { RelayToCurrentState(deadSubordinateElement); }

    private void UponSubordinateIsAvailableChanged(AUnitElementItem subordinateElement) { RelayToCurrentState(subordinateElement); }

    private void UponSubordinateRepairCompleted(AUnitElementItem subordinateElement) { RelayToCurrentState(subordinateElement); }

    [Obsolete("Not yet used")]
    private void UponEnemyDetected() { RelayToCurrentState(); }

    /// <summary>
    /// Called from the FSM just after a state change and just before state_EnterState() is called. 
    /// It atomically follows previousState.ExitState(). When EnterState is a coroutine method (returns IEnumerator), 
    /// the relayed version of this method provides an opportunity to configure the state before any other event relay 
    /// methods can be called during the state.
    /// </summary>
    private void UponPreconfigureState() { RelayToCurrentState(); }

    private void UponRelationsChangedWith(Player player) { RelayToCurrentState(player); }

    // 2.15.17 No need for state-specific handling of owner change as existing orders are canceled and state returned to Idling

    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponAwarenessChgd(IOwnerItem_Ltd item) { RelayToCurrentState(item); }

    private void UponHQElementChanged() { RelayToCurrentState(); }  // Called after Elements have been notified of HQChangeCompletion

    private void UponAlertStatusChanged() { RelayToCurrentState(); }

    /// <summary>
    /// Called whenever damage is incurred by this Unit, whether by a subordinate element or by the Cmd itself.
    /// </summary>
    private void UponUnitDamageIncurred() { RelayToCurrentState(); }

    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this Cmd's need for repair, returning <c>true</c> if repairs are needed, <c>false</c> otherwise.
    /// Default unitHealthThreshold is 100%.
    /// <remarks>1.12.18 Now OK to use from Repair-related states as RestartState and/or FsmReturnHandlers need it.</remarks>
    /// </summary>
    /// <param name="unitHealthThreshold">The health threshold.</param>
    /// <returns></returns>
    protected virtual bool AssessNeedForRepair(float unitHealthThreshold = Constants.OneHundredPercent) {
        D.Assert(!IsDead);
        if (__debugSettings.DisableRepair) {
            return false;
        }
        // 12.9.17 No need for _debugSettings.AllPlayersInvulnerable as its role is to keep damage from being taken
        if (__debugSettings.RepairAnyDamage) {
            unitHealthThreshold = Constants.OneHundredPercent;
        }
        if (Data.UnitHealth < unitHealthThreshold) {
            D.Log(ShowDebugLog, "{0} has determined it needs Repair.", DebugName);
            return true;
        }
        return false;
    }

    protected void AssessAvailabilityStatus_Repair() {
        __ValidateCurrentStateWhenAssessingAvailabilityStatus_Repair();
        NewOrderAvailability repairAvailabilityStatus;
        var unitHealth = Data.UnitHealth;
        // Can't Assert unitHealth < OneHundredPercent as possible for element to complete repair in 1 frame gaps

        if (Utility.IsInRange(unitHealth, HealthThreshold_Damaged, Constants.OneHundredPercent)) {
            repairAvailabilityStatus = NewOrderAvailability.EasilyAvailable;
        }
        else if (Utility.IsInRange(unitHealth, HealthThreshold_BadlyDamaged, HealthThreshold_Damaged)) {
            repairAvailabilityStatus = NewOrderAvailability.FairlyAvailable;
        }
        else {
            repairAvailabilityStatus = NewOrderAvailability.BarelyAvailable;
        }
        ChangeAvailabilityTo(repairAvailabilityStatus);
    }

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
    [Obsolete]
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
        throw new InvalidOperationException("{0} can't directly take a hit!".Inject(DebugName));
    }

    // 3.16.18 ApplyDamage and AssessCripplingDamageToEquipment moved to Data

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
        _debugCntls.showUnitTrackingLabels += ShowUnitTrackingLabelsChangedEventHandler;
        if (_debugCntls.ShowUnitTrackingLabels) {
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
        EnableTrackingLabel(_debugCntls.ShowUnitTrackingLabels);
    }

    private void CleanupTrackingLabel() {
        if (_debugCntls != null) {
            _debugCntls.showUnitTrackingLabels -= ShowUnitTrackingLabelsChangedEventHandler;
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
            var icon = DisplayMgr.TrackingIcon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
        UnsubscribeFromSensorEvents();
    }

    private void UnsubscribeFromSensorEvents() {
        // Cmds can be destroyed before being initialized
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

    /// <summary>
    /// Indicates whether this operational Cmd has commenced operations.
    /// <remarks> 1.12.18 OPTIMIZE Can be removed as Element's test for error during assault has never occurred.</remarks>
    /// </summary>
    public bool __IsActivelyOperating { get; protected set; }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateCurrentStateNotACalledState() {
        if (IsCurrentStateCalled) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
            D.Error("{0}.{1} should not be called while in Call()ed state {2}.", DebugName, callerIdMessage, CurrentState.ToString());
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    protected abstract void __ValidateCurrentStateWhenAssessingAvailabilityStatus_Repair();


    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateHQElementChanging(AUnitElementItem incomingHQElement) {
        D.AssertNotNull(incomingHQElement);
        if (!Elements.Contains(incomingHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Error("{0} assigned HQElement {1} that is not already present in Unit.", DebugName, incomingHQElement.DebugName);
        }
        if (!IsOperational) {
            // 4.5.17 First assignment of a HQ to an, as yet, non-operational Cmd. Can come from startup/new game or a formed fleet
            D.Assert(!IsDead);
            D.AssertNull(HQElement);
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    protected virtual void __ValidateAddElement(AUnitElementItem element) {
        if (Elements.Contains(element)) {
            D.Error("{0} attempting to add {1} that is already present.", DebugName, element.DebugName);
        }
        if (element.IsHQ) {
            D.Error("{0} adding element {1} already designated as the HQ Element.", DebugName, element.DebugName);
        }
        if (IsOperational) {
            // 5.8.17 FormationMgr not yet initialized when adding during construction
            D.Assert(IsJoinable, DebugName);
        }
    }

    [System.Diagnostics.Conditional("DEBUG")]
    protected abstract void __ValidateStateForSensorEventSubscription();

    public override bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        if (_debugCntls.IsAllIntelCoverageComprehensive) {
            return true;
        }
        if (IsOwnerChangeUnderway) {
            D.Error("NOT AN ERROR.");   // 5.20.17 OPTIMIZE Should no longer be needed now that 1) Cmds change owners before the last 
            return true;                // element Owner property actually changes, and 2) FleetCmds don't track the HQElement's
                                        // IntelCoverage change if the HQElement is about to create LoneFleetCmd and leave
        }
        bool isEntitled = Owner.IsRelationshipWith(player, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance);
        return isEntitled;
    }

    public override void __SimulateAttacked() {
        Elements.ForAll(e => e.__SimulateAttacked());
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ReportMRSensorStatus() {
        D.Assert(!IsDead);
        if (!IsApplicationQuiting) {
            if (!MRSensorMonitor.IsOperational) {
                string dmgText = Data.Sensors.Where(s => s.RangeCategory == RangeCategory.Medium).All(s => s.IsDamaged) ? "have been damaged and" : "are simply cycling but";
                D.Log("{0}.MRSensors {1} are down.", DebugName, dmgText);
            }
        }
    }

    #endregion

    #region IFleetNavigableDestination Members

    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually via the context menu
                return _optimalCameraViewingDistance;
            }
            return UnitMaxFormationRadius + CameraStat.OptimalViewingDistanceAdder;
        }
        set { base.OptimalCameraViewingDistance = value; }
    }

    public override bool IsRetainedFocusEligible { get { return UserIntelCoverage != IntelCoverage.None; } }

    #endregion

    #region IFormationMgrClient Members

    void IFormationMgrClient.PositionElementInFormation(IUnitElement element, FormationStationSlotInfo stationSlotInfo) {
        PositionElementInFormation_Internal(element, stationSlotInfo);
    }

    protected abstract void PositionElementInFormation_Internal(IUnitElement element, FormationStationSlotInfo stationSlotInfo);

    void IFormationMgrClient.HandleMaxFormationRadiusDetermined(float maxFormationRadius) {
        UnitMaxFormationRadius = maxFormationRadius;
    }

    #endregion

    #region IFsmEventSubscriptionMgrClient Members

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        D.Log(ShowDebugLog, "{0}'s access to info about FsmTgt {1} has changed.", DebugName, fsmTgt.DebugName);
        UponFsmTgtInfoAccessChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        UponFsmTgtDeath(deadFsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleAwarenessChgd(IMortalItem_Ltd item) {
        D.Assert(!IsDead);
        D.Assert(!item.IsDead, item.DebugName);  // awareness changes not used when item dies
        // 11.16.17 Can be owned by our owner when a new Cmd is created or Element Constructed or Refit
        UponAwarenessChgd(item);
    }

    #endregion

    #region IUnitCmd Members

    IUnitElement IUnitCmd.HQElement { get { return HQElement; } }

    OutputsYield IUnitCmd.UnitOutputs { get { return Data.UnitOutputs; } }

    #endregion

}

