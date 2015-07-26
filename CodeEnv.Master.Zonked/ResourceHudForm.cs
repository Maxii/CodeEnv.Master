// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceHudElement.cs
// HudForm customized for displaying info about Resources.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// HudForm customized for displaying info about Resources.
/// </summary>
[System.Obsolete]
public class ResourceHudForm : AHudForm {

    public override HudFormID FormID { get { return HudFormID.Resource; } }

    private UILabel _categoryLabel;
    private UILabel _descriptionLabel;
    private UILabel _imageNameLabel;
    private UISprite _imageSprite;

    protected override void Awake() {
        base.Awake();
        InitializeReferences();
    }

    private void InitializeReferences() {
        var immediateChildLabels = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UILabel>();
        _categoryLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ClampContent);
        _descriptionLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ResizeHeight);
        var immediateChildSprites = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UISprite>();
        var imageFrameSprite = immediateChildSprites.Single(s => s.name != TempGameValues.BackgroundSpriteName);
        _imageSprite = imageFrameSprite.gameObject.GetSafeMonoBehaviourInImmediateChildren<UISprite>();
        _imageNameLabel = imageFrameSprite.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    protected override void AssignValuesToWidgets() {
        var content = FormContent as ResourceHudFormContent;
        ResourceID resID = content.ResourceID;
        _categoryLabel.text = resID.GetResourceCategory().GetValueName();
        _descriptionLabel.text = resID.GetResourceDescription();
        _imageSprite.atlas = MyNguiUtilities.GetAtlas(resID.GetImageAtlasID());
        _imageSprite.spriteName = resID.GetImageFilename();
        _imageNameLabel.text = resID.GetValueName();
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

