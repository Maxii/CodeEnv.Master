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

using System.Linq;

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a planetoid.
/// </summary>
public class PlanetoidView : MovingView {

    protected new PlanetoidPresenter Presenter {
        get { return base.Presenter as PlanetoidPresenter; }
        set { base.Presenter = value; }
    }

    private SphereCollider _keepoutCollider;

    protected override void Awake() {
        base.Awake();
        _keepoutCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
    }

    protected override void InitializePresenter() {
        Presenter = new PlanetoidPresenter(this);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

