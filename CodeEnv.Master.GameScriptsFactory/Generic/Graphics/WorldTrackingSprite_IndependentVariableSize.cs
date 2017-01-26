// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WorldTrackingSprite_IndependentVariableSize.cs
// Sprite resident in world space that tracks world objects and does not maintain a constant size on the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Sprite resident in world space that tracks world objects and does not maintain a constant size on the screen.
/// <remarks>This version has its own UIPanel which is parented to the tracked target. It does not interact with the mouse.</remarks>
/// </summary>
public class WorldTrackingSprite_IndependentVariableSize : AWorldTrackingWidget_VariableSize {

    /// <summary>
    /// TEMP. The desired dimensions in pixels of this sprite. 
    /// </summary>
    public Vector2 desiredSpriteDimensions = new Vector2(16, 16);    //TODO: Use properly sized sprite

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", AtlasIDPropChangedHandler); }
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
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");
        _panel = gameObject.GetSingleComponentInChildren<UIPanel>();
        _panel.depth = DrawDepth;
        _panel.alpha = Constants.ZeroF;
        _panel.enabled = false;

        __AdjustSpriteSize();
    }

    /// <summary>
    /// Sets the specified sprite name.
    /// </summary>
    /// <param name="spriteName">Name of the sprite.</param>
    public override void Set(string spriteName) {
        D.Assert(Widget.atlas != null, WidgetTransform, "Sprite atlas has not been assigned.");
        Widget.spriteName = spriteName;
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

    private void AtlasIDPropChangedHandler() {
        Widget.atlas = AtlasID.GetAtlas();
    }

    #endregion

    protected override int GetSmallestWidgetDimension() {
        return Mathf.RoundToInt(Mathf.Min(Widget.localSize.x, Widget.localSize.y));
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private void __AdjustSpriteSize() {
        int spriteWidth = Mathf.RoundToInt(desiredSpriteDimensions.x);
        int spriteHeight = Mathf.RoundToInt(desiredSpriteDimensions.y);
        Widget.SetDimensions(spriteWidth, spriteHeight);
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

