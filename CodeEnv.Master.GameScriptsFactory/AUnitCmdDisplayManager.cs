// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdDisplayManager.cs
// COMMENT - one line to give a brief idea of what the file does.
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
/// 
/// </summary>
public abstract class AUnitCmdDisplayManager : AMortalItemDisplayManager {


    public InteractableTrackingSprite Icon { get; private set; }

    public new AUnitCmdItem Item { get { return base.Item as AUnitCmdItem; } }

    //protected override float SphericalHighlightRadius { get { return Item.UnitRadius; } }

    protected override float ItemTypeCircleScale { get { return 0.03F; } }

    protected override float RadiusOfHighlightCircle {
        // this override eliminates the Radius of the Item in the calculation as Cmd radius changes with HQ Element
        get { return Screen.height * ItemTypeCircleScale; }
    }


    private bool _isIconInCameraLOS = true;
    private float _initialRadiusOfPrimaryMesh;

    public AUnitCmdDisplayManager(AUnitCmdItem item)
        : base(item) {
        _isCirclesRadiusDynamic = false;
    }


    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        _initialRadiusOfPrimaryMesh = primaryMeshRenderer.bounds.size.x / 2F;
        primaryMeshRenderer.castShadows = false;
        primaryMeshRenderer.receiveShadows = false;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == GetCullingLayer());    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeOther(GameObject itemGo) {
        base.InitializeOther(itemGo);

        InitializeIcon();
    }


    private void InitializeIcon() {
        D.Assert(PlayerPrefsManager.Instance.IsElementIconsEnabled);
        Icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(Item, TrackingWidgetFactory.IconAtlasID.Fleet,
            new Vector2(24, 24), WidgetPlacement.Above);
        // icon enabled state controlled by _icon.Show()
        var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        iconCameraLosChgdListener.enabled = true;
    }


    protected abstract Layers GetCullingLayer();

    public void ResizePrimaryMesh() {
        float scale = Item.Radius / _initialRadiusOfPrimaryMesh;
        _primaryMeshRenderer.transform.localScale = new Vector3(scale, scale, scale);
    }


    private void ShowIcon(bool toShow) {
        //D.Log("{0}.ShowIcon({1}) called.", GetType().Name, toShow);
        if (Icon.IsShowing == toShow) {
            D.Warn("{0} recording duplicate call to ShowIcon({1}).", GetType().Name, toShow);
            return;
        }
        Icon.Show(toShow);
    }

    //protected override void OnIsDisplayEnabledChanged() {
    //    base.OnIsDisplayEnabledChanged();
    //    if (!IsDisplayEnabled) {
    //        if (Icon != null && Icon.IsShowing) {
    //            ShowIcon(false);
    //        }
    //    }
    //    else {
    //        // Nothing to do when enabled. Icon will show when determined by AssessInCameraLosChanged()
    //    }
    //}


    //protected override void OnMeshInCameraLOSChanged(bool isMeshInCameraLOS) {
    //    base.OnMeshInCameraLOSChanged(isMeshInCameraLOS);

    //    if (IsDisplayEnabled) {
    //        if (!isMeshInCameraLOS) {
    //            // mesh moved beyond culling distance while on the screen or off the screen while within culling distance

    //            // Nothing to do for mesh as it is handled by base class (and it auto shows/hides)
    //            // Nothing to do for icon as it is managed by OnIconInCameraLOSChanged() below
    //        }
    //        else {
    //            // mesh moved within culling distance while on the screen or onto the screen while within culling distance

    //            // Nothing to do for mesh as it is handled by base class (and it auto shows/hides)
    //            // Nothing to do for icon as it is managed by OnIconInCameraLOSChanged() below
    //        }
    //    }
    //}

    private void OnIconInCameraLOSChanged(bool isIconInCameraLOS) {
        D.Log("{0}.OnIconInCameraLOSChanged({1}) called.", GetType().Name, isIconInCameraLOS);
        _isIconInCameraLOS = isIconInCameraLOS;
        AssessInCameraLOS();
        AssessShowing();

        //if (IsDisplayEnabled) {
        //    if (isIconInCameraLOS) {
        //        // icon moved onto the screen
        //        //if (!_isMeshShowing) {    // cmdIcon showing or not has nothing to do with the mesh
        //        if (!Icon.IsShowing) {
        //            ShowIcon(true);
        //        }
        //        //}
        //    }
        //    else {
        //        // icon moved off the screen
        //        if (Icon.IsShowing) {
        //            ShowIcon(false);
        //        }
        //    }
        //}
    }

    protected override void AssessShowing() {
        base.AssessShowing();
        bool toShowIcon = IsDisplayEnabled && _isIconInCameraLOS;   // mesh showing isn't relevant
        ShowIcon(toShowIcon);
    }

    protected override void AssessInCameraLOS() {
        // one or the other inCameraLOS needs to be true for this to be set to true, both inCameraLOS needs to be false for this to trigger false
        InCameraLOS = _isMeshInCameraLOS || _isIconInCameraLOS;
    }


    public override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    protected override void ShowCircle(bool toShow, Highlights highlight) {
        ShowCircle(toShow, highlight, Icon.WidgetTransform);
    }



}

