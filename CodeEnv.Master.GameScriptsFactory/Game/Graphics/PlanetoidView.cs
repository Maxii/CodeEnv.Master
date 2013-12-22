// --------------------------------------------------------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a planetoid.
/// </summary>
public class PlanetoidView : AFollowableView {

    public new PlanetoidPresenter Presenter {
        get { return base.Presenter as PlanetoidPresenter; }
        protected set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;
    private IEnumerable<Animation> _animations;

    protected override void Awake() {
        base.Awake();
        _keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
        _animations = gameObject.GetComponentsInImmediateChildren<Animation>();
    }

    protected override void InitializePresenter() {
        Presenter = new PlanetoidPresenter(this);
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
                if (newMode != ViewDisplayMode.ThreeDAnimation) {
                    Show3DMesh(false);
                }
                break;
            case ViewDisplayMode.ThreeDAnimation:
                if (newMode != ViewDisplayMode.ThreeD) {
                    Show3DMesh(false);
                }
                _animations.ForAll(a => a.enabled = false);
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
                _animations.ForAll(a => a.enabled = true);
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    private void Show3DMesh(bool toShow) {
        // TODO Planetoid mesh always shows at this point
    }

    private void Show2DIcon(bool toShow) {
        // TODO
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

