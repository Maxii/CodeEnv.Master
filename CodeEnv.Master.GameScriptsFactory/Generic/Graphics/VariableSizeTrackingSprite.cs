// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VariableSizeTrackingSprite.cs
// Sprite resident in world space that tracks world objects. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Sprite resident in world space that tracks world objects.  
/// The user perceives the widget changing size as the camera and/or tracked gameObject moves.
/// </summary>
public class VariableSizeTrackingSprite : AWorldTrackingWidget_VariableSize {

    /// <summary>
    /// TEMP. The desired dimensions in pixels of this sprite. 
    /// </summary>
    public Vector2 desiredSpriteDimensions = new Vector2(16, 16);    //TODO: Use properly sized sprite

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", AtlasIDPropChangedHandler); }
    }

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");
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

    protected override int GetSmallestWidgetDimension() {
        return Mathf.RoundToInt(Mathf.Min(Widget.localSize.x, Widget.localSize.y));
    }

    private void __AdjustSpriteSize() {
        int spriteWidth = Mathf.RoundToInt(desiredSpriteDimensions.x);
        int spriteHeight = Mathf.RoundToInt(desiredSpriteDimensions.y);
        Widget.SetDimensions(spriteWidth, spriteHeight);
    }

    #region Event and Property Change Handlers

    private void AtlasIDPropChangedHandler() {
        Widget.atlas = AtlasID.GetAtlas();
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

