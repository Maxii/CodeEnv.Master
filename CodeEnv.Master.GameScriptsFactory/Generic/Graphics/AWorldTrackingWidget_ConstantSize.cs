// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWorldTrackingWidget_ConstantSize.cs
// Abstract base class world-space tracking widget that becomes parented to and tracks a world target. 
// The user perceives the widget at a constant size as the camera and/or tracked gameObject moves.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class world-space tracking widget that becomes parented to and tracks a world target. The user perceives
/// the widget at a constant size as the camera and/or tracked gameObject moves.
/// </summary>
public abstract class AWorldTrackingWidget_ConstantSize : AWorldTrackingWidget {

    protected override void Awake() {
        base.Awake();
        Widget.gameObject.AddComponent<ScaleRelativeToCamera>();
    }

    protected override float CalcMaxShowDistance(float max) {
        // widgets are constant size so always legible no matter what max is used
        return max;
    }

    // avoid turning scaler off as the size of the widget can effect when CameraLOSChangedRelay determines its visibility changes

}

