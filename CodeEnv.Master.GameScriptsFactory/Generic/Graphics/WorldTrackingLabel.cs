// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WorldTrackingLabel.cs
// Label resident in world space that tracks world objects and maintains a constant size on the screen.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Label resident in world space that tracks world objects and maintains a constant size on the screen.  
/// <remarks>This version is parented to a common UIPanel for the tracked target's type. It does not interact with the mouse.</remarks>
/// </summary>
public class WorldTrackingLabel : AWorldTrackingWidget_ConstantSize {

    /// <summary>
    /// Temporary. The desired size in pixels of this font. 
    /// of the UILabel font to reflect this desired size. The longer term solution is to have my fonts be the 
    /// desired size to begin with, eliminating any change requirement. 
    /// </summary>
    public int desiredFontSize = 6;    //TODO: As I have only a couple of font sizes available now, this setting will change the size 

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    protected new UILabel Widget { get { return base.Widget as UILabel; } }

    protected override void Awake() {
        base.Awake();

        GameObject widgetGo = WidgetTransform.gameObject;

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        CameraLosChangedListener = widgetGo.AddComponent<CameraLosChangedListener>();
        Profiler.EndSample();
        // Note: do not disable CameraLosChangedListener, as disabling it will also eliminate OnBecameVisible() events

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

    protected override void Show() {
        base.Show();
        enabled = Target.IsMobile;
    }

    protected override void Hide() {
        base.Hide();
        enabled = false;
    }

    void Update() {
        RefreshPosition();
    }

    protected void RefreshPosition() {
        SetPosition();
    }

    protected override void SetPosition() {
        //D.Log("{0} aligning position with target {1}. Offset is {2}.", DebugName, Target.Transform.name, _offset);
        transform.position = Target.Position + _offset;
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

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private void __AdjustFontSize() {
        Widget.fontSize = desiredFontSize;
        Widget.MakePixelPerfect();
    }

    #endregion


}

