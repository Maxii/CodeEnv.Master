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
/// independent of the distance from the tracked world object to the main camera.
/// </summary>
public abstract class AUITrackingWidget : ATrackingWidget {

    private Camera _uiCamera;

    protected override void Awake() {
        base.Awake();
        D.AssertEqual(Layers.UI, (Layers)gameObject.layer);
        _uiCamera = NGUITools.FindCameraForLayer((int)Layers.UI);
    }

    void Update() {  // OPTIMIZE Could be done ~ 4 times less frequently, change when improve TrackingWidget
        RefreshPosition();  // 
        AssessShowDistance();
    }

    /// <summary>
    /// Assesses whether to show or hide the widget based on
    /// the widget's distance to the camera. If implemented, this is expensive.
    /// Default does nothing.
    /// <remarks>11.13.16 Currently, only UITrackingLabels use min/max show distances.</remarks>
    /// </summary>
    protected virtual void AssessShowDistance() { }

    protected override void Show() {
        base.Show();
        enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        enabled = false;
    }


    protected override float CalcMaxShowDistance(float max) {
        // widgets are constant size in UI Layer so always legible no matter what max is used
        return max;
    }

    protected override void SetPosition() {
        transform.OverlayPosition(Target.Position + _offset, Camera.main, _uiCamera);
        //D.Log("{0} resulting position of UI element = {1}.", Name, transform.position);
        transform.SetWorldPositionZ(Constants.ZeroF);
    }

    /// <summary>
    /// Refreshes the position of this widget.
    /// <remarks>Only needed by UITrackingWidgets as all others are children of their tracked target.</remarks>
    /// </summary>
    private void RefreshPosition() {
        SetPosition();
    }

}

