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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Sprite resident in world space that tracks world objects.  The user perceives the widget at a constant size as the camera and/or tracked gameObject moves. 
/// </summary>
public class ConstantSizeTrackingSprite : AWorldTrackingWidget_ConstantSize {

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.atlas != null, "Sprite atlas has not been assigned.", true, WidgetTransform);
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, "Sprite size not set.", this);
    }

    /// <summary>
    /// Sets the sprite filename to load.
    /// </summary>
    /// <param name="spriteFilename">The Filename of the sprite in the atlas.</param>
    public override void Set(string spriteFilename) {
        Widget.spriteName = spriteFilename;
    }

    /// <summary>
    /// Temporary. Sets the dimensions of the sprite in pixels. 
    /// If not set, the sprite will assume the dimensions found in the atlas.
    /// 
    /// TODO: As I have only one sprite size available now (32, 32), this setting will change the dimensions
    /// of UISprite to reflect this desired size. The longer term solution is to have my sprites be the 
    /// desired size to begin with, eliminating any dimension change requirement. 
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public virtual void __SetDimensions(int width, int height) {
        Widget.SetDimensions(width, height);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

