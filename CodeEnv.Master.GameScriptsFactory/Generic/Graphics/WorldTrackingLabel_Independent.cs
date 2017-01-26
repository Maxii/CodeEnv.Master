// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WorldTrackingLabel_Independent.cs
/// Label resident in world space that tracks world objects and maintains a constant size on the screen.  
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
/// <remarks>This version has its own UIPanel which is parented to the tracked target. It does not interact with the mouse.</remarks>
/// </summary>
public class WorldTrackingLabel_Independent : AWorldTrackingWidget_ConstantSize {

    /// <summary>
    /// Temporary. The desired size in pixels of this font. 
    /// of the UILabel font to reflect this desired size. The longer term solution is to have my fonts be the 
    /// desired size to begin with, eliminating any change requirement. 
    /// </summary>
    public int desiredFontSize = 6;    //TODO: As I have only a couple of font sizes available now, this setting will change the size 

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    private int _drawDepth = -5;
    /// <summary>
    /// The depth of the UIPanel that determines draw order. Higher values will be
    /// drawn after lower values placing them in front of the lower values. In general, 
    /// these depth values should be less than 0 as the Panels that manage the UI are
    /// usually set to 0 so they draw over other Panels.
    /// </summary>
    public int DrawDepth {
        get { return _drawDepth; }
        set { SetProperty<int>(ref _drawDepth, value, "DrawDepth", DrawDepthPropChangedHandler); }
    }

    protected new UILabel Widget { get { return base.Widget as UILabel; } }

    private UIPanel _panel;

    protected override void Awake() {
        base.Awake();
        _panel = gameObject.GetSingleComponentInChildren<UIPanel>();
        _panel.depth = DrawDepth;
        _panel.alpha = Constants.ZeroF;
        _panel.enabled = false;

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
        _panel.alpha = Constants.OneF;
        _panel.enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        _panel.alpha = Constants.ZeroF;
        _panel.enabled = false;
    }

    protected override void SetPosition() {
        //D.Log("{0} aligning position with target {1}. Offset is {2}.", DebugName, Target.Transform.name, _offset);
        transform.localPosition = _offset;
    }

    #region Event and Property Change Handlers

    private void DrawDepthPropChangedHandler() {
        _panel.depth = DrawDepth;
    }

    #endregion

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

    protected override void __RenameGameObjects() {
        base.__RenameGameObjects();
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DebugName : OptionalRootName;
            _panel.name = rootName + Constants.Space + typeof(UIPanel).Name;
        }
    }

    #endregion


}

