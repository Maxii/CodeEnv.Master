// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstantSizeTrackingSprite.cs
//  Sprite resident in world space that tracks world objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Sprite resident in world space that tracks world objects.  
/// The user perceives the widget at a constant size as the camera and/or tracked gameObject moves. 
/// </summary>
public class ConstantSizeTrackingSprite : AWorldTrackingWidget_ConstantSize, ITrackingSprite {

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    private IconInfo _iconInfo;
    public IconInfo IconInfo {
        get { return _iconInfo; }
        set { SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
    }

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");

        GameObject widgetGo = WidgetTransform.gameObject;

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        CameraLosChangedListener = widgetGo.AddComponent<CameraLosChangedListener>();
        Profiler.EndSample();
        // Note: do not disable CameraLosChangedListener, as disabling it will also eliminate OnBecameVisible() events
    }

    /// <summary>
    /// Sets the sprite filename to load.
    /// </summary>
    /// <param name="spriteFilename">The Filename of the sprite in the atlas.</param>
    public override void Set(string spriteFilename) {
        D.Assert(Widget.atlas != null, WidgetTransform, "Sprite atlas has not been assigned.");
        Widget.spriteName = spriteFilename;
    }

    protected virtual void SetDimensions(Vector2 size) {
        Widget.SetDimensions(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
    }

    #region Event and Property Change Handlers

    private void IconInfoPropChangedHandler() {
        Widget.atlas = IconInfo.AtlasID.GetAtlas();
        Set(IconInfo.Filename);
        Color = IconInfo.Color;
        SetDimensions(IconInfo.Size);
        Placement = IconInfo.Placement;
        NGUITools.SetLayer(gameObject, (int)IconInfo.Layer);
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

