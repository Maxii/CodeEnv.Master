﻿// --------------------------------------------------------------------------------------------------------------------
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
/// Sprite resident in world space that tracks world objects.  The user perceives the widget changing size as the camera and/or tracked gameObject moves.
/// </summary>
public class VariableSizeTrackingSprite : AWorldTrackingWidget_VariableSize {

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", OnAtlasIDChanged); }
    }

    /// <summary>
    /// Temporary. The desired dimensions in pixels of this sprite. 
    /// TODO: As I have only one sprite size available now (32, 32), this setting will change the dimensions
    /// of UISprite to reflect this desired size. The longer term solution is to have my sprites be the 
    /// desired size to begin with, eliminating any dimension change requirement. 
    /// </summary>
    public Vector2 desiredSpriteDimensions = new Vector2(16, 16);

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, "Sprite size not set.", this);
        __AdjustSpriteSize();
    }

    /// <summary>
    /// Sets the specified sprite name.
    /// </summary>
    /// <param name="spriteName">Name of the sprite.</param>
    public override void Set(string spriteName) {
        D.Assert(Widget.atlas != null, "Sprite atlas has not been assigned.", true, WidgetTransform);
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

    private void OnAtlasIDChanged() {
        Widget.atlas = AtlasID.GetAtlas();
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

