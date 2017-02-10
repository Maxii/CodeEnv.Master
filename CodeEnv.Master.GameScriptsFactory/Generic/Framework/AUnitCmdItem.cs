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
public abstract class AUnitCmdItem : AMortalItemStateMachine, IUnitCmd, IUnitCmd_Ltd, IFleetNavigable, IUnitAttackable, IFormationMgrClient {

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

    public IList<ISensorRangeMonitor> SensorRangeMonitors { get; private set; }

    public new CmdCameraStat CameraStat {
        protected get { return base.CameraStat as CmdCameraStat; }
        set { base.CameraStat = value; }
    }

    protected new UnitCmdDisplayManager DisplayMgr { get { return base.DisplayMgr as UnitCmdDisplayManager; } }
    protected AFormationManager FormationMgr { get; private set; }

    private ITrackingWidget _trackingLabel;
    private FixedJoint _hqJoint;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Elements = new List<AUnitElementItem>();
        SensorRangeMonitors = new List<ISensorRangeMonitor>();
        FormationMgr = InitializeFormationMgr();
    }

    protected abstract AFormationManager InitializeFormationMgr();

    protected override void InitializeOnData() {
        base.InitializeOnData();
        D.AssertNotNull(transform.parent);
        UnitContainer = transform.parent;
        // the only collider is for player interaction with the item's CmdIcon
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitCmdData, Formation>(d => d.UnitFormation, UnitFormationPropChangedHandler));
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

    public override void FinalInitialize() {
        base.FinalInitialize();
        InitializeMonitorRanges();
        AssessIcon();
    }

    private void InitializeMonitorRanges() {
        SensorRangeMonitors.ForAll(srm => srm.InitializeRangeDistance());
    }

    #endregion  

    public override void CommenceOperations() {
        base.CommenceOperations();
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
        if (element.IsOperational != IsOperational) {
            D.Error("{0}: Adding element {1} with incorrect IsOperational state.", DebugName, element.DebugName);
        }
        Elements.Add(element);
        Data.AddElement(element.Data);
        element.Command = this;
        element.AttachAsChildOf(UnitContainer);
        //TODO consider changing HQElement
        var unattachedSensors = element.Data.Sensors.Where(sensor => sensor.RangeMonitor == null);
        if (unattachedSensors.Any()) {
            //D.Log(ShowDebugLog, "{0} is attaching {1}'s sensors: {2}.", DebugName, element.DebugName, unattachedSensors.Select(s => s.Name).Concatenate());
            AttachSensorsToMonitors(unattachedSensors.ToList());
            // WARNING: IEnumerable<T> lazy evaluation GOTCHA! The IEnumerable unattachedSensors at this point (after AttachSensorsToMonitors
            // completes) no longer points to any sensors as they would now all be attached. This is because the IEnumerable is not an 
            // already constructed collection. It is a pointer to a sensor that when called evaluates the sensor against the criteria. 
            // Since the Attach method modifies the sensor, the sensor being evaluated will no longer meet the unattached criteria, 
            // so unattachedSensors will no longer point to anything. Using ToList() to feed the method delivers a fully evaluated 
            // collection to the method, but this doesn't change the fact that Sensors now has no sensors that are unattached, thus 
            // the unattachedSensors IEnumerable when used again no longer points to anything.
        }
        if (!IsOperational) {
            // avoid the following extra work if adding during Cmd construction
            D.AssertNull(HQElement);    // During Cmd construction, HQElement will be designated AFTER all Elements are
            return;                         // added resulting in _formationMgr adding all elements into the formation at once
        }
        AssessIcon();
        FormationMgr.AddAndPositionNonHQElement(element);
    }

    /// <summary>
    /// Attaches one or more sensors to this command's SensorRangeMonitors.
    /// Note: Sensors are part of a Unit's elements but the monitors they attach to
    /// are children of the Command. Thus sensor range is always measured from
    /// the Command, not from the element.
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    private void AttachSensorsToMonitors(IList<Sensor> sensors) {
        sensors.ForAll(sensor => {
            var monitor = UnitFactory.Instance.AttachSensorToCmdsMonitor(sensor, this);
            if (!SensorRangeMonitors.Contains(monitor)) {
                // only need to record and setup range monitors once. The same monitor can have more than 1 sensor
                SensorRangeMonitors.Add(monitor);
                monitor.enemyTargetsInRange += EnemyTargetsInSensorRangeChangedEventHandler;
            }
        });
    }

    /// <summary>
    /// Detaches one or more sensors from this command's SensorRangeMonitors.
    /// Note: Sensors are part of a Unit's elements but the monitors they attach to
    /// are children of the Command. Thus sensor range is always measured from
    /// the Command, not from the element.
    /// </summary>
    /// <param name="sensors">The sensors.</param>
    private void DetachSensorsFromMonitors(IList<Sensor> sensors) {
        sensors.ForAll(sensor => {
            var monitor = sensor.RangeMonitor;
            bool isRangeMonitorStillInUse = monitor.Remove(sensor);

            if (!isRangeMonitorStillInUse) {
                monitor.enemyTargetsInRange -= EnemyTargetsInSensorRangeChangedEventHandler;
                monitor.Reset();    // OPTIMIZE either reset or destroy, not both
                SensorRangeMonitors.Remove(monitor);
                //D.Log(ShowDebugLog, "{0} is destroying unused {1} as a result of removing {2}.", DebugName, typeof(SensorRangeMonitor).Name, sensor.Name);
                GameUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
            }
        });
    }

    public virtual void RemoveElement(AUnitElementItem element) {
        bool isRemoved = Elements.Remove(element);
        D.Assert(isRemoved, element.DebugName);
        Data.RemoveElement(element.Data);

        DetachSensorsFromMonitors(element.Data.Sensors);
        if (!IsOperational) {
            return; // avoid the following work if removing during startup
        }
        if (Elements.Count == Constants.Zero) {
            if (Data.UnitHealth > Constants.ZeroF) {
                D.Error("{0} has UnitHealth of {1:0.0000} remaining.", DebugName, Data.UnitHealth);
            }
            IsOperational = false;  // tell Cmd its dead
            return;
        }
        AssessIcon();
        FormationMgr.HandleElementRemoval(element);
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

    public void HandleSubordinateElementDeath(IUnitElement deadSubordinateElement) {
        // No ShowDebugLog as I always want this to report except when it doesn't compile
        D.LogBold("{0} acknowledging {1} has been lost.", DebugName, deadSubordinateElement.DebugName);
        RemoveElement(deadSubordinateElement as AUnitElementItem);
        // state machine notification is after removal so attempts to acquire a replacement don't come up with same element
        if (IsOperational) {    // no point in notifying Cmd's Dead state of the subordinate element's death that killed it
            UponSubordinateElementDeath(deadSubordinateElement as AUnitElementItem);
        }
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
    protected void AssessAlertStatus() {

        Profiler.BeginSample("AssessAlertStatus LINQ IEnumerables", gameObject);
        var sensorRangeCatsDetectingEnemy = SensorRangeMonitors.Where(srm => srm.AreEnemyTargetsInRange).Select(srm => srm.RangeCategory);
        var sensorRangeCatsDetectingWarEnemy = SensorRangeMonitors.Where(srm => srm.AreEnemyWarTargetsInRange).Select(srm => srm.RangeCategory);
        Profiler.EndSample();

        if (sensorRangeCatsDetectingWarEnemy.Contains(RangeCategory.Short)) {
            Data.AlertStatus = AlertStatus.Red;
        }
        else if (sensorRangeCatsDetectingWarEnemy.Contains(RangeCategory.Medium) || sensorRangeCatsDetectingEnemy.Contains(RangeCategory.Short)) {
            Data.AlertStatus = AlertStatus.Yellow;
        }
        else {
            Data.AlertStatus = AlertStatus.Normal;
        }
    }

    protected bool TryGetHQCandidatesOf(Priority priority, out IEnumerable<AUnitElementItem> hqCandidates) {
        D.AssertNotDefault((int)priority);
        hqCandidates = Elements.Where(e => e.Data.HQPriority == priority);
        return hqCandidates.Any();
    }

    // 7.20.16 Not needed as Cmd is not detectable. The only way Cmd IntelCoverage changes is when HQELement Coverage changes.
    // Icon needs to be assessed when any of Cmd's elements has its coverage changed as that can change which icon to show
    //// protected override void HandleUserIntelCoverageChanged() {
    ////    base.HandleUserIntelCoverageChanged();
    ////    AssessIcon();   
    //// }

    #region Event and Property Change Handlers

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
            D.Log(ShowDebugLog, "{0} has just {1} of {2}.", DebugName, isAware ? "become aware" : "lost awareness", fleet.DebugName);
            HandleAwarenessOfFleetChanged(fleet, isAware);
        }
    }

    private void HandleAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        D.Assert(IsOperational);
        D.Assert(fleet.IsOperational);  // awareness changes not used when fleet dies
        D.AssertNotEqual(Owner, fleet.Owner_Debug); // should never be an awareness change from one of our own
        UponAwarenessOfFleetChanged(fleet, isAware);
    }


    private void EnemyTargetsInSensorRangeChangedEventHandler(object sender, EventArgs e) {
        HandleEnemyTargetsInSensorRangeChanged();
    }

    protected abstract void HandleEnemyTargetsInSensorRangeChanged();

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
    /// <param name="chgdRelationsPlayer">The other player.</param>
    public void HandleRelationsChanged(Player chgdRelationsPlayer) {
        SensorRangeMonitors.ForAll(srm => srm.HandleRelationsChanged(chgdRelationsPlayer));
        Elements.ForAll(e => e.HandleRelationsChanged(chgdRelationsPlayer));
        UponRelationsChanged(chgdRelationsPlayer);
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
        var previousHQElement = HQElement;
        if (previousHQElement != null) {
            previousHQElement.IsHQ = false;
            // don't remove previousHQElement.ShowDebugLog if ShowHQDebugLog as its probably dieing
        }
        else {
            // first assignment of HQ
            D.Assert(!IsOperational);
            // OPTIMIZE Just a FYI warning as formations currently assume this
            if (newHQElement.transform.rotation != Quaternion.identity) {
                D.Warn("{0} first HQ Element rotation = {1}.", DebugName, newHQElement.transform.rotation);
            }
        }
        if (!Elements.Contains(newHQElement)) {
            // the player will typically select/change the HQ element of a Unit from the elements already present in the unit
            D.Warn("{0} assigned HQElement {1} that is not already present in Unit.", DebugName, newHQElement.DebugName);
            AddElement(newHQElement);
        }
    }

    private void HQElementPropChangedHandler() {
        HandleHQElementChanged();
    }

    private void HandleHQElementChanged() {
        HQElement.IsHQ = true;
        Data.HQElementData = HQElement.Data;    // CmdData.Radius now returns Radius of new HQElement
        //D.Log(ShowDebugLog, "{0}'s HQElement is now {1}. Radius = {2:0.##}.", Data.ParentName, HQElement.Data.Name, Data.Radius);
        AttachCmdToHQElement(); // needs to occur before formation changed
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
        if (DisplayMgr != null) {
            DisplayMgr.ResizePrimaryMesh(Radius);
        }
    }

    private void FsmTargetDeathEventHandler(object sender, EventArgs e) {
        IMortalItem_Ltd deadFsmTgt = sender as IMortalItem_Ltd;
        UponFsmTgtDeath(deadFsmTgt);
    }

    private void FsmTgtInfoAccessChgdEventHandler(object sender, InfoAccessChangedEventArgs e) {
        IItem_Ltd fsmTgt = sender as IItem_Ltd;
        HandleFsmTgtInfoAccessChgd(e.Player, fsmTgt);
    }

    private void HandleFsmTgtInfoAccessChgd(Player playerWhoseInfoAccessChgd, IItem_Ltd fsmTgt) {
        if (playerWhoseInfoAccessChgd == Owner) {
            UponFsmTgtInfoAccessChgd(fsmTgt);
        }
    }

    private void FsmTgtOwnerChgdEventHandler(object sender, EventArgs e) {
        IItem_Ltd fsmTgt = sender as IItem_Ltd;
        HandleFsmTgtOwnerChgd(fsmTgt);
    }

    private void HandleFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        OwnerAIMgr.awarenessOfFleetChanged -= AwarenessOfFleetChangedEventHandler;
        // TODO what to do about existing orders and availability?
        ////UnregisterForOrders();
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
        UponOwnerChanged();
        // TODO what to do about existing orders and availability?
        ////OwnerAIMgr.RegisterForOrders(this);

    }

    private void UnitFormationPropChangedHandler() {
        FormationMgr.RepositionAllElementsInFormation(Elements.Cast<IUnitElement>().ToList());
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

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
    }

    protected void UnregisterForOrders() {
        OwnerAIMgr.DeregisterForOrders(this);
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

    private void UponRelationsChanged(Player chgdRelationsPlayer) { RelayToCurrentState(chgdRelationsPlayer); }

    private void UponOwnerChanged() { RelayToCurrentState(); }

    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponAwarenessOfFleetChanged(IFleetCmd_Ltd fleet, bool isAware) {
        RelayToCurrentState(fleet, isAware);
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
    public bool __CheckForDamage(bool isHQElementAlive, DamageStrength elementDamageSustained, float elementDamageSeverity) {
        //D.Log(ShowDebugLog, "{0}.__CheckForDamage() called. IsHQElementAlive = {1}, ElementDamageSustained = {2}, ElementDamageSeverity = {3}.",
        //DebugName, isHQElementAlive, elementDamageSustained, elementDamageSeverity);
        var cmdMissedChance = Constants.OneHundredPercent - elementDamageSeverity;
        bool missed = (isHQElementAlive) ? RandomExtended.Chance(cmdMissedChance) : false;
        if (missed) {
            //D.Log(ShowDebugLog, "{0} avoided a hit.", DebugName);
        }
        else {
            TakeHit(elementDamageSustained);
        }
        return !missed;
    }

    public override void TakeHit(DamageStrength elementDamageSustained) {
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        DamageStrength damageToCmd = elementDamageSustained - Data.DamageMitigation;
        float unusedDamageSeverity;
        bool isCmdAlive = ApplyDamage(damageToCmd, out unusedDamageSeverity);
        D.Assert(isCmdAlive, Data.DebugName);
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

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        CleanupTrackingLabel();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
        if (OwnerAIMgr != null) {    // Cmds can be destroyed before being initialized
            OwnerAIMgr.awarenessOfFleetChanged -= AwarenessOfFleetChangedEventHandler;
        }
    }

    private void UnsubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    // subscriptions contained completely within this gameObject (both subscriber
    // and subscribed) do not have to be cleaned up as all instances are destroyed

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
        IItem_Ltd itemFsmTgt = null;
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
                itemFsmTgt = fsmTgt as IItem_Ltd;
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
                itemFsmTgt = fsmTgt as IItem_Ltd;
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

