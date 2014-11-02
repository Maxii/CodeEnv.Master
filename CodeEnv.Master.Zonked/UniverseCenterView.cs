// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterView.cs
// A class for managing the UI of the object at the center of the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///A class for managing the UI of the object at the center of the universe.
/// </summary>
public class UniverseCenterView : AFocusableItemView {

    public float minCameraViewDistanceMultiplier = 2F;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 5F;
        Subscribe();    // no real need to subscribe at all if only subscription is PlayerIntelCoverage changes which these don't have
    }

    protected override void Start() {
        base.Start();
        AssessDiscernability(); // needed as FixedIntel gets set early and never changes
    }

    protected override void InitializeVisualMembers() {
        var meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var animation = meshRenderer.gameObject.GetComponent<Animation>();
        animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        animation.enabled = true;

        var cameraLosChgdListener = gameObject.GetSafeInterfaceInChildren<ICameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    protected override IIntel InitializePlayerIntel() {
        return new FixedIntel(IntelCoverage.Comprehensive);
    }

    protected override void InitializePresenter() {
        Presenter = new UniverseCenterPresenter(this);
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        // no reason to subscribe as Coverage does not change
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    public override float OptimalCameraViewingDistance { get { return gameObject.DistanceToCamera(); } }

    #endregion

}

