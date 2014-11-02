// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidView.cs
// An MVPresenter abstract base class for Planet and Moon Views.
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
/// An MVPresenter abstract base class for Planet and Moon Views.
/// </summary>
public abstract class APlanetoidView : AMortalItemView, ICameraFollowable {

    public float minCameraViewDistanceMultiplier = 2F;
    public float optimalCameraViewDistanceMultiplier = 8F;

    protected override void InitializeVisualMembers() {
        // Once the player initially discerns the planet, he will always be able to discern it
        var meshRenderers = gameObject.GetComponentsInImmediateChildren<MeshRenderer>();
        meshRenderers.ForAll(mr => {
            mr.castShadows = true;
            mr.receiveShadows = true;
            mr.enabled = true;
        });

        var animations = gameObject.GetComponentsInImmediateChildren<Animation>();
        animations.ForAll(a => {
            a.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
            a.enabled = true;
        });
        // TODO animation settings and distance controls

        var revolver = gameObject.GetSafeInterfaceInChildren<IRevolver>();
        revolver.enabled = true;
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var cameraLosChgdListener = gameObject.GetSafeInterfaceInImmediateChildren<ICameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    protected override IIntel InitializePlayerIntel() { return new ImprovingIntel(); }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optimalCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

