// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemManager.cs
// Manages a System. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a System. 
/// </summary>
public class SystemManager : AMonoBehaviourBase, IOnVisibleRelayTarget {

    public SystemData Data { get; private set; }
    public IntelLevel HumanPlayerIntelLevel { get; set; }
    public Visibility VisibilityState { get; set; }

    private GuiTrackingLabel _trackingLabel;
    public GuiTrackingLabel TrackingLabel {
        get {
            _trackingLabel = _trackingLabel ?? InitializeGuiTrackingLabel();
            return _trackingLabel;
        }
    }

    /// <summary>
    /// The separation between the pivot point on the lead ship and the fleet icon,
    ///as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 trackingLabelOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);
    public int maxAnimateDistance = TempGameValues.MaxSystemAnimateDistance;
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    private GuiCursorHudText _guiCursorHudText;
    private GuiCursorHudTextFactory_System _factory;

    private Transform _transform;
    private StarManager _starManager;
    private GuiCursorHud _cursorHud;

    void Awake() {
        _transform = transform;
        _starManager = gameObject.GetSafeMonoBehaviourComponentInChildren<StarManager>();
        _cursorHud = GuiCursorHud.Instance;
        VisibilityState = Visibility.Visible;
        UpdateRate = UpdateFrequency.Seldom;
    }

    void Start() {
        HumanPlayerIntelLevel = IntelLevel.OutOfDate;
        __InitializeSystemData();
    }

    private void __InitializeSystemData() {
        Data = Data ?? new SystemData(_transform);
        Data.Name = gameObject.name;
        Data.Capacity = 25;
        Data.DateHumanPlayerExplored = new GameDate(1, TempGameValues.StartingGameYear);
        Data.CombatStrength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f);
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = Players.Opponent_3;
        Data.Resources = new OpeYield(3.1F, 2.0F, 4.8F);
        Data.SpecialResources = new XYield(XResource.Special_1, 0.3F);
        Data.SettlementSize = SettlementSize.City;
    }

    private GuiTrackingLabel InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefab = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefab == null) {
            Debug.LogError("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return null;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefab);
        // NGUITools.AddChild handles all scale, rotation, posiition, parent and layer settings
        guiTrackingLabelCloneGO.name = gameObject.name + CommonTerms.Label;  // readable name of runtime instantiated label

        GuiTrackingLabel trackingLabel = guiTrackingLabelCloneGO.GetSafeMonoBehaviourComponent<GuiTrackingLabel>();
        // assign the system as the Target of the tracking label
        trackingLabel.Target = _transform;
        trackingLabel.TargetPivotOffset = new Vector3(Constants.ZeroF, _starManager.transform.collider.bounds.extents.y, Constants.ZeroF);
        trackingLabel.OffsetFromPivot = trackingLabelOffsetFromPivot;
        trackingLabel.Set(gameObject.name);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
        Debug.Log("A new {0} for {1} has been created.".Inject(typeof(GuiTrackingLabel), gameObject.name));
        return trackingLabel;
    }

    public void DisplayCursorHUD() {
        if (_factory == null) {
            _factory = new GuiCursorHudTextFactory_System(Data);
        }
        if (_guiCursorHudText != null && _guiCursorHudText.IntelLevel == HumanPlayerIntelLevel) {
            // TODO this only updates the age of the intel at this stage. Other values will also be dynamic and need updating
            UpdateGuiCursorHudText(GuiCursorHudDisplayLineKeys.IntelState);
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

    void Update() {
        if (ToUpdate()) {
            OptimizeDisplay();
        }
    }

    private void OptimizeDisplay() {
        bool toEnableHeirarchy = false;
        bool toShowTrackingLabel = false;
        switch (VisibilityState) {
            case Visibility.Visible:
                int distanceToCamera = _transform.DistanceToCameraInt();
                if (distanceToCamera < maxAnimateDistance) {
                    toEnableHeirarchy = true;
                }

                if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                    toShowTrackingLabel = true;
                }
                break;
            case Visibility.Invisible:
                // if invisible, neither the heirarchy or label should be enabled
                break;
            case Visibility.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(VisibilityState));
        }
        ShowTrackingLabel(toShowTrackingLabel);
        EnableHeirarchy(toEnableHeirarchy);
    }

    private void ShowTrackingLabel(bool toShow) {
        if (!toShow && _trackingLabel == null) {
            return;
        }
        TrackingLabel.IsShowing = toShow;
    }

    private void EnableHeirarchy(bool toEnable) {
        _starManager.EnableHeirarchy(toEnable);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IOnVisible Members

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

