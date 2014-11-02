// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
//  Item class for Systems. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Item class for Systems. 
/// </summary>
public class SystemItem : AItem, IZoomToFurthest, ISelectable {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    private SettlementCommandItem _settlement;
    public SettlementCommandItem Settlement {
        get { return _settlement; }
        set {
            if (_settlement != null && value != null) {
                D.Error("{0} cannot assign {1} when {2} is already present.", FullName, value.FullName, _settlement.FullName);
            }
            SetProperty<SettlementCommandItem>(ref _settlement, value, "Settlement", OnSettlementChanged);
        }
    }

    protected override float SphericalHighlightSizeMultiplier { get { return 1F; } }

    public float minCameraViewDistance = 2F;    // 2 units from the orbital plane
    public bool enableTrackingLabel = true;

    private ITrackingWidget _trackingLabel;
    private ICtxControl _ctxControl;
    private MeshRenderer __systemHighlightRenderer;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Radius = TempGameValues.SystemRadius;
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override void InitializeModelMembers() { }

    protected override void InitializeViewMembers() {
        base.InitializeViewMembers();
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SystemData>(Data);
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(this, WidgetPlacement.Above, minShowDistance);
        trackingLabel.Set(FullName);
        return trackingLabel;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        InitializeContextMenu(Owner);

        _orbitalPlaneCollider = gameObject.GetComponentInChildren<MeshCollider>();
        _orbitalPlaneCollider.isTrigger = true;
        _orbitalPlaneCollider.enabled = true;

        // IMPROVE meshRenderer's sole purpose right now is to allow receipt of visibility changes by CameraLosChangedListener 
        // Other ideas could include making an invisible bounds mesh for the plane like done for UIWidgets in CameraLosChangedListener
        var meshRenderer = _orbitalPlaneCollider.gameObject.GetComponent<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var orbitalPlaneLineRenderers = _orbitalPlaneCollider.gameObject.GetComponentsInChildren<LineRenderer>();
        orbitalPlaneLineRenderers.ForAll(lr => {
            lr.castShadows = false;
            lr.receiveShadows = false;
            lr.enabled = true;
        });

        var orbitalPlaneEventListener = UIEventListener.Get(_orbitalPlaneCollider.gameObject);
        orbitalPlaneEventListener.onHover += (go, isOver) => OnHover(isOver);
        orbitalPlaneEventListener.onClick += (go) => OnClick();
        orbitalPlaneEventListener.onDoubleClick += (go) => OnDoubleClick();
        orbitalPlaneEventListener.onPress += (go, isDown) => OnPress(isDown);

        var cameraLosChgdListener = CameraLosChangedListener.Get(_orbitalPlaneCollider.gameObject);
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;

        __systemHighlightRenderer = __FindSystemHighlight();
        __systemHighlightRenderer.castShadows = false;
        __systemHighlightRenderer.receiveShadows = false;
        __systemHighlightRenderer.enabled = true;
    }

    private void InitializeContextMenu(IPlayer owner) {
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        if (owner == TempGameValues.NoPlayer) {
            _ctxControl = new SystemCtxControl(this);
        }
        else {
            _ctxControl = owner.IsPlayer ? new SystemCtxControl_Player(this) as ICtxControl : new SystemCtxControl_AI(this);
        }
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    #region Model Methods

    private void OnSettlementChanged() {
        SettlementCmdData settlementData = null;
        if (Settlement != null) {
            settlementData = Settlement.Data;
            AttachSettlement(Settlement);
        }
        else {
            // The existing Settlement has been destroyed, so cleanup the orbit slot in prep for a future Settlement
            Data.SettlementOrbitSlot.DestroyOrbiter();
        }
        Data.SettlementData = settlementData;
        // The owner of a system and all it's celestial objects is determined by the ownership of the Settlement, if any
    }

    private void AttachSettlement(SettlementCommandItem settlementCmd) {
        Transform settlementUnit = settlementCmd.transform.parent;
        var orbiter = Data.SettlementOrbitSlot.AssumeOrbit(settlementUnit, "Settlement Orbiter"); // IMPROVE the only remaining OrbitSlot held in Data
        orbiter.enabled = settlementCmd.__OrbiterMoves;
        // enabling (or not) the system orbiter can also be handled by the SettlementCreator once isRunning
        D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);

        var systemIntelCoverage = PlayerIntel.CurrentCoverage;
        if (systemIntelCoverage == IntelCoverage.None) {
            D.Log("{0}.IntelCoverage set to None by its assigned System {1}.", settlementCmd.FullName, FullName);
        }
        // UNCLEAR should a new settlement being attached to a System take on the PlayerIntel state of the System??  See SystemPresenter.OnPlayerIntelCoverageChanged()
        Settlement.PlayerIntel.CurrentCoverage = systemIntelCoverage;
    }

