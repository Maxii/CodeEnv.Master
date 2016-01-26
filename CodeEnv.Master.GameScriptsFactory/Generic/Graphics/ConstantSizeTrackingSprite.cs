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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Sprite resident in world space that tracks world objects.  The user perceives the widget at a constant size as the camera and/or tracked gameObject moves. 
/// </summary>
public class ConstantSizeTrackingSprite : AWorldTrackingWidget_ConstantSize {

    private AtlasID _atlasID;
    public AtlasID AtlasID {
        get { return _atlasID; }
        set { SetProperty<AtlasID>(ref _atlasID, value, "AtlasID", AtlasIDPropChangedHandler); }
    }

    protected new UISprite Widget { get { return base.Widget as UISprite; } }

    protected override void Awake() {
        base.Awake();
        D.Assert(Widget.localSize != new Vector2(2, 2) && Widget.localSize != Vector2.zero, gameObject, "Sprite size not set.");
    }

    /// <summary>
    /// Sets the sprite filename to load.
    /// </summary>
    /// <param name="spriteFilename">The Filename of the sprite in the atlas.</param>
    public override void Set(string spriteFilename) {
        D.Assert(Widget.atlas != null, WidgetTransform, "Sprite atlas has not been assigned.");
        Widget.spriteName = spriteFilename;
    }

    /// <summary>
    /// Temporary. Sets the dimensions of the sprite in pixels. 
    /// If not set, the sprite will assume the dimensions found in the atlas.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public virtual void __SetDimensions(int width, int height) {        //TODO: Start with sprites of desired size
        Widget.SetDimensions(width, height);
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

