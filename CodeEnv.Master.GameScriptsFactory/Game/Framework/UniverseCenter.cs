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
public class UniverseCenter : StationaryItem {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    protected override float CalcOptimalCameraViewingDistance() {
        return GameManager.Settings.UniverseSize.Radius() * 0.9F;   // IMPROVE
    }

    #endregion

}

