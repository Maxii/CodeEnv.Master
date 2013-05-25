// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EclipticPlaneManager.cs
// Manages a Systems Ecliptic plane.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Manages a Systems Ecliptic plane.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class EclipticPlaneManager : AMonoBehaviourBase, ICameraTargetable {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    [SerializeField]
    private float minimumCameraViewingDistance = 10F;
    public float MinimumCameraViewingDistance {
        get {
            return minimumCameraViewingDistance;
        }
    }

    #endregion

}

