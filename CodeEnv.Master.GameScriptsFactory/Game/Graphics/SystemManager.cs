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
public class SystemManager : AMonoBehaviourBase, IOnVisible {

    public SystemData Data { get; private set; }
    public IGuiTrackingLabel GuiTrackingLabel { get; private set; }
    public IntelLevel HumanPlayerIntelLevel { get; set; }

    private GuiCursorHudText _guiCursorHudText;
    private GuiCursorHudTextFactory_System _factory;

    private Transform _transform;
    private StarManager _starManager;
    private GuiCursorHUD _cursorHud;

    void Awake() {
        _transform = transform;
        _starManager = gameObject.GetSafeMonoBehaviourComponentInChildren<StarManager>();
        _cursorHud = GuiCursorHUD.Instance;
        VisibilityState = Visibility.Visible;
        UpdateRate = UpdateFrequency.Seldom;
        InitializeGuiTrackingLabel();
    }

    private void InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefab = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefab == null) {
            Debug.LogError("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefab);
        // NGUITools.AddChild handles all scale, rotation, posiition, parent and layer settings
        string systemName = _transform.name;
        guiTrackingLabelCloneGO.name = systemName + CommonTerms.Label;  // readable name of runtime instantiated label

        GuiTrackingLabel = guiTrackingLabelCloneGO.GetInterface<IGuiTrackingLabel>();
        // assign the system as the Target of the guiHUD
        GuiTrackingLabel.Target = _transform;
        GuiTrackingLabel.Set(systemName);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
    }

    void Start() {
        HumanPlayerIntelLevel = IntelLevel.OutOfRange;
        __CreateSystemData();
    }

    private void __CreateSystemData() {
        Data = new SystemData();
        Data.Name = gameObject.name;
        Data.Capacity = 25;
        Data.DateHumanPlayerExplored = new GameDate(1, TempGameValues.StartingGameYear);
        Data.CombatStrength = 5000;
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = Players.Opponent_3;
        Data.Position = _transform.position;
        Data.Resources = new OpeYield(3.1F, 2.0F, 4.8F);
        Data.SpecialResources = new XYield(XResource.Special_1, 0.3F);
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
        bool toEnableGuiTrackingLabel = false;
        int distanceToCamera = _transform.DistanceToCameraInt();
        switch (VisibilityState) {
            case Visibility.Visible:
                if (distanceToCamera < TempGameValues.SystemAnimateDisplayThreshold) {
                    toEnableHeirarchy = true;
                }
                if (distanceToCamera < TempGameValues.SystemLabelDisplayThreshold) {
                    toEnableGuiTrackingLabel = true;
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
        GuiTrackingLabel.IsEnabled = toEnableGuiTrackingLabel;
    }

    private void EnableHeirarchy(bool toEnable) {
        _starManager.EnableHeirarchy(toEnable);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

