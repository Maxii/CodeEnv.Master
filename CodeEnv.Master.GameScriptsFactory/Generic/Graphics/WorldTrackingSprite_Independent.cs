// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WorldTrackingSprite_Independent.cs
/// Sprite resident in world space that tracks world objects and maintains a constant size on the screen.
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
/// Sprite resident in world space that tracks world objects and maintains a constant size on the screen.
/// <remarks>This version has its own UIPanel which is parented to the tracked target. It does not interact with the mouse.</remarks>
/// </summary>
public class WorldTrackingSprite_Independent : AWorldTrackingWidget_ConstantSize, IWorldTrackingSprite_Independent {

    private static Vector2 v2Two = new Vector2(2, 2);

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    private TrackingIconInfo _iconInfo;
    public TrackingIconInfo IconInfo {
        get { return _iconInfo; }
        set { SetProperty<TrackingIconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
    }

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

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    private UIPanel _panel;

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != v2Two && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");

        _panel = gameObject.GetSingleComponentInChildren<UIPanel>();
        _panel.depth = DrawDepth;
        _panel.alpha = Constants.ZeroF;
        _panel.enabled = false;

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

    protected virtual void SetDimensions(Vector2 size) {
        Widget.SetDimensions(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
    }

    protected override void SetPosition() {
        //D.Log("{0} aligning position with target {1}. Offset is {2}.", DebugName, Target.Transform.name, _offset);
        transform.localPosition = _offset;
    }

    #region Event and Property Change Handlers

    private void IconInfoPropChangedHandler() {
        D.AssertNotNull(IconInfo);

        Widget.atlas = IconInfo.AtlasID.GetAtlas();
        Set(IconInfo.Filename);
        Color = IconInfo.Color;
        SetDimensions(IconInfo.Size);
        Placement = IconInfo.Placement;
        NGUITools.SetLayer(gameObject, (int)IconInfo.Layer);

        CameraLosChangedListener.CheckForInvisibleMeshSizeChange();
    }

    private void DrawDepthPropChangedHandler() {
        _panel.depth = DrawDepth;
    }

    #endregion

    protected override void Cleanup() { }


    #region Debug

    protected override void __RenameGameObjects() {
        base.__RenameGameObjects();
        if (Target != null) {   // Target can be null if OptionalRootName is set before Target
            var rootName = OptionalRootName.IsNullOrEmpty() ? Target.DebugName : OptionalRootName;
            _panel.name = rootName + Constants.Space + typeof(UIPanel).Name;
        }
    }

    #endregion

}

