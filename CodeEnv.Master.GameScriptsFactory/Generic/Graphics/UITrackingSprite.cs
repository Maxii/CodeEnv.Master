// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UITrackingSprite.cs
// Sprite on the UI layer that tracks world objects
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
/// Sprite on the UI layer that tracks world objects.  The user perceives the sprite as maintaining a constant size on the screen
/// independant of the distance from the tracked world object to the main camera.
/// </summary>
public class UITrackingSprite : AUITrackingWidget {

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", OnAtlasIDChanged); }
    }

    /// <summary>
    /// Temporary. The desired dimensions in pixels of this sprite. 
    /// TODO: As I have only one sprite size available now (32, 32), this setting will scale
    /// UISprite to reflect this desired size. The longer term solution is to have my sprites be the 
    /// desired size to begin with, eliminating any scaling requirement. 
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

