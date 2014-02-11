﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidView.cs
// A class for managing the UI of a planetoid.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a planetoid.
/// </summary>
public class PlanetoidView : AMortalItemView, IPlanetoidViewable, ICameraFollowable {

    public new PlanetoidPresenter Presenter {
        get { return base.Presenter as PlanetoidPresenter; }
        protected set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;

    protected override void Awake() {
        base.Awake();
        _keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
        Subscribe();
    }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void InitializePresenter() {
        Presenter = new PlanetoidPresenter(this);
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IPlanetoidViewable Members

    public void ShowHit() {
        // TODO
        OnShowCompletion();
    }

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

