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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a System. 
/// </summary>
public class SystemManager : AGameObjectManager<SystemData> {

    private static string __highlightName = "SystemHighlightMesh";  // IMPROVE
    private static Color __focusedColor = Color.blue;
    private static Color __selectedColor = Color.yellow;

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
    public int maxAnimateDistance;  // must be initialized in Awake or Start as it eventually calls UnityEngine.Application.dataPath
    public int minTrackingLabelShowDistance = TempGameValues.MinSystemTrackingLabelShowDistance;
    public int maxTrackingLabelShowDistance = TempGameValues.MaxSystemTrackingLabelShowDistance;

    private IList<Behaviour> _behavioursToOptimize;
    private IList<Renderer> _renderersToOptimize;
    private IList<Collider> _collidersToOptimize;
    private IList<GameObject> _gameObjectsToOptimize;

    private Star _starManager;
    private MeshRenderer _systemHighlightRenderer;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _starManager = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>();
        maxAnimateDistance = AnimationSettings.Instance.MaxSystemAnimateDistance;
        _systemHighlightRenderer = __FindSystemHighlight();
        UpdateRate = FrameUpdateFrequency.Seldom;
        __InitializeData();
    }

    private MeshRenderer __FindSystemHighlight() {
        MeshRenderer[] meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer renderer = meshes.Single<MeshRenderer>(m => m.gameObject.name == __highlightName);
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        RegisterOptimizableComponents();
    }

    private void RegisterOptimizableComponents() {
        _behavioursToOptimize = gameObject.GetComponentsInChildren<Animation>().ToList<Behaviour>();
        _behavioursToOptimize.Union<Behaviour>(gameObject.GetSafeMonoBehaviourComponentsInChildren<Orbit>());
        _renderersToOptimize = gameObject.GetComponentsInChildren<Renderer>()
            .Where<Renderer>(r => r.gameObject.GetSafeMonoBehaviourComponent<VisibilityChangedRelay>() == null).ToList<Renderer>();
        _collidersToOptimize = new List<Collider>() {
            gameObject.GetSafeMonoBehaviourComponentInChildren<OrbitalPlane>().collider
        };
        _gameObjectsToOptimize = gameObject.GetSafeMonoBehaviourComponentsInChildren<Billboard>().Select(bm => bm.gameObject).ToList<GameObject>();
        // the only way to disable a Collider is to destroy it. They cannot be disabled/enabled as the init function is too costly
    }

    protected override IntelLevel __InitializeIntelLevel() {
        return IntelLevel.OutOfDate;
    }

    protected override void __InitializeData() {
        Data = new SystemData(_transform);
        Data.Name = gameObject.GetSafeMonoBehaviourComponentInChildren<Star>().gameObject.name;
        Data.OptionalParentName = gameObject.name;
        Data.LastHumanPlayerIntelDate = new GameDate();
        Data.Capacity = 25;
        Data.Resources = new OpeYield(3.1F, 2.0F, 4.8F);
        Data.SpecialResources = new XYield(XResource.Special_1, 0.3F);
        Data.Settlement = new SettlementData() {
            SettlementSize = SettlementSize.City,
            Population = 100,
            CapacityUsed = 10,
            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F)),
            Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
            Health = 38F,
            MaxHitPoints = 50F,
            Owner = GameManager.Instance.HumanPlayer
        };
    }

    private GuiTrackingLabel InitializeGuiTrackingLabel() {
        GameObject guiTrackingLabelPrefab = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefab == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
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
        Logger.Log("A new {0} for {1} has been created.".Inject(typeof(GuiTrackingLabel), gameObject.name));
        return trackingLabel;
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

    public enum SystemHighlights { None, Focus, Select }

    public void HighlightSystem(bool toShow, SystemHighlights highlight = SystemHighlights.None) {
        switch (highlight) {
            case SystemHighlights.Focus:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, __focusedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, __focusedColor);
                break;
            case SystemHighlights.Select:
                _systemHighlightRenderer.material.SetColor(UnityConstants.MainMaterialColor, __selectedColor);
                _systemHighlightRenderer.material.SetColor(UnityConstants.OutlineMaterialColor, __selectedColor);
                break;
            case SystemHighlights.None:
                D.Assert(!toShow, "{0} can only be used when turning off the highlight.".Inject(highlight.GetName()));
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
        _systemHighlightRenderer.gameObject.SetActive(toShow);
    }

    protected override void OptimizeDisplay() {
        bool toEnableHeirarchy = false; // IMPROVE this. Do all heirarchy items get turned off when visible but out of range?
        bool toShowTrackingLabel = false;
        bool toEnableCollider = false;
        if (IsVisible) {
            toEnableCollider = true;
            int distanceToCamera = _transform.DistanceToCameraInt();
            if (distanceToCamera < maxAnimateDistance) {
                toEnableHeirarchy = true;
            }

            if (Utility.IsInRange(distanceToCamera, minTrackingLabelShowDistance, maxTrackingLabelShowDistance)) {
                toShowTrackingLabel = true;
            }
        }
        ShowTrackingLabel(toShowTrackingLabel);
        EnableHeirarchy(toEnableHeirarchy);
        _collidersToOptimize.ForAll<Collider>(c => c.enabled = toEnableCollider);
    }

    private void ShowTrackingLabel(bool toShow) {
        if (!toShow && _trackingLabel == null) {
            return;
        }
        TrackingLabel.IsShowing = toShow;
    }

    private bool _isHeirarchyEnabled = true;    // all components start enabled
    private void EnableHeirarchy(bool toEnable) {
        if (_isHeirarchyEnabled == toEnable) { return; }
        _gameObjectsToOptimize.ForAll<GameObject>(go => go.SetActive(toEnable));
        _behavioursToOptimize.ForAll<Behaviour>(b => b.enabled = toEnable);
        _renderersToOptimize.ForAll<Renderer>(r => r.enabled = toEnable);
        _isHeirarchyEnabled = toEnable;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

