﻿// --------------------------------------------------------------------------------------------------------------------
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

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a system's star.
/// </summary>
public class StarView : AFocusableItemView {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.Ships, Layers.BasesSettlements, Layers.Planetoids, Layers.Stars);

    public new StarPresenter Presenter {
        get { return base.Presenter as StarPresenter; }
        protected set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;
    private Light _starLight;
    private Billboard _billboard;

    protected override void Awake() {
        base.Awake();
        (_collider as SphereCollider).radius = TempGameValues.StarRadius;
        circleScaleFactor = 1.0F;
        _keepoutCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(c => c.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
        _starLight = gameObject.GetComponentInChildren<Light>();
        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        Subscribe();    // no real need to subscribe at all if only subscription is PlayerIntelCoverage changes which these don't have
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
        InitializeStarSettings();
    }

    #region Mouse Events

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        if (IsDiscernible) {
            Presenter.OnHover(isOver);
        }
    }

    protected override void OnLeftClick() {
        base.OnLeftClick();
        Presenter.OnLeftClick();
    }

    #endregion

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _billboard.enabled = IsDiscernible;
    }

    private void InitializeStarSettings() {
        _starLight.range = GameManager.Settings.UniverseSize.Radius();
        _starLight.intensity = 0.5F;
        _starLight.cullingMask = _starLightCullingMask;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

