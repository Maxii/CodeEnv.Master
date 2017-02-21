// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DummyTargetManager.cs
// A ICameraTargetable gameObject moved around the camera's OuterBoundary to allow the camera to zoom, truck and pedestal.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A ICameraTargetable gameObject moved around the camera's OuterBoundary to allow the camera to zoom, truck and pedestal.
/// </summary>
public class DummyTargetManager : AMonoSingleton<DummyTargetManager>, ICameraTargetable {

    private const float CameraMinViewingDistance_DummyTarget = 5F;

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsCameraTargetEligible { get { return true; } }

    public float MinimumCameraViewingDistance { get { return CameraMinViewingDistance_DummyTarget; } }

    #endregion

}

