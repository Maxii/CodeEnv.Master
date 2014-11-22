// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarView.cs
// A class for managing the UI of a system's star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a system's star.
/// </summary>
public class StarView : AFocusableItemView {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.Ship, Layers.Facility, Layers.Planetoid, Layers.Star);

    public new StarPresenter Presenter {
        get { return base.Presenter as StarPresenter; }
        protected set { base.Presenter = value; }
    }

    public float minCameraViewDistanceMultiplier = 2F;
    public float optimalCameraViewDistanceMultiplier = 8F;

    private Billboard _billboard;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 1.0F;
        Subscribe();    // no real need to subscribe at all if only subscription is PlayerIntelCoverage changes which these don't have
    }

    protected override void InitializeVisualMembers() {
        var meshRenderer = gameObject.GetComponentInImmediateChildren<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var glowRenderers = gameObject.GetComponentsInChildren<MeshRenderer>().Except(meshRenderer);
        glowRenderers.ForAll(gr => {
            gr.castShadows = false;
            gr.receiveShadows = false;
            gr.enabled = true;
        });

        var starLight = gameObject.GetComponentInChildren<Light>();
        starLight.range = GameManager.GameSettings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        _billboard.enabled = true;

        var animation = gameObject.GetComponentInChildren<Animation>();
        animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        animation.enabled = true;
        // TODO animation settings and distance controls

        var revolvers = gameObject.GetSafeMonoBehaviourComponentsInChildren<Revolver>();
        revolvers.ForAll(r => r.enabled = true);
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var cameraLosChgdListener = gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    protected override IIntel InitializePlayerIntel() {
        return new FixedIntel(IntelCoverage.Comprehensive);
    }

    protected override void InitializePresenter() {
        Presenter = new StarPresenter(this);
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        // no reason to subscribe as Coverage does not change
    }

    protected override void Start() {
        base.Start();
        AssessDiscernability(); // needed as FixedIntel gets set early and never changes
    }

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        Presenter.OnLeftClick();
    }

    #endregion

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _billboard.enabled = IsDiscernible;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optimalCameraViewDistanceMultiplier; } }

    #endregion

}

