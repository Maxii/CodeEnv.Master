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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
        set {
            D.AssertDefault((int)_resourceID);   // occurs only once between Resets
            SetProperty<ResourceID>(ref _resourceID, value, "ResourceID", ResourceIDPropSetHandler);
        }
    }

    public override FormID FormID { get { return FormID.ResourceHud; } }

    private UILabel _categoryLabel;
    private UILabel _descriptionLabel;
    private UILabel _imageNameLabel;
    private UISprite _imageSprite;

    protected override void InitializeValuesAndReferences() {
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _categoryLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ClampContent);   // HACK
        _descriptionLabel = immediateChildLabels.Single(l => l.overflowMethod == UILabel.Overflow.ResizeHeight);    // HACK
        var imageFrameSprite = gameObject.GetSafeComponentsInChildren<UISprite>().Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _imageSprite = imageFrameSprite.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        _imageNameLabel = imageFrameSprite.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void ResourceIDPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected override void AssignValuesToMembers() {
        _categoryLabel.text = ResourceID.GetResourceCategory().GetValueName();
        _descriptionLabel.text = ResourceID.GetResourceDescription();
        _imageSprite.atlas = ResourceID.GetImageAtlasID().GetAtlas();
        _imageSprite.spriteName = ResourceID.GetImageFilename();
        _imageNameLabel.text = ResourceID.GetValueName();
    }

    public override void Reset() {
        _resourceID = default(ResourceID);
    }

    protected override void Cleanup() { }


}

