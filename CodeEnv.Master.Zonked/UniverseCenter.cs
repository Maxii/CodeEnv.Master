// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenter.cs
// Universe Center management.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Universe Center management.
/// </summary>
[System.Obsolete]
public class UniverseCenter : StationaryItem {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible {
        get { return true; }
    }

    protected override float CalcOptimalCameraViewingDistance() {
        return GameManager.GameSettings.UniverseSize.Radius() * 0.9F;   // IMPROVE
    }

    #endregion

}

