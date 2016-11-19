// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VariableSizeTrackingLabel.cs
// Label resident in world space that tracks world objects. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;

/// <summary>
/// Label resident in world space that tracks world objects.  The user perceives the widget changing size as the camera and/or tracked gameObject moves.
/// </summary>
public class VariableSizeTrackingLabel : AWorldTrackingWidget_VariableSize {

    /// <summary>
    /// Temporary. The desired size in pixels of this font. 
    /// </summary>
    public int desiredFontSize = 6;    //TODO: User properly sized font

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

    protected override int GetSmallestWidgetDimension() {
        return Widget.fontSize;
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

