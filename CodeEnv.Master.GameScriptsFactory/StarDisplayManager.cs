// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayManager.cs
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
public class StarDisplayManager : ADiscernibleItemDisplayManager {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.ShipCull, Layers.FacilityCull, Layers.PlanetoidCull, Layers.StarCull);


    public InteractableTrackingSprite Icon { get; private set; }

    /// <summary>
    /// Circle scale factor specific to the derived type of the Item.
    /// e.g. ShipItem, CommandItem, StarItem, etc.
    /// </summary>
    protected override float ItemTypeCircleScale { get { return 1.5F; } }


    //private IEnumerable<MeshRenderer> _glowRenderers;
    private Billboard _glowBillboard;

    private bool _isIconInCameraLOS = true;



    public StarDisplayManager(StarItem item) : base(item) { }

    protected override MeshRenderer InitializePrimaryMesh(GameObject itemGo) {
        var primaryMeshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();
        primaryMeshRenderer.castShadows = true;
        primaryMeshRenderer.receiveShadows = true;
        D.Assert((Layers)(primaryMeshRenderer.gameObject.layer) == Layers.StarCull);    // layer automatically handles showing
        return primaryMeshRenderer;
    }

    protected override void InitializeSecondaryMeshes(GameObject itemGo) {
        base.InitializeSecondaryMeshes(itemGo);

        var glowRenderers = itemGo.GetComponentsInChildren<MeshRenderer>().Except(_primaryMeshRenderer);
        glowRenderers.ForAll(gr => {
            gr.castShadows = false;
            gr.receiveShadows = false;
            D.Assert((Layers)(gr.gameObject.layer) == Layers.StarCull); // layer automatically handles showing
            gr.enabled = true;
        });
    }

    protected override void InitializeOther(GameObject itemGo) {
        base.InitializeOther(itemGo);
        _glowBillboard = itemGo.GetSafeMonoBehaviourComponentInChildren<Billboard>();

        var starLight = _glowBillboard.gameObject.GetComponentInChildren<Light>();
        starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        InitializeIcon();
    }

    //protected override void Initialize() {
    //    base.Initialize();
    //    var itemGo = _item.gameObject;
    //    var meshRenderer = itemGo.GetComponentInImmediateChildren<MeshRenderer>();

    //    _glowBillboard = itemGo.GetSafeMonoBehaviourComponentInChildren<Billboard>();

    //    _glowRenderers = _glowBillboard.gameObject.GetComponentsInChildren<MeshRenderer>();
    //    _glowRenderers.ForAll(gr => {
    //        gr.castShadows = false;
    //        gr.receiveShadows = false;
    //    });

    //    var starLight = _glowBillboard.gameObject.GetComponentInChildren<Light>();
    //    starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
    //    starLight.intensity = 0.5F;
    //    starLight.cullingMask = _starLightCullingMask;
    //    starLight.enabled = true;

    //    // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

    //    InitializeIcon();
    //}

    private void InitializeIcon() {
        Icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(Item, TrackingWidgetFactory.IconAtlasID.Contextual,
            new Vector2(16, 16), WidgetPlacement.Over);
        Icon.Set("Icon01");

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

    protected override void ShowPrimaryMesh() {
        base.ShowPrimaryMesh();
        _glowBillboard.enabled = true;
    }

    protected override void HidePrimaryMesh() {
        base.HidePrimaryMesh();
        _glowBillboard.enabled = false;
    }

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

    protected override void AssessShowing() {
        base.AssessShowing();
        bool toShowIcon = IsDisplayEnabled && _isIconInCameraLOS && !_isMeshShowing;
        ShowIcon(toShowIcon);
    }

    protected override void AssessInCameraLOS() {
        // one or the other inCameraLOS needs to be true for this to be set to true, both inCameraLOS needs to be false for this to trigger false
        InCameraLOS = _isMeshInCameraLOS || _isIconInCameraLOS;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

