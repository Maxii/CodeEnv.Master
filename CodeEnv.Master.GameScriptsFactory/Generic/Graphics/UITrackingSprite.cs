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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Sprite on the UI layer that tracks world objects.  The user perceives the sprite as maintaining a constant size on the screen
/// independent of the distance from the tracked world object to the main camera.
/// </summary>
public class UITrackingSprite : AUITrackingWidget {

    private static Vector2 v2Two = new Vector2(2, 2);

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", AtlasIDPropChangedHandler); }
    }

    /// <summary>
    /// Temporary. The desired dimensions in pixels of this sprite. 
    /// </summary>
    public Vector2 desiredSpriteDimensions = new Vector2(16, 16);    //TODO: Use properly sized sprite

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != v2Two && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");
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

}

