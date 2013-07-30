// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FollowableItemControl.cs
// Manages a moveable Celestial Object's interaction with the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Manages a moveable Celestial Object's interaction with the camera.
/// </summary>
public class FollowableItemControl : FocusableItemControl, ICameraFollowable {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 2.0F;
    public float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion
}