    protected override void OnOwnerChanging(IPlayer newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsPlayer != newOwner.IsPlayer) {
                // Kind of owner has changed between AI, Player and NoPlayer so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        if (_trackingLabel != null) {
            _trackingLabel.Show(IsDiscernible);
        }
        _orbitalPlaneCollider.enabled = IsDiscernible;
        // orbitalPlane LineRenderers don't render when not visible to the camera
    }

    protected override void OnPlayerIntelCoverageChanged() {
        base.OnPlayerIntelCoverageChanged();
        // construct list each time as Settlement presence can change with time
        if (Settlement != null) {
            Settlement.PlayerIntel.CurrentCoverage = PlayerIntel.CurrentCoverage;
        }
        // The approach below acquired all item children in the system and gave them the same IntelCoverage as the system
        //var childItemsInSystem = gameObject.GetSafeMonoBehaviourComponentsInChildren<AItem>().Except(this);
        //childItemsInSystem.ForAll(i => i.PlayerIntel.CurrentCoverage = PlayerIntel.CurrentCoverage);
    }

    private void OnIsSelectedChanged() {
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        //D.Log("{0}.Highlight({1}) called. IsDiscernible = {2}, SystemHighlightRendererGO.activeSelf = {3}.",
        //gameObject.name, highlight, IsDiscernible, _systemHighlightRenderer.gameObject.activeSelf);
        switch (highlight) {
            case Highlights.Focused:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.FocusedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.FocusedColor.ToUnityColor());
                break;
            case Highlights.Selected:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.SelectedColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.SelectedColor.ToUnityColor());
                break;
            case Highlights.SelectedAndFocus:
                __systemHighlightRenderer.gameObject.SetActive(true);
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Main, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                __systemHighlightRenderer.material.SetColor(UnityConstants.MaterialColor_Outline, UnityDebugConstants.GeneralHighlightColor.ToUnityColor());
                break;
            case Highlights.None:
                __systemHighlightRenderer.gameObject.SetActive(false);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        return renderer;
    }

    #endregion

    #region Mouse Events

    // Note: No need to filter occlusion checking with IsDiscernible within these events 
    // as turning the collider on and off accomplishes the same thing. In the case of 
    // OnHover, if hovering and IsDiscernible changes to false, the Ngui event system
    // will send out an OnHover(false) when it sees the collider disappear.

    protected override void OnHover(bool isOver) {
        if (AssessOnHoverEvent(isOver)) {
            ExecuteOnHoverContent(isOver);
        }
    }

    protected override void OnClick() {
        GameObject occludedObject;
        if (CheckForOccludedObject(out occludedObject)) {
            GameInputHelper.Notify(occludedObject, "OnClick");
            return;
        }
        base.OnClick();
    }

    protected override void OnPress(bool isDown) {
        GameObject occludedObject;
        if (CheckForOccludedObject(out occludedObject)) {
            GameInputHelper.Notify(occludedObject, "OnPress", isDown);
            return;
        }
        base.OnPress(isDown);
    }

    protected override void OnDoubleClick() {
        GameObject occludedObject;
        if (CheckForOccludedObject(out occludedObject)) {
            GameInputHelper.Notify(occludedObject, "OnDoubleClick");
            return;
        }
        base.OnDoubleClick();
    }

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !GameInput.Instance.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #region Occluded Object Checking

    /* The Ngui event system generates events for the first object with a collider
     *  under the mouse that its Raycast encounters. As this SystemView [orbitalPlane] 
     *  collider hides the collider of any portion of an object that is behind it, some 
     *  or all of an object can effectively be occluded from detection by the event 
     *  system. The CheckForOccludedObject() method tests for and returns the 
     *  occluded object found, if any. If an object is found, it is notified of the event.
     *  
     * This is sufficient for all event types except OnHover. The Ngui Event system only 
     * generates OnHover events when it detects a different object under the mouse. This
     * means OnHover events are only generated twice per object - once when the object
     * is first detected under the mouse [OnHover(true)], and once when the same object
     * is no longer detected under the mouse (OnHover(false)). Because of this, an object
     * that is occluded cannot be detected because the object under the mouse doesn't 
     * change. The workaround implemented here is to continuously spawn occluded object
     * checks while the mouse is over the SystemView collider. To make this approach sufficiently 
     * responsive to the user, two different mechanisms spawn these checks - mouse movement 
     * and an elapsed time approach. The elapsed time approach is needed because some 
     * occluded objects move on their own (eg - fleets and planetoids) and can move under 
     * the mouse by themselves without any required user mouse movement.
     */

    /// <summary>
    /// Checks to see if this SystemView collider is occluding another object behind it.
    /// </summary>
    /// <param name="occludedObject">The occluded object that was found or null if no object was found.</param>
    /// <returns><c>true</c> if an occluded object was found, else <c>false</c>.</returns>
    private bool CheckForOccludedObject(out GameObject occludedObject) {
        Layers orbitalPlaneLayer = (Layers)_orbitalPlaneCollider.gameObject.layer;
        D.Assert(orbitalPlaneLayer == Layers.SystemOrbitalPlane, "{0} Layer {1} should be {2}.".Inject(GetType().Name, orbitalPlaneLayer.GetName(), Layers.SystemOrbitalPlane.GetName()));

        UICamera eventDispatcher = CameraControl.Instance.MainCameraEventDispatcher;
        var savedMask = eventDispatcher.eventReceiverMask;
        eventDispatcher.eventReceiverMask = savedMask.RemoveFromMask(orbitalPlaneLayer);
        bool isObjectOccluded = false;
        occludedObject = null;
        if (UICamera.Raycast(Input.mousePosition)) {
            // A spawned check from OnHover can occur before SystemView.OnHover(false) is called to turn off spawning.
            // This occurs when the mouse moves between the orbitalPlane and a UI element, resulting in hoveredObject returning the UI element
            if (!UICamera.isOverUI) {
                occludedObject = UICamera.hoveredObject;
                //D.Log("{0}.{1} found occluded object {2}.", Presenter.FullName, GetType().Name, occludedObject.name);
                isObjectOccluded = true;
            }
        }
        // Alternative way to Raycast
        //var maskWithoutSystemViewLayer = savedMask.RemoveFromMask(systemViewLayer);
        //RaycastHit hit;
        //if (Physics.Raycast(UICamera.currentRay, out hit, 500F, maskWithoutSystemViewLayer)) {
        //    occludedObject = hit.collider.gameObject;
        //}
        eventDispatcher.eventReceiverMask = savedMask;
        return isObjectOccluded;
    }

    #region OnHover Occluded Object Checking Workaround

    /// <summary>
    /// Executes the responsibilities of this class associated with an OnHover event.
    /// Separated from OnHover() itself so that it can be called when needed based
    /// on what occluded objects are found.
    /// </summary>
    /// <param name="isOver">if set to <c>true</c> [is over].</param>
    private void ExecuteOnHoverContent(bool isOver) {
        base.OnHover(isOver);
    }

    /// <summary>
    /// Assesses the OnHover events received from the Ngui Event System by this SystemView.
    /// Returns true if the event should be executed by this class, false if not.
    /// </summary>
    /// <param name="isOver">if set to <c>true</c> [is over].</param>
    /// <returns></returns>
    private bool AssessOnHoverEvent(bool isOver) {
        EnableCheckingForOccludedObjects(isOver);
        bool toExecute = false;
        if (isOver) {
            if (_currentOccludedObject == null) {
                // just arrived over the orbitalPlane with no occluded object beneath the mouse
                toExecute = true;
            }
        }
        else {
            // leaving the orbitalPlane
            toExecute = true;
            if (_currentOccludedObject != null) {
                // there is an occludedObject underneath the mouse as we leave the orbitalPlane
                GameInputHelper.Notify(_currentOccludedObject, "OnHover", false);
                _currentOccludedObject = null;
            }
        }
        return toExecute;
    }

    /// <summary>
    /// The occluded object recorded during the last check. Can be null.
    /// </summary>
    private GameObject _currentOccludedObject;

    /// <summary>
    /// The OnHover version of CheckForOccludedObject() which also
    /// </summary>
    private void CheckForOccludedObjectAndProcessOnHoverNotifications() {
        GameObject newOccludedObject;
        CheckForOccludedObject(out newOccludedObject);

        // now process any required notifications to said objects
        if (newOccludedObject == null) {
            // new state is not occluded
            if (_currentOccludedObject != null) {
                // occluded -> notOccluded transition
                GameInputHelper.Notify(_currentOccludedObject, "OnHover", false);
                ExecuteOnHoverContent(true);
                _currentOccludedObject = newOccludedObject;   // null
            }
            // notOccluded -> notOccluded transition: do nothing as System already knows hovered = true
        }
        else {
            // new state is occluded
            if (_currentOccludedObject != null) {
                // occluded -> occluded transition
                if (newOccludedObject != _currentOccludedObject) {
                    // occluded -> different occluded transition
                    GameInputHelper.Notify(_currentOccludedObject, "OnHover", false);
                    GameInputHelper.Notify(newOccludedObject, "OnHover", true);
                    _currentOccludedObject = newOccludedObject;
                }
                // occluded -> same occluded transition: do nothing
            }
            else {
                // notOccluded -> occluded transtion
                // also handles offSystem -> occluded transition with unneeded ProcessSystemViewOnHover(false)
                ExecuteOnHoverContent(false);
                GameInputHelper.Notify(newOccludedObject, "OnHover", true);
                _currentOccludedObject = newOccludedObject;
            }
        }
    }

    /// <summary>
    ///  Enables continuous checking for occluded objects over time (currently every 1 second)
    ///  and each time the mouse moves. Used to work around the fact that UICamera only sends 
    ///  OnHover events when the object under the mouse changes. This way, the check for an occluded 
    ///  object continues to occur even when the underlying object (this SystemView collider) remains
    ///  under the mouse.
    /// </summary>
    /// <param name="toEnable">if set to <c>true</c> enable checking. If false, disables it.</param>
    private void EnableCheckingForOccludedObjects(bool toEnable) {
        if (_spawnOccludedObjectChecksJob == null) {
            _spawnOccludedObjectChecksJob = new Job(SpawnOccludedObjectChecks());
        }
        if (toEnable) {
            CheckForOccludedObjectAndProcessOnHoverNotifications();
            _spawnOccludedObjectChecksJob.Start();
            UICamera.onMouseMove += OnMouseMoveWhileCheckingForOccludedObjects;
            //D.Log("Occluded Object Checking BEGUN.");
        }
        else {
            _spawnOccludedObjectChecksJob.Kill();
            UICamera.onMouseMove -= OnMouseMoveWhileCheckingForOccludedObjects;
            //D.Log("Occluded Object Checking ENDED.");
        }
    }

    /// <summary>
    /// Coroutine Job that spawns OccludedObjectChecks over time. Used to 
    /// detect occluded objects when the occluded object can move itself to/from
    /// under the mouse without any required mouse motion.
    /// </summary>
    private Job _spawnOccludedObjectChecksJob;

    /// <summary>
    /// Coroutine that spawns occluded object checks.
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnOccludedObjectChecks() {
        while (true) {
            CheckForOccludedObjectAndProcessOnHoverNotifications();
            yield return new WaitForSeconds(1F);
        }
    }

    private void OnMouseMoveWhileCheckingForOccludedObjects(Vector2 delta) {
        CheckForOccludedObjectAndProcessOnHoverNotifications();
    }

    #endregion

    #endregion

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return minCameraViewDistance; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    public override float OptimalCameraViewingDistance { get { return gameObject.DistanceToCamera(); } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

}

