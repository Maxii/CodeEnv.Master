// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayMgr.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class StarDisplayMgr : AMonoBase {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.ShipCull, Layers.FacilityCull, Layers.PlanetoidCull, Layers.StarCull);

    private bool _inCameraLOS = true;
    public bool InCameraLOS {
        get { return _inCameraLOS; }
        set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS"); }
        //set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnInCameraLosChanged); }
    }


    //public event Action<bool> onInCameraLosChanged;

    public UIEventListener IconEventListener { get { return _icon.EventListener; } }

    public GameColor IconColor { set { _icon.Color = value; } }

    //public IWidgetTrackable TrackedItem { private get; set; }

    private InteractableTrackingSprite _icon;
    private Animation _animation;
    private IEnumerable<MeshRenderer> _glowRenderers;
    private Billboard _glowBillboard;

    private bool _isMeshShowing;

    private bool _isMeshInCameraLOS = true;
    private bool _isIconInCameraLOS = true;
    //private bool _allowShowing;
    //public bool AllowShowing {
    //    get { return _allowShowing; }
    //    set { SetProperty<bool>(ref _allowShowing, value, "AllowShowing", OnAllowShowingChanged); }
    //}

    //protected override void Awake() {
    //    base.Awake();
    //    enabled = false;
    //}

    //protected override void Start() {
    //    base.Start();
    //    Initialize();
    //}

    public void Initialize(IWidgetTrackable trackedItem) {
        var meshRenderer = gameObject.GetComponentInImmediateChildren<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        _glowRenderers = gameObject.GetComponentsInChildren<MeshRenderer>().Except(meshRenderer);
        _glowRenderers.ForAll(gr => {
            gr.castShadows = false;
            gr.receiveShadows = false;
            //gr.enabled = true;
        });

        var starLight = gameObject.GetComponentInChildren<Light>();
        starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        _glowBillboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        //_billboard.enabled = true;

        _animation = gameObject.GetComponentInChildren<Animation>();
        _animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        //_animation.enabled = true;
        // TODO animation settings and distance controls

        // revolvers = gameObject.GetSafeMonoBehaviourComponentsInChildren<Revolver>();
        // revolvers.ForAll(r => r.axisOfRotation = new Vector3(Constants.Zero, Constants.One, Constants.Zero));
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var meshCameraLosChgdListener = meshRenderer.gameObject.GetSafeMonoBehaviourComponent<CameraLosChangedListener>();
        meshCameraLosChgdListener.onCameraLosChanged += (go, isMeshInCameraLOS) => OnMeshInCameraLOSChanged(isMeshInCameraLOS);
        meshCameraLosChgdListener.enabled = true;

        ShowMesh(true);

        InitializeIcon(trackedItem);
    }

    private void InitializeIcon(IWidgetTrackable trackedItem) {
        //D.Assert(TrackedItem != null);
        _icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(trackedItem, TrackingWidgetFactory.IconAtlasID.Contextual,
            new Vector2(16, 16), WidgetPlacement.Over);
        _icon.Set("Icon01");
        //ChangeIconColor(starItem.Owner.Color);

        var iconCameraLosChgdListener = _icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        iconCameraLosChgdListener.enabled = true;

        UnityUtility.AttachChildToParent(_icon.gameObject, gameObject);
        //D.Log("{0} initialized its Icon.", FullName);
        // icon enabled state controlled by _icon.Show()
    }
    //private void InitializeIcon() {
    //    StarItem starItem = gameObject.GetSafeMonoBehaviourComponentInParents<StarItem>();
    //    //float minShowDistance = Camera.main.layerCullDistances[(int)Layers.StarCull];
    //    _icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(starItem, TrackingWidgetFactory.IconAtlasID.Contextual,
    //        new Vector2(16, 16), WidgetPlacement.Over);
    //    _icon.Set("Icon01");
    //    ChangeIconColor(starItem.Owner.Color);

    //    //var cmdIconEventListener = _icon.EventListener;
    //    //cmdIconEventListener.onHover += (cmdIconGo, isOver) => OnHover(isOver);
    //    //cmdIconEventListener.onClick += (cmdIconGo) => OnClick();
    //    //cmdIconEventListener.onDoubleClick += (cmdIconGo) => OnDoubleClick();
    //    //cmdIconEventListener.onPress += (cmdIconGo, isDown) => OnPress(isDown);

    //    var iconCameraLosChgdListener = _icon.CameraLosChangedListener;
    //    iconCameraLosChgdListener.onCameraLosChanged += (cmdIconGo, inCameraLOS) => OnIconInCameraLOSChanged(inCameraLOS);
    //    iconCameraLosChgdListener.enabled = true;

    //    UnityUtility.AttachChildToParent(_icon.gameObject, gameObject);
    //    //D.Log("{0} initialized its Icon.", FullName);
    //    // icon enabled state controlled by _icon.Show()
    //}

    private void ShowIcon(bool toShow) {
        D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
        if (_icon.IsShowing == toShow) {
            D.Warn("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
        }
        _icon.Show(toShow);
    }
    //private void ShowIcon(bool toShow) {
    //    if (_icon != null) {
    //        D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
    //        if (_icon.IsShowing == toShow) {
    //            D.Warn("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
    //        }
    //        _icon.Show(toShow);
    //    }
    //}

    //public void ChangeIconColor(GameColor color) {
    //    if (_icon != null) {
    //        _icon.Color = color;
    //    }
    //}
    protected override void OnDisable() {
        base.OnDisable();
        if (_icon != null && _icon.IsShowing) {
            ShowIcon(false);
        }
        if (_isMeshShowing) {
            ShowMesh(false);
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        // do nothing. Icon will show when determined by AssessInCameraLosChanged()
    }

    //private void OnAllowShowingChanged() {
    //    // can't turn meshRenderer off as lose OnMeshInCameraLOSChanged events
    //    if (!AllowShowing) {
    //        if (_icon.IsShowing) {
    //            ShowIcon(false);
    //        }
    //        ShowMesh(false);
    //    }
    //}


    private void ShowMesh(bool toShow) {
        // can't turn meshRenderer off as lose OnMeshInCameraLOSChanged events
        D.Log("{0}.ShowMesh({1}) called.", GetType().Name, toShow);
        if (_isMeshShowing == toShow) {
            D.Warn("{0} recording duplicate call to ShowMesh({1}).", GetType().Name, toShow);
        }
        _glowBillboard.enabled = toShow;
        _animation.enabled = toShow;
        _glowRenderers.ForAll(gr => gr.enabled = toShow);
        _isMeshShowing = toShow;
    }



    private void OnMeshInCameraLOSChanged(bool isMeshInCameraLOS) {
        D.Log("{0}.OnMeshInCameraLOSChanged({1}) called. IconShowing = {2}.", GetType().Name, isMeshInCameraLOS, _icon.IsShowing);
        _isMeshInCameraLOS = isMeshInCameraLOS;
        AssessInCameraLosChanged();

        if (enabled) {
            if (!isMeshInCameraLOS) {
                // mesh moved beyond culling distance while on the screen or off the screen while within culling distance
                D.Assert(!_icon.IsShowing);
                if (UnityUtility.IsWithinCameraViewport(_transform.position)) {
                    // mesh moved beyond culling distance while on the screen
                    // icon only shows when in front of the camera and beyond the star mesh's culling distance
                    ShowIcon(true);
                }
                else {
                    // mesh moved off the screen while within culling distance
                }
                ShowMesh(false);
            }
            else {
                // mesh moved within culling distance while on the screen or onto the screen while within culling distance
                ShowMesh(true);
                ShowIcon(false);
            }
        }
    }
    //private void OnMeshInCameraLOSChanged(bool meshInCameraLOS) {
    //    D.Log("{0}.OnMeshInCameraLOSChanged({1}) called. IconShowing = {2}.", GetType().Name, meshInCameraLOS, _icon.IsShowing);
    //    _isMeshInCameraLOS = meshInCameraLOS;
    //    AssessInCameraLosChanged();

    //    if (AllowShowing) {
    //        if (!meshInCameraLOS) {
    //            // mesh moved beyond culling distance while on the screen or off the screen while within culling distance
    //            D.Assert(!_icon.IsShowing);
    //            if (UnityUtility.IsWithinCameraViewport(_transform.position)) {
    //                // mesh moved beyond culling distance while on the screen
    //                // icon only shows when in front of the camera and beyond the star mesh's culling distance
    //                ShowIcon(true);
    //            }
    //            else {
    //                // mesh moved off the screen while within culling distance
    //            }
    //            ShowMesh(false);
    //        }
    //        else {
    //            // mesh moved within culling distance while on the screen or onto the screen while within culling distance
    //            ShowMesh(true);
    //            ShowIcon(false);
    //        }
    //    }
    //}


    private void OnIconInCameraLOSChanged(bool isIconInCameraLOS) {
        D.Log("{0}.OnIconInCameraLOSChanged({1}) called.", GetType().Name, isIconInCameraLOS);
        _isIconInCameraLOS = isIconInCameraLOS;
        AssessInCameraLosChanged();

        if (enabled) {
            if (isIconInCameraLOS) {
                // icon moved onto the screen
                if (!_isMeshShowing) {
                    if (!_icon.IsShowing) {
                        ShowIcon(true);
                    }
                }
            }
            else {
                // icon moved off the screen
                if (_icon.IsShowing) {
                    ShowIcon(false);
                }
            }
        }
    }
    //private void OnIconInCameraLOSChanged(bool iconInCameraLOS) {
    //    D.Log("{0}.OnIconInCameraLOSChanged({1}) called.", GetType().Name, iconInCameraLOS);
    //    _isIconInCameraLOS = iconInCameraLOS;
    //    AssessInCameraLosChanged();

    //    if (AllowShowing) {
    //        if (iconInCameraLOS) {
    //            // icon moved onto the screen
    //            if (!_isMeshShowing) {
    //                if (!_icon.IsShowing) {
    //                    ShowIcon(true);
    //                }
    //            }
    //        }
    //        else {
    //            // icon moved off the screen
    //            if (_icon.IsShowing) {
    //                ShowIcon(false);
    //            }
    //        }
    //    }
    //}

    private void AssessInCameraLosChanged() {
        // one or the other inCameraLOS needs to be true for this to trigger true, both inCameraLOS needs to be false for this to trigger false
        InCameraLOS = _isMeshInCameraLOS || _isIconInCameraLOS;
    }

    //private void OnInCameraLosChanged() {
    //    if (onInCameraLosChanged != null) {
    //        onInCameraLosChanged(InCameraLOS);
    //    }
    //}

    protected override void Cleanup() {
        // no unsubscribing needed as all subscriptions are to children
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

