// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PooledSphericalHighlight.cs
// Spawnable Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Spawnable Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target.
/// </summary>
public class PooledSphericalHighlight : SphericalHighlight {

    #region Event and Property Change Handlers

    private void OnSpawned() {
        ValidateReuseable();
    }

    private void OnDespawned() {
        ResetForReuse();
    }

    #endregion

    protected override void Cleanup() {
        DestroyTrackingLabel();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

