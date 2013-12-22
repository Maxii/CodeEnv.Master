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

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///A class for managing the UI of the object at the center of the universe.
/// </summary>
public class UniverseCenterView : AFocusableView {

    private SphereCollider _keepoutCollider;
    private Animation _animation;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 5F;
        _animation = gameObject.GetComponentInChildren<Animation>();
        (_collider as SphereCollider).radius = TempGameValues.UniverseCenterRadius;
        _keepoutCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(c => c.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
    }

    protected override void InitializePresenter() {
        Presenter = new UniverseCenterPresenter(this);
    }

    protected override void OnDisplayModeChanging(ViewDisplayMode newMode) {
        base.OnDisplayModeChanging(newMode);
        ViewDisplayMode previousMode = DisplayMode;
        switch (previousMode) {
            case ViewDisplayMode.Hide:
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(false);
                break;
            case ViewDisplayMode.ThreeD:
                if (newMode != ViewDisplayMode.ThreeDAnimation) { Show3DMesh(false); }
                break;
            case ViewDisplayMode.ThreeDAnimation:
                if (newMode != ViewDisplayMode.ThreeD) { Show3DMesh(false); }
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
                break;
            case ViewDisplayMode.TwoD:
                Show2DIcon(true);
                break;
            case ViewDisplayMode.ThreeD:
                Show3DMesh(true);
                break;
            case ViewDisplayMode.ThreeDAnimation:
                Show3DMesh(true);
                _animation.enabled = true;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    private void Show2DIcon(bool toShow) {
        // TODO
    }

    private void Show3DMesh(bool toShow) {
        // TODO shows all the time right now
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get { return true; }
    }

    protected override float CalcOptimalCameraViewingDistance() {
        return GameManager.Settings.UniverseSize.Radius() * 0.9F;   // IMPROVE
    }

    #endregion


}

