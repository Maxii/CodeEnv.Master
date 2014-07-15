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

    /// <summary>
    /// The Collider encompassing the bounds of the UniverseCenter that intercepts input events for this view. 
    /// This collider also detects collisions with other operating objects in the universe and therefore
    /// should NOT be disabled when it is undiscernible.
    /// </summary>
    protected new SphereCollider Collider { get { return base.Collider as SphereCollider; } }

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 5F;
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

