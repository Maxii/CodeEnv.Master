// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPropulsionManager.cs
// Manages the propulsion of a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the propulsion of a ship.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ShipPropulsionManager : AMonoBehaviourBase, ICameraFollowable, IZoomToClosest, IOnVisible {

    public FleetData Data { get; set; }
    public IntelLevel HumanPlayerIntelLevel { get; set; }

    private BoxCollider _collider;
    private Rigidbody _rigidbody;
    private GameEventManager _eventMgr;
    private Transform _transform;

    private GuiCursorHUD _cursorHud;
    private IGuiTrackingLabel _guiTrackingLabel;


    private GuiCursorHudText _guiCursorHudText;
    private GuiCursorHudTextFactory_Fleet _factory;



    private Vector3 thrustDirection = Vector3.forward;

    void Awake() {
        _transform = transform;
        // this approach allows this script to be located with the mesh or on the parent
        _rigidbody = gameObject.GetComponentInChildren<Rigidbody>();
        if (_rigidbody == null) {
            _rigidbody = _transform.parent.rigidbody;
            if (_rigidbody == null) {
                Debug.LogError("Can not find Rigidbody. Destroying {0}.".Inject(this.GetType().Name));
                Destroy(gameObject);
            }
        }
        _collider = gameObject.GetComponentInChildren<BoxCollider>();
        _eventMgr = GameEventManager.Instance;
        _cursorHud = GuiCursorHUD.Instance;
        InitializeGuiTrackingLabel();
        VisibilityState = Visibility.Visible;
        UpdateRate = UpdateFrequency.Normal;
    }

    private void InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefabGO = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefabGO == null) {
            Debug.LogError("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefabGO);
        // NGUITools.AddChild handles all scale, rotation, posiition, parent and layer settings
        string shipName = "Borg Ship";
        guiTrackingLabelCloneGO.name = shipName + CommonTerms.Label;

        _guiTrackingLabel = guiTrackingLabelCloneGO.GetInterface<IGuiTrackingLabel>();
        // assign the ship as the Target of the tracking label
        _guiTrackingLabel.Target = _transform;
        _guiTrackingLabel.Set(shipName);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
    }

    void Start() {
        __CreateData();
        HumanPlayerIntelLevel = IntelLevel.ShortRangeSensors;
    }

    private void __CreateData() {
        Data = new FleetData();
        Data.Name = gameObject.name;
        Data.CombatStrength = 5000;
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = Players.Opponent_3;
        Data.Position = _transform.position;
    }

    public void DisplayCursorHUD() {
        if (_factory == null) {
            _factory = new GuiCursorHudTextFactory_Fleet(Data);
        }
        if (_guiCursorHudText != null && _guiCursorHudText.IntelLevel == HumanPlayerIntelLevel) {
            // TODO this only updates the fleet's distance at this stage. Other values will also be dynamic and need updating
            Data.Position = _transform.position;    // TODO this should occur in either Update() or better, Data should reference _transform
            UpdateGuiCursorHudText(GuiCursorHudDisplayLineKeys.Distance);
        }
        else {
            _guiCursorHudText = _factory.MakeInstance_GuiCursorHudText(HumanPlayerIntelLevel);
        }
        _cursorHud.Set(_guiCursorHudText);
    }


    private void UpdateGuiCursorHudText(GuiCursorHudDisplayLineKeys key) {
        if (HumanPlayerIntelLevel != _guiCursorHudText.IntelLevel) {
            Debug.LogError("{0} {1} and {2} must be the same.".Inject(typeof(IntelLevel), HumanPlayerIntelLevel.GetName(), _guiCursorHudText.IntelLevel.GetName()));
            return;
        }

        IColoredTextList coloredTextList = _factory.MakeInstance_ColoredTextList(HumanPlayerIntelLevel, key);
        _guiCursorHudText.Replace(key, coloredTextList);
    }


    public void ClearCursorHUD() {
        _cursorHud.Clear();
    }

    void OnHover(bool isOver) {
        //Debug.Log("Ship.OnHover() called.");
        _guiTrackingLabel.IsHighlighted = isOver;
        if (isOver) {
            // _cursorHud.Set("Ship HUD Test");
            DisplayCursorHUD();
        }
        else {
            ClearCursorHUD();
        }
    }

    void OnDoubleClick() {
        thrustDirection = -thrustDirection;
    }

    void Update() {
        if (ToUpdate()) {
            OptimizeDisplay();
        }
    }

    private void OptimizeDisplay() {
        bool toEnableHeirarchy = false;
        bool toEnableGuiLabel = false;
        int distanceToCamera = _transform.DistanceToCameraInt();
        switch (VisibilityState) {
            case Visibility.Visible:
                if (distanceToCamera < TempGameValues.SystemAnimateDisplayThreshold) {
                    toEnableHeirarchy = true;
                }
                if (distanceToCamera < TempGameValues.SystemLabelDisplayThreshold) {
                    toEnableGuiLabel = true;
                }
                break;
            case Visibility.Invisible:
                // if invisible, neither the heirarchy or label should be enabled
                break;
            case Visibility.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(VisibilityState));
        }

        EnableHeirarchy(toEnableHeirarchy);
        _guiTrackingLabel.IsEnabled = toEnableGuiLabel;
    }

    void FixedUpdate() {
        _rigidbody.AddRelativeForce(thrustDirection * 1000);
    }

    private void EnableHeirarchy(bool toEnable) {
        // TODO
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 5.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.size.magnitude * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public void OnClick() {
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 20.0F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.size.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 2.0F;
    public float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

    #region IOnVisible Members

    public Visibility VisibilityState { get; set; }

    public void OnBecameVisible() {
        //Debug.Log("{0} has become visible.".Inject(gameObject.name));
        VisibilityState = Visibility.Visible;
        OptimizeDisplay();
    }

    public void OnBecameInvisible() {
        //Debug.Log("{0} has become invisible.".Inject(gameObject.name));
        VisibilityState = Visibility.Invisible;
        OptimizeDisplay();
    }

    #endregion

}

