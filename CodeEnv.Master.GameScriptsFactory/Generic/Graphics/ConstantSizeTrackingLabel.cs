// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstantSizeTrackingLabel.cs
// Label resident in world space that tracks world objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;

/// <summary>
/// Label resident in world space that tracks world objects.  The user perceives the widget at a constant size as the camera and/or tracked gameObject moves.  
/// </summary>
public class ConstantSizeTrackingLabel : AWorldTrackingWidget_ConstantSize {

    /// <summary>
    /// Temporary. The desired size in pixels of this font. 
    /// TODO: As I have only a couple of font sizes available now, this setting will change the size 
    /// of the UILabel font to reflect this desired size. The longer term solution is to have my fonts be the 
    /// desired size to begin with, eliminating any change requirement. 
    /// </summary>
    public int desiredFontSize = 6;

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
        OnTextChanged();
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

    private void OnTextChanged() {
        Widget.MakePixelPerfect();
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

