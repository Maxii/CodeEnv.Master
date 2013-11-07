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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///A class for managing the UI of the object at the center of the universe.
/// </summary>
public class UniverseCenterView : View {

    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 5F;
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

