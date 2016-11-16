// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UITrackingLabel.cs
//  Label on the UI layer that tracks world objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Label on the UI layer that tracks world objects.  The user perceives the label as maintaining a constant size on the screen
/// independent of the distance from the tracked world object to the main camera.
/// </summary>
public class UITrackingLabel : AUITrackingWidget {

    public int desiredFontSize = 8;

    protected new UILabel Widget { get { return base.Widget as UILabel; } }

    protected override void Awake() {
        base.Awake();
        __AdjustFontSize();
    }

    /// <summary>
    /// Sets the label's text.
    /// </summary>
    /// <param name="text">The text.</param>
    public override void Set(string text) {
        if (Widget.text == text) { return; }
        Widget.text = text;
        Widget.MakePixelPerfect();
    }

    /// <summary>
    /// Assesses whether to show or hide the widget based on
    /// the widget's distance to the camera. Expensive.
    /// <remarks>11.13.16 Currently, only UITrackingLabels use min/max show distances.</remarks>
    /// </summary>
    protected override void AssessShowDistance() {
        base.AssessShowDistance();
        if (_toCheckShowDistance) {
            if (IsWithinShowDistance) {
                Show();
            }
            else {
                Hide();
            }
        }
    }

    protected override void AlignWidgetOtherTo(WidgetPlacement placement) {
        base.AlignWidgetOtherTo(placement);
        var alignment = NGUIText.Alignment.Center;

        switch (placement) {
            case WidgetPlacement.Above:
            case WidgetPlacement.Below:
            case WidgetPlacement.Over:
                break;
            case WidgetPlacement.Left:
            case WidgetPlacement.AboveLeft:
            case WidgetPlacement.BelowLeft:
                alignment = NGUIText.Alignment.Right;
                break;
            case WidgetPlacement.Right:
            case WidgetPlacement.AboveRight:
            case WidgetPlacement.BelowRight:
                alignment = NGUIText.Alignment.Left;
                break;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
        Widget.alignment = alignment;
    }

    private void __AdjustFontSize() {
        Widget.fontSize = desiredFontSize;
        Widget.MakePixelPerfect();
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

