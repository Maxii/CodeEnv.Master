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

    private SphereCollider _keepoutCollider;

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 5F;
        (_collider as SphereCollider).radius = TempGameValues.UniverseCenterRadius;
        _keepoutCollider = gameObject.GetComponentsInChildren<SphereCollider>().Single(c => c.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        _keepoutCollider.radius = (_collider as SphereCollider).radius * TempGameValues.KeepoutRadiusMultiplier;
        Subscribe();    // no real need to subscribe at all if only subscription is PlayerIntelCoverage changes which these don't have
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

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get { return true; }
    }

    protected override float CalcOptimalCameraViewingDistance() {
        return GameManager.Settings.UniverseSize.Radius() * 0.9F;   // IMPROVE
    }

    #endregion

}

