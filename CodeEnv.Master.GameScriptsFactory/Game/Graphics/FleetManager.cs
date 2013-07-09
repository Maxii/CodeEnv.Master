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

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;
using System.Linq;

/// <summary>
/// Manages a Fleet.
/// </summary>
public class FleetManager : AMonoBehaviourBase {

    public FleetData Data { get; set; }
    public IntelLevel HumanPlayerIntelLevel { get; set; }
    public Transform LeadShip { get; set; }

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

    /// <summary>
    /// The offset that determines the point on the lead ship from which
    ///  the Fleet Icon pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _fleetIconPivotOffset;

    public int minTrackingLabelShowDistance = TempGameValues.MinFleetTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxFleetTrackingLabelShowDistance;

    private SphereCollider _iconCollider;
    private GameEventManager _eventMgr;
    private Transform _transform;
    private Transform _billboardTransform;
    private Transform _fleetIcon;

    private GuiCursorHud _cursorHud;
    private GuiCursorHudText _guiCursorHudText;
    private GuiCursorHudTextFactory_Fleet _factory;

    private ShipManager[] _shipManagers;
    private Navigator[] _shipNavigators;

    void Awake() {
        _transform = transform;
        gameObject.name = "Borg Fleet";
        // Collider is with the fleet icon because it needs to scale with the size of the icon
        _iconCollider = gameObject.GetComponentInChildren<SphereCollider>();
        _billboardTransform = gameObject.GetSafeMonoBehaviourComponentInChildren<BillboardManager>().transform;
        _fleetIcon = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>().transform.parent;
        _eventMgr = GameEventManager.Instance;
        _cursorHud = GuiCursorHud.Instance;
        UpdateRate = UpdateFrequency.Continuous;
    }


    void Start() {
        HumanPlayerIntelLevel = IntelLevel.ShortRangeSensors;
        InitializeFleet();
        __GetFleetUnderway();
    }

    private void InitializeFleet() {
        // overall fleet container gameobject is this FleetManager's parent
        _shipManagers = _transform.parent.gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipManager>();
        int length = _shipManagers.Length;
        _shipNavigators = new Navigator[length];
        for (int i = 0; i < length; i++) {
            _shipNavigators[i] = _shipManagers[i].Navigator;
        }
        LeadShip = _shipManagers[0].transform;
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, LeadShip.collider.bounds.extents.y, Constants.ZeroF);
        __InitializeFleetData();
    }

    // IMPROVE fleet data needs to be updatable when something in fleet data changes (besides speed)
    private void __InitializeFleetData() {
        Data = Data ?? new FleetData(_transform);
        Data.CombatStrength = new CombatStrength();
        foreach (var sMgr in _shipManagers) {
            ShipData sData = sMgr.Data;
            Data.CombatStrength.AddToTotal(sData.CombatStrength);
            // TODO more?
        }
        // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
        // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
        Data.MaxSpeed = _shipManagers.MinBy(mgr => mgr.Data.MaxSpeed).Data.MaxSpeed;

        Data.DateHumanPlayerExplored = new GameDate(1, TempGameValues.StartingGameYear);
        Data.Name = gameObject.name;
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = Players.Opponent_3;
        // Data.Composition = 
        UpdateSpeed();
    }

    public void __GetFleetUnderway() {
        foreach (Navigator nav in _shipNavigators) {
            nav.ChangeHeading(_transform.forward);
            nav.RequestedSpeed = 2.0F;
        }
    }

    private GuiTrackingLabel InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefabGO = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefabGO == null) {
            Debug.LogError("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
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

    public void UpdateSpeed() {
        // MinBy is a MoreLinq Nuget package extension method made available by Radical. I can also get it from
        // Nuget package manager, but installing it placed alot of things in my solution that I didn't know how to organize
        Data.Speed = _shipNavigators.MinBy(nav => nav.CurrentSpeed).CurrentSpeed;
    }

    public void DisplayCursorHUD() {
        if (_factory == null) {
            _factory = new GuiCursorHudTextFactory_Fleet(Data);
        }
        if (_guiCursorHudText != null && _guiCursorHudText.IntelLevel == HumanPlayerIntelLevel) {
            // TODO this only updates the fleet's distance at this stage. Other values will also be dynamic and need updating
            UpdateGuiCursorHudText(GuiCursorHudDisplayLineKeys.Distance, GuiCursorHudDisplayLineKeys.Speed);
        }
        else {
            _guiCursorHudText = _factory.MakeInstance_GuiCursorHudText(HumanPlayerIntelLevel);
        }
        _cursorHud.Set(_guiCursorHudText);
    }

    private void UpdateGuiCursorHudText(params GuiCursorHudDisplayLineKeys[] keys) {
        if (HumanPlayerIntelLevel != _guiCursorHudText.IntelLevel) {
            Debug.LogError("{0} {1} and {2} must be the same.".Inject(typeof(IntelLevel), HumanPlayerIntelLevel.GetName(), _guiCursorHudText.IntelLevel.GetName()));
            return;
        }

        foreach (var key in keys) {
            IColoredTextList coloredTextList = _factory.MakeInstance_ColoredTextList(HumanPlayerIntelLevel, key);
            _guiCursorHudText.Replace(key, coloredTextList);

        }
    }

    public void ClearCursorHUD() {
        _cursorHud.Clear();
    }


    public void ChangeFleetHeading(Vector3 newHeading) {
        foreach (var nav in _shipNavigators) {
            nav.ChangeHeading(newHeading);
        }
    }

    void Update() {
        if (ToUpdate()) {
            TrackLeadShip();
            OptimizeDisplayPerformance();
        }
    }

    private void TrackLeadShip() {
        _transform.position = LeadShip.position;
        Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_transform.position + _fleetIconPivotOffset);
        _fleetIcon.position = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconOffsetFromPivot);
    }

    private void OptimizeDisplayPerformance() {
        bool toEnableHeirarchy = false;

        if (UnityUtility.IsVisibleAt(_transform.position)) {
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

