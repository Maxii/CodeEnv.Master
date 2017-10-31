// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WorldTrackingSprite.cs
// Sprite resident in world space that tracks world objects and maintains a constant size on the screen.  
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
/// <remarks>This version is parented to a common UIPanel for the tracked target's type. It does not interact with the mouse.</remarks>
/// </summary>
public class WorldTrackingSprite : AWorldTrackingWidget_ConstantSize, IWorldTrackingSprite {

    private static Vector2 v2Two = new Vector2(2, 2);

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    private TrackingIconInfo _iconInfo;
    public TrackingIconInfo IconInfo {
        get { return _iconInfo; }
        set { SetProperty<TrackingIconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
    }

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != v2Two && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");

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
        enabled = Target.IsMobile;
    }

    protected override void Hide() {
        base.Hide();
        enabled = false;
    }

    protected virtual void SetDimensions(IntVector2 size) {
        Widget.SetDimensions(size.x, size.y);
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

    #endregion

    protected override void Cleanup() { }

}

