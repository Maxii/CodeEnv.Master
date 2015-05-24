// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUITrackingWidget.cs
// Abstract base class widget parent on the UI layer that tracks world objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class widget parent on the UI layer that tracks world objects.  The user perceives the widget as maintaining a constant size on the screen
/// independant of the distance from the tracked world object to the main camera.
/// </summary>
public abstract class AUITrackingWidget : ATrackingWidget {

    private Camera _uiCamera;

    protected override void Awake() {
        base.Awake();
        D.Assert((Layers)gameObject.layer == Layers.UI);
        _uiCamera = NGUITools.FindCameraForLayer((int)Layers.UI);
    }

    protected override float CalcMaxShowDistance(float max) {
        // widgets are constant size in UI Layer so always legible no matter what max is used
        return max;
    }

    protected override void SetPosition() {
        //D.Log("Target position = {0}, Offset = {1}.", _target.Transform.position, _offset);
        _transform.OverlayPosition(Target.Position + _offset, Camera.main, _uiCamera);
        //D.Log("Resulting position of UI element = {0}.", _transform.position);
        _transform.SetWorldPositionZ(Constants.ZeroF);
    }

    protected override void RefreshPositionOnUpdate() {
        SetPosition();
    }

}

