﻿// --------------------------------------------------------------------------------------------------------------------
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

using System.Linq;
using CodeEnv.Master.Common;
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

    protected override void Awake() {
        base.Awake();
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxCelestialObjectAnimateDistanceFactor * Radius);
    }

    protected override void Start() {
        base.Start();
        InitializePresenter();  // moved from Awake as some Presenters need immediate access to this Behaviour's parent which may not yet be assigned if Instantiated at runtime
    }

    protected virtual void InitializePresenter() {
        Presenter = new SectorPresenter(this);
    }

    protected override void RegisterComponentsToDisable() {
        // disable the Animation in the item's mesh, but no other animations
        disableComponentOnCameraDistance = disableComponentOnCameraDistance.Union(gameObject.GetComponentsInImmediateChildren<Animation>());
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

