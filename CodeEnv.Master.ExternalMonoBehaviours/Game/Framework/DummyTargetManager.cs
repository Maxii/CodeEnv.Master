// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DummyTargetManager.cs
// Placeholder containing ICameraTargetable interface values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Placeholder containing ICameraTargetable interface values.
/// </summary>
public class DummyTargetManager : MonoBehaviour, ICameraTargetable {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsTargetable {
        get { return true; }
    }

    public float MinimumCameraViewingDistance {
        get { return 50F; }
    }

    #endregion
}

