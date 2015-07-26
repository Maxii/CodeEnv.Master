// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceForm.cs
// Forms that displays info about a resource.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Forms that displays info about a resource.
/// </summary>
public class ResourceForm : AForm {

    private ResourceID _resourceID;
    public ResourceID ResourceID {
        get { return _resourceID; }
        set { SetProperty<ResourceID>(ref _resourceID, value, "ResourceID", OnResourceIDChanged); }
    }

    public override FormID FormID { get { return FormID.ResourceHud; } }

    private UILabel _categoryLabel;
    private UILabel _descriptionLabel;
    private UILabel _imageNameLabel;
    private UISprite _imageSprite;

    protected override void InitializeValuesAndReferences() {
        var immediateChildLabels = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UILabel>();
        _categoryLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ClampContent);
        _descriptionLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ResizeHeight);
        var imageFrameSprite = gameObject.GetSafeMonoBehavioursInChildren<UISprite>().Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _imageSprite = imageFrameSprite.gameObject.GetSafeFirstMonoBehaviourInImmediateChildrenOnly<UISprite>();
        _imageNameLabel = imageFrameSprite.gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
    }

    private void OnResourceIDChanged() {
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        _categoryLabel.text = ResourceID.GetResourceCategory().GetValueName();
        _descriptionLabel.text = ResourceID.GetResourceDescription();
        _imageSprite.atlas = ResourceID.GetImageAtlasID().GetAtlas();
        _imageSprite.spriteName = ResourceID.GetImageFilename();
        _imageNameLabel.text = ResourceID.GetValueName();
    }

    public override void Reset() {
        _resourceID = ResourceID.None;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

