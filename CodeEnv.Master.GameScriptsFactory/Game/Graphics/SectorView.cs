// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorView.cs
// A class for managing the UI of a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a Sector.
/// </summary>
public class SectorView : AView, IViewable {

    public SectorPresenter Presenter { get; private set; }

    protected float _radius;
    /// <summary>
    /// The [float] radius of this object in units measured as the distance from the 
    ///center to the min or max extent. As bounds is a bounding box it is the longest 
    /// diagonal from the center to a corner of the box. Most of the time, the collider can be
    /// used to calculate this size, assuming it doesn't change size dynmaically. 
    /// Alternatively, a mesh can be used.
    /// </summary>
    public override float Radius {
        get {
            if (_radius == Constants.ZeroF) {
                _radius = TempGameValues.SectorDiagonalLength / 2F;
            }
            return _radius;
        }
    }

    // private Animation _animation;    // TODO meshes and animations need to be added to sectors

    protected override void Awake() {
        base.Awake();
        // _animation = gameObject.GetComponentInChildren<Animation>();
    }

    protected override void Start() {
        base.Start();
        InitializePresenter();  // moved from Awake as some Presenters need immediate access to this Behaviour's parent which may not yet be assigned if Instantiated at runtime
    }

    protected virtual void InitializePresenter() {
        Presenter = new SectorPresenter(this);
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
                //_animation.enabled = false;
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
                //_animation.enabled = true;
                break;
            case ViewDisplayMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(DisplayMode));
        }
    }

    private void Show3DMesh(bool toShow) {
        // TODO need a sector mesh 
    }

    private void Show2DIcon(bool toShow) {
        // TODO unclear whether sector meshes will have icons
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

