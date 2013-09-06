// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetManager.cs
// Manages a Fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a Fleet.
/// </summary>
public class FleetManager : AGameObjectManager<FleetData> {

    public override float CurrentSpeed { get { return Data.LeadShipData.CurrentSpeed; } }

    private GuiTrackingLabel _trackingLabel;
    public GuiTrackingLabel TrackingLabel {
        get {
            if (_trackingLabel == null) {
                _trackingLabel = InitializeGuiTrackingLabel();
            }
            return _trackingLabel;
        }
    }

    /// <summary>
    /// The separation between the pivot point on the lead ship and the fleet icon,
    ///as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 _fleetIconOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);

    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;

    /// <summary>
    /// The offset that determines the point on the lead ship from which
    ///  the Fleet Icon pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _fleetIconPivotOffset;

    private SphereCollider _iconCollider;
    private Transform _billboardTransform;
    private Transform _fleetIcon;

    private ShipManager[] _shipManagers;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        gameObject.name = "Borg Fleet";
        // Collider is with the fleet icon because it needs to scale with the size of the icon
        _iconCollider = gameObject.GetComponentInChildren<SphereCollider>();
        _billboardTransform = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>().transform;
        _fleetIcon = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>().transform.parent;
        UpdateRate = UpdateFrequency.Continuous;
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        InitializeFleet();
        __InitializeData();
        __GetFleetUnderway();
    }

    protected override IntelLevel __InitializeIntelLevel() {
        return IntelLevel.LongRangeSensors;
    }

    private void InitializeFleet() {
        // overall fleet container gameobject is this FleetManager's parent
        _shipManagers = _transform.parent.gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipManager>();
    }

    protected override void __InitializeData() {
        Data = new FleetData(_transform);
        // there is no ItemName for a fleet
        Data.OptionalParentName = gameObject.name;
        Data.LastHumanPlayerIntelDate = new GameDate();

        foreach (var shipMgr in _shipManagers) {
            Data.AddShip(shipMgr.Data);
        }

        Transform leadShip = _shipManagers[0].transform;
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, leadShip.collider.bounds.extents.y, Constants.ZeroF);
        AssignLeadShip(leadShip.gameObject.GetSafeMonoBehaviourComponent<ShipManager>().Data);
    }

    public void AssignLeadShip(ShipData shipData) {
        Data.LeadShipData = shipData;
    }

    private void __GetFleetUnderway() {
        ChangeFleetHeading(_transform.forward);
        ChangeFleetSpeed(2.0F);
    }

    private GuiTrackingLabel InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefabGO = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefabGO == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return null;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefabGO);
        // NGUITools.AddChild handles all scale, rotation, posiition, parent and layer settings
        guiTrackingLabelCloneGO.name = gameObject.name + CommonTerms.Label;

        GuiTrackingLabel trackingLabel = guiTrackingLabelCloneGO.GetSafeMonoBehaviourComponent<GuiTrackingLabel>();
        // assign the ship as the Target of the tracking label
        trackingLabel.Target = _transform;
        trackingLabel.Set(gameObject.name);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
        return trackingLabel;
    }

    protected override GuiHudLineKeys[] OptionalCursorHudLinesToUpdate() {
        return new GuiHudLineKeys[1] {
            GuiHudLineKeys.Speed
        };
    }

    protected override void UpdateGuiCursorHudText(params GuiHudLineKeys[] keys) {
        if (HumanPlayerIntelLevel != _guiCursorHudText.IntelLevel) {
            D.Error("{0} {1} and {2} must be the same.".Inject(typeof(IntelLevel), HumanPlayerIntelLevel.GetName(), _guiCursorHudText.IntelLevel.GetName()));
            return;
        }
        IColoredTextList coloredTextList;
        foreach (var key in keys) {
            coloredTextList = GuiHudTextFactory.MakeInstance(key, HumanPlayerIntelLevel, Data);
            _guiCursorHudText.Replace(key, coloredTextList);
        }
    }

    public void ChangeFleetHeading(Vector3 newHeading) {
        foreach (var shipMgr in _shipManagers) {
            shipMgr.ChangeHeading(newHeading);
        }
    }

    public void ChangeFleetSpeed(float newSpeed) {
        foreach (var shipMgr in _shipManagers) {
            shipMgr.ChangeSpeed(newSpeed);
        }
    }

    protected override void OnToUpdate() {
        base.OnToUpdate();
        TrackLeadShip();
    }

    private void TrackLeadShip() {  // OPTIMIZE should be able to do this a bit more efficiently?
        _transform.position = Data.LeadShipData.Position;
        Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_transform.position + _fleetIconPivotOffset);
        _fleetIcon.localPosition = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconOffsetFromPivot) - _transform.position;
    }

    protected override void OptimizeDisplay() {
        bool toEnableHeirarchy = false;

        if (_isVisible) {
            toEnableHeirarchy = true;

            if (_trackingLabel != null) {
                int distanceToCamera = _transform.DistanceToCameraInt();
                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    TrackingLabel.IsShowing = true;
                }
                else {
                    TrackingLabel.IsShowing = false;
                }
            }
        }

        EnableHeirarchy(toEnableHeirarchy);
    }

    private void EnableHeirarchy(bool toEnable) {
        // TODO
        _billboardTransform.gameObject.SetActive(toEnable);
        _iconCollider.enabled = toEnable;
    }

    void OnDestroy() {
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

