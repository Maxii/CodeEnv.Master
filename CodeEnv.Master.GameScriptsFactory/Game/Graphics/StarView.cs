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
public class StarView : AFocusableView {

    public new StarPresenter Presenter {
        get { return base.Presenter as StarPresenter; }
        protected set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;
    private Light _starLight;
    private IEnumerable<StarGlowAnimator> _glowAnimators;
    private StarAnimator _starAnimator;
    private Animation _animation;
    private Billboard _billboard;

    protected override void Awake() {
        base.Awake();
        (_collider as SphereCollider).radius = TempGameValues.StarRadius;
        circleScaleFactor = 1.0F;
        _keepoutCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(c => c.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
        _starLight = gameObject.GetComponentInChildren<Light>();
        _glowAnimators = gameObject.GetSafeMonoBehaviourComponentsInChildren<StarGlowAnimator>();
        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        _animation = gameObject.GetComponentInChildren<Animation>();
        _starAnimator = gameObject.GetSafeMonoBehaviourComponent<StarAnimator>();
    }

    protected override void InitializePresenter() {
        Presenter = new StarPresenter(this);
    }

    protected override void Start() {
        base.Start();
        InitializeStarSettings();
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        if (DisplayMode != ViewDisplayMode.Hide) {
            Presenter.OnHover(isOver);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    private void OnLeftClick() {
        if (DisplayMode != ViewDisplayMode.Hide) {
            Presenter.OnLeftClick();
        }
    }

    protected override void OnDisplayModeChanging(ViewDisplayMode newMode) {
        base.OnDisplayModeChanging(newMode);
        ViewDisplayMode previousMode = DisplayMode;
        switch (previousMode) {
            case ViewDisplayMode.Hide:
                _billboard.enabled = true;
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(false);
                break;
            case ViewDisplayMode.ThreeD:
                if (newMode != ViewDisplayMode.ThreeDAnimation) { Show3DMesh(false); }
                break;
            case ViewDisplayMode.ThreeDAnimation:
                if (newMode != ViewDisplayMode.ThreeD) { Show3DMesh(false); }
                _glowAnimators.ForAll(ga => ga.gameObject.SetActive(false));
                _starAnimator.enabled = false;
                _animation.enabled = false;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousMode));
        }
    }

    protected override void OnDisplayModeChanged() {
        base.OnDisplayModeChanged();
        switch (DisplayMode) {
            case ViewDisplayMode.Hide:
                _billboard.enabled = false;
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(true);
                break;
            case ViewDisplayMode.ThreeD:
                Show3DMesh(true);
                break;
            case ViewDisplayMode.ThreeDAnimation:
                Show3DMesh(true);
                _glowAnimators.ForAll(ga => ga.gameObject.SetActive(true));
                _starAnimator.enabled = true;
                _animation.enabled = true;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    private void Show3DMesh(bool toShow) {
        // TODO Star mesh shows all the time for now
    }

    private void Show2DIcon(bool toShow) {
        // TODO need icon 
    }

    private void InitializeStarSettings() {
        _starLight.range = GameManager.Settings.UniverseSize.Radius();
        _starLight.intensity = 0.5F;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

