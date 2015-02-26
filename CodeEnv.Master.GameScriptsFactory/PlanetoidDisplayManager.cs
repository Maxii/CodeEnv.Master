// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidDisplayManager.cs
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
public class PlanetoidDisplayManager : AMortalItemDisplayManager {


    public InteractableTrackingSprite Icon { get; private set; }

    private bool _isIconInCameraLOS = true;


    public PlanetoidDisplayManager(APlanetoidItem item) : base(item) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var meshRenderers = itemGo.GetComponentsInImmediateChildren<MeshRenderer>();
        var primaryMeshRenderer = meshRenderers.Single(mr => mr.gameObject.GetComponent<Revolver>() != null);
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.PlanetoidCull);   // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeSecondaryMeshes(GameObject itemGo) {
        base.InitializeSecondaryMeshes(itemGo);

        var atmoRenderer = itemGo.GetComponentsInImmediateChildren<MeshRenderer>().Except(_primaryMeshRenderer).Single();
        atmoRenderer.gameObject.layer = (int)Layers.PlanetoidCull;  // layer automatically handles showing
        atmoRenderer.castShadows = true;
        atmoRenderer.receiveShadows = true;
        atmoRenderer.enabled = true;

        var atmoAnimation = atmoRenderer.gameObject.GetComponent<Animation>();
        atmoAnimation.cullingType = AnimationCullingType.BasedOnRenderers;  // automatically handles showing
        atmoAnimation.enabled = true;
    }

    protected override void InitializeOther(GameObject itemGo) {
        base.InitializeOther(itemGo);

        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        InitializeIcon();
    }

    private void InitializeIcon() {
        Icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(Item, TrackingWidgetFactory.IconAtlasID.Contextual,
            new Vector2(12, 12), WidgetPlacement.Over);
        Icon.Set("Icon02");

        var iconCameraLosChgdListener = Icon.CameraLosChangedListener;
        iconCameraLosChgdListener.onCameraLosChanged += (iconGo, isIconInCameraLOS) => OnIconInCameraLOSChanged(isIconInCameraLOS);
        iconCameraLosChgdListener.enabled = true;
        //D.Log("{0} initialized its Icon.", FullName);
        // icon enabled state controlled by _icon.Show()
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
    //            D.Assert(!Icon.IsShowing);
    //            if (UnityUtility.IsWithinCameraViewport(Item.Position)) {
    //                // mesh moved beyond culling distance while on the screen
    //                // icon only shows when in front of the camera and beyond the star mesh's culling distance
    //                ShowIcon(true);
    //            }
    //            else {
    //                // mesh moved off the screen while within culling distance
    //            }
    //        }
    //        else {
    //            // mesh moved within culling distance while on the screen or onto the screen while within culling distance
    //            ShowIcon(false);
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
        //        if (!_isMeshShowing) {
        //            if (!Icon.IsShowing) {
        //                ShowIcon(true);
        //            }
        //        }
        //    }
        //    else {
        //        // icon moved off the screen
        //        if (Icon.IsShowing) {
        //            ShowIcon(false);
        //        }
        //    }
        //}
    }

    protected override void AssessInCameraLOS() {
        // one or the other inCameraLOS needs to be true for this to be set to true, both inCameraLOS needs to be false for this to trigger false
        InCameraLOS = _isMeshInCameraLOS || _isIconInCameraLOS;
    }

    protected override void AssessShowing() {
        base.AssessShowing();
        bool toShowIcon = IsDisplayEnabled && _isIconInCameraLOS && _isMeshShowing;
        ShowIcon(toShowIcon);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

